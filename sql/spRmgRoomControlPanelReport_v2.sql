/*
    spRmgRoomControlPanelReport_v2
    Bản tối ưu của spRmgRoomControlPanelReport (Room Control Panel). Procedure cũ giữ nguyên.

    Giống bản cũ: tham số, tên cột, 16 dòng thống kê và công thức từng dòng.
    @ZoneCode nhận đúng dạng web đang gửi: A2  hoặc  A1','A2

    Khác bản cũ:
      - Lọc ngày bằng khoảng >= / < thay cho datediff() trên cột.
      - Đọc đặt phòng một lần vào bảng tạm rồi tính các dòng từ đó.
      - Không ghép @ZoneCode vào câu lệnh SQL (hết SQL injection).
      - Gom theo ngày thật. Bản cũ gom theo chuỗi ngày+tháng+năm nên 1/11 và 11/1 bị cộng chung.

    Chạy thử:
      EXEC dbo.spRmgRoomControlPanelReport_v2 '2025-10-30', '2025-11-14', N'A2';
*/
IF OBJECT_ID('dbo.spRmgRoomControlPanelReport_v2', 'P') IS NOT NULL
    DROP PROCEDURE dbo.spRmgRoomControlPanelReport_v2;
GO

-- =============================================
-- Người sửa : Việt Anh
-- Ngày sửa  : 26/11/2026
-- Nội dung  : Tạo bản _v2 từ spRmgRoomControlPanelReport để Room Control Panel tải nhanh hơn
--             1. Lọc ngày bằng khoảng >= / < thay cho datediff() trên cột
--             2. Đọc đặt phòng vào bảng tạm một lần rồi tính các dòng thống kê
--             3. Không ghép @ZoneCode vào câu lệnh SQL (chặn SQL injection)
--             4. Sửa lỗi gom ngày: 1/11 và 11/1 không còn bị cộng chung
-- =============================================
CREATE PROCEDURE dbo.spRmgRoomControlPanelReport_v2
(
    @FromDate datetime,
    @ToDate   datetime,
    @ZoneCode nvarchar(50)
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @From datetime = CAST(CAST(@FromDate AS date) AS datetime);
    DECLARE @End  datetime = DATEADD(DAY, 1, CAST(CAST(@ToDate AS date) AS datetime));
    DECLARE @Zones nvarchar(120) = N',' + REPLACE(REPLACE(@ZoneCode, N'''', N''), N' ', N'') + N',';

    -- Loại phòng thuộc zone đã chọn
    SELECT ID, ISNULL(CASE WHEN IsPseudo <> 1 THEN 1 ELSE 0 END, 0) AS IsReal
    INTO #RoomType
    FROM dbo.RoomType
    WHERE CHARINDEX(N',' + REPLACE(ZoneCode, N' ', N'') + N',', @Zones) > 0;

    -- Ngày trên cột (lấy theo RoomInventory như bản cũ)
    SELECT DISTINCT CAST(CAST([Date] AS date) AS datetime) AS d
    INTO #Days
    FROM dbo.RoomInventory
    WHERE [Date] >= @From AND [Date] < @End;

    DECLARE @Rooms int =
        (SELECT COUNT(a.ID) FROM dbo.Room a WITH (NOLOCK)
         JOIN #RoomType t ON t.ID = a.RoomTypeID AND t.IsReal = 1);

    -- Giá theo ngày trong khoảng (dùng cho 6 dòng)
    SELECT CAST(CAST(c.RateDate AS date) AS datetime) AS d,
           t.IsReal,
           CASE WHEN a.ReservationDate >= @From AND a.ReservationDate < @End THEN 1 ELSE 0 END AS BookedInRange,
           a.NoOfRoom,
           ISNULL(a.NoOfAdult, 0) * ISNULL(a.NoOfRoom, 0) AS Adults,
           (ISNULL(c.NoOfChild, 0) + ISNULL(c.NoOfChild1, 0) + ISNULL(c.NoOfChild2, 0)) * ISNULL(a.NoOfRoom, 0) AS Children
    INTO #Rate
    FROM dbo.ReservationRate c WITH (NOLOCK)
    JOIN dbo.Reservation a WITH (NOLOCK) ON a.ID = c.ReservationID
    JOIN #RoomType t ON t.ID = a.RoomTypeID
    JOIN dbo.ReservationType b ON b.ID = a.ReservationTypeID AND b.Deduct = 1
    WHERE c.RateDate >= @From AND c.RateDate < @End
      AND a.MainGuest = 1 AND a.ReservationNo > 0 AND a.[Status] NOT IN (3, 4, 7);

    SELECT d,
           SUM(CASE WHEN IsReal = 1 AND BookedInRange = 1 THEN ISNULL(NoOfRoom, 0) END) AS Reserved,
           SUM(CASE WHEN IsReal = 1 THEN ISNULL(NoOfRoom, 0) END) AS Occupied,
           SUM(ISNULL(NoOfRoom, 0)) AS Stayovers,
           SUM(Adults) AS Adults,
           SUM(Children) AS Children
    INTO #RateByDay
    FROM #Rate
    GROUP BY d;

    -- Khách đến / đi: đọc bảng Reservation một lần
    SELECT a.ArrivalDate, a.DepartureDate, a.NoOfRoom,
           a.NoOfAdult + a.NoOfChild + a.NoOfChild1 + a.NoOfChild2 AS Persons
    INTO #Resv
    FROM dbo.Reservation a WITH (NOLOCK)
    JOIN #RoomType t ON t.ID = a.RoomTypeID
    JOIN dbo.ReservationType b ON b.ID = a.ReservationTypeID AND b.Deduct = 1
    WHERE a.MainGuest = 1 AND a.ReservationNo > 0 AND a.[Status] NOT IN (3, 4, 7)
      AND ((a.ArrivalDate >= @From AND a.ArrivalDate < @End)
        OR (a.DepartureDate >= @From AND a.DepartureDate < @End));

    SELECT CAST(CAST(ArrivalDate AS date) AS datetime) AS d, SUM(ISNULL(NoOfRoom, 0)) AS Rooms, SUM(Persons) AS Persons
    INTO #Arrival
    FROM #Resv
    WHERE ArrivalDate >= @From AND ArrivalDate < @End
    GROUP BY CAST(ArrivalDate AS date);

    SELECT CAST(CAST(DepartureDate AS date) AS datetime) AS d, SUM(ISNULL(NoOfRoom, 0)) AS Rooms, SUM(Persons) AS Persons
    INTO #Departure
    FROM #Resv
    WHERE DepartureDate >= @From AND DepartureDate < @End
    GROUP BY CAST(DepartureDate AS date);

    -- Phòng đang ở (dòng Min Occupancy)
    SELECT CAST(CAST(c.RateDate AS date) AS datetime) AS d, ISNULL(SUM(a.NoOfRoom), 0) AS Rooms
    INTO #InHouse
    FROM dbo.ReservationRate c WITH (NOLOCK)
    JOIN dbo.Reservation a WITH (NOLOCK) ON a.ID = c.ReservationID
    JOIN dbo.Room r WITH (NOLOCK) ON r.ID = a.RoomID
    JOIN #RoomType t ON t.ID = r.RoomTypeID AND t.IsReal = 1
    JOIN dbo.ReservationType b ON b.ID = a.ReservationTypeID AND b.Deduct = 1
    WHERE c.RateDate >= @From AND c.RateDate < @End
      AND a.[Status] IN (1, 6) AND a.MainGuest = 1 AND a.ReservationNo > 0
      AND DATEDIFF(DAY, a.ArrivalDate, c.RateDate) >= 0
      AND DATEDIFF(DAY, c.RateDate, a.DepartureDate) > 0
    GROUP BY CAST(c.RateDate AS date);

    -- Phòng Out of Order (1) / Out of Service (2) theo ngày
    SELECT k.d,
           COUNT(DISTINCT CASE WHEN o.OOOStatus = 1 THEN o.RoomID END) AS OOO,
           COUNT(DISTINCT CASE WHEN o.OOOStatus = 2 THEN o.RoomID END) AS OOS
    INTO #Blocked
    FROM #Days k
    LEFT JOIN (SELECT bb.RoomID, bb.OOOStatus, bb.FromDateOOO, bb.ToDateOOO
               FROM dbo.BusinessBlock bb
               JOIN dbo.Room r WITH (NOLOCK) ON r.ID = bb.RoomID
               JOIN #RoomType t ON t.ID = r.RoomTypeID
               WHERE bb.OOOStatus IN (1, 2) AND bb.RoomID > 0
                 AND DATEDIFF(DAY, bb.FromDateOOO, @ToDate) >= 0
                 AND DATEDIFF(DAY, bb.ToDateOOO, @FromDate) <= 0) o
        ON DATEDIFF(DAY, k.d, o.FromDateOOO) <= 0 AND DATEDIFF(DAY, k.d, o.ToDateOOO) > 0
    GROUP BY k.d;

    -- 16 dòng thống kê
    SELECT v.Statisticname, v.[Index], k.d AS [Date], CAST(v.Value AS decimal(18, 1)) AS Value
    INTO #Result
    FROM #Days k
    JOIN #Blocked x ON x.d = k.d
    LEFT JOIN #RateByDay r ON r.d = k.d
    LEFT JOIN #Arrival a ON a.d = k.d
    LEFT JOIN #Departure p ON p.d = k.d
    LEFT JOIN #InHouse h ON h.d = k.d
    CROSS APPLY (VALUES
        ('Total Physical Rooms',   1, CAST(@Rooms AS decimal(18, 1))),
        ('Total Inventory Rooms',  2, @Rooms - x.OOO),
        ('Sell Limits',            3, @Rooms),
        ('Total Rooms Reserved',   4, ISNULL(r.Reserved, 0)),
        ('Arrival Room',           5, a.Rooms),
        ('Stayovers',              6, r.Stayovers),
        ('Departure Rooms',        7, p.Rooms),
        ('Adults In House',        8, r.Adults),
        ('Childen In House',       9, r.Children),
        ('Max Occupancy %',       10, CAST(CAST(r.Occupied AS decimal(18, 1)) * 100 / NULLIF(CAST(@Rooms AS decimal(18, 1)), 0) AS decimal(18, 1))),
        ('Min Occupancy %',       11, ISNULL(CAST(CAST(h.Rooms AS decimal(18, 1)) * 100 / NULLIF(CAST(@Rooms AS decimal(18, 1)), 0) AS decimal(18, 1)), 0)),
        ('Available Rooms',       12, @Rooms - x.OOO - r.Occupied),
        ('Out of Order',          13, x.OOO),
        ('Out of Service',        14, x.OOS),
        ('Departure Persons',     15, p.Persons),
        ('Arrival Persons',       16, a.Persons)
    ) v (Statisticname, [Index], Value);

    -- Xoay ngày thành cột
    DECLARE @cols nvarchar(max) = STUFF(
        (SELECT ',' + QUOTENAME(CONVERT(nvarchar(10), d, 111)) FROM #Days ORDER BY d FOR XML PATH('')), 1, 1, '');

    IF @cols IS NULL RETURN;

    DECLARE @sql nvarchar(max) = N'
        SELECT *
        FROM (SELECT Statisticname, [Index], CONVERT(nvarchar(10), [Date], 111) AS [Date], Value FROM #Result) s
        PIVOT (SUM(Value) FOR [Date] IN (' + @cols + N')) p
        ORDER BY [Index];';

    EXEC sys.sp_executesql @sql;
END
GO

/*
    spRmgRoomFacilityForecastReport_v2
    Bản tối ưu của spRmgRoomFacilityForecastReport (Room Facility Forecast). Procedure cũ giữ nguyên.

    Giống bản cũ: tham số, tên cột, các dòng thống kê và công thức từng dòng.
    @ZoneCode nhận đúng dạng web đang gửi: A2  hoặc  A1','A2

    Chạy thử:
      EXEC dbo.spRmgRoomFacilityForecastReport_v2 '2025-10-30', '2025-11-14', N'A2';
*/
IF OBJECT_ID('dbo.spRmgRoomFacilityForecastReport_v2', 'P') IS NOT NULL
    DROP PROCEDURE dbo.spRmgRoomFacilityForecastReport_v2;
GO

-- =============================================
-- Người sửa : Việt Anh
-- Ngày sửa  : 27/11/2026
-- Nội dung  : Tạo bản _v2 từ spRmgRoomFacilityForecastReport để Room Facility Forecast tải nhanh hơn
--             1. Lọc ngày bằng khoảng >= / < thay cho datediff() trên cột
--             2. Đọc đặt phòng vào bảng tạm rồi tính các dòng thống kê, không đọc lại nhiều lần
--             3. Không ghép @ZoneCode vào câu lệnh SQL (chặn SQL injection)
--             4. Dòng Occupancy %: gom theo ngày thật, 1/11 và 11/1 không còn bị cộng chung
-- =============================================
CREATE PROCEDURE dbo.spRmgRoomFacilityForecastReport_v2
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

    -- Ngày trong khoảng (theo RoomInventory như bản cũ)
    SELECT DISTINCT [Date] AS d
    INTO #Days
    FROM dbo.RoomInventory
    WHERE [Date] >= @From AND [Date] < @End;

    -- Giá theo ngày: đọc một lần cho các dòng lấy từ ReservationRate
    SELECT c.RateDate,
           t.IsReal,
           a.[Status],
           a.NoOfRoom,
           ISNULL(c.NoOfAdult, 0) * ISNULL(a.NoOfRoom, 0) AS Adults,
           (ISNULL(c.NoOfChild, 0) + ISNULL(c.NoOfChild1, 0) + ISNULL(c.NoOfChild2, 0)) * ISNULL(a.NoOfRoom, 0) AS Children,
           CASE WHEN a.ProfileGroupID = 0 AND a.ProfileAgentID = 0 AND a.ProfileCompanyID = 0 AND a.ProfileSourceID = 0 THEN 1 ELSE 0 END AS IsIndividual,
           CASE WHEN a.ProfileGroupID <> 0 OR a.ProfileAgentID <> 0 OR a.ProfileCompanyID <> 0 OR a.ProfileSourceID <> 0 THEN 1 ELSE 0 END AS IsNotIndividual
    INTO #Rate
    FROM dbo.ReservationRate c WITH (NOLOCK)
    JOIN dbo.Reservation a WITH (NOLOCK) ON a.ID = c.ReservationID
    JOIN #RoomType t ON t.ID = a.RoomTypeID
    JOIN dbo.ReservationType b ON b.ID = a.ReservationTypeID AND b.Deduct = 1
    WHERE c.RateDate >= @From AND c.RateDate < @End
      AND a.MainGuest = 1 AND a.ReservationNo > 0 AND a.[Status] NOT IN (3, 4, 7);

    -- Khách đến / đi: đọc bảng Reservation một lần
    SELECT a.ArrivalDate, a.DepartureDate, t.IsReal, a.NoOfRoom,
           a.NoOfAdult + a.NoOfChild + a.NoOfChild1 + a.NoOfChild2 AS Persons
    INTO #Resv
    FROM dbo.Reservation a WITH (NOLOCK)
    JOIN #RoomType t ON t.ID = a.RoomTypeID
    JOIN dbo.ReservationType b ON b.ID = a.ReservationTypeID AND b.Deduct = 1
    WHERE a.MainGuest = 1 AND a.ReservationNo > 0 AND a.[Status] NOT IN (3, 4, 7)
      AND ((a.ArrivalDate >= @From AND a.ArrivalDate < @End)
        OR (a.DepartureDate >= @From AND a.DepartureDate < @End));

    CREATE TABLE #Result (StatisticName nvarchar(50), [Index] int, [Date] datetime, Value decimal(18, 1));

    -- 1 Total Rooms Reserved, 8 Individual rooms, 9 Not Individual rooms
    INSERT INTO #Result
    SELECT N'Total Rooms Reserved', 1, RateDate, SUM(NoOfRoom) FROM #Rate WHERE IsReal = 1 GROUP BY RateDate;

    INSERT INTO #Result
    SELECT N'Individual rooms', 8, RateDate, SUM(NoOfRoom) FROM #Rate WHERE IsReal = 1 AND IsIndividual = 1 GROUP BY RateDate;

    INSERT INTO #Result
    SELECT N'Not Individual rooms', 9, RateDate, SUM(NoOfRoom) FROM #Rate WHERE IsReal = 1 AND IsNotIndividual = 1 GROUP BY RateDate;

    -- 4 Stayovers
    INSERT INTO #Result
    SELECT N'Stayovers', 4, RateDate, SUM(ISNULL(NoOfRoom, 0)) FROM #Rate WHERE IsReal = 1 AND [Status] IN (1, 2, 6) GROUP BY RateDate;

    -- 6 Adults, 7 Children
    INSERT INTO #Result
    SELECT N'Adults in house', 6, RateDate, SUM(Adults) FROM #Rate GROUP BY RateDate;

    INSERT INTO #Result
    SELECT N'Children in house', 7, RateDate, SUM(Children) FROM #Rate GROUP BY RateDate;

    -- 2 Occupancy %
    DECLARE @Rooms int =
        (SELECT COUNT(a.ID) FROM dbo.Room a WITH (NOLOCK)
         JOIN #RoomType t ON t.ID = a.RoomTypeID AND t.IsReal = 1);

    INSERT INTO #Result
    SELECT N'Occupancy %', 2, k.d,
           CAST(CAST(y.Reserved AS decimal(18, 1)) * 100 / NULLIF(CAST(@Rooms AS decimal(18, 1)), 0) AS decimal(18, 1))
    FROM #Days k
    LEFT JOIN (SELECT CAST(RateDate AS date) AS d, SUM(ISNULL(NoOfRoom, 0)) AS Reserved
               FROM #Rate WHERE IsReal = 1 GROUP BY CAST(RateDate AS date)) y
        ON y.d = CAST(k.d AS date);

    -- 3 Arrival Room, 11 Arrival Persons
    INSERT INTO #Result
    SELECT N'Arrival Room', 3, ArrivalDate, SUM(NoOfRoom)
    FROM #Resv WHERE ArrivalDate >= @From AND ArrivalDate < @End GROUP BY ArrivalDate;

    INSERT INTO #Result
    SELECT N'Arrival Persons', 11, ArrivalDate, SUM(Persons)
    FROM #Resv WHERE ArrivalDate >= @From AND ArrivalDate < @End GROUP BY ArrivalDate;

    -- 5 Departure Room, 12 Departure Persons
    INSERT INTO #Result
    SELECT N'Departure Room', 5, DepartureDate, SUM(ISNULL(NoOfRoom, 0))
    FROM #Resv WHERE DepartureDate >= @From AND DepartureDate < @End AND IsReal = 1 GROUP BY DepartureDate;

    INSERT INTO #Result
    SELECT N'Departure Persons', 12, DepartureDate, SUM(Persons)
    FROM #Resv WHERE DepartureDate >= @From AND DepartureDate < @End GROUP BY DepartureDate;

    -- 10 Out of Order
    INSERT INTO #Result
    SELECT N'Out of Order', 10, k.d, COUNT(DISTINCT o.RoomID)
    FROM #Days k
    LEFT JOIN (SELECT bb.RoomID, bb.FromDateOOO, bb.ToDateOOO
               FROM dbo.BusinessBlock bb
               JOIN dbo.Room r WITH (NOLOCK) ON r.ID = bb.RoomID
               JOIN #RoomType t ON t.ID = r.RoomTypeID
               WHERE bb.OOOStatus = 1 AND bb.RoomID > 0
                 AND DATEDIFF(DAY, bb.FromDateOOO, @ToDate) >= 0
                 AND DATEDIFF(DAY, bb.ToDateOOO, @FromDate) <= 0) o
        ON DATEDIFF(DAY, k.d, o.FromDateOOO) <= 0 AND DATEDIFF(DAY, k.d, o.ToDateOOO) > 0
    GROUP BY k.d;

    -- Xoay ngày thành cột
    DECLARE @cols nvarchar(max) = STUFF(
        (SELECT ',' + QUOTENAME(x.d)
         FROM (SELECT DISTINCT CONVERT(nvarchar(10), d, 111) AS d FROM #Days) x
         ORDER BY x.d FOR XML PATH('')), 1, 1, '');

    IF @cols IS NULL RETURN;

    DECLARE @sql nvarchar(max) = N'
        SELECT *
        FROM (SELECT StatisticName, [Index], [Date], Value FROM #Result) s
        PIVOT (SUM(Value) FOR [Date] IN (' + @cols + N')) p
        ORDER BY [Index];';

    EXEC sys.sp_executesql @sql;
END
GO

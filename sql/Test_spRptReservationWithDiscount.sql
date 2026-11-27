-- Test data: Reservation With Discount
-- SP: dbo.spRptReservationWithDiscount
-- dateType: 0 Arrival, 1 Stay, 2 Booking
-- sort:     0 Room No., 1 Conf. No., 2 Arrival, 3 Name
-- Chuỗi ID trống = All (SP không lọc IN). Status trống = không lọc status.

USE oneSPMSh_Tach_A1A2;
SET NOCOUNT ON;

-- 1) Lookup ID (copy vào biến bên dưới nếu muốn lọc)
SELECT TOP 20 ID, Code, Name FROM RoomClass WITH (NOLOCK) ORDER BY Code;
SELECT TOP 20 ID, Code, Name FROM RoomType WITH (NOLOCK) ORDER BY Code;
SELECT TOP 20 ID, RateCode FROM RateCode WITH (NOLOCK) ORDER BY RateCode;
SELECT TOP 20 ID, Code, Name FROM Market WITH (NOLOCK) ORDER BY Code;
SELECT TOP 20 ID, Code, Name FROM Source WITH (NOLOCK) ORDER BY Code;

DECLARE
    @fromDate  DATETIME     = '20251030',  -- khớp Business Date trên UI: 30/10/2025
    @toDate    DATETIME     = '20251030',
    @dateType  INT          = 0,           -- Arrival Dates
    @rmClasses VARCHAR(100) = '',          -- vd N'1,2'
    @rmTypes   VARCHAR(100) = '',
    @rateIDs   VARCHAR(100) = '',
    @mkIDs     VARCHAR(100) = '',
    @srcIDs    VARCHAR(100) = '',
    @statuses  VARCHAR(100) = '',          -- 0 Reserved .. 8 Day Use, trống = không lọc
    @sort      INT          = 0;

-- 2) All filters empty (giống bấm View mặc định trên web)
EXEC dbo.spRptReservationWithDiscount
    @fromDate  = @fromDate,
    @toDate    = @toDate,
    @dateType  = @dateType,
    @rmClasses = @rmClasses,
    @rmTypes   = @rmTypes,
    @rateIDs   = @rateIDs,
    @mkIDs     = @mkIDs,
    @srcIDs    = @srcIDs,
    @statuses  = @statuses,
    @sort      = @sort;

-- 3) Stay Dates / Booking Dates (đổi @dateType rồi exec lại)
-- EXEC dbo.spRptReservationWithDiscount @fromDate=@fromDate,@toDate=@toDate,@dateType=1,@rmClasses='',@rmTypes='',@rateIDs='',@mkIDs='',@srcIDs='',@statuses='',@sort=0;
-- EXEC dbo.spRptReservationWithDiscount @fromDate=@fromDate,@toDate=@toDate,@dateType=2,@rmClasses='',@rmTypes='',@rateIDs='',@mkIDs='',@srcIDs='',@statuses='',@sort=0;

-- 4) Có lọc: sửa ID cho khớp bước 1, status Reserved+Checked In (0,1)
-- EXEC dbo.spRptReservationWithDiscount
--     @fromDate='20251030', @toDate='20251030', @dateType=0,
--     @rmClasses='1', @rmTypes='', @rateIDs='', @mkIDs='', @srcIDs='',
--     @statuses='0,1', @sort=1;

-- 5) Số dòng / cột (kỳ vọng 15 cột SP, web ẩn ID còn 14)
SELECT COUNT(*) AS DiscountResvInRange
FROM Reservation r WITH (NOLOCK)
WHERE (r.DiscountRate <> 0 OR r.DiscountAmount <> 0)
  AND r.ArrivalDate >= @fromDate AND r.ArrivalDate < DATEADD(DAY, 1, @toDate);
GO

-- Web-only: same as spRptIncurringDepositSummary except @TransCode is a comma list.
-- Does not DROP or ALTER dbo.spRptIncurringDepositSummary.
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.spRptIncurringDepositSummaryWeb', N'P') IS NOT NULL
    DROP PROCEDURE dbo.spRptIncurringDepositSummaryWeb;
GO
CREATE PROCEDURE [dbo].[spRptIncurringDepositSummaryWeb]
    @TransCode VARCHAR(50) = '8520',
    @FromDate datetime = '2014/4/16',
    @ToDate datetime = '2014/4/16',
    @Type INT = 1,
    @Cashier VARCHAR(30) = '',
    @Zone VARCHAR(30) = ''
AS
BEGIN
    SET NOCOUNT ON;
    IF (@Type = 0)
    BEGIN
        SELECT
            r.LastName, r.RoomNo, fd.InvoiceNo, r.ArrivalDate, r.DepartureDate, r.NoOfNight,
            fd.CurrencyID, fd.Reference AS PaymentMethod, fd.TransactionDate,
            CASE WHEN (fd.Amount < 0) THEN (fd.Amount * -1) ELSE 0 END AS DepositAmount,
            CASE WHEN (fd.Amount > 0) THEN (fd.Amount) ELSE 0 END AS [ReturnAmount],
            r.ConfirmationNo, fd.UserName, fd.UserID, fd.ProfitCenterCode
        FROM dbo.FolioDetail fd WITH (NOLOCK)
        LEFT JOIN dbo.Folio f WITH (NOLOCK) ON fd.FolioID = f.ID
        LEFT JOIN dbo.Reservation r WITH (NOLOCK) ON f.ReservationID = r.ID
        LEFT JOIN dbo.RoomType rt WITH (NOLOCK) ON r.RoomTypeID = rt.ID
        WHERE fd.TransactionCode IN (SELECT LTRIM(RTRIM(ID)) FROM dbo.function_string_to_table(@TransCode, ','))
          AND fd.[Status] = 0
          AND DATEDIFF(DAY, fd.TransactionDate, @FromDate) <= 0
          AND DATEDIFF(DAY, fd.TransactionDate, @ToDate) >= 0
          AND (@Cashier = '' OR fd.UserID IN (SELECT ID FROM dbo.function_string_to_table(@Cashier, ',')))
          AND (@Zone = '' OR rt.ZoneID IN (SELECT ID FROM dbo.function_string_to_table(@Zone, ',')))
        ORDER BY [UserName], [TransactionDate]
    END
    ELSE
    BEGIN
        SELECT
            r.LastName, r.RoomNo, fd.InvoiceNo, r.ArrivalDate, r.DepartureDate, r.NoOfNight,
            fd.CurrencyID, fd.Reference AS PaymentMethod, fd.TransactionDate,
            CASE WHEN (fd.Amount < 0) THEN (fd.Amount * -1) ELSE 0 END AS DepositAmount,
            CASE WHEN (fd.Amount > 0) THEN (fd.Amount) ELSE 0 END AS [ReturnAmount],
            r.ConfirmationNo, fd.UserName, fd.UserID, fd.ProfitCenterCode
        FROM dbo.FolioDetail fd WITH (NOLOCK)
        LEFT JOIN dbo.Folio f WITH (NOLOCK) ON fd.FolioID = f.ID
        LEFT JOIN dbo.Reservation r WITH (NOLOCK) ON f.ReservationID = r.ID
        LEFT JOIN dbo.RoomType rt WITH (NOLOCK) ON r.RoomTypeID = rt.ID
        WHERE fd.TransactionCode IN (SELECT LTRIM(RTRIM(ID)) FROM dbo.function_string_to_table(@TransCode, ','))
          AND fd.[Status] = 0
          AND r.[Status] IN (1, 6)
          AND (@Cashier = '' OR fd.UserID IN (SELECT ID FROM dbo.function_string_to_table(@Cashier, ',')))
          AND (@Zone = '' OR rt.ZoneID IN (SELECT ID FROM dbo.function_string_to_table(@Zone, ',')))
        ORDER BY [UserName], [TransactionDate]
    END
END
GO

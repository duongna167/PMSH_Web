-- Web-only Collection: same result set as spRptIncurringDepositCollection,
-- date filter is sargable (no DATEDIFF on FolioDetail.TransactionDate).
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.spRptIncurringDepositCollectionWeb', N'P') IS NOT NULL
    DROP PROCEDURE dbo.spRptIncurringDepositCollectionWeb;
GO
CREATE PROCEDURE [dbo].[spRptIncurringDepositCollectionWeb]
    @TransCode VARCHAR(10) = '8520',
    @FromDate datetime = '2014/4/16',
    @ToDate datetime = '2014/4/16',
    @Cashier VARCHAR(30) = '',
    @Zone VARCHAR(30) = ''
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @FromDt date = CAST(@FromDate AS date);
    DECLARE @ToExcl datetime = DATEADD(DAY, 1, CAST(@ToDate AS date));

    SELECT
        r.LastName,
        r.RoomNo,
        fd.InvoiceNo,
        r.ArrivalDate,
        r.DepartureDate,
        r.NoOfNight,
        fd.CurrencyID,
        fd.Reference AS PaymentMethod,
        fd.TransactionDate,
        fd.Amount * -1 AS DepositAmount,
        0.00 AS ReturnAmount,
        r.ConfirmationNo,
        fd.UserName,
        fd.UserID,
        fd.ProfitCenterCode
    FROM dbo.FolioDetail fd WITH (NOLOCK)
    LEFT JOIN dbo.Folio f WITH (NOLOCK) ON fd.FolioID = f.ID
    LEFT JOIN dbo.Reservation r WITH (NOLOCK) ON f.ReservationID = r.ID
    LEFT JOIN dbo.RoomType rt WITH (NOLOCK) ON r.RoomTypeID = rt.ID
    WHERE fd.TransactionCode = @TransCode
      AND fd.[Status] = 0
      AND fd.Amount < 0
      AND fd.TransactionDate >= @FromDt
      AND fd.TransactionDate < @ToExcl
      AND (@Cashier = '' OR fd.UserID IN (SELECT ID FROM dbo.function_string_to_table(@Cashier, ',')))
      AND (@Zone = '' OR rt.ZoneID IN (SELECT ID FROM dbo.function_string_to_table(@Zone, ',')))
END
GO

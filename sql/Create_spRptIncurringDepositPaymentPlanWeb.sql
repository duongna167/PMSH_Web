-- Web-only: same as spRptIncurringDepositPaymentPlan except @TransCode is a comma list.
-- Does not DROP or ALTER dbo.spRptIncurringDepositPaymentPlan.
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.spRptIncurringDepositPaymentPlanWeb', N'P') IS NOT NULL
    DROP PROCEDURE dbo.spRptIncurringDepositPaymentPlanWeb;
GO
CREATE PROCEDURE [dbo].[spRptIncurringDepositPaymentPlanWeb]
    @TransCode VARCHAR(50) = '8520',
    @FromDate datetime = '2014/4/16',
    @ToDate datetime = '2014/5/16',
    @Cashier VARCHAR(30) = '',
    @Zone VARCHAR(30) = '',
    @NotBalance VARCHAR(3) = ''
AS
BEGIN
    SET NOCOUNT ON;
    SELECT dep.*,
           ISNULL(dep.Deposit, 0) - ISNULL(dep.[Return], 0) AS [Balance]
    FROM (
        SELECT
            r.LastName,
            r.RoomNo,
            r.ConfirmationNo,
            fd.FolioID AS FolioNo,
            r.ArrivalDate,
            r.DepartureDate,
            r.NoOfNight,
            fd.CurrencyID,
            SUM(CASE WHEN (fd.Amount < 0) THEN (fd.Amount * -1) ELSE 0 END) AS Deposit,
            SUM(CASE WHEN (fd.Amount > 0) THEN (fd.Amount) ELSE 0 END) AS [Return]
        FROM dbo.FolioDetail fd WITH (NOLOCK)
        LEFT JOIN dbo.Folio f WITH (NOLOCK) ON fd.FolioID = f.ID
        LEFT JOIN dbo.Reservation r WITH (NOLOCK) ON f.ReservationID = r.ID
        LEFT JOIN dbo.RoomType rt WITH (NOLOCK) ON r.RoomTypeID = rt.ID
        WHERE fd.TransactionCode IN (SELECT LTRIM(RTRIM(ID)) FROM dbo.function_string_to_table(@TransCode, ','))
          AND fd.[Status] = 0
          AND DATEDIFF(DAY, r.DepartureDate, @FromDate) <= 0
          AND DATEDIFF(DAY, r.DepartureDate, @ToDate) >= 0
          AND (@Cashier = '' OR fd.UserID IN (SELECT ID FROM dbo.function_string_to_table(@Cashier, ',')))
          AND (@Zone = '' OR rt.ZoneID IN (SELECT ID FROM dbo.function_string_to_table(@Zone, ',')))
        GROUP BY r.LastName, r.RoomNo, r.ConfirmationNo, fd.FolioID, r.ArrivalDate, r.DepartureDate, r.NoOfNight, fd.CurrencyID
    ) AS dep
    WHERE (@NotBalance = '' OR (ISNULL(dep.Deposit, 0) - ISNULL(dep.[Return], 0)) <> 0)
    ORDER BY dep.RoomNo, dep.FolioNo
END
GO

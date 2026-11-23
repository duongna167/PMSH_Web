-- WinForm ucAR_Adjustment calls spRpt_ARAdjustment (@iFromDate, @iToDate).
-- Missing on some hotel DBs; recreate if absent.
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.spRpt_ARAdjustment', N'P') IS NULL
BEGIN
    EXEC(N'
    CREATE PROCEDURE [dbo].[spRpt_ARAdjustment]
        @iFromDate DATETIME,
        @iToDate DATETIME
    AS
    BEGIN
        SET NOCOUNT ON;
        SELECT
            a.AccountNo,
            a.AccountName,
            CONVERT(varchar(10), t.TransactionDate, 103) AS TransactionDate,
            t.TransactionCode,
            t.Description,
            t.Amount,
            t.CurrencyID,
            t.Reference,
            t.Supplement,
            t.CreatedBy AS [User]
        FROM ARAccountReceivableTrans t
        INNER JOIN ARAccountReceivable a ON a.ID = t.AccountReceivableID
        WHERE DATEDIFF(DAY, @iFromDate, t.TransactionDate) >= 0
          AND DATEDIFF(DAY, t.TransactionDate, @iToDate) >= 0
          AND (
                ISNULL(t.IsAdjusted, 0) = 1
                OR t.TransactionCode IN (
                    SELECT DISTINCT AdjustmentCode
                    FROM Transactions
                    WHERE AdjustmentCode IS NOT NULL
                      AND LTRIM(RTRIM(AdjustmentCode)) <> ''''
                )
          )
        ORDER BY t.TransactionDate, a.AccountNo;
    END
    ');
END
GO

-- WinForm Aging Summary By Type columns:
-- AccountType Desc, Acct. Type, Curr., Level1-5 Cap., Total
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.spRptARAgingSummaryByAccType', N'P') IS NOT NULL
    DROP PROCEDURE dbo.spRptARAgingSummaryByAccType;
GO

CREATE PROCEDURE [dbo].[spRptARAgingSummaryByAccType]
    @date DATETIME,
    @sort VARCHAR(50) = 'AccountType Desc.',
    @level1Max INT = 30,
    @level2Max INT = 60,
    @level3Max INT = 90,
    @level4Max INT = 120,
    @level5Max INT = -1
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH rawAmt AS (
        SELECT
            art.AccountReceivableID AS ARID,
            art.CurrencyID,
            CASE WHEN DATEDIFF(DAY, art.TransactionDate, @date) >= 0 AND DATEDIFF(DAY, art.TransactionDate, @date) <= @level1Max THEN art.Amount ELSE 0 END AS Level1Amount,
            CASE WHEN DATEDIFF(DAY, art.TransactionDate, @date) > @level1Max AND DATEDIFF(DAY, art.TransactionDate, @date) <= @level2Max THEN art.Amount ELSE 0 END AS Level2Amount,
            CASE WHEN DATEDIFF(DAY, art.TransactionDate, @date) > @level2Max AND DATEDIFF(DAY, art.TransactionDate, @date) <= @level3Max THEN art.Amount ELSE 0 END AS Level3Amount,
            CASE WHEN DATEDIFF(DAY, art.TransactionDate, @date) > @level3Max AND DATEDIFF(DAY, art.TransactionDate, @date) <= @level4Max THEN art.Amount ELSE 0 END AS Level4Amount,
            CASE WHEN DATEDIFF(DAY, art.TransactionDate, @date) > @level4Max
                  AND (@level5Max <= @level4Max OR DATEDIFF(DAY, art.TransactionDate, @date) <= @level5Max)
                 THEN art.Amount ELSE 0 END AS Level5Amount
        FROM dbo.ARAccountReceivableTrans art
        INNER JOIN dbo.ARAccountReceivable ar ON art.AccountReceivableID = ar.ID
        WHERE art.Balance <> 0
        UNION ALL
        SELECT
            p.AccountReceivableID,
            p.CurrencyID,
            CASE WHEN DATEDIFF(DAY, p.TransactionDate, @date) >= 0 AND DATEDIFF(DAY, t.TransactionDate, @date) <= @level1Max THEN p.Amount ELSE 0 END,
            CASE WHEN DATEDIFF(DAY, t.TransactionDate, @date) > @level1Max AND DATEDIFF(DAY, t.TransactionDate, @date) <= @level2Max THEN p.Amount ELSE 0 END,
            CASE WHEN DATEDIFF(DAY, t.TransactionDate, @date) > @level2Max AND DATEDIFF(DAY, t.TransactionDate, @date) <= @level3Max THEN p.Amount ELSE 0 END,
            CASE WHEN DATEDIFF(DAY, t.TransactionDate, @date) > @level3Max AND DATEDIFF(DAY, t.TransactionDate, @date) <= @level4Max THEN p.Amount ELSE 0 END,
            CASE WHEN DATEDIFF(DAY, t.TransactionDate, @date) > @level4Max
                  AND (@level5Max <= @level4Max OR DATEDIFF(DAY, p.TransactionDate, t.TransactionDate) <= @level5Max)
                 THEN p.Amount ELSE 0 END
        FROM dbo.ARPayment p
        INNER JOIN dbo.ARAccountReceivable ar ON p.AccountReceivableID = ar.ID
        LEFT JOIN dbo.ARAccountReceivableTrans t ON t.ID = p.AccountReceivableTransID
        WHERE t.Balance <> 0
    )
    SELECT
        ISNULL(typ.Description, N'') AS [AccountType Desc],
        ISNULL(typ.[Type], N'') AS [Acct. Type],
        r.CurrencyID AS [Curr.],
        SUM(r.Level1Amount) AS [Level1 Cap.],
        SUM(r.Level2Amount) AS [Level2 Cap.],
        SUM(r.Level3Amount) AS [Level3 Cap.],
        SUM(r.Level4Amount) AS [Level4 Cap.],
        SUM(r.Level5Amount) AS [Level5 Cap.],
        SUM(r.Level1Amount + r.Level2Amount + r.Level3Amount + r.Level4Amount + r.Level5Amount) AS Total
    FROM rawAmt r
    INNER JOIN dbo.ARAccountReceivable ar ON r.ARID = ar.ID
    LEFT JOIN dbo.ARAccountType typ ON ar.AccountTypeID = typ.ID
    GROUP BY ISNULL(typ.Description, N''), ISNULL(typ.[Type], N''), r.CurrencyID
    ORDER BY
        CASE WHEN @sort LIKE N'Acct.%' THEN ISNULL(typ.[Type], N'') END,
        ISNULL(typ.Description, N''),
        ISNULL(typ.[Type], N''),
        r.CurrencyID;
END
GO

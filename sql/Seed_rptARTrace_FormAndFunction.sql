-- AR Trace report (End Of Day). New FormAndFunction.Code = rptARTrace (not cash_AR_Trace).
-- Group 012_5 Report Management. Grants Admin + groups/users that already have rptNoShowReport.
SET NOCOUNT ON;

DECLARE @rptId INT = (SELECT TOP 1 ID FROM FormAndFunctionGroup WITH (NOLOCK) WHERE Code = N'012_5');
IF @rptId IS NULL
BEGIN
    RAISERROR(N'FormAndFunctionGroup 012_5 not found.', 16, 1);
    RETURN;
END;

IF NOT EXISTS (SELECT 1 FROM FormAndFunction WITH (NOLOCK) WHERE Code = N'rptARTrace')
BEGIN
    INSERT INTO FormAndFunction
        (Code, Name, Description, ShiftKey, CtrlKey, AltKey, ShortcutKey, FormAndFunctionGroupID,
         CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, IsHide, OrderIndex, Inactive, MappingLinkWeb)
    VALUES
        (N'rptARTrace', N'AR Trace', N'AR Trace', 0, 0, 0, N'', @rptId,
         N'seed', GETDATE(), N'seed', GETDATE(), 0, 0, 0, N'');
END
ELSE
BEGIN
    UPDATE FormAndFunction
    SET Name = N'AR Trace',
        Description = N'AR Trace',
        FormAndFunctionGroupID = @rptId,
        IsHide = 0,
        Inactive = 0,
        UpdatedBy = N'seed',
        UpdatedDate = GETDATE()
    WHERE Code = N'rptARTrace';
END;

DECLARE @fnId INT = (SELECT TOP 1 ID FROM FormAndFunction WITH (NOLOCK) WHERE Code = N'rptARTrace');
DECLARE @now DATETIME = GETDATE();

DECLARE @groups TABLE (UserGroupID INT PRIMARY KEY);
INSERT INTO @groups (UserGroupID)
SELECT ID FROM UserGroup WITH (NOLOCK) WHERE Code = N'Admin' OR Name = N'Administrator'
UNION
SELECT DISTINCT d.UserGroupID
FROM UserGroupRightDistribution d WITH (NOLOCK)
INNER JOIN FormAndFunction f WITH (NOLOCK) ON f.ID = d.FormAndFunctionID
WHERE f.Code = N'rptNoShowReport' AND ISNULL(d.FormAndFunctionID, 0) > 0;

INSERT INTO UserGroupRightDistribution
    (FormAndFunctionID, UserGroupID, ViewRight, CreateRight, ModifyRight, DeleteRight, SpecialRight,
     CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, FormAndFunctionDataID)
SELECT @fnId, g.UserGroupID, 1, 1, 1, 1, 1, N'seed', @now, N'seed', @now, 0
FROM @groups g
WHERE NOT EXISTS (
    SELECT 1 FROM UserGroupRightDistribution d WITH (NOLOCK)
    WHERE d.UserGroupID = g.UserGroupID AND d.FormAndFunctionID = @fnId
);

-- User-level only when group does not already have the right (XOR login would drop a duplicate).
INSERT INTO UserRightDistribution
    (FormAndFunctionID, UserID, ViewRight, CreateRight, ModifyRight, DeleteRight, SpecialRight,
     CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, FormAndFunctionDataID)
SELECT @fnId, d.UserID, 1, 1, 1, 1, 1, N'seed', @now, N'seed', @now, 0
FROM UserRightDistribution d WITH (NOLOCK)
INNER JOIN FormAndFunction f WITH (NOLOCK) ON f.ID = d.FormAndFunctionID
INNER JOIN Users u WITH (NOLOCK) ON u.ID = d.UserID
WHERE f.Code = N'rptNoShowReport'
  AND ISNULL(d.FormAndFunctionID, 0) > 0
  AND NOT EXISTS (
      SELECT 1 FROM @groups g WHERE g.UserGroupID = u.UserGroupID
  )
  AND NOT EXISTS (
      SELECT 1 FROM UserRightDistribution x WITH (NOLOCK)
      WHERE x.UserID = d.UserID AND x.FormAndFunctionID = @fnId
  );

SELECT f.ID, f.Code, f.Name, f.FormAndFunctionGroupID
FROM FormAndFunction f
WHERE f.Code = N'rptARTrace';

SELECT COUNT(*) AS GroupGrants
FROM UserGroupRightDistribution WHERE FormAndFunctionID = @fnId;
SELECT COUNT(*) AS UserGrants
FROM UserRightDistribution WHERE FormAndFunctionID = @fnId;
GO

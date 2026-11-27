-- Reservation With Discount. New FormAndFunction.Code = rptReservationWithDiscount.
SET NOCOUNT ON;

DECLARE @rptId INT = (SELECT TOP 1 ID FROM FormAndFunctionGroup WITH (NOLOCK) WHERE Code = N'012_5');
IF @rptId IS NULL
BEGIN
    RAISERROR(N'FormAndFunctionGroup 012_5 not found.', 16, 1);
    RETURN;
END;

IF NOT EXISTS (SELECT 1 FROM FormAndFunction WITH (NOLOCK) WHERE Code = N'rptReservationWithDiscount')
BEGIN
    INSERT INTO FormAndFunction
        (Code, Name, Description, ShiftKey, CtrlKey, AltKey, ShortcutKey, FormAndFunctionGroupID,
         CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, IsHide, OrderIndex, Inactive, MappingLinkWeb)
    VALUES
        (N'rptReservationWithDiscount', N'Reservation With Discount', N'Reservation With Discount', 0, 0, 0, N'', @rptId,
         N'seed', GETDATE(), N'seed', GETDATE(), 0, 0, 0, N'');
END
ELSE
BEGIN
    UPDATE FormAndFunction
    SET Name = N'Reservation With Discount',
        Description = N'Reservation With Discount',
        FormAndFunctionGroupID = @rptId,
        IsHide = 0,
        Inactive = 0,
        UpdatedBy = N'seed',
        UpdatedDate = GETDATE()
    WHERE Code = N'rptReservationWithDiscount';
END;

DECLARE @fnId INT = (SELECT TOP 1 ID FROM FormAndFunction WITH (NOLOCK) WHERE Code = N'rptReservationWithDiscount');
DECLARE @now DATETIME = GETDATE();

DECLARE @groups TABLE (UserGroupID INT PRIMARY KEY);
INSERT INTO @groups (UserGroupID)
SELECT ID FROM UserGroup WITH (NOLOCK) WHERE Code = N'Admin' OR Name = N'Administrator'
UNION
SELECT DISTINCT d.UserGroupID
FROM UserGroupRightDistribution d WITH (NOLOCK)
INNER JOIN FormAndFunction f WITH (NOLOCK) ON f.ID = d.FormAndFunctionID
WHERE f.Code = N'rptReservationRateCheck' AND ISNULL(d.FormAndFunctionID, 0) > 0;

INSERT INTO UserGroupRightDistribution
    (FormAndFunctionID, UserGroupID, ViewRight, CreateRight, ModifyRight, DeleteRight, SpecialRight,
     CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, FormAndFunctionDataID)
SELECT @fnId, g.UserGroupID, 1, 1, 1, 1, 1, N'seed', @now, N'seed', @now, 0
FROM @groups g
WHERE NOT EXISTS (
    SELECT 1 FROM UserGroupRightDistribution d WITH (NOLOCK)
    WHERE d.UserGroupID = g.UserGroupID AND d.FormAndFunctionID = @fnId
);

INSERT INTO UserRightDistribution
    (FormAndFunctionID, UserID, ViewRight, CreateRight, ModifyRight, DeleteRight, SpecialRight,
     CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, FormAndFunctionDataID)
SELECT @fnId, d.UserID, 1, 1, 1, 1, 1, N'seed', @now, N'seed', @now, 0
FROM UserRightDistribution d WITH (NOLOCK)
INNER JOIN FormAndFunction f WITH (NOLOCK) ON f.ID = d.FormAndFunctionID
INNER JOIN Users u WITH (NOLOCK) ON u.ID = d.UserID
WHERE f.Code = N'rptReservationRateCheck'
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
WHERE f.Code = N'rptReservationWithDiscount';
GO

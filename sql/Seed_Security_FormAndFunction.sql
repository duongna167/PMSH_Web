-- SECURITY menu catalog (hotel DB).
-- Reuses WinForm User Management codes (011_Usr / usr_* / Sec_UserGroup_View).
-- Web-only Users Management keeps UsersManagement_web.

SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM FormAndFunctionGroup WITH (NOLOCK) WHERE Code = N'011_Usr')
BEGIN
    INSERT INTO FormAndFunctionGroup (Code, Name, Description, ParentID, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, IsHide, Inactive)
    VALUES (N'011_Usr', N'Security', N'SECURITY', 0, N'seed', GETDATE(), N'seed', GETDATE(), 0, 0);
END
ELSE
BEGIN
    UPDATE FormAndFunctionGroup
    SET Name = N'Security',
        Description = N'SECURITY',
        UpdatedBy = N'seed',
        UpdatedDate = GETDATE()
    WHERE Code = N'011_Usr';
END;

DECLARE @secGroupId INT = (SELECT TOP 1 ID FROM FormAndFunctionGroup WITH (NOLOCK) WHERE Code = N'011_Usr');

IF NOT EXISTS (SELECT 1 FROM FormAndFunctionGroup WITH (NOLOCK) WHERE Code = N'011_1' AND ParentID = @secGroupId)
BEGIN
    IF EXISTS (SELECT 1 FROM FormAndFunctionGroup WITH (NOLOCK) WHERE Code = N'011_1')
        UPDATE FormAndFunctionGroup
        SET Name = N'Security Config',
            Description = N'Security Config',
            ParentID = @secGroupId,
            UpdatedBy = N'seed',
            UpdatedDate = GETDATE()
        WHERE Code = N'011_1';
    ELSE
        INSERT INTO FormAndFunctionGroup (Code, Name, Description, ParentID, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, IsHide, Inactive)
        VALUES (N'011_1', N'Security Config', N'Security Config', @secGroupId, N'seed', GETDATE(), N'seed', GETDATE(), 0, 0);
END
ELSE
BEGIN
    UPDATE FormAndFunctionGroup
    SET Name = N'Security Config',
        Description = N'Security Config',
        UpdatedBy = N'seed',
        UpdatedDate = GETDATE()
    WHERE Code = N'011_1';
END;

DECLARE @cfgId INT = (SELECT TOP 1 ID FROM FormAndFunctionGroup WITH (NOLOCK) WHERE Code = N'011_1');

DECLARE @fn TABLE (Code NVARCHAR(200), Name NVARCHAR(200), Description NVARCHAR(200));
INSERT INTO @fn (Code, Name, Description) VALUES
    (N'Sec_UserGroup_View', N'User of group', N'User of group'),
    (N'usr_Functions', N'Add Functions To List', N'Add Functions To List'),
    (N'UsersManagement_web', N'Users Management', N'Users Management'),
    (N'usr_UserRight', N'User mapping', N'User mapping'),
    (N'usr_GroupUserRight', N'User mapping by group', N'User mapping by group');

MERGE FormAndFunction AS t
USING @fn AS s
ON t.Code = s.Code
WHEN MATCHED THEN
    UPDATE SET
        t.Name = s.Name,
        t.Description = s.Description,
        t.FormAndFunctionGroupID = @cfgId,
        t.IsHide = 0,
        t.Inactive = 0,
        t.UpdatedBy = N'seed',
        t.UpdatedDate = GETDATE()
WHEN NOT MATCHED THEN
    INSERT (Code, Name, Description, ShiftKey, CtrlKey, AltKey, ShortcutKey, FormAndFunctionGroupID,
            CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, IsHide, OrderIndex, Inactive, MappingLinkWeb)
    VALUES (s.Code, s.Name, s.Description, 0, 0, 0, N'', @cfgId,
            N'seed', GETDATE(), N'seed', GETDATE(), 0, 0, 0, N'');

DECLARE @adminId INT = (SELECT TOP 1 ID FROM UserGroup WITH (NOLOCK) WHERE Code = N'Admin' OR Name = N'Administrator');

IF @adminId IS NOT NULL
BEGIN
    INSERT INTO UserGroupRightDistribution
        (FormAndFunctionID, UserGroupID, ViewRight, CreateRight, ModifyRight, DeleteRight, SpecialRight,
         CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, FormAndFunctionDataID)
    SELECT f.ID, @adminId, 1, 1, 1, 1, 1, N'seed', GETDATE(), N'seed', GETDATE(), 0
    FROM FormAndFunction f
    INNER JOIN @fn s ON s.Code = f.Code
    WHERE NOT EXISTS (
        SELECT 1 FROM UserGroupRightDistribution d
        WHERE d.UserGroupID = @adminId AND d.FormAndFunctionID = f.ID
    );
END;

SELECT g.ID, g.Code, g.Name, g.ParentID
FROM FormAndFunctionGroup g
WHERE g.Code IN (N'011_Usr', N'011_1');

SELECT f.ID, f.Code, f.Name, f.FormAndFunctionGroupID
FROM FormAndFunction f
INNER JOIN @fn s ON s.Code = f.Code;
GO

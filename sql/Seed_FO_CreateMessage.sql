SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.FOInternalMessage', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FOInternalMessage (
        ID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Message NVARCHAR(MAX) NOT NULL,
        FromDate DATE NOT NULL,
        ToDate DATE NOT NULL,
        IsDelete BIT NOT NULL CONSTRAINT DF_FOInternalMessage_IsDelete DEFAULT (0),
        UserInsertID INT NULL,
        CreateDate DATETIME NULL,
        UserUpdateID INT NULL,
        UpdateDate DATETIME NULL
    );
END;

IF COL_LENGTH(N'dbo.FOInternalMessage', N'ToDepartmentID') IS NULL
    ALTER TABLE dbo.FOInternalMessage ADD ToDepartmentID INT NOT NULL CONSTRAINT DF_FOInternalMessage_ToDept DEFAULT (0);

IF OBJECT_ID(N'dbo.FOInternalMessageReceived', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FOInternalMessageReceived (
        ID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        MessageID INT NOT NULL,
        UserID INT NOT NULL,
        ReceivedDate DATETIME NOT NULL,
        CONSTRAINT UQ_FOInternalMessageReceived UNIQUE (MessageID, UserID)
    );
END;

DECLARE @foId INT = (SELECT TOP 1 ID FROM FormAndFunctionGroup WITH (NOLOCK) WHERE Code = N'004_FO&Csh');
IF @foId IS NULL
    SET @foId = (SELECT TOP 1 ID FROM FormAndFunctionGroup WITH (NOLOCK) WHERE Code = N'004_3');

DECLARE @fn TABLE (Code NVARCHAR(200), Name NVARCHAR(200), Description NVARCHAR(200), GroupId INT);
INSERT INTO @fn (Code, Name, Description, GroupId) VALUES
    (N'fo_CreateMessage_web', N'Create Message', N'Create Message', @foId);

MERGE FormAndFunction AS t
USING @fn AS s
ON t.Code = s.Code
WHEN MATCHED THEN
    UPDATE SET
        t.Name = s.Name,
        t.Description = s.Description,
        t.FormAndFunctionGroupID = ISNULL(s.GroupId, t.FormAndFunctionGroupID),
        t.IsHide = 0,
        t.Inactive = 0,
        t.UpdatedBy = N'seed',
        t.UpdatedDate = GETDATE()
WHEN NOT MATCHED THEN
    INSERT (Code, Name, Description, ShiftKey, CtrlKey, AltKey, ShortcutKey, FormAndFunctionGroupID,
            CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, IsHide, OrderIndex, Inactive, MappingLinkWeb)
    VALUES (s.Code, s.Name, s.Description, 0, 0, 0, N'', s.GroupId,
            N'seed', GETDATE(), N'seed', GETDATE(), 0, 0, 0, N'');

INSERT INTO UserGroupRightDistribution
    (FormAndFunctionID, UserGroupID, ViewRight, CreateRight, ModifyRight, DeleteRight, SpecialRight,
     CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, FormAndFunctionDataID)
SELECT f.ID, g.ID, 1, 1, 1, 1, 1, N'seed', GETDATE(), N'seed', GETDATE(), 0
FROM FormAndFunction f
INNER JOIN @fn s ON s.Code = f.Code
INNER JOIN UserGroup g WITH (NOLOCK) ON g.Code IN (N'Admin', N'FOM') OR g.Name IN (N'Administrator', N'FOM', N'Admin')
WHERE NOT EXISTS (
    SELECT 1 FROM UserGroupRightDistribution d
    WHERE d.UserGroupID = g.ID AND d.FormAndFunctionID = f.ID
);

SELECT f.ID, f.Code, f.Name, f.FormAndFunctionGroupID
FROM FormAndFunction f
INNER JOIN @fn s ON s.Code = f.Code;
GO

-- PMSH hotel DB: login rights by FormAndFunction.Code (WinForm Tag).
-- Group XOR user exception (UserRightDistribution row that also exists on the group = revoke).
-- Run on the hotel database (not CMS BookingEngine).

IF OBJECT_ID(N'dbo.GetPermissionNamesByUserGroup', N'P') IS NOT NULL
    DROP PROCEDURE dbo.GetPermissionNamesByUserGroup;
GO

CREATE PROCEDURE [dbo].[GetPermissionNamesByUserGroup]
    @UserGroupID INT,
    @UserID INT
AS
BEGIN
    SET NOCOUNT ON;

    IF @UserGroupID IS NULL OR @UserGroupID = 0
        SELECT @UserGroupID = UserGroupID FROM dbo.Users WITH (NOLOCK) WHERE ID = @UserID;

    ;WITH GroupIds AS (
        SELECT FormAndFunctionID AS Id
        FROM dbo.UserGroupRightDistribution WITH (NOLOCK)
        WHERE UserGroupID = @UserGroupID AND ISNULL(FormAndFunctionID, 0) > 0
    ),
    UserIds AS (
        SELECT FormAndFunctionID AS Id
        FROM dbo.UserRightDistribution WITH (NOLOCK)
        WHERE UserID = @UserID AND ISNULL(FormAndFunctionID, 0) > 0
    ),
    Effective AS (
        SELECT Id FROM GroupIds
        UNION
        SELECT Id FROM UserIds
        EXCEPT
        SELECT g.Id FROM GroupIds g INNER JOIN UserIds u ON g.Id = u.Id
    )
    SELECT
        LTRIM(RTRIM(ff.Code)) AS Code,
        LTRIM(RTRIM(ff.Code)) AS Name,
        LTRIM(RTRIM(ff.Name)) AS DisplayName
    FROM Effective e
    INNER JOIN dbo.FormAndFunction ff WITH (NOLOCK) ON ff.ID = e.Id
    WHERE ISNULL(ff.IsHide, 0) = 0
      AND ISNULL(ff.Inactive, 0) = 0
      AND LTRIM(RTRIM(ISNULL(ff.Code, N''))) <> N'';
END
GO

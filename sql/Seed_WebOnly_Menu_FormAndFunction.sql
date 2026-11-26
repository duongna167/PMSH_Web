-- Web-only menu codes (no WinForm FormAndFunction.Code).
SET NOCOUNT ON;

DECLARE @hkpId INT = (SELECT TOP 1 ID FROM FormAndFunctionGroup WITH (NOLOCK) WHERE Code = N'007_1');
DECLARE @rptId INT = (SELECT TOP 1 ID FROM FormAndFunctionGroup WITH (NOLOCK) WHERE Code = N'012_5');
DECLARE @admId INT = (SELECT TOP 1 ID FROM FormAndFunctionGroup WITH (NOLOCK) WHERE Code = N'009_1');

DECLARE @fn TABLE (Code NVARCHAR(200), Name NVARCHAR(200), Description NVARCHAR(200), GroupId INT);
INSERT INTO @fn (Code, Name, Description, GroupId) VALUES
    (N'hkp_TurndownManagement_web', N'Turndown Management', N'Turndown Management', @hkpId),
    (N'hkp_HKPStatus_web', N'HKP Status', N'HKP Status', @hkpId),
    (N'rpt_ReportByChart_web', N'Report By Chart', N'Report By Chart', @rptId),
    (N'rpt_Spr_FFPAndotherCards', N'Frequent Flyer Programs and other Cards', N'Frequent Flyer Programs and other Cards', @rptId),
    (N'rptARTrace', N'AR Trace', N'AR Trace', @rptId),
    (N'rptReservationWithDiscount', N'Reservation With Discount', N'Reservation With Discount', @rptId),
    (N'admin_SetupEmail_web', N'Setup Email', N'Setup Email', @admId),
    (N'admin_SendEmail_web', N'Send Email', N'Send Email', @admId),
    (N'admin_SendSMS_web', N'Send SMS', N'Send SMS', @admId),
    (N'admin_SendEmailBirthday_web', N'Send Email Birthday', N'Send Email Birthday', @admId),
    (N'admin_EmailSendingHistory_web', N'Email Sending History', N'Email Sending History', @admId);

MERGE FormAndFunction AS t
USING @fn AS s
ON t.Code = s.Code
WHEN MATCHED THEN
    UPDATE SET
        t.Name = s.Name,
        t.Description = s.Description,
        t.FormAndFunctionGroupID = s.GroupId,
        t.IsHide = 0,
        t.Inactive = 0,
        t.UpdatedBy = N'seed',
        t.UpdatedDate = GETDATE()
WHEN NOT MATCHED THEN
    INSERT (Code, Name, Description, ShiftKey, CtrlKey, AltKey, ShortcutKey, FormAndFunctionGroupID,
            CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, IsHide, OrderIndex, Inactive, MappingLinkWeb)
    VALUES (s.Code, s.Name, s.Description, 0, 0, 0, N'', s.GroupId,
            N'seed', GETDATE(), N'seed', GETDATE(), 0, 0, 0, N'');

DECLARE @adminGroupId INT = (SELECT TOP 1 ID FROM UserGroup WITH (NOLOCK) WHERE Code = N'Admin' OR Name = N'Administrator');

IF @adminGroupId IS NOT NULL
BEGIN
    INSERT INTO UserGroupRightDistribution
        (FormAndFunctionID, UserGroupID, ViewRight, CreateRight, ModifyRight, DeleteRight, SpecialRight,
         CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, FormAndFunctionDataID)
    SELECT f.ID, @adminGroupId, 1, 1, 1, 1, 1, N'seed', GETDATE(), N'seed', GETDATE(), 0
    FROM FormAndFunction f
    INNER JOIN @fn s ON s.Code = f.Code
    WHERE NOT EXISTS (
        SELECT 1 FROM UserGroupRightDistribution d
        WHERE d.UserGroupID = @adminGroupId AND d.FormAndFunctionID = f.ID
    );
END;

SELECT f.ID, f.Code, f.Name, f.FormAndFunctionGroupID
FROM FormAndFunction f
INNER JOIN @fn s ON s.Code = f.Code;
GO

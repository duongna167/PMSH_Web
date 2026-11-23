SET NOCOUNT ON;
GO

ALTER PROCEDURE [dbo].[spSearchUsers]
	@LastName nvarchar(150),
	@FirstName nvarchar(150),
	@LoginName nvarchar(50),
	@JobTitle nvarchar(50),
	@Department nvarchar(150),
	@Cashier int,
	@Status int
AS
BEGIN
	DECLARE @Command nvarchar(max)
	SET @Command = N'select a.Status, Case when a.Status=-1 then ''Terminated'' when a.Status=0 then ''Inactive'' else ''Active'' end [StatusText],' +
		N'a.ID, a.FirstName+'' ''+a.MiddleName+'' ''+a.LastName [FullName],' +
		N'a.LoginName, f.Name [JobTitle], Case When(a.Sex=1) then ''Male'' else ''Female'' end [Sex], a.BirthOfDate,' +
		N'a.Telephone, a.HandPhone, a.HomeAddress, a.Resident, a.PostalCode, a.IsCashier, b.Name [Department],' +
		N'a.CreateDate, ISNULL(c.LoginName, N'''') [CreateBy], a.UpdateDate, ISNULL(d.LoginName, N'''') [UpdateBy] ' +
		N'from Users a with (nolock) ' +
		N'inner join UserGroup b with (nolock) on a.UserGroupID = b.ID ' +
		N'left join Users c with (nolock) on a.UserInsertID = c.ID ' +
		N'left join Users d with (nolock) on a.UserUpdateID = d.ID ' +
		N'left join JobTitle f with (nolock) on a.JobTitleID = f.ID ' +
		N'Where 1=1 '

	IF (@LastName != N'')
		SET @Command = @Command + N' and a.LastName like N''%' + @LastName + N'%'''
	IF (@FirstName != N'')
		SET @Command = @Command + N' and a.FirstName like N''%' + @FirstName + N'%'''
	IF (@LoginName != N'')
		SET @Command = @Command + N' and a.LoginName like N''%' + @LoginName + N'%'''
	IF (@JobTitle != N'')
		SET @Command = @Command + N' and f.Name like N''%' + @JobTitle + N'%'''
	IF (@Department != N'')
		SET @Command = @Command + N' and b.Name like N''%' + @Department + N'%'''

	IF (@Cashier = 0)
		SET @Command = @Command + N' and a.IsCashier = 1'
	ELSE IF (@Cashier = 1)
		SET @Command = @Command + N' and a.IsCashier = 0'

	IF (@Status = 0)
		SET @Command = @Command + N' and a.Status = 1'
	ELSE IF (@Status = 1)
		SET @Command = @Command + N' and a.Status = 0'
	ELSE IF (@Status = 2)
		SET @Command = @Command + N' and a.Status = -1'

	EXEC (@Command + N' order by a.ID DESC')
END
GO
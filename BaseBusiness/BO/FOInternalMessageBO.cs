using System;
using System.Collections.Generic;
using System.Linq;
using BaseBusiness.bc;
using BaseBusiness.Facade;
using BaseBusiness.util;
using Dapper;
using Microsoft.Data.SqlClient;

namespace BaseBusiness.BO
{
    public class BirthdayNotifRow
    {
        public int ProfileID { get; set; }
        public string Name { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MiddleName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Nationality { get; set; }
        public string Passport { get; set; }
        public string IdentityCard { get; set; }
        public string Telephone { get; set; }
        public string HandPhone { get; set; }
        public string Fax { get; set; }
        public string Email { get; set; }
        public string Website { get; set; }
        public string Address { get; set; }
        public string HomeAddress { get; set; }
        public string City { get; set; }
        public string Company { get; set; }
        public string BusinessTitle { get; set; }
        public string Occupation { get; set; }
    }

    public class InternalMessageNotifRow
    {
        public int ID { get; set; }
        public string FromName { get; set; }
        public string Message { get; set; }
        public bool IsRead { get; set; }
        public DateTime? SentDate { get; set; }
    }

    public class TraceNotifRow
    {
        public int ID { get; set; }
        public string Department { get; set; }
        public string TracesTime { get; set; }
        public string TracesText { get; set; }
        public string GuestName { get; set; }
        public int ReservationID { get; set; }
        public bool Resolved { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class FOInternalMessageBO : BaseBO
    {
        private readonly FOInternalMessageFacade facade = FOInternalMessageFacade.Instance;
        protected static FOInternalMessageBO instance = new();

        protected FOInternalMessageBO()
        {
            baseFacade = facade;
        }

        public static FOInternalMessageBO Instance => instance;

        public DateTime BusinessDate()
        {
            const string sql = "SELECT TOP 1 BusinessDate FROM BusinessDate WITH (NOLOCK)";
            using var conn = new SqlConnection(DBUtils.GetDBConnectionString());
            var d = conn.ExecuteScalar<DateTime?>(sql);
            return (d ?? DateTime.Today).Date;
        }

        private const string BirthdayTodayFilter = @"
p.DateOfBirth IS NOT NULL
  AND YEAR(p.DateOfBirth) > 1900
  AND DAY(CAST(p.DateOfBirth AS DATE)) = DAY(@biz)
  AND MONTH(CAST(p.DateOfBirth AS DATE)) = MONTH(@biz)
  AND LTRIM(RTRIM(ISNULL(p.Firstname, N'') + ISNULL(p.LastName, N''))) <> N''";

        public int CountBirthdays(DateTime biz)
        {
            var sql = $@"
SELECT COUNT(1)
FROM Profile p WITH (NOLOCK)
WHERE {BirthdayTodayFilter}";
            using var conn = new SqlConnection(DBUtils.GetDBConnectionString());
            return conn.ExecuteScalar<int>(sql, new { biz = biz.Date });
        }

        public List<BirthdayNotifRow> ListBirthdays(DateTime biz)
        {
            var sql = $@"
SELECT
    p.ID AS ProfileID,
    LTRIM(RTRIM(ISNULL(p.LastName, N'') + N' ' + ISNULL(p.Firstname, N''))) AS Name,
    ISNULL(p.Firstname, N'') AS FirstName,
    ISNULL(p.LastName, N'') AS LastName,
    ISNULL(p.MiddleName, N'') AS MiddleName,
    p.DateOfBirth,
    ISNULL(n.Name, N'') AS Nationality,
    ISNULL(p.PassPort, N'') AS Passport,
    ISNULL(p.IdentityCard, N'') AS IdentityCard,
    ISNULL(p.Telephone, N'') AS Telephone,
    ISNULL(p.HandPhone, N'') AS HandPhone,
    ISNULL(p.Fax, N'') AS Fax,
    ISNULL(p.Email, N'') AS Email,
    ISNULL(p.Website, N'') AS Website,
    ISNULL(p.Address, N'') AS Address,
    ISNULL(p.HomeAddress, N'') AS HomeAddress,
    ISNULL(p.City, N'') AS City,
    ISNULL(p.Company, N'') AS Company,
    ISNULL(p.BusinessTitle, N'') AS BusinessTitle,
    ISNULL(p.Occupation, N'') AS Occupation
FROM Profile p WITH (NOLOCK)
LEFT JOIN Nationality n WITH (NOLOCK) ON p.NationalityID = n.ID
WHERE {BirthdayTodayFilter}
ORDER BY Name";
            using var conn = new SqlConnection(DBUtils.GetDBConnectionString());
            return conn.Query<BirthdayNotifRow>(sql, new { biz = biz.Date }).AsList();
        }

        public int GetUserDepartmentId(int userId)
        {
            const string sql = "SELECT DepartmentID FROM Users WITH (NOLOCK) WHERE ID = @userId";
            using var conn = new SqlConnection(DBUtils.GetDBConnectionString());
            return conn.ExecuteScalar<int?>(sql, new { userId }) ?? 0;
        }

        public string GetUserDepartmentName(int userId)
        {
            const string sql = @"
SELECT ISNULL(d.Name, N'')
FROM Users u WITH (NOLOCK)
LEFT JOIN Department d WITH (NOLOCK) ON d.ID = u.DepartmentID
WHERE u.ID = @userId";
            using var conn = new SqlConnection(DBUtils.GetDBConnectionString());
            return conn.ExecuteScalar<string>(sql, new { userId }) ?? "";
        }

        private const string DeptFilter = @"
ISNULL(m.IsDelete, 0) = 0
  AND CAST(m.FromDate AS DATE) <= @biz
  AND (
        ISNULL(m.ToDepartmentID, 0) = 0
     OR ISNULL(m.ToDepartmentID, 0) = @deptId
     OR (N',' + REPLACE(ISNULL(m.ToDepartmentIDs, N''), N' ', N'') + N',' LIKE N'%,' + CAST(@deptId AS NVARCHAR(20)) + N',%')
  )";

        private const string VisibleFilter = DeptFilter + @"
  AND CAST(m.ToDate AS DATE) >= @biz";

        public int CountUnreadMessages(DateTime biz, int userId)
        {
            var deptId = GetUserDepartmentId(userId);
            var sql = $@"
SELECT COUNT(1)
FROM FOInternalMessage m WITH (NOLOCK)
WHERE {VisibleFilter}
  AND NOT EXISTS (
      SELECT 1 FROM FOInternalMessageReceived r WITH (NOLOCK)
      WHERE r.MessageID = m.ID AND r.UserID = @userId
  )";
            using var conn = new SqlConnection(DBUtils.GetDBConnectionString());
            return conn.ExecuteScalar<int>(sql, new { biz = biz.Date, userId, deptId });
        }

        public List<InternalMessageNotifRow> ListMessages(DateTime biz, int userId)
        {
            var deptId = GetUserDepartmentId(userId);
            var sql = $@"
SELECT
    m.ID,
    ISNULL(fd.Name, N'') AS FromName,
    m.Message,
    CAST(CASE WHEN r.ID IS NULL THEN 0 ELSE 1 END AS BIT) AS IsRead,
    m.CreateDate AS SentDate
FROM FOInternalMessage m WITH (NOLOCK)
LEFT JOIN Users u WITH (NOLOCK) ON m.UserInsertID = u.ID
LEFT JOIN Department fd WITH (NOLOCK) ON fd.ID = u.DepartmentID
LEFT JOIN FOInternalMessageReceived r WITH (NOLOCK) ON r.MessageID = m.ID AND r.UserID = @userId
WHERE {VisibleFilter}
ORDER BY m.CreateDate DESC, m.ID DESC";
            using var conn = new SqlConnection(DBUtils.GetDBConnectionString());
            return conn.Query<InternalMessageNotifRow>(sql, new { biz = biz.Date, userId, deptId }).AsList();
        }

        public int InsertMessage(string message, DateTime fromDate, DateTime toDate, int userId, string toDepartmentIds)
        {
            var ids = (toDepartmentIds ?? "")
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s.Trim(), out var n) ? n : (int?)null)
                .Where(n => n.HasValue)
                .Select(n => n.Value)
                .Distinct()
                .ToList();
            var isAll = ids.Count == 0 || ids.Contains(0);
            var toDepartmentId = isAll ? 0 : ids[0];
            var idList = isAll ? "" : string.Join(",", ids);

            const string sql = @"
INSERT INTO FOInternalMessage (Message, FromDate, ToDate, IsDelete, UserInsertID, CreateDate, ToDepartmentID, ToDepartmentIDs)
VALUES (@message, @fromDate, @toDate, 0, @userId, @createDate, @toDepartmentId, @idList);
SELECT CAST(SCOPE_IDENTITY() AS INT);";
            using var conn = new SqlConnection(DBUtils.GetDBConnectionString());
            return conn.ExecuteScalar<int>(sql, new
            {
                message,
                fromDate = fromDate.Date,
                toDate = toDate.Date,
                userId,
                toDepartmentId,
                idList,
                createDate = TextUtils.GetBusinessDateTime()
            });
        }

        public bool MarkReceived(int messageId, int userId)
        {
            const string sql = @"
IF NOT EXISTS (SELECT 1 FROM FOInternalMessageReceived WITH (NOLOCK) WHERE MessageID = @messageId AND UserID = @userId)
INSERT INTO FOInternalMessageReceived (MessageID, UserID, ReceivedDate)
VALUES (@messageId, @userId, @receivedDate);";
            using var conn = new SqlConnection(DBUtils.GetDBConnectionString());
            conn.Execute(sql, new { messageId, userId, receivedDate = TextUtils.GetBusinessDateTime() });
            return true;
        }

        private const string TraceInRangeFilter = @"
ISNULL(t.IsDelete, 0) = 0
  AND CAST(t.FromDate AS DATE) <= @biz
  AND CAST(t.ToDate AS DATE) >= @biz
  AND t.DepartmentID = @deptId";

        public int CountOpenTraces(DateTime biz, int userId)
        {
            var deptId = GetUserDepartmentId(userId);
            var sql = $@"
SELECT COUNT(1)
FROM ReservationTraces t WITH (NOLOCK)
WHERE {TraceInRangeFilter}
  AND ISNULL(t.Resolved, 0) = 0";
            using var conn = new SqlConnection(DBUtils.GetDBConnectionString());
            return conn.ExecuteScalar<int>(sql, new { biz = biz.Date, deptId });
        }

        public List<TraceNotifRow> ListOpenTraces(DateTime biz, int userId)
        {
            var deptId = GetUserDepartmentId(userId);
            var sql = $@"
SELECT
    t.ID,
    ISNULL(d.Name, N'') AS Department,
    ISNULL(t.TracesTime, N'') AS TracesTime,
    ISNULL(CONVERT(NVARCHAR(MAX), t.TracesText), N'') AS TracesText,
    -- Tên khách = tên hiển thị đầy đủ (Reservation.LastName / Profile.Account dạng Họ, Tên);
    -- trước đây ghép LastName + Firstname nên bị lặp chữ.
    -- Trace gắn booking: ưu tiên tên trên booking (dòng cũ có thể lưu nhầm ProfileID của khách khác)
    LTRIM(RTRIM(COALESCE(
        NULLIF(LTRIM(RTRIM(ISNULL(rsv.LastName, N''))), N''),
        NULLIF(LTRIM(RTRIM(ISNULL(p.Account, N''))), N''),
        N''
    ))) AS GuestName,
    t.ReservationID,
    CAST(CASE WHEN ISNULL(t.Resolved, 0) = 0 THEN 0 ELSE 1 END AS BIT) AS Resolved,
    t.FromDate,
    t.ToDate
FROM ReservationTraces t WITH (NOLOCK)
LEFT JOIN Department d WITH (NOLOCK) ON t.DepartmentID = d.ID
LEFT JOIN Reservation rsv WITH (NOLOCK) ON rsv.ID = t.ReservationID
LEFT JOIN Profile p WITH (NOLOCK) ON p.ID = CASE
    WHEN ISNULL(t.ProfileID, 0) > 0 THEN t.ProfileID
    ELSE rsv.ProfileIndividualId
END
WHERE {TraceInRangeFilter}
ORDER BY ISNULL(t.Resolved, 0), t.FromDate, t.ID";
            using var conn = new SqlConnection(DBUtils.GetDBConnectionString());
            return conn.Query<TraceNotifRow>(sql, new { biz = biz.Date, deptId }).AsList();
        }
    }
}

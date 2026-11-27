using System;
using System.Collections.Generic;
using BaseBusiness.bc;
using BaseBusiness.Facade;
using BaseBusiness.util;
using Dapper;
using Microsoft.Data.SqlClient;

namespace BaseBusiness.BO
{
    public class UserDetailHistoryRow
    {
        public string Username { get; set; }
        public string TypedPassword { get; set; }
        public int Type { get; set; }
        public string ComputerName { get; set; }
        public string ComputerIP { get; set; }
        public DateTime? CreatedDate { get; set; }
    }

    public class UserDetailBO : BaseBO
    {
        private UserDetailFacade facade = UserDetailFacade.Instance;
        protected static UserDetailBO instance = new UserDetailBO();

        protected UserDetailBO()
        {
            this.baseFacade = facade;
        }

        public static UserDetailBO Instance
        {
            get { return instance; }
        }

        public List<UserDetailHistoryRow> Search(int userId, int type, string? username = null)
        {
            const string sql = @"
SELECT u.LoginName AS Username, ud.TypedPassword, ud.Type,
       ud.ComputerName, ud.ComputerIP, ud.CreatedDate
FROM UserDetail ud WITH (NOLOCK)
LEFT JOIN Users u WITH (NOLOCK) ON ud.UserID = u.ID
WHERE ud.ID > 0
  AND (@userId = 0 OR ud.UserID = @userId)
  AND (@username = '' OR CHARINDEX(LOWER(@username), LOWER(u.LoginName)) > 0)
  AND (@type < 0 OR ud.Type = @type)
ORDER BY u.LoginName, ud.CreatedDate DESC";

            using var conn = new SqlConnection(DBUtils.GetDBConnectionString());
            return conn.Query<UserDetailHistoryRow>(sql, new { userId, type, username = username?.Trim() ?? "" }).AsList();
        }
    }
}

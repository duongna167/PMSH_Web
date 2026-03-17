using BaseBusiness.util;
using Microsoft.Data.SqlClient;
using Security.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Security.Services.Implements
{
    public class SecurityService : ISecurityService
    {
        public DataTable UserGroupData(string? Code, string? Name, int inactive = 0)
        {
            try
            {
                SqlParameter[] param = [
                    new SqlParameter("@Code", Code ?? string.Empty),
                    new SqlParameter("@Name", Name ?? string.Empty),
                    new SqlParameter("@Inactive", inactive)
                    ];
                DataTable myTable = DataTableHelper.getTableData("spFrmUserGroupSearch", param);
                return myTable;

            }
            catch (Exception ex)
            {
                throw new Exception($"ERROR: {ex.Message}", ex);
            }
        }

        public DataTable AddFuncitionsToListData(string codetag, string namerights, int isDataRight)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@Code", codetag),
                new SqlParameter("@Name", namerights),
                new SqlParameter("@IsDataRight", isDataRight),

            };

            DataTable myTable = DataTableHelper.getTableData("spPermissionAndShortcutKey_Search", param);
            return myTable;
        }
        public DataTable UsersManagementData(string lastName, string firstName, string loginName, int userStatus, int cashierStatus, string jobtitle, string department)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@LastName", lastName),
                new SqlParameter("@FirstName", firstName),
                new SqlParameter("@LoginName", loginName),
                new SqlParameter("@Status", userStatus),
                new SqlParameter("@Cashier", cashierStatus),
                new SqlParameter("@JobTitle", jobtitle),
                new SqlParameter("@Department", department),
            };

            DataTable myTable = DataTableHelper.getTableData("spSearchUsers", param);
            return myTable;
        }
    }
}

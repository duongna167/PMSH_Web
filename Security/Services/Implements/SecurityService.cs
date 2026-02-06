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
    public class SecurityService: ISecurityService
    {
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
    }
}

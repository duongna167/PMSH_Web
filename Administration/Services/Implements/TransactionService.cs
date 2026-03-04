using Administration.Services.Interfaces;
using BaseBusiness.util;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Administration.Services.Implements
{
    public class TransactionService : ITransactionService
    {
        public DataTable SearchTransaction(string code, string description, int groupID, int subGroupID)
        {
            try
            {
                SqlParameter[] param = new SqlParameter[]
                {
                    new SqlParameter("@Code", code),
                    new SqlParameter("@Description", description),
                    new SqlParameter("@GroupID", groupID),
                    new SqlParameter("@SubGroupID", subGroupID),

                };
                DataTable myTable = DataTableHelper.getTableData("spSearchTransaction", param);
                return myTable;
            }
            catch (SqlException ex)
            {
                throw new Exception($"ERROR: {ex.Message}", ex);
            }
        }
        public DataTable GetTransactionSubGroup()
        {
            SqlParameter[] param =
            [
                new SqlParameter("@sqlCommand",
                    @"Select * from TransactionSubGroup where Code like N'%' or Description like N'%'")
                    ];

            DataTable myTable = DataTableHelper.getTableData("spSearchAllForTrans", param);
            return myTable;
        }
    }
}

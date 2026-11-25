using BaseBusiness.util;
using Microsoft.Data.SqlClient;
using Profile.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Profile.Services.Implements
{
    public class FutureService: IFutureService
    {
        public DataTable SearchProfileFuture(int profileID, int profileType)
        {
            try
            {
                SqlParameter[] param = new SqlParameter[]
                {
                    new SqlParameter("@ProfileID", profileID),
                    new SqlParameter("@Type", profileType),
        

                };

                DataTable myTable = DataTableHelper.getTableData("spProfileFuture", param);
                return myTable ?? new DataTable();
            }
            catch (SqlException ex)
            {

                throw new Exception($"ERROR: {ex.Message}", ex);
            }
        }
    }
}

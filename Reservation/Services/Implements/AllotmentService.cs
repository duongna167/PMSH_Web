using BaseBusiness.util;
using Microsoft.Data.SqlClient;
using Reservation.Services.Interfaces;
using System.Data;

namespace Reservation.Services.Implements
{
    public class AllotmentService : IAllotmentService
    {
        public async Task<DataTable> GetAllAllotmentData(string? Code, string? Name, int inactive = 0)
        {
            try
            {
                SqlParameter[] param = [
                    new SqlParameter("@Code", Code ?? string.Empty),
                    new SqlParameter("@Name", Name ?? string.Empty),
                    new SqlParameter("@Inactive", inactive)
                    ];
                DataTable myTable = DataTableHelper.getTableData("spFrmRateClassSearch", param);
                return myTable;

            }
            catch (Exception ex)
            {

                throw new Exception($"ERROR: {ex.Message}", ex);
            }
        }

        public DataTable AllotmentType(string code, string name, int inactive)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@Code", code ?? ""),
                new SqlParameter("@Name", name ?? ""),
                new SqlParameter("@Inactive", inactive)
            };

            DataTable myTable = DataTableHelper.getTableData("spFrmAllotmentTypeSearch", param);
            return myTable;
        }

        public DataTable AllotmentStage(string code, string name, int inactive)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@Code", code ?? ""),
                new SqlParameter("@Name", name ?? ""),
                new SqlParameter("@Inactive", inactive)
            };

            DataTable myTable = DataTableHelper.getTableData("spFrmAllotmentStageSearch", param);
            return myTable;
        }
    }
}

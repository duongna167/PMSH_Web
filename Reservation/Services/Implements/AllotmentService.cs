using BaseBusiness.BO;
using BaseBusiness.Model;
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

        public DataTable AllotmentSearch(string code, string marketId, string allotmentTypeId, string profileId, string isDefault, string zone)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@Code", code ?? ""),
                new SqlParameter("@MarketID", marketId ?? ""),
                new SqlParameter("@AllotmentTypeID", allotmentTypeId ?? ""),
                new SqlParameter("@ProfileID", profileId ?? ""),
                new SqlParameter("@IsDefault", isDefault ?? ""),
                new SqlParameter("@Zone", zone ?? ""),
            };

            DataTable myTable = DataTableHelper.getTableData("spAllotmentSearch", param);
            return myTable;
        }

        public DataTable GetAllotmentDetail(int allotmentID, string roomTypeCodes, DateTime showHistory)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@AllotmentID", allotmentID), 
                new SqlParameter("@RoomType", roomTypeCodes), 
                new SqlParameter("@ShowHistory", showHistory)
            };

            DataTable myTable = DataTableHelper.getTableData("spAllotmentDetailSearch_Temp", param);
            return myTable;
        }

        public DataTable GetAllotmentResvSearch(string allotmentIDs, int roomTypeID)
        {
            try
            {
                SqlParameter[] param = new SqlParameter[]
                {
                    new SqlParameter("@AllotmentID", allotmentIDs ?? ""),
                    new SqlParameter("@RoomTypeID", roomTypeID)
                };

                DataTable myTable = DataTableHelper.getTableData("spAllotmentResvSearch", param);
                return myTable;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi gọi spAllotmentResvSearch: {ex.Message}", ex);
            }
        }

        public DataTable GetAllotmentDefaultByStage(DateTime fromDate, DateTime toDate, int type, string allotmentId, string paraDate, string paraDateConvert)
        {
            try
            {
                SqlParameter[] param = new SqlParameter[]
                {
                    new SqlParameter("@FromDate", SqlDbType.DateTime) { Value = fromDate },
                    new SqlParameter("@ToDate", SqlDbType.DateTime) { Value = toDate },
                    new SqlParameter("@Type", SqlDbType.Int) { Value = type },
                    new SqlParameter("@AllotmentID", SqlDbType.VarChar, 20) { Value = allotmentId },
                    new SqlParameter("@ParaDate", SqlDbType.NVarChar, 255) { Value = paraDate },
                    new SqlParameter("@ParaDateConvert", SqlDbType.NVarChar, 4000) { Value = paraDateConvert }
                };

                DataTable myTable = DataTableHelper.getTableData("spAllotmentDefaultByStage", param);

                return myTable;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi gọi spAllotmentDefaultByStage: {ex.Message}", ex);
            }
        }
        //public DataTable AllotmentReport(string code, string marketId, string allotmentTypeId)
        //{
        //    SqlParameter[] param = new SqlParameter[]
        //    {
        //        new SqlParameter("@Code", code ?? ""),
        //        new SqlParameter("@MarketID", marketId ?? ""),
        //        new SqlParameter("@AllotmentTypeID", allotmentTypeId ?? "")
        //    };

        //    DataTable myTable = DataTableHelper.getTableData("spAllotmentSearch", param);
        //    return myTable;
        //}
    }
}

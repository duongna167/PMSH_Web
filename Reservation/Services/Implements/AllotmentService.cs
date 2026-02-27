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

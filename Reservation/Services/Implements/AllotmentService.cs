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

        public async Task<(bool canDelete, string message)> CanDeleteAllotment(int allotmentId)
        {
            var checkDetailTask = Task.Run(() =>
                    Convert.ToInt32(TextUtils.Select($"SELECT ISNULL(SUM(Quantity),0) FROM AllotmentDetail WITH (NOLOCK) WHERE AllotmentID = {allotmentId}").Rows[0][0]));

            var checkResvTask = Task.Run(() =>
                Convert.ToInt32(TextUtils.Select($"SELECT COUNT(ID) FROM Reservation WITH (NOLOCK) WHERE AllotmentID = {allotmentId}").Rows[0][0]));

            var checkRateTask = Task.Run(() =>
                Convert.ToInt32(TextUtils.Select($"SELECT COUNT(ID) FROM ReservationRate WITH (NOLOCK) WHERE AllotmentID = {allotmentId}").Rows[0][0]));

            // Đợi cả 3 truy vấn hoàn tất cùng lúc
            await Task.WhenAll(checkDetailTask, checkResvTask, checkRateTask);

            // Kiểm tra kết quả
            if (checkDetailTask.Result > 0)
                return (false, "This allotment was used in Detail. Choose another allotment.");

            if (checkResvTask.Result > 0)
                return (false, "This allotment was used in Reservation. Choose another allotment.");

            if (checkRateTask.Result > 0)
                return (false, "This allotment was used in Reservation Rate. Choose another allotment.");

            return (true, string.Empty);
        }

        public async Task<bool> DeleteAllotment(int allotmentId)
        {
            try
            {
                await Task.Run(() => {
                    AllotmentDetailBO.Instance.DeleteByAttribute("AllotmentID", allotmentId);

                    AllotmentBO.Instance.Delete(allotmentId);
                });

                return true;
            }
            catch (Exception ex)
            {
                return false;
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


        public DataTable GetAllotmentReport(DateTime fromDate, DateTime toDate, string columnsString,string expressionString,int byAllotmentType,int byAllotmentDetail)
        {
            int typeValue;

            if (byAllotmentDetail == 1)
            {
                typeValue = byAllotmentDetail;   // ưu tiên Detail
            }
            else
            {
                typeValue = byAllotmentType;     // nếu không thì lấy Type
            }

            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@FromDate", fromDate),
                new SqlParameter("@ToDate", toDate),
                new SqlParameter("@Type", typeValue),
                new SqlParameter("@ParaDate", columnsString),
                new SqlParameter("@ParaDateConvert", expressionString),
            };
            DataTable myTable;

            if (byAllotmentType == 0 && byAllotmentDetail == 0)
            {
                myTable = DataTableHelper.getTableData("spReportAllotmentByAllotmentType", param);
            }
            else if (byAllotmentType == 1 && byAllotmentDetail == 0)
            {
                myTable = DataTableHelper.getTableData("spReportAllotmentByRoomType", param);
            }
            else if (byAllotmentDetail == 1)
            {
                myTable = DataTableHelper.getTableData("spReportAllotmentByAllotmentTypeDetail", param);
            }
            else
            {
                throw new ArgumentException("Invalid byAllotmentType value.");
            }

            return myTable;
        }

        public DataTable GetAllotmentandRoomTypeReport(DateTime fromDate, DateTime toDate, string columnsString, string expressionString, string allotmentType)
        {
 

            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@FromDate", fromDate),
                new SqlParameter("@ToDate", toDate),
                new SqlParameter("@AllotmentTypeID", allotmentType),
                new SqlParameter("@ParaDate", columnsString),
                new SqlParameter("@ParaDateConvert", expressionString),
            };
            DataTable  myTable = DataTableHelper.getTableData("spReportAllotmentByAllotmentTypeAndRoomType", param);
          

            return myTable;
        }
        public DataTable GetAllotmentandRoomTypeGroupByAllReport(DateTime fromDate, DateTime toDate, string columnsString, string expressionString, string allotmentType)
        {


            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@FromDate", fromDate),
                new SqlParameter("@ToDate", toDate),
                new SqlParameter("@AllotmentTypeID", allotmentType),
                new SqlParameter("@ParaDate", columnsString),
                new SqlParameter("@ParaDateConvert", expressionString),
            };
            DataTable myTable = DataTableHelper.getTableData("spReportAllotmentByAllotmentTypeAndRoomTypeGroupByAllotment", param);


            return myTable;
        }

        public DataTable GetAllotmentandRoomTypeGroupByRTReport(DateTime fromDate, DateTime toDate, string columnsString, string expressionString, string allotmentType)
        {


            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@FromDate", fromDate),
                new SqlParameter("@ToDate", toDate),
                new SqlParameter("@AllotmentTypeID", allotmentType),
                new SqlParameter("@ParaDate", columnsString),
                new SqlParameter("@ParaDateConvert", expressionString),
            };
            DataTable myTable = DataTableHelper.getTableData("spReportAllotmentByAllotmentTypeAndRoomTypeGroupByRoomType", param);


            return myTable;
        }
        public DataTable GetAllotmentProfileReport(DateTime fromDate, DateTime toDate, string columnsString, string expressionString)
        {


            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@FromDate", fromDate),
                new SqlParameter("@ToDate", toDate),
                new SqlParameter("@isPosted", false),
                new SqlParameter("@ParaDate", columnsString),
                new SqlParameter("@ParaDateConvert", expressionString),
                new SqlParameter("@IncludeCutoffAll", "0")
            };
            DataTable myTable = DataTableHelper.getTableData("spReportAllotmentByProfile", param);


            return myTable;
        }
        public DataTable GetAllotmentRoomtypedetailReport(DateTime fromDate, DateTime toDate, string columnsString, string expressionString)
        {


            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@FromDate", fromDate),
                new SqlParameter("@ToDate", toDate),
                new SqlParameter("@Type", false),
                new SqlParameter("@ParaDate", columnsString),
                new SqlParameter("@ParaDateConvert", expressionString),
     
            };
            DataTable myTable = DataTableHelper.getTableData("spReportAllotmentByRoomTypeDetail", param);


            return myTable;
        }
    }
}

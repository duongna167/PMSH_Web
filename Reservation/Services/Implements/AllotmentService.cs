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

        //transfer allotment/inventory
        public async Task<bool> ProcessTransfer(AllotmentTransferModel model, int stageId, int cutoffDay, DateTime? cutoffDate)
        {
            AllotmentTransferBO.Instance.Insert(model);

            for (var date = model.FromDate.Value; date <= model.ToDate.Value; date = date.AddDays(1))
            {
                // A. Cập nhật cho bên NHẬN (ToAllotmentID) -> Tăng Quantity
                UpdateOrInsertDetailFinal(
                    model.ToAllotmentID.Value,
                    model.RoomTypeID.Value,
                    date,
                    model.Quantity.Value,
                    model.CreateBy,
                    stageId,
                    cutoffDay,
                    cutoffDate
                );

                // B. Cập nhật cho bên CHUYỂN (FromAllotmentID) -> Giảm Quantity (nếu không phải Inventory)
                if (model.FromAllotmentID > 0)
                {
                    UpdateOrInsertDetailFinal(
                        model.FromAllotmentID.Value,
                        model.RoomTypeID.Value,
                        date,
                        -model.Quantity.Value, // Trừ số lượng bên chuyển
                        model.CreateBy,
                        stageId,
                        cutoffDay,
                        cutoffDate
                    );
                }
            }

            TextUtils.ExcuteSQL("DELETE dbo.AllotmentDetail WHERE Quantity = 0");

            return true;
        }

        private void UpdateOrInsertDetailFinal(int allotId, int rtId, DateTime date, int qtyChange, string user, int stageId, int cDay, DateTime? cDate)
        {
            string sqlCheck = $@"SELECT ID, Quantity FROM AllotmentDetail WITH (NOLOCK) 
                        WHERE AllotmentID = {allotId} AND RoomTypeID = {rtId} 
                        AND DATEDIFF(day, AllotmentDate, '{date:yyyy-MM-dd}') = 0";

            DataTable dt = TextUtils.Select(sqlCheck);
            string cutoffStr = cDate.HasValue ? $"'{cDate:yyyy-MM-dd}'" : "'1900-01-01'";

            if (dt != null && dt.Rows.Count > 0)
            {
                // UPDATE: Cộng dồn số lượng
                int newQty = Convert.ToInt32(dt.Rows[0]["Quantity"]) + qtyChange;
                string sqlUpdate = $@"UPDATE AllotmentDetail SET Quantity = {newQty}, 
                            CutOffDate = {cutoffStr}, CutOffDay = {cDay}, AllotmentStageID = {stageId},
                            UpdateBy = '{user}', UpdateDate = GETDATE() WHERE ID = {dt.Rows[0]["ID"]}";
                TextUtils.ExcuteSQL(sqlUpdate);
            }
            else if (qtyChange > 0)
            {
                // Tạo mới dòng detail
                string sqlInsert = $@"INSERT INTO AllotmentDetail(AllotmentID, AllotmentDate, RoomTypeID, Quantity, CreateBy, CreateDate, CutOffDate, CutOffDay, AllotmentStageID) 
                            VALUES ({allotId}, '{date:yyyy-MM-dd}', {rtId}, {qtyChange}, '{user}', GETDATE(), {cutoffStr}, {cDay}, {stageId})";
                TextUtils.ExcuteSQL(sqlInsert);
            }
        }

        public DataTable GetAllotmentLookupData()
        {

            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@Code", ""),
                new SqlParameter("@MarketID", ""),
                new SqlParameter("@AllotmentTypeID", ""),
                new SqlParameter("@ProfileID", ""),
                new SqlParameter("@IsDefault", ""),
                new SqlParameter("@Zone", "")
            };

            return DataTableHelper.getTableData("spAllotmentSearch", param);
        }

        public int GetActualAvailability(int fromAllotmentId, int roomTypeId, DateTime date)
        {
            try
            {
                // TRƯỜNG HỢP 1: Chuyển từ Allotment
                if (fromAllotmentId > 0)
                {
                    SqlParameter[] param = new SqlParameter[]
                    {
                new SqlParameter("@RoomTypeID", roomTypeId),
                new SqlParameter("@Date", date),
                new SqlParameter("@AllotmentID", fromAllotmentId),
                new SqlParameter("@Type", 1)
                    };

                    DataTable dt = DataTableHelper.getTableData("spCheckSetupAllotment", param);

                    if (dt != null && dt.Rows.Count > 0)
                    {
                        return Convert.ToInt32(dt.Rows[0]["Available"]);
                    }
                }
                // TRƯỜNG HỢP 2: Chuyển từ Inventory 
                else
                {
                    SqlParameter[] param = new SqlParameter[]
                    {
                new SqlParameter("@RoomTypeID", roomTypeId),
                new SqlParameter("@Date", date),
                new SqlParameter("@ReservationID", 0),
                new SqlParameter("@BlockID", 0)       
                    };

                    DataTable dt = DataTableHelper.getTableData("spCheckOverBooking", param);

                    if (dt != null && dt.Rows.Count > 0)
                    {
                        return Convert.ToInt32(dt.Rows[0]["Available"]);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi gọi Store check tồn kho: {ex.Message}");
            }
            return 0;
        }

        public int GetAvailability(int fromAllotID, int rtID, DateTime date)
        {
            return GetActualAvailability(fromAllotID, rtID, date);
        }

        public DataTable AllotmentSearchTransfer(string fromDate, string toDate, int allotmentFrom, int allotmentTo, string roomType)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@FromDate", fromDate ?? ""),
                new SqlParameter("@ToDate", toDate ?? ""),
                new SqlParameter("@AllotmentFrom", allotmentFrom),
                new SqlParameter("@AllotmentTo", allotmentTo),
                new SqlParameter("@RoomType", roomType ?? "")
            };

            DataTable myTable = DataTableHelper.getTableData("spAllotmentTransferSearch", param);
            return myTable;
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

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
        public void UpdateOrInsertDetail(int allotmentId, int roomTypeId, DateTime date, int quantityChange)
        {
            string sqlCheck = $@"SELECT ID, Quantity FROM AllotmentDetail WITH (NOLOCK) 
                        WHERE AllotmentID = {allotmentId} 
                        AND RoomTypeID = {roomTypeId} 
                        AND CAST(AllotmentDate AS DATE) = '{date:yyyy-MM-dd}'";

            DataTable dt = TextUtils.Select(sqlCheck);

            if (dt != null && dt.Rows.Count > 0)
            {
                // ĐÃ CÓ: Thực hiện UPDATE cộng dồn số lượng
                int currentQty = Convert.ToInt32(dt.Rows[0]["Quantity"]);
                int newQty = currentQty + quantityChange;
                int detailId = Convert.ToInt32(dt.Rows[0]["ID"]);

                string sqlUpdate = $"UPDATE AllotmentDetail SET Quantity = {newQty} WHERE ID = {detailId}";
                TextUtils.ExcuteSQL(sqlUpdate);
            }
            else
            {
                // CHƯA CÓ: Thực hiện INSERT dòng mới
                // quantityChange có thể âm nếu là bên chuyển đi,
                if (quantityChange > 0)
                {
                    string sqlInsert = $@"INSERT INTO AllotmentDetail (AllotmentID, RoomTypeID, AllotmentDate, Quantity) 
                                 VALUES ({allotmentId}, {roomTypeId}, '{date:yyyy-MM-dd}', {quantityChange})";
                    TextUtils.ExcuteSQL(sqlInsert);
                }
            }
        }

        public async Task<bool> ProcessTransfer(AllotmentTransferModel model)
        {
            AllotmentTransferBO.Instance.Insert(model);

            for (var date = model.FromDate.Value; date <= model.ToDate.Value; date = date.AddDays(1))
            {
                // Nếu chưa có dòng Detail cho ngày đó thì Insert, có rồi thì Update cộng thêm Quantity
                UpdateOrInsertDetail(model.ToAllotmentID.Value, model.RoomTypeID.Value, date, model.Quantity.Value);

                if (model.FromAllotmentID > 0)
                {
                    UpdateOrInsertDetail(model.FromAllotmentID.Value, model.RoomTypeID.Value, date, -model.Quantity.Value);
                }
            }
            return true;
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

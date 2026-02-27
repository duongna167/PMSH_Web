using BaseBusiness.BO;
using BaseBusiness.Model;
using BaseBusiness.util;
using DocumentFormat.OpenXml.Bibliography;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Reservation.Services.Interfaces;
using System.Data;
using static BaseBusiness.util.ValidationUtils;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;

namespace Reservation.Controllers
{
    public class AllotmentController : Controller
    {

        private readonly IConfiguration _configuration;
        private readonly ILogger<AllotmentController> _logger;
        private readonly IMemoryCache _cache;
        private readonly IAllotmentService _iAllotmentService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AllotmentController(ILogger<AllotmentController> logger,
                IMemoryCache cache, IConfiguration configuration, IAllotmentService iAllotmentService, IHttpContextAccessor httpContextAccessor)
        {
            _cache = cache;
            _logger = logger;
            _configuration = configuration;
            _iAllotmentService = iAllotmentService;
            _httpContextAccessor = httpContextAccessor;

        }

        #region Allotment Type
        [HttpGet]
        public IActionResult GetAllotmentType(string code, string name, int inactive)
        {
            try
            {
                DataTable dataTable = _iAllotmentService.AllotmentType(code, name, inactive);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  Code = !string.IsNullOrEmpty(d["Code"].ToString()) ? d["Code"] : "",
                                  Name = !string.IsNullOrEmpty(d["Name"].ToString()) ? d["Name"] : "",
                                  Description = !string.IsNullOrEmpty(d["Description"].ToString()) ? d["Description"] : "",
                                  InactiveText = !string.IsNullOrEmpty(d["InactiveText"].ToString()) ? d["InactiveText"] : "",
                                  CreatedBy = !string.IsNullOrEmpty(d["CreatedBy"].ToString()) ? d["CreatedBy"] : "",
                                  CreatedDate = !string.IsNullOrEmpty(d["CreatedDate"].ToString()) ? d["CreatedDate"] : "",
                                  UpdatedBy = !string.IsNullOrEmpty(d["UpdatedBy"].ToString()) ? d["UpdatedBy"] : "",
                                  UpdatedDate = !string.IsNullOrEmpty(d["UpdatedDate"].ToString()) ? d["UpdatedDate"] : "",
                                  ID = !string.IsNullOrEmpty(d["ID"].ToString()) ? d["ID"] : "",
                                  Inactive = !string.IsNullOrEmpty(d["Inactive"].ToString()) ? d["Inactive"] : "",
                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        [HttpGet]
        public IActionResult AllotmentType()
        {
            return PartialView("~/Views/Reservation/Allotment/AllotmentType.cshtml");
        }
        [HttpPost]
        public IActionResult AllotmentTypeSave([FromBody] AllotmentTypeModel model)
        {
            string message = "";
            var listErrors = GetErrors(
                Check(model == null, "general", "Invalid data"),

                Check(model?.Code, "code", "Code is not blank."),
                Check(model?.Name, "name", "Name is not blank.")
            );

            if (listErrors.Count == 0 && model != null)
            {
                bool isDuplicate = AllotmentTypeBO.Instance
                    .IsDuplicateCode(model.Code, model.ID);

                var duplicateError = CheckDuplicate(
                    isDuplicate,
                    "code",
                    $"This code already exists: [{model.Code}]"
                );

                if (duplicateError != null)
                {
                    listErrors.Add(duplicateError);
                }
            }
            if (listErrors.Count > 0)
            {
                return Json(new { success = false, errors = listErrors });
            }
            try
            {
                var businessDates = PropertyUtils.ConvertToList<BusinessDateModel>(
                    BusinessDateBO.Instance.FindAll()
                );

                DateTime businessDate = businessDates[0].BusinessDate;
                if (model.ID == 0)
                {
                    model.CreateDate = businessDate;
                    model.CreatedDate = businessDate;
                    model.UpdateDate = DateTime.Now;
                    model.UpdatedDate = DateTime.Now;

                    AllotmentTypeBO.Instance.Insert(model);
                    message = "Insert successfully!";
                }
                else
                {
                    var oldData = (AllotmentTypeModel)AllotmentTypeBO.Instance.FindByPrimaryKey(model.ID);

                    if (oldData != null)
                    {
                        //model.Code = oldData.Code;
                        model.CreateBy = oldData.CreateBy;
                        model.CreatedBy = oldData.CreatedBy;
                        model.CreateDate = oldData.CreatedDate;
                        model.CreatedDate = oldData.CreatedDate;
                    }

                    model.UpdateDate = DateTime.Now;
                    model.UpdatedDate = DateTime.Now;

                    AllotmentTypeBO.Instance.Update(model);
                    message = "Update successfully!";
                }

            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Json(new { success = false, message });
            }

            return Json(new { success = true, message });
        }
        [HttpPost]
        public IActionResult DeleteAllotmentType(int id)
        {
            try
            {
                AllotmentTypeBO.Instance.Delete(id);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, ex.Message });
            }

            return Json(new { success = true });
        }
        #endregion

        #region Allotment Stage
        [HttpGet]
        public IActionResult GetAllotmentStage(string code, string name, int inactive)
        {
            try
            {
                DataTable dataTable = _iAllotmentService.AllotmentStage(code, name, inactive);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  Code = !string.IsNullOrEmpty(d["Code"].ToString()) ? d["Code"] : "",
                                  Name = !string.IsNullOrEmpty(d["Name"].ToString()) ? d["Name"] : "",
                                  Description = !string.IsNullOrEmpty(d["Description"].ToString()) ? d["Description"] : "",
                                  InactiveText = !string.IsNullOrEmpty(d["InactiveText"].ToString()) ? d["InactiveText"] : "",
                                  CreatedBy = !string.IsNullOrEmpty(d["CreatedBy"].ToString()) ? d["CreatedBy"] : "",
                                  CreatedDate = !string.IsNullOrEmpty(d["CreatedDate"].ToString()) ? d["CreatedDate"] : "",
                                  UpdatedBy = !string.IsNullOrEmpty(d["UpdatedBy"].ToString()) ? d["UpdatedBy"] : "",
                                  UpdatedDate = !string.IsNullOrEmpty(d["UpdatedDate"].ToString()) ? d["UpdatedDate"] : "",
                                  ID = !string.IsNullOrEmpty(d["ID"].ToString()) ? d["ID"] : "",
                                  Inactive = !string.IsNullOrEmpty(d["Inactive"].ToString()) ? d["Inactive"] : "",
                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        public IActionResult AllotmentStage()
        {
            return PartialView("~/Views/Reservation/Allotment/AllotmentStage.cshtml");
        }
        [HttpPost]
        public IActionResult AllotmentStageSave([FromBody] AllotmentStageModel model)
        {
            string message = "";
            var listErrors = GetErrors(
                Check(model == null, "general", "Invalid data"),

                Check(model?.Code, "code", "Code is not blank."),
                Check(model?.Name, "name", "Name is not blank.")
            );

            if (listErrors.Count == 0 && model != null)
            {
                bool isDuplicate = AllotmentStageBO.Instance
                    .IsDuplicateCode(model.Code, model.ID);

                var duplicateError = CheckDuplicate(
                    isDuplicate,
                    "code",
                    $"This code already exists: [{model.Code}]"
                );

                if (duplicateError != null)
                {
                    listErrors.Add(duplicateError);
                }
            }
            if (listErrors.Count > 0)
            {
                return Json(new { success = false, errors = listErrors });
            }
            try
            {
                var businessDates = PropertyUtils.ConvertToList<BusinessDateModel>(
                    BusinessDateBO.Instance.FindAll()
                );

                DateTime businessDate = businessDates[0].BusinessDate;
                if (model.ID == 0)
                {
                    model.CreateDate = businessDate;
                    model.CreatedDate = businessDate;
                    model.UpdateDate = DateTime.Now;
                    model.UpdatedDate = DateTime.Now;

                    AllotmentStageBO.Instance.Insert(model);
                    message = "Insert successfully!";
                }
                else
                {
                    var oldData = (AllotmentStageModel)AllotmentStageBO.Instance.FindByPrimaryKey(model.ID);

                    if (oldData != null)
                    {
                        //model.Code = oldData.Code;
                        model.CreateBy = oldData.CreateBy;
                        model.CreatedBy = oldData.CreatedBy;
                        model.CreateDate = oldData.CreatedDate;
                        model.CreatedDate = oldData.CreatedDate;
                    }

                    model.UpdateDate = DateTime.Now;
                    model.UpdatedDate = DateTime.Now;

                    AllotmentStageBO.Instance.Update(model);
                    message = "Update successfully!";
                }

            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Json(new { success = false, message });
            }

            return Json(new { success = true, message });

        }
        [HttpPost]
        public IActionResult DeleteAllotmentStage(int id)
        {
            try
            {
                AllotmentStageBO.Instance.Delete(id);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, ex.Message });
            }

            return Json(new { success = true });
        }
        #endregion

        #region Allotment Search
        [HttpGet]
        public IActionResult GetAllotmentSearch(string code, string marketId, string allotmentTypeId, string profileId, string isDefault, string zone)
        {
            try
            {
                DataTable dataTable = _iAllotmentService.AllotmentSearch(code, marketId, allotmentTypeId, profileId, isDefault, zone);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  Code = !string.IsNullOrEmpty(d["Code"].ToString()) ? d["Code"] : "",
                                  AllotmentName = !string.IsNullOrEmpty(d["AllotmentName"].ToString()) ? d["AllotmentName"] : "",
                                  AccountName = !string.IsNullOrEmpty(d["AccountName"].ToString()) ? d["AccountName"] : "",
                                  MarketID = !string.IsNullOrEmpty(d["MarketID"].ToString()) ? d["MarketID"] : "",
                                  CuttOfDay = !string.IsNullOrEmpty(d["CuttOfDay"].ToString()) ? d["CuttOfDay"] : "",
                                  CuttOfDate = !string.IsNullOrEmpty(d["CuttOfDate"].ToString()) ? d["CuttOfDate"] : "",
                                  AllotmentTypeID = !string.IsNullOrEmpty(d["AllotmentTypeID"].ToString()) ? d["AllotmentTypeID"] : "",
                                  CreateBy = !string.IsNullOrEmpty(d["CreateBy"].ToString()) ? d["CreateBy"] : "",
                                  CreateDate = !string.IsNullOrEmpty(d["CreateDate"].ToString()) ? d["CreateDate"] : "",
                                  UpdateBy = !string.IsNullOrEmpty(d["UpdateBy"].ToString()) ? d["UpdateBy"] : "",
                                  UpdateDate = !string.IsNullOrEmpty(d["UpdateDate"].ToString()) ? d["UpdateDate"] : "",
                                  ProfileID = !string.IsNullOrEmpty(d["ProfileID"].ToString()) ? d["ProfileID"] : "",
                                  ID = !string.IsNullOrEmpty(d["ID"].ToString()) ? d["ID"] : "",
                                  IsDefault = !string.IsNullOrEmpty(d["IsDefault"].ToString()) ? d["IsDefault"] : "",
                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        public IActionResult AllotmentSearch()
        {
            List<MarketModel> listMarket = PropertyUtils.ConvertToList<MarketModel>(MarketBO.Instance.FindAll());
            ViewBag.MarketList = listMarket;

            List<AllotmentTypeModel> listAllotType = PropertyUtils.ConvertToList<AllotmentTypeModel>(AllotmentTypeBO.Instance.FindAll());
            ViewBag.AllotTypeList = listAllotType;

            List<ZoneModel> listZone = PropertyUtils.ConvertToList<ZoneModel>(ZoneBO.Instance.FindAll());
            ViewBag.ZoneList = listZone;

            return PartialView("~/Views/Reservation/Allotment/AllotmentSearch.cshtml");
        }

        #region Xử lý Allotment Detail
        [HttpGet]
        public IActionResult GetAllotmentDetail(int allotmentId, bool isHistoryChecked)
        {
            try
            {
                // Nếu không check History thì lấy ngày Business hiện tại, ngược lại lấy 01/01/1900
                DateTime showHistoryDate = isHistoryChecked ? new DateTime(1900, 1, 1) : TextUtils.GetBusinessDate();

                string roomTypeCodes = GetRoomTypeCodes(allotmentId);

                // Chuẩn bị danh sách các mã để gửi xuống Client (xóa bỏ ngoặc vuông [ ])
                var columnList = roomTypeCodes.Split(',')
                                    .Select(x => x.Trim('[', ']'))
                                    .Where(x => !string.IsNullOrEmpty(x))
                                    .ToList();

                // Gọi Service lấy dữ liệu thô
                DataTable dtRaw = _iAllotmentService.GetAllotmentDetail(allotmentId, roomTypeCodes, showHistoryDate);

                //  Thực hiện logic gộp ngày và TÍNH TOÁN TOTAL
                var processedData = ProcessAllotmentGrouping(dtRaw);

                // TRẢ VỀ CẢ DỮ LIỆU VÀ DANH SÁCH CỘT
                return Json(new
                {
                    data = processedData,
                    roomTypeColumns = columnList
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private List<Dictionary<string, object>> ProcessAllotmentGrouping(DataTable dt)
        {
            var result = new List<Dictionary<string, object>>();
            if (dt.Rows.Count == 0) return result;

            // Chuyển DataTable thành List để dễ duyệt
            var rows = dt.AsEnumerable().ToList();
            var flags = new int[rows.Count]; // 0: chưa xử lý, 1: hiển thị, 2: bị gộp (ẩn)

            for (int i = 0; i < rows.Count; i++)
            {
                if (flags[i] == 2) continue;

                var currentRow = rows[i];
                flags[i] = 1;

                // Tạo object kết quả cho dòng này
                var item = currentRow.Table.Columns.Cast<DataColumn>()
                .ToDictionary(col => col.ColumnName, col => {
                    var val = currentRow[col];
                    if (val == DBNull.Value) return 0;
                    return val;
                });
                // TÍNH TOTAL
                int totalQuantity = 0;
                foreach (DataColumn col in currentRow.Table.Columns)
                {
                    string name = col.ColumnName.ToLower();
                    // cộng dồn các cột 
                    if (!new[] { "date", "stage", "cutoff", "total", "flag", "allotmentstageid", "from date", "to date" }.Contains(name))
                    {
                        totalQuantity += Convert.ToInt32(item[col.ColumnName]);
                    }
                }
                item["Total"] = totalQuantity; // Gán giá trị tổng vừa tính vào cột Total
                                               
                DateTime startDate = Convert.ToDateTime(currentRow["date"]);
                DateTime endDate = startDate;

                item["From Date"] = startDate.ToString("dd/MM/yyyy");
                item["To Date"] = endDate.ToString("dd/MM/yyyy");

                // Vòng lặp tìm các dòng kế tiếp để gộp
                for (int j = i + 1; j < rows.Count; j++)
                {
                    DateTime nextDate = Convert.ToDateTime(rows[j]["date"]);

                    // Điều kiện gộp: Ngày liên tiếp (+1) VÀ Dữ liệu các cột giống hệt nhau
                    if ((nextDate - endDate).Days == 1 && IsSameData(rows[i], rows[j]))
                    {
                        endDate = nextDate;
                        item["To Date"] = endDate.ToString("dd/MM/yyyy");
                        flags[j] = 2; // Đánh dấu dòng này bị gộp
                    }
                    else break;
                }
                result.Add(item);
            }
            return result;
        }

        // Hàm so sánh dữ liệu các cột (Stage, CutOff và các loại RoomType)
        private bool IsSameData(DataRow row1, DataRow row2)
        {
            // So sánh Stage và CutOff
            if (row1["Stage"].ToString() != row2["Stage"].ToString() ||
                row1["CutOff"].ToString() != row2["CutOff"].ToString()) return false;

            // So sánh các cột RoomType (ví dụ POK, POT...)
            // Duyệt qua các cột không phải hệ thống để so sánh số lượng

            foreach (DataColumn col in row1.Table.Columns)
            {
                string name = col.ColumnName.ToLower();
                if (new[] { "date", "stage", "cutoff", "total", "flag", "allotmentstageid" }.Contains(name)) continue;

                // Chuyển về string và xử lý null thành "0" để so sánh chính xác
                string val1 = row1[col] == DBNull.Value ? "0" : row1[col].ToString();
                string val2 = row2[col] == DBNull.Value ? "0" : row2[col].ToString();

                if (val1 != val2) return false;
            }
            return true;
        }

        // Hàm lấy danh sách RoomType hiện có của Allotment này để tạo chuỗi [POK],[POT]...
        private string GetRoomTypeCodes(int allotmentId)
        {
            // Câu lệnh SQL lấy danh sách Code của RoomType
            string command = $@"SELECT b.Code 
                       FROM AllotmentDetail a WITH (NOLOCK)
                       JOIN RoomType b WITH (NOLOCK) ON a.RoomTypeID = b.ID 
                       WHERE a.AllotmentID = {allotmentId} 
                       GROUP BY b.Code 
                       ORDER BY b.Code";

            DataTable dtRoomType = TextUtils.Select(command);

            if (dtRoomType == null || dtRoomType.Rows.Count == 0) return "";

            // Nối chuỗi thành định dạng [POK],[POT]... để truyền vào PIVOT trong Store
            string roomTypeCodes = "";
            for (int i = 0; i < dtRoomType.Rows.Count; i++)
            {
                roomTypeCodes += "[" + dtRoomType.Rows[i][0].ToString() + "],";
            }

            return roomTypeCodes.TrimEnd(',');
        }
        #endregion

        #region Xử lý gridReservation

        [HttpGet]
        public IActionResult GetAllotmentResvSearch(int allotmentId, string roomTypeCode)
        {
            try
            {
                // Lấy RoomTypeID từ Code để phục vụ logic lọc 
                int roomTypeId = 0;
                if (!string.IsNullOrEmpty(roomTypeCode))
                {
                    string sqlRoom = $"SELECT ID FROM RoomType WITH (NOLOCK) WHERE Code = '{roomTypeCode}'";
                    DataTable dtRoom = TextUtils.Select(sqlRoom);
                    if (dtRoom.Rows.Count > 0)
                    {
                        roomTypeId = Convert.ToInt32(dtRoom.Rows[0]["ID"]);
                    }
                }

                // Lấy chuỗi danh sách ReservationID liên quan đến Allotment hiẹn tại
                string sqlResvIds = $@"SELECT DISTINCT a.ReservationID 
                               FROM dbo.ReservationRate a WITH (NOLOCK)
                               JOIN dbo.Reservation b WITH (NOLOCK) ON a.ReservationID = b.ID 
                               WHERE b.Status IN (0,5,1,6,2) 
                               AND a.AllotmentID = {allotmentId}";

                DataTable dtResvIds = TextUtils.Select(sqlResvIds);
                if (dtResvIds.Rows.Count == 0) return Json(new List<object>());

                string arrResvID = string.Join(",", dtResvIds.AsEnumerable().Select(r => r["ReservationID"].ToString()));

                DataTable dtResult = _iAllotmentService.GetAllotmentResvSearch(arrResvID, roomTypeId);

                // Map dữ liệu
                var result = (from d in dtResult.AsEnumerable()
                              select new
                              {
                                  ConfNo = d["ConfirmationNo"],
                                  MG = d["MG"],
                                  Nat = d["Nationality"],
                                  GuestName = d["GuestName"],
                                  Rms = d["NoOfRoom"],
                                  RoNo = d["RoomNo"],
                                  RoType = d["R_RoomType"],
                                  Arr = d["Arrival"] != DBNull.Value ? Convert.ToDateTime(d["Arrival"]).ToString("dd/MM/yyyy") : "",
                                  N = d["R_NoOfNight"],
                                  Dep = d["Departure"] != DBNull.Value ? Convert.ToDateTime(d["Departure"]).ToString("dd/MM/yyyy") : "",
                                  Adults = d["Adults"],
                                  Child = d["Child"],
                                  Child1 = d["Child1"],
                                  Child2 = d["Child2"],
                                  Status = d["Status"],
                                  Price = d["Price"],
                                  PriceNet = d["PriceNet"],
                                  Curr = d["Currency"],
                                  Packages = d["Packages"],
                                  MarketCode = d["MarketCode"],
                                  CreateBy = d["R_CreateBy"],
                                  CreateDate = d["R_CreateDate"] != DBNull.Value ? Convert.ToDateTime(d["R_CreateDate"]).ToString("dd/MM/yyyy HH:mm") : "",
                                  UpdateBy = d["R_UpdateBy"],
                                  UpdateDate = d["R_UpdateDate"] != DBNull.Value ? Convert.ToDateTime(d["R_UpdateDate"]).ToString("dd/MM/yyyy HH:mm") : "",
                                  ReservationID = d["ReservationID"]
                              }).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region new/edit/delete
        [HttpPost]
        public IActionResult SaveAllotment([FromBody] AllotmentModel model)
        {
            try
            {
                var listErrors = GetErrors(
                    Check(model == null, "general", "No data received."),
                    Check(model?.Code, "allot_setup_code", "Code is not blank."),
                    Check(model?.ProfileID, "allot_setup_profileID", "Profile is not blank."),
                    Check(model?.AllotmentTypeID, "allot_setup_type", "Allotment Type is not blank.")
                );

                if (listErrors.Count == 0 && model != null)
                {
                    bool isDuplicate = AllotmentBO.Instance.IsDuplicateCode(model.Code, model.ID);
                    var duplicateError = CheckDuplicate(isDuplicate, "code", $"This code already exists: [{model.Code}]");
                    if (duplicateError != null) listErrors.Add(duplicateError);
                }

                if (listErrors.Count > 0)
                {
                    return Json(new { success = false, errors = listErrors });
                }

                DateTime businessDate = TextUtils.GetBusinessDate();

                if (model.ID == 0) // Thêm mới (New)
                {
                    model.CreateDate = businessDate;
                    model.UpdateDate = DateTime.Now;

                    AllotmentBO.Instance.Insert(model);
                    return Json(new { success = true, message = "Insert successfully!" });
                }
                else // Chỉnh sửa (Edit)
                {
                    var oldData = (AllotmentModel)AllotmentBO.Instance.FindByPrimaryKey(model.ID);
                    if (oldData == null) return Json(new { success = false, message = "Data not found." });

                    model.CreateDate = oldData.CreateDate;
                    model.CreateBy = oldData.CreateBy;

                    model.UpdateDate = DateTime.Now;

                    AllotmentBO.Instance.Update(model);
                    return Json(new { success = true, message = "Update successfully!" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }
        #endregion

        #endregion

    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaseBusiness.BO;
using BaseBusiness.Model;
using BaseBusiness.util;
using DevExpress.XtraRichEdit.Import.Html;
using FrontDesk.Services.Implements;
using FrontDesk.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using BaseBusiness.util;
using Microsoft.Data.SqlClient;
using Org.BouncyCastle.Asn1;
using System.ServiceModel.Channels;
using static System.Runtime.InteropServices.JavaScript.JSType;
using DevExpress.Data.ODataLinq;
using System.Net.NetworkInformation;
using System.Collections;
using DevExpress.XtraRichEdit.Import.Doc;
namespace FrontDesk.Controllers
{
    public class FrontDeskController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<FrontDeskController> _logger;
        private readonly IMemoryCache _cache;
        private readonly IFrontDeskService _iFrontDeskService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public FrontDeskController(ILogger<FrontDeskController> logger,
                IMemoryCache cache, IConfiguration configuration, IFrontDeskService iFrontDeskService, IHttpContextAccessor httpContextAccessor)
        {
            _cache = cache;
            _logger = logger;
            _configuration = configuration;
            _iFrontDeskService = iFrontDeskService;
            _httpContextAccessor = httpContextAccessor;

        }

        public IActionResult TelephoneBook()
        {
            List<TelephoneBookCategoryModel> tlplist = PropertyUtils.ConvertToList<TelephoneBookCategoryModel>(TelephoneBookCategoryBO.Instance.FindAll());
            // "-all-" / "--All--" is a UI helper option, always show it first.
            var sortedList = (tlplist ?? new List<TelephoneBookCategoryModel>())
                .OrderBy(x =>
                {
                    var n = (x?.Name ?? string.Empty).Trim();
                    return (n.Equals("-all-", StringComparison.OrdinalIgnoreCase) || n.Equals("--All--", StringComparison.OrdinalIgnoreCase))
                        ? 0
                        : 1;
                })
                .ThenBy(x => (x?.Name ?? string.Empty).Trim())
                .ToList();
            ViewBag.TelephoneBookCategoryList = sortedList;
            DataTable dataTable = _iFrontDeskService.TelephoneBook("", "", "");
            var result = (from d in dataTable.AsEnumerable()
                          select new
                          {
                              id = d["id"]?.ToString(),
                              name = d["name"]?.ToString(),

                          }).ToList();
            ViewBag.TelephoneSelect = result;
            return PartialView();
        }
        [HttpGet]
        public JsonResult GetTelephoneBook(string CategoryCode, string BookCode)
        {
            CategoryCode = CategoryCode ?? "";
            BookCode = BookCode ?? "";
            try
            {
                DataTable dataTable = _iFrontDeskService.TelephoneBook("", CategoryCode, BookCode);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  id = d["id"]?.ToString(),
                                  name = d["name"]?.ToString(),

                              }).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetTelephoneDetail(int id, string name)
        {
            try
            {
                string sql = @"SELECT ID, Name, Telephone, Address, Remark, Color
                       FROM TelephoneBook
                       WHERE ID > 0";

                if (name != "--All--")
                {
                    sql += " AND TelephoneBookCategoryID = " + id;
                }

                sql += " ORDER BY Name";

                DataTable dt = TextUtils.Select(sql);
                var result = (from d in dt.AsEnumerable()
                              select new
                              {
                                  ID = d["ID"]?.ToString(),
                                  Name = d["Name"]?.ToString(),
                                  Telephone = d["Telephone"]?.ToString(),
                                  Address = d["Address"]?.ToString(),
                                  Remark = d["Remark"]?.ToString(),
                                  Color = d["Color"]?.ToString(),
                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet]
        public JsonResult GetTelephoneById(int id)
        {
            try
            {
                var obj = (TelephoneBookModel)TelephoneBookBO.Instance.FindByPrimaryKey(id);
                return Json(obj);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpPost]
        public JsonResult InsertTelephoneBook(string name, string telephone, string address, string remark, string webAddress, int categoryId, int color, int userID)
        {
            try
            {
                DateTime businessDate = TextUtils.GetBussinessDateTime();

                var vnPhoneRegex = new System.Text.RegularExpressions.Regex(@"^((0|\+84)(3|5|7|8|9)[0-9]{8}|0\d{2,3}\d{7,8})$");

                if (string.IsNullOrWhiteSpace(telephone) || !vnPhoneRegex.IsMatch(telephone))
                {
                    return Json(new { success = false, message = "Số điện thoại Việt Nam không hợp lệ (di động hoặc máy bàn)!" });
                }

                var model = new TelephoneBookModel
                {
                    Name = name,
                    Telephone = telephone,
                    Address = address,
                    Remark = remark,
                    WebAddress = webAddress,
                    TelephoneBookCategoryID = categoryId,
                    Color = color,
                    CreateDate = businessDate,
                    UserInsertID = userID
                };

                TelephoneBookBO.Instance.Insert(model);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpPost]
        public JsonResult UpdateTelephoneBook(int id, string name, string telephone, string address,
    string remark, string webAddress, int categoryId, int color, int userID)
        {
            try
            {
                DateTime businessDate = TextUtils.GetBussinessDateTime();

                var model = new TelephoneBookModel
                {
                    ID = id,
                    Name = name,
                    Telephone = telephone,
                    Address = address,
                    Remark = remark,
                    WebAddress = webAddress,
                    TelephoneBookCategoryID = categoryId,
                    Color = color,
                    UserUpdateID = userID,
                    UpdateDate = businessDate
                };

                TelephoneBookBO.Instance.Update(model);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpPost]
        public JsonResult DeleteTelephoneBook(int id)
        {
            try
            {
                TelephoneBookBO.Instance.Delete(id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpGet]
        public IActionResult TelephoneSwitchSearch(string roomNo, int foStatus)
        {
            try
            {
                DataTable dataTable = _iFrontDeskService.TelephoneSwitch(roomNo, foStatus);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  id = d["ID"]?.ToString(),
                                  roomNo = d["RoomNo"]?.ToString(),
                                  foStatus = d["FOStatus"]?.ToString(),
                                  code = d["Code"]?.ToString(),
                                  guestName = d["GuestName"]?.ToString(),
                                  checkInDate = d["CheckInDate"]?.ToString(),
                                  checkOutDate = d["CheckOutDate"]?.ToString(),
                                  newValue = d["NewValue"]?.ToString()
                              }).ToList();

                // Dùng Ok thay vì Json để System.Text.Json serialize theo đúng tên bạn đặt
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        public IActionResult TelephoneSwitch()
        {
            return View(); // View này sẽ chứa DataGrid + script gọi API
        }

        [HttpPost]
        public IActionResult UpdateTelephoneSwitch([FromBody] TelephoneSwitchUpdateModel model)
        {
            try
            {
                string sqlGetObjectId = "exec spTelephoneSwitchSearch @RoomNo, @FOStatus";
                var spParams = new SqlParameter[]
                {
                    new SqlParameter("@RoomNo", model.RoomNo),
                    new SqlParameter("@FOStatus", 2)
                };

                DataTable objDt = DataTableHelper.ExecuteQuery(sqlGetObjectId, spParams);

                int objectId = 0;
                if (objDt.Rows.Count > 0 && objDt.Rows[0]["ID"] != DBNull.Value)
                {
                    objectId = Convert.ToInt32(objDt.Rows[0]["ID"]);
                }
                int userId = HttpContext.Session.GetInt32("UserID") ?? 0;
                string userName = HttpContext.Session.GetString("LoginName") ?? "system";

                var computerName = Environment.MachineName;

                string sqlSelect = @"SELECT TOP 1 NewValue 
                             FROM RoomStatusHistory 
                             WHERE RoomNo = @RoomNo 
                             ORDER BY ChangeDate DESC";

                var selectParams = new SqlParameter[]
                {
                    new SqlParameter("@RoomNo", model.RoomNo)
                };

                DataTable dt = DataTableHelper.ExecuteQuery(sqlSelect, selectParams);

                string oldValue = "Off"; // mặc định
                if (dt.Rows.Count > 0 && dt.Rows[0]["NewValue"] != DBNull.Value)
                {
                    oldValue = dt.Rows[0]["NewValue"].ToString();
                }

                string newValue = model.NewValue;

                string sql1 = @"INSERT INTO RoomStatusHistory 
                        (ObjectID, TableName, UserName, RoomNo, Action, ComputerName, OldValue, NewValue, ChangeDate) 
                        VALUES (@ObjectID, @TableName, @UserName, @RoomNo, @Action, @ComputerName, @OldValue, @NewValue, GETDATE());
                        SELECT SCOPE_IDENTITY();";

                var parameters1 = new SqlParameter[]
                {
                    new SqlParameter("@ObjectID", objectId),
                    new SqlParameter("@TableName", "Room"),
                    new SqlParameter("@UserName", userName),  // lấy trực tiếp từ session
                    new SqlParameter("@RoomNo", model.RoomNo),
                    new SqlParameter("@Action", "Telephone switch"),
                    new SqlParameter("@ComputerName", computerName),
                    new SqlParameter("@OldValue", oldValue),
                    new SqlParameter("@NewValue", newValue)
                };

                int historyId = DataTableHelper.ExecuteInsertAndReturnId(sql1, parameters1);

                string sql2 = @"INSERT INTO TelephoneSwitch (RoomNo, GuestName, Status, CreateDate) 
                        VALUES (@RoomNo, @GuestName, @Status, GETDATE());
                        SELECT SCOPE_IDENTITY();";

                int status = model.NewValue == "On" ? 1 : 0;

                var parameters2 = new SqlParameter[]
                {
                    new SqlParameter("@RoomNo", model.RoomNo),
                    new SqlParameter("@GuestName", model.GuestName ?? (object)DBNull.Value),
                    new SqlParameter("@Status", status)
                };

                int switchId = DataTableHelper.ExecuteInsertAndReturnId(sql2, parameters2);

                return Json(new { success = true, historyId, switchId, userName });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        public IActionResult DialingInformation()
        {
            List<ZoneModel> listzo = PropertyUtils.ConvertToList<ZoneModel>(ZoneBO.Instance.FindAll());
            ViewBag.ZoneList = listzo;
            return PartialView(); // View này sẽ chứa DataGrid + script gọi API
        }
        [HttpGet]
        public IActionResult GetDialingInformation(DateTime fromDate, DateTime toDate, string phoneNo, int view, string zone)
        {
            try
            {
                DataTable dataTable = _iFrontDeskService.DialingInformation(fromDate, toDate, phoneNo, view, zone);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  FromPhoneNo = !string.IsNullOrEmpty(d["From Phone No"].ToString()) ? d["From Phone No"] : "",
                                  ToPhoneNo = !string.IsNullOrEmpty(d["To Phone No"].ToString()) ? d["To Phone No"] : "",
                                  TimeStart = !string.IsNullOrEmpty(d["Time Start"].ToString()) ? d["Time Start"] : "",
                                  TimeEnd = !string.IsNullOrEmpty(d["Time End"].ToString()) ? d["Time End"] : "",
                                  Duration = !string.IsNullOrEmpty(d["Duration"].ToString()) ? d["Duration"] : "",
                                  Area = !string.IsNullOrEmpty(d["Area"].ToString()) ? d["Area"] : "",
                                  Amount = !string.IsNullOrEmpty(d["Amount"].ToString()) ? d["Amount"] : "",
                                  Currency = !string.IsNullOrEmpty(d["Currency"].ToString()) ? d["Currency"] : "",
                              }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        #region WakeUpCall
        public IActionResult WakeUpCall()
        {
            List<ZoneModel> listzo = PropertyUtils.ConvertToList<ZoneModel>(ZoneBO.Instance.FindAll());
            ViewBag.ZoneList = listzo;
            List<RoomModel> listro = PropertyUtils.ConvertToList<RoomModel>(RoomBO.Instance.FindAll());
            ViewBag.RoomList = listro;
            List<RoomClassModel> listroclass = PropertyUtils.ConvertToList<RoomClassModel>(RoomClassBO.Instance.FindAll());
            ViewBag.RoomClassList = listroclass;
            List<BusinessDateModel> businessDateModel = PropertyUtils.ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll());
            ViewBag.BusinessDate = businessDateModel[0].BusinessDate;
            return View();
        }

        [HttpGet]
        public IActionResult WakeUpCallFindRoom(string roomNoset, string reservationHolder, string zone, string confirmNo)
        {
            roomNoset = roomNoset ?? "";
            reservationHolder = reservationHolder ?? "";
            zone = zone ?? "";
            confirmNo = confirmNo ?? "";
            try
            {
                DataTable dataTable = _iFrontDeskService.WakeUpCallFindRoom(roomNoset, reservationHolder, zone, confirmNo);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  ID = !string.IsNullOrEmpty(d["ID"].ToString()) ? d["ID"].ToString() : "",
                                  Room = !string.IsNullOrEmpty(d["Room"].ToString()) ? d["Room"].ToString() : "",
                                  ConfirmNo = !string.IsNullOrEmpty(d["Confirm No"].ToString()) ? d["Confirm No"].ToString() : "",
                                  Name = !string.IsNullOrEmpty(d["Name"].ToString()) ? d["Name"].ToString() : "",
                                  ShareRoom = !string.IsNullOrEmpty(d["Share Room"].ToString()) ? d["Share Room"].ToString() : "",
                                  ArrDate = !string.IsNullOrEmpty(d["Arr Date"].ToString()) ? Convert.ToDateTime(d["Arr Date"]).ToString("yyyy-MM-dd") : "",
                                  DepDate = !string.IsNullOrEmpty(d["Dep Date"].ToString()) ? Convert.ToDateTime(d["Dep Date"]).ToString("yyyy-MM-dd") : "",
                                  ReservationHolder = !string.IsNullOrEmpty(d["Reservation Holder"].ToString()) ? d["Reservation Holder"].ToString() : ""

                              }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult WakeUpCallSearch(DateTime currentDate, string searchforName, int isSpecial)
        {

            searchforName = searchforName ?? "";

            try
            {
                DataTable dataTable = _iFrontDeskService.WakeUpCallSearch(currentDate, searchforName, isSpecial);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  ID = !string.IsNullOrEmpty(d["ID"].ToString()) ? d["ID"].ToString() : "",
                                  RoomID = !string.IsNullOrEmpty(d["RoomID"].ToString()) ? d["RoomID"].ToString() : "",
                                  Room = !string.IsNullOrEmpty(d["Room"].ToString()) ? d["Room"].ToString() : "",

                                  Name = !string.IsNullOrEmpty(d["Name"].ToString()) ? d["Name"].ToString() : "",
                                  DateTime = !string.IsNullOrEmpty(d["Date/Time"].ToString()) ? Convert.ToDateTime(d["Date/Time"]).ToString("yyyy-MM-dd") : "",

                              }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult ViewWakeUpCallAccount(int roomID, int shareRoom)
        {



            try
            {
                DataTable dataTable = _iFrontDeskService.ViewWakeUpCallAccount(roomID, shareRoom);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {

                                  Account = !string.IsNullOrEmpty(d["Account"].ToString()) ? d["Account"].ToString() : "",


                              }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        [HttpGet]
        public IActionResult ViewWakeUpCall(string name, string group, string roomview, DateTime fromDateview, DateTime toDateview, string timeDailyview, int roomClass)
        {

            name = name ?? "";
            group = group ?? "";
            roomview = roomview ?? "";
            timeDailyview = timeDailyview ?? "";
            string hour = "";
            string minute = "";

            if (!string.IsNullOrEmpty(timeDailyview))
            {
                var parts = timeDailyview.Split(':');
                if (parts.Length == 2)
                {
                    hour = parts[0];
                    minute = parts[1];
                }
            }



            try
            {
                DataTable dataTable = _iFrontDeskService.ViewWakeUpCall(name, group, roomview, fromDateview, toDateview, hour, minute, roomClass);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  ID = d["ID"]?.ToString() ?? "",
                                  WakeUpID = d["WakeUpID"]?.ToString() ?? "",
                                  RoomNo = d["Room No"]?.ToString() ?? "",
                                  Status = d["Status"]?.ToString() ?? "",
                                  GuestName = d["Guest Name"]?.ToString() ?? "",
                                  GroupName = d["Group Name"]?.ToString() ?? "",
                                  WUDate = d["WU Date"]?.ToString() ?? "",
                                  WUTime = d["WU Time"]?.ToString() ?? "",
                                  ShareRoom = d["ShareRoom"]?.ToString() ?? "",
                                  CreatedDate = d["Created Date"]?.ToString() ?? "",
                                  UpdatedDate = d["Updated Date"]?.ToString() ?? "",
                                  UserCancel = d["User Cancel"]?.ToString() ?? "",
                                  UserSetup = d["User Setup"]?.ToString() ?? "",
                                  ConfirmationNo = d["ConfirmationNo"]?.ToString() ?? "",
                                  ArrivalDate = d["ArrivalDate"]?.ToString() ?? "",
                                  DepartureDate = d["DepartureDate"]?.ToString() ?? ""


                              }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult SetWakeUpCall([FromBody] WakeUpCallRequest request)
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();
                string callType = request.callType ?? "";
                string timeDate = request.timeDate ?? "";
                string timeDaily = request.timeDaily ?? "";
                string message = "";
                List<WakeUpCallRow> selectedRows = request.selectedRows ?? new List<WakeUpCallRow>();
                #region insert 1 ngày cho phòng ko phải Group
                if (request.callType == "date")//insert 1 ngày cho phòng ko phải Group
                {

                    for (int i = 0; i < request.selectedRows.Count - 1; i++)
                    {
                        WakeUpCallRow row = request.selectedRows[i];
                        string strDateTime = request.singleDate.ToString("yyyy-MM-dd") + " " + request.timeDate;

                        DateTime wkTime = ConvertStringDateTime(strDateTime);
                        //check xem ngay wk co vuot qua ngay Departure ko
                        string strCheckResult = checkDepartureDate(wkTime, row.Room, row.Id.ToString());
                        message += strCheckResult;
                        if (strCheckResult != "") continue;
                        int shareRoom = 0;
                        //if (arrShareRoom.Length > 1)
                        //    shareRoom = int.Parse(arrShareRoom[i]);
                        WakeUpCallModel modelWUC = new WakeUpCallModel();
                        modelWUC.UpdateDate = DateTime.Now;
                        modelWUC.CreateDate = DateTime.Now;
                        modelWUC.WakeUpTime = wkTime;
                        modelWUC.RoomID = int.Parse(row.Id);
                        modelWUC.Status = 0;
                        modelWUC.UserInsertID = int.Parse(request.userID);
                        modelWUC.Name = row.Name;
                        modelWUC.ProfileGroupID = 0;
                        modelWUC.ShareRoom = shareRoom;
                        int wcID = (int)WakeUpCallBO.Instance.Insert(modelWUC);
                        message += "wake up call is created for room: " + row.Room + " \n";
                        #region ghi log
                        string description = "Wake up call set for room: " + row.Room + ", Time: " + request.timeDate + " from " + request.singleDate.ToShortDateString() + " to " + request.singleDate.ToShortDateString();
                        WakeUpCallLogModel wcLogModel = new WakeUpCallLogModel();
                        wcLogModel.CreateDate = DateTime.Now;
                        wcLogModel.WakeUpCallID = wcID;
                        wcLogModel.ActionDescription = description;
                        WakeUpCallLogBO.Instance.Insert(wcLogModel);
                        #endregion
                    }

                }
                #endregion
                #region insert nhiều ngày cho phòng ko phải Group
                if (request.callType == "daily")//insert nhiều ngày cho phòng ko phải Group
                {

                    TimeSpan oneDate = new TimeSpan(1, 0, 0, 0);
                    for (int i = 0; i < request.selectedRows.Count - 1; i++)
                    {
                        for (DateTime date = request.fromDate; date <= request.toDate; date += oneDate)
                        {

                            string strDateTime = date.ToString("dd/MM/yyyy") + " " + timeDaily;
                            WakeUpCallRow row = request.selectedRows[i];
                            //check xem ngay wk co vuot qua ngay Departure ko
                            string strCheckResult = checkDepartureDate(date, row.Room, row.Id.ToString());
                            message += strCheckResult;
                            if (strCheckResult != "") continue;

                            int shareRoom = 0;
                            //if (arrShareRoom.Length > 1)
                            //    shareRoom = int.Parse(arrShareRoom[i]);

                            WakeUpCallModel modelWUC = new WakeUpCallModel();
                            modelWUC.UpdateDate = DateTime.Now;
                            modelWUC.CreateDate = DateTime.Now;
                            modelWUC.WakeUpTime = ConvertStringDateTime(strDateTime);
                            modelWUC.RoomID = int.Parse(row.Id);
                            modelWUC.Status = 0;
                            modelWUC.UserInsertID = int.Parse(request.userID);
                            modelWUC.Name = row.Name;
                            modelWUC.ProfileGroupID = 0;
                            modelWUC.ShareRoom = shareRoom;
                            int wcID = (int)WakeUpCallBO.Instance.Insert(modelWUC);
                            message += "wake up call is created for room: " + row.Room + " \n";

                            #region ghi log
                            if (date == request.fromDate)
                            {
                                string description = "Wake up call set for room: " + row.Room + ", Time: " + request.timeDaily + " from " + request.fromDate.ToShortDateString() + " to " + request.toDate.ToShortDateString();
                                WakeUpCallLogModel wcLogModel = new WakeUpCallLogModel();
                                wcLogModel.CreateDate = DateTime.Now;
                                wcLogModel.WakeUpCallID = wcID;
                                wcLogModel.ActionDescription = description;
                                WakeUpCallLogBO.Instance.Insert(wcLogModel);
                            }
                            #endregion
                        }
                    }

                }
                #endregion
                pt.CommitTransaction();
                return Json(new { success = true, message = message });


            }
            catch (Exception ex)
            {
                pt.RollBack();
                return Json(new { success = false, message = ex.Message });

            }
            finally
            {
                pt.CloseConnection();

            }
        }

        [HttpPost]
        public IActionResult CancelWakeUpCall([FromBody] WakeUpCallRequest request)
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();
                string callType = request.callType ?? "";
                string timeDate = request.timeDate ?? "";
                string timeDaily = request.timeDaily ?? "";
                string message = "";
                List<BusinessDateModel> businessDateModel = PropertyUtils.ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll());
                DateTime SystemDate = businessDateModel[0].BusinessDate;
                List<WakeUpCallRow> selectedRows = request.selectedRows ?? new List<WakeUpCallRow>();
                #region Cancel tất cả wakeupcall ở những phòng không thuộc group
                if (request.callType == "daily")//xóa tất cả wakeupcall ở những phòng không thuộc group
                {

                    for (int i = 0; i < request.selectedRows.Count - 1; i++)
                    {
                        WakeUpCallRow row = request.selectedRows[i];

                        WakeUpCallModel wcModel = (WakeUpCallModel)WakeUpCallBO.Instance.FindByAttribute("RoomID", int.Parse(row.Id))[0];

                        #region update Cancel WUC
                        wcModel.UpdateDate = SystemDate;
                        wcModel.UserUpdateID = int.Parse(request.userID);
                        wcModel.Status = 4;//failed
                        WakeUpCallBO.Instance.Update(wcModel);
                        #endregion


                        //WakeUpCallBO.Instance.DeleteByAttribute("RoomID", int.Parse(arrRoomID[i]));  

                        #region ghi log                        
                        WakeUpCallLogModel wcLogModel = new WakeUpCallLogModel();
                        wcLogModel.CreateDate = wcModel.UpdateDate;
                        wcLogModel.WakeUpCallID = wcModel.ID;
                        wcLogModel.ActionDescription = "Cancel Wake up calls for room:" + row.Room;
                        wcLogModel.RoomNo = row.Room;
                        wcLogModel.GuestName = wcModel.Name;
                        wcLogModel.InsertDate = wcModel.WakeUpTime.ToShortDateString();
                        wcLogModel.InsertTime = wcModel.WakeUpTime.TimeOfDay.ToString();
                        WakeUpCallLogBO.Instance.Insert(wcLogModel);
                        #endregion   
                    }
                    message = "Delete Successfully!";

                }
                #endregion

                #region Cancel từng Schedule không thuộc group
                if (request.callType == "date")//xóa từng Schedule không thuộc group
                {


                    for (int i = 0; i < request.selectedRows.Count - 1; i++)
                    {
                        WakeUpCallRow row = request.selectedRows[i];
                        int wcID = int.Parse(row.Id);
                        WakeUpCallModel wcModel = (WakeUpCallModel)WakeUpCallBO.Instance.FindByPrimaryKey(wcID);
                        #region update Cancel WUC
                        wcModel.UpdateDate = SystemDate;
                        wcModel.UserUpdateID = int.Parse(request.userID);
                        wcModel.Status = 4;//failed
                        WakeUpCallBO.Instance.Update(wcModel);
                        #endregion
                        //WakeUpCallBO.Instance.Delete(wcID);
                        #region ghi log
                        string description = "All wake up calls are cancelled for room: " + row.Room;
                        WakeUpCallLogModel wcLogModel = new WakeUpCallLogModel();
                        wcLogModel.CreateDate = wcModel.CreateDate;
                        wcLogModel.WakeUpCallID = wcModel.ID;
                        wcLogModel.ActionDescription = description;
                        wcLogModel.RoomNo = row.Room;
                        wcLogModel.GuestName = wcModel.Name;
                        wcLogModel.InsertDate = wcModel.WakeUpTime.ToShortDateString();
                        wcLogModel.InsertTime = wcModel.WakeUpTime.TimeOfDay.ToString();
                        WakeUpCallLogBO.Instance.Insert(wcLogModel);
                        #endregion   
                    }
                    message = "Delete Successfully!";
                }
                #endregion
                pt.CommitTransaction();
                return Json(new { success = true, message = message });


            }
            catch (Exception ex)
            {
                pt.RollBack();
                return Json(new { success = false, message = ex.Message });

            }
            finally
            {
                pt.CloseConnection();

            }
        }
        private string checkDepartureDate(DateTime wkDate, string roomNo, string roomID)
        {
            string message = "";
            #region check thời gian wc có vượt quá ngày departure ko
            string command = "Select Max(DepartureDate) as lastDeparture from dbo.Reservation with (nolock) where Status in (1,6) and ReservationNo > 0 and RoomID=" + roomID.ToString();
            DataTable dtresv = BaseBusiness.util.TextUtils.Select(command);
            string a = dtresv.Rows[0][0].ToString();
            DateTime lastDeparture = ConvertStringDateTime(dtresv.Rows[0][0].ToString());
            TimeSpan tcompare = lastDeparture - wkDate;
            if (tcompare.Days < 0)
                message += "Can not create for room: " + roomNo + "  Wakeup Date must be <= the departure date \n";
            #region
            string command1 = "select ID from Wakeupcall where RoomID = " + roomID.ToString() + " and datediff(minute,WakeUpTime,'" + wkDate.ToString("MM/dd/yyyy HH:mm") + "')=0 and Status not in (3,4,5)";
            DataTable dt = BaseBusiness.util.TextUtils.Select(command1);
            if (dt.Rows.Count > 0)
                message += "wake up call for room: " + roomNo + " is exist already, can not create more \n";
            #endregion

            return message;
            #endregion 
        }
        private DateTime ConvertStringDateTime(string strDate)
        {
            DateTime result = new DateTime();

            if (strDate.Contains("/"))
            {
                string day;
                string month;
                string yearAndTime;
                string[] arrStrDate = strDate.Split('/');
                day = arrStrDate[0];
                month = arrStrDate[1];
                yearAndTime = arrStrDate[2];
                try
                {
                    DateTime test = Convert.ToDateTime("12/13/2010");
                    //chạy qua đây tức là định dạng MM/dd/yyyy
                    string strResult = month + "/" + day + "/" + yearAndTime;
                    result = Convert.ToDateTime(strResult);
                }
                catch
                {
                    //chạy vào đây tức là định dạng dd/MM/yyyy
                    string strResult = day + "/" + month + "/" + yearAndTime;
                    result = Convert.ToDateTime(strResult);
                }
            }
            if (strDate.Contains("."))
            {
                string day;
                string month;
                string yearAndTime;
                string[] arrStrDate = strDate.Split('.');
                day = arrStrDate[0];
                month = arrStrDate[1];
                yearAndTime = arrStrDate[2];
                try
                {
                    DateTime test = Convert.ToDateTime("12/13/2010");
                    //chạy qua đây tức là định dạng MM/dd/yyyy
                    string strResult = month + "/" + day + "/" + yearAndTime;
                    result = Convert.ToDateTime(strResult);
                }
                catch
                {
                    //chạy vào đây tức là định dạng dd/MM/yyyy
                    string strResult = day + "/" + month + "/" + yearAndTime;
                    result = Convert.ToDateTime(strResult);
                }
            }
            if (strDate.Contains("-"))
            {
                result = Convert.ToDateTime(strDate);
            }

            return result;
        }
        public class WakeUpCallRequest
        {
            public string userID { get; set; }
            public string callType { get; set; }
            public DateTime singleDate { get; set; }
            public string timeDate { get; set; }
            public DateTime toDate { get; set; }
            public DateTime fromDate { get; set; }
            public string timeDaily { get; set; }
            public List<WakeUpCallRow> selectedRows { get; set; }
        }
        public class WakeUpCallRow
        {
            public string Id { get; set; }
            public string Room { get; set; }
            public string ConfirmNo { get; set; }
            public string Name { get; set; }
            public string ShareRoom { get; set; }
            public string ArrDate { get; set; }
            public string DepDate { get; set; }
            public string ReservationHolder { get; set; }
        }

        #endregion
        #region PostingToRoom
        public IActionResult PostingToRoom()
        {

            return PartialView();
        }
        [HttpPost]
        public IActionResult PostToRoom(DateTime fromDate, string roomNos, string userName, int userID)
        {



            try
            {
                string[] rooms = roomNos.Split(',');

                for (int i = 0; i < rooms.Length; i++)
                {
                    string RoomNo = rooms[i].Trim();

                    PostingRoomChargeWithBB(RoomNo, fromDate, userName, userID);
                    PostingFixedCharge(RoomNo, fromDate, userName, userID);
                    PostingPackage(RoomNo, fromDate, userName, userID);
                }

                return Json(new
                {
                    success = true,
                    message = "Posting Complete!"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        private void PostingPackage(string RoomNo, DateTime Date, string userName, int userID)
        {
            DataTable tbReservation = TextUtils.Select("Select * From Reservation Where (Status =1 or Status =6) And IsAdvanceBill = 0 And ReservationNo <> 0 And RoomType <>'XXX' And RoomNo = '" + RoomNo + "' ");
            if (tbReservation.Rows.Count > 0)
            {
                /* Xử lý trên từng đặt phòng */
                for (int k = 0; k < tbReservation.Rows.Count; k++)
                {
                    if (CheckAdvanceBill(int.Parse(tbReservation.Rows[k]["ID"].ToString()), Date) == false)
                    {
                        int ReservationID = int.Parse(tbReservation.Rows[k]["ID"].ToString());
                        int RoomTypeID = int.Parse(tbReservation.Rows[k]["RoomTypeID"].ToString());
                        string RoomType = tbReservation.Rows[k]["RoomType"].ToString();

                        /* Lấy danh sách các package của từng RSV */
                        DataTable tb = TextUtils.Select("Select Distinct PackageID From ReservationPackage Where datediff(day, BeginDate, '" + Date.ToString("yyyy/MM/dd") + "') >=0 And datediff(day, EndDate, '" + Date.ToString("yyyy/MM/dd") + "')<0 And ReservationID = " + TextUtils.ToInt(tbReservation.Rows[k]["ID"].ToString()) + " ");
                        for (int pk = 0; pk < tb.Rows.Count; pk++)
                        {
                            /* Kiểm tra xem Package không bao gồm trong tiền phòng thì mới chạy */
                            if ((TextUtils.Select("Select * From Package where ID =" + TextUtils.ToInt(tb.Rows[pk]["PackageID"].ToString()) + "").Rows.Count > 0))
                            {
                                if (bool.Parse(TextUtils.Select("Select * From Package where ID =" + TextUtils.ToInt(tb.Rows[pk]["PackageID"].ToString()) + "").Rows[0]["IncludedInRate"].ToString()) == false)
                                {
                                    /* Lấy danh sách các trong bảng ReservationPackage ứng với mỗi đặt phòng */
                                    DataTable tbReservationPackage = TextUtils.getTable2("spNightAuditPackage", "tbNightAuditPackage", new SqlParameter("ReservationID", TextUtils.ToInt(tbReservation.Rows[k]["ID"].ToString())), new SqlParameter("PackageID", TextUtils.ToInt(tb.Rows[pk]["PackageID"].ToString())), new SqlParameter("@RateDate", Date));
                                    if (tbReservationPackage.Rows.Count > 0)
                                    {
                                        #region Các biến khai báo dùng để insert vào dữ liệu
                                        int ProfitCenterID = 0; string ProfitCenterCode = ""; string ConfirmNo = "";
                                        int ProfileID = 0; int RoomID = 0;
                                        int FolioNo = 1; int PackageID = 0; string PackageCode = "";
                                        decimal TotalAmount = 0;
                                        decimal AmountMasterReturn = 0; string err = ""; int ProfileGroupID = 0;
                                        int pFolioID = 0; int count = tbReservationPackage.Rows.Count; string AccountName = "";
                                        int OriginRsvID = TextUtils.ToInt(tbReservation.Rows[k]["ID"].ToString());
                                        int OriginFolioID = GetFolioID(OriginRsvID);
                                        string lstTransactionCode = TextUtils.Select("Select KeyValue From ConfigSystem Where KeyName ='PackageDiscount'").Rows[0]["KeyValue"].ToString();
                                        string[] TransactionCode = new string[count];
                                        string[] Description = new string[count];
                                        string[] ArticleCode = new string[count];
                                        decimal[] Amount = new decimal[count];
                                        int[] Quantity = new int[count];
                                        string[] CurrencyID = new string[count];
                                        string[] Reffrence = new string[count];
                                        string[] Supplement = new string[count];
                                        bool[] PostingStatus = new bool[count];
                                        bool[] IncludeRate = new bool[count];
                                        bool[] TaxInclude = new bool[count];
                                        bool[] isTransactionPosting = new bool[count];

                                        ProfitCenterID = pProfitCenterID;
                                        ProfitCenterCode = pProfitCenterCode;
                                        ProfileGroupID = TextUtils.ToInt(tbReservation.Rows[k]["ProfileGroupID"].ToString());
                                        ConfirmNo = tbReservation.Rows[k]["ConfirmationNo"].ToString();
                                        ReservationID = TextUtils.ToInt(tbReservation.Rows[k]["ID"].ToString());
                                        RoomID = TextUtils.ToInt(tbReservation.Rows[k]["ID"].ToString());
                                        RoomNo = tbReservation.Rows[k]["RoomNo"].ToString();//Cái này phải lấy từ bảng Room ?
                                        PackageID = TextUtils.ToInt(tb.Rows[pk]["PackageID"].ToString());
                                        PackageModel mPkg = (PackageModel)PackageBO.Instance.FindByAttribute("ID", PackageID)[0];
                                        PackageCode = mPkg.TransCodeAlt; //Transaction Code để lưu dòng tổng
                                        string Desc = mPkg.TextInNightAudit;//"PKG";
                                        #endregion

                                        bool isPostingPackage = false;
                                        int dem = 0;
                                        for (int p = 0; p < tbReservationPackage.Rows.Count; p++)
                                        {
                                            if (GetPostingRhythm(tbReservationPackage.Rows[p]["PostingRhythmID"].ToString(), TextUtils.ToInt(tbReservationPackage.Rows[p]["ID"].ToString()), Date) == true)
                                            {
                                                //TransactionCode[dem] = tbReservationPackage.Rows[p]["TransactionCode"].ToString();

                                                //Lấy transactioncode đã setup để chuyển về các nhà hàng 
                                                TransactionCode[dem] = GetTransactionCodeBB(ReservationID.ToString(), tbReservationPackage.Rows[p]["TransactionCode"].ToString());

                                                Description[dem] = tbReservationPackage.Rows[p]["Description"].ToString();
                                                ArticleCode[dem] = tbReservationPackage.Rows[p]["ArticlesCode"].ToString();
                                                TaxInclude[dem] = bool.Parse(tbReservationPackage.Rows[p]["IsTaxInclude"].ToString());

                                                #region Tính số tiền theo các hình thức charge

                                                switch (tbReservationPackage.Rows[p]["CalculationRuleID"].ToString())
                                                {
                                                    case "1"://Per Person
                                                        {
                                                            if (TaxInclude[dem] == false)
                                                                Amount[dem] = TextUtils.ToDecimal(tbReservationPackage.Rows[p]["Amount"].ToString()) * (TextUtils.ToDecimal(tbReservation.Rows[k]["NoOfAdult"].ToString()) + TextUtils.ToDecimal(tbReservation.Rows[k]["NoOfChild"].ToString()) + TextUtils.ToDecimal(tbReservation.Rows[k]["NoOfChild1"].ToString()));
                                                            else
                                                                Amount[dem] = TextUtils.ToDecimal(tbReservationPackage.Rows[p]["AmountAfterTax"].ToString()) * (TextUtils.ToDecimal(tbReservation.Rows[k]["NoOfAdult"].ToString()) + TextUtils.ToDecimal(tbReservation.Rows[k]["NoOfChild"].ToString()) + TextUtils.ToDecimal(tbReservation.Rows[k]["NoOfChild1"].ToString()));
                                                            break;
                                                        }
                                                    case "2"://Per Adult
                                                        {
                                                            if (TaxInclude[dem] == false)
                                                                Amount[dem] = TextUtils.ToDecimal(tbReservationPackage.Rows[p]["Amount"].ToString()) * TextUtils.ToDecimal(tbReservation.Rows[k]["NoOfAdult"].ToString());
                                                            else
                                                                Amount[dem] = TextUtils.ToDecimal(tbReservationPackage.Rows[p]["AmountAfterTax"].ToString()) * TextUtils.ToDecimal(tbReservation.Rows[k]["NoOfAdult"].ToString());
                                                            break;
                                                        }

                                                    case "3"://Per Child
                                                        {
                                                            if (TaxInclude[dem] == false)
                                                                Amount[dem] = TextUtils.ToDecimal(tbReservationPackage.Rows[p]["Amount"].ToString()) * TextUtils.ToDecimal(tbReservation.Rows[k]["NoOfChild"].ToString());
                                                            else
                                                                Amount[dem] = TextUtils.ToDecimal(tbReservationPackage.Rows[p]["AmountAfterTax"].ToString()) * TextUtils.ToDecimal(tbReservation.Rows[k]["NoOfChild"].ToString());
                                                            break;
                                                        }
                                                    case "4"://Per Room
                                                        {
                                                            if (TaxInclude[dem] == false)
                                                                Amount[dem] = TextUtils.ToDecimal(tbReservationPackage.Rows[p]["Amount"].ToString());
                                                            else
                                                                Amount[dem] = TextUtils.ToDecimal(tbReservationPackage.Rows[p]["AmountAfterTax"].ToString());
                                                            break;
                                                        }
                                                    case "5"://Child 1 - extrabed
                                                        {
                                                            if (TaxInclude[dem] == false)
                                                                Amount[dem] = TextUtils.ToDecimal(tbReservationPackage.Rows[p]["Amount"].ToString()) * (TextUtils.ToDecimal(tbReservation.Rows[k]["NoOfChild"].ToString()));
                                                            else
                                                                Amount[dem] = TextUtils.ToDecimal(tbReservationPackage.Rows[p]["AmountAfterTax"].ToString()) * (TextUtils.ToDecimal(tbReservation.Rows[k]["NoOfChild"].ToString()));
                                                            break;
                                                        }
                                                    case "6"://Child 2  - Phụ thu 25$
                                                        {
                                                            if (TaxInclude[dem] == false)
                                                                Amount[dem] = TextUtils.ToDecimal(tbReservationPackage.Rows[p]["Amount"].ToString()) * (TextUtils.ToDecimal(tbReservation.Rows[k]["NoOfChild1"].ToString()));
                                                            else
                                                                Amount[dem] = TextUtils.ToDecimal(tbReservationPackage.Rows[p]["AmountAfterTax"].ToString()) * (TextUtils.ToDecimal(tbReservation.Rows[k]["NoOfChild1"].ToString()));
                                                            break;
                                                        }
                                                    case "7"://Child 3
                                                        {
                                                            if (TaxInclude[dem] == false)
                                                                Amount[dem] = TextUtils.ToDecimal(tbReservationPackage.Rows[p]["Amount"].ToString()) * (TextUtils.ToDecimal(tbReservation.Rows[k]["NoOfChild2"].ToString()));
                                                            else
                                                                Amount[dem] = TextUtils.ToDecimal(tbReservationPackage.Rows[p]["AmountAfterTax"].ToString()) * (TextUtils.ToDecimal(tbReservation.Rows[k]["NoOfChild2"].ToString()));
                                                            break;
                                                        }
                                                    case "8":// Adult + Child 1 (Tính Extrabed)
                                                        {
                                                            if (TaxInclude[dem] == false)
                                                                Amount[dem] = TextUtils.ToDecimal(tbReservationPackage.Rows[p]["Amount"].ToString()) * (TextUtils.ToDecimal(tbReservation.Rows[k]["NoOfAdult"].ToString()) + TextUtils.ToDecimal(tbReservation.Rows[k]["NoOfChild"].ToString()));
                                                            else
                                                                Amount[dem] = TextUtils.ToDecimal(tbReservationPackage.Rows[p]["AmountAfterTax"].ToString()) * (TextUtils.ToDecimal(tbReservation.Rows[k]["NoOfAdult"].ToString()) + TextUtils.ToDecimal(tbReservation.Rows[k]["NoOfChild"].ToString()));
                                                            break;
                                                        }
                                                    case "9"://Adult + C1 + C2 (Tính ExtraBed + Surcharge)
                                                        {
                                                            if (TaxInclude[dem] == false)
                                                                Amount[dem] = TextUtils.ToDecimal(tbReservationPackage.Rows[p]["Amount"].ToString()) * (TextUtils.ToDecimal(tbReservation.Rows[k]["NoOfAdult"].ToString()) + TextUtils.ToDecimal(tbReservation.Rows[k]["NoOfChild"].ToString()) + TextUtils.ToDecimal(tbReservation.Rows[k]["NoOfChild1"].ToString()));
                                                            else
                                                                Amount[dem] = TextUtils.ToDecimal(tbReservationPackage.Rows[p]["AmountAfterTax"].ToString()) * (TextUtils.ToDecimal(tbReservation.Rows[k]["NoOfAdult"].ToString()) + TextUtils.ToDecimal(tbReservation.Rows[k]["NoOfChild"].ToString()) + TextUtils.ToDecimal(tbReservation.Rows[k]["NoOfChild1"].ToString()));
                                                            break;
                                                        }
                                                    case "10"://Adult + C2 (Tính Surcharge)
                                                        {
                                                            if (TaxInclude[dem] == false)
                                                                Amount[dem] = TextUtils.ToDecimal(tbReservationPackage.Rows[p]["Amount"].ToString()) * (TextUtils.ToDecimal(tbReservation.Rows[k]["NoOfAdult"].ToString()) + TextUtils.ToDecimal(tbReservation.Rows[k]["NoOfChild1"].ToString()));
                                                            else
                                                                Amount[dem] = TextUtils.ToDecimal(tbReservationPackage.Rows[p]["AmountAfterTax"].ToString()) * (TextUtils.ToDecimal(tbReservation.Rows[k]["NoOfAdult"].ToString()) + TextUtils.ToDecimal(tbReservation.Rows[k]["NoOfChild1"].ToString()));
                                                            break;
                                                        }
                                                    case "11"://C1 + C2 (Extrabed + Surcharge)
                                                        {
                                                            if (TaxInclude[dem] == false)
                                                                Amount[dem] = TextUtils.ToDecimal(tbReservationPackage.Rows[p]["Amount"].ToString()) * (TextUtils.ToDecimal(tbReservation.Rows[k]["NoOfChild1"].ToString()) + TextUtils.ToDecimal(tbReservation.Rows[k]["NoOfChild2"].ToString()));
                                                            else
                                                                Amount[dem] = TextUtils.ToDecimal(tbReservationPackage.Rows[p]["AmountAfterTax"].ToString()) * (TextUtils.ToDecimal(tbReservation.Rows[k]["NoOfChild1"].ToString()) + TextUtils.ToDecimal(tbReservation.Rows[k]["NoOfChild2"].ToString()));
                                                            break;
                                                        }

                                                }
                                                #endregion

                                                TotalAmount = TotalAmount + Convert.ToDecimal(Amount[dem]);
                                                Quantity[dem] = TextUtils.ToInt(tbReservationPackage.Rows[p]["Quantity"].ToString());//Xem lai cho nay!!!
                                                CurrencyID[dem] = tbReservationPackage.Rows[p]["CurrencyID"].ToString();
                                                PostingStatus[dem] = true;// GetPostingRhythm(tbReservationPackage.Rows[p]["PostingRhythmID"].ToString(), TextUtils.ToInt(tbReservationPackage.Rows[p]["ID"].ToString()));
                                                IncludeRate[dem] = GetIncludeRate(TextUtils.ToInt(tbReservationPackage.Rows[p]["PackageID"].ToString()));
                                                if (tbReservation.Rows[k]["RateCode"].ToString() != "")
                                                    Reffrence[dem] = "R:" + RoomNo + "; D:" + Date.ToString("dd/MM") + "; C:" + ConfirmNo + "; R" + tbReservation.Rows[k]["RateCode"].ToString();
                                                else
                                                    Reffrence[dem] = "R:" + RoomNo + "; D:" + Date.ToString("dd/MM") + "; C:" + ConfirmNo;
                                                dem = dem + 1;
                                            }
                                        }

                                        /* Tính lại Amount khi có Discount */
                                        DataTable dis = TextUtils.Select("Select DiscountRate, DiscountAmount From ReservationRate Where ReservationID = " + ReservationID + " And datediff(day,RateDate,'" + Date.ToString("yyyy/MM/dd") + "')=0");
                                        if (dis.Rows.Count > 0)
                                        {
                                            if (Convert.ToInt32(dis.Rows[0]["DiscountRate"]) > 0 || Convert.ToInt32(dis.Rows[0]["DiscountAmount"]) > 0)
                                            {
                                                for (int r = 0; r < dem; r++)
                                                    if (lstTransactionCode.Contains(TransactionCode[r]))
                                                        Amount[r] = (Amount[r] - TotalAmount * TextUtils.ToDecimal(dis.Rows[0]["DiscountRate"].ToString()) / 100) - TextUtils.ToDecimal(dis.Rows[0]["DiscountAmount"].ToString());
                                            }

                                        }
                                        /* Kêt thúc tính Discount */

                                        #region Gọi hàm insert dữ liệu
                                        DateTime SystemDate = TextUtils.GetBusinessDate();
                                        /* Mở bảng Routing để lấy số ReservationID cần charge tiền */
                                        DataTable tbRoutingExits;

                                        #region Trường hợp có khai báo Routing nhưng không phải là Routing về MasterFolio
                                        tbRoutingExits = TextUtils.Select("Select * From Routing Where IsMasterFolio = 0 And ToReservationID = " + ReservationID + " ");
                                        if (tbRoutingExits.Rows.Count > 0)
                                        {
                                            for (int c = 0; c < tbRoutingExits.Rows.Count; c++)
                                            {
                                                //Lay so Folio cần charge tiền
                                                DataTable tbRSVFolio1 = TextUtils.Select("SELECT dbo.Folio.ID AS FolioID FROM dbo.Folio WHERE dbo.Folio.ReservationID = " + ReservationID + " And dbo.Folio.ConfirmationNo ='" + ConfirmNo + "'  And FolioNo= " + tbRoutingExits.Rows[c]["ToFolioNo"].ToString() + "");
                                                if (tbRSVFolio1.Rows.Count > 0)
                                                    pFolioID = TextUtils.ToInt(tbRSVFolio1.Rows[0]["FolioID"].ToString());
                                                FolioNo = TextUtils.ToInt(tbRoutingExits.Rows[c]["ToFolioNo"].ToString());
                                                ProfileID = TextUtils.ToInt(tbRoutingExits.Rows[c]["ProfileID"].ToString());
                                                AccountName = tbRoutingExits.Rows[c]["AccountName"].ToString();
                                                ReservationID = TextUtils.ToInt(tbRoutingExits.Rows[c]["ToReservationID"].ToString());
                                                int FolioID = GetFolioID(ReservationID, FolioNo, ConfirmNo);
                                                if (GetStatusFolio(FolioID) == false)
                                                {
                                                    //Kiem tra xem nhung transaction nao duoc routing?
                                                    for (int d = 0; d < count; d++)
                                                    {
                                                        //Kiem tra xem transaction nay co duoc routing ve folio ?
                                                        string[] arr = TextUtils.GetArrayTransaction(tbRoutingExits.Rows[c]["TransactionCodes"].ToString());
                                                        if (Array.IndexOf(arr, TransactionCode[d]) >= 0 && isPostingPackage == false)//Nếu có trong danh sách Trans được routing
                                                        {

                                                            PostingPackage(true, SystemDate, Date, ProfitCenterID, ProfitCenterCode, ConfirmNo, ReservationID, RoomID, OriginRsvID, OriginFolioID, ProfileID, AccountName, FolioNo, PackageID, PackageCode, TransactionCode, ArticleCode, Amount, TaxInclude, Quantity, CurrencyID, MasterCurrencyID, Reffrence, Supplement, ref AmountMasterReturn, ref err, Desc, RoomTypeID, RoomType, userID, userName);
                                                            isPostingPackage = true;
                                                        }

                                                    }
                                                }
                                            }

                                        }

                                        #endregion

                                        #region Trường hợp Routing đến MasterFolio - mới thêm
                                        DataTable tbRoutingMasterFolio = TextUtils.Select("Select * From Routing Where IsMasterFolio = 1 And ConfirmationNo ='" + ConfirmNo + "'");
                                        if (tbRoutingMasterFolio.Rows.Count > 0)
                                        {
                                            for (int s = 0; s < tbRoutingMasterFolio.Rows.Count; s++)
                                            {
                                                ReservationID = TextUtils.ToInt(tbReservation.Rows[k]["ID"].ToString());
                                                int Windows = -1;
                                                //Lay so Folio của Master Folio
                                                DataTable tbRSVFolio = TextUtils.Select("SELECT dbo.Folio.ID AS FolioID FROM dbo.Folio WHERE dbo.Folio.Status =0 And dbo.Folio.FolioNo =-1 And dbo.Folio.ConfirmationNo ='" + ConfirmNo + "'");
                                                if (tbRSVFolio.Rows.Count > 0)
                                                    pFolioID = TextUtils.ToInt(tbRSVFolio.Rows[0]["FolioID"].ToString());

                                                //Lấy ProfileID để tạo Folio
                                                ProfileID = TextUtils.ToInt(tbRoutingMasterFolio.Rows[s]["ProfileID"].ToString());
                                                AccountName = tbRoutingMasterFolio.Rows[s]["AccountName"].ToString();
                                                string[] arr = TextUtils.GetArrayTransaction(tbRoutingMasterFolio.Rows[s]["TransactionCodes"].ToString());
                                                bool isRounting = false;
                                                //Kiểm tra theo có routing không?
                                                for (int d = 0; d < count; d++)
                                                {
                                                    if (Array.IndexOf(arr, TransactionCode[d]) >= 0)
                                                    {
                                                        isRounting = true;
                                                    }
                                                }
                                                //if (Array.IndexOf(arr, TransactionCode[0]) >= 0) //Trường hợp có trong danh sách Transaction
                                                if (isRounting == true)
                                                {
                                                    PostingPackage(true, SystemDate, Date, ProfitCenterID, ProfitCenterCode, ConfirmNo, ReservationID, RoomID, OriginRsvID, OriginFolioID, ProfileID, AccountName, Windows, PackageID, PackageCode, TransactionCode, ArticleCode, Amount, TaxInclude, Quantity, CurrencyID, MasterCurrencyID, Reffrence, Supplement, ref AmountMasterReturn, ref err, Desc, RoomTypeID, RoomType, userID, userName);
                                                    isPostingPackage = true;
                                                }
                                            }
                                        }

                                        #endregion

                                        #region Không có trong Routing lấy Routing Default
                                        DataTable tbRoutingExits3 = TextUtils.Select("Select * From Routing Where ConfirmationNo = '" + ConfirmNo + "'");
                                        if (tbRoutingExits3.Rows.Count == 0 || isPostingPackage == false)
                                        {
                                            #region xử lý những đặt phòng đã checkin
                                            if (TextUtils.ToInt(tbReservation.Rows[k]["Status"].ToString()) == 1)
                                            {
                                                //Lấy số Folio, Window,... từ bảng Folio thông qua ReservationID
                                                DataTable tbRSVFolio3 = TextUtils.Select("SELECT dbo.Folio.ID AS FolioID FROM dbo.Folio WHERE dbo.Folio.ReservationID = " + ReservationID + " And dbo.Folio.ConfirmationNo ='" + ConfirmNo + "'");
                                                if (tbRSVFolio3.Rows.Count > 0)
                                                    pFolioID = TextUtils.ToInt(tbRSVFolio3.Rows[0]["FolioID"].ToString());
                                                else
                                                    pFolioID = 0;

                                                DataTable dtGetFromFolio = TextUtils.Select("Select * From Folio Where FolioNo = 1 And ReservationID = " + ReservationID);
                                                if (dtGetFromFolio.Rows.Count > 0)
                                                {
                                                    //Lấy ProfileID để tạo Folio
                                                    ProfileID = TextUtils.ToInt(dtGetFromFolio.Rows[0]["ProfileID"].ToString());
                                                    AccountName = dtGetFromFolio.Rows[0]["AccountName"].ToString();
                                                    //Trường hợp đã có Folio
                                                    PostingPackage(true, SystemDate, Date, ProfitCenterID, ProfitCenterCode, ConfirmNo, ReservationID, RoomID, OriginRsvID, OriginFolioID, ProfileID, AccountName, 1, PackageID, PackageCode, TransactionCode, ArticleCode, Amount, TaxInclude, Quantity, CurrencyID, MasterCurrencyID, Reffrence, Supplement, ref AmountMasterReturn, ref err, Desc, RoomTypeID, RoomType, 0, "");
                                                }
                                                else
                                                {
                                                    //Trường hợp chưa có, tạo mới Folio
                                                    CreateFolio(ReservationID, 1);
                                                    ProfileID = TextUtils.ToInt(tbReservation.Rows[k]["ProfileIndividualID"].ToString());
                                                    AccountName = tbReservation.Rows[k]["LastName"].ToString();
                                                    PostingPackage(true, SystemDate, Date, ProfitCenterID, ProfitCenterCode, ConfirmNo, ReservationID, RoomID, OriginRsvID, OriginFolioID, ProfileID, AccountName, 1, PackageID, PackageCode, TransactionCode, ArticleCode, Amount, TaxInclude, Quantity, CurrencyID, MasterCurrencyID, Reffrence, Supplement, ref AmountMasterReturn, ref err, Desc, RoomTypeID, RoomType, 0, "");
                                                }

                                            }
                                            #endregion
                                        }
                                        #endregion

                                        #endregion
                                    }
                                }
                            }
                        }
                    }
                }


            }

        }
        public int pProfitCenterID = 2;//Hotel
        public string pProfitCenterCode = "0";
        public string MasterCurrencyID = "VND";
        public const string _CURRENCY_1 = "VND";
        public const string _CURRENCY_2 = "USD";
        private void PostingRoomChargeWithBB(string RoomNo, DateTime Date, string userName, int userID)
        {
            DateTime SystemDate = TextUtils.GetBusinessDate();
            string RoomCharge_P = TextUtils.Select("Select KeyValue From ConfigSystem Where KeyName = 'RoomCharge_P'").Rows[0]["KeyValue"].ToString();//Code nay lay tu ConfigSystem; 
            string PackageModeCharge = TextUtils.Select("Select KeyValue From ConfigSystem Where KeyName = 'PackageIncludeType'").Rows[0]["KeyValue"].ToString();//Dùng để xác định chi lấy từ ReservationPackage hay PackageDetail 

            DataTable tbRsv = TextUtils.Select("Select * From Reservation Where RoomNo = '" + RoomNo + "' And (Status =1 or Status =6) And IsAdvanceBill = 0 And ReservationNo<>0 And RoomType <>'XXX' ");
            if (tbRsv.Rows.Count > 0)
            {
                //Xử lý trên từng đặt phòng
                for (int t = 0; t < tbRsv.Rows.Count; t++)
                {
                    if (CheckAdvanceBill(int.Parse(tbRsv.Rows[t]["ID"].ToString()), Date) == false)
                    {
                        int RoomTypeID = int.Parse(tbRsv.Rows[t]["RoomTypeID"].ToString());
                        int ReservationID = int.Parse(tbRsv.Rows[t]["ID"].ToString());
                        string RoomType = tbRsv.Rows[t]["RoomType"].ToString();
                        bool isPostingRoomCharge = false; //Kiểm tra trạng thái đã được Post hay chưa
                        DataTable tbNightAuditRoomCharge = TextUtils.getTable2("spNightAuditRoomCharge", "tbNightAuditRoomCharge", new SqlParameter("ReservationID", TextUtils.ToInt(tbRsv.Rows[t]["ID"].ToString())), new SqlParameter("@RateDate", Date));
                        if (tbNightAuditRoomCharge.Rows.Count > 0)
                        {
                            if (decimal.Parse(tbNightAuditRoomCharge.Rows[0]["Rate"].ToString()) > 0)
                            {
                                #region Khai báo các biến để lưu dữ liệu
                                int ProfitCenterID = 0;
                                string ProfitCenterCode = ""; string ConfirmNo = "";
                                int ProfileID = 0; int RoomID = 0;
                                int PackageID = 0; string PackageCode = "";
                                decimal AmountMasterReturn = 0; string err = ""; int ProfileGroupID = 0; int pFolioID = 0;
                                string Desc = "Room Charge - " + tbRsv.Rows[t]["RoomType"].ToString();
                                string sql = "";
                                DataTable tb = TextUtils.Select("Select ID, PackageID, TransactionCode, Description, Price, PriceAfterTax, IsTaxInclude, CalculationRuleID, PostingRhythmID From ReservationPackage Where ReservationID = " + TextUtils.ToInt(tbRsv.Rows[t]["ID"].ToString()) + " And Datediff(day,'" + Date.ToString("MM/dd/yyyy") + "',BeginDate) <=0 And Datediff(day,'" + Date.ToString("MM/dd/yyyy") + "',EndDate) >0 ");
                                DataTable detail = TextUtils.Select("Select * From PackageDetail where PackageID = " + _GetPackageID(int.Parse(tbRsv.Rows[t]["ID"].ToString())));
                                //int count = detail.Rows.Count + 1;
                                int count = tb.Rows.Count + 1;
                                string AccountName = "";
                                string PackackageBB = TextUtils.Select("Select KeyValue From ConfigSystem Where KeyName ='PackageBB'").Rows[0]["KeyValue"].ToString();
                                string PackackageBBChild = TextUtils.Select("Select KeyValue From ConfigSystem Where KeyName ='PackageBBChild'").Rows[0]["KeyValue"].ToString();
                                //string ChildSurChage = TextUtils.Select("Select KeyValue From ConfigSystem Where KeyName ='ChildSurChage'").Rows[0]["KeyValue"].ToString();
                                string[] TransactionCode = new string[count];
                                string[] Description = new string[count];
                                string[] ArticleCode = new string[count];
                                decimal[] Amount = new decimal[count];
                                int[] Quantity = new int[count];
                                string[] CurrencyID = new string[count];
                                string[] Reffrence = new string[count];
                                string[] Supplement = new string[count];
                                bool[] PostingStatus = new bool[count];
                                bool[] IncludeRate = new bool[count];
                                bool[] TaxInclude = new bool[count];
                                bool[] isTransactionPosting = new bool[count];
                                int OriginRsvID = TextUtils.ToInt(tbRsv.Rows[t]["ID"].ToString());
                                int OriginFolioID = GetFolioID(OriginRsvID);
                                string RoomCharge = tbNightAuditRoomCharge.Rows[0]["TransactionCode"].ToString();
                                #endregion

                                #region Gán giá trị cho các biến dùng chung
                                ProfitCenterID = pProfitCenterID;
                                ProfitCenterCode = pProfitCenterCode;
                                ProfileGroupID = TextUtils.ToInt(tbRsv.Rows[t]["ProfileGroupID"].ToString());
                                ConfirmNo = tbRsv.Rows[t]["ConfirmationNo"].ToString();
                                ReservationID = TextUtils.ToInt(tbRsv.Rows[t]["ID"].ToString());
                                RoomID = int.Parse(tbNightAuditRoomCharge.Rows[0]["RoomID"].ToString());
                                RoomNo = tbNightAuditRoomCharge.Rows[0]["RoomNo"].ToString();
                                PackageCode = RoomCharge_P;
                                #endregion

                                #region Gán các thông tin liên quan đế tiền phòng
                                decimal Rate = 0; decimal AmountRoom = 0;
                                Amount[0] = Rate;
                                TransactionCode[0] = RoomCharge;
                                if (TextUtils.Select("Select Description From Transactions Where Code = '" + RoomCharge + "'").Rows.Count > 0)
                                    Description[0] = "Room Charge - " + tbRsv.Rows[t]["RoomType"].ToString();
                                ArticleCode[0] = "";
                                Quantity[0] = 1;
                                CurrencyID[0] = tbRsv.Rows[t]["CurrencyID"].ToString();
                                if (tbRsv.Rows[t]["RateCode"].ToString() != "")
                                    Reffrence[0] = "R:" + RoomNo + "; D:" + Date.ToString("dd/MM") + "; C:" + ConfirmNo + "; R" + tbRsv.Rows[t]["RateCode"].ToString();
                                else
                                    Reffrence[0] = "R:" + RoomNo + "; D:" + Date.ToString("dd/MM") + "; C:" + ConfirmNo;
                                PostingStatus[0] = true;
                                IncludeRate[0] = true; //Kiem tra lai truong hop nay!!!
                                isTransactionPosting[0] = true;
                                TaxInclude[0] = bool.Parse(tbNightAuditRoomCharge.Rows[0]["IsTaxInclude"].ToString());
                                bool TaxIncludeRoomRate = TaxInclude[0];
                                #endregion

                                #region Gán các thông tin liên quan đế tiền Package, VAP, ChildSurCharge
                                if (PackageModeCharge == "0") //Lấy chi tiết từ ReservationPackage
                                {
                                    for (int pk = 0; pk < tb.Rows.Count; pk++)
                                    {
                                        //Kiểm tra xem Package có bao gồm trong tiền phòng thì mới post Package này
                                        DataTable dtpkg = TextUtils.Select("Select * From Package where ID =" + TextUtils.ToInt(tb.Rows[pk]["PackageID"].ToString()) + " And IncludedInRate = 1 ");
                                        if (dtpkg.Rows.Count > 0)
                                        {
                                            //Kiểm tra thời điểm charge
                                            if (GetPostingRhythm(tb.Rows[pk]["PostingRhythmID"].ToString(), TextUtils.ToInt(tb.Rows[pk]["ID"].ToString()), Date) == true)
                                            {
                                                #region Gán lại tên hiển thị của các gói package
                                                Desc = dtpkg.Rows[0]["TextInNightAudit"].ToString() + " - RT: " + tbRsv.Rows[t]["RoomType"].ToString();
                                                #endregion

                                                #region Lấy từ Reservation Package

                                                #region Lấy TransactionCode đã Config để xác định doanh thu chuyển về nhà hàng nào
                                                //if (PackackageBB.Contains(tb.Rows[pk]["TransactionCode"].ToString()))
                                                //    /* region Trường hợp là Package ăn sáng người lớn thì lấy từ bảng NightAuditBB chuyển về các nhà hàng tương ứng */
                                                //    GetRestaurantToPost(tbRsv.Rows[t]["ID"].ToString(), ref TransactionCode[pk + 1], ref TransactionCode[pk + 1]);
                                                //else if (PackackageBBChild.Contains(tb.Rows[pk]["TransactionCode"].ToString()))
                                                //    /* region Trường hợp là Package ăn sáng (Trẻ em) thì lấy từ bảng NightAuditBB chuyển về các nhà hàng tương ứng */
                                                //    GetRestaurantToPost(tbRsv.Rows[t]["ID"].ToString(), ref TransactionCode[pk + 1], ref TransactionCode[pk + 1]);
                                                //else if (GetChildSurCharge(tb.Rows[pk]["TransactionCode"].ToString()) == true) /* Dùng khi set up bao gồm trong tiền phòng */
                                                //    #region Trường hợp là tiền SurCharge trẻ em
                                                //    TransactionCode[pk + 1] = tb.Rows[pk]["TransactionCode"].ToString();
                                                //    #endregion
                                                //else
                                                //    #region Trường hợp là tiền vui chơi VAP
                                                //    TransactionCode[pk + 1] = tb.Rows[pk]["TransactionCode"].ToString();
                                                //    #endregion
                                                #endregion

                                                /* New ---*/
                                                TransactionCode[pk + 1] = GetTransactionCodeBB(tbRsv.Rows[t]["ID"].ToString(), tb.Rows[pk]["TransactionCode"].ToString());

                                                Description[pk + 1] = tb.Rows[pk]["Description"].ToString() + " - " + tbRsv.Rows[t]["RoomType"].ToString();
                                                ArticleCode[pk + 1] = "";
                                                Quantity[pk + 1] = 1;
                                                CurrencyID[pk + 1] = tbRsv.Rows[t]["CurrencyID"].ToString();
                                                Reffrence[pk + 1] = "R:" + RoomNo + "; D:" + Date.ToString("dd/MM") + "; C:" + ConfirmNo;
                                                PostingStatus[pk + 1] = true;
                                                IncludeRate[pk + 1] = true;
                                                isTransactionPosting[pk + 1] = true;
                                                TaxInclude[pk + 1] = bool.Parse(tb.Rows[pk]["IsTaxInclude"].ToString());

                                                #region Tính số tiền theo các hình thức charge --> sau khi chạy đúng thay bằng 1 hàm gọi từ bên ngoài

                                                switch (tb.Rows[pk]["CalculationRuleID"].ToString())
                                                {
                                                    case "1"://Per Person
                                                        {
                                                            if (TaxInclude[pk + 1] == false)
                                                                Amount[pk + 1] = TextUtils.ToDecimal(tb.Rows[pk]["Price"].ToString()) * (TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfAdult"].ToString()) + TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfChild"].ToString()) + TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfChild1"].ToString()));
                                                            else
                                                                Amount[pk + 1] = TextUtils.ToDecimal(tb.Rows[pk]["PriceAfterTax"].ToString()) * (TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfAdult"].ToString()) + TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfChild"].ToString()) + TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfChild1"].ToString()));
                                                            break;
                                                        }
                                                    case "2"://Per Adult
                                                        {
                                                            if (TaxInclude[pk + 1] == false)
                                                                Amount[pk + 1] = TextUtils.ToDecimal(tb.Rows[pk]["Price"].ToString()) * TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfAdult"].ToString());
                                                            else
                                                                Amount[pk + 1] = TextUtils.ToDecimal(tb.Rows[pk]["PriceAfterTax"].ToString()) * TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfAdult"].ToString());
                                                            break;
                                                        }

                                                    case "3"://Per Child
                                                        {
                                                            //Amount[pk + 1] = TextUtils.ToDecimal(tb.Rows[pk]["Price"].ToString()) * (TextUtils.ToDecimal(tbRsv.Rows[t]["NoOfChild"].ToString()) + TextUtils.ToDecimal(tbRsv.Rows[t]["NoOfChild1"].ToString()) + TextUtils.ToDecimal(tbRsv.Rows[t]["NoOfChild2"].ToString()));
                                                            if (TaxInclude[pk + 1] == false)
                                                                Amount[pk + 1] = TextUtils.ToDecimal(tb.Rows[pk]["Price"].ToString()) * TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfChild"].ToString());
                                                            else
                                                                Amount[pk + 1] = TextUtils.ToDecimal(tb.Rows[pk]["PriceAfterTax"].ToString()) * TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfChild"].ToString());
                                                            break;
                                                        }
                                                    case "4"://Per Room
                                                        {
                                                            if (TaxInclude[pk + 1] == false)
                                                                Amount[pk + 1] = TextUtils.ToDecimal(tb.Rows[pk]["Price"].ToString());
                                                            else
                                                                Amount[pk + 1] = TextUtils.ToDecimal(tb.Rows[pk]["PriceAfterTax"].ToString());
                                                            break;
                                                        }
                                                    case "5"://Child 1 - extrabed
                                                        {
                                                            if (TaxInclude[pk + 1] == false)
                                                                Amount[pk + 1] = TextUtils.ToDecimal(tb.Rows[pk]["Price"].ToString()) * TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfChild"].ToString());
                                                            else
                                                                Amount[pk + 1] = TextUtils.ToDecimal(tb.Rows[pk]["PriceAfterTax"].ToString()) * TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfChild"].ToString());
                                                            break;
                                                        }
                                                    case "6"://Child 2  - Phụ thu 25$
                                                        {
                                                            if (TaxInclude[pk + 1] == false)
                                                                Amount[pk + 1] = TextUtils.ToDecimal(tb.Rows[pk]["Price"].ToString()) * TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfChild1"].ToString());
                                                            else
                                                                Amount[pk + 1] = TextUtils.ToDecimal(tb.Rows[pk]["PriceAfterTax"].ToString()) * TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfChild1"].ToString());
                                                            break;
                                                        }
                                                    case "7"://Child 3
                                                        {
                                                            if (TaxInclude[pk + 1] == false)
                                                                Amount[pk + 1] = TextUtils.ToDecimal(tb.Rows[pk]["Price"].ToString()) * TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfChild2"].ToString());
                                                            else
                                                                Amount[pk + 1] = TextUtils.ToDecimal(tb.Rows[pk]["PriceAfterTax"].ToString()) * TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfChild2"].ToString());
                                                            break;
                                                        }
                                                    case "8":// Adult + Child 1 (Tính Extrabed)
                                                        {
                                                            if (TaxInclude[pk + 1] == false)
                                                                Amount[pk + 1] = TextUtils.ToDecimal(tb.Rows[pk]["Price"].ToString()) * (TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfAdult"].ToString()) + TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfChild"].ToString()));
                                                            else
                                                                Amount[pk + 1] = TextUtils.ToDecimal(tb.Rows[pk]["PriceAfterTax"].ToString()) * (TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfAdult"].ToString()) + TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfChild"].ToString()));
                                                            break;
                                                        }
                                                    case "9"://Adult + C1 + C2 (Tính ExtraBed + Surcharge)
                                                        {
                                                            if (TaxInclude[pk + 1] == false)
                                                                Amount[pk + 1] = TextUtils.ToDecimal(tb.Rows[pk]["Price"].ToString()) * (TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfAdult"].ToString()) + TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfChild"].ToString()) + TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfChild1"].ToString()));
                                                            else
                                                                Amount[pk + 1] = TextUtils.ToDecimal(tb.Rows[pk]["PriceAfterTax"].ToString()) * (TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfAdult"].ToString()) + TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfChild"].ToString()) + TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfChild1"].ToString()));
                                                            break;
                                                        }
                                                    case "10"://Adult + C2 (Tính Surcharge)
                                                        {
                                                            if (TaxInclude[pk + 1] == false)
                                                                Amount[pk + 1] = TextUtils.ToDecimal(tb.Rows[pk]["Price"].ToString()) * (TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfAdult"].ToString()) + TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfChild1"].ToString()));
                                                            else
                                                                Amount[pk + 1] = TextUtils.ToDecimal(tb.Rows[pk]["PriceAfterTax"].ToString()) * (TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfAdult"].ToString()) + TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfChild1"].ToString()));
                                                            break;
                                                        }
                                                    case "11"://C1 + C2 (Extrabed + Surcharge)
                                                        {
                                                            if (TaxInclude[pk + 1] == false)
                                                                Amount[pk + 1] = TextUtils.ToDecimal(tb.Rows[pk]["Price"].ToString()) * (TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfChild"].ToString()) + TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfChild1"].ToString()));
                                                            else
                                                                Amount[pk + 1] = TextUtils.ToDecimal(tb.Rows[pk]["PriceAfterTax"].ToString()) * (TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfChild"].ToString()) + TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfChild1"].ToString()));
                                                            break;
                                                        }

                                                }
                                                #endregion

                                                #region Gán lại tiền phòng sau khi đã trừ các khoản nằm trong tiền phòng ???

                                                if (TaxInclude[pk + 1] == false)
                                                {
                                                    //AmountRoom = AmountRoom + Amount[pk + 1] * 1155 / 1000; //Thay bang cac gia tri lay trong bang config

                                                    decimal d1 = 0, d2 = 0;
                                                    GetAmountSource(tb.Rows[pk]["TransactionCode"].ToString(), Amount[pk + 1], false, ref d1, ref d2);
                                                    AmountRoom = AmountRoom + d2;
                                                }
                                                else
                                                {
                                                    AmountRoom = AmountRoom + Amount[pk + 1];
                                                }

                                                #endregion

                                                #endregion
                                            }
                                        }
                                    }
                                }
                                else //Lấy chi tiết từ bảng PackageDetail
                                {
                                    #region Lấy từ Package Detail
                                    //for (int pk = 0; pk < tb.Rows.Count; pk++)
                                    if (tb.Rows.Count > 0)
                                    {
                                        DataTable dtcheck = TextUtils.Select("Select * From Package where ID =" + TextUtils.ToInt(tb.Rows[0]["PackageID"].ToString()) + " And IncludedInRate = 1 ");
                                        if (dtcheck.Rows.Count > 0)
                                        {
                                            DataTable dt_detail = TextUtils.Select("Select * From PackageDetail where PackageID = " + _GetPackageID(int.Parse(tbRsv.Rows[t]["ID"].ToString())));
                                            if (dt_detail.Rows.Count > 0)
                                            {
                                                for (int d = 0; d < dt_detail.Rows.Count; d++)
                                                {
                                                    //Kiểm tra thời điểm charge
                                                    if (GetPostingRhythmPackage(dt_detail.Rows[d]["RhythmPostingID"].ToString(), TextUtils.ToInt(dt_detail.Rows[d]["ID"].ToString()), Date) == true)
                                                    {
                                                        string tmp = "";
                                                        #region Gán lại tên hiển thị của các gói package
                                                        Desc = dtcheck.Rows[0]["TextInNightAudit"].ToString() + " - RT: " + tbRsv.Rows[t]["RoomType"].ToString();
                                                        #endregion

                                                        #region Trường hợp là tiền vui chơi VAP
                                                        TransactionCode[d + 1] = dt_detail.Rows[d]["TransCode"].ToString();
                                                        #endregion

                                                        /* New ---*/
                                                        TransactionCode[d + 1] = GetTransactionCodeBB(tbRsv.Rows[t]["ID"].ToString(), dt_detail.Rows[d]["TransCode"].ToString());

                                                        Description[d + 1] = dt_detail.Rows[d]["Description"].ToString() + " - " + tbRsv.Rows[t]["RoomType"].ToString();
                                                        ArticleCode[d + 1] = "";
                                                        Quantity[d + 1] = 1;
                                                        CurrencyID[d + 1] = tbRsv.Rows[t]["CurrencyID"].ToString();
                                                        Reffrence[d + 1] = "R:" + RoomNo + "; D:" + Date.ToString("dd/MM") + "; C:" + ConfirmNo;
                                                        PostingStatus[d + 1] = true;
                                                        IncludeRate[d + 1] = true;
                                                        isTransactionPosting[d + 1] = true;
                                                        TaxInclude[d + 1] = bool.Parse(dt_detail.Rows[d]["IsTaxInclude"].ToString());

                                                        #region Tính số tiền theo các hình thức charge

                                                        switch (dt_detail.Rows[d]["CalculationRuleID"].ToString())
                                                        {
                                                            case "1"://Per Person
                                                                {
                                                                    if (TaxInclude[d + 1] == false)
                                                                        Amount[d + 1] = TextUtils.ToDecimal(dt_detail.Rows[d]["Price"].ToString()) * (TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfAdult"].ToString()) + TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfChild"].ToString()) + TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfChild1"].ToString()));
                                                                    else
                                                                        Amount[d + 1] = TextUtils.ToDecimal(dt_detail.Rows[d]["PriceAfterTax"].ToString()) * (TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfAdult"].ToString()) + TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfChild"].ToString()) + TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfChild1"].ToString()));
                                                                    break;
                                                                }
                                                            case "2"://Per Adult
                                                                {
                                                                    if (TaxInclude[d + 1] == false)
                                                                        Amount[d + 1] = TextUtils.ToDecimal(dt_detail.Rows[d]["Price"].ToString()) * TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfAdult"].ToString());
                                                                    else
                                                                        Amount[d + 1] = TextUtils.ToDecimal(dt_detail.Rows[d]["PriceAfterTax"].ToString()) * TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfAdult"].ToString());
                                                                    break;
                                                                }

                                                            case "3"://Per Child
                                                                {
                                                                    //Amount[pk + 1] = TextUtils.ToDecimal(tb.Rows[pk]["Price"].ToString()) * (TextUtils.ToDecimal(tbRsv.Rows[t]["NoOfChild"].ToString()) + TextUtils.ToDecimal(tbRsv.Rows[t]["NoOfChild1"].ToString()) + TextUtils.ToDecimal(tbRsv.Rows[t]["NoOfChild2"].ToString()));
                                                                    if (TaxInclude[d + 1] == false)
                                                                        Amount[d + 1] = TextUtils.ToDecimal(dt_detail.Rows[d]["Price"].ToString()) * TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfChild"].ToString());
                                                                    else
                                                                        Amount[d + 1] = TextUtils.ToDecimal(dt_detail.Rows[d]["PriceAfterTax"].ToString()) * TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfChild"].ToString());
                                                                    break;
                                                                }
                                                            case "4"://Per Room
                                                                {
                                                                    if (TaxInclude[d + 1] == false)
                                                                        Amount[d + 1] = TextUtils.ToDecimal(dt_detail.Rows[d]["Price"].ToString());
                                                                    else
                                                                        Amount[d + 1] = TextUtils.ToDecimal(dt_detail.Rows[d]["PriceAfterTax"].ToString());
                                                                    break;
                                                                }
                                                            case "5"://Child 1 - extrabed
                                                                {
                                                                    if (TaxInclude[d + 1] == false)
                                                                        Amount[d + 1] = TextUtils.ToDecimal(dt_detail.Rows[d]["Price"].ToString()) * TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfChild"].ToString());
                                                                    else
                                                                        Amount[d + 1] = TextUtils.ToDecimal(dt_detail.Rows[d]["PriceAfterTax"].ToString()) * TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfChild"].ToString());
                                                                    break;
                                                                }
                                                            case "6"://Child 2  - Phụ thu 25$
                                                                {
                                                                    if (TaxInclude[d + 1] == false)
                                                                        Amount[d + 1] = TextUtils.ToDecimal(dt_detail.Rows[d]["Price"].ToString()) * TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfChild1"].ToString());
                                                                    else
                                                                        Amount[d + 1] = TextUtils.ToDecimal(dt_detail.Rows[d]["PriceAfterTax"].ToString()) * TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfChild1"].ToString());
                                                                    break;
                                                                }
                                                            case "7"://Child 3
                                                                {
                                                                    if (TaxInclude[d + 1] == false)
                                                                        Amount[d + 1] = TextUtils.ToDecimal(dt_detail.Rows[d]["Price"].ToString()) * TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfChild2"].ToString());
                                                                    else
                                                                        Amount[d + 1] = TextUtils.ToDecimal(dt_detail.Rows[d]["PriceAfterTax"].ToString()) * TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfChild2"].ToString());
                                                                    break;
                                                                }
                                                            case "8":// Adult + Child 1 (Tính Extrabed)
                                                                {
                                                                    if (TaxInclude[d + 1] == false)
                                                                        Amount[d + 1] = TextUtils.ToDecimal(dt_detail.Rows[d]["Price"].ToString()) * (TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfAdult"].ToString()) + TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfChild"].ToString()));
                                                                    else
                                                                        Amount[d + 1] = TextUtils.ToDecimal(dt_detail.Rows[d]["PriceAfterTax"].ToString()) * (TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfAdult"].ToString()) + TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfChild"].ToString()));
                                                                    break;
                                                                }
                                                            case "9"://Adult + C1 + C2 (Tính ExtraBed + Surcharge)
                                                                {
                                                                    if (TaxInclude[d + 1] == false)
                                                                        Amount[d + 1] = TextUtils.ToDecimal(dt_detail.Rows[d]["Price"].ToString()) * (TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfAdult"].ToString()) + TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfChild"].ToString()) + TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfChild1"].ToString()));
                                                                    else
                                                                        Amount[d + 1] = TextUtils.ToDecimal(dt_detail.Rows[d]["PriceAfterTax"].ToString()) * (TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfAdult"].ToString()) + TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfChild"].ToString()) + TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfChild1"].ToString()));
                                                                    break;
                                                                }
                                                            case "10"://Adult + C2 (Tính Surcharge)
                                                                {
                                                                    if (TaxInclude[d + 1] == false)
                                                                        Amount[d + 1] = TextUtils.ToDecimal(dt_detail.Rows[d]["Price"].ToString()) * (TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfAdult"].ToString()) + TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfChild1"].ToString()));
                                                                    else
                                                                        Amount[d + 1] = TextUtils.ToDecimal(dt_detail.Rows[d]["PriceAfterTax"].ToString()) * (TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfAdult"].ToString()) + TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfChild1"].ToString()));
                                                                    break;
                                                                }
                                                            case "11"://C1 + C2 (Extrabed + Surcharge)
                                                                {
                                                                    if (TaxInclude[d + 1] == false)
                                                                        Amount[d + 1] = TextUtils.ToDecimal(dt_detail.Rows[d]["Price"].ToString()) * (TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfChild"].ToString()) + TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfChild1"].ToString()));
                                                                    else
                                                                        Amount[d + 1] = TextUtils.ToDecimal(dt_detail.Rows[d]["PriceAfterTax"].ToString()) * (TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfChild"].ToString()) + TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["NoOfChild1"].ToString()));
                                                                    break;
                                                                }

                                                        }
                                                        #endregion

                                                        #region Gán lại tiền phòng sau khi đã trừ các khoản nằm trong tiền phòng ???

                                                        if (TaxInclude[d + 1] == false)
                                                        {
                                                            //AmountRoom = AmountRoom + Amount[d + 1] * 1155 / 1000; //Đổi lại bằng các biến sau khi bug dữ liệu đúng

                                                            decimal d1 = 0, d2 = 0;
                                                            GetAmountSource(dt_detail.Rows[d]["TransCode"].ToString(), Amount[d + 1], false, ref d1, ref d2);
                                                            AmountRoom = AmountRoom + d2;
                                                        }
                                                        else
                                                        {
                                                            AmountRoom = AmountRoom + Amount[d + 1];

                                                        }

                                                        #endregion
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    #endregion
                                }

                                #region Tính tiền chiết khấu theo từng RSV --> Số tiền chiết khấu được xác định trên giá trị còn lại sau khi trừ đi package ăn sáng
                                decimal DiscountRate = TextUtils.ToDecimal(tbNightAuditRoomCharge.Rows[0]["DiscountRate"].ToString());
                                decimal DiscountAmount = TextUtils.ToDecimal(tbNightAuditRoomCharge.Rows[0]["DiscountAmount"].ToString());

                                //Tính tiền phòng còn lại sau khi đã trừ đi các khoản bao gồm trong tiền phòng
                                if (TaxIncludeRoomRate == false)
                                {
                                    decimal d1 = 0, d2 = 0;
                                    GetAmountSource(tbNightAuditRoomCharge.Rows[0]["TransactionCode"].ToString(),
                                       TextUtils.ToDecimal(Convert.ToString(tbNightAuditRoomCharge.Rows[0]["Rate"])),
                                        false, ref d1, ref d2);
                                    Amount[0] = d2 - AmountRoom;
                                    Amount[0] = Amount[0] - (d2 * DiscountRate / 100) - DiscountAmount;

                                    //Amount[0] = TextUtils.ToDecimal(tbNightAuditRoomCharge.Rows[0]["Rate"].ToString()) * 1155 / 1000 - AmountRoom;
                                    //Amount[0] = (Amount[0] - TextUtils.ToDecimal(tbNightAuditRoomCharge.Rows[0]["Rate"].ToString()) * 1155 / 1000 * DiscountRate / 100) - DiscountAmount;

                                    TaxInclude[0] = true;
                                    Rate = Amount[0];
                                }
                                else
                                {
                                    Amount[0] = TextUtils.ToDecimal(tbNightAuditRoomCharge.Rows[0]["RateAfterTax"].ToString()) - AmountRoom;
                                    Amount[0] = (Amount[0] - TextUtils.ToDecimal(tbNightAuditRoomCharge.Rows[0]["RateAfterTax"].ToString()) * DiscountRate / 100) - DiscountAmount;
                                    TaxInclude[0] = true;
                                    Rate = Amount[0];
                                }

                                #endregion


                                #endregion

                                #region Trường hợp có routing thông thường
                                DataTable tbRouting = TextUtils.Select("Select * From Routing Where IsMasterFolio = 0 And isDefault = 0 And FromReservationID = " + TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["ID"].ToString()) + "");
                                if (tbRouting.Rows.Count > 0)
                                {
                                    #region Trường hợp có trong Routing
                                    for (int r = 0; r < tbRouting.Rows.Count; r++)
                                    {
                                        if (Rate > 0)
                                        {
                                            int Windows = TextUtils.ToInt(tbRouting.Rows[r]["ToFolioNo"].ToString());
                                            ProfileID = TextUtils.ToInt(tbRouting.Rows[r]["ProfileID"].ToString());
                                            AccountName = tbRouting.Rows[r]["AccountName"].ToString();
                                            int FolioID = GetFolioID(ReservationID, Windows, ConfirmNo);
                                            /*Kiểm tra xem Folio còn mở không để posting Tiền*/
                                            if (GetStatusFolio(FolioID) == false)
                                            {
                                                string[] arr = TextUtils.GetArrayTransaction(tbRouting.Rows[r]["TransactionCodes"].ToString());
                                                if (Array.IndexOf(arr, RoomCharge) >= 0 && isPostingRoomCharge == false) //Nếu có trong danh sách Trans được routing
                                                {
                                                    PostingPackage(true, SystemDate, Date, ProfitCenterID, ProfitCenterCode, ConfirmNo, ReservationID, RoomID, OriginRsvID, OriginFolioID, ProfileID, AccountName, Windows, PackageID, PackageCode, TransactionCode, ArticleCode, Amount, TaxInclude, Quantity, CurrencyID, MasterCurrencyID, Reffrence, Supplement, ref AmountMasterReturn, ref err, Desc, RoomTypeID, RoomType, userID, userName);
                                                    isPostingRoomCharge = true;
                                                }
                                            }
                                            break;
                                        }
                                    }
                                    #endregion
                                }

                                #endregion

                                #region Trường hợp Routing đến MasterFolio
                                pFolioID = 0;
                                DataTable tbRoutingMasterFolio = TextUtils.Select("Select * From Routing Where IsMasterFolio = 1 And ConfirmationNo = '" + tbRsv.Rows[t]["ConfirmationNo"].ToString() + "'");
                                if (tbRoutingMasterFolio.Rows.Count > 0)
                                    for (int r = 0; r < tbRoutingMasterFolio.Rows.Count; r++)
                                    {
                                        if (Rate > 0 && TextUtils.ToInt(tbRsv.Rows[t]["Status"].ToString()) == 1)
                                        {
                                            DataTable tbRSVFolio = TextUtils.Select("SELECT dbo.Folio.ID AS FolioID FROM dbo.Folio WHERE dbo.Folio.Status =0 And dbo.Folio.FolioNo =-1 And dbo.Folio.ConfirmationNo ='" + tbRsv.Rows[t]["ConfirmationNo"].ToString() + "'");
                                            if (tbRSVFolio.Rows.Count > 0)
                                                pFolioID = TextUtils.ToInt(tbRSVFolio.Rows[0]["FolioID"].ToString());
                                            else
                                                ProfileID = TextUtils.ToInt(tbRoutingMasterFolio.Rows[r]["ProfileID"].ToString());
                                            AccountName = tbRoutingMasterFolio.Rows[r]["AccountName"].ToString();
                                            string[] arr = TextUtils.GetArrayTransaction(tbRoutingMasterFolio.Rows[r]["TransactionCodes"].ToString());

                                            #region Trường Transaction Code có trong danh sách được routing
                                            if (Array.IndexOf(arr, RoomCharge) >= 0 && isPostingRoomCharge == false)
                                            {
                                                PostingPackage(true, SystemDate, Date, ProfitCenterID, ProfitCenterCode, ConfirmNo, ReservationID, RoomID, OriginRsvID, OriginFolioID, ProfileID, AccountName, -1, PackageID, PackageCode, TransactionCode, ArticleCode, Amount, TaxInclude, Quantity, CurrencyID, MasterCurrencyID, Reffrence, Supplement, ref AmountMasterReturn, ref err, Desc, RoomTypeID, RoomType, userID, userName);
                                                isPostingRoomCharge = true;
                                                break;
                                            }
                                            #endregion
                                        }
                                    }

                                #endregion

                                #region Trường hợp Posting vào cửa sổ Default window =1
                                //Không có trong Routing thì Routing về cửa sổ windows =1 của chính nó
                                DataTable tbRoutingExits = TextUtils.Select("Select * From Routing Where ConfirmationNo = '" + ConfirmNo + "'");
                                if (tbRoutingExits.Rows.Count == 0 || isPostingRoomCharge == false)
                                {
                                    if (Rate != 0 && TextUtils.ToInt(tbRsv.Rows[t]["Status"].ToString()) == 1)
                                    {
                                        //Lấy số Folio, Window từ bảng Folio
                                        DataTable dtGetFromFolio = TextUtils.Select("Select * From Folio Where FolioNo = 1 And ReservationID = " + TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["ID"].ToString()));
                                        if (dtGetFromFolio.Rows.Count > 0)
                                        {
                                            ProfileID = TextUtils.ToInt(dtGetFromFolio.Rows[0]["ProfileID"].ToString());
                                            AccountName = dtGetFromFolio.Rows[0]["AccountName"].ToString();
                                            PostingPackage(true, SystemDate, Date, ProfitCenterID, ProfitCenterCode, ConfirmNo, ReservationID, RoomID, OriginRsvID, OriginFolioID, ProfileID, AccountName, 1, PackageID, PackageCode, TransactionCode, ArticleCode, Amount, TaxInclude, Quantity, CurrencyID, MasterCurrencyID, Reffrence, Supplement, ref AmountMasterReturn, ref err, Desc, RoomTypeID, RoomType, userID, userName);
                                        }
                                        else
                                        {
                                            /* Nếu không có post vào folio mặc định */
                                            CreateFolio(TextUtils.ToInt(tbNightAuditRoomCharge.Rows[0]["ID"].ToString()), 1);
                                            ProfileID = TextUtils.ToInt(tbRsv.Rows[t]["ProfileIndividualID"].ToString());
                                            AccountName = tbRsv.Rows[t]["LastName"].ToString();
                                            PostingPackage(true, SystemDate, Date, ProfitCenterID, ProfitCenterCode, ConfirmNo, ReservationID, RoomID, OriginRsvID, OriginFolioID, ProfileID, AccountName, 1, PackageID, PackageCode, TransactionCode, ArticleCode, Amount, TaxInclude, Quantity, CurrencyID, MasterCurrencyID, Reffrence, Supplement, ref AmountMasterReturn, ref err, Desc, RoomTypeID, RoomType, userID, userName);
                                        }
                                    }
                                }
                                #endregion
                            }
                        }
                    }
                }
            }




        }
        public bool PostingPackage(bool AutoPosting, DateTime _SysDate, DateTime _BusinessDate, int _ProID, string _ProCode, string _ConfirmNo, int _RsvID, int _RoomID, int _OriginRsvID, int _OriginFolioID,
                                     int _ProfileID, string _AccountName, int _Win, int _PkgID, string _PkgCode, string[] _TransCode, string[] _ArCode,
                                     decimal[] _Amount, bool[] _TaxInclude, int[] _Quan, string[] _CurrencyID, string _CurrencyLocal,
                                     string[] _Ref, string[] _Supp, ref decimal _AmountLocalReturn, ref string _Message, string Description, int RoomTypeID, string RoomType, int userID, string userName)
        {
            ProcessTransactions pt = new ProcessTransactions();

            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();

                #region Gán giá trị cho ngày hệ thống
                //_BusinessDate  = TextUtils.GetBusinessDateTime();
                #endregion


                #region Lấy ra số FolioID
                int _RsvID_Return = 0;
                int FolioID = GetOrCreateFolioID(_SysDate, _BusinessDate, _ConfirmNo, _RsvID, _Win, _ProfileID, _AccountName, ref _RsvID_Return, ref _Message, userID);
                _RsvID = _RsvID_Return;

                #endregion

                if (FolioID > 0)
                {
                    #region Khai báo Model

                    FolioDetailModel mFD_Group = new FolioDetailModel();
                    FolioDetailModel mFD_Subgroup = new FolioDetailModel();
                    FolioDetailModel mFD_Detail = new FolioDetailModel();
                    decimal Rate = 0;

                    #endregion

                    #region Gán thông tin của các biến static

                    #region Group
                    mFD_Group.ProfitCenterID = _ProID;
                    mFD_Group.ProfitCenterCode = _ProCode;
                    mFD_Group.Status = false;

                    mFD_Group.CurrencyID = _CurrencyLocal;
                    mFD_Group.CurrencyMaster = _CurrencyLocal;

                    mFD_Group.ReservationID = _RsvID;
                    mFD_Group.OriginReservationID = _OriginRsvID;

                    mFD_Group.RoomID = _RoomID;

                    mFD_Group.FolioID = FolioID;
                    mFD_Group.OriginFolioID = _OriginFolioID;

                    mFD_Group.TransactionDate = _BusinessDate;
                    mFD_Group.PackageID = _PkgID;

                    mFD_Group.UserID = userID;
                    mFD_Group.UserName = userName;
                    mFD_Group.CashierNo = userName;
                    mFD_Group.ShiftID = userID;

                    mFD_Group.UserInsertID = userID;
                    mFD_Group.UserUpdateID = userID;
                    mFD_Group.CreateDate = _SysDate;
                    mFD_Group.UpdateDate = _SysDate;

                    if (AutoPosting == true)
                    {
                        mFD_Group.UserID = 0;
                        mFD_Group.UserName = "$$";
                        mFD_Group.CashierNo = "";
                        mFD_Group.ShiftID = 0;
                    }
                    else
                    {
                        mFD_Group.UserID = userID;
                        mFD_Group.UserName = userName;
                        mFD_Group.CashierNo = userName;
                        mFD_Group.ShiftID = userID;
                    }

                    #endregion

                    #region Subgroup
                    mFD_Subgroup.ProfitCenterID = _ProID;
                    mFD_Subgroup.ProfitCenterCode = _ProCode;
                    mFD_Subgroup.Status = false;

                    mFD_Subgroup.CurrencyMaster = _CurrencyLocal;

                    mFD_Subgroup.ReservationID = _RsvID;
                    mFD_Subgroup.OriginReservationID = _OriginRsvID;

                    mFD_Subgroup.RoomID = _RoomID;

                    mFD_Subgroup.FolioID = FolioID;
                    mFD_Subgroup.OriginFolioID = _OriginFolioID;

                    mFD_Subgroup.TransactionDate = _BusinessDate;
                    mFD_Subgroup.PackageID = _PkgID;

                    mFD_Subgroup.UserID = userID;
                    mFD_Subgroup.UserName = userName;
                    mFD_Subgroup.CashierNo = userName;
                    mFD_Subgroup.ShiftID = userID;

                    mFD_Subgroup.UserInsertID = userID;
                    mFD_Subgroup.UserUpdateID = userID;
                    mFD_Subgroup.CreateDate = _SysDate;
                    mFD_Subgroup.UpdateDate = _SysDate;
                    if (AutoPosting == true)
                    {
                        mFD_Subgroup.UserID = 0;
                        mFD_Subgroup.UserName = "$$";
                        mFD_Subgroup.CashierNo = "";
                        mFD_Subgroup.ShiftID = 0;
                    }
                    else
                    {
                        mFD_Subgroup.UserID = userID;
                        mFD_Subgroup.UserName = userName;
                        mFD_Subgroup.CashierNo = userName;
                        mFD_Subgroup.ShiftID = userID;
                    }
                    #endregion

                    #region Detail
                    mFD_Detail.ProfitCenterID = _ProID;
                    mFD_Detail.ProfitCenterCode = _ProCode;
                    mFD_Detail.Status = false;
                    mFD_Detail.CurrencyMaster = _CurrencyLocal;

                    mFD_Detail.ReservationID = _RsvID;
                    mFD_Detail.OriginReservationID = _OriginRsvID;

                    mFD_Detail.RoomID = _RoomID;

                    mFD_Detail.FolioID = FolioID;
                    mFD_Detail.OriginFolioID = _OriginFolioID;

                    mFD_Detail.TransactionDate = _BusinessDate;
                    mFD_Detail.PackageID = _PkgID;

                    mFD_Detail.UserID = userID;
                    mFD_Detail.UserName = userName;
                    mFD_Detail.CashierNo = userName;
                    mFD_Detail.ShiftID = userID;

                    mFD_Detail.UserInsertID = userID;
                    mFD_Detail.UserUpdateID = userID;
                    mFD_Detail.CreateDate = _SysDate;
                    mFD_Detail.UpdateDate = _SysDate;

                    if (AutoPosting == true)
                    {
                        mFD_Detail.UserID = 0;
                        mFD_Detail.UserName = "$$";
                        mFD_Detail.CashierNo = "";
                        mFD_Detail.ShiftID = 0;
                    }
                    else
                    {
                        mFD_Detail.UserID = userID;
                        mFD_Detail.UserName = userName;
                        mFD_Detail.CashierNo = userName;
                        mFD_Detail.ShiftID = userID;
                    }
                    #endregion

                    #endregion

                    #region Lấy ra thông tin của Transaction Pkg
                    //TransactionsModel mT_Group = (TransactionsModel)pt.FindByAttribute("Transactions", "Code", _PkgCode)[0];
                    TransactionsModel mT_Group = (TransactionsModel)TransactionsBO.Instance.FindByAttribute("Code", _PkgCode)[0];
                    #endregion

                    #region Insert dòng tổng <Invoice>

                    mFD_Group.IsSplit = true;
                    mFD_Group.PostType = 3;
                    mFD_Group.RowState = 1;
                    mFD_Group.Quantity = 1;

                    mFD_Group.TransactionGroupID = mT_Group.TransactionGroupID;
                    mFD_Group.TransactionSubgroupID = mT_Group.TransactionSubGroupID;
                    mFD_Group.GroupCode = mT_Group.GroupCode;
                    mFD_Group.SubgroupCode = mT_Group.SubgroupCode;
                    mFD_Group.GroupType = mT_Group.GroupType;

                    mFD_Group.ArticleCode = "";
                    mFD_Group.TransactionCode = mT_Group.Code;

                    //if (Description == "PKG")
                    //    mFD_Group.Description = mT_Group.Description;
                    //else
                    //    mFD_Group.Description = Description; // mT_Group.Description;

                    mFD_Group.Description = Description;

                    mFD_Group.Reference = _Ref[0];

                    mFD_Group.RoomID = _RoomID;

                    mFD_Group.Price = 0;
                    mFD_Group.Amount = 0;
                    mFD_Group.AmountMaster = 0;
                    mFD_Group.AmountGross = 0;
                    mFD_Group.AmountMasterGross = 0;
                    mFD_Group.AmountBeforeTax = 0;
                    mFD_Group.AmountMasterBeforeTax = 0;

                    mFD_Group.CurrencyID = _CurrencyID[0];// _CurrencyLocal;
                    mFD_Group.CurrencyMaster = _CurrencyLocal;

                    mFD_Group.RoomTypeID = RoomTypeID;
                    mFD_Group.RoomType = RoomType;
                    mFD_Group.ID = (int)FolioDetailBO.Instance.Insert(mFD_Group);
                    mFD_Group.InvoiceNo = mFD_Group.ID.ToString();
                    mFD_Group.TransactionNo = mFD_Group.InvoiceNo;

                    #endregion

                    #region Thực hiện posting chi tiết
                    for (int i = 0; i < _TransCode.Length; i++)
                    {
                        if (_TransCode[i] != null && _TransCode[i] != "" && _Amount[i] > 0)
                        {
                            #region Lấy thông tin của Trans.Code
                            // TransactionsModel mT = (TransactionsModel)pt.FindByAttribute("Transactions", "Code", _TransCode[i])[0];
                            TransactionsModel mT = (TransactionsModel)TransactionsBO.Instance.FindByAttribute("Code", _TransCode[i])[0];
                            #endregion

                            #region Kiểm tra xem đã có Generate
                            //  ArrayList arr = new ArrayList(pt.FindByAttribute("GenerateTransaction", "TransactionCode", _TransCode[i]));
                            ArrayList arr = new ArrayList();
                            arr.AddRange(GenerateTransactionBO.Instance.FindByAttribute("TransactionCode", _TransCode[i]));

                            #endregion

                            #region Nếu chưa tồn tại trong Generate
                            if ((arr == null) || (arr.Count == 0))
                            {
                                mFD_Detail.CurrencyID = _CurrencyID[i];
                                mFD_Detail.IsSplit = false;
                                mFD_Detail.PostType = 3;
                                mFD_Detail.RowState = 2;

                                mFD_Detail.TransactionGroupID = mT.TransactionGroupID;
                                mFD_Detail.TransactionSubgroupID = mT.TransactionSubGroupID;
                                mFD_Detail.GroupCode = mT.GroupCode;
                                mFD_Detail.SubgroupCode = mT.SubgroupCode;
                                mFD_Detail.GroupType = mT.GroupType;

                                mFD_Detail.ArticleCode = _ArCode[i];
                                mFD_Detail.TransactionCode = mT.Code;
                                mFD_Detail.Description = mT.Description;

                                mFD_Detail.Quantity = _Quan[i];
                                //Làm tròn VND
                                if (_CurrencyID[i] == "VND")
                                {
                                    mFD_Detail.Amount = Math.Round(_Amount[i], 0);
                                    mFD_Detail.AmountBeforeTax = Math.Round(mFD_Detail.Amount, 0);
                                    mFD_Detail.Price = Math.Round(mFD_Detail.Amount / mFD_Detail.Quantity, 0);

                                    if (i == 0)
                                    {
                                        mFD_Detail.AmountMaster = Math.Round(pt.ExchangeCurrency(_BusinessDate, _CurrencyID[i], _CurrencyLocal, mFD_Detail.Amount), 0);
                                        Rate = Math.Round(mFD_Detail.AmountMaster / mFD_Detail.Amount, 0);
                                    }
                                    else
                                        mFD_Detail.AmountMaster = Math.Round(mFD_Detail.Amount * Rate, 0);

                                    mFD_Detail.AmountMasterBeforeTax = Math.Round(mFD_Detail.AmountMaster, 0);

                                    mFD_Detail.AmountGross = Math.Round(mFD_Detail.Amount, 0);
                                    mFD_Detail.AmountMasterGross = Math.Round(mFD_Detail.AmountMaster, 0);
                                }
                                else
                                {
                                    mFD_Detail.Amount = _Amount[i];
                                    mFD_Detail.AmountBeforeTax = mFD_Detail.Amount;
                                    mFD_Detail.Price = mFD_Detail.Amount / mFD_Detail.Quantity;

                                    if (i == 0)
                                    {
                                        mFD_Detail.AmountMaster = pt.ExchangeCurrency(_BusinessDate, _CurrencyID[i], _CurrencyLocal, mFD_Detail.Amount);
                                        Rate = mFD_Detail.AmountMaster / mFD_Detail.Amount;
                                    }
                                    else
                                        mFD_Detail.AmountMaster = mFD_Detail.Amount * Rate;

                                    mFD_Detail.AmountMasterBeforeTax = mFD_Detail.AmountMaster;

                                    mFD_Detail.AmountGross = mFD_Detail.Amount;
                                    mFD_Detail.AmountMasterGross = mFD_Detail.AmountMaster;
                                }

                                mFD_Detail.InvoiceNo = mFD_Group.InvoiceNo;
                                mFD_Detail.RoomType = RoomType;
                                mFD_Detail.RoomTypeID = RoomTypeID;

                                mFD_Detail.ID = (int)FolioDetailBO.Instance.Insert(mFD_Detail);
                                mFD_Detail.TransactionNo = mFD_Detail.ID.ToString();
                                FolioDetailBO.Instance.Update(mFD_Detail);

                                //Cập nhập thông tin Invoice
                                //Làm tròn VND
                                if (_CurrencyID[i] == "VND")
                                {
                                    mFD_Group.AmountBeforeTax = Math.Round(mFD_Group.AmountBeforeTax + mFD_Detail.AmountBeforeTax, 0);
                                    mFD_Group.AmountMasterBeforeTax = Math.Round(mFD_Group.AmountMasterBeforeTax + mFD_Detail.AmountMasterBeforeTax, 0);

                                    mFD_Group.Amount = Math.Round(mFD_Group.Amount + mFD_Detail.AmountMaster, 0);
                                    mFD_Group.AmountMaster = Math.Round(mFD_Group.AmountMaster + mFD_Detail.AmountMaster, 0);

                                    mFD_Group.AmountGross = Math.Round(mFD_Group.AmountGross + mFD_Detail.AmountGross, 0);
                                    mFD_Group.AmountMasterGross = Math.Round(mFD_Group.AmountMasterGross + mFD_Detail.AmountMasterGross, 0);
                                }
                                else
                                {
                                    mFD_Group.AmountBeforeTax = mFD_Group.AmountBeforeTax + mFD_Detail.AmountBeforeTax;
                                    mFD_Group.AmountMasterBeforeTax = mFD_Group.AmountMasterBeforeTax + mFD_Detail.AmountMasterBeforeTax;

                                    mFD_Group.Amount = mFD_Group.Amount + mFD_Detail.AmountMaster;
                                    mFD_Group.AmountMaster = mFD_Group.AmountMaster + mFD_Detail.AmountMaster;

                                    mFD_Group.AmountGross = mFD_Group.AmountGross + mFD_Detail.AmountGross;
                                    mFD_Group.AmountMasterGross = mFD_Group.AmountMasterGross + mFD_Detail.AmountMasterGross;
                                }
                            }
                            #endregion

                            #region Nếu có tồn tại generate -> thực hiện
                            else
                            {
                                #region Khai báo biến
                                decimal s1 = 0, s2 = 0, s3 = 0;
                                decimal CurrentAmount = 0;
                                decimal BaseAmount = _Amount[i];
                                GenerateTransactionModel mGT;
                                #endregion

                                #region Lấy ra thông tin giá trước thuế
                                if (_TaxInclude[i] == true)
                                    BaseAmount = GetAmount(arr, Convert.ToDecimal(BaseAmount));
                                #endregion

                                #region Insert dòng tổng
                                mFD_Subgroup.CurrencyID = _CurrencyID[i];
                                mFD_Subgroup.IsSplit = true;
                                mFD_Subgroup.PostType = 3;
                                mFD_Subgroup.RowState = 2;

                                mFD_Subgroup.Reference = _Ref[i];
                                mFD_Subgroup.Supplement = _Supp[i];

                                mFD_Subgroup.TransactionGroupID = mT.TransactionGroupID;
                                mFD_Subgroup.TransactionSubgroupID = mT.TransactionSubGroupID;
                                mFD_Subgroup.GroupCode = mT.GroupCode;
                                mFD_Subgroup.SubgroupCode = mT.SubgroupCode;
                                mFD_Subgroup.GroupType = mT.GroupType;

                                mFD_Subgroup.ArticleCode = _ArCode[i];
                                mFD_Subgroup.TransactionCode = mT.Code;
                                mFD_Subgroup.Description = mT.Description;//mT.Description;

                                mFD_Subgroup.Quantity = _Quan[i];

                                mFD_Subgroup.Price = 0;
                                mFD_Subgroup.Amount = 0;
                                mFD_Subgroup.AmountMaster = 0;
                                mFD_Subgroup.AmountBeforeTax = 0;
                                mFD_Subgroup.AmountMasterBeforeTax = 0;

                                mFD_Subgroup.RoomType = RoomType;
                                mFD_Subgroup.RoomTypeID = RoomTypeID;
                                mFD_Subgroup.ID = (int)FolioDetailBO.Instance.Insert(mFD_Subgroup); //Dong tong cap 2

                                mFD_Subgroup.InvoiceNo = mFD_Group.InvoiceNo;
                                mFD_Subgroup.TransactionNo = mFD_Subgroup.ID.ToString();
                                #endregion

                                for (int j = 0; j < arr.Count; j++)
                                {
                                    #region Đổ dữ liệu vào Model
                                    mGT = (GenerateTransactionModel)arr[j];
                                    #endregion

                                    #region Lấy ra CurrentAmount
                                    if (mGT.Type == 0)
                                    {
                                        if (mGT.BaseAmount == 0)
                                            CurrentAmount = (mGT.Percentage * BaseAmount) / 100;
                                        else if (mGT.BaseAmount == 1)
                                            CurrentAmount = (mGT.Percentage * s1) / 100;
                                        else if (mGT.BaseAmount == 2)
                                            CurrentAmount = (mGT.Percentage * s2) / 100;
                                        else
                                            CurrentAmount = (mGT.Percentage * s3) / 100;
                                    }
                                    else if (mGT.Type == 1)
                                    {
                                        CurrentAmount = mGT.Amount;
                                    }

                                    //CurrentAmount = GetAmountFormat(CurrentAmount);

                                    #endregion

                                    #region Lấy dữ liệu vào s1,s2,s3
                                    if ((mGT.Subtotal1 == true) && (mGT.Subtotal2 == false) && (mGT.Subtotal3 == false))
                                    {
                                        s1 = s1 + CurrentAmount;
                                    }
                                    else if ((mGT.Subtotal1 == true) && (mGT.Subtotal2 == true) && (mGT.Subtotal3 == false))
                                    {
                                        s1 = s1 + CurrentAmount;
                                        s2 = CurrentAmount;
                                    }
                                    else if ((mGT.Subtotal1 == true) && (mGT.Subtotal2 == false) && (mGT.Subtotal3 == true))
                                    {
                                        s1 = s1 + CurrentAmount;
                                        s3 = CurrentAmount;
                                    }
                                    else if ((mGT.Subtotal1 == true) && (mGT.Subtotal2 == true) && (mGT.Subtotal3 == true))
                                    {
                                        s1 = s1 + CurrentAmount;
                                        s2 = CurrentAmount;
                                        s3 = CurrentAmount;
                                    }
                                    #endregion

                                    #region Đổ dữ liệu vào Model
                                    mFD_Detail.CurrencyID = _CurrencyID[i];
                                    mFD_Detail.IsSplit = false;
                                    mFD_Detail.PostType = 3;
                                    mFD_Detail.RowState = 3;

                                    mFD_Detail.TransactionGroupID = mGT.TransactionGroupID;
                                    mFD_Detail.TransactionSubgroupID = mGT.TransactionSubGroupID;
                                    mFD_Detail.GroupCode = mGT.GroupCode;
                                    mFD_Detail.SubgroupCode = mGT.SubgroupCode;
                                    mFD_Detail.GroupType = mGT.GroupType;

                                    mFD_Detail.TransactionCode = mGT.TransactionCodeDetail;
                                    mFD_Detail.Description = mGT.Description;

                                    if ((_TaxInclude[i] == true) && (j == arr.Count - 1))
                                        mFD_Detail.Amount = _Amount[i] - mFD_Subgroup.Amount;
                                    else
                                        mFD_Detail.Amount = GetAmountFormat(CurrentAmount);

                                    if (_CurrencyID[i] == "VND")
                                        mFD_Detail.Amount = Math.Round(mFD_Detail.Amount);

                                    mFD_Detail.Quantity = _Quan[i];
                                    //Làm tròn VND
                                    if (_CurrencyID[i] == "VND")
                                    {
                                        mFD_Detail.AmountBeforeTax = Math.Round(mFD_Detail.Amount, 0);
                                        mFD_Detail.Price = Math.Round(mFD_Detail.Amount / mFD_Detail.Quantity, 0);
                                        mFD_Detail.AmountGross = Math.Round(mFD_Detail.Amount, 0);
                                    }
                                    else
                                    {
                                        mFD_Detail.AmountBeforeTax = mFD_Detail.Amount;
                                        mFD_Detail.Price = mFD_Detail.Amount / mFD_Detail.Quantity;
                                        mFD_Detail.AmountGross = mFD_Detail.Amount;

                                    }

                                    if (_CurrencyID[i] == "VND")
                                    {
                                        if ((i == 0) && (j == 0))
                                        {
                                            // Tính ra tỷ giá nếu là dòng đầu
                                            mFD_Detail.AmountMaster = Math.Round(pt.ExchangeCurrency(_BusinessDate, _CurrencyID[i], _CurrencyLocal, mFD_Detail.Amount), 0);
                                            Rate = mFD_Detail.AmountMaster / mFD_Detail.Amount;
                                        }
                                        else
                                            mFD_Detail.AmountMaster = mFD_Detail.Amount * Rate;

                                        if (j == 0)
                                        {
                                            // Nếu là dòng đầu -> insert giá trước thuế.
                                            mFD_Subgroup.AmountBeforeTax = Math.Round(mFD_Detail.Amount, 0);
                                            mFD_Subgroup.AmountMasterBeforeTax = Math.Round(mFD_Detail.AmountMaster, 0);
                                        }

                                        mFD_Detail.AmountMasterBeforeTax = Math.Round(mFD_Detail.AmountMaster, 0);
                                        mFD_Detail.AmountMasterGross = Math.Round(mFD_Detail.AmountMaster, 0);
                                    }
                                    else
                                    {
                                        if ((i == 0) && (j == 0))
                                        {
                                            // Tính ra tỷ giá nếu là dòng đầu
                                            mFD_Detail.AmountMaster = pt.ExchangeCurrency(_BusinessDate, _CurrencyID[i], _CurrencyLocal, mFD_Detail.Amount);
                                            Rate = mFD_Detail.AmountMaster / mFD_Detail.Amount;
                                        }
                                        else
                                            mFD_Detail.AmountMaster = mFD_Detail.Amount * Rate;

                                        if (j == 0)
                                        {
                                            // Nếu là dòng đầu -> insert giá trước thuế.
                                            mFD_Subgroup.AmountBeforeTax = mFD_Detail.Amount;
                                            mFD_Subgroup.AmountMasterBeforeTax = mFD_Detail.AmountMaster;
                                        }

                                        mFD_Detail.AmountMasterBeforeTax = mFD_Detail.AmountMaster;
                                        mFD_Detail.AmountMasterGross = mFD_Detail.AmountMaster;

                                    }

                                    #endregion

                                    #region Insert Du lieu
                                    mFD_Detail.RoomTypeID = RoomTypeID;
                                    mFD_Detail.RoomType = RoomType;
                                    mFD_Detail.InvoiceNo = mFD_Subgroup.InvoiceNo;
                                    mFD_Detail.TransactionNo = mFD_Subgroup.TransactionNo;
                                    mFD_Detail.ID = (int)FolioDetailBO.Instance.Insert(mFD_Detail);
                                    if (_CurrencyID[i] == "VND")
                                    {
                                        mFD_Subgroup.AmountMaster = Math.Round(mFD_Subgroup.AmountMaster + mFD_Detail.AmountMaster, 0);
                                        mFD_Subgroup.Amount = Math.Round(mFD_Subgroup.Amount + mFD_Detail.Amount, 0);
                                    }
                                    else
                                    {
                                        mFD_Subgroup.AmountMaster = mFD_Subgroup.AmountMaster + mFD_Detail.AmountMaster;
                                        mFD_Subgroup.Amount = mFD_Subgroup.Amount + mFD_Detail.Amount;

                                    }
                                    #endregion
                                }
                                // Tính giá Gross
                                mFD_Subgroup.AmountGross = mFD_Subgroup.Amount;
                                mFD_Subgroup.AmountMasterGross = mFD_Subgroup.AmountMaster;
                                // Tính giá Net số tiền nhập vào là giá sau thuế
                                if (_CurrencyID[i] == "VND")
                                {
                                    if (_TaxInclude[i] == true)
                                    {
                                        mFD_Subgroup.Amount = Math.Round(_Amount[i], 0);
                                        mFD_Subgroup.AmountMaster = Math.Round(_Amount[i] * Rate, 0);
                                    }
                                    mFD_Subgroup.Price = Math.Round(mFD_Subgroup.Amount / mFD_Subgroup.Quantity, 0);
                                }
                                else
                                {
                                    if (_TaxInclude[i] == true)
                                    {
                                        mFD_Subgroup.Amount = _Amount[i];
                                        mFD_Subgroup.AmountMaster = _Amount[i] * Rate;
                                    }
                                    mFD_Subgroup.Price = mFD_Subgroup.Amount / mFD_Subgroup.Quantity;

                                }
                                // Update thông tin của subgroup
                                FolioDetailBO.Instance.Update(mFD_Subgroup);

                                //Làm tròn VND
                                // Cập nhật thông tin group
                                if (_CurrencyID[i] == "VND")
                                {
                                    mFD_Group.AmountBeforeTax = Math.Round(mFD_Group.AmountBeforeTax + mFD_Subgroup.AmountBeforeTax, 0);
                                    mFD_Group.AmountMasterBeforeTax = Math.Round(mFD_Group.AmountMasterBeforeTax + mFD_Subgroup.AmountMasterBeforeTax, 0);

                                    mFD_Group.Amount = Math.Round(mFD_Group.Amount + mFD_Subgroup.Amount, 0);
                                    mFD_Group.AmountMaster = Math.Round(mFD_Group.AmountMaster + mFD_Subgroup.AmountMaster, 0);

                                    mFD_Group.AmountGross = Math.Round(mFD_Group.AmountGross + mFD_Subgroup.AmountGross, 0);
                                    mFD_Group.AmountMasterGross = Math.Round(mFD_Group.AmountMasterGross + mFD_Subgroup.AmountMasterGross, 0);
                                }
                                else
                                {
                                    mFD_Group.AmountBeforeTax = mFD_Group.AmountBeforeTax + mFD_Subgroup.AmountBeforeTax;
                                    mFD_Group.AmountMasterBeforeTax = mFD_Group.AmountMasterBeforeTax + mFD_Subgroup.AmountMasterBeforeTax;

                                    mFD_Group.Amount = mFD_Group.Amount + mFD_Subgroup.Amount;
                                    mFD_Group.AmountMaster = mFD_Group.AmountMaster + mFD_Subgroup.AmountMaster;

                                    mFD_Group.AmountGross = mFD_Group.AmountGross + mFD_Subgroup.AmountGross;
                                    mFD_Group.AmountMasterGross = mFD_Group.AmountMasterGross + mFD_Subgroup.AmountMasterGross;

                                }
                            }
                            #endregion
                        }
                    }
                    #endregion

                    #region Commit va Return

                    mFD_Group.Price = mFD_Group.Amount;
                    FolioDetailBO.Instance.Update(mFD_Group);
                    UpdateBalance(_RsvID, FolioID, ref _Message);
                    InsertHistory(_SysDate, _BusinessDate, mFD_Group.FolioID, mFD_Group.FolioID, mFD_Group.InvoiceNo, HistoryType.Night_Post,
                        GetActionText(HistoryType.Night_Post, mFD_Group.TransactionCode, mFD_Group.Description),
                        "$$", mFD_Group.TransactionCode, mFD_Group.Description, mFD_Group.Amount, mFD_Group.Supplement, "", "", "");

                    //pt.CommitTransaction();
                    //pt.CloseConnection();
                    return true;

                    #endregion
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                //pt.RollBack();
                _Message = ex.Message;
                return false;
            }
        }
        public static string GetActionText(HistoryType _ActionType, string _Code, string _Text)
        {
            if (_ActionType == HistoryType.Gen_Post)
                return "[POST_GEN] - " + _Code + " - " + _Text;

            if (_ActionType == HistoryType.Basic_Post)
                return "[POST] - " + _Code + " - " + _Text;

            if (_ActionType == HistoryType.Night_Post)
                return "[NIGHT_POST]" + _Code + " - " + _Text;

            if (_ActionType == HistoryType.Early_Post)
                return "[EARLY_CHECKOUT]" + _Code + " - " + _Text;

            if (_ActionType == HistoryType.Advance_Post)
                return "[ADVANCE_BILL]" + _Code + " - " + _Text;

            if (_ActionType == HistoryType.Correct)
                return "[CORRECT_TRANSACTION] - " + _Code + " " + _Text;

            if (_ActionType == HistoryType.Split)
                return "[SPLIT_TRANSACTION] - " + _Code + " from " + _Text;

            if (_ActionType == HistoryType.Tranferred)
                return "[TRANFERRED] - " + _Code + " - " + _Text;

            if (_ActionType == HistoryType.Deleted)
                return "[DELETED] - " + _Code + " - " + _Text;

            if (_ActionType == HistoryType.Payment)
                return "[PAY] - " + _Code + " - " + _Text;

            if (_ActionType == HistoryType.Print)
                return "[PRINT] - " + _Code + " - " + _Text;
            return "";
        }
        public enum HistoryType { Gen_Post, Basic_Post, Night_Post, Early_Post, Advance_Post, Correct, Split, Tranferred, Deleted, Payment, Print };
        public void InsertHistory(DateTime _SysDate, DateTime _BusinessDate, int _Action_FolioID, int _AfterAction_FolioID, string _InvoiceNo,
                                                 HistoryType _ActionType, string _ActionText, string _ActionByUser, string _Code, string _Desc,
                                                 decimal _Amount, string _Supplement, string _ReasonCode, string _ReasonText, string _Terminal)
        {
            try
            {
                PostingHistoryModel mPH = new PostingHistoryModel();
                mPH.ActionType = GetActionType(_ActionType);
                if ((_ActionType == HistoryType.Advance_Post) || (_ActionType == HistoryType.Basic_Post) || (_ActionType == HistoryType.Early_Post)
                    || (_ActionType == HistoryType.Gen_Post) || (_ActionType == HistoryType.Night_Post) || (_ActionType == HistoryType.Payment))
                    mPH.ActionText = _ActionText + " - Amount($" + _Amount.ToString("###,###,###.##") + ")";
                else
                    mPH.ActionText = _ActionText;
                mPH.ActionDate = _SysDate;
                mPH.TransactionDate = _BusinessDate;
                mPH.ActionUser = _ActionByUser;
                mPH.InvoiceNo = _InvoiceNo;
                mPH.Action_FolioID = _Action_FolioID;
                mPH.AfterAction_FolioID = _AfterAction_FolioID;
                mPH.Amount = _Amount;
                mPH.Supplement = _Supplement;
                mPH.Code = _Code;
                mPH.Description = _Desc;
                mPH.ReasonCode = _ReasonCode;
                mPH.ReasonText = _ReasonText;
                mPH.Terminal = _Terminal;
                mPH.Machine = TextUtils.GetHostName();
                if (_ActionType != HistoryType.Night_Post)
                    mPH.Property = "PMS";
                else
                    mPH.Property = "NIGHT";

                PostingHistoryBO.Instance.Insert(mPH);
            }
            catch (Exception ex)
            {
                return;
            }
        }
        public int GetActionType(HistoryType _ActionType)
        {
            if (_ActionType == HistoryType.Gen_Post)
                return 0;

            if (_ActionType == HistoryType.Basic_Post)
                return 1;

            if (_ActionType == HistoryType.Night_Post)
                return 2;

            if (_ActionType == HistoryType.Early_Post)
                return 3;

            if (_ActionType == HistoryType.Advance_Post)
                return 4;

            if (_ActionType == HistoryType.Correct)
                return 5;

            if (_ActionType == HistoryType.Split)
                return 6;

            if (_ActionType == HistoryType.Tranferred)
                return 7;

            if (_ActionType == HistoryType.Deleted)
                return 8;

            if (_ActionType == HistoryType.Payment)
                return 9;

            if (_ActionType == HistoryType.Print)
                return 10;
            return -1;
        }
        public bool UpdateBalance(int _ReservationID, int _FolioID, ref string _Message)
        {
            try
            {
                TextUtils.ExcuteSQL("Update Folio set BalanceVND=dbo.getBalanceOfFolio(" + _FolioID + ",'" + _CURRENCY_1 + "')," +
                                "BalanceUSD=dbo.getBalanceOfFolio(" + _FolioID + ",'" + _CURRENCY_2 + "') Where ID=" + _FolioID);
                TextUtils.ExcuteSQL("Update Reservation set BalanceVND=dbo.getBalanceOfGih(" + _ReservationID + ",'" + _CURRENCY_1 + "')," +
                                "BalanceUSD=dbo.getBalanceOfGih(" + _ReservationID + ",'" + _CURRENCY_2 + "') Where ID=" + _ReservationID);
                return true;
            }
            catch (Exception ex)
            {
                _Message = ex.Message;
                return false;
            }
        }
        public static decimal GetAmountFormat(decimal Amount)
        {
            Decimal Result = Convert.ToDecimal(Amount.ToString("###,###,###.00"));
            return Result;
        }

        private bool GetIncludeRate(int PackageID)
        {
            /* Trả về true nếu package nằm trong giá; false : nếu không bao gồm trong giá */
            if (TextUtils.Select("Select * From Package Where IncludedInRate =1 And ID = " + PackageID).Rows.Count > 0)
                return true;
            else
                return false;
        }

        private void PostingFixedCharge(string RoomNo, DateTime Date, string userName, int userID)
        {
            DateTime SystemDate = TextUtils.GetBusinessDate();
            DateTime PostingDate; DateTime BeginDate; DateTime EndDate; DateTime BusinessDate = Date;
            DataTable tbReservation = TextUtils.Select("Select * From Reservation Where RoomNo = '" + RoomNo + "' And (Status =1 or Status =6) And IsAdvanceBill =0");
            if (tbReservation.Rows.Count > 0)
            {
                for (int r = 0; r < tbReservation.Rows.Count; r++)
                {
                    if (CheckAdvanceBill(int.Parse(tbReservation.Rows[r]["ID"].ToString()), BusinessDate) == false)
                    {
                        int RoomTypeID = int.Parse(tbReservation.Rows[r]["RoomTypeID"].ToString());
                        string RoomType = tbReservation.Rows[r]["RoomType"].ToString();
                        int ReservationID = int.Parse(tbReservation.Rows[r]["ID"].ToString());
                        //Lấy danh sách các Fixed Charge trong bảng ReservationFixedCharge tương ứng với từng RSV
                        DataTable tbNightAuditFixedCharge = TextUtils.getTable2("spNightAuditFixedCharge", "tbNightAuditFixedCharge", new SqlParameter("ReservationID", TextUtils.ToInt(tbReservation.Rows[r]["ID"].ToString())), new SqlParameter("@RateDate", TextUtils.GetBusinessDate()));
                        if (tbNightAuditFixedCharge.Rows.Count > 0)
                        {
                            for (int f = 0; f < tbNightAuditFixedCharge.Rows.Count; f++)
                            {
                                #region //Kiểm tra thời điểm charge --> Có charge hay không?
                                bool PostingStatus = false;
                                switch (tbNightAuditFixedCharge.Rows[f]["PostingRhythmID"].ToString())
                                {
                                    case "1"://Every Night
                                        {
                                            BeginDate = Convert.ToDateTime(tbNightAuditFixedCharge.Rows[f]["BeginDate"]);
                                            EndDate = Convert.ToDateTime(tbNightAuditFixedCharge.Rows[f]["EndDate"]);
                                            if (TextUtils.CompareDate(BeginDate, BusinessDate) <= 0 && TextUtils.CompareDate(EndDate, BusinessDate) > 0)
                                                PostingStatus = true;
                                            else
                                                PostingStatus = false;
                                            break;
                                        }
                                    case "2"://At Date
                                        {
                                            PostingDate = Convert.ToDateTime(tbNightAuditFixedCharge.Rows[f]["PostingDate"]);
                                            if (TextUtils.CompareDate(PostingDate, BusinessDate) == 0)
                                                PostingStatus = true;
                                            else
                                                PostingStatus = false;
                                            break;
                                        }
                                    case "3"://At Day
                                        {
                                            string[] PD = tbNightAuditFixedCharge.Rows[f]["PostingDay"].ToString().Split(',');
                                            PostingStatus = false;
                                            DateTime ArrivalDate = Convert.ToDateTime(tbReservation.Rows[r]["ArrivalDate"]);
                                            for (int d = 0; d < PD.Length; d++)
                                            {
                                                int AddDay = TextUtils.ToInt(PD[d]) - 1;
                                                if (TextUtils.CompareDate(ArrivalDate.AddDays(AddDay), BusinessDate) == 0)
                                                    PostingStatus = true;
                                            }

                                            break;
                                        }
                                    case "4"://Checkin
                                        {
                                            DateTime ArrivalDate = Convert.ToDateTime(tbReservation.Rows[r]["ArrivalDate"]);
                                            if (TextUtils.CompareDate(BusinessDate, ArrivalDate) == 0)
                                                PostingStatus = true;
                                            else
                                                PostingStatus = false;
                                            break;
                                        }
                                    case "5"://Check Out
                                        {
                                            DateTime DepartureDate = Convert.ToDateTime(tbReservation.Rows[r]["DepartureDate"]);
                                            if (TextUtils.CompareDate(BusinessDate, DepartureDate.AddDays(-1)) == 0)
                                                PostingStatus = true;
                                            else
                                                PostingStatus = false;
                                            break;
                                        }
                                }
                                #endregion

                                bool isPostingFixedCharge = false;
                                if (PostingStatus == true)
                                {
                                    #region Gán giá trị cho các biến

                                    #region Lấy các giá trị từ bảng Reservation
                                    int RoomID = TextUtils.ToInt(tbReservation.Rows[r]["RoomID"].ToString());
                                    //string RoomNo = tbReservation.Rows[r]["RoomNo"].ToString();
                                    int FromReservationID = TextUtils.ToInt(tbReservation.Rows[r]["ID"].ToString());
                                    string ConfirmNo = tbReservation.Rows[r]["ConfirmationNo"].ToString();
                                    string Reffrence = "";
                                    if (tbReservation.Rows[r]["RateCode"].ToString() != "")
                                        Reffrence = "R:" + RoomNo + "; D:" + TextUtils.GetBusinessDate().ToString("dd/MM") + "; C:" + ConfirmNo + "; R" + tbReservation.Rows[r]["RateCode"].ToString();
                                    else
                                        Reffrence = "R:" + RoomNo + "; D:" + TextUtils.GetBusinessDate().ToString("dd/MM") + "; C:" + ConfirmNo + " ";
                                    #endregion

                                    #region Lấy thông tin từ bảng ReservationFixedCharge
                                    bool TaxInclude = false;
                                    TaxInclude = bool.Parse(tbNightAuditFixedCharge.Rows[f]["IsTaxInclude"].ToString());
                                    decimal Amount = TaxInclude == true ? TextUtils.ToDecimal(tbNightAuditFixedCharge.Rows[f]["AmountAfterTax"].ToString()) : TextUtils.ToDecimal(tbNightAuditFixedCharge.Rows[f]["Amount"].ToString());
                                    string CurrencyID = tbNightAuditFixedCharge.Rows[f]["CurrencyID"].ToString();
                                    string TransactionCode = tbNightAuditFixedCharge.Rows[f]["TransactionCode"].ToString();
                                    string ArticlesCode = tbNightAuditFixedCharge.Rows[f]["ArticlesCode"].ToString();
                                    int Quantity = Convert.ToInt16(tbNightAuditFixedCharge.Rows[f]["Quantity"].ToString());
                                    Amount = Amount * Quantity;
                                    #endregion

                                    #region Các biến khác
                                    string Supplement = "";
                                    decimal AmountReturn = 0;
                                    decimal AmountMasterReturn = 0;
                                    string err = "";
                                    int Windows = 1;
                                    string Description = "";
                                    string TransNoReturn = "";
                                    string AccountName = "";
                                    int OriginRsvID = TextUtils.ToInt(tbReservation.Rows[r]["ID"].ToString());
                                    int OriginFolioID = GetFolioID(OriginRsvID);
                                    #endregion

                                    #region Không tính chiết khấu cho Fixed Charge???
                                    //Amount = (Amount - Amount * TextUtils.ToDecimal(tbReservation.Rows[r]["DiscountRate"].ToString()) / 100) - TextUtils.ToDecimal(tbReservation.Rows[r]["DiscountAmount"].ToString());
                                    #endregion

                                    #endregion

                                    #region Trường hợp thông thường
                                    //Mở bảng Routing để lấy số ReservationID, Window cần charge tiền
                                    DataTable tbRouting = TextUtils.Select("Select * From Routing Where IsMasterFolio = 0 And isDefault = 0 And FromReservationID = " + FromReservationID.ToString() + " Order By isDefault");
                                    if (tbRouting.Rows.Count > 0)
                                    {
                                        for (int c = 0; c < tbRouting.Rows.Count; c++)
                                        {
                                            //Lấy ProfileID để tạo Folio
                                            int ProfileID = TextUtils.ToInt(tbRouting.Rows[c]["ProfileID"].ToString());
                                            AccountName = tbRouting.Rows[c]["AccountName"].ToString();
                                            int FolioID = GetFolioID(ReservationID, Windows, ConfirmNo);
                                            if (GetStatusFolio(FolioID) == false)
                                            {
                                                string[] arr = TextUtils.GetArrayTransaction(tbRouting.Rows[c]["TransactionCodes"].ToString());
                                                if (Array.IndexOf(arr, TransactionCode) >= 0 && isPostingFixedCharge == false)
                                                {
                                                    ReservationID = TextUtils.ToInt(tbRouting.Rows[c]["ToReservationID"].ToString());
                                                    Windows = TextUtils.ToInt(tbRouting.Rows[c]["ToFolioNo"].ToString());
                                                    isPostingFixedCharge = true;
                                                    PostingToFolio(true, SystemDate, Date, pProfitCenterID, pProfitCenterCode, ConfirmNo, ReservationID, RoomID, OriginRsvID, OriginFolioID, ProfileID, AccountName, Windows, TransactionCode, ArticlesCode, Reffrence, Supplement, Amount, TaxInclude, Quantity, CurrencyID, MasterCurrencyID, ref AmountReturn, ref AmountMasterReturn, ref TransNoReturn, ref err, RoomTypeID, RoomType, 0, "");
                                                }
                                            }
                                        }

                                    }

                                    #endregion

                                    #region Trường hợp Routing đến MasterFolio
                                    int pFolioID = 0;
                                    DataTable tbRoutingMasterFolio1 = TextUtils.Select("Select * From Routing Where IsMasterFolio = 1 And ConfirmationNo = '" + ConfirmNo + "'");
                                    if (tbRoutingMasterFolio1.Rows.Count > 0)
                                        for (int s = 0; s < tbRoutingMasterFolio1.Rows.Count; s++)
                                        {
                                            //Lay so FolioID 
                                            DataTable tbRSVFolio1 = TextUtils.Select("SELECT dbo.Folio.ID AS FolioID FROM dbo.Folio WHERE dbo.Folio.Status = 0 And dbo.Folio.FolioNo =-1 And dbo.Folio.ConfirmationNo ='" + ConfirmNo + "'");

                                            if (tbRSVFolio1.Rows.Count > 0)
                                                pFolioID = TextUtils.ToInt(tbRSVFolio1.Rows[0]["FolioID"].ToString());
                                            else
                                                pFolioID = TextUtils.CreateFolioAtNight(ReservationID, 0, "0", ConfirmNo, Windows, TextUtils.ToInt(tbRoutingMasterFolio1.Rows[0]["ProfileID"].ToString()), tbRoutingMasterFolio1.Rows[0]["AccountName"].ToString());

                                            int ProfileID = TextUtils.ToInt(tbRoutingMasterFolio1.Rows[s]["ProfileID"].ToString());
                                            AccountName = tbRoutingMasterFolio1.Rows[s]["AccountName"].ToString();
                                            string[] arr = TextUtils.GetArrayTransaction(tbRoutingMasterFolio1.Rows[s]["TransactionCodes"].ToString());
                                            if (Array.IndexOf(arr, TransactionCode) >= 0 && isPostingFixedCharge == false)
                                            {
                                                ReservationID = 0;
                                                Windows = -1;
                                                PostingToFolio(true, SystemDate, Date, pProfitCenterID, pProfitCenterCode, ConfirmNo, ReservationID, RoomID, OriginRsvID, OriginFolioID, ProfileID, AccountName, Windows, TransactionCode, ArticlesCode, Reffrence, Supplement, Amount, TaxInclude, Quantity, CurrencyID, MasterCurrencyID, ref AmountReturn, ref AmountMasterReturn, ref TransNoReturn, ref err, RoomTypeID, RoomType, 0, "");
                                                isPostingFixedCharge = true;
                                            }

                                            break;
                                        }

                                    #endregion

                                    #region Không có trong Routing lấy Routing Default
                                    DataTable tbRoutingExits1 = TextUtils.Select("Select * From Routing Where ConfirmationNo = '" + ConfirmNo + "'");
                                    if (tbRoutingExits1.Rows.Count == 0 || isPostingFixedCharge == false)
                                    {
                                        if (Amount > 0 && TextUtils.ToInt(tbReservation.Rows[r]["Status"].ToString()) == 1)
                                        {
                                            //Lấy số Folio, Window,... từ bảng Folio thông qua ReservationID
                                            int ProfileID = TextUtils.ToInt(tbReservation.Rows[r]["ProfileIndividualID"].ToString());
                                            AccountName = tbReservation.Rows[r]["LastName"].ToString();
                                            DataTable dtGetFromFolio = TextUtils.Select("Select * From Folio Where FolioNo = 1 And ReservationID = " + FromReservationID);

                                            if (dtGetFromFolio.Rows.Count > 0)
                                                PostingToFolio(true, SystemDate, Date, pProfitCenterID, pProfitCenterCode, ConfirmNo, TextUtils.ToInt(tbReservation.Rows[r]["ID"].ToString()), RoomID, OriginRsvID, OriginFolioID, ProfileID, AccountName, Windows, TransactionCode, ArticlesCode, Reffrence, Supplement, Amount, TaxInclude, Quantity, CurrencyID, MasterCurrencyID, ref AmountReturn, ref AmountMasterReturn, ref TransNoReturn, ref err, RoomTypeID, RoomType, 0, "");
                                            else
                                            {
                                                PostingToFolio(true, SystemDate, Date, pProfitCenterID, pProfitCenterCode, ConfirmNo, TextUtils.ToInt(tbReservation.Rows[r]["ID"].ToString()), RoomID, OriginRsvID, OriginFolioID, ProfileID, AccountName, Windows, TransactionCode, ArticlesCode, Reffrence, Supplement, Amount, TaxInclude, Quantity, CurrencyID, MasterCurrencyID, ref AmountReturn, ref AmountMasterReturn, ref TransNoReturn, ref err, RoomTypeID, RoomType, 0, "");
                                            }

                                        }
                                    }
                                    #endregion

                                }
                            }
                        }
                    }
                }
            }


        }
        public bool PostingToFolio(bool AutoPosting, DateTime _SysDate, DateTime _BusinessDate, int _ProID, string _ProCode, string _ConfirmNo, int _RsvID, int _RoomID, int _OriginRsvID, int _OriginFolioID,
                                   int _ProfileID, string _AccountName, int _Win, string _TransCode, string _ArCode, string _Ref, string _Supp,
                                   decimal _Amount, bool _TaxInclude, int _Quan, string _CurrencyID, string _CurrencyLocal,
                                   ref decimal _AmountReturn, ref decimal _AmountLocalReturn, ref string _TransNoReturn, ref string _Message, int RoomTypeID, string RoomType, int userID, string userName)
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                #region Gán giá trị cho ngày hệ thống
                // _BusinessDate = TextUtils.GetBusinessDateTime();
                #endregion

                #region Mở kết nối và bắt đầu 1 Transaction
                pt.OpenConnection();
                pt.BeginTransaction();
                #endregion

                #region Lấy ra thông tin của FolioID

                int _RsvID_Return = 0;
                int FolioID = GetOrCreateFolioID(_SysDate, _BusinessDate, _ConfirmNo, _RsvID, _Win, _ProfileID, _AccountName, ref _RsvID_Return, ref _Message, userID);
                _RsvID = _RsvID_Return;

                #endregion

                if (FolioID > 0)
                {
                    #region Khai báo Model

                    FolioDetailModel mFD_Detail = new FolioDetailModel();
                    FolioDetailModel mFD_Master = new FolioDetailModel();

                    #endregion

                    #region Lấy ra thông tin của TransCode
                    TransactionsModel mT = (TransactionsModel)TransactionsBO.Instance.FindByAttribute("Code", _TransCode)[0];
                    //TransactionsModel mT = (TransactionsModel)pt.FindByAttribute("Transactions", "Code", _TransCode)[0];
                    #endregion

                    #region Gán giá trị có các biến statictis

                    mFD_Detail.ProfitCenterID = _ProID;
                    mFD_Detail.ProfitCenterCode = _ProCode;
                    mFD_Detail.Status = false;

                    mFD_Detail.CurrencyID = _CurrencyID;
                    mFD_Detail.CurrencyMaster = _CurrencyLocal;

                    mFD_Detail.ReservationID = _RsvID;
                    mFD_Detail.OriginReservationID = _OriginRsvID;

                    mFD_Detail.RoomID = _RoomID;

                    mFD_Detail.FolioID = FolioID;
                    mFD_Detail.OriginFolioID = _OriginFolioID;

                    mFD_Detail.Quantity = _Quan;
                    mFD_Detail.TransactionDate = _BusinessDate;
                    mFD_Detail.PackageID = 0;

                    mFD_Detail.UserInsertID = userID;
                    mFD_Detail.UserUpdateID = userID;
                    mFD_Detail.CreateDate = _SysDate;
                    mFD_Detail.UpdateDate = _SysDate;
                    if (AutoPosting == true)
                    {
                        mFD_Detail.UserID = 0;
                        mFD_Detail.UserName = "$$";
                        mFD_Detail.CashierNo = "";
                        mFD_Detail.ShiftID = 0;
                    }
                    else
                    {
                        mFD_Detail.UserID = userID;
                        mFD_Detail.UserName = userName;
                        mFD_Detail.CashierNo = userName;
                        mFD_Detail.ShiftID = userID;
                    }
                    mFD_Master.ProfitCenterID = _ProID;
                    mFD_Master.ProfitCenterCode = _ProCode;
                    mFD_Master.Status = false;

                    mFD_Master.CurrencyID = _CurrencyID;
                    mFD_Master.CurrencyMaster = _CurrencyLocal;

                    mFD_Master.ReservationID = _RsvID;
                    mFD_Master.OriginReservationID = _OriginRsvID;
                    mFD_Master.RoomID = _RoomID;
                    mFD_Master.FolioID = FolioID;
                    mFD_Master.OriginFolioID = _OriginFolioID;

                    mFD_Master.Quantity = _Quan;
                    mFD_Master.TransactionDate = _BusinessDate;
                    mFD_Master.PackageID = 0;

                    mFD_Master.UserInsertID = userID;
                    mFD_Master.UserUpdateID = userID;
                    mFD_Master.CreateDate = _SysDate;
                    mFD_Master.UpdateDate = _SysDate;
                    if (AutoPosting == true)
                    {
                        mFD_Master.UserID = 0;
                        mFD_Master.UserName = "$$";
                        mFD_Master.CashierNo = "";
                        mFD_Master.ShiftID = 0;
                    }
                    else
                    {
                        mFD_Master.UserID = userID;
                        mFD_Master.UserName = userName;
                        mFD_Master.CashierNo = userName;
                        mFD_Master.ShiftID = userID;
                    }
                    #endregion

                    #region Kiểm tra xem Transaction này có ? trong Generate ?
                    ArrayList arr = new ArrayList();
                    arr.AddRange(GenerateTransactionBO.Instance.FindByAttribute("TransactionCode", _TransCode));

                    // ArrayList arr = new ArrayList(pt.FindByAttribute("GenerateTransaction", "TransactionCode", _TransCode));
                    #endregion

                    #region Nếu chưa tồn tại trong Generate.
                    if ((arr == null) || (arr.Count == 0))
                    {
                        //Gán thông tin cho các propertie còn lại
                        mFD_Detail.IsSplit = false;
                        mFD_Detail.Reference = _Ref;
                        mFD_Detail.Supplement = _Supp;

                        mFD_Detail.TransactionGroupID = mT.TransactionGroupID;
                        mFD_Detail.TransactionSubgroupID = mT.TransactionSubGroupID;
                        mFD_Detail.GroupCode = mT.GroupCode;
                        mFD_Detail.SubgroupCode = mT.SubgroupCode;
                        mFD_Detail.GroupType = mT.GroupType;
                        mFD_Detail.RoomID = _RoomID;
                        mFD_Detail.ArticleCode = _ArCode;
                        mFD_Detail.TransactionCode = mT.Code;
                        mFD_Detail.Description = mT.Description;

                        if (_CurrencyID == "VND")
                        {
                            mFD_Detail.Amount = Math.Round(_Amount, 0);
                            mFD_Detail.AmountBeforeTax = Math.Round(_Amount, 0);
                            mFD_Detail.Price = Math.Round(mFD_Detail.Amount / mFD_Detail.Quantity, 0);
                            mFD_Detail.AmountMaster = Math.Round(pt.ExchangeCurrency(_BusinessDate, _CurrencyID, _CurrencyLocal, _Amount), 0);
                            mFD_Detail.AmountMasterBeforeTax = Math.Round(mFD_Detail.AmountMaster, 0);
                            mFD_Detail.AmountGross = Math.Round(mFD_Detail.Amount, 0);
                            mFD_Detail.AmountMasterGross = Math.Round(mFD_Detail.AmountMaster, 0);
                        }
                        else
                        {
                            mFD_Detail.Amount = _Amount;
                            mFD_Detail.AmountBeforeTax = _Amount;
                            mFD_Detail.Price = mFD_Detail.Amount / mFD_Detail.Quantity;
                            mFD_Detail.AmountMaster = pt.ExchangeCurrency(_BusinessDate, _CurrencyID, _CurrencyLocal, _Amount);
                            mFD_Detail.AmountMasterBeforeTax = mFD_Detail.AmountMaster;
                            mFD_Detail.AmountGross = mFD_Detail.Amount;
                            mFD_Detail.AmountMasterGross = mFD_Detail.AmountMaster;
                        }
                        mFD_Detail.PostType = 1;
                        mFD_Detail.RowState = 1;
                        //Thực hiện post
                        mFD_Detail.RoomType = RoomType;
                        mFD_Detail.RoomTypeID = RoomTypeID;
                        mFD_Detail.ID = (int)pt.Insert(mFD_Detail);

                        mFD_Detail.InvoiceNo = mFD_Detail.ID.ToString();
                        mFD_Detail.TransactionNo = mFD_Detail.ID.ToString();

                        pt.Update(mFD_Detail);
                        //Update số dư
                        UpdateBalance(_RsvID, FolioID, ref _Message);
                        //Trả về thông tin
                        _AmountReturn = mFD_Detail.Amount;
                        _AmountLocalReturn = mFD_Detail.AmountMaster;
                        _TransNoReturn = mFD_Detail.TransactionNo;
                        // Ghi histoty
                        InsertHistory(_SysDate, _BusinessDate, mFD_Detail.FolioID, mFD_Detail.FolioID, mFD_Detail.InvoiceNo, HistoryType.Night_Post,
                            GetActionText(HistoryType.Night_Post, mFD_Detail.TransactionCode, mFD_Detail.Description),
                            "**", mFD_Detail.TransactionCode, mFD_Detail.Description, mFD_Detail.Amount, mFD_Detail.Supplement, "", "", "");
                    }
                    #endregion

                    #region Nếu đã tồn tại trong Generate -> l?y ra và th?c hi?n
                    else
                    {
                        #region Khai báo bi?n
                        decimal s1 = 0, s2 = 0, s3 = 0;
                        decimal CurrentAmount = 0;
                        decimal BaseAmount = _Amount;
                        decimal Rate = 0;
                        GenerateTransactionModel mGT;
                        #endregion

                        #region L?y ra thông tin c?a amount tru?c thu?
                        if (_TaxInclude == true)
                            BaseAmount = GetAmount(arr, Convert.ToDecimal(BaseAmount));
                        #endregion

                        #region Insert dòng tổng
                        mFD_Master.IsSplit = true;
                        mFD_Master.Reference = _Ref;
                        mFD_Master.Supplement = _Supp;

                        mFD_Master.TransactionGroupID = mT.TransactionGroupID;
                        mFD_Master.TransactionSubgroupID = mT.TransactionSubGroupID;
                        mFD_Master.GroupCode = mT.GroupCode;
                        mFD_Master.SubgroupCode = mT.SubgroupCode;
                        mFD_Master.GroupType = mT.GroupType;

                        mFD_Master.ArticleCode = _ArCode;
                        mFD_Master.TransactionCode = mT.Code;
                        mFD_Master.Description = mT.Description;//mT.Description;

                        mFD_Master.Quantity = _Quan;

                        mFD_Master.Price = 0;
                        mFD_Master.Amount = 0;
                        mFD_Master.AmountMaster = 0;
                        mFD_Master.AmountBeforeTax = 0;
                        mFD_Master.AmountMasterBeforeTax = 0;

                        mFD_Master.PostType = 2;
                        mFD_Master.RowState = 1;
                        mFD_Master.RoomID = _RoomID;
                        mFD_Master.RoomTypeID = RoomTypeID;
                        mFD_Master.RoomType = RoomType;
                        mFD_Master.ID = (int)pt.Insert(mFD_Master);

                        mFD_Master.InvoiceNo = mFD_Master.ID.ToString();
                        mFD_Master.TransactionNo = mFD_Master.ID.ToString();
                        #endregion

                        for (int j = 0; j < arr.Count; j++)
                        {
                            #region Đổ dữ liệu vào Model
                            mGT = (GenerateTransactionModel)arr[j];
                            #endregion

                            #region Lấy ra CurrentAmount
                            if (mGT.Type == 0)
                            {
                                if (mGT.BaseAmount == 0)
                                    CurrentAmount = (mGT.Percentage * BaseAmount) / 100;
                                else if (mGT.BaseAmount == 1)
                                    CurrentAmount = (mGT.Percentage * s1) / 100;
                                else if (mGT.BaseAmount == 2)
                                    CurrentAmount = (mGT.Percentage * s2) / 100;
                                else
                                    CurrentAmount = (mGT.Percentage * s3) / 100;
                            }
                            else if (mGT.Type == 1)
                            {
                                CurrentAmount = mGT.Amount;
                            }
                            #endregion

                            #region L?y d? li?u vào s1,s2,s3
                            if ((mGT.Subtotal1 == true) && (mGT.Subtotal2 == false) && (mGT.Subtotal3 == false))
                            {
                                s1 = s1 + CurrentAmount;
                            }
                            else if ((mGT.Subtotal1 == true) && (mGT.Subtotal2 == true) && (mGT.Subtotal3 == false))
                            {
                                s1 = s1 + CurrentAmount;
                                s2 = CurrentAmount;
                            }
                            else if ((mGT.Subtotal1 == true) && (mGT.Subtotal2 == false) && (mGT.Subtotal3 == true))
                            {
                                s1 = s1 + CurrentAmount;
                                s3 = CurrentAmount;
                            }
                            else if ((mGT.Subtotal1 == true) && (mGT.Subtotal2 == true) && (mGT.Subtotal3 == true))
                            {
                                s1 = s1 + CurrentAmount;
                                s2 = CurrentAmount;
                                s3 = CurrentAmount;
                            }
                            #endregion

                            #region Ð? d? li?u vào model Model

                            mFD_Detail.Reference = _Ref;

                            mFD_Detail.IsSplit = false;
                            mFD_Detail.PostType = 2;
                            mFD_Detail.RowState = 2;

                            mFD_Detail.TransactionGroupID = mGT.TransactionGroupID;
                            mFD_Detail.TransactionSubgroupID = mGT.TransactionSubGroupID;
                            mFD_Detail.GroupCode = mGT.GroupCode;
                            mFD_Detail.SubgroupCode = mGT.SubgroupCode;
                            mFD_Detail.GroupType = mGT.GroupType;

                            mFD_Detail.TransactionCode = mGT.TransactionCodeDetail;
                            mFD_Detail.Description = mGT.Description;
                            //Làm tròn VND
                            if (_CurrencyID == "VND")
                            {
                                if ((_TaxInclude == true) && (j == arr.Count - 1))
                                    mFD_Detail.Amount = Math.Round(_Amount - mFD_Master.Amount, 0);
                                else
                                    mFD_Detail.Amount = Math.Round(GetAmountFormat(CurrentAmount), 0);
                                mFD_Detail.AmountBeforeTax = Math.Round(mFD_Detail.Amount, 0);
                                mFD_Detail.Price = Math.Round(mFD_Detail.Amount / mFD_Detail.Quantity, 0);
                                mFD_Detail.AmountGross = Math.Round(mFD_Detail.Amount, 0);
                                if (j == 0)
                                {
                                    // Tính ra t? giá n?u là dòng d?u
                                    mFD_Detail.AmountMaster = Math.Round(pt.ExchangeCurrency(_BusinessDate, _CurrencyID, _CurrencyLocal, mFD_Detail.Amount), 0);
                                    Rate = mFD_Detail.AmountMaster / mFD_Detail.Amount;
                                    // N?u là dòng d?u -> insert giá tru?c thu?.
                                    mFD_Master.AmountBeforeTax = Math.Round(mFD_Detail.Amount, 0);
                                    mFD_Master.AmountMasterBeforeTax = Math.Round(mFD_Detail.AmountMaster, 0);
                                }
                                else
                                    mFD_Detail.AmountMaster = Math.Round(mFD_Detail.Amount * Rate, 0);

                                mFD_Detail.AmountMasterBeforeTax = Math.Round(mFD_Detail.AmountMaster, 0);
                                mFD_Detail.AmountMasterGross = Math.Round(mFD_Detail.AmountMaster, 0);
                            }
                            else
                            {
                                if ((_TaxInclude == true) && (j == arr.Count - 1))
                                    mFD_Detail.Amount = _Amount - mFD_Master.Amount;
                                else
                                    mFD_Detail.Amount = GetAmountFormat(CurrentAmount);
                                mFD_Detail.AmountBeforeTax = mFD_Detail.Amount;
                                mFD_Detail.Price = mFD_Detail.Amount / mFD_Detail.Quantity;
                                mFD_Detail.AmountGross = mFD_Detail.Amount;
                                if (j == 0)
                                {
                                    // Tính ra t? giá n?u là dòng d?u
                                    mFD_Detail.AmountMaster = pt.ExchangeCurrency(_BusinessDate, _CurrencyID, _CurrencyLocal, mFD_Detail.Amount);
                                    Rate = mFD_Detail.AmountMaster / mFD_Detail.Amount;
                                    // N?u là dòng d?u -> insert giá tru?c thu?.
                                    mFD_Master.AmountBeforeTax = mFD_Detail.Amount;
                                    mFD_Master.AmountMasterBeforeTax = mFD_Detail.AmountMaster;
                                }
                                else
                                    mFD_Detail.AmountMaster = mFD_Detail.Amount * Rate;

                                mFD_Detail.AmountMasterBeforeTax = mFD_Detail.AmountMaster;
                                mFD_Detail.AmountMasterGross = mFD_Detail.AmountMaster;
                            }
                            #endregion

                            #region Insert Du lieu

                            mFD_Detail.InvoiceNo = mFD_Master.InvoiceNo;
                            mFD_Detail.TransactionNo = mFD_Master.TransactionNo;
                            mFD_Detail.RoomID = _RoomID;
                            mFD_Detail.RoomType = RoomType;
                            mFD_Detail.RoomTypeID = RoomTypeID;
                            mFD_Detail.ID = (int)pt.Insert(mFD_Detail);

                            mFD_Master.AmountMaster = mFD_Master.AmountMaster + mFD_Detail.AmountMaster;
                            mFD_Master.Amount = mFD_Master.Amount + mFD_Detail.Amount;

                            #endregion
                        }
                        // Tính giá Gross
                        // Làm tròn VND
                        if (_CurrencyID == "VND")
                        {
                            mFD_Master.AmountGross = Math.Round(mFD_Master.Amount, 0);
                            mFD_Master.AmountMasterGross = Math.Round(mFD_Master.AmountMaster, 0);
                            // Tính giá Net n?u s? ti?n nh?p vào là giá sau thu?
                            if (_TaxInclude == true)
                            {
                                mFD_Master.Amount = Math.Round(_Amount, 0);
                                mFD_Master.AmountMaster = Math.Round(_Amount * Rate, 0);
                            }
                            mFD_Master.Price = Math.Round(mFD_Master.Amount / mFD_Master.Quantity);
                        }
                        else
                        {
                            mFD_Master.AmountGross = mFD_Master.Amount;
                            mFD_Master.AmountMasterGross = mFD_Master.AmountMaster;
                            // Tính giá Net n?u s? ti?n nh?p vào là giá sau thu?
                            if (_TaxInclude == true)
                            {
                                mFD_Master.Amount = _Amount;
                                mFD_Master.AmountMaster = _Amount * Rate;
                            }
                            mFD_Master.Price = mFD_Master.Amount / mFD_Master.Quantity;
                        }
                        pt.Update(mFD_Master);
                        //Update số dư
                        UpdateBalance(_RsvID, FolioID, ref _Message);
                        //Trả về thông tin
                        _AmountReturn = mFD_Master.Amount;
                        _AmountLocalReturn = mFD_Master.AmountMaster;
                        _TransNoReturn = mFD_Master.TransactionNo;
                        // Ghi histoty
                        InsertHistory(_SysDate, _BusinessDate, mFD_Master.FolioID, mFD_Master.FolioID, mFD_Master.InvoiceNo, HistoryType.Night_Post,
                            GetActionText(HistoryType.Night_Post, mFD_Master.TransactionCode, mFD_Master.Description),
                            "$$", mFD_Master.TransactionCode, mFD_Master.Description, mFD_Master.Amount, mFD_Master.Supplement, "", "", "");

                    }
                    #endregion

                    #region Commit-Return
                    pt.CommitTransaction();
                    pt.CloseConnection();
                    return true;
                    #endregion
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                pt.CloseConnection();
                _Message = ex.Message;
                return false;
            }
        }
        private static bool CheckAdvanceBill(int ReservationID, DateTime B_Date)
        {
            if (TextUtils.Select("Select * From AdvanceBill Where ReservationID =" + ReservationID + " ").Rows.Count == 0)
            {
                return false;
            }
            else
            {
                DataTable dt = TextUtils.Select("Select * From AdvanceBill Where datediff(day, DateAdvanceBill, '" + B_Date.ToString("yyyy/MM/dd") + "') >0 And ReservationID =" + ReservationID + " ");
                if (dt.Rows.Count > 0)
                    return false;
                else
                    return true;
            }
        }
        private int _GetPackageID(int ReservationID)
        {
            DataTable dt = TextUtils.Select("Select * From ReservationPackage Where ReservationID = " + ReservationID);
            if (dt.Rows.Count > 0)
                return int.Parse(dt.Rows[0]["PackageID"].ToString());
            else
                return 0;
        }
        private int GetFolioID(int ReservationID)
        {
            DataTable dt = TextUtils.Select("Select ID From Folio Where ReservationID = " + ReservationID);
            if (dt.Rows.Count > 0)
                return TextUtils.ToInt(dt.Rows[0]["ID"].ToString());
            else
                return 0;
        }
        private bool GetPostingRhythm(string status, int pReservationPackageID, DateTime dt)
        {
            bool PostingStatus = false;
            DateTime PostingDate;
            int PostingDay;
            DateTime BusinessDate = TextUtils.GetBusinessDate();
            DataTable tbReservationPackage = TextUtils.Select("Select * From ReservationPackage Where ID = " + pReservationPackageID);
            if (tbReservationPackage.Rows.Count > 0)
            {
                DateTime bd = Convert.ToDateTime(tbReservationPackage.Rows[0]["BeginDate"].ToString());
                DateTime ed = Convert.ToDateTime(tbReservationPackage.Rows[0]["EndDate"].ToString());
                switch (status)
                {
                    case "1"://Every Night
                        {
                            if (TextUtils.CompareDate(bd, dt) <= 0 && TextUtils.CompareDate(ed, dt) >= 0)
                                PostingStatus = true;
                            else
                                PostingStatus = false;
                            break;
                        }
                    case "2"://Date
                        {
                            PostingDate = Convert.ToDateTime(tbReservationPackage.Rows[0]["PostingDate"]);
                            if (TextUtils.CompareDate(PostingDate, BusinessDate) == 0)
                                PostingStatus = true;
                            else
                                PostingStatus = false;
                            break;
                        }
                    case "3"://Day
                        {
                            //C2 - New
                            string str = tbReservationPackage.Rows[0]["PostingDay"].ToString();
                            string[] arr = str.Split(',');
                            PostingStatus = false;
                            for (int d = 0; d < arr.Length; d++)
                            {
                                PostingDay = TextUtils.ToInt(arr[d].ToString()) - 1;
                                DateTime ArrivalDate = bd;
                                if (TextUtils.CompareDate(bd.AddDays(PostingDay), dt) == 0)
                                {
                                    PostingStatus = true;
                                }
                            }

                            break;
                        }
                    case "4"://Checkin
                        {
                            DateTime ArrivalDate = Convert.ToDateTime(tbReservationPackage.Rows[0]["BeginDate"]);
                            if (TextUtils.CompareDate(dt, ArrivalDate) == 0)
                                PostingStatus = true;
                            else
                                PostingStatus = false;
                            break;
                        }
                    case "5"://Check Out
                        {
                            DateTime DepartureDate = Convert.ToDateTime(tbReservationPackage.Rows[0]["EndDate"]);
                            if (TextUtils.CompareDate(dt, DepartureDate.AddDays(-1)) == 0)
                                PostingStatus = true;
                            else
                                PostingStatus = false;
                            break;
                        }
                }

            }
            return PostingStatus;
        }
        private string GetTransactionCodeBB(string ReservationID, string TransactionCode)
        {
            DataTable dt_RoomType = TextUtils.Select("Select * From RoomType with (nolock)");
            DataRow dr_RoomType;
            string rt = "";
            DataTable dt = TextUtils.Select("Select RoomTypeID From Reservation Where ID = " + int.Parse(ReservationID));
            if (dt.Rows.Count > 0)
            {
                dr_RoomType = GetDataRow(dt_RoomType, "ID", dt.Rows[0]["RoomTypeID"].ToString());
                if (dr_RoomType != null)
                {
                    DataTable dtDetail = TextUtils.Select("Select TransactionCode From NightAuditBB Where Code = '" + TransactionCode + "' And Zone = '" + dr_RoomType["ZoneCode"].ToString() + "'");
                    if (dtDetail.Rows.Count > 0)
                    {
                        rt = dtDetail.Rows[0]["TransactionCode"].ToString();
                    }
                    else
                    {
                        rt = TransactionCode;

                    }
                }
            }
            return rt;
        }
        public DataRow GetDataRow(DataTable table, string nameColCheck, object valueColCheck)
        {
            if (null == table) return null;
            if (valueColCheck.ToString() == "") return null;
            DataRow mydatarow = null;
            string sSQL = table.Columns[nameColCheck].ColumnName + "='" + valueColCheck.ToString().Replace("'", "''") + "'";
            table.DefaultView.RowFilter = sSQL;
            if (table.DefaultView.Count > 0)
                mydatarow = table.DefaultView.ToTable().Rows[0];
            table.DefaultView.RowFilter = null;
            return mydatarow;
        }
        private bool GetStatusFolio(int FolioID)
        {
            bool st = false;
            DataTable dt = TextUtils.Select("Select Status From Folio where ID = " + FolioID);
            if (dt.Rows.Count > 0)
            {
                st = Convert.ToBoolean(dt.Rows[0]["Status"]);
            }
            return st;
        }
        public static int GetFolioID(int ReservationID, int WindowNo, string ConfirmationNo)
        {
            try
            {
                //Tim kiem Folio
                Expression exp = new Expression("ReservationID", ReservationID, "=");
                exp = exp.And(new Expression("FolioNo", WindowNo, "="));
                exp = exp.And(new Expression("ConfirmationNo", ConfirmationNo, "="));
                exp = exp.And(new Expression("Status", 0, "="));
                ArrayList arr = FolioBO.Instance.FindByExpression(exp);
                //Kiem tra dieu kien va tra ve ket qua
                if (arr.Count == 0)
                    return 0;
                else
                {
                    return ((FolioModel)arr[0]).ID;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public static void GetAmountSource(string TransactionCode, decimal InputAmount, bool TaxInclude, ref decimal Amount, ref decimal AmountNet)
        {
            try
            {
                #region Lấy danh sách của Generate
                ArrayList arr = GenerateTransactionBO.Instance.FindByAttribute("TransactionCode", TransactionCode);
                #endregion

                #region Nếu có tồn tại trong generate
                if (arr.Count > 0)
                {
                    #region Nếu giá nhập vào là giá đã bao gồm SVC+VAT
                    if (TaxInclude == true)
                    {
                        Amount = GetAmount(arr, InputAmount);
                        AmountNet = InputAmount;
                    }
                    #endregion

                    #region Nếu giá đưa vào là giá ++
                    else
                    {
                        // Khai báo biến
                        GenerateTransactionModel mGT;
                        decimal s1 = 0, s2 = 0, s3 = 0;
                        decimal CurrentAmount = 0;
                        // Thực hiện
                        for (int i = 0; i < arr.Count; i++)
                        {
                            // Đổ dữ liệu vào model
                            mGT = (GenerateTransactionModel)arr[i];
                            // Lấy ra current amount
                            if (mGT.BaseAmount == 0)
                                CurrentAmount = (mGT.Percentage * InputAmount) / 100;
                            else if (mGT.BaseAmount == 1)
                                CurrentAmount = (mGT.Percentage * s1) / 100;
                            else if (mGT.BaseAmount == 2)
                                CurrentAmount = (mGT.Percentage * s2) / 100;
                            else
                                CurrentAmount = (mGT.Percentage * s3) / 100;
                            // Nhặt dữ liệu vào s1,s2,s3
                            if ((mGT.Subtotal1 == true) && (mGT.Subtotal2 == false) && (mGT.Subtotal3 == false))
                            {
                                s1 = s1 + CurrentAmount;
                            }
                            else if ((mGT.Subtotal1 == true) && (mGT.Subtotal2 == true) && (mGT.Subtotal3 == false))
                            {
                                s1 = s1 + CurrentAmount;
                                s2 = CurrentAmount;
                            }
                            else if ((mGT.Subtotal1 == true) && (mGT.Subtotal2 == false) && (mGT.Subtotal3 == true))
                            {
                                s1 = s1 + CurrentAmount;
                                s3 = CurrentAmount;
                            }
                            else if ((mGT.Subtotal1 == true) && (mGT.Subtotal2 == true) && (mGT.Subtotal3 == true))
                            {
                                s1 = s1 + CurrentAmount;
                                s2 = CurrentAmount;
                                s3 = CurrentAmount;
                            }
                            // Tính giá sau thuế
                            AmountNet = AmountNet + CurrentAmount;
                        }
                        // Lấy giá trước thuế
                        Amount = InputAmount;
                    }
                    #endregion
                }
                #endregion

                #region Nếu không tồn tại trong generate
                else
                {
                    Amount = InputAmount;
                    AmountNet = InputAmount;
                }
                #endregion
            }
            catch (Exception ex)
            {

            }
        }
        protected static decimal GetAmount(ArrayList arr, decimal InputAmount)
        {
            #region Khai báo biến

            string s1 = "B0", s2 = "B0", s3 = "B0";
            string BaseAmount = "B";
            string CurrentAmount = "";

            GenerateTransactionModel mGT;

            string result = "";

            #endregion

            for (int i = 0; i < arr.Count; i++)
            {
                #region Do du lieu vao Model
                mGT = (GenerateTransactionModel)arr[i];
                #endregion

                #region Lay ra CurrentAmount

                if (mGT.BaseAmount == 0)
                    CurrentAmount = "B" + Convert.ToString(Convert.ToDecimal(mGT.Percentage) / 100);
                else if (mGT.BaseAmount == 1)
                    CurrentAmount = "B" + (mGT.Percentage * GetNumber(s1)) / 100;
                else if (mGT.BaseAmount == 2)
                    CurrentAmount = "B" + (mGT.Percentage * GetNumber(s2)) / 100;
                else
                    CurrentAmount = "B" + (mGT.Percentage * GetNumber(s3)) / 100;

                #endregion

                #region Lay du lieu vao s1,s2,s3
                if ((mGT.Subtotal1 == true) && (mGT.Subtotal2 == false) && (mGT.Subtotal3 == false))
                {
                    s1 = "B" + (GetNumber(s1) + GetNumber(CurrentAmount));
                }
                else if ((mGT.Subtotal1 == true) && (mGT.Subtotal2 == true) && (mGT.Subtotal3 == false))
                {
                    s1 = "B" + (GetNumber(s1) + GetNumber(CurrentAmount));
                    s2 = CurrentAmount;
                }
                else if ((mGT.Subtotal1 == true) && (mGT.Subtotal2 == false) && (mGT.Subtotal3 == true))
                {
                    s1 = "B" + (GetNumber(s1) + GetNumber(CurrentAmount));
                    s3 = CurrentAmount;
                }
                else if ((mGT.Subtotal1 == true) && (mGT.Subtotal2 == true) && (mGT.Subtotal3 == true))
                {
                    s1 = "B" + (GetNumber(s1) + GetNumber(CurrentAmount));
                    s2 = CurrentAmount;
                    s3 = CurrentAmount;
                }
                #endregion

                if (result.Equals(""))
                    result = CurrentAmount;
                else
                    result = "B" + (GetNumber(result) + GetNumber(CurrentAmount));
            }

            return InputAmount / GetNumber(result);
        }
        public static decimal GetNumber(string InputAmount)
        {
            return Convert.ToDecimal(InputAmount.Trim('B'));
        }
        private bool GetPostingRhythmPackage(string status, int PackageDetailID, DateTime dt)
        {
            bool PostingStatus = false;
            DateTime PostingDate;
            int PostingDay;
            DateTime BusinessDate = TextUtils.GetBusinessDate();
            DataTable tbReservationPackage = TextUtils.Select("Select * From PackageDetail Where ID = " + PackageDetailID);
            if (tbReservationPackage.Rows.Count > 0)
            {
                DateTime bd = Convert.ToDateTime(tbReservationPackage.Rows[0]["BeginDate"].ToString());
                DateTime ed = Convert.ToDateTime(tbReservationPackage.Rows[0]["EndDate"].ToString());
                switch (status)
                {
                    case "1"://Every Night
                        {
                            if (TextUtils.CompareDate(bd, dt) <= 0 && TextUtils.CompareDate(ed, dt) >= 0)
                                PostingStatus = true;
                            else
                                PostingStatus = false;
                            break;
                        }
                    case "2"://Date
                        {
                            PostingDate = Convert.ToDateTime(tbReservationPackage.Rows[0]["PostingDate"]);
                            if (TextUtils.CompareDate(PostingDate, BusinessDate) == 0)
                                PostingStatus = true;
                            else
                                PostingStatus = false;
                            break;
                        }
                    case "3"://Day
                        {
                            //C2 - New
                            string str = tbReservationPackage.Rows[0]["PostingDay"].ToString();
                            string[] arr = str.Split(',');
                            PostingStatus = false;
                            for (int d = 0; d < arr.Length; d++)
                            {
                                PostingDay = TextUtils.ToInt(arr[d].ToString()) - 1;
                                DateTime ArrivalDate = bd;
                                if (TextUtils.CompareDate(bd.AddDays(PostingDay), dt) == 0)
                                {
                                    PostingStatus = true;
                                }
                            }

                            break;
                        }
                    case "4"://Checkin
                        {
                            DateTime ArrivalDate = Convert.ToDateTime(tbReservationPackage.Rows[0]["BeginDate"]);
                            if (TextUtils.CompareDate(dt, ArrivalDate) == 0)
                                PostingStatus = true;
                            else
                                PostingStatus = false;
                            break;
                        }
                    case "5"://Check Out
                        {
                            DateTime DepartureDate = Convert.ToDateTime(tbReservationPackage.Rows[0]["EndDate"]);
                            if (TextUtils.CompareDate(dt, DepartureDate.AddDays(-1)) == 0)
                                PostingStatus = true;
                            else
                                PostingStatus = false;
                            break;
                        }
                }

            }
            return PostingStatus;
        }
        public int GetOrCreateFolioID(DateTime _SysDate, DateTime _BusinessDate, string _ConfirmationNo, int _ReservationID,
                                             int _WindowNo, int _ProfileID, string _AccountName, ref int _ReservationID_Return, ref string _Message, int userID)
        {
            try
            {

                #region Kiểm tra đã có folio này hay chưa
                BaseBusiness.util.Expression exp;

                string sql = string.Empty;
                DataTable dt = null;
                if (_WindowNo < 0)
                {
                    sql = $"SELECT * FROM Folio " +
         $"WHERE ConfirmationNo = '{_ConfirmationNo}' " +
         $"AND FolioNo = '{_WindowNo}'";


                    //exp = new BaseBusiness.util.Expression("ConfirmationNo", _ConfirmationNo, "=");
                    //exp = exp.And(new BaseBusiness.util.Expression("FolioNo", _WindowNo, "="));
                }
                else
                {
                    sql = $"SELECT * FROM Folio " +
        $"WHERE ReservationID = '{_ReservationID}' " +
        $"AND FolioNo = '{_WindowNo}'";
                    //exp = new BaseBusiness.util.Expression("ReservationID", _ReservationID, "=");
                    //exp = exp.And(new BaseBusiness.util.Expression("FolioNo", _WindowNo, "="));
                }

                dt = TextUtils.Select(sql);

                ArrayList arr = new ArrayList();

                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        arr.Add(MapFolio(row));
                    }
                }
                //ArrayList arr = pt.FindByExpression("Folio", exp);
                #endregion
                #region Nếu có rồi thì trả về ID thông tin
                if ((arr != null) && (arr.Count > 0))
                {
                    _ReservationID_Return = ((FolioModel)arr[0]).ReservationID;
                    return ((FolioModel)arr[0]).ID;
                }
                #endregion

                #region Nếu chưa có thì tạo mới
                else
                {
                    FolioModel mF = new FolioModel();
                    mF.ARNo = "";
                    mF.BalanceUSD = 0;
                    mF.ConfirmationNo = _ConfirmationNo;
                    mF.FolioDate = _BusinessDate;
                    mF.CreateDate = _SysDate;
                    mF.UpdateDate = _SysDate;
                    mF.UserInsertID = userID;
                    mF.UserUpdateID = userID;
                    mF.FolioNo = _WindowNo;
                    mF.ProfileID = _ProfileID;
                    mF.AccountName = _AccountName;
                    mF.Status = false;
                    if (_WindowNo < 0)
                    {
                        mF.IsMasterFolio = true;
                        mF.ReservationID = GetOrCreateRsvMaster(_SysDate, _ConfirmationNo, _ReservationID, ref _Message, userID);
                    }
                    else
                    {
                        mF.IsMasterFolio = false;
                        mF.ReservationID = _ReservationID;
                    }
                    if (mF.ReservationID > 0)
                    {
                        _ReservationID_Return = mF.ReservationID;
                        return (int)FolioBO.Instance.Insert(mF);
                    }
                    else
                        return 0;
                }
                #endregion
            }
            catch (Exception ex)
            {
                _Message = ex.Message;
                return 0;
            }
        }
        public int GetOrCreateRsvMaster(DateTime _SysDate, string _ConfirmationNo, int _FromRsvID, ref string _Message, int userID)
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();

                //Kiểm tra xem RsvMA đã có hay chưa
                BaseBusiness.util.Expression exp = new BaseBusiness.util.Expression("ConfirmationNo", _ConfirmationNo, "=");
                exp = exp.And(new BaseBusiness.util.Expression("ReservationNo", "0", "="));
                ArrayList arr = pt.FindByExpression("Reservation", exp);
                //Nếu có rồi thì lấy ra
                if ((arr != null) && (arr.Count > 0))
                    return ((ReservationModel)arr[0]).ID;
                //Nếu chưa có thì tạo mới
                else
                {
                    ReservationModel mR = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(_FromRsvID);
                    mR.Status = 0;
                    mR.MainGuest = false;
                    mR.PostingMaster = true;
                    mR.TotalAmount = 0;
                    mR.NoOfAdult = 0;
                    mR.NoOfChild = 0;
                    mR.NoOfChild1 = 0;
                    mR.NoOfChild2 = 0;
                    mR.NoOfRoom = 1;
                    mR.Rate = 0;
                    mR.CurrencyId = "USD";
                    mR.DropOffReqdId = 0;
                    mR.PickupReqdId = 0;
                    mR.RoomTypeId = 0;
                    mR.RtcId = 0;
                    mR.RoomType = "";
                    mR.RoomId = 0;
                    mR.RoomNo = "";
                    mR.UserInsertId = userID;
                    mR.UserUpdateId = userID;
                    mR.CreateDate = _SysDate;
                    mR.UpdateDate = _SysDate;
                    mR.ProfileIndividualId = 0;
                    mR.ReservationNo = "0";
                    mR.ShareRoom = 0;

                    mR.Status = 1;
                    pt.CommitTransaction();
                    return (int)ReservationBO.Instance.Insert(mR);
                }
            }
            catch (Exception ex)
            {
                pt.RollBack();
                _Message = ex.Message;
                return 0;
            }
        }
        private FolioModel MapFolio(DataRow row)
        {
            if (row == null) return null;

            FolioModel model = new FolioModel();

            model.ID = row["ID"] != DBNull.Value ? Convert.ToInt32(row["ID"]) : 0;
            model.ARNo = row["ARNo"] != DBNull.Value ? row["ARNo"].ToString() : string.Empty;
            model.FolioDate = row["FolioDate"] != DBNull.Value ? Convert.ToDateTime(row["FolioDate"]) : DateTime.MinValue;
            model.FolioNo = row["FolioNo"] != DBNull.Value ? Convert.ToInt32(row["FolioNo"]) : 0;
            model.ReservationID = row["ReservationID"] != DBNull.Value ? Convert.ToInt32(row["ReservationID"]) : 0;
            model.ProfileID = row["ProfileID"] != DBNull.Value ? Convert.ToInt32(row["ProfileID"]) : 0;
            model.AccountName = row["AccountName"] != DBNull.Value ? row["AccountName"].ToString() : string.Empty;
            model.Status = row["Status"] != DBNull.Value && Convert.ToBoolean(row["Status"]);
            model.IsMasterFolio = row["IsMasterFolio"] != DBNull.Value && Convert.ToBoolean(row["IsMasterFolio"]);
            model.ConfirmationNo = row["ConfirmationNo"] != DBNull.Value ? row["ConfirmationNo"].ToString() : string.Empty;
            model.BalanceUSD = row["BalanceUSD"] != DBNull.Value ? Convert.ToDecimal(row["BalanceUSD"]) : 0m;
            model.BalanceVND = row["BalanceVND"] != DBNull.Value ? Convert.ToDecimal(row["BalanceVND"]) : 0m;
            model.IsPrintVAT = row["IsPrintVAT"] != DBNull.Value && Convert.ToBoolean(row["IsPrintVAT"]);
            model.CreateDate = row["CreateDate"] != DBNull.Value ? Convert.ToDateTime(row["CreateDate"]) : DateTime.MinValue;
            model.UpdateDate = row["UpdateDate"] != DBNull.Value ? Convert.ToDateTime(row["UpdateDate"]) : DateTime.MinValue;
            model.UserUpdateID = row["UserUpdateID"] != DBNull.Value ? Convert.ToInt32(row["UserUpdateID"]) : 0;
            model.UserInsertID = row["UserInsertID"] != DBNull.Value ? Convert.ToInt32(row["UserInsertID"]) : 0;

            return model;
        }
        public int CreateFolio(int RoutingID, int UserID)
        {
            try
            {
                RoutingModel mOR = (RoutingModel)RoutingBO.Instance.FindByPrimaryKey(RoutingID);

                FolioModel mF = new FolioModel();
                mF.FolioDate = TextUtils.GetBusinessDate();
                mF.FolioNo = mOR.ToFolioNo;
                mF.ReservationID = mOR.ToReservationID;
                //mF.RoomID = mOR.ToRoomID;
                //if (mOR.ToRoomID != 0)
                //    mF.RoomNo = ((RoomModel)RoomBO.Instance.FindByPK(mOR.ToRoomID)).RoomNo;
                //else
                //    mF.RoomNo = "";
                mF.ProfileID = mOR.ProfileID;
                if (mOR.ProfileID > 0)
                    mF.AccountName = ((ProfileModel)ProfileBO.Instance.FindByPrimaryKey(mOR.ProfileID)).Account;
                else
                    mF.AccountName = "";
                mF.Status = false;
                if (mOR.IsMasterFolio == false)
                    mF.IsMasterFolio = false;
                else
                    mF.IsMasterFolio = true;
                if (mOR.FromReservationID > 0)
                    mF.ConfirmationNo = ((ReservationModel)ReservationBO.Instance.FindByPrimaryKey(mOR.FromReservationID)).ConfirmationNo;
                else
                    mF.ConfirmationNo = mOR.ConfirmationNo;

                mF.UserInsertID = UserID;
                mF.CreateDate = TextUtils.GetSystemDate();
                mF.UserUpdateID = UserID;
                mF.UpdateDate = mF.CreateDate;

                return (int)FolioBO.Instance.Insert(mF);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        #endregion


        #region MapTransaction
        public IActionResult MapTransactionCode()
        {

            return PartialView();
        }
        [HttpGet]
        public IActionResult MapTransactionCodeData()
        {
            try
            {
                string sql = @"SELECT * FROM InterfaceToAcc ORDER BY TransactionCode DESC";

                DataTable dt = TextUtils.Select(sql);

                var result = (from d in dt.AsEnumerable()
                              select new
                              {
                                  ID = d["ID"]?.ToString(),
                                  ProfitCenter = d["ProfitCenter"]?.ToString(),
                                  TransactionCode = d["TransactionCode"]?.ToString(),
                                  Description = d["Description"]?.ToString(),
                                  AccountCode = d["AccountCode"]?.ToString(),
                                  TK_No = d["TK_No"]?.ToString(),
                                  TK_Co = d["TK_Co"]?.ToString(),
                                  TK_Co_Deposit = d["TK_Co_Deposit"]?.ToString(),
                                  TK_No_DT = d["TK_No_DT"]?.ToString(),
                                  MaBP_ACC = d["MaBP_ACC"]?.ToString(),
                                  MaDT_ACC = d["MaDT_ACC"]?.ToString(),
                                  MaCN_ACC = d["MaCN_ACC"]?.ToString(),
                                  Nguoc = d["Nguoc"]?.ToString(),
                                  MaBCDT = d["MaBCDT"]?.ToString(),
                                  PerDT = d["PerDT"]?.ToString(),
                                  MaBPDC = d["MaBPDC"]?.ToString(),
                                  IsSynchronous = d["IsSynchronous"]?.ToString()
                              }).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
        [HttpPost]
        public IActionResult UpdateMapTransactionCode(InterfaceToAccModel model)
        {
            try
            {
                if (model == null)
                {
                    return Json(new { success = false, message = "Invalid data." });
                }

                // INSERT
                if (model.ID == 0)
                {
                    InterfaceToAccBO.Instance.Insert(model);
                }
                // UPDATE
                else
                {
                    InterfaceToAccModel modelDb =
                        (InterfaceToAccModel)InterfaceToAccBO.Instance.FindByPrimaryKey(model.ID);

                    if (modelDb == null)
                    {
                        return Json(new { success = false, message = "Record not found." });
                    }

                    // GÁN LẠI GIÁ TRỊ
                    modelDb.ProfitCenter = model.ProfitCenter;
                    modelDb.TransactionCode = model.TransactionCode;
                    modelDb.Description = model.Description;

                    modelDb.AccountCode = model.AccountCode;
                    modelDb.TK_No = model.TK_No;
                    modelDb.TK_Co = model.TK_Co;
                    modelDb.TK_Co_Deposit = model.TK_Co_Deposit;
                    modelDb.TK_No_DT = model.TK_No_DT;

                    modelDb.MaBP_ACC = model.MaBP_ACC;
                    modelDb.MaDT_ACC = model.MaDT_ACC;
                    modelDb.MaCN_ACC = model.MaCN_ACC;

                    modelDb.PerDT = model.PerDT;
                    modelDb.MaBCDT = model.MaBCDT;
                    modelDb.MaBPDC = model.MaBPDC;

                    InterfaceToAccBO.Instance.Update(modelDb);
                }

                return Json(new
                {
                    success = true,
                    message = "Save successfully."
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
        [HttpPost]
        public IActionResult DeleteTransactionCode(string ID)
        {
            try
            {

                InterfaceToAccBO.Instance.Delete(Convert.ToInt32(ID));



                return Json(new { success = true, message = "AttendantPoint delete successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        #endregion
        #region Tuan_2/2026
        public IActionResult ProcessFOStatus()
        {
            return PartialView();
        }

        [HttpGet]
        public IActionResult GetRoomPFOS(int isVacant)
        {
            try
            {
                DataTable table = _iFrontDeskService.GetRoomPFOS(isVacant);
                var result = (from d in table.AsEnumerable()
                              select d.Table.Columns.Cast<DataColumn>().ToDictionary(
                              col => col.ColumnName,
                              col =>
                              {
                                  var value = d[col.ColumnName];
                                  if (value == DBNull.Value) return null;

                                  // CreatedDate: KHÔNG ToString
                                  if (col.ColumnName == "CreatedDate" || col.ColumnName == "UpdatedDate")
                                      return value;

                                  // Các field khác: ToString
                                  return value.ToString();
                              }
                          )).ToList();
                return Json(result);

            }
            catch (Exception ex)
            {

                return Json(ex.Message);
            }
        }

        [HttpPost]
        public JsonResult UpdateRoomPFOS(int roomId, int roomTypeID, bool isVacant, int userID = 0)
        {
            try
            {
                if (RoomTypeBO.Instance.FindByPrimaryKey(roomTypeID) is RoomTypeModel rt)
                {
                    if (rt.IsPseudo == true)
                        return Json(new { success = false, message = "Dummy room not check." });
                }
                else return Json(new { success = false, message = "Room Type not found." });

                // Valid Room Model
                var roomModel = RoomBO.Instance.FindByPrimaryKey(roomId) as RoomModel;
                if (roomModel == null) return Json(new { success = false, message = "Room not found." });

                // Valid Logic Reservation từ Service
                int resvCount = _iFrontDeskService.GetReservationCount(roomId, isVacant);

                if (isVacant && resvCount != 0)
                    return Json(new { success = false, message = "Room is Occupied. Please check again reservation." });

                if (!isVacant && resvCount == 0)
                    return Json(new { success = false, message = "Room is vacant. Please check again reservation." });

                // Nếu tất cả Valid -> Gọi Service thực thi Update
                var result = _iFrontDeskService.ExecuteUpdateStatus(roomModel, isVacant, userID);

                return Json(new
                {
                    success = true,
                    message = $"Changed FOStatus Room No. {result.roomNo} to {result.statusName} successfully."
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }
        #endregion
    }
}

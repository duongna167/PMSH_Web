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
            var sortedList = tlplist.OrderBy(x => x.Name).ToList();
            ViewBag.TelephoneBookCategoryList = sortedList;
            DataTable dataTable = _iFrontDeskService.TelephoneBook("","","");
            var result = (from d in dataTable.AsEnumerable()
                          select new
                          {
                              id = d["id"]?.ToString(),
                              name = d["name"]?.ToString(),

                          }).ToList();
            ViewBag.TelephoneSelect = result;
            return View();
        }
        [HttpGet]
        public JsonResult GetTelephoneBook(string CategoryCode,string BookCode)
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
        public JsonResult GetTelephoneDetail(int id)
        {
            try
            {
                string sql = @"SELECT ID, Name, Telephone, Address, Remark, Color
               FROM TelephoneBook
               WHERE ID > 0";

                if (id != 0)
                {
                    sql += " AND TelephoneBookCategoryID = " + id;
                }

                sql += " ORDER BY Name";

                DataTable dt = TextUtils.Select(sql);
                return Json(dt);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult InsertTelephoneBook(string name, string telephone, string address, string remark, string webAddress, int categoryId, int color)
        {
            try
            {
                var vnPhoneRegex = new System.Text.RegularExpressions.Regex(@"^((0|\+84)(3|5|7|8|9)[0-9]{8}|0\d{2,3}\d{7,8})$");

                if (string.IsNullOrWhiteSpace(telephone) || !vnPhoneRegex.IsMatch(telephone))
                {
                    return Json(new { success = false, message = "Số điện thoại Việt Nam không hợp lệ (di động hoặc máy bàn)!" });
                }
                int userId = HttpContext.Session.GetInt32("UserID") ?? 0;

                var model = new TelephoneBookModel
                {
                    Name = name,
                    Telephone = telephone,
                    Address = address,
                    Remark = remark,
                    WebAddress = webAddress,
                    TelephoneBookCategoryID = categoryId,
                    Color = color,
                    CreateDate = DateTime.Now,
                    UserInsertID = userId
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
    string remark, string webAddress, int categoryId, int color)
        {
            try
            {
                int userId = HttpContext.Session.GetInt32("UserID") ?? 0;

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
                    UserUpdateID = userId,
                    UpdateDate = DateTime.Now
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
            return View(); // View này sẽ chứa DataGrid + script gọi API
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
        public IActionResult WakeUpCallFindRoom(string roomNoset, string reservationHolder, string zone,string confirmNo)
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
        public IActionResult WakeUpCallSearch(DateTime currentDate, string searchforName, int  isSpecial)
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
        public IActionResult ViewWakeUpCall(string name, string group, string roomview,DateTime fromDateview,DateTime toDateview,string timeDailyview,int  roomClass)
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
                if (request.callType== "date")//insert 1 ngày cho phòng ko phải Group
                {

                    for (int i = 0; i < request.selectedRows.Count-1; i++)
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
                        string description = "Wake up call set for room: " + row.Room + ", Time: " + request.timeDate+ " from " + request.singleDate.ToShortDateString() + " to " + request.singleDate.ToShortDateString();
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
            if(dt.Rows.Count>0)
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


    }
}

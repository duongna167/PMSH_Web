using BaseBusiness.BO;
using BaseBusiness.Model;
using BaseBusiness.util;
using DevExpress.Charts.Native;
using DevExpress.Data.ODataLinq;
using DevExpress.DataAccess.DataFederation;
using DevExpress.XtraCharts.Native;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Miscellaneous.Hubs;
using Miscellaneous.Services.Implements;
using Miscellaneous.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Policy;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using static BaseBusiness.util.ValidationUtils;
using static DevExpress.CodeParser.CodeStyle.Formatting.Rules;
using static System.Runtime.InteropServices.JavaScript.JSType;
namespace Miscellaneous.Controllers
{
    public class MiscellaneousController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<MiscellaneousController> _logger;
        private readonly IMemoryCache _cache;
        private readonly IMiscellaneousService _iMiscellaneousService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHubContext<TagScanHub> _hubContext;

        // ===== RFID debounce (500ms) =====
        private static readonly object _rfidLock = new();
        private static string _lastRfidCode = null;
        private static DateTime _lastRfidTime = DateTime.MinValue;
        private const int RFID_DEBOUNCE_MS = 500;

        public MiscellaneousController(ILogger<MiscellaneousController> logger,
             IMemoryCache cache, IConfiguration configuration, IMiscellaneousService iMiscellaneousService, IHttpContextAccessor httpContextAccessor, IHubContext<TagScanHub> hubContext)
        {
            _cache = cache;
            _logger = logger;
            _configuration = configuration;
            _iMiscellaneousService = iMiscellaneousService;
            _httpContextAccessor = httpContextAccessor;
            _hubContext = hubContext;
        }

        #region CardManagement

        public IActionResult CardManagement()
        {
            int baudRate = 9600;
            TagScannerHelper.Instance.CodeReceived += MainForm_TagScanCodeReceived;

            // Ensure connection from settings (will show message if missing)
            TagScannerHelper.Instance.ConnectFromSettings(_configuration, baudRate);
            return View();
        }
        [HttpGet]
        public IActionResult GetCardManagement()
        {
            string sql = @"SELECT a.*, N'Card Hotel' AS CardType, 
                    CASE 
                        WHEN a.Status = 0 THEN N'Inactive' 
                        WHEN a.Status = 1 THEN N'Active' 
                        ELSE N'Other' 
                    END AS StatusText 
                    FROM Card a WITH (NOLOCK)";

            var dt = _iMiscellaneousService.CardManagement(sql);

            // Convert DataTable -> List<object>
            var list = dt.AsEnumerable().Select(row => new {
                ID = row["ID"],
                CardType = row["CardType"],
                CardTypeID = row["CardTypeID"],
                CanSell = row["CanSell"],
                Status = row["Status"],
                StatusText = row["StatusText"],
                CreatedBy = row["CreatedBy"],
                CreatedDate = row["CreatedDate"],
                UpdatedBy = row["UpdatedBy"],
                UpdatedDate = row["UpdatedDate"]
            }).ToList();

            return Json(list);
        }
        [HttpPost]
        public IActionResult SaveCard([FromBody] CardModel model)
        {
            var listErrors = GetErrors(
                Check(model == null, "general", "Invalid data."),
                Check(string.IsNullOrWhiteSpace(model?.ID), "cardID", "Card ID is required.")
            );

            if (listErrors.Count > 0)
            {
                return Json(new { success = false, errors = listErrors });
            }

            try
            {
                var existedCards = CardBO.Instance.FindByAttribute("ID", model.ID);
                bool isExist = existedCards != null && existedCards.Count > 0;

                var businessDates = PropertyUtils.ConvertToList<BusinessDateModel>(
                    BusinessDateBO.Instance.FindAll()
                );
                DateTime businessDate = businessDates[0].BusinessDate;

                // ================= INSERT =================
                if (!isExist)
                {
                    if (CardBO.Instance.IsDuplicateCardId(model.ID))
                    {
                        return Json(new
                        {
                            success = false,
                            message = $"Card ID already exists: [{model.ID}]"
                        });
                    }

                    model.CreatedDate = businessDate;
                    model.UpdatedDate = DateTime.Now;
                    string statusTextInsert = model.Status switch
                    {
                        1 => "Active",
                        0 => "Inactive",
                        _ => "Other"
                    };

                    CardBO.Instance.InsertCard(model);

                    return Json(new
                    {
                        success = true,
                        message = "Insert successfully!",
                        data = new
                        {
                            id = model.ID,
                            cardTypeID = model.CardTypeID,
                            cardType = "Card Hotel",
                            status = model.Status,
                            statusText = statusTextInsert,
                            createdBy = model.CreatedBy,
                            createdDate = model.CreatedDate,
                            updatedBy = model.UpdatedBy,
                            updatedDate = model.UpdatedDate
                        }
                    });
                }

                // ================= UPDATE =================
                var oldData = (CardModel)existedCards[0];

                model.CreatedBy = oldData.CreatedBy;
                model.CreatedDate = oldData.CreatedDate;
                model.UpdatedDate = DateTime.Now;
                string statusTextEdit = model.Status switch
                {
                    1 => "Active",
                    0 => "Inactive",
                    _ => "Other"
                };
                CardBO.Instance.Update(model);

                return Json(new
                {
                    success = true,
                    message = "Update successfully!",
                    data = new
                    {
                        id = model.ID,
                        cardTypeID = model.CardTypeID,
                        cardType = "Card Hotel",
                        status = model.Status,
                        statusText = statusTextEdit,
                        createdBy = model.CreatedBy,
                        createdDate = model.CreatedDate,
                        updatedBy = model.UpdatedBy,
                        updatedDate = model.UpdatedDate
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        [HttpPost]
        public IActionResult DeleteCard([FromBody] CardModel model)
        {
            var username = HttpContext.Session.GetString("LoginName") ?? "";

            using (var conn = new SqlConnection(DBUtils.GetDBConnectionString()))
            {
                conn.Open();

                // Check xem Card có tồn tại chưa
                string checkSql = "SELECT COUNT(*) FROM Card WHERE ID = @ID";
                using (var checkCmd = new SqlCommand(checkSql, conn))
                {
                    checkCmd.Parameters.AddWithValue("@ID", model.ID);
                    int count = (int)checkCmd.ExecuteScalar();
                    if (count == 0)
                    {
                        return Json(new { success = false, message = "Card ID does not exist" });
                    }
                }

                // Xóa bản ghi
                string sql = "DELETE FROM Card WHERE ID = @ID";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@ID", model.ID);

                    int rows = cmd.ExecuteNonQuery();
                    return Ok(new { success = rows > 0, id = model.ID });
                }
            }
        }
        #endregion

        private void MainForm_TagScanCodeReceived(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return;

            // Convert 10 số → 8 số (nếu cần)
            var finalCode = TagScannerHelper.Instance.Convert10To8TagNo(code);

            lock (_rfidLock)
            {
                var now = DateTime.UtcNow;

                // Chặn scan trùng trong 500ms
                if (_lastRfidCode == finalCode &&
                    (now - _lastRfidTime).TotalMilliseconds < RFID_DEBOUNCE_MS)
                {
                    return;
                }

                _lastRfidCode = finalCode;
                _lastRfidTime = now;
            }

            //  Đẩy realtime sang client
            _hubContext.Clients.All.SendAsync("ReceiveCode", finalCode);
        }

        [HttpGet]
        public IActionResult TestRFID(string code)
        {
            MainForm_TagScanCodeReceived(code);
            return Ok("sent");
        }

        #region Meal Report
        public IActionResult MealReport()
        {
            return View();
        }
        [HttpGet]
        public IActionResult GetMealReport(DateTime fromDate, DateTime toDate, int type)
        {
            try
            {
                DataTable dataTable = _iMiscellaneousService.MealReport(fromDate, toDate, type);
                var result = (from d in dataTable.AsEnumerable()
                              select new    
                              {
                                  STT = !string.IsNullOrEmpty(d["STT"].ToString()) ? d["STT"] : "",
                                  ID = !string.IsNullOrEmpty(d["ID"].ToString()) ? d["ID"] : "",
                                  FromMeal = !string.IsNullOrEmpty(d["FromMeal"].ToString()) ? d["FromMeal"] : "",
                                  ToMeal = !string.IsNullOrEmpty(d["ToMeal"].ToString()) ? d["ToMeal"] : "",
                                  BreakFast = !string.IsNullOrEmpty(d["BreakFast"].ToString()) ? d["BreakFast"] : "",
                                  Lunch = !string.IsNullOrEmpty(d["Lunch"].ToString()) ? d["Lunch"] : "",
                                  Dinner = !string.IsNullOrEmpty(d["Dinner"].ToString()) ? d["Dinner"] : "",
                                  Type = !string.IsNullOrEmpty(d["Type"].ToString()) ? d["Type"] : "",
                                  FromDate = !string.IsNullOrEmpty(d["FromDate"].ToString()) ? d["FromDate"] : "",
                                  ToDate = !string.IsNullOrEmpty(d["ToDate"].ToString()) ? d["ToDate"] : "",
                                  ReservationID = !string.IsNullOrEmpty(d["ReservationID"].ToString()) ? d["ReservationID"] : "",
                                  Account = !string.IsNullOrEmpty(d["Account"].ToString()) ? d["Account"] : "",
                                  RoomNo = !string.IsNullOrEmpty(d["RoomNo"].ToString()) ? d["RoomNo"] : "",
                                  CardID = !string.IsNullOrEmpty(d["CardID"].ToString()) ? d["CardID"] : "",
                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
            //  report.DataSource = dataTable;

            // Không cần gán parameter
            // report.RequestParameters = false;

            // return PartialView("_ReportViewerPartial", report);
        }
        [HttpGet]
        public IActionResult GetMealSearchCustom(DateTime fromDate, DateTime toDate, string mealShiftID, string roomNo)
        {
            try
            {
                DataTable dataTable = _iMiscellaneousService.ReportCustom(fromDate, toDate, mealShiftID, roomNo);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  STT = !string.IsNullOrEmpty(d["STT"].ToString()) ? d["STT"] : "",
                                  ID = !string.IsNullOrEmpty(d["ID"].ToString()) ? d["ID"] : "",
                                  ReservationID = !string.IsNullOrEmpty(d["ReservationID"].ToString()) ? d["ReservationID"] : "",
                                  UsingDate = !string.IsNullOrEmpty(d["UsingDate"].ToString()) ? d["UsingDate"] : "",
                                  RoomNo = !string.IsNullOrEmpty(d["RoomNo"].ToString()) ? d["RoomNo"] : "",
                                  RoomType = !string.IsNullOrEmpty(d["RoomType"].ToString()) ? d["RoomType"] : "",
                                  Account = !string.IsNullOrEmpty(d["Account"].ToString()) ? d["Account"] : "",
                                  ProfileID = !string.IsNullOrEmpty(d["ProfileID"].ToString()) ? d["ProfileID"] : "",
                                  MealShift = !string.IsNullOrEmpty(d["MealShift"].ToString()) ? d["MealShift"] : "",
                                  Zone = !string.IsNullOrEmpty(d["Zone"].ToString()) ? d["Zone"] : "",
                                  RestaurantName = !string.IsNullOrEmpty(d["RestaurantName"].ToString()) ? d["RestaurantName"] : "",
                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        
        [HttpGet]
        public IActionResult GetReportCancelMeal(DateTime fromDate, DateTime toDate)
        {
            try
            {


                DataTable dataTable = _iMiscellaneousService.ReportCancelMeal(fromDate, toDate);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  STT = !string.IsNullOrEmpty(d["STT"].ToString()) ? d["STT"] : "",
                                  ID = !string.IsNullOrEmpty(d["ID"].ToString()) ? d["ID"] : "",
                                  ReservationID = !string.IsNullOrEmpty(d["ReservationID"].ToString()) ? d["ReservationID"] : "",
                                  CancelDate = !string.IsNullOrEmpty(d["CancelDate"].ToString()) ? d["CancelDate"] : "",
                                  RoomNo = !string.IsNullOrEmpty(d["RoomNo"].ToString()) ? d["RoomNo"] : "",
                                  RoomType = !string.IsNullOrEmpty(d["RoomType"].ToString()) ? d["RoomType"] : "",
                                  Account = !string.IsNullOrEmpty(d["Account"].ToString()) ? d["Account"] : "",
                                  ProfileID = !string.IsNullOrEmpty(d["ProfileID"].ToString()) ? d["ProfileID"] : "",
                                  CardID = !string.IsNullOrEmpty(d["CardID"].ToString()) ? d["CardID"] : "",
                                  MealShift = !string.IsNullOrEmpty(d["MealShift"].ToString()) ? d["MealShift"] : "",
                                  Zone = !string.IsNullOrEmpty(d["Zone"].ToString()) ? d["Zone"] : "",
                                  RestaurantName = !string.IsNullOrEmpty(d["RestaurantName"].ToString()) ? d["RestaurantName"] : "",
                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
            //  report.DataSource = dataTable;

            // Không cần gán parameter
            // report.RequestParameters = false;

            // return PartialView("_ReportViewerPartial", report);
        }
       
        [HttpGet]
       
        public IActionResult GetReportWaiveMeal(DateTime fromDate, DateTime toDate, string mealShift, string roomNo)
        {
            try
            {


                DataTable dataTable = _iMiscellaneousService.ReportWaiveMeal(fromDate, toDate, mealShift, roomNo);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                      
                                  ReservationID = !string.IsNullOrEmpty(d["ReservationID"].ToString()) ? d["ReservationID"] : "",
                                  RateDate = !string.IsNullOrEmpty(d["RateDate"].ToString()) ? d["RateDate"] : "",
                                  BreakFast = !string.IsNullOrEmpty(d["BreakFast"].ToString()) ? d["BreakFast"] : "",
                                  Lunch = !string.IsNullOrEmpty(d["Lunch"].ToString()) ? d["Lunch"] : "",
                                  Dinner = !string.IsNullOrEmpty(d["Dinner"].ToString()) ? d["Dinner"] : "",
                                  RoomNo = !string.IsNullOrEmpty(d["RoomNo"].ToString()) ? d["RoomNo"] : "",
                                  Account = !string.IsNullOrEmpty(d["Account"].ToString()) ? d["Account"] : "",
                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
            //  report.DataSource = dataTable;

            // Không cần gán parameter
            // report.RequestParameters = false;

            // return PartialView("_ReportViewerPartial", report);
        }
       
        [HttpGet]
        public IActionResult GetReportUsedBreakFastMeal(DateTime fromDate, DateTime toDate, string roomNo)
        {
            try
            {


                DataTable dataTable = _iMiscellaneousService.ReportUsedBreakFastMeal(fromDate, toDate, roomNo);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {

                                  STT = !string.IsNullOrEmpty(d["STT"].ToString()) ? d["STT"] : "",
                                  ID = !string.IsNullOrEmpty(d["ID"].ToString()) ? d["ID"] : "",
                                  ReservationID = !string.IsNullOrEmpty(d["ReservationID"].ToString()) ? d["ReservationID"] : "",
                                  UsingDate = !string.IsNullOrEmpty(d["UsingDate"].ToString()) ? d["UsingDate"] : "",
                                  RoomNo = !string.IsNullOrEmpty(d["RoomNo"].ToString()) ? d["RoomNo"] : "",
                                  RoomType = !string.IsNullOrEmpty(d["RoomType"].ToString()) ? d["RoomType"] : "",
                                  Account = !string.IsNullOrEmpty(d["Account"].ToString()) ? d["Account"] : "",
                                  ProfileID = !string.IsNullOrEmpty(d["ProfileID"].ToString()) ? d["ProfileID"] : "",
                                  MealShift = !string.IsNullOrEmpty(d["MealShift"].ToString()) ? d["MealShift"] : "",
                                  Zone = !string.IsNullOrEmpty(d["Zone"].ToString()) ? d["Zone"] : "",
                                  RestaurantName = !string.IsNullOrEmpty(d["RestaurantName"].ToString()) ? d["RestaurantName"] : "",
                                  Amount = !string.IsNullOrEmpty(d["Amount"].ToString()) ? d["Amount"] : "",
                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
            //  report.DataSource = dataTable;

            // Không cần gán parameter
            // report.RequestParameters = false;

            // return PartialView("_ReportViewerPartial", report);
        }
        [HttpGet]
        public IActionResult GetReportUseBreakFastMeal(DateTime fromDate, DateTime toDate, string roomNo)
        {
            try
            {


                DataTable dataTable = _iMiscellaneousService.ReportUseBreakFastMeal(fromDate, toDate, roomNo);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {

                                  STT = !string.IsNullOrEmpty(d["STT"].ToString()) ? d["STT"] : "",              
                                  UsingDate = !string.IsNullOrEmpty(d["UsingDate"].ToString()) ? d["UsingDate"] : "",
                                  MealShift = !string.IsNullOrEmpty(d["MealShift"].ToString()) ? d["MealShift"] : "",
                                  Account = !string.IsNullOrEmpty(d["Account"].ToString()) ? d["Account"] : "",
                                  RoomNo = !string.IsNullOrEmpty(d["RoomNo"].ToString()) ? d["RoomNo"] : "",
                                  Zone = !string.IsNullOrEmpty(d["Zone"].ToString()) ? d["Zone"] : "",                    
                                  Amount = !string.IsNullOrEmpty(d["Amount"].ToString()) ? d["Amount"] : "",
                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
            //  report.DataSource = dataTable;

            // Không cần gán parameter
            // report.RequestParameters = false;

            // return PartialView("_ReportViewerPartial", report);
        }
        #endregion

        #region Guest Card
        [HttpGet]
        public IActionResult GetIssuingCardToGuests(string firstName, string confirmationNo, string crsNo, string roomNo, string zone, string guestName, string rsvHolder, string isShowRS, string isShowCard, DateTime arrFrom, DateTime arrTo, string status, string ci_Day, string co_Day, string findCardID, string reservationID)
        {
            try
            {


                DataTable dataTable = _iMiscellaneousService.IssuingCardToGuests(firstName, confirmationNo, crsNo, roomNo, zone, guestName, rsvHolder, isShowRS, isShowCard, arrFrom, arrTo, status, ci_Day, co_Day, findCardID, reservationID);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {

                                  Check = !string.IsNullOrEmpty(d["Check"].ToString()) ? d["Check"] : "",
                                  CardID = !string.IsNullOrEmpty(d["CardID"].ToString()) ? d["CardID"] : "",
                                  ConfirmationNo = !string.IsNullOrEmpty(d["ConfirmationNo"].ToString()) ? d["ConfirmationNo"] : "",
                                  MainGuest = !string.IsNullOrEmpty(d["MainGuest"].ToString()) ? d["MainGuest"] : "",
                                  Nat = !string.IsNullOrEmpty(d["Nat"].ToString()) ? d["Nat"] : "",
                                  Title = !string.IsNullOrEmpty(d["Title"].ToString()) ? d["Title"] : "",
                                  Account = !string.IsNullOrEmpty(d["Account"].ToString()) ? d["Account"] : "",
                                  DateOfBirth = !string.IsNullOrEmpty(d["DateOfBirth"].ToString()) ? d["DateOfBirth"] : "",
                                  PassPort = !string.IsNullOrEmpty(d["PassPort"].ToString()) ? d["PassPort"] : "",
                                  IdentityCard = !string.IsNullOrEmpty(d["IdentityCard"].ToString()) ? d["IdentityCard"] : "",
                                  RoomNo = !string.IsNullOrEmpty(d["RoomNo"].ToString()) ? d["RoomNo"] : "",
                                  RoomType = !string.IsNullOrEmpty(d["RoomType"].ToString()) ? d["RoomType"] : "",
                                  ArrivalDate = !string.IsNullOrEmpty(d["ArrivalDate"].ToString()) ? d["ArrivalDate"] : "",
                                  NoOfNight = !string.IsNullOrEmpty(d["NoOfNight"].ToString()) ? d["NoOfNight"] : "",
                                  DepartureDate = !string.IsNullOrEmpty(d["DepartureDate"].ToString()) ? d["DepartureDate"] : "",
                                  Package = !string.IsNullOrEmpty(d["Package"].ToString()) ? d["Package"] : "",
                                  NoOfAdult = !string.IsNullOrEmpty(d["NoOfAdult"].ToString()) ? d["NoOfAdult"] : "",
                                  NoOfChild = !string.IsNullOrEmpty(d["NoOfChild"].ToString()) ? d["NoOfChild"] : "",
                                  NoOfChild1 = !string.IsNullOrEmpty(d["NoOfChild1"].ToString()) ? d["NoOfChild1"] : "",
                                  NoOfChild2 = !string.IsNullOrEmpty(d["NoOfChild2"].ToString()) ? d["NoOfChild2"] : "",
                                  StatusText = !string.IsNullOrEmpty(d["StatusText"].ToString()) ? d["StatusText"] : "",
                                  ReservationHolder = !string.IsNullOrEmpty(d["ReservationHolder"].ToString()) ? d["ReservationHolder"] : "",
                                  Address = !string.IsNullOrEmpty(d["Address"].ToString()) ? d["Address"] : "",
                                  ProfileID = !string.IsNullOrEmpty(d["ProfileID"].ToString()) ? d["ProfileID"] : "",
                                  ReservationID = !string.IsNullOrEmpty(d["ReservationID"].ToString()) ? d["ReservationID"] : "",
                                  IsCard = !string.IsNullOrEmpty(d["IsCard"].ToString()) ? d["IsCard"] : "",
                                  Status = !string.IsNullOrEmpty(d["Status"].ToString()) ? d["Status"] : "",
                                  Packages = !string.IsNullOrEmpty(d["Packages"].ToString()) ? d["Packages"] : "",
                                  Breakfast = !string.IsNullOrEmpty(d["Breakfast"].ToString()) ? d["Breakfast"] : "",
                                  Lunch = !string.IsNullOrEmpty(d["Lunch"].ToString()) ? d["Lunch"] : "",
                                  Dinner = !string.IsNullOrEmpty(d["Dinner"].ToString()) ? d["Dinner"] : "",
                                  ShareRoom = !string.IsNullOrEmpty(d["ShareRoom"].ToString()) ? d["ShareRoom"] : "",
                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
            //  report.DataSource = dataTable;

            // Không cần gán parameter
            // report.RequestParameters = false;

            // return PartialView("_ReportViewerPartial", report);
        }
        public IActionResult IssuingCardToGuests()
        {
            List<ZoneModel> listzo = PropertyUtils.ConvertToList<ZoneModel>(ZoneBO.Instance.FindAll());
            ViewBag.ZoneList = listzo;
            int baudRate = 9600;
            TagScannerHelper.Instance.CodeReceived += MainForm_TagScanCodeReceived;
      
            // Ensure connection from settings (will show message if missing)
            TagScannerHelper.Instance.ConnectFromSettings(_configuration, baudRate);
            return View();

        }

        [HttpPost]
        public IActionResult AddCard([FromBody] AddCardRequest request)
        {
            var username = HttpContext.Session.GetString("LoginName") ?? "";
            int userId = HttpContext.Session.GetInt32("UserID") ?? 0;

            using (var conn = new SqlConnection(DBUtils.GetDBConnectionString()))
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        var reservationID = request.Reservation.ID;
                        var cardID = request.Reservation.CardId;

                        // Kiểm tra cardID có tồn tại trong bảng Card không
                        var checkCmd = new SqlCommand("SELECT COUNT(1) FROM dbo.Card WHERE ID = @CardID", conn, tran);
                        checkCmd.Parameters.AddWithValue("@CardID", cardID);
                        int exists = (int)checkCmd.ExecuteScalar();

                        if (exists == 0)
                        {
                            tran.Rollback();
                            return BadRequest(new { message = "This CardID does not exist in the system, cannot be updated!" });
                        }

                        // Nếu tồn tại mới update Reservation và Card
                        var cmd = new SqlCommand(@"
                            UPDATE dbo.Reservation SET CardID = @CardID WHERE ID = @ReservationID;
                            UPDATE dbo.Card SET CanSell = 0 WHERE ID = @CardID;

                            INSERT INTO ActivityLog(TableName,ObjectID,UserID,UserName,ChangeDate,Change,OldValue,NewValue,Description)
                            VALUES ('Reservation', @ReservationID, @UserID, @UserName, GETDATE(), 'CardID', '', @CardID, 'Add card');

                            INSERT INTO Interface(KeyValue,Description,CreateDate)
                            VALUES ('SCN', 'SCN|R#' + CAST(@ReservationID AS NVARCHAR(10)) + '|CA' + @CardID + '|SR' + CAST(@ReservationID AS NVARCHAR(10)) + '|', GETDATE());
                        ", conn, tran);

                        cmd.Parameters.AddWithValue("@ReservationID", reservationID);
                        cmd.Parameters.AddWithValue("@CardID", cardID);
                        cmd.Parameters.AddWithValue("@UserID", userId);
                        cmd.Parameters.AddWithValue("@UserName", username);

                        cmd.ExecuteNonQuery();
                        tran.Commit();

                        return Ok(new { message = "Card added successfully" });
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        return StatusCode(500, new { error = ex.Message });
                    }
                }
            }
        }

        public class AddCardRequest
        {
            public ReservationModel Reservation { get; set; }
            public CardModel Card { get; set; }
        }

        [HttpPost]
        public IActionResult CancelCard([FromBody] AddCardRequest request)
        {
            var username = HttpContext.Session.GetString("LoginName") ?? "";
            int userId = HttpContext.Session.GetInt32("UserID") ?? 0;

            using (var conn = new SqlConnection(DBUtils.GetDBConnectionString()))
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        var reservationID = request.Reservation.ID;
                        var cardID = request.Reservation.CardId;

                        // Nếu tồn tại mới update Reservation và Card
                        var cmd = new SqlCommand(@"
                            UPDATE dbo.Reservation SET CardID = '' WHERE ID = @ReservationID;
                            UPDATE dbo.Card SET CanSell = 0 WHERE ID = @CardID;

                            INSERT INTO ActivityLog(TableName,ObjectID,UserID,UserName,ChangeDate,Change,OldValue,NewValue,Description)

                            VALUES ('Reservation', @ReservationID, @UserID, @UserName, GETDATE(), 'CardID', @CardID, '', 'Cancel card');

                            INSERT INTO Interface(KeyValue,Description,CreateDate)
                            VALUES ('SCN', 'SCN|R#' + CAST(@ReservationID AS NVARCHAR(10)) + '|CA' + @CardID + '|SR' + CAST(@ReservationID AS NVARCHAR(10)) + '|', GETDATE());
                        ", conn, tran);

                        cmd.Parameters.AddWithValue("@ReservationID", reservationID);
                        cmd.Parameters.AddWithValue("@CardID", cardID);
                        cmd.Parameters.AddWithValue("@UserID", userId);
                        cmd.Parameters.AddWithValue("@UserName", username);

                        cmd.ExecuteNonQuery();
                        tran.Commit();

                        return Ok(new { message = "Card cancel successfully" });
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        return StatusCode(500, new { error = ex.Message });
                    }
                }
            }
        }
        #endregion

    }
}

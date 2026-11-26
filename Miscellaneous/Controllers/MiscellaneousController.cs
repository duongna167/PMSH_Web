using BaseBusiness.Auth;
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

        [HttpGet, RequireFunction("misc_CardManagement")]
        public IActionResult CardManagement()
        {
            int baudRate = 9600;
            TagScannerHelper.Instance.CodeReceived += MainForm_TagScanCodeReceived;

            // Ensure connection from settings (will show message if missing)
            TagScannerHelper.Instance.ConnectFromSettings(_configuration, baudRate);
            return View();
        }

        [HttpGet, RequireFunction("misc_CardManagement")]
        public IActionResult GetCardManagement() =>
            CardAction(() => Json(CardRows(_iMiscellaneousService.Cards())));

        // isNew = true: thêm thẻ (quét/nhập đủ mã), false: sửa loại thẻ và trạng thái
        [HttpPost, ValidateAntiForgeryToken, RequireFunction("misc_CardManagement")]
        public IActionResult SaveCard(string? id, int cardTypeId, int status, bool isNew)
        {
            id = id?.Trim() ?? "";
            if (id.Length == 0) return CardError("ID cannot be blank!");
            if (isNew && (id.Length != 8 || !id.All(char.IsAsciiDigit))) return CardError("Card ID must be 8 digits.");
            if (cardTypeId != 0) return CardError("Card type is invalid.");
            if (status is < 0 or > 2) return CardError("Status is invalid.");

            return CardAction(() =>
            {
                var userName = HttpContext.Session.GetString("LoginName") ?? "";
                if (isNew && !_iMiscellaneousService.InsertCard(id, cardTypeId, status, userName))
                    return CardError("This card already exists on the system.");
                if (!isNew && !_iMiscellaneousService.UpdateCard(id, cardTypeId, status, userName))
                    return CardError("Card ID does not exist.");

                return Json(new { success = true, data = CardRows(_iMiscellaneousService.Cards(id)).FirstOrDefault() });
            });
        }

        [HttpPost, ValidateAntiForgeryToken, RequireFunction("misc_CardManagement")]
        public IActionResult DeleteCard(string? id)
        {
            if (string.IsNullOrWhiteSpace(id)) return CardError("Please select a card.");
            return CardAction(() => _iMiscellaneousService.DeleteCard(id.Trim()) switch
            {
                CardDeleteResult.InUse => CardError("Card is used. Not delete!"),
                CardDeleteResult.NotFound => CardError("Card ID does not exist."),
                _ => Json(new { success = true })
            });
        }

        private static List<Dictionary<string, object?>> CardRows(DataTable table) =>
            table.AsEnumerable().Select(row => table.Columns.Cast<DataColumn>().ToDictionary(
                col => char.ToLowerInvariant(col.ColumnName[0]) + col.ColumnName[1..],
                col => row[col] == DBNull.Value ? null : row[col])).ToList();

        private IActionResult CardError(string message) => BadRequest(new { success = false, message });

        private IActionResult CardAction(Func<IActionResult> action)
        {
            try
            {
                return action();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Card management failed");
                return StatusCode(500, new { success = false, message = "Cannot process card. Please check the database configuration." });
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
        [HttpGet, RequireFunction("misc_ReportMeal")]
        public IActionResult MealReport()
        {
            ViewBag.MealBusinessDate = _iMiscellaneousService.GetMealBusinessDate().ToString("yyyy-MM-dd");
            return PartialView("~/Views/Miscellaneous/MealReport.cshtml");
        }

        [HttpGet, RequireFunction("misc_ReportMeal")]
        public IActionResult GetMealReport(DateTime fromDate, DateTime toDate, int type) =>
            ReadMealReport(fromDate, toDate, () => type is >= 0 and <= 2
                ? _iMiscellaneousService.MealReport(fromDate, toDate, type)
                : throw new ArgumentException("Invalid meal change type."));

        [HttpGet, RequireFunction("misc_ReportMeal")]
        public IActionResult GetMealSearchCustom(DateTime fromDate, DateTime toDate, string mealShiftID = "", string roomNo = "") =>
            ReadMealReport(fromDate, toDate, () => _iMiscellaneousService.ReportCustom(fromDate, toDate, mealShiftID, roomNo));

        [HttpGet, RequireFunction("misc_ReportMeal")]
        public IActionResult GetReportCancelMeal(DateTime fromDate, DateTime toDate) =>
            ReadMealReport(fromDate, toDate, () => _iMiscellaneousService.ReportCancelMeal(fromDate, toDate));

        [HttpGet, RequireFunction("misc_ReportMeal")]
        public IActionResult GetReportWaiveMeal(DateTime fromDate, DateTime toDate, string mealShift = "", string roomNo = "") =>
            ReadMealReport(fromDate, toDate, () => _iMiscellaneousService.ReportWaiveMeal(fromDate, toDate, mealShift, roomNo));

        [HttpGet, RequireFunction("misc_ReportMeal")]
        public IActionResult GetReportUsedBreakFastMeal(DateTime fromDate, DateTime toDate, string roomNo = "") =>
            ReadMealReport(fromDate, toDate, () => _iMiscellaneousService.ReportUsedBreakFastMeal(fromDate, toDate, roomNo));

        [HttpGet, RequireFunction("misc_ReportMeal")]
        public IActionResult GetReportUseBreakFastMeal(DateTime fromDate, DateTime toDate, string roomNo = "") =>
            ReadMealReport(fromDate, toDate, () => _iMiscellaneousService.ReportUseBreakFastMeal(fromDate, toDate, roomNo));

        [HttpPost, ValidateAntiForgeryToken, RequireFunction("misc_ReportMeal")]
        public IActionResult CancelReportedMeal(int id)
        {
            if (id <= 0) return BadRequest(new { success = false, message = "Please select a meal record." });
            try
            {
                if (!_iMiscellaneousService.CancelReportedMeal(id))
                    return BadRequest(new { success = false, message = "Only active meal records from the current business date can be cancelled. Refresh the report and try again." });
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cannot cancel meal record {MealID}", id);
                return StatusCode(500, new { success = false, message = "Cannot cancel the meal record." });
            }
        }

        private IActionResult ReadMealReport(DateTime fromDate, DateTime toDate, Func<DataTable> load)
        {
            if (!ModelState.IsValid || fromDate == default || toDate == default || fromDate.Date > toDate.Date)
                return BadRequest(new { success = false, message = "Please enter a valid date range (From Date <= To Date)." });
            try
            {
                DataTable table = load();
                // Keep all SP columns (including IDs for cancellation), and serialize dates as ISO values.
                var rows = table.AsEnumerable().Select(row => table.Columns.Cast<DataColumn>().ToDictionary(
                    col => System.Text.Json.JsonNamingPolicy.CamelCase.ConvertName(col.ColumnName),
                    col => row[col] == DBNull.Value ? null : row[col]));
                return Json(rows);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cannot load meal report from {FromDate} to {ToDate}", fromDate, toDate);
                return StatusCode(500, new { success = false, message = "Cannot load meal report. Please check the database configuration." });
            }
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
        [RequireFunction("misc_LoadCardsforGuests")]
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

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

        // Add card to guest (frmCardByReservation)
        [HttpGet, RequireFunction("misc_LoadCardsforGuests")]
        public IActionResult IssuingCardToGuests()
        {
            ViewBag.BusinessDate = _iMiscellaneousService.GetMealBusinessDate().ToString("yyyy-MM-dd");
            ViewBag.ZoneCodes = PropertyUtils.ConvertToList<ZoneModel>(ZoneBO.Instance.FindAll())
                .Select(z => z.Code).Where(c => !string.IsNullOrWhiteSpace(c)).OrderBy(c => c).ToList();

            int baudRate = 9600;
            TagScannerHelper.Instance.CodeReceived += MainForm_TagScanCodeReceived;

            // Ensure connection from settings (will show message if missing)
            TagScannerHelper.Instance.ConnectFromSettings(_configuration, baudRate);
            return View();
        }

        [HttpGet, RequireFunction("misc_LoadCardsforGuests")]
        public IActionResult SearchCardGuests(string? firstName, string? confirmationNo, string? crsNo, string? roomNo, string? zone,
            string? guestName, string? rsvHolder, string? cardID, bool roomSharer, bool showHasCard, DateTime? arrFrom, DateTime? arrTo, int searchType)
        {
            // Thứ tự giống cboSearchType bên WinForms
            var status = searchType switch { 1 => "1,6", 2 => "0,5", 3 => "0", 4 => "1", 5 => "6", 6 => "2", _ => "" };
            return CardAction(() => Json(_iMiscellaneousService.IssuingCardToGuests(
                    firstName?.Trim()!, confirmationNo?.Trim()!, crsNo?.Trim()!, roomNo?.Trim()!, zone!, guestName?.Trim()!, rsvHolder?.Trim()!,
                    roomSharer ? "" : "Y", showHasCard ? "" : "Y", arrFrom ?? DateTime.MinValue, arrTo ?? DateTime.MinValue, status,
                    searchType == 7 ? "Y" : "", searchType == 8 ? "Y" : "", cardID?.Trim()!, "")
                .AsEnumerable().Select(row => row.Table.Columns.Cast<DataColumn>().ToDictionary(
                    col => col.ColumnName.ToLowerInvariant(),
                    col => row[col] == DBNull.Value ? null : row[col])).ToList()));
        }

        [HttpGet, RequireFunction("misc_LoadCardsforGuests")]
        public IActionResult CheckGuestCard(string? id) =>
            CardAction(() => Json(new { exists = !string.IsNullOrWhiteSpace(id) && _iMiscellaneousService.Cards(id.Trim()).Rows.Count > 0 }));

        [HttpPost, ValidateAntiForgeryToken, RequireFunction("misc_LoadCardsforGuestsAddCard")]
        public IActionResult AddGuestCards(List<CardAssignment> items)
        {
            if (items == null || items.Count == 0) return CardError("You must be add card to guests!");
            if (items.Any(x => x.ReservationId <= 0 || string.IsNullOrWhiteSpace(x.CardId))
                || items.Select(x => x.CardId!.Trim()).Distinct().Count() != items.Count
                || items.Select(x => x.ReservationId).Distinct().Count() != items.Count)
                return CardError("Invalid card list.");

            return CardAction(() =>
            {
                var result = _iMiscellaneousService.IssueCards(items, HttpContext.Session.GetInt32("UserID") ?? 0,
                    HttpContext.Session.GetString("LoginName") ?? "");
                return Json(new { success = true, processed = result.Processed, skipped = result.Skipped });
            });
        }

        [HttpPost, ValidateAntiForgeryToken, RequireFunction("misc_LoadCardsforGuestsCancelCard")]
        public IActionResult CancelGuestCards(List<int> reservationIds)
        {
            if (reservationIds == null || reservationIds.Count == 0) return CardError("Select guests to cancel!");

            return CardAction(() =>
            {
                var result = _iMiscellaneousService.CancelCards(reservationIds, HttpContext.Session.GetInt32("UserID") ?? 0,
                    HttpContext.Session.GetString("LoginName") ?? "");
                return Json(new { success = true, processed = result.Processed, skipped = result.Skipped });
            });
        }
        #endregion

    }
}

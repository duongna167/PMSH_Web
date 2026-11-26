using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using BaseBusiness.Auth;
using BaseBusiness.BO;
using BaseBusiness.Model;
using BaseBusiness.util;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Miscellaneous.Services.Interfaces;

namespace Miscellaneous.Controllers
{
    /// <summary>
    /// Meal management (frmMealChangeManager). Đường dẫn vẫn là /Miscellaneous/...
    /// </summary>
    [Route("Miscellaneous/[action]")]
    public class MealChangeController : Controller
    {
        private readonly IMiscellaneousService _service;
        private readonly ILogger<MealChangeController> _logger;

        public MealChangeController(IMiscellaneousService service, ILogger<MealChangeController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet, RequireFunction("misc_MealChangeManager")]
        public IActionResult MealManagement()
        {
            ViewBag.MealBusinessDate = _service.GetMealBusinessDate().ToString("yyyy-MM-dd");
            ViewBag.ZoneCodes = PropertyUtils.ConvertToList<ZoneModel>(ZoneBO.Instance.FindAll())
                .Select(z => z.Code).Where(c => !string.IsNullOrWhiteSpace(c)).OrderBy(c => c).ToList();
            return View("~/Views/Miscellaneous/MealManagement.cshtml");
        }

        [HttpGet, RequireFunction("misc_MealChangeManager")]
        public IActionResult SearchMealGuests(string confirmationNo, string roomNo, string zone, string guestName, string rsvHolder,
            string cardID, bool roomSharer, DateTime? arrFrom, DateTime? arrTo, int searchType, string excludeIds)
        {
            // Thứ tự giống cboSearchType bên WinForms
            var status = searchType switch { 1 => "1,6", 2 => "0,5", 3 => "0", 4 => "1", 5 => "6", 6 => "2", _ => "" };
            return Read(() => Rows(_service.IssuingCardToGuests("", confirmationNo?.Trim(), "", roomNo?.Trim(), zone,
                guestName?.Trim(), rsvHolder?.Trim(), roomSharer ? "" : "Y", "",
                arrFrom ?? DateTime.MinValue, arrTo ?? DateTime.MinValue, status,
                searchType == 7 ? "Y" : "", searchType == 8 ? "Y" : "", cardID?.Trim().Replace("_", ""), excludeIds)));
        }

        [HttpGet, RequireFunction("misc_MealChangeManager")]
        public IActionResult GetMealGuestInfo(int reservationId) =>
            Read(() => new
            {
                meal = _service.MealSummary(reservationId),
                history = Rows(_service.MealChangeHistory(reservationId))
            });

        [HttpPost, ValidateAntiForgeryToken, RequireFunction("misc_MealChangeManagerOK")]
        public IActionResult ProcessMealChange(MealChangeRequest request)
        {
            var message = Validate(request);
            if (message != null) return BadRequest(new { success = false, message });
            try
            {
                var result = _service.ProcessMealChange(request, HttpContext.Session.GetString("LoginName") ?? "");
                return Json(new { success = true, processed = result.Processed, skipped = result.Skipped });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cannot process meal change");
                return StatusCode(500, new { success = false, message = "Cannot process meal. No data was changed." });
            }
        }

        // CheckUpdate
        private string Validate(MealChangeRequest request)
        {
            if (!ModelState.IsValid || request.FromDate == default || request.ToDate == default
                || request.Type is < 0 or > 3 || request.FromMeal is < 0 or > 2 || request.ToMeal is < 0 or > 2)
                return "Invalid data.";

            var businessDate = _service.GetMealBusinessDate();
            var from = request.FromDate.Date;
            var to = request.ToDate.Date;
            var errors = new List<string>();

            if (request.Type == 0 && request.FromMeal == request.ToMeal && from == to)
                errors.Add("Please choose date and meal to continue");
            if ((request.Type == 1 || request.Type == 2) && to < from)
                errors.Add("To date >= From date");
            if (to < businessDate)
                errors.Add("To date >= Business date");
            if (from < businessDate)
                errors.Add("From date >= Business date");
            if (request.Type == 0 && request.FromMeal == 0 && request.ToMeal > 0)
                errors.Add("Breakfast cannot move to Lunch or Dinner");
            if (request.ReservationIds == null || request.ReservationIds.Count == 0)
                errors.Add("Please select a guest before");

            return errors.Count == 0 ? null : string.Join("\n", errors);
        }

        private IActionResult Read(Func<object> load)
        {
            try
            {
                return Json(load());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cannot load meal management data");
                return StatusCode(500, new { success = false, message = "Cannot load data. Please check the database configuration." });
            }
        }

        // Key viết thường hết để view không phụ thuộc cách đặt tên cột của stored procedure
        private static List<Dictionary<string, object>> Rows(DataTable table) =>
            table.AsEnumerable().Select(row => table.Columns.Cast<DataColumn>().ToDictionary(
                col => col.ColumnName.ToLowerInvariant(),
                col => row[col] == DBNull.Value ? null : row[col])).ToList();
    }
}

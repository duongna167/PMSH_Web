using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using BaseBusiness.Auth;
using HouseKeeping.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HouseKeeping.Controllers
{
    /// <summary>
    /// Set Color / Set Text của Room Availability. Đường dẫn vẫn là /HouseKeeping/...
    /// </summary>
    [Route("HouseKeeping/[action]")]
    public class RoomAvailabilityNoteController : Controller
    {
        private const int MaxDays = 366;
        private readonly IHouseKeepingService _service;
        private readonly ILogger<RoomAvailabilityNoteController> _logger;

        public RoomAvailabilityNoteController(IHouseKeepingService service, ILogger<RoomAvailabilityNoteController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // Nút Detail (Room Available Resvs Detail)
        [HttpGet, RequireFunction("rmg_RoomAvailibility")]
        public IActionResult GetRoomAvailabilityDetail(string? roomTypeCode, DateTime date)
        {
            if (string.IsNullOrWhiteSpace(roomTypeCode) || date == default) return Error("Please select a room type and a date.");
            try
            {
                var table = _service.RoomAvailabilityDetail(roomTypeCode.Trim(), date);
                if (table == null) return Error("Please select a room type row.");

                string Text(DataRow row, string column) => Convert.ToString(row[column])?.Trim() ?? "";
                DateTime? Day(DataRow row, string column) => row[column] == DBNull.Value ? null : Convert.ToDateTime(row[column]);
                var rows = table.AsEnumerable().Select((row, index) => new
                {
                    index = index + 1,
                    confirmationNo = Text(row, "ConfirmationNo"),
                    crsNo = Text(row, "CRSNo"),
                    type = Text(row, "Type"),
                    room = Text(row, "Room"),
                    noOfRoom = row["No Of Room"] == DBNull.Value ? 0 : Convert.ToInt32(row["No Of Room"]),
                    profile = Text(row, "Profile"),
                    fromDate = Day(row, "From Date"),
                    toDate = Day(row, "To Date")
                }).ToList();
                return Json(rows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cannot load room availability detail");
                return StatusCode(500, new { success = false, message = "Cannot load detail. Please check the database configuration." });
            }
        }

        [HttpGet, RequireFunction("rmg_RoomAvailibility")]
        public IActionResult GetRoomAvailabilityNotes(DateTime fromDate, DateTime toDate)
        {
            if (fromDate == default || toDate < fromDate) return Error("Please enter a valid From Date and To Date.");
            try
            {
                var notes = _service.RoomAvailabilityNotes(fromDate, toDate).AsEnumerable().Select(row => new
                {
                    date = Convert.ToDateTime(row["Date"]).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    dayOfWeek = Convert.ToString(row["DayOfWeek"]),
                    color = ToHtmlColor(Convert.ToString(row["ColorName"])),
                    description = Convert.ToString(row["Description"])
                }).ToList();
                return Json(notes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cannot load room availability notes");
                return StatusCode(500, new { success = false, message = "Cannot load color and text. Please check the database configuration." });
            }
        }

        // type = color | text. color dạng #rrggbb, để trống là bỏ màu. description để trống là xóa ghi chú.
        [HttpPost, ValidateAntiForgeryToken, RequireFunction("rmg_RoomAvailibility")]
        public IActionResult SaveRoomAvailabilityNotes(string? type, DateTime fromDate, DateTime toDate,
            List<int> weekdays, string? color, string? description)
        {
            if (fromDate == default || toDate == default || toDate.Date < fromDate.Date)
                return Error("To Date must be greater than or equal to From Date.");
            if ((toDate.Date - fromDate.Date).Days >= MaxDays) return Error("The date range is too long.");
            if (weekdays == null || weekdays.Count == 0) return Error("Please choose at least one day of week.");

            string? colorName = null, text = null;
            if (type == "color")
            {
                if (string.IsNullOrWhiteSpace(color)) colorName = "";
                else if (!TryToWinFormsColor(color, out colorName)) return Error("Color is invalid.");
            }
            else if (type == "text")
            {
                text = description?.Trim() ?? "";
                if (text.Length > 200) return Error("Description is limited to 200 characters.");
            }
            else return Error("Invalid data.");

            var dates = Enumerable.Range(0, (toDate.Date - fromDate.Date).Days + 1)
                .Select(offset => fromDate.Date.AddDays(offset))
                .Where(date => weekdays.Contains((int)date.DayOfWeek))
                .ToList();
            if (dates.Count == 0) return Error("No date matches the selected days of week.");

            try
            {
                _service.SaveRoomAvailabilityNotes(dates, colorName, text, HttpContext.Session.GetInt32("UserID") ?? 0);
                return Json(new { success = true, days = dates.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cannot save room availability notes");
                return StatusCode(500, new { success = false, message = "Cannot save. No data was changed." });
            }
        }

        private IActionResult Error(string message) => BadRequest(new { success = false, message });

        // Bản WinForms lưu màu dạng số Color.ToArgb(), ví dụ -65281
        private static string? ToHtmlColor(string? colorName) =>
            int.TryParse(colorName, out var argb) ? "#" + (argb & 0xFFFFFF).ToString("x6") : null;

        private static bool TryToWinFormsColor(string html, out string colorName)
        {
            colorName = "";
            if (html.Length != 7 || html[0] != '#'
                || !int.TryParse(html[1..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
                return false;
            colorName = unchecked((int)0xFF000000 | rgb).ToString(CultureInfo.InvariantCulture);
            return true;
        }
    }
}

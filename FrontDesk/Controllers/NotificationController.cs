using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BaseBusiness.Auth;
using BaseBusiness.BO;
using BaseBusiness.Model;
using BaseBusiness.util;
using FrontDesk.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FrontDesk.Controllers
{
    public class NotificationController : Controller
    {
        private readonly INotificationService _service;

        public NotificationController(INotificationService service)
        {
            _service = service;
        }

        private int SessionUserId => HttpContext.Session.GetInt32("UserID") ?? 0;

        private string SessionUserName =>
            HttpContext.Session.GetString("LoginName")
            ?? HttpContext.Session.GetString("UserName")
            ?? "";

        [HttpGet]
        public IActionResult Summary()
        {
            if (SessionUserId <= 0)
                return Json(new { birthdayCount = 0, messageCount = 0, traceCount = 0, total = 0 });
            var (b, m, t) = _service.Summary(SessionUserId);
            return Json(new { birthdayCount = b, messageCount = m, traceCount = t, total = b + m + t });
        }

        [HttpGet]
        public IActionResult Birthday()
        {
            return PartialView("~/Views/FrontDesk/NotificationBirthday.cshtml");
        }

        [HttpGet]
        public IActionResult BirthdayData()
        {
            try
            {
                var rows = _service.Birthdays() ?? new();
                return Json(rows.Select(r => new
                {
                    name = r.Name ?? "",
                    dateOfBirth = r.DateOfBirth,
                    nationality = r.Nationality ?? ""
                }).ToList());
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        [RequireFunction("fo_CreateMessage_web")]
        public IActionResult CreateMessage()
        {
            ViewBag.FromName = _service.LoginDepartmentName(SessionUserId);
            var depts = PropertyUtils.ConvertToList<DepartmentModel>(DepartmentBO.Instance.FindAll())
                ?? new List<DepartmentModel>();
            ViewBag.Departments = depts
                .Where(d => d != null && !d.Inactive && !string.IsNullOrWhiteSpace(d.Name))
                .OrderBy(d => d.Name)
                .ToList();
            return PartialView("~/Views/FrontDesk/CreateMessage.cshtml");
        }

        [HttpPost]
        [RequireFunction("fo_CreateMessage_web")]
        public IActionResult SaveMessage(string message, string fromDate, string toDate, int toDepartmentId)
        {
            try
            {
                if (SessionUserId <= 0)
                    return Json(new { code = 1, msg = "Please login." });
                if (string.IsNullOrWhiteSpace(message))
                    return Json(new { code = 1, msg = "Message can not be blank." });
                if (!TryParseUiDate(fromDate, out var from) || !TryParseUiDate(toDate, out var to))
                    return Json(new { code = 1, msg = "From Date and To Date are required." });
                if (to.Date < from.Date)
                    return Json(new { code = 1, msg = "To Date must be on or after From Date." });
                _service.SaveMessage(message.Trim(), from, to, SessionUserId, toDepartmentId);
                return Json(new { code = 0, msg = "Message sent successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { code = 1, msg = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult MessageList()
        {
            return PartialView("~/Views/FrontDesk/NotificationMessage.cshtml");
        }

        [HttpGet]
        public IActionResult MessageData()
        {
            try
            {
                var rows = _service.Messages(SessionUserId) ?? new();
                return Json(rows.Select(r => new
                {
                    id = r.ID,
                    fromName = r.FromName ?? "",
                    message = r.Message ?? "",
                    isRead = r.IsRead,
                    receivedDate = r.ReceivedDate
                }).ToList());
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        [HttpPost]
        public IActionResult MarkReceived(int id)
        {
            try
            {
                if (SessionUserId <= 0)
                    return Json(new { code = 1, msg = "Please login." });
                if (id <= 0)
                    return Json(new { code = 1, msg = "Please select a message." });
                _service.MarkReceived(id, SessionUserId);
                return Json(new { code = 0, msg = "Received." });
            }
            catch (Exception ex)
            {
                return Json(new { code = 1, msg = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult TraceList()
        {
            return PartialView("~/Views/FrontDesk/NotificationTrace.cshtml");
        }

        [HttpGet]
        public IActionResult TraceData()
        {
            try
            {
                var rows = _service.OpenTraces(SessionUserId) ?? new();
                return Json(rows.Select(r => new
                {
                    id = r.ID,
                    department = r.Department ?? "",
                    tracesTime = r.TracesTime ?? "",
                    tracesText = r.TracesText ?? "",
                    guestName = r.GuestName ?? "",
                    reservationID = r.ReservationID,
                    resolved = r.Resolved,
                    fromDate = r.FromDate,
                    toDate = r.ToDate
                }).ToList());
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        [HttpPost]
        public IActionResult ResolveTraces([FromForm] List<int> selectedIds)
        {
            try
            {
                var err = _service.ResolveTraces(SessionUserId, SessionUserName, selectedIds);
                if (!string.IsNullOrEmpty(err))
                    return Json(new { code = 1, msg = err });
                return Json(new { code = 0, msg = "Trace was resolved successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { code = 1, msg = ex.Message });
            }
        }

        private static bool TryParseUiDate(string raw, out DateTime date)
        {
            date = default;
            if (string.IsNullOrWhiteSpace(raw))
                return false;
            var s = raw.Trim();
            string[] formats = { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd", "yyyy-MM-ddTHH:mm:ss", "dd/MM/yyyy HH:mm:ss" };
            if (DateTime.TryParseExact(s, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                return true;
            return DateTime.TryParse(s, CultureInfo.GetCultureInfo("vi-VN"), DateTimeStyles.None, out date);
        }
    }
}

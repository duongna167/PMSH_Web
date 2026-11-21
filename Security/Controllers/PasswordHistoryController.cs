using System;
using System.Linq;
using BaseBusiness.Auth;
using Microsoft.AspNetCore.Mvc;
using Security.Services.Interfaces;

namespace Security.Controllers
{
    [RequireFunction("usr_PasswordHistory")]
    public class PasswordHistoryController : Controller
    {
        private readonly IPasswordHistoryService _service;

        public PasswordHistoryController(IPasswordHistoryService service)
        {
            _service = service;
        }

        [RequireFunction("usr_PasswordHistory")]
        public IActionResult PasswordHistory()
        {
            ViewBag.UserList = _service.Users();
            return PartialView("~/Views/Security/PasswordHistory.cshtml");
        }

        [HttpGet]
        public IActionResult PasswordHistoryData(int userId, int type)
        {
            try
            {
                var rows = _service.Search(userId, type);
                var result = rows.Select(r => new
                {
                    Username = r.Username ?? "",
                    TypedPassword = r.TypedPassword ?? "",
                    Type = r.Type == 0 ? "Change" : "Wrong",
                    ComputerName = r.ComputerName ?? "",
                    ComputerIP = r.ComputerIP ?? "",
                    CreatedDate = r.CreatedDate
                }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
    }
}

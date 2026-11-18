using System;
using System.Data;
using System.Linq;
using BaseBusiness.BO;
using BaseBusiness.Model;
using BaseBusiness.util;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using User.Services.Interfaces;

namespace User.Controllers
{
    public class UserController : Controller
    {
        private const string PendingUserIdKey = "PendingChangeUserId";
        private const string PendingModeKey = "PendingChangeMode";
        private const string PendingLoginNameKey = "PendingLoginName";
        private const int MaxPasswordFails = 5;
        private static readonly object LoginFailLock = new();

        private readonly IConfiguration _configuration;
        private readonly ILogger<UserController> _logger;
        private readonly IMemoryCache _cache;
        private readonly IUserService _iUserService;

        public UserController(ILogger<UserController> logger,
             IMemoryCache cache, IConfiguration configuration, IUserService UserService)
        {
            _cache = cache;
            _logger = logger;
            _configuration = configuration;
            _iUserService = UserService;
        }

        [AllowAnonymous]
        public IActionResult Index()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult ChangePassword(string mode)
        {
            var pendingId = HttpContext.Session.GetInt32(PendingUserIdKey);
            if (pendingId == null || pendingId == 0)
                return RedirectToAction("Index");

            var resolved = string.Equals(mode, "renew", StringComparison.OrdinalIgnoreCase) ? "renew" : "first";
            var sessionMode = HttpContext.Session.GetString(PendingModeKey);
            if (!string.IsNullOrEmpty(sessionMode))
                resolved = sessionMode;

            ViewBag.Mode = resolved;
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public IActionResult Login()
        {
            try
            {
                string loginName = Request.Form["LoginName"].ToString();
                string password = Request.Form["Password"].ToString();

                var user = _iUserService.FindByLoginName(loginName);
                if (user == null || user.ID == 0 || !_iUserService.VerifyPassword(user, password))
                {
                    return HandleFailedPassword(user, loginName);
                }

                ClearLoginFails(loginName);

                if (user.Status == -1)
                {
                    return Json(new { code = 7, msg = "This user account has been terminated. Please contact the system administrator." });
                }

                if (user.Status == 0)
                {
                    return Json(new { code = 6, msg = "This user account is inactive. Please contact the system administrator." });
                }

                var today = DateTime.Now.Date;
                if (HasExpireDate(user.PassExpireDate) && today > user.PassExpireDate.Date)
                {
                    _iUserService.LockAccount(user);
                    return Json(new { code = 5, msg = "Your password has expired and this user account has been locked. Please contact the system administrator to restore access." });
                }

                if (!user.PasswordStatus)
                {
                    SetPending(user, "first");
                    return Json(new { code = 3, msg = "Please set a new password before accessing the system.", mode = "first" });
                }

                var payload = BuildLoginPayload(user, loginName);
                if (payload.Error != null)
                    return payload.Error;

                var remaining = RemainingDays(user.PassExpireDate, today);
                if (HasExpireDate(user.PassExpireDate) && remaining >= 0 && remaining <= 5)
                {
                    SetPending(user, "renew");
                    SignIn(user, loginName);
                    var warnMsg = remaining == 0
                        ? "Your password will expire today. Do you want to change it now?"
                        : $"Your password will expire in {remaining} day{(remaining == 1 ? "" : "s")}. Do you want to change it now?";
                    return Json(new
                    {
                        code = 4,
                        msg = warnMsg,
                        daysRemaining = remaining,
                        data = payload.Data,
                        namelogin = payload.LoginName,
                        userID = payload.UserId,
                        businessDate = payload.BusinessDate
                    });
                }

                SignIn(user, loginName);
                return Json(new
                {
                    code = 0,
                    msg = "Successfully",
                    data = payload.Data,
                    namelogin = payload.LoginName,
                    userID = payload.UserId,
                    businessDate = payload.BusinessDate
                });
            }
            catch (Exception ex)
            {
                return Json(new { code = 1, msg = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpPost]
        public IActionResult ChangePassword()
        {
            try
            {
                var pendingId = HttpContext.Session.GetInt32(PendingUserIdKey);
                var mode = HttpContext.Session.GetString(PendingModeKey) ?? "first";
                var loginName = HttpContext.Session.GetString(PendingLoginNameKey) ?? "";

                if (pendingId == null || pendingId == 0)
                    return Json(new { code = -1, msg = "Your session has expired. Please sign in again." });

                string currentPassword = Request.Form["CurrentPassword"].ToString();
                string newPassword = Request.Form["NewPassword"].ToString();
                string confirmPassword = Request.Form["ConfirmPassword"].ToString();

                if (string.IsNullOrEmpty(newPassword))
                    return Json(new { code = -1, msg = "New password is required." });

                if (string.IsNullOrEmpty(confirmPassword))
                    return Json(new { code = -1, msg = "Confirm password is required." });

                if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
                    return Json(new { code = -1, msg = "The new password and confirmation do not match." });

                bool requireCurrent = string.Equals(mode, "renew", StringComparison.OrdinalIgnoreCase);
                if (!_iUserService.ChangePassword(pendingId.Value, currentPassword, newPassword, requireCurrent, out var error))
                    return Json(new { code = -1, msg = error });

                var user = (UsersModel)UsersBO.Instance.FindByPrimaryKey(pendingId.Value);
                if (user == null || user.ID == 0)
                    return Json(new { code = -1, msg = "The user account was not found." });

                var payload = BuildLoginPayload(user, string.IsNullOrEmpty(loginName) ? user.LoginName : loginName);
                if (payload.Error != null)
                    return payload.Error;

                ClearPending();
                SignIn(user, payload.LoginName);
                return Json(new
                {
                    code = 0,
                    msg = "Successfully",
                    data = payload.Data,
                    namelogin = payload.LoginName,
                    userID = payload.UserId,
                    businessDate = payload.BusinessDate
                });
            }
            catch (Exception ex)
            {
                return Json(new { code = 1, msg = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpPost]
        public IActionResult CancelChangePassword()
        {
            ClearPending();
            HttpContext.Session.Remove("UserID");
            HttpContext.Session.Remove("LoginName");
            HttpContext.Session.Remove("CashierNo");
            return Json(new { code = 0 });
        }

        private IActionResult HandleFailedPassword(UsersModel user, string loginName)
        {
            const string invalidMsg = "Invalid Username and/or Password, or Account is not active!";

            if (user == null || user.ID == 0)
                return Json(new { code = -1, msg = "The username or password is incorrect. Please try again.", showTries = false });

            if (user.Status == -1)
                return Json(new { code = 7, msg = "This user account has been terminated. Please contact the system administrator." });

            if (user.Status == 0)
                return Json(new { code = 6, msg = "This user account is inactive. Please contact the system administrator." });

            var fails = RegisterLoginFail(loginName);
            var remaining = MaxPasswordFails - fails;

            if (fails >= MaxPasswordFails)
            {
                _iUserService.LockAccount(user);
                ClearLoginFails(loginName);
                return Json(new
                {
                    code = 8,
                    msg = invalidMsg + "\n\n0 tries left.",
                    lockMsg = "You have entered wrong password(s) for 5 times, for security reasons this account is now locked.\nPlease contact administrators to regain access to your account."
                });
            }

            if (fails == 1)
                return Json(new { code = -1, msg = "The username or password is incorrect. Please try again.", showTries = false });

            var tryWord = remaining == 1 ? "try" : "tries";
            return Json(new
            {
                code = -1,
                msg = invalidMsg + "\n\n" + remaining + " " + tryWord + " left.",
                showTries = true,
                triesLeft = remaining
            });
        }

        private int RegisterLoginFail(string loginName)
        {
            var key = LoginFailKey(loginName);
            lock (LoginFailLock)
            {
                var fails = _cache.TryGetValue(key, out int current) ? current + 1 : 1;
                _cache.Set(key, fails, TimeSpan.FromMinutes(30));
                return fails;
            }
        }

        private void ClearLoginFails(string loginName)
        {
            _cache.Remove(LoginFailKey(loginName));
        }

        private static string LoginFailKey(string loginName)
        {
            return "login-fail:" + (loginName ?? string.Empty).Trim().ToLowerInvariant();
        }

        private void SetPending(UsersModel user, string mode)
        {
            HttpContext.Session.SetInt32(PendingUserIdKey, user.ID);
            HttpContext.Session.SetString(PendingModeKey, mode);
            HttpContext.Session.SetString(PendingLoginNameKey, user.LoginName ?? "");
        }

        private void ClearPending()
        {
            HttpContext.Session.Remove(PendingUserIdKey);
            HttpContext.Session.Remove(PendingModeKey);
            HttpContext.Session.Remove(PendingLoginNameKey);
        }

        private void SignIn(UsersModel user, string loginName)
        {
            HttpContext.Session.SetInt32("UserID", user.ID);
            HttpContext.Session.SetString("LoginName", loginName ?? user.LoginName ?? "");
            HttpContext.Session.SetInt32("CashierNo", user.CashierNo);
        }

        private static bool HasExpireDate(DateTime expireDate)
        {
            return expireDate.Year >= 1900;
        }

        private static int RemainingDays(DateTime expireDate, DateTime today)
        {
            return (expireDate.Date - today).Days;
        }

        private LoginPayload BuildLoginPayload(UsersModel user, string loginName)
        {
            var result2 = _iUserService.PermissionNames(user.UserGroupID, user.ID);
            var businessDates = PropertyUtils.ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll());
            var resultname = (from d in result2.AsEnumerable()
                              select new
                              {
                                  Name = !string.IsNullOrEmpty(d["Name"].ToString()) ? d["Name"] : "",
                              }).ToList();

            if (businessDates == null || businessDates.Count == 0)
            {
                return new LoginPayload
                {
                    Error = Json(new { code = 2, msg = "Business date is not set in the system. Please configure BusinessDate in the database." })
                };
            }

            return new LoginPayload
            {
                Data = resultname,
                LoginName = loginName ?? user.LoginName,
                UserId = user.ID,
                BusinessDate = businessDates[0].BusinessDate.ToString("dd/MM/yyyy")
            };
        }

        private sealed class LoginPayload
        {
            public object Data { get; set; }
            public string LoginName { get; set; }
            public int UserId { get; set; }
            public string BusinessDate { get; set; }
            public JsonResult Error { get; set; }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaseBusiness.BO;
using BaseBusiness.Model;
using BaseBusiness.util;
using DevExpress.ClipboardSource.SpreadsheetML;
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
        [HttpPost]
        public async Task<IActionResult> Login()
        {
            try
            {
                string loginName = Request.Form["LoginName"].ToString();
                string password = Request.Form["Password"].ToString();
                var result = _iUserService.Login(loginName, password);
                int UserGroupID = result.UserGroupID;
                int UserID = result.ID;
                int CashierNo = result.CashierNo;
                var result2 = _iUserService.PermissionNames(UserGroupID, UserID);
                HttpContext.Session.SetInt32("UserID", UserID);
                HttpContext.Session.SetString("LoginName", loginName);
                HttpContext.Session.SetInt32("CashierNo", CashierNo);

                var businessDates = PropertyUtils.ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll());
                int memberTypeID = 0; int roomTypeID = 0; int vipID = 0;
                var resultname = (from d in result2.AsEnumerable()
                                  select new
                                  {
                                      Name = !string.IsNullOrEmpty(d["Name"].ToString()) ? d["Name"] : "",

                                  }).ToList();


                if (result.ID != 0)
                {
                    if (businessDates == null || businessDates.Count == 0)
                    {
                        return Json(new { code = 2, msg = "Business date is not set in the system. Please configure BusinessDate in the database." });
                    }
                    return Json(new { code = 0, msg = "Successfully", data = resultname, namelogin = loginName, userID = UserID, businessDate = businessDates[0].BusinessDate.ToString("dd/MM/yyyy") });
                }
                else
                {
                    return Json(new { code = -1, msg = "The username or password is incorrect. Please try again." });
                }

            }
            catch (Exception ex)
            {
                return Json(new { code = 1, msg = ex.Message });
            }

        }
    }
}
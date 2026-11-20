using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using BaseBusiness.Auth;
using BaseBusiness.Model;
using BaseBusiness.util;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Security.Helpers;
using Security.Services.Interfaces;

namespace Security.Controllers
{
    [RequireFunction("UsersManagement_web")]
    public class UsersManagementController : Controller
    {
        private readonly IUsersManagementService _service;

        public UsersManagementController(IUsersManagementService service)
        {
            _service = service;
        }

        [RequireFunction("UsersManagement_web")]
        public IActionResult UsersManagement()
        {
            ViewBag.UserGroupList = _service.Groups();
            ViewBag.JobTitleList = _service.JobTitles();
            ViewBag.DepartmentList = _service.Departments();
            return PartialView("~/Views/Security/UsersManagement.cshtml");
        }

        [HttpGet]
        public IActionResult UsersManagementData(string lastName, string firstName, string loginName, int userStatus, int cashierStatus, string jobtitle, string department)
        {
            lastName ??= "";
            firstName ??= "";
            loginName ??= "";
            jobtitle ??= "";
            department ??= "";
            try
            {
                DataTable dataTable = _service.Search(lastName, firstName, loginName, userStatus, cashierStatus, jobtitle, department);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  ID = d["ID"] != DBNull.Value ? d["ID"].ToString() : "",
                                  Status = d["Status"] != DBNull.Value ? Convert.ToInt32(d["Status"]) : 0,
                                  StatusText = d["StatusText"] != DBNull.Value ? d["StatusText"].ToString() : "",
                                  FullName = d["FullName"] != DBNull.Value ? d["FullName"].ToString() : "",
                                  LoginName = d["LoginName"] != DBNull.Value ? d["LoginName"].ToString() : "",
                                  JobTitle = d["JobTitle"] != DBNull.Value ? d["JobTitle"].ToString() : "",
                                  Sex = d["Sex"] != DBNull.Value ? d["Sex"].ToString() : "",
                                  BirthOfDate = d["BirthOfDate"] != DBNull.Value ? Convert.ToDateTime(d["BirthOfDate"]) : (DateTime?)null,
                                  Telephone = d["Telephone"] != DBNull.Value ? d["Telephone"].ToString() : "",
                                  HandPhone = d["HandPhone"] != DBNull.Value ? d["HandPhone"].ToString() : "",
                                  HomeAddress = d["HomeAddress"] != DBNull.Value ? d["HomeAddress"].ToString() : "",
                                  Resident = d["Resident"] != DBNull.Value ? d["Resident"].ToString() : "",
                                  PostalCode = d["PostalCode"] != DBNull.Value ? d["PostalCode"].ToString() : "",
                                  IsCashier = d["IsCashier"] != DBNull.Value && Convert.ToBoolean(d["IsCashier"]),
                                  Department = d["Department"] != DBNull.Value ? d["Department"].ToString() : "",
                                  CreateDate = d["CreateDate"] != DBNull.Value ? Convert.ToDateTime(d["CreateDate"]) : (DateTime?)null,
                                  CreateBy = d["CreateBy"] != DBNull.Value ? d["CreateBy"].ToString() : "",
                                  UpdateDate = d["UpdateDate"] != DBNull.Value ? Convert.ToDateTime(d["UpdateDate"]) : (DateTime?)null,
                                  UpdateBy = d["UpdateBy"] != DBNull.Value ? d["UpdateBy"].ToString() : ""
                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        [HttpGet]
        public IActionResult UsersManagementDetail(int id)
        {
            try
            {
                return Json(_service.GetById(id));
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult UpdateUsersManagement(UsersModel modelRoom)
        {
            try { return ToJson(_service.Save(modelRoom, UserDetailLog.ClientIp(HttpContext))); }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public IActionResult ResetPassUsersManagement(UsersModel modelRoom)
        {
            try
            {
                var r = _service.ResetPassword(modelRoom, UserDetailLog.ClientIp(HttpContext));
                if (!r.Success)
                    return Json(new { success = false, message = r.Message });
                var data = r.Data as Dictionary<string, object> ?? new Dictionary<string, object>();
                return Json(new
                {
                    success = true,
                    message = r.Message,
                    plainPassword = data.GetValueOrDefault("plainPassword"),
                    passExpireDate = data.GetValueOrDefault("passExpireDate"),
                    hasEmail = data.GetValueOrDefault("hasEmail")
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult SendResetPasswordEmail(int id, string plainPassword)
        {
            try
            {
                int sendBy = HttpContext.Session.GetInt32("UserID") ?? 0;
                return ToJson(_service.SendResetPasswordEmail(id, plainPassword, sendBy));
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult LockUsersManagement(int id)
        {
            try { return ToJson(_service.Lock(id)); }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public IActionResult UsersManagementDelete(int id)
        {
            try { return ToJson(_service.Delete(id)); }
            catch (Exception ex) { return BadRequest(new { success = false, message = ex.Message }); }
        }

        private static JsonResult ToJson(OpResult r) =>
            new(new { success = r.Success, message = r.Message, data = r.Data, errors = r.Errors });
    }
}

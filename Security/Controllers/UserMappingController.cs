using System;
using BaseBusiness.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Security.Services.Interfaces;

namespace Security.Controllers
{
    public class UserMappingController : Controller
    {
        private readonly IUserMappingService _service;

        public UserMappingController(IUserMappingService service)
        {
            _service = service;
        }

        [RequireFunction("usr_UserRight")]
        public IActionResult UserMapping() =>
            PartialView("~/Views/Security/UserMapping.cshtml");

        [RequireFunction("usr_GroupUserRight")]
        public IActionResult UserMappingByGroup() =>
            PartialView("~/Views/Security/UserMappingByGroup.cshtml");

        [RequireFunction("usr_UserRight")]
        [HttpGet]
        public IActionResult MappingUsers()
        {
            try { return Json(_service.Users()); }
            catch (Exception ex) { return Json(new { error = ex.Message }); }
        }

        [RequireFunction("usr_GroupUserRight")]
        [HttpGet]
        public IActionResult MappingGroups()
        {
            try { return Json(_service.Groups()); }
            catch (Exception ex) { return Json(new { error = ex.Message }); }
        }

        [RequireFunction("usr_GroupUserRight")]
        [HttpGet]
        public IActionResult MappingGroupUsers(int groupId)
        {
            try { return Json(_service.GroupUsers(groupId)); }
            catch (Exception ex) { return Json(new { error = ex.Message }); }
        }

        [RequireFunction("usr_UserRight", "usr_GroupUserRight")]
        [HttpGet]
        public IActionResult Catalog(int isDataRight)
        {
            try { return Json(_service.Catalog(isDataRight == 1)); }
            catch (Exception ex) { return Json(new { error = ex.Message }); }
        }

        [RequireFunction("usr_UserRight", "usr_GroupUserRight")]
        [HttpGet]
        public IActionResult GrantedIds(int ownerId, int isDataRight, int isUser)
        {
            try { return Json(_service.GrantedIds(ownerId, isDataRight == 1, isUser == 1)); }
            catch (Exception ex) { return Json(new { error = ex.Message }); }
        }

        [RequireFunction("usr_UserRight", "usr_GroupUserRight")]
        [HttpGet]
        public IActionResult RightTree(int ownerId, int isDataRight, int isUser)
        {
            try { return Json(_service.RightTree(ownerId, isDataRight == 1, isUser == 1)); }
            catch (Exception ex) { return Json(new { error = ex.Message }); }
        }

        [RequireFunction("usr_UserRight")]
        [HttpPost]
        public IActionResult SaveUserRights(int userId, int isDataRight, string ids)
        {
            try
            {
                var login = HttpContext.Session.GetString("LoginName") ?? "";
                var r = _service.SaveUserRights(userId, isDataRight == 1, ids, login);
                return Json(new { success = r.Success, message = r.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [RequireFunction("usr_GroupUserRight")]
        [HttpPost]
        public IActionResult SaveGroupRights(int groupId, int isDataRight, string ids)
        {
            try
            {
                var login = HttpContext.Session.GetString("LoginName") ?? "";
                var r = _service.SaveGroupRights(groupId, isDataRight == 1, ids, login);
                return Json(new { success = r.Success, message = r.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}

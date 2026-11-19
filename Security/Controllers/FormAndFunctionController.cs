using System;
using BaseBusiness.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Security.Services.Interfaces;

namespace Security.Controllers
{
    [RequireFunction("usr_Functions")]
    public class FormAndFunctionController : Controller
    {
        private readonly IFormAndFunctionService _service;

        public FormAndFunctionController(IFormAndFunctionService service)
        {
            _service = service;
        }

        [RequireFunction("usr_Functions")]
        public IActionResult AddFuncitionsToList()
        {
            ViewBag.FormAndFunctionGroupList = _service.FunctionGroups();
            ViewBag.FormAndFunctionDataGroupList = _service.DataGroups();
            return PartialView("~/Views/Security/AddFuncitionsToList.cshtml");
        }

        [HttpGet]
        public IActionResult AddFuncitionsToListData(string codetag, string namerights, int isDataRight)
        {
            try { return Json(_service.SearchGrid(codetag, namerights, isDataRight)); }
            catch (Exception ex) { return Json(ex.Message); }
        }

        [HttpGet]
        public IActionResult AddFuncitionsToListDetail(int id, int isDataRight)
        {
            try { return Json(_service.Detail(id, isDataRight)); }
            catch (Exception ex) { return Json(new { error = ex.Message }); }
        }

        [HttpPost]
        public IActionResult UpdateFormAndFunction(int ID, string codetagnew, string namerightnew, string descriptionnew, string mappingLinkWeb, int groupnew, int isHide, int isShift, int isCtrl, int isAlt, string functionkeynew, int _IsDataRight, string userName, int userID)
        {
            if (string.IsNullOrWhiteSpace(userName))
                userName = HttpContext.Session.GetString("LoginName") ?? "";
            var r = _service.Save(ID, codetagnew, namerightnew, descriptionnew, groupnew, isHide, _IsDataRight, userName);
            return Json(new { success = r.Success, message = r.Message });
        }

        [HttpPost]
        public IActionResult DeleteFormAndFunction(int ID, int isDataRight)
        {
            try
            {
                var r = _service.Delete(ID, isDataRight);
                return Json(new { success = r.Success, message = r.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult FunctionGroupTree(int isDataRight)
        {
            try { return Json(_service.GroupTree(isDataRight)); }
            catch (Exception ex) { return Json(new { code = -1, msg = ex.Message }); }
        }

        [HttpGet]
        public IActionResult FunctionGroupDetail(int id, int isDataRight)
        {
            try { return Json(_service.GroupDetail(id, isDataRight)); }
            catch (Exception ex) { return Json(new { code = -1, msg = ex.Message }); }
        }

        [HttpPost]
        public IActionResult FunctionGroupSave(int id, string code, string name, string description, int parentId, int isHide, int isDataRight)
        {
            var userName = HttpContext.Session.GetString("LoginName") ?? "";
            var r = _service.SaveGroup(id, code, name, description, parentId, isHide, isDataRight, userName);
            return Json(new { success = r.Success, message = r.Message });
        }

        [HttpPost]
        public IActionResult FunctionGroupDelete(int id, int isDataRight)
        {
            var r = _service.DeleteGroup(id, isDataRight);
            return Json(new { success = r.Success, message = r.Message });
        }
    }
}

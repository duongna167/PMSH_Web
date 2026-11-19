using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using BaseBusiness.Auth;
using Microsoft.AspNetCore.Mvc;
using Security.Helpers;
using Security.Services.Interfaces;

namespace Security.Controllers
{
    [RequireFunction("Sec_UserGroup_View")]
    public class UserGroupController : Controller
    {
        private readonly IUserGroupService _service;

        public UserGroupController(IUserGroupService service)
        {
            _service = service;
        }

        [RequireFunction("Sec_UserGroup_View")]
        public IActionResult UserGroup()
        {
            return PartialView("~/Views/Security/UserGroup.cshtml");
        }

        public IActionResult UserGroupData(string? code, string? name, int inactive = 0)
        {
            try
            {
                DataTable dataTable = _service.Search(code, name, inactive);
                var data = (from d in dataTable.AsEnumerable()
                            select d.Table.Columns.Cast<DataColumn>()
                                .ToDictionary(
                                    col => col.ColumnName,
                                    col =>
                                    {
                                        var value = d[col.ColumnName];
                                        if (value == DBNull.Value) return null;
                                        if (col.ColumnName == "CreatedDate" || col.ColumnName == "UpdatedDate" || col.ColumnName == "Inactive")
                                            return value;
                                        return value.ToString();
                                    }
                                )).ToList();
                return Json(new { totalCount = data.Count, data });
            }
            catch (Exception ex)
            {
                return Json(new { totalCount = 0, data = new List<object>(), error = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult UserGroupSave(string id, string codeUG, string nameUG, string descriptionaccty, string user, string inactive)
        {
            try
            {
                return ToJson(_service.Save(id, codeUG, nameUG, descriptionaccty, user, inactive));
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult UserGroupDelete(int id)
        {
            try
            {
                return ToJson(_service.Delete(id));
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        private IActionResult ToJson(OpResult r)
        {
            if (r.NotFound)
                return NotFound(new { success = false, message = r.Message });
            return Json(new { success = r.Success, message = r.Message, data = r.Data, errors = r.Errors });
        }
    }
}

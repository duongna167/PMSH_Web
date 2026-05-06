using System.Collections;
using System.Data;
using System.Linq;
using Administration.Services.Interfaces;
using BaseBusiness.BO;
using BaseBusiness.Model;
using BaseBusiness.util;
using Microsoft.AspNetCore.Mvc;

namespace Administration.Controllers
{
    [Route("/Administration/RateCode")]
    public class RateClassController : Controller
    {
        private readonly IRateClassService _svRateClass;
        public RateClassController(IRateClassService rateClass)
        {
            _svRateClass = rateClass;
        }
        [HttpGet("RateClass")] // Truyeenf DataGrid , script api
        public IActionResult RateClass()
        {
            return PartialView("~/Views/Administration/RateCode/RateClass.cshtml");
            // Truyền đường dẫn chuẩn vào để tìm đúng
        }



        [HttpGet("GetAllRateClass")]
        public async Task<IActionResult> RateClassGetAll(string? code, string? name, int inactive = 0)
        {
            try
            {
                DataTable dataTable = await _svRateClass.RateClassTypeData(code, name, inactive);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  ID = d["ID"]?.ToString() ?? "",
                                  Code = d["Code"]?.ToString() ?? "",
                                  Name = d["Name"]?.ToString() ?? "",
                                  Description = d["Description"]?.ToString() ?? "",
                                  CreatedDate = d["CreatedDate"] != DBNull.Value ? Convert.ToDateTime(d["CreatedDate"]) : (DateTime?)null,
                                  UpdatedDate = d["UpdatedDate"] != DBNull.Value ? Convert.ToDateTime(d["UpdatedDate"]) : (DateTime?)null,
                                  CreatedBy = d["CreatedBy"]?.ToString() ?? "",
                                  UpdatedBy = d["UpdatedBy"]?.ToString() ?? "",
                                  Inactive = d["Inactive"] != DBNull.Value ? Convert.ToInt32(d["Inactive"]) : 0,

                              }).ToList();
                return Json(new
                {
                    totalCount = result.Count, // 🔥 số bản ghi
                    data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }



        [HttpPost("RateClassSave")]
        public async Task<IActionResult> RateClassSave(string idRateClass, string codeClass, string nameClass, string descriptionaccty, string user, string inactive)
        {
            try
            {
                // Collect validation errors
                var errors = new List<object>();

                // Validate inputs
                if (string.IsNullOrWhiteSpace(codeClass))
                    errors.Add(new { field = "code", message = "Code is required." });
                else if (codeClass.Length > 50)
                    errors.Add(new { field = "code", message = "Code must be at most 50 characters." });
                if (string.IsNullOrWhiteSpace(nameClass))
                    errors.Add(new { field = "name", message = "Name is required." });
                else if (nameClass.Length > 50)
                    errors.Add(new { field = "name", message = "Name must be at most 50 characters." });

                if (!string.IsNullOrEmpty(descriptionaccty) && descriptionaccty.Length > 500)
                    errors.Add(new { field = "description", message = "Description must be at most 500 characters." });

                if (string.IsNullOrWhiteSpace(user))
                    return NotFound(new { success = false, message = "Rate Class not found UserID ." });
                else
                {
                    user = user.Trim().Trim('"');
                    if (user.Length > 100)
                        return NotFound(new { success = false, message = "Rate Class must be at most 100 characters ." });
                }

                if (inactive != null && inactive != "0" && inactive != "1")
                    errors.Add(new { field = "ckActive", message = "Active must be checked or uncheked." });

                // Validate ID format for update
                int parsedId = 0;
                bool isUpdate = false;
                if (!string.IsNullOrWhiteSpace(idRateClass) && idRateClass != "0")
                {
                    if (!int.TryParse(idRateClass, out parsedId) || parsedId <= 0)
                        return NotFound(new { success = false, message = "Invalid Rate Class ID format." });
                    else
                        isUpdate = true;
                }
                
                // Use system date/time at the moment of the operation (not BusinessDate)
                var systemNow = TextUtils.GetSystemDate();


                // Check for duplicate Code (case-insensitive) for insert/update
                if (!string.IsNullOrWhiteSpace(codeClass))
                {
                    var allRateClasses = PropertyUtils.ConvertToList<RateClassModel>(RateClassBO.Instance.FindAll()) ?? new List<RateClassModel>();
                    bool duplicate = allRateClasses.Any(r => string.Equals(r.Code?.Trim(), codeClass.Trim(), StringComparison.OrdinalIgnoreCase) && r.ID != parsedId);
                    if (duplicate)
                        errors.Add(new { field = "code", message = "Code already exists." });

                }

                // Return errors if any
                if (errors.Count != 0)
                {
                    return Json(new { success = false, message = "Validation failed.", errors });
                }

                // Prepare model
                RateClassModel _Model = new();
                _Model.Code = codeClass.Trim();
                _Model.Name = nameClass.Trim();
                _Model.Description = descriptionaccty ?? string.Empty;
                _Model.Inactive = inactive == "1";

                if (isUpdate)
                {
                    // Verify existing record
                    var existing = RateClassBO.Instance.FindByPrimaryKey(parsedId) as RateClassModel;
                    if (existing == null || existing.ID == 0)
                    {
                        return NotFound(new { success = false, message = $"Rate Class not found (ID = {parsedId})" });
                    }

                    _Model.ID = parsedId;
                    _Model.UpdatedBy = user;
                    _Model.UpdatedDate = systemNow;
                    _Model.CreatedBy = existing.CreatedBy;
                    _Model.CreatedDate = existing.CreatedDate;
                    RateClassBO.Instance.Update(_Model);
                    return Json(new
                    {
                        success = true,
                        message = $"Changes saved successfully ID: {_Model.ID}.",
                        data = new { id = _Model.ID }
                    });
                }
                else
                {
                    _Model.UpdatedBy = user;
                    _Model.CreatedBy = user;
                    _Model.CreatedDate = systemNow;
                    _Model.UpdatedDate = _Model.CreatedDate;
                    RateClassBO.Instance.Insert(_Model);
                    return Json(new { success = true, message = "Record has been created successfully.", data = new { id = _Model.ID } });

                }

            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        [HttpPost("RateClassDelete")]
        public IActionResult RateClassDelete(int id)
        {
            try
            {
                ArrayList arr = RateCodeBO.Instance.FindByAttribute("RateClassID", id);
                if (arr.Count > 0)
                {
                    return Json(new { success = false, message = "Rate Class is being referenced to in other modules.\nDelete failed.!" });
                }
                RateClassBO.Instance.Delete(id);

                return Json(new { success = true, message = $"Record was removed successfully ID: {id}." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}


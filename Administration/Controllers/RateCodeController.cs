using System.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Administration.Services.Interfaces;
using BaseBusiness.BO;
using BaseBusiness.Model;
using BaseBusiness.util;
using DevExpress.DataProcessing.InMemoryDataProcessor;
using Microsoft.AspNetCore.Mvc;

namespace Administration.Controllers
{
    [Route("/Administration/RateCode")]
    public class RateCodeController(IRateCodeService rateCodeService) : Controller
    {
        private readonly IRateCodeService _rateCodeSer = rateCodeService;

        [HttpGet("")]
        public IActionResult RateCode()
        {
            List<RateCodeModel> listRateCode = PropertyUtils.ConvertToList<RateCodeModel>(RateCodeBO.Instance.FindAll());
            List<RateCategoryModel> listRateCate = PropertyUtils.ConvertToList<RateCategoryModel>(RateCategoryBO.Instance.FindAll());
            List<RateClassModel> listRateClass = PropertyUtils.ConvertToList<RateClassModel>(RateClassBO.Instance.FindAll());
            ViewBag.RateCodeList = listRateCode;
            ViewBag.RateCateList = listRateCate;
            ViewBag.RateClass = listRateClass;
            return PartialView("~/Views/Administration/RateCode/RateCode.cshtml");
        }
        [HttpGet("GetAllRateCode")]
        public IActionResult GetAllRateCode(string rateCode, string rateCategory)
        {
            try
            {
                DataTable dataTable = _rateCodeSer.GetAllRateCode(rateCode, rateCategory);

                var data = (from d in dataTable.AsEnumerable()
                            select new
                            {
                                ID = d.Field<int>("ID"),
                                Sequence = d.Field<int>("Sequence"),
                                RateCode = d.Field<string>("RateCode") ?? "",
                                Category = d.Field<string>("Category") ?? "",
                                Description = d.Field<string>("Description") ?? ""
                            }).ToList();

                return Json(new
                {
                    totalCount = data.Count, // 🔥 số bản ghi
                    data
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    totalCount = 0,
                    data = new List<object>(),
                    error = ex.Message
                });
            }
        }

        [HttpGet("GetByID")]
        public async Task<IActionResult> GetByID(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Invalid ID"
                    });
                }
                var codeModel = RateCodeBO.Instance.FindByPrimaryKey(id) as RateCodeModel;
                if (codeModel == null || codeModel.ID == 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Rate Code not found (ID = {id})"
                    });
                }

                return Json(new
                {
                    success = true,
                    message = "Success",
                    data = codeModel
                });
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, $"GetByID failed with ID {id}");
                return Json(new
                {
                    success = false,
                    message = "Error: " + ex.Message
                });
            }
        }


        [HttpPost("Save")]
        public IActionResult Save([FromBody] RateCodeDto model)
        {
            try
            {
                Console.WriteLine($"Model: {System.Text.Json.JsonSerializer.Serialize(model)}");
                // Collect validation errors as field-message pairs
                var errors = new List<object>();

                if (string.IsNullOrWhiteSpace(model.RateCode))
                    errors.Add(new { field = "txtRateCode", message = "RateCode is required." });
                else if (model.RateCode.Length > 100)
                    errors.Add(new { field = "txtRateCode", message = "RateCode must be at most 100 characters." });

                if (!string.IsNullOrEmpty(model.Description) && model.Description.Length > 500)
                    errors.Add(new { field = "txtDescription", message = "Description must be at most 500 characters." });

                if (model.Sequence < 0)
                    errors.Add(new { field = "txtSequence", message = "Sequence must be greater than or equal to 0." });

                if (model.Display != 0 && model.Display != 1)
                    errors.Add(new { field = "display", message = "Display must be .Net 0 or C++." });

                if (model.DayUse != 0 && model.DayUse != 1)
                    errors.Add(new { field = "chkDayUse", message = "Day Use must be checked or uncheked." });

                if (model.UserID <= 0)
                    return NotFound(new { success = false, message = "Rate Code not found UserID ." });

                if (model.RateClass <= 0)
                    errors.Add(new { field = "modalRateCodeSel", message = "Rate Class is required." });

                // Check duplicate RateCode (case-insensitive)
                var allRateCodes = PropertyUtils.ConvertToList<RateCodeModel>(RateCodeBO.Instance.FindAll()) ?? [];
                if (!string.IsNullOrWhiteSpace(model.RateCode))
                {
                    bool duplicate = allRateCodes.Any(r => string.Equals(r.RateCode?.Trim(), model.RateCode.Trim(), StringComparison.OrdinalIgnoreCase) && r.ID != model.Id);
                    if (duplicate)
                        errors.Add(new { field = "modalRateCodeSel", message = "RateCode already exists." });
                }
                var allRateCategorys = PropertyUtils.ConvertToList<RateCategoryModel>(RateCategoryBO.Instance.FindAll()) ?? [];

                //Kiểm tra Category

                if (model.RateCategory <= 0)
                {
                    errors.Add(new { field = "modalRateCategorySel", message = "Rate Category is required." });
                }
                else
                {
                    // Kiểm tra duplicate RateCategory (ví dụ nếu cần)
                    bool duplicate = allRateCategorys.Any(r =>
                        string.Equals(r.Code?.Trim(), model.RateCategory.ToString(), StringComparison.OrdinalIgnoreCase)
                        && r.ID != model.Id);

                    if (duplicate)
                        errors.Add(new { field = "modalRateCategorySel", message = "Rate Category already exists." });
                }


                // Return errors if any
                if (errors.Count != 0)
                {
                    return Json(new { success = false, message = "Validation failed.", errors });
                }

                // Map entity for insert/update
                RateCodeModel entity;
                List<BusinessDateModel> businessDates = PropertyUtils.ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll());
                if (businessDates == null || businessDates.Count == 0)
                    return NotFound(new { success = false, message = "Business date not available. Contact system administrator." });

                if (model.Id > 0)
                {
                    var existing = RateCodeBO.Instance.FindByPrimaryKey(model.Id) as RateCodeModel;
                    if (existing == null || existing.ID == 0)
                        return NotFound(new { success = false, message = $"RateCode not found (ID = {model.Id})" });

                    entity = existing;
                    entity.UserUpdateID = model.UserID;
                    entity.UpdateDate = businessDates![0].BusinessDate;
                }
                else
                {
                    entity = new RateCodeModel();
                    entity.UserUpdateID = model.UserID;
                    entity.CreateDate = businessDates![0].BusinessDate;
                    entity.UpdateDate = businessDates![0].BusinessDate;
                }

                // Map properties
                entity.RateCode = model.RateCode?.Trim() ?? string.Empty;
                entity.Descripton = model.Description ?? string.Empty;
                entity.RateCategoryID = model.RateCategory;
                entity.RateClassID = model.RateClass;
                entity.Sequence = model.Sequence;
                entity.DefaultDisplay = (byte)Math.Clamp(model.Display, 0, 255);
                entity.DayUse = model.DayUse == 1;
                entity.Status = model.Active;
                entity.Negotiated = model.Negotiated;
                entity.IndividualOnly = model.IndividualOnly;
                entity.IsModify = model.IsModifiable;

                // Save entity
                if (model.Id > 0)
                {
                    RateCodeBO.Instance.Update(entity);
                    return Json(new { success = true, message = "Update Rate Code successfully.", data = new { id = model.Id } });
                }
                else
                {
                    RateCodeBO.Instance.Insert(entity);
                    return Json(new { success = true, message = "Create Rate Code successfully.", data = new { id = entity.ID } });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        [HttpPost("RateCodeDelete")]
        public IActionResult RateCodeDelete(int id)
        {
            try
            {
                if (RateCodeBO.Instance.FindByPrimaryKey(id) is not RateCodeModel existing || existing.ID == 0)
                {
                    return Ok(new { success = false, message = $"RateCode ID {id} not found." });
                }

                // Lấy danh sách RateCodeDetail
                var rateCodeDetails = PropertyUtils.ConvertToList<RateCodeDetailModel>(
                    RateCodeDetailBO.Instance.FindByAttribute("RateCodeID", id)
                );

                // Lấy danh sách UserRateCodePermission liên quan
                var userPermissions = PropertyUtils.ConvertToList<UserRateCodePermissionModel>(
                    UserRateCodePermissionBO.Instance.FindByAttribute("RateCodeID", id)
                );

                // Kiểm tra tồn tại dữ liệu trước khi xóa
                if (rateCodeDetails != null && rateCodeDetails.Count > 0)

                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Cannot delete this RateCode because it is used in details."
                    });
                }
                if (userPermissions != null && userPermissions.Count > 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Cannot delete this RateCode because it is used in user permissions.."
                    });
                }

                RateCodeBO.Instance.Delete(id);
                return Json(new { success = true, message = $"Record was removed successfully ID: {id}." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }


        public class RateCodeDto
        {
            public int UserID { get; set; } = 0;
            public int Id { get; set; } = 0;

            public string RateCode { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;

            public int RateClass { get; set; } = 0;
            public int RateCategory { get; set; } = 0;
            public int Sequence { get; set; } = 0;
            public int Display { get; set; } = 0;
            public int DayUse { get; set; } = 0;

            public bool Active { get; set; } = false;
            public bool Negotiated { get; set; } = false;
            public bool IndividualOnly { get; set; } = false;
            public bool IsModifiable { get; set; } = false;
        }
    }
}

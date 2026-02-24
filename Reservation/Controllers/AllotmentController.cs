using BaseBusiness.BO;
using BaseBusiness.Model;
using BaseBusiness.util;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Reservation.Services.Interfaces;
using System.Data;
using static BaseBusiness.util.ValidationUtils;

namespace Reservation.Controllers
{
    public class AllotmentController : Controller
    {

        private readonly IConfiguration _configuration;
        private readonly ILogger<AllotmentController> _logger;
        private readonly IMemoryCache _cache;
        private readonly IAllotmentService _iAllotmentService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AllotmentController(ILogger<AllotmentController> logger,
                IMemoryCache cache, IConfiguration configuration, IAllotmentService iAllotmentService, IHttpContextAccessor httpContextAccessor)
        {
            _cache = cache;
            _logger = logger;
            _configuration = configuration;
            _iAllotmentService = iAllotmentService;
            _httpContextAccessor = httpContextAccessor;

        }

        #region Allotment Type
        [HttpGet]
        public IActionResult GetAllotmentType(string code, string name, int inactive)
        {
            try
            {
                DataTable dataTable = _iAllotmentService.AllotmentType(code, name, inactive);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  Code = !string.IsNullOrEmpty(d["Code"].ToString()) ? d["Code"] : "",
                                  Name = !string.IsNullOrEmpty(d["Name"].ToString()) ? d["Name"] : "",
                                  Description = !string.IsNullOrEmpty(d["Description"].ToString()) ? d["Description"] : "",
                                  InactiveText = !string.IsNullOrEmpty(d["InactiveText"].ToString()) ? d["InactiveText"] : "",
                                  CreatedBy = !string.IsNullOrEmpty(d["CreatedBy"].ToString()) ? d["CreatedBy"] : "",
                                  CreatedDate = !string.IsNullOrEmpty(d["CreatedDate"].ToString()) ? d["CreatedDate"] : "",
                                  UpdatedBy = !string.IsNullOrEmpty(d["UpdatedBy"].ToString()) ? d["UpdatedBy"] : "",
                                  UpdatedDate = !string.IsNullOrEmpty(d["UpdatedDate"].ToString()) ? d["UpdatedDate"] : "",
                                  ID = !string.IsNullOrEmpty(d["ID"].ToString()) ? d["ID"] : "",
                                  Inactive = !string.IsNullOrEmpty(d["Inactive"].ToString()) ? d["Inactive"] : "",
                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        [HttpGet]
        public IActionResult AllotmentType()
        {
            return PartialView("~/Views/Reservation/Allotment/AllotmentType.cshtml");
        }
        [HttpPost]
        public IActionResult AllotmentTypeSave([FromBody] AllotmentTypeModel model)
        {
            string message = "";
            var listErrors = GetErrors(
                Check(model == null, "general", "Invalid data"),

                Check(model?.Code, "code", "Code is not blank."),
                Check(model?.Name, "name", "Name is not blank.")
            );

            if (listErrors.Count == 0 && model != null)
            {
                bool isDuplicate = AllotmentTypeBO.Instance
                    .IsDuplicateCode(model.Code, model.ID);

                var duplicateError = CheckDuplicate(
                    isDuplicate,
                    "code",
                    $"This code already exists: [{model.Code}]"
                );

                if (duplicateError != null)
                {
                    listErrors.Add(duplicateError);
                }
            }
            if (listErrors.Count > 0)
            {
                return Json(new { success = false, errors = listErrors });
            }
            try
            {
                var businessDates = PropertyUtils.ConvertToList<BusinessDateModel>(
                    BusinessDateBO.Instance.FindAll()
                );

                DateTime businessDate = businessDates[0].BusinessDate;
                if (model.ID == 0)
                {
                    model.CreateDate = businessDate;
                    model.CreatedDate = businessDate;
                    model.UpdateDate = DateTime.Now;
                    model.UpdatedDate = DateTime.Now;

                    AllotmentTypeBO.Instance.Insert(model);
                    message = "Insert successfully!";
                }
                else
                {
                    var oldData = (AllotmentTypeModel)AllotmentTypeBO.Instance.FindByPrimaryKey(model.ID);

                    if (oldData != null)
                    {
                        //model.Code = oldData.Code;
                        model.CreateBy = oldData.CreateBy;
                        model.CreatedBy = oldData.CreatedBy;
                        model.CreateDate = oldData.CreatedDate;
                        model.CreatedDate = oldData.CreatedDate;
                    }

                    model.UpdateDate = DateTime.Now;
                    model.UpdatedDate = DateTime.Now;

                    AllotmentTypeBO.Instance.Update(model);
                    message = "Update successfully!";
                }

            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Json(new { success = false, message });
            }

            return Json(new { success = true, message });
        }
        [HttpPost]
        public IActionResult DeleteAllotmentType(int id)
        {
            try
            {
                AllotmentTypeBO.Instance.Delete(id);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, ex.Message });
            }

            return Json(new { success = true });
        }
        #endregion

        #region Allotment Stage
        [HttpGet]
        public IActionResult GetAllotmentStage(string code, string name, int inactive)
        {
            try
            {
                DataTable dataTable = _iAllotmentService.AllotmentStage(code, name, inactive);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  Code = !string.IsNullOrEmpty(d["Code"].ToString()) ? d["Code"] : "",
                                  Name = !string.IsNullOrEmpty(d["Name"].ToString()) ? d["Name"] : "",
                                  Description = !string.IsNullOrEmpty(d["Description"].ToString()) ? d["Description"] : "",
                                  InactiveText = !string.IsNullOrEmpty(d["InactiveText"].ToString()) ? d["InactiveText"] : "",
                                  CreatedBy = !string.IsNullOrEmpty(d["CreatedBy"].ToString()) ? d["CreatedBy"] : "",
                                  CreatedDate = !string.IsNullOrEmpty(d["CreatedDate"].ToString()) ? d["CreatedDate"] : "",
                                  UpdatedBy = !string.IsNullOrEmpty(d["UpdatedBy"].ToString()) ? d["UpdatedBy"] : "",
                                  UpdatedDate = !string.IsNullOrEmpty(d["UpdatedDate"].ToString()) ? d["UpdatedDate"] : "",
                                  ID = !string.IsNullOrEmpty(d["ID"].ToString()) ? d["ID"] : "",
                                  Inactive = !string.IsNullOrEmpty(d["Inactive"].ToString()) ? d["Inactive"] : "",
                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        public IActionResult AllotmentStage()
        {
            return PartialView("~/Views/Reservation/Allotment/AllotmentStage.cshtml");
        }
        [HttpPost]
        public IActionResult AllotmentStageSave([FromBody] AllotmentStageModel model)
        {
            string message = "";
            var listErrors = GetErrors(
                Check(model == null, "general", "Invalid data"),

                Check(model?.Code, "code", "Code is not blank."),
                Check(model?.Name, "name", "Name is not blank.")
            );

            if (listErrors.Count == 0 && model != null)
            {
                bool isDuplicate = AllotmentStageBO.Instance
                    .IsDuplicateCode(model.Code, model.ID);

                var duplicateError = CheckDuplicate(
                    isDuplicate,
                    "code",
                    $"This code already exists: [{model.Code}]"
                );

                if (duplicateError != null)
                {
                    listErrors.Add(duplicateError);
                }
            }
            if (listErrors.Count > 0)
            {
                return Json(new { success = false, errors = listErrors });
            }
            try
            {
                var businessDates = PropertyUtils.ConvertToList<BusinessDateModel>(
                    BusinessDateBO.Instance.FindAll()
                );

                DateTime businessDate = businessDates[0].BusinessDate;
                if (model.ID == 0)
                {
                    model.CreateDate = businessDate;
                    model.CreatedDate = businessDate;
                    model.UpdateDate = DateTime.Now;
                    model.UpdatedDate = DateTime.Now;

                    AllotmentStageBO.Instance.Insert(model);
                    message = "Insert successfully!";
                }
                else
                {
                    var oldData = (AllotmentStageModel)AllotmentStageBO.Instance.FindByPrimaryKey(model.ID);

                    if (oldData != null)
                    {
                        //model.Code = oldData.Code;
                        model.CreateBy = oldData.CreateBy;
                        model.CreatedBy = oldData.CreatedBy;
                        model.CreateDate = oldData.CreatedDate;
                        model.CreatedDate = oldData.CreatedDate;
                    }

                    model.UpdateDate = DateTime.Now;
                    model.UpdatedDate = DateTime.Now;

                    AllotmentStageBO.Instance.Update(model);
                    message = "Update successfully!";
                }

            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Json(new { success = false, message });
            }

            return Json(new { success = true, message });

        }
        [HttpPost]
        public IActionResult DeleteAllotmentStage(int id)
        {
            try
            {
                AllotmentStageBO.Instance.Delete(id);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, ex.Message });
            }

            return Json(new { success = true });
        }
        #endregion


    }
}

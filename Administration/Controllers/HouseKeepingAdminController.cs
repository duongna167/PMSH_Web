using Administration.Services.Implements;
using Administration.Services.Interfaces;
using BaseBusiness.BO;
using BaseBusiness.Model;
using BaseBusiness.util;
using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.IISIntegration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BaseBusiness.util.ValidationUtils;

namespace Administration.Controllers
{
    public class HouseKeepingAdminController : Controller
    {
        private readonly IConfiguration _configuration;

        private readonly ILogger<HouseKeepingAdminController> _logger;
        private readonly IMemoryCache _cache;
        private readonly IHouseKeepingAdminService _iHouseKeepingAdminService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public HouseKeepingAdminController(ILogger<HouseKeepingAdminController> logger,
                IMemoryCache cache, IConfiguration configuration, IHttpContextAccessor httpContextAccessor, IHouseKeepingAdminService iHouseKeepingAdminService)
        {
            _cache = cache;
            _logger = logger;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _iHouseKeepingAdminService = iHouseKeepingAdminService;
        }

        #region Attendants
        public IActionResult Attendants()
        {
            List<FloorModel> listfloor = PropertyUtils.ConvertToList<FloorModel>(FloorBO.Instance.FindAll());
            ViewBag.FloorList = listfloor;
            List<hkpSectionModel> listsshkp = PropertyUtils.ConvertToList<hkpSectionModel>(hkpSectionBO.Instance.FindAll());
            ViewBag.hkpSectionList = listsshkp;
            return View("~/Views/Administration/HouseKeepingAdmin/Attendants.cshtml");
        }

        [HttpGet]
        public IActionResult AttendantsData()
        {
            try
            {
                var getattendantsData = hkpAttendantBO.AttendantsData();
                DataTable dt = PropertyUtils.ConvertToDataTable(getattendantsData);
                var result = (from d in dt.AsEnumerable()
                              select new
                              {
                                  ID = d["ID"]?.ToString() ?? "",
                                  Name = d["Name"]?.ToString() ?? "",
                                  MobileNo = d["MobileNo"]?.ToString() ?? "",
                                  FloorID = d["FloorID"]?.ToString() ?? "",
                                  SectionID = d["SectionID"]?.ToString() ?? "",
                                  JobCode = d["JobCode"]?.ToString() ?? "",
                                  Monday = d["Monday"]?.ToString() ?? "",
                                  Tuesday = d["Tuesday"]?.ToString() ?? "",
                                  Wednesday = d["Wednesday"]?.ToString() ?? "",
                                  Thursday = d["Thursday"]?.ToString() ?? "",
                                  Friday = d["Friday"]?.ToString() ?? "",
                                  Saturday = d["Saturday"]?.ToString() ?? "",
                                  Sunday = d["Sunday"]?.ToString() ?? "",
                                  IsActive = d["IsActive"]?.ToString() ?? "",
                                  CreatedBy = d["CreatedBy"]?.ToString() ?? "",
                                  CreatedDate = d["CreatedDate"]?.ToString() ?? "",
                                  UpdatedBy = d["UpdatedBy"]?.ToString() ?? "",
                                  UpdatedDate = d["UpdatedDate"]?.ToString() ?? "",
                                  Floor = d["Floor"]?.ToString() ?? "",
                                  Section = d["Section"]?.ToString() ?? ""


                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        [HttpPost]
        public IActionResult AttendantsDelete(int id)
        {
            try
            {
                ArrayList arr = hkpTaskSheetBO.Instance.FindByAttribute("AttendantID", id);
                if (arr.Count == 0)
                    arr = hkpAttendantPointBO.Instance.FindByAttribute("AttendantID", id);
                if (arr.Count > 0)
                {
                    return Json(new { success = false, message = "Attendant is being referenced to in other modules.\nDelete failed.!" });
                 
                }
                hkpAttendantBO.Instance.Delete(id);

                return Json(new { success = true, message = "Success Delete!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult AttendantsSave(int id, string name, string mobileNo, string jobCode,int floorID,int sectionID, int isActive, int sunday, int monday, int tuesday, int wednesday, int thursday, int friday, int saturday,string user)
        {
            user = user?.Replace("\"", "").Trim();
            List<BusinessDateModel> businessDateModel = PropertyUtils.ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll());
            try
            {

                hkpAttendantModel model = new hkpAttendantModel();
                if (id > 0)
                    model = (hkpAttendantModel)hkpAttendantBO.Instance.FindByPrimaryKey(id);
                model.Name = name;
                model.MobileNo = mobileNo;
                model.FloorID = floorID;
                model.SectionID = sectionID;
                model.JobCode = jobCode;
                model.IsActive = (isActive == 1);
                model.Sunday = (sunday == 1);
                model.Monday = (monday == 1);
                model.Tuesday = (tuesday == 1);
                model.Wednesday = (wednesday == 1);
                model.Thursday = (thursday == 1);
                model.Friday = (friday == 1);
                model.Saturday = (saturday == 1);
                model.UpdatedBy = user;
                model.UpdatedDate = businessDateModel[0].BusinessDate;
                if (id == 0)
                {
                    model.CreatedBy = model.UpdatedBy;
                    model.CreatedDate = model.UpdatedDate;
                    hkpAttendantBO.Instance.Insert(model);
                }
                else
                {
                    hkpAttendantBO.Instance.Update(model);
                }


                return Json(new { success = true, message = "Insert success!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        #endregion

        #region FacilityCode
        public IActionResult FacilityCode()
        {
            List<hkpFacilityCategoryModel> facilityCategory = PropertyUtils.ConvertToList<hkpFacilityCategoryModel>(FacilityCategoryBO.Instance.FindAll());
            ViewBag.FacilityCategory = facilityCategory;
            return View("~/Views/Administration/HouseKeepingAdmin/FacilityCode.cshtml");
        }
        [HttpGet]
        public IActionResult FacilityCodeData(string code,int isActive)
        {
            code = code ?? "";
            try
            {
                DataTable dataTable = _iHouseKeepingAdminService.FacilityCodeData(code, isActive);

                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  ID = d["ID"]?.ToString() ?? "",
                                  Code = d["Code"]?.ToString() ?? "",
                                  Description = d["Description"]?.ToString() ?? "",
                                  InactiveText = d["InactiveText"]?.ToString() ?? "",
                                  CreatedBy = d["CreatedBy"]?.ToString() ?? "",
                                  CreatedDate = d["CreatedDate"] == DBNull.Value ? null : Convert.ToDateTime(d["CreatedDate"]).ToString("yyyy-MM-dd HH:mm:ss"),
                                  UpdatedBy = d["UpdatedBy"]?.ToString() ?? "",
                                  UpdatedDate = d["UpdatedDate"] == DBNull.Value ? null : Convert.ToDateTime(d["UpdatedDate"]).ToString("yyyy-MM-dd HH:mm:ss"),
                                  Inactive = d["Inactive"]?.ToString() ?? ""

                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetFacilityCodeById(int id)
        {
            var data = (hkpFacilityCodeModel)hkpFacilityCodeBO.Instance.FindByPrimaryKey(id);

            if (data == null)
            {
                return Json(new { inactive = 0, facilityCategoryID = 0, sequence = 0 });
            }

            return Json(new
            {
                inactive = data.Inactive,
                facilityCategoryID = data.FacilityCategoryID,
                sequence = data.Sequence
            });
        }

        [HttpPost]
        public IActionResult FacilityCodeSave([FromBody] hkpFacilityCodeModel model)
        {
            string message = "";
            var listErrors = GetErrors(
                Check(model == null, "general", "Invalid data"),
                Check(model?.Sequence < 0, "sequence", "Sequence must be >= 0"),
                Check(model?.Code, "code", "Code is not blank."),
                Check(model?.FacilityCategoryID, "facilityCategoryID", "Please select a facility category. ")
            );

            if (listErrors.Count > 0)
            {
                return Json(new { success = false, errors = listErrors });
            }
            try
            {

                if (model.ID == 0)
                {
                    model.CreatedDate = DateTime.Now;
                    model.UpdatedDate = DateTime.Now;

                    hkpFacilityCodeBO.Instance.Insert(model);
                    message = "Insert successfully!";
                }
                else
                {
                    var oldData = (hkpFacilityCodeModel)hkpFacilityCodeBO.Instance.FindByPrimaryKey(model.ID);

                    if (oldData != null)
                    {
                        model.CreatedBy = oldData.CreatedBy;
                        model.CreatedDate = oldData.CreatedDate;
                    }

                    model.UpdatedDate = DateTime.Now;

                    hkpFacilityCodeBO.Instance.Update(model);
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
        public IActionResult FacilityCodeDelete(int id)
        {
            try
            {
                ArrayList arr = hkpTaskSheetFacilityBO.Instance.FindByAttribute("FacilityCodeID", id);
           
                if (arr.Count > 0)
                {
                    return Json(new { success = false, message = "FacilityCode exist in TaskSheetFacility. You can not delete!" });

                }
                hkpFacilityCodeBO.Instance.Delete(id);

                return Json(new { success = true, message = "Success Delete!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        #endregion

        #region FacilityCategory
        [HttpGet]
        public IActionResult GetFacilityCategory(string code, string name, int inactive)
        {
            try
            {
                DataTable dataTable = _iHouseKeepingAdminService.FacilityCategoryData(code, name, inactive);
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
        public IActionResult FacilityCategory()
        {
            return View("~/Views/Administration/HouseKeepingAdmin/FacilityCategory.cshtml");
        }
        [HttpPost]
        public IActionResult FacilityCategorySave([FromBody] hkpFacilityCategoryModel model)
        {
            string message = "";
            var listErrors = GetErrors(
                Check(model, "general", "Invalid data"),

                Check(model?.Code, "code", "Code is not blank."),
                Check(model?.Name, "name", "Name is not blank.")
            );

            if (listErrors.Count > 0)
            {
                return Json(new { success = false, errors = listErrors });
            }
            try
            {
                if (model.ID == 0)
                {
                    model.CreatedDate = DateTime.Now;
                    model.UpdatedDate = DateTime.Now;

                    FacilityCategoryBO.Instance.Insert(model);
                    message = "Insert successfully!";
                }
                else
                {
                    var oldData = (hkpFacilityCategoryModel)FacilityCategoryBO.Instance.FindByPrimaryKey(model.ID);

                    if (oldData != null)
                    {
                        model.CreatedBy = oldData.CreatedBy;
                        model.CreatedDate = oldData.CreatedDate;
                    }

                    model.UpdatedDate = DateTime.Now;

                    FacilityCategoryBO.Instance.Update(model);
                    message = "Update successfully!";
                }
                return Json(new { success = true, message });

            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }

        }
        [HttpPost]
        public IActionResult DeleteFacilityCategory(int id)
        {
            try
            {
                FacilityCategoryBO.Instance.Delete(id);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, ex.Message });
            }

            return Json(new { success = true });
        }
        #endregion

        #region Section
        public IActionResult Section()
        {
            List<hkpSectionModel> hkpSection = PropertyUtils.ConvertToList<hkpSectionModel>(hkpSectionBO.Instance.FindAll());
            ViewBag.hkpSection = hkpSection;

            return View("~/Views/Administration/HouseKeepingAdmin/Section.cshtml");
        }
        [HttpGet]
        public IActionResult SectionData(string code, string description, int isActive)
        {
            code = code ?? "";
            description = description ?? "";
            try
            {
                DataTable dataTable = _iHouseKeepingAdminService.SectionData(code, description, isActive);

                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  ID = d["ID"]?.ToString() ?? "",
                                  Code = d["Code"]?.ToString() ?? "",
                                  Description = d["Description"]?.ToString() ?? "",
                                  InactiveText = d["InactiveText"]?.ToString() ?? "",
                                  CreatedBy = d["CreatedBy"]?.ToString() ?? "",
                                  CreatedDate = d["CreatedDate"] == DBNull.Value ? null : Convert.ToDateTime(d["CreatedDate"]).ToString("yyyy-MM-dd HH:mm:ss"),
                                  UpdatedBy = d["UpdatedBy"]?.ToString() ?? "",
                                  UpdatedDate = d["UpdatedDate"] == DBNull.Value ? null : Convert.ToDateTime(d["UpdatedDate"]).ToString("yyyy-MM-dd HH:mm:ss"),
                                  Inactive = d["Inactive"]?.ToString() ?? ""

                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult SectionSave(int id, string codenew, string descriptionnew, int isActive, string user)
        {
            var pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();

                user = (user ?? string.Empty).Replace("\"", "").Trim();

                var businessDates = PropertyUtils.ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll());
                var businessDate = businessDates[0].BusinessDate;

                hkpSectionModel model;
                bool isNew = (id == 0);

                if (isNew)
                {
                    model = new hkpSectionModel
                    {
                        Code = codenew?.Trim(),
                        Description = descriptionnew?.Trim(),
                        Inactive = (isActive == 1),
                        CreatedBy = user,
                        CreatedDate = businessDate,
                        UpdatedBy = user,
                        UpdatedDate = businessDate
                    };

                    hkpSectionBO.Instance.Insert(model);
                }
                else
                {
                    model = (hkpSectionModel)hkpSectionBO.Instance.FindByPrimaryKey(id);
                    if (model == null)
                    {
                        throw new Exception($"Không tìm thấy Section có ID = {id}");
                    }

                    model.Code = codenew?.Trim();
                    model.Description = descriptionnew?.Trim();
                    model.Inactive = (isActive == 1);
                    model.UpdatedBy = user;
                    model.UpdatedDate = businessDate;

                    hkpSectionBO.Instance.Update(model);
                }

                pt.CommitTransaction();

                return Json(new
                {
                    success = true,
                    message = isNew ? "Insert success!" : "Update success!"
                });
            }
            catch (Exception ex)
            {
                pt.RollBack();
                return BadRequest(new { success = false, message = ex.Message });
            }
            finally
            {
                pt.CloseConnection();
            }
        }
        [HttpPost]
        public IActionResult SectionDelete(int id)
        {
            try
            {

                hkpSectionBO.Instance.Delete(id);

                return Json(new { success = true, message = "Success Delete!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        #endregion

        #region HouseKeepingEmployee

        public IActionResult HouseKeepingEmployee()
        {
            List<hkpEmployeeModel> hkpEmployee = PropertyUtils.ConvertToList<hkpEmployeeModel>(hkpEmployeeBO.Instance.FindAll());
            ViewBag.hkpEmployee = hkpEmployee;

            return View("~/Views/Administration/HouseKeepingAdmin/HouseKeepingEmployee.cshtml");
        }
        [HttpGet]
        public IActionResult HouseKeepingEmployeeData( string description, int isActive)
        {
      
            description = description ?? "";
            try
            {
                DataTable dataTable = _iHouseKeepingAdminService.HouseKeepingEmployeeData(description, isActive);

                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  ID = d["ID"]?.ToString() ?? "",
                                  Name = d["Name"]?.ToString() ?? "",
                 
                                  InactiveText = d["InactiveText"]?.ToString() ?? "",
                                  CreatedBy = d["CreatedBy"]?.ToString() ?? "",
                                  CreatedDate = d["CreatedDate"] == DBNull.Value ? null : Convert.ToDateTime(d["CreatedDate"]).ToString("yyyy-MM-dd HH:mm:ss"),
                                  UpdatedBy = d["UpdatedBy"]?.ToString() ?? "",
                                  UpdatedDate = d["UpdatedDate"] == DBNull.Value ? null : Convert.ToDateTime(d["UpdatedDate"]).ToString("yyyy-MM-dd HH:mm:ss"),
                                  Inactive = d["Inactive"]?.ToString() ?? ""

                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult HouseKeepingEmployeeSave(int id, string codenew, string descriptionnew, int isActive, string user)
        {
            var pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();

                user = (user ?? string.Empty).Replace("\"", "").Trim();

                var businessDates = PropertyUtils.ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll());
                var businessDate = businessDates[0].BusinessDate;

                hkpEmployeeModel model;
                bool isNew = (id == 0);

                if (isNew)
                {
                    model = new hkpEmployeeModel
                    {
                        Name = codenew?.Trim(),
                        Description = descriptionnew?.Trim(),
                        Inactive = (isActive == 1),
                        CreatedBy = user,
                        CreatedDate = businessDate,
                        UpdatedBy = user,
                        UpdatedDate = businessDate
                    };

                    hkpEmployeeBO.Instance.Insert(model);
                }
                else
                {
                    model = (hkpEmployeeModel)hkpEmployeeBO.Instance.FindByPrimaryKey(id);
                    if (model == null)
                    {
                        throw new Exception($"Không tìm thấy Section có ID = {id}");
                    }

                    model.Name = codenew?.Trim();
                    model.Description = descriptionnew?.Trim();
                    model.Inactive = (isActive == 1);
                    model.UpdatedBy = user;
                    model.UpdatedDate = businessDate;

                    hkpEmployeeBO.Instance.Update(model);
                }

                pt.CommitTransaction();

                return Json(new
                {
                    success = true,
                    message = isNew ? "Insert success!" : "Update success!"
                });
            }
            catch (Exception ex)
            {
                pt.RollBack();
                return BadRequest(new { success = false, message = ex.Message });
            }
            finally
            {
                pt.CloseConnection();
            }
        }
        [HttpPost]
        public IActionResult HouseKeepingEmployeeDelete(int id)
        {
            try
            {

                hkpEmployeeBO.Instance.Delete(id);

                return Json(new { success = true, message = "Success Delete!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        #endregion

        #region  LostAndFoundZone

        public IActionResult LostAndFoundZone()
        {

            return View("~/Views/Administration/HouseKeepingAdmin/LostAndFoundZone.cshtml");
        }
        [HttpGet]
        public IActionResult LostAndFoundZoneData()
        {
           
            try
            {
                DataTable dataTable = TextUtils.Select("select a.ID,a.Code,a.Name,a.Inactive,a.CreatedBy,a.CreatedDate,a.UpdatedBy,a.UpdateDate from lafZone a");

                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  ID = d["ID"]?.ToString() ?? "",
                                  Code = d["Code"]?.ToString() ?? "",
                                  Name = d["Name"]?.ToString() ?? "",
         
                                  CreatedBy = d["CreatedBy"]?.ToString() ?? "",
                                  CreatedDate = d["CreatedDate"] == DBNull.Value ? null : Convert.ToDateTime(d["CreatedDate"]).ToString("yyyy-MM-dd HH:mm:ss"),
                                  UpdatedBy = d["UpdatedBy"]?.ToString() ?? "",
                                  UpdateDate = d["UpdateDate"] == DBNull.Value ? null : Convert.ToDateTime(d["UpdateDate"]).ToString("yyyy-MM-dd HH:mm:ss"),
                                  Inactive = d["Inactive"]?.ToString() ?? ""

                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        [HttpPost]
        public IActionResult LostAndFoundZoneSave(int id, string codenew, string descriptionnew, int isActive, string user)
        {
            var pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();

                user = (user ?? string.Empty).Replace("\"", "").Trim();

                var businessDates = PropertyUtils.ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll());
                var businessDate = businessDates[0].BusinessDate;

                lafZoneModel model;
                bool isNew = (id == 0);

                if (isNew)
                {
                    model = new lafZoneModel
                    {
                        Code = codenew?.Trim(),
                        Name = descriptionnew?.Trim(),
                        Inactive = (isActive == 1),
                        CreatedBy = user,
                        CreatedDate = businessDate,
                        UpdatedBy = user,
                        UpdateDate = businessDate
                    };

                    lafZoneBO.Instance.Insert(model);
                }
                else
                {
                    model = (lafZoneModel)lafZoneBO.Instance.FindByPrimaryKey(id);
                    if (model == null)
                    {
                        throw new Exception($"Không tìm thấy lafZone có ID = {id}");
                    }

                    model.Code = codenew?.Trim();
                    model.Name = descriptionnew?.Trim();
                    model.Inactive = (isActive == 1);
                    model.UpdatedBy = user;
                    model.UpdateDate = businessDate;

                    hkpSectionBO.Instance.Update(model);
                }

                pt.CommitTransaction();

                return Json(new
                {
                    success = true,
                    message = isNew ? "Insert success!" : "Update success!"
                });
            }
            catch (Exception ex)
            {
                pt.RollBack();
                return BadRequest(new { success = false, message = ex.Message });
            }
            finally
            {
                pt.CloseConnection();
            }
        }
        [HttpPost]
        public IActionResult LostAndFoundZoneDelete(int id)
        {
            try
            {

                lafZoneBO.Instance.Delete(id);

                return Json(new { success = true, message = "Success Delete!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        #endregion

        #region LostAndFoundStatus
        public IActionResult LostAndFoundStatus()
        {

            return View("~/Views/Administration/HouseKeepingAdmin/LostAndFoundStatus.cshtml");
        }
        [HttpGet]
        public IActionResult LostAndFoundStatusData()
        {

            try
            {
                DataTable dataTable = TextUtils.Select("select * from lafStatus");

                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  ID = d["ID"]?.ToString() ?? "",
                                  Code = d["Code"]?.ToString() ?? "",
                                  Name = d["Name"]?.ToString() ?? "",

                                  CreatedBy = d["CreatedBy"]?.ToString() ?? "",
                                  CreatedDate = d["CreatedDate"] == DBNull.Value ? null : Convert.ToDateTime(d["CreatedDate"]).ToString("yyyy-MM-dd HH:mm:ss"),
                                  UpdatedBy = d["UpdatedBy"]?.ToString() ?? "",
                                  UpdateDate = d["UpdateDate"] == DBNull.Value ? null : Convert.ToDateTime(d["UpdateDate"]).ToString("yyyy-MM-dd HH:mm:ss"),
                                  Inactive = d["Inactive"]?.ToString() ?? "",
                                  IsDefault = d["IsDefault"]?.ToString() ?? ""

                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        [HttpPost]
        public IActionResult LostAndFoundStatusSave(int id, string codenew, string descriptionnew, int isActive, int isDefault, string user)
        {
            var pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();

                user = (user ?? string.Empty).Replace("\"", "").Trim();

                var businessDates = PropertyUtils.ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll());
                var businessDate = businessDates[0].BusinessDate;

                lafStatusModel model;
                bool isNew = (id == 0);

                if (isNew)
                {
                    model = new lafStatusModel
                    {
                        Code = codenew?.Trim(),
                        Name = descriptionnew?.Trim(),
                        Inactive = (isActive == 1),
                        IsDefault = (isDefault == 1),
                        CreatedBy = user,
                        CreatedDate = businessDate,
                        UpdatedBy = user,
                        UpdateDate = businessDate
                    };

                    lafStatusBO.Instance.Insert(model);
                }
                else
                {
                    model = (lafStatusModel)lafStatusBO.Instance.FindByPrimaryKey(id);
                    if (model == null)
                    {
                        throw new Exception($"Không tìm thấy lafZone có ID = {id}");
                    }

                    model.Code = codenew?.Trim();
                    model.Name = descriptionnew?.Trim();
                    model.Inactive = (isActive == 1);
                    model.IsDefault = (isDefault == 1);
                    model.UpdatedBy = user;
                    model.UpdateDate = businessDate;

                    lafStatusBO.Instance.Update(model);
                }

                pt.CommitTransaction();

                return Json(new
                {
                    success = true,
                    message = isNew ? "Insert success!" : "Update success!"
                });
            }
            catch (Exception ex)
            {
                pt.RollBack();
                return BadRequest(new { success = false, message = ex.Message });
            }
            finally
            {
                pt.CloseConnection();
            }
        }
        [HttpPost]
        public IActionResult LostAndFoundStatusDelete(int id)
        {
            try
            {

                lafStatusBO.Instance.Delete(id);

                return Json(new { success = true, message = "Success Delete!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        #endregion

        #region LostAndFoundQuality
        public IActionResult LostAndFoundQuality()
        {

            return View("~/Views/Administration/HouseKeepingAdmin/LostAndFoundQuality.cshtml");
        }
        [HttpGet]
        public IActionResult LostAndFoundQualityData()
        {

            try
            {
                DataTable dataTable = TextUtils.Select("select a.ID,a.Code,a.Name,a.Inactive ,a.CreatedBy,a.CreatedDate,a.UpdatedBy,a.UpdateDate from lafQualityType a");

                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  ID = d["ID"]?.ToString() ?? "",
                                  Code = d["Code"]?.ToString() ?? "",
                                  Name = d["Name"]?.ToString() ?? "",

                                  CreatedBy = d["CreatedBy"]?.ToString() ?? "",
                                  CreatedDate = d["CreatedDate"] == DBNull.Value ? null : Convert.ToDateTime(d["CreatedDate"]).ToString("yyyy-MM-dd HH:mm:ss"),
                                  UpdatedBy = d["UpdatedBy"]?.ToString() ?? "",
                                  UpdateDate = d["UpdateDate"] == DBNull.Value ? null : Convert.ToDateTime(d["UpdateDate"]).ToString("yyyy-MM-dd HH:mm:ss"),
                                  Inactive = d["Inactive"]?.ToString() ?? ""

                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        [HttpPost]
        public IActionResult LostAndFoundQualitySave(int id, string codenew, string descriptionnew, int isActive,  string user)
        {
            var pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();

                user = (user ?? string.Empty).Replace("\"", "").Trim();

                var businessDates = PropertyUtils.ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll());
                var businessDate = businessDates[0].BusinessDate;

                lafQualityTypeModel model;
                bool isNew = (id == 0);

                if (isNew)
                {
                    model = new lafQualityTypeModel
                    {
                        Code = codenew?.Trim(),
                        Name = descriptionnew?.Trim(),
                        Inactive = (isActive == 1),
                  
                        CreatedBy = user,
                        CreatedDate = businessDate,
                        UpdatedBy = user,
                        UpdateDate = businessDate
                    };

                    lafQualityTypeBO.Instance.Insert(model);
                }
                else
                {
                    model = (lafQualityTypeModel)lafQualityTypeBO.Instance.FindByPrimaryKey(id);
                    if (model == null)
                    {
                        throw new Exception($"Không tìm thấy lafZone có ID = {id}");
                    }

                    model.Code = codenew?.Trim();
                    model.Name = descriptionnew?.Trim();
                    model.Inactive = (isActive == 1);
                    model.UpdatedBy = user;
                    model.UpdateDate = businessDate;

                    lafQualityTypeBO.Instance.Update(model);
                }

                pt.CommitTransaction();

                return Json(new
                {
                    success = true,
                    message = isNew ? "Insert success!" : "Update success!"
                });
            }
            catch (Exception ex)
            {
                pt.RollBack();
                return BadRequest(new { success = false, message = ex.Message });
            }
            finally
            {
                pt.CloseConnection();
            }
        }
        [HttpPost]
        public IActionResult LostAndFoundQualityDelete(int id)
        {
            try
            {

                lafQualityTypeBO.Instance.Delete(id);

                return Json(new { success = true, message = "Success Delete!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        #endregion
    }
}

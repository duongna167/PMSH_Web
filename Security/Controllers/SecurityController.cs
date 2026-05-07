using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BaseBusiness.BO;
using BaseBusiness.Model;
using BaseBusiness.util;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Security.Services.Interfaces;
using static DevExpress.Xpo.Helpers.AssociatedCollectionCriteriaHelper;
namespace Security.Controllers
{
    public class SecurityController : Controller
    {
        private readonly ILogger<SecurityController> _logger;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        private readonly ISecurityService _iSecurityService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SecurityController(ILogger<SecurityController> logger, IMemoryCache cache, IConfiguration configuration, ISecurityService iSecurityService, IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _cache = cache;
            _configuration = configuration;
            _iSecurityService = iSecurityService;
            _httpContextAccessor = httpContextAccessor;
        }

        #region Users Group 
        public IActionResult UserGroup()
        {
            return PartialView();
        }

        public IActionResult UserGroupData(string? code, string? name, int inactive = 0)
        {
            try
            {
                DataTable dataTable = _iSecurityService.UserGroupData(code, name, inactive);

                var data = (from d in dataTable.AsEnumerable()
                            select d.Table.Columns.Cast<DataColumn>()
                                //.Where(col => col.ColumnName != "AllotmentStageID" && col.ColumnName != "flag" && col.ColumnName != "Total")
                                .ToDictionary(
                                    col => col.ColumnName,
                                    col =>
                                    {
                                        var value = d[col.ColumnName];
                                        if (value == DBNull.Value) return null;

                                        // CreatedDate: KHÔNG ToString
                                        if (col.ColumnName == "CreatedDate" || col.ColumnName == "UpdatedDate" || col.ColumnName == "Inactive")
                                            return value;

                                        // Các field khác: ToString
                                        return value.ToString();
                                    }
                                )).ToList();
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

        [HttpPost]
        public async Task<IActionResult> UserGroupSave(string id, string codeUG, string nameUG, string descriptionaccty, string user, string inactive)
        {
            try
            {
                // Collect validation errors
                var errors = new List<object>();

                // Validate inputs
                if (string.IsNullOrWhiteSpace(codeUG))
                    errors.Add(new { field = "code", message = "Code is required." });
                else if (codeUG.Length > 50)
                    errors.Add(new { field = "code", message = "Code must be at most 50 characters." });
                if (string.IsNullOrWhiteSpace(nameUG))
                    errors.Add(new { field = "name", message = "Name is required." });
                else if (nameUG.Length > 50)
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
                if (!string.IsNullOrWhiteSpace(id) && id != "0")
                {
                    if (!int.TryParse(id, out parsedId) || parsedId <= 0)
                        return NotFound(new { success = false, message = "Invalid Rate Class ID format." });
                    else
                        isUpdate = true;
                }
                
                // Use system date/time at the moment of the operation (not BusinessDate)
                var systemNow = TextUtils.GetSystemDate();


                // Check for duplicate Code (case-insensitive) for insert/update
                if (!string.IsNullOrWhiteSpace(codeUG))
                {
                    var allUserGroups = PropertyUtils.ConvertToList<UserGroupModel>(UserGroupBO.Instance.FindAll()) ?? [];
                    bool duplicate = allUserGroups.Any(r => string.Equals(r.Code?.Trim(), codeUG.Trim(), StringComparison.OrdinalIgnoreCase) && r.ID != parsedId);
                    if (duplicate)
                        errors.Add(new { field = "code", message = "Code already exists." });

                }

                // Return errors if any
                if (errors.Count != 0)
                {
                    return Json(new { success = false, message = "Validation failed.", errors });
                }

                // Prepare model
                UserGroupModel _Model = new()
                {
                    Code = codeUG.Trim(),
                    Name = nameUG.Trim(),
                    Description = descriptionaccty ?? string.Empty,
                    Inactive = inactive == "1"
                };

                if (isUpdate)
                {
                    // Verify existing record
                    if (UserGroupBO.Instance.FindByPrimaryKey(parsedId) is not UserGroupModel existing || existing.ID == 0)
                    {
                        return NotFound(new { success = false, message = $"User Group not found (ID = {parsedId})" });
                    }

                    _Model.ID = parsedId;
                    _Model.UpdatedBy = user;
                    _Model.UpdatedDate = systemNow;
                    _Model.CreatedBy = existing.CreatedBy;
                    _Model.CreatedDate = existing.CreatedDate;
                    UserGroupBO.Instance.Update(_Model);
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
                    UserGroupBO.Instance.Insert(_Model);
                    return Json(new { success = true, message = "Record has been created successfully.", data = new { id = _Model.ID } });

                }

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
                // ArrayList arr = UserGroupBO.Instance.FindByAttribute("RateClassID", id);
                // if (arr.Count > 0)
                // {
                //     return Json(new { success = false, message = "Rate Class is being referenced to in other modules.\nDelete failed.!" });
                // }
                var checkID = UserGroupBO.Instance.FindByPrimaryKey(id);
                if (checkID == null)
                {
                    return Json(new { success = false, message = "User Group not found.!" });
                }

                UserGroupBO.Instance.Delete(id);

                return Json(new { success = true, message = $"Record was removed successfully ID: {id}." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        #endregion

        #region  AddFuncitionsToList
        public IActionResult AddFuncitionsToList()
        {
            List<ShortcutKeyModel> listsc = PropertyUtils.ConvertToList<ShortcutKeyModel>(ShortcutKeyBO.Instance.FindAll());
            ViewBag.ShortcutKeyList = listsc;

            List<FormAndFunctionGroupModel> listfromFuc = PropertyUtils.ConvertToList<FormAndFunctionGroupModel>(FormAndFunctionGroupBO.Instance.FindAll());
            ViewBag.FormAndFunctionGroupList = listfromFuc;
            return PartialView();
        }
        [HttpGet]
        public IActionResult AddFuncitionsToListData(string codetag, string namerights, int isDataRight)
        {
            try
            {


                DataTable dataTable = _iSecurityService.AddFuncitionsToListData(codetag, namerights, isDataRight);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  ID = d["ID"] != DBNull.Value ? d["ID"].ToString() : "",

                                  Code = d["Code"] != DBNull.Value ? d["Code"].ToString() : "",

                                  Name = d["Name"] != DBNull.Value ? d["Name"].ToString() : "",
                                  MappingLinkWeb = d["MappingLinkWeb"] != DBNull.Value ? d["MappingLinkWeb"].ToString() : "",
                                  ParentID = d["ParentID"] != DBNull.Value
                                    ? Convert.ToInt32(d["ParentID"])
                                    : 0,

                                  ShiftKey = d["ShiftKey"] != DBNull.Value
                                    ? Convert.ToInt32(d["ShiftKey"])
                                    : 0,

                                  CtrlKey = d["CtrlKey"] != DBNull.Value
                                    ? Convert.ToInt32(d["CtrlKey"])
                                    : 0,

                                  AltKey = d["AltKey"] != DBNull.Value
                                    ? Convert.ToInt32(d["AltKey"])
                                    : 0,

                                  ShortcutKey = d["ShortcutKey"] != DBNull.Value
                                    ? d["ShortcutKey"].ToString()
                                    : "",

                                  StrShortcutKey = d["strShortcutKey"] != DBNull.Value
                                    ? d["strShortcutKey"].ToString()
                                    : "",

                                  IsHide = d["IsHide"] != DBNull.Value
                                    ? Convert.ToInt32(d["IsHide"])
                                    : 0

                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
            //  report.DataSource = dataTable;

            // Không cần gán parameter
            // report.RequestParameters = false;

            // return PartialView("_ReportViewerPartial", report);
        }
        [HttpGet]
        public IActionResult AddFuncitionsToListDetail(int id, int isDataRight)
        {
            try
            {
                object result = null; // 👈 khai báo ngoài if/else

                if (isDataRight == 1)
                {
                    string sql = $"SELECT * FROM FormAndFunctionData WHERE ID = '{id}'";
                    DataTable dt = TextUtils.Select(sql);

                    result = (from d in dt.AsEnumerable()
                              select new
                              {
                                  ID = d["ID"] == DBNull.Value ? null : d["ID"].ToString(),
                                  Code = d["Code"] == DBNull.Value ? null : d["Code"].ToString(),
                                  Name = d["Name"] == DBNull.Value ? null : d["Name"].ToString(),
                                  Description = d["Description"] == DBNull.Value ? null : d["Description"].ToString(),

                                  FormAndFunctionDataGroupID =
                                      d["FormAndFunctionDataGroupID"] == DBNull.Value
                                      ? (int?)null
                                      : Convert.ToInt32(d["FormAndFunctionDataGroupID"]),

                                  CreatedBy = d["CreatedBy"] == DBNull.Value ? null : d["CreatedBy"].ToString(),
                                  CreatedDate = d["CreatedDate"] == DBNull.Value
                                      ? (DateTime?)null
                                      : Convert.ToDateTime(d["CreatedDate"]),

                                  UpdatedBy = d["UpdatedBy"] == DBNull.Value ? null : d["UpdatedBy"].ToString(),
                                  UpdatedDate = d["UpdatedDate"] == DBNull.Value
                                      ? (DateTime?)null
                                      : Convert.ToDateTime(d["UpdatedDate"]),

                                  OrderIndex = d["OrderIndex"] == DBNull.Value
                                      ? (int?)null
                                      : Convert.ToInt32(d["OrderIndex"]),

                                  IsHide = d["IsHide"] != DBNull.Value && Convert.ToBoolean(d["IsHide"])
                              }).ToList();
                }
                else
                {
                    string sql = $"SELECT * FROM FormAndFunction WHERE ID = '{id}'";
                    DataTable dt = TextUtils.Select(sql);

                    result = (from d in dt.AsEnumerable()
                              select new
                              {
                                  ID = d["ID"] == DBNull.Value ? null : d["ID"].ToString(),
                                  Code = d["Code"] == DBNull.Value ? null : d["Code"].ToString(),
                                  Name = d["Name"] == DBNull.Value ? null : d["Name"].ToString(),
                                  Description = d["Description"] == DBNull.Value ? null : d["Description"].ToString(),

                                  ShiftKey = d["ShiftKey"] != DBNull.Value && Convert.ToBoolean(d["ShiftKey"]),
                                  CtrlKey = d["CtrlKey"] != DBNull.Value && Convert.ToBoolean(d["CtrlKey"]),
                                  AltKey = d["AltKey"] != DBNull.Value && Convert.ToBoolean(d["AltKey"]),

                                  ShortcutKey = d["ShortcutKey"] == DBNull.Value ? null : d["ShortcutKey"].ToString(),

                                  FormAndFunctionGroupID =
                                      d["FormAndFunctionGroupID"] == DBNull.Value
                                      ? (int?)null
                                      : Convert.ToInt32(d["FormAndFunctionGroupID"]),

                                  CreatedBy = d["CreatedBy"] == DBNull.Value ? null : d["CreatedBy"].ToString(),
                                  CreatedDate = d["CreatedDate"] == DBNull.Value
                                      ? (DateTime?)null
                                      : Convert.ToDateTime(d["CreatedDate"]),

                                  UpdatedBy = d["UpdatedBy"] == DBNull.Value ? null : d["UpdatedBy"].ToString(),
                                  UpdatedDate = d["UpdatedDate"] == DBNull.Value
                                      ? (DateTime?)null
                                      : Convert.ToDateTime(d["UpdatedDate"]),

                                  IsHide = d["IsHide"] != DBNull.Value && Convert.ToBoolean(d["IsHide"]),

                                  OrderIndex = d["OrderIndex"] == DBNull.Value
                                      ? (int?)null
                                      : Convert.ToInt32(d["OrderIndex"]),

                                  Inactive = d["Inactive"] != DBNull.Value && Convert.ToBoolean(d["Inactive"]),

                                  MappingLinkWeb = d["MappingLinkWeb"] == DBNull.Value
                                      ? null
                                      : d["MappingLinkWeb"].ToString()
                              }).ToList();
                }

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
        [HttpPost]
        public IActionResult UpdateFormAndFunction(int ID, string codetagnew, string namerightnew, string descriptionnew, string mappingLinkWeb, int groupnew, int isHide, int isShift, int isCtrl, int isAlt, string functionkeynew, int _IsDataRight, string userName, int userID)
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();
                if (_IsDataRight == 1)
                {

                    #region Insert/update quyền dữ liệu
                    FormAndFunctionDataModel _model;
                    if (ID > 0)
                        _model = (FormAndFunctionDataModel)FormAndFunctionDataBO.Instance.FindByPrimaryKey(ID);
                    else
                        _model = new FormAndFunctionDataModel();
                    _model.Code = codetagnew;
                    _model.Name = namerightnew;
                    _model.Description = descriptionnew;
                    _model.FormAndFunctionDataGroupID = groupnew;
                    _model.UpdatedDate = TextUtils.GetSystemDate();
                    _model.UpdatedBy = userName;
                    _model.IsHide = isHide == 1;
                    if (ID > 0)
                    {
                        _model.ID = ID;
                        FormAndFunctionDataBO.Instance.Update(_model);
                    }
                    else
                    {
                        _model.CreatedBy = userName;
                        _model.CreatedDate = TextUtils.GetSystemDate();
                        FormAndFunctionDataBO.Instance.Insert(_model);
                    }
                    #endregion
                }
                else
                {
                    #region Insert/update quyền chức năng
                    FormAndFunctionModel _model;
                    if (ID > 0)
                        _model = (FormAndFunctionModel)FormAndFunctionBO.Instance.FindByPrimaryKey(ID);
                    else
                        _model = new FormAndFunctionModel();
                    _model.Code = codetagnew;
                    _model.Name = namerightnew;
                    _model.Description = descriptionnew;
                    _model.FormAndFunctionGroupID = groupnew;
                    _model.ShiftKey = isShift == 1;
                    _model.CtrlKey = isCtrl == 1;
                    _model.AltKey = isAlt == 1;
                    _model.ShortcutKey = functionkeynew;
                    _model.UpdatedDate = TextUtils.GetSystemDate();
                    _model.UpdatedBy = userName;
                    _model.MappingLinkWeb = mappingLinkWeb;
                    _model.IsHide = isHide == 1;
                    if (ID > 0)
                    {
                        _model.ID = ID;
                        FormAndFunctionBO.Instance.Update(_model);
                    }
                    else
                    {
                        _model.CreatedBy = userName;
                        _model.CreatedDate = TextUtils.GetSystemDate();
                        FormAndFunctionBO.Instance.Insert(_model);
                    }
                    #endregion
                }

                pt.CommitTransaction();

                return Json(new { success = true, message = "Successfully." });
            }
            catch (Exception ex)
            {
                pt.RollBack();
                return Json(new { success = false, message = ex.Message });
            }
            finally
            {
                pt.CloseConnection();

            }
        }
        #endregion

        #region UsersManagement 
        public IActionResult UsersManagement()
        {
            List<UserGroupModel> listug = PropertyUtils.ConvertToList<UserGroupModel>(UserGroupBO.Instance.FindAll());
            ViewBag.UserGroupList = listug;
            List<JobTitleModel> listjt = PropertyUtils.ConvertToList<JobTitleModel>(JobTitleBO.Instance.FindAll());
            ViewBag.JobTitleList = listjt;
            List<DepartmentModel> listdp = PropertyUtils.ConvertToList<DepartmentModel>(DepartmentBO.Instance.FindAll());
            ViewBag.DepartmentList = listdp;
            return PartialView();
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


                DataTable dataTable = _iSecurityService.UsersManagementData(lastName, firstName, loginName, userStatus, cashierStatus, jobtitle, department);
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

                                  BirthOfDate = d["BirthOfDate"] != DBNull.Value
                                ? Convert.ToDateTime(d["BirthOfDate"])
                                : (DateTime?)null,

                                  Telephone = d["Telephone"] != DBNull.Value ? d["Telephone"].ToString() : "",
                                  HandPhone = d["HandPhone"] != DBNull.Value ? d["HandPhone"].ToString() : "",
                                  HomeAddress = d["HomeAddress"] != DBNull.Value ? d["HomeAddress"].ToString() : "",
                                  Resident = d["Resident"] != DBNull.Value ? d["Resident"].ToString() : "",
                                  PostalCode = d["PostalCode"] != DBNull.Value ? d["PostalCode"].ToString() : "",

                                  IsCashier = d["IsCashier"] != DBNull.Value ? Convert.ToBoolean(d["IsCashier"]) : false,
                                  Department = d["Department"] != DBNull.Value ? d["Department"].ToString() : "",

                                  CreateDate = d["CreateDate"] != DBNull.Value
                               ? Convert.ToDateTime(d["CreateDate"])
                               : (DateTime?)null,
                                  CreateBy = d["CreateBy"] != DBNull.Value ? d["CreateBy"].ToString() : "",

                                  UpdateDate = d["UpdateDate"] != DBNull.Value
                               ? Convert.ToDateTime(d["UpdateDate"])
                               : (DateTime?)null,
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
                UsersModel modelRoom = (UsersModel)UsersBO.Instance.FindByPrimaryKey(id);

                if (modelRoom != null && !string.IsNullOrEmpty(modelRoom.PasswordHash))
                {
                    modelRoom.PasswordHash = DBUtils.Decrypt(
                        modelRoom.PasswordHash,
                        DBUtils.passPhrase,
                        DBUtils.saltValue,
                        DBUtils.hashAlgorithm,
                        DBUtils.passwordIterations,
                        DBUtils.initVector,
                        DBUtils.keySize
                    );
                }

                return Json(modelRoom);

            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
        [HttpPost]
        public IActionResult UpdateUsersManagement(UsersModel modelRoom)
        {
            try
            {
                modelRoom.PasswordHash = DBUtils.Encrypt(
                    modelRoom.PasswordHash,
                    DBUtils.passPhrase,
                    DBUtils.saltValue,
                    DBUtils.hashAlgorithm,
                    DBUtils.passwordIterations,
                    DBUtils.initVector,
                    DBUtils.keySize
                );
                var newLogin = (modelRoom.LoginName ?? string.Empty).Trim();
                modelRoom.LoginName = newLogin;

                if (string.IsNullOrWhiteSpace(newLogin))
                    return Json(new { success = false, message = "Login Name is required." });

                var allUsers = PropertyUtils.ConvertToList<UsersModel>(UsersBO.Instance.FindAll()) ?? [];
                bool dupLogin = allUsers.Any(u =>
                    u.ID != modelRoom.ID &&
                    !string.IsNullOrWhiteSpace(u.LoginName) &&
                    string.Equals(u.LoginName.Trim(), newLogin, StringComparison.OrdinalIgnoreCase));

                if (dupLogin)
                    return Json(new { success = false, message = "Login Name already exists." });

                if (modelRoom.ID == 0)
                {
                    // Insert
                    int pID = (int)UsersBO.Instance.Insert(modelRoom);
                    modelRoom.ID = pID;

                    if (modelRoom.IsCashier == true)
                        modelRoom.CashierNo = modelRoom.ID * 17;

                    UsersBO.Instance.Update(modelRoom);

                    return Json(new { success = true, message = "User inserted successfully." });
                }
                else
                {
                    // Update
                    UsersBO.Instance.Update(modelRoom);

                    return Json(new { success = true, message = "User updated successfully." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpPost]
        public IActionResult ResetPassUsersManagement(UsersModel modelRoom)
        {
            try
            {


                modelRoom.PasswordHash = "hYCF/unmJPi2vhV1I/WGmw==";
                modelRoom.UpdateDate = TextUtils.GetSystemDate();
                // Update
                UsersBO.Instance.Update(modelRoom);

                return Json(new { success = true, message = "User Reset Pass successfully." });

            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpPost]
        public IActionResult UsersManagementDelete(int id)
        {
            try
            {
                // ArrayList arr = UserGroupBO.Instance.FindByAttribute("RateClassID", id);
                // if (arr.Count > 0)
                // {
                //     return Json(new { success = false, message = "Rate Class is being referenced to in other modules.\nDelete failed.!" });
                // }
                var checkID = UsersBO.Instance.FindByPrimaryKey(id);
                if (checkID == null)
                {
                    return Json(new { success = false, message = "User  not found.!" });
                }

                UsersBO.Instance.Delete(id);

                return Json(new { success = true, message = $"Record was removed successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        #endregion
    }
}

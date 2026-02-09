using System;
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
        #region  AddFuncitionsToList
        public IActionResult AddFuncitionsToList()
        {
            List<ShortcutKeyModel> listsc = PropertyUtils.ConvertToList<ShortcutKeyModel>(ShortcutKeyBO.Instance.FindAll());
            ViewBag.ShortcutKeyList = listsc;

            List<FormAndFunctionGroupModel> listfromFuc = PropertyUtils.ConvertToList<FormAndFunctionGroupModel>(FormAndFunctionGroupBO.Instance.FindAll());
            ViewBag.FormAndFunctionGroupList = listfromFuc;
            return View();
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
        public IActionResult UpdateFormAndFunction(int ID, string codetagnew, string namerightnew, string descriptionnew,string mappingLinkWeb,int groupnew,int isHide,int isShift,int isCtrl,int isAlt,string functionkeynew,int _IsDataRight,string userName,int  userID)
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();
                if (_IsDataRight==1)
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
                    _model.ShiftKey = isShift==1;
                    _model.CtrlKey = isCtrl==1;
                    _model.AltKey = isAlt==1;
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

        #region
        public IActionResult UsersManagement()
        {
            List<UserGroupModel> listug = PropertyUtils.ConvertToList<UserGroupModel>(UserGroupBO.Instance.FindAll());
            ViewBag.UserGroupList = listug;

            return View();
        }
        #endregion
    }
}

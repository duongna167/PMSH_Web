using Administration.Helpers;
using Administration.Services.Interfaces;
using BaseBusiness.BO;
using BaseBusiness.Model;
using BaseBusiness.util;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Data;
using System.Text;
using static BaseBusiness.util.ValidationUtils;
namespace Administration.Controllers
{
    public class AdministrationController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AdministrationController> _logger;
        private readonly IMemoryCache _cache;
        private readonly IAdministrationService _iAdministrationService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public AdministrationController(ILogger<AdministrationController> logger,
                IMemoryCache cache, IConfiguration configuration, IAdministrationService iAdministrationService, IHttpContextAccessor httpContextAccessor)
        {
            _cache = cache;
            _logger = logger;
            _configuration = configuration;
            _iAdministrationService = iAdministrationService;
            _httpContextAccessor = httpContextAccessor;

        }
        /// <summary>Wall-clock time for Created/Updated audit fields on master data (not hotel business date).</summary>
        DateTime auditDateTime = DateTime.Now;

        #region MemberList
        [HttpGet]
        public IActionResult GetMemberList(string code, string name, int inactive)
        {
            try
            {
                DataTable dataTable = _iAdministrationService.MemberList(code, name, inactive);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  Code = !string.IsNullOrEmpty(d["Code"].ToString()) ? d["Code"] : "",
                                  Name = !string.IsNullOrEmpty(d["Name"].ToString()) ? d["Name"] : "",
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
        public IActionResult MemberList()
        {
            return View();
        }
        [HttpPost]
        public ActionResult InsertMember()
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();

                MemberTypeModel member = new MemberTypeModel();

                // Lấy dữ liệu từ form
                member.Code = Request.Form["txtcode"].ToString();
                member.Name = Request.Form["txtname"].ToString();
                member.Description = Request.Form["txtdescription"].ToString();
                member.Inactive = !string.IsNullOrEmpty(Request.Form["inactive"])
                                  && Request.Form["inactive"].ToString() == "on";

                // Thông tin người dùng
                member.CreatedBy = HttpContext.Session.GetString("LoginName") ?? "";
                member.UpdatedBy = member.CreatedBy;
                member.CreatedDate = DateTime.Now;
                member.UpdatedDate = DateTime.Now;

                // Gọi BO để lưu
                long memberId = MemberTypeBO.Instance.Insert(member);

                pt.CommitTransaction();

                return Json(new { success = true, id = memberId });
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
        [HttpPost]
        public ActionResult UpdateMember()
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();

                MemberTypeModel member = new MemberTypeModel();

                // Lấy ID từ form (có khi edit)
                member.ID = !string.IsNullOrEmpty(Request.Form["id"])
                             ? int.Parse(Request.Form["id"])
                             : 0;

                // Lấy dữ liệu từ form
                member.Code = Request.Form["txtcode"].ToString();
                member.Name = Request.Form["txtname"].ToString();
                member.Description = Request.Form["txtdescription"].ToString();
                member.Inactive = !string.IsNullOrEmpty(Request.Form["inactive"])
                                  && Request.Form["inactive"].ToString() == "on";

                string loginName = HttpContext.Session.GetString("LoginName") ?? "";

                if (member.ID == 0) // Insert mới
                {
                    member.CreatedBy = loginName;
                    member.CreatedDate = DateTime.Now;
                    member.UpdatedBy = loginName;
                    member.UpdatedDate = DateTime.Now;

                    MemberTypeBO.Instance.Insert(member);
                }
                else // Update
                {
                    // Trước khi update, lấy lại bản ghi cũ từ DB để giữ CreatedBy, CreatedDate
                    var oldData = MemberTypeBO.Instance.GetById(member.ID, pt.Connection, pt.Transaction);

                    if (oldData != null)
                    {
                        member.CreatedBy = oldData.CreatedBy;
                        member.CreatedDate = oldData.CreatedDate;
                    }

                    member.UpdatedBy = loginName;
                    member.UpdatedDate = DateTime.Now;

                    MemberTypeBO.Instance.Update(member);
                }

                pt.CommitTransaction();
                return Json(new { success = true });
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
        [HttpGet]
        public IActionResult GetMemberDescription(int id)
        {
            try
            {
                string desc = MemberTypeBO.Instance.GetDescriptionById(id);

                return Json(new
                {
                    success = true,
                    description = desc ?? ""
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpPost]
        public ActionResult DeleteMember()
        {
            try
            {

                MemberTypeModel memberModel = (MemberTypeModel)MemberTypeBO.Instance.FindByPrimaryKey(int.Parse(Request.Form["id"].ToString()));
                if (memberModel == null || memberModel.ID == 0)
                {
                    return Json(new { code = 1, msg = "Can not find Lost And Found" });

                }
                var delMemberTypeMsg = AdministrationDeleteGuards.GetDeleteMemberTypeBlockReason(memberModel.ID);
                if (delMemberTypeMsg != null)
                    return Json(new { code = 1, msg = delMemberTypeMsg });
                MemberTypeBO.Instance.Delete(int.Parse(Request.Form["id"].ToString()));
                return Json(new { code = 0, msg = "Delete Lost And Found was successfully" });

            }
            catch (Exception ex)
            {
                return Json(new { code = 1, msg = ex.Message });
            }

        }
        #endregion

        #region MemberCategory
        [HttpGet]
        public IActionResult GetMemberCategory(string code, string name, int inactive)
        {
            try
            {
                DataTable dataTable = _iAdministrationService.MemberCategory(code, name, inactive);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  Code = !string.IsNullOrEmpty(d["Code"].ToString()) ? d["Code"] : "",
                                  Name = !string.IsNullOrEmpty(d["Name"].ToString()) ? d["Name"] : "",
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
        public IActionResult MemberCategory()
        {
            return View();
        }
        [HttpPost]
        public ActionResult InsertMemberCategory()
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();

                MemberCategoryModel member = new MemberCategoryModel();

                // Lấy dữ liệu từ form
                member.Code = Request.Form["txtcode"].ToString();
                member.Name = Request.Form["txtname"].ToString();
                //member.Description = Request.Form["txtdescription"].ToString();
                member.Inactive = !string.IsNullOrEmpty(Request.Form["inactive"])
                                  && Request.Form["inactive"].ToString() == "on";

                // Thông tin người dùng
                member.CreatedBy = HttpContext.Session.GetString("LoginName") ?? "";
                member.UpdatedBy = member.CreatedBy;
                member.CreatedDate = DateTime.Now;
                member.UpdatedDate = DateTime.Now;

                // Gọi BO để lưu
                long memberId = MemberCategoryBO.Instance.Insert(member);

                pt.CommitTransaction();

                return Json(new { success = true, id = memberId });
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
        [HttpPost]
        public ActionResult UpdateMemberCategory()
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();

                MemberCategoryModel member = new MemberCategoryModel();

                // Lấy ID từ form (có khi edit)
                member.ID = !string.IsNullOrEmpty(Request.Form["id"])
                             ? int.Parse(Request.Form["id"])
                             : 0;

                // Lấy dữ liệu từ form
                member.Code = Request.Form["txtcode"].ToString();
                member.Name = Request.Form["txtname"].ToString();
                //member.Description = Request.Form["txtdescription"].ToString();
                member.Inactive = !string.IsNullOrEmpty(Request.Form["inactive"])
                                  && Request.Form["inactive"].ToString() == "on";

                string loginName = HttpContext.Session.GetString("LoginName") ?? "";

                if (member.ID == 0) // Insert mới
                {
                    member.CreatedBy = loginName;
                    member.CreatedDate = DateTime.Now;
                    member.UpdatedBy = loginName;
                    member.UpdatedDate = DateTime.Now;

                    MemberCategoryBO.Instance.Insert(member);
                }
                else // Update
                {
                    // Trước khi update, lấy lại bản ghi cũ từ DB để giữ CreatedBy, CreatedDate
                    var oldData = MemberCategoryBO.Instance.GetById(member.ID, pt.Connection, pt.Transaction);

                    if (oldData != null)
                    {
                        member.CreatedBy = oldData.CreatedBy;
                        member.CreatedDate = oldData.CreatedDate;
                    }

                    member.UpdatedBy = loginName;
                    member.UpdatedDate = DateTime.Now;

                    MemberCategoryBO.Instance.Update(member);
                }

                pt.CommitTransaction();
                return Json(new { success = true });
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
        [HttpPost]
        public ActionResult DeleteMemberCategory()
        {
            try
            {

                MemberCategoryModel memberModel = (MemberCategoryModel)MemberCategoryBO.Instance.FindByPrimaryKey(int.Parse(Request.Form["id"].ToString()));
                if (memberModel == null || memberModel.ID == 0)
                {
                    return Json(new { code = 1, msg = "Can not find Lost And Found" });

                }
                var delMemberCatMsg = AdministrationDeleteGuards.GetDeleteMemberCategoryBlockReason(memberModel.ID);
                if (delMemberCatMsg != null)
                    return Json(new { code = 1, msg = delMemberCatMsg });
                MemberCategoryBO.Instance.Delete(int.Parse(Request.Form["id"].ToString()));
                return Json(new { code = 0, msg = "Delete Lost And Found was successfully" });

            }
            catch (Exception ex)
            {
                return Json(new { code = 1, msg = ex.Message });
            }

        }
        #endregion

        #region Currency
        //[HttpGet]
        //public IActionResult GetCurrency()
        //{
        //    try
        //    {
        //        var result = _iAdministrationService.GetAllCurrency().ToList();
        //        return Json(result);
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(ex.Message);
        //    }
        //}

        public IActionResult Currency()
        {
            return PartialView();
        }

        [HttpGet]
        public IActionResult SearchCurrency(string code, int isActive)
        {
            try
            {
                SqlParameter[] param =
                [
                    new SqlParameter("@sqlCommand",
                    $@"  select   a.ID,
                            a.Description,
                            a.MasterStatus,
                            a.UserInsertID,
                            a.CreateDate,
                            a.UpdateDate,
                            a.UserUpdateID,
                            a.TransactionCode,
                            a.IsShow,
                            a.Inactive,
                            a.Decimals,
                            a.IsSynchronous,
                            (case MasterStatus when 0 then '' when 1 then 'X' end)as [IsMaster],
                    (case Inactive when 0 then '' when 1 then 'X' end)as [Inactive], (b.Code+' - '+b.Description)
                    as [Trans],a.Description from Currency a left join Transactions b on a.TransactionCode=b.Code
                    where 1=1 and a.ID like N'%{code}%' and a.Inactive = {isActive} order by a.ID desc")
                        ];
                DataTable dataTable = DataTableHelper.getTableData("spSearchAllForTrans", param);

                var result = (from d in dataTable.AsEnumerable()
                              select d.Table.Columns.Cast<DataColumn>()
                                  //.Where(col => col.ColumnName != "AllotmentStageID" && col.ColumnName != "flag" && col.ColumnName != "Total")
                                  .ToDictionary(
                                      col => col.ColumnName,
                                      col =>
                                      {
                                          var value = d[col.ColumnName];
                                          if (value == DBNull.Value) return null;

                                          // CreatedDate: KHÔNG ToString
                                          if (col.ColumnName == "CreatedDate" || col.ColumnName == "UpdatedDate" || col.ColumnName == "IsShow" || col.ColumnName == "Inactive")
                                              return value;

                                          // Các field khác: ToString
                                          return value.ToString();
                                      }
                                  )).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public ActionResult InsertCurrency()
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();

                CurrencyModel member = new CurrencyModel();

                member.IsShow = !string.IsNullOrEmpty(Request.Form["isShow"])
                                 && Request.Form["isShow"].ToString() == "on";
                member.Inactive = !string.IsNullOrEmpty(Request.Form["inactive"])
                                 && Request.Form["inactive"].ToString() == "on";
                member.MasterStatus = !string.IsNullOrEmpty(Request.Form["masterStatus"])
                                 && Request.Form["masterStatus"].ToString() == "on";
                // Lấy dữ liệu từ form
                member.ID = Request.Form["code"].ToString();
                member.Description = Request.Form["description"].ToString();
                int seqValue;
                if (int.TryParse(Request.Form["seq"], out seqValue))
                {
                    member.Decimals = seqValue;
                }
                else
                {
                    member.Decimals = 0; // hoặc giá trị mặc định
                }
                member.UserInsertID = HttpContext.Session.GetInt32("UserID") ?? 0;
                member.UserUpdateID = member.UserInsertID;
                member.CreateDate = DateTime.Now;
                member.UpdateDate = DateTime.Now;
                if (string.IsNullOrWhiteSpace(member.ID))
                    return Json(new { success = false, message = "The code cannot be left blank." });

                string memberId = CurrencyBO.Instance.InsertStringId(member);

                pt.CommitTransaction();

                return Json(new { success = true, id = memberId });
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
        [HttpPost]
        public ActionResult UpdateCurrency()
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();

                CurrencyModel member = new CurrencyModel();

                // Lấy ID từ form
                member.IsShow = !string.IsNullOrEmpty(Request.Form["isShow"])
                                 && Request.Form["isShow"].ToString() == "on";
                member.Inactive = !string.IsNullOrEmpty(Request.Form["inactive"])
                                 && Request.Form["inactive"].ToString() == "on";
                member.MasterStatus = !string.IsNullOrEmpty(Request.Form["masterStatus"])
                                 && Request.Form["masterStatus"].ToString() == "on";
                // Lấy dữ liệu từ form
                member.ID = Request.Form["code"].ToString();
                member.Description = Request.Form["description"].ToString();
                int seqValue;
                if (int.TryParse(Request.Form["seq"], out seqValue))
                {
                    member.Decimals = seqValue;
                }
                else
                {
                    member.Decimals = 0; // hoặc giá trị mặc định
                }

                int loginName = HttpContext.Session.GetInt32("UserID") ?? 0;
                if (string.IsNullOrWhiteSpace(member.ID))
                    return Json(new { success = false, message = "The code cannot be left blank." });

                if (member.ID == "") // Insert mới
                {
                    member.UserInsertID = loginName;
                    member.CreateDate = DateTime.Now;
                    member.UserUpdateID = loginName;
                    member.UpdateDate = DateTime.Now;

                    CurrencyBO.Instance.InsertStringId(member);
                }
                else // Update
                {
                    // Trước khi update, lấy lại bản ghi cũ từ DB để giữ CreatedBy, CreatedDate
                    var oldData = CurrencyBO.Instance.GetById(member.ID, pt.Connection, pt.Transaction);

                    if (oldData != null)
                    {

                        member.UserInsertID = oldData.UserInsertID;
                        member.CreateDate = oldData.CreateDate;
                    }

                    member.UserUpdateID = loginName;
                    member.UpdateDate = DateTime.Now;

                    CurrencyBO.Instance.UpdateStringId(member);
                }

                pt.CommitTransaction();
                return Json(new { success = true });
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


        [HttpPost]
        public ActionResult DeleteCurrency()
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();
                // Lấy ID string từ form
                string id = Request.Form["id"].ToString();

                if (string.IsNullOrEmpty(id))
                    return Json(new { code = 1, msg = "ID is null or empty" });

                // Lấy model theo ID
                CurrencyModel memberModel = CurrencyBO.Instance.GetById(id, pt.Connection, pt.Transaction);


                if (memberModel == null || string.IsNullOrEmpty(memberModel.ID))
                    return Json(new { code = 1, msg = "Cannot find Currency" });

                // Xóa model trực tiếp bằng string ID
                CurrencyBO.Instance.DeleteStringId(memberModel);

                return Json(new { code = 0, msg = "Deleted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { code = 1, msg = ex.Message });
            }
        }


        [HttpGet]
        public IActionResult GetCurrencyById(string id)
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();

                CurrencyModel member = CurrencyBO.Instance.GetById(id, pt.Connection, pt.Transaction);

                pt.CommitTransaction();

                if (member == null)
                    return Json(new { success = false, message = "Not found" });

                return Json(new
                {
                    success = true,
                    id = member.ID,
                    description = member.Description ?? "",
                    decimals = member.Decimals,
                    inactive = member.Inactive,
                    isMaster = member.MasterStatus,
                    isShow = member.IsShow
                });
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

        #region hkpEmployee
        [HttpGet]
        public IActionResult GethkpEmployee(string code, string name, int inactive)
        {
            try
            {
                DataTable dataTable = _iAdministrationService.hkpEmployee(code, name, inactive);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  Name = !string.IsNullOrEmpty(d["Name"].ToString()) ? d["Name"] : "",
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
        public IActionResult hkpEmployee()
        {
            return View();
        }
        [HttpPost]
        public ActionResult InserthkpEmployee()
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();

                hkpEmployeeModel member = new hkpEmployeeModel();



                member.Name = Request.Form["txtname"].ToString();
                member.Description = Request.Form["txtdescription"].ToString();
                member.Inactive = !string.IsNullOrEmpty(Request.Form["inactive"])
                                  && Request.Form["inactive"].ToString() == "on";
                member.CreatedBy = HttpContext.Session.GetString("LoginName") ?? "";
                member.UpdatedBy = member.CreatedBy;
                member.CreatedDate = DateTime.Now;
                member.UpdatedDate = DateTime.Now;

                // Gọi BO để lưu
                long memberId = hkpEmployeeBO.Instance.Insert(member);

                pt.CommitTransaction();

                return Json(new { success = true, id = memberId });
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
        [HttpPost]
        public ActionResult UpdatehkpEmployee()
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();

                hkpEmployeeModel member = new hkpEmployeeModel();

                // Lấy ID từ form (có khi edit)
                member.ID = !string.IsNullOrEmpty(Request.Form["id"])
                             ? int.Parse(Request.Form["id"])
                             : 0;

                // Lấy dữ liệu từ form
                member.Name = Request.Form["txtname"].ToString();
                member.Description = Request.Form["txtdescription"].ToString();
                member.Inactive = !string.IsNullOrEmpty(Request.Form["inactive"])
                                  && Request.Form["inactive"].ToString() == "on";
                string loginName = HttpContext.Session.GetString("LoginName") ?? "";

                if (member.ID == 0) // Insert mới
                {
                    member.CreatedBy = loginName;
                    member.CreatedDate = DateTime.Now;
                    member.UpdatedBy = loginName;
                    member.UpdatedDate = DateTime.Now;

                    hkpEmployeeBO.Instance.Insert(member);
                }
                else // Update
                {
                    // Trước khi update, lấy lại bản ghi cũ từ DB để giữ CreatedBy, CreatedDate
                    var oldData = hkpEmployeeBO.Instance.GetById(member.ID, pt.Connection, pt.Transaction);

                    if (oldData != null)
                    {
                        member.CreatedBy = oldData.CreatedBy;
                        member.CreatedDate = oldData.CreatedDate;
                    }

                    member.UpdatedBy = loginName;
                    member.UpdatedDate = DateTime.Now;

                    hkpEmployeeBO.Instance.Update(member);
                }

                pt.CommitTransaction();
                return Json(new { success = true });
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
        [HttpPost]
        public ActionResult DeletehkpEmployee()
        {
            try
            {
                hkpEmployeeModel memberModel = (hkpEmployeeModel)hkpEmployeeBO.Instance.FindByPrimaryKey(int.Parse(Request.Form["id"].ToString()));
                if (memberModel == null || memberModel.ID == 0)
                {
                    return Json(new { code = 1, msg = "Can not find Country" });

                }
                hkpEmployeeBO.Instance.Delete(int.Parse(Request.Form["id"].ToString()));
                return Json(new { code = 0, msg = "Delete Country was successfully" });

            }
            catch (Exception ex)
            {
                return Json(new { code = 1, msg = ex.Message });
            }

        }
        [HttpGet]
        public IActionResult GethkpEmployeeById(int id)
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();

                hkpEmployeeModel member = hkpEmployeeBO.Instance.GetById(id, pt.Connection, pt.Transaction);

                pt.CommitTransaction();

                if (member == null)
                    return Json(new { success = false, message = "Not found" });

                return Json(new
                {
                    success = true,
                    id = member.ID,
                    description = member.Description ?? "",
                    name = member.Name ?? "",
                    inactive = member.Inactive

                });
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

        #region ConfigStatusColor
        [HttpGet]
        public IActionResult GetStatusList()
        {
            try
            {
                DataTable dataTable = _iAdministrationService.StatusList();
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  ColorName = !string.IsNullOrEmpty(d["ColorName"].ToString()) ? d["ColorName"] : "",
                                  FontColorName = !string.IsNullOrEmpty(d["FontColorName"].ToString()) ? d["FontColorName"] : "",
                                  StatusName = !string.IsNullOrEmpty(d["Status Name"].ToString()) ? d["Status Name"] : "",
                                  Description = !string.IsNullOrEmpty(d["Description"].ToString()) ? d["Description"] : "",
                                  ID = !string.IsNullOrEmpty(d["ID"].ToString()) ? d["ID"] : "",
                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        public IActionResult ConfigStatusColor()
        {
            return PartialView();
        }
        [HttpPost]
        public ActionResult UpdateConfigStatusColor()
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();

                HKPStatusColorModel member = new HKPStatusColorModel();

                // Lấy ID từ form
                member.ID = !string.IsNullOrEmpty(Request.Form["id"])
                             ? int.Parse(Request.Form["id"])
                             : 0;
                member.ColorName = Request.Form["bgColor"].ToString();
                member.FontColorName = Request.Form["fontColor"].ToString();
                member.StatusName = Request.Form["name"].ToString();

                int loginName = HttpContext.Session.GetInt32("UserID") ?? 0;

                if (member.ID == 0) // Insert mới
                {

                    member.UserInsertID = loginName;
                    member.CreateDate = auditDateTime;
                    member.UserUpdateID = loginName;
                    member.UpdateDate = auditDateTime;

                    HKPStatusColorBO.Instance.Insert(member);
                }
                else // Update
                {
                    // Trước khi update, lấy lại bản ghi cũ từ DB để giữ CreatedBy, CreatedDate
                    var oldData = HKPStatusColorBO.Instance.GetById(member.ID, pt.Connection, pt.Transaction);

                    if (oldData != null)
                    {
                        member.Description = oldData.Description;
                        member.UserInsertID = oldData.UserInsertID;
                        member.CreateDate = oldData.CreateDate;
                    }

                    member.UserUpdateID = loginName;
                    member.UpdateDate = auditDateTime;

                    HKPStatusColorBO.Instance.Update(member);
                }

                pt.CommitTransaction();
                return Json(new { success = true });
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

        #region Message
        public ActionResult CreateMessage()
        {
            var model = new ConfigSystemModel
            {
                Desciption = ConfigSystemBO.GetConfigDesciption()
            };
            return View(model);
        }
        [HttpPost]
        public ActionResult UpdateMessage(string desc)
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();

                int loginUser = HttpContext.Session.GetInt32("UserID") ?? 0;

                // Update Msg
                ConfigSystemBO.Instance.UpdateMsg(desc, loginUser, pt.Connection, pt.Transaction);

                pt.CommitTransaction();
                return Json(new { success = true });
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

        #region MemberTypeSearch
        [HttpGet]
        public IActionResult GetMemberTypeSearch(DateTime fromDate, DateTime toDate, string status, string memberID, int isSortByCardName)
        {
            try
            {
                DataTable dataTable = _iAdministrationService.Member(fromDate, toDate, status, memberID, isSortByCardName);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  Name = !string.IsNullOrEmpty(d["Name"].ToString()) ? d["Name"] : "",
                                  Arrival = !string.IsNullOrEmpty(d["Arrival"].ToString()) ? d["Arrival"] : "",
                                  Depart = !string.IsNullOrEmpty(d["Depart"].ToString()) ? d["Depart"] : "",
                                  Nights = !string.IsNullOrEmpty(d["Nights"].ToString()) ? d["Nights"] : "",
                                  RoNo = !string.IsNullOrEmpty(d["RoNo"].ToString()) ? d["RoNo"] : "",
                                  Status = !string.IsNullOrEmpty(d["Status"].ToString()) ? d["Status"] : "",
                                  Member = !string.IsNullOrEmpty(d["Member"].ToString()) ? d["Member"] : "",
                                  CardNumber = !string.IsNullOrEmpty(d["CardNumber"].ToString()) ? d["CardNumber"] : "",
                                  CardHolder = !string.IsNullOrEmpty(d["CardHolder"].ToString()) ? d["CardHolder"] : "",
                                  MarketCode = !string.IsNullOrEmpty(d["MarketCode"].ToString()) ? d["MarketCode"] : "",
                                  RateCode = !string.IsNullOrEmpty(d["RateCode"].ToString()) ? d["RateCode"] : "",
                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        public ActionResult MemberTypeSearch()
        {
            List<MemberTypeModel> listctry = PropertyUtils.ConvertToList<MemberTypeModel>(MemberTypeBO.Instance.FindAll());
            ViewBag.MemberTypeList = listctry;
            List<MemberCategoryModel> listmbc = PropertyUtils.ConvertToList<MemberCategoryModel>(MemberCategoryBO.Instance.FindAll());
            ViewBag.MemberCategoryList = listmbc;
            return View();
        }
        #endregion

        #region PostingHistory
        [HttpGet]
        public IActionResult GetPostingHistory(DateTime fromDate, DateTime toDate, string fromFolioID, string toFolioID, string actionType, string user)
        {
            try
            {
                DataTable dataTable = _iAdministrationService.PostingHistory(fromDate, toDate, fromFolioID, toFolioID, actionType, user);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  Property = !string.IsNullOrEmpty(d["Property"].ToString()) ? d["Property"] : "",
                                  AcctionText = !string.IsNullOrEmpty(d["AcctionText"].ToString()) ? d["AcctionText"] : "",
                                  TransactionFate = !string.IsNullOrEmpty(d["TransactionFate"].ToString()) ? d["TransactionFate"] : "",
                                  AcctionDate = !string.IsNullOrEmpty(d["AcctionDate"].ToString()) ? d["AcctionDate"] : "",
                                  ActionUser = !string.IsNullOrEmpty(d["RoNo"].ToString()) ? d["RoNo"] : "",
                                  AmountIncTax = !string.IsNullOrEmpty(d["Status"].ToString()) ? d["Status"] : "",
                                  MoreInfornamtion = !string.IsNullOrEmpty(d["Member"].ToString()) ? d["Member"] : "",
                                  Machine = !string.IsNullOrEmpty(d["CardNumber"].ToString()) ? d["CardNumber"] : "",
                                  InvoiceNo = !string.IsNullOrEmpty(d["CardHolder"].ToString()) ? d["CardHolder"] : "",
                                  ActionType = !string.IsNullOrEmpty(d["MarketCode"].ToString()) ? d["MarketCode"] : "",
                                  FromFolioID = !string.IsNullOrEmpty(d["RateCode"].ToString()) ? d["RateCode"] : "",
                                  FromName = !string.IsNullOrEmpty(d["RateCode"].ToString()) ? d["RateCode"] : "",
                                  FromRoom = !string.IsNullOrEmpty(d["RateCode"].ToString()) ? d["RateCode"] : "",
                                  ToFolioID = !string.IsNullOrEmpty(d["RateCode"].ToString()) ? d["RateCode"] : "",
                                  Name = !string.IsNullOrEmpty(d["RateCode"].ToString()) ? d["RateCode"] : "",
                                  ToName = !string.IsNullOrEmpty(d["RateCode"].ToString()) ? d["RateCode"] : "",
                                  ToRoom = !string.IsNullOrEmpty(d["RateCode"].ToString()) ? d["RateCode"] : "",
                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        public ActionResult PostingHistory()
        {
            List<UsersModel> listuser = PropertyUtils.ConvertToList<UsersModel>(UsersBO.Instance.FindAll());
            ViewBag.UsersList = listuser;
            return View();
        }
        #endregion

        #region PersonInCharge
        public ActionResult PersonInCharge()
        {

            List<PersonInChargeGroupModel> listpic = PropertyUtils.ConvertToList<PersonInChargeGroupModel>(PersonInChargeGroupBO.Instance.FindAll());

            ViewBag.PersonInChargeGroupList = listpic;

            List<PersonInChargeZoneModel> listzone = PropertyUtils.ConvertToList<PersonInChargeZoneModel>(PersonInChargeZoneBO.Instance.FindAll());

            ViewBag.PersonInChargeZoneList = listzone;
            return PartialView();
        }
        [HttpGet]
        public IActionResult GetPersonInCharge(string code, string name, string group, string zone, string isActive)
        {
            code = code?.Trim() ?? "";
            name = name?.Trim() ?? "";
            //group = SanitizeCsv(group);
            //zone = SanitizeCsv(zone);

            var groupIds = !string.IsNullOrEmpty(group)
             ? group.Split(',').Where(s => !string.IsNullOrWhiteSpace(s)).ToList()
             : new List<string> { "" };

            var zoneIds = !string.IsNullOrEmpty(zone)
                ? zone.Split(',').Where(s => !string.IsNullOrWhiteSpace(s)).ToList()
                : new List<string> { "" };

            DataTable combinedTable = null;

            try
            {
                foreach (var gId in groupIds)
                {
                    foreach (var zId in zoneIds)
                    {
                        DataTable dt = _iAdministrationService.PersonInChargeData(code, name, gId, zId, isActive);

                        if (combinedTable == null)
                        {
                            combinedTable = dt.Clone();
                        }

                        if (dt != null && dt.Rows.Count > 0)
                        {
                            foreach (DataRow row in dt.Rows)
                            {
                                combinedTable.ImportRow(row);
                            }
                        }
                    }
                }

                if (combinedTable == null || combinedTable.Rows.Count == 0)
                {
                    return Json(new List<object>());
                }
                var result = combinedTable.AsEnumerable()
                    .GroupBy(r => r["ID"].ToString())
                    .Select(g => g.First())
                    .Select(d => new
                    {

                        Code = !string.IsNullOrEmpty(d["Code"].ToString()) ? d["Code"] : "",
                        Name = !string.IsNullOrEmpty(d["Name"].ToString()) ? d["Name"] : "",
                        Telephone = !string.IsNullOrEmpty(d["Telephone"].ToString()) ? d["Telephone"] : "",
                        Mobile = !string.IsNullOrEmpty(d["Mobile"].ToString()) ? d["Mobile"] : "",
                        Email = !string.IsNullOrEmpty(d["Email"].ToString()) ? d["Email"] : "",
                        Description = !string.IsNullOrEmpty(d["Description"].ToString()) ? d["Description"] : "",
                        ZoneID = !string.IsNullOrEmpty(d["ZoneID"].ToString()) ? d["ZoneID"] : "",
                        GroupID = !string.IsNullOrEmpty(d["GroupID"].ToString()) ? d["GroupID"] : "",
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
                return BadRequest(new
                {
                    message = ex.Message
                });
            }

        }
        [HttpPost]
        public IActionResult PersonInChargeSave([FromBody] PersonInChargeModel model)
        {
            string message = "";

            var listErrors = GetErrors(
                Check(model == null, "general", "Invalid data"),

                Check(model?.Name, "txtname", "Name cannot be blank."),
                Check(model?.PersonInChargeZoneID <= 0, "personInChargeZoneId", "Zone cannot be blank.")
            );

            if (listErrors.Count > 0)
            {
                return Json(new { success = false, errors = listErrors });
            }
            try
            {
                if (model.ID == 0)
                {
                    string sql = @"
                        SELECT 
                            RIGHT('000000' + CAST(ISNULL(MAX(CAST(Code AS INT)), 0) + 1 AS VARCHAR(6)), 6)
                        FROM PersonInCharge WITH (UPDLOCK, HOLDLOCK)
                    ";
                    DataTable dt = TextUtils.Select(sql);
                    if (dt == null || dt.Rows.Count == 0)
                        throw new Exception("Cannot generate Person In Charge code.");

                    model.Code = dt.Rows[0][0].ToString();
                    model.CreatedDate = DateTime.Now;
                    model.UpdatedDate = DateTime.Now;

                    PersonInChargeBO.Instance.Insert(model);
                    message = "Insert successfully!";
                }
                else
                {
                    var oldData = (PersonInChargeModel)PersonInChargeBO.Instance.FindByPrimaryKey(model.ID);

                    if (oldData != null)
                    {
                        model.Code = oldData.Code;
                        model.CreatedBy = oldData.CreatedBy;
                        model.CreatedDate = oldData.CreatedDate;
                    }

                    model.UpdatedDate = DateTime.Now;

                    PersonInChargeBO.Instance.Update(model);
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
        public IActionResult DeletePersonInCharge(int id)
        {
            try
            {
                if (PersonInChargeBO.Instance.FindByPrimaryKey(id) is not PersonInChargeModel existing || existing.ID == 0)
                {
                    return Ok(new { success = false, message = $"Person In Charge not found." });
                }

                var reservation = PropertyUtils.ConvertToList<ReservationModel>(
                    ReservationBO.Instance.FindByAttribute("PersonInChargeID", id)
                );

                if (reservation != null && reservation.Count > 0)

                {
                    return Json(new
                    {
                        success = false,
                        message = "Cannot delete this Person In Charge because it is used in Reservation."
                    });
                }
                PersonInChargeBO.Instance.Delete(id);
                return Json(new { success = true, message = $"Record was removed successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, ex.Message });
            }

            return Json(new { success = true });
        }
        #endregion 

        #region PersonInChargeGroup
        public ActionResult PersonInChargeGroup()
        {
            return PartialView();
        }
        [HttpGet]
        public IActionResult GetPersonInChargeGroup(string code, string name, int inactive)
        {
            try
            {
                DataTable dataTable = _iAdministrationService.PersonInChargeGroupData(code, name, inactive);
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
        [HttpPost]
        public IActionResult PersonInChargeGroupSave([FromBody] PersonInChargeGroupModel model)
        {
            string message = "";
            var listErrors = GetErrors(
                Check(model, "general", "Invalid data"),

                Check(model?.Code, "picg_code", "Code is not blank."),
                //Check(PersonInChargeGroupBO.Instance.IsDuplicate("Code", model.Code, model.ID),
                //    "code", "This code already exists."),
                Check(model?.Name, "picg_name", "Name is not blank.")
            );

            if (listErrors.Count > 0)
            {
                return Json(new { success = false, errors = listErrors });
            }
            try
            {
                if (model.ID == 0)
                {
                    model.CreateDate = auditDateTime;
                    model.CreatedDate = auditDateTime;
                    model.UpdateDate = auditDateTime;
                    model.UpdatedDate = auditDateTime;

                    PersonInChargeGroupBO.Instance.Insert(model);
                    message = "Insert successfully!";
                }
                else
                {
                    var oldData = (PersonInChargeGroupModel)PersonInChargeGroupBO.Instance.FindByPrimaryKey(model.ID);

                    if (oldData != null)
                    {
                        model.UserInsertID = oldData.UserInsertID;
                        model.CreatedBy = oldData.CreatedBy;
                        model.CreateDate = oldData.CreatedDate;
                        model.CreatedDate = oldData.CreatedDate;
                    }

                    model.UpdateDate = auditDateTime;
                    model.UpdatedDate = auditDateTime;

                    PersonInChargeGroupBO.Instance.Update(model);
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
        public IActionResult DeletePersonInChargeGroup(int id)
        {
            try
            {
                var msgPicg = AdministrationDeleteGuards.GetDeletePersonInChargeGroupBlockReason(id);
                if (msgPicg != null)
                    return Json(new { success = false, message = msgPicg });
                PersonInChargeGroupBO.Instance.Delete(id);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, ex.Message });
            }

            return Json(new { success = true });
        }
        #endregion

        #region PersonInChargeZone
        public ActionResult PersonInChargeZone()
        {
            return PartialView();
        }
        [HttpGet]
        public IActionResult GetPersonInChargeZone(string code, string name, string inactive)
        {
            try
            {
                DataTable dataTable = _iAdministrationService.PersonInChargeZoneData(code, name, inactive);
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
        [HttpPost]
        public IActionResult PersonInChargeZoneSave([FromBody] PersonInChargeZoneModel model)
        {
            string message = "";
            var listErrors = GetErrors(
                Check(model, "general", "Invalid data"),

                Check(model?.Code, "picz_code", "Code is not blank."),
                //Check(PersonInChargeZoneBO.Instance.IsDuplicate("Code", model.Code, model.ID),
                //       "code", "This code already exists."),
                Check(model?.Name, "picz_name", "Name is not blank.")
            );

            if (listErrors.Count > 0)
            {
                return Json(new { success = false, errors = listErrors });
            }
            try
            {
                if (model.ID == 0)
                {
                    model.CreateDate = DateTime.Now;
                    model.CreatedDate = DateTime.Now;
                    model.UpdateDate = DateTime.Now;
                    model.UpdatedDate = DateTime.Now;

                    PersonInChargeZoneBO.Instance.Insert(model);
                    message = "Insert successfully!";
                }
                else
                {
                    var oldData = (PersonInChargeZoneModel)PersonInChargeZoneBO.Instance.FindByPrimaryKey(model.ID);

                    if (oldData != null)
                    {
                        model.UserInsertID = oldData.UserInsertID;
                        model.CreatedBy = oldData.CreatedBy;
                        model.CreateDate = oldData.CreatedDate;
                        model.CreatedDate = oldData.CreatedDate;
                    }

                    model.UpdateDate = DateTime.Now;
                    model.UpdatedDate = DateTime.Now;

                    PersonInChargeZoneBO.Instance.Update(model);
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
        public IActionResult DeletePersonInChargeZone(int id)
        {
            try
            {
                var msgPicz = AdministrationDeleteGuards.GetDeletePersonInChargeZoneBlockReason(id);
                if (msgPicz != null)
                    return Json(new { success = false, message = msgPicz });
                PersonInChargeZoneBO.Instance.Delete(id);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, ex.Message });
            }

            return Json(new { success = true });
        }
        #endregion

        #region ApprovedBy
        public ActionResult ApproveBy()
        {
            return PartialView();
        }
        [HttpGet]
        public IActionResult GetApprovedBy(string code, string name, string isActive)
        {
            try
            {
                DataTable dataTable = _iAdministrationService.ApproveListData(code, name, isActive);
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
        [HttpPost]
        public IActionResult ApprovedBySave([FromBody] ApprovedbyModel model)
        {
            string message = "";
            var listErrors = GetErrors(
                Check(model, "general", "Invalid data"),

                Check(model?.Code, "appB_code", "Code is not blank."),
                //Check(ApprovedbyBO.Instance.IsDuplicate("Code", model.Code, model.ID),
                //        "code", "This code already exists."),
                Check(model?.Name, "appB_name", "Name is not blank.")
            );

            if (listErrors.Count > 0)
            {
                return Json(new { success = false, errors = listErrors });
            }
            try
            {
                if (model.ID == 0)
                {
                    model.CreateDate = DateTime.Now;
                    model.CreatedDate = DateTime.Now;
                    model.UpdateDate = DateTime.Now;
                    model.UpdatedDate = DateTime.Now;

                    ApprovedbyBO.Instance.Insert(model);
                    message = "Insert successfully!";
                }
                else
                {
                    var oldData = (ApprovedbyModel)ApprovedbyBO.Instance.FindByPrimaryKey(model.ID);

                    if (oldData != null)
                    {
                        model.UserInsertID = oldData.UserInsertID;
                        model.CreatedBy = oldData.CreatedBy;
                        model.CreateDate = oldData.CreatedDate;
                        model.CreatedDate = oldData.CreatedDate;
                    }

                    model.UpdateDate = DateTime.Now;
                    model.UpdatedDate = DateTime.Now;

                    ApprovedbyBO.Instance.Update(model);
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
        public IActionResult DeleteApprovedby(int id)
        {
            try
            {
                ApprovedbyBO.Instance.Delete(id);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, ex.Message });
            }

            return Json(new { success = true });
        }
        #endregion

        #region Deposit/Cancellation Rules Search 
        public IActionResult DepositRule()
        {
            List<UsersModel> listUser = PropertyUtils.ConvertToList<UsersModel>(UsersBO.Instance.FindAll());
            ViewBag.UsersList = listUser;

            List<CurrencyModel> listCurr = PropertyUtils.ConvertToList<CurrencyModel>(CurrencyBO.Instance.FindAll());
            ViewBag.CurrencyList = listCurr;
            return PartialView();
        }
        [HttpGet]
        public IActionResult GetDepositRule(string code, string description)
        {
            try
            {
                DataTable dt = _iAdministrationService.DepositRule(code, description);
                var result = (from r in dt.AsEnumerable()
                              select new
                              {
                                  Code = !string.IsNullOrEmpty(r["Code"].ToString()) ? r["Code"] : "",
                                  Description = !string.IsNullOrEmpty(r["Description"].ToString()) ? r["Description"] : "",
                                  Type = !string.IsNullOrEmpty(r["Type"].ToString()) ? r["Type"] : "",
                                  AmountValue = !string.IsNullOrEmpty(r["AmountValue"].ToString()) ? r["AmountValue"] : "",
                                  CurrencyID = !string.IsNullOrEmpty(r["CurrencyID"].ToString()) ? r["CurrencyID"] : "",
                                  DaysBeforeArrival = !string.IsNullOrEmpty(r["DaysBeforeArrival"].ToString()) ? r["DaysBeforeArrival"] : "",
                                  DaysAfterBooking = !string.IsNullOrEmpty(r["DaysAfterBooking"].ToString()) ? r["DaysAfterBooking"] : "",
                                  UserInsertID = r["UserInsertID"] != DBNull.Value ? Convert.ToInt32(r["UserInsertID"]) : 0,
                                  CreateDate = !string.IsNullOrEmpty(r["CreateDate"].ToString()) ? r["CreateDate"] : "",
                                  UserUpdateID = !string.IsNullOrEmpty(r["UserUpdateID"].ToString()) ? r["UserUpdateID"] : "",
                                  UpdateDate = !string.IsNullOrEmpty(r["UpdateDate"].ToString()) ? r["UpdateDate"] : "",
                                  ID = !string.IsNullOrEmpty(r["ID"].ToString()) ? r["ID"] : "",
                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        [HttpGet]
        public IActionResult GetDepositRuleById(int id)
        {
            var data = (DepositRuleModel)DepositRuleBO.Instance.FindByPrimaryKey(id);

            if (data == null)
            {
                return Json(new { inactive = false, sequence = 0 });
            }

            return Json(new
            {
                inactive = data.Inactive,
                sequence = data.Sequence
            });
        }
        [HttpPost]
        public IActionResult DepositRuleSave([FromBody] DepositRuleModel model)
        {
            var listErrors = GetErrors(
                Check(model, "general", "Invalid data"),
                Check(model?.AmountValue < 0, "amountValue", "Deposit Amount cannot be negative."),
                Check(model?.Type == 0 && string.IsNullOrWhiteSpace(model?.CurrencyID), "currencyID", "Currency is required for Flat type."),
                Check((model?.DaysBeforeArrival ?? 0) < 0, "dayBA", "Days Before Arrival cannot be negative."),
                Check((model?.DaysAfterBooking ?? 0) < 0, "dayAB", "Days After Booking cannot be negative."),
                Check((model?.Sequence ?? 0) < 0, "seq", "Sequence cannot be negative."),
                Check(model?.Code, "code", "Code is not blank."),
                Check(model?.Description, "des", "Description is not blank.")
            );

            if (listErrors.Count > 0)
            {
                return Json(new { success = false, errors = listErrors });
            }
            string message = "";

            try
            {
                if (model.ID == 0)
                {
                    model.CreateDate = DateTime.Now;
                    model.UpdateDate = DateTime.Now;
                    DepositRuleBO.Instance.Insert(model);
                    message = $"Successfully Insert Deposit Rule: {model.Code}";
                }
                else
                {
                    var old = (DepositRuleModel)DepositRuleBO.Instance.FindByPrimaryKey(model.ID);
                    if (old != null)
                    {
                        model.Code = old.Code;
                        model.CreateDate = old.CreateDate;
                        model.UserInsertID = old.UserInsertID;
                    }

                    model.UpdateDate = DateTime.Now;
                    DepositRuleBO.Instance.Update(model);
                    message = $"Successfully Updated Deposit Rule: {model.Code}.";
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
        public IActionResult DepositRuleDelete(int id)
        {
            try
            {
                var msgDr = AdministrationDeleteGuards.GetDeleteDepositRuleBlockReason(id);
                if (msgDr != null)
                    return Json(new { success = false, message = msgDr });
                DepositRuleBO.Instance.Delete(id);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, ex.Message });
            }

            return Json(new { success = true });
        }

        public IActionResult CancellationRule()
        {
            List<UsersModel> listUser = PropertyUtils.ConvertToList<UsersModel>(UsersBO.Instance.FindAll());
            ViewBag.UsersList = listUser;

            List<CurrencyModel> listCurr = PropertyUtils.ConvertToList<CurrencyModel>(CurrencyBO.Instance.FindAll());
            ViewBag.CurrencyList = listCurr;
            return PartialView();
        }
        [HttpGet]
        public IActionResult GetCancellationRule(string code, string description)
        {
            try
            {
                DataTable dt = _iAdministrationService.CancellationRule(code, description);
                var result = (from r in dt.AsEnumerable()
                              select new
                              {
                                  Code = !string.IsNullOrEmpty(r["Code"].ToString()) ? r["Code"] : "",
                                  Description = !string.IsNullOrEmpty(r["Description"].ToString()) ? r["Description"] : "",
                                  Type = !string.IsNullOrEmpty(r["Type"].ToString()) ? r["Type"] : "",
                                  AmountValue = !string.IsNullOrEmpty(r["AmountValue"].ToString()) ? r["AmountValue"] : "",
                                  CurrencyID = !string.IsNullOrEmpty(r["CurrencyID"].ToString()) ? r["CurrencyID"] : "",
                                  DaysBeforeArrival = !string.IsNullOrEmpty(r["DaysBeforeArrival"].ToString()) ? r["DaysBeforeArrival"] : "",
                                  CancelBeforeTime = !string.IsNullOrEmpty(r["CancelBeforeTime"].ToString()) ? r["CancelBeforeTime"] : "",
                                  UserInsertID = r["UserInsertID"] != DBNull.Value ? Convert.ToInt32(r["UserInsertID"]) : 0,
                                  CreateDate = !string.IsNullOrEmpty(r["CreateDate"].ToString()) ? r["CreateDate"] : "",
                                  UserUpdateID = !string.IsNullOrEmpty(r["UserUpdateID"].ToString()) ? r["UserUpdateID"] : "",
                                  UpdateDate = !string.IsNullOrEmpty(r["UpdateDate"].ToString()) ? r["UpdateDate"] : "",
                                  ID = !string.IsNullOrEmpty(r["ID"].ToString()) ? r["ID"] : "",
                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        [HttpGet]
        public IActionResult GetCancellationRuleById(int id)
        {
            var data = (CancellationRuleModel)CancellationRuleBO.Instance.FindByPrimaryKey(id);

            if (data == null)
            {
                return Json(new { inactive = false, sequence = 0 });
            }

            return Json(new
            {
                cancelBeforeTime = data.CancelBeforeTime,
                inactive = data.Inactive,
                sequence = data.Sequence
            });
        }
        [HttpPost]
        public IActionResult CancellationRuleSave([FromBody] CancellationRuleModel model)
        {
            var listErrors = GetErrors(
                Check(model, "general", "Invalid data"),

                Check(model?.Code, "code", "Code is not blank."),
                Check(model?.Description, "des", "Description is not blank."),
                Check((model?.AmountValue ?? 0) < 0, "amountValue", "Cancellation Amount cannot be negative."),
                // type =1 Percent
                Check(model?.Type == 1 && (model?.AmountValue ?? 0) > 100, "amountValue", "Percent cannot exceed 100%."),
                Check(model?.Type == 0 && string.IsNullOrWhiteSpace(model?.CurrencyID), "currencys", "Currency is required for Flat type."),

                Check((model?.DaysBeforeArrival ?? 0) < 0, "dayBA", "Days Before Arrival cannot be negative."),

                Check((model?.Sequence ?? 0) < 0, "seq", "Sequence cannot be negative.")
            );

            if (listErrors.Count > 0)
            {
                return Json(new { success = false, errors = listErrors });
            }

            string message = "";

            try
            {
                if (model.ID == 0)
                {
                    model.CreateDate = DateTime.Now;
                    model.UpdateDate = DateTime.Now;
                    CancellationRuleBO.Instance.Insert(model);
                    message = $"Successfully Insert Deposit Rule: {model.Code}";
                }
                else
                {
                    var old = (CancellationRuleModel)CancellationRuleBO.Instance.FindByPrimaryKey(model.ID);
                    if (old != null)
                    {
                        model.Code = old.Code;
                        model.CreateDate = old.CreateDate;
                        model.UserInsertID = old.UserInsertID;
                    }

                    model.UpdateDate = DateTime.Now;
                    CancellationRuleBO.Instance.Update(model);
                    message = $"Successfully Updated Deposit Rule: {model.Code}.";
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
        public IActionResult CancellationRuleDelete(int id)
        {
            try
            {
                var msgCr = AdministrationDeleteGuards.GetDeleteCancellationRuleBlockReason(id);
                if (msgCr != null)
                    return Json(new { success = false, message = msgCr });
                CancellationRuleBO.Instance.Delete(id);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, ex.Message });
            }

            return Json(new { success = true });
        }
        #endregion

        #region ItemCategory/City
        [HttpGet]
        public IActionResult GetCity(string code, string name, int inactive)
        {
            try
            {
                DataTable dataTable = _iAdministrationService.City(code, name, inactive);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  Code = !string.IsNullOrEmpty(d["Code"].ToString()) ? d["Code"] : "",
                                  Name = !string.IsNullOrEmpty(d["Name"].ToString()) ? d["Name"] : "",
                                  Description = !string.IsNullOrEmpty(d["Description"].ToString()) ? d["Description"] : "",
                                  Country = !string.IsNullOrEmpty(d["Country"].ToString()) ? d["Country"] : "",
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
        public IActionResult City()
        {
            List<CountryModel> listctry = PropertyUtils.ConvertToList<CountryModel>(CountryBO.Instance.FindAll());
            ViewBag.CountryList = listctry;
            return PartialView("ItemCategory/City");
        }
        [HttpPost]
        public IActionResult CitySave([FromBody] CityModel model)
        {
            string message = "";
            var listErrors = GetErrors(
                Check(model, "general", "Invalid data"),

                Check(model?.Code, "city_code", "Code is not blank."),
                Check(model?.Name, "city_name", "Name is not blank."),
                Check(model?.CountryID, "city_countryID", "Please select a country. ")
            );

            if (listErrors.Count > 0)
            {
                return Json(new { success = false, errors = listErrors });
            }
            try
            {

                if (model.ID == 0)
                {
                    model.CreateDate = auditDateTime;
                    model.CreatedDate = auditDateTime;
                    model.UpdateDate = auditDateTime;
                    model.UpdatedDate = auditDateTime;

                    CityBO.Instance.Insert(model);
                    message = "Insert successfully!";
                }
                else
                {
                    var oldData = (CityModel)CityBO.Instance.FindByPrimaryKey(model.ID);

                    if (oldData != null)
                    {
                        model.UserInsertID = oldData.UserInsertID;
                        model.CreatedBy = oldData.CreatedBy;
                        model.CreateDate = oldData.CreatedDate;
                        model.CreatedDate = oldData.CreatedDate;
                    }

                    model.UpdateDate = auditDateTime;
                    model.UpdatedDate = auditDateTime;

                    CityBO.Instance.Update(model);
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
        public IActionResult DeleteCity(int id)
        {
            try
            {
                var msgCity = AdministrationDeleteGuards.GetDeleteCityBlockReason(id);
                if (msgCity != null)
                    return Json(new { success = false, message = msgCity });
                CityBO.Instance.Delete(id);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, ex.Message });
            }

            return Json(new { success = true });
        }
        #endregion

        #region ItemCategory/Country
        [HttpGet]
        public IActionResult GetCountry(string code, string name, int inactive)
        {
            try
            {
                DataTable dataTable = _iAdministrationService.Country(code, name, inactive);
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
        public IActionResult Country()
        {
            return PartialView("ItemCategory/Country");
        }
        [HttpPost]
        public IActionResult CountrySave([FromBody] CountryModel model)
        {
            string message = "";
            var listErrors = GetErrors(
                Check(model, "general", "Invalid data"),

                Check(model?.Code, "couT_code", "Code is not blank."),
                Check(model?.Name, "couT_name", "Name is not blank.")
            );

            if (listErrors.Count > 0)
            {
                return Json(new { success = false, errors = listErrors });
            }
            try
            {
                if (model.ID == 0)
                {
                    model.CreateDate = auditDateTime;
                    model.CreatedDate = auditDateTime;
                    model.UpdateDate = auditDateTime;
                    model.UpdatedDate = auditDateTime;

                    CountryBO.Instance.Insert(model);
                    message = "Insert successfully!";
                }
                else
                {
                    var oldData = (CountryModel)CountryBO.Instance.FindByPrimaryKey(model.ID);

                    if (oldData != null)
                    {
                        model.UserInsertID = oldData.UserInsertID;
                        model.CreatedBy = oldData.CreatedBy;
                        model.CreateDate = oldData.CreatedDate;
                        model.CreatedDate = oldData.CreatedDate;
                    }

                    model.UpdateDate = auditDateTime;
                    model.UpdatedDate = auditDateTime;

                    CountryBO.Instance.Update(model);
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
        public IActionResult DeleteCountry(int id)
        {
            try
            {
                var msgCtry = AdministrationDeleteGuards.GetDeleteCountryBlockReason(id);
                if (msgCtry != null)
                    return Json(new { success = false, message = msgCtry });
                CountryBO.Instance.Delete(id);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, ex.Message });
            }

            return Json(new { success = true });
        }
        #endregion

        #region ItemCategory/Language
        [HttpGet]
        public IActionResult GetLanguage(string code, string name, int inactive)
        {
            try
            {
                DataTable dataTable = _iAdministrationService.Language(code, name, inactive);
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
        public IActionResult Language()
        {
            return PartialView("ItemCategory/Language");
        }
        [HttpPost]
        public IActionResult LanguageSave([FromBody] LanguageModel model)
        {
            string message = "";
            var listErrors = GetErrors(
                Check(model, "general", "Invalid data"),

                Check(model?.Code, "lanG_code", "Code is not blank."),
                Check(model?.Name, "lanG_name", "Name is not blank.")
            );

            if (listErrors.Count > 0)
            {
                return Json(new { success = false, errors = listErrors });
            }
            try
            {
                if (model.ID == 0)
                {
                    model.CreateDate = auditDateTime;
                    model.CreatedDate = auditDateTime;
                    model.UpdateDate = auditDateTime;
                    model.UpdatedDate = auditDateTime;

                    LanguageBO.Instance.Insert(model);
                    message = "Insert successfully!";
                }
                else
                {
                    var oldData = (LanguageModel)LanguageBO.Instance.FindByPrimaryKey(model.ID);

                    if (oldData != null)
                    {
                        model.UserInsertID = oldData.UserInsertID;
                        model.CreatedBy = oldData.CreatedBy;
                        model.CreateDate = oldData.CreatedDate;
                        model.CreatedDate = oldData.CreatedDate;
                    }

                    model.UpdateDate = auditDateTime;
                    model.UpdatedDate = auditDateTime;

                    LanguageBO.Instance.Update(model);
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
        public IActionResult DeleteLanguage(int id)
        {
            try
            {
                var msgLang = AdministrationDeleteGuards.GetDeleteLanguageBlockReason(id);
                if (msgLang != null)
                    return Json(new { success = false, message = msgLang });
                LanguageBO.Instance.Delete(id);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, ex.Message });
            }

            return Json(new { success = true });
        }
        #endregion

        #region ItemCategory/Nationality
        [HttpGet]
        public IActionResult GetNationality(string code, string name, int inactive)
        {
            try
            {


                DataTable dataTable = _iAdministrationService.Nationality(code, name, inactive);
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
        public IActionResult Nationality()
        {
            return PartialView("ItemCategory/Nationality");
        }
        [HttpPost]
        public IActionResult NationalitySave([FromBody] NationalityModel model)
        {
            string message = "";
            var listErrors = GetErrors(
                Check(model, "general", "Invalid data"),

                Check(model?.Code, "ntl_code", "Code is not blank."),
                Check(model?.Name, "ntl_name", "Name is not blank.")
            );

            if (listErrors.Count > 0)
            {
                return Json(new { success = false, errors = listErrors });
            }
            try
            {
                if (model.ID == 0)
                {
                    model.CreateDate = auditDateTime;
                    model.CreatedDate = auditDateTime;
                    model.UpdateDate = auditDateTime;
                    model.UpdatedDate = auditDateTime;

                    NationalityBO.Instance.Insert(model);
                    message = "Insert successfully!";
                }
                else
                {
                    var oldData = (NationalityModel)NationalityBO.Instance.FindByPrimaryKey(model.ID);

                    if (oldData != null)
                    {
                        model.UserInsertID = oldData.UserInsertID;
                        model.CreatedBy = oldData.CreatedBy;
                        model.CreateDate = oldData.CreatedDate;
                        model.CreatedDate = oldData.CreatedDate;
                    }

                    model.UpdateDate = auditDateTime;
                    model.UpdatedDate = auditDateTime;

                    NationalityBO.Instance.Update(model);
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
        public IActionResult DeleteNationality(int id)
        {
            try
            {
                var msgNat = AdministrationDeleteGuards.GetDeleteNationalityBlockReason(id);
                if (msgNat != null)
                    return Json(new { success = false, message = msgNat });
                NationalityBO.Instance.Delete(id);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, ex.Message });
            }

            return Json(new { success = true });
        }
        #endregion

        #region ItemCategory/Title
        [HttpGet]
        public IActionResult GetTitle(string code, string name, int inactive)
        {
            try
            {
                DataTable dataTable = _iAdministrationService.Title(code, name, inactive);
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
        public IActionResult Title()
        {
            return PartialView("ItemCategory/Title");
        }
        [HttpPost]
        public IActionResult TitleSave([FromBody] TitleModel model)
        {
            string message = "";
            var listErrors = GetErrors(
                Check(model, "general", "Invalid data"),

                Check(model?.Code, "titL_code", "Code is not blank."),
                Check(model?.Name, "titL_name", "Name is not blank.")
            );

            if (listErrors.Count > 0)
            {
                return Json(new { success = false, errors = listErrors });
            }
            try
            {
                if (model.ID == 0)
                {
                    model.CreateDate = auditDateTime;
                    model.CreatedDate = auditDateTime;
                    model.UpdateDate = auditDateTime;
                    model.UpdatedDate = auditDateTime;

                    TitleBO.Instance.Insert(model);
                    message = "Insert successfully!";
                }
                else
                {
                    var oldData = (TitleModel)TitleBO.Instance.FindByPrimaryKey(model.ID);

                    if (oldData != null)
                    {
                        model.UserInsertID = oldData.UserInsertID;
                        model.CreatedBy = oldData.CreatedBy;
                        model.CreateDate = oldData.CreatedDate;
                        model.CreatedDate = oldData.CreatedDate;
                    }

                    model.UpdateDate = auditDateTime;
                    model.UpdatedDate = auditDateTime;

                    TitleBO.Instance.Update(model);
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
        public IActionResult DeleteTitle(int id)
        {
            try
            {
                var msgTitle = AdministrationDeleteGuards.GetDeleteTitleBlockReason(id);
                if (msgTitle != null)
                    return Json(new { success = false, message = msgTitle });
                TitleBO.Instance.Delete(id);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, ex.Message });
            }

            return Json(new { success = true });
        }
        #endregion

        #region ItemCategory/Territory
        [HttpGet]
        public IActionResult GetTerritory(string code, string name, int inactive)
        {
            try
            {
                DataTable dataTable = _iAdministrationService.Territory(code, name, inactive);
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
        public IActionResult Territory()
        {
            return PartialView("ItemCategory/Territory");
        }
        [HttpPost]
        public IActionResult TerritorySave([FromBody] TerritoryModel model)
        {
            string message = "";
            var listErrors = GetErrors(
                Check(model, "general", "Invalid data"),

                Check(model?.Code, "terr_code", "Code is not blank."),
                Check(model?.Name, "terr_name", "Name is not blank.")
            );

            if (listErrors.Count > 0)
            {
                return Json(new { success = false, errors = listErrors });
            }
            try
            {
                if (model.ID == 0)
                {
                    model.CreateDate = auditDateTime;
                    model.CreatedDate = auditDateTime;
                    model.UpdateDate = auditDateTime;
                    model.UpdatedDate = auditDateTime;

                    TerritoryBO.Instance.Insert(model);
                    message = "Insert successfully!";
                }
                else
                {
                    var oldData = (TerritoryModel)TerritoryBO.Instance.FindByPrimaryKey(model.ID);

                    if (oldData != null)
                    {
                        model.UserInsertID = oldData.UserInsertID;
                        model.CreatedBy = oldData.CreatedBy;
                        model.CreateDate = oldData.CreatedDate;
                        model.CreatedDate = oldData.CreatedDate;
                    }

                    model.UpdateDate = auditDateTime;
                    model.UpdatedDate = auditDateTime;

                    TerritoryBO.Instance.Update(model);
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
        public IActionResult DeleteTerritory(int id)
        {
            try
            {
                var msgTerr = AdministrationDeleteGuards.GetDeleteTerritoryBlockReason(id);
                if (msgTerr != null)
                    return Json(new { success = false, message = msgTerr });
                TerritoryBO.Instance.Delete(id);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, ex.Message });
            }

            return Json(new { success = true });
        }
        #endregion

        #region ItemCategory/State
        [HttpGet]
        public IActionResult GetState(string code, string name, int inactive)
        {
            try
            {
                DataTable dataTable = _iAdministrationService.State(code, name, inactive);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  Code = !string.IsNullOrEmpty(d["ZipCode"].ToString()) ? d["ZipCode"] : "",
                                  Name = !string.IsNullOrEmpty(d["StateName"].ToString()) ? d["StateName"] : "",
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
        public IActionResult State()
        {
            //List<CountryModel> listctry = PropertyUtils.ConvertToList<CountryModel>(CountryBO.Instance.FindAll());
            //ViewBag.CountryList = listctry;
            return PartialView("ItemCategory/State");
        }
        [HttpPost]
        public IActionResult StateSave([FromBody] StateModel model)
        {
            string message = "";
            var listErrors = GetErrors(
                Check(model, "general", "Invalid data"),

                Check(model?.ZipCode, "stt_zipCode", "Code is not blank."),
                Check(model?.StateName, "stt_stateName", "Name is not blank.")
            );

            if (listErrors.Count > 0)
            {
                return Json(new { success = false, errors = listErrors });
            }
            try
            {

                if (model.ID == 0)
                {
                    model.CreateDate = auditDateTime;
                    model.CreatedDate = auditDateTime;
                    model.UpdateDate = auditDateTime;
                    model.UpdatedDate = auditDateTime;

                    StateBO.Instance.Insert(model);
                    message = "Insert successfully!";
                }
                else
                {
                    var oldData = (StateModel)StateBO.Instance.FindByPrimaryKey(model.ID);

                    if (oldData != null)
                    {
                        model.UserInsertID = oldData.UserInsertID;
                        model.CreatedBy = oldData.CreatedBy;
                        model.CreateDate = oldData.CreatedDate;
                        model.CreatedDate = oldData.CreatedDate;
                    }

                    model.UpdateDate = auditDateTime;
                    model.UpdatedDate = auditDateTime;

                    StateBO.Instance.Update(model);
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
        public IActionResult DeleteState(int id)
        {
            try
            {
                var msgSt = AdministrationDeleteGuards.GetDeleteStateBlockReason(id);
                if (msgSt != null)
                    return Json(new { success = false, message = msgSt });
                StateBO.Instance.Delete(id);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, ex.Message });
            }

            return Json(new { success = true });
        }
        #endregion

        #region ItemCategory/VIP
        [HttpGet]
        public IActionResult GetVIP(string code, string name, int inactive)
        {
            try
            {
                DataTable dataTable = _iAdministrationService.VIP(code, name, inactive);
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
        public IActionResult VIP()
        {
            return PartialView("ItemCategory/VIP");
        }
        [HttpPost]
        public IActionResult VIPSave([FromBody] VIPModel model)
        {
            string message = "";
            var listErrors = GetErrors(
                Check(model, "general", "Invalid data"),

                Check(model?.Code, "vip_code", "Code is not blank."),
                Check(model?.Name, "vip_name", "Name is not blank.")
            );

            if (listErrors.Count > 0)
            {
                return Json(new { success = false, errors = listErrors });
            }
            try
            {
                if (model.ID == 0)
                {
                    model.CreateDate = auditDateTime;
                    model.CreatedDate = auditDateTime;
                    model.UpdateDate = auditDateTime;
                    model.UpdatedDate = auditDateTime;

                    VIPBO.Instance.Insert(model);
                    message = "Insert successfully!";
                }
                else
                {
                    var oldData = (VIPModel)VIPBO.Instance.FindByPrimaryKey(model.ID);

                    if (oldData != null)
                    {
                        model.UserInsertID = oldData.UserInsertID;
                        model.CreatedBy = oldData.CreatedBy;
                        model.CreateDate = oldData.CreatedDate;
                        model.CreatedDate = oldData.CreatedDate;
                    }

                    model.UpdateDate = auditDateTime;
                    model.UpdatedDate = auditDateTime;

                    VIPBO.Instance.Update(model);
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
        public IActionResult DeleteVIP(int id)
        {
            try
            {
                var msgVip = AdministrationDeleteGuards.GetDeleteVipBlockReason(id);
                if (msgVip != null)
                    return Json(new { success = false, message = msgVip });
                VIPBO.Instance.Delete(id);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, ex.Message });
            }

            return Json(new { success = true });
        }
        #endregion

        #region ItemCategory/Market
        [HttpGet]
        public IActionResult GetMarket(string code, string name, int inactive)
        {
            try
            {
                DataTable dataTable = _iAdministrationService.Market(code, name, inactive);
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
        public IActionResult GetMarketById(int id)
        {
            var data = (MarketModel)MarketBO.Instance.FindByPrimaryKey(id);

            if (data == null)
            {
                return Json(new Dictionary<string, object>
                {
                    ["regional"] = "",
                    ["groupType"] = 0,
                    ["marketTypeID"] = 0
                });
            }

            // Dictionary keys stay stable for all JSON serializers (camelCase / PascalCase issues in some clients).
            return Json(new Dictionary<string, object>
            {
                ["regional"] = data.Regional ?? "",
                ["groupType"] = data.GroupType,
                ["marketTypeID"] = data.MarketTypeID
            });
        }
        public IActionResult Market()
        {
            List<MarketTypeModel> listmktype = PropertyUtils.ConvertToList<MarketTypeModel>(MarketTypeBO.Instance.FindAll());
            ViewBag.MarketTypeList = listmktype;
            return PartialView("ItemCategory/Market");
        }
        [HttpPost]
        public IActionResult MarketSave([FromBody] MarketModel model)
        {
            string message = "";
            var listErrors = GetErrors(
                Check(model, "general", "Invalid data"),

                Check(model?.Code, "marK_code", "Code is not blank."),
                Check(model?.Name, "marK_name", "Name is not blank."),
                Check(model?.MarketTypeID, "marK_marketTypeID", "Please choose market type.")
            );

            if (listErrors.Count > 0)
            {
                return Json(new { success = false, errors = listErrors });
            }
            try
            {
                if (model.ID == 0)
                {
                    model.CreateDate = auditDateTime;
                    model.CreatedDate = auditDateTime;
                    model.UpdateDate = auditDateTime;
                    model.UpdatedDate = auditDateTime;

                    MarketBO.Instance.Insert(model);
                    message = "Insert successfully!";
                }
                else
                {
                    var oldData = (MarketModel)MarketBO.Instance.FindByPrimaryKey(model.ID);

                    if (oldData != null)
                    {
                        model.UserInsertID = oldData.UserInsertID;
                        model.CreatedBy = oldData.CreatedBy;
                        model.CreateDate = oldData.CreatedDate;
                        model.CreatedDate = oldData.CreatedDate;
                    }

                    model.UpdateDate = auditDateTime;
                    model.UpdatedDate = auditDateTime;

                    MarketBO.Instance.Update(model);
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
        public IActionResult DeleteMarket(int id)
        {
            try
            {
                var msgMkt = AdministrationDeleteGuards.GetDeleteMarketBlockReason(id);
                if (msgMkt != null)
                    return Json(new { success = false, message = msgMkt });
                MarketBO.Instance.Delete(id);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, ex.Message });
            }

            return Json(new { success = true });
        }
        #endregion

        #region ItemCategory/MarketType
        [HttpGet]
        public IActionResult GetMarketType(string code, string name, int inactive)
        {
            try
            {
                DataTable dataTable = _iAdministrationService.MarketType(code, name, inactive);
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
        public IActionResult MarketType()
        {
            return PartialView("ItemCategory/MarketType");
        }
        [HttpPost]
        public IActionResult MarketTypeSave([FromBody] MarketTypeModel model)
        {
            string message = "";
            var listErrors = GetErrors(
                Check(model, "general", "Invalid data"),

                Check(model?.Code, "marketType_code", "Code is not blank."),
                Check(model?.Name, "marketType_name", "Name is not blank.")
            );

            if (listErrors.Count > 0)
            {
                return Json(new { success = false, errors = listErrors });
            }
            try
            {
                if (model.ID == 0)
                {
                    model.CreateDate = auditDateTime;
                    model.CreatedDate = auditDateTime;
                    model.UpdateDate = auditDateTime;
                    model.UpdatedDate = auditDateTime;

                    MarketTypeBO.Instance.Insert(model);
                    message = "Insert successfully!";
                }
                else
                {
                    var oldData = (MarketTypeModel)MarketTypeBO.Instance.FindByPrimaryKey(model.ID);

                    if (oldData != null)
                    {
                        model.UserInsertID = oldData.UserInsertID;
                        model.CreatedBy = oldData.CreatedBy;
                        model.CreateDate = oldData.CreatedDate;
                        model.CreatedDate = oldData.CreatedDate;
                    }

                    model.UpdateDate = auditDateTime;
                    model.UpdatedDate = auditDateTime;

                    MarketTypeBO.Instance.Update(model);
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
        public IActionResult DeleteMarketType(int id)
        {
            try
            {
                var msgMt = AdministrationDeleteGuards.GetDeleteMarketTypeBlockReason(id);
                if (msgMt != null)
                    return Json(new { success = false, message = msgMt });
                MarketTypeBO.Instance.Delete(id);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, ex.Message });
            }

            return Json(new { success = true });
        }
        #endregion

        #region ItemCategory/PickupDropPlace
        [HttpGet]
        public IActionResult GetPickupDropPlace(string code, string name, int inactive)
        {
            try
            {
                DataTable dataTable = _iAdministrationService.PickupDropPlace(code, name, inactive);
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

        public IActionResult PickupDropPlace()
        {
            return PartialView("ItemCategory/PickupDropPlace");
        }
        [HttpPost]
        public IActionResult PickupDropPlaceSave([FromBody] PickupDropPlaceModel model)
        {
            string message = "";
            var listErrors = GetErrors(
                Check(model, "general", "Invalid data"),

                Check(model?.Code, "pickupDropPlace_code", "Code is not blank."),
                Check(model?.Name, "pickupDropPlace_name", "Name is not blank.")
            );

            if (listErrors.Count > 0)
            {
                return Json(new { success = false, errors = listErrors });
            }
            try
            {
                if (model.ID == 0)
                {
                    model.CreateDate = auditDateTime;
                    model.CreatedDate = auditDateTime;
                    model.UpdateDate = auditDateTime;
                    model.UpdatedDate = auditDateTime;

                    PickupDropPlaceBO.Instance.Insert(model);
                    message = "Insert successfully!";
                }
                else
                {
                    var oldData = (PickupDropPlaceModel)PickupDropPlaceBO.Instance.FindByPrimaryKey(model.ID);

                    if (oldData != null)
                    {
                        model.UserInsertID = oldData.UserInsertID;
                        model.CreatedBy = oldData.CreatedBy;
                        model.CreateDate = oldData.CreatedDate;
                        model.CreatedDate = oldData.CreatedDate;
                    }

                    model.UpdateDate = auditDateTime;
                    model.UpdatedDate = auditDateTime;

                    PickupDropPlaceBO.Instance.Update(model);
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
        public IActionResult DeletePickupDropPlace(int id)
        {
            try
            {
                var msgPdp = AdministrationDeleteGuards.GetDeletePickupDropPlaceBlockReason(id);
                if (msgPdp != null)
                    return Json(new { success = false, message = msgPdp });
                PickupDropPlaceBO.Instance.Delete(id);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, ex.Message });
            }

            return Json(new { success = true });
        }
        #endregion

        #region ItemCategory/TransportType
        [HttpGet]
        public IActionResult GetTransportType(string code, string name, int inactive)
        {
            try
            {
                DataTable dataTable = _iAdministrationService.TransportType(code, name, inactive);
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
        public IActionResult TransportType()
        {
            return PartialView("ItemCategory/TransportType");
        }
        [HttpPost]
        public IActionResult TransportTypeSave([FromBody] TransportTypeModel model)
        {
            string message = "";
            var listErrors = GetErrors(
                Check(model, "general", "Invalid data"),

                Check(model?.Code, "transT_code", "Code is not blank."),
                Check(model?.Name, "transT_name", "Name is not blank.")
            );

            if (listErrors.Count > 0)
            {
                return Json(new { success = false, errors = listErrors });
            }
            try
            {
                if (model.ID == 0)
                {
                    model.CreateDate = auditDateTime;
                    model.CreatedDate = auditDateTime;
                    model.UpdateDate = auditDateTime;
                    model.UpdatedDate = auditDateTime;

                    TransportTypeBO.Instance.Insert(model);
                    message = "Insert successfully!";
                }
                else
                {
                    var oldData = (TransportTypeModel)TransportTypeBO.Instance.FindByPrimaryKey(model.ID);

                    if (oldData != null)
                    {
                        model.UserInsertID = oldData.UserInsertID;
                        model.CreatedBy = oldData.CreatedBy;
                        model.CreateDate = oldData.CreatedDate;
                        model.CreatedDate = oldData.CreatedDate;
                    }

                    model.UpdateDate = auditDateTime;
                    model.UpdatedDate = auditDateTime;

                    TransportTypeBO.Instance.Update(model);
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
        public IActionResult DeleteTransportType(int id)
        {
            try
            {
                var msgTt = AdministrationDeleteGuards.GetDeleteTransportTypeBlockReason(id);
                if (msgTt != null)
                    return Json(new { success = false, message = msgTt });
                TransportTypeBO.Instance.Delete(id);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, ex.Message });
            }

            return Json(new { success = true });
        }
        #endregion

        #region ItemCategory/ReservationType
        [HttpGet]
        public IActionResult GetReservationType()
        {
            try
            {
                DataTable dataTable = _iAdministrationService.ReservationType();
                var colNames = string.Join(", ", dataTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName));
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  Code = !string.IsNullOrEmpty(d["Code"].ToString()) ? d["Code"] : "",
                                  Name = !string.IsNullOrEmpty(d["Name"].ToString()) ? d["Name"] : "",
                                  Sequence = !string.IsNullOrEmpty(d["Sequence"].ToString()) ? d["Sequence"] : "",
                                  ArrivalTimeRequired = !string.IsNullOrEmpty(d["ArrivalTimeRequired"].ToString()) ? d["ArrivalTimeRequired"] : "",
                                  CreditCardRequired = !string.IsNullOrEmpty(d["CreditCardRequired"].ToString()) ? d["CreditCardRequired"] : "",
                                  Deduct = !string.IsNullOrEmpty(d["Deduct"].ToString()) ? d["Deduct"] : "",
                                  DepositRequired = !string.IsNullOrEmpty(d["DepositRequired"].ToString()) ? d["DepositRequired"] : "",
                                  Inactive = !string.IsNullOrEmpty(d["Inactive"].ToString()) ? d["Inactive"] : "",
                                  ID = !string.IsNullOrEmpty(d["ID"].ToString()) ? d["ID"] : "",
                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        public IActionResult ReservationType()
        {
            return PartialView("ItemCategory/ReservationType");
        }
        [HttpPost]
        public IActionResult ReservationTypeSave([FromBody] ReservationTypeModel model)
        {
            string message = "";

            var listErrors = GetErrors(
                Check(model, "general", "Invalid data"),

                Check(model?.Code, "resT_code", "Code is not blank."),
                Check(model?.Name, "resT_name", "Description is not blank."),
                Check(model?.Sequence < 0, "resT_seq", "Sequence cannot be negative.")
            );

            if (listErrors.Count > 0)
            {
                return Json(new { success = false, errors = listErrors });
            }
            try
            {
                if (model.ID == 0)
                {
                    model.CreateDate = auditDateTime;
                    model.UpdateDate = auditDateTime;

                    ReservationTypeBO.Instance.Insert(model);
                    message = "Insert successfully!";
                }
                else
                {
                    var oldData = (ReservationTypeModel)ReservationTypeBO.Instance.FindByPrimaryKey(model.ID);

                    if (oldData != null)
                    {
                        model.UserInsertID = oldData.UserInsertID;
                        model.CreateDate = oldData.CreateDate;
                    }

                    model.UpdateDate = auditDateTime;

                    ReservationTypeBO.Instance.Update(model);
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
        public IActionResult DeleteReservationType(int id)
        {
            try
            {
                var msgRt = AdministrationDeleteGuards.GetDeleteReservationTypeBlockReason(id);
                if (msgRt != null)
                    return Json(new { success = false, message = msgRt });
                ReservationTypeBO.Instance.Delete(id);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, ex.Message });
            }

            return Json(new { success = true });
        }
        #endregion

        #region ItemCategory/Reason
        [HttpGet]
        public IActionResult GetReason(string code, string name, int inactive)
        {
            try
            {
                DataTable dataTable = _iAdministrationService.Reason(code, name, inactive);
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
        public IActionResult Reason()
        {
            return PartialView("ItemCategory/Reason");
        }
        [HttpPost]
        public IActionResult ReasonSave([FromBody] ReasonModel model)
        {
            string message = "";
            var listErrors = GetErrors(
                Check(model, "general", "Invalid data"),

                Check(model?.Code, "reaS_code", "Code is not blank."),
                Check(model?.Name, "reaS_name", "Name is not blank.")
            );

            if (listErrors.Count > 0)
            {
                return Json(new { success = false, errors = listErrors });
            }
            try
            {
                if (model.ID == 0)
                {
                    model.CreateDate = auditDateTime;
                    model.CreatedDate = auditDateTime;
                    model.UpdateDate = auditDateTime;
                    model.UpdatedDate = auditDateTime;

                    ReasonBO.Instance.Insert(model);
                    message = "Insert successfully!";
                }
                else
                {
                    var oldData = (ReasonModel)ReasonBO.Instance.FindByPrimaryKey(model.ID);

                    if (oldData != null)
                    {
                        model.UserInsertID = oldData.UserInsertID;
                        model.CreatedBy = oldData.CreatedBy;
                        model.CreateDate = oldData.CreatedDate;
                        model.CreatedDate = oldData.CreatedDate;
                    }

                    model.UpdateDate = auditDateTime;
                    model.UpdatedDate = auditDateTime;

                    ReasonBO.Instance.Update(model);
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
        public IActionResult DeleteReason(int id)
        {
            try
            {
                var msgRsn = AdministrationDeleteGuards.GetDeleteReasonBlockReason(id);
                if (msgRsn != null)
                    return Json(new { success = false, message = msgRsn });
                ReasonBO.Instance.Delete(id);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, ex.Message });
            }

            return Json(new { success = true });
        }
        #endregion

        #region ItemCategory/Origin
        [HttpGet]
        public IActionResult GetOrigin(string code, string name, int inactive)
        {
            try
            {
                DataTable dataTable = _iAdministrationService.Origin(code, name, inactive);
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
        public IActionResult Origin()
        {
            return PartialView("ItemCategory/Origin");
        }
        [HttpPost]
        public IActionResult OriginSave([FromBody] OriginModel model)
        {
            string message = "";
            var listErrors = GetErrors(
                Check(model, "general", "Invalid data"),

                Check(model?.Code, "orig_code", "Code is not blank."),
                Check(model?.Name, "orig_name", "Name is not blank.")
            );

            if (listErrors.Count > 0)
            {
                return Json(new { success = false, errors = listErrors });
            }
            try
            {
                if (model.ID == 0)
                {
                    model.CreateDate = auditDateTime;
                    model.CreatedDate = auditDateTime;
                    model.UpdateDate = auditDateTime;
                    model.UpdatedDate = auditDateTime;

                    OriginBO.Instance.Insert(model);
                    message = "Insert successfully!";
                }
                else
                {
                    var oldData = (OriginModel)OriginBO.Instance.FindByPrimaryKey(model.ID);

                    if (oldData != null)
                    {
                        model.UserInsertID = oldData.UserInsertID;
                        model.CreatedBy = oldData.CreatedBy;
                        model.CreateDate = oldData.CreatedDate;
                        model.CreatedDate = oldData.CreatedDate;
                    }

                    model.UpdateDate = auditDateTime;
                    model.UpdatedDate = auditDateTime;

                    OriginBO.Instance.Update(model);
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
        public IActionResult DeleteOrigin(int id)
        {
            try
            {
                var msgOrg = AdministrationDeleteGuards.GetDeleteOriginBlockReason(id);
                if (msgOrg != null)
                    return Json(new { success = false, message = msgOrg });
                OriginBO.Instance.Delete(id);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, ex.Message });
            }

            return Json(new { success = true });
        }
        #endregion

        #region ItemCategory/Source
        [HttpGet]
        public IActionResult GetSource(string code, string name, int inactive)
        {
            try
            {
                DataTable dataTable = _iAdministrationService.Source(code, name, inactive);
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

        public IActionResult Source()
        {
            return PartialView("ItemCategory/Source");
        }

        [HttpPost]
        public IActionResult SourceSave([FromBody] SourceModel model)
        {
            string message = "";
            var listErrors = GetErrors(
                Check(model, "general", "Invalid data"),

                Check(model?.Code, "sour_code", "Code is not blank."),
                Check(model?.Name, "sour_name", "Name is not blank.")
            );

            if (listErrors.Count > 0)
            {
                return Json(new { success = false, errors = listErrors });
            }
            try
            {

                if (model.ID == 0)
                {
                    model.CreateDate = auditDateTime;
                    model.CreatedDate = auditDateTime;
                    model.UpdateDate = auditDateTime;
                    model.UpdatedDate = auditDateTime;

                    SourceBO.Instance.Insert(model);
                    message = "Insert successfully!";
                }
                else
                {
                    var oldData = (SourceModel)SourceBO.Instance.FindByPrimaryKey(model.ID);

                    if (oldData != null)
                    {
                        model.UserInsertID = oldData.UserInsertID;
                        model.CreatedBy = oldData.CreatedBy;
                        model.CreateDate = oldData.CreatedDate;
                        model.CreatedDate = oldData.CreatedDate;
                    }

                    model.UpdateDate = auditDateTime;
                    model.UpdatedDate = auditDateTime;

                    SourceBO.Instance.Update(model);
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
        public IActionResult DeleteSource(int id)
        {
            try
            {
                var msgSrc = AdministrationDeleteGuards.GetDeleteSourceBlockReason(id);
                if (msgSrc != null)
                    return Json(new { success = false, message = msgSrc });
                SourceBO.Instance.Delete(id);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, ex.Message });
            }

            return Json(new { success = true });
        }
        #endregion

        #region ItemCategory/AlertsSetup
        [HttpGet]
        public IActionResult GetAlertsSetup(string code, string name, int inactive)
        {
            try
            {
                DataTable dataTable = _iAdministrationService.AlertsSetup(code, name, inactive);
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
        public IActionResult AlertsSetup()
        {
            return PartialView("ItemCategory/AlertsSetup");
        }
        [HttpPost]
        public IActionResult AlertsSetupSave([FromBody] AlertsSetupModel model)
        {
            string message = "";
            var listErrors = GetErrors(
                Check(model, "general", "Invalid data"),

                Check(model?.Code, "code", "Code is not blank."),
                Check(model?.Description, "name", "Name is not blank.")
            );

            if (listErrors.Count > 0)
            {
                return Json(new { success = false, errors = listErrors });
            }
            try
            {

                if (model.ID == 0)
                {
                    model.CreateDate = auditDateTime;
                    model.CreatedDate = auditDateTime;
                    model.UpdateDate = auditDateTime;
                    model.UpdatedDate = auditDateTime;

                    AlertsSetupBO.Instance.Insert(model);
                    message = "Insert successfully!";
                }
                else
                {
                    var oldData = (AlertsSetupModel)AlertsSetupBO.Instance.FindByPrimaryKey(model.ID);

                    if (oldData != null)
                    {
                        model.UserInsertID = oldData.UserInsertID;
                        model.CreatedBy = oldData.CreatedBy;
                        model.CreateDate = oldData.CreatedDate;
                        model.CreatedDate = oldData.CreatedDate;
                    }

                    model.UpdateDate = auditDateTime;
                    model.UpdatedDate = auditDateTime;

                    AlertsSetupBO.Instance.Update(model);
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
        public IActionResult DeleteAlertsSetup(int id)
        {
            try
            {
                var msgAl = AdministrationDeleteGuards.GetDeleteAlertsSetupBlockReason(id);
                if (msgAl != null)
                    return Json(new { success = false, message = msgAl });
                AlertsSetupBO.Instance.Delete(id);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, ex.Message });
            }

            return Json(new { success = true });
        }
        #endregion

        #region ItemCategory/Comment
        [HttpGet]
        public IActionResult GetComment(string code, string name, int inactive)
        {
            try
            {
                DataTable dataTable = _iAdministrationService.Comment(code, name, inactive);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  Code = !string.IsNullOrEmpty(d["Code"].ToString()) ? d["Code"] : "",
                                  Description = !string.IsNullOrEmpty(d["Description"].ToString()) ? d["Description"] : "",
                                  CommentType = !string.IsNullOrEmpty(d["CommentType"].ToString()) ? d["CommentType"] : "",
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
        public IActionResult Comment()
        {
            List<CommentTypeModel> listctry = PropertyUtils.ConvertToList<CommentTypeModel>(CommentTypeBO.Instance.FindAll());
            ViewBag.CommentTypeList = listctry;
            return PartialView("ItemCategory/Comment");
        }
        [HttpPost]
        public IActionResult CommentSave([FromBody] CommentModel model)
        {
            string message = "";
            var listErrors = GetErrors(
                Check(model, "general", "Invalid data"),

                Check(model?.Code, "comT_code", "Code is not blank."),
                Check(model?.CommentTypeID ?? 0, "comT_commentTypeID", "Comment type must be choose")
            );

            if (listErrors.Count > 0)
            {
                return Json(new { success = false, errors = listErrors });
            }
            try
            {

                if (model.ID == 0)
                {
                    model.CreateDate = auditDateTime;
                    model.CreatedDate = auditDateTime;
                    model.UpdateDate = auditDateTime;
                    model.UpdatedDate = auditDateTime;

                    CommentBO.Instance.Insert(model);
                    message = "Insert successfully!";
                }
                else
                {
                    var oldData = (CommentModel)CommentBO.Instance.FindByPrimaryKey(model.ID);

                    if (oldData != null)
                    {
                        model.UserInsertID = oldData.UserInsertID;
                        model.CreatedBy = oldData.CreatedBy;
                        model.CreateDate = oldData.CreatedDate;
                        model.CreatedDate = oldData.CreatedDate;
                    }

                    model.UpdateDate = auditDateTime;
                    model.UpdatedDate = auditDateTime;

                    CommentBO.Instance.Update(model);
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
        public IActionResult DeleteComment(int id)
        {
            try
            {
                CommentBO.Instance.Delete(id);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, ex.Message });
            }

            return Json(new { success = true });
        }
        #endregion

        #region ItemCategory/CommentType
        [HttpGet]
        public IActionResult GetCommentType(string code, string name, int inactive)
        {
            try
            {
                DataTable dataTable = _iAdministrationService.CommentType(code, name, inactive);
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
        public IActionResult CommentType()
        {
            return PartialView("ItemCategory/CommentType");
        }

        [HttpPost]
        public IActionResult CommentTypeSave([FromBody] CommentTypeModel model)
        {
            string message = "";
            var listErrors = GetErrors(
                Check(model, "general", "Invalid data"),

                Check(model?.Code, "comTy_code", "Code is not blank."),
                Check(model?.Name, "comTy_name", "Name is not blank.")
            );

            if (listErrors.Count > 0)
            {
                return Json(new { success = false, errors = listErrors });
            }
            try
            {
                if (model.ID == 0)
                {
                    model.CreateDate = auditDateTime;
                    model.CreatedDate = auditDateTime;
                    model.UpdateDate = auditDateTime;
                    model.UpdatedDate = auditDateTime;

                    CommentTypeBO.Instance.Insert(model);
                    message = "Insert successfully!";
                }
                else
                {
                    var oldData = (CommentTypeModel)CommentTypeBO.Instance.FindByPrimaryKey(model.ID);

                    if (oldData != null)
                    {
                        model.UserInsertID = oldData.UserInsertID;
                        model.CreatedBy = oldData.CreatedBy;
                        model.CreateDate = oldData.CreatedDate;
                        model.CreatedDate = oldData.CreatedDate;
                    }

                    model.UpdateDate = auditDateTime;
                    model.UpdatedDate = auditDateTime;

                    CommentTypeBO.Instance.Update(model);
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
        public IActionResult DeleteCommentType(int id)
        {
            try
            {
                var msgCt = AdministrationDeleteGuards.GetDeleteCommentTypeBlockReason(id);
                if (msgCt != null)
                    return Json(new { success = false, message = msgCt });
                CommentTypeBO.Instance.Delete(id);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, ex.Message });
            }

            return Json(new { success = true });
        }
        #endregion

        #region ItemCategory/Season
        [HttpGet]
        public IActionResult GetSeason(string code, string name, int inactive)
        {
            try
            {
                DataTable dataTable = _iAdministrationService.Season(code, name, inactive);
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
        public IActionResult Season()
        {
            return PartialView("ItemCategory/Season");
        }
        [HttpPost]
        public IActionResult SeasonSave([FromBody] SeasonModel model)
        {
            string message = "";
            var listErrors = GetErrors(
                Check(model, "general", "Invalid data"),

                Check(model?.Code, "seas_code", "Code is not blank."),
                Check(model?.Name, "seas_name", "Name is not blank.")
            );

            if (listErrors.Count > 0)
            {
                return Json(new { success = false, errors = listErrors });
            }
            try
            {
                if (model.ID == 0)
                {
                    model.CreateDate = auditDateTime;
                    model.CreatedDate = auditDateTime;
                    model.UpdateDate = auditDateTime;
                    model.UpdatedDate = auditDateTime;

                    SeasonBO.Instance.Insert(model);
                    message = "Insert successfully!";
                }
                else
                {
                    var oldData = (SeasonModel)SeasonBO.Instance.FindByPrimaryKey(model.ID);

                    if (oldData != null)
                    {
                        model.UserInsertID = oldData.UserInsertID;
                        model.CreatedBy = oldData.CreatedBy;
                        model.CreateDate = oldData.CreatedDate;
                        model.CreatedDate = oldData.CreatedDate;
                    }

                    model.UpdateDate = auditDateTime;
                    model.UpdatedDate = auditDateTime;

                    SeasonBO.Instance.Update(model);
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
        public IActionResult DeleteSeason(int id)
        {
            try
            {
                var msgSeas = AdministrationDeleteGuards.GetDeleteSeasonBlockReason(id);
                if (msgSeas != null)
                    return Json(new { success = false, message = msgSeas });
                SeasonBO.Instance.Delete(id);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, ex.Message });
            }

            return Json(new { success = true });
        }
        #endregion

        #region ItemCategory/Zone
        [HttpGet]
        public IActionResult GetZone(string code, string name, int inactive)
        {
            try
            {
                DataTable dataTable = _iAdministrationService.Zone(code, name, inactive);
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
                                  ID = d["ID"] != DBNull.Value ? Convert.ToInt32(d["ID"]) : 0,
                                  Inactive = !string.IsNullOrEmpty(d["Inactive"].ToString()) ? d["Inactive"] : "",
                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        public IActionResult Zone()
        {
            return PartialView("ItemCategory/Zone");
        }
        [HttpPost]
        public IActionResult ZoneSave([FromBody] ZoneModel model)
        {
            string message = "";
            var listErrors = GetErrors(
                Check(model, "general", "Invalid data"),

                Check(model?.Code, "zone_code", "Code is not blank."),
                Check(model?.Name, "zone_name", "Name is not blank.")
            );

            if (listErrors.Count > 0)
            {
                return Json(new { success = false, errors = listErrors });
            }
            try
            {

                if (model.ID == 0)
                {
                    model.CreateDate = auditDateTime;
                    model.CreatedDate = auditDateTime;
                    model.UpdateDate = auditDateTime;
                    model.UpdatedDate = auditDateTime;

                    ZoneBO.Instance.Insert(model);
                    message = "Insert successfully!";
                }
                else
                {
                    var oldData = (ZoneModel)ZoneBO.Instance.FindByPrimaryKey(model.ID);

                    if (oldData != null)
                    {
                        model.UserInsertID = oldData.UserInsertID;
                        model.CreatedBy = oldData.CreatedBy;
                        model.CreateDate = oldData.CreatedDate;
                        model.CreatedDate = oldData.CreatedDate;
                    }

                    model.UpdateDate = auditDateTime;
                    model.UpdatedDate = auditDateTime;

                    ZoneBO.Instance.Update(model);
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
        public IActionResult DeleteZone(int id)
        {
            try
            {
                var msgZn = AdministrationDeleteGuards.GetDeleteZoneBlockReason(id);
                if (msgZn != null)
                    return Json(new { success = false, message = msgZn });
                ZoneBO.Instance.Delete(id);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, ex.Message });
            }

            return Json(new { success = true });
        }
        #endregion

        #region ItemCategory/Department
        [HttpGet]
        public IActionResult GetDepartment(string code, string name, int inactive)
        {
            try
            {
                DataTable dataTable = _iAdministrationService.Department(code, name, inactive);
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
        public IActionResult Department()
        {
            return PartialView("ItemCategory/Department");
        }
        [HttpPost]
        public IActionResult DepartmentSave([FromBody] DepartmentModel model)
        {
            string message = "";
            var listErrors = GetErrors(
                Check(model, "general", "Invalid data"),

                Check(model?.Code, "depa_code", "Code is not blank."),
                Check(model?.Name, "depa_name", "Name is not blank.")
            );

            if (listErrors.Count > 0)
            {
                return Json(new { success = false, errors = listErrors });
            }
            try
            {
                if (model.ID == 0)
                {
                    model.CreateDate = auditDateTime;
                    model.CreatedDate = auditDateTime;
                    model.UpdateDate = auditDateTime;
                    model.UpdatedDate = auditDateTime;

                    DepartmentBO.Instance.Insert(model);
                    message = "Insert successfully!";
                }
                else
                {
                    var oldData = (DepartmentModel)DepartmentBO.Instance.FindByPrimaryKey(model.ID);

                    if (oldData != null)
                    {
                        model.UserInsertID = oldData.UserInsertID;
                        model.CreatedBy = oldData.CreatedBy;
                        model.CreateDate = oldData.CreatedDate;
                        model.CreatedDate = oldData.CreatedDate;
                    }

                    model.UpdateDate = auditDateTime;
                    model.UpdatedDate = auditDateTime;

                    DepartmentBO.Instance.Update(model);
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
        public IActionResult DeleteDepartment(int id)
        {
            try
            {
                var msgDep = AdministrationDeleteGuards.GetDeleteDepartmentBlockReason(id);
                if (msgDep != null)
                    return Json(new { success = false, message = msgDep });
                DepartmentBO.Instance.Delete(id);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, ex.Message });
            }

            return Json(new { success = true });
        }
        #endregion

        #region ItemCategory/Occupancy

        public IActionResult Occupancy()
        {
            return PartialView("ItemCategory/Occupancy");
        }
        [HttpGet]
        public IActionResult GetOccupancy()
        {
            try
            {
                DataTable dt = TextUtils.Select(@"SELECT o.ID,  CASE o.[Type] 
                    WHEN 0 THEN 'Hotel' ELSE 'Room Type' END AS [Type], b.Name AS RoomType,
                    o.Occupancylevel,o.Title,o.Email, o.[Description],o.Color, 
                    o.CreateDate, o.CreateBy, o.UpdateDate, o.UpdateBy 
                    FROM Occupancy o left JOIN RoomType b ON o.RoomTypeID=b.ID");
                var result = (from r in dt.AsEnumerable()
                              select new
                              {
                                  ID = !string.IsNullOrEmpty(r["ID"].ToString()) ? r["ID"] : "",
                                  Type = !string.IsNullOrEmpty(r["Type"].ToString()) ? r["Type"] : "",
                                  Occupancylevel = !string.IsNullOrEmpty(r["Occupancylevel"].ToString()) ? r["Occupancylevel"] : "",
                                  Title = !string.IsNullOrEmpty(r["Title"].ToString()) ? r["Title"] : "",
                                  Email = !string.IsNullOrEmpty(r["Email"].ToString()) ? r["Email"] : "",
                                  Description = !string.IsNullOrEmpty(r["Description"].ToString()) ? r["Description"] : "",
                                  Color = !string.IsNullOrEmpty(r["Color"].ToString()) ? r["Color"] : "",
                                  CreateDate = !string.IsNullOrEmpty(r["CreateDate"].ToString()) ? r["CreateDate"] : "",
                                  CreateBy = !string.IsNullOrEmpty(r["CreateBy"].ToString()) ? r["CreateBy"] : "",
                                  UpdateDate = !string.IsNullOrEmpty(r["UpdateDate"].ToString()) ? r["UpdateDate"] : "",
                                  UpdateBy = !string.IsNullOrEmpty(r["UpdateBy"].ToString()) ? r["UpdateBy"] : "",
                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        [HttpPost]
        public IActionResult OccupancySave([FromBody] OccupancyModel model)
        {
            var listErrors = GetErrors(
                Check(model, "general", "Invalid data"),
                Check((model?.Occupancylevel ?? 0) < 0 || (model?.Occupancylevel ?? 0) > 100, "occLevel", "Must be between 0 and 100"),
                Check(model?.Occupancylevel < 0, "occLevel", "Occupancy Level cannot be negative.")
            );

            if (listErrors.Count > 0)
            {
                return Json(new { success = false, errors = listErrors });
            }
            string message;
            try
            {
                if (model.ID == 0)
                {
                    model.CreateDate = auditDateTime;
                    model.UpdateDate = auditDateTime;
                    OccupancyBO.Instance.Insert(model);
                    message = "Insert successfully.";
                }
                else
                {
                    var oldData = (OccupancyModel)OccupancyBO.Instance.FindByPrimaryKey(model.ID);
                    if (oldData != null)
                    {
                        model.CreateBy = oldData.CreateBy;
                        model.CreateDate = oldData.CreateDate;
                    }
                    model.UpdateDate = auditDateTime;
                    OccupancyBO.Instance.Update(model);
                    message = "Update successfully.";
                }
                return Json(new { success = true, message = message });
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        [HttpPost]
        public IActionResult OccupancyDelete(int id)
        {

            try
            {
                var msgOcc = AdministrationDeleteGuards.GetDeleteOccupancyBlockReason(id);
                if (msgOcc != null)
                    return Json(new { success = false, message = msgOcc });
                OccupancyBO.Instance.Delete(id);
                return Json(new { success = true, message = "Delete successfully." });
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        #endregion      

        #region ItemCategory/ConfirmationConfig

        public IActionResult ConfirmationConfig()
        {
            return PartialView("ItemCategory/ConfirmationConfig");
        }
        [HttpGet]
        public IActionResult GetConfirmationConfig()
        {
            try
            {
                DataTable dt = TextUtils.Select("SELECT * FROM ConfirmationConfig");
                var result = (from r in dt.AsEnumerable()
                              select new
                              {
                                  ID = !string.IsNullOrEmpty(r["ID"].ToString()) ? r["ID"] : "",
                                  EmailAddress = !string.IsNullOrEmpty(r["EmailAddress"].ToString()) ? r["EmailAddress"] : "",
                                  MailUser = !string.IsNullOrEmpty(r["MailUser"].ToString()) ? r["MailUser"] : "",
                                  MailPassword = !string.IsNullOrEmpty(r["MailPassword"].ToString()) ? r["MailPassword"] : "",
                                  ServerName = !string.IsNullOrEmpty(r["ServerName"].ToString()) ? r["ServerName"] : "",
                                  ServerPort = !string.IsNullOrEmpty(r["ServerPort"].ToString()) ? r["ServerPort"] : "",
                                  MailSubject = !string.IsNullOrEmpty(r["MailSubject"].ToString()) ? r["MailSubject"] : "",
                                  MailBody = !string.IsNullOrEmpty(r["MailBody"].ToString()) ? r["MailBody"] : "",
                                  MailSubjectENG = !string.IsNullOrEmpty(r["MailSubjectENG"].ToString()) ? r["MailSubjectENG"] : "",
                                  MailBodyENG = !string.IsNullOrEmpty(r["MailBodyENG"].ToString()) ? r["MailBodyENG"] : "",
                                  CreatedBy = !string.IsNullOrEmpty(r["CreatedBy"].ToString()) ? r["CreatedBy"] : "",
                                  CreatedDate = !string.IsNullOrEmpty(r["CreatedDate"].ToString()) ? r["CreatedDate"] : "",
                                  UpdatedBy = !string.IsNullOrEmpty(r["UpdatedBy"].ToString()) ? r["UpdatedBy"] : "",
                                  UpdatedDate = !string.IsNullOrEmpty(r["UpdatedDate"].ToString()) ? r["UpdatedDate"] : "",
                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        [HttpPost]
        public IActionResult ConfirmationConfigSave([FromBody] ConfirmationConfigModel model)
        {
            var listErrors = GetErrors(
                Check(model, "general", "Invalid data"),

                Check(model?.EmailAddress, "cfC_emailAddress", "Email address is not blank."),
                Check(model?.MailUser, "cfC_mailUser", "Mail user is not blank."),
                Check(model?.MailPassword, "cfC_mailPassword", "Mail password is not blank."),
                Check(model?.ServerName, "cfC_serverName", "Server name is not blank."),
                Check(model?.ServerPort ?? 0, "cfC_serverPort", "Port must be greater than 0."),
                Check(model?.MailSubject, "cfC_mailSubject", "Mail Subject is not blank."),
                Check(model?.MailBody, "cfC_mailBody", "Mail body is not blank."),
                Check(model?.MailSubjectENG, "cfC_mailSubjectENG", "Mail Subject english is not blank."),
                Check(model?.MailBodyENG, "cfC_mailBodyENG", "Mail body english is not blank.")
            );

            if (listErrors.Count > 0)
            {
                return Json(new { success = false, errors = listErrors });
            }
            string message;
            try
            {
                var oldData = (ConfirmationConfigModel)ConfirmationConfigBO.Instance.FindByPrimaryKey(model.ID);
                if (oldData != null)
                {
                    model.CreatedBy = oldData.CreatedBy;
                    model.CreatedDate = oldData.CreatedDate;
                }
                model.UpdatedDate = DateTime.Now;
                ConfirmationConfigBO.Instance.Update(model);
                message = "Update successfully.";
                return Json(new { success = true, message = message });
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        #endregion

        #region ItemCategory/ConfirmationTemp

        public IActionResult ConfirmationTemp()
        {
            List<RateCodeModel> listRateCode = PropertyUtils.ConvertToList<RateCodeModel>(RateCodeBO.Instance.FindAll());
            ViewBag.RateCodeList = listRateCode;
            List<LanguageModel> listLanguage = PropertyUtils.ConvertToList<LanguageModel>(LanguageBO.Instance.FindAll());
            ViewBag.LanguageList = listLanguage;
            return PartialView("ItemCategory/ConfirmationTemp");
        }
        [HttpGet]
        public IActionResult GetConfirmationTemp()
        {
            try
            {
                DataTable dt = TextUtils.Select(@"SELECT  
                            ct.ID,ct.LetterName,ct.RateCodeID,
                            rc.RateCode AS RateCode,ct.Nationality,ct.Template
                            FROM ConfirmationTemp ct
                            LEFT JOIN RateCode rc ON ct.RateCodeID = rc.ID");
                var result = (from r in dt.AsEnumerable()
                              select new
                              {
                                  ID = !string.IsNullOrEmpty(r["ID"].ToString()) ? r["ID"] : "",
                                  LetterName = !string.IsNullOrEmpty(r["LetterName"].ToString()) ? r["LetterName"] : "",
                                  RateCodeID = !string.IsNullOrEmpty(r["RateCodeID"].ToString()) ? r["RateCodeID"] : "",
                                  RateCode = !string.IsNullOrEmpty(r["RateCode"].ToString()) ? r["RateCode"] : "",
                                  Nationality = !string.IsNullOrEmpty(r["Nationality"].ToString()) ? r["Nationality"] : "",
                                  Template = !string.IsNullOrEmpty(r["Template"].ToString()) ? r["Template"] : "",
                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        [HttpPost]
        public IActionResult ConfirmationTempSave([FromBody] ConfirmationTempModel model)
        {
            var listErrors = GetErrors(
               Check(model, "general", "Invalid data"),

               Check(model?.LetterName, "letterNameInput", "Letter name is not blank."),
               Check(model?.RateCodeID, "cfT_rateCodeID", "Please select Rate Code."),
               Check(model?.Nationality, "cfT_nationality", "Please select Nationality.")
            );

            if (listErrors.Count > 0)
            {
                return Json(new { success = false, errors = listErrors });
            }
            string message;
            try
            {
                if (model.ID == 0)
                {
                    model.CreatedDate = DateTime.Now;
                    model.UpdatedDate = DateTime.Now;
                    ConfirmationTempBO.Instance.Insert(model);
                    message = $"Insert \"{model.LetterName}\" successfully.";
                }
                else
                {
                    var oldData = (ConfirmationTempModel)ConfirmationTempBO.Instance.FindByPrimaryKey(model.ID);
                    if (oldData != null)
                    {
                        model.CreatedBy = oldData.CreatedBy;
                        model.CreatedDate = oldData.CreatedDate;
                    }
                    model.UpdatedDate = DateTime.Now;
                    ConfirmationTempBO.Instance.Update(model);
                    message = $"Update \"{model.LetterName}\" successfully.";
                }
                return Json(new { success = true, message = message, id = model.ID });
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        [HttpPost]
        public IActionResult ConfirmationTempDelete(int id)
        {
            try
            {
                DataTable dt = TextUtils.Select($@"SELECT COUNT(1)
                                            FROM ConfirmationTemp
                                            WHERE ID = {id}
                                              AND RateCodeID > 0
                                            ");
                // if (dt.Rows.Count > 0 && Convert.ToInt32(dt.Rows[0][0]) > 0)
                //     return Json(new { success = false, message = "Cannot delete. This template is already linked to a Rate Code." });
                ConfirmationTempBO.Instance.Delete(id);
                return Json(new { success = true, message = "Delete successfully." });
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        #endregion

        #region ItemCategory/Owner
        [HttpGet]
        public IActionResult GetOwner(string code, string name, int inactive)
        {
            try
            {
                DataTable dataTable = _iAdministrationService.Owner(code, name, inactive);
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
        public IActionResult Owner()
        {
            return View("ItemCategory/Owner");
        }
        [HttpPost]
        public IActionResult OwnerSave([FromBody] OwnerModel model)
        {
            string message = "";

            var listErrors = GetErrors(
                Check(model, "general", "Invalid data"),

                Check(model?.Code, "code", "Code is not blank."),
                Check(model?.Name, "name", "Description is not blank.")
            );

            if (listErrors.Count > 0)
            {
                return Json(new { success = false, errors = listErrors });
            }
            try
            {
                if (model.ID == 0)
                {
                    model.CreateDate = DateTime.Now;
                    model.CreatedDate = DateTime.Now;
                    model.UpdateDate = DateTime.Now;
                    model.UpdatedDate = DateTime.Now;

                    OwnerBO.Instance.Insert(model);
                    message = "Insert successfully!";
                }
                else
                {
                    var oldData = (OwnerModel)OwnerBO.Instance.FindByPrimaryKey(model.ID);

                    if (oldData != null)
                    {
                        model.UserInsertID = oldData.UserInsertID;
                        model.CreatedBy = oldData.CreatedBy;
                        model.CreateDate = oldData.CreateDate;
                        model.CreatedDate = oldData.CreatedDate;
                    }

                    model.UpdateDate = DateTime.Now;
                    model.UpdatedDate = DateTime.Now;

                    OwnerBO.Instance.Update(model);
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
        public IActionResult DeleteOwner(int id)
        {
            try
            {
                var msgOwn = AdministrationDeleteGuards.GetDeleteOwnerBlockReason(id);
                if (msgOwn != null)
                    return Json(new { success = false, message = msgOwn });
                OwnerBO.Instance.Delete(id);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, ex.Message });
            }

            return Json(new { success = true });
        }
        #endregion

        #region ItemCategory/PropertyType
        [HttpGet]
        public IActionResult GetPropertyType(string code, string description, int sequence)
        {
            try
            {
                DataTable dataTable = _iAdministrationService.PropertyType(code, description, sequence);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  Code = !string.IsNullOrEmpty(d["Code"].ToString()) ? d["Code"] : "",
                                  Sequence = !string.IsNullOrEmpty(d["Sequence"].ToString()) ? d["Sequence"] : "",
                                  Description = !string.IsNullOrEmpty(d["Description"].ToString()) ? d["Description"] : "",
                                  CreatedBy = !string.IsNullOrEmpty(d["CreatedBy"].ToString()) ? d["CreatedBy"] : "",
                                  CreatedDate = !string.IsNullOrEmpty(d["CreatedDate"].ToString()) ? d["CreatedDate"] : "",
                                  UpdatedBy = !string.IsNullOrEmpty(d["UpdatedBy"].ToString()) ? d["UpdatedBy"] : "",
                                  UpdatedDate = !string.IsNullOrEmpty(d["UpdatedDate"].ToString()) ? d["UpdatedDate"] : "",
                                  ID = !string.IsNullOrEmpty(d["ID"].ToString()) ? d["ID"] : "",
                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        public IActionResult PropertyType()
        {
            return PartialView("ItemCategory/PropertyType");
        }
        [HttpPost]
        public IActionResult PropertyTypeSave([FromBody] PropertyTypeModel model)
        {
            var listErrors = GetErrors(
                Check(model == null, "general", "Invalid data"),

                Check(model?.Code, "proTp_code", "Code is not blank."),
                Check(model.Sequence < 0, "proTp_sequence", "Sequence must be >= 0")

            );

            if (listErrors.Count > 0)
            {
                return Json(new { success = false, errors = listErrors });
            }

            string message;
            try
            {
                if (model.ID == 0)
                {
                    model.CreatedDate = auditDateTime;
                    model.UpdatedDate = auditDateTime;
                    PropertyTypeBO.Instance.Insert(model);
                    message = "Insert successfully.";
                }
                else
                {
                    var oldData = (PropertyTypeModel)PropertyTypeBO.Instance.FindByPrimaryKey(model.ID);
                    if (oldData != null)
                    {
                        model.CreatedBy = oldData.CreatedBy;
                        model.CreatedDate = oldData.CreatedDate;
                    }
                    model.UpdatedDate = auditDateTime;
                    PropertyTypeBO.Instance.Update(model);
                    message = "Update successfully.";
                }
                return Json(new { success = true, message = message });
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        [HttpPost]
        public IActionResult PropertyTypeDelete(int id)
        {
            try
            {
                var msgPt = AdministrationDeleteGuards.GetDeletePropertyTypeBlockReason(id);
                if (msgPt != null)
                    return Json(new { success = false, message = msgPt });
                PropertyTypeBO.Instance.Delete(id);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, ex.Message });
            }
            return Json(new { success = true });
        }
        #endregion

        #region ItemCategory/Property
        [HttpGet]
        public IActionResult GetProperty()
        {
            try
            {
                DataTable dataTable = _iAdministrationService.Property();
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  PropertyTypeID = !string.IsNullOrEmpty(d["PropertyTypeID"].ToString()) ? d["PropertyTypeID"] : "",
                                  PropertyCode = !string.IsNullOrEmpty(d["PropertyCode"].ToString()) ? d["PropertyCode"] : "",
                                  PropertyName = !string.IsNullOrEmpty(d["PropertyName"].ToString()) ? d["PropertyName"] : "",
                                  Telephone = !string.IsNullOrEmpty(d["Telephone"].ToString()) ? d["Telephone"] : "",
                                  Fax = !string.IsNullOrEmpty(d["Fax"].ToString()) ? d["Fax"] : "",
                                  Email = !string.IsNullOrEmpty(d["Email"].ToString()) ? d["Email"] : "",
                                  Website = !string.IsNullOrEmpty(d["Website"].ToString()) ? d["Website"] : "",
                                  Address = !string.IsNullOrEmpty(d["Address"].ToString()) ? d["Address"] : "",
                                  ServerName = !string.IsNullOrEmpty(d["ServerName"].ToString()) ? d["ServerName"] : "",
                                  DatabaseName = !string.IsNullOrEmpty(d["DatabaseName"].ToString()) ? d["DatabaseName"] : "",
                                  Login = !string.IsNullOrEmpty(d["Login"].ToString()) ? d["Login"] : "",
                                  Password = !string.IsNullOrEmpty(d["Password"].ToString()) ? d["Password"] : "",
                                  PropertyType = !string.IsNullOrEmpty(d["PropertyType"].ToString()) ? d["PropertyType"] : "",
                                  CreatedBy = !string.IsNullOrEmpty(d["CreatedBy"].ToString()) ? d["CreatedBy"] : "",
                                  CreatedDate = !string.IsNullOrEmpty(d["CreatedDate"].ToString()) ? d["CreatedDate"] : "",
                                  UpdatedBy = !string.IsNullOrEmpty(d["UpdatedBy"].ToString()) ? d["UpdatedBy"] : "",
                                  UpdatedDate = !string.IsNullOrEmpty(d["UpdatedDate"].ToString()) ? d["UpdatedDate"] : "",
                                  Inactive = !string.IsNullOrEmpty(d["Inactive"].ToString()) ? d["Inactive"] : "",
                                  ID = !string.IsNullOrEmpty(d["ID"].ToString()) ? d["ID"] : "",
                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        public IActionResult Property()
        {
            List<PropertyTypeModel> listPropertyType = PropertyUtils.ConvertToList<PropertyTypeModel>(PropertyTypeBO.Instance.FindAll());
            ViewBag.PropertyTypeList = listPropertyType;
            return PartialView("ItemCategory/Property");
        }
        [HttpPost]
        public IActionResult PropertySave([FromBody] PropertyModel model)
        {
            var listErrors = GetErrors(
                Check(model, "general", "Invalid data"),
                Check(model?.PropertyCode, "prop_code", "Code is not blank."),
                Check(model?.PropertyName, "prop_propertyName", "Name is not blank."),
                Check(model?.PropertyTypeID, "prop_propertyType", "Property type is not blank."),
                Check(model?.ServerName, "prop_serverName", "Server name is not blank."),
                Check(model?.DatabaseName, "prop_databaseName", "Database name is not blank."),
                Check(model?.Login, "prop_login", "Login account is not blank."),
                Check(model?.Password, "prop_password", "Password is not blank."),
                Check(model?.Password?.Length < 6, "prop_password", "Password must be at least 6 characters.")
            );

            if (listErrors.Count > 0)
            {
                return Json(new { success = false, errors = listErrors });
            }
            string message;
            try
            {
                if (model.ID == 0)
                {
                    model.CreatedDate = DateTime.Now;
                    model.UpdatedDate = DateTime.Now;
                    PropertyBO.Instance.Insert(model);
                    message = "Insert successfully.";
                }
                else
                {
                    var oldData = (PropertyModel)PropertyBO.Instance.FindByPrimaryKey(model.ID);
                    if (oldData != null)
                    {
                        model.CreatedBy = oldData.CreatedBy;
                        model.CreatedDate = oldData.CreatedDate;
                    }
                    model.UpdatedDate = DateTime.Now;
                    PropertyBO.Instance.Update(model);
                    message = "Update successfully.";
                }
                return Json(new { success = true, message = message });
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        [HttpPost]
        public IActionResult PropertyDelete(int id)
        {
            try
            {
                var msgProp = AdministrationDeleteGuards.GetDeletePropertyBlockReason(id);
                if (msgProp != null)
                    return Json(new { success = false, message = msgProp });
                PropertyBO.Instance.Delete(id);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, ex.Message });
            }
            return Json(new { success = true });
        }
        #endregion

        #region ItemCategory/PropertyPermission
        [HttpGet]
        public IActionResult GetPropertyPermission(string userID)
        {
            try
            {
                DataTable dataTable = _iAdministrationService.PropertyPermission(userID);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  PropertyID = !string.IsNullOrEmpty(d["PropertyID"].ToString()) ? d["PropertyID"] : "",
                                  PropertyCode = !string.IsNullOrEmpty(d["PropertyCode"].ToString()) ? d["PropertyCode"] : "",
                                  PropertyName = !string.IsNullOrEmpty(d["PropertyName"].ToString()) ? d["PropertyName"] : "",
                                  UserID = !string.IsNullOrEmpty(d["UserID"].ToString()) ? d["UserID"] : "",
                                  LoginName = !string.IsNullOrEmpty(d["LoginName"].ToString()) ? d["LoginName"] : "",
                                  CreatedBy = !string.IsNullOrEmpty(d["CreatedBy"].ToString()) ? d["CreatedBy"] : "",
                                  CreatedDate = !string.IsNullOrEmpty(d["CreatedDate"].ToString()) ? d["CreatedDate"] : "",
                                  UpdatedBy = !string.IsNullOrEmpty(d["UpdatedBy"].ToString()) ? d["UpdatedBy"] : "",
                                  UpdatedDate = !string.IsNullOrEmpty(d["UpdatedDate"].ToString()) ? d["UpdatedDate"] : "",
                                  ID = !string.IsNullOrEmpty(d["ID"].ToString()) ? d["ID"] : "",
                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        public IActionResult PropertyPermission()
        {
            List<PropertyModel> listProperty = PropertyUtils.ConvertToList<PropertyModel>(PropertyBO.Instance.FindAll());
            ViewBag.PropertyList = listProperty;
            List<UsersModel> listuser = PropertyUtils.ConvertToList<UsersModel>(UsersBO.Instance.FindAll());
            ViewBag.UsersList = listuser;
            return PartialView("ItemCategory/PropertyPermission");
        }

        [HttpPost]
        public IActionResult PropertyPermissionSave([FromBody] List<PropertyPermissionModel> listModels)
        {
            var listErrors = GetErrors(
                Check(listModels, "general", "No data received."),
                Check(listModels != null && listModels.Count == 0, "general", "No data received.")
            );

            if (listErrors.Count > 0)
                return Json(new { success = false, errors = listErrors });

            try
            {
                int rowIndex = 1;

                foreach (var model in listModels)
                {
                    // VALIDATE REQUIRED TRƯỚC
                    var rowErrors = GetErrors(
                        Check(model.UserID == 0, "propPer_chooseUser    ", "User is required."),
                        Check(model.PropertyID == 0, "propPer_choosePropertyType", "Property is required.")
                    );

                    if (rowErrors.Count > 0)
                    {
                        return Json(new { success = false, errors = rowErrors });
                    }

                    // CHỈ KHI DỮ LIỆU HỢP LỆ MỚI CHECK DUPLICATE
                    bool isDuplicate = PropertyPermissionBO.Instance
                        .IsDuplicatePermission(model.UserID, model.PropertyID, model.ID);

                    if (isDuplicate)
                    {
                        UsersModel userLogin =
                            (UsersModel)UsersBO.Instance.FindByPrimaryKey(model.UserID);

                        string userName = !string.IsNullOrEmpty(userLogin?.LoginName)
                            ? $"User '{userLogin.LoginName}'"
                            : "This user";

                        return Json(new
                        {
                            success = false,
                            errors = new[]
                            {
                        new {
                            field = "choosePropertyType",
                            message = $"{userName} already has this property."
                        }
                    }
                        });
                    }

                    rowIndex++;
                }

                // ================= SAVE =================
                foreach (var model in listModels)
                {
                    if (model.ID == 0)
                    {
                        model.CreatedDate = DateTime.Now;
                        model.UpdatedDate = DateTime.Now;
                        PropertyPermissionBO.Instance.Insert(model);
                    }
                    else
                    {
                        var oldData = (PropertyPermissionModel)
                            PropertyPermissionBO.Instance.FindByPrimaryKey(model.ID);

                        if (oldData != null)
                        {
                            model.CreatedBy = oldData.CreatedBy;
                            model.CreatedDate = oldData.CreatedDate;
                        }

                        model.UpdatedDate = DateTime.Now;
                        PropertyPermissionBO.Instance.Update(model);
                    }
                }

                return Json(new { success = true, message = "Successfully saved permissions!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public IActionResult DeletePropertyPermission([FromBody] List<int> ids)
        {
            string message = "";
            int successCount = 0;

            try
            {
                if (ids == null || ids.Count == 0)
                {
                    return Json(new { success = false, message = "No items selected to delete." });
                }

                foreach (var id in ids)
                {
                    if (id > 0)
                    {
                        PropertyPermissionBO.Instance.Delete(id);
                        successCount++;
                    }
                }

                message = $"Successfully deleted {successCount} items!";
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }

            return Json(new { success = true, message });
        }
        #endregion //

        #region ItemCategory/PackageForecastGroup
        [HttpGet]
        public IActionResult GetPackageForecastGroup(string code, string name, int inactive)
        {
            try
            {
                DataTable dataTable = _iAdministrationService.PackageForecastGroup(code, name, inactive);
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
        public IActionResult PackageForecastGroup()
        {
            return PartialView("ItemCategory/PackageForecastGroup");
        }
        [HttpPost]
        public IActionResult PackageForecastGroupSave([FromBody] PackageForecastGroupModel model)
        {
            string message = "";
            var listErrors = GetErrors(
                Check(model, "general", "Invalid data"),

                Check(model?.Code, "pfg_code", "Code is not blank."),
                Check(model?.Name, "pfg_name", "Description is not blank.")
            );

            if (listErrors.Count > 0)
            {
                return Json(new { success = false, errors = listErrors });
            }
            try
            {
                if (model.ID == 0)
                {
                    model.CreateDate = auditDateTime;
                    model.CreatedDate = auditDateTime;
                    model.UpdateDate = auditDateTime;
                    model.UpdatedDate = auditDateTime;

                    PackageForecastGroupBO.Instance.Insert(model);
                    message = "Insert successfully!";
                }
                else
                {
                    var oldData = (PackageForecastGroupModel)PackageForecastGroupBO.Instance.FindByPrimaryKey(model.ID);

                    if (oldData != null)
                    {
                        model.UserInsertID = oldData.UserInsertID;
                        model.CreatedBy = oldData.CreatedBy;
                        model.CreateDate = oldData.CreatedDate;
                        model.CreatedDate = oldData.CreatedDate;
                    }

                    model.UpdateDate = auditDateTime;
                    model.UpdatedDate = auditDateTime;

                    PackageForecastGroupBO.Instance.Update(model);
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
        public IActionResult DeletePackageForecastGroup(int id)
        {
            try
            {
                var msgPfg = AdministrationDeleteGuards.GetDeletePackageForecastGroupBlockReason(id);
                if (msgPfg != null)
                    return Json(new { success = false, message = msgPfg });
                PackageForecastGroupBO.Instance.Delete(id);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, ex.Message });
            }

            return Json(new { success = true });
        }
        #endregion //

        #region ItemCategory/PreferenceGroup
        [HttpGet]
        public IActionResult GetPreferenceGroup(string code, string name, int inactive)
        {
            try
            {
                DataTable dataTable = _iAdministrationService.PreferenceGroup(code, name, inactive);
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
        public IActionResult PreferenceGroup()
        {
            return PartialView("ItemCategory/PreferenceGroup");
        }
        [HttpPost]
        public IActionResult PreferenceGroupSave([FromBody] PreferenceGroupModel model)
        {
            string message = "";
            var listErrors = GetErrors(
                Check(model, "general", "Invalid data"),

                Check(model?.Code, "preG_code", "Code is not blank."),
                Check(model?.Name, "preG_name", "Description is not blank.")
            );

            if (listErrors.Count > 0)
            {
                return Json(new { success = false, errors = listErrors });
            }
            try
            {
                if (model.ID == 0)
                {
                    model.CreateDate = auditDateTime;
                    model.CreatedDate = auditDateTime;
                    model.UpdateDate = auditDateTime;
                    model.UpdatedDate = auditDateTime;

                    PreferenceGroupBO.Instance.Insert(model);
                    message = "Insert successfully!";
                }
                else
                {
                    var oldData = (PreferenceGroupModel)PreferenceGroupBO.Instance.FindByPrimaryKey(model.ID);

                    if (oldData != null)
                    {
                        model.UserInsertID = oldData.UserInsertID;
                        model.CreatedBy = oldData.CreatedBy;
                        model.CreateDate = oldData.CreatedDate;
                        model.CreatedDate = oldData.CreatedDate;
                    }

                    model.UpdateDate = auditDateTime;
                    model.UpdatedDate = auditDateTime;

                    PreferenceGroupBO.Instance.Update(model);
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
        public IActionResult DeletePreferenceGroup(int id)
        {
            try
            {
                var msgPg = AdministrationDeleteGuards.GetDeletePreferenceGroupBlockReason(id);
                if (msgPg != null)
                    return Json(new { success = false, message = msgPg });
                PreferenceGroupBO.Instance.Delete(id);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, ex.Message });
            }

            return Json(new { success = true });
        }
        #endregion

        #region ItemCategory/GroupOwner

        public IActionResult GroupOwner()
        {
            return View("ItemCategory/GroupOwner");
        }
        [HttpGet]
        public IActionResult GetGroupOwner()
        {
            try
            {
                DataTable dt = TextUtils.Select(@"SELECT * From GroupOwner with (nolock) Order by ID");
                var result = (from r in dt.AsEnumerable()
                              select new
                              {
                                  ID = !string.IsNullOrEmpty(r["ID"].ToString()) ? r["ID"] : "",
                                  GroupOwnerName = !string.IsNullOrEmpty(r["GroupOwnerName"].ToString()) ? r["GroupOwnerName"] : "",
                                  GroupOwnerCode = !string.IsNullOrEmpty(r["GroupOwnerCode"].ToString()) ? r["GroupOwnerCode"] : "",
                                  Description = !string.IsNullOrEmpty(r["Description"].ToString()) ? r["Description"] : "",
                                  Contact = !string.IsNullOrEmpty(r["Contact"].ToString()) ? r["Contact"] : "",
                                  Address = !string.IsNullOrEmpty(r["Address"].ToString()) ? r["Address"] : "",
                                  Email = !string.IsNullOrEmpty(r["Email"].ToString()) ? r["Email"] : "",
                                  Telephone = !string.IsNullOrEmpty(r["Telephone"].ToString()) ? r["Telephone"] : "",
                                  CreatedDate = !string.IsNullOrEmpty(r["CreatedDate"].ToString()) ? r["CreatedDate"] : "",
                                  CreatedBy = !string.IsNullOrEmpty(r["CreatedBy"].ToString()) ? r["CreatedBy"] : "",
                                  UpdatedDate = !string.IsNullOrEmpty(r["UpdatedDate"].ToString()) ? r["UpdatedDate"] : "",
                                  UpdatedBy = !string.IsNullOrEmpty(r["UpdatedBy"].ToString()) ? r["UpdatedBy"] : "",
                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        [HttpPost]
        public IActionResult GroupOwnerSave([FromBody] GroupOwnerModel model)
        {
            string message = "";

            var listErrors = GetErrors(
                Check(model, "general", "Invalid data"),
                Check(model?.GroupOwnerCode, "code", "Code is not blank."),
                Check(model?.GroupOwnerName, "name", "Name is not blank.")
            );

            if (listErrors.Count == 0 && model != null)
            {
                bool isDuplicate = GroupOwnerBO.Instance
                    .IsDuplicateCode(model.GroupOwnerCode, model.ID);

                var duplicateError = CheckDuplicate(
                    isDuplicate,
                    "code",
                    $"This code already exists: [{model.GroupOwnerCode}]"
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
                if (model.ID == 0)
                {
                    model.CreatedDate = DateTime.Now;
                    model.UpdatedDate = DateTime.Now;
                    GroupOwnerBO.Instance.Insert(model);
                    message = "Insert successfully.";
                }
                else
                {
                    var oldData = (GroupOwnerModel)GroupOwnerBO.Instance.FindByPrimaryKey(model.ID);
                    if (oldData != null)
                    {
                        model.GroupOwnerCode = oldData.GroupOwnerCode;
                        model.CreatedBy = oldData.CreatedBy;
                        model.CreatedDate = oldData.CreatedDate;
                    }
                    model.UpdatedDate = DateTime.Now;
                    GroupOwnerBO.Instance.Update(model);
                    message = "Update successfully.";
                }
                return Json(new { success = true, message = message });
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        [HttpPost]
        public IActionResult GroupOwnerDelete(int id)
        {
            try
            {
                var msgGo = AdministrationDeleteGuards.GetDeleteGroupOwnerBlockReason(id);
                if (msgGo != null)
                    return Json(new { success = false, message = msgGo });
                GroupOwnerBO.Instance.Delete(id);
                return Json(new { success = true, message = "Delete successfully." });
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        #endregion

        #region ItemCategory/GroupAndOwner

        public IActionResult GroupAndRoom()
        {
            List<RoomOwnerProfileModel> rooms = PropertyUtils.ConvertToList<RoomOwnerProfileModel>(RoomOwnerProfileBO.Instance.FindAll());
            ViewBag.RoomOwnerList = rooms;

            List<GroupOwnerModel> groups = PropertyUtils.ConvertToList<GroupOwnerModel>(GroupOwnerBO.Instance.FindAll());
            ViewBag.GroupOwnerList = groups;
            return View("ItemCategory/GroupAndRoom");
        }
        [HttpGet]
        public IActionResult GetGroupAndOwner()
        {
            try
            {
                DataTable dt = TextUtils.Select(@"
                    SELECT 
                        gao.ID,
                        gao.GroupOwnerID,
                        go.GroupOwnerName AS GroupOwnerName,
                        gao.RoomOwnerID,
                        r.RoomNo AS RoomNo,
                        gao.CreatedDate,
                        gao.CreatedBy,
                        gao.UpdatedDate,
                        gao.UpdatedBy
                    FROM GroupAndOwner gao WITH (NOLOCK)
                    LEFT JOIN GroupOwner go WITH (NOLOCK) ON gao.GroupOwnerID = go.ID
                    LEFT JOIN RoomOwnerProfile r WITH (NOLOCK) ON gao.RoomOwnerID = r.ID
                    ORDER BY gao.ID
                ");

                var result = (from r in dt.AsEnumerable()
                              select new
                              {
                                  id = r["ID"]?.ToString(),
                                  groupOwnerID = r["GroupOwnerID"]?.ToString(),
                                  groupOwnerName = r["GroupOwnerName"]?.ToString(),
                                  roomOwnerID = r["RoomOwnerID"]?.ToString(),
                                  roomNo = r["RoomNo"]?.ToString(),
                                  createdDate = r["CreatedDate"],
                                  createdBy = r["CreatedBy"]?.ToString(),
                                  updatedDate = r["UpdatedDate"],
                                  updatedBy = r["UpdatedBy"]?.ToString()
                              }).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        [HttpPost]
        public IActionResult GroupAndOwnerSave([FromBody] GroupAndOwnerModel model)
        {
            string message = "";

            var listErrors = GetErrors(
                Check(model, "general", "Invalid data"),
                Check(model?.GroupOwnerID, "groupOwnerID", "Please select group owner."),
                Check(model?.RoomOwnerID, "roomOwnerID", "Please select room owner.")
            );

            if (listErrors.Count == 0 && model != null)
            {
                bool isDuplicate = GroupAndOwnerBO.Instance.IsDuplicatGroupAndOwner(model.RoomOwnerID, model.ID);
                if (isDuplicate)
                {
                    var groupAndOwner = GroupAndOwnerBO.Instance
                       .FindByAttribute("RoomOwnerID", model.RoomOwnerID)
                       .Cast<GroupAndOwnerModel>()
                       .FirstOrDefault(x => x.ID != model.ID);

                    string groupOwnerName = "another group";

                    if (groupAndOwner != null)
                    {
                        groupOwnerName = GroupOwnerBO.Instance
                            .FindByPrimaryKey(groupAndOwner.GroupOwnerID)
                            is GroupOwnerModel gr
                                ? gr.GroupOwnerName
                                : groupOwnerName;
                    }
                    var duplicateError = CheckDuplicate(isDuplicate, "roomOwnerID", $"This room already was GroupOwner: [" + groupOwnerName + "].");
                    if (duplicateError != null) { listErrors.Add(duplicateError); }
                }
            }

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
                    GroupAndOwnerBO.Instance.Insert(model);
                    message = "Insert successfully.";
                }
                else
                {
                    var oldData = (GroupAndOwnerModel)GroupAndOwnerBO.Instance.FindByPrimaryKey(model.ID);
                    if (oldData != null)
                    {
                        model.RoomOwnerID = oldData.RoomOwnerID;
                        model.CreatedBy = oldData.CreatedBy;
                        model.CreatedDate = oldData.CreatedDate;
                    }
                    model.UpdatedDate = DateTime.Now;
                    GroupAndOwnerBO.Instance.Update(model);
                    message = "Update successfully.";
                }
                return Json(new { success = true, message = message });
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        [HttpPost]
        public IActionResult GroupAndOwnerDelete(int id)
        {
            try
            {
                GroupAndOwnerBO.Instance.Delete(id);
                return Json(new { success = true, message = "Delete successfully." });
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        #endregion

        #region ItemCategory/RoomOwner

        public IActionResult RoomOwner()
        {
            List<RoomModel> rooms = PropertyUtils.ConvertToList<RoomModel>(RoomBO.Instance.FindAll());
            ViewBag.RoomList = rooms;

            List<OwnerModel> owners = PropertyUtils.ConvertToList<OwnerModel>(OwnerBO.Instance.FindAll());
            ViewBag.OwnerList = owners;

            return View("ItemCategory/RoomOwner");
        }
        [HttpGet]
        public IActionResult GetRoomOwnerProfile()
        {
            try
            {
                DataTable dt = TextUtils.Select(@"
                    SELECT 
                        ro.ID,
                        ro.OwnerName,
                        ro.OwnerCode,
                        r.ID AS RoomID,
                        r.RoomNo,
                        ro.CreatedDate,
                        ro.CreatedBy,
                        ro.UpdatedDate,
                        ro.UpdatedBy
                    FROM RoomOwnerProfile ro WITH (NOLOCK)
                    LEFT JOIN Room r WITH (NOLOCK) ON ro.RoomID = r.ID
                    ORDER BY ro.ID 
                ");
                var result = (from r in dt.AsEnumerable()
                              select new
                              {
                                  ID = !string.IsNullOrEmpty(r["ID"].ToString()) ? r["ID"] : "",
                                  OwnerName = !string.IsNullOrEmpty(r["OwnerName"].ToString()) ? r["OwnerName"] : "",
                                  OwnerCode = !string.IsNullOrEmpty(r["OwnerCode"].ToString()) ? r["OwnerCode"] : "",
                                  RoomNo = !string.IsNullOrEmpty(r["RoomNo"].ToString()) ? r["RoomNo"] : "",
                                  RoomID = !string.IsNullOrEmpty(r["RoomID"].ToString()) ? r["RoomID"] : "",
                                  CreatedDate = !string.IsNullOrEmpty(r["CreatedDate"].ToString()) ? r["CreatedDate"] : "",
                                  CreatedBy = !string.IsNullOrEmpty(r["CreatedBy"].ToString()) ? r["CreatedBy"] : "",
                                  UpdatedDate = !string.IsNullOrEmpty(r["UpdatedDate"].ToString()) ? r["UpdatedDate"] : "",
                                  UpdatedBy = !string.IsNullOrEmpty(r["UpdatedBy"].ToString()) ? r["UpdatedBy"] : "",
                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        [HttpPost]
        public IActionResult RoomOwnerProfileSave([FromBody] RoomOwnerProfileModel model)
        {
            string message = "";

            var listErrors = GetErrors(
                Check(model, "general", "Invalid data"),
                Check(model?.RoomID, "roomID", "Please select Room."),
                Check(model?.OwnerCode, "ownerCode", "Please select Owner.")
            );

            if (listErrors.Count == 0 && model != null)
            {
                bool isDuplicate = RoomOwnerProfileBO.Instance
                    .IsDuplicateRoomOwner(model.RoomID, model.ID);

                if (isDuplicate)
                {
                    var owners = RoomOwnerProfileBO.Instance
                        .FindByAttribute("RoomID", model.RoomID)
                        .Cast<RoomOwnerProfileModel>()
                        .Where(x => x.ID != model.ID)
                        .ToList();

                    string ownerName = owners.FirstOrDefault()?.OwnerName ?? "another owner";

                    var duplicateError = CheckDuplicate(isDuplicate, "roomID", $"This room already was Owner: [" + ownerName + "].");
                    if (duplicateError != null) { listErrors.Add(duplicateError); }
                }
            }

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
                    RoomOwnerProfileBO.Instance.Insert(model);
                    message = "Insert successfully.";
                }
                else
                {
                    var oldData = (RoomOwnerProfileModel)RoomOwnerProfileBO.Instance.FindByPrimaryKey(model.ID);
                    if (oldData != null)
                    {
                        model.RoomNo = oldData.RoomNo;
                        model.RoomID = oldData.RoomID;
                        model.CreatedBy = oldData.CreatedBy;
                        model.CreatedDate = oldData.CreatedDate;
                    }
                    model.UpdatedDate = DateTime.Now;
                    RoomOwnerProfileBO.Instance.Update(model);
                    message = "Update successfully.";
                }
                return Json(new { success = true, message = message });
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        [HttpPost]
        public IActionResult RoomOwnerProfileDelete(int id)
        {
            try
            {
                var msgRop = AdministrationDeleteGuards.GetDeleteRoomOwnerProfileBlockReason(id);
                if (msgRop != null)
                    return Json(new { success = false, message = msgRop });
                RoomOwnerProfileBO.Instance.Delete(id);
                return Json(new { success = true, message = "Delete successfully." });
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        #endregion

        #region ItemCategory/Priority
        [HttpGet]
        public IActionResult GetPriority(string code, string name, int inactive)
        {
            try
            {
                DataTable dataTable = _iAdministrationService.Priority(code, name, inactive);
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
        public IActionResult Priority()
        {
            return PartialView("ItemCategory/Priority");
        }
        [HttpPost]
        public IActionResult PrioritySave([FromBody] PriorityModel model)
        {
            string message = "";
            var listErrors = GetErrors(
                Check(model, "general", "Invalid data"),

                Check(model?.Code, "proT_code", "Code is not blank."),
                Check(model?.Name, "proT_name", "Name is not blank.")
            );

            if (listErrors.Count > 0)
            {
                return Json(new { success = false, errors = listErrors });
            }
            try
            {
                if (model.ID == 0)
                {
                    model.CreateDate = auditDateTime;
                    model.CreatedDate = auditDateTime;
                    model.UpdateDate = auditDateTime;
                    model.UpdatedDate = auditDateTime;

                    PriorityBO.Instance.Insert(model);
                    message = "Insert successfully!";
                }
                else
                {
                    var oldData = (PriorityModel)PriorityBO.Instance.FindByPrimaryKey(model.ID);

                    if (oldData != null)
                    {
                        model.UserInsertID = oldData.UserInsertID;
                        model.CreatedBy = oldData.CreatedBy;
                        model.CreateDate = oldData.CreatedDate;
                        model.CreatedDate = oldData.CreatedDate;
                    }

                    model.UpdateDate = auditDateTime;
                    model.UpdatedDate = auditDateTime;

                    PriorityBO.Instance.Update(model);
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
        public IActionResult DeletePriority(int id)
        {
            try
            {
                var msgPr = AdministrationDeleteGuards.GetDeletePriorityBlockReason(id);
                if (msgPr != null)
                    return Json(new { success = false, message = msgPr });
                PriorityBO.Instance.Delete(id);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, ex.Message });
            }

            return Json(new { success = true });
        }
        #endregion

        #region ItemCategory/Promotion
        [HttpGet]
        public IActionResult GetPromotion(string code, string name, int inactive)
        {
            try
            {
                DataTable dataTable = _iAdministrationService.Promotion(code, name, inactive);
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
        public IActionResult Promotion()
        {
            return PartialView("ItemCategory/Promotion");
        }
        [HttpPost]
        public IActionResult PromotionSave([FromBody] PromotionModel model)
        {
            string message = "";
            var listErrors = GetErrors(
                Check(model, "general", "Invalid data"),

                Check(model?.Code, "prom_code", "Code is not blank."),
                Check(model?.Name, "prom_name", "Name is not blank.")
            );

            if (listErrors.Count > 0)
            {
                return Json(new { success = false, errors = listErrors });
            }
            try
            {
                if (model.ID == 0)
                {
                    model.CreateDate = auditDateTime;
                    model.CreatedDate = auditDateTime;
                    model.UpdateDate = auditDateTime;
                    model.UpdatedDate = auditDateTime;

                    PromotionBO.Instance.Insert(model);
                    message = "Insert successfully!";
                }
                else
                {
                    var oldData = (PromotionModel)PromotionBO.Instance.FindByPrimaryKey(model.ID);

                    if (oldData != null)
                    {
                        model.UserInsertID = oldData.UserInsertID;
                        model.CreatedBy = oldData.CreatedBy;
                        model.CreateDate = oldData.CreatedDate;
                        model.CreatedDate = oldData.CreatedDate;
                    }

                    model.UpdateDate = auditDateTime;
                    model.UpdatedDate = auditDateTime;

                    PromotionBO.Instance.Update(model);
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
        public IActionResult DeletePromotion(int id)
        {
            try
            {
                var msgProm = AdministrationDeleteGuards.GetDeletePromotionBlockReason(id);
                if (msgProm != null)
                    return Json(new { success = false, message = msgProm });
                PromotionBO.Instance.Delete(id);
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

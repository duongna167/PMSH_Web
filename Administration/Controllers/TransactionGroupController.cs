using Administration.Services.Interfaces;
using BaseBusiness.BO;
using BaseBusiness.Model;
using BaseBusiness.util;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Administration.Controllers
{
    public class TransactionGroupController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<TransactionGroupController> _logger;
        private readonly IMemoryCache _cache;
        private readonly ITransactionGroupService _iTransactionGroupService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public TransactionGroupController(ILogger<TransactionGroupController> logger,
                IMemoryCache cache, IConfiguration configuration, IHttpContextAccessor httpContextAccessor, ITransactionGroupService iTransactionGroupService)
        {
            _cache = cache;
            _logger = logger;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _iTransactionGroupService = iTransactionGroupService;
        }

        public IActionResult TransGroup()
        {
            return PartialView();
        }
        [HttpGet]
        public IActionResult SearchTransactionGroup(string groupCode, string description)
        {
            try
            {
                var data = _iTransactionGroupService.SearchTransactionGroup(groupCode ?? "", description ?? "");

                var result = (from d in data.AsEnumerable()
                              select d.Table.Columns.Cast<DataColumn>()
                                  .ToDictionary(
                                      col => col.ColumnName,
                                      col => d[col.ColumnName]?.ToString()
                                  )).ToList();
                return Json(result);


            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }

        }

        [HttpPost]
        public ActionResult SaveTransactionGroup()
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();

                if (string.IsNullOrEmpty(Request.Form["code"].ToString()))
                {
                    return Json(new { code = 1, msg = "Group Code can not be blank" });
                }
                if (int.Parse(Request.Form["id"].ToString()) == 0)
                {
                    TransactionGroupModel transGroup = new TransactionGroupModel();
                    transGroup.Code = Request.Form["code"].ToString();
                    transGroup.GenerateID = 0;
                    transGroup.Description = Request.Form["description"].ToString();
                    transGroup.Type = int.Parse(Request.Form["type"].ToString());
                    transGroup.CreateDate = transGroup.UpdateDate = DateTime.Now;
                    transGroup.UserInsertID = transGroup.UserUpdateID = int.Parse(Request.Form["userID"].ToString());
                    long groupID = TransactionGroupBO.Instance.Insert(transGroup);
                    TransactionGroupModel model = (TransactionGroupModel)TransactionGroupBO.Instance.FindByPrimaryKey((int)groupID);
                    model.Seq = model.ID;
                    TransactionGroupBO.Instance.Update(model);
                    pt.CommitTransaction();
                    return Json(new { code = 0, msg = "Group transaction was created successfully" });
                }
                else
                {
                    TransactionGroupModel model = (TransactionGroupModel)TransactionGroupBO.Instance.FindByPrimaryKey(int.Parse(Request.Form["id"].ToString()));
                    List<TransactionGroupModel> folioDetail = PropertyUtils.ConvertToList<TransactionGroupModel>(TransactionGroupBO.Instance.
                        FindAll()).Where(x => x.Code == Request.Form["code"].ToString() && x.ID != int.Parse(Request.Form["id"].ToString())).ToList();
                    if (folioDetail.Count > 0)
                    {
                        return Json(new { code = 1, msg = "Code has exits" });
                    }
                    model.Code = Request.Form["code"].ToString();
                    model.Description = Request.Form["description"].ToString();
                    model.Type = int.Parse(Request.Form["type"].ToString());
                    model.UpdateDate = DateTime.Now;
                    model.UserUpdateID = int.Parse(Request.Form["userID"].ToString());
                    TransactionGroupBO.Instance.Update(model);
                    pt.CommitTransaction();
                    return Json(new { code = 0, msg = "Group transaction was updated successfully" });
                }


            }
            catch (Exception ex)
            {
                pt.RollBack();
                return Json(new { code = 1, msg = ex.Message });
            }
            finally
            {
                pt.CloseConnection();

            }
        }

        [HttpGet]
        public IActionResult GetTransactionGroupByID(int id)
        {
            try
            {
                TransactionGroupModel model = (TransactionGroupModel)TransactionGroupBO.Instance.FindByPrimaryKey((int)id);
                if (model == null || model.ID == 0)
                {
                    return Json(new TransactionGroupModel());
                }
                return Json(model);


            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }

        }

        [HttpPost]
        public ActionResult DeleteTransactionGroup(int id)
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();

                TransactionGroupModel model = (TransactionGroupModel)TransactionGroupBO.Instance.FindByPrimaryKey((int)id);
                if (model == null || model.ID == 0)
                {
                    return Json(new { code = 1, msg = "Group transaction not found" });
                }
                List<TransactionSubGroupModel> subGroup = PropertyUtils.ConvertToList<TransactionSubGroupModel>(TransactionSubGroupBO.Instance.FindByAttribute("TransactionGroupID", id));
                if (subGroup.Count > 0)
                {
                    return Json(new { code = 1, msg = "Can not delete this transaction group, exits at 1 least transation sub group" });
                }
                List<TransactionsModel> tran = PropertyUtils.ConvertToList<TransactionsModel>(TransactionsBO.Instance.FindByAttribute("TransactionGroupID", id));
                if (tran.Count > 0)
                {
                    return Json(new { code = 1, msg = "Can not delete this transaction group, exits at 1 least transation" });
                }
                TransactionGroupBO.Instance.Delete(id);


                pt.CommitTransaction();
                return Json(new { code = 0, msg = "Transaction Group was deleted successfully" });


            }
            catch (Exception ex)
            {
                pt.RollBack();
                return Json(new { code = 1, msg = ex.Message });
            }
            finally
            {
                pt.CloseConnection();

            }
        }
    }
}

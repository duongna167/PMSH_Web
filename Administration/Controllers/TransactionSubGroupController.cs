using Administration.Services.Implements;
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
    public class TransactionSubGroupController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<TransactionSubGroupController> _logger;
        private readonly IMemoryCache _cache;
        private readonly ITransactionSubGroupService _iTransactionSubGroupService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public TransactionSubGroupController(ILogger<TransactionSubGroupController> logger,
                IMemoryCache cache, IConfiguration configuration, IHttpContextAccessor httpContextAccessor, ITransactionSubGroupService iTransactionSubGroupService)
        {
            _cache = cache;
            _logger = logger;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _iTransactionSubGroupService = iTransactionSubGroupService;
        }
        public IActionResult TransSubGroup()
        {
            return PartialView();
        }
        [HttpGet]
        public IActionResult SearchTransactionSubGroup(string groupCode, string description, int groupID)
        {
            try
            {
                var data = _iTransactionSubGroupService.SearchTransactionSubGroup(groupCode ?? "", description ?? "", groupID);

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
        public ActionResult SaveTransactionSubGroup()
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
                    TransactionSubGroupModel transGroup = new TransactionSubGroupModel();
                    transGroup.Code = Request.Form["code"].ToString();
                    transGroup.GenerateID = 0;
                    transGroup.Description = Request.Form["description"].ToString();
                    transGroup.TransactionGroupID = int.Parse(Request.Form["groupID"].ToString());
                    transGroup.CreateDate = transGroup.UpdateDate = DateTime.Now;
                    transGroup.UserInsertID = transGroup.UserUpdateID = int.Parse(Request.Form["userID"].ToString());
                    long groupID = TransactionSubGroupBO.Instance.Insert(transGroup);
                    TransactionSubGroupModel model = (TransactionSubGroupModel)TransactionSubGroupBO.Instance.FindByPrimaryKey((int)groupID);
                    model.Seq = model.ID;
                    TransactionSubGroupBO.Instance.Update(model);
                    pt.CommitTransaction();
                    return Json(new { code = 0, msg = "Transaction sub group was created successfully" });
                }
                else
                {
                    TransactionSubGroupModel model = (TransactionSubGroupModel)TransactionSubGroupBO.Instance.FindByPrimaryKey(int.Parse(Request.Form["id"].ToString()));
                    List<TransactionSubGroupModel> folioDetail = PropertyUtils.ConvertToList<TransactionSubGroupModel>(TransactionSubGroupBO.Instance.
                        FindAll()).Where(x => x.Code == Request.Form["code"].ToString() && x.ID != int.Parse(Request.Form["id"].ToString())).ToList();
                    if (folioDetail.Count > 0)
                    {
                        return Json(new { code = 1, msg = "Code has exits" });
                    }
                    model.Code = Request.Form["code"].ToString();
                    model.Description = Request.Form["description"].ToString();
                    model.TransactionGroupID = int.Parse(Request.Form["groupID"].ToString());
                    model.UpdateDate = DateTime.Now;
                    model.UserUpdateID = int.Parse(Request.Form["userID"].ToString());
                    TransactionGroupBO.Instance.Update(model);
                    pt.CommitTransaction();
                    return Json(new { code = 0, msg = "Transaction sub group was updated successfully" });
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
        public IActionResult GetTransactionSubGroupByID(int id)
        {
            try
            {
                TransactionSubGroupModel model = (TransactionSubGroupModel)TransactionSubGroupBO.Instance.FindByPrimaryKey((int)id);
                if (model == null || model.ID == 0)
                {
                    return Json(new TransactionSubGroupModel());
                }
                return Json(model);


            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }

        }


        [HttpPost]
        public ActionResult DeleteTransactionSubGroup(int id)
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();

                TransactionSubGroupModel model = (TransactionSubGroupModel)TransactionSubGroupBO.Instance.FindByPrimaryKey((int)id);
                if (model == null || model.ID == 0)
                {
                    return Json(new { code = 1, msg = "Transaction Sub Group not found" });
                }

                List<TransactionsModel> tran = PropertyUtils.ConvertToList<TransactionsModel>(TransactionsBO.Instance.FindByAttribute("TransactionSubGroupID", id));
                if (tran.Count > 0)
                {
                    return Json(new { code = 1, msg = "Can not delete this transaction sub group, exits at 1 least transation" });
                }
                TransactionGroupBO.Instance.Delete(id);


                pt.CommitTransaction();
                return Json(new { code = 0, msg = "Transaction Sub Group was deleted successfully" });


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

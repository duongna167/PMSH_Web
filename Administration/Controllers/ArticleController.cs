using Administration.Services.Implements;
using Administration.Services.Interfaces;
using BaseBusiness.BO;
using BaseBusiness.Model;
using BaseBusiness.util;
using DocumentFormat.OpenXml.EMMA;
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
    public class ArticleController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ArticleController> _logger;
        private readonly IMemoryCache _cache;
        private readonly IArticleService _iArticleService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public ArticleController(ILogger<ArticleController> logger,
                IMemoryCache cache, IConfiguration configuration, IHttpContextAccessor httpContextAccessor, IArticleService iArticleService)
        {
            _cache = cache;
            _logger = logger;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _iArticleService = iArticleService;
        }
        #region Article 
        public IActionResult Search()
        {
            return View();
        }
        public IActionResult Article()
        {
            List<TransactionsModel> listTransac = PropertyUtils.ConvertToList<TransactionsModel>(TransactionsBO.Instance.FindAll());
            ViewBag.TransactionsList = listTransac;

            List<CurrencyModel> listCurr = PropertyUtils.ConvertToList<CurrencyModel>(CurrencyBO.Instance.FindAll());
            ViewBag.CurrencyList = listCurr;
            return PartialView();
        }
        [HttpGet]
        public IActionResult SearchArticle(string tranCode, string articleCode, string articleDescription, string articleSupplement)
        {
            try
            {
                var data = _iArticleService.SearchArticle(tranCode ?? "", articleCode ?? "", articleDescription ?? "", articleSupplement ?? "");

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
        public IActionResult ArticleListSave(int id, string codenew, decimal dfprice, string descriptionnew, string transactionsListnew, string currList, string supplementNew, string user, int userID, int isActive)
        {
            ProcessTransactions pt = new ProcessTransactions();
            pt.OpenConnection();
            pt.BeginTransaction();

            user = user?.Replace("\"", "").Trim();
            List<BusinessDateModel> businessDateModel = PropertyUtils.ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll());
            var businessDate = businessDateModel[0].BusinessDate;
            try
            {
                bool isNew = (id == 0);

                ArticleModel model;
                if (isNew)
                {
                    model = new ArticleModel
                    {
                        Code = codenew?.Trim(),
                        Description = descriptionnew?.Trim(),
                        IsActive = (isActive == 1),
                        DefaultPrice = dfprice,
                        TransactionCode = transactionsListnew,
                        CurrencyID = currList,
                        CreateDate = businessDate,
                        UpdateDate = businessDate,
                        UserInsertID = userID,
                        UserUpdateID = 0,
                        Supplement = supplementNew
                    };

                    ArticleBO.Instance.Insert(model);
                }
                else
                {
                    model = (ArticleModel)ArticleBO.Instance.FindByPrimaryKey(id);
                    if (model == null)
                    {
                        throw new Exception($"Không tìm thấy Article có ID = {id}");
                    }

                    model.Code = codenew;
                    model.Description = descriptionnew;

                    model.DefaultPrice = dfprice;
                    model.CurrencyID = currList;

                    model.TransactionCode = transactionsListnew;
                    model.Supplement = supplementNew;
                    model.UpdateDate = businessDate;
                    model.UserUpdateID = userID;

                    ArticleBO.Instance.Update(model);
                    #region Update thông tin bến bảng RestaurantClassArticleLnk
                    model.Description = model.Description.Replace("'", "`");
                    pt.UpdateCommand("Update RestaurantClassArticleLnk set ArticleDescription=N'" + model.Description + "' where ArticleCode= N'" + model.Code + "' ");
                    #endregion
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
        public IActionResult ArticleListDelete(int id)
        {
            try
            {

                ArticleBO.Instance.Delete(id);

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

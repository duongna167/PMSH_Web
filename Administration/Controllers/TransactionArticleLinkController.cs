using Administration.Services.Interfaces;
using BaseBusiness.BO;
using BaseBusiness.Model;
using BaseBusiness.util;
using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

using System.Data;


namespace Administration.Controllers
{
    [Route("/Administration/Transaction")]
    public class TransactionArticleLinkController : Controller
    {
        private readonly ITransactionArticleLinkService _serviceTransactionArticleLink;
        public TransactionArticleLinkController(ITransactionArticleLinkService transactionArticleLink)
        {
            _serviceTransactionArticleLink = transactionArticleLink;
        }
        [HttpGet("TransactionArticleLink")]
        public IActionResult TransactionArticleLink()

        {
            List<TransactionsModel> listTracsaction = PropertyUtils.ConvertToList<TransactionsModel>(TransactionsBO.Instance.FindAll());
            List<ArticleModel> listArticle = PropertyUtils.ConvertToList<ArticleModel>(ArticleBO.Instance.FindAll());

            ViewBag.TransactionList = listTracsaction;
            ViewBag.ArticleList = listArticle;

            return PartialView("~/Views/Administration/TransactionArticleLink.cshtml");
        }
        [HttpGet("GetAllTransactionArticleLink")]
        public IActionResult GetAllTransactionArticleLink(
            string? transCode,
            string? articleCode,
            string? transDes,
            string? articleDes,
            int skip = 0,
            int take = 50,
            string? sort = null,
            string? group = null
        )
        {
            var dt = _serviceTransactionArticleLink.SearchTypeData(
                transCode,
                articleCode,
                transDes,
                articleDes,
                sort,
                group,
                skip,
                take
            );

            var list = dt.AsEnumerable().Select(d => new
            {
                id = d.Field<int>("ID"),
                infor = d["Infor"]?.ToString(),
                transactionCode = d["TransactionCode"]?.ToString(),
                transactionDescription = d["TransactionDescription"]?.ToString(),
                articleCode = d["ArticleCode"]?.ToString(),
                articleDescription = d["ArticleDescription"]?.ToString()
            }).ToList();

            if (!string.IsNullOrEmpty(group))
            {
                var grouped = list
                    .GroupBy(x => x.transactionCode)
                    .Select(g => new
                    {
                        key = g.Key,
                        items = g.ToList(),
                        count = g.Count()
                    });

                return Json(new
                {
                    data = grouped,
                    totalCount = list.Count
                });
            }
            return Json(new
            {
                data = list,
                totalCount = list.Count // hoặc query COUNT riêng
            });
        }

        [HttpPost("InsertTAL")]
        public IActionResult InsertTAL([FromBody] TransactionArticleLinkDTO dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Payload is null"
                    });
                }
                var errors = new List<object>();

                if (dto.SelectedTransactions.Count == 0)
                {
                    errors.Add(new { field = "selectedTransactions", message = "Transaction Code not null." });
                }
                if (dto.SelectedArticles.Count == 0)
                {
                    errors.Add(new { field = "selectedArticles", message = "Article Code not null." });
                }

                //Business validate
                foreach (var item in dto.SelectedTransactions)
                {
                    var transList = TransactionsBO.Instance.FindByAttribute("Code", item);
                    if (transList == null || transList.Count == 0)
                        errors.Add(new { field = "selectedTransactions", message = $"Invalid Transaction Code: {item}." });
                }
                foreach (var item in dto.SelectedArticles)
                {
                    var articleList = ArticleBO.Instance.FindByAttribute("Code", item);
                    if (articleList == null || articleList.Count == 0)
                        errors.Add(new { field = "selectedArticles", message = $"Invalid Article Code: {item}." });
                }

                // ===== RETURN IF ERROR =====
                if (errors.Count != 0)
                {
                    return Json(new { success = false, message = "Validation failed.", errors });
                }

                int count = _serviceTransactionArticleLink.InsertTAL(dto.SelectedTransactions, dto.SelectedArticles);
                return Ok(new
                {
                    success = true,
                    message = "Success",
                    data = new { count }
                });
            }
            catch (KeyNotFoundException ex)
            {
                return Json(new
                {
                    success = false,
                    errors = ex.Message
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    errors = ex.Message,
                    innerException = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                });
            }

        }
        [HttpPost("UpdateTAL")]
        public IActionResult UpdateTAL([FromBody] TransactionArticleLinkDTO dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Payload is null"
                    });
                }
                var errors = new List<object>();
                if (string.IsNullOrWhiteSpace(dto.TransactionEdit))
                    errors.Add(new { field = "editTransactions", message = "Transaction Code is required." });
                if (string.IsNullOrWhiteSpace(dto.ArticleEdit))
                    errors.Add(new { field = "editArticles", message = "Article Code is required." });

                if (dto.ID <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Transaction Article Link ID is required for update."
                    });
                }
                // Business validate

                var entity = TransactionArticleLnkBO.Instance.FindByPrimaryKey(dto.ID);
                if (entity == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Transaction Article Link not found."
                    });
                }

                if (!string.IsNullOrWhiteSpace(dto.TransactionEdit))
                {
                    var transList = TransactionsBO.Instance.FindByAttribute("Code", dto.TransactionEdit);
                    if (transList == null || transList.Count == 0)
                        errors.Add(new { field = "editTransactions", message = "Invalid Transaction Code." });
                }

                if (!string.IsNullOrWhiteSpace(dto.ArticleEdit))
                {
                    var articleList = ArticleBO.Instance.FindByAttribute("Code", dto.ArticleEdit);
                    if (articleList == null || articleList.Count == 0)
                        errors.Add(new { field = "editTransactions", message = "Invalid Article Code." });
                }


                // ===== RETURN IF ERROR =====
                if (errors.Count != 0)
                {
                    return Json(new { success = false, message = "Validation failed.", errors });
                }

                int IDUP = _serviceTransactionArticleLink.UpdateTAL(dto.ID, dto.TransactionEdit, dto.ArticleEdit);
                return Ok(new
                {
                    success = true,
                    message = "Success",
                    data = new { dto.ID }
                });
            }
            catch (KeyNotFoundException ex)
            {
                return Json(new
                {
                    success = false,
                    errors = ex.Message
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    errors = ex.Message,
                    innerException = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                });
            }

        }

        public class TransactionArticleLinkDTO
        {
            public List<string> SelectedArticles { get; set; } = [];
            public List<string> SelectedTransactions { get; set; } = [];
            public int ID { get; set; } = 0;

            public string TransactionEdit { get; set; } = string.Empty;
            public string ArticleEdit { get; set; } = string.Empty;
        }
    }
}

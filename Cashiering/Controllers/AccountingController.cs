using BaseBusiness.BO;
using BaseBusiness.Model;
using BaseBusiness.util;
using Cashiering.Commons.Helpers;
using Cashiering.Dto;
using Cashiering.Services.Implements;
using Cashiering.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static DevExpress.Utils.Filtering.ExcelFilterOptions;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace Cashiering.Controllers
{
    public class AccountingController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccountingController> _logger;
        private readonly IMemoryCache _cache;
        private readonly IAccountingService _iAccountingService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public AccountingController(ILogger<AccountingController> logger,
                IMemoryCache cache, IConfiguration configuration, IAccountingService iAccountingService)
        {
            _cache = cache;
            _logger = logger;
            _configuration = configuration;
            _iAccountingService = iAccountingService;
        }

        #region DatVp __ Accounting: Search
        public IActionResult AccountingSearch()
        {
            ViewBag.cboAccountType = ListItemHelper.GetARAccountType();
            ViewBag.cboCountry = ListItemHelper.GetCountry();
            ViewBag.cboCity = ListItemHelper.GetCity();
            ViewBag.cboCity2 = ListItemHelper.GetCityText();

            return PartialView(); // View này sẽ chứa DataGrid + script gọi API
        }


        [HttpGet]
        public IActionResult SearchAccounting(string accountName, string accountNo, int accountType, string balance)
        {
            try
            {

                DataTable resultExchangeData = _iAccountingService.AccountSearch(accountName, accountNo, accountType, balance);
                var resultExchange = (from d in resultExchangeData.AsEnumerable()
                                      select d.Table.Columns.Cast<DataColumn>()
                                          //.Where(col => col.ColumnName != "AllotmentStageID" && col.ColumnName != "flag" && col.ColumnName != "Total")
                                          .ToDictionary(
                col => col.ColumnName,
                col =>
                {
                    var value = d[col.ColumnName];
                    if (value == DBNull.Value) return null;

                    // CreatedDate: KHÔNG ToString
                    if (col.ColumnName == "CreatedDate" || col.ColumnName == "UpdatedDate")
                        return value;

                    // Các field khác: ToString
                    return value.ToString();
                }
            )
                                          ).ToList();
                return Json(resultExchange);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        #endregion

        #region DatVP __ Accounting: Add
        [HttpPost]
        public ActionResult AccountReceivableAdd(SaveARAccountRequestDto dto)
        {
            // Gán UserName từ session hoặc context nếu DTO chưa có
            if (string.IsNullOrEmpty(dto.UserName))
            {
                return Json(new { Success = true, Message = "Username not found." });
            }

            var result = _iAccountingService.SaveARAccount(dto);

            if (result.Success)
                return Json(new { Success = true, Message = result.Message });
            else
                return Json(new { Success = false, Message = result.Message, errors = result.Errors });
        }

        #endregion

        #region DatVP __ Accounting: Maintaince
        [HttpGet]
        public async Task<IActionResult> GetAccountReceivable(int id)
        {
            try
            {

                ARAccountReceivableModel model = (ARAccountReceivableModel)ARAccountReceivableBO.Instance.FindByPrimaryKey(id);
                return Json(model);
            }
            catch (Exception ex)
            {
                return Json(new ARAccountTypeModel());
            }
        }

        [HttpGet]
        public async Task<IActionResult> SearchMaintenance(string dateCheck, int arID, string folioNo, string isActive, string paymentOnly, string print, DateTime fromDate, DateTime toDate)
        {
            try
            {

                var data = _iAccountingService.AccountMaintence(dateCheck ?? "", arID, folioNo ?? "", isActive ?? "", paymentOnly ?? "", print ?? "", fromDate, toDate);
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


        #endregion

        #region DatVP __ Inovice: Common infor
        [HttpGet]
        public async Task<IActionResult> SearchInfoInvoice(int folioID, int arID)
        {
            try
            {
                string sqlCommand = "";
                sqlCommand = $"SELECT ConfirmationNo, AccountName, ReservationID, ProfileID, FolioNo, Status FROM Folio WITH (NOLOCK) WHERE ID IN ({folioID}) ";
                var data = _iAccountingService.SearchByCommmand(sqlCommand);
                var result = (from d in data.AsEnumerable()
                              select d.Table.Columns.Cast<DataColumn>()
                                  .ToDictionary(
                                      col => col.ColumnName,
                                      col => d[col.ColumnName]?.ToString()
                                  )).ToList();

                FolioModel folio = (FolioModel)FolioBO.Instance.FindByPrimaryKey(folioID);
                if (folio == null || folio.ID == 0)
                {
                    return Json(new
                    {
                        code = 1,
                        msg = "Could not find folio"
                    });
                }
                sqlCommand = $"SELECT ConfirmationNo, ArrivalDate, DepartureDate, LastName, RoomID, RoomNo FROM dbo.Reservation WITH (NOLOCK) WHERE ID = {folio.ReservationID}";
                var data2 = _iAccountingService.SearchByCommmand(sqlCommand);
                var result2 = (from d in data2.AsEnumerable()
                               select d.Table.Columns.Cast<DataColumn>()
                                   .ToDictionary(
                                        col => col.ColumnName,
                                        col =>
                                        {
                                            var value = d[col.ColumnName];
                                            if (value == DBNull.Value) return null;

                                            // CreatedDate: KHÔNG ToString
                                            if (col.ColumnName == "ArrivalDate" || col.ColumnName == "DepartureDate")
                                                return value;

                                            // Các field khác: ToString
                                            return value.ToString();
                                        }
                                    )).ToList();

                sqlCommand = $"SELECT AccountName,AccountNo FROM dbo.ARAccountReceivable WITH (NOLOCK) WHERE ID = {arID}";
                var data3 = _iAccountingService.SearchByCommmand(sqlCommand);
                var result3 = (from d in data3.AsEnumerable()
                               select d.Table.Columns.Cast<DataColumn>()
                                   .ToDictionary(
                                       col => col.ColumnName,
                                       col => d[col.ColumnName]?.ToString()
                                   )).ToList();
                return Json(new
                {
                    code = 0,
                    result1 = result,
                    result2 = result2,
                    result3 = result3
                });

            }
            catch (Exception ex)
            {
                return Json(new
                {
                    code = 1,
                    msg = ex.Message
                });
            }
        }


        #endregion

        #region DatVP __ Inovice: Search
        [HttpGet]
        public async Task<IActionResult> InvoiceSearch(int folioID)
        {
            try
            {

                var data = _iAccountingService.InvoiceSearch(folioID, 0);
                var result = (from d in data.AsEnumerable()
                              select d.Table.Columns.Cast<DataColumn>()
                              .ToDictionary(
                                    col => col.ColumnName,
                                    col =>
                                    {
                                        var value = d[col.ColumnName];
                                        if (value == DBNull.Value) return null;

                                        // CreatedDate: KHÔNG ToString
                                        if (col.ColumnName == "Date" || col.ColumnName == "Time")
                                            return value;

                                        // Các field khác: ToString
                                        return value.ToString();
                                    }
                                )).ToList();

                return Json(new
                {
                    result1 = result,

                });

            }
            catch (Exception ex)
            {
                return Json(new
                {

                    msg = ex.Message
                });
            }
        }


        #endregion

        #region DatVP __ Invoice: Transfer
        [HttpGet]
        public async Task<IActionResult> SearchARInfo(string accountName, string accountNo, string folioNo, string isActive, string folioID)
        {
            try
            {

                var data = _iAccountingService.SearchInfoAR(accountName ?? "", accountNo ?? "", folioNo ?? "", isActive ?? "0", folioID);
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
                return Json(new
                {

                    msg = ex.Message
                });
            }
        }
        #endregion

        #region DatVP __ Invoice: Posting
        [HttpPost]
        public ActionResult PostArticle()
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();
                List<BusinessDateModel> businessDateModel = PropertyUtils.ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll());

                int postType = int.Parse(Request.Form["postType"].ToString());
                string listItemJson = Request.Form["listItem"];

                if (string.IsNullOrEmpty(listItemJson))
                {
                    return Json(new { code = 1, msg = "Could not find Transaction!" });
                }
                var itemList = JsonSerializer.Deserialize<List<ItemPost>>(listItemJson);
                if (itemList.Count < 1)
                {
                    return Json(new { code = 1, msg = "Could not find Transaction!" });

                }


                // tìm invoice lớn nhất 
                string invoiceNo = (FolioDetailBO.GetTopInvoiceNo() + 1).ToString();
                int shiftID = int.Parse(Request.Form["shiftID"].ToString());
                string shiftName = Request.Form["shiftName"].ToString();
                foreach (var itemTrans in itemList)
                {
                    string transactionNo = (FolioDetailBO.GetTopTransactioNo()).ToString();

                    string tranCode = itemTrans.transCode;
                    if (string.IsNullOrEmpty(tranCode))
                    {
                        return Json(new { code = 1, msg = "Please choose Transaction/Article!" });

                    }
                    List<TransactionsModel> trans = PropertyUtils.ConvertToList<TransactionsModel>(TransactionsBO.Instance.FindByAttribute("Code", tranCode));
                    if (trans.Count < 1)
                    {
                        return Json(new { code = 1, msg = "Could not find Transaction!" });

                    }


                    // tìm folio của reservation
                    FolioModel folio = (FolioModel)FolioBO.Instance.FindByPrimaryKey(int.Parse(Request.Form["folioID"].ToString()));

                    if (folio == null || folio.ID == 0)
                    {
                        return Json(new { code = 1, msg = $"Could not find Folio. Please check Folio" });

                    }

                    if (folio.Status == true)
                    {
                        return Json(new { code = 1, msg = $"Can not post. Folio has been being locked" });

                    }
                    ReservationModel reservation = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(folio.ReservationID);
                    if (reservation == null || reservation.ID == 0)
                    {
                        return Json(new { code = 1, msg = $"Could not find reservation. Please check reservation" });

                    }
                    #region lưu transaction chính vào folio detail
                    // kiểm tra xem transaction chọn để post có article không
                    string articleCode = itemTrans.articleCode;
                    FolioDetailModel folioArticle = new FolioDetailModel();
                    folioArticle.UserID = int.Parse(Request.Form["userID"].ToString());
                    folioArticle.ShiftID = shiftID;
                    folioArticle.UserName = Request.Form["userID"].ToString();
                    folioArticle.CashierNo = shiftName;
                    folioArticle.ReservationID = folioArticle.OriginReservationID = folio.ReservationID;
                    folioArticle.FolioID = folioArticle.OriginFolioID = folio.ID;
                    folioArticle.InvoiceNo = invoiceNo;
                    folioArticle.TransactionNo = transactionNo;
                    folioArticle.ReceiptNo = "";
                    folioArticle.TransactionDate = businessDateModel[0].BusinessDate;
                    folioArticle.ProfitCenterID = 2;
                    folioArticle.ProfitCenterCode = "0";
                    folioArticle.TransactionGroupID = trans[0].TransactionGroupID;
                    folioArticle.TransactionSubgroupID = trans[0].TransactionSubGroupID;
                    folioArticle.GroupCode = trans[0].GroupCode;
                    folioArticle.SubgroupCode = trans[0].SubgroupCode;
                    folioArticle.GroupType = trans[0].GroupType;
                    folioArticle.TransactionCode = tranCode;
                    if (!string.IsNullOrEmpty(articleCode))
                    {
                        folioArticle.ArticleCode = articleCode;
                        string articleName = !string.IsNullOrEmpty(itemTrans.articleName) ? itemTrans.articleName : string.Empty;
                        folioArticle.Reference = $"A[{articleCode}]-{articleName}";
                    }
                    else
                    {
                        folioArticle.ArticleCode = "";
                    }
                    if (!string.IsNullOrEmpty(Request.Form["referencePost"].ToString()))
                    {
                        folioArticle.Reference = Request.Form["referencePost"].ToString();

                    }
                    folioArticle.Status = false;
                    if (postType == 1)
                    {
                        folioArticle.RowState = 1;
                        folioArticle.PostType = 2;
                    }
                    else
                    {
                        folioArticle.RowState = 2;
                        folioArticle.PostType = 3;
                    }
                    folioArticle.IsSplit = true;
                    folioArticle.Quantity = int.Parse(!string.IsNullOrEmpty(itemTrans.quantity) ? itemTrans.quantity : "0");
                    folioArticle.Price = decimal.Parse(!string.IsNullOrEmpty(itemTrans.priceNet) ? itemTrans.priceNet : "0");
                    folioArticle.Amount = decimal.Parse(!string.IsNullOrEmpty(itemTrans.amountNet) ? itemTrans.amountNet : "0");
                    folioArticle.CurrencyID = folioArticle.CurrencyMaster = "VND";
                    folioArticle.AmountMaster = decimal.Parse(!string.IsNullOrEmpty(itemTrans.amountNet) ? itemTrans.amountNet : "0");
                    folioArticle.Description = trans[0].Description;
                    folioArticle.AmountBeforeTax = folioArticle.AmountMasterBeforeTax = decimal.Parse(!string.IsNullOrEmpty(itemTrans.amount) ? itemTrans.amount : "0");
                    folioArticle.AmountGross = folioArticle.AmountMasterGross = decimal.Parse(!string.IsNullOrEmpty(itemTrans.amountNet) ? itemTrans.amountNet : "0"); ;
                    folioArticle.RoomType = "";
                    folioArticle.RoomTypeID = 0;
                    folioArticle.UserInsertID = folioArticle.UserUpdateID = int.Parse(Request.Form["userID"].ToString());
                    folioArticle.CreateDate = folioArticle.UpdateDate = DateTime.Now;
                    folioArticle.RoomID = reservation.RoomId;
                    folioArticle.Property = folioArticle.CheckNo = folioArticle.OriginARNo = "";
                    folioArticle.IsPostedAR = false;
                    folioArticle.ARTransID = 0;
                    folioArticle.IsTransfer = false;
                    FolioDetailBO.Instance.Insert(folioArticle);
                    #endregion

                    #region lưu transaction từ generate transaction và folio detail
                    List<GenerateTransactionModel> generateTransaction = PropertyUtils.ConvertToList<GenerateTransactionModel>(GenerateTransactionBO.Instance.FindByAttribute("TransactionCode", tranCode));
                    if (generateTransaction.Count > 0)
                    {
                        bool isVat = false;
                        bool isSvc = false;
                        int indexVat = -1;
                        int indexSvc = -1;
                        // Kiểm tra xem generate transaction có Tax không
                        for (int i = 0; i < generateTransaction.Count; i++)
                        {
                            if (generateTransaction[i].GroupCode == "Tax" && generateTransaction[i].SubgroupCode == "Tax")
                            {
                                isVat = true;
                                indexVat = i;
                                break;
                            }
                        }
                        // Kiểm tra xem generate transaction có Svc không
                        for (int i = 0; i < generateTransaction.Count; i++)
                        {
                            if (generateTransaction[i].GroupCode == "Tax" && generateTransaction[i].SubgroupCode == "SVC")
                            {
                                isSvc = true;
                                indexSvc = i;
                                break;
                            }
                        }
                        foreach (var item in generateTransaction)
                        {
                            if (item.GroupCode == "Tax" && item.SubgroupCode == "Tax")
                            {
                                FolioDetailModel folioSub = new FolioDetailModel();
                                folioSub.UserID = int.Parse(Request.Form["userID"].ToString());
                                folioSub.ShiftID = shiftID;
                                folioSub.UserName = Request.Form["userID"].ToString();
                                folioSub.CashierNo = shiftName;
                                folioSub.ReservationID = folioSub.OriginReservationID = folio.ReservationID;
                                folioSub.FolioID = folioSub.OriginFolioID = folio.ID;
                                folioSub.InvoiceNo = invoiceNo;
                                folioSub.TransactionNo = transactionNo;
                                folioSub.ReceiptNo = "";
                                folioSub.TransactionDate = businessDateModel[0].BusinessDate;
                                folioSub.ProfitCenterID = 2;
                                folioSub.ProfitCenterCode = "0";
                                folioSub.TransactionGroupID = item.TransactionGroupID;
                                folioSub.TransactionSubgroupID = item.TransactionSubGroupID;
                                folioSub.GroupCode = item.GroupCode;
                                folioSub.SubgroupCode = item.SubgroupCode;
                                folioSub.GroupType = item.GroupType;
                                folioSub.TransactionCode = item.TransactionCodeDetail;
                                folioSub.ArticleCode = "";
                                folioSub.Status = false;
                                if (postType == 1)
                                {
                                    folioSub.RowState = 2;
                                    folioSub.PostType = 2;
                                }
                                else
                                {
                                    folioSub.RowState = 3;
                                    folioSub.PostType = 3;
                                }
                                folioSub.IsSplit = false;
                                folioSub.Quantity = int.Parse(!string.IsNullOrEmpty(itemTrans.quantity) ? itemTrans.quantity : "0"); ;
                                if (item.GroupCode == "Tax" && item.GroupCode == "Tax")
                                {
                                    folioSub.Price = decimal.Parse(!string.IsNullOrEmpty(itemTrans.priceNet) ? itemTrans.priceNet : "0") * (item.Percentage / 100) / (1 + (item.Percentage / 100));
                                }
                                folioSub.Amount = folioSub.AmountMaster = folioSub.AmountBeforeTax = folioSub.AmountMasterBeforeTax = folioSub.AmountGross = folioSub.AmountMasterGross = folioSub.Price * folioSub.Quantity;
                                folioSub.CurrencyID = folioSub.CurrencyMaster = "VND";
                                folioSub.Description = item.Description;
                                folioSub.Reference = "";
                                folioSub.RoomType = "";
                                folioSub.RoomTypeID = 0;
                                folioSub.UserInsertID = folioSub.UserUpdateID = int.Parse(Request.Form["userID"].ToString());
                                folioSub.CreateDate = folioSub.UpdateDate = DateTime.Now;
                                folioSub.RoomID = reservation.RoomId;
                                folioSub.Property = folioSub.CheckNo = folioSub.OriginARNo = "";
                                folioSub.IsPostedAR = false;
                                folioSub.ARTransID = 0;
                                folioSub.IsTransfer = false;
                                FolioDetailBO.Instance.Insert(folioSub);
                            }

                            else if (item.GroupCode == "Tax" && item.SubgroupCode == "SVC")
                            {
                                decimal priceVat = 0;
                                if (isVat == true)
                                {
                                    decimal percent = generateTransaction.Where(x => x.GroupCode == "Tax" && x.SubgroupCode == "Tax").FirstOrDefault().Percentage;
                                    priceVat = decimal.Parse(!string.IsNullOrEmpty(itemTrans.priceNet) ? itemTrans.priceNet : "0") * (percent / 100) / (1 + (percent / 100));

                                }
                                FolioDetailModel folioSub = new FolioDetailModel();
                                folioSub.UserID = int.Parse(Request.Form["userID"].ToString());
                                folioSub.ShiftID = shiftID;
                                folioSub.UserName = Request.Form["userID"].ToString();
                                folioSub.CashierNo = shiftName;
                                folioSub.ReservationID = folioSub.OriginReservationID = folio.ReservationID;
                                folioSub.FolioID = folioSub.OriginFolioID = folio.ID;
                                folioSub.InvoiceNo = invoiceNo;
                                folioSub.TransactionNo = transactionNo;
                                folioSub.ReceiptNo = "";
                                folioSub.TransactionDate = businessDateModel[0].BusinessDate;
                                folioSub.ProfitCenterID = 2;
                                folioSub.ProfitCenterCode = "0";
                                folioSub.TransactionGroupID = item.TransactionGroupID;
                                folioSub.TransactionSubgroupID = item.TransactionSubGroupID;
                                folioSub.GroupCode = item.GroupCode;
                                folioSub.SubgroupCode = item.SubgroupCode;
                                folioSub.GroupType = item.GroupType;
                                folioSub.TransactionCode = item.TransactionCodeDetail;
                                folioSub.ArticleCode = "";
                                folioSub.Status = false;
                                if (postType == 1)
                                {
                                    folioSub.RowState = 2;
                                    folioSub.PostType = 2;
                                }
                                else
                                {
                                    folioSub.RowState = 3;
                                    folioSub.PostType = 3;
                                }
                                folioSub.IsSplit = false;
                                folioSub.Quantity = int.Parse(!string.IsNullOrEmpty(itemTrans.quantity) ? itemTrans.quantity : "0");
                                if (item.GroupCode == "Tax" && item.GroupCode == "Tax")
                                {
                                    folioSub.Price = (decimal.Parse(!string.IsNullOrEmpty(itemTrans.priceNet) ? itemTrans.priceNet : "0") - priceVat) * (item.Percentage / 100) / (1 + (item.Percentage / 100));
                                }
                                folioSub.Amount = folioSub.AmountMaster = folioSub.AmountBeforeTax = folioSub.AmountMasterBeforeTax = folioSub.AmountGross = folioSub.AmountMasterGross = folioSub.Price * folioSub.Quantity;
                                folioSub.CurrencyID = folioSub.CurrencyMaster = "VND";
                                folioSub.Description = item.Description;
                                folioSub.Reference = "";
                                folioSub.RoomType = "";
                                folioSub.RoomTypeID = 0;
                                folioSub.UserInsertID = folioSub.UserUpdateID = int.Parse(Request.Form["userID"].ToString());
                                folioSub.CreateDate = folioSub.UpdateDate = DateTime.Now;
                                folioSub.RoomID = reservation.RoomId;
                                folioSub.Property = folioSub.CheckNo = folioSub.OriginARNo = "";
                                folioSub.IsPostedAR = false;
                                folioSub.ARTransID = 0;
                                folioSub.IsTransfer = false;
                                FolioDetailBO.Instance.Insert(folioSub);
                            }

                            else
                            {
                                decimal priceVat = 0;
                                decimal priceSvc = 0;
                                if (isVat == true)
                                {
                                    decimal percent = generateTransaction[indexVat].Percentage;
                                    priceVat = decimal.Parse(!string.IsNullOrEmpty(itemTrans.priceNet) ? itemTrans.priceNet : "0") * (percent / 100) / (1 + (percent / 100));
                                }
                                if (isSvc == true)
                                {
                                    decimal percent = generateTransaction[indexSvc].Percentage;
                                    priceSvc = (decimal.Parse(!string.IsNullOrEmpty(itemTrans.priceNet) ? itemTrans.priceNet : "0") - priceVat) * (percent / 100) / (1 + (percent / 100));
                                }
                                FolioDetailModel folioSub = new FolioDetailModel();
                                folioSub.UserID = int.Parse(Request.Form["userID"].ToString());
                                folioSub.ShiftID = shiftID;
                                folioSub.UserName = Request.Form["userID"].ToString();
                                folioSub.CashierNo = shiftName;
                                folioSub.ReservationID = folioSub.OriginReservationID = folio.ReservationID;
                                folioSub.FolioID = folioSub.OriginFolioID = folio.ID;
                                folioSub.InvoiceNo = invoiceNo;
                                folioSub.TransactionNo = transactionNo;
                                folioSub.ReceiptNo = "";
                                folioSub.TransactionDate = businessDateModel[0].BusinessDate;
                                folioSub.ProfitCenterID = 2;
                                folioSub.ProfitCenterCode = "0";
                                folioSub.TransactionGroupID = item.TransactionGroupID;
                                folioSub.TransactionSubgroupID = item.TransactionSubGroupID;
                                folioSub.GroupCode = item.GroupCode;
                                folioSub.SubgroupCode = item.SubgroupCode;
                                folioSub.GroupType = item.GroupType;
                                folioSub.TransactionCode = item.TransactionCodeDetail;
                                folioSub.ArticleCode = "";
                                folioSub.Status = false;
                                if (postType == 1)
                                {
                                    folioSub.RowState = 2;
                                    folioSub.PostType = 2;
                                }
                                else
                                {
                                    folioSub.RowState = 3;
                                    folioSub.PostType = 3;
                                }
                                folioSub.IsSplit = false;
                                folioSub.Quantity = int.Parse(!string.IsNullOrEmpty(itemTrans.quantity) ? itemTrans.quantity : "0");
                                if (isVat == false && isSvc == false)
                                {
                                    folioSub.Price = decimal.Parse(!string.IsNullOrEmpty(itemTrans.priceNet) ? itemTrans.priceNet : "0") - decimal.Parse(!string.IsNullOrEmpty(itemTrans.priceNet) ? itemTrans.priceNet : "0") * (item.Percentage / 100);

                                }
                                else
                                {
                                    folioSub.Price = decimal.Parse(!string.IsNullOrEmpty(itemTrans.priceNet) ? itemTrans.priceNet : "0") - priceVat - priceSvc;
                                }
                                folioSub.Amount = folioSub.AmountMaster = folioSub.AmountBeforeTax = folioSub.AmountMasterBeforeTax = folioSub.AmountGross = folioSub.AmountMasterGross = folioSub.Price * folioSub.Quantity;
                                folioSub.CurrencyID = folioSub.CurrencyMaster = "VND";
                                folioSub.Description = item.Description;
                                folioSub.Reference = "";
                                folioSub.RoomType = "";
                                folioSub.RoomTypeID = 0;
                                folioSub.UserInsertID = folioSub.UserUpdateID = int.Parse(Request.Form["userID"].ToString());
                                folioSub.CreateDate = folioSub.UpdateDate = DateTime.Now;
                                folioSub.RoomID = reservation.RoomId;
                                folioSub.Property = folioSub.CheckNo = folioSub.OriginARNo = "";
                                folioSub.IsPostedAR = false;
                                folioSub.ARTransID = 0;
                                folioSub.IsTransfer = false;
                                FolioDetailBO.Instance.Insert(folioSub);
                            }
                        }
                    }
                    #endregion

                    #region update lại balance VND của folio và reservation
                    int reservationID = folio.ReservationID;
                    decimal balance = FolioDetailBO.CalculateBalance(reservationID);
                    folio.BalanceVND = balance;
                    FolioBO.Instance.Update(folio);

                    // update balance reservation
                    ReservationModel res = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(reservationID);
                    res.BalanceVND = balance;
                    ReservationBO.Instance.Update(res);
                    #endregion

                    #region lưu posting history

                    PostingHistoryModel postingHistory = new PostingHistoryModel();
                    postingHistory.ActionType = 0;
                    postingHistory.ActionText = $"[POST_GEN] - {tranCode} - {trans[0].Description}";
                    postingHistory.ActionDate = DateTime.Now;
                    postingHistory.ActionUser = Request.Form["userName"].ToString();
                    postingHistory.Amount = folioArticle.AmountMaster;
                    postingHistory.InvoiceNo = folioArticle.InvoiceNo;
                    postingHistory.Supplement = "";
                    postingHistory.Code = tranCode;
                    postingHistory.Description = trans[0].Description;
                    postingHistory.TransactionDate = businessDateModel[0].BusinessDate;
                    postingHistory.ReasonCode = "";
                    postingHistory.ReasonCode = "";
                    postingHistory.Terminal = "";
                    postingHistory.Machine = Environment.MachineName;
                    postingHistory.Action_FolioID = postingHistory.AfterAction_FolioID = folio.ID;
                    postingHistory.Property = "PMS";
                    PostingHistoryBO.Instance.Insert(postingHistory);
                    #endregion
                }

                pt.CommitTransaction();
                return Json(new { code = 0, msg = "Post transaction was successfully" });

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
        #endregion



        #region AccountType

        public IActionResult AccountType()
        {

            List<CurrencyModel> listcurr = PropertyUtils.ConvertToList<CurrencyModel>(CurrencyBO.Instance.FindAll());
            ViewBag.CurrencyList = listcurr;
            return PartialView();
        }

        [HttpGet]
        public IActionResult AccountTypeData()
        {
            try
            {
                DataTable dataTable = _iAccountingService.AccountTypeData();

                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  ID = d["ID"]?.ToString() ?? "",
                                  Type = d["Type"]?.ToString() ?? "",
                                  Description = d["Description"]?.ToString() ?? "",
                                  CreditLimit = d["CreditLimit"] != DBNull.Value ? Convert.ToDecimal(d["CreditLimit"]) : 0,
                                  CurrencyID = d["CurrencyID"]?.ToString() ?? "",
                                  StatementMode = d["StatementMode"]?.ToString() ?? "",
                                  ReminderCycle = d["ReminderCycle"]?.ToString() ?? "",
                                  DayOfMonth = d["DayOfMonth"] != DBNull.Value ? Convert.ToInt32(d["DayOfMonth"]) : 0,
                                  Amount = d["Amount"] != DBNull.Value ? Convert.ToDecimal(d["Amount"]) : 0,
                                  DayOrderThan = d["DayOrderThan"] != DBNull.Value ? Convert.ToInt32(d["DayOrderThan"]) : 0,
                                  Percentage = d["Percentage"] != DBNull.Value ? Convert.ToDecimal(d["Percentage"]) : 0,
                                  IncludePayment = d["IncludePayment"] != DBNull.Value ? Convert.ToInt32(d["IncludePayment"]) : 0,
                                  CreatedDate = d["CreatedDate"] != DBNull.Value ? Convert.ToDateTime(d["CreatedDate"]) : (DateTime?)null,
                                  UpdatedDate = d["UpdatedDate"] != DBNull.Value ? Convert.ToDateTime(d["UpdatedDate"]) : (DateTime?)null,
                                  UserInsert = d["UserInsert"]?.ToString() ?? "",
                                  UserUpdate = d["UserUpdate"]?.ToString() ?? ""

                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult AccountTypeSave(string typeacc, string descriptionaccty, int creditLimit, string currencyacc, string statementmode, string remindercycle, int dayofmonth, string check, int dayolderthan, int amountorPercentage, string includePayment, string id, string user)
        {
            try
            {
                List<BusinessDateModel> businessDateModel = PropertyUtils.ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll());
                ARAccountTypeModel _Model = new ARAccountTypeModel();
                _Model.Type = typeacc;
                _Model.Description = descriptionaccty;
                _Model.CreditLimit = creditLimit;
                _Model.StatementMode = statementmode;
                _Model.ReminderCycle = remindercycle;
                _Model.DayOfMonth = dayofmonth;
                _Model.DayOrderThan = dayolderthan;

                if (check == "amount")
                {
                    _Model.Amount = amountorPercentage;
                    _Model.Percentage = 0;
                }
                else
                {
                    _Model.Percentage = amountorPercentage;
                    _Model.Amount = 0;
                }

                _Model.CurrencyID = currencyacc;
                _Model.IncludePayment = includePayment == "1" ? true : false;

                if (!string.IsNullOrEmpty(id) && id != "0")
                {
                    _Model.UpdatedBy = user;
                    _Model.UpdatedDate = businessDateModel[0].BusinessDate;
                    _Model.ID = int.Parse(id);
                    ARAccountTypeBO.Instance.Update(_Model);
                }
                else
                {
                    _Model.CreatedBy = user;
                    _Model.UpdatedBy = user;
                    _Model.CreatedDate = businessDateModel[0].BusinessDate;
                    _Model.UpdatedDate = _Model.CreatedDate;
                    ARAccountTypeBO.Instance.Insert(_Model);
                }

                return Json(new { success = true, message = "Success" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult AccountTypeDelete(int id)
        {
            try
            {
                ARAccountTypeBO.Instance.Delete(id);

                return Json(new { success = true, message = "Success Delete!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        #endregion

        #region ARAgingLevels

        [HttpGet]
        public IActionResult GetARAgingLevelsData()
        {
            // Lấy dữ liệu từ BO (giống hệt code cũ của bạn)
            List<ARAgingLevelsModel> list = PropertyUtils.ConvertToList<ARAgingLevelsModel>(ARAgingLevelsBO.Instance.FindAll());

            // Trả về dữ liệu kiểu JSON thay vì View
            return Json(new { success = true, data = list });
        }
        [HttpPost]
        public IActionResult ARAgingLevelsSave(string level1, string level2, string level3, string level4, string level5, string user)
        {
            try
            {
                List<BusinessDateModel> businessDateModel = PropertyUtils.ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll());

                var levels = new List<string> { level1, level2, level3, level4, level5 };

                for (int i = 1; i <= 5; i++)
                {
                    string value = levels[i - 1];

                    // kiểm tra phải là số
                    if (!int.TryParse(value, out int number))
                    {
                        return BadRequest(new { success = false, message = $"Day of Levels{i} must be number" });
                    }
                    if (number == 0)
                    {
                        return BadRequest(new { success = false, message = $"Day of Levels{i} cannot be 0" });
                    }

                    // Kiểm tra DB đã có record Levels = i chưa
                    DataTable dt = TextUtils.Select("SELECT * FROM ARAgingLevels WHERE Levels='" + i + "'");

                    if (dt.Rows.Count > 0)
                    {
                        // Update
                        ARAgingLevelsModel model = (ARAgingLevelsModel)ARAgingLevelsBO.Instance.FindByPrimaryKey(Convert.ToInt32(dt.Rows[0]["ID"].ToString()));

                        model.AgingValue = number;
                        model.UpdatedDate = businessDateModel[0].BusinessDate; ;
                        model.UpdatedBy = user;

                        ARAgingLevelsBO.Instance.Update(model);
                    }
                    else
                    {
                        // Insert
                        ARAgingLevelsModel model = new ARAgingLevelsModel
                        {
                            Levels = i.ToString(),
                            AgingValue = number,
                            Description = "",
                            CreatedDate = businessDateModel[0].BusinessDate,
                            UpdatedDate = businessDateModel[0].BusinessDate,
                            CreatedBy = user,
                            UpdatedBy = user
                        };

                        ARAgingLevelsBO.Instance.Insert(model);
                    }
                }

                return Json(new { success = true, message = "Save success!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }


        #endregion

        #region AROpening


        public IActionResult AROpening()
        {
            List<CurrencyModel> listcurr = PropertyUtils.ConvertToList<CurrencyModel>(CurrencyBO.Instance.FindAll());
            ViewBag.CurrencyList = listcurr;

            return PartialView(); // View này sẽ chứa DataGrid + script gọi API
        }

        [HttpGet]
        public IActionResult AROpeningData()
        {
            try
            {
                DataTable dataTable = _iAccountingService.AROpeningData();

                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  ID = d["ID"]?.ToString() ?? "",
                                  ARID = d["ARID"]?.ToString() ?? "",
                                  AccountName = d["AccountName"]?.ToString() ?? "",
                                  Type = d["Type"]?.ToString() ?? "",
                                  AccountNo = d["AccountNo"]?.ToString() ?? "",
                                  City = d["City"]?.ToString() ?? "",
                                  Balance = d["Balance"]?.ToString() ?? "",
                                  ContactName = d["ContactName"]?.ToString() ?? "",
                                  CurrencyID = d["CurrencyID"]?.ToString() ?? "",
                                  CreatedDate = d["CreatedDate"]?.ToString() ?? "",
                                  UpdatedDate = d["UpdatedDate"]?.ToString() ?? "",
                                  CreatedBy = d["CreatedBy"]?.ToString() ?? "",
                                  UpdatedBy = d["UpdatedBy"]?.ToString() ?? ""
                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }


        [HttpPost]
        public IActionResult AROpeningSave(string accountName, string accountNo, string arid, string balance, string city, string contactName, string createdBy, string createdDate, string currencyID, string id, string type, string updatedBy, string updatedDate, string user)
        {
            List<BusinessDateModel> businessDateModel = PropertyUtils.ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll());
            try
            {
                ARAccountReceivableOldBalancesModel model;
                if (!string.IsNullOrEmpty(id))
                {
                    if (currencyID == "")
                    {
                        throw new Exception("Currency");
                    }
                    model = (ARAccountReceivableOldBalancesModel)ARAccountReceivableOldBalancesBO.Instance.FindByPrimaryKey(int.Parse(id));
                    model.Amount = Convert.ToDecimal(balance);

                    model.CurrencyID = currencyID;
                    model.UpdatedBy = user;
                    model.UpdatedDate = businessDateModel[0].BusinessDate;
                    ARAccountReceivableOldBalancesBO.Instance.Update(model);
                }
                else
                {
                    if (!string.IsNullOrEmpty(balance))
                    {
                        if (currencyID == "")
                        {
                            throw new Exception("Currency");
                        }
                        model = new ARAccountReceivableOldBalancesModel();
                        model.AccountReceivableID = Convert.ToInt32(arid);
                        model.Amount = Convert.ToDecimal(balance);
                        model.CurrencyID = currencyID;
                        model.UpdatedBy = user;
                        model.UpdatedDate = businessDateModel[0].BusinessDate;
                        model.CreatedBy = user;
                        model.CreatedDate = businessDateModel[0].BusinessDate;
                        ARAccountReceivableOldBalancesBO.Instance.Insert(model);
                    }
                }
                return Json(new { success = true, message = "Data updated!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region ARTraces
        public IActionResult ARTraces()
        {
            List<BusinessDateModel> businessDateModel = PropertyUtils.ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll());
            ViewBag.BusinessDate = businessDateModel[0].BusinessDate;
            return PartialView();
        }
        [HttpGet]
        public IActionResult ARTracesData()
        {
            try
            {
                DataTable dataTable = _iAccountingService.ARTracesData();

                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  ID = d["ID"]?.ToString() ?? "",
                                  Name = d["Name"]?.ToString() ?? "",
                                  TraceText = d["TraceText"]?.ToString() ?? "",
                                  TraceAt = d["TraceAt"] == DBNull.Value ? null : Convert.ToDateTime(d["TraceAt"]).ToString("yyyy-MM-dd HH:mm:ss"),
                                  ResolvedAt = d["ResolvedAt"] == DBNull.Value ? null : Convert.ToDateTime(d["ResolvedAt"]).ToString("yyyy-MM-dd HH:mm:ss"),
                                  ResolvedBy = d["ResolvedBy"]?.ToString() ?? "",
                                  CreatedBy = d["CreatedBy"]?.ToString() ?? "",
                                  CreatedDate = d["CreatedDate"] == DBNull.Value ? null : Convert.ToDateTime(d["CreatedDate"]).ToString("yyyy-MM-dd HH:mm:ss"),
                                  UpdatedBy = d["UpdatedBy"]?.ToString() ?? "",
                                  UpdatedDate = d["UpdatedDate"] == DBNull.Value ? null : Convert.ToDateTime(d["UpdatedDate"]).ToString("yyyy-MM-dd HH:mm:ss")

                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult ARAccountReceivableSearch()
        {
            try
            {
                DataTable dataTable = _iAccountingService.ARAccountReceivableSearch();

                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {

                                  Check = d["Check"]?.ToString() ?? "",
                                  AccountName = d["AccountName"]?.ToString() ?? "",
                                  AccountNo = d["AccountNo"]?.ToString() ?? "",
                                  CreditLimit = d["CreditLimit"]?.ToString() ?? "",
                                  CurrencyID = d["CurrencyID"]?.ToString() ?? "",
                                  ContactName = d["ContactName"]?.ToString() ?? "",
                                  AccountTypeID = d["AccountTypeID"]?.ToString() ?? "",
                                  TelePhone = d["TelePhone"]?.ToString() ?? "",
                                  Fax = d["Fax"]?.ToString() ?? "",
                                  Email = d["Email"]?.ToString() ?? "",
                                  Address1 = d["Address1"]?.ToString() ?? "",
                                  StatusFlagged = d["StatusFlagged"]?.ToString() ?? "",
                                  StatusInactive = d["StatusInactive"]?.ToString() ?? "",
                                  PaymentDueDays = d["PaymentDueDays"]?.ToString() ?? "",
                                  Description = d["Description"]?.ToString() ?? "",
                                  CreatedBy = d["CreatedBy"]?.ToString() ?? "",
                                  CreatedDate = d["CreatedDate"]?.ToString() ?? "",
                                  UpdatedBy = d["UpdatedBy"]?.ToString() ?? "",
                                  UpdatedDate = d["UpdatedDate"]?.ToString() ?? "",
                                  ID = d["ID"]?.ToString() ?? "",
                                  BalanceVND = d["BalanceVND"]?.ToString() ?? "",
                                  BalanceUSD = d["BalanceUSD"]?.ToString() ?? ""

                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        [HttpPost]
        public IActionResult ARTraceUpdate(int idselectedRowData, DateTime tracetime, string traceText, string user)
        {
            try
            {
                user = user?.Replace("\"", "").Trim();
                ARTraceModel model = (ARTraceModel)ARTraceBO.Instance.FindByPrimaryKey(idselectedRowData);
                List<BusinessDateModel> businessDateModel = PropertyUtils.ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll());
                model.TraceText = traceText;
                model.TraceAt = tracetime;
                model.UpdatedBy = user;
                model.UpdatedDate = businessDateModel[0].BusinessDate;
                // Gọi Business Object để lưu
                ARTraceBO.Instance.Update(model);


                return Json(new { success = true, message = "Update success!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult ARTraceSave([FromBody] ARTraceSaveModel model)
        {
            try
            {
                model.user = model.user?.Replace("\"", "").Trim();
                List<BusinessDateModel> businessDateModel = PropertyUtils.ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll());
                foreach (var acc in model.selectedAccounts)
                {
                    ARTraceModel trace = new ARTraceModel
                    {
                        ARAccountID = acc.id,
                        TraceAt = model.tracetime,
                        TraceText = model.tracetext,
                        ResolvedAt = new DateTime(1900, 1, 1),
                        ResolvedBy = "Unresolved",
                        CreatedBy = model.user,
                        CreatedDate = businessDateModel[0].BusinessDate,
                        UpdatedBy = model.user,
                        UpdatedDate = businessDateModel[0].BusinessDate
                    };

                    // Gọi Business Object để lưu
                    ARTraceBO.Instance.Insert(trace);
                }


                return Json(new { success = true, message = "Insert success!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        public class ARTraceSaveModel
        {
            public DateTime tracetime { get; set; }
            public string tracetext { get; set; }
            public string user { get; set; }
            public List<AccountInfo> selectedAccounts { get; set; }
        }

        public class AccountInfo
        {
            public int id { get; set; }
            public string accountName { get; set; }
            public string accountNo { get; set; }
            public string contactName { get; set; }
            public string telePhone { get; set; }
            public string email { get; set; }
        }

        [HttpPost]
        public IActionResult ARTraceDelete(int id)
        {
            try
            {
                ARTraceBO.Instance.Delete(id);

                return Json(new { success = true, message = "Success Delete!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult ARTraceResolve(int id, string user)
        {
            List<BusinessDateModel> businessDateModel = PropertyUtils.ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll());
            DateTime businessDate = businessDateModel[0].BusinessDate;

            // Ghép ngày từ businessDate + giờ hiện tại
            DateTime currentTime = DateTime.Now;
            DateTime resolvedAt = new DateTime(
                businessDate.Year,
                businessDate.Month,
                businessDate.Day,
                currentTime.Hour,
                currentTime.Minute,
                currentTime.Second
            );

            user = user?.Replace("\"", "").Trim();
            try
            {
                ARTraceModel model = (ARTraceModel)ARTraceBO.Instance.FindByPrimaryKey(id);
                model.ResolvedAt = resolvedAt;
                model.ResolvedBy = user;
                model.UpdatedBy = user;
                model.UpdatedDate = businessDateModel[0].BusinessDate;
                ARTraceBO.Instance.Update(model);

                return Json(new { success = true, message = "Success !" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        [HttpPost]
        public IActionResult ARTraceUnresolve(int id, string user)
        {
            List<BusinessDateModel> businessDateModel = PropertyUtils.ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll());
            user = user?.Replace("\"", "").Trim();
            try
            {

                ARTraceModel model = (ARTraceModel)ARTraceBO.Instance.FindByPrimaryKey(id);
                model.ResolvedAt = new DateTime(1900, 1, 1);
                model.ResolvedBy = "Unresolved";
                model.UpdatedBy = user;
                model.UpdatedDate = businessDateModel[0].BusinessDate;
                ARTraceBO.Instance.Update(model);

                return Json(new { success = true, message = "Success !" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        #endregion

        #region ARPaymentReport
        public IActionResult ARPaymentReport()
        {
            List<UsersModel> listUsers = PropertyUtils.ConvertToList<UsersModel>(UsersBO.Instance.FindAll());
            ViewBag.ListUsers = listUsers;
            return PartialView();
        }

        [HttpGet]
        public IActionResult GETARPaymentReport(DateTime fromDate, DateTime toDate, string accountNo, string cashier, int viewBy = 0)
        {
            try
            {

                DataTable resultExchangeData = _iAccountingService.ARPaymentReport(fromDate, toDate, accountNo, cashier, viewBy);
                var resultExchange = (from d in resultExchangeData.AsEnumerable()
                                      select d.Table.Columns.Cast<DataColumn>()
                                          //.Where(col => col.ColumnName != "AllotmentStageID" && col.ColumnName != "flag" && col.ColumnName != "Total")
                                          .ToDictionary(
                                            col => col.ColumnName,
                                            col =>
                                            {
                                                var value = d[col.ColumnName];
                                                if (value == DBNull.Value) return null;

                                                // CreatedDate: KHÔNG ToString
                                                if (col.ColumnName == "CreatedDate" || col.ColumnName == "UpdatedDate")
                                                    return value;

                                                // Các field khác: ToString
                                                return value.ToString();
                                            }
                                        )).ToList();
                return Json(resultExchange);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        #endregion
    }
}

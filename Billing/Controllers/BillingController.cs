using BaseBusiness.BO;
using BaseBusiness.Model;
using BaseBusiness.util;
using Billing.Dto;
using Billing.Services.Interfaces;
using DevExpress.Office.Utils;
using DevExpress.Web.Internal;
using DevExpress.XtraReports.Design;
using DevExpress.XtraReports.UI;
using DevExpress.XtraRichEdit.Fields;
using DevExpress.XtraRichEdit.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Transactions;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace Billing.Controllers
{
    public class BillingController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<BillingController> _logger;
        private readonly IMemoryCache _cache;
        private readonly IPostService _iPostService;
        private readonly ITransferTransactionService _iTransferTransactionService;
        private readonly ICrashierService _iCrashierService;
        private readonly IInvoicingService _invoicingService;
        private readonly IAdjustTransactionService _iAdjustTransactionService;
        public BillingController(ILogger<BillingController> logger,
                IMemoryCache cache, IConfiguration configuration, IPostService iPostService, ITransferTransactionService transferTransactionService, ICrashierService iCrashierService, IInvoicingService invoicingService, IAdjustTransactionService adjustTransaction)
        {
            _cache = cache;
            _logger = logger;
            _configuration = configuration;
            _iPostService = iPostService;
            _iTransferTransactionService = transferTransactionService;
            _iCrashierService = iCrashierService;
            _invoicingService = invoicingService;
            _iAdjustTransactionService = adjustTransaction;
        }


        #region DatVP __ Billing: Print
        [HttpPost]
        public ActionResult PrintBilling(string arrivalDate, string departureDate, string folioNo, string confirmationNo, string roomNo, List<DataBillingRecord> dataBilling, string customerName)
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();
                string url = "";
                DataTable myData = _invoicingService.GetPreviewBillingAmount(int.Parse(confirmationNo), int.Parse(folioNo));
                Thread.CurrentThread.CurrentCulture = new CultureInfo("vi-VN");
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("vi-VN");
                XtraReport report = new Billing.Templates.Preview.PreviewBilling();
                decimal total = Convert.ToDecimal(myData.Rows[0]["Total"] ?? 0);
                decimal net = Convert.ToDecimal(myData.Rows[0]["Net"] ?? 0);
                decimal svc = Convert.ToDecimal(myData.Rows[0]["Svc"] ?? 0);
                decimal vat = Convert.ToDecimal(myData.Rows[0]["Tax"] ?? 0);
                report.Parameters["arrival_date"].Value = arrivalDate;
                report.Parameters["departure_date"].Value = departureDate;
                report.Parameters["folio_no"].Value = folioNo;
                report.Parameters["confirmation_no"].Value = confirmationNo;
                report.Parameters["room_no"].Value = roomNo;
                report.Parameters["name_customer"].Value = customerName;
                report.Parameters["company_name"].Value = "";
                report.Parameters["total_amount"].Value = total.ToString();
                report.Parameters["net_amount"].Value = net.ToString();
                report.Parameters["svc_amount"].Value = svc.ToString();
                report.Parameters["vat_amount"].Value = vat.ToString();
                report.Parameters["balance_amount"].Value = "";

                report.DataSource = dataBilling;
                report.CreateDocument();

                using (MemoryStream msPdf = new MemoryStream())
                {
                    report.ExportToPdf(msPdf);
                    string base64Pdf = Convert.ToBase64String(msPdf.ToArray());
                    url = $"data:application/pdf;base64,{base64Pdf}";

                }
                pt.CommitTransaction();
                return Json(url);
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

        #region DatVP __ Billing: Common
        [HttpGet]
        public async Task<IActionResult> GetInforService()
        {
            try
            {

                var groupTransaction = TransactionGroupBO.GetList();
                var groupSubTransaction = TransactionSubGroupBO.GetList();
                var transactions = TransactionsBO.GetList();
                var articles = ArticleBO.GetList();

                return Json(new
                {
                    groupTransaction = groupTransaction,
                    groupSubTransaction = groupSubTransaction,
                    transactions = transactions,
                    articles = articles
                });
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUser()
        {
            try
            {

                List<UsersModel> users = PropertyUtils.ConvertToList<UsersModel>(UsersBO.Instance.FindAll());

                return Json(users);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetFolioNo(int reservationID)
        {
            try
            {

                var result = FolioBO.GetFolioNo(reservationID);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetReasonAdjustmentTransaction()
        {
            try
            {

                return Json(_iAdjustTransactionService.GetReasonAdjust());
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        #endregion

        #region DatVP __ Billing: Post
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
                    List<FolioModel> folio = PropertyUtils.ConvertToList<FolioModel>(FolioBO.Instance.FindByAttribute("ReservationID", int.Parse(Request.Form["rsvID"].ToString())))
                        .Where(x => x.FolioNo == int.Parse(Request.Form["window"].ToString())).ToList();
                    if (folio.Count < 1)
                    {
                        return Json(new { code = 1, msg = $"Could not find Folio. Please check Folio" });

                    }

                    if (folio[0].Status == true)
                    {
                        return Json(new { code = 1, msg = $"Can not post. Folio has been being locked" });

                    }

                    #region lưu transaction chính vào folio detail
                    // kiểm tra xem transaction chọn để post có article không
                    string articleCode = itemTrans.articleCode;
                    FolioDetailModel folioArticle = new FolioDetailModel();
                    folioArticle.UserID = int.Parse(Request.Form["userID"].ToString());
                    folioArticle.ShiftID = shiftID;
                    folioArticle.UserName = Request.Form["userName"].ToString();
                    folioArticle.CashierNo = shiftName;
                    folioArticle.ReservationID = folioArticle.OriginReservationID = int.Parse(Request.Form["rsvID"].ToString());
                    folioArticle.FolioID = folioArticle.OriginFolioID = folio[0].ID;
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
                    folioArticle.RoomID = int.Parse(Request.Form["roomID"].ToString());
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
                                folioSub.UserName = Request.Form["userName"].ToString();
                                folioSub.CashierNo = shiftName;
                                folioSub.ReservationID = folioSub.OriginReservationID = int.Parse(Request.Form["rsvID"].ToString());
                                folioSub.FolioID = folioSub.OriginFolioID = folio[0].ID;
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
                                folioSub.RoomID = int.Parse(Request.Form["roomID"].ToString());
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
                                folioSub.UserName = Request.Form["userName"].ToString();
                                folioSub.CashierNo = shiftName;
                                folioSub.ReservationID = folioSub.OriginReservationID = int.Parse(Request.Form["rsvID"].ToString());
                                folioSub.FolioID = folioSub.OriginFolioID = folio[0].ID;
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
                                folioSub.RoomID = int.Parse(Request.Form["roomID"].ToString());
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
                                folioSub.UserName = Request.Form["userName"].ToString();
                                folioSub.CashierNo = shiftName;
                                folioSub.ReservationID = folioSub.OriginReservationID = int.Parse(Request.Form["rsvID"].ToString());
                                folioSub.FolioID = folioSub.OriginFolioID = folio[0].ID;
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
                                folioSub.RoomID = int.Parse(Request.Form["roomID"].ToString());
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
                    int reservationID = int.Parse(Request.Form["rsvID"].ToString());
                    decimal balance = FolioDetailBO.CalculateBalance(reservationID);
                    folio[0].BalanceVND = balance;
                    FolioBO.Instance.Update(folio[0]);

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
                    postingHistory.Action_FolioID = postingHistory.AfterAction_FolioID = folio[0].ID;
                    postingHistory.Property = "PMS";
                    PostingHistoryBO.Instance.Insert(postingHistory);
                    #endregion
                }

                pt.CommitTransaction();
                return Json(new { code = 0, msg = "Posting created successfully" });

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
        public async Task<IActionResult> CalculatePrice(string transactionCode, string price)
        {
            try
            {
                if (string.IsNullOrEmpty(transactionCode))
                {
                    price = "0";
                }
                decimal net = _iPostService.CalculatePrice(transactionCode, decimal.Parse(price));

                return Json(net);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        [HttpGet]
        public async Task<IActionResult> CalculateNet(string transactionCode, string price)
        {
            try
            {
                if (string.IsNullOrEmpty(transactionCode))
                {
                    price = "0";
                }
                decimal net = _iPostService.CalculateNet(transactionCode, decimal.Parse(price));

                return Json(net);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        #endregion

        #region DatVP __ Billing: Edit Posting
        [HttpGet]
        public async Task<IActionResult> GetFolioDetailMaster(string transactionNo)
        {
            try
            {
                var result = FolioDetailBO.GetFolioDetailMaster(transactionNo);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetFolioDetailMasterEdit(string invoiceNoPosting)
        {
            try
            {
                var result = FolioDetailBO.GetFolioDetailMasterEdit(invoiceNoPosting);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        [HttpPost]
        public ActionResult EditPosting()
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();
                List<FolioDetailModel> trans = PropertyUtils.ConvertToList<FolioDetailModel>(FolioDetailBO.Instance.FindByAttribute("TransactionNo", Request.Form["transactionNo"].ToString()));
                if (trans.Count < 1)
                {
                    return Json(new { code = 1, msg = "Could not find!" });

                }
                string transCode = "";
                for (int i = 0; i < trans.Count; i++)
                {
                    if (trans[i].IsSplit == true && trans[i].RowState != trans[i].PostType)
                    {
                        transCode = trans[i].TransactionCode;
                        break;
                    }
                }
                List<GenerateTransactionModel> generateTransaction = PropertyUtils.ConvertToList<GenerateTransactionModel>(GenerateTransactionBO.Instance.FindByAttribute("TransactionCode", transCode));
                decimal priceVat = 0;
                decimal priceSvc = 0;
                if (generateTransaction.Count > 0)
                {

                    // Kiểm tra xem generate transaction có Tax không
                    for (int i = 0; i < generateTransaction.Count; i++)
                    {
                        if (generateTransaction[i].GroupCode == "Tax" && generateTransaction[i].SubgroupCode == "Tax")
                        {
                            priceVat = decimal.Parse(Request.Form["amount"].ToString()) * (generateTransaction[i].Percentage / 100) / (1 + (generateTransaction[i].Percentage / 100));
                            break;
                        }
                    }
                    // Kiểm tra xem generate transaction có Svc không
                    for (int i = 0; i < generateTransaction.Count; i++)
                    {
                        if (generateTransaction[i].GroupCode == "Tax" && generateTransaction[i].SubgroupCode == "SVC")
                        {
                            priceSvc = (decimal.Parse(Request.Form["amount"].ToString()) - priceVat) * (generateTransaction[i].Percentage / 100) / (1 + (generateTransaction[i].Percentage / 100));

                            break;
                        }
                    }
                }
                foreach (var item in trans)
                {
                    FolioDetailModel folio = (FolioDetailModel)FolioDetailBO.Instance.FindByPrimaryKey(item.ID);

                    folio.TransactionDate = DateTime.Parse(Request.Form["transactionDate"]);
                    if (folio.IsSplit == true)
                    {
                        folio.Price = decimal.Parse(Request.Form["price"].ToString());
                        folio.Quantity = int.Parse(Request.Form["quantity"].ToString());
                        folio.Amount = folio.AmountMaster = folio.AmountGross = folio.AmountMasterGross = folio.Price * folio.Quantity;
                        folio.AmountBeforeTax = folio.AmountMasterBeforeTax = folio.Amount - priceVat - priceSvc;
                        if (folio.RowState == 1)
                        {
                            folio.Supplement = Request.Form["supplement"].ToString();

                            folio.Reference = Request.Form["reference"].ToString();
                        }
                    }
                   
                    else
                    {
                        if (folio.SubgroupCode == "Tax")
                        {
                            folio.Quantity = int.Parse(Request.Form["quantity"].ToString());

                            folio.Price = priceVat / folio.Quantity;
                            folio.Amount = folio.AmountMaster = folio.AmountGross = folio.AmountMasterGross = folio.AmountBeforeTax = folio.AmountMasterBeforeTax = priceVat;
                        }
                        else if (folio.SubgroupCode == "Svc")
                        {
                            folio.Quantity = int.Parse(Request.Form["quantity"].ToString());

                            folio.Price = priceSvc / folio.Quantity;
                            folio.Amount = folio.AmountMaster = folio.AmountGross = folio.AmountMasterGross = folio.AmountBeforeTax = folio.AmountMasterBeforeTax = priceSvc;
                        }
                        else
                        {
                            decimal priceMain = decimal.Parse(Request.Form["amount"].ToString()) - priceVat - priceSvc;
                            folio.Quantity = int.Parse(Request.Form["quantity"].ToString());

                            folio.Price = priceMain / folio.Quantity;
                            folio.Amount = folio.AmountMaster = folio.AmountGross = folio.AmountMasterGross = folio.AmountBeforeTax = folio.AmountMasterBeforeTax = priceMain;
                        }
                    }
                   
                        FolioDetailBO.Instance.Update(folio);
                }

                pt.CommitTransaction();
                return Json(new { code = 0, msg = "Edit Posting was successfully" });

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

        #region DatVP __ Billing: Payment
        [HttpGet]
        public async Task<IActionResult> GetPaymentFO()
        {
            try
            {
                List<TransactionsModel> list = TransactionsBO.GetPaymentFO();
                return Json(list);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        [HttpPost]
        public ActionResult PostPayment()
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();

                int reservationID = int.Parse(Request.Form["rsvID"].ToString());
                int transID = int.Parse(Request.Form["transID"].ToString());
                int folioID = int.Parse(Request.Form["folioNoID"].ToString());

                if (transID == 0)
                {
                    return Json(new { code = 0, msg = "Could not find payment code" });

                }
                List<FolioModel> folio = PropertyUtils.ConvertToList<FolioModel>(FolioBO.Instance.FindByAttribute("ReservationID", reservationID)).Where(x => x.ID == int.Parse(Request.Form["folioNoID"].ToString())).ToList();
                if (folio.Count < 1)
                {
                    return Json(new { code = 1, msg = $"Could not find Folio. Please check Folio" });

                }
                int shiftID = int.Parse(Request.Form["shiftID"].ToString());
                string shiftName = Request.Form["shiftName"].ToString();

                TransactionsModel trans = (TransactionsModel)TransactionsBO.Instance.FindByPrimaryKey(transID);
                string invoiceNo = (FolioDetailBO.GetTopInvoiceNo() + 1).ToString();
                string transactionNo = (FolioDetailBO.GetTopTransactioNo()).ToString();

                #region insert vào folio detail
                FolioDetailModel folioDetail = new FolioDetailModel();
                folioDetail.UserID = int.Parse(Request.Form["userID"].ToString());
                folioDetail.ShiftID = shiftID;
                folioDetail.UserName = Request.Form["userName"].ToString();
                folioDetail.CashierNo = shiftName;
                folioDetail.ReservationID = folioDetail.OriginReservationID = reservationID;
                folioDetail.FolioID = folioDetail.OriginFolioID = folio[0].ID;
                folioDetail.InvoiceNo = invoiceNo;
                folioDetail.TransactionNo = transactionNo;
                folioDetail.ReceiptNo = "";
                folioDetail.TransactionDate = DateTime.Parse(Request.Form["transDate"].ToString());
                folioDetail.ProfitCenterID = 2;
                folioDetail.ProfitCenterCode = "0";
                folioDetail.TransactionGroupID = trans.TransactionGroupID;
                folioDetail.TransactionSubgroupID = trans.TransactionSubGroupID;
                folioDetail.GroupCode = trans.GroupCode;
                folioDetail.SubgroupCode = trans.SubgroupCode;
                folioDetail.GroupType = trans.GroupType;
                folioDetail.TransactionCode = trans.Code;

                folioDetail.Reference = Request.Form["reference"].ToString();

                folioDetail.ArticleCode = "";

                folioDetail.Status = false;

                folioDetail.RowState = 1;
                folioDetail.PostType = 1;

                folioDetail.IsSplit = false;
                folioDetail.Quantity = 1;
                folioDetail.Price = 0 - decimal.Parse(Request.Form["amount"].ToString());
                folioDetail.Amount = 0 - decimal.Parse(Request.Form["amount"].ToString());
                folioDetail.CurrencyID = folioDetail.CurrencyMaster = "VND";
                folioDetail.AmountMaster = 0 - decimal.Parse(Request.Form["amount"].ToString());
                folioDetail.Description = trans.Description;
                folioDetail.AmountBeforeTax = folioDetail.AmountMasterBeforeTax = 0 - decimal.Parse(Request.Form["amount"].ToString());
                folioDetail.AmountGross = folioDetail.AmountMasterGross = 0 - decimal.Parse(Request.Form["amount"].ToString());
                folioDetail.RoomType = "";
                folioDetail.RoomTypeID = 0;
                folioDetail.UserInsertID = folioDetail.UserUpdateID = int.Parse(Request.Form["userID"].ToString());
                folioDetail.CreateDate = folioDetail.UpdateDate = DateTime.Now;
                folioDetail.RoomID = 0;
                folioDetail.Property = folioDetail.CheckNo = folioDetail.OriginARNo = "";
                folioDetail.IsPostedAR = false;
                folioDetail.ARTransID = 0;
                folioDetail.IsTransfer = false;
                FolioDetailBO.Instance.Insert(folioDetail);
                #endregion

                #region update lại balance của reservation và folio
                decimal balance = FolioDetailBO.CalculateBalance(reservationID);
                //decimal  amountold = folio[0].BalanceVND;
                folio[0].BalanceVND = balance;
                FolioBO.Instance.Update(folio[0]);

                // update balance reservation
                ReservationModel res = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(reservationID);
                res.BalanceVND = folio[0].BalanceVND;
                ReservationBO.Instance.Update(res);
                #endregion
                pt.CommitTransaction();
                return Json(new { code = 0, msg = "Payment was posted successfully", balanceVND = folio[0].BalanceVND });

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
        public async Task<IActionResult> GetBalanceVND(int rsvID)
        {
            try
            {
                List<ReservationModel> posting = new List<ReservationModel>();
                   posting = ReservationBO.GetBalanceVND(rsvID);
                return Json(posting);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        #endregion

        #region Billing: Create Folio
        [HttpPost]
        public ActionResult CreateFolio([FromBody] FolioModel model)
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();

                var res = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(model.ReservationID);

                var existingFolios = FolioBO.Instance.FindByAttribute("ReservationID", model.ReservationID);
                var usedFolioNos = existingFolios.Cast<FolioModel>().Select(x => x.FolioNo).ToList();

                // Validate if user's requested window is already in use
                if (usedFolioNos.Contains(model.FolioNo))
                {
                    return Json(new { code = 1, msg = $"Window {model.FolioNo} is already in use. Please select another window!" });
                }

                if (model.IsMasterFolio)
                {
                    // If UI lacks profile, mapping Company/Agent correctly
                    if (model.ProfileID <= 0 || model.ProfileID == res.ProfileIndividualId)
                    {
                        model.ProfileID = res.ProfileCompanyId > 0 ? res.ProfileCompanyId : (res.ProfileAgentId > 0 ? res.ProfileAgentId : res.ProfileGroupId);
                        DataTable dtProfile = TextUtils.Select($"SELECT Account FROM Profile WHERE ID = {model.ProfileID}");
                        if (dtProfile.Rows.Count > 0) model.AccountName = dtProfile.Rows[0]["Account"].ToString();
                    }
                }

                model.FolioDate = TextUtils.GetBusinessDate();
                model.CreateDate = model.UpdateDate = DateTime.Now;
                FolioBO.Instance.Insert(model);

                pt.CommitTransaction();
                return Json(new { code = 0, msg = "Folio created successfully", folioNo = model.FolioNo, profileName = model.AccountName });
            }
            catch (Exception ex)
            {
                pt.RollBack();
                return Json(new { code = 1, msg = ex.Message });
            }
            finally { pt.CloseConnection(); }
        }
        #endregion

        #region DatVP __ Billing: Delete Folio
        [HttpPost]
        public ActionResult DeleteFolio()
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();
                int folioNo = int.Parse(Request.Form["folioNo"].ToString());
                List<FolioDetailModel> folioDetail = PropertyUtils.ConvertToList<FolioDetailModel>(FolioDetailBO.Instance.FindByAttribute("FolioID", folioNo));
                if (folioDetail.Count > 0)
                {
                    return Json(new { code = 1, msg = "Can not delete this folio" });
                }
                FolioBO.Instance.Delete(folioNo);
                pt.CommitTransaction();
                return Json(new { code = 0, msg = "Delete folio was created successfully" });

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

        #region DatVP __ Billing: Lock Folio
        [HttpPost]
        public ActionResult LockFolioOnly()
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();
                int folioNo = int.Parse(Request.Form["folioNo"].ToString());
                FolioModel folioModel = (FolioModel)FolioBO.Instance.FindByPrimaryKey(folioNo);

                if (folioModel == null || folioModel.ID == 0)
                {
                    return Json(new { code = 1, msg = "Could not find folio" });
                }
                folioModel.Status = true;
                FolioBO.Instance.Update(folioModel);
                pt.CommitTransaction();
                return Json(new { code = 0, msg = "Folio was locked " });

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

        [HttpPost]
        public ActionResult LockFolio()
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();
                // Lấy chuỗi folioNo từ Request.Form
                string folioNo = Request.Form["folioNo"];

                // Kiểm tra dữ liệu
                if (string.IsNullOrEmpty(folioNo))
                {
                    return Json(new { code = 1, msg = "Could not find folio" });
                }

                // Tách chuỗi thành mảng và chuyển thành List<int>
                List<int> folioIds;
                try
                {
                    folioIds = folioNo.Split(',')
                                     .Select(x => int.Parse(x.Trim()))
                                     .ToList();
                }
                catch (FormatException)
                {
                    return Json(new { code = 1, msg = "Could not find folio" });
                }

                // Kiểm tra danh sách
                if (folioIds.Count == 0)
                {
                    return Json(new { code = 1, msg = "Coud not find folio" });
                }
                foreach (var item in folioIds)
                {
                    FolioModel folioModel = (FolioModel)FolioBO.Instance.FindByPrimaryKey(item);

                    if (folioModel == null || folioModel.ID == 0)
                    {
                        return Json(new { code = 1, msg = "Could not find folio" });
                    }
                    folioModel.Status = true;
                    FolioBO.Instance.Update(folioModel);
                }
                pt.CommitTransaction();
                return Json(new { code = 0, msg = "Folio was locked " });

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

        #region DatVP __ Billing: UnLock Folio
        [HttpPost]
        public ActionResult UnLockFolioOnly()
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();
                int folioNo = int.Parse(Request.Form["folioNo"].ToString());
                FolioModel folioModel = (FolioModel)FolioBO.Instance.FindByPrimaryKey(folioNo);

                if (folioModel == null || folioModel.ID == 0)
                {
                    return Json(new { code = 1, msg = "Could not find folio" });
                }
                folioModel.Status = false;
                FolioBO.Instance.Update(folioModel);
                pt.CommitTransaction();
                return Json(new { code = 0, msg = "Folio was unlocked " });

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

        [HttpPost]
        public ActionResult UnLockFolio()
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();
                // Lấy chuỗi folioNo từ Request.Form
                string folioNo = Request.Form["folioNo"];

                // Kiểm tra dữ liệu
                if (string.IsNullOrEmpty(folioNo))
                {
                    return Json(new { code = 1, msg = "Could not find folio" });
                }

                // Tách chuỗi thành mảng và chuyển thành List<int>
                List<int> folioIds;
                try
                {
                    folioIds = folioNo.Split(',')
                                     .Select(x => int.Parse(x.Trim()))
                                     .ToList();
                }
                catch (FormatException)
                {
                    return Json(new { code = 1, msg = "Could not find folio" });
                }

                // Kiểm tra danh sách
                if (folioIds.Count == 0)
                {
                    return Json(new { code = 1, msg = "Coud not find folio" });
                }
                foreach (var item in folioIds)
                {
                    FolioModel folioModel = (FolioModel)FolioBO.Instance.FindByPrimaryKey(item);

                    if (folioModel == null || folioModel.ID == 0)
                    {
                        return Json(new { code = 1, msg = "Could not find folio" });
                    }
                    folioModel.Status = false;
                    FolioBO.Instance.Update(folioModel);
                }
                pt.CommitTransaction();
                return Json(new { code = 0, msg = "Folio was unlocked " });

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

        #region DatVP __ Billing: Posting History
        [HttpGet]
        public async Task<IActionResult> PostingHistory(int type, int invoiceNo, int folioID)
        {
            try
            {
                List<PostingHistoryModel> posting = new List<PostingHistoryModel>();
                if (type == 1)
                {
                    posting = PostingHistoryBO.GetPostingHistoryByFolio(folioID);
                }
                else
                {
                    posting = PostingHistoryBO.GetPostingHistoryByInvoiceNo(invoiceNo);
                }

                return Json(posting);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        #endregion

        #region DatVP __ Billing: Delete Transaction
        [HttpPost]
        public ActionResult DeleteTransaction(List<int> folioDetailID, string reasonCode, string reasonText)
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();
                foreach (var item in folioDetailID)
                {
                    FolioDetailModel folioDetailModel = (FolioDetailModel)FolioDetailBO.Instance.FindByPrimaryKey(item);
                    if (folioDetailModel == null || folioDetailModel.ID == 0) continue;
                    List<FolioDetailModel> folioDetail = PropertyUtils.ConvertToList<FolioDetailModel>(FolioDetailBO.Instance.FindByAttribute("TransactionNo", folioDetailModel.TransactionNo));
                    if (folioDetail.Count > 0)
                    {
                        foreach (var folioItem in folioDetail)
                        {
                            folioItem.Status = true;
                            FolioDetailBO.Instance.Update(folioItem);

                            #region lưu posting history
                            List<BusinessDateModel> businessDateModel = PropertyUtils.ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll());
                            if (folioItem.RowState == 1)
                            {
                                PostingHistoryModel postingHistory = new PostingHistoryModel();
                                postingHistory.ActionType = 8;
                                postingHistory.ActionText = $"[DELETED] - {folioItem.TransactionCode} - {folioItem.Description}";
                                postingHistory.ActionDate = DateTime.Now;
                                postingHistory.ActionUser = Request.Form["userName"].ToString();
                                postingHistory.Amount = folioItem.AmountMaster;
                                postingHistory.InvoiceNo = folioItem.InvoiceNo;
                                postingHistory.Supplement = "";
                                postingHistory.Code = folioItem.TransactionCode;
                                postingHistory.Description = folioItem.Description;
                                postingHistory.TransactionDate = businessDateModel[0].BusinessDate;
                                postingHistory.ReasonCode = reasonCode;
                                postingHistory.ReasonCode = reasonText;
                                postingHistory.Terminal = "";
                                postingHistory.Machine = Environment.MachineName;
                                postingHistory.Action_FolioID = postingHistory.AfterAction_FolioID = folioItem.FolioID;
                                postingHistory.Property = "PMS";
                                PostingHistoryBO.Instance.Insert(postingHistory);
                            }

                            #endregion
                        }
                    }
                    #region update lại balance VND của folio và reservation
                    int reservationID = folioDetailModel.ReservationID;
                    decimal balance = FolioDetailBO.CalculateBalance(reservationID);
                    FolioModel folio = (FolioModel)FolioBO.Instance.FindByPrimaryKey(folioDetailModel.FolioID);
                    folio.BalanceVND = balance;
                    FolioBO.Instance.Update(folio);

                    // update balance reservation
                    ReservationModel res = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(reservationID);
                    res.BalanceVND = balance;
                    ReservationBO.Instance.Update(res);
                    #endregion
                }
                pt.CommitTransaction();
                return Json(new { code = 0, msg = "Delete Transaction was successfully " });

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
        public async Task<IActionResult> GetDeletionReason()
        {
            try
            {
                List<CommentModel> users = PropertyUtils.ConvertToList<CommentModel>(CommentBO.Instance.FindByAttribute("CommentTypeID", 8));

                return Json(users);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetDeletionReasonByCode(string code)
        {
            try
            {
                CommentModel users = PropertyUtils.ConvertToList<CommentModel>(CommentBO.Instance.FindByAttribute("Code", code)).FirstOrDefault();

                return Json(users);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        #endregion

        #region DatVP __ Billing: Split Transaction
        [HttpPost]
        public ActionResult SplitTransaction(int folioDetailID, decimal discountAmount, decimal discountPercent, decimal amount, string userName, string userID, int shiftID, string shiftName)
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();
                FolioDetailModel folioDetail = (FolioDetailModel)FolioDetailBO.Instance.FindByPrimaryKey(folioDetailID);
                if (folioDetail == null || folioDetail.ID == 0)
                {
                    return Json(new { code = 0, msg = "Could not find transaction to spliy " });

                }

                List<FolioDetailModel> listFolioDetail = PropertyUtils.ConvertToList<FolioDetailModel>(FolioDetailBO.Instance.FindByAttribute("TransactionNo", folioDetail.TransactionNo));
                // precentMain là tỉ lệ của transaction sẽ được thêm, percentSub là tỉ lệ của transaction sẽ được update
                decimal percentMain = discountPercent / 100;
                decimal percentSub = (100 - discountPercent) / 100;
                if (discountAmount > 0)
                {
                    percentMain = discountAmount / amount;
                    percentSub = (amount - discountAmount) / amount;
                }
                if (listFolioDetail.Count > 0)
                {
                    foreach (var item in listFolioDetail)
                    {
                        #region Insert 1 transaction theo transaction split 
                        FolioDetailModel folioDetailMain = (FolioDetailModel)item.Clone();
                        folioDetailMain.Price = Math.Round(item.Price * percentMain);
                        folioDetailMain.Amount = Math.Round(item.Amount * percentMain);
                        folioDetailMain.AmountMaster = Math.Round(item.AmountMaster * percentMain);
                        folioDetailMain.AmountBeforeTax = Math.Round(item.AmountBeforeTax * percentMain);
                        folioDetailMain.AmountMasterBeforeTax = Math.Round(item.AmountMasterBeforeTax * percentMain);
                        folioDetailMain.AmountGross = Math.Round(item.AmountGross * percentMain);
                        folioDetailMain.AmountMasterGross = Math.Round(item.AmountMasterGross * percentMain);
                        folioDetailMain.ShiftID = shiftID;
                        folioDetailMain.CashierNo = shiftName.ToString();
                        folioDetailMain.UserID = int.Parse(userID);
                        folioDetailMain.UserName = userName;
                        folioDetailMain.ReservationID = folioDetailMain.OriginReservationID = item.ReservationID;
                        folioDetailMain.FolioID = folioDetailMain.OriginFolioID = item.FolioID;
                        folioDetailMain.InvoiceNo = (FolioDetailBO.GetTopInvoiceNo() + 1).ToString();
                        folioDetailMain.TransactionNo = (FolioDetailBO.GetTopTransactioNo() + 1).ToString();
                        folioDetailMain.ReceiptNo = item.ReceiptNo;
                        folioDetailMain.TransactionDate = item.TransactionDate;
                        folioDetailMain.ProfitCenterID = item.ProfitCenterID;
                        folioDetailMain.ProfitCenterCode = item.ProfitCenterCode;
                        folioDetailMain.TransactionGroupID = item.TransactionGroupID;
                        folioDetailMain.TransactionSubgroupID = item.TransactionSubgroupID;
                        folioDetailMain.GroupCode = item.GroupCode;
                        folioDetailMain.SubgroupCode = item.SubgroupCode;
                        folioDetailMain.GroupType = item.GroupType;
                        folioDetailMain.TransactionCode = item.TransactionCode;
                        folioDetailMain.ArticleCode = item.ArticleCode;
                        folioDetailMain.Status = item.Status;
                        folioDetailMain.RowState = item.RowState;
                        folioDetailMain.PostType = item.PostType;

                        folioDetailMain.IsSplit = item.IsSplit;
                        folioDetailMain.Quantity = item.Quantity;
                        folioDetailMain.CurrencyID = folioDetailMain.CurrencyMaster = item.CurrencyID;
                        folioDetailMain.Description = item.Description;
                        folioDetailMain.RoomType = item.RoomType;
                        folioDetailMain.RoomTypeID = item.RoomTypeID;
                        folioDetailMain.UserInsertID = folioDetailMain.UserUpdateID = int.Parse(userID);
                        folioDetailMain.CreateDate = folioDetailMain.UpdateDate = DateTime.Now;
                        folioDetailMain.RoomID = item.RoomID;
                        folioDetailMain.Property = folioDetailMain.CheckNo = folioDetailMain.OriginARNo = item.Property;
                        folioDetailMain.IsPostedAR = item.IsPostedAR;
                        folioDetailMain.ARTransID = item.ARTransID;
                        folioDetailMain.IsTransfer = item.IsTransfer;
                        if (item.IsSplit == true)
                        {
                            folioDetailMain.Reference = $"{item.AmountMaster} split in to {folioDetailMain.AmountGross}";
                        }
                        FolioDetailBO.Instance.Insert(folioDetailMain);


                        #endregion

                        #region update lại transaction split
                        if (item.IsSplit == true)
                        {
                            item.Reference = $"{item.AmountMaster} split in to {Math.Round(item.AmountGross * percentSub)}";
                        }
                        item.Price = Math.Round(item.Price * percentSub);
                        item.Amount = Math.Round(item.Amount * percentSub);
                        item.AmountMaster = Math.Round(item.AmountMaster * percentSub);
                        item.AmountBeforeTax = Math.Round(item.AmountBeforeTax * percentSub);
                        item.AmountMasterBeforeTax = Math.Round(item.AmountMasterBeforeTax * percentSub);
                        item.AmountGross = Math.Round(item.AmountGross * percentSub);
                        item.AmountMasterGross = Math.Round(item.AmountMasterGross * percentSub);
                        item.UserUpdateID = int.Parse(userID);
                        item.UpdateDate = DateTime.Now;
                        FolioDetailBO.Instance.Update(item);
                        #endregion

                        #region lưu posting history 
                        List<BusinessDateModel> businessDateModel = PropertyUtils.ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll());
                        if (folioDetailMain.IsSplit == true)
                        {
                            PostingHistoryModel postingHistory = new PostingHistoryModel();
                            postingHistory.ActionType = 6;
                            postingHistory.ActionText = $"[SPLIT_TRANSACTION] - {folioDetailMain.TransactionCode} from {folioDetailMain.Reference}";
                            postingHistory.ActionDate = DateTime.Now;
                            postingHistory.ActionUser = Request.Form["userName"].ToString();
                            postingHistory.Amount = folioDetailMain.AmountMaster;
                            postingHistory.InvoiceNo = folioDetailMain.InvoiceNo;
                            postingHistory.Supplement = "";
                            postingHistory.Code = folioDetailMain.TransactionCode;
                            postingHistory.Description = folioDetailMain.Description;
                            postingHistory.TransactionDate = businessDateModel[0].BusinessDate;
                            postingHistory.ReasonCode = "";
                            postingHistory.ReasonCode = "";
                            postingHistory.Terminal = "";
                            postingHistory.Machine = Environment.MachineName;
                            postingHistory.Action_FolioID = postingHistory.AfterAction_FolioID = folioDetailMain.FolioID;
                            postingHistory.Property = "PMS";
                            PostingHistoryBO.Instance.Insert(postingHistory);
                        }

                        if (item.IsSplit == true)
                        {
                            PostingHistoryModel postingHistory = new PostingHistoryModel();
                            postingHistory.ActionType = 6;
                            postingHistory.ActionText = $"[SPLIT_TRANSACTION] - {item.TransactionCode} from {item.Reference}";
                            postingHistory.ActionDate = DateTime.Now;
                            postingHistory.ActionUser = Request.Form["userName"].ToString();
                            postingHistory.Amount = item.AmountMaster;
                            postingHistory.InvoiceNo = item.InvoiceNo;
                            postingHistory.Supplement = "";
                            postingHistory.Code = item.TransactionCode;
                            postingHistory.Description = item.Description;
                            postingHistory.TransactionDate = businessDateModel[0].BusinessDate;
                            postingHistory.ReasonCode = "";
                            postingHistory.ReasonCode = "";
                            postingHistory.Terminal = "";
                            postingHistory.Machine = Environment.MachineName;
                            postingHistory.Action_FolioID = postingHistory.AfterAction_FolioID = item.FolioID;
                            postingHistory.Property = "PMS";
                            PostingHistoryBO.Instance.Insert(postingHistory);
                        }
                        #endregion

                        #region update lại balance VND của folio và reservation
                        int reservationID = item.ReservationID;
                        decimal balance = FolioDetailBO.CalculateBalance(reservationID);
                        FolioModel folio = (FolioModel)FolioBO.Instance.FindByPrimaryKey(item.FolioID);
                        folio.BalanceVND = balance;
                        FolioBO.Instance.Update(folio);

                        // update balance reservation
                        ReservationModel res = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(reservationID);
                        res.BalanceVND = balance;
                        ReservationBO.Instance.Update(res);
                        #endregion
                    }
                }
                pt.CommitTransaction();
                return Json(new { code = 0, msg = "Split transaction was successfully " });

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

        #region DatVP __ Billing: Transfer To Window
        [HttpPost]
        public ActionResult TransferToWindow(string userName, int userID, List<int> folioDetailID, int folioMasterID, int folioID)
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();
                foreach (var item in folioDetailID)
                {
                    #region transfer transaction
                    List<BusinessDateModel> businessDateModel = PropertyUtils.ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll());

                    FolioDetailModel folioDetailModel = (FolioDetailModel)FolioDetailBO.Instance.FindByPrimaryKey(item);
                    if (folioDetailModel == null || folioDetailModel.ID == 0) continue;
                    List<FolioDetailModel> folioDetail = PropertyUtils.ConvertToList<FolioDetailModel>(FolioDetailBO.Instance.FindByAttribute("TransactionNo", folioDetailModel.TransactionNo));
                    ReservationModel res = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(folioDetailModel.ReservationID);

                    if (folioDetail.Count > 0)
                    {
                        foreach (var itemFolioDetail in folioDetail)
                        {
                            itemFolioDetail.FolioID = folioID;
                            itemFolioDetail.UserUpdateID = userID;
                            itemFolioDetail.UpdateDate = DateTime.Now;
                            itemFolioDetail.Supplement = $"<<< {res.LastName}-F[{folioMasterID}]";
                            FolioDetailBO.Instance.Update(itemFolioDetail);

                            if (itemFolioDetail.RowState == 1)
                            {
                                #region lưu posting history

                                PostingHistoryModel postingHistory = new PostingHistoryModel();
                                postingHistory.ActionType = 7;
                                postingHistory.ActionText = $"[TRANFERRED] - {itemFolioDetail.TransactionCode} - {itemFolioDetail.Description}";
                                postingHistory.ActionDate = DateTime.Now;
                                postingHistory.ActionUser = Request.Form["userName"].ToString();
                                postingHistory.Amount = itemFolioDetail.AmountMaster;
                                postingHistory.InvoiceNo = itemFolioDetail.InvoiceNo;
                                postingHistory.Supplement = "";
                                postingHistory.Code = itemFolioDetail.TransactionCode;
                                postingHistory.Description = itemFolioDetail.Description;
                                postingHistory.TransactionDate = businessDateModel[0].BusinessDate;
                                postingHistory.ReasonCode = "";
                                postingHistory.ReasonCode = "";
                                postingHistory.Terminal = "";
                                postingHistory.Machine = Environment.MachineName;
                                postingHistory.Action_FolioID = folioMasterID;
                                postingHistory.AfterAction_FolioID = folioID;
                                postingHistory.Property = "PMS";
                                PostingHistoryBO.Instance.Insert(postingHistory);
                                #endregion
                            }

                        }

                        #region update lại balance VND của folio và reservation
                        decimal balance = FolioDetailBO.CalculateBalance(folioDetailModel.ReservationID);

                        FolioModel folioMaster = (FolioModel)FolioBO.Instance.FindByPrimaryKey(folioMasterID);
                        folioMaster.BalanceVND = folioMaster.BalanceVND- balance;
                        FolioBO.Instance.Update(folioMaster);


                        FolioModel folio = (FolioModel)FolioBO.Instance.FindByPrimaryKey(folioID);
                        folio.BalanceVND = folio.BalanceVND + balance;
                        FolioBO.Instance.Update(folio);
                        // update balance reservation
                        res.BalanceVND = balance;
                        ReservationBO.Instance.Update(res);
                        #endregion
                    }
                    #endregion





                }
                pt.CommitTransaction();
                return Json(new { code = 0, msg = "Transfer transaction to window was successfully " });

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

        #region DatVP __ Billing: Transfer Transaction

        [HttpGet]
        public async Task<IActionResult> SearchGuestInRoom(string room, string name)
        {
            try
            {
                if (string.IsNullOrEmpty(room))
                {
                    room = "";
                }
                if (string.IsNullOrEmpty(name))
                {
                    name = "";
                }
                DataTable myData = _iTransferTransactionService.SearchGuestInRoom(room, name);

                var result = (from d in myData.AsEnumerable()

                              select new
                              {
                                  ReservationID = d["ReservationID"].ToString(),
                                  FolioID = d["FolioID"].ToString(),
                                  Room = d["Room"].ToString(),
                                  Name = d["Name"].ToString(),
                                  Balance = d["Balance"].ToString(),
                                  MainGuest = d["MainGuest"].ToString(),


                              }).ToList();


                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        [HttpPost]
        public ActionResult TransferTransaction(string userName, int userID, List<int> folioDetailID, int folioMasterID, int folioID)
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();
                FolioModel folioMasterCheck = (FolioModel)FolioBO.Instance.FindByPrimaryKey(folioMasterID);
                if (folioMasterCheck == null || folioMasterCheck.ID == 0)
                {
                    return Json(new { code = 1, msg = "Could not find folio transfer " });

                }
                if (folioMasterCheck.Status == true)
                {
                    return Json(new { code = 1, msg = "Folio transfer was locked " });

                }
                FolioModel folioTransferedCheck = (FolioModel)FolioBO.Instance.FindByPrimaryKey(folioID);

                if (folioTransferedCheck == null || folioTransferedCheck.ID == 0)
                {
                    return Json(new { code = 1, msg = "Could not find folio transfered " });

                }
                if (folioTransferedCheck.Status == true)
                {
                    return Json(new { code = 1, msg = "Folio transfered was locked " });

                }
                foreach (var item in folioDetailID)
                {
                    #region transfer transaction
                    List<BusinessDateModel> businessDateModel = PropertyUtils.ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll());

                    FolioDetailModel folioDetailModel = (FolioDetailModel)FolioDetailBO.Instance.FindByPrimaryKey(item);
                    if (folioDetailModel == null || folioDetailModel.ID == 0) continue;
                    List<FolioDetailModel> folioDetail = PropertyUtils.ConvertToList<FolioDetailModel>(FolioDetailBO.Instance.FindByAttribute("TransactionNo", folioDetailModel.TransactionNo));
                    ReservationModel res = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(folioDetailModel.ReservationID);

                    if (folioDetail.Count > 0)
                    {
                        foreach (var itemFolioDetail in folioDetail)
                        {
                            itemFolioDetail.FolioID = folioID;
                            itemFolioDetail.UserUpdateID = userID;
                            itemFolioDetail.UpdateDate = DateTime.Now;
                            if (itemFolioDetail.RowState == 1)
                            {
                                itemFolioDetail.Supplement = $"<<< #{res.RoomNo},{res.LastName}-F{folioMasterID}";

                            }
                            FolioDetailBO.Instance.Update(itemFolioDetail);

                            if (itemFolioDetail.RowState == 1)
                            {
                                #region lưu posting history

                                PostingHistoryModel postingHistory = new PostingHistoryModel();
                                postingHistory.ActionType = 7;
                                postingHistory.ActionText = $"[TRANFERRED] - {itemFolioDetail.TransactionCode} - {itemFolioDetail.Description}";
                                postingHistory.ActionDate = DateTime.Now;
                                postingHistory.ActionUser = Request.Form["userName"].ToString();
                                postingHistory.Amount = itemFolioDetail.AmountMaster;
                                postingHistory.InvoiceNo = itemFolioDetail.InvoiceNo;
                                postingHistory.Supplement = "";
                                postingHistory.Code = itemFolioDetail.TransactionCode;
                                postingHistory.Description = itemFolioDetail.Description;
                                postingHistory.TransactionDate = businessDateModel[0].BusinessDate;
                                postingHistory.ReasonCode = "";
                                postingHistory.ReasonCode = "";
                                postingHistory.Terminal = "";
                                postingHistory.Machine = Environment.MachineName;
                                postingHistory.Action_FolioID = folioMasterID;
                                postingHistory.AfterAction_FolioID = folioID;
                                postingHistory.Property = "PMS";
                                PostingHistoryBO.Instance.Insert(postingHistory);
                                #endregion
                            }

                        }

                        #region update lại balance VND của folio và reservation
                        decimal balance = FolioDetailBO.CalculateBalance(folioDetailModel.ReservationID);

                        FolioModel folioMaster = (FolioModel)FolioBO.Instance.FindByPrimaryKey(folioMasterID);
                        folioMaster.BalanceVND = balance;
                        FolioBO.Instance.Update(folioMaster);


                        FolioModel folio = (FolioModel)FolioBO.Instance.FindByPrimaryKey(folioID);
                        folio.BalanceVND = balance;
                        FolioBO.Instance.Update(folio);
                        // update balance reservation
                        res.BalanceVND = balance;
                        ReservationBO.Instance.Update(res);
                        #endregion
                    }
                    #endregion





                }
                pt.CommitTransaction();
                return Json(new { code = 0, msg = "Transfer transaction to window was successfully " });

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

        #region DatVP __ Billing: Adjust Transaction
        [HttpGet]
        public async Task<IActionResult> CheckAdjustCode(string transactionCode)
        {
            try
            {
                List<TransactionsModel> trans = PropertyUtils.ConvertToList<TransactionsModel>(TransactionsBO.Instance.FindByAttribute("Code", transactionCode));
                if (trans.Count < 1)
                {
                    return Json(new
                    {
                        code = 1,
                        msg = "Could not find transactions"
                    });
                }
                if (trans[0].AdjustmentCode == "" || string.IsNullOrEmpty(trans[0].AdjustmentCode))
                {
                    return Json(new
                    {
                        code = 1,
                        msg = "Adjustment Code could not find"
                    });
                }
                return Json(new
                {
                    code = 0,
                    msg = ""
                });
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        [HttpPost]
        public ActionResult PostAdjustmentTransaction()
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();
                List<BusinessDateModel> businessDateModel = PropertyUtils.ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll());
                decimal price = 0;
                decimal priceNet = 0;
                int postType = int.Parse(Request.Form["postType"].ToString());
                FolioDetailModel folioAdjust = (FolioDetailModel)FolioDetailBO.Instance.FindByPrimaryKey(int.Parse(Request.Form["folioDetailAdjustID"].ToString()));
                if (folioAdjust == null || folioAdjust.ID == 0)
                {
                    return Json(new { code = 0, msg = "Could not find transaction" });

                }

                if (postType == 1)
                {
                    price = decimal.Parse(Request.Form["adjustAmount"].ToString());
                    priceNet = decimal.Parse(Request.Form["adjustNet"].ToString());
                }
                else
                {
                    price = folioAdjust.AmountBeforeTax * (decimal.Parse(Request.Form["percentage"].ToString()) / 100);
                    priceNet = folioAdjust.AmountGross * (decimal.Parse(Request.Form["percentage"].ToString()) / 100);
                }
                // tìm invoice lớn nhất 
                string invoiceNo = (FolioDetailBO.GetTopInvoiceNo() + 1).ToString();

                string transactionNo = (FolioDetailBO.GetTopTransactioNo()).ToString();

                string tranCode = Request.Form["transCode"].ToString();
                if (string.IsNullOrEmpty(tranCode))
                {
                    return Json(new { code = 1, msg = "Please choose Transaction/Article!" });

                }
                List<TransactionsModel> transMain = PropertyUtils.ConvertToList<TransactionsModel>(TransactionsBO.Instance.FindByAttribute("Code", tranCode));
                List<TransactionsModel> trans = PropertyUtils.ConvertToList<TransactionsModel>(TransactionsBO.Instance.FindByAttribute("Code", transMain[0].AdjustmentCode));

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
                int shiftID = int.Parse(Request.Form["shiftID"].ToString());
                string shiftName = Request.Form["shiftName"].ToString();
                #region lưu transaction chính vào folio detail
                // kiểm tra xem transaction chọn để post có article không
                FolioDetailModel folioArticle = new FolioDetailModel();
                folioArticle.UserID = int.Parse(Request.Form["userID"].ToString());
                folioArticle.ShiftID = shiftID;
                folioArticle.CashierNo = shiftName;
                folioArticle.UserName = Request.Form["userName"].ToString();
                folioArticle.ReservationID = folioArticle.OriginReservationID = int.Parse(Request.Form["rsvID"].ToString());
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
                folioArticle.ArticleCode = "";
                folioArticle.Reference = Request.Form["reference"].ToString();

                folioArticle.Status = false;
                folioArticle.RowState = 1;
                folioArticle.PostType = 2;

                folioArticle.IsSplit = true;
                folioArticle.Quantity = 1;
                folioArticle.Price = priceNet;
                folioArticle.Amount = priceNet;
                folioArticle.CurrencyID = folioArticle.CurrencyMaster = "VND";
                folioArticle.AmountMaster = priceNet;
                folioArticle.Description = trans[0].Description;
                folioArticle.AmountBeforeTax = folioArticle.AmountMasterBeforeTax = price;
                folioArticle.AmountGross = folioArticle.AmountMasterGross = priceNet; ;
                folioArticle.RoomType = "";
                folioArticle.RoomTypeID = 0;
                folioArticle.UserInsertID = folioArticle.UserUpdateID = int.Parse(Request.Form["userID"].ToString());
                folioArticle.CreateDate = folioArticle.UpdateDate = DateTime.Now;
                folioArticle.RoomID = int.Parse(Request.Form["roomID"].ToString());
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
                            folioSub.UserName = Request.Form["userName"].ToString();
                            folioSub.ReservationID = folioSub.OriginReservationID = int.Parse(Request.Form["rsvID"].ToString());
                            folioSub.FolioID = folioSub.OriginFolioID = folio.ID;
                            folioSub.InvoiceNo = invoiceNo;
                            folioSub.ShiftID = shiftID;
                            folioSub.CashierNo = shiftName;
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
                            folioSub.RowState = 2;
                            folioSub.PostType = 2;

                            folioSub.IsSplit = false;
                            folioSub.Quantity = 1;
                            if (item.GroupCode == "Tax" && item.GroupCode == "Tax")
                            {
                                folioSub.Price = priceNet * (item.Percentage / 100) / (1 + (item.Percentage / 100));
                            }
                            folioSub.Amount = folioSub.AmountMaster = folioSub.AmountBeforeTax = folioSub.AmountMasterBeforeTax = folioSub.AmountGross = folioSub.AmountMasterGross = folioSub.Price * folioSub.Quantity;
                            folioSub.CurrencyID = folioSub.CurrencyMaster = "VND";
                            folioSub.Description = item.Description;
                            folioSub.Reference = "";
                            folioSub.RoomType = "";
                            folioSub.RoomTypeID = 0;
                            folioSub.UserInsertID = folioSub.UserUpdateID = int.Parse(Request.Form["userID"].ToString());
                            folioSub.CreateDate = folioSub.UpdateDate = DateTime.Now;
                            folioSub.RoomID = int.Parse(Request.Form["roomID"].ToString());
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
                                priceVat = priceNet * (percent / 100) / (1 + (percent / 100));

                            }
                            FolioDetailModel folioSub = new FolioDetailModel();
                            folioSub.UserID = int.Parse(Request.Form["userID"].ToString());
                            folioSub.UserName = Request.Form["userName"].ToString();
                            folioSub.ReservationID = folioSub.OriginReservationID = int.Parse(Request.Form["rsvID"].ToString());
                            folioSub.FolioID = folioSub.OriginFolioID = folio.ID;
                            folioSub.InvoiceNo = invoiceNo;
                            folioSub.ShiftID = shiftID;
                            folioSub.CashierNo = shiftName;
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

                            folioSub.RowState = 2;
                            folioSub.PostType = 2;
                            folioSub.IsSplit = false;
                            folioSub.Quantity = 1;
                            if (item.GroupCode == "Tax" && item.GroupCode == "Tax")
                            {
                                folioSub.Price = (priceNet - priceVat) * (item.Percentage / 100) / (1 + (item.Percentage / 100));
                            }
                            folioSub.Amount = folioSub.AmountMaster = folioSub.AmountBeforeTax = folioSub.AmountMasterBeforeTax = folioSub.AmountGross = folioSub.AmountMasterGross = folioSub.Price * folioSub.Quantity;
                            folioSub.CurrencyID = folioSub.CurrencyMaster = "VND";
                            folioSub.Description = item.Description;
                            folioSub.Reference = "";
                            folioSub.RoomType = "";
                            folioSub.RoomTypeID = 0;
                            folioSub.UserInsertID = folioSub.UserUpdateID = int.Parse(Request.Form["userID"].ToString());
                            folioSub.CreateDate = folioSub.UpdateDate = DateTime.Now;
                            folioSub.RoomID = int.Parse(Request.Form["roomID"].ToString());
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
                                priceVat = priceNet * (percent / 100) / (1 + (percent / 100));
                            }
                            if (isSvc == true)
                            {
                                decimal percent = generateTransaction[indexSvc].Percentage;
                                priceSvc = (priceNet - priceVat) * (percent / 100) / (1 + (percent / 100));
                            }
                            FolioDetailModel folioSub = new FolioDetailModel();
                            folioSub.UserID = int.Parse(Request.Form["userID"].ToString());
                            folioSub.UserName = Request.Form["userName"].ToString();
                            folioSub.ReservationID = folioSub.OriginReservationID = int.Parse(Request.Form["rsvID"].ToString());
                            folioSub.FolioID = folioSub.OriginFolioID = folio.ID;
                            folioSub.InvoiceNo = invoiceNo;
                            folioSub.ShiftID = shiftID;
                            folioSub.CashierNo = shiftName;
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
                            folioSub.RowState = 2;
                            folioSub.PostType = 2;

                            folioSub.IsSplit = false;
                            folioSub.Quantity = 1;
                            if (isVat == false && isSvc == false)
                            {
                                folioSub.Price = priceNet - priceNet * (item.Percentage / 100);

                            }
                            else
                            {
                                folioSub.Price = priceNet - priceVat - priceSvc;
                            }
                            folioSub.Amount = folioSub.AmountMaster = folioSub.AmountBeforeTax = folioSub.AmountMasterBeforeTax = folioSub.AmountGross = folioSub.AmountMasterGross = folioSub.Price * folioSub.Quantity;
                            folioSub.CurrencyID = folioSub.CurrencyMaster = "VND";
                            folioSub.Description = item.Description;
                            folioSub.Reference = "";
                            folioSub.RoomType = "";
                            folioSub.RoomTypeID = 0;
                            folioSub.UserInsertID = folioSub.UserUpdateID = int.Parse(Request.Form["userID"].ToString());
                            folioSub.CreateDate = folioSub.UpdateDate = DateTime.Now;
                            folioSub.RoomID = int.Parse(Request.Form["roomID"].ToString());
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
                int reservationID = int.Parse(Request.Form["rsvID"].ToString());
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

                pt.CommitTransaction();
                return Json(new { code = 0, msg = "Adjustment Transaction was successfully" });

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

        #region DatVP __ Billing: Shift Login
        [HttpPost]
        public async Task<IActionResult> ShiftLogin()
        {
            try
            {
                string loginName = Request.Form["LoginName"].ToString();
                string password = Request.Form["Password"].ToString();
                var model = _iCrashierService.Login(loginName, password);
                return Json(model);

            }
            catch (Exception ex)
            {
                return Json(new ShiftModel());
            }

        }

        [HttpPost]
        public async Task<IActionResult> GetCrashierInShift()
        {
            try
            {
                int userID = int.Parse(Request.Form["userID"].ToString());
                List<BusinessDateModel> businessDateModel = PropertyUtils.ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll());
                var model = ShiftBO.GetShiftByUser(businessDateModel[0].BusinessDate, userID);
                return Json(model);

            }
            catch (Exception ex)
            {
                return Json(new ShiftModel());
            }

        }
        [HttpPost]
        public async Task<IActionResult> GetInfoShift()
        {
            try
            {
                int shiftID = int.Parse(Request.Form["shiftID"].ToString());
                ShiftModel shift = (ShiftModel)ShiftBO.Instance.FindByPrimaryKey(shiftID);
                return Json(shift);

            }
            catch (Exception ex)
            {
                return Json(new ShiftModel());
            }

        }
        #endregion

        #region DatVP __ Invoicing: Billing
        [HttpGet]
        public async Task<IActionResult> SearchFolio(int guestStatus, int folioStatus, int folioType, string name, string room, string folioNo, string confirmationNo, string date)
        {
            try
            {
                // Kiểm tra và xử lý giá trị date
                DateTime? parsedDate = string.IsNullOrEmpty(date) ? (DateTime?)null : DateTime.TryParse(date, out DateTime tempDate) ? tempDate : (DateTime?)null;

                // Gọi dịch vụ với date đã xử lý
                var data = _invoicingService.SearchFolio(guestStatus, folioStatus, folioType, name, room, folioNo, confirmationNo, parsedDate?.ToString() ?? "");

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
                return Json(new { error = ex.Message });
            }
        }
        #endregion

        #region DatVP __ Billing: Select Option
        [HttpPost]
        public ActionResult GetTransactionBySelectOption()
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();
                #region lưu reservation item inventory
                string itemInventoryString = Request.Form["users"].ToString();
                List<int> userIDs = new List<int>();

                if (!string.IsNullOrEmpty(itemInventoryString))
                {
                    userIDs = itemInventoryString.Split(',')
                                                    .Select(x => int.Parse(x)).Where(x => x != 0)
                                                    .ToList();
                }
                DateTime fromDate = DateTime.Parse(Request.Form["fromDate"].ToString());
                DateTime toDate = DateTime.Parse(Request.Form["toDate"].ToString());
                int groupID = int.Parse(Request.Form["group"].ToString());
                int subGroupID = int.Parse(Request.Form["subGroup"].ToString());
                string transCodeID = Request.Form["code"].ToString();
                string shiftNo = Request.Form["shiftNo"].ToString();
                string checkNo = Request.Form["checkNo"].ToString();
                int rsvID = int.Parse(Request.Form["rsvID"].ToString());
                var result = FolioDetailBO.GetTransactionCodeBySelectOption(fromDate, toDate, groupID, subGroupID, transCodeID, shiftNo, checkNo, rsvID, userIDs);
                #endregion
                pt.CommitTransaction();
                return Json(result);

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

        #region Billing: Posting
        [HttpGet]
        public IActionResult GetCurrencies()
        {
            try
            {
                string sql = "select ID from Currency";

                DataTable dt = TextUtils.Select(sql);

                var result = (from r in dt.AsEnumerable()
                              select new
                              {
                                  ID = r["ID"],
                              }).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
        [HttpGet]
        public IActionResult GetTransactionGroups()
        {
            try
            {
                string sql = "select ID, Description from TransactionGroup where Description like N'%%' and Type != 1 order by Description";

                DataTable dt = TextUtils.Select(sql);

                var result = (from r in dt.AsEnumerable()
                              select new
                              {
                                  ID = r["ID"], 
                                  Description = !string.IsNullOrEmpty(r["Description"].ToString()) ? r["Description"] : ""
                              }).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
        [HttpGet]
        public IActionResult GetFolioNoByReservationID(int reservationID)
        {
            try
            {
                string sql = $"select ID,FolioNo, Status from Folio where ReservationID = {reservationID} Order By FolioNo";

                DataTable dt = TextUtils.Select(sql);

                var result = (from r in dt.AsEnumerable()
                              select new
                              {
                                  ID = r["ID"],
                                  FolioNo = r["FolioNo"],
                                  Status = r["Status"] != DBNull.Value ? Convert.ToBoolean(r["Status"]) : false
                              }).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
        [HttpGet]
        public IActionResult GetTransactionSubGroups(int groupId)
        {
            try
            {
                string sql = string.Format("select ID, Description from TransactionSubgroup where TransactionGroupID={0} and Description like N'%%' order by Description", groupId);

                DataTable dt = TextUtils.Select(sql);

                var result = (from r in dt.AsEnumerable()
                              select new
                              {
                                  ID = r["ID"],
                                  Description = !string.IsNullOrEmpty(r["Description"].ToString()) ? r["Description"] : ""
                              }).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
        [HttpGet]
        public IActionResult GetTransactions(int groupId, int subGroupId)
        {
            try
            {
                string sql = string.Format(@"
            select Code, Description, DefaultPrice 
            from Transactions 
            where TransactionGroupID={0} 
              and TransactionSubGroupID={1} 
              and (Code like N'%%' or Description like N'%%') 
              and ((GroupType != 1) AND (ManualPosting = 1)) 
              and (IsActive = 1) 
            order by Code", groupId, subGroupId);


                DataTable dt = TextUtils.Select(sql);

                var result = (from r in dt.AsEnumerable()
                              select new
                              {
                                  Code = !string.IsNullOrEmpty(r["Code"].ToString()) ? r["Code"] : "",
                                  Description = !string.IsNullOrEmpty(r["Description"].ToString()) ? r["Description"] : "",
                                  DefaultPrice = dt.Columns.Contains("DefaultPrice") && r["DefaultPrice"] != DBNull.Value ? r["DefaultPrice"] : 0
                              }).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
        [HttpGet]
        public IActionResult GetArticles(string transactionCode)
        {
            try
            {
                string safeCode = transactionCode.Replace("'", "''");

                string sql = string.Format(@"
                    select Code, Description, DefaultPrice 
                    from Article 
                    where (Code like N'%%' or Description like N'%%') 
                      and Code in (select ArticleCode from TransactionArticleLnk where TransactionCode='{0}') 
                    order by Code", safeCode);

                DataTable dt = TextUtils.Select(sql);

                var result = (from r in dt.AsEnumerable()
                              select new
                              {
                                  Code = !string.IsNullOrEmpty(r["Code"].ToString()) ? r["Code"] : "",
                                  Description = !string.IsNullOrEmpty(r["Description"].ToString()) ? r["Description"] : "",
                                  DefaultPrice = r["DefaultPrice"] != DBNull.Value ? r["DefaultPrice"] : 0
                              }).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
        [HttpGet]
        public IActionResult CalculatePricePlusPlus(string transactionCode, decimal netPrice)
        {
            try
            {
                decimal gross = _iPostService.CalculatePriceNet(transactionCode, netPrice);
                return Json(gross);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        [HttpGet]
        public IActionResult CalculatePriceNet(string transactionCode, decimal grossPrice)
        {
            try
            {
                decimal net = _iPostService.CalculatePricePlusPlus(transactionCode, grossPrice);
                return Json(net);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        [HttpPost]
        public IActionResult PostingSave([FromBody] List<FolioDetailModel> models)
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                if (models == null || models.Count == 0) return BadRequest("No data received.");

                pt.OpenConnection();
                pt.BeginTransaction();

                int nextInvoiceNo = FolioDetailBO.GetTopInvoiceNo() + 1;
                string batchInvoiceNo = nextInvoiceNo.ToString();

                int nextTransNo = FolioDetailBO.GetTopTransactioNo();
                int count = 0;
                DateTime businessDate = TextUtils.GetBusinessDate();

                bool isInvoicePosting = models.Any(m => m.PostType == 3);

                if (isInvoicePosting)
                {
                    decimal totalAmount = models.Sum(m => m.Amount);
                    decimal totalAmountBeforeTax = models.Sum(m => m.AmountBeforeTax);
                    var firstItem = models.First();

                    string invoiceName = !string.IsNullOrEmpty(firstItem.Description) ? firstItem.Description : "Invoicing Package";

                    string masterTransCode = "8449";
                    int mGroupId = 0, mSubgroupId = 0;
                    string mGroupCode = "", mSubgroupCode = "";

                    var masterTransList = TransactionsBO.Instance.FindByAttribute("Code", masterTransCode);
                    if (masterTransList != null && masterTransList.Count > 0)
                    {
                        var tInfo = (TransactionsModel)masterTransList[0];
                        mGroupId = tInfo.TransactionGroupID;
                        mGroupCode = tInfo.GroupCode;
                        mSubgroupId = tInfo.TransactionSubGroupID;
                        mSubgroupCode = tInfo.SubgroupCode;
                    }

                    // TẠO DÒNG MASTER INVOICE
                    FolioDetailModel masterLine = new FolioDetailModel
                    {
                        UserID = firstItem.UserID,
                        UserName = firstItem.UserName,
                        ShiftID = firstItem.ShiftID,
                        CashierNo = firstItem.CashierNo,
                        FolioID = firstItem.FolioID,
                        OriginFolioID = firstItem.OriginFolioID,
                        ReservationID = firstItem.ReservationID,
                        OriginReservationID = firstItem.OriginReservationID,
                        RoomID = firstItem.RoomID,
                        RoomType = firstItem.RoomType,
                        RoomTypeID = firstItem.RoomTypeID,

                        CreateDate = DateTime.Now,
                        UpdateDate = DateTime.Now,
                        TransactionDate = businessDate,
                        InvoiceNo = batchInvoiceNo,
                        TransactionNo = (nextTransNo + count).ToString(),

                        TransactionCode = masterTransCode,
                        Description = invoiceName,
                        TransactionGroupID = mGroupId,
                        GroupCode = mGroupCode,
                        TransactionSubgroupID = mSubgroupId,
                        SubgroupCode = mSubgroupCode,

                        Reference = firstItem.Reference,
                        Supplement = firstItem.Supplement,
                        CheckNo = firstItem.CheckNo,

                        Quantity = 1,
                        Price = totalAmount,
                        Amount = totalAmount,
                        AmountMaster = totalAmount,
                        AmountGross = totalAmount,
                        AmountMasterGross = totalAmount,
                        AmountBeforeTax = totalAmountBeforeTax,
                        AmountMasterBeforeTax = totalAmountBeforeTax,
                        CurrencyID = firstItem.CurrencyID,
                        CurrencyMaster = firstItem.CurrencyMaster,

                        RowState = 1, // MASTER HIỂN THỊ TRÊN BILLING
                        PostType = 3,
                        IsSplit = true,
                        Status = false,
                        ProfitCenterID = 2,
                        ProfitCenterCode = "0",
                        IsTransfer = false,
                        IsPostedAR = false,
                        Property = "",
                        OriginARNo = ""
                    };
                    FolioDetailBO.Instance.Insert(masterLine);

                    // History cho Master Line
                    PostingHistoryModel phMaster = new PostingHistoryModel
                    {
                        ActionType = 0,
                        ActionText = $"[POST_INV_MASTER] - {masterLine.TransactionCode} - {masterLine.Description}",
                        ActionDate = DateTime.Now,
                        ActionUser = masterLine.UserName,
                        Amount = masterLine.Amount,
                        InvoiceNo = masterLine.InvoiceNo,
                        Code = masterLine.TransactionCode,
                        Description = masterLine.Description,
                        TransactionDate = masterLine.TransactionDate,
                        Machine = Environment.MachineName,
                        Action_FolioID = masterLine.FolioID,
                        AfterAction_FolioID = masterLine.FolioID,
                        Property = "PMS"
                    };
                    PostingHistoryBO.Instance.Insert(phMaster);

                    count++;
                }

                //CÁC DÒNG CHI TIẾT (Sub-details)
                foreach (var item in models)
                {
                    List<GenerateTransactionModel> currentGenConfigs = PropertyUtils.ConvertToList<GenerateTransactionModel>(
                    GenerateTransactionBO.Instance.FindByAttribute("TransactionCode", item.TransactionCode));

                    var transList = TransactionsBO.Instance.FindByAttribute("Code", item.TransactionCode);
                    if (transList != null && transList.Count > 0)
                    {
                        var transInfo = (TransactionsModel)transList[0];
                        item.TransactionGroupID = transInfo.TransactionGroupID;
                        item.GroupCode = transInfo.GroupCode;
                        item.TransactionSubgroupID = transInfo.TransactionSubGroupID;
                        item.SubgroupCode = transInfo.SubgroupCode;
                    }

                    var roomTypeList = RoomTypeBO.Instance.FindByAttribute("Code", item.RoomType);
                    if (roomTypeList != null && roomTypeList.Count > 0)
                    {
                        var roomTypeInfo = (RoomTypeModel)roomTypeList[0];
                        item.RoomTypeID = roomTypeInfo.ID;
                    }

                    item.CreateDate = DateTime.Now; item.UpdateDate = DateTime.Now; item.TransactionDate = businessDate;
                    item.InvoiceNo = batchInvoiceNo; item.TransactionNo = (nextTransNo + count).ToString();
                    count++;

                    item.Status = false; item.ProfitCenterID = 2; item.ProfitCenterCode = "0"; item.OriginARNo = "";
                    item.RowState = isInvoicePosting ? 2 : 1;
                    item.IsSplit = currentGenConfigs.Count > 0;
                    item.IsPostedAR = false; item.ARTransID = 0; item.IsTransfer = false;

                    FolioDetailBO.Instance.Insert(item);

                    PostingHistoryModel postingHistory = new PostingHistoryModel
                    {
                        ActionType = 0,
                        ActionText = $"[POST_GEN] - {item.TransactionCode} - {item.Description}",
                        ActionDate = DateTime.Now,
                        ActionUser = item.UserName,
                        Amount = item.Amount,
                        InvoiceNo = item.InvoiceNo,
                        Code = item.TransactionCode,
                        Description = item.Description,
                        TransactionDate = item.TransactionDate,
                        Machine = Environment.MachineName,
                        Action_FolioID = item.FolioID,
                        AfterAction_FolioID = item.FolioID,
                        Property = "PMS"
                    };
                    PostingHistoryBO.Instance.Insert(postingHistory);

                    #region lưu transaction từ generate transaction vào folio detail
               
                    if (currentGenConfigs.Count > 0)
                    {
                        bool isVat = false;
                        bool isSvc = false;
                        int indexVat = -1;
                        int indexSvc = -1;
                        for (int i = 0; i < currentGenConfigs.Count; i++)
                        {
                            if (currentGenConfigs[i].GroupCode == "Tax" && currentGenConfigs[i].SubgroupCode == "Tax")
                            {
                                isVat = true;
                                indexVat = i;
                            }
                            else if (currentGenConfigs[i].GroupCode == "Tax" && currentGenConfigs[i].SubgroupCode == "SVC")
                            {
                                isSvc = true;
                                indexSvc = i;
                            }
                        }

                        decimal baseNet = item.AmountMasterBeforeTax;

                        foreach (var genItem in currentGenConfigs)
                        {
                            if (genItem.GroupCode == "Tax" && genItem.SubgroupCode == "Tax")
                            {
                                decimal svcAmount = 0;
                                if (isSvc)
                                {
                                    decimal svcPercent = currentGenConfigs[indexSvc].Percentage / 100m;
                                    svcAmount = baseNet * svcPercent;
                                }
                                decimal taxPercent = genItem.Percentage / 100m;
                                decimal taxAmount = (baseNet + svcAmount) * taxPercent;

                                FolioDetailModel folioSub = new FolioDetailModel
                                {
                                    UserID = item.UserID,
                                    UserName = item.UserName,
                                    ShiftID = item.ShiftID,
                                    CashierNo = item.CashierNo,
                                    ReservationID = item.ReservationID,
                                    OriginReservationID = item.OriginReservationID,
                                    FolioID = item.FolioID,
                                    OriginFolioID = item.OriginFolioID,
                                    InvoiceNo = item.InvoiceNo,
                                    TransactionNo = item.TransactionNo,
                                    ReceiptNo = "",
                                    TransactionDate = item.TransactionDate,
                                    ProfitCenterID = 2,
                                    ProfitCenterCode = "0",
                                    TransactionGroupID = genItem.TransactionGroupID,
                                    TransactionSubgroupID = genItem.TransactionSubGroupID,
                                    GroupCode = genItem.GroupCode,
                                    SubgroupCode = genItem.SubgroupCode,
                                    GroupType = genItem.GroupType,
                                    TransactionCode = genItem.TransactionCodeDetail,
                                    ArticleCode = "",
                                    Status = false,
                                    RowState = isInvoicePosting ? 3 : 2,
                                    PostType = 3,
                                    IsSplit = false,
                                    Quantity = 1,
                                    Price = taxAmount,
                                    Amount = taxAmount,
                                    AmountMaster = taxAmount,
                                    AmountBeforeTax = taxAmount,
                                    AmountMasterBeforeTax = taxAmount,
                                    AmountGross = taxAmount,
                                    AmountMasterGross = taxAmount,
                                    CurrencyID = item.CurrencyID,
                                    CurrencyMaster = item.CurrencyMaster,
                                    Description = genItem.Description,
                                    Reference = "",
                                    Supplement = "",
                                    RoomType = "",
                                    RoomTypeID = 0,
                                    RoomID = item.RoomID,
                                    UserInsertID = item.UserID,
                                    UserUpdateID = item.UserID,
                                    CreateDate = DateTime.Now,
                                    UpdateDate = DateTime.Now,
                                    Property = "",
                                    CheckNo = "",
                                    OriginARNo = "",
                                    IsPostedAR = false,
                                    ARTransID = 0,
                                    IsTransfer = false
                                };
                                FolioDetailBO.Instance.Insert(folioSub);
                            }
                            else if (genItem.GroupCode == "Tax" && genItem.SubgroupCode == "SVC")
                            {
                                decimal svcPercent = genItem.Percentage / 100m;
                                decimal svcAmount = baseNet * svcPercent;

                                FolioDetailModel folioSub = new FolioDetailModel
                                {
                                    UserID = item.UserID,
                                    UserName = item.UserName,
                                    ShiftID = item.ShiftID,
                                    CashierNo = item.CashierNo,
                                    ReservationID = item.ReservationID,
                                    OriginReservationID = item.OriginReservationID,
                                    FolioID = item.FolioID,
                                    OriginFolioID = item.OriginFolioID,
                                    InvoiceNo = item.InvoiceNo,
                                    TransactionNo = item.TransactionNo,
                                    ReceiptNo = "",
                                    TransactionDate = item.TransactionDate,
                                    ProfitCenterID = 2,
                                    ProfitCenterCode = "0",
                                    TransactionGroupID = genItem.TransactionGroupID,
                                    TransactionSubgroupID = genItem.TransactionSubGroupID,
                                    GroupCode = genItem.GroupCode,
                                    SubgroupCode = genItem.SubgroupCode,
                                    GroupType = genItem.GroupType,
                                    TransactionCode = genItem.TransactionCodeDetail,
                                    ArticleCode = "",
                                    Status = false,
                                    RowState = isInvoicePosting ? 3 : 2,
                                    PostType = 3,
                                    IsSplit = false,
                                    Quantity = 1,
                                    Price = svcAmount,
                                    Amount = svcAmount,
                                    AmountMaster = svcAmount,
                                    AmountBeforeTax = svcAmount,
                                    AmountMasterBeforeTax = svcAmount,
                                    AmountGross = svcAmount,
                                    AmountMasterGross = svcAmount,
                                    CurrencyID = item.CurrencyID,
                                    CurrencyMaster = item.CurrencyMaster,
                                    Description = genItem.Description,
                                    Reference = "",
                                    Supplement = "",
                                    RoomType = "",
                                    RoomTypeID = 0,
                                    RoomID = item.RoomID,
                                    UserInsertID = item.UserID,
                                    UserUpdateID = item.UserID,
                                    CreateDate = DateTime.Now,
                                    UpdateDate = DateTime.Now,
                                    Property = "",
                                    CheckNo = "",
                                    OriginARNo = "",
                                    IsPostedAR = false,
                                    ARTransID = 0,
                                    IsTransfer = false
                                };
                                FolioDetailBO.Instance.Insert(folioSub);
                            }
                            else
                            {
                                decimal genPercent = genItem.Percentage / 100m;
                                decimal allocAmount = baseNet * genPercent;
                                FolioDetailModel folioSub = new FolioDetailModel
                                {
                                    UserID = item.UserID,
                                    UserName = item.UserName,
                                    ShiftID = item.ShiftID,
                                    CashierNo = item.CashierNo,
                                    ReservationID = item.ReservationID,
                                    OriginReservationID = item.OriginReservationID,
                                    FolioID = item.FolioID,
                                    OriginFolioID = item.OriginFolioID,
                                    InvoiceNo = item.InvoiceNo,
                                    TransactionNo = item.TransactionNo,
                                    ReceiptNo = "",
                                    TransactionDate = item.TransactionDate,
                                    ProfitCenterID = 2,
                                    ProfitCenterCode = "0",
                                    TransactionGroupID = genItem.TransactionGroupID,
                                    TransactionSubgroupID = genItem.TransactionSubGroupID,
                                    GroupCode = genItem.GroupCode,
                                    SubgroupCode = genItem.SubgroupCode,
                                    GroupType = genItem.GroupType,
                                    TransactionCode = genItem.TransactionCodeDetail,
                                    ArticleCode = "",
                                    Status = false,
                                    RowState = isInvoicePosting ? 3 : 2,
                                    PostType = 3,
                                    IsSplit = false,
                                    Quantity = 1,
                                    Price = allocAmount,
                                    Amount = allocAmount,
                                    AmountMaster = allocAmount,
                                    AmountBeforeTax = allocAmount,
                                    AmountMasterBeforeTax = allocAmount,
                                    AmountGross = allocAmount,
                                    AmountMasterGross = allocAmount,
                                    CurrencyID = item.CurrencyID,
                                    CurrencyMaster = item.CurrencyMaster,
                                    Description = genItem.Description,
                                    Reference = "",
                                    Supplement = "",
                                    RoomType = "",
                                    RoomTypeID = 0,
                                    RoomID = item.RoomID,
                                    UserInsertID = item.UserID,
                                    UserUpdateID = item.UserID,
                                    CreateDate = DateTime.Now,
                                    UpdateDate = DateTime.Now,
                                    Property = "",
                                    CheckNo = "",
                                    OriginARNo = "",
                                    IsPostedAR = false,
                                    ARTransID = 0,
                                    IsTransfer = false
                                };
                                FolioDetailBO.Instance.Insert(folioSub);
                            }
                        }
                    }
                    #endregion
                }

                // CẬP NHẬT LẠI BALANCE
                int reservationID = models.First().ReservationID;
                int folioID = models.First().FolioID;
                decimal balance = FolioDetailBO.CalculateBalance(reservationID);

                FolioModel currentFolio = (FolioModel)FolioBO.Instance.FindByPrimaryKey(folioID);
                if (currentFolio != null) { currentFolio.BalanceVND = balance; FolioBO.Instance.Update(currentFolio); }

                ReservationModel res = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(reservationID);
                if (res != null) { res.BalanceVND = balance; ReservationBO.Instance.Update(res); }

                // COMMIT GIAO DỊCH
                pt.CommitTransaction();
                return Ok(new { success = true, message = "Posting successful!", invoiceNo = batchInvoiceNo });
            }
            catch (Exception ex)
            {
                pt.RollBack();
                return StatusCode(500, new { success = false, message = ex.Message });
            }
            finally
            {
                pt.CloseConnection();
            }
        }
        #endregion

        #region Billing: View Transaction Detail
        [HttpGet]
        public IActionResult GetTransactionDetailBreakdown(int invoiceNo)
        {
            try
            {
                DataTable dataTable = _iPostService.TransactionDetail(invoiceNo);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  ID = !string.IsNullOrEmpty(d["Index"].ToString()) ? d["Index"] : "",
                                  GroupCode = !string.IsNullOrEmpty(d["GroupCode"].ToString()) ? d["GroupCode"] : "",
                                  SubgroupCode = !string.IsNullOrEmpty(d["SubgroupCode"].ToString()) ? d["SubgroupCode"] : "",
                                  PostType = !string.IsNullOrEmpty(d["PostType"].ToString()) ? d["PostType"] : "",
                                  RowState = !string.IsNullOrEmpty(d["RowState"].ToString()) ? d["RowState"] : "",
                                  IsSplit = !string.IsNullOrEmpty(d["IsSplit"].ToString()) ? d["IsSplit"] : "",
                                  InvoiceNo = !string.IsNullOrEmpty(d["InvoiceNo"].ToString()) ? d["InvoiceNo"] : "",
                                  TransactionNo = !string.IsNullOrEmpty(d["TransactionNo"].ToString()) ? d["TransactionNo"] : "",
                                  Date = !string.IsNullOrEmpty(d["Date"].ToString()) ? d["Date"] : "",
                                  Time = !string.IsNullOrEmpty(d["Time"].ToString()) ? d["Time"] : "",
                                  IsVisible = !string.IsNullOrEmpty(d["IsVisible"].ToString()) ? d["IsVisible"] : "",
                                  Code = !string.IsNullOrEmpty(d["Code"].ToString()) ? d["Code"] : "",
                                  Description = !string.IsNullOrEmpty(d["Description"].ToString()) ? d["Description"] : "",
                                  VirtualCode = !string.IsNullOrEmpty(d["VirtualCode"].ToString()) ? d["VirtualCode"] : "",
                                  Amount = !string.IsNullOrEmpty(d["Amount"].ToString()) ? d["Amount"] : "",
                                  Currency = !string.IsNullOrEmpty(d["Currency"].ToString()) ? d["Currency"] : "",
                                  Supplement = !string.IsNullOrEmpty(d["Supplement"].ToString()) ? d["Supplement"] : "",
                                  Reference = !string.IsNullOrEmpty(d["Reference"].ToString()) ? d["Reference"] : "",
                                  UserName = !string.IsNullOrEmpty(d["UserName"].ToString()) ? d["UserName"] : "",
                                  ShiftID = !string.IsNullOrEmpty(d["ShiftID"].ToString()) ? d["ShiftID"] : ""
                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
        #endregion

    }
}
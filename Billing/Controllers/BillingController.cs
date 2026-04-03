using BaseBusiness.BO;
using BaseBusiness.Model;
using BaseBusiness.util;
using Billing.Dto;
using Billing.Services.Interfaces;
using DevExpress.Office.Utils;
using DevExpress.Web;
using DevExpress.Web.Internal;
using DevExpress.XtraReports.Design;
using DevExpress.XtraReports.UI;
using DevExpress.XtraRichEdit.Fields;
using DevExpress.XtraRichEdit.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
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
    public partial class BillingController : Controller
    {
        private const string SplitTransactionProcedureName = "spSplitTransactionByDev";
        private readonly IConfiguration _configuration;
        private readonly ILogger<BillingController> _logger;
        private readonly IMemoryCache _cache;
        private readonly IPostService _iPostService;
        private readonly ITransferTransactionService _iTransferTransactionService;
        private readonly ICrashierService _iCrashierService;
        private readonly IInvoicingService _invoicingService;
        private readonly IAdjustTransactionService _iAdjustTransactionService;
        private readonly IPostingInvoiceService _iPostingInvoiceService;
        public BillingController(ILogger<BillingController> logger,
                IMemoryCache cache, IConfiguration configuration, IPostService iPostService, ITransferTransactionService transferTransactionService, ICrashierService iCrashierService, IInvoicingService invoicingService, IAdjustTransactionService adjustTransaction, IPostingInvoiceService postingInvoiceService)
        {
            _cache = cache;
            _logger = logger;
            _configuration = configuration;
            _iPostService = iPostService;
            _iTransferTransactionService = transferTransactionService;
            _iCrashierService = iCrashierService;
            _invoicingService = invoicingService;
            _iAdjustTransactionService = adjustTransaction;
            _iPostingInvoiceService = postingInvoiceService;
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
                List<ReservationModel> posting = ReservationBO.GetBalanceVND(rsvID);
                if (posting != null && posting.Count > 0)
                {
                    var groupReservations = ReservationBO.Instance.FindByAttribute("ConfirmationNo", posting[0].ConfirmationNo).Cast<ReservationModel>().ToList();
                    decimal totalGrpBalance = 0;
                    foreach (var r in groupReservations)
                    {
                        // Dynamically calculate actual live balance for each room
                        decimal roomLiveBalance = FolioDetailBO.CalculateBalance(r.ID);
                        totalGrpBalance += roomLiveBalance;

                        // Sync it back to the database as a safety net
                        if (r.BalanceVND != roomLiveBalance)
                        {
                            r.BalanceVND = roomLiveBalance;
                            ReservationBO.Instance.Update(r);
                        }
                    }
                    posting[0].BalanceVND = totalGrpBalance;
                }
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

                if (model.IsMasterFolio == true)
                {
                    var masterRes = ReservationBO.Instance.FindByAttribute("ConfirmationNo", res.ConfirmationNo)
                                                          .Cast<ReservationModel>()
                                                          .FirstOrDefault(r => r.PostingMaster == true);
                    if (masterRes != null)
                    {
                        model.ReservationID = masterRes.ID;
                        res = masterRes;
                    }
                }

                List<FolioModel> existingFolios;
                if (model.IsMasterFolio == true)
                    existingFolios = FolioBO.Instance.FindByAttribute("ConfirmationNo", res.ConfirmationNo).Cast<FolioModel>().ToList();
                else
                    existingFolios = FolioBO.Instance.FindByAttribute("ReservationID", model.ReservationID).Cast<FolioModel>().ToList();

                var usedFolioNos = existingFolios.Where(x => x.IsMasterFolio == model.IsMasterFolio)
                                                 .Select(x => x.FolioNo).ToList();

                if (usedFolioNos.Contains(model.FolioNo))
                {
                    return Json(new { code = 1, msg = $"Window {model.FolioNo} is already in use. Please select another window!" });
                }

                if (model.IsMasterFolio == true)
                {
                    if (model.ProfileID <= 0 || model.ProfileID == res.ProfileIndividualId)
                    {
                        model.ProfileID = res.ProfileCompanyId > 0 ? res.ProfileCompanyId : (res.ProfileAgentId > 0 ? res.ProfileAgentId : res.ProfileGroupId);
                        DataTable dtProfile = TextUtils.Select($"SELECT Account FROM Profile WHERE ID = {model.ProfileID}");
                        if (dtProfile.Rows.Count > 0) model.AccountName = dtProfile.Rows[0]["Account"].ToString();
                    }
                }

                model.IsPrintVAT = false;
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

        #region Billing: Split Transaction
        private ActionResult TrySplitTransactionByProcedure(SplitTransactionDto request)
        {
            SqlParameter[] parameters =
            {
                new SqlParameter("@FolioDetailID", SqlDbType.Int) { Value = request.FolioDetailID },
                new SqlParameter("@DiscountAmount", SqlDbType.Decimal) { Precision = 18, Scale = 4, Value = request.DiscountAmount },
                new SqlParameter("@DiscountPercent", SqlDbType.Decimal) { Precision = 18, Scale = 4, Value = request.DiscountPercent },
                new SqlParameter("@Amount", SqlDbType.Decimal) { Precision = 18, Scale = 4, Value = request.Amount },
                new SqlParameter("@UserName", SqlDbType.NVarChar, 255) { Value = request.UserName ?? string.Empty },
                new SqlParameter("@UserID", SqlDbType.Int) { Value = request.UserID },
                new SqlParameter("@ShiftID", SqlDbType.Int) { Value = request.ShiftID },
                new SqlParameter("@ShiftName", SqlDbType.NVarChar, 255) { Value = request.ShiftName ?? string.Empty }
            };

            try
            {
                DataTable result = DataTableHelper.getTableData(SplitTransactionProcedureName, parameters);
                if (result.Rows.Count < 1)
                {
                    return Json(new { code = 0, msg = "Split transaction was successfully " });
                }

                DataRow row = result.Rows[0];
                int code = result.Columns.Contains("Code") ? Convert.ToInt32(row["Code"]) : 0;
                string msg = result.Columns.Contains("Msg")
                    ? row["Msg"]?.ToString() ?? string.Empty
                    : (code == 0 ? "Split transaction was successfully " : "Split transaction failed");

                return Json(new { code, msg });
            }
            catch (Exception ex)
            {
                if (IsMissingStoredProcedure(ex, SplitTransactionProcedureName))
                {
                    return null;
                }

                return Json(new { code = 1, msg = ex.Message });
            }
        }

        private static bool IsMissingStoredProcedure(Exception ex, string procedureName)
        {
            if (ex == null)
            {
                return false;
            }

            string message = ex.Message ?? string.Empty;
            return message.IndexOf("Could not find stored procedure", StringComparison.OrdinalIgnoreCase) >= 0
                && message.IndexOf(procedureName, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        [HttpPost]
        public ActionResult SplitTransaction([FromBody] SplitTransactionDto request)
        {
            if (request == null)
            {
                return Json(new { code = 1, msg = "Request Data Invalid Or NULL" });
            }

            ActionResult splitProcedureResult = TrySplitTransactionByProcedure(request);
            if (splitProcedureResult != null)
            {
                return splitProcedureResult;
            }

            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();
                FolioDetailModel folioDetail = (FolioDetailModel)FolioDetailBO.Instance.FindByPrimaryKey(request.FolioDetailID);
                if (folioDetail == null || folioDetail.ID == 0)
                {
                    return Json(new { code = 0, msg = "Could not find transaction to split " });

                }

                bool splitWholeInvoice = folioDetail.PostType == 3
                    && folioDetail.RowState == 1
                    && folioDetail.IsSplit
                    && !string.IsNullOrWhiteSpace(folioDetail.InvoiceNo);

                List<FolioDetailModel> listFolioDetail = splitWholeInvoice
                    ? PropertyUtils.ConvertToList<FolioDetailModel>(FolioDetailBO.Instance.FindByAttribute("InvoiceNo", folioDetail.InvoiceNo))
                        .OrderBy(x => x.RowState)
                        .ThenBy(x => x.TransactionNo)
                        .ThenBy(x => x.ID)
                        .ToList()
                    : PropertyUtils.ConvertToList<FolioDetailModel>(FolioDetailBO.Instance.FindByAttribute("TransactionNo", folioDetail.TransactionNo))
                        .OrderBy(x => x.RowState)
                        .ThenBy(x => x.ID)
                        .ToList();
                // percentMain là tỉ lệ của transaction sẽ được thêm, percentSub là tỉ lệ của transaction sẽ được update
                decimal percentMain = request.DiscountPercent / 100;
                decimal percentSub = (100 - request.DiscountPercent) / 100;
                if (request.DiscountAmount > 0)
                {
                    percentMain = request.DiscountAmount / request.Amount;
                    percentSub = (request.Amount - request.DiscountAmount) / request.Amount;
                }
                if (listFolioDetail.Count > 0)
                {
                    string newInvoiceNo = (FolioDetailBO.GetTopInvoiceNo() + 1).ToString();
                    int nextTransactionSeed = FolioDetailBO.GetTopTransactioNo();
                    int transactionOffset = 1;
                    Dictionary<string, string> transactionMap = new Dictionary<string, string>();
                    List<BusinessDateModel> businessDateModel = PropertyUtils.ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll());
                    int lastFolioDetailId = listFolioDetail.Last().ID;

                    foreach (string sourceTransactionNo in listFolioDetail
                        .Select(x => x.TransactionNo)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Distinct())
                    {
                        transactionMap[sourceTransactionNo] = (nextTransactionSeed + transactionOffset).ToString();
                        transactionOffset++;
                    }

                    string fallbackTransactionNo = (nextTransactionSeed + transactionOffset).ToString();

                    foreach (var item in listFolioDetail)
                    {
                        #region Insert 1 transaction theo transaction split 
                        FolioDetailModel folioDetailMain = (FolioDetailModel)item.Clone();
                        string clonedTransactionNo = transactionMap.ContainsKey(item.TransactionNo)
                            ? transactionMap[item.TransactionNo]
                            : fallbackTransactionNo;

                        folioDetailMain.Price = Math.Round(item.Price * percentMain);
                        folioDetailMain.Amount = Math.Round(item.Amount * percentMain);
                        folioDetailMain.AmountMaster = Math.Round(item.AmountMaster * percentMain);
                        folioDetailMain.AmountBeforeTax = Math.Round(item.AmountBeforeTax * percentMain);
                        folioDetailMain.AmountMasterBeforeTax = Math.Round(item.AmountMasterBeforeTax * percentMain);
                        folioDetailMain.AmountGross = Math.Round(item.AmountGross * percentMain);
                        folioDetailMain.AmountMasterGross = Math.Round(item.AmountMasterGross * percentMain);
                        folioDetailMain.ShiftID = request.ShiftID;
                        folioDetailMain.CashierNo = request.ShiftName?.ToString() ?? "";
                        folioDetailMain.UserID = request.UserID;
                        folioDetailMain.UserName = request.UserName;
                        folioDetailMain.ReservationID = folioDetailMain.OriginReservationID = item.ReservationID;
                        folioDetailMain.FolioID = folioDetailMain.OriginFolioID = item.FolioID;
                        folioDetailMain.InvoiceNo = newInvoiceNo;
                        folioDetailMain.TransactionNo = clonedTransactionNo;
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
                        folioDetailMain.UserInsertID = folioDetailMain.UserUpdateID = request.UserID;
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
                            item.Reference = $"{item.AmountMaster} split in to {item.AmountGross - folioDetailMain.AmountGross}";
                        }
                        item.Price = item.Price - folioDetailMain.Price;
                        item.Amount = item.Amount - folioDetailMain.Amount;
                        item.AmountMaster = item.AmountMaster - folioDetailMain.AmountMaster;
                        item.AmountBeforeTax = item.AmountBeforeTax - folioDetailMain.AmountBeforeTax;
                        item.AmountMasterBeforeTax = item.AmountMasterBeforeTax - folioDetailMain.AmountMasterBeforeTax;
                        item.AmountGross = item.AmountGross - folioDetailMain.AmountGross;
                        item.AmountMasterGross = item.AmountMasterGross - folioDetailMain.AmountMasterGross;
                        item.UserUpdateID = request.UserID;
                        item.UpdateDate = DateTime.Now;
                        FolioDetailBO.Instance.Update(item);
                        #endregion

                        #region lưu posting history 
                        if (folioDetailMain.IsSplit == true)
                        {
                            PostingHistoryModel postingHistory = new PostingHistoryModel();
                            postingHistory.ActionType = 6;
                            postingHistory.ActionText = $"[SPLIT_TRANSACTION] - {folioDetailMain.TransactionCode} from {folioDetailMain.Reference}";
                            postingHistory.ActionDate = DateTime.Now;
                            postingHistory.ActionUser = request.UserName;
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
                            postingHistory.ActionUser = request.UserName;
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
                        if (item.ID == lastFolioDetailId)
                        {
                            int reservationID = folioDetail.ReservationID;
                            decimal balance = FolioDetailBO.CalculateBalance(reservationID);
                            ReservationModel mainRes = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(reservationID);
                            if (mainRes != null)
                            {
                                var groupRes = PropertyUtils.ConvertToList<ReservationModel>(ReservationBO.Instance.FindByAttribute("ConfirmationNo", mainRes.ConfirmationNo.ToString()));
                                foreach (var gRes in groupRes)
                                {
                                    if (gRes.BalanceVND != balance)
                                    {
                                        gRes.BalanceVND = balance;
                                        ReservationBO.Instance.Update(gRes);
                                    }

                                    var groupFolios = PropertyUtils.ConvertToList<FolioModel>(FolioBO.Instance.FindByAttribute("ReservationID", gRes.ID));
                                    foreach (var gFolio in groupFolios)
                                    {
                                        if (gFolio.BalanceVND != balance)
                                        {
                                            gFolio.BalanceVND = balance;
                                            FolioBO.Instance.Update(gFolio);
                                        }
                                    }
                                }
                            }
                        }
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
                        folioMaster.BalanceVND = folioMaster.BalanceVND - balance;
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
        public async Task<IActionResult> CheckAdjustCode(string invoiceNo)
        {
            try
            {
                SqlParameter[] param =
                [
                    new SqlParameter("@sqlCommand",
                    $@"  select   a.TransactionNo,
                            a.TransactionCode+' - '+a.Description as Infor,
                            a.Amount,
                            a.CurrencyID,
                            a.ProfitCenterID,
                            a.ProfitCenterCode,
                            a.RoomTypeID,
                            a.RoomType,
                            c.Code as AdjCode,
                            a.RowState,
                            a.PostType
                        from FolioDetail as a WITH (NOLOCK),
                            Transactions as b WITH (NOLOCK),
                            Transactions as c  WITH (NOLOCK) 
                        where a.TransactionCode=b.Code 
                            and b.AdjustmentCode=c.Code 
                            and a.InvoiceNo ='{invoiceNo}' order by a.ID")
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

        //[HttpPost]
        // [HttpGet]
        // public async Task<IActionResult> CheckAdjustCode(string transactionCode)
        // {
        //     try
        //     {
        //         List<TransactionsModel> trans = PropertyUtils.ConvertToList<TransactionsModel>(TransactionsBO.Instance.FindByAttribute("Code", transactionCode));
        //         if (trans.Count < 1)
        //         {
        //             return Json(new
        //             {
        //                 code = 1,
        //                 msg = "Could not find transactions"
        //             });
        //         }
        //         if (trans[0].AdjustmentCode == "" || string.IsNullOrEmpty(trans[0].AdjustmentCode))
        //         {
        //             return Json(new
        //             {
        //                 code = 1,
        //                 msg = "Adjustment Code could not find"
        //             });
        //         }
        //         return Json(new
        //         {
        //             code = 0,
        //             msg = ""
        //         });
        //     }
        //     catch (Exception ex)
        //     {
        //         return Json(ex.Message);
        //     }
        // }

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
                if (!int.TryParse(Request.Form["postType"].ToString(), out int postType)) postType = 1;
                if (!int.TryParse(Request.Form["folioDetailAdjustID"].ToString(), out int folioDetailAdjustID)) folioDetailAdjustID = 0;

                FolioDetailModel folioAdjust = (FolioDetailModel)FolioDetailBO.Instance.FindByPrimaryKey(folioDetailAdjustID);
                if (folioAdjust == null || folioAdjust.ID == 0)
                {
                    return Json(new { code = 0, msg = "Could not find transaction" });

                }

                if (postType == 1)
                {
                    if (!decimal.TryParse(Request.Form["adjustAmount"].ToString(), out price)) price = 0;
                    if (!decimal.TryParse(Request.Form["adjustNet"].ToString(), out priceNet)) priceNet = 0;
                }
                else
                {
                    if (!decimal.TryParse(Request.Form["percentage"].ToString(), out decimal percentage)) percentage = 0;
                    price = folioAdjust.AmountBeforeTax * (percentage / 100);
                    priceNet = folioAdjust.AmountGross * (percentage / 100);
                }

                // Adjustments decrease balance, so we make sure amount is negative
                price = -Math.Abs(price);
                priceNet = -Math.Abs(priceNet);
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
                if (!int.TryParse(Request.Form["folioID"].ToString(), out int folioID)) folioID = 0;
                FolioModel folio = (FolioModel)FolioBO.Instance.FindByPrimaryKey(folioID);
                if (folio == null || folio.ID == 0)
                {
                    return Json(new { code = 1, msg = $"Could not find Folio. Please check Folio" });

                }

                if (folio.Status == true)
                {
                    return Json(new { code = 1, msg = $"Can not post. Folio has been being locked" });

                }
                if (!int.TryParse(Request.Form["shiftID"].ToString(), out int shiftID)) shiftID = 0;
                string shiftName = Request.Form["shiftName"].ToString();
                #region lưu transaction chính vào folio detail
                // kiểm tra xem transaction chọn để post có article không
                FolioDetailModel folioArticle = new FolioDetailModel();
                if (!int.TryParse(Request.Form["userID"].ToString(), out int userID)) userID = 0;
                folioArticle.UserID = userID;
                folioArticle.ShiftID = shiftID;
                folioArticle.CashierNo = shiftName;
                folioArticle.UserName = Request.Form["userName"].ToString();
                if (!int.TryParse(Request.Form["rsvID"].ToString(), out int rsvID)) rsvID = 0;
                folioArticle.ReservationID = folioArticle.OriginReservationID = rsvID;
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
                folioArticle.TransactionCode = trans[0].Code;
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
                folioArticle.UserInsertID = folioArticle.UserUpdateID = userID;
                folioArticle.CreateDate = folioArticle.UpdateDate = DateTime.Now;
                if (!int.TryParse(Request.Form["roomID"].ToString(), out int roomID)) roomID = 0;
                folioArticle.RoomID = roomID;
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
                decimal balance = FolioDetailBO.CalculateBalance(rsvID);
                ReservationModel mainRes = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(rsvID);
                if (mainRes != null)
                {
                    var groupRes = PropertyUtils.ConvertToList<ReservationModel>(ReservationBO.Instance.FindByAttribute("ConfirmationNo", mainRes.ConfirmationNo.ToString()));
                    foreach (var gRes in groupRes)
                    {
                        gRes.BalanceVND = balance;
                        ReservationBO.Instance.Update(gRes);

                        var groupFolios = PropertyUtils.ConvertToList<FolioModel>(FolioBO.Instance.FindByAttribute("ReservationID", gRes.ID));
                        foreach (var gFolio in groupFolios)
                        {
                            gFolio.BalanceVND = balance;
                            FolioBO.Instance.Update(gFolio);
                        }
                    }
                }
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
        public IActionResult GetTransactionSubGroups(int groupId)
        {
            try
            {
                string sql = string.Format(@"select ID, Description from TransactionSubgroup where TransactionGroupID={0} 
                            and Description like N'%%' order by Description", groupId);

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
            select Code, Description, DefaultPrice, TaxInclude
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
                                  DefaultPrice = dt.Columns.Contains("DefaultPrice") && r["DefaultPrice"] != DBNull.Value ? r["DefaultPrice"] : 0,
                                  TaxInclude = dt.Columns.Contains("TaxInclude") && r["TaxInclude"] != DBNull.Value && Convert.ToBoolean(r["TaxInclude"])
                              }).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Trả về danh sách invoice code (GroupType = 2) để FE F3 chọn riêng như WinForm.
        /// </summary>
        [HttpGet]
        public IActionResult GetInvoiceTransactions()
        {
            try
            {
                string sql = @"
                    select Code, Description
                    from Transactions
                    where GroupType = 2
                      and IsActive = 1
                    order by Code";

                DataTable dt = TextUtils.Select(sql);

                var result = (from r in dt.AsEnumerable()
                              select new
                              {
                                  Code = !string.IsNullOrEmpty(r["Code"].ToString()) ? r["Code"] : "",
                                  Description = !string.IsNullOrEmpty(r["Description"].ToString()) ? r["Description"] : ""
                              }).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Trả về invoice code mặc định từ ConfigSystem.CODE_INVOICE để F3 dùng ngầm như WinForm.
        /// </summary>
        [HttpGet]
        public IActionResult GetDefaultInvoiceTransaction()
        {
            try
            {
                string sql = @"
                    select top 1
                        cs.KeyValue as Code,
                        t.Description
                    from ConfigSystem cs
                    left join Transactions t on t.Code = cs.KeyValue
                    where cs.KeyName = 'CODE_INVOICE'";

                DataTable dt = TextUtils.Select(sql);
                if (dt.Rows.Count == 0)
                {
                    return Json(new { error = "Config CODE_INVOICE not found." });
                }

                string code = dt.Rows[0]["Code"]?.ToString() ?? string.Empty;
                string description = dt.Rows[0]["Description"]?.ToString() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(code))
                {
                    return Json(new { error = "Config CODE_INVOICE is empty." });
                }

                Expression exp = new Expression("GroupType", 2, "=");
                exp = exp.And(new Expression("IsActive", 1, "="));
                exp = exp.And(new Expression("Code", code, "="));
                ArrayList arrTrans = TransactionsBO.Instance.FindByExpression(exp);

                if (arrTrans == null || arrTrans.Count == 0)
                {
                    return Json(new { error = "Default invoice code is invalid or inactive." });
                }

                TransactionsModel invoiceTransaction = (TransactionsModel)arrTrans[0];

                return Json(new
                {
                    Code = invoiceTransaction.Code,
                    Description = invoiceTransaction.Description ?? description
                });
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

        private sealed class InvoicePostingMetadata
        {
            public string MasterTransactionCode { get; set; }
            public string InvoiceDescription { get; set; }
            public string InvoiceReference { get; set; }
            public string InvoiceSupplement { get; set; }
        }

        private static InvoicePostingMetadata ParseInvoicePostingMetadata(string rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return new InvoicePostingMetadata();
            }

            try
            {
                return JsonSerializer.Deserialize<InvoicePostingMetadata>(rawValue) ?? new InvoicePostingMetadata();
            }
            catch
            {
                return new InvoicePostingMetadata
                {
                    MasterTransactionCode = rawValue.Trim()
                };
            }
        }

        // [HttpPost]
        // public IActionResult PostingSave([FromBody] PostingRequest request)
        // {
        //     ProcessTransactions pt = new ProcessTransactions();
        //     try
        //     {
        //         if (request.Details == null || request.Details.Count == 0)
        //             return BadRequest("No data received.");

        //         pt.OpenConnection();
        //         pt.BeginTransaction();

        //         var firstItem = request.Details.First();
        //         int reservationID = firstItem.ReservationID;
        //         DateTime businessDate = TextUtils.GetBusinessDate();
        //         DateTime sysDate = DateTime.Now;
        //         string currencyLocal = "VND";

        //         int nextTransNo = FolioDetailBO.GetTopTransactioNo();
        //         int count = 0;
        //         bool isInvoicePosting = request.Details.Any(m => m.PostType == 3);
        //         decimal exchangeRate = 1;

        //         int nextInvoiceNo = FolioDetailBO.GetTopInvoiceNo() + 1;
        //         string batchInvoiceNo = nextInvoiceNo.ToString();

        //         int dbFolioID;

        //         if (request.CurrentFolioID > 0)
        //         {
        //             // kiểm tra có đổi window không
        //             var currentFolio = (FolioModel)FolioBO.Instance.FindByPrimaryKey(request.CurrentFolioID);

        //             int currentFolioNo = currentFolio.IsMasterFolio
        //                 ? -currentFolio.FolioNo
        //                 : currentFolio.FolioNo;

        //             if (currentFolioNo == request.FolioNo)
        //             {
        //                 dbFolioID = request.CurrentFolioID;
        //             }
        //             else
        //             {
        //                 // user đổi window → check / create
        //                 dbFolioID = EnsureFolio(reservationID, request.FolioNo, pt);
        //             }
        //         }
        //         else
        //         {
        //             dbFolioID = EnsureFolio(reservationID, request.FolioNo, pt);
        //         }

        //         // 2. NẾU LÀ POST INVOICE (F3) -> TẠO DÒNG MASTER (RowState = 1)
        //         if (isInvoicePosting)
        //         {
        //             if (string.IsNullOrEmpty(request.MasterCode))
        //                 throw new Exception("Master Transaction Code is required for Invoice Posting.");

        //             var masterTransList = TransactionsBO.Instance.FindByAttribute("Code", request.MasterCode);
        //             if (masterTransList == null || masterTransList.Count == 0)
        //                 throw new Exception("Master Transaction Code not found: " + request.MasterCode);

        //             var mT_Group = (TransactionsModel)masterTransList[0];
        //             decimal totalAmount = request.Details.Sum(m => m.Amount);
        //             decimal totalAmountBeforeTax = request.Details.Sum(m => m.AmountBeforeTax);

        //             decimal totalGross = request.Details.Sum(m => m.AmountGross);

        //             // Tính AmountMaster cho dòng tổng
        //             decimal totalAmountMaster = TextUtils.ExchangeCurrency(businessDate, firstItem.CurrencyID, currencyLocal, totalAmount);
        //             // Tính tỷ giá thực tế để áp cho các dòng detail phía sau 
        //             if (totalAmount != 0) exchangeRate = totalAmountMaster / totalAmount;

        //             var invoiceMeta = ParseInvoicePostingMetadata(firstItem.Property);

        //             FolioDetailModel masterLine = new FolioDetailModel
        //             {
        //                 UserID = firstItem.UserID,
        //                 UserName = firstItem.UserName,
        //                 ShiftID = firstItem.ShiftID,

        //                 InvoiceNo = batchInvoiceNo,
        //                 CashierNo = firstItem.CashierNo,
        //                 FolioID = dbFolioID,
        //                 OriginFolioID = dbFolioID,
        //                 ReservationID = reservationID,
        //                 OriginReservationID = reservationID,
        //                 RoomID = firstItem.RoomID,
        //                 RoomType = firstItem.RoomType,
        //                 RoomTypeID = firstItem.RoomTypeID,
        //                 TransactionDate = businessDate,
        //                 CreateDate = sysDate,
        //                 UpdateDate = sysDate,
        //                 TransactionCode = mT_Group.Code,
        //                 Description = !string.IsNullOrEmpty(invoiceMeta.InvoiceDescription) ? invoiceMeta.InvoiceDescription : mT_Group.Description,
        //                 TransactionGroupID = mT_Group.TransactionGroupID,
        //                 GroupCode = mT_Group.GroupCode,
        //                 TransactionSubgroupID = mT_Group.TransactionSubGroupID,
        //                 SubgroupCode = mT_Group.SubgroupCode,
        //                 GroupType = mT_Group.GroupType,

        //                 Quantity = 1,
        //                 Price = totalAmount,
        //                 Amount = totalAmount,
        //                 AmountMaster = totalAmountMaster,
        //                 AmountGross = totalGross,
        //                 AmountMasterGross = totalGross * exchangeRate,
        //                 AmountBeforeTax = totalAmountBeforeTax,
        //                 AmountMasterBeforeTax = totalAmountBeforeTax * exchangeRate,
        //                 CurrencyID = !string.IsNullOrEmpty(firstItem.CurrencyID)
        //                 ? firstItem.CurrencyID
        //                 : currencyLocal,
        //                 CurrencyMaster = currencyLocal,

        //                 Reference = !string.IsNullOrEmpty(invoiceMeta.InvoiceReference) ? invoiceMeta.InvoiceReference : firstItem.Reference,
        //                 Supplement = !string.IsNullOrEmpty(invoiceMeta.InvoiceSupplement) ? invoiceMeta.InvoiceSupplement : firstItem.Supplement,
        //                 CheckNo = firstItem.CheckNo,

        //                 RowState = 1,
        //                 PostType = 3,
        //                 IsSplit = true,
        //                 Status = false,
        //                 ProfitCenterID = 2,
        //                 IsTransfer = false,
        //                 IsPostedAR = false,
        //                 Property = "",
        //                 OriginARNo = "",
        //                 UserInsertID = firstItem.UserInsertID,
        //                 UserUpdateID = firstItem.UserUpdateID
        //             };

        //             int masterLineID = (int)pt.Insert(masterLine);
        //             batchInvoiceNo = masterLineID.ToString();


        //             // Cập nhật InvoiceNo đồng bộ
        //             string sqlUpdMaster = string.Format("UPDATE FolioDetail SET InvoiceNo = '{0}', TransactionNo = '{0}' WHERE ID = {1}", batchInvoiceNo, masterLineID);
        //             pt.UpdateCommand(sqlUpdMaster);

        //             // History cho Master Line
        //             PostingHistoryModel phMaster = new PostingHistoryModel
        //             {
        //                 ActionType = 0,
        //                 ActionText = $"[POST_INV_MASTER] - {masterLine.TransactionCode} - {masterLine.Description}",
        //                 ActionDate = DateTime.Now,
        //                 ActionUser = masterLine.UserName,
        //                 Amount = masterLine.Amount,
        //                 InvoiceNo = batchInvoiceNo,
        //                 Code = masterLine.TransactionCode,
        //                 Description = masterLine.Description,
        //                 TransactionDate = masterLine.TransactionDate,
        //                 Machine = Environment.MachineName,
        //                 Action_FolioID = masterLine.FolioID,
        //                 AfterAction_FolioID = masterLine.FolioID,
        //                 Property = "PMS"
        //             };
        //             PostingHistoryBO.Instance.Insert(phMaster);
        //             count++;
        //         }

        //         // CÁC DÒNG DETAIL (RowState = 2)
        //         foreach (var item in request.Details)
        //         {
        //             if (string.IsNullOrEmpty(item.CurrencyID))
        //             {
        //                 item.CurrencyID = !string.IsNullOrEmpty(firstItem.CurrencyID)
        //                     ? firstItem.CurrencyID
        //                     : currencyLocal; // fallback luôn VND
        //             }
        //             item.FolioID = dbFolioID;
        //             item.ShiftID = firstItem.ShiftID;
        //             item.CashierNo = firstItem.CashierNo;

        //             item.InvoiceNo = batchInvoiceNo;
        //             item.TransactionNo = (nextTransNo + count).ToString();
        //             item.TransactionDate = businessDate;
        //             item.CreateDate = sysDate;
        //             item.UpdateDate = sysDate;
        //             item.RowState = isInvoicePosting ? 2 : 1;
        //             item.RoomID = item.RoomID > 0 ? item.RoomID : firstItem.RoomID;

        //             if (item.AmountGross == 0)
        //             {
        //                 item.AmountGross = item.Amount;
        //             }
        //             item.AmountMaster = item.Amount * exchangeRate;
        //             item.AmountMasterGross = item.AmountGross * exchangeRate;
        //             item.AmountMasterBeforeTax = item.AmountBeforeTax * exchangeRate;
        //             item.CurrencyMaster = currencyLocal;
        //             item.UserInsertID = item.UserInsertID;
        //             item.UserUpdateID = item.UserUpdateID;

        //             List<GenerateTransactionModel> genConfigs = PropertyUtils.ConvertToList<GenerateTransactionModel>(
        //                 GenerateTransactionBO.Instance.FindByAttribute("TransactionCode", item.TransactionCode));

        //             item.IsSplit = genConfigs.Count > 0;

        //             var transListDetail = TransactionsBO.Instance.FindByAttribute("Code", item.TransactionCode);

        //             if (transListDetail != null && transListDetail.Count > 0)
        //             {
        //                 var tInfo = (TransactionsModel)transListDetail[0];
        //                 item.TransactionGroupID = tInfo.TransactionGroupID;
        //                 item.GroupCode = tInfo.GroupCode;
        //                 item.TransactionSubgroupID = tInfo.TransactionSubGroupID;
        //                 item.SubgroupCode = tInfo.SubgroupCode;
        //             }
        //             else
        //             {
        //                 throw new Exception("Transaction not found: " + item.TransactionCode);
        //             }

        //             var roomTypeList = RoomTypeBO.Instance.FindByAttribute("Code", item.RoomType);
        //             if (roomTypeList != null && roomTypeList.Count > 0)
        //             {
        //                 var roomTypeInfo = (RoomTypeModel)roomTypeList[0];
        //                 item.RoomTypeID = roomTypeInfo.ID;
        //             }

        //             pt.Insert(item);

        //             PostingHistoryModel postingHistory = new PostingHistoryModel
        //             {
        //                 ActionType = 0,
        //                 ActionText = $"[POST_GEN] - {item.TransactionCode} - {item.Description}",
        //                 ActionDate = DateTime.Now,
        //                 ActionUser = item.UserName,
        //                 Amount = item.Amount,
        //                 InvoiceNo = item.InvoiceNo,
        //                 Code = item.TransactionCode,
        //                 Description = item.Description,
        //                 TransactionDate = item.TransactionDate,
        //                 Machine = Environment.MachineName,
        //                 Action_FolioID = item.FolioID,
        //                 AfterAction_FolioID = item.FolioID,
        //                 Property = "PMS"
        //             };
        //             PostingHistoryBO.Instance.Insert(postingHistory);

        //             count++;

        //             // 4. TÁCH THUẾ/PHÍ SVC (RowState = 3)
        //             if (genConfigs.Count > 0)
        //             {
        //                 decimal baseNet = item.AmountBeforeTax > 0 ? item.AmountBeforeTax : item.Amount;
        //                 foreach (var genItem in genConfigs)
        //                 {
        //                     decimal calcAmount = Math.Round((baseNet * genItem.Percentage) / 100m, 0);

        //                     // Lấy Group/Subgroup thông tin
        //                     var transList = TransactionsBO.Instance.FindByAttribute("Code", genItem.TransactionCodeDetail);

        //                     int groupID = 0;
        //                     string groupCode = "";
        //                     int subGroupID = 0;
        //                     string subGroupCode = "";

        //                     if (transList != null && transList.Count > 0)
        //                     {
        //                         var tInfo = (TransactionsModel)transList[0];
        //                         groupID = tInfo.TransactionGroupID;
        //                         groupCode = tInfo.GroupCode;
        //                         subGroupID = tInfo.TransactionSubGroupID;
        //                         subGroupCode = tInfo.SubgroupCode;
        //                     }

        //                     FolioDetailModel taxLine = new FolioDetailModel
        //                     {
        //                         UserID = item.UserID,
        //                         UserName = item.UserName,
        //                         FolioID = dbFolioID,
        //                         ShiftID = item.ShiftID,
        //                         CashierNo = item.CashierNo,

        //                         OriginFolioID = dbFolioID,
        //                         ReservationID = reservationID,
        //                         OriginReservationID = reservationID,
        //                         InvoiceNo = batchInvoiceNo,
        //                         TransactionNo = (nextTransNo + count).ToString(),
        //                         TransactionDate = businessDate,
        //                         RoomID = item.RoomID,
        //                         TransactionCode = genItem.TransactionCodeDetail,
        //                         Description = genItem.Description,

        //                         TransactionGroupID = groupID,
        //                         GroupCode = groupCode,
        //                         TransactionSubgroupID = subGroupID,
        //                         SubgroupCode = subGroupCode,

        //                         Quantity = 1,
        //                         Price = calcAmount,
        //                         Amount = calcAmount,
        //                         AmountMaster = calcAmount * exchangeRate,
        //                         AmountGross = calcAmount,
        //                         AmountBeforeTax = calcAmount,
        //                         AmountMasterBeforeTax = calcAmount * exchangeRate,

        //                         CurrencyID = item.CurrencyID,
        //                         CurrencyMaster = currencyLocal,

        //                         RowState = 3,
        //                         PostType = 3,
        //                         Status = false,
        //                         ProfitCenterID = 2,

        //                         CreateDate = sysDate,
        //                         UpdateDate = sysDate,
        //                         UserInsertID = item.UserInsertID,
        //                         UserUpdateID = item.UserUpdateID
        //                     };
        //                     count++;
        //                     pt.Insert(taxLine);

        //                     PostingHistoryBO.Instance.Insert(new PostingHistoryModel
        //                     {
        //                         ActionType = 0,
        //                         ActionText = $"[POST_TAX] - {taxLine.TransactionCode} - {taxLine.Description}",
        //                         ActionDate = DateTime.Now,
        //                         ActionUser = taxLine.UserName,
        //                         Amount = taxLine.Amount,
        //                         InvoiceNo = taxLine.InvoiceNo,
        //                         Code = taxLine.TransactionCode,
        //                         Description = taxLine.Description,
        //                         TransactionDate = taxLine.TransactionDate,
        //                         Machine = Environment.MachineName,
        //                         Action_FolioID = taxLine.FolioID,
        //                         AfterAction_FolioID = taxLine.FolioID,
        //                         Property = "PMS"
        //                     });
        //                 }
        //             }
        //         }

        //         // 4. CẬP NHẬT BALANCE (SỐ DƯ)
        //         // A. Tính Balance riêng cho Folio hiện tại 
        //         string sqlCalcFolio = string.Format("SELECT SUM(Amount) FROM FolioDetail WITH (NOLOCK) WHERE FolioID = {0}", dbFolioID);
        //         DataTable dtFolio = TextUtils.Select(sqlCalcFolio);
        //         decimal folioBalance = (dtFolio.Rows.Count > 0 && dtFolio.Rows[0][0] != DBNull.Value)
        //                                ? Convert.ToDecimal(dtFolio.Rows[0][0]) : 0;

        //         // B. Tính Balance tổng cho cả Reservation (để hiện tổng nợ của khách trên toàn bộ các folio)
        //         decimal resBalance = FolioDetailBO.CalculateBalance(reservationID);

        //         // C. Cập nhật vào DB dùng pt.UpdateCommand để bảo đảm Transaction
        //         string sqlUpdFolio = string.Format(
        //             "UPDATE Folio SET BalanceVND = {0} WHERE ID = {1}",
        //             folioBalance.ToString(System.Globalization.CultureInfo.InvariantCulture), dbFolioID);
        //         pt.UpdateCommand(sqlUpdFolio);

        //         string sqlUpdRes = string.Format(
        //             "UPDATE Reservation SET BalanceVND = {0} WHERE ID = {1}",
        //             resBalance.ToString(System.Globalization.CultureInfo.InvariantCulture), reservationID);
        //         pt.UpdateCommand(sqlUpdRes);

        //         pt.CommitTransaction();
        //         return Ok(new { success = true, message = "Posted successfully!", invoiceNo = batchInvoiceNo });
        //     }
        //     catch (Exception ex)
        //     {
        //         if (pt.Transaction != null && pt.Transaction.Connection != null)
        //         {
        //             pt.RollBack();
        //         }
        //         return StatusCode(500, new { success = false, message = ex.Message });
        //     }
        //     finally { pt.CloseConnection(); }
        // }

        private int EnsureFolio(int reservationId, int folioNo, ProcessTransactions pt)
        {
            bool isMaster = folioNo < 0;
            int realFolioNo = Math.Abs(folioNo);

            string sql = string.Format(@"SELECT TOP 1 ID FROM Folio WITH (NOLOCK) WHERE ReservationID = {0} AND FolioNo = {1} AND IsMasterFolio = {2}",
                                        reservationId, realFolioNo, isMaster ? 1 : 0);

            DataTable dt = TextUtils.Select(sql);
            if (dt != null && dt.Rows.Count > 0) return Convert.ToInt32(dt.Rows[0]["ID"]);

            FolioModel newFolio = new FolioModel
            {
                ReservationID = reservationId,
                FolioNo = realFolioNo,
                IsMasterFolio = isMaster,
                Status = false,
                FolioDate = TextUtils.GetBusinessDate(),
                BalanceVND = 0,
                CreateDate = DateTime.Now,
                UpdateDate = DateTime.Now
            };

            return (int)pt.Insert(newFolio);
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

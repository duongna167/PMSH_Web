using BaseBusiness.BO;
using BaseBusiness.Model;
using BaseBusiness.util;
using Cashiering.Commons.Helpers;
using Cashiering.Services.Interfaces;
using DevExpress.ClipboardSource.SpreadsheetML;
using DevExpress.XtraReports.UI;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Cashiering.Controllers
{
    public class CashieringController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<CashieringController> _logger;
        private readonly IMemoryCache _cache;
        private readonly ICashieringService _iCashieringService;
        private readonly ICloseShiftService _iCloseShiftService;
        private readonly ICashieringManagerService _iCashieringManagerService;
        private readonly IFolioVATSearchService _iFolioVATSearchService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public CashieringController(ILogger<CashieringController> logger, IFolioVATSearchService iFolioVATService,
                IMemoryCache cache, IConfiguration configuration, ICashieringService iCashieringService, IHttpContextAccessor httpContextAccessor, ICloseShiftService iCloseShiftService, ICashieringManagerService iCashieringManagerService)
        {
            _cache = cache;
            _logger = logger;
            _configuration = configuration;
            _iCashieringService = iCashieringService;
            _httpContextAccessor = httpContextAccessor;
            _iCloseShiftService = iCloseShiftService;
            _iCashieringManagerService = iCashieringManagerService;
            _iFolioVATSearchService = iFolioVATService;
        }

        public IActionResult Index()
        {
            return View(); // View này sẽ chứa DataGrid + script gọi API
        }
        public IActionResult PostingJournal()
        {
            List<TransactionGroupModel> tlplist = PropertyUtils.ConvertToList<TransactionGroupModel>(TransactionGroupBO.Instance.FindAll());
            ViewBag.TransactionGroupList = tlplist; // Truyền danh sách TransactionGroupModel vào ViewBag để sử dụng trong View
            List<TransactionSubGroupModel> transg = PropertyUtils.ConvertToList<TransactionSubGroupModel>(TransactionSubGroupBO.Instance.FindAll());
            ViewBag.TransactionSubGroupList = transg; // Truyền danh sách TransactionSubGroupModel vào ViewBag để sử dụng trong View
            List<UsersModel> user = PropertyUtils.ConvertToList<UsersModel>(UsersBO.Instance.FindAll());
            ViewBag.UsersList = user;
            List<TransactionsModel> trans = PropertyUtils.ConvertToList<TransactionsModel>(TransactionsBO.Instance.FindAll());
            ViewBag.TransactionsList = trans;
            return View(); // View này sẽ chứa DataGrid + script gọi API
        }

        [HttpGet]
        public IActionResult GetPostingJournal(
            string cashierNo,
            string transactionCodeList,
            string roomNoList,
            DateTime fromDate,
            DateTime toDate,
            string fromProfitCode,
            string toProfitCode,
            string groupID,
            string subgroupID)
        {
            try
            {
                DataTable dataTable = _iCashieringService.PostingJournal(cashierNo, transactionCodeList, roomNoList, fromDate, toDate, fromProfitCode, toProfitCode, groupID, subgroupID);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  ConfirmationNo = !string.IsNullOrEmpty(d["ConfirmationNo"].ToString()) ? d["ConfirmationNo"] : "",
                                  CRSNo = !string.IsNullOrEmpty(d["CRSNo"].ToString()) ? d["CRSNo"] : "",
                                  FolioNo = !string.IsNullOrEmpty(d["FolioNo"].ToString()) ? d["FolioNo"] : "",
                                  AccountName = !string.IsNullOrEmpty(d["AccountName"].ToString()) ? d["AccountName"] : "",
                                  Room = !string.IsNullOrEmpty(d["Room"].ToString()) ? d["Room"] : "",
                                  SubGroupCode = !string.IsNullOrEmpty(d["SubGroupCode"].ToString()) ? d["SubGroupCode"] : "",
                                  GroupCode = !string.IsNullOrEmpty(d["GroupCode"].ToString()) ? d["GroupCode"] : "",
                                  TransactionCode = !string.IsNullOrEmpty(d["TransactionCode"].ToString()) ? d["TransactionCode"] : "",
                                  Description = !string.IsNullOrEmpty(d["Description"].ToString()) ? d["Description"] : "",
                                  Supplement = !string.IsNullOrEmpty(d["Supplement"].ToString()) ? d["Supplement"] : "",
                                  Reference = !string.IsNullOrEmpty(d["Reference"].ToString()) ? d["Reference"] : "",
                                  Time = !string.IsNullOrEmpty(d["Time"].ToString()) ? d["Time"] : "",
                                  TransactionDate = !string.IsNullOrEmpty(d["TransactionDate"].ToString()) ? d["TransactionDate"] : "",
                                  ReservationID = !string.IsNullOrEmpty(d["ReservationID"].ToString()) ? d["ReservationID"] : "",
                                  WinNo = !string.IsNullOrEmpty(d["WinNo"].ToString()) ? d["WinNo"] : "",
                                  CashierNo = !string.IsNullOrEmpty(d["CashierNo"].ToString()) ? d["CashierNo"] : "",
                                  ShiftID = !string.IsNullOrEmpty(d["ShiftID"].ToString()) ? d["ShiftID"] : "",
                                  InvoiceNo = !string.IsNullOrEmpty(d["InvoiceNo"].ToString()) ? d["InvoiceNo"] : "",
                                  Market = !string.IsNullOrEmpty(d["Market"].ToString()) ? d["Market"] : "",
                                  Source = !string.IsNullOrEmpty(d["Source"].ToString()) ? d["Source"] : "",
                                  Package = !string.IsNullOrEmpty(d["Package"].ToString()) ? d["Package"] : "",
                                  Origin = !string.IsNullOrEmpty(d["Origin"].ToString()) ? d["Origin"] : "",
                                  Company = !string.IsNullOrEmpty(d["Company"].ToString()) ? d["Company"] : "",
                                  Group = !string.IsNullOrEmpty(d["Group"].ToString()) ? d["Group"] : "",
                                  Agent = !string.IsNullOrEmpty(d["Agent"].ToString()) ? d["Agent"] : "",
                                  ReservationHolder = !string.IsNullOrEmpty(d["ReservationHolder"].ToString()) ? d["ReservationHolder"] : "",
                                  Country = !string.IsNullOrEmpty(d["Country"].ToString()) ? d["Country"] : "",
                                  Market_C = !string.IsNullOrEmpty(d["Market_C"].ToString()) ? d["Market_C"] : "",
                                  Source_C = !string.IsNullOrEmpty(d["Source_C"].ToString()) ? d["Source_C"] : "",
                                  Origin_C = !string.IsNullOrEmpty(d["Origin_C"].ToString()) ? d["Origin_C"] : "",
                                  Company_C = !string.IsNullOrEmpty(d["Company_C"].ToString()) ? d["Company_C"] : "",
                                  Group_C = !string.IsNullOrEmpty(d["Group_C"].ToString()) ? d["Group_C"] : "",
                                  Agent_C = !string.IsNullOrEmpty(d["Agent_C"].ToString()) ? d["Agent_C"] : "",
                                  ReservationHolder_C = !string.IsNullOrEmpty(d["ReservationHolder_C"].ToString()) ? d["ReservationHolder_C"] : "",
                                  HolderCode = !string.IsNullOrEmpty(d["HolderCode"].ToString()) ? d["HolderCode"] : "",
                                  Country_C = !string.IsNullOrEmpty(d["Country_C"].ToString()) ? d["Country_C"] : "",
                                  ProfitCenterCode = !string.IsNullOrEmpty(d["ProfitCenterCode"].ToString()) ? d["ProfitCenterCode"] : "",
                                  CreditVND = !string.IsNullOrEmpty(d["CreditVND"].ToString()) ? d["CreditVND"] : "",
                                  CreditUSD = !string.IsNullOrEmpty(d["CreditUSD"].ToString()) ? d["CreditUSD"] : "",
                                  DebitVND = !string.IsNullOrEmpty(d["DebitVND"].ToString()) ? d["DebitVND"] : "",
                                  DebitUSD = !string.IsNullOrEmpty(d["DebitUSD"].ToString()) ? d["DebitUSD"] : "",
                                  ArrivalDate = !string.IsNullOrEmpty(d["ArrivalDate"].ToString()) ? d["ArrivalDate"] : "",
                                  DepartureDate = !string.IsNullOrEmpty(d["DepartureDate"].ToString()) ? d["DepartureDate"] : "",
                                  Nationality = !string.IsNullOrEmpty(d["Nationality"].ToString()) ? d["Nationality"] : "",
                                  ProfileCode = !string.IsNullOrEmpty(d["ProfileCode"].ToString()) ? d["ProfileCode"] : "",


                              }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {

                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        [HttpGet]
        public IActionResult GetSearchTransactionJournalByNotVatInfor(
            string cashierNo, string transactionCodeList, string roomNoList,
            DateTime fromDate, DateTime toDate, string fromProfitCode, string toProfitCode, string groupID, string subgroupID)
        {
            try
            {
                DataTable dataTable = _iCashieringService.SearchTransactionJournalByNotVatInfor(
                    cashierNo, transactionCodeList, roomNoList, fromDate, toDate, fromProfitCode, toProfitCode, groupID, subgroupID);

                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  ConfirmationNo = !string.IsNullOrEmpty(d["ConfirmationNo"].ToString()) ? d["ConfirmationNo"] : "",
                                  CRSNo = !string.IsNullOrEmpty(d["CRSNo"].ToString()) ? d["CRSNo"] : "",
                                  FolioNo = !string.IsNullOrEmpty(d["FolioNo"].ToString()) ? d["FolioNo"] : "",
                                  AccountName = !string.IsNullOrEmpty(d["AccountName"].ToString()) ? d["AccountName"] : "",
                                  Room = !string.IsNullOrEmpty(d["Room"].ToString()) ? d["Room"] : "",
                                  SubGroupCode = !string.IsNullOrEmpty(d["SubGroupCode"].ToString()) ? d["SubGroupCode"] : "",
                                  GroupCode = !string.IsNullOrEmpty(d["GroupCode"].ToString()) ? d["GroupCode"] : "",
                                  TransactionCode = !string.IsNullOrEmpty(d["TransactionCode"].ToString()) ? d["TransactionCode"] : "",
                                  Description = !string.IsNullOrEmpty(d["Description"].ToString()) ? d["Description"] : "",
                                  Supplement = !string.IsNullOrEmpty(d["Supplement"].ToString()) ? d["Supplement"] : "",
                                  Reference = !string.IsNullOrEmpty(d["Reference"].ToString()) ? d["Reference"] : "",
                                  Time = !string.IsNullOrEmpty(d["Time"].ToString()) ? d["Time"] : "",
                                  TransactionDate = !string.IsNullOrEmpty(d["TransactionDate"].ToString()) ? d["TransactionDate"] : "",
                                  ReservationID = !string.IsNullOrEmpty(d["ReservationID"].ToString()) ? d["ReservationID"] : "",
                                  WinNo = !string.IsNullOrEmpty(d["WinNo"].ToString()) ? d["WinNo"] : "",
                                  CashierNo = !string.IsNullOrEmpty(d["CashierNo"].ToString()) ? d["CashierNo"] : "",
                                  ShiftID = !string.IsNullOrEmpty(d["ShiftID"].ToString()) ? d["ShiftID"] : "",
                                  InvoiceNo = !string.IsNullOrEmpty(d["InvoiceNo"].ToString()) ? d["InvoiceNo"] : "",
                                  Market = !string.IsNullOrEmpty(d["Market"].ToString()) ? d["Market"] : "",
                                  Source = !string.IsNullOrEmpty(d["Source"].ToString()) ? d["Source"] : "",
                                  Package = !string.IsNullOrEmpty(d["Package"].ToString()) ? d["Package"] : "",
                                  Origin = !string.IsNullOrEmpty(d["Origin"].ToString()) ? d["Origin"] : "",
                                  Company = !string.IsNullOrEmpty(d["Company"].ToString()) ? d["Company"] : "",
                                  Group = !string.IsNullOrEmpty(d["Group"].ToString()) ? d["Group"] : "",
                                  Agent = !string.IsNullOrEmpty(d["Agent"].ToString()) ? d["Agent"] : "",
                                  ReservationHolder = !string.IsNullOrEmpty(d["ReservationHolder"].ToString()) ? d["ReservationHolder"] : "",
                                  Country = !string.IsNullOrEmpty(d["Country"].ToString()) ? d["Country"] : "",
                                  Market_C = !string.IsNullOrEmpty(d["Market_C"].ToString()) ? d["Market_C"] : "",
                                  Source_C = !string.IsNullOrEmpty(d["Source_C"].ToString()) ? d["Source_C"] : "",
                                  Origin_C = !string.IsNullOrEmpty(d["Origin_C"].ToString()) ? d["Origin_C"] : "",
                                  Company_C = !string.IsNullOrEmpty(d["Company_C"].ToString()) ? d["Company_C"] : "",
                                  Group_C = !string.IsNullOrEmpty(d["Group_C"].ToString()) ? d["Group_C"] : "",
                                  Agent_C = !string.IsNullOrEmpty(d["Agent_C"].ToString()) ? d["Agent_C"] : "",
                                  ReservationHolder_C = !string.IsNullOrEmpty(d["ReservationHolder_C"].ToString()) ? d["ReservationHolder_C"] : "",
                                  HolderCode = !string.IsNullOrEmpty(d["HolderCode"].ToString()) ? d["HolderCode"] : "",
                                  Country_C = !string.IsNullOrEmpty(d["Country_C"].ToString()) ? d["Country_C"] : "",
                                  ProfitCenterCode = !string.IsNullOrEmpty(d["ProfitCenterCode"].ToString()) ? d["ProfitCenterCode"] : "",
                                  CreditVND = !string.IsNullOrEmpty(d["CreditVND"].ToString()) ? d["CreditVND"] : "",
                                  CreditUSD = !string.IsNullOrEmpty(d["CreditUSD"].ToString()) ? d["CreditUSD"] : "",
                                  DebitVND = !string.IsNullOrEmpty(d["DebitVND"].ToString()) ? d["DebitVND"] : "",
                                  DebitUSD = !string.IsNullOrEmpty(d["DebitUSD"].ToString()) ? d["DebitUSD"] : "",
                                  ArrivalDate = !string.IsNullOrEmpty(d["ArrivalDate"].ToString()) ? d["ArrivalDate"] : "",
                                  DepartureDate = !string.IsNullOrEmpty(d["DepartureDate"].ToString()) ? d["DepartureDate"] : "",
                                  Nationality = !string.IsNullOrEmpty(d["Nationality"].ToString()) ? d["Nationality"] : "",
                                  ProfileCode = !string.IsNullOrEmpty(d["ProfileCode"].ToString()) ? d["ProfileCode"] : "",


                              }).ToList();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetSearchTransactionJournalByVatInfor(
            string cashierNo, string transactionCodeList, string roomNoList,
            DateTime fromDate, DateTime toDate, string fromProfitCode, string toProfitCode, string groupID, string subgroupID)
        {
            try
            {
                DataTable dataTable = _iCashieringService.SearchTransactionJournalByVatInfor(
                    cashierNo, transactionCodeList, roomNoList, fromDate, toDate, fromProfitCode, toProfitCode, groupID, subgroupID);

                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  ConfirmationNo = !string.IsNullOrEmpty(d["ConfirmationNo"].ToString()) ? d["ConfirmationNo"] : "",
                                  CRSNo = !string.IsNullOrEmpty(d["CRSNo"].ToString()) ? d["CRSNo"] : "",
                                  FolioNo = !string.IsNullOrEmpty(d["FolioNo"].ToString()) ? d["FolioNo"] : "",
                                  AccountName = !string.IsNullOrEmpty(d["AccountName"].ToString()) ? d["AccountName"] : "",
                                  Room = !string.IsNullOrEmpty(d["Room"].ToString()) ? d["Room"] : "",
                                  SubGroupCode = !string.IsNullOrEmpty(d["SubGroupCode"].ToString()) ? d["SubGroupCode"] : "",
                                  GroupCode = !string.IsNullOrEmpty(d["GroupCode"].ToString()) ? d["GroupCode"] : "",
                                  TransactionCode = !string.IsNullOrEmpty(d["TransactionCode"].ToString()) ? d["TransactionCode"] : "",
                                  Description = !string.IsNullOrEmpty(d["Description"].ToString()) ? d["Description"] : "",
                                  Supplement = !string.IsNullOrEmpty(d["Supplement"].ToString()) ? d["Supplement"] : "",
                                  Reference = !string.IsNullOrEmpty(d["Reference"].ToString()) ? d["Reference"] : "",
                                  Time = !string.IsNullOrEmpty(d["Time"].ToString()) ? d["Time"] : "",
                                  TransactionDate = !string.IsNullOrEmpty(d["TransactionDate"].ToString()) ? d["TransactionDate"] : "",
                                  ReservationID = !string.IsNullOrEmpty(d["ReservationID"].ToString()) ? d["ReservationID"] : "",
                                  WinNo = !string.IsNullOrEmpty(d["WinNo"].ToString()) ? d["WinNo"] : "",
                                  CashierNo = !string.IsNullOrEmpty(d["CashierNo"].ToString()) ? d["CashierNo"] : "",
                                  ShiftID = !string.IsNullOrEmpty(d["ShiftID"].ToString()) ? d["ShiftID"] : "",
                                  InvoiceNo = !string.IsNullOrEmpty(d["InvoiceNo"].ToString()) ? d["InvoiceNo"] : "",
                                  Market = !string.IsNullOrEmpty(d["Market"].ToString()) ? d["Market"] : "",
                                  Source = !string.IsNullOrEmpty(d["Source"].ToString()) ? d["Source"] : "",
                                  Package = !string.IsNullOrEmpty(d["Package"].ToString()) ? d["Package"] : "",
                                  Origin = !string.IsNullOrEmpty(d["Origin"].ToString()) ? d["Origin"] : "",
                                  Company = !string.IsNullOrEmpty(d["Company"].ToString()) ? d["Company"] : "",
                                  Group = !string.IsNullOrEmpty(d["Group"].ToString()) ? d["Group"] : "",
                                  Agent = !string.IsNullOrEmpty(d["Agent"].ToString()) ? d["Agent"] : "",
                                  ReservationHolder = !string.IsNullOrEmpty(d["ReservationHolder"].ToString()) ? d["ReservationHolder"] : "",
                                  Country = !string.IsNullOrEmpty(d["Country"].ToString()) ? d["Country"] : "",
                                  Market_C = !string.IsNullOrEmpty(d["Market_C"].ToString()) ? d["Market_C"] : "",
                                  Source_C = !string.IsNullOrEmpty(d["Source_C"].ToString()) ? d["Source_C"] : "",
                                  Origin_C = !string.IsNullOrEmpty(d["Origin_C"].ToString()) ? d["Origin_C"] : "",
                                  Company_C = !string.IsNullOrEmpty(d["Company_C"].ToString()) ? d["Company_C"] : "",
                                  Group_C = !string.IsNullOrEmpty(d["Group_C"].ToString()) ? d["Group_C"] : "",
                                  Agent_C = !string.IsNullOrEmpty(d["Agent_C"].ToString()) ? d["Agent_C"] : "",
                                  ReservationHolder_C = !string.IsNullOrEmpty(d["ReservationHolder_C"].ToString()) ? d["ReservationHolder_C"] : "",
                                  HolderCode = !string.IsNullOrEmpty(d["HolderCode"].ToString()) ? d["HolderCode"] : "",
                                  Country_C = !string.IsNullOrEmpty(d["Country_C"].ToString()) ? d["Country_C"] : "",
                                  ProfitCenterCode = !string.IsNullOrEmpty(d["ProfitCenterCode"].ToString()) ? d["ProfitCenterCode"] : "",
                                  CreditVND = !string.IsNullOrEmpty(d["CreditVND"].ToString()) ? d["CreditVND"] : "",
                                  CreditUSD = !string.IsNullOrEmpty(d["CreditUSD"].ToString()) ? d["CreditUSD"] : "",
                                  DebitVND = !string.IsNullOrEmpty(d["DebitVND"].ToString()) ? d["DebitVND"] : "",
                                  DebitUSD = !string.IsNullOrEmpty(d["DebitUSD"].ToString()) ? d["DebitUSD"] : "",
                                  ArrivalDate = !string.IsNullOrEmpty(d["ArrivalDate"].ToString()) ? d["ArrivalDate"] : "",
                                  DepartureDate = !string.IsNullOrEmpty(d["DepartureDate"].ToString()) ? d["DepartureDate"] : "",
                                  Nationality = !string.IsNullOrEmpty(d["Nationality"].ToString()) ? d["Nationality"] : "",
                                  ProfileCode = !string.IsNullOrEmpty(d["ProfileCode"].ToString()) ? d["ProfileCode"] : "",


                              }).ToList();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        [HttpGet]
        public IActionResult GetCashierAudit(string userName, DateTime fromDate, DateTime toDate, string shiftID)

        {
            try
            {
                DataTable dataTable = _iCashieringService.CashierAudit(userName, fromDate, toDate, shiftID);


                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  Date = !string.IsNullOrEmpty(d["Date"].ToString()) ? d["Date"] : "",
                                  ID = !string.IsNullOrEmpty(d["ID"].ToString()) ? d["ID"] : "",
                                  ShiftNo = !string.IsNullOrEmpty(d["ShiftNo"].ToString()) ? d["ShiftNo"] : "",
                                  FullName = !string.IsNullOrEmpty(d["FullName"].ToString()) ? d["FullName"] : "",
                                  CashierNo = !string.IsNullOrEmpty(d["CashierNo"].ToString()) ? d["CashierNo"] : "",
                                  UserName = !string.IsNullOrEmpty(d["UserName"].ToString()) ? d["UserName"] : "",
                                  LoginTime = !string.IsNullOrEmpty(d["LoginTime"].ToString()) ? d["LoginTime"] : "",
                                  LogoutTime = !string.IsNullOrEmpty(d["LogoutTime"].ToString()) ? d["LogoutTime"] : "",
                                  AmountVND = !string.IsNullOrEmpty(d["AmountVND"].ToString()) ? d["AmountVND"] : "",
                                  AmountUSD = !string.IsNullOrEmpty(d["AmountUSD"].ToString()) ? d["AmountUSD"] : "",
                                  CountTransaction = !string.IsNullOrEmpty(d["CountTransaction"].ToString()) ? d["CountTransaction"] : "",
                                  Status = !string.IsNullOrEmpty(d["Status"].ToString()) ? d["Status"] : "",
                              }).ToList();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        public IActionResult CashierAudit()
        {
            List<TransactionGroupModel> tlplist = PropertyUtils.ConvertToList<TransactionGroupModel>(TransactionGroupBO.Instance.FindAll());
            ViewBag.TransactionGroupList = tlplist; // Truyền danh sách TransactionGroupModel vào ViewBag để sử dụng trong View
            List<TransactionSubGroupModel> transg = PropertyUtils.ConvertToList<TransactionSubGroupModel>(TransactionSubGroupBO.Instance.FindAll());
            ViewBag.TransactionSubGroupList = transg; // Truyền danh sách TransactionSubGroupModel vào ViewBag để sử dụng trong View
            List<UsersModel> user = PropertyUtils.ConvertToList<UsersModel>(UsersBO.Instance.FindAll());
            ViewBag.UsersList = user;
            List<TransactionsModel> trans = PropertyUtils.ConvertToList<TransactionsModel>(TransactionsBO.Instance.FindAll());
            ViewBag.TransactionsList = trans;
            return View(); // View này sẽ chứa DataGrid + script gọi API
        }
        [HttpGet]
        public IActionResult GetShiftDetail(int shiftID)
        {
            try
            {
                // type=1: header
                DataTable headerTable = _iCashieringService.ShiftDetail(shiftID, 1);
                var header = (from d in headerTable.AsEnumerable()
                              select new
                              {
                                  CashierNo = d["CashierNo"]?.ToString() ?? "",
                                  ShiftNo = d["ShiftNo"]?.ToString() ?? "",
                                  ShiftDate = d["ShiftDate"]?.ToString() ?? "",
                                  UserName = d["UserName"]?.ToString() ?? "",
                                  FullName = d["FullName"]?.ToString() ?? "",
                                  LoginTime = d["LoginTime"]?.ToString() ?? "",
                                  LogoutTime = d["LogoutTime"]?.ToString() ?? ""
                              }).FirstOrDefault();

                // type=0: detail grid
                DataTable detailTable = _iCashieringService.ShiftDetail(shiftID, 0);
                var details = (from d in detailTable.AsEnumerable()
                               select new
                               {
                                   TransactionCode = d["TransactionCode"]?.ToString() ?? "",
                                   Description = d["Description"]?.ToString() ?? "",
                                   Amount = d["Amount"]?.ToString() ?? "",
                                   TransactionDate = d.Field<DateTime?>("TransactionDate"),
                                   Reference = d["Reference"]?.ToString() ?? "",
                                   Supplement = d["Supplement"]?.ToString() ?? "",
                                   FolioID = d["FolioID"]?.ToString() ?? "",
                                   RoomNo = d["RoomNo"]?.ToString() ?? "",
                                   Account = d["Account"]?.ToString() ?? "",
                                   PaymentType = d["*"]?.ToString() ?? "",
                                   CurrencyID = d["CurrencyID"]?.ToString() ?? "",
                               }).ToList();

                return Ok(new { header, details });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        [HttpGet]
        public IActionResult GetExchangeRate()

        {
            try
            {
                DataTable dataTable = _iCashieringService.ExchangeRate();


                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  ID = !string.IsNullOrEmpty(d["ID"].ToString()) ? d["ID"] : "",
                                  DateTime = !string.IsNullOrEmpty(d["DateTime"].ToString()) ? d["DateTime"] : "",
                                  FromCurrencyID = !string.IsNullOrEmpty(d["FromCurrencyID"].ToString()) ? d["FromCurrencyID"] : "",
                                  ToCurrencyID = !string.IsNullOrEmpty(d["ToCurrencyID"].ToString()) ? d["ToCurrencyID"] : "",
                                  BuyRate = !string.IsNullOrEmpty(d["BuyRate"].ToString()) ? d["BuyRate"] : "",
                                  SellRate = !string.IsNullOrEmpty(d["SellRate"].ToString()) ? d["SellRate"] : "",
                                  ExChangeRateMin = !string.IsNullOrEmpty(d["ExChangeRateMin"].ToString()) ? d["ExChangeRateMin"] : "",
                                  DenominationMax = !string.IsNullOrEmpty(d["DenominationMax"].ToString()) ? d["DenominationMax"] : "",
                                  ExChangeRateMax = !string.IsNullOrEmpty(d["ExChangeRateMax"].ToString()) ? d["ExChangeRateMax"] : "",
                                  ExChangeRateMedium = !string.IsNullOrEmpty(d["ExChangeRateMedium"].ToString()) ? d["ExChangeRateMedium"] : "",
                                  DenominationMedium = !string.IsNullOrEmpty(d["DenominationMedium"].ToString()) ? d["DenominationMedium"] : "",
                                  DenominationMin = !string.IsNullOrEmpty(d["DenominationMin"].ToString()) ? d["DenominationMin"] : "",
                                  CreateBy = !string.IsNullOrEmpty(d["CreateBy"].ToString()) ? d["CreateBy"] : "",
                                  UpdateBy = !string.IsNullOrEmpty(d["UpdateBy"].ToString()) ? d["UpdateBy"] : "",
                                  CreateDate = !string.IsNullOrEmpty(d["CreateDate"].ToString()) ? d["CreateDate"] : "",
                                  UpdateDate = !string.IsNullOrEmpty(d["UpdateDate"].ToString()) ? d["UpdateDate"] : "",
                              }).ToList();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        public IActionResult ExchangeRate()
        {
            List<CurrencyModel> crrlist = PropertyUtils.ConvertToList<CurrencyModel>(CurrencyBO.Instance.FindAll());
            ViewBag.CurrencyList = crrlist;
            return View(); // View này sẽ chứa DataGrid + script gọi API
        }
        [HttpPost]
        public IActionResult Insert(ExchangeRateModel model)
        {
            try
            {
                // Lấy UserID từ session login
                int? userId = HttpContext.Session.GetInt32("UserID");
                if (userId == null)
                {
                    return Unauthorized(new { error = "Session expired. Please login again." });
                }

                // Gán UserInsertID và UserUpdateID từ session
                model.UserInsertID = userId.Value;
                model.CreateDate = DateTime.Now;
                model.UserUpdateID = userId.Value;
                model.UpdateDate = DateTime.Now;

                string sql = @"
        EXEC sp_executesql N'
        INSERT INTO ExchangeRate
        (DateTime,FromCurrencyID,ToCurrencyID,
         ExChangeRate,ExChangeRateSell,
         UserInsertID,CreateDate,UserUpdateID,UpdateDate,
         ExChangeRateMax,DenominationMax,
         ExChangeRateMin,DenominationMin,
         ExChangeRateMedium,DenominationMedium)
        VALUES (@DateTime,@FromCurrencyID,@ToCurrencyID,
                @ExChangeRate,@ExChangeRateSell,
                @UserInsertID,@CreateDate,@UserUpdateID,@UpdateDate,
                @ExChangeRateMax,@DenominationMax,
                @ExChangeRateMin,@DenominationMin,
                @ExChangeRateMedium,@DenominationMedium)
        SELECT @@IDENTITY AS ''ID''',
        N'@DateTime datetime,@FromCurrencyID nvarchar(3),@ToCurrencyID nvarchar(3),
          @ExChangeRate decimal(18,4),@ExChangeRateSell decimal(18,4),
          @UserInsertID int,@CreateDate datetime,@UserUpdateID int,@UpdateDate datetime,
          @ExChangeRateMax decimal(18,4),@DenominationMax int,
          @ExChangeRateMin decimal(18,4),@DenominationMin int,
          @ExChangeRateMedium decimal(18,4),@DenominationMedium int',
        @DateTime=@DateTime,@FromCurrencyID=@FromCurrencyID,@ToCurrencyID=@ToCurrencyID,
        @ExChangeRate=@ExChangeRate,@ExChangeRateSell=@ExChangeRateSell,
        @UserInsertID=@UserInsertID,@CreateDate=@CreateDate,@UserUpdateID=@UserUpdateID,@UpdateDate=@UpdateDate,
        @ExChangeRateMax=@ExChangeRateMax,@DenominationMax=@DenominationMax,
        @ExChangeRateMin=@ExChangeRateMin,@DenominationMin=@DenominationMin,
        @ExChangeRateMedium=@ExChangeRateMedium,@DenominationMedium=@DenominationMedium";

                SqlParameter[] parameters = {
            new SqlParameter("@DateTime", model.DateTime ?? (object)DBNull.Value),
            new SqlParameter("@FromCurrencyID", model.FromCurrencyID ?? (object)DBNull.Value),
            new SqlParameter("@ToCurrencyID", model.ToCurrencyID ?? (object)DBNull.Value),
            new SqlParameter("@ExChangeRate", model.ExChangeRate),
            new SqlParameter("@ExChangeRateSell", model.ExChangeRateSell),
            new SqlParameter("@UserInsertID", model.UserInsertID),
            new SqlParameter("@CreateDate", model.CreateDate),
            new SqlParameter("@UserUpdateID", model.UserUpdateID),
            new SqlParameter("@UpdateDate", model.UpdateDate),
            new SqlParameter("@ExChangeRateMax", model.ExChangeRateMax),
            new SqlParameter("@DenominationMax", model.DenominationMax),
            new SqlParameter("@ExChangeRateMin", model.ExChangeRateMin),
            new SqlParameter("@DenominationMin", model.DenominationMin),
            new SqlParameter("@ExChangeRateMedium", model.ExChangeRateMedium),
            new SqlParameter("@DenominationMedium", model.DenominationMedium)
        };

                int newId = DataTableHelper.ExecuteInsertAndReturnId(sql, parameters);

                return Json(new { id = newId, inserted = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }


        [HttpPost]
        public IActionResult Update(ExchangeRateModel model)
        {
            try
            {
                int? userId = HttpContext.Session.GetInt32("UserID");
                if (userId == null)
                {
                    return Unauthorized(new { error = "Session expired. Please login again." });
                }

                model.UserUpdateID = userId.Value;
                model.UpdateDate = DateTime.Now;

                string sql = @"
            UPDATE ExchangeRate
            SET
                DateTime=@DateTime,
                FromCurrencyID=@FromCurrencyID,
                ToCurrencyID=@ToCurrencyID,
                ExChangeRate=@ExChangeRate,
                ExChangeRateSell=@ExChangeRateSell,
                UserUpdateID=@UserUpdateID,
                UpdateDate=@UpdateDate,
                ExChangeRateMax=@ExChangeRateMax,
                DenominationMax=@DenominationMax,
                ExChangeRateMin=@ExChangeRateMin,
                DenominationMin=@DenominationMin,
                ExChangeRateMedium=@ExChangeRateMedium,
                DenominationMedium=@DenominationMedium
            WHERE ID=@ID";

                SqlParameter[] parameters = {
            new SqlParameter("@DateTime", model.DateTime ?? (object)DBNull.Value),
            new SqlParameter("@FromCurrencyID", model.FromCurrencyID ?? (object)DBNull.Value),
            new SqlParameter("@ToCurrencyID", model.ToCurrencyID ?? (object)DBNull.Value),
            new SqlParameter("@ExChangeRate", model.ExChangeRate),
            new SqlParameter("@ExChangeRateSell", model.ExChangeRateSell),
            new SqlParameter("@UserUpdateID", model.UserUpdateID),
            new SqlParameter("@UpdateDate", model.UpdateDate),
            new SqlParameter("@ExChangeRateMax", model.ExChangeRateMax),
            new SqlParameter("@DenominationMax", model.DenominationMax),
            new SqlParameter("@ExChangeRateMin", model.ExChangeRateMin),
            new SqlParameter("@DenominationMin", model.DenominationMin),
            new SqlParameter("@ExChangeRateMedium", model.ExChangeRateMedium),
            new SqlParameter("@DenominationMedium", model.DenominationMedium),
            new SqlParameter("@ID", model.ID)
        };

                int rows = DataTableHelper.ExecuteNonQueryText(sql, parameters);

                return Json(new { id = model.ID, updated = rows });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        [HttpPost]
        public IActionResult Delete(int id)
        {
            try
            {
                string sql = "DELETE FROM ExchangeRate WHERE ID=@ID";

                SqlParameter[] parameters = {
            new SqlParameter("@ID", id)
        };

                int rows = DataTableHelper.ExecuteNonQueryText(sql, parameters);

                return Json(new { deleted = rows });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        [HttpGet]
        public IActionResult GetExchangeCurrency(string account, string passPort, string roomNo, DateTime fromDate, DateTime toDate, int isDelete)
        {
            try
            {
                DataTable dataTable = _iCashieringService.ExchangeCurrency(account, passPort, roomNo, fromDate, toDate, isDelete);

                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  ProfileID = !string.IsNullOrEmpty(d["ProfileID"].ToString()) ? d["ProfileID"] : "",
                                  ID = !string.IsNullOrEmpty(d["ID"].ToString()) ? d["ID"] : "",
                                  TransactionDate = !string.IsNullOrEmpty(d["TransactionDate"].ToString()) ? d["TransactionDate"] : "",
                                  Account = !string.IsNullOrEmpty(d["Account"].ToString()) ? d["Account"] : "",
                                  PassPort = !string.IsNullOrEmpty(d["PassPort"].ToString()) ? d["PassPort"] : "",
                                  RoomNo = !string.IsNullOrEmpty(d["RoomNo"].ToString()) ? d["RoomNo"] : "",
                                  TotalAmount = !string.IsNullOrEmpty(d["TotalAmount"].ToString()) ? d["TotalAmount"] : "",
                                  Address = !string.IsNullOrEmpty(d["Address"].ToString()) ? d["Address"] : "",
                                  Description = !string.IsNullOrEmpty(d["Description"].ToString()) ? d["Description"] : "",
                                  CashierName = !string.IsNullOrEmpty(d["CashierName"].ToString()) ? d["CashierName"] : "",
                                  CreatedBy = !string.IsNullOrEmpty(d["CreatedBy"].ToString()) ? d["CreatedBy"] : "",
                                  Createddate = !string.IsNullOrEmpty(d["Createddate"].ToString()) ? d["Createddate"] : "",
                                  UpdatedBy = !string.IsNullOrEmpty(d["UpdatedBy"].ToString()) ? d["UpdatedBy"] : "",
                                  UpdatedDate = !string.IsNullOrEmpty(d["UpdatedDate"].ToString()) ? d["UpdatedDate"] : "",
                                  ExchangeCurrencyID = !string.IsNullOrEmpty(d["ExchangeCurrencyID"].ToString()) ? d["ExchangeCurrencyID"] : "",
                                  StatusText = !string.IsNullOrEmpty(d["StatusText"].ToString()) ? d["StatusText"] : "",
                                  Status = !string.IsNullOrEmpty(d["Status"].ToString()) ? d["Status"] : "",
                                  InvoiceNo = !string.IsNullOrEmpty(d["InvoiceNo"].ToString()) ? d["InvoiceNo"] : "",
                              }).ToList();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        public IActionResult ExchangeCurrency()
        {

            return View(); // View này sẽ chứa DataGrid + script gọi API
        }


        #region DatVP __ Close Shift
        public IActionResult CloseShift()
        {
            return View();
        }
        [HttpGet]
        public IActionResult GetTransactionByShift(int shiftID)
        {
            try
            {
                // type = 0: payment
                DataTable resultPaymentData = _iCloseShiftService.GetCloseShift(shiftID, 0);

                var resultPayment = (from d in resultPaymentData.AsEnumerable()
                                     select d.Table.Columns.Cast<DataColumn>()
                                         //.Where(col => col.ColumnName != "AllotmentStageID" && col.ColumnName != "flag" && col.ColumnName != "Total")
                                         .ToDictionary(
                                             col => col.ColumnName,
                                             col => d[col.ColumnName]?.ToString()
                                         )).ToList();

                // type = 1: Exchange
                DataTable resultExchangeData = _iCloseShiftService.GetCloseShift(shiftID, 1);
                var resultExchange = (from d in resultExchangeData.AsEnumerable()
                                      select d.Table.Columns.Cast<DataColumn>()
                                          //.Where(col => col.ColumnName != "AllotmentStageID" && col.ColumnName != "flag" && col.ColumnName != "Total")
                                          .ToDictionary(
                                              col => col.ColumnName,
                                              col => d[col.ColumnName]?.ToString()
                                          )).ToList();
                return Json(new
                {
                    resultPayment = resultPayment,
                    resultExchange = resultExchange
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CloseShiftIn()
        {
            try
            {
                int shiftID = int.Parse(Request.Form["shiftID"].ToString());
                ShiftModel shift = (ShiftModel)ShiftBO.Instance.FindByPrimaryKey(shiftID);
                if (shift == null || shift.ID == 0)
                {
                    return Json(new
                    {
                        code = 1,
                        msg = "Could not find shift"
                    });
                }
                shift.Status = true;
                ShiftBO.Instance.Update(shift);
                return Json(new
                {
                    code = 0,
                    msg = "Close shift was successfully"
                });

            }
            catch (Exception ex)
            {
                return Json(new ShiftModel());
            }

        }
        #endregion


        #region DatVP __ Cashiering Manager
        public IActionResult CashieringManager()
        {
            ViewBag.cboZone = ListItemHelper.GetZoneProvider();

            return PartialView();
        }

        [HttpGet]
        public IActionResult GuestInHouse(string room, string name, string block, string group, string company, string confirmationNo, DateTime arrivalDate, DateTime arrivalTo, DateTime departure, string crsNo, string package, string guestName, int zone, int typeSearch, int chooseArrivalFrom, int chooseArrivalTo, int chooseDeparture)
        {
            try
            {
                string formattedArrivalFrom = arrivalDate.ToString("yyyy-MM-dd");
                string formattedArrivalTo = arrivalTo.ToString("yyyy-MM-dd");
                string formattedDeparture = departure.ToString("yyyy-MM-dd");

                if (chooseArrivalFrom == 0)
                {
                    formattedArrivalFrom = "";
                }
                if (chooseArrivalTo == 0)
                {
                    formattedArrivalTo = "";
                }
                if (chooseDeparture == 0)
                {
                    formattedDeparture = "";
                }
                DataTable resultExchangeData = _iCashieringManagerService.GetGUestInHouse(room, name, block, group, "", company, confirmationNo, formattedArrivalFrom, formattedArrivalTo, formattedDeparture, crsNo, package, guestName, zone, typeSearch);
                var resultExchange = resultExchangeData.AsEnumerable()
                   .Select(d =>
                       d.Table.Columns.Cast<DataColumn>()
                       .ToDictionary(
                           col => col.ColumnName,
                           col =>
                           {
                               if ((col.ColumnName == "Arrival" || col.ColumnName == "Departure")
                                   && DateTime.TryParse(d[col.ColumnName]?.ToString(), out DateTime dt))
                               {
                                   return dt.ToString("dd/MM/yyyy");
                               }

                               return d[col.ColumnName]?.ToString();
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

        #region DatVP __ VAT Search
        public IActionResult VATSearch()
        {

            return View();
        }

        #endregion

        #region DatVP __ Folio VAT Search
        public IActionResult FolioVATSearch()
        {

            return View();
        }


        [HttpGet]
        public IActionResult SearchFolioVAT(DateTime fromDate, DateTime toDate, int folioStatus, int printStatus)
        {
            try
            {

                DataTable resultExchangeData = _iFolioVATSearchService.SearchFolioVAT(fromDate, toDate, folioStatus, printStatus, 1);
                var resultExchange = (from d in resultExchangeData.AsEnumerable()
                                      select d.Table.Columns.Cast<DataColumn>()
                                          //.Where(col => col.ColumnName != "AllotmentStageID" && col.ColumnName != "flag" && col.ColumnName != "Total")
                                          .ToDictionary(
                                              col => col.ColumnName,
                                              col => d[col.ColumnName]?.ToString()
                                          )).ToList();
                return Json(resultExchange);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region DatVP __ FolioVATSearchByAccouting
        public IActionResult FolioSearchByAccouting()
        {
            ViewBag.cboAccountType = ListItemHelper.GetARAccountType();

            return View();
        }

        #endregion

        [HttpPost]
        public IActionResult GetFolioHistoryView(DateTime fromDate, DateTime toDate, string fromFolioID, string toFolioID, string fromRoom, string toRoom, string actionType, string user)
        {
            try
            {
                DataTable dataTable = _iCashieringService.FolioHistoryView(fromDate, toDate, fromFolioID, fromRoom, toRoom, toFolioID, actionType, user);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  Property = !string.IsNullOrEmpty(d["Property"].ToString()) ? d["Property"] : "",
                                  ActionText = !string.IsNullOrEmpty(d["ActionText"].ToString()) ? d["ActionText"] : "",
                                  TransactionDate = !string.IsNullOrEmpty(d["TransactionDate"].ToString()) ? d["TransactionDate"] : "",
                                  ActionDate = !string.IsNullOrEmpty(d["ActionDate"].ToString()) ? d["ActionDate"] : "",
                                  ActionUser = !string.IsNullOrEmpty(d["ActionUser"].ToString()) ? d["ActionUser"] : "",
                                  AmountIncTax = !string.IsNullOrEmpty(d["AmountIncTax"].ToString()) ? d["AmountIncTax"] : "",
                                  MoreInformation = !string.IsNullOrEmpty(d["MoreInformation"].ToString()) ? d["MoreInformation"] : "",
                                  Machine = !string.IsNullOrEmpty(d["Machine"].ToString()) ? d["Machine"] : "",
                                  InvoiceNo = !string.IsNullOrEmpty(d["InvoiceNo"].ToString()) ? d["InvoiceNo"] : "",
                                  ActionType = !string.IsNullOrEmpty(d["ActionType"].ToString()) ? d["ActionType"] : "",
                                  FromFolioID = !string.IsNullOrEmpty(d["FromFolioID"].ToString()) ? d["FromFolioID"] : "",
                                  FromName = !string.IsNullOrEmpty(d["FromName"].ToString()) ? d["FromName"] : "",
                                  FromRoom = !string.IsNullOrEmpty(d["FromRoom"].ToString()) ? d["FromRoom"] : "",
                                  ToFolioID = !string.IsNullOrEmpty(d["ToFolioID"].ToString()) ? d["ToFolioID"] : "",
                                  ToName = !string.IsNullOrEmpty(d["ToName"].ToString()) ? d["ToName"] : "",
                                  ToRoom = !string.IsNullOrEmpty(d["ToRoom"].ToString()) ? d["ToRoom"] : "",
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
        public ActionResult FolioHistoryView()
        {
            List<UsersModel> listuser = PropertyUtils.ConvertToList<UsersModel>(UsersBO.Instance.FindAll());
            ViewBag.UsersList = listuser;
            return PartialView();
        }
        [HttpGet]
        public IActionResult GetSearchPostingHistoryDetail(string invoiceNo)
        {
            try
            {
                DataTable dataTable = _iCashieringService.SearchPostingHistoryDetail(invoiceNo);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {

                                  ActionText = !string.IsNullOrEmpty(d["ActionText"].ToString()) ? d["ActionText"] : "",
                                  ReasonText = !string.IsNullOrEmpty(d["ReasonText"].ToString()) ? d["ReasonText"] : "",
                                  TransactionDate = !string.IsNullOrEmpty(d["TransactionDate"].ToString()) ? d["TransactionDate"] : "",
                                  FolioID = !string.IsNullOrEmpty(d["FolioID"].ToString()) ? d["FolioID"] : "",
                                  ActionDate = !string.IsNullOrEmpty(d["ActionDate"].ToString()) ? d["ActionDate"] : "",
                                  ActionUser = !string.IsNullOrEmpty(d["ActionUser"].ToString()) ? d["ActionUser"] : "",
                                  AccountName = !string.IsNullOrEmpty(d["AccountName"].ToString()) ? d["AccountName"] : "",
                                  Machine = !string.IsNullOrEmpty(d["Machine"].ToString()) ? d["Machine"] : "",
                                  InvoiceNo = !string.IsNullOrEmpty(d["InvoiceNo"].ToString()) ? d["InvoiceNo"] : "",
                                  ActionType = !string.IsNullOrEmpty(d["ActionType"].ToString()) ? d["ActionType"] : "",
                                  ID = !string.IsNullOrEmpty(d["ID"].ToString()) ? d["ID"] : "",

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
        private int GetCurrentShiftID()
        {
            string loginName = HttpContext.Session.GetString("LoginName") ?? "";
            int shiftID = 0;

            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();

                var cmd = new SqlCommand(@"
            SELECT TOP 1 ShiftID 
            FROM FolioDetail 
            WHERE UserName = @LoginName 
            ORDER BY TransactionDate DESC", connection);

                cmd.Parameters.AddWithValue("@LoginName", loginName);

                var queryResult = cmd.ExecuteScalar();
                if (queryResult != null)
                    shiftID = Convert.ToInt32(queryResult);
            }

            return shiftID;
        }


        [HttpGet]
        public IActionResult GetCashierReport(int mode)
        {
            try
            {
                int shiftID = GetCurrentShiftID();

                DataTable dataTable = _iCashieringService.CashierReport(shiftID, mode);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  Room = d["Room"]?.ToString() ?? "",
                                  AccountName = d["AccountName"]?.ToString() ?? "",
                                  FolioNo = d["FolioNo"]?.ToString() ?? "",
                                  TransactionDate = d["TransactionDate"]?.ToString() ?? "",
                                  TransactionCode = d["TransactionCode"]?.ToString() ?? "",
                                  ConfirmationNo = d["ConfirmationNo"]?.ToString() ?? "",
                                  Description = d["Description"]?.ToString() ?? "",
                                  Supplement = d["Supplement"]?.ToString() ?? "",
                                  Amount = d["Amount"]?.ToString() ?? "",
                                  CurrencyID = d["CurrencyID"]?.ToString() ?? "",
                                  InvoiceNo = d["InvoiceNo"]?.ToString() ?? "",
                                  WinNo = d["WinNo"]?.ToString() ?? "",
                                  ArNo = d["ArNo"]?.ToString() ?? "",
                                  ShiftID = d["ShiftID"]?.ToString() ?? "",
                                  DebitVND = d["DebitVND"]?.ToString() ?? "",
                                  CreditVND = d["CreditVND"]?.ToString() ?? "",
                                  DebitUSD = d["DebitUSD"]?.ToString() ?? "",
                                  CreditUSD = d["CreditUSD"]?.ToString() ?? "",
                              }).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        public ActionResult CashierReport()
        {
            int shiftID = GetCurrentShiftID();
            ViewBag.ShiftID = shiftID;   // 👈 Gửi ra view
            return View();
        }

        [HttpPost]
        public ActionResult UpdateFolioDetail()
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();

                FolioDetailModel member = new FolioDetailModel();

                // Lấy ID từ form
                member.ID = int.Parse(Request.Form["hiddenID"]);

                member.Price = Convert.ToDecimal(Request.Form["txtprice"]);
                member.Quantity = Convert.ToInt32(Request.Form["txtquantity"]);
                member.Supplement = Request.Form["txtsupplement"].ToString();
                member.Reference = Request.Form["txtreference"].ToString();
                member.TransactionDate = Convert.ToDateTime(Request.Form["txttransactionDatee"]);
                int loginName = HttpContext.Session.GetInt32("UserID") ?? 0;
                member.UserUpdateID = member.UserInsertID;
                member.CreateDate = DateTime.Now;
                member.UpdateDate = DateTime.Now;

                if (member.ID == 0) // Insert mới
                {
                    member.UserInsertID = loginName;
                    member.CreateDate = DateTime.Now;
                    member.UserUpdateID = loginName;
                    member.UpdateDate = DateTime.Now;

                    FolioDetailBO.Instance.Insert(member);
                }
                else // Update
                {
                    // Trước khi update, lấy lại bản ghi cũ từ DB để giữ CreatedBy, CreatedDate
                    var oldData = FolioDetailBO.Instance.GetById(member.ID, pt.Connection, pt.Transaction);

                    if (oldData != null)
                    {
                        member.TransactionCode = oldData.TransactionCode;
                        member.Description = oldData.Description;
                        member.Amount = oldData.Amount;
                        member.CurrencyID = oldData.CurrencyID;
                        member.ArticleCode = oldData.ArticleCode;
                        member.CheckNo = oldData.CheckNo;
                        member.UserInsertID = oldData.UserInsertID;
                        member.CreateDate = oldData.CreateDate;
                    }

                    member.UserUpdateID = loginName;
                    member.UpdateDate = DateTime.Now;

                    FolioDetailBO.Instance.Update(member);
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

        #region ExchangeCurrencyReport
        public IActionResult ExchangeCurrencyReport()
        {
            List<BusinessDateModel> businessDateModel = PropertyUtils.ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll());
            ViewBag.BusinessDate = businessDateModel[0].BusinessDate;

            List<ZoneModel> listzo = PropertyUtils.ConvertToList<ZoneModel>(ZoneBO.Instance.FindAll());
            ViewBag.ZoneList = listzo;
            List<UsersModel> listus = PropertyUtils.ConvertToList<UsersModel>(UsersBO.Instance.FindAll());
            ViewBag.UsersList = listus;
            return View(); // View này sẽ chứa DataGrid + script gọi API
        }

        [HttpGet]
        public IActionResult ExchangeCurrencyReportData(DateTime fromDate, string cachier, string zonecode)
        {
            try
            {


                DataTable dataTable = _iCashieringService.ExchangeCurrencyReportData(fromDate, cachier, zonecode);
                // Nếu có dữ liệu, tạo report
                var report = new Reports.RptExchangeCurrencyByCashier();
                report.DataSource = dataTable;

                // 👉 Lấy giá trị CashierName đầu tiên trong DataTable để gán vào nhãn trong report
                if (dataTable.Rows.Count > 0)
                {
                    string cashierName = dataTable.Rows[0]["CashierName"]?.ToString() ?? "";
                    report.Parameters["CashierName"].Value = cashierName;  // nếu bạn có Parameter tên CashierName
                    report.Parameters["TransactionDate"].Value = dataTable.Rows[0]["TransactionDate"]?.ToString() ?? "";
                    report.Parameters["ShiftID"].Value = dataTable.Rows[0]["ShiftID"]?.ToString() ?? "";
                    report.Parameters["LogoutTime"].Value = dataTable.Rows[0]["LogoutTime"]?.ToString() ?? "";
                    report.Parameters["LoginTime"].Value = dataTable.Rows[0]["LoginTime"]?.ToString() ?? "";
                }
                using (var ms = new MemoryStream())
                {
                    report.ExportToPdf(ms);
                    ms.Seek(0, SeekOrigin.Begin);
                    // Chuyển dữ liệu PDF thành base64
                    string base64String = Convert.ToBase64String(ms.ToArray());
                    return Json(new { pdfBase64 = $"data:application/pdf;base64,{base64String}" });
                }

            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        #endregion

        #region GetExchangeCurrencyPrint
        [HttpGet]
        public IActionResult ExchangeCurrencyPrint(string id)
        {
            try
            {
                List<BusinessDateModel> businessDateModel = PropertyUtils.ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll());
                string bsd = $"ngày {businessDateModel[0].BusinessDate:dd} tháng {businessDateModel[0].BusinessDate:MM} năm {businessDateModel[0].BusinessDate:yyyy}";


                DataTable dataTable = _iCashieringService.ExchangeCurrencyPrint(id);
                // Nếu có dữ liệu, tạo report
                var report = new Reports.RptExchangeCurrencyPrint();
                report.DataSource = dataTable;

                // Format hiển thị cho các control trong report
                (report.FindControl("xrTableCellAmount", true) as XRTableCell).TextFormatString = "{0:#,0}";
                (report.FindControl("xrTableCellExchangeRate", true) as XRTableCell).TextFormatString = "{0:#,0}";
                (report.FindControl("xrTableCellAmountInVND", true) as XRTableCell).TextFormatString = "{0:#,0}";
                (report.FindControl("xrTableCellAmountInVNDTotal", true) as XRTableCell).TextFormatString = "{0:#,0}";

                // 👉 Lấy giá trị CashierName đầu tiên trong DataTable để gán vào nhãn trong report
                if (dataTable.Rows.Count > 0)
                {
                    string InvoiceNo = dataTable.Rows[0]["InvoiceNo"]?.ToString() ?? "";
                    report.Parameters["InvoiceNo"].Value = InvoiceNo;  // nếu bạn có Parameter tên CashierName
                    report.Parameters["Account"].Value = dataTable.Rows[0]["Account"]?.ToString() ?? "";
                    report.Parameters["PassPort"].Value = dataTable.Rows[0]["PassPort"]?.ToString() ?? "";
                    report.Parameters["RoomNo"].Value = dataTable.Rows[0]["RoomNo"]?.ToString() ?? "";
                    report.Parameters["Address"].Value = dataTable.Rows[0]["Address"]?.ToString() ?? "";
                    report.Parameters["Bunisessdate"].Value = bsd;
                }
                using (var ms = new MemoryStream())
                {
                    report.ExportToPdf(ms);
                    ms.Seek(0, SeekOrigin.Begin);
                    // Chuyển dữ liệu PDF thành base64
                    string base64String = Convert.ToBase64String(ms.ToArray());
                    return Json(new { pdfBase64 = $"data:application/pdf;base64,{base64String}" });
                }

            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        #endregion
        #region Tuan_Invoice 6/2/2026
        public IActionResult EInvoiceManager()
        {
            return PartialView();
        }

        [HttpGet]
        public IActionResult SetUpInvoiceSerial()
        {
            try
            {
                DataTable table = _iCashieringManagerService.SetUpInvoiceSerial();
                var result = (from d in table.AsEnumerable()
                              select d.Table.Columns.Cast<DataColumn>().ToDictionary(
                                  col => col.ColumnName,
                                  col =>
                                  {
                                      {
                                          var value = d[col.ColumnName];
                                          if (value == DBNull.Value) return null;

                                          // CreatedDate: KHÔNG ToString
                                          if (col.ColumnName == "CreatedDate" || col.ColumnName == "UpdatedDate")
                                              return value;

                                          // Các field khác: ToString
                                          return value.ToString();
                                      }
                                  }
                              )).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        public IActionResult SearchInvoiceOrInvoiceDetail(DateTime fromDate, DateTime toDate, string searchName, string confNo,
         string folioNo, string invoiceNo, string invoiceSerial, int print, int resType, int viewBy, int id)
        {
            searchName ??= "";
            invoiceSerial ??= "";
            confNo ??= "";
            folioNo ??= "";
            invoiceNo ??= "";
            try
            {
                DataTable dataTable = _iCashieringManagerService.SearchInvoiceOrInvoiceDetail(fromDate, toDate, searchName, confNo,
                    folioNo, invoiceNo, invoiceSerial, print, resType, viewBy, id);
                var result = (from d in dataTable.AsEnumerable()
                              select d.Table.Columns.Cast<DataColumn>().ToDictionary(
                              col => col.ColumnName,
                              col =>
                              {
                                  var value = d[col.ColumnName];
                                  if (value == DBNull.Value) return null;
                                  if (col.ColumnName == "verificationCode") return null;

                                  // CreatedDate: KHÔNG ToString
                                  if (col.ColumnName == "CreatedDate" || col.ColumnName == "UpdatedDate")
                                      return value;

                                  // Các field khác: ToString
                                  return value.ToString();
                              }
                          )).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        #endregion
    }
}




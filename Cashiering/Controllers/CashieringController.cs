using BaseBusiness.Auth;
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
        [RequireFunction("cash_Functions_PostingJournal")]
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
            return PartialView(); // View này sẽ chứa DataGrid + script gọi API
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
        public IActionResult GetCashierAudit(string userName, DateTime fromDate, DateTime toDate, string shiftID, bool includeDate = true)

        {
            try
            {
                // Không tick "Include Date" -> lấy tất cả các ngày (không giới hạn khoảng ngày)
                if (!includeDate)
                {
                    fromDate = new DateTime(1900, 1, 1);
                    toDate = new DateTime(2070, 12, 31); // < 2079 để hợp cả kiểu smalldatetime của SP
                }
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
        [RequireFunction("cash_Functions_CashierAudit")]
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
            return PartialView(); // View này sẽ chứa DataGrid + script gọi API
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
        // Xóa mềm giao dịch đổi tiền: Status = 1 (giống btnDelete_Click của frmExchangeCurrencySearch)
        [HttpPost]
        public IActionResult DeleteExchangeCurrency(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { success = false, message = "Exchange currency ID not found." });

                string loginName = HttpContext.Session.GetString("LoginName") ?? "";
                if (string.IsNullOrEmpty(loginName))
                    return Unauthorized(new { success = false, message = "Session expired. Please login again." });

                int rows = DataTableHelper.ExecuteNonQueryText(
                    "UPDATE ExchangeCurrency SET Status = 1, UpdatedBy = @UpdatedBy, UpdatedDate = GETDATE() WHERE ID = @ID",
                    new SqlParameter[]
                    {
                        new SqlParameter("@UpdatedBy", loginName),
                        new SqlParameter("@ID", id)
                    });

                return Ok(new { success = true, message = "Deleted successfully.", rows });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [RequireFunction("cash_ExchangeCurrency")]
        public IActionResult ExchangeCurrency()
        {

            return PartialView(); // View này sẽ chứa DataGrid + script gọi API
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
        [RequireFunction("cash_Manager")]
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
        [RequireFunction("cash_Functions_FolioHistory")]
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
        [RequireFunction("cash_ExchangeCurrencyReport")]
        public IActionResult ExchangeCurrencyReport()
        {
            List<BusinessDateModel> businessDateModel = PropertyUtils.ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll());
            ViewBag.BusinessDate = businessDateModel[0].BusinessDate;

            List<ZoneModel> listzo = PropertyUtils.ConvertToList<ZoneModel>(ZoneBO.Instance.FindAll());
            ViewBag.ZoneList = listzo;
            List<UsersModel> listus = PropertyUtils.ConvertToList<UsersModel>(UsersBO.Instance.FindAll());
            ViewBag.UsersList = listus;
            return PartialView(); // View này sẽ chứa DataGrid + script gọi API
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

        // =====================================================================================================
        //  MÀN EXCHANGE CURRENCY – NÚT NEW / EDIT / INVOICE
        //  Port từ WinForms: frmExchangeCurrency.cs + frmExchangeCurrencyProfileSearch.cs
        //  Các hàm hỗ trợ đặt tên tiền tố "ExCu" để không lẫn với phần còn lại của controller.
        // =====================================================================================================

        #region Exchange Currency - DTO

        /// <summary>Một dòng đổi tiền trên lưới của form New/Edit (tiền tệ, mệnh giá, số lượng)</summary>
        public class ExchangeCurrencyLineDto
        {
            /// <summary>ID dòng trong bảng ExchangeCurrencyRate (0 = dòng mới chưa lưu)</summary>
            public int ID { get; set; }
            /// <summary>Mã tiền tệ khách đưa vào (FromCurrencyID), ví dụ USD</summary>
            public string Currency { get; set; } = "";
            /// <summary>Mệnh giá tờ tiền (ví dụ 100, 50, 20...)</summary>
            public int Denomination { get; set; }
            /// <summary>Số lượng tờ</summary>
            public int Quantity { get; set; }
        }

        /// <summary>Dữ liệu trình duyệt gửi lên khi bấm OK/Save ở form New/Edit</summary>
        public class ExchangeCurrencySaveDto
        {
            /// <summary>ID giao dịch (0 = thêm mới, >0 = sửa)</summary>
            public int ID { get; set; }
            public int ProfileID { get; set; }
            public int ReservationID { get; set; }
            public string RoomNo { get; set; } = "";
            public string Address { get; set; } = "";
            public string Description { get; set; } = "";
            /// <summary>Ca thu ngân hiện tại (web lấy từ localStorage.shiftID sau khi đăng nhập ca)</summary>
            public int ShiftID { get; set; }
            /// <summary>Các dòng đổi tiền hiện có trên lưới</summary>
            public List<ExchangeCurrencyLineDto> Lines { get; set; } = new();
            /// <summary>ID các dòng bị xóa khi sửa (sẽ xóa mềm IsDelete = 1)</summary>
            public List<int> DeletedLineIDs { get; set; } = new();
        }
        #endregion

        #region Exchange Currency - Hàm hỗ trợ

        /// <summary>Đổi tên cột sang camelCase (chỉ hạ chữ cái đầu) để khớp tên field phía JavaScript</summary>
        private static string CamelKey(string name)
        {
            return string.IsNullOrEmpty(name) ? name : char.ToLowerInvariant(name[0]) + name.Substring(1);
        }

        /// <summary>
        /// Chuyển DataTable thành danh sách dictionary camelCase.
        /// (System.Text.Json giữ nguyên key của Dictionary nên phải tự camelCase ở đây.)
        /// Lưu ý: cột "ID" sẽ thành "iD" – nếu muốn key "id" thì đặt alias "Id" trong câu SQL.
        /// </summary>
        private static List<Dictionary<string, object?>> TableToRows(DataTable dt)
        {
            return dt.AsEnumerable().Select(r => dt.Columns.Cast<DataColumn>()
                .ToDictionary(c => CamelKey(c.ColumnName), c => r[c] == DBNull.Value ? null : r[c])).ToList();
        }

        /// <summary>Ngày business date hiện tại của khách sạn (bảng BusinessDate)</summary>
        private DateTime ExCuBusinessDate()
        {
            return PropertyUtils.ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll())[0].BusinessDate;
        }

        /// <summary>Ngày giờ hiện tại của máy chủ SQL (GETDATE) – giống TextUtils.GetSystemDate()</summary>
        private static DateTime ExCuSystemDate()
        {
            return Convert.ToDateTime(DataTableHelper.ExecuteQuery("SELECT GETDATE()").Rows[0][0]);
        }

        /// <summary>
        /// Ngày business date + giờ hệ thống – giống TextUtils.GetBusinessDateTime() của WinForms.
        /// Dùng làm ngày giao dịch (qua đêm vẫn tính theo business date).
        /// </summary>
        private DateTime ExCuBusinessDateTime()
        {
            DateTime b = ExCuBusinessDate();
            DateTime s = ExCuSystemDate();
            return new DateTime(b.Year, b.Month, b.Day, s.Hour, s.Minute, s.Second, s.Millisecond);
        }

        /// <summary>
        /// Lấy tỷ giá theo ngày business date, mệnh giá và loại tiền (SP spExchangeCurrencyByDenomination).
        /// Trả 0 nếu chưa khai báo tỷ giá.
        /// </summary>
        private decimal ExCuGetRate(string currency, int denomination)
        {
            DataTable tbl = DataTableHelper.getTableData("spExchangeCurrencyByDenomination", new SqlParameter[]
            {
                new SqlParameter("@BusDate", ExCuBusinessDate()),
                new SqlParameter("@Denomination", denomination),
                new SqlParameter("@FromCurrencyID", currency ?? "")
            });
            if (tbl != null && tbl.Rows.Count > 0 && tbl.Columns.Contains("ExChangeRate") && tbl.Rows[0]["ExChangeRate"] != DBNull.Value)
                return Convert.ToDecimal(tbl.Rows[0]["ExChangeRate"]);
            return 0;
        }

        /// <summary>
        /// Sinh số hóa đơn: ddMMyy + số thứ tự 4 chữ số (giống TextUtils.GenerateInvoiceCode của WinForms).
        /// Win chỉ đọc bảng Invoice; ở đây lấy số lớn nhất của cả Invoice và ExchangeCurrency trong ngày
        /// để hai giao dịch đổi tiền cùng ngày không trùng số.
        /// Dùng khóa ứng dụng (sp_getapplock) để 2 người lưu cùng lúc không sinh trùng số; khóa tự nhả khi commit/rollback.
        /// </summary>
        private static string ExCuGenerateInvoiceNo(SqlConnection conn, SqlTransaction tx, DateTime sysDate)
        {
            // Khóa độc quyền trong phạm vi transaction hiện tại (chờ tối đa 10 giây)
            using (var lockCmd = new SqlCommand(
                "EXEC sp_getapplock @Resource = 'ExchangeCurrencyInvoiceNo', @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000", conn, tx))
            {
                lockCmd.ExecuteNonQuery();
            }

            string prefix = sysDate.ToString("ddMMyy");
            int max = 0;

            // Đọc mã mới nhất của 1 bảng, lấy phần số đuôi sau 6 ký tự ngày rồi cập nhật max
            void ReadMax(string sql)
            {
                using var cmd = new SqlCommand(sql, conn, tx);
                cmd.Parameters.AddWithValue("@Like", prefix + "%");
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    string code = rd[0]?.ToString() ?? "";
                    if (code.Length > 6 && int.TryParse(code.Substring(6), out int n) && n > max)
                        max = n;
                }
            }

            ReadMax("SELECT TOP 1 InvoiceCode FROM dbo.Invoice WITH (NOLOCK) WHERE InvoiceCode LIKE @Like ORDER BY ID DESC");
            ReadMax("SELECT TOP 1 InvoiceNo FROM dbo.ExchangeCurrency WITH (NOLOCK) WHERE InvoiceNo LIKE @Like ORDER BY ID DESC");

            return prefix + (max + 1).ToString("D4");
        }
        #endregion

        #region Exchange Currency - Dữ liệu cho form

        /// <summary>Danh sách tiền tệ ngoại tệ (MasterStatus = 0) và mã tiền tệ chính (MasterStatus = 1) cho combo trên lưới</summary>
        [HttpGet]
        public IActionResult ExchangeCurrencyLookups()
        {
            try
            {
                DataTable cur = DataTableHelper.ExecuteQuery("SELECT ID, Description FROM Currency WITH (NOLOCK) WHERE MasterStatus = 0");
                DataTable master = DataTableHelper.ExecuteQuery("SELECT TOP 1 ID FROM Currency WITH (NOLOCK) WHERE MasterStatus = 1");
                return Ok(new
                {
                    currencies = TableToRows(cur),
                    masterCurrency = master.Rows.Count > 0 ? master.Rows[0][0].ToString() : ""
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>Tỷ giá của 1 loại tiền + mệnh giá (gọi khi người dùng đổi tiền tệ/mệnh giá trên lưới)</summary>
        [HttpGet]
        public IActionResult ExchangeCurrencyRate(string currency, int denomination)
        {
            try
            {
                return Ok(new { rate = ExCuGetRate(currency, denomination) });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>Tìm profile/reservation cho popup chọn khách (SP spExchangeCurrencyProfileSearch)</summary>
        [HttpGet]
        public IActionResult ExchangeCurrencyProfileSearch(string guestName, string roomNo, string confirmationNo, int searchType = 1)
        {
            try
            {
                DataTable dt = DataTableHelper.getTableData("spExchangeCurrencyProfileSearch", new SqlParameter[]
                {
                    new SqlParameter("@GuestName", (guestName ?? "").Trim()),
                    new SqlParameter("@RoomNo", (roomNo ?? "").Trim()),
                    new SqlParameter("@ConfirmationNo", confirmationNo ?? ""),
                    new SqlParameter("@SearchType", searchType)
                });
                return Ok(TableToRows(dt));
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>Thông tin profile + reservation đã chọn (tên, passport/CMND, địa chỉ, số phòng) để điền lên form</summary>
        [HttpGet]
        public IActionResult ExchangeCurrencyProfileInfo(int profileId, int reservationId)
        {
            try
            {
                var p = (ProfileModel?)ProfileBO.Instance.FindByPrimaryKey(profileId);
                if (p == null)
                    return NotFound(new { success = false, message = "Profile not found." });

                // Số phòng lấy từ reservation (nếu khách đang ở); khách vãng lai thì để trống
                string roomNo = "";
                int rsvId = 0;
                if (reservationId > 0)
                {
                    var r = (ReservationModel?)ReservationBO.Instance.FindByPrimaryKey(reservationId);
                    if (r != null) { roomNo = r.RoomNo ?? ""; rsvId = r.ID; }
                }

                // Ưu tiên Passport, không có thì dùng số CMND
                string passport = string.IsNullOrEmpty(p.PassPort) ? (p.IdentityCard ?? "") : p.PassPort;
                return Ok(new
                {
                    profileId = p.ID,
                    account = p.Account ?? "",
                    passPort = passport,
                    address = p.Address ?? "",
                    reservationId = rsvId,
                    roomNo
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>Dữ liệu để sửa 1 giao dịch (nút Edit): header + thông tin khách + các dòng chưa bị xóa</summary>
        [HttpGet]
        public IActionResult ExchangeCurrencyEditData(int id)
        {
            try
            {
                // Header giao dịch
                DataTable h = DataTableHelper.ExecuteQuery(
                    "SELECT ID, ProfileID, ReservationID, RoomNo, Address, ShiftID, Description, Status, InvoiceNo FROM ExchangeCurrency WITH (NOLOCK) WHERE ID = @ID",
                    new SqlParameter[] { new SqlParameter("@ID", id) });
                if (h.Rows.Count == 0)
                    return NotFound(new { success = false, message = "Record not found." });

                // Thông tin khách từ bảng Profile
                int profileId = h.Rows[0]["ProfileID"] == DBNull.Value ? 0 : Convert.ToInt32(h.Rows[0]["ProfileID"]);
                DataTable p = DataTableHelper.ExecuteQuery(
                    "SELECT Account, PassPort, IdentityCard FROM [Profile] WITH (NOLOCK) WHERE ID = @ID",
                    new SqlParameter[] { new SqlParameter("@ID", profileId) });

                string account = "", passport = "";
                if (p.Rows.Count > 0)
                {
                    account = p.Rows[0]["Account"]?.ToString() ?? "";
                    passport = p.Rows[0]["PassPort"]?.ToString() ?? "";
                    if (string.IsNullOrEmpty(passport)) passport = p.Rows[0]["IdentityCard"]?.ToString() ?? "";
                }

                // Các dòng đổi tiền chưa bị xóa mềm
                DataTable lines = DataTableHelper.ExecuteQuery(
                    @"SELECT ID, FromCurrencyID AS Currency, Denomination, Quantity, Amount, ExchangeRate
                      FROM ExchangeCurrencyRate WITH (NOLOCK)
                      WHERE ExchangeCurrencyID = @ID AND ISNULL(IsDelete, 0) = 0
                      ORDER BY ID",
                    new SqlParameter[] { new SqlParameter("@ID", id) });

                var header = TableToRows(h)[0];
                return Ok(new
                {
                    header,
                    account,
                    passPort = passport,
                    lines = TableToRows(lines)
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        #endregion

        #region Exchange Currency - Lưu (New / Edit)

        /// <summary>
        /// Lưu giao dịch đổi tiền (ID = 0: thêm mới, ID > 0: sửa). Port từ btnOK/btnSave của frmExchangeCurrency.
        /// Các bước: kiểm tra phiên + dữ liệu → kiểm tra ca thu ngân đang mở → tính lại tiền/tỷ giá trên server
        /// → ghi header + các dòng trong một SqlTransaction (lỗi thì rollback toàn bộ).
        /// </summary>
        [HttpPost]
        public IActionResult SaveExchangeCurrency([FromBody] ExchangeCurrencySaveDto dto)
        {
            try
            {
                // Phải đăng nhập (lấy tên thu ngân để ghi CreatedBy/UpdatedBy)
                string loginName = HttpContext.Session.GetString("LoginName") ?? "";
                if (string.IsNullOrEmpty(loginName))
                    return Unauthorized(new { success = false, message = "Session expired. Please login again." });

                if (dto == null)
                    return BadRequest(new { success = false, message = "Invalid data." });

                // ===== Kiểm tra giống checkVadil / checkRow của WinForms =====
                if (dto.ProfileID <= 0)
                    return Ok(new { success = false, message = "Profile not blank!" });
                if (dto.Lines == null || dto.Lines.Count == 0)
                    return Ok(new { success = false, message = "Data null.Not Save." });

                // Tiền tệ chính (đích quy đổi)
                string master;
                DataTable m = DataTableHelper.ExecuteQuery("SELECT TOP 1 ID FROM Currency WITH (NOLOCK) WHERE MasterStatus = 1");
                if (m.Rows.Count == 0)
                    return Ok(new { success = false, message = "Master currency not found." });
                master = m.Rows[0][0].ToString() ?? "";

                // Ca thu ngân phải đang mở (thay cho CasheringUtils.CheckCashier của Win)
                if (dto.ShiftID <= 0)
                    return Ok(new { success = false, message = "Please login to cashier shift first." });
                DataTable sh = DataTableHelper.ExecuteQuery(
                    "SELECT COUNT(1) FROM Shift WITH (NOLOCK) WHERE ID = @ID AND ISNULL(Status, 0) = 0",
                    new SqlParameter[] { new SqlParameter("@ID", dto.ShiftID) });
                if (Convert.ToInt32(sh.Rows[0][0]) == 0)
                    return Ok(new { success = false, message = "Cashier shift is closed. Please login to cashier shift again." });

                // Tính lại số tiền và tỷ giá trên server (không tin giá trị từ trình duyệt)
                var calc = new List<(ExchangeCurrencyLineDto Line, decimal Amount, decimal Rate, decimal Vnd)>();
                foreach (var l in dto.Lines)
                {
                    if (string.IsNullOrWhiteSpace(l.Currency))
                        return Ok(new { success = false, message = "Currencies not blank." });
                    if (l.Denomination <= 0)
                        return Ok(new { success = false, message = "Denomination >0" });
                    if (l.Quantity <= 0)
                        return Ok(new { success = false, message = "Quantity >0" });

                    // Amount = mệnh giá x số lượng (ngoại tệ)
                    decimal amount = (decimal)l.Denomination * l.Quantity;
                    if (amount <= 0)
                        return Ok(new { success = false, message = "Amount >0" });

                    // Tỷ giá theo ngày business date + mệnh giá
                    decimal rate = ExCuGetRate(l.Currency, l.Denomination);
                    if (rate <= 0)
                        return Ok(new { success = false, message = "Exchange Rate >0 (" + l.Currency + " / " + l.Denomination + ")" });

                    // Thành tiền quy đổi sang tiền chính
                    decimal vnd = amount * rate;
                    if (vnd <= 0)
                        return Ok(new { success = false, message = "Amount In VND >0" });

                    calc.Add((l, amount, rate, vnd));
                }
                decimal total = calc.Sum(x => x.Vnd);

                DateTime sysDate = ExCuSystemDate();

                // Toàn bộ ghi dữ liệu nằm trong 1 transaction
                using var conn = new SqlConnection(DBUtils.GetDBConnectionString());
                conn.Open();
                using var tx = conn.BeginTransaction();
                try
                {
                    int headerId;
                    string invoiceNo;

                    if (dto.ID <= 0)
                    {
                        // ===== Thêm mới =====
                        // Sinh số hóa đơn trong cùng transaction (có khóa) rồi ghi header với Status = 0 (đang hiệu lực)
                        invoiceNo = ExCuGenerateInvoiceNo(conn, tx, sysDate);
                        using var ins = new SqlCommand(@"
                            INSERT INTO ExchangeCurrency
                                (TransactionDate, ProfileID, ReservationID, RoomNo, Address, ShiftID, CashierName,
                                 TotalAmount, Description, Status, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, InvoiceNo)
                            VALUES
                                (@TransactionDate, @ProfileID, @ReservationID, @RoomNo, @Address, @ShiftID, @CashierName,
                                 @TotalAmount, @Description, 0, @User, @Now, @User, @Now, @InvoiceNo);
                            SELECT CAST(SCOPE_IDENTITY() AS int);", conn, tx);
                        ins.Parameters.AddWithValue("@TransactionDate", ExCuBusinessDateTime());
                        ins.Parameters.AddWithValue("@ProfileID", dto.ProfileID);
                        ins.Parameters.AddWithValue("@ReservationID", dto.ReservationID);
                        ins.Parameters.AddWithValue("@RoomNo", dto.RoomNo ?? "");
                        ins.Parameters.AddWithValue("@Address", dto.Address ?? "");
                        ins.Parameters.AddWithValue("@ShiftID", dto.ShiftID);
                        ins.Parameters.AddWithValue("@CashierName", loginName);
                        ins.Parameters.AddWithValue("@TotalAmount", total);
                        ins.Parameters.AddWithValue("@Description", dto.Description ?? "");
                        ins.Parameters.AddWithValue("@User", loginName);
                        ins.Parameters.AddWithValue("@Now", sysDate);
                        ins.Parameters.AddWithValue("@InvoiceNo", invoiceNo);
                        headerId = Convert.ToInt32(ins.ExecuteScalar());

                        // Ghi từng dòng đổi tiền
                        foreach (var c in calc)
                            ExCuInsertLine(conn, tx, headerId, c.Line, c.Amount, c.Rate, master, loginName, sysDate);
                    }
                    else
                    {
                        // ===== Sửa =====
                        headerId = dto.ID;
                        int oldShift;
                        using (var chk = new SqlCommand("SELECT ShiftID, InvoiceNo FROM ExchangeCurrency WHERE ID = @ID", conn, tx))
                        {
                            chk.Parameters.AddWithValue("@ID", headerId);
                            using var rd = chk.ExecuteReader();
                            if (!rd.Read())
                            {
                                tx.Rollback();
                                return Ok(new { success = false, message = "Record not found." });
                            }
                            oldShift = rd["ShiftID"] == DBNull.Value ? 0 : Convert.ToInt32(rd["ShiftID"]);
                            invoiceNo = rd["InvoiceNo"]?.ToString() ?? "";
                        }

                        // Giống Win: chỉ cho sửa trong đúng ca của giao dịch
                        if (oldShift != dto.ShiftID)
                        {
                            tx.Rollback();
                            return Ok(new { success = false, message = "Current cashier shift not equal shift exchange" });
                        }

                        // Cập nhật header (giữ nguyên số hóa đơn và ca)
                        using (var upd = new SqlCommand(@"
                            UPDATE ExchangeCurrency SET
                                ProfileID = @ProfileID, ReservationID = @ReservationID, RoomNo = @RoomNo, Address = @Address,
                                CashierName = @CashierName, TotalAmount = @TotalAmount, Description = @Description,
                                UpdatedBy = @User, UpdatedDate = @Now
                            WHERE ID = @ID", conn, tx))
                        {
                            upd.Parameters.AddWithValue("@ProfileID", dto.ProfileID);
                            upd.Parameters.AddWithValue("@ReservationID", dto.ReservationID);
                            upd.Parameters.AddWithValue("@RoomNo", dto.RoomNo ?? "");
                            upd.Parameters.AddWithValue("@Address", dto.Address ?? "");
                            upd.Parameters.AddWithValue("@CashierName", loginName);
                            upd.Parameters.AddWithValue("@TotalAmount", total);
                            upd.Parameters.AddWithValue("@Description", dto.Description ?? "");
                            upd.Parameters.AddWithValue("@User", loginName);
                            upd.Parameters.AddWithValue("@Now", sysDate);
                            upd.Parameters.AddWithValue("@ID", headerId);
                            upd.ExecuteNonQuery();
                        }

                        // Dòng bị xóa: xóa mềm (IsDelete = 1), chỉ xóa dòng thuộc đúng giao dịch này
                        foreach (int delId in dto.DeletedLineIDs ?? new List<int>())
                        {
                            using var del = new SqlCommand(@"
                                UPDATE ExchangeCurrencyRate SET IsDelete = 1, UpdatedBy = @User, UpdatedDate = @Now
                                WHERE ID = @ID AND ExchangeCurrencyID = @HID", conn, tx);
                            del.Parameters.AddWithValue("@User", loginName);
                            del.Parameters.AddWithValue("@Now", sysDate);
                            del.Parameters.AddWithValue("@ID", delId);
                            del.Parameters.AddWithValue("@HID", headerId);
                            del.ExecuteNonQuery();
                        }

                        // Các dòng còn lại: dòng đã có thì cập nhật, dòng mới thì thêm
                        foreach (var c in calc)
                        {
                            bool exists = false;
                            if (c.Line.ID > 0)
                            {
                                using var ex = new SqlCommand("SELECT COUNT(1) FROM ExchangeCurrencyRate WHERE ID = @ID AND ExchangeCurrencyID = @HID", conn, tx);
                                ex.Parameters.AddWithValue("@ID", c.Line.ID);
                                ex.Parameters.AddWithValue("@HID", headerId);
                                exists = Convert.ToInt32(ex.ExecuteScalar()) > 0;
                            }

                            if (exists)
                            {
                                using var ul = new SqlCommand(@"
                                    UPDATE ExchangeCurrencyRate SET
                                        FromCurrencyID = @From, ToCurrencyID = @To, Denomination = @Den, Quantity = @Qty,
                                        ExchangeRate = @Rate, Amount = @Amount, UpdatedBy = @User, UpdatedDate = @Now
                                    WHERE ID = @ID", conn, tx);
                                ul.Parameters.AddWithValue("@From", c.Line.Currency);
                                ul.Parameters.AddWithValue("@To", master);
                                ul.Parameters.AddWithValue("@Den", c.Line.Denomination);
                                ul.Parameters.AddWithValue("@Qty", c.Line.Quantity);
                                ul.Parameters.AddWithValue("@Rate", c.Rate);
                                ul.Parameters.AddWithValue("@Amount", c.Amount);
                                ul.Parameters.AddWithValue("@User", loginName);
                                ul.Parameters.AddWithValue("@Now", sysDate);
                                ul.Parameters.AddWithValue("@ID", c.Line.ID);
                                ul.ExecuteNonQuery();
                            }
                            else
                            {
                                // Giống Win: dòng chưa tồn tại thì thêm mới
                                ExCuInsertLine(conn, tx, headerId, c.Line, c.Amount, c.Rate, master, loginName, sysDate);
                            }
                        }
                    }

                    tx.Commit();
                    return Ok(new { success = true, message = "Saved successfully.", id = headerId, invoiceNo, totalAmount = total });
                }
                catch
                {
                    // Lỗi bất kỳ: hoàn tác toàn bộ rồi ném lên catch ngoài để trả thông báo
                    try { tx.Rollback(); } catch { }
                    throw;
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>Ghi 1 dòng đổi tiền vào ExchangeCurrencyRate (IsDelete = 0) trong transaction đang mở</summary>
        private static void ExCuInsertLine(SqlConnection conn, SqlTransaction tx, int headerId, ExchangeCurrencyLineDto line,
            decimal amount, decimal rate, string masterCurrency, string user, DateTime now)
        {
            using var cmd = new SqlCommand(@"
                INSERT INTO ExchangeCurrencyRate
                    (ExchangeCurrencyID, FromCurrencyID, ToCurrencyID, Denomination, Quantity, ExchangeRate, Amount,
                     IsDelete, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate)
                VALUES
                    (@HID, @From, @To, @Den, @Qty, @Rate, @Amount, 0, @User, @Now, @User, @Now)", conn, tx);
            cmd.Parameters.AddWithValue("@HID", headerId);
            cmd.Parameters.AddWithValue("@From", line.Currency);
            cmd.Parameters.AddWithValue("@To", masterCurrency);
            cmd.Parameters.AddWithValue("@Den", line.Denomination);
            cmd.Parameters.AddWithValue("@Qty", line.Quantity);
            cmd.Parameters.AddWithValue("@Rate", rate);
            cmd.Parameters.AddWithValue("@Amount", amount);
            cmd.Parameters.AddWithValue("@User", user);
            cmd.Parameters.AddWithValue("@Now", now);
            cmd.ExecuteNonQuery();
        }
        #endregion

        // =====================================================================================================
        //  MÀN EXCHANGE RATES (khai báo tỷ giá theo ngày)
        //  Port từ WinForms: frmGenExChangeRates.cs
        //  (Hàm GetExchangeRate / ExchangeRate / Insert / Update / Delete cũ nằm ở phần trên của controller.)
        // =====================================================================================================

        // =====================================================================================================
        //  MÀN GUEST IN HOUSE SEARCH (Cashiering Manager) – các nút/tab động phía dưới lưới
        //  Win: ClassReservation.CreateButton(pnReservation, ReservationID) tạo nút theo dữ liệu của từng reservation.
        //  Web: suy ra từ dữ liệu – xem chú thích từng điều kiện bên dưới.
        // =====================================================================================================
        #region Guest In House - Nút theo từng reservation

        /// <summary>
        /// Trả về các tab cần hiện cho 1 reservation: Shares, Routing, Package, Billing.
        /// Điều kiện (suy ra từ dữ liệu, chưa có mã nguồn gốc của Win để đối chiếu):
        ///  - shares : là khách chính (MainGuest = 1) và còn reservation khác cùng ShareRoom
        ///  - routing: có bản ghi Routing với FromReservationID = reservation
        ///  - package: có bản ghi ReservationPackage của reservation
        ///  - billing: có folio của reservation
        /// </summary>
        [HttpGet]
        public IActionResult GuestInHouseButtons(int reservationId)
        {
            try
            {
                DataTable dt = DataTableHelper.ExecuteQuery(@"
SELECT
  CASE WHEN r.MainGuest = 1 AND ISNULL(r.ShareRoom, 0) > 0 AND EXISTS
       (SELECT 1 FROM Reservation s WITH (NOLOCK) WHERE s.ShareRoom = r.ShareRoom AND s.ID <> r.ID) THEN 1 ELSE 0 END AS Shares,
  CASE WHEN EXISTS (SELECT 1 FROM Routing x WITH (NOLOCK) WHERE x.FromReservationID = r.ID) THEN 1 ELSE 0 END AS Routing,
  CASE WHEN EXISTS (SELECT 1 FROM ReservationPackage p WITH (NOLOCK) WHERE p.ReservationID = r.ID) THEN 1 ELSE 0 END AS Package,
  CASE WHEN EXISTS (SELECT 1 FROM Folio f WITH (NOLOCK) WHERE f.ReservationID = r.ID) THEN 1 ELSE 0 END AS Billing
FROM Reservation r WITH (NOLOCK)
WHERE r.ID = @ID",
                    new SqlParameter[] { new SqlParameter("@ID", reservationId) });

                if (dt.Rows.Count == 0)
                    return Ok(new { shares = false, routing = false, package = false, billing = false });

                DataRow r0 = dt.Rows[0];
                return Ok(new
                {
                    shares = Convert.ToInt32(r0["Shares"]) == 1,
                    routing = Convert.ToInt32(r0["Routing"]) == 1,
                    package = Convert.ToInt32(r0["Package"]) == 1,
                    billing = Convert.ToInt32(r0["Billing"]) == 1
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        #endregion

        #region Exchange Rate - DTO

        /// <summary>Dữ liệu trình duyệt gửi lên khi bấm Save ở màn Exchange Rates</summary>
        public class ExchangeRateSaveDto
        {
            /// <summary>ID tỷ giá (0 = thêm mới, >0 = sửa)</summary>
            public int ID { get; set; }
            /// <summary>Ngày áp dụng tỷ giá</summary>
            public DateTime? DateTime { get; set; }
            /// <summary>Tiền tệ nguồn (ngoại tệ)</summary>
            public string FromCurrencyID { get; set; } = "";
            /// <summary>Tiền tệ đích (luôn là tiền tệ chính)</summary>
            public string ToCurrencyID { get; set; } = "";
            /// <summary>Tỷ giá mua (Buy Rate)</summary>
            public decimal ExChangeRate { get; set; }
            /// <summary>Tỷ giá bán (Sell Rate)</summary>
            public decimal ExChangeRateSell { get; set; }
            /// <summary>Tỷ giá mua + mệnh giá cho mức Max / Medium / Min</summary>
            public decimal ExChangeRateMax { get; set; }
            public int DenominationMax { get; set; }
            public decimal ExChangeRateMedium { get; set; }
            public int DenominationMedium { get; set; }
            public decimal ExChangeRateMin { get; set; }
            public int DenominationMin { get; set; }
        }
        #endregion

        #region Exchange Rate - Dữ liệu và lưu

        /// <summary>
        /// Dữ liệu cho 2 combo: danh sách tiền tệ đang dùng (Inactive = 0, giống loadComGXRo1 của Win)
        /// và tiền tệ chính (MasterStatus = 1, giống loadComGXRo2 / GetMasterCurrency).
        /// </summary>
        [HttpGet]
        public IActionResult ExchangeRateLookups()
        {
            try
            {
                DataTable cur = DataTableHelper.ExecuteQuery(
                    "SELECT ID, ID + ' - ' + Description AS Code FROM Currency WITH (NOLOCK) WHERE Inactive = 0 ORDER BY ID");
                DataTable master = DataTableHelper.ExecuteQuery(
                    "SELECT TOP 1 ID, ID + ' - ' + Description AS Code FROM Currency WITH (NOLOCK) WHERE MasterStatus = 1");
                return Ok(new
                {
                    currencies = TableToRows(cur),
                    masterCurrency = master.Rows.Count > 0 ? master.Rows[0]["ID"].ToString() : "",
                    masterCurrencyText = master.Rows.Count > 0 ? master.Rows[0]["Code"].ToString() : ""
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Danh sách tỷ giá cho lưới (giống loadData của WinForms), mới nhất trước.
        /// Cột ID đặt alias "Id" để key JSON là "id" (khóa của lưới).
        /// </summary>
        [HttpGet]
        public IActionResult ExchangeRateList()
        {
            try
            {
                DataTable dt = DataTableHelper.ExecuteQuery(@"
SELECT a.ID AS Id, a.DateTime, a.FromCurrencyID, a.ToCurrencyID,
       a.ExChangeRate AS BuyRate, a.ExChangeRateSell AS SellRate,
       a.ExChangeRateMax, a.DenominationMax,
       a.ExChangeRateMedium, a.DenominationMedium,
       a.ExChangeRateMin, a.DenominationMin,
       b.LoginName AS CreateBy, a.CreateDate,
       c.LoginName AS UpdateBy, a.UpdateDate
FROM ExchangeRate a WITH (NOLOCK)
LEFT JOIN Users b ON a.UserInsertID = b.ID
LEFT JOIN Users c ON a.UserUpdateID = c.ID
ORDER BY a.DateTime DESC");
                return Ok(TableToRows(dt));
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Lưu tỷ giá (New: ID = 0, Edit: ID > 0). Kiểm tra giống frmGenExChangeRates.CheckForm:
        /// phải chọn tiền tệ, From khác To, ngày không nhỏ hơn business date,
        /// không trùng (ngày + cặp tiền tệ) khi thêm mới, và giới hạn giờ sửa tỷ giá của hôm nay.
        /// </summary>
        [HttpPost]
        public IActionResult SaveExchangeRate([FromBody] ExchangeRateSaveDto dto)
        {
            try
            {
                // Lấy UserID từ session để ghi người tạo/sửa
                int? userId = HttpContext.Session.GetInt32("UserID");
                if (userId == null)
                    return Unauthorized(new { success = false, message = "Session expired. Please login again." });
                if (dto == null)
                    return BadRequest(new { success = false, message = "Invalid data." });

                // ----- Kiểm tra dữ liệu nhập -----
                if (string.IsNullOrWhiteSpace(dto.FromCurrencyID) || string.IsNullOrWhiteSpace(dto.ToCurrencyID))
                    return BadRequest(new { success = false, message = "You must select currency for exchange !!!" });
                if (string.Equals(dto.FromCurrencyID, dto.ToCurrencyID, StringComparison.OrdinalIgnoreCase))
                    return BadRequest(new { success = false, message = "From currency, to currency is the same !!!" });
                if (dto.DateTime == null)
                    return BadRequest(new { success = false, message = "Please select a date." });

                // Ngày cấu hình không được nhỏ hơn business date
                DateTime cfgDate = dto.DateTime.Value.Date;
                DateTime busDateTime = ExCuBusinessDateTime();
                if (busDateTime.Date > cfgDate)
                    return BadRequest(new { success = false, message = "You must select config date more than or equal businessdate !!!" });

                // ----- Đã có tỷ giá cùng ngày + cặp tiền tệ? -----
                DataTable dup = DataTableHelper.ExecuteQuery(
                    "SELECT ID FROM ExchangeRate WHERE DATEDIFF(DAY, DateTime, @D) = 0 AND FromCurrencyID = @F AND ToCurrencyID = @T",
                    new SqlParameter[]
                    {
                        new SqlParameter("@D", cfgDate),
                        new SqlParameter("@F", dto.FromCurrencyID),
                        new SqlParameter("@T", dto.ToCurrencyID)
                    });

                if (dup.Rows.Count > 0)
                {
                    // Thêm mới mà đã có tỷ giá của ngày đó → không cho
                    if (dto.ID <= 0)
                        return BadRequest(new { success = false, message = "Exchange rate for day is confided !!!" });

                    // Sửa tỷ giá của ngày hôm nay sau giờ cấu hình (ConfigSystem.ExchangeRate_Time, mặc định 24 = không giới hạn).
                    // Lưu ý: Win miễn giới hạn này cho admin; web chưa có cách nhận biết admin nên áp cho mọi người dùng.
                    int hour = 24;
                    DataTable cfg = DataTableHelper.ExecuteQuery(
                        "SELECT [KeyValue] FROM dbo.[ConfigSystem] WHERE [KeyName] = 'ExchangeRate_Time'");
                    if (cfg.Rows.Count > 0) int.TryParse(cfg.Rows[0][0]?.ToString(), out hour);
                    if (busDateTime.Hour >= hour && ExCuSystemDate().Date == cfgDate)
                        return BadRequest(new { success = false, message = "You must config exchange today before " + hour + " !!!" });
                }

                DateTime now = ExCuSystemDate();
                int id = dto.ID;

                if (dto.ID <= 0)
                {
                    // ----- Thêm mới: ghi cả người tạo lẫn người sửa -----
                    const string sql = @"
INSERT INTO ExchangeRate
 (DateTime, FromCurrencyID, ToCurrencyID, ExChangeRate, ExChangeRateSell,
  UserInsertID, CreateDate, UserUpdateID, UpdateDate,
  ExChangeRateMax, DenominationMax, ExChangeRateMin, DenominationMin, ExChangeRateMedium, DenominationMedium)
VALUES
 (@DateTime, @FromCurrencyID, @ToCurrencyID, @ExChangeRate, @ExChangeRateSell,
  @UserID, @Now, @UserID, @Now,
  @Max, @DenoMax, @Min, @DenoMin, @Med, @DenoMed);
SELECT CAST(SCOPE_IDENTITY() AS int) AS ID";
                    id = DataTableHelper.ExecuteInsertAndReturnId(sql, ExRateParams(dto, cfgDate, userId.Value, now));
                }
                else
                {
                    // ----- Sửa: chỉ cập nhật người sửa / ngày sửa -----
                    const string sql = @"
UPDATE ExchangeRate SET
  DateTime = @DateTime, FromCurrencyID = @FromCurrencyID, ToCurrencyID = @ToCurrencyID,
  ExChangeRate = @ExChangeRate, ExChangeRateSell = @ExChangeRateSell,
  UserUpdateID = @UserID, UpdateDate = @Now,
  ExChangeRateMax = @Max, DenominationMax = @DenoMax,
  ExChangeRateMin = @Min, DenominationMin = @DenoMin,
  ExChangeRateMedium = @Med, DenominationMedium = @DenoMed
WHERE ID = @ID";
                    var ps = ExRateParams(dto, cfgDate, userId.Value, now).ToList();
                    ps.Add(new SqlParameter("@ID", dto.ID));
                    DataTableHelper.ExecuteNonQueryText(sql, ps.ToArray());
                }

                return Ok(new { success = true, id });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>Tạo danh sách tham số SQL dùng chung cho INSERT và UPDATE tỷ giá</summary>
        private static SqlParameter[] ExRateParams(ExchangeRateSaveDto d, DateTime date, int userId, DateTime now)
        {
            return new[]
            {
                new SqlParameter("@DateTime", date),
                new SqlParameter("@FromCurrencyID", d.FromCurrencyID),
                new SqlParameter("@ToCurrencyID", d.ToCurrencyID),
                new SqlParameter("@ExChangeRate", d.ExChangeRate),
                new SqlParameter("@ExChangeRateSell", d.ExChangeRateSell),
                new SqlParameter("@UserID", userId),
                new SqlParameter("@Now", now),
                new SqlParameter("@Max", d.ExChangeRateMax),
                new SqlParameter("@DenoMax", d.DenominationMax),
                new SqlParameter("@Min", d.ExChangeRateMin),
                new SqlParameter("@DenoMin", d.DenominationMin),
                new SqlParameter("@Med", d.ExChangeRateMedium),
                new SqlParameter("@DenoMed", d.DenominationMedium)
            };
        }
        #endregion
    }
}




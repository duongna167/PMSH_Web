using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using BaseBusiness.BO;
using BaseBusiness.Model;
using BaseBusiness.util;
using DevExpress.CodeParser;
using DevExpress.XtraReports.UI;
using DevExpress.XtraRichEdit.Import.Doc;
using DevExpress.XtraRichEdit.Import.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using BaseBusiness.Auth;
using Report.Services.Implements;
using Report.Services.Interfaces;

namespace Report.Controllers
{
    public class ReportController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ReportController> _logger;
        private readonly IMemoryCache _cache;
        private readonly IReportService _iReportService;
        private readonly ICompositeViewEngine _viewEngine;

        public ReportController(ILogger<ReportController> logger,
             IMemoryCache cache, IConfiguration configuration, IReportService iReportService,
             ICompositeViewEngine viewEngine)
        {
            _cache = cache;
            _logger = logger;
            _configuration = configuration;
            _iReportService = iReportService;
            _viewEngine = viewEngine;
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult BookingSource()
        {
            List<RateCodeModel> list = PropertyUtils.ConvertToList<RateCodeModel>(RateCodeBO.Instance.FindAll());
            ViewBag.RateCodeList = list;
            return View();

        }
        public IActionResult ReservationSummaryReport()
        {
            return View();

        }
        public IActionResult GroupReservationReport()
        {
            return View();

        }
        public IActionResult GuestStayOverReport()
        {
            return View();

        }
        public IActionResult GuestStayReport()
        {
            return View();

        }
        public IActionResult TraceReport()
        {
            List<RoomClassModel> list = PropertyUtils.ConvertToList<RoomClassModel>(RoomClassBO.Instance.FindAll());
            List<DepartmentModel> list2 = PropertyUtils.ConvertToList<DepartmentModel>(DepartmentBO.Instance.FindAll());
            ViewBag.RoomClassList = list;
            ViewBag.DepartmentList = list2;
            return View();

        }
        public IActionResult ReportNationalityStatistics()
        {
            List<RoomClassModel> list = PropertyUtils.ConvertToList<RoomClassModel>(RoomClassBO.Instance.FindAll());
            return View();

        }
        public IActionResult OtherReport()
        {
            List<ConfigSystemModel> list = PropertyUtils.ConvertToList<ConfigSystemModel>(ConfigSystemBO.Instance.FindAll());

            // Tìm dòng có KeyValue = "NameCompany"
            var companyConfig = list.FirstOrDefault(x => x.KeyValue == "NameCompany");
            // Gửi dữ liệu qua View
            ViewBag.CompanyName = companyConfig.Desciption;
            return View();

        }
        public IActionResult OtherReportChart()
        {
            List<ConfigSystemModel> list = PropertyUtils.ConvertToList<ConfigSystemModel>(ConfigSystemBO.Instance.FindAll());

            // Tìm dòng có KeyValue = "NameCompany"
            var companyConfig = list.FirstOrDefault(x => x.KeyValue == "NameCompany");
            // Gửi dữ liệu qua View
            ViewBag.CompanyName = companyConfig.Desciption;
            return View();

        }

        private static readonly Dictionary<string, string> ChartReportCodes = new(StringComparer.OrdinalIgnoreCase)
        {
            ["RoomOccupancyChart"] = "rptRoomOccupancyChart",
            ["RevenueDetailChart"] = "rptRevenueDetailChart",
            ["RevenueReportChart"] = "rptRevenueReportChart",
            ["RevenueByChart"] = "rptRevenueByChart",
            ["ReservationCancellationChart"] = "rptReservationCancellationChart",
        };

        private static readonly Dictionary<string, string> ChartReportTitles = new(StringComparer.OrdinalIgnoreCase)
        {
            ["RoomOccupancyChart"] = "Room Occupancy Chart",
            ["RevenueDetailChart"] = "Revenue Detail Chart",
            ["RevenueReportChart"] = "Revenue Report Chart",
            ["RevenueByChart"] = "Revenue By Chart",
            ["ReservationCancellationChart"] = "Reservation Cancellation Chart",
        };

        [HttpGet]
        public IActionResult ChartReport(string name)
        {
            if (HttpContext.Session.GetInt32("UserID") is null or <= 0)
                return Redirect("/User/Index");

            if (string.IsNullOrWhiteSpace(name) || !ChartReportCodes.TryGetValue(name, out var code))
                return Redirect("/Home/Index?denied=1");

            if (!SessionHasFunction(code)
                && !SessionHasFunction("rpt_ReportByChart_web")
                && !SessionHasFunction("rpt_ViewReport"))
                return Redirect("/Home/Index?denied=1");

            if (name.Equals("RoomOccupancyChart", StringComparison.OrdinalIgnoreCase)
                || name.Equals("RevenueByChart", StringComparison.OrdinalIgnoreCase))
            {
                ViewBag.ZoneList = PropertyUtils.ConvertToList<ZoneModel>(ZoneBO.Instance.FindAll());
                ViewBag.RoomTypeList = PropertyUtils.ConvertToList<RoomTypeModel>(RoomTypeBO.Instance.FindAll());
            }
            else if (name.Equals("ReservationCancellationChart", StringComparison.OrdinalIgnoreCase))
            {
                ViewBag.ComList = PropertyUtils.ConvertToList<CommentModel>(CommentBO.Instance.FindAll());
            }

            ViewBag.ReportTitle = ChartReportTitles[name];
            return View(name);
        }

        private bool SessionHasFunction(string code)
        {
            var raw = HttpContext.Session.GetString("PermissionCodes");
            if (string.IsNullOrWhiteSpace(raw) || string.IsNullOrWhiteSpace(code))
                return false;

            try
            {
                var list = System.Text.Json.JsonSerializer.Deserialize<string[]>(raw) ?? Array.Empty<string>();
                return list.Any(item => string.Equals(item?.Trim(), code.Trim(), StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return false;
            }
        }
        public IActionResult FreeUpgradeReport()
        {
            return View();

        }
        public IActionResult RatecodebyDateReport()
        {
            List<RateCodeModel> list = PropertyUtils.ConvertToList<RateCodeModel>(RateCodeBO.Instance.FindAll());
            ViewBag.RateCodeList = list;
            return View();

        }
        public IActionResult GuestMarketReport()
        {
            List<ZoneModel> list = PropertyUtils.ConvertToList<ZoneModel>(ZoneBO.Instance.FindAll());
            ViewBag.ZoneList = list;
            return View();
        }
        [HttpGet]
        public IActionResult GuestMarket(DateTime fromDate, DateTime toDate, int  zonecode, string  currency)
        {
            try
            { 
                List<RoomTypeModel> roomTypesInZone = PropertyUtils
                .ConvertToList<RoomTypeModel>(RoomTypeBO.Instance.FindAll())
                .Where(rt => rt.ZoneID == zonecode)
                .ToList();

            // Lấy danh sách các ID, nối thành chuỗi nếu GuestMarketReport yêu cầu string zonecode
            string zone = string.Join(",", roomTypesInZone.Select(rt => rt.ID.ToString()));

            // Tạo report
            //XtraReport report = new OneSPMSh.Report.ReportNationalityStatistics();

            // Lấy dữ liệu cho báo cáo
            DataTable dataTable = _iReportService.GuestMarketReport(fromDate, toDate, currency, zone);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  MarketType = !string.IsNullOrEmpty(d["MarketType"].ToString()) ? d["MarketType"] : "",
                                  Description = !string.IsNullOrEmpty(d["Description"].ToString()) ? d["Description"] : "",
                                  Code = !string.IsNullOrEmpty(d["Code"].ToString()) ? d["Code"] : "",
                                  D_Persons = !string.IsNullOrEmpty(d["D_Persons"].ToString()) ? d["D_Persons"] : "",
                                  D_Room = !string.IsNullOrEmpty(d["D_Room"].ToString()) ? d["D_Room"] : "",
                                  D_Occ = !string.IsNullOrEmpty(d["D_Occ"].ToString()) ? d["D_Occ"] : "",
                                  _D_Occ = !string.IsNullOrEmpty(d["_D_Occ"].ToString()) ? d["_D_Occ"] : "",
                                  D_Accom = !string.IsNullOrEmpty(d["D_Accom"].ToString()) ? d["D_Accom"] : "",
                                  _D_Accom = !string.IsNullOrEmpty(d["_D_Accom"].ToString()) ? d["_D_Accom"] : "",
                                  D_TotalAccom = !string.IsNullOrEmpty(d["D_TotalAccom"].ToString()) ? d["D_TotalAccom"] : "",

                                  M_Persons = !string.IsNullOrEmpty(d["M_Persons"].ToString()) ? d["M_Persons"] : "",
                                  M_Room = !string.IsNullOrEmpty(d["M_Room"].ToString()) ? d["M_Room"] : "",
                                  M_Occ = !string.IsNullOrEmpty(d["M_Occ"].ToString()) ? d["M_Occ"] : "",
                                  _M_Occ = !string.IsNullOrEmpty(d["_M_Occ"].ToString()) ? d["_M_Occ"] : "",
                                  M_Accom = !string.IsNullOrEmpty(d["M_Accom"].ToString()) ? d["M_Accom"] : "",

                                  Y_Persons = !string.IsNullOrEmpty(d["Y_Persons"].ToString()) ? d["Y_Persons"] : "",
                                  Y_Room = !string.IsNullOrEmpty(d["Y_Room"].ToString()) ? d["Y_Room"] : "",
                                  Y_Occ = !string.IsNullOrEmpty(d["Y_Occ"].ToString()) ? d["Y_Occ"] : "",
                                  _Y_Occ = !string.IsNullOrEmpty(d["_Y_Occ"].ToString()) ? d["_Y_Occ"] : "",
                                  Y_Accom = !string.IsNullOrEmpty(d["Y_Accom"].ToString()) ? d["Y_Accom"] : "",

                                  M_TotalAccom = !string.IsNullOrEmpty(d["M_TotalAccom"].ToString()) ? d["M_TotalAccom"] : "",
                                  Y_TotalAccom = !string.IsNullOrEmpty(d["Y_TotalAccom"].ToString()) ? d["Y_TotalAccom"] : "",
                                  Total_D_Occ = !string.IsNullOrEmpty(d["Total_D_Occ"].ToString()) ? d["Total_D_Occ"] : "",

                                  Total_M_Occ = !string.IsNullOrEmpty(d["Total_M_Occ"].ToString()) ? d["Total_M_Occ"] : "",
                                  Total_Y_Occ = !string.IsNullOrEmpty(d["Total_Y_Occ"].ToString()) ? d["Total_Y_Occ"] : "",
                                  CurrencyID = !string.IsNullOrEmpty(d["CurrencyID"].ToString()) ? d["CurrencyID"] : "",
                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        [HttpGet]
        public IActionResult RatecodebyDate(DateTime fromDate, DateTime toDate, string ratecode)
        {
            try
            {
                //XtraReport report = new OneSPMSh.Report.RatecodebyDateReport();

                DataTable dataTable = _iReportService.RatecodebyDate(fromDate, toDate, ratecode);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  RateCode = !string.IsNullOrEmpty(d["RateCode"].ToString()) ? d["RateCode"] : "",
                                  Descripton = !string.IsNullOrEmpty(d["Descripton"].ToString()) ? d["Descripton"] : "",
                                  FromDate = !string.IsNullOrEmpty(d["FromDate"].ToString()) ? d["FromDate"] : "",
                                  ToDate = !string.IsNullOrEmpty(d["ToDate"].ToString()) ? d["ToDate"] : "",
                                  Code = !string.IsNullOrEmpty(d["Code"].ToString()) ? d["Code"] : "",
                                  Rate = !string.IsNullOrEmpty(d["Rate"].ToString()) ? d["Rate"] : "",
                                  RoomTypeCode = !string.IsNullOrEmpty(d["RoomTypeCode"].ToString()) ? d["RoomTypeCode"] : "",
                                  CurrencyID = !string.IsNullOrEmpty(d["CurrencyID"].ToString()) ? d["CurrencyID"] : "",
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
        [HttpGet]
        public IActionResult NationalityStatistics(DateTime fromDate, DateTime toDate, string status, string sortOder)
        {
            // XtraReport report = new OneSPMSh.Report.ReportNationalityStatistics();
            try
            {
                DataTable dataTable = _iReportService.ReportNationalityStatistics(fromDate, toDate, status, sortOder);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  Nationality = !string.IsNullOrEmpty(d["Nationality"].ToString()) ? d["Nationality"] : "",
                                  Description = !string.IsNullOrEmpty(d["Description"].ToString()) ? d["Description"] : "",
                                  ArrAdult = !string.IsNullOrEmpty(d["ArrAdult"].ToString()) ? d["ArrAdult"] : "",
                                  ArrChild = !string.IsNullOrEmpty(d["ArrChild"].ToString()) ? d["ArrChild"] : "",
                                  ArrChild1 = !string.IsNullOrEmpty(d["ArrChild1"].ToString()) ? d["ArrChild1"] : "",
                                  ArrChild2 = !string.IsNullOrEmpty(d["ArrChild2"].ToString()) ? d["ArrChild2"] : "",
                                  ArrTotal = !string.IsNullOrEmpty(d["ArrTotal"].ToString()) ? d["ArrTotal"] : "",
                                  ArrPercen = !string.IsNullOrEmpty(d["ArrPercen"].ToString()) ? d["ArrPercen"] : "",
                                  NightAdult = !string.IsNullOrEmpty(d["NightAdult"].ToString()) ? d["NightAdult"] : "",
                                  NightChild = !string.IsNullOrEmpty(d["NightChild"].ToString()) ? d["NightChild"] : "",
                                  NightChild1 = !string.IsNullOrEmpty(d["NightChild1"].ToString()) ? d["NightChild1"] : "",
                                  NightChild2 = !string.IsNullOrEmpty(d["NightChild2"].ToString()) ? d["NightChild2"] : "",
                                  BedNightPercen = !string.IsNullOrEmpty(d["BedNightPercen"].ToString()) ? d["BedNightPercen"] : "",
                                  NightTotal = !string.IsNullOrEmpty(d["NightTotal"].ToString()) ? d["NightTotal"] : "",
                                  RoomNightPercen = !string.IsNullOrEmpty(d["RoomNightPercen"].ToString()) ? d["RoomNightPercen"] : "",
                                  StayDur = !string.IsNullOrEmpty(d["StayDur"].ToString()) ? d["StayDur"] : "",
                                  PYStayDur = !string.IsNullOrEmpty(d["PYStayDur"].ToString()) ? d["PYStayDur"] : "",
                                  ArrPreYear = !string.IsNullOrEmpty(d["ArrPreYear"].ToString()) ? d["ArrPreYear"] : "",
                                  NightPreYear = !string.IsNullOrEmpty(d["NightPreYear"].ToString()) ? d["NightPreYear"] : "",
                                  RoomNightPreYear = !string.IsNullOrEmpty(d["RoomNightPreYear"].ToString()) ? d["RoomNightPreYear"] : "",
                                  RoomNightPreYPercen = !string.IsNullOrEmpty(d["RoomNightPreYPercen"].ToString()) ? d["RoomNightPreYPercen"] : "",
                                  ProfitLoss = !string.IsNullOrEmpty(d["ProfitLoss"].ToString()) ? d["ProfitLoss"] : "",
                                  ProfitLossPercen = !string.IsNullOrEmpty(d["ProfitLossPercen"].ToString()) ? d["ProfitLossPercen"] : "",
                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
            // report.DataSource = dataTable;

            // Không cần gán parameter
            //report.RequestParameters = false;

            // return PartialView("_ReportViewerPartial", report);
        }
        [HttpGet]
        public IActionResult BookingSourceData(DateTime fromDate, DateTime toDate)
        {
            //XtraReport report = new OneSPMSh.Report.Report1();
            try
            {
                DataTable dataTable = _iReportService.GetBookingSourceData(fromDate, toDate);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  Source = !string.IsNullOrEmpty(d["Source"].ToString()) ? d["Source"] : "",
                                  RNActual = !string.IsNullOrEmpty(d["RNActual"].ToString()) ? d["RNActual"] : "",
                                  BUSMIXActual = !string.IsNullOrEmpty(d["BUSMIXActual"].ToString()) ? d["BUSMIXActual"] : "",
                                  ADRActual = !string.IsNullOrEmpty(d["ADRActual"].ToString()) ? d["ADRActual"] : "",
                                  REVForcast = !string.IsNullOrEmpty(d["REVForcast"].ToString()) ? d["REVForcast"] : "",
                                  REVMIXForcast = !string.IsNullOrEmpty(d["REVMIXForcast"].ToString()) ? d["REVMIXForcast"] : "",
                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
            //report.DataSource = dataTable;

            //// Không cần gán parameter
            //report.RequestParameters = false;

            //return PartialView("_ReportViewerPartial", report);
        }
        public ActionResult RenderDailyBreakfastDetail()
        {
            return PartialView("ReservationSummaryReport"); // Tên file cshtml
        }

        [HttpGet]
        public IActionResult ReservationSummary(DateTime fromDate, DateTime toDate)
        {
            //XtraReport report = new OneSPMSh.Report.ReservationSummaryReport();

            try
            {
                DataTable dataTable = _iReportService.ReservationSummaryReport(fromDate, toDate);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  ByBookingCode = !string.IsNullOrEmpty(d["ByBookingCode"].ToString()) ? d["ByBookingCode"] : "",
                                  ByRoom = !string.IsNullOrEmpty(d["ByRoom"].ToString()) ? d["ByRoom"] : "",
                                  ByRoomNight = !string.IsNullOrEmpty(d["ByRoomNight"].ToString()) ? d["ByRoomNight"] : "",

                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
            // report.DataSource = dataTable;

            // Không cần gán parameter
            //  report.RequestParameters = false;

            //  return PartialView("_ReportViewerPartial", report);
        }

        [HttpGet]
        public IActionResult BlacklistReporteData(DateTime fromDate, DateTime toDate)
        {
            //XtraReport report = new OneSPMSh.Report.ReservationSummaryReport();

            try
            {
                DataTable dataTable = _iReportService.BlacklistReporteData(fromDate, toDate);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  Nat = !string.IsNullOrEmpty(d["Nat"].ToString()) ? d["Nat"] : "",
                                  Account = !string.IsNullOrEmpty(d["Account"].ToString()) ? d["Account"] : "",
                                  SpecialUpdateBy = !string.IsNullOrEmpty(d["SpecialUpdateBy"].ToString()) ? d["SpecialUpdateBy"] : "",
                                  SpecialUpdateDate = !string.IsNullOrEmpty(d["SpecialUpdateDate"].ToString()) ? d["SpecialUpdateDate"] : "",

                                  Passport = !string.IsNullOrEmpty(d["Passport"].ToString()) ? d["Passport"] : "",
                                  DateOfBirth = !string.IsNullOrEmpty(d["DateOfBirth"].ToString()) ? d["DateOfBirth"] : "",
                                  ConfirmationNo = !string.IsNullOrEmpty(d["ConfirmationNo"].ToString()) ? d["ConfirmationNo"] : "",
                                  BlackListReason = !string.IsNullOrEmpty(d["BlackListReason"].ToString()) ? d["BlackListReason"] : "",

                                  RoomNo = !string.IsNullOrEmpty(d["RoomNo"].ToString()) ? d["RoomNo"] : "",
                                  ArrivalDate = !string.IsNullOrEmpty(d["ArrivalDate"].ToString()) ? d["ArrivalDate"] : "",
                                  DepartureDate = !string.IsNullOrEmpty(d["DepartureDate"].ToString()) ? d["DepartureDate"] : "",

                                  ReservationHolder = !string.IsNullOrEmpty(d["ReservationHolder"].ToString()) ? d["ReservationHolder"] : "",
                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
            // report.DataSource = dataTable;

            // Không cần gán parameter
            //  report.RequestParameters = false;

            //  return PartialView("_ReportViewerPartial", report);
        }
        [HttpGet]
        public IActionResult GroupReservation(DateTime fromDate, DateTime toDate, string noofRoom)
        {
            //XtraReport report = new OneSPMSh.Report.GroupReservationReport();
            try
            {
                DataTable dataTable = _iReportService.GroupReservation(fromDate, toDate, noofRoom);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  ConfirmationNo = !string.IsNullOrEmpty(d["ConfirmationNo"].ToString()) ? d["ConfirmationNo"] : "",
                                  CRSNo = !string.IsNullOrEmpty(d["CRSNo"].ToString()) ? d["CRSNo"] : "",
                                  GroupCode = !string.IsNullOrEmpty(d["GroupCode"].ToString()) ? d["GroupCode"] : "",
                                  ReservationTypeCode = !string.IsNullOrEmpty(d["ReservationTypeCode"].ToString()) ? d["ReservationTypeCode"] : "",
                                  ArrivalDate = !string.IsNullOrEmpty(d["ArrivalDate"].ToString()) ? d["ArrivalDate"] : "",
                                  DepartureDate = !string.IsNullOrEmpty(d["DepartureDate"].ToString()) ? d["DepartureDate"] : "",
                                  TotalRoom = !string.IsNullOrEmpty(d["TotalRoom"].ToString()) ? d["TotalRoom"] : "",
                                  Nights = !string.IsNullOrEmpty(d["Nights"].ToString()) ? d["Nights"] : "",
                                  ReservationHolder = !string.IsNullOrEmpty(d["ReservationHolder"].ToString()) ? d["ReservationHolder"] : "",
                                  SaleInCharge = !string.IsNullOrEmpty(d["SaleInCharge"].ToString()) ? d["SaleInCharge"] : "",
                                  RoomNight = !string.IsNullOrEmpty(d["RoomNight"].ToString()) ? d["RoomNight"] : "",
                                  Persons = !string.IsNullOrEmpty(d["Persons"].ToString()) ? d["Persons"] : "",
                                  RoomOccupancy = !string.IsNullOrEmpty(d["RoomOccupancy"].ToString()) ? d["RoomOccupancy"] : "",
                                  PersonRoom = !string.IsNullOrEmpty(d["PersonRoom"].ToString()) ? d["PersonRoom"] : "",

                                  MarketCode = !string.IsNullOrEmpty(d["MarketCode"].ToString()) ? d["MarketCode"] : "",
                                  SourceCode = !string.IsNullOrEmpty(d["SourceCode"].ToString()) ? d["SourceCode"] : "",
                                  ReservationDate = !string.IsNullOrEmpty(d["ReservationDate"].ToString()) ? d["ReservationDate"] : "",
                                  Price = !string.IsNullOrEmpty(d["Price"].ToString()) ? d["Price"] : "",
                                  AmountBeforTax = !string.IsNullOrEmpty(d["AmountBeforTax"].ToString()) ? d["AmountBeforTax"] : "",
                                  AmountAfterTax = !string.IsNullOrEmpty(d["AmountAfterTax"].ToString()) ? d["AmountAfterTax"] : "",

                                  CurrencyID = !string.IsNullOrEmpty(d["Price"].ToString()) ? d["Price"] : "",
                                  OptionDate = !string.IsNullOrEmpty(d["OptionDate"].ToString()) ? d["OptionDate"] : "",
                                  OptionDateDesc = !string.IsNullOrEmpty(d["OptionDateDesc"].ToString()) ? d["OptionDateDesc"] : "",
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
        [HttpGet]
        public IActionResult GuestStayOver(DateTime fromDate, DateTime toDate)
        {
            //XtraReport report = new OneSPMSh.Report.GuestStayOverReport();
            try
            {
                DataTable dataTable = _iReportService.GuestStayOver(fromDate, toDate);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  ConfirmationNo = !string.IsNullOrEmpty(d["ConfirmationNo"].ToString()) ? d["ConfirmationNo"] : "",
                                  Lastname = !string.IsNullOrEmpty(d["Lastname"].ToString()) ? d["Lastname"] : "",
                                  Title = !string.IsNullOrEmpty(d["Title"].ToString()) ? d["Title"] : "",
                                  Country = !string.IsNullOrEmpty(d["Country"].ToString()) ? d["Country"] : "",
                                  ArrivalDate = !string.IsNullOrEmpty(d["ArrivalDate"].ToString()) ? d["ArrivalDate"] : "",
                                  DepartureDate = !string.IsNullOrEmpty(d["DepartureDate"].ToString()) ? d["DepartureDate"] : "",
                                  NoOfAdult = !string.IsNullOrEmpty(d["NoOfAdult"].ToString()) ? d["NoOfAdult"] : "",
                                  NoOfChild = !string.IsNullOrEmpty(d["NoOfChild"].ToString()) ? d["NoOfChild"] : "",
                                  NoOfChild1 = !string.IsNullOrEmpty(d["NoOfChild1"].ToString()) ? d["NoOfChild1"] : "",
                                  NoOfChild2 = !string.IsNullOrEmpty(d["NoOfChild2"].ToString()) ? d["NoOfChild2"] : "",
                                  NoOfRoom = !string.IsNullOrEmpty(d["NoOfRoom"].ToString()) ? d["NoOfRoom"] : "",
                                  RoomType = !string.IsNullOrEmpty(d["RoomType"].ToString()) ? d["RoomType"] : "",
                                  RoomNo = !string.IsNullOrEmpty(d["RoomNo"].ToString()) ? d["RoomNo"] : "",
                                  RoomNight = !string.IsNullOrEmpty(d["RoomNight"].ToString()) ? d["RoomNight"] : "",
                                  Code = !string.IsNullOrEmpty(d["Code"].ToString()) ? d["Code"] : "",
                                  ReservationHolder = !string.IsNullOrEmpty(d["ReservationHolder"].ToString()) ? d["ReservationHolder"] : "",

                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
            // report.DataSource = dataTable;

            // Không cần gán parameter
            // report.RequestParameters = false;

            // return PartialView("_ReportViewerPartial", report);
        }

        [HttpGet]
        public IActionResult GuestStay(string noofName, string stayno, string stayand)
        {
            //XtraReport report = new OneSPMSh.Report.GuestStayReport();
            try
            {
                DataTable dataTable = _iReportService.GuestStay(noofName, stayno, stayand);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  Code = !string.IsNullOrEmpty(d["Code"].ToString()) ? d["Code"] : "",
                                  Vip = !string.IsNullOrEmpty(d["Vip"].ToString()) ? d["Vip"] : "",
                                  GuestName = !string.IsNullOrEmpty(d["GuestName"].ToString()) ? d["GuestName"] : "",
                                  StayNo = !string.IsNullOrEmpty(d["StayNo"].ToString()) ? d["StayNo"] : "",
                                  DateOfBirth = !string.IsNullOrEmpty(d["DateOfBirth"].ToString()) ? d["DateOfBirth"] : "",
                                  Passport = !string.IsNullOrEmpty(d["Passport"].ToString()) ? d["Passport"] : "",
                                  IdentityCard = !string.IsNullOrEmpty(d["IdentityCard"].ToString()) ? d["IdentityCard"] : "",
                                  Address = !string.IsNullOrEmpty(d["Address"].ToString()) ? d["Address"] : "",
                                  City = !string.IsNullOrEmpty(d["City"].ToString()) ? d["City"] : "",
                                  Nat = !string.IsNullOrEmpty(d["Nat"].ToString()) ? d["Nat"] : "",
                                  HandPhone = !string.IsNullOrEmpty(d["HandPhone"].ToString()) ? d["HandPhone"] : "",
                                  Telephone = !string.IsNullOrEmpty(d["Telephone"].ToString()) ? d["Telephone"] : "",


                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
            // report.DataSource = dataTable;

            // Không cần gán parameter
            //report.RequestParameters = false;

            //     return PartialView("_ReportViewerPartial", report);
        }
        [HttpGet]
        public IActionResult OTAsMonthlyReport(DateTime fromDate, string Number, string type, string currencyID)
        {
            Number = Number ?? "";
            try
            {
                var startDate = fromDate;
                var months = Enumerable.Range(0, 3).Select(i => startDate.AddMonths(i)).ToList();

                // Tách type string thành list
                var typeList = type.Split(',', StringSplitOptions.RemoveEmptyEntries);

                List<DataRow> allRows = new List<DataRow>();

                foreach (var t in typeList)
                {
                    // Gọi service cho từng loại type riêng biệt
                    var dt = _iReportService.OTAMonthlyReport(fromDate, Number, t.Trim(), currencyID);
                    allRows.AddRange(dt.AsEnumerable());
                }

                // Gộp kết quả vào danh sách duy nhất
                var result = allRows.Select(d => new
                {
                    ProfileID = d["ProfileID"]?.ToString() ?? "",
                    GuestNo = d["GuestNo"]?.ToString() ?? "",
                    GuestName = d["GuestName"]?.ToString() ?? "",
                    City = d["City"]?.ToString() ?? "",
                    CurrencyID = d["CurrencyID"]?.ToString() ?? "",

                    RoomRevenue = months.Select(m => new
                    {
                        Month = m.ToString("MMM-yyyy"),
                        Value = d[$"RR_{m.ToString("MMM", CultureInfo.InvariantCulture)}"]?.ToString() ?? ""
                    }),
                    RoomNights = months.Select(m => new
                    {
                        Month = m.ToString("MMM-yyyy"),
                        Value = d[$"RN_{m.ToString("MMM", CultureInfo.InvariantCulture)}"]?.ToString() ?? ""
                    })
                }).ToList();

                var monthHeaders = months.Select(m => m.ToString("MMM-yyyy")).ToList();

                return Json(new
                {
                    Data = result,
                    MonthHeaders = monthHeaders
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }


        [HttpGet]
        public IActionResult TraceReportView(DateTime fromDate, DateTime toDate, int roomClass, int department, int status, int byAlphabetical, int byRoom, int byVip, int pseudoRoom, int reserved, int checkedIn, int dueout, int individual, int blockcode, int vipOnly)
        {
            XtraReport report = new OneSPMSh.Report.TraceReport();

            DataTable dataTable = _iReportService.TraceReportView(fromDate, toDate, roomClass, department, status, byAlphabetical, byRoom, byVip, pseudoRoom, reserved, checkedIn, dueout, individual, blockcode, vipOnly);

            // 👉 Thêm cột TraceDateOnly
            if (!dataTable.Columns.Contains("TraceDateOnly"))
                dataTable.Columns.Add("TraceDateOnly", typeof(DateTime));

            foreach (DataRow row in dataTable.Rows)
            {
                if (row["TraceDate"] != DBNull.Value)
                {
                    DateTime traceDate = Convert.ToDateTime(row["TraceDate"]);
                    row["TraceDateOnly"] = traceDate.Date; // 🛠 chỉ lấy phần Ngày
                }
            }
            report.Parameters["DateIn"].Value = DateTime.Now;
            report.DataSource = dataTable;
            report.RequestParameters = false;

            return PartialView("_ReportViewerPartial", report);
        }

        [HttpGet]
        public IActionResult DailyPickupReport(DateTime fromDate, DateTime toDate,string zone)
        {
            //XtraReport report = new OneSPMSh.Report.GuestStayReport();
            try
            {
                DataTable dataTable = _iReportService.DailyPickupReport(fromDate, toDate, zone);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  No = !string.IsNullOrEmpty(d["No"].ToString()) ? d["No"] : "",
                                  ConfirmationNo = !string.IsNullOrEmpty(d["ConfirmationNo"].ToString()) ? d["ConfirmationNo"] : "",
                                  GuestName = !string.IsNullOrEmpty(d["GuestName"].ToString()) ? d["GuestName"] : "",
                                  Company = !string.IsNullOrEmpty(d["Company"].ToString()) ? d["Company"] : "",
                                  ArrivalDate = !string.IsNullOrEmpty(d["ArrivalDate"].ToString()) ? d["ArrivalDate"] : "",
                                  DepartureDate = !string.IsNullOrEmpty(d["DepartureDate"].ToString()) ? d["DepartureDate"] : "",
                                  NoOfRoom = !string.IsNullOrEmpty(d["NoOfRoom"].ToString()) ? d["NoOfRoom"] : "",
                                  NoOfNight = !string.IsNullOrEmpty(d["NoOfNight"].ToString()) ? d["NoOfNight"] : "",
                                  RoomType = !string.IsNullOrEmpty(d["RoomType"].ToString()) ? d["RoomType"] : "",
                                  RateAfterTax = !string.IsNullOrEmpty(d["RateAfterTax"].ToString()) ? d["RateAfterTax"] : "",
                                  Total = !string.IsNullOrEmpty(d["Total"].ToString()) ? d["Total"] : "",
                                  MarketCode = !string.IsNullOrEmpty(d["MarketCode"].ToString()) ? d["MarketCode"] : "",
                                  RateCode = !string.IsNullOrEmpty(d["RateCode"].ToString()) ? d["RateCode"] : "",
                                  CreatedBy = !string.IsNullOrEmpty(d["CreatedBy"].ToString()) ? d["CreatedBy"] : "",
                                

                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
            // report.DataSource = dataTable;

            // Không cần gán parameter
            //report.RequestParameters = false;

            //     return PartialView("_ReportViewerPartial", report);
        }

        [HttpGet]
        public IActionResult DailyBreakfastDetail(DateTime fromDate)
        {
            //XtraReport report = new OneSPMSh.Report.GuestStayReport();
            try
            {
                DataTable dataTable = _iReportService.DailyBreakfastDetail(fromDate);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  No = !string.IsNullOrEmpty(d["No"].ToString()) ? d["No"] : "",
                                  Title = !string.IsNullOrEmpty(d["Title"].ToString()) ? d["Title"] : "",
                                  ConfirmationNo = !string.IsNullOrEmpty(d["ConfirmationNo"].ToString()) ? d["ConfirmationNo"] : "",
                                  GuestName = !string.IsNullOrEmpty(d["GuestName"].ToString()) ? d["GuestName"] : "",
                                  ArrivalDate = !string.IsNullOrEmpty(d["ArrivalDate"].ToString()) ? d["ArrivalDate"] : "",
                                  DepartureDate = !string.IsNullOrEmpty(d["DepartureDate"].ToString()) ? d["DepartureDate"] : "",
                                  RoomNo = !string.IsNullOrEmpty(d["RoomNo"].ToString()) ? d["RoomNo"] : "",
                                  ProfileIndividualID = !string.IsNullOrEmpty(d["ProfileIndividualID"].ToString()) ? d["ProfileIndividualID"] : "",
                                  Price = !string.IsNullOrEmpty(d["Price"].ToString()) ? d["Price"] : "",
                                  Company = !string.IsNullOrEmpty(d["Company"].ToString()) ? d["Company"] : "",
                                  Person = !string.IsNullOrEmpty(d["Person"].ToString()) ? d["Person"] : "",
                                  Package = !string.IsNullOrEmpty(d["Package"].ToString()) ? d["Package"] : "",
                                  National = !string.IsNullOrEmpty(d["National"].ToString()) ? d["National"] : "",
                                  MainGuest = !string.IsNullOrEmpty(d["MainGuest"].ToString()) ? d["MainGuest"] : "",
                            


                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
            // report.DataSource = dataTable;

            // Không cần gán parameter
            //report.RequestParameters = false;

            //     return PartialView("_ReportViewerPartial", report);
        }

        [HttpGet]
        public IActionResult DepartureExtendedReport(DateTime fromDate)
        {
            //XtraReport report = new OneSPMSh.Report.GuestStayReport();
            try
            {
                DataTable dataTable = _iReportService.DepartureExtendedReport(fromDate);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  RoomNo = !string.IsNullOrEmpty(d["Room No."].ToString()) ? d["Room No."] : "",
                                  Name = !string.IsNullOrEmpty(d["Name"].ToString()) ? d["Name"] : "",
                                  ArrivalDate = !string.IsNullOrEmpty(d["Arrival Date"].ToString()) ? d["Arrival Date"] : "",
                                  OriginalDepDate = !string.IsNullOrEmpty(d["Original Dep. Date"].ToString()) ? d["Original Dep. Date"] : "",
                                  DepartureDate = !string.IsNullOrEmpty(d["Departure Date"].ToString()) ? d["Departure Date"] : "",
                                  Nts = !string.IsNullOrEmpty(d["Nts."].ToString()) ? d["Nts."] : "",
                                  Cur = !string.IsNullOrEmpty(d["Cur."].ToString()) ? d["Cur."] : "",
                                  Prs = !string.IsNullOrEmpty(d["Prs."].ToString()) ? d["Prs."] : "",
                                  Rms = !string.IsNullOrEmpty(d["Rms."].ToString()) ? d["Rms."] : "",
                                  Balance = !string.IsNullOrEmpty(d["Balance"].ToString()) ? d["Balance"] : "",
                                  ResvHolder = !string.IsNullOrEmpty(d["Resv. Holder"].ToString()) ? d["Resv. Holder"] : "",
                                  ResvStatus = !string.IsNullOrEmpty(d["Resv. Status"].ToString()) ? d["Resv. Status"] : "",
                                  DepartureTime = !string.IsNullOrEmpty(d["Departure Time"].ToString()) ? d["Departure Time"] : "",
  



                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
            // report.DataSource = dataTable;

            // Không cần gán parameter
            //report.RequestParameters = false;

            //     return PartialView("_ReportViewerPartial", report);
        }

        [HttpGet]
        public IActionResult DepartureIndividualAndGroupData(DateTime fromDate)
        {
            //XtraReport report = new OneSPMSh.Report.GuestStayReport();
            try
            {
                DataTable dataTable = _iReportService.DepartureIndividualAndGroupData(fromDate);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  RoomNo = !string.IsNullOrEmpty(d["RoomNo"].ToString()) ? d["RoomNo"] : "",
                                  RoomType = !string.IsNullOrEmpty(d["RoomType"].ToString()) ? d["RoomType"] : "",
                                  ArrivalDate = !string.IsNullOrEmpty(d["ArrivalDate"].ToString()) ? d["ArrivalDate"] : "",
                                  VIP = !string.IsNullOrEmpty(d["VIP"].ToString()) ? d["VIP"] : "",
                                  Name = !string.IsNullOrEmpty(d["Name"].ToString()) ? d["Name"] : "",
                                  NoOfRoom = !string.IsNullOrEmpty(d["NoOfRoom"].ToString()) ? d["NoOfRoom"] : "",
                                  NoOfAdult = !string.IsNullOrEmpty(d["NoOfAdult"].ToString()) ? d["NoOfAdult"] : "",
                                  NoOfChild = !string.IsNullOrEmpty(d["NoOfChild"].ToString()) ? d["NoOfChild"] : "",
                                  NoOfChild1 = !string.IsNullOrEmpty(d["NoOfChild1"].ToString()) ? d["NoOfChild1"] : "",
                                  NoOfNight = !string.IsNullOrEmpty(d["NoOfNight"].ToString()) ? d["NoOfNight"] : "",
                                  Group = !string.IsNullOrEmpty(d["Group"].ToString()) ? d["Group"] : "",
                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
            // report.DataSource = dataTable;

            // Không cần gán parameter
            //report.RequestParameters = false;

            //     return PartialView("_ReportViewerPartial", report);
        }

        [HttpGet]
        public IActionResult FixChargeReportData(DateTime fromDate ,DateTime toDate,string trancode,string status)
        {
            //XtraReport report = new OneSPMSh.Report.GuestStayReport();
            try
            {
                trancode = string.IsNullOrWhiteSpace(trancode) ? "" : trancode;
                var items = status.Split(',')
                                .Where(s => !string.IsNullOrWhiteSpace(s))
                                .Select(s => s.Trim())
                                .ToList();

                if (items.Count == 0)
                    return BadRequest("Status is empty.");

                var formattedList = new List<string>();

                for (int i = 0; i < items.Count; i++)
                {
                    string value = items[i];

                    // Nếu chỉ có 1 phần tử
                    if (items.Count == 1)
                    {
                        formattedList.Add(value);
                    }
                    // Nếu là phần tử đầu tiên và danh sách có nhiều phần tử
                    else if (i == 0)
                    {
                        formattedList.Add($"{value}'");
                    }
                    // Nếu là phần tử cuối cùng
                    else if (i == items.Count - 1)
                    {
                        formattedList.Add($"'{value}");
                    }
                    // Các phần tử ở giữa
                    else
                    {
                        formattedList.Add($"'{value}'");
                    }
                }

                string formattedStatus = string.Join(",", formattedList);
                DataTable dataTable = _iReportService.FixChargeReport(fromDate, toDate, trancode, formattedStatus);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  ConfNo = !string.IsNullOrEmpty(d["ConfNo"].ToString()) ? d["ConfNo"] : "",
                                  RoomNo = !string.IsNullOrEmpty(d["RoomNo"].ToString()) ? d["RoomNo"] : "",
                                  GuestName = !string.IsNullOrEmpty(d["GuestName"].ToString()) ? d["GuestName"] : "",
                                  Arr = !string.IsNullOrEmpty(d["Arr"].ToString()) ? d["Arr"] : "",
                                  Dep = !string.IsNullOrEmpty(d["Dep"].ToString()) ? d["Dep"] : "",
                                  TransactionCode = !string.IsNullOrEmpty(d["TransactionCode"].ToString()) ? d["TransactionCode"] : "",
                                  Description = !string.IsNullOrEmpty(d["Description"].ToString()) ? d["Description"] : "",
                                  Nbr = !string.IsNullOrEmpty(d["Nbr"].ToString()) ? d["Nbr"] : "",
                                  AmountNet = !string.IsNullOrEmpty(d["AmountNet"].ToString()) ? d["AmountNet"] : "",
                                  Amount = !string.IsNullOrEmpty(d["Amount"].ToString()) ? d["Amount"] : "",
                                  Curr = !string.IsNullOrEmpty(d["Curr"].ToString()) ? d["Curr"] : "",
                                  Status = !string.IsNullOrEmpty(d["Status"].ToString()) ? d["Status"] : "",
                                  ReservationHolder = !string.IsNullOrEmpty(d["ReservationHolder"].ToString()) ? d["ReservationHolder"] : "",
                                  BeginDate = !string.IsNullOrEmpty(d["BeginDate"].ToString()) ? d["BeginDate"] : "",
                                  EndDate = !string.IsNullOrEmpty(d["EndDate"].ToString()) ? d["EndDate"] : "",


                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
            // report.DataSource = dataTable;

            // Không cần gán parameter
            //report.RequestParameters = false;

            //     return PartialView("_ReportViewerPartial", report);
        }
        [HttpGet]
        public IActionResult FreeUpgradeReportData(DateTime fromDate, DateTime toDate,string viewBy,string status)
        {
            //XtraReport report = new OneSPMSh.Report.GuestStayReport();
            try
            {
                status = status ?? "";
                DataTable dataTable = _iReportService.FreeUpgradeReport(fromDate, toDate, viewBy, status);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {

                                  ConfirmationNo = !string.IsNullOrEmpty(d["ConfirmationNo"].ToString()) ? d["ConfirmationNo"] : "",
                                  LastName = !string.IsNullOrEmpty(d["LastName"].ToString()) ? d["LastName"] : "",
                                  ArrivalDate = !string.IsNullOrEmpty(d["ArrivalDate"].ToString()) ? d["ArrivalDate"] : "",
                                  DepartureDate = !string.IsNullOrEmpty(d["DepartureDate"].ToString()) ? d["DepartureDate"] : "",
                                  RoomNo = !string.IsNullOrEmpty(d["RoomNo"].ToString()) ? d["RoomNo"] : "",
                                  RoomType = !string.IsNullOrEmpty(d["RoomType"].ToString()) ? d["RoomType"] : "",
                                  ReservationHolder = !string.IsNullOrEmpty(d["ReservationHolder"].ToString()) ? d["ReservationHolder"] : "",
                                  RTC = !string.IsNullOrEmpty(d["RTC"].ToString()) ? d["RTC"] : "",
                                  RateCode = !string.IsNullOrEmpty(d["RateCode"].ToString()) ? d["RateCode"] : "",
                                  UpgradeBy = !string.IsNullOrEmpty(d["UpgradeBy"].ToString()) ? d["UpgradeBy"] : "",
                                  UpgradeWhy = !string.IsNullOrEmpty(d["UpgradeWhy"].ToString()) ? d["UpgradeWhy"] : "",



                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
            // report.DataSource = dataTable;

            // Không cần gán parameter
            //report.RequestParameters = false;

            //     return PartialView("_ReportViewerPartial", report);
        }

        [HttpGet]
        public IActionResult ReveunueByData(DateTime fromDate, DateTime toDate, string reservation, string roomType, string zone, string viewBy, string sortOrder)
        {
            //XtraReport report = new OneSPMSh.Report.GuestStayReport();
            try
            {
                roomType = roomType ?? "";
                zone = zone ?? "";
                DataTable dataTable = _iReportService.ReveunueByData(fromDate, toDate, reservation, roomType, zone, viewBy, sortOrder);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  RoomNo = !string.IsNullOrEmpty(d["RoomNo"].ToString()) ? d["RoomNo"] : "",
                                  FolioID = !string.IsNullOrEmpty(d["FolioID"].ToString()) ? d["FolioID"] : "",
                                  ConfirmationNo = !string.IsNullOrEmpty(d["ConfirmationNo"].ToString()) ? d["ConfirmationNo"] : "",
                                  GuestName = !string.IsNullOrEmpty(d["GuestName"].ToString()) ? d["GuestName"] : "",
                                  ReservationHolder = !string.IsNullOrEmpty(d["ReservationHolder"].ToString()) ? d["ReservationHolder"] : "",
                                  Status = !string.IsNullOrEmpty(d["Status"].ToString()) ? d["Status"] : "",
                                  ArrivalDate = !string.IsNullOrEmpty(d["ArrivalDate"].ToString()) ? d["ArrivalDate"] : "",
                                  DepartureDate = !string.IsNullOrEmpty(d["DepartureDate"].ToString()) ? d["DepartureDate"] : "",
                                  Currency = !string.IsNullOrEmpty(d["Currency"].ToString()) ? d["Currency"] : "",
                                  RoomRevenue = !string.IsNullOrEmpty(d["RoomRevenue"].ToString()) ? d["RoomRevenue"] : "",
                                  FBRevenue = !string.IsNullOrEmpty(d["FBRevenue"].ToString()) ? d["FBRevenue"] : "",
                                  OtherRevenue = !string.IsNullOrEmpty(d["OtherRevenue"].ToString()) ? d["OtherRevenue"] : "",
                                  TotalRevenue = !string.IsNullOrEmpty(d["TotalRevenue"].ToString()) ? d["TotalRevenue"] : "",
                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
            // report.DataSource = dataTable;

            // Không cần gán parameter
            //report.RequestParameters = false;

            //     return PartialView("_ReportViewerPartial", report);
        }
        [HttpGet]
        public IActionResult RoomOccupancyReport(DateTime fromDate, DateTime toDate, string zone)
        {
            //XtraReport report = new OneSPMSh.Report.GuestStayReport();
            try
            {
                zone = zone ?? "";
                DataTable dataTable = _iReportService.RoomOccupancyReport(fromDate, toDate, zone);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  Fair = !string.IsNullOrEmpty(d["Fair"].ToString()) ? d["Fair"] : "",
                                  Date = !string.IsNullOrEmpty(d["Date"].ToString()) ? d["Date"] : "",
                                  VacRo = !string.IsNullOrEmpty(d["Vac Ro"].ToString()) ? d["Vac Ro"] : "",
                                  ResRo = !string.IsNullOrEmpty(d["Res Ro"].ToString()) ? d["Res Ro"] : "",
                                  NonDeductRo = !string.IsNullOrEmpty(d["Non Deduct Ro"].ToString()) ? d["Non Deduct Ro"] : "",
                                  AltRo = !string.IsNullOrEmpty(d["Alt Ro"].ToString()) ? d["Alt Ro"] : "",
                                  TotaRo1 = !string.IsNullOrEmpty(d["Tota Ro1"].ToString()) ? d["Tota Ro1"] : "",
                                  ARo = !string.IsNullOrEmpty(d["A Ro"].ToString()) ? d["A Ro"] : "",
                                  DepRo = !string.IsNullOrEmpty(d["Dep Ro"].ToString()) ? d["Dep Ro"] : "",
                                  OOO = !string.IsNullOrEmpty(d["OOO"].ToString()) ? d["OOO"] : "",
                                  TotaRo2 = !string.IsNullOrEmpty(d["Tota Ro2"].ToString()) ? d["Tota Ro2"] : "",
                                  PT = !string.IsNullOrEmpty(d["%"].ToString()) ? d["%"] : "",
                                  MonthDisplay = !string.IsNullOrEmpty(d["MonthDisplay"].ToString()) ? d["MonthDisplay"] : "",
                                  MonthGroup = !string.IsNullOrEmpty(d["MonthGroup"].ToString()) ? d["MonthGroup"] : "",
                                  DOW = !string.IsNullOrEmpty(d["DOW"].ToString()) ? d["DOW"] : "",
                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
            // report.DataSource = dataTable;

            // Không cần gán parameter
            //report.RequestParameters = false;

            //     return PartialView("_ReportViewerPartial", report);
        }
        [HttpGet]
        public IActionResult RoomOccupancyChart(DateTime fromDate, DateTime toDate, string zone)
        {
            //XtraReport report = new OneSPMSh.Report.GuestStayReport();
            try
            {
                zone = zone ?? "";
                DataTable dataTable = _iReportService.RoomOccupancyReport(fromDate, toDate, zone);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  Fair = !string.IsNullOrEmpty(d["Fair"].ToString()) ? d["Fair"] : "",
                                  Date = !string.IsNullOrEmpty(d["Date"].ToString()) ? d["Date"] : "",
                                  VacRo = !string.IsNullOrEmpty(d["Vac Ro"].ToString()) ? d["Vac Ro"] : "",
                                  ResRo = !string.IsNullOrEmpty(d["Res Ro"].ToString()) ? d["Res Ro"] : "",
                                  NonDeductRo = !string.IsNullOrEmpty(d["Non Deduct Ro"].ToString()) ? d["Non Deduct Ro"] : "",
                                  AltRo = !string.IsNullOrEmpty(d["Alt Ro"].ToString()) ? d["Alt Ro"] : "",
                                  TotaRo1 = !string.IsNullOrEmpty(d["Tota Ro1"].ToString()) ? d["Tota Ro1"] : "",
                                  ARo = !string.IsNullOrEmpty(d["A Ro"].ToString()) ? d["A Ro"] : "",
                                  DepRo = !string.IsNullOrEmpty(d["Dep Ro"].ToString()) ? d["Dep Ro"] : "",
                                  OOO = !string.IsNullOrEmpty(d["OOO"].ToString()) ? d["OOO"] : "",
                                  TotaRo2 = !string.IsNullOrEmpty(d["Tota Ro2"].ToString()) ? d["Tota Ro2"] : "",
                                  PT = !string.IsNullOrEmpty(d["%"].ToString()) ? d["%"] : "",
                                  MonthDisplay = !string.IsNullOrEmpty(d["MonthDisplay"].ToString()) ? d["MonthDisplay"] : "",
                                  MonthGroup = !string.IsNullOrEmpty(d["MonthGroup"].ToString()) ? d["MonthGroup"] : "",
                                  DOW = !string.IsNullOrEmpty(d["DOW"].ToString()) ? d["DOW"] : "",
                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
            // report.DataSource = dataTable;

            // Không cần gán parameter
            //report.RequestParameters = false;

            //     return PartialView("_ReportViewerPartial", report);
        }

        [HttpGet]
        public IActionResult ReservationCancellationsReport(DateTime fromDate, DateTime toDate, string commnet, string typeDate, string zone)
        {
            //XtraReport report = new OneSPMSh.Report.GuestStayReport();
            try
            {
                commnet = commnet ?? "";
                zone = zone ?? "";
                DataTable dataTable = _iReportService.ReservationCancellationsReport(fromDate, toDate, commnet, typeDate, zone);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  Name = !string.IsNullOrEmpty(d["Name"].ToString()) ? d["Name"] : "",
                                  Group = !string.IsNullOrEmpty(d["Group"].ToString()) ? d["Group"] : "",
                                  Company = !string.IsNullOrEmpty(d["Company"].ToString()) ? d["Company"] : "",
                                  ArrivalDate = !string.IsNullOrEmpty(d["ArrivalDate"].ToString()) ? d["ArrivalDate"] : "",
                                  NoOfNight = !string.IsNullOrEmpty(d["NoOfNight"].ToString()) ? d["NoOfNight"] : "",
                                  DepartureDate = !string.IsNullOrEmpty(d["DepartureDate"].ToString()) ? d["DepartureDate"] : "",
                                  Rate = !string.IsNullOrEmpty(d["Rate"].ToString()) ? d["Rate"] : "",
                                  RoomType = !string.IsNullOrEmpty(d["RoomType"].ToString()) ? d["RoomType"] : "",
                                  NoOfRoom = !string.IsNullOrEmpty(d["NoOfRoom"].ToString()) ? d["NoOfRoom"] : "",
                                  ReservationTypeCode = !string.IsNullOrEmpty(d["ReservationTypeCode"].ToString()) ? d["ReservationTypeCode"] : "",
                                  ReservationBy = !string.IsNullOrEmpty(d["ReservationBy"].ToString()) ? d["ReservationBy"] : "",
                                  CancellationDate = !string.IsNullOrEmpty(d["CancellationDate"].ToString()) ? d["CancellationDate"] : "",
                                  CancelBy = !string.IsNullOrEmpty(d["CancelBy"].ToString()) ? d["CancelBy"] : "",
                                  Description = !string.IsNullOrEmpty(d["Description"].ToString()) ? d["Description"] : ""
                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
            // report.DataSource = dataTable;

            // Không cần gán parameter
            //report.RequestParameters = false;

            //     return PartialView("_ReportViewerPartial", report);
        }

        [HttpGet]
        public IActionResult ReservationStatisticsReport(DateTime fromDate,string zone)
        {
            zone = zone ?? "";
            //XtraReport report = new OneSPMSh.Report.GuestStayReport();
            try
            {
                DataTable dataTable = _iReportService.ReservationStatisticsReport(fromDate, zone);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  MarketCode = !string.IsNullOrEmpty(d["Market Code"].ToString()) ? d["Market Code"] : "",
                                  NoOfRooms = !string.IsNullOrEmpty(d["No. Of Rooms"].ToString()) ? d["No. Of Rooms"] : "",
                                  RoomRevenue = !string.IsNullOrEmpty(d["Room Revenue"].ToString()) ? d["Room Revenue"] : "",
                                  FBRevenue = !string.IsNullOrEmpty(d["F&B Revenue"].ToString()) ? d["F&B Revenue"] : "",
                                  MISCRevenue = !string.IsNullOrEmpty(d["MISC Revenue"].ToString()) ? d["MISC Revenue"] : "",
                                  adr = !string.IsNullOrEmpty(d["ADR"].ToString()) ? d["ADR"] : "",
                                  Occ = !string.IsNullOrEmpty(d["% Occ"].ToString()) ? d["% Occ"] : "",
                                  NoofGuest = !string.IsNullOrEmpty(d["No. of Guest"].ToString()) ? d["No. of Guest"] : "",
                                  MultiOccpc = !string.IsNullOrEmpty(d["% Multi Occ"].ToString()) ? d["% Multi Occ"] : "",
                                  MultiOcc = !string.IsNullOrEmpty(d["Multi Occ"].ToString()) ? d["Multi Occ"] : "",
                                  SingleOcc = !string.IsNullOrEmpty(d["Single Occ"].ToString()) ? d["Single Occ"] : "",
                                
                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
            // report.DataSource = dataTable;

            // Không cần gán parameter
            //report.RequestParameters = false;

            //     return PartialView("_ReportViewerPartial", report);
        }

        [HttpGet]
        public IActionResult ReservationbyCompanyReport(DateTime fromDate, DateTime toDate, string roomClass, string roomType, string zone, int  searchCrip, int  sortOrder,string noOfRoom)
        {
            //XtraReport report = new OneSPMSh.Report.GuestStayReport();
            try
            {
                roomClass = roomClass ?? "";
                roomType = roomType ?? "";
                noOfRoom = noOfRoom ?? "";
                zone = zone ?? "";
                DataTable dataTable = _iReportService.ReservationbyCompanyReport(fromDate, toDate, roomClass, roomType, zone, searchCrip, sortOrder, noOfRoom);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  RoomNo = !string.IsNullOrEmpty(d["RoomNo"].ToString()) ? d["RoomNo"] : "",
                                  Account = !string.IsNullOrEmpty(d["Account"].ToString()) ? d["Account"] : "",
                                  Company = !string.IsNullOrEmpty(d["Company"].ToString()) ? d["Company"] : "",
                                  ArrivalDate = !string.IsNullOrEmpty(d["ArrivalDate"].ToString()) ? d["ArrivalDate"] : "",
                                  Agent = !string.IsNullOrEmpty(d["Agent"].ToString()) ? d["Agent"] : "",
                                  Source = !string.IsNullOrEmpty(d["Source"].ToString()) ? d["Source"] : "",
                                  Group = !string.IsNullOrEmpty(d["Group"].ToString()) ? d["Group"] : "",
                                  DepartureDate = !string.IsNullOrEmpty(d["DepartureDate"].ToString()) ? d["DepartureDate"] : "",
                                  NoOfAdult = !string.IsNullOrEmpty(d["NoOfAdult"].ToString()) ? d["NoOfAdult"] : "",
                                  NoOfChild = !string.IsNullOrEmpty(d["NoOfChild"].ToString()) ? d["NoOfChild"] : "",
                                  NoOfChild1 = !string.IsNullOrEmpty(d["NoOfChild1"].ToString()) ? d["NoOfChild1"] : "",
                                  NoOfRoom = !string.IsNullOrEmpty(d["NoOfRoom"].ToString()) ? d["NoOfRoom"] : "",
                                  NoOfNight = !string.IsNullOrEmpty(d["NoOfNight"].ToString()) ? d["NoOfNight"] : "",

                                  ReservationTypeCode = !string.IsNullOrEmpty(d["ReservationTypeCode"].ToString()) ? d["ReservationTypeCode"] : "",
                                  RoomType = !string.IsNullOrEmpty(d["RoomType"].ToString()) ? d["RoomType"] : "",
                                  MarketCode = !string.IsNullOrEmpty(d["MarketCode"].ToString()) ? d["MarketCode"] : "",
                                  SourceCode = !string.IsNullOrEmpty(d["SourceCode"].ToString()) ? d["SourceCode"] : "",
                                  LoginName = !string.IsNullOrEmpty(d["LoginName"].ToString()) ? d["LoginName"] : "",

                                  CreateDate = !string.IsNullOrEmpty(d["CreateDate"].ToString()) ? d["CreateDate"] : "",
                                  RateCode = !string.IsNullOrEmpty(d["RateCode"].ToString()) ? d["RateCode"] : "",
                                  Rate = !string.IsNullOrEmpty(d["Rate"].ToString()) ? d["Rate"] : "",
                                  Packages = !string.IsNullOrEmpty(d["Packages"].ToString()) ? d["Packages"] : "",
                                  CurrencyID = !string.IsNullOrEmpty(d["CurrencyID"].ToString()) ? d["CurrencyID"] : "",

                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
            // report.DataSource = dataTable;

            // Không cần gán parameter
            //report.RequestParameters = false;

            //     return PartialView("_ReportViewerPartial", report);
        }

        [HttpGet]
        public IActionResult IncurringDepositCollectionData(string fromDate, string toDate, string zone, string cashier)
        {
            try
            {
                DataTable dataTable = _iReportService.IncurringDepositCollectionData(
                    ParseReportDate(fromDate), ParseReportDate(toDate), zone ?? "", cashier ?? "");
                return Json(MapIncurringDepositTxnRows(dataTable));
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        [HttpGet]
        public IActionResult IncurringDepositReturnData(string fromDate, string toDate, string zone, string cashier)
        {
            try
            {
                DataTable dataTable = _iReportService.IncurringDepositReturnData(
                    ParseReportDate(fromDate), ParseReportDate(toDate), zone ?? "", cashier ?? "");
                return Json(MapIncurringDepositTxnRows(dataTable));
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        [HttpGet]
        public IActionResult IncurringDepositPaymentPlanData(string fromDate, string toDate, string zone, string cashier, string notbalance)
        {
            try
            {
                DataTable dataTable = _iReportService.IncurringDepositPaymentPlanData(
                    ParseReportDate(fromDate), ParseReportDate(toDate), zone ?? "", cashier ?? "", notbalance ?? "");
                return Json(MapIncurringDepositBalanceRows(dataTable));
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        [HttpGet]
        public IActionResult IncurringDepositSummaryData(string fromDate, string toDate, string zone, string cashier, string type)
        {
            try
            {
                DataTable dataTable = _iReportService.IncurringDepositSummaryData(
                    ParseReportDate(fromDate), ParseReportDate(toDate), zone ?? "", cashier ?? "",
                    string.IsNullOrWhiteSpace(type) ? "0" : type);
                return Json(MapIncurringDepositBalanceRows(dataTable));
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        private static List<Dictionary<string, object>> MapIncurringDepositTxnRows(DataTable table)
        {
            if (table == null || table.Rows.Count == 0)
                return new List<Dictionary<string, object>>();
            var result = new List<Dictionary<string, object>>(table.Rows.Count);
            var depCol = FirstExisting(table.Rows[0], "DepositAmount", "Deposit");
            var retCol = FirstExisting(table.Rows[0], "ReturnAmount", "Return");
            foreach (DataRow d in table.Rows)
            {
                result.Add(new Dictionary<string, object>(12)
                {
                    ["lastName"] = ColStr(d, "LastName"),
                    ["roomNo"] = ColStr(d, "RoomNo"),
                    ["invoiceNo"] = FirstCol(d, "InvoiceNo", "FolioNo"),
                    ["arrivalDate"] = FirstCol(d, "ArrivalDate"),
                    ["departureDate"] = FirstCol(d, "DepartureDate"),
                    ["noOfNight"] = ColDec(d, "NoOfNight"),
                    ["currencyID"] = ColStr(d, "CurrencyID"),
                    ["paymentMethod"] = ColStr(d, "PaymentMethod"),
                    ["transactionDate"] = FirstCol(d, "TransactionDate"),
                    ["depositAmount"] = ColDec(d, depCol),
                    ["returnAmount"] = ColDec(d, retCol),
                    ["confirmationNo"] = ColStr(d, "ConfirmationNo"),
                    ["userName"] = ColStr(d, "UserName"),
                    ["profitCenterCode"] = ColStr(d, "ProfitCenterCode")
                });
            }
            return result;
        }

        private static List<Dictionary<string, object>> MapIncurringDepositBalanceRows(DataTable table)
        {
            var result = new List<Dictionary<string, object>>();
            if (table == null) return result;
            foreach (DataRow d in table.Rows)
            {
                var deposit = ColDec(d, FirstExisting(d, "Deposit", "DepositAmount"));
                var returned = ColDec(d, FirstExisting(d, "Return", "ReturnAmount"));
                var balance = d.Table.Columns.Contains("Balance")
                    ? ColDec(d, "Balance")
                    : deposit - returned;
                result.Add(new Dictionary<string, object>
                {
                    ["lastName"] = ColStr(d, "LastName"),
                    ["roomNo"] = ColStr(d, "RoomNo"),
                    ["confirmationNo"] = ColStr(d, "ConfirmationNo"),
                    ["folioNo"] = FirstCol(d, "FolioNo", "InvoiceNo"),
                    ["invoiceNo"] = FirstCol(d, "InvoiceNo", "FolioNo"),
                    ["arrivalDate"] = FirstCol(d, "ArrivalDate"),
                    ["departureDate"] = FirstCol(d, "DepartureDate"),
                    ["noOfNight"] = ColDec(d, "NoOfNight"),
                    ["currencyID"] = ColStr(d, "CurrencyID"),
                    ["paymentMethod"] = ColStr(d, "PaymentMethod"),
                    ["transactionDate"] = FirstCol(d, "TransactionDate"),
                    ["deposit"] = deposit,
                    ["return"] = returned,
                    ["balance"] = balance,
                    ["depositAmount"] = deposit,
                    ["returnAmount"] = returned,
                    ["userName"] = ColStr(d, "UserName"),
                    ["profitCenterCode"] = ColStr(d, "ProfitCenterCode")
                });
            }
            return result;
        }

        [HttpGet]
        public IActionResult DepositActivityData(string DueDate_FromDate, string DueDate_ToDate, string Arrival_FromDate, string Arrival_ToDate, string Post_FromDate, string Post_ToDate, string Cashier, int DepositOption, int RsvStatus, int Sort)
        {
            //XtraReport report = new OneSPMSh.Report.GuestStayReport();
            try
            {
                DueDate_FromDate = DueDate_FromDate ?? "";
                DueDate_ToDate = DueDate_ToDate ?? "";
                Arrival_FromDate = Arrival_FromDate ?? "";
                Arrival_ToDate = Arrival_ToDate ?? "";
                Post_FromDate = Post_FromDate ?? "";
                Post_ToDate = Post_ToDate ?? "";
                Cashier = Cashier ?? "";
                DataTable dataTable = _iReportService.DepositActivityData(DueDate_FromDate, DueDate_ToDate, Arrival_FromDate, Arrival_ToDate, Post_FromDate, Post_ToDate, Cashier, DepositOption, RsvStatus, Sort);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  ConfirmationNo = d["ConfirmationNo"]?.ToString() ?? "",
                                  RsvID = d["RsvID"]?.ToString() ?? "",
                                  RsqID = d["RsqID"]?.ToString() ?? "",
                                  RoomNo = d["RoomNo"]?.ToString() ?? "",
                                  LastName = d["LastName"]?.ToString() ?? "",
                                  Company = d["Company"]?.ToString() ?? "",
                                  RateCode = d["RateCode"]?.ToString() ?? "",
                                  Rate = d["Rate"]?.ToString() ?? "",
                                  RoomType = d["RoomType"]?.ToString() ?? "",
                                  ArrivalDate = d["ArrivalDate"]?.ToString() ?? "",
                                  DepartureDate = d["DepartureDate"]?.ToString() ?? "",
                                  Status = d["Status"]?.ToString() ?? "",
                                  DepositComment = d["DepositComment"]?.ToString() ?? "",
                                  Rsq = d["Rsq"]?.ToString() ?? "",
                                  PaidAmount = d["PaidAmount"]?.ToString() ?? "",
                                  DueAmount = d["DueAmount"]?.ToString() ?? "",
                                  CurrencyID = d["CurrencyID"]?.ToString() ?? "",
                                  Type = d["Type"]?.ToString() ?? "", // 'aRequest'
                                  PostDate = d["PostDate"]?.ToString() ?? "", // '1/1/2011'
                                  DueDate = d["DueDate"]?.ToString() ?? ""
                              }).ToList();


                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
            // report.DataSource = dataTable;

            // Không cần gán parameter
            //report.RequestParameters = false;

            //     return PartialView("_ReportViewerPartial", report);
        }

        [HttpGet]
        public IActionResult NoShowReportData(string fromDate, string toDate, int roomClass, string zone)
        {
            zone = zone ?? "";
            try
            {
                DataTable dataTable = _iReportService.NoShowReportData(ParseReportDate(fromDate), ParseReportDate(toDate), roomClass, zone);
                if (dataTable == null) return Json(Array.Empty<object>());
                var result = new List<Dictionary<string, object>>(dataTable.Rows.Count);
                foreach (DataRow d in dataTable.Rows)
                {
                    var last = ColStr(d, "LastName");
                    var first = FirstCol(d, "FirstName", "First Name");
                    var guest = string.IsNullOrWhiteSpace(first) ? last
                        : (string.IsNullOrWhiteSpace(last) ? first : last + ", " + first);
                    var company = ColStr(d, "Company");
                    var agent = ColStr(d, "Agent");
                    var source = ColStr(d, "Source");
                    var companySrc = company;
                    if (!string.IsNullOrWhiteSpace(agent) && !string.Equals(agent, company, StringComparison.OrdinalIgnoreCase))
                        companySrc = string.IsNullOrWhiteSpace(companySrc) ? agent : companySrc + " / " + agent;
                    if (!string.IsNullOrWhiteSpace(source) && !string.Equals(source, company, StringComparison.OrdinalIgnoreCase) && !string.Equals(source, agent, StringComparison.OrdinalIgnoreCase))
                        companySrc = string.IsNullOrWhiteSpace(companySrc) ? source : companySrc + " / " + source;
                    result.Add(new Dictionary<string, object>
                    {
                        ["confirmationNo"] = ColStr(d, "ConfirmationNo"),
                        ["roomNo"] = ColStr(d, "RoomNo"),
                        ["guestName"] = guest,
                        ["companySource"] = companySrc,
                        ["arrivalDate"] = FirstCol(d, "ArrivalDate"),
                        ["departureDate"] = FirstCol(d, "DepartureDate"),
                        ["roomType"] = ColStr(d, "RoomType"),
                        ["businessBlockCode"] = ColStr(d, "BusinessBlockCode"),
                        ["marketCode"] = ColStr(d, "MarketCode"),
                        ["paymentMethod"] = ColStr(d, "PaymentMethod"),
                        ["reservationTypeCode"] = ColStr(d, "ReservationTypeCode"),
                        ["creditCardNo"] = ColStr(d, "CreditCardNo"),
                        ["rateCode"] = ColStr(d, "RateCode"),
                        ["expirationDate"] = FirstCol(d, "ExpirationDate"),
                        ["rateAmount"] = ColDec(d, "RateAmount"),
                        ["noOfRoom"] = ColDec(d, "NoOfRoom"),
                        ["noOfNight"] = ColDec(d, "NoOfNight"),
                        ["totalAmount"] = ColDec(d, "TotalAmount"),
                        ["depositPaid"] = ColDec(d, "DepositPaid"),
                    });
                }
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new Dictionary<string, object> { ["error"] = ex.Message });
            }
            // report.DataSource = dataTable;

            // Không cần gán parameter
            //report.RequestParameters = false;

            //     return PartialView("_ReportViewerPartial", report);
        }


        [HttpGet]
        public IActionResult ReservationPreblockedyData(DateTime fromDate, DateTime toDate, string roomClass, string roomtype,string pseudo,string chkviponly,string individual,string blockcode,string preblocked)
        {
            roomClass = roomClass ?? "";
            roomtype = roomtype ?? "";
            pseudo = pseudo ?? "";
            chkviponly = chkviponly ?? "";
            individual = individual ?? "";
            blockcode = blockcode ?? "";

            preblocked = preblocked ?? "";
            //XtraReport report = new OneSPMSh.Report.GuestStayReport();
            try
            {
                DataTable dataTable = _iReportService.ReservationPreblockedyData(fromDate, toDate, roomClass, roomtype, pseudo, chkviponly, individual, blockcode, preblocked);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  RoomNo = !string.IsNullOrEmpty(d["RoomNo"].ToString()) ? d["RoomNo"] : "",
                                  Name = !string.IsNullOrEmpty(d["Name"].ToString()) ? d["Name"] : "",
                                  VIP = !string.IsNullOrEmpty(d["VIP"].ToString()) ? d["VIP"] : "",
                                  ArrivalDate = !string.IsNullOrEmpty(d["ArrivalDate"].ToString()) ? d["ArrivalDate"] : "",
                                  DepartureDate = !string.IsNullOrEmpty(d["DepartureDate"].ToString()) ? d["DepartureDate"] : "",

                                  ETA = !string.IsNullOrEmpty(d["ETA"].ToString()) ? d["ETA"] : "",
                                  ETD = !string.IsNullOrEmpty(d["ETD"].ToString()) ? d["ETD"] : "",

                                  NoOfAdult = !string.IsNullOrEmpty(d["NoOfAdult"].ToString()) ? d["NoOfAdult"] : "",
                                  NoOfChild = !string.IsNullOrEmpty(d["NoOfChild"].ToString()) ? d["NoOfChild"] : "",
                                  NoOfChild1 = !string.IsNullOrEmpty(d["NoOfChild1"].ToString()) ? d["NoOfChild1"] : "",
                                  NoOfRoom = !string.IsNullOrEmpty(d["NoOfRoom"].ToString()) ? d["NoOfRoom"] : "",
                                  NoOfNight = !string.IsNullOrEmpty(d["NoOfNight"].ToString()) ? d["NoOfNight"] : "",
                                  RoomType = !string.IsNullOrEmpty(d["RoomType"].ToString()) ? d["RoomType"] : "",


                                  HKStatusID = !string.IsNullOrEmpty(d["HKStatusID"].ToString()) ? d["HKStatusID"] : "",
                                  FOStatus = !string.IsNullOrEmpty(d["FOStatus"].ToString()) ? d["FOStatus"] : "",

                                  Rate = !string.IsNullOrEmpty(d["Rate"].ToString()) ? d["Rate"] : "",
                                  RateCode = !string.IsNullOrEmpty(d["RateCode"].ToString()) ? d["RateCode"] : "",
                                  CurrencyID = !string.IsNullOrEmpty(d["CurrencyID"].ToString()) ? d["CurrencyID"] : "",
                                  DepositReceivedMaster = !string.IsNullOrEmpty(d["DepositReceivedMaster"].ToString()) ? d["DepositReceivedMaster"] : "",
                                  DepositRsqMaster = !string.IsNullOrEmpty(d["DepositRsqMaster"].ToString()) ? d["DepositRsqMaster"] : "",
                                  ResType = !string.IsNullOrEmpty(d["ResType"].ToString()) ? d["ResType"] : "",

                                  Company = !string.IsNullOrEmpty(d["Company"].ToString()) ? d["Company"] : "",
                                  Agent = !string.IsNullOrEmpty(d["Agent"].ToString()) ? d["Agent"] : "",
                                  Source = !string.IsNullOrEmpty(d["Source"].ToString()) ? d["Source"] : "",
                                  BusinessBlockCode = !string.IsNullOrEmpty(d["BusinessBlockCode"].ToString()) ? d["BusinessBlockCode"] : "",
                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
            // report.DataSource = dataTable;

            // Không cần gán parameter
            //report.RequestParameters = false;

            //     return PartialView("_ReportViewerPartial", report);
        }

        [HttpGet]
        public IActionResult ReservationSummaryData(string fromDate, string toDate, string roomType, string zone, string viewBy, string market)
        {
            try
            {
                zone = (zone ?? "").Trim();
                roomType = (roomType ?? "").Trim();
                market = (market ?? "").Trim();
                if (string.IsNullOrWhiteSpace(viewBy) || viewBy == "0") viewBy = "VND";
                if (string.IsNullOrWhiteSpace(zone))
                    zone = JoinCsvCodes(
                        PropertyUtils.ConvertToList<ZoneModel>(ZoneBO.Instance.FindAll())
                            .Where(z => z != null && !z.Inactive && !string.IsNullOrWhiteSpace(z.Code))
                            .Select(z => z.Code), 30);
                if (string.IsNullOrWhiteSpace(roomType))
                    roomType = JoinCsvCodes(
                        PropertyUtils.ConvertToList<RoomTypeModel>(RoomTypeBO.Instance.FindAll())
                            .Where(r => r != null && r.InActive == 0 && !string.IsNullOrWhiteSpace(r.Code))
                            .Select(r => r.Code), 8000);
                if (string.IsNullOrWhiteSpace(market))
                    market = JoinCsvCodes(
                        PropertyUtils.ConvertToList<MarketModel>(MarketBO.Instance.FindAll())
                            .Where(m => m != null && !m.Inactive && !string.IsNullOrWhiteSpace(m.Code))
                            .Select(m => m.Code), 8000);

                DataTable dataTable = _iReportService.ReservationSummaryData(
                    ParseReportDate(fromDate), ParseReportDate(toDate), roomType, zone, viewBy, market);
                if (dataTable == null) return Json(Array.Empty<object>());

                var result = new List<Dictionary<string, object>>(dataTable.Rows.Count);
                foreach (DataRow d in dataTable.Rows)
                {
                    result.Add(new Dictionary<string, object>
                    {
                        ["rsvSummaryDate"] = d.Table.Columns.Contains("RsvSummaryDate") ? d["RsvSummaryDate"] : "",
                        ["weekDate"] = ColStr(d, "WeekDate"),
                        ["totalOccRms"] = ColDec(d, "TotalOccRms"),
                        ["indRms"] = ColDec(d, "IndRms"),
                        ["blkRmsPU"] = ColDec(d, "BlkRmsPU"),
                        ["blkRmsNotPU"] = ColDec(d, "BlkRmsNotPU"),
                        ["totalAvailable"] = ColDec(d, "TotalAvailable"),
                        ["occPer"] = ColDec(d, "OccPer"),
                        ["roomRevenue"] = ColDec(d, "RoomRevenue"),
                        ["grpRevenue"] = ColDec(d, "GrpRevenue"),
                        ["indRevenue"] = ColDec(d, "IndRevenue"),
                        ["avrRate"] = ColDec(d, "AvrRate"),
                        ["arrRms"] = ColDec(d, "ArrRms"),
                        ["blkArrRms"] = ColDec(d, "BlkArrRms"),
                        ["charrRms"] = ColDec(d, "CHArrRms"),
                        ["depRms"] = ColDec(d, "DepRms"),
                        ["blkDepRms"] = ColDec(d, "BlkDepRms"),
                        ["chdepRms"] = ColDec(d, "CHDepRms"),
                        ["oOORms"] = ColDec(d, "OOORms"),
                        ["adl"] = ColDec(d, "Adl"),
                        ["chl"] = ColDec(d, "Chl"),
                        ["chl1"] = ColDec(d, "Chl1"),
                        ["chl2"] = ColDec(d, "Chl2"),
                        ["totPer"] = ColDec(d, "TotPer"),
                        ["arrPrs"] = ColDec(d, "ArrPrs"),
                        ["depPrs"] = ColDec(d, "DepPrs"),
                        ["monthDisplay"] = ColStr(d, "MonthDisplay"),
                        ["monthGroup"] = ColStr(d, "MonthGroup"),
                    });
                }
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new Dictionary<string, object> { ["error"] = ex.Message });
            }
            // report.DataSource = dataTable;

            // Không cần gán parameter
            //report.RequestParameters = false;

            //     return PartialView("_ReportViewerPartial", report);
        }


        [HttpGet]
        public IActionResult ProductActivityData(DateTime fromDate, DateTime toDate, string type, string currency)
        {
            try
            {
                List<object> finalResult = new List<object>();

                // Tách chuỗi "1,2,0,3" thành mảng
                var typeList = type.Split(',', StringSplitOptions.RemoveEmptyEntries);

                foreach (var t in typeList)
                {
                    DataTable dataTable = _iReportService.ProductActivityData(fromDate, toDate, t.Trim(), currency);

                    var result = (from d in dataTable.AsEnumerable()
                                  select new
                                  {
                                      ProfileID = d["ProfileID"]?.ToString() ?? "",
                                      GuestNo = d["GuestNo"]?.ToString() ?? "",
                                      GuestName = d["GuestName"]?.ToString() ?? "",
                                      City = d["City"]?.ToString() ?? "",
                                      CurrencyID = d["CurrencyID"]?.ToString() ?? "",
                                      TotalTurnover = d["TotalTurnover"]?.ToString() ?? "",
                                      RoomPackage = d["RoomPackage"]?.ToString() ?? "",
                                      RoomSales = d["RoomSales"]?.ToString() ?? "",
                                      FBTurnover = d["FBTurnover"]?.ToString() ?? "",
                                      EventTurnover = d["EventTurnover"]?.ToString() ?? "",
                                      MicelliousTurnover = d["MicelliousTurnover"]?.ToString() ?? "",
                                      TaxService = d["TaxService"]?.ToString() ?? "",
                                      TotalAdult = d["TotalAdult"]?.ToString() ?? "",
                                      TotalChild = d["TotalChild"]?.ToString() ?? "",
                                      TotalChild1 = d["TotalChild1"]?.ToString() ?? "",
                                      TotalChild2 = d["TotalChild2"]?.ToString() ?? "",
                                      BedNightAC = d["BedNightAC"]?.ToString() ?? "",
                                      BedNightAll = d["BedNightAll"]?.ToString() ?? "",
                                      RoomNight = d["RoomNight"]?.ToString() ?? "",
                                      AmountAvg = d["AmountAvg"]?.ToString() ?? "",
                                  }).ToList();

                    finalResult.AddRange(result);
                }

                return Json(finalResult);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        [HttpGet]
        public IActionResult SalesinChargeActivityData(DateTime fromDate, DateTime toDate, string type, string currency)
        {
            try
            {
                List<object> finalResult = new List<object>();

                // Tách chuỗi "1,2,0,3" thành mảng
                var typeList = type.Split(',', StringSplitOptions.RemoveEmptyEntries);

                foreach (var t in typeList)
                {
                    DataTable dataTable = _iReportService.SalesinChargeActivityData(fromDate, toDate, t.Trim(), currency);

                    var result = (from d in dataTable.AsEnumerable()
                                  select new
                                  {
                                      ProfileID = d["ProfileID"]?.ToString() ?? "",
                                      GuestNo = d["GuestNo"]?.ToString() ?? "",
                                      GuestName = d["GuestName"]?.ToString() ?? "",
                                      City = d["City"]?.ToString() ?? "",
                                      CurrencyID = d["CurrencyID"]?.ToString() ?? "",
                                      SaleInCharge = d["SaleInCharge"]?.ToString() ?? "",
                                      TotalTurnover = d["TotalTurnover"]?.ToString() ?? "",
                                      RoomSales = d["RoomSales"]?.ToString() ?? "",
                                      TaxService = d["TaxService"]?.ToString() ?? "",
                                      TotalAdult = d["TotalAdult"]?.ToString() ?? "",
                                      TotalChild = d["TotalChild"]?.ToString() ?? "",
                                      TotalChild1 = d["TotalChild1"]?.ToString() ?? "",
                                      TotalChild2 = d["TotalChild2"]?.ToString() ?? "",
                                      BedNightAC = d["BedNightAC"]?.ToString() ?? "",
                                      BedNightAll = d["BedNightAll"]?.ToString() ?? "",
                                      RoomNight = d["RoomNight"]?.ToString() ?? "",
                                      AmountAvg = d["AmountAvg"]?.ToString() ?? "",
                                  }).ToList();

                    finalResult.AddRange(result);
                }

                return Json(finalResult);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        [HttpGet]
        public IActionResult TAProductionReportData(DateTime fromDate, DateTime toDate, string type, string currency)
        {
            try
            {
                List<object> finalResult = new List<object>();

                // Tách chuỗi "1,2,0,3" thành mảng
                var typeList = type.Split(',', StringSplitOptions.RemoveEmptyEntries);

                foreach (var t in typeList)
                {
                    DataTable dataTable = _iReportService.TAProductionReportData(fromDate, toDate, t.Trim(), currency);

                    var result = (from d in dataTable.AsEnumerable()
                                  select new
                                  {
                                      ProfileID = d["ProfileID"]?.ToString() ?? "",
                                      GuestNo = d["GuestNo"]?.ToString() ?? "",
                                      GuestName = d["GuestName"]?.ToString() ?? "",
                                      City = d["City"]?.ToString() ?? "",
                                      CurrencyID = d["CurrencyID"]?.ToString() ?? "",
                                      TotalTurnover = d["TotalTurnover"]?.ToString() ?? "",
                                      RoomPackage = d["RoomPackage"]?.ToString() ?? "",
                                      RoomSales = d["RoomSales"]?.ToString() ?? "",
                                      FBTurnover = d["FBTurnover"]?.ToString() ?? "",
                                      EventTurnover = d["EventTurnover"]?.ToString() ?? "",
                                      MicelliousTurnover = d["MicelliousTurnover"]?.ToString() ?? "",
                                      TaxService = d["TaxService"]?.ToString() ?? "",
                                      TotalAdult = d["TotalAdult"]?.ToString() ?? "",
                                      TotalChild = d["TotalChild"]?.ToString() ?? "",
                                      TotalChild1 = d["TotalChild1"]?.ToString() ?? "",
                                      TotalChild2 = d["TotalChild2"]?.ToString() ?? "",
                                      BedNightAC = d["BedNightAC"]?.ToString() ?? "",
                                      BedNightAll = d["BedNightAll"]?.ToString() ?? "",
                                      RoomNight = d["RoomNight"]?.ToString() ?? "",
                                      AmountAvg = d["AmountAvg"]?.ToString() ?? "",
                                      RsvPersonInCharge = d["RsvPersonInCharge"]?.ToString() ?? "",
                                  }).ToList();

                    finalResult.AddRange(result);
                }

                return Json(finalResult);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }


        [HttpGet]
        public IActionResult AlertsData(DateTime fromDate, DateTime toDate, string viewBy, string altercode)
        {
            try
            {

                    DataTable dataTable = _iReportService.AlertsData(fromDate, toDate, viewBy, altercode);

                    var result = (from d in dataTable.AsEnumerable()
                                  select new
                                  {
                                      ConfirmationNo = d["ConfirmationNo"]?.ToString() ?? "",
                                      RoomNo = d["RoomNo"]?.ToString() ?? "",
                                      GuestName = d["GuestName"]?.ToString() ?? "",
                                      ArrivalDate = d["ArrivalDate"]?.ToString() ?? "",
                                      DepartureDate = d["DepartureDate"]?.ToString() ?? "",
                                      RoomType = d["RoomType"]?.ToString() ?? "",
                                      AlertArea = d["AlertArea"]?.ToString() ?? "",
                                      AlertCode = d["AlertCode"]?.ToString() ?? "",
                                      AlertDescription = d["AlertDescription"]?.ToString() ?? "",
                                      Status = d["Status"]?.ToString() ?? "",
                                   
                                  }).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        [HttpGet]
        public IActionResult TransportationData(DateTime fromDate, DateTime toDate, string transportType, int viewBy,int reservationStatus, int sortByGuestName, int sortByRoom, int sortByTime, int sortByVIP)
        {
            try
            {

                DataTable dataTable = _iReportService.TransportationData(fromDate, toDate, transportType, viewBy, reservationStatus, sortByGuestName, sortByRoom, sortByTime, sortByVIP);

                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  ConfirmationNo = d["ConfirmationNo"]?.ToString() ?? "",
                                  DateTime = d["DateTime"]?.ToString() ?? "",
                                  GuestName = d["Guest Name"]?.ToString() ?? "",
                                  StationCode = d["Station Code"]?.ToString() ?? "",
                                  CarrierCode = d["Carrier Code"]?.ToString() ?? "",
                                  TransportCode = d["Transport Number"]?.ToString() ?? "",
                                  Adults = d["Adults"]?.ToString() ?? "",
                                  Children = d["Children"]?.ToString() ?? "",
                                  Children1 = d["Children 1"]?.ToString() ?? "",
                                  Children2 = d["Children 2"]?.ToString() ?? "",
                                  ResvStatus = d["Resv Status"]?.ToString() ?? "",

                                  TransportDate = d["TransportDate"]?.ToString() ?? "",
                                  Room = d["Room"]?.ToString() ?? "",
                                  vip = d["VIP"]?.ToString() ?? "",
                                  Description = d["Description"]?.ToString() ?? "",
                                  Type = d["Type"]?.ToString() ?? "",
                                  TransportName = d["TransportName"]?.ToString() ?? "",
                              }).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        [HttpGet]
        public IActionResult NationalStatisticsData(DateTime fromDate, DateTime toDate, int zone, string viewBy)
        {
            try
            {
                // zone = 0: mọi khu. Ngược lại chỉ lấy loại phòng thuộc zone đã chọn.
                List<RoomTypeModel> roomTypesInZone = PropertyUtils
                    .ConvertToList<RoomTypeModel>(RoomTypeBO.Instance.FindAll());
                if (zone > 0)
                    roomTypesInZone = roomTypesInZone.Where(rt => rt.ZoneID == zone).ToList();

                // Lấy mã khu vực (zone)
                string zonecode = string.Join(",", roomTypesInZone.Select(rt => rt.ID.ToString()));

                // Lấy dữ liệu báo cáo
                DataTable dataTable = _iReportService.NationalStatisticsData(fromDate, toDate, zonecode, viewBy);

                // Tính tổng giá trị Sum1 trong tất cả các bản ghi
                decimal totalSum1 = 0;
                var dataList = (from d in dataTable.AsEnumerable()
                                let sum1Value = !string.IsNullOrEmpty(d["Sum1"].ToString()) ? Convert.ToDecimal(d["Sum1"]) : 0
                                select new
                                {
                                    National = !string.IsNullOrEmpty(d["National"].ToString()) ? d["National"] : "",
                                    MonthArrival = !string.IsNullOrEmpty(d["MonthArrival"].ToString()) ? d["MonthArrival"] : "",
                                    YearArrival = !string.IsNullOrEmpty(d["YearArrival"].ToString()) ? d["YearArrival"] : "",
                                    Sum1 = sum1Value,
                                    Zone1 = d["1"]?.ToString() ?? "",
                                    Zone2 = d["2"]?.ToString() ?? "",
                                    Zone3 = d["3"]?.ToString() ?? "",
                                    Zone4 = d["4"]?.ToString() ?? "",
                                    Zone5 = d["5"]?.ToString() ?? "",
                                    Zone6 = d["6"]?.ToString() ?? "",
                                    Zone7 = d["7"]?.ToString() ?? "",
                                    Zone8 = d["8"]?.ToString() ?? "",
                                    Zone9 = d["9"]?.ToString() ?? "",
                                    Zone10 = d["10"]?.ToString() ?? "",
                                    Zone11 = d["11"]?.ToString() ?? "",
                                    Zone12 = d["12"]?.ToString() ?? "",
                                    Zone13 = d["13"]?.ToString() ?? "",
                                    Zone14 = d["14"]?.ToString() ?? "",
                                    Zone15 = d["15"]?.ToString() ?? "",
                                    Zone16 = d["16"]?.ToString() ?? "",
                                    Zone17 = d["17"]?.ToString() ?? "",
                                    Zone18 = d["18"]?.ToString() ?? "",
                                    Zone19 = d["19"]?.ToString() ?? "",
                                    Zone20 = d["20"]?.ToString() ?? "",
                                    Zone21 = d["21"]?.ToString() ?? "",
                                    Zone22 = d["22"]?.ToString() ?? "",
                                    Zone23 = d["23"]?.ToString() ?? "",
                                    Zone24 = d["24"]?.ToString() ?? "",
                                    Zone25 = d["25"]?.ToString() ?? "",
                                    Zone26 = d["26"]?.ToString() ?? "",
                                    Zone27 = d["27"]?.ToString() ?? "",
                                    Zone28 = d["28"]?.ToString() ?? "",
                                    Zone29 = d["29"]?.ToString() ?? "",
                                    Zone30 = d["30"]?.ToString() ?? "",
                                    Zone31 = d["31"]?.ToString() ?? "",
                                }).ToList();

                // Tính tổng Sum1
                totalSum1 = dataList.Sum(x => x.Sum1);

                // Thêm cột Percentage cho tất cả các bản ghi
                var result = dataList.Select(item => new
                {
                    item.National,
                    item.MonthArrival,
                    item.YearArrival,
                    item.Sum1,
                    Percentage = totalSum1 > 0 ? (item.Sum1 / totalSum1) * 100 : 0, // Tính tỷ lệ phần trăm cho tất cả
                    item.Zone1,
                    item.Zone2,
                    item.Zone3,
                    item.Zone4,
                    item.Zone5,
                    item.Zone6,
                    item.Zone7,
                    item.Zone8,
                    item.Zone9,
                    item.Zone10,
                    item.Zone11,
                    item.Zone12,
                    item.Zone13,
                    item.Zone14,
                    item.Zone15,
                    item.Zone16,
                    item.Zone17,
                    item.Zone18,
                    item.Zone19,
                    item.Zone20,
                    item.Zone21,
                    item.Zone22,
                    item.Zone23,
                    item.Zone24,
                    item.Zone25,
                    item.Zone26,
                    item.Zone27,
                    item.Zone28,
                    item.Zone29,
                    item.Zone30,
                    item.Zone31
                }).ToList();

                // Trả về kết quả dạng JSON
                return Json(result);
            }
            catch (Exception ex)
            {
                // Trả về thông báo lỗi nếu có lỗi
                return Json(ex.Message);
            }
        }

        [HttpGet]
        public IActionResult SalesinChargeReportsForm1Data(DateTime fromDate, string viewBy)
        {
            try
            {

                    DataTable dataTable = _iReportService.SalesinChargeReportsForm1Data(fromDate , viewBy);

                    var result = (from d in dataTable.AsEnumerable()
                                  select new
                                  {
                                      SaleInCharge = d["SaleInCharge"]?.ToString() ?? "",
                                      SaleInChargeGroup = d["SaleInChargeGroup"]?.ToString() ?? "",
                                      Zone = d["Zone"]?.ToString() ?? "",
                                      T1 = d["T1"]?.ToString() ?? "",
                                      T2 = d["T2"]?.ToString() ?? "",
                                      T3 = d["T3"]?.ToString() ?? "",
                                      T4 = d["T4"]?.ToString() ?? "",
                                      T5 = d["T5"]?.ToString() ?? "",
                                      T6 = d["T6"]?.ToString() ?? "",
                                      T7 = d["T7"]?.ToString() ?? "",
                                      T8 = d["T8"]?.ToString() ?? "",
                                      T9 = d["T9"]?.ToString() ?? "",
                                      T10 = d["T10"]?.ToString() ?? "",
                                      T11 = d["T11"]?.ToString() ?? "",
                                      T12 = d["T12"]?.ToString() ?? "",
                                      Q1 = d["Q1"]?.ToString() ?? "",
                                      Q2 = d["Q2"]?.ToString() ?? "",
                                      Q3 = d["Q3"]?.ToString() ?? "",
                                      Q4 = d["Q4"]?.ToString() ?? "",
                                      Total = d["Total"]?.ToString() ?? ""

                                  }).ToList();


                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        [HttpGet]
        public IActionResult RevenueDetailData(DateTime fromDate, DateTime toDate)
        {
            //XtraReport report = new OneSPMSh.Report.GuestStayReport();
            try
            {
                DataTable dataTable = _iReportService.RevenueDetailData(fromDate, toDate);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  Seq = !string.IsNullOrEmpty(d["Seq"].ToString()) ? d["Seq"] : "",
                                  ProfitCode = !string.IsNullOrEmpty(d["ProfitCode"].ToString()) ? d["ProfitCode"] : "",
                                  GroupCode = !string.IsNullOrEmpty(d["GroupCode"].ToString()) ? d["GroupCode"] : "",
                                  GroupDescription = !string.IsNullOrEmpty(d["GroupDescription"].ToString()) ? d["GroupDescription"] : "",
                                  SubgroupCode = !string.IsNullOrEmpty(d["SubgroupCode"].ToString()) ? d["SubgroupCode"] : "",
                                  Amount = !string.IsNullOrEmpty(d["Amount"].ToString()) ? d["Amount"] : "",
                                  CurrencyID = !string.IsNullOrEmpty(d["CurrencyID"].ToString()) ? d["CurrencyID"] : "",
                                  AmountMater = !string.IsNullOrEmpty(d["AmountMater"].ToString()) ? d["AmountMater"] : "",
                                  CurrencyMaster = !string.IsNullOrEmpty(d["CurrencyMaster"].ToString()) ? d["CurrencyMaster"] : "",
                                  TotalUSD = !string.IsNullOrEmpty(d["TotalUSD"].ToString()) ? d["TotalUSD"] : "",
                                  TotalSVCUSD = !string.IsNullOrEmpty(d["TotalSVCUSD"].ToString()) ? d["TotalSVCUSD"] : "",
                                  TotalVATUSD = !string.IsNullOrEmpty(d["TotalVATUSD"].ToString()) ? d["TotalVATUSD"] : "",
                                  Code = !string.IsNullOrEmpty(d["Code"].ToString()) ? d["Code"] : "",
                                  Description = !string.IsNullOrEmpty(d["Description"].ToString()) ? d["Description"] : "",
                                  SubGroupDescription = !string.IsNullOrEmpty(d["SubGroupDescription"].ToString()) ? d["SubGroupDescription"] : "",

                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
            // report.DataSource = dataTable;

            // Không cần gán parameter
            //report.RequestParameters = false;

            //     return PartialView("_ReportViewerPartial", report);
        }

        [HttpGet]
        public IActionResult RevenueSummaryData(DateTime fromDate, DateTime toDate)
        {
            //XtraReport report = new OneSPMSh.Report.GuestStayReport();
            try
            {
                DataTable dataTable = _iReportService.RevenueSummaryData(fromDate, toDate);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  Seq = !string.IsNullOrEmpty(d["Seq"].ToString()) ? d["Seq"] : "",
                                  ProfitCode = !string.IsNullOrEmpty(d["ProfitCode"].ToString()) ? d["ProfitCode"] : "",
                                  GroupCode = !string.IsNullOrEmpty(d["GroupCode"].ToString()) ? d["GroupCode"] : "",
                                  GroupDescription = !string.IsNullOrEmpty(d["GroupDescription"].ToString()) ? d["GroupDescription"] : "",
                                  SubgroupCode = !string.IsNullOrEmpty(d["SubgroupCode"].ToString()) ? d["SubgroupCode"] : "",
                                  Amount = !string.IsNullOrEmpty(d["Amount"].ToString()) ? d["Amount"] : "",
                                  CurrencyID = !string.IsNullOrEmpty(d["CurrencyID"].ToString()) ? d["CurrencyID"] : "",
                                  AmountMater = !string.IsNullOrEmpty(d["AmountMater"].ToString()) ? d["AmountMater"] : "",
                                  CurrencyMaster = !string.IsNullOrEmpty(d["CurrencyMaster"].ToString()) ? d["CurrencyMaster"] : "",
                                  TotalUSD = !string.IsNullOrEmpty(d["TotalUSD"].ToString()) ? d["TotalUSD"] : "",
                                  TotalVND = !string.IsNullOrEmpty(d["TotalVND"].ToString()) ? d["TotalVND"] : "",

                                  Code = !string.IsNullOrEmpty(d["Code"].ToString()) ? d["Code"] : "",
                                  Description = !string.IsNullOrEmpty(d["Description"].ToString()) ? d["Description"] : "",
                                  SubGroupDescription = !string.IsNullOrEmpty(d["SubGroupDescription"].ToString()) ? d["SubGroupDescription"] : "",

                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
            // report.DataSource = dataTable;

            // Không cần gán parameter
            //report.RequestParameters = false;

            //     return PartialView("_ReportViewerPartial", report);
        }
        [HttpGet]
        public IActionResult RoomMovesData(DateTime fromDate, DateTime toDate)
        {
            //XtraReport report = new OneSPMSh.Report.GuestStayReport();
            try
            {
                DataTable dataTable = _iReportService.RoomMovesData(fromDate, toDate);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  Name = !string.IsNullOrEmpty(d["Name"].ToString()) ? d["Name"] : "",
                                  OldRoom = !string.IsNullOrEmpty(d["OldRoom"].ToString()) ? d["OldRoom"] : "",
                                  OldRoomType = !string.IsNullOrEmpty(d["OldRoomType"].ToString()) ? d["OldRoomType"] : "",
                                  OldHKStatus = !string.IsNullOrEmpty(d["OldHKStatus"].ToString()) ? d["OldHKStatus"] : "",
                                  NewRoom = !string.IsNullOrEmpty(d["NewRoom"].ToString()) ? d["NewRoom"] : "",
                                  NewRoomType = !string.IsNullOrEmpty(d["NewRoomType"].ToString()) ? d["NewRoomType"] : "",
                                  NewHKStatus = !string.IsNullOrEmpty(d["NewHKStatus"].ToString()) ? d["NewHKStatus"] : "",
                                  User = !string.IsNullOrEmpty(d["User"].ToString()) ? d["User"] : "",
                                  MoveDate = !string.IsNullOrEmpty(d["MoveDate"].ToString()) ? d["MoveDate"] : "",
                                  Reason = !string.IsNullOrEmpty(d["Reason"].ToString()) ? d["Reason"] : "",
                              }).ToList();

                Console.WriteLine("So dong: " + result.Count);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
            // report.DataSource = dataTable;

            // Không cần gán parameter
            //report.RequestParameters = false;

            //     return PartialView("_ReportViewerPartial", report);
        }
      

        [HttpGet]
        public IActionResult DepositTransferredAtCheckIn(DateTime fromDate)
        {
            try
            {
                DataTable dataTable = _iReportService.DepositTransferredAtCheckIn(fromDate);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  Name = $"{d["LastName"]}",
                                  RoomNo = !string.IsNullOrEmpty(d["RoomNo"].ToString()) ? d["RoomNo"] : "",
                                  PaidOn = !string.IsNullOrEmpty(d["PaidOn"].ToString()) ? d["PaidOn"] : "",
                                  DepositRsq = !string.IsNullOrEmpty(d["DepositRsq"].ToString()) ? d["DepositRsq"] : "",
                                  DepositPayment = !string.IsNullOrEmpty(d["DepositPayment"].ToString()) ? d["DepositPayment"] : "",
                                  CurrencyMaster = !string.IsNullOrEmpty(d["CurrencyMaster"].ToString()) ? d["CurrencyMaster"] : "",
                                  DepartureDate = !string.IsNullOrEmpty(d["DepartureDate"].ToString()) ? d["DepartureDate"] : "",                            
                              }).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult DepositRequestLogReportData(DateTime fromDate, DateTime toDate)
        {
            try
            {
                DataTable dataTable = _iReportService.DepositRequestLogReportData(fromDate, toDate);

                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  rsvID = d["RsvID"]?.ToString() ?? "",
                                  reservationNo = d["ReservationNo"]?.ToString() ?? "",
                                  confirmationNo = d["ConfirmationNo"]?.ToString() ?? "",
                                  guestName = d["GuestName"]?.ToString() ?? "",
                                  roomNo = d["RoomNo"]?.ToString() ?? "",
                                  roomType = d["RoomType"]?.ToString() ?? "",
                                  arrivalDate = d["ArrivalDate"]?.ToString() ?? "",
                                  departureDate = d["DepartureDate"]?.ToString() ?? "",
                                  change = d["Change"]?.ToString() ?? "",
                                  oldValue = d["OldValue"]?.ToString() ?? "",
                                  newValue = d["NewValue"]?.ToString() ?? "",
                                  changeDate = d["ChangeDate"]?.ToString() ?? "",
                                  userName = d["UserName"]?.ToString() ?? "",
                                  description = d["Description"]?.ToString() ?? ""
                              }).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi load dữ liệu log: " + ex.Message });
            }
        }
        [HttpGet]
        public IActionResult TransferARReportsData(DateTime fromDate)
        {
            try
            {
                DataTable dataTable = _iReportService.TransferARReportsData(fromDate);

                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  acc = d["ACC"]?.ToString() ?? "",
                                  companyOrName = d["Company/name"]?.ToString() ?? "",
                                  account = d["Account"]?.ToString() ?? "",
                                  accountName = d["AccountName"]?.ToString() ?? "",
                                  roomNo = d["RoomNo"]?.ToString() ?? "",
                                  folioNo = d["FolioNo"]?.ToString() ?? "",
                                  amount = d["Amount"]?.ToString() ?? "",
                                  userName = d["UserName"]?.ToString() ?? "",
                                  currencyID = d["CurrencyID"]?.ToString() ?? ""
                              }).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy dữ liệu Transfer AR: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult ArticleByRoomsData(DateTime fromDate, DateTime toDate,string zone,string room, int viewBy,string article)
        {
            zone = zone ?? "";
            room = room ?? "";
            article = article ?? "";
            try
            {
                DataTable dataTable = _iReportService.ArticleByRoomsDatas(fromDate, toDate, zone, room, viewBy, article);

                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  TransactionDate = d["TransactionDate"].ToString(),
                                  RoomNo = d["RoomNo"].ToString(),
                                  AccountName = d["AccountName"].ToString(),
                                  TransactionCode = d["TransactionCode"].ToString(),
                                  TrnsDescription = d["TrnsDescription"].ToString(),
                                  ArticleCode = d["ArticleCode"].ToString(),
                                  ArticleDescription = d["ArticleDescription"].ToString(),
                                  InvoiceNo = d["InvoiceNo"].ToString(),
                                  Supplement = d["Supplement"].ToString(),
                                  Reference = d["Reference"].ToString(),
                                  CurrencyMaster = d["CurrencyMaster"].ToString(),
                                  DebitAmount = d["DebitAmount"].ToString(),
                                  CreditAmount = d["CreditAmount"].ToString(),
                                  AmountMaster = d["AmountMaster"].ToString(),
                                  CashierNo = d["CashierNo"].ToString(),
                                  UserName = d["UserName"].ToString(),
                                  RoomClass = d["RoomClass"].ToString(),
                                  TransactionGroup = d["TransactionGroup"].ToString(),
                                  TransactionSubGroup = d["TransactionSubGroup"].ToString(),
                                  Quantity = d["Quantity"].ToString(),
                                  Price = d["Price"].ToString(),
                                  AmountBeforeTax = d["AmountBeforeTax"].ToString(),
                                  Amount = d["Amount"].ToString(),
                                  ConfirmationNo = d["ConfirmationNo"].ToString(),
                                  LastName = d["LastName"].ToString(),
                                  ArrivalDate = d["ArrivalDate"].ToString(),
                                  DepartureDate = d["DepartureDate"].ToString()
                              }).ToList();


                return Json(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy dữ liệu Transfer AR: " + ex.Message });
            }
        }


        [HttpGet]
        public IActionResult DailyMinibarReportData(DateTime fromDate, DateTime toDate, string zone, string room, int viewBy, string article)
        {
            zone = zone ?? "";
            room = room ?? "";
            article = article ?? "";
            try
            {
                DataTable dataTable = _iReportService.DailyMinibarReportData(fromDate, toDate, zone, room, viewBy, article);

                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  TransactionDate = d["TransactionDate"].ToString(),
                                  RoomNo = d["RoomNo"].ToString(),
                                  AccountName = d["AccountName"].ToString(),
                                  TransactionCode = d["TransactionCode"].ToString(),
                                  TrnsDescription = d["TrnsDescription"].ToString(),
                                  ArticleCode = d["ArticleCode"].ToString(),
                                  ArticleDescription = d["ArticleDescription"].ToString(),
                                  InvoiceNo = d["InvoiceNo"].ToString(),
                                  Supplement = d["Supplement"].ToString(),
                                  Reference = d["Reference"].ToString(),
                                  CurrencyMaster = d["CurrencyMaster"].ToString(),
                                  DebitAmount = d["DebitAmount"].ToString(),
                                  CreditAmount = d["CreditAmount"].ToString(),
                                  AmountMaster = d["AmountMaster"].ToString(),
                                  CashierNo = d["CashierNo"].ToString(),
                                  UserName = d["UserName"].ToString(),
                                  RoomClass = d["RoomClass"].ToString(),
                                  TransactionGroup = d["TransactionGroup"].ToString(),
                                  TransactionSubGroup = d["TransactionSubGroup"].ToString(),
                                  Quantity = d["Quantity"].ToString(),
                                  Price = d["Price"].ToString(),
                                  AmountBeforeTax = d["AmountBeforeTax"].ToString(),
                                  Amount = d["Amount"].ToString(),
                                  ConfirmationNo = d["ConfirmationNo"].ToString(),
                                  LastName = d["LastName"].ToString(),
                                  ArrivalDate = d["ArrivalDate"].ToString(),
                                  DepartureDate = d["DepartureDate"].ToString()
                              }).ToList();


                return Json(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy dữ liệu Transfer AR: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult RoomDiscrepancy(int Sleep = 0, int Skip = 0, int Person = 0)
        {
            try
            {
                DataTable dataTable = _iReportService.RoomDiscrepancy(Sleep, Skip, Person); 
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  RoomNo = !string.IsNullOrEmpty(d["Room No"].ToString()) ? d["Room No"] : "",
                                  RoomType = !string.IsNullOrEmpty(d["Room Type"].ToString()) ? d["Room Type"] : "",
                                  RoomStatus = !string.IsNullOrEmpty(d["Room Status"].ToString()) ? d["Room Status"] : "",
                                  FOStatus = !string.IsNullOrEmpty(d["FO Status"].ToString()) ? d["FO Status"] : "",
                                  HKStatus = !string.IsNullOrEmpty(d["HK Status"].ToString()) ? d["HK Status"] : "",
                                  HKPersons = !string.IsNullOrEmpty(d["HK Persons"].ToString()) ? d["HK Persons"] : "",
                                  FOPersons = !string.IsNullOrEmpty(d["FO Persons"].ToString()) ? d["FO Persons"] : "",
                                  SleepOrSkip = !string.IsNullOrEmpty(d["Sleep / Skip"].ToString()) ? d["Sleep / Skip"] : "",
                                  PersonDiscrepancy = !string.IsNullOrEmpty(d["Person Discrepancy"].ToString()) ? d["Person Discrepancy"] : ""
                              }).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
        [HttpGet]
        public IActionResult DepositLedger(DateTime fromDate)
        {
            try
            {
                DataTable dataTable = _iReportService.DepositLedger(fromDate);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  Name = !string.IsNullOrEmpty(d["Name"].ToString()) ? d["Name"] : "",
                                  ConfirmNo = !string.IsNullOrEmpty(d["ConfirmNo"].ToString()) ? d["ConfirmNo"] : "",
                                  Arrival = !string.IsNullOrEmpty(d["Arrival"].ToString()) ? d["Arrival"] : "",
                                  Departure = !string.IsNullOrEmpty(d["Departure"].ToString()) ? d["Departure"] : "",
                                  Room = !string.IsNullOrEmpty(d["Room"].ToString()) ? d["Room"] : "",
                                  ReservationTypeCode = !string.IsNullOrEmpty(d["ReservationTypeCode"].ToString()) ? d["ReservationTypeCode"] : "",
                                  RsvStatus = !string.IsNullOrEmpty(d["RsvStatus"].ToString()) ? d["RsvStatus"] : "",
                                  LastPaid = !string.IsNullOrEmpty(d["LastPaid"].ToString()) ? d["LastPaid"] : "",
                                  DepositBalance = !string.IsNullOrEmpty(d["DepositBalance"].ToString()) ? d["DepositBalance"] : "",
                                  CurrencyID = !string.IsNullOrEmpty(d["CurrencyID"].ToString()) ? d["CurrencyID"] : "",
                              }).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
      
        [HttpGet]
        public IActionResult RevenueReports(DateTime fromDate, int type)
        {
            try
            {
                if (fromDate < new DateTime(1753, 1, 1))
                {
                    fromDate = DateTime.Today;
                }
                DataTable dataTable = _iReportService.RevenueReports(fromDate, type);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  TransactionCode = !string.IsNullOrEmpty(d["TransactionCode"].ToString()) ? d["TransactionCode"] : "",
                                  TransactionName = !string.IsNullOrEmpty(d["TransactionName"].ToString()) ? d["TransactionName"] : "",
                                  CurrencyID = !string.IsNullOrEmpty(d["CurrencyID"].ToString()) ? d["CurrencyID"] : "",           
                                  SubGroupDescription = !string.IsNullOrEmpty(d["SubGroupDescription"].ToString()) ? d["SubGroupDescription"] : "",
                                  AmountBeForeTax = !string.IsNullOrEmpty(d["AmountBeForeTax"].ToString()) ? Convert.ToDecimal(d["AmountBeForeTax"]) : 0,
                                  AmountGross = !string.IsNullOrEmpty(d["AmountGross"].ToString()) ? Convert.ToDecimal(d["AmountGross"]) : 0,
                                  AmountBeforeTaxM = !string.IsNullOrEmpty(d["AmountBeforeTaxM"].ToString()) ? Convert.ToDecimal(d["AmountBeforeTaxM"]) : 0,
                                  AmountGrossM = !string.IsNullOrEmpty(d["AmountGrossM"].ToString()) ? Convert.ToDecimal(d["AmountGrossM"]) : 0,
                                  AmountBeforeTaxY = !string.IsNullOrEmpty(d["AmountBeforeTaxY"].ToString()) ? Convert.ToDecimal(d["AmountBeforeTaxY"]) : 0,
                                  AmountGrossY = !string.IsNullOrEmpty(d["AmountGrossY"].ToString()) ? Convert.ToDecimal(d["AmountGrossY"]) : 0
                              }).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
        [HttpGet]
        public IActionResult PostingJournalInvoicing(string fromDate, string toDate)
        {
            try
            {
                DataTable dataTable = _iReportService.PostingJournalInvoicing(ParseReportDate(fromDate), ParseReportDate(toDate));
                if (dataTable == null) return Json(Array.Empty<object>());
                var result = new List<Dictionary<string, object>>(dataTable.Rows.Count);
                foreach (DataRow d in dataTable.Rows)
                {
                    object timeVal = "";
                    var timeCol = FirstExisting(d, "Time", "TransactionDate");
                    if (!string.IsNullOrEmpty(timeCol) && d[timeCol] != DBNull.Value && d[timeCol] is DateTime tdt)
                        timeVal = tdt;
                    else
                        timeVal = FirstCol(d, "Time", "TransactionDate");
                    result.Add(new Dictionary<string, object>
                    {
                        ["room"] = FirstCol(d, "Room", "RoomNo"),
                        ["folioNo"] = FirstCol(d, "FolioNo", "FolioID", "Folio"),
                        ["transactionCode"] = FirstCol(d, "TransactionCode", "Code"),
                        ["description"] = ColStr(d, "Description"),
                        ["usd"] = ColDec(d, "DebitUSD") + ColDec(d, "CreditUSD"),
                        ["vnd"] = ColDec(d, "DebitVND") + ColDec(d, "CreditVND"),
                        ["transactionDate"] = timeVal,
                        ["cashierNo"] = FirstCol(d, "CashierNo", "Cashier"),
                    });
                }
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new Dictionary<string, object> { ["error"] = ex.Message });
            }
            // report.DataSource = dataTable;

            // Không cần gán parameter
            //report.RequestParameters = false;

            //     return PartialView("_ReportViewerPartial", report);
        }

        [HttpGet]
        public IActionResult BookingSummaryByStatusData(DateTime fromDate, DateTime toDate)
        {
            try
            {

                var data = _iReportService.BookingSummaryByStatusData(fromDate, toDate);
                var result = (from d in data.AsEnumerable()
                              select new
                              {

                                  Status = !string.IsNullOrEmpty(d["Status"].ToString()) ? d["Status"] : "",
                                  StatusName = !string.IsNullOrEmpty(d["StatusName"].ToString()) ? d["StatusName"] : "",
                                  Total = !string.IsNullOrEmpty(d["Total"].ToString()) ? d["Total"] : "",
                              }).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error generating cancellation journal report: {ex.Message}");
            }
        }


        [HttpGet]
        public IActionResult VacantRoomData(string roomClass, string roomtype,string FromRoom,string ToRoom,string OrderByRoomNo,string OrderByHKPStatus,string OrderByFOStatus,string HKPStatus,string FOStatus,string IsGroupByRoomClass)
        {
            roomClass = roomClass ?? "";
            roomtype = roomtype ?? "";
            FromRoom = FromRoom ?? "";
            ToRoom = ToRoom ?? "";
            try
            {

                var data = _iReportService.VacantRoomData(roomClass, roomtype, FromRoom, ToRoom, OrderByRoomNo, OrderByHKPStatus, OrderByFOStatus, HKPStatus, FOStatus,IsGroupByRoomClass);
                var result = (from d in data.AsEnumerable()
                              select new
                              {

                                  RoomNo = !string.IsNullOrEmpty(d["RoomNo"].ToString()) ? d["RoomNo"] : "",
                                  Code = !string.IsNullOrEmpty(d["Code"].ToString()) ? d["Code"] : "",
                                  FOStatus = !string.IsNullOrEmpty(d["FOStatus"].ToString()) ? d["FOStatus"] : "",

                                  Name = !string.IsNullOrEmpty(d["Name"].ToString()) ? d["Name"] : "",
                                  ArrDate = !string.IsNullOrEmpty(d["Arr.Date"].ToString()) ? d["Arr.Date"] : "",
                                  DepDate = !string.IsNullOrEmpty(d["Dep.Date"].ToString()) ? d["Dep.Date"] : "",

                                  ResvStatus = !string.IsNullOrEmpty(d["Resv. Status"].ToString()) ? d["Resv. Status"] : "",
                                  Adl = !string.IsNullOrEmpty(d["Adl"].ToString()) ? d["Adl"] : "",
                                  Chld = !string.IsNullOrEmpty(d["Chld"].ToString()) ? d["Chld"] : "",

                                  Status = !string.IsNullOrEmpty(d["Status"].ToString()) ? d["Status"] : "",
                                  NextBlock = !string.IsNullOrEmpty(d["NextBlock"].ToString()) ? d["NextBlock"] : "",
                              }).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error generating cancellation journal report: {ex.Message}");
            }
        }

        [HttpGet]
        public IActionResult CancellationJournal(DateTime fromDate, DateTime toDate)
        {
            try
            {
                string sql = @"Select AdjustmentCode from Transactions where AdjustmentCode!='''' and AdjustmentCode is not null group by AdjustmentCode";
                DataTable dt = TextUtils.Select(sql);

                var transactionCodeList = string.Join("','", dt.AsEnumerable().Select(row => row["AdjustmentCode"]?.ToString().Trim()).Where(x => !string.IsNullOrEmpty(x)).Distinct());

                var data = _iReportService.CancellationJournal(fromDate, toDate, transactionCodeList);
                var result = (from d in data.AsEnumerable()
                              select new
                              {
                                  TransactionDate = !string.IsNullOrEmpty(d["TransactionDate"].ToString()) ? d["TransactionDate"] : "",
                                  Time = !string.IsNullOrEmpty(d["Time"].ToString()) ? d["Time"] : "",
                                  CashierNo = !string.IsNullOrEmpty(d["CashierNo"].ToString()) ? d["CashierNo"] : "",
                                  Room = !string.IsNullOrEmpty(d["Room"].ToString()) ? d["Room"] : "",
                                  FolioNo = !string.IsNullOrEmpty(d["FolioNo"].ToString()) ? d["FolioNo"] : "",
                                  TransactionCode = !string.IsNullOrEmpty(d["TransactionCode"].ToString()) ? d["TransactionCode"] : "",
                                  Description = !string.IsNullOrEmpty(d["Description"].ToString()) ? d["Description"] : "",
                                  DebitUSD = !string.IsNullOrEmpty(d["DebitUSD"].ToString()) ? d["DebitUSD"] : "",
                                  DebitVND = !string.IsNullOrEmpty(d["DebitVND"].ToString()) ? d["DebitVND"] : "",
                                  Reference = !string.IsNullOrEmpty(d["Reference"].ToString()) ? d["Reference"] : "",
                              }).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error generating cancellation journal report: {ex.Message}");
            }
        }
        [HttpGet]
        public IActionResult TrialBalance(DateTime dtpDate, string currency)
        {
            try
            {          
                var data = _iReportService.TrialBalance(dtpDate, currency);
                var result = (from d in data.AsEnumerable()
                              select new
                              {
                                  BoldField = !string.IsNullOrEmpty(d["BoldField"].ToString()) ? d["BoldField"] : "",
                                  Code = !string.IsNullOrEmpty(d["Code"].ToString()) ? d["Code"] : "",
                                  Description = !string.IsNullOrEmpty(d["Description"].ToString()) ? d["Description"] : "",
                                  Amount = !string.IsNullOrEmpty(d["Amount"].ToString()) ? d["Amount"] : "",                                 
                              }).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error generating cancellation journal report: {ex.Message}");
            }
        }
        [HttpGet]
        public IActionResult ReservationRateCheck(DateTime date, string status, int ind, int pseudo, int variance,
                                              int fixRate, int package, int dcReason, string sort,
                                              int showFixCharge, int showAlerts)
        {
            try
            {
                var statuses = (status ?? "")
                    .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                    .Where(code => code.Length == 1 && code[0] >= '0' && code[0] <= '8')
                    .Distinct()
                    .ToArray();
                if (statuses.Length == 0)
                    statuses = new[] { "0", "1", "2", "3", "4", "5", "6", "7", "8" };

                var loadStatus = (string code) =>
                {
                    var data = _iReportService.ReservationRateCheck(date, code, ind, pseudo, variance,
                                                                    fixRate, package, dcReason, sort, showFixCharge, showAlerts);

                    return (from d in data.AsEnumerable()
                              select new
                              {                             
                                  ConfirmationNo = !string.IsNullOrEmpty(d["ConfirmationNo"].ToString()) ? d["ConfirmationNo"] : "",
                                  Name = !string.IsNullOrEmpty(d["Name"].ToString()) ? d["Name"] : "",
                                  Arrival = !string.IsNullOrEmpty(d["Arrival"].ToString()) ? d["Arrival"] : "",
                                  Departure = !string.IsNullOrEmpty(d["Departure"].ToString()) ? d["Departure"] : "",
                                  Nts = !string.IsNullOrEmpty(d["Nts."].ToString()) ? d["Nts."] : "",
                                  Adl = !string.IsNullOrEmpty(d["Adl."].ToString()) ? d["Adl."] : "",
                                  Chl = !string.IsNullOrEmpty(d["Chl."].ToString()) ? d["Chl."] : "",
                                  RoomType = !string.IsNullOrEmpty(d["Room Type"].ToString()) ? d["Room Type"] : "",
                                  RateCode = !string.IsNullOrEmpty(d["Rate Code"].ToString()) ? d["Rate Code"] : "",
                                  RateCodeAmt = !string.IsNullOrEmpty(d["Rate Code Amt."].ToString()) ? d["Rate Code Amt."] : "",
                                  RateAmt = !string.IsNullOrEmpty(d["Rate Amt."].ToString()) ? d["Rate Amt."] : "",
                                  Variance = !string.IsNullOrEmpty(d["Variance"].ToString()) ? d["Variance"] : "",
                                  PayMth = !string.IsNullOrEmpty(d["Pay Mth."].ToString()) ? d["Pay Mth."] : "",
                                  ResvStatus = !string.IsNullOrEmpty(d["Resv. Status"].ToString()) ? d["Resv. Status"] : "",
                                  FixedRate = !string.IsNullOrEmpty(d["Fixed Rate"].ToString()) ? d["Fixed Rate"] : "",
                                  RoomNo = !string.IsNullOrEmpty(d["Room No."].ToString()) ? d["Room No."] : "",
                                  Holder = !string.IsNullOrEmpty(d["Holder"].ToString()) ? d["Holder"] : "",
                                  MarketCode = !string.IsNullOrEmpty(d["MarketCode"].ToString()) ? d["MarketCode"] : "",
                                  Cur = !string.IsNullOrEmpty(d["Cur."].ToString()) ? d["Cur."] : "",
                                  ReservationPacketes =  !string.IsNullOrEmpty(d["Reservation Packages"].ToString()) ? d["Reservation Packages"] : "",

                              }).ToList();
                };

                var result = loadStatus(statuses[0]);
                if (statuses.Length == 1)
                    return Json(result);

                var seen = result.ToHashSet();
                foreach (var code in statuses.Skip(1))
                {
                    var rows = loadStatus(code);
                    result.AddRange(rows.Where(row => !seen.Contains(row)));
                    seen.UnionWith(rows);
                }

                return Json(sort == "Name"
                    ? result.OrderBy(row => row.Name)
                    : result.OrderBy(row => row.RoomNo));
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error generating Reservation Rate Code Check report: {ex.Message}");
            }
        }
        [HttpGet]
        public IActionResult GuestLedger(DateTime date, string statusList)
        {
            try
            {
                var data = _iReportService.GuestLedger(date, statusList);

                var result = (from d in data.AsEnumerable()
                              select new
                              {
                                  RoomNo = !string.IsNullOrEmpty(d["RoomNo"].ToString()) ? d["RoomNo"] : "",
                                  Reservation = !string.IsNullOrEmpty(d["Reservation"].ToString()) ? d["Reservation"] : "",
                                  LastName = !string.IsNullOrEmpty(d["LastName"].ToString()) ? d["LastName"] : "",
                                  ConfirmationNo = !string.IsNullOrEmpty(d["ConfirmationNo"].ToString()) ? d["ConfirmationNo"] : "",
                                  ArrivalDate = !string.IsNullOrEmpty(d["ArrivalDate"].ToString()) ? d["ArrivalDate"] : "",
                                  DepartureDate = !string.IsNullOrEmpty(d["DepartureDate"].ToString()) ? d["DepartureDate"] : "",
                                  FolioID = !string.IsNullOrEmpty(d["FolioID"].ToString()) ? d["FolioID"] : "",
                                  Amount = !string.IsNullOrEmpty(d["Amount"].ToString()) ? d["Amount"] : "",
                                  CurrencyID = !string.IsNullOrEmpty(d["CurrencyID"].ToString()) ? d["CurrencyID"] : "",
                     
                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error generating Reservation Rate Code Check report: {ex.Message}");
            }
        }
        [HttpGet]
        public IActionResult RevenueRoomReport(DateTime fromDate, DateTime toDate, string roomTypes, int zone)
        {
            try
            {
                var data = _iReportService.RevenueRoomReport(fromDate, toDate, roomTypes, zone);

                var result = (from d in data.AsEnumerable()
                              select new
                              {
                                  RoomType = !string.IsNullOrEmpty(d["RoomType"].ToString()) ? d["RoomType"] : "",
                                  Zone = !string.IsNullOrEmpty(d["Zone"].ToString()) ? d["Zone"] : "",
                                  RoomDate = !string.IsNullOrEmpty(d["RoomDate"].ToString()) ? d["RoomDate"] : "",
                                  TotalRoom = !string.IsNullOrEmpty(d["TotalRoom"].ToString()) ? d["TotalRoom"] : "",
                                  Room = !string.IsNullOrEmpty(d["Room"].ToString()) ? d["Room"] : "",
                                  OCC = !string.IsNullOrEmpty(d["OCC"].ToString()) ? d["OCC"] : "",
                                  Persons = !string.IsNullOrEmpty(d["Persons"].ToString()) ? d["Persons"] : "",
                                  RoomSales = !string.IsNullOrEmpty(d["RoomSales"].ToString()) ? d["RoomSales"] : "",
                                  TurnOver = !string.IsNullOrEmpty(d["TurnOver"].ToString()) ? d["TurnOver"] : "",
                                  RoomSalesVND = !string.IsNullOrEmpty(d["RoomSalesVND"].ToString()) ? d["RoomSalesVND"] : "",
                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error generating Reservation Rate Code Check report: {ex.Message}");
            }
        }
        [HttpGet]
        public IActionResult OccupancybyPersonData(DateTime fromDate, DateTime toDate, int roomTypeID)
        {
            try
            {
                DataTable dt = _iReportService.OccupancybyPerson(fromDate, toDate, roomTypeID);
                var result = (from d in dt.AsEnumerable()
                              select new
                              {
                                  RateCode = !string.IsNullOrEmpty(d["RateCode"].ToString()) ? d["RateCode"] : "",
                                  DayOfWeek = !string.IsNullOrEmpty(d["DayOfWeek"].ToString()) ? d["DayOfWeek"] : "",
                                  Date = !string.IsNullOrEmpty(d["Date"].ToString()) ? d["Date"] : "",
                                  VacRo = !string.IsNullOrEmpty(d["VacRo"].ToString()) ? d["VacRo"] : "",
                                  ResRo = !string.IsNullOrEmpty(d["ResRo"].ToString()) ? d["ResRo"] : "",
                                  IndPax = !string.IsNullOrEmpty(d["IndPax"].ToString()) ? d["IndPax"] : "",
                                  GrpPax = !string.IsNullOrEmpty(d["GrpPax"].ToString()) ? d["GrpPax"] : "",
                                  TotalPax1 = !string.IsNullOrEmpty(d["TotalPax1"].ToString()) ? d["TotalPax1"] : "",
                                  ArrPax = !string.IsNullOrEmpty(d["ArrPax"].ToString()) ? d["ArrPax"] : "",
                                  DepPax = !string.IsNullOrEmpty(d["DepPax"].ToString()) ? d["DepPax"] : "",
                                  WaitPax = !string.IsNullOrEmpty(d["WaitPax"].ToString()) ? d["WaitPax"] : "",
                                  OOORo = !string.IsNullOrEmpty(d["OOORo"].ToString()) ? d["OOORo"] : "",
                                  TotalPax2 = !string.IsNullOrEmpty(d["TotalPax2"].ToString()) ? d["TotalPax2"] : "",
                                  Phantram = !string.IsNullOrEmpty(d["%"].ToString()) ? d["%"] : "",
                                  AccomOrPerson = !string.IsNullOrEmpty(d["Accom/Person"].ToString()) ? d["Accom/Person"] : "",
                                  Accommodation = !string.IsNullOrEmpty(d["Accommodation"].ToString()) ? d["Accommodation"] : "",
                                  FAndB = !string.IsNullOrEmpty(d["F&B"].ToString()) ? d["F&B"] : "",
                                  Total = !string.IsNullOrEmpty(d["Total"].ToString()) ? d["Total"] : "",

                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        [HttpGet]
        public IActionResult ArrivalsDetailedData(DateTime fromDate, DateTime toDate, string roomClass,string roomtype,string market,string rateCode,string source, string vip,string viponlycheck,string noPost, int sortOrder,
            string  pseudo,string checkedInToday,string cancellations,string zeroRateOnly,int  disRoomSharer,int searchCriteria,int ckhArrivalDate)
        {
            roomClass = roomClass ?? "";
            roomtype = roomtype ?? "";
            market = market ?? "";
            rateCode = rateCode ?? "";
            source = source ?? "";
            pseudo = pseudo ?? "";
            vip = vip ?? "";
            viponlycheck = viponlycheck ?? "";
            noPost = noPost ?? "";
            checkedInToday = checkedInToday ?? "";
            cancellations = cancellations ?? "";
            zeroRateOnly = zeroRateOnly ?? "";
            try
            {
                DataTable dt = _iReportService.ArrivalsDetailedData(
                                        fromDate,
                                        toDate,
                                        roomClass,
                                        roomtype,
                                        market,
                                        rateCode,
                                        source,
                                        vip,
                                        viponlycheck,
                                        noPost,
                                        sortOrder,
                                        pseudo,
                                        checkedInToday,
                                        cancellations,
                                        zeroRateOnly,
                                        disRoomSharer,
                                        searchCriteria,
                                        ckhArrivalDate
                                    );

                var result = (from d in dt.AsEnumerable()
                              select new
                              {
                                  RoomNo = d["RoomNo"]?.ToString() ?? "",
                                  Name = d["Name"]?.ToString() ?? "",
                                  ConfirmationNo = d["ConfirmationNo"]?.ToString() ?? "",
                                  Company = d["Company"]?.ToString() ?? "",
                                  Agent = d["Agent"]?.ToString() ?? "",
                                  Source = d["Source"]?.ToString() ?? "",
                                  BusinessBlockCode = d["BusinessBlockCode"]?.ToString() ?? "",
                                  ArrivalDate = d["ArrivalDate"]?.ToString() ?? "",
                                  RoomClassID = d["RoomClassID"]?.ToString() ?? "",
                                  DepartureDate = d["DepartureDate"]?.ToString() ?? "",
                                  ETA = d["ETA"]?.ToString() ?? "",
                                  RoomType = d["RoomType"]?.ToString() ?? "",
                                  PickupCarrierCode = d["PickupCarrierCode"]?.ToString() ?? "",
                                  PickupTransportType = d["PickupTransportType"]?.ToString() ?? "",
                                  NoOfAdult = d["NoOfAdult"]?.ToString() ?? "",
                                  NoOfChild = d["NoOfChild"]?.ToString() ?? "",
                                  NoOfChild1 = d["NoOfChild1"]?.ToString() ?? "",
                                  NoOfChild2 = d["NoOfChild2"]?.ToString() ?? "",
                                  NoOfRoom = d["NoOfRoom"]?.ToString() ?? "",
                                  MarketCode = d["MarketCode"]?.ToString() ?? "",
                                  SourceCode = d["SourceCode"]?.ToString() ?? "",
                                  ReservationTypeCode = d["ReservationTypeCode"]?.ToString() ?? "",
                                  Status = d["Status"]?.ToString() ?? "",
                                  RateCode = d["RateCode"]?.ToString() ?? "",
                                  Rate = d["Rate"]?.ToString() ?? "",
                                  CurrencyID = d["CurrencyID"]?.ToString() ?? "",
                                  PaymentMethod = d["PaymentMethod"]?.ToString() ?? "",
                                  CreditCardNo = d["CreditCardNo"]?.ToString() ?? "",
                                  ExpirationDate = d["ExpirationDate"]?.ToString() ?? "",
                                  AmountMaster = d["AmountMaster"]?.ToString() ?? "",
                                  CurrencyMaster = d["CurrencyMaster"]?.ToString() ?? "",
                                  ProfileID = d["ProfileID"]?.ToString() ?? "",
                                  NoPost = d["NoPost"]?.ToString() ?? "",
                                  IsPseudo = d["IsPseudo"]?.ToString() ?? "",
                                  ShareRoomName = d["ShareRoomName"]?.ToString() ?? "",
                                  AccompanyName = d["AccompanyName"]?.ToString() ?? "",
                                  ItemInventory = d["ItemInventory"]?.ToString() ?? "",
                                  FixedCharge = d["FixedCharge"]?.ToString() ?? "",
                                  Specials = d["Specials"]?.ToString() ?? "",
                                  Packages = d["Packages"]?.ToString() ?? "",
                                  VIP = d["VIP"]?.ToString() ?? "",
                                  EmployeeID = d["EmployeeID"]?.ToString() ?? "",
                                  RoutingTransaction = d["RoutingTransaction"]?.ToString() ?? "",
                                  RoutingToProfile = d["RoutingToProfile"]?.ToString() ?? "",
                                  Party = d["Party"]?.ToString() ?? "",
                                  Comment = d["Comment"]?.ToString() ?? "",
                                  PrintRate = d["PrintRate"]?.ToString() ?? "",
                                  MemberType = d["MemberType"]?.ToString() ?? "",
                                  MemberNo = d["MemberNo"]?.ToString() ?? "",
                                  PrevStays = d["PrevStays"]?.ToString() ?? "",
                                  LastRoom = d["LastRoom"]?.ToString() ?? "",
                                  ReservationHolder = d["ReservationHolder"]?.ToString() ?? "",
                                  GroupCode = d["GroupCode"]?.ToString() ?? ""
                              }).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }



        [HttpGet]
        public IActionResult ArrivalDetailGroupbyHoldersData(DateTime fromDate, DateTime toDate, string roomClass, string roomtype, string market, string rateCode, string source, string vip, string viponlycheck, string noPost, int sortOrder,
            string pseudo, string checkedInToday, string cancellations, string zeroRateOnly, int disRoomSharer, int searchCriteria, int ckhArrivalDate)
        {
            roomClass = roomClass ?? "";
            roomtype = roomtype ?? "";
            market = market ?? "";
            rateCode = rateCode ?? "";
            source = source ?? "";
            pseudo = pseudo ?? "";
            vip = vip ?? "";
            viponlycheck = viponlycheck ?? "";
            noPost = noPost ?? "";
            checkedInToday = checkedInToday ?? "";
            cancellations = cancellations ?? "";
            zeroRateOnly = zeroRateOnly ?? "";
            try
            {
                DataTable dt = _iReportService.ArrivalDetailGroupbyHoldersData(
    fromDate,
    toDate,
    roomClass,
    roomtype,
    market,
    rateCode,
    source,
    vip,
    viponlycheck,
    noPost,
    sortOrder,
    pseudo,
    checkedInToday,
    cancellations,
    zeroRateOnly,
    disRoomSharer,
    searchCriteria,
    ckhArrivalDate
);

                var result = (from d in dt.AsEnumerable()
                              select new
                              {
                                  RoomNo = d["RoomNo"]?.ToString() ?? "",
                                  Name = d["Name"]?.ToString() ?? "",
                                  ConfirmationNo = d["ConfirmationNo"]?.ToString() ?? "",
                                  Company = d["Company"]?.ToString() ?? "",
                                  Agent = d["Agent"]?.ToString() ?? "",
                                  Source = d["Source"]?.ToString() ?? "",
                                  BusinessBlockCode = d["BusinessBlockCode"]?.ToString() ?? "",
                                  ArrivalDate = d["ArrivalDate"]?.ToString() ?? "",
                                  RoomClassID = d["RoomClassID"]?.ToString() ?? "",
                                  DepartureDate = d["DepartureDate"]?.ToString() ?? "",
                                  ETA = d["ETA"]?.ToString() ?? "",
                                  RoomType = d["RoomType"]?.ToString() ?? "",
                                  PickupCarrierCode = d["PickupCarrierCode"]?.ToString() ?? "",
                                  PickupTransportType = d["PickupTransportType"]?.ToString() ?? "",
                                  NoOfAdult = d["NoOfAdult"]?.ToString() ?? "",
                                  NoOfChild = d["NoOfChild"]?.ToString() ?? "",
                                  NoOfChild1 = d["NoOfChild1"]?.ToString() ?? "",
                                  NoOfChild2 = d["NoOfChild2"]?.ToString() ?? "",
                                  NoOfRoom = d["NoOfRoom"]?.ToString() ?? "",
                                  MarketCode = d["MarketCode"]?.ToString() ?? "",
                                  SourceCode = d["SourceCode"]?.ToString() ?? "",
                                  ReservationTypeCode = d["ReservationTypeCode"]?.ToString() ?? "",
                                  Status = d["Status"]?.ToString() ?? "",
                                  RateCode = d["RateCode"]?.ToString() ?? "",
                                  Rate = d["Rate"]?.ToString() ?? "",
                                  CurrencyID = d["CurrencyID"]?.ToString() ?? "",
                                  PaymentMethod = d["PaymentMethod"]?.ToString() ?? "",
                                  CreditCardNo = d["CreditCardNo"]?.ToString() ?? "",
                                  ExpirationDate = d["ExpirationDate"]?.ToString() ?? "",
                                  AmountMaster = d["AmountMaster"]?.ToString() ?? "",
                                  CurrencyMaster = d["CurrencyMaster"]?.ToString() ?? "",
                                  ProfileID = d["ProfileID"]?.ToString() ?? "",
                                  NoPost = d["NoPost"]?.ToString() ?? "",
                                  IsPseudo = d["IsPseudo"]?.ToString() ?? "",
                                  ShareRoomName = d["ShareRoomName"]?.ToString() ?? "",
                                  AccompanyName = d["AccompanyName"]?.ToString() ?? "",
                                  ItemInventory = d["ItemInventory"]?.ToString() ?? "",
                                  FixedCharge = d["FixedCharge"]?.ToString() ?? "",
                                  Specials = d["Specials"]?.ToString() ?? "",
                                  Packages = d["Packages"]?.ToString() ?? "",
                                  VIP = d["VIP"]?.ToString() ?? "",
                                  EmployeeID = d["EmployeeID"]?.ToString() ?? "",
                                  RoutingTransaction = d["RoutingTransaction"]?.ToString() ?? "",
                                  RoutingToProfile = d["RoutingToProfile"]?.ToString() ?? "",
                                  Party = d["Party"]?.ToString() ?? "",
                                  Comment = d["Comment"]?.ToString() ?? "",
                                  PrintRate = d["PrintRate"]?.ToString() ?? "",
                                  MemberType = d["MemberType"]?.ToString() ?? "",
                                  MemberNo = d["MemberNo"]?.ToString() ?? "",
                                  PrevStays = d["PrevStays"]?.ToString() ?? "",
                                  LastRoom = d["LastRoom"]?.ToString() ?? "",
                                  ReservationHolder = d["ReservationHolder"]?.ToString() ?? "",
                                  GroupCode = d["GroupCode"]?.ToString() ?? ""
                              }).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }


        [HttpGet]
        public IActionResult DepartureReportsData(DateTime fromDate, DateTime toDate, string roomClass, string roomtype, string paymethod, string rateCode, string block,
            string zone, string vip, string viponlycheck, int sortOrder,
           string pseudo, string dueout, string checkout, string disRoomSharer, string  specials, string lateCheckOut, string earlyDep,string agents,string company,
           string source,string individuals,string group)
        {
            roomClass = roomClass ?? "";
            roomtype = roomtype ?? "";
            paymethod = paymethod ?? "";
            rateCode = rateCode ?? "";
            block = block ?? "";
            zone = zone ?? "";
            vip = vip ?? "";
            viponlycheck = viponlycheck ?? "";
            pseudo = pseudo ?? "";
            dueout = dueout ?? "";
            checkout = checkout ?? "";
            disRoomSharer = disRoomSharer ?? "";
            specials = specials ?? "";
            lateCheckOut = lateCheckOut ?? "";
            earlyDep = earlyDep ?? "";
            agents = agents ?? "";
            company = company ?? "";
            source = source ?? "";
            individuals = individuals ?? "";
            group = group ?? "";

            try
            {
                DataTable dt = _iReportService.DepartureReportsData(
    fromDate,
    toDate,
    roomClass,
    roomtype,
    paymethod,
    rateCode,
    block,
    zone,
    vip,
    viponlycheck,
    sortOrder,
    pseudo,
    dueout,
    checkout,
    disRoomSharer,
    specials,
    lateCheckOut,
    earlyDep,
    agents,
    company,
    source,
    individuals,
    group);


                var result = (from d in dt.AsEnumerable()
                              select new
                              {
                                  RoomNo = d["RoomNo"]?.ToString() ?? "",
                                  Name = d["Name"]?.ToString() ?? "",
                                  ReservationHolder = d["ReservationHolder"]?.ToString() ?? "",
                                  Status = d["Status"]?.ToString() ?? "",
                                  VIP = d["VIP"]?.ToString() ?? "",
                                  ArrivalDate = d["ArrivalDate"]?.ToString() ?? "",
                                  DepartureDate = d["DepartureDate"]?.ToString() ?? "",
                                  OriginalDepartureDate = d["OriginalDepartureDate"]?.ToString() ?? "",
                                  NoOfAdult = d["NoOfAdult"]?.ToString() ?? "",
                                  NoOfChild = d["NoOfChild"]?.ToString() ?? "",
                                  NoOfChild1 = d["NoOfChild1"]?.ToString() ?? "",
                                  NoOfChild2 = d["NoOfChild2"]?.ToString() ?? "",
                                  Prs = d["Prs"]?.ToString() ?? "",
                                  NoOfRoom = d["NoOfRoom"]?.ToString() ?? "",
                                  NoOfNight = d["NoOfNight"]?.ToString() ?? "",
                                  RoomType = d["RoomType"]?.ToString() ?? "",
                                  BusinessBlockCode = d["BusinessBlockCode"]?.ToString() ?? "",
                                  RateCode = d["RateCode"]?.ToString() ?? "",
                                  ResType = d["ResType"]?.ToString() ?? "",
                                  ETD = d["ETD"]?.ToString() ?? "",
                                  PaymentMethod = d["PaymentMethod"]?.ToString() ?? "",
                                  FolioBalance = d["FolioBalance"]?.ToString() ?? "",
                                  ShareRoomName = d["ShareRoomName"]?.ToString() ?? "",
                                  AccompanyName = d["AccompanyName"]?.ToString() ?? "",
                                  Balance = d["Balance"]?.ToString() ?? "",
                                  CurrencyMaster = d["CurrencyMaster"]?.ToString() ?? "",
                                  Comment = d["Comment"]?.ToString() ?? "",
                                  Packages = d["Packages"]?.ToString() ?? "",
                                  Specials = d["Specials"]?.ToString() ?? "",
                                  ItemInventory = d["ItemInventory"]?.ToString() ?? "",
                                  GroupCode = d["GroupCode"]?.ToString() ?? ""
                              }).ToList();


                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        [HttpGet]
        public IActionResult SummarybyArticle(DateTime fromDate, DateTime toDate, string transaction, string article, string cashierNo, string roomClass, string room, string orderBy, string netDisp, int isShowDeleted)
        {
            try
            {
                DataTable dt = _iReportService.SummarybyArticle(fromDate, toDate, transaction, article, cashierNo, roomClass, room, orderBy, netDisp, isShowDeleted);
                var result = (from d in dt.AsEnumerable()
                              select new
                              {
                                  TransactionCode = !string.IsNullOrEmpty(d["TransactionCode"].ToString()) ? d["TransactionCode"] : "",
                                  TrnsDescription = !string.IsNullOrEmpty(d["TrnsDescription"].ToString()) ? d["TrnsDescription"] : "",
                                  ArticleCode = !string.IsNullOrEmpty(d["ArticleCode"].ToString()) ? d["ArticleCode"] : "",
                                  ArticleDescription = !string.IsNullOrEmpty(d["ArticleDescription"].ToString()) ? d["ArticleDescription"] : "",
                                  Price = !string.IsNullOrEmpty(d["Price"].ToString()) ? d["Price"] : "",
                                  AmountBeforeTax = !string.IsNullOrEmpty(d["AmountBeforeTax"].ToString()) ? d["AmountBeforeTax"] : "",
                                  UserName = !string.IsNullOrEmpty(d["UserName"].ToString()) ? d["UserName"] : "",
                                  TransactionSubGroup = !string.IsNullOrEmpty(d["TransactionSubGroup"].ToString()) ? d["TransactionSubGroup"] : "",
                                  TransactionGroup = !string.IsNullOrEmpty(d["TransactionGroup"].ToString()) ? d["TransactionGroup"] : "",
                                  Quantity = !string.IsNullOrEmpty(d["Quantity"].ToString()) ? d["Quantity"] : "",
                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error generating Reservation Rate Code Check report: {ex.Message}");
            }
        }

        [HttpGet]
        public IActionResult JournalByCashierArticleData(DateTime fromDate, DateTime toDate, string transaction, string article, string cashier, string roomclass, string room, string viewBy, string netDisp)
        {
            transaction = transaction ?? "";
            article = article ?? "";
            cashier = cashier ?? "";
            roomclass = roomclass ?? "";
            room = room ?? "";
            viewBy = viewBy ?? "";
            netDisp = netDisp ?? "";
            try
            {
                DataTable dt = _iReportService.JournalByCashierArticleData(fromDate, toDate, transaction, article, cashier, roomclass, room, viewBy, netDisp);
                var result = (from d in dt.AsEnumerable()
                              select new
                              {
                                  TransactionDate = d["TransactionDate"].ToString() ?? "",
                                  RoomNo = d["RoomNo"].ToString() ?? "",
                                  AccountName = d["AccountName"].ToString() ?? "",
                                  TransactionCode = d["TransactionCode"].ToString() ?? "",
                                  TrnsDescription = d["TrnsDescription"].ToString() ?? "",
                                  ArticleCode = d["ArticleCode"].ToString() ?? "",
                                  ArticleDescription = d["ArticleDescription"].ToString() ?? "",
                                  InvoiceNo = d["InvoiceNo"].ToString() ?? "",
                                  Supplement = d.Table.Columns.Contains("Supplement")
                                 ? d["Supplement"].ToString() ?? ""
                                 : (d.Table.Columns.Contains("Expr1") ? d["Expr1"].ToString() ?? "" : ""),
                                  Reference = d["Reference"].ToString() ?? "",
                                  CurrencyMaster = d["CurrencyMaster"].ToString() ?? "",
                                  DebitAmount = d["DebitAmount"].ToString() ?? "",
                                  CreditAmount = d["CreditAmount"].ToString() ?? "",
                                  AmountMaster = d["AmountMaster"].ToString() ?? "",
                                  CashierNo = d["CashierNo"].ToString() ?? "",
                                  UserName = d["UserName"].ToString() ?? "",
                                  RoomClass = d["RoomClass"].ToString() ?? "",
                                  TransactionGroup = d["TransactionGroup"].ToString() ?? "",
                                  TransactionSubGroup = d["TransactionSubGroup"].ToString() ?? "",
                                  Price = d.Table.Columns.Contains("Price") ? d["Price"].ToString() ?? "" : "",
                                  AmountBeforeTax = d.Table.Columns.Contains("AmountBeforeTax") ? d["AmountBeforeTax"].ToString() ?? "" : "",
                                  Amount = d.Table.Columns.Contains("Amount") ? d["Amount"].ToString() ?? "" : "",
                                  TransDescription = d["TransDescription"].ToString() ?? ""
                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error generating Reservation Rate Code Check report: {ex.Message}");
            }
        }

        [HttpGet]
        public IActionResult ManagerReport(string businessDate, string currency, int recalc = 0)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(currency) || currency == "0") currency = "VND";
                DataTable dt = _iReportService.ManagerReport(ParseReportDate(businessDate), currency, recalc == 1);
                if (dt == null) return Json(Array.Empty<object>());
                var result = new List<Dictionary<string, object>>(dt.Rows.Count);
                foreach (DataRow d in dt.Rows)
                {
                    result.Add(new Dictionary<string, object>
                    {
                        ["code"] = ColStr(d, "Code"),
                        ["itemName"] = ColStr(d, "ItemName"),
                        ["currentDay"] = ColDec(d, "CurrentDay"),
                        ["currentMonth"] = ColDec(d, "CurrentMonth"),
                        ["currentYear"] = ColDec(d, "CurrentYear"),
                        ["lastDay"] = ColDec(d, "LastDay"),
                        ["lastMonth"] = ColDec(d, "LastMonth"),
                        ["lastYear"] = ColDec(d, "LastYear"),
                    });
                }
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new Dictionary<string, object> { ["error"] = ex.Message });
            }
        }
        [HttpGet]
        public IActionResult CashierSummary(DateTime dtpFromDate, string cboType)
        {
            try
            {
                DataTable dt = _iReportService.CashierSummary(dtpFromDate, cboType);
                if (dt == null) return Json(Array.Empty<object>());
                var result = new List<Dictionary<string, object>>(dt.Rows.Count);
                foreach (DataRow d in dt.Rows)
                {
                    var cashierNo = ColStr(d, "CashierNo");
                    result.Add(new Dictionary<string, object>
                    {
                        ["cashierGroup"] = cashierNo,
                        ["cashierNo"] = cashierNo,
                        ["userName"] = ColStr(d, "UserName"),
                        ["loginTime"] = FirstCol(d, "LoginTime"),
                        ["logoutTime"] = FirstCol(d, "LogoutTime"),
                        ["transactionCode"] = ColStr(d, "TransactionCode"),
                        ["description"] = ColStr(d, "Description"),
                        ["currencyID"] = ColStr(d, "CurrencyID"),
                        ["amount"] = ColDec(d, "Amount"),
                        ["exch"] = ColDec(d, FirstExisting(d, "Exch", "ExchangeRate", "ExchRate")),
                        ["amountMaster"] = ColDec(d, "AmountMaster"),
                    });
                }
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new Dictionary<string, object> { ["error"] = ex.Message });
            }
        }
        [HttpGet]
        public IActionResult DailyRevenueReportNew(string dateView)
        {
            try
            {
                DataTable dt = _iReportService.DailyRevenueReportNew(ParseReportDate(dateView));
                var result = (from d in dt.AsEnumerable()
                              select new
                              {
                                  GroupName = !string.IsNullOrEmpty(d["GroupName"].ToString()) ? d["GroupName"] : "",
                                  SubGroup = !string.IsNullOrEmpty(d["SubGroup"].ToString()) ? d["SubGroup"] : "",
                                  GroupID = !string.IsNullOrEmpty(d["GroupID"].ToString()) ? d["GroupID"] : "",
                                  ItemName = !string.IsNullOrEmpty(d["ItemName"].ToString()) ? d["ItemName"] : "",
                                  Daily_Value = !string.IsNullOrEmpty(d["Daily_Value"].ToString()) ? d["Daily_Value"] : "",
                                  M_Actual = !string.IsNullOrEmpty(d["M_Actual"].ToString()) ? d["M_Actual"] : "",
                                  M_Budget = !string.IsNullOrEmpty(d["M_Budget"].ToString()) ? d["M_Budget"] : "",
                                  M_LastYear = !string.IsNullOrEmpty(d["M_LastYear"].ToString()) ? d["M_LastYear"] : "",
                                  Y_Actual = !string.IsNullOrEmpty(d["Y_Actual"].ToString()) ? d["Y_Actual"] : "",
                                  Y_Budget = !string.IsNullOrEmpty(d["Y_Budget"].ToString()) ? d["Y_Budget"] : "",
                                  Y_LastYear = !string.IsNullOrEmpty(d["Y_LastYear"].ToString()) ? d["Y_LastYear"] : "",
                                  Budget_Month = !string.IsNullOrEmpty(d["Budget_Month"].ToString()) ? d["Budget_Month"] : "",
                                  Budget_Month_Per = !string.IsNullOrEmpty(d["Budget_Month_Per"].ToString()) ? d["Budget_Month_Per"] : "",
                                  Budget_Year = !string.IsNullOrEmpty(d["Budget_Year"].ToString()) ? d["Budget_Year"] : "",
                                  Budget_Year_Per = !string.IsNullOrEmpty(d["Budget_Year_Per"].ToString()) ? d["Budget_Year_Per"] : "",
                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
        [HttpGet]
        public IActionResult DailyRevenueReportsV2(string dateView)
        {
            try
            {
                DataTable dt = _iReportService.DailyRevenueReportsV2(ParseReportDate(dateView));
                if (dt == null) return Json(Array.Empty<object>());
                var result = (from d in dt.AsEnumerable()
                              select new
                              {
                                  GroupName = !string.IsNullOrEmpty(d["GroupName"].ToString()) ? d["GroupName"] : "",
                                  SubGroup = !string.IsNullOrEmpty(d["SubGroup"].ToString()) ? d["SubGroup"] : "",
                                  GroupID = !string.IsNullOrEmpty(d["GroupID"].ToString()) ? d["GroupID"] : "",
                                  ItemName = !string.IsNullOrEmpty(d["ItemName"].ToString()) ? d["ItemName"] : "",
                                  Daily_Value = !string.IsNullOrEmpty(d["Daily_Value"].ToString()) ? d["Daily_Value"] : "",
                                  SVC = !string.IsNullOrEmpty(d["SVC"].ToString()) ? d["SVC"] : "",
                                  VAT = !string.IsNullOrEmpty(d["VAT"].ToString()) ? d["VAT"] : "",
                                  VATO = !string.IsNullOrEmpty(d["VATO"].ToString()) ? d["VATO"] : "",
                                  Total = !string.IsNullOrEmpty(d["Total"].ToString()) ? d["Total"] : "",                                 
                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

      
        [HttpGet]
        public IActionResult CashierAudit(string date, string cashierList, string transactionCodeList, string type,
            string user, string transactions)
        {
            try
            {
                cashierList = string.IsNullOrWhiteSpace(cashierList) ? (user ?? "") : cashierList;
                transactionCodeList = string.IsNullOrWhiteSpace(transactionCodeList) ? (transactions ?? "") : transactionCodeList;
                type = string.IsNullOrWhiteSpace(type) ? "0" : type;
                DataTable dt = _iReportService.CashierAudit(ParseReportDate(date), cashierList, transactionCodeList, type);
                if (dt == null) return Json(Array.Empty<object>());
                var result = new List<Dictionary<string, object>>(dt.Rows.Count);
                foreach (DataRow d in dt.Rows)
                {
                    result.Add(new Dictionary<string, object>
                    {
                        ["pay"] = ColStr(d, "*"),
                        ["confirmationNo"] = ColStr(d, "ConfirmationNo"),
                        ["userName"] = ColStr(d, "UserName"),
                        ["folioID"] = ColStr(d, "FolioID"),
                        ["roomNo"] = ColStr(d, "RoomNo"),
                        ["account"] = ColStr(d, "Account"),
                        ["reference"] = ColStr(d, "Reference"),
                        ["supplement"] = ColStr(d, "Supplement"),
                        ["transactionDate"] = FirstCol(d, "TransactionDate"),
                        ["transactionCode"] = ColStr(d, "TransactionCode"),
                        ["description"] = ColStr(d, "Description"),
                        ["amount"] = ColDec(d, "Amount"),
                        ["currencyID"] = ColStr(d, "CurrencyID"),
                        ["cashierNo"] = ColStr(d, "CashierNo"),
                    });
                }
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
        [HttpGet]
        public IActionResult RevenueSpa(DateTime fromDate, DateTime toDate)
        {
            try
            {

                var data = _iReportService.RevenueSpa(fromDate, toDate);
                var result = (from d in data.AsEnumerable()
                              select new
                              {
                                  TransactionDate = !string.IsNullOrEmpty(d["TransactionDate"].ToString()) ? d["TransactionDate"] : "",
                                  Code = !string.IsNullOrEmpty(d["Code"].ToString()) ? d["Code"] : "",
                                  TransactionName = !string.IsNullOrEmpty(d["TransactionName"].ToString()) ? d["TransactionName"] : "",
                                  CurrencyID = !string.IsNullOrEmpty(d["CurrencyID"].ToString()) ? d["CurrencyID"] : "",
                                  AmountBeForeTax = decimal.TryParse(d["AmountBeForeTax"]?.ToString(), out var amtBeforeTax) ? amtBeforeTax : 0,
                                  AmountGross = decimal.TryParse(d["AmountGross"]?.ToString(), out var amtGross) ? amtGross : 0,
                                  UserName = !string.IsNullOrEmpty(d["UserName"].ToString()) ? d["UserName"] : "",                          
                              }).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {

                return Json(new { error = ex.Message });
            }
        }
        [HttpGet]
        public IActionResult StatisticRoomType(DateTime fromDate, DateTime toDate, string roomTypeCsv)
        {

            try
            {
                roomTypeCsv = roomTypeCsv ?? "";
                var data = _iReportService.StatisticRoomType(fromDate, toDate, roomTypeCsv);
                var result = (from d in data.AsEnumerable()
                              select new
                              {
                                  Date = !string.IsNullOrEmpty(d["Date"].ToString()) ? d["Date"] : "",
                                  RoomType = !string.IsNullOrEmpty(d["Room Type"].ToString()) ? d["Room Type"] : "",
                                  Description = !string.IsNullOrEmpty(d["Description"].ToString()) ? d["Description"] : "",
                                  PotentialRms = !string.IsNullOrEmpty(d["Potential Rms"].ToString()) ? d["Potential Rms"] : "",
                                  RmsOcc = !string.IsNullOrEmpty(d["Rms Occ"].ToString()) ? d["Rms Occ"] : "",
                                  Prs = !string.IsNullOrEmpty(d["Prs"].ToString()) ? d["Prs"] : "",                               
                                  RoomRevenue = !string.IsNullOrEmpty(d["Room Revenue"].ToString()) ? d["Room Revenue"] : "",
                              }).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {

                return Json(new { error = ex.Message });
            }
        }
        [HttpGet]
        public IActionResult RoomStatistic(string year, string fromMonth, string toMonth, string fromRoom, string toRoom)
        {

            try
            {        
                var data = _iReportService.RoomStatistic(year, fromMonth, toMonth, fromRoom, toRoom);
                var result = (from d in data.AsEnumerable()
                              select new
                              {
                                  RoomNo = !string.IsNullOrEmpty(d["Room No"].ToString()) ? d["Room No"] : "",
                                  Month = !string.IsNullOrEmpty(d["Month"].ToString()) ? d["Month"] : "",
                                  RoomArrivals = !string.IsNullOrEmpty(d["Room Arrivals"].ToString()) ? d["Room Arrivals"] : "",
                                  RoomNights = !string.IsNullOrEmpty(d["Room Nights"].ToString()) ? d["Room Nights"] : "",
                                  BedNights = !string.IsNullOrEmpty(d["Bed Nights"].ToString()) ? d["Bed Nights"] : "",
                                  RoomRevenue = !string.IsNullOrEmpty(d["Room Revenue"].ToString()) ? d["Room Revenue"] : "",
                                  ADR = !string.IsNullOrEmpty(d["ADR"].ToString()) ? d["ADR"] : "",
                              }).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {

                return Json(new { error = ex.Message });
            }
        }
        [HttpGet]
        public IActionResult GuestTrialBalance(string date, int isRouting, int isCheckOut, string roomTypeId)
        {
            try
            {
                var data = _iReportService.GuestTrialBalance(ParseReportDate(date), isRouting, isCheckOut, roomTypeId ?? "");
                if (data == null) return Json(Array.Empty<object>());
                var result = new List<Dictionary<string, object>>(data.Rows.Count);
                foreach (DataRow d in data.Rows)
                {
                    result.Add(new Dictionary<string, object>
                    {
                        ["reservation"] = ColStr(d, "Reservation"),
                        ["accountName"] = ColStr(d, "AccountName"),
                        ["arrivalDate"] = FirstCol(d, "ArrivalDate"),
                        ["departureDate"] = FirstCol(d, "DepartureDate"),
                        ["status"] = ColStr(d, "Status"),
                        ["openBalance"] = ColDec(d, "OpenBalance"),
                        ["debit"] = ColDec(d, "Debit"),
                        ["debitTranfer"] = ColDec(d, "DebitTranfer"),
                        ["credit"] = ColDec(d, "Credit"),
                        ["creditTranfer"] = ColDec(d, "CreditTranfer"),
                        ["closeBalance"] = ColDec(d, "CloseBalance"),
                        ["currency"] = ColStr(d, "Currency"),
                        ["room"] = ColStr(d, "Room"),
                        ["folioID"] = ColStr(d, "FolioID"),
                    });
                }
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }




        [HttpGet]
        public IActionResult ArrivalsandCheckInTodayData(string roomClass ,string roomtype, string paymethod,string vip,string viewBy,string pseudo,string chkviponly,int disRoomSharer,string nopost)
        {
            try
            {
                roomClass = roomClass ?? "";
                roomtype = roomtype ?? "";
                paymethod = paymethod ?? "";
                vip = vip ?? "";
                viewBy = viewBy ?? "";
                pseudo = pseudo ?? "";
                chkviponly = chkviponly ?? "";
                nopost = nopost ?? "";
                DataTable dataTable = _iReportService.ArrivalsandCheckInTodayData(roomClass, roomtype, paymethod, vip, viewBy, pseudo, chkviponly, disRoomSharer, nopost);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  RoomNo = !string.IsNullOrEmpty(d["RoomNo"].ToString()) ? d["RoomNo"] : "",
                                  Name = !string.IsNullOrEmpty(d["Name"].ToString()) ? d["Name"] : "",
                                  VIP = !string.IsNullOrEmpty(d["VIP"].ToString()) ? d["VIP"] : "",
                                  ReservationStatus = !string.IsNullOrEmpty(d["ReservationStatus"].ToString()) ? d["ReservationStatus"] : "",
                                  Company = !string.IsNullOrEmpty(d["Company"].ToString()) ? d["Company"] : "",
                                  Agent = !string.IsNullOrEmpty(d["Agent"].ToString()) ? d["Agent"] :"",
                                  BusinessBlockCode = !string.IsNullOrEmpty(d["BusinessBlockCode"].ToString()) ? d["BusinessBlockCode"] : "",
                                  ArrivalDate = !string.IsNullOrEmpty(d["ArrivalDate"].ToString()) ? d["ArrivalDate"] : "",
                                  ETA = !string.IsNullOrEmpty(d["ETA"].ToString()) ? d["ETA"] : "",
                                  ETD = !string.IsNullOrEmpty(d["ETD"].ToString()) ? d["ETD"] : "",
                                  DepartureDate = !string.IsNullOrEmpty(d["DepartureDate"].ToString()) ? d["DepartureDate"] : "",
                                  RoomType = !string.IsNullOrEmpty(d["RoomType"].ToString()) ? d["RoomType"] : "",
                                  NoOfAdult = !string.IsNullOrEmpty(d["NoOfAdult"].ToString()) ? d["NoOfAdult"] : "",
                                  NoOfChild = !string.IsNullOrEmpty(d["NoOfChild"].ToString()) ? d["NoOfChild"] : "",
                                  NoOfChild1 = !string.IsNullOrEmpty(d["NoOfChild1"].ToString()) ? d["NoOfChild1"] : "",
                                  NoOfChild2 = !string.IsNullOrEmpty(d["NoOfChild2"].ToString()) ? d["NoOfChild2"] : "",
                                  RoomClassCode = !string.IsNullOrEmpty(d["RoomClassCode"].ToString()) ? d["RoomClassCode"] : "",

                                  NoOfRoom = !string.IsNullOrEmpty(d["NoOfRoom"].ToString()) ? d["NoOfRoom"] : "",
                                  PaymentMethod = !string.IsNullOrEmpty(d["PaymentMethod"].ToString()) ? d["PaymentMethod"] : "",
                                  RateCode = !string.IsNullOrEmpty(d["RateCode"].ToString()) ? d["RateCode"] : "",
                                  Rate = !string.IsNullOrEmpty(d["Rate"].ToString()) ? d["Rate"] : "",
                                  CurrencyID = !string.IsNullOrEmpty(d["CurrencyID"].ToString()) ? d["CurrencyID"] : "",

                                  Comment = !string.IsNullOrEmpty(d["Comment"].ToString()) ? d["Comment"] : "",
                                  ItemInventory = !string.IsNullOrEmpty(d["ItemInventory"].ToString()) ? d["ItemInventory"] : "",
                                  Specials = !string.IsNullOrEmpty(d["Specials"].ToString()) ? d["Specials"] : "",
                              }).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }




        [HttpGet]
        public IActionResult LeadtimeReportsData(DateTime fromDate, string zone, string typeDate)
        {
            try
            {
                zone = zone ?? "";
                DateTime toDate = fromDate.AddDays(30); // Tự động tính ngày kết thúc

                List<string> columnNames = new List<string>();
                List<string> isnullExpressions = new List<string>();

                if (typeDate == "1")
                {
                    // Dạng từng ngày [dM]
                    for (int i = 0; i < 31; i++)
                    {
                        DateTime currentDate = fromDate.AddDays(i);
                        string day = currentDate.Day.ToString();       // Không format "00" để tránh lỗi 017
                        string month = currentDate.Month.ToString();   // Không thêm số 0

                        string column = $"[{day}{month}]";
                        string expression = $"'{day}' = ISNULL({column}, 0)";

                        columnNames.Add(column);
                        isnullExpressions.Add(expression);
                    }
                }
                else if (typeDate == "0")
                {
                    // Dạng theo tháng [M]
                    for (int i = 0; i < 12; i++)
                    {
                        DateTime currentMonth = fromDate.AddMonths(i);
                        int month = currentMonth.Month;

                        string column = $"[{month}]";
                        string expression = $"'{i + 1}' = ISNULL({column}, 0)";

                        columnNames.Add(column);
                        isnullExpressions.Add(expression);
                    }
                }

                string columnsString = string.Join(",", columnNames);
                string expressionString = string.Join(",", isnullExpressions);

                // Gọi service
                DataTable dataTable = _iReportService.LeadtimeReportsData(
                    fromDate, toDate, zone, typeDate, columnsString, expressionString
                );

                // Xử lý dữ liệu trả về
                var result = dataTable.AsEnumerable().Select(d =>
                {
                    var item = new Dictionary<string, object>
                    {
                        ["MarketCode"] = d["MarketCode"]?.ToString() ?? "",
                        ["MarketName"] = d["MarketName"]?.ToString() ?? "",
                        ["MarketType"] = d["MarketType"]?.ToString() ?? ""
                    };

                    // Thêm các cột động
                    var dynamicColumns = dataTable.Columns.Cast<DataColumn>()
                        .Select(c => c.ColumnName)
                        .Where(c => c != "MarketCode" && c != "MarketName" && c != "MarketType")
                        .Distinct();

                    // Xác định giá trị đầu tiên từ dynamicColumns (sau khi loại bỏ [] nếu có)
                    string firstCol = dynamicColumns.FirstOrDefault();
                    string firstKey = firstCol?.Trim('[', ']');  // bỏ [] nếu có

                    foreach (var col in dynamicColumns)
                    {
                        var value = d[col];
                        if (value != DBNull.Value)
                        {
                            string rawKey = col.StartsWith("[") ? col.Trim('[', ']') : col;
                            string key = rawKey;

                            // Nếu key dài đúng 3 ký tự và toàn số, thì thay bằng giá trị đầu tiên + "_"
                            if (rawKey.Length == 3 && rawKey.All(char.IsDigit))
                            {
                                key = $"{firstKey}_";
                            }

                            // Nếu key đã tồn tại thì thêm "_" cho đến khi không trùng
                            while (item.ContainsKey(key))
                            {
                                key += "_";
                            }

                            item[key] = value;
                        }
                    }



                    return item;
                }).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }



        [HttpGet]
        public IActionResult RatecodeReportsData(DateTime fromDate, string zone,string rate,string viewBy)
        {
            try
            {
                zone = zone ?? "";
                rate = rate ?? "";
                DateTime toDate = fromDate.AddMonths(11); // Tự động tính ngày kết thúc

                List<string> columnNames = new List<string>();
                List<string> isnullExpressions = new List<string>();

                    // Dạng theo tháng [M]
                    for (int i = 0; i < 12; i++)
                    {
                        DateTime currentMonth = fromDate.AddMonths(i);
                        int month = currentMonth.Month;

                        string column = $"[{month}]";
                        string expression = $"'{i + 1}' = ISNULL({column}, 0)";

                        columnNames.Add(column);
                        isnullExpressions.Add(expression);
                    }
                

                string columnsString = string.Join(",", columnNames);
                string expressionString = string.Join(",", isnullExpressions);

                // Gọi service
                DataTable dataTable = _iReportService.RatecodeReportsData(fromDate, toDate, zone, rate, viewBy, columnsString, expressionString);

                // Xử lý dữ liệu trả về
                var result = dataTable.AsEnumerable().Select(d =>
                {
                    var item = new Dictionary<string, object>
                    {
                        ["RateCode"] = d["RateCode"]?.ToString() ?? "",
                        ["Row"] = d["Row"]?.ToString() ?? "",
                    };

                    // Thêm các cột động
                    var dynamicColumns = dataTable.Columns.Cast<DataColumn>()
                        .Select(c => c.ColumnName)
                        .Where(c => c != "RateCode" && c != "Row")
                        .Distinct();

                    // Xác định giá trị đầu tiên từ dynamicColumns (sau khi loại bỏ [] nếu có)
                    string firstCol = dynamicColumns.FirstOrDefault();
                    string firstKey = firstCol?.Trim('[', ']');  // bỏ [] nếu có

                    foreach (var col in dynamicColumns)
                    {
                        var value = d[col];
                        if (value != DBNull.Value)
                        {
                            string rawKey = col.StartsWith("[") ? col.Trim('[', ']') : col;
                            string key = rawKey;

                            // Nếu key dài đúng 3 ký tự và toàn số, thì thay bằng giá trị đầu tiên + "_"
                            if (rawKey.Length == 3 && rawKey.All(char.IsDigit))
                            {
                                key = $"{firstKey}_";
                            }

                            // Nếu key đã tồn tại thì thêm "_" cho đến khi không trùng
                            while (item.ContainsKey(key))
                            {
                                key += "_";
                            }

                            item[key] = value;
                        }
                    }



                    return item;
                }).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult AnnualRoomOccupancyData(DateTime fromDate, string zone)
        {
            try
            {
                zone = zone ?? "";
                DateTime toDate = fromDate.AddMonths(11); // Tự động tính ngày kết thúc

                List<string> columnNames = new List<string>();
                List<string> isnullExpressions = new List<string>();

                // Dạng theo tháng [M]
                for (int i = 0; i < 12; i++)
                {
                    DateTime currentMonth = fromDate.AddMonths(i);
                    int month = currentMonth.Month;

                    string column = $"[{month}]";
                    string expression = $"'{i + 1}' = ISNULL({column}, 0)";

                    columnNames.Add(column);
                    isnullExpressions.Add(expression);
                }


                string columnsString = string.Join(",", columnNames);
                string expressionString = string.Join(",", isnullExpressions);

                // Gọi service
                DataTable dataTable = _iReportService.AnnualRoomOccupancyData(fromDate, toDate, zone,columnsString, expressionString);

                // Xử lý dữ liệu trả về
                var result = dataTable.AsEnumerable().Select(d =>
                {
                    var item = new Dictionary<string, object>
                    {
                        ["day"] = d["day"]?.ToString() ?? "",
                    };

                    // Thêm các cột động
                    var dynamicColumns = dataTable.Columns.Cast<DataColumn>()
                        .Select(c => c.ColumnName)
                        .Where(c => c != "day")
                        .Distinct();

                    // Xác định giá trị đầu tiên từ dynamicColumns (sau khi loại bỏ [] nếu có)
                    string firstCol = dynamicColumns.FirstOrDefault();
                    string firstKey = firstCol?.Trim('[', ']');  // bỏ [] nếu có

                    foreach (var col in dynamicColumns)
                    {
                        var value = d[col];
                        if (value != DBNull.Value)
                        {
                            string rawKey = col.StartsWith("[") ? col.Trim('[', ']') : col;
                            string key = rawKey;


                            item[key] = value;
                        }
                    }



                    return item;
                }).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        private IActionResult JsonTable(DataTable table) => Json(TableRows(table));

        private static List<Dictionary<string, object>> TableRows(DataTable table)
        {
            var rows = new List<Dictionary<string, object>>();
            if (table != null)
            {
                foreach (DataRow r in table.Rows)
                {
                    var d = new Dictionary<string, object>();
                    foreach (DataColumn c in table.Columns)
                    {
                        var v = r[c];
                        d[c.ColumnName] = v == DBNull.Value || v == null ? "" : v;
                    }
                    rows.Add(d);
                }
            }
            return rows;
        }

        private static DateTime NzDate(DateTime d) => d == default ? DateTime.Today : d;

        private static string ColStr(DataRow d, string col)
        {
            if (d == null || d.Table == null || !d.Table.Columns.Contains(col) || d[col] == DBNull.Value) return "";
            return Convert.ToString(d[col]) ?? "";
        }

        private static decimal ColDec(DataRow d, string col)
        {
            if (d == null || d.Table == null || string.IsNullOrEmpty(col) || !d.Table.Columns.Contains(col) || d[col] == DBNull.Value)
                return 0m;
            try { return Convert.ToDecimal(d[col]); }
            catch { return 0m; }
        }

        private static string ColDate(DataRow d, string col)
        {
            if (d == null || d.Table == null || string.IsNullOrEmpty(col) || !d.Table.Columns.Contains(col) || d[col] == DBNull.Value) return "";
            if (d[col] is DateTime dt)
            {
                if (dt.Year <= 1900) return "";
                return dt.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
            }
            if (DateTime.TryParse(Convert.ToString(d[col]), out var parsed))
            {
                if (parsed.Year <= 1900) return "";
                return parsed.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
            }
            return Convert.ToString(d[col]) ?? "";
        }

        private static string ColDateTime(DataRow d, string col)
        {
            if (d == null || d.Table == null || string.IsNullOrEmpty(col) || !d.Table.Columns.Contains(col) || d[col] == DBNull.Value) return "";
            if (d[col] is DateTime dt)
            {
                if (dt.Year <= 1900) return "";
                return dt.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
            }
            if (DateTime.TryParse(Convert.ToString(d[col]), out var parsed))
            {
                if (parsed.Year <= 1900) return "";
                return parsed.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
            }
            return Convert.ToString(d[col]) ?? "";
        }

        private static string FirstExisting(DataRow d, params string[] cols)
        {
            if (d?.Table == null || cols == null) return "";
            foreach (var c in cols)
                if (!string.IsNullOrEmpty(c) && d.Table.Columns.Contains(c)) return c;
            return "";
        }

        private static string FirstCol(DataRow d, params string[] cols) => ColStr(d, FirstExisting(d, cols));

        private static string CsvIds(string raw, int maxLen = 100)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";
            var parts = raw.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => p.Length > 0 && p.All(char.IsDigit));
            var sb = new StringBuilder();
            foreach (var p in parts)
            {
                var next = sb.Length == 0 ? p : "," + p;
                if (sb.Length + next.Length > maxLen) break;
                sb.Append(next);
            }
            return sb.ToString();
        }

        private static string JoinCsvCodes(IEnumerable<string> codes, int maxLen)
        {
            if (codes == null) return "";
            var sb = new StringBuilder();
            foreach (var raw in codes)
            {
                var p = (raw ?? "").Trim();
                if (p.Length == 0) continue;
                var next = sb.Length == 0 ? p : "," + p;
                if (sb.Length + next.Length > maxLen) break;
                sb.Append(next);
            }
            return sb.ToString();
        }

        private static DateTime ParseReportDate(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return DateTime.Today;
            raw = raw.Trim();
            if (DateTime.TryParseExact(raw, new[] { "yyyy-MM-dd", "dd/MM/yyyy", "dd/MM/yy", "yyyy-MM-ddTHH:mm:ss" },
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                return d.Date;
            if (DateTime.TryParse(raw, CultureInfo.GetCultureInfo("vi-VN"), DateTimeStyles.None, out d))
                return d.Date;
            if (DateTime.TryParse(raw, out d)) return d.Date;
            return DateTime.Today;
        }

        [HttpGet]
        public IActionResult GuestBirthdayReportData(DateTime fromDate, DateTime toDate, string reservationStatus, string orderBy)
        {
            try { return JsonTable(_iReportService.GuestBirthdayReportData(NzDate(fromDate), NzDate(toDate), reservationStatus, orderBy)); }
            catch (Exception ex) { return Json(ex.Message); }
        }

        [HttpGet]
        public IActionResult GuestInHousebyCompanyData(string roomClass, string roomType, string companyId, string pseudo, string sortOrder)
        {
            try { return JsonTable(_iReportService.GuestInHousebyCompanyData(roomClass, roomType, companyId, pseudo, sortOrder)); }
            catch (Exception ex) { return Json(ex.Message); }
        }

        [HttpGet]
        public IActionResult GuestInHousebyGroupData(DateTime fromDate, DateTime toDate, string roomClass, string blockCode, string vipOnly, string printRate, string sortOrder)
        {
            try { return JsonTable(_iReportService.GuestInHousebyGroupData(NzDate(fromDate), NzDate(toDate), roomClass, blockCode, vipOnly, printRate, sortOrder)); }
            catch (Exception ex) { return Json(ex.Message); }
        }

        [HttpGet]
        public IActionResult GuestInHousebyRoomData(string roomClass, string roomType, string payment, string pseudo, string vip, string vipOnly, string individual, string blockCode, int disRoomSharer, string sortOrder, string noPost)
        {
            try { return JsonTable(_iReportService.GuestInHousebyRoomData(roomClass, roomType, payment, pseudo, vip, vipOnly, individual, blockCode, disRoomSharer, sortOrder, noPost)); }
            catch (Exception ex) { return Json(ex.Message); }
        }

        [HttpGet]
        public IActionResult GuestInHouseCashPaymentMethodData(DateTime fromDate)
        {
            try { return JsonTable(_iReportService.GuestInHouseCashPaymentMethodData(NzDate(fromDate))); }
            catch (Exception ex) { return Json(ex.Message); }
        }

        [HttpGet]
        public IActionResult GuestInHouseSpecialRequestsData(DateTime fromDate, DateTime toDate, DateTime? departure, int sortOrder, string special)
        {
            try { return JsonTable(_iReportService.GuestInHouseSpecialRequestsData(NzDate(fromDate), NzDate(toDate), departure, sortOrder, special)); }
            catch (Exception ex) { return Json(ex.Message); }
        }

        [HttpGet]
        public IActionResult MessagesGuestsINHAndDueArrivalData(string receiveStatus)
        {
            try { return JsonTable(_iReportService.MessagesGuestsINHAndDueArrivalData(receiveStatus)); }
            catch (Exception ex) { return Json(ex.Message); }
        }

        [HttpGet]
        public IActionResult GuestInHouseComplimentaryAndHouseUseData(bool vipOnly, string vipIds, bool showComp, bool showHU, bool displayComment, string sort)
        {
            try { return JsonTable(_iReportService.GuestInHouseComplimentaryAndHouseUseData(vipOnly, vipIds, showComp, showHU, displayComment, sort)); }
            catch (Exception ex) { return Json(ex.Message); }
        }

        [HttpGet]
        public IActionResult GIHComplimentaryData()
        {
            try { return JsonTable(_iReportService.GIHComplimentaryData()); }
            catch (Exception ex) { return Json(ex.Message); }
        }

        [HttpGet]
        public IActionResult HouseKeepingStatusData(string roomClassId, string roomTypeId, string blockCode, DateTime fromDate, DateTime toDate, string orderBy, bool arrivals, bool arrived, bool stayOver, bool dueOut, bool departed, int vipId)
        {
            try { return JsonTable(_iReportService.HouseKeepingStatusData(roomClassId, roomTypeId, blockCode, NzDate(fromDate), NzDate(toDate), orderBy, arrivals, arrived, stayOver, dueOut, departed, vipId)); }
            catch (Exception ex) { return Json(ex.Message); }
        }

        [HttpGet]
        public IActionResult OutOfOrderByPersonData(DateTime fromDate, DateTime toDate, string roomType, string zone, string orderBy, int oooStatus)
        {
            try { return JsonTable(_iReportService.OutOfOrderByPersonData(NzDate(fromDate), NzDate(toDate), roomType, zone, orderBy, oooStatus)); }
            catch (Exception ex) { return Json(ex.Message); }
        }

        [HttpGet]
        public IActionResult DialingInformationData(DateTime fromDate)
        {
            try { return JsonTable(_iReportService.DialingInformationData(NzDate(fromDate))); }
            catch (Exception ex) { return Json(ex.Message); }
        }

        [HttpGet]
        public IActionResult ProfileAddressData(string profileType, string nameStart, string vipOnly)
        {
            try { return JsonTable(_iReportService.ProfileAddressData(profileType, nameStart, vipOnly)); }
            catch (Exception ex) { return Json(ex.Message); }
        }

        [HttpGet]
        public IActionResult ProfileProductivityData(DateTime fromDate, DateTime toDate, string name)
        {
            try { return JsonTable(_iReportService.ProfileProductivityData(NzDate(fromDate), NzDate(toDate), name)); }
            catch (Exception ex) { return Json(ex.Message); }
        }

        [HttpGet]
        public IActionResult ComplimentaryGuestReportData(DateTime fromDate)
        {
            try { return JsonTable(_iReportService.ComplimentaryGuestReportData(NzDate(fromDate))); }
            catch (Exception ex) { return Json(ex.Message); }
        }

        [HttpGet]
        public IActionResult PaidOutReportData(DateTime fromDate, DateTime toDate)
        {
            try { return JsonTable(_iReportService.PaidOutReportData(NzDate(fromDate), NzDate(toDate))); }
            catch (Exception ex) { return Json(ex.Message); }
        }

        [HttpGet]
        public IActionResult HistoryAndForecastData(DateTime fromDate, DateTime toDate, string zone)
        {
            try { return JsonTable(_iReportService.HistoryAndForecastData(NzDate(fromDate), NzDate(toDate), zone)); }
            catch (Exception ex) { return Json(ex.Message); }
        }

        [HttpGet]
        public IActionResult MarketSegmentDetailData(DateTime fromDate, DateTime toDate, string zone)
        {
            try { return JsonTable(_iReportService.MarketSegmentDetailData(NzDate(fromDate), NzDate(toDate), zone)); }
            catch (Exception ex) { return Json(ex.Message); }
        }

        [HttpGet]
        public IActionResult GroupBalanceSearch(string confirmationNo)
        {
            try { return JsonTable(_iReportService.GroupBalanceSearch(confirmationNo)); }
            catch (Exception ex) { return Json(ex.Message); }
        }

        [HttpGet]
        public IActionResult GroupBalanceDetail(string confirmationNo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(confirmationNo))
                    return Json("Confirmation No dose not exits.");
                var table = _iReportService.GroupBalanceDetail(confirmationNo);
                var sum = _iReportService.GroupBalanceSummary(confirmationNo);
                return Json(new
                {
                    rows = TableRows(table),
                    holder = sum.Holder,
                    groupName = sum.GroupName,
                    totalRooms = sum.TotalRooms,
                    inHouse = sum.InHouse
                });
            }
            catch (Exception ex) { return Json(ex.Message); }
        }

        [HttpGet]
        public IActionResult ProfileProductionStatisticsData(DateTime fromDate, DateTime toDate, int profileType, int profileId, string roomClassId, string currencyId, bool includeTax, bool rollupMaster, string metrics)
        {
            try
            {
                return JsonTable(_iReportService.ProfileProductionStatisticsData(
                    NzDate(fromDate), NzDate(toDate), profileType, profileId, roomClassId, currencyId, includeTax, rollupMaster, metrics));
            }
            catch (Exception ex) { return Json(ex.Message); }
        }

        [HttpGet]
        public IActionResult BookingbyHolderData(DateTime date, string holder, string market)
        {
            try { return JsonTable(_iReportService.BookingbyHolderData(NzDate(date), holder, market)); }
            catch (Exception ex) { return Json(ex.Message); }
        }

        [HttpGet]
        public IActionResult ReportHolderLookup()
        {
            try { return JsonTable(_iReportService.ReportHolderLookup()); }
            catch (Exception ex) { return Json(ex.Message); }
        }

        [HttpGet]
        [RequireFunction("rptAgingbyTypeOnly")]
        public IActionResult AgingbyTypeOnlyData(DateTime date, int accType, string sort)
        {
            try { return JsonTable(_iReportService.AgingbyTypeOnlyData(NzDate(date), accType, sort ?? "")); }
            catch (Exception ex) { return Json(ex.Message); }
        }

        [HttpGet]
        [RequireFunction("rptAgingSummaryByType")]
        public IActionResult AgingSummaryByTypeData(string date, string sort)
        {
            try { return JsonTable(_iReportService.AgingSummaryByTypeData(ParseReportDate(date), sort ?? "")); }
            catch (Exception ex) { return Json(ex.Message); }
        }

        [HttpGet]
        [RequireFunction("rptARAdjustment")]
        public IActionResult ARAdjustmentData(string fromDate, string toDate)
        {
            try { return JsonTable(_iReportService.ARAdjustmentData(ParseReportDate(fromDate), ParseReportDate(toDate))); }
            catch (Exception ex) { return Json(ex.Message); }
        }

        [HttpGet]
        [RequireFunction("rptARCreditLimit")]
        public IActionResult ARCreditLimitData(int option1, int option2, int sort)
        {
            try { return JsonTable(_iReportService.ARCreditLimitData(option1, option2, sort)); }
            catch (Exception ex) { return Json(ex.Message); }
        }

        [HttpGet]
        [RequireFunction("rptARLedger")]
        public IActionResult ARLedgerData(DateTime date)
        {
            try { return JsonTable(_iReportService.ARLedgerData(NzDate(date))); }
            catch (Exception ex) { return Json(ex.Message); }
        }

        [HttpGet]
        [RequireFunction("rptARPayments")]
        public IActionResult ARPaymentsData(DateTime date)
        {
            try { return JsonTable(_iReportService.ARPaymentsData(NzDate(date))); }
            catch (Exception ex) { return Json(ex.Message); }
        }

        [HttpGet]
        [RequireFunction("rptARTransferReport")]
        public IActionResult ARTransferReportData(string date)
        {
            try { return JsonTable(_iReportService.ARTransferReportData(ParseReportDate(date))); }
            catch (Exception ex) { return Json(ex.Message); }
        }

        [HttpGet]
        [RequireFunction("rpt_Spr_ProfileUpdate")]
        public IActionResult ProfileUpdateReportData(string date, string confirmationNo, string roomNo, string name)
        {
            try
            {
                var table = _iReportService.ProfileUpdateReportData(ParseReportDate(date), confirmationNo ?? "", roomNo ?? "", name ?? "");
                var result = new List<Dictionary<string, object>>();
                if (table != null)
                {
                    foreach (DataRow d in table.Rows)
                    {
                        result.Add(new Dictionary<string, object>
                        {
                            ["id"] = ColStr(d, "ID"),
                            ["nationality"] = ColStr(d, "Nationality"),
                            ["confirmationNo"] = ColStr(d, "ConfirmationNo"),
                            ["roomNo"] = ColStr(d, "RoomNo"),
                            ["account"] = ColStr(d, "Account"),
                            ["dateOfBirth"] = ColDate(d, "DateOfBirth"),
                            ["passPort"] = FirstCol(d, "PassPort", "Passport"),
                            ["identityCard"] = ColStr(d, "IdentityCard"),
                            ["email"] = ColStr(d, "Email"),
                            ["address"] = ColStr(d, "Address"),
                            ["createBy"] = ColStr(d, "CreateBy"),
                            ["createDate"] = ColDate(d, "CreateDate"),
                            ["updateBy"] = ColStr(d, "UpdateBy"),
                            ["status"] = ColStr(d, "Status"),
                            ["updateDate"] = ColDate(d, FirstExisting(d, "UpdateDate", "UpDateDate", "UpdDate"))
                        });
                    }
                }
                return Json(result);
            }
            catch (Exception ex) { return Json(ex.Message); }
        }

        [HttpGet]
        [RequireFunction("rpt_Spr_FFPAndotherCards")]
        public IActionResult FrequentFlyerData(DateTime fromDate, DateTime toDate, string status, string memberID, int isSortByCardName = 0)
        {
            try
            {
                DataTable dataTable = _iReportService.FrequentFlyerData(NzDate(fromDate), NzDate(toDate), status ?? "", memberID ?? "", isSortByCardName);
                var result = new List<Dictionary<string, object>>();
                if (dataTable != null)
                {
                    foreach (DataRow d in dataTable.Rows)
                    {
                        result.Add(new Dictionary<string, object>
                        {
                            ["roNo"] = ColStr(d, "RoNo"),
                            ["name"] = ColStr(d, "Name"),
                            ["cardNumber"] = ColStr(d, "CardNumber"),
                            ["arrival"] = ColDate(d, "Arrival"),
                            ["depart"] = ColDate(d, "Depart"),
                            ["nights"] = ColStr(d, "Nights"),
                            ["marketCode"] = ColStr(d, "MarketCode"),
                            ["rateCode"] = ColStr(d, "RateCode"),
                            ["status"] = ColStr(d, "Status"),
                            ["member"] = ColStr(d, "Member"),
                            ["cardHolder"] = ColStr(d, "CardHolder")
                        });
                    }
                }
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        [HttpGet]
        [RequireFunction("rpt_Spr_ReservationView")]
        public IActionResult ReservationViewData(int status, string nationality, string city, string country)
        {
            try { return JsonTable(_iReportService.ReservationViewData(status, nationality, city, country)); }
            catch (Exception ex) { return Json(ex.Message); }
        }

        [HttpGet]
        [RequireFunction("rpt_Spr_ReportComments")]
        public IActionResult ReservationCommentsData(string fromDate, string toDate, int status, int viewBy)
        {
            try
            {
                var st = status == 1 ? "0,5" : status == 2 ? "1,6" : status == 3 ? "2" : "";
                var type = viewBy == 2 ? 1 : 0;
                return JsonTable(_iReportService.ReservationCommentsData(
                    ParseReportDate(fromDate), ParseReportDate(toDate), st, viewBy, type));
            }
            catch (Exception ex) { return Json(ex.Message); }
        }

        [HttpGet]
        [RequireFunction("rpt_Spr_PackageReportbyPerson")]
        public IActionResult PackageReportbyPersonData(DateTime fromDate, DateTime toDate)
        {
            try
            {
                DataTable dataTable = _iReportService.PackageReportbyPersonData(NzDate(fromDate), NzDate(toDate));
                var result = new List<Dictionary<string, object>>();
                if (dataTable != null)
                {
                    foreach (DataRow d in dataTable.Rows)
                    {
                        result.Add(new Dictionary<string, object>
                        {
                            ["package"] = ColStr(d, "Package"),
                            ["name"] = FirstCol(d, "PackageName", "Name"),
                            ["a"] = ColDec(d, "NoOfAdult"),
                            ["c"] = ColDec(d, "NoOfChild"),
                            ["c1"] = ColDec(d, "NoOfChild1"),
                            ["c2"] = ColDec(d, "NoOfChild2"),
                            ["bn1"] = ColDec(d, "BN1"),
                            ["bn2"] = ColDec(d, FirstExisting(d, "Person", "BN2")),
                            ["rn"] = ColDec(d, "RN")
                        });
                    }
                }
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        [HttpGet]
        [RequireFunction("rptARTrace")]
        public IActionResult ARTraceData(string fromDate, string toDate, string state)
        {
            try
            {
                var st = string.IsNullOrWhiteSpace(state) ? "All" : state.Trim();
                DataTable dataTable = _iReportService.ARTraceData(ParseReportDate(fromDate), ParseReportDate(toDate), st);
                var result = new List<Dictionary<string, object>>();
                if (dataTable != null)
                {
                    foreach (DataRow d in dataTable.Rows)
                    {
                        result.Add(new Dictionary<string, object>
                        {
                            ["accountName"] = FirstCol(d, "Account Name", "AccountName"),
                            ["acctNo"] = FirstCol(d, "Acct. No", "Acct No", "AccountNo"),
                            ["time"] = ColDateTime(d, FirstExisting(d, "Time", "TraceAt")),
                            ["resolvedAt"] = ColDateTime(d, FirstExisting(d, "Resolved At", "ResolvedAt")),
                            ["resolvedBy"] = FirstCol(d, "Resolved By", "ResolvedBy"),
                            ["createdBy"] = FirstCol(d, "Created By", "CreatedBy")
                        });
                    }
                }
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        [HttpGet]
        [RequireFunction("rptReservationWithDiscount")]
        public IActionResult ReservationWithDiscountData(string fromDate, string toDate, int dateType,
            string rmClasses, string rmTypes, string rateIDs, string mkIDs, string srcIDs, string statuses, int sort)
        {
            try
            {
                DataTable dataTable = _iReportService.ReservationWithDiscountData(
                    ParseReportDate(fromDate),
                    ParseReportDate(toDate),
                    dateType,
                    CsvIds(rmClasses),
                    CsvIds(rmTypes),
                    CsvIds(rateIDs),
                    CsvIds(mkIDs),
                    CsvIds(srcIDs),
                    CsvIds(statuses),
                    sort);
                var result = new List<Dictionary<string, object>>();
                if (dataTable != null)
                {
                    foreach (DataRow d in dataTable.Rows)
                    {
                        result.Add(new Dictionary<string, object>
                        {
                            ["name"] = FirstCol(d, "Name", "LastName"),
                            ["arrival"] = ColDate(d, FirstExisting(d, "Arrival", "ArrivalDate")),
                            ["departure"] = ColDate(d, FirstExisting(d, "Departure", "DepartureDate")),
                            ["confNo"] = FirstCol(d, "Conf. No.", "Conf. No", "ConfirmationNo", "ConfNo"),
                            ["rms"] = ColDec(d, FirstExisting(d, "Rms.", "Rms", "NoOfRoom", "Rooms")),
                            ["rmNo"] = FirstCol(d, "Rm No.", "Rm No", "RoomNo", "Room Number"),
                            ["rmType"] = FirstCol(d, "Rm Type", "Room Type", "RoomType", "RoomTypeCode"),
                            ["rateCode"] = FirstCol(d, "Rate Code", "RateCode"),
                            ["cur"] = FirstCol(d, "Cur.", "Cur", "Currency", "CurrencyCode"),
                            ["originalRate"] = ColDec(d, FirstExisting(d, "Original Rate", "OriginalRate")),
                            ["discountAmt"] = ColDec(d, FirstExisting(d, "Discount Amt.", "Discount Amt", "DiscountAmt")),
                            ["discountPct"] = ColDec(d, FirstExisting(d, "Discount %", "Discount%", "DiscountPct", "DiscountPercent")),
                            ["discountRate"] = ColDec(d, FirstExisting(d, "Discount Rate", "DiscountRate")),
                            ["discountReason"] = FirstCol(d, "Discount Reason", "DiscountReason", "Reason")
                        });
                    }
                }
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        private static readonly TimeSpan LookupCacheTtl = TimeSpan.FromMinutes(15);

        private List<T> CachedLookup<T>(string key, Func<List<T>> factory)
        {
            return _cache.GetOrCreate(key, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = LookupCacheTtl;
                return factory() ?? new List<T>();
            }) ?? new List<T>();
        }

        private void BindLookups(params string[] keys)
        {
            foreach (var key in keys)
            {
                switch (key)
                {
                    case "zone":
                        ViewBag.ZoneList = CachedLookup("rpt:zone:v2", () =>
                            PropertyUtils.ConvertToList<ZoneModel>(ZoneBO.Instance.FindAll())
                                .Where(z => z != null && !z.Inactive && !string.IsNullOrWhiteSpace(z.Code))
                                .GroupBy(z => z.Code, StringComparer.OrdinalIgnoreCase)
                                .Select(g => g.First())
                                .OrderBy(z => z.Code, StringComparer.OrdinalIgnoreCase)
                                .ToList());
                        break;
                    case "roomType":
                        ViewBag.RoomTypeList = CachedLookup("rpt:roomType", () => PropertyUtils.ConvertToList<RoomTypeModel>(RoomTypeBO.Instance.FindAll()));
                        break;
                    case "roomClass":
                        ViewBag.RoomClassList = CachedLookup("rpt:roomClass", () => PropertyUtils.ConvertToList<RoomClassModel>(RoomClassBO.Instance.FindAll()));
                        break;
                    case "market":
                        ViewBag.MarketList = CachedLookup("rpt:market", () => PropertyUtils.ConvertToList<MarketModel>(MarketBO.Instance.FindAll()));
                        break;
                    case "rateCode":
                        ViewBag.RateCodeList = CachedLookup("rpt:rateCode", () => PropertyUtils.ConvertToList<RateCodeModel>(RateCodeBO.Instance.FindAll()));
                        break;
                    case "room":
                        ViewBag.RoomList = CachedLookup("rpt:room", () => PropertyUtils.ConvertToList<RoomModel>(RoomBO.Instance.FindAll()));
                        break;
                    case "article":
                        ViewBag.ArticleList = CachedLookup("rpt:article", () => PropertyUtils.ConvertToList<ArticleModel>(ArticleBO.Instance.FindAll()));
                        break;
                    case "source":
                        ViewBag.SourceList = CachedLookup("rpt:source", () => PropertyUtils.ConvertToList<SourceModel>(SourceBO.Instance.FindAll()));
                        break;
                    case "payTxn":
                        ViewBag.TransactionsList = CachedLookup("rpt:payTxn", () =>
                            PropertyUtils.ConvertToList<TransactionsModel>(TransactionsBO.Instance.FindAll()).Where(x => x.GroupType == 1).ToList());
                        break;
                    case "txn":
                        ViewBag.TransactionsList = CachedLookup("rpt:txn", () => PropertyUtils.ConvertToList<TransactionsModel>(TransactionsBO.Instance.FindAll()));
                        break;
                    case "block":
                        ViewBag.BusinessBlockList = CachedLookup("rpt:block", () => PropertyUtils.ConvertToList<BusinessBlockModel>(BusinessBlockBO.Instance.FindAll()));
                        break;
                    case "cashier":
                        ViewBag.CashierUserList = CachedLookup("rpt:cashier", () => PropertyUtils.ConvertToList<CashierUserModel>(CashierUserBO.Instance.FindAll()));
                        break;
                    case "arPay":
                        ViewBag.ARPaymentList = CachedLookup("rpt:arPay", () =>
                            PropertyUtils.ConvertToList<ARPaymentModel>(ARPaymentBO.Instance.FindAll())
                                .GroupBy(x => x.TransactionCode)
                                .Select(g => g.First())
                                .ToList());
                        break;
                    case "vip":
                        ViewBag.VIPList = CachedLookup("rpt:vip", () => PropertyUtils.ConvertToList<VIPModel>(VIPBO.Instance.FindAll()));
                        break;
                    case "transport":
                        ViewBag.TransportTypeList = CachedLookup("rpt:transport", () => PropertyUtils.ConvertToList<TransportTypeModel>(TransportTypeBO.Instance.FindAll()));
                        break;
                    case "users":
                        ViewBag.UsersList = CachedLookup("rpt:users", () => PropertyUtils.ConvertToList<UsersModel>(UsersBO.Instance.FindAll()));
                        break;
                    case "pref":
                        ViewBag.PreferenceList = CachedLookup("rpt:pref", () => PropertyUtils.ConvertToList<PreferenceModel>(PreferenceBO.Instance.FindAll()));
                        break;
                    case "company":
                        ViewBag.CompanyList = CachedLookup("rpt:company", () =>
                            PropertyUtils.ConvertToList<ProfileModel>(ProfileBO.Instance.FindAll()).Where(x => x.Type == 2).ToList());
                        break;
                    case "holder":
                        break;
                    case "dept":
                        ViewBag.DepartmentList = CachedLookup("rpt:dept", () => PropertyUtils.ConvertToList<DepartmentModel>(DepartmentBO.Instance.FindAll()));
                        break;
                    case "comment":
                        ViewBag.ComList = CachedLookup("rpt:comment", () => PropertyUtils.ConvertToList<CommentModel>(CommentBO.Instance.FindAll()));
                        break;
                    case "memberType":
                        ViewBag.MemberTypeList = CachedLookup("rpt:memberType", () =>
                            PropertyUtils.ConvertToList<MemberTypeModel>(MemberTypeBO.Instance.FindAll())
                                .Where(x => !x.Inactive)
                                .OrderBy(x => x.Name ?? x.Code)
                                .ToList());
                        break;
                    case "memberCat":
                        ViewBag.MemberCategoryList = CachedLookup("rpt:memberCat", () =>
                            PropertyUtils.ConvertToList<MemberCategoryModel>(MemberCategoryBO.Instance.FindAll())
                                .Where(x => !x.Inactive)
                                .OrderBy(x => x.Name ?? x.Code)
                                .ToList());
                        break;
                    case "alert":
                        ViewBag.ALertList = CachedLookup("rpt:alert", () => PropertyUtils.ConvertToList<AlertsSetupModel>(AlertsSetupBO.Instance.FindAll()));
                        break;
                    case "currency":
                        ViewBag.CurrencyList = CachedLookup("rpt:currency", () => PropertyUtils.ConvertToList<CurrencyModel>(CurrencyBO.Instance.FindAll()));
                        break;
                    case "nationality":
                        ViewBag.NationalityList = CachedLookup("rpt:nationality", () =>
                            PropertyUtils.ConvertToList<NationalityModel>(NationalityBO.Instance.FindAll())
                                ?.Where(x => !x.Inactive)
                                .OrderBy(x => x.Name ?? x.Code)
                                .ToList());
                        break;
                    case "city":
                        ViewBag.CityList = CachedLookup("rpt:city", () =>
                            PropertyUtils.ConvertToList<CityModel>(CityBO.Instance.FindAll())
                                ?.Where(x => !x.Inactive)
                                .OrderBy(x => x.Name ?? x.Code)
                                .ToList());
                        break;
                    case "country":
                        ViewBag.CountryList = CachedLookup("rpt:country", () =>
                            PropertyUtils.ConvertToList<CountryModel>(CountryBO.Instance.FindAll())
                                ?.Where(x => !x.Inactive)
                                .OrderBy(x => x.Name ?? x.Code)
                                .ToList());
                        break;
                    case "arAccType":
                        ViewBag.ARAccountTypeList = CachedLookup("rpt:arAccType", () =>
                            PropertyUtils.ConvertToList<ARAccountTypeModel>(ARAccountTypeBO.Instance.FindAll()));
                        break;
                }
            }
        }

        public IActionResult LoadReport(string reportName,string title)
        {
            switch (reportName)
            {
                case "RatecodebyDateReport":
                case "OtherReport":
                case "OtherReportChart":
                    BindLookups("rateCode");
                    break;
                case "FixChargeReport":
                    BindLookups("txn");
                    break;
                case "RevenueBy":
                case "RevenueByChart":
                case "RoomOccupancyStatistics":
                case "RevenueRoom":
                case "Revenuebyroom":
                case "SaleinChargeActivity":
                case "ProductActivity":
                case "TAProductionReport":
                    BindLookups("zone", "roomType");
                    break;
                case "DailyMinibarReport":
                    BindLookups("room", "article");
                    break;
                case "DepositActivity":
                    BindLookups("cashier");
                    break;
                case "IncurringDepositCollection":
                case "IncurringDepositReturn":
                case "IncurringDepositSummary":
                case "IncurringDepositPaymentPlan":
                    BindLookups("cashier", "zone");
                    break;
                case "JournalByCashierAndArticle":
                    BindLookups("payTxn", "room", "article", "cashier", "roomClass");
                    break;
                case "ArticleByRoom":
                    BindLookups("zone", "room", "article");
                    break;
                case "ReservationStatistics":
                case "RoomOccupancy":
                case "RoomOccupancyChart":
                case "GuestMarketReport":
                case "NationalStatistics":
                case "LeadTimeReport":
                case "HistoryAndForecast":
                case "MarketSegmentDetail":
                case "GuestLedger":
                case "PostingJournalInvoicing":
                case "CancellationJournal":
                    BindLookups("zone");
                    break;
                case "DeparturesReport":
                    BindLookups("roomClass", "roomType", "zone", "vip", "rateCode", "payTxn", "block");
                    break;
                case "ArrivalsDetailed":
                case "ArrivalDetailedGroupbyHolder":
                    BindLookups("roomClass", "roomType", "source", "vip", "rateCode", "market");
                    break;
                case "ReservationPreblocked":
                case "VacantRoom":
                case "GuestTrialBalance":
                    BindLookups("roomClass", "roomType");
                    break;
                case "ManagerReport":
                    BindLookups("currency");
                    break;
                case "Transportation":
                    BindLookups("transport");
                    break;
                case "OccupancyByPercon":
                case "RoomTypeStatistic":
                    BindLookups("roomType");
                    break;
                case "NoShowReport":
                    BindLookups("roomClass", "zone");
                    break;
                case "CashierAudit":
                    BindLookups("payTxn", "users");
                    break;
                case "RoomStatistic":
                case "ReservationRateCheck":
                case "TrialBalance":
                case "DepositRequestLogReport":
                    BindLookups("roomClass");
                    break;
                case "TransferARReport":
                    BindLookups("cashier");
                    break;
                case "GroupBalance":
                    break;
                case "ProfileProductionStatistics":
                    BindLookups("roomClass", "currency");
                    break;
                case "BookingbyHolder":
                    BindLookups("market");
                    break;
                case "AnnualRoomOccupancy":
                case "RatecodeReport":
                    BindLookups("zone", "rateCode");
                    break;
                case "ReservationbyCompany":
                    BindLookups("roomClass", "roomType", "zone");
                    break;
                case "ArrivalsAndCheckInToday":
                    BindLookups("roomClass", "roomType", "arPay", "vip");
                    break;
                case "DailyPickupReport":
                    BindLookups("roomClass", "dept", "zone");
                    break;
                case "ReservationSummary":
                    BindLookups("zone", "roomType", "market", "currency");
                    break;
                case "SaleinChargeReport":
                    BindLookups("zone", "roomType", "market");
                    break;
                case "GuestInHousebyCompany":
                    BindLookups("roomClass", "roomType", "company");
                    break;
                case "GuestInHousebyGroup":
                    BindLookups("roomClass", "block");
                    break;
                case "GuestInHousebyRoom":
                    BindLookups("roomClass", "roomType", "vip", "payTxn", "block");
                    break;
                case "GuestInHouseSpecialRequests":
                    BindLookups("pref");
                    break;
                case "GuestInHouseComplimentaryAndHouseUse":
                case "GIHComplimentaryandHouseUse":
                    BindLookups("vip");
                    break;
                case "HouseKeepingStatus":
                    BindLookups("roomClass", "roomType", "block", "vip");
                    break;
                case "OutOfOrderByPerson":
                    BindLookups("roomType", "zone");
                    break;
                case "TraceReport":
                case "RoomDiscrepancy":
                case "RevenueSummary":
                case "RevenueDetail":
                case "OTAsMonthlyReport":
                case "BlacklistReport":
                case "BookingSource":
                case "ReportNationalityStatistics":
                    BindLookups("roomClass", "dept");
                    break;
                case "ReservationCancellation":
                    BindLookups("comment", "zone");
                    break;
                case "Alerts":
                    BindLookups("alert");
                    break;
                case "FrequentFlyerReport":
                    BindLookups("memberCat", "memberType");
                    break;
                case "PackageReportbyPerson":
                    break;
                case "ARTrace":
                    break;
                case "ReservationWithDiscount":
                    BindLookups("roomClass", "roomType", "rateCode", "market", "source");
                    break;
                case "ReservationView":
                    BindLookups("nationality", "city", "country");
                    break;
                case "ReservationComments":
                    break;
                case "AgingbyTypeOnly":
                    BindLookups("arAccType");
                    break;
                case "ReservationCancellationChart":
                    BindLookups("comment");
                    break;
            }
            ViewBag.ReportTitle = title;
            var found = _viewEngine.FindView(ControllerContext, reportName, isMainPage: false);
            if (!found.Success)
            {
                found = _viewEngine.GetView(executingFilePath: null, viewPath: $"~/Views/Report/{reportName}.cshtml", isMainPage: false);
            }
            if (!found.Success)
            {
                var safeTitle = System.Net.WebUtility.HtmlEncode(title ?? reportName);
                var safeName = System.Net.WebUtility.HtmlEncode(reportName);
                return Content(
                    $"<div class=\"p-3\">Báo cáo <b>{safeTitle}</b> chưa có màn hình (thiếu Views/Report/{safeName}.cshtml).</div>",
                    "text/html");
            }
            return PartialView(reportName, title);
        }





















        //[HttpGet]
        //public IActionResult TestChartReport(DateTime fromDate, DateTime toDate)
        //{
        //    // Khởi tạo report
        //    XtraReport report = new OneSPMSh.Report.Report2();

        //    // Lấy dữ liệu gốc
        //    DataTable originalTable = BookingSourceReportBO.GetBookingSourceData(fromDate, toDate);
        //    DataTable singleRowTable = originalTable.Clone();

        //    if (originalTable.Rows.Count > 0)
        //    {
        //        singleRowTable.ImportRow(originalTable.Rows[0]); // Chỉ lấy dòng đầu tiên
        //    }

        //    // Gán nguồn dữ liệu
        //    report.DataSource = singleRowTable;

        //    // Lấy biểu đồ từ report
        //    var chart = report.FindControl("xrChart2", true) as DevExpress.XtraReports.UI.XRChart;
        //    if (chart != null)
        //    {
        //        chart.Series.Clear();

        //        string[] valueColumns = { "RNActual", "BUSMIXActual", "ADRActual", "REVForcast", "REVMIXForcast" };

        //        foreach (var column in valueColumns)
        //        {
        //            var series = new DevExpress.XtraCharts.Series(column, DevExpress.XtraCharts.ViewType.Bar);
        //            series.ArgumentDataMember = "Source         "; // Trục X
        //            series.ValueDataMembers.AddRange(column);  // Trục Y
        //            chart.Series.Add(series);
        //        }

        //        var diagram = chart.Diagram as DevExpress.XtraCharts.XYDiagram;
        //        if (diagram != null)
        //        {
        //            diagram.AxisY.WholeRange.SetMinMaxValues(0, 1000);
        //            diagram.AxisY.Title.Text = "Giá trị";
        //            diagram.AxisY.Title.Visibility = DevExpress.Utils.DefaultBoolean.True;

        //            diagram.AxisX.Title.Text = "Nguồn đặt phòng";
        //            diagram.AxisX.Title.Visibility = DevExpress.Utils.DefaultBoolean.True;
        //        }
        //    }

        //    return PartialView("_ReportViewerPartial", report);
        //}

        [HttpGet]
        public IActionResult DashBoard(DateTime businessDate)
        {
            try
            {
                var businessDates = PropertyUtils.ConvertToList<BusinessDateModel>(
                    BusinessDateBO.Instance.FindAll()
                );

                 businessDate = businessDates[0].BusinessDate;
                int totalDueinCheckedint = 0;
                int guestInHouseCount = 0;
                int dueoutCount = 0;

                // Khai báo biến tổng cho các cột cần tính
                decimal totalA = 0;
                decimal totalC = 0;
                decimal totalC1 = 0;
                decimal totalC2 = 0;

                for (int status = 3; status <= 4; status++)
                {
                    DataTable dt = _iReportService.ReservationSearchRSVDate(businessDate,status);
                    totalDueinCheckedint += dt.Rows.Count;

                    // Tính tổng từng cột
                    foreach (DataRow row in dt.Rows)
                    {
                        if (dt.Columns.Contains("Adults") && row["Adults"] != DBNull.Value)
                            totalA += Convert.ToDecimal(row["Adults"]);

                        if (dt.Columns.Contains("Child") && row["Child"] != DBNull.Value)
                            totalC += Convert.ToDecimal(row["Child"]);

                        if (dt.Columns.Contains("Child1") && row["Child1"] != DBNull.Value)
                            totalC1 += Convert.ToDecimal(row["Child1"]);

                        if (dt.Columns.Contains("Child2") && row["Child2"] != DBNull.Value)
                            totalC2 += Convert.ToDecimal(row["Child2"]);
                    }
                }

                DataTable dtGuestInHouse = _iReportService.ReservationSearchRSVDate(new DateTime(1900, 1, 1), 1);
                guestInHouseCount = dtGuestInHouse.Rows.Count;

                DataTable dtdueoutCount = _iReportService.ReservationSearchRSVDate(businessDate,5);
                dueoutCount = dtdueoutCount.Rows.Count;

                DataTable dtcheckoutCount = _iReportService.ReservationSearchRSVDate(businessDate, 6);
                int checkoutCount = dtcheckoutCount.Rows.Count;


                DataTable dtrsvCount = _iReportService.ReservationSearchRSVDate(businessDate, 2);
                int rsvCount = dtrsvCount.AsEnumerable()
                          .Count(row => !string.IsNullOrWhiteSpace(row["RoomNo"]?.ToString()));



                List<ZoneModel> listzo = PropertyUtils.ConvertToList<ZoneModel>(ZoneBO.Instance.FindAll());
                decimal occupancyPercent = 0;
                //foreach (var zone in listzo)
                //{
                //    string zoneCode = zone.Code;

                //    // Gọi hàm với từng ZoneCode
                //    DataTable dataTable = _iReportService.RoomFacilityForecastData(businessDate, businessDate, zoneCode);

                //    string columnName = businessDate.ToString("yyyy/MM/dd");

                //    // Tìm dòng có StatisticName = 'Occupancy %'
                //    var occupancyRow = dataTable.AsEnumerable()
                //        .FirstOrDefault(row => row.Field<string>("StatisticName") == "Occupancy %");

                //    if (occupancyRow != null && dataTable.Columns.Contains(columnName))
                //    {
                //        var valueObj = occupancyRow[columnName];
                //        if (valueObj != DBNull.Value)
                //        {
                //             occupancyPercent = Convert.ToDecimal(valueObj);
           
                //        }
                //    }
                //}
                string paraDate = $"[{businessDate.Day}{businessDate.Month}]";
                string paraDateConvert = $"'Date1' = Convert(nvarchar,sum(isnull({paraDate},0)))";

                List<ReservationTypeModel> listresttype  = PropertyUtils.ConvertToList<ReservationTypeModel>(ReservationTypeBO.Instance.FindAll());
                var ids = listresttype.Select(x => x.ID.ToString()).ToList();
                string resvType;

                if (ids.Count == 0)
                {
                    resvType = "''"; // Trường hợp rỗng
                }
                else if (ids.Count == 1)
                {
                    resvType = $"'{ids[0]}'";
                }
                else
                {
                    var middle = ids.Skip(1).Take(ids.Count - 2)
                                    .Select(id => $"'{id}'");

                    string first = $"{ids.First()}'";
                    string last = $"'{ids.Last()}";

                    resvType = string.Join(",", new[] { first }.Concat(middle).Append(last));
                }

               DataTable roomavl = _iReportService.RoomAvailableNew(businessDate,paraDate, paraDateConvert, resvType, listzo[0].Code);


                var chartStatus = new List<object>();

                List<RoomTypeModel> listrt = PropertyUtils.ConvertToList<RoomTypeModel>(RoomTypeBO.Instance.FindAll());
                var validRoomTypeCodes = new HashSet<string>(listrt.Select(rt => rt.Code));

                int totalRoom = 0;
                int avail = 0;
                int oooInt = 0;
                int totaldate1 = 0;
                int booked = 0;
                if (roomavl != null && roomavl.Rows.Count > 0)
                {
                    // Lọc các dòng có Roomtype hợp lệ
                 var groupedData = roomavl.AsEnumerable()
                        .Where(row => validRoomTypeCodes.Contains(row["Roomtype"]?.ToString()))
                        .ToList();

                    // Tính tổng TotalRooms
                    totalRoom = groupedData
                        .Sum(row => Convert.ToInt32(row["TotalRooms"]));

                    // Lấy OOO từ row DisplaySequence = "99999"
                    var row99999 = roomavl.AsEnumerable()
                        .FirstOrDefault(row => row["DisplaySequence"]?.ToString() == "99999");
                    if (row99999 == null)
                    {
                        // fallback: dùng dòng cuối nếu không tìm thấy "99999"
                        row99999 = roomavl.AsEnumerable().Last();
                    }
                    if (row99999 != null && int.TryParse(row99999["Date1"]?.ToString(), out int parsedValue))
                    {
                        oooInt = parsedValue;
                    }

                    // Lấy OCC (booked) từ row DisplaySequence = "333"
                    var row333 = roomavl.AsEnumerable()
                        .FirstOrDefault(row => row["DisplaySequence"]?.ToString() == "333");
                    if (row333 != null && int.TryParse(row333["Date1"]?.ToString(), out int val))
                    {
                        booked = val;
                    }

                    // Lấy OOS từ row DisplaySequence = "666" (lần xuất hiện cuối)
                    int oosInt = 0;
                    var row666 = roomavl.AsEnumerable()
                        .Where(row => row["DisplaySequence"]?.ToString() == "666")
                        .LastOrDefault();
                    if (row666 != null && int.TryParse(row666["Date1"]?.ToString(), out int oosVal))
                    {
                        oosInt = oosVal;
                    }

                    // Available = Total - OOO - OOS - OCC
                    avail = totalRoom - oooInt - oosInt - booked;

                    totaldate1 = groupedData
                        .Sum(row => Convert.ToInt32(row["Date1"]));

                    chartStatus.Add(new { Label = "OCC", Value = booked });
                    chartStatus.Add(new { Label = "Available", Value = avail });
                    chartStatus.Add(new { Label = "OOO", Value = oooInt });
                    chartStatus.Add(new { Label = "OOS", Value = oosInt });
                    chartStatus.Add(new { Label = "Total", Value = totalRoom });
                }


                if (totalRoom > 0)
                {
                    occupancyPercent = Math.Round((decimal)booked * 100 / totalRoom, 1);
                }



                return Json(new
                {
                    TotalDueinCheckedint = totalDueinCheckedint,
                    GuestInHouseCount = guestInHouseCount,
                    TotaCheckedoutint = checkoutCount,
                    TotalDueoutint = dueoutCount,
                    TotalA = totalA,
                    TotalC = totalC+ totalC1+ totalC2,
                    OccupancyPercent = occupancyPercent,
                    RsvCount = rsvCount,
                    ChartStatus = chartStatus
                });
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

    }

}

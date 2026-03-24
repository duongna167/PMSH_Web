using BaseBusiness.BO;
using BaseBusiness.Model;
using BaseBusiness.util;
using DevExpress.Data.Filtering.Helpers;
using DevExpress.Utils.CommonDialogs.Internal;
using DevExpress.Web.Internal;
using DevExpress.XtraReports.UI;
using DevExpress.XtraRichEdit.Import.Doc;
using DevExpress.XtraRichEdit.Import.Html;
using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Logging;
using Newtonsoft.Json;
using Reservation.Commons.Helpers;
using Reservation.Dto;
using Reservation.Services.Interfaces;
using System;
using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Transactions;
using System.Xml;
using static DevExpress.CodeParser.CodeStyle.Formatting.Rules;
using static log4net.Appender.RollingFileAppender;
using static Reservation.Dto.ReservationPackageDTO;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Text = DocumentFormat.OpenXml.Wordprocessing.Text;
namespace Reservation.Controllers
{
    public class ReservationController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ReservationController> _logger;
        private readonly IMemoryCache _cache;
        private readonly IReservationService _iReservationService;
        private readonly IFolioDetailService _iFolioDetailService;
        private readonly IDepositService _iDepositService;
        private readonly IRoutingService _iRoutingService;
        private readonly IGroupReservationService _iGroupReservationService;
        private readonly IMessageService _iMessageService;
        private readonly IShareService _iShareService;
        private readonly IGroupAdminService _iGroupAdminService;
        private readonly IWebHostEnvironment _environment;
        private readonly string _secretKey;
        private readonly string _iv;
        public ReservationController(ILogger<ReservationController> logger,
                IMemoryCache cache, IConfiguration configuration, IReservationService iReservationService, IFolioDetailService folioDetailService,
                IDepositService iDepositService, IRoutingService iRoutingService, IGroupReservationService iGroupReservationService,
                IMessageService iMessageService, IShareService iShareService, IGroupAdminService iGroupAdminService
            , IWebHostEnvironment environment)
        {
            _secretKey = configuration["Encryption:Key"] ?? throw new ArgumentNullException("Encryption:Key is missing in configuration");
            _iv = configuration["Encryption:IV"] ?? throw new ArgumentNullException("Encryption:IV is missing in configuration");

            _cache = cache;
            _logger = logger;
            _configuration = configuration;
            _iReservationService = iReservationService;
            _iFolioDetailService = folioDetailService;
            _iDepositService = iDepositService;
            _iRoutingService = iRoutingService;
            _iGroupReservationService = iGroupReservationService;
            _iMessageService = iMessageService;
            _iShareService = iShareService;
            _iGroupAdminService = iGroupAdminService;
            _environment = environment;
        }
        [HttpPost]
        public IActionResult EncryptId(int id)
        {
            try
            {
                string encryptedId = Encrypt(id.ToString(), _secretKey, _iv);
                return Json(new { success = true, encryptedId });
            }
            catch (Exception)
            {
                return Json(new { success = false });
            }
        }

        public IActionResult NewReservation(string key)
        {
            int? id = null;
            if (!string.IsNullOrEmpty(key))
            {
                try
                {
                    string decryptedId = Decrypt(key, _secretKey, _iv);
                    id = int.Parse(decryptedId);
                }
                catch (Exception)
                {
                    return BadRequest("Invalid key");
                }
            }

            List<BusinessDateModel> businessDateModel = PropertyUtils.ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll());

            ViewBag.cboReasonRes = ListItemHelper.GetReasonAdjustForReservationProvider();
            ViewBag.cboNationality = ListItemHelper.GetNationalityProvider();
            ViewBag.cboNationality = ListItemHelper.GetNationalityProvider();
            ViewBag.cboTitle = ListItemHelper.GetTitleProviderRSV();
            ViewBag.cboCity = ListItemHelper.GetCityProvider();
            ViewBag.cboVIP = ListItemHelper.GetVIPProvider();
            ViewBag.cboMemberType = ListItemHelper.GetMemberTypeProvider();

            ViewBag.cboRoomType = ListItemHelper.GetRoomTyeProvider();
            ViewBag.cboCurrency = ListItemHelper.GetCurrencyProvider();
            ViewBag.cboPackage = ListItemHelper.GetPackagesProvider();
            ViewBag.cboReason = ListItemHelper.GetReasonProvider();
            ViewBag.cboReservationType = ListItemHelper.GetReservationTypeProvider();
            ViewBag.cboSource = ListItemHelper.GetSourceProvider();
            ViewBag.cboMarket = ListItemHelper.GetMarketProvider();
            ViewBag.cboAllotmentType = ListItemHelper.GetAllotmentTypeProvider();
            ViewBag.cboZone = ListItemHelper.GetZoneProvider();
            ViewBag.cboPersonInCharge = ListItemHelper.GetPersonInChargeProvider();
            ViewBag.cboPaymentMethod = ListItemHelper.GetPaymentMethodProvider();
            ViewBag.cboPromotion = ListItemHelper.GetPromotionProvider();
            ViewBag.cboGroupPreferenceProvider = ListItemHelper.GetGroupPreferenceProvider();
            ViewBag.cboTransportType = ListItemHelper.GetTransportTypeProvider();
            ViewBag.businesDate = businessDateModel[0].BusinessDate;
            ViewBag.cboItem = ListItemHelper.GetItemInventoryProvider();
            ViewBag.configETA = _iReservationService.GetConfigETA();
            ViewBag.configETD = _iReservationService.GetConfigETD();

            ReservationModel reservation = new ReservationModel();
            if (id.HasValue)
            {
                reservation = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(id.Value);
            }
            ViewBag.Reservation = reservation;

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView(); // hoặc PartialView("_NewReservationPartial")
            }
            return View();
        }

        private string Encrypt(string plainText, string key, string iv)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key);
                aes.IV = Encoding.UTF8.GetBytes(iv);
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                using (var ms = new System.IO.MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (var sw = new System.IO.StreamWriter(cs))
                    {
                        sw.Write(plainText);
                    }
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        private string Decrypt(string cipherText, string key, string iv)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key);
                aes.IV = Encoding.UTF8.GetBytes(iv);
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                using (var ms = new System.IO.MemoryStream(Convert.FromBase64String(cipherText)))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var sr = new System.IO.StreamReader(cs))
                {
                    return sr.ReadToEnd();
                }
            }
        }

        public IActionResult SearchReservation()
        {

            List<BusinessDateModel> businessDateModel = PropertyUtils.ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll());
            ViewBag.cboNationality = ListItemHelper.GetNationalityProvider();
            ViewBag.cboTitle = ListItemHelper.GetTitleProvider();
            ViewBag.cboCity = ListItemHelper.GetCityProvider();
            ViewBag.cboVIP = ListItemHelper.GetVIPProvider();
            ViewBag.cboMemberType = ListItemHelper.GetMemberTypeProvider();
            //ViewBag.cboProfileAgent = ListItemHelper.GetProfileAgentProvider();
            //ViewBag.cboProfileCompany = ListItemHelper.GetProfileCompanyProvider();'
            //ViewBag.cboProfileContact = ListItemHelper.GetProfileContactProvider();
            ViewBag.cboRoomType = ListItemHelper.GetRoomTyeProvider();
            ViewBag.cboCurrency = ListItemHelper.GetCurrencyProvider();
            ViewBag.cboPackage = ListItemHelper.GetPackagesProvider();
            ViewBag.cboReason = ListItemHelper.GetReasonProvider();
            ViewBag.cboReservationType = ListItemHelper.GetReservationTypeProvider();
            ViewBag.cboSource = ListItemHelper.GetSourceProvider();
            ViewBag.cboMarket = ListItemHelper.GetMarketProvider();
            ViewBag.cboZone = ListItemHelper.GetZoneProvider();
            ViewBag.cboRoomType = ListItemHelper.GetRoomTyeProvider();
            ViewBag.cboRoom = ListItemHelper.GetRoomProvider();
            ViewBag.cboRoomClass = ListItemHelper.GetRoomClassProvider();
            //ViewBag.cboProfile = ListItemHelper.GetProfileProvider();
            ViewBag.cboAllotmentType = ListItemHelper.GetAllotmentTypeProvider();
            ViewBag.cboPersonInCharge = ListItemHelper.GetPersonInChargeProvider();
            ViewBag.cboPaymentMethod = ListItemHelper.GetPaymentMethodProvider();
            ViewBag.cboPromotion = ListItemHelper.GetPromotionProvider();
            ViewBag.cboGroupPreferenceProvider = ListItemHelper.GetGroupPreferenceProvider();
            ViewBag.cboTransportType = ListItemHelper.GetTransportTypeProvider();
            ViewBag.businesDate = businessDateModel[0].BusinessDate;
            ViewBag.cboItem = ListItemHelper.GetItemInventoryProvider();
            ViewBag.cboTransaction = ListItemHelper.GetTransactionProvider(true, "Choose Transaction");
            return View();
        }


        public IActionResult NewGroupReservation()
        {
            List<BusinessDateModel> businessDateModel = PropertyUtils.ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll());
            ViewBag.businesDate = businessDateModel[0].BusinessDate;
            ViewBag.cboMarket = ListItemHelper.GetMarketProvider();
            ViewBag.cboCity = ListItemHelper.GetCityProvider();
            ViewBag.cboPackage = ListItemHelper.GetPackagesProvider();
            ViewBag.cboReservationType = ListItemHelper.GetReservationTypeProvider();

            return PartialView();
        }
        public IActionResult SearchGroupReservationModal()
        {
            List<BusinessDateModel> businessDateModel = PropertyUtils.ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll());
            ViewBag.businesDate = businessDateModel[0].BusinessDate;
            ViewBag.cboMarket = ListItemHelper.GetMarketProvider();
            ViewBag.cboCity = ListItemHelper.GetCityProvider();
            ViewBag.cboPackage = ListItemHelper.GetPackagesProvider();
            ViewBag.cboReservationType = ListItemHelper.GetReservationTypeProvider();

            return PartialView();
        }
        public IActionResult WaitList()
        {
            ViewBag.cboRoomType = ListItemHelper.GetRoomTyeCodeProvider();
            return View();
        }


        public IActionResult GroupAdmin(string key, string displayType, string name, string roomNo)
        {
            int? id = null;
            if (!string.IsNullOrEmpty(key))
            {
                try
                {
                    string decryptedId = Decrypt(key, _secretKey, _iv);
                    id = int.Parse(decryptedId);
                }
                catch (Exception)
                {
                    return BadRequest("Invalid key");
                }
            }
            List<Dictionary<string, object>> reservations = new List<Dictionary<string, object>>();
            if (id.HasValue)
            {
                DataTable reservationTable = _iGroupAdminService.SearchGroupAdmin(id.ToString(), displayType, name, roomNo);
                reservations = reservationTable.AsEnumerable().Select(row =>
                    reservationTable.Columns.Cast<DataColumn>()
                        .ToDictionary(
                            column => column.ColumnName,
                            column => row[column] is DBNull ? null : row[column]
                        )
                ).ToList();
            }

            ViewBag.Reservation = reservations;
            ViewBag.cboRoomType = ListItemHelper.GetRoomTyeProvider();
            ViewBag.cboFloor = ListItemHelper.GetFloorProvider();
            return View();
        }
        public IActionResult OverBooking()
        {

            ViewBag.cboRoomType = ListItemHelper.GetRoomTyeProvider();

            return PartialView();

        }

        public IActionResult RateCodeAuthor()
        {
            ViewBag.cboUser = ListItemHelper.GetUserProvider();
            ViewBag.cboRateCode = ListItemHelper.GetRateCodeProvider();

            return View();
        }
        #region DatVP __ Commmon
        [HttpGet]
        public async Task<IActionResult> GetInfoProfile(int profileID)
        {
            try
            {

                ProfileModel profile = (ProfileModel)ProfileBO.Instance.FindByPrimaryKey(profileID);

                return Json(profile);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetTransactionPayment(int profileID)
        {
            try
            {

                List<TransactionsModel> trans = PropertyUtils.ConvertToList<TransactionsModel>(TransactionsBO.Instance.FindByAttribute("GroupCode", "PAY"));

                return Json(trans);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetInfoProfileIndividual(int id)
        {
            try
            {
                ReservationModel res = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(id);
                ProfileModel profile = (ProfileModel)ProfileBO.Instance.FindByPrimaryKey(res.ProfileIndividualId);

                return Json(profile);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetAllProfiles()
        {
            try
            {
                List<ProfileModel> profile = PropertyUtils.ConvertToList<ProfileModel>(ProfileBO.Instance.FindAll());

                return Json(profile);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetRateCode(DateTime arrivalDate, DateTime departure, int adults, int roomType)
        {
            try
            {
                DataTable myData = _iReservationService.GetRateCode(arrivalDate, departure, adults, roomType);


                var result = (from d in myData.AsEnumerable()
                              select new
                              {
                                  RateCodeID = int.Parse(d["RateCodeID"].ToString()),
                                  RoomTypeID = d["RoomTypeID"].ToString(),
                                  RateCode = d["RateCode"].ToString(),
                                  Amount = d["Amount"].ToString(),
                                  AmountAfterTax = d["AmountAfterTax"].ToString()

                                  //FigureImage = d["FigureImage"].ToString(),

                              }).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal Server Error", detail = ex.Message });
            }

        }

        [HttpGet]
        public async Task<IActionResult> GetAllRooms(DateTime fromDate, DateTime ToDate, string floor, string roomTypeID, string smoking, string foStatus,
            string hkStatus, string isDummy, string roomNo)
        {
            try
            {
                int roomID = 0;
                int type = 0;
                string hk = "";
                if (string.IsNullOrEmpty(roomNo))
                {
                    roomNo = "";
                }
                if (string.IsNullOrEmpty(floor))
                {
                    floor = "";
                }
                if (string.IsNullOrEmpty(roomTypeID))
                {
                    roomTypeID = "";
                }
                if (string.IsNullOrEmpty(smoking))
                {
                    smoking = "";
                }
                if (string.IsNullOrEmpty(foStatus))
                {
                    foStatus = "";
                }
                if (string.IsNullOrEmpty(hkStatus))
                {
                    hk = "";
                }
                else
                {
                    hk = string.Join("','", hkStatus.Split(','));
                }
                var list = PropertyUtils.ConvertToList<RoomModel>(RoomBO.Instance.FindAll()).Where(x => x.RoomNo == roomNo).ToList();
                if (list.Count > 0)
                {
                    roomID = list[0].ID;
                }

                DataTable myData = _iReservationService.GetRoomAvailable(fromDate, ToDate, floor, roomTypeID, smoking, foStatus, hk, isDummy, roomNo, roomID, type);
                var result = (from d in myData.AsEnumerable()
                              select new
                              {
                                  RoomID = d["RoomID"].ToString(),
                                  RoomNo = d["RoomNo"].ToString(),
                                  RoomType = d["RoomType"].ToString(),
                                  HKStatus = d["HKStatus"].ToString(),
                                  FO = d["FO"].ToString(),
                                  Floor = d["Floor"].ToString(),
                                  Connecting = d["Connecting"].ToString(),
                                  RoomTypeID = d["RoomTypeID"].ToString(),
                                  Dummy = d["Dummy"].ToString(),
                                  GuestCheckOut = d["GuestCheckOut"].ToString(),
                                  Balcony = d["Balcony"].ToString(),

                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetAllMarket()
        {
            try
            {
                List<MarketTypeModel> listMarketType = PropertyUtils.ConvertToList<MarketTypeModel>(MarketTypeBO.Instance.FindByAttribute("Inactive", 0));
                List<MarketModel> listMarket = PropertyUtils.ConvertToList<MarketModel>(MarketBO.Instance.FindAll());

                // Phẳng hóa dữ liệu
                var marketTypes = listMarketType.Select(mt => new
                {
                    Id = mt.ID,
                    Name = mt.Name,
                    Code = mt.Code,
                    ParentId = 0 // Root
                });

                var markets = listMarket.Select(m => new
                {
                    Id = m.ID,
                    Name = m.Name,
                    Code = m.Code,
                    ParentId = m.MarketTypeID // Là ID của market type
                });

                // Ghép lại thành một list duy nhất
                var treeData = marketTypes.Concat(markets).ToList();

                return Json(treeData);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllotmentSearch(string code, string marketID, string allotmentTypeID, string profileID, string zone, string isDefault)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                {
                    code = "";
                }
                if (string.IsNullOrEmpty(marketID) || marketID == "0")
                {
                    marketID = "";
                }
                if (string.IsNullOrEmpty(allotmentTypeID) || allotmentTypeID == "0")
                {
                    allotmentTypeID = "";
                }
                if (string.IsNullOrEmpty(profileID))
                {
                    profileID = "";
                }
                if (string.IsNullOrEmpty(zone))
                {
                    zone = "";
                }
                if (string.IsNullOrEmpty(isDefault))
                {
                    isDefault = "";
                }
                DataTable myData = _iReservationService.GetAllotment(code, marketID, profileID, isDefault, zone, allotmentTypeID);
                List<MarketModel> listMarketType = PropertyUtils.ConvertToList<MarketModel>(MarketBO.Instance.FindByAttribute("Inactive", 0));
                List<ProfileModel> listProfile = PropertyUtils.ConvertToList<ProfileModel>(ProfileBO.Instance.FindByAttribute("Active", "false"));
                List<AllotmentTypeModel> listAllotmentType = PropertyUtils.ConvertToList<AllotmentTypeModel>(AllotmentTypeBO.Instance.FindByAttribute("Inactive", 0));

                var result = (from d in myData.AsEnumerable()
                              let marketIDs = d["MarketID"].ToString()
                              let profileIDs = d["MarketID"].ToString()
                              let allotmentTypeIDs = d["AllotmentTypeID"].ToString()
                              let matchedMarket = listMarketType.FirstOrDefault(m => m.ID.ToString() == marketIDs)
                              let matchedProfile = listProfile.FirstOrDefault(m => m.ID.ToString() == profileIDs)
                              let allotmentType = listAllotmentType.FirstOrDefault(m => m.ID.ToString() == allotmentTypeIDs)

                              select new
                              {
                                  ID = d["ID"].ToString(),
                                  Code = d["Code"].ToString(),
                                  AllotmentName = d["AllotmentName"].ToString(),
                                  AccountName = d["AccountName"].ToString(),
                                  MarketID = d["MarketID"].ToString(),
                                  Market = matchedMarket != null ? matchedMarket.Code : "",
                                  Profile = matchedProfile != null ? matchedProfile.LastName : "",
                                  AllotmentType = allotmentType != null ? allotmentType.Code : "",
                                  CuttOfDay = d["CuttOfDay"].ToString(),
                                  CuttOfDate = d["CuttOfDate"].ToString(),
                                  AllotmentTypeID = d["AllotmentTypeID"].ToString(),
                                  CreateBy = d["CreateBy"].ToString(),
                                  CreateDate = !string.IsNullOrEmpty(d["CreateDate"].ToString()) ? d["CreateDate"] : "",
                                  UpdateBy = d["UpdateBy"].ToString(),
                                  UpdateDate = !string.IsNullOrEmpty(d["UpdateDate"].ToString()) ? d["UpdateDate"] : "",
                                  ProfileID = d["ProfileID"].ToString(),
                                  IsDefault = d["IsDefault"].ToString(),

                              }).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllotmentSearchDetail(int allotmentID)
        {
            try
            {
                List<RoomTypeModel> roomTypeModels = PropertyUtils.ConvertToList<RoomTypeModel>(RoomTypeBO.Instance.FindByAttribute("Inactive", 0));
                string allCodes = string.Join(
                    ",",
                    roomTypeModels.Select(x => $"[{x.Code}]")
                );
                DateTime date = new DateTime(1900, 1, 1);
                DataTable myData = _iReservationService.GetAllotmentDetail(allotmentID, allCodes, date);

                var result = (from d in myData.AsEnumerable()
                              select d.Table.Columns.Cast<DataColumn>()
                                  .Where(col => col.ColumnName != "AllotmentStageID" && col.ColumnName != "flag"
                                  && col.ColumnName != "Total" && col.ColumnName != "From Date" && col.ColumnName != "To Date"
                                  && myData.AsEnumerable().Any(r =>
                                    !r.IsNull(col) && !string.IsNullOrWhiteSpace(r[col].ToString()))

                                    )
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

        [HttpGet]
        public async Task<IActionResult> GetAllPreference(string code, int preferenceGroup)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                {
                    code = "";
                }
                DataTable myData = _iReservationService.GetReservationPreference(code, preferenceGroup);

                var result = (from d in myData.AsEnumerable()

                              select new
                              {
                                  PreferenceID = d["PreferenceID"].ToString(),
                                  Code = d["Code"].ToString(),
                                  Description = d["Description"].ToString(),


                              }).ToList();


                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ReservationRateQueryDetail(DateTime fromDate, DateTime toDate, int roomType, int adults, int noOfNight, int packageID,
            int promotionID, int func, int display, int dayUse, int c1, int c2, int c3, int noOfRoom, string currency)
        {
            try
            {
                string tableName = "VwRatRateQuery";
                string onRows = "RateCode";
                string onRowsAlias = "RateCode";
                string onCols = "Code";
                string sumcol = "A2";

                DataTable myData = _iReservationService.ReservationRateQueryDetail(fromDate, toDate, roomType, adults, noOfNight, packageID,
                 promotionID, tableName, onRows, onRowsAlias, onCols, sumcol, func, currency, display, dayUse, c1, c2, c3, noOfRoom);

                var result = (from d in myData.AsEnumerable()
                              select d.Table.Columns.Cast<DataColumn>()
                                  //.Where(col => col.ColumnName != "AllotmentStageID" && col.ColumnName != "flag" && col.ColumnName != "Total")
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

        [HttpGet]
        public async Task<IActionResult> CaculateNet(DateTime fromDate, DateTime toDate, int rateCodeID, int roomTypeID,
            string currencyID, int packageID, int day, decimal price, string transactionCode, decimal discountPercent, decimal discountAmount)
        {
            try
            {
                if (rateCodeID != 0)
                {
                    var data = _iReservationService.ReservationGetRateQueryDetail(fromDate, toDate, rateCodeID, roomTypeID, currencyID, packageID, day);
                    transactionCode = data.Rows[0]["TransactionCode"].ToString();
                }
                else
                {

                    transactionCode = PropertyUtils.ConvertToList<ConfigSystemModel>(ConfigSystemBO.Instance.FindAll()).
                    Where(x => x.KeyName == "RoomCharge").ToList()[0].KeyValue;

                }
                var (originalPrice, priceAfter, priceDiscount, priceAfterDiscount) = _iReservationService.CalculateNet(price, transactionCode, discountAmount, discountPercent);

                // Tạo đối tượng JSON để trả về
                var result = new
                {
                    Price = originalPrice,
                    PriceAfter = priceAfter,
                    PriceDiscount = priceDiscount,
                    PriceAfterDiscount = priceAfterDiscount
                };
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        [HttpGet]
        public async Task<IActionResult> CaculateNetReverse(DateTime fromDate, DateTime toDate, int rateCodeID, int roomTypeID,
    string currencyID, int packageID, int day, decimal price, string transactionCode, decimal discountPercent, decimal discountAmount)
        {
            try
            {
                if (rateCodeID != 0)
                {
                    var data = _iReservationService.ReservationGetRateQueryDetail(fromDate, toDate, rateCodeID, roomTypeID, currencyID, packageID, day);
                    transactionCode = data.Rows[0]["TransactionCode"].ToString();
                }
                else
                {

                    transactionCode = PropertyUtils.ConvertToList<ConfigSystemModel>(ConfigSystemBO.Instance.FindAll()).
                    Where(x => x.KeyName == "RoomCharge").ToList()[0].KeyValue;

                }
                var (originalPrice, priceAfter, priceDiscount, priceAfterDiscount) = _iReservationService.CalculateNetReverse(price, transactionCode, discountAmount, discountPercent);

                // Tạo đối tượng JSON để trả về
                var result = new
                {
                    Price = originalPrice,
                    PriceAfter = priceAfter,
                    PriceDiscount = priceDiscount,
                    PriceAfterDiscount = priceAfterDiscount
                };
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetBusinessDate()
        {
            try
            {
                List<BusinessDateModel> listMarket = PropertyUtils.ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll());


                return Json(listMarket[0].BusinessDate);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetReasonWaitList()
        {
            try
            {
                List<CommentModel> ressult = PropertyUtils.ConvertToList<CommentModel>(CommentBO.Instance.FindByAttribute("CommentTypeID", 3));


                return Json(ressult);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPriority()
        {
            try
            {
                List<PriorityModel> ressult = PropertyUtils.ConvertToList<PriorityModel>(PriorityBO.Instance.FindByAttribute("Inactive", 0));


                return Json(ressult);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRoomType()
        {
            try
            {
                List<RoomTypeModel> ressult = PropertyUtils.ConvertToList<RoomTypeModel>(RoomTypeBO.Instance.FindByAttribute("Inactive", 0));


                return Json(ressult);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetMarket()
        {
            try
            {
                List<MarketModel> ressult = PropertyUtils.ConvertToList<MarketModel>(MarketBO.Instance.FindByAttribute("Inactive", 0));


                return Json(ressult);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
        #endregion

        #region DatVP __ save reservation
        [HttpPost]
        public ActionResult SaveReservation()
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();
                List<BusinessDateModel> businessDate = PropertyUtils.ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll());

                ConfigSystemModel configSystems = PropertyUtils.ConvertToList<ConfigSystemModel>(ConfigSystemBO.Instance.FindByAttribute("KeyName", "RoomChargeVND")).FirstOrDefault();

                int roomTypeID = 0; int vipID = 0;
                int.TryParse(Request.Form["memberType"].ToString(), out int memberTypeID);

                if (string.IsNullOrEmpty(Request.Form["vipID"].ToString()))
                {
                    vipID = 0;
                }
                if (!string.IsNullOrEmpty(Request.Form["roomTypeID"].ToString()))
                {
                    roomTypeID = int.Parse(Request.Form["roomTypeID"].ToString());

                }
                if (roomTypeID == 0)
                {
                    return Json(new { code = 1, msg = "Room Type cannot be blank" });

                }
                if (string.IsNullOrEmpty(Request.Form["profileIndividualID"].ToString()))
                {
                    return Json(new { code = 1, msg = "Profile cannot be blank" });
                }
                if (string.IsNullOrEmpty(Request.Form["reservationTypeCode"].ToString()))
                {
                    return Json(new { code = 1, msg = "Reservation Type cannot be blank" });
                }
                if (string.IsNullOrEmpty(Request.Form["marketID"].ToString()))
                {
                    return Json(new { code = 1, msg = "Market cannot be blank" });

                }
                var nationalitycheck = Request.Form["nationality"];

                if (string.IsNullOrWhiteSpace(nationalitycheck) || nationalitycheck == "0")
                {
                    return Json(new { code = 1, msg = "Nationality cannot be blank" });
                }
                string itemInventoryString = Request.Form["itemInventory"].ToString();
                List<int> itemInventory = itemInventoryString
                .Split(',')
                .Where(x => !string.IsNullOrWhiteSpace(x))   // khác "" và " "
                .Select(int.Parse)
                .Where(x => x != 0)
                .ToList();


                string itemInventoryRes = "";
                if (itemInventory.Count > 0)
                {
                    foreach (var item in itemInventory)
                    {
                        ItemModel itemModel = (ItemModel)ItemBO.Instance.FindByPrimaryKey(item);
                        if (itemModel != null && itemModel.ID != 0)
                        {

                            itemInventoryRes += itemModel.Code + ",";

                        }
                    }
                }
                MemberTypeModel memberType = (MemberTypeModel)MemberTypeBO.Instance.FindByPrimaryKey(memberTypeID);
                VIPModel vip = (VIPModel)VIPBO.Instance.FindByPrimaryKey(vipID);
                RoomTypeModel roomType = (RoomTypeModel)RoomTypeBO.Instance.FindByPrimaryKey(roomTypeID);
                if (int.Parse(Request.Form["reservationID"].ToString()) == 0)
                {
                    ReservationModel reservationModel = new ReservationModel();
                    #region lưu reservation
                    reservationModel.ConfirmationNo = (ReservationBO.GetTopConfirmationNo() + 1).ToString();
                    reservationModel.ReservationNo = (ReservationBO.GetTopID() + 1).ToString();
                    reservationModel.ReservationDate = businessDate[0].BusinessDate;
                    reservationModel.ProfileAgentId = string.IsNullOrEmpty(Request.Form["profileAgentID"].ToString()) ? 0 : int.Parse(Request.Form["profileAgentID"].ToString());
                    reservationModel.AgentName = Request.Form["agentName"].ToString();
                    reservationModel.ProfileCompanyId = string.IsNullOrEmpty(Request.Form["profileCompanyID"].ToString()) ? 0 : int.Parse(Request.Form["profileCompanyID"].ToString());
                    reservationModel.CompanyName = Request.Form["companyName"].ToString();
                    reservationModel.ProfileSourceId = 0;
                    reservationModel.SourceName = "";
                    reservationModel.ProfileGroupId = 0;
                    reservationModel.GroupCode = Request.Form["groupCode"].ToString();
                    reservationModel.GroupName = "";
                    reservationModel.ProfileContactId = string.IsNullOrEmpty(Request.Form["profileContactID"].ToString()) ? 0 : int.Parse(Request.Form["profileContactID"].ToString());
                    reservationModel.ContactName = Request.Form["contactName"].ToString();
                    reservationModel.ContactPhone = Request.Form["contactPhone"].ToString();
                    reservationModel.ProfileComment = "";
                    reservationModel.ProfileIndividualId = int.Parse(Request.Form["profileIndividualID"].ToString());
                    reservationModel.LastName = Request.Form["lastName"].ToString();
                    reservationModel.FirstName = Request.Form["firstName"].ToString();
                    if (Request.Form["title"].ToString() != "0")
                    {
                        TitleModel title = (TitleModel)TitleBO.Instance.FindByPrimaryKey(int.Parse(Request.Form["title"].ToString()));
                        reservationModel.Title = title.Code;
                    }
                    else
                    {
                        reservationModel.Title = Request.Form["title"].ToString();

                    }
                    reservationModel.Phone = Request.Form["phone"].ToString();
                    reservationModel.Email = Request.Form["email"].ToString();
                    if (int.Parse(Request.Form["walkIn"].ToString()) == 1)
                    {
                        reservationModel.IsWalkIn = true;
                    }
                    else
                    {
                        reservationModel.IsWalkIn = false;

                    }
                    if (memberType != null)
                    {
                        reservationModel.MemberType = memberType.Code;
                        reservationModel.MemberLevel = memberType.Level;
                    }
                    else
                    {
                        reservationModel.MemberType = "";
                        reservationModel.MemberLevel = "";
                    }
                    reservationModel.MemberNo = Request.Form["memberNo"].ToString();
                    if (vip != null)
                    {
                        reservationModel.VipId = vip.ID;
                        reservationModel.Vip = vip.Code;
                    }
                    else
                    {
                        reservationModel.VipId = 0;
                        reservationModel.Vip = "";
                    }
                    reservationModel.Address = Request.Form["address"].ToString();
                    reservationModel.City = Request.Form["city"].ToString();
                    reservationModel.Zip = "";
                    reservationModel.State = "";
                    if (Request.Form["nationality"].ToString() != "0")
                    {
                        NationalityModel nationality = (NationalityModel)NationalityBO.Instance.FindByPrimaryKey(int.Parse(Request.Form["nationality"].ToString()));
                        reservationModel.Country = nationality.Code;
                    }
                    else
                    {
                        reservationModel.Country = "";

                    }
                    reservationModel.Language = "";
                    reservationModel.ArrivalDate = DateTime.Parse(Request.Form["arrival"].ToString());
                    reservationModel.OriginalArrivalDate = DateTime.Parse(Request.Form["arrival"].ToString());
                    reservationModel.NoOfNight = int.Parse(Request.Form["noOfNight"].ToString());
                    reservationModel.DepartureDate = DateTime.Parse(Request.Form["departure"].ToString());
                    reservationModel.OriginalDepartureDate = DateTime.Parse(Request.Form["departure"].ToString());
                    reservationModel.NoOfAdult = int.Parse(Request.Form["noOfAdult"].ToString());
                    reservationModel.NoOfChild = int.Parse(Request.Form["noOfChild"].ToString());
                    reservationModel.NoOfChild1 = int.Parse(Request.Form["noOfChild1"].ToString());
                    reservationModel.NoOfChild2 = int.Parse(Request.Form["noOfChild2"].ToString());
                    reservationModel.NoOfRoom = int.Parse(Request.Form["noOfRoom"].ToString());
                    if (roomType != null)
                    {
                        reservationModel.RoomTypeId = roomType.ID;
                        reservationModel.RoomType = roomType.Code;
                    }
                    else
                    {
                        reservationModel.RoomTypeId = 0;
                        reservationModel.RoomType = "";
                    }
                    reservationModel.RtcId = int.Parse(Request.Form["rtcID"].ToString());
                    if (string.IsNullOrEmpty(Request.Form["roomNo"].ToString()))
                    {
                        reservationModel.RoomId = 0;
                        reservationModel.RoomNo = "";
                    }
                    else
                    {
                        reservationModel.RoomId = int.Parse(Request.Form["roomID"].ToString());
                        reservationModel.RoomNo = Request.Form["roomNo"].ToString();
                    }

                    reservationModel.BusinessBlockId = 0;
                    reservationModel.BusinessBlockCode = "";
                    reservationModel.Eta = !string.IsNullOrEmpty(Request.Form["eta"].ToString()) ? Request.Form["eta"].ToString() : "";
                    reservationModel.CheckInDate = DateTime.Parse(Request.Form["arrival"].ToString());
                    reservationModel.Etd = !string.IsNullOrEmpty(Request.Form["etd"].ToString()) ? Request.Form["etd"].ToString() : "";
                    reservationModel.CheckOutDate = DateTime.Parse(Request.Form["departure"].ToString());
                    reservationModel.ReservationTypeId = int.Parse(Request.Form["reservationType"].ToString());
                    reservationModel.ReservationTypeCode = Request.Form["reservationTypeCode"].ToString();
                    if (string.IsNullOrEmpty(Request.Form["marketID"].ToString()))
                    {
                        reservationModel.MarketId = 0;

                    }
                    else
                    {
                        reservationModel.MarketId = int.Parse(Request.Form["marketID"].ToString());

                    }
                    reservationModel.MarketCode = Request.Form["marketCode"].ToString();
                    reservationModel.SourceId = int.Parse(Request.Form["sourceID"].ToString());
                    reservationModel.SourceCode = Request.Form["sourceCode"].ToString();
                    reservationModel.OriginId = 0;
                    reservationModel.OriginCode = "";
                    reservationModel.CCHolder = "";
                    if (string.IsNullOrEmpty(Request.Form["bookerID"].ToString()))
                    {
                        reservationModel.BookerId = 0;
                        reservationModel.BookerName = "";
                    }
                    else
                    {
                        reservationModel.BookerId = int.Parse(Request.Form["bookerID"].ToString());
                        reservationModel.BookerName = Request.Form["bookerName"].ToString();
                    }

                    reservationModel.BookerDetails = "";
                    if (int.Parse(Request.Form["noPost"].ToString()) == 1)
                    {
                        reservationModel.NoPost = true;

                    }
                    else
                    {
                        reservationModel.NoPost = false;

                    }
                    if (int.Parse(Request.Form["printRate"].ToString()) == 1)
                    {
                        reservationModel.PrintRate = true;

                    }
                    else
                    {
                        reservationModel.PrintRate = false;

                    }
                    reservationModel.ConfirmationStatus = true;
                    reservationModel.VideoCheckOutStatus = false;
                    reservationModel.CRSNo = "";
                    reservationModel.DiscountAmount = decimal.Parse(Request.Form["discountAmount"].ToString());
                    reservationModel.DiscountRate = decimal.Parse(Request.Form["discountRate"].ToString());
                    reservationModel.DiscountReason = Request.Form["discountReason"].ToString();
                    reservationModel.Comment = Request.Form["comment"].ToString();
                    reservationModel.BalanceUSD = 0;
                    reservationModel.BalanceVND = decimal.Parse(Request.Form["rateAfter"].ToString());
                    reservationModel.ApprovalCode = "";
                    reservationModel.ApprovalAmount = 0;
                    reservationModel.SuiteWith = "";
                    reservationModel.PaymentMethod = "";
                    reservationModel.CreditCardNo = "";
                    reservationModel.ExpirationDate = DateTime.Now;
                    reservationModel.TaxTypeId = 0;
                    reservationModel.ExemptNumber = "";
                    reservationModel.PickupReqdId = int.Parse(Request.Form["pickedId"].ToString());
                    reservationModel.PickupTransportType = Request.Form["pickUpTransportType"].ToString();
                    reservationModel.PickupStationCode = Request.Form["pickUpStationCode"].ToString();
                    reservationModel.PickupCarrierCode = Request.Form["pickUpCarrierCode"].ToString();
                    reservationModel.PickupTime = Request.Form["pickUpTime"].ToString();
                    reservationModel.PickupTransportNo = Request.Form["pickUpTransportNo"].ToString();
                    reservationModel.PickupArrivalDate = DateTime.Parse(Request.Form["pickUpDate"].ToString());
                    reservationModel.PickupDescription = Request.Form["pickUpDescription"].ToString();
                    reservationModel.DropOffReqdId = int.Parse(Request.Form["dropOffId"].ToString());
                    reservationModel.DropOffTransportType = Request.Form["dropOffTransportType"].ToString();
                    reservationModel.DropOffStationCode = Request.Form["dropOffStationCode"].ToString();
                    reservationModel.DropOffCarrierCode = Request.Form["dropOffCarrierCode"].ToString();
                    reservationModel.DropOffTime = Request.Form["dropOffTime"].ToString();
                    reservationModel.DropOffTransportNo = Request.Form["dropOffTransportNo"].ToString();
                    reservationModel.DropOffDepartureDate = DateTime.Parse(Request.Form["dropOffDate"].ToString());
                    reservationModel.DropOffDescription = Request.Form["dropOffDescription"].ToString();
                    reservationModel.PackageId = int.Parse(Request.Form["packageID"].ToString());
                    reservationModel.Packages = Request.Form["packages"].ToString();
                    reservationModel.Relationship = ReservationBO.GetTopID() + 1;
                    reservationModel.Status = DateTime.Parse(Request.Form["arrival"].ToString()) == businessDate[0].BusinessDate ? 5 : 0;
                    reservationModel.PostingMaster = false;
                    reservationModel.MainGuest = true;
                    if (string.IsNullOrEmpty(Request.Form["rateCode"].ToString()))
                    {
                        reservationModel.RateCodeId = 0;
                        reservationModel.RateCode = "";
                    }
                    else
                    {
                        reservationModel.RateCodeId = int.Parse(Request.Form["rateCodeID"].ToString());
                        reservationModel.RateCode = Request.Form["rateCode"].ToString();
                    }
                    reservationModel.Rate = decimal.Parse(Request.Form["rateAmount"].ToString());
                    reservationModel.RateAfterTax = decimal.Parse(Request.Form["rateAfter"].ToString());
                    reservationModel.FixedRate = false;
                    reservationModel.TotalAmount = decimal.Parse(Request.Form["rateAmount"].ToString());
                    reservationModel.CurrencyId = "VND";
                    reservationModel.Party = "";
                    reservationModel.PartyGuest = "";
                    reservationModel.IsPasserBy = false;
                    string color = Request.Form["color"].ToString();
                    //reservationModel.Color = Request.Form["color"].ToString();
                    reservationModel.Color = "";

                    reservationModel.ARNo = "";
                    reservationModel.ItemInventory = itemInventoryRes;
                    reservationModel.Specials = Request.Form["specials"].ToString();
                    reservationModel.ShareRoom = ReservationBO.GetTopID() + 1;
                    reservationModel.NoShowStatus = false;
                    reservationModel.ShareRoomName = "";
                    reservationModel.AccompanyName = "";
                    reservationModel.RoutingTransaction = "";
                    reservationModel.RoutingToProfile = Request.Form["firstName"].ToString();
                    reservationModel.FixedCharge = "";
                    reservationModel.CommentGroup = "";
                    reservationModel.UserInsertId = int.Parse(Request.Form["userID"].ToString());
                    reservationModel.CreateDate = DateTime.Now;
                    reservationModel.UserUpdateId = int.Parse(Request.Form["userID"].ToString());
                    reservationModel.UpdateDate = DateTime.Now;
                    reservationModel.CreateBy = Request.Form["userName"].ToString();
                    reservationModel.UpdateBy = Request.Form["userName"].ToString();
                    reservationModel.SpecialUpdateBy = Request.Form["userName"].ToString();
                    reservationModel.SpecialUpdateDate = DateTime.Now;
                    reservationModel.IsAdvanceBill = false;
                    if (string.IsNullOrEmpty(Request.Form["allotmentID"].ToString()))
                    {
                        reservationModel.AllotmentId = 0;
                        reservationModel.AllotmentCode = "";
                    }
                    else
                    {
                        reservationModel.AllotmentId = int.Parse(Request.Form["allotmentID"].ToString());
                        reservationModel.AllotmentCode = Request.Form["allotmentCode"].ToString();
                    }
                    reservationModel.PinCode = (ReservationBO.GetTopID() + 1).ToString();
                    reservationModel.PersonInChargeId = int.Parse(Request.Form["perrsonInCharge"].ToString());
                    reservationModel.RoomNight = int.Parse(Request.Form["roomNight"].ToString());
                    reservationModel.CardId = "";
                    reservationModel.Breakfast = false;
                    reservationModel.Dinner = false;
                    reservationModel.Lunch = false;
                    reservationModel.FixedMeal = false;
                    reservationModel.VoucherId = "";
                    int reservationID = Convert.ToInt32(ReservationBO.Instance.Insert(reservationModel));
                    reservationModel.ID = reservationID;
                    #endregion


                    #region lưu log activity insert reservation
                    ActivityLogModel activityLog = new ActivityLogModel();
                    activityLog.TableName = "Reservation";
                    activityLog.ObjectID = reservationModel.ID;
                    activityLog.UserID = int.Parse(Request.Form["userID"].ToString());
                    activityLog.UserName = Request.Form["userName"].ToString();
                    activityLog.ChangeDate = DateTime.Now;
                    activityLog.Change = "Insert";
                    TextUtils.FillValues(activityLog, (ReservationModel)null, reservationModel);
                    activityLog.Description = $"Create New Reservation: {reservationModel.ConfirmationNo}";
                    ActivityLogBO.Instance.Insert(activityLog);
                    #endregion

                    #region lưu reservation master
                    if (!string.IsNullOrEmpty(Request.Form["profileAgentID"].ToString()) || !string.IsNullOrEmpty(Request.Form["profileCompanyID"].ToString()))
                    {
                        ReservationModel reservationMaster = new ReservationModel();
                        reservationMaster = reservationModel;
                        reservationMaster.ReservationNo = "0";
                        reservationMaster.ProfileIndividualId = 0;
                        reservationMaster.LastName = "* " + reservationMaster.AgentName + ", Master *";
                        reservationMaster.FirstName = "Master *";
                        reservationMaster.NoOfAdult = 0;
                        reservationMaster.NoOfChild = reservationMaster.NoOfChild1 = reservationMaster.NoOfChild2 = 0;
                        reservationMaster.IsWalkIn = reservationMaster.IsWalkIn;
                        reservationMaster.RoomTypeId = 8;
                        reservationMaster.RoomType = "DMR";
                        reservationMaster.RtcId = 8;
                        reservationMaster.RoomId = 0;
                        reservationMaster.RoomNo = "";
                        reservationMaster.PostingMaster = true;
                        reservationMaster.MainGuest = false;
                        reservationMaster.ShareRoom = reservationModel.ShareRoom + 1;
                        reservationMaster.Status = DateTime.Parse(Request.Form["arrival"].ToString()) == businessDate[0].BusinessDate ? 5 : 0;
                        ReservationBO.Instance.Insert(reservationMaster);
                    }
                    #endregion

                    #region lưu reservation item inventory
                    if (itemInventory.Count > 0)
                    {
                        foreach (var item in itemInventory)
                        {
                            ItemModel itemModel = (ItemModel)ItemBO.Instance.FindByPrimaryKey(item);
                            if (itemModel != null && itemModel.ID != 0)
                            {
                                ReservationItemInventoryModel reservationItemInventory = new ReservationItemInventoryModel();
                                reservationItemInventory.ReservationID = (int)reservationID;
                                reservationItemInventory.ItemID = itemModel.ID;
                                reservationItemInventory.Code = itemModel.Code;
                                reservationItemInventory.Name = itemModel.Name;
                                reservationItemInventory.BeginDate = reservationModel.ArrivalDate;
                                reservationItemInventory.EndDate = reservationModel.DepartureDate;
                                reservationItemInventory.Quantity = 1;
                                reservationItemInventory.RateCode = "";
                                reservationItemInventory.PackageID = reservationModel.PackageId;
                                reservationItemInventory.Package = reservationModel.Packages;
                                reservationItemInventory.ReservationFixedChargeID = 0;
                                reservationItemInventory.UserInsertID = reservationItemInventory.UserUpdateID = int.Parse(Request.Form["userID"].ToString());
                                reservationItemInventory.CreateDate = reservationItemInventory.UpdateDate = DateTime.Now;
                                ReservationItemInventoryBO.Instance.Insert(reservationItemInventory);
                            }

                        }
                    }
                    #endregion

                    #region tạo folio
                    FolioModel folioModel = new FolioModel();
                    folioModel.ARNo = "";
                    folioModel.FolioDate = reservationModel.ReservationDate;
                    folioModel.FolioNo = 1;
                    folioModel.ReservationID = (int)reservationID;
                    folioModel.ProfileID = reservationModel.ProfileIndividualId;
                    folioModel.AccountName = reservationModel.LastName;
                    folioModel.Status = reservationModel.NoPost == true ? true : false;

                    folioModel.ConfirmationNo = reservationModel.ConfirmationNo;
                    folioModel.BalanceUSD = folioModel.BalanceVND = reservationModel.RateAfterTax;
                    folioModel.CreateDate = folioModel.UpdateDate = DateTime.Now;
                    folioModel.UserInsertID = folioModel.UserUpdateID = reservationModel.UserInsertId;
                    FolioBO.Instance.Insert(folioModel);
                    #endregion

                    #region lưu reservation amount currency
                    ReservationAmountByCurrencyModel reservationAmountCurrency = new ReservationAmountByCurrencyModel();
                    reservationAmountCurrency.ReservationID = (int)reservationID;
                    reservationAmountCurrency.ConfirmationNo = int.Parse(reservationModel.ConfirmationNo);
                    reservationAmountCurrency.CurrencyID = "VND";
                    reservationAmountCurrency.AmountAfterTax = reservationModel.Rate;
                    reservationAmountCurrency.AmountBeforTax = reservationModel.RateAfterTax;
                    reservationAmountCurrency.UserInsertID = reservationAmountCurrency.UserInsertID = int.Parse(Request.Form["userID"].ToString());
                    reservationAmountCurrency.CreateDate = reservationAmountCurrency.UpdateDate = DateTime.Now;
                    ReservationAmountByCurrencyBO.Instance.Insert(reservationAmountCurrency);
                    #endregion

                    #region update profile
                    int profileIndividualIDUpdate = int.Parse(Request.Form["profileIndividualID"].ToString());
                    ProfileModel profile = (ProfileModel)ProfileBO.Instance.FindByPrimaryKey(profileIndividualIDUpdate);
                    if (profile == null || profile.ID == 0)
                    {
                        pt.RollBack();
                        return Json(new { code = 1, msg = "Could not find profile" });
                    }
                    //string Title = Request.Form["Title"].ToString();
                    //string Phone = Request.Form["Phone"].ToString();
                    //string Passport = Request.Form["Passport"].ToString();
                    //string IdentityCard = Request.Form["IdentityCard"].ToString();
                    //string Email = Request.Form["Email"].ToString();
                    //string Address = Request.Form["Address"].ToString();
                    //string DateOfBirth = Request.Form["DateOfBirth"].ToString();
                    //string Nationality = Request.Form["Nationality"].ToString();
                    //string City = Request.Form["City"].ToString();
                    //string VIP = Request.Form["VIP"].ToString();
                    //string MemberType = Request.Form["MemberType"].ToString();
                    //string CardNo = Request.Form["CardNo"].ToString();
                    //string GroupCode = Request.Form["GroupCode"].ToString();
                    //string ContactPhone = Request.Form["ContactPhone"].ToString();
                    //string PrefRoom = Request.Form["PrefRoom"].ToString();
                    profile.TitleID = !string.IsNullOrEmpty(Request.Form["title"].ToString()) ? int.Parse(Request.Form["title"].ToString()) : 0;
                    profile.HandPhone = !string.IsNullOrEmpty(Request.Form["phone"].ToString()) ? Request.Form["phone"].ToString() : "";
                    profile.Email = !string.IsNullOrEmpty(Request.Form["email"].ToString()) ? Request.Form["email"].ToString() : "";
                    profile.Address = !string.IsNullOrEmpty(Request.Form["address"].ToString()) ? Request.Form["address"].ToString() : "";
                    profile.PassPort = !string.IsNullOrEmpty(Request.Form["passport"].ToString()) ? Request.Form["passport"].ToString() : "";
                    profile.IdentityCard = !string.IsNullOrEmpty(Request.Form["identityCard"].ToString()) ? Request.Form["identityCard"].ToString() : "";
                    profile.DateOfBirth = !string.IsNullOrEmpty(Request.Form["dateOfBirth"].ToString()) ? DateTime.Parse(Request.Form["dateOfBirth"].ToString()) : DateTime.Now;
                    profile.NationalityID = !string.IsNullOrEmpty(Request.Form["nationality"].ToString()) ? int.Parse(Request.Form["nationality"].ToString()) : 0;
                    profile.City = !string.IsNullOrEmpty(Request.Form["city"].ToString()) ? Request.Form["city"].ToString() : "";
                    profile.VIPID = !string.IsNullOrEmpty(Request.Form["vipID"].ToString()) ? int.Parse(Request.Form["vipID"].ToString()) : 0;
                    profile.CreditCard = !string.IsNullOrEmpty(Request.Form["memberNo"].ToString()) ? Request.Form["memberNo"].ToString() : "";
                    ProfileBO.Instance.Update(profile);
                    #endregion

                    #region lưu reservation rate
                    ReservationRateModel reservationRate = new ReservationRateModel();
                    reservationRate.ReservationID = (int)reservationID;
                    reservationRate.RateCodeID = reservationModel.RateCodeId;
                    reservationRate.RateDate = businessDate[0].BusinessDate;
                    reservationRate.Rate = reservationModel.Rate;
                    reservationRate.FixedRate = false;
                    reservationRate.CurrencyID = "VND";
                    reservationRate.RoomID = reservationModel.RoomId;
                    reservationRate.RoomNo = reservationModel.RoomNo;
                    reservationRate.RoomTypeID = reservationModel.RoomTypeId;
                    reservationRate.RoomType = reservationModel.RoomType;
                    reservationRate.ChangeRateStatus = false;
                    reservationRate.TransactionCode = configSystems.KeyValue;
                    reservationRate.RoomRevenueBeforeTax = reservationModel.Rate;
                    reservationRate.RoomRevenueAfterTax = reservationModel.RateAfterTax;
                    reservationRate.DiscountAmount = reservationModel.DiscountAmount;
                    reservationRate.DiscountRate = reservationModel.DiscountRate;
                    reservationRate.RateAfterTax = reservationModel.RateAfterTax;
                    reservationRate.IsTaxInclude = false;
                    reservationRate.NoOfAdult = reservationModel.NoOfAdult;
                    reservationRate.NoOfChild = reservationModel.NoOfChild;
                    reservationRate.NoOfChild1 = reservationModel.NoOfChild1;
                    reservationRate.NoOfChild2 = reservationModel.NoOfChild2;
                    reservationRate.MarketID = reservationModel.MarketId;
                    reservationRate.SourceID = reservationModel.SourceId;
                    reservationRate.UserInsertID = reservationRate.UserUpdateID = int.Parse(Request.Form["userID"].ToString());
                    reservationRate.CreateDate = reservationRate.UpdateDate = DateTime.Now;
                    reservationRate.AllotmentID = reservationModel.AllotmentId;
                    reservationRate.FixedRateByUser = "";
                    reservationRate.RTCID = reservationModel.RtcId;
                    reservationRate.UpgradeBy = "";
                    reservationRate.UpgradeWhy = "";
                    reservationRate.DiscountReason = reservationModel.DiscountReason;
                    reservationRate.Breakfast = reservationRate.Lunch = reservationRate.Dinner = reservationRate.FixedMeal = false;
                    ReservationRateBO.Instance.Insert(reservationRate);
                    #endregion
                    pt.CommitTransaction();
                    return Json(new { code = 0, msg = $"The number Confirmation No : {reservationModel.ConfirmationNo}" });

                }
                else
                {
                    ReservationModel reservationModel = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(int.Parse(Request.Form["reservationID"].ToString()));
                    if (reservationModel == null || reservationModel.ID == 0)
                    {
                        return Json(new { code = 1, msg = "Could not fint reservation" });

                    }
                    ReservationModel oldReservationData = (ReservationModel)reservationModel.Clone();
                    #region edit reservation
                    reservationModel.ProfileAgentId = string.IsNullOrEmpty(Request.Form["profileAgentID"].ToString()) ? 0 : int.Parse(Request.Form["profileAgentID"].ToString());
                    reservationModel.AgentName = Request.Form["agentName"].ToString();
                    reservationModel.ProfileCompanyId = string.IsNullOrEmpty(Request.Form["profileCompanyID"].ToString()) ? 0 : int.Parse(Request.Form["profileCompanyID"].ToString());
                    reservationModel.CompanyName = Request.Form["companyName"].ToString();
                    reservationModel.ProfileSourceId = 0;
                    reservationModel.SourceName = "";
                    reservationModel.ProfileGroupId = 0;
                    reservationModel.GroupCode = Request.Form["groupCode"].ToString();
                    reservationModel.GroupName = "";
                    reservationModel.ProfileContactId = string.IsNullOrEmpty(Request.Form["profileContactID"].ToString()) ? 0 : int.Parse(Request.Form["profileContactID"].ToString());
                    reservationModel.ContactName = Request.Form["contactName"].ToString();
                    reservationModel.ContactPhone = Request.Form["contactPhone"].ToString();
                    reservationModel.ProfileComment = "";
                    reservationModel.ProfileIndividualId = int.Parse(Request.Form["profileIndividualID"].ToString());
                    reservationModel.LastName = Request.Form["lastName"].ToString();
                    reservationModel.FirstName = Request.Form["firstName"].ToString();
                    if (Request.Form["title"].ToString() != "0")
                    {
                        TitleModel title = (TitleModel)TitleBO.Instance.FindByPrimaryKey(int.Parse(Request.Form["title"].ToString()));
                        reservationModel.Title = title.Code;
                    }
                    else
                    {
                        reservationModel.Title = Request.Form["title"].ToString();

                    }
                    reservationModel.Phone = Request.Form["phone"].ToString();
                    reservationModel.Email = Request.Form["email"].ToString();
                    if (int.Parse(Request.Form["walkIn"].ToString()) == 1)
                    {
                        reservationModel.IsWalkIn = true;
                    }
                    else
                    {
                        reservationModel.IsWalkIn = false;

                    }
                    if (memberType != null)
                    {
                        reservationModel.MemberType = memberType.Code;
                        reservationModel.MemberLevel = memberType.Level;
                    }
                    else
                    {
                        reservationModel.MemberType = "";
                        reservationModel.MemberLevel = "";
                    }
                    reservationModel.MemberNo = Request.Form["memberNo"].ToString();
                    if (vip != null)
                    {
                        reservationModel.VipId = vip.ID;
                        reservationModel.Vip = vip.Code;
                    }
                    else
                    {
                        reservationModel.VipId = 0;
                        reservationModel.Vip = "";
                    }
                    reservationModel.Address = Request.Form["address"].ToString();
                    reservationModel.City = Request.Form["city"].ToString();
                    reservationModel.Zip = "";
                    reservationModel.State = "";
                    if (Request.Form["nationality"].ToString() != "null")
                    {
                        NationalityModel nationality = (NationalityModel)NationalityBO.Instance.FindByPrimaryKey(int.Parse(Request.Form["nationality"].ToString()));
                        reservationModel.Country = nationality.Code;
                    }
                    else
                    {
                        reservationModel.Country = "";

                    }
                    reservationModel.Language = "";
                    reservationModel.ArrivalDate = DateTime.Parse(Request.Form["arrival"].ToString());
                    reservationModel.OriginalArrivalDate = DateTime.Parse(Request.Form["arrival"].ToString());
                    reservationModel.NoOfNight = int.Parse(Request.Form["noOfNight"].ToString());
                    reservationModel.DepartureDate = DateTime.Parse(Request.Form["departure"].ToString());
                    reservationModel.OriginalDepartureDate = DateTime.Parse(Request.Form["departure"].ToString());
                    reservationModel.NoOfAdult = int.Parse(Request.Form["noOfAdult"].ToString());
                    reservationModel.NoOfChild = int.Parse(Request.Form["noOfChild"].ToString());
                    reservationModel.NoOfChild1 = int.Parse(Request.Form["noOfChild1"].ToString());
                    reservationModel.NoOfChild2 = int.Parse(Request.Form["noOfChild2"].ToString());
                    reservationModel.NoOfRoom = int.Parse(Request.Form["noOfRoom"].ToString());
                    if (roomType != null)
                    {
                        reservationModel.RoomTypeId = roomType.ID;
                        reservationModel.RoomType = roomType.Code;
                    }
                    else
                    {
                        reservationModel.RoomTypeId = 0;
                        reservationModel.RoomType = "";
                    }
                    reservationModel.RtcId = int.Parse(Request.Form["rtcID"].ToString());
                    if (string.IsNullOrEmpty(Request.Form["roomNo"].ToString()))
                    {
                        reservationModel.RoomId = 0;
                        reservationModel.RoomNo = "";
                    }
                    else
                    {
                        reservationModel.RoomId = int.Parse(Request.Form["roomID"].ToString());
                        reservationModel.RoomNo = Request.Form["roomNo"].ToString();
                    }

                    reservationModel.BusinessBlockId = 0;
                    reservationModel.BusinessBlockCode = "";
                    reservationModel.Eta = !string.IsNullOrEmpty(Request.Form["eta"].ToString()) ? Request.Form["eta"].ToString() : "";
                    reservationModel.CheckInDate = DateTime.Parse(Request.Form["arrival"].ToString());
                    reservationModel.Etd = !string.IsNullOrEmpty(Request.Form["etd"].ToString()) ? Request.Form["etd"].ToString() : "";
                    reservationModel.CheckOutDate = DateTime.Parse(Request.Form["arrival"].ToString());
                    reservationModel.ReservationTypeId = int.Parse(Request.Form["reservationType"].ToString());
                    reservationModel.ReservationTypeCode = Request.Form["reservationTypeCode"].ToString();
                    if (string.IsNullOrEmpty(Request.Form["marketID"].ToString()))
                    {
                        reservationModel.MarketId = 0;

                    }
                    else
                    {
                        reservationModel.MarketId = int.Parse(Request.Form["marketID"].ToString());

                    }
                    reservationModel.MarketCode = Request.Form["marketCode"].ToString();
                    reservationModel.SourceId = int.Parse(Request.Form["sourceID"].ToString());
                    reservationModel.SourceCode = Request.Form["sourceCode"].ToString();
                    reservationModel.OriginId = 0;
                    reservationModel.OriginCode = "";
                    reservationModel.CCHolder = "";
                    if (string.IsNullOrEmpty(Request.Form["bookerID"].ToString()))
                    {
                        reservationModel.BookerId = 0;
                        reservationModel.BookerName = "";
                    }
                    else
                    {
                        reservationModel.BookerId = int.Parse(Request.Form["bookerID"].ToString());
                        reservationModel.BookerName = Request.Form["bookerName"].ToString();
                    }

                    reservationModel.BookerDetails = "";
                    if (int.Parse(Request.Form["noPost"].ToString()) == 1)
                    {
                        reservationModel.NoPost = true;

                    }
                    else
                    {
                        reservationModel.NoPost = false;

                    }
                    if (int.Parse(Request.Form["printRate"].ToString()) == 1)
                    {
                        reservationModel.PrintRate = true;

                    }
                    else
                    {
                        reservationModel.PrintRate = false;

                    }
                    reservationModel.ConfirmationStatus = true;
                    reservationModel.VideoCheckOutStatus = false;
                    reservationModel.CRSNo = "";
                    reservationModel.DiscountAmount = decimal.Parse(Request.Form["discountAmount"].ToString());
                    reservationModel.DiscountRate = decimal.Parse(Request.Form["discountRate"].ToString());
                    reservationModel.DiscountReason =  Request.Form["discountReason"].ToString();
                    reservationModel.Comment = Request.Form["comment"].ToString();
                    reservationModel.BalanceUSD = 0;
                    reservationModel.BalanceVND = decimal.Parse(Request.Form["rateAfter"].ToString());
                    reservationModel.ApprovalCode = "";
                    reservationModel.ApprovalAmount = 0;
                    reservationModel.SuiteWith = "";
                    reservationModel.PaymentMethod = "";
                    reservationModel.CreditCardNo = "";
                    reservationModel.ExpirationDate = DateTime.Now;
                    reservationModel.TaxTypeId = 0;
                    reservationModel.ExemptNumber = "";
                    reservationModel.PickupReqdId = int.Parse(Request.Form["pickedId"].ToString());
                    reservationModel.PickupTransportType = Request.Form["pickUpTransportType"].ToString();
                    reservationModel.PickupStationCode = Request.Form["pickUpStationCode"].ToString();
                    reservationModel.PickupCarrierCode = Request.Form["pickUpCarrierCode"].ToString();
                    reservationModel.PickupTime = Request.Form["pickUpTime"].ToString();
                    reservationModel.PickupTransportNo = Request.Form["pickUpTransportNo"].ToString();
                    reservationModel.PickupArrivalDate = DateTime.Parse(Request.Form["pickUpDate"].ToString());
                    reservationModel.PickupDescription = Request.Form["pickUpDescription"].ToString();
                    reservationModel.DropOffReqdId = int.Parse(Request.Form["dropOffId"].ToString());
                    reservationModel.DropOffTransportType = Request.Form["dropOffTransportType"].ToString();
                    reservationModel.DropOffStationCode = Request.Form["dropOffStationCode"].ToString();
                    reservationModel.DropOffCarrierCode = Request.Form["dropOffCarrierCode"].ToString();
                    reservationModel.DropOffTime = Request.Form["dropOffTime"].ToString();
                    reservationModel.DropOffTransportNo = Request.Form["dropOffTransportNo"].ToString();
                    reservationModel.DropOffDepartureDate = DateTime.Parse(Request.Form["dropOffDate"].ToString());
                    reservationModel.DropOffDescription = Request.Form["dropOffDescription"].ToString();
                    reservationModel.PackageId = int.Parse(Request.Form["packageID"].ToString());
                    reservationModel.Packages = Request.Form["packages"].ToString();
                    reservationModel.Status = DateTime.Parse(Request.Form["arrival"].ToString()) == businessDate[0].BusinessDate ? 5 : 0;
                    reservationModel.PostingMaster = false;
                    //reservationModel.MainGuest = true;
                    if (string.IsNullOrEmpty(Request.Form["rateCode"].ToString()))
                    {
                        reservationModel.RateCodeId = 0;
                        reservationModel.RateCode = "";
                    }
                    else
                    {
                        reservationModel.RateCodeId = int.Parse(Request.Form["rateCodeID"].ToString());
                        reservationModel.RateCode = Request.Form["rateCode"].ToString();
                    }
                    reservationModel.Rate = decimal.Parse(Request.Form["rateAmount"].ToString());
                    reservationModel.RateAfterTax = decimal.Parse(Request.Form["rateAfter"].ToString());
                    reservationModel.FixedRate = false;
                    reservationModel.TotalAmount = decimal.Parse(Request.Form["rateAmount"].ToString());
                    reservationModel.CurrencyId = "VND";
                    reservationModel.Party = "";
                    reservationModel.PartyGuest = "";
                    reservationModel.IsPasserBy = false;
                    //reservationModel.Color = Request.Form["color"].ToString();
                    string colorres = Request.Form["color"].ToString();
                    if (!string.IsNullOrWhiteSpace(colorres) && colorres != "#000000")
                    {
                        System.Drawing.Color color = ColorTranslator.FromHtml(colorres);
                        reservationModel.Color = color.ToArgb().ToString();
                    }
                    else
                    {
                        reservationModel.Color = ""; // hoặc "" tùy kiểu DB
                    }
                    reservationModel.ARNo = "";
                    reservationModel.ItemInventory = Request.Form["itemInventory"].ToString();
                    reservationModel.Specials = Request.Form["specials"].ToString();
                    reservationModel.NoShowStatus = false;
                    reservationModel.ShareRoomName = "";
                    reservationModel.AccompanyName = "";
                    reservationModel.RoutingTransaction = "";
                    reservationModel.RoutingToProfile = Request.Form["firstName"].ToString();
                    reservationModel.FixedCharge = "";
                    reservationModel.CommentGroup = "";
                    reservationModel.UserInsertId = int.Parse(Request.Form["userID"].ToString());
                    reservationModel.CreateDate = DateTime.Now;
                    reservationModel.UserUpdateId = int.Parse(Request.Form["userID"].ToString());
                    reservationModel.UpdateDate = DateTime.Now;
                    reservationModel.CreateBy = Request.Form["userName"].ToString();
                    reservationModel.UpdateBy = Request.Form["userName"].ToString();
                    reservationModel.SpecialUpdateBy = Request.Form["userName"].ToString();
                    reservationModel.SpecialUpdateDate = DateTime.Now;
                    reservationModel.IsAdvanceBill = false;
                    if (string.IsNullOrEmpty(Request.Form["allotmentID"].ToString()))
                    {
                        reservationModel.AllotmentId = 0;
                        reservationModel.AllotmentCode = "";
                    }
                    else
                    {
                        reservationModel.AllotmentId = int.Parse(Request.Form["allotmentID"].ToString());
                        reservationModel.AllotmentCode = Request.Form["allotmentCode"].ToString();
                    }
                    reservationModel.PersonInChargeId = int.Parse(Request.Form["perrsonInCharge"].ToString());
                    reservationModel.RoomNight = int.Parse(Request.Form["roomNight"].ToString());
                    reservationModel.CardId = "";
                    reservationModel.Breakfast = false;
                    reservationModel.Dinner = false;
                    reservationModel.Lunch = false;
                    reservationModel.FixedMeal = false;
                    reservationModel.VoucherId = "";
                    ReservationBO.Instance.Update(reservationModel);
                    #endregion

                    #region lưu log activity update reservation
                    ActivityLogModel activityLog = new ActivityLogModel();
                    activityLog.TableName = "Reservation";
                    activityLog.ObjectID = reservationModel.ID;
                    activityLog.UserID = int.Parse(Request.Form["userID"].ToString());
                    activityLog.UserName = Request.Form["userName"].ToString();
                    activityLog.ChangeDate = DateTime.Now;
                    activityLog.Change = "Update";
                    TextUtils.FillValues(activityLog, oldReservationData, reservationModel);
                    activityLog.Description = $"Update Reservation: {reservationModel.ConfirmationNo}";
                    ActivityLogBO.Instance.Insert(activityLog);
                    #endregion

                    #region edit reservation rate
                    ReservationRateModel reservationRate = PropertyUtils.ConvertToList<ReservationRateModel>(ReservationRateBO.Instance.FindByAttribute("ReservationID", reservationModel.ID)).FirstOrDefault();
                    if (reservationRate != null)
                    {
                        reservationRate.RateCodeID = reservationModel.RateCodeId;
                        reservationRate.RateDate = businessDate[0].BusinessDate;
                        reservationRate.Rate = reservationModel.Rate;
                        reservationRate.RoomID = reservationModel.RoomId;
                        reservationRate.RoomNo = reservationModel.RoomNo;
                        reservationRate.RoomTypeID = reservationModel.RoomTypeId;
                        reservationRate.RoomType = reservationModel.RoomType;
                        reservationRate.RoomRevenueBeforeTax = reservationModel.Rate;
                        reservationRate.RoomRevenueAfterTax = reservationModel.RateAfterTax;
                        reservationRate.DiscountAmount = reservationModel.DiscountAmount;
                        reservationRate.DiscountRate = reservationModel.DiscountRate;
                        reservationRate.RateAfterTax = reservationModel.RateAfterTax;
                        reservationRate.NoOfAdult = reservationModel.NoOfAdult;
                        reservationRate.NoOfChild = reservationModel.NoOfChild;
                        reservationRate.NoOfChild1 = reservationModel.NoOfChild1;
                        reservationRate.NoOfChild2 = reservationModel.NoOfChild2;
                        reservationRate.MarketID = reservationModel.MarketId;
                        reservationRate.SourceID = reservationModel.SourceId;
                        reservationRate.UserUpdateID = int.Parse(Request.Form["userID"].ToString());
                        reservationRate.UpdateDate = DateTime.Now;
                        reservationRate.AllotmentID = reservationModel.AllotmentId;
                        reservationRate.RTCID = reservationModel.RtcId;
                        reservationRate.DiscountReason = reservationModel.DiscountReason;
                        ReservationRateBO.Instance.Update(reservationRate);
                    }
                    #endregion

                    #region update profile
                    ProfileModel profile = (ProfileModel)ProfileBO.Instance.FindByPrimaryKey(int.Parse(Request.Form["profileIndividualID"].ToString()));
                    //data cũ để viết log 
                    ProfileModel oldProfileData = (ProfileModel)profile.Clone();
                    if (profile == null || profile.ID == 0)
                    {
                        pt.RollBack();
                        return Json(new { code = 1, msg = "Could not find profile" });
                    }
                    //string Title = Request.Form["Title"].ToString();
                    //string Phone = Request.Form["Phone"].ToString();
                    //string Passport = Request.Form["Passport"].ToString();
                    //string IdentityCard = Request.Form["IdentityCard"].ToString();
                    //string Email = Request.Form["Email"].ToString();
                    //string Address = Request.Form["Address"].ToString();
                    //string DateOfBirth = Request.Form["DateOfBirth"].ToString();
                    //string Nationality = Request.Form["Nationality"].ToString();
                    //string City = Request.Form["City"].ToString();
                    //string VIP = Request.Form["VIP"].ToString();
                    //string MemberType = Request.Form["MemberType"].ToString();
                    //string CardNo = Request.Form["CardNo"].ToString();
                    //string GroupCode = Request.Form["GroupCode"].ToString();
                    //string ContactPhone = Request.Form["ContactPhone"].ToString();
                    //string PrefRoom = Request.Form["PrefRoom"].ToString();
                    profile.TitleID = !string.IsNullOrEmpty(Request.Form["title"].ToString()) ? int.Parse(Request.Form["title"].ToString()) : 0;
                    profile.RoomNights = int.Parse(Request.Form["roomNight"].ToString());
                    profile.HandPhone = !string.IsNullOrEmpty(Request.Form["phone"].ToString()) ? Request.Form["phone"].ToString() : "";
                    profile.Email = !string.IsNullOrEmpty(Request.Form["email"].ToString()) ? Request.Form["email"].ToString() : "";
                    profile.Address = !string.IsNullOrEmpty(Request.Form["address"].ToString()) ? Request.Form["address"].ToString() : "";
                    profile.PassPort = !string.IsNullOrEmpty(Request.Form["passport"].ToString()) ? Request.Form["passport"].ToString() : "";
                    profile.IdentityCard = !string.IsNullOrEmpty(Request.Form["identityCard"].ToString()) ? Request.Form["identityCard"].ToString() : "";
                    profile.DateOfBirth = !string.IsNullOrEmpty(Request.Form["dateOfBirth"].ToString()) ? DateTime.Parse(Request.Form["dateOfBirth"].ToString()) : DateTime.Now;
                    profile.NationalityID = !string.IsNullOrEmpty(Request.Form["nationality"].ToString()) ? int.Parse(Request.Form["nationality"].ToString()) : 0;
                    profile.City = !string.IsNullOrEmpty(Request.Form["city"].ToString()) ? Request.Form["city"].ToString() : "";
                    profile.VIPID = !string.IsNullOrEmpty(Request.Form["vipID"].ToString()) ? int.Parse(Request.Form["vipID"].ToString()) : 0;
                    profile.CreditCard = !string.IsNullOrEmpty(Request.Form["memberNo"].ToString()) ? Request.Form["memberNo"].ToString() : "";
                    ProfileBO.Instance.Update(profile);
                    #endregion

                    #region Viết log cho profile nếu update
                    ActivityLogModel profileLog = new ActivityLogModel
                    {
                        TableName = "Profile",
                        ObjectID = profile.ID,
                        UserID = int.Parse(Request.Form["userID"].ToString()),
                        UserName = Request.Form["userName"].ToString(),
                        ChangeDate = DateTime.Now,
                        Change = "Update",
                        Description = $"Update Profile Info from Reservation: {reservationModel.ConfirmationNo}"
                    };
                    TextUtils.FillValues(profileLog, oldProfileData, profile);

                    // Chỉ Insert Log nếu có thay đổi
                    if (!string.IsNullOrEmpty(profileLog.NewValue))
                    {
                        ActivityLogBO.Instance.Insert(profileLog);
                    }
                    #endregion

                    pt.CommitTransaction();
                    return Json(new { code = 0, msg = $"Update reservation created successfully. ConfirmationNo : {reservationModel.ConfirmationNo}" });
                }


            }
            catch (Exception ex)
            {
                pt.RollBack();
                return Json(new { code = 1, msg = ex.Message, errors = ex.ToString() });
            }
            finally
            {
                pt.CloseConnection();

            }
        }
        #endregion

        #region DatVP __ search reservation
        [HttpGet]
        public async Task<IActionResult> SearchReservation2(int searchType, string name, string firstName, string reservationHolder, string confirmationNo,
            string crsNo, string roomNo, string roomType, string package, string zone, DateTime arrivalFrom, int chooseFromDate, int chooseToDate, DateTime arrivalTo, string roomSharer, string owner)
        {
            try
            {
                string formattedDate = arrivalFrom.ToString("yyyy-MM-dd");
                string formattedDate2 = arrivalTo.ToString("yyyy-MM-dd");
                if (chooseFromDate == 0)
                {
                    formattedDate = "";
                }
                if (chooseToDate == 0)
                {
                    formattedDate2 = "";
                }
                var data = _iReservationService.SearchReservation(searchType, name, firstName, reservationHolder, confirmationNo,
                crsNo, roomNo, roomType, package, zone, formattedDate, formattedDate2, roomSharer, owner);

                var result = (from d in data.AsEnumerable()
                              select d.Table.Columns.Cast<DataColumn>()
                                  //.Where(col => col.ColumnName != "AllotmentStageID" && col.ColumnName != "flag" && col.ColumnName != "Total")
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
        [HttpGet]
        public async Task<IActionResult> GetProfileIndividual()
        {
            try
            {
                List<ProfileModel> roomTypeModels = ReservationBO.GetProfileIndividual();
                return Json(roomTypeModels);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetReservationByID(int ID)
        {
            try
            {
                ReservationModel reservationModel = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(ID);
                return Json(reservationModel);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        [HttpPost]
        public ActionResult EditReservation(int ID)
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();
                List<BusinessDateModel> businessDate = PropertyUtils.ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll());
                int memberTypeID = 0; int roomTypeID = 0; int vipID = 0;
                if (string.IsNullOrEmpty(Request.Form["memberType"].ToString()))
                {
                    memberTypeID = 0;
                }
                if (string.IsNullOrEmpty(Request.Form["vipID"].ToString()))
                {
                    vipID = 0;
                }
                if (!string.IsNullOrEmpty(Request.Form["roomTypeID"].ToString()))
                {
                    roomTypeID = int.Parse(Request.Form["roomTypeID"].ToString());
                }
                if (string.IsNullOrEmpty(Request.Form["lastName"].ToString()))
                {
                    return Json(new { code = 1, msg = "Profile cannot be blank" });
                }
                if (string.IsNullOrEmpty(Request.Form["reservationTypeCode"].ToString()))
                {
                    return Json(new { code = 1, msg = "Reservation Type cannot be blank" });
                }
                if (decimal.Parse(Request.Form["rateAmount"].ToString()) == 0)
                {
                    return Json(new { code = 1, msg = "Rate cannot be blank " });

                }
                MemberTypeModel memberType = (MemberTypeModel)MemberTypeBO.Instance.FindByPrimaryKey(memberTypeID);
                VIPModel vip = (VIPModel)VIPBO.Instance.FindByPrimaryKey(vipID);
                RoomTypeModel roomType = (RoomTypeModel)RoomTypeBO.Instance.FindByPrimaryKey(roomTypeID);
                ReservationModel reservationModel = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(int.Parse(Request.Form["rsvID"].ToString()));
                #region edit reservation
                reservationModel.ConfirmationNo = (ReservationBO.GetTopConfirmationNo() + 1).ToString();
                reservationModel.ReservationNo = (ReservationBO.GetTopID() + 1).ToString();
                reservationModel.ReservationDate = businessDate[0].BusinessDate;
                reservationModel.ProfileAgentId = int.Parse(Request.Form["profileAgentID"].ToString());
                reservationModel.AgentName = Request.Form["agentName"].ToString();
                reservationModel.ProfileCompanyId = int.Parse(Request.Form["profileCompanyID"].ToString());
                reservationModel.CompanyName = Request.Form["companyName"].ToString();
                reservationModel.ProfileSourceId = 0;
                reservationModel.SourceName = "";
                reservationModel.ProfileGroupId = 0;
                reservationModel.GroupCode = Request.Form["groupCode"].ToString();
                reservationModel.GroupName = "";
                reservationModel.ProfileContactId = int.Parse(Request.Form["profileContactID"].ToString());
                reservationModel.ContactName = Request.Form["contactName"].ToString();
                reservationModel.ContactPhone = Request.Form["contactPhone"].ToString();
                reservationModel.ProfileComment = "";
                reservationModel.ProfileIndividualId = int.Parse(Request.Form["profileIndividualID"].ToString());
                reservationModel.LastName = Request.Form["lastName"].ToString();
                reservationModel.FirstName = Request.Form["firstName"].ToString();
                reservationModel.Title = Request.Form["title"].ToString();
                reservationModel.Phone = Request.Form["phone"].ToString();
                reservationModel.Email = Request.Form["email"].ToString();
                if (memberType != null)
                {
                    reservationModel.MemberType = memberType.Code;
                    reservationModel.MemberLevel = memberType.Level;
                }
                else
                {
                    reservationModel.MemberType = "";
                    reservationModel.MemberLevel = "";
                }
                reservationModel.MemberNo = Request.Form["memberNo"].ToString();
                if (vip != null)
                {
                    reservationModel.VipId = vip.ID;
                    reservationModel.Vip = vip.Code;
                }
                else
                {
                    reservationModel.VipId = 0;
                    reservationModel.Vip = "";
                }
                reservationModel.Address = Request.Form["address"].ToString();
                reservationModel.City = Request.Form["city"].ToString();
                reservationModel.Zip = "";
                reservationModel.State = "";
                reservationModel.Country = Request.Form["nationality"].ToString();
                reservationModel.Language = "";
                reservationModel.ArrivalDate = DateTime.Parse(Request.Form["arrival"].ToString());
                reservationModel.OriginalArrivalDate = DateTime.Parse(Request.Form["arrival"].ToString());
                reservationModel.NoOfNight = int.Parse(Request.Form["noOfNight"].ToString());
                reservationModel.DepartureDate = DateTime.Parse(Request.Form["departure"].ToString());
                reservationModel.OriginalDepartureDate = DateTime.Parse(Request.Form["departure"].ToString());
                reservationModel.NoOfAdult = int.Parse(Request.Form["noOfAdult"].ToString());
                reservationModel.NoOfChild = int.Parse(Request.Form["noOfChild"].ToString());
                reservationModel.NoOfChild1 = int.Parse(Request.Form["noOfChild1"].ToString());
                reservationModel.NoOfChild2 = int.Parse(Request.Form["noOfChild2"].ToString());
                reservationModel.NoOfRoom = int.Parse(Request.Form["noOfRoom"].ToString());
                if (roomType != null)
                {
                    reservationModel.RoomTypeId = roomType.ID;
                    reservationModel.RoomType = roomType.Code;
                }
                else
                {
                    reservationModel.RoomTypeId = 0;
                    reservationModel.RoomType = "";
                }
                reservationModel.RtcId = int.Parse(Request.Form["rtcID"].ToString());
                if (string.IsNullOrEmpty(Request.Form["roomNo"].ToString()))
                {
                    reservationModel.RoomId = 0;
                    reservationModel.RoomNo = "";
                }
                else
                {
                    reservationModel.RoomId = int.Parse(Request.Form["roomID"].ToString());
                    reservationModel.RoomNo = Request.Form["roomNo"].ToString();
                }

                reservationModel.BusinessBlockId = 0;
                reservationModel.BusinessBlockCode = "";
                reservationModel.Eta = !string.IsNullOrEmpty(Request.Form["eta"].ToString()) ? Request.Form["eta"].ToString() : "";
                reservationModel.CheckInDate = DateTime.Parse(Request.Form["arrival"].ToString());
                reservationModel.Etd = !string.IsNullOrEmpty(Request.Form["etd"].ToString()) ? Request.Form["etd"].ToString() : "";
                reservationModel.CheckOutDate = DateTime.Parse(Request.Form["arrival"].ToString());
                reservationModel.ReservationTypeId = int.Parse(Request.Form["reservationType"].ToString());
                reservationModel.ReservationTypeCode = Request.Form["reservationTypeCode"].ToString();
                if (string.IsNullOrEmpty(Request.Form["marketID"].ToString()))
                {
                    reservationModel.MarketId = 0;

                }
                else
                {
                    reservationModel.MarketId = int.Parse(Request.Form["marketID"].ToString());

                }
                reservationModel.MarketCode = Request.Form["marketCode"].ToString();
                reservationModel.SourceId = int.Parse(Request.Form["sourceID"].ToString());
                reservationModel.SourceCode = Request.Form["sourceCode"].ToString();
                reservationModel.OriginId = 0;
                reservationModel.OriginCode = "";
                reservationModel.CCHolder = "";
                if (string.IsNullOrEmpty(Request.Form["bookerID"].ToString()))
                {
                    reservationModel.BookerId = 0;
                    reservationModel.BookerName = "";
                }
                else
                {
                    reservationModel.BookerId = int.Parse(Request.Form["bookerID"].ToString());
                    reservationModel.BookerName = Request.Form["bookerName"].ToString();
                }

                reservationModel.BookerDetails = "";
                reservationModel.NoPost = false;
                reservationModel.PrintRate = true;
                reservationModel.ConfirmationStatus = true;
                reservationModel.VideoCheckOutStatus = false;
                reservationModel.CRSNo = "";
                reservationModel.DiscountAmount = decimal.Parse(Request.Form["discountAmount"].ToString());
                reservationModel.DiscountRate = decimal.Parse(Request.Form["discountRate"].ToString());
                reservationModel.DiscountReason = "";
                reservationModel.Comment = Request.Form["comment"].ToString();
                reservationModel.BalanceUSD = 0;
                reservationModel.BalanceVND = 0;
                reservationModel.ApprovalCode = "";
                reservationModel.ApprovalAmount = 0;
                reservationModel.SuiteWith = "";
                reservationModel.PaymentMethod = "";
                reservationModel.CreditCardNo = "";
                reservationModel.ExpirationDate = DateTime.Now;
                reservationModel.TaxTypeId = 0;
                reservationModel.ExemptNumber = "";
                reservationModel.PickupReqdId = int.Parse(Request.Form["pickedId"].ToString());
                reservationModel.PickupTransportType = Request.Form["pickUpTransportType"].ToString();
                reservationModel.PickupStationCode = Request.Form["pickUpStationCode"].ToString();
                reservationModel.PickupCarrierCode = Request.Form["pickUpCarrierCode"].ToString();
                reservationModel.PickupTime = Request.Form["pickUpTime"].ToString();
                reservationModel.PickupTransportNo = Request.Form["pickUpTransportNo"].ToString();
                reservationModel.PickupArrivalDate = DateTime.Parse(Request.Form["pickUpDate"].ToString());
                reservationModel.PickupDescription = Request.Form["pickUpDescription"].ToString();
                reservationModel.DropOffReqdId = int.Parse(Request.Form["dropOffId"].ToString());
                reservationModel.DropOffTransportType = Request.Form["dropOffTransportType"].ToString();
                reservationModel.DropOffStationCode = Request.Form["dropOffStationCode"].ToString();
                reservationModel.DropOffCarrierCode = Request.Form["dropOffCarrierCode"].ToString();
                reservationModel.DropOffTime = Request.Form["dropOffTime"].ToString();
                reservationModel.DropOffTransportNo = Request.Form["dropOffTransportNo"].ToString();
                reservationModel.DropOffDepartureDate = DateTime.Parse(Request.Form["dropOffDate"].ToString());
                reservationModel.DropOffDescription = Request.Form["dropOffDescription"].ToString();
                reservationModel.PackageId = int.Parse(Request.Form["packageID"].ToString());
                reservationModel.Packages = Request.Form["packages"].ToString();
                reservationModel.Relationship = ReservationBO.GetTopID() + 1;
                reservationModel.Status = 0;
                reservationModel.PostingMaster = false;
                reservationModel.MainGuest = true;
                if (string.IsNullOrEmpty(Request.Form["rateCode"].ToString()))
                {
                    reservationModel.RateCodeId = 0;
                    reservationModel.RateCode = "";
                }
                else
                {
                    reservationModel.RateCodeId = int.Parse(Request.Form["rateCodeID"].ToString());
                    reservationModel.RateCode = Request.Form["rateCode"].ToString();
                }
                reservationModel.Rate = decimal.Parse(Request.Form["rateAmount"].ToString());
                reservationModel.RateAfterTax = decimal.Parse(Request.Form["rateAfter"].ToString());
                reservationModel.FixedRate = false;
                reservationModel.TotalAmount = decimal.Parse(Request.Form["rateAmount"].ToString());
                reservationModel.CurrencyId = "VND";
                reservationModel.Party = "";
                reservationModel.PartyGuest = "";
                reservationModel.IsPasserBy = false;
                reservationModel.Color = Request.Form["color"].ToString();
                reservationModel.ARNo = "";
                reservationModel.ItemInventory = Request.Form["itemInventory"].ToString();
                reservationModel.Specials = Request.Form["specials"].ToString();
                reservationModel.ShareRoom = ReservationBO.GetTopID() + 1;
                reservationModel.NoShowStatus = false;
                reservationModel.ShareRoomName = "";
                reservationModel.AccompanyName = "";
                reservationModel.RoutingTransaction = "";
                reservationModel.RoutingToProfile = Request.Form["firstName"].ToString();
                reservationModel.FixedCharge = "";
                reservationModel.CommentGroup = "";
                reservationModel.IsWalkIn = false;
                reservationModel.UserUpdateId = int.Parse(Request.Form["userID"].ToString());
                reservationModel.UpdateDate = DateTime.Now;
                reservationModel.UpdateBy = Request.Form["userName"].ToString();
                reservationModel.SpecialUpdateBy = Request.Form["userName"].ToString();
                reservationModel.SpecialUpdateDate = DateTime.Now;
                reservationModel.IsAdvanceBill = false;
                if (string.IsNullOrEmpty(Request.Form["allotmentID"].ToString()))
                {
                    reservationModel.AllotmentId = 0;
                    reservationModel.AllotmentCode = "";
                }
                else
                {
                    reservationModel.AllotmentId = int.Parse(Request.Form["allotmentID"].ToString());
                    reservationModel.AllotmentCode = Request.Form["allotmentCode"].ToString();
                }
                reservationModel.PinCode = (ReservationBO.GetTopID() + 1).ToString();
                reservationModel.PersonInChargeId = int.Parse(Request.Form["perrsonInCharge"].ToString());
                reservationModel.RoomNight = int.Parse(Request.Form["roomNight"].ToString());
                reservationModel.CardId = "";
                reservationModel.Breakfast = false;
                reservationModel.Dinner = false;
                reservationModel.Lunch = false;
                reservationModel.FixedMeal = false;
                reservationModel.VoucherId = "";
                ReservationBO.Instance.Update(reservationModel);
                #endregion

                #region lưu reservation item inventory
                string itemInventoryString = Request.Form["itemInventory"].ToString();
                List<int> itemInventory = itemInventoryString.Split(',')
                                                    .Select(x => int.Parse(x)).Where(x => x != 0)
                                                    .ToList();
                //if (itemInventory.Count > 0)
                //{
                //    foreach (var item in itemInventory)
                //    {
                //        ItemModel itemModel = (ItemModel)ItemBO.Instance.FindByPrimaryKey(item);
                //        if (itemModel != null && itemModel.ID != 0)
                //        {
                //            ReservationItemInventoryModel reservationItemInventory = new ReservationItemInventoryModel();
                //            reservationItemInventory.ReservationID = (int)reservationID;
                //            reservationItemInventory.ItemID = itemModel.ID;
                //            reservationItemInventory.Code = itemModel.Code;
                //            reservationItemInventory.Name = itemModel.Name;
                //            reservationItemInventory.BeginDate = reservationModel.ArrivalDate;
                //            reservationItemInventory.EndDate = reservationModel.DepartureDate;
                //            reservationItemInventory.Quantity = 1;
                //            reservationItemInventory.RateCode = "";
                //            reservationItemInventory.PackageID = reservationModel.PackageId;
                //            reservationItemInventory.Package = reservationModel.Packages;
                //            reservationItemInventory.ReservationFixedChargeID = 0;
                //            reservationItemInventory.UserInsertID = reservationItemInventory.UserUpdateID = int.Parse(Request.Form["userID"].ToString());
                //            reservationItemInventory.CreateDate = reservationItemInventory.UpdateDate = DateTime.Now;
                //            ReservationItemInventoryBO.Instance.Insert(reservationItemInventory);
                //        }

                //    }
                //}
                #endregion
                pt.CommitTransaction();
                return Json(new { code = 0, msg = "Update reservation created successfully" });

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
        public ActionResult EditReservationRoomPlan(int ID)
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();
                List<BusinessDateModel> businessDate = PropertyUtils.ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll());
                int memberTypeID = 0; int roomTypeID = 0; int vipID = 0;
                if (string.IsNullOrEmpty(Request.Form["memberType"].ToString()))
                {
                    memberTypeID = 0;
                }
                if (string.IsNullOrEmpty(Request.Form["vipID"].ToString()))
                {
                    vipID = 0;
                }
                if (!string.IsNullOrEmpty(Request.Form["roomTypeID"].ToString()))
                {
                    roomTypeID = int.Parse(Request.Form["roomTypeID"].ToString());
                }
                if (string.IsNullOrEmpty(Request.Form["lastName"].ToString()))
                {
                    return Json(new { code = 1, msg = "Profile cannot be blank" });
                }
                if (string.IsNullOrEmpty(Request.Form["reservationTypeCode"].ToString()))
                {
                    return Json(new { code = 1, msg = "Reservation Type cannot be blank" });
                }
                if (decimal.Parse(Request.Form["rateAmount"].ToString()) == 0)
                {
                    return Json(new { code = 1, msg = "Rate cannot be blank " });

                }
                MemberTypeModel memberType = (MemberTypeModel)MemberTypeBO.Instance.FindByPrimaryKey(memberTypeID);
                VIPModel vip = (VIPModel)VIPBO.Instance.FindByPrimaryKey(vipID);
                RoomTypeModel roomType = (RoomTypeModel)RoomTypeBO.Instance.FindByPrimaryKey(roomTypeID);
                ReservationModel reservationModel = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(int.Parse(Request.Form["rsvID"].ToString()));
                #region edit reservation

                reservationModel.ProfileComment = "";
                reservationModel.LastName = Request.Form["lastName"].ToString();
                reservationModel.FirstName = Request.Form["firstName"].ToString();
                reservationModel.Phone = Request.Form["phone"].ToString();
                reservationModel.Email = Request.Form["email"].ToString();


                reservationModel.ArrivalDate = DateTime.Parse(Request.Form["arrival"].ToString());
                reservationModel.OriginalArrivalDate = DateTime.Parse(Request.Form["arrival"].ToString());
                reservationModel.NoOfNight = int.Parse(Request.Form["noOfNight"].ToString());
                reservationModel.DepartureDate = DateTime.Parse(Request.Form["departure"].ToString());
                reservationModel.OriginalDepartureDate = DateTime.Parse(Request.Form["departure"].ToString());
                reservationModel.NoOfAdult = int.Parse(Request.Form["noOfAdult"].ToString());
                reservationModel.NoOfChild = int.Parse(Request.Form["noOfChild"].ToString());
                reservationModel.NoOfChild1 = int.Parse(Request.Form["noOfChild1"].ToString());
                reservationModel.NoOfChild2 = int.Parse(Request.Form["noOfChild2"].ToString());
                reservationModel.NoOfRoom = int.Parse(Request.Form["noOfRoom"].ToString());
                if (roomType != null)
                {
                    reservationModel.RoomTypeId = roomType.ID;
                    reservationModel.RoomType = roomType.Code;
                }
                else
                {
                    reservationModel.RoomTypeId = 0;
                    reservationModel.RoomType = "";
                }
                reservationModel.RtcId = int.Parse(Request.Form["rtcID"].ToString());
                if (string.IsNullOrEmpty(Request.Form["roomNo"].ToString()))
                {
                    reservationModel.RoomId = 0;
                    reservationModel.RoomNo = "";
                }
                else
                {
                    reservationModel.RoomId = int.Parse(Request.Form["roomID"].ToString());
                    reservationModel.RoomNo = Request.Form["roomNo"].ToString();
                }

                reservationModel.Eta = !string.IsNullOrEmpty(Request.Form["eta"].ToString()) ? Request.Form["eta"].ToString() : "";
                reservationModel.CheckInDate = DateTime.Parse(Request.Form["arrival"].ToString());
                reservationModel.Etd = !string.IsNullOrEmpty(Request.Form["etd"].ToString()) ? Request.Form["etd"].ToString() : "";
                reservationModel.CheckOutDate = DateTime.Parse(Request.Form["arrival"].ToString());

                if (string.IsNullOrEmpty(Request.Form["marketID"].ToString()))
                {
                    reservationModel.MarketId = 0;

                }
                else
                {
                    reservationModel.MarketId = int.Parse(Request.Form["marketID"].ToString());

                }

                if (string.IsNullOrEmpty(Request.Form["rateCode"].ToString()))
                {
                    reservationModel.RateCodeId = 0;
                    reservationModel.RateCode = "";
                }
                else
                {
                    reservationModel.RateCodeId = int.Parse(Request.Form["rateCodeID"].ToString());
                    reservationModel.RateCode = Request.Form["rateCode"].ToString();
                }
                reservationModel.Rate = decimal.Parse(Request.Form["rateAmount"].ToString());
                reservationModel.RateAfterTax = decimal.Parse(Request.Form["rateAfter"].ToString());
                reservationModel.FixedRate = false;
                reservationModel.TotalAmount = decimal.Parse(Request.Form["rateAmount"].ToString());
                reservationModel.CurrencyId = "VND";
                reservationModel.Party = "";
                reservationModel.DepositRequest = "";
                reservationModel.PartyGuest = "";
                reservationModel.IsPasserBy = false;
                //reservationModel.Color = Request.Form["color"].ToString();
                reservationModel.ARNo = "";
                reservationModel.ShareRoom = ReservationBO.GetTopID() + 1;
                reservationModel.NoShowStatus = false;
                reservationModel.ShareRoomName = "";
                reservationModel.AccompanyName = "";
                reservationModel.RoutingTransaction = "";
                reservationModel.RoutingToProfile = Request.Form["firstName"].ToString();
                reservationModel.FixedCharge = "";
                reservationModel.CommentGroup = "";
                reservationModel.IsWalkIn = false;
                reservationModel.UserUpdateId = int.Parse(Request.Form["userID"].ToString());
                reservationModel.UpdateDate = DateTime.Now;
                reservationModel.UpdateBy = Request.Form["userName"].ToString();
                reservationModel.SpecialUpdateBy = Request.Form["userName"].ToString();
                reservationModel.SpecialUpdateDate = DateTime.Now;
                reservationModel.IsAdvanceBill = false;
                if (string.IsNullOrEmpty(Request.Form["allotmentID"].ToString()))
                {
                    reservationModel.AllotmentId = 0;
                    reservationModel.AllotmentCode = "";
                }
                else
                {
                    reservationModel.AllotmentId = int.Parse(Request.Form["allotmentID"].ToString());
                    reservationModel.AllotmentCode = Request.Form["allotmentCode"].ToString();
                }
                reservationModel.PinCode = (ReservationBO.GetTopID() + 1).ToString();
                reservationModel.PersonInChargeId = int.Parse(Request.Form["perrsonInCharge"].ToString());
                reservationModel.RoomNight = int.Parse(Request.Form["roomNight"].ToString());
                reservationModel.CardId = "";
                reservationModel.Breakfast = false;
                reservationModel.Dinner = false;
                reservationModel.Lunch = false;
                reservationModel.FixedMeal = false;
                reservationModel.VoucherId = "";
                ReservationBO.Instance.Update(reservationModel);
                #endregion

                #region lưu reservation item inventory
                string itemInventoryString = Request.Form["itemInventory"].ToString();
                List<int> itemInventory = itemInventoryString.Split(',')
                                                    .Select(x => int.Parse(x)).Where(x => x != 0)
                                                    .ToList();
                //if (itemInventory.Count > 0)
                //{
                //    foreach (var item in itemInventory)
                //    {
                //        ItemModel itemModel = (ItemModel)ItemBO.Instance.FindByPrimaryKey(item);
                //        if (itemModel != null && itemModel.ID != 0)
                //        {
                //            ReservationItemInventoryModel reservationItemInventory = new ReservationItemInventoryModel();
                //            reservationItemInventory.ReservationID = (int)reservationID;
                //            reservationItemInventory.ItemID = itemModel.ID;
                //            reservationItemInventory.Code = itemModel.Code;
                //            reservationItemInventory.Name = itemModel.Name;
                //            reservationItemInventory.BeginDate = reservationModel.ArrivalDate;
                //            reservationItemInventory.EndDate = reservationModel.DepartureDate;
                //            reservationItemInventory.Quantity = 1;
                //            reservationItemInventory.RateCode = "";
                //            reservationItemInventory.PackageID = reservationModel.PackageId;
                //            reservationItemInventory.Package = reservationModel.Packages;
                //            reservationItemInventory.ReservationFixedChargeID = 0;
                //            reservationItemInventory.UserInsertID = reservationItemInventory.UserUpdateID = int.Parse(Request.Form["userID"].ToString());
                //            reservationItemInventory.CreateDate = reservationItemInventory.UpdateDate = DateTime.Now;
                //            ReservationItemInventoryBO.Instance.Insert(reservationItemInventory);
                //        }

                //    }
                //}
                #endregion
                pt.CommitTransaction();
                return Json(new { code = 0, msg = "Update reservation created successfully" });

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

        #region DatVp __  Reservation: Cancel
        [HttpGet]
        public ActionResult FindCancelReservation(int reservationID)
        {
            try
            {
                SqlParameter[] param = [
                        new SqlParameter("@ReservationID", reservationID ),
                    ];
                DataTable dataTable = DataTableHelper.getTableData("spReservationCancellationHistory", param);
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
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult CancelReservation()
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();
                //validate form cancel
                if (string.IsNullOrEmpty(Request.Form["reasonCancellation"].ToString()))
                {
                    return Json(new { code = 1, msg = "Please choose reason cancel" });

                }
                ReservationModel rsv = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(int.Parse(Request.Form["rsvID"].ToString()));
                if (rsv == null)
                {
                    return Json(new { code = 1, msg = "Can not find reservation" });
                }
                #region  check xem Reservation có deposit không
                var deposit = DepositPaymentBO.Instance.FindByAttribute("ReservationID", rsv.ID);
                if (deposit.Count > 0)
                {
                    return Json(new { code = 1, msg = "Payment in advance exist on the reservation. You must balance the amount paid before" });

                }
                #endregion

                if (rsv.MainGuest == true)
                {
                    // neu la main guest thi update status cac row co conformation no giong no thanh cancel
                    List<ReservationModel> listRsv = PropertyUtils.ConvertToList<ReservationModel>(ReservationBO.Instance.FindByAttribute("ConfirmationNo", rsv.ConfirmationNo));
                    foreach (var item in listRsv)
                    {
                        item.UpdateDate = rsv.SpecialUpdateDate = DateTime.Now;
                        item.UserUpdateId = int.Parse(Request.Form["userID"].ToString());
                        item.UpdateBy = rsv.SpecialUpdateBy = Request.Form["userName"].ToString();
                        ReservationBO.Instance.Update(item);

                        #region insert vào  bảng ActivityLog
                        string status = "";
                        if (item.Status == 0)
                        {
                            status = "RESERVED";
                        }
                        if (item.Status == 5)
                        {
                            status = "DUE IN";
                        }
                        item.Status = 3;
                        ActivityLogModel activityLog = new ActivityLogModel();
                        activityLog.TableName = "Reservation";
                        activityLog.ObjectID = item.ID;
                        activityLog.UserID = int.Parse(Request.Form["userID"].ToString());
                        activityLog.UserName = Request.Form["userName"].ToString();
                        activityLog.ChangeDate = DateTime.Now;
                        activityLog.Change = "Status";
                        activityLog.NewValue = "CANCEL";
                        activityLog.OldValue = status;
                        activityLog.Description = "";
                        ActivityLogBO.Instance.Insert(activityLog);
                        #endregion

                        #region insert ReservationCancellation 
                        string cancellationNo = ReservationCancellationBO.GetTopCancellatioNo();
                        ReservationCancellationModel reservationCancellation = new ReservationCancellationModel();
                        reservationCancellation.ReservationID = item.ID;
                        reservationCancellation.CancellationDate = DateTime.Now;
                        reservationCancellation.CancellationNo = !string.IsNullOrEmpty(cancellationNo) ? cancellationNo : "0";
                        reservationCancellation.ReasonCancellation = Request.Form["reasonCancellation"].ToString();
                        reservationCancellation.Description = Request.Form["description"].ToString();
                        reservationCancellation.CreateDate = reservationCancellation.UpdateDate = DateTime.Now;
                        reservationCancellation.UserInsertID = reservationCancellation.UserUpdateID = int.Parse(Request.Form["userID"].ToString());
                        ReservationCancellationBO.Instance.Insert(reservationCancellation);
                        #endregion
                    }
                }
                else
                {
                    // neu la room sharer thi chi update row do thanh cancel
                    rsv.Status = 3;
                    rsv.UpdateDate = rsv.SpecialUpdateDate = DateTime.Now;
                    rsv.UserUpdateId = int.Parse(Request.Form["userID"].ToString());
                    rsv.UpdateBy = rsv.SpecialUpdateBy = Request.Form["userName"].ToString();
                    ReservationBO.Instance.Update(rsv);

                    #region Insert ActivityLog
                    string status = "";
                    if (rsv.Status == 0)
                    {
                        status = "RESERVED";
                    }
                    if (rsv.Status == 5)
                    {
                        status = "DUE IN";
                    }
                    ActivityLogModel activityLog = new ActivityLogModel();
                    activityLog.TableName = "Reservation";
                    activityLog.ObjectID = rsv.ID;
                    activityLog.UserID = int.Parse(Request.Form["userID"].ToString());
                    activityLog.UserName = Request.Form["userName"].ToString();
                    activityLog.ChangeDate = DateTime.Now;
                    activityLog.Change = "Status";
                    activityLog.NewValue = "CANCEL";
                    activityLog.OldValue = status;
                    activityLog.Description = "";
                    ActivityLogBO.Instance.Insert(activityLog);
                    #endregion

                    #region insert ReservationCancellation 
                    string cancellationNo = ReservationCancellationBO.GetTopCancellatioNo();
                    ReservationCancellationModel reservationCancellation = new ReservationCancellationModel();
                    reservationCancellation.ReservationID = rsv.ID;
                    reservationCancellation.CancellationDate = DateTime.Now;
                    reservationCancellation.CancellationNo = !string.IsNullOrEmpty(cancellationNo) ? cancellationNo : "0";
                    reservationCancellation.ReasonCancellation = Request.Form["reasonCancellation"].ToString();
                    reservationCancellation.Description = Request.Form["description"].ToString();
                    reservationCancellation.CreateDate = reservationCancellation.UpdateDate = DateTime.Now;
                    reservationCancellation.UserInsertID = reservationCancellation.UserUpdateID = int.Parse(Request.Form["userID"].ToString());
                    ReservationCancellationBO.Instance.Insert(reservationCancellation);
                    #endregion
                }

                pt.CommitTransaction();
                return Json(new { code = 0, msg = "Cancel reservation successfully" });

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

        #region DatVP __ registration card
        [HttpGet]
        public async Task<IActionResult> GetRegistrationCard()
        {
            try
            {
                List<RegistrationCardModel> result = RegistrationCardBO.GetRegistrationCard();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        #endregion

        #region DatVP __ Reservation: Accompanying
        [HttpGet]
        public async Task<IActionResult> GetReservationAccompanyingByReservationID(int reservationID)
        {
            try
            {

                var result = ReservationAccompanyBO.GetReservationAccompanyByReservationID(reservationID);

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        [HttpPost]
        public ActionResult AttachAccompanying()
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();
                if (string.IsNullOrEmpty(Request.Form["profileAgentID"].ToString()))
                {
                    return Json(new { code = 1, msg = "Could not find profile attached" });
                }
                if (string.IsNullOrEmpty(Request.Form["rsvID"].ToString()))
                {
                    return Json(new { code = 1, msg = "Please choose booking" });
                }
                var checkReservationAccompany = ReservationAccompanyBO.GetReservationAccompany(int.Parse(Request.Form["rsvID"].ToString()), int.Parse(Request.Form["profileAgentID"].ToString()));
                if (checkReservationAccompany.Count > 0)
                {
                    return Json(new { code = 1, msg = "This profile has been attached, please choose another profile" });

                }
                ReservationAccompanyModel model = new ReservationAccompanyModel();
                model.ReservationID = int.Parse(Request.Form["rsvID"].ToString());
                model.ProfileIndividualID = int.Parse(Request.Form["profileAgentID"].ToString());
                model.UserInsertID = model.UserUpdateID = int.Parse(Request.Form["userID"].ToString());
                model.UpdateDate = model.CreateDate = DateTime.Now;
                ReservationAccompanyBO.Instance.Insert(model);
                pt.CommitTransaction();
                return Json(new { code = 0, msg = "Profile was attacheđ successfully" });

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
        public ActionResult DettachAccompanying(int id)
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();
                if (id == 0)
                {
                    return Json(new { code = 1, msg = "Please choose profile dettach" });
                }


                ReservationAccompanyBO.Instance.Delete(id);
                pt.CommitTransaction();
                return Json(new { code = 0, msg = "Profile was dettached successfully" });

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

        #region Reservation : CheckIn
        [HttpPost]
        public IActionResult CheckInBooking()
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                int rsvID = int.Parse(Request.Form["rsvID"].ToString());
                string userName = Request.Form["userName"].ToString();
                int userID = int.Parse(Request.Form["userID"].ToString());

                pt.OpenConnection();
                pt.BeginTransaction();

                ReservationModel reservation = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(rsvID);
                if (reservation == null) return Json(new { code = 1, msg = "Reservation not found." });

                DateTime businessDate = TextUtils.GetBusinessDate();
                if (reservation.ArrivalDate.Date != businessDate.Date)
                {
                    return Json(new { code = 1, msg = $"Arrival Date must be equal to Business Date ({businessDate:dd/MM/yyyy})." });
                }

                //  VALIDATION: Trạng thái  0:Reservation hoặc 5:Due In được Check-in
                if (reservation.Status == 1) return Json(new { code = 1, msg = "Guest is already Checked-in." });
                int[] blockStatus = { 2, 3, 7 }; // CO, Cancel, NoShow
                if (blockStatus.Contains(reservation.Status))
                {
                    return Json(new { code = 1, msg = "Invalid status for Check-in." });
                }

                // VALIDATION: Số phòng
                if (reservation.RoomId <= 0)
                {
                    return Json(new { code = 1, msg = "Please assign a room before Check-in." });
                }

                if (reservation.NoOfRoom > 1)
                {
                    return Json(new { code = 1, msg = "Split first reservation. Please!" });
                }

                // VALIDATION: Share Room 
                if (reservation.ShareRoom > 0 && !reservation.MainGuest)
                {
                    string sqlCheckMG = $@"SELECT COUNT(*) FROM Reservation 
                                   WHERE RoomId = {reservation.RoomId} 
                                   AND MainGuest = 1 AND Status = 1";

                    DataTable dtMG = TextUtils.Select(sqlCheckMG);
                    int countMG = (dtMG.Rows.Count > 0) ? Convert.ToInt32(dtMG.Rows[0][0]) : 0;

                    if (countMG <= 0)
                    {
                        return Json(new { code = 1, msg = "Main guest must be checked in before room sharer." });
                    }
                }

                // VALIDATION: Tình trạng phòng thực tế 
                var room = (RoomModel)RoomBO.Instance.FindByPrimaryKey(reservation.RoomId);
                if (room != null)
                {
                    if (room.FOStatus == 1)
                        return Json(new { code = 1, msg = $"Room {room.RoomNo} is Occupied." });

                    // HKStatusID:  4 - Inspected, 1 - Clean
                    if (room.HKStatusID != 4 && room.HKStatusID != 1)
                    {
                        return Json(new { code = 1, msg = $"Room {room.RoomNo} is not Clean/Inspected (Current HK: {room.HKStatusID})." });
                    }
                }

                ReservationModel oldData = (ReservationModel)reservation.Clone();

                reservation.Status = 1; // Checked-In
                reservation.CheckInDate = DateTime.Now;
                reservation.UpdateBy = userName;
                reservation.UserUpdateId = userID;
                reservation.UpdateDate = DateTime.Now;
                reservation.SpecialUpdateBy = userName;
                reservation.SpecialUpdateDate = DateTime.Now;
                ReservationBO.Instance.Update(reservation);

                #region sinh folio cho main guest
                if (reservation.MainGuest == true)
                {
                    string checkSql = $"SELECT COUNT(*) FROM Folio WHERE ReservationID = {reservation.ID}";
                    int countFolio = Convert.ToInt32(TextUtils.Select(checkSql).Rows[0][0]);

                    if (countFolio == 0)
                    {
                        // --- TẠO GUEST FOLIO (Window 1) ---
                        FolioModel guestFolio = new FolioModel
                        {
                            ReservationID = reservation.ID,
                            FolioDate = businessDate,
                            FolioNo = 1,
                            ProfileID = reservation.ProfileIndividualId,
                            AccountName = $"{reservation.LastName} {reservation.FirstName}",
                            ConfirmationNo = reservation.ConfirmationNo,
                            Status = false,
                            IsMasterFolio = false,
                            CreateDate = DateTime.Now,
                            UpdateDate = DateTime.Now,
                            UserInsertID = userID,
                            UserUpdateID = userID
                        };
                        FolioBO.Instance.Insert(guestFolio);

                        if (reservation.ProfileCompanyId > 0 || reservation.ProfileAgentId > 0 || reservation.ProfileGroupId > 0)
                        {
                            int masterProfileID = reservation.ProfileCompanyId > 0 ? reservation.ProfileCompanyId : (reservation.ProfileAgentId > 0 ? reservation.ProfileAgentId : reservation.ProfileGroupId);
                            string accName = "";
                            DataTable dtAcc = TextUtils.Select($"SELECT Account FROM Profile WHERE ID = {masterProfileID}");
                            if (dtAcc.Rows.Count > 0) accName = dtAcc.Rows[0]["Account"].ToString();

                            FolioModel masterFolio = new FolioModel
                            {
                                ReservationID = reservation.ID,
                                FolioDate = businessDate,
                                FolioNo = -1,
                                ProfileID = masterProfileID,
                                AccountName = accName,
                                ConfirmationNo = reservation.ConfirmationNo,
                                Status = false,
                                IsMasterFolio = true,
                                CreateDate = DateTime.Now,
                                UpdateDate = DateTime.Now,
                                UserInsertID = userID,
                                UserUpdateID = userID
                            };
                            FolioBO.Instance.Insert(masterFolio);
                        }
                    }
                }
                #endregion

                if (room != null)
                {
                    room.FOStatus = 1;         // Occupied
                    room.CurrResvStatus = 1;   // Đánh dấu phòng có khách In-house
                    room.UserUpdateID = userID;
                    room.UpdateDate = DateTime.Now;
                    RoomBO.Instance.Update(room);
                }

                // KHỞI TẠO OPTIONS (ReservationOptions)
                ReservationOptionsModel option = new ReservationOptionsModel
                {
                    ReservationID = reservation.ID,
                    Billing = true, // Sau CI vào Billing luôn
                                    // Các trường khác mặc định false
                    Accompany = false,
                    AddOn = false,
                    AgentCompany = false,
                    Alerts = false,
                    CallerInfo = false,
                    Cancel = false,
                    Changes = false,
                    Confirmation = false,
                    CreditCards = false,
                    Del = false,
                    DepositCancelation = false,
                    History = false,
                    HouseKeeping = false,
                    Messages = false,
                    Shares = false,
                    Traces = false
                };
                ReservationOptionsBO.Instance.Insert(option);

                //  GHI LOG 
                ActivityLogModel activityLog = new ActivityLogModel
                {
                    TableName = "Reservation",
                    ObjectID = reservation.ID,
                    UserID = userID,
                    UserName = userName,
                    ChangeDate = DateTime.Now,
                    Change = "Check-In",
                    Description = $"Check-in guest: {reservation.LastName} {reservation.FirstName} - Room: {reservation.RoomNo} | Auto generate folio no 1"
                };

                TextUtils.FillValues(activityLog, oldData, reservation);
                ActivityLogBO.Instance.Insert(activityLog);

                pt.CommitTransaction();
                return Json(new { code = 0, msg = $"Check-in success! Room: {room?.RoomNo}" });
            }
            catch (Exception ex)
            {
                pt.RollBack();
                return Json(new { code = 1, msg = "System Error: " + ex.Message });
            }
            finally
            {
                pt.CloseConnection();
            }
        }

        [HttpPost]
        public ActionResult PrintRegistrationCard(int id)
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();
                string url = "";
                ReservationModel reservation = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(id);
                ProfileModel profile = (ProfileModel)ProfileBO.Instance.FindByPrimaryKey(reservation.ProfileIndividualId);

                XtraReport report = new WebApp.Templates.RegistrationCard.V_RegistrationCard();
                report.Parameters["ConfirmationNo"].Value = reservation.ConfirmationNo;
                report.Parameters["ArrivalDate"].Value = reservation.ArrivalDate.ToString().Split(" ")[0];
                report.Parameters["DepartureDate"].Value = reservation.DepartureDate.ToString().Split(" ")[0];
                report.Parameters["ArrivalTime"].Value = reservation.Eta.ToString();
                report.Parameters["DepartureTime"].Value = reservation.Etd.ToString();
                report.Parameters["RoomType"].Value = reservation.RoomType;
                report.Parameters["RoomNo"].Value = reservation.RoomNo;
                report.Parameters["Account"].Value = reservation.LastName;
                report.Parameters["National"].Value = reservation.Language;
                report.Parameters["Dob"].Value = profile.DateOfBirth.ToString();
                report.Parameters["Cccd"].Value = profile.IdentityCard == "" ? profile.PassPort : profile.IdentityCard;
                report.Parameters["ContactInfo"].Value = $"{profile.HandPhone} - {profile.Email} - {profile.Address}";
                report.Parameters["Booker"].Value = reservation.BookerName;

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

        #region DatVP __ reservation billing
        [HttpGet]
        public async Task<IActionResult> GetFolioDetailByFolioID(int mode, int folioNo)
        {
            try
            {
                DataTable myData = _iFolioDetailService.GetFolioDetailByFolioID(folioNo, mode);

                var result = (from d in myData.AsEnumerable()

                              select new
                              {
                                  Select = d["Select"].ToString(),
                                  Date = d["Date"].ToString(),

                                  CodePrefix = d["CodePrefix"].ToString(),
                                  Code = d["Code"].ToString(),
                                  Description = d["Description"].ToString(),

                                  Amount = d["Amount"].ToString(),
                                  Currency = d["Currency"].ToString(),
                                  Supplement = d["Supplement"].ToString(),
                                  Reference = d["Reference"].ToString(),
                                  UserName = d["UserName"].ToString(),
                                  Time = d["Time"].ToString(),
                                  GroupCode = d["GroupCode"].ToString(),
                                  SubgroupCode = d["SubgroupCode"].ToString(),
                                  PostType = d["PostType"].ToString(),
                                  RowState = d["RowState"].ToString(),
                                  IsSplit = d["IsSplit"].ToString(),
                                  InvoiceNo = d["InvoiceNo"].ToString(),
                                  TransactionNo = d["TransactionNo"].ToString(),

                                  ID = d["ID"].ToString(),
                                  FolioID = d["FolioID"].ToString(),
                                  GroupType = d["GroupType"].ToString(),
                                  ShiftID = d["ShiftID"].ToString(),
                                  ProfitCenterID = d["ProfitCenterID"].ToString(),
                                  ProfitCenterCode = d["ProfitCenterCode"].ToString(),
                                  RoomTypeID = d["RoomTypeID"].ToString(),
                                  RoomType = d["RoomType"].ToString(),
                                  Property = d["Property"].ToString(),
                                  CheckNo = d["CheckNo"].ToString(),


                              }).ToList();


                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        #endregion

        #region DatVP __ Reservation: fixed charges
        [HttpGet]
        public async Task<IActionResult> GetFixedChargesByReservationID(int reservationID)
        {
            try
            {

                var result = ReservationFixedChargeBO.Instance.FindByAttribute("ReservationID", reservationID);
                ReservationModel reservation = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(reservationID);
                return Json(new
                {
                    reservationFixedCharge = result,
                    reservation = reservation
                });

            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> CaculateNetFixedCharge(string transactionCode, decimal price)
        {
            try
            {

                decimal net = _iReservationService.CalculateNetFixedCharge(transactionCode, price);

                return Json(net);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        [HttpPost]
        public ActionResult SaveReservationFixedCharge()
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();
                List<BusinessDateModel> businessDate = PropertyUtils.ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll());
                TransactionsModel tran = (TransactionsModel)TransactionsBO.Instance.FindByAttribute("Code", Request.Form["transCode"].ToString())[0];
                ReservationFixedChargeModel reservationFixedCharge = new ReservationFixedChargeModel();
                reservationFixedCharge.ReservationID = int.Parse(Request.Form["rsvID"].ToString());
                reservationFixedCharge.TransactionCode = Request.Form["transCode"].ToString();
                reservationFixedCharge.ArticlesCode = "";
                reservationFixedCharge.BeginDate = DateTime.Parse(Request.Form["beignDate"].ToString());
                reservationFixedCharge.EndDate = DateTime.Parse(Request.Form["endDate"].ToString());
                reservationFixedCharge.Quantity = int.Parse(Request.Form["quantity"].ToString());
                reservationFixedCharge.Amount = decimal.Parse(Request.Form["amount"].ToString());
                reservationFixedCharge.AmountAfterTax = decimal.Parse(Request.Form["net"].ToString());
                reservationFixedCharge.CurrencyID = Request.Form["currency"].ToString();
                reservationFixedCharge.PostingRhythmID = int.Parse(Request.Form["postingRhythmlID"].ToString());
                reservationFixedCharge.PostingDate = businessDate[0].BusinessDate;
                reservationFixedCharge.PostingDay = "";
                reservationFixedCharge.ProfitCenterID = 0;
                reservationFixedCharge.Description = tran.Description;
                reservationFixedCharge.IsTaxInclude = false;
                reservationFixedCharge.UserInsertID = reservationFixedCharge.UserUpdateID = int.Parse(Request.Form["userID"].ToString());
                reservationFixedCharge.UpdateDate = reservationFixedCharge.CreateDate = DateTime.Now;
                ReservationFixedChargeBO.Instance.Insert(reservationFixedCharge);
                pt.CommitTransaction();
                return Json(new { code = 0, msg = "New reservation fixed charge created successfully" });

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
        public ActionResult SaveReservationFixedChargeDefault(string keyName, int reservationID)
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();
                List<BusinessDateModel> businessDate = PropertyUtils.ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll());
                ConfigSystemModel config = (ConfigSystemModel)ConfigSystemBO.Instance.FindByAttribute("KeyName", keyName)[0];
                TransactionsModel tran = (TransactionsModel)TransactionsBO.Instance.FindByAttribute("Code", config.KeyValue)[0];
                ReservationModel reservation = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(reservationID);
                ReservationFixedChargeModel reservationFixedCharge = new ReservationFixedChargeModel();
                reservationFixedCharge.ReservationID = reservationID;
                reservationFixedCharge.TransactionCode = tran.Code;
                reservationFixedCharge.ArticlesCode = "";
                reservationFixedCharge.BeginDate = reservation.ArrivalDate;
                reservationFixedCharge.EndDate = reservation.DepartureDate;
                reservationFixedCharge.Quantity = 1;
                reservationFixedCharge.Amount = 0;
                reservationFixedCharge.AmountAfterTax = 0;
                reservationFixedCharge.CurrencyID = "VND";
                reservationFixedCharge.PostingRhythmID = 1;
                reservationFixedCharge.PostingDate = businessDate[0].BusinessDate;
                reservationFixedCharge.PostingDay = "";
                reservationFixedCharge.ProfitCenterID = 0;
                reservationFixedCharge.Description = tran.Description;
                reservationFixedCharge.IsTaxInclude = false;
                reservationFixedCharge.UserInsertID = reservationFixedCharge.UserUpdateID = reservation.UserInsertId;
                reservationFixedCharge.UpdateDate = reservationFixedCharge.CreateDate = DateTime.Now;
                ReservationFixedChargeBO.Instance.Insert(reservationFixedCharge);
                pt.CommitTransaction();
                return Json(new { code = 0, msg = "New reservation fixed charge created successfully" });

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

        #region DatVP __ Reservation: deposit
        [HttpGet]
        public async Task<IActionResult> GetDepositRequestByReservationID(int reservationID)
        {
            try
            {

                DataTable myData = _iDepositService.SearchDepositRequest(reservationID);
                DataTable myData2 = _iDepositService.SearchDepositPayment(reservationID, 0);

                var result = (from d in myData.AsEnumerable()

                              select new
                              {
                                  ResqID = d["RsqID"].ToString(),
                                  DueDate = d["DueDate"].ToString(),
                                  ReservationID = d["ReservationID"].ToString(),
                                  Percentage = d["Percentage"].ToString(),
                                  Request = d["Request"].ToString(),
                                  Type = d["Type"].ToString(),
                                  DueAmount = d["DueAmount"].ToString(),
                                  PaidAmount = d["PaidAmount"].ToString(),
                                  Currency = d["Currency"].ToString(),
                                  Comment = d["Comment"].ToString(),
                                  RuleCode = d["RuleCode"].ToString(),
                                  CurrencyID = d["CurrencyID"].ToString(),
                                  CreateDate = d["CreateDate"].ToString(),
                              }).ToList();

                var result2 = (from d in myData2.AsEnumerable()

                               select new
                               {
                                   PaymentID = d["PaymentID"].ToString(),
                                   TransactionDate = d["TransactionDate"].ToString(),
                                   Code = d["Code"].ToString(),
                                   Description = d["Description"].ToString(),
                                   Reference = d["Reference"].ToString(),
                                   Supplement = d["Supplement"].ToString(),
                                   ReceiptNo = d["ReceiptNo"].ToString(),
                                   Amount = d["Amount"].ToString(),
                                   CurrencyID = d["CurrencyID"].ToString(),
                                   AmountUSD = d["AmountUSD"].ToString(),
                                   AmountVND = d["AmountVND"].ToString(),
                                   IsProcess = d["IsProcess"].ToString(),
                                   ShiftID = d["ShiftID"].ToString(),
                                   CreateDate = d["CreateDate"].ToString(),
                                   CreateBy = d["CreatedBy"].ToString(),

                               }).ToList();
                return Json(new
                {
                    request = result,
                    payment = result2
                });

            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetDepositRule()
        {
            try
            {
                List<DepositRuleModel> depositRules = PropertyUtils.ConvertToList<DepositRuleModel>(DepositRuleBO.Instance.FindAll());

                return Json(depositRules);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDepositRuleByID(int id)
        {
            try
            {
                DepositRuleModel deposit = (DepositRuleModel)DepositRuleBO.Instance.FindByPrimaryKey(id);

                return Json(deposit);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        [HttpPost]
        public ActionResult SaveDepositRequest()
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();
                List<BusinessDateModel> businessDate = PropertyUtils.ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll());
                if (decimal.Parse(Request.Form["amount"].ToString()) == 0)
                {
                    return Json(new { code = 1, msg = "Deposit amount can be not equal 0" });
                }
                DepositRsqModel depositRsq = new DepositRsqModel();
                depositRsq.ReservationID = int.Parse(Request.Form["rsvID"].ToString());
                depositRsq.RequestDate = businessDate[0].BusinessDate;
                depositRsq.ChargeType = int.Parse(Request.Form["chargeType"].ToString());
                depositRsq.DepositRuleID = int.Parse(Request.Form["depositeRuleID"].ToString());
                depositRsq.DueDate = DateTime.Parse(Request.Form["dueDate"].ToString());
                depositRsq.Amount = decimal.Parse(Request.Form["amount"].ToString());
                depositRsq.AmountMaster = decimal.Parse(Request.Form["amount"].ToString());
                depositRsq.CurrencyID = depositRsq.CurrencyMaster = Request.Form["curencyID"].ToString();
                depositRsq.IsMasterFolio = false;
                depositRsq.UserInsertID = depositRsq.UserUpdateID = int.Parse(Request.Form["userID"].ToString());
                depositRsq.UpdateDate = depositRsq.CreateDate = DateTime.Now;
                depositRsq.IsAuto = false;
                depositRsq.RequestType = int.Parse(Request.Form["requestType"].ToString());
                depositRsq.PaidAmount = 0;
                depositRsq.DueAmount = decimal.Parse(Request.Form["amount"].ToString());
                DepositRsqBO.Instance.Insert(depositRsq);
                pt.CommitTransaction();
                return Json(new { code = 0, msg = "Deposit Payment was created successfully" });

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
        public ActionResult SaveDepositPayment()
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();
                List<BusinessDateModel> businessDate = PropertyUtils.ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll());
                if (int.Parse(Request.Form["transCode"].ToString()) == 0)
                {
                    return Json(new { code = 1, msg = "Transaction type  can be not equal 0" });
                }
                if (decimal.Parse(Request.Form["amount"].ToString()) == 0)
                {
                    return Json(new { code = 1, msg = "Payment amount can be not equal 0" });
                }
                #region lấy user
                UsersModel userLogin = (UsersModel)UsersBO.Instance.FindByPrimaryKey(int.Parse(Request.Form["userID"].ToString()));
                #endregion
                #region lấy transaction code thanh toán
                TransactionsModel trans = (TransactionsModel)TransactionsBO.Instance.FindByPrimaryKey(int.Parse(Request.Form["transCode"].ToString()));
                #endregion
                #region insert deposit payment 
                DepositPaymentModel payment = new DepositPaymentModel();
                payment.ReservationID = int.Parse(Request.Form["rsvID"].ToString());
                payment.DepositRsqID = 0;
                payment.TransactionDate = DateTime.Parse(Request.Form["dueDate"].ToString());
                payment.TransactionCode = trans.Code;
                payment.Reference = Request.Form["reference"].ToString();
                payment.Description = trans.Description;
                payment.Supplement = Request.Form["supplement"].ToString();
                payment.Amount = 0 - decimal.Parse(Request.Form["amount"].ToString());
                payment.CurrencyID = Request.Form["curencyID"].ToString();
                payment.AmountMaster = 0 - decimal.Parse(Request.Form["amount"].ToString());
                payment.CurrencyMaster = Request.Form["curencyID"].ToString();
                payment.IsProcess = false;
                payment.ReceiptNo = "";
                payment.IsMasterFolio = false;
                payment.UserID = int.Parse(Request.Form["userID"].ToString());
                payment.UserName = Request.Form["userName"].ToString();
                payment.CashierNo = userLogin.CashierNo.ToString();
                payment.ShiftID = int.Parse(Request.Form["shiftID"].ToString());
                payment.UserInsertID = int.Parse(Request.Form["userID"].ToString());
                payment.UserUpdateID = int.Parse(Request.Form["userID"].ToString());
                payment.CreateDate = DateTime.Now;
                payment.UpdateDate = DateTime.Now;
                payment.ReservationTypeID = 2;
                payment.PaymentCode = trans.Code;
                long paymentID = DepositPaymentBO.Instance.Insert(payment);
                #endregion

                #region update lại ReceiptNo của deposit payment  vừa insert
                DepositPaymentModel paymentModel = (DepositPaymentModel)DepositPaymentBO.Instance.FindByPrimaryKey((int)paymentID);
                paymentModel.ReceiptNo = paymentModel.ID.ToString();
                DepositPaymentBO.Instance.Update(paymentModel);
                #endregion

                #region insert deposit payment default
                ConfigSystemModel config = PropertyUtils.ConvertToList<ConfigSystemModel>(ConfigSystemBO.Instance.FindByAttribute("KeyName", "DEPOSIT")).FirstOrDefault();
                DepositPaymentModel paymentDefault = new DepositPaymentModel();
                paymentDefault.ReservationID = int.Parse(Request.Form["rsvID"].ToString());
                paymentDefault.DepositRsqID = 0;
                paymentDefault.TransactionDate = DateTime.Parse(Request.Form["dueDate"].ToString());
                paymentDefault.TransactionCode = config.KeyValue;
                paymentDefault.Reference = Request.Form["reference"].ToString();
                paymentDefault.Description = trans.Description;
                paymentDefault.Supplement = Request.Form["supplement"].ToString();
                paymentDefault.Amount = decimal.Parse(Request.Form["amount"].ToString());
                paymentDefault.CurrencyID = Request.Form["curencyID"].ToString();
                paymentDefault.AmountMaster = decimal.Parse(Request.Form["amount"].ToString());
                paymentDefault.CurrencyMaster = Request.Form["curencyID"].ToString();
                paymentDefault.IsProcess = false;
                paymentDefault.ReceiptNo = paymentID.ToString();
                paymentDefault.IsMasterFolio = false;
                paymentDefault.UserID = int.Parse(Request.Form["userID"].ToString());
                paymentDefault.UserName = Request.Form["userName"].ToString();
                paymentDefault.CashierNo = userLogin.CashierNo.ToString();
                paymentDefault.ShiftID = int.Parse(Request.Form["shiftID"].ToString());
                paymentDefault.UserInsertID = int.Parse(Request.Form["userID"].ToString());
                paymentDefault.UserUpdateID = int.Parse(Request.Form["userID"].ToString());
                paymentDefault.CreateDate = DateTime.Now;
                paymentDefault.UpdateDate = DateTime.Now;
                paymentDefault.ReservationTypeID = 2;
                paymentDefault.PaymentCode = trans.Code;
                DepositPaymentBO.Instance.Insert(paymentDefault);
                #endregion



                #region update lại paid và due của các deposit payment
                decimal totalDepositAmountPayment = decimal.Parse(Request.Form["amount"].ToString());
                List<DepositRsqModel> listDepositRequest = PropertyUtils.ConvertToList<DepositRsqModel>(DepositRsqBO.Instance.FindByAttribute("ReservationID", paymentModel.ReservationID));
                int i = 0;
                while (i < listDepositRequest.Count && totalDepositAmountPayment > 0)
                {
                    if (totalDepositAmountPayment > listDepositRequest[i].Amount)
                    {
                        listDepositRequest[i].PaidAmount = listDepositRequest[i].Amount;

                    }
                    else
                    {
                        listDepositRequest[i].PaidAmount = totalDepositAmountPayment;

                    }
                    listDepositRequest[i].DueAmount = listDepositRequest[i].Amount - listDepositRequest[i].PaidAmount;
                    DepositRsqBO.Instance.Update(listDepositRequest[i]);
                    totalDepositAmountPayment = totalDepositAmountPayment - listDepositRequest[i].Amount;
                    i++;
                }
                #endregion
                #region insert deposit request
                DepositRsqModel depositRsq = new DepositRsqModel();
                depositRsq.ReservationID = int.Parse(Request.Form["rsvID"].ToString());
                depositRsq.RequestDate = businessDate[0].BusinessDate;
                depositRsq.Description = "";
                depositRsq.ChargeType = 0;
                depositRsq.DepositRuleID = 0;
                depositRsq.DueDate = DateTime.Parse(Request.Form["dueDate"].ToString());
                depositRsq.Amount = depositRsq.AmountMaster = 0;
                depositRsq.CurrencyID = "VND";
                depositRsq.CurrencyMaster = "";
                depositRsq.IsMasterFolio = false;
                depositRsq.UserInsertID = depositRsq.UserUpdateID = int.Parse(Request.Form["userID"].ToString());
                depositRsq.UpdateDate = depositRsq.CreateDate = DateTime.Now;
                depositRsq.IsAuto = false;
                depositRsq.RequestType = 0;
                depositRsq.PaidAmount = depositRsq.DueAmount = 0 - decimal.Parse(Request.Form["amount"].ToString());
                DepositRsqBO.Instance.Insert(depositRsq);
                #endregion
                pt.CommitTransaction();
                return Json(new { code = 0, msg = "Deposit Payment was created successfully" });

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
        public ActionResult PrintReceipt(int id, string userName)
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();
                string url = "";
                DepositPaymentModel deposit = (DepositPaymentModel)DepositPaymentBO.Instance.FindByPrimaryKey(id);
                ReservationModel reservation = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(deposit.ReservationID);

                XtraReport report = new Templates.DepositReceipt.Report1();
                report.Parameters["Date"].Value = deposit.TransactionDate.ToString("dd/MM/yyyy");
                report.Parameters["GuestName"].Value = reservation.ArrivalDate.ToString().Split(" ")[0];
                report.Parameters["PaymentMethod"].Value = deposit.Description.ToString();
                report.Parameters["ReceiptNo"].Value = deposit.ReceiptNo.ToString();
                report.Parameters["ReceiptReason"].Value = $"Deposit Res.No {deposit.ReservationID}";
                report.Parameters["RoomNo"].Value = reservation.RoomNo.ToString();
                report.Parameters["Time"].Value = deposit.TransactionDate.ToString("hh:mm");
                report.Parameters["User"].Value = userName;
                report.Parameters["TotalAmount"].Value =
          Math.Abs(deposit.AmountMaster)
              .ToString("#,##0.00", System.Globalization.CultureInfo.InvariantCulture)
              .Replace(",", ".");

                report.Parameters["ConfNo"].Value = reservation.ConfirmationNo;
                report.Parameters["ArrDate"].Value = reservation.ArrivalDate.ToString("dd/MM/yyyy");
                report.Parameters["DepDate"].Value = reservation.DepartureDate.ToString("dd/MM/yyyy");
                report.Parameters["CurrencyID"].Value = reservation.CurrencyId;
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

        #region DatVP __ Reservation: routing
        [HttpGet]
        public async Task<IActionResult> SearchRouting(string reservationID, string confirmationNo)
        {
            try
            {

                DataTable myData = _iRoutingService.SearchRouting(reservationID, confirmationNo);

                var result = (from d in myData.AsEnumerable()

                              select new
                              {
                                  AccountName = d["AccountName"].ToString(),
                                  TransactionCodes = d["TransactionCodes"].ToString(),
                                  FromDate = Convert.ToDateTime(d["FromDate"]).ToString("dd/MM/yyyy"),
                                  ToDate = Convert.ToDateTime(d["ToDate"]).ToString("dd/MM/yyyy"),
                                  RoomNo = d["RoomNo"].ToString(),
                                  ToFolioNo = d["ToFolioNo"].ToString(),
                                  Limit = d["Limit"].ToString(),
                                  IsMasterFolio = d["IsMasterFolio"].ToString(),
                                  UserInsertID = d["UserInsertID"].ToString(),
                                  CreateDate = Convert.ToDateTime(d["CreateDate"]).ToString("dd/MM/yyyy"),
                                  UserUpdateID = d["UserUpdateID"].ToString(),
                                  UpdateDate = Convert.ToDateTime(d["UpdateDate"]).ToString("dd/MM/yyyy"),
                                  ID = d["ID"].ToString(),
                                  ToReservationID = d["ToReservationID"].ToString(),

                              }).ToList();

                return Json(result);

            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }


        [HttpPost]
        public ActionResult SaveRouting()
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();
                List<BusinessDateModel> businessDate = PropertyUtils.ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll());
                if (int.Parse(Request.Form["transactionCodes"].ToString()) == 0)
                {
                    return Json(new { code = 1, msg = "Please choose transaction" });
                }
                ReservationModel res = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(int.Parse(Request.Form["rsvID"].ToString()));
                RoutingModel routing = new RoutingModel();
                routing.Type = int.Parse(Request.Form["transactionRouting"].ToString());
                routing.FromReservationID = int.Parse(Request.Form["rsvID"].ToString());
                routing.ToReservationID = int.Parse(Request.Form["rsvID"].ToString());
                routing.ToFolioNo = int.Parse(Request.Form["toFolioNo"].ToString());
                routing.ToRoomID = int.Parse(Request.Form["toRoom"].ToString());
                routing.ProfileID = res.ProfileIndividualId;
                routing.AccountName = res.LastName;
                routing.TransactionCodes = Request.Form["transactionCodes"].ToString() + ",";
                routing.Limit = decimal.Parse(Request.Form["transactionCodes"].ToString());
                routing.Percents = 0;
                routing.FromDate = DateTime.Parse(Request.Form["fromDate"].ToString());
                routing.ToDate = DateTime.Parse(Request.Form["toDate"].ToString());
                if (Request.Form["entireDate"].ToString() == "1")
                {
                    routing.EntireDate = true;

                }
                else
                {
                    routing.EntireDate = false;

                }
                routing.IsDefault = false;
                routing.IsMasterFolio = false;
                routing.ConfirmationNo = res.ConfirmationNo;
                routing.UserInsertID = routing.UserUpdateID = int.Parse(Request.Form["userID"].ToString());
                routing.UpdateDate = routing.CreateDate = DateTime.Now;
                RoutingBO.Instance.Insert(routing);
                pt.CommitTransaction();
                return Json(new { code = 0, msg = "Routing transaction was created successfully" });

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
        public async Task<IActionResult> SearchReservationMaster(int reservationID)
        {
            try
            {
                List<ReservationModel> reservations = ReservationBO.GetReservationMaster(reservationID);

                return Json(reservations.Count);

            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> SearchRoomRouting()
        {
            try
            {
                string sqlCommand = "SELECT dbo.Reservation.RoomID, dbo.Room.RoomNo, dbo.Reservation.FirstName, dbo.Reservation.LastName, dbo.Reservation.ArrivalDate,   dbo.Reservation.DepartureDate, dbo.Reservation.ID  FROM dbo.Reservation WITH (NOLOCK) INNER JOIN dbo.Room WITH (NOLOCK) ON dbo.Reservation.RoomID = dbo.Room.ID  WHERE (dbo.Reservation.Status =1 OR dbo.Reservation.Status =6) And  MainGuest = 1  And Reservation.ID <> 56802 And dbo.Room.RoomNo like '%'";
                DataTable myData = _iRoutingService.SearchAllForTrans(sqlCommand);

                var result = (from d in myData.AsEnumerable()

                              select new
                              {
                                  RoomID = d["RoomID"].ToString(),
                                  RoomNo = d["RoomNo"].ToString(),
                                  FirstName = d["FirstName"].ToString(),
                                  LastName = d["LastName"].ToString(),
                                  ArrivalDate = d["ArrivalDate"].ToString(),
                                  DepartureDate = d["DepartureDate"].ToString(),
                                  ID = d["ID"].ToString(),

                              }).ToList();

                return Json(result);

            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        #endregion

        #region DatVP __ Group Reservation
        [HttpGet]
        public async Task<IActionResult> SearchGroupReservation(DateTime fromDate, DateTime toDate, int noOfNight)
        {
            try
            {
                DataTable myData = _iGroupReservationService.GetGroupReservation(fromDate, toDate, noOfNight);

                var result = (from d in myData.AsEnumerable()

                              select new
                              {
                                  ConfirmationNo = d["ConfirmationNo"].ToString(),
                                  CRSNo = d["CRSNo"].ToString(),
                                  ReservationTypeCode = d["ReservationTypeCode"].ToString(),
                                  TotalRoom = d["TotalRoom"].ToString(),
                                  ArrivalDate = d.Field<DateTime?>("ArrivalDate"),
                                  Nights = d["Nights"].ToString(),
                                  DepartureDate = d.Field<DateTime?>("DepartureDate"),
                                  GroupCode = d["GroupCode"].ToString(),
                                  ReservationHolder = d["ReservationHolder"].ToString(),
                                  SaleInCharge = d["SaleInCharge"].ToString(),
                                  RoomNight = d["RoomNight"].ToString(),
                                  Persons = d["Persons"].ToString(),
                                  RoomOccupancy = d["RoomOccupancy"].ToString(),
                                  PersonRoom = d["PersonRoom"].ToString(),
                                  MarketCode = d["MarketCode"].ToString(),
                                  SourceCode = d["SourceCode"].ToString(),
                                  ReservationDate = d.Field<DateTime?>("ReservationDate"),
                                  Price = d["Price"].ToString(),
                                  AmountBeforTax = d["AmountBeforTax"].ToString(),
                                  AmountAfterTax = d["AmountAfterTax"].ToString(),
                                  CurrencyID = d["CurrencyID"].ToString(),
                                  OptionDate = d["OptionDate"].ToString(),
                                  OptionDateDesc = d["OptionDateDesc"].ToString(),
                              }).ToList();


                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        public async Task<IActionResult> SearchGroupReservationTest(DateTime fromDate, DateTime toDate, int noOfNight, int skip = 0, int take = 20)
        {
            try
            {

                (DataTable myData, int totalCount) = _iGroupReservationService.GetGroupReservationTest(fromDate, toDate, noOfNight, skip, take);

                var result = (from d in myData.AsEnumerable()

                              select new
                              {
                                  ConfirmationNo = d["ConfirmationNo"].ToString(),
                                  CRSNo = d["CRSNo"].ToString(),
                                  ReservationTypeCode = d["ReservationTypeCode"].ToString(),
                                  TotalRoom = d["TotalRoom"].ToString(),
                                  ArrivalDate = d.Field<DateTime?>("ArrivalDate"),
                                  Nights = d["Nights"].ToString(),
                                  DepartureDate = d.Field<DateTime?>("DepartureDate"),
                                  GroupCode = d["GroupCode"].ToString(),
                                  ReservationHolder = d["ReservationHolder"].ToString(),
                                  SaleInCharge = d["SaleInCharge"].ToString(),
                                  Persons = d["Persons"].ToString(),
                                  RoomOccupancy = d["RoomOccupancy"].ToString(),
                                  MarketCode = d["MarketCode"].ToString(),
                                  SourceCode = d["SourceCode"].ToString(),
                                  ReservationDate = d.Field<DateTime?>("ReservationDate"),
                                  Price = d["Price"].ToString(),
                                  AmountBeforTax = d["AmountBeforTax"].ToString(),
                                  AmountAfterTax = d["AmountAfterTax"].ToString(),
                                  CurrencyID = d["CurrencyID"].ToString(),
                                  OptionDate = d["OptionDate"].ToString(),
                                  OptionDateDesc = d["OptionDateDesc"].ToString(),
                                  PersonRoom = d.Table.Columns.Contains("PersonRoom") ? d["PersonRoom"].ToString() : "0",
                                  RoomNight = d.Table.Columns.Contains("RoomNight") ? d["RoomNight"].ToString() : "0",
                              }).ToList();


                return Json(new { data = result, totalCount = totalCount });
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetRoomAvailable(DateTime fromDate, DateTime toDate)
        {
            try
            {
                SqlParameter[] param = new SqlParameter[]
                {
                    new SqlParameter("@FromDate", fromDate),
                    new SqlParameter("@ToDate", toDate),
                };
                DataTable data = DataTableHelper.getTableData("spAvailableRoomDetailfromtodate ", param);
                var result = (from d in data.AsEnumerable()
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

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        public class GetInfoRoom
        {
            public int ID { get; set; }
            public string RoomTypeCode { get; set; }
            public int NumberOfRoom { get; set; }
            public decimal RateCode { get; set; }
        }

        [HttpPost]
        public ActionResult SaveGroupReservation()
        {
            ProcessTransactions pt = new ProcessTransactions();

            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();
                var rawGridData = Request.Form["gridData"];
                var gridData = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(rawGridData);
                List<GetInfoRoom> listRoom = new List<GetInfoRoom>();
                if (int.Parse(Request.Form["profileIndividualID"].ToString()) == 0)
                {
                    return Json(new { code = 1, msg = "Please choose profile" });

                }
                if (int.Parse(Request.Form["resType"].ToString()) == 0)
                {
                    return Json(new { code = 1, msg = "Please choose Reservation Type" });

                }
                if (int.Parse(Request.Form["market"].ToString()) == 0)
                {
                    return Json(new { code = 1, msg = "Please choose market" });

                }
                foreach (var row in gridData)
                {
                    var NumofRoom = row.ContainsKey("NumofRoom") ? int.Parse(row["NumofRoom"]?.ToString()) : 0;
                    if (NumofRoom == 0)
                    {
                        continue;
                    }
                    var roomTyeID = row.ContainsKey("RoomTypeID") ? int.Parse(row["RoomTypeID"]?.ToString()) : 0;
                    var roomTypeCode = row.ContainsKey("RoomType") ? row["RoomType"]?.ToString() : "";
                    var rateCode = row.ContainsKey("RateCode") ? decimal.Parse(row["RateCode"]?.ToString()) : 0;
                    GetInfoRoom room = new GetInfoRoom();
                    room.ID = roomTyeID;
                    room.RoomTypeCode = roomTypeCode;
                    room.NumberOfRoom = NumofRoom;
                    room.RateCode = rateCode;
                    listRoom.Add(room);
                    //// Xử lý các cột là ngày
                    //foreach (var key in row.Keys)
                    //{
                    //    if (DateTime.TryParse(key, out DateTime date))
                    //    {
                    //        var value = Convert.ToInt32(row[key]); // số phòng còn
                    //                                                // TODO: Lưu ngày, số lượng phòng theo từng ngày
                    //    }
                    //}
                }

                if (listRoom.Count > 0)
                {
                    List<BusinessDateModel> businessDate = PropertyUtils.ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll());
                    string transactionCode = PropertyUtils.ConvertToList<ConfigSystemModel>(ConfigSystemBO.Instance.FindAll()).
                    Where(x => x.KeyName == "RoomCharge").ToList()[0].KeyValue;
                    var confomationNo = (ReservationBO.GetTopConfirmationNo() + 1).ToString();
                    foreach (var room in listRoom)
                    {

                        #region tạo reservation
                        ReservationModel reservationModel = new ReservationModel();
                        reservationModel.ConfirmationNo = confomationNo;
                        reservationModel.ReservationNo = (ReservationBO.GetTopID() + 1).ToString();
                        reservationModel.ReservationDate = businessDate[0].BusinessDate;
                        reservationModel.ProfileAgentId = 0;
                        reservationModel.AgentName = "";
                        reservationModel.ProfileCompanyId = 0;
                        reservationModel.CompanyName = "";
                        reservationModel.ProfileSourceId = 0;
                        reservationModel.SourceName = "";
                        reservationModel.ProfileGroupId = 0;
                        reservationModel.GroupCode = "";
                        reservationModel.GroupName = "";
                        reservationModel.ProfileContactId = 0;
                        reservationModel.ContactName = "";
                        reservationModel.ContactPhone = "";
                        reservationModel.ProfileComment = "";
                        reservationModel.ProfileIndividualId = int.Parse(Request.Form["profileIndividualID"].ToString());
                        reservationModel.LastName = Request.Form["lastName"].ToString();
                        reservationModel.FirstName = Request.Form["lastName"].ToString();
                        reservationModel.Title = "";
                        reservationModel.Phone = "";
                        reservationModel.Email = "";
                        reservationModel.MemberType = "";
                        reservationModel.MemberLevel = "";
                        reservationModel.MemberNo = "";
                        reservationModel.VipId = 0;
                        reservationModel.Vip = "";
                        reservationModel.Address = "";
                        reservationModel.City = "";
                        reservationModel.Zip = "";
                        reservationModel.State = "";
                        reservationModel.Country = "";
                        reservationModel.Language = "";
                        reservationModel.ArrivalDate = DateTime.Parse(Request.Form["arrivalDate"].ToString());
                        reservationModel.OriginalArrivalDate = DateTime.Parse(Request.Form["arrivalDate"].ToString());
                        reservationModel.NoOfNight = int.Parse(Request.Form["noOfNight"].ToString());
                        reservationModel.DepartureDate = DateTime.Parse(Request.Form["departureDate"].ToString());
                        reservationModel.OriginalDepartureDate = DateTime.Parse(Request.Form["departureDate"].ToString());
                        reservationModel.NoOfAdult = 2;
                        reservationModel.NoOfChild = 0;
                        reservationModel.NoOfChild1 = 0;
                        reservationModel.NoOfChild2 = 0;
                        reservationModel.NoOfRoom = room.NumberOfRoom;
                        reservationModel.RoomTypeId = room.ID;
                        reservationModel.RoomType = room.RoomTypeCode;
                        reservationModel.RtcId = room.ID;
                        reservationModel.RoomId = 0;
                        reservationModel.RoomNo = "";
                        reservationModel.BusinessBlockId = 0;
                        reservationModel.BusinessBlockCode = "";
                        reservationModel.Eta = "14:00";
                        reservationModel.CheckInDate = DateTime.Parse(Request.Form["arrivalDate"].ToString());
                        reservationModel.Etd = "12:00";
                        reservationModel.CheckOutDate = DateTime.Parse(Request.Form["arrivalDate"].ToString());
                        reservationModel.ReservationTypeId = int.Parse(Request.Form["resType"].ToString());
                        reservationModel.ReservationTypeCode = Request.Form["resTypeCode"].ToString();
                        reservationModel.MarketId = int.Parse(Request.Form["market"].ToString());
                        reservationModel.MarketCode = Request.Form["marketCode"].ToString();
                        reservationModel.SourceId = 0;
                        reservationModel.SourceCode = "";
                        reservationModel.OriginId = 0;
                        reservationModel.OriginCode = "";
                        reservationModel.CCHolder = "";
                        reservationModel.BookerId = 0;
                        reservationModel.BookerName = "";
                        reservationModel.BookerDetails = "";
                        reservationModel.NoPost = false;
                        reservationModel.PrintRate = true;
                        reservationModel.ConfirmationStatus = true;
                        reservationModel.VideoCheckOutStatus = false;
                        reservationModel.CRSNo = "";
                        reservationModel.DiscountAmount = 0;
                        reservationModel.DiscountRate = 0;
                        reservationModel.Comment = "";
                        reservationModel.BalanceUSD = 0;
                        reservationModel.BalanceVND = 0;
                        reservationModel.ApprovalCode = "";
                        reservationModel.ApprovalAmount = 0;
                        reservationModel.SuiteWith = "";
                        reservationModel.PaymentMethod = "";
                        reservationModel.CreditCardNo = "";
                        reservationModel.ExpirationDate = DateTime.Now;
                        reservationModel.TaxTypeId = 0;
                        reservationModel.ExemptNumber = "";
                        reservationModel.PickupReqdId = 0;
                        reservationModel.PickupTransportType = "";
                        reservationModel.PickupStationCode = "";
                        reservationModel.PickupCarrierCode = "";
                        reservationModel.PickupTime = Request.Form["arrivalDate"].ToString();
                        reservationModel.PickupTransportNo = "";
                        reservationModel.PickupDescription = "";
                        reservationModel.DropOffReqdId = 0;
                        reservationModel.DropOffTransportType = "";
                        reservationModel.DropOffStationCode = "";
                        reservationModel.DropOffCarrierCode = "";
                        reservationModel.DropOffTime = "";
                        reservationModel.DropOffTransportNo = "";
                        reservationModel.DropOffDepartureDate = DateTime.Parse(Request.Form["arrivalDate"].ToString());
                        reservationModel.DropOffDescription = "";
                        reservationModel.PackageId = int.Parse(Request.Form["package"].ToString());
                        reservationModel.Packages = Request.Form["packageCode"].ToString();
                        reservationModel.Relationship = ReservationBO.GetTopID() + 1;
                        reservationModel.Status = DateTime.Parse(Request.Form["arrivalDate"].ToString()) == businessDate[0].BusinessDate ? 5 : 0;
                        reservationModel.PostingMaster = false;
                        reservationModel.MainGuest = true;
                        reservationModel.RateCodeId = 0;
                        reservationModel.RateCode = "";

                        reservationModel.Rate = _iGroupReservationService.CalculatePriceFromNet(room.RateCode, transactionCode);
                        reservationModel.RateAfterTax = room.RateCode;
                        reservationModel.FixedRate = false;
                        reservationModel.TotalAmount = room.RateCode;
                        reservationModel.CurrencyId = "VND";
                        reservationModel.Party = "";
                        reservationModel.PartyGuest = "";
                        reservationModel.IsPasserBy = false;
                        reservationModel.Color = "";
                        reservationModel.ARNo = "";
                        reservationModel.ItemInventory = "";
                        reservationModel.Specials = "";
                        reservationModel.ShareRoom = ReservationBO.GetTopID() + 1;
                        reservationModel.NoShowStatus = false;
                        reservationModel.ShareRoomName = "";
                        reservationModel.AccompanyName = "";
                        reservationModel.RoutingTransaction = "";
                        reservationModel.RoutingToProfile = "";
                        reservationModel.FixedCharge = "";
                        reservationModel.CommentGroup = "";
                        reservationModel.IsWalkIn = false;
                        reservationModel.UserInsertId = int.Parse(Request.Form["userID"].ToString());
                        reservationModel.CreateDate = DateTime.Now;
                        reservationModel.UserUpdateId = int.Parse(Request.Form["userID"].ToString());
                        reservationModel.UpdateDate = DateTime.Now;
                        reservationModel.CreateBy = Request.Form["userName"].ToString();
                        reservationModel.UpdateBy = Request.Form["userName"].ToString();
                        reservationModel.SpecialUpdateBy = Request.Form["userName"].ToString();
                        reservationModel.SpecialUpdateDate = DateTime.Now;
                        reservationModel.IsAdvanceBill = false;
                        reservationModel.AllotmentId = 0;
                        reservationModel.AllotmentCode = "";

                        reservationModel.PinCode = (ReservationBO.GetTopID() + 1).ToString();
                        reservationModel.PersonInChargeId = 0;
                        reservationModel.RoomNight = 1;
                        reservationModel.CardId = "";
                        reservationModel.Breakfast = false;
                        reservationModel.Dinner = false;
                        reservationModel.Lunch = false;
                        reservationModel.FixedMeal = false;
                        reservationModel.VoucherId = "";
                        ReservationBO.Instance.Insert(reservationModel);
                        #endregion

                        #region tạo folio detail
                        FolioModel folioModel = new FolioModel();
                        folioModel.ARNo = "";
                        folioModel.FolioDate = reservationModel.ReservationDate;
                        folioModel.FolioNo = 1;
                        folioModel.ReservationID = reservationModel.ID;
                        folioModel.ProfileID = reservationModel.ProfileIndividualId;
                        folioModel.AccountName = reservationModel.LastName;
                        folioModel.Status = true;
                        folioModel.ConfirmationNo = reservationModel.ConfirmationNo;
                        folioModel.BalanceUSD = folioModel.BalanceVND = 0;
                        folioModel.CreateDate = folioModel.UpdateDate = DateTime.Now;
                        folioModel.UserInsertID = folioModel.UserUpdateID = reservationModel.UserInsertId;
                        FolioBO.Instance.Insert(folioModel);
                        #endregion

                    }
                    return Json(new { code = 0, msg = "Group Profile was created successffully" });

                }
                else
                {
                    return Json(new { code = 1, msg = "Please type value of number of room" });

                }
                pt.CommitTransaction();


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



            // Xử lý lưu
        }
        #endregion

        #region DatVP __ Reservation: Message
        [HttpGet]
        public async Task<IActionResult> SearchMessage(string Name, string ReservationHolder, string ConfirmationNo, string CRSNo, string RoomNo, string Zone, string Status, string Receive, string Print)
        {
            try
            {
                // Kiểm tra và gán giá trị rỗng nếu tham số là null
                if (string.IsNullOrEmpty(Name))
                {
                    Name = "";
                }
                if (string.IsNullOrEmpty(ReservationHolder))
                {
                    ReservationHolder = "";
                }
                if (string.IsNullOrEmpty(ConfirmationNo))
                {
                    ConfirmationNo = "";
                }
                if (string.IsNullOrEmpty(CRSNo))
                {
                    CRSNo = "";
                }
                if (string.IsNullOrEmpty(RoomNo))
                {
                    RoomNo = "";
                }
                if (string.IsNullOrEmpty(Zone))
                {
                    Zone = "";
                }
                if (!string.IsNullOrEmpty(Status))
                {
                    // Từ "0,5,1,6,2" => "0','5','1','6','2"
                    Status = string.Join("','", Status.Split(',').Select(s => s.Trim()));
                }
                if (!string.IsNullOrEmpty(Print))
                {
                    if (Print == "0")
                    {
                        // Nếu chỉ là 0 thì gán thành "0','1"
                        Print = "0','1";
                    }
                    else
                    {
                        // Nếu là chuỗi khác, ví dụ "0,2" thì tách như bình thường
                        Print = string.Join("','", Print.Split(',').Select(s => s.Trim()));
                    }
                }


                var data = _iMessageService.SearchMessage(Name, ReservationHolder, ConfirmationNo, CRSNo, RoomNo, Zone, Status, Receive, Print);

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

        #region DatVP __ Reservation: Delete
        [HttpPost]
        public ActionResult DeleteReservation()
        {
            try
            {

                ReservationModel reservation = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(int.Parse(Request.Form["reservationID"].ToString()));
                if (reservation == null || reservation.ID == 0)
                {
                    return Json(new { code = 1, msg = "Can not find reservation" });

                }
                ReservationBO.Instance.Delete(int.Parse(Request.Form["reservationID"].ToString()));
                return Json(new { code = 0, msg = "Delete reservation was successfully" });

            }
            catch (Exception ex)
            {
                return Json(new { code = 1, msg = ex.Message });
            }

        }
        #endregion

        #region DatVP __ Reservation: Share
        [HttpGet]
        public async Task<IActionResult> SearchShare(int reservationID)
        {
            try
            {

                ReservationModel reservation = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(reservationID);
                if (reservation == null || reservation.ID == 0)
                {
                    return Json(new
                    {
                        code = 1,
                        msg = "Can not find reservation"
                    });
                }
                var dataRateDetail = _iShareService.ShareRateDetail(reservationID);
                var dataRoomDetail = _iShareService.ShareRoomDetail(reservationID);
                var dataReservationDetail = _iShareService.ShareReservationDetail(reservationID, reservation.ShareRoom);

                var resultRateDetail = (from d in dataRateDetail.AsEnumerable()
                                        select d.Table.Columns.Cast<DataColumn>()
                                            .ToDictionary(
                                                col => col.ColumnName,
                                                col => d[col.ColumnName]?.ToString()
                                            )).ToList();
                var resultRoomDetail = (from d in dataRoomDetail.AsEnumerable()
                                        select d.Table.Columns.Cast<DataColumn>()
                                            .ToDictionary(
                                                col => col.ColumnName,
                                                col => d[col.ColumnName]?.ToString()
                                            )).ToList();
                var resultReservationDetail = (from d in dataReservationDetail.AsEnumerable()
                                               select d.Table.Columns.Cast<DataColumn>()
                                                   .ToDictionary(
                                                       col => col.ColumnName,
                                                       col => d[col.ColumnName]?.ToString()
                                                   )).ToList();
                return Json(new
                {
                    code = 0,
                    msg = "Succesfully",
                    resultRateDetail = resultRateDetail,
                    resultRoomDetail = resultRoomDetail,
                    resultReservationDetail = resultReservationDetail
                });
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        #endregion

        #region DatVp __ Reservation: Reinstate
        [HttpPost]
        public ActionResult ReinstateReservation()
        {
            ProcessTransactions pt = new ProcessTransactions();

            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();
                ReservationModel reservation = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(int.Parse(Request.Form["reservationID"].ToString()));
                if (reservation == null || reservation.ID == 0)
                {
                    return Json(new { code = 1, msg = "Can not find reservation" });

                }
                string status = Request.Form["status"].ToString();
                int statusCode = 0;
                switch (status)
                {
                    case "RESVED":
                        statusCode = 0;
                        break;
                    case "CHECKED IN":
                        statusCode = 1;
                        break;
                    case "CHECKED OUT":
                        statusCode = 2;
                        break;
                    case "CANCEL":
                        statusCode = 3;
                        break;
                    default:
                        statusCode = 0;
                        break;
                }

                #region Reinstate booking với đang ở trạng thái cancel -> booking trở về trạng thái reservation và clear room
                if (statusCode == 3)
                {
                    #region update reservation
                    reservation.Status = 0;
                    reservation.RoomId = 0;
                    reservation.RoomNo = "";
                    reservation.UpdateBy = Request.Form["userName"].ToString();
                    reservation.UpdateDate = DateTime.Now;
                    reservation.UserUpdateId = int.Parse(Request.Form["userID"].ToString());
                    ReservationBO.Instance.Update(reservation);
                    #endregion
                    #region insert log actitvity
                    ActivityLogModel activityLogModel = new ActivityLogModel();
                    activityLogModel.TableName = "Reservation";
                    activityLogModel.ObjectID = reservation.ID;
                    activityLogModel.UserID = int.Parse(Request.Form["userID"].ToString());
                    activityLogModel.UserName = Request.Form["userName"].ToString();
                    activityLogModel.ChangeDate = DateTime.Now;
                    activityLogModel.Change = "Status";
                    activityLogModel.OldValue = "3";
                    activityLogModel.NewValue = "0";
                    activityLogModel.Description = "";
                    ActivityLogBO.Instance.Insert(activityLogModel);
                    #endregion
                }
                #endregion


                #region Reinstate booking với đang ở trạng thái check out -> booking trở về trạng thái due out
                if (statusCode == 2)
                {
                    #region update reservation
                    reservation.Status = 6;
                    reservation.UpdateBy = Request.Form["userName"].ToString();
                    reservation.UpdateDate = DateTime.Now;
                    reservation.UserUpdateId = int.Parse(Request.Form["userID"].ToString());
                    ReservationBO.Instance.Update(reservation);
                    #endregion
                    #region insert log actitvity
                    ActivityLogModel activityLogModel = new ActivityLogModel();
                    activityLogModel.TableName = "Reservation";
                    activityLogModel.ObjectID = reservation.ID;
                    activityLogModel.UserID = int.Parse(Request.Form["userID"].ToString());
                    activityLogModel.UserName = Request.Form["userName"].ToString();
                    activityLogModel.ChangeDate = DateTime.Now;
                    activityLogModel.Change = "Status";
                    activityLogModel.OldValue = "2";
                    activityLogModel.NewValue = "6";
                    activityLogModel.Description = "";
                    ActivityLogBO.Instance.Insert(activityLogModel);
                    #endregion
                }
                #endregion

                #region booking đang ở trạng thái check in -> booking trở về trạng thái due in
                if (statusCode == 1)
                {
                    #region check xem đã có trong folio detail hay chưa, nếu có dịch vụ rồi thì không cho reinstate
                    List<FolioDetailModel> folioDetails = PropertyUtils.ConvertToList<FolioDetailModel>(FolioDetailBO.Instance.FindByAttribute("ReservationID", reservation.ID));
                    if (folioDetails.Count > 0)
                    {
                        return Json(new { code = 1, msg = "Can not reinstate" });

                    }
                    #endregion

                    #region update reservation
                    reservation.Status = 5;
                    reservation.UpdateBy = Request.Form["userName"].ToString();
                    reservation.UpdateDate = DateTime.Now;
                    reservation.UserUpdateId = int.Parse(Request.Form["userID"].ToString());
                    ReservationBO.Instance.Update(reservation);
                    #endregion
                    #region insert log actitvity
                    ActivityLogModel activityLogModel = new ActivityLogModel();
                    activityLogModel.TableName = "Reservation";
                    activityLogModel.ObjectID = reservation.ID;
                    activityLogModel.UserID = int.Parse(Request.Form["userID"].ToString());
                    activityLogModel.UserName = Request.Form["userName"].ToString();
                    activityLogModel.ChangeDate = DateTime.Now;
                    activityLogModel.Change = "Status";
                    activityLogModel.OldValue = "1";
                    activityLogModel.NewValue = "5";
                    activityLogModel.Description = "";
                    ActivityLogBO.Instance.Insert(activityLogModel);
                    #endregion
                }
                #endregion
                pt.CommitTransaction();

                return Json(new { code = 0, msg = "Reinstate was successfully!" });

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

        #region DatVp __ Reservation: Split . Modified:3/2026 by TuanDB
        [HttpPost]
        public ActionResult SplitReservation()
        {
            try
            {
                int reservationID = int.Parse(Request.Form["reservationID"].ToString());
                int pNoOfRoom = int.Parse(Request.Form["noOfRoom"].ToString());
                int userID = int.Parse(Request.Form["userID"].ToString());
                string userName = Request.Form["userName"].ToString();
                string partyGuest = Request.Form["partyGuest"].ToString() ?? "";
                int roomID = 0;

                int newReservationID = _iReservationService.Split(
                    reservationID,
                    pNoOfRoom,
                    userID,
                    partyGuest,
                    roomID
                );

                return Json(new
                {
                    code = 0,
                    msg = "Split was successfully!",
                    reservationGuestID = newReservationID
                });
            }
            catch (Exception ex)
            {
                return Json(new { code = 1, msg = ex.Message });
            }
        }
        [HttpPost]
        public ActionResult SplitAllReservation()
        {
            // Hiện chỉ Fe chỉ nhận select single , dự phòng cho multislect 1 mảng ids

            try
            {
                string ids = Request.Form["reservationID"].ToString();
                string userIdText = Request.Form["userID"].ToString();
                string userName = Request.Form["userName"].ToString();
                string partyGuest = Request.Form["partyGuest"].ToString() ?? "";

                if (string.IsNullOrWhiteSpace(ids))
                {
                    return Json(new { code = 1, msg = "Can not find reservation" });
                }

                if (!int.TryParse(userIdText, out int userId))
                {
                    return Json(new { code = 1, msg = "Invalid userID" });
                }

                List<int> idList = ids
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => int.Parse(x.Trim()))
                    .Distinct()
                    .ToList();

                if (idList.Count == 0)
                {
                    return Json(new { code = 1, msg = "Can not find reservation" });
                }

                List<int> successIds = new List<int>();
                List<string> errorMessages = new List<string>();

                foreach (int reservationId in idList)
                {
                    try
                    {
                        ReservationModel reservation =
                            (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(reservationId);

                        if (reservation == null || reservation.ID == 0)
                        {
                            errorMessages.Add($"Reservation {reservationId}: Can not find reservation");
                            continue;
                        }

                        if (reservation.NoOfRoom <= 1)
                        {
                            errorMessages.Add($"Reservation {reservationId}: NoOfRoom must be greater than 1");
                            continue;
                        }

                        _iReservationService.SplitAll(
                            reservation.ID,
                            reservation.NoOfRoom,
                            userId,
                            userName,
                            partyGuest
                        );

                        successIds.Add(reservationId);
                    }
                    catch (Exception ex)
                    {
                        errorMessages.Add($"Reservation {reservationId}: {ex.Message}");
                    }
                }

                if (successIds.Count == 0)
                {
                    return Json(new
                    {
                        code = 1,
                        msg = "Split failed",
                        successReservationIds = successIds,
                        errors = errorMessages
                    });
                }

                return Json(new
                {
                    code = 0,
                    msg = errorMessages.Count == 0
                        ? "Split all was successfully!"
                        : "Split completed with some errors",
                    successReservationIds = successIds,
                    errors = errorMessages
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

        [HttpPost]
        public ActionResult SplitSpecialReservation()
        {
            try
            {
                int reservationID = int.Parse(Request.Form["reservationID"].ToString());
                int pNoOfRoom = int.Parse(Request.Form["noOfRoom"].ToString());
                int userID = int.Parse(Request.Form["userID"].ToString());
                string userName = Request.Form["userName"].ToString();
                string partyGuest = Request.Form["partyGuest"].ToString() ?? "";

                int newReservationID = _iReservationService.SplitSpecial(
                    reservationID,
                    pNoOfRoom,
                    userID,
                    partyGuest


                );

                return Json(new
                {
                    code = 0,
                    msg = "Split Special was successfully!",
                    reservationGuestID = newReservationID
                });
            }
            catch (Exception ex)
            {
                return Json(new { code = 1, msg = ex.Message });
            }

        }

        [HttpPost]
        public ActionResult SplitSpecialAllReservation()
        {
            // Hiện chỉ Fe chỉ nhận select single , dự phòng cho multislect 1 mảng ids
            try
            {
                string ids = Request.Form["reservationID"].ToString();
                string userIdText = Request.Form["userID"].ToString();
                string userName = Request.Form["userName"].ToString();
                string partyGuest = Request.Form["partyGuest"].ToString() ?? "";

                if (string.IsNullOrWhiteSpace(ids))
                {
                    return Json(new { code = 1, msg = "Can not find reservation" });
                }

                if (!int.TryParse(userIdText, out int userId))
                {
                    return Json(new { code = 1, msg = "Invalid userID" });
                }

                List<int> idList = [.. ids
                    .Split([','], StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => int.Parse(x.Trim()))
                    .Distinct()];

                if (idList.Count == 0)
                {
                    return Json(new { code = 1, msg = "Can not find reservation" });
                }

                List<int> successIds = [];
                List<string> errorMessages = [];

                foreach (int reservationId in idList)
                {
                    try
                    {
                        ReservationModel reservation =
                            (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(reservationId);

                        if (reservation == null || reservation.ID == 0)
                        {
                            errorMessages.Add($"Reservation {reservationId}: Can not find reservation");
                            continue;
                        }

                        if (reservation.NoOfRoom <= 1)
                        {
                            errorMessages.Add($"Reservation {reservationId}: NoOfRoom must be greater than 1");
                            continue;
                        }

                        _iReservationService.SplitAllSpecial(
                            reservation.ID,
                            reservation.NoOfRoom,
                            userId,
                            partyGuest
                        );

                        successIds.Add(reservationId);
                    }
                    catch (Exception ex)
                    {
                        errorMessages.Add($"Reservation {reservationId}: {ex.Message}");
                    }
                }

                if (successIds.Count == 0)
                {
                    return Json(new
                    {
                        code = 0,
                        msg = "No reservations were split successfully.",
                        successReservationIds = successIds,
                        errors = errorMessages
                    });
                }

                return Json(new
                {
                    code = 0,
                    msg = errorMessages.Count == 0
                        ? "Split all special was successfully!"
                        : "Split completed with some errors",
                    successReservationIds = successIds,
                    errors = errorMessages
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

        [HttpGet]
        public ActionResult GenSearchMultipleSelect(int confirmationNo)
        {
            try
            {

                DataTable dataTable = _iReservationService.ResConfNoList(confirmationNo);

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
                                          if (col.ColumnName == "Arrival" || col.ColumnName == "Departure" || col.ColumnName == "IsShow" || col.ColumnName == "Inactive")
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

        public ActionResult SplitAllRoomShare()
        {
            try
            {
                string ids = Request.Form["reservationID"].ToString() ?? "";
                string userIdText = Request.Form["userID"].ToString() ?? "";

                if (string.IsNullOrWhiteSpace(ids))
                {
                    return Json(new { code = 1, msg = "Can not find reservation" });
                }

                if (!int.TryParse(userIdText, out int userId))
                {
                    return Json(new { code = 1, msg = "Invalid userID" });
                }

                List<int> idList = ids
                    .Split([','], StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => int.TryParse(x, out _))
                    .Select(int.Parse)
                    .Distinct()
                    .ToList();
                if (idList.Count == 0)
                {
                    return Json(new { code = 1, msg = "Can not find reservation" });
                }

                _iReservationService.SplitAllRoomSharer(idList, userId);

                return Json(new
                {
                    code = 0,
                    msg = "Room sharers processed successfully."
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

        #region DatVP __ Reserrvation: Room Assign
        [HttpPost]
        public ActionResult AssignRoom()
        {
            ProcessTransactions pt = new ProcessTransactions();

            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();
                ReservationModel reservation = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(int.Parse(Request.Form["rsvID"].ToString()));
                if (reservation == null || reservation.ID == 0)
                {
                    return Json(new { code = 0, msg = "Can not find Reservation!" });

                }
                #region update lại room Reservation
                reservation.RoomId = int.Parse(Request.Form["roomID"].ToString());
                reservation.RoomNo = Request.Form["roomNo"].ToString();
                reservation.UpdateBy = Request.Form["userName"].ToString();
                reservation.UserUpdateId = int.Parse(Request.Form["userID"].ToString());
                reservation.UpdateDate = DateTime.Now;
                ReservationBO.Instance.Update(reservation);
                #endregion

                #region update lại room sharer
                List<ReservationModel> reservations = ReservationBO.GetReservationRoomSharer(reservation.ID);
                if (reservations.Count > 0)
                {
                    foreach (var item in reservations)
                    {
                        item.RoomId = int.Parse(Request.Form["roomID"].ToString());
                        item.RoomNo = Request.Form["roomNo"].ToString();
                        item.UpdateBy = Request.Form["userName"].ToString();
                        item.UserUpdateId = int.Parse(Request.Form["userID"].ToString());
                        item.UpdateDate = DateTime.Now;
                        ReservationBO.Instance.Update(item);

                    }
                }
                #endregion
                pt.CommitTransaction();

                return Json(new { code = 0, msg = "Assign Room was successfully!" });

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
        public ActionResult UnAssignRoom()
        {
            ProcessTransactions pt = new ProcessTransactions();

            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();
                ReservationModel reservation = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(int.Parse(Request.Form["rsvID"].ToString()));
                if (reservation == null || reservation.ID == 0)
                {
                    return Json(new { code = 1, msg = "Can not find Reservation!" });

                }
                if (reservation.Status != 0 && reservation.Status != 5)
                {
                    return Json(new { code = 1, msg = "Only Reservation or Due In status can be assigned/unassigned." });
                }
                #region update lại room Reservation
                reservation.RoomId = 0;
                reservation.RoomNo = "";
                reservation.UpdateBy = Request.Form["userName"].ToString();
                reservation.UserUpdateId = int.Parse(Request.Form["userID"].ToString());
                reservation.UpdateDate = DateTime.Now;
                ReservationBO.Instance.Update(reservation);
                #endregion

                #region update lại room sharer
                List<ReservationModel> reservations = ReservationBO.GetReservationRoomSharer(reservation.ID);
                if (reservations.Count > 0)
                {
                    foreach (var item in reservations)
                    {
                        item.RoomId = 0;
                        item.RoomNo = "";
                        item.UpdateBy = Request.Form["userName"].ToString();
                        item.UserUpdateId = int.Parse(Request.Form["userID"].ToString());
                        item.UpdateDate = DateTime.Now;
                        ReservationBO.Instance.Update(item);

                    }
                }
                #endregion
                pt.CommitTransaction();

                return Json(new { code = 0, msg = "Unassign Room was successfully!" });

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

        #region  Tuan __ Reservation: Group Admin
        [HttpGet]
        public async Task<IActionResult> SearchGroupAdmin(string confirmationNo = "", int sorting = 0, string displayStattus = "10", string name = "", string roomNo = "")
        {
            try
            {
                var data = _iGroupAdminService.SearchGroupAdmin2(confirmationNo, sorting, displayStattus, name, roomNo);
                var result = (from d in data.AsEnumerable()
                              select d.Table.Columns.Cast<DataColumn>()
                                  //.Where(col => col.ColumnName != "AllotmentStageID" && col.ColumnName != "flag" && col.ColumnName != "Total")
                                  .ToDictionary(
                                      col => col.ColumnName,
                                       col =>
                                       {
                                           var value = d[col.ColumnName];
                                           if (value == DBNull.Value) return null;

                                           // CreatedDate: KHÔNG ToString
                                           if (col.ColumnName == "CreatedDate" || col.ColumnName == "UpdatedDate"
                                           || col.ColumnName == "Arrival" || col.ColumnName == "Departure")
                                               return value;

                                           // Các field khác: ToString
                                           return value.ToString();
                                       })).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        #endregion

        #region  DatVP __ Reservation: Group check in 
        [HttpGet]
        public async Task<IActionResult> SearchGroupCheckInRoom(string confirmationNo, int type, string name, string roomNo)
        {
            try
            {
                string @Inspected = "", @Clean = "", @AllRooms = "", @CleanAndInspected = "";
                if (confirmationNo == null || string.IsNullOrEmpty(confirmationNo))
                {
                    return Json(0);
                }
                if (type == 1)
                {
                    @Inspected = confirmationNo;
                }
                else if (type == 2)
                {
                    @Clean = confirmationNo;
                }
                else if (type == 3)
                {
                    @AllRooms = confirmationNo;
                }
                else
                {
                    @CleanAndInspected = confirmationNo;
                }

                var data = _iGroupAdminService.SearchGroupCheckInRoom(confirmationNo, @Inspected, @Clean, @AllRooms, @CleanAndInspected);
                // var data = _iGroupAdminService.SearchGroupAdmin(confirmationNo, type.ToString(), name, roomNo);
                var result = (from d in data.AsEnumerable()
                              select d.Table.Columns.Cast<DataColumn>()
                                  //.Where(col => col.ColumnName != "AllotmentStageID" && col.ColumnName != "flag" && col.ColumnName != "Total")
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
        public ActionResult GroupCheckIn([FromBody] List<int> ids)
        {
            ProcessTransactions pt = new ProcessTransactions();

            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();
                for (int i = 0; i < ids.Count; i++)
                {
                    ReservationModel reservation = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(ids[i]);
                    if (reservation == null || reservation.ID == 0)
                    {
                        return Json(new { code = 1, msg = "Can not find reservation" });

                    }
                    reservation.Status = 1;
                    ReservationBO.Instance.Update(reservation);

                    ReservationOptionsModel optionsModel = new ReservationOptionsModel();
                    optionsModel.ReservationID = reservation.ID;
                    optionsModel.Billing = true;
                    ReservationOptionsBO.Instance.Insert(optionsModel);
                }

                pt.CommitTransaction();

                return Json(new { code = 0, msg = "Check In was successfully!" });

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

        #region DatVP __ Reservation: View Profile
        [HttpGet]
        public async Task<IActionResult> ViewProfileByID(int profileID)
        {
            try
            {
                ProfileModel profile = (ProfileModel)ProfileBO.Instance.FindByPrimaryKey(profileID);
                return Json(profile);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        #endregion

        #region DatVP __ Resserrvation: Add On
        #endregion

        #region DatVP __ Reservation: Traces
        [HttpGet]
        public async Task<IActionResult> GetDepartmentTraces()
        {
            try
            {

                List<DepartmentModel> result = PropertyUtils.ConvertToList<DepartmentModel>(DepartmentBO.Instance.FindAll());
                return Json(result);

            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> SearchTrace(string departmentID, string resolved, DateTime date, string name, string reservationID)
        {
            try
            {

                var data = _iReservationService.SearchTrace(departmentID ?? "", resolved ?? "", date, name ?? "", reservationID);

                var result = (from d in data.AsEnumerable()
                              select d.Table.Columns.Cast<DataColumn>()
                                  //.Where(col => col.ColumnName != "AllotmentStageID" && col.ColumnName != "flag" && col.ColumnName != "Total")
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
        public ActionResult SaveTrace()
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();

                if (string.IsNullOrEmpty(Request.Form["department"].ToString()))
                {
                    return Json(new { code = 1, msg = $"Please choose department" });

                }
                if (string.IsNullOrEmpty(Request.Form["traceText"].ToString()))
                {
                    return Json(new { code = 1, msg = $"Trace text cant not be blank" });

                }
                ReservationTracesModel trace = new ReservationTracesModel();
                trace.FromDate = DateTime.Parse(Request.Form["fromDate"].ToString());
                trace.ToDate = DateTime.Parse(Request.Form["toDate"].ToString());
                trace.TracesTime = Request.Form["time"].ToString();
                trace.DepartmentID = int.Parse(Request.Form["department"].ToString());
                trace.Resolved = 0;
                trace.TracesText = Request.Form["traceText"].ToString();
                trace.ProfileID = int.Parse(Request.Form["profileID"].ToString());
                trace.ReservationID = int.Parse(Request.Form["reservationID"].ToString());
                trace.UserInsertID = trace.UserUpdateID = int.Parse(Request.Form["userID"].ToString());
                trace.CreateDate = trace.UpdateDate = DateTime.Now;
                trace.ResolvedBy = "";
                trace.IsDelete = false;
                long id = ReservationTracesBO.Instance.Insert(trace);

                ActivityLogModel activityLog = new ActivityLogModel();
                activityLog.TableName = "Reservation";
                activityLog.UserID = int.Parse(Request.Form["userID"].ToString());
                activityLog.UserName = Request.Form["userName"].ToString();
                activityLog.ChangeDate = DateTime.Now;
                activityLog.Change = "ReservationTraces";
                activityLog.ObjectID = (int)id;
                activityLog.OldValue = "";
                activityLog.NewValue = $"[FromDate]{trace.FromDate},[ToDate]{trace.ToDate},[TraceTime]{trace.TracesTime},[DepartmentID]{trace.DepartmentID},[TraceText]{trace.TracesText},[Resolved]{trace.Resolved}";
                activityLog.Description = "";
                ActivityLogBO.Instance.Insert(activityLog);
                pt.CommitTransaction();
                return Json(new { code = 0, msg = $"Trace was created successfully" });

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
        public ActionResult ResolveTraces(int userID, string userName, List<int> selectedIds)
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();
                if (selectedIds.Count <= 0)
                {
                    return Json(new { code = 1, msg = $"Please choose at least 1 traces" });
                }
                for (int i = 0; i < selectedIds.Count; i++)
                {
                    ReservationTracesModel traces = (ReservationTracesModel)ReservationTracesBO.Instance.FindByPrimaryKey(selectedIds[i]);
                    if (traces.ID == 0 || traces == null)
                    {
                        return Json(new { code = 1, msg = $"Could not trace in trace list" });

                    }
                }
                for (int i = 0; i < selectedIds.Count; i++)
                {
                    ReservationTracesModel traces = (ReservationTracesModel)ReservationTracesBO.Instance.FindByPrimaryKey(selectedIds[i]);
                    if (traces.Resolved == 1)
                    {
                        continue;
                    }
                    traces.Resolved = 1;
                    traces.ResolvedBy = userName;
                    traces.UserUpdateID = userID;
                    traces.UpdateDate = DateTime.Now;
                    ReservationTracesBO.Instance.Update(traces);
                }
                pt.CommitTransaction();
                return Json(new { code = 0, msg = $"Trace was created successfully" });

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

        #region DatVP __ Reservation: Alerts
        [HttpGet]
        public async Task<IActionResult> GetAlertsByReservationID(int reservationID)
        {
            try
            {

                var data = _iReservationService.SearchReservationAlerts(reservationID);

                var result = (from d in data.AsEnumerable()
                              select d.Table.Columns.Cast<DataColumn>()
                                  //.Where(col => col.ColumnName != "AllotmentStageID" && col.ColumnName != "flag" && col.ColumnName != "Total")
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

        [HttpGet]
        public async Task<IActionResult> GetSetUpAlert()
        {
            try
            {

                List<AlertsSetupModel> result = PropertyUtils.ConvertToList<AlertsSetupModel>(AlertsSetupBO.Instance.FindAll());
                return Json(result);

            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        [HttpPost]
        public ActionResult SaveAlert()
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();

                if (string.IsNullOrEmpty(Request.Form["code"].ToString()))
                {
                    return Json(new { code = 1, msg = $"Please chose code alert" });

                }
                ReservationAlertsModel alert = new ReservationAlertsModel();
                alert.ReservationID = int.Parse(Request.Form["reservationID"].ToString());
                alert.Code = Request.Form["code"].ToString();
                alert.Description = Request.Form["description"].ToString();
                alert.Area = Request.Form["area"].ToString();
                alert.UserInsertID = alert.UserUpdateID = int.Parse(Request.Form["userID"].ToString());
                alert.CreateDate = alert.UpdateDate = DateTime.Now;
                alert.WarningDay = int.Parse(Request.Form["warningDay"].ToString());
                if (int.Parse(Request.Form["actice"].ToString()) == 1)
                {
                    alert.IsActive = 1;
                }
                else
                {
                    alert.IsActive = 0;
                }
                long id = ReservationAlertsBO.Instance.Insert(alert);
                ReservationAlertsModel alertUpdate = (ReservationAlertsModel)ReservationAlertsBO.Instance.FindByPrimaryKey((int)id);
                alertUpdate.OriginAlertID = (int)id;
                ReservationAlertsBO.Instance.Update(alertUpdate);

                ActivityLogModel activityLog = new ActivityLogModel();
                activityLog.TableName = "Reservation";
                activityLog.UserID = int.Parse(Request.Form["userID"].ToString());
                activityLog.UserName = Request.Form["userName"].ToString();
                activityLog.ChangeDate = DateTime.Now;
                activityLog.Change = "ReservationAlerts";
                activityLog.ObjectID = (int)id;
                activityLog.OldValue = "";
                activityLog.NewValue = $"[Code]{alert.Code},[Area]{alert.Area},[Description]{alert.Description},";
                activityLog.Description = "";
                ActivityLogBO.Instance.Insert(activityLog);
                pt.CommitTransaction();
                return Json(new { code = 0, msg = $"Alert was created successfully" });
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

        #region DatVP __ Reservation: Changes
        [HttpGet]
        public IActionResult SearchChanges(int objectId)
        {
            try
            {
                SqlParameter[] param =
                [
                    new SqlParameter("@sqlCommand", $@"
                    SELECT 
                        UserName,
                        CONVERT(varchar, ChangeDate, 108) AS [Time],
                        ChangeDate AS [Date],
                        Change,
                        OldValue,
                        NewValue,
                        Description
                    FROM ActivityLog WITH (NOLOCK)
                    WHERE TableName = 'Reservation'
                    AND ObjectID = {objectId}
                    ORDER BY ChangeDate")];

                DataTable data = DataTableHelper.getTableData("spSearchAllForTrans", param);

                var result = (from d in data.AsEnumerable()
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
                return Json(ex.Message);
            }
        }
        #endregion

        #region DatVP __  Wait List
        [HttpGet]
        public async Task<IActionResult> SearchWaitList(string name, string priority, string market, string roomType, string reason, string rateCode, string phone, string date)
        {
            try
            {
                DateTime parsedDate;
                if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out parsedDate))
                {
                    var data = _iReservationService.SearchWaitlist(name, priority, market, roomType, reason, rateCode, phone, parsedDate);
                    var result = (from d in data.AsEnumerable()
                                  select d.Table.Columns.Cast<DataColumn>()
                                      .ToDictionary(
                                          col => col.ColumnName,
                                          col => d[col.ColumnName]?.ToString()
                                      )).ToList();
                    return Json(result);
                }
                else
                {
                    return BadRequest("Invalid date format");
                }
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        [HttpPost]
        public ActionResult SaveWaitList()
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();
                ReservationModel rsv = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(int.Parse(Request.Form["rsvID"].ToString()));
                if (rsv == null || rsv.ID == 0)
                {
                    return Json(new { code = 1, msg = "Could not find Reservation" });

                }
                #region update status của reservation về trạng thái wait list
                rsv.Status = 4;
                rsv.UserInsertId = int.Parse(Request.Form["userID"].ToString());
                rsv.UpdateBy = rsv.SpecialUpdateBy = Request.Form["userName"].ToString();
                rsv.UpdateDate = rsv.SpecialUpdateDate = DateTime.Now;
                ReservationBO.Instance.Update(rsv);
                #endregion

                #region insert vào bảng wait list
                WaitListModel waitList = new WaitListModel();
                waitList.ReservationID = rsv.ID;
                waitList.ReasonID = int.Parse(Request.Form["reasoneID"].ToString());
                waitList.PriorityID = int.Parse(Request.Form["priorityID"].ToString());
                waitList.TelephoneNumber = Request.Form["telephone"].ToString();
                waitList.Description = Request.Form["description"].ToString();
                waitList.UserUpdateID = waitList.UserInsertID = int.Parse(Request.Form["userID"].ToString());
                waitList.CreateDate = waitList.UpdateDate = DateTime.Now;
                WaitListBO.Instance.Insert(waitList);
                #endregion

                #region ghi log activity log
                ActivityLogModel activityLog = new ActivityLogModel();
                activityLog.TableName = "Reservation";
                activityLog.ObjectID = rsv.ID;
                activityLog.UserID = int.Parse(Request.Form["userID"].ToString());
                activityLog.UserName = Request.Form["userName"].ToString();
                activityLog.ChangeDate = DateTime.Now;
                activityLog.Change = "WaitList";
                activityLog.OldValue = "";
                activityLog.NewValue = $"[ReasonID]{waitList.ReasonID}[PriorityID]{waitList.PriorityID}[TelephoneNumber]{waitList.TelephoneNumber}[Description]{waitList.Description}";
                activityLog.Description = "";
                ActivityLogBO.Instance.Insert(activityLog);
                #endregion
                pt.CommitTransaction();
                return Json(new { code = 0, msg = "New wait list was created successfully" });

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
        public async Task<IActionResult> GetWaitListByID(int waitListID)
        {
            try
            {
                WaitListModel waitList = (WaitListModel)WaitListBO.Instance.FindByPrimaryKey(waitListID);
                return Json(waitList);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        [HttpPost]
        public ActionResult WaitListAcceptRes()
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();
                WaitListModel waitList = (WaitListModel)WaitListBO.Instance.FindByPrimaryKey(int.Parse(Request.Form["waitListID"].ToString()));
                if (waitList == null || waitList.ID == 0)
                {
                    return Json(new { code = 1, msg = "Could not find wait list. Please another waitlist or refresh " });

                }
                ReservationModel rsv = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(waitList.ReservationID);
                if (rsv == null || rsv.ID == 0)
                {
                    return Json(new { code = 1, msg = "Could not find reservation in this waitlist " });

                }
                rsv.Status = 0;
                ReservationBO.Instance.Update(rsv);

                WaitListBO.Instance.Delete(waitList.ID);
                pt.CommitTransaction();
                return Json(new { code = 0, msg = "New wait list was created successfully" });

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

        #region TuanDB Renew __ OverBooking: Search
        [HttpGet]
        public IActionResult SearchOverBooking(DateTime date)
        {
            try
            {
                string sqlCommand = $@"
            SELECT
                    [Date],
                    RoomType,
                    Quantity,
                    OverBookLevel AS [NotoSell],
                    CASE 
                        WHEN [Type] = 0 THEN (OverBookLevel - Quantity)
                        ELSE NULL
                    END AS Overbooking,
                    CASE 
                        WHEN [Type] = 0 THEN 'Number'
                        ELSE 'Percentage'
                    END AS [Type],
                    CreateBy,
                    CreateDate,
                    UpdateBy,
                    UpdateDate,
                    ID,
                    RoomTypeID
                FROM OverBooking WITH (NOLOCK)
                WHERE [Date] >= CONVERT(date, '{date:yyyyMMdd}')
                ORDER BY [Date]";

                var data = _iReservationService.SearchOverBooking(sqlCommand);
                var result = (from d in data
                .AsEnumerable()
                              select d.Table.Columns.Cast<DataColumn>().ToDictionary(
                              col => col.ColumnName,
                              col =>
                              {
                                  var value = d[col.ColumnName];
                                  if (value == DBNull.Value) return null;

                                  // CreatedDate: KHÔNG ToString
                                  if (col.ColumnName == "CreateDate" || col.ColumnName == "UpdateDate" || col.ColumnName == "Date")
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

        #region DatVP __ OverBooking: New
        [HttpGet]
        public async Task<IActionResult> GetCNumberOfRoom(int roomTypeID)
        {
            try
            {
                var result = RoomBO.GetNumberOfRoom(roomTypeID);
                return Json(result);

            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        [HttpPost]
        public ActionResult SaveOverBooking()
        {
            ProcessTransactions pt = new ProcessTransactions();

            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();

                List<BusinessDateModel> businessDates = PropertyUtils.ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll());

                // ===== GET FORM DATA =====
                int id = int.TryParse(Request.Form["id"], out var tmpId) ? tmpId : 0;

                var daysJson = Request.Form["days"].ToString();
                if (string.IsNullOrEmpty(daysJson))
                    return Json(new { code = 1, msg = "No days selected" });

                List<string> days = System.Text.Json.JsonSerializer.Deserialize<List<string>>(daysJson) ?? new List<string>();
                if (days == null || days.Count == 0)
                    return Json(new { code = 1, msg = "Please select valid days" });

                var fromDateStr = Request.Form["fromDate"];
                var toDateStr = Request.Form["toDate"];
                var roomTypeStr = Request.Form["roomType"];
                var obLevelStr = Request.Form["obLevel"].ToString();
                var quantityStr = Request.Form["quantity"].ToString();
                var type = Request.Form["type"];
                var userName = Request.Form["userName"].ToString();
                var userIDStr = Request.Form["userID"].ToString();

                if (!DateTime.TryParse(fromDateStr, out DateTime fromDate) ||
                    !DateTime.TryParse(toDateStr, out DateTime toDate))
                    return Json(new { code = 1, msg = "Invalid date format" });

                int quantity = int.Parse(quantityStr);
                int obLevel = string.IsNullOrEmpty(obLevelStr) ? 0 : int.Parse(obLevelStr);
                int userID = int.Parse(userIDStr);

                if (string.IsNullOrWhiteSpace(roomTypeStr) || !int.TryParse(roomTypeStr, out int roomTypeId) || roomTypeId <= 0)
                    return Json(new { code = 1, msg = "Please select a valid room type" });

                RoomTypeModel? roomTypeModel = RoomTypeBO.Instance.FindByPrimaryKey(roomTypeId) as RoomTypeModel;

                if (roomTypeModel == null)
                    return Json(new { code = 1, msg = "Room type not found" });
                // ===== UPDATE (id > 0) =====
                if (id > 0)
                {
                    OverbookingModel? overBooking =
                        OverbookingBO.Instance.FindByPrimaryKey(id) as OverbookingModel;

                    if (overBooking == null)
                        return Json(new { code = 1, msg = "Overbooking not found" });

                    overBooking.RoomTypeID = roomTypeModel?.ID ?? 0;
                    overBooking.RoomType = roomTypeModel?.Code ?? "";
                    overBooking.Quantity = quantity;
                    overBooking.OverbookLevel = quantity + obLevel;
                    overBooking.Type = type == "Number" ? 0 : 1;
                    overBooking.UpdateBy = userName;
                    overBooking.UpdateDate = businessDates![0].BusinessDate;

                    OverbookingBO.Instance.Update(overBooking);

                    ActivityLogBO.Instance.Insert(new ActivityLogModel
                    {
                        TableName = "Overbooking",
                        ObjectID = id,
                        UserID = userID,
                        UserName = userName,
                        ChangeDate = businessDates![0].BusinessDate,
                        Change = "Update",
                        OldValue = "",
                        NewValue = $"Ro.Type: {roomTypeModel?.Code} - Qty: {quantity} - On: {overBooking.Date:dd/MM/yyyy}"
                    });

                    pt.CommitTransaction();
                    return Json(new { code = 0, msg = "Over Booking updated successfully" });
                }

                // ===== INSERT (id == 0) =====
                var dayNames = new List<string> { "sun", "mon", "tue", "wed", "thu", "fri", "sat" };
                DateTime currentDate = fromDate;

                while (currentDate <= toDate)
                {
                    string dayName = dayNames[(int)currentDate.DayOfWeek];

                    if (days.Contains(dayName))
                    {
                        if (OverbookingBO.CheckOverBooking(roomTypeId, currentDate) == 0)
                        {
                            OverbookingModel overBooking = new OverbookingModel
                            {
                                RoomTypeID = roomTypeModel?.ID ?? 0,
                                RoomType = roomTypeModel?.Code ?? "",
                                Quantity = quantity,
                                Date = currentDate,
                                OverbookLevel = quantity + obLevel,
                                Type = type == "Number" ? 0 : 1,
                                CreateBy = userName,
                                UpdateBy = userName,
                                CreateDate = businessDates![0].BusinessDate,
                                UpdateDate = businessDates![0].BusinessDate
                            };

                            OverbookingBO.Instance.Insert(overBooking);
                        }
                    }

                    currentDate = currentDate.AddDays(1);
                }

                ActivityLogBO.Instance.Insert(new ActivityLogModel
                {
                    TableName = "Overbooking",
                    ObjectID = 0,
                    UserID = userID,
                    UserName = userName,
                    ChangeDate = DateTime.Now,
                    Change = "Insert",
                    OldValue = "",
                    NewValue = $"Ro.Type: {roomTypeModel?.Code} - Qty: {quantity} - From: {fromDate:dd/MM/yyyy} - To: {toDate:dd/MM/yyyy}"
                });

                pt.CommitTransaction();
                return Json(new { code = 0, msg = "Over Booking created successfully" });
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

        #region DatVP __ OverBooking: Activity Log
        [HttpGet]
        public async Task<IActionResult> GetActivityLogOverBooking()
        {
            try
            {

                string sqlCommand = $"SELECT UserName,  Convert(varchar,ChangeDate, 108) Time, ChangeDate Date, Change, OldValue, NewValue, Description FROM ActivityLog WITH (NOLOCK)WHERE TableName = 'Overbooking' ORDER BY ChangeDate";
                var data = _iReservationService.ActivityLogOverbooking(sqlCommand);
                var result = (from d in data.AsEnumerable()
                              select d.Table.Columns.Cast<DataColumn>()
                                  .ToDictionary(
                                    col => col.ColumnName,
                                    col =>
                                    {
                                        var value = d[col.ColumnName];
                                        if (value == DBNull.Value) return null;

                                        // CreatedDate: KHÔNG ToString
                                        if (col.ColumnName == "CreateDate" || col.ColumnName == "UpdateDate" || col.ColumnName == "Date")
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

        #region DatVP __ Reservation: CheckOutAll/Zero

        [HttpPost]
        public ActionResult CheckOutAll(List<CheckOutDTO> listItem, int userID, string userName)
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();
                if (listItem.Count > 0)
                {
                    foreach (var item in listItem)
                    {
                        ReservationModel rsv = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(int.Parse(item.id));

                        if (int.Parse(item.reservationStatus) == 1)
                        {
                            item.coStatus = "Not OK";
                            item.message = "Today not equal departure date";
                            continue;
                        }
                        List<FolioModel> folios = PropertyUtils.ConvertToList<FolioModel>(FolioBO.Instance.FindByAttribute("ReservationID", int.Parse(item.id)));
                        if (folios.Count > 0)
                        {
                            foreach (var folio in folios)
                            {
                                if (folio.BalanceVND != 0)
                                {

                                    item.coStatus = "Not OK";
                                    item.message = "Folio Not Balance with currency VND";
                                }
                                continue;

                            }
                        }
                        item.coStatus = "OK";
                        item.message = "Checked Out";

                        rsv.Status = 2;
                        ReservationBO.Instance.Update(rsv);

                        #region thêm log activity log
                        ActivityLogModel activityLog = new ActivityLogModel();
                        activityLog.TableName = "Reservation";
                        activityLog.ObjectID = rsv.ID;
                        activityLog.UserID = userID;
                        activityLog.UserName = userName;
                        activityLog.ChangeDate = DateTime.Now;
                        activityLog.Change = "Status";
                        activityLog.OldValue = "DUE OUT";
                        activityLog.NewValue = "CHECKED OUT";
                        activityLog.Description = "";
                        ActivityLogBO.Instance.Insert(activityLog);
                        #endregion
                    }
                }
                pt.CommitTransaction();
                return Json(new { code = 0, msg = "Check out all was  successfully", data = listItem });
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

        #region DatVP __ Reservation: CheckOutAll

        [HttpPost]
        public ActionResult CheckOut(int userID, string userName, int reservationID)
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();
                ReservationModel rsv = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(reservationID);
                if (rsv == null || rsv.ID == 0)
                {
                    return Json(new { code = 1, msg = " Could not find Reservation" });

                }
                List<FolioModel> folios = PropertyUtils.ConvertToList<FolioModel>(FolioBO.Instance.FindByAttribute("ReservationID", rsv.ID));
                if (folios.Count > 0)
                {
                    for (int i = 0; i < folios.Count; i++)
                    {
                        if (folios[i].BalanceVND < 0 || folios[i].BalanceVND > 5)
                        {
                            return Json(new { code = 1, msg = "Folio not balanced with currency VND" });
                        }


                    }
                }
                rsv.Status = 2;
                ReservationBO.Instance.Update(rsv);

                #region thêm log activity log
                ActivityLogModel activityLog = new ActivityLogModel();
                activityLog.TableName = "Reservation";
                activityLog.ObjectID = rsv.ID;
                activityLog.UserID = userID;
                activityLog.UserName = userName;
                activityLog.ChangeDate = DateTime.Now;
                activityLog.Change = "Status";
                activityLog.OldValue = "DUE OUT";
                activityLog.NewValue = "CHECKED OUT";
                activityLog.Description = "";
                ActivityLogBO.Instance.Insert(activityLog);
                #endregion
                pt.CommitTransaction();
                return Json(new { code = 0, msg = "Check out was successfully" });
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

        #region DatVP __ Group Admin: Room Sharer
        [HttpPost]
        public ActionResult RoomSharer()
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();
                int reservationID = int.Parse(Request.Form["rsvID"].ToString());
                ReservationModel rsv = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(reservationID);
                if (rsv == null || rsv.ID == 0)
                {
                    return Json(new { code = 1, msg = " Could not find Reservation" });

                }
                if (rsv.MainGuest == false)
                {
                    return Json(new { code = 1, msg = " This selected is not main guest reservation" });

                }
                List<BusinessDateModel> businessDateModel = PropertyUtils.ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll());

                ReservationModel rsvRoomSharer = (ReservationModel)rsv.Clone();
                foreach (var prop in typeof(ReservationModel).GetProperties())
                {
                    if (prop.CanWrite && prop.Name != "ID")
                    {
                        prop.SetValue(rsvRoomSharer, prop.GetValue(rsv));
                    }
                }
                long idRoomSharer = ReservationBO.Instance.Insert(rsvRoomSharer);
                ReservationModel rsvUpdate = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(idRoomSharer);
                rsvUpdate.ReservationNo = rsvUpdate.PinCode = rsvUpdate.ID.ToString();
                rsvUpdate.ReservationDate = businessDateModel[0].BusinessDate;
                rsvUpdate.NoOfAdult = rsvUpdate.NoOfChild = rsvUpdate.NoOfChild1 = rsvUpdate.NoOfChild2 = rsvUpdate.NoOfRoom = 0;
                rsvUpdate.DiscountAmount = rsvUpdate.DiscountRate = 0;
                rsvUpdate.PackageId = 0;
                rsvUpdate.Packages = "";
                rsvUpdate.MainGuest = false;
                rsvUpdate.RateCodeId = 0;
                rsvUpdate.Rate = rsvUpdate.RateAfterTax = rsvUpdate.TotalAmount = 0;
                rsvUpdate.RoutingToProfile = "";
                ReservationBO.Instance.Update(rsvUpdate);
                #region thêm log activity log
                ActivityLogModel activityLog = new ActivityLogModel();
                activityLog.TableName = "Reservation";
                activityLog.ObjectID = rsvUpdate.ID;
                activityLog.UserID = int.Parse(Request.Form["userID"].ToString());
                activityLog.UserName = Request.Form["userName"].ToString();
                activityLog.ChangeDate = DateTime.Now;
                activityLog.Change = "Insert";
                activityLog.OldValue = activityLog.NewValue = activityLog.Description = "";
                ActivityLogBO.Instance.Insert(activityLog);

                List<Dictionary<string, object>> reservations = new List<Dictionary<string, object>>();
                if (rsvUpdate.ConfirmationNo != "")
                {
                    DataTable reservationTable = _iGroupAdminService.spReservationSearchByConfirmationNo(rsvUpdate.ConfirmationNo);
                    reservations = reservationTable.AsEnumerable().Select(row =>
                        reservationTable.Columns.Cast<DataColumn>()
                            .ToDictionary(
                                column => column.ColumnName,
                                column => row[column] is DBNull ? null : row[column]
                            )
                    ).ToList();
                }
                #endregion
                pt.CommitTransaction();
                return Json(new { code = 0, msg = "Room Sharer was successfully", reservations = reservations });
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

        #region DatVP __ Group Admin: AutoRoomAssign
        [HttpGet]
        public async Task<IActionResult> SearchRoomAssign(string smoking, string floor, DateTime arrivalDate, DateTime departureDate,
            string confirmationNo, string roomTypeID, string hkStatus)
        {
            try
            {
                string hk = "";
                if (string.IsNullOrEmpty(hkStatus))
                {
                    hk = "";
                }
                else
                {
                    hk = string.Join("','", hkStatus.Split(','));
                }

                DataTable myData = _iReservationService.ReservationAutoRoomAssignment(1, "", "", smoking ?? "", floor ?? "", "", arrivalDate, departureDate,
                    hk, confirmationNo, roomTypeID, "");
                var result = (from d in myData.AsEnumerable()
                              select new
                              {
                                  RoomNo = d["RoomNo"].ToString(),
                                  RoomType = d["RoomType"].ToString(),
                                  HKStatus = d["HKStatus"].ToString(),
                                  FO = d["FO"].ToString(),
                                  HKStatusID = d["HKStatusID"].ToString(),
                                  RoomClass = d["RoomClass"].ToString(),
                                  Floor = d["Floor"].ToString(),
                                  BackToBack = d["back-to-back"].ToString(),
                                  Balcony = d["Balcony"].ToString(),
                                  Connecting = d["Connecting"].ToString(),

                                  Description = d["Description"].ToString(),
                                  RoomID = d["RoomID"].ToString(),
                                  RoomTypeID = d["RoomTypeID"].ToString(),
                                  ID = d["ID"].ToString(),

                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        [HttpPost]
        public ActionResult AssignAutoRoom(List<int> listRsvID, List<string> listRoom)
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();
                string roomSelected = "";
                // Check if listRsvID is empty
                if (listRsvID.Count < 1)
                {
                    return Json(new { code = 1, msg = "Must be at least 1 booking" });
                }
                if (listRoom.Count < 1)
                {
                    return Json(new { code = 1, msg = "Must be at least 1 room" });
                }
                // Check if listRoom has enough elements to match listRsvID
                if (listRoom.Count < listRsvID.Count)
                {
                    return Json(new { code = 1, msg = "Not enough room numbers provided for all bookings" });
                }

                for (int i = 0; i < listRsvID.Count; i++)
                {
                    ReservationModel rsv = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(listRsvID[i]);
                    if (rsv != null)
                    {
                        rsv.RoomNo = listRoom[i];
                        RoomModel room = PropertyUtils.ConvertToList<RoomModel>(RoomBO.Instance.FindByAttribute("RoomNo", listRoom[i])).FirstOrDefault();
                        rsv.RoomId = room.ID;
                        RoomTypeModel roomType = (RoomTypeModel)RoomTypeBO.Instance.FindByPrimaryKey(room.RoomTypeID);
                        rsv.RoomType = roomType.Code;
                        rsv.RoomTypeId = roomType.ID;
                        ReservationBO.Instance.Update(rsv);
                        roomSelected += $"{listRoom[i]} - {roomType.Code} - {rsv.LastName} - Assign successfull \n";
                    }
                    else
                    {
                        // Handle case where reservation is not found
                        pt.RollBack();
                        return Json(new { code = 1, msg = $"Reservation with ID {listRsvID[i]} not found" });
                    }
                }

                pt.CommitTransaction();
                return Json(new { code = 0, msg = "Rooms assigned auto successfully", roomSelected = roomSelected });
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

        #region DatVP __ Rate Code Author
        [HttpGet]
        public async Task<IActionResult> SearchRateCodeAutho(int user, int rateCode)
        {
            try
            {
                List<UserRateCodePermissionModel> result = PropertyUtils.ConvertToList<UserRateCodePermissionModel>(UserRateCodePermissionBO.Instance.FindAll())
                    .Where(x => (user == 0 || x.UserID == user) && (rateCode == 0 || x.RateCodeID == rateCode))
                    .ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult SaveRateCodeAuthor()
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();
                if (int.Parse(Request.Form["userID"].ToString()) == 0)
                {
                    return Json(new { code = 1, msg = "Please choose user" });

                }
                if (int.Parse(Request.Form["rateCodeID"].ToString()) == 0)
                {
                    return Json(new { code = 1, msg = "Please choose rate code" });

                }
                List<UserRateCodePermissionModel> result = PropertyUtils.ConvertToList<UserRateCodePermissionModel>(UserRateCodePermissionBO.Instance.FindAll())
                .Where(x => x.UserID == int.Parse(Request.Form["userID"].ToString()) && x.RateCodeID == int.Parse(Request.Form["rateCodeID"].ToString()))
                .ToList();
                if (result.Count > 0)
                {
                    return Json(new { code = 1, msg = "User - Rate Code Permisson was invalid" });

                }
                UserRateCodePermissionModel model = new UserRateCodePermissionModel();
                model.UserID = int.Parse(Request.Form["userID"].ToString());
                UsersModel user = (UsersModel)UsersBO.Instance.FindByPrimaryKey(model.UserID);
                model.UserName = user.LoginName;
                model.RateCodeID = int.Parse(Request.Form["rateCodeID"].ToString());
                RateCodeModel rateCode = (RateCodeModel)RateCodeBO.Instance.FindByPrimaryKey(model.RateCodeID);
                model.RateCode = rateCode.RateCode;
                model.CreatedBy = model.UpdatedBy = Request.Form["userName"].ToString();
                model.CreatedDate = model.UpdatedDate = DateTime.Now;
                UserRateCodePermissionBO.Instance.Insert(model);
                pt.CommitTransaction();
                return Json(new { code = 0, msg = "User - Rate Code Permisson was created successfully" });

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
        public ActionResult DeleteRateCodeAuthor()
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();


                UserRateCodePermissionModel model = (UserRateCodePermissionModel)UserRateCodePermissionBO.Instance.FindByPrimaryKey(int.Parse(Request.Form["id"].ToString()));
                if (model == null || model.ID == 0)
                {
                    return Json(new { code = 1, msg = "Can't not find User - Rate Code Permisson" });

                }
                UserRateCodePermissionBO.Instance.Delete(model.ID);
                pt.CommitTransaction();
                return Json(new { code = 0, msg = "User - Rate Code Permisson was deleted successfully" });

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

        #region DatVP __ Reservation: Confirmation letter
        [HttpGet]
        public async Task<IActionResult> ReadDoc(int id)
        {
            try
            {
                // Kiểm tra ID hợp lệ
                if (id <= 0)
                {
                    return Json(new { error = "ID không hợp lệ." });
                }

                // Đường dẫn tệp gốc
                string filePath = Path.Combine(_environment.WebRootPath, "templates", "confirmation_letter.docx");

                // Kiểm tra sự tồn tại của tệp gốc
                if (!System.IO.File.Exists(filePath))
                {
                    return Json(new { error = $"Tệp gốc không tồn tại tại: {filePath}" });
                }

                // Lấy thông tin reservation
                ReservationModel reservationModel = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(id);
                if (reservationModel == null)
                {
                    return Json(new { error = "Không tìm thấy reservation với ID đã cung cấp." });
                }
                ProfileModel profile = (ProfileModel)ProfileBO.Instance.FindByPrimaryKey(reservationModel.ProfileIndividualId);
                // Tạo thư mục tạm nếu chưa tồn tại
                string tempFolder = Path.Combine(_environment.WebRootPath, "templates");
                if (!Directory.Exists(tempFolder))
                {
                    Directory.CreateDirectory(tempFolder);
                }

                // Tạo tên tệp tạm duy nhất
                string tempFileName = $"confirmation_letter_{Guid.NewGuid()}.docx";
                string tempFilePath = Path.Combine(tempFolder, tempFileName);

                // Sao chép tệp gốc sang tệp tạm
                try
                {
                    System.IO.File.Copy(filePath, tempFilePath, true);
                }
                catch (IOException ex)
                {
                    return Json(new { error = $"Lỗi khi sao chép tệp: {ex.Message}" });
                }

                // Tạo từ điển các placeholder và giá trị thay thế
                var replacements = new Dictionary<string, string>
                {
                    { "«Export.Company»", profile.Company ?? "" },
                    { "«Export.LastName»", reservationModel.LastName ?? "" },
                    { "«Export.SystemDate»", DateTime.Now.ToString() },
                    { "«Export.HandPhone»", profile.HandPhone ?? "" },
                    { "«Export.Email»", profile.Email ?? "" },

                    { "«Export.ConfirmationNo»", reservationModel.ConfirmationNo ?? "" },
                    { "«Export.RoomType»", reservationModel.RoomType ?? "" },
                    { "«Export.ArrivalDate»", reservationModel.ArrivalDate.ToString("dd/MM/yyyy") ?? "" },
                    { "«Export.DepartureDate»", reservationModel.DepartureDate.ToString("dd/MM/yyyy") ?? "" },
                    { "«Export.RateNet»", reservationModel.RateAfterTax.ToString() ?? "" }

                };

                // Thay thế các placeholder trong tài liệu
                try
                {
                    ReplacePlaceholdersInWordDocument(tempFilePath, replacements);
                }
                catch (Exception ex)
                {
                    return Json(new { error = $"Lỗi khi thay thế văn bản: {ex.Message}" });
                }

                // Đọc tệp đã chỉnh sửa vào luồng
                byte[] fileBytes;
                try
                {
                    fileBytes = System.IO.File.ReadAllBytes(tempFilePath);
                }
                catch (Exception ex)
                {
                    return Json(new { error = $"Lỗi khi đọc tệp tạm: {ex.Message}" });
                }
                finally
                {
                    // Xóa tệp tạm
                    try
                    {
                        if (System.IO.File.Exists(tempFilePath))
                        {
                            System.IO.File.Delete(tempFilePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Lỗi khi xóa tệp tạm: {ex.Message}");
                    }
                }
                // Trả về tệp dưới dạng tải xuống
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", $"confirmation_letter_{id}.docx");
            }
            catch (Exception ex)
            {
                return Json(new { error = $"Lỗi xử lý: {ex.Message}" });
            }
        }

        private void ReplacePlaceholdersInWordDocument(string filePath, Dictionary<string, string> replacements)
        {
            try
            {
                // Mở tài liệu Word
                using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(filePath, true))
                {
                    // Duyệt qua tất cả các phần tử văn bản trong tài liệu
                    foreach (var text in wordDoc.MainDocumentPart.Document.Descendants<Text>())
                    {
                        string currentText = text.Text;
                        bool modified = false;

                        // Thay thế từng placeholder
                        foreach (var replacement in replacements)
                        {
                            if (currentText.Contains(replacement.Key))
                            {
                                currentText = currentText.Replace(replacement.Key, replacement.Value);
                                modified = true;
                            }
                        }

                        // Cập nhật văn bản nếu có thay đổi
                        if (modified)
                        {
                            text.Text = currentText;
                        }
                    }

                    // Lưu thay đổi
                    wordDoc.MainDocumentPart.Document.Save();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi thay thế văn bản trong tài liệu: {ex.Message}");
            }
        }
        #endregion

        #region DatVP __ Reservation: Room Move
        [HttpPost]
        public ActionResult RoomMove()
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();

                ReservationModel rsv = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(int.Parse(Request.Form["rsvID"].ToString()));
                if (rsv == null || rsv.ID == 0)
                {
                    return Json(new { code = 1, msg = " Could not find Reservation" });
                }
                if (string.IsNullOrEmpty(Request.Form["reason"].ToString()) || Request.Form["reason"].ToString() == "0")
                {
                    return Json(new { code = 1, msg = " Please choose reason" });
                }
                if (string.IsNullOrEmpty(Request.Form["moveRoomMove"].ToString()))
                {
                    return Json(new { code = 1, msg = " Please choose new room" });
                }

                RoomModel roomCurrent = (RoomModel)RoomBO.Instance.FindByPrimaryKey(rsv.RoomId);
                if (roomCurrent == null || roomCurrent.ID == 0)
                {
                    return Json(new { code = 1, msg = " Could not find current room" });
                }
                var roomNew = PropertyUtils.ConvertToList<RoomModel>(RoomBO.Instance.FindByAttribute("RoomNo", Request.Form["moveRoomMove"].ToString()));
                if (roomNew.Count < 1)
                {
                    return Json(new { code = 1, msg = " Could not find new room" });
                }
                if (roomCurrent.ID == roomNew[0].ID)
                {
                    return Json(new { code = 1, msg = " Room was choosen to move have not to equal room current" });

                }
                ReasonModel reason = (ReasonModel)ReasonBO.Instance.FindByPrimaryKey(int.Parse(Request.Form["reason"].ToString()));
                if (reason == null || reason.ID == 0)
                {
                    return Json(new { code = 1, msg = " Could not find reason" });
                }
                RoomMoveModel roomMoveModel = new RoomMoveModel();
                roomMoveModel.ReservationID = rsv.ID;
                roomMoveModel.FromRoomID = roomCurrent.ID;
                roomMoveModel.FromStatus = roomCurrent.HKStatusID;
                roomMoveModel.ToRoomID = roomNew[0].ID;
                roomMoveModel.ToStatus = roomNew[0].HKStatusID;
                roomMoveModel.MoveDate = DateTime.Parse(Request.Form["moveDate"].ToString());
                roomMoveModel.Reason = reason.Description;
                roomMoveModel.CreateDate = roomMoveModel.UpdateDate = DateTime.Now;
                roomMoveModel.UserInsertID = roomMoveModel.UserUpdateID = int.Parse(Request.Form["userID"].ToString());
                RoomMoveBO.Instance.Insert(roomMoveModel);


                rsv.RoomId = roomNew[0].ID;
                rsv.RoomNo = roomNew[0].RoomNo;
                ReservationBO.Instance.Update(rsv);
                pt.CommitTransaction();
                return Json(new { code = 0, msg = "Room Move was successfully" });
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

        #region DatVP __ Reservation: Reservation Item Inventory
        [HttpGet]
        public async Task<IActionResult> GetReservationItemByResevationID(int reservationID)
        {
            try
            {
                List<ReservationItemInventoryModel> result = PropertyUtils.ConvertToList<ReservationItemInventoryModel>(ReservationItemInventoryBO.Instance.FindAll())
                    .Where(x => x.ReservationID == reservationID)
                    .ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
        #endregion

        #region  Nam_Packages Tuan_Renew
        [HttpGet]
        public IActionResult GetPackageByReservationID(
            DateTime? beginDate,
            DateTime? endDate,
            int type = 0,
            int reservationID = 0,
            int packageID = 0,
            int rateCodeID = 0)
        {
            try
            {
                if (reservationID <= 0)
                    return Json(new { Success = false, message = "ReservationID is required" });

                var businessDates = PropertyUtils
                    .ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll());

                if (businessDates == null || businessDates.Count == 0)
                    return Json(new { Success = false, message = "Business date not available" });

                var businessDate = businessDates[0].BusinessDate;
                beginDate ??= businessDate;
                endDate ??= businessDate;
                /* ========= PHASE 0 : type = 0 ========= */

                if (type == 0)
                {
                    var phase0 = _iReservationService.SearchReservationPackages(reservationID, packageID, type, beginDate.Value, endDate.Value, rateCodeID);
                    if (phase0 == null || phase0.Rows.Count == 0)
                        return Json(new { Success = true, message = "No packages detail found for this reservation package." });

                    var phaseResult = phase0.AsEnumerable()
                        .Select(r => phase0.Columns.Cast<DataColumn>()
                            .ToDictionary(
                                c => c.ColumnName,
                                c => r[c] == DBNull.Value ? null : r[c]
                            ))
                        .ToList();
                    return Json(new
                    {
                        Success = true,
                        DetailsPackage = phaseResult
                    });

                }

                /* ========= PHASE 1 : type = 1 ========= */
                var phase1 = _iReservationService.SearchReservationPackages(
                    reservationID,
                    packageID,
                    type: 1,
                    beginDate.Value,
                    endDate.Value,
                    rateCodeID
                );

                if (phase1 == null || phase1.Rows.Count == 0)
                    return Json(new { Success = true, message = "No packages found for this reservation." });

                var phase1Result = phase1.AsEnumerable()
                    .Select(r => phase1.Columns.Cast<DataColumn>()
                        .ToDictionary(
                            c => c.ColumnName,
                            c => r[c] == DBNull.Value ? null : r[c]
                        ))
                    .ToList();

                /* ========= PHASE 2 : type = 2 (FLAT) ========= */

                var phase2Results = new List<ReservationPackageSummary>();

                foreach (DataRow row in phase1.Rows)
                {
                    var packageId = row["ID"] != DBNull.Value
                        ? Convert.ToInt32(row["ID"])
                        : 0;

                    if (packageId <= 0)
                        continue;

                    var rowBeginDate = row["Begin Date"] != DBNull.Value
                        ? Convert.ToDateTime(row["Begin Date"])
                        : beginDate.Value;

                    var rowEndDate = row["End Date"] != DBNull.Value
                        ? Convert.ToDateTime(row["End Date"])
                        : endDate.Value;

                    // ✅ service đã mapping model
                    var items = _iReservationService.GetReservationPackagePhase2(
                        reservationID,
                        packageId,
                        rowBeginDate,
                        rowEndDate,
                        rateCodeID
                    );

                    if (items != null && items.Count > 0)
                        phase2Results.AddRange(items);
                }
                return Json(new
                {
                    Success = true,
                    summary = phase1Result,
                    Details = phase2Results
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }



        #endregion

        //#region Nam__Group Admin: CheckinGroup
        //[HttpPost]
        //public ActionResult CheckinGroup()
        //{
        //    ProcessTransactions pt = new ProcessTransactions();
        //    try
        //    {
        //        pt.OpenConnection();
        //        pt.BeginTransaction();
        //        int _RoomID = int.Parse(Request.Form["rsvID"].ToString());
        //        int reservationID = int.Parse(Request.Form["roomID"].ToString());
        //        int shareRoom = int.Parse(Request.Form["shareRoom"].ToString());
        //        int isPseudo = int.Parse(Request.Form["isPseudo"].ToString());
        //        int _Status = int.Parse(Request.Form["reservationStatus"].ToString());
        //        int _NoOfRoom = int.Parse(Request.Form["noOfRoom"].ToString());
        //        int userID = int.Parse(Request.Form["userID"].ToString());
        //        DateTime  arivalDate = DateTime.Parse(Request.Form["arivalDate"].ToString());
        //        string mg = Request.Form["mg"].ToString(); // Lấy giá trị mg từ form
        //        List<BusinessDateModel> businessDateModel = PropertyUtils.ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll());
        //        if (reservationID > 0)
        //        {
        //            bool _MainGuest = false;

        //            if (mg.Equals("X", StringComparison.OrdinalIgnoreCase))
        //                _MainGuest = true;
        //            else
        //                _MainGuest = false;

        //            #region 1.CI MG trước
        //            if (_MainGuest == false && ReservationBO.CheckCheckIn(shareRoom) == 1)
        //            {
        //                return Json(new { code = 1, msg = "Main guest must be check in before Room sharer." });

        //            }
        //            #endregion

        //            #region 2.Kiểm tra thông tin Alert nếu có
        //            ArrayList arrAlert = ReservationBO.GetReservationAlerts(reservationID);
        //            if (arrAlert.Count > 0)
        //            {
        //                for (int i = 0; i < arrAlert.Count; i++)
        //                {
        //                    if (((ReservationAlertsModel)arrAlert[i]).Area == "Check-In")
        //                    {
        //                        //frmReservationAlertDisplay objRAS = new frmReservationAlertDisplay();
        //                        //objRAS.ReservationAlertID = ((ReservationAlertsModel)arrAlert[i]).ID;
        //                        //objRAS.ShowDialog();
        //                    }
        //                }
        //            }
        //            #endregion

        //            #region 3.Kiểm tra nếu ngày đến = BusinessDate thì cho CI - Khách đã CI không cho CI nữa
        //            int a = TextUtils.CompareDate(businessDateModel[0].BusinessDate, arivalDate);
        //            if (a != 0)
        //            {
        //                return Json(new { code = 1, msg = "Arrival Date must be equal Business Date" });
        //            }
        //            //Khách đã CI không ch2o CI nữa
        //            if (_Status == 1 || _Status == 2 || _Status == 3 || _Status == 4 || _Status == 6 || _Status == 7)
        //            {
        //                return Json(new { code = 1, msg = "This room checked in, checked out or cancelled. Not check in" });

        //            }
        //            #endregion

        //            #region 4.Trường hợp chưa có số phòng Ass trước CI sau
        //            if (_MainGuest == true && _RoomID == 0)
        //            {
        //                if (_NoOfRoom == 1)
        //                {
        //                    ReservationModel mOR = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(reservationID);
        //                    //frmRoomAvailableSearch objR = new frmRoomAvailableSearch(true);
        //                    //objR.RoomTypeID = mOR.RoomTypeID;
        //                    //objR.ArrivalDate = mOR.ArrivalDate;
        //                    //objR.DepartureDate = mOR.DepartureDate;
        //                    //objR.RateCodeID = mOR.RateCodeID;
        //                    //objR.ShowDialog();
        //                    if (mOR.RoomId > 0)
        //                    {
        //                        //Assign phòng
        //                        ReservationBO.RoomAssignment(mOR, reservationID, mOR.RoomId, userID, shareRoom == 1);
        //                        //Clear biến nhớ
        //                        mOR = null;
        //                        //Gan gia tri
        //                        _RoomID = mOR.RoomId;
        //                    }
        //                }
        //                else
        //                {
        //                    return Json(new { code = 1, msg = "Split first reservation. Please!" });


        //                }
        //            }
        //            #endregion

        //            #region 5.Trường hợp đã có số phòng
        //            //Chỉ check trạng thái phòng đối với MG
        //            if (_MainGuest == true && _RoomID > 0)
        //            {
        //                //Nếu phòng bẩn không cho CI. Chỉ có phòng sạch mới cho CI
        //                if (ReservationBO.GetFOStatus(_RoomID) == 0 && (ReservationBO.GetHKStatusID(_RoomID) == 4 || isPseudo == 1))
        //                {
        //                    //Hỏi trước khi CI
        //                    if (MessageBox.Show("Are you sure you want to check in this Room?", SQLCommands.Caption_confirm, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        //                    {
        //                        ClassReservation.CreateCheckIn(ReservationID, _RoomID, _NoOfRoom, UserID, 0);

        //                        #region Update lại trạng thái CurrResvStatus trong bảng Room
        //                        if (_RoomID > 0)
        //                            ClassReservation.UpdateReservationStatus(null, _RoomID);
        //                        #endregion

        //                        #region Open, close phone - Bỏ đi
        //                        //if (_MainGuest == true)
        //                        //{
        //                        //    TelephoneSwitchBO.insertToTelephoneSwitch(grvGrid.GetRowCellValue(grvGrid.FocusedRowHandle, "RoomNo").ToString(), grvGrid.GetRowCellValue(grvGrid.FocusedRowHandle, "GuestName").ToString(), true, _SysDate);
        //                        //}
        //                        #endregion

        //                        LoadInfoSearch();
        //                        //Focus con trỏ chuột tại dòng đã select khi load lai dữ liệu
        //                        if (grvGrid.RowCount > 0)
        //                        {
        //                            if (CurrenRowIndex >= grvGrid.RowCount)
        //                                CurrenRowIndex = 0;
        //                            grvGrid.FocusedRowHandle = CurrenRowIndex;
        //                        }
        //                    }
        //                }
        //                // Hiển thị thông báo nếu ko CI được
        //                else
        //                {
        //                    if (ClassReservation.GetFOStatus(_RoomID) == 1)
        //                        MessageBox.Show("Room is Occupied. Not Check In", TextUtils.Caption_Message);
        //                    else if (ClassReservation.GetHKStatusID(_RoomID) == 1)
        //                        MessageBox.Show("Room is Clean Non-checked. Not Check In", TextUtils.Caption_Message);
        //                    else if (ClassReservation.GetHKStatusID(_RoomID) == 2)
        //                        MessageBox.Show("Room is Dirty. Not Check In", TextUtils.Caption_Message);
        //                    else if (ClassReservation.GetHKStatusID(_RoomID) == 3)
        //                        MessageBox.Show("Room is Pickup. Not Check In", TextUtils.Caption_Message);
        //                    else if (ClassReservation.GetHKStatusID(_RoomID) == 5)
        //                        MessageBox.Show("Room is Out Of Order. Not Check In", TextUtils.Caption_Message);
        //                    else if (ClassReservation.GetHKStatusID(_RoomID) == 6)
        //                        //if(Global.HotelID!=2) //FLC OOS vẫn cho checkin
        //                        MessageBox.Show("Room is Out Of Service. Not Check In", TextUtils.Caption_Message);
        //                }
        //            }
        //            //Đối với RS không cần check trạng thái phòng
        //            else if (_MainGuest == false && _RoomID > 0)
        //            {
        //                //Hỏi trước khi CI
        //                if (MessageBox.Show("Are you sure you want to check in this Room?", SQLCommands.Caption_confirm, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        //                {
        //                    ClassReservation.CreateCheckIn(ReservationID, _RoomID, _NoOfRoom, UserID, 0);
        //                    LoadInfoSearch();
        //                    //Focus con trỏ chuột tại dòng đã select khi load lai dữ liệu
        //                    if (grvGrid.RowCount > 0)
        //                    {
        //                        if (CurrenRowIndex >= grvGrid.RowCount)
        //                            CurrenRowIndex = 0;
        //                        grvGrid.FocusedRowHandle = CurrenRowIndex;
        //                    }
        //                }
        //            }
        //            #endregion


        //        }
        //        ReservationModel rsv = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(reservationID);

        //        pt.CommitTransaction();
        //        return Json(new { code = 0, msg = "Room Sharer was successfully" });
        //    }
        //    catch (Exception ex)
        //    {
        //        pt.RollBack();
        //        return Json(new { code = 1, msg = ex.Message });
        //    }
        //    finally
        //    {
        //        pt.CloseConnection();
        //    }
        //}

        //#endregion

        #region Tuan_GetSetUpPackage
        [HttpGet]
        public IActionResult GetSetUpPackage(string searchKey = "")
        {
            try
            {
                DataTable table = _iReservationService.GetSetUpPackage(searchKey);
                var result = (from d in table.AsEnumerable()
                              select d.Table.Columns.Cast<DataColumn>().ToDictionary(
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
                return Json(result);

            }
            catch (Exception ex)
            {

                return Json(ex.Message);
            }
        }
        #endregion

        #region
        public IActionResult RateQuery()
        {
            List<RateCodeModel> listRateCode = PropertyUtils.ConvertToList<RateCodeModel>(RateCodeBO.Instance.FindAll());
            List<RateCategoryModel> listRateCate = PropertyUtils.ConvertToList<RateCategoryModel>(RateCategoryBO.Instance.FindAll());
            List<RateClassModel> listRateClass = PropertyUtils.ConvertToList<RateClassModel>(RateClassBO.Instance.FindAll());
            ViewBag.RateCodeList = listRateCode;
            ViewBag.RateCateList = listRateCate;
            ViewBag.RateClass = listRateClass;
            List<RoomTypeModel> listrt = PropertyUtils.ConvertToList<RoomTypeModel>(RoomTypeBO.Instance.FindAll());
            ViewBag.RoomTypeList = listrt;
            List<PackageModel> listPackage = PropertyUtils.ConvertToList<PackageModel>(PackageBO.Instance.FindAll());
            ViewBag.PackageList = listPackage;
            return View();
        }
        #endregion

        #region showbutton New Reservation
        [HttpGet]
        public IActionResult ReservationOptions(int id)
        {
            try
            {
                var model = ReservationOptionsBO.Instance
                    .FindByAttribute("ReservationID", id)?
                    .Cast<object>()
                    .FirstOrDefault() as ReservationOptionsModel;

                if (model == null)
                {
                    return Json(null);
                }
                return Json(model);
            }
            catch (Exception ex)
            {

                return Json(ex.Message);
            }
        }
        #endregion

    }
}

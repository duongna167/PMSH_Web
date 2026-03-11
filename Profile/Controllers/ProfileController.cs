using BaseBusiness.BO;
using BaseBusiness.Contants;
using BaseBusiness.Model;
using BaseBusiness.util;
using DevExpress.Web.Internal;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Profile.Commons.Helpers;
using Profile.DTO;
using Profile.Services.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.ServiceModel.Channels;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using static BaseBusiness.util.ValidationUtils;
using static DevExpress.XtraPrinting.Native.PageSizeInfo;

namespace Profile.Controllers
{
    public class ProfileController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ProfileController> _logger;
        private readonly IMemoryCache _cache;
        private readonly IProfileExportService _iProfileService;
        private readonly IMembershipService _iMembershipService;
        private readonly IFutureService _iFutureService;
        private readonly IExtendProfileService _iExtendProfile;
        private readonly INewProfileService _newProfile;

        public ProfileController(ILogger<ProfileController> logger,
             IMemoryCache cache, IConfiguration configuration, IProfileExportService iProfileService, IMembershipService iMembershipService, IFutureService iFutureService
             , INewProfileService newProfile, IExtendProfileService iExtendProfile)
        {
            _cache = cache;
            _logger = logger;
            _configuration = configuration;
            _iProfileService = iProfileService;
            _iMembershipService = iMembershipService;
            _iFutureService = iFutureService;
            _newProfile = newProfile;
            _iExtendProfile = iExtendProfile;
        }
        public IActionResult Index()
        {
            return PartialView();
        }

        public IActionResult ProfileIndividual()
        {
            return PartialView("~/Views/Profile/NewProfileModal/ProfileIndividual.cshtml");

        }


        #region common
        /// <summary>
        /// get all city
        /// </summary>
        /// <param ></param>
        /// <returns>list city model</returns>

        [HttpGet]
        public async Task<IActionResult> GetAllCity()
        {
            List<CityModel> list = new List<CityModel>();
            try
            {
                list = PropertyUtils.ConvertToList<CityModel>(CityBO.Instance.FindAll());
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }

            return Json(list);

        }

        // get all language
        [HttpGet]
        public async Task<IActionResult> GetAllLanguage()
        {
            List<LanguageModel> list = new List<LanguageModel>();
            try
            {
                list = PropertyUtils.ConvertToList<LanguageModel>(LanguageBO.Instance.FindAll());
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }

            return Json(list);

        }

        // get all title
        [HttpGet]
        public async Task<IActionResult> GetAllTitle()
        {
            List<TitleModel> list = new List<TitleModel>();
            try
            {
                list = PropertyUtils.ConvertToList<TitleModel>(TitleBO.Instance.FindAll());
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }

            return Json(list);

        }

        // get all nationality
        [HttpGet]
        public async Task<IActionResult> GetAllNationality()
        {
            List<NationalityModel> list = new List<NationalityModel>();
            try
            {
                list = PropertyUtils.ConvertToList<NationalityModel>(NationalityBO.Instance.FindAll());
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }

            return Json(list);

        }

        // get all country
        [HttpGet]
        public async Task<IActionResult> GetAllCountry()
        {
            List<CountryModel> list = new List<CountryModel>();
            try
            {
                list = PropertyUtils.ConvertToList<CountryModel>(CountryBO.Instance.FindAll());
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }

            return Json(list);

        }

        // get all state
        [HttpGet]
        public async Task<IActionResult> GetAllState()
        {
            List<StateModel> list = new List<StateModel>();
            try
            {
                // list = PropertyUtils.ConvertToList<StateModel>(StateBO.Instance.FindAll()).Where(x => x.Inactive == true).ToList();
                list = PropertyUtils.ConvertToList<StateModel>(StateBO.Instance.FindAll()).ToList();
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }

            return Json(list);

        }

        // get all state
        [HttpGet]
        public async Task<IActionResult> GetAllVIP()
        {
            List<VIPModel> list = new List<VIPModel>();
            try
            {
                list = PropertyUtils.ConvertToList<VIPModel>(VIPBO.Instance.FindAll());
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }

            return Json(list);

        }

        // get all owner
        [HttpGet]
        public async Task<IActionResult> GetAllOwner()
        {
            List<OwnerModel> list = new List<OwnerModel>();
            try
            {
                list = PropertyUtils.ConvertToList<OwnerModel>(OwnerBO.Instance.FindAll());
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }

            return Json(list);

        }

        // get all terrtory
        [HttpGet]
        public async Task<IActionResult> GetAllTerritory()
        {
            List<TerritoryModel> list = new List<TerritoryModel>();
            try
            {
                list = PropertyUtils.ConvertToList<TerritoryModel>(TerritoryBO.Instance.FindAll());
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }

            return Json(list);

        }
        [HttpGet]
        public async Task<IActionResult> GetAllCompanyType()
        {
            List<CompanyTypeModel> list = new List<CompanyTypeModel>();
            try
            {
                list = PropertyUtils.ConvertToList<CompanyTypeModel>(CompanyTypeBO.Instance.FindAll());
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }

            return Json(list);

        }
        [HttpGet]
        public async Task<IActionResult> GetAllCurrency()
        {
            List<CurrencyModel> list = new List<CurrencyModel>();
            try
            {
                list = PropertyUtils.ConvertToList<CurrencyModel>(CurrencyBO.Instance.FindAll());
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }

            return Json(list);

        }

        [HttpGet]
        public async Task<IActionResult> GetAllMarket()
        {
            List<MarketModel> list = new List<MarketModel>();
            try
            {
                list = PropertyUtils.ConvertToList<MarketModel>(MarketBO.Instance.FindAll());
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }

            return Json(list);

        }

        [HttpGet]
        public async Task<IActionResult> GetMarketType()
        {
            List<MarketTypeModel> list = new List<MarketTypeModel>();
            try
            {
                list = PropertyUtils.ConvertToList<MarketTypeModel>(MarketTypeBO.Instance.FindAll());
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }

            return Json(list);

        }
        [HttpGet]
        public async Task<IActionResult> GetMarketByFK(int marketTypeID)
        {
            try
            {
                var list = PropertyUtils
                    .ConvertToList<MarketModel>(MarketBO.Instance.FindAll());

                if (marketTypeID != 0)
                {
                    list = list
                        .Where(x => x.MarketTypeID == marketTypeID)
                        .ToList();
                }

                return Json(list);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        //get all membertype
        [HttpGet]
        public async Task<IActionResult> GetAllMemberType()
        {
            List<MemberTypeModel> list = new List<MemberTypeModel>();
            try
            {
                list = PropertyUtils.ConvertToList<MemberTypeModel>(MemberTypeBO.Instance.FindAll()).ToList();
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }

            return Json(list);

        }
        //get all agent
        [HttpGet]
        public async Task<IActionResult> GetAllAgent()
        {
            List<ProfileModel> list = new List<ProfileModel>();
            try
            {
                list = PropertyUtils.ConvertToList<ProfileModel>(ProfileBO.Instance.FindAll()).Where(x => x.Type == 1).ToList();
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }

            return Json(list);

        }

        //get all company
        [HttpGet]
        public async Task<IActionResult> GetAllCompany()
        {
            List<ProfileModel> list = new List<ProfileModel>();
            try
            {
                list = PropertyUtils.ConvertToList<ProfileModel>(ProfileBO.Instance.FindAll()).Where(x => x.Type == 2).ToList();
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }

            return Json(list);

        }

        //get all contact
        [HttpGet]
        public async Task<IActionResult> GetAllContact()
        {
            List<ProfileModel> list = new List<ProfileModel>();
            try
            {
                list = PropertyUtils.ConvertToList<ProfileModel>(ProfileBO.Instance.FindAll()).Where(x => x.Type == 5).ToList();
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }

            return Json(list);

        }

        //get all profile individual
        [HttpGet]
        public async Task<IActionResult> GetAllProfileIndividual()
        {
            List<ProfileModel> list = new List<ProfileModel>();
            try
            {
                list = PropertyUtils.ConvertToList<ProfileModel>(ProfileBO.Instance.FindAll()).Where(x => x.Type == 0).ToList();
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }

            return Json(list);

        }

        //get all roomtype
        [HttpGet]
        public IActionResult GetAllRoomType()
        {
            try
            {
                var list = PropertyUtils
                    .ConvertToList<RoomTypeModel>(RoomTypeBO.Instance.FindAll())
                    .ToList();

                return Json(list);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        //get all room by room type
        [HttpGet]
        public async Task<IActionResult> GetRoomByRoomType(int roomTypeID)
        {
            List<RoomModel> list = new List<RoomModel>();
            try
            {
                list = PropertyUtils.ConvertToList<RoomModel>(RoomBO.Instance.FindAll()).Where(room => roomTypeID == 0 || room.RoomTypeID == roomTypeID).ToList();
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }

            return Json(list);

        }
        #endregion

        #region SearchProfile
        public IActionResult SearchProfile()
        {
            return PartialView();
        }

        [HttpGet]
        public async Task<IActionResult> GetAllProfiles(string code, string account, string firstName, string keyWord, string city, string type, bool showSaleInCharge)
        {
            try
            {
                DataTable myData = ProfileBO.GetAllProfile(code, account, firstName, keyWord, city, type, showSaleInCharge);

                var result = (from d in myData.AsEnumerable()
                              select new
                              {
                                  ID = int.Parse(d["_ProfileID"].ToString()),
                                  Code = !string.IsNullOrEmpty(d["Code"].ToString()) ? d["Code"] : "",
                                  VIP = !string.IsNullOrEmpty(d["VIP"].ToString()) ? d["VIP"] : "",
                                  Account = !string.IsNullOrEmpty(d["Account"].ToString()) ? d["Account"] : "",
                                  PassPort = !string.IsNullOrEmpty(d["PassPort"].ToString()) ? d["PassPort"] : "",
                                  IdentityCard = !string.IsNullOrEmpty(d["IdentityCard"].ToString()) ? d["IdentityCard"] : "",
                                  Address = !string.IsNullOrEmpty(d["Address"].ToString()) ? d["Address"] : "",
                                  City = !string.IsNullOrEmpty(d["City"].ToString()) ? d["City"] : "",
                                  Nationality = !string.IsNullOrEmpty(d["Nationality"].ToString()) ? d["Nationality"] : "",
                                  HandPhone = !string.IsNullOrEmpty(d["HandPhone"].ToString()) ? d["HandPhone"] : "",
                                  Telephone = !string.IsNullOrEmpty(d["Telephone"].ToString()) ? d["Telephone"] : "",
                                  Email = !string.IsNullOrEmpty(d["Email"].ToString()) ? d["Email"] : "",
                                  Keyword = !string.IsNullOrEmpty(d["Keyword"].ToString()) ? d["Keyword"] : "",
                                  PostalCode = !string.IsNullOrEmpty(d["PostalCode"].ToString()) ? d["PostalCode"] : "",
                                  ReturnGuest = !string.IsNullOrEmpty(d["ReturnGuest"].ToString()) ? d["ReturnGuest"] : "",
                                  StayNo = !string.IsNullOrEmpty(d["StayNo"].ToString()) ? d["StayNo"] : "",
                                  Type = !string.IsNullOrEmpty(d["Type"].ToString()) ? d["Type"] : "",
                                  TaxCode = !string.IsNullOrEmpty(d["TaxCode"].ToString()) ? d["TaxCode"] : "",
                                  FullAccount = !string.IsNullOrEmpty(d["FullAccount"].ToString()) ? d["FullAccount"] : "",
                                  HomeAddress = !string.IsNullOrEmpty(d["HomeAddress"].ToString()) ? d["HomeAddress"] : "",
                                  ARNo = !string.IsNullOrEmpty(d["ARNo"].ToString()) ? d["ARNo"] : "",
                                  Website = !string.IsNullOrEmpty(d["Website"].ToString()) ? d["Website"] : "",
                                  DateOfBirth = !string.IsNullOrEmpty(d["DateOfBirth"].ToString()) ? d["DateOfBirth"] : "",
                                  AcctContact = !string.IsNullOrEmpty(d["AcctContact"].ToString()) ? d["AcctContact"] : "",
                                  Description = !string.IsNullOrEmpty(d["Description"].ToString()) ? d["Description"] : "",
                                  AcctIsBlackListContact = !string.IsNullOrEmpty(d["IsBlackList"].ToString()) ? d["IsBlackList"] : "",
                                  PersonInChargeID = !string.IsNullOrEmpty(d["PersonInChargeID"].ToString()) ? d["PersonInChargeID"] : "",

                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllProfileTest(string code, string account, string firstName, string keyWord, string city, string type, bool showSaleInCharge, int skip = 0, int take = 15)
        {
            try
            {
                (DataTable myData, int totalCount) = ProfileBO.GetAllProfileTest(code, account, firstName, keyWord, city, type, showSaleInCharge, skip, take);
                var result = (from d in myData.AsEnumerable()
                              select new
                              {
                                  ID = int.Parse(d["_ProfileID"].ToString()),
                                  Code = !string.IsNullOrEmpty(d["Code"].ToString()) ? d["Code"] : "",
                                  VIP = !string.IsNullOrEmpty(d["VIP"].ToString()) ? d["VIP"] : "",
                                  Account = !string.IsNullOrEmpty(d["Account"].ToString()) ? d["Account"] : "",
                                  PassPort = !string.IsNullOrEmpty(d["PassPort"].ToString()) ? d["PassPort"] : "",
                                  IdentityCard = !string.IsNullOrEmpty(d["IdentityCard"].ToString()) ? d["IdentityCard"] : "",
                                  Address = !string.IsNullOrEmpty(d["Address"].ToString()) ? d["Address"] : "",
                                  City = !string.IsNullOrEmpty(d["City"].ToString()) ? d["City"] : "",
                                  Nationality = !string.IsNullOrEmpty(d["Nationality"].ToString()) ? d["Nationality"] : "",
                                  HandPhone = !string.IsNullOrEmpty(d["HandPhone"].ToString()) ? d["HandPhone"] : "",
                                  Telephone = !string.IsNullOrEmpty(d["Telephone"].ToString()) ? d["Telephone"] : "",
                                  Email = !string.IsNullOrEmpty(d["Email"].ToString()) ? d["Email"] : "",
                                  Keyword = !string.IsNullOrEmpty(d["Keyword"].ToString()) ? d["Keyword"] : "",
                                  PostalCode = !string.IsNullOrEmpty(d["PostalCode"].ToString()) ? d["PostalCode"] : "",
                                  ReturnGuest = !string.IsNullOrEmpty(d["ReturnGuest"].ToString()) ? d["ReturnGuest"] : "",
                                  StayNo = !string.IsNullOrEmpty(d["StayNo"].ToString()) ? d["StayNo"] : "",
                                  Type = !string.IsNullOrEmpty(d["Type"].ToString()) ? d["Type"] : "",
                                  TaxCode = !string.IsNullOrEmpty(d["TaxCode"].ToString()) ? d["TaxCode"] : "",
                                  FullAccount = !string.IsNullOrEmpty(d["FullAccount"].ToString()) ? d["FullAccount"] : "",
                                  HomeAddress = !string.IsNullOrEmpty(d["HomeAddress"].ToString()) ? d["HomeAddress"] : "",
                                  ARNo = !string.IsNullOrEmpty(d["ARNo"].ToString()) ? d["ARNo"] : "",
                                  Website = !string.IsNullOrEmpty(d["Website"].ToString()) ? d["Website"] : "",
                                  DateOfBirth = !string.IsNullOrEmpty(d["DateOfBirth"].ToString()) ? d["DateOfBirth"] : "",
                                  AcctContact = !string.IsNullOrEmpty(d["AcctContact"].ToString()) ? d["AcctContact"] : "",
                                  Description = !string.IsNullOrEmpty(d["Description"].ToString()) ? d["Description"] : "",
                                  AcctIsBlackListContact = !string.IsNullOrEmpty(d["IsBlackList"].ToString()) ? d["IsBlackList"] : "",
                                  PersonInChargeID = !string.IsNullOrEmpty(d["PersonInChargeID"].ToString()) ? d["PersonInChargeID"] : "",

                              }).ToList();
                return Json(new
                {
                    data = result,
                    totalCount = totalCount
                });
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        [HttpGet]
        public ActionResult GetProfileByID(int id)
        {
            try
            {
                ProfileModel list = (ProfileModel)ProfileBO.Instance.FindByPrimaryKey(id);
                if (list != null && list.ID != 0)
                {
                    return Json(new
                    {
                        code = 0,
                        msg = "Success",
                        profile = list
                    });
                }
                else
                {
                    return Json(new
                    {
                        code = 1,
                        msg = "The profile could not be found. "
                    });
                }

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
        public ActionResult EditProfile()
        {
            try
            {
                int id = 0;
                int.TryParse(Request.Form["ID"], out id);

                bool isNew = id <= 0;
                string Message = "";
                ProfileModel profile;

                if (isNew)
                {
                    // INSERT
                    profile = new ProfileModel();
                    profile.CreateDate = DateTime.Now;
                    profile.UserInsertID = 136;
                }
                else
                {
                    // UPDATE
                    profile = (ProfileModel)ProfileBO.Instance.FindByPrimaryKey(id);
                    if (profile == null)
                        return Json(new { code = 1, msg = "Profile not found" });
                }
                if (int.Parse(Request.Form["Type"]) == 0)
                {
                    if (Request.Form["LastNameIndivdual"].ToString() == "")
                    {
                        Message += "Last name not blank\n";
                        return Json(new { code = 1, msg = Message });
                    }

                    if (Request.Form["FirstNameIndividual"].ToString() == "")
                    {
                        Message += "First name not blank\n";
                        return Json(new { code = 1, msg = Message });
                    }
                    if (Request.Form["NationalityIndividual"].ToString() == "" || int.Parse(Request.Form["NationalityIndividual"].ToString()) == 0)
                    {
                        Message += "Nationality not blank\n";
                        return Json(new { code = 1, msg = Message });
                    }
                    if (Request.Form["BlackListIndividual"].ToString().ToLower() == "true")
                    {
                        if (Request.Form["BlackListReasonIndividual"].ToString() == "")
                        {
                            Message += "Reason Black list not blank\n";
                            return Json(new { code = 1, msg = Message });
                        }
                    }

                    string codeIndividual = Request.Form["CodeIndividual"].ToString();

                    if (!string.IsNullOrEmpty(codeIndividual))
                    {
                        List<ProfileModel> tran =
                            PropertyUtils.ConvertToList<ProfileModel>(
                                ProfileBO.Instance.FindByAttribute("Code", codeIndividual)
                            );

                        if (tran != null && tran.Count > 0)
                        {
                            Message += "Profile code existing in system!\n";
                            return Json(new { code = 1, msg = Message });
                        }
                    }


                    if (Request.Form["CodeIndividual"].ToString() != "")
                    {
                        if (Request.Form["CodeIndividual"].ToString().Trim().Length > 13)
                        {
                            Message += "Length Code format!\n";
                            return Json(new { code = 1, msg = Message });
                        }
                    }
                    profile.Type = int.Parse(Request.Form["Type"]);
                    profile.Code = Request.Form["CodeIndividual"].ToString();
                    profile.Account = Request.Form["AccountIndividual"].ToString();
                    profile.FullAccount = Request.Form["FullAccount"].ToString(); ;
                    profile.LastName = Request.Form["LastNameIndivdual"].ToString();
                    profile.Firstname = Request.Form["FirstNameIndividual"].ToString();
                    profile.MiddleName = Request.Form["MiddleNameIndividual"].ToString();
                    profile.LanguageID = int.Parse(Request.Form["LanguageIndividual"].ToString());
                    profile.TitleID = int.Parse(Request.Form["TitleIndividual"].ToString());
                    profile.Address = Request.Form["AddressIndividual"].ToString();
                    //   profile.HomeAddress = "";
                    profile.City = Request.Form["CityIndividual"].ToString();
                    profile.HomeAddress = Request.Form["BusAddressIndividual"].ToString();
                    profile.PostalCode = Request.Form["PostalIndividual"].ToString();
                    profile.CountryID = int.Parse(Request.Form["CountryIndividual"].ToString());
                    profile.StateID = int.Parse(Request.Form["StateIndividual"].ToString());
                    profile.Salutation = Request.Form["SalutationIndividual"].ToString();
                    profile.VIPID = !string.IsNullOrEmpty(Request.Form["VIPIndividual"].ToString()) ? int.Parse(Request.Form["VIPIndividual"].ToString()) : 0;
                    profile.VIPReason = Request.Form["ReasonIndividual"].ToString();
                    profile.PrefRoom = Request.Form["PrefRoomIndividual"].ToString();
                    profile.PassPort = Request.Form["PassportIndividual"].ToString();
                    profile.Keyword = Request.Form["KeywordIndividual"].ToString();
                    profile.DateOfBirth = !string.IsNullOrEmpty(Request.Form["DobIndividual"].ToString()) ? DateTime.Parse(Request.Form["DobIndividual"].ToString()) : DateTime.MinValue;
                    profile.NationalityID = int.Parse(Request.Form["NationalityIndividual"].ToString());
                    profile.Description = Request.Form["NoteIndividual"].ToString();
                    profile.Telephone = Request.Form["TelephoneIndividual"].ToString();
                    profile.Fax = Request.Form["FaxIndividual"].ToString();
                    profile.Email = Request.Form["EmailIndividual"].ToString();
                    profile.Website = Request.Form["WebsiteIndividual"].ToString();
                    profile.HandPhone = Request.Form["HandPhoneIndividual"].ToString();
                    profile.MailList = false;
                    profile.Active = Request.Form["ActiveIndividual"].ToString().ToLower() == "true";
                    profile.Contact = Request.Form["ContactIndividual"].ToString().ToLower() == "true";
                    profile.History = false;
                    profile.ContactProfileID = 0;
                    profile.ARNo = "";
                    profile.Position = "";
                    profile.Department = "";
                    profile.EnvelopGreeting = "";
                    profile.OwnerID = 0;
                    profile.TerritoryID = 0;
                    profile.PersonInChargeID = 0;
                    profile.AcctContact = "";
                    profile.CurrencyID = "";
                    profile.TaxCode = Request.Form["TaxIndividual"].ToString();
                    profile.Type = 0;
                    profile.IdentityCard = Request.Form["CardIndividual"].ToString();
                    profile.MemberType = "";
                    profile.MemberNo = "";
                    profile.LastRoom = "";
                    profile.Lastvisit = DateTime.Now;
                    profile.LastRate = "";
                    profile.LastRateCode = "";
                    profile.LastARNo = "";
                    profile.LastMemberNo = "";
                    profile.ReturnGuest = -1;
                    profile.IsBlackList = Request.Form["BlackListIndividual"].ToString().ToLower() == "true";
                    profile.BlackListReason = Request.Form["BlackListReasonIndividual"].ToString();
                    profile.SpecialUpdateBy = "";
                    profile.SpecialUpdateDate = DateTime.Now;
                    profile.UserInsertID = profile.UserUpdateID = 136;
                    profile.CreateDate = profile.UpdateDate = DateTime.Now;
                    profile.StayNo = 0;
                    profile.GuestNo = "";
                    profile.Occupation = "";
                    profile.BonusPoints = 0;
                    profile.GuestGroupID = 0;
                    profile.Birthplace = "";
                    profile.ExpressCheckout = false;
                    profile.PayTV = false;
                    profile.FirstReservation = profile.LastReservation = profile.WeddingAnniversary = profile.Firstvisit = profile.Expiry = profile.LastContact = DateTime.MinValue;
                    profile.CreditCard = "";
                    profile.RateCode = "";
                    profile.RoomNights = 0;
                    profile.BedNights = 0;
                    profile.TotalTurnover = 0;
                    profile.LodgePackageTurover = 0;
                    profile.LodgeTurnover = 0;
                    profile.FBTurnover = 0;
                    profile.EventTurnover = 0;
                    profile.OtherTurnover = 0;
                    profile.Company = Request.Form["Company2Individual"].ToString();
                    profile.BusinessTitle = Request.Form["BusinessTitleIndividual"].ToString();
                    profile.Other = Request.Form["OtherIndividual"].ToString();
                    profile.Religion = Request.Form["ReligionIndividual"].ToString();
                    profile.Nation = Request.Form["NationIndividual"].ToString(); ;
                    profile.PurposeOfStay = Request.Form["PurposeIndividual"].ToString();
                    profile.MarketID = 0;
                    profile.IsTransfer = false;
                }
                else if (int.Parse(Request.Form["Type"]) == 1 || int.Parse(Request.Form["Type"]) == 2 || int.Parse(Request.Form["Type"]) == 3)
                {
                    if (Request.Form["CodeCOM"].ToString() == "")
                    {
                        Message += "Code not blank\n";
                        return Json(new { code = 1, msg = Message });
                    }
                    if (Request.Form["AccountCOM"].ToString() == "")
                    {
                        Message += "Account not blank\n";
                        return Json(new { code = 1, msg = Message });
                    }
                    if (Request.Form["BlackListCOMchecked"].ToString().ToLower() == "true")
                    {
                        if (Request.Form["BlackListCOM"].ToString() == "")
                        {
                            Message += "Reason Black list not blank\n";
                            return Json(new { code = 1, msg = Message });
                        }
                    }



                    if (Request.Form["CodeCOM"].ToString() != "")
                    {
                        if (Request.Form["CodeCOM"].ToString().Length > 13)
                        {
                            Message += "Length Code format!\n";
                            return Json(new { code = 1, msg = Message });
                        }
                    }

                    string codeIndividual = Request.Form["CodeCOM"].ToString();

                    if (!string.IsNullOrEmpty(codeIndividual))
                    {
                        List<ProfileModel> tran =
                            PropertyUtils.ConvertToList<ProfileModel>(
                                ProfileBO.Instance.FindByAttribute("Code", codeIndividual)
                            );

                        if (tran != null && tran.Count > 0)
                        {
                            Message += "Profile code existing in system!\n";
                            return Json(new { code = 1, msg = Message });
                        }
                    }
                    profile.Type = int.Parse(Request.Form["Type"]);

                    profile.Code = Request.Form["CodeCOM"].ToString();
                    profile.Account = Request.Form["AccountCOM"].ToString();
                    profile.FullAccount = Request.Form["FullAccount"].ToString();

                    profile.CountryID = !string.IsNullOrEmpty(Request.Form["CountryCOM"])
                        ? int.Parse(Request.Form["CountryCOM"])
                        : 0;

                    profile.City = Request.Form["CityCOM"].ToString();
                    profile.PostalCode = Request.Form["PostalCOM"].ToString();

                    profile.StateID = !string.IsNullOrEmpty(Request.Form["StateCOM"])
                        ? int.Parse(Request.Form["StateCOM"])
                        : 0;

                    profile.Address = Request.Form["AddressCOM"].ToString();
                    profile.HomeAddress = Request.Form["BusAddressCOM"].ToString();

                    profile.Active = Request.Form["ActiveCOM"].ToString().ToLower() == "true";
                    profile.Keyword = Request.Form["KeywordCOM"].ToString();

                    profile.TaxCode = Request.Form["TaxCOM"].ToString();
                    profile.Telephone = Request.Form["TelephoneCOM"].ToString();
                    profile.HandPhone = Request.Form["HandPhoneCOM"].ToString();
                    profile.Email = Request.Form["EmailCOM"].ToString();
                    profile.Website = Request.Form["WebsiteCOM"].ToString();

                    profile.Company = Request.Form["Company2COM"].ToString();
                    profile.BusinessTitle = Request.Form["BusTitleCOM"].ToString();
                    profile.Description = Request.Form["NoteCOM"].ToString();

                    profile.IsBlackList = Request.Form["BlackListCOMchecked"].ToString().ToLower() == "true";
                    profile.BlackListReason = Request.Form["BlackListCOM"].ToString();

                    profile.OwnerID = !string.IsNullOrEmpty(Request.Form["OwnerCOM"])
                        ? int.Parse(Request.Form["OwnerCOM"])
                        : 0;

                    profile.TerritoryID = !string.IsNullOrEmpty(Request.Form["TerritoryCOM"])
                        ? int.Parse(Request.Form["TerritoryCOM"])
                        : 0;

                    profile.MemberType = Request.Form["CompanyTypeCOM"].ToString();
                    profile.ARNo = Request.Form["ARCOM"].ToString();
                    profile.CurrencyID = Request.Form["CurrencyCOM"].ToString();
                    profile.AcctContact = Request.Form["ContractName"].ToString();

                    profile.PersonInChargeID = !string.IsNullOrEmpty(Request.Form["SaleInChargeCOM"])
                        ? int.Parse(Request.Form["SaleInChargeCOM"])
                        : 0;

                    profile.MarketID = !string.IsNullOrEmpty(Request.Form["MarketCOM"])
                        ? int.Parse(Request.Form["MarketCOM"])
                        : 0;

                }
                else if (int.Parse(Request.Form["Type"]) == 4)
                {
                    if (Request.Form["CodeGroup"].ToString() == "")
                    {
                        Message += "Code not blank\n";
                        return Json(new { code = 1, msg = Message });
                    }

                    if (Request.Form["CodeGroup"].ToString() != "")
                    {
                        if (Request.Form["CodeGroup"].ToString().Length > 13)
                        {
                            Message += "Length Code format!\n";
                            return Json(new { code = 1, msg = Message });
                        }
                    }

                    string codeIndividual = Request.Form["CodeGroup"].ToString();

                    if (!string.IsNullOrEmpty(codeIndividual))
                    {
                        List<ProfileModel> tran =
                            PropertyUtils.ConvertToList<ProfileModel>(
                                ProfileBO.Instance.FindByAttribute("Code", codeIndividual)
                            );

                        if (tran != null && tran.Count > 0)
                        {
                            Message += "Profile code existing in system!\n";
                            return Json(new { code = 1, msg = Message });
                        }
                    }
                    if (Request.Form["GroupName"].ToString() == "")
                    {
                        Message += "Group Name not blank\n";
                        return Json(new { code = 1, msg = Message });
                    }

                    profile.Type = int.Parse(Request.Form["Type"]);

                    profile.Code = Request.Form["CodeGroup"].ToString();
                    profile.Account = Request.Form["GroupName"].ToString();
                    profile.LanguageID = int.Parse(Request.Form["LanguageGroup"]); // dễ crash

                    profile.CountryID = !string.IsNullOrEmpty(Request.Form["CountryGroup"])
                        ? int.Parse(Request.Form["CountryGroup"])
                        : 0;

                    profile.City = Request.Form["CityGroup"].ToString();
                    profile.PostalCode = Request.Form["PostalGroup"].ToString();

                    profile.StateID = !string.IsNullOrEmpty(Request.Form["StateGroup"])
                        ? int.Parse(Request.Form["StateGroup"])
                        : 0;

                    profile.Address = Request.Form["AddressGroup"].ToString();
                    profile.HomeAddress = Request.Form["HomeAddressGroup"].ToString();

                    profile.History = Request.Form["HistoryGroup"].ToString().ToLower() == "true";

                    profile.VIPID = int.Parse(Request.Form["VipGroup"]);
                    profile.VIPReason = Request.Form["VipReasonGroup"].ToString();
                    profile.HandPhone = Request.Form["HandPhoneGroup"].ToString();
                    profile.Email = Request.Form["EmailGroup"].ToString();
                    profile.Website = Request.Form["WebsiteGroup"].ToString();
                    profile.Fax = Request.Form["Fax"].ToString();
                    profile.Description = Request.Form["NotesGroup"].ToString();

                    profile.AcctContact = Request.Form["AcctContact"].ToString();
                    profile.CurrencyID = Request.Form["CurrGroup"].ToString();
                    profile.DateOfBirth = DateTime.Now;
                    profile.SpecialUpdateDate = Convert.ToDateTime("01/01/1900");
                    profile.Lastvisit = Convert.ToDateTime("01/01/1900");
                    profile.FirstReservation = Convert.ToDateTime("01/01/1900");
                    profile.LastReservation = Convert.ToDateTime("01/01/1900");
                    profile.WeddingAnniversary = Convert.ToDateTime("01/01/1900");
                    profile.Firstvisit = Convert.ToDateTime("01/01/1900");
                    profile.Expiry = Convert.ToDateTime("01/01/1900");
                    profile.LastContact = Convert.ToDateTime("01/01/1900");
                }
                if (isNew)
                    ProfileBO.Instance.Insert(profile);
                else
                    ProfileBO.Instance.Update(profile);
                return Json(new
                {
                    code = 0,
                    msg = isNew ? "Insert successfully" : "Update successfully"
                });
            }
            catch (Exception ex)
            {
                return Json(new { code = 1, msg = ex.Message });
            }

        }

        [HttpGet]
        public async Task<IActionResult> GetAllProfiles2(string code, string account, string firstName, string keyWord, string city, int type, bool showSaleInCharge, int page = 1, int pageSize = 15)
        {
            try
            {
                (DataTable data, int totalCount) = ProfileBO.GetAllProfile2(code, account, firstName, keyWord, city, type, showSaleInCharge, page, pageSize);
                var result = (from d in data.AsEnumerable()
                              select new
                              {
                                  ID = int.Parse(d["_ProfileID"].ToString()),
                                  Code = d["Code"]?.ToString() ?? "",
                                  VIP = d["VIP"]?.ToString() ?? "",
                                  Account = d["Account"]?.ToString() ?? "",
                                  PassPort = d["PassPort"]?.ToString() ?? "",
                                  IdentityCard = d["IdentityCard"]?.ToString() ?? "",
                                  Address = d["Address"]?.ToString() ?? "",
                                  City = d["City"]?.ToString() ?? "",
                                  Nationality = d["Nationality"]?.ToString() ?? "",
                                  HandPhone = d["HandPhone"]?.ToString() ?? "",
                                  Telephone = d["Telephone"]?.ToString() ?? "",
                                  Email = d["Email"]?.ToString() ?? "",
                                  Keyword = d["Keyword"]?.ToString() ?? "",
                                  PostalCode = d["PostalCode"]?.ToString() ?? "",
                                  ReturnGuest = d["ReturnGuest"]?.ToString() ?? "",
                                  StayNo = d["StayNo"]?.ToString() ?? "",
                                  Type = d["Type"]?.ToString() ?? "",
                                  TaxCode = d["TaxCode"]?.ToString() ?? "",
                                  FullAccount = d["FullAccount"]?.ToString() ?? "",
                                  HomeAddress = d["HomeAddress"]?.ToString() ?? "",
                                  ARNo = d["ARNo"]?.ToString() ?? "",
                                  Website = d["Website"]?.ToString() ?? "",
                                  DateOfBirth = d["DateOfBirth"]?.ToString() ?? "",
                                  AcctContact = d["AcctContact"]?.ToString() ?? "",
                                  Description = d["Description"]?.ToString() ?? "",
                                  AcctIsBlackListContact = d["IsBlackList"]?.ToString() ?? "",
                                  PersonInChargeID = d["PersonInChargeID"]?.ToString() ?? ""
                              }).ToList();
                return Json(new { data = result, totalCount = totalCount });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        #endregion

        #region Tuan_ profile
        [HttpPost]
        public ActionResult SaveProfileOld(SaveProfileRequestDto dto)
        {
            if (dto == null)
            {
                return Json(new ApiResponse
                {
                    Success = false,
                    Message = "Invalid request"
                });
            }

            var result = _newProfile.CreateProfile(dto);
            return Json(result);
        }
        [HttpPost]
        public IActionResult DeleteProfile(int profileID)
        {
            var result = _iExtendProfile.DeleteProfile(profileID);

            if (!result.Success)
            {
                return Json(new { result.Success, message = result.Message });
            }

            return Json(new { result.Success, message = result.Message });
        }

        #endregion

        #region new profile in reservation
        //[ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult SaveProfileInReservation()
        {
            try
            {
                ProfileModel profile = new ProfileModel();

                profile.Account = Request.Form["AccountIndividual"].ToString();
                profile.FullAccount = "";
                profile.LastName = Request.Form["LastNameIndivdual"].ToString();
                profile.Firstname = Request.Form["FirstNameIndividual"].ToString();
                profile.MiddleName = Request.Form["MiddleNameIndividual"].ToString();
                profile.Type = 0;
                profile.UserInsertID = profile.UserUpdateID = int.Parse(Request.Form["userID"].ToString());
                profile.CreateDate = profile.UpdateDate = DateTime.Now;
                long id = ProfileBO.Instance.Insert(profile);
                return Json(new { code = 0, msg = "New profile created successfully", id = id });

            }
            catch (Exception ex)
            {
                return Json(new { code = 1, msg = ex.Message });
            }

        }
        #endregion

        #region DatVP __ Profile Export
        public IActionResult ProfileExport()
        {
            return PartialView();
        }
        public IActionResult ExportProfile()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> SearchProfileExport(DateTime fromDate, DateTime toDate)
        {
            try
            {
                var data = _iProfileService.SearchProfileExport(fromDate, toDate);

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
                return StatusCode(500, ex.Message);
            }
        }
        [HttpGet]
        public IActionResult ExportXML(DateTime fromDate, DateTime toDate)
        {
            DataTable myData = _iProfileService.ExportXML(fromDate, toDate);

            if (myData == null || myData.Rows.Count == 0)
                return BadRequest("No data to export");

            DataSet ds = new DataSet("KHAI_BAO_TAM_TRU");
            ds.Tables.Add(myData.Copy());

            var ms = new MemoryStream();

            XmlWriterSettings settings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                Indent = true,
                CloseOutput = false   // 🔥 CỰC KỲ QUAN TRỌNG
            };

            using (XmlWriter writer = XmlWriter.Create(ms, settings))
            {
                ds.WriteXml(writer);
            }

            ms.Position = 0;

            string fileName = $"ThongTinKhach_{DateTime.Now.Ticks}.xml";

            return File(ms, "application/xml", fileName);
        }


        #endregion

        #region DatVP __ Profile History
        [HttpGet]
        public async Task<IActionResult> SearchProfileHistory(int profileID)
        {
            try
            {
                var data = _iProfileService.SearchProfileHistory(profileID, 0, "");

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
                return StatusCode(500, ex.Message);
            }
        }
        #endregion

        #region DatVP __ Profile Membership
        public IActionResult Membership()
        {
            return PartialView("~/Views/Profile/Options/Membership.cshtml");

        }

        [HttpGet]
        public async Task<IActionResult> GetMemberType()
        {
            List<MemberTypeModel> list = new List<MemberTypeModel>();
            try
            {
                list = PropertyUtils.ConvertToList<MemberTypeModel>(MemberTypeBO.Instance.FindAll());
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }

            return Json(list);

        }

        [HttpGet]
        public async Task<IActionResult> SearchProfileMembership(int profileID, string inactive)
        {
            try
            {
                var data = _iMembershipService.SearchProfileMembership(profileID, inactive ?? "", "");

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
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMemberTypeByID(int id)
        {
            try
            {
                MemberTypeModel memberType = (MemberTypeModel)MemberTypeBO.Instance.FindByPrimaryKey(id);
                return Json(memberType);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetMembershipByID(int id)
        {
            try
            {
                ProfileMemberCardModel memberShip = (ProfileMemberCardModel)ProfileMemberCardBO.Instance.FindByPrimaryKey(id);
                return Json(memberShip);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        [HttpGet]
        public async Task<IActionResult> DeleteNewMembership(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id)) return null;
                ProfileMemberCardBO.Instance.Delete(int.Parse(id));
                return Json(new { code = 0, msg = "ProfileMemberCard deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost]
        public ActionResult SaveMembership()
        {
            try
            {
                int profileID = int.Parse(Request.Form["profileID"].ToString());
                if (profileID == 0 || string.IsNullOrEmpty(Request.Form["profileID"].ToString()))
                {
                    return Json(new { code = 1, msg = "Profile ID is required" });
                }
                if (string.IsNullOrEmpty(Request.Form["memberTypeID"].ToString()) || int.Parse(Request.Form["memberTypeID"].ToString()) == 0)
                {
                    return Json(new { code = 1, msg = "Member Type ID is required" });
                }
                if (string.IsNullOrEmpty(Request.Form["memberNo"].ToString()))
                {
                    return Json(new { code = 1, msg = "Card No  is required" });
                }
                ProfileMemberCardModel membership = new ProfileMemberCardModel();
                membership.ProfileID = profileID;
                membership.MemberNo = Request.Form["memberNo"].ToString();
                membership.MemberTypeID = int.Parse(Request.Form["memberTypeID"].ToString());
                membership.Description = Request.Form["description"].ToString();
                membership.Expiry = DateTime.Parse(Request.Form["expiry"].ToString());
                membership.Status = 1;
                membership.InActive = int.Parse(Request.Form["inactive"].ToString()) == 1 ? true : false;
                membership.IsDeleted = false;
                membership.Sequence = !string.IsNullOrEmpty(Request.Form["sequence"].ToString()) ? int.Parse(Request.Form["sequence"].ToString()) : 0;
                membership.CreatedDate = membership.UpdatedDate = DateTime.Now;
                membership.CreatedBy = membership.UpdatedBy = Request.Form["userName"].ToString();
                ProfileMemberCardBO.Instance.Insert(membership);
                return Json(new { code = 0, msg = "Membership was created successfully" });

            }
            catch (Exception ex)
            {
                return Json(new { code = 1, msg = ex.Message });
            }

        }
        #endregion

        #region Nam __ Profile changes

        public IActionResult Changes()
        {
            return PartialView("~/Views/Profile/Options/Changes.cshtml");

        }
        [HttpGet]
        public async Task<IActionResult> SearchProfileChanges(int profileID)
        {
            try
            {

                string sql = $"SELECT UserName,  Convert(varchar,ChangeDate, 108) Time, ChangeDate Date, Change, OldValue,NewValue, Description FROM ActivityLog WITH (NOLOCK)WHERE TableName = 'Profile' AND ObjectID = '{profileID}' ORDER BY ChangeDate";

                DataTable dataTable = TextUtils.Select(sql);

                var result = (from d in dataTable.AsEnumerable()
                              select d.Table.Columns.Cast<DataColumn>()
                                  .ToDictionary(
                                      col => col.ColumnName,
                                      col => d[col.ColumnName]?.ToString()
                                  )).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }


        #endregion

        #region Nam __ Profile future
        public IActionResult Future()
        {
            return PartialView("~/Views/Profile/Options/Future.cshtml");

        }
        [HttpGet]
        public async Task<IActionResult> SearchProfileFuture(int profileID, int profileType)
        {
            try
            {
                var data = _iFutureService.SearchProfileFuture(profileID, profileType);

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
                return StatusCode(500, ex.Message);
            }
        }
        [HttpPost]
        public JsonResult FullNameChange(string fullName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fullName))
                {
                    return Json(new { success = false });
                }

                string[] Name = SplitStringStandard(fullName, 0);

                if (Name == null || Name.Length < 5 || Name[0] == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Validation failed",
                        errors = new[]
                                            {
                            new { field = "fullNameIndividual", message = "Typing format is not correct." },
                            new { field = "fullNameContact", message = "Typing format is not correct." }
                        }
                    });
                }

                // Giữ nguyên logic cũ
                string fullNamedone = Name[3]?.ToString().Trim();
                string firstName = Name[0]?.ToString();

                string lastName = "";
                string middlenamedone = "";

                if (Name[1] != null)
                    lastName = Name[1].ToString();

                if (Name[2] != null)
                    middlenamedone = Name[2].ToString();

                string titleCode = "";
                int? titleID = null;

                if (Name[4] != null && Name[4] != "0")
                {
                    var title = (TitleModel)TitleBO.Instance
                        .FindByPrimaryKey(Convert.ToInt32(Name[4].ToString()));

                    if (title != null)
                    {
                        titleCode = title.Code;
                        titleID = title.ID;
                    }
                }

                string salutation = "Dear " + firstName;

                return Json(new
                {
                    success = true,
                    FullName = fullNamedone,
                    FirstName = firstName,
                    MiddleName = middlenamedone,
                    LastName = lastName,
                    TitleCode = titleCode,
                    TitleID = titleID,
                    Salutation = salutation
                });
            }
            catch (Exception ex)
            {
                // Có thể log ex nếu cần
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        public static string[] SplitStringStandard(string Name, int Type)
        {
            //0:First; 1:Last; 2:Midle; 3:Name; 4:TitleID
            string[] Result = new string[5];
            string[] Arr;
            //Tách chuỗi
            Arr = Name.Split(',');
            //Nếu nhập không đúng quy định
            if (Arr.Length == 1)
                return Result;
            else if (Arr.Length > 1)
            {
                //Nhặt ra LastName, MidleName
                Arr[0] = Arr[0].Trim();
                string[] LastName = Arr[0].Split(' ');
                if (LastName.Length > 0)
                {
                    //Nhặt ra LastName
                    Result[1] = LastName[0].Trim();
                    //Convert chữ cái đầu thành chữ IN HOA
                    Result[1] = char.ToUpper(LastName[0][0]) + LastName[0].Substring(1);

                    //Nhặt ra MidleName
                    for (int l = 1; l < LastName.Length; l++)
                    {
                        if (Result[2] != null)
                        {
                            //Conver chữ cái đầu sang chư IH
                            if (LastName[l] != null && LastName[l] != "")
                            {
                                LastName[l] = char.ToUpper(LastName[l][0]) + LastName[l].Substring(1);
                                Result[2] = Result[2].Trim() + " " + LastName[l];
                            }
                        }
                        else
                        {
                            //Conver chữ cái đầu sang chư IH
                            if (LastName[l] != null && LastName[l] != "")
                            {
                                LastName[l] = char.ToUpper(LastName[l][0]) + LastName[l].Substring(1);
                                Result[2] = LastName[l].Trim();
                            }
                        }
                    }
                    if (Result[2] == null)
                    {
                        Result[2] = "";
                        //Bỏ khoảng trắng
                        Result[2] = Result[2].Trim();
                    }
                }
                //Xác định Title           
                string title = Arr[Arr.Length - 1].Trim();
                title = title.Replace("(", ""); title = title.Replace(")", "");
                title = title.Replace("{", ""); title = title.Replace("}", "");
                title = title.Replace("[", ""); title = title.Replace("]", "");
                ArrayList arr1 = TitleBO.Instance.FindByAttribute("Code", title);
                if (arr1.Count > 0)
                    Result[4] = ((TitleModel)arr1[0]).ID.ToString();
                else
                    Result[4] = "0";
                //Nếu TitleID có tồn tại thì Remove nó trong Name
                if (Result[4] != "0")
                {
                    //Bỏ Titile nếu có
                    Name = Name.Substring(0, (Name.Length - Arr[Arr.Length - 1].Length) - 1);
                    //Bỏ LastName, MidleName
                    if (Arr.Length > 2)
                    {
                        string[] LM = Name.Split(',');
                        Name = Name.Remove(0, (LM[0].Length + 1));
                    }
                }
                else
                {
                    //Bỏ LastName, MidleName 
                    string[] LM = Name.Split(',');
                    Name = Name.Remove(0, (LM[0].Length + 1));
                }
                //Xác định Chuỗi còn lại trước khi Remove
                if (Arr.Length == 2 && Result[4] != "0")
                {
                    Result[0] = Name;
                    Result[0] = char.ToUpper(Name[0]) + Name.Substring(1);
                    Result[3] = Result[0].Trim() + ", " + ((TitleModel)TitleBO.Instance.FindByPrimaryKey(int.Parse(Result[4].ToString()))).Code;
                }
                else
                {
                    //Nhặt ra FirstName
                    Name = Name.Trim();
                    string[] FirstName = Name.Split(',');
                    //Nếu nhập sai Title thì remove title đi
                    if (FirstName.Length > 1 && Result[4] == "0")
                        Name = Name.Substring(0, (Name.Length - Arr[Arr.Length - 1].Length) - 1);
                    //Tách FirstName
                    FirstName = Name.Split(' ');
                    for (int f = 0; f < FirstName.Length; f++)
                    {
                        if (Result[0] != null)
                        {
                            //Conver chữ cái đầu sang chư IH
                            FirstName[f] = char.ToUpper(FirstName[f][0]) + FirstName[f].Substring(1);
                            Result[0] = Result[0].Trim() + " " + FirstName[f];
                        }
                        else
                        {
                            //Conver chữ cái đầu sang chữ IH
                            if (FirstName[f] != "")
                            {
                                FirstName[f] = char.ToUpper(FirstName[f][0]) + FirstName[f].Substring(1);
                                Result[0] = FirstName[f].Trim();
                            }
                            else
                            {
                                Result[0] = "";
                            }
                        }
                    }
                    //Nhặt ra Name
                    if (Result[4] == "0")
                        if (Result[2].Trim() != "")
                            Result[3] = Result[1].Trim() + " " + Result[2].Trim() + "," + " " + Result[0].Trim();
                        else
                            Result[3] = Result[1].Trim() + "," + " " + Result[0].Trim();
                    else
                        if (Result[2].Trim() != "")
                            Result[3] = Result[1].Trim() + " " + Result[2].Trim() + ", " + Result[0].Trim() + ", " + ((TitleModel)TitleBO.Instance.FindByPrimaryKey(int.Parse(Result[4].ToString()))).Code;
                        else
                            Result[3] = Result[1].Trim() + ", " + Result[0].Trim() + ", " + ((TitleModel)TitleBO.Instance.FindByPrimaryKey(int.Parse(Result[4].ToString()))).Code;
                }
            }
            return Result;
        }

        #endregion

        #region Tuan_ProfileSreach
        [HttpGet]
        public async Task<IActionResult> ProfileIndividualSearch(string LastName, string FirstName)
        {
            try
            {
                Microsoft.Data.SqlClient.SqlParameter[] param = [
                    new Microsoft.Data.SqlClient.SqlParameter("@LastName", LastName ?? string.Empty),
                    new Microsoft.Data.SqlClient.SqlParameter("@FirstName", FirstName ?? string.Empty)
                    ];
                DataTable myTable = DataTableHelper.getTableData("spProfileIndividualSearchName", param);


                var relust = myTable.AsEnumerable()
                    .Select(d => new
                    {
                        ID = d.Field<int?>("ID") ?? 0,
                        Name = d.Field<string>("Name") ?? "",
                    })
                    .ToList();
                return Json(new
                {
                    success = true,
                    data = relust
                });
            }
            catch (Exception ex)
            {

                return Json(new
                {
                    success = false,
                    message = "Error: " + ex.Message
                });
            }
        }


        [HttpGet]
        public async Task<IActionResult> ProfileGroupSearch(string GroupName)
        {
            try
            {
                Microsoft.Data.SqlClient.SqlParameter[] param = [
                    new Microsoft.Data.SqlClient.SqlParameter("@GroupName", GroupName ?? string.Empty)];
                DataTable myTable = DataTableHelper.getTableData("spProfileGroupSearchName", param);


                var relust = myTable.AsEnumerable()
                    .Select(d => new
                    {
                        ID = d.Field<int?>("ID") ?? 0,
                        Account = d.Field<string>("Account") ?? "",
                    })
                    .ToList();
                return Json(new
                {
                    success = true,
                    data = relust
                });
            }
            catch (Exception ex)
            {

                return Json(new
                {
                    success = false,
                    message = "Error: " + ex.Message
                });
            }
        }
        [HttpGet("FindIndividualByID")]
        public async Task<IActionResult> FindIndividualByID(int id)
        {
            try
            {
                if (ProfileBO.Instance.FindByPrimaryKey(id) is not ProfileModel result)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Profile not found or invalid type"
                    });
                }
                return Json(new
                {
                    success = true,
                    data = result
                });
            }
            catch (Exception ex)
            {

                return Json(new
                {
                    success = false,
                    message = "Error: " + ex.Message
                });
            }
        }
        [HttpGet("FindGroupByID")]
        public async Task<IActionResult> FindGroupByID(int id)
        {
            try
            {
                if (ProfileBO.Instance.FindByPrimaryKey(id) is not ProfileModel result)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Profile not found or invalid type"
                    });
                }
                return Json(new
                {
                    success = true,
                    data = result
                });
            }
            catch (Exception ex)
            {

                return Json(new
                {
                    success = false,
                    message = "Error: " + ex.Message
                });
            }
        }

        #endregion

        #region Nam showNotification
        [HttpGet]
        public async Task<IActionResult> ShowNotification()
        {
            try
            {
                var businessDates = PropertyUtils.ConvertToList<BusinessDateModel>(
                    BusinessDateBO.Instance.FindAll()
                );

                DateTime businessDate = businessDates[0].BusinessDate;
                string sql = $@"
                    SELECT DateOfBirth, Firstname
                    FROM Profile WITH (NOLOCK)
                    WHERE DAY(DateOfBirth)   = DAY('{businessDate:yyyy-MM-dd}')
                      AND MONTH(DateOfBirth) = MONTH('{businessDate:yyyy-MM-dd}')
                      AND YEAR(DateOfBirth) BETWEEN 2023 AND YEAR(GETDATE())
                      AND Firstname IS NOT NULL
                      AND LTRIM(RTRIM(Firstname)) <> ''";

                DataTable dataTable = TextUtils.Select(sql);

                var result = (from d in dataTable.AsEnumerable()
                              select d.Table.Columns.Cast<DataColumn>()
                                  .ToDictionary(
                                      col => col.ColumnName,
                                      col => d[col.ColumnName]?.ToString()
                                  )).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        #endregion

        #region New/Edit Profile
        public IActionResult Individual()
        {
            return PartialView("~/Views/Profile/NewProfileModal/Individual.cshtml");

        }

        public IActionResult Company()
        {
            return PartialView("~/Views/Profile/NewProfileModal/Company.cshtml");
        }

        public IActionResult Contact()
        {
            return PartialView("~/Views/Profile/NewProfileModal/Contact.cshtml");

        }

        public IActionResult Group()
        {
            return PartialView("~/Views/Profile/NewProfileModal/Group.cshtml");

        }

        [HttpPost]
        public IActionResult SaveProfile([FromBody] ProfileModel model)
        {
            try
            {

                var checks = new List<ValidationError?>();
                if (model == null)
                {
                    return Json(new { success = false, message = "Invalid data (model null)" });
                }

                bool isDuplicate = ProfileBO.Instance
                   .IsDuplicateCode(model.Code, model.ID);

                switch (model.Type)
                {
                    case 0: // Individual 
                        bool isInvalidIndividual = string.IsNullOrEmpty(model.Code) || model.Code.Length < 13;
                        checks.Add(Check(isInvalidIndividual, "ind_txtCode", "Invalid code (minimum 13 characters)"));
                        checks.Add(Check(model.Account, "ind_txtFullName", "Full name not blank"));
                        checks.Add(Check(model.Firstname, "ind_txtFirstName", "First name not blank"));
                        checks.Add(Check(model.LastName, "ind_txtLastName", "Last name not blank"));
                        checks.Add(Check(model.NationalityID, "ind_txtNationality", "Nationality not blank"));

                        checks.Add(CheckDuplicate(isDuplicate, "ind_txtCode", $"This code already exists: [{model.Code}]"));
                        break;

                    case 1: // Travel Agent 
                    case 2: // Company  
                    case 3: // SourceDeleteProfile
                        bool isInvalidTax = string.IsNullOrEmpty(model.Code) || model.Code.Length < 13;
                        checks.Add(Check(isInvalidTax, "com_txtCode", "Invalid code (minimum 13 characters)"));
                        checks.Add(Check(model.Account, "com_txtAccount", "Account not blank"));
                        checks.Add(CheckDuplicate(isDuplicate, "com_txtCode", $"This code already exists: [{model.Code}]"));

                        break;
                    case 4: // Group
                        bool isInvalidGroupCode = string.IsNullOrEmpty(model.Code) || model.Code.Length < 13;
                        checks.Add(Check(isInvalidGroupCode, "gro_txtCode", "Invalid code (minimum 13 characters)"));
                        checks.Add(Check(model.Account, "gro_txtGroupName", "Group Name be blank"));
                        checks.Add(CheckDuplicate(isDuplicate, "gro_txtCode", $"This code already exists: [{model.Code}]"));

                        break;
                    case 5: // Contact
                        bool isInvalidContactCode = string.IsNullOrEmpty(model.Code) || model.Code.Length < 13;
                        checks.Add(Check(isInvalidContactCode, "con_txtCode", "Invalid code (minimum 13 characters)"));
                        checks.Add(Check(model.Account, "con_txtFullName", "Full name not blank"));
                        checks.Add(Check(model.Firstname, "con_txtFirstName", "First name not blank"));
                        checks.Add(Check(model.LastName, "con_txtLastName", "Last name not blank"));
                        checks.Add(CheckDuplicate(isDuplicate, "con_txtCode", $"This code already exists: [{model.Code}]"));

                        break;

                    default: 
                        
                        break;
                }

                // Gom lỗi
                var listErrors = GetErrors(checks.ToArray());

                if (listErrors.Count > 0)
                {
                    return Json(new { success = false, errors = listErrors });
                }

                DateTime businessDate = TextUtils.GetBusinessDate();

                if (model.ID > 0)
                {
                    var oldData = (ProfileModel)ProfileBO.Instance.FindByPrimaryKey(model.ID);
                    if (oldData == null) return Json(new { success = false, message = "The data does not exist or has been deleted." });
                    model.CreateDate = oldData.CreateDate;
                    model.UserInsertID = oldData.UserInsertID;
                    model.UpdateDate = businessDate;
                    ProfileBO.Instance.Update(model);
                }
                else
                {

                    model.CreateDate = businessDate;
                    model.UpdateDate = businessDate;
                    ProfileBO.Instance.Insert(model);
                }

                return Json(new { success = true, message = "Save successfully" });

            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }

        }

        #endregion

    }
}

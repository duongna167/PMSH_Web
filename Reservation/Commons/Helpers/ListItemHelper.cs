using BaseBusiness.BO;
using BaseBusiness.Model;
using BaseBusiness.util;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reservation.Commons.Helpers
{
    public static class ListItemHelper
    {
        private static readonly string _textDefault = "";

        /// <summary>
        /// Lấy tất cả danh sách nationality inactive cho dropdown
        /// </summary>
        /// <param name="defaultValue">Giá trị mặc định.</param>
        /// <param name="textDefault">Text thứ hai.</param>
        /// <returns>Danh sách nationality</returns>
        public static List<SelectListItem> GetNationalityProvider(bool defaultValue = true, string textDefault = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(textDefault)) textDefault = _textDefault;

                var items = new List<SelectListItem>();
                List<NationalityModel> list = PropertyUtils.ConvertToList<NationalityModel>(NationalityBO.Instance.FindByAttribute("Inactive", 0));
                if (list.Count > 0)
                {
                    items = list.Select(p => new SelectListItem { Value = p.ID.ToString(), Text = p.Name, Selected = false }).ToList();
                }
                if (defaultValue)
                    items.Insert(0, new SelectListItem { Text = textDefault, Value = "0", Selected = true });

                return items;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return new List<SelectListItem>();
            }
        }

        /// <summary>
        /// Lấy tất cả danh sách Title inactive cho dropdown
        /// </summary>
        /// <param name="defaultValue">Giá trị mặc định.</param>
        /// <param name="textDefault">Text thứ hai.</param>
        /// <returns>Danh sách Title</returns>
        public static List<SelectListItem> GetTitleProvider(bool defaultValue = true, string textDefault = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(textDefault)) textDefault = _textDefault;

                var items = new List<SelectListItem>();
                List<TitleModel> list = PropertyUtils.ConvertToList<TitleModel>(TitleBO.Instance.FindByAttribute("Inactive", 0));
                if (list.Count > 0)
                {
                    items = list.Select(p => new SelectListItem { Value = p.Code, Text = p.Name, Selected = false }).ToList();
                }
                if (defaultValue)
                    items.Insert(0, new SelectListItem { Text = textDefault, Value = "", Selected = true });

                return items;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return new List<SelectListItem>();
            }
        }
        public static List<SelectListItem> GetTitleProviderRSV(bool defaultValue = true, string textDefault = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(textDefault)) textDefault = _textDefault;

                var items = new List<SelectListItem>();
                List<TitleModel> list = PropertyUtils.ConvertToList<TitleModel>(TitleBO.Instance.FindByAttribute("Inactive", 0));
                if (list.Count > 0)
                {
                    items = list.Select(p => new SelectListItem { Value = p.ID.ToString(), Text = p.Code, Selected = false }).ToList();
                }
                if (defaultValue)
                    items.Insert(0, new SelectListItem { Text = textDefault, Value = "0", Selected = true });

                return items;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return new List<SelectListItem>();
            }
        }
        /// <summary>
        /// Lấy tất cả danh sách City inactive cho dropdown
        /// </summary>
        /// <param name="defaultValue">Giá trị mặc định.</param>
        /// <param name="textDefault">Text thứ hai.</param>
        /// <returns>Danh sách City</returns>
        public static List<SelectListItem> GetCityProvider(bool defaultValue = true, string textDefault = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(textDefault)) textDefault = _textDefault;

                var items = new List<SelectListItem>();
                List<CityModel> list = PropertyUtils.ConvertToList<CityModel>(CityBO.Instance.FindByAttribute("Inactive", 0));
                if (list.Count > 0)
                {
                    items = list.Select(p => new SelectListItem { Value = p.Name, Text = p.Name, Selected = false }).ToList();
                }
                if (defaultValue)
                    items.Insert(0, new SelectListItem { Text = textDefault, Value = "", Selected = true });

                return items;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return new List<SelectListItem>();
            }
        }

        /// <summary>
        /// Lấy tất cả danh sách VIP inactive cho dropdown
        /// </summary>
        /// <param name="defaultValue">Giá trị mặc định.</param>
        /// <param name="textDefault">Text thứ hai.</param>
        /// <returns>Danh sách VIP</returns>
        public static List<SelectListItem> GetVIPProvider(bool defaultValue = true, string textDefault = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(textDefault)) textDefault = _textDefault;

                var items = new List<SelectListItem>();
                List<VIPModel> list = PropertyUtils.ConvertToList<VIPModel>(VIPBO.Instance.FindByAttribute("Inactive", 0));
                if (list.Count > 0)
                {
                    items = list.Select(p => new SelectListItem { Value = p.ID.ToString(), Text = p.Name, Selected = false }).ToList();
                }
                if (defaultValue)
                    items.Insert(0, new SelectListItem { Text = textDefault, Value = "0", Selected = true });

                return items;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return new List<SelectListItem>();
            }
        }

        /// <summary>
        /// Lấy tất cả danh sách MemberType inactive cho dropdown
        /// </summary>
        /// <param name="defaultValue">Giá trị mặc định.</param>
        /// <param name="textDefault">Text thứ hai.</param>
        /// <returns>Danh sách MemberType</returns>
        public static List<SelectListItem> GetMemberTypeProvider(bool defaultValue = true, string textDefault = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(textDefault)) textDefault = _textDefault;

                var items = new List<SelectListItem>();
                List<MemberTypeModel> list = PropertyUtils.ConvertToList<MemberTypeModel>(MemberTypeBO.Instance.FindByAttribute("Inactive", 0));
                if (list.Count > 0)
                {
                    items = list.Select(p => new SelectListItem { Value = p.ID.ToString(), Text = p.Name, Selected = false }).ToList();
                }
                if (defaultValue)
                    items.Insert(0, new SelectListItem { Text = textDefault, Value = "0", Selected = true });

                return items;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return new List<SelectListItem>();
            }
        }

        /// <summary>
        /// Lấy tất cả danh sách Profile là Agent cho dropdown
        /// </summary>
        /// <param name="defaultValue">Giá trị mặc định.</param>
        /// <param name="textDefault">Text thứ hai.</param>
        /// <returns>Danh sách Profile là Agent</returns>
        public static List<SelectListItem> GetProfileAgentProvider(bool defaultValue = true, string textDefault = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(textDefault)) textDefault = _textDefault;

                var items = new List<SelectListItem>();
                List<ProfileModel> list = PropertyUtils.ConvertToList<ProfileModel>(ProfileBO.Instance.FindByAttribute("Type", 1));
                if (list.Count > 0)
                {
                    items = list.Select(p => new SelectListItem { Value = p.ID.ToString(), Text = p.Account, Selected = false }).ToList();
                }
                if (defaultValue)
                    items.Insert(0, new SelectListItem { Text = textDefault, Value = "0", Selected = true });

                return items;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return new List<SelectListItem>();
            }
        }

        /// <summary>
        /// Lấy tất cả danh sách Profile là Company cho dropdown
        /// </summary>
        /// <param name="defaultValue">Giá trị mặc định.</param>
        /// <param name="textDefault">Text thứ hai.</param>
        /// <returns>Danh sách Profile là Company</returns>
        public static List<SelectListItem> GetProfileCompanyProvider(bool defaultValue = true, string textDefault = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(textDefault)) textDefault = _textDefault;

                var items = new List<SelectListItem>();
                List<ProfileModel> list = PropertyUtils.ConvertToList<ProfileModel>(ProfileBO.Instance.FindByAttribute("Type", 2));
                if (list.Count > 0)
                {
                    items = list.Select(p => new SelectListItem { Value = p.ID.ToString(), Text = p.Account, Selected = false }).ToList();
                }
                if (defaultValue)
                    items.Insert(0, new SelectListItem { Text = textDefault, Value = "0", Selected = true });

                return items;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return new List<SelectListItem>();
            }
        }

        /// <summary>
        /// Lấy tất cả danh sách Profile là Contact cho dropdown
        /// </summary>
        /// <param name="defaultValue">Giá trị mặc định.</param>
        /// <param name="textDefault">Text thứ hai.</param>
        /// <returns>Danh sách Profile là Contact</returns>
        public static List<SelectListItem> GetProfileContactProvider(bool defaultValue = true, string textDefault = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(textDefault)) textDefault = _textDefault;

                var items = new List<SelectListItem>();
                List<ProfileModel> list = PropertyUtils.ConvertToList<ProfileModel>(ProfileBO.Instance.FindByAttribute("Type", 5));
                if (list.Count > 0)
                {
                    items = list.Select(p => new SelectListItem { Value = p.ID.ToString(), Text = p.Account, Selected = false }).ToList();
                }
                if (defaultValue)
                    items.Insert(0, new SelectListItem { Text = textDefault, Value = "0", Selected = true });

                return items;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return new List<SelectListItem>();
            }
        }

        /// <summary>
        /// Lấy tất cả danh sách RoomType cho dropdown
        /// </summary>
        /// <param name="defaultValue">Giá trị mặc định.</param>
        /// <param name="textDefault">Text thứ hai.</param>
        /// <returns>Danh sách RoomType</returns>
        public static List<SelectListItem> GetRoomTyeProvider(bool defaultValue = true, string textDefault = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(textDefault)) textDefault = _textDefault;

                var items = new List<SelectListItem>();
                List<RoomTypeModel> list = PropertyUtils.ConvertToList<RoomTypeModel>(RoomTypeBO.Instance.FindByAttribute("InActive", 0));
                if (list.Count > 0)
                {
                    items = list.Select(p => new SelectListItem { Value = p.ID.ToString(), Text = p.Code, Selected = false }).ToList();
                }
                if (defaultValue)
                    items.Insert(0, new SelectListItem { Text = textDefault, Value = "0", Selected = true });

                return items;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return new List<SelectListItem>();
            }
        }

        /// <summary>
        /// Lấy tất cả danh sách RoomType cho dropdown
        /// </summary>
        /// <param name="defaultValue">Giá trị mặc định.</param>
        /// <param name="textDefault">Text thứ hai.</param>
        /// <returns>Danh sách RoomType</returns>
        public static List<SelectListItem> GetRoomTyeCodeProvider(bool defaultValue = true, string textDefault = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(textDefault)) textDefault = _textDefault;

                var items = new List<SelectListItem>();
                List<RoomTypeModel> list = PropertyUtils.ConvertToList<RoomTypeModel>(RoomTypeBO.Instance.FindByAttribute("InActive", 0));
                if (list.Count > 0)
                {
                    items = list.Select(p => new SelectListItem { Value = p.Code.ToString(), Text = p.Code + "- " + p.Name, Selected = false }).ToList();
                }
                if (defaultValue)
                    items.Insert(0, new SelectListItem { Text = textDefault, Value = "0", Selected = true });

                return items;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return new List<SelectListItem>();
            }
        }
        /// <summary>
        /// Lấy tất cả danh sách Room cho dropdown
        /// </summary>
        /// <param name="defaultValue">Giá trị mặc định.</param>
        /// <param name="textDefault">Text thứ hai.</param>
        /// <returns>Danh sách Room</returns>
        public static List<SelectListItem> GetRoomProvider(bool defaultValue = true, string textDefault = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(textDefault)) textDefault = _textDefault;

                var items = new List<SelectListItem>();
                List<RoomModel> list = PropertyUtils.ConvertToList<RoomModel>(RoomBO.Instance.FindAll());
                if (list.Count > 0)
                {
                    items = list.Select(p => new SelectListItem { Value = p.RoomNo.ToString(), Text = p.RoomNo, Selected = false }).ToList();
                }
                if (defaultValue)
                    items.Insert(0, new SelectListItem { Text = textDefault, Value = "0", Selected = true });

                return items;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return new List<SelectListItem>();
            }
        }
        /// <summary>
        /// Lấy tất cả danh sách Room cho dropdown
        /// </summary>
        /// <param name="defaultValue">Giá trị mặc định.</param>
        /// <param name="textDefault">Text thứ hai.</param>
        /// <returns>Danh sách Room</returns>
        public static List<SelectListItem> GetRoomClassProvider(bool defaultValue = true, string textDefault = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(textDefault)) textDefault = _textDefault;

                var items = new List<SelectListItem>();
                List<RoomClassModel> list = PropertyUtils.ConvertToList<RoomClassModel>(RoomClassBO.Instance.FindByAttribute("InActive", 0));
                if (list.Count > 0)
                {
                    items = list.Select(p => new SelectListItem { Value = p.ID.ToString(), Text = p.Code, Selected = false }).ToList();
                }
                if (defaultValue)
                    items.Insert(0, new SelectListItem { Text = textDefault, Value = "0", Selected = true });

                return items;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return new List<SelectListItem>();
            }
        }

        /// <summary>
        /// Lấy tất cả danh sách Currency  cho dropdown
        /// </summary>
        /// <param name="defaultValue">Giá trị mặc định.</param>
        /// <param name="textDefault">Text thứ hai.</param>
        /// <returns>Danh sách Currency</returns>
        public static List<SelectListItem> GetCurrencyProvider(bool defaultValue = true, string textDefault = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(textDefault)) textDefault = _textDefault;

                var items = new List<SelectListItem>();
                List<CurrencyModel> list = PropertyUtils.ConvertToList<CurrencyModel>(CurrencyBO.Instance.FindByAttribute("Inactive", 0));
                if (list.Count > 0)
                {
                    items = list.Select(p => new SelectListItem { Value = p.ID.ToString(), Text = p.Description, Selected = false }).ToList();
                }
                if (defaultValue)
                    items.Insert(0, new SelectListItem { Text = textDefault, Value = "0", Selected = true });

                return items;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return new List<SelectListItem>();
            }
        }

        /// <summary>
        /// Lấy tất cả danh sách Packages cho dropdown
        /// </summary>
        /// <param name="defaultValue">Giá trị mặc định.</param>
        /// <param name="textDefault">Text thứ hai.</param>
        /// <returns>Danh sách Packages</returns>
        public static List<SelectListItem> GetPackagesProvider(bool defaultValue = true, string textDefault = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(textDefault)) textDefault = _textDefault;

                var items = new List<SelectListItem>();
                List<PackageModel> list = PropertyUtils.ConvertToList<PackageModel>(PackageBO.Instance.FindByAttribute("Active", 1));
                if (list.Count > 0)
                {
                    items = list.Select(p => new SelectListItem { Value = p.ID.ToString(), Text = p.Code + " - " + p.Description, Selected = false }).ToList();
                }
                if (defaultValue)
                    items.Insert(0, new SelectListItem { Text = textDefault, Value = "0", Selected = true });

                return items;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return new List<SelectListItem>();
            }
        }

        /// <summary>
        /// Lấy tất cả danh sách Packages cho dropdown
        /// </summary>
        /// <param name="defaultValue">Giá trị mặc định.</param>
        /// <param name="textDefault">Text thứ hai.</param>
        /// <returns>Danh sách Packages</returns>
        public static List<SelectListItem> GetReasonProvider(bool defaultValue = true, string textDefault = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(textDefault)) textDefault = _textDefault;

                var items = new List<SelectListItem>();
                List<ReasonModel> list = PropertyUtils.ConvertToList<ReasonModel>(ReasonBO.Instance.FindByAttribute("Inactive", 0));
                if (list.Count > 0)
                {
                    items = list.Select(p => new SelectListItem { Value = p.ID.ToString(), Text = p.Code + " - " + p.Name, Selected = false }).ToList();
                }
                if (defaultValue)
                    items.Insert(0, new SelectListItem { Text = textDefault, Value = "0", Selected = true });

                return items;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return new List<SelectListItem>();
            }
        }

        /// <summary>
        /// Lấy tất cả danh sách ReservationType cho dropdown
        /// </summary>
        /// <param name="defaultValue">Giá trị mặc định.</param>
        /// <param name="textDefault">Text thứ hai.</param>
        /// <returns>Danh sách ReservationType</returns>
        public static List<SelectListItem> GetReservationTypeProvider(bool defaultValue = true, string textDefault = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(textDefault)) textDefault = _textDefault;

                var items = new List<SelectListItem>();
                List<ReservationTypeModel> list = PropertyUtils.ConvertToList<ReservationTypeModel>(ReservationTypeBO.Instance.FindByAttribute("Inactive", 0));
                if (list.Count > 0)
                {
                    items = list.Select(p => new SelectListItem { Value = p.ID.ToString(), Text = p.Name, Selected = false }).ToList();
                }
                if (defaultValue)
                    items.Insert(0, new SelectListItem { Text = textDefault, Value = "0", Selected = true });

                return items;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return new List<SelectListItem>();
            }
        }


        /// <summary>
        /// Lấy tất cả danh sách Source cho dropdown
        /// </summary>
        /// <param name="defaultValue">Giá trị mặc định.</param>
        /// <param name="textDefault">Text thứ hai.</param>
        /// <returns>Danh sách Source</returns>
        public static List<SelectListItem> GetSourceProvider(bool defaultValue = true, string textDefault = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(textDefault)) textDefault = _textDefault;

                var items = new List<SelectListItem>();
                List<SourceModel> list = PropertyUtils.ConvertToList<SourceModel>(SourceBO.Instance.FindByAttribute("Inactive", 0));
                if (list.Count > 0)
                {
                    items = list.Select(p => new SelectListItem { Value = p.ID.ToString(), Text = p.Name, Selected = false }).ToList();
                }
                if (defaultValue)
                    items.Insert(0, new SelectListItem { Text = textDefault, Value = "0", Selected = true });

                return items;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return new List<SelectListItem>();
            }
        }

        /// <summary>
        /// Lấy tất cả danh sách Market cho dropdown
        /// </summary>
        /// <param name="defaultValue">Giá trị mặc định.</param>
        /// <param name="textDefault">Text thứ hai.</param>
        /// <returns>Danh sách Market</returns>
        public static List<SelectListItem> GetMarketProvider(bool defaultValue = true, string textDefault = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(textDefault)) textDefault = _textDefault;

                var items = new List<SelectListItem>();
                List<MarketModel> list = PropertyUtils.ConvertToList<MarketModel>(MarketBO.Instance.FindByAttribute("Inactive", 0));
                if (list.Count > 0)
                {
                    items = list.Select(p => new SelectListItem { Value = p.ID.ToString(), Text = p.Name, Selected = false }).ToList();
                }
                if (defaultValue)
                    items.Insert(0, new SelectListItem { Text = textDefault, Value = "0", Selected = true });

                return items;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return new List<SelectListItem>();
            }
        }

        /// <summary>
        /// Lấy tất cả danh sách Profile cho dropdown
        /// </summary>
        /// <param name="defaultValue">Giá trị mặc định.</param>
        /// <param name="textDefault">Text thứ hai.</param>
        /// <returns>Danh sách Profile</returns>
        public static List<SelectListItem> GetProfileProvider(bool defaultValue = true, string textDefault = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(textDefault)) textDefault = _textDefault;

                var items = new List<SelectListItem>();
                List<ProfileModel> list = PropertyUtils.ConvertToList<ProfileModel>(ProfileBO.Instance.FindAll());
                if (list.Count > 0)
                {
                    items = list.Select(p => new SelectListItem { Value = p.ID.ToString(), Text = p.Account, Selected = false }).ToList();
                }
                if (defaultValue)
                    items.Insert(0, new SelectListItem { Text = textDefault, Value = "0", Selected = true });

                return items;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return new List<SelectListItem>();
            }
        }

        /// <summary>
        /// Lấy tất cả danh sách AllotmentType cho dropdown
        /// </summary>
        /// <param name="defaultValue">Giá trị mặc định.</param>
        /// <param name="textDefault">Text thứ hai.</param>
        /// <returns>Danh sách AllotmentType</returns>
        public static List<SelectListItem> GetAllotmentTypeProvider(bool defaultValue = true, string textDefault = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(textDefault)) textDefault = _textDefault;

                var items = new List<SelectListItem>();
                List<AllotmentTypeModel> list = PropertyUtils.ConvertToList<AllotmentTypeModel>(AllotmentTypeBO.Instance.FindAll());
                if (list.Count > 0)
                {
                    items = list.Select(p => new SelectListItem { Value = p.ID.ToString(), Text = p.Name, Selected = false }).ToList();
                }
                if (defaultValue)
                    items.Insert(0, new SelectListItem { Text = textDefault, Value = "0", Selected = true });

                return items;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return new List<SelectListItem>();
            }
        }

        /// <summary>
        /// Lấy tất cả danh sách PersonInCharge cho dropdown
        /// </summary>
        /// <param name="defaultValue">Giá trị mặc định.</param>
        /// <param name="textDefault">Text thứ hai.</param>
        /// <returns>Danh sách PersonInCharge</returns>
        public static List<SelectListItem> GetPersonInChargeProvider(bool defaultValue = true, string textDefault = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(textDefault)) textDefault = _textDefault;

                var items = new List<SelectListItem>();
                List<PersonInChargeModel> list = PropertyUtils.ConvertToList<PersonInChargeModel>(PersonInChargeBO.Instance.FindAll());
                if (list.Count > 0)
                {
                    items = list.Select(p => new SelectListItem { Value = p.ID.ToString(), Text = p.Name, Selected = false }).ToList();
                }
                if (defaultValue)
                    items.Insert(0, new SelectListItem { Text = textDefault, Value = "0", Selected = true });

                return items;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return new List<SelectListItem>();
            }
        }


        /// <summary>
        /// Lấy tất cả danh sách PaymentMethod cho dropdown
        /// </summary>
        /// <param name="defaultValue">Giá trị mặc định.</param>
        /// <param name="textDefault">Text thứ hai.</param>
        /// <returns>Danh sách PaymentMethod</returns>
        public static List<SelectListItem> GetPaymentMethodProvider(bool defaultValue = true, string textDefault = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(textDefault)) textDefault = _textDefault;

                var items = new List<SelectListItem>();
                List<PaymentMethodModel> list = PropertyUtils.ConvertToList<PaymentMethodModel>(PaymentMethodBO.Instance.FindAll());
                if (list.Count > 0)
                {
                    items = list.Select(p => new SelectListItem { Value = p.ID.ToString(), Text = p.CreditCardNo, Selected = false }).ToList();
                }
                if (defaultValue)
                    items.Insert(0, new SelectListItem { Text = textDefault, Value = "0", Selected = true });

                return items;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return new List<SelectListItem>();
            }
        }


        /// <summary>
        /// Lấy tất cả danh sách Promotion cho dropdown
        /// </summary>
        /// <param name="defaultValue">Giá trị mặc định.</param>
        /// <param name="textDefault">Text thứ hai.</param>
        /// <returns>Danh sách Promotion</returns>
        public static List<SelectListItem> GetPromotionProvider(bool defaultValue = true, string textDefault = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(textDefault)) textDefault = _textDefault;

                var items = new List<SelectListItem>();
                List<PromotionModel> list = PropertyUtils.ConvertToList<PromotionModel>(PromotionBO.Instance.FindAll());
                if (list.Count > 0)
                {
                    items = list.Select(p => new SelectListItem { Value = p.ID.ToString(), Text = p.Name, Selected = false }).ToList();
                }
                if (defaultValue)
                    items.Insert(0, new SelectListItem { Text = textDefault, Value = "0", Selected = true });

                return items;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return new List<SelectListItem>();
            }
        }

        /// <summary>
        /// Lấy tất cả danh sách Group Preference cho dropdown
        /// </summary>
        /// <param name="defaultValue">Giá trị mặc định.</param>
        /// <param name="textDefault">Text thứ hai.</param>
        /// <returns>Danh sách Group Preference</returns>
        public static List<SelectListItem> GetGroupPreferenceProvider(bool defaultValue = true, string textDefault = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(textDefault)) textDefault = _textDefault;

                var items = new List<SelectListItem>();
                List<PreferenceGroupModel> list = PropertyUtils.ConvertToList<PreferenceGroupModel>(PreferenceGroupBO.Instance.FindByAttribute("Inactive", 0));
                if (list.Count > 0)
                {
                    items = list.Select(p => new SelectListItem { Value = p.ID.ToString(), Text = p.Name, Selected = false }).ToList();
                }
                if (defaultValue)
                    items.Insert(0, new SelectListItem { Text = textDefault, Value = "0", Selected = true });

                return items;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return new List<SelectListItem>();
            }
        }

        /// <summary>
        /// Lấy tất cả danh sách Transport Type cho dropdown
        /// </summary>
        /// <param name="defaultValue">Giá trị mặc định.</param>
        /// <param name="textDefault">Text thứ hai.</param>
        /// <returns>Danh sách Transport Type</returns>
        public static List<SelectListItem> GetTransportTypeProvider(bool defaultValue = true, string textDefault = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(textDefault)) textDefault = _textDefault;

                var items = new List<SelectListItem>();
                List<TransportTypeModel> list = PropertyUtils.ConvertToList<TransportTypeModel>(TransportTypeBO.Instance.FindByAttribute("Inactive", 0));
                if (list.Count > 0)
                {
                    items = list.Select(p => new SelectListItem { Value = p.ID.ToString(), Text = p.Name, Selected = false }).ToList();
                }
                if (defaultValue)
                    items.Insert(0, new SelectListItem { Text = textDefault, Value = "0", Selected = true });

                return items;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return new List<SelectListItem>();
            }
        }

        /// <summary>
        /// Lấy tất cả danh sách item inventory cho dropdown
        /// </summary>
        /// <param name="defaultValue">Giá trị mặc định.</param>
        /// <param name="textDefault">Text thứ hai.</param>
        /// <returns>Danh sách Item Inventory</returns>
        public static List<SelectListItem> GetItemInventoryProvider(bool defaultValue = true, string textDefault = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(textDefault)) textDefault = _textDefault;

                var items = new List<SelectListItem>();
                List<ItemModel> list = PropertyUtils.ConvertToList<ItemModel>(ItemBO.Instance.FindAll());
                if (list.Count > 0)
                {
                    items = list.Select(p => new SelectListItem { Value = p.ID.ToString(), Text = p.Name, Selected = false }).ToList();
                }
                if (defaultValue)
                    items.Insert(0, new SelectListItem { Text = textDefault, Value = "0", Selected = true });

                return items;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return new List<SelectListItem>();
            }
        }

        /// <summary>
        /// Lấy tất cả danh sách XONE cho dropdown
        /// </summary>
        /// <param name="defaultValue">Giá trị mặc định.</param>
        /// <param name="textDefault">Text thứ hai.</param>
        /// <returns>Danh sách zONE</returns>
        public static List<SelectListItem> GetZoneProvider(bool defaultValue = true, string textDefault = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(textDefault)) textDefault = _textDefault;

                var items = new List<SelectListItem>();
                List<ZoneModel> list = PropertyUtils.ConvertToList<ZoneModel>(ZoneBO.Instance.FindAll());
                if (list.Count > 0)
                {
                    items = list.Select(p => new SelectListItem { Value = p.Code.ToString(), Text = p.Code, Selected = false }).ToList();
                }
                if (defaultValue)
                    items.Insert(0, new SelectListItem { Text = textDefault, Value = "", Selected = true });

                return items;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return new List<SelectListItem>();
            }
        }

        /// <summary>
        /// Lấy tất cả danh sách transaction cho dropdown
        /// </summary>
        /// <param name="defaultValue">Giá trị mặc định.</param>
        /// <param name="textDefault">Text thứ hai.</param>
        /// <returns>Danh sách Item Inventory</returns>
        public static List<SelectListItem> GetTransactionProvider(bool defaultValue = true, string textDefault = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(textDefault)) textDefault = _textDefault;

                var items = new List<SelectListItem>();
                List<TransactionsModel> list = PropertyUtils.ConvertToList<TransactionsModel>(TransactionsBO.Instance.FindByAttribute("IsActive", 1));
                if (list.Count > 0)
                {
                    items = list.Select(p => new SelectListItem { Value = p.Code.ToString(), Text = p.Code + " - " + p.Description, Selected = false }).ToList();
                }
                if (defaultValue)
                    items.Insert(0, new SelectListItem { Text = textDefault, Value = "0", Selected = true });

                return items;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return new List<SelectListItem>();
            }
        }

        public static List<SelectListItem> GetTransactionProvider2(bool defaultValue = true, string textDefault = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(textDefault)) textDefault = "— Chọn giao dịch —";
                var items = new List<SelectListItem>();

                var list = PropertyUtils.ConvertToList<TransactionsModel>(
                    TransactionsBO.Instance.FindByAttribute("IsActive", 1));

                if (list?.Count > 0)
                {
                    items = list.Select(p => new SelectListItem
                    {
                        Value = p.Code.ToString(),
                        Text = p.Code + " - " + p.Description
                    }).ToList();
                }

                // Luôn thêm option mặc định nếu cần
                if (defaultValue)
                {
                    items.Insert(0, new SelectListItem
                    {
                        Text = textDefault,
                        Value = "0",
                        Selected = true  // Chỉ có tác dụng nếu không bị model binding ghi đè
                    });
                }

                return items;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return new List<SelectListItem>
        {
            new SelectListItem { Text = "— Lỗi tải dữ liệu —", Value = "0" }
        };
            }
        }
        /// <summary>
        /// Lấy tất cả danh sách transaction cho dropdown
        /// </summary>
        /// <param name="defaultValue">Giá trị mặc định.</param>
        /// <param name="textDefault">Text thứ hai.</param>
        /// <returns>Danh sách Item Inventory</returns>
        public static List<SelectListItem> GetFloorProvider(bool defaultValue = true, string textDefault = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(textDefault)) textDefault = _textDefault;

                var items = new List<SelectListItem>();
                List<FloorModel> list = PropertyUtils.ConvertToList<FloorModel>(FloorBO.Instance.FindByAttribute("Inactive", 0));
                if (list.Count > 0)
                {
                    items = list.Select(p => new SelectListItem { Value = p.Code.ToString(), Text = p.Code + " - " + p.Description, Selected = false }).ToList();
                }
                if (defaultValue)
                    items.Insert(0, new SelectListItem { Text = textDefault, Value = "0", Selected = true });

                return items;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return new List<SelectListItem>();
            }
        }
        /// <summary>
        /// Lấy tất cả danh sách user cho dropdown
        /// </summary>
        /// <param name="defaultValue">Giá trị mặc định.</param>
        /// <param name="textDefault">Text thứ hai.</param>
        /// <returns>Danh sách User</returns>
        public static List<SelectListItem> GetUserProvider(bool defaultValue = true, string textDefault = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(textDefault)) textDefault = _textDefault;

                var items = new List<SelectListItem>();
                List<UsersModel> list = PropertyUtils.ConvertToList<UsersModel>(UsersBO.Instance.FindAll());
                if (list.Count > 0)
                {
                    items = list.Select(p => new SelectListItem { Value = p.ID.ToString(), Text = p.LoginName, Selected = false }).ToList();
                }
                if (defaultValue)
                    items.Insert(0, new SelectListItem { Text = textDefault, Value = "0", Selected = true });

                return items;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return new List<SelectListItem>();
            }
        }

        /// <summary>
        /// Lấy tất cả danh sách rate code cho dropdown
        /// </summary>
        /// <param name="defaultValue">Giá trị mặc định.</param>
        /// <param name="textDefault">Text thứ hai.</param>
        /// <returns>Danh sách Rate Code</returns>
        public static List<SelectListItem> GetRateCodeProvider(bool defaultValue = true, string textDefault = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(textDefault)) textDefault = _textDefault;

                var items = new List<SelectListItem>();
                List<RateCodeModel> list = PropertyUtils.ConvertToList<RateCodeModel>(RateCodeBO.Instance.FindAll());
                if (list.Count > 0)
                {
                    items = list.Select(p => new SelectListItem { Value = p.ID.ToString(), Text = p.RateCode + "--" + p.Descripton, Selected = false }).ToList();
                }
                if (defaultValue)
                    items.Insert(0, new SelectListItem { Text = textDefault, Value = "0", Selected = true });

                return items;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return new List<SelectListItem>();
            }
        }

        public static List<SelectListItem> GetReasonAdjustForReservationProvider(bool defaultValue = false, string textDefault = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(textDefault))
                    textDefault = _textDefault; // nếu class bạn đã có biến này

                var items = new List<SelectListItem>();
                List<CommentModel> list = CommentBO.GetReasonAdjustForReservation();

                if (list.Count > 0)
                {
                    items = list.Select(p => new SelectListItem
                    {
                        Value = p.Description,
                        Text = p.Code + " - " + p.Description,
                        Selected = false
                    }).ToList();
                }

                if (defaultValue)
                {
                    items.Insert(0, new SelectListItem
                    {
                        Text = textDefault,
                        Value = "0",
                        Selected = true
                    });
                }

                return items;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return [];
            }
        }
    }
}

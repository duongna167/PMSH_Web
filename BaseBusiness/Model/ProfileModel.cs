using BaseBusiness.bc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseBusiness.Model
{
    public class ProfileModel :  BaseModel
    {
        public int ID { get; set; }

        public string Code { get; set; }
        public string Account { get; set; }
        public string FullAccount { get; set; }
        public string LastName { get; set; }
        public string Firstname { get; set; }
        public string MiddleName { get; set; }
        public int LanguageID { get; set; }
        public int TitleID { get; set; }
        public string Address { get; set; }
        public string HomeAddress { get; set; }
        public string City { get; set; }
        public string PostalCode { get; set; }
        public int CountryID { get; set; }
        public int StateID { get; set; }
        public string Salutation { get; set; }
        public int VIPID { get; set; }
        public string VIPReason { get; set; }
        public string PrefRoom { get; set; }
        public DateTime DateOfBirth { get; set; }
        public int NationalityID { get; set; }
        public string PassPort { get; set; }
        public string Keyword { get; set; }
        public string Description { get; set; }
        public string Telephone { get; set; }
        public string Fax { get; set; }
        public string Email { get; set; }
        public string Website { get; set; }
        public string HandPhone { get; set; }
        public bool MailList { get; set; }
        public bool Active { get; set; }
        public bool Contact { get; set; }
        public bool History { get; set; }
        public int ContactProfileID { get; set; }
        public string ARNo { get; set; }
        public string Position { get; set; }
        public string Department { get; set; }
        public string EnvelopGreeting { get; set; }
        public int OwnerID { get; set; }
        public int TerritoryID { get; set; }
        public int PersonInChargeID { get; set; }
        public string AcctContact { get; set; }
        public string CurrencyID { get; set; }
        public string TaxCode { get; set; }
        public int ProfileTypeID { get; set; }
        public int Type { get; set; }
        public string IdentityCard { get; set; }
        public string MemberType { get; set; }
        public string MemberNo { get; set; }
        public string LastRoom { get; set; }
        public DateTime Lastvisit { get; set; }
        public string LastRate { get; set; }
        public string LastRateCode { get; set; }
        public string LastARNo { get; set; }
        public string LastMemberNo { get; set; }
        public int ReturnGuest { get; set; }
        public bool IsBlackList { get; set; }
        public string BlackListReason { get; set; }
        public string SpecialUpdateBy { get; set; }
        public DateTime SpecialUpdateDate { get; set; }
        public int UserInsertID { get; set; }
        public DateTime CreateDate { get; set; }
        public int UserUpdateID { get; set; }
        public DateTime UpdateDate { get; set; }
        public int StayNo { get; set; }
        public string GuestNo { get; set; }
        public string Occupation { get; set; }
        public int BonusPoints { get; set; }
        public int GuestGroupID { get; set; }
        public string Birthplace { get; set; }
        public bool ExpressCheckout { get; set; }
        public bool PayTV { get; set; }
        public DateTime FirstReservation { get; set; }
        public DateTime LastReservation { get; set; }
        public DateTime WeddingAnniversary { get; set; }
        public DateTime Firstvisit { get; set; }
        public string CreditCard { get; set; }
        public DateTime Expiry { get; set; }
        public DateTime LastContact { get; set; }
        public string RateCode { get; set; }
        public int RoomNights { get; set; }
        public int BedNights { get; set; }
        public decimal TotalTurnover { get; set; }
        public decimal LodgeTurnover { get; set; }
        public decimal LodgePackageTurover { get; set; }
        public decimal FBTurnover { get; set; }
        public decimal EventTurnover { get; set; }
        public decimal OtherTurnover { get; set; }
        public int ReturnGuestOld { get; set; }
        public string Company { get; set; }
        public string BusinessTitle { get; set; }
        public string Other { get; set; }
        public bool IsSynchronous { get; set; }
        public string Religion { get; set; }
        public string Nation { get; set; }
        public string PurposeOfStay { get; set; }
        public int MarketID { get; set; }
        public bool IsTransfer { get; set; }
    }
}

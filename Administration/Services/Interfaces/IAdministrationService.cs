using BaseBusiness.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaseBusiness.Model;
using DevExpress.XtraRichEdit.Model;
namespace Administration.Services.Interfaces
{
        public interface IAdministrationService
        {
                public DataTable MemberList(string code, string name, int inactive);
                public DataTable MemberCategory(string code, string name, int inactive);

                public DataTable City(string code, string name, int inactive);
                public DataTable Country(string code, string name, int inactive);
                public DataTable Language(string code, string name, int inactive);
                public DataTable Nationality(string code, string name, int inactive);
                public DataTable Title(string code, string name, int inactive);
                public DataTable Territory(string code, string name, int inactive);
                public DataTable State(string code, string name, int inactive);
                public DataTable VIP(string code, string name, int inactive);
                public DataTable Market(string code, string name, int inactive);
                public DataTable MarketType(string code, string name, int inactive);
                public DataTable PickupDropPlace(string code, string name, int inactive);
                public DataTable TransportType(string code, string name, int inactive);
                public DataTable Reason(string code, string name, int inactive);
                public DataTable Origin(string code, string name, int inactive);
                public DataTable Source(string code, string name, int inactive);
                public DataTable AlertsSetup(string code, string name, int inactive);
                public DataTable Comment(string code, string name, int inactive);
                public DataTable CommentType(string code, string name, int inactive);
                public DataTable Season(string code, string name, int inactive);
                public DataTable Zone(string code, string name, int inactive);
                public DataTable Department(string code, string name, int inactive);
                public DataTable Owner(string code, string name, int inactive);
                public DataTable PropertyType(string code, string description, int sequence);
                public DataTable ReservationType();
                public DataTable PackageForecastGroup(string code, string name, int inactive);
                public DataTable PreferenceGroup(string code, string name, int inactive);
                public List<CurrencyModel> Currency(string ID, bool IsShow = false, bool Inactive = false, bool IsMaster = false);
                public List<CurrencyModel> GetAllCurrency();
                public DataTable hkpEmployee(string code, string name, int inactive);
                public DataTable Property();
                public DataTable PropertyPermission(string userID);
                public DataTable StatusList();
                public DataTable ConfigSystem();
                public DataTable Member(DateTime fromDate, DateTime toDate, string status, string memberID, int isSortByCardName);
                public DataTable PostingHistory(DateTime fromDate, DateTime toDate, string fromFolioID, string toFolioID, string actionType, string user);
                public DataTable PersonInChargeGroupData(string code, string name, string isActive);
                public DataTable PersonInChargeZoneData(string code, string name, string isActive);
                public DataTable ApproveListData(string code, string name, string isActive);
                public DataTable PersonInChargeData(string code, string description, string group, string zone, string isActive);
                public DataTable RateCategory(string code, string name, int inactive);
                public DataTable DepositRule(string code, string description);
                public DataTable CancellationRule(string code, string description);
        }
}

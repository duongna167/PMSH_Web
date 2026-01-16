using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Administration.Services.Interfaces;
using BaseBusiness.BO;
using BaseBusiness.Model;
using BaseBusiness.util;
using DevExpress.XtraRichEdit.Model;
using Microsoft.Data.SqlClient;
using static DevExpress.DataProcessing.InMemoryDataProcessor.AddSurrogateOperationAlgorithm;

namespace Administration.Services.Implements
{
    public class AdministrationService : IAdministrationService
    {
        public DataTable MemberList(string code, string name, int inactive)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@Code", code ?? ""),
                new SqlParameter("@Name", name ?? ""),
                new SqlParameter("@Inactive", inactive)
            };

            DataTable myTable = DataTableHelper.getTableData("spFrmMemberTypeSearch", param);
            return myTable;
        }
        public DataTable MemberCategory(string code, string name, int inactive)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@Code", code ?? ""),
                new SqlParameter("@Name", name ?? ""),
                new SqlParameter("@Inactive", inactive)
            };

            DataTable myTable = DataTableHelper.getTableData("spFrmMemberCategorySearch", param);
            return myTable;
        }
        public DataTable City(string code, string name, int inactive)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@Code", code ?? ""),
                new SqlParameter("@Name", name ?? ""),
                new SqlParameter("@Inactive", inactive)
            };

            DataTable myTable = DataTableHelper.getTableData("spFrmCitySearch", param);
            return myTable;
        }
        public DataTable Country(string code, string name, int inactive)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@Code", code ?? ""),
                new SqlParameter("@Name", name ?? ""),
                new SqlParameter("@Inactive", inactive)
            };

            DataTable myTable = DataTableHelper.getTableData("spFrmCountrySearch", param);
            return myTable;
        }
        public DataTable Language(string code, string name, int inactive)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@Code", code ?? ""),
                new SqlParameter("@Name", name ?? ""),
                new SqlParameter("@Inactive", inactive)
            };

            DataTable myTable = DataTableHelper.getTableData("spFrmLanguageSearch", param);
            return myTable;
        }
        public DataTable Nationality(string code, string name, int inactive)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@Code", code ?? ""),
                new SqlParameter("@Name", name ?? ""),
                new SqlParameter("@Inactive", inactive)
            };

            DataTable myTable = DataTableHelper.getTableData("spFrmNationalitySearch", param);
            return myTable;
        }
        public DataTable Title(string code, string name, int inactive)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@Code", code ?? ""),
                new SqlParameter("@Name", name ?? ""),
                new SqlParameter("@Inactive", inactive)
            };

            DataTable myTable = DataTableHelper.getTableData("spFrmTitleSearch", param);
            return myTable;
        }
        public DataTable Territory(string code, string name, int inactive)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@Code", code ?? ""),
                new SqlParameter("@Name", name ?? ""),
                new SqlParameter("@Inactive", inactive)
            };

            DataTable myTable = DataTableHelper.getTableData("spFrmTerritorySearch", param);
            return myTable;
        }
        public DataTable State(string code, string name, int inactive)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@Code", code ?? ""),
                new SqlParameter("@Name", name ?? ""),
                new SqlParameter("@Inactive", inactive)
            };

            DataTable myTable = DataTableHelper.getTableData("spFrmStateSearch", param);
            return myTable;
        }
        public DataTable VIP(string code, string name, int inactive)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@Code", code ?? ""),
                new SqlParameter("@Name", name ?? ""),
                new SqlParameter("@Inactive", inactive)
            };

            DataTable myTable = DataTableHelper.getTableData("spFrmVIPSearch", param);
            return myTable;
        }
        public DataTable Market(string code, string name, int inactive)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@Code", code ?? ""),
                new SqlParameter("@Name", name ?? ""),
                new SqlParameter("@Inactive", inactive)
            };

            DataTable myTable = DataTableHelper.getTableData("spFrmMarketSearch", param);
            return myTable;

        }
        public DataTable MarketType(string code, string name, int inactive)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@Code", code ?? ""),
                new SqlParameter("@Name", name ?? ""),
                new SqlParameter("@Inactive", inactive)
            };

            DataTable myTable = DataTableHelper.getTableData("spFrmMarketTypeSearch", param);
            return myTable;
        }
        public DataTable PickupDropPlace(string code, string name, int inactive)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@Code", code ?? ""),
                new SqlParameter("@Name", name ?? ""),
                new SqlParameter("@Inactive", inactive)
            };

            DataTable myTable = DataTableHelper.getTableData("spFrmPickupDropPlaceSearch", param);
            return myTable;
        }
        public DataTable TransportType(string code, string name, int inactive)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@Code", code ?? ""),
                new SqlParameter("@Name", name ?? ""),
                new SqlParameter("@Inactive", inactive)
            };

            DataTable myTable = DataTableHelper.getTableData("spFrmTransportTypeSearch", param);
            return myTable;
        }
        public DataTable Reason(string code, string name, int inactive)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@Code", code ?? ""),
                new SqlParameter("@Name", name ?? ""),
                new SqlParameter("@Inactive", inactive)
            };

            DataTable myTable = DataTableHelper.getTableData("spFrmReasonSearch", param);
            return myTable;
        }
        public DataTable Origin(string code, string name, int inactive)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@Code", code ?? ""),
                new SqlParameter("@Name", name ?? ""),
                new SqlParameter("@Inactive", inactive)
            };

            DataTable myTable = DataTableHelper.getTableData("spFrmOriginSearch", param);
            return myTable;
        }
        public DataTable Source(string code, string name, int inactive)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@Code", code ?? ""),
                new SqlParameter("@Name", name ?? ""),
                new SqlParameter("@Inactive", inactive)
            };

            DataTable myTable = DataTableHelper.getTableData("spFrmSourceSearch", param);
            return myTable;
        }
        public DataTable AlertsSetup(string code, string name, int inactive)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@Code", code ?? ""),
                new SqlParameter("@Name", name ?? ""),
                new SqlParameter("@Inactive", inactive)
            };

            DataTable myTable = DataTableHelper.getTableData("spFrmAlertsSetupSearch", param);
            return myTable;
        }
        public DataTable Comment(string code, string name, int inactive)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@Code", code ?? ""),
                new SqlParameter("@Name", name ?? ""),
                new SqlParameter("@Inactive", inactive)
            };

            DataTable myTable = DataTableHelper.getTableData("spFrmCommentSearch", param);
            return myTable;
        }
        public DataTable CommentType(string code, string name, int inactive)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@Code", code ?? ""),
                new SqlParameter("@Name", name ?? ""),
                new SqlParameter("@Inactive", inactive)
            };

            DataTable myTable = DataTableHelper.getTableData("spFrmCommentTypeSearch", param);
            return myTable;
        }
        public DataTable Season(string code, string name, int inactive)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@Code", code ?? ""),
                new SqlParameter("@Name", name ?? ""),
                new SqlParameter("@Inactive", inactive)
            };

            DataTable myTable = DataTableHelper.getTableData("spFrmSeasonSearch", param);
            return myTable;
        }
        public DataTable Zone(string code, string name, int inactive)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@Code", code ?? ""),
                new SqlParameter("@Name", name ?? ""),
                new SqlParameter("@Inactive", inactive)
            };

            DataTable myTable = DataTableHelper.getTableData("spFrmZoneSearch", param);
            return myTable;
        }
        public DataTable Department(string code, string name, int inactive)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@Code", code ?? ""),
                new SqlParameter("@Name", name ?? ""),
                new SqlParameter("@Inactive", inactive)
            };

            DataTable myTable = DataTableHelper.getTableData("spFrmDepartmentSearch", param);
            return myTable;
        }
        public DataTable Owner(string code, string name, int inactive)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@Code", code ?? ""),
                new SqlParameter("@Name", name ?? ""),
                new SqlParameter("@Inactive", inactive)
            };

            DataTable myTable = DataTableHelper.getTableData("spFrmOwnerSearch", param);
            return myTable;
        }
        public DataTable PropertyType(string code, string description, int sequence)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@Code", code ?? ""),
                new SqlParameter("@Description", description ?? ""),
                new SqlParameter("@Sequence", sequence)
            };

            DataTable myTable = DataTableHelper.getTableData("spPropertyTypeSearch", param);
            return myTable;
        }
        public DataTable ReservationType()
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@sqlCommand",
                    @"SELECT Code,
                             Name,
                             CASE WHEN Deduct = 1 THEN 'x' ELSE '' END AS Deduct,
                             CASE WHEN ArrivalTimeRequired = 1 THEN 'x' ELSE '' END AS ArrivalTimeRequired,
                             CASE WHEN CreditCardRequired = 1 THEN 'x' ELSE '' END AS CreditCardRequired,
                             CASE WHEN DepositRequired = 1 THEN 'x' ELSE '' END AS DepositRequired,
                             CASE WHEN Inactive = 1 THEN 'x' ELSE '' END AS Inactive,
                             Sequence,
                             ID
                      FROM ReservationType WITH (NOLOCK)
                      ORDER BY Sequence ASC")
                    };

            DataTable myTable = DataTableHelper.getTableData("spSearchAllForTrans", param);
            return myTable;
        }
        public DataTable PackageForecastGroup(string code, string name, int inactive)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@Code", code ?? ""),
                new SqlParameter("@Name", name ?? ""),
                new SqlParameter("@Inactive", inactive)
            };

            DataTable myTable = DataTableHelper.getTableData("spFrmPackageForecastGroupSearch", param);
            return myTable;
        }
        public DataTable PreferenceGroup(string code, string name, int inactive)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@Code", code ?? ""),
                new SqlParameter("@Name", name ?? ""),
                new SqlParameter("@Inactive", inactive)
            };

            DataTable myTable = DataTableHelper.getTableData("spFrmPreferenceGroupSearch", param);
            return myTable;
        }
        public List<CurrencyModel> Currency(
            string? ID,
            bool IsShow = false,
            bool Inactive = false,
            bool IsMaster = false)
        {
            try
            {
                var list = CurrencyBO.Instance.FindAll(); // ArrayList

                var result = PropertyUtils
                    .ConvertToList<CurrencyModel>(list)
                    ?.Where(x =>
                        // ID filter: chỉ lọc khi ID có giá trị
                        (string.IsNullOrWhiteSpace(ID) || x.ID == ID.Trim()) &&

                        // bool filters
                        x.IsShow == IsShow &&
                        x.Inactive == Inactive &&
                        x.MasterStatus == IsMaster
                    )
                    .ToList();

                return result ?? [];
            }
            catch (Exception ex)
            {
                throw new Exception($"ERROR Currency(): {ex.Message}", ex);
            }
        }

        public List<CurrencyModel> GetAllCurrency()
        {
            try
            {
                string sql = @"
                    SELECT *
                    FROM Currency
                    WHERE IsShow = 1 AND Inactive = 0
                    ORDER BY ID
                ";

                DataTable dataTable = TextUtils.Select(sql.ToString());
                // Chuyển DataTable sang List<CurrencyModel>
                var currencies = (from d in dataTable.AsEnumerable()
                                  select new CurrencyModel
                                  {
                                      ID = d["ID"]?.ToString() ?? string.Empty,
                                      Description = d["Description"]?.ToString() ?? string.Empty,
                                      MasterStatus = d["MasterStatus"] != DBNull.Value ? Convert.ToBoolean(d["MasterStatus"]) : false,
                                      UserInsertID = d["UserInsertID"] != DBNull.Value ? Convert.ToInt32(d["UserInsertID"]) : 0,
                                      CreateDate = d["CreateDate"] != DBNull.Value ? Convert.ToDateTime(d["CreateDate"]) : DateTime.MinValue,
                                      UpdateDate = d["UpdateDate"] != DBNull.Value ? Convert.ToDateTime(d["UpdateDate"]) : DateTime.MinValue,
                                      UserUpdateID = d["UserUpdateID"] != DBNull.Value ? Convert.ToInt32(d["UserUpdateID"]) : 0,
                                      TransactionCode = d["TransactionCode"]?.ToString() ?? string.Empty,
                                      IsShow = d["IsShow"] != DBNull.Value ? Convert.ToBoolean(d["IsShow"]) : false,
                                      Inactive = d["Inactive"] != DBNull.Value ? Convert.ToBoolean(d["Inactive"]) : false,
                                      Decimals = d["Decimals"] != DBNull.Value ? Convert.ToInt32(d["Decimals"]) : 0,
                                      IsSynchronous = d["IsSynchronous"] != DBNull.Value ? Convert.ToBoolean(d["IsSynchronous"]) : false
                                  }).ToList();
                return currencies;
            }
            catch (Exception ex)
            {
                throw new Exception($"ERROR: {ex.Message}", ex);
            }
        }

        public DataTable hkpEmployee(string code, string name, int inactive)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@Code", code ?? ""),
                new SqlParameter("@Name", name ?? ""),
                new SqlParameter("@Inactive", inactive)
            };

            DataTable myTable = DataTableHelper.getTableData("spFrmhkpEmployeeSearch", param);
            return myTable;
        }
        public DataTable Property()
        {
            SqlParameter[] param = new SqlParameter[]
            {
        new SqlParameter("@sqlCommand",
            @"select a.*,b.Code as PropertyType from Property a with(nolock) join PropertyType b with(nolock) on a.PropertyTypeID=b.ID")
            };

            DataTable myTable = DataTableHelper.getTableData("spSearchAllForTrans", param);
            return myTable;
        }
        public DataTable PropertyPermission(string userID)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@UserID", userID ?? "")
            };

            DataTable myTable = DataTableHelper.getTableData("spPropertyPermissionSearch", param);
            return myTable;
        }
        public DataTable StatusList()
        {
            SqlParameter[] param = new SqlParameter[] { };

            DataTable myTable = DataTableHelper.getTableData("spHKPSelectStatus", param);
            return myTable;
        }
        public DataTable ConfigSystem()
        {
            SqlParameter[] param = new SqlParameter[]
            {
        new SqlParameter("@sqlCommand",
            @"select Desciption From ConfigSystem Where KeyName = ''Msg''")
            };

            DataTable myTable = DataTableHelper.getTableData("spSearchAllForTrans", param);
            return myTable;
        }
        public DataTable Member(DateTime fromDate, DateTime toDate, string status, string memberID, int isSortByCardName)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@FromDate", fromDate),
                new SqlParameter("@ToDate", toDate),
                new SqlParameter("@Status", status),
                new SqlParameter("@MemberID", memberID),
                new SqlParameter("@IsSortByCardName", isSortByCardName)
            };

            DataTable myTable = DataTableHelper.getTableData("spRptMember", param);
            return myTable;
        }
        public DataTable PostingHistory(DateTime fromDate, DateTime toDate, string fromFolioID, string toFolioID, string actionType, string user)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@FromDate", fromDate),
                new SqlParameter("@ToDate", toDate),
                new SqlParameter("@FromFolioID", fromFolioID ?? ""),
                new SqlParameter("@ToFolioID", toFolioID ?? ""),
                new SqlParameter("@ActionType", actionType ?? ""),
                new SqlParameter("@User", user ?? "")
            };

            DataTable myTable = DataTableHelper.getTableData("spSearchPostingHistoryGeneral", param);
            return myTable;
        }

        public DataTable PersonInChargeData(string code, string description, string group, string zone, string isActive)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@Code", code),
                new SqlParameter("@Name", description),
                new SqlParameter("@GroupID", group),
                new SqlParameter("@ZoneID",zone),
                new SqlParameter("@Inactive", isActive)
            };

            DataTable myTable = DataTableHelper.getTableData("spPersonInChargeSearch", param);
            return myTable;
        }
        public DataTable PersonInChargeGroupData(string code, string name, string isActive)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@Code", code),
                new SqlParameter("@Name", name),
                new SqlParameter("@Inactive", isActive)
            };

            DataTable myTable = DataTableHelper.getTableData("spFrmPersonInChargeGroupSearch", param);
            return myTable;
        }
        public DataTable PersonInChargeZoneData(string code, string description, string isActive)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@Code", code),
                new SqlParameter("@Name", description),
                new SqlParameter("@Inactive", isActive)
            };

            DataTable myTable = DataTableHelper.getTableData("spFrmPersonInChargeZoneSearch", param);
            return myTable;
        }
        public DataTable ApproveListData(string code, string description, string isActive)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@Code", code),
                new SqlParameter("@Name", description),
                new SqlParameter("@Inactive", isActive)
            };

            DataTable myTable = DataTableHelper.getTableData("spFrmApprovedbySearch", param);
            return myTable;
        }

        public DataTable RateCategory(string code, string name, int inactive)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Code", code ?? ""),
                new SqlParameter("@Name", name ?? ""),
                new SqlParameter("@Inactive", inactive),
            };

            DataTable dt = DataTableHelper.getTableData("spFrmRateCategorySearch", parameters);
            return dt;
        }
        public DataTable DepositRule(string code, string description)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Code", code ?? ""),
                new SqlParameter("@Description", description ?? ""),
            };

            DataTable dt = DataTableHelper.getTableData("spSearchDepositRule", parameters);
            return dt;
        }
        public DataTable CancellationRule(string code, string description)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Code", code ?? ""),
                new SqlParameter("@Description", description ?? ""),
            };

            DataTable dt = DataTableHelper.getTableData("spSearchCancellationRule", parameters);
            return dt;
        }
    }
}

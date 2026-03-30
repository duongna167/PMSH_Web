using BaseBusiness.BO;
using BaseBusiness.Model;
using BaseBusiness.util;
using DevExpress.CodeParser;
using DevExpress.DataAccess.DataFederation;
using DevExpress.XtraReports.Serialization;
using DevExpress.XtraRichEdit.Import.Doc;
using Microsoft.AspNetCore.Server.IISIntegration;
using Microsoft.Data.SqlClient;
using Reservation.Dto;
using Reservation.Services.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using static Reservation.Dto.ReservationPackageDTO;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Expression = BaseBusiness.util.Expression;

namespace Reservation.Services.Implements
{
    public class ReservationService : IReservationService
    {

        public DataTable ActivityLogOverbooking(string sqlCommand)
        {
            try
            {
                SqlParameter[] param = new SqlParameter[]
                {
                    new SqlParameter("@sqlCommand", sqlCommand),

                };

                DataTable myTable = DataTableHelper.getTableData("spSearchzAllForTrans", param);
                return myTable;
            }
            catch (SqlException ex)
            {

                throw new Exception($"ERROR: {ex.Message}", ex);
            }
        }

        public (decimal price, decimal priceAfter, decimal priceDiscount, decimal priceAfterDiscount) CalculateNet(decimal Price, string TransactionCode, decimal DiscountAmount, decimal DiscountPercent)
        {
            try
            {
                decimal svc = 0;
                decimal vat = 0;
                decimal room = 0;
                decimal priceDiscount = 0;
                decimal priceAfterDiscount = 0;
                List<GenerateTransactionModel> generateTransactionModels = PropertyUtils.ConvertToList<GenerateTransactionModel>(GenerateTransactionBO.Instance.FindAll()).
                Where(x => x.TransactionCode == TransactionCode).ToList();
                #region lấy ra phần trăm giá room, svc và vat
                if (generateTransactionModels.Count > 0)
                {
                    foreach (var item in generateTransactionModels)
                    {
                        if (item.SubgroupCode == "RR")
                        {
                            room = item.Percentage;
                        }
                        if (item.SubgroupCode == "SVC")
                        {
                            svc = item.Percentage;
                        }
                        if (item.SubgroupCode == "Tax")
                        {
                            vat = item.Percentage;
                        }
                    }
                }
                #endregion

                #region tính giá trị net
                decimal priceAfter = (Price + (Price * svc / 100) + (Price + (Price * svc / 100)) * vat / 100);
                #endregion

                #region tính giá trị rate code và net sau discount percent
                priceDiscount = Price - (DiscountPercent / 100) * Price;
                priceAfterDiscount = priceAfter - (DiscountPercent / 100) * priceAfter;
                #endregion

                #region tính giá trị rate code và net sau discount amount
                priceAfterDiscount = priceAfterDiscount - DiscountAmount;
                priceDiscount = (priceAfterDiscount / (1 + vat / 100)) / (1 + svc / 100);
                #endregion
                return (Price, priceAfter, priceDiscount, priceAfterDiscount);
            }
            catch (SqlException ex)
            {

                throw new Exception($"Error: {ex.Message}", ex);
            }
        }

        public (decimal Price, decimal priceAfter, decimal priceDiscount, decimal priceAfterDiscount) CalculateNetReverse(decimal priceAfterDiscount, string TransactionCode, decimal DiscountAmount, decimal DiscountPercent)
        {
            try
            {
                decimal svc = 0;
                decimal vat = 0;
                decimal room = 0;
                decimal Price = 0;
                decimal priceAfter = 0;
                decimal priceDiscount = 0;

                // Retrieve percentages for room, service charge, and VAT
                List<GenerateTransactionModel> generateTransactionModels = PropertyUtils.ConvertToList<GenerateTransactionModel>(GenerateTransactionBO.Instance.FindAll())
                    .Where(x => x.TransactionCode == TransactionCode).ToList();

                #region Retrieve percentages for room, svc, and vat
                if (generateTransactionModels.Count > 0)
                {
                    foreach (var item in generateTransactionModels)
                    {
                        if (item.SubgroupCode == "RR")
                        {
                            room = item.Percentage;
                        }
                        if (item.SubgroupCode == "SVC")
                        {
                            svc = item.Percentage;
                        }
                        if (item.SubgroupCode == "Tax")
                        {
                            vat = item.Percentage;
                        }
                    }
                }
                #endregion

                #region Calculate original Price by reversing the calculations
                // Step 1: Reverse the discount amount
                priceAfter = priceAfterDiscount + DiscountAmount;

                // Step 2: Reverse the discount percent
                if (DiscountPercent != 100) // Avoid division by zero
                {
                    priceAfter = priceAfter / (1 - DiscountPercent / 100);
                }
                else
                {
                    throw new Exception("DiscountPercent of 100% is invalid for reverse calculation.");
                }

                // Step 3: Reverse the service charge and VAT to get base Price
                Price = priceAfter / ((1 + vat / 100) * (1 + svc / 100));

                // Step 4: Recalculate forward to get priceDiscount
                priceDiscount = Price * (1 - DiscountPercent / 100);
                #endregion

                return (Price, priceAfter, priceDiscount, priceAfterDiscount);
            }
            catch (SqlException ex)
            {
                throw new Exception($"Error: {ex.Message}", ex);
            }
        }
        public decimal CalculateNetFixedCharge(string transactionCode, decimal price)
        {
            try
            {
                decimal svc = 0;
                decimal vat = 0;
                decimal room = 0;
                List<GenerateTransactionModel> generateTransactionModels = PropertyUtils.ConvertToList<GenerateTransactionModel>(GenerateTransactionBO.Instance.FindAll()).
                Where(x => x.TransactionCode == transactionCode).ToList();
                #region lấy ra phần trăm giá room, svc và vat
                if (generateTransactionModels.Count > 0)
                {
                    foreach (var item in generateTransactionModels)
                    {
                        if (item.SubgroupCode == "R_MISC")
                        {
                            room = item.Percentage;
                        }
                        if (item.SubgroupCode == "SVC")
                        {
                            svc = item.Percentage;
                        }
                        if (item.SubgroupCode == "Tax")
                        {
                            vat = item.Percentage;
                        }
                    }
                }
                #endregion

                #region tính giá trị net fiexed charge
                decimal priceAfter = (price + (price * svc / 100) + (price + (price * svc / 100)) * vat / 100);
                #endregion

                return priceAfter;
            }
            catch (SqlException ex)
            {

                throw new Exception($"Error: {ex.Message}", ex);
            }
        }




        /// <summary>
        /// DatVP: Lấy danh sách allotment từ store procedure
        /// </summary>
        /// <param name="code">code</param>
        /// <param name="marketID">id market</param>
        /// <param name="profileID">id profile</param>
        /// <param name="isDefault">isDefault</param>
        /// <param name="allotmentTypeID">id allotmentType</param
        /// <returns>Data table chứa danh sách allotment</returns>
        public DataTable GetAllotment(string code, string marketID, string profileID, string isDefault, string zone, string allotmentTypeID)
        {
            try
            {
                SqlParameter[] param = new SqlParameter[]
                {
                    new SqlParameter("@Code", code),
                    new SqlParameter("@MarketID", marketID),
                    new SqlParameter("@AllotmentTypeID", allotmentTypeID),
                    new SqlParameter("@ProfileID", profileID),
                    new SqlParameter("@IsDefault", isDefault),
                    new SqlParameter("@Zone", zone),

                };

                DataTable myTable = DataTableHelper.getTableData("spAllotmentSearch", param);
                return myTable;
            }
            catch (SqlException ex)
            {

                throw new Exception($"ERROR: {ex.Message}", ex);
            }

        }

        /// <summary>
        /// DatVP: Lây danh sách allotment detail theo allotment id từ store procedure
        /// </summary>
        /// <param name="allotmentID">id allotment</param>
        /// <param name="roomType">room type</param>
        /// <param name="showHistory">ngày xem</param>
        /// <returns>Data table chứa thông tin chi tiết của allotment</returns>
        public DataTable GetAllotmentDetail(int allotmentID, string roomType, DateTime showHistory)
        {
            try
            {
                SqlParameter[] param = new SqlParameter[]
                {
                    new SqlParameter("@AllotmentID", allotmentID),
                    new SqlParameter("@RoomType", roomType),
                    new SqlParameter("@ShowHistory", showHistory)
,

                };

                DataTable myTable = DataTableHelper.getTableData("spAllotmentDetailSearch_Temp", param);
                return myTable;
            }
            catch (SqlException ex)
            {

                throw new Exception($"ERROR: {ex.Message}", ex);
            }

        }

        public string GetConfigETA()
        {
            try
            {
                return ConfigSystemBO.GetConfigETA();
            }
            catch (SqlException ex)
            {

                throw new Exception($"ERROR: {ex.Message}", ex);
            }
        }

        public string GetConfigETD()
        {
            try
            {
                return ConfigSystemBO.GetConfigETD();
            }
            catch (SqlException ex)
            {

                throw new Exception($"ERROR: {ex.Message}", ex);
            }
        }

        public DataTable GetRateCode(DateTime arrival, DateTime departure, int adults, int roomType)
        {
            try
            {
                SqlParameter[] param = new SqlParameter[]
                {
                    new SqlParameter("@ArrivalDate", arrival),
                    new SqlParameter("@DepartureDate", departure),
                    new SqlParameter("@Adults", adults),
                    new SqlParameter("@RoomType", roomType),
                };

                DataTable myTable = DataTableHelper.getTableData("Web_GetRateCode", param);
                return myTable;
            }
            catch (SqlException ex)
            {

                throw new Exception($"Lỗi cơ sở dữ liệu khi lấy RateCode: {ex.Message}", ex);
            }

        }

        /// <summary>
        /// DatVP: Lây danh sách reservation preference từ store procedure
        /// </summary>
        /// <param name="code">id allotment</param>
        /// <param name="group">room type</param>
        /// <returns>Data table chứa danh sách preference</returns>
        public DataTable GetReservationPreference(string code, int group)
        {
            try
            {
                SqlParameter[] param = new SqlParameter[]
                {
                    new SqlParameter("@Code", code),
                    new SqlParameter("@Group", group),


                };

                DataTable myTable = DataTableHelper.getTableData("spReservationPreference", param);
                return myTable;
            }
            catch (SqlException ex)
            {

                throw new Exception($"ERROR: {ex.Message}", ex);
            }

        }

        /// <summary>
        /// DatVP: Lây danh sách room available từ store procedure
        /// </summary>
        /// <returns>Data table chứa danh sách room available</returns>
        public DataTable GetRoomAvailable(DateTime fromDate, DateTime toDate, string floor, string roomTypeID, string smoking, string foStatus, string hkStatus, string isDummy, string roomNo, int roomID, int Type)
        {
            try
            {
                SqlParameter[] param = new SqlParameter[]
                {
                    new SqlParameter("@FromDate", fromDate),
                    new SqlParameter("@ToDate", toDate),
                    new SqlParameter("@Floor", floor),
                    new SqlParameter("@RoomTypeID", roomTypeID),
                    new SqlParameter("@Smoking", smoking),
                    new SqlParameter("@FOStatus", foStatus),
                    new SqlParameter("@HKStatusID", hkStatus),
                    new SqlParameter("@IsDummy", isDummy),
                    new SqlParameter("@RoomNo", roomNo),
                    new SqlParameter("@RoomID", roomID),
                    new SqlParameter("@Type", Type),
                };

                DataTable myTable = DataTableHelper.getTableData("spAvailableRoomsSearch", param);
                return myTable;
            }
            catch (SqlException ex)
            {

                throw new Exception($"Error: {ex.Message}", ex);
            }

        }


        public DataTable ReservationGetRateQueryDetail(DateTime fromDate, DateTime toDate, int rateCodeID, int roomType, string currency, int packageID, int day)
        {
            try
            {
                SqlParameter[] param = new SqlParameter[]
                {
                    new SqlParameter("@ArrivalDate", fromDate),
                    new SqlParameter("@DepartureDate", toDate),
                    new SqlParameter("@RateCodeID", rateCodeID),

                    new SqlParameter("@RoomTypeID", roomType),
                    new SqlParameter("@CurrencyID", currency),
                    new SqlParameter("@PackageID", packageID),
                    new SqlParameter("@Day", day),

                };

                DataTable myTable = DataTableHelper.getTableData("spReservationGetRateQuery", param);
                return myTable;
            }
            catch (SqlException ex)
            {

                throw new Exception($"Error: {ex.Message}", ex);
            }
        }

        public DataTable ReservationRateQueryDetail(DateTime fromDate, DateTime toDate, int roomType, int adults, int noOfNight,
            int packageID, int promotionID, string tableName, string onRows, string onRowsAlias, string onCols, string sumcol,
            int func, string currency, int display, int dayUse, int c1, int c2, int c3, int noOfRoom)
        {
            try
            {
                SqlParameter[] param = new SqlParameter[]
                {
                    new SqlParameter("@fromDate", fromDate),
                    new SqlParameter("@toDate", toDate),
                    new SqlParameter("@roomType", roomType),
                    new SqlParameter("@adults", adults),
                    new SqlParameter("@NoOfNight", noOfNight),
                    new SqlParameter("@PackageID", packageID),
                    new SqlParameter("@PromotionID", promotionID),
                    new SqlParameter("@table", tableName),
                    new SqlParameter("@onrows", onRows),
                    new SqlParameter("@onrowsalias", onRowsAlias),
                    new SqlParameter("@oncols", onCols),
                    new SqlParameter("@sumcol", sumcol),
                    new SqlParameter("@func", func),
                    new SqlParameter("@currency", currency),
                    new SqlParameter("@display", display),
                    new SqlParameter("@dayuse", dayUse),
                    new SqlParameter("@c1", c1),
                    new SqlParameter("@c2", c2),
                    new SqlParameter("@c3", c3),
                    new SqlParameter("@NoOfRoom", noOfRoom),
                };

                DataTable myTable = DataTableHelper.getTableData("spReservationRateQueryDetail", param);
                return myTable;
            }
            catch (SqlException ex)
            {

                throw new Exception($"ERROR: {ex.Message}", ex);
            }

        }

        public DataTable SearchOverBooking(string sqlCommand)
        {

            try
            {
                SqlParameter[] param = new SqlParameter[]
                {
                    new SqlParameter("@sqlCommand", sqlCommand),

                };

                DataTable myTable = DataTableHelper.getTableData("spSearchAllForTrans", param);
                return myTable;
            }
            catch (SqlException ex)
            {

                throw new Exception($"ERROR: {ex.Message}", ex);
            }
        }

        public DataTable SearchReservation(int searchType, string name, string firstName, string reservationHolder, string confirmationNo,
            string crsNo, string roomNo, string roomType, string package, string zone, string arrivalFrom, string arrivalTo, string roomSharer, string owner)
        {
            try
            {

                SqlParameter[] param = new SqlParameter[]
                {
                    new SqlParameter("@SearchType", searchType),
                    new SqlParameter("@Name", name ?? ""),
                    new SqlParameter("@FirstName", firstName ?? ""),
                    new SqlParameter("@ReservationHolder", reservationHolder ?? ""),
                    new SqlParameter("@ConfirmationNo", confirmationNo ?? ""),
                    new SqlParameter("@CRSNo", crsNo ?? ""),
                    new SqlParameter("@RoomNo", roomNo ?? ""),
                    new SqlParameter("@RoomType", roomType ?? ""),
                    new SqlParameter("@Package", package ?? ""),
                    new SqlParameter("@Zone", zone ?? ""),

                    new SqlParameter("@ArrivalFrom", searchType != 1 ? arrivalFrom : ""),
                    new SqlParameter("@ArrivalTo", searchType !=1 ? arrivalTo: ""),
                    new SqlParameter("@RoomSharer", roomSharer),
                    new SqlParameter("@CreateDate", ""),
                    new SqlParameter("@CreateBy", ""),
                    new SqlParameter("@Departure", ""),
                    new SqlParameter("@StayOn", ""),
                    new SqlParameter("@Market",""),
                    new SqlParameter("@Source",""),
                    new SqlParameter("@ReservationType", ""),
                    new SqlParameter("@MemberType", ""),
                    new SqlParameter("@ARNo",""),
                    new SqlParameter("@BusinessBlock", ""),
                    new SqlParameter("@VIP", ""),
                    new SqlParameter("@ChkVIPOnly", ""),
                    new SqlParameter("@MasterFolio", ""),
                    new SqlParameter("@SpecialUpdatedDate", ""),
                    new SqlParameter("@SaleInChagre", ""),
                    new SqlParameter("@RateCode", ""),
                    new SqlParameter("@IsTransfer", ""),
                    new SqlParameter("@VoucherNo",  ""),
                    new SqlParameter("@Owner", owner ?? "")
                };

                DataTable myTable = DataTableHelper.getTableData("spReservationSearch", param);
                return myTable;
            }
            catch (SqlException ex)
            {
                throw new Exception($"ERROR: {ex.Message}", ex);


            }
        }

        public DataTable SearchWaitlist(string name, string priority, string market, string roomType, string reason, string rateCode, string phone, DateTime date)
        {
            try
            {

                SqlParameter[] param = new SqlParameter[]
                {
                    new SqlParameter("@Name", name ?? ""),
                    new SqlParameter("@Priority", priority ?? ""),
                    new SqlParameter("@Market", market ?? ""),
                    new SqlParameter("@RoomType", roomType ?? ""),
                    new SqlParameter("@Reason", reason ?? ""),
                    new SqlParameter("@RateCode", rateCode ?? ""),
                    new SqlParameter("@Phone", phone ?? ""),
                    new SqlParameter("@Date", date),

                };

                DataTable myTable = DataTableHelper.getTableData("spReservationWaitList", param);
                return myTable;
            }
            catch (SqlException ex)
            {
                throw new Exception($"ERROR: {ex.Message}", ex);


            }
        }

        public DataTable SearchReservationAlerts(int reservationID)
        {
            try
            {
                SqlParameter[] param = new SqlParameter[]
                {
                    new SqlParameter("@ReservationID", reservationID),

                };

                DataTable myTable = DataTableHelper.getTableData("spReservationAlerts", param);
                return myTable;
            }
            catch (SqlException ex)
            {

                throw new Exception($"ERROR: {ex.Message}", ex);
            }
        }

        public DataTable SearchTrace(string departmentID, string resolved, DateTime date, string name, string reservationID)
        {
            try
            {
                SqlParameter[] param = new SqlParameter[]
                {
                    new SqlParameter("@Department", departmentID),
                    new SqlParameter("@Resolved", resolved),
                    new SqlParameter("@Date", date),
                    new SqlParameter("@Name", name),
                    new SqlParameter("@ReservationID", reservationID),


                };

                DataTable myTable = DataTableHelper.getTableData("spReservationTracesSearch", param);
                return myTable;
            }
            catch (SqlException ex)
            {

                throw new Exception($"ERROR: {ex.Message}", ex);
            }
        }

        public DataTable ReservationAutoRoomAssignment(int type, string roomType, string roomClass, string smoking, string floor,
            string startFromRoom, DateTime arrivalDate, DateTime departureDate, string hkStatusID, string confirmationNo, string rsvRoomTypeID, string notAssRoomNo)
        {
            try
            {
                SqlParameter[] param = new SqlParameter[]
                {
                    new SqlParameter("@Type", type),
                    new SqlParameter("@RoomType", roomType),
                    new SqlParameter("@RoomClass", roomClass),
                    new SqlParameter("@Smocking", smoking),
                    new SqlParameter("@Floor", floor),
                    new SqlParameter("@StartFromRoom", startFromRoom),
                    new SqlParameter("@ArrivalDate", arrivalDate),
                    new SqlParameter("@DepartureDate", departureDate),
                    new SqlParameter("@HKStatusID", hkStatusID),
                    new SqlParameter("@ConfirmationNo", confirmationNo),
                    new SqlParameter("@RsvRoomTypeID", rsvRoomTypeID),
                    new SqlParameter("@NotAssRoomNo", notAssRoomNo),
                };

                DataTable myTable = DataTableHelper.getTableData("spReservationAutoRoomAssignment", param);
                return myTable;
            }
            catch (SqlException ex)
            {

                throw new Exception($"ERROR: {ex.Message}", ex);
            }
        }
        public DataTable SearchReservationPackages(
            int reservationID,
            int packageID,
            int type,
            DateTime beginDate,
            DateTime endDate,
            int rateCodeID)
        {
            try
            {
                SqlParameter[] param =
                [
                    new SqlParameter("@ReservationID", reservationID),
                    new SqlParameter("@PackageID", packageID),
                    new SqlParameter("@Type", type),
                    new SqlParameter("@BeginDate", beginDate),
                    new SqlParameter("@EndDate", endDate),
                    new SqlParameter("@RateCodeID", rateCodeID)
                ];

                return DataTableHelper.getTableData("spReservationPackage", param);
            }
            catch (SqlException ex)
            {
                throw new Exception($"ERROR spReservationPackage: {ex.Message}", ex);
            }
        }
        public List<ReservationPackageSummary> GetReservationPackagePhase2(
            int reservationId,
            int packageId,
            DateTime beginDate,
            DateTime endDate,
            int rateCodeId)
        {
            try
            {
                // 1. Tạo câu lệnh SQL Dynamic dùng linh hoạt hơn không phụ thuộc vào type
                // Lưu ý: Format ngày tháng thành 'yyyy-MM-dd' để SQL hiểu chính xác
                string sqlQuery = string.Format(@"
                    SELECT 
                        a.PackageID,
                        ISNULL(d.RateCode, '') AS RateCode, 
                        b.Code AS Package, 
                        b.Description,
                        a.Quantity AS Qty, 
                        a.Price AS TotalPrice, 
                        a.PriceAfterTax AS TotalPriceAfterTax, 
                        a.CurrencyID,
                        a.BeginDate, 
                        a.EndDate,
                        a.ReservationID, 
                        CASE WHEN a.Excluded = 1 THEN 'x' ELSE '' END AS Excl,
                        a.RateCodeID,
                        a.IsTaxInclude,
                        a.TransactionCode,
                        a.CalculationRuleID,
                        a.PostingRhythmID,
                        a.PostingDay,
                        c.NoOfAdult, c.NoOfChild, c.NoOfChild1, c.NoOfChild2,
                        DATEDIFF(day, a.BeginDate, a.EndDate) AS Night
                    FROM dbo.ReservationPackage a WITH (NOLOCK)
                    INNER JOIN dbo.Package b WITH (NOLOCK) ON a.PackageID = b.ID
                    INNER JOIN dbo.Reservation c WITH (NOLOCK) ON a.ReservationID = c.ID
                    LEFT JOIN dbo.RateCode d WITH (NOLOCK) ON a.RateCodeID = d.ID
                    WHERE 
                        a.ReservationID = {0}
                        AND b.ID = {1}
                        AND a.RateCodeID = {2}
                        AND DATEDIFF(day, a.BeginDate, '{3}') = 0
                        AND DATEDIFF(day, a.EndDate, '{4}') = 0
                    ",
                    reservationId,
                    packageId,
                    rateCodeId,
                    beginDate.ToString("yyyy-MM-dd"),
                    endDate.ToString("yyyy-MM-dd")
                );

                // 2. Chuẩn bị tham số cho spSearchAllForTrans
                SqlParameter[] parameters = [new SqlParameter("@sqlCommand", sqlQuery)];

                // 3. Gọi DB qua hàm getTableData có sẵn
                // Giả sử class chứa getTableData tên là DaoHelper
                DataTable dataTable = DataTableHelper.getTableData("spSearchAllForTrans", parameters);

                // 4. Map dữ liệu sang List object (Code của bạn)
                if (dataTable == null || dataTable.Rows.Count == 0)
                    return new List<ReservationPackageSummary>();

                var result = (from d in dataTable.AsEnumerable()
                              select new ReservationPackageSummary
                              {
                                  PackageID = d.Field<int>("PackageID"),
                                  RateCode = d["RateCode"]?.ToString() ?? string.Empty,
                                  Package = d["Package"]?.ToString() ?? string.Empty,
                                  Description = d["Description"]?.ToString() ?? string.Empty,

                                  Qty = d["Qty"] != DBNull.Value ? Convert.ToInt32(d["Qty"]) : 0,
                                  TotalPrice = d["TotalPrice"] != DBNull.Value ? Convert.ToDecimal(d["TotalPrice"]) : 0,
                                  TotalPriceAfterTax = d["TotalPriceAfterTax"] != DBNull.Value ? Convert.ToDecimal(d["TotalPriceAfterTax"]) : 0,

                                  CurrencyID = d["CurrencyID"]?.ToString() ?? string.Empty,

                                  BeginDate = d["BeginDate"] != DBNull.Value ? Convert.ToDateTime(d["BeginDate"]) : DateTime.MinValue,
                                  EndDate = d["EndDate"] != DBNull.Value ? Convert.ToDateTime(d["EndDate"]) : DateTime.MinValue,

                                  ReservationID = d["ReservationID"] != DBNull.Value ? Convert.ToInt32(d["ReservationID"]) : 0,
                                  Excl = d["Excl"]?.ToString() ?? string.Empty,

                                  RateCodeID = d["RateCodeID"] != DBNull.Value ? Convert.ToInt32(d["RateCodeID"]) : 0,

                                  IsTaxInclude = d["IsTaxInclude"] != DBNull.Value && Convert.ToBoolean(d["IsTaxInclude"]),
                                  TransactionCode = d["TransactionCode"]?.ToString() ?? string.Empty,
                                  CalculationRuleID = d["CalculationRuleID"] != DBNull.Value ? Convert.ToInt32(d["CalculationRuleID"]) : 0,
                                  PostingRhythmID = d["PostingRhythmID"] != DBNull.Value ? Convert.ToInt32(d["PostingRhythmID"]) : 0,
                                  PostingDay = d["PostingDay"] != DBNull.Value ? Convert.ToDateTime(d["EndDate"]) : DateTime.MinValue,

                                  NoOfAdult = d["NoOfAdult"] != DBNull.Value ? Convert.ToInt32(d["NoOfAdult"]) : 0,
                                  NoOfChild = d["NoOfChild"] != DBNull.Value ? Convert.ToInt32(d["NoOfChild"]) : 0,
                                  NoOfChild1 = d["NoOfChild1"] != DBNull.Value ? Convert.ToInt32(d["NoOfChild1"]) : 0,
                                  NoOfChild2 = d["NoOfChild2"] != DBNull.Value ? Convert.ToInt32(d["NoOfChild2"]) : 0,

                                  Night = d["Night"] != DBNull.Value ? Convert.ToInt32(d["Night"]) : 0
                              }).ToList();

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"ERROR GetReservationPackagePhase2: {ex.Message}", ex);
            }
        }

        public DataTable GetSetUpPackage(string searchKey = "")
        {
            string filter = string.IsNullOrEmpty(searchKey) ? "%" : $"%{searchKey}%";
            string query = $@"Select * From Package where ((Code like N'{filter}' or Description like N'{filter}'))
                                And Active = 1 Order By Code";
            SqlParameter[] parameters = [
                new SqlParameter("sqlCommand",query)
            ];
            DataTable table = DataTableHelper.getTableData("spSearchAllForTrans", parameters);
            return table;

        }

        //C2 Get RoomRevenue no Transaction
        public static void GetRoomRevenue(int ReservationID)
        {
            #region 1.Khai báo biến 
            ReservationModel mR = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(ReservationID);
            DataTable dtCS;
            DataTable dtFC;
            DataTable dtPkg;
            DataTable dtPkgInc;
            DataTable dtRR;
            decimal Rate;
            //FC
            decimal AmountBeforTax = 0;
            decimal AmountAfterTax = 0;
            decimal RoomRevenueBeforeTax = 0;
            decimal RoomRevenueAfterTax = 0;
            //Pkg
            decimal Price;
            decimal OriginPrice;
            bool _RoIsTaxInclude;
            int FC;
            int PkgP = 0;
            int PkgC = 0;
            //code room revenue
            string _CodeRoomRevenue = "";
            #endregion

            #region 2.Nhặt ra code thuộc nhóm RoomRevenue 
            dtCS = TextUtils.Select("SELECT Code AS KeyValue FROM Transactions WITH (NOLOCK) WHERE TransactionGroupID = 6 ");
            for (int t = 0; t < dtCS.Rows.Count; t++)
            {
                if (_CodeRoomRevenue == "")
                    _CodeRoomRevenue = dtCS.Rows[t]["KeyValue"].ToString() ?? "";
                else
                    _CodeRoomRevenue = _CodeRoomRevenue + "," + dtCS.Rows[t]["KeyValue"].ToString();
            }
            #endregion

            #region 3.Nhặt RoomRevenue trong bảng FixedCharge 
            dtFC = TextUtils.Select("SELECT PostingRhythmID, CurrencyID, Amount, AmountAfterTax, Quantity, PostingDate, PostingDay FROM ReservationFixedCharge WITH (NOLOCK)" +
                                    "WHERE ReservationID = " + ReservationID + " " +
                                    "AND TransactionCode IN (" + _CodeRoomRevenue + ") ");
            #endregion

            #region 4.Nhặt RoomRevenue trong bảng ReservationPackage 
            dtPkg = TextUtils.Select("SELECT Price, PriceAfterTax, CurrencyID, Quantity, PostingDate, PostingDay, CalculationRuleID, PostingRhythmID FROM ReservationPackage WITH (NOLOCK)" +
                                     "WHERE ReservationID = " + ReservationID + " " +
                                     "AND TransactionCode IN (" + _CodeRoomRevenue + ") ");
            #endregion

            #region 5.Xác định tiền Package Include trong tiền phòng 
            dtPkgInc = TextUtils.Select("SELECT a.Quantity, a.Price,a.PriceAfterTax, a.CurrencyID, a.TransactionCode, a.CalculationRuleID, a.PostingRhythmID, PostingDate " +
                                        "FROM dbo.ReservationPackage a WITH (NOLOCK), dbo.Package b WITH (NOLOCK) " +
                                        "WHERE a.PackageID = b.ID " +
                                        "AND b.IncludedInRate = 1 " +
                                        "AND a.ReservationID = " + ReservationID + " ");
            #endregion

            #region 6.Process by date 
            dtRR = TextUtils.Select("SELECT ID, RateDate, Rate, RateAfterTax, IsTaxInclude, DiscountRate, DiscountAmount, TransactionCode, CurrencyID " +
                                    "FROM ReservationRate WITH (NOLOCK) " +
                                    "WHERE ReservationID = " + ReservationID + " ");
            for (int i = 0; i < dtRR.Rows.Count; i++)
            {
                #region 6.1.Xác định tiền phòng trong bảng ReservationRate 
                _RoIsTaxInclude = Convert.ToBoolean(dtRR.Rows[i]["IsTaxInclude"].ToString());
                //* NoOfRoom
                if (_RoIsTaxInclude == false)
                {
                    if (mR.NoOfRoom > 0)
                        Rate = TextUtils.ToDecimal(dtRR.Rows[i]["Rate"]?.ToString() ?? "0") * mR.NoOfRoom;
                    else
                        Rate = TextUtils.ToDecimal(dtRR.Rows[i]["Rate"].ToString() ?? "0");
                }
                else
                {
                    if (mR.NoOfRoom > 0)
                        Rate = TextUtils.ToDecimal(dtRR.Rows[i]["RateAfterTax"].ToString() ?? "0") * mR.NoOfRoom;
                    else
                        Rate = TextUtils.ToDecimal(dtRR.Rows[i]["RateAfterTax"].ToString() ?? "0");
                }
                //Xác định tiền phòng/ngày đã trừ Discount
                Rate = Rate - (Rate * TextUtils.ToDecimal(dtRR.Rows[i]["DiscountRate"].ToString() ?? "0") / 100)
                            - TextUtils.ToDecimal(dtRR.Rows[i]["DiscountAmount"].ToString() ?? "0") * mR.NoOfRoom;
                #endregion

                #region 6.2.Xác định tiền phòng trong bảng ReservationFixcharge 
                for (int k = 0; k < dtFC.Rows.Count; k++)
                {
                    FC = 0;

                    #region Xác định thời điểm charge 
                    switch (dtFC.Rows[k]["PostingRhythmID"].ToString())
                    {
                        case "1"://Everyday
                            {
                                FC = 1;
                                break;
                            }
                        case "2"://Date(At date)
                            {
                                FC = 2;
                                break;
                            }
                        case "3"://Day(At Day Of use)
                            {
                                FC = 3;
                                break;
                            }
                        case "4"://At Check in
                            {
                                FC = 4;
                                break;
                            }
                        case "5"://At Check Out
                            {
                                FC = 5;
                                break;
                            }
                    }
                    #endregion

                    #region Every day 
                    if (FC == 1)
                    {
                        if (dtRR.Rows[i]["CurrencyID"].ToString() == dtFC.Rows[k]["CurrencyID"].ToString())
                        {
                            if (_RoIsTaxInclude == false)
                                Rate = Rate + (TextUtils.ToDecimal(dtFC.Rows[k]["Amount"].ToString() ?? "0") * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString() ?? "0"));
                            else
                                Rate = Rate + (TextUtils.ToDecimal(dtFC.Rows[k]["AmountAfterTax"].ToString() ?? "0") * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString() ?? "0"));
                        }
                        else
                        {
                            if (_RoIsTaxInclude == false)
                                Rate = Rate + (TextUtils.ExchangeCurrency(mR.ReservationDate, dtFC.Rows[k]["CurrencyID"].ToString() ?? "", dtRR.Rows[i]["CurrencyID"].ToString() ?? "", TextUtils.ToDecimal(dtFC.Rows[k]["Amount"].ToString() ?? "0")) * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString() ?? "0"));
                            else
                                Rate = Rate + (TextUtils.ExchangeCurrency(mR.ReservationDate, dtFC.Rows[k]["CurrencyID"].ToString() ?? "", dtRR.Rows[i]["CurrencyID"].ToString() ?? "", TextUtils.ToDecimal(dtFC.Rows[k]["AmountAfterTax"].ToString() ?? "0")) * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString() ?? "0"));
                        }
                    }
                    #endregion

                    #region Date(At date) 
                    else if (FC == 2)
                    {
                        if (TextUtils.CompareDate(Convert.ToDateTime(dtRR.Rows[i]["RateDate"]), Convert.ToDateTime(dtFC.Rows[k]["PostingDate"])) == 0)
                            if (dtRR.Rows[i]["CurrencyID"].ToString() == dtFC.Rows[k]["CurrencyID"].ToString())
                            {
                                if (_RoIsTaxInclude == false)
                                    Rate = Rate + (TextUtils.ToDecimal(dtFC.Rows[k]["Amount"].ToString()) * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString()));
                                else
                                    Rate = Rate + (TextUtils.ToDecimal(dtFC.Rows[k]["AmountAfterTax"].ToString()) * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString()));
                            }
                            else
                            {
                                if (_RoIsTaxInclude == false)
                                    Rate = Rate + (TextUtils.ExchangeCurrency(mR.ReservationDate, dtFC.Rows[k]["CurrencyID"].ToString(), dtRR.Rows[i]["CurrencyID"].ToString(), TextUtils.ToDecimal(dtFC.Rows[k]["Amount"].ToString())) * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString()));
                                else
                                    Rate = Rate + (TextUtils.ExchangeCurrency(mR.ReservationDate, dtFC.Rows[k]["CurrencyID"].ToString(), dtRR.Rows[i]["CurrencyID"].ToString(), TextUtils.ToDecimal(dtFC.Rows[k]["AmountAfterTax"].ToString())) * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString()));
                            }
                    }
                    #endregion

                    #region Day(At Day Of use) 
                    else if (FC == 3)
                    {
                        string _Postingday = dtFC.Rows[k]["PostingDay"].ToString();
                        string[] _arrPD = _Postingday.Split(',');
                        for (int _pd = 0; _pd < _arrPD.Length; _pd++)
                        {
                            if (_arrPD[_pd] != "")
                            {
                                if (i == int.Parse(_arrPD[_pd].ToString()) - 1)
                                {
                                    if (dtRR.Rows[i]["CurrencyID"].ToString() == dtFC.Rows[k]["CurrencyID"].ToString())
                                    {
                                        if (_RoIsTaxInclude == false)
                                            Rate = Rate + (TextUtils.ToDecimal(dtFC.Rows[k]["Amount"].ToString()) * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString()));
                                        else
                                            Rate = Rate + (TextUtils.ToDecimal(dtFC.Rows[k]["AmountAfterTax"].ToString()) * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString()));
                                    }
                                    else
                                    {
                                        if (_RoIsTaxInclude == false)
                                            Rate = Rate + (TextUtils.ExchangeCurrency(mR.ReservationDate, dtFC.Rows[k]["CurrencyID"].ToString(), dtRR.Rows[i]["CurrencyID"].ToString(), TextUtils.ToDecimal(dtFC.Rows[k]["Amount"].ToString())) * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString()));
                                        else
                                            Rate = Rate + (TextUtils.ExchangeCurrency(mR.ReservationDate, dtFC.Rows[k]["CurrencyID"].ToString(), dtRR.Rows[i]["CurrencyID"].ToString(), TextUtils.ToDecimal(dtFC.Rows[k]["AmountAfterTax"].ToString())) * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString()));
                                    }
                                }
                            }
                        }
                    }
                    #endregion

                    #region At Check in 
                    else if (FC == 4)
                    {
                        if (i == 0)
                            if (dtRR.Rows[i]["CurrencyID"].ToString() == dtFC.Rows[k]["CurrencyID"].ToString())
                            {
                                if (_RoIsTaxInclude == false)
                                    Rate = Rate + (TextUtils.ToDecimal(dtFC.Rows[k]["Amount"].ToString()) * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString()));
                                else
                                    Rate = Rate + (TextUtils.ToDecimal(dtFC.Rows[k]["AmountAfterTax"].ToString()) * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString()));
                            }
                            else
                            {
                                if (_RoIsTaxInclude == false)
                                    Rate = Rate + (TextUtils.ExchangeCurrency(mR.ReservationDate, dtFC.Rows[k]["CurrencyID"].ToString(), dtRR.Rows[i]["CurrencyID"].ToString(), TextUtils.ToDecimal(dtFC.Rows[k]["Amount"].ToString())) * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString()));
                                else
                                    Rate = Rate + (TextUtils.ExchangeCurrency(mR.ReservationDate, dtFC.Rows[k]["CurrencyID"].ToString(), dtRR.Rows[i]["CurrencyID"].ToString(), TextUtils.ToDecimal(dtFC.Rows[k]["AmountAfterTax"].ToString())) * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString()));
                            }
                    }
                    #endregion

                    #region At Check Out 
                    else if (FC == 5)
                    {
                        if (i == dtRR.Rows.Count - 1)
                            if (dtRR.Rows[i]["CurrencyID"].ToString() == dtFC.Rows[k]["CurrencyID"].ToString())
                            {
                                if (_RoIsTaxInclude == false)
                                    Rate = Rate + (TextUtils.ToDecimal(dtFC.Rows[k]["Amount"].ToString()) * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString()));
                                else
                                    Rate = Rate + (TextUtils.ToDecimal(dtFC.Rows[k]["AmountAfterTax"].ToString()) * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString()));
                            }
                            else
                            {
                                if (_RoIsTaxInclude == false)
                                    Rate = Rate + (TextUtils.ExchangeCurrency(mR.ReservationDate, dtFC.Rows[k]["CurrencyID"].ToString(), dtRR.Rows[i]["CurrencyID"].ToString(), TextUtils.ToDecimal(dtFC.Rows[k]["Amount"].ToString())) * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString()));
                                else
                                    Rate = Rate + (TextUtils.ExchangeCurrency(mR.ReservationDate, dtFC.Rows[k]["CurrencyID"].ToString(), dtRR.Rows[i]["CurrencyID"].ToString(), TextUtils.ToDecimal(dtFC.Rows[k]["AmountAfterTax"].ToString())) * TextUtils.ToDecimal(dtFC.Rows[k]["Quantity"].ToString()));
                            }
                    }
                    #endregion                                              
                }
                #endregion

                #region 6.3.Nhặt ra tiền phòng trong bảng ReservationPackage 
                for (int l = 0; l < dtPkg.Rows.Count; l++)
                {
                    Price = 0;
                    //Xác định giá gốc * NoOfRoom                    
                    if (_RoIsTaxInclude == false)
                    {
                        if (mR.NoOfRoom > 0)
                            OriginPrice = Decimal.Parse(dtPkg.Rows[l]["Price"].ToString()) * mR.NoOfRoom;
                        else
                            OriginPrice = Decimal.Parse(dtPkg.Rows[l]["Price"].ToString());
                    }
                    else
                    {
                        if (mR.NoOfRoom > 0)
                            OriginPrice = Decimal.Parse(dtPkg.Rows[l]["PriceAfterTax"].ToString()) * mR.NoOfRoom;
                        else
                            OriginPrice = Decimal.Parse(dtPkg.Rows[l]["PriceAfterTax"].ToString());
                    }

                    #region Phương thức charge CanculationRule
                    switch (dtPkg.Rows[l]["CalculationRuleID"].ToString())
                    {
                        case "1"://Per/Person(từng người trong phòng)
                            {
                                PkgC = 1;
                                break;
                            }
                        case "2"://Per Adult
                            {
                                PkgC = 2;
                                break;
                            }
                        case "3"://Per Child
                            {
                                PkgC = 3;
                                break;
                            }
                        case "4"://Per Room
                            {
                                PkgC = 4;
                                break;
                            }
                        case "5"://Per C1
                            {
                                PkgC = 5;
                                break;
                            }
                        case "6"://Per C2
                            {
                                PkgC = 6;
                                break;
                            }
                        case "8"://Per A + C
                            {
                                PkgC = 8;
                                break;
                            }
                        case "9"://Per A + C + C1
                            {
                                PkgC = 9;
                                break;
                            }
                        case "10"://Per A + C1
                            {
                                PkgC = 10;
                                break;
                            }
                        case "11"://Per C + C1
                            {
                                PkgC = 11;
                                break;
                            }
                    }
                    //Xác định tiền phòng

                    //Per/Person(từng người trong phòng)
                    if (PkgC == 1)
                    {
                        if (mR.NoOfAdult > 0)
                            Price = OriginPrice * Decimal.Parse(mR.NoOfAdult.ToString());
                        if (mR.NoOfChild > 0)
                            Price = Price + (OriginPrice * Decimal.Parse(mR.NoOfChild.ToString()));
                        if (mR.NoOfChild1 > 0)
                            Price = Price + (OriginPrice * Decimal.Parse(mR.NoOfChild1.ToString()));
                        if (mR.NoOfChild2 > 0)
                            Price = Price + (OriginPrice * Decimal.Parse(mR.NoOfChild2.ToString()));
                        else if (mR.NoOfAdult == 0 && mR.NoOfChild == 0 && mR.NoOfChild1 == 0 && mR.NoOfChild2 == 0)
                        {
                            Price = 0;
                        }
                    }
                    //Per Adult
                    else if (PkgC == 2)
                    {
                        if (mR.NoOfAdult > 0)
                            Price = OriginPrice * Decimal.Parse(mR.NoOfAdult.ToString());
                        else if (mR.NoOfAdult == 0)
                            Price = 0;
                    }
                    //Per Child
                    else if (PkgC == 3)
                    {
                        if (mR.NoOfChild > 0)
                            Price = OriginPrice * Decimal.Parse(mR.NoOfChild.ToString());
                        else if (mR.NoOfChild == 0)
                            Price = 0;
                    }
                    //Per Room
                    else if (PkgC == 4)
                    {
                        Price = OriginPrice;
                    }
                    //Per Child1
                    else if (PkgC == 5)
                    {
                        if (mR.NoOfChild1 > 0)
                            Price = OriginPrice * Decimal.Parse(mR.NoOfChild1.ToString());
                        else if (mR.NoOfChild1 == 0)
                            Price = 0;
                    }
                    //Per Child2
                    else if (PkgC == 6)
                    {
                        if (mR.NoOfChild2 > 0)
                            Price = OriginPrice * Decimal.Parse(mR.NoOfChild2.ToString());
                        else if (mR.NoOfChild2 == 0)
                            Price = 0;
                    }
                    //Per A + C
                    else if (PkgC == 8)
                    {
                        if (mR.NoOfAdult > 0)
                            Price = OriginPrice * Decimal.Parse(mR.NoOfAdult.ToString());
                        if (mR.NoOfChild > 0)
                            Price = Price + (OriginPrice * Decimal.Parse(mR.NoOfChild.ToString()));
                        else if (mR.NoOfAdult == 0 && mR.NoOfChild == 0)
                            Price = 0;
                    }
                    //Per A + C + C1
                    else if (PkgC == 9)
                    {
                        if (mR.NoOfAdult > 0)
                            Price = OriginPrice * Decimal.Parse(mR.NoOfAdult.ToString());
                        if (mR.NoOfChild > 0)
                            Price = Price + (OriginPrice * Decimal.Parse(mR.NoOfChild.ToString()));
                        if (mR.NoOfChild1 > 0)
                            Price = Price + (OriginPrice * Decimal.Parse(mR.NoOfChild1.ToString()));
                        else if (mR.NoOfAdult == 0 && mR.NoOfChild == 0 && mR.NoOfChild1 == 0)
                            Price = 0;
                    }
                    //Per A + C1
                    else if (PkgC == 10)
                    {
                        if (mR.NoOfAdult > 0)
                            Price = OriginPrice * Decimal.Parse(mR.NoOfAdult.ToString());
                        if (mR.NoOfChild1 > 0)
                            Price = Price + (OriginPrice * Decimal.Parse(mR.NoOfChild1.ToString()));
                        else if (mR.NoOfAdult == 0 && mR.NoOfChild1 == 0)
                            Price = 0;
                    }
                    //Per C + C1
                    else if (PkgC == 11)
                    {
                        if (mR.NoOfChild > 0)
                            Price = OriginPrice * Decimal.Parse(mR.NoOfChild.ToString());
                        if (mR.NoOfChild1 > 0)
                            Price = Price + (OriginPrice * Decimal.Parse(mR.NoOfChild1.ToString()));
                        else if (mR.NoOfChild == 0 && mR.NoOfChild1 == 0)
                            Price = 0;
                    }
                    #endregion

                    #region Xác định thời điểm charge
                    switch (dtPkg.Rows[l]["PostingRhythmID"].ToString())
                    {
                        case "1"://Everyday
                            {
                                PkgP = 1;
                                break;
                            }
                        case "2"://Date(At date)
                            {
                                PkgP = 2;
                                break;
                            }
                        case "3"://Day(At Day Of use)
                            {
                                PkgP = 3;
                                break;
                            }
                        case "4"://At Check in
                            {
                                PkgP = 4;
                                break;
                            }
                        case "5"://At Check Out
                            {
                                PkgP = 5;
                                break;
                            }
                    }
                    #endregion

                    #region Every day
                    if (PkgP == 1)
                    {
                        if (dtRR.Rows[i]["CurrencyID"].ToString() == dtPkg.Rows[l]["CurrencyID"].ToString())
                            Rate = Rate + Price;
                        else
                        {
                            Rate = Rate + (TextUtils.ExchangeCurrency(mR.ReservationDate, dtPkg.Rows[l]["CurrencyID"].ToString(), dtRR.Rows[i]["CurrencyID"].ToString(), Price) * TextUtils.ToDecimal(dtPkg.Rows[l]["Quantity"].ToString()));
                        }
                    }
                    #endregion

                    #region Date(At date)
                    else if (PkgP == 2)
                    {
                        if (TextUtils.CompareDate(Convert.ToDateTime(dtRR.Rows[i]["RateDate"]), Convert.ToDateTime(dtPkg.Rows[l]["PostingDate"])) == 0)
                            if (dtRR.Rows[i]["CurrencyID"].ToString() == dtPkg.Rows[l]["CurrencyID"].ToString())
                                Rate = Rate + (Price * TextUtils.ToDecimal(dtPkg.Rows[l]["Quantity"].ToString()));
                            else
                                Rate = Rate + (TextUtils.ExchangeCurrency(mR.ReservationDate, dtPkg.Rows[l]["CurrencyID"].ToString(), dtRR.Rows[i]["CurrencyID"].ToString(), Price) * TextUtils.ToDecimal(dtPkg.Rows[l]["Quantity"].ToString()));
                    }
                    #endregion

                    #region Day(At Day Of use)
                    else if (PkgP == 3)
                    {
                        string _Postingday = dtPkg.Rows[l]["PostingDay"].ToString();
                        string[] _arrPD = _Postingday.Split(',');
                        for (int _pd = 0; _pd < _arrPD.Length; _pd++)
                        {
                            if (_arrPD[_pd] != "")
                            {
                                if (i == int.Parse(_arrPD[_pd].ToString()) - 1)
                                {
                                    if (dtRR.Rows[i]["CurrencyID"].ToString() == dtPkg.Rows[l]["CurrencyID"].ToString())
                                        Rate = Rate + (Price * TextUtils.ToDecimal(dtPkg.Rows[l]["Quantity"].ToString()));
                                    else
                                        Rate = Rate + (TextUtils.ExchangeCurrency(mR.ReservationDate, dtPkg.Rows[l]["CurrencyID"].ToString(), dtRR.Rows[i]["CurrencyID"].ToString(), Price) * TextUtils.ToDecimal(dtPkg.Rows[l]["Quantity"].ToString()));
                                }
                            }
                        }
                    }
                    #endregion

                    #region At Check in
                    else if (PkgP == 4)
                    {
                        if (i == 0)
                            if (dtRR.Rows[i]["CurrencyID"].ToString() == dtPkg.Rows[l]["CurrencyID"].ToString())
                                Rate = Rate + (Price * TextUtils.ToDecimal(dtPkg.Rows[l]["Quantity"].ToString()));
                            else
                                Rate = Rate + (TextUtils.ExchangeCurrency(mR.ReservationDate, dtPkg.Rows[l]["CurrencyID"].ToString(), dtRR.Rows[i]["CurrencyID"].ToString(), Price) * TextUtils.ToDecimal(dtPkg.Rows[l]["Quantity"].ToString()));
                    }
                    #endregion

                    #region At Check Out
                    else if (PkgP == 5)
                    {
                        if (i == dtRR.Rows.Count - 1)
                            if (dtRR.Rows[i]["CurrencyID"].ToString() == dtPkg.Rows[l]["CurrencyID"].ToString())
                                Rate = Rate + (Price * TextUtils.ToDecimal(dtPkg.Rows[l]["Quantity"].ToString()));
                            else
                                Rate = Rate + (TextUtils.ExchangeCurrency(mR.ReservationDate, dtPkg.Rows[l]["CurrencyID"].ToString(), dtRR.Rows[i]["CurrencyID"].ToString(), Price) * TextUtils.ToDecimal(dtPkg.Rows[l]["Quantity"].ToString()));
                    }
                    #endregion
                }
                #endregion               

                #region 6.4.Xác định tiền Package Include trong tiền phòng 
                for (int m = 0; m < dtPkgInc.Rows.Count; m++)
                {
                    Price = 0;
                    //Xác định giá gốc * NoOfRoom                    
                    if (_RoIsTaxInclude == false)
                    {
                        if (mR.NoOfRoom > 0)
                            OriginPrice = Decimal.Parse(dtPkgInc.Rows[m]["Price"].ToString()) * mR.NoOfRoom;
                        else
                            OriginPrice = Decimal.Parse(dtPkgInc.Rows[m]["Price"].ToString());
                    }
                    else
                    {
                        if (mR.NoOfRoom > 0)
                            OriginPrice = Decimal.Parse(dtPkgInc.Rows[m]["PriceAfterTax"].ToString()) * mR.NoOfRoom;
                        else
                            OriginPrice = Decimal.Parse(dtPkgInc.Rows[m]["PriceAfterTax"].ToString());
                    }

                    #region Phương thức charge CanculationRule 
                    PkgC = 0;
                    switch (dtPkgInc.Rows[m]["CalculationRuleID"].ToString())
                    {
                        case "1"://Per/Person(từng người trong phòng)
                            {
                                PkgC = 1;
                                break;
                            }
                        case "2"://Per Adult
                            {
                                PkgC = 2;
                                break;
                            }
                        case "3"://Per Child
                            {
                                PkgC = 3;
                                break;
                            }
                        case "4"://Per Room
                            {
                                PkgC = 4;
                                break;
                            }
                        case "5"://Per C1
                            {
                                PkgC = 5;
                                break;
                            }
                        case "6"://Per C2
                            {
                                PkgC = 6;
                                break;
                            }
                        case "8"://Per A + C
                            {
                                PkgC = 8;
                                break;
                            }
                        case "9"://Per A + C + C1
                            {
                                PkgC = 9;
                                break;
                            }
                        case "10"://Per A + C1
                            {
                                PkgC = 10;
                                break;
                            }
                        case "11"://Per C + C1
                            {
                                PkgC = 11;
                                break;
                            }
                    }

                    //Xác định tiền phòng
                    //Per/Person(từng người trong phòng)
                    if (PkgC == 1)
                    {
                        if (mR.NoOfAdult > 0)
                            Price = OriginPrice * Decimal.Parse(mR.NoOfAdult.ToString());
                        if (mR.NoOfChild > 0)
                            Price = Price + (OriginPrice * Decimal.Parse(mR.NoOfChild.ToString()));
                        if (mR.NoOfChild1 > 0)
                            Price = Price + (OriginPrice * Decimal.Parse(mR.NoOfChild1.ToString()));
                        if (mR.NoOfChild2 > 0)
                            Price = Price + (OriginPrice * Decimal.Parse(mR.NoOfChild2.ToString()));
                        else if (mR.NoOfAdult == 0 && mR.NoOfChild == 0 && mR.NoOfChild1 == 0 && mR.NoOfChild2 == 0)
                            Price = 0;
                    }
                    //Per Adult
                    else if (PkgC == 2)
                    {
                        if (mR.NoOfAdult > 0)
                            Price = OriginPrice * Decimal.Parse(mR.NoOfAdult.ToString());
                        else if (mR.NoOfAdult == 0)
                            Price = 0;
                    }
                    //Per Child
                    else if (PkgC == 3)
                    {
                        if (mR.NoOfChild > 0)
                            Price = OriginPrice * Decimal.Parse(mR.NoOfChild.ToString());
                        else if (mR.NoOfChild == 0)
                            Price = 0;
                    }
                    //Per Room
                    else if (PkgC == 4)
                    {
                        Price = OriginPrice;
                    }
                    //Per Child1
                    else if (PkgC == 5)
                    {
                        if (mR.NoOfChild1 > 0)
                            Price = OriginPrice * Decimal.Parse(mR.NoOfChild1.ToString());
                        else if (mR.NoOfChild1 == 0)
                            Price = 0;
                    }
                    //Per Child2
                    else if (PkgC == 6)
                    {
                        if (mR.NoOfChild2 > 0)
                            Price = OriginPrice * Decimal.Parse(mR.NoOfChild2.ToString());
                        else if (mR.NoOfChild2 == 0)
                            Price = 0;
                    }
                    //Per A + C
                    else if (PkgC == 8)
                    {
                        if (mR.NoOfAdult > 0)
                            Price = OriginPrice * Decimal.Parse(mR.NoOfAdult.ToString());
                        if (mR.NoOfChild > 0)
                            Price = Price + (OriginPrice * Decimal.Parse(mR.NoOfChild.ToString()));
                        else if (mR.NoOfAdult == 0 && mR.NoOfChild == 0)
                            Price = 0;
                    }
                    //Per A + C + C1
                    else if (PkgC == 9)
                    {
                        if (mR.NoOfAdult > 0)
                            Price = OriginPrice * Decimal.Parse(mR.NoOfAdult.ToString());
                        if (mR.NoOfChild > 0)
                            Price = Price + (OriginPrice * Decimal.Parse(mR.NoOfChild.ToString()));
                        if (mR.NoOfChild1 > 0)
                            Price = Price + (OriginPrice * Decimal.Parse(mR.NoOfChild1.ToString()));
                        else if (mR.NoOfAdult == 0 && mR.NoOfChild == 0 && mR.NoOfChild1 == 0)
                            Price = 0;
                    }
                    //Per A + C1
                    else if (PkgC == 10)
                    {
                        if (mR.NoOfAdult > 0)
                            Price = OriginPrice * Decimal.Parse(mR.NoOfAdult.ToString());
                        if (mR.NoOfChild1 > 0)
                            Price = Price + (OriginPrice * Decimal.Parse(mR.NoOfChild1.ToString()));
                        else if (mR.NoOfAdult == 0 && mR.NoOfChild1 == 0)
                            Price = 0;
                    }
                    //Per C + C1
                    else if (PkgC == 11)
                    {
                        if (mR.NoOfChild > 0)
                            Price = OriginPrice * Decimal.Parse(mR.NoOfChild.ToString());
                        if (mR.NoOfChild1 > 0)
                            Price = Price + (OriginPrice * Decimal.Parse(mR.NoOfChild1.ToString()));
                        else if (mR.NoOfChild == 0 && mR.NoOfChild1 == 0)
                            Price = 0;
                    }
                    #endregion

                    #region Xác định thời điểm charge 
                    PkgP = 0;
                    switch (dtPkgInc.Rows[m]["PostingRhythmID"].ToString())
                    {
                        case "1"://Everyday
                            {
                                PkgP = 1;
                                break;
                            }
                        case "2"://Date(At date)
                            {
                                PkgP = 2;
                                break;
                            }
                        case "3"://Day(At Day Of use)
                            {
                                PkgP = 3;
                                break;
                            }
                        case "4"://At Check in
                            {
                                PkgP = 4;
                                break;
                            }
                        case "5"://At Check Out
                            {
                                PkgP = 5;
                                break;
                            }
                    }
                    #endregion

                    #region Trừ đi tiền Package có Include trong tiền phòng 

                    #region Every day 
                    if (PkgP == 1)
                    {
                        //kiểm tra loại tiền 
                        if (dtRR.Rows[i]["CurrencyID"].ToString() == dtPkgInc.Rows[m]["CurrencyID"].ToString())
                        {
                            AmountBeforTax = 0;
                            AmountAfterTax = 0;
                            //Xác định tiền trước thuế và sau thuế
                            //TextUtils.GetSourceAmount(dtPkgInc.Rows[m]["TransactionCode"].ToString(), Price, ref AmountBeforTax, ref AmountAfterTax);
                            ReservationBO.GetAmountSource(dtPkgInc.Rows[m]["TransactionCode"].ToString(), Price, _RoIsTaxInclude, ref AmountBeforTax, ref AmountAfterTax);

                            if (_RoIsTaxInclude == true)
                                Rate = Rate - AmountAfterTax * TextUtils.ToDecimal(dtPkgInc.Rows[m]["Quantity"].ToString());
                            else
                                Rate = Rate - AmountBeforTax * TextUtils.ToDecimal(dtPkgInc.Rows[m]["Quantity"].ToString());
                        }
                        else
                        {
                            AmountBeforTax = 0;
                            AmountAfterTax = 0;
                            //Xác định tiền trước thuế và sau thuế
                            ReservationBO.GetAmountSource(dtPkgInc.Rows[m]["TransactionCode"].ToString(), TextUtils.ExchangeCurrency(mR.ReservationDate, dtPkgInc.Rows[m]["CurrencyID"].ToString(), dtRR.Rows[i]["CurrencyID"].ToString(), Price), _RoIsTaxInclude, ref AmountBeforTax, ref AmountAfterTax);

                            if (_RoIsTaxInclude == true)
                                Rate = Rate - (AmountAfterTax * TextUtils.ToDecimal(dtPkgInc.Rows[m]["Quantity"].ToString()));
                            else
                                Rate = Rate - (AmountBeforTax * TextUtils.ToDecimal(dtPkgInc.Rows[m]["Quantity"].ToString()));
                        }
                    }
                    #endregion

                    #region Date(At date) 
                    else if (PkgP == 2)
                    {
                        if (TextUtils.CompareDate(Convert.ToDateTime(dtRR.Rows[i]["RateDate"]), Convert.ToDateTime(dtPkgInc.Rows[m]["PostingDate"])) == 0)
                        {
                            if (dtRR.Rows[i]["CurrencyID"].ToString() == dtPkgInc.Rows[m]["CurrencyID"].ToString())
                            {
                                AmountBeforTax = 0;
                                AmountAfterTax = 0;
                                //Xác định tiền trước thuế và sau thuế
                                //TextUtils.GetSourceAmount(dtPkgInc.Rows[m]["TransactionCode"].ToString(), Price, ref AmountBeforTax, ref AmountAfterTax);
                                ReservationBO.GetAmountSource(dtPkgInc.Rows[m]["TransactionCode"].ToString(), Price, _RoIsTaxInclude, ref AmountBeforTax, ref AmountAfterTax);
                                if (_RoIsTaxInclude == true)
                                    Rate = Rate - (AmountAfterTax * TextUtils.ToDecimal(dtPkgInc.Rows[m]["Quantity"].ToString()));
                                else
                                    Rate = Rate - (AmountBeforTax * TextUtils.ToDecimal(dtPkgInc.Rows[m]["Quantity"].ToString()));
                            }
                            else
                            {
                                AmountBeforTax = 0;
                                AmountAfterTax = 0;
                                //Xác định tiền trước thuế và sau thuế
                                //TextUtils.GetSourceAmount(dtPkgInc.Rows[m]["TransactionCode"].ToString(), TextUtils.ExchangeCurrency(mR.ReservationDate, dtPkgInc.Rows[m]["CurrencyID"].ToString(), dtRR.Rows[i]["CurrencyID"].ToString(), Price), ref AmountBeforTax, ref AmountAfterTax);
                                ReservationBO.GetAmountSource(dtPkgInc.Rows[m]["TransactionCode"].ToString(), TextUtils.ExchangeCurrency(mR.ReservationDate, dtPkgInc.Rows[m]["CurrencyID"].ToString(), dtRR.Rows[i]["CurrencyID"].ToString(), Price), _RoIsTaxInclude, ref AmountBeforTax, ref AmountAfterTax);
                                if (_RoIsTaxInclude == true)
                                    Rate = Rate - (AmountAfterTax * TextUtils.ToDecimal(dtPkgInc.Rows[m]["Quantity"].ToString()));
                                else
                                    Rate = Rate - (AmountBeforTax * TextUtils.ToDecimal(dtPkgInc.Rows[m]["Quantity"].ToString()));

                            }
                        }
                    }
                    #endregion

                    #region Day(At Day Of use) 
                    else if (PkgP == 3)
                    {

                    }
                    #endregion

                    #region At Check in 
                    else if (PkgP == 4)
                    {
                        if (i == 0)
                            if (dtRR.Rows[i]["CurrencyID"].ToString() == dtPkgInc.Rows[m]["CurrencyID"].ToString())
                            {
                                AmountBeforTax = 0;
                                AmountAfterTax = 0;
                                //Xác định tiền trước thuế và sau thuế
                                ReservationBO.GetAmountSource(dtPkgInc.Rows[m]["TransactionCode"].ToString(), Price, _RoIsTaxInclude, ref AmountBeforTax, ref AmountAfterTax);
                                if (_RoIsTaxInclude == true)
                                    Rate = Rate - (AmountAfterTax * TextUtils.ToDecimal(dtPkgInc.Rows[m]["Quantity"].ToString()));
                                else
                                    Rate = Rate - (AmountAfterTax * TextUtils.ToDecimal(dtPkgInc.Rows[m]["Quantity"].ToString()));
                            }
                            else
                            {
                                AmountBeforTax = 0;
                                AmountAfterTax = 0;
                                //Xác định tiền trước thuế và sau thuế
                                ReservationBO.GetAmountSource(dtPkgInc.Rows[m]["TransactionCode"].ToString(), TextUtils.ExchangeCurrency(mR.ReservationDate, dtPkgInc.Rows[m]["CurrencyID"].ToString(), dtRR.Rows[i]["CurrencyID"].ToString(), Price), _RoIsTaxInclude, ref AmountBeforTax, ref AmountAfterTax);
                                if (_RoIsTaxInclude == true)
                                    Rate = Rate - (AmountAfterTax * TextUtils.ToDecimal(dtPkgInc.Rows[m]["Quantity"].ToString()));
                                else
                                    Rate = Rate - (AmountBeforTax * TextUtils.ToDecimal(dtPkgInc.Rows[m]["Quantity"].ToString()));
                            }
                    }
                    #endregion

                    #region At Check Out
                    else if (PkgP == 5)
                    {
                        if (i == dtRR.Rows.Count - 1)
                            if (dtRR.Rows[i]["CurrencyID"].ToString() == dtPkgInc.Rows[m]["CurrencyID"].ToString())
                            {
                                AmountBeforTax = 0;
                                AmountAfterTax = 0;
                                //Xác định tiền trước thuế và sau thuế
                                ReservationBO.GetAmountSource(dtPkgInc.Rows[m]["TransactionCode"].ToString(), Price, _RoIsTaxInclude, ref AmountBeforTax, ref AmountAfterTax);
                                if (_RoIsTaxInclude == true)
                                    Rate = Rate - (AmountAfterTax * TextUtils.ToDecimal(dtPkgInc.Rows[m]["Quantity"].ToString()));
                                else
                                    Rate = Rate + (AmountBeforTax * TextUtils.ToDecimal(dtPkgInc.Rows[m]["Quantity"].ToString()));
                            }
                            else
                            {
                                AmountBeforTax = 0;
                                AmountAfterTax = 0;
                                //Xác định tiền trước thuế và sau thuế
                                ReservationBO.GetAmountSource(dtPkgInc.Rows[m]["TransactionCode"].ToString(), TextUtils.ExchangeCurrency(mR.ReservationDate, dtPkgInc.Rows[m]["CurrencyID"].ToString(), dtRR.Rows[i]["CurrencyID"].ToString(), Price), _RoIsTaxInclude, ref AmountBeforTax, ref AmountAfterTax);
                                if (_RoIsTaxInclude == true)
                                    Rate = Rate - (AmountAfterTax * TextUtils.ToDecimal(dtPkgInc.Rows[m]["Quantity"].ToString()));
                                else
                                    Rate = Rate - (AmountBeforTax * TextUtils.ToDecimal(dtPkgInc.Rows[m]["Quantity"].ToString()));
                            }
                    }
                    #endregion

                    #endregion
                }
                #endregion

                #region 6.5.Update lại RoomRevenue theo từng ngày trong bảng ReservationRate 
                RoomRevenueBeforeTax = 0;
                RoomRevenueAfterTax = 0;
                //Xác định tiền ++, net
                ReservationBO.GetAmountSource(dtRR.Rows[i]["TransactionCode"].ToString(), Rate, _RoIsTaxInclude, ref RoomRevenueBeforeTax, ref RoomRevenueAfterTax);
                ReservationRateModel mRR = (ReservationRateModel)ReservationRateBO.Instance.FindByPrimaryKey(TextUtils.ToInt(dtRR.Rows[i]["ID"].ToString()));
                if (RoomRevenueBeforeTax >= 0)
                {
                    mRR.RoomRevenueBeforeTax = RoomRevenueBeforeTax;
                    mRR.RoomRevenueAfterTax = RoomRevenueAfterTax;
                }
                else
                {
                    mRR.RoomRevenueBeforeTax = 0;
                    mRR.RoomRevenueAfterTax = 0;
                }
                ReservationRateBO.Instance.Update(mRR);
                #endregion
            }
            #endregion
        }

        public void RoomAssignment(ReservationModel mOR, int ReservationID, int RoomID, int UserID, bool RoomSharer)
        {
            ProcessTransactions pt = new ProcessTransactions();
            pt.OpenConnection();
            pt.BeginTransaction();
            //Xác định đặt phòng đã tồn tại để lấy giá trị
            RoomModel mORms = (RoomModel)RoomBO.Instance.FindByPrimaryKey(RoomID);

            #region Kiếm tra xem có bao nhiêu khách ở cùng phòng với khách này 
            DataTable dtARS = pt.getTable("spCheckReservationAssignRoomSharer", "dtARS",
                      new SqlParameter("@ReservationID", ReservationID),
                      new SqlParameter("@ShareRoom", mOR.ShareRoom));
            if (dtARS.Rows.Count > 0)
            {
                RoomSharer = true;
            }
            #endregion

            //Mở conn

            try
            {
                #region Trường hợp Chưa có số phòng và số Rooms = 1 
                if (mOR.RoomId == 0 && mOR.NoOfRoom == 1)
                {
                    #region Assign khách chính 
                    //Update lại dữ liệu vào bảng Rsv
                    mOR.RoomId = mORms.ID;
                    mOR.RoomNo = mORms.RoomNo;
                    mOR.RoomTypeId = mORms.RoomTypeID;
                    mOR.RoomType = ((RoomTypeModel)pt.FindByPK("RoomType", mORms.RoomTypeID)).Code;
                    if (mOR.RateCodeId == 0)
                        mOR.RtcId = mOR.RoomTypeId;
                    mOR.UserUpdateId = UserID;
                    pt.Update(mOR);
                    //Update lại dữ liệu vào bảng RsvRate
                    if (mOR.RateCodeId == 0)
                    {
                        string sqlRR = "Update ReservationRate with (rowlock) SET " +
                                       "RoomID = " + mORms.ID + ", " +
                                       "RoomNo = '" + mORms.RoomNo + "', " +
                                       "RoomTypeID = " + mORms.RoomTypeID + ", " +
                                       "RoomType = '" + mOR.RoomType + "', " +
                                       "RTCID = " + mORms.RoomTypeID + ", " +
                                       "UserUpdateID = " + UserID + " " +
                                       "WHERE ID IN (SELECT ID FROM ReservationRate WITH (NOLOCK) WHERE ReservationID = " + ReservationID + ") ";
                        pt.UpdateCommand(sqlRR);
                    }
                    else
                    {
                        string sqlRR = "Update ReservationRate with (rowlock) SET " +
                                     "RoomID = " + mORms.ID + ", " +
                                     "RoomNo = '" + mORms.RoomNo + "', " +
                                     "RoomTypeID = " + mORms.RoomTypeID + ", " +
                                     "RoomType = '" + mOR.RoomType + "', " +
                                     "UserUpdateID = " + UserID + " " +
                                     "WHERE ID IN (SELECT ID FROM ReservationRate WITH (NOLOCK) WHERE ReservationID = " + ReservationID + ") ";
                        pt.UpdateCommand(sqlRR);
                    }

                    #region Interface
                    ReservationBO.IF_REC(mOR.ID, mORms.RoomNo);
                    //ReservationBO.IF_REC(mOR, mOR.ID, null, 0, mOR.RoomNo, 0, 1);
                    #endregion

                    #endregion

                    #region Assign khách ở cùng phòng
                    if (RoomSharer == true)
                    {
                        for (int i = 0; i < dtARS.Rows.Count; i++)
                        {
                            //Không có RateCode
                            if (mOR.RateCodeId == 0)
                            {
                                //Cập nhật lại RoomID trong bảng Reservaton
                                string sqlRS = "UPDATE Reservation with (rowlock) SET " +
                                               "RoomID = " + mORms.ID + ", " +
                                               "RoomNo = '" + mORms.RoomNo + "', " +
                                               "RoomTypeID = " + mORms.RoomTypeID + ", " +
                                               "RoomType = '" + mOR.RoomType + "', " +
                                               "RTCID = " + mORms.RoomTypeID + ", " +
                                               "UserUpdateID = " + UserID + " " +
                                               "WHERE ID = " + TextUtils.ToInt(dtARS.Rows[i]["ID"].ToString()) + " ";
                                pt.UpdateCommand(sqlRS);
                                //Cập nhật lại RoomID trong bảng ReservatonRate
                                string sqlRRS = "UPDATE ReservationRate with (rowlock) SET " +
                                                "RoomID = " + mORms.ID + ", " +
                                                "RoomNo = '" + mORms.RoomNo + "', " +
                                                "RoomTypeID = " + mORms.RoomTypeID + ", " +
                                                "RoomType = '" + mOR.RoomType + "', " +
                                                "RTCID = " + mORms.RoomTypeID + ", " +
                                                "UserUpdateID = " + UserID + " " +
                                                "WHERE ID IN (SELECT ID FROM ReservationRate WITH (NOLOCK) WHERE ReservationID = " + TextUtils.ToInt(dtARS.Rows[i]["ID"].ToString()) + ") ";
                                pt.UpdateCommand(sqlRRS);
                            }
                            else
                            {
                                //Cập nhật lại RoomID trong bảng Reservaton
                                string sqlRS = "UPDATE Reservation with (rowlock) SET " +
                                               "RoomID = " + mORms.ID + ", " +
                                               "RoomNo = '" + mORms.RoomNo + "', " +
                                               "RoomTypeID = " + mORms.RoomTypeID + ", " +
                                               "RoomType = '" + mOR.RoomType + "', " +
                                               "UserUpdateID = " + UserID + " " +
                                               "WHERE ID = " + TextUtils.ToInt(dtARS.Rows[i]["ID"].ToString()) + " ";
                                pt.UpdateCommand(sqlRS);
                                //Cập nhật lại RoomID trong bảng ReservatonRate
                                string sqlRRS = "UPDATE ReservationRate with (rowlock) SET " +
                                                "RoomID = " + mORms.ID + ", " +
                                                "RoomNo = '" + mORms.RoomNo + "', " +
                                                "RoomTypeID = " + mORms.RoomTypeID + ", " +
                                                "RoomType = '" + mOR.RoomType + "', " +
                                                "UserUpdateID = " + UserID + " " +
                                                "WHERE ID IN (SELECT ID FROM ReservationRate WITH (NOLOCK) WHERE ReservationID = " + TextUtils.ToInt(dtARS.Rows[i]["ID"].ToString()) + ") ";
                                pt.UpdateCommand(sqlRRS);
                            }

                            #region Interface
                            ReservationBO.IF_REC(TextUtils.ToInt(dtARS.Rows[i]["ID"].ToString()), mORms.RoomNo);
                            //ReservationBO.IF_REC(null, TextUtils.ToInt(dtARS.Rows[i]["ID"].ToString()), null, 0, mOR.RoomNo, 0, 1);
                            #endregion
                        }
                    }
                    #endregion

                }
                #endregion

                #region Trường hợp số Rooms > 1 
                if (mOR.NoOfRoom > 1)
                {
                    Split(ReservationID, mOR.NoOfRoom, UserID, "", mORms.ID);
                }
                #endregion

                //Nếu không bị lỗi - ghi dữ liệu vào bảng
                pt.CommitTransaction();
            }
            catch (Exception ex)
            {
                //Lỗi đóng Conn 
                pt.CloseConnection();
                throw new Exception(ex.Message);

            }
            //Nếu bị lỗi Rollback lại dữ liệu đã ghi
            finally
            {
                pt.CloseConnection();
            }

            #region Update lại trạng thái CurrResvStatus trong bảng Room 
            if (mORms.ID > 0)
                ReservationBO.UpdateReservationStatus(null, mORms.ID);
            #endregion

        }

        //Split 
        public int Split(int reservationID, int pNoOfRoom, int userID, string partyGuest, int roomID)
        {
            // ID của reservation mới được tạo ra sau khi split.
            // Nếu split ra nhiều reservation trong vòng lặp, biến này sẽ giữ ID của reservation cuối cùng được insert.
            int rsv1ID = 0;

            // Chỉ cho phép split khi số lượng phòng lớn hơn 1.
            if (pNoOfRoom > 1)
            {
                // Biến này đang được khai báo trong code gốc nhưng thực tế không dùng đến trong thân hàm.
                //string confNo = "";

                // Dùng để lưu ShareRoom của reservation mới, đặc biệt khi reservation được xác định là MainGuest.
                int shareRoom = 0;

                // Lưu lại reservation gốc ban đầu để cuối hàm còn recalculate amount / group cho đúng booking gốc.
                int originalReservationID = reservationID;

                // Lấy reservation gốc để xác định nhóm room share.
                ReservationModel m = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(originalReservationID);

                // Tìm tất cả reservation có cùng ShareRoom với reservation gốc.
                // Đây chính là tập các reservation đang thuộc cùng một nhóm share-room.
                Expression eRsv = new("ShareRoom", m.ShareRoom, "=");
                ArrayList aRsv = ReservationBO.Instance.FindByExpression(eRsv);

                // Mở transaction để đảm bảo toàn bộ quá trình split là một đơn vị xử lý.
                ProcessTransactions pt = new();
                pt.OpenConnection();
                pt.BeginTransaction();

                try
                {
                    // Nếu có reservation trong nhóm share thì bắt đầu xử lý từng reservation.
                    if (aRsv.Count > 0)
                    {
                        for (int iR = 0; iR < aRsv.Count; iR++)
                        {
                            // Lấy ID reservation hiện tại trong nhóm share.
                            reservationID = ((ReservationModel)aRsv[iR]).ID;

                            ReservationModel mOR = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(reservationID);
                            // Đọc đầy đủ dữ liệu reservation từ DB trong transaction hiện tại.

                            #region Tạo mới Profile

                            // Lấy profile cá nhân của reservation hiện tại.
                            ProfileModel mP = (ProfileModel)ProfileBO.Instance.FindByPrimaryKey(mOR.ProfileIndividualId);

                            // Sinh mã Code mới cho profile.
                            mP.Code = ProfileBO.Instance.GenerateNo4("Code");

                            // Đặt lại trạng thái guest theo logic gốc.
                            mP.ReturnGuest = -1;
                            mP.StayNo = 0;

                            #region 1.&&
                            // Reset một loạt thông tin thống kê / membership / loyalty / hồ sơ cũ
                            // để profile mới được tạo ra mang tính chất “guest tách mới”, không kế thừa lịch sử nghiệp vụ của profile cũ.
                            mP.GuestNo = mP.Occupation = mP.Birthplace = "";
                            mP.BonusPoints = mP.GuestGroupID = 0;
                            mP.ExpressCheckout = mP.PayTV = false;
                            mP.CreditCard = mP.RateCode = "";
                            mP.RoomNights = mP.BedNights = 0;
                            mP.TotalTurnover = mP.LodgeTurnover = mP.LodgePackageTurover = mP.FBTurnover = mP.EventTurnover = mP.OtherTurnover = 0;
                            mP.FirstReservation = Convert.ToDateTime("01/01/1900");
                            mP.LastReservation = Convert.ToDateTime("01/01/1900");
                            mP.WeddingAnniversary = Convert.ToDateTime("01/01/1900");
                            mP.Firstvisit = Convert.ToDateTime("01/01/1900");
                            mP.Expiry = Convert.ToDateTime("01/01/1900");
                            mP.LastContact = Convert.ToDateTime("01/01/1900");
                            #endregion

                            // Insert profile mới và lấy ra ID profile vừa tạo.
                            int pProfileID = (int)pt.Insert(mP);

                            #endregion

                            #region Cập nhật dữ liệu vào bảng Reservation

                            // Reservation mới được tạo dựa trên dữ liệu reservation đang xét.
                            // Code gốc dùng chính object mOR, chỉnh sửa dữ liệu rồi insert thành một record mới.

                            // Cập nhật user thao tác.
                            mOR.UserUpdateId = userID;

                            // Cập nhật người chỉnh sửa đặc biệt / người update.
                            mOR.SpecialUpdateBy = mOR.UpdateBy;

                            // Reset các thông tin không muốn mang theo khi split sang reservation mới.
                            mOR.Specials = "";
                            mOR.ItemInventory = "";
                            mOR.FixedCharge = "";
                            mOR.Vip = "";
                            mOR.VipId = 0;
                            mOR.Phone = "";
                            mOR.Email = "";
                            mOR.MemberLevel = "";
                            mOR.MemberNo = "";
                            mOR.MemberType = "";
                            mOR.Address = "";

                            // Xác định status theo logic gốc:
                            // - Nếu ngày đến = ngày reservation thì status = 5
                            // - Ngược lại status = 0
                            if (TextUtils.CompareDate(mOR.ArrivalDate, mOR.ReservationDate) == 0)
                                mOR.Status = 5;
                            else
                                mOR.Status = 0;

                            // Reservation mới sau split không còn là PostingMaster.
                            mOR.PostingMaster = false;

                            // Nếu bản hiện tại là MainGuest thì NoOfRoom = 1, ngược lại = 0
                            // theo đúng logic gốc của WinForms.
                            if (mOR.MainGuest == true)
                                mOR.NoOfRoom = 1;
                            else
                                mOR.NoOfRoom = 0;

                            // Gán profile mới cho reservation mới.
                            mOR.ProfileIndividualId = pProfileID;

                            // Nếu có RoomID truyền vào thì gán room mới cho reservation mới.
                            // Đây là nhánh dùng cho Room Assignment.
                            if (roomID > 0)
                            {
                                RoomModel mORms = (RoomModel)pt.FindByPK("Room", roomID);
                                mOR.RoomId = roomID;
                                mOR.RoomNo = mORms.RoomNo;
                                mOR.RoomTypeId = mORms.RoomTypeID;
                                mOR.RtcId = mORms.RoomTypeID;
                                mOR.RoomType = ((RoomTypeModel)pt.FindByPK("RoomType", mORms.RoomTypeID)).Code;
                            }

                            // Insert reservation mới.
                            rsv1ID = (int)pt.Insert(mOR);

                            // Sau khi insert xong, cập nhật các field phụ thuộc vào ID mới sinh.
                            mOR.ID = rsv1ID;
                            mOR.PinCode = rsv1ID.ToString();
                            mOR.ReservationNo = rsv1ID.ToString();
                            mOR.BalanceUSD = 0;
                            mOR.BalanceVND = 0;

                            // Nếu reservation mới là MainGuest:
                            // - ShareRoom của nó sẽ là chính ID của nó
                            // - shareRoom local variable cũng lưu giá trị này để các reservation còn lại trỏ theo
                            if (mOR.MainGuest == true)
                            {
                                mOR.ShareRoom = rsv1ID;
                                shareRoom = rsv1ID;

                                // Nếu nhóm share có hơn 1 reservation thì đảm bảo ReservationOptions.Shares = true
                                if (aRsv.Count > 1)
                                {
                                    #region Ghi dữ liệu vào ReservationOption

                                    int ReservationOptionID = ReservationBO.GetReservationOptionID(rsv1ID, pt);
                                    if (ReservationOptionID == 0)
                                    {
                                        ReservationOptionsModel mRO = new ReservationOptionsModel();
                                        mRO.ReservationID = rsv1ID;
                                        mRO.Shares = true;
                                        pt.Insert(mRO);
                                    }
                                    else
                                    {
                                        ReservationOptionsModel mRO = (ReservationOptionsModel)pt.FindByPK("ReservationOptions", ReservationOptionID);
                                        mRO.ID = ReservationOptionID;
                                        mRO.Shares = true;
                                        pt.Update(mRO);
                                    }

                                    #endregion
                                }
                            }
                            else
                            {
                                // Nếu không phải MainGuest thì reservation mới sẽ dùng ShareRoom của main reservation vừa xác định.
                                mOR.ShareRoom = shareRoom;
                            }

                            // Update lại reservation mới sau khi đã set PinCode, ReservationNo, ShareRoom...
                            pt.Update(mOR);

                            // Nếu reservation hiện tại là MainGuest thì update lại NoOfRoom của reservation cũ
                            // bằng pNoOfRoom - 1 và set PartyGuest.
                            if (mOR.MainGuest == true)
                            {
                                string sqlmOR = "UPDATE Reservation with (rowlock) Set NoOfRoom = " + pNoOfRoom + " - " + 1 + ", PartyGuest = '" + partyGuest + "' " +
                                                "WHERE ID = " + reservationID + " ";
                                pt.UpdateCommand(sqlmOR);
                            }

                            // Nếu reservation hiện tại không phải MainGuest và số phòng ban đầu là 2
                            // thì sau khi split phần còn lại bằng 0.
                            if (mOR.MainGuest == false && pNoOfRoom == 2)
                            {
                                string sql = "UPDATE Reservation with (rowlock) Set NoOfRoom = 0, PartyGuest = '" + partyGuest + "' " +
                                             "WHERE ID = " + reservationID + " ";
                                pt.UpdateCommand(sql);
                            }

                            // Ghi PartyGuest cho reservation mới để phục vụ danh sách party đã tách.
                            if (partyGuest != "")
                            {
                                string sqlP = "UPDATE Reservation with (rowlock) Set PartyGuest = '" + partyGuest + "' " +
                                              "WHERE ID = " + rsv1ID + " ";
                                pt.UpdateCommand(sqlP);
                            }

                            #endregion

                            #region Cập nhật dữ liệu vào bảng ReservationRate

                            // Nếu có Room Assignment thì nạp Room để gán vào ReservationRate mới.
                            RoomModel mpORms = null;
                            if (roomID > 0)
                            {
                                mpORms = (RoomModel)pt.FindByPK("Room", roomID);
                            }

                            // Lấy toàn bộ ReservationRate của reservation cũ.
                            DataTable CRR = pt.getTable(
                                "spCheckReservationRate",
                                "tbRsvR",
                                new SqlParameter("@ReservationID", reservationID)
                            );

                            // Copy từng dòng ReservationRate sang reservation mới.
                            for (int i = 0; i < CRR.Rows.Count; i++)
                            {
                                ReservationRateModel mRr = new ReservationRateModel();
                                mRr.ReservationID = rsv1ID;
                                mRr.RateCodeID = int.Parse(CRR.Rows[i]["RateCodeID"].ToString());
                                mRr.TransactionCode = CRR.Rows[i]["TransactionCode"].ToString();
                                mRr.RateDate = Convert.ToDateTime(CRR.Rows[i]["RateDate"]);
                                mRr.RateDate = new DateTime(mRr.RateDate.Year, mRr.RateDate.Month, mRr.RateDate.Day, 0, 0, 0);
                                mRr.Rate = Convert.ToDecimal(CRR.Rows[i]["Rate"].ToString());
                                mRr.RateAfterTax = Convert.ToDecimal(CRR.Rows[i]["RateAfterTax"].ToString());
                                mRr.RoomRevenueBeforeTax = Convert.ToDecimal(CRR.Rows[i]["RoomRevenueBeforeTax"].ToString());
                                mRr.RoomRevenueAfterTax = Convert.ToDecimal(CRR.Rows[i]["RoomRevenueAfterTax"].ToString());
                                mRr.DiscountAmount = Convert.ToDecimal(CRR.Rows[i]["DiscountAmount"].ToString());
                                mRr.DiscountRate = Convert.ToDecimal(CRR.Rows[i]["DiscountRate"].ToString());
                                mRr.IsTaxInclude = bool.Parse(CRR.Rows[i]["IsTaxInclude"].ToString());
                                mRr.NoOfAdult = TextUtils.ToInt(CRR.Rows[i]["NoOfAdult"].ToString());
                                mRr.NoOfChild = TextUtils.ToInt(CRR.Rows[i]["NoOfChild"].ToString());
                                mRr.NoOfChild1 = TextUtils.ToInt(CRR.Rows[i]["NoOfChild1"].ToString());
                                mRr.NoOfChild2 = TextUtils.ToInt(CRR.Rows[i]["NoOfChild2"].ToString());
                                mRr.MarketID = TextUtils.ToInt(CRR.Rows[i]["MarketID"].ToString());
                                mRr.SourceID = TextUtils.ToInt(CRR.Rows[i]["SourceID"].ToString());
                                mRr.AllotmentID = TextUtils.ToInt(CRR.Rows[i]["AllotmentID"].ToString());
                                mRr.CurrencyID = CRR.Rows[i]["CurrencyID"].ToString();
                                mRr.FixedRate = bool.Parse(CRR.Rows[i]["FixedRate"].ToString());
                                mRr.RoomID = int.Parse(CRR.Rows[i]["RoomID"].ToString());
                                mRr.RoomNo = CRR.Rows[i]["RoomNo"].ToString();
                                mRr.RoomTypeID = int.Parse(CRR.Rows[i]["RoomTypeID"].ToString());
                                mRr.RoomType = CRR.Rows[i]["RoomType"].ToString();
                                mRr.RTCID = int.Parse(CRR.Rows[i]["RTCID"].ToString());

                                // Nếu có room mới thì override room info trong ReservationRate.
                                if (roomID > 0)
                                {
                                    mRr.RoomID = roomID;
                                    mRr.RoomNo = mpORms.RoomNo;
                                    mRr.RoomTypeID = mpORms.RoomTypeID;
                                    mRr.RoomType = ((RoomTypeModel)pt.FindByPK("RoomType", mpORms.RoomTypeID)).Code;
                                }

                                // Gán thông tin tạo/cập nhật.
                                mRr.UserInsertID = mRr.UserUpdateID = userID;
                                mRr.CreateDate = mRr.UpdateDate = TextUtils.GetSystemDate();

                                // Insert ReservationRate mới.
                                int RR1ID = (int)pt.Insert(mRr);
                            }

                            #endregion

                            #region Tạo Routing Khi tách từ Origin Reservation

                            // Toàn bộ phần Routing trong code gốc đang bị comment.
                            // Bản service giữ nguyên, không thay đổi hành vi.
                            //Expression expR = new Expression("FromReservationID", reservationID, "=");
                            //ArrayList arrR = pt.FindByExpression("Routing", expR);
                            //if (arrR.Count > 0)
                            //{
                            //    for (int r = 0; r < arrR.Count; r++)
                            //    {
                            //        if (((RoutingModel)arrR[r]).ToFolioNo == 1)
                            //            ClassReservation.CreateRouting(rsv1ID, rsv1ID, ((RoutingModel)arrR[r]).ToFolioNo, ((RoutingModel)arrR[r]).FromDate, ((RoutingModel)arrR[r]).ToDate, pProfileID, ((RoutingModel)arrR[r]).TransactionCodes, pt);
                            //        else
                            //            ClassReservation.CreateRouting(rsv1ID, ((RoutingModel)arrR[r]).ToReservationID, ((RoutingModel)arrR[r]).ToFolioNo, ((RoutingModel)arrR[r]).FromDate, ((RoutingModel)arrR[r]).ToDate, ((RoutingModel)arrR[r]).ProfileID, ((RoutingModel)arrR[r]).TransactionCodes, pt);
                            //    }
                            //}

                            #endregion

                            #region Cập nhật dữ liệu vào bảng ReservationPackage nếu có

                            // Copy package từ reservation cũ sang reservation mới.
                            ReservationBO.CopyTbReservationPackage(reservationID, rsv1ID, userID, pt);

                            #endregion

                            #region Cập nhật dữ liệu vào bảng ReservationFixedCharge nếu có

                            // Code gốc đang comment phần copy FixedCharge.
                            // Giữ nguyên hành vi, không tự ý bật lại.
                            //ClassReservation.CopyTbReservationFixedCharge(reservationID, rsv1ID, userID, pt);

                            // Copy Special từ reservation cũ sang reservation mới.
                            ReservationBO.CopyTbReservationSpecial(reservationID, rsv1ID, userID, pt);

                            #endregion

                            #region Ghi dữ liệu vào ReservationOption nếu có

                            // Kiểm tra booking hiện tại có Routing hay không.
                            bool _Routing = false;
                            ReservationBO.CheckRouting(mOR.ConfirmationNo, ref _Routing, pt);

                            // Nếu reservation thuộc Group hoặc có Routing thì ghi/điều chỉnh ReservationOptions.
                            if (mOR.ProfileGroupId > 0 || _Routing == true)
                            {
                                int ReservationOptionID = ReservationBO.GetReservationOptionID(rsv1ID, pt);
                                if (ReservationOptionID == 0)
                                {
                                    ReservationOptionsModel mRO = new ReservationOptionsModel();
                                    mRO.ReservationID = rsv1ID;

                                    if (mOR.ProfileGroupId > 0)
                                        mRO.GroupOptions = true;

                                    if (_Routing == true)
                                        mRO.Routing = true;

                                    pt.Insert(mRO);
                                }
                                else
                                {
                                    ReservationOptionsModel mRO = (ReservationOptionsModel)pt.FindByPK("ReservationOptions", ReservationOptionID);
                                    mRO.ID = ReservationOptionID;

                                    if (mOR.ProfileGroupId > 0)
                                        mRO.GroupOptions = true;

                                    if (_Routing == true)
                                        mRO.Routing = true;

                                    pt.Update(mRO);
                                }
                            }

                            #endregion

                            #region Ghi dữ liệu vào bảng ReservationAmountByCurrency

                            // Xóa dữ liệu amount cũ của reservation mới trước khi tính lại.
                            pt.DeleteByAttribute("ReservationAmountByCurrency", "ReservationID", rsv1ID.ToString());

                            // Tính lại amount theo currency cho reservation mới.
                            ReservationBO.GetAmountByCurrency(rsv1ID, userID, pt);

                            #endregion

                            #region Tính RoomRevenue theo từng ngày cho bảng ReservationRate

                            // Tính lại RoomRevenue cho reservation mới.
                            if (rsv1ID > 0)
                                ReservationBO.GetRoomRevenue(rsv1ID, pt);

                            #endregion

                            #region Interface

                            // Gọi interface theo logic cũ.
                            //ReservationBO.IF_REN(mOR, mOR.ID);

                            #endregion

                            // Xử lý meal theo package cho reservation mới.
                            //ReservationBO.ProcessMeal(pt, mOR, false);
                        }

                        // Sau khi kết thúc vòng lặp, giảm số phòng còn lại để split theo logic gốc.
                        pNoOfRoom = pNoOfRoom - 1;

                        #region Ghi dữ liệu vào bảng ReservationAmountByCurrency - Rsv gốc

                        // Sau khi split xong, tính lại amount cho reservation gốc.
                        if (originalReservationID > 0)
                        {
                            pt.DeleteByAttribute("ReservationAmountByCurrency", "ReservationID", originalReservationID.ToString());
                            ReservationBO.GetAmountByCurrency(originalReservationID, userID, pt);
                        }

                        #endregion
                    }

                    // Commit toàn bộ transaction nếu không có lỗi.
                    pt.CommitTransaction();
                }
                catch (Exception)
                {
                    // Giữ nguyên hành vi mức service:
                    // - đóng connection
                    // - trả về 0 nếu lỗi
                    // Không hiển thị MessageBox như WinForms vì service không có UI.
                    pt.RollBack();
                    throw;
                }
                finally
                {
                    // Đảm bảo connection luôn được đóng.
                    pt.CloseConnection();
                }

                #region Tính RoomRevenue theo từng ngày cho bảng ReservationRate - Rsv gốc

                // Tính lại RoomRevenue cho reservation gốc sau khi transaction đã hoàn tất.
                if (originalReservationID > 0)
                    GetRoomRevenue(originalReservationID);

                #endregion

                #region Ghi dữ liệu vào bảng ReservationGroup và ReservationGroupAmountByCurrency

                // Rebuild ReservationGroup / ReservationGroupAmountByCurrency cho confirmation group.
                // Điều kiện tiên quyết là AmountByCurrency đã được ghi xong trước đó.
                //ReservationBO.CreateReservationGroup(originalReservationID, m.ConfirmationNo, "");

                #endregion
            }

            // Trả về ID của reservation mới cuối cùng được tạo ra.
            return rsv1ID;
        }

        public int SplitAll(int ReservationID, int pNoOfRoom, int UserID, string username, string PartyGuest)
        {
            if (pNoOfRoom > 1)
            {
                int ShareRoom = 0;
                int pReservationID = ReservationID;
                //Kiểm tra xem Booking này có RoomShare hay không?
                ReservationModel m = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(pReservationID);
                Expression eRsv = new Expression("ShareRoom", m.ShareRoom, "=");
                ArrayList aRsv = ReservationBO.Instance.FindByExpression(eRsv);
                //Chỉ tách 5 Rms 1 lần
                int MaxpNoOfRoom = pNoOfRoom;
                if (pNoOfRoom > 6)
                    MaxpNoOfRoom = 6;
                //Tách từng phiếu đặt phòng
                for (int j = 0; j < MaxpNoOfRoom; j++)
                {
                    //Mở conn
                    ProcessTransactions pt = new();
                    pt.OpenConnection();
                    pt.BeginTransaction();
                    try
                    {
                        if (aRsv.Count > 0)
                        {
                            for (int iR = 0; iR < aRsv.Count; iR++)
                            {
                                //Xác định đặt phòng đã tồn tại để lấy giá trị
                                ReservationID = ((ReservationModel)aRsv[iR]).ID;
                                ReservationModel mOR = (ReservationModel)pt.FindByPK("Reservation", ReservationID);

                                #region Tạo mới Profile
                                //DataTable CR = pt.Select("Select Top 1 MAX(Convert(int,Code)) AS Code FROM Profile WITH (NOLOCK)");
                                ProfileModel mP = (ProfileModel)pt.FindByPK("Profile", mOR.ProfileIndividualId);
                                //int leg = CR.Rows[0]["Code"].ToString().Length;
                                //mP.Code = "0000" + Convert.ToString(Convert.ToUInt32(CR.Rows[0]["Code"].ToString()) + 1);
                                //mP.Code = mP.Code.Remove(0, mP.Code.Length - leg);
                                mP.Code = ProfileBO.Instance.GenerateNo4("Code");
                                mP.ReturnGuest = -1;
                                mP.StayNo = 0;
                                #region 1.&&
                                mP.GuestNo = mP.Occupation = mP.Birthplace = "";
                                mP.BonusPoints = mP.GuestGroupID = 0;
                                mP.ExpressCheckout = mP.PayTV = false;
                                mP.CreditCard = mP.RateCode = "";
                                mP.RoomNights = mP.BedNights = 0;
                                mP.TotalTurnover = mP.LodgeTurnover = mP.LodgePackageTurover = mP.FBTurnover = mP.EventTurnover = mP.OtherTurnover = 0;
                                mP.FirstReservation = Convert.ToDateTime("01/01/1900");
                                mP.LastReservation = Convert.ToDateTime("01/01/1900");
                                mP.WeddingAnniversary = Convert.ToDateTime("01/01/1900");
                                mP.Firstvisit = Convert.ToDateTime("01/01/1900");
                                mP.Expiry = Convert.ToDateTime("01/01/1900");
                                mP.LastContact = Convert.ToDateTime("01/01/1900");
                                #endregion

                                int pProfileID = (int)pt.Insert(mP);
                                #endregion

                                #region Cập nhật dữ liệu vào bảng Reservation
                                mOR.UserInsertId = mOR.UserUpdateId = UserID;
                                //mOR.CreateDate = mOR.UpdateDate = mOR.SpecialUpdateDate = TextUtils.GetSystemDate();
                                //mOR.CreateBy = Global.UserName;
                                mOR.SpecialUpdateBy = mOR.UpdateBy = username;
                                //mOR.ReservationDate = TextUtils.GetBusinessDate();
                                mOR.Specials = "";
                                mOR.ItemInventory = "";
                                mOR.FixedCharge = "";
                                mOR.Vip = "";
                                mOR.VipId = 0;
                                mOR.Phone = "";
                                mOR.Email = "";
                                mOR.Address = "";
                                mOR.MemberLevel = "";
                                mOR.MemberNo = "";
                                mOR.MemberType = "";
                                if (TextUtils.CompareDate(mOR.ArrivalDate, TextUtils.GetBusinessDate()) == 0)
                                    mOR.Status = 5;
                                else
                                    mOR.Status = 0;

                                mOR.PostingMaster = false;
                                if (mOR.MainGuest == true)
                                    mOR.NoOfRoom = 1;
                                else
                                    mOR.NoOfRoom = 0;
                                mOR.ProfileIndividualId = pProfileID;
                                mOR.BalanceVND = 0;
                                mOR.BalanceUSD = 0;
                                int Rsv1ID = (int)pt.Insert(mOR);
                                //Update ReservationNo,ConfirmNo vào bảng Rsv
                                mOR.ID = Rsv1ID;
                                mOR.PinCode = Rsv1ID.ToString();
                                mOR.ReservationNo = Rsv1ID.ToString();
                                if (mOR.MainGuest == true)
                                {
                                    //mOR.ConfirmationNo = pReservationID.ToString();
                                    //ConfNo = pReservationID.ToString();
                                    mOR.ShareRoom = Rsv1ID;
                                    ShareRoom = Rsv1ID;

                                    if (aRsv.Count > 1)
                                    {
                                        #region Ghi dữ liệu vào ReservationOption
                                        int ReservationOptionID = ReservationBO.GetReservationOptionID(Rsv1ID, pt);
                                        if (ReservationOptionID == 0)
                                        {
                                            ReservationOptionsModel mRO = new ReservationOptionsModel();
                                            mRO.ReservationID = Rsv1ID;
                                            mRO.Shares = true;
                                            pt.Insert(mRO);
                                        }
                                        else
                                        {
                                            ReservationOptionsModel mRO = (ReservationOptionsModel)pt.FindByPK("ReservationOptions", ReservationOptionID);
                                            mRO.ID = ReservationOptionID;
                                            mRO.Shares = true;
                                            pt.Update(mRO);
                                        }
                                        #endregion
                                    }
                                }
                                else
                                {
                                    mOR.ShareRoom = ShareRoom;
                                }
                                pt.Update(mOR);

                                //19/01/2010 - Cập nhật lại số NoOfRoom của đặt phòng đã tồn tại (Khi làm RS số NoOfRoom của RS = số NoOfRoom của MG)
                                string sqlmOR = "UPDATE Reservation with (rowlock) Set NoOfRoom = " + pNoOfRoom + " - " + 1 + ", PartyGuest = '" + PartyGuest + "' " +
                                                "WHERE ID = " + ReservationID + " ";
                                pt.UpdateCommand(sqlmOR);

                                //Nếu số NoOfRoom của RS = 2 - 1 thì update lại No.Rms = 0 trong bảng Rsv
                                if (mOR.MainGuest == false && pNoOfRoom == 2)
                                {
                                    string sql = "UPDATE Reservation with (rowlock) Set NoOfRoom = 0, PartyGuest = '" + PartyGuest + "' " +
                                               "WHERE ID = " + ReservationID + " ";
                                    pt.UpdateCommand(sql);
                                }

                                //Update date PartyGuest de Liet ke danh sach da tach Party
                                if (PartyGuest != "")
                                {
                                    string sqlP = "UPDATE Reservation with (rowlock) Set PartyGuest = '" + PartyGuest + "' " +
                                                   "WHERE ID = " + Rsv1ID + " ";
                                    pt.UpdateCommand(sqlP);
                                }

                                #endregion

                                #region Cập nhật dữ liệu vào bảng ReservationRate
                                //Select dữ liệu từ bảng ReservationRate    
                                DataTable CRR = pt.getTable("spCheckReservationRate", "tbRsvR",
                                               new SqlParameter("@ReservationID", ReservationID));
                                for (int i = 0; i < CRR.Rows.Count; i++)
                                {
                                    ReservationRateModel mRr = new ReservationRateModel();
                                    mRr.ReservationID = Rsv1ID;
                                    mRr.RateCodeID = int.Parse(CRR.Rows[i]["RateCodeID"].ToString());
                                    mRr.RateDate = Convert.ToDateTime(CRR.Rows[i]["RateDate"]);
                                    mRr.RateDate = new DateTime(mRr.RateDate.Year, mRr.RateDate.Month, mRr.RateDate.Day, 0, 0, 0);
                                    mRr.TransactionCode = CRR.Rows[i]["TransactionCode"].ToString();
                                    mRr.Rate = Convert.ToDecimal(CRR.Rows[i]["Rate"].ToString());
                                    mRr.RateAfterTax = Convert.ToDecimal(CRR.Rows[i]["RateAfterTax"].ToString());
                                    mRr.RoomRevenueBeforeTax = Convert.ToDecimal(CRR.Rows[i]["RoomRevenueBeforeTax"].ToString());
                                    mRr.RoomRevenueAfterTax = Convert.ToDecimal(CRR.Rows[i]["RoomRevenueAfterTax"].ToString());
                                    mRr.DiscountAmount = Convert.ToDecimal(CRR.Rows[i]["DiscountAmount"].ToString());
                                    mRr.DiscountRate = Convert.ToDecimal(CRR.Rows[i]["DiscountRate"].ToString());
                                    mRr.IsTaxInclude = bool.Parse(CRR.Rows[i]["IsTaxInclude"].ToString());
                                    mRr.NoOfAdult = TextUtils.ToInt(CRR.Rows[i]["NoOfAdult"].ToString());
                                    mRr.NoOfChild = TextUtils.ToInt(CRR.Rows[i]["NoOfChild"].ToString());
                                    mRr.NoOfChild1 = TextUtils.ToInt(CRR.Rows[i]["NoOfChild1"].ToString());
                                    mRr.NoOfChild2 = TextUtils.ToInt(CRR.Rows[i]["NoOfChild2"].ToString());
                                    mRr.MarketID = TextUtils.ToInt(CRR.Rows[i]["MarketID"].ToString());
                                    mRr.SourceID = TextUtils.ToInt(CRR.Rows[i]["SourceID"].ToString());
                                    mRr.AllotmentID = TextUtils.ToInt(CRR.Rows[i]["AllotmentID"].ToString());
                                    mRr.CurrencyID = CRR.Rows[i]["CurrencyID"].ToString();
                                    mRr.FixedRate = bool.Parse(CRR.Rows[i]["FixedRate"].ToString());
                                    mRr.RoomID = int.Parse(CRR.Rows[i]["RoomID"].ToString());
                                    mRr.RoomNo = CRR.Rows[i]["RoomNo"].ToString();
                                    mRr.RoomTypeID = int.Parse(CRR.Rows[i]["RoomTypeID"].ToString());
                                    mRr.RoomType = CRR.Rows[i]["RoomType"].ToString();
                                    mRr.RTCID = int.Parse(CRR.Rows[i]["RTCID"].ToString());
                                    mRr.UserInsertID = mRr.UserUpdateID = UserID;
                                    mRr.CreateDate = mRr.UpdateDate = TextUtils.GetSystemDate();
                                    int RR1ID = (int)pt.Insert(mRr);
                                }
                                #endregion

                                #region Tạo Routing Khi tách từ Origin Reservation - Bỏ
                                ////Tìm kiếm xem Reservation Origin có bao nhiêu Routing
                                //Expression expR = new Expression("FromReservationID", ReservationID, "=");
                                //ArrayList arrR = pt.FindByExpression("Routing", expR);
                                //if (arrR.Count > 0)
                                //{
                                //    for (int r = 0; r < arrR.Count; r++)
                                //    {
                                //        //Nếu WindownNo ==1 thì Routing default về chính nó
                                //        if (((RoutingModel)arrR[r]).ToFolioNo == 1)
                                //            TextUtils.CreateRouting(Rsv1ID, Rsv1ID, ((RoutingModel)arrR[r]).ToFolioNo, ((RoutingModel)arrR[r]).FromDate, ((RoutingModel)arrR[r]).ToDate, pProfileID, ((RoutingModel)arrR[r]).TransactionCodes, pt);
                                //        else
                                //            TextUtils.CreateRouting(Rsv1ID, ((RoutingModel)arrR[r]).ToReservationID, ((RoutingModel)arrR[r]).ToFolioNo, ((RoutingModel)arrR[r]).FromDate, ((RoutingModel)arrR[r]).ToDate, ((RoutingModel)arrR[r]).ProfileID, ((RoutingModel)arrR[r]).TransactionCodes, pt);
                                //    }
                                //}
                                #endregion

                                #region Cập nhật dự liệu vào bảng ReservationPackage nếu có
                                ReservationBO.CopyTbReservationPackage(ReservationID, Rsv1ID, UserID, pt);
                                #endregion

                                #region Cập nhật dự liệu vào bảng ReservationFixedCharge nếu có - Bỏ
                                //ClassReservation.CopyTbReservationFixedCharge(ReservationID, Rsv1ID, UserID, pt);
                                #endregion

                                #region Ghi dữ liệu vào ReservationOption nếu có
                                //Check Routing FolioMaster
                                bool _Routing = false;
                                ReservationBO.CheckRouting(mOR.ConfirmationNo, ref _Routing, pt);
                                //Trường hợp có Group hoặc có Routing thì ghi hoặc sửa dữ liệu trong bảng ReservationOptions
                                if (mOR.ProfileGroupId > 0 || _Routing == true)
                                {
                                    int ReservationOptionID = ReservationBO.GetReservationOptionID(Rsv1ID, pt);
                                    if (ReservationOptionID == 0)
                                    {
                                        ReservationOptionsModel mRO = new ReservationOptionsModel();
                                        mRO.ReservationID = Rsv1ID;
                                        if (mOR.ProfileGroupId > 0)
                                            mRO.GroupOptions = true;
                                        if (_Routing == true)
                                            mRO.Routing = true;
                                        pt.Insert(mRO);
                                    }
                                    else
                                    {
                                        ReservationOptionsModel mRO = (ReservationOptionsModel)pt.FindByPK("ReservationOptions", ReservationOptionID); ;
                                        mRO.ID = ReservationOptionID;
                                        if (mOR.ProfileGroupId > 0)
                                            mRO.GroupOptions = true;
                                        if (_Routing == true)
                                            mRO.Routing = true;
                                        pt.Update(mRO);
                                    }
                                }
                                #endregion

                                #region Ghi dữ liệu vào bảng ReservationAmountByCurrency
                                //Xóa dữ liệu trước khi Insert
                                pt.DeleteByAttribute("ReservationAmountByCurrency", "ReservationID", Rsv1ID.ToString());
                                //Tính lại số liệu rồi ghi dữ liệu 
                                ReservationBO.GetAmountByCurrency(Rsv1ID, UserID, pt);
                                #endregion

                                #region Tính RoomRevenue theo từng ngày cho bảng ReservationRate
                                if (Rsv1ID > 0)
                                    ReservationBO.GetRoomRevenue(Rsv1ID, pt);
                                #endregion

                                #region Interface
                                //if (ClassReservation.GetDateNoOfDay(mOR.ArrivalDate, TextUtils.GetBusinessDate()) <= 3)
                                ReservationUtil.IF_REN(mOR, mOR.ID);
                                #endregion

                                //Xử lý bưa ăn của khách theo Package
                                ReservationUtil.ProcessMeal(pt, mOR, false);
                            }
                            //Xác định lại số Rooms còn lại để Split
                            pNoOfRoom = pNoOfRoom - 1;
                            //Chỉ tách 5 Rms 1 lần
                            MaxpNoOfRoom = MaxpNoOfRoom - 1;
                        }
                        j = 0;

                        #region Ghi dữ liệu vào bảng ReservationAmountByCurrency - Rsv gốc 
                        if (pReservationID > 0)
                        {
                            //Xóa dữ liệu trước khi Insert
                            pt.DeleteByAttribute("ReservationAmountByCurrency", "ReservationID", pReservationID.ToString());
                            //Tính lại số liệu rồi ghi dữ liệu 
                            ReservationBO.GetAmountByCurrency(pReservationID, UserID, pt);
                        }
                        #endregion                                             

                        //Nếu không bị lỗi - ghi dữ liệu vào bảng
                        pt.CommitTransaction();
                    }
                    catch (Exception ex)
                    {
                        pt.RollBack();
                        throw;
                    }
                    //Nếu bị lỗi Rollback lại dữ liệu đã ghi
                    finally
                    {
                        pt.CloseConnection();
                    }

                    #region Tính RoomRevenue theo từng ngày cho bảng ReservationRate - Rsv gốc 
                    if (pReservationID > 0)
                        GetRoomRevenue(pReservationID);
                    #endregion

                    #region Ghi dữ liệu vào bảng ReservationGroup và ReservationGroupAmountByCurrency 
                    //Chú ý phải thực hiện ghi dữ liệu vào bảng ReservationAmountByCurrency trước                            
                    ReservationUtil.CreateReservationGroup(m.ID, m.ConfirmationNo, "", UserID);
                    #endregion

                }
            }
            return ReservationID;
        }

        public int SplitSpecial(int ReservationID, int pNoOfRoom, int UserID, string PartyGuest)
        {
            int roomID = 0;
            int pNewReservationID;

            // Lưu lại reservation gốc ban đầu để cuối hàm còn recalculate amount / group cho đúng booking gốc.
            int originalReservationID = ReservationID;

            // Lấy reservation gốc để xác định dữ liệu hiện tại.
            ReservationModel m = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(originalReservationID) ?? throw new Exception("Reservation not found.");

            // Lấy dữ liệu thật từ reservation gốc
            int _ShareRoom = m.ShareRoom;
            int _NoOfAdult = m.NoOfAdult;
            int _NoOfChild = m.NoOfChild;
            int _NoOfChild1 = m.NoOfChild1;
            int _NoOfChild2 = m.NoOfChild2;

            // Lưu lại ProfileIndividualID
            int pOldProfileIndividualID = m.ProfileIndividualId;

            if (pNoOfRoom <= 0)
                throw new Exception("Number of room is invalid.");

            if (pNoOfRoom > 1)
            {
                #region Tách phiếu đặt phòng - Main Guest
                pNewReservationID = Split(ReservationID, pNoOfRoom, UserID, PartyGuest, roomID);
                #endregion

                if (pNewReservationID <= 0)
                    throw new Exception("Split reservation failed.");

                // Kiểm tra xem Booking này có RoomShare hay không?
                Expression eRsv = new("ShareRoom", _ShareRoom, "=");
                eRsv = eRsv.And(new Expression("MainGuest", 0, "="));
                ArrayList aRsv = ReservationBO.Instance.FindByExpression(eRsv);

                if (aRsv.Count == 0)
                {
                    #region Tạo Room Sharer từ phiếu đặt phòng - Room Sharer

                    // Chỉ tạo RS cho phiếu đặt phòng mới tách ra
                    if (pNoOfRoom > 2)
                    {
                        int Person = _NoOfAdult + _NoOfChild + _NoOfChild1 + _NoOfChild2;
                        for (int i = 0; i < Person - 1; i++)
                            ReservationUtil.GenCreateRoomShareNoTransaction(pNewReservationID, UserID);
                    }

                    // Tạo RS cho phiếu đặt phòng mới tách ra và phiếu đặt phòng gốc
                    if (pNoOfRoom == 2)
                    {
                        int Person = _NoOfAdult + _NoOfChild + _NoOfChild1 + _NoOfChild2;
                        for (int j = 0; j < Person - 1; j++)
                        {
                            ReservationUtil.GenCreateRoomShareNoTransaction(pNewReservationID, UserID);
                            ReservationUtil.GenCreateRoomShareNoTransaction(ReservationID, UserID);
                        }
                    }

                    #endregion
                }

                return pNewReservationID;
            }
            else if (pNoOfRoom == 1)
            {
                if (pOldProfileIndividualID == 0)
                    throw new Exception("Old profile individual ID is invalid.");

                // Kiểm tra xem số RS vượt quá số lượng cho phép không cho Share nữa
                if (ReservationUtil.CheckShareRoom(_ShareRoom) >= ReservationUtil.CheckShareRoomTotal(_ShareRoom))
                    throw new Exception("RoomSharer exceed number person.");

                // Kiểm tra xem Booking này có RoomShare hay không?
                Expression eRsv = new Expression("ShareRoom", _ShareRoom, "=");
                eRsv = eRsv.And(new Expression("MainGuest", 0, "="));
                ArrayList aRsv = ReservationBO.Instance.FindByExpression(eRsv);

                // Nếu không có Rs mới tạo RS - đi số RS đã tạo
                int Person = _NoOfAdult + _NoOfChild + _NoOfChild1 + _NoOfChild2 - aRsv.Count;
                for (int i = 0; i < Person - 1; i++)
                    ReservationUtil.GenCreateRoomShare(ReservationID, UserID);

                return ReservationID;
            }

            throw new Exception("Unhandled split special case.");
        }
        public int SplitAllSpecial(int ReservationID, int pNoOfRoom, int UserID, string PartyGuest)
        {
            int roomID = 0;
            int lastNewReservationID = 0;

            // Lưu lại reservation gốc ban đầu để cuối hàm còn recalculate amount / group cho đúng booking gốc.
            int originalReservationID = ReservationID;

            // Lấy reservation gốc để xác định dữ liệu hiện tại.
            ReservationModel m = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(originalReservationID) ?? throw new Exception("Reservation not found.");

            // Lấy dữ liệu thật từ reservation gốc
            int _ShareRoom = m.ShareRoom;
            int _NoOfAdult = m.NoOfAdult;
            int _NoOfChild = m.NoOfChild;
            int _NoOfChild1 = m.NoOfChild1;
            int _NoOfChild2 = m.NoOfChild2;

            // Lưu lại ProfileIndividualID

            if (pNoOfRoom <= 0)
                throw new Exception("Number of room is invalid.");

            if (pNoOfRoom > 1)
            {
                // Kiểm tra xem Booking này có RoomShare hay không?
                Expression eRsv = new("ShareRoom", _ShareRoom, "=");
                eRsv = eRsv.And(new Expression("MainGuest", 0, "="));
                ArrayList aRsv = ReservationBO.Instance.FindByExpression(eRsv);

                //Tách alll
                int splitLimit = Math.Min(pNoOfRoom, 5); // hoặc 4 nếu đúng nghiệp vụ là 4
                int currentRoomCount = pNoOfRoom;
                int splitCount = 0;

                //Chỉ chạy mỗi lần 5 phòng 
                while (currentRoomCount > 1 && splitCount < splitLimit)
                {
                    int newReservationID = Split(ReservationID, currentRoomCount, UserID, "", roomID);
                    if (newReservationID <= 0)
                        throw new Exception("Split reservation failed.");
                    lastNewReservationID = newReservationID;

                    if (aRsv.Count == 0)
                    {
                        int person = _NoOfAdult + _NoOfChild + _NoOfChild1 + _NoOfChild2;

                        if (currentRoomCount > 2)
                        {
                            for (int i = 0; i < person - 1; i++)
                                ReservationUtil.GenCreateRoomShareNoTransaction(newReservationID, UserID);
                        }
                        else if (currentRoomCount == 2)
                        {
                            for (int i = 0; i < person - 1; i++)
                            {
                                ReservationUtil.GenCreateRoomShareNoTransaction(newReservationID, UserID);
                                ReservationUtil.GenCreateRoomShareNoTransaction(ReservationID, UserID);
                            }
                        }
                    }

                    currentRoomCount--;
                    splitCount++;
                }
                return lastNewReservationID;
            }
            throw new Exception("Unhandled split special case.");
        }

        public void SplitAllRoomSharer(List<int> reservationIds, int UserID)
        {
            if (reservationIds == null || reservationIds.Count == 0)
            {
                throw new Exception("No Reservaion Selected.");
            }
            foreach (int reservationId in reservationIds)
            {
                ReservationModel reservation = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(reservationId);
                if (reservation == null || reservation.ID == 0)
                    continue;

                int person = reservation.NoOfAdult + reservation.NoOfChild
                            + reservation.NoOfChild1 + reservation.NoOfChild2;

                Expression eRsv = new Expression("ShareRoom", reservation.ShareRoom, "=");
                eRsv = eRsv.And(new Expression("MainGuest", 0, "="));
                eRsv = eRsv.And(new Expression("Status", 3, "<>"));
                eRsv = eRsv.And(new Expression("Status", 4, "<>"));
                eRsv = eRsv.And(new Expression("Status", 7, "<>"));

                ArrayList aRsv = ReservationBO.Instance.FindByExpression(eRsv);

                if (reservation.NoOfRoom == 1)
                {
                    int missingSharerCount = (person - aRsv.Count) - 1;
                    if (missingSharerCount > 0)
                    {
                        for (int j = 0; j < missingSharerCount; j++)
                        {
                            ReservationUtil.GenCreateRoomShareNoTransaction(reservation.ID, UserID);
                        }
                    }
                }
            }
        }
        public DataTable ResConfNoList(int confirmationNo)
        {
            try
            {
                SqlParameter[] param =
                [
                    new SqlParameter("@sqlCommand",
                    $@"  SELECT ID,
                            ConfirmationNo AS [ConfNo],
                            CASE WHEN MainGuest = 1 then 'X' ELSE '' END AS [MG],
                            NoOfRoom as [Nbr],
                            Country AS [Nat], 
                            LastName AS [Name], 
                            RoomNo AS [RoNo], 
                            RoomID As [RoomID],
                            RoomType as [RoType],  
                            ArrivalDate as [Arrival], 
                            DepartureDate as [Departure], 
                            NoOfAdult as [A], NoOfChild as [C], 
                            NoOfChild1 as [C1], NoOfChild2 as [C2], 
                            ShareRoom as [SR], dbo.fnGetRsvStatus(Status) 
                            AS Status,  Status as [HKStatusID]
                        FROM Reservation WITH (NOLOCK)
                        WHERE ConfirmationNo = '{confirmationNo}' AND ReservationNo > 0 AND MainGuest = 1 AND (Status = 0 OR Status = 5 OR Status = 1 OR Status = 6) ORDER BY Arrival, [RoNo], [RoType], ID ASC, Nbr DESC")
                        ];
                DataTable dataTable = DataTableHelper.getTableData("spSearchAllForTrans", param);
                return dataTable;

            }
            catch (Exception ex)
            {
                throw new Exception($"ERROR: {ex.Message}", ex);
            }
        }
    }
}

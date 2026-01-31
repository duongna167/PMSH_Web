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

                DataTable myTable = DataTableHelper.getTableData("spSearchAllForTrans", param);
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


    }
}

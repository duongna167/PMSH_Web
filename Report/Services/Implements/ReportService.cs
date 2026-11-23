using Microsoft.Data.SqlClient;
using Report.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaseBusiness.util;
using System.Globalization;
using System.Reflection.Metadata;
using BaseBusiness.BO;
using BaseBusiness.Model;
namespace Report.Services.Implements
{
    public class ReportService : IReportService
    {
        public DataTable GetBookingSourceData(DateTime fromDate, DateTime toDate)
        {
            SqlParameter[] param = new SqlParameter[]
            {
              new SqlParameter("@From", fromDate),
                new SqlParameter("@To", toDate),
            };

            DataTable myTable = DataTableHelper.getTableData("spRptBookingSource", param);
            return myTable;
        }
        public DataTable BlacklistReporteData(DateTime fromDate, DateTime toDate)
        {
            SqlParameter[] param = new SqlParameter[]
            {
              new SqlParameter("@FromDate", fromDate),
                new SqlParameter("@ToDate", toDate),
                  new SqlParameter("@Type", "0"),
            };

            DataTable myTable = DataTableHelper.getTableData("spRptBlacklistReport", param);
            return myTable;
        }
        public DataTable GroupReservation(DateTime fromDate, DateTime toDate, string NoOfRoom)
        {
            SqlParameter[] param = new SqlParameter[]
            {
              new SqlParameter("@FromDate", fromDate),
                new SqlParameter("@ToDate", toDate),
                    new SqlParameter("@NoOfRoom", NoOfRoom),
            };

            DataTable myTable = DataTableHelper.getTableData("spRptGroupReservationReport", param);
            return myTable;
        }
        public DataTable GuestStayOver(DateTime fromDate, DateTime toDate)
        {
            SqlParameter[] param = new SqlParameter[]
            {
              new SqlParameter("@FromDate", fromDate),
                new SqlParameter("@ToDate", toDate)
            };

            DataTable myTable = DataTableHelper.getTableData("spGuestStayOver_New", param);
            return myTable;
        }
        public DataTable GuestStay(string noofName, string stayno, string stayand)
        {
            SqlParameter[] param = new SqlParameter[]
            {
        new SqlParameter("@Name", noofName ?? string.Empty),
        new SqlParameter("@Return", stayno ?? string.Empty),
        new SqlParameter("@ReturnTo", stayand ?? string.Empty)
            };

            DataTable myTable = DataTableHelper.getTableData("spRptReturnGuest", param);
            return myTable;
        }
        public DataTable OTAMonthlyReport(DateTime fromDate, string Number, string type, string currencyID)
        {
            DateTime toDateParsed = fromDate.AddMonths(3);

            // Lấy ngày cuối cùng của tháng
            int lastDay = DateTime.DaysInMonth(toDateParsed.Year, toDateParsed.Month);
            DateTime toDate = new DateTime(toDateParsed.Year, toDateParsed.Month, lastDay);

            SqlParameter[] param = new SqlParameter[]
            {
          new SqlParameter("@FromDate", fromDate),
                new SqlParameter("@ToDate", toDate),
        new SqlParameter("@ProfileID","0"),
        new SqlParameter("@CurrencyID", currencyID),
                new SqlParameter("@Type", type),
                 new SqlParameter("@Number", Number),
            };

            DataTable myTable = DataTableHelper.getTableData("spRptRevenue_MonthlyByHolderReport", param);
            return myTable;
        }
        public DataTable ReportNationalityStatistics(DateTime fromDate, DateTime toDate, string status, string sortOder)
        {
            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@FromMonth", fromDate),
               new SqlParameter("@ToMonth", toDate),
               new SqlParameter("@PrevFromMonth", new DateTime(2024, 4, 1, 0, 0, 0)),
               new SqlParameter("@PrevToMonth", new DateTime(2024, 4, 1, 0, 0, 0)),
               new SqlParameter("@Status", status),
               new SqlParameter("@SortOder", sortOder),
            };

            DataTable myTable = DataTableHelper.getTableData("spRptNationalCompareStatistics", param);
            return myTable;
        }

        public DataTable ArrivalsDetailedData(DateTime fromDate, DateTime toDate, string roomClass, string roomtype, string market, string rateCode, string source, string vip, string viponlycheck, string noPost, int sortOrder,
            string pseudo, string checkedInToday, string cancellations, string zeroRateOnly, int disRoomSharer, int searchCriteria, int ckhArrivalDate)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@FromDate", fromDate),
                new SqlParameter("@ToDate", toDate),
                new SqlParameter("@RoomClass", roomClass),
                new SqlParameter("@RoomType", roomtype),
                new SqlParameter("@Market", market),
                new SqlParameter("@RateCode", rateCode),
                new SqlParameter("@Source", source),
                new SqlParameter("@Pseudo", pseudo),
                new SqlParameter("@CheckedInToday", checkedInToday),
                new SqlParameter("@Cancellations", cancellations),
                new SqlParameter("@VIP", vip),
                new SqlParameter("@SortOrder", sortOrder),
                new SqlParameter("@BusinessDate", DateTime.Parse("2024-08-29 00:00:00")),
                new SqlParameter("@SearchCriteria", searchCriteria),
                new SqlParameter("@CkhArrivalDate", ckhArrivalDate),
                new SqlParameter("@ZeroRateOnly", zeroRateOnly),
                new SqlParameter("@DisRoomSharer", disRoomSharer),
                new SqlParameter("@ChkVIPOnly", viponlycheck),
                new SqlParameter("@NoPost", noPost),
            };


            DataTable myTable = DataTableHelper.getTableData("spRptArrivalDetailed", param);
            return myTable;
        }

        public DataTable ArrivalDetailGroupbyHoldersData(DateTime fromDate, DateTime toDate, string roomClass, string roomtype, string market, string rateCode, string source, string vip, string viponlycheck, string noPost, int sortOrder,
          string pseudo, string checkedInToday, string cancellations, string zeroRateOnly, int disRoomSharer, int searchCriteria, int ckhArrivalDate)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@FromDate", fromDate),
                new SqlParameter("@ToDate", toDate),
                new SqlParameter("@RoomClass", roomClass),
                new SqlParameter("@RoomType", roomtype),
                new SqlParameter("@Market", market),
                new SqlParameter("@RateCode", rateCode),
                new SqlParameter("@Source", source),
                new SqlParameter("@Pseudo", pseudo),
                new SqlParameter("@CheckedInToday", checkedInToday),
                new SqlParameter("@Cancellations", cancellations),
                new SqlParameter("@VIP", vip),
                new SqlParameter("@SortOrder", sortOrder),
                new SqlParameter("@BusinessDate", DateTime.Parse("2024-08-29 00:00:00")),
                new SqlParameter("@SearchCriteria", searchCriteria),
                new SqlParameter("@CkhArrivalDate", ckhArrivalDate),
                new SqlParameter("@ZeroRateOnly", zeroRateOnly),
                new SqlParameter("@DisRoomSharer", disRoomSharer),
                new SqlParameter("@ChkVIPOnly", viponlycheck),
                new SqlParameter("@NoPost", noPost),
            };


            DataTable myTable = DataTableHelper.getTableData("spRptArrivalDetailed", param);
            return myTable;
        }


        public DataTable DepartureReportsData(DateTime fromDate, DateTime toDate, string roomClass, string roomtype, string paymethod, string rateCode, string block,
            string zone, string vip, string viponlycheck, int sortOrder,
           string pseudo, string dueout, string checkout, string disRoomSharer, string specials, string lateCheckOut, string earlyDep, string agents, string company,
           string source, string individuals, string group)
        {
            SqlParameter[] param = new SqlParameter[]
            {
     new SqlParameter("@FromDate", fromDate),
                new SqlParameter("@ToDate", toDate),
                new SqlParameter("@RoomClass", roomClass),
                new SqlParameter("@RoomType", roomtype),
                new SqlParameter("@PaymentMethod", paymethod),
                new SqlParameter("@RateCode", rateCode),
                new SqlParameter("@BlockCode", block),
                new SqlParameter("@Zone", zone),
                new SqlParameter("@VIP", vip),
                new SqlParameter("@ChkVIPOnly", viponlycheck),
                new SqlParameter("@SortOrder", sortOrder),
                new SqlParameter("@PseudoRooms", pseudo),
                new SqlParameter("@DueOut", dueout),
                new SqlParameter("@CheckedOut", checkout),
                new SqlParameter("@DisRoomSharer", disRoomSharer),
                //new SqlParameter("@Specials", specials),
                new SqlParameter("@LateCheckOut", lateCheckOut),
                new SqlParameter("@EarlyDep", earlyDep),
                new SqlParameter("@Agent", agents),
                new SqlParameter("@Company", company),
                new SqlParameter("@Source", source),
                new SqlParameter("@Individual", individuals),
                new SqlParameter("@Group", group)
            };


            DataTable myTable = DataTableHelper.getTableData("spRptDepartures", param);
            return myTable;
        }

        public DataTable AlertsData(DateTime fromDate, DateTime toDate, string viewBy, string altercode)
        {
            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@FromDate", fromDate),
               new SqlParameter("@ToDate", toDate),
               new SqlParameter("@Area", viewBy),
               new SqlParameter("@AlertCode", altercode),
            };

            DataTable myTable = DataTableHelper.getTableData("spRptAlerts", param);
            return myTable;
        }
        public DataTable ReservationSummaryReport(DateTime fromDate, DateTime toDate)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@FromDate", fromDate),
                new SqlParameter("@ToDate", toDate),
            };

            DataTable myTable = DataTableHelper.getTableData("spRptSummaryReservation", param);
            return myTable;
        }
        public DataTable DepositRequestLogReportData(DateTime fromDate, DateTime toDate)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@FromDate", fromDate),
                new SqlParameter("@ToDate", toDate),
            };

            DataTable myTable = DataTableHelper.getTableData("spRptDepositRequestLog", param);
            return myTable;
        }
        public DataTable ArticleByRoomsDatas(DateTime fromDate, DateTime toDate, string zone, string room, int  viewBy, string article)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@dtpFromDate", fromDate),
                new SqlParameter("@dtpToDate", toDate),
                new SqlParameter("@Room", room),
                new SqlParameter("@Article", article),
                new SqlParameter("@RoomClass", ""),
                new SqlParameter("@Transaction", ""),
                new SqlParameter("@NetDisp", ""),
                new SqlParameter("@OrderBy", ""),
                new SqlParameter("@CashierNo", ""),
                new SqlParameter("@Zone", zone),
                new SqlParameter("@PostBy",viewBy),

            };

            DataTable myTable = DataTableHelper.getTableData("spRptJournalByArticleRoom", param);
            return myTable;
        }

        public DataTable DailyMinibarReportData(DateTime fromDate, DateTime toDate, string zone, string room, int viewBy, string article)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@dtpFromDate", fromDate),
                new SqlParameter("@dtpToDate", toDate),
                new SqlParameter("@Room", room),
                new SqlParameter("@Article", article),
                new SqlParameter("@RoomClass", ""),
                new SqlParameter("@Transaction", ""),
                new SqlParameter("@NetDisp", ""),
                new SqlParameter("@OrderBy", ""),
                new SqlParameter("@CashierNo", ""),
                new SqlParameter("@Zone", zone),
                new SqlParameter("@PostBy",viewBy),

            };

            DataTable myTable = DataTableHelper.getTableData("spRptDailyMinibarReport", param);
            return myTable;
        }
        public DataTable JournalByCashierArticleData(DateTime fromDate, DateTime toDate, string transaction, string article, string cashier, string roomclass, string room, string viewBy, string netDisp)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                  new SqlParameter("@dtpFromDate", fromDate),
                  new SqlParameter("@dtpToDate", toDate),
                  new SqlParameter("@Room", room),
                  new SqlParameter("@Article", article),
                  new SqlParameter("@RoomClass", roomclass),
                  new SqlParameter("@Transaction",transaction),
                  new SqlParameter("@NetDisp",netDisp),
                  new SqlParameter("@CashierNo",cashier),
                  new SqlParameter("@OrderBy",viewBy),

            };

            DataTable myTable = DataTableHelper.getTableData("spRptJournalByCashier", param);
            return myTable;
        }
        public DataTable TransferARReportsData(DateTime fromDate)
        {
            SqlParameter[] param = new SqlParameter[]
            {
              new SqlParameter("@dtpFromDate", fromDate),

            };

            DataTable myTable = DataTableHelper.getTableData("spRptTranferAR", param);
            return myTable;
        }
        public DataTable IncurringDepositCollectionData(DateTime fromDate, DateTime toDate, string zone, string cashier)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                  new SqlParameter("@FromDate", fromDate),
                  new SqlParameter("@ToDate", toDate),
                  new SqlParameter("@Zone", zone),
                  new SqlParameter("@Cashier", cashier),
                  new SqlParameter("@TransCode", "8520"),

            };

            DataTable myTable = DataTableHelper.getTableData("spRptIncurringDepositCollection", param);
            return myTable;
        }
        public DataTable IncurringDepositReturnData(DateTime fromDate, DateTime toDate, string zone, string cashier)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                  new SqlParameter("@FromDate", fromDate),
                  new SqlParameter("@ToDate", toDate),
                  new SqlParameter("@Zone", zone),
                  new SqlParameter("@Cashier", cashier),
                  new SqlParameter("@TransCode", "8520"),

            };

            DataTable myTable = DataTableHelper.getTableData("spRptIncurringDepositReturn", param);
            return myTable;
        }
        public DataTable IncurringDepositPaymentPlanData(DateTime fromDate, DateTime toDate, string zone, string cashier,string notbalance)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                  new SqlParameter("@FromDate", fromDate),
                  new SqlParameter("@ToDate", toDate),
                  new SqlParameter("@Zone", zone),
                  new SqlParameter("@Cashier", cashier),
                  new SqlParameter("@TransCode", "8520"),
                  new SqlParameter("@NotBalance", notbalance),

            };

            DataTable myTable = DataTableHelper.getTableData("spRptIncurringDepositPaymentPlan", param);
            return myTable;
        }

        public DataTable IncurringDepositSummaryData(DateTime fromDate, DateTime toDate, string zone, string cashier, string type)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                  new SqlParameter("@FromDate", fromDate),
                  new SqlParameter("@ToDate", toDate),
                  new SqlParameter("@Zone", zone),
                  new SqlParameter("@Cashier", cashier),
                  new SqlParameter("@TransCode", "8520"),
                  new SqlParameter("@Type", type),

            };

            DataTable myTable = DataTableHelper.getTableData("spRptIncurringDepositSummary", param);
            return myTable;
        }
        public DataTable TraceReportView(DateTime fromDate, DateTime toDate, int roomClass, int department, int status, int byAlphabetical, int byRoom, int byVip, int pseudoRoom, int reserved, int checkedIn, int dueout, int individual, int blockcode, int vipOnly)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@FromDate", fromDate),
                new SqlParameter("@ToDate", toDate),
                new SqlParameter("@RoomClassID", roomClass),
                new SqlParameter("@DepartmentID", department),
                new SqlParameter("@Status", status),
                new SqlParameter("@ByAlphabetical", byAlphabetical),
                new SqlParameter("@ByRoom", byRoom),
                new SqlParameter("@ByVIP", byVip),
                new SqlParameter("@IsPseudo", pseudoRoom),
                new SqlParameter("@IsReserved", reserved),
                new SqlParameter("@CheckedIn", checkedIn),
                new SqlParameter("@DueOut", dueout),
                new SqlParameter("@Individual", individual),
                new SqlParameter("@BlockCode", blockcode),
                new SqlParameter("@VIPOnly", vipOnly),
            };

            DataTable myTable = DataTableHelper.getTableData("spRptGuestsInHouseTrace", param);
            return myTable;
        }


        public DataTable ArrivalsandCheckInTodayData(string roomClass, string roomtype, string paymethod, string vip, string viewBy, string pseudo, string chkviponly, int disRoomSharer, string nopost)
        {
            List<BusinessDateModel> businessDateModel = PropertyUtils.ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll());
            var businessDate = businessDateModel[0].BusinessDate;
            SqlParameter[] param = new SqlParameter[]
            {
                   new SqlParameter("@RoomClass", roomClass),
                   new SqlParameter("@RoomType", roomtype),
                   new SqlParameter("@Payment", paymethod),
                   new SqlParameter("@VIP",vip),
                   new SqlParameter("@SortOrder",viewBy),
                   new SqlParameter("@Pseudo", pseudo),
                   new SqlParameter("@ChkVIPOnly",chkviponly),
                   new SqlParameter("@BusinessDate", businessDate.Date),
                   new SqlParameter("@DisRoomSharer", disRoomSharer),
                   new SqlParameter("@NoPost",nopost),
            };

            DataTable myTable = DataTableHelper.getTableData("spRptArrivalAndCheckedInToday", param);
            return myTable;
        }

        public DataTable ReservationPreblockedyData(DateTime fromDate, DateTime toDate, string roomClass, string roomtype, string pseudo, string chkviponly, string individual, string blockcode, string preblocked)
        {
            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomClass", roomClass),
                new SqlParameter("@RoomType", roomtype),
                             new SqlParameter("@FromDate", fromDate),
                new SqlParameter("@ToDate", toDate),

                               new SqlParameter("@PseudoRooms", pseudo),
                new SqlParameter("@VIPOnly", chkviponly),
                             new SqlParameter("@Individual", individual),
                new SqlParameter("@BlockCode", blockcode),
                   new SqlParameter("@PreblockedOnly", preblocked),
            };

            DataTable myTable = DataTableHelper.getTableData("spRptReservationPreblocked", param);
            return myTable;
        }

        public DataTable DepositActivityData(string DueDate_FromDate, string DueDate_ToDate, string Arrival_FromDate, string Arrival_ToDate, string Post_FromDate, string Post_ToDate, string Cashier, int DepositOption, int RsvStatus, int Sort)
        {
            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@DueDate_FromDate", DueDate_FromDate),
                new SqlParameter("@DueDate_ToDate", DueDate_ToDate),
                             new SqlParameter("@Arrival_FromDate", Arrival_FromDate),
                new SqlParameter("@Arrival_ToDate", Arrival_ToDate),

                               new SqlParameter("@Cashier", Cashier),
                new SqlParameter("@Post_FromDate", Post_FromDate),
                             new SqlParameter("@Post_ToDate", Post_ToDate),
                new SqlParameter("@DepositOption", DepositOption),
                   new SqlParameter("@RsvStatus", RsvStatus),
                      new SqlParameter("@Sort", Sort),
            };

            DataTable myTable = DataTableHelper.getTableData("spRptDepositActivity", param);
            return myTable;
        }
        public DataTable TransportationData(DateTime fromDate, DateTime toDate, string transportType, int viewBy, int reservationStatus, int sortByGuestName, int sortByRoom, int sortByTime, int sortByVIP)
        {
            SqlParameter[] param = new SqlParameter[]
            {

                             new SqlParameter("@dtpFromDate", fromDate),
                new SqlParameter("@dtpToDate", toDate),

                               new SqlParameter("@cboviewBy", viewBy),
                new SqlParameter("@TransportType", transportType),
                             new SqlParameter("@ResvStatus", reservationStatus),
                new SqlParameter("@ByGuestName", sortByGuestName),
                   new SqlParameter("@ByRoom", sortByRoom),
                                   new SqlParameter("@ByTime", sortByTime),
                   new SqlParameter("@ByVIP", sortByVIP),
            };

            DataTable myTable = DataTableHelper.getTableData("spRptTransportation", param);
            return myTable;
        }


        public DataTable RatecodebyDate(DateTime fromDate, DateTime toDate, string ratecode)
        {
            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@FromDate", fromDate),
                new SqlParameter("@ToDate", toDate),
                new SqlParameter("@strRateCode", ratecode),
                       new SqlParameter("@strCurrency", "USD"),
                           new SqlParameter("@strRooTypeCode", ""),
            };

            DataTable myTable = DataTableHelper.getTableData("spRptRevenue_RateCodeByDateReport", param);
            return myTable;
        }

        public DataTable DepartureIndividualAndGroupData(DateTime fromDate)
        {
            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@DepartureDate", fromDate),
            };

            DataTable myTable = DataTableHelper.getTableData("spRptDepartureIndividualAndGroup", param);
            return myTable;
        }

        public DataTable BookingSummaryByStatusData(DateTime fromDate, DateTime toDate)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@FromDate", fromDate),
                new SqlParameter("@ToDate", toDate),
            };

            DataTable myTable = DataTableHelper.getTableData("sp_GetReservationStatusSummary_ByDate", param);
            return myTable;
        }

        public DataTable VacantRoomData(string roomClass, string roomtype, string FromRoom, string ToRoom, string OrderByRoomNo, string OrderByHKPStatus, string OrderByFOStatus, string HKPStatus, string FOStatus, string IsGroupByRoomClass)
        {
            DateTime BusinessDate = TextUtils.GetBussinessDateTime();
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@RoomClassID", roomClass),
                new SqlParameter("@RoomTypeID", roomtype),
                 new SqlParameter("@FromRoom", FromRoom),
                new SqlParameter("@ToRoom", ToRoom),

                   new SqlParameter("@OrderByRoomNo", OrderByRoomNo),
                new SqlParameter("@OrderByHKPStatus", OrderByHKPStatus),
                  new SqlParameter("@OrderByFOStatus", OrderByFOStatus),
                new SqlParameter("@HKPStatus", HKPStatus),

                     new SqlParameter("@FOStatus", FOStatus),
                new SqlParameter("@IsGroupByRoomClass", IsGroupByRoomClass),
                       new SqlParameter("@BusinessDate",BusinessDate.Date),
            };

            DataTable myTable = DataTableHelper.getTableData("spRptVacantRoom", param);
            return myTable;
        }

        public DataTable GuestMarketReport(DateTime fromDate, DateTime toDate, string currency, string zonecode)
        {
            DateTime firstDate = new DateTime(DateTime.Now.Year, 1, 1);
            SqlParameter[] param = new SqlParameter[]
            {
              new SqlParameter("@FromDate", fromDate),
                new SqlParameter("@ToDate", toDate),
                    new SqlParameter("@FirstDate", firstDate),
                        new SqlParameter("@Currency", currency),
                            new SqlParameter("@RoomTypeID", zonecode),
            };

            DataTable myTable = DataTableHelper.getTableData("spRptMarketReportFolio", param);
            return myTable;
        }
        public DataTable DailyPickupReport(DateTime fromDate, DateTime toDate,string zone)
        {
            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@Year", fromDate),
                new SqlParameter("@YearTo", toDate),
                  new SqlParameter("@Zone", zone),
            };

            DataTable myTable = DataTableHelper.getTableData("spRptDailyPickup", param);
            return myTable;
        }
        public DataTable DailyBreakfastDetail(DateTime fromDate)
        {
            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@ViewDate", fromDate),
            };

            DataTable myTable = DataTableHelper.getTableData("spRptDailyBreakfastDetail", param);
            return myTable;
        }
        public DataTable FreeUpgradeReport(DateTime fromDate, DateTime toDate, string viewBy, string status)
        {
            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@Status", status),
                        new SqlParameter("@ViewBy", viewBy),
                                          new SqlParameter("@Type", "0"),
            };

            DataTable myTable = DataTableHelper.getTableData("spFreeUpgradeReport", param);
            return myTable;
        }
        public DataTable FixChargeReport(DateTime fromDate, DateTime toDate, string trancode, string status)
        {
            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@Status", status),
                        new SqlParameter("@TransactionCode", trancode)
            };

            DataTable myTable = DataTableHelper.getTableData("spRptFixServices", param);
            return myTable;
        }

        public DataTable ReveunueByData(DateTime fromDate, DateTime toDate, string reservation, string roomType, string zone, string viewBy, string sortOrder)
        {
            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@Searchby", reservation),
                        new SqlParameter("@RoomType", roomType),
                        new SqlParameter("@Zone", zone),
                        new SqlParameter("@ViewBy", viewBy),
                        new SqlParameter("@ViewRate", "1"),
                        new SqlParameter("@SortOrder", sortOrder)
            };

            DataTable myTable = DataTableHelper.getTableData("spRptRevenueBy", param);
            return myTable;
        }
        public DataTable DepartureExtendedReport(DateTime fromDate)
        {
            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@date", fromDate),
            };

            DataTable myTable = DataTableHelper.getTableData("spRptDepartureExtended", param);
            return myTable;
        }
        public DataTable RoomOccupancyReport(DateTime fromDate, DateTime toDate, string zone)
        {
            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@dtpFromDate", fromDate),
               new SqlParameter("@dtpToDate", toDate),
               new SqlParameter("@Zone", zone),
            };

            DataTable myTable = DataTableHelper.getTableData("spRptRoomOccupancy", param);
            return myTable;
        }

        public DataTable ReservationCancellationsReport(DateTime fromDate, DateTime toDate, string commnet, string typeDate, string zone)
        {
            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@FromDate", fromDate),
               new SqlParameter("@ToDate", toDate),
               new SqlParameter("@Reason", commnet),
               new SqlParameter("@TypeDate", typeDate),
               new SqlParameter("@Zone", zone),
            };

            DataTable myTable = DataTableHelper.getTableData("spRptReservationCancellations", param);
            return myTable;
        }

        public DataTable ReservationStatisticsReport(DateTime fromDate,string zone)
        {
            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@Month", fromDate),
                   new SqlParameter("@Zone", zone),
            };

            DataTable myTable = DataTableHelper.getTableData("spRptReservationStatistic", param);
            return myTable;
        }

        public DataTable ReservationbyCompanyReport(DateTime fromDate, DateTime toDate, string roomClass, string roomType, string zone, int  searchCrip, int  sortOrder, string noOfRoom)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@FromDate", fromDate),
                new SqlParameter("@ToDate", toDate),
                new SqlParameter("@RoomClass", roomClass),
                new SqlParameter("@RoomType", roomType),
                new SqlParameter("@Zone", zone),
                new SqlParameter("@SearchCrip", searchCrip),
                new SqlParameter("@SortOrder", sortOrder),
                new SqlParameter("@NoOfRoom", noOfRoom),
                new SqlParameter("@Company", ""),
                new SqlParameter("@Agent", ""),
                new SqlParameter("@Source", ""),
                new SqlParameter("@Group", ""),
                new SqlParameter("@RateCode", ""),
                new SqlParameter("@ReservationType", ""),
            };

            DataTable myTable = DataTableHelper.getTableData("spRptReservationByCompany", param);
            return myTable;
        }

        public DataTable NoShowReportData(DateTime fromDate, DateTime toDate, int roomClass,string zone)
        {
            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@dtpFromDate", fromDate),
                  new SqlParameter("@dtpToDate", toDate),
                     new SqlParameter("@RoomClassID", roomClass),
                                 new SqlParameter("@Zone", zone),
            };

            DataTable myTable = DataTableHelper.getTableData("spRptNoShow", param);
            return myTable;
        }
        public DataTable ReservationSummaryData(DateTime fromDate, DateTime toDate, string roomType, string zone, string viewBy, string market)
        {
            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@dtpFromDate", fromDate),
                  new SqlParameter("@dtpToDate", toDate),
                     new SqlParameter("@Zone", zone),
                           new SqlParameter("@RoomType", roomType),
                                 new SqlParameter("@Market", market),
                                 new SqlParameter("@Currency", viewBy),
            };

            DataTable myTable = DataTableHelper.getTableData("spRptReservationSummaryNew", param);
            return myTable;
        }
        public DataTable ProductActivityData(DateTime fromDate, DateTime toDate, string type, string currency)
        {
            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@FromDate", fromDate),
                  new SqlParameter("@ToDate", toDate),
                     new SqlParameter("@Type", type),
                           new SqlParameter("@CurrencyID", currency),
                                 new SqlParameter("@ProfileID", "0"),
            };

            DataTable myTable = DataTableHelper.getTableData("spRptProductActivity", param);
            return myTable;
        }
        public DataTable NationalStatisticsData(DateTime fromDate, DateTime toDate, string roomtype, string viewBy)
        {
            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@dtpFromDate", fromDate),
                  new SqlParameter("@dtpToDate", toDate),
                     new SqlParameter("@Type", viewBy),
                           new SqlParameter("@RoomType", roomtype),
            };

            DataTable myTable = DataTableHelper.getTableData("spRptNational", param);
            return myTable;
        }

        public DataTable SalesinChargeReportsForm1Data(DateTime fromDate, string viewBy)
        {
            List<BusinessDateModel> businessDateModel = PropertyUtils.ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll());
            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@Year", fromDate),
                     new SqlParameter("@Currency", viewBy),
                       new SqlParameter("@PersonInCharge", ""),
                         new SqlParameter("@Zone", ""),
                                  new SqlParameter("@BusDate",businessDateModel[0].BusinessDate),
            };

            DataTable myTable = DataTableHelper.getTableData("spRptSaleInChargeReport", param);
            return myTable;
        }
        public DataTable SalesinChargeActivityData(DateTime fromDate, DateTime toDate, string type, string currency)
        {
            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@FromDate", fromDate),
                  new SqlParameter("@ToDate", toDate),
                     new SqlParameter("@Type", type),
                           new SqlParameter("@CurrencyID", currency),
                                 new SqlParameter("@ProfileID", "0"),
            };

            DataTable myTable = DataTableHelper.getTableData("spRptSalesInChargeActivity", param);
            return myTable;
        }

        public DataTable RevenueDetailData(DateTime fromDate, DateTime toDate)
        {
            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@dtpFromDate", fromDate),
                           new SqlParameter("@dtpToDate", toDate),
            };

            DataTable myTable = DataTableHelper.getTableData("spRptRevenueFull", param);
            return myTable;
        }

        public DataTable RevenueSummaryData(DateTime fromDate, DateTime toDate)
        {
            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@dtpFromDate", fromDate),
                           new SqlParameter("@dtpToDate", toDate),
            };

            DataTable myTable = DataTableHelper.getTableData("spRptRevenueSummary", param);
            return myTable;
        }

        public DataTable TAProductionReportData(DateTime fromDate, DateTime toDate, string type, string currency)
        {
            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@FromDate", fromDate),
                  new SqlParameter("@ToDate", toDate),
                     new SqlParameter("@Type", type),
                           new SqlParameter("@CurrencyID", currency),
                                 new SqlParameter("@ProfileID", "0"),
            };

            DataTable myTable = DataTableHelper.getTableData("spRptRevenue_HolderProductionReport", param);
            return myTable;
        }
        public DataTable LeadtimeReportsData(DateTime fromDate, DateTime toDate, string zone, string isDaily, string day, string daysNames)
        {
            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@FromDate", fromDate),
                  new SqlParameter("@ToDate", toDate),
                     new SqlParameter("@isDaily", isDaily),
                           new SqlParameter("@Zone", zone),
                                 new SqlParameter("@DaysNames", daysNames),
                                         new SqlParameter("@Days", day),
            };

            DataTable myTable = DataTableHelper.getTableData("spRptLeadTime", param);
            return myTable;
        }

        public DataTable RatecodeReportsData(DateTime fromDate, DateTime toDate, string zone, string rate, string viewby, string day, string daysNames)
        {
            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@FromDate", fromDate),
                  new SqlParameter("@ToDate", toDate),
                     new SqlParameter("@Zone", zone),
                           new SqlParameter("@RateCode", rate),
                                 new SqlParameter("@Currency", viewby),
                                 new SqlParameter("@MonthNames", daysNames),
                                         new SqlParameter("@Months", day),
            };

            DataTable myTable = DataTableHelper.getTableData("spRptRateCodeReport", param);
            return myTable;
        }
        public DataTable AnnualRoomOccupancyData(DateTime fromDate, DateTime toDate, string zone, string day, string daysNames)
        {
            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@FromDate", fromDate),
                  new SqlParameter("@ToDate", toDate),
                     new SqlParameter("@Zone", zone),
                                 new SqlParameter("@MonthNames", daysNames),
                                         new SqlParameter("@Months", day),
            };

            DataTable myTable = DataTableHelper.getTableData("spRptAnnualRoomOccupancy", param);
            return myTable;
        }

        public DataTable RoomMovesData(DateTime fromDate, DateTime toDate)
        {

            SqlParameter[] param = new SqlParameter[]
             {
             new SqlParameter("@FromDate", fromDate),
             new SqlParameter("@ToDate", toDate),
             };

            DataTable myTable = DataTableHelper.getTableData("spRptRoomMoves", param);
            return myTable;
        }
        public DataTable DepositTransferredAtCheckIn(DateTime fromDate)
        {
            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@dtpFromDate", fromDate),
            };

            DataTable myTable = DataTableHelper.getTableData("spRptDepositTransferredAtCheckIn", param);
            return myTable;
        }
        public DataTable RoomDiscrepancy(int Sleep, int Skip, int Person)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Sleep", Sleep),
                new SqlParameter("@Skip", Skip),
                new SqlParameter("@Person", Person)
            };

            return DataTableHelper.getTableData("spRptRoomDescrepancy", parameters);
        }

        public DataTable DepositLedger(DateTime fromDate)
        {
            SqlParameter[] param = new SqlParameter[]
            {
        new SqlParameter("@BusinessDate", SqlDbType.Date) { Value = fromDate.Date }
            };

            DataTable myTable = DataTableHelper.getTableData("spRptDepositLedger", param);
            return myTable;
        }
        public DataTable RevenueReports(DateTime fromDate, int type)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@dtpFromDate", fromDate),
                new SqlParameter("@Type", type)
            };

            return DataTableHelper.getTableData("spRptRevenue", param);
        }
        public DataTable PostingJournalInvoicing(DateTime fromDate, DateTime toDate)
        {

            SqlParameter[] param = new SqlParameter[]
             {
                 new SqlParameter("@FromDate", fromDate),
                 new SqlParameter("@ToDate", toDate),
             };

            DataTable myTable = DataTableHelper.getTableData("spRptPostingJournal", param);
            return myTable;
        }
        public DataTable CancellationJournal(DateTime fromDate, DateTime toDate, string transactionCodeList)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@FromDate", fromDate),
                new SqlParameter("@ToDate", toDate),
                new SqlParameter("@TransactionCodeList", transactionCodeList),
            };

            return DataTableHelper.getTableData("spRptCancellationJournal", param);
        }

        public DataTable TrialBalance(DateTime dtpDate, string currency)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@dtpDate", dtpDate),
                new SqlParameter("@Currency", currency),

            };

            return DataTableHelper.getTableData("spRptTrialBalance", param);

        }
        public DataTable ReservationRateCheck(DateTime date, string status, int ind, int pseudo, int variance,
                                      int fixRate, int package, int dcReason, string sort,
                                      int showFixCharge, int showAlerts)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@date", date),
                new SqlParameter("@status", status),
                new SqlParameter("@ind", ind),
                new SqlParameter("@pseudo", pseudo),
                new SqlParameter("@variance", variance),
                new SqlParameter("@fixRate", fixRate),
                new SqlParameter("@package", package),
                new SqlParameter("@dcReason", dcReason),
                new SqlParameter("@sort", sort),
                new SqlParameter("@showFixCharge", showFixCharge),
                new SqlParameter("@showAlerts", showAlerts),
             };

            return DataTableHelper.getTableData("spRptReservationRateCodeCheck", param);
        }
        public DataTable GuestLedger(DateTime date, string statusList)
        {
            string trimmed = statusList.Trim('\'');
            string formattedStatus = $"{trimmed.Replace("'", "'")}";

            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@Date", date),
                new SqlParameter("@Status", formattedStatus),
            };
            return DataTableHelper.getTableData("spRptGuestLedger", param);
        }
        public DataTable OccupancybyPerson(DateTime fromDate, DateTime toDate, int roomTypeID)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@FromDate", fromDate),
                new SqlParameter("@ToDate", toDate),
                new SqlParameter("@RoomTypeID", roomTypeID)
            };

            return DataTableHelper.getTableData("spRptOccupancyByPerson", param);
        }
        public DataTable SummarybyArticle(DateTime fromDate, DateTime toDate, string transaction, string article, string cashierNo, string roomClass, string room, string orderBy, string netDisp, int isShowDeleted)
        {

            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@dtpFromDate", fromDate),
                new SqlParameter("@dtpToDate", toDate),
                new SqlParameter("@Transaction", ""),
                new SqlParameter("@Article", ""),
                new SqlParameter("@CashierNo", ""),
                new SqlParameter("@RoomClass", ""),
                new SqlParameter("@Room", ""),
                new SqlParameter("@OrderBy", ""),
                new SqlParameter("@NetDisp", "NET"),
                new SqlParameter("@IsShowDeleted", isShowDeleted)
            };

            return DataTableHelper.getTableData("spRptJournalByArticle", param);
        }
        public DataTable ManagerReport(DateTime businessDate, string currency)
        {
            // Chặn lỗi nếu ngày quá nhỏ
            if (businessDate < new DateTime(2, 1, 1))
            {
                businessDate = new DateTime(2, 1, 1); // hoặc bạn có thể return empty DataTable nếu muốn
            }

            DateTime currentBusinessDate = businessDate.AddDays(1);
            DateTime lastBusinessDate = businessDate.AddYears(-1);
            DateTime tomorrow = currentBusinessDate;
            DateTime tomorrowLastYear = tomorrow.AddYears(-1);
            DateTime firstDateYear = new DateTime(businessDate.Year, 1, 1);

            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@BusinessDate", businessDate),
                new SqlParameter("@CurrentBusinessDate", currentBusinessDate),
                new SqlParameter("@LastBusinessDate", lastBusinessDate),
                new SqlParameter("@Tomorrow", tomorrow),
                new SqlParameter("@TomorrowLastYear", tomorrowLastYear),
                new SqlParameter("@FirstDateYEAR", firstDateYear),
                new SqlParameter("@Currency", currency)
            };

            return DataTableHelper.getTableData("spRptManagerReport", param);
        }

        public DataTable CashierSummary(DateTime dtpFromDate, string cboType)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@dtpFromDate", dtpFromDate),
                new SqlParameter("@cboType", cboType)

            };

            return DataTableHelper.getTableData("spRptCahierSummary", param);
        }

        public DataTable DailyRevenueReportNew(DateTime dateView)
        {
            // Lùi 1 năm
            DateTime lastYear = dateView.AddYears(-1);

            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@DateView", dateView),
                new SqlParameter("@LastYear", lastYear)
            };

            return DataTableHelper.getTableData("spRptDailyRevenueReport_New", param);

        }
        public DataTable DailyRevenueReportsV2(DateTime dateView)
        {
            // Lùi 1 năm
            DateTime lastYear = dateView.AddYears(-1);

            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@DateView", dateView),
                new SqlParameter("@LastYear", lastYear)
            };

            return DataTableHelper.getTableData("spRptDailyRevenueReport_V2", param);
        }
        public DataTable CashierAudit(DateTime date, string cashierList, string transactionCodeList, string type)
        {
            // Nếu null hoặc trắng, ta để nguyên null cho stored procedure xử lý
            object cashierParam = string.IsNullOrWhiteSpace(cashierList) ? DBNull.Value : (object)cashierList;
            object transParam = string.IsNullOrWhiteSpace(transactionCodeList) ? DBNull.Value : (object)transactionCodeList;
            object typeParam = string.IsNullOrWhiteSpace(type) ? "0" : (object)type;

            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@Date", date),
                new SqlParameter("@Cashier", cashierParam),
                new SqlParameter("@TransactionCode", transParam),
                new SqlParameter("@Type", typeParam)
            };

            return DataTableHelper.getTableData("spRptCashierAudit_New", param);
        }

        public DataTable RevenueSpa(DateTime fromDate, DateTime toDate)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@dtpFromDate", fromDate),
                new SqlParameter("@dtpToDate", toDate),
     
            };

            return DataTableHelper.getTableData("spRptSpa", param);
        }
        public DataTable StatisticRoomType(DateTime fromDate, DateTime toDate, string roomTypeCsv)
        {
            object roomTypeParam = (object)(roomTypeCsv?.Trim() ?? "");
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@FromDate", fromDate),
                new SqlParameter("@ToDate", toDate),
                new SqlParameter("@RoomType", roomTypeParam),

            };

            return DataTableHelper.getTableData("spRptStatisticRoomType", param);
        }
        public DataTable RoomStatistic(string year, string fromMonth, string toMonth, string fromRoom, string toRoom)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@Year", year),
                new SqlParameter("@FromMonth", fromMonth),
                new SqlParameter("@ToMonth", toMonth),
                new SqlParameter("@FromRoom", fromRoom),
                new SqlParameter("@ToRoom", toRoom),
            };
            return DataTableHelper.getTableData("spRptRoomStatistic", param);
        }
        public DataTable GuestTrialBalance(DateTime date, int isRouting, int isCheckOut, string roomTypeId)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@Date", date),
                new SqlParameter("@IsRounting", isRouting),
                new SqlParameter("@IsCheckOut", isCheckOut),
                new SqlParameter("@RoomTypeID", string.IsNullOrEmpty(roomTypeId) ? "" : roomTypeId),

            };
            return DataTableHelper.getTableData("spRptGuestTrialBalance", param);
        }
        public DataTable RevenueRoomReport(DateTime fromDate, DateTime toDate, string roomTypes, int zone)
        {
            if (string.IsNullOrEmpty(roomTypes)) roomTypes = "";
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@dtpFromDate", fromDate),
                new SqlParameter("@dtpToDate", toDate),
                new SqlParameter("@RoomType", roomTypes),
                new SqlParameter("@Zone", zone),

            };
            return DataTableHelper.getTableData("spRptRevenueRoom", param);
        }
        public DataTable ReservationSearch(int status)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@SearchType", status),
                new SqlParameter("@Name", ""),
                new SqlParameter("@FirstName", ""),
                new SqlParameter("@ReservationHolder", ""),
                new SqlParameter("@ConfirmationNo", ""),
                new SqlParameter("@CRSNo", ""),
                new SqlParameter("@RoomNo", ""),
                new SqlParameter("@RoomType", ""),
                new SqlParameter("@Package", ""),
                new SqlParameter("@Zone", ""),
                new SqlParameter("@ArrivalFrom", ""),
                new SqlParameter("@ArrivalTo", ""),
                new SqlParameter("@RoomSharer", "IncludeRS"),
                new SqlParameter("@CreateDate", ""),
                new SqlParameter("@CreateBy", ""),
                new SqlParameter("@Departure", ""),
                new SqlParameter("@StayOn", ""),
                new SqlParameter("@Market", ""),
                new SqlParameter("@Source", ""),
                new SqlParameter("@ReservationType", ""),
                new SqlParameter("@MemberType", ""),
                new SqlParameter("@ARNo", ""),
                new SqlParameter("@BusinessBlock", ""),
                new SqlParameter("@VIP", ""),
                new SqlParameter("@ChkVIPOnly", ""),
                new SqlParameter("@MasterFolio", ""),
                new SqlParameter("@SpecialUpdatedDate", ""),
                new SqlParameter("@SaleInChagre", ""),
                new SqlParameter("@RateCode", ""),
                new SqlParameter("@IsTransfer", ""),
                new SqlParameter("@Owner", "")
            };

            return DataTableHelper.getTableData("spReservationSearch", param);
        }
        public DataTable ReservationSearchDate(DateTime businessDate, int status)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@SearchType", status),
                new SqlParameter("@ArrivalFrom", businessDate),
                new SqlParameter("@Name", ""),
                new SqlParameter("@FirstName", ""),
                new SqlParameter("@ReservationHolder", ""),
                new SqlParameter("@ConfirmationNo", ""),
                new SqlParameter("@CRSNo", ""),
                new SqlParameter("@RoomNo", ""),
                new SqlParameter("@RoomType", ""),
                new SqlParameter("@Package", ""),
                new SqlParameter("@Zone", ""),
                new SqlParameter("@ArrivalTo", ""),
                new SqlParameter("@RoomSharer", "IncludeRS"),
                new SqlParameter("@CreateDate", ""),
                new SqlParameter("@CreateBy", ""),
                new SqlParameter("@Departure", ""),
                new SqlParameter("@StayOn", ""),
                new SqlParameter("@Market", ""),
                new SqlParameter("@Source", ""),
                new SqlParameter("@ReservationType", ""),
                new SqlParameter("@MemberType", ""),
                new SqlParameter("@ARNo", ""),
                new SqlParameter("@BusinessBlock", ""),
                new SqlParameter("@VIP", ""),
                new SqlParameter("@ChkVIPOnly", ""),
                new SqlParameter("@MasterFolio", ""),
                new SqlParameter("@SpecialUpdatedDate", ""),
                new SqlParameter("@SaleInChagre", ""),
                new SqlParameter("@RateCode", ""),
                new SqlParameter("@IsTransfer", ""),
                new SqlParameter("@Owner", "")
            };

            return DataTableHelper.getTableData("spReservationSearch", param);
        }
        public DataTable RoomFacilityForecastData(DateTime businessDate, DateTime businessDateto, string zone)
        {
            SqlParameter[] param = new SqlParameter[]
            {
        new SqlParameter("@FromDate", businessDate),
         new SqlParameter("@ToDate", businessDateto),
          new SqlParameter("@ZoneCode", zone),

            };

            DataTable myTable = DataTableHelper.getTableData("spRmgRoomFacilityForecastReport", param);
            return myTable;
        }
        public DataTable ReservationSearchRSVDate(DateTime businessDate, int status)
        {
            SqlParameter[] param = new SqlParameter[]
            {
        new SqlParameter("@SearchType", status),
        new SqlParameter("@Name", ""),
        new SqlParameter("@FirstName", ""),
        new SqlParameter("@ReservationHolder", ""),
        new SqlParameter("@ConfirmationNo", ""),
        new SqlParameter("@CRSNo", ""),
        new SqlParameter("@RoomNo", ""),
        new SqlParameter("@RoomType", ""),
        new SqlParameter("@Package", ""),
        new SqlParameter("@Zone", ""),
        new SqlParameter("@ArrivalFrom", businessDate.Year == 1900 ? (object)"" : businessDate),
        new SqlParameter("@ArrivalTo", businessDate.Year == 1900 ? (object)"" : businessDate),
        new SqlParameter("@RoomSharer", "IncludeRS"),
        new SqlParameter("@CreateDate", ""),
        new SqlParameter("@CreateBy", ""),
        new SqlParameter("@Departure", ""),
        new SqlParameter("@StayOn", ""),
        new SqlParameter("@Market", ""),
        new SqlParameter("@Source", ""),
        new SqlParameter("@ReservationType", ""),
        new SqlParameter("@MemberType", ""),
        new SqlParameter("@ARNo", ""),
        new SqlParameter("@BusinessBlock", ""),
        new SqlParameter("@VIP", ""),
        new SqlParameter("@ChkVIPOnly", ""),
        new SqlParameter("@MasterFolio", ""),
        new SqlParameter("@SpecialUpdatedDate", ""),
        new SqlParameter("@SaleInChagre", ""),
        new SqlParameter("@RateCode", ""),
        new SqlParameter("@IsTransfer", ""),
        new SqlParameter("@Owner", "")
            };

            return DataTableHelper.getTableData("spReservationSearch", param);
        }
        public DataTable RoomAvailableNew(DateTime businessDate, string paraDate, string paraDateConvert, string resvType,string Code)
        {
            SqlParameter[] param = new SqlParameter[]
            {
        new SqlParameter("@FromDate", businessDate),
         new SqlParameter("@ToDate", businessDate),
          new SqlParameter("@ParaDate", paraDate),
           new SqlParameter("@ParaDateConvert", paraDateConvert),
            new SqlParameter("@ResvType", resvType),
               new SqlParameter("@NonDeduct", "1"),
                 new SqlParameter("@IncludeOverbook", ""),
                  new SqlParameter("@IncludeAllotment", "1"),
                   new SqlParameter("@ViewbyAllotement", "1"),
                     new SqlParameter("@Zone", Code),
            };

            DataTable myTable = DataTableHelper.getTableData("spRmgRoomAvailableNew", param);
            return myTable;
        }

        private static string Nz(string value) => value ?? "";

        public DataTable GuestBirthdayReportData(DateTime fromDate, DateTime toDate, string reservationStatus, string orderBy)
        {
            SqlParameter[] param =
            {
                new SqlParameter("@dtpFromDate", fromDate),
                new SqlParameter("@dtpToDate", toDate),
                new SqlParameter("@ReservationStatus", string.IsNullOrWhiteSpace(reservationStatus) ? "(1)" : reservationStatus),
                new SqlParameter("@OrderBy", Nz(orderBy)),
            };
            return DataTableHelper.getTableData("spRptGuestBirthday", param);
        }

        public DataTable GuestInHousebyCompanyData(string roomClass, string roomType, string companyId, string pseudo, string sortOrder)
        {
            SqlParameter[] param =
            {
                new SqlParameter("@RoomClass", Nz(roomClass)),
                new SqlParameter("@RoomType", Nz(roomType)),
                new SqlParameter("@CompanyID", string.IsNullOrWhiteSpace(companyId) ? "0" : companyId),
                new SqlParameter("@Pseudo", Nz(pseudo)),
                new SqlParameter("@SortOrder", string.IsNullOrWhiteSpace(sortOrder) ? "0" : sortOrder),
            };
            return DataTableHelper.getTableData("spRptGuestsInHouseByCompany", param);
        }

        public DataTable GuestInHousebyGroupData(DateTime fromDate, DateTime toDate, string roomClass, string blockCode, string vipOnly, string printRate, string sortOrder)
        {
            SqlParameter[] param =
            {
                new SqlParameter("@FromDate", fromDate),
                new SqlParameter("@ToDate", toDate),
                new SqlParameter("@RoomClass", Nz(roomClass)),
                new SqlParameter("@BlockCode", Nz(blockCode)),
                new SqlParameter("@VIPOnly", Nz(vipOnly)),
                new SqlParameter("@PrintRate", Nz(printRate)),
                new SqlParameter("@SortOrder", string.IsNullOrWhiteSpace(sortOrder) ? "0" : sortOrder),
            };
            return DataTableHelper.getTableData("spRptGuestsInHouseByGroup", param);
        }

        public DataTable GuestInHousebyRoomData(string roomClass, string roomType, string payment, string pseudo, string vip, string vipOnly, string individual, string blockCode, int disRoomSharer, string sortOrder, string noPost)
        {
            SqlParameter[] param =
            {
                new SqlParameter("@RoomClass", Nz(roomClass)),
                new SqlParameter("@RoomType", Nz(roomType)),
                new SqlParameter("@Payment", Nz(payment)),
                new SqlParameter("@Pseudo", Nz(pseudo)),
                new SqlParameter("@VIP", Nz(vip)),
                new SqlParameter("@ChkVIPOnly", Nz(vipOnly)),
                new SqlParameter("@Individual", Nz(individual)),
                new SqlParameter("@BlockCode", Nz(blockCode)),
                new SqlParameter("@DisRoomSharer", disRoomSharer),
                new SqlParameter("@SortOrder", string.IsNullOrWhiteSpace(sortOrder) ? "0" : sortOrder),
                new SqlParameter("@NoPost", Nz(noPost)),
            };
            return DataTableHelper.getTableData("spRptGuestsInHouseByRoom", param);
        }

        public DataTable GuestInHouseCashPaymentMethodData(DateTime fromDate)
        {
            return DataTableHelper.getTableData("spRptGuestsInHouseCashPaymentMethod", new[] { new SqlParameter("@FromDate", fromDate) });
        }

        public DataTable GuestInHouseSpecialRequestsData(DateTime fromDate, DateTime toDate, DateTime? departure, int sortOrder, string special)
        {
            SqlParameter[] param =
            {
                new SqlParameter("@FromDate", fromDate),
                new SqlParameter("@ToDate", toDate),
                new SqlParameter("@Departure", departure.HasValue ? departure.Value : (object)DBNull.Value),
                new SqlParameter("@SortOder", sortOrder),
                new SqlParameter("@Special", Nz(special)),
            };
            return DataTableHelper.getTableData("spRptGuestsInHouseSpecialRequests", param);
        }

        public DataTable MessagesGuestsINHAndDueArrivalData(string receiveStatus)
        {
            return DataTableHelper.getTableData("spRptMessage", new[] { new SqlParameter("@ReceiveStatus", Nz(receiveStatus)) });
        }

        public DataTable GuestInHouseComplimentaryAndHouseUseData(bool vipOnly, string vipIds, bool showComp, bool showHU, bool displayComment, string sort)
        {
            SqlParameter[] param =
            {
                new SqlParameter("@vipOnly", vipOnly),
                new SqlParameter("@vipIDs", Nz(vipIds)),
                new SqlParameter("@showComp", showComp),
                new SqlParameter("@showHU", showHU),
                new SqlParameter("@displayComment", displayComment),
                new SqlParameter("@sort", string.IsNullOrWhiteSpace(sort) ? "Room No." : sort),
            };
            return DataTableHelper.getTableData("spRptGuestsInHouseCompHU", param);
        }

        public DataTable GIHComplimentaryData()
        {
            return DataTableHelper.getTableData("spRpt_GIHComplimentary");
        }

        public DataTable HouseKeepingStatusData(string roomClassId, string roomTypeId, string blockCode, DateTime fromDate, DateTime toDate, string orderBy, bool arrivals, bool arrived, bool stayOver, bool dueOut, bool departed, int vipId)
        {
            SqlParameter[] param =
            {
                new SqlParameter("@RoomClassID", Nz(roomClassId)),
                new SqlParameter("@RoomTypeID", Nz(roomTypeId)),
                new SqlParameter("@BlockCode", Nz(blockCode)),
                new SqlParameter("@FromDate", fromDate),
                new SqlParameter("@ToDate", toDate),
                new SqlParameter("@OrderByRoomNo", orderBy == "room"),
                new SqlParameter("@OrderByArrivalTime", orderBy == "arrival"),
                new SqlParameter("@OrderByResvStatus", orderBy == "status"),
                new SqlParameter("@OrderByAlphabetical", orderBy == "name"),
                new SqlParameter("@Arrivals", arrivals),
                new SqlParameter("@Arrived", arrived),
                new SqlParameter("@StayOver", stayOver),
                new SqlParameter("@DueOut", dueOut),
                new SqlParameter("@Departed", departed),
                new SqlParameter("@BusinessDate", fromDate),
                new SqlParameter("@VIPID", vipId),
            };
            return DataTableHelper.getTableData("spRptHouseKeepingStatus", param);
        }

        public DataTable OutOfOrderByPersonData(DateTime fromDate, DateTime toDate, string roomType, string zone, string orderBy, int oooStatus)
        {
            SqlParameter[] param =
            {
                new SqlParameter("@FromDate", fromDate),
                new SqlParameter("@ToDate", toDate),
                new SqlParameter("@RoomType", Nz(roomType)),
                new SqlParameter("@Zone", Nz(zone)),
                new SqlParameter("@OrderBy", string.IsNullOrWhiteSpace(orderBy) ? " order by b.RoomNo" : orderBy),
                new SqlParameter("@OOOStatus", oooStatus),
            };
            return DataTableHelper.getTableData("spRptOOOByReason", param);
        }

        public DataTable DialingInformationData(DateTime fromDate)
        {
            return DataTableHelper.getTableData("spRptDialInformation", new[] { new SqlParameter("@Date", fromDate) });
        }

        public DataTable ProfileAddressData(string profileType, string nameStart, string vipOnly)
        {
            SqlParameter[] param =
            {
                new SqlParameter("@ProfileType", Nz(profileType)),
                new SqlParameter("@NameStart", Nz(nameStart)),
                new SqlParameter("@VIPOnly", string.IsNullOrWhiteSpace(vipOnly) ? "0" : vipOnly),
            };
            return DataTableHelper.getTableData("spRptProfileAddress", param);
        }

        public DataTable ProfileProductivityData(DateTime fromDate, DateTime toDate, string name)
        {
            SqlParameter[] param =
            {
                new SqlParameter("@FromDate", fromDate),
                new SqlParameter("@ToDate", toDate),
                new SqlParameter("@Name", Nz(name)),
            };
            return DataTableHelper.getTableData("spRptProfileProductivity", param);
        }

        public DataTable ComplimentaryGuestReportData(DateTime fromDate)
        {
            return DataTableHelper.getTableData("spRptGuestReport_Compl", new[] { new SqlParameter("@dtpFromDate", fromDate) });
        }

        public DataTable PaidOutReportData(DateTime fromDate, DateTime toDate)
        {
            SqlParameter[] param =
            {
                new SqlParameter("@dtpFromDate", fromDate),
                new SqlParameter("@dtpToDate", toDate),
            };
            return DataTableHelper.getTableData("spRptPaidOut", param);
        }

        public DataTable HistoryAndForecastData(DateTime fromDate, DateTime toDate, string zone)
        {
            SqlParameter[] param =
            {
                new SqlParameter("@FromDate", fromDate),
                new SqlParameter("@ToDate", toDate),
                new SqlParameter("@Zone", Nz(zone)),
            };
            return DataTableHelper.getTableData("spRptHistoryAndForecast", param);
        }

        public DataTable MarketSegmentDetailData(DateTime fromDate, DateTime toDate, string zone)
        {
            SqlParameter[] param =
            {
                new SqlParameter("@FromDate", fromDate),
                new SqlParameter("@ToDate", toDate),
                new SqlParameter("@Zone", Nz(zone)),
            };
            return DataTableHelper.getTableData("spRptMarketSegmentDetailReport", param);
        }

        public DataTable GroupBalanceSearch(string confirmationNo)
        {
            SqlParameter[] param =
            {
                new SqlParameter("@ConfirmationNo", Nz(confirmationNo)),
                new SqlParameter("@Type", SqlDbType.Int) { Value = 0 },
            };
            return DataTableHelper.getTableData("spRptGroupBalance", param);
        }

        public DataTable GroupBalanceDetail(string confirmationNo)
        {
            SqlParameter[] param =
            {
                new SqlParameter("@ConfirmationNo", Nz(confirmationNo)),
                new SqlParameter("@Type", SqlDbType.Int) { Value = 1 },
            };
            return DataTableHelper.getTableData("spRptGroupBalance", param);
        }

        public (string Holder, string GroupName, int TotalRooms, int InHouse) GroupBalanceSummary(string confirmationNo)
        {
            var conf = Nz(confirmationNo);
            string holder = "";
            string groupName = "";
            var search = GroupBalanceSearch(conf);
            if (search != null)
            {
                foreach (DataRow row in search.Rows)
                {
                    var no = Col(row, "ConfirmationNo");
                    if (string.Equals(no, conf, StringComparison.OrdinalIgnoreCase) || search.Rows.Count == 1)
                    {
                        holder = Col(row, "GuestName");
                        groupName = Col(row, "GroupCode");
                        break;
                    }
                }
                if (string.IsNullOrEmpty(holder) && search.Rows.Count > 0)
                {
                    holder = Col(search.Rows[0], "GuestName");
                    groupName = Col(search.Rows[0], "GroupCode");
                }
            }

            int totalRooms = ToInt(Scalar(@"SELECT COUNT(ID) FROM Reservation WITH (NOLOCK)
WHERE ConfirmationNo = @Conf AND MainGuest = 1 AND ReservationNo > 0
AND Status <> 3 AND Status <> 4 AND Status <> 7", conf));
            int inHouse = ToInt(Scalar(@"SELECT COUNT(ID) FROM Reservation WITH (NOLOCK)
WHERE ConfirmationNo = @Conf AND MainGuest = 1 AND ReservationNo > 0
AND (Status = 1 OR Status = 6)", conf));
            return (holder, groupName, totalRooms, inHouse);
        }

        public DataTable ProfileProductionStatisticsData(DateTime fromDate, DateTime toDate, int profileType, int profileId, string roomClassId, string currencyId, bool includeTax, bool rollupMaster, string metrics)
        {
            var selected = (metrics ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(key => ProfileMetricSps.ContainsKey(key))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            DataTable? relations = null;
            if (rollupMaster && profileId > 0)
            {
                relations = DataTableHelper.ExecuteQuery(
                    "SELECT ProfileID, ProfileType FROM ProfileRelationships WITH (NOLOCK) WHERE ProfileParentID = @Id",
                    new[] { new SqlParameter("@Id", profileId) });
            }

            var parts = new DataTable[selected.Length];
            Parallel.For(0, selected.Length, new ParallelOptions { MaxDegreeOfParallelism = Math.Min(4, Math.Max(1, selected.Length)) }, i =>
            {
                var spec = ProfileMetricSps[selected[i]];
                parts[i] = LoadProfileMetric(spec.Sp, fromDate, toDate, profileType, profileId, roomClassId, currencyId, includeTax, rollupMaster, relations);
            });

            var source = new DataTable("Source");
            foreach (var part in parts)
                CopyProfileRows(source, part);
            return source;
        }

        public DataTable ReportHolderLookup()
        {
            return DataTableHelper.ExecuteQuery(@"
SELECT p.ID, p.Account, p.Type
FROM Profile p WITH (NOLOCK)
WHERE p.Type IN (1, 2)
  AND ISNULL(p.Account, '') <> ''
  AND EXISTS (
        SELECT 1 FROM Reservation r WITH (NOLOCK)
        WHERE r.ProfileCompanyID = p.ID OR r.ProfileAgentID = p.ID
  )
ORDER BY p.Account");
        }

        public DataTable BookingbyHolderData(DateTime date, string holder, string market)
        {
            var monthStart = new DateTime(date.Year, date.Month, 1);
            int days = DateTime.DaysInMonth(monthStart.Year, monthStart.Month);
            var dest = new DataTable();
            dest.Columns.Add("RoomType", typeof(string));
            dest.Columns.Add("Zone", typeof(string));
            for (int d = 1; d <= days; d++) dest.Columns.Add(d.ToString(), typeof(int));
            dest.Columns.Add("Total", typeof(int));

            DataTable src;
            try
            {
                src = DataTableHelper.getTableData("spRptBookingbyHolder", new[]
                {
                    new SqlParameter("@Holder", Nz(holder)),
                    new SqlParameter("@Market", Nz(market)),
                    new SqlParameter("@Date", monthStart),
                });
            }
            catch
            {
                return dest;
            }
            if (src == null || src.Rows.Count == 0) return dest;

            foreach (DataRow row in src.Rows)
            {
                var nr = dest.NewRow();
                nr["RoomType"] = Col(row, "RoomType");
                nr["Zone"] = Col(row, "Zone");
                int total = 0;
                for (int d = 1; d <= days; d++)
                {
                    int v = 0;
                    var name = d.ToString();
                    if (row.Table.Columns.Contains(name) && row[name] != DBNull.Value)
                        int.TryParse(Convert.ToString(row[name]), out v);
                    nr[name] = v;
                    total += v;
                }
                nr["Total"] = total;
                dest.Rows.Add(nr);
            }
            return dest;
        }

        private static readonly Dictionary<string, (string Sp, string Caption)> ProfileMetricSps = new(StringComparer.OrdinalIgnoreCase)
        {
            ["roomNights"] = ("spRptProfileStatistic_RoomNights", "Room Nights"),
            ["arrivalRooms"] = ("spRptProfileStatistic_ArrivalRooms", "Arrival Rooms"),
            ["cancelledRooms"] = ("spRptProfileStatistic_CancelledRooms", "Cancelled Rooms"),
            ["noShowRooms"] = ("spRptProfileStatistic_NoShowRooms", "No Show Rooms"),
            ["dayUseRooms"] = ("spRptProfileStatistic_DayUseRooms", "Day Use Rooms"),
            ["roomRevenue"] = ("spRptProfileStatistic_RoomRevenue", "Room Revenue"),
            ["adults"] = ("spRptProfileStatistic_Adults", "Adults"),
            ["children"] = ("spRptProfileStatistic_Children", "Children"),
            ["reservationNights"] = ("spRptProfileStatistic_ReservationNights", "Reservation Nights"),
            ["reservationArrivals"] = ("spRptProfileStatistic_ReservationArrivals", "Reservation Arrivals"),
            ["cancelledReservations"] = ("spRptProfileStatistic_CancelledReservations", "Cancelled Reservations"),
            ["noShowReservations"] = ("spRptProfileStatistic_NoShowReservations", "No Show Reservations"),
            ["dayUseReservations"] = ("spRptProfileStatistic_DayUseReservations", "Day Use Reservations"),
        };

        private static DataTable LoadProfileMetric(string sp, DateTime fromDate, DateTime toDate, int profileType, int profileId, string roomClassId, string currencyId, bool includeTax, bool rollupMaster, DataTable? relations)
        {
            var source = LoadProfileStatistic(sp, fromDate, toDate, profileType, profileId, roomClassId, currencyId, includeTax, rollupMaster);
            if (relations == null || relations.Rows.Count == 0) return source;
            foreach (DataRow rel in relations.Rows)
            {
                int.TryParse(Convert.ToString(rel["ProfileType"]), out var relType);
                int.TryParse(Convert.ToString(rel["ProfileID"]), out var relId);
                var child = LoadProfileStatistic(sp, fromDate, toDate, relType, relId, roomClassId, currencyId, includeTax, false);
                CopyProfileRows(source, child);
            }
            return source;
        }

        private static DataTable LoadProfileStatistic(string sp, DateTime fromDate, DateTime toDate, int profileType, int profileId, string roomClassId, string currencyId, bool includeTax, bool rollupMaster)
        {
            try
            {
                return DataTableHelper.getTableData(sp, new[]
                {
                    new SqlParameter("@FromDate", fromDate),
                    new SqlParameter("@ToDate", toDate),
                    new SqlParameter("@ProfileType", profileType),
                    new SqlParameter("@ProfileID", profileId),
                    new SqlParameter("@RoomClassID", Nz(roomClassId)),
                    new SqlParameter("@CurrencyID", Nz(currencyId)),
                    new SqlParameter("@IncludeTax", includeTax),
                    new SqlParameter("@RollupMaster", rollupMaster),
                }) ?? new DataTable();
            }
            catch
            {
                return new DataTable();
            }
        }

        private static void CopyProfileRows(DataTable source, DataTable temp)
        {
            if (temp == null || temp.Rows.Count == 0) return;
            if (source.Columns.Count == 0)
            {
                foreach (DataColumn c in temp.Columns) source.Columns.Add(c.ColumnName, c.DataType);
            }
            foreach (DataRow row in temp.Rows)
            {
                var nr = source.NewRow();
                foreach (DataColumn c in source.Columns)
                {
                    if (temp.Columns.Contains(c.ColumnName))
                        nr[c.ColumnName] = row[c.ColumnName];
                }
                source.Rows.Add(nr);
            }
        }

        private static string Scalar(string sql, string conf)
        {
            var dt = DataTableHelper.ExecuteQuery(sql, new[] { new SqlParameter("@Conf", conf ?? "") });
            if (dt == null || dt.Rows.Count == 0) return "0";
            return Convert.ToString(dt.Rows[0][0]) ?? "0";
        }

        private static int ToInt(string v)
        {
            int.TryParse(v, out var n);
            return n;
        }

        public DataTable FrequentFlyerData(DateTime fromDate, DateTime toDate, string status, string memberID, int isSortByCardName)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@FromDate", fromDate),
                new SqlParameter("@ToDate", toDate),
                new SqlParameter("@Status", status ?? ""),
                new SqlParameter("@MemberID", memberID ?? ""),
                new SqlParameter("@IsSortByCardName", isSortByCardName)
            };
            return DataTableHelper.getTableData("spRptMember", param);
        }

        public DataTable PackageReportbyPersonData(DateTime fromDate, DateTime toDate)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@FromDate", fromDate),
                new SqlParameter("@ToDate", toDate)
            };
            return DataTableHelper.getTableData("spRptPackageReport_New", param);
        }

        public DataTable AgingbyTypeOnlyData(DateTime date, int accType, string sort)
        {
            var sortBy = string.Equals(sort?.Trim(), "Account Number", StringComparison.OrdinalIgnoreCase)
                ? "Account Number"
                : "Account Name";
            var levels = PropertyUtils.ConvertToList<ARAgingLevelsModel>(ARAgingLevelsBO.Instance.FindAll())
                .OrderBy(x => x.ID)
                .ToList();
            int L(int i, int fallback) => i < levels.Count ? levels[i].AgingValue : fallback;
            SqlParameter[] param =
            {
                new SqlParameter("@accType", accType),
                new SqlParameter("@date", date),
                new SqlParameter("@sort", sortBy),
                new SqlParameter("@level1Max", L(0, 30)),
                new SqlParameter("@level2Max", L(1, 60)),
                new SqlParameter("@level3Max", L(2, 90)),
                new SqlParameter("@level4Max", L(3, 120)),
                new SqlParameter("@level5Max", L(4, -1))
            };
            return DataTableHelper.getTableData("spRptARAgingByAccountType", param);
        }

        public DataTable AgingSummaryByTypeData(DateTime date, string sort)
        {
            var levels = PropertyUtils.ConvertToList<ARAgingLevelsModel>(ARAgingLevelsBO.Instance.FindAll())
                .OrderBy(x => x.ID)
                .ToList();
            int L(int i, int fallback) => i < levels.Count ? levels[i].AgingValue : fallback;
            SqlParameter[] param =
            {
                new SqlParameter("@date", date),
                new SqlParameter("@sort", string.IsNullOrWhiteSpace(sort) ? "AccountType Desc." : sort),
                new SqlParameter("@level1Max", L(0, 30)),
                new SqlParameter("@level2Max", L(1, 60)),
                new SqlParameter("@level3Max", L(2, 90)),
                new SqlParameter("@level4Max", L(3, 120)),
                new SqlParameter("@level5Max", L(4, -1))
            };
            return DataTableHelper.getTableData("spRptARAgingSummaryByAccType", param);
        }

        public DataTable ARAdjustmentData(DateTime fromDate, DateTime toDate)
        {
            return DataTableHelper.getTableData("spRpt_ARAdjustment", new[]
            {
                new SqlParameter("@iFromDate", fromDate),
                new SqlParameter("@iToDate", toDate)
            });
        }

        public DataTable ARCreditLimitData(int option1, int option2, int sort)
        {
            SqlParameter[] param =
            {
                new SqlParameter("@Option1", option1),
                new SqlParameter("@Option2", option2),
                new SqlParameter("@Sort", sort)
            };
            return DataTableHelper.getTableData("spRptARCreditLimit", param);
        }

        public DataTable ARLedgerData(DateTime date)
        {
            return DataTableHelper.getTableData("spRptARLedger", new[] { new SqlParameter("@date", date) });
        }

        public DataTable ARPaymentsData(DateTime date)
        {
            var source = DataTableHelper.getTableData("spRpt_ARPaymentsReport", new[] { new SqlParameter("@iForDate", date) });
            if (source == null || source.Rows.Count == 0) return source;
            for (int i = 0; i < source.Rows.Count - 1; i++)
            {
                int transId = ToInt(Convert.ToString(source.Rows[i]["ARTID"]));
                int invoiceNo = ToInt(Convert.ToString(source.Rows[i]["InvoiceNo"]));
                decimal amount = ToDec(source.Rows[i]["Amount"]);
                decimal amountPaid = ToDec(source.Rows[i]["AmountPaid"]);
                for (int j = i + 1; j < source.Rows.Count; j++)
                {
                    if (transId > 0 && transId == ToInt(Convert.ToString(source.Rows[j]["ARTID"])))
                    {
                        amount += amountPaid;
                        source.Rows[j]["Amount"] = amount;
                        amountPaid = ToDec(source.Rows[j]["AmountPaid"]);
                        if (amount + amountPaid <= 0)
                        {
                            source.Rows[j]["Allocated"] = amount;
                            source.Rows[j]["Unallocated"] = Math.Abs(amount + amountPaid);
                        }
                        else
                        {
                            source.Rows[j]["Allocated"] = Math.Abs(amountPaid);
                            source.Rows[j]["Unallocated"] = 0m;
                        }
                        source.Rows[j]["ARTID"] = -1;
                    }
                    if (invoiceNo == ToInt(Convert.ToString(source.Rows[j]["InvoiceNo"])))
                    {
                        source.Rows[i]["AmountPaid"] = ToDec(source.Rows[i]["AmountPaid"]) + ToDec(source.Rows[j]["AmountPaid"]);
                        source.Rows[j]["ARTID"] = -2;
                    }
                }
            }
            for (int i = source.Rows.Count - 1; i >= 0; i--)
            {
                if (ToInt(Convert.ToString(source.Rows[i]["ARTID"])) == -2)
                    source.Rows[i].Delete();
            }
            source.AcceptChanges();
            return source;
        }

        public DataTable ARTransferReportData(DateTime date)
        {
            return DataTableHelper.getTableData("spRpt_ARTransferReport", new[]
            {
                new SqlParameter("@TransferDate", date),
                new SqlParameter("@CashierName", "")
            });
        }

        public DataTable ProfileUpdateReportData(DateTime date, string confirmationNo, string roomNo, string name)
        {
            return ProfileBO.GetAllProfileUpdate(date, confirmationNo ?? "", roomNo ?? "", name ?? "");
        }

        public DataTable ReservationViewData(int status, string nationality, string city, string country)
        {
            return DataTableHelper.getTableData("spReservationView", new[]
            {
                new SqlParameter("@Status", SqlDbType.Int) { Value = status },
                new SqlParameter("@Nationality", TextUtils.GetSplitString(Nz(nationality))),
                new SqlParameter("@City", TextUtils.GetSplitString(Nz(city))),
                new SqlParameter("@Country", TextUtils.GetSplitString(Nz(country))),
            });
        }

        public DataTable ReservationCommentsData(DateTime fromDate, DateTime toDate, string status, int viewBy, int type)
        {
            return DataTableHelper.getTableData("spReservationComments", new[]
            {
                new SqlParameter("@FromDate", fromDate),
                new SqlParameter("@ToDate", toDate),
                new SqlParameter("@Status", TextUtils.GetSplitString(Nz(status))),
                new SqlParameter("@ViewBy", SqlDbType.Int) { Value = viewBy },
                new SqlParameter("@Type", SqlDbType.Int) { Value = type },
            });
        }

        private static decimal ToDec(object v)
        {
            if (v == null || v == DBNull.Value) return 0m;
            decimal.TryParse(Convert.ToString(v), out var n);
            return n;
        }

        private static string Col(DataRow row, string name)
        {
            if (row?.Table == null || !row.Table.Columns.Contains(name) || row[name] == DBNull.Value) return "";
            return Convert.ToString(row[name]) ?? "";
        }
    }
}

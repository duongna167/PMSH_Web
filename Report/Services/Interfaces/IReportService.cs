using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Report.Services.Interfaces
{
    public interface IReportService
    {
        DataTable GetBookingSourceData(DateTime fromDate, DateTime toDate);
        DataTable GroupReservation(DateTime fromDate, DateTime toDate, string NoOfRoom);
        DataTable GuestStayOver(DateTime fromDate, DateTime toDate);
        DataTable GuestStay(string noofName, string stayno, string stayand);
        DataTable ReportNationalityStatistics(DateTime fromDate, DateTime toDate, string status, string sortOder);
        DataTable ReservationSummaryReport(DateTime fromDate, DateTime toDate);
        DataTable TraceReportView(DateTime fromDate, DateTime toDate, int roomClass, int department, int status, int byAlphabetical, int byRoom, int byVip, int pseudoRoom, int reserved, int checkedIn, int dueout, int individual, int blockcode, int vipOnly);
        DataTable RatecodebyDate(DateTime fromDate, DateTime toDate, string ratecode);
         DataTable GuestMarketReport(DateTime fromDate, DateTime toDate, string currency, string zonecode);
        DataTable OTAMonthlyReport(DateTime fromDate, string Number, string type, string currencyID);
        DataTable DailyPickupReport(DateTime fromDate, DateTime toDate,string zone);
        DataTable DailyBreakfastDetail(DateTime fromDate);
        DataTable FreeUpgradeReport(DateTime fromDate, DateTime toDate, string viewBy, string status);
        DataTable FixChargeReport(DateTime fromDate, DateTime toDate, string trancode, string status);
        DataTable ReveunueByData(DateTime fromDate, DateTime toDate, string reservation, string roomType, string zone, string viewBy, string sortOrder);
        DataTable DepartureExtendedReport(DateTime fromDate);
        DataTable RoomOccupancyReport(DateTime fromDate, DateTime toDate, string zone);
        DataTable ReservationCancellationsReport(DateTime fromDate, DateTime toDate, string commnet, string typeDate, string zone);
        DataTable ReservationStatisticsReport(DateTime fromDate,string zone);
        DataTable ReservationbyCompanyReport(DateTime fromDate, DateTime toDate, string roomClass, string roomType, string zone,int  searchCrip, int  sortOrder, string noOfRoom);
        DataTable NoShowReportData(DateTime fromDate, DateTime toDate, int roomClass, string zone);
        DataTable ReservationSummaryData(DateTime fromDate, DateTime toDate, string roomType, string zone, string viewBy, string market);
        DataTable ProductActivityData(DateTime fromDate, DateTime toDate, string type, string currency);
        DataTable NationalStatisticsData(DateTime fromDate, DateTime toDate, string roomtype, string viewBy);
        DataTable SalesinChargeReportsForm1Data(DateTime fromDate, string viewBy);
        DataTable SalesinChargeActivityData(DateTime fromDate, DateTime toDate, string type, string currency);
        DataTable RevenueDetailData(DateTime fromDate, DateTime toDate);
        DataTable RevenueSummaryData(DateTime fromDate, DateTime toDate);
        DataTable TAProductionReportData(DateTime fromDate, DateTime toDate, string type, string currency);
        DataTable LeadtimeReportsData(DateTime fromDate, DateTime toDate, string zone, string isDaily, string day, string daysNames);
        DataTable RatecodeReportsData(DateTime fromDate, DateTime toDate, string zone, string rate, string viewby, string day, string daysNames);
        DataTable AnnualRoomOccupancyData(DateTime fromDate, DateTime toDate, string zone, string day, string daysNames);
        DataTable RoomMovesData(DateTime fromDate, DateTime toDate);
        DataTable DepositTransferredAtCheckIn(DateTime fromDate);
        DataTable RoomDiscrepancy(int Sleep = 0, int Skip = 0, int Person = 0);
        DataTable DepositLedger(DateTime fromDate);
        DataTable RevenueReports(DateTime fromDate, int type);
        DataTable BlacklistReporteData(DateTime fromDate, DateTime toDate);
        DataTable AlertsData(DateTime fromDate, DateTime toDate, string viewBy, string altercode);
        DataTable ArrivalsandCheckInTodayData(string roomClass, string roomtype, string paymethod, string vip, string viewBy, string pseudo, string chkviponly, int disRoomSharer, string nopost);
        DataTable PostingJournalInvoicing(DateTime fromDate, DateTime toDate);
        DataTable CancellationJournal(DateTime fromDate, DateTime toDate, string transactionCodeList);
        DataTable TrialBalance(DateTime dtpDate, string currency);
        DataTable ReservationRateCheck(DateTime date, string status, int ind, int pseudo, int variance, int fixRate, int package, int dcReason, string sort, int showFixCharge, int showAlerts);
        DataTable GuestLedger(DateTime date, string statusList);
        DataTable OccupancybyPerson(DateTime fromDate, DateTime toDate, int roomTypeID);
        DataTable ReservationPreblockedyData(DateTime fromDate, DateTime toDate, string roomClass, string roomtype, string pseudo, string chkviponly, string individual, string blockcode, string preblocked);
        DataTable TransportationData(DateTime fromDate, DateTime toDate, string transportType, int viewBy, int reservationStatus, int sortByGuestName, int sortByRoom, int sortByTime, int sortByVIP);
        DataTable DepartureIndividualAndGroupData(DateTime fromDate);
        DataTable BookingSummaryByStatusData(DateTime fromDate, DateTime toDate);
        DataTable VacantRoomData(string roomClass, string roomtype, string FromRoom, string ToRoom, string OrderByRoomNo, string OrderByHKPStatus, string OrderByFOStatus, string HKPStatus, string FOStatus, string IsGroupByRoomClass);
        DataTable SummarybyArticle(DateTime fromDate, DateTime toDate, string transaction, string article, string cashierNo, string roomClass, string room, string orderBy, string netDisp, int isShowDeleted);
        DataTable ManagerReport(DateTime businessDate, string currency, bool recalc);
        DataTable CashierSummary(DateTime dtpFromDate, string cboType); 
        DataTable ArrivalsDetailedData(DateTime fromDate, DateTime toDate, string roomClass, string roomtype, string market, string rateCode, string source, string vip, string viponlycheck, string noPost, int sortOrder,
            string pseudo, string checkedInToday, string cancellations, string zeroRateOnly, int disRoomSharer, int searchCriteria, int ckhArrivalDate);
        DataTable ArrivalDetailGroupbyHoldersData(DateTime fromDate, DateTime toDate, string roomClass, string roomtype, string market, string rateCode, string source, string vip, string viponlycheck, string noPost, int sortOrder,
         string  pseudo, string checkedInToday, string cancellations, string zeroRateOnly, int disRoomSharer, int searchCriteria, int ckhArrivalDate);

        DataTable DepartureReportsData(DateTime fromDate, DateTime toDate, string roomClass, string roomtype, string paymethod, string rateCode, string block,
            string zone, string vip, string viponlycheck, int sortOrder,
           string pseudo, string dueout, string checkout, string disRoomSharer, string specials, string lateCheckOut, string earlyDep, string agents, string company,
           string source, string individuals, string group);
        DataTable DepositRequestLogReportData(DateTime fromDate, DateTime toDate);
        DataTable TransferARReportsData(DateTime fromDate);
        DataTable ArticleByRoomsDatas(DateTime fromDate, DateTime toDate, string zone, string room, int viewBy, string article);
        DataTable DailyRevenueReportNew(DateTime dateView);
        DataTable DailyRevenueReportsV2(DateTime dateView);
        DataTable CashierAudit(DateTime date, string cashierList, string transactionCodeList, string type);
        DataTable RevenueSpa(DateTime fromDate, DateTime toDate);
        DataTable StatisticRoomType(DateTime fromDate, DateTime toDate, string roomTypeCsv);
        DataTable JournalByCashierArticleData(DateTime fromDate, DateTime toDate, string transaction, string article, string cashier, string roomclass, string room, string viewBy, string netDisp);
        DataTable IncurringDepositCollectionData(DateTime fromDate, DateTime toDate, string zone, string cashier);
        DataTable IncurringDepositPaymentPlanData(DateTime fromDate, DateTime toDate, string zone, string cashier, string notbalance);
        DataTable IncurringDepositReturnData(DateTime fromDate, DateTime toDate, string zone, string cashier);
        DataTable IncurringDepositSummaryData(DateTime fromDate, DateTime toDate, string zone, string cashier, string type);
        DataTable DepositActivityData(string DueDate_FromDate, string DueDate_ToDate, string Arrival_FromDate, string Arrival_ToDate, string Post_FromDate, string Post_ToDate, string Cashier, int DepositOption, int RsvStatus, int Sort);
        DataTable RoomStatistic(string year, string fromMonth, string toMonth, string fromRoom, string toRoom);
        DataTable GuestTrialBalance(DateTime date, int isRouting, int isCheckOut, string roomTypeId);
        DataTable DailyMinibarReportData(DateTime fromDate, DateTime toDate, string zone, string room, int viewBy, string article);
        DataTable RevenueRoomReport(DateTime fromDate, DateTime toDate, string roomTypes, int zone);
        DataTable ReservationSearch( int status);
        DataTable ReservationSearchDate(DateTime businessDate,int status);
        DataTable RoomFacilityForecastData(DateTime fromDate, DateTime toDate, string zone);
        DataTable ReservationSearchRSVDate(DateTime businessDate, int status);
        DataTable RoomAvailableNew(DateTime businessDate, string paraDate, string paraDateConvert, string resvType,string Code);
        DataTable GuestBirthdayReportData(DateTime fromDate, DateTime toDate, string reservationStatus, string orderBy);
        DataTable GuestInHousebyCompanyData(string roomClass, string roomType, string companyId, string pseudo, string sortOrder);
        DataTable GuestInHousebyGroupData(DateTime fromDate, DateTime toDate, string roomClass, string blockCode, string vipOnly, string printRate, string sortOrder);
        DataTable GuestInHousebyRoomData(string roomClass, string roomType, string payment, string pseudo, string vip, string vipOnly, string individual, string blockCode, int disRoomSharer, string sortOrder, string noPost);
        DataTable GuestInHouseCashPaymentMethodData(DateTime fromDate);
        DataTable GuestInHouseSpecialRequestsData(DateTime fromDate, DateTime toDate, DateTime? departure, int sortOrder, string special);
        DataTable MessagesGuestsINHAndDueArrivalData(string receiveStatus);
        DataTable GuestInHouseComplimentaryAndHouseUseData(bool vipOnly, string vipIds, bool showComp, bool showHU, bool displayComment, string sort);
        DataTable GIHComplimentaryData();
        DataTable HouseKeepingStatusData(string roomClassId, string roomTypeId, string blockCode, DateTime fromDate, DateTime toDate, string orderBy, bool arrivals, bool arrived, bool stayOver, bool dueOut, bool departed, int vipId);
        DataTable OutOfOrderByPersonData(DateTime fromDate, DateTime toDate, string roomType, string zone, string orderBy, int oooStatus);
        DataTable DialingInformationData(DateTime fromDate);
        DataTable ProfileAddressData(string profileType, string nameStart, string vipOnly);
        DataTable ProfileProductivityData(DateTime fromDate, DateTime toDate, string name);
        DataTable ComplimentaryGuestReportData(DateTime fromDate);
        DataTable PaidOutReportData(DateTime fromDate, DateTime toDate);
        DataTable HistoryAndForecastData(DateTime fromDate, DateTime toDate, string zone);
        DataTable MarketSegmentDetailData(DateTime fromDate, DateTime toDate, string zone);
        DataTable GroupBalanceSearch(string confirmationNo);
        DataTable GroupBalanceDetail(string confirmationNo);
        (string Holder, string GroupName, int TotalRooms, int InHouse) GroupBalanceSummary(string confirmationNo);
        DataTable ProfileProductionStatisticsData(DateTime fromDate, DateTime toDate, int profileType, int profileId, string roomClassId, string currencyId, bool includeTax, bool rollupMaster, string metrics);
        DataTable BookingbyHolderData(DateTime date, string holder, string market);
        DataTable ReportHolderLookup();
        DataTable FrequentFlyerData(DateTime fromDate, DateTime toDate, string status, string memberID, int isSortByCardName);
        DataTable PackageReportbyPersonData(DateTime fromDate, DateTime toDate);
        DataTable ARTraceData(DateTime fromDate, DateTime toDate, string state);
        DataTable ReservationWithDiscountData(DateTime fromDate, DateTime toDate, int dateType,
            string rmClasses, string rmTypes, string rateIDs, string mkIDs, string srcIDs, string statuses, int sort);
        DataTable AgingbyTypeOnlyData(DateTime date, int accType, string sort);
        DataTable AgingSummaryByTypeData(DateTime date, string sort);
        DataTable ARAdjustmentData(DateTime fromDate, DateTime toDate);
        DataTable ARCreditLimitData(int option1, int option2, int sort);
        DataTable ARLedgerData(DateTime date);
        DataTable ARPaymentsData(DateTime date);
        DataTable ARTransferReportData(DateTime date);
        DataTable ProfileUpdateReportData(DateTime date, string confirmationNo, string roomNo, string name);
        DataTable ReservationViewData(int status, string nationality, string city, string country);
        DataTable ReservationCommentsData(DateTime fromDate, DateTime toDate, string status, int viewBy, int type);
    }

}

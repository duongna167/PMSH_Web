using BaseBusiness.util;
using HouseKeeping.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Policy;

namespace HouseKeeping.Services.Implements
{
    public class HouseKeepingService : IHouseKeepingService
    {
        public DataTable RoomControlPanelData(DateTime fromDate, DateTime toDate, string zone)
        {
            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@FromDate", fromDate),
                new SqlParameter("@ToDate", toDate),
                 new SqlParameter("@ZoneCode", zone),
            };

            DataTable myTable = DataTableHelper.getTableData("spRmgRoomControlPanelReport", param);
            return myTable;
        }

        public DataTable RoomFacilityForecastData(DateTime fromDate, DateTime toDate, string zone)
        {
            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@FromDate", fromDate),
                new SqlParameter("@ToDate", toDate),
                 new SqlParameter("@ZoneCode", zone),

            };

            DataTable myTable = DataTableHelper.getTableData("spRmgRoomFacilityForecastReport", param);
            return myTable;
        }
        public DataTable GuestServiceStatusData(int servicestatsu, string room, string roomStatus, string zone)
        {
            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@roomStatus", roomStatus),
                new SqlParameter("@serviceStatus", servicestatsu),
                 new SqlParameter("@roomNo", room),
                   new SqlParameter("@Zone", zone),
            };

            DataTable myTable = DataTableHelper.getTableData("spGetRoomOCC", param);
            return myTable;
        }
        public DataTable RoomStatusData(int cleannon_room, int clean, int dirty, int pickup, int oocheck, int oscheck, int vacant, int occupied, int arrivals, int arrived, int stayover, int dayuse, int dueout, int departed, int notReserved, int departuredarr, string roomType, string zone, string roomFrom, string roomTo)
        {
            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@Clean", cleannon_room),
                new SqlParameter("@Dirty", dirty),
                 new SqlParameter("@Pickup", pickup),
                        new SqlParameter("@Inspected", clean),

                          new SqlParameter("@OOOrder", oocheck),
                new SqlParameter("@OOService", oscheck),
                 new SqlParameter("@Vacant", vacant),
                        new SqlParameter("@Occupied", occupied),

                             new SqlParameter("@Arrivals", arrivals),
                new SqlParameter("@Arrived", arrived),
                 new SqlParameter("@Stayover", stayover),
                        new SqlParameter("@DayUse", dayuse),
                          new SqlParameter("@DueOut", dueout),
                new SqlParameter("@Departed", departed),
                 new SqlParameter("@NotReserved", notReserved),
                        new SqlParameter("@Departured", departuredarr),
                          new SqlParameter("@CurrentDate", DateTime.Now),
            };

            DataTable myTable = DataTableHelper.getTableData("spHkpRoomStatus", param);
            return myTable;
        }

        public DataTable CheckLogStatus(string RoomNo, DateTime fromDate, DateTime toDate, string username)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomNo", RoomNo),
                new SqlParameter("@UserName",username),
                 new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),

            };

            DataTable myTable = DataTableHelper.getTableData("spRoomStatusHistory", param);
            return myTable;
        }
        public DataTable RoomPlanData(DateTime fromDate, DateTime toDate, int orderbyroom, string owner)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@Owner", owner),
                new SqlParameter("@OrderByRoom",orderbyroom),
                 new SqlParameter("@MinDate", fromDate),
                        new SqlParameter("@MaxDate", toDate),


            };

            DataTable myTable = DataTableHelper.getTableData("spRmgRoomPlan", param);
            return myTable;
        }
        public DataTable SummaryTotalPhysicalRoom(string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone)
     ,


            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusSummaryTotalPhysicalRoom", param);
            return myTable;
        }
        public DataTable StatusSummaryOutOfOrder(DateTime datebunisess, string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone) ,
                 new SqlParameter("@BusinessDate",datebunisess)


            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusSummaryOutOfOrder", param);
            return myTable;
        }
        public DataTable SummaryOutOfService(DateTime datebunisess, string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone) ,
                 new SqlParameter("@BusinessDate",datebunisess)


            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusSummaryOutOfService", param);
            return myTable;
        }
        public DataTable ActivityStayOver(DateTime datebunisess, string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone) ,
                 new SqlParameter("@BusinessDate",datebunisess)


            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusActivityStayOver", param);
            return myTable;
        }
        public DataTable ActivityDepartureExpected(DateTime datebunisess, string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone) ,
                 new SqlParameter("@BusinessDate",datebunisess)


            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusActivityDepartureExpected", param);
            return myTable;
        }

        public DataTable ActivityDepartureActual(DateTime datebunisess, string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone) ,
                 new SqlParameter("@BusinessDate",datebunisess)


            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusActivityDepartureActual", param);
            return myTable;
        }

        public DataTable ActivityArrivalExpected(DateTime datebunisess, string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone) ,
                 new SqlParameter("@BusinessDate",datebunisess)


            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusActivityArrivalExpected", param);
            return myTable;
        }
        public DataTable ActivityArrivalActual(DateTime datebunisess, string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone) ,
                 new SqlParameter("@BusinessDate",datebunisess)


            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusActivityArrivalActual", param);
            return myTable;
        }
        public DataTable TaskSheetStatusData(DateTime fromDate, DateTime toDate, string attendant, string room, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@FromDate", fromDate),
                new SqlParameter("@ToDate",toDate) ,
                 new SqlParameter("@AttendantID",attendant),
                    new SqlParameter("@RoomNo",room),
                        new SqlParameter("@Zone",zone)
            };

            DataTable myTable = DataTableHelper.getTableData("spTasksheetStatusScreenSearch", param);
            return myTable;
        }
        public DataTable TaskAssignmentData(DateTime fromDate, string tasksheet, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@TaskDate", fromDate),
                new SqlParameter("@TasksheetNo",tasksheet) ,
                 new SqlParameter("@Status",""),
                    new SqlParameter("@Zone",zone)

            };

            DataTable myTable = DataTableHelper.getTableData("spTaskAssignmentSearch", param);
            return myTable;
        }

        public DataTable RoomAttendentDailyWorksheetData(DateTime fromDate, string attendant, string tasksheet, string roomStatus, string facilityCode)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@BusinessDate", fromDate),
                new SqlParameter("@AttendantID",attendant) ,
                 new SqlParameter("@TasksheetNo",tasksheet),
                   new SqlParameter("@HKStatusID",roomStatus),
                    new SqlParameter("@FacilityCode",facilityCode),
            };

            DataTable myTable = DataTableHelper.getTableData("spTurndownTasksheetDailySearchV2Web", param);
            return myTable;
        }
        public DataTable TurndownTasksheet(DateTime fromDate, string attendant, string roomStatus)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@BusinessDate", fromDate),
                new SqlParameter("@AttendantID",attendant) ,
                   new SqlParameter("@HKStatusID",roomStatus),
            };

            DataTable myTable = DataTableHelper.getTableData("spRptTurndownTasksheet", param);
            return myTable;
        }
        public DataTable AutoMakeupServiceRoom(string roomIDs, string _ListSection)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@SectionID", _ListSection),
                new SqlParameter("@RoomNo",roomIDs) ,
            };

            DataTable myTable = DataTableHelper.getTableData("spTurndownTasksheets", param);
            return myTable;
        }
        public DataTable HKPGetTaskSheets(DateTime BusinessDate, int page, string zoneexpan, string taskcodeExpanded, string hkpSectionExpanded)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@date", BusinessDate),
                new SqlParameter("@page",page) ,
                 new SqlParameter("@zoneID",zoneexpan),
                    new SqlParameter("@facilityTask",taskcodeExpanded),
                              new SqlParameter("@sectionIDs",hkpSectionExpanded)

            };

            DataTable myTable = DataTableHelper.getTableData("spHKPGetTaskSheets", param);
            return myTable;
        }
        public DataTable HKPTurndownTaskSheetGrid(string taskid, string status)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@taskID", taskid),
                new SqlParameter("@status",status) ,

            };

            DataTable myTable = DataTableHelper.getTableData("spHKPTurndownTaskSheetGrid", param);
            return myTable;
        }
        public DataTable ActivityExtendedStay(DateTime datebunisess, string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone) ,
                 new SqlParameter("@BusinessDate",datebunisess)


            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusActivityExtendedStay", param);
            return myTable;
        }
        public DataTable RoomAttendantDailyWorkSheet(DateTime fromDatere, DateTime toDatere, string taskcodeexpan, string attendantrepop, string tasksheetpopre, string dueoutonly)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@FromDate", fromDatere),
                new SqlParameter("@ToDate",toDatere) ,
                 new SqlParameter("@FacilityTask",""),
                 new SqlParameter("@AttendantID",attendantrepop),
                 new SqlParameter("@TaskSheetNo",tasksheetpopre),
                 new SqlParameter("@DueOutOnly",dueoutonly),
            };

            DataTable myTable = DataTableHelper.getTableData("spRptRoomAttendantDailyWorkSheet", param);
            return myTable;
        }
        public DataTable RAWorkSheet(DateTime fromDatere, DateTime toDatere, string taskcodeexpan, string attendantrepop, string tasksheetpopre, string dueoutonly)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@FromDate", fromDatere),
                new SqlParameter("@ToDate",toDatere) ,
                 new SqlParameter("@FacilityTask",""),
                 new SqlParameter("@AttendantID",attendantrepop),
                 new SqlParameter("@TaskSheetNo",tasksheetpopre),
                 new SqlParameter("@DueOutOnly",dueoutonly),
            };

            DataTable myTable = DataTableHelper.getTableData("spRptRAWorkSheet", param);
            return myTable;
        }
        public DataTable SupChecklistFloor(DateTime fromDatere, DateTime toDatere, string taskcodeexpan, string attendantrepop, string tasksheetpopre, string dueoutonly)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@FromDate", fromDatere),
                new SqlParameter("@ToDate",toDatere) ,
                 new SqlParameter("@FacilityTask",""),
                 new SqlParameter("@AttendantID",attendantrepop),
                 new SqlParameter("@TaskSheetNo",tasksheetpopre),
                 new SqlParameter("@DueOutOnly",dueoutonly),
            };

            DataTable myTable = DataTableHelper.getTableData("spRptSupChecklistFloor", param);
            return myTable;
        }
        public DataTable ViewTurnDown(string roomTypead, string sectionAd, string zoneAd, string fromRoom, string toRoom, string HKStatusID, string ReservationStatus, string arrived, string turndownStatus)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomType", roomTypead),
                new SqlParameter("@Section",sectionAd) ,
                 new SqlParameter("@Zone",zoneAd),
                 new SqlParameter("@FromRoom",fromRoom),
                 new SqlParameter("@ToRoom",toRoom),
                 new SqlParameter("@HKStatusID",HKStatusID),

                         new SqlParameter("@ReservationStatus",ReservationStatus),
                 new SqlParameter("@TurndownStatus",turndownStatus),
                 new SqlParameter("@RsvArrivals",arrived),
            };

            DataTable myTable = DataTableHelper.getTableData("spTurndownManagement", param);
            return myTable;
        }
        public DataTable ActivityEarlyDeparture(DateTime datebunisess, string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone) ,
                 new SqlParameter("@BusinessDate",datebunisess)


            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusActivityEarlyDeparture", param);
            return myTable;
        }
        public DataTable TasksheetDetailsSearch(int ID)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@TasksheetID", ID),
                new SqlParameter("@AttendantID","") ,


            };

            DataTable myTable = DataTableHelper.getTableData("spTasksheetDetailsSearch", param);
            return myTable;
        }
        public DataTable ActivityDayUseRoom(DateTime datebunisess, string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone) ,
                 new SqlParameter("@BusinessDate",datebunisess)


            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusActivityDayUseRoom", param);
            return myTable;
        }
        public DataTable StatusHKInspected(string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone) ,


            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusHKInspected", param);
            return myTable;
        }
        public DataTable StatusHKClean(string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone) ,



            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusHKClean", param);
            return myTable;
        }
        public DataTable StatusHKDirty(string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone) ,

            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusHKDirty", param);
            return myTable;
        }
        public DataTable StatusHKOutOfOrder(string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone) ,

            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusHKOutOfOrder", param);
            return myTable;
        }
        public DataTable StatusHKOutOfService(string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone) ,

            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusHKOutOfService", param);
            return myTable;
        }

        public DataTable StatusEndOfDayGroupAndBlock(DateTime datebunisess, string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone) ,
                  new SqlParameter("@BusinessDate",datebunisess)
            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusEndOfDayGroupAndBlock", param);
            return myTable;
        }

        public DataTable StatusEndOfDayIndividual(DateTime datebunisess, string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone) ,
                  new SqlParameter("@BusinessDate",datebunisess)
            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusEndOfDayIndividual", param);
            return myTable;
        }
        public DataTable StatusEndOfDayCHU(DateTime datebunisess, string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone) ,
                  new SqlParameter("@BusinessDate",datebunisess)
            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusEndOfDayCHU", param);
            return myTable;
        }
        public DataTable StatusEndOfDayMaxOccTonight(DateTime datebunisess, string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone) ,
                  new SqlParameter("@BusinessDate",datebunisess)
            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusEndOfDayMaxOccTonight", param);
            return myTable;
        }
        public DataTable StatusEndOfDayRoomRevenue(DateTime datebunisess, string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone) ,
                  new SqlParameter("@BusinessDate",datebunisess)
            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusEndOfDayRoomRevenue", param);
            return myTable;
        }
        public DataTable StatusEndOfDayMaxOccTonightVIP(DateTime datebunisess, string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone) ,
                  new SqlParameter("@BusinessDate",datebunisess)
            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusEndOfDayMaxOccTonightVIP", param);
            return myTable;
        }
        public DataTable StatusEndOfDayIndividualVIP(DateTime datebunisess, string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone) ,
                  new SqlParameter("@BusinessDate",datebunisess)
            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusEndOfDayIndividualVIP", param);
            return myTable;
        }
        public DataTable StatusEndOfDayGroupAndBlockVIP(DateTime datebunisess, string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone) ,
                  new SqlParameter("@BusinessDate",datebunisess)
            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusEndOfDayGroupAndBlockVIP", param);
            return myTable;
        }

        public DataTable StatusEndOfDayCHUVIP(DateTime datebunisess, string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone) ,
                  new SqlParameter("@BusinessDate",datebunisess)
            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusEndOfDayCHUVIP", param);
            return myTable;
        }
        public DataTable StatusActivityDepartureExpectedVIP(DateTime datebunisess, string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone) ,
                  new SqlParameter("@BusinessDate",datebunisess)
            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusActivityDepartureExpectedVIP", param);
            return myTable;
        }

        public DataTable StatusActivityStayOverVIP(DateTime datebunisess, string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone) ,
                  new SqlParameter("@BusinessDate",datebunisess)
            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusActivityStayOverVIP", param);
            return myTable;
        }

        public DataTable StatusActivityDepartureActualVIP(DateTime datebunisess, string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone) ,
                  new SqlParameter("@BusinessDate",datebunisess)
            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusActivityDepartureActualVIP", param);
            return myTable;
        }
        public DataTable StatusActivityArrivalExpectedVIP(DateTime datebunisess, string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone) ,
                  new SqlParameter("@BusinessDate",datebunisess)
            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusActivityArrivalExpectedVIP", param);
            return myTable;
        }
        public DataTable StatusActivityArrivalActualVIP(DateTime datebunisess, string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone) ,
                  new SqlParameter("@BusinessDate",datebunisess)
            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusActivityArrivalActualVIP", param);
            return myTable;
        }

        public DataTable StatusActivityExtendedStayVIP(DateTime datebunisess, string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone) ,
                  new SqlParameter("@BusinessDate",datebunisess)
            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusActivityExtendedStayVIP", param);
            return myTable;
        }
        public DataTable StatusActivityEarlyDepartureVIP(DateTime datebunisess, string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone) ,
                  new SqlParameter("@BusinessDate",datebunisess)
            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusActivityEarlyDepartureVIP", param);
            return myTable;
        }
        public DataTable StatusActivityDayUseRoomVIP(DateTime datebunisess, string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone) ,
                  new SqlParameter("@BusinessDate",datebunisess)
            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusActivityDayUseRoomVIP", param);
            return myTable;
        }

        public DataTable StatusActivityWakeInRoomVIP(DateTime datebunisess, string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone) ,
                  new SqlParameter("@BusinessDate",datebunisess)
            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusActivityWakeInRoomVIP", param);
            return myTable;
        }

        public DataTable StatusActivityWalkInRoom(DateTime datebunisess, string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone) ,
                  new SqlParameter("@BusinessDate",datebunisess)
            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusActivityWalkInRoom", param);
            return myTable;
        }
        public DataTable StatusSummaryOutOfOrderDetail(DateTime datebunisess, string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone) ,
                  new SqlParameter("@BusinessDate",datebunisess)
            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusSummaryOutOfOrderDetail", param);
            return myTable;
        }
        public DataTable StatusSummaryOutOfServiceDetail(DateTime datebunisess, string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone) ,
                  new SqlParameter("@BusinessDate",datebunisess)
            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusSummaryOutOfServiceDetail", param);
            return myTable;
        }
        public DataTable StatusActivityStayOverDetail(DateTime datebunisess, string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone) ,
                  new SqlParameter("@BusinessDate",datebunisess)
            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusActivityStayOverDetail", param);
            return myTable;
        }
        public DataTable StatusActivityDepartureExpectedDetail(DateTime datebunisess, string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone) ,
                  new SqlParameter("@BusinessDate",datebunisess)
            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusActivityDepartureExpectedDetail", param);
            return myTable;
        }
        public DataTable StatusActivityDepartureActualDetail(DateTime datebunisess, string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone) ,
                  new SqlParameter("@BusinessDate",datebunisess)
            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusActivityDepartureActualDetail", param);
            return myTable;
        }
        public DataTable StatusActivityArrivalExpectedDetail(DateTime datebunisess, string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone) ,
                  new SqlParameter("@BusinessDate",datebunisess)
            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusActivityArrivalExpectedDetail", param);
            return myTable;
        }
        public DataTable StatusActivityArrivalActualDetail(DateTime datebunisess, string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone) ,
                  new SqlParameter("@BusinessDate",datebunisess)
            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusActivityArrivalActualDetail", param);
            return myTable;
        }
        public DataTable StatusActivityExtendedStayDetail(DateTime datebunisess, string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone) ,
                  new SqlParameter("@BusinessDate",datebunisess)
            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusActivityExtendedStayDetail", param);
            return myTable;
        }
        public DataTable StatusActivityEarlyDepartureDetail(DateTime datebunisess, string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone) ,
                  new SqlParameter("@BusinessDate",datebunisess)
            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusActivityEarlyDepartureDetail", param);
            return myTable;
        }
        public DataTable StatusActivityDayUseRoomDetail(DateTime datebunisess, string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone) ,
                  new SqlParameter("@BusinessDate",datebunisess)
            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusActivityDayUseRoomDetail", param);
            return myTable;
        }
        public DataTable StatusActivityWalkInRoomDetail(DateTime datebunisess, string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone) ,
                  new SqlParameter("@BusinessDate",datebunisess)
            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusActivityWalkInRoomDetail", param);
            return myTable;
        }
        public DataTable StatusHKVacantCleanDetail(string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone) ,
            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusHKVacantCleanDetail", param);
            return myTable;
        }
        public DataTable StatusHKInspectedOCCDetail(string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone) ,
            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusHKInspectedOCCDetail", param);
            return myTable;
        }
        public DataTable StatusHKVCNDetail(string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone) ,
            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusHKVCNDetail", param);
            return myTable;
        }
        public DataTable StatusHKCleanOCCDetail(string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone) ,
            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusHKCleanOCCDetail", param);
            return myTable;
        }
        public DataTable StatusActivityDueOutDetail(string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone) ,
            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusActivityDueOutDetail", param);
            return myTable;
        }
        public DataTable StatusHKVacantDirtyDetail(string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone) ,
            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusHKVacantDirtyDetail", param);
            return myTable;
        }
        public DataTable StatusHKDirtyOCCDetail(string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone) ,
            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusHKDirtyOCCDetail", param);
            return myTable;
        }
        public DataTable StatusHKOutOfOrderVacantDetail(string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone) ,
            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusHKOutOfOrderVacantDetail", param);
            return myTable;
        }
        public DataTable StatusHKOutOfServiceVacantDetail(string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone) ,
            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusHKOutOfServiceVacantDetail", param);
            return myTable;
        }
        public DataTable StatusHKOutOfServiceOCCDetail(string roomtype, string zone)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@RoomTypeID", roomtype),
                new SqlParameter("@ZoneID",zone) ,
            };

            DataTable myTable = DataTableHelper.getTableData("spRmgStatusHKOutOfServiceOCCDetail", param);
            return myTable;
        }
        public DataTable TurndownTasksheetData(DateTime fromDate, string attendant, string IsDueOut, string roomStatus)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@BusinessDate", fromDate),
                new SqlParameter("@AttendantID",attendant) ,
                  new SqlParameter("@IsDueOut", IsDueOut),
                new SqlParameter("@HKStatusID",roomStatus) ,
            };

            DataTable myTable = DataTableHelper.getTableData("spTurndownTasksheetSearch", param);
            return myTable;
        }
        public DataTable TasksheetAutomatically(string HK_FO, DateTime taskdateauto, string floorauto, string roomTypeauto, string zonecodeauto, string subzonecodeauto, string includeroomAS, string arrivalOnly, string _ListRoomNotAss)
        {

            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@HKStatusID", HK_FO),
                new SqlParameter("@FOStatus","") ,
               new SqlParameter("@RoomType",roomTypeauto) ,
                new SqlParameter("@IncludeAss",includeroomAS) ,
                 new SqlParameter("@SectionID","") ,
                  new SqlParameter("@IsDueOut","") ,
                   new SqlParameter("@ArrivalOnly",arrivalOnly) ,

                      new SqlParameter("@DueOut","") ,
                   new SqlParameter("@Zone",zonecodeauto) ,
                    new SqlParameter("@RoomNotAssign",_ListRoomNotAss) ,

                         new SqlParameter("@Floor",floorauto) ,
                   new SqlParameter("@subZone",subzonecodeauto) ,
            };

            DataTable myTable = DataTableHelper.getTableData("spTasksheetAutomatically", param);
            return myTable;
        }
        public DataTable LostAndFound(DateTime fromDate, DateTime toDate)
        {
            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@FromDate", fromDate),
               new SqlParameter("@ToDate", toDate),
            };

            DataTable myTable = DataTableHelper.getTableData("spSearchLostAndFound", param);
            return myTable;
        }
        public DataTable RoomAvailabilityData(DateTime fromDate, DateTime toDate, string zone, string columnsString, string expressionString, string idList, string includeOverbooking, string stringincludeAllotment)
        {
            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@FromDate", fromDate),
               new SqlParameter("@ToDate", toDate),
               new SqlParameter("@Zone", zone),
               new SqlParameter("@ParaDate", columnsString),
               new SqlParameter("@ParaDateConvert", expressionString),
               new SqlParameter("@ResvType", idList),
               new SqlParameter("@NonDeduct", "1"),
                 new SqlParameter("@IncludeOverbook", includeOverbooking),
                   new SqlParameter("@IncludeAllotment", stringincludeAllotment),
                     new SqlParameter("@ViewbyAllotement", "1"),
            };

            DataTable myTable = DataTableHelper.getTableData("spRmgRoomAvailableNew", param);
            return myTable;
        }
        public DataTable GetRoomByAllotment(DateTime fromDate, DateTime toDate, string zone, string columnsString, string expressionString, string idList, string includeOverbooking, string stringincludeAllotment)
        {
            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@FromDate", fromDate),
               new SqlParameter("@ToDate", toDate),
               new SqlParameter("@Zone", zone),
               new SqlParameter("@ParaDate", columnsString),
               new SqlParameter("@ParaDateConvert", expressionString),
               new SqlParameter("@ResvType",idList),
               new SqlParameter("@NonDeduct", "1"),
                 new SqlParameter("@IncludeOverbook", includeOverbooking),
                   new SqlParameter("@IncludeAllotment", stringincludeAllotment),
                     new SqlParameter("@ViewbyAllotement", "1"),
            };

            DataTable myTable = DataTableHelper.getTableData("spGetRoomByAllotment", param);
            return myTable;
        }
        public DataTable GetBookedRoom(DateTime fromDate, DateTime toDate, string zone, string columnsString, string expressionString, string idList, string includeOverbooking, string stringincludeAllotment)
        {
            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@FromDate", fromDate),
               new SqlParameter("@ToDate", toDate),
               new SqlParameter("@Zone", zone),
               new SqlParameter("@ParaDate", columnsString),
               new SqlParameter("@ParaDateConvert", expressionString),
               new SqlParameter("@ResvType", idList),
               new SqlParameter("@NonDeduct", "1"),
     new SqlParameter("@IncludeOverbook", includeOverbooking),
                   new SqlParameter("@IncludeAllotment", stringincludeAllotment),
                     new SqlParameter("@ViewbyAllotement", "1"),
            };

            DataTable myTable = DataTableHelper.getTableData("spGetBookedRoom", param);
            return myTable;
        }
        public DataTable SelectAvailibilityColor(DateTime fromDate, DateTime toDate)
        {
            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@FromDate", fromDate),
               new SqlParameter("@ToDate", toDate)
            };

            DataTable myTable = DataTableHelper.getTableData("spSelectAvailibilityColor", param);
            return myTable;
        }
        public DataTable RoomAssignmentData(DateTime arrival, string groupProfile, string vipcode, string name, string reservationTypeCode, string crsno, string roomTypeCode, string roomNo, string confirmationNo)
        {
            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@ArrivalDate", arrival),
               new SqlParameter("@Group", groupProfile),
                new SqlParameter("@VIP", vipcode),
               new SqlParameter("@Name", name),
                new SqlParameter("@ReservationType", reservationTypeCode),
               new SqlParameter("@CRSNo", crsno),
                  new SqlParameter("@RoomType", roomTypeCode),
                new SqlParameter("@RoomNo", roomNo),
               new SqlParameter("@ConfirmationNo", confirmationNo),
                     new SqlParameter("@BusinessBlock", ""),
               new SqlParameter("@Zone", "")
            };

            DataTable myTable = DataTableHelper.getTableData("spReservationAssignRoom", param);
            return myTable;
        }
    }
}

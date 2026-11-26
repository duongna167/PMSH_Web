using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaseBusiness.Model;
using BaseBusiness.util;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Miscellaneous.Services.Interfaces;

namespace Miscellaneous.Services.Implements
{
    public partial class MiscellaneousService : IMiscellaneousService
    {
        public DateTime GetMealBusinessDate() => TextUtils.GetBusinessDate().Date;

        public bool CancelReportedMeal(int id)
        {
            // The eligibility check and update are atomic; a repeated request cannot cancel twice.
            using var connection = new SqlConnection(DBUtils.GetDBConnectionString());
            using var command = new SqlCommand(@"
                UPDATE dbo.MealDailyUsing
                SET Inactive = 1, UsingDate = GETDATE()
                WHERE ID = @ID AND Inactive = 0
                  AND DATEDIFF(DAY, UsingDate, dbo.getBusinessDate()) = 0;", connection);
            command.Parameters.Add("@ID", SqlDbType.Int).Value = id;
            connection.Open();
            return command.ExecuteNonQuery() > 0;
        }

        public DataTable MealReport(DateTime fromDate, DateTime toDate, int type)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@FromDate", fromDate),
                new SqlParameter("@ToDate", toDate),
                new SqlParameter("@Type", type )
            };

            DataTable resultTable = DataTableHelper.getTableData("spMeal_SearchChange", parameters);

            return resultTable;
        }
        public DataTable ReportCustom(DateTime fromDate, DateTime toDate, string mealShiftID, string roomNo)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@FromDate", fromDate),
                new SqlParameter("@ToDate", toDate),
                new SqlParameter("@MealShiftID", mealShiftID ?? "" ),
                new SqlParameter("@RoomNo", roomNo ?? "" ),
            };

            DataTable resultTable = DataTableHelper.getTableData("spMeal_SearchCustom", parameters);

            return resultTable;
        }
        public DataTable ReportCancelMeal(DateTime fromDate, DateTime toDate)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@FromDate", fromDate),
                new SqlParameter("@ToDate", toDate),

            };

            DataTable resultTable = DataTableHelper.getTableData("spMeal_SearchCancel", parameters);

            return resultTable;
        }
        public DataTable ReportWaiveMeal(DateTime fromDate, DateTime toDate, string mealShift, string roomNo)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@FromDate", fromDate),
                new SqlParameter("@ToDate", toDate),
                new SqlParameter("@MealShift", mealShift ?? "" ),
                new SqlParameter("@RoomNo", roomNo ?? "" ),
            };

            DataTable resultTable = DataTableHelper.getTableData("spMeal_SearchWaive", parameters);

            return resultTable;
        }
        public DataTable ReportUsedBreakFastMeal(DateTime fromDate, DateTime toDate, string roomNo)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@FromDate", fromDate),
                new SqlParameter("@ToDate", toDate),       
                new SqlParameter("@RoomNo", roomNo ?? "" ),
            };

            DataTable resultTable = DataTableHelper.getTableData("spMeal_UsedBreakfast", parameters);

            return resultTable;
        }
        public DataTable ReportUseBreakFastMeal(DateTime fromDate, DateTime toDate, string roomNo)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@FromDate", fromDate),
                new SqlParameter("@ToDate", toDate),
                new SqlParameter("@RoomNo", roomNo ?? "" ),
            };

            DataTable resultTable = DataTableHelper.getTableData("spMeal_UseBreakfast", parameters);

            return resultTable;
        }
        public DataTable IssuingCardToGuests(string firstName, string confirmationNo, string crsNo, string roomNo, string zone, string guestName, string rsvHolder, string isShowRS, string isShowCard, DateTime arrFrom, DateTime arrTo, string status, string ci_Day, string co_Day, string findCardID, string reservationID)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@FirstName", firstName ?? ""),
                new SqlParameter("@ConfirmationNo", confirmationNo ?? ""),
                new SqlParameter("@CRSNo", crsNo ?? ""),
                new SqlParameter("@RoomNo", roomNo ?? ""),
                new SqlParameter("@Zone", zone ?? ""),
                new SqlParameter("@GuestName", guestName ?? ""),
                new SqlParameter("@RsvHolder", rsvHolder ?? ""),
                new SqlParameter("@IsShowRS", isShowRS ?? "" ),
                new SqlParameter("@IsShowCard", isShowCard ?? "" ),
                new SqlParameter("@ArrFrom", arrFrom == DateTime.MinValue ? "" : arrFrom.ToString("yyyy-MM-dd")),
                new SqlParameter("@ArrTo", arrTo == DateTime.MinValue ? "" : arrTo.ToString("yyyy-MM-dd")),

                new SqlParameter("@Status", status ?? "" ),
                new SqlParameter("@CI_Day", ci_Day ?? "" ),
                new SqlParameter("@CO_Day", co_Day ?? "" ),
                new SqlParameter("@FindCardID", findCardID ?? "" ),
                new SqlParameter("@ReservationID", reservationID ?? "" ),
            };

            DataTable resultTable = DataTableHelper.getTableData("spReservationSearchByCard", parameters);

            return resultTable;
        }
    }
}

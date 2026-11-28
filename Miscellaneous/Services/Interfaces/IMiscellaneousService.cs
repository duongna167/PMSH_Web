using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaseBusiness.Model;

namespace Miscellaneous.Services.Interfaces
{
    public enum CardDeleteResult { Deleted = 0, InUse = 1, NotFound = 2 }

    public interface IMiscellaneousService
    {
        DataTable Cards(string? id = null);
        DataTable ComputerCardReaders(string cardReaderIds);
        DataTable CardReaders();
        DataTable Computers();
        bool SaveComputerCardReader(int cardReaderId, IReadOnlyList<int> computerIds);
        int DeleteComputerCardReaders(IReadOnlyList<int> ids);
        bool InsertCard(string id, int cardTypeId, int status, string userName);
        bool UpdateCard(string id, int cardTypeId, int status, string userName);
        CardDeleteResult DeleteCard(string id);
        CardIssueResult IssueCards(IReadOnlyList<CardAssignment> items, int userId, string userName);
        CardIssueResult CancelCards(IReadOnlyList<int> reservationIds, int userId, string userName);
        DataTable MealChangeHistory(int reservationId);
        string MealSummary(int reservationId);
        MealChangeResult ProcessMealChange(MealChangeRequest request, string userName);
        DataTable MealReport(DateTime fromDate, DateTime toDate, int type);
        DateTime GetMealBusinessDate();
        bool CancelReportedMeal(int id);
        DataTable ReportCustom(DateTime fromDate, DateTime toDate, string mealShiftID, string roomNo);
        DataTable ReportCancelMeal(DateTime fromDate, DateTime toDate);
        DataTable ReportWaiveMeal(DateTime fromDate, DateTime toDate, string mealShift, string roomNo);
       
        DataTable ReportUsedBreakFastMeal(DateTime fromDate, DateTime toDate, string roomNo);
        DataTable ReportUseBreakFastMeal(DateTime fromDate, DateTime toDate, string roomNo);
        DataTable IssuingCardToGuests(string firstName, string confirmationNo, string crsNo, string roomNo, string zone, string guestName, string rsvHolder, string isShowRS, string isShowCard, DateTime arrFrom, DateTime arrTo, string status, string ci_Day, string co_Day, string findCardID, string reservationID);
    }
}

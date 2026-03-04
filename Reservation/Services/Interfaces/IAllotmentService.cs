using BaseBusiness.Model;
using System.Data;

namespace Reservation.Services.Interfaces
{
    public interface IAllotmentService
    {
        DataTable AllotmentType(string code, string name, int inactive);
        DataTable AllotmentStage(string code, string name, int inactive);
        DataTable AllotmentSearch(string code, string marketId, string allotmentTypeId, string profileId, string isDefault, string zone);
        DataTable AllotmentSearchTransfer(string fromDate, string toDate, int allotmentFrom, int allotmentTo, string roomType);
        DataTable GetAllotmentDetail(int allotmentID, string roomTypeCodes, DateTime showHistory);
        DataTable GetAllotmentResvSearch(string allotmentIDs, int roomTypeID);
        DataTable GetAllotmentDefaultByStage(DateTime fromDate, DateTime toDate, int type, string allotmentId, string paraDate, string paraDateConvert);
        Task<(bool canDelete, string message)> CanDeleteAllotment(int allotmentId);
        Task<bool> DeleteAllotment(int allotmentId);
        Task<bool> ProcessTransfer(AllotmentTransferModel model, int stageId, int cutoffDay, DateTime? cutoffDate);
        DataTable GetAllotmentLookupData();
        int GetActualAvailability(int fromAllotmentId, int roomTypeId, DateTime date);
        int GetAvailability(int fromAllotID, int rtID, DateTime date);
        //DataTable AllotmentReport(string code, string name, int inactive);
        Task<DataTable> GetAllAllotmentData(string? Code, string? Name, int inactive = 0);
    }
}

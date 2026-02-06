using System.Data;

namespace Reservation.Services.Interfaces
{
    public interface IAllotmentService
    {
        DataTable AllotmentType(string code, string name, int inactive);
        DataTable AllotmentStage(string code, string name, int inactive);
        Task<DataTable> GetAllAllotmentData(string? Code, string? Name, int inactive = 0);
    }
}

using System.Data;


namespace NightAudit.Services.Interfaces
{
    public interface IRoomRateService
    {
        DataTable SearchRoomRate(DateTime date, bool warning, bool reservation, bool checkIN, bool checkOut, bool dueIn, bool dueOut, bool cancel);
    }
}

using System.Data;

namespace NightAudit.Services.Interfaces
{
    public interface IFixedChargeService
    {
        /// <summary>Fixed Charge của các đặt phòng đang hiệu lực trong một ngày, lọc theo trạng thái đặt phòng (màn Search Fix Charge).</summary>
        DataTable SearchFixedCharge(DateTime date, bool warning, bool reservation, bool checkIn, bool checkOut, bool dueIn, bool dueOut);
    }
}

using System.Data;

namespace NightAudit.Services.Interfaces
{
    public interface IRunNightAuditService
    {
        /// <summary>Danh sách khách đang ở (Guest In House) để chọn phòng cần đăng tiền.</summary>
        DataTable SearchGuestInHouse();
    }
}

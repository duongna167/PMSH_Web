using System.Data;
using BaseBusiness.util;
using Microsoft.Data.SqlClient;
using NightAudit.Services.Interfaces;

namespace NightAudit.Services.Implements
{
    public class RunNightAuditService : IRunNightAuditService
    {
        public DataTable SearchGuestInHouse()
        {
            // TypeSearch = 0: chỉ khách đang ở (Guest In House); các điều kiện lọc khác để trống như bản desktop
            SqlParameter[] param =
            [
                new SqlParameter("@Room", ""),
                new SqlParameter("@Name", ""),
                new SqlParameter("@Block", ""),
                new SqlParameter("@Group", ""),
                new SqlParameter("@Party", ""),
                new SqlParameter("@Company", ""),
                new SqlParameter("@ConfirmationNo", ""),
                new SqlParameter("@ArrivalDateFrom", ""),
                new SqlParameter("@ArrivalDateTo", ""),
                new SqlParameter("@DepartureDate", ""),
                new SqlParameter("@CRSNo", ""),
                new SqlParameter("@Package", ""),
                new SqlParameter("@OrderBy", 0),
                new SqlParameter("@GuestType", ""),
                new SqlParameter("@ZoneID", 0),
                new SqlParameter("@TypeSearch", 0)
            ];

            return DataTableHelper.getTableData("spSearchGuestInHouse", param);
        }
    }
}

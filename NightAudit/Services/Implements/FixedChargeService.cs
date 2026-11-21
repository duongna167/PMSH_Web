using System.Data;
using BaseBusiness.util;
using Microsoft.Data.SqlClient;
using NightAudit.Services.Interfaces;

namespace NightAudit.Services.Implements
{
    public class FixedChargeService : IFixedChargeService
    {
        public DataTable SearchFixedCharge(DateTime date, bool warning, bool reservation, bool checkIn, bool checkOut, bool dueIn, bool dueOut)
        {
            // SP nhận ngày và các cờ dạng chuỗi ('True'/'False') như bản desktop; ngày dạng yyyyMMdd để không phụ thuộc cấu hình ngôn ngữ của SQL Server
            SqlParameter[] param =
            [
                new SqlParameter("@Date", date.ToString("yyyyMMdd")),
                new SqlParameter("@Warning", warning.ToString()),
                new SqlParameter("@Rsv", reservation.ToString()),
                new SqlParameter("@CI", checkIn.ToString()),
                new SqlParameter("@CO", checkOut.ToString()),
                new SqlParameter("@DI", dueIn.ToString()),
                new SqlParameter("@DO", dueOut.ToString())
            ];

            return DataTableHelper.getTableData("spSearchFixCharge", param);
        }
    }
}

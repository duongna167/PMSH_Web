using BaseBusiness.util;
using NightAudit.Services.Interfaces;
using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NightAudit.Services.Implements
{
    public class RoomRateService : IRoomRateService
    {
        public DataTable SearchRoomRate(DateTime date, bool warning, bool reservation, bool checkIN, bool checkOut, bool dueIn, bool dueOut, bool cancel)
        {
            try
            {
                SqlParameter[] param = [
                    new SqlParameter("@Date", date),
                    new SqlParameter("@Warning", warning.ToString()),
                    new SqlParameter("@Rsv", reservation.ToString() ),
                    new SqlParameter("@CI", checkIN.ToString()),
                    new SqlParameter("@CO", checkOut.ToString()),
                    new SqlParameter("@DI", dueIn.ToString()),
                    new SqlParameter("@DO", dueOut.ToString()),
                    new SqlParameter("@CAN", cancel.ToString())
                    ];
                DataTable myTable = DataTableHelper.getTableData("spSearchRoomRate", param);
                return myTable;
            }
            catch (Exception ex)
            {
                throw new Exception($"ERROR: {ex.Message}", ex);
            }

        }
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaseBusiness.BO;
using BaseBusiness.util;
using Cashiering.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Org.BouncyCastle.Tls;
namespace Cashiering.Services.Implements
{
    public class CashieringManagerService : ICashieringManagerService
    {
        public DataTable GetGUestInHouse(string room, string name, string block, string group, string party, string company, string confirmationNo, string arrivalDate, string arrivalTo, string departure, string crsNo, string package, string guestName, int zone, int typeSearch)
        {
            try
            {
                SqlParameter[] param = new SqlParameter[]
                {

                     new SqlParameter("@Room", room ?? ""),
                     new SqlParameter("@Name", name ?? ""),
                     new SqlParameter("@Block", block ?? ""),
                     new SqlParameter("@Group", group ?? ""),
                     new SqlParameter("@Party", party ?? ""),
                     new SqlParameter("@Company", company ?? ""),
                     new SqlParameter("@ConfirmationNo", confirmationNo ?? ""),
                     new SqlParameter("@ArrivalDateFrom", arrivalDate),
                     new SqlParameter("@ArrivalDateTo", arrivalTo),
                     new SqlParameter("@DepartureDate", departure),
                     new SqlParameter("@CRSNo", crsNo ?? ""),
                    new SqlParameter("@Package", package ?? ""),
                    new SqlParameter("@OrderBy", "0"),

                     new SqlParameter("@GuestType", guestName ?? ""),
                     new SqlParameter("@ZoneID", zone),
                    new SqlParameter("@TypeSearch", typeSearch),

                };

                DataTable myTable = DataTableHelper.getTableData("spSearchGuestInHouse", param);
                return myTable;
            }
            catch (SqlException ex)
            {

                throw new Exception($"ERROR: {ex.Message}", ex);
            }
        }
    }
}

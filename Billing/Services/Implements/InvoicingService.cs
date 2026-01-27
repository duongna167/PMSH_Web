using BaseBusiness.Model;
using BaseBusiness.util;
using Billing.Services.Interfaces;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Billing.Services.Implements
{
    public class InvoicingService : IInvoicingService
    {
        public DataTable SearchFolio(int guestStatus, int folioStatus, int folioType, string name, string room, string folioNo, string confirmationNo, string date)
        {
            try
            {
                SqlParameter[] param = new SqlParameter[]
                {
                    new SqlParameter("@GuestStatus", guestStatus),

                     new SqlParameter("@FolioStatus", folioStatus),
                     new SqlParameter("@FolioType", folioType),
                     new SqlParameter("@Name", name ?? ""),
                     new SqlParameter("@Room", room ?? ""),
                     new SqlParameter("@FolioNo", folioNo ?? ""),
                     new SqlParameter("@ConfirmationNo", confirmationNo ?? ""),
                     new SqlParameter("@Date", date),


                };

                DataTable myTable = DataTableHelper.getTableData("spSearchFolio", param);
                return myTable;
            }
            catch (SqlException ex)
            {

                throw new Exception($"ERROR: {ex.Message}", ex);
            }
        }

        public DataTable GetPreviewBillingAmount(int confirmationNo, int folioID)
        {
            SqlParameter[] param = new SqlParameter[]
                {
                    new SqlParameter("@ConfirmationNo",confirmationNo),
                    new SqlParameter("@FolioNo",folioID),
                };

            DataTable myTable = DataTableHelper.getTableData("Get_PreviewBilling_Amount", param);
            return myTable;
        }
    }
}

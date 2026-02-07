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
        public DataTable SetUpInvoiceSerial()
        {
            // Kết quả trả về chuỗi định dạng 'yyyy-MM-dd' để SQL Server hiểu đúng
            var BusinessDate = TextUtils.GetBusinessDateTime();
            string tenYearsAgo = BusinessDate.AddYears(-10).ToString("yyyy-MM-dd");

            // Lưu ý: Dùng a.VATDate >= '{tenYearsAgo}' sẽ nhanh hơn DATEDIFF
            string query = $@"SELECT DISTINCT a.SerialNo 
                      FROM FolioVAT a 
                      WHERE a.VATDate >= '{tenYearsAgo}' 
                      AND a.Status = 1 
                      AND ISNULL(a.FormNo, '') <> '' 
                      AND ISNULL(a.SerialNo, '') <> ''";

            // 3. Thực thi qua Store Procedure
            SqlParameter[] parameters = [
                new SqlParameter("sqlCommand", query)
            ];

            DataTable table = DataTableHelper.getTableData("spSearchAllForTrans", parameters);
            return table;
        }

        public DataTable SearchInvoiceOrInvoiceDetail(DateTime fromDate, DateTime toDate, string searchName, string confNo,
         string folioNo, string invoiceNo, string invoiceSerial, int print, int resType, int viewBy, int id)
        {

            List<SqlParameter> param = new List<SqlParameter>
            {
                new("@ConfirmationNo", confNo),
                new("@FolioNo", folioNo),
                new("@SerialNo", invoiceSerial),
                new("@InvoiceNo", invoiceNo),
                new("@FromDate", fromDate),
                new("@ToDate",toDate) ,
                new("@Print",print),
                new("@ResType",resType),
            };
            if (viewBy == 1)
            {
                param.Add(new SqlParameter("@ID", id));
                DataTable myTableDetail = DataTableHelper.getTableData("spVAT_SearchVAT_Detail_HDDT", [.. param]);
                return myTableDetail;

            }

            DataTable myTable = DataTableHelper.getTableData("spVAT_SearchVAT_HDDT", [..param]);
            return myTable;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cashiering.Services.Interfaces
{
    public interface ICashieringManagerService
    {
        /// <summary>
        /// DatVP: Lây danh sách guest in house
        /// </summary>

        /// <returns>Data table chứa danh sách guest in house</returns>
        DataTable GetGUestInHouse(string room, string name, string block, string group, string party, string company, string confirmationNo, string arrivalDate, string arrivalTo, string departure, string crsNo, string package, string guestName, int zone, int typeSearch);
        public DataTable SetUpInvoiceSerial();
        public DataTable SearchInvoiceOrInvoiceDetail(DateTime fromDate, DateTime toDate, string searchName, string confNo,
         string folioNo, string invoiceNo, string invoiceSerial, int print, int resType, int viewBy, int id);
    }
}

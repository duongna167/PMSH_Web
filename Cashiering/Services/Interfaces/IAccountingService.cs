using BaseBusiness.Contants;
using Cashiering.Dto;
using DevExpress.Charts.Native;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cashiering.Services.Interfaces
{
    public interface IAccountingService
    {
        /// <summary>
        /// DatVP: Lây danh sách  account receivable
        /// </summary>


        /// <returns>Data table chứa danh sách account receivable</returns>
        DataTable AccountSearch(string accountName, string accountNo, int accountType, string balance);


        /// <summary>
        /// DatVP: Lây danh sách account maintenacnce
        /// </summary>


        /// <returns>Data table chứa danh sáchaccount maintenacnce</returns>
        DataTable AccountMaintence(string dateCheck, int arID, string folioNo, string isActive, string paymentOnly, string print, DateTime fromDate, DateTime toDate);


        /// <summary>
        /// DatVP: search by command
        /// </summary>
        /// <returns>search by command</returns>
        DataTable SearchByCommmand(string sqlCommand);

        /// <summary>
        /// DatVP: Lây danh sách  account receivable
        /// </summary>


        /// <returns>Data table chứa danh sách account receivable</returns>
        DataTable InvoiceSearch(int folioID, int mode);

        /// <summary>
        /// DatVP: Lây danh sách folio transfer account receivable
        /// </summary>


        /// <returns>Data table chứa anh sách folio transfer account receivable</returns>
        DataTable SearchInfoAR(string accountName, string accountNo, string folioNo, string isActive, string folioID);
        DataTable AccountTypeData();
        DataTable AROpeningData();
        DataTable ARTracesData();
        DataTable ARAccountReceivableSearch();
        DataTable ARPaymentReport(DateTime FromDate, DateTime ToDate, string AccountNo, string Cashier, int ViewBy = 0);

        // Tuan:ADd
        ApiResponseAddError<ValidationErrorDto> SaveARAccount(SaveARAccountRequestDto dto);
    }


}

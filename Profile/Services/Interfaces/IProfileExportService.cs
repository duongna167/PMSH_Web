using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Profile.Services.Interfaces
{
    public interface IProfileExportService
    {
        /// <summary>
        /// DatVP: Lây danh sách profile export
        /// </summary>
        /// <param name="fromDate">from date</param>
        /// <param name="toDate">to date</param>
        /// <returns>Data table chứa danh sách profile export</returns>
        DataTable SearchProfileExport(DateTime fromDate, DateTime toDate);


        /// <summary>
        /// DatVP: Lây danh sách profile history
        /// </summary>
        /// <param name="profileID">id profile</param>
        /// <param name="type">type</param>
        /// <param name="confirmationNo">confirmation no</param>
        /// <returns>Data table chứa danh sách profile history</returns>
        DataTable SearchProfileHistory(string profileID, int type, string confirmationNo);

        /// <summary>
        /// History Summary (WinForm: frmProfileSummaryHistory - spProfileSummaryHistory)
        /// </summary>
        DataTable SearchProfileSummaryHistory(string profileID, string confirmationNo);
        DataTable ExportXML(DateTime fromDate, DateTime toDate);

        /// <summary>
        /// Lấy danh sách profile để export ra file .CSV (WinForm: frmProfileExport_C)
        /// </summary>
        DataTable SearchProfileExportCSV(DateTime fromDate, DateTime toDate);

        /// <summary>
        /// Export Profile to Excel (WinForm: frmProfileExport_V2).
        /// guestInHouse = true dùng spGuestProfileSearchGuest, ngược lại spGuestProfileSearch
        /// </summary>
        DataTable SearchProfileExportV2(DateTime fromDate, DateTime toDate, bool guestInHouse);
    }
}

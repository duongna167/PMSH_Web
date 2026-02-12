using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reservation.Services.Interfaces
{
    public interface IGroupReservationService
    {
        /// <summary>
        /// DatVP: lấy danh sách reservation group
        /// </summary>
        /// <param name="fromDate">from date</param>
        /// <param name="toDate">to date</param>
        /// <param name="noOfRoom">no of room</param>
        /// <returns>Data table chứa danh sách reservation group</returns>
        DataTable GetGroupReservation(DateTime fromDate,DateTime toDate, int noOfRoom);


        /// <summary>
        /// DatVP: Tính tiền price của rate code group reservation theo net
        /// </summary>
        /// <param name="priceAfter">giá trị tiền net</param>
        /// <param name="transactionCode">transaction code</param>
        /// <returns>Data table chứa danh sách reservation group</returns>
        decimal CalculatePriceFromNet(decimal priceAfter, string transactionCode);
        DataTable GetRoomTypeAvailableDetail(DateTime fromDate);
    }
}

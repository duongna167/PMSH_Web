using BaseBusiness.util;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Reservation.Dto.ReservationPackageDTO;

namespace Reservation.Services.Interfaces
{
    public interface IReservationService
    {
        DataTable GetRateCode(DateTime arrival, DateTime departure, int adults, int roomType);

        /// <summary>
        /// DatVP: Lây danh sách room available từ store procedure
        /// </summary>
        /// <returns>Data table chứa danh sách room available</returns>
        DataTable GetRoomAvailable(DateTime fromDate, DateTime toDate, string floor, string roomTypeID, string smoking, string foStatus, string hkStatus, string isDummy, string roomNo, int roomID, int Type);
        DataTable GetAllotment(string code, string marketID, string profileID, string isDefault, string zone, string allotmentTypeID);
        DataTable GetAllotmentDetail(int allotmentID, string roomType, DateTime showHistory);
        DataTable GetReservationPreference(string code, int group);
        DataTable ReservationRateQueryDetail(DateTime fromDate, DateTime toDate, int roomType, int adults, int noOfNight, int packageID, int promotionID,
            string tableName, string onRows, string onRowsAlias, string onCols, string sumcol, int func, string currency, int display, int dayUse, int c1, int c2,
            int c3, int noOfRoom);

        /// <summary>
        /// DatVP: lấy rate query detail
        /// </summary>
        /// <param name="fromDate">arival date</param>
        /// <param name="toDate">departure date</param>
        /// <param name="rateCodeID">id rate code</param>
        /// <param name="roomType">id room type</param>
        /// <param name="currency">Currency</param>
        /// <param name="packageID">id package</param>
        /// <param name="day">day</param>
        /// <returns>Giá trị net</returns>
        DataTable ReservationGetRateQueryDetail(DateTime fromDate, DateTime toDate, int rateCodeID, int roomType, string currency, int packageID, int day);

        /// <summary>
        /// DatVP: Tính net cho rate code
        /// </summary>
        /// <param name="Price">Giá trị tiền rate code</param>
        /// <param name="TransactionCode">Giá trị tiền rate code</param>
        /// <returns>Giá trị net</returns>
        (decimal price, decimal priceAfter, decimal priceDiscount, decimal priceAfterDiscount) CalculateNet(decimal Price, string TransactionCode, decimal DiscountAmount, decimal DiscountPercent);
        (decimal Price, decimal priceAfter, decimal priceDiscount, decimal priceAfterDiscount) CalculateNetReverse(decimal priceAfterDiscount, string TransactionCode, decimal DiscountAmount, decimal DiscountPercent);

        /// <summary>
        /// DatVP: Tính net cho fixed charge
        /// </summary>
        /// <param name="transactionCode"> code transaction</param>
        /// <param name="price">giá trị tiền</param>
        /// <returns>Giá trị net</returns>
        decimal CalculateNetFixedCharge(string transactionCode, decimal price);


        /// <summary>
        /// DatVP: search reservation
        /// </summary>
        /// <returns>datâtble list reservation</returns>
        DataTable SearchReservation(int searchType, string name, string firstName, string reservationHolder, string confirmationNo,
            string crsNo, string roomNo, string roomType, string package, string zone, string arrivalFrom, string arrivalTo, string roomSharer, string owner);


        /// <summary>
        /// DatVP: search waitt list
        /// </summary>
        /// <returns>datatable list wait lít</returns>
        DataTable SearchWaitlist(string name, string priority, string market, string roomType, string reason, string rateCode, string phone, DateTime date);

        /// <summary>
        /// DatVP: se
        /// </summary>
        /// <returns>datatable list wait lít</returns>
        DataTable SearchOverBooking(string sqlCommand);

        DataTable ActivityLogOverbooking(string sqlCommand);

        string GetConfigETA();
        string GetConfigETD();

        /// <summary>
        /// DatVP: seearch reservation alert
        /// </summary>
        /// <param name="reservationID">reservation id</param>

        /// <returns>datatable list reservation alert</returns>
        DataTable SearchReservationAlerts(int reservationID);

        /// <summary>
        /// DatVP: seearch reservation trace
        /// </summary>
        /// <param name="departmentID">department id</param>
        /// <param name="resolved">trace status -- All: '',Resolved: 1,UnResolveđ:0</param>
        /// <param name="date">trace create date</param>
        /// <param name="name">profile name</param>
        /// <param name="reservationID">reservation id</param>

        /// <returns>datatable list reservation alert</returns>
        DataTable SearchTrace(string departmentID, string resolved, DateTime date, string name, string reservationID);

        /// <summary>
        /// DatVP: seearch room auto assign
        /// </summary>
        /// <param name="type">type auto: 1. Assign, 2. UnAssign</param>
        /// <param name="roomType">name room type</param>
        /// <param name="roomClass">room class</param>
        /// <param name="smoking">smoking</param>
        /// <param name="floor">floor</param>
        /// <param name="startFromRoom">start from room</param>
        /// <param name="arrivalDate">arrival date</param>
        /// <param name="departureDate">departure date</param>
        /// <param name="hkStatusID"> hk status</param>
        /// <param name="confirmationNo">confirmation no</param>
        /// <param name="rsvRoomTypeID">id room type</param>
        /// <param name="notAssRoomNo">mot ass room no</param>
        /// <returns>datatable list room auto assign</returns>
        DataTable ReservationAutoRoomAssignment(int type, string roomType, string roomClass, string smoking, string floor,
            string startFromRoom, DateTime arrivalDate, DateTime departureDate, string hkStatusID, string confirmationNo, string rsvRoomTypeID, string notAssRoomNo);
        DataTable SearchReservationPackages(
            int reservationID,
            int packageID,
            int type,
            DateTime beginDate,
            DateTime endDate,
            int rateCodeID);

        List<ReservationPackageSummary> GetReservationPackagePhase2(int reservationId, int packageId, DateTime beginDate, DateTime endDate, int rateCodeId);
    }
}

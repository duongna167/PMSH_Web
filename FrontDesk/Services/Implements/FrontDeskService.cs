using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaseBusiness.BO;
using BaseBusiness.Model;
using BaseBusiness.util;
using FrontDesk.Services.Interfaces;
using Microsoft.Data.SqlClient;

namespace FrontDesk.Services.Implements
{
    public class FrontDeskService : IFrontDeskService
    {

        public DataTable TelephoneBook(string categoryId, string categoryCode, string telephoneCode)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@Action", false),
                new SqlParameter("@TelephoneBookCategoryID", categoryId ?? ""),
                new SqlParameter("@TelephoneBookCategoryCode", categoryCode ?? ""),
                new SqlParameter("@TelephoneBookCode", telephoneCode ?? "")
            };

            return DataTableHelper.getTableData("spTelephoneBookSearch", param);
        }

        public DataTable GetTelephoneBookByCategory(string categoryId, string searchTerm)
        {
            string sql = @"SELECT ID, Name, Telephone, Address, Remark, Color 
                   FROM TelephoneBook 
                   WHERE ID > 0";

            if (!string.IsNullOrEmpty(categoryId) && categoryId != "25")
                sql += $" AND TelephoneBookCategoryID = {categoryId}";

            if (!string.IsNullOrEmpty(searchTerm))
                sql += $" AND Telephone LIKE N'%{searchTerm}%'";

            sql += " ORDER BY Name";
            Console.WriteLine("Final SQL passed to SP:");
            Console.WriteLine(sql);

            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@sqlCommand", sql)
            };

            return DataTableHelper.getTableData("spSearchAllForTrans", param);
        }
        public DataTable TelephoneSwitch(string roomNo, int foStatus)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@RoomNo", string.IsNullOrEmpty(roomNo) ? (object)DBNull.Value : roomNo),
                new SqlParameter("@FOStatus", foStatus)
            };

            DataTable myTable = DataTableHelper.getTableData("spTelephoneSwitchSearch", param);
            return myTable;
        }
        public int InsertRoomStatusHistory(int objectId, string tableName, string userName, string roomNo,
                           string action, string computerName, string oldValue, string newValue, DateTime changeDate)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@ObjectID", objectId),
                new SqlParameter("@TableName", tableName),
                new SqlParameter("@UserName", userName),
                new SqlParameter("@RoomNo", roomNo),
                new SqlParameter("@Action", action),
                new SqlParameter("@ComputerName", computerName),
                new SqlParameter("@OldValue", oldValue),
                new SqlParameter("@NewValue", newValue),
                new SqlParameter("@ChangeDate", changeDate)
            };

            string sql = @"
                INSERT INTO RoomStatusHistory (ObjectID,TableName,UserName,RoomNo,Action,ComputerName,OldValue,NewValue,ChangeDate) 
                OUTPUT INSERTED.ID
                VALUES (@ObjectID,@TableName,@UserName,@RoomNo,@Action,@ComputerName,@OldValue,@NewValue,@ChangeDate);";

            int newId = DataTableHelper.ExecuteInsertAndReturnId(sql, param);
            return newId;
        }

        public int InsertTelephoneSwitch(string roomNo, string guestName, int status, DateTime createDate)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@RoomNo", roomNo),
                new SqlParameter("@GuestName", guestName),
                new SqlParameter("@Status", status),
                new SqlParameter("@CreateDate", createDate)
            };

            string sql = @"
                INSERT INTO TelephoneSwitch (RoomNo, GuestName, Status, CreateDate)
                OUTPUT INSERTED.ID
                VALUES (@RoomNo, @GuestName, @Status, @CreateDate);";

            int newId = DataTableHelper.ExecuteInsertAndReturnId(sql, param);
            return newId;
        }
        public DataTable DialingInformation(DateTime fromDate, DateTime toDate, string phoneNo, int view, string zone)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@FromDate", fromDate),
                new SqlParameter("@ToDate", toDate),
                new SqlParameter("@PhoneNo", phoneNo ?? ""),
                new SqlParameter("@View", view),
                new SqlParameter("@Zone", zone ?? "")
            };

            DataTable myTable = DataTableHelper.getTableData("spDialInformationExt", param);
            return myTable;
        }

        public DataTable WakeUpCallFindRoom(string roomNoset, string reservationHolder, string zone, string confirmNo)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@RoomNo", roomNoset),
                new SqlParameter("@ConfirmationNo", confirmNo),
                new SqlParameter("@ReservationHolder", reservationHolder),
                new SqlParameter("@Zone", zone),

            };

            DataTable myTable = DataTableHelper.getTableData("spWakeUpCallFindRoom", param);
            return myTable;
        }
        public DataTable WakeUpCallSearch(DateTime currentDate, string searchforName, int isSpecial)
        {
            int IsProfile = 0;
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@Name", searchforName),
                new SqlParameter("@CurrentDate", currentDate),
                new SqlParameter("@IsSpecial", isSpecial),
                new SqlParameter("@IsProfile", IsProfile),

            };

            DataTable myTable = DataTableHelper.getTableData("spWakeUpCallSearch", param);
            return myTable;
        }
        public DataTable ViewWakeUpCall(string name, string group, string roomview, DateTime fromDateview, DateTime toDateview, string hour, string minute, int roomClass)
        {
            int IsExport = 0;
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@Name", name),
                new SqlParameter("@GroupID", group),
                new SqlParameter("@RoomNo", roomview),
                new SqlParameter("@FromDate", fromDateview),
                new SqlParameter("@ToDate", toDateview),
                new SqlParameter("@RoomClassID", roomClass),
                new SqlParameter("@IsExport", IsExport),
                  new SqlParameter("@Hour", hour),
                     new SqlParameter("@Minute", minute),
            };

            DataTable myTable = DataTableHelper.getTableData("spWakeUpCallView", param);
            return myTable;
        }
        public DataTable ViewWakeUpCallAccount(int roomID, int shareRoom)
        {
            int IsExport = 0;
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@RoomID", roomID),
                new SqlParameter("@ShareRoom", shareRoom)
            };

            DataTable myTable = DataTableHelper.getTableData("spWakeUpCallFindMainGuest", param);
            return myTable;
        }

        public DataTable GetRoomPFOS(int isVacant)
        {
            string query;
            if (isVacant == 0)
            {
                query = "Select * from Room where ((RoomNo like N'%') or (RoomName like N'%')) AND (FOStatus = 0) AND (RoomTypeCode <> 'XXX') Order by RoomNo";
            }

            else
            {
                query = "Select * from Room where ((RoomNo like N'%') or (RoomName like N'%')) AND (FOStatus = 1) AND (RoomTypeCode <> 'XXX') Order by RoomNo";
            }

            SqlParameter[] parameters = [new SqlParameter("sqlCommand", query)];
            DataTable table = DataTableHelper.getTableData("spSearchAllForTrans", parameters);
            return table;
        }


        // Hàm lấy số lượng Reservation để Controller tự check Valid
        public int GetReservationCount(int roomId, bool isVacant)
        {
            string businessDate = TextUtils.GetBusinessDate().ToString("yyyy/MM/dd");
            string sql = $@"SELECT Count(a.ID) 
                        FROM ReservationRate a WITH (NOLOCK)
                        JOIN Reservation b WITH (NOLOCK) ON a.ReservationID = b.ID
                        WHERE a.RoomID = {roomId} 
                        AND a.RoomID > 0 
                        AND b.ReservationNo > 0 
                        AND (b.Status = 1 OR b.Status = 6) ";

            if (isVacant)
            {
                sql += $" AND DATEDIFF(DAY, a.RateDate, '{businessDate}') <= 0";
            }

            DataTable dt = TextUtils.Select(sql);
            return (dt != null && dt.Rows.Count > 0) ? Convert.ToInt32(dt.Rows[0][0]) : 0;
        }

        // Hàm Update sau khi Controller đã valid xong
        public (string roomNo, string statusName) ExecuteUpdateStatus(RoomModel roomModel, bool isVacant, int userID)
        {
            roomModel.FOStatus = isVacant ? 0 : 1;
            roomModel.HKFOStatus = isVacant ? 0 : 1;
            roomModel.UserUpdateID = userID;
            roomModel.UpdateDate = DateTime.Now;

            RoomBO.Instance.Update(roomModel);

            return (roomModel.RoomNo, isVacant ? "vacant" : "occupied");
        }

    }
}

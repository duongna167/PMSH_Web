using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaseBusiness.BO;
using BaseBusiness.Model;
using BaseBusiness.util;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using RoomManagement.Services.Interfaces;
using RoomManagement.Dto;

namespace RoomManagement.Services.Implements
{
    public class RoomManagementService : IRoomManagementService
    {
        public DataTable Discrepancy(int sleep, int skip, int person, string floor, string room, string zone, string roomClass)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@Sleep", sleep),
                new SqlParameter("@Skip", skip),
                new SqlParameter("@Person", person),
                new SqlParameter("@Floor", floor ?? ""),
                new SqlParameter("@Room", room ?? ""),
                new SqlParameter("@Zone", zone ?? ""),
                new SqlParameter("@RoomClass", roomClass ?? "")
            };

            DataTable myTable = DataTableHelper.getTableData("spHKDescrepancy", param);
            return myTable;
        }
        public DataTable ItemDailyInventory(
                int groupId,
                DateTime firstDate,
                DateTime secondDate,
                DateTime thirthDate,
                DateTime fourthDate,
                DateTime fifthDate,
                DateTime sixthDate,
                DateTime seventhDate,
                DateTime eighthDate,
                DateTime ninthDate,
                DateTime tenthDate,
                DateTime eleventhDate,
                DateTime twelvethDate,
                DateTime thirtheenDate,
                DateTime fourtheenDate,
                DateTime fiftheenDate)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@GroupID", groupId),
                new SqlParameter("@FirstDate", firstDate),
                new SqlParameter("@SecondDate", secondDate),
                new SqlParameter("@ThirthDate", thirthDate),
                new SqlParameter("@FourthDate", fourthDate),
                new SqlParameter("@FifthDate", fifthDate),
                new SqlParameter("@SixthDate", sixthDate),
                new SqlParameter("@SeventhDate", seventhDate),
                new SqlParameter("@EighthDate", eighthDate),
                new SqlParameter("@NinthDate", ninthDate),
                new SqlParameter("@TenthDate", tenthDate),
                new SqlParameter("@EleventhDate", eleventhDate),
                new SqlParameter("@TwelvethDate", twelvethDate),
                new SqlParameter("@ThirtheenDate", thirtheenDate),
                new SqlParameter("@FourtheenDate", fourtheenDate),
                new SqlParameter("@FiftheenDate", fiftheenDate)
            };

            DataTable myTable = DataTableHelper.getTableData("spItemDailyInventory", param);
            return myTable;
        }
        public bool UpdateItemInventory(InventoryUpdateRequest model)
        {
            using (SqlConnection conn = new SqlConnection(DBUtils.GetDBConnectionString()))
            {
                conn.Open();

                // 1. Lấy ID theo ngày và ItemID
                int? id = null;
                using (SqlCommand cmd = new SqlCommand(@"
            EXEC spSearchAllForTrans @sqlCommand = N'SELECT ID FROM ItemInventory WHERE ItemID = @ItemID AND DATEDIFF(DAY, Date, @MatchDate) = 0'", conn))
                {
                    cmd.Parameters.AddWithValue("@ItemID", model.ItemID);
                    cmd.Parameters.AddWithValue("@MatchDate", model.MatchDate);

                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                        id = Convert.ToInt32(result);
                }

                if (id == null)
                    return false;

                // 2. Thực hiện UPDATE bằng sp_executesql
                using (SqlCommand cmd = new SqlCommand("sp_executesql", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    string sqlUpdate = @"
                UPDATE ItemInventory
                SET 
                    ItemID = @ItemID,
                    Date = @Date,
                    Quantity = @Quantity,
                    VendorID = @VendorID,
                    Sun = @Sun,
                    Mon = @Mon,
                    Tue = @Tue,
                    Wed = @Wed,
                    Thu = @Thu,
                    Fri = @Fri,
                    Sat = @Sat,
                    CreateDate = @CreateDate,
                    UpdateDate = @UpdateDate,
                    UserInsertID = @UserID,
                    UserUpdateID = @UserID
                WHERE ID = @ID";

                    string parameterDef = @"
                @ItemID int,
                @Date datetime,
                @Quantity int,
                @VendorID int,
                @Sun nvarchar(4000),
                @Mon nvarchar(4000),
                @Tue nvarchar(4000),
                @Wed nvarchar(4000),
                @Thu nvarchar(4000),
                @Fri nvarchar(4000),
                @Sat nvarchar(4000),
                @CreateDate datetime,
                @UpdateDate datetime,
                @UserID int,
                @ID int";

                    cmd.Parameters.AddWithValue("@stmt", sqlUpdate);
                    cmd.Parameters.AddWithValue("@params", parameterDef);

                    var now = DateTime.Now;

                    cmd.Parameters.AddWithValue("@ItemID", model.ItemID);
                    cmd.Parameters.AddWithValue("@Date", model.Date);
                    cmd.Parameters.AddWithValue("@Quantity", model.Quantity);
                    cmd.Parameters.AddWithValue("@VendorID", 0);
                    cmd.Parameters.AddWithValue("@Sun", "False");
                    cmd.Parameters.AddWithValue("@Mon", "False");
                    cmd.Parameters.AddWithValue("@Tue", "False");
                    cmd.Parameters.AddWithValue("@Wed", "False");
                    cmd.Parameters.AddWithValue("@Thu", "False");
                    cmd.Parameters.AddWithValue("@Fri", "False");
                    cmd.Parameters.AddWithValue("@Sat", "False");
                    cmd.Parameters.AddWithValue("@CreateDate", now);
                    cmd.Parameters.AddWithValue("@UpdateDate", now);
                    cmd.Parameters.AddWithValue("@UserID", model.UserID);
                    cmd.Parameters.AddWithValue("@ID", id.Value);

                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }
        public DataTable ItemInventoryAvailable(
                int groupId,
                DateTime firstDate,
                DateTime secondDate,
                DateTime thirthDate,
                DateTime fourthDate,
                DateTime fifthDate,
                DateTime sixthDate,
                DateTime seventhDate,
                DateTime eighthDate,
                DateTime ninthDate,
                DateTime tenthDate,
                DateTime elevenDate,
                DateTime twelveDate,
                DateTime thirtheenDate,
                DateTime fourtheenDate,
                DateTime fiftheenDate)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@GroupID", groupId),
                new SqlParameter("@FirstDate", firstDate),
                new SqlParameter("@SecondDate", secondDate),
                new SqlParameter("@ThirthDate", thirthDate),
                new SqlParameter("@FourthDate", fourthDate),
                new SqlParameter("@FifthDate", fifthDate),
                new SqlParameter("@SixthDate", sixthDate),
                new SqlParameter("@SeventhDate", seventhDate),
                new SqlParameter("@EighthDate", eighthDate),
                new SqlParameter("@NinthDate", ninthDate),
                new SqlParameter("@TenthDate", tenthDate),
                new SqlParameter("@ElevenDate", elevenDate),
                new SqlParameter("@TwelveDate", twelveDate),
                new SqlParameter("@ThirtheenDate", thirtheenDate),
                new SqlParameter("@FourtheenDate", fourtheenDate),
                new SqlParameter("@FiftheenDate", fiftheenDate)
            };

            // Giả sử bạn có helper để gọi SP và trả DataTable
            DataTable resultTable = DataTableHelper.getTableData("spItemInventoryAvailable", parameters);

            return resultTable;
        }
        public DataTable ItemResvDetail(int itemID, DateTime day)

        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@day", day),
                new SqlParameter("@ItemID", itemID)
            };

            // Giả sử bạn có helper để gọi SP và trả DataTable
            DataTable resultTable = DataTableHelper.getTableData("spItemResvDetail", parameters);

            return resultTable;
        }

        public DataTable ItemSearch(string groupID, string name)

        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@GroupID", groupID),
                new SqlParameter("@Name", name ?? "")
            };

            DataTable resultTable = DataTableHelper.getTableData("spItemSearch", parameters);

            return resultTable;
        }
        public DataTable OOOSload(int status, string roomNo, int roomClassID, DateTime fromDate, DateTime toDate, string zone)

        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Status", status),
                new SqlParameter("@RoomNo", roomNo ?? ""),
                new SqlParameter("@RoomClassID", roomClassID ),
                new SqlParameter("@FromDate", fromDate ),
                new SqlParameter("@ToDate", toDate ),
                new SqlParameter("@Zone", zone ?? "" )
            };

            DataTable resultTable = DataTableHelper.getTableData("spOOOSload", parameters);

            return resultTable;
        }
        public DataTable AvailableRoomsSearchOOO(string isDummy, string smoking, string floor, string roomTypeCode, string foStatus, string hkStatusID, string roomNo, DateTime fromDate, DateTime toDate, string zoneCode)

        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@IsDummy", isDummy ?? ""),
                new SqlParameter("@Smoking", smoking ?? ""),
                new SqlParameter("@Floor", floor ?? ""),
                new SqlParameter("@RoomTypeCode", roomTypeCode ?? ""),
                new SqlParameter("@FOStatus", foStatus ?? ""),
                new SqlParameter("@HKStatusID", hkStatusID ?? ""),
                new SqlParameter("@RoomNo", roomNo ?? ""),
                new SqlParameter("@FromDate", fromDate ),
                new SqlParameter("@ToDate", toDate ),
                new SqlParameter("@ZoneCode", zoneCode ?? "" )
            };

            DataTable resultTable = DataTableHelper.getTableData("spAvailableRoomsSearchOOO", parameters);

            return resultTable;
        }
        public DataTable RoomStatusHistoryOOO(string roomNo, DateTime fromDate, DateTime toDate, string userName)

        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@RoomNo", roomNo ?? ""),
                new SqlParameter("@FromDate", fromDate ),
                new SqlParameter("@ToDate", toDate ),
                new SqlParameter("@UserName", userName ?? "" )
            };

            DataTable resultTable = DataTableHelper.getTableData("spRoomStatusHistoryOOO", parameters);

            return resultTable;
        }
        // =====================================================================================================
        //  Item List / Item Daily Inventory
        // =====================================================================================================

        public DataTable ItemGroups() =>
            DataTableHelper.ExecuteQuery("SELECT ID, Code, Name FROM ItemGroup ORDER BY ID");

        public DataTable ItemNames() =>
            DataTableHelper.ExecuteQuery("SELECT ID, ItemGroupID, Name FROM Item ORDER BY ItemGroupID, ID");

        public DataTable ItemDepartments() =>
            DataTableHelper.ExecuteQuery("SELECT ID, Code, Name FROM Department WHERE ISNULL(Inactive, 0) = 0 ORDER BY Code");

        public DataTable ItemTransactions() =>
            DataTableHelper.ExecuteQuery("SELECT Code, Description FROM Transactions ORDER BY Code");

        public DataTable ItemGet(int id) =>
            DataTableHelper.ExecuteQuery(
                "SELECT ID, ItemGroupID, Code, Name, Description, TransactionCode, DepartmentID, QuantityStock, QuantityDefault, " +
                "AvailableFrom, AvailableTo, SetupTime, SetDownTime, Traces FROM Item WHERE ID = @ID",
                new[] { new SqlParameter("@ID", id) });

        public int ItemSave(ItemSaveRequest r, int userId, DateTime businessDate)
        {
            var p = new List<SqlParameter>
            {
                new SqlParameter("@ItemGroupID", r.ItemGroupID),
                new SqlParameter("@Code", (object?)r.Code?.Trim() ?? DBNull.Value),
                new SqlParameter("@Name", r.Name!.Trim()),
                new SqlParameter("@Description", (object?)r.Description?.Trim() ?? DBNull.Value),
                new SqlParameter("@TransactionCode", (object?)r.TransactionCode?.Trim() ?? DBNull.Value),
                new SqlParameter("@DepartmentID", r.DepartmentID),
                new SqlParameter("@QuantityStock", r.QuantityStock ?? 0),
                new SqlParameter("@QuantityDefault", r.QuantityDefault ?? 0),
                new SqlParameter("@AvailableFrom", (object?)r.AvailableFrom?.Trim() ?? DBNull.Value),
                new SqlParameter("@AvailableTo", (object?)r.AvailableTo?.Trim() ?? DBNull.Value),
                new SqlParameter("@SetupTime", (object?)r.SetupTime?.Trim() ?? DBNull.Value),
                new SqlParameter("@SetDownTime", (object?)r.SetDownTime?.Trim() ?? DBNull.Value),
                new SqlParameter("@Traces", (object?)r.Traces?.Trim() ?? DBNull.Value),
                new SqlParameter("@BusinessDate", businessDate),
                new SqlParameter("@UserID", userId)
            };

            if (r.ID > 0)
            {
                // Sửa: giữ nguyên ngày/người tạo, chỉ cập nhật ngày/người sửa
                p.Add(new SqlParameter("@ID", r.ID));
                DataTableHelper.ExecuteNonQueryText(
                    "UPDATE Item SET ItemGroupID = @ItemGroupID, Code = @Code, Name = @Name, Description = @Description, " +
                    "TransactionCode = @TransactionCode, DepartmentID = @DepartmentID, QuantityStock = @QuantityStock, " +
                    "QuantityDefault = @QuantityDefault, AvailableFrom = @AvailableFrom, AvailableTo = @AvailableTo, " +
                    "SetupTime = @SetupTime, SetDownTime = @SetDownTime, Traces = @Traces, UpdateDate = @BusinessDate, UserUpdateID = @UserID " +
                    "WHERE ID = @ID", p.ToArray());
                return r.ID;
            }

            return DataTableHelper.ExecuteInsertAndReturnId(
                "INSERT INTO Item (ItemGroupID, Code, Name, Description, TransactionCode, DepartmentID, QuantityStock, QuantityDefault, " +
                "AvailableFrom, AvailableTo, SetupTime, SetDownTime, Traces, CreateDate, UpdateDate, UserInsertID, UserUpdateID) " +
                "VALUES (@ItemGroupID, @Code, @Name, @Description, @TransactionCode, @DepartmentID, @QuantityStock, @QuantityDefault, " +
                "@AvailableFrom, @AvailableTo, @SetupTime, @SetDownTime, @Traces, @BusinessDate, @BusinessDate, @UserID, 0); " +
                "SELECT CAST(SCOPE_IDENTITY() AS int)", p.ToArray());
        }

        public bool ItemDelete(int id)
        {
            try
            {
                return DataTableHelper.ExecuteNonQueryText("DELETE FROM Item WHERE ID = @ID", new[] { new SqlParameter("@ID", id) }) > 0;
            }
            catch (SqlException ex) when (ex.Number == 547)
            {
                // Vướng khóa ngoại: Item đã được dùng ở nơi khác
                return false;
            }
        }

        public void ItemInventoryLoad(ItemInventoryLoadRequest r, int userId, DateTime businessDate)
        {
            using var conn = new SqlConnection(DBUtils.GetDBConnectionString());
            conn.Open();
            using var tx = conn.BeginTransaction();

            for (DateTime day = r.FromDate.Date; day <= r.ToDate.Date; day = day.AddDays(1))
            {
                using var cmd = new SqlCommand(
                    "UPDATE ItemInventory SET Quantity = @Qty, UpdateDate = @BusinessDate, UserUpdateID = @UserID " +
                    "WHERE ItemID = @ItemID AND DATEDIFF(DAY, [Date], @Day) = 0; " +
                    "IF @@ROWCOUNT = 0 INSERT INTO ItemInventory (ItemID, [Date], Quantity, CreateDate, UpdateDate, UserInsertID, UserUpdateID) " +
                    "VALUES (@ItemID, @Day, @Qty, @BusinessDate, @BusinessDate, @UserID, @UserID)", conn, tx);
                cmd.Parameters.AddWithValue("@ItemID", r.ItemID);
                cmd.Parameters.AddWithValue("@Day", day);
                cmd.Parameters.AddWithValue("@Qty", r.Quantity ?? 0);
                cmd.Parameters.AddWithValue("@BusinessDate", businessDate);
                cmd.Parameters.AddWithValue("@UserID", userId);
                cmd.ExecuteNonQuery();
            }

            tx.Commit();
        }

        public DataTable SearchAllForTrans(string sqlCommand)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
        new SqlParameter("@sqlCommand", sqlCommand ?? "")
            };

            DataTable resultTable = DataTableHelper.getTableData("spSearchAllForTrans", parameters);

            return resultTable;
        }
        

    }
}

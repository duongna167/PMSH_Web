using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaseBusiness.Model;
using RoomManagement.Dto;

namespace RoomManagement.Services.Interfaces
{
    public interface IRoomManagementService
    {
        DataTable Discrepancy(int sleep, int skip, int person, string floor, string room, string zone, string roomClass);
        DataTable ItemDailyInventory(
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
                DateTime fiftheenDate);
        bool UpdateItemInventory(InventoryUpdateRequest model);
        DataTable ItemInventoryAvailable(
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
                DateTime fiftheenDate);
        DataTable ItemResvDetail(int itemID, DateTime day);
        DataTable ItemSearch(string groupID, string name);
        DataTable OOOSload(int status, string roomNo, int roomClassID, DateTime fromDate, DateTime toDate, string zone);

        DataTable AvailableRoomsSearchOOO(string isDummy, string smoking,string floor, string roomTypeCode,string foStatus,string hkStatusID, string roomNo , DateTime fromDate, DateTime toDate, string zoneCode);
        DataTable RoomStatusHistoryOOO(string roomNo, DateTime fromDate, DateTime toDate, string userName);
        DataTable SearchAllForTrans(string sqlCommand);

        // ---- Item List / Item Daily Inventory ----
        DataTable ItemGroups();
        DataTable ItemNames();
        DataTable ItemDepartments();
        DataTable ItemTransactions();
        DataTable ItemGet(int id);
        int ItemSave(ItemSaveRequest request, int userId, DateTime businessDate);
        /// <summary>Xóa Item; trả về false nếu Item đang được dùng (vướng khóa ngoại).</summary>
        bool ItemDelete(int id);
        /// <summary>Nạp tồn kho mỗi ngày trong khoảng: ngày đã có thì ghi đè, chưa có thì thêm mới.</summary>
        void ItemInventoryLoad(ItemInventoryLoadRequest request, int userId, DateTime businessDate);
    }
    }

using BaseBusiness.Auth;
using BaseBusiness.BO;
using BaseBusiness.Model;
using BaseBusiness.util;
using Dapper;
using DevExpress.CodeParser;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Crypto;
using RoomManagement.Dto;
using RoomManagement.Services.Interfaces;
using System.Data;
using static BaseBusiness.util.ValidationUtils;

namespace RoomManagement.Controllers
{
    public class RoomManagementController : Controller
    {
        private readonly ILogger<RoomManagementController> _logger;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        private readonly IRoomManagementService _iRoomManagementService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RoomManagementController(ILogger<RoomManagementController> logger, IMemoryCache cache, IConfiguration configuration, IRoomManagementService iRoomManagementService, IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _cache = cache;
            _configuration = configuration;
            _iRoomManagementService = iRoomManagementService;
            _httpContextAccessor = httpContextAccessor;
        }
        // Define your actions here
        #region Discrepancy

        [RequireFunction("rmg_Discrepancy")]
        public IActionResult Discrepancy()
        {
            List<ZoneModel> listzo = PropertyUtils.ConvertToList<ZoneModel>(ZoneBO.Instance.FindAll());
            ViewBag.ZoneList = listzo;
            List<RoomClassModel> listrl = PropertyUtils.ConvertToList<RoomClassModel>(RoomClassBO.Instance.FindAll());
            ViewBag.RoomClassList = listrl;
            List<RoomModel> listroom = PropertyUtils.ConvertToList<RoomModel>(RoomBO.Instance.FindAll());
            ViewBag.RoomList = listroom;
            List<FloorModel> listfloor = PropertyUtils.ConvertToList<FloorModel>(FloorBO.Instance.FindAll());
            ViewBag.FloorList = listfloor;
            return PartialView();
        }
        [HttpGet]
        public IActionResult GetDiscrepancy(int sleep, int skip, int person, string floor, string room, string zone, string roomClass)
        {
            try
            {


                DataTable dataTable = _iRoomManagementService.Discrepancy(sleep, skip, person, floor, room, zone, roomClass);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  ID = !string.IsNullOrEmpty(d["ID"].ToString()) ? d["ID"] : "",
                                  RoomNo = !string.IsNullOrEmpty(d["Room No"].ToString()) ? d["Room No"] : "",
                                  RoomStatus = !string.IsNullOrEmpty(d["Room Status"].ToString()) ? d["Room Status"] : "",
                                  FOStatus = !string.IsNullOrEmpty(d["FO Status"].ToString()) ? d["FO Status"] : "",
                                  HKStatus = !string.IsNullOrEmpty(d["HK Status"].ToString()) ? d["HK Status"] : "",
                                  HKPersons = !string.IsNullOrEmpty(d["HK Persons"].ToString()) ? d["HK Persons"] : "",
                                  FOPersons = !string.IsNullOrEmpty(d["FO Persons"].ToString()) ? d["FO Persons"] : "",
                                  Discrepancy = !string.IsNullOrEmpty(d["Discrepancy"].ToString()) ? d["Discrepancy"] : "",
                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
            //  report.DataSource = dataTable;

            // Không cần gán parameter
            // report.RequestParameters = false;

            // return PartialView("_ReportViewerPartial", report);
        }

        #endregion

        [HttpPost("UpdateHKFOStatus")]
        public IActionResult UpdateHKFOStatus([FromBody] List<RoomUpdateDTO> dto)
        {
            if (dto == null || dto.Count == 0)
                return Json(new { success = false, message = "Không có phòng nào được chọn" });

            // Validate status
            if (dto.Any(d => d.NewHKFOStatus < 0 || d.NewHKFOStatus > 1))
                return Json(new { success = false, message = "HKFO Status không hợp lệ" });


            try
            {
                var roomIds = dto
                    .Select(d => d.RoomIds)
                    .Where(id => id > 0)
                    .Distinct()
                    .ToList();


                //  Load rooms (1 query)
                var rooms = roomIds.Any()
                    ? RoomBO.Instance.GetList<RoomModel>(
                        "SELECT * FROM Room WHERE ID IN @Ids",
                        new { Ids = roomIds })
                    : new List<RoomModel>();



                var foundIDs = rooms.Select(r => r.ID).ToList();
                var notFoundIDs = roomIds
                    .Where(id => id <= 0 || !foundIDs.Contains(id))
                    .ToList();

                foreach (var room in rooms)
                {
                    var req = dto.First(d => d.RoomIds == room.ID);

                    if (room.HKFOStatus != req.NewHKFOStatus)
                    {
                        room.HKFOStatus = req.NewHKFOStatus;
                        room.UserUpdateID = req.UpdateByID;
                        room.UpdateDate = DateTime.Now;

                        RoomBO.Instance.Update(room);
                    }
                }


                return Ok(new
                {
                    success = true,
                    updated = rooms.Count,
                    skipped = notFoundIDs
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [RequireFunction("rmg_ItemListDailyInv")]
        public IActionResult GetItemDailyInventory(
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
            try
            {
                DataTable dataTable = _iRoomManagementService.ItemDailyInventory(
                groupId,
                firstDate,
                secondDate,
                thirthDate,
                fourthDate,
                fifthDate,
                sixthDate,
                seventhDate,
                eighthDate,
                ninthDate,
                tenthDate,
                eleventhDate,
                twelvethDate,
                thirtheenDate,
                fourtheenDate,
                fiftheenDate);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  ID = !string.IsNullOrEmpty(d["ID"].ToString()) ? d["ID"] : "",
                                  ItemID = !string.IsNullOrEmpty(d["ItemID"].ToString()) ? d["ItemID"] : "",
                                  ItemName = !string.IsNullOrEmpty(d["Item Name"].ToString()) ? d["Item Name"] : "",
                                  FirstDate = !string.IsNullOrEmpty(d["FirstDate"].ToString()) ? d["FirstDate"] : "",
                                  SecondDate = !string.IsNullOrEmpty(d["SecondDate"].ToString()) ? d["SecondDate"] : "",
                                  ThirthDate = !string.IsNullOrEmpty(d["ThirthDate"].ToString()) ? d["ThirthDate"] : "",
                                  FourthDate = !string.IsNullOrEmpty(d["FourthDate"].ToString()) ? d["FourthDate"] : "",
                                  FifthDate = !string.IsNullOrEmpty(d["FifthDate"].ToString()) ? d["FifthDate"] : "",
                                  SixthDate = !string.IsNullOrEmpty(d["SixthDate"].ToString()) ? d["SixthDate"] : "",
                                  SeventhDate = !string.IsNullOrEmpty(d["SeventhDate"].ToString()) ? d["SeventhDate"] : "",
                                  EighthDate = !string.IsNullOrEmpty(d["EighthDate"].ToString()) ? d["EighthDate"] : "",
                                  NinthDate = !string.IsNullOrEmpty(d["NinthDate"].ToString()) ? d["NinthDate"] : "",
                                  TenthDate = !string.IsNullOrEmpty(d["TenthDate"].ToString()) ? d["TenthDate"] : "",
                                  EleventhDate = !string.IsNullOrEmpty(d["EleventhDate"].ToString()) ? d["EleventhDate"] : "",
                                  TwelvethDate = !string.IsNullOrEmpty(d["TwelvethDate"].ToString()) ? d["TwelvethDate"] : "",
                                  ThirtheenDate = !string.IsNullOrEmpty(d["ThirtheenDate"].ToString()) ? d["ThirtheenDate"] : "",
                                  FourtheenDate = !string.IsNullOrEmpty(d["FourtheenDate"].ToString()) ? d["FourtheenDate"] : "",
                                  FiftheenDate = !string.IsNullOrEmpty(d["FiftheenDate"].ToString()) ? d["FiftheenDate"] : "",
                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
            //  report.DataSource = dataTable;

            // Không cần gán parameter
            // report.RequestParameters = false;

            // return PartialView("_ReportViewerPartial", report);
        }
        [RequireFunction("rmg_ItemListDailyInv")]
        public IActionResult ItemDailyInventory()
        {
            return PartialView();
        }

        [HttpPost]
        public IActionResult UpdateItemInventory(int ItemID, DateTime Date, DateTime MatchDate, int Quantity, int UserID)
        {
            if (ItemID == null)
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ" });

            try
            {


                _logger.LogInformation($"UpdateItemInventory called with ItemID={ItemID}, Date={Date}, Quantity={Quantity}, UserID={UserID}");
                List<ItemInventoryModel> listzo = PropertyUtils.ConvertToList<ItemInventoryModel>(ItemInventoryBO.Instance.FindAll()).Where(x => x.ItemID == ItemID && x.Date.Date == MatchDate.Date).ToList();

                var listitem = listzo[0];
                listitem.ItemID = ItemID;

                // Thông tin người dùng
                listitem.Date = Date;
                listitem.Quantity = Quantity;
                listitem.UserUpdateID = UserID;
                listitem.UpdateDate = DateTime.Now;
                try
                {
                    ItemInventoryBO.Instance.Update(listitem);

                    return Ok(new
                    {
                        success = true,
                        message = "Inventory updated successfully"
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating inventory");

                    return BadRequest(new
                    {
                        success = false,
                        message = "Failed to update inventory"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật ItemInventory");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        [HttpGet]
        [RequireFunction("rmg_ItemInventoryAvailable")]
        public IActionResult GetItemInventoryAvailable(
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
            try
            {


                DataTable dataTable = _iRoomManagementService.ItemInventoryAvailable(
                groupId,
                firstDate,
                secondDate,
                thirthDate,
                fourthDate,
                fifthDate,
                sixthDate,
                seventhDate,
                eighthDate,
                ninthDate,
                tenthDate,
                elevenDate,
                twelveDate,
                thirtheenDate,
                fourtheenDate,
                fiftheenDate);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  ID = !string.IsNullOrEmpty(d["ID"].ToString()) ? d["ID"] : "",
                                  ItemID = !string.IsNullOrEmpty(d["ItemID"].ToString()) ? d["ItemID"] : "",
                                  ItemName = !string.IsNullOrEmpty(d["Item Name"].ToString()) ? d["Item Name"] : "",
                                  FirstDate1 = !string.IsNullOrEmpty(d["FirstDate1"].ToString()) ? d["FirstDate1"] : "",
                                  SecondDate1 = !string.IsNullOrEmpty(d["SecondDate1"].ToString()) ? d["SecondDate1"] : "",
                                  ThirthDate1 = !string.IsNullOrEmpty(d["ThirthDate1"].ToString()) ? d["ThirthDate1"] : "",
                                  FourthDate1 = !string.IsNullOrEmpty(d["FourthDate1"].ToString()) ? d["FourthDate1"] : "",
                                  FifthDate1 = !string.IsNullOrEmpty(d["FifthDate1"].ToString()) ? d["FifthDate1"] : "",
                                  SixthDate1 = !string.IsNullOrEmpty(d["SixthDate1"].ToString()) ? d["SixthDate1"] : "",
                                  SeventhDate1 = !string.IsNullOrEmpty(d["SeventhDate1"].ToString()) ? d["SeventhDate1"] : "",
                                  EighthDate1 = !string.IsNullOrEmpty(d["EighthDate1"].ToString()) ? d["EighthDate1"] : "",
                                  NinthDate1 = !string.IsNullOrEmpty(d["NinthDate1"].ToString()) ? d["NinthDate1"] : "",
                                  TenthDate1 = !string.IsNullOrEmpty(d["TenthDate1"].ToString()) ? d["TenthDate1"] : "",
                                  EleventhDate1 = !string.IsNullOrEmpty(d["EleventhDate1"].ToString()) ? d["EleventhDate1"] : "",
                                  TwelvethDate1 = !string.IsNullOrEmpty(d["TwelvethDate1"].ToString()) ? d["TwelvethDate1"] : "",
                                  ThirtheenDate1 = !string.IsNullOrEmpty(d["ThirtheenDate1"].ToString()) ? d["ThirtheenDate1"] : "",
                                  FourtheenDate1 = !string.IsNullOrEmpty(d["FourtheenDate1"].ToString()) ? d["FourtheenDate1"] : "",
                                  FiftheenDate1 = !string.IsNullOrEmpty(d["FiftheenDate1"].ToString()) ? d["FiftheenDate1"] : "",

                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
            //  report.DataSource = dataTable;

            // Không cần gán parameter
            // report.RequestParameters = false;

            // return PartialView("_ReportViewerPartial", report);
        }
        [RequireFunction("rmg_ItemInventoryAvailable")]
        public IActionResult ItemInventoryAvailable()
        {
            return PartialView();
        }
        [HttpGet]
        [RequireFunction("rmg_ItemInventoryAvailable")]
        public IActionResult GetItemResvDetail(int itemID, DateTime day)
        {
            try
            {
                DataTable dataTable = _iRoomManagementService.ItemResvDetail(itemID, day);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  ConfirmationNo = !string.IsNullOrEmpty(d["Confirmation No"].ToString()) ? d["Confirmation No"] : "",
                                  Name = !string.IsNullOrEmpty(d["Name"].ToString()) ? d["Name"] : "",
                                  RoomNo = !string.IsNullOrEmpty(d["Room No"].ToString()) ? d["Room No"] : "",
                                  ReservedFrom = !string.IsNullOrEmpty(d["Reserved From"].ToString()) ? d["Reserved From"] : "",
                                  ReservedTo = !string.IsNullOrEmpty(d["Reserved To"].ToString()) ? d["Reserved To"] : "",
                                  Qty = !string.IsNullOrEmpty(d["Qty"].ToString()) ? d["Qty"] : "",
                                  ID = !string.IsNullOrEmpty(d["ID"].ToString()) ? d["ID"] : "",

                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
            //  report.DataSource = dataTable;

            // Không cần gán parameter
            // report.RequestParameters = false;

            // return PartialView("_ReportViewerPartial", report);
        }
        [HttpGet]
        [RequireFunction("rmg_ItemListView")]
        public IActionResult GetItemSearch(string groupID, string name)
        {
            try
            {
                DataTable dataTable = _iRoomManagementService.ItemSearch(groupID, name);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  ID = !string.IsNullOrEmpty(d["ID"].ToString()) ? d["ID"] : "",
                                  Group = !string.IsNullOrEmpty(d["Group"].ToString()) ? d["Group"] : "",
                                  Code = !string.IsNullOrEmpty(d["Code"].ToString()) ? d["Code"] : "",
                                  Name = !string.IsNullOrEmpty(d["Name"].ToString()) ? d["Name"] : "",
                                  Description = !string.IsNullOrEmpty(d["Description"].ToString()) ? d["Description"] : "",
                                  TransactionCode = !string.IsNullOrEmpty(d["Transaction Code"].ToString()) ? d["Transaction Code"] : "",
                                  Cost = !string.IsNullOrEmpty(d["Cost"].ToString()) ? d["Cost"] : "",
                                  RateAmount = !string.IsNullOrEmpty(d["RateAmount"].ToString()) ? d["RateAmount"] : "",
                                  Department = !string.IsNullOrEmpty(d["Department"].ToString()) ? d["Department"] : "",
                                  QinStock = !string.IsNullOrEmpty(d["Q in Stock"].ToString()) ? d["Q in Stock"] : "",
                                  DefaultQ = !string.IsNullOrEmpty(d["Default Q"].ToString()) ? d["Default Q"] : "",
                                  SetupTime = !string.IsNullOrEmpty(d["Setup Time"].ToString()) ? d["Setup Time"] : "",
                                  SetdownTime = !string.IsNullOrEmpty(d["Setdown Time"].ToString()) ? d["Setdown Time"] : "",
                                  AvailFrom = !string.IsNullOrEmpty(d["Avail From"].ToString()) ? d["Avail From"] : "",
                                  AvailTo = !string.IsNullOrEmpty(d["Avail To"].ToString()) ? d["Avail To"] : "",
                                  Traces = !string.IsNullOrEmpty(d["Traces"].ToString()) ? d["Traces"] : "",

                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
            //  report.DataSource = dataTable;

            // Không cần gán parameter
            // report.RequestParameters = false;

            // return PartialView("_ReportViewerPartial", report);
        }
        [RequireFunction("rmg_ItemListView")]
        public IActionResult ItemSearch()
        {
            return PartialView();
        }
        // =====================================================================================================
        //  Item List / Item Daily Inventory / Item Availability
        // =====================================================================================================

        /// <summary>Dữ liệu cho các ô chọn: nhóm Item, phòng ban, mã giao dịch và danh sách Item.</summary>
        [HttpGet]
        public IActionResult ItemLookups()
        {
            try
            {
                static List<Dictionary<string, object?>> Rows(DataTable t) => t.AsEnumerable()
                    .Select(r => t.Columns.Cast<DataColumn>().ToDictionary(c => c.ColumnName, c => r[c] == DBNull.Value ? null : r[c]))
                    .ToList();

                return Json(new
                {
                    success = true,
                    groups = Rows(_iRoomManagementService.ItemGroups()),
                    items = Rows(_iRoomManagementService.ItemNames()),
                    departments = Rows(_iRoomManagementService.ItemDepartments()),
                    transactions = Rows(_iRoomManagementService.ItemTransactions())
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [RequireFunction("rmg_ItemListView")]
        public IActionResult ItemGet(int id)
        {
            try
            {
                DataTable t = _iRoomManagementService.ItemGet(id);
                if (t.Rows.Count == 0) return Json(new { success = false, message = "Item does not exist." });

                var row = t.Columns.Cast<DataColumn>().ToDictionary(c => c.ColumnName, c => t.Rows[0][c] == DBNull.Value ? null : t.Rows[0][c]);
                return Json(new { success = true, data = row });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>Thêm mới (ID = 0) hoặc sửa Item. Quyền New/Edit kiểm tra theo loại thao tác.</summary>
        [HttpPost]
        public IActionResult ItemSave([FromBody] ItemSaveRequest model)
        {
            try
            {
                if (model == null) return Json(new { success = false, message = "No data received." });

                string right = model.ID > 0 ? "rmg_ItemListEdit" : "rmg_ItemListNew";
                if (!HasFunctionRight(right)) return StatusCode(403, new { code = 403, msg = "Access Denied" });

                var errors = GetErrors(
                    Check(model.Name, "name", "Name must not be blank."),
                    Check(model.ItemGroupID, "group", "Item Group must not be blank."),
                    Check(model.QuantityStock == null, "quantityStock", "Quantity in stock must not be blank."),
                    Check((model.Code?.Length ?? 0) > 200, "code", "Code must be at most 200 characters."),
                    Check((model.Name?.Length ?? 0) > 200, "name", "Name must be at most 200 characters."),
                    Check((model.Description?.Length ?? 0) > 200, "description", "Description must be at most 200 characters."),
                    Check((model.TransactionCode?.Length ?? 0) > 400, "transactionCode", "Transaction Code is too long."),
                    Check((model.Traces?.Length ?? 0) > 510, "traces", "Trace Text must be at most 510 characters."),
                    Check((model.SetupTime?.Length ?? 0) > 100, "setupTime", "Setup Time must be at most 100 characters."),
                    Check((model.SetDownTime?.Length ?? 0) > 100, "setdownTime", "Setdown Time must be at most 100 characters."),
                    Check((model.AvailableFrom?.Length ?? 0) > 100, "availableFrom", "Available From must be at most 100 characters."),
                    Check((model.AvailableTo?.Length ?? 0) > 100, "availableTo", "Available To must be at most 100 characters.")
                );
                if (errors.Count > 0) return Json(new { success = false, errors });

                int userId = HttpContext.Session.GetInt32("UserID") ?? 0;
                DateTime businessDate = PropertyUtils.ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll())[0].BusinessDate;

                int id = _iRoomManagementService.ItemSave(model, userId, businessDate);
                return Json(new { success = true, message = "Save successfully", id });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [RequireFunction("rmg_ItemListDelete")]
        public IActionResult ItemDelete(int id)
        {
            try
            {
                if (id <= 0) return Json(new { success = false, message = "Please select an item." });

                return _iRoomManagementService.ItemDelete(id)
                    ? Json(new { success = true, message = "Delete successfully" })
                    : Json(new { success = false, message = "Cannot delete this item because it is in use." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>Nạp số lượng tồn kho cho một Item theo khoảng ngày (không cho chọn ngày quá khứ).</summary>
        [HttpPost]
        [RequireFunction("rmg_ItemListDailyInv")]
        public IActionResult ItemInventoryLoad([FromBody] ItemInventoryLoadRequest model)
        {
            try
            {
                if (model == null) return Json(new { success = false, message = "No data received." });

                DateTime businessDate = PropertyUtils.ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll())[0].BusinessDate;

                var errors = GetErrors(
                    Check(model.ItemID, "item", "Please select an item."),
                    Check(model.FromDate, "fromDate", "Begin Date is required."),
                    Check(model.ToDate, "toDate", "End Date is required."),
                    Check(model.FromDate != default && model.FromDate.Date < businessDate.Date, "fromDate", "Can not choose the past date."),
                    Check(model.FromDate != default && model.ToDate != default && model.ToDate.Date < model.FromDate.Date, "toDate", "End Date must be later than Begin Date."),
                    Check(model.Quantity == null || model.Quantity < 0, "quantity", "Quantity is wrong format.")
                );

                // Số lượng nạp không được vượt tồn kho khai báo của Item
                if (errors.Count == 0)
                {
                    DataTable item = _iRoomManagementService.ItemGet(model.ItemID);
                    if (item.Rows.Count == 0)
                        errors.Add(new ValidationError { Field = "item", Message = "Item does not exist." });
                    else
                    {
                        int stock = item.Rows[0]["QuantityStock"] == DBNull.Value ? 0 : Convert.ToInt32(item.Rows[0]["QuantityStock"]);
                        if (model.Quantity > stock)
                            errors.Add(new ValidationError { Field = "quantity", Message = $"You loaded over quantity for this item, quantity must be less than or equal to {stock}." });
                    }
                }
                if (errors.Count > 0) return Json(new { success = false, errors });

                int userId = HttpContext.Session.GetInt32("UserID") ?? 0;
                _iRoomManagementService.ItemInventoryLoad(model, userId, businessDate);
                return Json(new { success = true, message = "Save successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>Người dùng đang đăng nhập có quyền (FormAndFunction) này không.</summary>
        private bool HasFunctionRight(string code)
        {
            string raw = HttpContext.Session.GetString("PermissionCodes") ?? "";
            return raw.Contains("\"" + code + "\"", StringComparison.OrdinalIgnoreCase);
        }


        #region avaiable room search
        [HttpGet]
        public IActionResult GetAvailableRoomsSearchOOO(string isDummy, string smoking, string floor, string roomTypeCode, string foStatus, string hkStatusID, string roomNo, DateTime fromDate, DateTime toDate, string zoneCode)
        {
            try
            {
                DataTable dataTable = _iRoomManagementService.AvailableRoomsSearchOOO(isDummy, smoking, floor, roomTypeCode, foStatus, hkStatusID, roomNo, fromDate, toDate, zoneCode);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  RoomID = !string.IsNullOrEmpty(d["RoomID"].ToString()) ? d["RoomID"] : "",
                                  RoomNo = !string.IsNullOrEmpty(d["RoomNo"].ToString()) ? d["RoomNo"] : "",
                                  RoomType = !string.IsNullOrEmpty(d["Room Type"].ToString()) ? d["Room Type"] : "",
                                  RoomTypeName = !string.IsNullOrEmpty(d["Room Type Name"].ToString()) ? d["Room Type Name"] : "",
                                  HKStatus = !string.IsNullOrEmpty(d["HK Status"].ToString()) ? d["HK Status"] : "",
                                  FO = !string.IsNullOrEmpty(d["FO"].ToString()) ? d["FO"] : "",
                                  Floor = !string.IsNullOrEmpty(d["Floor"].ToString()) ? d["Floor"] : "",
                                  Smoking = !string.IsNullOrEmpty(d["Smoking"].ToString()) ? d["Smoking"] : "",
                                  RoomTypeID = !string.IsNullOrEmpty(d["RoomTypeID"].ToString()) ? d["RoomTypeID"] : "",
                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
            //  report.DataSource = dataTable;

            // Không cần gán parameter
            // report.RequestParameters = false;

            // return PartialView("_ReportViewerPartial", report);
        }

        //[HttpPost]
        //public ActionResult SaveOOOS()
        //{
        //    ProcessTransactions pt = new ProcessTransactions();
        //    try
        //    {
        //        pt.OpenConnection();
        //        pt.BeginTransaction();
        //        string rooomSelectString = Request.Form["roomSelect"].ToString();
        //        List<int> roomSelect = rooomSelectString.Split(',')
        //                                            .Select(x => int.Parse(x)).Where(x => x != 0)

        //                                            .ToList();
        //        // check  chon  room
        //        if(roomSelect.Count < 1)
        //        {
        //            return Json(new { code = 1, msg = "New item out of order/service created successfully" });

        //        }

        //        DateTime toDate = DateTime.Parse(Request.Form["itemToDate"].ToString());
        //        DateTime fromDate = DateTime.Parse(Request.Form["itemFromDate"].ToString());

        //        for(int i = 0; i < roomSelect.Count; i++)
        //        {
        //            if()
        //            BusinessBlockModel BusinessBlock = new BusinessBlockModel();

        //        }
        //        #endregion
        //        pt.CommitTransaction();
        //        return Json(new { code = 0, msg = "New item out of order/service created successfully" });

        //    }
        //    catch (Exception ex)
        //    {
        //        pt.RollBack();
        //        return Json(new { code = 1, msg = ex.Message });
        //    }
        //    finally
        //    {
        //        pt.CloseConnection();

        //    }
        //}
        #endregion

        [HttpGet]
        public IActionResult GetCards()
        {
            string sql = @"SELECT a.*, N'Card Hotel' AS CardType, 
                    CASE 
                        WHEN a.Status = 0 THEN N'Inactive' 
                        WHEN a.Status = 1 THEN N'Active' 
                        ELSE N'Other' 
                    END AS StatusText 
                    FROM Card a WITH (NOLOCK)";

            var dt = _iRoomManagementService.SearchAllForTrans(sql);

            return Json(dt);
        }
        [HttpPost]
        public IActionResult InsertBusinessBlock()
        {
            ProcessTransactions pt = new ProcessTransactions();
            bool isTransactionActive = false;

            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();
                isTransactionActive = true;

                // Lấy danh sách phòng
                var roomIds = Request.Form["roomSelect"].ToString()
                                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(int.Parse).ToList();
                DateTime fromDate = DateTime.TryParse(Request.Form["itemFromDate"], out var fd) ? fd : DateTime.Now;
                DateTime toDate = DateTime.TryParse(Request.Form["itemToDate"], out var td) ? td : DateTime.Now;
                byte oooOrS = byte.TryParse(Request.Form["oooOrS"], out var ooo) ? ooo : (byte)0;
                int rtStatus = int.TryParse(Request.Form["rtStatus"], out var rt) ? rt : 0;
                string reasonCode = Request.Form["comment"].ToString();
                string reasonNote = Request.Form["txtReasonDesc"].ToString();
                var userId = HttpContext.Session.GetInt32("UserID") ?? 0;
                long lastInsertId = 0;

                foreach (var roomId in roomIds)
                {
                    string name = $"OutOfOrder/Service";
                    string roomNo = RoomBO.Instance.GetRoomNoById(roomId, pt.Connection, pt.Transaction);
                    BusinessBlockModel bb = new BusinessBlockModel()
                    {
                        RoomID = roomId,
                        RoomNo = roomNo,
                        Name = name,
                        FromDateOOO = fromDate,
                        ToDateOOO = toDate,
                        OOOStatus = oooOrS,
                        ReturnStatus = rtStatus,
                        ReasonID = !string.IsNullOrEmpty(reasonCode) ? int.Parse(reasonCode) : 0,
                        ReasonNote = reasonNote,
                        UserInsertID = userId,
                        CreateDate = DateTime.Now
                    };

                    lastInsertId = BusinessBlockBO.Instance.Insert(bb, pt.Connection, pt.Transaction);
                    string code = $"OOOS{lastInsertId}";

                    // Update code vào record vừa insert
                    BusinessBlockBO.Instance.Update(lastInsertId, code, pt.Connection, pt.Transaction);
                }

                pt.CommitTransaction();
                isTransactionActive = false;

                return Json(new { success = true, lastInsertId });
            }
            catch (Exception ex)
            {
                if (isTransactionActive)
                {
                    try { pt.RollBack(); } catch { }
                }
                return Json(new { success = false, message = ex.Message });
            }
            finally
            {
                pt.CloseConnection();
            }
        }


        #region Out of Order/Service (OOOS)
        [HttpGet]
        public IActionResult GetOOOSload(int status, string roomNo, int roomClassID, DateTime fromDate, DateTime toDate, string zone)
        {
            try
            {
                DataTable dataTable = _iRoomManagementService.OOOSload(status, roomNo, roomClassID, fromDate, toDate, zone);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  ID = !string.IsNullOrEmpty(d["ID"].ToString()) ? d["ID"] : "",
                                  Code = !string.IsNullOrEmpty(d["Code"].ToString()) ? d["Code"] : "",
                                  RoomNo = !string.IsNullOrEmpty(d["Room No"].ToString()) ? d["Room No"] : "",
                                  RoomID = !string.IsNullOrEmpty(d["RoomID"].ToString()) ? d["RoomID"] : "",
                                  RoomType = !string.IsNullOrEmpty(d["Room Type"].ToString()) ? d["Room Type"] : "",
                                  Floor = !string.IsNullOrEmpty(d["Floor"].ToString()) ? d["Floor"] : "",
                                  Zone = !string.IsNullOrEmpty(d["Zone"].ToString()) ? d["Zone"] : "",
                                  FromDate = !string.IsNullOrEmpty(d["From Date"].ToString()) ? d["From Date"] : "",
                                  ToDate = !string.IsNullOrEmpty(d["To Date"].ToString()) ? d["To Date"] : "",
                                  NoOfNights = !string.IsNullOrEmpty(d["No Of Nights"].ToString()) ? d["No Of Nights"] : "",
                                  ReasonCode = !string.IsNullOrEmpty(d["Reason Code"].ToString()) ? d["Reason Code"] : "",
                                  Reason = !string.IsNullOrEmpty(d["Reason"].ToString()) ? d["Reason"] : "",
                                  ReasonID = !string.IsNullOrEmpty(d["ReasonID"].ToString()) ? d["ReasonID"] : "",
                                  ReturnStatusID = !string.IsNullOrEmpty(d["ReturnStatusID"].ToString()) ? d["ReturnStatusID"] : "",
                                  ReturnStatus = !string.IsNullOrEmpty(d["Return Status"].ToString()) ? d["Return Status"] : "",
                                  Status = !string.IsNullOrEmpty(d["Status"].ToString()) ? d["Status"] : "",
                                  UserCreate = !string.IsNullOrEmpty(d["User Create"].ToString()) ? d["User Create"] : "",
                                  CreatedDate = !string.IsNullOrEmpty(d["Created Date"].ToString()) ? d["Created Date"] : "",
                                  UserUpdate = !string.IsNullOrEmpty(d["User Update"].ToString()) ? d["User Update"] : "",
                                  UpdatedDate = !string.IsNullOrEmpty(d["Updated Date"].ToString()) ? d["Updated Date"] : "",
                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        [RequireFunction("rmg_OOOS_View")]
        public IActionResult OOOSload()
        {
            List<RoomModel> listroom = PropertyUtils.ConvertToList<RoomModel>(RoomBO.Instance.FindAll());
            ViewBag.RoomList = listroom;
            List<ZoneModel> listzone = PropertyUtils.ConvertToList<ZoneModel>(ZoneBO.Instance.FindAll());
            ViewBag.ZoneList = listzone;
            List<RoomClassModel> listrclass = PropertyUtils.ConvertToList<RoomClassModel>(RoomClassBO.Instance.FindAll());
            ViewBag.RoomClassList = listrclass;
            List<CommentModel> listcmt = PropertyUtils.ConvertToList<CommentModel>(CommentBO.Instance.FindAll());
            ViewBag.CommentList = listcmt;
            return PartialView();
        }
        [HttpPost]
        public IActionResult DeleteBusinessBlock(int id)
        {
            try
            {
                string sql = "DELETE FROM BusinessBlock WHERE ID = @ID";

                SqlParameter[] parameters = {
                new SqlParameter("@ID", id)
        };

                int rows = DataTableHelper.ExecuteNonQueryText(sql, parameters);

                return Json(new { deleted = rows });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        [HttpGet]
        public IActionResult GetRoomStatusHistoryOOO(string roomNo, DateTime fromDate, DateTime toDate, string userName)
        {
            try
            {
                DataTable dataTable = _iRoomManagementService.RoomStatusHistoryOOO(roomNo, fromDate, toDate, userName);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {

                                  RoomNo = !string.IsNullOrEmpty(d["RoomNo"].ToString()) ? d["RoomNo"] : "",
                                  OldValue = !string.IsNullOrEmpty(d["OldValue"].ToString()) ? d["OldValue"] : "",
                                  NewValue = !string.IsNullOrEmpty(d["NewValue"].ToString()) ? d["NewValue"] : "",
                                  UserName = !string.IsNullOrEmpty(d["UserName"].ToString()) ? d["UserName"] : "",
                                  Action = !string.IsNullOrEmpty(d["Action"].ToString()) ? d["Action"] : "",
                                  ComputerName = !string.IsNullOrEmpty(d["ComputerName"].ToString()) ? d["ComputerName"] : "",
                                  ChangeDate = !string.IsNullOrEmpty(d["ChangeDate"].ToString()) ? d["ChangeDate"] : "",

                              }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
            //  report.DataSource = dataTable;

            // Không cần gán parameter
            // report.RequestParameters = false;

            // return PartialView("_ReportViewerPartial", report);
        }

        [HttpPost]
        public IActionResult SaveBusinessBlock([FromBody] List<BusinessBlockModel> models)
        {
            try
            {
                var listErrors = GetErrors(
                    Check(models, "general", "No data received."),
                    Check(models != null && models.Count == 0, "general", "No data received.")
                );

                if (listErrors.Count > 0)
                {
                    return Json(new { success = false, errors = listErrors });
                }

                foreach (var model in models)
                {
                    var itemErrors = GetErrors(
                            Check(model, "general", "Invalid data"),
                            Check(model.FromDateOOO == default, "itemFromDate", "From Date is required."),
                            Check(model.ToDateOOO == default, "itemToDate", "To Date is required."),
                            Check(model.FromDateOOO >= model.ToDateOOO, "itemToDate", "To Date must be >= From Date"),
                            Check(model.ReasonID, "comment", "Please select a reason!"),
                            Check(model.RoomID, "roomSelect", "Haven't selected a room yet!")
                    );

                    if (itemErrors.Count > 0)
                    {
                        return Json(new { success = false, errors = itemErrors });
                    }
                }

                var businessDates = PropertyUtils.ConvertToList<BusinessDateModel>(
                    BusinessDateBO.Instance.FindAll()
                );

                DateTime businessDate = businessDates[0].BusinessDate;

                foreach (var model in models)
                {
                    if (model.ID > 0)
                    {
                        var oldData = (BusinessBlockModel)BusinessBlockBO.Instance.FindByPrimaryKey(model.ID);
                        model.CreateDate = oldData.CreateDate;
                        model.UserInsertID = oldData.UserInsertID;
                        model.Name = oldData.Name;
                        model.Code = oldData.Code;
                        model.UpdateDate = DateTime.Now;
                        BusinessBlockBO.Instance.Update(model);
                    } else {
                        string newCode;
                        int maxTry = 20; // tránh loop vô hạn
                        int tryCount = 0;

                        do
                        {
                            newCode = GenerateOOOSCode();
                            tryCount++;

                            if (tryCount > maxTry)
                                throw new Exception("Unable to generate unique code.");
                        }
                        while (BusinessBlockBO.Instance.IsDuplicateCode(newCode, 0));
                        model.Code = newCode;
                        model.Name = "OutOfOrder/Service";
                        model.CreateDate = businessDate;
                        model.UpdateDate = DateTime.Now;
                        BusinessBlockBO.Instance.Insert(model);
                    }
                }

                return Json(new { success = true, message = "Save successfully" });
            
            } catch (Exception ex) {
                 return Json(new { success = false, message = "Error: " + ex.Message });   
            }

        }
        private string GenerateOOOSCode()
        {
            var random = new Random();
            return $"OOOS{random.Next(0, 999999):D6}";
        }


        #endregion

    }
}

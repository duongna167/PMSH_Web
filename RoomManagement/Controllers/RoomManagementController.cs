using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaseBusiness.BO;
using BaseBusiness.Model;
using BaseBusiness.util;
using Dapper;
using DevExpress.XtraPrinting.Native;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RoomManagement.Dto;
using RoomManagement.Services.Implements;
using RoomManagement.Services.Interfaces;
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
            return View();
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

        [HttpPost("UpdateHKFOStatus")]
        public IActionResult UpdateHKFOStatus([FromBody] RoomUpdateDTO dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.RoomNo))
                return BadRequest(new { success = false, message = "Không có phòng nào được chọn" });

            try
            {
                string connString = new AppConfiguration()?.ConnectionString;
                if (string.IsNullOrEmpty(connString))
                    return BadRequest(new { success = false, message = "Chuỗi kết nối không hợp lệ" });

                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();

                    string sql = @"
                        UPDATE Room
                        SET HKFOStatus = @NewHKFOStatus
                        WHERE RoomNo = @RoomNo
                  ";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.Add("@NewHKFOStatus", SqlDbType.Int).Value = dto.NewHKFOStatus;
                        cmd.Parameters.Add("@RoomNo", SqlDbType.VarChar, 10).Value = dto.RoomNo;

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected == 0)
                            return BadRequest(new { success = false, message = "Phòng không thỏa điều kiện hoặc không tồn tại" });
                    }
                }

                return Ok(new { success = true, message = "Cập nhật thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.ToString() });
            }
        }

        [HttpGet]
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
        public IActionResult ItemDailyInventory()
        {
            return View();
        }

        [HttpPost]
        public IActionResult UpdateItemInventory(int ItemID, DateTime Date, DateTime MatchDate,int Quantity,int UserID)
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
        public IActionResult ItemInventoryAvailable()
        {
            return View();
        }
        [HttpGet]
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
        public IActionResult ItemResvDetail()
        {
            return View();
        }
        [HttpGet]
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
        public IActionResult ItemSearch()
        {
            List<TransactionsModel> listpmtr = PropertyUtils.ConvertToList<TransactionsModel>(TransactionsBO.Instance.FindAll());
            ViewBag.TransactionsList = listpmtr;
            List<DepartmentModel> listdpm = PropertyUtils.ConvertToList<DepartmentModel>(DepartmentBO.Instance.FindAll());
            ViewBag.DepartmentList = listdpm;
            return View();
        }
        [HttpPost]
        [HttpPost]
        public int InsertItem([FromBody] ItemDto model)
        {
            using (SqlConnection conn = new SqlConnection(DBUtils.GetDBConnectionString()))
            {
                conn.Open();
                var userId = HttpContext.Session.GetInt32("UserID") ?? 0;
                using (SqlCommand cmd = new SqlCommand("sp_executesql", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    string sqlInsert = @"
                INSERT INTO Item 
                (Code, Name, Description, TransactionCode, ArticleCode, Cost, RateCodeID, RateAmount, ItemGroupID, DepartmentID, QuantityStock, QuantityDefault, SetupTime, SetDownTime, AvailableFrom, AvailableTo, Traces, Attribute, CreateDate, UpdateDate, UserInsertID, UserUpdateID) 
                VALUES 
                (@Code, @Name, @Description, @TransactionCode, @ArticleCode, @Cost, @RateCodeID, @RateAmount, @ItemGroupID, @DepartmentID, @QuantityStock, @QuantityDefault, @SetupTime, @SetDownTime, @AvailableFrom, @AvailableTo, @Traces, @Attribute, @CreateDate, @UpdateDate, @UserInsertID, @UserUpdateID); 
                SELECT @@IDENTITY AS 'ID'";

                    string parameterDefs = @"
                @Code nvarchar(1),
                @Name nvarchar(1),
                @Description nvarchar(1),
                @TransactionCode nvarchar(9),
                @ArticleCode nvarchar(4000),
                @Cost nvarchar(4000),
                @RateCodeID int,
                @RateAmount decimal(1,0),
                @ItemGroupID int,
                @DepartmentID int,
                @QuantityStock int,
                @QuantityDefault int,
                @SetupTime nvarchar(1),
                @SetDownTime nvarchar(1),
                @AvailableFrom nvarchar(10),
                @AvailableTo nvarchar(10),
                @Traces nvarchar(1),
                @Attribute nvarchar(4000),
                @CreateDate datetime,
                @UpdateDate datetime,
                @UserInsertID int,
                @UserUpdateID int";

                    cmd.Parameters.AddWithValue("@stmt", sqlInsert);
                    cmd.Parameters.AddWithValue("@params", parameterDefs);
                    cmd.Parameters.AddWithValue("@Code", model.Code);
                    cmd.Parameters.AddWithValue("@Name", model.Name);
                    cmd.Parameters.AddWithValue("@Description", model.Description);
                    cmd.Parameters.AddWithValue("@TransactionCode", model.TransactionCode);
                    cmd.Parameters.AddWithValue("@ArticleCode", model.ArticleCode);
                    cmd.Parameters.AddWithValue("@Cost", model.Cost);
                    cmd.Parameters.AddWithValue("@RateCodeID", model.RateCodeID);
                    cmd.Parameters.AddWithValue("@RateAmount", model.RateAmount);
                    cmd.Parameters.AddWithValue("@ItemGroupID", model.ItemGroupID);
                    cmd.Parameters.AddWithValue("@DepartmentID", model.DepartmentID);
                    cmd.Parameters.AddWithValue("@QuantityStock", model.QuantityStock);
                    cmd.Parameters.AddWithValue("@QuantityDefault", model.QuantityDefault);
                    cmd.Parameters.AddWithValue("@SetupTime", model.SetupTime);
                    cmd.Parameters.AddWithValue("@SetDownTime", model.SetDownTime);
                    cmd.Parameters.AddWithValue("@AvailableFrom", model.AvailableFrom);
                    cmd.Parameters.AddWithValue("@AvailableTo", model.AvailableTo);
                    cmd.Parameters.AddWithValue("@Traces", model.Traces);
                    cmd.Parameters.AddWithValue("@Attribute", model.Attribute);
                    cmd.Parameters.AddWithValue("@CreateDate", model.CreateDate);
                    cmd.Parameters.AddWithValue("@UpdateDate", model.UpdateDate);
                    cmd.Parameters.AddWithValue("@UserInsertID", userId);
                    cmd.Parameters.AddWithValue("@UserUpdateID", model.UserUpdateID);

                    object result = cmd.ExecuteScalar();
                    return Convert.ToInt32(result);
                }
            }
        }
        [HttpPost]
        public IActionResult UpdateItem([FromBody] ItemModel item)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(DBUtils.GetDBConnectionString()))
                {
                    conn.Open();

                    var cmd = new SqlCommand(@"
                UPDATE Item SET 
                    Code = @Code,
                    Name = @Name,
                    Description = @Description,
                    TransactionCode = @TransactionCode,
                    ArticleCode = @ArticleCode,
                    Cost = @Cost,
                    RateCodeID = @RateCodeID,
                    RateAmount = @RateAmount,
                    ItemGroupID = @ItemGroupID,
                    DepartmentID = @DepartmentID,
                    QuantityStock = @QuantityStock,
                    QuantityDefault = @QuantityDefault,
                    SetupTime = @SetupTime,
                    SetDownTime = @SetDownTime,
                    AvailableFrom = @AvailableFrom,
                    AvailableTo = @AvailableTo,
                    Traces = @Traces,
                    Attribute = @Attribute,
                    CreateDate = @CreateDate,
                    UpdateDate = @UpdateDate,
                    UserInsertID = @UserInsertID,
                    UserUpdateID = @UserUpdateID
                WHERE ID = @ID", conn);

                    cmd.Parameters.AddWithValue("@ID", item.ID);
                    cmd.Parameters.AddWithValue("@Code", item.Code ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Name", item.Name ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Description", item.Description ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@TransactionCode", item.TransactionCode ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ArticleCode", item.ArticleCode ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Cost", item.Cost ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@RateCodeID", item.RateCodeID);
                    cmd.Parameters.AddWithValue("@RateAmount", item.RateAmount);
                    cmd.Parameters.AddWithValue("@ItemGroupID", item.ItemGroupID);
                    cmd.Parameters.AddWithValue("@DepartmentID", item.DepartmentID);
                    cmd.Parameters.AddWithValue("@QuantityStock", item.QuantityStock);
                    cmd.Parameters.AddWithValue("@QuantityDefault", item.QuantityDefault);
                    cmd.Parameters.AddWithValue("@SetupTime", item.SetupTime ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@SetDownTime", item.SetDownTime ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@AvailableFrom", string.IsNullOrEmpty(item.AvailableFrom) ? (object)DBNull.Value : item.AvailableFrom);
                    cmd.Parameters.AddWithValue("@AvailableTo", string.IsNullOrEmpty(item.AvailableTo) ? (object)DBNull.Value : item.AvailableTo);
                    cmd.Parameters.AddWithValue("@Traces", item.Traces ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Attribute", item.Attribute ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CreateDate", item.CreateDate);
                    cmd.Parameters.AddWithValue("@UpdateDate", item.UpdateDate);
                    cmd.Parameters.AddWithValue("@UserInsertID", item.UserInsertID);
                    cmd.Parameters.AddWithValue("@UserUpdateID", item.UserUpdateID);

                    int rows = cmd.ExecuteNonQuery();

                    return Ok(rows > 0 ? "Update thành công" : "Không có dòng nào được cập nhật");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Lỗi khi cập nhật: " + ex.Message);
            }
        }

        [HttpPost]
        [HttpPost]
        public IActionResult DeleteItem(int id)
        {
            try
            {
                string sql = "DELETE FROM Item WHERE ID = @ID";

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
            //  report.DataSource = dataTable;

            // Không cần gán parameter
            // report.RequestParameters = false;

            // return PartialView("_ReportViewerPartial", report);
        }
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
            return View();
        }

        #region out of order/service management
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
        [HttpPost]
        public IActionResult UpdateandInsertBusinessBlock()
        {
            ProcessTransactions pt = new ProcessTransactions();

            try
            {
                string roomIdStr = Request.Form["roomSelect"].ToString() ?? "";
                string roomNoStr = Request.Form["roomNo"].ToString() ?? "";
                string fromDateStr = Request.Form["itemFromDate"].ToString();
                string toDateStr = Request.Form["itemToDate"].ToString();
                string commentStr = Request.Form["comment"].ToString();
                string idStr = Request.Form["id"].ToString();

                DateTime.TryParse(fromDateStr, out DateTime fromDate);
                DateTime.TryParse(toDateStr, out DateTime toDate);

                var listErrors = GetErrors(
                    Check(string.IsNullOrWhiteSpace(roomIdStr), "roomSelect", "Haven't selected a room yet!"),

                    Check(fromDate == default, "itemFromDate", "From Date is required!"),
                    Check(toDate == default, "itemToDate", "To Date is required!"),
                    Check(toDate <= fromDate && fromDate != default, "itemToDate", "To Date must be greater than From Date!"),

                    Check(string.IsNullOrWhiteSpace(commentStr), "comment", "Please select a reason!")
                );

                if (listErrors.Count > 0)
                    return Json(new { success = false, errors = listErrors });


                pt.OpenConnection();
                pt.BeginTransaction();

                string[] roomIds = roomIdStr.Split(',', StringSplitOptions.RemoveEmptyEntries);
                string[] roomNos = roomNoStr.Split(',', StringSplitOptions.RemoveEmptyEntries);
                int userId = HttpContext.Session.GetInt32("UserID") ?? 0;
                byte oooStatus = byte.TryParse(Request.Form["oooOrS"], out byte b) ? b : (byte)0;
                int returnStatus = int.TryParse(Request.Form["rtStatus"], out int rs) ? rs : 0;
                int reasonId = int.Parse(commentStr);
                string reasonNote = Request.Form["txtReasonDesc"].ToString();
                int blockId = int.TryParse(idStr, out int bid) ? bid : 0;

                for (int i = 0; i < roomIds.Length; i++)
                {
                    int roomID = int.Parse(roomIds[i]);
                    string roomNo = roomNos[i];

                    BusinessBlockModel bbModel;
                    if (blockId > 0)
                    {
                        bbModel = (BusinessBlockModel)BusinessBlockBO.Instance.FindByPrimaryKey(blockId) ?? new BusinessBlockModel();
                    }
                    else
                    {
                        bbModel = new BusinessBlockModel();
                    }

                    // GÁN DỮ LIỆU
                    bbModel.Code = $"OOOS{blockId}";
                    bbModel.RoomID = roomID;
                    bbModel.RoomNo = roomNo;
                    bbModel.Name = "OutOfOrder/Service";
                    bbModel.FromDateOOO = fromDate;
                    bbModel.ToDateOOO = toDate;
                    bbModel.OOOStatus = oooStatus;
                    bbModel.ReturnStatus = returnStatus;
                    bbModel.ReasonID = reasonId;
                    bbModel.ReasonNote = reasonNote;
                    bbModel.UserUpdateID = userId;
                    bbModel.UpdateDate = DateTime.Now;

                    if (blockId > 0)
                    {
                        BusinessBlockBO.Instance.Update(bbModel);
                    }
                    else
                    {
                        bbModel.UserInsertID = userId;
                        bbModel.CreateDate = DateTime.Now;
                        BusinessBlockBO.Instance.Insert(bbModel);
                    }
                }

                pt.CommitTransaction();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                pt.RollBack();
                return Json(new { success = false, message = ex.Message });
            }
            finally
            {
                pt.CloseConnection();
            }
        }


    }


}


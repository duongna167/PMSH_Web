using System.Collections;
using System.Data;
using System.Linq;
using Administration.DTO;
using Administration.Services;
using Administration.Services.Interfaces;
using BaseBusiness.BO;
using BaseBusiness.Model;
using BaseBusiness.util;
using Microsoft.AspNetCore.Mvc;
using static Administration.DTO.RateCodeDetailDTO;

namespace Administration.Controllers
{
    [Route("/Administration/RateCode")]
    public class RateCodeDetailController(IRateCodeDetailService detail) : Controller
    {
        private readonly IRateCodeDetailService _detail = detail;

        [HttpGet("RateCodeDetail")] // Truyeenf DataGrid , script api
        public IActionResult RateCodeDetail()
        {
            List<RateCodeModel> listRateCode = PropertyUtils.ConvertToList<RateCodeModel>(RateCodeBO.Instance.FindAll());
            List<RateCategoryModel> listRateCate = PropertyUtils.ConvertToList<RateCategoryModel>(RateCategoryBO.Instance.FindAll());
            List<SeasonModel> listSeason = PropertyUtils.ConvertToList<SeasonModel>(SeasonBO.Instance.FindAll());
            List<PackageModel> listPackage = PropertyUtils.ConvertToList<PackageModel>(PackageBO.Instance.FindAll());
            List<RoomTypeModel> listRoomType = PropertyUtils.ConvertToList<RoomTypeModel>(RoomTypeBO.Instance.FindAll());
            List<TransactionsModel> listTransaction = PropertyUtils.ConvertToList<TransactionsModel>(TransactionsBO.Instance.FindAll());
            ViewBag.RateCodeList = listRateCode;
            ViewBag.RateCateList = listRateCate;
            ViewBag.RateSeason = listSeason;
            ViewBag.PackageList = listPackage;
            ViewBag.RoomTypeList = listRoomType;
            ViewBag.TransactionList = listTransaction;
            return PartialView("~/Views/Administration/RateCode/RateCodeDetail.cshtml");
            // Truyền đường dẫn chuẩn vào để tìm đúng
        }

        [HttpGet("GetAllRateCodeDetail")]
        public async Task<IActionResult> GetAllRateCodeDetail(
            string? rateCode,
            string? rateCategory,
            int? typeOfDate,
            DateTime? fromDate,
            DateTime? toDate)
        {
            try
            {
                DataTable dataTable = await _detail.RateCodeTypeData(rateCode, rateCategory, typeOfDate, fromDate, toDate);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  RateCode = d["RateCode"]?.ToString() ?? "",
                                  Description = d["Description"]?.ToString() ?? "",
                                  RateCategory = d["RateCategory"]?.ToString() ?? "",
                                  IDRateCode = d["ID"] != DBNull.Value ? Convert.ToInt32(d["ID"]) : 0,
                                  BeginDate = d["BeginDate"] != DBNull.Value ? Convert.ToDateTime(d["BeginDate"]) : (DateTime?)null,
                                  EndDate = d["EndDate"] != DBNull.Value ? Convert.ToDateTime(d["EndDate"]) : (DateTime?)null
                              }).ToList();

                return Json(result);

            }
            catch (Exception ex)
            {

                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("RateCodeGroupDataByID")]
        public async Task<IActionResult> RateCodeGroupDataByID(int? rateCodeID)
        {
            try
            {
                DataTable dataTable = await _detail.RateCodeGroupDataByID(rateCodeID);
                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  code = d["RateCode"]?.ToString() ?? "",
                                  roomType = d["RoomType"]?.ToString() ?? "",
                                  package = d["Package"]?.ToString() ?? "",
                                  beginDate = d["BeginDate"] != DBNull.Value ? Convert.ToDateTime(d["BeginDate"]) : (DateTime?)null,
                                  endDate = d["EndDate"] != DBNull.Value ? Convert.ToDateTime(d["EndDate"]) : (DateTime?)null,
                                  transaction = d["TransactionCode"]?.ToString() ?? "",
                                  curr = d["CurrencyID"]?.ToString() ?? "",
                                  RateCodeID = d["RateCodeID"] != DBNull.Value ? Convert.ToInt32(d["RateCodeID"]) : 0,
                                  PackageID = d["PackageID"] != DBNull.Value ? Convert.ToInt32(d["PackageID"]) : (int?)null,
                                  RoomTypeID = d["RoomTypeID"] != DBNull.Value ? Convert.ToInt32(d["RoomTypeID"]) : 0,
                                  A1 = d["A1"] != DBNull.Value ? Convert.ToInt32(d["A1"]) : 0,
                                  A2 = d["A2"] != DBNull.Value ? Convert.ToInt32(d["A2"]) : 0,
                                  A3 = d["A3"] != DBNull.Value ? Convert.ToInt32(d["A3"]) : 0,
                                  A4 = d["A4"] != DBNull.Value ? Convert.ToInt32(d["A4"]) : 0,
                                  A5 = d["A5"] != DBNull.Value ? Convert.ToInt32(d["A5"]) : 0,
                                  A6 = d["A6"] != DBNull.Value ? Convert.ToInt32(d["A6"]) : 0,
                                  C1 = d["C1"] != DBNull.Value ? Convert.ToInt32(d["C1"]) : 0,
                                  C2 = d["C2"] != DBNull.Value ? Convert.ToInt32(d["C2"]) : 0,
                                  C3 = d["C3"] != DBNull.Value ? Convert.ToInt32(d["C3"]) : 0,
                                  MinLOS = d["MinLOS"] != DBNull.Value ? Convert.ToInt32(d["MinLOS"]) : 0,
                                  MaxLOS = d["MaxLOS"] != DBNull.Value ? Convert.ToInt32(d["MaxLOS"]) : 0,
                                  MinNoOfRoom = d["MinNoOfRoom"] != DBNull.Value ? Convert.ToInt32(d["MinNoOfRoom"]) : 0,
                                  MaxNoOfRoom = d["MaxNoOfRoom"] != DBNull.Value ? Convert.ToInt32(d["MaxNoOfRoom"]) : 0
                              }).ToList();

                return Json(result);

            }
            catch (Exception ex)
            {

                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        [HttpGet("GetDetails")]
        public async Task<IActionResult> GetDetails([FromQuery] RateCodeDetailInputDto input)
        {
            try
            {
                DataTable dt = await _detail.GetRateCodeDetailsAsync(input);

                var result = dt.AsEnumerable().Select(d => new RateCodeDetailOutputDto
                {
                    ID = d["ID"] != DBNull.Value ? Convert.ToInt32(d["ID"]) : 0,
                    RateCode = d["RateCode"]?.ToString() ?? "",
                    RoomType = d["RoomType"]?.ToString() ?? "",
                    RateDate = d["RateDate"] != DBNull.Value ? Convert.ToDateTime(d["RateDate"]) : null,
                    RateCodeID = d["RateCodeID"] != DBNull.Value ? Convert.ToInt32(d["RateCodeID"]) : 0,
                    RoomTypeID = d["RoomTypeID"] != DBNull.Value ? Convert.ToInt32(d["RoomTypeID"]) : 0,

                    A1 = d["A1"] != DBNull.Value ? Convert.ToDecimal(d["A1"]) : 0,
                    A1AfterTax = d["A1AfterTax"] != DBNull.Value ? Convert.ToDecimal(d["A1AfterTax"]) : 0,
                    A2 = d["A2"] != DBNull.Value ? Convert.ToDecimal(d["A2"]) : 0,
                    A2AfterTax = d["A2AfterTax"] != DBNull.Value ? Convert.ToDecimal(d["A2AfterTax"]) : 0,
                    A3 = d["A3"] != DBNull.Value ? Convert.ToDecimal(d["A3"]) : 0,
                    A3AfterTax = d["A3AfterTax"] != DBNull.Value ? Convert.ToDecimal(d["A3AfterTax"]) : 0,
                    A4 = d["A4"] != DBNull.Value ? Convert.ToDecimal(d["A4"]) : 0,
                    A4AfterTax = d["A4AfterTax"] != DBNull.Value ? Convert.ToDecimal(d["A4AfterTax"]) : 0,
                    A5 = d["A5"] != DBNull.Value ? Convert.ToDecimal(d["A5"]) : 0,
                    A5AfterTax = d["A5AfterTax"] != DBNull.Value ? Convert.ToDecimal(d["A5AfterTax"]) : 0,
                    A6 = d["A6"] != DBNull.Value ? Convert.ToDecimal(d["A6"]) : 0,
                    A6AfterTax = d["A6AfterTax"] != DBNull.Value ? Convert.ToDecimal(d["A6AfterTax"]) : 0,

                    C1 = d["C1"] != DBNull.Value ? Convert.ToDecimal(d["C1"]) : 0,
                    C1AfterTax = d["C1AfterTax"] != DBNull.Value ? Convert.ToDecimal(d["C1AfterTax"]) : 0,
                    C2 = d["C2"] != DBNull.Value ? Convert.ToDecimal(d["C2"]) : 0,
                    C2AfterTax = d["C2AfterTax"] != DBNull.Value ? Convert.ToDecimal(d["C2AfterTax"]) : 0,
                    C3 = d["C3"] != DBNull.Value ? Convert.ToDecimal(d["C3"]) : 0,
                    C3AfterTax = d["C3AfterTax"] != DBNull.Value ? Convert.ToDecimal(d["C3AfterTax"]) : 0,

                    AdultExtra = d.Table.Columns.Contains("AdultExtra") && d["AdultExtra"] != DBNull.Value ? Convert.ToDecimal(d["AdultExtra"]) : 0,
                    AdultExtraTax = d.Table.Columns.Contains("AdultExtraTax") && d["AdultExtraTax"] != DBNull.Value ? Convert.ToDecimal(d["AdultExtraTax"]) : 0,

                    TransactionCode = d["TransactionCode"]?.ToString() ?? "",
                    CurrencyID = d["CurrencyID"]?.ToString() ?? ""
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("GetRateCodeDetailById")]
        public IActionResult GetRateCodeDetailById(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return Json(new { success = false, message = "Invalid ID." });
                }

                var m = RateCodeDetailBO.Instance.FindByPrimaryKey(id) as RateCodeDetailModel;
                if (m == null || m.ID == 0)
                {
                    return Json(new { success = false, message = "Record not found." });
                }

                var rc = RateCodeBO.Instance.FindByPrimaryKey(m.RateCodeID) as RateCodeModel;
                var rateCodeStr = rc?.RateCode ?? "";

                return Json(new { success = true, data = m, rateCode = rateCodeStr });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("SaveRateCodeDetail")]
        public IActionResult SaveRateCodeDetail([FromBody] RateCodeDetailSaveRequest? req)
        {
            try
            {
                if (req == null)
                {
                    return BadRequest(new { success = false, message = "Invalid payload." });
                }

                if (req.RateCodeId <= 0)
                {
                    return BadRequest(new { success = false, message = "Rate Code is required." });
                }

                if (req.RoomTypeId <= 0)
                {
                    return BadRequest(new { success = false, message = "Room Type is required." });
                }

                var fd = req.FromDate ?? DateTime.Today;
                var td = req.ToDate ?? req.FromDate ?? DateTime.Today;

                if (req.Id > 0)
                {
                    if (RateCodeDetailBO.Instance.FindByPrimaryKey(req.Id) is not RateCodeDetailModel existing || existing.ID == 0)
                    {
                        return BadRequest(new { success = false, message = $"Rate Code Detail ID {req.Id} not found." });
                    }

                    ApplySaveRequest(existing, req, fd, td);
                    existing.UserUpdateID = req.UserId;
                    existing.UpdateDate = DateTime.Now;
                    RateCodeDetailBO.Instance.Update(existing);
                    return Json(new { success = true, message = "Changes saved successfully.", data = new { id = existing.ID } });
                }

                var insert = new RateCodeDetailModel();
                ApplySaveRequest(insert, req, fd, td);
                insert.UserInsertID = req.UserId;
                insert.UserUpdateID = req.UserId;
                insert.CreateDate = DateTime.Now;
                insert.UpdateDate = DateTime.Now;

                var newId = RateCodeDetailBO.Instance.Insert(insert);
                return Json(new { success = true, message = "Record has been created successfully.", data = new { id = (int)newId } });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("DeleteRateCodeDetail")]
        public IActionResult DeleteRateCodeDetail(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { success = false, message = "Invalid ID." });
                }

                if (RateCodeDetailBO.Instance.FindByPrimaryKey(id) is not RateCodeDetailModel existing || existing.ID == 0)
                {
                    return BadRequest(new { success = false, message = "Record not found." });
                }

                RateCodeDetailBO.Instance.Delete(id);
                return Json(new { success = true, message = "Record removed successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        private static void ApplySaveRequest(RateCodeDetailModel m, RateCodeDetailSaveRequest r, DateTime fd, DateTime td)
        {
            m.RateCodeID = r.RateCodeId;
            m.RoomTypeID = r.RoomTypeId;
            m.PackageID = r.PackageId;
            m.SeasonID = r.SeasonId;

            m.RateDate = fd;
            m.FromDate = fd;
            m.ToDate = td;

            m.MinLOS = r.MinLos;
            m.MaxLOS = r.MaxLos;
            m.MinNoOfRoom = r.MinNoOfRoom;
            m.MaxNoOfRoom = r.MaxNoOfRoom;

            m.TransactionCode = r.TransactionCode?.Trim() ?? string.Empty;
            m.CurrencyID = r.CurrencyId?.Trim() ?? string.Empty;

            m.PrintRate = r.PrintRate;
            m.Discount = r.Discount;

            m.A1 = r.A1;
            m.A2 = r.A2;
            m.A3 = r.A3;
            m.A4 = r.A4;
            m.A5 = r.A5;
            m.A6 = r.A6;
            m.A7 = r.A7;
            m.A8 = r.A8;
            m.A9 = r.A9;
            m.A10 = r.A10;
            m.A11 = r.A11;
            m.A12 = r.A12;
            m.A13 = r.A13;
            m.A14 = r.A14;
            m.A15 = r.A15;

            m.A1AfterTax = r.A1AfterTax;
            m.A2AfterTax = r.A2AfterTax;
            m.A3AfterTax = r.A3AfterTax;
            m.A4AfterTax = r.A4AfterTax;
            m.A5AfterTax = r.A5AfterTax;
            m.A6AfterTax = r.A6AfterTax;
            m.A7AfterTax = r.A7AfterTax;
            m.A8AfterTax = r.A8AfterTax;
            m.A9AfterTax = r.A9AfterTax;
            m.A10AfterTax = r.A10AfterTax;
            m.A11AfterTax = r.A11AfterTax;
            m.A12AfterTax = r.A12AfterTax;
            m.A13AfterTax = r.A13AfterTax;
            m.A14AfterTax = r.A14AfterTax;
            m.A15AfterTax = r.A15AfterTax;

            m.C1 = r.C1;
            m.C2 = r.C2;
            m.C3 = r.C3;
            m.C1AfterTax = r.C1AfterTax;
            m.C2AfterTax = r.C2AfterTax;
            m.C3AfterTax = r.C3AfterTax;

            m.AdultExtra = r.AdultExtra;
            m.AdultExtraTax = r.AdultExtraTax;
        }
    }
}

// [HttpGet("GetAllRateCodeDetail")]
// public async Task<IActionResult> GetAllRateCodeDetail(
//     string? rateCode,
//     string? rateCategory,
//     int? typeOfDate,
//     DateTime? fromDate,
//     DateTime? toDate)
// {
//     var parameters = new Dictionary<string, object?>
//     {
//         { "@strRateCode", rateCode },
//         { "@strRateCategory", rateCategory },
//         { "@TypeOfDate", typeOfDate ?? 0 },
//         { "@FromDate", fromDate },
//         { "@ToDate",  }
//     };
//     var data = await _detail.RateCodeTypeData(parameters);
//     var result = data.AsEnumerable()
//     .Select(row => data.Columns.Cast<DataColumn>()
//     .ToDictionary(
//         col => col.ColumnName,
//         col2 => row[col2] == DBNull.Value ? null : row[col2]
//     )).ToList();
//     return Json(result);
// }

// [HttpGet("RateCodeGroupDataByID")]
// public async Task<IActionResult> RateCodeGroupDataByID(int? RateCodeID)
// {
//     var parameters = new Dictionary<string, object?>
//     {
//         { "@RateCodeID", RateCodeID },
//     };
//     var data = await _detail.RateCodeGroupDataByID(parameters);
//     var result = data.AsEnumerable()
//     .Select(row => data.Columns.Cast<DataColumn>()
//     .ToDictionary(
//         col => col.ColumnName,
//         col2 => row[col2] == DBNull.Value ? null : row[col2]
//     )).ToList();
//     return Json(result);
// }

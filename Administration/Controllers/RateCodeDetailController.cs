using System.Collections;
using System.Data;
using System.Linq;
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
            ViewBag.RateCodeList = listRateCode;
            ViewBag.RateCateList = listRateCate;
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

using Administration.DTO;
using Administration.Services.Interfaces;
using BaseBusiness.BO;
using BaseBusiness.Model;
using BaseBusiness.util;
using Microsoft.AspNetCore.Mvc;
using static Administration.DTO.PackageDetailDTO;

namespace Administration.Controllers
{
    [Route("/Administration/RateCode")]
    public class PackageDetailController : Controller
    {
        private readonly IPackageDetailService _packagedetail;
        public PackageDetailController(IPackageDetailService packagedetail)
        {
            _packagedetail = packagedetail;
        }
        [HttpGet("PackageDetail")] // Truyeenf DataGrid , script api
        public IActionResult PackageDetail()
        {
            return View("~/Views/Administration/RateCode/PackageDetail.cshtml");
            // Truyền đường dẫn chuẩn vào để tìm đúng
        }

        [HttpGet("GetPackageDetailByIDPackage")]
        public IActionResult GetPackageDetailByIDPackage(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid PackageID"
                    });
                }

                var dtoList = _packagedetail.GetPackageDetailsByPackageID(id);

                if (dtoList == null || dtoList.Count == 0)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "No package detail found",
                        data = new List<PackageDetailDTO>()
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Success",
                    data = dtoList
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message,
                    innerException = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        [HttpPost("SaveDetailPackage")]
        public IActionResult SaveDetailPackage([FromBody] PackageDetailDTO dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Payload is null"
                    });
                }

                var errors = new List<object>();
                //Basic Validation
                if (dto.CalculationRuleID < 0 || dto.CalculationRuleID == null)
                {
                    errors.Add(new { field = "calculationRuleID", message = "Calculation is required." });
                }
                if (dto.RhythmPostingID < 0)
                {
                    errors.Add(new { field = "rhythmPostingID", message = "RhythmPosting is required." });
                }
                if (dto.SeasonID < 0)
                {
                    errors.Add(new { field = "seasonID", message = "Season is required." });
                }
                if (dto.Price < 0)
                    errors.Add(new { field = "price", message = "Price must be greater than or equal to 0." });
                if (dto.PriceAfterTax < 0)
                    errors.Add(new { field = "priceAfterTax", message = "Price After Tax must be greater than or equal to 0." });

                if (string.IsNullOrWhiteSpace(dto.TransCode))
                    errors.Add(new { field = "transCode", message = "Transaction Code is required." });

                if (string.IsNullOrWhiteSpace(dto.CurrencyID))
                    errors.Add(new { field = "currencyID", message = "Currency Code is required." });
                //Business Validation

                // User
                if (dto.UserInsertID <= 0)
                {
                    return NotFound(new { success = false, message = "UserID not found." });
                }
                else
                {
                    var user = UsersBO.Instance.FindByPrimaryKey(dto.UserInsertID);
                    if (user == null)
                    {
                        return NotFound(new { success = false, message = "Invalid User Insert." });
                    }
                }

                List<BusinessDateModel> businessDates = PropertyUtils.ConvertToList<BusinessDateModel>(BusinessDateBO.Instance.FindAll());
                if (businessDates == null || businessDates.Count == 0)
                    return NotFound(new { success = false, message = "Business date not available. Contact system administrator." });

                // PackageID Validation
                if (!dto.PackageID.HasValue || dto.PackageID.Value <= 0)
                {
                    errors.Add(new { field = "packageID", message = "Package is required." });
                }
                else
                {
                    var package = PackageBO.Instance.FindByPrimaryKey(dto.PackageID.Value);
                    if (package == null)
                    {
                        errors.Add(new { field = "packageID", message = "Invalid Package." });
                    }
                }

                //RhythmPosting Validation
                var RhythmPostingList = RhythmPostingBO.Instance.FindByPrimaryKey(dto.RhythmPostingID);
                if (RhythmPostingList == null)
                    errors.Add(new { field = "rhythmPostingID", message = "Business date not available. Contact system administrator." });

                // Transaction Code
                if (!string.IsNullOrWhiteSpace(dto.TransCode))
                {
                    var transList = TransactionsBO.Instance.FindByAttribute("Code", dto.TransCode);
                    if (transList == null || transList.Count == 0)
                        errors.Add(new { field = "transCode", message = "Invalid Transaction Code." });
                }
                if (!string.IsNullOrWhiteSpace(dto.CurrencyID))
                {
                    var currencyList = CurrencyBO.Instance.FindByAttribute("ID", dto.CurrencyID);
                    if (currencyList == null || currencyList.Count == 0)
                        errors.Add(new { field = "currencyID", message = "Invalid Transaction Code." });
                }
                // ===== RETURN IF ERROR =====
                if (errors.Count != 0)
                {
                    return Json(new { success = false, message = "Validation failed.", errors });
                }
                // ===== CALL SERVICE =====
                int id = _packagedetail.Save(dto);


                return Ok(new
                {
                    success = true,
                    message = "Success",
                    data = new { id }
                });

            }
            catch (KeyNotFoundException ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message,
                    innerException = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        [HttpPost("PackageDetailDelete")]
        public IActionResult PackageDetailDelete([FromBody] DeleteRequest delete)
        {
            try
            {
                int id = delete.ID;
                if (PackageDetailBO.Instance.FindByPrimaryKey(id) is not PackageDetailModel existing || existing.ID == 0)
                {
                    return Ok(new { success = false, message = $"Package Detail ID {id} not found." });
                }
                PackageDetailBO.Instance.Delete(id);
                return Ok(new { success = true, message = $"Successfully deleted Package Detail ID {id}." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}

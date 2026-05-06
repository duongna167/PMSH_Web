using System.Collections;
using System.Data;
using System.Linq;
using Administration.Services.Interfaces;
using BaseBusiness.bc;
using BaseBusiness.BO;
using BaseBusiness.Model;
using BaseBusiness.util;
using Microsoft.AspNetCore.Mvc;

namespace Administration.Controllers
{
    [Route("/Administration/RateCode")]
    public class RateCodeUserRightController : Controller
    {
        private readonly IRateCodeUserRightService _svRateCodeUserRight;
        public RateCodeUserRightController(IRateCodeUserRightService RateCodeUserRight)
        {
            _svRateCodeUserRight = RateCodeUserRight;
        }
        [HttpGet("RateCodeUserRight")] // Truyeenf DataGrid , script api
        public IActionResult RateCodeUserRight()
        {
            List<UsersModel> listUser = PropertyUtils.ConvertToList<UsersModel>(UsersBO.Instance.FindAll());
            List<RateCodeModel> listRateCode = PropertyUtils.ConvertToList<RateCodeModel>(RateCodeBO.Instance.FindAll());
            ViewBag.RateCodeList = listRateCode;
            ViewBag.UserList = listUser;


            return PartialView("~/Views/Administration/RateCode/RateCodeUserRight.cshtml");
            // Truyền đường dẫn chuẩn vào để tìm đúng
        }



        [HttpGet("GetAllRateCodeUserRight")]
        public async Task<IActionResult> RateCodeUserRightGetAll(string? userName, string? rateCode)
        {
            try
            {
                // Xử lý null
                userName = userName?.Trim() ?? "";
                rateCode = rateCode?.Trim() ?? "";

                // Escape ký tự ' (bắt buộc để tránh SQL injection)
                userName = userName.Replace("'", "''");
                rateCode = rateCode.Replace("'", "''");
                DataTable dataTable = _svRateCodeUserRight.RateCodeUserRightTypeData(userName, rateCode);


                var result = (from d in dataTable.AsEnumerable()
                              select new
                              {
                                  ID = d["ID"]?.ToString() ?? "",
                                  UserName = d["UserName"]?.ToString() ?? "",
                                  UserID = d["UserID"]?.ToString() ?? "",
                                  RateCodeID = d["RateCodeID"] != DBNull.Value ? Convert.ToInt32(d["RateCodeID"]) : 0,
                                  RateCode = d["RateCode"]?.ToString() ?? "",
                                  CreatedDate = d["CreatedDate"] != DBNull.Value ? Convert.ToDateTime(d["CreatedDate"]) : (DateTime?)null,
                                  UpdatedDate = d["UpdatedDate"] != DBNull.Value ? Convert.ToDateTime(d["UpdatedDate"]) : (DateTime?)null,
                                  CreatedBy = d["CreatedBy"]?.ToString() ?? "",
                                  UpdatedBy = d["UpdatedBy"]?.ToString() ?? "",
                              }).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("RateCodeUserRightSave")]
        public async Task<IActionResult> RateCodeUserRightSave([FromBody] RateCodeUserRightSaveDto dto)
        {
            try
            {
                var errors = new List<object>();

                var userLogin = dto.UserLogin?.Trim().Trim('"') ?? string.Empty;

                // ===== VALIDATION =====
                if (dto.UserID <= 0)
                    errors.Add(new { field = "user", message = "User ID is required." });

                if (string.IsNullOrWhiteSpace(dto.UserName))
                    errors.Add(new { field = "user", message = "User Name is required." });
                else if (dto.UserName.Length > 50)
                    errors.Add(new { field = "user", message = "User Name max length is 50." });

                if (dto.RateCodeID <= 0)
                    errors.Add(new { field = "rateCode", message = "Rate Code ID is required." });

                if (string.IsNullOrWhiteSpace(dto.RateCode))
                    errors.Add(new { field = "rateCode", message = "Rate Code Name is required." });
                else if (dto.RateCode.Length > 20)
                    errors.Add(new { field = "rateCode", message = "Rate Code Name max length is 20." });

                if (string.IsNullOrWhiteSpace(userLogin))
                    return NotFound(new { success = false, message = "Login user is required." });

                // ===== CHECK ID =====
                int parsedId = 0;
                bool isUpdate = false;

                if (dto.Id > 0)
                {
                    parsedId = dto.Id;
                    isUpdate = true;
                }
                
                // Use system date/time at the moment of the operation (not BusinessDate)
                var systemNow = TextUtils.GetSystemDate();

                // ===== DUPLICATE CHECK (User + RateCode) =====
                var allPermissions =
                    PropertyUtils.ConvertToList<UserRateCodePermissionModel>(
                        UserRateCodePermissionBO.Instance.FindAll()
                    ) ?? [];

                bool isDuplicate = allPermissions.Any(x =>
                    x.UserID == dto.UserID &&
                    x.RateCodeID == dto.RateCodeID &&
                    x.ID != parsedId
                );

                if (isDuplicate)
                    errors.Add(new { field = "user", message = "This user already has permission for this RateCode." });

                // ===== RETURN ERRORS =====
                if (errors.Count != 0)
                {
                    return Json(new { success = false, message = "Validation failed.", errors });
                }

                // ===== PREPARE MODEL =====
                var userName = dto.UserName!;
                var rateCode = dto.RateCode!;

                UserRateCodePermissionModel model = new()
                {
                    UserID = dto.UserID,
                    UserName = userName.Trim(),
                    RateCodeID = dto.RateCodeID,
                    RateCode = rateCode.Trim()
                };
                // ===== UPDATE =====
                if (isUpdate)
                {
                    var existing =
                        UserRateCodePermissionBO.Instance.FindByPrimaryKey(parsedId)
                        as UserRateCodePermissionModel;

                    if (existing == null || existing.ID == 0)
                    {
                        return NotFound(new
                        {
                            success = false,
                            message = $"Permission not found (ID = {parsedId})"
                        });
                    }

                    model.ID = parsedId;
                    model.CreatedBy = existing.CreatedBy;
                    model.CreatedDate = existing.CreatedDate;
                    model.UpdatedBy = userLogin;
                    model.UpdatedDate = systemNow;

                    UserRateCodePermissionBO.Instance.Update(model);
                    return Json(new
                    {
                        success = true,
                        message = $"Changes saved successfully ID: {model.ID}.",
                        data = new { id = model.ID }
                    });

                }
                // ===== INSERT =====
                else
                {
                    model.CreatedBy = userLogin;
                    model.CreatedDate = systemNow;
                    model.UpdatedBy = userLogin;
                    model.UpdatedDate = model.CreatedDate;

                    UserRateCodePermissionBO.Instance.Insert(model);
                    return Json(new { success = true, message = "Record has been created successfully.", data = new { id = model.ID } });

                }
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost("RateCodeUserRightDelete")]
        public IActionResult RateCodeUserRightDelete(int id)
        {
            try
            {
                if (UserRateCodePermissionBO.Instance.FindByPrimaryKey(id) is not UserRateCodePermissionModel existing || existing.ID == 0)
                {
                    return NotFound(new { success = false, message = $"Rate Code User Right ID {id} not found." });
                }

                UserRateCodePermissionBO.Instance.Delete(id);
                return Json(new { success = true, message = $"Record was removed successfully ID: {id}." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        public class RateCodeUserRightSaveDto
        {
            public int Id { get; set; }
            public int UserID { get; set; }
            public string? UserName { get; set; }
            public int RateCodeID { get; set; }
            public string? RateCode { get; set; }
            public string? UserLogin { get; set; }
        }

    }
}


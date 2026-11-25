using System.Data;
using BaseBusiness.Auth;
using Microsoft.AspNetCore.Mvc;
using NightAudit.Services.Implements;
using NightAudit.Services.Interfaces;
using static BaseBusiness.util.ValidationUtils;

namespace NightAudit.Controllers
{
    /// <summary>Map Transaction Code với mã tài khoản kế toán (frmMapTransaction).</summary>
    [RequireFunction("tool_MapTransactionCode")]
    public class MapTransactionController : Controller
    {
        private readonly IMapTransactionService _service;

        public MapTransactionController(IMapTransactionService service)
        {
            _service = service;
        }

        [HttpGet]
        public IActionResult MapTransaction()
        {
            return PartialView("~/Views/NightAudit/MapTransaction.cshtml");
        }

        [HttpGet]
        public IActionResult MapTransactionData()
        {
            try
            {
                DataTable t = _service.GetAll();
                var data = t.AsEnumerable()
                    .Select(r => t.Columns.Cast<DataColumn>().ToDictionary(c => c.ColumnName, c => r[c] == DBNull.Value ? null : r[c]))
                    .ToList();
                return Json(new { success = true, data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>Kiểm tra giá trị: tên cột hợp lệ, độ dài, PerDT là số nguyên. Trả danh sách lỗi theo tên cột.</summary>
        private static List<ValidationError> Validate(Dictionary<string, string?> values, bool requireTransactionCode)
        {
            var errors = new List<ValidationError>();
            foreach (var (key, value) in values)
            {
                if (!MapTransactionService.Columns.TryGetValue(key, out int max))
                {
                    errors.Add(new ValidationError { Field = key, Message = "Invalid column." });
                    continue;
                }
                if (key.Equals("PerDT", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrWhiteSpace(value) && !int.TryParse(value, out _))
                        errors.Add(new ValidationError { Field = key, Message = "PerDT must be a whole number." });
                }
                else if (max > 0 && (value?.Length ?? 0) > max)
                {
                    errors.Add(new ValidationError { Field = key, Message = $"{key} must be at most {max} characters." });
                }
            }

            if (requireTransactionCode &&
                !values.Any(v => v.Key.Equals("TransactionCode", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(v.Value)))
                errors.Add(new ValidationError { Field = "TransactionCode", Message = "Transaction Code must not be blank." });

            return errors;
        }

        [HttpPost]
        public IActionResult MapTransactionAdd([FromBody] Dictionary<string, string?> model)
        {
            try
            {
                if (model == null) return Json(new { success = false, message = "No data received." });

                var errors = Validate(model, true);
                if (errors.Count > 0) return Json(new { success = false, errors });

                int id = _service.Add(model);
                return Json(new { success = true, message = "Add successfully", id });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>Lưu các dòng đã sửa: mỗi phần tử gồm ID và các cột đã đổi.</summary>
        [HttpPost]
        public IActionResult MapTransactionSave([FromBody] List<Dictionary<string, string?>> rows)
        {
            try
            {
                if (rows == null || rows.Count == 0) return Json(new { success = false, message = "There is no change to save." });

                // Kiểm tra toàn bộ trước khi ghi để không lưu dở dang
                foreach (var row in rows)
                {
                    if (!row.TryGetValue("ID", out string? idText) || !int.TryParse(idText, out int id) || id <= 0)
                        return Json(new { success = false, message = "Invalid row." });

                    var values = row.Where(v => !v.Key.Equals("ID", StringComparison.OrdinalIgnoreCase)).ToDictionary(v => v.Key, v => v.Value);
                    var errors = Validate(values, false);
                    if (errors.Count > 0)
                        return Json(new { success = false, message = $"Row {id}: " + errors[0].Message });
                }

                foreach (var row in rows)
                {
                    int id = int.Parse(row["ID"]!);
                    _service.Update(id, row.Where(v => !v.Key.Equals("ID", StringComparison.OrdinalIgnoreCase)).ToDictionary(v => v.Key, v => v.Value));
                }

                return Json(new { success = true, message = "Update Completed!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult MapTransactionDelete(int id)
        {
            try
            {
                if (id <= 0) return Json(new { success = false, message = "Please select a row." });
                return _service.Delete(id)
                    ? Json(new { success = true, message = "Delete successfully" })
                    : Json(new { success = false, message = "The row does not exist." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}

using System.Data;
using BaseBusiness.Auth;
using EInvoice.Dto;
using EInvoice.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EInvoice.Controllers
{
    /// <summary>Màn Property E-Invoice: nguồn dữ liệu (kết nối DB động) và cấu hình nhà cung cấp theo từng Property.</summary>
    [RequireFunction("EInvoice_Config")]
    public class EInvoicePropertyController : Controller
    {
        private readonly IEInvoicePropertyService _service;

        public EInvoicePropertyController(IEInvoicePropertyService service)
        {
            _service = service;
        }

        [HttpGet]
        public IActionResult PropertyEinvoice()
        {
            return View("~/Views/EInvoice/PropertyEinvoice.cshtml");
        }

        // ---------------------------------------------------------------------------------------------
        //  Dữ liệu
        // ---------------------------------------------------------------------------------------------

        [HttpGet]
        public IActionResult PropertyData(int id = 0)
        {
            return Rows(() => _service.SearchProperties(id));
        }

        /// <summary>Field cấu hình của một Provider (kèm giá trị đã lưu của Property) và các option của chúng.</summary>
        [HttpGet]
        public IActionResult ProviderConfigData(int propertyId, int providerId)
        {
            try
            {
                return Json(new
                {
                    fields = ToRows(_service.GetProviderFields(propertyId, providerId)),
                    options = ToRows(_service.GetProviderOptions(providerId))
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // ---------------------------------------------------------------------------------------------
        //  Thao tác
        // ---------------------------------------------------------------------------------------------

        [HttpPost]
        public IActionResult PropertySave([FromBody] PropertySaveRequest request)
        {
            return Run(request, user => _service.SaveProperty(request, user));
        }

        [HttpPost]
        public IActionResult PropertyDelete(int id)
        {
            return Run(id, _ => _service.DeleteProperty(id));
        }

        [HttpPost]
        public IActionResult PropertyTest([FromBody] PropertyTestRequest request)
        {
            return Run(request, _ => _service.TestConnection(request));
        }

        /// <summary>Mã hóa mật khẩu/secret còn lưu dạng chữ thường (sau khi chuyển dữ liệu từ hệ thống cũ).</summary>
        [HttpPost]
        public IActionResult PropertyEncryptSecrets()
        {
            return Run(new object(), user => _service.EncryptLegacySecrets(user));
        }

        // ---------------------------------------------------------------------------------------------
        //  Hàm dùng chung
        // ---------------------------------------------------------------------------------------------

        private static List<Dictionary<string, object?>> ToRows(DataTable table)
        {
            return table.AsEnumerable()
                .Select(row => table.Columns.Cast<DataColumn>()
                    .ToDictionary(col => col.ColumnName, col => row[col] == DBNull.Value ? null : row[col]))
                .ToList();
        }

        private IActionResult Rows(Func<DataTable> load)
        {
            try
            {
                var data = ToRows(load());
                return Json(new { totalCount = data.Count, data });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        private IActionResult Run(object? request, Func<string, OpResult> action)
        {
            if (request == null)
                return BadRequest(new { success = false, message = "Request data is required." });

            string user = HttpContext.Session.GetString("LoginName") ?? "";
            if (string.IsNullOrWhiteSpace(user))
                return Unauthorized(new { success = false, message = "Session expired. Please login again." });

            try
            {
                OpResult r = action(user);
                return Json(new { success = r.Success, message = r.Message, data = r.Data, errors = r.Errors });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}

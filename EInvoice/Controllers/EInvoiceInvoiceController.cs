using System.Data;
using BaseBusiness.Auth;
using EInvoice.Dto;
using EInvoice.Runtime;
using EInvoice.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EInvoice.Controllers
{
    /// <summary>
    /// Màn thao tác hóa đơn PMS: tìm hóa đơn, Upload/Publish/Issue, xem nhật ký, xem trước request.
    /// Quyền theo từng action: EInvoice_View (xem), EInvoice_Issue (gửi nhà cung cấp), EInvoice_Config (xem trước / chi tiết log).
    /// </summary>
    public class EInvoiceInvoiceController : Controller
    {
        private readonly IEInvoiceInvoiceService _service;
        private readonly IEInvoiceRuntimeService _runtime;

        public EInvoiceInvoiceController(IEInvoiceInvoiceService service, IEInvoiceRuntimeService runtime)
        {
            _service = service;
            _runtime = runtime;
        }

        // ---------------------------------------------------------------------------------------------
        //  Màn hình
        // ---------------------------------------------------------------------------------------------

        /// <summary>Trang đầy đủ (không phải popup) - mở từ menu Cashiering > E invoice.</summary>
        [HttpGet, RequireFunction("EInvoice_View")]
        public IActionResult InvoiceManagement()
        {
            return View("~/Views/EInvoice/InvoiceManagement.cshtml");
        }

        [HttpGet, RequireFunction("EInvoice_Config")]
        public IActionResult InvoicePreview()
        {
            return PartialView("~/Views/EInvoice/InvoicePreview.cshtml");
        }

        // ---------------------------------------------------------------------------------------------
        //  Dữ liệu
        // ---------------------------------------------------------------------------------------------

        [HttpGet, RequireFunction("EInvoice_View")]
        public IActionResult InvoiceData([FromQuery] InvoiceSearchRequest request)
        {
            return Rows(() => _service.SearchInvoices(request));
        }

        /// <summary>Property được phép thao tác theo người dùng đăng nhập.</summary>
        [HttpGet, RequireFunction("EInvoice_View")]
        public IActionResult AllowedProperties()
        {
            int userId = HttpContext.Session.GetInt32("UserID") ?? 0;
            int groupId = HttpContext.Session.GetInt32("UserGroupID") ?? 0;
            return Rows(() => _service.GetAllowedProperties(userId, groupId));
        }

        /// <summary>Nghiệp vụ Provider của Property hỗ trợ (để hiện/ẩn các mục menu E Invoice).</summary>
        [HttpGet, RequireFunction("EInvoice_View")]
        public IActionResult ProviderActions(int propertyId)
        {
            return Rows(() => _service.GetProviderActions(propertyId));
        }

        [HttpGet, RequireFunction("EInvoice_View")]
        public IActionResult LogData([FromQuery] LogSearchRequest request)
        {
            return Rows(() => _service.SearchLogs(request));
        }

        /// <summary>Request/response nguyên văn của một lần gọi nhà cung cấp.</summary>
        [HttpGet, RequireFunction("EInvoice_Config")]
        public IActionResult LogDetail(long id)
        {
            return Rows(() => _service.GetLogDetail(id));
        }

        // ---------------------------------------------------------------------------------------------
        //  Thao tác
        // ---------------------------------------------------------------------------------------------

        [HttpPost, RequireFunction("EInvoice_Issue")]
        public async Task<IActionResult> ExecuteBatch([FromBody] InvoiceBatchRequest request, CancellationToken cancellationToken)
        {
            return await Run(request, user => _service.ExecuteBatchAsync(request, user, cancellationToken));
        }

        /// <summary>Dựng request nhưng KHÔNG gửi đi; dữ liệu nhạy cảm đã được che.</summary>
        [HttpPost, RequireFunction("EInvoice_Config")]
        public async Task<IActionResult> PreviewRequest([FromBody] PreviewRequest request)
        {
            return await Run(request, user => Task.FromResult(_runtime.PreviewRequest(request, user)));
        }

        // ---------------------------------------------------------------------------------------------
        //  Hàm dùng chung
        // ---------------------------------------------------------------------------------------------

        private IActionResult Rows(Func<DataTable> load)
        {
            try
            {
                DataTable table = load();
                var data = table.AsEnumerable()
                    .Select(row => table.Columns.Cast<DataColumn>()
                        .ToDictionary(col => col.ColumnName, col => row[col] == DBNull.Value ? null : row[col]))
                    .ToList();
                return Json(new { totalCount = data.Count, data });
            }
            catch (ProviderBuildException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        private async Task<IActionResult> Run(object? request, Func<RuntimeUser, Task<OpResult>> action)
        {
            if (request == null)
                return BadRequest(new { success = false, message = "Request data is required." });

            string loginName = HttpContext.Session.GetString("LoginName") ?? "";
            if (string.IsNullOrWhiteSpace(loginName))
                return Unauthorized(new { success = false, message = "Session expired. Please login again." });

            var user = new RuntimeUser(HttpContext.Session.GetInt32("UserID"), HttpContext.Session.GetString("UserName"), loginName);

            try
            {
                OpResult r = await action(user);
                var body = new { success = r.Success, message = r.Message, data = r.Data, errors = r.Errors };
                // Lỗi nghiệp vụ trả 400 để giao diện hiện lỗi nhưng vẫn đọc được kết quả từng hóa đơn
                return r.Success || r.Data != null ? Json(body) : BadRequest(body);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(StatusCodes.Status499ClientClosedRequest, new { success = false, message = "Request cancelled." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}

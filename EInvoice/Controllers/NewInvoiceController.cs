using System.Data;
using BaseBusiness.Auth;
using EInvoice.Runtime;
using EInvoice.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace EInvoice.Controllers
{
    /// <summary>
    /// New Invoice (New Summary Invoice) trên màn Electronic Invoice PMS: chọn folio chưa xuất hóa đơn, tạo hóa đơn, xem Billing.
    /// Mọi thao tác chạy trên DB PMS của Property đang chọn (propertyId).
    /// </summary>
    [RequireFunction("EInvoice_Issue")]
    public class NewInvoiceController : Controller
    {
        private readonly INewInvoiceService _service;
        private readonly ILogger<NewInvoiceController> _logger;

        public NewInvoiceController(INewInvoiceService service, ILogger<NewInvoiceController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>Kiểm tra Property và trả về tên + ngày nghiệp vụ; giao diện gọi trước khi mở popup để báo lỗi rõ ràng.</summary>
        [HttpGet]
        public IActionResult Context(int propertyId)
        {
            try
            {
                var (name, businessDate) = _service.GetContext(propertyId);
                return Json(new { success = true, propertyName = name, businessDate = businessDate.ToString("yyyy-MM-dd") });
            }
            catch (Exception ex)
            {
                return Fail(ex, "Context");
            }
        }

        /// <summary>Popup chọn folio chưa xuất hóa đơn (kèm popup chi tiết hóa đơn và Billing).</summary>
        [HttpGet]
        public IActionResult NewInvoice(int propertyId)
        {
            var (name, businessDate) = _service.GetContext(propertyId);

            // Ngày mặc định theo ngày nghiệp vụ của Property: tìm folio 30 ngày gần nhất, ngày hóa đơn = ngày nghiệp vụ
            ViewBag.PropertyId = propertyId;
            ViewBag.PropertyName = name;
            ViewBag.FromDate = businessDate.Date.AddDays(-30).ToString("yyyy-MM-dd");
            ViewBag.ToDate = businessDate.Date.ToString("yyyy-MM-dd");
            return PartialView("~/Views/EInvoice/NewInvoice.cshtml");
        }

        [HttpGet]
        public IActionResult SearchFolios(int propertyId, string? confirmationNo, string? folioNo, string? roomNo, DateTime fromDate, DateTime toDate)
        {
            try
            {
                return Json(ToRows(_service.SearchFolios(propertyId, confirmationNo ?? "", folioNo ?? "", roomNo ?? "", fromDate, toDate)));
            }
            catch (Exception ex)
            {
                return Fail(ex, "SearchFolios");
            }
        }

        [HttpGet]
        public IActionResult FolioDetails(int propertyId, int folioId, DateTime fromDate, DateTime toDate)
        {
            try
            {
                return Json(ToRows(_service.GetFolioDetails(propertyId, folioId, fromDate, toDate)));
            }
            catch (Exception ex)
            {
                return Fail(ex, "FolioDetails");
            }
        }

        [HttpGet]
        public IActionResult FolioTransactions(int propertyId, int folioId)
        {
            try
            {
                return Json(ToRows(_service.GetFolioTransactions(propertyId, folioId), isoDates: true));
            }
            catch (Exception ex)
            {
                return Fail(ex, "FolioTransactions");
            }
        }

        public class CreateInvoiceBody
        {
            public int PropertyId { get; set; }
            public List<int> FolioIds { get; set; } = new();
            public string? FromDate { get; set; }
            public string? ToDate { get; set; }
            public string? VATDate { get; set; }
            public string? FormNo { get; set; }
            public string? Serial { get; set; }
            public int StartInvoiceNo { get; set; }
            public decimal? ExchangeRate { get; set; }
            public string? Currency { get; set; }
            public string? PaymentType { get; set; }
            public string? Comment { get; set; }
            public int GroupType { get; set; }
            public bool IsSameProfile { get; set; }
            public bool IgnoreDuplicateInvoiceNo { get; set; }
        }

        [HttpPost]
        public IActionResult CreateInvoice([FromBody] CreateInvoiceBody body)
        {
            try
            {
                if (body == null) return BadRequest(new { success = false, message = "Request is invalid" });
                if (body.PropertyId <= 0) return BadRequest(new { success = false, message = "Please select property!" });
                if (body.StartInvoiceNo < 1) return BadRequest(new { success = false, message = "Invoice no. cannot be blank" });
                if (string.IsNullOrWhiteSpace(body.Serial)) return BadRequest(new { success = false, message = "Invoice serial cannot be blank" });
                if (string.IsNullOrWhiteSpace(body.FormNo)) return BadRequest(new { success = false, message = "Form no. cannot be blank" });

                string loginName = HttpContext.Session.GetString("LoginName") ?? "";

                var request = new NewInvoiceRequest
                {
                    FolioIds = body.FolioIds ?? new List<int>(),
                    FromDate = DateTime.TryParse(body.FromDate, out var fd) ? fd : DateTime.Today,
                    ToDate = DateTime.TryParse(body.ToDate, out var td) ? td : DateTime.Today,
                    VATDate = DateTime.TryParse(body.VATDate, out var vd) ? vd : DateTime.Today,
                    FormNo = body.FormNo.Trim(),
                    SerialNo = body.Serial.Trim(),
                    StartInvoiceNo = body.StartInvoiceNo,
                    ExchangeRate = body.ExchangeRate ?? 0,
                    CurrencyID = string.IsNullOrWhiteSpace(body.Currency) ? "VND" : body.Currency.Trim(),
                    PaymentMethod = body.PaymentType?.Trim() ?? "",
                    Note = body.Comment?.Trim() ?? "",
                    GroupType = body.GroupType,
                    IsSameProfile = body.IsSameProfile,
                    IgnoreDuplicateInvoiceNo = body.IgnoreDuplicateInvoiceNo
                };

                var (createdIds, errors) = _service.Create(body.PropertyId, request, loginName);
                bool hasErrors = errors.Count > 0;

                return Json(new
                {
                    success = !hasErrors,
                    ids = createdIds,
                    count = createdIds.Count,
                    errors,
                    message = hasErrors
                        ? "The process failed. No invoice was created."
                        : $"The process completed successfully! Created {createdIds.Count} invoice(s)."
                });
            }
            catch (DuplicateInvoiceException dup)
            {
                // 409: giao diện hỏi người dùng có muốn tạo tiếp với các số trùng không, rồi gửi lại IgnoreDuplicateInvoiceNo = true
                return Conflict(new
                {
                    success = false,
                    isDuplicateConflict = true,
                    duplicateInvoiceNos = dup.DuplicateInvoiceNos,
                    message = $"The invoice no(s): [{string.Join(", ", dup.DuplicateInvoiceNos)}] already exist in system."
                });
            }
            catch (Exception ex)
            {
                return Fail(ex, "CreateInvoice");
            }
        }

        private IActionResult Fail(Exception ex, string action)
        {
            if (ex is not ProviderBuildException) _logger.LogError(ex, "NewInvoice.{Action} error", action);
            return BadRequest(new { success = false, message = ex.Message });
        }

        /// <summary>DataTable -> danh sách dictionary theo tên cột; ngày dạng dd/MM/yyyy (hoặc ISO khi cần giờ).</summary>
        private static List<Dictionary<string, object?>> ToRows(DataTable table, bool isoDates = false)
        {
            return table.AsEnumerable().Select(r =>
                table.Columns.Cast<DataColumn>().ToDictionary(
                    col => col.ColumnName,
                    col =>
                    {
                        var v = r[col];
                        if (v == DBNull.Value) return null;
                        if (v is DateTime dt) return isoDates ? dt.ToString("o") : dt.ToString("dd/MM/yyyy");
                        return v;
                    })).ToList();
        }
    }
}

using System.Data;
using BaseBusiness.Auth;
using EInvoice.Dto;
using EInvoice.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EInvoice.Controllers
{
    /// <summary>Màn Provider Management (Provider, Field, Option) và API Management (Operation, Mapping, Response Rule).</summary>
    [RequireFunction("EInvoice_Config")]
    public class EInvoiceProviderController : Controller
    {
        private readonly IEInvoiceProviderService _service;

        public EInvoiceProviderController(IEInvoiceProviderService service)
        {
            _service = service;
        }

        [HttpGet]
        public IActionResult ProviderManagement()
        {
            return View("~/Views/EInvoice/ProviderManagement.cshtml");
        }

        // ---------------------------------------------------------------------------------------------
        //  Dữ liệu cho các lưới
        // ---------------------------------------------------------------------------------------------

        [HttpGet]
        public IActionResult ProviderData()
        {
            return Rows(() => _service.SearchProviders());
        }

        [HttpGet]
        public IActionResult FieldData(int providerId)
        {
            return Rows(() => _service.SearchFields(providerId));
        }

        [HttpGet]
        public IActionResult OptionData(int fieldId)
        {
            return Rows(() => _service.SearchOptions(fieldId));
        }

        [HttpGet]
        public IActionResult OperationData(int providerId)
        {
            return Rows(() => _service.SearchOperations(providerId));
        }

        [HttpGet]
        public IActionResult MappingData(int operationId)
        {
            return Rows(() => _service.SearchMappings(operationId));
        }

        [HttpGet]
        public IActionResult RuleData(int operationId)
        {
            return Rows(() => _service.SearchRules(operationId));
        }

        // ---------------------------------------------------------------------------------------------
        //  Lưu (thêm / sửa)
        // ---------------------------------------------------------------------------------------------

        [HttpPost]
        public IActionResult ProviderSave([FromBody] ProviderSaveRequest request)
        {
            return Save(request, user => _service.SaveProvider(request, user));
        }

        [HttpPost]
        public IActionResult FieldSave([FromBody] ProviderFieldSaveRequest request)
        {
            return Save(request, user => _service.SaveField(request, user));
        }

        [HttpPost]
        public IActionResult OptionSave([FromBody] ProviderFieldOptionSaveRequest request)
        {
            return Save(request, user => _service.SaveOption(request, user));
        }

        [HttpPost]
        public IActionResult OperationSave([FromBody] OperationSaveRequest request)
        {
            return Save(request, user => _service.SaveOperation(request, user));
        }

        [HttpPost]
        public IActionResult MappingSave([FromBody] MappingSaveRequest request)
        {
            return Save(request, user => _service.SaveMapping(request, user));
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
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        private IActionResult Save(object? request, Func<string, OpResult> save)
        {
            if (request == null)
                return BadRequest(new { success = false, message = "Request data is required." });

            string user = HttpContext.Session.GetString("LoginName") ?? "";
            if (string.IsNullOrWhiteSpace(user))
                return Unauthorized(new { success = false, message = "Session expired. Please login again." });

            try
            {
                OpResult r = save(user);
                return Json(new { success = r.Success, message = r.Message, data = r.Data, errors = r.Errors });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}

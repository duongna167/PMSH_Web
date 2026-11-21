using BaseBusiness.Auth;
using Microsoft.AspNetCore.Mvc;
using NightAudit.Services.Interfaces;

namespace NightAudit.Controllers
{
    /// <summary>Export Revenue sang kế toán (frmExportRevenue_New) — menu Night Audit.</summary>
    [RequireFunction("tool_ExportData")]
    public class ExportRevenueController : Controller
    {
        private readonly IExportRevenueService _service;

        public ExportRevenueController(IExportRevenueService service)
        {
            _service = service;
        }

        [HttpGet]
        public IActionResult ExportRevenue()
        {
            return PartialView("~/Views/NightAudit/ExportRevenue.cshtml");
        }

        /// <summary>Export Revenue by market (frmExportRevenueByMarket): lọc theo Market và Source.</summary>
        [HttpGet]
        public IActionResult ExportRevenueByMarket() => AccountingView("market", "Export Revenue By Market");

        /// <summary>Export Revenue (Sun System format) (frmExportRevenue_SunSystem).</summary>
        [HttpGet]
        public IActionResult ExportRevenueSunSystem() => AccountingView("sun", "Export Revenue (Sun System format)");

        /// <summary>Export Revenue by invoice (frmExportRevenue_FLC_V1).</summary>
        [HttpGet]
        public IActionResult ExportRevenueByInvoice() => AccountingView("invoice", "Export Revenue By Invoice");

        /// <summary>Ba bản này dùng chung một giao diện, khác nhau ở chế độ (mode) và bộ lọc.</summary>
        private IActionResult AccountingView(string mode, string title)
        {
            ViewBag.Mode = mode;
            ViewBag.Title = title;
            if (mode == "market")
            {
                var (markets, sources) = _service.GetMarketSourceLookups();
                ViewBag.Markets = markets;
                ViewBag.Sources = sources;
            }
            return PartialView("~/Views/NightAudit/ExportRevenueAccounting.cshtml");
        }

        [HttpGet]
        public IActionResult ExportRevenueAccountingData(string mode, DateTime date, string? marketIds, string? sourceIds)
        {
            try
            {
                if (mode != "market" && mode != "sun" && mode != "invoice")
                    return Json(new { success = false, message = "Invalid mode." });

                var r = _service.SearchAccounting(mode, date, marketIds ?? "", sourceIds ?? "");
                if (r.Error != null) return Json(new { success = false, message = r.Error });

                return Json(new
                {
                    success = true,
                    data = r.Rows,
                    totalDebit = r.TotalDebit,
                    totalCredit = r.TotalCredit,
                    excel = new { startRow = r.ExcelStartRow, fileName = r.ExcelFileName, headers = r.ExcelHeaders, rows = r.ExcelRows }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult ExportRevenueData(DateTime date, bool summary = true)
        {
            try
            {
                var result = _service.Search(date, summary);
                if (result.Error != null) return Json(new { success = false, message = result.Error });
                return Json(new { success = true, data = result.Rows });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}

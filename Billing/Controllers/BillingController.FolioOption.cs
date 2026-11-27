using BaseBusiness.util;
using Billing.Services.Implements;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Globalization;
using System.Linq;

namespace Billing.Controllers
{
    /// <summary>
    /// Folio Option (port từ WinForm frmCshFolioOption): chọn kiểu in folio rồi xem trước / in.
    /// </summary>
    public partial class BillingController
    {
        [HttpGet]
        public IActionResult GetFolioOptionInfo(int folioId)
        {
            try
            {
                var svc = new FolioOptionService();
                var ctx = svc.LoadContext(folioId);
                int folioNo = ctx.Folio.FolioNo;
                return Json(new
                {
                    code = 0,
                    folioId = ctx.Folio.ID,
                    accountName = ctx.Folio.AccountName,
                    window = folioNo > 0 ? folioNo.ToString() : "M" + (-folioNo)
                });
            }
            catch (Exception ex)
            {
                return Json(new { code = 1, msg = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult FolioOptionPrint(int folioId, string style, bool vietnamese, string cashier, string invoiceNos, string user,
            string fromDate = null, string toDate = null, string text1 = null, string text2 = null)
        {
            try
            {
                if (folioId <= 0)
                    return Json(new { code = 1, msg = "Please choose a folio." });

                style = (style ?? "").Trim().ToUpperInvariant();
                if (!FolioOptionService.SupportedStyles.Contains(style) || style.StartsWith("FOLIO_VAT_"))
                    return Json(new { code = 1, msg = "This print style is not available." });

                var svc = new FolioOptionService();
                // WinForm: From = ngày đến của reservation, To = ngày hiện tại (DateTimePicker ẩn trên form)
                var ctx = svc.LoadContext(folioId);
                DateTime defaultTo = ctx.BusinessDate > DateTime.Today ? ctx.BusinessDate : DateTime.Today;
                DateTime from = ParseFolioOptionDate(fromDate, ctx.ArrivalDate.Date);
                DateTime to = ParseFolioOptionDate(toDate, defaultTo).Date.AddDays(1).AddSeconds(-1);

                string[] invoices = (invoiceNos ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                FolioOptionService.PrintResult data = style switch
                {
                    FolioOptionService.STYLE_FOLIO_DETAIL => svc.BuildFolioDetail(folioId, from, to, cashier, user, text1, text2),
                    _ => svc.BuildBillingFamily(style, folioId, from, to, vietnamese, cashier, invoices)
                };
                return Json(new { code = 0, data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FolioOptionPrint failed. FolioID={FolioID}, Style={Style}", folioId, style);
                return Json(new { code = 1, msg = ex.Message });
            }
        }

        /// <summary>Giá trị mặc định cho hộp thoại thông tin VAT (frmCshVATInfor).</summary>
        [HttpGet]
        public IActionResult GetFolioVatInfo(int folioId)
        {
            try
            {
                var svc = new FolioOptionService();
                var info = svc.GetVatInfoDefaults(folioId);
                bool valid = svc.CheckValidPrintVAT(folioId, out string printedMessage);
                return Json(new { code = 0, data = info, allowPrinting = valid, printedMessage });
            }
            catch (Exception ex)
            {
                return Json(new { code = 1, msg = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult FolioOptionPrintVat(int folioId, string style, bool printForLocal, string cashier, FolioOptionService.VatInfo info)
        {
            try
            {
                if (folioId <= 0)
                    return Json(new { code = 1, msg = "Please choose a folio." });
                style = (style ?? "").Trim().ToUpperInvariant();
                if (!style.StartsWith("FOLIO_VAT_") || !FolioOptionService.SupportedStyles.Contains(style))
                    return Json(new { code = 1, msg = "Invalid VAT style." });

                var data = new FolioOptionService().BuildFolioVat(style, folioId, printForLocal, info, cashier);
                return Json(new { code = 0, data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FolioOptionPrintVat failed. FolioID={FolioID}, Style={Style}", folioId, style);
                return Json(new { code = 1, msg = ex.Message });
            }
        }

        /// <summary>Print History (frmPrintVATSearch): lịch sử in hóa đơn VAT của folio.</summary>
        [HttpGet]
        public IActionResult GetFolioPrintVatHistory(int folioId)
        {
            try
            {
                DataTable dt = new FolioOptionService().GetPrintVatHistory(folioId);
                var columns = dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray();
                var rows = dt.Rows.Cast<DataRow>().Select(r => columns.Select(c =>
                {
                    object v = r[c];
                    if (v == DBNull.Value) return "";
                    if (v is DateTime d) return d.ToString("dd/MM/yyyy HH:mm");
                    if (v is decimal m) return m.ToString("#,##0.##", CultureInfo.InvariantCulture);
                    return v.ToString();
                }).ToArray()).ToArray();
                return Json(new { code = 0, columns, rows });
            }
            catch (Exception ex)
            {
                return Json(new { code = 1, msg = ex.Message });
            }
        }

        private static DateTime ParseFolioOptionDate(string value, DateTime fallback)
        {
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            string[] formats = { "yyyy-MM-dd", "dd/MM/yyyy", "yyyy-MM-ddTHH:mm:ss" };
            if (DateTime.TryParseExact(value.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d))
                return d;
            return DateTime.TryParse(value, out d) ? d : fallback;
        }
    }
}

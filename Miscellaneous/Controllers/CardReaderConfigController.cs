using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using BaseBusiness.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Miscellaneous.Services.Interfaces;

namespace Miscellaneous.Controllers
{
    /// <summary>
    /// Config Computer - Card Reader. Đường dẫn vẫn là /Miscellaneous/...
    /// Chưa có mã quyền riêng nên dùng chung quyền Card Management.
    /// </summary>
    [Route("Miscellaneous/[action]")]
    public class CardReaderConfigController : Controller
    {
        private const string Right = "misc_CardManagement";
        private readonly IMiscellaneousService _service;
        private readonly ILogger<CardReaderConfigController> _logger;

        public CardReaderConfigController(IMiscellaneousService service, ILogger<CardReaderConfigController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet, RequireFunction(Right)]
        public IActionResult ConfigComputerCardReader() => PartialView("~/Views/Miscellaneous/ConfigComputerCardReader.cshtml");

        [HttpGet, RequireFunction(Right)]
        public IActionResult GetCardReaderLookups() => Run(() => Json(new
        {
            readers = Rows(_service.CardReaders()),
            computers = Rows(_service.Computers())
        }));

        [HttpGet, RequireFunction(Right)]
        public IActionResult GetComputerCardReaders(string? cardReaderIds)
        {
            // Chỉ nhận số để chuỗi gửi cho stored procedure luôn hợp lệ
            var ids = (cardReaderIds ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(value => int.TryParse(value, out var id) ? id : 0)
                .Where(id => id > 0);
            return Run(() => Json(Rows(_service.ComputerCardReaders(string.Join(",", ids)))));
        }

        [HttpPost, ValidateAntiForgeryToken, RequireFunction(Right)]
        public IActionResult SaveComputerCardReader(int cardReaderId, List<int> computerIds)
        {
            var errors = new List<string>();
            if (cardReaderId <= 0) errors.Add("CardReader can not be blank.");
            if (computerIds == null || computerIds.Count == 0) errors.Add("Computer can not be blank.");
            if (errors.Count > 0) return Error(string.Join("\n", errors));

            return Run(() => _service.SaveComputerCardReader(cardReaderId, computerIds!)
                ? Json(new { success = true })
                : Error("CardReader does not exist."));
        }

        [HttpPost, ValidateAntiForgeryToken, RequireFunction(Right)]
        public IActionResult DeleteComputerCardReaders(List<int> ids)
        {
            if (ids == null || ids.Count == 0) return Error("Please select a config to delete.");
            return Run(() => Json(new { success = true, deleted = _service.DeleteComputerCardReaders(ids) }));
        }

        private IActionResult Error(string message) => BadRequest(new { success = false, message });

        private IActionResult Run(Func<IActionResult> action)
        {
            try
            {
                return action();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Card reader config failed");
                return StatusCode(500, new { success = false, message = "Cannot process. Please check the database configuration." });
            }
        }

        private static List<Dictionary<string, object?>> Rows(DataTable table) =>
            table.AsEnumerable().Select(row => table.Columns.Cast<DataColumn>().ToDictionary(
                col => col.ColumnName.ToLowerInvariant(),
                col => row[col] == DBNull.Value ? null : row[col])).ToList();
    }
}

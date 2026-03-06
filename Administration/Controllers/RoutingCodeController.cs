using BaseBusiness.BO;
using BaseBusiness.Model;
using BaseBusiness.util;
using Microsoft.AspNetCore.Mvc;

namespace Administration.Controllers
{
    [Route("/Administration/RoutingCode")]
    public class RoutingCodeController : Controller
    {
        public RoutingCodeController()
        {
        }

        [HttpGet("")]
        public IActionResult RoutingCode()
        {
            List<TransactionsModel> listTransac = PropertyUtils.ConvertToList<TransactionsModel>(TransactionsBO.Instance.FindAll());
            ViewBag.TransactionsList = listTransac;

            return PartialView("~/Views/Administration/RoutingCode.cshtml");
            // Truyền đường dẫn chuẩn vào để tìm đúng
        }
        [HttpGet("GetAllRoutingCode")]
        public IActionResult GetAllRoutingCode()
        {
            try
            {
                var list = PropertyUtils.ConvertToList<RoutingCodeModel>(
                RoutingCodeBO.Instance.FindAll());

                return Json(new
                {
                    success = true,
                    message = "Success",
                    data = list
                });
            }
            catch (Exception ex)
            {

                return Json(new
                {
                    success = false,
                    message = "Error: " + ex.Message
                });
            }
        }

        public class RoutingCodeUpsertDTO
        {
            public int Id { get; set; }                 // l
            public string Code { get; set; }            // nvarchar(7)
            public string Description { get; set; }     // nvarchar(?)
            public List<string> TransactionCodes { get; set; } = new(); // select multiple
        }

        [HttpPost("UpsertRoutingCode")]
        public IActionResult UpsertRoutingCode([FromBody] RoutingCodeUpsertDTO dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest(new { success = false, message = "Payload is null" });

                var errors = new List<object>();

                // ===== Rule for Id (l) =====
                if (dto.Id < 0)
                    errors.Add(new { field = "rouC_id", message = "ID < 0 is invalid." });

                // ===== Required validate =====
                if (string.IsNullOrWhiteSpace(dto.Code))
                    errors.Add(new { field = "rouC_codeAdd", message = "Code not null." });

                if (string.IsNullOrWhiteSpace(dto.Description))
                    errors.Add(new { field = "rouC_descriptionAdd", message = "Description not null." });

                if (dto.TransactionCodes == null || dto.TransactionCodes.Count == 0)
                    errors.Add(new { field = "rouC_transactionCodeIDAdd", message = "TransactionCodes not null." });

                // ===== Business validate transaction codes exist =====
                if (dto.TransactionCodes != null)
                {
                    foreach (var t in dto.TransactionCodes)
                    {
                        if (string.IsNullOrWhiteSpace(t))
                        {
                            errors.Add(new { field = "rouC_transactionCodeIDAdd", message = "Transaction code cannot be empty." });
                            continue;
                        }

                        var transList = TransactionsBO.Instance.FindByAttribute("Code", t);
                        if (transList == null || transList.Count == 0)
                            errors.Add(new { field = "rouC_transactionCodeIDAdd", message = $"Invalid Transaction Code: {t}." });
                    }
                }

                if (errors.Count != 0)
                    return Json(new { success = false, message = "Validation failed.", errors });

                // ===== Normalize input =====
                var code = dto.Code.Trim();
                var desc = dto.Description.Trim();

                // Clean list + unique (optional)
                var transArr = dto.TransactionCodes
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .Distinct()
                    .ToList();

                // Save as "1002,1003," (same style as your example)
                var transStr = string.Join(",", transArr) + ",";

                // ===== UPSERT =====
                if (dto.Id == 0)
                {
                    // INSERT
                    var model = new RoutingCodeModel
                    {
                        Code = code,
                        Description = desc,
                        TransactionCodes = transStr
                    };

                    // --- Use your BO ---
                    // Case A: Insert returns int id
                    // Case B: Insert returns void but sets model.Id
                    // Case C: Insert returns void and doesn't set Id -> fallback by querying (if allowed by your BO)
                    int newId = 0;

                    var insertResult = RoutingCodeBO.Instance.Insert(model);

                    return Ok(new
                    {
                        success = true,
                        message = "Routing code inserted successfully",
                        data = new { id = newId, action = "insert" }
                    });
                }
                else
                {
                    // UPDATE
                    var existing = RoutingCodeBO.Instance.FindByPrimaryKey(dto.Id) as RoutingCodeModel;
                    if (existing == null)
                        return Json(new { success = false, message = $"Not found. Id={dto.Id}" });

                    existing.Code = code;
                    existing.Description = desc;
                    existing.TransactionCodes = transStr;

                    RoutingCodeBO.Instance.Update(existing);

                    return Ok(new
                    {
                        success = true,
                        message = $"Routing code {dto.Id} updated successfully",
                        data = new { id = dto.Id, action = "update" }
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    errors = ex.Message,
                    innerException = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        public class DeleteRequest
        {
            public int ID { get; set; }
        }

        [HttpPost("RoutingDelete")]
        public IActionResult RoutingDelete([FromBody] DeleteRequest delete)
        {
            try
            {
                int id = delete.ID;
                if (RoutingCodeBO.Instance.FindByPrimaryKey(id) is not RoutingCodeModel existing || existing.ID == 0)
                {
                    return Ok(new { success = false, message = $"Routing Code ID {id} not found." });
                }

                RoutingCodeBO.Instance.Delete(id);
                return Ok(new { success = true, message = $"Successfully deleted Routing Code ID {id}." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}

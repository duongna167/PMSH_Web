using System.Data;
using System.Globalization;
using System.Text;
using BaseBusiness.util;
using EInvoice.Dto;
using EInvoice.Runtime;
using EInvoice.Services.Interfaces;
using Microsoft.Data.SqlClient;

namespace EInvoice.Services.Implements
{
    public class EInvoiceInvoiceService : IEInvoiceInvoiceService
    {
        private readonly IEInvoiceRuntimeService _runtime;

        // Nghiệp vụ được phép chạy từ màn hình (UPLOAD_AND_PUBLISH = Upload rồi Publish)
        private static readonly string[] AllowedActions = ["ISSUE_INVOICE", "UPLOAD_INVOICE", "PUBLISH_INVOICE", "UPLOAD_AND_PUBLISH"];

        public EInvoiceInvoiceService(IEInvoiceRuntimeService runtime)
        {
            _runtime = runtime;
        }

        // =====================================================================================================
        //  Tìm hóa đơn
        // =====================================================================================================

        public DataTable SearchInvoices(InvoiceSearchRequest request)
        {
            if (request.PropertyID <= 0)
                throw new ProviderBuildException("Please select property!");

            PmsSourceConfig source = EInvoiceRuntimeService.LoadSource(request.PropertyID);
            if (string.IsNullOrWhiteSpace(source.InvoiceSearchSP))
                throw new ProviderBuildException($"Property {source.Code} chưa cấu hình Invoice Search SP.");

            if (!DateTime.TryParseExact(request.FromDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime from)
                || !DateTime.TryParseExact(request.ToDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime to))
                throw new ProviderBuildException("From Date or To Date is invalid.");

            from = from.Date;
            to = to.Date.AddDays(1).AddTicks(-1);
            if (from > to)
                throw new ProviderBuildException("From Date cannot be greater than To Date.");

            var table = new DataTable();
            using (var conn = new SqlConnection(source.BuildConnectionString()))
            using (var cmd = new SqlCommand(source.InvoiceSearchSP, conn) { CommandType = CommandType.StoredProcedure, CommandTimeout = 120 })
            {
                cmd.Parameters.AddWithValue("@ConfirmationNo", request.ConfNo ?? "");
                cmd.Parameters.AddWithValue("@FolioNo", request.FolioNo ?? "");
                cmd.Parameters.AddWithValue("@SerialNo", request.Serial ?? "");
                cmd.Parameters.AddWithValue("@InvoiceNo", request.InvoiceNo ?? "");
                cmd.Parameters.Add("@FromDate", SqlDbType.DateTime).Value = from;
                cmd.Parameters.Add("@ToDate", SqlDbType.DateTime).Value = to;
                cmd.Parameters.AddWithValue("@Print", request.Print);
                cmd.Parameters.AddWithValue("@ResType", request.ResType);
                cmd.Parameters.AddWithValue("@ZoneID", "");
                new SqlDataAdapter(cmd).Fill(table);
            }

            // Lọc theo tên khách/công ty (SP không có tham số này)
            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                string name = request.Name.Trim();
                for (int i = table.Rows.Count - 1; i >= 0; i--)
                {
                    bool match = Contains(table.Rows[i], "Customer_Name", name) || Contains(table.Rows[i], "Customer_Company", name);
                    if (!match) table.Rows.RemoveAt(i);
                }
            }

            ApplyProviderStates(table, request.PropertyID);
            return table;
        }

        private static bool Contains(DataRow row, string column, string text) =>
            row.Table.Columns.Contains(column) && row[column] != DBNull.Value
            && Convert.ToString(row[column])!.Contains(text, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Ghi đè trạng thái theo nhật ký thực thi: có Publish thành công thì "Published", chỉ có Upload thì "Uploaded",
        /// đồng thời lấy Pattern/Serial/InvoiceNo/InvoiceDate do nhà cung cấp trả về.
        /// </summary>
        private static void ApplyProviderStates(DataTable table, int propertyId)
        {
            if (table.Rows.Count == 0 || !table.Columns.Contains("ID")) return;

            var xml = new StringBuilder("<ids>");
            foreach (DataRow r in table.Rows)
                if (r["ID"] != DBNull.Value) xml.Append("<i v=\"").Append(Convert.ToInt32(r["ID"])).Append("\"/>");
            xml.Append("</ids>");

            DataTable states = DataTableHelper.getTableData("spEInvoice_InvoiceStates",
                [new SqlParameter("@PropertyID", propertyId), new SqlParameter("@Ids", SqlDbType.Xml) { Value = xml.ToString() }]);
            if (states.Rows.Count == 0) return;

            // Kết quả mỗi (hóa đơn, nghiệp vụ) một dòng; Publish/Issue được ưu tiên hơn Upload
            var byInvoice = new Dictionary<int, (bool Upload, bool Publish, DataRow? Values)>();
            foreach (DataRow s in states.Rows)
            {
                int id = Convert.ToInt32(s["SourceRecordID"]);
                string action = Convert.ToString(s["BusinessActionCode"])?.Trim().ToUpperInvariant() ?? "";
                byInvoice.TryGetValue(id, out var cur);

                bool publish = action is "PUBLISH_INVOICE" or "ISSUE_INVOICE";
                bool upload = action == "UPLOAD_INVOICE";
                bool applyValues = publish || !cur.Publish;
                byInvoice[id] = (cur.Upload || upload, cur.Publish || publish, applyValues ? s : cur.Values);
            }

            if (!table.Columns.Contains("FormNo")) table.Columns.Add("FormNo", typeof(string));

            foreach (DataRow r in table.Rows)
            {
                if (r["ID"] == DBNull.Value || !byInvoice.TryGetValue(Convert.ToInt32(r["ID"]), out var st)) continue;

                if (st.Publish && table.Columns.Contains("EStatus")) r["EStatus"] = "Published";
                else if (st.Upload && table.Columns.Contains("EStatus")) r["EStatus"] = "Uploaded";

                if (st.Values == null) continue;
                SetText(r, "FormNo", st.Values["Pattern"]);
                SetText(r, "SerialNo", st.Values["Serial"]);
                SetText(r, "EInvoiceNo", st.Values["InvoiceNo"]);
                if (st.Values["InvoiceDate"] != DBNull.Value && table.Columns.Contains("VATDate"))
                    r["VATDate"] = st.Values["InvoiceDate"];
            }
        }

        private static void SetText(DataRow row, string column, object? value)
        {
            string text = Convert.ToString(value)?.Trim() ?? "";
            if (text.Length > 0 && row.Table.Columns.Contains(column)) row[column] = text;
        }

        // =====================================================================================================
        //  Nghiệp vụ hàng loạt
        // =====================================================================================================

        public async Task<OpResult> ExecuteBatchAsync(InvoiceBatchRequest request, RuntimeUser user, CancellationToken ct = default)
        {
            if (request.PropertyID <= 0) return OpResult.Fail("Vui lòng chọn Property.");

            string action = (request.ActionCode ?? "").Trim().ToUpperInvariant();
            if (action.Length == 0) return OpResult.Fail("ActionCode is required.");
            if (!AllowedActions.Contains(action)) return OpResult.Fail($"ActionCode '{action}' không được hỗ trợ.");

            List<int> ids = request.FolioVatIds?.Where(x => x > 0).Distinct().ToList() ?? [];
            if (ids.Count == 0) return OpResult.Fail("Vui lòng chọn ít nhất một hóa đơn.");
            if (!request.ConfirmSend) return OpResult.Fail("Bạn chưa xác nhận thực hiện nghiệp vụ.");

            var results = new List<object>();
            var notes = new List<(int Id, bool Success, string Message)>();
            int ok = 0, failed = 0;

            foreach (int id in ids)
            {
                string completed = "";
                try
                {
                    string message;
                    if (action == "UPLOAD_AND_PUBLISH")
                    {
                        // Upload lỗi thì tuyệt đối không Publish
                        ProviderExecutionResult up = await _runtime.ExecuteActionAsync(request.PropertyID, id, "UPLOAD_INVOICE", user, ct);
                        if (!up.Success) throw new StepFailed("UPLOAD_INVOICE", "Upload thất bại: " + up.Message);
                        completed = "UPLOAD_INVOICE";

                        ProviderExecutionResult pub = await _runtime.ExecuteActionAsync(request.PropertyID, id, "PUBLISH_INVOICE", user, ct);
                        if (!pub.Success) throw new StepFailed("PUBLISH_INVOICE", "Upload thành công nhưng Publish thất bại: " + pub.Message);

                        completed = "PUBLISH_INVOICE";
                        message = string.Join("; ", new[] { up.Message, pub.Message }.Where(m => !string.IsNullOrWhiteSpace(m)));
                    }
                    else
                    {
                        ProviderExecutionResult r = await _runtime.ExecuteActionAsync(request.PropertyID, id, action, user, ct);
                        if (!r.Success) throw new StepFailed(action, r.Message);
                        completed = action;
                        message = r.Message;
                    }

                    ok++;
                    results.Add(new { folioVATID = id, success = true, completedStep = completed, message });
                    notes.Add((id, true, message));
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (StepFailed ex)
                {
                    failed++;
                    results.Add(new { folioVATID = id, success = false, completedStep = completed, failedStep = ex.Step, message = ex.Message });
                    notes.Add((id, false, ex.Message));
                }
                catch (ProviderBuildException ex)
                {
                    failed++;
                    results.Add(new { folioVATID = id, success = false, completedStep = completed, failedStep = action, message = ex.Message });
                    notes.Add((id, false, ex.Message));
                }
                catch (Exception ex)
                {
                    failed++;
                    string msg = "Không thể thực hiện nghiệp vụ: " + ex.Message;
                    results.Add(new { folioVATID = id, success = false, completedStep = completed, failedStep = action, message = msg });
                    notes.Add((id, false, msg));
                }
            }

            string text = BuildMessage(action, ok, failed, notes);
            var data = new { successCount = ok, failedCount = failed, results };
            return failed == 0 && ok > 0
                ? OpResult.Ok(text, data)
                : new OpResult { Success = false, Message = text, Data = data };
        }

        /// <summary>Một bước của nghiệp vụ bị nhà cung cấp từ chối (khác với lỗi hệ thống).</summary>
        private sealed class StepFailed : Exception
        {
            public string Step { get; }
            public StepFailed(string step, string message) : base(message) { Step = step; }
        }

        private static string BuildMessage(string action, int ok, int failed, List<(int Id, bool Success, string Message)> items)
        {
            string name = action switch
            {
                "ISSUE_INVOICE" => "Issue",
                "UPLOAD_INVOICE" => "Upload",
                "PUBLISH_INVOICE" => "Publish",
                "UPLOAD_AND_PUBLISH" => "Upload & Publish",
                _ => "Thực hiện"
            };

            string summary = failed == 0 ? $"{name} thành công {ok} hóa đơn PMS."
                : ok > 0 ? $"{name}: thành công {ok}, thất bại {failed} hóa đơn PMS."
                : $"{name} thất bại {failed} hóa đơn PMS.";

            IEnumerable<string> details = items.Select(i =>
            {
                string m = string.IsNullOrWhiteSpace(i.Message)
                    ? (i.Success ? $"{name} thành công." : "Nhà cung cấp không trả nội dung lỗi.")
                    : i.Message.Trim().TrimEnd('.');
                return i.Success ? $"{m} (FolioVATID: {i.Id})." : $"{name} thất bại hóa đơn PMS (FolioVATID: {i.Id}). Lỗi: {m}.";
            });
            return summary + " " + string.Join(" ", details);
        }

        // =====================================================================================================
        //  Nhật ký
        // =====================================================================================================

        public DataTable GetAllowedProperties(int userId, int userGroupId) =>
            DataTableHelper.getTableData("spEInvoice_AllowedProperties",
                [new SqlParameter("@UserID", userId), new SqlParameter("@UserGroupID", userGroupId), new SqlParameter("@SourceType", "PMS")]);

        public DataTable GetProviderActions(int propertyId) =>
            DataTableHelper.getTableData("spEInvoice_ProviderActions", [new SqlParameter("@PropertyID", propertyId)]);

        public DataTable SearchLogs(LogSearchRequest request)
        {
            if (request.PropertyID <= 0) throw new ProviderBuildException("Please select property first!");

            DateTime? Date(string? s) => DateTime.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d) ? d.Date : null;

            return DataTableHelper.getTableData("spEInvoice_LogSearch",
            [
                new SqlParameter("@PropertyID", request.PropertyID),
                new SqlParameter("@FolioVATID", int.TryParse(request.FolioVatId, out int id) && id > 0 ? id : DBNull.Value),
                new SqlParameter("@ActionCode", string.IsNullOrWhiteSpace(request.LogType) ? DBNull.Value : request.LogType.Trim().ToUpperInvariant()),
                new SqlParameter("@Status", request.Status is "0" or "1" ? int.Parse(request.Status) : DBNull.Value),
                new SqlParameter("@FromDate", (object?)Date(request.FromDate) ?? DBNull.Value),
                new SqlParameter("@ToDate", (object?)Date(request.ToDate) ?? DBNull.Value)
            ]);
        }

        public DataTable GetLogDetail(long id) =>
            DataTableHelper.getTableData("spEInvoice_ExecutionLogDetail", [new SqlParameter("@ID", id)]);
    }
}

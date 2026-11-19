using System.Data;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using BaseBusiness.util;
using EInvoice.Dto;
using EInvoice.Runtime;
using EInvoice.Services.Interfaces;
using Microsoft.Data.SqlClient;

namespace EInvoice.Services.Implements
{
    public class EInvoiceRuntimeService : IEInvoiceRuntimeService
    {
        private readonly DynamicProviderDocumentBuilder _documentBuilder;
        private readonly ProviderExecutionService _executor;
        private readonly PmsInvoiceContextBuilder _contextBuilder;

        // Khóa JSON/header chứa bí mật: giá trị bị che trong bản xem trước
        private static readonly Regex SensitiveKey = new(@"pass|secret|token|api[_-]?key|authorization|credential", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public EInvoiceRuntimeService(DynamicProviderDocumentBuilder documentBuilder, ProviderExecutionService executor, PmsInvoiceContextBuilder contextBuilder)
        {
            _documentBuilder = documentBuilder;
            _executor = executor;
            _contextBuilder = contextBuilder;
        }

        // =====================================================================================================
        //  Xem trước request
        // =====================================================================================================

        public OpResult PreviewRequest(PreviewRequest request, RuntimeUser user)
        {
            if (request.PropertyID <= 0) return OpResult.Fail("PropertyID is required.");
            if (request.OperationID <= 0) return OpResult.Fail("OperationID is required.");
            if (request.FolioVATID <= 0) return OpResult.Fail("FolioVATID is required.");

            try
            {
                ProviderOperationRuntimeModel? operation = LoadOperation(0, null, request.OperationID);
                if (operation == null)
                    return OpResult.Fail("Provider Operation does not exist.");

                List<ProviderMappingModel> mappings = LoadMappings(operation.ID);
                if (mappings.Count == 0)
                    return OpResult.Fail("Operation does not have any mappings.");

                Dictionary<string, object?> config = LoadConfig(request.PropertyID, operation.ProviderID);
                if (config.Count == 0)
                    return OpResult.Fail("Property chưa cấu hình Provider của Operation này.");

                // Một số provider dùng API_URL thay cho BASE_URL
                if (!HasValue(config, "BASE_URL"))
                {
                    if (!HasValue(config, "API_URL"))
                        return OpResult.Fail("Property chưa nhập BASE_URL/API_URL.");
                    config["BASE_URL"] = Convert.ToString(config["API_URL"])?.Trim() ?? string.Empty;
                }

                PmsSourceConfig source = LoadSource(request.PropertyID);
                ProviderRequestContext context = _contextBuilder.Create(source, config, request.FolioVATID,
                    LoadPrevious(request.PropertyID, request.FolioVATID), user.UserId, user.UserName, user.LoginName);

                var builder = new DynamicProviderRequestBuilder(_documentBuilder);
                ProviderRequestResult result = builder.Build(operation, mappings, context);

                return OpResult.Ok("Provider request preview created successfully.", new
                {
                    propertyID = request.PropertyID,
                    folioVATID = request.FolioVATID,
                    result.OperationID,
                    result.OperationCode,
                    result.HttpMethod,
                    result.BaseUrl,
                    result.Endpoint,
                    result.Url,
                    headers = MaskDictionary(result.Headers),
                    queryParameters = MaskDictionary(result.QueryParameters),
                    pathParameters = result.PathParameters,
                    formFields = MaskDictionary(result.FormFields),
                    body = MaskJson(result.Body),
                    result.AppliedMappings,
                    result.SkippedMappings,
                    result.Warnings
                });
            }
            catch (ProviderBuildException ex)
            {
                return OpResult.Fail(ex.Message);
            }
            catch (Exception ex)
            {
                return OpResult.Fail("Preview provider request failed: " + ex.Message);
            }
        }

        // =====================================================================================================
        //  Thực thi một Business Action cho một hóa đơn PMS
        // =====================================================================================================

        public async Task<ProviderExecutionResult> ExecuteActionAsync(int propertyId, int folioVatId, string actionCode, RuntimeUser user, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(actionCode)) throw new ProviderBuildException("ActionCode không được để trống.");
            if (propertyId <= 0) throw new ProviderBuildException("PropertyID không hợp lệ.");
            if (folioVatId <= 0) throw new ProviderBuildException("FolioVATID không hợp lệ.");

            actionCode = actionCode.Trim().ToUpperInvariant();

            var persistence = new ProviderExecutionPersistenceContext
            {
                PropertyID = propertyId,
                FolioVATID = folioVatId,
                SourceType = "PMS",
                SourceRecordID = folioVatId,
                OperationCode = actionCode,
                ExecutedBy = user.DisplayName
            };

            try
            {
                if (string.IsNullOrWhiteSpace(user.DisplayName))
                    throw new ProviderBuildException("Phiên đăng nhập đã hết hạn.");

                PmsSourceConfig source = LoadSource(propertyId);
                persistence.SourceType = source.SourceType;

                DataTable pp = DataTableHelper.getTableData("spEInvoice_RuntimeProvider", [new SqlParameter("@PropertyID", propertyId)]);
                if (pp.Rows.Count == 0)
                    throw new ProviderBuildException("Property chưa cấu hình Provider mặc định.");
                int providerId = Convert.ToInt32(pp.Rows[0]["ProviderID"]);
                persistence.ProviderID = providerId;

                ProviderOperationRuntimeModel? operation = LoadOperation(providerId, actionCode, 0);
                if (operation == null)
                    throw new ProviderBuildException($"ProviderID {providerId} chưa cấu hình chức năng '{actionCode}'.");

                persistence.OperationID = operation.ID;
                persistence.OperationCode = string.IsNullOrWhiteSpace(operation.OperationCode) ? actionCode : operation.OperationCode;

                List<ProviderMappingModel> mappings = LoadMappings(operation.ID);
                if (mappings.Count == 0)
                    throw new ProviderBuildException($"Operation '{operation.OperationCode}' chưa có request mapping.");

                Dictionary<string, object?> config = LoadConfig(propertyId, operation.ProviderID);
                if (config.Count == 0)
                    throw new ProviderBuildException("Property chưa lưu cấu hình Provider.");
                if (!HasValue(config, "BASE_URL"))
                    throw new ProviderBuildException("Property chưa cấu hình BASE_URL.");

                ProviderRequestContext context = _contextBuilder.Create(source, config, folioVatId,
                    LoadPrevious(propertyId, folioVatId), user.UserId, user.UserName, user.LoginName);

                await AuthenticateAsync(operation, config, context, cancellationToken);

                // Publish cần kết quả Upload thành công trước đó
                if (actionCode == "PUBLISH_INVOICE")
                {
                    if (context.PreviousResponse is not Dictionary<string, object?> previous)
                        throw new ProviderBuildException("Không đọc được kết quả Upload trước đó.");
                    if (string.IsNullOrWhiteSpace(Convert.ToString(previous.GetValueOrDefault("InvoiceNumber"))))
                        throw new ProviderBuildException($"Không tìm thấy kết quả Upload thành công của FolioVATID {folioVatId}.");
                }

                ProviderExecutionResult result = await _executor.ExecuteAsync(operation, mappings, context, persistence, cancellationToken);

                if (result.Success)
                    await SynchronizeStatusAsync(source, folioVatId, actionCode, result.ReserveResult, cancellationToken);

                return result;
            }
            catch (OperationCanceledException)
            {
                await _executor.SaveFailureLogAsync(persistence, "Thao tác đã bị hủy.", CancellationToken.None);
                throw;
            }
            catch (StatusSyncException)
            {
                // Nhà cung cấp đã xử lý thành công và đã có log thành công; chỉ bước đồng bộ DB PMS lỗi
                // nên không ghi thêm log thất bại gây hiểu nhầm.
                throw;
            }
            catch (Exception ex)
            {
                await _executor.SaveFailureLogAsync(persistence, ex.Message, CancellationToken.None);
                throw;
            }
        }

        /// <summary>
        /// Operation yêu cầu xác thực: chạy Operation đăng nhập đã khai báo, lấy token từ phản hồi rồi đặt vào PreviousResponse
        /// để mapping (SourceType = RESPONSE) đưa token vào header của Operation chính.
        /// Không có Operation đăng nhập (Basic/API key tĩnh) thì bỏ qua: header đọc thẳng từ CONFIG.
        /// </summary>
        private async Task AuthenticateAsync(ProviderOperationRuntimeModel mainOperation, Dictionary<string, object?> config,
            ProviderRequestContext mainContext, CancellationToken cancellationToken)
        {
            if (!mainOperation.IsAuthenticationRequired) return;
            if (!mainOperation.AuthenticationOperationID.HasValue || mainOperation.AuthenticationOperationID.Value <= 0) return;

            int authId = mainOperation.AuthenticationOperationID.Value;
            ProviderOperationRuntimeModel? auth = LoadOperation(0, null, authId);
            if (auth == null)
                throw new ProviderBuildException($"Không tìm thấy Authentication Operation ID {authId} của operation '{mainOperation.OperationCode}'.");
            if (auth.ProviderID != mainOperation.ProviderID)
                throw new ProviderBuildException("Authentication Operation và Main Operation không cùng Provider.");

            List<ProviderMappingModel> authMappings = LoadMappings(auth.ID);
            if (authMappings.Count == 0)
                throw new ProviderBuildException($"Authentication Operation '{auth.OperationCode}' chưa có request mapping.");

            var authContext = new ProviderRequestContext
            {
                Config = new Dictionary<string, object?>(config, StringComparer.OrdinalIgnoreCase),
                Property = mainContext.Property,
                Session = new Dictionary<string, object?>(mainContext.Session, StringComparer.OrdinalIgnoreCase),
                System = new Dictionary<string, object?>(mainContext.System, StringComparer.OrdinalIgnoreCase)
            };

            ProviderRawExecutionResult authResult = await _executor.ExecuteRawAsync(auth, authMappings, authContext, cancellationToken);

            if (!authResult.Success)
                throw new ProviderBuildException($"Xác thực Provider thất bại tại operation '{auth.OperationCode}'. {authResult.Message}");
            if (string.IsNullOrWhiteSpace(authResult.RawResponse))
                throw new ProviderBuildException($"Authentication Operation '{auth.OperationCode}' không trả dữ liệu.");

            try
            {
                JsonNode? node = JsonNode.Parse(authResult.RawResponse)
                    ?? throw new ProviderBuildException($"Response của Authentication Operation '{auth.OperationCode}' đang rỗng.");
                mainContext.PreviousResponse = node;
            }
            catch (JsonException ex)
            {
                throw new ProviderBuildException($"Response của Authentication Operation '{auth.OperationCode}' không phải JSON hợp lệ.", ex);
            }
        }

        // =====================================================================================================
        //  Đồng bộ trạng thái về DB PMS của Property
        // =====================================================================================================

        private sealed class StatusSyncException : Exception
        {
            public StatusSyncException(string message, Exception inner) : base(message, inner) { }
        }

        /// <summary>
        /// Ghi kết quả vào tblUploadInvoiceLog / tblPulishInvoiceLog của DB PMS để SP tìm hóa đơn (EStatus, EInvoiceNo...) hiển thị đúng.
        /// Mỗi DB PMS có thể có bộ cột khác nhau nên chỉ ghi các cột thật sự tồn tại trong bảng.
        /// </summary>
        private static async Task SynchronizeStatusAsync(PmsSourceConfig source, int folioVatId, string actionCode, ReserveInvoiceResult? reserve, CancellationToken ct)
        {
            bool upload = actionCode is "UPLOAD_INVOICE" or "ISSUE_INVOICE" or "PUBLISH_INVOICE";
            bool publish = actionCode is "ISSUE_INVOICE" or "PUBLISH_INVOICE";
            if (!upload && !publish) return;

            try
            {
                await using var conn = new SqlConnection(source.BuildConnectionString());
                await conn.OpenAsync(ct);
                await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(ct);

                if (upload)
                {
                    var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Status"] = 1,
                        ["LogDate"] = DateTime.Now,
                        ["LogIndex"] = 0,
                        ["InputKey"] = "",
                        ["TotalReturn"] = ""
                    };
                    if (reserve != null)
                    {
                        SetIfAny(values, "RefID", reserve.Key);
                        SetIfAny(values, "Pattern", reserve.Pattern);
                        SetIfAny(values, "SerialNo", reserve.Serial);
                        SetIfAny(values, "InvoiceNo", reserve.InvoiceNo);
                        SetIfAny(values, "verificationCode", reserve.VerificationCode);
                        SetIfAny(values, "keySearch", reserve.KeySearch);
                        if (DateTime.TryParse(reserve.InvoiceDate, out DateTime d)) values["InvoiceDate"] = d;
                    }
                    // Đã phát hành: ReturnValue khác rỗng (SP tìm hóa đơn dựa vào đó để hiện "Published")
                    if (publish) values["ReturnValue"] = string.IsNullOrWhiteSpace(reserve?.InvoiceNo) ? "OK" : reserve!.InvoiceNo!;
                    await UpsertAsync(conn, tx, "tblUploadInvoiceLog", folioVatId, values, ct);
                }

                if (publish)
                {
                    var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Status"] = 1,
                        ["LogDate"] = DateTime.Now,
                        ["LogIndex"] = 0,
                        ["InputKey"] = "",
                        ["ReturnValue"] = "",
                        ["TotalReturn"] = "",
                        ["AdjustType"] = 0
                    };
                    await UpsertAsync(conn, tx, "tblPulishInvoiceLog", folioVatId, values, ct);
                }

                await tx.CommitAsync(ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new StatusSyncException(
                    $"Provider đã xử lý thành công nhưng đồng bộ trạng thái FolioVATID {folioVatId} về database PMS thất bại: {ex.Message}", ex);
            }
        }

        private static void SetIfAny(Dictionary<string, object?> values, string column, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value)) values[column] = value;
        }

        /// <summary>UPDATE theo FolioVATID, chưa có dòng thì INSERT; chỉ dùng các cột tồn tại trong bảng.</summary>
        private static async Task UpsertAsync(SqlConnection conn, SqlTransaction tx, string table, int folioVatId, Dictionary<string, object?> desired, CancellationToken ct)
        {
            var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await using (var cmd = new SqlCommand("SELECT name FROM sys.columns WHERE object_id = OBJECT_ID(@t)", conn, tx))
            {
                cmd.Parameters.AddWithValue("@t", "dbo." + table);
                await using var rd = await cmd.ExecuteReaderAsync(ct);
                while (await rd.ReadAsync(ct)) existing.Add(rd.GetString(0));
            }
            if (!existing.Contains("FolioVATID"))
                throw new InvalidOperationException($"Bảng {table} không có cột FolioVATID.");

            var cols = desired.Where(kv => existing.Contains(kv.Key)).ToList();
            if (cols.Count == 0) return;

            string set = string.Join(", ", cols.Select((c, i) => $"[{c.Key}] = @p{i}"));
            string names = string.Join(", ", cols.Select(c => $"[{c.Key}]"));
            string pars = string.Join(", ", cols.Select((c, i) => $"@p{i}"));

            string sql = $"UPDATE dbo.[{table}] SET {set} WHERE FolioVATID = @id; " +
                         $"IF @@ROWCOUNT = 0 INSERT INTO dbo.[{table}] (FolioVATID, {names}) VALUES (@id, {pars});";

            await using var upsert = new SqlCommand(sql, conn, tx) { CommandTimeout = 120 };
            upsert.Parameters.AddWithValue("@id", folioVatId);
            for (int i = 0; i < cols.Count; i++)
                upsert.Parameters.AddWithValue($"@p{i}", cols[i].Value ?? DBNull.Value);
            await upsert.ExecuteNonQueryAsync(ct);
        }

        // =====================================================================================================
        //  Nạp cấu hình runtime (qua stored procedure)
        // =====================================================================================================

        /// <summary>Thông tin kết nối DB nguồn của Property (giải mã mật khẩu). Chỉ chấp nhận nguồn PMS đang hoạt động.</summary>
        internal static PmsSourceConfig LoadSource(int propertyId)
        {
            DataTable dt = DataTableHelper.getTableData("spEInvoice_PropertyConnection", [new SqlParameter("@ID", propertyId)]);
            if (dt.Rows.Count == 0)
                throw new ProviderBuildException("Không tìm thấy Property.");

            DataRow r = dt.Rows[0];
            if (Convert.ToBoolean(r["Inactive"]))
                throw new ProviderBuildException("Property đang Inactive.");

            string sourceType = Convert.ToString(r["SourceType"]) ?? "";
            if (!string.Equals(sourceType, "PMS", StringComparison.OrdinalIgnoreCase))
                throw new ProviderBuildException($"Property {Convert.ToString(r["Code"])} không phải cấu hình PMS.");

            return new PmsSourceConfig
            {
                PropertyID = propertyId,
                Code = Convert.ToString(r["Code"]) ?? "",
                Name = Convert.ToString(r["Name"]) ?? "",
                SourceType = sourceType.ToUpperInvariant(),
                DatabaseIP = Convert.ToString(r["Database_IP"]) ?? "",
                DatabaseName = Convert.ToString(r["Database_Name"]) ?? "",
                DatabaseUser = Convert.ToString(r["Database_User"]) ?? "",
                DatabasePassword = EInvoiceSecret.Decrypt(r["Database_Password"] == DBNull.Value ? null : Convert.ToString(r["Database_Password"])),
                InvoiceSearchSP = Convert.ToString(r["InvoiceSearchSP"]) ?? "",
                InvoiceDetailSP = Convert.ToString(r["InvoiceDetailSP"]) ?? ""
            };
        }

        private static ProviderOperationRuntimeModel? LoadOperation(int providerId, string? actionCode, int operationId)
        {
            DataTable dt = DataTableHelper.getTableData("spEInvoice_RuntimeOperation",
            [
                new SqlParameter("@ProviderID", providerId),
                new SqlParameter("@ActionCode", (object?)actionCode ?? DBNull.Value),
                new SqlParameter("@OperationID", operationId)
            ]);
            if (dt.Rows.Count == 0) return null;

            DataRow r = dt.Rows[0];
            string Text(string col, string def) => r[col] == DBNull.Value ? def : (Convert.ToString(r[col])?.Trim() ?? def);

            var op = new ProviderOperationRuntimeModel
            {
                ID = Convert.ToInt32(r["ID"]),
                ProviderID = Convert.ToInt32(r["ProviderID"]),
                OperationCode = Text("OperationCode", ""),
                OperationName = Text("OperationName", ""),
                HttpMethod = Text("HttpMethod", "POST"),
                Endpoint = Text("Endpoint", ""),
                ContentType = Text("ContentType", "application/json"),
                BodyType = Text("BodyType", "JSON"),
                ExternalMethodCode = Text("ExternalMethodCode", ""),
                ActionCode = Text("ActionCode", ""),
                IsAuthenticationRequired = r["IsAuthenticationRequired"] != DBNull.Value && Convert.ToBoolean(r["IsAuthenticationRequired"]),
                AuthenticationOperationID = r["AuthenticationOperationID"] == DBNull.Value ? null : Convert.ToInt32(r["AuthenticationOperationID"]),
                TimeoutSeconds = r["TimeoutSeconds"] == DBNull.Value ? 120 : Convert.ToInt32(r["TimeoutSeconds"]),
                Inactive = r["Inactive"] != DBNull.Value && Convert.ToBoolean(r["Inactive"])
            };

            DataTable rules = DataTableHelper.getTableData("spEInvoice_RuntimeRules", [new SqlParameter("@OperationID", op.ID)]);
            foreach (DataRow x in rules.Rows)
            {
                op.ResponseRules.Add(new ProviderResponseRuleRuntimeModel
                {
                    ID = Convert.ToInt32(x["ID"]),
                    OperationID = Convert.ToInt32(x["OperationID"]),
                    RuleCode = x["RuleCode"] == DBNull.Value ? "" : Convert.ToString(x["RuleCode"])!,
                    SourcePath = x["SourcePath"] == DBNull.Value ? "" : Convert.ToString(x["SourcePath"])!,
                    Operator = x["Operator"] == DBNull.Value ? "" : Convert.ToString(x["Operator"])!,
                    ExpectedValue = x["ExpectedValue"] == DBNull.Value ? null : Convert.ToString(x["ExpectedValue"]),
                    ResultType = x["ResultType"] == DBNull.Value ? "" : Convert.ToString(x["ResultType"])!,
                    Priority = x["Priority"] == DBNull.Value ? 0 : Convert.ToInt32(x["Priority"]),
                    Inactive = x["Inactive"] != DBNull.Value && Convert.ToBoolean(x["Inactive"])
                });
            }
            return op;
        }

        private static List<ProviderMappingModel> LoadMappings(int operationId)
        {
            DataTable dt = DataTableHelper.getTableData("spEInvoice_RuntimeMappings", [new SqlParameter("@OperationID", operationId)]);
            string? S(DataRow r, string c) => r[c] == DBNull.Value ? null : Convert.ToString(r[c]);
            bool B(DataRow r, string c) => r[c] != DBNull.Value && Convert.ToBoolean(r[c]);

            var list = new List<ProviderMappingModel>();
            foreach (DataRow r in dt.Rows)
            {
                list.Add(new ProviderMappingModel
                {
                    ID = Convert.ToInt32(r["ID"]),
                    OperationID = Convert.ToInt32(r["OperationID"]),
                    MappingLocation = S(r, "MappingLocation") ?? "",
                    TargetPath = S(r, "TargetPath") ?? "",
                    SourceType = S(r, "SourceType") ?? "",
                    SourceValue = S(r, "SourceValue"),
                    DataType = S(r, "DataType") ?? "STRING",
                    IsRequired = B(r, "IsRequired"),
                    DefaultValue = S(r, "DefaultValue"),
                    FormatString = S(r, "FormatString"),
                    OmitWhenNull = B(r, "OmitWhenNull"),
                    PrefixValue = S(r, "PrefixValue"),
                    SuffixValue = S(r, "SuffixValue"),
                    DisplayOrder = r["DisplayOrder"] == DBNull.Value ? 0 : Convert.ToInt32(r["DisplayOrder"]),
                    Inactive = B(r, "Inactive"),
                    ConditionSourceType = S(r, "ConditionSourceType"),
                    ConditionSourceValue = S(r, "ConditionSourceValue"),
                    ConditionOperator = S(r, "ConditionOperator"),
                    ConditionExpectedValue = S(r, "ConditionExpectedValue")
                });
            }
            return list;
        }

        /// <summary>Giá trị cấu hình của Property theo FieldCode; field mã hóa được giải mã tại đây (chỉ dùng trong bộ nhớ).</summary>
        private static Dictionary<string, object?> LoadConfig(int propertyId, int providerId)
        {
            DataTable dt = DataTableHelper.getTableData("spEInvoice_RuntimeConfig",
                [new SqlParameter("@PropertyID", propertyId), new SqlParameter("@ProviderID", providerId)]);

            var config = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow r in dt.Rows)
            {
                string code = r["FieldCode"] == DBNull.Value ? "" : Convert.ToString(r["FieldCode"])!.Trim();
                if (code.Length == 0) continue;

                string? value = r["FieldValue"] == DBNull.Value ? null : Convert.ToString(r["FieldValue"]);
                if (value != null && Convert.ToBoolean(r["IsEncrypted"]))
                    value = EInvoiceSecret.Decrypt(value);
                config[code] = value;
            }
            return config;
        }

        private PreviousInvoiceResult? LoadPrevious(int propertyId, int folioVatId)
        {
            DataTable dt = DataTableHelper.getTableData("spEInvoice_RuntimePrevious",
                [new SqlParameter("@PropertyID", propertyId), new SqlParameter("@FolioVATID", folioVatId)]);
            if (dt.Rows.Count == 0 || dt.Rows[0][0] == DBNull.Value) return null;

            string raw = Convert.ToString(dt.Rows[0][0]) ?? "";
            return string.IsNullOrWhiteSpace(raw) ? null : _contextBuilder.ParsePreviousInvoiceResult(raw);
        }

        private static bool HasValue(Dictionary<string, object?> config, string key) =>
            config.TryGetValue(key, out object? v) && !string.IsNullOrWhiteSpace(Convert.ToString(v));

        // =====================================================================================================
        //  Che dữ liệu nhạy cảm trong bản xem trước
        // =====================================================================================================

        private static Dictionary<string, string> MaskDictionary(IDictionary<string, string> source)
        {
            return source.ToDictionary(kv => kv.Key, kv => SensitiveKey.IsMatch(kv.Key) ? "********" : kv.Value);
        }

        private static JsonNode? MaskJson(JsonNode? node)
        {
            switch (node)
            {
                case JsonObject obj:
                    var copy = new JsonObject();
                    foreach (var kv in obj)
                        copy[kv.Key] = SensitiveKey.IsMatch(kv.Key) && kv.Value is not (JsonObject or JsonArray)
                            ? JsonValue.Create("********")
                            : MaskJson(kv.Value);
                    return copy;
                case JsonArray arr:
                    var items = new JsonArray();
                    foreach (var item in arr) items.Add(MaskJson(item));
                    return items;
                default:
                    return node?.DeepClone();
            }
        }
    }
}

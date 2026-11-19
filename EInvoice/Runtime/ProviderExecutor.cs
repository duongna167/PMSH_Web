using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace EInvoice.Runtime
{
    /*
     * Thực thi một Operation: dựng request, gửi HTTP/SOAP, đánh giá Response Rule, đọc Response Mapping và ghi nhật ký thực thi.
     * Chuyển từ ProviderExecutionService của PMSH_Web, giữ nguyên logic.
     */
    public sealed class ProviderExecutionService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        private readonly IConfiguration _configuration;

        private readonly DynamicProviderDocumentBuilder _documentBuilder;

        public ProviderExecutionService(IHttpClientFactory httpClientFactory, IConfiguration configuration, DynamicProviderDocumentBuilder documentBuilder)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _configuration = configuration;
            _documentBuilder = documentBuilder ?? throw new ArgumentNullException(nameof(documentBuilder));
        }

        private sealed class ResponsePayload
        {
            public string Format { get; set; }
                = string.Empty;

            public JsonDocument? Json { get; set; }

            public XDocument? Xml { get; set; }
        }

        /// <summary>
        /// Thực thi operation kỹ thuật.
        ///
        /// Không:
        /// - Kiểm tra hóa đơn đã xử lý thành công.
        /// - Yêu cầu FolioVATID.
        /// - Parse ProviderBusinessResponse.
        /// - Parse ReserveInvoiceResult.
        /// - Ghi log cấp số hóa đơn.
        ///
        /// Dùng cho:
        /// - Login lấy token.
        /// - Lấy hash.
        /// - Lấy challenge.
        /// - Các prerequisite operation.
        /// </summary>
        public async Task<ProviderRawExecutionResult> ExecuteRawAsync(ProviderOperationRuntimeModel operation, IReadOnlyList<ProviderMappingModel> mappings, ProviderRequestContext context, CancellationToken cancellationToken = default)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            if (mappings == null)
            {
                throw new ArgumentNullException(nameof(mappings));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (operation.Inactive)
            {
                throw new ProviderBuildException($"Operation '{operation.OperationCode}' " + "đang Inactive.");
            }

            var builder = new DynamicProviderRequestBuilder(_documentBuilder);

            ProviderRequestResult request = builder.Build(operation, mappings, context);

            ProviderHttpExecutionResult httpResult = await SendAsync(operation, request, cancellationToken);

            if (!httpResult.HttpSuccess)
            {
                return new ProviderRawExecutionResult
                {
                    Success = false,

                    Message = $"Operation " + $"'{operation.OperationCode}' " + "trả về HTTP lỗi.",

                    Request = request,

                    Http = httpResult,

                    RawResponse = httpResult.RawResponse ?? string.Empty
                };
            }

            return new ProviderRawExecutionResult
            {
                Success = true,

                Message = $"Operation " + $"'{operation.OperationCode}' " + "thực hiện thành công.",

                Request = request,

                Http = httpResult,

                RawResponse = httpResult.RawResponse ?? string.Empty
            };
        }



        public async Task<ProviderExecutionResult> ExecuteAsync(
        ProviderOperationRuntimeModel operation,
        IReadOnlyList<ProviderMappingModel> mappings,
        ProviderRequestContext context,
        ProviderExecutionPersistenceContext persistence,
        CancellationToken cancellationToken = default)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            if (mappings == null)
            {
                throw new ArgumentNullException(nameof(mappings));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            if (persistence == null)
            {
                throw new ArgumentNullException(nameof(persistence));
            }

            if (persistence.PropertyID <= 0)
            {
                throw new ProviderBuildException("PropertyID dùng để lưu kết quả " + "không hợp lệ.");
            }

            string sourceType = string.IsNullOrWhiteSpace(persistence.SourceType) ? "PMS" : persistence.SourceType.Trim().ToUpperInvariant();

            if (sourceType != "PMS" && sourceType != "POS")
            {
                throw new ProviderBuildException($"SourceType không hợp lệ: {sourceType}.");
            }

            if (sourceType == "PMS" && persistence.FolioVATID <= 0)
            {
                throw new ProviderBuildException("FolioVATID dùng để lưu kết quả PMS không hợp lệ.");
            }

            if (sourceType == "POS" && persistence.SourceRecordID <= 0)
            {
                throw new ProviderBuildException("TransID dùng để lưu kết quả POS không hợp lệ.");
            }

            if (persistence.ProviderID <= 0)
            {
                throw new ProviderBuildException("ProviderID dùng để lưu kết quả " + "không hợp lệ.");
            }

            if (persistence.OperationID <= 0)
            {
                throw new ProviderBuildException("OperationID dùng để lưu kết quả " + "không hợp lệ.");
            }

            bool alreadySuccessful = await HasSuccessfulExecutionAsync(persistence, cancellationToken);

            if (alreadySuccessful)
            {
                var duplicateFailure = new ProviderExecutionResult
                {
                    Success = false,

                    Message = "Hóa đơn này đã được Provider xử lý " + "thành công trước đó. Không được cấp số lại.",

                    ProviderCode = -1,

                    RawResponse = string.Empty
                };

                return await PersistAndReturnAsync(persistence, duplicateFailure, cancellationToken);
            }

            var builder = new DynamicProviderRequestBuilder(_documentBuilder);

            ProviderRequestResult request;

            try
            {
                request = builder.Build(operation, mappings, context);
            }
            catch (Exception ex)
            {
                var buildFailure = new ProviderExecutionResult
                {
                    Success = false,
                    Message = "Không build được request Provider: " + ex.Message,
                    ProviderCode = 0,
                    RawResponse = string.Empty
                };

                return await PersistAndReturnAsync(persistence, buildFailure, cancellationToken);
            }

            ProviderHttpExecutionResult httpResult;

            try
            {
                httpResult = await SendAsync(operation, request, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                var transportFailure = new ProviderExecutionResult
                {
                    Success = false,
                    Message = "Không thể gửi request đến Provider: " + ex.Message,
                    ProviderCode = 0,
                    Request = request,
                    RawResponse = string.Empty
                };

                return await PersistAndReturnAsync(persistence, transportFailure, cancellationToken);
            }

            if (!httpResult.HttpSuccess)
            {
                var httpFailure = new ProviderExecutionResult
                {
                    Success = false,
                    Message = "Provider trả về HTTP lỗi.",
                    ProviderCode = httpResult.StatusCode,
                    Http = httpResult,
                    Request = request,
                    RawResponse = httpResult.RawResponse
                };

                return await PersistAndReturnAsync(persistence, httpFailure, cancellationToken);
            }

            ProviderDynamicResponseResult dynamicResponse;

            try
            {
                // dynamicResponse = ParseDynamicResponse(httpResult.RawResponse, mappings);

                // NormalizeVnptPublishResult(dynamicResponse);

                dynamicResponse = ParseDynamicResponse(httpResult.RawResponse, mappings);
            }
            catch (ProviderBuildException ex)
            {
                var mappingFailure = new ProviderExecutionResult
                {
                    Success = false,
                    Message = ex.Message,
                    ProviderCode = 0,
                    Http = httpResult,
                    Request = request,
                    RawResponse = httpResult.RawResponse
                };

                return await PersistAndReturnAsync(persistence, mappingFailure, cancellationToken);
            }

            bool providerSuccess;

            try
            {
                providerSuccess = EvaluateResponseRules(httpResult.RawResponse, operation.ResponseRules);
            }
            catch (ProviderBuildException ex)
            {
                var ruleFailure = new ProviderExecutionResult
                {
                    Success = false,
                    Message = ex.Message,
                    ProviderCode = 0,
                    Http = httpResult,
                    Request = request,
                    RawResponse = httpResult.RawResponse
                };

                return await PersistAndReturnAsync(persistence, ruleFailure, cancellationToken);
            }

            if (!providerSuccess)
            {
                string? errorCode = dynamicResponse.GetString("ErrorCode");

                string? errorMessage = dynamicResponse.GetString("ErrorMessage");

                int providerCode = 0;

                if (!string.IsNullOrWhiteSpace(errorCode))
                {
                    int.TryParse(errorCode, out providerCode);
                }

                var providerFailure = new ProviderExecutionResult
                {
                    Success = false,
                    Message = !string.IsNullOrWhiteSpace(errorMessage) ? errorMessage : "Provider xử lý thất bại.",
                    ProviderCode = providerCode,
                    Http = httpResult,
                    Request = request,
                    RawResponse = httpResult.RawResponse
                };

                return await PersistAndReturnAsync(persistence, providerFailure, cancellationToken);
            }

            ReserveInvoiceResult reserveResult;

            try
            {
                reserveResult = BuildReserveResult(dynamicResponse, context);
            }
            catch (ProviderBuildException ex)
            {
                var reserveFailure = new ProviderExecutionResult
                {
                    Success = false,
                    Message = ex.Message,
                    ProviderCode = 0,
                    Http = httpResult,
                    Request = request,
                    RawResponse = httpResult.RawResponse
                };

                return await PersistAndReturnAsync(persistence, reserveFailure, cancellationToken);
            }

            var successResult = new ProviderExecutionResult
            {
                Success = true,
                Message = "Cấp số và mã CQT thành công.",
                ProviderCode = 0,
                Http = httpResult,
                Request = request,
                ReserveResult = reserveResult,
                RawResponse = httpResult.RawResponse
            };

            return await PersistAndReturnAsync(persistence, successResult, cancellationToken);
        }

        private async Task<ProviderExecutionResult> PersistAndReturnAsync(ProviderExecutionPersistenceContext persistence, ProviderExecutionResult result, CancellationToken cancellationToken)
        {
            try
            {
                await SaveExecutionLogAsync(persistence, result, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
            }

            return result;
        }

        private async Task<ProviderHttpExecutionResult> SendAsync(ProviderOperationRuntimeModel operation, ProviderRequestResult requestData, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(requestData.Url))
            {
                throw new ProviderBuildException("Provider request URL đang trống.");
            }

            string method = string.IsNullOrWhiteSpace(requestData.HttpMethod) ? "POST" : requestData.HttpMethod
                        .Trim()
                        .ToUpperInvariant();

            using var httpRequest = new HttpRequestMessage(new HttpMethod(method), requestData.Url);

            bool canHaveBody = method != "GET" && method != "HEAD";

            if (canHaveBody)
            {
                string bodyType = string.IsNullOrWhiteSpace(requestData.BodyType) ? "JSON" : requestData.BodyType.Trim().ToUpperInvariant();

                string body;

                if (bodyType == "SOAP11")
                {
                    body = BuildSoap11Envelope(requestData);
                }
                else
                {
                    body = requestData.Body?.ToJsonString(new JsonSerializerOptions
                    {
                        WriteIndented = false
                    }) ?? "{}";
                }

                string contentType = string.IsNullOrWhiteSpace(operation.ContentType) ? "application/json" : operation.ContentType.Trim();

                httpRequest.Content = new StringContent(body, Encoding.UTF8, contentType);

                if (bodyType == "SOAP11")
                {
                    string soapAction = BuildSoapAction(requestData);

                    httpRequest.Headers.TryAddWithoutValidation("SOAPAction", "\"" + soapAction + "\"");

                }
            }

            foreach (KeyValuePair<string, string> header in requestData.Headers)
            {
                if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                bool added = httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);

                if (!added && httpRequest.Content != null)
                {
                    httpRequest.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            int timeoutSeconds = operation.TimeoutSeconds > 0 ? operation.TimeoutSeconds : 120;

            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            timeoutSource.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            Stopwatch stopwatch = Stopwatch.StartNew();

            try
            {
                HttpClient client = _httpClientFactory.CreateClient();
                client.Timeout = Timeout.InfiniteTimeSpan; // thời gian chờ do CancellationTokenSource theo TimeoutSeconds của Operation quyết định
                using HttpResponseMessage response = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseContentRead, timeoutSource.Token);

                string raw = await response.Content.ReadAsStringAsync();

                stopwatch.Stop();

                return new ProviderHttpExecutionResult
                {
                    HttpSuccess = response.IsSuccessStatusCode,

                    StatusCode = (int)response.StatusCode,

                    ReasonPhrase = response.ReasonPhrase ?? string.Empty,

                    ResponseContentType = response.Content.Headers.ContentType?.ToString() ?? string.Empty,

                    RawResponse = raw,

                    ElapsedMilliseconds = stopwatch.ElapsedMilliseconds
                };
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();

                throw new ProviderBuildException($"Provider request timeout sau " + $"{timeoutSeconds} giây.");
            }
            catch (HttpRequestException ex)
            {
                stopwatch.Stop();

                throw new ProviderBuildException("Không thể kết nối Provider: " + ex.Message, ex);
            }
        }

        private static string BuildSoapAction(ProviderRequestResult requestData)
        {
            string namespaceValue = string.IsNullOrWhiteSpace(requestData.ExternalMethodCode) ? "http://tempuri.org/" : requestData.ExternalMethodCode.Trim();

            if (!namespaceValue.EndsWith("/"))
            {
                namespaceValue += "/";
            }

            if (string.IsNullOrWhiteSpace(requestData.ActionCode))
            {
                throw new ProviderBuildException("SOAP ActionCode đang trống.");
            }

            return namespaceValue + requestData.ActionCode.Trim();
        }

        private static string BuildSoap11Envelope(ProviderRequestResult requestData)
        {
            if (string.IsNullOrWhiteSpace(requestData.ActionCode
            ))
            {
                throw new ProviderBuildException("SOAP ActionCode đang trống.");
            }

            string namespaceValue = string.IsNullOrWhiteSpace(requestData.ExternalMethodCode) ? "http://tempuri.org/" : requestData.ExternalMethodCode.Trim();

            if (!namespaceValue.EndsWith("/"))
            {
                namespaceValue += "/";
            }

            string actionCode = requestData.ActionCode.Trim();

            XElement methodElement = new XElement(XName.Get(actionCode, namespaceValue));

            if (requestData.Body != null)
            {
                foreach (KeyValuePair<string, JsonNode?> item in requestData.Body)
                {
                    string value = item.Value == null ? string.Empty : GetJsonNodeText(item.Value);

                    methodElement.Add(new XElement(XName.Get(item.Key, namespaceValue), value));
                }
            }

            XNamespace soap = "http://schemas.xmlsoap.org/soap/envelope/";

            XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";

            XNamespace xsd = "http://www.w3.org/2001/XMLSchema";

            XDocument document =
                    new XDocument(new XDeclaration("1.0", "utf-8", null),
                        new XElement(soap + "Envelope",

                        new XAttribute(XNamespace.Xmlns + "xsi", xsi),

                        new XAttribute(XNamespace.Xmlns + "xsd", xsd),

                        new XAttribute(XNamespace.Xmlns + "soap", soap),

                        new XElement(soap + "Body", methodElement)
                        )
                    );

            return document.ToString(SaveOptions.DisableFormatting
            );
        }

        private static string GetJsonNodeText(JsonNode node)
        {
            if (node is JsonValue value)
            {
                if (value.TryGetValue<string>(out string? stringValue))
                {
                    return stringValue ?? string.Empty;
                }

                if (value.TryGetValue<int>(out int intValue))
                {
                    return intValue.ToString();
                }

                if (value.TryGetValue<long>(out long longValue))
                {
                    return longValue.ToString();
                }

                if (value.TryGetValue<decimal>(out decimal decimalValue))
                {
                    return decimalValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
                }

                if (value.TryGetValue<bool>(
                    out bool boolValue))
                {
                    return boolValue ? "true" : "false";
                }
            }

            return node.ToJsonString().Trim('"');
        }

        private static object? GetJsonValueByPath(JsonElement root, string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            string[] parts = path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            JsonElement current = root;

            foreach (string part in parts)
            {
                /*
                 * ==================================================
                 * 1. Nếu current đang là STRING chứa JSON
                 *    thì parse tiếp JSON bên trong.
                 * ==================================================
                 */
                current = ExpandJsonStringIfNeeded(current);

                /*
                 * ==================================================
                 * 2. Parse segment:
                 *
                 * result
                 * Message[0]
                 * items[1]
                 * ==================================================
                 */
                ParsePathSegment(part, out string propertyName, out int? arrayIndex);


                /*
                 * ==================================================
                 * 3. Đọc property nếu segment có property name
                 * ==================================================
                 */
                if (!string.IsNullOrWhiteSpace(propertyName))
                {
                    current = ExpandJsonStringIfNeeded(current);

                    if (current.ValueKind != JsonValueKind.Object)
                    {
                        return null;
                    }

                    if (!TryGetPropertyIgnoreCase(current, propertyName, out JsonElement propertyValue))
                    {
                        return null;
                    }

                    current = propertyValue;
                }


                /*
                 * ==================================================
                 * 4. Nếu có [index] thì đọc phần tử array
                 * ==================================================
                 */
                if (arrayIndex.HasValue)
                {
                    /*
                     * Property vừa lấy có thể là:
                     *
                     * "[{...}]"
                     *
                     * tức JSON STRING.
                     *
                     * Parse nó thành JsonElement array trước.
                     */
                    current = ExpandJsonStringIfNeeded(current);

                    if (current.ValueKind != JsonValueKind.Array)
                    {
                        return null;
                    }

                    int index = arrayIndex.Value;

                    if (index < 0)
                    {
                        return null;
                    }

                    int currentIndex = 0;

                    bool found = false;

                    JsonElement selected = default;

                    foreach (JsonElement item in current.EnumerateArray())
                    {
                        if (currentIndex == index)
                        {
                            selected = item;

                            found = true;

                            break;
                        }

                        currentIndex++;
                    }

                    if (!found)
                    {
                        return null;
                    }

                    current = selected;
                }
            }

            /*
             * Nếu value cuối cùng vẫn là JSON string chứa JSON,
             * ta không cần expand nữa trừ khi caller muốn object.
             *
             * Với InvoiceNo thì current sẽ là STRING "237".
             */
            return ConvertJsonElementToObject(current);
        }

        private static bool TryGetPropertyIgnoreCase(JsonElement element, string propertyName, out JsonElement value)
        {
            /*
             * Thử đúng case trước.
             */
            if (element.TryGetProperty(propertyName, out value))
            {
                return true;
            }

            /*
             * Fallback case-insensitive.
             */
            foreach (JsonProperty property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;

                    return true;
                }
            }

            value = default;

            return false;
        }

        private static object? ConvertJsonElementToObject(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Null => null,

                JsonValueKind.Undefined => null,

                JsonValueKind.String => element.GetString(),

                JsonValueKind.True => true,

                JsonValueKind.False => false,

                JsonValueKind.Number => element.TryGetInt64(out long longValue) ? longValue : element.TryGetDecimal(out decimal decimalValue) ? decimalValue : element.GetDouble(),

                JsonValueKind.Object => element.GetRawText(),

                JsonValueKind.Array => element.GetRawText(),

                _ => element.GetRawText()
            };
        }

        private static void ParsePathSegment(string segment, out string propertyName, out int? arrayIndex)
        {
            propertyName = segment.Trim();

            arrayIndex = null;

            int openBracket = propertyName.IndexOf('[');

            if (openBracket < 0)
            {
                return;
            }

            int closeBracket = propertyName.IndexOf(']', openBracket + 1);

            if (closeBracket < 0)
            {
                throw new ProviderBuildException($"Response path segment '{segment}' " + "không hợp lệ.");
            }

            string indexText = propertyName.Substring(openBracket + 1, closeBracket - openBracket - 1);

            if (!int.TryParse(indexText, out int index))
            {
                throw new ProviderBuildException($"Array index '{indexText}' " + $"trong response path '{segment}' " + "không hợp lệ.");
            }

            propertyName = propertyName.Substring(0, openBracket).Trim();

            arrayIndex = index;
        }

        private static JsonElement ExpandJsonStringIfNeeded(JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.String)
            {
                return element;
            }

            string? text = element.GetString();

            if (string.IsNullOrWhiteSpace(text))
            {
                return element;
            }

            text = text.Trim();

            /*
             * Chỉ thử parse nếu nhìn giống JSON object/array.
             */
            bool looksLikeJson = (text.StartsWith("{") && text.EndsWith("}")) || (text.StartsWith("[") && text.EndsWith("]"));

            if (!looksLikeJson)
            {
                return element;
            }

            try
            {
                using JsonDocument nestedDocument = JsonDocument.Parse(text);

                /*
                 * Clone bắt buộc vì JsonDocument sẽ Dispose
                 * sau khi rời method.
                 */
                return nestedDocument.RootElement.Clone();
            }
            catch (JsonException)
            {
                /*
                 * String nhìn giống JSON nhưng parse không được:
                 * coi nó như string bình thường.
                 */
                return element;
            }
        }

        private static bool EvaluateSingleResponseRule( object? actualValue, string? ruleOperator, string? expectedValue)
        {
            string op = (ruleOperator ?? string.Empty).Trim().ToUpperInvariant();

            string? actualText = actualValue == null ? null : Convert.ToString(actualValue, CultureInfo.InvariantCulture);

            string actual = actualText?.Trim() ?? string.Empty;

            string expected = expectedValue?.Trim() ?? string.Empty;

            return op switch
            {
                "IS_NULL" => actualValue == null,

                "IS_NOT_NULL" => actualValue != null,

                "EMPTY" => actualValue == null || string.IsNullOrWhiteSpace(actualText),

                "NOT_EMPTY" => actualValue != null && !string.IsNullOrWhiteSpace(actualText),

                "EQUALS" => string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase),

                "NOT_EQUALS" => !string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase),

                "STARTS_WITH" => actual.StartsWith(expected, StringComparison.OrdinalIgnoreCase),

                "NOT_STARTS_WITH" => !actual.StartsWith(expected, StringComparison.OrdinalIgnoreCase),

                "CONTAINS" => actual.Contains(expected, StringComparison.OrdinalIgnoreCase),

                "NOT_CONTAINS" => !actual.Contains(expected, StringComparison.OrdinalIgnoreCase),

                _ => throw new ProviderBuildException($"Response Rule Operator " + $"'{ruleOperator}' " + "không được hỗ trợ.")
            };
        }

        private static bool EvaluateResponseRules(string rawResponse, IReadOnlyList<ProviderResponseRuleRuntimeModel> rules)
        {
            if (string.IsNullOrWhiteSpace(rawResponse))
            {
                throw new ProviderBuildException("Response Provider rỗng.");
            }

            if (rules == null || rules.Count == 0)
            {
                throw new ProviderBuildException("Operation chưa cấu hình Response Rule.");
            }

            ResponsePayload payload = ParseResponsePayload(rawResponse);


            /*
             * =====================================================
             * FAILURE RULE
             * =====================================================
             */
            List<ProviderResponseRuleRuntimeModel> failureRules = rules
                        .Where(x =>!x.Inactive && string.Equals(x.ResultType, "FAILURE", StringComparison.OrdinalIgnoreCase))
                        .OrderBy(x => x.Priority)
                        .ThenBy(x => x.ID)
                        .ToList();

            foreach (ProviderResponseRuleRuntimeModel rule in failureRules)
            {
                object? actual = ResolveResponseValue(payload, rule.SourcePath);

                if (EvaluateSingleResponseRule(actual, rule.Operator, rule.ExpectedValue))
                {
                    return false;
                }
            }


            /*
             * =====================================================
             * SUCCESS RULE
             * =====================================================
             */
            List<ProviderResponseRuleRuntimeModel> successRules = rules
                                                                    .Where(x =>!x.Inactive && string.Equals(x.ResultType, "SUCCESS", StringComparison.OrdinalIgnoreCase))
                                                                    .OrderBy(x => x.Priority)
                                                                    .ThenBy(x => x.ID)
                                                                    .ToList();

            if (successRules.Count == 0)
            {
                throw new ProviderBuildException("Operation chưa cấu hình SUCCESS Response Rule.");
            }


            /*
             * Tất cả SUCCESS rules phải match.
             */
            foreach (ProviderResponseRuleRuntimeModel rule in successRules)
            {
                object? actual = ResolveResponseValue(payload, rule.SourcePath);

                bool matched = EvaluateSingleResponseRule(actual, rule.Operator, rule.ExpectedValue);

                if (!matched)
                {
                    return false;
                }
            }

            return true;
        }

        private static ReserveInvoiceResult BuildReserveResult(ProviderDynamicResponseResult response, ProviderRequestContext context)
        {
            string? invoiceNo = response.GetString("InvoiceNo");

            if (string.IsNullOrWhiteSpace(invoiceNo))
            {
                throw new ProviderBuildException("Provider báo thành công nhưng " + "Response Mapping không trả InvoiceNo.");
            }

            string? transactionID = response.GetString("TransactionID");

            string? reservationCode = response.GetString("ReservationCode");


            var result = new ReserveInvoiceResult
            {
                InvoiceNo = invoiceNo,

                VerificationCode = response.GetString("VerificationCode"),

                /*
                 * Ưu tiên canonical mapping.
                 *
                 * Không phụ thuộc tên Provider.
                 */
                Key = response.GetString("Key") ?? reservationCode ?? transactionID,

                Pattern = response.GetString("Pattern") ?? GetDictionaryString(context.Config, "TEMPLATE_CODE"),

                Serial = response.GetString("Serial") ?? GetDictionaryString(context.Config, "INVOICE_SERIES"),

                InvoiceDate = response.GetString("InvoiceDate") ?? GetInvoiceDateString(context.Invoice),

                KeySearch = response.GetString("KeySearch") ?? transactionID
            };

            return result;
        }

        private static string? GetDictionaryString(IDictionary<string, object?> dictionary, string key)
        {
            if (dictionary == null)
            {
                return null;
            }

            if (!dictionary.TryGetValue(key, out object? value))
            {
                return null;
            }

            return value?.ToString();
        }

        private static string? GetInvoiceDateString(object? invoice)
        {
            if (invoice == null)
            {
                return null;
            }

            var property = invoice.GetType().GetProperty("InvoiceDate");

            object? value = property?.GetValue(invoice);

            if (value == null)
            {
                return null;
            }

            if (value is DateTime dateTime)
            {
                return dateTime.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
            }

            if (DateTime.TryParse(value.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsed))
            {
                return parsed.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
            }

            return value.ToString();
        }

        private static ProviderBusinessResponse? ParseBusinessResponse(string rawResponse)
        {
            if (string.IsNullOrWhiteSpace(rawResponse))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<ProviderBusinessResponse>(rawResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }
                );
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static ReserveInvoiceResult? ParseReserveResult(string? message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return null;
            }

            try
            {
                List<ReserveInvoiceResult>? results = JsonSerializer.Deserialize<List<ReserveInvoiceResult>>(message, new JsonSerializerOptions {PropertyNameCaseInsensitive = true});

                return results?.FirstOrDefault();
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static object ToDbValue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return DBNull.Value;
            }

            return value.Trim();
        }

        private static object ToProviderDateDbValue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return DBNull.Value;
            }

            bool parsed = DateTime.TryParseExact(value.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result);

            if (!parsed)
            {
                return DBNull.Value;
            }

            return result.Date;
        }

        private static ProviderDynamicResponseResult ParseDynamicResponse(string rawResponse, IEnumerable<ProviderMappingModel> mappings)
        {
            if (string.IsNullOrWhiteSpace(rawResponse))
            {
                throw new ProviderBuildException("Response Provider rỗng.");
            }

            var result = new ProviderDynamicResponseResult();

            var responseMappings = mappings
                    .Where(x => !x.Inactive && string.Equals(x.MappingLocation, "RESPONSE", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(x => x.DisplayOrder)
                    .ThenBy(x => x.ID)
                    .ToList();



            foreach (ProviderMappingModel mapping in responseMappings)
            {
            }


            /*
             * Không có RESPONSE mapping:
             * không ép parse JSON/XML.
             */
            if (responseMappings.Count == 0)
            {
                return result;
            }

            ResponsePayload payload = ParseResponsePayload(rawResponse);

            foreach (ProviderMappingModel mapping in responseMappings)
            {
                /*
                 * Bước 1:
                 * Lấy giá trị gốc từ JSON/XML như logic hiện tại.
                 */
                object? sourceValue = ResolveResponseValue(payload, mapping.SourceValue);

                /*
                 * Bước 2:
                 * Chỉ transform nếu FormatString bắt đầu bằng REGEX:.
                 *
                 * Những mapping cũ không có REGEX:
                 * sẽ được trả về nguyên trạng.
                 */
                object? mappedValue = ApplyResponseMappingTransform(sourceValue, mapping);


                result.Values[mapping.TargetPath] = mappedValue;
            }

            return result;
        }

        private static object? ApplyResponseMappingTransform(object? sourceValue, ProviderMappingModel mapping)
        {
            if (mapping == null)
            {
                return sourceValue;
            }

            string formatString = mapping.FormatString?.Trim() ?? string.Empty;

            /*
             * Tương thích ngược tuyệt đối:
             *
             * Mapping không có prefix REGEX:
             * sẽ giữ nguyên hành vi hiện tại.
             */
            if (!formatString.StartsWith("REGEX:", StringComparison.OrdinalIgnoreCase))
            {
                return sourceValue;
            }

            string pattern = formatString["REGEX:".Length..].Trim();

            if (string.IsNullOrWhiteSpace(pattern))
            {
                throw new ProviderBuildException($"RESPONSE mapping '{mapping.TargetPath}' " + "có FormatString REGEX nhưng không có pattern.");
            }

            string input = Convert.ToString(sourceValue, CultureInfo.InvariantCulture) ?? string.Empty;

            if (string.IsNullOrWhiteSpace(input))
            {
                return ResolveMissingTransformedValue(mapping, "Giá trị nguồn rỗng.");
            }

            Match match;

            try
            {
                match = Regex.Match(input.Trim(), pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
            }
            catch (ArgumentException ex)
            {
                throw new ProviderBuildException($"Regex của RESPONSE mapping " + $"'{mapping.TargetPath}' không hợp lệ.", ex);
            }
            catch (RegexMatchTimeoutException ex)
            {
                throw new ProviderBuildException($"Regex của RESPONSE mapping " + $"'{mapping.TargetPath}' xử lý quá thời gian.", ex);
            }

            if (!match.Success)
            {
                return ResolveMissingTransformedValue(mapping, "Giá trị Provider không khớp regex.");
            }

            /*
             * Quy ước:
             * tên named group phải giống TargetPath.
             *
             * TargetPath = InvoiceNo
             * Regex      = (?<InvoiceNo>...)
             */
            Group group = match.Groups[mapping.TargetPath];

            if (!group.Success || string.IsNullOrWhiteSpace(group.Value))
            {
                return ResolveMissingTransformedValue(mapping, $"Không tìm thấy regex group " + $"'{mapping.TargetPath}'.");
            }

            return group.Value.Trim();
        }

        private static object? ResolveMissingTransformedValue( ProviderMappingModel mapping, string reason)
        {
            if (!string.IsNullOrWhiteSpace(mapping.DefaultValue))
            {
                return mapping.DefaultValue.Trim();
            }

            if (mapping.IsRequired)
            {
                throw new ProviderBuildException($"Không thể tạo RESPONSE field " + $"'{mapping.TargetPath}'. {reason}");
            }

            return null;
        }

        private static ResponsePayload ParseResponsePayload(string rawResponse)
        {
            if (string.IsNullOrWhiteSpace(rawResponse))
            {
                throw new ProviderBuildException("Response Provider rỗng.");
            }

            string text = rawResponse.Trim();


            /*
             * =====================================================
             * JSON
             * =====================================================
             */
            if (text.StartsWith("{") || text.StartsWith("["))
            {
                try
                {
                    JsonDocument json = JsonDocument.Parse(text);

                    return new ResponsePayload
                    {
                        Format = "JSON",
                        Json = json
                    };
                }
                catch (JsonException ex)
                {
                    throw new ProviderBuildException("Response JSON của Provider " + "không hợp lệ.", ex);
                }
            }


            /*
             * =====================================================
             * XML / SOAP
             * =====================================================
             */
            if (text.StartsWith("<"))
            {
                try
                {
                    XDocument xml = XDocument.Parse(text, LoadOptions.PreserveWhitespace);

                    return new ResponsePayload
                    {
                        Format = "XML", Xml = xml
                    };
                }
                catch (Exception ex)
                {
                    throw new ProviderBuildException("Response XML/SOAP của Provider " + "không hợp lệ.", ex);
                }
            }


            throw new ProviderBuildException("Không xác định được định dạng " + "Response Provider.");
        }

        private static object? ResolveResponseValue(ResponsePayload payload, string? sourcePath)
        {
            if (payload == null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                return null;
            }

            if (payload.Format == "JSON")
            {
                if (payload.Json == null)
                {
                    return null;
                }

                return GetJsonValueByPath(payload.Json.RootElement, sourcePath);
            }

            if (payload.Format == "XML")
            {
                if (payload.Xml == null)
                {
                    return null;
                }

                return GetXmlValueByPath(payload.Xml, sourcePath);
            }

            return null;
        }

        private static object? GetXmlValueByPath(XDocument document, string? sourcePath)
        {
            if (document.Root == null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                return null;
            }

            string path = sourcePath.Trim();

            /*
             * Cho phép:
             *
             * Envelope/Body/...
             *
             * hoặc:
             *
             * soap:Envelope/soap:Body/...
             */
            string[] parts = path.Split(new[] { '/', '.' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parts.Length == 0)
            {
                return null;
            }

            IEnumerable<XElement> current = new[] {document.Root};

            int startIndex = 0;

            string firstPart = RemoveXmlNamespacePrefix(parts[0]);

            /*
             * Root hiện đã là Envelope
             * thì không tìm Envelope bên dưới Envelope nữa.
             */
            if (string.Equals(document.Root.Name.LocalName, firstPart, StringComparison.OrdinalIgnoreCase))
            {
                startIndex = 1;
            }

            for (int i = startIndex; i < parts.Length; i++)
            {
                string part = RemoveXmlNamespacePrefix(parts[i]);

                current = current
                        .SelectMany(x => x.Elements())
                        .Where(x => string.Equals(x.Name.LocalName, part, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                if (!current.Any())
                {
                    return null;
                }
            }

            XElement? element = current.FirstOrDefault();

            return element?.Value;
        }

        private static string RemoveXmlNamespacePrefix( string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string text = value.Trim();

            int colonIndex = text.IndexOf(':');

            if (colonIndex >= 0 && colonIndex < text.Length - 1)
            {
                return text[(colonIndex + 1)..];
            }

            return text;
        }

        private static bool IsNullOrEmptyResponseValue(object? value)
        {
            if (value == null)
                return true;

            if (value is string text)
                return string.IsNullOrWhiteSpace(text);

            return false;
        }

        private async Task SaveExecutionLogAsync(ProviderExecutionPersistenceContext persistence, ProviderExecutionResult result, CancellationToken cancellationToken)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection") ?? throw new ProviderBuildException("Không tìm thấy DefaultConnection " + "để ghi Provider log.");

            ReserveInvoiceResult? reserve = result.ReserveResult;

            const string sql = @"
                INSERT INTO dbo.EInvoiceProviderExecutionLog
                (
                    PropertyID,
                    FolioVATID,
                    SourceType,
                    SourceRecordID,
                    ProviderID,
                    OperationID,
                    OperationCode,

                    ProviderKey,
                    Pattern,
                    Serial,
                    InvoiceNo,
                    InvoiceDate,
                    VerificationCode,
                    KeySearch,

                    HttpSuccess,
                    HttpStatusCode,
                    ProviderSuccess,
                    ProviderCode,

                    RequestUrl,
                    RequestBody,
                    RawResponse,
                    ElapsedMilliseconds,
                    ErrorMessage,

                    CreatedBy,
                    CreatedDate
                )
                VALUES
                (
                    @PropertyID,
                    @FolioVATID,
                    @SourceType,
                    @SourceRecordID,
                    @ProviderID,
                    @OperationID,
                    @OperationCode,

                    @ProviderKey,
                    @Pattern,
                    @Serial,
                    @InvoiceNo,
                    @InvoiceDate,
                    @VerificationCode,
                    @KeySearch,

                    @HttpSuccess,
                    @HttpStatusCode,
                    @ProviderSuccess,
                    @ProviderCode,

                    @RequestUrl,
                    @RequestBody,
                    @RawResponse,
                    @ElapsedMilliseconds,
                    @ErrorMessage,

                    @CreatedBy,
                    SYSDATETIME()
                );
            ";

            await using var connection = new SqlConnection(connectionString);

            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(sql, connection);

            command.Parameters.Add("@PropertyID", SqlDbType.Int).Value = persistence.PropertyID;

            command.Parameters.Add("@FolioVATID", SqlDbType.Int).Value = persistence.FolioVATID > 0 ? persistence.FolioVATID : DBNull.Value;

            string sourceType = string.IsNullOrWhiteSpace(persistence.SourceType) ? "PMS" : persistence.SourceType.Trim().ToUpperInvariant();

            command.Parameters.Add("@SourceType", SqlDbType.VarChar, 10).Value = sourceType;

            command.Parameters.Add("@SourceRecordID", SqlDbType.Int).Value = persistence.SourceRecordID;

            command.Parameters.Add("@ProviderID", SqlDbType.Int).Value = persistence.ProviderID > 0 ? persistence.ProviderID : DBNull.Value;

            command.Parameters.Add("@OperationID", SqlDbType.Int).Value = persistence.OperationID > 0 ? persistence.OperationID : DBNull.Value;

            command.Parameters.Add("@OperationCode", SqlDbType.NVarChar, 100).Value = string.IsNullOrWhiteSpace(persistence.OperationCode) ? string.Empty : persistence.OperationCode.Trim();

            command.Parameters.Add("@ProviderKey", SqlDbType.NVarChar, 100).Value = ToDbValue(reserve?.Key);

            command.Parameters.Add("@Pattern", SqlDbType.NVarChar, 50).Value = ToDbValue(reserve?.Pattern);

            command.Parameters.Add("@Serial", SqlDbType.NVarChar, 50).Value = ToDbValue(reserve?.Serial);

            command.Parameters.Add("@InvoiceNo", SqlDbType.NVarChar, 50).Value = ToDbValue(reserve?.InvoiceNo);

            command.Parameters.Add("@InvoiceDate", SqlDbType.Date).Value = reserve == null ? DBNull.Value : ToProviderDateDbValue(reserve.InvoiceDate);

            command.Parameters.Add("@VerificationCode", SqlDbType.NVarChar, 200).Value = ToDbValue(reserve?.VerificationCode);

            command.Parameters.Add("@KeySearch", SqlDbType.NVarChar, 200).Value = ToDbValue(reserve?.KeySearch);

            command.Parameters.Add("@HttpSuccess", SqlDbType.Bit).Value = result.Http?.HttpSuccess ?? result.Success;

            command.Parameters.Add("@HttpStatusCode", SqlDbType.Int).Value = result.Http?.StatusCode ?? (result.Success ? 200 : 0);

            command.Parameters.Add("@ProviderSuccess", SqlDbType.Bit).Value = result.Success;

            command.Parameters.Add("@ProviderCode", SqlDbType.Int).Value = result.ProviderCode;

            command.Parameters.Add("@RequestUrl", SqlDbType.NVarChar, 1000).Value = ToDbValue(result.Request?.Url);

            string requestBody = result.Request?.Body?.ToJsonString() ?? "{}";

            command.Parameters.Add("@RequestBody", SqlDbType.NVarChar, -1).Value = requestBody;

            command.Parameters.Add("@RawResponse", SqlDbType.NVarChar, -1).Value = ToDbValue(result.RawResponse);

            command.Parameters.Add("@ElapsedMilliseconds", SqlDbType.BigInt).Value = result.Http?.ElapsedMilliseconds ?? 0;

            // Lưu cả thông báo thành công lẫn thất bại để View Logs hiển thị
            // đúng nội dung mà luồng thực thi đã trả về cho người dùng.
            command.Parameters.Add("@ErrorMessage", SqlDbType.NVarChar, 2000).Value = ToDbValue(result.Message);

            command.Parameters.Add("@CreatedBy", SqlDbType.NVarChar, 100).Value = ToDbValue(persistence.ExecutedBy);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task SaveTestExecutionLogAsync(ProviderExecutionPersistenceContext persistence, ProviderExecutionResult result, CancellationToken cancellationToken = default)
        {
            if (persistence == null)
            {
                throw new ArgumentNullException(nameof(persistence));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            await SaveExecutionLogAsync(persistence, result, cancellationToken);
        }

        public async Task SaveFailureLogAsync(ProviderExecutionPersistenceContext persistence, string errorMessage, CancellationToken cancellationToken = default)
        {
            if (persistence == null)
            {
                return;
            }

            var failureResult = new ProviderExecutionResult
            {
                Success = false,
                Message = string.IsNullOrWhiteSpace(errorMessage) ? "Không thể chuẩn bị dữ liệu gửi Provider." : errorMessage.Trim(),
                ProviderCode = 0,
                Request = new ProviderRequestResult
                {
                    OperationID = persistence.OperationID,
                    OperationCode = persistence.OperationCode,
                    ActionCode = persistence.OperationCode,
                    Body = new JsonObject
                    {
                        ["actionCode"] = persistence.OperationCode,
                        ["propertyID"] = persistence.PropertyID,
                        ["sourceType"] = persistence.SourceType,
                        ["sourceRecordID"] = persistence.SourceRecordID
                    }
                },
                RawResponse = string.Empty
            };

            try
            {
                await SaveExecutionLogAsync(persistence, failureResult, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
            }
        }

        private async Task<bool> HasSuccessfulExecutionAsync(ProviderExecutionPersistenceContext persistence, CancellationToken cancellationToken)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection") ?? throw new ProviderBuildException("Không tìm thấy DefaultConnection.");

            string sourceType = string.IsNullOrWhiteSpace(persistence.SourceType) ? "PMS" : persistence.SourceType.Trim().ToUpperInvariant();

            const string sql = @"
                SELECT TOP 1 1
                FROM dbo.EInvoiceProviderExecutionLog
                WHERE PropertyID = @PropertyID

                  AND SourceType = @SourceType

                  AND SourceRecordID = @SourceRecordID

                  AND ProviderID = @ProviderID

                  AND OperationID = @OperationID

                  AND ProviderSuccess = 1;
            ";

            await using var connection = new SqlConnection(connectionString);

            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(sql, connection);

            command.Parameters.Add("@PropertyID", SqlDbType.Int).Value = persistence.PropertyID;

            command.Parameters.Add("@SourceType", SqlDbType.VarChar, 10).Value = sourceType;

            command.Parameters.Add("@SourceRecordID", SqlDbType.Int).Value = persistence.SourceRecordID;

            command.Parameters.Add("@ProviderID", SqlDbType.Int).Value = persistence.ProviderID;

            command.Parameters.Add("@OperationID", SqlDbType.Int).Value = persistence.OperationID;

            object? result = await command.ExecuteScalarAsync(cancellationToken);

            return result != null && result != DBNull.Value;
        }
    }
}

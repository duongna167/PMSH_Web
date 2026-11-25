using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace EInvoice.Runtime
{
    /*
     * Các model runtime của engine gọi nhà cung cấp hóa đơn điện tử (context, kết quả dựng request,
     * kết quả thực thi, cấu hình Operation/Mapping/Response Rule). Chuyển từ PMSH_Web, giữ nguyên nội dung.
     */
    // ---- ProviderBuildException ----
    public class ProviderBuildException : Exception
    {
        public ProviderBuildException(string message) : base(message)
        {
        }

        public ProviderBuildException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    // ---- ProviderRequestContext ----
    public class ProviderRequestContext
    {
        /// <summary>
        /// Dữ liệu header hóa đơn.
        /// SourceType = INVOICE sẽ đọc từ đây.
        /// </summary>
        public object? Invoice { get; set; }

        /// <summary>
        /// Dữ liệu chi tiết hóa đơn.
        /// SourceType = INVOICEDETAIL sẽ đọc từ đây.
        /// Giữ lại để tương thích code cũ.
        /// </summary>
        public object? InvoiceDetail { get; set; }

        /// <summary>
        /// Các collection động dùng cho SourceType = COLLECTION.
        ///
        /// Ví dụ:
        /// Collections["Items"] = invoiceDetails;
        /// Collections["TaxBreakdowns"] = taxBreakdowns;
        /// Collections["Payments"] = payments;
        ///
        /// Builder sẽ đọc theo SourceValue:
        /// COLLECTION:Items.ItemName
        /// COLLECTION:TaxBreakdowns.TaxAmount
        /// </summary>
        public Dictionary<string, object?> Collections { get; set; }
            = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Thông tin property/khách sạn.
        /// SourceType = PROPERTY sẽ đọc từ đây.
        /// </summary>
        public object? Property { get; set; }

        /// <summary>
        /// Cấu hình provider theo property.
        /// Ví dụ API_KEY, USERNAME, PASSWORD, TAX_CODE.
        /// </summary>
        public Dictionary<string, object?> Config { get; set; }
            = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Dữ liệu session của người dùng đang đăng nhập.
        /// </summary>
        public Dictionary<string, object?> Session { get; set; }
            = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Dữ liệu hệ thống như CurrentDate, CurrentDateTime...
        /// </summary>
        public Dictionary<string, object?> System { get; set; }
            = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Response của operation trước.
        /// Dùng khi SourceType = RESPONSE.
        /// </summary>
        public object? PreviousResponse { get; set; }
    }

    // ---- ProviderRequestResult ----
    public class ProviderRequestResult
    {
        public int OperationID { get; set; }

        public string OperationCode { get; set; } = string.Empty;

        public string HttpMethod { get; set; } = "POST";

        public string BaseUrl { get; set; } = string.Empty;

        public string Endpoint { get; set; } = string.Empty;

        public string Url { get; set; } = string.Empty;

        public string BodyType { get; set; } = "JSON";

        public string ActionCode { get; set; } = string.Empty;

        public string ExternalMethodCode { get; set; } = string.Empty;

        public Dictionary<string, string> Headers { get; set; }
            = new(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, string> QueryParameters { get; set; }
            = new(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, string> PathParameters { get; set; }
            = new(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, string> FormFields { get; set; }
            = new(StringComparer.OrdinalIgnoreCase);

        public JsonObject Body { get; set; } = new();

        public List<string> AppliedMappings { get; set; } = new();

        public List<string> SkippedMappings { get; set; } = new();

        public List<string> Warnings { get; set; } = new();
    }

    // ---- ProviderInvoiceDetailRuntimeModel ----
    public sealed class ProviderInvoiceDetailRuntimeModel
    {

        public int ItemType { get; set; } = 1;
        
        public int LineNumber { get; set; }

        public int ID { get; set; }

        public int FolioDetailID { get; set; }

        public string ItemCode { get; set; }
            = string.Empty;

        public string ItemName { get; set; }
            = string.Empty;

        public string UnitName { get; set; }
            = string.Empty;

        public decimal Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal AmountBeforeTax { get; set; }

        public decimal VATRate { get; set; }

        public decimal TaxAmount { get; set; }

        public decimal DiscountRate { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal TotalAmount { get; set; }

        public string Supplement { get; set; }
            = string.Empty;

        public string Reference { get; set; }
            = string.Empty;

        public decimal ServiceChargeRate { get; set; }

        public decimal ServiceChargeAmount { get; set; }

        public decimal TaxableAmount => AmountBeforeTax + ServiceChargeAmount;

        public decimal TaxableUnitPrice
        {
            get
            {
                if (Quantity == 0m)
                {
                    return 0m;
                }

                return TaxableAmount / Quantity;
            }
        }
    }

    // ---- PreviousInvoiceResult ----
    public sealed class PreviousInvoiceResult
    {
        public string Key { get; set; }
            = string.Empty;

        public string InvoiceNumber { get; set; }
            = string.Empty;

        public string InvoiceForm { get; set; }
            = string.Empty;

        public string InvoiceSerial { get; set; }
            = string.Empty;

        public string InvoiceDate { get; set; }
            = string.Empty;

        public string VerificationCode { get; set; }
            = string.Empty;

        public string KeySearch { get; set; }
            = string.Empty;
    }

    // ---- ProviderRawExecutionResult ----
    /// <summary>
    /// Kết quả của operation kỹ thuật như login,
    /// lấy token hoặc lấy hash.
    ///
    /// Không áp dụng xử lý nghiệp vụ cấp số hóa đơn.
    /// </summary>
    public sealed class ProviderRawExecutionResult
    {
        public bool Success { get; set; }

        public string Message { get; set; }
            = string.Empty;

        public ProviderRequestResult? Request { get; set; }

        public ProviderHttpExecutionResult? Http { get; set; }

        public string RawResponse { get; set; }
            = string.Empty;
    }

    // ---- ProviderDynamicResponseResult ----
    public sealed class ProviderDynamicResponseResult
    {
        /// <summary>
        /// Các giá trị canonical sau khi map response.
        ///
        /// Ví dụ:
        /// InvoiceNo
        /// TransactionID
        /// ReservationCode
        /// VerificationCode
        /// ErrorCode
        /// ErrorMessage
        /// </summary>
        public Dictionary<string, object?> Values { get; set; }
            = new(StringComparer.OrdinalIgnoreCase);

        public string? GetString(string key)
        {
            if (!Values.TryGetValue(key, out object? value))
                return null;

            return value?.ToString();
        }

        public object? GetValue(string key)
        {
            Values.TryGetValue(key, out object? value);

            return value;
        }

        public bool HasValue(string key)
        {
            if (!Values.TryGetValue(key, out object? value))
                return false;

            if (value == null)
                return false;

            if (value is string text)
                return !string.IsNullOrWhiteSpace(text);

            return true;
        }
    }

    // ---- ProviderDocumentFieldRuntimeModel ----
    public class ProviderDocumentFieldRuntimeModel
    {
        public int ID { get; set; }

        public int ProviderID { get; set; }

        /// <summary>
        /// Mã document definition.
        ///
        /// Ví dụ:
        /// VNPT_INVOICE_XML
        ///
        /// Nhưng engine không quan tâm đây là VNPT hay provider nào.
        /// </summary>
        public string DocumentCode { get; set; }
            = string.Empty;

        /// <summary>
        /// XML / JSON
        /// </summary>
        public string DocumentType { get; set; }
            = string.Empty;

        /// <summary>
        /// Root của document.
        /// Ví dụ XML: DSHDon
        /// </summary>
        public string RootName { get; set; }
            = string.Empty;

        /// <summary>
        /// Đọc từ cột JsonPath hiện tại trong DB.
        ///
        /// Trong runtime gọi chung là TargetPath
        /// để không phụ thuộc JSON.
        /// </summary>
        public string TargetPath { get; set; }
            = string.Empty;

        public string SourceType { get; set; }
            = string.Empty;

        public string SourceValue { get; set; }
            = string.Empty;

        public string DataType { get; set; }
            = "STRING";

        public bool IsRequired { get; set; }

        public string? DefaultValue { get; set; }

        public string? FormatString { get; set; }

        public bool OmitWhenNull { get; set; }

        public int DisplayOrder { get; set; }

        public string? CollectionName { get; set; }

        public string? ItemPath { get; set; }
    }

    // ---- ReserveInvoiceResult ----
    public class ReserveInvoiceResult
    {
        public string? Key { get; set; }

        public string? Pattern { get; set; }

        public string? Serial { get; set; }

        public string? InvoiceNo { get; set; }

        public string? InvoiceDate { get; set; }

        public string? VerificationCode { get; set; }

        public string? KeySearch { get; set; }
    }

    // ---- ProviderBusinessResponse ----
    public class ProviderBusinessResponse
    {
        public int Success { get; set; }

        public int Code { get; set; }

        public string? Message { get; set; }

        public object? Data { get; set; }
    }

    // ---- ProviderOperationRuntimeModel ----
    public class ProviderOperationRuntimeModel
    {
        public int ID { get; set; }

        public int ProviderID { get; set; }

        public string OperationCode { get; set; } = string.Empty;

        public string OperationName { get; set; } = string.Empty;

        public string HttpMethod { get; set; } = "POST";

        public string BaseUrl { get; set; } = string.Empty;

        public string Endpoint { get; set; } = string.Empty;

        public string ContentType { get; set; }
            = "application/json";

        // ==============================
        // Dynamic body/transport metadata
        // ==============================

        public string BodyType { get; set; }
            = "JSON";

        public string ActionCode { get; set; }
            = string.Empty;

        public string ExternalMethodCode { get; set; }
            = string.Empty;

        public int TimeoutSeconds { get; set; } = 120;

        public bool IsAuthenticationRequired { get; set; }

        public int? AuthenticationOperationID { get; set; }

        public bool Inactive { get; set; }

        public List<ProviderResponseRuleRuntimeModel> ResponseRules { get; set; }
            = new();
    }

    // ---- ProviderMappingModel ----
    public class ProviderMappingModel
    {
        public int ID { get; set; }

        public int OperationID { get; set; }

        public string MappingLocation { get; set; }
            = string.Empty;

        public string TargetPath { get; set; }
            = string.Empty;

        public string SourceType { get; set; }
            = string.Empty;

        public string? SourceValue { get; set; }

        public string DataType { get; set; }
            = "STRING";

        public bool IsRequired { get; set; }

        public string? DefaultValue { get; set; }

        public string? FormatString { get; set; }

        public bool OmitWhenNull { get; set; }

        public string? PrefixValue { get; set; }

        public string? SuffixValue { get; set; }

        public string? ConditionSourceType { get; set; }

        public string? ConditionSourceValue { get; set; }

        public string? ConditionOperator { get; set; }

        public string? ConditionExpectedValue { get; set; }

        public int DisplayOrder { get; set; }

        public bool Inactive { get; set; }
    }

    // ---- ProviderResponseRuleRuntimeModel ----
    public sealed class ProviderResponseRuleRuntimeModel
    {
        public int ID { get; set; }

        public int OperationID { get; set; }

        public string RuleCode { get; set; }
            = string.Empty;

        /// <summary>
        /// Đường dẫn trong JSON response.
        ///
        /// Ví dụ:
        /// errorCode
        /// result.invoiceNo
        /// data.status
        /// response.code
        /// </summary>
        public string SourcePath { get; set; }
            = string.Empty;

        /// <summary>
        /// Toán tử đánh giá.
        ///
        /// IS_NULL
        /// IS_NOT_NULL
        /// EQUALS
        /// NOT_EQUALS
        /// EMPTY
        /// NOT_EMPTY
        /// </summary>
        public string Operator { get; set; }
            = string.Empty;

        /// <summary>
        /// Giá trị dùng để so sánh.
        ///
        /// Ví dụ:
        /// 00
        /// SUCCESS
        /// true
        ///
        /// Không cần khi Operator = IS_NULL.
        /// </summary>
        public string? ExpectedValue { get; set; }

        /// <summary>
        /// SUCCESS hoặc FAILURE.
        /// </summary>
        public string ResultType { get; set; }
            = string.Empty;

        public int Priority { get; set; }

        public bool Inactive { get; set; }
    }

    // ---- ProviderExecutionPersistenceContext ----
    public sealed class ProviderExecutionPersistenceContext
    {
        public int PropertyID { get; set; }

        public int FolioVATID { get; set; }

        public int ProviderID { get; set; }

        public int OperationID { get; set; }

        public string SourceType { get; set; }
         = "PMS";

        public int SourceRecordID { get; set; }

        public string OperationCode { get; set; }
            = string.Empty;

        public string ExecutedBy { get; set; }
            = string.Empty;
    }

    // ---- ProviderExecutionResult ----
    public sealed class ProviderExecutionResult
    {
        public bool Success { get; set; }

        public string Message { get; set; }
            = string.Empty;

        public int ProviderCode { get; set; }

        public ProviderHttpExecutionResult Http { get; set; }

        public ProviderRequestResult Request { get; set; }

        public ReserveInvoiceResult? ReserveResult { get; set; }

        public string RawResponse { get; set; }
            = string.Empty;
    }

    // ---- ProviderHttpExecutionResult ----
    public class ProviderHttpExecutionResult
    {
        public bool HttpSuccess { get; set; }

        public int StatusCode { get; set; }

        public string ReasonPhrase { get; set; } = string.Empty;

        public string ResponseContentType { get; set; } = string.Empty;

        public string RawResponse { get; set; } = string.Empty;

        public long ElapsedMilliseconds { get; set; }
    }
}

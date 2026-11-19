namespace EInvoice.Dto
{
    /// <summary>Kết quả xử lý trả về từ service (cùng kiểu với OpResult của module Security).</summary>
    public class OpResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public object? Data { get; set; }
        public List<object>? Errors { get; set; }

        public static OpResult Ok(string message, object? data = null) =>
            new() { Success = true, Message = message, Data = data };

        public static OpResult Fail(string message, List<object>? errors = null) =>
            new() { Success = false, Message = message, Errors = errors };
    }

    // ===================================================================================================
    //  Provider Management: Provider / Field / Option
    // ===================================================================================================

    /// <summary>Thêm (ID = 0) hoặc sửa nhà cung cấp hóa đơn điện tử.</summary>
    public class ProviderSaveRequest
    {
        public int ID { get; set; }
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? LogoPath { get; set; }
        public bool Inactive { get; set; }
    }

    /// <summary>Thêm (ID = 0) hoặc sửa field cấu hình của nhà cung cấp.</summary>
    public class ProviderFieldSaveRequest
    {
        public int ID { get; set; }
        public int ProviderID { get; set; }
        public string? FieldCode { get; set; }
        public string? DisplayName { get; set; }
        public string? Description { get; set; }
        public string? DataType { get; set; }
        public string? ControlType { get; set; }
        public bool IsRequired { get; set; }
        public bool IsEncrypted { get; set; }
        public bool IsReadOnly { get; set; }
        public string? DefaultValue { get; set; }
        public string? Placeholder { get; set; }
        public int? MaxLength { get; set; }
        public string? RegexPattern { get; set; }
        public string? GroupName { get; set; }
        public int DisplayOrder { get; set; }
        public int ColumnWidth { get; set; } = 6;
        public bool Inactive { get; set; }
        public string? VisibleWhenFieldCode { get; set; }
        public string? VisibleWhenOperator { get; set; }
        public string? VisibleWhenValue { get; set; }
    }

    /// <summary>Thêm (ID = 0) hoặc sửa lựa chọn của field dạng Select.</summary>
    public class ProviderFieldOptionSaveRequest
    {
        public int ID { get; set; }
        public int ProviderFieldID { get; set; }
        public string? OptionValue { get; set; }
        public string? OptionText { get; set; }
        public bool IsDefault { get; set; }
        public int DisplayOrder { get; set; }
        public bool Inactive { get; set; }
    }

    // ===================================================================================================
    //  Operation / Mapping (giai đoạn 2)
    // ===================================================================================================

    /// <summary>Thêm (ID = 0) hoặc sửa API (Operation) của nhà cung cấp.</summary>
    public class OperationSaveRequest
    {
        public int ID { get; set; }
        public int ProviderID { get; set; }
        public string? OperationCode { get; set; }
        public string? OperationName { get; set; }
        public string? BusinessActionCode { get; set; }
        public string? HttpMethod { get; set; }
        public string? EndpointPath { get; set; }
        public string? ContentType { get; set; }
        public string? BodyType { get; set; }
        public int TimeoutSeconds { get; set; } = 60;
        public bool IsAuthenticationRequired { get; set; }
        public int? AuthenticationOperationID { get; set; }
        public string? ExternalMethodCode { get; set; }
        public string? ActionCode { get; set; }
        public string? SuccessStatusCodes { get; set; }
        public string? Description { get; set; }
        public int DisplayOrder { get; set; }
        public bool Inactive { get; set; }
    }

    /// <summary>Thêm (ID = 0) hoặc sửa Mapping tham số request/response của một Operation.</summary>
    public class MappingSaveRequest
    {
        public int ID { get; set; }
        public int OperationID { get; set; }
        public string? MappingLocation { get; set; }
        public string? TargetPath { get; set; }
        public string? SourceType { get; set; }
        public string? SourceValue { get; set; }
        public string? DataType { get; set; }
        public bool IsRequired { get; set; }
        public string? DefaultValue { get; set; }
        public string? FormatString { get; set; }
        public bool OmitWhenNull { get; set; } = true;
        public string? PrefixValue { get; set; }
        public string? SuffixValue { get; set; }
        public int DisplayOrder { get; set; }
        public bool Inactive { get; set; }
        public string? ConditionSourceType { get; set; }
        public string? ConditionSourceValue { get; set; }
        public string? ConditionOperator { get; set; }
        public string? ConditionExpectedValue { get; set; }
    }

    // ===================================================================================================
    //  Property E-Invoice (giai đoạn 3)
    // ===================================================================================================

    /// <summary>Giá trị một field cấu hình của nhà cung cấp do người dùng nhập cho Property.</summary>
    public class PropertyValueInput
    {
        public int ProviderFieldID { get; set; }
        public string? FieldValue { get; set; }
        /// <summary>true: giữ nguyên giá trị đã lưu (dùng cho mật khẩu/secret để trống khi sửa).</summary>
        public bool KeepExistingValue { get; set; }
    }

    /// <summary>Thêm (ID = 0) hoặc sửa Property: nguồn dữ liệu (kết nối DB động) và cấu hình Provider.</summary>
    public class PropertySaveRequest
    {
        public int ID { get; set; }
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? SourceType { get; set; } = "PMS";
        public string? TaxCode { get; set; }
        public string? DatabaseIP { get; set; }
        public string? DatabaseName { get; set; }
        public string? DatabaseUser { get; set; }
        /// <summary>Để trống khi sửa = giữ mật khẩu đã lưu.</summary>
        public string? DatabasePassword { get; set; }
        public string? InvoiceSearchSP { get; set; }
        public string? InvoiceDetailSP { get; set; }
        public bool Inactive { get; set; }
        public int ProviderID { get; set; }
        public List<PropertyValueInput> Values { get; set; } = new();
    }

    /// <summary>Kiểm tra kết nối tới DB nguồn bằng thông tin đang nhập (chưa cần lưu).</summary>
    public class PropertyTestRequest
    {
        public int ID { get; set; }
        public string? DatabaseIP { get; set; }
        public string? DatabaseName { get; set; }
        public string? DatabaseUser { get; set; }
        /// <summary>Để trống và ID > 0 = dùng mật khẩu đã lưu.</summary>
        public string? DatabasePassword { get; set; }
        public string? InvoiceSearchSP { get; set; }
        public string? InvoiceDetailSP { get; set; }
    }

    // ===================================================================================================
    //  Engine chạy hóa đơn (giai đoạn 4)
    // ===================================================================================================

    /// <summary>Xem trước request sẽ gửi cho nhà cung cấp (không gửi đi): Property + Operation + FolioVATID.</summary>
    public class PreviewRequest
    {
        public int PropertyID { get; set; }
        public int OperationID { get; set; }
        public int FolioVATID { get; set; }
    }

    /// <summary>Người đang thao tác (lấy từ Session ở controller) để ghi vào context và nhật ký.</summary>
    public sealed record RuntimeUser(int? UserId, string? UserName, string? LoginName)
    {
        public string DisplayName => UserName ?? LoginName ?? string.Empty;
    }
}

// ===================================================================================================
//  Màn thao tác hóa đơn PMS (giai đoạn 5)
// ===================================================================================================
namespace EInvoice.Dto
{
    /// <summary>Điều kiện tìm hóa đơn PMS (truyền xuống stored procedure tìm hóa đơn của Property).</summary>
    public class InvoiceSearchRequest
    {
        public int PropertyID { get; set; }
        public string? Name { get; set; }
        public string? ConfNo { get; set; }
        public string? FolioNo { get; set; }
        public string? Serial { get; set; }
        public string? InvoiceNo { get; set; }
        public string? FromDate { get; set; }   // yyyy-MM-dd
        public string? ToDate { get; set; }     // yyyy-MM-dd
        public int Print { get; set; }
        public int ResType { get; set; } = -1;
    }

    /// <summary>Thực hiện một nghiệp vụ cho nhiều hóa đơn (ActionCode: UPLOAD_INVOICE, PUBLISH_INVOICE, ISSUE_INVOICE, UPLOAD_AND_PUBLISH).</summary>
    public class InvoiceBatchRequest
    {
        public int PropertyID { get; set; }
        public string? ActionCode { get; set; }
        public List<int>? FolioVatIds { get; set; }
        public bool ConfirmSend { get; set; }
    }
}

namespace EInvoice.Dto
{
    /// <summary>Điều kiện lọc nhật ký thực thi của một Property (mọi điều kiện đều tùy chọn).</summary>
    public class LogSearchRequest
    {
        public int PropertyID { get; set; }
        public string? FolioVatId { get; set; }
        public string? LogType { get; set; }     // UPLOAD_INVOICE, PUBLISH_INVOICE, ISSUE_INVOICE...
        public string? Status { get; set; }      // "1" thành công, "0" thất bại
        public string? FromDate { get; set; }    // yyyy-MM-dd
        public string? ToDate { get; set; }
    }
}

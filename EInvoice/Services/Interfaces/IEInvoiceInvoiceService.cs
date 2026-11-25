using System.Data;
using EInvoice.Dto;

namespace EInvoice.Services.Interfaces
{
    /// <summary>Thao tác với hóa đơn PMS: tìm hóa đơn, chạy nghiệp vụ hàng loạt, xem nhật ký.</summary>
    public interface IEInvoiceInvoiceService
    {
        /// <summary>Tìm hóa đơn trong DB PMS của Property, trạng thái Uploaded/Published lấy từ nhật ký thực thi.</summary>
        DataTable SearchInvoices(InvoiceSearchRequest request);

        /// <summary>Chạy nghiệp vụ lần lượt cho từng hóa đơn; một hóa đơn lỗi không chặn các hóa đơn còn lại.</summary>
        Task<OpResult> ExecuteBatchAsync(InvoiceBatchRequest request, RuntimeUser user, CancellationToken ct = default);

        /// <summary>Property người dùng được phép thao tác (nhóm Administrator thấy tất cả).</summary>
        DataTable GetAllowedProperties(int userId, int userGroupId);

        /// <summary>Các nghiệp vụ (ISSUE_INVOICE, UPLOAD_INVOICE...) mà Provider của Property hỗ trợ.</summary>
        DataTable GetProviderActions(int propertyId);

        DataTable SearchLogs(LogSearchRequest request);

        DataTable GetLogDetail(long id);
    }
}

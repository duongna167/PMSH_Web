using EInvoice.Dto;
using EInvoice.Runtime;

namespace EInvoice.Services.Interfaces
{
    /// <summary>Engine chạy hóa đơn điện tử cho nguồn PMS: xem trước request và thực thi một nghiệp vụ (Upload, Publish, Issue...).</summary>
    public interface IEInvoiceRuntimeService
    {
        /// <summary>Dựng request sẽ gửi cho nhà cung cấp từ dữ liệu hóa đơn thật và cấu hình Mapping. Không gửi đi; che mật khẩu/token.</summary>
        OpResult PreviewRequest(PreviewRequest request, RuntimeUser user);

        /// <summary>
        /// Thực thi một Business Action (UPLOAD_INVOICE, PUBLISH_INVOICE, ISSUE_INVOICE...) cho một hóa đơn PMS:
        /// đăng nhập nếu API yêu cầu, gọi nhà cung cấp, đánh giá Response Rule, ghi nhật ký và đồng bộ trạng thái về DB PMS.
        /// </summary>
        Task<ProviderExecutionResult> ExecuteActionAsync(int propertyId, int folioVatId, string actionCode, RuntimeUser user, CancellationToken cancellationToken = default);
    }
}

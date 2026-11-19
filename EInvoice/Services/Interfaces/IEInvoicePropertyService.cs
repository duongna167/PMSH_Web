using System.Data;
using EInvoice.Dto;

namespace EInvoice.Services.Interfaces
{
    /// <summary>Quản lý Property E-Invoice: nguồn dữ liệu (kết nối DB động) và cấu hình nhà cung cấp theo từng Property.</summary>
    public interface IEInvoicePropertyService
    {
        /// <summary>Danh sách Property (id = 0: tất cả). Không bao giờ trả mật khẩu.</summary>
        DataTable SearchProperties(int id = 0);

        /// <summary>Field của một Provider kèm giá trị đã lưu của Property. Field mã hóa chỉ trả cờ HasSavedValue.</summary>
        DataTable GetProviderFields(int propertyId, int providerId);

        /// <summary>Các lựa chọn (option) của tất cả field thuộc một Provider.</summary>
        DataTable GetProviderOptions(int providerId);

        OpResult SaveProperty(PropertySaveRequest request, string user);
        OpResult DeleteProperty(int id);

        /// <summary>Thử kết nối DB nguồn và kiểm tra các SP đã khai báo có tồn tại không.</summary>
        OpResult TestConnection(PropertyTestRequest request);

        /// <summary>Mã hóa mọi mật khẩu/secret còn lưu dạng chữ thường (dữ liệu chuyển từ hệ thống cũ). Chạy lại nhiều lần an toàn.</summary>
        OpResult EncryptLegacySecrets(string user);
    }
}

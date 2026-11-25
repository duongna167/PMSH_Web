using System.Data;

namespace EInvoice.Services.Interfaces
{
    /// <summary>Yêu cầu tạo hóa đơn từ các folio đã chọn (New Summary Invoice).</summary>
    public class NewInvoiceRequest
    {
        /// <summary>Danh sách FolioID cần xuất hóa đơn.</summary>
        public List<int> FolioIds { get; set; } = new();

        /// <summary>Khoảng ngày lọc giao dịch chưa xuất hóa đơn.</summary>
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public DateTime VATDate { get; set; }
        public string FormNo { get; set; } = "";
        public string SerialNo { get; set; } = "";

        /// <summary>Số hóa đơn bắt đầu, tự tăng cho các hóa đơn tiếp theo (hiển thị 7 chữ số).</summary>
        public int StartInvoiceNo { get; set; }

        public decimal ExchangeRate { get; set; }
        public string CurrencyID { get; set; } = "VND";
        public string PaymentMethod { get; set; } = "";
        public string Note { get; set; } = "";

        /// <summary>
        /// 0 = tách theo tỷ lệ VAT (mặc định của WinForms), 1 = tất cả folio gộp trong 1 hóa đơn theo loại,
        /// 2 = gộp theo từng folio, 3 = gộp theo loại dịch vụ.
        /// </summary>
        public int GroupType { get; set; }

        /// <summary>Lấy thông tin khách theo cùng một profile (menu "The same Profile").</summary>
        public bool IsSameProfile { get; set; }

        /// <summary>Đã được người dùng xác nhận tiếp tục dù có số hóa đơn trùng.</summary>
        public bool IgnoreDuplicateInvoiceNo { get; set; }
    }

    /// <summary>Báo trùng số hóa đơn để giao diện hỏi người dùng có muốn tiếp tục không.</summary>
    public class DuplicateInvoiceException : Exception
    {
        public List<string> DuplicateInvoiceNos { get; }

        public DuplicateInvoiceException(List<string> duplicates)
            : base("Duplicate invoice no(s): " + string.Join(", ", duplicates))
        {
            DuplicateInvoiceNos = duplicates;
        }
    }

    /// <summary>
    /// Tạo hóa đơn mới (New Invoice) từ các folio chưa xuất hóa đơn. Mỗi Property là một DB PMS riêng nên
    /// mọi thao tác đọc/ghi đều chạy trên DB của Property được chọn (không phải DB của web).
    /// </summary>
    public interface INewInvoiceService
    {
        /// <summary>Tên Property và ngày nghiệp vụ của DB Property (để hiện trên popup và đặt ngày mặc định).</summary>
        (string PropertyName, DateTime BusinessDate) GetContext(int propertyId);

        /// <summary>Danh sách folio chưa xuất hóa đơn (spVAT_SearchFolioForVAT).</summary>
        DataTable SearchFolios(int propertyId, string confirmationNo, string folioNo, string roomNo, DateTime fromDate, DateTime toDate);

        /// <summary>Các giao dịch của folio chưa nằm trong hóa đơn nào (spVAT_GetFolioDetailNotInVAT).</summary>
        DataTable GetFolioDetails(int propertyId, int folioId, DateTime fromDate, DateTime toDate);

        /// <summary>Toàn bộ giao dịch của folio để xem Billing (spSearchTransactionInFolio).</summary>
        DataTable GetFolioTransactions(int propertyId, int folioId);

        /// <summary>Tạo hóa đơn; trả về danh sách ID hóa đơn đã tạo. Ném DuplicateInvoiceException nếu trùng số.</summary>
        (List<int> CreatedIds, List<string> Errors) Create(int propertyId, NewInvoiceRequest request, string loginName);
    }
}

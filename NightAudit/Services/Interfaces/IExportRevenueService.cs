namespace NightAudit.Services.Interfaces
{
    /// <summary>Một dòng định khoản kế toán của báo cáo Export Revenue.</summary>
    public class ExportRevenueRow
    {
        public string AccountCode { get; set; } = "";
        public string AccountName { get; set; } = "";
        public string CustomerCode { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal DebitUSD { get; set; }
        public decimal CreditUSD { get; set; }
        public decimal DebitVND { get; set; }
        public decimal CreditVND { get; set; }
        public string MaBoPhan { get; set; } = "";
        public string MaDoanhThu { get; set; } = "";
        public int NhomDK { get; set; }
        public string MaBPDC { get; set; } = "";
        /// <summary>Mã transaction gốc (chỉ dùng cho các bản Sun System / By invoice).</summary>
        public string TransactionCode { get; set; } = "";
        /// <summary>Số hóa đơn (chỉ dùng cho bản By invoice).</summary>
        public string InvoiceNo { get; set; } = "";
    }

    /// <summary>Kết quả tìm kiếm: lỗi nghiệp vụ (nếu có) hoặc danh sách dòng định khoản.</summary>
    public class ExportRevenueResult
    {
        public string? Error { get; set; }
        public List<ExportRevenueRow> Rows { get; set; } = new();
    }

    /// <summary>Kết quả của các bản Export Revenue có định dạng Excel riêng (By market / Sun System / By invoice).</summary>
    public class ExportAccountingResult
    {
        public string? Error { get; set; }
        /// <summary>Các dòng hiển thị trên lưới (đã sắp theo NhomDK, DebitUSD giảm dần như desktop).</summary>
        public List<ExportRevenueRow> Rows { get; set; } = new();
        /// <summary>Các dòng Excel đã tính sẵn: số cột -> giá trị ô.</summary>
        public List<Dictionary<int, object?>> ExcelRows { get; set; } = new();
        /// <summary>Dòng bắt đầu ghi dữ liệu trong file Excel (theo file mẫu của desktop).</summary>
        public int ExcelStartRow { get; set; }
        public string ExcelFileName { get; set; } = "";
        /// <summary>Ô "Total Debit Amount" / "Total Credit Amount" (chỉ bản Sun System và By invoice), đã định dạng ###,###,###.</summary>
        public string TotalDebit { get; set; } = "";
        public string TotalCredit { get; set; } = "";
        /// <summary>Tiêu đề cột Excel: số cột -> tên.</summary>
        public Dictionary<int, string> ExcelHeaders { get; set; } = new();
    }

    /// <summary>Export Revenue sang kế toán: bản chuẩn (frmExportRevenue_New) và các bản By market / Sun System / By invoice.</summary>
    public interface IExportRevenueService
    {
        /// <param name="date">Ngày cần xuất.</param>
        /// <param name="summary">true = Summary (gộp), false = Detail (chi tiết).</param>
        ExportRevenueResult Search(DateTime date, bool summary);

        /// <param name="mode">"market" (frmExportRevenueByMarket), "sun" (frmExportRevenue_SunSystem) hoặc "invoice" (frmExportRevenue_FLC_V1).</param>
        /// <param name="marketIds">Danh sách Market ID cách nhau dấu phẩy (chỉ bản market, rỗng = tất cả).</param>
        /// <param name="sourceIds">Danh sách Source ID cách nhau dấu phẩy (chỉ bản market, rỗng = tất cả).</param>
        ExportAccountingResult SearchAccounting(string mode, DateTime date, string marketIds, string sourceIds);

        /// <summary>Danh sách Market và Source cho bộ lọc của bản By market.</summary>
        (List<(int Id, string Name)> Markets, List<(int Id, string Name)> Sources) GetMarketSourceLookups();
    }
}

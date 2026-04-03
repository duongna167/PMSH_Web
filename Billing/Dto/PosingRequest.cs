using BaseBusiness.Model;

namespace Billing.Dto
{
    public class PostingRequest
    {
        public int FolioNo { get; set; }
        public int CurrentFolioID { get; set; }
        public string ConfirmNo { get; set; } = string.Empty;
        public int ProfileID { get; set; }
        public string AccountName { get; set; } = string.Empty;
        public string RoomNo { get; set; } = string.Empty;
        public string MasterCode { get; set; }
        public List<FolioDetailModel> Details { get; set; }
        public bool ChkExpressService { get; set; }
    }

    public class PostingSaveRequestDto : PostingRequest
    {
        public string Mode { get; set; } = string.Empty;
        public PostingInvoiceRequestDto? InvoiceRequest { get; set; }
    }

    public class PostingInvoiceRequestDto
    {
        public bool AutoPosting { get; set; }
        public DateTime SysDate { get; set; }
        public DateTime BusinessDate { get; set; }
        public int ProID { get; set; }
        public string ProCode { get; set; } = string.Empty;
        public string ConfirmNo { get; set; } = string.Empty;
        public int RsvID { get; set; }
        public int RoomID { get; set; }
        // Bắt buộc thêm để giữ đúng logic cũ của IPTV:
        // WinForms cũ dùng mR.RoomNo, không phải RoomID
        public string RoomNo { get; set; } = string.Empty;

        public int ProfileID { get; set; }
        public string AccountName { get; set; } = string.Empty;
        public int Win { get; set; }
        public string InvoiceCode { get; set; } = string.Empty;
        public string InvoiceDesc { get; set; } = string.Empty;
        public string InvoiceRef { get; set; } = string.Empty;
        public string InvoiceSupp { get; set; } = string.Empty;
        public string InvoiceNo { get; set; } = string.Empty;
        public string CurrencyLocal { get; set; } = string.Empty;
        public int UserID { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int ShiftID { get; set; }

        public List<PostingInvoiceItemDto> Items { get; set; } = new List<PostingInvoiceItemDto>();
    }

    public class PostingInvoiceItemDto
    {
        public string TransCode { get; set; } = string.Empty;
        public string Desc { get; set; } = string.Empty;
        public string ArCode { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public bool TaxInclude { get; set; }
        public int Quan { get; set; }
        public string CurrencyID { get; set; } = string.Empty;
        public int RoomTypeID { get; set; }
        public string RoomType { get; set; } = string.Empty;
        public string Ref { get; set; } = string.Empty;
        public string Supp { get; set; } = string.Empty;

        // Phục vụ phần post IPTV / tổng hợp sau khi post
        public decimal AmountNetForIptv { get; set; }
        public string ArticleDesc { get; set; } = string.Empty;
    }
}

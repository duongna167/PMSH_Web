namespace BaseBusiness.Model
{
    using System;
    using BaseBusiness.bc;

    public class ARAccountReceivableTransModel : BaseModel
    {
        public int ID { get; set; }

        public int? AccountReceivableID { get; set; }

        public string? AccountNo { get; set; }

        public int? FolioID { get; set; }

        public DateTime? TransactionDate { get; set; }

        public string? TransactionCode { get; set; }

        public string? Description { get; set; }

        public decimal? Amount { get; set; }

        public decimal? Paid { get; set; }

        public decimal? Balance { get; set; }

        public string? CurrencyID { get; set; }

        public decimal? AmountMaster { get; set; }

        public string? CurrencyMaster { get; set; }

        public string? Reference { get; set; }

        public string? Supplement { get; set; }

        public bool? IsTranferFO { get; set; }

        public bool? IsTransferred { get; set; }

        public bool? IsAdjusted { get; set; }

        public bool? IsActive { get; set; }

        public bool? IsPrinted { get; set; }

        public DateTime? CheckedOutDate { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime? CreatedDate { get; set; }

        public string? UpdatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        public int? FolioDetailID { get; set; }
    }
}
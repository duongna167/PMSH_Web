using System;
using BaseBusiness.bc;

namespace BaseBusiness.Model
{

    public class AccountReceivableTransModel : BaseModel
    {
        public int ID { get; set; }

        public string? ARNo { get; set; }

        public int? FolioID { get; set; }

        public string? FolioName { get; set; }

        public DateTime? TransactionDate { get; set; }

        public string? TransactionCode { get; set; }

        public string? Description { get; set; }

        public decimal? Amount { get; set; }

        public string? CurrencyID { get; set; }

        public decimal? AmountCurrencyLimit { get; set; }

        public string? CurrencyLimit { get; set; }

        public string? Reference { get; set; }

        public string? Supplement { get; set; }

        public bool? IsTranferFO { get; set; }

        public decimal? AmountMaster { get; set; }

        public string? CurrencyMaster { get; set; }

        public int? UserInsertID { get; set; }

        public DateTime? CreateDate { get; set; }

        public int? UserUpdateID { get; set; }

        public DateTime? UpdateDate { get; set; }
    }
}
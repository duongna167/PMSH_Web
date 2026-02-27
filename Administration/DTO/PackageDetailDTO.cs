namespace Administration.DTO
{
    public class PackageDetailDTO
    {
        public int ID { get; set; }

        public int? PackageID { get; set; }
        public int? SeasonID { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public string? TransCode { get; set; }
        public string? ArticlesCode { get; set; }
        public string? TransCodeOver { get; set; }
        public string? ArticlesCodeOver { get; set; }

        public string? Description { get; set; }

        public decimal? Price { get; set; }
        public string? CurrencyID { get; set; }

        public decimal? AllowanceAmount { get; set; }

        public int RhythmPostingID { get; set; }
        public int? CalculationRuleID { get; set; } = 0;

        public DateTime? PostingDate { get; set; }
        public string? PostingDay { get; set; }

        public decimal? PriceAfterTax { get; set; }
        public bool? IsTaxInclude { get; set; }

        public int UserInsertID { get; set; }

        public DateTime? CreateDate { get; set; }

        public int? UserUpdateID { get; set; }
        public DateTime? UpdateDate { get; set; }

        // property mới để chứa description từ transaction
        public string? TransactionDescription { get; set; } = string.Empty;

        public class DeleteRequest
        {
            public int ID { get; set; }
        }
    }
}

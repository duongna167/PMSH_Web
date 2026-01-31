namespace Reservation.Dto
{
    public class ReservationPackageDTO
    {
        public class ReservationPackageSummary
        {
            public int PackageID { get; set; }
            public string RateCode { get; set; }
            public string Package { get; set; }
            public string Description { get; set; }

            public int Qty { get; set; }
            public decimal TotalPrice { get; set; }
            public decimal TotalPriceAfterTax { get; set; }

            public string CurrencyID { get; set; }

            public DateTime BeginDate { get; set; }
            public DateTime EndDate { get; set; }

            public int ReservationID { get; set; }
            public string Excl { get; set; }

            public int RateCodeID { get; set; }

            // From Type = 2
            public bool IsTaxInclude { get; set; }
            public string TransactionCode { get; set; }
            public int CalculationRuleID { get; set; }
            public int PostingRhythmID { get; set; }
            public DateTime PostingDay { get; set; }

            public int NoOfAdult { get; set; }
            public int NoOfChild { get; set; }
            public int NoOfChild1 { get; set; }
            public int NoOfChild2 { get; set; }

            public int Night { get; set; }
        }

    }
}

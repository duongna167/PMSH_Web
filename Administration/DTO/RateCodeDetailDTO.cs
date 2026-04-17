namespace Administration.DTO
{
    /// <summary>JSON body for SaveRateCodeDetail (camelCase from client).</summary>
    public class RateCodeDetailSaveRequest
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int RateCodeId { get; set; }
        public int RoomTypeId { get; set; }
        public int PackageId { get; set; }
        public int SeasonId { get; set; }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public string? TransactionCode { get; set; }
        public string? CurrencyId { get; set; }

        public int MinLos { get; set; }
        public int MaxLos { get; set; }
        public int MinNoOfRoom { get; set; }
        public int MaxNoOfRoom { get; set; }

        public bool PrintRate { get; set; }
        public bool Discount { get; set; }

        public decimal A1 { get; set; }
        public decimal A2 { get; set; }
        public decimal A3 { get; set; }
        public decimal A4 { get; set; }
        public decimal A5 { get; set; }
        public decimal A6 { get; set; }
        public decimal A7 { get; set; }
        public decimal A8 { get; set; }
        public decimal A9 { get; set; }
        public decimal A10 { get; set; }
        public decimal A11 { get; set; }
        public decimal A12 { get; set; }
        public decimal A13 { get; set; }
        public decimal A14 { get; set; }
        public decimal A15 { get; set; }

        public decimal A1AfterTax { get; set; }
        public decimal A2AfterTax { get; set; }
        public decimal A3AfterTax { get; set; }
        public decimal A4AfterTax { get; set; }
        public decimal A5AfterTax { get; set; }
        public decimal A6AfterTax { get; set; }
        public decimal A7AfterTax { get; set; }
        public decimal A8AfterTax { get; set; }
        public decimal A9AfterTax { get; set; }
        public decimal A10AfterTax { get; set; }
        public decimal A11AfterTax { get; set; }
        public decimal A12AfterTax { get; set; }
        public decimal A13AfterTax { get; set; }
        public decimal A14AfterTax { get; set; }
        public decimal A15AfterTax { get; set; }

        public decimal C1 { get; set; }
        public decimal C2 { get; set; }
        public decimal C3 { get; set; }
        public decimal C1AfterTax { get; set; }
        public decimal C2AfterTax { get; set; }
        public decimal C3AfterTax { get; set; }

        public decimal AdultExtra { get; set; }
        public decimal AdultExtraTax { get; set; }
    }

    public class RateCodeDetailDTO
    {
        public class RateCodeDetailInputDto
        {
            public int RateCodeID { get; set; }
            public DateTime? BeginDate { get; set; }
            public DateTime? EndDate { get; set; }
            public string RoomType { get; set; } = "PVT";
            public string TransCode { get; set; } = "1006";
            public string CurrencyID { get; set; } = "USD";
            public int PackageID { get; set; } = 0;

            public decimal A1 { get; set; } = 0;
            public decimal A2 { get; set; } = 0;
            public decimal A3 { get; set; } = 0;
            public decimal A4 { get; set; } = 0;
            public decimal A5 { get; set; } = 0;
            public decimal A6 { get; set; } = 0;

            public decimal C1 { get; set; } = 0;
            public decimal C2 { get; set; } = 0;
            public decimal C3 { get; set; } = 0;

            public int MinLOS { get; set; } = 0;
            public int MaxLOS { get; set; } = 0;
            public int MinRoom { get; set; } = 0;
            public int MaxRoom { get; set; } = 0;
        }

        public class RateCodeDetailOutputDto
        {
            public int ID { get; set; }
            public string RateCode { get; set; } = "";
            public string RoomType { get; set; } = "";
            public DateTime? RateDate { get; set; }
            public int RateCodeID { get; set; }
            public int RoomTypeID { get; set; }

            public decimal A1 { get; set; }
            public decimal A1AfterTax { get; set; }
            public decimal A2 { get; set; }
            public decimal A2AfterTax { get; set; }
            public decimal A3 { get; set; }
            public decimal A3AfterTax { get; set; }
            public decimal A4 { get; set; }
            public decimal A4AfterTax { get; set; }
            public decimal A5 { get; set; }
            public decimal A5AfterTax { get; set; }
            public decimal A6 { get; set; }
            public decimal A6AfterTax { get; set; }

            public decimal C1 { get; set; }
            public decimal C1AfterTax { get; set; }
            public decimal C2 { get; set; }
            public decimal C2AfterTax { get; set; }
            public decimal C3 { get; set; }
            public decimal C3AfterTax { get; set; }

            public decimal AdultExtra { get; set; }
            public decimal AdultExtraTax { get; set; }

            public string TransactionCode { get; set; } = "";
            public string CurrencyID { get; set; } = "";
        }


    }
}

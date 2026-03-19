using BaseBusiness.bc;

namespace BaseBusiness.Model
{
    public class ReservationGroupAmountByCurrencyModel : BaseModel
    {
        public int ID { get; set; }
        public int ReservationGroupID { get; set; }
        public string CurrencyID { get; set; }
        public decimal AmountBeforTax { get; set; }
        public decimal AmountAfterTax { get; set; }
        public int UserInsertID { get; set; }
        public DateTime CreateDate { get; set; }
        public int UserUpdateID { get; set; }
        public DateTime UpdateDate { get; set; }
    }
}
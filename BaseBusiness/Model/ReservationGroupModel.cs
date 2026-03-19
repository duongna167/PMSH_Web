using BaseBusiness.bc;

namespace BaseBusiness.Model
{
    public class ReservationGroupModel : BaseModel
    {
        public int ID { get; set; }
        public int ConfirmationNo { get; set; }
        public DateTime FirstArrival { get; set; }
        public DateTime LastDeparture { get; set; }
        public int TotalRoom { get; set; }
        public int TotalAdult { get; set; }
        public int TotalChild { get; set; }
        public int TotalChild1 { get; set; }
        public int TotalChild2 { get; set; }
        public decimal TotalReservationBalance { get; set; }
        public string Comment { get; set; }
        public int ProfileContactID { get; set; }
        public int UserInsertID { get; set; }
        public DateTime CreateDate { get; set; }
        public int UserUpdateID { get; set; }
        public DateTime UpdateDate { get; set; }
        public DateTime OptionDate { get; set; }
        public string OptionDateDesc { get; set; }
    }
}
using BaseBusiness.bc;

namespace BaseBusiness.Model
{
    public class MessageModel : BaseModel
    {
        public int ID { get; set; }
        public string? Code { get; set; }
        public int ReservationID { get; set; }
        public string? Language { get; set; }
        public string? GuestName { get; set; }
        public string? Title { get; set; }
        public string? Company { get; set; }
        public string? Phone { get; set; }
        public string? Message { get; set; }
        public string? ReceiveBy { get; set; }
        public DateTime? ReceivedDate { get; set; }
        public DateTime? PrintDate { get; set; }
        public bool? ReceiveStatus { get; set; }
        public bool? PrintStatus { get; set; }
        public int? VideoStatus { get; set; }
        public int? LampStatus { get; set; }
        public bool? IsDelete { get; set; }
        public string? CreateBy { get; set; }
        public DateTime? CreateDate { get; set; }
        public string? UpdateBy { get; set; }
        public DateTime? UpdateDate { get; set; }
        public DateTime? SendDate { get; set; }
        public bool? SendStatus { get; set; }
    }
}
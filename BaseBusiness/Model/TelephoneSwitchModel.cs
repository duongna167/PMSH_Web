using BaseBusiness.bc;

namespace BaseBusiness.Model
{
    public class TelephoneSwitchModel : BaseModel
    {
        public int ID { get; set; }
        public string RoomNo { get; set; }
        public string GuestName { get; set; }
        public int Status { get; set; }
        public DateTime CreateDate { get; set; }
    }

}

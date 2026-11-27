using BaseBusiness.bc;
using System;

namespace BaseBusiness.Model
{
    public class FOInternalMessageReceivedModel : BaseModel
    {
        public int ID { get; set; }
        public int MessageID { get; set; }
        public int UserID { get; set; }
        public DateTime ReceivedDate { get; set; }
    }
}

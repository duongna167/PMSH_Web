using BaseBusiness.bc;
using System;

namespace BaseBusiness.Model
{
    public class UserDetailModel : BaseModel
    {
        public int ID { get; set; }
        public int UserID { get; set; }
        public string TypedPassword { get; set; }
        public string ComputerName { get; set; }
        public string ComputerIP { get; set; }
        public int Type { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}

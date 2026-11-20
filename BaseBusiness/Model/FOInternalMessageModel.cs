using BaseBusiness.bc;
using System;

namespace BaseBusiness.Model
{
    public class FOInternalMessageModel : BaseModel
    {
        public int ID { get; set; }
        public string Message { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int ToDepartmentID { get; set; }
        public bool IsDelete { get; set; }
        public int UserInsertID { get; set; }
        public DateTime CreateDate { get; set; }
        public int UserUpdateID { get; set; }
        public DateTime UpdateDate { get; set; }
    }
}

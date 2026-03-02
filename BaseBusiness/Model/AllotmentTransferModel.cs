using BaseBusiness.bc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseBusiness.Model
{
    public class AllotmentTransferModel : BaseModel
    {
        public int ID { get;set; }
        public int? FromAllotmentID { get; set; } // null/0  thì là trans.Inventory
        public int? ToAllotmentID { get; set; }
        public int? RoomTypeID { get; set; }
        public int? Quantity { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string CreateBy { get; set; }
        public DateTime? CreateDate { get; set; }
        public string UpdateBy { get; set; }
        public DateTime? UpdateDate { get; set; }
        public string Description { get; set; }
    }
}

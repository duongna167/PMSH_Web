using BaseBusiness.bc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseBusiness.Model
{
    public class ItemInventoryModel : BaseModel
    {
        public int ID { get; set; }

        public int ItemID { get; set; }

        public DateTime Date { get; set; }

        public int Quantity { get; set; }

        public int VendorID { get; set; }

        public bool Sun { get; set; }
        public bool Mon { get; set; }
        public bool Tue { get; set; }
        public bool Wed { get; set; }
        public bool Thu { get; set; }
        public bool Fri { get; set; }
        public bool Sat { get; set; }

        public DateTime CreateDate { get; set; }

        public DateTime UpdateDate { get; set; }

        public int UserInsertID { get; set; }

        public int UserUpdateID { get; set; }
    }
}

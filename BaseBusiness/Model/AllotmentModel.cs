using BaseBusiness.bc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseBusiness.Model
{
    public class AllotmentModel : BaseModel
    {
        public int ID { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public int ProfileID { get; set; }
        public string AccountName { get; set; }
        public int MarketID { get;set; }
        public int CuttOfDay { get; set; }
        public DateTime CuttOfDate { get; set; }
        public int AllotmentTypeID { get; set; }
        public bool IsDefault { get; set; }
        public string CreateBy { get; set;}
        public DateTime CreateDate { get; set; }
        public string UpdateBy { get; set; }
        public DateTime UpdateDate { get; set; }
    }
}

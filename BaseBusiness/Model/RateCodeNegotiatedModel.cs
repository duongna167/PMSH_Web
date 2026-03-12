using BaseBusiness.bc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseBusiness.Model
{
    public class RateCodeNegotiatedModel : BaseModel
    {
        public int ID { get; set; }
        public int RateCodeID { get; set; }
        public int ProfileID { get; set; }
        public string CommissionCode { get; set; }
        public DateTime BeginSellDate { get; set; }
        public DateTime EndSellDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
    }
}

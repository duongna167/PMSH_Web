using BaseBusiness.bc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseBusiness.Model
{
    public class InterfaceToAccModel : BaseModel
    {
        public int ID { get; set; }

        public string ProfitCenter { get; set; }

        public string TransactionCode { get; set; }

        public string Description { get; set; }

        public string AccountCode { get; set; }

        public string TK_No { get; set; }

        public string TK_Co { get; set; }

        public string TK_Co_Deposit { get; set; }

        public string TK_No_DT { get; set; }

        public string MaBP_ACC { get; set; }

        public string MaDT_ACC { get; set; }

        public string MaCN_ACC { get; set; }

        public bool Nguoc { get; set; }

        public string MaBCDT { get; set; }

        public int PerDT { get; set; }

        public string MaBPDC { get; set; }

        public bool IsSynchronous { get; set; }
    }
}

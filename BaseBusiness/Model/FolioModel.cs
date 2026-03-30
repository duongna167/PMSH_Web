using BaseBusiness.bc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseBusiness.Model
{
    public class FolioModel : BaseModel
    {
        public int ID { get; set; }
        public string ARNo { get; set; }
        public DateTime FolioDate { get;set; }
        public int FolioNo { get; set; }
        public int ReservationID { get; set; }
        public int ProfileID { get; set; }
        public string AccountName { get; set; }
        public bool Status { get; set; }
        public bool IsMasterFolio { get; set; }
        public string ConfirmationNo { get; set; }
        public decimal BalanceUSD { get; set; }
        public decimal BalanceVND { get; set; }
        public bool IsPrintVAT { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime UpdateDate { get; set; }
        public int UserUpdateID { get; set; }
        public int UserInsertID { get; set; }
    }
}

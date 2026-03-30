using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseBusiness.Model
{
    public class SplitTransactionDto
    {
        public int FolioDetailID { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal Amount { get; set; }
        public string UserName { get; set; }
        public string UserID { get; set; }
        public int ShiftID { get; set; }
        public string ShiftName { get; set; }
    }
}

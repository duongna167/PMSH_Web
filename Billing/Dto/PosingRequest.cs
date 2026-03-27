using Billing.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Billing.Controllers
{
    public class PostingRequest 
    {
        public int UserID { get; set; }
        public string UserName { get; set; }
        public int ShiftID { get; set; }
        public string CashierNo { get; set; }

        public int ReservationID { get; set; }
        public int FolioID { get; set; }
        public int RoomID { get; set; }

        public string Description { get; set; }
        public string CurrencyID { get; set; }

        public int PostType { get; set; }
        public string RoomType { get; set; }

        public string Reference { get; set; }
        public string Supplement { get; set; }
        public decimal AmountBeforeTax { get; set; }
        public decimal AmountGross { get; set; }
        public decimal Amount { get; set; }
        public int OriginFolioID { get; set; }
        public string InvoiceNo { get; set; }
        public string TransactionNo { get; set; }
        public string ReceiptNo { get; set; }
        public DateTime TransactionDate { get; set; }
        public int ProfitCenterID { get; set; }
        public string ProfitCenterCode { get; set; }
        public int TransactionGroupID { get; set; }
        public int TransactionSubgroupID { get; set; }
        public string GroupCode { get; set; }
        public string SubgroupCode { get; set; }
        public int GroupType { get; set; }
        public string TransactionCode { get; set; }
        public string ArticleCode { get; set; }
        public bool Status { get; set; }
        public int RowState { get; set; }
        public int IsSplit { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal AmountMaster { get; set; }
        public string CurrencyMaster { get; set; }
        public int PackageID { get; set; }
        public decimal AmountMasterBeforeTax { get; set; }
        public decimal AmountMasterGross { get; set; }
        public int RoomTypeID { get; set; }
        public int UserInsertID { get; set; }
        public DateTime CreateDate { get; set; }
        public int UserUpdateID { get; set; }
        public DateTime UpdateDate { get; set; }
        public int OriginReservationID { get; set; }
        public string Property { get; set; }
        public string CheckNo { get; set; }
        public string OriginARNo { get; set; }
        public bool IsPostedAR { get; set; }
        public int ARTransID { get; set; }
        public int IsPrintVAT { get; set; }
        public bool IsTransfer { get; set; }
        public string POS_Unclose { get; set; }
    }
}

using BaseBusiness.Model;

namespace Billing.Dto
{
    public class PostingRequest
    {
        public int FolioNo { get; set; }
        public int CurrentFolioID { get; set; }
        public string MasterCode { get; set; }
        public string MasterDescription { get; set; }
        public string MasterReference { get; set; }
        public string MasterSupplement { get; set; }
        public string MasterCheckNo { get; set; }
        public bool IsExpress { get; set; }
        public bool IsDiscount { get; set; }
        public List<FolioDetailModel> Details { get; set; }
    }
}

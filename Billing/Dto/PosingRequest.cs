using BaseBusiness.Model;

namespace Billing.Controllers
{
    public class PostingRequest
    {
        public int FolioNo { get; set; }
        public int CurrentFolioID { get; set; }
        public string MasterCode { get; set; }
        public List<FolioDetailModel> Details { get; set; }
    }
}

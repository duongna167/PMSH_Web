using BaseBusiness.bc;

namespace BaseBusiness.Model
{
    public class LicenseModel : BaseModel
    {
        public int ID { get; set; }

        public string? Name { get; set; }

        public string? BranchName { get; set; }

        public string? Address { get; set; }

        public string? Tel { get; set; }

        public string? Fax { get; set; }

        public string? Email { get; set; }

        public string? Website { get; set; }

        public string? District { get; set; }

        public string? City { get; set; }

        public string? Country { get; set; }

        public string? Version { get; set; }

        public string? Description { get; set; }

        public string? AlarmBefore { get; set; }

        public string? StartDate { get; set; }

        public string? EndDate { get; set; }

        public string? LicenseCode { get; set; }

        public int? SiteID { get; set; }
    }
}
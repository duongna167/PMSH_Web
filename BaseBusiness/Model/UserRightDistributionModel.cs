using BaseBusiness.bc;
using System;

namespace BaseBusiness.Model
{
    public class UserRightDistributionModel : BaseModel
    {
        public int ID { get; set; }

        public int FormAndFunctionID { get; set; }

        public int UserID { get; set; }

        public bool ViewRight { get; set; }

        public bool CreateRight { get; set; }

        public bool ModifyRight { get; set; }

        public bool DeleteRight { get; set; }

        public bool SpecialRight { get; set; }

        public string CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; }

        public string UpdatedBy { get; set; }

        public DateTime UpdatedDate { get; set; }

        public int FormAndFunctionDataID { get; set; }
    }
}

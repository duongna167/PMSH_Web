using BaseBusiness.bc;
using System;

namespace BaseBusiness.Model
{
    public class FormAndFunctionDataGroupModel : BaseModel
    {
        public int ID { get; set; }

        public string Code { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public int ParentID { get; set; }

        public string CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; }

        public string UpdatedBy { get; set; }

        public DateTime UpdatedDate { get; set; }

        public bool IsHide { get; set; }

        public bool Inactive { get; set; }
    }
}

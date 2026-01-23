using BaseBusiness.bc;
using BaseBusiness.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseBusiness.Facade
{
    public class hkpFacilityCategoryFacade : BaseFacadeDB
    {
        protected static hkpFacilityCategoryFacade instance = new hkpFacilityCategoryFacade(new hkpFacilityCategoryModel());
        protected hkpFacilityCategoryFacade(hkpFacilityCategoryModel model) : base(model)
        {
        }
        public static hkpFacilityCategoryFacade Instance
        {
            get { return instance; }
        }
        protected hkpFacilityCategoryFacade() : base()
        {
        }
    }
}

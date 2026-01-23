using BaseBusiness.bc;
using BaseBusiness.Facade;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseBusiness.BO
{
    public class FacilityCategoryBO : BaseBO
    {
        private hkpFacilityCategoryFacade facade = hkpFacilityCategoryFacade.Instance;
        protected static FacilityCategoryBO instance = new FacilityCategoryBO();

        protected FacilityCategoryBO()
        {
            this.baseFacade = facade;
        }

        public static FacilityCategoryBO Instance
        {
            get { return instance; }
        }
    }
}

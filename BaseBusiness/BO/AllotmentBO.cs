using BaseBusiness.bc;
using BaseBusiness.Facade;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseBusiness.BO
{
    public class AllotmentBO : BaseBO
    {
        private AllotmentFacade facade = AllotmentFacade.Instance;
        protected static AllotmentBO instance = new AllotmentBO();

        protected AllotmentBO()
        {
            this.baseFacade = facade;
        }

        public static AllotmentBO Instance
        {
            get { return instance; }
        }

        public bool IsDuplicateCode(string code, long id = 0)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;

            return IsDuplicateCode(
             "Allotment",
             "Code",
             code.Trim(),
             id
            );

        }
    }
}

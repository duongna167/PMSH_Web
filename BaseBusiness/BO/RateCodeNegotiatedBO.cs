using BaseBusiness.bc;
using BaseBusiness.Facade;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseBusiness.BO
{
    public class RateCodeNegotiatedBO : BaseBO
    {
        private RateCodeNegotiatedFacade facade = RateCodeNegotiatedFacade.Instance;
        protected static RateCodeNegotiatedBO instance = new RateCodeNegotiatedBO();

        protected RateCodeNegotiatedBO()
        {
            this.baseFacade = facade;
        }

        public static RateCodeNegotiatedBO Instance
        {
            get { return instance; }
        }
    }
}

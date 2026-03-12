using BaseBusiness.bc;
using BaseBusiness.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseBusiness.Facade
{
    public class RateCodeNegotiatedFacade : BaseFacadeDB
    {
        protected static RateCodeNegotiatedFacade instance = new RateCodeNegotiatedFacade(new RateCodeNegotiatedModel());
        protected RateCodeNegotiatedFacade(RateCodeNegotiatedModel model) : base(model)
        {
        }
        public static RateCodeNegotiatedFacade Instance
        {
            get { return instance; }
        }
        protected RateCodeNegotiatedFacade() : base()
        {
        }
    }
}

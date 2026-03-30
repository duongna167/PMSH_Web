using BaseBusiness.bc;
using BaseBusiness.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseBusiness.Facade
{
    public class LicenseFacade : BaseFacadeDB
    {
        protected static LicenseFacade instance = new(new LicenseModel());
        protected LicenseFacade(LicenseModel model) : base(model)
        {
        }
        public static LicenseFacade Instance
        {
            get { return instance; }
        }
        protected LicenseFacade() : base()
        {
        }
    }
}

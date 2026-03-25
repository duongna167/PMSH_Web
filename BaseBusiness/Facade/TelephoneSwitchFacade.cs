using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaseBusiness.bc;
using BaseBusiness.Model;

namespace BaseBusiness.Facade
{
    public class TelephoneSwitchFacade : BaseFacadeDB
    {
        protected static TelephoneSwitchFacade instance = new(new TelephoneSwitchModel());
        protected TelephoneSwitchFacade(TelephoneSwitchModel model) : base(model)
        {
        }
        public static TelephoneSwitchFacade Instance
        {
            get { return instance; }
        }
        protected TelephoneSwitchFacade() : base()
        {
        }
    }
}

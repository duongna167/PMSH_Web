using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaseBusiness.bc;
using BaseBusiness.Facade;

namespace BaseBusiness.BO
{
    public class TelephoneSwitchBO : BaseBO
    {
        private readonly TelephoneSwitchFacade facade = TelephoneSwitchFacade.Instance;
        protected static TelephoneSwitchBO instance = new();

        protected TelephoneSwitchBO()
        {
            baseFacade = facade;
        }
        public static TelephoneSwitchBO Instance
        {
            get { return instance; }
        }

    }
}

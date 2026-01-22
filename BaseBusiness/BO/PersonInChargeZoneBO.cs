using BaseBusiness.bc;
using BaseBusiness.Facade;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseBusiness.BO
{
    public class PersonInChargeZoneBO : BaseBO
    {
        private PersonInChargeZoneFacade facade = PersonInChargeZoneFacade.Instance;
        protected static PersonInChargeZoneBO instance = new PersonInChargeZoneBO();

        protected PersonInChargeZoneBO()
        {
            this.baseFacade = facade;
        }

        public static PersonInChargeZoneBO Instance
        {
            get { return instance; }
        }


    }
}

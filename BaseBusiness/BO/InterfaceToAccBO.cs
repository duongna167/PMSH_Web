using BaseBusiness.bc;
using BaseBusiness.Facade;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseBusiness.BO
{
    public class InterfaceToAccBO : BaseBO
    {
        private InterfaceToAccFacade facade = InterfaceToAccFacade.Instance;
        protected static InterfaceToAccBO instance = new InterfaceToAccBO();

        protected InterfaceToAccBO()
        {
            this.baseFacade = facade;
        }

        public static InterfaceToAccBO Instance
        {
            get { return instance; }
        }
    }
}

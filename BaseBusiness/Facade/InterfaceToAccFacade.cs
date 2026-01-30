using BaseBusiness.bc;
using BaseBusiness.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseBusiness.Facade
{
    public class InterfaceToAccFacade : BaseFacadeDB
    {
        protected static InterfaceToAccFacade instance = new InterfaceToAccFacade(new InterfaceToAccModel());
        protected InterfaceToAccFacade(InterfaceToAccModel model) : base(model)
        {
        }
        public static InterfaceToAccFacade Instance
        {
            get { return instance; }
        }
        protected InterfaceToAccFacade() : base()
        {
        }
    }
}

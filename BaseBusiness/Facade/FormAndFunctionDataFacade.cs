using BaseBusiness.bc;
using BaseBusiness.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseBusiness.Facade
{
    public class FormAndFunctionDataFacade : BaseFacadeDB
    {
        protected static FormAndFunctionDataFacade instance = new FormAndFunctionDataFacade(new FormAndFunctionDataModel());
        protected FormAndFunctionDataFacade(FormAndFunctionDataModel model) : base(model)
        {
        }
        public static FormAndFunctionDataFacade Instance
        {
            get { return instance; }
        }
        protected FormAndFunctionDataFacade() : base()
        {
        }
    }
}

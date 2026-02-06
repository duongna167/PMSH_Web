using BaseBusiness.bc;
using BaseBusiness.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseBusiness.Facade
{
    public class FormAndFunctionGroupFacade : BaseFacadeDB
    {
        protected static FormAndFunctionGroupFacade instance = new FormAndFunctionGroupFacade(new FormAndFunctionGroupModel());
        protected FormAndFunctionGroupFacade(FormAndFunctionGroupModel model) : base(model)
        {
        }
        public static FormAndFunctionGroupFacade Instance
        {
            get { return instance; }
        }
        protected FormAndFunctionGroupFacade() : base()
        {
        }
    }
}

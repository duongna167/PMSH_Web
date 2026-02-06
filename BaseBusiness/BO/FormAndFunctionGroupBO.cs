using BaseBusiness.bc;
using BaseBusiness.Facade;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseBusiness.BO
{
    public class FormAndFunctionGroupBO : BaseBO
    {
        private FormAndFunctionGroupFacade facade = FormAndFunctionGroupFacade.Instance;
        protected static FormAndFunctionGroupBO instance = new FormAndFunctionGroupBO();

        protected FormAndFunctionGroupBO()
        {
            this.baseFacade = facade;
        }

        public static FormAndFunctionGroupBO Instance
        {
            get { return instance; }
        }
    }
}

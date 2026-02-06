using BaseBusiness.bc;
using BaseBusiness.Facade;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseBusiness.BO
{
    public class FormAndFunctionDataBO : BaseBO
    {
        private FormAndFunctionDataFacade facade = FormAndFunctionDataFacade.Instance;
        protected static FormAndFunctionDataBO instance = new FormAndFunctionDataBO();

        protected FormAndFunctionDataBO()
        {
            this.baseFacade = facade;
        }

        public static FormAndFunctionDataBO Instance
        {
            get { return instance; }
        }
    }
}

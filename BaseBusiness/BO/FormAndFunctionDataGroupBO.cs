using BaseBusiness.bc;
using BaseBusiness.Facade;

namespace BaseBusiness.BO
{
    public class FormAndFunctionDataGroupBO : BaseBO
    {
        private FormAndFunctionDataGroupFacade facade = FormAndFunctionDataGroupFacade.Instance;
        protected static FormAndFunctionDataGroupBO instance = new FormAndFunctionDataGroupBO();

        protected FormAndFunctionDataGroupBO()
        {
            this.baseFacade = facade;
        }

        public static FormAndFunctionDataGroupBO Instance
        {
            get { return instance; }
        }
    }
}

using BaseBusiness.bc;
using BaseBusiness.Model;

namespace BaseBusiness.Facade
{
    public class FormAndFunctionDataGroupFacade : BaseFacadeDB
    {
        protected static FormAndFunctionDataGroupFacade instance = new FormAndFunctionDataGroupFacade(new FormAndFunctionDataGroupModel());
        protected FormAndFunctionDataGroupFacade(FormAndFunctionDataGroupModel model) : base(model)
        {
        }
        public static FormAndFunctionDataGroupFacade Instance
        {
            get { return instance; }
        }
        protected FormAndFunctionDataGroupFacade() : base()
        {
        }
    }
}

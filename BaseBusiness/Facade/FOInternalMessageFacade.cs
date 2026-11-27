using BaseBusiness.bc;
using BaseBusiness.Model;

namespace BaseBusiness.Facade
{
    public class FOInternalMessageFacade : BaseFacadeDB
    {
        protected static FOInternalMessageFacade instance = new(new FOInternalMessageModel());
        protected FOInternalMessageFacade(FOInternalMessageModel model) : base(model) { }
        public static FOInternalMessageFacade Instance => instance;
        protected FOInternalMessageFacade() : base() { }
    }
}

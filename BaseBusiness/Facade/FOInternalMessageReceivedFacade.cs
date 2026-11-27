using BaseBusiness.bc;
using BaseBusiness.Model;

namespace BaseBusiness.Facade
{
    public class FOInternalMessageReceivedFacade : BaseFacadeDB
    {
        protected static FOInternalMessageReceivedFacade instance = new(new FOInternalMessageReceivedModel());
        protected FOInternalMessageReceivedFacade(FOInternalMessageReceivedModel model) : base(model) { }
        public static FOInternalMessageReceivedFacade Instance => instance;
        protected FOInternalMessageReceivedFacade() : base() { }
    }
}

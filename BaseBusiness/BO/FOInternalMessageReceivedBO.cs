using BaseBusiness.bc;
using BaseBusiness.Facade;

namespace BaseBusiness.BO
{
    public class FOInternalMessageReceivedBO : BaseBO
    {
        private readonly FOInternalMessageReceivedFacade facade = FOInternalMessageReceivedFacade.Instance;
        protected static FOInternalMessageReceivedBO instance = new();

        protected FOInternalMessageReceivedBO()
        {
            baseFacade = facade;
        }

        public static FOInternalMessageReceivedBO Instance => instance;
    }
}

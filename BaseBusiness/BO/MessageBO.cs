using BaseBusiness.bc;
using BaseBusiness.Facade;

namespace BaseBusiness.BO
{
    public class MessageBO : BaseBO
    {
        private readonly MessageFacade facade = MessageFacade.Instance;
        protected static MessageBO instance = new();

        protected MessageBO()
        {
            baseFacade = facade;
        }

        public static MessageBO Instance
        {
            get { return instance; }
        }

    }
}

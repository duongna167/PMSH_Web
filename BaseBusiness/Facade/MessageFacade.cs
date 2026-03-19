using BaseBusiness.bc;
using BaseBusiness.Model;


namespace BaseBusiness.Facade
{
    public class MessageFacade : BaseFacadeDB
    {
        protected static MessageFacade instance = new(new MessageModel());
        protected MessageFacade(MessageModel model) : base(model)
        {
        }
        public static MessageFacade Instance
        {
            get { return instance; }
        }
        protected MessageFacade() : base()
        {
        }
    }
}

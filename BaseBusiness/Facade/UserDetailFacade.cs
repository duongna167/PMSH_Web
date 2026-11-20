using BaseBusiness.bc;
using BaseBusiness.Model;

namespace BaseBusiness.Facade
{
    public class UserDetailFacade : BaseFacadeDB
    {
        protected static UserDetailFacade instance = new UserDetailFacade(new UserDetailModel());
        protected UserDetailFacade(UserDetailModel model) : base(model)
        {
        }
        public static UserDetailFacade Instance
        {
            get { return instance; }
        }
        protected UserDetailFacade() : base()
        {
        }
    }
}

using BaseBusiness.bc;
using BaseBusiness.Model;

namespace BaseBusiness.Facade
{
    public class UserRightDistributionFacade : BaseFacadeDB
    {
        protected static UserRightDistributionFacade instance = new UserRightDistributionFacade(new UserRightDistributionModel());
        protected UserRightDistributionFacade(UserRightDistributionModel model) : base(model)
        {
        }
        public static UserRightDistributionFacade Instance
        {
            get { return instance; }
        }
        protected UserRightDistributionFacade() : base()
        {
        }
    }
}

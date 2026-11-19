using BaseBusiness.bc;
using BaseBusiness.Facade;

namespace BaseBusiness.BO
{
    public class UserRightDistributionBO : BaseBO
    {
        private UserRightDistributionFacade facade = UserRightDistributionFacade.Instance;
        protected static UserRightDistributionBO instance = new UserRightDistributionBO();

        protected UserRightDistributionBO()
        {
            this.baseFacade = facade;
        }

        public static UserRightDistributionBO Instance
        {
            get { return instance; }
        }
    }
}

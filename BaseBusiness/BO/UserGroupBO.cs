using BaseBusiness.bc;
using BaseBusiness.Facade;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseBusiness.BO
{
    public class UserGroupBO : BaseBO
    {
        private UserGroupFacade facade = UserGroupFacade.Instance;
        protected static UserGroupBO instance = new UserGroupBO();

        protected UserGroupBO()
        {
            this.baseFacade = facade;
        }

        public static UserGroupBO Instance
        {
            get { return instance; }
        }
    }
}

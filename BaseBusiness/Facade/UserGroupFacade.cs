using BaseBusiness.bc;
using BaseBusiness.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseBusiness.Facade
{
    public class UserGroupFacade : BaseFacadeDB
    {
        protected static UserGroupFacade instance = new UserGroupFacade(new UserGroupModel());
        protected UserGroupFacade(UserGroupModel model) : base(model)
        {
        }
        public static UserGroupFacade Instance
        {
            get { return instance; }
        }
        protected UserGroupFacade() : base()
        {
        }
    }
}

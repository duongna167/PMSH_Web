using BaseBusiness.bc;
using BaseBusiness.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseBusiness.Facade
{
    public class GroupOwnerFacade : BaseFacadeDB
    {
        protected static GroupOwnerFacade instance = new GroupOwnerFacade(new GroupOwnerModel());
        protected GroupOwnerFacade(GroupOwnerModel model) : base(model)
        {
        }
        public static GroupOwnerFacade Instance
        {
            get { return instance; }
        }
        protected GroupOwnerFacade() : base()
        {
        }
    }
}

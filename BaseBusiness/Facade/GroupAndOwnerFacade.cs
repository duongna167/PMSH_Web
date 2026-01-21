using BaseBusiness.bc;
using BaseBusiness.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseBusiness.Facade
{
    public class GroupAndOwnerFacade : BaseFacadeDB
    {
        protected static GroupAndOwnerFacade instance = new GroupAndOwnerFacade(new GroupAndOwnerModel());
        protected GroupAndOwnerFacade(GroupAndOwnerModel model) : base(model)
        {
        }
        public static GroupAndOwnerFacade Instance
        {
            get { return instance; }
        }
        protected GroupAndOwnerFacade() : base()
        {
        }
    }
}

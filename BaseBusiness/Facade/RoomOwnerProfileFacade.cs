using BaseBusiness.bc;
using BaseBusiness.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseBusiness.Facade
{
    public class RoomOwnerProfileFacade : BaseFacadeDB
    {
        protected static RoomOwnerProfileFacade instance = new RoomOwnerProfileFacade(new RoomOwnerProfileModel());
        protected RoomOwnerProfileFacade(RoomOwnerProfileModel model) : base(model)
        {
        }
        public static RoomOwnerProfileFacade Instance
        {
            get { return instance; }
        }
        protected RoomOwnerProfileFacade() : base()
        {
        }
    }
}

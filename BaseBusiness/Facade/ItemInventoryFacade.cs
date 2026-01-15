using BaseBusiness.bc;
using BaseBusiness.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseBusiness.Facade
{
    public class ItemInventoryFacade : BaseFacadeDB
    {
        protected static ItemInventoryFacade instance = new ItemInventoryFacade(new ItemInventoryModel());
        protected ItemInventoryFacade(ItemInventoryModel model) : base(model)
        {
        }
        public static ItemInventoryFacade Instance
        {
            get { return instance; }
        }
        protected ItemInventoryFacade() : base()
        {
        }
    }
}

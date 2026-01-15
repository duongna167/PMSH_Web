using BaseBusiness.bc;
using BaseBusiness.Facade;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseBusiness.BO
{
    public class ItemInventoryBO : BaseBO
    {
        private ItemInventoryFacade facade = ItemInventoryFacade.Instance;
        protected static ItemInventoryBO instance = new ItemInventoryBO();

        protected ItemInventoryBO()
        {
            this.baseFacade = facade;
        }

        public static ItemInventoryBO Instance
        {
            get { return instance; }
        }
    }
}

using BaseBusiness.bc;
using BaseBusiness.Facade;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseBusiness.BO
{
    public class CardBO : BaseBO
    {
        private CardFacade facade = CardFacade.Instance;
        protected static CardBO instance = new CardBO();

        protected CardBO()
        {
            this.baseFacade = facade;
        }

        public static CardBO Instance
        {
            get { return instance; }
        }

    }
}

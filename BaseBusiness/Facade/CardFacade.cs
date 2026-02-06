using BaseBusiness.bc;
using BaseBusiness.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseBusiness.Facade
{
    public class CardFacade : BaseFacadeDB
    {
        protected static CardFacade instance = new CardFacade(new CardModel());
        protected CardFacade(CardModel model) : base(model)
        {
        }
        public static CardFacade Instance
        {
            get { return instance; }
        }
        protected CardFacade() : base()
        {
        }
    }
}

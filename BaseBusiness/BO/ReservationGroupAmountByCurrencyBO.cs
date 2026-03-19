using BaseBusiness.bc;
using BaseBusiness.Facade;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseBusiness.BO
{
    public class ReservationGroupAmountByCurrencyBO : BaseBO
    {
        private readonly ReservationGroupAmountByCurrencyFacade facade = ReservationGroupAmountByCurrencyFacade.Instance;
        protected static ReservationGroupAmountByCurrencyBO instance = new();

        protected ReservationGroupAmountByCurrencyBO()
        {
            this.baseFacade = facade;
        }

        public static ReservationGroupAmountByCurrencyBO Instance
        {
            get { return instance; }
        }
        
    }
}

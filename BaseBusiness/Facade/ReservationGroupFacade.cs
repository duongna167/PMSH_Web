using BaseBusiness.bc;
using BaseBusiness.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseBusiness.Facade
{
    public class ReservationGroupFacade : BaseFacadeDB
    {
        protected static ReservationGroupFacade instance = new(new ReservationGroupModel());
        protected ReservationGroupFacade(ReservationGroupModel model) : base(model)
        {
        }
        public static ReservationGroupFacade Instance
        {
            get { return instance; }
        }
        protected ReservationGroupFacade() : base()
        {
        }
    }
}

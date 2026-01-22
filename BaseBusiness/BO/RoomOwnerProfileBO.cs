using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaseBusiness.bc;
using BaseBusiness.Facade;
using BaseBusiness.Model;
using Microsoft.Data.SqlClient;
namespace BaseBusiness.BO
{
    using Dapper;
    public class RoomOwnerProfileBO : BaseBO
    {
        private RoomOwnerProfileFacade facade = RoomOwnerProfileFacade.Instance;
        protected static RoomOwnerProfileBO instance = new RoomOwnerProfileBO();

        protected RoomOwnerProfileBO()
        {
            this.baseFacade = facade;
        }

        public static RoomOwnerProfileBO Instance
        {
            get { return instance; }
        }

    }
}

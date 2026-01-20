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
    public class GroupOwnerBO : BaseBO
    {
        private GroupOwnerFacade facade = GroupOwnerFacade.Instance;
        protected static GroupOwnerBO instance = new GroupOwnerBO();

        protected GroupOwnerBO()
        {
            this.baseFacade = facade;
        }

        public static GroupOwnerBO Instance
        {
            get { return instance; }
        }

    }
}

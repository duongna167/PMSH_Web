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
    public class GroupAndOwnerBO : BaseBO
    {
        private GroupAndOwnerFacade facade = GroupAndOwnerFacade.Instance;
        protected static GroupAndOwnerBO instance = new GroupAndOwnerBO();

        protected GroupAndOwnerBO()
        {
            this.baseFacade = facade;
        }

        public static GroupAndOwnerBO Instance
        {
            get { return instance; }
        }

    }
}

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
    public class LicenseBO : BaseBO
    {
        private readonly LicenseFacade facade = LicenseFacade.Instance;
        protected static LicenseBO instance = new();

        protected LicenseBO()
        {
            this.baseFacade = facade;
        }

        public static LicenseBO Instance
        {
            get { return instance; }
        }
    }
}

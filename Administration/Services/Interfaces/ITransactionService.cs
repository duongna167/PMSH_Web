using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Administration.Services.Interfaces
{
    public interface ITransactionService
    {
        public DataTable SearchTransaction(string code, string description, int groupID, int subGroupID);
        public DataTable GetTransactionSubGroup();

    }
}

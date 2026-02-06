using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Security.Services.Interfaces
{
    public interface ISecurityService
    {
        DataTable AddFuncitionsToListData(string codetag, string namerights, int isDataRight);
    }
}

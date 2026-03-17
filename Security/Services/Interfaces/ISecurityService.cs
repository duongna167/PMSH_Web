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
        DataTable UserGroupData(string? Code, string? Name, int inactive = 0);
        DataTable AddFuncitionsToListData(string codetag, string namerights, int isDataRight);
        DataTable UsersManagementData(string lastName, string firstName, string loginName, int userStatus, int cashierStatus, string jobtitle, string department);
    }
}

using System.Collections.Generic;
using System.Data;
using BaseBusiness.Model;
using Security.Helpers;

namespace Security.Services.Interfaces
{
    public interface IUsersManagementService
    {
        List<UserGroupModel> Groups();
        List<JobTitleModel> JobTitles();
        List<DepartmentModel> Departments();
        DataTable Search(string lastName, string firstName, string loginName, int userStatus, int cashierStatus, string jobtitle, string department);
        UsersModel? GetById(int id);
        OpResult Save(UsersModel model);
        OpResult ResetPassword(UsersModel model);
        OpResult SendResetPasswordEmail(int id, string plainPassword, int sendBy);
        OpResult Lock(int id);
        OpResult Delete(int id);
    }
}

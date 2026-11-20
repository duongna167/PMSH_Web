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
        OpResult Save(UsersModel model, string computerIp = "", int actorUserId = 0, string actorUserName = "");
        OpResult ResetPassword(UsersModel model, string computerIp = "", int actorUserId = 0, string actorUserName = "");
        OpResult SendResetPasswordEmail(int id, string plainPassword, int sendBy);
        OpResult Delete(int id, int actorUserId = 0, string actorUserName = "");
    }
}

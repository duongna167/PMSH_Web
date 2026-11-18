using BaseBusiness.Model;
using System.Data;

namespace User.Services.Interfaces
{
    public interface IUserService
    {
        UsersModel Login(string LoginName, string Password);
        UsersModel FindByLoginName(string loginName);
        bool VerifyPassword(UsersModel user, string password);
        void LockAccount(UsersModel user);
        void InsertLoginActivityLog(int objectId, string loginName, string change, string oldValue, string newValue, string description);
        bool ChangePassword(int userId, string currentPassword, string newPassword, bool requireCurrent, out string error);
        DataTable PermissionNames(int UserGroupID, int UserID);
    }
}

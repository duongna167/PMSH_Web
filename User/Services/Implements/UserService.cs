using BaseBusiness.BO;
using BaseBusiness.Model;
using BaseBusiness.util;
using BaseBusiness.Util;
using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Linq;
using User.Services.Interfaces;

namespace User.Services.Implements
{
    public class UserService : IUserService
    {
        public UsersModel Login(string LoginName, string Password)
        {
            try
            {
                var user = FindByLoginName(LoginName);
                if (user == null || user.ID == 0 || !VerifyPassword(user, Password))
                    return new UsersModel();
                return user;
            }
            catch
            {
                return new UsersModel();
            }
        }

        public UsersModel FindByLoginName(string loginName)
        {
            var name = (loginName ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(name))
                return null;

            var list = PropertyUtils.ConvertToList<UsersModel>(UsersBO.Instance.FindAll());
            return list?.FirstOrDefault(x =>
                !string.IsNullOrWhiteSpace(x.LoginName) &&
                string.Equals(x.LoginName.Trim(), name, StringComparison.OrdinalIgnoreCase));
        }

        public bool VerifyPassword(UsersModel user, string password)
        {
            if (user == null || string.IsNullOrEmpty(user.PasswordHash) || password == null)
                return false;

            var stored = user.PasswordHash;
            if (string.Equals(stored, EncryptPassword(password), StringComparison.Ordinal))
                return true;
            if (string.Equals(stored, MD5.Encrypt(password), StringComparison.Ordinal))
                return true;

            try
            {
                var plain = DBUtils.Decrypt(
                    stored,
                    DBUtils.passPhrase,
                    DBUtils.saltValue,
                    DBUtils.hashAlgorithm,
                    DBUtils.passwordIterations,
                    DBUtils.initVector,
                    DBUtils.keySize);
                return string.Equals(plain, password, StringComparison.Ordinal);
            }
            catch
            {
                return false;
            }
        }

        public void LockAccount(UsersModel user)
        {
            if (user == null || user.ID == 0)
                return;

            var current = (UsersModel)UsersBO.Instance.FindByPrimaryKey(user.ID);
            if (current == null)
                return;

            current.Status = 0;
            current.UpdateDate = DateTime.Now;
            UsersBO.Instance.Update(current);
            user.Status = 0;
        }

        public void InsertLoginActivityLog(int objectId, string loginName, string change, string oldValue, string newValue, string description)
        {
            try
            {
                ActivityLogBO.Instance.Insert(new ActivityLogModel
                {
                    TableName = "Users",
                    ObjectID = objectId,
                    UserID = objectId,
                    UserName = loginName ?? string.Empty,
                    ChangeDate = TextUtils.GetSystemDate(),
                    Change = change,
                    OldValue = oldValue ?? string.Empty,
                    NewValue = newValue ?? string.Empty,
                    Description = description ?? string.Empty
                });
            }
            catch
            {
            }
        }

        public bool ChangePassword(int userId, string currentPassword, string newPassword, bool requireCurrent, out string error)
        {
            error = null;
            var user = (UsersModel)UsersBO.Instance.FindByPrimaryKey(userId);
            if (user == null || user.ID == 0)
            {
                error = "The user account was not found.";
                return false;
            }

            if (string.IsNullOrEmpty(newPassword))
            {
                error = "New password is required.";
                return false;
            }

            if (!HasLetterAndDigit(newPassword))
            {
                error = "The password must contain both letters and numbers.";
                return false;
            }

            if (requireCurrent)
            {
                if (string.IsNullOrEmpty(currentPassword) || !VerifyPassword(user, currentPassword))
                {
                    error = "The current password is incorrect.";
                    return false;
                }

                if (string.Equals(currentPassword, newPassword, StringComparison.Ordinal))
                {
                    error = "The new password must be different from the current password.";
                    return false;
                }
            }

            user.PasswordHash = EncryptPassword(newPassword);
            user.PasswordStatus = true;
            user.PassExpireDate = DateTime.Now.Date.AddDays(60);
            user.UpdateDate = DateTime.Now;
            UsersBO.Instance.Update(user);
            return true;
        }

        public DataTable PermissionNames(int UserGroupID, int UserID)
        {
            return LoadEffectiveCodes(UserGroupID, UserID, isDataRight: false);
        }

        public DataTable PermissionDataCodes(int UserGroupID, int UserID)
        {
            return LoadEffectiveCodes(UserGroupID, UserID, isDataRight: true);
        }

        private static DataTable LoadEffectiveCodes(int userGroupId, int userId, bool isDataRight)
        {
            if (userGroupId <= 0 && userId > 0)
            {
                if (UsersBO.Instance.FindByPrimaryKey(userId) is UsersModel user)
                    userGroupId = user.UserGroupID;
            }

            var idColumn = isDataRight ? "FormAndFunctionDataID" : "FormAndFunctionID";
            var catalog = isDataRight ? "FormAndFunctionData" : "FormAndFunction";
            var inactiveFilter = isDataRight ? "" : " AND ISNULL(ff.Inactive, 0) = 0";

            var sql = $@"
;WITH GroupIds AS (
    SELECT {idColumn} AS Id
    FROM UserGroupRightDistribution WITH (NOLOCK)
    WHERE UserGroupID = @UserGroupID AND ISNULL({idColumn}, 0) > 0
),
UserIds AS (
    SELECT {idColumn} AS Id
    FROM UserRightDistribution WITH (NOLOCK)
    WHERE UserID = @UserID AND ISNULL({idColumn}, 0) > 0
),
Effective AS (
    SELECT Id FROM GroupIds
    UNION
    SELECT Id FROM UserIds
    EXCEPT
    SELECT g.Id FROM GroupIds g INNER JOIN UserIds u ON g.Id = u.Id
),
Allowed AS (
    SELECT ff.*
    FROM Effective e
    INNER JOIN {catalog} ff WITH (NOLOCK) ON ff.ID = e.Id
    WHERE ISNULL(ff.IsHide, 0) = 0
      {inactiveFilter}
      AND LTRIM(RTRIM(ISNULL(ff.Code, N''))) <> N''
)
SELECT LTRIM(RTRIM(ff.Code)) AS Code,
       LTRIM(RTRIM(ff.Code)) AS Name,
       LTRIM(RTRIM(ff.Name)) AS DisplayName
FROM Allowed ff";

            return DataTableHelper.ExecuteQuery(sql, new[]
            {
                new SqlParameter("@UserGroupID", userGroupId),
                new SqlParameter("@UserID", userId)
            });
        }

        private static string EncryptPassword(string plainText)
        {
            return DBUtils.Encrypt(
                plainText ?? string.Empty,
                DBUtils.passPhrase,
                DBUtils.saltValue,
                DBUtils.hashAlgorithm,
                DBUtils.passwordIterations,
                DBUtils.initVector,
                DBUtils.keySize);
        }

        private static bool HasLetterAndDigit(string password)
        {
            bool letter = false;
            bool digit = false;
            foreach (var c in password)
            {
                if (char.IsLetter(c)) letter = true;
                else if (char.IsDigit(c)) digit = true;
                if (letter && digit) return true;
            }
            return false;
        }
    }
}

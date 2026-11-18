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
            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@UserGroupID", UserGroupID),
               new SqlParameter("@UserID", UserID),
            };

            DataTable myTable = DataTableHelper.getTableData("GetPermissionNamesByUserGroup", param);
            return myTable;
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

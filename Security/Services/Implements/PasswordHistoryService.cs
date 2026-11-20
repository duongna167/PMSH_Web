using System.Collections.Generic;
using System.Linq;
using BaseBusiness.BO;
using BaseBusiness.Model;
using BaseBusiness.util;
using Security.Services.Interfaces;

namespace Security.Services.Implements
{
    public class PasswordHistoryService : IPasswordHistoryService
    {
        public List<UsersModel> Users()
        {
            var list = PropertyUtils.ConvertToList<UsersModel>(UsersBO.Instance.FindAll()) ?? new List<UsersModel>();
            return list
                .Where(u => u != null && !string.IsNullOrWhiteSpace(u.LoginName))
                .OrderBy(u => u.LoginName)
                .ToList();
        }

        public List<UserDetailHistoryRow> Search(int userId, int type)
        {
            return UserDetailBO.Instance.Search(userId, type) ?? new List<UserDetailHistoryRow>();
        }
    }
}

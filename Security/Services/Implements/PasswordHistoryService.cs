using System.Collections.Generic;
using BaseBusiness.BO;
using Security.Services.Interfaces;

namespace Security.Services.Implements
{
    public class PasswordHistoryService : IPasswordHistoryService
    {
        public List<UserDetailHistoryRow> Search(int userId, int type, string? username = null)
        {
            return UserDetailBO.Instance.Search(userId, type, username) ?? new List<UserDetailHistoryRow>();
        }
    }
}

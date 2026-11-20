using System.Collections.Generic;
using BaseBusiness.BO;
using BaseBusiness.Model;

namespace Security.Services.Interfaces
{
    public interface IPasswordHistoryService
    {
        List<UsersModel> Users();
        List<UserDetailHistoryRow> Search(int userId, int type);
    }
}

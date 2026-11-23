using System.Collections.Generic;
using BaseBusiness.BO;

namespace Security.Services.Interfaces
{
    public interface IPasswordHistoryService
    {
        List<UserDetailHistoryRow> Search(int userId, int type, string? username = null);
    }
}

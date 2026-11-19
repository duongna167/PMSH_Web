using System.Collections.Generic;
using Security.Helpers;

namespace Security.Services.Interfaces
{
    public interface IUserMappingService
    {
        List<object> Users();
        List<object> Groups();
        List<object> GroupUsers(int groupId);
        List<object> Catalog(bool isDataRight);
        List<int> GrantedIds(int ownerId, bool isDataRight, bool isUser);
        List<object> RightTree(int ownerId, bool isDataRight, bool isUser);
        OpResult SaveUserRights(int userId, bool isDataRight, string ids, string login);
        OpResult SaveGroupRights(int groupId, bool isDataRight, string ids, string login);
    }
}

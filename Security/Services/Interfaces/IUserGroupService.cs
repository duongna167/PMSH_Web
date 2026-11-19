using System.Data;
using Security.Helpers;

namespace Security.Services.Interfaces
{
    public interface IUserGroupService
    {
        DataTable Search(string? code, string? name, int inactive = 0);
        OpResult Save(string id, string codeUG, string nameUG, string description, string user, string inactive);
        OpResult Delete(int id);
    }
}

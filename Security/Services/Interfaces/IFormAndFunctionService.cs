using System.Collections.Generic;
using BaseBusiness.Model;
using Security.Helpers;

namespace Security.Services.Interfaces
{
    public interface IFormAndFunctionService
    {
        List<FormAndFunctionGroupModel> FunctionGroups();
        List<FormAndFunctionDataGroupModel> DataGroups();
        List<object> SearchGrid(string codetag, string namerights, int isDataRight);
        object Detail(int id, int isDataRight);
        OpResult Save(int id, string code, string name, string description, int groupId, int isHide, int isDataRight, string userName);
        OpResult Delete(int id, int isDataRight);
        List<object> GroupTree(int isDataRight);
        object GroupDetail(int id, int isDataRight);
        OpResult SaveGroup(int id, string code, string name, string description, int parentId, int isHide, int isDataRight, string userName);
        OpResult DeleteGroup(int id, int isDataRight);
    }
}

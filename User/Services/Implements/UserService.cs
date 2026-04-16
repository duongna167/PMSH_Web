using BaseBusiness.BO;
using BaseBusiness.Model;
using BaseBusiness.util;
using BaseBusiness.Util;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using User.Services.Interfaces;

namespace User.Services.Implements
{
    public class UserService : IUserService
    {
        public UsersModel Login(string LoginName, string Password)
        {
            try
            {
                string PasswordHash = MD5.Encrypt(Password);
                var list = PropertyUtils.ConvertToList<UsersModel>(UsersBO.Instance.FindAll()).Where(x => x.LoginName == LoginName && x.PasswordHash == PasswordHash && x.Status==1).ToList();
                if (list.Count > 0) {
                    return list[0];
                }
                return new UsersModel();
            }
            catch (Exception ex) { 
                return new UsersModel();
            }
        }
        public DataTable PermissionNames(int UserGroupID,int UserID)
        {
            SqlParameter[] param = new SqlParameter[]
            {
               new SqlParameter("@UserGroupID", UserGroupID),
                  new SqlParameter("@UserID", UserID),
            };

            DataTable myTable = DataTableHelper.getTableData("GetPermissionNamesByUserGroup", param);
            return myTable;
        }
    }
}

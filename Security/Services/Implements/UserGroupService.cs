using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using BaseBusiness.BO;
using BaseBusiness.Model;
using BaseBusiness.util;
using Microsoft.Data.SqlClient;
using Security.Helpers;
using Security.Services.Interfaces;

namespace Security.Services.Implements
{
    public class UserGroupService : IUserGroupService
    {
        public DataTable Search(string? code, string? name, int inactive = 0)
        {
            SqlParameter[] param =
            [
                new SqlParameter("@Code", code ?? string.Empty),
                new SqlParameter("@Name", name ?? string.Empty),
                new SqlParameter("@Inactive", inactive)
            ];
            return DataTableHelper.getTableData("spFrmUserGroupSearch", param);
        }

        public OpResult Save(string id, string codeUG, string nameUG, string description, string user, string inactive)
        {
            var errors = new List<object>();
            if (string.IsNullOrWhiteSpace(codeUG))
                errors.Add(new { field = "code", message = "Code is required." });
            else if (codeUG.Length > 50)
                errors.Add(new { field = "code", message = "Code must be at most 50 characters." });
            if (string.IsNullOrWhiteSpace(nameUG))
                errors.Add(new { field = "name", message = "Name is required." });
            else if (nameUG.Length > 50)
                errors.Add(new { field = "name", message = "Name must be at most 50 characters." });
            if (!string.IsNullOrEmpty(description) && description.Length > 500)
                errors.Add(new { field = "description", message = "Description must be at most 500 characters." });
            if (string.IsNullOrWhiteSpace(user))
                return OpResult.Missing("User not found.");
            user = user.Trim().Trim('"');
            if (user.Length > 100)
                return OpResult.Missing("User must be at most 100 characters.");
            if (inactive != "0" && inactive != "1")
                errors.Add(new { field = "ckActive", message = "Active must be checked or uncheked." });

            int parsedId = 0;
            bool isUpdate = false;
            if (!string.IsNullOrWhiteSpace(id) && id != "0")
            {
                if (!int.TryParse(id, out parsedId) || parsedId <= 0)
                    return OpResult.Missing("Invalid User Group ID format.");
                isUpdate = true;
            }

            if (!string.IsNullOrWhiteSpace(codeUG))
            {
                var all = PropertyUtils.ConvertToList<UserGroupModel>(UserGroupBO.Instance.FindAll()) ?? [];
                if (all.Any(r => string.Equals(r.Code?.Trim(), codeUG.Trim(), StringComparison.OrdinalIgnoreCase) && r.ID != parsedId))
                    errors.Add(new { field = "code", message = "Code already exists." });
            }
            if (errors.Count != 0)
                return OpResult.Fail("Validation failed.", errors);

            var systemNow = TextUtils.GetSystemDate();
            var model = new UserGroupModel
            {
                Code = codeUG.Trim(),
                Name = nameUG.Trim(),
                Description = description ?? string.Empty,
                Inactive = inactive == "1"
            };
            if (isUpdate)
            {
                if (UserGroupBO.Instance.FindByPrimaryKey(parsedId) is not UserGroupModel existing || existing.ID == 0)
                    return OpResult.Missing($"User Group not found (ID = {parsedId})");
                model.ID = parsedId;
                model.UpdatedBy = user;
                model.UpdatedDate = systemNow;
                model.CreatedBy = existing.CreatedBy;
                model.CreatedDate = existing.CreatedDate;
                UserGroupBO.Instance.Update(model);
                return OpResult.Ok($"Changes saved successfully ID: {model.ID}.", new { id = model.ID });
            }
            model.UpdatedBy = user;
            model.CreatedBy = user;
            model.CreatedDate = systemNow;
            model.UpdatedDate = model.CreatedDate;
            UserGroupBO.Instance.Insert(model);
            return OpResult.Ok("Record has been created successfully.", new { id = model.ID });
        }

        public OpResult Delete(int id)
        {
            var check = UserGroupBO.Instance.FindByPrimaryKey(id);
            if (check == null)
                return OpResult.Fail("User Group not found.!");
            UserGroupBO.Instance.Delete(id);
            return OpResult.Ok($"Record was removed successfully ID: {id}.");
        }
    }
}

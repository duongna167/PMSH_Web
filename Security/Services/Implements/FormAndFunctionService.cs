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
    public class FormAndFunctionService : IFormAndFunctionService
    {
        public List<FormAndFunctionGroupModel> FunctionGroups() =>
            PropertyUtils.ConvertToList<FormAndFunctionGroupModel>(FormAndFunctionGroupBO.Instance.FindAll()) ?? new List<FormAndFunctionGroupModel>();

        public List<FormAndFunctionDataGroupModel> DataGroups()
        {
            try
            {
                return PropertyUtils.ConvertToList<FormAndFunctionDataGroupModel>(FormAndFunctionDataGroupBO.Instance.FindAll()) ?? new List<FormAndFunctionDataGroupModel>();
            }
            catch
            {
                return new List<FormAndFunctionDataGroupModel>();
            }
        }

        public List<object> SearchGrid(string codetag, string namerights, int isDataRight)
        {
            SqlParameter[] param =
            [
                new SqlParameter("@Code", codetag ?? ""),
                new SqlParameter("@Name", namerights ?? ""),
                new SqlParameter("@IsDataRight", isDataRight)
            ];
            var dataTable = DataTableHelper.getTableData("spPermissionAndShortcutKey_Search", param);
            var groupAudit = BuildAuditByCode(isDataRight == 1, true);
            var leafAudit = BuildAuditByCode(isDataRight == 1, false);
            var result = new List<object>();
            foreach (DataRow d in dataTable.Rows)
            {
                var code = SecurityRow.Cell(d, "Code");
                var createdBy = SecurityRow.Cell(d, "CreatedBy");
                var updatedBy = SecurityRow.Cell(d, "UpdatedBy");
                var createdDate = SecurityRow.CellDate(d, "CreatedDate");
                var updatedDate = SecurityRow.CellDate(d, "UpdatedDate");
                if (string.IsNullOrWhiteSpace(createdBy) && string.IsNullOrWhiteSpace(updatedBy)
                    && createdDate == null && updatedDate == null
                    && !string.IsNullOrWhiteSpace(code))
                {
                    if (!groupAudit.TryGetValue(code.Trim(), out var audit))
                        leafAudit.TryGetValue(code.Trim(), out audit);
                    if (audit != null)
                    {
                        createdBy = audit.CreatedBy;
                        updatedBy = audit.UpdatedBy;
                        createdDate = audit.CreatedDate;
                        updatedDate = audit.UpdatedDate;
                    }
                }
                result.Add(new
                {
                    ID = SecurityRow.Cell(d, "ID"),
                    Code = code,
                    Name = SecurityRow.Cell(d, "Name"),
                    ParentID = SecurityRow.CellInt(d, "ParentID"),
                    IsHide = SecurityRow.CellInt(d, "IsHide"),
                    CreatedBy = createdBy,
                    CreatedDate = createdDate,
                    UpdatedBy = updatedBy,
                    UpdatedDate = updatedDate
                });
            }
            return result;
        }

        private sealed class AuditInfo
        {
            public string CreatedBy { get; set; } = "";
            public DateTime? CreatedDate { get; set; }
            public string UpdatedBy { get; set; } = "";
            public DateTime? UpdatedDate { get; set; }
        }

        private static Dictionary<string, AuditInfo> BuildAuditByCode(bool isDataRight, bool isGroup)
        {
            var map = new Dictionary<string, AuditInfo>(StringComparer.OrdinalIgnoreCase);
            void Add(string code, string createdBy, DateTime createdDate, string updatedBy, DateTime updatedDate)
            {
                if (string.IsNullOrWhiteSpace(code) || map.ContainsKey(code.Trim()))
                    return;
                map[code.Trim()] = new AuditInfo
                {
                    CreatedBy = createdBy,
                    CreatedDate = createdDate.Year >= 1900 ? createdDate : null,
                    UpdatedBy = updatedBy,
                    UpdatedDate = updatedDate.Year >= 1900 ? updatedDate : null
                };
            }
            try
            {
                if (isDataRight && isGroup)
                {
                    foreach (var x in PropertyUtils.ConvertToList<FormAndFunctionDataGroupModel>(FormAndFunctionDataGroupBO.Instance.FindAll()) ?? [])
                        Add(x.Code, x.CreatedBy, x.CreatedDate, x.UpdatedBy, x.UpdatedDate);
                }
                else if (isDataRight)
                {
                    foreach (var x in PropertyUtils.ConvertToList<FormAndFunctionDataModel>(FormAndFunctionDataBO.Instance.FindAll()) ?? [])
                        Add(x.Code, x.CreatedBy, x.CreatedDate, x.UpdatedBy, x.UpdatedDate);
                }
                else if (isGroup)
                {
                    foreach (var x in PropertyUtils.ConvertToList<FormAndFunctionGroupModel>(FormAndFunctionGroupBO.Instance.FindAll()) ?? [])
                        Add(x.Code, x.CreatedBy, x.CreatedDate, x.UpdatedBy, x.UpdatedDate);
                }
                else
                {
                    foreach (var x in PropertyUtils.ConvertToList<FormAndFunctionModel>(FormAndFunctionBO.Instance.FindAll()) ?? [])
                        Add(x.Code, x.CreatedBy, x.CreatedDate, x.UpdatedBy, x.UpdatedDate);
                }
            }
            catch { }
            return map;
        }

        public object Detail(int id, int isDataRight)
        {
            if (isDataRight == 1)
            {
                DataTable dt = TextUtils.Select($"SELECT * FROM FormAndFunctionData WHERE ID = '{id}'");
                return (from d in dt.AsEnumerable()
                        select new
                        {
                            ID = d["ID"] == DBNull.Value ? null : d["ID"].ToString(),
                            Code = d["Code"] == DBNull.Value ? null : d["Code"].ToString(),
                            Name = d["Name"] == DBNull.Value ? null : d["Name"].ToString(),
                            Description = d["Description"] == DBNull.Value ? null : d["Description"].ToString(),
                            FormAndFunctionDataGroupID = d["FormAndFunctionDataGroupID"] == DBNull.Value ? (int?)null : Convert.ToInt32(d["FormAndFunctionDataGroupID"]),
                            CreatedBy = d["CreatedBy"] == DBNull.Value ? null : d["CreatedBy"].ToString(),
                            CreatedDate = d["CreatedDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(d["CreatedDate"]),
                            UpdatedBy = d["UpdatedBy"] == DBNull.Value ? null : d["UpdatedBy"].ToString(),
                            UpdatedDate = d["UpdatedDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(d["UpdatedDate"]),
                            OrderIndex = d["OrderIndex"] == DBNull.Value ? (int?)null : Convert.ToInt32(d["OrderIndex"]),
                            IsHide = d["IsHide"] != DBNull.Value && Convert.ToBoolean(d["IsHide"])
                        }).ToList();
            }

            DataTable fn = TextUtils.Select($"SELECT * FROM FormAndFunction WHERE ID = '{id}'");
            return (from d in fn.AsEnumerable()
                    select new
                    {
                        ID = d["ID"] == DBNull.Value ? null : d["ID"].ToString(),
                        Code = d["Code"] == DBNull.Value ? null : d["Code"].ToString(),
                        Name = d["Name"] == DBNull.Value ? null : d["Name"].ToString(),
                        Description = d["Description"] == DBNull.Value ? null : d["Description"].ToString(),
                        ShiftKey = d["ShiftKey"] != DBNull.Value && Convert.ToBoolean(d["ShiftKey"]),
                        CtrlKey = d["CtrlKey"] != DBNull.Value && Convert.ToBoolean(d["CtrlKey"]),
                        AltKey = d["AltKey"] != DBNull.Value && Convert.ToBoolean(d["AltKey"]),
                        ShortcutKey = d["ShortcutKey"] == DBNull.Value ? null : d["ShortcutKey"].ToString(),
                        FormAndFunctionGroupID = d["FormAndFunctionGroupID"] == DBNull.Value ? (int?)null : Convert.ToInt32(d["FormAndFunctionGroupID"]),
                        CreatedBy = d["CreatedBy"] == DBNull.Value ? null : d["CreatedBy"].ToString(),
                        CreatedDate = d["CreatedDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(d["CreatedDate"]),
                        UpdatedBy = d["UpdatedBy"] == DBNull.Value ? null : d["UpdatedBy"].ToString(),
                        UpdatedDate = d["UpdatedDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(d["UpdatedDate"]),
                        IsHide = d["IsHide"] != DBNull.Value && Convert.ToBoolean(d["IsHide"]),
                        OrderIndex = d["OrderIndex"] == DBNull.Value ? (int?)null : Convert.ToInt32(d["OrderIndex"]),
                        Inactive = d["Inactive"] != DBNull.Value && Convert.ToBoolean(d["Inactive"]),
                        MappingLinkWeb = d["MappingLinkWeb"] == DBNull.Value ? null : d["MappingLinkWeb"].ToString()
                    }).ToList();
        }

        public OpResult Save(int id, string code, string name, string description, int groupId, int isHide, int isDataRight, string userName)
        {
            var pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();
                var now = TextUtils.GetSystemDate();
                if (isDataRight == 1)
                {
                    var model = id > 0
                        ? (FormAndFunctionDataModel)FormAndFunctionDataBO.Instance.FindByPrimaryKey(id)
                        : new FormAndFunctionDataModel();
                    model.Code = code;
                    model.Name = name;
                    model.Description = description;
                    model.FormAndFunctionDataGroupID = groupId;
                    model.UpdatedDate = now;
                    model.UpdatedBy = userName;
                    model.IsHide = isHide == 1;
                    if (id > 0)
                    {
                        model.ID = id;
                        FormAndFunctionDataBO.Instance.Update(model);
                    }
                    else
                    {
                        model.CreatedBy = userName;
                        model.CreatedDate = now;
                        FormAndFunctionDataBO.Instance.Insert(model);
                    }
                }
                else
                {
                    var model = id > 0
                        ? (FormAndFunctionModel)FormAndFunctionBO.Instance.FindByPrimaryKey(id)
                        : new FormAndFunctionModel();
                    model.Code = code;
                    model.Name = name;
                    model.Description = description;
                    model.FormAndFunctionGroupID = groupId;
                    model.UpdatedDate = now;
                    model.UpdatedBy = userName;
                    model.IsHide = isHide == 1;
                    if (id > 0)
                    {
                        model.ID = id;
                        FormAndFunctionBO.Instance.Update(model);
                    }
                    else
                    {
                        model.CreatedBy = userName;
                        model.CreatedDate = now;
                        FormAndFunctionBO.Instance.Insert(model);
                    }
                }
                pt.CommitTransaction();
                UserMappingService.InvalidateCatalog();
                return OpResult.Ok("Successfully.");
            }
            catch (Exception ex)
            {
                pt.RollBack();
                return OpResult.Fail(ex.Message);
            }
            finally
            {
                pt.CloseConnection();
            }
        }

        public OpResult Delete(int id, int isDataRight)
        {
            if (id <= 0)
                return OpResult.Fail("Select a function, not a group.");
            if (isDataRight == 1)
                FormAndFunctionDataBO.Instance.DeleteByAttribute("ID", id);
            else
                FormAndFunctionBO.Instance.DeleteByAttribute("ID", id);
            UserMappingService.InvalidateCatalog();
            return OpResult.Ok("Record was removed successfully.");
        }

        public List<object> GroupTree(int isDataRight)
        {
            if (isDataRight == 1)
            {
                return DataGroups().Select(g => (object)new
                {
                    id = g.ID,
                    parentId = g.ParentID,
                    code = g.Code,
                    name = g.Name,
                    description = g.Description,
                    isHide = g.IsHide,
                    createdBy = g.CreatedBy,
                    createdDate = g.CreatedDate,
                    updatedBy = g.UpdatedBy,
                    updatedDate = g.UpdatedDate
                }).ToList();
            }

            return FunctionGroups().Select(g => (object)new
            {
                id = g.ID,
                parentId = g.ParentID,
                code = g.Code,
                name = g.Name,
                description = g.Description,
                isHide = g.IsHide,
                createdBy = g.CreatedBy,
                createdDate = g.CreatedDate,
                updatedBy = g.UpdatedBy,
                updatedDate = g.UpdatedDate
            }).ToList();
        }

        public object GroupDetail(int id, int isDataRight)
        {
            if (id <= 0) return null;
            if (isDataRight == 1)
            {
                var g = (FormAndFunctionDataGroupModel)FormAndFunctionDataGroupBO.Instance.FindByPrimaryKey(id);
                if (g == null || g.ID == 0) return null;
                return new
                {
                    id = g.ID,
                    parentId = g.ParentID,
                    code = g.Code,
                    name = g.Name,
                    description = g.Description,
                    isHide = g.IsHide
                };
            }
            var fg = (FormAndFunctionGroupModel)FormAndFunctionGroupBO.Instance.FindByPrimaryKey(id);
            if (fg == null || fg.ID == 0) return null;
            return new
            {
                id = fg.ID,
                parentId = fg.ParentID,
                code = fg.Code,
                name = fg.Name,
                description = fg.Description,
                isHide = fg.IsHide
            };
        }

        public OpResult SaveGroup(int id, string code, string name, string description, int parentId, int isHide, int isDataRight, string userName)
        {
            code = (code ?? "").Trim();
            name = (name ?? "").Trim();
            description = (description ?? "").Trim();
            if (string.IsNullOrEmpty(name))
                return OpResult.Fail("Name is required.");
            if (parentId == id && id > 0)
                return OpResult.Fail("A group cannot be its own parent.");

            var now = TextUtils.GetSystemDate();
            try
            {
                if (isDataRight == 1)
                {
                    var all = DataGroups();
                    if (id > 0 && IsGroupDescendant(all.Select(x => (x.ID, x.ParentID)), id, parentId))
                        return OpResult.Fail("Invalid parent group.");
                    var model = id > 0
                        ? (FormAndFunctionDataGroupModel)FormAndFunctionDataGroupBO.Instance.FindByPrimaryKey(id)
                        : new FormAndFunctionDataGroupModel();
                    if (id > 0 && (model == null || model.ID == 0))
                        return OpResult.Fail("Group not found.");
                    model.Code = code;
                    model.Name = name;
                    model.Description = description;
                    model.ParentID = parentId < 0 ? 0 : parentId;
                    model.IsHide = isHide == 1;
                    model.UpdatedBy = userName;
                    model.UpdatedDate = now;
                    if (id > 0)
                    {
                        model.ID = id;
                        FormAndFunctionDataGroupBO.Instance.Update(model);
                    }
                    else
                    {
                        model.CreatedBy = userName;
                        model.CreatedDate = now;
                        FormAndFunctionDataGroupBO.Instance.Insert(model);
                    }
                }
                else
                {
                    var all = FunctionGroups();
                    if (id > 0 && IsGroupDescendant(all.Select(x => (x.ID, x.ParentID)), id, parentId))
                        return OpResult.Fail("Invalid parent group.");
                    var model = id > 0
                        ? (FormAndFunctionGroupModel)FormAndFunctionGroupBO.Instance.FindByPrimaryKey(id)
                        : new FormAndFunctionGroupModel();
                    if (id > 0 && (model == null || model.ID == 0))
                        return OpResult.Fail("Group not found.");
                    model.Code = code;
                    model.Name = name;
                    model.Description = description;
                    model.ParentID = parentId < 0 ? 0 : parentId;
                    model.IsHide = isHide == 1;
                    model.UpdatedBy = userName;
                    model.UpdatedDate = now;
                    if (id > 0)
                    {
                        model.ID = id;
                        FormAndFunctionGroupBO.Instance.Update(model);
                    }
                    else
                    {
                        model.CreatedBy = userName;
                        model.CreatedDate = now;
                        FormAndFunctionGroupBO.Instance.Insert(model);
                    }
                }
                UserMappingService.InvalidateCatalog();
                return OpResult.Ok("Successfully.");
            }
            catch (Exception ex)
            {
                return OpResult.Fail(ex.Message);
            }
        }

        public OpResult DeleteGroup(int id, int isDataRight)
        {
            if (id <= 0)
                return OpResult.Fail("Please select a group.");
            try
            {
                if (isDataRight == 1)
                {
                    if (DataGroups().Any(x => x.ParentID == id))
                        return OpResult.Fail("Cannot delete a group that has child groups.");
                    var used = PropertyUtils.ConvertToList<FormAndFunctionDataModel>(FormAndFunctionDataBO.Instance.FindAll())
                        ?.Any(x => x.FormAndFunctionDataGroupID == id) == true;
                    if (used)
                        return OpResult.Fail("Cannot delete a group that is in use.");
                    FormAndFunctionDataGroupBO.Instance.DeleteByAttribute("ID", id);
                }
                else
                {
                    if (FunctionGroups().Any(x => x.ParentID == id))
                        return OpResult.Fail("Cannot delete a group that has child groups.");
                    var used = PropertyUtils.ConvertToList<FormAndFunctionModel>(FormAndFunctionBO.Instance.FindAll())
                        ?.Any(x => x.FormAndFunctionGroupID == id) == true;
                    if (used)
                        return OpResult.Fail("Cannot delete a group that is in use.");
                    FormAndFunctionGroupBO.Instance.DeleteByAttribute("ID", id);
                }
                UserMappingService.InvalidateCatalog();
                return OpResult.Ok("Record was removed successfully.");
            }
            catch (Exception ex)
            {
                return OpResult.Fail(ex.Message);
            }
        }

        private static bool IsGroupDescendant(IEnumerable<(int ID, int ParentID)> rows, int nodeId, int parentId)
        {
            if (parentId <= 0) return false;
            var map = rows.ToDictionary(x => x.ID, x => x.ParentID);
            var guard = 0;
            var cur = parentId;
            while (cur > 0 && guard++ < 50)
            {
                if (cur == nodeId) return true;
                if (!map.TryGetValue(cur, out cur)) break;
            }
            return false;
        }
    }
}

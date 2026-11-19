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
    public class UserMappingService : IUserMappingService
    {
        public List<object> Users()
        {
            var dt = DataTableHelper.ExecuteQuery(@"
SELECT a.ID, a.LoginName,
       LTRIM(RTRIM(ISNULL(a.FirstName,'') + ' ' + ISNULL(a.MiddleName,'') + ' ' + ISNULL(a.LastName,''))) AS FullName,
       b.Name AS GroupName, a.UserGroupID, c.Name AS DepartmentName
FROM Users a WITH (NOLOCK)
LEFT JOIN UserGroup b WITH (NOLOCK) ON a.UserGroupID = b.ID
LEFT JOIN Department c WITH (NOLOCK) ON a.DepartmentID = c.ID
WHERE a.Status = 1
ORDER BY a.LoginName");
            return dt.AsEnumerable().Select(d => (object)new
            {
                id = SecurityRow.CellInt(d, "ID"),
                loginName = SecurityRow.Cell(d, "LoginName"),
                fullName = SecurityRow.Cell(d, "FullName"),
                groupName = SecurityRow.Cell(d, "GroupName"),
                departmentName = SecurityRow.Cell(d, "DepartmentName"),
                userGroupId = SecurityRow.CellInt(d, "UserGroupID")
            }).ToList();
        }

        public List<object> Groups()
        {
            var list = PropertyUtils.ConvertToList<UserGroupModel>(UserGroupBO.Instance.FindAll()) ?? new List<UserGroupModel>();
            return list.Where(g => !g.Inactive).Select(g => (object)new { id = g.ID, code = g.Code, name = g.Name }).ToList();
        }

        public List<object> GroupUsers(int groupId)
        {
            var dt = DataTableHelper.ExecuteQuery(@"
SELECT a.ID, a.LoginName,
       LTRIM(RTRIM(ISNULL(a.FirstName,'') + ' ' + ISNULL(a.MiddleName,'') + ' ' + ISNULL(a.LastName,''))) AS FullName
FROM Users a WITH (NOLOCK)
WHERE a.Status = 1 AND a.UserGroupID = @GroupId
ORDER BY a.LoginName",
                new[] { new SqlParameter("@GroupId", groupId) });
            return dt.AsEnumerable().Select(d => (object)new
            {
                id = SecurityRow.CellInt(d, "ID"),
                loginName = SecurityRow.Cell(d, "LoginName"),
                fullName = SecurityRow.Cell(d, "FullName")
            }).ToList();
        }

        private sealed class RightNode
        {
            public int id { get; set; }
            public int parentId { get; set; }
            public string name { get; set; } = "";
            public bool isLeaf { get; set; }
        }

        private static readonly object CatalogLock = new();
        private static List<RightNode>? _fnCatalog;
        private static List<RightNode>? _dataCatalog;
        private static List<int>? _fnLeafIds;
        private static List<int>? _dataLeafIds;

        public static void InvalidateCatalog()
        {
            lock (CatalogLock)
            {
                _fnCatalog = null;
                _dataCatalog = null;
                _fnLeafIds = null;
                _dataLeafIds = null;
            }
        }

        public List<object> Catalog(bool isDataRight)
        {
            InvalidateCatalog();
            return GetCatalog(isDataRight).Select(n => (object)n).ToList();
        }

        public List<int> GrantedIds(int ownerId, bool isDataRight, bool isUser)
        {
            if (ownerId <= 0)
                return new List<int>();
            var idColumn = isDataRight ? "FormAndFunctionDataID" : "FormAndFunctionID";
            HashSet<int> groupIds;
            HashSet<int> userIds = new();
            int groupOwner = ownerId;
            if (isUser)
            {
                groupOwner = LoadUserGroupId(ownerId);
                userIds = LoadDistIds("UserRightDistribution", "UserID", ownerId, idColumn);
            }
            groupIds = LoadDistIds("UserGroupRightDistribution", "UserGroupID", groupOwner, idColumn);
            var leaves = new HashSet<int>(GetLeafIds(isDataRight));
            if (!isUser)
                return groupIds.Where(leaves.Contains).ToList();
            var granted = new HashSet<int>(groupIds);
            foreach (var id in userIds)
            {
                if (!granted.Add(id))
                    granted.Remove(id);
            }
            granted.RemoveWhere(id => !leaves.Contains(id));
            return granted.ToList();
        }

        public List<object> RightTree(int ownerId, bool isDataRight, bool isUser)
        {
            var granted = new HashSet<int>(GrantedIds(ownerId, isDataRight, isUser));
            return GetCatalog(isDataRight).Select(n => (object)new
            {
                n.id,
                n.parentId,
                n.name,
                n.isLeaf,
                granted = n.isLeaf && granted.Contains(n.id)
            }).ToList();
        }

        private static List<RightNode> GetCatalog(bool isDataRight)
        {
            lock (CatalogLock)
            {
                if (isDataRight)
                    return _dataCatalog ??= LoadCatalog(true);
                return _fnCatalog ??= LoadCatalog(false);
            }
        }

        private static List<int> GetLeafIds(bool isDataRight)
        {
            var cat = GetCatalog(isDataRight);
            lock (CatalogLock)
            {
                if (isDataRight)
                    return _dataLeafIds ??= cat.Where(n => n.isLeaf).Select(n => n.id).ToList();
                return _fnLeafIds ??= cat.Where(n => n.isLeaf).Select(n => n.id).ToList();
            }
        }

        private static List<RightNode> LoadCatalog(bool isDataRight)
        {
            var groupTable = isDataRight ? "FormAndFunctionDataGroup" : "FormAndFunctionGroup";
            var leafTable = isDataRight ? "FormAndFunctionData" : "FormAndFunction";
            var groupFk = isDataRight ? "FormAndFunctionDataGroupID" : "FormAndFunctionGroupID";
            var groups = DataTableHelper.ExecuteQuery($"SELECT ID, Name, ParentID FROM {groupTable} WITH (NOLOCK) WHERE ISNULL(IsHide,0)=0 ORDER BY Code, ID");
            var leaves = DataTableHelper.ExecuteQuery($"SELECT ID, Name, {groupFk} AS GroupID FROM {leafTable} WITH (NOLOCK) WHERE ISNULL(IsHide,0)=0 ORDER BY Name, ID");
            var items = new List<RightNode>();
            foreach (DataRow g in groups.Rows)
            {
                var gid = SecurityRow.CellInt(g, "ID");
                var parent = SecurityRow.CellInt(g, "ParentID");
                items.Add(new RightNode
                {
                    id = -gid,
                    parentId = parent == 0 ? 0 : -parent,
                    name = DisplayGroupName(SecurityRow.Cell(g, "Name")),
                    isLeaf = false
                });
            }
            foreach (DataRow f in leaves.Rows)
            {
                items.Add(new RightNode
                {
                    id = SecurityRow.CellInt(f, "ID"),
                    parentId = -SecurityRow.CellInt(f, "GroupID"),
                    name = SecurityRow.Cell(f, "Name"),
                    isLeaf = true
                });
            }
            return items;
        }

        private static string DisplayGroupName(string name)
        {
            if (string.Equals(name, "User Management", StringComparison.OrdinalIgnoreCase))
                return "Security";
            return name;
        }

        private static int LoadUserGroupId(int userId)
        {
            var dt = DataTableHelper.ExecuteQuery(
                "SELECT UserGroupID FROM Users WITH (NOLOCK) WHERE ID = @Id",
                new[] { new SqlParameter("@Id", userId) });
            if (dt.Rows.Count == 0)
                return 0;
            return SecurityRow.CellInt(dt.Rows[0], "UserGroupID");
        }

        public OpResult SaveUserRights(int userId, bool isDataRight, string ids, string login)
        {
            if (userId <= 0)
                return OpResult.Fail("Select a user.");
            var exists = DataTableHelper.ExecuteQuery(
                "SELECT ID FROM Users WITH (NOLOCK) WHERE ID = @Id",
                new[] { new SqlParameter("@Id", userId) });
            if (exists.Rows.Count == 0)
                return OpResult.Fail("User not found.");
            SaveUserDistribution(userId, LoadUserGroupId(userId), isDataRight, ParseIdSet(ids), login);
            return OpResult.Ok("Rights saved.");
        }

        public OpResult SaveGroupRights(int groupId, bool isDataRight, string ids, string login)
        {
            if (groupId <= 0)
                return OpResult.Fail("Select a group.");
            if (ids == null)
                return OpResult.Fail("Rights tree is still loading.");
            var checkedIds = ParseIdSet(ids);
            var idColumn = isDataRight ? "FormAndFunctionDataID" : "FormAndFunctionID";
            var existing = LoadDistIds("UserGroupRightDistribution", "UserGroupID", groupId, idColumn);
            if (checkedIds.Count == 0 && existing.Count > 0)
                return OpResult.Fail("Save cancelled: no functions ticked. This would remove all group rights.");
            SaveGroupDistribution(groupId, isDataRight, checkedIds, login);
            return OpResult.Ok("Rights saved.");
        }

        private static HashSet<int> LoadDistIds(string table, string ownerCol, int ownerId, string idColumn)
        {
            var set = new HashSet<int>();
            if (ownerId <= 0)
                return set;
            var dt = DataTableHelper.ExecuteQuery(
                $"SELECT {idColumn} AS Rid FROM {table} WITH (NOLOCK) WHERE {ownerCol} = @Id AND ISNULL({idColumn},0) > 0",
                new[] { new SqlParameter("@Id", ownerId) });
            foreach (DataRow r in dt.Rows)
            {
                var v = SecurityRow.CellInt(r, "Rid");
                if (v > 0) set.Add(v);
            }
            return set;
        }

        private static HashSet<int> ParseIdSet(string ids)
        {
            var set = new HashSet<int>();
            if (string.IsNullOrWhiteSpace(ids))
                return set;
            foreach (var part in ids.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                if (int.TryParse(part.Trim(), out var n) && n > 0)
                    set.Add(n);
            }
            return set;
        }

        private static void SaveUserDistribution(int userId, int groupId, bool isDataRight, HashSet<int> checkedIds, string login)
        {
            var idColumn = isDataRight ? "FormAndFunctionDataID" : "FormAndFunctionID";
            var groupIds = LoadDistIds("UserGroupRightDistribution", "UserGroupID", groupId, idColumn);
            var userIds = LoadDistIds("UserRightDistribution", "UserID", userId, idColumn);
            var now = TextUtils.GetSystemDate();
            if (string.IsNullOrWhiteSpace(login))
                login = "";
            foreach (var id in GetLeafIds(isDataRight))
            {
                bool inGroup = groupIds.Contains(id);
                bool check = checkedIds.Contains(id);
                bool hasUser = userIds.Contains(id);
                bool needUserRow = (check && !inGroup) || (!check && inGroup);
                if (needUserRow && !hasUser)
                    InsertUserRight(userId, id, isDataRight, login, now);
                else if (!needUserRow && hasUser)
                    DeleteUserRight(userId, id, isDataRight);
            }
        }

        private static void SaveGroupDistribution(int groupId, bool isDataRight, HashSet<int> checkedIds, string login)
        {
            var idColumn = isDataRight ? "FormAndFunctionDataID" : "FormAndFunctionID";
            var existing = LoadDistIds("UserGroupRightDistribution", "UserGroupID", groupId, idColumn);
            var now = TextUtils.GetSystemDate();
            foreach (var id in GetLeafIds(isDataRight))
            {
                bool check = checkedIds.Contains(id);
                bool has = existing.Contains(id);
                if (check && !has)
                    InsertGroupRight(groupId, id, isDataRight, login, now);
                else if (!check && has)
                    DeleteGroupRight(groupId, id, isDataRight);
            }
        }

        private static void InsertUserRight(int userId, int functionId, bool isDataRight, string login, DateTime now)
        {
            DataTableHelper.ExecuteNonQueryText(@"
IF NOT EXISTS (
    SELECT 1 FROM UserRightDistribution WITH (NOLOCK)
    WHERE UserID = @UserID
      AND ISNULL(FormAndFunctionID,0) = @FnId
      AND ISNULL(FormAndFunctionDataID,0) = @DataId)
INSERT INTO UserRightDistribution
    (FormAndFunctionID, UserID, ViewRight, CreateRight, ModifyRight, DeleteRight, SpecialRight,
     CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, FormAndFunctionDataID)
VALUES
    (@FnId, @UserID, 1, 1, 1, 1, 1, @Login, @Now, @Login, @Now, @DataId)",
                new[]
                {
                    new SqlParameter("@UserID", userId),
                    new SqlParameter("@FnId", isDataRight ? 0 : functionId),
                    new SqlParameter("@DataId", isDataRight ? functionId : 0),
                    new SqlParameter("@Login", (object)login ?? ""),
                    new SqlParameter("@Now", now)
                });
        }

        private static void DeleteUserRight(int userId, int functionId, bool isDataRight)
        {
            var col = isDataRight ? "FormAndFunctionDataID" : "FormAndFunctionID";
            DataTableHelper.ExecuteNonQueryText(
                $"DELETE FROM UserRightDistribution WHERE UserID = @UserID AND {col} = @Fid",
                new[]
                {
                    new SqlParameter("@UserID", userId),
                    new SqlParameter("@Fid", functionId)
                });
        }

        private static void InsertGroupRight(int groupId, int functionId, bool isDataRight, string login, DateTime now)
        {
            UserGroupRightDistributionBO.Instance.Insert(new UserGroupRightDistributionModel
            {
                UserGroupID = groupId,
                FormAndFunctionID = isDataRight ? 0 : functionId,
                FormAndFunctionDataID = isDataRight ? functionId : 0,
                ViewRight = true,
                CreateRight = true,
                ModifyRight = true,
                DeleteRight = true,
                SpecialRight = true,
                CreatedBy = login,
                CreatedDate = now,
                UpdatedBy = login,
                UpdatedDate = now
            });
        }

        private static void DeleteGroupRight(int groupId, int functionId, bool isDataRight)
        {
            var col = isDataRight ? "FormAndFunctionDataID" : "FormAndFunctionID";
            UserGroupRightDistributionBO.Instance.DeleteByExpression(
                new Expression("UserGroupID", groupId, "=").And(new Expression(col, functionId, "=")));
        }
    }
}

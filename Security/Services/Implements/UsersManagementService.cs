using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using BaseBusiness.BO;
using BaseBusiness.Model;
using BaseBusiness.util;
using Microsoft.Data.SqlClient;
using Security.Helpers;
using Security.Services.Interfaces;

namespace Security.Services.Implements
{
    public class UsersManagementService : IUsersManagementService
    {
        public List<UserGroupModel> Groups() =>
            PropertyUtils.ConvertToList<UserGroupModel>(UserGroupBO.Instance.FindAll()) ?? new List<UserGroupModel>();

        public List<JobTitleModel> JobTitles() =>
            PropertyUtils.ConvertToList<JobTitleModel>(JobTitleBO.Instance.FindAll()) ?? new List<JobTitleModel>();

        public List<DepartmentModel> Departments() =>
            PropertyUtils.ConvertToList<DepartmentModel>(DepartmentBO.Instance.FindAll()) ?? new List<DepartmentModel>();

        public DataTable Search(string lastName, string firstName, string loginName, int userStatus, int cashierStatus, string jobtitle, string department)
        {
            SqlParameter[] param =
            [
                new SqlParameter("@LastName", lastName ?? ""),
                new SqlParameter("@FirstName", firstName ?? ""),
                new SqlParameter("@LoginName", loginName ?? ""),
                new SqlParameter("@JobTitle", jobtitle ?? ""),
                new SqlParameter("@Department", department ?? ""),
                new SqlParameter("@Cashier", cashierStatus),
                new SqlParameter("@Status", userStatus)
            ];
            return DataTableHelper.getTableData("spSearchUsers", param);
        }

        public UsersModel? GetById(int id)
        {
            var model = UsersBO.Instance.FindByPrimaryKey(id) as UsersModel;
            if (model != null && !string.IsNullOrEmpty(model.PasswordHash))
            {
                model.PasswordHash = DBUtils.Decrypt(
                    model.PasswordHash, DBUtils.passPhrase, DBUtils.saltValue, DBUtils.hashAlgorithm,
                    DBUtils.passwordIterations, DBUtils.initVector, DBUtils.keySize);
            }
            return model;
        }

        public OpResult Save(UsersModel modelRoom, string computerIp = "", int actorUserId = 0, string actorUserName = "")
        {
            modelRoom.PasswordHash = DBUtils.Encrypt(
                modelRoom.PasswordHash, DBUtils.passPhrase, DBUtils.saltValue, DBUtils.hashAlgorithm,
                DBUtils.passwordIterations, DBUtils.initVector, DBUtils.keySize);
            var newLogin = (modelRoom.LoginName ?? string.Empty).Trim();
            modelRoom.LoginName = newLogin;
            if (string.IsNullOrWhiteSpace(newLogin))
                return OpResult.Fail("Login Name is required.");

            var allUsers = PropertyUtils.ConvertToList<UsersModel>(UsersBO.Instance.FindAll()) ?? [];
            bool dupLogin = allUsers.Any(u =>
                u.ID != modelRoom.ID &&
                !string.IsNullOrWhiteSpace(u.LoginName) &&
                string.Equals(u.LoginName.Trim(), newLogin, StringComparison.OrdinalIgnoreCase));
            if (dupLogin)
                return OpResult.Fail("Login Name already exists.");

            if (modelRoom.ID == 0)
            {
                modelRoom.PasswordStatus = false;
                modelRoom.ID = (int)UsersBO.Instance.Insert(modelRoom);
                if (modelRoom.IsCashier == true)
                    modelRoom.CashierNo = modelRoom.ID * 17;
                UsersBO.Instance.Update(modelRoom);
                WriteUsersActivityLog(modelRoom.ID, actorUserId, actorUserName, "Insert", null, modelRoom,
                    $"Create user {modelRoom.LoginName}");
                return OpResult.Ok("User inserted successfully.");
            }

            var existing = (UsersModel)UsersBO.Instance.FindByPrimaryKey(modelRoom.ID);
            if (existing != null && existing.Status == -1 && modelRoom.Status == 1)
                return OpResult.Fail("A terminated user cannot be activated again.");

            bool passwordChanged = existing != null && existing.PasswordHash != modelRoom.PasswordHash;
            if (passwordChanged)
                modelRoom.PasswordStatus = false;
            else if (existing != null)
                modelRoom.PasswordStatus = existing.PasswordStatus;
            UsersBO.Instance.Update(modelRoom);
            if (passwordChanged)
                UserDetailLog.Write(modelRoom.ID, "", UserDetailLog.TypeChange, computerIp);

            var desc = $"Update user {modelRoom.LoginName}";
            if (existing != null && existing.Status != modelRoom.Status)
                desc += $". Status {StatusName(existing.Status)} -> {StatusName(modelRoom.Status)}";
            WriteUsersActivityLog(modelRoom.ID, actorUserId, actorUserName, "Update", existing, modelRoom, desc);
            return OpResult.Ok("User updated successfully.");
        }

        public OpResult ResetPassword(UsersModel modelRoom, string computerIp = "", int actorUserId = 0, string actorUserName = "")
        {
            if (modelRoom == null || modelRoom.ID == 0)
                return OpResult.Fail("Please save the user before resetting the password.");
            var user = UsersBO.Instance.FindByPrimaryKey(modelRoom.ID) as UsersModel;
            if (user == null)
                return OpResult.Fail("User not found.");

            string tempPassword = GenerateRandomPassword(8);
            user.PasswordHash = DBUtils.Encrypt(
                tempPassword, DBUtils.passPhrase, DBUtils.saltValue, DBUtils.hashAlgorithm,
                DBUtils.passwordIterations, DBUtils.initVector, DBUtils.keySize);
            user.PasswordStatus = false;
            if (user.Status != -1)
                user.Status = 1;
            user.PassExpireDate = DateTime.Now.Date.AddDays(60);
            user.UpdateDate = DateTime.Now;
            if (modelRoom.UserUpdateID != 0)
                user.UserUpdateID = modelRoom.UserUpdateID;
            UsersBO.Instance.Update(user);
            UserDetailLog.Write(user.ID, "", UserDetailLog.TypeChange, computerIp);
            WriteUsersActivityLog(user.ID, actorUserId, actorUserName, "ResetPassword", null, null,
                $"Reset password for user {user.LoginName}");
            return OpResult.Ok("The password has been reset.", new Dictionary<string, object>
            {
                ["plainPassword"] = tempPassword,
                ["passExpireDate"] = user.PassExpireDate.ToString("yyyy-MM-dd"),
                ["hasEmail"] = !string.IsNullOrWhiteSpace(user.Email),
                ["status"] = user.Status
            });
        }

        public OpResult SendResetPasswordEmail(int id, string plainPassword, int sendBy)
        {
            if (id == 0)
                return OpResult.Fail("Please save the user before resetting the password.");
            if (string.IsNullOrWhiteSpace(plainPassword))
                return OpResult.Fail("The password was reset. Reset Password email template was not found.");
            var user = UsersBO.Instance.FindByPrimaryKey(id) as UsersModel;
            if (user == null)
                return OpResult.Fail("User not found.");
            if (string.IsNullOrWhiteSpace(user.Email))
                return OpResult.Fail("The password was reset. Email was not sent because this user has no email address.");

            PmsMailConfig.EnsureVietAnhSmtpConfig();
            var template = PmsMailConfig.EnsureResetPasswordTemplate();
            if (template == null)
                return OpResult.Fail("The password was reset. Reset Password email template was not found.");

            var fullName = string.Join(" ", new[] { user.FirstName, user.MiddleName, user.LastName }
                .Where(x => !string.IsNullOrWhiteSpace(x))).Trim();
            if (string.IsNullOrWhiteSpace(fullName))
                fullName = user.FullName ?? user.LoginName ?? "";

            string subject = template.Name;
            string body = ReplaceMailPlaceholders(template.Content ?? "", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["LoginName"] = user.LoginName ?? "",
                ["Password"] = plainPassword,
                ["FullName"] = fullName
            });
            try
            {
                SendSmtpMail(user.Email.Trim(), subject, body);
                InsertMailHistory(template.ID, subject, body, user.Email.Trim(), sendBy, 1, "");
                return OpResult.Ok("The new password has been sent to the user email.");
            }
            catch (Exception mailEx)
            {
                InsertMailHistory(template.ID, subject, body, user.Email.Trim(), sendBy, 0, mailEx.Message);
                return OpResult.Fail("The password was reset but the email could not be sent.");
            }
        }

        public OpResult Delete(int id, int actorUserId = 0, string actorUserName = "")
        {
            var user = UsersBO.Instance.FindByPrimaryKey(id) as UsersModel;
            if (user == null)
                return OpResult.Fail("User  not found.!");
            UsersBO.Instance.Delete(id);
            WriteUsersActivityLog(id, actorUserId, actorUserName, "Delete", user, null,
                $"Delete user {user.LoginName}");
            return OpResult.Ok("Record was removed successfully");
        }

        private static string StatusName(int status) => status switch
        {
            1 => "Active",
            0 => "Inactive",
            -1 => "Terminated",
            _ => status.ToString()
        };

        private static void WriteUsersActivityLog(
            int objectId, int actorUserId, string actorUserName, string change,
            UsersModel? oldUser, UsersModel? newUser, string description)
        {
            string? oldPwd = oldUser?.PasswordHash;
            string? newPwd = newUser?.PasswordHash;
            string? oldSalt = oldUser?.SaltKey;
            string? newSalt = newUser?.SaltKey;
            try
            {
                if (oldUser != null)
                {
                    oldUser.PasswordHash = "";
                    oldUser.SaltKey = "";
                }
                if (newUser != null)
                {
                    newUser.PasswordHash = "";
                    newUser.SaltKey = "";
                }

                var log = new ActivityLogModel
                {
                    TableName = "Users",
                    ObjectID = objectId,
                    UserID = actorUserId,
                    UserName = actorUserName ?? "",
                    ChangeDate = DateTime.Now,
                    Change = change,
                    Description = description ?? ""
                };
                TextUtils.FillValues(log, oldUser, newUser);
                ActivityLogBO.Instance.Insert(log);
            }
            catch
            {
            }
            finally
            {
                if (oldUser != null)
                {
                    oldUser.PasswordHash = oldPwd;
                    oldUser.SaltKey = oldSalt;
                }
                if (newUser != null)
                {
                    newUser.PasswordHash = newPwd;
                    newUser.SaltKey = newSalt;
                }
            }
        }

        private static string GenerateRandomPassword(int length)
        {
            const string letters = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ";
            const string digits = "23456789";
            var chars = new char[length];
            chars[0] = letters[RandomNumberGenerator.GetInt32(letters.Length)];
            chars[1] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
            string all = letters + digits;
            for (int i = 2; i < length; i++)
                chars[i] = all[RandomNumberGenerator.GetInt32(all.Length)];
            return new string(chars.OrderBy(_ => RandomNumberGenerator.GetInt32(int.MaxValue)).ToArray());
        }

        private static string ReplaceMailPlaceholders(string content, Dictionary<string, string> values)
        {
            return Regex.Replace(content, @"##(\w+)##", match =>
            {
                var key = match.Groups[1].Value;
                return values.TryGetValue(key, out var val) ? val : match.Value;
            });
        }

        private static string MailConfigValue(string keyName)
        {
            return PropertyUtils.ConvertToList<ConfigSystemModel>(
                ConfigSystemBO.Instance.FindByAttribute("KeyName", keyName))?.FirstOrDefault()?.KeyValue ?? "";
        }

        private static void SendSmtpMail(string recipient, string subject, string body)
        {
            string host = MailConfigValue("MAIL_SERVERNAME");
            string portText = MailConfigValue("MAIL_SERVERPORT");
            string from = MailConfigValue("MAIL_FROM");
            string password = MailConfigValue("MAIL_PASSWORD");
            string fromName = MailConfigValue("MAIL_FROMNAME");
            string sslText = MailConfigValue("MAIL_USE_SSL");
            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
                throw new Exception("SMTP is not configured.");
            int port = 587;
            int.TryParse(portText, out port);
            bool ssl = sslText == "1" || sslText.Equals("true", StringComparison.OrdinalIgnoreCase);
            using var mail = new MailMessage
            {
                From = new MailAddress(from, string.IsNullOrWhiteSpace(fromName) ? from : fromName),
                Subject = subject ?? "",
                Body = body ?? "",
                IsBodyHtml = true
            };
            mail.To.Add(recipient);
            using var smtp = new SmtpClient(host, port)
            {
                EnableSsl = ssl,
                Credentials = new NetworkCredential(from, password),
                DeliveryMethod = SmtpDeliveryMethod.Network
            };
            smtp.Send(mail);
        }

        private static void InsertMailHistory(int templateId, string subject, string content, string sendTo, int sendBy, int sendOk, string reason)
        {
            try
            {
                ContactEmailHistoryBO.Instance.Insert(new ContactEmailHistoryModel
                {
                    GroupID = 0,
                    Subject = subject,
                    Content = content,
                    ContactRelateID = 0,
                    SendBy = sendBy,
                    SendDate = DateTime.Now,
                    SendOK = sendOk,
                    SendTo = sendTo,
                    Reason = reason ?? "",
                    SuccessSendDate = sendOk == 1 ? DateTime.Now : DateTime.MinValue,
                    ContactEmailTemplateID = templateId
                });
            }
            catch
            {
            }
        }
    }
}

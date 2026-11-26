using System;
using System.Linq;
using BaseBusiness.BO;
using BaseBusiness.Model;

namespace BaseBusiness.util
{
    public static class PmsMailConfig
    {
        public static void EnsureVietAnhSmtpConfig()
        {
            UpsertMailKey("MAIL_FROMNAME", "SSMC", "Sender Name");
            UpsertMailKey("MAIL_SERVERNAME", "smtp.gmail.com", "Server host");
            UpsertMailKey("MAIL_SERVERPORT", "587", "Server port");
            UpsertMailKey("MAIL_FROM", "vietanh3204@gmail.com", "user name");
            UpsertMailKey("MAIL_PASSWORD", "lfok miuf lqnd kydi", "password");
            UpsertMailKey("MAIL_USE_SSL", "true", "ssl");
        }

        public static ContactEmailTemplateModel EnsureResetPasswordTemplate()
        {
            var list = PropertyUtils.ConvertToList<ContactEmailTemplateModel>(ContactEmailTemplateBO.Instance.FindAll()) ?? [];
            var existing = list.FirstOrDefault(t =>
                !string.IsNullOrWhiteSpace(t.Name) &&
                string.Equals(t.Name.Trim(), "Reset Password", StringComparison.OrdinalIgnoreCase));
            if (existing != null)
                return existing;

            var html =
                "<p>Dear ##FullName##,</p>" +
                "<p>Your Property Management System login password has been reset by the administrator.</p>" +
                "<p>Login name: <strong>##LoginName##</strong><br/>" +
                "Temporary password: <strong>##Password##</strong></p>" +
                "<p>Please sign in with this temporary password and change it immediately. You will be required to set a new password on first login.</p>" +
                "<p>If you did not request this change, contact the system administrator.</p>" +
                "<p>Regards,<br/>Front Office / System Administrator</p>";

            var model = new ContactEmailTemplateModel
            {
                Name = "Reset Password",
                Content = html,
                Language = 1
            };
            model.ID = Convert.ToInt32(ContactEmailTemplateBO.Instance.Insert(model));
            return model;
        }

        private static void UpsertMailKey(string keyName, string keyValue, string description)
        {
            var item = PropertyUtils.ConvertToList<ConfigSystemModel>(
                ConfigSystemBO.Instance.FindByAttribute("KeyName", keyName))?.FirstOrDefault();
            if (item != null)
                return;

            ConfigSystemBO.Instance.Insert(new ConfigSystemModel
            {
                KeyName = keyName,
                KeyValue = keyValue,
                Desciption = description,
                CreateDate = DateTime.Now,
                UpdateDate = DateTime.Now
            });
        }
    }
}

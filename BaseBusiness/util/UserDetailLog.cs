using System;
using System.Net;
using BaseBusiness.BO;
using BaseBusiness.Model;
using Microsoft.AspNetCore.Http;

namespace BaseBusiness.util
{
    public static class UserDetailLog
    {
        public const string WebComputerName = "WEB";
        public const int TypeChange = 0;
        public const int TypeWrong = 1;

        public static string ClientIp(HttpContext http)
        {
            var ip = http?.Connection?.RemoteIpAddress;
            if (ip == null)
                return "";
            if (ip.IsIPv4MappedToIPv6)
                ip = ip.MapToIPv4();
            if (IPAddress.IPv6Loopback.Equals(ip))
                return "127.0.0.1";
            return ip.ToString();
        }

        public static void Write(int userId, string typedPassword, int type, string computerIp)
        {
            if (userId <= 0)
                return;
            try
            {
                var pwd = typedPassword ?? "";
                if (pwd.Length > 50)
                    pwd = pwd.Substring(0, 50);
                var ip = computerIp ?? "";
                if (ip.Length > 30)
                    ip = ip.Substring(0, 30);

                UserDetailBO.Instance.Insert(new UserDetailModel
                {
                    UserID = userId,
                    TypedPassword = pwd,
                    ComputerName = WebComputerName,
                    ComputerIP = ip,
                    Type = type,
                    CreatedDate = DateTime.Now
                });
            }
            catch
            {
            }
        }
    }
}

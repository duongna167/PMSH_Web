using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;

namespace BaseBusiness.Auth
{
    /// <summary>
    /// Allows the action when the signed-in session holds at least one of the given FormAndFunction codes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public sealed class RequireFunctionAttribute : ActionFilterAttribute
    {
        private readonly string[] _codes;

        public RequireFunctionAttribute(params string[] codes)
        {
            _codes = codes ?? Array.Empty<string>();
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var http = context.HttpContext;
            var userId = http.Session.GetInt32("UserID");
            var ajax = IsAjax(http.Request);

            if (userId == null || userId <= 0)
            {
                context.Result = ajax
                    ? new JsonResult(new { code = 401, msg = "Login required" }) { StatusCode = 401 }
                    : new RedirectResult("/User/Index");
                return;
            }

            var granted = ReadCodes(http.Session.GetString("PermissionCodes"));
            var needed = _codes.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()).ToArray();
            if (needed.Length > 0 && !needed.Any(granted.Contains))
            {
                context.Result = ajax
                    ? new JsonResult(new { code = 403, msg = "Access Denied" }) { StatusCode = 403 }
                    : new RedirectResult("/Home/Index?denied=1");
            }
        }

        private static HashSet<string> ReadCodes(string raw)
        {
            var granted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(raw))
                return granted;

            try
            {
                var list = JsonConvert.DeserializeObject<string[]>(raw) ?? Array.Empty<string>();
                foreach (var item in list)
                {
                    if (!string.IsNullOrWhiteSpace(item))
                        granted.Add(item.Trim());
                }
            }
            catch
            {
                granted.Clear();
            }

            return granted;
        }

        private static bool IsAjax(HttpRequest request)
        {
            if (string.Equals(request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase))
                return true;

            var accept = request.Headers.Accept.ToString();
            return accept.Contains("application/json", StringComparison.OrdinalIgnoreCase)
                && !accept.Contains("text/html", StringComparison.OrdinalIgnoreCase);
        }
    }
}

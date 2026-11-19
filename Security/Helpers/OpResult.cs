using System.Collections.Generic;

namespace Security.Helpers
{
    public class OpResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public object? Data { get; set; }
        public List<object>? Errors { get; set; }
        public bool NotFound { get; set; }

        public static OpResult Ok(string message, object? data = null) =>
            new() { Success = true, Message = message, Data = data };

        public static OpResult Fail(string message, List<object>? errors = null) =>
            new() { Success = false, Message = message, Errors = errors };

        public static OpResult Missing(string message) =>
            new() { Success = false, NotFound = true, Message = message };
    }
}

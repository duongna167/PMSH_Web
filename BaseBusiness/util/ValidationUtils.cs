using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace BaseBusiness.util
{
    public static class ValidationUtils
    {
        public class ValidationError
        {
            public string Field { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
        }

        // ========= CORE =========
        public static ValidationError? Check(bool isInvalid, string field, string message)
            => isInvalid ? new ValidationError { Field = field, Message = message } : null;

        // ========= STRING =========
        public static ValidationError? Check(string? value, string field, string message)
            => Check(string.IsNullOrWhiteSpace(value), field, message);

        // ========= INT =========
        public static ValidationError? Check(int value, string field, string message)
            => Check(value <= 0, field, message);

        public static ValidationError? Check(int? value, string field, string message)
            => Check(!value.HasValue || value.Value <= 0, field, message);

        // ========= DECIMAL =========
        public static ValidationError? Check(decimal value, string field, string message)
            => Check(value <= 0, field, message);

        public static ValidationError? Check(decimal? value, string field, string message)
            => Check(!value.HasValue || value.Value <= 0, field, message);

        // ========= DATETIME =========
        public static ValidationError? Check(DateTime value, string field, string message)
            => Check(value == default, field, message);

        public static ValidationError? Check(DateTime? value, string field, string message)
            => Check(!value.HasValue || value.Value == default, field, message);

        // ========= OBJECT =========
        public static ValidationError? Check(object? value, string field, string message)
            => Check(value == null, field, message);

        // ========= LIST =========
        public static ValidationError? Check<T>(IEnumerable<T>? list, string field, string message)
            => Check(list == null || !list.Any(), field, message);

        // ========= COLLECT =========
        public static List<ValidationError> GetErrors(params ValidationError?[] checks)
            => checks.Where(x => x != null).Select(x => x!).ToList();

        // ========= CSV =========
        public static string SanitizeCsv(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            if (!Regex.IsMatch(input, @"^\d+(,\d+)*$"))
                throw new Exception("Invalid input");

            return input;
        }

        // ========= DUPLICATE CODE =========
        public static ValidationError? CheckDuplicateCode(
            string? code,
            int currentId,
            Func<string, int, bool> isDuplicateFunc,
            string field,
            string message)
        {
            if (string.IsNullOrWhiteSpace(code))
                return null; 

            bool isDuplicate = isDuplicateFunc(code, currentId);
            return Check(isDuplicate, field, message);
        }

    }

}
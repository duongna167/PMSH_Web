namespace Cashiering.Dto
{
    public class SaveARAccountRequestDto
    {
        public int Id { get; set; }
        public int Profile { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public int AccountType { get; set; }
        public decimal CreditLimit { get; set; } // Dùng int? để tránh lỗi nếu rỗng
        public string Contact { get; set; }
        public string Phone { get; set; }
        public string Fax { get; set; }
        public string Email { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string Address3 { get; set; }
        public int City { get; set; }
        public string PostalCode { get; set; }
        public int Country { get; set; }
        public string Description { get; set; }
        public bool Flagged { get; set; } // MVC tự parse checkbox on/off thành true/false
        public bool Inactive { get; set; }
        public int PaymentDue { get; set; }
        public string UserName { get; set; }
    }
    public class ValidationErrorDto
    {
        public string Field { get; set; }
        public string Message { get; set; }
    }
}

namespace RoomManagement.Dto
{
    /// <summary>Thêm (ID = 0) hoặc sửa một Item (màn Item List).</summary>
    public class ItemSaveRequest
    {
        public int ID { get; set; }
        public int ItemGroupID { get; set; }
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        /// <summary>Danh sách Transaction Code, cách nhau bằng dấu phẩy.</summary>
        public string? TransactionCode { get; set; }
        public int DepartmentID { get; set; }
        public int? QuantityStock { get; set; }
        public int? QuantityDefault { get; set; }
        public string? AvailableFrom { get; set; }
        public string? AvailableTo { get; set; }
        public string? SetupTime { get; set; }
        public string? SetDownTime { get; set; }
        public string? Traces { get; set; }
    }

    /// <summary>Nạp số lượng tồn kho theo ngày cho một Item (màn Item Daily Inventory > Load).</summary>
    public class ItemInventoryLoadRequest
    {
        public int ItemID { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int? Quantity { get; set; }
    }
}

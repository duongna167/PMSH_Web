namespace Profile.DTO
{
    public class MergeRequest
    {
        public int SourceID { get; set; } // Profile sẽ bị xóa
        public int DestID { get; set; }   // Profile giữ lại (Original)
        public List<string> OverrideFields { get; set; } // Danh sách các trường tích giữ lại tscih
        public int Type { get; set; }
        public int UserUpdateID { get; set; }
    }
}

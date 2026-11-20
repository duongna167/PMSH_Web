namespace NightAudit.Dto
{
    /// <summary>Đăng tiền phòng / Fixed Charge / Package cho các phòng được chọn theo một ngày (màn Posting Room).</summary>
    public class PostingRoomRequest
    {
        /// <summary>Ngày cần đăng (yyyy-MM-dd).</summary>
        public DateTime Date { get; set; }

        /// <summary>Số phòng cần đăng (mọi đặt phòng đang ở trong các phòng này đều được đăng).</summary>
        public List<string>? Rooms { get; set; }
    }
}

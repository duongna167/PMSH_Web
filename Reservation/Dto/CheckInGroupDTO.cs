namespace Reservation.Dto
{
    public class CheckInGroupDTO
    {
        /// <summary>
        /// Request dùng chung cho nghiệp vụ check-in nhóm.
        /// Có thể check-in theo ProfileGroupId hoặc ConfirmationNo.
        /// </summary>
        public class CheckInRequest
        {
            /// <summary>
            /// Mã nhóm profile. Dùng cho luồng check-in theo group.
            /// </summary>
            public int? ProfileGroupId { get; set; }

            /// <summary>
            /// Confirmation number. Dùng cho luồng check-in theo số xác nhận.
            /// </summary>
            public string ConfirmationNo { get; set; } = "";

            /// <summary>
            /// User thực hiện check-in.
            /// </summary>
            public int UserId { get; set; }

            /// <summary>
            /// Kiểu lọc phòng:
            /// 1 = InspectedOnly
            /// 2 = CleanOnly
            /// 3 = CleanOrInspected
            /// 4 = All
            /// </summary>
            public int FilterType { get; set; }

            /// <summary>
            /// Nếu true thì bỏ qua alert và vẫn check-in.
            /// Nếu false thì reservation có alert sẽ bị chặn.
            /// </summary>
            public bool IgnoreAlert { get; set; }

            /// <summary>
            /// Danh sách reservation được chọn trước.
            /// Phần này hữu ích cho luồng ConfirmationNo nếu UI phía web đã cho chọn sẵn.
            /// Nếu không dùng, service có thể tự query theo ConfirmationNo.
            /// </summary>
            public List<int> SelectedReservationIds { get; set; } = [];
        }
        /// <summary>
        /// DTO đại diện cho 1 reservation ứng viên để check-in.
        /// Dữ liệu này được map ra từ DB/store/query.
        /// </summary>
        public class ReservationCheckInDto
        {
            /// <summary>
            /// Reservation ID
            /// </summary>
            public int ReservationId { get; set; }

            /// <summary>
            /// Room ID
            /// </summary>
            public int RoomId { get; set; }

            /// <summary>
            /// Số lượng phòng / số room liên quan.
            /// Đặt tên theo logic cũ là NoOfRoom để bám sát CreateCheckIn.
            /// </summary>
            public int NoOfRoom { get; set; }

            /// <summary>
            /// Số phòng hiển thị.
            /// </summary>
            public string RoomNo { get; set; }

            /// <summary>
            /// Housekeeping status.
            /// Theo code cũ:
            /// 1 = Clean
            /// 4 = Inspected
            /// </summary>
            public int HKStatusId { get; set; }

            /// <summary>
            /// Có phải main guest hay không.
            /// </summary>
            public bool IsMainGuest { get; set; }

            /// <summary>
            /// Mã nhóm share room.
            /// </summary>
            public int ShareRoom { get; set; }
        }

        /// <summary>
        /// Kết quả xử lý của từng reservation / từng phòng.
        /// </summary>
        public class CheckInItemResult
        {
            /// <summary>
            /// Reservation ID tương ứng.
            /// </summary>
            public int ReservationId { get; set; }

            /// <summary>
            /// Số phòng tương ứng.
            /// </summary>
            public string RoomNo { get; set; }

            /// <summary>
            /// Thành công hay không.
            /// </summary>
            public bool Success { get; set; }

            /// <summary>
            /// Thông báo cho từng item.
            /// </summary>
            public string Message { get; set; }
        }

        /// <summary>
        /// Kết quả tổng của một lần check-in nhóm.
        /// </summary>
        public class CheckInResult
        {
            public CheckInResult()
            {
                Items = new List<CheckInItemResult>();
                Messages = new List<string>();
            }

            /// <summary>
            /// Có ít nhất 1 reservation check-in thành công hay không.
            /// </summary>
            public bool Success { get; set; }

            /// <summary>
            /// Tổng số reservation được yêu cầu xử lý.
            /// </summary>
            public int TotalRequested { get; set; }

            /// <summary>
            /// Tổng số reservation check-in thành công.
            /// </summary>
            public int TotalCheckedIn { get; set; }

            /// <summary>
            /// Thông báo tổng.
            /// </summary>
            public string Message { get; set; }

            /// <summary>
            /// Danh sách thông báo phụ / cảnh báo / giải thích.
            /// </summary>
            public List<string> Messages { get; set; }

            /// <summary>
            /// Kết quả chi tiết theo từng reservation.
            /// </summary>
            public List<CheckInItemResult> Items { get; set; }
        }
    }

}
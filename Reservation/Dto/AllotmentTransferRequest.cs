using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reservation.Dto
{
    public class AllotmentTransferRequest
    {
        public int FromAllotmentID { get; set; }
        public int ToAllotmentID { get; set; }
        public int RoomTypeID { get; set; }
        public int Quantity { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string CreateBy { get; set; }
        public string Description { get; set; }
        // Các trường bổ sung cho bảng Detail
        public int AllotmentStageID { get; set; }
        public int CutOffDay { get; set; }
        public DateTime? CutOffDate { get; set; }
    }
}

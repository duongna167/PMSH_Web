using System;
using System.Collections.Generic;

namespace Miscellaneous.Services.Interfaces
{
    public class MealChangeRequest
    {
        /// <summary>0: Change, 1: Add, 2: Cancel, 3: Lock</summary>
        public int Type { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        /// <summary>0: Breakfast, 1: Lunch, 2: Dinner</summary>
        public int FromMeal { get; set; }
        public int ToMeal { get; set; }
        // Cho phép để trống: project bật Nullable nên string thường sẽ bị coi là bắt buộc
        public string? Description { get; set; }
        public List<int> ReservationIds { get; set; } = new();
    }

    public class MealChangeResult
    {
        public int Processed { get; set; }
        public List<string> Skipped { get; } = new();
    }
}

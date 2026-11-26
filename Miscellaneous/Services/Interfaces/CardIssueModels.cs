using System.Collections.Generic;

namespace Miscellaneous.Services.Interfaces
{
    public class CardAssignment
    {
        public int ReservationId { get; set; }
        public string? CardId { get; set; }
    }

    public class CardIssueResult
    {
        public int Processed { get; set; }
        public List<string> Skipped { get; } = new();
    }
}

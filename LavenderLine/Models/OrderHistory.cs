using NodaTime;

namespace LavenderLine.Models
{
    public class OrderHistory
    {
        public int Id { get; set; }
        public long OrderId { get; set; }
        public string ChangedBy { get; set; }
        public Instant ChangeDate { get; set; }
        public string FieldChanged { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }

        public Order Order { get; set; }
    }
}

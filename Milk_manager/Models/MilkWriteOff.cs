using System;

namespace Milk_manager.Models
{
    public class MilkWriteOff
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public double Liters { get; set; }
        public string? Reason { get; set; }
    }
}

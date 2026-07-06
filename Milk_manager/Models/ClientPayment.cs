using System;

namespace Milk_manager.Models
{
    public class ClientPayment
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public string? Comment { get; set; }
    }
}

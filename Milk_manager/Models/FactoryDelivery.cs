using System;

namespace Milk_manager.Models
{
    public class FactoryDelivery
    {
        public int Id { get; set; }
        public string FactoryName { get; set; }
        public DateTime Date { get; set; }
        public double Liters { get; set; }
        public decimal PricePerLiter { get; set; }

        public decimal TotalSum => (decimal)Liters * PricePerLiter;
    }
}

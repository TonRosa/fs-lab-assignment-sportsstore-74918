using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.Contracts.DTOs;

namespace Shared.Contracts.Events
{
    public class OrderSubmitted
    {
        public Guid OrderId { get; set; }
        public Guid CustomerId { get; set; }
        public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public List<OrderItemDto> Items { get; set; } = new();
        public decimal TotalAmount { get; set; }
        public ShippingAddressDto ShippingAddress { get; set; } = new();
    }
}

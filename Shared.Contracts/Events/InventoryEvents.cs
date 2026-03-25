using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.Contracts.DTOs;

namespace Shared.Contracts.Events
{
    public class InventoryCheckRequested
    {
        public Guid OrderId { get; set; }
        public string CorrelationId { get; set; } = string.Empty;
        public List<OrderItemDto> Items { get; set; } = new();
    }

    public class InventoryConfirmed
    {
        public Guid OrderId { get; set; }
        public string CorrelationId { get; set; } = string.Empty;
        public DateTime ConfirmedAt { get; set; } = DateTime.UtcNow;
    }

    public class InventoryFailed
    {
        public Guid OrderId { get; set; }
        public string CorrelationId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }
}

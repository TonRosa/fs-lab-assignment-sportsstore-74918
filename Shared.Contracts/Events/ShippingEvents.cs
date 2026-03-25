using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.Contracts.DTOs;

namespace Shared.Contracts.Events
{
    public class ShippingRequested
    {
        public Guid OrderId { get; set; }
        public string CorrelationId { get; set; } = string.Empty;
        public ShippingAddressDto ShippingAddress { get; set; } = new();
    }

    public class ShippingCreated
    {
        public Guid OrderId { get; set; }
        public string CorrelationId { get; set; } = string.Empty;
        public string TrackingNumber { get; set; } = string.Empty;
        public DateTime EstimatedDispatch { get; set; }
    }

    public class ShippingFailed
    {
        public Guid OrderId { get; set; }
        public string CorrelationId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }
}

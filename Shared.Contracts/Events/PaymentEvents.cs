using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Contracts.Events
{
    public class PaymentProcessingRequested
    {
        public Guid OrderId { get; set; }
        public string CorrelationId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public Guid CustomerId { get; set; }
    }

    public class PaymentApproved
    {
        public Guid OrderId { get; set; }
        public string CorrelationId { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public DateTime ApprovedAt { get; set; } = DateTime.UtcNow;
    }

    public class PaymentRejected
    {
        public Guid OrderId { get; set; }
        public string CorrelationId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Contracts.Enums
{
    public enum OrderStatus
    {
        Submitted,
        InventoryPending,
        InventoryConfirmed,
        InventoryFailed,
        PaymentPending,
        PaymentApproved,
        PaymentFailed,
        ShippingPending,
        ShippingCreated,
        Completed,
        Failed
    }
}
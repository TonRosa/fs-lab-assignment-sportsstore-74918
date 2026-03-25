namespace OrderManagement.API.Messaging
{
    public static class QueueNames
    {
        public const string OrderSubmitted = "order.submitted";
        public const string InventoryCheckRequested = "inventory.check.requested";
        public const string InventoryConfirmed = "inventory.confirmed";
        public const string InventoryFailed = "inventory.failed";
        public const string PaymentProcessingRequested = "payment.processing.requested";
        public const string PaymentApproved = "payment.approved";
        public const string PaymentRejected = "payment.rejected";
        public const string ShippingRequested = "shipping.requested";
        public const string ShippingCreated = "shipping.created";
        public const string ShippingFailed = "shipping.failed";
    }
}

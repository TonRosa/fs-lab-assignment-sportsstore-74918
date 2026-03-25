using AutoMapper;
using OrderManagement.API.DTOs;
using OrderManagement.API.Models;

namespace OrderManagement.API.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Product
        CreateMap<Product, ProductDto>();

        // Order
        CreateMap<Order, OrderDto>()
            .ForMember(dest => dest.CustomerName,
                opt => opt.MapFrom(src => src.Customer.Name));

        // OrderItem
        CreateMap<OrderItem, OrderItemDto>();

        // Records
        CreateMap<PaymentRecord, PaymentRecordDto>();
        CreateMap<ShipmentRecord, ShipmentRecordDto>();
        CreateMap<InventoryRecord, InventoryRecordDto>();
    }
}
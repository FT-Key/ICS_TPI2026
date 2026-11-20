using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Domain.Entities;

namespace Dsw2025Tpi.Application.Mappers;

public static class OrderMappingExtensions
{
    public static OrderItemModel.ResponseOrderItemModel MapToResponse(this OrderItem item) =>
        new(
            item.Id,
            item.Quantity,
            item.UnitPrice,
            item.OrderId,
            item.ProductId,
            item.Subtotal);

    public static OrderModel.ResponseOrderModel MapToResponse(this Order order) =>
        new(
            order.Id,
            order.Date,
            order.ShippingAddress,
            order.BillingAddress,
            order.Notes,
            order.CustomerId,
            order.Customer?.Name,
            order.Status,
            order.TotalAmount,
            order.OrderItems.Select(i => i.MapToResponse()).ToList());
}

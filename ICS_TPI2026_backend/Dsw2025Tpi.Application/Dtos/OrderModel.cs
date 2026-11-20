using Dsw2025Tpi.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dsw2025Tpi.Application.Dtos;

public record OrderModel
{
    // El CustomerId no forma parte del request: se obtiene siempre del claim "id" del JWT (AT-01).
    public record RequestOrderModel(string? ShippingAddress, string? BillingAddress, string? Notes, List<OrderItemModel.RequestOrderItemModel> OrderItems);
    public record ResponseOrderModel(Guid Id, DateTime Date, string? ShippingAddress, string? BillingAddress, string? Notes, Guid CustomerId, string? CustomerName, OrderStatus Status, decimal TotatAmount, List<OrderItemModel.ResponseOrderItemModel> OrderItems);
    public record SearchOrder(Guid? CustomerId, string? Status, int PageNumber = 1, int PageSize = 10);

    public record ResponsePagination(IEnumerable<ResponseOrderModel> Items, int TotalCount);
}

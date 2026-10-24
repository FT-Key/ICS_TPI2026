using Azure.Core;
using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Application.Exceptions;
using Dsw2025Tpi.Application.Mappers;
using Dsw2025Tpi.Application.Validation;
using Dsw2025Tpi.Data.Repositories;
using Dsw2025Tpi.Domain.Entities;
using Dsw2025Tpi.Domain.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dsw2025Tpi.Application.Services
{
    public class OrdersManagementService
    {
        private readonly IRepository _repository;


        public OrdersManagementService(IRepository repository)
        {
            _repository = repository;
        }
        public async Task<OrderModel.ResponseOrderModel?> GetOrderById(Guid id)
        {
            var order = await _repository.GetById<Order>(id, nameof(Order.OrderItems), "OrderItems.Product");
            if (order == null) throw new EntityNotFoundException($"Order not found");

            return order.MapToResponse();
        }

        public async Task<OrderModel.ResponsePagination?> GetAllOrders(OrderModel.SearchOrder request)
        {
            // 1. Parseo de Estado (Igual que tenías)
            OrderStatus? status = null;
            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                if (!Enum.TryParse<OrderStatus>(request.Status, true, out var parsedStatus))
                {
                    // Opción: Ignorar filtro inválido o lanzar error. Aquí lanzamos.
                    throw new ArgumentException($"Estado de orden invalido: {request.Status}");
                }
                status = parsedStatus;
            }

            // 2. Validación de Cliente
            if (request.CustomerId.HasValue)
            {
                var customer = await _repository.GetById<Customer>(request.CustomerId.Value);
                if (customer == null)
                    throw new EntityNotFoundException($"Customer con el ID {request.CustomerId} not found.");
            }

            // 3. Obtener datos filtrados
            var orders = await _repository.GetFiltered<Order>(
                o =>
                    o.Status != OrderStatus.CANCELLED &&
                    (!request.CustomerId.HasValue || o.CustomerId == request.CustomerId.Value) &&
                    (!status.HasValue || o.Status == status.Value),
                include: new[] { "OrderItems.Product", "Customer" }
            );

            // 4. Calcular Total (Para la paginación)
            var totalCount = orders?.Count() ?? 0;

            // 5. Validar y Corregir Paginación
            int pageNumber = request.PageNumber <= 0 ? 1 : request.PageNumber;
            int pageSize = request.PageSize <= 0 ? 10 : request.PageSize;

            // 6. Paginar y Mapear
            var paginatedItems = (orders ?? Enumerable.Empty<Order>())
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(order => order.MapToResponse())
                .ToList();

            // 7. Devolver Objeto Paginado
            return new OrderModel.ResponsePagination(paginatedItems, totalCount);
        }

        // El customerId proviene del claim "id" del JWT, nunca del body (AT-01).
        public async Task<OrderModel.ResponseOrderModel> AddOrder(OrderModel.RequestOrderModel request, Guid customerId)
        {
            OrderValidator.Validate(request);

            if (request.OrderItems == null || !request.OrderItems.Any())
                throw new ArgumentException("La orden tiene que tener mas de un item.");

            var customer = await _repository.GetById<Customer>(customerId);
            if (customer == null)
                throw new EntityNotFoundException($"Customer con ID {customerId} not found.");

            var order = new Order(
                request.ShippingAddress,
                request.BillingAddress,
                request.Notes,
                customerId
            );

            // Una unica consulta batch (WHERE Id IN ...) para todos los productos de la orden.
            var productIds = request.OrderItems.Select(i => i.ProductId).Distinct().ToList();
            var products = await _repository.GetFiltered<Product>(p => productIds.Contains(p.Id))
                ?? Enumerable.Empty<Product>();
            var productsById = products.ToDictionary(p => p.Id);

            // Cantidad total pedida por producto (soporta el mismo producto repetido en varios items).
            var requestedQuantities = request.OrderItems
                .GroupBy(i => i.ProductId)
                .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

            var orderItems = new List<OrderItem>(request.OrderItems.Count);

            foreach (var item in request.OrderItems)
            {
                if (!productsById.TryGetValue(item.ProductId, out var product))
                    throw new EntityNotFoundException($"Producto not found: {item.ProductId}");

                if (product.StockQuantity < requestedQuantities[item.ProductId])
                    throw new InvalidOperationException($"Stock insuficiente: {product.Name}");

                var orderItem = new OrderItem(
                    item.Quantity,
                    product.CurrentUnitPrice,
                    order.Id,
                    product.Id
                );
                orderItems.Add(orderItem);
            }

            // El descuento se aplica en memoria sobre las entidades trackeadas...
            foreach (var (productId, quantity) in requestedQuantities)
            {
                productsById[productId].StockQuantity -= quantity;
            }

            order.OrderItems = orderItems;

            // ...y un unico SaveChangesAsync persiste la orden y el descuento de stock.
            await _repository.Add(order);

            return order.MapToResponse();
        }

        public async Task<OrderModel.ResponseOrderModel> UpdateOrderStatus(Guid id, string newStatus)
        {
            var order = await _repository.GetById<Order>(id, nameof(Order.OrderItems));

            if (order == null)
                throw new EntityNotFoundException($"Order con el ID: {id} not found");

            if (!Enum.TryParse<OrderStatus>(newStatus, true, out var status) || int.TryParse(newStatus, out _))
                throw new ArgumentException("El estado no es valido");

            if (status == OrderStatus.CANCELLED && order.OrderItems.Count > 0)
            {
                // Una unica consulta batch (WHERE Id IN ...) para todos los productos de la orden.
                var productIds = order.OrderItems.Select(i => i.ProductId).Distinct().ToList();
                var products = await _repository.GetFiltered<Product>(p => productIds.Contains(p.Id))
                    ?? Enumerable.Empty<Product>();
                var productsById = products.ToDictionary(p => p.Id);

                var quantitiesToRestore = order.OrderItems
                    .GroupBy(i => i.ProductId)
                    .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

                // Restitucion de stock en memoria sobre las entidades trackeadas.
                foreach (var (productId, quantity) in quantitiesToRestore)
                {
                    if (productsById.TryGetValue(productId, out var product))
                    {
                        product.StockQuantity += quantity;
                    }
                }
            }

            order.Status = status;

            // Un unico SaveChangesAsync persiste el estado y la restitucion de stock.
            await _repository.SaveChangesAsync();

            return order.MapToResponse();
        }
    }
}


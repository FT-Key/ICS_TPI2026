using Dsw2025Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Dsw2025Tpi.Data.Helpers;

public static class DbContextExtensions
{
    public static void Seedwork<T>(this Dsw2025TpiContext context, string dataSource) where T : class
    {
        if (context.Set<T>().Any()) return;
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, dataSource));
        var entities = JsonSerializer.Deserialize<List<T>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });
        if (entities == null || entities.Count == 0) return;

        // Para Orders, necesitamos manejar las dependencias
        if (typeof(T) == typeof(Order))
        {
            var orders = entities.Cast<Order>().ToList();
            foreach (var order in orders)
            {
                // Verificar que el Customer existe
                var customerExists = context.Customers.Any(c => c.Id == order.CustomerId);
                if (!customerExists)
                {
                    // Crear un customer por defecto o skip esta orden
                    Console.WriteLine($"Warning: Customer {order.CustomerId} not found for order {order.Id}. Skipping order.");
                    continue;
                }

                // Agregar la orden sin OrderItems primero
                var orderToAdd = new Order(order.ShippingAddress, order.BillingAddress, order.Notes, order.CustomerId)
                {
                    
                    Date = order.Date,
                    Status = order.Status
                };
                
                context.Orders.Add(orderToAdd);
            }
            context.SaveChanges();

            // manejar OrderItems por cada uno que estén en el JSON
            foreach (var order in orders)
            {
                if (order.OrderItems?.Any() == true)
                {
                    foreach (var item in order.OrderItems)
                    {
                        // Verificar que el Product existe
                        var product = context.Products.Find(item.ProductId);
                        if (product != null)
                        {
                            var orderItem = new OrderItem(item.Quantity, product.CurrentUnitPrice, order.Id, item.ProductId)
                            {
                                
                            };
                            context.OrderItems.Add(orderItem);
                        }
                    }
                }
            }
        }
        else
        {
            // Para otras entidades (Products, Customers), agregar normalmente
            context.Set<T>().AddRange(entities);
        }
        
        context.SaveChanges();

        // Actualizar precios después de que todo esté insertado
        if (typeof(T) == typeof(Order))
        {
            var orders = context.Orders
                .Include(o => o.OrderItems)
                .ToList();

            foreach (var order in orders)
            {
                foreach (var item in order.OrderItems)
                {
                    var product = context.Products.Find(item.ProductId);
                    if (product != null)
                    {
                        item.UnitPrice = product.CurrentUnitPrice;
                    }
                }
            }
            context.SaveChanges();
        }
    }
}
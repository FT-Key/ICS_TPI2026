using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Application.Exceptions;
using Dsw2025Tpi.Application.Services;
using Dsw2025Tpi.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Dsw2025Tpi.Api.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly OrdersManagementService _service;

    public OrdersController(OrdersManagementService service)
    {
        _service = service;
    }

    [HttpGet()]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllOrders([FromQuery] OrderModel.SearchOrder request)
    {
        var result = await _service.GetAllOrders(request);

        // Si el servicio devuelve null o la lista de items está vacía
        if (result == null || result.Items == null || !result.Items.Any())
            return NoContent();

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin, User")]
    public async Task<IActionResult> AddOrder([FromBody] OrderModel.RequestOrderModel request)
    {
        try
        {
            // BOLA Prevention: Obtener CustomerId del JWT claim "id"
            var userIdClaim = User.FindFirst("id");
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var authenticatedCustomerId))
            {
                return Unauthorized("Token inválido: no se encontró el ID del usuario.");
            }

            // Validar que el CustomerId del request coincida con el del JWT
            if (request.CustomerId != authenticatedCustomerId)
            {
                return Forbid("No tiene permiso para crear órdenes para otro cliente.");
            }

            var Order = await _service.AddOrder(request);
            
            return CreatedAtAction(nameof(GetOrderById), new { id = Order.Id }, Order);
        }
        catch (ArgumentException ae)
        {
            
            return BadRequest(ae.Message);
        }
        catch (InvalidOperationException ioe)
        {
            
            return BadRequest(ioe.Message);
        }
        catch (EntityNotFoundException ioe)
        {
            return BadRequest(ioe.Message);
        }
    }


    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetOrderById(Guid id)
    {
        try
        {
            
            var order = await _service.GetOrderById(id);
            
            return Ok(order);
        }
        catch (InvalidOperationException ioe)
        {
            
            return NotFound(ioe.Message);
        }
        catch (EntityNotFoundException ioe)
        {
            
            return NotFound(ioe.Message);
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateOrderStatus(Guid id, string newStatus)
    {
        try
        {
            
            var order = await _service.UpdateOrderStatus(id, newStatus);
            
            if (order == null) return NotFound();
            
            return Ok(order);
        }
        catch (ArgumentException ae)
        {
            
            return BadRequest(ae.Message);
        }
        catch (InvalidOperationException ioe)
        {
            
            return NotFound(ioe.Message);
        }
        catch (EntityNotFoundException ioe)
        {
            
            return NotFound(ioe.Message);
        }
    }
}


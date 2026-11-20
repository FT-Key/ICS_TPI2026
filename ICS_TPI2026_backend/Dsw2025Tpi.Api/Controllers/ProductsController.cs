using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Application.Exceptions;
using Dsw2025Tpi.Application.Services;
using Dsw2025Tpi.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ApplicationException = Dsw2025Tpi.Application.Exceptions.ApplicationException;

namespace Dsw2025Tpi.Api.Controllers;

[ApiController]
[Route("api/products")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly ProductsManagementService _service;

    public ProductsController(ProductsManagementService service)
    {
        _service = service;
    }

    /* [HttpGet()] codigo anterior donde muestra todos los productos sin paginacion
    [AllowAnonymous]
    public async Task<IActionResult> GetAllProducts()
    {
        var products = await _service.GetAllProducts();
        if (products == null || !products.Any()) return NoContent();
        return Ok(products);
    } */

    [HttpGet()]
    [AllowAnonymous]
    public async Task<IActionResult> GetProducts([FromQuery] ProductModel.FilterProduct filter)
    {
        // TP4-02: el catálogo público solo expone productos activos (se ignora "status").
        var result = await _service.GetProducts(filter, includeInactive: false);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,User")]
    public async Task<IActionResult> GetProductById(Guid id)
    {
        try
        {
            
            var product = await _service.GetProductById(id);
            
            return Ok(product);
        }
        catch (EntityNotFoundException ioe)
        {
            
            return NotFound(ioe.Message);
        }
    }

    [HttpPost()]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddProduct([FromBody] ProductModel.RequestProductModel request)
    {
        try
        {
            var product = await _service.AddProduct(request);
            // TP4-02: 201 Created con header Location apuntando a GET /api/products/{id}.
            return CreatedAtAction(nameof(GetProductById), new { id = product.Id }, product);
        }
        catch (DuplicatedEntityException de)
        {
            // TP4-02: SKU repetido -> 409 Conflict (nunca 500).
            return Conflict(de.Message);
        }
        catch (ArgumentException ae)
        {
            return BadRequest(ae.Message);
        }
        catch (ApplicationException ioe)
        {
            return BadRequest(ioe.Message);
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] ProductModel.RequestProductModel request)
    {
        try
        {
            
            var product = await _service.UpdateProduct(id, request);
            
            return Ok(product);
        }
        catch (EntityNotFoundException ioe)
        {
            
            return NotFound(ioe.Message);
        }
        catch (DuplicatedEntityException de)
        {
            return Conflict(de.Message);
        }
        catch (ArgumentException ae)
        {
            
            return BadRequest(ae.Message);
        }
        catch (ApplicationException ioe)
        {
            return BadRequest(ioe.Message);
        }
    }

    [HttpPatch("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> PatchProduct(Guid id)
    {
        try
        {
            
            await _service.PatchProduct(id);
            return NoContent();
        }
        catch (ApplicationException ioe)
        {
            
            return NotFound(ioe.Message);
        }
        catch (ArgumentException ae)
        {
            
            return BadRequest(ae.Message);
        }
    }

    /*dashboard!!!*/

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAuthProducts([FromQuery] ProductModel.FilterProduct request)
    {
        // Panel admin: incluye inactivos y respeta el filtro "status" (enabled / disabled).
        var products = await _service.GetProducts(request, includeInactive: true);
        return Ok(products);
    }
}

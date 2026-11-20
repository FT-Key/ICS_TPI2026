using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Application.Exceptions;
using Dsw2025Tpi.Application.Mappers;
using Dsw2025Tpi.Application.Validation;
using Dsw2025Tpi.Data.Repositories;
using Dsw2025Tpi.Domain.Entities;
using Dsw2025Tpi.Domain.Interfaces;
using Azure.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ApplicationException = Dsw2025Tpi.Application.Exceptions.ApplicationException;

namespace Dsw2025Tpi.Application.Services
{
    public class ProductsManagementService
    {
        private readonly ILogger<ProductsManagementService> _logger;
        private readonly IRepository _repository;

        public ProductsManagementService(IRepository repository, ILogger<ProductsManagementService> logger)
        {
            _repository = repository;
            _logger = logger;
        }
        public async Task<ProductModel.ResponseProductModel?> GetProductById(Guid id)
        {
            var product = await _repository.GetById<Product>(id);
            return product == null
                ? throw new EntityNotFoundException("Product not found")
                : product.MapToResponse();
        }

        public async Task<IEnumerable<ProductModel.ResponseProductModel>?> GetAllProducts()
        {
            return (await _repository
                .GetFiltered<Product>(p => p.IsActive))?
                .Select(p => p.MapToResponse());
        }

        public async Task<ProductModel.ResponseProductModel> AddProduct(ProductModel.RequestProductModel request)
        {
            ProductValidator.Validate(request);
            var exist = await _repository.First<Product>(p => p.Sku == request.Sku);
            if (exist != null) throw new DuplicatedEntityException($"Un producto con Sku {request.Sku} ya existe");
            var product = new Product(request.Sku, request.InternalCode, request.Name, request.Description, request.CurrentUnitPrice, request.StockQuantity);
            await _repository.Add(product);
            return product.MapToResponse();
        }
        public async Task<ProductModel.ResponseProductModel> UpdateProduct(Guid id, ProductModel.RequestProductModel request)
        {
            var exist = await _repository.GetById<Product>(id);
            if (exist == null)
                throw new EntityNotFoundException("Product not found");

            ProductValidator.Validate(request);

            var sku = await _repository.First<Product>(p => p.Sku == request.Sku && p.IsActive && p.Id != id);
            if (sku != null) throw new DuplicatedEntityException($"Un producto con Sku {request.Sku} ya existe");

            exist.Sku = request.Sku;
            exist.InternalCode = request.InternalCode;
            exist.Name = request.Name;
            exist.Description = request.Description;
            exist.CurrentUnitPrice = request.CurrentUnitPrice;
            exist.StockQuantity = request.StockQuantity;

            await _repository.Update(exist);

            return exist.MapToResponse();
        }

        public async Task PatchProduct(Guid id)
        {
            var exist = await _repository.GetById<Product>(id);
            if (exist == null)
                throw new EntityNotFoundException("Product not found");
            if (exist.IsActive == false)
                throw new ApplicationException("Producto ya deshabilitado");
            exist.IsActive = false;
            await _repository.Update(exist);
        }

        /// <summary>
        /// Listado paginado de productos (TP4-02).
        /// - includeInactive = false (catálogo público): solo productos activos, se ignora Status.
        /// - includeInactive = true (panel admin): respeta Status ("enabled" / "disabled" / todos).
        /// - Search busca en Name, Sku e InternalCode sin distinguir mayúsculas.
        /// - Orden estable por Sku y luego Id, Skip((page-1)*size).Take(size); TotalItems = total filtrado.
        /// </summary>
        public async Task<ProductModel.ResponsePagination> GetProducts(ProductModel.FilterProduct request, bool includeInactive = false)
        {
            bool? isActive = includeInactive
                ? request.Status switch
                {
                    "enabled" => true,
                    "disabled" => false,
                    _ => null
                }
                : true;

            var search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim().ToLower();

            _logger.LogInformation("Consulta de productos (includeInactive: {IncludeInactive}, search: {Search})", includeInactive, search);

            // ToLower().Contains se traduce a SQL (LOWER(...) LIKE) tanto en SQL Server como en SQLite.
            var filtered = await _repository.GetFiltered<Product>(p =>
                (isActive == null || p.IsActive == isActive)
                && (search == null
                    || (p.Name != null && p.Name.ToLower().Contains(search))
                    || (p.Sku != null && p.Sku.ToLower().Contains(search))
                    || (p.InternalCode != null && p.InternalCode.ToLower().Contains(search))));

            var ordered = (filtered ?? Enumerable.Empty<Product>())
                .OrderBy(p => p.Sku, StringComparer.Ordinal)
                .ThenBy(p => p.Id)
                .ToList();

            var totalItems = ordered.Count;
            var pageNumber = request.pageNumber is > 0 ? request.pageNumber.Value : 1;
            var pageSize = request.PageSize is > 0 ? request.PageSize.Value : Math.Max(totalItems, 1);

            var page = ordered
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(p => p.MapToResponse())
                .ToList();

            return new ProductModel.ResponsePagination(page, totalItems);
        }
    }
}


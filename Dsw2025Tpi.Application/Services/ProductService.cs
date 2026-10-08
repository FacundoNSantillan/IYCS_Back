using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Application.Exceptions;
using Dsw2025Tpi.Domain.Entities;
using Dsw2025Tpi.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace Dsw2025Tpi.Application.Services
{
    public class ProductService
    {
        private readonly IRepository _repository;
        public ProductService(IRepository repository)
        {
            _repository = repository;
        }

        public async Task<ProductModel.ProductResponse> CreateProduct(ProductModel.ProductRequest request)
        {
            var validationErrors = new Dictionary<string, string>();

            if (request.CurrentUnitPrice <= 0) validationErrors.Add("price", "El precio debe ser mayor a cero.");
            if (request.StockQuantity < 0) validationErrors.Add("stock", "El stock no puede ser negativo.");

            var skuExists = await _repository.First<Product>(p => p.Sku == request.Sku);
            if (skuExists != null) validationErrors.Add("sku", "Este SKU ya está en uso.");

            var codeExists = await _repository.First<Product>(p => p.InternalCode == request.InternalCode);
            if (codeExists != null) validationErrors.Add("cui", "Este Código Único ya existe.");

            var nameExists = await _repository.First<Product>(p => p.Name == request.Name);
            if (nameExists != null) validationErrors.Add("name", "Ya existe un producto con este nombre.");

            if (validationErrors.Count > 0)
            {
                var jsonError = JsonSerializer.Serialize(validationErrors);
                throw new BadRequestException(jsonError);
            }

            var product = new Product(
                        request.Sku,
                        request.InternalCode,
                        request.Name,
                        request.Description,
                        request.CurrentUnitPrice,
                        request.StockQuantity);

            await _repository.Add(product);

            return new ProductModel.ProductResponse(
                product.Id,
                product.Sku!,
                product.InternalCode!,
                product.Name!,
                product.Description!,
                product.CurrentUnitPrice,
                product.StockQuantity,
                product.IsActive);
        }

        public async Task<PagedModel.PagedResponse<ProductModel.ProductResponse>?> GetAllProducts(
            string? search,
            bool? isActive,
            bool? hasStock,
            int pageNumber = 1,
            int pageSize = 10)
        {
            if (_repository is null)
            {
                throw new NoContentException("There aren´t products in the Data Base.");
            }

            var allProducts = await _repository.GetAll<Product>();
            var query = allProducts.AsQueryable();

            if (isActive.HasValue)
            {
                query = query.Where(p => p.IsActive == isActive.Value);
            }

            if (hasStock.HasValue && hasStock.Value)
            {
                query = query.Where(p => p.StockQuantity > 0);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower().Trim();
                query = query.Where(p => p.Name.ToLower().Contains(search));
            }

            var productsFiltered = query.ToList();
            var total = productsFiltered.Count();

            if (total == 0)
                return null;

            var pagedProducts = productsFiltered
                .OrderBy(p => p.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductModel.ProductResponse(
                    p.Id,
                    p.Sku,
                    p.InternalCode,
                    p.Name,
                    p.Description,
                    p.CurrentUnitPrice,
                    p.StockQuantity,
                    p.IsActive
                )).ToList();

            return new PagedModel.PagedResponse<ProductModel.ProductResponse>(
                pageNumber,
                pageSize,
                total,
                pagedProducts
        );
        }

        public async Task<ProductModel.ProductResponse?> GetProductById(Guid id)
        {
            if (id == Guid.Empty || _repository is null)
            {
                throw new NotFoundException("There isn't a product with the provided ID.");
            }

            var product = await _repository.GetById<Product>(id);

            if (product is null)
            {
                throw new NotFoundException("There isn't a product in the Data Base.");
            }

            return new ProductModel.ProductResponse(
                product.Id,
                product.Sku!,
                product.InternalCode!,
                product.Name!, 
                product.Description!,
                product.CurrentUnitPrice,
                product.StockQuantity,
                product.IsActive
            );
                
        }

        public async Task<ProductModel.ProductResponse?> UpdateProduct(Guid id, ProductModel.ProductRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Sku))
                throw new BadRequestException("The SKU is obligatory.");

            if (string.IsNullOrWhiteSpace(request.Name))
                throw new BadRequestException("The NAME is obligatory.");

            if (request.CurrentUnitPrice <= 0)
                throw new BadRequestException("The product price must be greater than zero.");

            if (request.StockQuantity < 0)
                throw new BadRequestException("The STOCK product can't be negative.");

            var product = await _repository.GetById<Product>(id);

            product!.Sku = request.Sku!;
            product.InternalCode = request.InternalCode;
            product.Name = request.Name;
            product.Description = request.Description;
            product.CurrentUnitPrice = request.CurrentUnitPrice;
            product.StockQuantity = (int)request.StockQuantity;
            product.IsActive = request.IsActive;

            var productUpdate = await _repository.Update(product);

            return new ProductModel.ProductResponse(
                productUpdate.Id,
                productUpdate.Sku!,
                productUpdate.InternalCode!,
                productUpdate.Name!,
                productUpdate.Description!,
                productUpdate.CurrentUnitPrice,
                productUpdate.StockQuantity,
                productUpdate.IsActive);

        }

        public async Task<ProductDisabledModel.ProductDisabledResponse?> DisabledProduct(Guid id, ProductDisabledModel.ProductDisabledRequest request)
        {

             if (id == Guid.Empty || _repository is null)
            {
                throw new NotFoundException("There isn´t a product with the povided ID.");
            }

            var product = await _repository.GetById<Product>(id);

            product.IsActive = request.IsActive;

            var stateUpdate = await _repository.Update(product);

            return new ProductDisabledModel.ProductDisabledResponse(
                stateUpdate.Id,
                stateUpdate.IsActive);

        }

        

    }
}

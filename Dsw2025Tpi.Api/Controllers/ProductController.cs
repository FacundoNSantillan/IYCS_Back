using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Application.Services;
using Dsw2025Tpi.Domain.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dsw2025Tpi.Api.Controllers
{

    [ApiController]
    [Route("/api/products")]
    public class ProductController : Controller
    {
        private readonly ProductService _productService;

        public ProductController(ProductService productService)
        {
            _productService = productService;
        }

        [HttpPost("/api/products")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateProduct([FromBody] ProductModel.ProductRequest request)
        {
            try
            {
                var product = await _productService.CreateProduct(request);

                return Created($"/api/products/{product.Id}", product);

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("/api/products")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllProducts(
            string? search,
            [FromQuery] bool? isActive,
            [FromQuery] bool? hasStock,
            [FromQuery(Name = "pageNumber")] int pageNumber = 1,
            [FromQuery(Name = "pageSize")] int pageSize = 10)
        {
            try
            {

                if (pageNumber <= 0)
                {
                    return BadRequest("Page number must be greater than 0.");
                }

                if (pageSize <= 0 || pageSize > 100)
                {
                    return BadRequest("Page size must be between 1 and 100.");
                }

                var products = await _productService.GetAllProducts(search, isActive, hasStock, pageNumber, pageSize);

                if (products is null || !products.Items.Any())
                {
                    return NotFound("There aren´t products available.");
                }

                return Ok(products);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error obtaining products: {ex.Message}");
            }
        }


        [HttpGet("/api/products/{id}")]
        public async Task<IActionResult> GetProductById(Guid id)
        {

            try
            {
                var product = await _productService.GetProductById(id);
                if (product is null)
                {
                    return NotFound("Product not found.");
                }
                return Ok(product);
            }
            catch (Exception ex)
            {
                return NotFound($"Error obtaining products: {ex.Message}");

            }
        }


        [HttpPut("/api/products/{id}")]
        public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] ProductModel.ProductRequest request)
        {
            try
            {
                var updatedProduct = await _productService.UpdateProduct(id, request);

                return Ok(updatedProduct);

            }
            catch (Exception ex)
            {
                return BadRequest($"Error updating the product: {ex.Message}");

            }


        }

        [HttpPatch("/api/products/{id}")]
        public async Task<IActionResult> DisabledProduct(Guid id, [FromBody] ProductDisabledModel.ProductDisabledRequest request)
        {

            try
            {
                var productDisabled = await _productService.DisabledProduct(id, request);

                return NoContent();

            }
            catch (Exception ex)
            {
                return BadRequest($"Error disabling the product: {ex.Message}");
            }


        }

    }
}

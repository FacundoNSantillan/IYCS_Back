using Microsoft.AspNetCore.Mvc;
using Dsw2025Tpi.Application.Services;
using Dsw2025Tpi.Domain.Entities;
using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Application.Exceptions;
using Microsoft.EntityFrameworkCore.Storage.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Dsw2025Tpi.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("/api/orders")]
    public class OrderController : ControllerBase
    {
        private readonly OrderService _orderService;
        public OrderController(OrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpPost("/api/orders")]
        [Authorize]
        public async Task<IActionResult> CreateOrder([FromBody] OrderModel.OrderRequest request)
        {
            try
            {
                var claimValue = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(claimValue))
                    return Unauthorized("Usuario no identificado en el token.");

                var newRequest = request with
                {
                    UserName = claimValue,
                    CustomerId = null
                };

                var order = await _orderService.CreateOrderAsync(newRequest);
                return Created($"/api/orders/{order.Id}",order);
            }
            catch (BadRequestException ex)
            {
                return BadRequest($"Error creating order: {ex.Message}");
            }
        }

        [HttpGet("/api/orders")]
        public async Task<IActionResult> GetAllOrders(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] Guid? customer,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(status) && !Enum.TryParse<OrderStatus>(status, true, out _))
                {
                    return BadRequest($"Invalid status '{status}'.");
                }
                else if (pageNumber <= 0)
                {
                    return BadRequest("Page number must be greater than 0.");
                }
                else if (pageSize <= 0 || pageSize > 100)
                {
                    return BadRequest("Page size must be between 1 and 100.");
                }

                var orders = await _orderService.GetAllOrders(search, status, customer, pageNumber, pageSize);

                if (orders is null || !orders.Items.Any())
                {
                    if (!string.IsNullOrWhiteSpace(status))
                    {
                        return NotFound($"There aren't orders with the status: {status}.");
                    }
                    else if (!string.IsNullOrWhiteSpace(search))
                    {
                        return NotFound($"There aren't orders that match the search: '{search}'.");
                    }
                    else
                    {
                        return NotFound("There aren't orders in the system.");
                    }
                }

                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Internal server error.",
                    error = ex.Message
                });
            }
        }

        [HttpGet("/api/orders/{id}")]
        public async Task<IActionResult> GetOrderById(Guid id)
        {
            try
            {
                var order = await _orderService.GetOrderById(id);

                if (order is null)
                {
                    return NotFound("Order not found.");
                }
                return Ok(order);
            }
            catch (Exception ex)
            {
                return NotFound($"Error finding order: {ex.Message}");
            }
        }

        [HttpPut("/api/orders/{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(Guid id, [FromBody] UpdateOrderStatusModel.UpdateOrderStatusRequest request)
        {
            try
            {
                var updateOrder = await _orderService.UpdateOrderStatus(id, request);
                return Ok(updateOrder);
            }
            catch (Exception ex) 
            {
                return BadRequest($"Error updating order: {ex.Message}");
            }
        }
    }
}

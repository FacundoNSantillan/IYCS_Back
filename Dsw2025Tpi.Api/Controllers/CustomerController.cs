using Dsw2025Tpi.Application.Services;
using Dsw2025Tpi.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dsw2025Tpi.Api.Controllers
{

    [ApiController]
    [Authorize]
    [Route("api/customers")]
    public class CustomerController : ControllerBase
    {
        private readonly CustomersService _customer;
        public CustomerController(CustomersService custService)
        {
            _customer = custService;
        }


        [HttpGet("/api/customers")]
        [AllowAnonymous]
     
        public async Task<IActionResult> GetAllCustomers()
        {
            try
            {

                var customers = await _customer.GetAllCustomers();
                if (customers is null || !customers.Any())
                {
                    return NotFound("There aren't customers available.");
                }
                return Ok(customers);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error obtaining customers: {ex.Message}");
            }


        } 

        }
}

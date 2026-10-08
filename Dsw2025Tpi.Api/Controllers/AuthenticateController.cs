using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Application.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Dsw2025Ej15.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthenticateController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager; 
    private readonly SignInManager<IdentityUser> _signInManager; 
    private readonly JwtTokenService _jwtTokenService;
    private readonly AuthenticateServices _authService;

    public AuthenticateController(UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        JwtTokenService jwtTokenService,
        AuthenticateServices authServices)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
        _authService = authServices;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginModel request)
    {

        try
        {
            var token = await _authService.LoginUser(request);

            if (token == null)
                return Unauthorized("Invalid username or password.");

            return Ok(new { token });
        }
        catch (Exception ex)
        {
            
            return StatusCode(500, $"An error occurred while logging in: {ex.Message}");
        }
        
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterModel registerModel)
    {
        try
        {
            var (succeeded, errors) = await _authService.RegisterUser(registerModel);

            if (!succeeded)
                return BadRequest(new { errors });

            return Ok("User successfully registered.");
        }
        catch (Exception ex)
        {
            
            return StatusCode(500, $"An error occurred while registering the user: {ex.Message}");
        }

    }
}

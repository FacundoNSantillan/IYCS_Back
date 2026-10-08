using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Domain.Entities;
using Dsw2025Tpi.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dsw2025Tpi.Application.Services
{
    public class AuthenticateServices
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly JwtTokenService _jwtTokenService;
        private readonly IRepository _repository;

        public AuthenticateServices(UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            JwtTokenService jwtTokenService,
            IRepository repository)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _jwtTokenService = jwtTokenService;
            _repository = repository;
        }

        public async Task<string?> LoginUser(LoginModel loginModel)
        {

            if (string.IsNullOrWhiteSpace(loginModel.Username) || string.IsNullOrWhiteSpace(loginModel.Password))
                return null;

            var user = await _userManager.FindByNameAsync(loginModel.Username);
            if (user == null)
                return null;

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginModel.Password, false);
            if (!result.Succeeded)
                return null;

            return await _jwtTokenService.GenerateToken(loginModel.Username);
        }

        public async Task<(bool Succeeded, IEnumerable<string> Errors)> RegisterUser(RegisterModel registerModel)
        {
            var errors = new List<string>();

          
            if (string.IsNullOrWhiteSpace(registerModel.Username))
                errors.Add("Username is required.");

            if (string.IsNullOrWhiteSpace(registerModel.Password))
                errors.Add("Password is required.");

            if (string.IsNullOrWhiteSpace(registerModel.Email))
                errors.Add("Email is required.");

            if (registerModel.Password?.Length < 8)
                errors.Add("Password must be at least 8 characters long.");

            if (errors.Any())
                return (false, errors);

            var userExists = await _userManager.FindByNameAsync(registerModel.Username);
            if (userExists != null)
            {
                return (false, new List<string> { "Username is already taken." });
            }

            var user = new IdentityUser
            {
                UserName = registerModel.Username,
                Email = registerModel.Email
            };
            var result = await _userManager.CreateAsync(user, registerModel.Password);

            if (result.Succeeded)
            {
                try
                {
                    var newCustomer = new Customer(
                    registerModel.Username,
                    registerModel.Email,
                    "Sin teléfono"
                    );
                    await _repository.Add(newCustomer);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error creando customer: {ex.Message}");
                }
                if (!await _roleManager.RoleExistsAsync("Admin"))
                    await _roleManager.CreateAsync(new IdentityRole("Admin"));
                if (!await _roleManager.RoleExistsAsync("Client"))
                    await _roleManager.CreateAsync(new IdentityRole("Client"));

                var userCount = _userManager.Users.Count();
                var roleName = userCount == 1 ? "Admin" : "Client";

                await _userManager.AddToRoleAsync(user, roleName);
            }

            return (result.Succeeded, result.Errors.Select(e => e.Description));
        }

    }
}

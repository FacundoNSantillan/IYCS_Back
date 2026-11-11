using Dsw2025Tpi.Api;
using Dsw2025Tpi.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
namespace Dsw2025Tpi.IntegrationTests;
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string TestJwtKey = "testing-key-with-at-least-thirty-two-characters";
    private readonly SqliteConnection _applicationConnection = new("Data Source=:memory:");
    private readonly SqliteConnection _authenticationConnection = new("Data Source=:memory:");
    public CustomWebApplicationFactory()
    {
        _applicationConnection.Open();
        _authenticationConnection.Open();
    }
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration(configuration =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = TestJwtKey
            });
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<Dsw2025TpiContext>>();
            services.RemoveAll<Dsw2025TpiContext>();
            services.RemoveAll<IDbContextOptionsConfiguration<Dsw2025TpiContext>>();
            services.RemoveAll<DbContextOptions<AuthenticateContext>>();
            services.RemoveAll<AuthenticateContext>();
            services.RemoveAll<IDbContextOptionsConfiguration<AuthenticateContext>>();
            services.AddDbContext<Dsw2025TpiContext>(options => options.UseSqlite(_applicationConnection));
            services.AddDbContext<AuthenticateContext>(options => options.UseSqlite(_authenticationConnection));
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters.IssuerSigningKey = new
                SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtKey));
                options.TokenValidationParameters.ValidateIssuerSigningKey = true;
                options.TokenValidationParameters.ValidateIssuer = false;
                options.TokenValidationParameters.ValidateAudience = false;
                options.TokenValidationParameters.RoleClaimType = ClaimTypes.Role;
            });
        });
    }
    public async Task InitializeDatabasesAsync()
    {
        using var scope = Services.CreateScope();
        var applicationDb = scope.ServiceProvider.GetRequiredService<Dsw2025TpiContext>();
        var authenticationDb = scope.ServiceProvider.GetRequiredService<AuthenticateContext>();
        await applicationDb.Database.EnsureCreatedAsync();
        await authenticationDb.Database.EnsureCreatedAsync();
    }
    public async Task<string> CreateTokenAsync(string username, string role) =>
    (await CreateUserAsync(username, role)).Token;
    public async Task<TestUser> CreateUserAsync(string username, string role)
    {
        using var scope = Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        if (!await roleManager.RoleExistsAsync(role))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole(role));
            if (!roleResult.Succeeded)
                throw new InvalidOperationException("Could not create test role.");
        }
        var user = new IdentityUser(username)
        {
            Email = $"{username}@test.com"
        };
        var userResult = await userManager.CreateAsync(user, "Password123!");
        if (!userResult.Succeeded)
            throw new InvalidOperationException("Could not create test user.");
        var roleResultForUser = await userManager.AddToRoleAsync(user, role);
        if (!roleResultForUser.Succeeded)
            throw new InvalidOperationException("Could not assign test role.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtKey));
        var token = new JwtSecurityToken(
        issuer: "integration-tests",
        audience: "integration-tests",
        claims:
        [
        new Claim(JwtRegisteredClaimNames.Sub, username),
new Claim(ClaimTypes.NameIdentifier, user.Id),
new Claim(ClaimTypes.Role, role)
        ],
        expires: DateTime.UtcNow.AddMinutes(10),
        signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new TestUser(new JwtSecurityTokenHandler().WriteToken(token), user.Id);
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _applicationConnection.Dispose();
            _authenticationConnection.Dispose();
        }
        base.Dispose(disposing);
    }
}
public sealed record TestUser(string Token, string Id);

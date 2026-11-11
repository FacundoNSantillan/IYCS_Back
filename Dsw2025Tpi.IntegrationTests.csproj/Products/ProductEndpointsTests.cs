using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Data;
using Dsw2025Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
namespace Dsw2025Tpi.IntegrationTests.Products;
public class ProductEndpointsTests
{
    [Fact]
    public async Task CreateProduct_WhenUnauthenticated_ReturnsUnauthorized()
    {
        using var factory = new CustomWebApplicationFactory();
        await factory.InitializeDatabasesAsync();
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/products", new
        {
            sku = "SKU-001",
            internalCode = "CODE-001",
            name = "Mouse Gamer",
            description = "Product for tests",
            currentUnitPrice = 100,
            stockQuantity = 10,
            isActive = true
        });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
    [Fact]
    public async Task CreateProduct_WhenAuthenticatedAsClient_ReturnsForbidden()
    {
        using var factory = new CustomWebApplicationFactory();
        await factory.InitializeDatabasesAsync();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await
        factory.CreateTokenAsync("client", "Client"));
        var response = await client.PostAsJsonAsync("/api/products", new
        {
            sku = "SKU-001",
            internalCode = "CODE-001",
            name = "Mouse Gamer",
            description = "Product for tests",
            currentUnitPrice = 100,
            stockQuantity = 10,
            isActive = true
        });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
    [Fact]
    public async Task CreateProduct_WhenAuthenticatedAsAdmin_PersistsInSqliteAndReturnsCreated()
    {
        using var factory = new CustomWebApplicationFactory();
        await factory.InitializeDatabasesAsync();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await
        factory.CreateTokenAsync("admin", "Admin"));
        var response = await client.PostAsJsonAsync("/api/products", new
        {
            sku = "SKU-001",
            internalCode = "CODE-001",
            name = "Mouse Gamer",
            description = "Product for tests",
            currentUnitPrice = 100,
            stockQuantity = 10,
            isActive = true
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Dsw2025TpiContext>();
        var product = await db.Products.SingleAsync(item => item.Sku == "SKU-001");
        Assert.Equal("Mouse Gamer", product.Name);
        Assert.Equal(100, product.CurrentUnitPrice);
        Assert.Equal(10, product.StockQuantity);
    }
    [Fact]
    public async Task CreateProduct_WhenSkuAlreadyExists_ReturnsBadRequest()
    {
        using var factory = new CustomWebApplicationFactory();
        await factory.InitializeDatabasesAsync();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Dsw2025TpiContext>();
        db.Products.Add(new Product("SKU-001", "CODE-001", "Existing", "Product for tests", 100, 10));
        await db.SaveChangesAsync();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await
        factory.CreateTokenAsync("admin", "Admin"));
        var response = await client.PostAsJsonAsync("/api/products", new
        {
            sku = "SKU-001",
            internalCode = "CODE-002",
            name = "Mouse Gamer",
            description = "Product for tests",
            currentUnitPrice = 100,
            stockQuantity = 10,
            isActive = true
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    [Fact]
    public async Task GetProducts_WithQueryStringFilters_ReturnsFilteredResultsFromSqlite()
    {
        using var factory = new CustomWebApplicationFactory();
        await factory.InitializeDatabasesAsync();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Dsw2025TpiContext>();
        db.Products.AddRange(
        new Product("SKU-MOUSE", "CODE-MOUSE", "Mouse Gamer", "Product for tests", 100, 10),
        new Product("SKU-KEYBOARD", "CODE-KEYBOARD", "Teclado Mecánico", "Product for tests", 200, 10),
        new Product("SKU-MONITOR", "CODE-MONITOR", "Monitor", "Product for tests", 300, 10));
        await db.SaveChangesAsync();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/products?search=mouse&pageNumber=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<PagedModel.PagedResponse<ProductModel.ProductResponse>>();
        Assert.NotNull(payload);
        var product = Assert.Single(payload.Items);
        Assert.Equal("Mouse Gamer", product.Name);
        Assert.Equal(1, payload.TotalCount);
    }
    [Fact]
    public async Task GetProducts_PublicEndpoint_ReturnsOnlyActiveProducts()
    {
        using var factory = new CustomWebApplicationFactory();
        await factory.InitializeDatabasesAsync();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Dsw2025TpiContext>();
            var active = new Product("SKU-ACTIVE", "CODE-ACTIVE", "Active product", "Product for tests", 100, 10);
            var inactive = new Product("SKU-INACTIVE", "CODE-INACTIVE", "Inactive product", "Product for tests", 100, 10)
            {
                IsActive = false
            };
            db.Products.AddRange(active, inactive);
            await db.SaveChangesAsync();
        }
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/products");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<PagedModel.PagedResponse<ProductModel.ProductResponse>>();
        Assert.NotNull(payload);
        var product = Assert.Single(payload.Items);
        Assert.True(product.IsActive);
    }
    [Fact]
    public async Task GetAuthProducts_WhenCalledByAdmin_IncludesInactiveProducts()
    {
        using var factory = new CustomWebApplicationFactory();
        await factory.InitializeDatabasesAsync();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Dsw2025TpiContext>();
            db.Products.Add(new Product("SKU-INACTIVE", "CODE-INACTIVE", "Inactive product", "Product for tests", 100, 10)
            {
                IsActive = false
            });
            await db.SaveChangesAsync();
        }
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await
        factory.CreateTokenAsync("admin", "Admin"));
        var response = await client.GetAsync("/api/products/admin");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<PagedModel.PagedResponse<ProductModel.ProductResponse>>();
        Assert.NotNull(payload);
        var product = Assert.Single(payload.Items);
        Assert.False(product.IsActive);
    }
}
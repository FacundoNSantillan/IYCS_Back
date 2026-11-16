using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Dsw2025Tpi.Data;
using Dsw2025Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dsw2025Tpi.IntegrationTests.Orders;

public class OrderEndpointsTests
{
    [Fact]
    public async Task CreateOrder_WhenUnauthenticated_ReturnsUnauthorized()
    {
        using var factory = new CustomWebApplicationFactory();
        await factory.InitializeDatabasesAsync();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/orders", CreateRequest(Guid.NewGuid(), 1));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrder_WhenAuthenticatedWithValidData_PersistsOrderAndDecrementsStock()
    {
        using var factory = new CustomWebApplicationFactory();
        await factory.InitializeDatabasesAsync();
        var user = await factory.CreateUserAsync("customer", "Client");
        var product = new Product("SKU-001", "CODE-001", "Mouse Gamer", "Product for tests", 10, 10);

        using (var seedScope = factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<Dsw2025TpiContext>();
            db.Customers.Add(new Customer("Customer", "customer@test.com", "123", user.Id)
            {
                CustomerId = Guid.NewGuid()
            });
            db.Products.Add(product);
            await db.SaveChangesAsync();
        }

        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);
        var response = await client.PostAsJsonAsync("/api/orders", CreateRequest(product.Id, 3));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var verificationScope = factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<Dsw2025TpiContext>();
        Assert.Single(await verificationDb.Orders.ToListAsync());
        var storedProduct = await verificationDb.Products.SingleAsync(item => item.Id == product.Id);
        Assert.Equal(7, storedProduct.StockQuantity);
    }

    [Fact]
    public async Task CreateOrder_WhenStockIsInsufficient_ReturnsBadRequestAndDoesNotPersistOrder()
    {
        using var factory = new CustomWebApplicationFactory();
        await factory.InitializeDatabasesAsync();
        var user = await factory.CreateUserAsync("customer", "Client");
        var product = new Product("SKU-001", "CODE-001", "Mouse Gamer", "Product for tests", 10, 2);

        using (var seedScope = factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<Dsw2025TpiContext>();
            db.Customers.Add(new Customer("Customer", "customer@test.com", "123", user.Id)
            {
                CustomerId = Guid.NewGuid()
            });
            db.Products.Add(product);
            await db.SaveChangesAsync();
        }

        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);
        var response = await client.PostAsJsonAsync("/api/orders", CreateRequest(product.Id, 5));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var verificationScope = factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<Dsw2025TpiContext>();
        Assert.Empty(await verificationDb.Orders.ToListAsync());
        var storedProduct = await verificationDb.Products.SingleAsync(item => item.Id == product.Id);
        Assert.Equal(2, storedProduct.StockQuantity);
    }

    private static object CreateRequest(Guid productId, int quantity) => new
    {
        customerId = (Guid?)null,
        userName = (string?)null,
        orderItems = new[] { new { productId, quantity } },
        shippingAddress = "Street 123",
        billingAddress = "Street 123",
        notes = "Order for tests"
    };
}

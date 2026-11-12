using System.Linq.Expressions;
using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Application.Exceptions;
using Dsw2025Tpi.Application.Services;
using Dsw2025Tpi.Domain.Entities;
using Dsw2025Tpi.Domain.Interfaces;
using Moq;

namespace Dsw2025Tpi.UnitTests.Services;

public class OrderServiceTests
{
    [Fact]
    public async Task CreateOrder_WithValidItemsAndStock_CalculatesTotalAndDecrementsProductStock()
    {
        var customer = CreateCustomer();
        var product = new Product("SKU-001", "CODE-001", "Mouse Gamer", "Product for tests", 10, 10);
        var repository = CreateRepository(customer, product);
        var service = new OrderService(repository.Object);

        var response = await service.CreateOrderAsync(CreateRequest(product.Id, 3));

        Assert.Equal(30, response.TotalAmout);
        Assert.Equal(7, product.StockQuantity);
        repository.Verify(r => r.Add(It.IsAny<Order>()), Times.Once);
        repository.Verify(r => r.Update(It.Is<Product>(p => p.Id == product.Id && p.StockQuantity == 7)), Times.Once);
        repository.Verify(r => r.Update(It.Is<Order>(o => o.TotalAmount == 30)), Times.Once);
    }

    [Fact]
    public async Task CreateOrder_WhenProductStockIsInsufficient_ThrowsAndDoesNotPersistOrder()
    {
        var customer = CreateCustomer();
        var product = new Product("SKU-001", "CODE-001", "Mouse Gamer", "Product for tests", 10, 2);
        var repository = CreateRepository(customer, product);
        var service = new OrderService(repository.Object);

        await Assert.ThrowsAsync<BadRequestException>(() => service.CreateOrderAsync(CreateRequest(product.Id, 5)));

        Assert.Equal(2, product.StockQuantity);
        repository.Verify(r => r.Add(It.IsAny<Order>()), Times.Never);
        repository.Verify(r => r.Update(It.IsAny<Product>()), Times.Never);
    }

    [Fact]
    public async Task CreateOrder_WhenRepeatedProductQuantityExceedsStock_DoesNotPersistOrder()
    {
        var customer = CreateCustomer();
        var product = new Product("SKU-001", "CODE-001", "Mouse Gamer", "Product for tests", 10, 5);
        var repository = CreateRepository(customer, product);
        var service = new OrderService(repository.Object);

        var request = new OrderModel.OrderRequest(
            null,
            "user-id",
            [new OrderItemModel.Request(product.Id, 3), new OrderItemModel.Request(product.Id, 3)],
            "Street 123",
            "Street 123",
            null);

        await Assert.ThrowsAsync<BadRequestException>(() => service.CreateOrderAsync(request));

        Assert.Equal(5, product.StockQuantity);
        repository.Verify(r => r.Add(It.IsAny<Order>()), Times.Never);
        repository.Verify(r => r.Update(It.IsAny<Product>()), Times.Never);
    }

    [Fact]
    public async Task CreateOrder_WhenCustomerDoesNotExist_ThrowsAndDoesNotChangeProductsOrOrders()
    {
        var repository = new Mock<IRepository>();
        var service = new OrderService(repository.Object);

        await Assert.ThrowsAsync<BadRequestException>(() => service.CreateOrderAsync(CreateRequest(Guid.NewGuid(), 1)));

        repository.Verify(r => r.Add(It.IsAny<Order>()), Times.Never);
        repository.Verify(r => r.Update(It.IsAny<Product>()), Times.Never);
    }

    private static Mock<IRepository> CreateRepository(Customer customer, Product product)
    {
        var repository = new Mock<IRepository>();
        repository.Setup(r => r.First<Customer>(
                It.IsAny<Expression<Func<Customer, bool>>>(),
                It.IsAny<string[]>()))
            .ReturnsAsync(customer);

        repository.Setup(r => r.First<Product>(
                It.IsAny<Expression<Func<Product, bool>>>(),
                It.IsAny<string[]>()))
            .ReturnsAsync(product);

        repository.Setup(r => r.Add(It.IsAny<Order>()))
            .ReturnsAsync((Order order) => order);

        repository.Setup(r => r.Update(It.IsAny<Product>()))
            .ReturnsAsync((Product item) => item);

        repository.Setup(r => r.Update(It.IsAny<Order>()))
            .ReturnsAsync((Order order) => order);

        return repository;
    }

    private static Customer CreateCustomer() => new("Customer", "customer@test.com", "123", "user-id")
    {
        CustomerId = Guid.NewGuid()
    };

    private static OrderModel.OrderRequest CreateRequest(Guid productId, int quantity) =>
        new(null, "user-id", [new OrderItemModel.Request(productId, quantity)], "Street 123", "Street 123", null);
}
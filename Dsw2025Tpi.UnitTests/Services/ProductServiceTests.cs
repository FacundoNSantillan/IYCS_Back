using System.Linq.Expressions;
using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Application.Exceptions;
using Dsw2025Tpi.Application.Services;
using Dsw2025Tpi.Domain.Entities;
using Dsw2025Tpi.Domain.Interfaces;
using Moq;
 
namespace Dsw2025Tpi.UnitTests.Services;
 
public class ProductServiceTests
{
    [Fact]
    public async Task CreateProduct_WithValidData_ReturnsProductAndCallsRepositoryAdd()
    {
        var repository = CreateRepositoryWithAvailableProductData();
        repository.Setup(r => r.Add(It.IsAny<Product>()))
            .ReturnsAsync((Product product) => product);
        var service = new ProductService(repository.Object);
        var request = CreateRequest();
 
        var response = await service.CreateProduct(request);
 
        Assert.Equal(request.Sku, response.Sku);
        Assert.Equal(request.Name, response.Name);
        Assert.Equal(request.CurrentUnitPrice, response.Price);
        Assert.Equal(request.StockQuantity, response.StockQuantity);
        repository.Verify(r => r.Add(It.Is<Product>(p =>
            p.Sku == request.Sku &&
            p.Name == request.Name &&
            p.CurrentUnitPrice == request.CurrentUnitPrice &&
            p.StockQuantity == request.StockQuantity)), Times.Once);
    }
 
    [Fact]
    public async Task CreateProduct_WithDuplicateSku_ThrowsBadRequestAndDoesNotAdd()
    {
        var repository = new Mock<IRepository>();
        repository.Setup(r => r.First<Product>(
                It.IsAny<Expression<Func<Product, bool>>>(),
                It.IsAny<string[]>()))
            .ReturnsAsync(new Product("SKU-001", "CODE-001", "Existing", "Existing product", 100, 10));
        var service = new ProductService(repository.Object);
 
        var exception = await Assert.ThrowsAsync<BadRequestException>(() => service.CreateProduct(CreateRequest()));
 
        Assert.Contains("sku", exception.Message, StringComparison.OrdinalIgnoreCase);
        repository.Verify(r => r.Add(It.IsAny<Product>()), Times.Never);
    }
 
    [Theory]
    [InlineData(0, 10)]
    [InlineData(100, 0)]
    public async Task CreateProduct_WithInvalidPriceOrStock_ThrowsBeforeUsingRepository(decimal price, decimal stock)
    {
        var repository = new Mock<IRepository>(MockBehavior.Strict);
        var service = new ProductService(repository.Object);
        var request = CreateRequest(price: price, stock: stock);
 
        await Assert.ThrowsAsync<BadRequestException>(() => service.CreateProduct(request));
 
        repository.VerifyNoOtherCalls();
    }
 
    private static Mock<IRepository> CreateRepositoryWithAvailableProductData()
    {
        var repository = new Mock<IRepository>();
        repository.Setup(r => r.First<Product>(
                It.IsAny<Expression<Func<Product, bool>>>(),
                It.IsAny<string[]>()))
            .ReturnsAsync((Product?)null);
        return repository;
    }
 
    private static ProductModel.ProductRequest CreateRequest(decimal price = 100, decimal stock = 10) =>
        new("SKU-001", "CODE-001", "Mouse Gamer", "Product for tests", price, stock, true);
}

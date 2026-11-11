using Dsw2025Tpi.Application.Services;
using Dsw2025Tpi.Domain.Entities;
using Dsw2025Tpi.Domain.Interfaces;
using Moq;
 
namespace Dsw2025Tpi.UnitTests.Services;
 
public class ProductServiceQueryTests
{
    [Fact]
    public async Task GetAllProducts_WhenActiveFilterIsTrue_ReturnsOnlyActiveProducts()
    {
        var active = CreateProduct("SKU-A", "CODE-A", "Active product", true);
        var inactive = CreateProduct("SKU-I", "CODE-I", "Inactive product", false);
        var service = CreateService([active, inactive]);
 
        var response = await service.GetAllProducts(null, true, null);
 
        Assert.NotNull(response);
        Assert.Single(response.Items);
        Assert.Equal(active.Id, response.Items[0].Id);
    }
 
    [Fact]
    public async Task GetAllProducts_WithPaginationFilter_AppliesSkipAndTakeCorrectly()
    {
        var products = Enumerable.Range(1, 15)
            .Select(index => CreateProduct($"SKU-{index:D2}", $"CODE-{index:D2}", $"Product {index:D2}", true))
            .ToList();
        var service = CreateService(products);
 
        var response = await service.GetAllProducts(null, null, null, pageNumber: 2, pageSize: 5);
 
        Assert.NotNull(response);
        Assert.Equal(15, response.TotalCount);
        Assert.Equal(5, response.Items.Count);
        Assert.Equal("Product 06", response.Items[0].Name);
    }
 
    [Theory]
    [InlineData("mouse", "Mouse Gamer")]
    [InlineData("SKU-TECLADO", "Teclado Mecánico")]
    [InlineData("CODE-MONITOR", "Monitor")]
    public async Task GetAllProducts_WithSearchTerm_FiltersByNameSkuOrInternalCode(string search, string expectedName)
    {
        var products = new List<Product>
        {
            CreateProduct("SKU-MOUSE", "CODE-MOUSE", "Mouse Gamer", true),
            CreateProduct("SKU-TECLADO", "CODE-TECLADO", "Teclado Mecánico", true),
            CreateProduct("SKU-MONITOR", "CODE-MONITOR", "Monitor", true)
        };
        var service = CreateService(products);
 
        var response = await service.GetAllProducts(search, null, null);
 
        Assert.NotNull(response);
        var product = Assert.Single(response.Items);
        Assert.Equal(expectedName, product.Name);
    }
 
    private static ProductService CreateService(IEnumerable<Product> products)
    {
        var repository = new Mock<IRepository>();
        repository.Setup(r => r.GetAll<Product>(It.IsAny<string[]>()))
            .ReturnsAsync(products);
        return new ProductService(repository.Object);
    }
 
    private static Product CreateProduct(string sku, string internalCode, string name, bool isActive)
    {
        var product = new Product(sku, internalCode, name, "Product for tests", 100, 10)
        {
            IsActive = isActive
        };
        return product;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dsw2025Tpi.Domain.Entities
{
    public class Product : EntityBase
    {
        public Product()
        {

        }

        public Product(string sku, string internalCode, string name, string description, decimal price, decimal stockQuantity)
        {
            Sku = sku;
            InternalCode = internalCode;
            Name = name;
            Description = description;
            CurrentUnitPrice = price;
            StockQuantity = (int)stockQuantity;
            IsActive = true;
            Id = Guid.NewGuid();
        }

        public string? Sku { get; set; }
        public string? InternalCode { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public decimal CurrentUnitPrice { get; set; }
        public int StockQuantity { get; set; }
        public bool IsActive { get; set; }
        public Guid Id { get; private set; }
        public ICollection<OrderItem> OrderItems { get; set; } = new HashSet<OrderItem>();




    }
}

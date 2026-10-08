using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dsw2025Tpi.Domain.Entities
{
    public class OrderItem : EntityBase
    {
        public OrderItem() { }

        public OrderItem(int quantity, decimal unitPrice, Product product)
        {
            Quantity = quantity;
            UnitPrice = product.CurrentUnitPrice;
            ProductId = product.Id;
        }

        public Guid OrderItemId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SubTotal {  get; set; }
        public Guid ProductId { get; set; }
        public Product Product { get; private set; }
        public Order Order { get; set; }
        public Guid OrderId { get; set; }
    }
}
    
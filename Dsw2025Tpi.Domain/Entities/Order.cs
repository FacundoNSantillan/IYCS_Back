using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dsw2025Tpi.Domain.Entities
{
    public class Order : EntityBase
    {
        public Order() { }
        public Order(Guid customerId, DateTime date, string shippingAddress, string billingAddress, string note, OrderStatus status) 
        {
            CustomerId = customerId;
            Date = date;
            ShippingAddress = shippingAddress;
            BillingAddress = billingAddress;
            Note = note;
            Status = status;
            Id = Guid.NewGuid();
        }

        public Guid Id { get; set; }
        public DateTime Date { get; set; }
        public string ShippingAddress { get; set; }
        public string BillingAddress { get; set; }
        public string? Note { get; set; }
        public decimal TotalAmount { get; set; }
        public OrderStatus Status { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; } = new HashSet<OrderItem>();
        public Customer Customer { get; set; }
        public Guid CustomerId { get; set; }

    }
}

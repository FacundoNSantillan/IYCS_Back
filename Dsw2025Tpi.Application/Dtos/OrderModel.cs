using Dsw2025Tpi.Application.Services;
using Dsw2025Tpi.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dsw2025Tpi.Application.Dtos
{
    public record OrderModel
    {
        public record OrderRequest(
            Guid? CustomerId,
            string? UserName,
            List<OrderItemModel.Request> OrderItems,
            string ShippingAddress,
            string BillingAddress,
            string? Notes
        );

        public record OrderResponse(
            Guid Id,
            DateTime Date,
            List<OrderItemModel.Response> OrderItems,
            string ShippingAddress,
            string BillingAddress,
            string Notes,
            decimal TotalAmout,
            string Status,
            string CustomerName
        );
    }
}

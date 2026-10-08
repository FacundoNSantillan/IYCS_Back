using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dsw2025Tpi.Application.Dtos
{
    public record OrderItemModel
    {
        public record Request(
            Guid ProductId,
            int Quantity
        );

        public record Response(
            Guid ProductId,
            string ProductName,
            int Quantity,
            decimal UnitPrice,
            decimal SubTotal
        );
    }
}

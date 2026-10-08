using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dsw2025Tpi.Application.Dtos
{
    public record CustomerModel
    {
        public record Request (
            string Name,
            string Email,
            string PhoneNumber
        );

        public record Response (
            Guid CustomerId,
            string Name,
            string Email, 
            string PhoneNumber
        );
    }
}

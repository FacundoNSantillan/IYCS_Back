using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dsw2025Tpi.Application.Dtos
{
    public record class ProductDisabledModel
    {
        public record ProductDisabledRequest(bool IsActive);

        public record ProductDisabledResponse(Guid Id, bool IsActive);
    }
}

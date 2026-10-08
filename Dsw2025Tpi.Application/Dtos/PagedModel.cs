using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dsw2025Tpi.Application.Dtos
{
    public record PagedModel
    {
        public record PagedResponse<T>(int CurrentPage, int PageSize, int TotalCount, List<T> Items);
    }
}

using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Application.Exceptions;
using Dsw2025Tpi.Domain.Interfaces;
using Dsw2025Tpi.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dsw2025Tpi.Application.Services
{
    public class CustomersService
    {
        private readonly IRepository _repository;
        public CustomersService(IRepository repository)
        {
            _repository = repository;
        }



        public async Task<List<CustomerModel.Response>> GetAllCustomers()
        {
            if (_repository is null)
            {
                throw new NoContentException( "There aren't products in the Data Base.");
            }

            var customers = await _repository.GetAll<Customer>();

            if (customers is null || !customers.Any())
            {
                throw new NoContentException("There aren't customers in the Data Base.");
            }

            var result = customers.Select(c => new CustomerModel.Response(
                c.CustomerId,
                c.Name!,
                c.Email!,
                c.PhoneNumber!
                )).ToList();

            return result;



        }

    }
}

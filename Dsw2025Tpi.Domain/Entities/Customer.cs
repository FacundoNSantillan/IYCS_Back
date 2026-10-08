using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dsw2025Tpi.Domain.Entities
{
    public class Customer : EntityBase
    {
        public Customer(string name, string email, string phoneNumber) 
        {
            Name = name;
            Email = email;
            PhoneNumber = phoneNumber;
           
        }

        public string Name { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        [Key]
        [Column("CustomerId")]
        public Guid CustomerId { get; set; }
        public ICollection <Order> Orders { get; set; }
    }
}

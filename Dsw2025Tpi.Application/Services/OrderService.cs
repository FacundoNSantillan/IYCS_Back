using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Application.Exceptions;
using Dsw2025Tpi.Domain.Entities;
using Dsw2025Tpi.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dsw2025Tpi.Application.Services
{
    public class OrderService
    {
        private readonly IRepository _repository;

        public OrderService(IRepository repository)
        {
            _repository = repository;
        }

        public async Task<OrderModel.OrderResponse> CreateOrderAsync(OrderModel.OrderRequest request)
        {
            Guid finalCustomerId;
            Customer? user = null;

            if (!string.IsNullOrEmpty(request.UserName))
            {
                user = await _repository.First<Customer>(u => u.Name == request.UserName);

                if (user == null)
                    throw new BadRequestException($"El usuario '{request.UserName}' no existe en la base de datos");

                finalCustomerId = user.CustomerId;
            }
            else if (request.CustomerId.HasValue)
            {
                finalCustomerId = request.CustomerId.Value;
                user = await _repository.First<Customer>(u => u.CustomerId == finalCustomerId);
            }
            else
            {
                throw new BadRequestException ("No se pudo identificar al cliente.");
            }
                if (request.OrderItems == null || !request.OrderItems.Any())
                throw new BadRequestException("The order must have at least one item");

            decimal total = 0;
            var orderItems = new List<OrderItem>();

            var productNames = new Dictionary<Guid, string>();

            var order = new Order
            {
                CustomerId = finalCustomerId,
                Date = DateTime.Now,
                ShippingAddress = request.ShippingAddress,
                BillingAddress = request.BillingAddress,
                Note = request.Notes ?? ""
            };

            order.Status = OrderStatus.Pending;

            await _repository.Add(order);

            foreach (var item in request.OrderItems)
            {
                var product = await _repository.First<Product>(p => p.Id == item.ProductId);
                if (product == null)
                    throw new BadRequestException($"Product {item.ProductId} not found");

                if (!product.IsActive)
                    throw new BadRequestException($"El producto '{product.Name}' ya no está disponible.");

                if (product.StockQuantity < item.Quantity)
                    throw new BadRequestException($"Insufficient stock for the product {product.Name}");

                productNames[product.Id] = product.Name;

                decimal subTotal = product.CurrentUnitPrice * item.Quantity;
                total += subTotal;

                orderItems.Add(new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = product.Id,
                    Quantity = item.Quantity,
                    UnitPrice = product.CurrentUnitPrice,
                    SubTotal = subTotal
                });

                product.StockQuantity -= item.Quantity;

                if (product.StockQuantity <= 0)
                {
                    product.StockQuantity = 0;
                    product.IsActive = false; 
                }
                await _repository.Update(product);
            }

            order.TotalAmount = total;
            order.OrderItems = orderItems;
            await _repository.Update(order);

            string customerName = user != null ? user.Name : "Cliente (Nombre no disponible)";

            return new OrderModel.OrderResponse(
                    order.Id,
                    order.Date,
                    order.OrderItems.Select(i => new OrderItemModel.Response(
                          i.ProductId, 
                          productNames.ContainsKey(i.ProductId) ? productNames[i.ProductId] : "Producto", 
                          i.Quantity, 
                          i.UnitPrice, 
                          i.SubTotal
                    )).ToList(),
                    order.ShippingAddress,
                    order.BillingAddress,
                    order.Note,
                    order.TotalAmount,
                    order.Status.ToString(),
                    order.Customer != null ? order.Customer.Name : "Cliente Simulado"
            );
        }

        public async Task<PagedModel.PagedResponse<OrderModel.OrderResponse>?> GetAllOrders(
            string? search,
            string? status = null,
            Guid? customer = null,
            int pageNumber = 1,
            int pageSize = 10)
        {
            if (_repository is null)
            {
                throw new NoContentException("There aren't orders in the Data Base.");
            }

            // Normalizamos el texto de búsqueda
            var normalizedSearch = string.IsNullOrWhiteSpace(search)
                ? null
                : search.Trim().ToLower();

            var allOrders = await _repository.GetFiltered<Order>(o =>
                // 1) Filtro por estado (sigue igual)
                (string.IsNullOrWhiteSpace(status) || o.Status.ToString() == status) &&

                // 2) Filtro por cliente específico (sigue igual)
                (!customer.HasValue || o.CustomerId == customer.Value) &&

                // 3) Filtro de búsqueda general (por todo MENOS estado)
                (
                    normalizedSearch == null ||                     // si no hay search, no filtra
                                                                    // Id de la orden
                    o.Id.ToString().ToLower().Contains(normalizedSearch) ||

                    // Nombre del cliente
                    (o.Customer != null &&
                     o.Customer.Name != null &&
                     o.Customer.Name.ToLower().Contains(normalizedSearch)) ||

                    // Direcciones
                    (!string.IsNullOrEmpty(o.ShippingAddress) &&
                     o.ShippingAddress.ToLower().Contains(normalizedSearch)) ||

                    (!string.IsNullOrEmpty(o.BillingAddress) &&
                     o.BillingAddress.ToLower().Contains(normalizedSearch)) ||

                    // Nota de la orden
                    (!string.IsNullOrEmpty(o.Note) &&
                     o.Note.ToLower().Contains(normalizedSearch)) ||

                    // Productos de la orden: nombre o SKU
                    o.OrderItems.Any(item =>
                        item.Product != null && (
                            (!string.IsNullOrEmpty(item.Product.Name) &&
                             item.Product.Name.ToLower().Contains(normalizedSearch)) ||
                            (!string.IsNullOrEmpty(item.Product.Sku) &&
                             item.Product.Sku.ToLower().Contains(normalizedSearch))
                        ))
                ),
                $"{nameof(Order.OrderItems)}",
                $"{nameof(Order.OrderItems)}.{nameof(OrderItem.Product)}",
                $"{nameof(Order.Customer)}"
            );

            var total = allOrders.Count();

            if (total == 0)
                return null;

            var pagedOrders = allOrders
                .OrderBy(o => o.Date)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new OrderModel.OrderResponse(
                    o.Id,
                    o.Date,
                    o.OrderItems.Select(item => new OrderItemModel.Response(
                        item.ProductId,
                        item.Product.Name,
                        item.Quantity,
                        item.UnitPrice,
                        item.SubTotal = item.Quantity * item.UnitPrice
                    )).ToList(),
                    o.ShippingAddress ?? "Sin dirección.",
                    o.BillingAddress ?? "Sin dirección.",
                    o.Note!,
                    o.OrderItems.Sum(item => item.Quantity * item.UnitPrice),
                    o.Status.ToString(),
                    o.Customer != null ? o.Customer.Name : "Cliente Simulado"
                ))
                .ToList();

            return new PagedModel.PagedResponse<OrderModel.OrderResponse>(
                pageNumber,
                pageSize,
                total,
                pagedOrders
        );
        }

        public async Task<OrderModel.OrderResponse?> GetOrderById(Guid id)
        {
            if(id == Guid.Empty || _repository is null)
            {
                throw new NotFoundException("There isn't a order with the provided ID.");
            }

            var order = await _repository.GetById<Order>(id,nameof(Order.OrderItems),$"{nameof(Order.OrderItems)}.{nameof(OrderItem.Product)}");

            if (order is null)
            {
                throw new NotFoundException("There isn't a order in the Data Base.");
            }

            return new OrderModel.OrderResponse(
                    order.Id,
                    order.Date,
                    order.OrderItems.Select(item => new OrderItemModel.Response(
                        item.ProductId,
                        item.Product.Name,
                        item.Quantity,
                        item.UnitPrice,
                        item.SubTotal = item.Quantity * item.UnitPrice
                    )).ToList(),
                    order.ShippingAddress,
                    order.BillingAddress,
                    order.Note!,
                    order.OrderItems.Sum(item => item.Quantity * item.UnitPrice),
                    order.Status.ToString(),
                    order.Customer != null ? order.Customer.Name : "Cliente Simulado"
            );
        }

        public async Task<UpdateOrderStatusModel.UpdateOrderStatusResponse?> UpdateOrderStatus(Guid id, UpdateOrderStatusModel.UpdateOrderStatusRequest request)
        {
            if (id == Guid.Empty || _repository is null)
            {
                throw new NotFoundException("There isn't a order with the provided ID.");
            }

            if (string.IsNullOrWhiteSpace(request.Status.ToString()))
                throw new BadRequestException("The Status is obligatory");

            var order = await _repository.GetById<Order>(id);

            order.Status = request.Status;

            var orderUpdate = await _repository.Update(order);

            return new UpdateOrderStatusModel.UpdateOrderStatusResponse(
                orderUpdate.Id,
                orderUpdate.Status
                );
        }
    }
}


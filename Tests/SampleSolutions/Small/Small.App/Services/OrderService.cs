using Small.App.Models;

namespace Small.App.Services
{
    public sealed class OrderService
    {
        public Order Create(Customer customer) => new Order { Customer = customer };
    }
}

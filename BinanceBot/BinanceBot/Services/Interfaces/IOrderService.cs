using BinanceBot.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BinanceBot.Services.Interfaces
{
    public interface IOrderService
    {
        Task<List<OrderEntity>> GetActiveOrders();
        Task<Order> GetOrder(OrderEntity orderEntity, string symbol);
        Task<Order> PostOrder(Order order, string symbol);
        void DeleteFilledOrderFrom(OrderEntity orderEntity);
    }
}

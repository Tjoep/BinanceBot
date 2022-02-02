using BinanceBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinanceBot.Services.Interfaces
{
    public interface ITableStorageService
    {
        Task AddOrder(OrderEntity order);
        Task UpdateOrder(OrderEntity order);
        Task DeleteOrder(OrderEntity order);
        Task<List<OrderEntity>> GetOrders();
        Task AddKlines(IEnumerable<KlineEntity> klines);
        Task<List<KlineEntity>> GetKlines();
        Task AddBtcRevenue(BtcRevenueEntity btcRevenue);
        Task<List<BtcRevenueEntity>> GetBtcRevenues();
    }
}

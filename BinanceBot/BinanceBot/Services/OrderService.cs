using BinanceBot.Clients.Interfaces;
using BinanceBot.Models;
using BinanceBot.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BinanceBot.Services
{
    public class OrderService : IOrderService
    {
        private readonly IBinanceClient _binanceClient;
        private readonly ITableStorageService _tableStorageService;
        
        public OrderService(IBinanceClient binanceClient, ITableStorageService tableStorageService)
        {
            _binanceClient = binanceClient;
            _tableStorageService = tableStorageService;
        }

        public async Task<List<OrderEntity>> GetActiveOrders()
        {
            var orderEntities = await _tableStorageService.GetOrders();
            return orderEntities;
        }

        public async Task<Order> GetOrder(OrderEntity orderEntity, string symbol)
        {
            var order = EntityToModel(orderEntity);
            var path = "api/v3/order?";
            var queryString = $"symbol={symbol}";
            queryString += $"&orderId={order.OrderId}";
            queryString += $"&origClientOrderId={order.ClientOrderId}";
            queryString += $"&timestamp={DateTimeOffset.Now.ToUnixTimeMilliseconds()}";

            var response = await _binanceClient.Get<Order>(path, queryString, false, true);
            return response;
        }

        
        public async Task<Order> PostOrder(Order order, string symbol)
        {
            var path = "api/v3/order?";

            var queryString = $"symbol={symbol}";
            queryString += $"&side={order.Side}";
            queryString += $"&newClientOrderId={order.ClientOrderId}";
            queryString += $"&type={order.Type}";
            queryString += $"&quantity={order.Quantity}";
            queryString += $"&price={order.Price}";
            queryString += $"&timeInForce={order.TimeInForce}";
            queryString += $"&timestamp={DateTimeOffset.Now.ToUnixTimeMilliseconds()}";

            var response = await _binanceClient.Post<Order>(path, queryString, false, true);

            if (response != null)
            {
                await AddNewOrder(response, order.PercentageDiff);
            }
            return response;
        }

        private async Task AddNewOrder(Order order, decimal delta)
        {
            var orderEntity = ModelToEntity(order, delta);
            await _tableStorageService.AddOrder(orderEntity);
        }

        public async void DeleteFilledOrderFrom(OrderEntity orderEntity)
        {
            await _tableStorageService.DeleteOrder(orderEntity);
        }

        public Order EntityToModel(OrderEntity orderEntity)
        {
            return new Order()
            {
                ClientOrderId = orderEntity.PartitionKey,
                OrderId = orderEntity.RowKey,
                Symbol = orderEntity.Symbol,
                PercentageDiff = decimal.Parse(orderEntity.Delta),
                OrigQty = orderEntity.OrigQty,
                Type = orderEntity.Type,
                Price = decimal.Parse(orderEntity.Price),
                Quantity = orderEntity.Quantity,
                Side = orderEntity.Side,
                Status = orderEntity.Status
            };
        }

        public static OrderEntity ModelToEntity(Order order, decimal delta)
        {
            return new OrderEntity()
            {
                PartitionKey = order.ClientOrderId,
                RowKey = order.OrderId,
                Symbol = order.Symbol,
                Delta = delta.ToString(),
                OrigQty = order.OrigQty,
                Type = order.Type,
                Price = order.Price.ToString(),
                Quantity = order.Quantity,
                Side = order.Side,
                Status = order.Status
            };
        }
    }
}

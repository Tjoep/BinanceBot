using BinanceBot.Models;
using BinanceBot.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BinanceBot.Services
{
    public class TableStorageService : ITableStorageService
    {
        private readonly string _connString;
        private readonly CloudTableClient cloudTableClient;
        
        public TableStorageService(IConfiguration config)
        {
            _connString = config["Storage:ConnectionString"];
            
            if (string.IsNullOrEmpty(_connString)) 
            {
                throw new ArgumentException("Storage connection string not found!");
            }

            var account = CloudStorageAccount.Parse(_connString);
            cloudTableClient = account.CreateCloudTableClient();

        }

        public async Task AddOrder(OrderEntity order)
        {
            var table = cloudTableClient.GetTableReference("Orders");
            await table.CreateIfNotExistsAsync();

            var operation = TableOperation.Insert(order);
            await table.ExecuteAsync(operation);
        }

        public async Task UpdateOrder(OrderEntity order)
        {
            var table = cloudTableClient.GetTableReference("Orders");
            await table.CreateIfNotExistsAsync();

            var operation = TableOperation.InsertOrReplace(order);
            await table.ExecuteAsync(operation);
        }

        public async Task DeleteOrder(OrderEntity order)
        {
            var table = cloudTableClient.GetTableReference("Orders");
            await table.CreateIfNotExistsAsync();

            var operation = TableOperation.Delete(order);
            await table.ExecuteAsync(operation);
        }

        public async Task<List<OrderEntity>> GetOrders()
        {
            var table = cloudTableClient.GetTableReference("Orders");
            await table.CreateIfNotExistsAsync();

            TableContinuationToken token = null;
            var orders = new List<OrderEntity>();
            do
            {
                var queryResult = await table.ExecuteQuerySegmentedAsync(new TableQuery<OrderEntity>(), token);
                orders.AddRange(queryResult.Results);
                token = queryResult.ContinuationToken;
            } while (token != null);

            return orders;
        }

        public async Task AddKlines(IEnumerable<KlineEntity> klines)
        {
            var table = cloudTableClient.GetTableReference("Klines");
            await table.CreateIfNotExistsAsync();

            var batch = new TableBatchOperation();

            foreach (var kline in klines)
            {
                batch.InsertOrReplace(kline);
            }
            await table.ExecuteBatchAsync(batch);
        }

        public async Task<List<KlineEntity>> GetKlines()
        {
            var table = cloudTableClient.GetTableReference("Klines");
            await table.CreateIfNotExistsAsync();

            TableContinuationToken token = null;
            var orders = new List<KlineEntity>();
            do
            {
                var queryResult = await table.ExecuteQuerySegmentedAsync(new TableQuery<KlineEntity>(), token);
                orders.AddRange(queryResult.Results);
                token = queryResult.ContinuationToken;
            } while (token != null);

            return orders;
        }

        public async Task AddBtcRevenue(BtcRevenueEntity btcRevenue)
        {
            var table = cloudTableClient.GetTableReference("Revenues");
            await table.CreateIfNotExistsAsync();

            var operation = TableOperation.InsertOrReplace(btcRevenue);
            await table.ExecuteAsync(operation);
        }

        public async Task<List<BtcRevenueEntity>> GetBtcRevenues()
        {
            var table = cloudTableClient.GetTableReference("Revenues");
            await table.CreateIfNotExistsAsync();

            TableContinuationToken token = null;
            var btcRevenues = new List<BtcRevenueEntity>();
            do
            {
                var queryResult = await table.ExecuteQuerySegmentedAsync(new TableQuery<BtcRevenueEntity>(), token);
                btcRevenues.AddRange(queryResult.Results);
                token = queryResult.ContinuationToken;
            } while (token != null);

            return btcRevenues;
        }
    }
}

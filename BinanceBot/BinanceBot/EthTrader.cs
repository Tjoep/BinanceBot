using BinanceBot.Models;
using BinanceBot.Services.Interfaces;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace BinanceBot
{
    /*
    
    Strategy:

    Creates BUY order for each x ∈ PERCENTAGE_DIFFS. 
    BUY_ORDER: 
    { 
        Price = CURRENT_ETH_PRICE * (1 - x), 
        Amount (in ETH) = (START_VALUE / |PERCENTAGE_DIFFS|) / CURRENT_ETH_PRICE 
    }
    
    If: BUY_ORDER fills: Create a sell order   
    Create SELL_ORDER 
    { 
        Price = BUY_ORDER.Price * (1 + BUY_ORDER.PercentageDiff), 
        Amount (in ETH) = BUY_ORDER.Amount 
    }

    If: SELL_ORDER fills: Create a buy order
    Create BUY_ORDER  
    { 
        Price = SELL_ORDER.Price * (1 - SELL_ORDER.PercentageDiff), 
        Amount (in ETH) = SELL_ORDER.Amount
    }
    */

    public class EthTrader
    {
        // Constants
        public const string SYMBOL = "ETHBUSD";
        public const string BUY_ASSET = "ETH";
        public const string SELL_ASSET = "BUSD";
        
        // Settings
        public const decimal START_VALUE = 200; // Total amount (in BUSD) to trade
        public static readonly decimal[] PERCENTAGE_DIFFS = { 0.5M, 1.0M, 1.5M, 2.5M, 5M, 7.5M, 10M };
        
        // TODO (maybe)
        // public const decimal COMPOUND_PERCENTAGE = 80M; // Percentage of the profits to re-invest
    
        // Services
        private readonly IAccountService _accountService;
        private readonly IMarketService _marketService;
        private readonly IOrderService _orderService;        
        
        public EthTrader(IAccountService accountService, IMarketService marketService, IOrderService orderService)
        {
            _accountService = accountService;
            _marketService = marketService;
            _orderService = orderService;
        }

        [FunctionName("EthTrader")]
        public async Task Run([TimerTrigger("*/20 * * * * *")] TimerInfo myTimer, ILogger log)
        {
            // Get Active orders from TableStorage
            var activeOrders = await _orderService.GetActiveOrders();

            // If there aren't any active orders, create new orders
            if (!activeOrders.Any())
            {
                await CreateNewOrders(log);
                return;
            }

            // Check active orders
            await CheckActiveOrders(activeOrders, log);
        }

        private async Task CreateNewOrders(ILogger log)
        {
            // Validate current balance
            var binanceWalletBalance = await _accountService.GetFreeBalance(SELL_ASSET);
            var tradableBalance = Math.Min(binanceWalletBalance, START_VALUE);

            if (tradableBalance < 15 * PERCENTAGE_DIFFS.Length)
            {
                log.LogInformation($"BUSD Balance too low, make sure you have atleast {15 * PERCENTAGE_DIFFS.Length} BUSD.");
                return;
            }

            // Determine amount of orders
            var currentPrice = await _marketService.GetLatestPrice(SYMBOL);
            var orderAmount = (tradableBalance * 0.95M) / PERCENTAGE_DIFFS.Length;

            foreach (var percentageDiff in PERCENTAGE_DIFFS)
            {
                var price = CalculateBuyPrice(currentPrice, percentageDiff.ToString());
                var quantity = decimal.Round(orderAmount / price, 4);

                var order = new Order()
                {
                    ClientOrderId = Guid.NewGuid().ToString(),
                    Price = price,
                    Quantity = quantity,
                    Side = "BUY",
                    Type = "LIMIT",
                    PercentageDiff = percentageDiff
                };
                var orderResponse = await _orderService.PostOrder(order, SYMBOL);
                if (orderResponse != null)
                {
                    log.LogInformation($"Created BUY order for {orderResponse.Price}");
                }
            }
        }

        private async Task CheckActiveOrders(List<OrderEntity> activeOrders, ILogger log)
        {
            var currentPrice = await _marketService.GetLatestPrice(SYMBOL);
            var ordersToCheck = activeOrders;

            log.LogInformation($"Current price of ETH: {currentPrice}");

            // Check all orders once in a while
            var random = new Random().Next(1000);
            if (random != 10)
            {
                ordersToCheck = ordersToCheck.Where(x => decimal.Parse(x.Price) >= currentPrice && x.Side == "BUY" || decimal.Parse(x.Price) <= currentPrice && x.Side == "SELL").ToList();
            }

            log.LogInformation($"Orders to check: {ordersToCheck.Count}");

            if (!ordersToCheck.Any())
            {
                log.LogInformation($"Trading round done, no orders to check");
                return;
            }

            // Check buy orders
            foreach (var localBuyOrder in ordersToCheck.Where(x => x.Side == "BUY"))
            {
                var binanceOrderResponse = await _orderService.GetOrder(localBuyOrder, SYMBOL);
                if (binanceOrderResponse == null) continue;

                if (binanceOrderResponse.Status == "FILLED")
                {
                    var sellPrice = CalculateSellPrice(binanceOrderResponse.Price, localBuyOrder.Delta);

                    var oppositeOrder = new Order()
                    {
                        ClientOrderId = Guid.NewGuid().ToString(),
                        Price = sellPrice,
                        Side = "SELL",
                        Quantity = binanceOrderResponse.OrigQty,
                        Type = "LIMIT",
                        PercentageDiff = decimal.Parse(localBuyOrder.Delta)
                    };

                    // Make sure SELL price is above current price
                    currentPrice = await _marketService.GetLatestPrice(SYMBOL);
                    while (currentPrice > sellPrice)
                    {
                        sellPrice += 3;
                    }

                    var postedOrder = await _orderService.PostOrder(oppositeOrder, SYMBOL);
                    if (postedOrder != null) // Created opposite order, delete original from local order database
                    {
                        _orderService.DeleteFilledOrderFrom(localBuyOrder);
                        Console.WriteLine($"Created SELL order for ${sellPrice}");
                        activeOrders = await _orderService.GetActiveOrders();
                    }
                }
            }
            
            foreach (var localSellOrder in ordersToCheck.Where(x => x.Side == "SELL"))
            {
                var binanceOrderResponse = await _orderService.GetOrder(localSellOrder, SYMBOL);
                if (binanceOrderResponse == null) continue;

                if (binanceOrderResponse.Status == "FILLED")
                {
                    var buyPrice = CalculateBuyPrice(binanceOrderResponse.Price, localSellOrder.Delta);

                    var oppositeOrder = new Order()
                    {
                        ClientOrderId = Guid.NewGuid().ToString(),
                        Price = buyPrice,
                        Side = "BUY",
                        Quantity = binanceOrderResponse.OrigQty,
                        Type = "LIMIT",
                        PercentageDiff = decimal.Parse(localSellOrder.Delta)
                    };

                    // Make sure buyPrice is lower than current price
                    currentPrice = await _marketService.GetLatestPrice(SYMBOL);
                    while (currentPrice < buyPrice)
                    {
                        buyPrice -= 3;
                    }

                    var postedOrder = await _orderService.PostOrder(oppositeOrder, SYMBOL);
                    if (postedOrder != null) // Created opposite order, delete original from local order database
                    {
                        _orderService.DeleteFilledOrderFrom(localSellOrder);
                        Console.WriteLine($"Created BUY order for ${buyPrice}");
                        activeOrders = await _orderService.GetActiveOrders();
                    }
                }
            }
            log.LogInformation($"Trading round done");

        }

        private static decimal CalculateBuyPrice(decimal sellPrice, string deltaStr)
        {
            var delta = decimal.Parse(deltaStr);
            var multiplier = decimal.Divide(100 - delta, 100);
            var price = sellPrice * multiplier;
            var roundedPrice = decimal.Round(price, 2);
            return roundedPrice;
        }

        private static decimal CalculateSellPrice(decimal buyPrice, string deltaStr)
        {
            var delta = decimal.Parse(deltaStr);
            var multiplier = decimal.Divide(delta + 100, 100);
            var price = buyPrice * multiplier;
            var roundedPrice = decimal.Round(price, 2);
            return roundedPrice;
        }
    }
}

using BinanceBot.Models;
using BinanceBot.Services.Interfaces;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;

namespace BinanceBot
{
    public class BtcTrader
    {
        // Constants
        public const string SYMBOL = "BTCBUSD";
        public const string BUY_ASSET = "BTC";
        public const string SELL_ASSET = "BUSD";

        // Services
        private readonly IMarketService _marketService;
        private readonly IAccountService _accountService;
        private readonly IOrderService _orderService;
        private readonly IEmailService _emailService;

        // Timer: https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-timer?tabs=csharp#ncrontab-expressions
        private const string Timer = "0 0 22 * * * "; // Once a day at 10 PM

        public BtcTrader(IMarketService marketService, IAccountService accountService, IOrderService orderService, IEmailService emailService)
        {
            _marketService = marketService;
            _accountService = accountService; 
            _orderService = orderService;
            _emailService = emailService;
        }


        [FunctionName("BtcTrader")]
        public async Task Run([TimerTrigger(Timer, RunOnStartup = true)]TimerInfo myTimer, ILogger log)
        {
            var (fearGreedIndexValue, fearGreedIndexClass) = await _marketService.GetFearGreedIndex();
            log.LogInformation($"Fear & Greed index = {fearGreedIndexValue} ({fearGreedIndexClass})");

            var MA_730 = await _marketService.GetMovingAverage(BUY_ASSET, 730);
            log.LogInformation($"730 day Moving Average: {MA_730}");

            var puellMultiple = await _marketService.GetPuellMultiple();
            log.LogInformation($"PuellMultiple index yesterday: {puellMultiple}");

            var currentBtcPrice = await _marketService.GetLatestPrice(SYMBOL);
            log.LogInformation($"Current BTC price: {currentBtcPrice}");

            var buy = fearGreedIndexValue < 20 && double.Parse(currentBtcPrice.ToString()) < MA_730 && puellMultiple < 0.5;
            var sell = fearGreedIndexValue > 75 && double.Parse(currentBtcPrice.ToString()) > MA_730 * 5 && puellMultiple > 4;

            if (buy)
            {
                log.LogInformation($"BUY Criteria met: Buying BTC");
                await ExecuteBuy();
            }
            else if (sell)
            {
                log.LogInformation($"SELL Criteria met: Selling BTC");
                await ExecuteSell();
            } else
            {
                log.LogInformation($"Criteria NOT met, will check again tomorrow");
            }
            
            // Checks spot wallet balance of BUY & SELL asset, puts them into a savings account if balance > minimumPurchaseAmount
            await _accountService.GetFlexibleSavingProducts(BUY_ASSET);
            await _accountService.GetFlexibleSavingProducts(SELL_ASSET);

        }

        // Redeems all BUSD from savings and uses 99.5% of BUSD balance to buy BTC
        private async Task ExecuteBuy()
        {
            
            await _accountService.RedeemFlexibleSavingsProduct(SELL_ASSET);

            // Give Binance some time to transfer BUSD to Spot wallet
            Thread.Sleep(5000);

            var availableBUSD = await _accountService.GetFreeBalance(SELL_ASSET);
            var currentPrice = await _marketService.GetLatestPrice(SYMBOL);

            var buyPrice = currentPrice * 0.995m;
            var quantity = (availableBUSD * 0.98m) / buyPrice;


            var buyOrder = new Order()
            {
                ClientOrderId = Guid.NewGuid().ToString(),
                Price = buyPrice,
                Side = "BUY",
                Quantity = quantity,
                Type = "LIMIT",
            };

            await _orderService.PostOrder(buyOrder, SYMBOL);

            var emailBody = $"TjoepBot created a buy order of {quantity} BTC at {buyPrice}";           
            _emailService.SendEmail("TJOEPBOT - CREATED BUY ORDER", emailBody);
        }

        // Redeems all BTC from savings and sells all BTC
        private async Task ExecuteSell()
        {
            await _accountService.RedeemFlexibleSavingsProduct(BUY_ASSET);

            // Give Binance some time to transfer BUSD to Spot wallet
            Thread.Sleep(5000);

            var availableBTC = await _accountService.GetFreeBalance(BUY_ASSET);
            var currentPrice = await _marketService.GetLatestPrice(SYMBOL);

            var sellPrice = currentPrice * 1.005m;

            var buyOrder = new Order()
            {
                ClientOrderId = Guid.NewGuid().ToString(),
                Price = sellPrice,
                Side = "SELL",
                Quantity = availableBTC,
                Type = "LIMIT",
            };

            await _orderService.PostOrder(buyOrder, SYMBOL);
            
            var emailBody = $"TjoepBot created a SELL order of {availableBTC} BTC at {sellPrice}";
            _emailService.SendEmail("TJOEPBOT - CREATED SELL ORDER", emailBody);
        }
    }
}

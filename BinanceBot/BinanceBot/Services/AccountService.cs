using BinanceBot.Clients.Interfaces;
using BinanceBot.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinanceBot.Services
{
    public class AccountService : IAccountService
    {
        private readonly IBinanceClient _binanceClient;

        public AccountService(IBinanceClient binanceClient)
        {
            _binanceClient = binanceClient;
        }

        public async Task<decimal> GetFreeBalance(string asset)
        {
            var path = "sapi/v1/capital/config/getall?";

            var queryString = $"timestamp={DateTimeOffset.Now.ToUnixTimeMilliseconds()}";

            var response = await _binanceClient.Get<List<FreeBalanceresponse>>(path, queryString, false, true);

            var balance = response.Where(x => x.Coin == asset).FirstOrDefault();
            return balance.Free;
        }

        // Purchases saving product for Asset if availablel
        public async Task GetFlexibleSavingProducts(string asset)
        {
            var freeBalance = await GetFreeBalance(asset);
            
            var path = "/sapi/v1/lending/daily/product/list?";
            var queryString = $"timestamp={DateTimeOffset.Now.ToUnixTimeMilliseconds()}";
            queryString += $"&status=SUBSCRIBABLE";
            var response = await _binanceClient.Get<List<FlexibleSavingProductResponse>>(path, queryString, false, true);

            var product = response.Where(x => x.asset == asset).FirstOrDefault();
            

            if (freeBalance >= decimal.Parse(product.minPurchaseAmount))
            {
                await PurchaseFlexibleSavingsProduct(product.productId, freeBalance);
            }
        }

        public async Task PurchaseFlexibleSavingsProduct(string productId, decimal amount)
        {
            var path = "/sapi/v1/lending/daily/purchase?";

            var queryString = $"productId={productId}";
            queryString += $"&amount={amount}";
            queryString += $"&timestamp={DateTimeOffset.Now.ToUnixTimeMilliseconds()}";
            
            var response = await _binanceClient.Post<PurchaseResponse>(path, queryString, false, true);
        }

        public async Task<(string, string)> GetFlexibleSavingsPositionAmount(string asset)
        {
            var path = "/sapi/v1/lending/daily/token/position?";
            var queryString = $"timestamp={DateTimeOffset.Now.ToUnixTimeMilliseconds()}";
            queryString += $"&asset={asset}";
            var response = await _binanceClient.Get<List<FlexibleSavingsPosition>>(path, queryString, false, true);

            var position = response.FirstOrDefault();

            if (position == null) return ("0", String.Empty);

            return (position.freeAmount, position.productId);
        }

        public async Task RedeemFlexibleSavingsProduct(string asset)
        {
            (var redeemableAmount, var productId) = await GetFlexibleSavingsPositionAmount(asset);

            if (decimal.Parse(redeemableAmount) > 0)
            {
                var path = "/sapi/v1/lending/daily/redeem?";

                var queryString = $"productId={productId}";
                queryString += $"&amount={redeemableAmount}";
                queryString += $"&type=FAST";
                queryString += $"&timestamp={DateTimeOffset.Now.ToUnixTimeMilliseconds()}";

                var response = await _binanceClient.Post<RedeemResponse>(path, queryString, false, true);
            }
        }


        private class RedeemResponse { }

        private class PurchaseResponse
        {
            public string PurchaseId { get; set; }
        }

        private class FreeBalanceresponse
        {
            public string Coin { get; set; }            
            public decimal Free { get; set; }
        }

        public class FlexibleSavingProductResponse
        {
            public string asset { get; set; }
            public string avgAnnualInterestRate { get; set; }
            public bool canPurchase { get; set; }
            public bool canRedeem { get; set; }
            public string dailyInterestPerThousand { get; set; }
            public bool featured { get; set; }
            public string minPurchaseAmount { get; set; }
            public string productId { get; set; }
            public string purchasedAmount { get; set; }
            public string status { get; set; }
            public string upLimit { get; set; }
            public string upLimitPerUser { get; set; }
        }

        private class FlexibleSavingsPosition
        {
            public string annualInterestRate { get; set; }
            public string asset { get; set; }
            public string avgAnnualInterestRate { get; set; }
            public bool canRedeem { get; set; }
            public string dailyInterestRate { get; set; }
            public string freeAmount { get; set; }
            public string productId { get; set; }
            public string productName { get; set; }
            public string redeemingAmount { get; set; }
            public string todayPurchasedAmount { get; set; }
            public string totalAmount { get; set; }
            public string totalInterest { get; set; }
        }


    }
}

using AngleSharp.Html.Parser;
using BinanceBot.Clients.Interfaces;
using BinanceBot.Models;
using BinanceBot.Services.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;

namespace BinanceBot.Services
{
    public class MarketService : IMarketService
    {

        private readonly IBinanceClient _binanceClient;
        private readonly ITableStorageService _tableStorageService;
        private readonly HttpClient _httpClient;

        public MarketService(IBinanceClient binanceClient, ITableStorageService tableStorageService)
        {
            _binanceClient = binanceClient;
            _httpClient = new HttpClient();
            _tableStorageService = tableStorageService;
        }

        public async Task<decimal> GetLatestPrice(string symbol)
        {
            var path = "api/v3/ticker/price";
            var queryString = $"?symbol={symbol}";
            var response = await _binanceClient.Get<LatestPriceResponseModel>(path, queryString);
            return response.Price;
        }

        public async Task<(int?, string)> GetFearGreedIndex()
        {
            var url = "https://api.alternative.me/fng/";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode) return (null, "");

            var responseString = await response.Content.ReadAsStringAsync();
            var fearGreedResponse = JsonConvert.DeserializeObject<FearGreedResponseModel>(responseString);
            return (fearGreedResponse.Data.First().Value, fearGreedResponse.Data.First().Value_Classification);
        }

        // Gets Kline Data from Binance Vision and stores it in Azure Table Storage.
        private async Task GetKlineData(string symbol, int daysBack = 200)
        {
            var now = DateTime.UtcNow;

            var monthlyUrl = $"https://data.binance.vision/data/spot/monthly/klines/{symbol}/1d/";
            var dailyUrl = $"https://data.binance.vision/data/spot/daily/klines/{symbol}/1d/";
            var queryDate = DateTime.UtcNow.AddDays(-daysBack);

            var klinesToAdd = new List<KlineEntity>();
            while (queryDate.Month < now.Month || queryDate.Year < now.Year)
            {
                var zipFileName = $"BTCBUSD-1d-{queryDate.Year}-{queryDate.Month:00}.zip";
                var zipUrl = monthlyUrl + zipFileName;
                var response = await _httpClient.GetAsync(zipUrl);

                var zipStream = new MemoryStream();
                await response.Content.CopyToAsync(zipStream);

                using var zip = new ZipArchive(zipStream);
                var csv = zip.Entries.FirstOrDefault();
                using var sr = new StreamReader(csv.Open());
                while (!sr.EndOfStream)
                {
                    var klineData = sr.ReadLine().Split(',');
                    var klineDate = GetDateTimeFromUnixTimestamp(klineData[0]);

                    var kline = new KlineEntity(klineDate)
                    {
                        Open = klineData[1],
                        High = klineData[2],
                        Low = klineData[3],
                        Close = klineData[4],
                        Symbol = symbol
                    };
                    klinesToAdd.Add(kline);
                }

                await _tableStorageService.AddKlines(klinesToAdd);
               
                queryDate = queryDate.AddMonths(1);
                klinesToAdd.Clear();
            }

            var day = 1;
            while (day < now.Day)
            {
                var zipFileName = $"BTCBUSD-1d-{queryDate.Year}-{queryDate.Month:00}-{day:00}.zip";
                var zipUrl = dailyUrl + zipFileName;
                var response = await _httpClient.GetAsync(zipUrl);

                var zipStream = new MemoryStream();
                await response.Content.CopyToAsync(zipStream);

                using var zip = new ZipArchive(zipStream);
                var csv = zip.Entries.FirstOrDefault();
                using var sr = new StreamReader(csv.Open());
                while (!sr.EndOfStream)
                {
                    var klineData = sr.ReadLine().Split(',');
                    var klineDate = GetDateTimeFromUnixTimestamp(klineData[0]);

                    var kline = new KlineEntity(klineDate)
                    {
                        Open = klineData[1],
                        High = klineData[2],
                        Low = klineData[3],
                        Close = klineData[4],
                        Symbol = symbol
                    };
                    klinesToAdd.Add(kline);
                }
                day++;
            }
            await _tableStorageService.AddKlines(klinesToAdd);
        }

        public async Task<double> GetMovingAverage(string symbol, int days = 200)
        {
            var queryDate = DateTime.UtcNow.AddDays(-days);
            var partitionKeyComparator = $"{queryDate.Year}{queryDate.Month:00}";

            var klines = await _tableStorageService.GetKlines();
            klines = klines.Where(x => int.Parse(x.PartitionKey) > int.Parse(partitionKeyComparator) || int.Parse(x.PartitionKey) == int.Parse(partitionKeyComparator) && int.Parse(x.RowKey) >= queryDate.Day).ToList();

            if (klines.Count < days) // Not enough data to calculate MA, so get the data first
            {
                await GetKlineData(symbol, days);
            }

            klines = await _tableStorageService.GetKlines();
            klines = klines.Where(x => int.Parse(x.PartitionKey) > int.Parse(partitionKeyComparator) || int.Parse(x.PartitionKey) == int.Parse(partitionKeyComparator) && int.Parse(x.RowKey) >= queryDate.Day).ToList();

            if (klines.Count != days)
            {
                throw new Exception("Missing Kline Data");
            }

            return klines.Sum(x => double.Parse(x.Close))  / days;
        }

        // Get's BTC revenue data from file (~/Data/dayrevenue.txt) and stores it in Azure Table Storage.
        private async Task LoadBtcRevenues()
        {
            var workingDirectory = Environment.CurrentDirectory;
            var projectDirectory = Directory.GetParent(workingDirectory).Parent.Parent.FullName;

            using var streamReader = new StreamReader($"{projectDirectory}//Data//dayrevenue.txt");
            
            
            while (!streamReader.EndOfStream)
            {
                var line = streamReader.ReadLine();
                var lineSplit = line.Split("\t");
                var dateString = lineSplit[0][0..^1];
                var revenue = lineSplit[1][0..^1];
                var date = DateTime.Parse(dateString);
                await _tableStorageService.AddBtcRevenue(new BtcRevenueEntity(date) { Revenue = revenue});
            }
            streamReader.Close();

        }

        // Scrapes yesterdays BTC revenue from website: "https://ycharts.com/indicators/bitcoin_miners_revenue_per_day"
        private async Task<string> GetBtcRevenueYesterday()
        {
            var url = "https://ycharts.com/indicators/bitcoin_miners_revenue_per_day";
            var response = await _httpClient.GetAsync(url);

            var responseString = await response.Content.ReadAsStringAsync();

            var parser = new HtmlParser();
            var document = parser.ParseDocument(responseString);

            var innerHtml = document.QuerySelector(".key-stat-title").InnerHtml[2..].Trim();
            var revenue = innerHtml.Split(" ")[0][0..^1];
           
            var date = DateTime.UtcNow.AddDays(-1);

            var btcRevenue = new BtcRevenueEntity(date)
            {
                Revenue = revenue
            };
            await _tableStorageService.AddBtcRevenue(btcRevenue);

            return revenue;
        }

        public async Task<double> GetPuellMultiple()
        {
            var btcRevenues = await _tableStorageService.GetBtcRevenues();
            
            if (btcRevenues.Count < 365)
            {
                await LoadBtcRevenues();
            }

            var revenueYesterdayStr = await GetBtcRevenueYesterday();
            var revenueYesterday = double.Parse(revenueYesterdayStr);   

            var queryDate = DateTime.UtcNow.AddDays(-365);
            var partitionKeyComparator = $"{queryDate.Year}{queryDate.Month:00}";
            var revenues365 = await _tableStorageService.GetBtcRevenues();
            revenues365 = revenues365.Where(x => int.Parse(x.PartitionKey) > int.Parse(partitionKeyComparator) || int.Parse(x.PartitionKey) == int.Parse(partitionKeyComparator) && int.Parse(x.RowKey) >= queryDate.Day).ToList();

            if (revenues365.Count != 365)
            {
                throw new Exception("Missing Revenue Data!");
            }

            var avgRevenue365 = revenues365.Sum(x => double.Parse(x.Revenue)) / revenues365.Count;
            return revenueYesterday / avgRevenue365;
        }

        private static DateTime GetDateTimeFromUnixTimestamp(string unixTimeStamp)
        {
            var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return dateTime.AddSeconds(double.Parse(unixTimeStamp) / 1000);
        }

        #region Http Response Models of external API's
        class LatestPriceResponseModel
        {
            public string Symbol { get; set; }
            public decimal Price { get; set; }
        }

        class FearGreedResponseModel
        {
            public string Name { get; set; }
            public List<Data> Data { get; set; }
        }

        class Data
        {
            public int Value { get; set; }
            public string Value_Classification { get; set; }
        }
        #endregion
    }
}

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using BinanceBot.Clients.Interfaces;

namespace BinanceBot.Clients
{
    public class BinanceClient : IBinanceClient
    {
        private readonly string _endPoint = "https://api.binance.com";
        private readonly string _apiKey;
        private readonly string _secretKey;

        public BinanceClient(IConfiguration config)
        {
            _apiKey = config["Binance:ApiKey"];
            _secretKey = config["Binance:SecretKey"];

            if (_apiKey == null || _secretKey == null)
            {
                throw new ArgumentException("Binance keys not found!");
            }
        }

        public async Task<T> Get<T>(string path, string queryString, bool timestamp = false, bool sign = false)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("X-MBX-APIKEY", _apiKey);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var url = _endPoint + '/' + path + queryString;
            if (sign) url += $"&signature={GetSignature(queryString)}";

            var response = await client.GetAsync(url);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                throw new Exception("Request limit reached!");
            }

            // Sometimes Binance can't find a placed order, retry 3 times.
            int retries = 0;
            while (response.StatusCode != System.Net.HttpStatusCode.TooManyRequests && !response.IsSuccessStatusCode && responseBody.Contains("Order does not exist.") && retries < 3)
            {
                System.Threading.Thread.Sleep(100);
                response = await client.GetAsync(url);
                responseBody = await response.Content.ReadAsStringAsync();
                retries++;
            }

            if (!response.IsSuccessStatusCode)
            {
                return default;
            }
            return JsonConvert.DeserializeObject<T>(responseBody);
        }

        public async Task<T> Post<T>(string path, string queryString, bool timestamp = false, bool sign = false)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("X-MBX-APIKEY", _apiKey);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var url = _endPoint + '/' + path + queryString;
            if (sign) url += $"&signature={GetSignature(queryString)}";

            using HttpResponseMessage response = await client.PostAsync(url, null);
            string responseBody = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                throw new Exception("Request limit reached!");
            }
            if (!response.IsSuccessStatusCode) return default;

            return JsonConvert.DeserializeObject<T>(responseBody);            
        }

        private string GetSignature(string queryString)
        {
            byte[] key = Encoding.ASCII.GetBytes(_secretKey);
            HMACSHA256 myhmacsha256 = new HMACSHA256(key);
            byte[] byteArray = Encoding.ASCII.GetBytes(queryString);
            MemoryStream stream = new MemoryStream(byteArray);
            string result = myhmacsha256.ComputeHash(stream).Aggregate("", (s, e) => s + String.Format("{0:x2}", e), s => s);
            return result;
        }
    }
}

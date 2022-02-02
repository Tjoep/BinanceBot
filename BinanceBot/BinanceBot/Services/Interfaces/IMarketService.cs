using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinanceBot.Services.Interfaces
{
    public interface IMarketService
    {
        Task<decimal> GetLatestPrice(string symbol);
        Task<(int?, string)> GetFearGreedIndex();
        Task<double> GetMovingAverage(string symbol, int days = 200);
        Task<double> GetPuellMultiple();

    }
}

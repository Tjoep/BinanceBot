using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinanceBot.Services.Interfaces
{
    public interface IAccountService
    {
        Task<decimal> GetFreeBalance(string asset);

        Task GetFlexibleSavingProducts(string asset);

        Task RedeemFlexibleSavingsProduct(string asset);
    }
}

using BinanceBot.Clients;
using BinanceBot.Clients.Interfaces;
using BinanceBot.Services;
using BinanceBot.Services.Interfaces;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using System.Threading;


[assembly: FunctionsStartup(typeof(BinanceBot.Startup))]

namespace BinanceBot
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var cultureInfo = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
            Thread.CurrentThread.CurrentCulture = cultureInfo;
            Thread.CurrentThread.CurrentUICulture = cultureInfo;


            // Register services
            builder.Services.AddSingleton<IAccountService, AccountService>();
            builder.Services.AddSingleton<IMarketService, MarketService>();
            builder.Services.AddSingleton<IOrderService, OrderService>();
            builder.Services.AddSingleton<ITableStorageService, TableStorageService>();
            builder.Services.AddSingleton<IBinanceClient, BinanceClient>();
            builder.Services.AddSingleton<IEmailService, EmailService>();
        }
    }
}

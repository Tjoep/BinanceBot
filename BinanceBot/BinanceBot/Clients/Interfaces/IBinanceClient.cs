using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinanceBot.Clients.Interfaces
{
    public interface IBinanceClient
    {
        Task<T> Get<T>(string path, string queryString, bool timestamp = false, bool sign = false);
        Task<T> Post<T>(string path, string queryString, bool timestamp = false, bool sign = false);


    }
}

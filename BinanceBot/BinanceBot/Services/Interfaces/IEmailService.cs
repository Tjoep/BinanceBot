using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinanceBot.Services.Interfaces
{
    public interface IEmailService
    {
        void SendEmail(string subject, string body);
    }
}

using BinanceBot.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace BinanceBot.Services
{
    public class EmailService : IEmailService
    {
        private readonly string _toMail;
        private readonly string _fromMail;
        private readonly string _password;


        public EmailService(IConfiguration config)
        {
            _toMail = config["Email:To"];
            _fromMail = config["Email:Mail"];
            _password = config["Email:Password"];
        }

        public void SendEmail(string subject, string body)
        {
            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(_fromMail, _password),
                EnableSsl = true,
            };

            smtpClient.Send(_fromMail, _toMail, subject, body);
        }
    }
}

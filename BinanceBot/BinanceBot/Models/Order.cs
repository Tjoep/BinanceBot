using System;
using System.Collections.Generic;
using System.Text;

namespace BinanceBot.Models
{
    public class Order
    {
        public string ClientOrderId { get; set; }
        public string OrderId { get; set; }
        public string Symbol { get; set; }
        public string Side { get; set; }
        public string Type { get; set; }
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
        public decimal OrigQty { get; set; }
        public string Status { get; set; }
        public decimal PercentageDiff { get; set; }
        public string TimeInForce => "GTC"; // Good Till Canceled
    }
}

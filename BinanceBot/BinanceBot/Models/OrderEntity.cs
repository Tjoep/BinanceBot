using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinanceBot.Models
{
    public class OrderEntity : TableEntity
    {
        public OrderEntity(string ClientOrderId, string OrderId)
        {
            this.PartitionKey = ClientOrderId;
            this.RowKey = OrderId;
        }
        public OrderEntity() { }
        
        public string Symbol { get; set; }
        public string Side { get; set; }
        public string Type { get; set; }
        public string Price { get; set; }
        public decimal Quantity { get; set; }
        public decimal OrigQty { get; set; }
        public string Status { get; set; }
        public string Delta { get; set; }
        public string TimeInForce => "GTC"; // Good Till Canceled
    }
}

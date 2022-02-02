using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinanceBot.Models
{
    public class KlineEntity : TableEntity
    {
        public KlineEntity(DateTime date)
        {
            this.PartitionKey = $"{date.Year}{date.Month:00}";
            this.RowKey = date.Day.ToString();
        }

        public KlineEntity() { }

        public string High { get; set; }
        public string Low { get; set; }
        public string Open { get; set; }
        public string Close { get; set; }   
        public string Symbol { get; set; }
    }
}

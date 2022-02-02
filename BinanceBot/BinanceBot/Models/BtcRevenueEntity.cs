using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinanceBot.Models
{
    public class BtcRevenueEntity : TableEntity
    {
        public BtcRevenueEntity(DateTime date)
        {
            this.PartitionKey = $"{date.Year}{date.Month:00}";
            this.RowKey = date.Day.ToString();
        }

        public BtcRevenueEntity() { }

        public string Revenue { get; set; }
    }
}

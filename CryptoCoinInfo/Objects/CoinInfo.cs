using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoCoinInfo.Objects
{
    public class CoinInfo
    {
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Last { get; set; }
        public decimal Bid { get; set; }
        public decimal Ask { get; set; }
        public decimal Vwap { get; set; }
        public decimal Volume { get; set; }
        public decimal Open { get; set; }
        public decimal Timestamp { get; set; }
        public decimal Difference { get; set; }
    }
}

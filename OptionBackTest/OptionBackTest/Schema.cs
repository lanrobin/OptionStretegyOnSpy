using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OptionBackTest
{
    [Serializable]
    public class OptionDesc
    {
        public string ContractSymbol { get; set; }
        public DateTime LastTradeDate { get; set; }
        public double Strike { get; set; }
        public string Type { get; set; } // P or C
        public double LastPrice { get; set; }
        public double Bid { get; set; }
        public double Ask { get; set; }
        public double Change { get; set; }
        public double PercentageChange { get; set; }
        public int Volume { get; set; }
        public double OpenInerest { get; set; }
        public double ImpliedVolatility { get;set; }
        public string ContractSize { get; set; }
        public string Currency { get; set; }
        public double UnderlyingLastPrice { get; set; }
        public DateTime ExpiredDate { get; set; }
    }

    [Serializable]
    public class Symbol
    {
        public string Name { get; set; }
        public DateTime Date { get; set; }
        public double Price { get; set; }
        public Dictionary<DateTime, List<OptionDesc>> Calls { get;} = new();
        public Dictionary<DateTime, List<OptionDesc>> Puts { get; } = new();
    }

    [Serializable]
    public class WeekVolatility
    {
        public DateTime Date { get; set; }
        public double V { get; set; }
        public double Open { get; set; }
        public double Close { get; set; }
        public double WeeklyHigh { get; set; }
        public double WeeklyLow { get; set; }
        public double Dividend { get; set; }
        public double MaxIncrease { get; set; }
        public double MaxDecrease { get; set; }
    }

    [Serializable]
    public class DailyVolatility
    {
        public DateTime Date { get; set; }
        public double Open { get; set; }
        public double Close { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double Dividend { get; set; }
        public double Split { get; set; }
    }

    [Serializable]
    public class DataCollection
    {
        public ReadOnlyDictionary<DateTime, Symbol> Symbols { get; set; }

        public ReadOnlyDictionary<DateTime, DailyVolatility> DailyVolatilities { get; set; }
        public ReadOnlyDictionary<DateTime, WeekVolatility> WeekVolatilities { get; set; }
    }
}

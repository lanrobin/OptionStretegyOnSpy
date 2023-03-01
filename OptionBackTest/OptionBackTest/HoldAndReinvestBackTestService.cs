using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OptionBackTest
{
    /// <summary>
    /// Test hold the stock and reinvest the dividends.
    /// </summary>
    public class HoldAndReinvestBackTestService : BaseBackTestService
    {
        private ILogger<HoldAndReinvestBackTestService> _logger;
        public HoldAndReinvestBackTestService(ILogger<HoldAndReinvestBackTestService> logger, Settings s) : base(s)
        {
            _logger = logger;
        }
        public override void Calculate(DataCollection dc)
        {
            Dictionary<DateTime, double> result = new Dictionary<DateTime, double>();
            var wv = dc.WeekVolatilities.Where(i => i.Key > DateTime.Parse(_settings.StartDate)).ToList();
            int stockCount = -1;
            double leftMoney = 0;
            if (wv != null && wv.Count > 1) {
                // We buy at weekly high.
                stockCount = (int)(InitialMount / wv[0].Value.WeeklyHigh);
                leftMoney = InitialMount - stockCount * wv[0].Value.WeeklyHigh;
                result.Add(wv[0].Key, stockCount * wv[0].Value.Close + leftMoney);
                _logger.LogInformation($"Start {wv[0].Key} with {stockCount} stocks and left:{leftMoney}");
            }
            for(int i = 1; i < wv.Count; i++)
            {
                var item = wv[i];
                if(item.Value.Dividend > 0)
                {
                    leftMoney += item.Value.Dividend * stockCount * 0.9; // 10% tax
                    int newStockCount = (int)(leftMoney / item.Value.WeeklyHigh);
                    leftMoney -= item.Value.WeeklyHigh * newStockCount;
                    stockCount += newStockCount;
                }
                result.Add(item.Key, stockCount * item.Value.Close + leftMoney);
                _logger.LogInformation($"Hold at {item.Key} with {stockCount} stocks and left:{leftMoney}");
            }

            _logger.LogInformation("Finished.");
            WriteToCVS("HoldAndReinvestBackTestService", result);
        }
    }
}

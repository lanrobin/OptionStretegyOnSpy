using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OptionBackTest
{
    public class SellPutWithoutProtectionBackTest : BaseBackTestService
    {
        // how many δ's protection. usually 1.
        private const double NUM_OF_DELTA = 0.5;

        private const double SELL_PUT_PERCENT = 1.0 + MIU - NUM_OF_DELTA * DELTA;

        private ILogger<SellPutWithoutProtectionBackTest> _logger;
        public SellPutWithoutProtectionBackTest(ILogger<SellPutWithoutProtectionBackTest> logger, Settings s) : base(s)
        {
            _logger = logger;
        }

        public override void Calculate(DataCollection dc)
        {
            Dictionary<DateTime, double> result = new Dictionary<DateTime, double>();

            var wv = dc.WeekVolatilities.Where(i => i.Key > DateTime.Parse("2009-12-31")).ToList();

            int contractCount = 0;
            double leftMoney = InitialMount;
            double sellPutValue = -1;

            Symbol preSym = null;
            double preClose = 0;
            if (wv.Count > 1)
            {
                var item = wv[0];
                contractCount = (int)(InitialMount / MARGIN_RATE / item.Value.Close) / 100;
                result.Add(item.Key, leftMoney);
                preClose = item.Value.Close;
                preSym = dc.Symbols[item.Key];
            }
            else
            {
                _logger.LogError("No weekly Volatility data.");
                return;
            }


            for (int i = 1; i < wv.Count; i++)
            {
                var dateKey = wv[i].Key;
                // if there is no option expired in this weekend, try the next week.
                while (i < wv.Count && !preSym.Puts.ContainsKey(dateKey))
                {
                    _logger.LogWarning($"No {dateKey} option for stock at:{preSym.Date}");
                    result.Add(dateKey, leftMoney);
                    i++;
                    dateKey = wv[i].Key;
                }

                if (i == wv.Count)
                {
                    _logger.LogError("Done in the middle.");
                    break;
                }

                int sellPutStockPrice = (int)Math.Floor(preClose * SELL_PUT_PERCENT);

                var puts = preSym.Puts[dateKey];
                for (int j = puts.Count - 1; j > 0; j--)
                {
                    // Find the option to sell.
                    if (puts[j].Strike <= sellPutStockPrice)
                    {
                        sellPutValue = puts[j].Bid;
                        break;
                    }
                }

                if (sellPutValue < 0)
                {
                    // No put at the price. we use the last one.
                    //ceilingPutValue = floorPutValue = puts.Last().Bid;
                    throw new InvalidDataException("No put at the price.");
                }

                // comes to here, we can calculate the gain and loss.
                var lostPerPut = 0.0;
                var priceCloseThisWeek = wv[i].Value.Close;
                if (priceCloseThisWeek >= sellPutStockPrice)
                {
                    lostPerPut = sellPutValue;
                }
                else
                {
                    lostPerPut = priceCloseThisWeek - sellPutStockPrice + sellPutValue;
                }
                leftMoney += contractCount * lostPerPut * 100;

                contractCount = (int)(leftMoney / MARGIN_RATE / priceCloseThisWeek) / 100;

                if (contractCount < 1)
                {
                    throw new InvalidDataException("Bankrupted.");
                }

                result.Add(dateKey, leftMoney);
                while (i < wv.Count && !dc.Symbols.ContainsKey(dateKey))
                {
                    _logger.LogWarning($"Symol Not found:{dateKey} is Friday, try previous day.");
                    // it is usually because Friday is close of market.
                    // so we need use the previous day's data.
                    var newDateKey = dateKey.AddDays(-1);
                    if (!dc.Symbols.ContainsKey(newDateKey))
                    {
                        i++;
                        if (i < wv.Count)
                        {
                            dateKey = wv[i].Key;
                            result.Add(dateKey, leftMoney);
                            _logger.LogWarning($"Symol Not found: Both {wv[i - 1].Key} and {newDateKey} are missing, skip to next week.");
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Symol Not found:{dateKey} is Friday, get the symbol for previous day.");
                        dateKey = newDateKey;
                        break;
                    }

                }
                if (dc.Symbols.ContainsKey(dateKey))
                {
                    preSym = dc.Symbols[dateKey];
                    preClose = preSym.Price;
                }
                else
                {
                    break;
                }
            }

            _logger.LogInformation("Finished.");
            WriteToCVS($"SellPutWithoutProtectionBackTest{(int)(NUM_OF_DELTA * 1000)}DELTA", result);
        }
    }
}

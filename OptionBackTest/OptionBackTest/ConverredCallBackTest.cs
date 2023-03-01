using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OptionBackTest
{
    public class ConverredCallBackTest : BaseBackTestService
    {
        // how many δ's protection. usually 1.
        private const double NUM_OF_DELTA = 0;

        private double COVERRED_CALL_PERCENT;

        private ILogger<ConverredCallBackTest> _logger;
        public ConverredCallBackTest(ILogger<ConverredCallBackTest> logger, Settings s):base(s)
        {
            _logger = logger;
            COVERRED_CALL_PERCENT = 1.0 + MIU + NUM_OF_DELTA * DELTA;
        }
        public override void Calculate(DataCollection dc)
        {
            Dictionary<DateTime, double> result = new Dictionary<DateTime, double>();

            var wv = dc.WeekVolatilities.Where(i => i.Key > DateTime.Parse(_settings.StartDate)).ToList();

            int contractCount = 0;
            int stockCount = 0;
            double leftMoney = InitialMount;
            double totalAsset = InitialMount;
            double sellCallValue = -1;

            Symbol preSym = null;
            double preClose = 0;
            if (wv.Count > 1)
            {
                var item = wv[0];
                stockCount = (int)(InitialMount / item.Value.Close);
                leftMoney -= stockCount* item.Value.Close;
                contractCount = stockCount / 100;
                result.Add(item.Key, totalAsset);
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
                while (i < wv.Count && !preSym.Calls.ContainsKey(dateKey))
                {
                    _logger.LogWarning($"No {dateKey} call option for stock at:{preSym.Date}");
                    result.Add(dateKey, totalAsset);
                    i++;
                    dateKey = wv[i].Key;
                }

                if (i >= wv.Count)
                {
                    _logger.LogError("Done in the middle.");
                    break;
                }

                int sellCallStockPrice = (int)Math.Ceiling(preClose * COVERRED_CALL_PERCENT);
                var calls = preSym.Calls[dateKey];
                for (int j = 0; j < calls.Count; j++)
                {
                    // Find the option to sell.
                    if (calls[j].Strike > sellCallStockPrice)
                    {
                        sellCallValue = calls[j - 1].Bid;
                        _logger.LogInformation($"SellCallValue: {sellCallValue} strike: {calls[j - 1].Strike}, stock price:{preClose} at {dateKey}.");

                        if(sellCallValue < 0.02)
                        {
                            _logger.LogWarning("Low premium.");
                        }
                        break;
                    }

                }

                if (sellCallValue < 0)
                {
                    // No put at the price. we use the last one.
                    //ceilingPutValue = floorPutValue = puts.Last().Bid;
                    throw new InvalidDataException($"No call option at the price {sellCallStockPrice}.");
                }

                // comes to here, we can calculate the gain and loss.
                var lostPerCall = 0.0;
                var priceCloseThisWeek = wv[i].Value.Close;
                if (priceCloseThisWeek <= sellCallStockPrice)
                {
                    lostPerCall = sellCallValue;
                }
                else
                {
                    lostPerCall = sellCallStockPrice - priceCloseThisWeek + sellCallValue;
                }

                leftMoney += contractCount * lostPerCall * 100;

                if (wv[i].Value.Dividend > 0)
                {
                    leftMoney += stockCount * wv[i].Value.Dividend * 0.9; // 10% tax
                    _logger.LogInformation($"Dividend stock account {stockCount} get {stockCount * wv[i].Value.Dividend * 0.9} at each {wv[i].Value.Dividend}.");
                }

                int newStockCount = (int)(leftMoney / priceCloseThisWeek);
                if (newStockCount > 0)
                {
                    _logger.LogInformation($"Buy more {newStockCount} stocks and total is {stockCount} at {dateKey}.");
                    stockCount += newStockCount;
                    contractCount = stockCount / 100;
                    leftMoney -= (newStockCount * priceCloseThisWeek);
                }

                var stockValue = stockCount * priceCloseThisWeek;

                if ((-leftMoney) > stockValue/2)
                {
                    throw new InvalidDataException("Margin call.");
                }

                totalAsset = leftMoney + stockValue;
                if (contractCount < 1)
                {
                    throw new InvalidDataException("Bankrupted.");
                }

                result.Add(dateKey, totalAsset);
                while (i < wv.Count && !dc.Symbols.ContainsKey(dateKey))
                {
                    _logger.LogWarning($"Symol Not found:{dateKey} is Friday, try previous day.");
                    // it is usually because Friday is close of market.
                    // so we need use the previous day's data.
                    var newDateKey = dateKey.AddDays(-1);
                    if (!dc.Symbols.ContainsKey(newDateKey))
                    {
                        i++;
                        if(i < wv.Count)
                        {
                            dateKey = wv[i].Key;
                            result.Add(dateKey, totalAsset);
                            _logger.LogWarning($"Symol Not found: Both {wv[i - 1].Key} and {newDateKey} are missing, skip to next week.");
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Symol found:{dateKey} is Friday, get the symbol for previous day:{newDateKey}.");
                        dateKey = newDateKey;
                        break;
                    }

                }
                if (dc.Symbols.ContainsKey(dateKey))
                {
                    preSym = dc.Symbols[dateKey];
                    preClose = preSym.Price;
                }else
                {
                    break;
                }
            }

            _logger.LogInformation("Finished.");
            WriteToCVS($"ConverredCallBackTest{(int)(NUM_OF_DELTA * 1000)}DELTA", result);
        }
    }
}

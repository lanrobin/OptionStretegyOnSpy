using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OptionBackTest
{
    public class SellATMPutBackTestService : BaseBackTestService
    {
        private ILogger<SellATMPutBackTestService> _logger;
        public SellATMPutBackTestService(ILogger<SellATMPutBackTestService> logger, Settings s) : base(s)
        {
            _logger = logger;
        }
        public override void Calculate(DataCollection dc)
        {
            Dictionary<DateTime, double> resultFloor = new Dictionary<DateTime, double>();
            Dictionary<DateTime, double> resultCeiling = new Dictionary<DateTime, double>();
            var wv = dc.WeekVolatilities.Where(i => i.Key > DateTime.Parse("2009-12-31")).ToList();
            int floorContractCount = 0;
            int ceilingContractCount = 0;
            double floorLeftMoney = InitialMount;
            double ceilingLeftMoney = InitialMount;
            double preClose = 0;
            double floorPutValue = -1;
            double ceilingPutValue = -1;
            double floorStrikePrice = 0;
            double ceilingStrikePrice = 0;
            Symbol preSym = null;
            if (wv.Count > 1)
            {
                var item = wv[0];
                ceilingContractCount = floorContractCount = (int)(InitialMount/ MARGIN_RATE / item.Value.Close) / 100;
                resultFloor.Add(item.Key, floorLeftMoney);
                resultCeiling.Add(item.Key, ceilingLeftMoney);
                preClose = item.Value.Close;
                preSym = dc.Symbols[item.Key];
            }else
            {
                _logger.LogError("No weekly Volatility data.");
                return;
            }

            
            for(int i = 1; i < wv.Count; i++)
            {
                var dateKey = wv[i].Key;
                // if there is no option expired in this weekend, try the next week.
                while (i < wv.Count && !preSym.Puts.ContainsKey(dateKey))
                {
                    _logger.LogWarning($"No {dateKey} option for stock at:{preSym.Date}");
                    resultFloor.Add(dateKey, floorLeftMoney);
                    resultCeiling.Add(dateKey, ceilingLeftMoney);
                    i++;
                    dateKey = wv[i].Key;
                }

                if(i == wv.Count)
                {
                    _logger.LogError("Done in the middle.");
                    break;
                }

                var puts = preSym.Puts[dateKey];
                for(int j = 0; j < puts.Count; j++)
                {
                    if (puts[j].Strike > preClose)
                    {
                        ceilingPutValue = puts[j].Bid;
                        ceilingStrikePrice  = puts[j].Strike;
                        if(j > 0)
                        {
                            floorPutValue = puts[j - 1].Bid;
                            floorStrikePrice = puts[j - 1].Strike;
                        }
                        else
                        {
                            floorPutValue = ceilingPutValue;
                            floorStrikePrice = ceilingStrikePrice;
                        }
                        break;
                    }
                }

                if(floorPutValue < 0)
                {
                    // No put at the price. we use the last one.
                    //ceilingPutValue = floorPutValue = puts.Last().Bid;
                    throw new InvalidDataException("No put at the price.");
                }

                // comes to here, we can calculate the gain and loss.
                var floorLostPerPut = wv[i].Value.Close >= floorStrikePrice ? floorPutValue : wv[i].Value.Close + floorPutValue - floorStrikePrice;
                floorLeftMoney += floorContractCount * floorLostPerPut * 100;
                var ceilingLostPerPut = wv[i].Value.Close >= ceilingStrikePrice ? ceilingPutValue : wv[i].Value.Close + ceilingPutValue - ceilingStrikePrice;
                ceilingLeftMoney += ceilingContractCount * ceilingLostPerPut * 100;

                floorContractCount = floorContractCount = (int)(floorLeftMoney / MARGIN_RATE / wv[i].Value.Close) / 100;
                ceilingContractCount = (int)(ceilingLeftMoney / MARGIN_RATE / wv[i].Value.Close) / 100;

                resultFloor.Add(dateKey, floorLeftMoney);
                resultCeiling.Add(dateKey, ceilingLeftMoney);
                while(i < wv.Count && !dc.Symbols.ContainsKey(dateKey))
                {
                    _logger.LogWarning($"Symol Not found:{dateKey} is Friday, try previous day.");
                    // it is usually because Friday is close of market.
                    // so we need use the previous day's data.
                    var newDateKey = dateKey.AddDays(-1);
                    if(!dc.Symbols.ContainsKey(newDateKey))
                    {
                        i++;
                        if (i < wv.Count)
                        {
                            dateKey = wv[i].Key;
                            resultFloor.Add(dateKey, floorLeftMoney);
                            resultCeiling.Add(dateKey, ceilingLeftMoney);
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
            WriteToCVS("SellATMPutBackTestServiceFloor", resultFloor);
            WriteToCVS("SellATMPutBackTestServiceCeiling", resultCeiling);
        }
    }
}

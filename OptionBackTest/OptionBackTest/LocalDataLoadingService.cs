using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace OptionBackTest
{
    /// <summary>
    /// We have two types of schema on option historial data.
    /// Before 2022 and after 2022.
    /// </summary>
    public class LocalDataLoadingService : IDataLoadingService
    {
        private string HistoryDataRootPath { get { return $"{_settings.DataRoot}/SPYOptions"; } }
        private string NewDataRootPath
        {
            get
            {
                return $"{_settings.DataRoot}/SPY-QQQ/{_settings.Symbol}";
            }
        }
        private string StockPriceHistoryRootPath
        {
            get
            {
                return $"{_settings.DataRoot}/history";
            }
        }
        private string WeeklyVolatilityRootPath
        {
            get
            {
                return $"{_settings.DataRoot}/volatility";
            }
        }
        private string DataCollectionRootPath
        {
            get
            {
                return $"{_settings.DataRoot}/json";
            }
        }
        private string SYMBOL { get { return _settings.Symbol; } }

        private Dictionary<DateTime, Symbol> Symbols = new();
        private Dictionary<DateTime, DailyVolatility> DailyVolatilities = new();
        private Dictionary<DateTime, WeekVolatility> WeekVolatilities = new();
        private bool Initialized = false;
        private ILogger<LocalDataLoadingService> _logger;
        private Settings _settings;

        public LocalDataLoadingService(ILogger<LocalDataLoadingService> _logger, Settings s)
        {
            this._logger = _logger;
            this._settings = s;
        }
        public DataCollection LoadData()
        {
            var jsonPath = $"{DataCollectionRootPath}//{SYMBOL}_serialized.dat";
            DataCollection dc = null;
            // Don't enable this block, it is very slow.
#if false
            if (!Initialized)
            {
                BinaryFormatter formatter = new BinaryFormatter();
                if (!File.Exists(jsonPath)) 
                {
                    LoadHistoryData(SYMBOL);
                    LoadNewData(SYMBOL);
                    LoadWeeklyVolatility(SYMBOL);
                    dc = new DataCollection()
                    {
                        Symbols = new ReadOnlyDictionary<DateTime, Symbol>(Symbols),
                        DailyVolatilities = new ReadOnlyDictionary<DateTime, DailyVolatility>(DailyVolatilities),
                        WeekVolatilities = new ReadOnlyDictionary<DateTime, WeekVolatility>(WeekVolatilities)
                    };
                    //File.WriteAllText(jsonPath, JsonConvert.SerializeObject(dc, Formatting.Indented));
                    // This way doesn't make program faster.
                    using (FileStream fs = new FileStream(jsonPath, FileMode.Open))
                    {
                        try
                        {
                            formatter.Serialize(fs, dc);
                        }
                        catch (SerializationException e)
                        {
                            _logger.LogError("Failed to serialize. Reason: " + e.Message);
                            throw;
                        }
                    }
                }
                else
                {
                    using (FileStream fs = new FileStream(jsonPath, FileMode.Open))
                    {
                        try
                        {
                            // Deserialize the hashtable from the file and
                            // assign the reference to the local variable.
                            dc = (DataCollection)formatter.Deserialize(fs);
                        }
                        catch (SerializationException e)
                        {
                            _logger.LogError("Failed to deserialize. Reason: " + e.Message);
                            throw;
                        }
                    }
                }
                Initialized= true;
            }
#else
            if (!Initialized)
            {
                LoadHistoryData(SYMBOL);
                LoadNewData(SYMBOL);
                LoadWeeklyVolatility(SYMBOL);
                dc = new DataCollection()
                {
                    Symbols = new ReadOnlyDictionary<DateTime, Symbol>(Symbols),
                    DailyVolatilities = new ReadOnlyDictionary<DateTime, DailyVolatility>(DailyVolatilities),
                    WeekVolatilities = new ReadOnlyDictionary<DateTime, WeekVolatility>(WeekVolatilities)
                };
                Initialized = true;
            }
#endif
            return dc;
        }

        private void LoadHistoryData(string symbolName)
        {
            Stopwatch sw = Stopwatch.StartNew();
            _logger.LogInformation($"Begin to load historical data.");
            var files = Directory.GetFiles(HistoryDataRootPath, "*.txt", SearchOption.AllDirectories);

#if false
            foreach (var file in files)
#else
            Parallel.ForEach(files, file =>
#endif
            {
                var lines = File.ReadAllLines(file).Skip(1); // first line is header;
                Dictionary<DateTime, Symbol> localSymbol = new();
                DateTime lastSymbolDate = DateTime.MinValue;
                foreach (var line in lines)
                {
                    var parts = line.Split(',');
                    if (parts.Length != 33)
                    {
                        throw new InvalidDataException($"line segments is not 33, it is {parts.Length}");
                    }

                    DateTime symbolDate = DateTime.Parse(parts[2].Trim());
                    Symbol s = null;
                    if (!localSymbol.TryGetValue(symbolDate, out s))
                    {
                        s = new Symbol()
                        {
                            Name = symbolName,
                            Price = double.Parse(parts[4].Trim()),
                            Date = symbolDate,
                        };
                        localSymbol.Add(symbolDate, s);
                    }

                    DateTime expireDate = DateTime.Parse(parts[5].Trim());
                    double strike = double.Parse(parts[19].Trim());
                    int strikeInt = (int)(strike * 1000);
                    OptionDesc call = new OptionDesc()
                    {
                        ContractSymbol = $"{symbolName}{expireDate.ToString("yyMMdd")}C{strikeInt.ToString("D8")}",
                        Type = "C",
                        LastPrice = parts[15].Trim().TryParseDoubleWithDefault(),
                        Bid = parts[17].Trim().TryParseDoubleWithDefault(),
                        Ask = parts[18].Trim().TryParseDoubleWithDefault(),
                        ImpliedVolatility = parts[13].Trim().TryParseDoubleWithDefault(),
                        UnderlyingLastPrice = s.Price,
                        ExpiredDate = expireDate,
                        Strike = strike,
                    };

                    List<OptionDesc> callOptions = null;
                    if (!s.Calls.TryGetValue(expireDate, out callOptions))
                    {
                        callOptions = new List<OptionDesc>();
                        s.Calls.Add(expireDate, callOptions);
                    }
                    callOptions.Add(call);


                    OptionDesc put = new OptionDesc()
                    {
                        ContractSymbol = $"{symbolName}{expireDate.ToString("yyMMdd")}P{strikeInt.ToString("D8")}",
                        Type = "P",
                        LastPrice = parts[23].Trim().TryParseDoubleWithDefault(),
                        Bid = parts[20].Trim().TryParseDoubleWithDefault(),
                        Ask = parts[21].Trim().TryParseDoubleWithDefault(),
                        ImpliedVolatility = parts[29].Trim().TryParseDoubleWithDefault(),
                        UnderlyingLastPrice = s.Price,
                        ExpiredDate = expireDate,
                        Strike = strike,
                    };
                    List<OptionDesc> putOptions = null;
                    if (!s.Puts.TryGetValue(expireDate, out putOptions))
                    {
                        putOptions = new List<OptionDesc>();
                        s.Puts.Add(expireDate, putOptions);
                    }
                    putOptions.Add(put);
                    if (lastSymbolDate != symbolDate)
                    {
                        lock (Symbols)
                        {
                            Symbols.Add(symbolDate, s);
                        }
                        lastSymbolDate = symbolDate;
                    }
                }
            }
            );
            sw.Stop();
            _logger.LogInformation($"Finish to load historical data with time:{sw.Elapsed}");
        }

        private void LoadNewData(string symbolName)
        {
            Stopwatch sw = Stopwatch.StartNew();
            _logger.LogInformation($"Begin to load new data.");
            var dailyPrices = GetDailyClosePrice(symbolName);
            var folders = Directory.GetDirectories(NewDataRootPath, "*", SearchOption.TopDirectoryOnly);
            //foreach (var f in folders)
            Parallel.ForEach(folders, f =>
            {
                var lastPart = f.Split("\\").Last();
                DateTime symbolDate = DateTime.MinValue;
                Symbol s = null;
                if (lastPart != null)
                {
                    symbolDate = DateTime.Parse(lastPart);

                    if (!Symbols.TryGetValue(symbolDate, out s))
                    {
                        s = new Symbol()
                        {
                            Name = symbolName,
                            Price = dailyPrices[symbolDate].Close,
                            Date = symbolDate,
                        };
                        Symbols.Add(symbolDate, s);
                    }
                    else
                    {
                        //_logger.LogWarning($"Stock for:{symbolDate} has been loaded. Skip");
                        // continue;
                        return;
                    }

                }
                else
                {
                    throw new InvalidDataException($"Invalid path:{f}");
                }

                var files = Directory.GetFiles(f, "*.csv", SearchOption.TopDirectoryOnly);

                foreach (var file in files)
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    if (fileName != null)
                    {
                        var parts = fileName.Split('_');
                        DateTime expireDate = DateTime.Parse(parts[0]);
                        string type = "C";
                        double stockPrice = dailyPrices[symbolDate].Close;
                        List<OptionDesc> optionList = null;
                        if (parts[1] == "call")
                        {
                            type = "C";
                            if (!s.Calls.TryGetValue(expireDate, out optionList))
                            {
                                optionList = new List<OptionDesc>();
                                s.Calls.Add(expireDate, optionList);
                            }
                        }
                        else if (parts[1] == "put")
                        {
                            type = "P";
                            if (!s.Puts.TryGetValue(expireDate, out optionList))
                            {
                                optionList = new List<OptionDesc>();
                                s.Puts.Add(expireDate, optionList);
                            }
                        }
                        else
                        {
                            throw new InvalidDataException($"Unknown option type:{parts[2]}");
                        }

                        var lines = File.ReadAllLines(file);
                        foreach (var line in lines)
                        {
                            var ps = line.Split(',');
                            if (ps.Length == 14)
                            {
                                OptionDesc od = new OptionDesc()
                                {
                                    ContractSymbol = ps[0],
                                    Type = type,
                                    LastPrice = ps[3].Trim().TryParseDoubleWithDefault(),
                                    Bid = ps[4].Trim().TryParseDoubleWithDefault(),
                                    Ask = ps[5].Trim().TryParseDoubleWithDefault(),
                                    ImpliedVolatility = ps[10].Trim().TryParseDoubleWithDefault(),
                                    UnderlyingLastPrice = stockPrice,
                                    ExpiredDate = expireDate,
                                    Strike = double.Parse(ps[2].Trim())
                                };
                                optionList.Add(od);
                            }
                            else
                            {
                                throw new InvalidDataException($"Options data is not 14 parts. it is:{ps.Length}");
                            }
                        }
                    }
                }
            });
            sw.Stop();
            _logger.LogInformation($"Finish to load new data with time:{sw.Elapsed}");
        }

        private Dictionary<DateTime, DailyVolatility> GetDailyClosePrice(string symbolName)
        {
            var lines = File.ReadAllLines($"{StockPriceHistoryRootPath}\\{symbolName}_unadjusted.csv").Skip(1);
            foreach (var l in lines)
            {
                var parts = l.Split(',');
                DateTime date = DateTime.Parse(parts[0]);
                var wv = new DailyVolatility()
                {
                    Date = date,
                    Open = parts[1].TryParseDoubleWithDefault(),
                    High = parts[2].TryParseDoubleWithDefault(),
                    Low = parts[3].TryParseDoubleWithDefault(),
                    Close = parts[4].TryParseDoubleWithDefault(),
                    Dividend = parts[6].TryParseDoubleWithDefault(),
                    Split = parts[7].TryParseDoubleWithDefault(),
                };

                DailyVolatilities.Add(date, wv);
            }
            return DailyVolatilities;
        }

        private Dictionary<DateTime, WeekVolatility> LoadWeeklyVolatility(string symbolName)
        {
            var lines = File.ReadAllLines($"{WeeklyVolatilityRootPath}\\{symbolName}weekly.csv").Skip(1);
            foreach (var l in lines)
            {
                var parts = l.Split(',');
                DateTime date = DateTime.Parse(parts[0]);
                var wv = new WeekVolatility()
                {
                    Date = date,
                    V = parts[1].TryParseDoubleWithDefault(),
                    Open = parts[2].TryParseDoubleWithDefault(),
                    Close = parts[3].TryParseDoubleWithDefault(),
                    WeeklyHigh = parts[4].TryParseDoubleWithDefault(),
                    WeeklyLow = parts[5].TryParseDoubleWithDefault(),
                    Dividend = parts[6].TryParseDoubleWithDefault(),
                    MaxDecrease = parts[7].TryParseDoubleWithDefault(),
                    MaxIncrease = parts[8].TryParseDoubleWithDefault(),
                };

                WeekVolatilities.Add(date, wv);
            }
            return WeekVolatilities;
        }
    }
}

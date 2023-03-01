using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OptionBackTest
{
    public abstract class BaseBackTestService : IBackTestService
    {
        public BaseBackTestService(Settings s) {
            _settings = s;
            MIU = _settings.Miu / 100.0;
            DELTA = _settings.Delta / 100.0;
        }

        protected Settings _settings;
        protected const double InitialMount = 25000;
        protected string ResultRoot { get { return $"{_settings.DataRoot}/result"; } }

        // Normal distribution μ=0.13543%，δ=2.27867%
        protected double MIU;  //SPY
        protected double DELTA; //SPY

        //protected const double MIU = 0.1713 / 100;  //QQQ
        //protected const double DELTA = 3.4062 / 100; //QQQ


        protected const double MARGIN_RATE = 0.25; // margin rate. 4 times

        public abstract void Calculate(DataCollection dc);

        protected void WriteToCVS(string fileName, Dictionary<DateTime, double> result)
        {
            File.WriteAllLines($"{ResultRoot}\\{_settings.Symbol}-{fileName}.csv", result.Select(x => $"{x.Key.ToString("yyyy-MM-dd")},{x.Value}").ToArray());
        }
    }
}

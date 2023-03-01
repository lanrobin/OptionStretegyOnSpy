using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OptionBackTest
{
    public class Settings
    {
        private IConfiguration _conf;
        public Settings(IConfiguration config) {
            _conf = config;
        }

        public string DataRoot
        {
            get
            {
                return _conf["Settings:DataRoot"];
            }
        }

        public string Symbol { get
            {
                return _conf["Settings:Symbol"];
            }
        }

        public string StartDate { get
            {
                return _conf["Settings:StartDate"];
            }
        }

        public double Miu { get { return double.Parse(_conf["Settings:Miu"]); } }
        public double Delta { get { return double.Parse(_conf["Settings:Delta"]); } }
    }
}

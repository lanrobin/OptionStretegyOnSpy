using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OptionBackTest
{
    public interface IBackTestService
    {
        public void Calculate(DataCollection dc);
    }
}

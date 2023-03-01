using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OptionBackTest
{
    public class AggregatedBackTestService : IBackTestService
    {
        private IServiceProvider _serviceProvider;
        private IEnumerable<BaseBackTestService> _backTests;
        public AggregatedBackTestService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _backTests = _serviceProvider.GetServices<BaseBackTestService>();
        }

        public void Calculate(DataCollection dc)
        {
            Parallel.ForEach(_backTests, (bt) => { bt.Calculate(dc); });
        }
    }
}

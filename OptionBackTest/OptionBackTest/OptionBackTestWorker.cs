using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OptionBackTest
{
    public class OptionBackTestWorker : IHostedService
    {
        private ILogger<OptionBackTestWorker> _logger;
        private IDataLoadingService _dataLoadingService;
        private IBackTestService backTestService;
        public OptionBackTestWorker(IDataLoadingService dataLoadingService, IBackTestService testService, ILogger<OptionBackTestWorker> _logger) {
            _dataLoadingService= dataLoadingService;
            backTestService = testService;
            this._logger= _logger;
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("OptionBackTestWorker StartAsync entry.");
            var dc = _dataLoadingService.LoadData();
            backTestService.Calculate(dc);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("OptionBackTestWorker StopAsync called.");
            return Task.CompletedTask;
        }

        
    }

    public static class DoubleExtension
    {
        public static double TryParseDoubleWithDefault(this string s, double defaultValue = 0)
        {
            double value = defaultValue;
            if(!double.TryParse(s, out value)) {
                value = defaultValue;
            }
            return value;
        }
    }
}

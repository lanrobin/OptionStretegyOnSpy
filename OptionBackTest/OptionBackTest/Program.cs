namespace OptionBackTest
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IHost host = Host.CreateDefaultBuilder(args)
                .ConfigureServices(services =>
                {
                    services.AddHostedService<OptionBackTestWorker>();
                    services.AddSingleton<IDataLoadingService, LocalDataLoadingService>();
                    services.AddTransient<BaseBackTestService, HoldAndReinvestBackTestService> ();
                    services.AddTransient<BaseBackTestService, SellATMPutBackTestService>();
                    services.AddTransient<BaseBackTestService, SellPutWithoutProtectionBackTest>();
                    services.AddTransient<BaseBackTestService, SellPutWithoutProtectionBackTest>();
                    services.AddTransient<BaseBackTestService, ConverredCallBackTest>();
                    services.AddSingleton<IBackTestService, AggregatedBackTestService>();
                    services.AddSingleton<Settings>();
                })
                .Build();

            host.Run();
        }
    }
}
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
                    services.AddSingleton<IBackTestService, ConverredCallBackTest>();
                    services.AddSingleton<Settings>();
                })
                .Build();

            host.Run();
        }
    }
}
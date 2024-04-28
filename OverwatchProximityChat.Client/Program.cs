
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Exceptions;
using System.Reflection;
using System.Runtime.InteropServices;

namespace OverwatchProximityChat.Client
{
    public class Program
    {
        [DllImport("Kernel32")]
        public static extern void AllocConsole();

        [DllImport("Kernel32", SetLastError = true)]
        public static extern void FreeConsole();

        [STAThread]
        public static void Main()
        {
            AllocConsole();
            Console.WriteLine("Starting..");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File($"Logs/{Assembly.GetExecutingAssembly().GetName().Name}.log")
                .WriteTo.Console()
                .Enrich.WithExceptionDetails()
                .CreateLogger();

            IHost host = Host.CreateDefaultBuilder()
               .ConfigureServices(services =>
               {
                   services.AddSingleton<App>();
                   services.AddSingleton<MainWindow>();
               }).ConfigureLogging(x => x.AddSerilog())
               .Build();

            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                Log.Logger.Error(args.ExceptionObject as Exception, "Unhandled exception occurred");
            };

            App app = host.Services.GetService<App>();
            app.Run();
        }
    }
}

using System.Windows;
using Akka.Actor;
using Serilog;

namespace ActorPipeline
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            var logger = new LoggerConfiguration()
                .WriteTo.Debug()
                .MinimumLevel.Warning()
                .CreateLogger();

            Log.Logger = logger;

            ActorGlobals.ActorSystem = ActorSystem.Create("ActorPipeline","akka { loglevel=WARNING,  loggers=[\"Akka.Logger.Serilog.SerilogLogger, Akka.Logger.Serilog\"]}");
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            ActorGlobals.ActorSystem.Terminate();
        }
    }
}

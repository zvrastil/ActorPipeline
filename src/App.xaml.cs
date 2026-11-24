using System.Windows;
using Akka.Actor;
using Akka.Configuration;
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
                .MinimumLevel.Debug()
                .CreateLogger();

            Log.Logger = logger;

            var config = ConfigurationFactory.ParseString("""
                akka {
                    loglevel = DEBUG
                    loggers = ["Akka.Logger.Serilog.SerilogLogger, Akka.Logger.Serilog"]
                }
                replaceable-message-mailbox {
                    mailbox-type = "ActorPipeline.Mailbox.ReplaceableMessageMailbox, ActorPipeline"
                }
                """);
            ActorGlobals.ActorSystem = ActorSystem.Create("ActorPipeline", config);
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            ActorGlobals.ActorSystem.Terminate();
        }
    }
}

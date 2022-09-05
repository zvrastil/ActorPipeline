using System.Windows;

namespace ActorPipeline
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Exit(object sender, ExitEventArgs e)
        {
            ActorGlobals.ActorSystem.Terminate();
        }
    }
}

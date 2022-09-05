using ActorPipeline.Actors;
using ActorPipeline.Crates;
using ActorPipeline.Images;
using ActorPipeline.Messages;
using ActorPipeline.Messages.Data;
using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ActorPipeline
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private IActorRef pipeline;
        private IActorRef imageSource;
        private IActorRef imageDisplay;
        private IActorRef bitmapUpdater;
        private bool isRunning = false;

        private static readonly IReadOnlyList<ImageFormatCrate> ImageFormats = new List<ImageFormatCrate> 
        { 
            new ImageFormatCrate(Images.ImageFormat.Grayscale8, "GrayScale 8 bit"),
            new ImageFormatCrate(Images.ImageFormat.Grayscale16, "GrayScale 16 bit"),
            new ImageFormatCrate(Images.ImageFormat.Color24, "RGB 24 bit"),
        };

        public MainWindow()
        {
            InitializeComponent();
            Format.ItemsSource = ImageFormats;
            Format.DisplayMemberPath = "Description";
            Format.SelectedValuePath = "Format";
            Format.SelectedIndex = 0;
            Format.SelectionChanged += Format_SelectionChanged;
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            pipeline = ActorGlobals.ActorSystem.ActorOf(Props.Create(() => new PipelineCoordinatorActor()), "pipeline");
            imageSource = await pipeline.Ask<IActorRef>(
                new PipelineControlMessages.CreateFilterMessage(
                    Props.Create(() => new ImageSourceActor()),  
                    FilterPropertiesBuilder.WithName("imageSource").AsSourceFilter().Build()
                    )
                );
            imageDisplay = await pipeline.Ask<IActorRef>(
                new PipelineControlMessages.CreateFilterMessage(
                    Props.Create(() => new ImageDisplayActor()), 
                    FilterPropertiesBuilder.WithName("imageDisplay").Build()
                    )
                );
            bitmapUpdater = ActorGlobals.ActorSystem.ActorOf(
                Props.Create(() => new WpfImageControlUpdaterActor(Image)).WithDispatcher("akka.actor.synchronized-dispatcher"),
                "bitmapUpdater"
                );

            imageDisplay.Tell(new PipelineFilterMessages.ConnectToMessage(imageSource));
            bitmapUpdater.Tell(new PipelineFilterMessages.ConnectToMessage(imageDisplay));

            pipeline.Tell(new PipelineControlMessages.SetImageFormatMessage(768, 512, (ImageFormat)Format.SelectedValue));
            pipeline.Tell(new PipelineControlMessages.SetDwellTimeMessage(500));
        }

        private void StartStop_Click(object sender, RoutedEventArgs e)
        {
            if (!isRunning)
            {
                pipeline.Tell(new PipelineControlMessages.StartMessage());
                isRunning = true;
                StartStop.Content = "Stop";
            }
            else
            {
                pipeline.Tell(new PipelineControlMessages.StopMessage());
                isRunning = false;
                StartStop.Content = "Start";
            }
        }

        private void Format_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            pipeline.Tell(new PipelineControlMessages.SetImageFormatMessage(768, 512, (ImageFormat)Format.SelectedValue));
        }
    }
}

using ActorPipeline.Actors;
using ActorPipeline.Crates;
using ActorPipeline.Images2;
using ActorPipeline.Messages;
using ActorPipeline.Messages.Data;
using Akka.Actor;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace ActorPipeline
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private IActorRef pipeline;
        private IActorRef imageSource;
        private IActorRef imageDownscaler;
        private IActorRef imageDisplay;
        private IActorRef bitmapUpdater;
        private bool isRunning = false;

        private static readonly IReadOnlyList<ImageFormatCrate> ImageFormats = new List<ImageFormatCrate> 
        { 
            new ImageFormatCrate(ImageFormat.Grayscale8, "GrayScale 8 bit"),
            new ImageFormatCrate(ImageFormat.Grayscale16, "GrayScale 16 bit"),
            new ImageFormatCrate(ImageFormat.Color24, "RGB 24 bit"),
        };

        private static readonly IReadOnlyList<ImageResolutionCrate> ImageResolutions = new List<ImageResolutionCrate>
        {
            new ImageResolutionCrate(768, 512),
            new ImageResolutionCrate(1536, 1024),
            new ImageResolutionCrate(3072, 2048),
            new ImageResolutionCrate(6144, 4096),
            new ImageResolutionCrate(512, 442),
            new ImageResolutionCrate(1024, 884),
            new ImageResolutionCrate(2048, 1768),
            new ImageResolutionCrate(4096, 3536),
            new ImageResolutionCrate(8192, 8192),
        };

        private static readonly IReadOnlyList<DwellTimeCrate> DwellTimes = new List<DwellTimeCrate>
        {
            new DwellTimeCrate(50, "50 ns"),
            new DwellTimeCrate(100, "100 ns"),
            new DwellTimeCrate(300, "300 ns"),
            new DwellTimeCrate(500, "500 ns"),
            new DwellTimeCrate(1000, "1 µs"),
            new DwellTimeCrate(5000, "5 µs"),
            new DwellTimeCrate(10000, "10 µs"),
            new DwellTimeCrate(30000, "30 µs"),
            new DwellTimeCrate(100000, "100 µs"),
            new DwellTimeCrate(400000, "400 µs"),
            new DwellTimeCrate(1000000, "1 ms"),
            new DwellTimeCrate(2000000, "2 ms"),
        };

        public MainWindow()
        {
            InitializeComponent();
            SetUpResolutionComboBox();
            SetUpDwellTimeComboBox();
            SetUpFormatComboBox();
            ImageBorder.SizeChanged += Image_SizeChanged;
            Loaded += MainWindow_Loaded;
        }

        private void SetUpResolutionComboBox()
        {
            Resolution.ItemsSource = ImageResolutions;
            Resolution.DisplayMemberPath = nameof(ImageResolutionCrate.Description);
            Resolution.SelectedIndex = 0;
            Resolution.SelectionChanged += ImageFormat_SelectionChanged;
        }

        private void SetUpDwellTimeComboBox()
        {
            DwellTime.ItemsSource = DwellTimes;
            DwellTime.DisplayMemberPath = nameof(DwellTimeCrate.Description);
            DwellTime.SelectedValuePath = nameof(DwellTimeCrate.DwellTimeNanoseconds);
            DwellTime.SelectedIndex = 2;
            DwellTime.SelectionChanged += DwellTime_SelectionChanged;
        }

        private void SetUpFormatComboBox()
        {
            Format.ItemsSource = ImageFormats;
            Format.DisplayMemberPath = nameof(ImageFormatCrate.Description);
            Format.SelectedValuePath = nameof(ImageFormatCrate.Format);
            Format.SelectedIndex = 0;
            Format.SelectionChanged += ImageFormat_SelectionChanged;
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
            imageDownscaler = await pipeline.Ask<IActorRef>(
                new PipelineControlMessages.CreateFilterMessage(
                    Props.Create(() => new ImageDownscalerActor()),
                    FilterPropertiesBuilder.WithName("imageDownscaler").Build()
                    )
                );
            imageDisplay = await pipeline.Ask<IActorRef>(
                new PipelineControlMessages.CreateFilterMessage(
                    Props.Create(() => new ImageDisplayActor(imageDownscaler)), 
                    FilterPropertiesBuilder.WithName("imageDisplay").Build()
                    )
                );
            bitmapUpdater = ActorGlobals.ActorSystem.ActorOf(
                Props.Create(() => new WpfImageControlUpdaterActor(Image)).WithDispatcher("akka.actor.synchronized-dispatcher"),
                "bitmapUpdater"
                );


            imageDownscaler.Tell(new PipelineFilterMessages.ConnectToMessage(imageSource));
            imageDisplay.Tell(new PipelineFilterMessages.ConnectToMessage(imageDownscaler));
            bitmapUpdater.Tell(new PipelineFilterMessages.ConnectToMessage(imageDisplay));

            UpdateImageFormat();
            UpdateDwellTime();
            UpdateDisplaySize();
            UpdateDigitalZoom();
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

        private void ImageFormat_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateImageFormat();
        }

        private void DwellTime_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateDwellTime();
        }

        private void DigitalZoom_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateDigitalZoom();
        }

        private void Image_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateDisplaySize();
        }

        private void UpdateImageFormat()
        {
            var res = (ImageResolutionCrate) Resolution.SelectedItem;
            pipeline?.Tell(
                new PipelineControlMessages.SetImageFormatMessage(res.Width, res.Height, (ImageFormat) Format.SelectedValue));
        }

        private void UpdateDwellTime()
        {
            pipeline?.Tell(new PipelineControlMessages.SetDwellTimeMessage((long) DwellTime.SelectedValue));
        }

        private void UpdateDisplaySize()
        {
            if (ImageBorder.ActualWidth > 0 && ImageBorder.ActualHeight > 0)
            {
                imageDisplay?.Tell(
                    new DisplayMessages.SetDisplaySizeMessage(new ImageSize((int)ImageBorder.ActualWidth, (int)ImageBorder.ActualHeight)));
            }
        }

        private void UpdateDigitalZoom()
        {
            imageDisplay?.Tell(new DisplayMessages.SetDigitalZoomMessage(ZoomFactor.Value, PanX.Value, PanY.Value));
        }
    }
}

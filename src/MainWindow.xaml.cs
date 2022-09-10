using System;
using ActorPipeline.Actors;
using ActorPipeline.Crates;
using ActorPipeline.Images2;
using ActorPipeline.Messages;
using ActorPipeline.Messages.Data;
using Akka.Actor;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
        private IActorRef imageDisplayPrimary;
        private IActorRef bitmapUpdaterPrimary;
        private IActorRef imageDisplayZoom;
        private IActorRef bitmapUpdaterZoom;
        private bool isRunning = false;

        private static readonly IReadOnlyList<ImageFormatCrate> ImageFormats = new List<ImageFormatCrate> 
        { 
            new ImageFormatCrate(ImageFormat.Grayscale8, "GrayScale 8 bit"),
            new ImageFormatCrate(ImageFormat.Grayscale16, "GrayScale 16 bit"),
            new ImageFormatCrate(ImageFormat.Bgra32, "RGB 24 bit"),
        };

        private static readonly IReadOnlyList<ImageSize> ImageResolutions = new List<ImageSize>
        {
            new ImageSize(768, 512),
            new ImageSize(1536, 1024),
            new ImageSize(3072, 2048),
            new ImageSize(6144, 4096),
            new ImageSize(512, 442),
            new ImageSize(1024, 884),
            new ImageSize(2048, 1768),
            new ImageSize(4096, 3536),
            new ImageSize(8192, 8192),
            new ImageSize(16384, 16384),
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
            ZoomImageBorder.SizeChanged += ZoomImageBorder_SizeChanged;
            Loaded += MainWindow_Loaded;
        }        

        private void SetUpResolutionComboBox()
        {
            Resolution.ItemsSource = ImageResolutions;
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

            imageDownscaler.Tell(new PipelineFilterMessages.ConnectToMessage(imageSource));

            bitmapUpdaterPrimary = ActorGlobals.ActorSystem.ActorOf(
                Props.Create(() => new WpfImageControlUpdaterActor(Image)).WithDispatcher("akka.actor.synchronized-dispatcher"),
                "bitmapUpdaterPrimary"
                );

            imageDisplayPrimary = await pipeline.Ask<IActorRef>(
                new PipelineControlMessages.CreateFilterMessage(
                    Props.Create(() => new ImageDisplayPipelineActor(imageDownscaler, bitmapUpdaterPrimary)), 
                    FilterPropertiesBuilder.WithName("imageDisplayPrimary").Build()
                    )
                );

            bitmapUpdaterZoom = ActorGlobals.ActorSystem.ActorOf(
                Props.Create(() => new WpfImageControlUpdaterActor(ZoomImage)).WithDispatcher("akka.actor.synchronized-dispatcher"),
                "bitmapUpdaterZoom"
                );

            imageDisplayZoom = await pipeline.Ask<IActorRef>(
                new PipelineControlMessages.CreateFilterMessage(
                    Props.Create(() => new ImageDisplayPipelineActor(imageDownscaler, bitmapUpdaterZoom)),
                    FilterPropertiesBuilder.WithName("imageDisplayZoom").Build()
                    )
                );
           
            UpdateImageFormat();
            UpdateDwellTime();
            UpdatePrimaryDisplaySize();
            UpdateZoomDisplaySize();
            imageDisplayPrimary.Tell(new DisplayMessages.SetDigitalZoomMessage(1.0, 0.5, 0.5));
            imageDisplayZoom.Tell(new DisplayMessages.SetDigitalZoomMessage(1.0, 0.5, 0.5));
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

        private void Image_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdatePrimaryDisplaySize();
        }

        private void ZoomImageBorder_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateZoomDisplaySize();
        }

        private void UpdateImageFormat()
        {
            var res = (ImageSize) Resolution.SelectedItem;
            pipeline?.Tell(
                new PipelineControlMessages.SetImageFormatMessage(res, (ImageFormat) Format.SelectedValue));
        }

        private void UpdateDwellTime()
        {
            pipeline?.Tell(new PipelineControlMessages.SetDwellTimeMessage((long) DwellTime.SelectedValue));
        }

        private void UpdatePrimaryDisplaySize()
        {
            if (ImageBorder.ActualWidth > 0 && ImageBorder.ActualHeight > 0)
            {
                imageDisplayPrimary?.Tell(
                    new DisplayMessages.SetDisplaySizeMessage(new ImageSize((int)ImageBorder.ActualWidth, (int)ImageBorder.ActualHeight)));
            }
        }

        private void UpdateZoomDisplaySize()
        {
            if (ZoomImageBorder.ActualWidth > 0 && ZoomImageBorder.ActualHeight > 0)
            {
                imageDisplayZoom?.Tell(
                    new DisplayMessages.SetDisplaySizeMessage(new ImageSize((int)ZoomImageBorder.ActualWidth, (int)ZoomImageBorder.ActualHeight)));
            }
        }

        private void Image_OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var position = e.GetPosition(Image);
            var relativePosition = new FractionalImagePosition(
                position.X / Image.ActualWidth, 
                position.Y / Image.ActualHeight
                );
            imageDisplayPrimary?.Tell(new DisplayMessages.ZoomIntoMessage(relativePosition, e.Delta/120.0));
        }
    }
}

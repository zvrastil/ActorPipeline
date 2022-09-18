using ActorPipeline.Actors;
using ActorPipeline.Crates;
using ActorPipeline.Images2;
using ActorPipeline.Messages;
using ActorPipeline.Messages.Data;
using Akka.Actor;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ActorPipeline.ViewModels
{
    internal class MainViewModel : INotifyPropertyChanged
    {
        private IActorRef pipeline;
        private IActorRef imageSource;
        private IActorRef imageDownscaler;
        private IActorRef imageDisplayPrimary;
        private IActorRef lutFilterPrimary;
        private IActorRef bitmapUpdaterPrimary;
        private IActorRef imageDisplayZoom;
        private IActorRef lutFilterZoom;
        private IActorRef bitmapUpdaterZoom;

        private ImageFormatCrate selectedImageFormat;
        private ImageSize selectedImageResolution;
        private DwellTimeCrate selectedDwellTime;
        private DigitalContrastAndBrightnessCrate selectedDcbPreset;

        private readonly DelegateCommand startStopCommand;
        private readonly DelegateCommand<bool> saveImageCommand;

        private bool isRunning;
        private FractionalImageRegion zoomedRegion = FractionalImageRegion.Invalid;

        public MainViewModel()
        {
            ImageFormats = CreateAvailableImageFormats();
            selectedImageFormat = ImageFormats[0];

            ImageResolutions = CreateAvailableResolutions();
            selectedImageResolution = ImageResolutions[0];

            DwellTimes = CreateAvailableDwellTimes();
            selectedDwellTime = DwellTimes[1];

            DcbPresets = new ObservableCollection<DigitalContrastAndBrightnessCrate>(CreateAvailableDcbPresets());
            selectedDcbPreset = DcbPresets.First();

            startStopCommand = new DelegateCommand(StartStopPipeline);
            saveImageCommand = new DelegateCommand<bool>(SaveImage);
        }        

        private static List<DwellTimeCrate> CreateAvailableDwellTimes()
        {
            return new List<DwellTimeCrate>
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
        }

        private static IReadOnlyList<ImageSize> CreateAvailableResolutions()
        {
            return new List<ImageSize>
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
        }

        private static IReadOnlyList<ImageFormatCrate> CreateAvailableImageFormats()
        {
            return new List<ImageFormatCrate>
            {
                new ImageFormatCrate(ImageFormat.Grayscale8, "GrayScale 8 bit"),
                new ImageFormatCrate(ImageFormat.Grayscale16, "GrayScale 16 bit"),
                new ImageFormatCrate(ImageFormat.Bgra32, "RGB 24 bit"),
            };
        }

        private static IReadOnlyList<DigitalContrastAndBrightnessCrate> CreateAvailableDcbPresets()
        {
            return new List<DigitalContrastAndBrightnessCrate>
            {
                new DigitalContrastAndBrightnessCrate("Default", 1.0, 0.0, 1.0),
                new DigitalContrastAndBrightnessCrate("Noisy", 2.0, -0.15, 1.0),
                new DigitalContrastAndBrightnessCrate("Backlight", 1.0, -0.1, 0.5),
                new DigitalContrastAndBrightnessCrate("Darkfield", 1.0, 0.1, 2.0),
                new DigitalContrastAndBrightnessCrate("Hard", 1.3, 0.0, 1.0),
                new DigitalContrastAndBrightnessCrate("Soft", 0.7, 0.0, 1.0),
                new DigitalContrastAndBrightnessCrate("Inverse", -1.0, 0.0, 1.0),
            };
        }

        public async Task CreatePipeline()
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
                Props.Create(() => new WpfImageDisplayUpdaterActor()).WithDispatcher("akka.actor.synchronized-dispatcher"),
                "bitmapUpdaterPrimary"
                );

            imageDisplayPrimary = await pipeline.Ask<IActorRef>(
                new PipelineControlMessages.CreateFilterMessage(
                    Props.Create(() => new ImageDisplayPipelineActor(imageDownscaler, bitmapUpdaterPrimary)),
                    FilterPropertiesBuilder.WithName("imageDisplayPrimary").Build()
                    )
                );

            lutFilterPrimary = await imageDisplayPrimary.Ask<IActorRef>(
                new DisplayMessages.AddFilterMessage(
                    Props.Create(() => new LutFilterActor()),
                    "lutFilterPrimary"
                )
            );

            bitmapUpdaterZoom = ActorGlobals.ActorSystem.ActorOf(
                Props.Create(() => new WpfImageDisplayUpdaterActor()).WithDispatcher("akka.actor.synchronized-dispatcher"),
                "bitmapUpdaterZoom"
                );

            imageDisplayZoom = await pipeline.Ask<IActorRef>(
                new PipelineControlMessages.CreateFilterMessage(
                    Props.Create(() => new ImageDisplayPipelineActor(imageDownscaler, bitmapUpdaterZoom)),
                    FilterPropertiesBuilder.WithName("imageDisplayZoom").Build()
                    )
                );

            lutFilterZoom = await imageDisplayZoom.Ask<IActorRef>(
                new DisplayMessages.AddFilterMessage(
                    Props.Create(() => new LutFilterActor()),
                    "lutFilterZoom"
                )
            );


            UpdateImageFormat();
            UpdateDwellTime();
            imageDisplayPrimary.Tell(new DisplayMessages.SetDigitalZoomMessage(1.0, 0.5, 0.5));
            imageDisplayZoom.Tell(new DisplayMessages.SetDigitalZoomMessage(1.0, 0.5, 0.5));

            ZoomedRegion = await imageDisplayPrimary.Ask<FractionalImageRegion>(new DisplayMessages.GetZoomedRegionMessage());
        }        

        public ImageFormatCrate SelectedImageFormat
        {
            get => selectedImageFormat;
            set
            {
                if (!selectedImageFormat.Equals(value))
                {
                    selectedImageFormat = value;
                    OnPropertyChanged();
                    UpdateImageFormat();
                }
            }
        }

        public IReadOnlyList<ImageFormatCrate> ImageFormats { get; }

        public ImageSize SelectedImageResolution
        {
            get => selectedImageResolution;
            set
            {
                if (!selectedImageResolution.Equals(value))
                {
                    selectedImageResolution = value;
                    OnPropertyChanged();
                    UpdateImageFormat();
                }
            }
        }

        public IReadOnlyList<ImageSize> ImageResolutions { get; }

        public DwellTimeCrate SelectedDwellTime
        {
            get => selectedDwellTime;
            set
            {
                if (!selectedDwellTime.Equals(value))
                {
                    selectedDwellTime = value;
                    OnPropertyChanged();
                    UpdateDwellTime();
                }
            }
        }

        public IReadOnlyList<DwellTimeCrate> DwellTimes { get; }

        public DigitalContrastAndBrightnessCrate SelectedDcbPreset
        {
            get => selectedDcbPreset;
            set
            {
                if (!selectedDcbPreset.Equals(value))
                {
                    selectedDcbPreset = value;
                    OnPropertyChanged();
                    UpdateLut();
                }
            }
        }

        public ObservableCollection<DigitalContrastAndBrightnessCrate> DcbPresets { get; }

        public ICommand StartStopCommand => startStopCommand;

        public string StartStopText => isRunning ? "Stop" : "Start";

        public ICommand SaveImageCommand => saveImageCommand;

        public IActorRef PrimaryDisplayUpdater => bitmapUpdaterPrimary;

        public IActorRef ZoomDisplayUpdater => bitmapUpdaterZoom;

        public FractionalImageRegion ZoomedRegion
        {
            get => zoomedRegion;
            set
            {
                if (zoomedRegion != value)
                {
                    zoomedRegion = value;
                    OnPropertyChanged();
                }
            }
        }

        public async Task ZoomInto(FractionalImagePosition zoomCenter, double delta)
        {
            if (imageDisplayPrimary != null)
            {
                imageDisplayPrimary.Tell(new DisplayMessages.ZoomIntoMessage(zoomCenter, delta));

                ZoomedRegion = await imageDisplayPrimary.Ask<FractionalImageRegion>(new DisplayMessages.GetZoomedRegionMessage());
            }
        }

        public async Task PanBy(double dx, double dy)
        {
            if (imageDisplayPrimary != null)
            {
                imageDisplayPrimary.Tell(new DisplayMessages.PanByMessage(dx, dy));
                
                ZoomedRegion = await imageDisplayPrimary.Ask<FractionalImageRegion>(new DisplayMessages.GetZoomedRegionMessage());
            }
        }

        private void UpdateImageFormat()
        {
            pipeline?.Tell(new PipelineControlMessages.SetImageFormatMessage(SelectedImageResolution, SelectedImageFormat.Format));
        }

        private void UpdateDwellTime()
        {
            pipeline?.Tell(new PipelineControlMessages.SetDwellTimeMessage(SelectedDwellTime.DwellTimeNanoseconds));
        }

        private void UpdateLut()
        {
            var message = new LutMessages.SetDigitalCBMessage(
                selectedDcbPreset.Contrast, 
                selectedDcbPreset.Brightness,
                selectedDcbPreset.Gamma);
            
            lutFilterPrimary?.Tell(message);
            lutFilterZoom?.Tell(message);
        }

        private void StartStopPipeline()
        {
            if (!isRunning)
            {
                pipeline.Tell(new PipelineControlMessages.StartMessage());
                isRunning = true;                
            }
            else
            {
                pipeline.Tell(new PipelineControlMessages.StopMessage());
                isRunning = false;
            }
            OnPropertyChanged(nameof(StartStopText));
        }

        private async void SaveImage(bool zoomedAreaOnly)
        {
            if (isRunning)
            {
                StartStopPipeline();
            }

            var saveDlg = new SaveFileDialog { Title = "Save Image", Filter = "TIF File|*.tif" };
            if (!saveDlg.ShowDialog().GetValueOrDefault())
            {
                return;
            }
            
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                bool success = await imageDisplayPrimary.Ask<bool>(new DisplayMessages.SaveImageMessage(MessageIdSource.NextId(), saveDlg.FileName, zoomedAreaOnly));
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void OnPropertyChanged([CallerMemberName]string propertyName = null)
        {
            if (propertyName == null)
            {
                return;
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}

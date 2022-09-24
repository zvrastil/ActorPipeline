using ActorPipeline.Actors;
using ActorPipeline.Images2;
using Akka.Actor;
using System.Windows;
using System.Windows.Input;
using ActorPipeline.ViewModels;
using ActorPipeline.Controls;
using System.Windows.Media;
using System.Windows.Data;

namespace ActorPipeline
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel viewModel;

        private Point previousPosition;
        private bool isDragging;
        private Matrix transformToDevice;

        public MainWindow()
        {
            viewModel = new MainViewModel();
            DataContext = viewModel;
            InitializeComponent();            
            Loaded += MainWindow_Loaded;
        }

        private void CreateZoomOverlay()
        {
            var adorner = ZoomDisplay.CreateDigitalZoomAdorner();
            adorner.SetBinding(DigitalZoomAdorner.ZoomedRegionProperty, nameof(MainViewModel.ZoomedRegion));
            adorner.SetBinding(DigitalZoomAdorner.DisplayWidthProperty, new Binding(nameof(ActualWidth)) { Source = PrimaryDisplay });
            adorner.SetBinding(DigitalZoomAdorner.DisplayHeightProperty, new Binding(nameof(ActualHeight)) { Source = PrimaryDisplay });
            ZoomDisplay.AddAdorner(adorner);
        }

        private void CreateScanLineOverlays()
        {
            foreach (var display in new[] {PrimaryDisplay, ZoomDisplay})
            {
                var adorner = display.CreateScanLineAdorner();
                adorner.SetBinding(ScanLineAdorner.PositionProperty,
                    new Binding(nameof(ImageDisplay.LastScanPosition)) {Source = display});
                display.AddAdorner(adorner);
            }
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var source = PresentationSource.FromVisual(this);
            transformToDevice = source.CompositionTarget.TransformToDevice;

            CreateScanLineOverlays();
            CreateZoomOverlay();

            await viewModel.CreatePipeline();
            viewModel.PrimaryDisplayUpdater.Tell(new WpfImageDisplayUpdaterActor.SetImageDisplayMessage(PrimaryDisplay));
            viewModel.ZoomDisplayUpdater.Tell(new WpfImageDisplayUpdaterActor.SetImageDisplayMessage(ZoomDisplay));                                 
        }                

        private async void Image_OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var position = e.GetPosition(PrimaryDisplay);
            var relativePosition = new FractionalImagePosition(
                position.X / PrimaryDisplay.ActualWidth, 
                position.Y / PrimaryDisplay.ActualHeight
                );
            await viewModel.ZoomInto(relativePosition, e.Delta / 120);
        }

        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            previousPosition = e.GetPosition(PrimaryDisplay);
            isDragging = true;
            PrimaryDisplay.ImageCaptureMouse();
        }

        private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isDragging = false;
            PrimaryDisplay.ImageReleaseMouseCapture();
        }

        private async void Image_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isDragging)
            {
                return;
            }
            var currentPosition = e.GetPosition(PrimaryDisplay);
            double dx = (currentPosition.X - previousPosition.X)/ PrimaryDisplay.ActualWidth;
            double dy = (currentPosition.Y - previousPosition.Y) / PrimaryDisplay.ActualHeight;
            previousPosition = currentPosition;
            await viewModel.PanBy(-dx, -dy);            
        }
    }
}

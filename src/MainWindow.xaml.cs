using ActorPipeline.Actors;
using ActorPipeline.Images2;
using ActorPipeline.Messages;
using Akka.Actor;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ActorPipeline.ViewModels;
using System.Windows.Documents;
using ActorPipeline.Controls;
using System.Windows.Media;

namespace ActorPipeline
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel viewModel;

        public MainWindow()
        {
            viewModel = new MainViewModel();
            DataContext = viewModel;
            InitializeComponent();            
            Loaded += MainWindow_Loaded;
        }

        private void CreateZoomOverlay()
        {
            var adorner = new DigitalZoomAdorner(ZoomImage) { Foreground = Brushes.LimeGreen, Thickness = 2.0 };
            adorner.SetBinding(DigitalZoomAdorner.ZoomedRegionProperty, nameof(MainViewModel.ZoomedRegion));
            var adornerLayer = AdornerLayer.GetAdornerLayer(ZoomImage);
            adornerLayer.Add(adorner);
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            CreateZoomOverlay();

            await viewModel.CreatePipeline();
            viewModel.PrimaryDisplayUpdater.Tell(new WpfImageControlUpdaterActor.SetImageControlMessage(Image, ImageBorder));
            viewModel.ZoomDisplayUpdater.Tell(new WpfImageControlUpdaterActor.SetImageControlMessage(ZoomImage, ZoomImageBorder));                                 
        }                

        private void Image_OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var position = e.GetPosition(Image);
            var relativePosition = new FractionalImagePosition(
                position.X / Image.ActualWidth, 
                position.Y / Image.ActualHeight
                );
            viewModel.ZoomInto(relativePosition, e.Delta / 120);
        }
    }
}

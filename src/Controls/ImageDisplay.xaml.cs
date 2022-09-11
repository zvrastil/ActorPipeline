using ActorPipeline.Images2;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace ActorPipeline.Controls
{
    /// <summary>
    /// Interaction logic for ImageDisplay.xaml
    /// </summary>
    public partial class ImageDisplay : UserControl
    {
        public ImageSource Source
        {
            get { return (ImageSource)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source", typeof(ImageSource), typeof(ImageDisplay), new PropertyMetadata(null, OnSourceChanged));

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ImageDisplay)d).Image.Source = (ImageSource)e.NewValue;
        }

        public static readonly RoutedEvent ImageMouseLeftButtonDownEvent = EventManager.RegisterRoutedEvent(
            name: "ImageMouseLeftButtonDown",
            routingStrategy: RoutingStrategy.Bubble,
            handlerType: typeof(MouseButtonEventHandler),
            ownerType: typeof(ImageDisplay));

        public event MouseButtonEventHandler ImageMouseLeftButtonDown
        {
            add { AddHandler(ImageMouseLeftButtonDownEvent, value); }
            remove { RemoveHandler(ImageMouseLeftButtonDownEvent, value); }
        }

        public static readonly RoutedEvent ImageMouseLeftButtonUpEvent = EventManager.RegisterRoutedEvent(
            name: "ImageMouseLeftButtonUp",
            routingStrategy: RoutingStrategy.Bubble,
            handlerType: typeof(MouseButtonEventHandler),
            ownerType: typeof(ImageDisplay));

        public event MouseButtonEventHandler ImageMouseLeftButtonUp
        {
            add { AddHandler(ImageMouseLeftButtonUpEvent, value); }
            remove { RemoveHandler(ImageMouseLeftButtonUpEvent, value); }
        }

        public static readonly RoutedEvent ImageMouseMoveEvent = EventManager.RegisterRoutedEvent(
            name: "ImageMouseMove",
            routingStrategy: RoutingStrategy.Bubble,
            handlerType: typeof(MouseEventHandler),
            ownerType: typeof(ImageDisplay));

        public event MouseEventHandler ImageMouseMove
        {
            add { AddHandler(ImageMouseMoveEvent, value); }
            remove { RemoveHandler(ImageMouseMoveEvent, value); }
        }

        public static readonly RoutedEvent ImageMouseWheelEvent = EventManager.RegisterRoutedEvent(
            name: "ImageMouseWheel",
            routingStrategy: RoutingStrategy.Bubble,
            handlerType: typeof(MouseWheelEventHandler),
            ownerType: typeof(ImageDisplay));

        public event MouseWheelEventHandler ImageMouseWheel
        {
            add { AddHandler(ImageMouseWheelEvent, value); }
            remove { RemoveHandler(ImageMouseWheelEvent, value); }
        }

        public ImageDisplay()
        {
            InitializeComponent();
        }

        public DigitalZoomAdorner CreateDigitalZoomAdorner() => 
            new DigitalZoomAdorner(Image) { Foreground = Brushes.LimeGreen, Thickness = 2.0 };

        public void AddDigitalZoomAdorner(DigitalZoomAdorner adorner)
        {
            var adornerLayer = AdornerLayer.GetAdornerLayer(Image);
            adornerLayer.Add(adorner);
        }

        public void ImageCaptureMouse()
        {
            Image.CaptureMouse();
        }

        public void ImageRelaseMouseCapture()
        {
            Image.ReleaseMouseCapture();
        }

        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var args = new MouseButtonEventArgs(e.MouseDevice, e.Timestamp, e.ChangedButton) { RoutedEvent = ImageMouseLeftButtonDownEvent };
            RaiseEvent(args);
        }

        private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var args = new MouseButtonEventArgs(e.MouseDevice, e.Timestamp, e.ChangedButton) { RoutedEvent = ImageMouseLeftButtonUpEvent };
            RaiseEvent(args);
        }

        private void Image_MouseMove(object sender, MouseEventArgs e)
        {
            var args = new MouseEventArgs(e.MouseDevice, e.Timestamp) { RoutedEvent = ImageMouseMoveEvent };
            RaiseEvent(args);
        }

        private void Image_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var args = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta) { RoutedEvent = ImageMouseWheelEvent };
            RaiseEvent(args);
        }
    }
}

namespace ActorPipeline.Messages.Data
{
    public sealed class ZoomedImageArea
    {
        public ZoomedImageArea(double zoomFactor, double panX, double panY, double aspectRatio)
        {
            ZoomFactor = zoomFactor;
            PanX = panX;
            PanY = panY;
            AspectRatio = aspectRatio;
        }

        public double ZoomFactor { get; }
        public double PanX { get; }
        public double PanY { get; }
        public double AspectRatio { get; }

        public override string ToString() => $"[{ZoomFactor}x, ({PanX}, {PanY}), aspectRatio: {AspectRatio}]";
    }
}

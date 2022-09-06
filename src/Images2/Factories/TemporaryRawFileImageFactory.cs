using System.IO;
using ActorPipeline.Images2.Images;

namespace ActorPipeline.Images2.Factories
{
    public class TemporaryRawFileImageFactory : IImageFactory
    {
        public static IImageFactory Instance { get; } = new TemporaryRawFileImageFactory();

        public IImage Create(int width, int height, ImageFormat format) => new TemporaryRawFileDecorator(TemporaryRawFileDecorator.GetTempFilePath(), width, height, format);

        private class TemporaryRawFileDecorator : RawFileImage
        {
            private readonly string filePath;

            public TemporaryRawFileDecorator(string filePath, int width, int height, ImageFormat format)
                : base(RawFileImageMode.Create, filePath, width, height, format)
            {
                this.filePath = filePath;
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                File.Delete(filePath);
            }

            public static string GetTempFilePath() => Path.GetTempFileName();
        }
    }
}

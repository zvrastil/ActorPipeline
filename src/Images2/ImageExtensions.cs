using System.Runtime.InteropServices;
using System.Windows.Markup;

namespace ActorPipeline.Images2
{
    public static class ImageExtensions
    {
        public static void AddDataToRegion(this IImage image, ImageRegion region, byte[] data)
        {
            for (int i = 0; i < region.Height; i++)
            {
                int y = i + region.Top;
                using (var line = image.LockLineForWriting(y))
                {
                    Marshal.Copy(
                        data, 
                        i*region.Width*image.BytesPerPixel, 
                        line.Ptr+region.Left*image.BytesPerPixel, 
                        region.Width*image.BytesPerPixel);
                    line.Commit();
                }
            }
        }

        public static byte[] ExtractData(this IImage image, ImageRegion region)
        {
            byte[] result = new byte[region.Width * region.Height * image.BytesPerPixel];
            for (int i = 0; i < region.Height; i++)
            {
                int y = i + region.Top;
                using (var line = image.LockLineForWriting(y))
                {
                    Marshal.Copy(
                        line.Ptr + region.Left * image.BytesPerPixel,
                        result,
                        i * region.Width * image.BytesPerPixel,
                        region.Width * image.BytesPerPixel
                        );
                }
            }

            return result;
        }
    }
}

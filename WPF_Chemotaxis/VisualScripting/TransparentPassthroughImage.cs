using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WPF_Chemotaxis.VisualScripting
{
    internal class TransparentPassthroughImage : Image
    {
        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            var source = (BitmapSource)Source;

            // Get the pixel of the source that was hit
            var x = (int)(hitTestParameters.HitPoint.X / ActualWidth * source.PixelWidth);
            var y = (int)(hitTestParameters.HitPoint.Y / ActualHeight * source.PixelHeight);

            if (x < 0 || y < 0 || x >= source.PixelWidth || y >= source.PixelHeight) return null; 

            var bytesPerPixel = (source.Format.BitsPerPixel + 7) / 8;
            var bytes = new byte[bytesPerPixel];
            var rect = new Int32Rect(x, y, 1, 1);

            source.CopyPixels(rect, bytes, bytesPerPixel, 0);

            if (source.Format == PixelFormats.Bgra32)
            {
                if (bytes[3] > 10) return new PointHitTestResult(this, hitTestParameters.HitPoint);
                else               return null;
            }
            else return base.HitTestCore(hitTestParameters);
        }
    }
}

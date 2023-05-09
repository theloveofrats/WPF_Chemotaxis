using System;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows;

namespace WPF_Chemotaxis.VisualScripting
{
    public class VSListMenuElement
    {
        public string IconResourcePath { get; private set; }
        public string UIDisplayLabel { get; private set; }
        public double IconSize { get; private set; }
        public Type TargetType { get; private set; }
        public Point NametagOffset { get; private set; }
        public bool tagAlignCentre { get; private set; }

        private static int colorIndex = 0;
        private static Color[] colors = new Color[] { Color.FromRgb(217,85,85),   Color.FromRgb(255, 122, 0), Color.FromRgb(253,245,103),
                                                      Color.FromRgb(100,177,242), Color.FromRgb(107,120,149), Color.FromRgb(80,141,93),
                                                      Color.FromRgb(107,120,149), Color.FromRgb(121,88,181),  Color.FromRgb(164,79,153)};



        public VSListMenuElement(string UIDisplayLabel, string IconResourcePath, double IconSize, Type TargetType, Point NametagOffset, bool tagAlignCentre)
        {
            this.UIDisplayLabel = UIDisplayLabel;
            this.IconResourcePath = IconResourcePath;
            this.IconSize = IconSize;
            this.TargetType = TargetType;
            this.NametagOffset = NametagOffset;
            this.tagAlignCentre = tagAlignCentre;
        }
        public VSListMenuElement(VSElementAttribute attribute, Type targetType)
        {
            this.UIDisplayLabel = attribute.ui_TypeLabel;
            this.IconResourcePath = attribute.symbolResourcePath;
            this.IconSize = attribute.symbolSize;
            this.TargetType = targetType;
            this.NametagOffset = new Point(attribute.tagX, attribute.tagY);
            this.tagAlignCentre = attribute.tagCentre;
        }

        public Image CreateModelElementControl()
        {
            Image image = new TransparentPassthroughImage();
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(@"pack://application:,,,/"+this.IconResourcePath);
            bmp.EndInit();

            var colorBmp = new WriteableBitmap(bmp);

            Color nextColor = colors[colorIndex++ % colors.Length];

            for(int i=0; i<colorBmp.PixelWidth; i++)
            {
                for(int j=0; j<colorBmp.PixelHeight; j++)
                {
                    if (colorBmp.GetPixel(i, j).A == 0) continue;

                    double intensity = colorBmp.GetPixel(i, j).R/255d;
                    Color newColor = Color.FromArgb(colorBmp.GetPixel(i, j).A, (byte)(intensity * nextColor.R), (byte)(intensity * nextColor.G), (byte)(intensity * nextColor.B));
                    colorBmp.SetPixel(i, j, newColor);
                }
            }

            image.Source = colorBmp;
            image.Width = 10 * IconSize;
            image.Height = 10 * IconSize;
            return image;
        }
    }
}

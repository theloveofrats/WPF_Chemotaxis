using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WPF_Chemotaxis.Model;

namespace WPF_Chemotaxis.Simulations
{    
    class RegionFinder
    {
        private WriteableBitmap img;
        private bool[,] dirty;
        private ICollection<Region> regions = new List<Region>();

        // Makes a bitmask for the various states each pixel can be in. Currently only 4 bits, space for a little more in the byte!
        // Ideally this would be replaced with a flexible rule system, but it's probably best to keep it to 8 toggles anyhow for performance reasons.

        public ICollection<Region> FindRegionsInImage(string path, out byte[] map, out int width, out int height)
        {
            //Forces conversion to 32bit BGRA first.
            this.img = SetSource(path);
            width = img.PixelWidth;
            height = img.PixelHeight;         
            dirty = new bool[width, height];

            //So now, all contiguous areas should have been put into regions!
            FindRegions(width, height);

            // First of all, just apply the open/closed map. Regions can do their own thing soon.
            map = new byte[width * height];
            for(int i=0; i<width; i++)
            {
                for(int j=0; j<height; j++)
                {
                    bool free  = img.GetPixel(i, j).R > 1 || img.GetPixel(i, j).G > 1 || img.GetPixel(i, j).B > 1;
                    Environment.PointType val = (free ? Environment.PointType.FREE : 0);
                    map[j * width + i] = (byte) val;
                }
            }

            return regions;
        }

        private void FindRegions(int w, int h)
        {
            Color clr;
            for(int i=0; i<w; i++)
            {
                for(int j=0; j<h; j++)
                {
                    //Already accounted for
                    if (dirty[i, j]) continue;
                    clr = img.GetPixel(i, j);
                    ICollection<Vector2Int> points = GetContiguousPoints(i, j);
                    RegionType rt = RegionType.GetRegionType(clr);

                    if (rt!=null && rt.Rules!=null && rt.Rules.Count > 0)
                    {
                        regions.Add(new Region(rt, points));
                    }
                }
            }
        }

        // Kind of a flood fill, but it sets an external mark to diry so we can get ALL regions.
        private ICollection<Vector2Int> GetContiguousPoints(int x,int y)
        {
            Color target_clr = img.GetPixel(x, y);
            ICollection<Vector2Int> coords = new HashSet<Vector2Int>();       

            Queue<Vector2Int> queue = new();
            queue.Enqueue(new Vector2Int(x,y));

            Vector2Int focus;
            while(queue.TryDequeue(out focus))
            {
                if (img.GetPixel(focus.X, focus.Y)==target_clr && !dirty[focus.X,focus.Y])
                {
                    coords.Add(focus);
                    dirty[focus.X, focus.Y] = true;
                    if (focus.X + 1 < img.PixelWidth) queue.Enqueue(new Vector2Int(focus.X + 1, focus.Y));
                    if (focus.X - 1 > 0) queue.Enqueue(new Vector2Int(focus.X - 1, focus.Y));
                    if (focus.Y + 1 < img.PixelHeight) queue.Enqueue(new Vector2Int(focus.X, focus.Y+1));
                    if (focus.Y - 1 > 0) queue.Enqueue(new Vector2Int(focus.X, focus.Y - 1));
                }
            }

            return coords;
        }


        private static WriteableBitmap SetSource(string pathToImage)
        {
            BitmapImage image = new BitmapImage();
            BitmapSource source;
            image.BeginInit();
            image.UriSource = new Uri(pathToImage);
            image.EndInit();

            if (image.Format != PixelFormats.Bgra32)
            {
                source = new FormatConvertedBitmap(image, PixelFormats.Bgra32, null, 0);
            }
            else
            {
                source = image;
            }
            return new WriteableBitmap(source);
        }
    }
}

namespace WPF_Chemotaxis.UX
{
	public class HeatPoint
	{
		public int X;
		public int Y;
		public byte Intensity;

		public HeatPoint(int in_x, int in_y, byte in_intensity)
		{
			this.X = in_x;
			this.Y = in_y;
			this.Intensity = in_intensity;
		}
	}
}
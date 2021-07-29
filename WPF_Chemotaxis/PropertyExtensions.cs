using System.Windows;

namespace WPF_Chemotaxis
{
	public static class PropertyExtensions
	{
		public static readonly DependencyProperty TitleProperty =
			DependencyProperty.RegisterAttached(
				"Title",
				typeof(string),
				typeof(PropertyExtensions),
				new PropertyMetadata(string.Empty));

		public static void SetTitle(DependencyObject obj, string value)
		{
			obj.SetValue(TitleProperty, value);
		}

		public static string GetTitle(DependencyObject obj)
		{
			return (string)obj.GetValue(TitleProperty);
		}
	}
}
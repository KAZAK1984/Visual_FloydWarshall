using System.Globalization;
using System.Windows.Data;

namespace Visual_FloydWarshall.Utility
{
	public sealed class SubtractOneConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!int.TryParse(value?.ToString(), out var parsedValue))
				return 0;

			return Math.Max(0, parsedValue - 1);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
			Binding.DoNothing;
	}
}

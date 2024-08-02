using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace SIL.FieldWorks.LexText.Controls
{
	internal class PositiveIntToRedBrushConverter: IValueConverter
	{
		private static readonly Brush RedBrush = new SolidColorBrush(Colors.Red);

		static PositiveIntToRedBrushConverter()
		{
			RedBrush.Freeze();
		}

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is int intValue)
			{
				if (intValue > 0)
					return RedBrush;
			}
			return DependencyProperty.UnsetValue;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}

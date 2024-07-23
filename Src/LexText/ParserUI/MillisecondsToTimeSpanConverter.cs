using System;
using System.Globalization;
using System.Windows.Data;

namespace SIL.FieldWorks.LexText.Controls
{
	public class MillisecondsToTimeSpanConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is long fileTime)
			{
				return TimeSpan.FromMilliseconds(fileTime);
			}
			return value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is TimeSpan timeSpan)
			{
				return timeSpan.TotalMilliseconds;
			}
			return value;
		}
	}
}

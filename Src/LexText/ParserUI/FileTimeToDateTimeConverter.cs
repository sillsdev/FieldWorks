using System;
using System.Globalization;
using System.Windows.Data;

namespace SIL.FieldWorks.LexText.Controls
{
	public class FileTimeToDateTimeConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is long fileTime)
			{
				return DateTime.FromFileTime(fileTime);
			}
			return value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is DateTime dateTime)
			{
				return dateTime.ToFileTime();
			}
			return value;
		}
	}
}

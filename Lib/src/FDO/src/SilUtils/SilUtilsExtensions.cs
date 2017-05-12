using System;

namespace SIL.Utils
{
	public static class SilUtilsExtensions
	{
		#region DateTime extensions
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a datetime value with the seconds and milliseconds stripped off (does not
		/// actually round to the nearest minute).
		/// </summary>
		/// <param name="value">The value.</param>
		/// ------------------------------------------------------------------------------------
		public static DateTime ToTheMinute(this DateTime value)
		{
			return (value.Second != 0 || value.Millisecond != 0) ?
				new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0) : value;
		}
		#endregion
	}
}

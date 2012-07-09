using System;
using System.Text;
using System.Globalization;
using System.Collections.Generic;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// This class encapsulates the generic (vague) date type. This type can store dates
	/// that are partially defined, i.e. the day, month, or year is unknown. Day and month
	/// are not supported for BC dates.
	/// </summary>
	public struct GenDate : IEquatable<GenDate>, IComparable, IComparable<GenDate>
	{
		/// <summary>
		/// The unknown day value.
		/// </summary>
		public const int UnknownDay = 0;
		/// <summary>
		/// The unknown month value.
		/// </summary>
		public const int UnknownMonth = 0;
		/// <summary>
		/// The unknown year value.
		/// </summary>
		public const int UnknownYear = 0;
		/// <summary>
		/// The maximum year value.
		/// </summary>
		public const int MaxYear = 9999;
		/// <summary>
		/// The minimum year value
		/// </summary>
		public const int MinYear = 1;
		/// <summary>
		/// The maximum month value.
		/// </summary>
		public const int MaxMonth = 12;
		/// <summary>
		/// The minimum month value.
		/// </summary>
		public const int MinMonth = 1;
		/// <summary>
		/// The minimum day value.
		/// </summary>
		public const int MinDay = 1;

		private const int LeapYear = 2008;

		/// <summary>
		/// The generic date precision types.
		/// </summary>
		public enum PrecisionType
		{
			/// <summary>
			/// Before
			/// </summary>
			Before = 0,
			/// <summary>
			/// Exact
			/// </summary>
			Exact = 1,
			/// <summary>
			/// Approximate
			/// </summary>
			Approximate = 2,
			/// <summary>
			/// After
			/// </summary>
			After = 3
		}

		private readonly int m_day;
		private readonly int m_month;
		private readonly int m_year;
		private readonly bool m_ad;
		private readonly PrecisionType m_precision;

		/// <summary>
		/// Initializes a new instance of the <see cref="GenDate"/> struct.
		/// </summary>
		/// <param name="precision">The precision.</param>
		/// <param name="month">The month.</param>
		/// <param name="day">The day.</param>
		/// <param name="year">The year.</param>
		/// <param name="ad">if set to <c>true</c> the date is AD, otherwise it is BC.</param>
		public GenDate(PrecisionType precision, int month, int day, int year, bool ad)
		{

			var badPart = ValidateParts(month, day, year, ad);
			if (badPart != null)
				throw new ArgumentOutOfRangeException(badPart);

			m_precision = precision;
			m_month = month;
			m_day = day;
			m_year = year;
			m_ad = ad;
		}

		/// <summary>
		/// If the specified arguments will make a valid GenDate, return null. Otherwise return the name of the bad parameter.
		/// </summary>
		/// <param name="month"></param>
		/// <param name="day"></param>
		/// <param name="year"></param>
		/// <param name="ad"></param>
		/// <returns></returns>
		public static string ValidateParts(int month, int day, int year, bool ad)
		{
			if (year != UnknownYear && (year < MinYear || year > MaxYear))
				return "year";

			if (month != UnknownMonth && (!ad || month < MinMonth || month > MaxMonth))
				return "month";

			if (day != UnknownDay && (!ad || month == UnknownMonth
				|| day < MinDay || day > DateTime.DaysInMonth(year == UnknownYear ? LeapYear : year, month)))
				return "day";
			return null; // all is well.
		}

		/// <summary>
		/// Gets the precision.
		/// </summary>
		/// <value>The precision.</value>
		public PrecisionType Precision
		{
			get
			{
				return m_precision;
			}
		}

		/// <summary>
		/// Gets the month.
		/// </summary>
		/// <value>The month.</value>
		public int Month
		{
			get
			{
				return m_month;
			}
		}

		/// <summary>
		/// Gets the day.
		/// </summary>
		/// <value>The day.</value>
		public int Day
		{
			get
			{
				return m_day;
			}
		}

		/// <summary>
		/// Gets the year.
		/// </summary>
		/// <value>The year.</value>
		public int Year
		{
			get
			{
				return m_year;
			}
		}

		/// <summary>
		/// Gets a value indicating whether the date is AD or BC.
		/// </summary>
		/// <value><c>true</c> if the year is AD; otherwise, the year is BC.</value>
		public bool IsAD
		{
			get
			{
				return m_ad;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this instance represents an empty generic date.
		/// </summary>
		/// <value><c>true</c> if this instance is empty; otherwise, <c>false</c>.</value>
		public bool IsEmpty
		{
			get
			{
				return m_year == UnknownYear && m_month == UnknownMonth && m_day == UnknownDay;
			}
		}

		private DateTime Date
		{
			get
			{
				return new DateTime(m_year == UnknownYear ? 1 : m_year, m_month == UnknownMonth ? 1 : m_month,
					m_day == UnknownDay ? 1 : m_day);
			}
		}

		/// <summary>
		/// Returns the long string format, for example "Before Friday, October 09, 2009".
		/// </summary>
		/// <returns></returns>
		public string ToLongString()
		{
			if (IsEmpty)
				return string.Empty;

			var date = Date;
			var sb = new StringBuilder();
			switch (m_precision)
			{
				case PrecisionType.Approximate:
					sb.Append(FwUtilsStrings.ksGenDateApprox);
					sb.Append(" ");
					break;
				case PrecisionType.After:
					sb.Append(FwUtilsStrings.ksGenDateAfter);
					sb.Append(" ");
					break;
				case PrecisionType.Before:
					sb.Append(FwUtilsStrings.ksGenDateBefore);
					sb.Append(" ");
					break;
			}

			if (m_month != UnknownMonth && m_ad)
			{
				if (m_day != UnknownDay && m_year != UnknownYear)
				{
					sb.Append(date.ToString("dddd"));
					sb.Append(", ");
				}
				sb.Append(date.ToString("MMMM"));
				if (m_day != UnknownDay)
				{
					sb.Append(" ");
					sb.Append(date.ToString("dd"));
				}

				if (m_year != UnknownYear)
					sb.Append(", ");
			}
			else if (m_year != UnknownYear && m_ad)
			{
				sb.Append(FwUtilsStrings.ksGenDateAD);
				sb.Append(" ");
			}

			if (m_year != UnknownYear)
			{
				sb.Append(m_year);
				if (!m_ad)
				{
					sb.Append(" ");
					sb.Append(FwUtilsStrings.ksGenDateBC);
				}
			}
			return sb.ToString();
		}

		/// <summary>
		/// Returns the short string format in M/d/yyy format (keep export consistant)
		/// </summary>
		/// <returns></returns>
		public string ToXMLExportShortString()
		{
			if (IsEmpty)
				return string.Empty;

			if (m_month != UnknownMonth && m_day != UnknownDay && m_year != UnknownYear && m_ad)
			{
				var date = Date;
				var sb = new StringBuilder();
				switch (m_precision)
				{
					case PrecisionType.Approximate:
						sb.Append(FwUtilsStrings.ksGenDateApprox);
						sb.Append(" ");
						break;
					case PrecisionType.After:
						sb.Append(FwUtilsStrings.ksGenDateAfter);
						sb.Append(" ");
						break;
					case PrecisionType.Before:
						sb.Append(FwUtilsStrings.ksGenDateBefore);
						sb.Append(" ");
						break;
				}
				sb.Append(date.ToString("M/d/yyy"));
				return sb.ToString();
			}
			else
			{
				return ToLongString();
			}
		}

		/// <summary>
		/// Returns the short string format, for example "Before 10/9/2009".
		/// </summary>
		/// <returns></returns>
		public string ToShortString()
		{
			if (IsEmpty)
				return string.Empty;

			if (m_month != UnknownMonth && m_day != UnknownDay && m_year != UnknownYear && m_ad)
			{
				var date = Date;
				var sb = new StringBuilder();
				switch (m_precision)
				{
					case PrecisionType.Approximate:
						sb.Append(FwUtilsStrings.ksGenDateApprox);
						sb.Append(" ");
						break;
					case PrecisionType.After:
						sb.Append(FwUtilsStrings.ksGenDateAfter);
						sb.Append(" ");
						break;
					case PrecisionType.Before:
						sb.Append(FwUtilsStrings.ksGenDateBefore);
						sb.Append(" ");
						break;
				}
				sb.Append(date.ToShortDateString());
				return sb.ToString();
			}
			else
			{
				return ToLongString();
			}
		}

		/// <summary>
		/// Returns the sort string format, for example "1:2009-10-09 0".
		/// </summary>
		/// <returns></returns>
		public string ToSortString()
		{
			if (IsEmpty)
				return string.Empty;

			return string.Format("{0}:{1:0000}-{2:00}-{3:00} {4}", m_ad ? "1" : "0", m_ad ? m_year : MaxYear - m_year + 1,
				m_month, m_day, (int)m_precision);
		}

		/// <summary>
		/// Returns the long string format.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return ToLongString();
		}

		private static string[] s_monthNames = null;

		/// <summary>
		/// Try to parse the string as a generic date.  If successful, return true and set all
		/// the output variables accordingly.
		/// </summary>
		public static bool TryParse(string date, out GenDate gen)
		{
			try
			{
				gen = LoadFromString(date);
				return true;
			}
			catch (Exception)
			{
				//Ok, reading the date accordingto our standard format failed, fall into historic backup case.
			}
			PrecisionType precision = PrecisionType.Exact;
			bool fAD = true;

			date = date.Trim();
			if (date.StartsWith(FwUtilsStrings.ksGenDateApprox))
			{
				precision = PrecisionType.Approximate;
				date = date.Substring(FwUtilsStrings.ksGenDateApprox.Length + 1);
			}
			else if (date.StartsWith(FwUtilsStrings.ksGenDateAfter))
			{
				precision = PrecisionType.After;
				date = date.Substring(FwUtilsStrings.ksGenDateAfter.Length + 1);
			}
			else if (date.StartsWith(FwUtilsStrings.ksGenDateBefore))
			{
				precision = PrecisionType.Before;
				date = date.Substring(FwUtilsStrings.ksGenDateBefore.Length + 1);
			}
			if (date.Contains(FwUtilsStrings.ksGenDateAD + " "))
			{
				fAD = true;
				date = date.Replace(FwUtilsStrings.ksGenDateAD + " ", "");
			}
			else if (date.EndsWith(FwUtilsStrings.ksGenDateBC))
			{
				fAD = false;
				date = date.Substring(0, date.Length - (FwUtilsStrings.ksGenDateBC.Length + 1));
			}
			int nDay = UnknownDay;
			int nMonth = UnknownMonth;
			int nYear = UnknownYear;
			if (Int32.TryParse(date, out nYear))
			{
				if (nYear <= 0)
					nYear = -nYear;
				if (ValidateParts(nMonth, nDay, nYear, fAD) == null)
				{
					gen = new GenDate(precision, nMonth, nDay, nYear, fAD);
					return true;
				}
			}
			if (fAD)
			{
				string rawDate = date;
				if (s_monthNames == null)
					s_monthNames = GetMonthNames();
				for (int i = 0; i < s_monthNames.Length; ++i)
				{
					if (date.StartsWith(s_monthNames[i]))
					{
						nMonth = i + 1;
						date = date.Substring(s_monthNames[i].Length);
						break;
					}
				}
				if (nMonth != UnknownMonth)
				{
					nYear = UnknownYear;
					if (date.StartsWith(", "))
					{
						date = date.Substring(2);
						if (Int32.TryParse(date, out nYear) && ValidateParts(nMonth, nDay, nYear, fAD) == null)
						{
							gen = new GenDate(precision, nMonth, nDay, nYear, fAD);
							return true;
						}
					}
					else if (date.StartsWith(" "))
					{
						date = date.Substring(1);
						if (Int32.TryParse(date, out nDay) && nDay < 32 && ValidateParts(nMonth, nDay, nYear, fAD) == null)
						{
							gen = new GenDate(precision, nMonth, nDay, nYear, fAD);
							return true;
						}
					}
					else if (String.IsNullOrEmpty(date) && ValidateParts(nMonth, nDay, nYear, fAD) == null)
					{
						gen = new GenDate(precision, nMonth, nDay, nYear, fAD);
						return true;
					}
				}
				DateTime dt;
				if (DateTime.TryParse(rawDate, out dt) && ValidateParts(dt.Month, dt.Day, dt.Year, fAD) == null)
				{
					gen = new GenDate(precision, dt.Month, dt.Day, dt.Year, fAD);
					return true;
				}
				// Any other last ditch efforts?
			}
			gen = new GenDate();
			return false;
		}

		private static string[] GetMonthNames()
		{
			// The code that you think would work throws an exception for 'en' (and
			// presumably other semi-generic language/culture tags):
			// string[] monthNames = CultureInfo.CurrentCulture.DateTimeFormat.MonthNames;
			// So we have to trick it into giving us the desired information.
			List<string> monthNames = new List<string>(12);
			for (int m = 1; m <= 12; ++m)
			{
				DateTime dt = new DateTime(2010, m, 1);
				string month = dt.ToString("MMMM");
				monthNames.Add(month);
			}
			return monthNames.ToArray();
		}

		/// <summary>
		/// Returns the hash code for this instance.
		/// </summary>
		/// <returns>
		/// A 32-bit signed integer that is the hash code for this instance.
		/// </returns>
		public override int GetHashCode()
		{
			return ToSortString().GetHashCode();
		}

		/// <summary>
		/// Indicates whether the current generic date is equal to another generic date.
		/// </summary>
		/// <param name="other">A generic date to compare with this generic date.</param>
		/// <returns>
		/// true if the current generic date is equal to the <paramref name="other"/> parameter; otherwise, false.
		/// </returns>
		public override bool Equals(object other)
		{
			if (!(other is GenDate))
				return false;
			return Equals((GenDate)other);
		}

		/// <summary>
		/// Indicates whether the current generic date is equal to another generic date.
		/// </summary>
		/// <param name="other">A generic date to compare with this generic date.</param>
		/// <returns>
		/// true if the current generic date is equal to the <paramref name="other"/> parameter; otherwise, false.
		/// </returns>
		public bool Equals(GenDate other)
		{
			if (IsEmpty && other.IsEmpty)
				return true;

			return CompareTo(other) == 0;
		}

		/// <summary>
		/// Compares to.
		/// </summary>
		/// <param name="other">The other.</param>
		/// <returns></returns>
		public int CompareTo(object other)
		{
			if (!(other is GenDate))
				throw new ArgumentException();
			return CompareTo((GenDate)other);
		}

		/// <summary>
		/// Compares the current generic date with another generic date.
		/// </summary>
		/// <param name="other">A generic date to compare with this generic date.</param>
		/// <returns>
		/// A 32-bit signed integer that indicates the relative order of the dates being compared. The return value has the following meanings:
		/// Value
		/// Meaning
		/// Less than zero
		/// This date is earlier than the <paramref name="other"/> parameter.
		/// Zero
		/// This date is equal to <paramref name="other"/>.
		/// Greater than zero
		/// This date is later than <paramref name="other"/>.
		/// </returns>
		public int CompareTo(GenDate other)
		{
			return ToSortString().CompareTo(other.ToSortString());
		}

		/// <summary>
		/// Implements the operator ==.
		/// </summary>
		/// <param name="g1">The first generic date.</param>
		/// <param name="g2">The second generic date.</param>
		/// <returns>The result of the operator.</returns>
		public static bool operator ==(GenDate g1, GenDate g2)
		{
			return g1.Equals(g2);
		}

		/// <summary>
		/// Implements the operator !=.
		/// </summary>
		/// <param name="g1">The first generic date.</param>
		/// <param name="g2">The second generic date.</param>
		/// <returns>The result of the operator.</returns>
		public static bool operator !=(GenDate g1, GenDate g2)
		{
			return !g1.Equals(g2);
		}

		/// <summary>
		/// Implements the operator &gt;.
		/// </summary>
		/// <param name="g1">The first generic date.</param>
		/// <param name="g2">The second generic date.</param>
		/// <returns>The result of the operator.</returns>
		public static bool operator >(GenDate g1, GenDate g2)
		{
			return g1.CompareTo(g2) > 0;
		}

		/// <summary>
		/// Implements the operator &gt;=.
		/// </summary>
		/// <param name="g1">The first generic date.</param>
		/// <param name="g2">The second generic date.</param>
		/// <returns>The result of the operator.</returns>
		public static bool operator >=(GenDate g1, GenDate g2)
		{
			return g1.CompareTo(g2) >= 0;
		}

		/// <summary>
		/// Implements the operator &lt;.
		/// </summary>
		/// <param name="g1">The first generic date.</param>
		/// <param name="g2">The second generic date.</param>
		/// <returns>The result of the operator.</returns>
		public static bool operator <(GenDate g1, GenDate g2)
		{
			return g1.CompareTo(g2) < 0;
		}

		/// <summary>
		/// Implements the operator &lt;=.
		/// </summary>
		/// <param name="g1">The first generic date.</param>
		/// <param name="g2">The second generic date.</param>
		/// <returns>The result of the operator.</returns>
		public static bool operator <=(GenDate g1, GenDate g2)
		{
			return g1.CompareTo(g2) <= 0;
		}

		/// <summary>
		/// Loads a standard GenDate integer from the string.
		/// </summary>
		/// <param name="genDateStr"></param>
		/// <returns></returns>
		public static GenDate LoadFromString(string genDateStr)
		{
			if (!string.IsNullOrEmpty(genDateStr) && Convert.ToInt32(genDateStr) != 0)
			{
				var ad = true;
				if (genDateStr.StartsWith("-"))
				{
					ad = false;
					genDateStr = genDateStr.Substring(1);
				}
				genDateStr = genDateStr.PadLeft(9, '0');
				var year = Convert.ToInt32(genDateStr.Substring(0, 4));
				var month = Convert.ToInt32(genDateStr.Substring(4, 2));
				var day = Convert.ToInt32(genDateStr.Substring(6, 2));
				var precision = (PrecisionType)Convert.ToInt32(genDateStr.Substring(8, 1));
				return new GenDate(precision, month, day, year, ad);
			}

			return new GenDate();
		}
	}
}

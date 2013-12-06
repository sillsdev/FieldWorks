using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.Filters
{
	public class DateTimeMatcher : BaseMatcher
	{
		DateMatchType m_type;
		DateTime m_start;
		DateTime m_end;

		/// <summary>
		/// Default constructor for IPersistAsXml
		/// </summary>
		public DateTimeMatcher()
		{
		}

		public DateTimeMatcher(DateTime start, DateTime end, DateMatchType type)
		{
			m_start = start;
			m_end = end;
			m_type = type;
			IsStartAD = true;
			IsEndAD = true;
			UnspecificMatching = false;
		}

		/// <summary>
		/// The start time (used for start of range and not range, on or after)
		/// </summary>
		public DateTime Start
		{
			get { return m_start; }
		}

		public DateMatchType MatchType
		{
			get { return m_type; }
		}

		/// <summary>
		/// The end time (used for end of range and not range, on or before)
		/// </summary>
		public DateTime End
		{
			get { return m_end; }
		}

		/// <summary>
		/// Flag whether we are matching GenDate objects instead of DateTime objects.
		/// </summary>
		public bool HandleGenDate { get; set; }

		// The next three properties are also used with GenDate comparisons.
		public bool IsStartAD { get; set; }
		public bool IsEndAD { get; set; }
		public bool UnspecificMatching { get; set; }

		public override bool Matches(ITsString arg)
		{
			string text = arg.Text;
			if (String.IsNullOrEmpty(text))
				return false;
			DateTime time;
			GenDate gen;
			if (!HandleGenDate && DateTime.TryParse(text, out time))
			{
				switch (m_type)
				{
					case DateMatchType.After:
						return time >= m_start;
					case DateMatchType.Before:
						return time <= m_end;
					case DateMatchType.Range:
					case DateMatchType.On:
						return time >= m_start && time <= m_end;
					case DateMatchType.NotRange:
						return time < m_start || time > m_end;
				}
			}
			else if (HandleGenDate && GenDate.TryParse(text, out gen))
			{
				switch (m_type)
				{
					case DateMatchType.After:
						return GenDateIsAfterDate(gen, m_start, IsStartAD);
					case DateMatchType.Before:
						return GenDateIsBeforeDate(gen, m_end, IsEndAD);
					case DateMatchType.Range:
					case DateMatchType.On:
						return GenDateIsInRange(gen);
					case DateMatchType.NotRange:
						return !GenDateIsInRange(gen);
				}
			}
			return false;
		}

		private bool GenDateIsBeforeDate(GenDate gen, DateTime date, bool fAD)
		{
			if (UnspecificMatching)
				return GenDateMightBeBeforeDate(gen, date, fAD);

			if (gen.IsAD && !fAD)		// AD > BC
				return false;
			if (!gen.IsAD && fAD)		// BC < AD
				return gen.Precision != GenDate.PrecisionType.After;
			if (!gen.IsAD && !fAD)		// Both BC
			{
				if (gen.Year > date.Year)
					return gen.Precision != GenDate.PrecisionType.After;
				else if (gen.Year < date.Year)
					return false;
			}
			if (gen.IsAD && fAD)		// Both AD
			{
				if (gen.Year < date.Year)
					return gen.Precision != GenDate.PrecisionType.After;
				else if (gen.Year > date.Year)
					return false;
			}
			if (gen.Month < date.Month)
			{
				return gen.Month != GenDate.UnknownMonth ||
					gen.Precision == GenDate.PrecisionType.Before;
			}
			else if (gen.Month > date.Month)
			{
				return false;
			}
			if (gen.Day == GenDate.UnknownDay)
				return gen.Precision == GenDate.PrecisionType.Before;
			return gen.Day <= date.Day && gen.Precision != GenDate.PrecisionType.After;
		}

		private bool GenDateMightBeBeforeDate(GenDate gen, DateTime date, bool fAD)
		{
			if (gen.IsAD && !fAD)		// AD > BC
				return gen.Precision == GenDate.PrecisionType.Before;
			if (!gen.IsAD && fAD)		// BC < AD
				return true;
			if (!gen.IsAD && !fAD)		// Both BC
			{
				if (gen.Year > date.Year)
					return true;
				else if (gen.Year < date.Year)
					return gen.Precision == GenDate.PrecisionType.Before;
			}
			if (gen.IsAD && fAD)		// Both AD
			{
				if (gen.Year < date.Year)
					return true;
				else if (gen.Year > date.Year)
					return gen.Precision == GenDate.PrecisionType.Before;
			}
			if (gen.Month < date.Month)
			{
				return gen.Month != GenDate.UnknownMonth ||
					gen.Precision != GenDate.PrecisionType.After;
			}
			else if (gen.Month > date.Month)
			{
				return gen.Precision == GenDate.PrecisionType.Before;
			}
			return gen.Day <= date.Day || gen.Precision == GenDate.PrecisionType.Before;
		}

		private bool GenDateIsAfterDate(GenDate gen, DateTime date, bool fAD)
		{
			if (UnspecificMatching)
				return GenDateMightBeAfterDate(gen, date, fAD);

			if (gen.IsAD && !fAD)		// AD > BC
				return gen.Precision != GenDate.PrecisionType.Before;
			if (!gen.IsAD && fAD)		// BC < AD
				return false;
			if (!gen.IsAD && !fAD)		// Both BC
			{
				if (gen.Year > date.Year)
					return false;
				else if (gen.Year < date.Year)
					return gen.Precision != GenDate.PrecisionType.Before;
			}
			if (gen.IsAD && fAD)		// Both AD
			{
				if (gen.Year < date.Year)
					return false;
				else if (gen.Year > date.Year)
					return gen.Precision != GenDate.PrecisionType.Before;
			}
			if (gen.Month < date.Month)
			{
				return gen.Month == GenDate.UnknownMonth &&
					gen.Precision == GenDate.PrecisionType.After;
			}
			else if (gen.Month > date.Month)
			{
				return gen.Precision != GenDate.PrecisionType.Before;
			}
			if (gen.Day == GenDate.UnknownDay)
				return gen.Precision == GenDate.PrecisionType.After;
			return gen.Day >= date.Day && gen.Precision != GenDate.PrecisionType.Before;
		}

		private bool GenDateMightBeAfterDate(GenDate gen, DateTime date, bool fAD)
		{
			if (gen.IsAD && !fAD)		// AD > BC
				return true;
			if (!gen.IsAD && fAD)		// BC < AD
				return gen.Precision == GenDate.PrecisionType.After;
			if (!gen.IsAD && !fAD)		// Both BC
			{
				if (gen.Year > date.Year)
					return gen.Precision == GenDate.PrecisionType.After;
				else if (gen.Year < date.Year)
					return true;
			}
			if (gen.IsAD && fAD)		// Both AD
			{
				if (gen.Year < date.Year)
					return gen.Precision == GenDate.PrecisionType.After;
				else if (gen.Year > date.Year)
					return true;
			}
			if (gen.Month < date.Month)
			{
				return gen.Month == GenDate.UnknownMonth &&
					gen.Precision != GenDate.PrecisionType.Before;
			}
			else if (gen.Month > date.Month)
			{
				return true;
			}
			if (gen.Day == GenDate.UnknownDay)
				return gen.Precision != GenDate.PrecisionType.Before;
			else if (gen.Day == date.Day)
				return gen.Precision != GenDate.PrecisionType.Before;
			else
				return gen.Day > date.Day || gen.Precision == GenDate.PrecisionType.After;
		}

		private bool GenDateIsInRange(GenDate gen)
		{
			return GenDateIsAfterDate(gen, m_start, IsStartAD) &&
				GenDateIsBeforeDate(gen, m_end, IsEndAD);
		}

		public override bool SameMatcher(IMatcher other)
		{
			DateTimeMatcher dtOther = other as DateTimeMatcher;
			if (dtOther == null)
				return false;
			return m_end == dtOther.m_end &&
				m_start == dtOther.m_start &&
				m_type == dtOther.m_type &&
				HandleGenDate == dtOther.HandleGenDate &&
				(HandleGenDate ? (IsStartAD == dtOther.IsStartAD && IsEndAD == dtOther.IsEndAD &&
					UnspecificMatching == dtOther.UnspecificMatching) : true);
		}

		public override void PersistAsXml(System.Xml.XmlNode node)
		{
			base.PersistAsXml(node);
			XmlUtils.AppendAttribute(node, "start", m_start.ToString("s", DateTimeFormatInfo.InvariantInfo));
			XmlUtils.AppendAttribute(node, "end", m_end.ToString("s", DateTimeFormatInfo.InvariantInfo));
			XmlUtils.AppendAttribute(node, "type", ((int)m_type).ToString());
			XmlUtils.AppendAttribute(node, "genDate", HandleGenDate.ToString());
			if (HandleGenDate)
			{
				XmlUtils.AppendAttribute(node, "startAD", IsStartAD.ToString());
				XmlUtils.AppendAttribute(node, "endAD", IsEndAD.ToString());
				XmlUtils.AppendAttribute(node, "unspecific", UnspecificMatching.ToString());
			}
		}

		public override void InitXml(System.Xml.XmlNode node)
		{
			base.InitXml(node);
			m_start = DateTime.Parse(XmlUtils.GetManditoryAttributeValue(node, "start"), DateTimeFormatInfo.InvariantInfo);
			m_end = DateTime.Parse(XmlUtils.GetManditoryAttributeValue(node, "end"), DateTimeFormatInfo.InvariantInfo);
			m_type = (DateMatchType)XmlUtils.GetMandatoryIntegerAttributeValue(node, "type");
			HandleGenDate = XmlUtils.GetOptionalBooleanAttributeValue(node, "genDate", false);
			IsStartAD = XmlUtils.GetOptionalBooleanAttributeValue(node, "startAD", true);
			IsEndAD = XmlUtils.GetOptionalBooleanAttributeValue(node, "endAD", true);
			UnspecificMatching = XmlUtils.GetOptionalBooleanAttributeValue(node, "unspecific", false);
		}

		/// <summary>
		/// Enumeration to indicate the three ways we can compare dates.
		/// </summary>
		public enum DateMatchType
		{
			On,
			Range,
			Before,
			After,
			NotRange
		}
	}

}

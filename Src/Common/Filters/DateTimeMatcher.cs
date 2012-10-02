using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using SIL.Utils;

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

		public override bool Matches(SIL.FieldWorks.Common.COMInterfaces.ITsString arg)
		{
			string text = arg.Text;
			if (String.IsNullOrEmpty(text))
				return false;
			try
			{
				DateTime time = DateTime.Parse(text);
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
			catch (FormatException)
			{
				return false; // an invalid date just doesn't match.
			}
			return false; // to make the compiler happy, or in case we somehow don't have a valid type
		}

		public override bool SameMatcher(IMatcher other)
		{
			DateTimeMatcher dtOther = other as DateTimeMatcher;
			if (dtOther == null)
				return false;
			return m_end == dtOther.m_end && m_start == dtOther.m_start && m_type == dtOther.m_type;
		}

		public override void PersistAsXml(System.Xml.XmlNode node)
		{
			base.PersistAsXml(node);
			XmlUtils.AppendAttribute(node, "start", m_start.ToString("s", DateTimeFormatInfo.InvariantInfo));
			XmlUtils.AppendAttribute(node, "end", m_end.ToString("s", DateTimeFormatInfo.InvariantInfo));
			XmlUtils.AppendAttribute(node, "type", ((int)m_type).ToString() );
		}

		public override void InitXml(System.Xml.XmlNode node)
		{
			base.InitXml(node);
			m_start = DateTime.Parse(XmlUtils.GetManditoryAttributeValue(node, "start"), DateTimeFormatInfo.InvariantInfo);
			m_end = DateTime.Parse(XmlUtils.GetManditoryAttributeValue(node, "end"), DateTimeFormatInfo.InvariantInfo);
			m_type = (DateMatchType)XmlUtils.GetMandatoryIntegerAttributeValue(node, "type");
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

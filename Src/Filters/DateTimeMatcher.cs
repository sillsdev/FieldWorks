// Copyright (c) 2006-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Globalization;
using System.Xml.Linq;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.Xml;

namespace SIL.FieldWorks.Filters
{
	public class DateTimeMatcher : BaseMatcher
	{
		/// <summary>
		/// Default constructor for IPersistAsXml
		/// </summary>
		public DateTimeMatcher()
		{
		}

		public DateTimeMatcher(DateTime start, DateTime end, DateMatchType type)
		{
			Start = start;
			End = end;
			MatchType = type;
			IsStartAD = true;
			IsEndAD = true;
			UnspecificMatching = false;
		}

		/// <summary>
		/// The start time (used for start of range and not range, on or after)
		/// </summary>
		public DateTime Start { get; private set; }

		public DateMatchType MatchType { get; private set; }

		/// <summary>
		/// The end time (used for end of range and not range, on or before)
		/// </summary>
		public DateTime End { get; private set; }

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
			var text = arg.Text;
			if (string.IsNullOrEmpty(text))
			{
				return false;
			}
			DateTime time;
			GenDate gen;
			if (!HandleGenDate && DateTime.TryParse(text, out time))
			{
				switch (MatchType)
				{
					case DateMatchType.After:
						return time >= Start;
					case DateMatchType.Before:
						return time <= End;
					case DateMatchType.Range:
					case DateMatchType.On:
						return time >= Start && time <= End;
					case DateMatchType.NotRange:
						return time < Start || time > End;
				}
			}
			else if (HandleGenDate && GenDate.TryParse(text, out gen))
			{
				switch (MatchType)
				{
					case DateMatchType.After:
						return GenDateIsAfterDate(gen, Start, IsStartAD);
					case DateMatchType.Before:
						return GenDateIsBeforeDate(gen, End, IsEndAD);
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
			{
				return GenDateMightBeBeforeDate(gen, date, fAD);
			}
			if (gen.IsAD && !fAD) // AD > BC
			{
				return false;
			}
			if (!gen.IsAD && fAD) // BC < AD
			{
				return gen.Precision != GenDate.PrecisionType.After;
			}
			if (!gen.IsAD && !fAD)		// Both BC
			{
				if (gen.Year > date.Year)
				{
					return gen.Precision != GenDate.PrecisionType.After;
				}
				if (gen.Year < date.Year)
				{
					return false;
				}
			}
			if (gen.IsAD && fAD)		// Both AD
			{
				if (gen.Year < date.Year)
				{
					return gen.Precision != GenDate.PrecisionType.After;
				}
				if (gen.Year > date.Year)
				{
					return false;
				}
			}
			if (gen.Month < date.Month)
			{
				return gen.Month != GenDate.UnknownMonth || gen.Precision == GenDate.PrecisionType.Before;
			}
			if (gen.Month > date.Month)
			{
				return false;
			}
			if (gen.Day == GenDate.UnknownDay)
			{
				return gen.Precision == GenDate.PrecisionType.Before;
			}
			return gen.Day <= date.Day && gen.Precision != GenDate.PrecisionType.After;
		}

		private bool GenDateMightBeBeforeDate(GenDate gen, DateTime date, bool fAD)
		{
			if (gen.IsAD && !fAD) // AD > BC
			{
				return gen.Precision == GenDate.PrecisionType.Before;
			}
			if (!gen.IsAD && fAD) // BC < AD
			{
				return true;
			}
			if (!gen.IsAD && !fAD)		// Both BC
			{
				if (gen.Year > date.Year)
				{
					return true;
				}
				if (gen.Year < date.Year)
				{
					return gen.Precision == GenDate.PrecisionType.Before;
				}
			}
			if (gen.IsAD && fAD)		// Both AD
			{
				if (gen.Year < date.Year)
				{
					return true;
				}
				if (gen.Year > date.Year)
				{
					return gen.Precision == GenDate.PrecisionType.Before;
				}
			}
			if (gen.Month < date.Month)
			{
				return gen.Month != GenDate.UnknownMonth || gen.Precision != GenDate.PrecisionType.After;
			}
			if (gen.Month > date.Month)
			{
				return gen.Precision == GenDate.PrecisionType.Before;
			}
			return gen.Day <= date.Day || gen.Precision == GenDate.PrecisionType.Before;
		}

		private bool GenDateIsAfterDate(GenDate gen, DateTime date, bool fAD)
		{
			if (UnspecificMatching)
			{
				return GenDateMightBeAfterDate(gen, date, fAD);
			}
			if (gen.IsAD && !fAD) // AD > BC
			{
				return gen.Precision != GenDate.PrecisionType.Before;
			}
			if (!gen.IsAD && fAD) // BC < AD
			{
				return false;
			}
			if (!gen.IsAD && !fAD)		// Both BC
			{
				if (gen.Year > date.Year)
				{
					return false;
				}
				if (gen.Year < date.Year)
				{
					return gen.Precision != GenDate.PrecisionType.Before;
				}
			}
			if (gen.IsAD && fAD)		// Both AD
			{
				if (gen.Year < date.Year)
				{
					return false;
				}
				if (gen.Year > date.Year)
				{
					return gen.Precision != GenDate.PrecisionType.Before;
				}
			}
			if (gen.Month < date.Month)
			{
				return gen.Month == GenDate.UnknownMonth && gen.Precision == GenDate.PrecisionType.After;
			}
			if (gen.Month > date.Month)
			{
				return gen.Precision != GenDate.PrecisionType.Before;
			}
			if (gen.Day == GenDate.UnknownDay)
			{
				return gen.Precision == GenDate.PrecisionType.After;
			}
			return gen.Day >= date.Day && gen.Precision != GenDate.PrecisionType.Before;
		}

		private static bool GenDateMightBeAfterDate(GenDate gen, DateTime date, bool fAD)
		{
			if (gen.IsAD && !fAD) // AD > BC
			{
				return true;
			}
			if (!gen.IsAD && fAD) // BC < AD
			{
				return gen.Precision == GenDate.PrecisionType.After;
			}
			if (!gen.IsAD && !fAD)		// Both BC
			{
				if (gen.Year > date.Year)
				{
					return gen.Precision == GenDate.PrecisionType.After;
				}
				if (gen.Year < date.Year)
				{
					return true;
				}
			}
			if (gen.IsAD && fAD)		// Both AD
			{
				if (gen.Year < date.Year)
				{
					return gen.Precision == GenDate.PrecisionType.After;
				}
				if (gen.Year > date.Year)
				{
					return true;
				}
			}
			if (gen.Month < date.Month)
			{
				return gen.Month == GenDate.UnknownMonth && gen.Precision != GenDate.PrecisionType.Before;
			}
			if (gen.Month > date.Month)
			{
				return true;
			}
			if (gen.Day == GenDate.UnknownDay)
			{
				return gen.Precision != GenDate.PrecisionType.Before;
			}
			if (gen.Day == date.Day)
			{
				return gen.Precision != GenDate.PrecisionType.Before;
			}
			return gen.Day > date.Day || gen.Precision == GenDate.PrecisionType.After;
		}

		private bool GenDateIsInRange(GenDate gen)
		{
			return GenDateIsAfterDate(gen, Start, IsStartAD) && GenDateIsBeforeDate(gen, End, IsEndAD);
		}

		public override bool SameMatcher(IMatcher other)
		{
			var dtOther = other as DateTimeMatcher;
			if (dtOther == null)
			{
				return false;
			}
			return End == dtOther.End &&
				Start == dtOther.Start &&
				MatchType == dtOther.MatchType &&
				HandleGenDate == dtOther.HandleGenDate &&
				(!HandleGenDate || (IsStartAD == dtOther.IsStartAD && IsEndAD == dtOther.IsEndAD && UnspecificMatching == dtOther.UnspecificMatching));
		}

		public override void PersistAsXml(XElement node)
		{
			base.PersistAsXml(node);
			XmlUtils.SetAttribute(node, "start", Start.ToString("s", DateTimeFormatInfo.InvariantInfo));
			XmlUtils.SetAttribute(node, "end", End.ToString("s", DateTimeFormatInfo.InvariantInfo));
			XmlUtils.SetAttribute(node, "type", ((int)MatchType).ToString());
			XmlUtils.SetAttribute(node, "genDate", HandleGenDate.ToString());
			if (HandleGenDate)
			{
				XmlUtils.SetAttribute(node, "startAD", IsStartAD.ToString());
				XmlUtils.SetAttribute(node, "endAD", IsEndAD.ToString());
				XmlUtils.SetAttribute(node, "unspecific", UnspecificMatching.ToString());
			}
		}

		public override void InitXml(XElement node)
		{
			base.InitXml(node);
			Start = DateTime.Parse(XmlUtils.GetMandatoryAttributeValue(node, "start"), DateTimeFormatInfo.InvariantInfo);
			End = DateTime.Parse(XmlUtils.GetMandatoryAttributeValue(node, "end"), DateTimeFormatInfo.InvariantInfo);
			MatchType = (DateMatchType)XmlUtils.GetMandatoryIntegerAttributeValue(node, "type");
			HandleGenDate = XmlUtils.GetOptionalBooleanAttributeValue(node, "genDate", false);
			IsStartAD = XmlUtils.GetOptionalBooleanAttributeValue(node, "startAD", true);
			IsEndAD = XmlUtils.GetOptionalBooleanAttributeValue(node, "endAD", true);
			UnspecificMatching = XmlUtils.GetOptionalBooleanAttributeValue(node, "unspecific", false);
		}
	}
}

// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using System.Xml.Linq;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.Xml;

namespace LanguageExplorer.Filters
{
	/// <summary>
	/// A matcher that tests for integers in a specified range.
	/// </summary>
	internal sealed class RangeIntMatcher : BaseMatcher, IIntMatcher
	{
		/// <summary />
		internal RangeIntMatcher(long min, long max)
		{
			Min = min;
			Max = max;
		}

		/// <summary>
		/// Default constructor for IPersistAsXml
		/// </summary>
		public RangeIntMatcher()
		{
		}

		/// <summary>
		/// Gets the min.
		/// </summary>
		internal long Min { get; private set; }

		/// <summary>
		/// Gets the max.
		/// </summary>
		internal long Max { get; private set; }

		/// <summary>
		/// Persists as XML.
		/// </summary>
		public override void PersistAsXml(XElement element)
		{
			base.PersistAsXml(element);
			XmlUtils.SetAttribute(element, "min", Min.ToString());
			XmlUtils.SetAttribute(element, "max", Max.ToString());
		}

		/// <summary>
		/// Inits the XML.
		/// </summary>
		public override void InitXml(IPersistAsXmlFactory factory, XElement element)
		{
			base.InitXml(factory, element);
			Min = XmlUtils.GetMandatoryIntegerAttributeValue(element, "min");
			Max = XmlUtils.GetMandatoryIntegerAttributeValue(element, "max");
		}

		#region Matcher Members

		/// <summary>
		/// See whether the object passes. Note that the string may be null, which
		/// we interpret here as zero.
		/// </summary>
		public override bool Matches(ITsString stringval)
		{
			if (string.IsNullOrEmpty(stringval?.Text))
			{
				return false;
			}
			try
			{
				return stringval.Text.Split(' ').Select(long.Parse).Any(x => x >= Min && x <= Max);
			}
			catch (OverflowException)
			{
				return false;
			}
		}

		/// <summary>
		/// True if it is the same class and member vars match.
		/// </summary>
		public override bool SameMatcher(IMatcher other)
		{
			return other is RangeIntMatcher rangeIntMatcher && rangeIntMatcher.Min == Min && rangeIntMatcher.Max == Max;
		}
		#endregion
	}
}
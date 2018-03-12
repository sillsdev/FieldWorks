// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using System.Xml.Linq;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.Xml;

namespace  SIL.FieldWorks.Filters
{
	/// <summary>
	/// An abstract class that currently just serves to group matchers that deal with integers.
	/// </summary>
	public abstract class IntMatcher : BaseMatcher
	{
	}

	/// <summary>
	/// A matcher that tests for integers in a specified range.
	/// </summary>
	public class RangeIntMatcher : IntMatcher
	{
		/// <summary>
		/// Create one.
		/// </summary>
		public RangeIntMatcher(long min, long max)
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
		public long Min { get; private set; }

		/// <summary>
		/// Gets the max.
		/// </summary>
		public long Max { get; private set; }

		/// <summary>
		/// Persists as XML.
		/// </summary>
		public override void PersistAsXml(XElement node)
		{
			base.PersistAsXml (node);
			XmlUtils.SetAttribute(node, "min", Min.ToString());
			XmlUtils.SetAttribute(node, "max", Max.ToString());
		}

		/// <summary>
		/// Inits the XML.
		/// </summary>
		public override void InitXml(XElement node)
		{
			base.InitXml (node);
			Min = XmlUtils.GetMandatoryIntegerAttributeValue(node, "min");
			Max = XmlUtils.GetMandatoryIntegerAttributeValue(node, "max");
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
				var values = stringval.Text.Split(' ').Select(s => long.Parse(s));
				return values.Any(x => x >= Min && x <= Max);
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
			var other2 = other as RangeIntMatcher;
			if (other2 == null)
			{
				return false;
			}
			return other2.Min == Min && other2.Max == Max;
		}

		#endregion

	}

	/// <summary>
	/// A matcher that passes if the string, interpreted as an integer base 10, is not equal
	/// to the argument.
	/// </summary>
	public class NotEqualIntMatcher : IntMatcher
	{
		/// <summary>
		/// Get the value to not match. Used for testing.
		/// </summary>
		public int NotEqualValue { get; private set; }

		/// <summary>
		/// normal constructor.
		/// </summary>
		public NotEqualIntMatcher(int val)
		{
			NotEqualValue = val;
		}

		/// <summary>
		/// default constructor for persistence
		/// </summary>
		public NotEqualIntMatcher()
		{
		}

		#region IMatcher Members

		/// <summary>
		/// Matcheses the specified stringval.
		/// </summary>
		public override bool Matches(ITsString stringval)
		{
			return stringval != null && int.Parse(stringval.Text) != NotEqualValue;
		}
		/// <summary>
		/// True if it is the same class and member vars match.
		/// </summary>
		public override bool SameMatcher(IMatcher other)
		{
			var other2 = other as NotEqualIntMatcher;
			return other2 != null && other2.NotEqualValue == NotEqualValue;
		}

		#endregion

		/// <summary>
		/// Persists as XML.
		/// </summary>
		public override void PersistAsXml(XElement node)
		{
			base.PersistAsXml (node);
			XmlUtils.SetAttribute(node, "val", NotEqualValue.ToString());
		}

		/// <summary>
		/// Inits the XML.
		/// </summary>
		public override void InitXml(XElement node)
		{
			base.InitXml (node);
			NotEqualValue = XmlUtils.GetMandatoryIntegerAttributeValue(node, "val");
		}
	}
}

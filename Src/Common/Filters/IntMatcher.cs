#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
using System;

using SIL.Utils;
using SIL.FieldWorks.Common.COMInterfaces;

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
		int m_min;
		int m_max;

		/// <summary>
		/// Create one.
		/// </summary>
		/// <param name="min"></param>
		/// <param name="max"></param>
		public RangeIntMatcher(int min, int max)
		{
			m_min = min;
			m_max = max;
		}

		/// <summary>
		/// Default constructor for IPersistAsXml
		/// </summary>
		public RangeIntMatcher()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the min.
		/// </summary>
		/// <value>The min.</value>
		/// ------------------------------------------------------------------------------------
		public int Min
		{
			get {return m_min; }
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the max.
		/// </summary>
		/// <value>The max.</value>
		/// ------------------------------------------------------------------------------------
		public int Max
		{
			get { return m_max; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Persists as XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------
		public override void PersistAsXml(System.Xml.XmlNode node)
		{
			base.PersistAsXml (node);
			XmlUtils.AppendAttribute(node, "min", m_min.ToString());
			XmlUtils.AppendAttribute(node, "max", m_max.ToString());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inits the XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------
		public override void InitXml(System.Xml.XmlNode node)
		{
			base.InitXml (node);
			m_min = XmlUtils.GetMandatoryIntegerAttributeValue(node, "min");
			m_max = XmlUtils.GetMandatoryIntegerAttributeValue(node, "max");
		}

		#region Matcher Members

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// See whether the object passes. Note that the string may be null, which
		/// we interpret here as zero.
		/// </summary>
		/// <param name="stringval">The stringval.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------------
		public override bool Matches(ITsString stringval)
		{
			if (stringval == null || String.IsNullOrEmpty(stringval.Text))
				return false;
			int val = Int32.Parse(stringval.Text);
			return val >= m_min && val <= m_max;
		}

		/// <summary>
		/// True if it is the same class and member vars match.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public override bool SameMatcher(IMatcher other)
		{
			RangeIntMatcher other2 = other as RangeIntMatcher;
			if (other2 == null)
				return false;
			return other2.m_min == m_min && other2.m_max == m_max;
		}

		#endregion

	}

	/// <summary>
	/// A matcher that passes if the string, interpreted as an integer base 10, is not equal
	/// to the argument.
	/// </summary>
	public class NotEqualIntMatcher : IntMatcher
	{
		int m_val;

		/// <summary>
		/// Get the value to not match. Used for testing.
		/// </summary>
		public int NotEqualValue
		{
			get { return m_val; }
		}

		/// <summary>
		/// normal constructor.
		/// </summary>
		/// <param name="val"></param>
		public NotEqualIntMatcher(int val)
		{
			m_val = val;
		}

		/// <summary>
		/// default constructor for persistence
		/// </summary>
		public NotEqualIntMatcher()
		{
		}
		#region IMatcher Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Matcheses the specified stringval.
		/// </summary>
		/// <param name="stringval">The stringval.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override bool Matches(ITsString stringval)
		{
			if (stringval == null)
				return false;
			return Int32.Parse(stringval.Text) != m_val;
		}
		/// <summary>
		/// True if it is the same class and member vars match.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public override bool SameMatcher(IMatcher other)
		{
			NotEqualIntMatcher other2 = other as NotEqualIntMatcher;
			if (other2 == null)
				return false;
			return other2.m_val == m_val;
		}

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Persists as XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------
		public override void PersistAsXml(System.Xml.XmlNode node)
		{
			base.PersistAsXml (node);
			XmlUtils.AppendAttribute(node, "val", m_val.ToString());
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Inits the XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------------
		public override void InitXml(System.Xml.XmlNode node)
		{
			base.InitXml (node);
			m_val = XmlUtils.GetMandatoryIntegerAttributeValue(node, "val");
		}

	}

}

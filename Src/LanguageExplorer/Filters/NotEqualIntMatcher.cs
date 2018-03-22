// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Linq;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.Xml;

namespace LanguageExplorer.Filters
{
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
		/// Matches the specified stringval.
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
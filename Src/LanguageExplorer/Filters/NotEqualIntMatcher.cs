// Copyright (c) 2004-2020 SIL International
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
	internal sealed class NotEqualIntMatcher : BaseMatcher, IIntMatcher
	{
		/// <summary>
		/// For use with IPersistAsXml
		/// </summary>
		internal NotEqualIntMatcher(XElement element)
			:base(element)
		{
			NotEqualValue = XmlUtils.GetMandatoryIntegerAttributeValue(element, "val");
		}

		/// <summary>
		/// Get the value to not match. Used for testing.
		/// </summary>
		internal int NotEqualValue { get; }

		/// <summary />
		internal NotEqualIntMatcher(int val)
		{
			NotEqualValue = val;
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
			return other is NotEqualIntMatcher notEqualIntMatcher && notEqualIntMatcher.NotEqualValue == NotEqualValue;
		}

		#endregion

		/// <summary>
		/// Persists as XML.
		/// </summary>
		public override void PersistAsXml(XElement element)
		{
			base.PersistAsXml(element);
			XmlUtils.SetAttribute(element, "val", NotEqualValue.ToString());
		}
	}
}
// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Linq;
using SIL.FieldWorks.Common.ViewsInterfaces;

namespace LanguageExplorer.Filters
{
	/// <summary>
	/// Matches if the pattern is exactly the argument.
	/// </summary>
	internal sealed class ExactMatcher : SimpleStringMatcher
	{
		/// <summary />
		internal ExactMatcher(IVwPattern pattern) : base(pattern) { }

		/// <summary>
		/// For use with IPersistAsXml
		/// </summary>
		internal ExactMatcher(XElement element)
			: base(element)
		{ }

		protected override bool CurrentResultDoesMatch(MatchRangePair match)
		{
			return match.IchMin == 0 && match.IchLim == m_tssSource.Length;
		}
	}
}
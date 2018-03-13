// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.ViewsInterfaces;

namespace SIL.FieldWorks.Filters
{
	/// <summary>
	/// Matches if the pattern is exactly the argument.
	/// </summary>
	public class ExactMatcher : SimpleStringMatcher
	{
		/// <summary>
		/// normal constructor
		/// </summary>
		/// <param name="pattern"></param>
		public ExactMatcher(IVwPattern pattern) : base(pattern) {}

		/// <summary>
		/// default for persistence
		/// </summary>
		public ExactMatcher() {}

		protected override bool CurrentResultDoesMatch(MatchRangePair match)
		{
			return match.IchMin == 0 && match.IchLim == m_tssSource.Length;
		}
	}
}
// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Filters
{
	/// <summary>
	/// Matches if the pattern occurs at the end of the argument
	/// </summary>
	public class EndMatcher : SimpleStringMatcher
	{
		/// <summary>
		/// normal constructor
		/// </summary>
		public EndMatcher(IVwPattern pattern) : base(pattern) {}

		/// <summary>
		/// default for persistence
		/// </summary>
		public EndMatcher() {}

		/// <summary />
		public override bool Matches(ITsString arg)
		{
			return arg != null && arg.Length >= Pattern.Pattern.Length && base.Matches(arg);
		}

		protected override bool CurrentResultDoesMatch(MatchRangePair match)
		{
			return match.IchLim == m_tssSource.Length;
		}
	}
}
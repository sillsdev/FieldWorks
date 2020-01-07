// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Filters
{
	/// <summary>
	/// Matches if the pattern occurs anywhere in the argument
	/// </summary>
	public class AnywhereMatcher : SimpleStringMatcher
	{
		/// <summary>
		/// normal constructor
		/// </summary>
		public AnywhereMatcher(IVwPattern pattern) : base(pattern) { }

		/// <summary>
		/// default for persistence
		/// </summary>
		public AnywhereMatcher() { }

		/// <summary />
		public override bool Matches(ITsString arg)
		{
			return arg != null && base.Matches(arg);
		}

		protected override bool CurrentResultDoesMatch(MatchRangePair match)
		{
			return match.IchMin >= 0;
		}
	}
}
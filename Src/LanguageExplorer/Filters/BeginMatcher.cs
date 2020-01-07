// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Filters
{
	/// <summary>
	/// Matches if the pattern occurs at the start of the argument
	/// </summary>
	public class BeginMatcher : SimpleStringMatcher
	{
		/// <summary />
		public BeginMatcher(IVwPattern pattern) : base(pattern) { }

		/// <summary>
		/// default for persistence
		/// </summary>
		public BeginMatcher() { }

		/// <summary />
		public override bool Matches(ITsString arg)
		{
			return arg != null && arg.Length >= Pattern.Pattern.Length && base.Matches(arg);
		}

		protected override bool CurrentResultDoesMatch(MatchRangePair match)
		{
			return match.IchMin == 0;
		}
	}
}
// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.Filters
{
	/// <summary>
	/// Like the base class, but match ignores case.
	/// </summary>
	public class ExactCaseInsensitiveLiteralMatcher : ExactLiteralMatcher
	{
		public ExactCaseInsensitiveLiteralMatcher(string target, int ws)
			: base(target.ToLower(), ws)
		{
		}

		internal override bool MatchText(string p)
		{
			return base.MatchText(p.ToLower());
		}
	}
}
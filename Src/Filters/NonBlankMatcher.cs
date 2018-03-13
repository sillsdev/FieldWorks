// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using SIL.LCModel.Core.KernelInterfaces;

namespace SIL.FieldWorks.Filters
{
	/// <summary>
	/// Matches non-blanks.
	/// </summary>
	public class NonBlankMatcher : BaseMatcher
	{
		/// <summary>
		/// The exact opposite of BlankMatcher.
		/// </summary>
		public override bool Matches(ITsString arg)
		{
			if (arg == null || arg.Length == 0)
			{
				return false;
			}
			return arg.Text.Any(t => !char.IsWhiteSpace(t));
		}
		/// <summary>
		/// True if it is the same class and member vars match.
		/// </summary>
		public override bool SameMatcher(IMatcher other)
		{
			return other is NonBlankMatcher;
		}
	}
}
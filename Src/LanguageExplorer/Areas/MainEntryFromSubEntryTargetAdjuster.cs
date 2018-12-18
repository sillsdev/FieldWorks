// Copyright (c) 2003-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using SIL.LCModel;

namespace LanguageExplorer.Areas
{
	/// <summary>
	/// If the initial target is a subentry replace it with the appropriate top-level entry.
	/// </summary>
	internal class MainEntryFromSubEntryTargetAdjuster : IPreferedTargetAdjuster
	{
		public ICmObject AdjustTarget(ICmObject firstMatch)
		{
			if (!(firstMatch is ILexEntry))
			{
				return firstMatch; // by default change nothing.
			}
			var subentry = (ILexEntry)firstMatch;
			var componentsEntryRef = subentry.EntryRefsOS.FirstOrDefault(se => se.RefType == LexEntryRefTags.krtComplexForm);
			var root = componentsEntryRef?.PrimaryEntryRoots.FirstOrDefault();
			return root ?? firstMatch;
		}
	}
}
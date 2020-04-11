// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using SIL.LCModel;

namespace LanguageExplorer.Controls
{
	/// <summary>
	/// If the initial target is a subentry replace it with the appropriate top-level entry.
	/// </summary>
	internal sealed class MainEntryFromSubEntryTargetAdjuster : IPreferredTargetAdjuster
	{
		ICmObject IPreferredTargetAdjuster.AdjustTarget(ICmObject firstMatch)
		{
			return !(firstMatch is ILexEntry) ? firstMatch : ((ILexEntry)firstMatch).EntryRefsOS.FirstOrDefault(se => se.RefType == LexEntryRefTags.krtComplexForm)?.PrimaryEntryRoots.FirstOrDefault() ?? firstMatch;
		}
	}
}
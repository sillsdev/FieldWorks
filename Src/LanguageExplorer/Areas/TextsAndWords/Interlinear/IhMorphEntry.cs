// Copyright (c) 2006-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary>
	/// This class handles the MorphEntry line when there is a current entry. Currently it
	/// is very nearly the same.
	/// </summary>
	internal class IhMorphEntry : IhMissingEntry
	{
		internal IhMorphEntry(IHelpTopicProvider helpTopicProvider) : base(helpTopicProvider)
		{
		}

		internal override int WasReal()
		{
			return 1;
		}
	}
}
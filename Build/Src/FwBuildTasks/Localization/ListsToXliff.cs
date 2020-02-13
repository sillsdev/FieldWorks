// Copyright (c) 2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace SIL.FieldWorks.Build.Tasks.Localization
{
	/// <summary>
	/// Convert FieldWorks lists from XML to XLIFF and split into several files to facilitate localization.
	/// </summary>
	public class ListsToXliff : Task
	{
		/// <summary>The XML file containing all FieldWorks list data</summary>
		[Required]
		public string SourceXml { get; set; }

		/// <summary>The directory where XLIFF files should be saved</summary>
		[Required]
		public string XliffOutputDir { get; set; }

		/// <summary>If specified, any strings in this locale will be included as 'final' translations</summary>
		public string TargetLocale { get; set; }

		public override bool Execute()
		{
			Log.LogMessage($"Converting '{SourceXml}' to XLIFF at '{XliffOutputDir}'.");
			if (string.IsNullOrWhiteSpace(TargetLocale))
			{
				TargetLocale = null;
			}
			LocalizeLists.SplitSourceLists(SourceXml, XliffOutputDir, TargetLocale);
			return true;
		}
	}
}

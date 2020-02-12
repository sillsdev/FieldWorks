// Copyright (c) 2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace SIL.FieldWorks.Build.Tasks.Localization
{
	/// <summary>
	/// Combine translated FieldWorks lists from XLIFF files into a single XML file.
	/// </summary>
	public class XliffToLists : Task
	{
		/// <summary>List of XLIFF files containing lists to be converted to XML</summary>
		[Required]
		public ITaskItem[] XliffSourceFiles { get; set; }

		/// <summary>XML file where translated lists should be saved</summary>
		[Required]
		public string OutputXml { get; set; }

		public override bool Execute()
		{
			Log.LogMessage($"Combining lists from {XliffSourceFiles.Length} XLIFF files into '{OutputXml}'.");
			LocalizeLists.CombineXliffFiles(XliffSourceFiles.Select(i => i.ItemSpec).ToList(), OutputXml);
			return true;
		}
	}
}

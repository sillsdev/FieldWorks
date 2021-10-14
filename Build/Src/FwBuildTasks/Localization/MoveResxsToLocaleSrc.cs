// Copyright (c) 2021 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Newtonsoft.Json;

namespace SIL.FieldWorks.Build.Tasks.Localization
{
	/// <summary>
	/// When we download translated resx files from Crowdin, they are under %original_path%, so they will be under
	/// $(fwrt)/Localizations/l10ns/(locale)/(Crowdin-Branch-Name)?/Localizations/LCM/ or
	/// $(fwrt)/Localizations/l10ns/(locale)/(Crowdin-Branch-Name)?/Src. <see cref="LocalizeFieldWorks"/> expects to find them under
	/// $(fwrt)/Localizations/l10ns/(locale)/Src (or src for LCM; the different case matters on Linux).
	///
	/// This task will move the files from where Crowdin puts them to where <see cref="LocalizeFieldWorks"/> expects them.
	/// </summary>
	public class MoveResxsToLocaleSrc : Task
	{
		/// <summary>The directory containing localizations for one locale</summary>
		[Required]
		public string LocaleDirectory { get; set; }

		/// <summary>The crowdin.json file used to up- and download translations</summary>
		public string CrowdinJson { get; set; }

		internal static string FwDirInCrowdin => "Src";
		internal static string LcmDirInCrowdin => Path.Combine("Localizations", "LCM");

		public override bool Execute()
		{
			var fwPartialPath = FwDirInCrowdin;
			var lcmPartialPath = LcmDirInCrowdin;
			var branch = GetCrowdinBranch();
			if (!string.IsNullOrEmpty(branch))
			{
				fwPartialPath = Path.Combine(branch, fwPartialPath);
				lcmPartialPath = Path.Combine(branch, lcmPartialPath);
			}
			else
			{
				branch = null;
			}

			// Check whether FW localizations were downloaded; move them if needed
			var fwSrcDir = new DirectoryInfo(Path.Combine(LocaleDirectory, fwPartialPath));
			var fwResxCount = CountResxs(fwSrcDir);
			if (fwResxCount != 0 && branch != null)
			{
				var destDir = Path.Combine(LocaleDirectory, "Src");
				Log.LogMessage(MessageImportance.Normal, $"Moving {fwResxCount} .resx files from {fwSrcDir.FullName} to {destDir}");
				fwSrcDir.MoveTo(destDir);
			}

			// Check whether LCM localizations were downloaded; move them to src
			var lcmSrcDir = new DirectoryInfo(Path.Combine(LocaleDirectory, lcmPartialPath));
			var lcmResxCount = CountResxs(lcmSrcDir);
			if (lcmResxCount == 0)
			{
				return false;
			}
			var lcmDestDir = Path.Combine(LocaleDirectory, "src");
			Log.LogMessage(MessageImportance.Normal, $"Moving {lcmResxCount} .resx files from {lcmSrcDir.FullName} to {lcmDestDir}");
			// Because src may not already exist (src and Src are different on Linux), create it
			Directory.CreateDirectory(lcmDestDir);
			// Because src may already exist (src and Src are the same on Windows), moving the whole directory at once will result in an exception.
			foreach (var file in lcmSrcDir.EnumerateFiles())
			{
				file.MoveTo(Path.Combine(lcmDestDir, file.Name));
			}
			foreach (var subDir in lcmSrcDir.EnumerateDirectories())
			{
				subDir.MoveTo(Path.Combine(lcmDestDir, subDir.Name));
			}

			return !Log.HasLoggedErrors;
		}

		private int CountResxs(DirectoryInfo dir)
		{
			if (!dir.Exists)
			{
				Log.LogError($"Localization directory not found ({dir.FullName})");
				return 0;
			}

			var count = dir.EnumerateFiles("*.resx", SearchOption.AllDirectories).Count();
			if (count == 0)
			{
				Log.LogError($"No localized resx files found in '{dir.FullName}'");
			}
			return count;
		}

		/// <remarks>
		/// If there is a crowdin.json file with a branch, include it in the path to the source localized resx file.
		/// LT-20831: Crowdin now includes the branch directory in %original_path%
		/// </remarks>
		internal string GetCrowdinBranch()
		{
			var crowdinObj = new {Branch = (string) null};
			return File.Exists(CrowdinJson) ? JsonConvert.DeserializeAnonymousType(File.ReadAllText(CrowdinJson), crowdinObj).Branch : null;
		}
	}
}
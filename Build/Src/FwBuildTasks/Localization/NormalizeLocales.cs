// Copyright (c) 2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace SIL.FieldWorks.Build.Tasks.Localization
{
	/// <summary>
	/// Crowdin includes country codes either always or never. Country codes are required for Chinese, but mostly get in the way for other languages.
	/// This task removes country codes from all locales except Chinese.
	/// </summary>
	public class NormalizeLocales : Task
	{
		/// <summary>The directory whose subdirectories are locale names and contain localizations</summary>
		[Required]
		public string L10nsDirectory { get; set; }

		public override bool Execute()
		{
			var locales = Directory.GetDirectories(L10nsDirectory).Select(Path.GetFileName);
			foreach (var locale in locales)
			{
				RenameLocale(locale, Normalize(locale));
			}
			return true;
		}

		/// <summary>
		/// Normalizes a locale in the form %language_code%-%region_code%, assuming the language is in the default region.
		/// That is, strip the region codes from all languages except Chinese (which must always have a region code).
		/// </summary>
		public static string Normalize(string locale)
		{
			return locale.Equals("zh-CN") ? locale : locale.Split('-')[0];
		}

		private void RenameLocale(string source, string dest)
		{
			if (source == dest)
			{
				Log.LogMessage($"Not normalizing '{source}'.");
				return;
			}

			var sourceDir = Path.Combine(L10nsDirectory, source);
			var destDir = Path.Combine(L10nsDirectory, dest);
			Directory.Move(sourceDir, destDir);

			foreach (var file in Directory.EnumerateFiles(destDir, "*", SearchOption.AllDirectories))
			{
				var nameNoExt = Path.GetFileNameWithoutExtension(file);
				// ReSharper disable once PossibleNullReferenceException - no files are null
				if (nameNoExt.EndsWith(source))
				{
					var lengthToKeep = nameNoExt.Length - source.Length;
					var dir = Path.GetDirectoryName(file);
					var ext = Path.GetExtension(file);
					// ReSharper disable once AssignNullToNotNullAttribute - no files are null
					var newName = Path.Combine(dir, $"{nameNoExt.Substring(0, lengthToKeep)}{dest}{ext}");
					File.Move(file, newName);
				}
			}
		}
	}
}

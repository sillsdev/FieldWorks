// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System.IO;
using System.Text;
using SIL.LCModel;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// The one-time, per-project mapping from the template vocabulary's "ipa" and
	/// "transliteration" roles to this project's actual vernacular writing systems (identified by
	/// ICU code, e.g. "sil-fonipa"). Configured once outside the template editor (LT-22712);
	/// unset roles simply mean the corresponding words_*/morphemes_* placeholders have no data
	/// and are dropped per InterlinearTemplateResolver's empty-line rule.
	/// </summary>
	public sealed class InterlinearTemplateWritingSystemMapping
	{
		public string IpaIcuCode { get; set; }
		public string TransliterationIcuCode { get; set; }

		private const string FileName = "LatexInterlinearTemplateWritingSystems.txt";

		public static string GetPath(string projectFolder)
		{
			return Path.Combine(LcmFileHelper.GetConfigSettingsDir(projectFolder), FileName);
		}

		/// <summary>An empty mapping (both roles unset) if the project has never configured
		/// one.</summary>
		public static InterlinearTemplateWritingSystemMapping Load(string projectFolder)
		{
			var path = GetPath(projectFolder);
			if (!File.Exists(path))
				return new InterlinearTemplateWritingSystemMapping();

			var lines = File.ReadAllLines(path, Encoding.UTF8);
			return new InterlinearTemplateWritingSystemMapping
			{
				IpaIcuCode = lines.Length > 0 ? EmptyToNull(lines[0]) : null,
				TransliterationIcuCode = lines.Length > 1 ? EmptyToNull(lines[1]) : null
			};
		}

		public void Save(string projectFolder)
		{
			var path = GetPath(projectFolder);
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			File.WriteAllLines(path, new[] { IpaIcuCode ?? string.Empty, TransliterationIcuCode ?? string.Empty }, new UTF8Encoding(false));
		}

		private static string EmptyToNull(string s)
		{
			return string.IsNullOrEmpty(s) ? null : s;
		}
	}
}

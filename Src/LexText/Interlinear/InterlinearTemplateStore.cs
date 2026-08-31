// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System.IO;
using System.Text;
using SIL.LCModel;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// Loads and saves the one "LaTeX Interlinear for Publication" template a project may have,
	/// under the same per-project ConfigurationSettings precedent DictionaryConfigurationModel
	/// uses. The saved file is plain text (the template as the user wrote it) so it stays
	/// copy/paste-portable -- no wrapping XML, no project-specific IDs.
	/// </summary>
	public static class InterlinearTemplateStore
	{
		private const string FileName = "LatexInterlinearTemplate.txt";
		private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

		public static string GetPath(string projectFolder)
		{
			return Path.Combine(LcmFileHelper.GetConfigSettingsDir(projectFolder), FileName);
		}

		public static bool HasSavedTemplate(string projectFolder)
		{
			return File.Exists(GetPath(projectFolder));
		}

		/// <summary>The project's saved template, or the built-in default if it has never saved
		/// one.</summary>
		public static string Load(string projectFolder)
		{
			var path = GetPath(projectFolder);
			return File.Exists(path) ? File.ReadAllText(path, Encoding.UTF8) : InterlinearTemplateDefault.Text;
		}

		public static void Save(string projectFolder, string templateText)
		{
			var path = GetPath(projectFolder);
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			File.WriteAllText(path, templateText, Utf8NoBom);
		}
	}
}

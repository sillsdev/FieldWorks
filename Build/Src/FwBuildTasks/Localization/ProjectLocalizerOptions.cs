// Copyright (c) 2015-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using Microsoft.Build.Framework;

namespace SIL.FieldWorks.Build.Tasks.Localization
{
	public class ProjectLocalizerOptions: LocalizerOptions
	{
		public ProjectLocalizerOptions(Localizer localizer, LocalizerOptions options):
			base(options)
		{
			_localizer = localizer;
			Version = _localizer.Version;
			FileVersion = _localizer.FileVersion;
			InformationVersion = _localizer.InformationVersion;
			Locale = _localizer.Locale;
			CurrentLocaleDir = _localizer.CurrentLocaleDir;
		}

		public string Version { get; }
		public string FileVersion { get; }
		public new string InformationVersion { get; }
		public string Locale { get; }
		public string CurrentLocaleDir { get; }

		private readonly Localizer _localizer;

		/// <returns><c>true</c> if the localized string has errors; <c>false</c> if it looks good</returns>
		internal bool HasErrors(string filename, string localizedText, string originalText, string comment)
		{
			return _localizer.HasErrors(filename, localizedText, originalText, comment);
		}

		public void LogError(string message)
		{
			_localizer.LogError(message);
			LogMessage(MessageImportance.High, message);
		}

	}
}

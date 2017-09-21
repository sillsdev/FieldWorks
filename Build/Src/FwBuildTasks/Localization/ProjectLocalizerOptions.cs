// Copyright (c) 2015-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using FwBuildTasks;

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
		}

		public string Version { get; }
		public string FileVersion { get; }
		public string InformationVersion { get; }
		public string Locale { get; }

		private readonly Localizer _localizer;

		public void LogError(string message)
		{
			_localizer.LogError(message);
		}

	}
}

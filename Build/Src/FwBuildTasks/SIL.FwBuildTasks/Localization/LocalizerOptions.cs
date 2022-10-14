// Copyright (c) 2015-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using Microsoft.Build.Framework;

namespace SIL.FieldWorks.Build.Tasks.Localization
{
	public class LocalizerOptions
	{
		public LocalizerOptions(LocalizeFieldWorks localizeFieldWorksTask)
		{
			_LocalizeFieldWorksTask = localizeFieldWorksTask;
			SrcFolder = localizeFieldWorksTask.SrcFolder;
			RootDir = localizeFieldWorksTask.RootDirectory;
			OutputFolder = localizeFieldWorksTask.OutputFolder;
			AssemblyInfoPath = localizeFieldWorksTask.AssemblyInfoPath;
			Config = localizeFieldWorksTask.Config;
			BuildSource = localizeFieldWorksTask.BuildSource;
			BuildBinaries = localizeFieldWorksTask.BuildBinaries;
			CopyStringsXml = localizeFieldWorksTask.CopyStringsXml;
		}

		public LocalizerOptions(LocalizerOptions otherOptions)
		{
			_LocalizeFieldWorksTask = otherOptions._LocalizeFieldWorksTask;
			SrcFolder = otherOptions.SrcFolder;
			RootDir = otherOptions.RootDir;
			OutputFolder = otherOptions.OutputFolder;
			AssemblyInfoPath = otherOptions.AssemblyInfoPath;
			Config = otherOptions.Config;
			BuildSource = otherOptions.BuildSource;
			BuildBinaries = otherOptions.BuildBinaries;
			CopyStringsXml = otherOptions.CopyStringsXml;
		}

		protected readonly LocalizeFieldWorks _LocalizeFieldWorksTask;

		public string SrcFolder { get; }
		public string RootDir { get; }
		public string OutputFolder { get; }
		public string AssemblyInfoPath { get; }
		public string Config { get; }
		public bool BuildSource { get; }
		public bool BuildBinaries { get; }
		public bool CopyStringsXml { get; }

		/// <summary>
		/// The path where we expect to store a file like strings-es.xml for a given locale.
		/// </summary>
		internal string StringsXmlPath(string locale)
		{
			// ReSharper disable InconsistentlySynchronizedField - the lock is needed only to log.
			return _LocalizeFieldWorksTask.StringsXmlPath(locale);
			// ReSharper restore InconsistentlySynchronizedField
		}

		/// <summary>
		/// The path where we expect to find a file like strings-es.xml for a given locale.
		/// </summary>
		internal string StringsXmlSourcePath(string locale)
		{
			// ReSharper disable InconsistentlySynchronizedField - the lock is needed only to log.
			return _LocalizeFieldWorksTask.StringsXmlSourcePath(locale);
			// ReSharper restore InconsistentlySynchronizedField
		}

		internal void LogMessage(MessageImportance importance, string message,
			params object[] messageArgs)
		{
			lock (_LocalizeFieldWorksTask.SyncObj)
			{
				_LocalizeFieldWorksTask.Log.LogMessage(importance, message, messageArgs);
			}
		}
	}
}

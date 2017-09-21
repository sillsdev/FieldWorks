// Copyright (c) 2015-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using FwBuildTasks;
using Microsoft.Build.Framework;

namespace SIL.FieldWorks.Build.Tasks.Localization
{
	public class LocalizerOptions
	{
		public LocalizerOptions(LocalizeFieldWorks localizeFieldWorksTask)
		{
			_LocalizeFieldWorksTask = localizeFieldWorksTask;
			SrcFolder = localizeFieldWorksTask.SrcFolder;
			OutputFolder = localizeFieldWorksTask.OutputFolder;
			RealBldFolder = localizeFieldWorksTask.RealBldFolder;
			AssemblyInfoPath = localizeFieldWorksTask.AssemblyInfoPath;
			Config = localizeFieldWorksTask.Config;
			StringsEnPath = localizeFieldWorksTask.StringsEnPath;
			StringsXmlFolder = localizeFieldWorksTask.StringsXmlFolder;
			BuildSource = localizeFieldWorksTask.BuildSource;
			BuildBinaries = localizeFieldWorksTask.BuildBinaries;
		}

		public LocalizerOptions(LocalizerOptions otherOptions)
		{
			_LocalizeFieldWorksTask = otherOptions._LocalizeFieldWorksTask;
			SrcFolder = otherOptions.SrcFolder;
			OutputFolder = otherOptions.OutputFolder;
			RealBldFolder = otherOptions.RealBldFolder;
			AssemblyInfoPath = otherOptions.AssemblyInfoPath;
			Config = otherOptions.Config;
			StringsEnPath = otherOptions.StringsEnPath;
			StringsXmlFolder = otherOptions.StringsXmlFolder;
			BuildSource = otherOptions.BuildSource;
			BuildBinaries = otherOptions.BuildBinaries;
		}

		protected readonly LocalizeFieldWorks _LocalizeFieldWorksTask;

		public string SrcFolder { get; }
		public string OutputFolder { get; }
		public string RealBldFolder { get; }
		public string AssemblyInfoPath { get; }
		public string Config { get; }
		public string StringsEnPath { get; }
		public string StringsXmlFolder { get; }
		public bool BuildSource { get; }
		public bool BuildBinaries { get; }

		/// <summary>
		/// The path where wer expect to store a file like strings-es.xml for a given locale.
		/// </summary>
		/// <param name="locale"></param>
		/// <returns></returns>
		internal string StringsXmlPath(string locale)
		{
			return _LocalizeFieldWorksTask.StringsXmlPath(locale);
		}

		/// <summary>
		/// The path where we expect to store a temporary form of the .PO file for a particular locale,
		/// such as Output/es.xml.
		/// </summary>
		internal string XmlPoFilePath(string locale)
		{
			return _LocalizeFieldWorksTask.XmlPoFilePath(locale);
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

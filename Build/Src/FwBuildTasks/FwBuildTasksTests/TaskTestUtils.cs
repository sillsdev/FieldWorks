// Copyright (c) 2016-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.Build.Framework;

namespace FwBuildTasks
{
	internal static class TaskTestUtils
	{
		public static void RecreateDirectory(string path)
		{
			if (Directory.Exists(path))
			{
				SIL.TestUtilities.TestUtilities.DeleteFolderThatMayBeInUse(path);
			}
			Thread.Sleep(1000); // wait for the directory to finish being deleted
			Directory.CreateDirectory(path);
		}
	}

	internal class TestBuildEngine : IBuildEngine
	{
		public List<Tuple<MessageImportance, string>> Messages = new List<Tuple<MessageImportance, string>>();
		public List<string> Warnings = new List<string>();
		public List<string> Errors = new List<string>();

		public void LogErrorEvent(BuildErrorEventArgs e)
		{
			Errors.Add(e.Message);
		}

		public void LogWarningEvent(BuildWarningEventArgs e)
		{
			Warnings.Add(e.Message);
		}

		public void LogMessageEvent(BuildMessageEventArgs e)
		{
			Messages.Add(new Tuple<MessageImportance, string>(e.Importance, e.Message));
		}

		public void LogCustomEvent(CustomBuildEventArgs e)
		{
			throw new NotImplementedException();
		}

		public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs)
		{
			throw new NotImplementedException();
		}

		public bool ContinueOnError => true;
		public int LineNumberOfTaskNode => -1;
		public int ColumnNumberOfTaskNode => -2;
		public string ProjectFileOfTaskNode => "TestProjectFile";

	}
}

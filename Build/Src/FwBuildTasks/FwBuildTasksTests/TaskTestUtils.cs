// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Build.Framework;

namespace FwBuildTasks
{
	internal static class TaskTestUtils
	{
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

// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using Microsoft.Build.Framework;

namespace NUnitReport
{
	class StubBuildEngine : IBuildEngine
	{
		public LoggerVerbosity Verbosity { get; private set; }

		public StubBuildEngine(LoggerVerbosity verbosity)
		{
			Verbosity = verbosity;
		}

		public void LogErrorEvent(BuildErrorEventArgs e)
		{
			throw new NotImplementedException();
		}

		public void LogWarningEvent(BuildWarningEventArgs e)
		{
			throw new NotImplementedException();
		}

		public void LogMessageEvent(BuildMessageEventArgs e)
		{
			if (MessageIsImportantEnough(e.Importance))
			{
				Console.WriteLine(e.Message);
			}
		}

		private bool MessageIsImportantEnough(MessageImportance messageImportance)
		{
			if (messageImportance == MessageImportance.High)
				return true;
			if (messageImportance == MessageImportance.Normal &&
				(Verbosity == LoggerVerbosity.Normal || Verbosity == LoggerVerbosity.Detailed || Verbosity == LoggerVerbosity.Diagnostic))
			{
				return true;
			}
			if (messageImportance == MessageImportance.Low &&
				(Verbosity == LoggerVerbosity.Diagnostic || Verbosity == LoggerVerbosity.Detailed))
			{
				return true;
			}
			return false;
		}

		public void LogCustomEvent(CustomBuildEventArgs e)
		{
			throw new NotImplementedException();
		}

		public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs)
		{
			throw new NotImplementedException();
		}

		public bool ContinueOnError
		{
			get { return true; }
		}

		public int LineNumberOfTaskNode
		{
			get { throw new NotImplementedException(); }
		}

		public int ColumnNumberOfTaskNode
		{
			get { throw new NotImplementedException(); }
		}

		public string ProjectFileOfTaskNode
		{
			get { throw new NotImplementedException(); }
		}
	}
}

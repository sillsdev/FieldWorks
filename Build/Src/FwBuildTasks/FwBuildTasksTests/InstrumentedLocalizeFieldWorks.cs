// Copyright (c) 2015-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;

namespace FwBuildTasks
{
	internal class InstrumentedLocalizeFieldWorks : LocalizeFieldWorks
	{
		public string ErrorMessages = string.Empty;

		public InstrumentedLocalizeFieldWorks()
		{
			BuildEngine = new MockBuildEngine();
			LocalizerType = typeof(InstrumentedLocalizer);
		}

		/// <summary>
		/// In normal operation, this is the same as RootDirectory. In test, we find the real one, to allow us to
		/// find fixed files like LocalizeResx.xml
		/// </summary>
		internal override string RealFwRoot
		{
			get
			{
				var path = BuildUtils.GetAssemblyFolder();
				while (Path.GetFileName(path) != "Build")
					path = Path.GetDirectoryName(path);
				return Path.GetDirectoryName(path);
			}
		}

		internal override void LogError(string message)
		{
			ErrorMessages += Environment.NewLine + message;
		}
	}
}

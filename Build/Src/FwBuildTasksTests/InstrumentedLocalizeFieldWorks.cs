// Copyright (c) 2015-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.FieldWorks.Build.Tasks.Localization;

namespace SIL.FieldWorks.Build.Tasks.FwBuildTasksTests
{
	internal class InstrumentedLocalizeFieldWorks : LocalizeFieldWorks
	{
		public string ErrorMessages = string.Empty;

		public InstrumentedLocalizeFieldWorks()
		{
			BuildEngine = new MockBuildEngine();
			LocalizerType = typeof(InstrumentedLocalizer);
		}

		protected override void LogError(string message)
		{
			ErrorMessages += Environment.NewLine + message;
		}
	}
}

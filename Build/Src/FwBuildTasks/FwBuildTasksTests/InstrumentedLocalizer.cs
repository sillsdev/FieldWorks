// Copyright (c) 2015-2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Build.Tasks.Localization;

namespace SIL.FieldWorks.Build.Tasks.FwBuildTasksTests
{
	internal class InstrumentedLocalizer: Localization.Localizer
	{
		protected override ProjectLocalizer CreateProjectLocalizer(string folder, ProjectLocalizerOptions options)
		{
			return new InstrumentedProjectLocalizer(folder, options);
		}
	}
}

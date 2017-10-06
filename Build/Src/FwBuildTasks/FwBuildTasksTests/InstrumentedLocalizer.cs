// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using SIL.FieldWorks.Build.Tasks.Localization;

namespace FwBuildTasks
{
	internal class InstrumentedLocalizer: Localizer
	{
		protected override ProjectLocalizer CreateProjectLocalizer(string folder, ProjectLocalizerOptions options)
		{
			return new InstrumentedProjectLocalizer(folder, options);
		}
	}
}

// Copyright (c) 2012-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace SIL.FieldWorks.Build.Tasks
{
	public class GenerateFwTargets : Task
	{
		[Required]
		public string TimeoutValuesFilePath { get; set; }

		[Required]
		public string NUnitConsolePath { get; set; }

		public override bool Execute()
		{
			try
			{
				var gen = new CollectTargets(Log, TimeoutValuesFilePath, NUnitConsolePath);
				gen.Generate();
				return true;
			}
			catch (CollectTargets.StopTaskException)
			{
				return false;
			}
		}
	}
}
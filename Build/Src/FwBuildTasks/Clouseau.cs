// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using Microsoft.Build.Utilities;

namespace FwBuildTasks
{
	public class Clouseau : Task
	{
		public string Assembly { get; set; }

		public override bool Execute()
		{
			Log.LogMessage("Excuse me " + Assembly + "Does your dog bite?");
			return true;
		}
	}
}

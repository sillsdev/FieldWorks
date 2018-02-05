// Copyright (c) 2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using FwBuildTasks;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace SIL.FieldWorks.Build.Tasks
{
	/// <returns>The version numbers from the MasterVersionInfo.txt file</returns>
	public class ParseVersionNumbers : Task
	{
		[Required]
		public string VersionInfo { get; set; }

		[Output]
		public string Major { get; set; }

		[Output]
		public string Minor { get; set; }

		[Output]
		public string Revision { get; set; }

		[Output]
		public string Descriptor { get; set; }

		public override bool Execute()
		{
			Dictionary<string, string> symbols;
			if (BuildUtils.ParseSymbolFile(VersionInfo, Log, out symbols))
			{
				Major = symbols["FWMAJOR"];
				Minor = symbols["FWMINOR"];
				Revision = symbols["FWREVISION"];
				Descriptor = symbols["FWBETAVERSION"];
				return true;
			}
			return false;
		}
	}
}

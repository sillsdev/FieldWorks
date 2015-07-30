// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace GenerateFwTargets
{
	/// <summary>
	/// This program should be run (the executable located) in a top-level subdirectory of the
	/// FieldWorks repository tree.  It scans all the .csproj files it can find in the parallel
	/// Src directory, and writes a .targets file for the msbuild system.
	/// </summary>
	class MainClass
	{
		public static void Main(string[] args)
		{
			var gen = new FwBuildTasks.CollectTargets();
			gen.Generate();
		}
	}
}

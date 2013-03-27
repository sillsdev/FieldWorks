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

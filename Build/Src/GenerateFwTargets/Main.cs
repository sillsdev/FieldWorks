using System;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;

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
			// Get the parent directory of the running program.  We assume that
			// this is the root of the FieldWorks repository tree.
			var x = Assembly.GetExecutingAssembly().CodeBase;
			string x1;
			if (x.StartsWith("file:///"))
				x1 = x.Substring(7);
			else
				x1 = x;
			Regex r = new Regex("^/[A-Z]:");
			if (r.IsMatch(x1))
				x1 = x1.Substring(1);
			var fwrt = Path.GetDirectoryName(x1);
			fwrt = Path.GetDirectoryName(fwrt);
			Console.WriteLine("CodeBase = '{0}'; FWROOT = '{1}'", x, fwrt);
			var gen = new GenerateTargets(fwrt);
			gen.Generate();
		}
	}
}

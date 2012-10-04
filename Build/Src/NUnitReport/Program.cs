using System;
using FwBuildTasks;

namespace NUnitReport
{
	/// <summary>
	/// This program is intended to take semi-colon separated project names (e.g. DiscourseTests),
	/// go out to Output/Debug and generate NUnit reports from selected 'projectname'.dll-nunit-output.xml files.
	/// </summary>
	class Program
	{

		static void Main(string[] args)
		{
			var report = new ReportGenerator(args);
			if (report.Projects.Count == 0)
			{
				GenerateUsageReport();
				return;
			}
			report.GenerateReport();
		}

		private static void GenerateUsageReport()
		{
			Console.WriteLine("Usage:");
			Console.WriteLine("      /? or /help    Generates this report.");
			Console.WriteLine("      /v:x           where x is one of the normal logger verbosity choices");
			Console.WriteLine("                     Verbosity defaults to minimal");
			Console.WriteLine("                     (m)inimal:  Only failures are reported");
			Console.WriteLine("                     (n)ormal:   Failures and Ignored tests are reported");
			Console.WriteLine("                     (d)etailed: Even successful tests are reported");
			Console.WriteLine("      /a             Collect all appropriate files from {root}/Output/Debug.");
			Console.WriteLine("                     This cannot be mixed with specific test project names.");
			Console.WriteLine("      testproject    space separated test project names");
			Console.WriteLine("");
			Console.WriteLine("Examples:");
			Console.WriteLine("   NUnitReport xWorksTests DiscourseTests");
			Console.WriteLine("   NUnitReport xWorksTests /v:d");
			Console.WriteLine("   NUnitReport /v:normal xWorksTests");
			Console.WriteLine("   NUnitReport /a /v:m");
			Console.WriteLine("");
			Console.WriteLine("NUnitReport looks for a file in {root}/Output/Debug with a name of the form");
			Console.WriteLine("'projectname'.dll-nunit-output.xml for each test project name");
		}
	}
}

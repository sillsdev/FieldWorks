/// --------------------------------------------------------------------------------------------
#region /// Copyright (c) 2002, SIL International. All Rights Reserved.
/// <copyright from='2002' to='2002' company='SIL International'>
///		Copyright (c) 2002, SIL International. All Rights Reserved.
///
///		Distributable under the terms of either the Common Public License or the
///		GNU Lesser General Public License, as specified in the LICENSING.txt file.
/// </copyright>
#endregion
///
/// File: mkVersion.cs
/// Responsibility: Eberhard Beilharz
/// Last reviewed:
///
/// <remarks>
/// Creates a file from a template and expands variables.
/// </remarks>
/// --------------------------------------------------------------------------------------------
using System;
using System.IO;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace SIL.FieldWorks.Tools
{
	/// <summary>
	/// Creates a file from a template and expands variables.
	/// </summary>
	class mkVersion
	{
		static void ShowHelp()
		{
			Console.WriteLine("\nmkVersion. Creates a file from a template and expands variables.");
			Console.WriteLine("Copyright (c) 2002-$YEAR, SIL International. All Rights Reserved.\n");
			Console.WriteLine("Syntax: {0} template outfile",
				Path.GetFileName(Application.ExecutablePath));
			Console.WriteLine("\nThe following variables are expanded:");
			Console.WriteLine("$YEAR\t\t\texpands with current year (4 digits)");
			Console.WriteLine("$MONTH\t\t\texpands with current month (2 digits)");
			Console.WriteLine("$DAY\t\t\texpands with current day (2 digits)");
			Console.WriteLine("$!<name>\t\texpands with environment variable %name%");
			Console.WriteLine("$!{<name>:<val>}\texpands with environment variable %name% if %name% is");
			Console.WriteLine("\t\t\tset, otherwise with <val>");
			Console.WriteLine("\nExample:\n\t$YEAR.$MONTH$DAY$!{BUILD_LEVEL:9} will be expanded to 2002.09119");
		}

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static int Main(string[] args)
		{
			try
			{
				if (args.Length < 2 || args.Length == 1 && (args[0] == "/?" || args[0] == "-?"))
				{
					ShowHelp();
					return 0;
				}

				string templateFile = args[0];
				string outFile = args[1];

				StreamReader stream = new StreamReader(templateFile);
				string fileContents = stream.ReadToEnd();
				stream.Close();

				Regex regex = new Regex("\\$YEAR");
				fileContents = regex.Replace(fileContents, DateTime.Now.Year.ToString());

				regex = new Regex("\\$MONTH");
				fileContents = regex.Replace(fileContents, string.Format("{0:MM}", DateTime.Now));

				regex = new Regex("\\$DAY");
				fileContents = regex.Replace(fileContents, string.Format("{0:dd}", DateTime.Now));

				regex = new Regex("\\$!((?<env>\\w+)|\\{(?<env>\\w+):(?<default>\\w+)\\})");
				Match match = regex.Match(fileContents);
				while (match.Success)
				{
					string strEnv = match.Result("${env}");
					string strDefault = match.Result("${default}");
					string strEnvValue = Environment.GetEnvironmentVariable(strEnv);
					if (strEnvValue != null && strEnvValue != string.Empty)
						fileContents = regex.Replace(fileContents, strEnvValue, 1, match.Index);
					else
						fileContents = regex.Replace(fileContents, strDefault, 1, match.Index);

					match = regex.Match(fileContents);
				}

				fileContents = string.Format("// This file is generated from {0}. Do NOT modify!\n{1}",
					Path.GetFileName(templateFile), fileContents);

				StreamWriter outStream = new StreamWriter(outFile);
				outStream.Write(fileContents);
				outStream.Close();
			}
			catch(Exception e)
			{
				System.Console.WriteLine("Internal program error in program {0}", e.Source);
				System.Console.WriteLine("\nDetails:\n{0}\nin method {1}.{2}\nStack trace:\n{3}",
					e.Message, e.TargetSite.DeclaringType.Name, e.TargetSite.Name, e.StackTrace);

				return 1;
			}

			return 0;
		}
	}
}

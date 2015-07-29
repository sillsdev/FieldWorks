// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ConvertLib;

namespace ConverterConsole
{
	class Program
	{
		/// <summary>
		/// The main entry point for the console application.
		/// </summary>
		static int Main(string[] args)
		{
			int pos;

			ConvertLib.Convert cv = new ConvertLib.Convert();

			//  Save the properties passed in so we can run the conversion utility as a console application.
			if (args.Length == 2)
			{
				cv.m_FileName = args[0];
				cv.m_OutFileName = args[1];
			}

			 //  Since only 2 parameters are passed in, we assume the output parameter based on the 1st parameter.
			else if (args.Length == 1)
			{
				cv.m_FileName = args[0];
				pos = args[0].IndexOf(".xml");
				if (pos == 0)
				{
					Console.WriteLine("There are only 2 parameters and the first one is not an xml file." + args[0]);
					return 3;
				}

				if (args[0].Length == pos + 4)
				{
					cv.m_OutFileName = args[0].Substring(0, pos) + "Out.fwdata";
				}
			}
			//  If the parameters passed in aren't valid, return a message about how to run this program.			if (args.Length == 1)
			else
			{
				Console.WriteLine("This program will convert an up-to-date version 6 database into a ");
				Console.WriteLine("format than can be used by the rearchitected version (version 7)");
				Console.WriteLine("The conversion is based on the original version 7 Data model");
				Console.WriteLine("There are 2 versions of this program.");
				Console.WriteLine("1.  The Windows version is named 'Converter' and will present a ");
				Console.WriteLine("    dialog box where the parameters can be entered");
				Console.WriteLine("2.  The command line version is named 'ConverterConsole' can be run ");
				Console.WriteLine("    from a DOS prompt or in a run box.");
				Console.WriteLine("    It takes 1 or 2 parameters");
				Console.WriteLine("    Parameter 1 The database name (in XML) to be converted");
				Console.WriteLine("    Parameter 2 The output file name (.fwdata)");
				Console.WriteLine("    If the second paramemeter is omitted, the output name is ");
				Console.WriteLine("    the same as the input name followed by the word 'Out'");
				Console.WriteLine("***Important*** If the output file exists, it will be overwritten without warning");
				return 1;
			}

			try
			{
				cv.Conversion();
			}
			catch (Exception error)
			{
				Console.Error.WriteLine(error.Message);
				return 2;
			}
			return 0; // all is well
		}
	}
}

// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: CommandLineParser.cs
// Responsibility:
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections;
using InstallLanguage.Errors;

namespace InstallLanguage
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for CommandLineParser.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class CommandLineParser
	{
		/// <summary>
		/// This class manages the interactions with the command line.
		/// It validates the input arguments and collects them for future
		/// use.
		/// </summary>
		public enum Flag { c, i, r, f, o, err, s, q, newlang,
			testMainParserRoutine, dontDoAnything, testICUDataParser, slow, customLanguages
		};
		public enum ParseResult { error, noerror, stop };
		private Hashtable flagUsed;
		//		private	string ldFile;

		public CommandLineParser()
		{
			flagUsed = new Hashtable();
			//			ldFile = "";
		}

		//		public string ldFilename
		//		{
		//			get { return ldFile; }
		//		}

		public bool FlagUsed(Flag flag)
		{
			return flagUsed.ContainsKey(flag);
		}

		public void RemoveFlag(Flag flag)
		{
			flagUsed.Remove(flag);
		}

		public string FlagData(Flag flag)
		{
			string data = "";
			if (FlagUsed(flag))
				data = (string)flagUsed[flag];
			return data;
		}

		public ICollection FlagsUsed()
		{
			return flagUsed.Keys;
		}

		public ParseResult Parse(string[] args)
		{
			// Test if input arguments were supplied:
			if (args.Length < 1)
			{
				ShowUsage();
				return ParseResult.error;
			}

			// parse the command line arguments to know what to do...
			int len = args.Length;
			for (int i=0; i<len; i++)
			{
				//internal call when writing system created xml and is wanting it installed now
				if (args[i].IndexOf("-newLang") != -1)
				{
					flagUsed.Add(Flag.newlang, "");
				}
				//display custom Languages
				else if (args[i].IndexOf("-customLanguages") != -1)
				{
					flagUsed.Add(Flag.customLanguages, "");
				}
				//add PUA
				else if (args[i].IndexOf("-c") != -1)
				{
					if(ParseResult.error==ShareFilename(Flag.c,Flag.i, args, ref i))
						return ParseResult.error;
				}
					// install ldfile
				else if (args[i].IndexOf("-i") != -1)
				{
					if(ParseResult.error==ShareFilename(Flag.i,Flag.c, args, ref i))
						return ParseResult.error;
				}
					// remove following locale
				else if (args[i].IndexOf("-r") != -1)
				{
					if (flagUsed.ContainsKey(Flag.r) ||
						len < i + 2)
					{
						ShowUsage();
						return ParseResult.error;
					}
					// Make sure we have a locale name, not a file name:
					string LocaleName = args[++i];
					int iDotPosn = LocaleName.LastIndexOf(".");
					//if you add a file extension to the name, remove it
					if (iDotPosn != -1)
						LocaleName = LocaleName.Substring(0, iDotPosn);
					// add locale to remove
					flagUsed.Add(Flag.r, LocaleName);
				}
					// Note: this must be before the -f flag
				else if (args[i].IndexOf("-slow") != -1)
				{
					flagUsed.Add(Flag.slow, "");
				}
				else if (args[i].IndexOf("-f") != -1)
				{
					flagUsed.Add(Flag.f, "");
				}
				else if (args[i].IndexOf("-s") != -1)
				{
					flagUsed.Add(Flag.s, "");
				}
				else if (args[i].IndexOf("-q") != -1)
				{
					flagUsed.Add(Flag.q, "");
				}
				else if (args[i].IndexOf("-dontDoAnything") != -1)
				{
					flagUsed.Add(Flag.dontDoAnything, "");
				}
				else if (args[i].IndexOf("-testICUDataParser") != -1)
				{
					if (flagUsed.ContainsKey(Flag.testICUDataParser) ||
						len < i + 2)
					{
						ShowUsage();
						return ParseResult.error;
					}
					// Make sure we have a locale name (or "root"), not a file name:
					string icuDataFile = args[++i];
					int iSlashPosn = icuDataFile.LastIndexOf(@"\");
					//if the filename includes a path, warn and truncate
					if(iSlashPosn!= -1 && iSlashPosn <= icuDataFile.Length-2)
					{
						Console.WriteLine("Warning, truncating path and searching for file in: {0}",
							Generic.GetIcuDir() + @"data\locales");
						icuDataFile = icuDataFile.Substring(iSlashPosn + 1);
					}
					int iDotPosn = icuDataFile.LastIndexOf(".");
					//if you add a file extension to the name, remove it
					if (iDotPosn != -1)
						icuDataFile = icuDataFile.Substring(0, iDotPosn);
					// add locale to remove

					flagUsed.Add(Flag.testICUDataParser, icuDataFile);
				}
				else if (args[i].IndexOf("-err") != -1)	// found match
				{
					if (flagUsed.ContainsKey(Flag.err) || len < i + 2)
					{
						ShowUsage();
						return ParseResult.error;
					}
					// todo change ShowError to take an ErrorCodes variable OR
					//      add a function to LangDefErrors to go from a string "-41"
					//      to an ErrorCodes
					string error;
					error = args[i + 1];
					int iError = Convert.ToInt16(error);
					Error.ShowError((ErrorCodes)iError);
					return ParseResult.stop;
				}
				else if (args[i].IndexOf("-o") != -1)
				{
					flagUsed.Add(Flag.o, "");
				}
				else if (args[i].IndexOf("-testMainParserRoutine") != -1)
				{
					flagUsed.Add(Flag.testMainParserRoutine, "");
				}
					//If a filename or an unknown flag is used
				else
				{
					//This does not allow invalid flags
					if (args[i][0].Equals('-'))
					{
						Console.WriteLine("Invalid arguments: {0}", args[i]);
						Console.WriteLine("");
						ShowUsage();
						return ParseResult.error;
					}
						// This allows the file name to be given at anytime, so long as no -i has been given yet.
					else if(flagUsed.ContainsKey(Flag.i))
					{
						Console.WriteLine("Invalid arguments: {0}", args[i]);
						Console.WriteLine("");
						ShowUsage();
						return ParseResult.error;
					}
						//This allows for InstallLanguage to work like so:
						//>InstallLanguage xyz
						//That command will properly install the xyz locale
					else
					{
						string FileName = args[i];
						if (!FileName.EndsWith(".xml"))
							FileName += ".xml";

						flagUsed.Add(Flag.i, FileName);
					}
				}
			}
			if(ParseResult.error==CheckSharedFilename())
				return ParseResult.error;
			return ParseResult.noerror;
		}

		/// <summary>
		/// Allows two flags to "share" the same parameter following the flag.
		/// </summary>
		/// <example>
		/// Using this with both Flag.i and Flag.c, all the following are the same:
		/// <code>
		/// InstallLangage -i ldf -c ldf.xml
		/// InstallLanguage -i -c ldf.xml
		/// InstallLanguage -c -i ldf
		/// InstallLanguage -c ldf -i
		/// InstallLanguage ldf -c
		/// ... etc
		/// </code>
		/// </example>
		/// <param name="primaryFlag">The flag that was just found.</param>
		/// <param name="otherFlag">The other flag that may be found sooner or later.</param>
		/// <param name="args">The argument list to parse.</param>
		/// <param name="i">The index of the flag that we just found in the argument list.</param>
		/// <returns></returns>
		private ParseResult ShareFilename(Flag primaryFlag, Flag otherFlag, string[] args, ref int i)
		{
			//Make sure flag isn't used multiple times and that it has a filename
			if (flagUsed.ContainsKey(primaryFlag) )
			{
				ShowUsage();
				return ParseResult.error;
			}

			// If we aren't given a LDF
			if( i>=args.Length - 1 || (args[i+1][0].Equals('-')))
			{
				if (flagUsed.ContainsKey(otherFlag) )
				{
					// Use the same value as otherFlag
					flagUsed.Add(primaryFlag,FlagData(otherFlag));
				}
				else
				{
					// Add a "null" because they may provide it later with the "-i" flag.
					flagUsed.Add(primaryFlag,null);
				}
				// Continue with the next argument, this was fine.
				return ParseResult.noerror;
			}

			// Make sure we have a file name, not just a locale name:
			// (We are assuming that the use will provide either a locale name
			// or an xml file, and no other possibilities)
			string FileName = args[++i];
			if (!FileName.EndsWith(".xml"))
				FileName += ".xml";

			flagUsed.Add(primaryFlag, FileName);

			// If no file was provided with the -i flag, use this flag
			if (flagUsed.ContainsKey(otherFlag))
			{
				if(flagUsed[otherFlag]==null)
				{
					flagUsed[otherFlag]=FileName;
				}
				else if(FlagData(otherFlag).ToLowerInvariant()!=FlagData(primaryFlag).ToLowerInvariant())
				{
					ShowUsage();
					return ParseResult.error;
				}
			}
			return ParseResult.noerror;
		}

		/// <summary>
		/// Checks to make sure that after all the arguments have been parsed the result is valid.
		/// Currently each Flag to be checked is entered manually into this function.
		/// </summary>
		/// <returns>The ParseResult showing if there was an error.</returns>
		private ParseResult CheckSharedFilename()
		{
			// Checks to make sure we have a value if the flag was used
			if(flagUsed.ContainsKey(Flag.i) && flagUsed[Flag.i]==null)
				return ParseResult.error;
			if(flagUsed.ContainsKey(Flag.c) && flagUsed[Flag.c]==null)
				return ParseResult.error;
			return ParseResult.noerror;
		}

		public static void ShowUsage()
		{
			Console.WriteLine("Usage: InstallLanguage [flags]");
			Console.WriteLine(" flags:  optional");
			Console.WriteLine("		If no flags are given, '-i' is assumed");
			Console.WriteLine("  [-i] [LD] = Installs the locale 'LD'.  ");
			Console.WriteLine("              If no locale definition file is given, uses the same file as '-c'");
			Console.WriteLine("  -c [LD] = Add user defined PUA characters in given 'LD' file.");
			Console.WriteLine("            If no locale file is given, uses the same file as '-i'");
			Console.WriteLine("      NOTE: Both -i and -c must have the same LD");
			//			Console.WriteLine("  -k = Install keyboards.");
			Console.WriteLine("  -o = restore orig files and reinstall lang def files in languages directory.");
			//			Console.WriteLine("  -p file = Execute file after completing installation.");
			Console.WriteLine("  -q = quiet mode - no User interaction required.");
			Console.WriteLine("  -r locale = Remove this locale. DELETES THE LANGUAGE DEFINITION XML FILE");
			Console.WriteLine("  -s = show custom locales currently installed.");
			//			Console.WriteLine("  -u file = Unzip file.zip containing all relevant information");
			//			Console.WriteLine("            and istall everything.");
			//			Console.WriteLine("  -z file = Zip all relevant information into file.zip.");
			Console.WriteLine(" ");

			Console.WriteLine("  -err error_number = display description of requested error.");

			//			Console.WriteLine("  -d database = Add/Update LgWritingSystem in database.");
			//			Console.WriteLine("  -e = Install encoding converters.");
			Console.WriteLine("  -customLanguages = Print the custom languages installed so far.");
			//			Console.WriteLine("  -f = Install fonts.");
			Console.WriteLine("  -testICUDataParser = parses ICU text files then immediately writes them back.");
			Console.WriteLine("  -dontDoAnything = will not do anything that you have just asked it to.");
		}
	}
}

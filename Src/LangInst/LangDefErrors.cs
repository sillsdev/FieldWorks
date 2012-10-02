using System;
using System.Collections;


namespace InstallLanguage
{
	namespace Errors
	{
		public class LDExceptions : System.Exception
		{
			public LDExceptions(ErrorCodes ec)
				: base("")
			{
				m_ec = ec;
				m_txtMsg = "";
			}
			public LDExceptions(ErrorCodes ec, string txt)
				: base("")
			{
				m_ec = ec;
				m_txtMsg = txt;
			}
			private ErrorCodes m_ec;
			private string m_txtMsg;
			public ErrorCodes ec
			{
				get { return m_ec; }
			}
			public bool HasConstructorText
			{
				get
				{
					if (m_txtMsg != null && m_txtMsg.Length > 0)
						return true;
					else
						return false;
				}
			}
			public string ConstructorText
			{
				get { return m_txtMsg; }
			}
		}

		public enum ErrorCodes
		{
			// Not specified - uninitialized
			None=1,

			// Success Value
			Success=0,

			// Command Line Argument Errors
			CommandLine=-1,

			// User cancellations (with indication of problem)
			CancelAccessFailure=-4,

			// Invalid Language Def file data
			LDBaseLocale=-11,
			LDNewLocale=-12,
			LDLocaleResources=-13,
			LDFileName=-14,
			LDBadData=-15,
			LDParsingError=-16,
			LDUsingISO3CountryName=-17,
			LDUsingISO3LanguageName=-18,
			LDUsingISO3ScriptName = -19,

			// Genrb errors
			ResIndexFile=-21,
			RootFile=-22,
			NewLocaleFile=-23,
			GeneralFile=-24,
			ExistingLocaleFile=25,

			// General file errors
			FileRead=-31,
			FileWrite=-32,
			FileRename=-33,
			FileNotFound=-34,

			// Specific file errors
			RootTxt_FileNotFound=-41,
			ResIndexTxt_FileNotFound=-42,
			RootRes_FileNotFound=-43,
			ResIndexRes_FileNotFound=-44,
			RootTxt_InvalidCustomResourceFormat=-45,
			RootTxt_CustomResourceNotFound=-46,

			// Registry related errors
			RegistryIcuDir=-50,
			RegistryIcuLanguageDir = -51,
			RegistryIcuTemplatesDir = -52,

			// subprocess Error Codes
			Gennames = -60,
			Genprops = -61,
			Gennorm = -62,
			Genbidi = -63,
			Gencase = -64,

			// PUA Error Codes
			PUAOutOfRange = -70,
			// note: we assume good PUA characters, so we don't check every detail,
			// this just is if we happen to discover an error while parsing.
			PUADefinitionFormat = -71,

			// ICU data file related errors
			ICUDataParsingError = -80,
			ICUNodeAccessError = -81,

			// Programming error
			ProgrammingError=-998,

			// Unknown Error
			NonspecificError=-999,
		};

		public class Error
		{
			private static Error singleton;

			private Hashtable ErrorStrings;

			public static void ShowError(ErrorCodes ec)
			{
				// todo - Is this enough?
				string error = Error.Text(ec);
				Console.WriteLine(" ");
				Console.WriteLine("Error:  error description");
				Console.WriteLine(" ");
				Console.WriteLine(ec + ":  \"" + error + "\"");
				Console.WriteLine(" ");
			}

			public static string Text(ErrorCodes ec)
			{
				if(singleton==null)
					singleton=new Error();
				if (singleton.ErrorStrings.Contains(ec))
					return (string)singleton.ErrorStrings[ec];
				return "Invalid ErrorCode";
			}

			private Error()
			{
				ErrorStrings = new Hashtable();
				ErrorStrings.Add(ErrorCodes.None, "No Value set yet - None");
				ErrorStrings.Add(ErrorCodes.Success, "No Errors");
				ErrorStrings.Add(ErrorCodes.CommandLine,
					"Invalid arguments on the command line");

				ErrorStrings.Add(ErrorCodes.CancelAccessFailure,
					"The user cancelled the install while ICU files were inaccessible due to memory mapping from another process.");

				ErrorStrings.Add(ErrorCodes.LDBaseLocale,
					"Base locale ontains invalid file names");
				ErrorStrings.Add(ErrorCodes.LDNewLocale, "Blank New Locale");
				ErrorStrings.Add(ErrorCodes.LDLocaleResources,
					"Local Resources mis-matching braces");
				ErrorStrings.Add(ErrorCodes.LDFileName, "Invalid LD Name or File");
				ErrorStrings.Add(ErrorCodes.LDBadData, "Invalid Data");
				ErrorStrings.Add(ErrorCodes.LDParsingError, "Not able to parse/read the XML Language Definition file.");
				ErrorStrings.Add(ErrorCodes.LDUsingISO3CountryName, "The Country Name is already used as an ISO3 value.");
				ErrorStrings.Add(ErrorCodes.LDUsingISO3LanguageName, "The Language Name is already used as an ISO3 value");
				ErrorStrings.Add(ErrorCodes.LDUsingISO3ScriptName, "The Script Name is already used as an ISO3 value");


				ErrorStrings.Add(ErrorCodes.ResIndexFile, "GENRB error creating res_index.res file");
				ErrorStrings.Add(ErrorCodes.RootFile, "GENRB error creating root.res file");
				ErrorStrings.Add(ErrorCodes.NewLocaleFile, "GENRB error creating new locale .res file");
				ErrorStrings.Add(ErrorCodes.GeneralFile, "GENRB error creating a file");

				ErrorStrings.Add(ErrorCodes.FileRead, "Error reading file");
				ErrorStrings.Add(ErrorCodes.FileWrite, "Error writing file");
				ErrorStrings.Add(ErrorCodes.FileRename, "Error renaming file");
				ErrorStrings.Add(ErrorCodes.FileNotFound, "File not found");

				ErrorStrings.Add(ErrorCodes.RootTxt_FileNotFound, "root.txt file not found");
				ErrorStrings.Add(ErrorCodes.ResIndexTxt_FileNotFound,
					"res_index.txt file not found");
				ErrorStrings.Add(ErrorCodes.RootRes_FileNotFound, "*_root.res file not found");
				ErrorStrings.Add(ErrorCodes.ResIndexRes_FileNotFound,
					"res_index.res file not found");
				ErrorStrings.Add(ErrorCodes.RootTxt_InvalidCustomResourceFormat,
					"root.txt Custom resource is not in the proper format.");

				ErrorStrings.Add(ErrorCodes.RootTxt_CustomResourceNotFound,
					"root.txt Custom resource item was not found.");


				ErrorStrings.Add(ErrorCodes.RegistryIcuDir,"Icu Dir not in the Registry.");
				ErrorStrings.Add(ErrorCodes.RegistryIcuLanguageDir,"Icu Data Language not in the Registry.");
				ErrorStrings.Add(ErrorCodes.RegistryIcuTemplatesDir,"Icu Code Templates not in the Registry.");

				// PUA Error Codes
				ErrorStrings.Add(ErrorCodes.Gennames,"ICU gennames exited with an error");
				ErrorStrings.Add(ErrorCodes.Genprops,"ICU genprops exited with an error");
				ErrorStrings.Add(ErrorCodes.Gennorm,"ICU gennorm exited with an error");
				ErrorStrings.Add(ErrorCodes.Genbidi,"ICU genbidi exited with an error");
				ErrorStrings.Add(ErrorCodes.Gencase,"ICU gencase exited with an error");
				ErrorStrings.Add(ErrorCodes.PUAOutOfRange,"Cannot insert given character, not within the PUA range");
				ErrorStrings.Add(ErrorCodes.PUADefinitionFormat,
					"Given an inproperly formatted character definition, either in the LDF or UnicodeData.txt" +
					"Note: we assume good PUA characters, so we don't check every detail " +
					"we just happened to discover an error while parsing. ");

				// ICU Data Errors
				ErrorStrings.Add(ErrorCodes.ICUDataParsingError,"Error parsing ICU file, not within expected format");
				ErrorStrings.Add(ErrorCodes.ICUNodeAccessError,"Error while trying to access an ICU datafile node.");

				ErrorStrings.Add(ErrorCodes.ProgrammingError, "Programming error");
				ErrorStrings.Add(ErrorCodes.NonspecificError, "Nonspecific error");

				// TODO: make sure all ErrorCodes have a corresponding ErrorString entry.
				// (this code won't work becuase you cannot use "foreach" with enums
				//				foreach (ErrorCodes ec in ErrorCodes)
				//				{
				//					if (! ErrorStrings.Contains(ec))
				//						throw new LDExceptions(ErrorCodes.FileWrite);
				//				}
			}
		}
	}
}

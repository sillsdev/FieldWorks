// Copyright (c) 2013-2017 SIL International
// This software is licensed under the MIT license (http://opensource.org/licenses/MIT)

using System;
using System.Text;

namespace SIL.FieldWorks.UnicodeCharEditor
{
	///<summary>
	///</summary>
	public enum ErrorCodes
	{
		///<summary>Not specified - uninitialized</summary>
		None = 1,

		///<summary>Success Value</summary>
		Success = 0,

		///<summary>Command Line Argument Errors</summary>
		CommandLine = -1,

		///<summary>User cancellations (with indication of problem)</summary>
		CancelAccessFailure = -4,

		///<summary>Invalid Language Def file data</summary>
		LdBaseLocale = -11,
		///<summary></summary>
		LdNewLocale = -12,
		///<summary></summary>
		LdLocaleResources = -13,
		///<summary></summary>
		LdFileName = -14,
		///<summary></summary>
		LdBadData = -15,
		///<summary></summary>
		LdParsingError = -16,
		///<summary></summary>
		LdUsingISO3CountryName = -17,
		///<summary></summary>
		LdUsingISO3LanguageName = -18,
		///<summary></summary>
		LdUsingISO3ScriptName = -19,

		// Genrb errors
		///<summary></summary>
		ResIndexFile = -21,
		///<summary></summary>
		RootFile = -22,
		///<summary></summary>
		NewLocaleFile = -23,
		///<summary></summary>
		GeneralFile = -24,
		///<summary></summary>
		ExistingLocaleFile = 25,

		// General file errors
		///<summary></summary>
		FileRead = -31,
		///<summary></summary>
		FileWrite = -32,
		///<summary></summary>
		FileRename = -33,
		///<summary></summary>
		FileNotFound = -34,

		// Specific file errors
		///<summary></summary>
		RootTxtFileNotFound = -41,
		///<summary></summary>
		ResIndexTxtFileNotFound = -42,
		///<summary></summary>
		RootResFileNotFound = -43,
		///<summary></summary>
		ResIndexResFileNotFound = -44,
		///<summary></summary>
		RootTxtInvalidCustomResourceFormat = -45,
		///<summary></summary>
		RootTxtCustomResourceNotFound = -46,

		// Registry related errors
		///<summary></summary>
		RegistryIcuDir = -50,
		///<summary></summary>
		RegistryIcuLanguageDir = -51,
		///<summary></summary>
		RegistryIcuTemplatesDir = -52,

		// subprocess Error Codes
		///<summary></summary>
		Gennames = -60,
		///<summary></summary>
		Genprops = -61,
		///<summary></summary>
		Gennorm = -62,
		///<summary></summary>
		Genbidi = -63,
		///<summary></summary>
		Gencase = -64,

		// PUA Error Codes
		///<summary></summary>
		PUAOutOfRange = -70,
		// note: we assume good PUA characters, so we don't check every detail,
		// this just is if we happen to discover an error while parsing.
		///<summary></summary>
		PUADefinitionFormat = -71,

		// ICU data file related errors
		///<summary></summary>
		ICUDataParsingError = -80,
		///<summary></summary>
		ICUNodeAccessError = -81,

		// Programming error
		///<summary></summary>
		ProgrammingError = -998,

		// Unknown Error
		///<summary></summary>
		NonspecificError = -999,
	}

	/// <summary>
	/// Extension methods for ErrorCodes enum
	/// </summary>
	public static class ErrorCodesExtensionMethods
	{
		/// <summary>
		/// Gets the description for the error code
		/// </summary>
		public static string GetDescription(this ErrorCodes errorCode)
		{
			var bldr = new StringBuilder();
			switch (errorCode)
			{
				case ErrorCodes.None:
					bldr.AppendLine("No Value set yet - None");
					break;
				case ErrorCodes.Success:
					bldr.AppendLine("No Errors");
					break;
				case ErrorCodes.CommandLine:
					bldr.AppendLine("Invalid arguments on the command line");
					break;
				case ErrorCodes.CancelAccessFailure:
					bldr.AppendLine(
						"The user cancelled the install while ICU files were inaccessible due to memory mapping from another process.");
					break;
				case ErrorCodes.LdBaseLocale:
					bldr.AppendLine("Base locale contains invalid file names");
					break;
				case ErrorCodes.LdNewLocale:
					bldr.AppendLine("Blank New Locale");
					break;
				case ErrorCodes.LdLocaleResources:
					bldr.AppendLine("Local Resources mis-matching braces");
					break;
				case ErrorCodes.LdFileName:
					bldr.AppendLine("Invalid LD Name or File");
					break;
				case ErrorCodes.LdBadData:
					bldr.AppendLine("Invalid Data");
					break;
				case ErrorCodes.LdParsingError:
					bldr.AppendLine("Not able to parse/read the XML Language Definition file.");
					break;
				case ErrorCodes.LdUsingISO3CountryName:
					bldr.AppendLine("The Country Name is already used as an ISO3 value.");
					break;
				case ErrorCodes.LdUsingISO3LanguageName:
					bldr.AppendLine("The Language Name is already used as an ISO3 value");
					break;
				case ErrorCodes.LdUsingISO3ScriptName:
					bldr.AppendLine("The Script Name is already used as an ISO3 value");
					break;
				case ErrorCodes.ResIndexFile:
					bldr.AppendLine("GENRB error creating res_index.res file");
					break;
				case ErrorCodes.RootFile:
					bldr.AppendLine("GENRB error creating root.res file");
					break;
				case ErrorCodes.NewLocaleFile:
					bldr.AppendLine("GENRB error creating new locale .res file");
					break;
				case ErrorCodes.GeneralFile:
					bldr.AppendLine("GENRB error creating a file");
					break;
				case ErrorCodes.FileRead:
					bldr.AppendLine("Error reading file");
					break;
				case ErrorCodes.FileWrite:
					bldr.AppendLine("Error writing file");
					break;
				case ErrorCodes.FileRename:
					bldr.AppendLine("Error renaming file");
					break;
				case ErrorCodes.FileNotFound:
					bldr.AppendLine("File not found");
					break;
				case ErrorCodes.RootTxtFileNotFound:
					bldr.AppendLine("root.txt file not found");
					break;
				case ErrorCodes.ResIndexTxtFileNotFound:
					bldr.AppendLine("res_index.txt file not found");
					break;
				case ErrorCodes.RootResFileNotFound:
					bldr.AppendLine("*_root.res file not found");
					break;
				case ErrorCodes.ResIndexResFileNotFound:
					bldr.AppendLine("res_index.res file not found");
					break;
				case ErrorCodes.RootTxtInvalidCustomResourceFormat:
					bldr.AppendLine("root.txt Custom resource is not in the proper format.");
					break;
				case ErrorCodes.RootTxtCustomResourceNotFound:
					bldr.AppendLine("root.txt Custom resource item was not found.");
					break;
				case ErrorCodes.RegistryIcuDir:
					bldr.AppendLine("Icu Dir not in the Registry.");
					break;
				case ErrorCodes.RegistryIcuLanguageDir:
					bldr.AppendLine("Icu Data Language not in the Registry.");
					break;
				case ErrorCodes.RegistryIcuTemplatesDir:
					bldr.AppendLine("Icu Code Templates not in the Registry.");
					break;
				case ErrorCodes.Gennames:
					bldr.AppendLine("ICU gennames exited with an error");
					break;
				case ErrorCodes.Genprops:
					bldr.AppendLine("ICU genprops exited with an error");
					break;
				case ErrorCodes.Gennorm:
					bldr.AppendLine("ICU gennorm exited with an error");
					break;
				case ErrorCodes.Genbidi:
					bldr.AppendLine("ICU genbidi exited with an error");
					break;
				case ErrorCodes.Gencase:
					bldr.AppendLine("ICU gencase exited with an error");
					break;
				case ErrorCodes.PUAOutOfRange:
					bldr.AppendLine("Cannot insert given character: not within the PUA range");
					break;
				case ErrorCodes.PUADefinitionFormat:
					bldr.AppendLine("Given an inproperly formatted character definition, either in the LDF or UnicodeData.txt");
					bldr.AppendLine("Note: we assume good PUA characters, so we don't check every detail.");
					bldr.AppendLine("We just happened to discover an error while parsing. ");
					break;
				case ErrorCodes.ICUDataParsingError:
					bldr.AppendLine("Error parsing ICU file, not within expected format");
					break;
				case ErrorCodes.ICUNodeAccessError:
					bldr.AppendLine("Error while trying to access an ICU datafile node.");
					break;
				case ErrorCodes.ProgrammingError:
					bldr.AppendLine("Programming error");
					break;
				case ErrorCodes.NonspecificError:
					bldr.AppendLine("Nonspecific error");
					break;
				default:
					bldr.AppendLine("Unknown error");
					break;
			}

			return bldr.ToString();
		}

		/// <summary>
		/// Converts the error code to a return value suitable to be returned from a Linux
		/// command line app.
		/// </summary>
		/// <param name="errorCode"></param>
		/// <returns></returns>
		public static short GetLinuxErrorCode(ErrorCodes errorCode)
		{
			// REVIEW: this method is currently not used. Previously we defined different values
			// on Windows and Linux for the items below with a comment saying that we need valid
			// return values on Linux. I'll keep this method for now, but if it turns out that we
			// don't need it it should be removed.

			// Valid linux return values are between 0-255
			// So returning -70 would return 186

			switch (errorCode)
			{
				case ErrorCodes.Success:
					return 0;
				case ErrorCodes.None:
					return 1;
				case ErrorCodes.CommandLine:
					return 2;
				case ErrorCodes.CancelAccessFailure:
					return 4;
				case ErrorCodes.LdBaseLocale:
					return 11;
				case ErrorCodes.LdNewLocale:
					return 12;
				case ErrorCodes.LdLocaleResources:
					return 13;
				case ErrorCodes.LdFileName:
					return 14;
				case ErrorCodes.LdBadData:
					return 15;
				case ErrorCodes.LdParsingError:
					return 16;
				case ErrorCodes.LdUsingISO3CountryName:
					return 17;
				case ErrorCodes.LdUsingISO3LanguageName:
					return 18;
				case ErrorCodes.LdUsingISO3ScriptName:
					return 19;
				case ErrorCodes.ResIndexFile:
					return 21;
				case ErrorCodes.RootFile:
					return 22;
				case ErrorCodes.NewLocaleFile:
					return 23;
				case ErrorCodes.GeneralFile:
					return 24;
				case ErrorCodes.ExistingLocaleFile:
					return 25;
				case ErrorCodes.FileRead:
					return 31;
				case ErrorCodes.FileWrite:
					return 32;
				case ErrorCodes.FileRename:
					return 33;
				case ErrorCodes.FileNotFound:
					return 34;
				case ErrorCodes.RootTxtFileNotFound:
					return 41;
				case ErrorCodes.ResIndexTxtFileNotFound:
					return 42;
				case ErrorCodes.RootResFileNotFound:
					return 43;
				case ErrorCodes.ResIndexResFileNotFound:
					return 44;
				case ErrorCodes.RootTxtInvalidCustomResourceFormat:
					return 45;
				case ErrorCodes.RootTxtCustomResourceNotFound:
					return 46;
				case ErrorCodes.RegistryIcuDir:
					return 50;
				case ErrorCodes.RegistryIcuLanguageDir:
					return 51;
				case ErrorCodes.RegistryIcuTemplatesDir:
					return 52;
				case ErrorCodes.Gennames:
					return 60;
				case ErrorCodes.Genprops:
					return 61;
				case ErrorCodes.Gennorm:
					return 62;
				case ErrorCodes.Genbidi:
					return 63;
				case ErrorCodes.Gencase:
					return 64;
				case ErrorCodes.PUAOutOfRange:
					return 70;
				case ErrorCodes.PUADefinitionFormat:
					return 71;
				case ErrorCodes.ICUDataParsingError:
					return 80;
				case ErrorCodes.ICUNodeAccessError:
					return 81;
				case ErrorCodes.ProgrammingError:
					return 254;
				case ErrorCodes.NonspecificError:
					return 255;
			}

			throw new NotImplementedException(string.Format("Unknown error code {0}", errorCode));
		}
	}
}

// Copyright (c) 2013-2020 SIL International
// This software is licensed under the MIT license (http://opensource.org/licenses/MIT)

using System;
using System.Text;

namespace SIL.FieldWorks.UnicodeCharEditor
{
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
	}
}
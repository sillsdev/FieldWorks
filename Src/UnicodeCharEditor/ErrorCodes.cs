// Copyright (c) 2013-2018 SIL International
// This software is licensed under the MIT license (http://opensource.org/licenses/MIT)

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
		///<summary />
		LdNewLocale = -12,
		///<summary />
		LdLocaleResources = -13,
		///<summary />
		LdFileName = -14,
		///<summary />
		LdBadData = -15,
		///<summary />
		LdParsingError = -16,
		///<summary />
		LdUsingISO3CountryName = -17,
		///<summary />
		LdUsingISO3LanguageName = -18,
		///<summary />
		LdUsingISO3ScriptName = -19,

		// Genrb errors
		///<summary />
		ResIndexFile = -21,
		///<summary />
		RootFile = -22,
		///<summary />
		NewLocaleFile = -23,
		///<summary />
		GeneralFile = -24,
		///<summary />
		ExistingLocaleFile = 25,

		// General file errors
		///<summary />
		FileRead = -31,
		///<summary />
		FileWrite = -32,
		///<summary />
		FileRename = -33,
		///<summary />
		FileNotFound = -34,

		// Specific file errors
		///<summary />
		RootTxtFileNotFound = -41,
		///<summary />
		ResIndexTxtFileNotFound = -42,
		///<summary />
		RootResFileNotFound = -43,
		///<summary />
		ResIndexResFileNotFound = -44,
		///<summary />
		RootTxtInvalidCustomResourceFormat = -45,
		///<summary />
		RootTxtCustomResourceNotFound = -46,

		// Registry related errors
		///<summary />
		RegistryIcuDir = -50,
		///<summary />
		RegistryIcuLanguageDir = -51,
		///<summary />
		RegistryIcuTemplatesDir = -52,

		// subprocess Error Codes
		///<summary />
		Gennames = -60,
		///<summary />
		Genprops = -61,
		///<summary />
		Gennorm = -62,
		///<summary />
		Genbidi = -63,
		///<summary />
		Gencase = -64,

		// PUA Error Codes
		///<summary />
		PUAOutOfRange = -70,
		// note: we assume good PUA characters, so we don't check every detail,
		// this just is if we happen to discover an error while parsing.
		///<summary />
		PUADefinitionFormat = -71,

		// ICU data file related errors
		///<summary />
		ICUDataParsingError = -80,
		///<summary />
		ICUNodeAccessError = -81,

		// Programming error
		///<summary />
		ProgrammingError = -998,

		// Unknown Error
		///<summary />
		NonspecificError = -999,
	}
}
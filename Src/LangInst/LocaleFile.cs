using System;
using System.IO;
using System.Xml;
using System.Text;
using System.Collections;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Win32;			// registry types
using System.Diagnostics;
using InstallLanguage.Errors;
using SIL.FieldWorks.Common.Utils;
// For PUACharacter
using SIL.FieldWorks.Common.FwUtils;

namespace InstallLanguage
{
	public class LocalEntry
	{
		public string name; // include only:  name (i.e., key)
		public string text; // includes:  "name { body }"
		public Hashtable children;	// key=name, data=LocalEntry
		public LocalEntry parent;	// parent of this entry
	}

	public struct UndoFiles
	{
		public string backupFile;
		public string originalFile;
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This is the one place (hopefully) where the locale is parsed and broken into it's
	/// seperate pieces [lang, country, variant] and then put back together with proper case
	/// to form the locale (in proper case).
	/// lang    => lower case
	/// country => upper case
	/// variant => upper case
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class LocaleParser
	{
		private string m_Locale, m_Lang, m_Country, m_Variant;
		private string m_Script;	// this will be between language and country when present

		#region Accessor methods
		public string Locale { get { return m_Locale; }}
		public string LangKey { get { return m_Lang; }}
		public string ScriptKey { get { return m_Script; } }
		public string CountryKey { get { return m_Country; }}
		public string VariantKey { get { return m_Variant; }}
		#endregion

		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// This is the one place (hopefully) where the locale is parsed and broken into it's
		/// seperate pieces [lang, script, country, variant] and then put back together with proper case
		/// to form the locale (in proper case).
		/// lang    => lower case
		/// script  => first letter cap, rest lower case
		/// country => upper case
		/// variant => upper case
		///
		/// If present, the 4 character script will be between the language and country portion of the ICU code.
		/// If the script is missing, the leading underscore will also not be present.
		///
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		public LocaleParser(string locale)
		{
			m_Lang = m_Script = m_Country = m_Variant = "";
			#region Compute the Lang, Script, Country, Variant and case proper Locale
			char [] seps = new char[] {'_', '-'};
			int startPos = 0;
			int endPos = locale.IndexOfAny(seps);  //IndexOf('_');
			if (endPos > 0)	// now process after the first underscore 'xkal_US', 'xkal_Latn_US', ...
			{
				m_Lang = locale.Substring(startPos, endPos - startPos).ToLowerInvariant();
				m_Locale = m_Lang + locale[endPos];
				startPos = endPos+1;
				endPos = locale.IndexOfAny(seps, startPos);

				// see if we just passed the script (will be 4 chars long)
				if (endPos > 0 && (endPos - startPos == 4))
				{
					m_Script = TitleCase(locale.Substring(startPos, endPos - startPos));
					m_Locale += m_Script + locale[endPos];
					startPos = endPos + 1;
					endPos = locale.IndexOfAny(seps, startPos);
				}
				else if (endPos == -1 && (locale.Length - startPos == 4))
				{	// no more underscores and the content since the last one has a length of 4
					m_Script = TitleCase(locale.Substring(startPos, locale.Length - startPos));
					m_Locale += m_Script;
					return;
				}

				if (endPos > 0)	// found a second underscore
				{
					m_Country = locale.Substring(startPos, endPos - startPos).ToUpperInvariant();
					m_Locale += m_Country + locale[endPos];
					startPos = endPos+1;
					endPos = locale.Length;
					if (endPos > 0)
					{
						m_Variant = locale.Substring(startPos, endPos-startPos).ToUpperInvariant();
						m_Locale += m_Variant;
					}
				}
				else
				{
					m_Country = locale.Substring(startPos, locale.Length-startPos).ToUpperInvariant();
					m_Locale += m_Country;
				}
			}
			else
			{
				m_Lang = locale.ToLowerInvariant();
				m_Locale = m_Lang;
			}
			#endregion
		}

		/// <summary>
		/// take the passed in string and return one that is captial for the first char and lowercase for the rest
		/// </summary>
		/// <param name="data">string to use</param>
		/// <returns>Asdf or Quick ...</returns>
		private string TitleCase(string data)
		{
			string allLower = data.ToLowerInvariant();
			string allUpper = data.ToUpperInvariant();
			string result = allUpper[0].ToString() + allLower.Substring(1);
			return result;
		}
	}

	/// <summary>
	/// Class for stroing boolean values for lang, country and variant.
	/// </summary>
	public class ICUInfo
	{
		private LocaleParser m_LocaleInfo;
		private bool m_LanguageExists;
		private bool m_ScriptExists;
		private bool m_CountryExists;
		private bool m_VariantExists;

		public LocaleParser LocaleItems { get {return m_LocaleInfo; } }
		public bool HasLanguage { get {return m_LanguageExists;} }
		public bool HasScript { get { return m_ScriptExists; } }
		public bool HasCountry { get { return m_CountryExists; } }
		public bool HasVariant { get { return m_VariantExists; } }

		public ICUInfo(string localeName, bool lang, bool script, bool country, bool variant)
		{
			m_LocaleInfo = new LocaleParser(localeName);
			m_LanguageExists = lang;
			m_ScriptExists = script;
			m_CountryExists = country;
			m_VariantExists = variant;
		}
	}

	/// <summary>
	/// Class for storing basic infor about ICU fields as well as custom and locale.
	/// </summary>
	public class CustomResourceInfo : ICUInfo
	{
		#region Private member variables
		private bool m_CustomExists;
		private bool m_LocaleExists;
		#endregion

		#region member Attributes
		public bool HasCustom { get {return m_CustomExists;} }
		public bool HasLocale { get {return m_LocaleExists;} }
		#endregion

		#region Constructor
		public CustomResourceInfo(string localeName, bool custom, bool locale, bool lang, bool script, bool country, bool variant) :
			base(localeName, lang, script, country, variant)
		{
			m_CustomExists = custom;
			m_LocaleExists = locale;
		}
		#endregion
	}


	public class LocaleFileClass
	{

		#region Temporary file processing
		ArrayList m_TempFiles;


		/// <summary>
		/// Add a file to the list of temporary files.
		/// </summary>
		/// <param name="strFile">Name of file to add</param>
		public void AddTempFile(string strFile)
		{
			LogFile.AddVerboseLine("Adding Temp File: <" + strFile + ">");
			m_TempFiles.Add(strFile);
		}


		/// <summary>
		/// Delete all files in the list of temporary files.
		/// </summary>
		public void RemoveTempFiles()
		{
			LogFile.AddVerboseLine("Removing Temp Files --- Start");
			foreach (string str in m_TempFiles)
			{
				Generic.DeleteFile(str);
			}
			LogFile.AddVerboseLine("Removing Temp Files --- Finish");
		}
		#endregion

		#region Undo File Stack
		ArrayList m_UndoFileStack;


		/// <summary>
		/// Create a copy of the backup and source file names for future use.
		/// </summary>
		/// <param name="srcFile">name of source (original) file</param>
		/// <param name="backupFile">name of backup file</param>
		public void AddUndoFileFrame( string srcFile, string backupFile)
		{
			LogFile.AddVerboseLine("Adding Undo File: <" + backupFile + ">");
			UndoFiles frame = new UndoFiles();
			frame.originalFile = srcFile;
			frame.backupFile = backupFile;
			m_UndoFileStack.Add(frame);
		}


		/// <summary>
		/// Remove the backup files that were created to restore original files if there
		/// were a problem.  If this method is called, there was no problem.  We're
		/// just removing the backup file - much like a temp file at this point.
		/// </summary>
		public void RemoveBackupFiles()
		{
			LogFile.AddVerboseLine("Removing Undo Files --- Start");
			for (int i = m_UndoFileStack.Count; --i >= 0; )
			{
				UndoFiles frame = (UndoFiles)m_UndoFileStack[i];
				if (File.Exists(frame.backupFile))
				{
					Generic.DeleteFile(frame.backupFile);
				}
			}
			LogFile.AddVerboseLine("Removing Undo Files --- Finish");
		}


		/// <summary>
		/// Copies the backup files over the source files and then deletes the backup files.
		/// </summary>
		public void RestoreFiles()
		{
			for (int i = m_UndoFileStack.Count; --i >= 0; )
			{
				UndoFiles frame = (UndoFiles)m_UndoFileStack[i];
				System.IO.FileInfo fi = new System.IO.FileInfo(frame.backupFile);
				if (fi.Exists)
					//				if (File.Exists(frame.backupFile))
				{
					// Use the safe versions of the methods so that the process can continue
					// even if there are errors.
					Generic.SafeFileCopyWithLogging(frame.backupFile, frame.originalFile, true);
					if (fi.Length <= 0)
					{
						// no data in the backup file, remove originalFile also
						Generic.SafeDeleteFile(frame.originalFile);
					}
					Generic.SafeDeleteFile(frame.backupFile);
				}
			}
		}

		#endregion

		#region member variables

		/// <summary>
		/// The parsed Language Data XML file.
		/// LocaleFileClass represents ONLY ONE LOCALE so this stores the shared parsed LD file for this locale.
		/// Both InstallLDFile and InstallPUACharacters will initialize this if it is null.
		/// </summary>
		/// <returns></returns>
		private Parser m_ldData=null;
		/// <summary>
		/// The name of the file that is currently parsed into m_ldData
		/// </summary>
		private string m_ldFilename = "";

		private string m_IcuData;			// this is the ICU Generated prefix for res files
		private string m_icuBase;
		private string m_localeDirectory;
		private string m_collDirectory;

		private string m_LocaleName;		// name of the ICU Custom Locale resource
		private string m_LanguageName;		// name of the ICU Custom Language resource
		private string m_Script;			// name of the ICU Custom Script resource
		private string m_Country;			// name of the ICU Custom Country resource
		private string m_Variant;			// name of the ICU Custom Variant resource

		private string m_sLanguageNamesAdded;
		private string m_sScriptNamesAdded;
		private string m_sCountryNamesAdded;
		private string m_sVariantNamesAdded;

		private string m_StartComment;		// comment line at start of custom resource
		private string m_EndComment;		// comment line at end of custom resource

		private Regex  m_StartCommentRegex;	// compiled RE for Start of custom resource
		private Regex  m_EndCommentRegex;	// compiled RE for End of custom resource
		private string NL = Environment.NewLine;

		private bool m_RunInSilentMode;		// don't show any GUI
		private bool m_runSlow;				// use the old slower file reading method

		/// <summary>
		/// The comment string to insert in files to indicate that the line was added by the given xml file.
		/// (Note: the -o flag installs more than one comment and this variable must be kept up to date.)
		/// Doesn't include the "#" or "//" to allow a more generic use
		/// </summary>
		private string m_comment;

		#endregion

		#region install language helper functions and member variables

		public bool RunSilent
		{
			get {return m_RunInSilentMode;}
			set {m_RunInSilentMode = value;}
		}

		/// <summary>
		/// Indicates whether we should use the new faster file parsing method or not.
		/// </summary>
		public bool RunSlow
		{
			get {return m_runSlow;}
			set {m_runSlow = value;}
		}

		///-------------------------------------------------------------------------------------
		///-------------------------------------------------------------------------------------
		public LocaleFileClass()
		{
			m_RunInSilentMode = false;		// default to NOT silent mode
			m_entries = new Hashtable();
			m_keys = new ArrayList();
			m_UndoFileStack = new ArrayList();
			m_TempFiles = new ArrayList();

			RegexOptions opts = RegexOptions.Compiled; // Regex options

			reStartEntry     = new Regex(@"^[\s]*[\S]+[\s]*{", opts);
			reResource       = new Regex(@"^[\s]*[\S]*[\s]*{", opts);
			reResourceQuoted = new Regex(@"^[\s]*""[\S]*""[\s]*{", opts);
			reClosingBrace   = new Regex(@"^[\s]*}[\s]*$",     opts);
			reCommentLine    = new Regex(@"^[\s]*//",          opts);
			reStringLiteral  = new Regex(@"^[\s]*""",          opts);
			reSpecialTag	 = new Regex(@"^[\s]*""%%[\S\s]*{[\S\s]*}",	  opts);
			reTransLit		 = new Regex(@"^[\s]*""%Translit%\S*""",	  opts);

			m_icuBase = Generic.GetIcuDir(); // + "\\icu28\\";
			m_localeDirectory = m_icuBase + "Data\\Locales\\";
			m_collDirectory = m_icuBase + "Data\\Coll\\";
			m_IcuData = Generic.GetIcuData(); // eg: icudt34l\

			m_LocaleName = "LocalesAdded";		// name of the ICU Custom Locale resource
			m_LanguageName = "LanguagesAdded";	// name of the ICU Custom Language resource
			m_Script = "ScriptsAdded";			// name of the ICU Custom Script resource
			m_Country = "CountriesAdded";		// name of the ICU Custom Country resource
			m_Variant = "VariantsAdded";		// name of the ICU Custom Variant resource

			m_sLanguageNamesAdded = "LanguageNamesAdded";	// more ICU Custom resource names
			m_sScriptNamesAdded = "ScriptNamesAdded";
			m_sCountryNamesAdded = "CountryNamesAdded";
			m_sVariantNamesAdded = "VariantNamesAdded";

			m_StartComment = "// PLEASE DO NOT MODIFY - START" ;
			m_EndComment   = "// PLEASE DO NOT MODIFY - END";

			m_StartCommentRegex = new Regex(@"[\s|\S]+" + m_StartComment,
				RegexOptions.Compiled | RegexOptions.Multiline);
			m_EndCommentRegex = new Regex(@"[\s|\S]+" + m_EndComment,
				RegexOptions.Compiled | RegexOptions.Multiline);

		}

		private Hashtable m_entries;
		private ArrayList m_keys;

		private Regex reStartEntry;
		private Regex reResource;
		private Regex reResourceQuoted;
		private Regex reClosingBrace;
		private Regex reCommentLine;
		private Regex reStringLiteral;
		private Regex reSpecialTag;
		private Regex reTransLit;

		/// <summary>
		/// See "FindCustomResource"
		/// 		present => found key with supplied text, do not modify root.txt
		///         replace => found key with different text, modify root.txt
		///         insert  => did not find key, insert into root.txt file</returns>
		/// </summary>
		public enum eAction {present, replace, insert, not_defined};



		private string FileToString(string inputFile)
		{
			LogFile.AddVerboseLine("StreamReader on <" + inputFile + ">");
			StreamReader inputStream = new StreamReader(inputFile,
				System.Text.Encoding.Default, true);	// Check for Unicode BOM chars.
			string fileData = inputStream.ReadToEnd();
			inputStream.Close();
			return fileData;
		}

		private void StringToFile( string inputFile, string data)
		{
			StreamWriter sw = new StreamWriter(inputFile, false, System.Text.Encoding.UTF8);
			sw.Write(data);
			sw.Close();
		}

		private string GetISO3Country(string locale)
		{
			StringUtils.InitIcuDataDir();	// initialize ICU data dir
			string isoCountry = Icu.GetISO3Country(locale);
			return isoCountry;
		}

		private string GetISO3Language(string locale)
		{
			StringUtils.InitIcuDataDir();	// initialize ICU data dir
			string isoLang = Icu.GetISO3Language(locale);
			return isoLang;
		}

		public void ParseLocaleName(ref string locale, // return case proper version
			out string lang, out string script, out string country, out string variant)
		{
			// use a single class/object for locale parsing - LocaleParser
			LocaleParser lp = new LocaleParser(locale);

			locale = lp.Locale;
			lang = lp.LangKey;
			script = lp.ScriptKey;
			country = lp.CountryKey;
			variant = lp.VariantKey;
		}


		public ICUInfo GetIcuResourceInfo(string inputFile, string locale, string icuLocale)
		{
			string lang, script, country, variant;
			#region Compute the Lang, Script, Country and Variant portion of the Locale
			ParseLocaleName(ref locale, out lang, out script, out country, out variant);
			#endregion

			bool bLanguage, bScript, bCountry, bVariant;	// C# defaults to 'false' at construction time

			// define all the nodes used for finding the resources in the XX.txt file
			NodeSpecification icuLanguages = new NodeSpecification(icuLocale, "Languages", lang);
			NodeSpecification icuScripts = new NodeSpecification(icuLocale, "Scripts", script);
			NodeSpecification icuCountries = new NodeSpecification(icuLocale, "Countries", country);
			NodeSpecification icuVariants = new NodeSpecification(icuLocale, "Variants", variant);

			bLanguage = ICUDataFiles.NodeExists(inputFile, icuLanguages);
			if (script.Length > 0)
				bScript = ICUDataFiles.NodeExists(inputFile, icuScripts);
			else
				bScript = false;	// not applicable if empty
			if (country.Length > 0)
				bCountry = ICUDataFiles.NodeExists(inputFile, icuCountries);
			else
				bCountry = false;	// not applicable if empty
			if (variant.Length > 0)
				bVariant = ICUDataFiles.NodeExists(inputFile, icuVariants);
			else
				bVariant = false;
			return new ICUInfo(locale, bLanguage, bScript, bCountry, bVariant);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a CustomResourceInfo object that contains the boolean properties for all
		/// of the locale portions existance in the custom resource in the passed in
		/// file (root.txt).
		/// </summary>
		/// <param name="inputFile">full path to root.txt file</param>
		/// <param name="locale">the locale to be parsed and looked for</param>
		/// <returns>an object (CustomResourceInfo) that contains the bool properties</returns>
		/// ------------------------------------------------------------------------------------
		public CustomResourceInfo GetCustomResourceInfo(string inputFile, string locale, string [] icuLocales)
		{
			string lang, script, country, variant;
			#region Compute the Lang, Script, Country and Variant portion of the Locale
			ParseLocaleName(ref locale, out lang, out script, out country, out variant);
			#endregion

			bool bCustom, bLocale, bLanguage, bScript, bCountry, bVariant;	// C# defaults to 'false' at construction time

			// define all the nodes used for finding the custom resources in the root.txt file
			NodeSpecification custom          = new NodeSpecification("root", "Custom");
			NodeSpecification customLocales   = new NodeSpecification("root", "Custom", "LocalesAdded");
			NodeSpecification customLanguages = new NodeSpecification("root", "Custom", "LanguagesAdded", lang);
			NodeSpecification customScripts   = new NodeSpecification("root", "Custom", "ScriptsAdded", script);
			NodeSpecification customCountries = new NodeSpecification("root", "Custom", "CountriesAdded", country);
			NodeSpecification customVariants  = new NodeSpecification("root", "Custom", "VariantsAdded", variant);

			// now set all the boolean values for each custom type
			bCustom = ICUDataFiles.NodeExists(inputFile, custom);
			bLocale = ICUDataFiles.AttributeExists(inputFile, customLocales, locale);

			// The following custom properties are associated with each Xx.txt file,
			//  so only say it's there if it exists and has all the wsNames as attributes.
			if (icuLocales != null && icuLocales.Length > 0)
			{
				// Start at true and set to false if any aren't found
				bLanguage = bScript = bCountry = bVariant = true;
				foreach (string name in icuLocales)
				{
					bLanguage = ICUDataFiles.AttributeExists(inputFile, customLanguages, name) && bLanguage;
					if (script.Length > 0)
						bScript = ICUDataFiles.AttributeExists(inputFile, customScripts, name) && bScript;
					else
						bScript = false;	// not applicable if empty
					if (country.Length > 0)
						bCountry = ICUDataFiles.AttributeExists(inputFile, customCountries, name) && bCountry;
					else
						bCountry = false;	// not applicable if empty
					if (variant.Length > 0)
						bVariant = ICUDataFiles.AttributeExists(inputFile, customVariants, name) && bVariant;
					else
						bVariant = false;
				}
			}
			else
			{
				bLanguage = ICUDataFiles.NodeExists(inputFile, customLanguages);
				bScript = ICUDataFiles.NodeExists(inputFile, customScripts);
				bCountry = ICUDataFiles.NodeExists(inputFile, customCountries);
				bVariant = ICUDataFiles.NodeExists(inputFile, customVariants);
			}
			#region Test code (commented out until moved to NUnit...)
			/*
			string testFile = "root {\r\n}";
			AddCustomResource(ref testFile);
			AddCustomCountry(ref testFile, "ZZZ");
			AddCustomCountry(ref testFile, "AAA");
			AddCustomCountry(ref testFile, "WWW");
			AddCustomCountry(ref testFile, "RRR");
			StreamWriter sw = new StreamWriter(@"C:\Dev\fwtexp\DistFiles\icu\data\locales\root_dlh_testA.txt");
			sw.Write(testFile);
			sw.Close();
			DeleteCustomCountry(ref testFile, "AAA");
			sw = new StreamWriter(@"C:\Dev\fwtexp\DistFiles\icu\data\locales\root_dlh_testB.txt");
			sw.Write(testFile);
			sw.Close();
			*/
			#endregion

			return new CustomResourceInfo(locale, bCustom, bLocale, bLanguage, bScript, bCountry, bVariant);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// A.	If custom locales are installed, remove .txt/.res version
		///		including original copies.
		/// B.	Copy any remaining original txt/res files to their original
		///		names.
		/// C.	For each language definition file in the languages
		///		directory, reinstall the ICU information
		/// </summary>
		/// <param name="installPuaCharatersToo">Whether the PUA Characters in the LDFs
		/// should be loaded as well</param>
		/// <returns>ErrorCodes</returns>
		/// ------------------------------------------------------------------------------------
		public ErrorCodes RestoreOrigFiles(bool installPuaCharatersToo)
		{
			ErrorCodes ret = ErrorCodes.Success;

			// read in the root.txt file for custom resource processing
			string fileData = FileToString(m_localeDirectory + "root.txt");


			// =================================================================
			// A.	If custom locales are installed, remove .txt/.res version
			//		including original copies.
			ArrayList customLocales = GetCustomLocales(fileData);
			foreach (string custlocale in customLocales)
			{
				string xxTxtFile = m_localeDirectory + custlocale + ".txt";
				string xxResFile = m_icuBase + m_IcuData + custlocale + ".res";
				string xxTxtFileOrig = Generic.MakeOrigFileName(xxTxtFile);
				string xxResFileOrig = Generic.MakeOrigFileName(xxResFile);

				Generic.DeleteFile(xxTxtFile);		// remove the xx.txt file
				Generic.DeleteFile(xxResFile);		// remove the xx.res file
				Generic.DeleteFile(xxTxtFileOrig);	// remove the xx.txt orig file
				Generic.DeleteFile(xxResFileOrig);	// remove the xx.res orig file
			}

			// =================================================================
			// B.	Copy any remaining original txt/res files to their original
			//		names.

			Generic.RestoreOrigFiles(m_localeDirectory, ".txt", true);
			Generic.RestoreOrigFiles(m_icuBase, ".res", true);

			// =================================================================
			// C.	For each language definition file in the languages
			//		directory, reinstall the ICU information
			//	-	First check to see if there is a same named file in the
			//		templates dir and copy that one here first, and make
			//		it writeable and then continue.

			DirectoryInfo di = new DirectoryInfo(Generic.GetIcuLanguageDir());
			System.IO.FileInfo[] fi = di.GetFiles("*.xml");
			string templatesPath = Generic.GetIcuTemplateDir();

			LogFile.AddLine("Reinstall the following LDFiles: ");

			foreach(System.IO.FileInfo f in fi)
			{
				try
				{
					string ldfName;
					ldfName = f.FullName;

					if (ldfName.EndsWith(".xml") == false)
						continue;

					// if this file name exists in the templates dir,
					//	copy it here
					//	make it writeable
					if (File.Exists(templatesPath + f.Name))
					{
						LogFile.AddLine("  Found same named file in Templates dir, using that one instead.");
						Generic.FileCopyWithLogging(templatesPath + f.Name, ldfName, true);
						FileAttributes attrib = File.GetAttributes(ldfName);
						if ((attrib & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
						{
							File.SetAttributes(ldfName, attrib ^ FileAttributes.ReadOnly);
						}
					}

					LogFile.AddLine("  " + ldfName);

					// installlanguage -i ldfName OR new
					InstallLDFile(ldfName);
					// installlanguage -c ldfName
					if(installPuaCharatersToo)
						InstallPUACharacters(ldfName);

					// remove temp files and backup files
					RemoveBackupFiles();
					RemoveTempFiles();
				}
				catch (LDExceptions e)
				{
					// remove temp files and backup files
					RemoveBackupFiles();
					RemoveTempFiles();
					ret = e.ec;
					LogFile.AddErrorLine("LDException: " + e.ec.ToString() + "-" + Error.Text(e.ec));
					if (e.HasConstructorText)
						LogFile.AddErrorLine("LDException Msg: " + e.ConstructorText);
				}
				// InstallLanguage -ii 5 dog.xml en.xml en_GB_EURO.xml fr__EURO.xml tl.xml
			}
			return ret;
		}



		#region Methods to interact with the Custom resource [add,delete,find] in root.txt

		public ArrayList GetCustomLocales(string data)
		{
			ArrayList Locales = new ArrayList();

			Regex reAllLocales = new Regex(@"^\s+LocalesAdded\s*{(?<locales>[^}]*)", RegexOptions.Multiline);
			Regex reLocale = new Regex(@"^\s+?""(?<key>[^""]+)""", RegexOptions.Multiline);

			Match matchCustLocales = reAllLocales.Match(data);
			if (matchCustLocales.Success)
			{
				string locales = matchCustLocales.Groups["locales"].Value;
				if (locales.Length > 0)		// fond the section, now pull out the locales
				{
					MatchCollection mc = reLocale.Matches(locales);
					for (int i = 0; i < mc.Count; i++)
					{
						Locales.Add(mc[i].Groups["key"].Value);
					}
				}
			}

			System.Diagnostics.Debug.Write("Found the following Custom Locales: ", "Root.txt");
			foreach (string s in Locales)
			{
				System.Diagnostics.Debug.Write(s + ", ");
			}
			if (Locales.Count == 0 )
				System.Diagnostics.Debug.WriteLine("None");
			else
				System.Diagnostics.Debug.WriteLine("");

			return Locales;
		}


		/// <summary>
		/// Print out the added Custom locales in a human readable format.
		/// Prints directly from the "Custom" block (shows the comments)
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public ArrayList GetCustomLocalesWithDates(string data, string parentKeyName)
		{
			ArrayList Locales = new ArrayList();

			Regex reAllLocales = new Regex(@"^\s+" + parentKeyName + @"\s*{(?<locales>[^}]*)", RegexOptions.Multiline);
			Regex reLocale = new Regex(@"^\s+?""(?<key>[^""]+)"",\s*//(?<comment>[\s|\S]+?)\r\n", RegexOptions.Multiline);

			Match matchCustLocales = reAllLocales.Match(data);
			if (matchCustLocales.Success)
			{
				string locales = matchCustLocales.Groups["locales"].Value;
				// found the section, now pull out the locales
				if (locales.Length > 0)
				{
					MatchCollection mc = reLocale.Matches(locales);
					for (int i = 0; i < mc.Count; i++)
					{
						Locales.Add("<" + mc[i].Groups["key"].Value + ">" + mc[i].Groups["comment"].Value);
					}
				}
			}

			System.Diagnostics.Debug.Write("Found the following Custom Locales: ", "Root.txt");
			foreach (string s in Locales)
			{
				System.Diagnostics.Debug.Write(s + ", ");
			}
			if (Locales.Count == 0 )
				System.Diagnostics.Debug.WriteLine("None");
			else
				System.Diagnostics.Debug.WriteLine("");

			return Locales;
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create the new empty (but valid) Custom resource section in the root file.
		/// </summary>
		/// <param name="fileData">string that gets the section added</param>
		/// <returns>true if successfull</returns>
		/// ------------------------------------------------------------------------------------
		private bool AddCustomResource(ref string fileData)
		{
			string regExprString;
			regExprString  = "(?<header>[\\s|\\S]*?)";		// header / pre-root portion
			regExprString += "(?<root>root[\\s]*?{)";		// start of root portion
			regExprString += "(?<rest>[\\s|\\S]+)";			// rest of the string

			Regex re = new Regex(regExprString);
			Match m = re.Match(fileData);

			if ( m.Groups.Count != 4 )	// 3 groups + expression
				throw new LDExceptions(ErrorCodes.RootTxt_InvalidCustomResourceFormat);

			string indent1 = "    ";		// four spaces to match current ICU format
			string newLine = NL;
			string indent2 = indent1 + indent1;
			string indent3 = indent2 + indent1;
			string closeSection = indent2 + "}" + NL;
			string exeName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;

			// build custom resource section string
			System.Globalization.CultureInfo ci = System.Globalization.CultureInfo.CreateSpecificCulture("en-US");
			string custom = NL;
			custom += indent1 + m_StartComment + NL;
			custom += indent1 + "// This section is maintained by the '" + exeName;
			custom += "' Application." + NL;
			// Note we can't use local culture info as Korean/Chinese, etc. will introduce utf-8
			// characters that will cause icu tools to fail. {THIS IS NO LONGER TRUE WITH ICU 3.4!)
			custom += indent1 + "// Created: " + DateTime.Now.ToString("F", ci) + NL;
			custom += indent1 + "Custom {" + NL;
			custom += indent2 + m_LocaleName + " {" + NL + closeSection;
			custom += indent2 + m_LanguageName + " {" + NL + closeSection;
			custom += indent2 + m_Script+ " {" + NL + closeSection;
			custom += indent2 + m_Country + " {" + NL + closeSection;
			custom += indent2 + m_Variant + " {" + NL + closeSection;
			custom += indent1 + "}" + NL;
			custom += indent1 + m_EndComment + NL + NL;

			fileData  = m.Groups["header"].Value;
			fileData += m.Groups["root"].Value;
			fileData += custom;
			fileData += m.Groups["rest"].Value;

			return true;
		}

		#region Add methods
		/// ------------------------------------------------------------------------------------
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// add the specified resource type to the custom resource section of the input string.
		/// </summary>
		/// <param name="fileData">This is the input string to search.</param>
		/// <param name="resourceName">The type name of the resource.</param>
		/// <param name="keyName">The key value to add to the resource section.</param>
		/// <returns>True if successful, else throws exception</returns>
		/// ------------------------------------------------------------------------------------
		/// ------------------------------------------------------------------------------------
		private bool AddCustomResource(ref string fileData, string resourceName, string keyName)
		{
			string regExprString;
			regExprString  = "(?<header>[\\s|\\S]*)";				// header / pre-root portion
			regExprString += "(?<root>root[\\s]*{[\\s|\\S]+)";		// root portion
			regExprString += "(?<custom>Custom[\\s]*{[\\s|\\S]+)";	// Custom portion
			regExprString += "(?<resourceData>" + resourceName + "[\\s]*?{[\\s|\\S]+?)";	// data for resource
			regExprString += "(?<resourceDataEnd>\"\",)";			// find the "", entry
			regExprString += "(?<spacing>[\\s|\\S]+?)";				// needed for alignment
			regExprString += "(?<rest>}[\\s|\\S]+)";				// rest of the string

			Regex re = new Regex(regExprString);
			Match m = re.Match(fileData);

			if ( m.Groups.Count != 8 )	// 7 groups + expression
				throw new LDExceptions(ErrorCodes.RootTxt_InvalidCustomResourceFormat);

			string spc = m.Groups["spacing"].Value;
			string indent = "    ";

			fileData  = m.Groups["header"].Value;
			fileData += m.Groups["root"].Value;
			fileData += m.Groups["custom"].Value;
			fileData += m.Groups["resourceData"].Value;
			fileData += "\"" + keyName + "\",";
			fileData += indent + "// added " + DateTime.Now.ToString();	// date comment
			fileData += spc + indent ;
			fileData += m.Groups["resourceDataEnd"].Value + spc;
			fileData += m.Groups["rest"].Value;

			return true;
		}


		public bool AddCustomLocale(ref string fileData, string keyName)
		{
			return AddCustomResource(ref fileData, m_LocaleName, keyName);
		}

		public bool AddCustomLanguage(ref string fileData, string keyName)
		{
			return AddCustomResource(ref fileData, m_LanguageName, keyName);
		}

		public bool AddCustomScript(ref string fileData, string keyName)
		{
			return AddCustomResource(ref fileData, m_Script, keyName);
		}

		public bool AddCustomCountry(ref string fileData, string keyName)
		{
			return AddCustomResource(ref fileData, m_Country, keyName);
		}

		public bool AddCustomVariant(ref string fileData, string keyName)
		{
			return AddCustomResource(ref fileData, m_Variant, keyName);
		}
		#endregion

		#region Delete methods
		/// ------------------------------------------------------------------------------------
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Delete the specified resource type to the custom resource section of the input string.
		/// </summary>
		/// <param name="fileData">This is the input string to search.</param>
		/// <param name="resourceName">The type name of the resource.</param>
		/// <param name="keyName">The key value to delete to the resource section.</param>
		/// <returns>True if successful, else throws exception</returns>
		/// ------------------------------------------------------------------------------------
		/// ------------------------------------------------------------------------------------
		private bool DeleteCustomResource(ref string fileData, string resourceName, string keyName)
		{
			string regExprString;
			regExprString  = "(?<header>[\\s|\\S]*)";				// header / pre-root portion
			regExprString += "(?<root>root[\\s]*{[\\s|\\S]+)";		// root portion
			regExprString += "(?<custom>Custom[\\s]*{[\\s|\\S]+)";	// Custom portion
			regExprString += "(?<resourceData>" + resourceName + "[\\s]*?{[\\s|\\S]+?)";	// data for resource
			regExprString += "(?<key>\"" + keyName +"\",[[\\s|\\S]]*?)";			// find the key entry
			regExprString += "(?<rest>\"[\\s|\\S]+)";				// rest of the input string

			Regex re = new Regex(regExprString, RegexOptions.Compiled | RegexOptions.Multiline);
			Match m = re.Match(fileData);
			if (!m.Success)
				throw new LDExceptions(ErrorCodes.RootTxt_CustomResourceNotFound);

			if ( m.Groups.Count != 7 )	// 6 groups + expression
				throw new LDExceptions(ErrorCodes.RootTxt_InvalidCustomResourceFormat);

			fileData  = m.Groups["header"].Value;
			fileData += m.Groups["root"].Value;
			fileData += m.Groups["custom"].Value;
			fileData += m.Groups["resourceData"].Value;
			// don't put out the key
			fileData += m.Groups["rest"].Value;

			return true;
		}


		public bool DeleteCustomLocale(ref string fileData, string keyName)
		{
			return DeleteCustomResource(ref fileData, m_LocaleName, keyName);
		}

		public bool DeleteCustomLanguage(ref string fileData, string keyName)
		{
			return DeleteCustomResource(ref fileData, m_LanguageName, keyName);
		}

		public bool DeleteCustomCountry(ref string fileData, string keyName)
		{
			return DeleteCustomResource(ref fileData, m_Country, keyName);
		}

		public bool DeleteCustomVariant(ref string fileData, string keyName)
		{
			return DeleteCustomResource(ref fileData, m_Variant, keyName);
		}

		/// ------------------------------------------------------------------------------------
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Delete the specified resource type to the custom resource section of the input string.
		/// </summary>
		/// <param name="fileData">This is the input string to search.</param>
		/// <param name="resourceName">The type name of the resource.</param>
		/// <param name="keyName">The key value to delete to the resource section.</param>
		/// <returns>True if successful, else throws exception</returns>
		/// ------------------------------------------------------------------------------------
		/// ------------------------------------------------------------------------------------
		private bool DeleteCustomResourceSection(ref string fileData, string resourceName, string keyName)
		{
			string regExprString;
			regExprString  = "(?<header>[\\s|\\S]*)";				// header / pre-root portion
			regExprString += "(?<root>root[\\s]*{[\\s|\\S]+)";		// root portion
			regExprString += "(?<custom>Custom[\\s]*{[\\s|\\S]+)";	// Custom portion
			regExprString += "(?<resourceData>" + resourceName + "[\\s]*?{[\\s|\\S]+?)";	// data for resource
			regExprString += "(?<key>" + keyName +"[\\s]*?{[\\s|\\S]+?}\r\n)";			// find the key section
			regExprString += "(?<rest>[\\s]*?}[\\s|\\S]+)";				// rest of the input string

			Regex re = new Regex(regExprString, RegexOptions.Compiled | RegexOptions.Multiline);
			Match m = re.Match(fileData);
			if (!m.Success)
				throw new LDExceptions(ErrorCodes.RootTxt_CustomResourceNotFound);

			if ( m.Groups.Count != 7 )	// 6 groups + expression
				throw new LDExceptions(ErrorCodes.RootTxt_InvalidCustomResourceFormat);

			fileData  = m.Groups["header"].Value;
			fileData += m.Groups["root"].Value;
			fileData += m.Groups["custom"].Value;
			fileData += m.Groups["resourceData"].Value;
			// don't put out the key
			fileData += m.Groups["rest"].Value;

			return true;
		}

		public bool DeleteCustomLanguageNamesAdded(ref string fileData, string keyName)
		{
			return DeleteCustomResourceSection(ref fileData, m_sLanguageNamesAdded, keyName);
		}
		public bool DeleteCustomScriptNamesAdded(ref string fileData, string keyName)
		{
			return DeleteCustomResourceSection(ref fileData, m_sScriptNamesAdded, keyName);
		}
		public bool DeleteCustomCountryNamesAdded(ref string fileData, string keyName)
		{
			return DeleteCustomResourceSection(ref fileData, m_sCountryNamesAdded, keyName);
		}
		public bool DeleteCustomVariantNamesAdded(ref string fileData, string keyName)
		{
			return DeleteCustomResourceSection(ref fileData, m_sVariantNamesAdded, keyName);
		}
		#endregion

		#region Find method(s)
		/// ------------------------------------------------------------------------------------
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// search through the string looking for the passed in resource type and key:
		/// eg. "Countries", "us".
		/// </summary>
		/// <param name="fileData">This is the input string to search.</param>
		/// <param name="resourceType">The type name of the resource.</param>
		/// <param name="key">The key value to look for in the resource.</param>
		/// <returns>eAction.present if found and  not_defined if not found.</returns>
		/// ------------------------------------------------------------------------------------
		/// ------------------------------------------------------------------------------------
		private eAction FindCustomResource(string fileData, string resourceType, string key)
		{
			string regExprString;
			regExprString = "[\\s|\\S]*?";	// find everything up to custom
			//			regExprString += @"root[\s]*?{[\s|\S]+?";
			regExprString += @"Custom[\s]*?{[\s|\S]+?";
			if (resourceType.Length > 0)
			{
				regExprString += resourceType + @"[\s]*?{[\s|\S]+?";
				regExprString += "\"" + key + "\"";
			}

			Regex re = new Regex(regExprString, RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.ECMAScript);
			/// --------------------------------------------------------------------------------
			/// --------------------------------------------------------------------------------
			// This statement will hang for some reason if it doesn't fine a match, so
			// add one at the end and just filter it out at the end.
			// string found = re.Match(fileData).ToString();
			string found="";
			if (resourceType.Length > 0)
				found = re.Match(fileData + "\"" + key + "\"").ToString();	// always find one
			else
				found = re.Match(fileData + "Custom { ").ToString();	// always find one

			if (found.Length > 0 && found.Length < fileData.Length)
			{
				//				Match m = m_StartCommentRegex.Match(fileData,0);
				//				string startFound = m.ToString();
				string startFound = m_StartCommentRegex.Match(fileData).ToString();
				string endFound = m_EndCommentRegex.Match(fileData).ToString();
				if (found.Length > startFound.Length && found.Length < endFound.Length )
					return eAction.present;	// has to be withing the custom resource
			}
			return eAction.not_defined;
		}
		#endregion

		#endregion


		///-------------------------------------------------------------------------------------
		/// <summary>
		/// Find a given resource in the file.
		/// </summary>
		/// <param name="inputFilespec">name of input file</param>
		/// <param name="topKey">Primary resource name</param>
		/// <param name="key">Key in the resource</param>
		/// <param name="text">Text in the key in the resource</param>
		/// <param name="lineOfKey">returns line where the key was found</param>
		/// <returns>present => found key with supplied text, do not modify root.txt
		///          replace => found key with different text, modify root.txt
		///          insert  => did not find key, insert into root.txt file</returns>
		///-------------------------------------------------------------------------------------
		public eAction FindResource(string inputFilespec, string topKey, string key,
			string text, out int lineOfKey)
		{

			eAction ret = eAction.insert;
			lineOfKey = 0;

			LogFile.AddVerboseLine("StreamReader on <" + inputFilespec + ">");
			StreamReader inputStream = new StreamReader(inputFilespec,
				System.Text.Encoding.Default, true);	// Check for Unicode BOM chars.

			try
			{
				string newLine;
				int lineCount = 0;
				bool fFoundResourceToSearch = false;
				int openColumn = 0;
				int closeColumn = 0;

				do
				{
					newLine = inputStream.ReadLine();
					if ((inputStream.Peek() > -1) && (newLine != null))
					{
						lineCount++;
						// Handle blank lines.
						if (newLine.Length == 0)
							continue;

						string s;
						// Handle commented lines.
						s = reCommentLine.Match(newLine).ToString();
						if (s.Length > 0)
							continue;

						// Handle lines starting with literal strings.
						s = reStringLiteral.Match(newLine).ToString();
						if (s.Length > 0)
						{
							string s1 = reResourceQuoted.Match(newLine).ToString();
							if (s1.Length <= 0)
								continue;
						}

						s = reClosingBrace.Match(newLine).ToString();
						if (fFoundResourceToSearch && (s.Length > 0))
						{
							lineOfKey = lineCount;
							break;
						}

						// Handle first line of resources.
						s = reResource.Match(newLine).ToString();
						if (s.Length <= 0)
							s = reResourceQuoted.Match(newLine).ToString();
						if (s.Length > 0)
						{
							s = s.Replace("{", "");
							s = s.Trim();

							if (s.Length > 0)
							{
								if (!fFoundResourceToSearch)
								{
									if (topKey == s)
									{
										fFoundResourceToSearch = true;
										//nesting = 1;
										continue;
									}
								}
								else
								{
									if ((s == "root") && (topKey == "Languages"))
										continue; // ignore

									if (key == s)
									{
										lineOfKey = lineCount;
										openColumn = newLine.IndexOf("\"");
										closeColumn = newLine.IndexOf("\"", openColumn + 1);

										string name;
										name = newLine.Substring(openColumn + 1,
											closeColumn - (openColumn + 1));

										if (name == text)
											ret = eAction.present;
										else
											ret = eAction.replace;
										break;
									}
									else
										continue;
								}
							}
						}
					}
				} while (newLine != null);
			}
			catch
			{
				Console.WriteLine("Error Finding Resource, should be ok.");
			}

			finally
			{
				inputStream.Close();
			}
			return ret;
		}


		/// <summary>
		/// Build an output file using the specified input file taking the action
		/// required to replace the specified line, or insert the text before the
		/// specified line.
		/// </summary>
		/// <param name="inputFilespec">The file we are reading from.</param>
		/// <param name="outputFilespec">The file we are modifying.</param>
		/// <param name="lineNumber">Line from the input file that we want to modify</param>
		/// <param name="newLineText">Text to replace or insert.</param>
		/// <param name="action">Currently only supports replace OR insert.</param>
		/// <param name="errorCode">Error to throw if it fails.</param>
		/// <returns>True if the action was performed on the file.</returns>
		public bool ModifyFile(string inputFilespec, string outputFilespec, int lineNumber,
			string newLineText,	eAction action, ErrorCodes errorCode)
		{
			bool rval = false;

			if ((action != eAction.insert) && (action != eAction.replace))
				return rval;

			if ((inputFilespec.Length <= 0) || (lineNumber < 0))
				return rval;

			try
			{
				Generic.DeleteFile(outputFilespec);
			}
			catch
			{
				string emsg = "Error while deleting file: " + outputFilespec;
				LogFile.AddErrorLine(emsg);
				throw new LDExceptions(ErrorCodes.FileWrite, emsg);
			}

			LogFile.AddVerboseLine("StreamReader on <" + inputFilespec + ">");
			StreamReader inputStream = new StreamReader(inputFilespec,
				System.Text.Encoding.Default, true);	// Check for Unicode BOM chars.
			int chT = inputStream.Peek();	// force autodetection of encoding.
			StreamWriter outputStream = null;
			try
			{
				outputStream = new StreamWriter(outputFilespec, false,
					inputStream.CurrentEncoding);
			}
			catch
			{
				throw new LDExceptions(errorCode);
			}

			string newLine;
			int lineCount = 0;
			while (inputStream.Peek() >= 0)
			{
				lineCount++;
				newLine = inputStream.ReadLine();
				if (lineCount == lineNumber)	// at the line in question
				{
					if (action == eAction.replace &&
						newLineText.Length > 0)
						outputStream.WriteLine(newLineText);	// write out the new line

					rval = true;
					if (action == eAction.insert)			// if eAction.insert
					{
						outputStream.WriteLine(newLineText);	// write out the new line
						outputStream.WriteLine(newLine);	// write out the line just read
					}
				}
				else
					outputStream.WriteLine(newLine);	// write out the line just read
			}
			inputStream.Close();
			outputStream.Close();
			return rval;
		}



		public void GenerateResFile(ArrayList files, string sTxtDir,
			string sResDir, ErrorCodes errorCodeIn, string callingRoutine)
		{
			ErrorCodes errorCode = errorCodeIn;
			LogFile.AddVerboseLine("*** Called GenerateResFile from " + callingRoutine);
			while (true)
			{
				try
				{
					// release any locked files first
					Icu.Cleanup();

					// Call icu\tools\genrb.exe to compile res_index.txt, root.txt and XXX.txt
					Process prc;
					prc= new Process();
					// genrb fails with quoted -i and -s arguments when the directory ends with backslash.
					string sIcuResDir = m_icuBase + m_IcuData;
					if (sIcuResDir.LastIndexOf("\\") == sIcuResDir.Length - 1)
						sIcuResDir = sIcuResDir.Substring(0, sIcuResDir.Length - 1);
					if (sTxtDir.LastIndexOf("\\") == sTxtDir.Length - 1)
						sTxtDir = sTxtDir.Substring(0, sTxtDir.Length - 1);
					if (sResDir.LastIndexOf("\\") == sResDir.Length - 1)
						sResDir = sResDir.Substring(0, sResDir.Length - 1);
					prc.StartInfo.FileName = m_icuBase + "tools\\genrb.exe ";
					// Arguments need directories surrounded in double quotes to cover spaces in path.
					StringBuilder bldr = new StringBuilder();
					bldr.AppendFormat("-v -k -d \"{0}\" -i \"{1}\" ", sResDir, sIcuResDir);
					bldr.AppendFormat("-s \"{0}\"", sTxtDir);

					foreach (string FileName in files)
						bldr.AppendFormat(" {0}", FileName.Substring(1 + FileName.LastIndexOf("\\")));

					prc.StartInfo.Arguments = bldr.ToString();
					prc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
					prc.StartInfo.WorkingDirectory = sTxtDir;
					LogFile.AddLine(" GenerateResFile: <" + prc.StartInfo.Arguments + ">");

					prc.Start();
					prc.WaitForExit();
					int ret;
					ret = prc.ExitCode;
					if (ret != 0)
					{
						// This should never happen anymore because we are generating the res files in
						// the Icu34 directory then moving them to Icu34\icudt34l where the files may be locked.
						// Now we are testing for locked files before we do any other file manipulation.
						if (ret == 4 && RunSilent == false)
						{
							LogFile.AddLine(" File Access Error: asking user to retry or Cancel.");

							// arguments to the MessageBox to ask user to retry or cancel
							string message = InstallLanguageStrings.ksStopAllFwApps;
							string caption = InstallLanguageStrings.ksInstallLanguageMsgCaption;
							MessageBoxButtons buttons = MessageBoxButtons.RetryCancel;
							MessageBoxIcon icon = MessageBoxIcon.Exclamation;
							MessageBoxDefaultButton defButton = MessageBoxDefaultButton.Button1;

							DialogResult result = MessageBox.Show(message, caption, buttons, icon, defButton);
							if(result == DialogResult.Retry)
								continue;	// repeat
							errorCode = ErrorCodes.CancelAccessFailure;
						}

						System.Console.WriteLine("GENRB returned error code " + ret.ToString());
						System.Console.WriteLine(Error.Text(errorCode));
						LogFile.AddErrorLine("GENRB returned error code " + ret.ToString());
						foreach(string FileName in files)
							System.Console.WriteLine(" " + FileName);
						throw new LDExceptions(errorCode);
					}
					break;
				}
				catch(Exception e)
				{
					System.Diagnostics.Debug.WriteLine(e.Message);
					throw new LDExceptions(errorCode, e.Message);
				}
			}
		}

		/// <summary>
		/// Shows the custom locales installed so far
		/// </summary>
		/// <returns></returns>
		public ErrorCodes ShowCustomLocales()
		{
			return ShowCustom(m_LocaleName);
		}

		/// <summary>
		/// Shows the custom languages installed so far
		/// </summary>
		/// <returns></returns>
		public ErrorCodes ShowCustomLanguages()
		{
			return ShowCustom(m_LanguageName);
		}

		/// <summary>
		/// Display the contents of the given Custom field in a human readable format.
		/// </summary>
		/// <param name="parentKeyName"></param>
		/// <returns></returns>
		public ErrorCodes ShowCustom(string parentKeyName)
		{
			ErrorCodes retVal = ErrorCodes.Success;	// hope for the best...

			string rootTxtInput = m_localeDirectory + "root.txt";
			string fileData = FileToString(rootTxtInput);

			// Get the active list of custom locales and find out
			// if it's ok to remove the lang, country, and variant.
			ArrayList customLocales = GetCustomLocalesWithDates(fileData, parentKeyName);

			// We have to manually echo all our lines so that they will appear
			// as standard out without any logfile timestamps
			if (customLocales.Count == 0)
			{
				LogFile.AddLine("There are currently 'NO' custom locales installed.");
				Console.WriteLine("There are currently 'NO' custom locales installed.");
			}
			else
			{
				LogFile.AddLine("The following " + customLocales.Count.ToString() +
					" custom locales are installed:");
				Console.WriteLine("The following " + customLocales.Count.ToString() +
					" custom locales are installed:");
			}

			foreach (string custlocale in customLocales)
			{
				Console.WriteLine(custlocale);
				LogFile.AddLine(custlocale);
			}

			return retVal;
		}


		public class xxFileInfo
		{
			public xxFileInfo(string name)
			{
				xxName = name;
			}
			public string xxName;		// "en", "df", "fr", ...
			public string xxLangKey;		// lang portion of ICU Locale
			public string xxScriptKey;		// script portion of ICU Locale
			public string xxCountryKey;		// country portion of ICU Locale
			public string xxVariantKey;		// variant portion of ICU Locale
			public string txtName;		// input txt file name
			public string resName;		// res file name
			public string txtNameTemp;	// temporary work file name
			public bool rmFromLang;		// true if it is to be removed from the Languages section
			public bool rmFromScript;	// true if it is to be removed from the Scripts section
			public bool rmFromCountry;	// true if it is to be removed from the Countries section
			public bool rmFromVariant;	// true if it is to be removed from the Variants section
		}

		public enum LocalePortion {Lang, Script, Country, Variant};
		private string LocalePortionAsString(LocalePortion portion)
		{
			string name = "";
			switch (portion)
			{
				case LocalePortion.Lang:
					name = "LanguagesAdded";
					break;
				case LocalePortion.Variant:
					name = "VariantsAdded";
					break;
				case LocalePortion.Country:
					name = "CountriesAdded";
					break;
				case LocalePortion.Script:
					name = "ScriptsAdded";
					break;
			}
			return name;
		}

		private void GetXXLangsForICULocalePortion(IcuDataNode rootNode, LocalePortion portion, string localeKey, ref Hashtable xxFiles)
		{
			string customChild = LocalePortionAsString(portion);
			NodeSpecification node = new NodeSpecification("root", "Custom", customChild, localeKey);
			IcuDataNode dataNode = node.FindNode(rootNode, false);
			if (dataNode != null)
			{
				// get the list of xx.txt files to edit in this section
				foreach (IcuDataNode.IcuDataAttribute attr in dataNode.Attributes)
				{
					xxFileInfo xxFile;
					string xx = attr.StringValue;	// the lang code {ex: "en"}
					if (xxFiles.ContainsKey(xx))
					{
						xxFile = xxFiles[xx] as xxFileInfo;
					}
					else
					{
						xxFile = new xxFileInfo(xx);
						xxFiles[xx] = xxFile;
					}
					if (portion == LocalePortion.Variant)
					{
						xxFile.rmFromVariant = true;
						xxFile.xxVariantKey = localeKey;
					}
					else if (portion == LocalePortion.Lang)
					{
						xxFile.rmFromLang = true;
						xxFile.xxLangKey = localeKey;
					}
					else if (portion == LocalePortion.Script)
					{
						xxFile.rmFromScript = true;
						xxFile.xxScriptKey = localeKey;
					}
					else if (portion == LocalePortion.Country)
					{
						xxFile.rmFromCountry = true;
						xxFile.xxCountryKey = localeKey;
					}
				}
				// remove the key from the section of custom in root.txt
				if (!ICUDataFiles.RemoveICUDataFileChild(rootNode,
					new NodeSpecification("root", "Custom", customChild),
					localeKey))
				{
					LogFile.AddVerboseLine("***Unable to remove <"+localeKey+"> from the <"+customChild+"> section in the root.txt");
				}

			}
		}

		public ErrorCodes RemoveLocale(string locale)
		{
			ErrorCodes retVal = ErrorCodes.Success;	// hope for the best...
			LogFile.AddLine("Removing Locale: <" + locale + ">");

			try
			{
				// create the core 6 files:
				// root.txt, res_index.txt, xx.txt,
				// icu26ldt_root.res, icu26ldt_res_index.res, icu26ldt_xx.res
				//
				string rootTxtInput = m_localeDirectory + "root.txt";
				string resIndexTxtInput = m_localeDirectory + "res_index.txt";
				string rootResInput = m_icuBase + m_IcuData + "root.res";
				string resIndexResInput = m_icuBase + m_IcuData + "res_index.res";
				string xxTxtFile = m_localeDirectory + locale + ".txt";
				string xxCollTxtFile = m_collDirectory + locale + ".txt";
				string xxResFile = m_icuBase + m_IcuData + locale + ".res";
				string xxXMLFile = Path.Combine(
					DirectoryFinder.GetFWDataSubDirectory("Languages"), locale + ".xml");
				string [] icuLocale = new string[1];
				icuLocale[0] = "en";	// locale;

				// the root file text has to exist for this process to work - throw exception
				if (!File.Exists(rootTxtInput))
					throw new LDExceptions(ErrorCodes.RootTxt_FileNotFound);

				// the res index file has to exist for this process to work - throw exception
				if (!File.Exists(resIndexTxtInput))
					throw new LDExceptions(ErrorCodes.ResIndexTxt_FileNotFound);


				CustomResourceInfo newLangInfo = GetCustomResourceInfo(rootTxtInput, locale, icuLocale);

				if (LogFile.IsLogging())	// put out some debuging info
				{
					LogFile.AddLine("Locale  : <" + newLangInfo.LocaleItems.Locale + ">");
					LogFile.AddLine("Language: <" + newLangInfo.LocaleItems.LangKey + ">");
					LogFile.AddLine("Script  : <" + newLangInfo.LocaleItems.ScriptKey + ">");
					LogFile.AddLine("Country : <" + newLangInfo.LocaleItems.CountryKey + ">");
					LogFile.AddLine("Variant : <" + newLangInfo.LocaleItems.VariantKey + ">");

					string custom = "";
					if (newLangInfo.HasLocale)
						custom += " Locale";
					if (newLangInfo.HasLanguage)
						custom += " Language";
					if (newLangInfo.HasScript)
						custom += " Script";
					if (newLangInfo.HasCountry)
						custom += " Country";
					if (newLangInfo.HasVariant)
						custom += " Variant";
					if (custom.Length <= 0)
						custom = " None";

					LogFile.AddLine("Components that exist in the custom resource:" + custom);
				}

				// DN-246 1. see if it's a factory locale - if so, do Nothing, just return
				if (newLangInfo.HasLocale == false &&
					(File.Exists(xxTxtFile) || File.Exists(xxCollTxtFile)))
				{
					LogFile.AddLine("It's a factory Locale - remove only the XML file we created.");
					Generic.DeleteFile(xxXMLFile);
					return retVal;	// factory locale
				}
				else if (newLangInfo.HasCustom == false)	// no custom section, what to do...?
				{
					LogFile.AddLine("The root.txt file doesn't contain the Custom Resource section - remove only the XML file we created.");
					Generic.DeleteFile(xxXMLFile);
					return retVal;
				}

				// A.
				// Create the Original backups
				Generic.BackupOrig(rootTxtInput);
				Generic.BackupOrig(rootResInput);
				Generic.BackupOrig(resIndexTxtInput);
				Generic.BackupOrig(resIndexResInput);

				// B.
				// Create the temporary files for the input/output files

				// For the RootTxtInput file (root.txt) create a temp file and a backup up file.
				string rootTxtTemp = Generic.CreateTempFile(rootTxtInput);
				AddTempFile(rootTxtTemp);
				string rootTxtBackup = Generic.CreateBackupFile(rootTxtInput);
				AddUndoFileFrame(rootTxtInput, rootTxtBackup);

				// For the RootResInput file (icudt26l_root.res) create a backup up file.
				string rootResBackup = Generic.CreateBackupFile(rootResInput);
				AddUndoFileFrame(rootResInput, rootResBackup);

				// For the ResIndexTxt input file (res_index.txt) create a temp file and a backup up file.
				string resIndexTxtTemp = Generic.CreateTempFile(resIndexTxtInput);
				AddTempFile(resIndexTxtTemp);
				string resIndexTxtBackup = Generic.CreateBackupFile(resIndexTxtInput);
				AddUndoFileFrame(resIndexTxtInput, resIndexTxtBackup);

				// For the ResIndexRes input file (icudt26l_res_index.res) create a Backup file.
				string resIndexResBackup = Generic.CreateBackupFile(resIndexResInput);
				AddUndoFileFrame(resIndexResInput, resIndexResBackup);

				// For the RootTxtInput file (XX_YY_ZZ.txt) create a temp file and a backup up file.
				string xxTxtTemp = Generic.CreateTempFile(xxTxtFile);
				AddTempFile(xxTxtTemp);
				string xxTxtBackup = Generic.CreateBackupFile(xxTxtFile);
				AddUndoFileFrame(xxTxtFile, xxTxtBackup);

				// For the XX_YY_ZZ.res input file (icudt26l_XX_YY_ZZ.res) create a backup file.
				string xxResBackup = Generic.CreateBackupFile(xxResFile);
				AddUndoFileFrame(xxResFile, xxResBackup);


				// Create the list of files to use with "genrb"
				ArrayList Files = new ArrayList();
				Files.Add(resIndexTxtTemp);
				Files.Add(rootTxtTemp);

				if (newLangInfo.HasLocale || newLangInfo.HasLanguage ||
					newLangInfo.HasScript || newLangInfo.HasCountry || newLangInfo.HasVariant)
				{
					if (newLangInfo.HasLocale)
					{
						if (ICUDataFiles.RemoveICUDataFileAttribute(rootTxtTemp,	//rootTxtInput,
							new NodeSpecification("root", "Custom", "LocalesAdded"),
							newLangInfo.LocaleItems.Locale) >= 0)
						{
							LogFile.AddLine("Removed custom locale entry.");
						}
						else
							LogFile.AddLine("***Unable to remove custom locale entry.");

					}

					// Get a list of current custom LoacalesAdded and build usage counts for
					// future deletions.
					IcuDataNode rootTxtTempNode = ICUDataFiles.ParsedFile(rootTxtTemp);
					IcuDataNode customLocalesNode = new NodeSpecification("root", "Custom", "LocalesAdded").FindNode(rootTxtTempNode, false);
					if( customLocalesNode == null )
						throw new Exception("Couldn't find the locale to remove");

					int langCount=0, scriptCount=0, countryCount=0, variantCount=0;
					foreach (IcuDataNode.IcuDataAttribute attr in customLocalesNode.Attributes)
					{
						string lang, script, country, variant;
						string templocale = attr.StringValue;
						ParseLocaleName(ref templocale, out lang, out script, out country, out variant);
						if (newLangInfo.HasLanguage && newLangInfo.LocaleItems.LangKey == lang)
							langCount++;

						if (newLangInfo.HasScript && newLangInfo.LocaleItems.ScriptKey == script)
							scriptCount++;

						if (newLangInfo.HasCountry && newLangInfo.LocaleItems.CountryKey == country)
							countryCount++;

						if (newLangInfo.HasVariant && newLangInfo.LocaleItems.VariantKey == variant)
							variantCount++;
					}

					Hashtable lstOfxxFiles = new Hashtable();
					if (newLangInfo.HasVariant && variantCount == 0)
					{
						GetXXLangsForICULocalePortion(rootTxtTempNode, LocalePortion.Variant, newLangInfo.LocaleItems.VariantKey, ref lstOfxxFiles);
						LogFile.AddLine("Removed custom variant entry.");
					}

					if (newLangInfo.HasCountry && countryCount == 0)
					{
						GetXXLangsForICULocalePortion(rootTxtTempNode, LocalePortion.Country, newLangInfo.LocaleItems.CountryKey, ref lstOfxxFiles);
						LogFile.AddLine("Removed custom country entry.");
					}

					if (newLangInfo.HasScript && scriptCount == 0)
					{
						GetXXLangsForICULocalePortion(rootTxtTempNode, LocalePortion.Script, newLangInfo.LocaleItems.ScriptKey, ref lstOfxxFiles);
						LogFile.AddLine("Removed custom script entry.");
					}

					if (newLangInfo.HasLanguage && langCount == 0)
					{
						GetXXLangsForICULocalePortion(rootTxtTempNode, LocalePortion.Lang, newLangInfo.LocaleItems.LangKey, ref lstOfxxFiles);
						LogFile.AddLine("Removed custom language entry.");
					}

					ICUDataFiles.WriteFile(rootTxtTemp/*rootTxtInput*/);

					foreach (DictionaryEntry de in lstOfxxFiles)
					{
						// fill in the remaining infor relating to these xx.txt files
						xxFileInfo xxfi = de.Value as xxFileInfo;
						xxfi.txtName = m_localeDirectory + xxfi.xxName + ".txt";
						xxfi.resName = m_icuBase + m_IcuData + xxfi.xxName + ".res";

						// Create the original backups
						Generic.BackupOrig(xxfi.txtName);
						Generic.BackupOrig(xxfi.resName);

						xxfi.txtNameTemp = Generic.CreateTempFile(xxfi.txtName);
						AddTempFile(xxfi.txtNameTemp);
						Files.Add(xxfi.txtNameTemp);

						string txtBackup = Generic.CreateBackupFile(xxfi.txtName);
						AddUndoFileFrame(xxfi.txtName, txtBackup);

						string resBackup = Generic.CreateBackupFile(xxfi.resName);
						AddUndoFileFrame(xxfi.resName, resBackup);
					}

					// now we need to actually edit the lang.txt files (EX: "en")
					foreach (DictionaryEntry de in lstOfxxFiles)
					{
						xxFileInfo xxfi = de.Value as xxFileInfo;
						IcuDataNode xxNode = ICUDataFiles.ParsedFile(xxfi.txtNameTemp);

						// remove the locale portion from the xx.txt file ("en.txt")
						if (xxfi.rmFromLang)
						{
							if (!ICUDataFiles.RemoveICUDataFileChild(xxNode,
								new NodeSpecification(xxfi.xxName, "Languages"), xxfi.xxLangKey))
							{
								LogFile.AddVerboseLine("***Unable to remove <"+xxfi.xxLangKey+"> from the <Languages> section in " + xxfi.xxName + ".txt");
							}
						}
						if (xxfi.rmFromScript)
						{
							if (!ICUDataFiles.RemoveICUDataFileChild(xxNode,
								new NodeSpecification(xxfi.xxName, "Scripts"), xxfi.xxScriptKey))
							{
								LogFile.AddVerboseLine("***Unable to remove <" + xxfi.xxScriptKey + "> from the <Scripts> section in " + xxfi.xxName + ".txt");
							}
						}
						if (xxfi.rmFromCountry)
						{
							if (!ICUDataFiles.RemoveICUDataFileChild(xxNode,
								new NodeSpecification(xxfi.xxName, "Countries"), xxfi.xxCountryKey))
							{
								LogFile.AddVerboseLine("***Unable to remove <" + xxfi.xxCountryKey + "> from the <Countries> section in " + xxfi.xxName + ".txt");
							}
						}
						if (xxfi.rmFromVariant)
						{
							if (!ICUDataFiles.RemoveICUDataFileChild(xxNode,
								new NodeSpecification(xxfi.xxName, "Variants"), xxfi.xxVariantKey))
							{
								LogFile.AddVerboseLine("***Unable to remove <"+xxfi.xxVariantKey+"> from the <Variants> section in " + xxfi.xxName + ".txt");
							}
						}
						ICUDataFiles.WriteFileAs(xxfi.txtNameTemp, xxfi.txtNameTemp);
					}

					// put the file back out - with custom resource changes
					GenerateResFile(Files, m_localeDirectory, m_icuBase + m_IcuData,
						ErrorCodes.GeneralFile, "RemoveLocale");

					Generic.FileCopyWithLogging(rootTxtTemp, rootTxtInput, true);
					Generic.FileCopyWithLogging(resIndexTxtTemp, resIndexTxtInput, true);
					foreach (DictionaryEntry de in lstOfxxFiles)
					{
						xxFileInfo xxfi = de.Value as xxFileInfo;
						Generic.FileCopyWithLogging(xxfi.txtNameTemp, xxfi.txtName, true);
					}

					// Need to clean up by deleting all of the _TEMP files.
					// ALWAYS try to Remove the XXX.txt, XXX.res and XXX.xml files.
					Generic.DeleteFile(xxTxtFile);
					Generic.DeleteFile(xxResFile);
					Generic.DeleteFile(xxXMLFile);
					// remove the backup files from the undo/recover file name list
					RemoveBackupFiles();
					LogFile.AddErrorLine("--- Successfully removed Locale ---");
				}
				else
				{
					// ALWAYS try to Remove the XXX.txt, XXX.res and XXX.xml files.
					Generic.DeleteFile(xxTxtFile);
					Generic.DeleteFile(xxResFile);
					Generic.DeleteFile(xxXMLFile);
					// remove the backup files from the undo/recover file name list
					RemoveBackupFiles();
					LogFile.AddErrorLine("NOTE -- NO Locale Data to Remove.");
				}

				RemoveCollation(locale);

				RemoveTempFiles();
			}
			catch (LDExceptions e)
			{
				RestoreFiles();		// copy backup files to original files.
				retVal = e.ec;
				LogFile.AddErrorLine("LDException: " + e.ec.ToString() + "-" + Error.Text(e.ec));
				if (e.HasConstructorText)
					LogFile.AddErrorLine("LDException Msg: " + e.ConstructorText);
			}
			catch
			{
				retVal = ErrorCodes.NonspecificError;
				LogFile.AddErrorLine("Exception: " + retVal.ToString() + "-" + Error.Text(retVal));
				RestoreFiles();	// copy backup files to original files.
			}

			if (retVal == ErrorCodes.Success)
			{
				RemoveBackupFiles();
				LogFile.AddErrorLine("--- Success ---");
			}

			return retVal;
		}

		private void RemoveCollation(string locale)
		{
			string xxTxtFile = m_collDirectory + locale + ".txt";
			if (!File.Exists(xxTxtFile))
				return;		// assume we don't have anything to do.
			LogFile.AddLine("Removing Collation: <" + locale + ">");

			string xxResFile = m_icuBase + m_IcuData + "coll\\" + locale + ".res";
			string resIndexTxtInput = m_collDirectory + "res_index.txt";
			string resIndexResInput = m_icuBase + m_IcuData + "coll\\res_index.res";

			// the res index file has to exist for this process to work - throw exception
			if (!File.Exists(resIndexTxtInput))
				throw new LDExceptions(ErrorCodes.ResIndexTxt_FileNotFound);

			// Create the Original backups
			Generic.BackupOrig(resIndexTxtInput);
			Generic.BackupOrig(resIndexResInput);

			// Create the temporary files for the input/output files
			// For the ResIndexTxt input file (res_index.txt) create a temp file and a backup up file.
			string resIndexTxtTemp = Generic.CreateTempFile(resIndexTxtInput);
			AddTempFile(resIndexTxtTemp);
			string resIndexTxtBackup = Generic.CreateBackupFile(resIndexTxtInput);
			AddUndoFileFrame(resIndexTxtInput, resIndexTxtBackup);
			// For the ResIndexRes input file (icudt26l_res_index.res) create a Backup file.
			string resIndexResBackup = Generic.CreateBackupFile(resIndexResInput);
			AddUndoFileFrame(resIndexResInput, resIndexResBackup);
			// For the TxtInput file (XX_YY_ZZ.txt) create a temp file and a backup up file.
			string xxTxtTemp = Generic.CreateTempFile(xxTxtFile);
			AddTempFile(xxTxtTemp);
			string xxTxtBackup = Generic.CreateBackupFile(xxTxtFile);
			AddUndoFileFrame(xxTxtFile, xxTxtBackup);
			// For the XX_YY_ZZ.res input file (icudt26l_XX_YY_ZZ.res) create a backup file.
			string xxResBackup = Generic.CreateBackupFile(xxResFile);
			AddUndoFileFrame(xxResFile, xxResBackup);

			// Remove from res_index.txt file.
			int line;
			eAction ret = FindResource(resIndexTxtInput, "InstalledLocales", locale, "", out line);
			if (ret == eAction.present)	// .. and ICU
			{
				ModifyFile(resIndexTxtInput, resIndexTxtTemp, line, "",	eAction.replace,
					ErrorCodes.ResIndexFile);
				LogFile.AddLine("Removed locale entry from resIndex file");
			}
			// Generate .res files, Process each file by itself and catch errors.
			ArrayList Files = new ArrayList();
			Files.Add(resIndexTxtTemp);
			GenerateResFile(Files, m_collDirectory, m_icuBase + m_IcuData + "coll",
				ErrorCodes.GeneralFile, "RemoveCollation");

			Generic.FileCopyWithLogging(resIndexTxtTemp, resIndexTxtInput, true);

			// Need to clean up by deleting all of the _TEMP files.
			// ALWAYS try to Remove the XXX.txt, XXX.res and XXX.xml files.
			Generic.DeleteFile(xxTxtFile);
			Generic.DeleteFile(xxResFile);
			// remove the backup files from the undo/recover file name list
			RemoveBackupFiles();
			LogFile.AddErrorLine("--- Successfully removed Collation ---");
		}

		private void GetModifiedLocales(IcuDataNode rootNode, string section, string locale,
			ArrayList rgXxTxtFiles, ArrayList rgXxResFiles)
		{
			IcuDataNode customNode = rootNode.Child("Custom");
			if (customNode == null)
				return;
			IcuDataNode sectionNode = customNode.Child(section);
			if (sectionNode == null)
				return;
			IcuDataNode localeNode = sectionNode.Child(locale);
			if (localeNode == null)
				return;
			IList rgAtts = localeNode.Attributes;
			for (int i = 0; i < rgAtts.Count; ++i)
			{
				IcuDataNode.IcuDataAttribute x = rgAtts[i] as IcuDataNode.IcuDataAttribute;
				if (x.StringValue != null)
				{
					string txtFile = m_localeDirectory + x.StringValue + ".txt";
					if (!rgXxTxtFiles.Contains(txtFile))
					{
						rgXxTxtFiles.Add(txtFile);
						string resFile = m_icuBase + m_IcuData + x.StringValue + ".res";
						rgXxResFiles.Add(resFile);
					}
				}
			}
		}

		#endregion

		/// <summary>
		/// Parses the language data file.  This is called by either InstallLDFile or InstallPUACharacters.
		/// </summary>
		/// <param name="ldFilename">The filename of the LD xml file to parse.</param>
		public void ParseLanguageDataFile(string ldFilename)
		{
			// If we don't have the correct ldData parsed, parse the correct data.
			if( m_ldData == null || m_ldFilename != ldFilename )
			{
				m_ldData = new Parser(ldFilename);

				// Make the comment be the name of the file we are using to add with the date
				// Note we can't use local culture info as Korean/Chinese, etc. will introduce utf-8
				// characters that will cause icu tools to fail.
				System.Globalization.CultureInfo ci = System.Globalization.CultureInfo.CreateSpecificCulture("en-US");
				m_comment = "[SIL-Corp] " + ldFilename + " User Added  " + System.DateTime.Now.ToString("F", ci);

				try
				{
					m_ldData.Populate();
				}
				catch
				{
					throw new LDExceptions(ErrorCodes.LDParsingError);
				}
			}
		}

		#region install PUA characters

		/// <summary>
		/// Installs the PUA characters (PUADefinitions) from the given xml file
		/// We do this by:
		/// 1.Maps the XML file to our LanguageDefinition class
		/// 2.Sorts the PUA characters
		/// 3.Opens UnicodeData.txt for reading and writing
		/// 4.Inserts the PUA characters via their codepoints
		/// 5. Update the DerivedBidiClass.txt file so that the bidi values match
		/// 6. Run the "genprops" command to write the actual files
		/// </summary>
		/// <param name="m_ldData">Our XML file containing our PUA Defintions</param>
		public void InstallPUACharacters(string ldFilename)
		{
			// 0. Intro: Prepare files

			// 0.1: File names
			string unicodeDataFilename = Generic.GetIcuDir() + @"data\unidata\UnicodeData.txt";
			string derivedBidiClassFilename = Generic.GetIcuDir() + @"data\unidata\DerivedBidiClass.txt";
			string derivedNormalizationPropsFilename = Generic.GetIcuDir() +
				@"data\unidata\DerivedNormalizationProps.txt";

			// 0.2: Create a one-time backup that will not be over written if the file exists
			Generic.BackupOrig(unicodeDataFilename);
			Generic.BackupOrig(derivedBidiClassFilename);
			Generic.BackupOrig(derivedNormalizationPropsFilename);

			// 0.3: Create a stack of files to restore if we encounter and error
			//			This allows us to work with the original files
			// If we are successful we call this.RemoveBackupFiles() to clean up
			// If we are not we call this.RestoreFiles() to restore the original files
			//		and delete the backups
			string unicodeDataBackup = Generic.CreateBackupFile(unicodeDataFilename);
			string derivedBidiClassBackup = Generic.CreateBackupFile(derivedBidiClassFilename);
			string derivedNormalizationPropsBackup = Generic.CreateBackupFile(derivedNormalizationPropsFilename);
			this.AddUndoFileFrame(unicodeDataFilename,unicodeDataBackup);
			this.AddUndoFileFrame(derivedBidiClassFilename,derivedBidiClassBackup);
			this.AddUndoFileFrame(derivedNormalizationPropsFilename, derivedNormalizationPropsBackup);

			//Initialize and populate the parser if necessary
			// 1. Maps our XML file to the LanguageDefinition class
			ParseLanguageDataFile(ldFilename);

			// There are no characters to install, we are done;
			if(m_ldData.PuaDefinitions == null || m_ldData.PuaDefinitions.Length == 0)
			{
				LogFile.AddLine("There are no PUA characters to install");
				return;
			}

			// (Step 1 has been moved before the "intro")
			// 2. Sort the PUA characters
			System.Array.Sort(m_ldData.PuaDefinitions);

			// 3. Open the file for reading and writing
			// 4. Insert the PUA via their codepoints
			ArrayList addToBidi;
			ArrayList removeFromBidi;
			ArrayList addToNorm;
			ArrayList removeFromNorm;
			InsertCharacters(m_ldData.PuaDefinitions, out addToBidi, out removeFromBidi,
				out addToNorm, out removeFromNorm);

			// 5. Update the DerivedBidiClass.txt file so that the bidi values match

			UCDComparer comparer  = new UCDComparer();
			addToBidi.Sort(comparer);
			removeFromBidi.Sort(comparer);
			addToNorm.Sort(comparer);
			removeFromNorm.Sort(comparer);

			UpdateUCDFile(addToBidi,removeFromBidi);
			UpdateUCDFile(addToNorm,removeFromNorm);

			// 6. Run the "genprops","gennames","gennorm" commands to write the actual files
			RunICUPUATools();

		}

		/// <summary>
		/// This produces some basic debugging files:
		/// 1. The DereivedBidiClass.txt after deleting and before adding
		/// 2. A list of all the values that need to be changed (deleted and added) in said file
		///
		/// THIS SORTS THE PUA DEFINITIONS, SO DON'T CALL IT AFTER SORTING.
		/// </summary>
		private void DebugPUAListFile(PUACharacter[] puaDefinitions)
		{
			StreamWriter puaDefinitionsWriter =
				new StreamWriter(Generic.GetIcuDir() + @"data\unidata\InsertPUA.txt", false,
				System.Text.Encoding.ASCII);
			puaDefinitionsWriter.WriteLine("PUA lines to insert by code");
			//Sort the PUA characters
			System.Array.Sort(puaDefinitions);
			foreach(PUACharacter puaDef in puaDefinitions)
				puaDefinitionsWriter.WriteLine(puaDef);
			puaDefinitionsWriter.Close();
		}

		/// <summary>
		/// This runs genprops and gennames in order to use UnicodeData.txt,
		/// DerivedBidiClass.txt, as well as other *.txt files in unidata to
		/// create <i>icuprefix/</i>uprops.icu and other binary data files.
		/// </summary>
		public void RunICUPUATools()
		{
			// run the following command:
			//
			//    icu\tools\genprops -u 4 -s icu\data\unidata -d icu -i icu
			//
			// Note: this compiles UnicodeData.txt to produce icudt26l/unames.icu.
			// Note: this compiles UnicodeData.txt, DerivedBidiClass.txt,
			//    plus numerous other data files to produce icudt26l/uprops.icu.

			// Get the icu directory information
			string icuDataDir = Generic.GetIcuDataDir();
			string icuDir = Generic.GetIcuDir();
			string icuPrefix  = Generic.GetIcuData();
			string icuBaseDir = icuDir.Substring(0, icuDir.LastIndexOf("\\"));
			string icuBaseDataDir = icuDataDir.Substring(0, icuDataDir.LastIndexOf("\\"));
			string icuUnidataDir = icuDir + "data\\unidata";

			// Make a one-time original backup of the files we are about to generate.
			string upropsFileName = icuDataDir + "uprops.icu";
			string unamesFileName = icuDataDir + "unames.icu";
			string unormFileName = icuDataDir + "unorm.icu";
			string ubidiFileName = icuDataDir + "ubidi.icu";
			string ucaseFileName = icuDataDir + "ucase.icu";
			this.AddUndoFileFrame(upropsFileName,Generic.CreateBackupFile(upropsFileName));
			this.AddUndoFileFrame(unamesFileName,Generic.CreateBackupFile(unamesFileName));
			this.AddUndoFileFrame(unormFileName,Generic.CreateBackupFile(unormFileName));
			this.AddUndoFileFrame(ubidiFileName,Generic.CreateBackupFile(ubidiFileName));
			this.AddUndoFileFrame(ucaseFileName,Generic.CreateBackupFile(ucaseFileName));
			Generic.BackupOrig(upropsFileName);
			Generic.BackupOrig(unamesFileName);
			Generic.BackupOrig(unormFileName);
			Generic.BackupOrig(ubidiFileName);
			Generic.BackupOrig(ucaseFileName);

			// Prepare the path arguments
			string args = " -u 5.1 -s \"" + icuUnidataDir + "\" -d " + "\"" + icuBaseDataDir + "\"" +
				" -i " + "\"" + icuBaseDataDir + "\"";
			string fileName = icuDir + "tools\\genprops.exe ";
			string workingDir = icuBaseDir;

			// Clean up the ICU and set the icuDatadir correctly
			Icu.Cleanup();
			Icu.SetDataDirectory(icuDir);

			Process genpropsProcess = new Process();
			genpropsProcess.StartInfo.FileName = fileName;
			genpropsProcess.StartInfo.WorkingDirectory = workingDir; //icuDir + "..\\";
			genpropsProcess.StartInfo.Arguments = args;
			genpropsProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

			// Allows us to re-direct the std. output for logging.
			genpropsProcess.StartInfo.UseShellExecute = false;
			genpropsProcess.StartInfo.RedirectStandardOutput = true;
			genpropsProcess.StartInfo.RedirectStandardError = true;

			genpropsProcess.Start();
			genpropsProcess.WaitForExit();
			int ret = genpropsProcess.ExitCode;

			// If gen props doesn't run correctly, log what it displays to the standar output
			// and throw and exception
			if( ret != 0 )
			{
				if (LogFile.IsLogging())
				{
					LogFile.AddErrorLine("Error running genprops:");
					LogFile.AddErrorLine(genpropsProcess.StandardOutput.ReadToEnd());
					LogFile.AddErrorLine(genpropsProcess.StandardError.ReadToEnd());
				}
				throw new LDExceptions(ErrorCodes.Genprops);
			}

			// In order to get the new names registered properly, you also need to run the
			// following command:
			//
			//    icu\tools\gennames -1 -u 4 -d icu\icudt34l icu\data\unidata\UnicodeData.txt
			//
			// Note: this compiles UnicodeData.txt to produce icu\icudt34l\unames.icu.

			Process gennamesProcess = new Process();
			gennamesProcess.StartInfo.FileName = icuDir + "tools\\gennames.exe";
			gennamesProcess.StartInfo.WorkingDirectory = icuDir + "..\\";
			// Note gennames can't take spaces in any of the path names, even if surrounded by
			// quotes.
			gennamesProcess.StartInfo.Arguments = "-1 -u 5.1 -d \"" + icuBaseDataDir +
				"\" \"" + icuUnidataDir + "\\UnicodeData.txt\"";
			gennamesProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			// The following StartInfo properties allows us to extract the standard output
			gennamesProcess.StartInfo.UseShellExecute = false;
			gennamesProcess.StartInfo.RedirectStandardOutput = true;
			gennamesProcess.StartInfo.RedirectStandardError = true;
			gennamesProcess.Start();
			gennamesProcess.WaitForExit();
			if( gennamesProcess.ExitCode != 0 )
			{
				if (LogFile.IsLogging())
				{
					LogFile.AddErrorLine("Error running gennames:");
					LogFile.AddErrorLine(gennamesProcess.StandardOutput.ReadToEnd());
					LogFile.AddErrorLine(gennamesProcess.StandardError.ReadToEnd());
				}
				throw new LDExceptions(ErrorCodes.Gennames);
			}

			// In order to get the normalization data registered properly, you also need to run
			// the following command:
			// This is necessary for fields 3 and 5 (Canonical combining class and the
			// decompostion value
			// Read source information and create a binary file with normalization data.
			//
			//	icu\tools\gennorm -u 4 -s icu\data\unidata -d icu\icudt34l -i icu\icudt34l
			//
			// Input: UnicodeData.txt, DerivedNormalizationProps.txt,
			// Output: icu\icudt34l\unorm.icu

			Process gennormProcess = new Process();
			gennormProcess.StartInfo.FileName = icuDir + "tools\\gennorm.exe";
			gennormProcess.StartInfo.WorkingDirectory = icuDir + "..\\";
			gennormProcess.StartInfo.Arguments = args;
			gennormProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			// The following StartInfo properties allows us to extract the standard output
			gennormProcess.StartInfo.UseShellExecute = false;
			gennormProcess.StartInfo.RedirectStandardOutput = true;
			gennormProcess.StartInfo.RedirectStandardError = true;
			gennormProcess.Start();
			gennormProcess.WaitForExit();
			if( gennormProcess.ExitCode != 0 )
			{
				if (LogFile.IsLogging())
				{
					LogFile.AddErrorLine("Error running gennorm:");
					LogFile.AddErrorLine(gennormProcess.StandardOutput.ReadToEnd());
					LogFile.AddErrorLine(gennormProcess.StandardError.ReadToEnd());
				}
				throw new LDExceptions(ErrorCodes.Gennorm);
			}

			// We also need to run genbidi.
			//
			//	icu\tools\genbidi -u 4 -s icu\data\unidata -d icu\icudt34l -i icu\icudt34l
			//
			Process genbidiProcess = new Process();
			genbidiProcess.StartInfo.FileName = icuDir + "tools\\genbidi.exe";
			genbidiProcess.StartInfo.WorkingDirectory = icuDir + "..\\";
			genbidiProcess.StartInfo.Arguments = args;
			genbidiProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			// The following StartInfo properties allows us to extract the standard output
			genbidiProcess.StartInfo.UseShellExecute = false;
			genbidiProcess.StartInfo.RedirectStandardOutput = true;
			genbidiProcess.StartInfo.RedirectStandardError = true;
			genbidiProcess.Start();
			genbidiProcess.WaitForExit();
			if( genbidiProcess.ExitCode != 0 )
			{
				if (LogFile.IsLogging())
				{
					LogFile.AddErrorLine("Error running genbidi:");
					LogFile.AddErrorLine(genbidiProcess.StandardOutput.ReadToEnd());
					LogFile.AddErrorLine(genbidiProcess.StandardError.ReadToEnd());
				}
				throw new LDExceptions(ErrorCodes.Genbidi);
			}

			// We need to run gencase.
			//
			//	icu\tools\gencase -u 4 -s icu\data\unidata -d icu\icudt34l -i icu\icudt34l
			//
			Process gencaseProcess = new Process();
			gencaseProcess.StartInfo.FileName = icuDir + "tools\\gencase.exe";
			gencaseProcess.StartInfo.WorkingDirectory = icuDir + "..\\";
			gencaseProcess.StartInfo.Arguments = args;
			gencaseProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			// The following StartInfo properties allows us to extract the standard output
			gencaseProcess.StartInfo.UseShellExecute = false;
			gencaseProcess.StartInfo.RedirectStandardOutput = true;
			gencaseProcess.StartInfo.RedirectStandardError = true;
			gencaseProcess.Start();
			gencaseProcess.WaitForExit();
			if( gencaseProcess.ExitCode != 0 )
			{
				if (LogFile.IsLogging())
				{
					LogFile.AddErrorLine("Error running gencase:");
					LogFile.AddErrorLine(gencaseProcess.StandardOutput.ReadToEnd());
					LogFile.AddErrorLine(gencaseProcess.StandardError.ReadToEnd());
				}
				throw new LDExceptions(ErrorCodes.Gencase);
			}
		}

		/// <summary>
		/// Inserts the given PUADefinitions (any Unicode character) into the UnicodeData.txt file.
		///
		/// This accounts for all the cases of inserting into the "first/last" blocks.  That
		/// is, it will split the blocks into two or move the first and last tags to allow a
		/// codepoint to be inserted correctly.
		///
		/// Also, this accounts for Hexadecimal strings that are within the unicode range, not
		/// just four digit unicode files.
		///
		/// <list type="number">
		/// <listheader>Assumptions made about the format</listheader>
		/// <item>The codepoints are in order</item>
		/// <item>There first last block will always have no space between the word first and
		/// the following ">"</item>
		/// <item>No other data entries contain the word first followed by a ">"</item>
		/// <item>There will always be a "last" on the line directly after a "first".</item>
		/// </list>
		///
		/// </summary>
		/// <remarks>
		/// Pseudocode for inserting lines:
		///	if the unicodePoint	is a first tag
		///		Get	first and last uncodePoint range
		///		Stick into array all the xmlPoints that fit within the uncodePoint range
		///			Look at the next xmlPoint
		///			if there are any
		///				call WriteCodepointBlock subroutine
		///	else if the unicodePoint is greater than the last point but less than or equal to "the xmlPoint"
		///		insert the missing line or replace	the	line
		///		look at	the	next xmlPoint
		///	else
		///		do nothing except write	the	line
		///</remarks>
		/// <param name="puaDefinitions">An array of PUADefinitions to insert into UnicodeData.txt.</param>
		///
		///	<param name="addToBidi">An ArrayList of BidiCharacters to remove from the DerivedBidiClass.txt file</param>
		///	<param name="removeFromBidi">An ArrayList of BidiCharacters to add to the DerivedBidiClass.txt file</param>
		private void InsertCharacters(PUACharacter[] puaDefinitions, out ArrayList addToBidi,
			out ArrayList removeFromBidi, out ArrayList addToNorm, out ArrayList removeFromNorm)
		{
			addToBidi = new ArrayList();
			removeFromBidi = new ArrayList();
			addToNorm = new ArrayList();
			removeFromNorm = new ArrayList();

			// Create a temporary file to write to and backup the original
			string unicodeFileName = Generic.GetIcuDir() + "data\\unidata\\UnicodeData.txt";
			string tempUnicodeFileName = Generic.CreateTempFile(unicodeFileName);

			AddTempFile(tempUnicodeFileName);

			// Open the file for reading and writing
			LogFile.AddVerboseLine("StreamReader on <" + unicodeFileName + ">");
			StreamReader reader = new StreamReader(unicodeFileName, System.Text.Encoding.ASCII);
			int chT = reader.Peek();	// force autodetection of encoding.
			StreamWriter writer = new StreamWriter(tempUnicodeFileName, false,
				System.Text.Encoding.ASCII);
			try
			{
				// Insert the PUA via their codepoints

				string line;
				string strFileCode; // current code in file
				int fileCode;
				int lastCode = 0;
				int newCode;
				// Start looking at the first codepoint
				int codeIndex = 0;

				// Used to find the type for casting ArrayLists to PUACharacter[]
				PUACharacter puaCharForType = new PUACharacter("");
				Type puaCharType = puaCharForType.GetType();

				//While there is a line to be read in the file
				while((line = reader.ReadLine()) != null)
				{
					// skip entirely blank lines
					if (line.Length <= 0)
						continue;

					//Grab codepoint
					strFileCode = line.Substring(0, line.IndexOf(';')).Trim();
					fileCode = Convert.ToInt32(strFileCode, 16);
					newCode = puaDefinitions[codeIndex].Character;

					//If it's a first tag
					if(line.ToLowerInvariant().IndexOf("first>") != -1)
					{
						// do the smarts necessary to insert the codepoints.

						// read in the "Last" line
						string line2 = reader.ReadLine();
						//Grabs the codepoint on the "last" tag
						string strEndCode = line2.Substring(0, line.IndexOf(';'));
						int endCode = Convert.ToInt32(strEndCode, 16);

						//A dynamic array that contains our range of codepoints and the
						//properties to go with it
						System.Collections.ArrayList codepointsWithinRange = new System.Collections.ArrayList();

						// While newCode satisfies: fileCode <= newCode <= endCode
						while (newCode >= fileCode && newCode <= endCode)
						{
							codepointsWithinRange.Add(puaDefinitions[codeIndex]);
							//If this is the last one stop looking for more
							if(++codeIndex >= puaDefinitions.Length)
								break;
							newCode = puaDefinitions[codeIndex].Character;
						}
						//If we still have codepoints to insert
						if(codepointsWithinRange.Count > 0)
						{
							//Grab the block name
							string blockName = line.Substring(line.IndexOf('<') +1,
								line.IndexOf(',')-line.IndexOf('<') -1);

							//Grab the data that follows the block tag
							//Ex: ;Cs;0;L;;;;;N;;;;;
							string data = line.Substring(line.IndexOf('>') + 1);
							//Do lots of smart stuff to insert the PUA characters into the block
							WriteCodepointBlock(writer,blockName,strFileCode,strEndCode,
								(PUACharacter[])codepointsWithinRange.ToArray(puaCharType),
								data,addToBidi,removeFromBidi,addToNorm,removeFromNorm);
						}
						else
						{
							writer.WriteLine(line);
							writer.WriteLine(line2);
						}
						//If we have no more codepoints to insert then just finish writing the
						//file
						if(codeIndex >= puaDefinitions.Length)
						{
							while((line = reader.ReadLine()) != null)
								writer.WriteLine(line);
							break;
						}
					}

					// If the new codepoint is greater than the last one processed in the file, but
					// less than or equal to the current codepoint in the file.
					else if(newCode > lastCode && newCode <= fileCode)
					{
						while (newCode <= fileCode)
						{
							LogCodepoint(puaDefinitions[codeIndex].CodePoint);

							// Add the PuaCharacter to the lists if it needs to be added.
							AddToLists(line, puaDefinitions[codeIndex], addToBidi,
								removeFromBidi, addToNorm, removeFromNorm);

							// Replace the line with the new PuaDefinition
							writer.WriteLine("{0} #{1}", puaDefinitions[codeIndex], m_comment);
							lastCode = newCode;

							// Look for the next PUA codepoint that we wish to insert, we are done
							// with this one If we are all done, push through the rest of the file.
							if (++codeIndex >= puaDefinitions.Length)
							{
								while ((line = reader.ReadLine()) != null)
									writer.WriteLine(line);
								break;
							}
							newCode = puaDefinitions[codeIndex].Character;
						}
						if (codeIndex >= puaDefinitions.Length)
							break;
						// Write out the original top of the section if it hasn't been replaced.
						if(fileCode != lastCode)
						{
							writer.WriteLine(line);
						}
					}
					//if it's not a first tag and the codepoints don't match
					else
					{
						writer.WriteLine(line);
					}
					lastCode = fileCode;
				}
			}
			finally
			{
				writer.Flush();
				writer.Close();
				reader.Close();
			}

			// Copy the temporary file to be the original
			Generic.FileCopyWithLogging(tempUnicodeFileName,unicodeFileName,true);
		}

		/// <summary>
		/// Checks whether the PUACharacter needs to be added to the lists, and adds if necessary.
		/// </summary>
		/// <param name="line">The line of the UnicodeData.txt that will be replaced.
		///		If a property matches, the value will not be added to the lists.</param>
		/// <param name="puaDefinition">The puaCharacter that is being inserted.</param>
		private void AddToLists(string line, PUACharacter puaDefinition,
			ArrayList addToBidi,  ArrayList removeFromBidi,  ArrayList addToNorm,
			ArrayList removeFromNorm)
		{
#if DEBUGGING_SOMETHING
			int temp = line.IndexOf("F16F");	// junk for a debugging breakpoint...
			temp++;
#endif

			// If the bidi type doesn't match add it to the lists to replace
			string bidi = GetField(line,UCDComparer.bidi+1);
			if(!puaDefinition.Bidi.Equals(bidi))
			{
				removeFromBidi.Add(new BidiCharacter(line));
				addToBidi.Add(new BidiCharacter(puaDefinition));
			}
			// If the new character doesn't match the decomposition, add it to the lists
			string decomposition = GetField(line,5);
			string puaRawDecomp = puaDefinition.Data[5-1];
			if(decomposition!=puaRawDecomp)
			{
				// Perform a quick attempt to remove basic decompositions
				// TODO: Extend this to actually remove more complicated entries?
				// Currently this will remove anything that we have added.
				if(decomposition.Trim()!="")
				{
					// If there is a '>' character in the decomposition field
					// then it is a compatability decomposition
					if(decomposition.IndexOf(">")!=-1)
						removeFromNorm.Add(new NormalizationCharacter(line,"NFKD_QC; N"));
					removeFromNorm.Add(new NormalizationCharacter(line,"NFD_QC; N"));
				}
				// Add the normalization to the lists, if necessary.
				if(puaDefinition.Decomposition!="")
				{
					// Add a canonical decomposition if necessary
					if(puaDefinition.DecompositionType=="")
						addToNorm.Add(new NormalizationCharacter(puaDefinition,"NFD_QC; N"));
					// Add a compatability decomposition always
					// (Apparently canonical decompositions are compatability decompositions,
					//		but not vise-versa
					addToNorm.Add(new NormalizationCharacter(puaDefinition,"NFKD_QC; N"));
				}
			}
		}


		/// <summary>
		/// Retrieves the given field from the given UnicodeData.txt line
		/// </summary>
		/// <param name="line">A line in the format of the UnicodeData.txt file</param>
		/// <param name="field">The field index</param>
		/// <returns>The value of the field</returns>
		string GetField(string line, int field)
		{
			// Find the bidi field
			return line.Split(new char[] {';'})[field];
		}

/*
Character Decomposition Mapping

The tags supplied with certain decomposition mappings generally indicate formatting
information. Where no such tag is given, the mapping is canonical. Conversely, the
presence of a formatting tag also indicates that the mapping is a compatibility mapping
and not a canonical mapping. In the absence of other formatting information in a
compatibility mapping, the tag is used to distinguish it from canonical mappings.

In some instances a canonical mapping or a compatibility mapping may consist of a single
character. For a canonical mapping, this indicates that the character is a canonical
equivalent of another single character. For a compatibility mapping, this indicates that
the character is a compatibility equivalent of another single character. The compatibility
formatting tags used are:

	Tag 		Description
	----------	-----------------------------------------
	<font>		A font variant (e.g. a blackletter form).
	<noBreak>	A no-break version of a space or hyphen.
	<initial>	An initial presentation form (Arabic).
	<medial>	A medial presentation form (Arabic).
	<final>		A final presentation form (Arabic).
	<isolated>	An isolated presentation form (Arabic).
	<circle>	An encircled form.
	<super>		A superscript form.
	<sub>		A subscript form.
	<vertical>	A vertical layout presentation form.
	<wide>		A wide (or zenkaku) compatibility character.
	<narrow>	A narrow (or hankaku) compatibility character.
	<small>		A small variant form (CNS compatibility).
	<square>	A CJK squared font variant.
	<fraction>	A vulgar fraction form.
	<compat>	Otherwise unspecified compatibility character.

Reminder: There is a difference between decomposition and decomposition mapping. The
decomposition mappings are defined in the UnicodeData, while the decomposition (also
termed "full decomposition") is defined in Chapter 3 to use those mappings recursively.

	* The canonical decomposition is formed by recursively applying the canonical
	  mappings, then applying the canonical reordering algorithm.

	* The compatibility decomposition is formed by recursively applying the canonical and
	  compatibility mappings, then applying the canonical reordering algorithm.


Decompositions and Normalization

Decomposition is specified in Chapter 3. UAX #15: Unicode Normalization Forms [Norm]
specifies the interaction between decomposition and normalization. That report specifies
how the decompositions defined in UnicodeData.txt are used to derive normalized forms of
Unicode text.

Note that as of the 2.1.9 update of the Unicode Character Database, the decompositions in
the UnicodeData.txt file can be used to recursively derive the full decomposition in
canonical order, without the need to separately apply canonical reordering. However,
canonical reordering of combining character sequences must still be applied in
decomposition when normalizing source text which contains any combining marks.

The QuickCheck property values are as follows:

Value 	Property	 	Description
-----	--------		-----------------------------------
No		NF*_QC			Characters that cannot ever occur in the respective normalization
						form.  See Decompositions and Normalization.
Maybe 	NFC_QC,NFKC_QC	Characters that may occur in in the respective normalization,
						depending on the context. See Decompositions and Normalization.
Yes		n/a				All other characters. This is the default value, and is not
						explicitly listed in the file.



*/
		/// <summary>
		/// Write a codepoint block, inserting the necessary codepoints properly.
		/// </summary>
		/// <param name="writer">UnicodeData.txt file to write lines to/</param>
		/// <param name="blockName">The name of the block (e.g. "Private Use")</param>
		/// <param name="beginning">First codepoint in the block</param>
		/// <param name="end">Last codepoint in the free block</param>
		/// <param name="puaCharacters">An array of codepoints within the block, including the ends.
		///		DO NOT pass in points external to the free block.</param>
		///	<param name="data">A string that contains all of our properties and such for the character range</param>
		///	<param name="addToBidi">An ArrayList of PUACharacters to remove from the DerivedBidiClass.txt file</param>
		///	<param name="removeFromBidi">An ArrayList of PUACharacters to add to the DerivedBidiClass.txt file</param>
		private void WriteCodepointBlock(StreamWriter writer, string blockName,  string beginning, string end,
			PUACharacter[] puaCharacters, string data, ArrayList addToBidi, ArrayList removeFromBidi,
			ArrayList addToNorm, ArrayList removeFromNorm)
		{
			//Write each entry
			foreach(PUACharacter puaCharacter in puaCharacters)
			{
				LogCodepoint(puaCharacter.CodePoint);

				// Construct an equivelant UnicodeData.txt line
				string line = puaCharacter.CodePoint + ";" + blockName
					+ data.Substring(data.IndexOf(';'));
				AddToLists(line,puaCharacter,addToBidi,removeFromBidi,addToNorm,removeFromNorm);

				//If the current xmlCodepoint is the same as the beginning codepoint
				if(puaCharacter.CompareTo(beginning)==0)
				{
					//Shift the beginning down one
					beginning=PUACharacter.AddHex(beginning,1);
					WriteUnicodeDataLine(puaCharacter,writer);
				}
					//If the current xmlCodepoint is between the beginning and end
				else if(puaCharacter.CompareTo(end)!=0)
				{
					//We're writing a range block below the current xmlCodepoint
					WriteRange(writer,beginning,PUACharacter.AddHex(puaCharacter.CodePoint,-1),blockName,data);
					//Writes the current xmlCodepoint line
					WriteUnicodeDataLine(puaCharacter,writer);
					//Increment the beginning by one
					beginning=PUACharacter.AddHex(puaCharacter.CodePoint,1);
				}
					//If the current xmlCodepoint is the same as the end codepoint
				else
				{
					//Moves the end down a codepoint address
					end=PUACharacter.AddHex(end,-1);
					//Write our range of data
					WriteRange(writer,beginning,end,blockName,data);
					//Writes the current line
					WriteUnicodeDataLine(puaCharacter,writer);
					return;
				}
			}
			//Write our range of data
			WriteRange(writer,beginning,end,blockName,data);
		}
		/// <summary>
		/// Writes a UnicodeData.txt style line including comments.
		/// </summary>
		/// <param name="puaChar">The character to write</param>
		/// <param name="tw">The writer to write it to.</param>
		private void WriteUnicodeDataLine(PUACharacter puaChar, TextWriter tw)
		{
			tw.WriteLine("{0} #{1}",puaChar,m_comment);
		}

		/// <summary>
		/// Updates a UCD style file as necessary.
		/// so that the entries match the ones we just inserted into UnicodeData.txt.
		/// </summary>
		/// <remarks>
		/// A UCD file is a "Unicode Character Database" text file.
		/// The specific documentation can be found at:
		/// http://www.unicode.org/Public/UNIDATA/UCD.html#UCD_File_Format
		/// </remarks>
		private void UpdateUCDFile(ArrayList addToUCD, ArrayList removeFromUCD)
		{
			string ucdFilenameWithoutPath="NO FILE";

			// Get the file name we want to modify.
			if(addToUCD.Count==0)
			{
				if(removeFromUCD.Count==0)
					// If we aren't supposed to change anything, we are done.
					return;
				else
					ucdFilenameWithoutPath = ((UCDCharacter)removeFromUCD[0]).FileName;
			}
			else
				ucdFilenameWithoutPath = ((UCDCharacter)addToUCD[0]).FileName;

			// Create a temporary file to write to and backup the original
			string ucdFilename = Generic.GetIcuDir() + @"data\unidata\" + ucdFilenameWithoutPath;
			string ucdFileTemp = Generic.CreateTempFile(ucdFilename);
			// Add the temp file to a list of files to be deleted when we are done.
			AddTempFile(ucdFileTemp);

			//All the streams necessary to read and write for the Bidi text file
			LogFile.AddVerboseLine("StreamReader on <" + ucdFilename + ">");
			StreamReader reader = new StreamReader(ucdFilename, System.Text.Encoding.ASCII);
			//These 2 streams are used to allow us to pass through the file twice w/out writing to the hard disk
			StringWriter stringWriter = new StringWriter();
			StringReader stringReader;
			//Writes out the final file (to a temp file that will be copied upon success)
			StreamWriter writer = new StreamWriter(ucdFileTemp, false, System.Text.Encoding.ASCII);

			//Does our first pass through of the file, removing necessary lines
			ModifyUCDFile(reader, stringWriter, removeFromUCD, false);
			stringReader = new StringReader(stringWriter.ToString());

			// Does the second pass through the file, adding necessary lines
			ModifyUCDFile(stringReader,writer,addToUCD,true);

			// write file
			writer.Flush();
			// close file
			writer.Close();
			reader.Close();
			stringWriter.Close();

			// If we get this far without an exception, copy the file over the original
			Generic.FileCopyWithLogging(ucdFileTemp,ucdFilename,true);
		}
		/// <summary>
		/// Makes three debug files for the DerivedBidiClass.txt:
		///
		/// A: Saves the "in-between" state (after deleting, before adding)
		/// B: Saves a list of what we are adding and deleting.
		/// C: Saves an actual file that doesn't add or delete anything.
		///		This will update the numbers, but won't do anything else
		///
		/// </summary>
		/// <param name="stringWriter">Used to get the file after deleting.</param>
		private void MakeDebugBidifiles(StringWriter stringWriter, ArrayList addToBidi, ArrayList removeFromBidi)
		{
			// TODO: Do we need to keep this debug test code?
			// This region makes some debug files

			// A: Saves the "in-between" state (after deleting, before adding)
			StreamWriter middle = new StreamWriter(Generic.GetIcuDir() +
				@"data\unidata\DerivedBidiClassMID.txt",false, System.Text.Encoding.ASCII);
			middle.WriteLine(stringWriter.ToString());
			middle.Close();

			// B: Saves a list of what we are adding and deleting.
			StreamWriter lists = new StreamWriter(Generic.GetIcuDir() + @"data\unidata\LISTS.txt",false,
				System.Text.Encoding.ASCII);
			lists.WriteLine("Add:");
			foreach( Object o in addToBidi )
			{
				lists.WriteLine(((PUACharacter)o).ToBidiString());
			}
			lists.WriteLine("Remove:");
			foreach( Object o in removeFromBidi )
			{
				lists.WriteLine(((PUACharacter)o).ToBidiString());
			}
			lists.Close();

			// C: Saves an actual file that doesn't add or delete anything.
			//This will update the numbers, but won't do anything else
			LogFile.AddVerboseLine("StreamReader on <" + "DerivedBidiClass.txt" + ">");
			StreamReader countReader = new StreamReader(Generic.GetIcuDir() +
				@"data\unidata\DerivedBidiClass.txt", true);
			StreamWriter countWriter = new StreamWriter(Generic.GetIcuDir() +
				@"data\unidata\DerivedBidiClassCOUNT.txt",false, System.Text.Encoding.ASCII);
			ModifyUCDFile(countReader,countWriter,new ArrayList(),true);
			countReader.Close();
			countWriter.Close();
			/// End of making debug files
		}

		/// <summary>
		/// This function will add or remove the given PUA characters from the given
		/// DerivedBidiClass.txt file by either inserting or not inserting them as necessary
		/// as it reads through the file in a single pass from <code>tr</code> to <code>tw</code>.
		/// </summary>
		/// <remarks>
		/// <list type="number">
		/// <listheader><description>Assumptions</description></listheader>
		/// <item><description>The like Bidi values are grouped together</description></item>
		/// </list>
		///
		/// <list type="number">Non Assumptions:
		/// <item><description>That comments add any information to the file.  We don't use comments for parsing.</description></item>
		/// <item><description>That "blank" lines appear only between different bidi value sections.</description></item>
		/// <item><description>That the comments should be in the format:
		///		# field2 [length of range]  Name_of_First_Character..Name_of_Last_Charachter
		///		(If it is not, we'll just ignore.</description></item>
		/// </list>
		/// </remarks>
		/// <param name="tr">A reader with information DerivedBidiData.txt</param>
		/// <param name="tw">A writer with information to write to DerivedBidiData.txt</param>
		/// <param name="puaCharacters">A list of PUACharacters to either add or remove from the file</param>
		/// <param name="add">Whether to add or remove the given characters</param>
		private void ModifyUCDFile(System.IO.TextReader tr, System.IO.TextWriter tw,
			ArrayList ucdCharacters, bool add)
		{
			if(ucdCharacters.Count==0)
			{
				// There is no point in processing this file if we aren't going to do anything to it.
				tw.Write(tr.ReadToEnd());
				// Allows us to know that there will be at least on ucdCharacter that we can access to get some properties
				return;
			}

			//contains our current line
			// not null so that we get into the while loop
			string line = "unused value";
			//Bidi class value from the previous line
			string lastProperty="blank";
			//Bidi class value from the current line
			string currentProperty;

			// the index of the PUACharacter the we are currently trying to insert
			// Note, the initial value will never be used, because there will be no
			//		bidi class value called "blank" in the file, thus it will be initialized before it is every used
			//		but VS requires an initialization value "just in case"
			int codeIndex=-1;
			// If we have read in the line already we want to use this line for the loop, set this to be true.
			bool dontRead = false;

			//Count the number of characters in each range
			int rangeCount = 0;

			//While there is a line to be read in the file

			while(  (dontRead && line!=null) || (line=tr.ReadLine()) != null )
			{
				dontRead = false;
				if( HasBidiData(line) )
				{
					// We found another valid codepoint, increment the count
					IncrementCount(ref rangeCount,line);

					currentProperty=GetProperty(line);

					//If this is a new section of bidi class values
					if(!((UCDCharacter)ucdCharacters[0]).SameRegion(currentProperty,lastProperty))
					{
						lastProperty = currentProperty;
						// Find one of the ucdCharacters in this range in the list of ucdCharacters to add.
						codeIndex = ucdCharacters.BinarySearch(currentProperty,new UCDComparer());
						// if we don't have any characters to put in this section
						if(codeIndex < 0)
						{
							tw.WriteLine(line);
							line = ReadToEndOfSection(tr,tw,lastProperty,rangeCount,(UCDCharacter)ucdCharacters[0]);
							rangeCount = 0;
							dontRead = true;
							continue;
						}

						// Back up to the beginning of the section of ucdCharacters that have the same bidiclass
						while(--codeIndex>=0 &&
							((UCDCharacter)ucdCharacters[codeIndex]).SameRegion(currentProperty));
						codeIndex++;
					}

					if ( codeIndex < 0 || codeIndex>=ucdCharacters.Count )
						throw new System.Exception("There was a conceptual error while parsing the UCD file." +
							"This should never happen.");

					#region insert_the_PUACharacter
					//Grab codepoint
					string code = line.Substring(0,line.IndexOf(';')).Trim();

					//If it's a range of codepoints
					if(code.IndexOf('.')!=-1)
					{
						#region if_range
						//Grabs the end codepoint
						string endCode = code.Substring(code.IndexOf("..")+2).Trim();
						code = code.Substring(0,code.IndexOf("..")).Trim();

						//A dynamic array that contains our range of codepoints and the properties to go with it
						System.Collections.ArrayList codepointsWithinRange = new System.Collections.ArrayList();

						// If the PUACharacter we want to insert is before the range
						while(
							//If this is the last one stop looking for more
							StillInRange(codeIndex,ucdCharacters,currentProperty) &&
							// For every character before the given value
							((PUACharacter)ucdCharacters[codeIndex]).CompareCodePoint(code) < 0
							)
						{
							//Insert characters before the code
							AddUCDLine(tw,(UCDCharacter)ucdCharacters[codeIndex],add);
							codeIndex++;
						}
						while(
							//If this is the last one stop looking for more
							StillInRange(codeIndex,ucdCharacters,currentProperty) &&
							// While our xmlCodepoint satisfies: code <= xmlCodepoint <= endCode
							((PUACharacter)ucdCharacters[codeIndex]).CompareCodePoint(endCode) < 1
							)
						{
							//Adds the puaCharacter to the list of codepoints that are in range
							codepointsWithinRange.Add(ucdCharacters[codeIndex]);
							codeIndex++;
						}
						//If we found any codepoints in the range to insert
						if(codepointsWithinRange.Count>0)
						{
							#region parse_comments
							//Do lots of smart stuff to insert the PUA characters into the block
							string generalCategory="";
							//Contains the beginning and ending range names
							string firstName="";
							string lastName="";

							//If a comment exists on the line in the proper format
							// e.g.   ---  # --- [ --- ] --- ... ---
							if(line.IndexOf('#')!=-1 && line.IndexOf('[')!=-1
								&& (line.IndexOf('#') <= line.IndexOf('[')) )
							{
								//Grabs the general category
								generalCategory = line.Substring(line.IndexOf('#')+1,line.IndexOf('[')-line.IndexOf('#')-1).Trim();
							}
							//find the index of the second ".." in the line
							int indexDotDot = line.Substring(line.IndexOf(']')).IndexOf("..");
							if( indexDotDot != -1 )
								indexDotDot += line.IndexOf(']');

							int cat = line.IndexOf(']') ;

							if(line.IndexOf('#')!=-1 && line.IndexOf('[')!=-1 && line.IndexOf(']')!=-1 && indexDotDot!=-1
								&& ( line.IndexOf('#') < line.IndexOf('[') )
								&& ( line.IndexOf('[') < line.IndexOf(']') )
								&& ( line.IndexOf(']') < indexDotDot )
								)
							{
								//Grab the name of the first character in the range
								firstName = line.Substring(line.IndexOf(']')+1,indexDotDot-line.IndexOf(']')-1).Trim();
								//Grab the name of the last character in the range
								lastName = line.Substring(indexDotDot+2).Trim();
							}
							#endregion
							WriteBidiCodepointBlock(tw,code,endCode,codepointsWithinRange,
								generalCategory,firstName,lastName,add);
						}
						else
						{
							tw.WriteLine(line);
						}
						#endregion
					}
						//if the codepoint in the file is equal to the codepoint that we want to insert
					else
					{
						if(PUACharacter.CompareHex(code,
							((PUACharacter)ucdCharacters[codeIndex]).CodePoint) > 0)
						{
							// Insert the new PuaDefinition before the line (as well as any others that might be)
							while(
								//If this is the last one stop looking for more
								StillInRange(codeIndex,ucdCharacters,currentProperty) &&
								// For every character before the given value
								((PUACharacter)ucdCharacters[codeIndex]).CompareCodePoint(code) < 0
								)
							{
								//Insert characters before the code
								AddUCDLine(tw,(UCDCharacter)ucdCharacters[codeIndex],add);
								codeIndex++;
							}
						}
						//if the codepoint in the file is equal to the codepoint that we want to insert
						if(StillInRange(codeIndex,ucdCharacters,currentProperty) &&
							(code == ((PUACharacter)ucdCharacters[codeIndex]).CodePoint))
						{
							// Replace the line with the new PuaDefinition
							AddUCDLine(tw,(UCDCharacter)ucdCharacters[codeIndex],add);
							// Look for the next PUA codepoint that we wish to insert
							codeIndex++;
						}
							//if it's not a first tag and the codepoints don't match
						else
						{
							tw.WriteLine(line);
						}
					}

					//If we have no more codepoints to insert in this section, then just finish writing this section
					if( ! StillInRange(codeIndex,ucdCharacters,currentProperty) )
					{
						line = ReadToEndOfSection(tr,tw,lastProperty,rangeCount,(UCDCharacter)ucdCharacters[0]);
						rangeCount = 0;
						dontRead = true;
						continue;
					}
					#endregion
				}
					//If it's a comment, simply write it out
				else
				{
					// find the total count comment and replace it with the current count.
					if(line.ToLowerInvariant().IndexOf("total code points")!=-1)
					{
						line = "# Total code points:" + rangeCount;
						rangeCount = 0;
					}
					tw.WriteLine(line);
				}
			}
		}

		#region ModifyUCDFile_helper_methods
		/// <summary>
		/// Read to the end of a given section of matching bidi class values.
		/// Passes everything read through to the writer.
		/// If this finds a section count comment, it will replace it with the current count.
		/// </summary>
		/// <param name="reader">The reader to read through.</param>
		/// <param name="lastBidiClass">The section we need to reed to the end of.</param>
		/// <param name="currentCount">The count of characters found before reading to the end of the section.</param>
		/// <param name="ucdCharacter">UCD Character used to know what kind of UCD file we are parsing.
		///		The actual contencts of the UCD Character are ignored.</param>
		/// <returns>The first line of the next section.</returns>
		private string ReadToEndOfSection(TextReader reader, TextWriter writer, string lastProperty, int currentCount, UCDCharacter ucdCharacter)
		{
			string line;
			string currentProperty;
			while((line = reader.ReadLine()) != null)
			{
				// if there is a bidi class value to read
				if(HasBidiData(line))
				{
					// increments the current count of codepoints found so far.
					IncrementCount(ref currentCount,line);

					// read the bidi value from the line
					currentProperty = GetProperty(line);

					// if it isn't in the current section we are done with section.
					if (!ucdCharacter.SameRegion(currentProperty,lastProperty))
						break;
				}
					// if its a comment, find the total count comment and replace it with the current count.
				else if(line.ToLowerInvariant().IndexOf("total code points")!=-1)
				{
					line = "# Total code points: " + currentCount;
					currentCount = 0;
				}
				// Write through all lines except the first line of the next section
				writer.WriteLine(line);
			}

			// Return the last line that we read
			// This is the first line of the next section, so someone will probably want to parse it.
			return line;
		}



		/// <summary>
		/// Prints a message to the console when storing a Unicode character.
		/// </summary>
		void LogCodepoint(string code)
		{
			if (LogFile.IsLogging())
				LogFile.AddErrorLine("Storing definition for Unicode character: " + code);
		}

		/// <summary>
		/// Given a line containing a valid code or code range, increments the count to include the code or code range.
		/// Uses XXXX..YYYY style range.
		/// </summary>
		/// <param name="currentCount">The current count to increment</param>
		/// <param name="line">The DerivedBidiClass.txt style line to use to increment.</param>
		/// <returns></returns>
		private static void IncrementCount(ref int currentCount, string line)
		{
			//Grab codepoint
			string code = line.Substring(0,line.IndexOf(';')).Trim();
			if(code.IndexOf('.')!=-1)
			{
				//Grabs the end codepoint
				string endCode = code.Substring(code.IndexOf("..")+2).Trim();
				code = code.Substring(0,code.IndexOf("..")).Trim();
				// Add all the characters in the range.
				currentCount+= (int)PUACharacter.SubHex(endCode,code)+1;
			}
				// we found another valid codepoint
			else
				currentCount++;
		}

		/// <summary>		returns true if the line is not just a comment
		/// i.e if it is of either of the following forms
		/// ------- ; ------ # ------
		/// ------- ; -------
		/// NOT ----- # ----- ; ------
		/// </summary>
		/// <param name="line"></param>
		/// <returns></returns>
		private static bool HasBidiData(string line)
		{
			return (line.IndexOf('#')>line.IndexOf(';') && line.IndexOf('#')!=-1 && line.IndexOf(';')!=-1) ||
				(line.IndexOf('#')==-1 && line.IndexOf(';')!=-1);
		}
		/// <summary>
		/// Reads the Property value from the given line of a UCD text file.
		/// (For example, if the text file is DerivedBidiClass.txt,
		///		this reads the bidi class value from the given line of a DerivedBidiClass.txt file.)
		/// </summary>
		/// <param name="line">A line from a UCD file in the following format:
		///		<code>code;Property[; other] [ # other]</code></param>
		/// <returns>The bidi class value, or bidi class value range </returns>
		public static string GetProperty(string line )
		{

			// (Note, by doing it in two steps, we are assured that even in strange cases like:
			//	code ; property # comment ; comment
			// it will stll work

			// Grab from the ; to the #, or the end of the line
			string propertyWithValue;
			//If a comment is not on the line
			if(line.IndexOf('#')==-1)
				propertyWithValue = line.Substring(line.IndexOf(';')+1).Trim();
				//If the line contains a comment
			else
				propertyWithValue = line.Substring(line.IndexOf(';')+1, line.IndexOf('#')-line.IndexOf(';')-1).Trim();

			// Return only from the first ';' to the second ';'
			if(propertyWithValue.IndexOf(';')!=-1)
				return propertyWithValue.Substring(0,propertyWithValue.IndexOf(';')).Trim();
			else
				return propertyWithValue;
		}
		/// <summary>
		/// Adds/"deletes" a given puaCharacter to the given TextWriter
		/// Will write a DerivedBidiClass.txt style line.
		/// </summary>
		/// <param name="puaCharacter">The character to add or delete</param>
		/// <param name="?"></param>
		public void AddUCDLine(TextWriter writer, UCDCharacter ucdCharacter, bool add)
		{
			if(add)
				writer.WriteLine(ucdCharacter.ToString()+" " + m_comment);
			// Uncomment this line to replace lines with a comment indicating that the line was removed.
			//			else
			//				writer.WriteLine("# DELETED LINE: {0}",puaCharacter.ToBidiString());
		}
		/// <summary>
		/// Checks to make sure the given codeIndex is within the current bidi section.
		/// Performs bounds checking as well.
		/// </summary>
		/// <param name="codeIndex">The index of a PUA character in puaCharacters</param>
		/// <param name="puaCharacters">The list of PUA characters</param>
		/// <param name="currentBidiClass">The current bidi class value.  If puaCharacters[codeIndex] doesn't match this we aren't in range.</param>
		/// <returns></returns>
		public bool StillInRange(int codeIndex, ArrayList ucdCharacters, string currentBidiClass)
		{
			return codeIndex < ucdCharacters.Count &&
				((UCDCharacter)ucdCharacters[codeIndex]).SameRegion(currentBidiClass);
		}
		#endregion
		/// <summary>
		/// Write a codepoint block, inserting the necessary codepoints properly.
		/// </summary>
		/// <param name="tw">DerivedBidiClass.txt file to write lines to.</param>
		/// <param name="originalBeginningCode">First codepoint in the block</param>
		/// <param name="originalEndCode">Last codepoint in the free block</param>
		/// <param name="codepointsWithinRange">An array of codepoints within the block, including the ends.
		///		DO NOT pass in points external to the free block.</param>
		///	<param name="generalCategory">The field that appears directly after the name in UnicodeData.txt.
		///		This will appear as the first element in a comment.</param>
		///	<param name="firstName">The name of the first letter in the range, for the comment</param>
		///	<param name="lastName">The name of the last letter in the range, for the comment</param>
		///	<param name="add"><code>true</code> for add <code>false</code> for delete.
		///	</summary>
		private void WriteBidiCodepointBlock(TextWriter writer,string originalBeginningCode,string originalEndCode,
			ArrayList codepointsWithinRange,
			string generalCategory,string firstName,string lastName,bool add)
		{
			//Allows us to store the original end and beginning code points while looping
			//through them
			string beginningCode=originalBeginningCode;
			string endCode=originalEndCode;

			UCDCharacter ucdCharacter;
			//Write each entry
			foreach(Object o in codepointsWithinRange)
			{
				ucdCharacter=(UCDCharacter)o;
				//If the current xmlCodepoint is the same as the beginning codepoint
				if(ucdCharacter.CompareTo(beginningCode)==0)
				{
					//Shift the beginning down one
					beginningCode = PUACharacter.AddHex(beginningCode,1);
					//Add or delete the character
					AddUCDLine(writer,ucdCharacter,add);
				}
					//If the current xmlCodepoint is between the beginning and end
				else if(ucdCharacter.CompareTo(endCode)!=0)
				{
					if(originalBeginningCode==beginningCode)
					{
						//We're writing a range block below the current xmlCodepoint
						WriteBidiRange(writer, beginningCode,
							PUACharacter.AddHex(ucdCharacter.CodePoint,-1), generalCategory,
							firstName, "???", ucdCharacter.Property);
					}
					else
					{
						//We're writing a range block below the current xmlCodepoint
						WriteBidiRange(writer, beginningCode,
							PUACharacter.AddHex(ucdCharacter.CodePoint,-1),
							generalCategory, "???", "???", ucdCharacter.Property);
					}
					AddUCDLine(writer,ucdCharacter,add);
					//Set the beginning code to be right after the ucdCharacterthat we just added
					beginningCode = PUACharacter.AddHex(ucdCharacter.CodePoint,1);
				}
					//If the current xmlCodepoint is the same as the end codepoint
				else
				{
					//Moves the end down a codepoint address
					endCode = PUACharacter.AddHex(endCode,-1);
					//Write our range of data
					WriteBidiRange(writer,beginningCode,endCode,generalCategory,"???","???",
						ucdCharacter.Property);
					//Writes the current line
					AddUCDLine(writer,ucdCharacter,add);
					return;
				}
			}
			//Write our range of data
			WriteBidiRange(writer,beginningCode,endCode,generalCategory,"???",lastName,
				((UCDCharacter)codepointsWithinRange[0]).Property);
		}

		/// <summary>
		/// Writes a block representing a range given first and last.
		/// If first is not before last, it will do the appropriate printing.
		/// </summary>
		/// <example>
		/// <code>
		/// ( { } are < > because this will interpret them as flags )
		///	E000;{Private Use, First};Co;0;L;;;;;N;;;;;
		/// F12F;{Private Use, Last};Co;0;L;;;;;N;;;;;   # [SIL-Corp] Added Feb 2004
		/// </code>
		/// -or-
		/// <code>
		///	E000;{Private Use};Co;0;L;;;;;N;;;;;
		/// </code>
		/// -or-
		/// <i>Nothing, since last was before beginning.</i>
		/// </example>
		/// <param name="writer">The StreamWriter to print to</param>
		/// <param name="beginning">A string hexadecimal representing the beginning.</param>
		/// <param name="end">A string hexadecimal representing the end.</param>
		/// <param name="generalCategory"></param>
		/// <param name="firstName"></param>
		/// <param name="lastName"></param>
		private int WriteBidiRange(TextWriter writer, string beginning, string end, string generalCategory,
			string firstName, string lastName, string bidiValue)
		{
			if(firstName=="")
				firstName="???";
			if(lastName=="")
				lastName="???";

			int codeRangeCount = (int)PUACharacter.SubHex(end,beginning)+1;

			switch(PUACharacter.CompareHex(end,beginning))
			{
			case -1:
				break;
			case 0:
				writer.WriteLine("{0,-14}; {1} # {2,-8} {3} OR {4}",
					beginning,bidiValue,generalCategory,firstName,lastName);
				break;
			case 1:
				string range = beginning+ ".." +end;
				string codeCount="["+codeRangeCount+"]";
				writer.WriteLine("{0,-14}; {2} # {3} {4,5} {5}..{6}",
					range,end,bidiValue,generalCategory,codeCount,firstName,lastName);
				break;
			}
			return codeRangeCount;
		}
		/// <summary>
		/// Writes a block representing a range given first and last.
		/// If first is not before last, it will do the appropriate printing.
		/// </summary>
		/// <example>
		/// <code>
		///	E000;{Private Use, First};Co;0;L;;;;;N;;;;;
		/// F12F;{Private Use, Last};Co;0;L;;;;;N;;;;;   # [SIL-Corp] Added Feb 2004
		/// </code>
		/// -or-
		/// <code>
		///	E000;{Private Use};Co;0;L;;;;;N;;;;;
		/// </code>
		/// -or-
		/// <i>Nothing, since last was before beginning.</i>
		/// </example>
		/// <param name="writer">The StreamWriter to print to</param>
		/// <param name="beginning">A string hexadecimal representing the beginning.</param>
		/// <param name="end">A string hexadecimal representing the end.</param>
		/// <param name="name">The name of the block, e.g. "Private Use"</param>
		/// <param name="data">The data to write after the block, e.g. ;Co;0;L;;;;;N;;;;;</param>
		private void WriteRange(StreamWriter writer, string beginning, string end, string name, string data)
		{
			switch(PUACharacter.CompareHex(end,beginning))
			{
			case -1:
				break;
			case 0:
				writer.WriteLine("{0};<{1}>{2}",beginning,name,data);
				break;
			case 1:
				writer.WriteLine("{0};<{1}, First>{2}",beginning,name,data);
				writer.WriteLine("{0};<{1}, Last>{2}",end,name,data);
				break;
			}
		}


		#endregion

		#region install LD File
		/// <summary>
		/// Process all changes related to ICU locale and collation files. This updates three
		/// source text files in icu\data\locales, and possibly two source text files in
		/// icu\data\coll, then uses these to generate corresponding res files in icu\icudtXXl
		/// and icu\icudtXXl\coll. Before modifying any of the files, it makes an original
		/// backup if one doesn't already exist. During the process, it makes backup files so
		/// that if anything goes wrong during the process, it can restore everything to the
		/// state prior to making these changes.
		/// </summary>
		/// <param name="m_ldData">The parser holding information from the language
		/// definition file</param>
		public void InstallLDFile(string ldFilename)
		{
			#region Just some test code Base Local Parsers
			//	This method was left here as a location where the parser test could be called
			//
			//			bool passed = TestBaseLocaleParsers();
			//
			//			bool test = true;
			//			if (test)
			//				throw new LDExceptions(ErrorCodes.ProgrammingError);
			#endregion

			// Parse the xml file into m_ldData
			ParseLanguageDataFile(ldFilename);

			// create the core 6 files:
			// root.txt, res_index.txt, xx.txt,
			// icu26ldt_root.res, icu26ldt_res_index.res, icu26ldt_xx.res
			//
			string rootTxtInput = m_localeDirectory + "root.txt";
			string resIndexTxtInput = m_localeDirectory + "res_index.txt";
			string rootResInput = m_icuBase + m_IcuData + "root.res";
			string resIndexResInput = m_icuBase + m_IcuData + "res_index.res";
			string xxTxtFile = m_localeDirectory + m_ldData.NewLocale + ".txt";
			string xxCollTxtFile = m_collDirectory + m_ldData.NewLocale + ".txt";
			string xxResFile = m_icuBase + m_IcuData + m_ldData.NewLocale + ".res";

			// the root file text has to exist for this process to work - throw exception
			if (!File.Exists(rootTxtInput))
				throw new LDExceptions(ErrorCodes.RootTxt_FileNotFound);

			// the res index file has to exist for this process to work - throw exception
			if (!File.Exists(resIndexTxtInput))
				throw new LDExceptions(ErrorCodes.ResIndexTxt_FileNotFound);

			// Determine which ICU locale resources should be updated with the name of the new language.
			int cwsNames = m_ldData.Names.Count;
			ICUInfo[] icuInfo = new ICUInfo[cwsNames];
			string[] rgXx = new string[cwsNames];
			string[] rgXxTxtFiles = new string[cwsNames];
			string[] rgXxResFiles = new string[cwsNames];
			string icuLocales = "";				// used for output tracing
			for (int iws = 0; iws < cwsNames; ++iws)
			{
				rgXx[iws] = ((StringWithWs)m_ldData.Names[iws]).icuLocale;
				icuLocales += " " + rgXx[iws];
				if (rgXx[iws] == m_ldData.NewLocale)
				{
					rgXxTxtFiles[iws] = null;	// (probably redundant...)
					rgXxResFiles[iws] = null;
				}
				else
				{
					rgXxTxtFiles[iws] = m_localeDirectory + rgXx[iws] + ".txt";
					rgXxResFiles[iws] = m_icuBase + m_IcuData + rgXx[iws] + ".res";
					// get information for this locale in the
					icuInfo[iws] = GetIcuResourceInfo(rgXxTxtFiles[iws], m_ldData.NewLocale, rgXx[iws]);
				}
			}

			// DN-271 use the Custom resource that contains all the Locales, Languages,
			// Countries and Variants that we've added to the root.txt file to see if
			// this is one of them.
			CustomResourceInfo newLangInfo = GetCustomResourceInfo(rootTxtInput, m_ldData.NewLocale, rgXx);

			if (LogFile.IsLogging())	// put out some debugging info
			{
				LogFile.AddLine("Locale  : <" + newLangInfo.LocaleItems.Locale + ">");
				LogFile.AddLine("Language: <" + newLangInfo.LocaleItems.LangKey + ">");
				LogFile.AddLine("Script  : <" + newLangInfo.LocaleItems.ScriptKey + ">");
				LogFile.AddLine("Country : <" + newLangInfo.LocaleItems.CountryKey + ">");
				LogFile.AddLine("Variant : <" + newLangInfo.LocaleItems.VariantKey + ">");

				string custom = "";
				if (newLangInfo.HasLocale)
					custom += " Locale";
				if (newLangInfo.HasLanguage)
					custom += " Language(" + icuLocales + ")";
				if (newLangInfo.HasScript)
					custom += " Script(" + icuLocales + ")";
				if (newLangInfo.HasCountry)
					custom += " Country(" + icuLocales + ")";
				if (newLangInfo.HasVariant)
					custom += " Variant(" + icuLocales + ")";
				if (custom.Length <= 0)
					custom = " None";

				LogFile.AddLine("Components that already exist in the custom resource: " + custom);

				for (int i = 0; i < cwsNames; i++)
				{
					string icu = "";
					ICUInfo info = icuInfo[i];
					if (info != null)
					{
						if (info.HasLanguage)
							icu += " Language";
						if (info.HasScript)
							icu += " Script";
						if (info.HasCountry)
							icu += " Country";
						if (info.HasVariant)
							icu += " Variant";
					}
					if (icu.Length <= 0)
						icu = " None";
					LogFile.AddLine("Components that already exist in " + rgXx[i] + ".txt: " + icu);
				}
			}

			// DN-246 1. see if it's a factory locale - if so, do Nothing, just return
			if (newLangInfo.HasLocale == false &&
				(File.Exists(xxTxtFile) || File.Exists(xxCollTxtFile)))
			{
				LogFile.AddLine("It's a factory Locale - do nothing");
				return;	// factory locale
			}

			// Check for ICU script and actual locale script key
			if (newLangInfo.LocaleItems.ScriptKey.Length > 0)
			{
				string icuScript, displayScript;
				Icu.UErrorCode err;
				StringUtils.InitIcuDataDir();	// initialize ICU data dir
				Icu.GetScriptCode(newLangInfo.LocaleItems.Locale, out icuScript, out err);

				if (newLangInfo.LocaleItems.ScriptKey != icuScript)
				{
					string script = newLangInfo.LocaleItems.ScriptKey;
					Icu.GetDisplayScript(newLangInfo.LocaleItems.Locale, "en", out displayScript, out err);

					string emsg = "For Locale " + newLangInfo.LocaleItems.Locale + ": ";
					emsg += "The script code <" + script +
						"> is mapping to the Icu code of <";
					emsg += icuScript + ">.  If you are specifying <" + displayScript;
					emsg += ">, please use <" + icuScript+ ">.  Otherwise, ";
					emsg += "please use a different script code.";
					LogFile.AddErrorLine(emsg);

					throw new LDExceptions(ErrorCodes.LDUsingISO3ScriptName, emsg);
				}
			}



			// Check for ICU country and actual locale country key : ISO
			if (newLangInfo.LocaleItems.CountryKey.Length > 0)
			{
				string icuCountry, displayCountry;
				Icu.UErrorCode err;
				StringUtils.InitIcuDataDir();	// initialize ICU data dir
				Icu.GetCountryCode(newLangInfo.LocaleItems.Locale, out icuCountry, out err);

				if (newLangInfo.LocaleItems.CountryKey != icuCountry)
				{
					string country = newLangInfo.LocaleItems.CountryKey;
					//					string isoCountry = GetISO3Country(newLangInfo.LocaleItems.Locale);
					Icu.GetDisplayCountry(newLangInfo.LocaleItems.Locale, "en",
						out displayCountry, out err);

					string emsg = "For Locale " + newLangInfo.LocaleItems.Locale + ": ";
					emsg += "The country code <" + country +
						"> is mapping to the Icu code of <";
					emsg += icuCountry + ">.  If you are specifying <" + displayCountry;
					emsg += ">, please use <" + icuCountry + ">.  Otherwise, ";
					emsg += "please use a different country code.";
					LogFile.AddErrorLine(emsg);

					throw new LDExceptions(ErrorCodes.LDUsingISO3CountryName, emsg);
				}
			}

			// Check for ICU language and actual locale language key
			if (newLangInfo.LocaleItems.LangKey.Length > 0)
			{
				string icuLanguage, displayLanguage;
				Icu.UErrorCode err;
				StringUtils.InitIcuDataDir();	// initialize ICU data dir
				Icu.GetLanguageCode(newLangInfo.LocaleItems.Locale, out icuLanguage, out err);

				if (newLangInfo.LocaleItems.LangKey != icuLanguage &&
					icuLanguage.Length > 0)
				{
					string language = newLangInfo.LocaleItems.LangKey;
					Icu.GetDisplayLanguage(newLangInfo.LocaleItems.Locale, "en",
						out displayLanguage, out err);

					string emsg = "For Locale " + newLangInfo.LocaleItems.Locale + ": ";
					emsg += "The language code <" + language +
						"> is mapping to the Icu code of <";
					emsg += icuLanguage + ">.  If you are specifying <" + displayLanguage;
					emsg += ">, please use <" + icuLanguage + ">.  Otherwise, ";
					emsg += "please use a different language code.";
					LogFile.AddErrorLine(emsg);

					throw new LDExceptions(ErrorCodes.LDUsingISO3LanguageName, emsg);
				}
			}

			// The icuSummary variables are only true if all the iculocales are true.
			// The icuAddToOne variables are true if any of the iculocales are false.
			bool icuSummaryLang = true, icuSummaryScript = true, icuSummaryCountry = true, icuSummaryVariant = true;
			bool icuAddToOneLang = false, icuAddToOneScript = false, icuAddToOneCountry = false, icuAddToOneVariant = false;
			foreach (ICUInfo info in icuInfo)
			{
				icuSummaryLang &= info.HasLanguage;
				icuSummaryScript &= info.HasScript;
				icuSummaryCountry &= info.HasCountry;
				icuSummaryVariant &= info.HasVariant;

				icuAddToOneLang |= !info.HasLanguage;
				icuAddToOneScript |= !info.HasScript;
				icuAddToOneCountry |= !info.HasCountry;
				icuAddToOneVariant |= !info.HasVariant;
			}

			// custom flags
			bool addToCLocale = !newLangInfo.HasLocale;
			bool addToCLanguage = !newLangInfo.HasLanguage && !icuSummaryLang;
			bool addToCScript = newLangInfo.LocaleItems.ScriptKey.Length > 0 && !newLangInfo.HasScript && !icuSummaryScript;
			bool addToCCountry = newLangInfo.LocaleItems.CountryKey.Length > 0 && !newLangInfo.HasCountry && !icuSummaryCountry;
			bool addToCVariant = newLangInfo.LocaleItems.VariantKey.Length>0 && !newLangInfo.HasVariant && !icuSummaryVariant;

			// A. ------------------------------------------------------------
			// Create the Original backups
			Generic.BackupOrig(rootTxtInput);
			Generic.BackupOrig(rootResInput);
			Generic.BackupOrig(resIndexTxtInput);
			Generic.BackupOrig(resIndexResInput);
			Generic.BackupOrig(xxTxtFile);
			Generic.BackupOrig(xxResFile);
			for (int iws = 0; iws < cwsNames; ++iws)
			{
				if (rgXxTxtFiles[iws] == null)
					continue;	// would match xxTxtFile
				Generic.BackupOrig(rgXxTxtFiles[iws]);
				Generic.BackupOrig(rgXxResFiles[iws]);
			}

			// B. ------------------------------------------------------------
			// Create the temporary files to serve as working copies
			string rootTxtTemp = Generic.CreateTempFile(rootTxtInput);	// root.txt
			string resIndexTxtTemp = Generic.CreateTempFile(resIndexTxtInput);	// res_index.txt
			string xxTxtTemp = Generic.CreateTempFile(xxTxtFile); // XX_YY_ZZ.txt

			AddTempFile(rootTxtTemp);
			AddTempFile(resIndexTxtTemp);
			AddTempFile(xxTxtTemp);

			string[] rgXxTxtTemps = new string[cwsNames];
			for (int iws = 0; iws < cwsNames; ++iws)
			{
				if (rgXxTxtFiles[iws] == null)
				{
					rgXxTxtTemps[iws] = null;	// (probably redundant...)
				}
				else
				{
					rgXxTxtTemps[iws] = Generic.CreateTempFile(rgXxTxtFiles[iws]);
					AddTempFile(rgXxTxtTemps[iws]);
				}
			}

			// C. ------------------------------------------------------------
			// Create the Undo-Restore backup files
			string rootTxtBackup = Generic.CreateBackupFile(rootTxtInput);
			string rootResBackup = Generic.CreateBackupFile(rootResInput);
			string resIndexTxtBackup = Generic.CreateBackupFile(resIndexTxtInput);
			string resIndexResBackup = Generic.CreateBackupFile(resIndexResInput);
			string xxTxtBackup = Generic.CreateBackupFile(xxTxtFile);
			string xxResBackup = Generic.CreateBackupFile(xxResFile);

			AddUndoFileFrame(rootTxtInput, rootTxtBackup);
			AddUndoFileFrame(rootResInput, rootResBackup);
			AddUndoFileFrame(resIndexTxtInput, resIndexTxtBackup);
			AddUndoFileFrame(resIndexResInput, resIndexResBackup);

			AddUndoFileFrame(xxTxtFile, xxTxtBackup);
			AddUndoFileFrame(xxResFile, xxResBackup);

			string[] rgXxTxtBackups = new string[cwsNames];
			string[] rgXxResBackups = new string[cwsNames];
			for (int iws = 0; iws < cwsNames; ++iws)
			{
				if (rgXxTxtFiles[iws] == null)
				{
					rgXxTxtBackups[iws] = null;		// (probably redundant...)
					rgXxResBackups[iws] = null;
				}
				else
				{
					rgXxTxtBackups[iws] = Generic.CreateBackupFile(rgXxTxtFiles[iws]);
					rgXxResBackups[iws] = Generic.CreateBackupFile(rgXxResFiles[iws]);
					AddUndoFileFrame(rgXxTxtFiles[iws], rgXxTxtBackups[iws]);
					AddUndoFileFrame(rgXxResFiles[iws], rgXxResBackups[iws]);
				}
			}

			int lineNumber;
			eAction er;

			// more logging information
			if (LogFile.IsLogging())	// put out some debuging info
			{
				LogFile.AddLine("The following changes are to be made to root.txt");
				LogFile.AddLine(" - Adding to Custom Locale  : " + addToCLocale.ToString());
				LogFile.AddLine(" - Adding to Custom Language: " + addToCLanguage.ToString());
				LogFile.AddLine(" - Adding to Custom Script  : " + addToCScript.ToString());
				LogFile.AddLine(" - Adding to Custom Country : " + addToCCountry.ToString());
				LogFile.AddLine(" - Adding to Custom Variant : " + addToCVariant.ToString());

				for (int iws = 0; iws < cwsNames; ++iws)
				{
					LogFile.AddLine("The following changes are to be made to " + rgXx[iws] + ".txt");
					LogFile.AddLine(" - Adding to ICU Language: " + (!icuInfo[iws].HasLanguage).ToString());

					LogFile.AddLine(" - Adding to ICU Script  : " + (newLangInfo.LocaleItems.ScriptKey.Length > 0 && !icuInfo[iws].HasScript).ToString());
					LogFile.AddLine(" - Adding to ICU Country : " + (newLangInfo.LocaleItems.CountryKey.Length > 0 && !icuInfo[iws].HasCountry).ToString());
					LogFile.AddLine(" - Adding to ICU Variant : " + (newLangInfo.LocaleItems.VariantKey.Length > 0 && !icuInfo[iws].HasVariant).ToString());
				}
			}

			//bool modifyCLanguage = false;
			//bool modifyCCountry = false;
			//bool modifyCVariant = false;
			//if (!addToCLanguage)
			//{
			//}
			//if (!addToCCountry)
			//{
			//}
			//if (!addToCVariant)
			//{
			//}

			// Add the custom resources to the root text input file
			// Those are children of 'root.Custom'
			if (addToCLocale || addToCLanguage || addToCScript || addToCCountry || addToCVariant ||	!newLangInfo.HasCustom)
			{
				if( !m_runSlow )
				{
					IcuDataNode rootNode = ICUDataFiles.ParsedFile(rootTxtInput);

					// If Custom doesn't exist, make it and it's four children
					if (new NodeSpecification("root","Custom").FindNode(rootNode,false) == null )
					{
						// Get the process name for the comment
						string exeName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;

						System.Globalization.CultureInfo ci = System.Globalization.CultureInfo.CreateSpecificCulture("en-US");
						// the comment
						string comment = "";
						comment += m_StartComment + NL;
						comment += "// This section is maintained by the '" + exeName;
						comment += "' Application." + NL;
						// Note we can't use local culture info as Korean/Chinese, etc. will introduce utf-8
						// characters that will cause icu tools to fail.
						comment += "// Created: " + DateTime.Now.ToString("F", ci);

						IcuDataNode customNode = new IcuDataNode("Custom",rootNode,comment,m_EndComment + NL + NL);

						customNode.AddChildSmart(new IcuDataNode("LocalesAdded",customNode,"",""),false);
						customNode.AddChildSmart(new IcuDataNode("LanguagesAdded",customNode,"",""),false);
						customNode.AddChildSmart(new IcuDataNode("ScriptsAdded", customNode, "", ""), false);
						customNode.AddChildSmart(new IcuDataNode("CountriesAdded", customNode, "", ""), false);
						customNode.AddChildSmart(new IcuDataNode("VariantsAdded", customNode, "", ""), false);

						// Add custom to root
						ICUDataFiles.SetICUDataFileNode(rootTxtInput, new NodeSpecification("root"), customNode, true);
					}
					// Make all the Custom Nodes
					if (addToCLocale)
						ICUDataFiles.SetICUDataFileAttribute(rootTxtInput,
							new NodeSpecification("root","Custom","LocalesAdded"),
							newLangInfo.LocaleItems.Locale, m_comment);

					if (addToCLanguage)
					{
						IcuDataNode customNode = rootNode.Child("Custom");
						IcuDataNode addedNames = customNode.Child("LanguagesAdded");
						IcuDataNode dnXX = new IcuDataNode(newLangInfo.LocaleItems.LangKey, addedNames, "", "");
						addedNames.AddChildSmart(dnXX, false);
						for (int iws = 0; iws < cwsNames; ++iws)
						{
							if (!icuInfo[iws].HasLanguage)	// rgfAddToICULanguage[iws])
							{
								string xx = ((StringWithWs)m_ldData.Names[iws]).icuLocale;
								Debug.Assert(xx != m_ldData.NewLocale);
								Debug.Assert(rgXxTxtFiles[iws] != null);
								dnXX.AddAttributeSmart(
									new IcuDataNode.IcuDataAttribute(xx, ", //" + m_comment + NL, true));
							}
						}
					}

					if (addToCScript)
					{
						// If ScriptsAdded doesn't exist, make it
						if (new NodeSpecification("root", "Custom", "ScriptsAdded").FindNode(rootNode, false) == null)
						{
							// Get the process name for the comment
							string exeName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;

							System.Globalization.CultureInfo ci = System.Globalization.CultureInfo.CreateSpecificCulture("en-US");
							// the comment
							string comment = "";
							comment += m_StartComment + NL;
							comment += "// This section is maintained by the '" + exeName;
							comment += "' Application." + NL;
							// Note we can't use local culture info as Korean/Chinese, etc. will introduce utf-8
							// characters that will cause icu tools to fail.
							comment += "// Created: " + DateTime.Now.ToString("F", ci);

							IcuDataNode customNode = rootNode.Child("Custom");
							customNode.AddChildSmart(new IcuDataNode("ScriptsAdded", customNode, "", ""), false);

							//// Add custom to root
							//ICUDataFiles.SetICUDataFileNode(rootTxtInput, new NodeSpecification("root"), customNode, true);
						}


						// first see if the script already exists, if so just add the icuLocale(s) to the attributes
						NodeSpecification scriptNodeSpec = new NodeSpecification("root", "Custom", "ScriptsAdded", newLangInfo.LocaleItems.ScriptKey);
						IcuDataNode scriptNode = scriptNodeSpec.FindNode(rootNode, false);
						if (scriptNode != null)
						{
							for (int iws = 0; iws < cwsNames; ++iws)
							{
								if (!icuInfo[iws].HasScript)
									scriptNode.AddAttribute(rgXx[iws], ", //" + m_comment + NL, true);
							}
						}
						else
						{
							IcuDataNode customNode = rootNode.Child("Custom");
							IcuDataNode addedNames = customNode.Child("ScriptsAdded");
							IcuDataNode dnXX = new IcuDataNode(newLangInfo.LocaleItems.ScriptKey, addedNames, "", "");
							addedNames.AddChildSmart(dnXX, false);
							for (int iws = 0; iws < cwsNames; ++iws)
							{
								if (!icuInfo[iws].HasScript)
								{
									string xx = ((StringWithWs)m_ldData.Names[iws]).icuLocale;
									Debug.Assert(xx != m_ldData.NewLocale);
									Debug.Assert(rgXxTxtFiles[iws] != null);
									dnXX.AddAttributeSmart(
										new IcuDataNode.IcuDataAttribute(xx, ", //" + m_comment + NL, true));
								}
							}
						}
					}

					if (addToCCountry)
					{
						// first see if the country already exists, if so just add the icuLocale(s) to the attributes
						NodeSpecification countryNodeSpec = new NodeSpecification("root", "Custom", "CountriesAdded", newLangInfo.LocaleItems.CountryKey);
						IcuDataNode countryNode = countryNodeSpec.FindNode(rootNode, false);
						if (countryNode != null)
						{
							for (int iws = 0; iws < cwsNames; ++iws)
							{
								if (!icuInfo[iws].HasCountry)
									countryNode.AddAttribute(rgXx[iws], ", //" + m_comment + NL, true);
							}
						}
						else
						{
							IcuDataNode customNode = rootNode.Child("Custom");
							IcuDataNode addedNames = customNode.Child("CountriesAdded");
							IcuDataNode dnXX = new IcuDataNode(newLangInfo.LocaleItems.CountryKey, addedNames, "", "");
							addedNames.AddChildSmart(dnXX, false);
							for (int iws = 0; iws < cwsNames; ++iws)
							{
								if (!icuInfo[iws].HasCountry)
								{
									string xx = ((StringWithWs)m_ldData.Names[iws]).icuLocale;
									Debug.Assert(xx != m_ldData.NewLocale);
									Debug.Assert(rgXxTxtFiles[iws] != null);
									dnXX.AddAttributeSmart(
										new IcuDataNode.IcuDataAttribute(xx, ", //" + m_comment + NL, true));
								}
							}
						}
					}

					if (addToCVariant)
					{
						// first see if the variant alredy exists, if so just add the icuLocale(s) to the attributes
						NodeSpecification variantNodeSpec = new NodeSpecification("root", "Custom", "VariantsAdded", newLangInfo.LocaleItems.VariantKey);
						IcuDataNode variantNode = variantNodeSpec.FindNode(rootNode, false);
						if (variantNode != null)
						{
							for (int iws = 0; iws < cwsNames; ++iws)
							{
								if (!icuInfo[iws].HasVariant)
									variantNode.AddAttribute(rgXx[iws], ", //" + m_comment + NL, true);
							}
						}
						else
						{

							IcuDataNode customNode = rootNode.Child("Custom");
							IcuDataNode addedNames = customNode.Child("VariantsAdded");
							IcuDataNode dnXX = new IcuDataNode(newLangInfo.LocaleItems.VariantKey, addedNames, "", "");
							addedNames.AddChildSmart(dnXX, false);
							for (int iws = 0; iws < cwsNames; ++iws)
							{
								if (!icuInfo[iws].HasVariant)
								{
									string xx = ((StringWithWs)m_ldData.Names[iws]).icuLocale;
									Debug.Assert(xx != m_ldData.NewLocale);
									Debug.Assert(rgXxTxtFiles[iws] != null);
									dnXX.AddAttributeSmart(
										new IcuDataNode.IcuDataAttribute(xx, ", //" + m_comment + NL, true));
								}
							}
						}
					}
				}
				else
				{
					// read in the root.txt file for custom resource processing
					string fileData = FileToString(rootTxtInput);

					// make sure the custom resource exists
					if (newLangInfo.HasCustom == false)
						AddCustomResource(ref fileData);

					if (addToCLocale)
						AddCustomLocale(ref fileData, newLangInfo.LocaleItems.Locale);

					if (addToCLanguage)
						AddCustomLanguage(ref fileData, newLangInfo.LocaleItems.LangKey);

					if (addToCScript)
						AddCustomScript(ref fileData, newLangInfo.LocaleItems.ScriptKey);

					if (addToCCountry)
						AddCustomCountry(ref fileData, newLangInfo.LocaleItems.CountryKey);

					if (addToCVariant)
						AddCustomVariant(ref fileData, newLangInfo.LocaleItems.VariantKey);

					StringToFile(rootTxtTemp, fileData);
					// put the file back out - with new custom resource changes
				}
			}

			// DN-246 Step #2
			// pull out the NewLocale data and create the new locale.txt file for writing
			if (m_ldData.NewLocale == string.Empty || m_ldData.NewLocale.Length <= 0)
			{
				// no NewLocale information - not valid
				throw new LDExceptions(ErrorCodes.NewLocaleFile);
			}

			// create the output file, replace previous if one already exists

			// DN-246 Step #3

			// Required data for locale file (in case base locale not defined)
			ReadAndHashString("    // Default Version" + NL +
				"    Version { \"1.0\" }" + NL);
			ReadAndHashString("    // Default to English" + NL +
				"    LocaleID:int { 0x09 }" + NL);

			string newLocaleAbbr_name = newLangInfo.LocaleItems.LangKey;
			string newLocaleAbbr_script = newLangInfo.LocaleItems.ScriptKey;
			string newLocaleAbbr_country = newLangInfo.LocaleItems.CountryKey;
			string newLocaleAbbr_variant = newLangInfo.LocaleItems.VariantKey;

			// If BaseLocale is populated in the LD file, fill the output file with information
			// from the source files for the BaseLocale name.
			// sample BaseLocale = "en_gb_EURO"
			if (m_ldData.BaseLocale != null && m_ldData.BaseLocale.Length > 0)
			{
				string baseName = "";
				string baseScript = "";
				string baseCountry = "";
				string baseVariant = "";
				string baseLocale = m_ldData.BaseLocale;

				ParseLocaleName(ref baseLocale, out baseName, out baseScript, out baseCountry,
					out baseVariant);

				// Read all of the base source locale files and write to the output file storage
				// area - m_entries.  Only write to the outputfile after this process is
				// method has completed sucessfully.

				string filename = m_localeDirectory;
				if (baseName.Length > 0)
				{
					filename += baseName;
					try
					{
						ReadAndHashLocaleFile(filename + ".txt");
					}
					catch
					{
						// just skip if the txt file doesn't exist - not required at this point
					}
				}
				filename += "_";
				if (baseScript.Length > 0)
				{
					filename += baseScript;
					try
					{
						ReadAndHashLocaleFile(filename + ".txt");
					}
					catch
					{
						// just skip if the txt file doesn't exist - not required at this point
					}
				}
				filename += "_";
				if (baseCountry.Length > 0)
				{
					filename += baseCountry;
					try
					{
						ReadAndHashLocaleFile(filename + ".txt");
					}
					catch
					{
						// just skip if the txt file doesn't exist - not required at this point
					}
				}
				filename += "_";
				if (baseVariant.Length > 0)
				{
					filename += baseVariant;
					try
					{
						ReadAndHashLocaleFile(filename + ".txt");
					}
					catch
					{
						// just skip if the txt file doesn't exist - not required at this point
					}
				}
			}
			else
			{
			}

			// DN-246 Step #4
			// Add LocaleResources to the output area - m_entries
			if (m_ldData.LocaleResources != null && m_ldData.LocaleResources.Length > 0)
			{
				ReadAndHashString(m_ldData.LocaleResources);
			}

			// Add The Locale ID value to the output area - m_entries
			if (m_ldData.LocaleWinLCID != null && m_ldData.LocaleWinLCID.Length > 0)
			{
				string IDLine = "    LocaleID:int { " + m_ldData.LocaleWinLCID + " }" + NL;
				ReadAndHashString(IDLine);
			}

			if( !m_runSlow )
				Generic.FileCopyWithLogging(rootTxtTemp, rootTxtInput, true);

			// DN-246 Step #5

			// This is where we update each XX.txt file with the needed locale properties: lang, country, variant.
			for (int iws = 0; iws < cwsNames; ++iws)
			{
				if (rgXxTxtFiles[iws] == null)
				{
					Debug.Assert(rgXx[iws] == m_ldData.NewLocale);
					Debug.Assert(icuInfo[iws].HasLanguage == false);	// rgfAddToICULanguage[iws] == false);
					continue;
				}
				bool fAddOrReplace = !icuInfo[iws].HasLanguage;
				if (!fAddOrReplace)
				{
					// Maybe we need to modify the value?
					string sNew = m_ldData.LocaleName;
					if (sNew != null && sNew.Length > 0)
					{
						string sOld = sNew;
						Icu.UErrorCode uerr = Icu.UErrorCode.U_ZERO_ERROR;
						Icu.GetDisplayName(m_ldData.NewLocale, rgXx[iws], out sOld, out uerr);
						if (uerr == Icu.UErrorCode.U_ZERO_ERROR)
							fAddOrReplace = (sNew != sOld);
					}
				}
				if (fAddOrReplace)
				{
					string name = m_ldData.LocaleName;
					if (name == null || name.Length <= 0 )
						name = newLocaleAbbr_name;

					ICUDataFiles.SetICUDataFileNode(rgXxTxtFiles[iws],
							new NodeSpecification(rgXx[iws], "Languages"),
							new IcuDataNode(newLocaleAbbr_name, name, "// " + m_comment),
							false);
				}
				// first Guess at adding the Script code
				fAddOrReplace = !icuInfo[iws].HasScript;
				if (!fAddOrReplace)
				{
					// Maybe we need to modify the value?
					string sNew = m_ldData.LocaleScript;
					if (sNew != null && sNew.Length > 0)
					{
						string sOld = sNew;
						Icu.UErrorCode uerr = Icu.UErrorCode.U_ZERO_ERROR;
						Icu.GetDisplayScript(m_ldData.NewLocale, rgXx[iws], out sOld, out uerr);
						if (uerr == Icu.UErrorCode.U_ZERO_ERROR)
							fAddOrReplace = (sNew != sOld);
					}
				}
				if (newLocaleAbbr_script.Length > 0 && fAddOrReplace)
				{
					string name = m_ldData.LocaleScript;
					if (name == null || name.Length <= 0)
						name = newLocaleAbbr_script;

					ICUDataFiles.SetICUDataFileNode(rgXxTxtFiles[iws],
							new NodeSpecification(rgXx[iws], "Scripts"),
							new IcuDataNode(newLocaleAbbr_script, name, "// " + m_comment), false);
				}
				// DN-246 Step #6
				fAddOrReplace = !icuInfo[iws].HasCountry;
				if (!fAddOrReplace)
				{
					// Maybe we need to modify the value?
					string sNew = m_ldData.LocaleCountry;
					if (sNew != null && sNew.Length > 0)
					{
						string sOld = sNew;
						Icu.UErrorCode uerr = Icu.UErrorCode.U_ZERO_ERROR;
						Icu.GetDisplayCountry(m_ldData.NewLocale, rgXx[iws], out sOld, out uerr);
						if (uerr == Icu.UErrorCode.U_ZERO_ERROR)
							fAddOrReplace = (sNew != sOld);
					}
				}
				if (newLocaleAbbr_country.Length > 0 && fAddOrReplace)
				{
					string name = m_ldData.LocaleCountry;
					if (name == null || name.Length <= 0 )
						name = newLocaleAbbr_country;

					ICUDataFiles.SetICUDataFileNode(rgXxTxtFiles[iws],
							new NodeSpecification(rgXx[iws],"Countries"),
							new IcuDataNode(newLocaleAbbr_country, name, "// " + m_comment),false);
				}
				// DN-246 Step #7
				fAddOrReplace = !icuInfo[iws].HasVariant;
				if (!fAddOrReplace)
				{
					// Maybe we need to modify the value?
					string sNew = m_ldData.LocaleVariant;
					if (sNew != null && sNew.Length > 0)
					{
						string sOld = sNew;
						Icu.UErrorCode uerr = Icu.UErrorCode.U_ZERO_ERROR;
						Icu.GetDisplayVariant(m_ldData.NewLocale, rgXx[iws], out sOld, out uerr);
						if (uerr == Icu.UErrorCode.U_ZERO_ERROR)
							fAddOrReplace = (sNew != sOld);
					}
				}
				if (newLocaleAbbr_variant.Length > 0 && fAddOrReplace)
				{
					string name = m_ldData.LocaleVariant;
					if (name == null || name.Length <= 0 )
						name = newLocaleAbbr_variant;

					ICUDataFiles.SetICUDataFileNode(rgXxTxtFiles[iws],
							new NodeSpecification(rgXx[iws],"Variants"),
							new IcuDataNode(newLocaleAbbr_variant, name, "// " + m_comment),false);
				}
			}


			// DN-246 Step #8

			// Produce the XXX.txt file.  Close the output file.
			WriteLocaleFile(m_ldData.NewLocale, xxTxtTemp);

			if( !m_runSlow )
			{
				ICUDataFiles.SetICUDataFileNode(resIndexTxtInput,
					new NodeSpecification("res_index:table(nofallback)","InstalledLocales"),
					new IcuDataNode(m_ldData.NewLocale, "", "// " + m_comment),false);
			}
			else
			{
				// Add to res_index.txt file if not already present.
				er = FindResource(resIndexTxtInput, "InstalledLocales", m_ldData.NewLocale, "",
					out lineNumber);
				ModifyFile(resIndexTxtInput, resIndexTxtTemp, lineNumber,
					"        " + m_ldData.NewLocale + " {\"\"}",	er, ErrorCodes.ResIndexFile);
			}

			if( !m_runSlow )
			{
				// Write all Files for all steps so far
				ICUDataFiles.WriteFiles();
				// Copy to the temp files because these are what we are going to use.
				Generic.FileCopyWithLogging(rootTxtInput,rootTxtTemp,true);
				Generic.FileCopyWithLogging(resIndexTxtInput,resIndexTxtTemp,true);
				for (int iws = 0; iws < cwsNames; ++iws)
				{
					if (rgXxTxtTemps[iws] != null)
						Generic.FileCopyWithLogging(rgXxTxtFiles[iws], rgXxTxtTemps[iws], true);
				}
			}

			// DN-246 Step #9 - generate .res files

			// Process all files together and catch errors.
			ArrayList Files = new ArrayList();
			Files.Add(resIndexTxtTemp);
			Files.Add(xxTxtTemp);
			Files.Add(rootTxtTemp);
			for (int iws = 0; iws < cwsNames; ++iws)
			{
				if (rgXxTxtTemps[iws] != null)
					Files.Add(rgXxTxtTemps[iws]);
			}
			GenerateResFile(Files, m_localeDirectory, m_icuBase + m_IcuData,
				ErrorCodes.GeneralFile, "InstallLDFile");
			Generic.FileCopyWithLogging(rootTxtTemp, rootTxtInput, true);
			Generic.FileCopyWithLogging(resIndexTxtTemp, resIndexTxtInput, true);
			Generic.FileCopyWithLogging(xxTxtTemp, xxTxtFile, true);
			for (int iws = 0; iws < cwsNames; ++iws)
			{
				if (rgXxTxtTemps[iws] != null)
					Generic.FileCopyWithLogging(rgXxTxtTemps[iws], rgXxTxtFiles[iws], true);
			}

			// FINAL STEP: Check for Collation information.
			string sColl = m_ldData.CollationElements;
			// Note, if we already have a collation file, we need to process it again in case
			// the user removed all collation elements to revert to the default collation.
			string colFile = m_collDirectory + m_ldData.NewLocale + ".txt";
			if (File.Exists(colFile) || sColl != null && sColl != "")
				InstallCollation();
			else if (m_ldData.BaseLocale != null && File.Exists(m_collDirectory + m_ldData.BaseLocale + ".txt"))
			{
				// make sure if it has a baselocale - and that has a collation - use it
				InstallCollation();
			}
		}


		/// <summary>
		/// Create the collation file, modify the collation index file, and generate those two
		/// resource files.
		/// </summary>
		private void InstallCollation()
		{
			ICUDataFiles.Reset();

			string resIndexTxtInput = m_collDirectory + "res_index.txt";
			string resIndexResInput = m_icuBase + m_IcuData + "coll\\res_index.res";
			// the res index file has to exist for this process to work - throw exception
			if (!File.Exists(resIndexTxtInput))
				throw new LDExceptions(ErrorCodes.ResIndexTxt_FileNotFound);

			string xxTxtFile = m_collDirectory + m_ldData.NewLocale + ".txt";
			string xxResFile = m_icuBase + m_IcuData + "coll\\" + m_ldData.NewLocale + ".res";

			// A. ------------------------------------------------------------
			// Create the Original backups
			Generic.BackupOrig(resIndexTxtInput);
			Generic.BackupOrig(resIndexResInput);
			Generic.BackupOrig(xxTxtFile);
			Generic.BackupOrig(xxResFile);

			// B. ------------------------------------------------------------
			// Create the temporary files to serve as working copies
			string resIndexTxtTemp = Generic.CreateTempFile(resIndexTxtInput);	// res_index.txt
			string xxTxtTemp = Generic.CreateTempFile(xxTxtFile); // XX_YY_ZZ.txt
			AddTempFile(resIndexTxtTemp);
			AddTempFile(xxTxtTemp);

			// C. ------------------------------------------------------------
			// Create the Undo-Restore backup files
			string resIndexTxtBackup = Generic.CreateBackupFile(resIndexTxtInput);
			string resIndexResBackup = Generic.CreateBackupFile(resIndexResInput);
			string xxTxtBackup = Generic.CreateBackupFile(xxTxtFile);
			string xxResBackup = Generic.CreateBackupFile(xxResFile);

			AddUndoFileFrame(resIndexTxtInput, resIndexTxtBackup);
			AddUndoFileFrame(resIndexResInput, resIndexResBackup);
			AddUndoFileFrame(xxTxtFile, xxTxtBackup);
			AddUndoFileFrame(xxResFile, xxResBackup);

			// Produce the XXX.txt file.
			WriteCollationFile(m_ldData.BaseLocale, m_ldData.NewLocale, xxTxtTemp);

			if( !m_runSlow )
			{
				ICUDataFiles.SetICUDataFileNode(resIndexTxtInput,
					new NodeSpecification("res_index:table(nofallback)","InstalledLocales"),
					new IcuDataNode(m_ldData.NewLocale, "", "// " + m_comment),false);
			}
			else
			{
				int lineNumber;
				eAction er;
				// Add to res_index.txt file if not already present.
				er = FindResource(resIndexTxtInput, "InstalledLocales", m_ldData.NewLocale, "",
					out lineNumber);
				ModifyFile(resIndexTxtInput, resIndexTxtTemp, lineNumber,
					"        " + m_ldData.NewLocale + " {\"\"}",	er, ErrorCodes.ResIndexFile);
			}

			if( !m_runSlow )
			{
				// Write all Files for all steps so far
				ICUDataFiles.WriteFiles();
				// Copy to the temp files because these are what we are going to use.
				Generic.FileCopyWithLogging(resIndexTxtInput,resIndexTxtTemp,true);
			}

			// Process all files together and catch errors.
			ArrayList Files = new ArrayList();
			Files.Add(resIndexTxtTemp);
			Files.Add(xxTxtTemp);
			GenerateResFile(Files, m_collDirectory, m_icuBase + m_IcuData + "coll",
				ErrorCodes.GeneralFile, "InstallCollation");

			Generic.FileCopyWithLogging(resIndexTxtTemp, resIndexTxtInput, true);
			Generic.FileCopyWithLogging(xxTxtTemp, xxTxtFile, true);
		}

		/// <summary>
		/// Consume "localename {" from input file.
		/// </summary>
		/// <param name="inputStream"></param>
		private void ReadFirstEntry(StreamReader inputStream)
		{
			string newLine, s;
			int bracePosition = -1;

			while (inputStream.Peek() > -1)
			{
				newLine = inputStream.ReadLine();
				if (newLine.Length == 0)
				{
					continue; // do not parse blank lines.
				}

				// look for a comment line
				s = reCommentLine.Match(newLine).ToString();
				if (s.Length > 0)
				{
					continue; // do not parse commented lines.
				}

				// look for a string literal line
				s = reStringLiteral.Match(newLine).ToString();
				if (s.Length > 0)
				{
					string s1 = reResourceQuoted.Match(newLine).ToString();
					if (s1.Length <= 0)
						continue; // do not parse lines starting with literal strings.
				}

				/// the code isn't using the reStartEntry Regex - Is that right??

				// if end of LocalEntry in the line
				bracePosition = newLine.IndexOf("{");
				if (-1 != bracePosition)
				{
					break;
				}
			}
		}


		/// <summary>
		/// Get the next entry from the input file.
		/// </summary>
		/// <param name="inputStream"></param>
		/// <returns></returns>
		public LocalEntry ReadNextEntry(StreamReader inputStream)
		{
			LocalEntry entry = new LocalEntry();
			string newLine;
			int bracePosition = -1;
			int startPosition = 0;
			bool fParsingEntry = false;
			bool fSeenData = false;
			int nesting = 0;

			fParsingEntry = true;
			while ((inputStream.Peek() > -1) && fParsingEntry)
			{
				fSeenData = true;
				bool fAddFinialNewLine = true;
				newLine = inputStream.ReadLine();

				if (newLine.Length == 0)
				{
					entry.text += newLine + NL;
					continue; // blank line
				}
				string s;
				s = reCommentLine.Match(newLine).ToString();
				if (s.Length > 0)
				{
					entry.text += newLine + NL;
					continue; // commented line.
				}

				s = reSpecialTag.Match(newLine).ToString();
				if (s.Length > 0)
				{
					string name = newLine.Substring(s.IndexOf("%%"));
					entry.name = name.Substring(0, name.IndexOf("\""));
					entry.text += newLine + NL;
					return entry;
				}

				s = reTransLit.Match(newLine).ToString();
				if (s.Length > 0)
				{
					string name = newLine.Substring(s.IndexOf("\""));
					entry.name = name.Substring(0, name.IndexOf("\"",1)+1);
					entry.text += newLine + NL;
					return entry;
				}

				s = reStringLiteral.Match(newLine).ToString();
				if (s.Length > 0)
				{
					string s1 = reResourceQuoted.Match(newLine).ToString();
					if (s1.Length <= 0)
					{
						entry.text += newLine + NL;
						int iLastQuote = newLine.LastIndexOf("\"");
						newLine = newLine.Substring(iLastQuote);
						fAddFinialNewLine = false;
					}
				}

				if ((! fParsingEntry) || (entry.name == null))
				{
					s = reStartEntry.Match(newLine).ToString();
					s = s.Replace("{", "");
					s = s.Trim();
					// todo - also need to parse and save type (ex. "int" in "LocaleID:int")

					if (s.Length > 0)
					{
						fParsingEntry = true;
						entry.name = s;
					}
				}

				int endOfLine = newLine.IndexOf("//");
				if (endOfLine == -1)
				{
					// line does not contain a comment.
					endOfLine = newLine.Length;
				}

				// Take into account opening braces.
				startPosition = bracePosition = 0;
				while (bracePosition != -1)
				{
					// if end of LocalEntry in the line
					// dlh --- bracePosition + 1);
					bracePosition = newLine.IndexOf("{", startPosition);
					if ((-1 != bracePosition) && (bracePosition < endOfLine))
					{
						// found an opening brace in the input line.
						nesting++;
					}
					startPosition = bracePosition + 1;	// don't find the same one again
				}

				// Take into account closing braces.
				startPosition = bracePosition = 0;
				while (bracePosition != -1)
				{
					bracePosition = newLine.IndexOf("}", startPosition);
					if ((-1 != bracePosition) && (bracePosition < endOfLine))
					{
						nesting--;
						// dlh ---
						//						if (nesting < 0)
						//						{
						//							throw new LDExceptions(ErrorCodes.LDBadData);
						//						}
						// TODO:  need to enhance to handle escaped brace
						if (nesting == 0)
						{
							fParsingEntry = false;
							break;
						}
					}
					startPosition = bracePosition + 1;	// don't find the same one again
				}
				if (fAddFinialNewLine)
					entry.text += newLine + NL;
			};
			if (fSeenData && fParsingEntry && nesting != -1)	// dlh --- 0)
				throw new LDExceptions(ErrorCodes.LDBadData);

			return entry;
		}


		/// <summary>
		/// Populate the entry hashtable from the input file.
		///
		/// NOTE:  subsequent calls to this method will overwrite entries that have already
		///        been encountered in previous files.
		/// </summary>
		/// <param name="inputFilespec"></param>
		private void ReadAndHashLocaleFile(string inputFilespec)
		{
			if (!File.Exists(inputFilespec))
				throw new LDExceptions(ErrorCodes.FileNotFound);

			LogFile.AddVerboseLine("StreamReader on <" + inputFilespec + ">");
			StreamReader inputStream = new StreamReader(inputFilespec,
				System.Text.Encoding.Default, true);	// Check for Unicode BOM chars.

			LocalEntry entry;

			// Consume "localename {" from input file.
			ReadFirstEntry(inputStream);

			// Insert all entries in locale file into hashtable
			do
			{
				entry = ReadNextEntry(inputStream);

				if ((entry.name != null) && (entry.name.Length > 0))
				{
					try
					{
						m_entries.Remove(entry.name);
					}
					finally
					{
						m_entries.Add(entry.name, entry);
						if (! m_keys.Contains(entry.name))
							m_keys.Add(entry.name);
					}

				}
			} while ((entry.name != null) || (entry.text != null));
			// This input needs to be closed, otherwise future file IO can fail
			inputStream.Close();
		}


		/// <summary>
		/// Add a string to the hash.
		/// </summary>
		/// <param name="inputString"></param>
		private void ReadAndHashString(string inputString)
		{
			byte[] inputByteArray = Encoding.UTF8.GetBytes(inputString);

			MemoryStream inputMemoryStream = new MemoryStream(inputByteArray);
			LogFile.AddVerboseLine("StreamReader on <" + "From a MemoryStream" + ">");
			StreamReader inputStream = new StreamReader(inputMemoryStream,
				System.Text.Encoding.Default, true);	// Check for Unicode BOM chars.

			LocalEntry entry;

			// Insert all entries in inputString file into hashtable
			do
			{
				entry = ReadNextEntry(inputStream);

				if ((entry.name != null) && (entry.name.Length > 0))
				{
					try
					{
						m_entries.Remove(entry.name);
					}
					finally
					{
						m_entries.Add(entry.name, entry);
						if (! m_keys.Contains(entry.name))
							m_keys.Add(entry.name);
					}

				}
			} while ((entry.name != null) || (entry.text != null));
		}


		/// <summary>
		/// Write the locale file.
		/// </summary>
		/// <param name="newLocaleName"></param>
		/// <param name="outputFilespec"></param>
		public void WriteLocaleFile(string newLocaleName, string outputFilespec)
		{
			StreamWriter outputStream = new StreamWriter(outputFilespec, false,
				System.Text.Encoding.UTF8);

			// Output locale name and opening brace.
			outputStream.WriteLine(newLocaleName + " {");
			bool fNeedScripts = true;
			bool fNeedCountries = true;
			bool fNeedLanguages = true;
			bool fNeedVariants = true;
			if (m_entries != null)
			{
				// Output all locale file entries.
				string s;
				LocalEntry entry;
				//ICollection ic = m_entries.Keys;
				foreach(string key in m_keys)
				{
					entry = (LocalEntry)m_entries[key];
					s = entry.text;

					if (s.IndexOf("Scripts") > 0 && s.IndexOf("{") > s.IndexOf("Scripts"))
						fNeedScripts = false;
					if (s.IndexOf("Countries") > 0 && s.IndexOf("{") > s.IndexOf("Countries"))
						fNeedCountries = false;
					if (s.IndexOf("Languages") > 0 && s.IndexOf("{") > s.IndexOf("Languages"))
						fNeedLanguages = false;
					if (s.IndexOf("Variants") > 0 && s.IndexOf("{") > s.IndexOf("Variants"))
						fNeedVariants = false;

					outputStream.WriteLine(s);
				}
			}
			// Output the standard Countries, Names, and Variants sections if needed.
			if (fNeedScripts)
			{
				outputStream.WriteLine("    Scripts {");
				outputStream.WriteLine("    }");
			}
			if (fNeedCountries)
			{
				outputStream.WriteLine("    Countries {");
				outputStream.WriteLine("    }");
			}
			if (fNeedLanguages)
			{
				outputStream.WriteLine("    Languages {");
				if (m_ldData != null)	// can be null during testing and possibly running
				{
					foreach (StringWithWs sww in m_ldData.Names)
					{
						if (sww.icuLocale == newLocaleName)
						{
							outputStream.WriteLine("        " + sww.icuLocale + " {\"" + sww.text +
								"\"}");
							break;
						}
					}
				}
				outputStream.WriteLine("    }");
			}
			if (fNeedVariants)
			{
				outputStream.WriteLine("    Variants {");
				outputStream.WriteLine("    }");
			}

			// Output closing brace.
			outputStream.WriteLine("}");

			// Close the output file.
			outputStream.Close();
		}

		/// <summary>
		/// Write the collation file.
		/// </summary>
		/// <param name="newLocaleName"></param>
		/// <param name="outputFilespec"></param>
		public void WriteCollationFile(string baseLocale, string newLocaleName, string outputFilespec)
		{
			// TODO:::
			// If there is a base locale
			//   copy it to the newlocale name
			//
			// Overwrite the standard collation sequence with the contents from the XML file
			//
			string newLocaleFileName = m_collDirectory + newLocaleName + ".txt";
			if (baseLocale != null && File.Exists(m_collDirectory + baseLocale + ".txt"))
			{
				string baseFile = m_collDirectory + baseLocale + ".txt";
				newLocaleFileName = outputFilespec;

				// read in the collation information
				IcuDataNode node = ICUDataFiles.ParsedFile(baseFile);

				// create a nodespec to use
				NodeSpecification stdColl = new NodeSpecification(baseLocale);
				node.ChangeRootName(newLocaleName);
				ICUDataFiles.WriteFileAs(baseFile, outputFilespec);
			}
			else if (!File.Exists(newLocaleFileName))
			{
				CreateNewLocaleFile(newLocaleName, outputFilespec);
				return;
			}

			// make sure there is some collation information in the lang xml file
			string coll = m_ldData.CollationElements;
			if (coll != null && coll.Length > 0)
			{
				// read in the collation information
				IcuDataNode node = ICUDataFiles.ParsedFile(outputFilespec);

				if (node != null && node.Child("___") != null && node.Children.Count == 1)
				{
					NodeSpecification nodeSpec = new NodeSpecification(newLocaleName);
					ICUDataFiles.RemoveICUDataFileChild(node, nodeSpec, "___");
					ICUDataFiles.SetICUDataFileNode(outputFilespec, nodeSpec, new IcuDataNode("Version", "1.08", ""), true);

					ICUDataFiles.SetICUDataFileNode(outputFilespec, nodeSpec, new IcuDataNode("collations", node, "", ""), false);
					NodeSpecification nodeSpecColl = new NodeSpecification(newLocaleName, "collations");
					IcuDataNode nodeColl = node.Child("collations");
					ICUDataFiles.SetICUDataFileNode(outputFilespec, nodeSpecColl, new IcuDataNode("standard", nodeColl, "", ""), true);
				}
				// create a nodespec to use
				NodeSpecification stdColl = new NodeSpecification(newLocaleName, "collations", "standard");
				ICUDataFiles.SetICUDataFileNode(outputFilespec, stdColl, new IcuDataNode("Sequence", coll, ""), true);
				ICUDataFiles.SetICUDataFileNode(outputFilespec, stdColl, new IcuDataNode("Version", "1.08", ""), false);
				ICUDataFiles.WriteFile(outputFilespec);
			}
			else
			{
				// This code will make sure we replace any existing collation information with 'nothing'.
				StreamWriter outputStream = new StreamWriter(outputFilespec, false,
					System.Text.Encoding.UTF8);

				// Output locale name and opening brace.
				outputStream.WriteLine("// " + m_comment);
				outputStream.WriteLine(newLocaleName + "{");
				outputStream.WriteLine("    // [SIL-Corp] the following comment and section copied from ICU files");
				outputStream.WriteLine("    /**");
				outputStream.WriteLine("     * so genrb doesn't issue warnings");
				outputStream.WriteLine("     */");
				outputStream.WriteLine("    ___{\"\"}");
				// Output closing brace.
				outputStream.WriteLine("}");

				// Close the output file.
				outputStream.Close();
				return;
			}
		}

		private void CreateNewLocaleFile(string newLocaleName, string outputFilespec)
		{
			StreamWriter outputStream = new StreamWriter(outputFilespec, false,
				System.Text.Encoding.UTF8);

			// Output locale name and opening brace.
			outputStream.WriteLine("// " + m_comment);
			outputStream.WriteLine(newLocaleName + "{");
			outputStream.WriteLine("    Version{\"1.0\"}");
			outputStream.WriteLine("    collations{");
			outputStream.WriteLine("        standard{");
			outputStream.WriteLine("            Sequence{\"" + m_ldData.CollationElements + "\"}");
			outputStream.WriteLine("            Version{\"1.0\"}");
			outputStream.WriteLine("        }");
			outputStream.WriteLine("    }");
			// Output closing brace.
			outputStream.WriteLine("}");

			// Close the output file.
			outputStream.Close();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method can be called to parse the .txt files in the locale dir of icu.  It is
		/// usefull for testing the parser.  The current known problems (Feb 2004) are for
		/// a few data types in the root.txt file: Transiterator Display Names, ...
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool TestBaseLocaleParsers(bool leaveTmpFiles)
		{
			// for each locale.txt do the following
			DirectoryInfo dInfo = new DirectoryInfo(m_localeDirectory);
			FileInfo[] files = dInfo.GetFiles("*.txt");
			bool fSuccess = true;

			foreach( FileInfo fi in files)
			{
				int pass = 0;
				string baseLocale = fi.Name.Substring(0, fi.Name.IndexOf('.'));
				string testName = m_localeDirectory + baseLocale + ".tmp";
				try
				{
					if (fi.Name.StartsWith("root"))	// skip all root flavors of the files
						continue;

					if (fi.Name.IndexOf("ja") != -1)
					{
						int qwer = 2;
						qwer++;
					}

					// make sure empty to start with
					m_entries.Clear();
					m_keys.Clear();
					string baseName = "";
					string baseScript = "";
					string baseCountry = "";
					string baseVariant = "";

					ParseLocaleName(ref baseLocale, out baseName, out baseScript, out baseCountry,
						out baseVariant);

					string filename = m_localeDirectory;
					if (baseName.Length > 0)
					{
						pass = 1;
						filename += baseName;
						try
						{
							ReadAndHashLocaleFile(filename + ".txt");
						}
						catch (LDExceptions e)
						{
							if (e.ec != ErrorCodes.FileNotFound)
								throw;
						}
					}

					filename += "_";
					if (baseScript.Length > 0)
					{
						pass = 2;
						filename += baseScript;
						try
						{
							ReadAndHashLocaleFile(filename + ".txt");
						}
						catch (LDExceptions e)
						{
							if (e.ec != ErrorCodes.FileNotFound)
								throw;
						}
					}

					filename += "_";
					if (baseCountry.Length > 0)
					{
						pass = 3;
						filename += baseCountry;
						try
						{
							ReadAndHashLocaleFile(filename + ".txt");
						}
						catch (LDExceptions e)
						{
							if (e.ec != ErrorCodes.FileNotFound)
								throw;
						}
					}

					filename += "_";
					if (baseVariant.Length > 0)
					{
						pass = 4;
						filename += baseVariant;
						try
						{
							ReadAndHashLocaleFile(filename + ".txt");
						}
						catch (LDExceptions e)
						{
							if (e.ec != ErrorCodes.FileNotFound)
								throw;
						}
					}

					pass = 5;
					WriteLocaleFile(baseLocale, testName);
				}
				catch
				{
					LogFile.AddErrorLine("Exception: processing " + fi.Name+ " pass " + pass.ToString());
					fSuccess = false;
				}
			}

			if (leaveTmpFiles == false)
			{
				FileInfo[] tmpfiles = dInfo.GetFiles("*.tmp");
				foreach( FileInfo fi in tmpfiles)
				{
					string file = fi.FullName;
					//					Generic.DeleteFile(file);
					if (File.Exists(file))
					{
						File.SetAttributes(file, FileAttributes.Normal);
						File.Delete(file);
					}
				}
			}

			return fSuccess;
		}
		#endregion

		/// <summary>
		/// The individual resource files are now stored in a subdirectory instead of having
		/// a descriptive name prepended to their names.  Make it so.
		/// </summary>
		public void MoveNewResFilesToSubDir()
		{
			// Release any locked files first. One place that can cause this lock is when we do a
			// -o command and a file such as xpig.xml is missing a name. In LanguageDefinitionFactory:GetDefaultICUValues,
			// we call GetDisplayLanguage because we couldn't get a name from the WritingSystem.
			// This call locks icudt40l\root.res so the move would fail. This call should only affect
			// InstallLanguage -- not a FieldWorks app calling InstallLanguage, so it shouldn't bother
			// the calling app, and we are about to leave InstallLanguage at this point.
			// Note: if/when InstallLanguage becomes a dll, these uses of Icu.Cleanup will need to be
			// refactored to use the interface JohnT developed to do this safely without messing other
			// uses of ICU in the main program.
			Icu.Cleanup();
			for (int i = 0; i < m_UndoFileStack.Count; ++i)
			{
				UndoFiles frame = (UndoFiles)m_UndoFileStack[i];
				string sFile = frame.originalFile;
				Debug.Assert(sFile.Length >= 5);
				if (sFile.Substring(sFile.Length - 4) == ".res")
				{
					int idx = sFile.LastIndexOf("\\");
					Debug.Assert(idx > 0);
					string sGenFile = sFile.Substring(0, idx) + "_" +
						sFile.Substring(idx + 1);
					if (System.IO.File.Exists(sGenFile))
						Generic.FileCopyWithLogging(sGenFile, sFile, true);
					Generic.DeleteFile(sGenFile);
				}
			}
		}
	}
}

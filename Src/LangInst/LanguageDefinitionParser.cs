using System;
using System.IO;
using System.Xml;
using InstallLanguage.Errors;
using SIL.FieldWorks.Common;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks;
// For PUACharacter
using SIL.FieldWorks.Common.FwUtils;

namespace InstallLanguage
{
	/// <summary>
	/// Summary description for Parser.
	/// </summary>
	public class Parser
	{
		//		string m_inputFilename = "tl.xml";
		private string m_inputFilename = "";
		// Future members could include:
		//  WritingSystem (XML that will load WritingSystem/Collation)
		//  CollationElements
		//  PUADefinitions
		//  Fonts
		//  Keyboards
		//  EncodingConverters
		//
		private string baseLocale = "";
		private string newLocale = "";
		private string localeResources = "";
		private string localeName = "";
		private string localeScript = "";
		private string localeCountry = "";
		private string localeVariant = "";
		private string collationElements = "";
		private string localeWinLCID = "";
		private PUACharacter[] puaChars = null;
		private System.Collections.ArrayList m_rgNames = null;

		public Parser(string inputFile)
		{
			m_inputFilename = inputFile;
			//baseLocale = "en_gb";
			////baseLocale = "";
			////baseLocale = "en_gb_IPA_xxx";
			////baseLocale = "en__IPA";
			////ReadAndHashString("    // Default to English\r\n    LocaleID:int { 0x09 }\r\n");
			//localeResources = "zoneStrings {\r\n" +
			//	"    \"Europe/London\",\r\n" +
			//	"    \"Greenwich Mean Time\",\r\n" +
			//	"    \"GMT\",\r\n" +
			//	"    \"British Summer Time\",\r\n" +
			//	"    \"BST\",\r\n"  +
			//	"}\r\n";
			//
			//newLocale = "tl_PH_IPA";
			//localeName = "Tagalog";
			//localeScript = "Latin";
			//localeCountry = "Philippines";
			//localeVariant = "International Phonetic Alphabet";
			//collationElements = "&\u0304<<\u0301<<\u030C<<\u0300<<\u0308";
			//localeWinLCID = "0x777";  // should override default AND values in base locale files
			//m_inputFilename = inputFile;
		}

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		public void PopulateFromLanguageClass()
		{
			//Extracts the locale filename from a given path
			int icuName = m_inputFilename.LastIndexOf("\\");
			string icuPortion = m_inputFilename.Substring(icuName+1);

			//Appears this maps the XML file to a LanguageDefinition class
			/////////////////
			ILgWritingSystemFactory wsf = LgWritingSystemFactoryClass.Create();

			LanguageDefinitionFactory langDefFactory = new LanguageDefinitionFactory(wsf, icuPortion);

			LanguageDefinition langDef = langDefFactory.InitializeFromXml(wsf, icuPortion) as LanguageDefinition;
			if (langDef == null)
			{
				throw new Exception("Unable to read and parse the input XML file " + m_inputFilename);
			}
			/////////////////

			int i=0;
			int cpua = langDef.PuaDefinitionCount;
			// if we have PUA characters in the LD file make an array of PUACharacters.  But be careful
			// to handle invalid definitions gracefully.
			if (langDef.PuaDefinitions != null && cpua != 0)
			{
				puaChars = new PUACharacter[cpua];
				foreach (CharDef charDef in langDef.PuaDefinitions)
				{
					try
					{
						puaChars[i] = new PUACharacter(charDef);
						++i;
					}
					catch
					{
					}
				}
			}
			if (i < cpua)
			{
				if (i == 0)
				{
					puaChars = null;
				}
				else
				{
					PUACharacter[] puaGoodChars = new PUACharacter[i];
					for (int ic = 0; ic < i; ++ic)
						puaGoodChars[ic] = puaChars[ic];
					puaChars = puaGoodChars;
				}
				if (LogFile.IsLogging())
					LogFile.AddErrorLine("Warning, " + (cpua - i) + " out of " + cpua +
						" PUA character definitions are invalid.");
			}
			baseLocale = langDef.BaseLocale;
			newLocale = langDef.XmlWritingSystem.WritingSystem.IcuLocale;
			localeResources = langDef.LocaleResources;
			// Get the collation elements, whether from the CollationElements element directly,
			// or from the WritingSystem element.
			collationElements = langDef.CollationElements;
			if (collationElements == null)
			{
				IWritingSystem lws = langDef.WritingSystem;
				int ccoll = lws.CollationCount;
				if (ccoll > 0)
					collationElements = lws.get_Collation(0).IcuRules;
			}
			localeWinLCID = langDef.XmlWritingSystem.WritingSystem.Locale.ToString();

			// make sure the newlocale has the proper case for each property:
			// lang, country and variant
			InstallLanguage.LocaleParser lp = new LocaleParser(newLocale);
			newLocale = lp.Locale;

			// Make sure the display names [Name, Country & Variant] have Unicode characters
			// greater than 7F converted to the \uxxxx format where xxxx is the unicode
			// hex value of the character.
			localeName    = ConvertToUnicodeNotation(langDef.LocaleName);
			localeScript  = ConvertToUnicodeNotation(langDef.LocaleScript);
			localeCountry = ConvertToUnicodeNotation(langDef.LocaleCountry);
			localeVariant = ConvertToUnicodeNotation(langDef.LocaleVariant);

			// Save the multilingual names of the writing system, together with the
			// ICU locale for each name.
			NameMultiUnicode rgName = langDef.XmlWritingSystem.Name;
			int cws = rgName.Count;
			// If we don't have a name, use the IcuLocale rather than going without a name.
			// Otherwise it won't register as a language in en.txt/res.
			if (cws == 0)
			{
				StringWithWs sw = new StringWithWs(langDef.XmlWritingSystem.WritingSystem.IcuLocale, "en");
				rgName.Add(sw);
				cws = 1;
			}
			m_rgNames = new System.Collections.ArrayList(cws);
			for (int iws = 0; iws < cws; ++iws)
			{
				StringWithWs x = rgName[iws];
				m_rgNames.Add(x);
			}

			// TODO - dlh
			// Once collationElements are handled, something will have to be checked there
			// as the current implementation assumes that it's in the valid format.

			wsf.Shutdown();		// This is (always) needed to balance creating the factory.
		}

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		public void Populate()
		{
			PopulateFromLanguageClass();
		}

		/// <summary>
		/// Call the other version of the ConvertToUnicodeNotation and return the finial string
		/// </summary>
		/// <param name="inString">strint to check for unicode/XML chars</param>
		/// <returns>finial string</returns>
		public static string ConvertToUnicodeNotation(String inString)
		{
			if (inString == null)	// handle a null string input string
				return "";

			string outString;
			if (ConvertToUnicodeNotation(inString, out outString))
				return outString;
			return inString;		// no change to the input string
		}

		public static bool ConvertToUnicodeNotation(String inString, out String outString)
		{
			// [Französische Süd- und Antarktisgebiete] --> [Franz\u00F6sische S\u00FCd- und Antarktisgebiete]
			// This code was removed: -> [& N < n\u0303<<<N\u0303] --> [&amp; N &lt; n\u0303&lt;&lt;&lt;N\u0303]

			bool hasReplacement = false;
			outString = "";
			foreach (char c in inString)
			{
				if (c > 0x7f)
				{
					outString += "\\u";
					outString += System.Convert.ToUInt16(c).ToString("X4");
					hasReplacement = true;
				}
				else
				{
					switch(c)
					{
						case '\'':
						case '"':
							outString += "\\u";
							outString += System.Convert.ToUInt16(c).ToString("X4");
							hasReplacement = true;
							break;
						default: outString += c;
							hasReplacement = true;
							break;
					}
				}
			}
			return hasReplacement;
		}

		#region Attributes for the Parser class

		// regular expressions used in this class
		// BaseLocale: en_GB_EURO
		public string BaseLocale
		{
			get	{return baseLocale;}
		}

		public PUACharacter[] PuaDefinitions
		{
			get	{return puaChars;}
		}

		// NewLocale: XX_YY_ZZZ
		public string NewLocale
		{
			get	{return newLocale;}
		}

		public string LocaleResources
		{
			get	{return localeResources;}
		}

		public string LocaleName
		{
			get	{return localeName;}
		}

		public string LocaleScript
		{
			get { return localeScript; }
		}

		public string LocaleCountry
		{
			get	{return localeCountry;}
		}

		public string LocaleVariant
		{
			get	{return localeVariant;}
		}

		public string CollationElements
		{
			get	{return collationElements;}
		}

		public string LocaleWinLCID
		{
			get { return localeWinLCID; }
		}

		/*		public string LDFile()
				{
					get	{return ldFile;}
				}
		*/

		public System.Collections.ArrayList Names
		{
			get { return m_rgNames; }
		}
		#endregion

	} // End of Parser class
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.XPath;
using Microsoft.Win32;

using SIL.FieldWorks.Common.Utils;

namespace LocaleStrings
{
	class Program
	{
		/// <summary>
		/// These are the various ways that newlines may be represented in the input strings.
		/// </summary>
		static string[] s_rgsNewline = new string[] { "\r\n", "\n" };
		/// <summary>
		/// These are the values that must be quoted by a backslash in a quoted string.
		/// </summary>
		static char[] s_rgchQuoted = new char[] { '"', '\\' };
		/// <summary>
		/// Flag that a string in a resx file contains "\\t", "\\r", "\\n", or "\\\"".
		/// These shouldn't be quoted in an XML file!
		/// </summary>
		static bool s_fBadStringValue = false;
		/// <summary>
		/// This program either extracts localizable strings from the files in the FieldWorks
		/// source tree into a POT file, or stores a localized strings-xx.xml file based on a
		/// PO file, or creates a test localization PO file.
		/// </summary>
		/// <param name="args"></param>
		/// <summary>
		/// DistFiles directory used on developer machine, but missing on end-user machines.
		/// We want to be able to run this on end-user machines to build the strings-???.xml file.
		/// </summary>
		static string s_DistFiles = "";

		static void Main(string[] args)
		{
			try
			{
				int iOpt;
				CommandLineOptions.BoolParam bpExtract = new CommandLineOptions.BoolParam("x",
					"extract",
					"Extract localizable strings into a POT file.",
					false);
				CommandLineOptions.BoolParam bpExcludeFlex = new CommandLineOptions.BoolParam("",
					"no-flex",
					"Don't extract strings specific to Language Explorer",
					false);
				CommandLineOptions.BoolParam bpExcludeTE = new CommandLineOptions.BoolParam("",
					"no-te",
					"Don't extract strings specific to Translation Editor",
					false);
				CommandLineOptions.BoolParam bpStore = new CommandLineOptions.BoolParam("s",
					"store",
					"Store strings from the PO file into the Language Explorer strings-xx.xml file",
					false);
				CommandLineOptions.StringParam spRoot = new CommandLineOptions.StringParam("r",
					"root",
					"The root directory of the FieldWorks source tree (typically C:\\FW)",
					null);
				CommandLineOptions.StringParam spPOTFile = new CommandLineOptions.StringParam("p",
					"pot",
					"Name of the output POT file",
					"FieldWorks.pot");
				CommandLineOptions.StringParam spTest = new CommandLineOptions.StringParam("t",
					"test",
					"Generate a test PO file for the given locale",
					"en-FOO");
				CommandLineOptions.BoolParam bpForce = new CommandLineOptions.BoolParam("",
					"force",
					"Overwrite existing (test?) PO file",
					false);
				List<string> rgsDirs = new List<string>();
				rgsDirs.Add("Src");
				CommandLineOptions.StringListParam slpDirs = new CommandLineOptions.StringListParam("d",
					"dir",
					"Add a subdirectory to those searched under the root (default = Src)",
					rgsDirs);
				CommandLineOptions.StringParam spMerge = new CommandLineOptions.StringParam("m",
					"merge",
					"Merge PO files together, overwriting the first one.",
					"messages.xx.po");
				CommandLineOptions.BoolParam bpCheck = new CommandLineOptions.BoolParam("c",
					"check",
					"Check translated strings for matching/valid argument markers.",
					false);

				CommandLineOptions.Param[] rgParam = new CommandLineOptions.Param[] {
					bpExtract, bpExcludeFlex, bpExcludeTE, bpStore, spRoot, spPOTFile, spTest,
					bpForce, slpDirs, spMerge, bpCheck
				};
				bool fOk = CommandLineOptions.Parse(args, ref rgParam, out iOpt);
				int cOps = 0;
				if (bpExtract.Value || bpExcludeFlex.Value || bpExcludeTE.Value)
					++cOps;
				if (bpStore.Value)
					++cOps;
				if (spMerge.HasValue)
					++cOps;
				if (spTest.HasValue)
					++cOps;
				if (bpCheck.Value)
					++cOps;
				if (cOps != 1)
					fOk = false;
				if (!fOk)
				{
					Usage(null, rgParam);
					return;
				}

				string sRoot;
				if (spRoot.HasValue)
				{
					sRoot = spRoot.Value;
					string devPath = Path.Combine(sRoot, "distfiles");
					if (Directory.Exists(devPath))
						s_DistFiles = "distfiles/";
				}
				else
					sRoot = GetRootDir();

				if (bpExtract.Value || bpExcludeFlex.Value || bpExcludeTE.Value)
				{
					string sOutputFile = spPOTFile.Value;
					if (!spPOTFile.HasValue)
					{
						if (bpExcludeTE.Value && bpExcludeFlex.Value)
							sOutputFile = "Common.pot";
						else if (bpExcludeTE.Value)
							sOutputFile = "Flex.pot";
						else if (bpExcludeFlex.Value)
							sOutputFile = "TE.pot";
					}
					ExtractPOTFile(sRoot, rgsDirs, bpExcludeTE.Value, bpExcludeFlex.Value, sOutputFile);
				}
				else if (bpStore.Value)
				{
					if (iOpt >= args.Length)
						Usage(null, rgParam);
					else if (!File.Exists(args[iOpt]))
						Usage(String.Format("{0} does not exist!", args[iOpt]), rgParam);
					else
						StoreLocalizedStrings(args[iOpt], sRoot);
				}
				else if (spTest.HasValue)
				{
					string sLocale = spTest.Value;
					if (VerifyValidLocale(sLocale))
					{
						string sOutputFile = Path.Combine(sRoot,
							String.Format("Localizations\\messages.{0}.po", sLocale));
						if (File.Exists(sOutputFile) && !bpForce.Value)
						{
							Usage(String.Format("{0} already exists.  Use --force to overwrite",
								sOutputFile), rgParam);
						}
						else
						{
							GenerateTestPOFile(sOutputFile, sRoot, rgsDirs, bpExcludeTE.Value,
								bpExcludeFlex.Value, sLocale);
						}
					}
					else
					{
						Usage(String.Format("{0} is not a valid Windows XP locale", sLocale), rgParam);
					}
				}
				else if (spMerge.HasValue)
				{
					if (iOpt >= args.Length)
						Usage("Missing second merge file", rgParam);
					else if (!File.Exists(spMerge.Value))
						Usage(String.Format("Invalid first merge file {0}", spMerge.Value), rgParam);
					else if (!File.Exists(args[iOpt]))
						Usage(String.Format("Invalid second merge file {0}", args[iOpt]), rgParam);
					else
						MergePOFiles(spMerge.Value, args[iOpt]);
				}
				else if (bpCheck.Value)
				{
					if (iOpt >= args.Length)
						Usage(null, rgParam);
					else if (!File.Exists(args[iOpt]))
						Usage(String.Format("{0} does not exist!", args[iOpt]), rgParam);
					else
						CheckLocalizedStrings(args[iOpt]);
				}
				else
				{
					Usage("Invalid command", rgParam);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
		}

		/// <summary>
		/// Tersely explain how to use the program.
		/// </summary>
		/// <param name="sMsg">optional error message</param>
		private static void Usage(string sMsg, CommandLineOptions.Param[] rgParam)
		{
			if (!String.IsNullOrEmpty(sMsg))
			{
				Console.WriteLine(sMsg);
				Console.WriteLine("");
			}
			Console.WriteLine("Usage: LocaleStrings.exe --extract [options]");
			Console.WriteLine("    or");
			Console.WriteLine("       LocaleStrings.exe --store [options] messages.<xx>.po");
			Console.WriteLine("    or");
			Console.WriteLine("       LocaleStrings.exe --check [options] messages.<xx>.po");
			Console.WriteLine("    or");
			Console.WriteLine("       LocaleStrings.exe --test lg-co");
			Console.WriteLine("    or");
			Console.WriteLine("       LocaleStrings.exe [options] --merge <old>.<xx>.po <new>.<xx>.po");
			Console.WriteLine("");
			CommandLineOptions.Usage(rgParam);
			Console.WriteLine("");
			Console.WriteLine("The --extract command creates a file (default FieldWorks.pot in the current");
			Console.WriteLine("directory) by extracting localizable strings from the C# resource files in the");
			Console.WriteLine("FieldWorks source tree.  It also extracts localizable strings from the XML");
			Console.WriteLine("configuration files in DistFiles/Language Explorer.  Plans are to also extract strings");
			Console.WriteLine("from the C/C++ resource (.rc) files, although that may not be done very soon.");
			Console.WriteLine("");
			Console.WriteLine("The --store command creates a file named strings-xx.xml from the messages.xx.po");
			Console.WriteLine("file, copying the existing strings-en.xml file, substituting translated strings");
			Console.WriteLine("for existing ones, and adding translated strings for the various label and abbr");
			Console.WriteLine("attributes in the other XML configuration files.");
			Console.WriteLine("");
			Console.WriteLine("The --check command reads through the messages.xx.po file to check that all the");
			Console.WriteLine("translated messages have valid argument markers matching the original strings.");
			Console.WriteLine("Problems are written to the standard output, and thus may be redirected to a file.");
			Console.WriteLine("");
			Console.WriteLine("The --test command create a messages.lg-co.po file which can be used to test");
			Console.WriteLine("the internationalization of the FieldWorks programs.");
			Console.WriteLine("");
			Console.WriteLine("The --merge command merges the strings from <new>.<xx>.po into <old>.<xx>.po,");
			Console.WriteLine("overwriting any conflicts in the original (<old>.<xx>.po) file.");

			throw new Exception("Invalid command arguments.");
		}

		/// <summary>
		/// Extract all the localizable strings from the C# resource (.resx) files in the
		/// FieldWorks source tree, the XML configuration files in the FieldWorks distribution
		/// tree, and (eventually) the C/C++ resource (.rc) files in the FieldWorks source
		/// tree, creating a POT file for the FieldWorks suite.
		/// </summary>
		/// <param name="sRoot"></param>
		/// <param name="rgsDirs"></param>
		/// <param name="fExcludeTE">exclude strings from Translation Editor specific sources</param>
		/// <param name="fExcludeFlex">exclude strings from Language Explorer specific sources</param>
		/// <param name="sOutputFile"></param>
		private static void ExtractPOTFile(string sRoot, List<string> rgsDirs, bool fExcludeTE,
			bool fExcludeFlex, string sOutputFile)
		{
			List<POString> rgsPOStrings = ExtractLocalizableStrings(sRoot, rgsDirs, fExcludeTE,
				fExcludeFlex);
			StreamWriter swOut = null;
			try
			{
				swOut = new StreamWriter(sOutputFile, false, Encoding.UTF8);
				WritePoHeader(swOut, sRoot, String.Empty);
				for (int i = 0; i < rgsPOStrings.Count; ++i)
					rgsPOStrings[i].Write(swOut);
			}
			finally
			{
				if (swOut != null)
					swOut.Close();
			}
		}

		/// <summary>
		/// Extract all the localizable strings from the C# resource (.resx) files in the
		/// FieldWorks source tree, the XML configuration files in the FieldWorks distribution
		/// tree, and (eventually) the C/C++ resource (.rc) files in the FieldWorks source
		/// tree.
		/// </summary>
		/// <param name="sRoot"></param>
		/// <param name="rgsDirs"></param>
		/// <param name="fExcludeTE"></param>
		/// <param name="fExcludeFlex"></param>
		/// <returns>Sorted and merged list of localizable strings</returns>
		private static List<POString> ExtractLocalizableStrings(string sRoot, List<string> rgsDirs,
			bool fExcludeTE, bool fExcludeFlex)
		{
			List<POString> rgsPOStrings = new List<POString>(1000);
			ExtractFromResxFiles(sRoot, rgsDirs, fExcludeTE, fExcludeFlex, rgsPOStrings);
			if (!fExcludeFlex)
				ExtractFromXmlConfigFiles(sRoot, rgsPOStrings);
			ExtractFromRcFiles(sRoot, rgsDirs, rgsPOStrings);

			rgsPOStrings.Sort(POString.CompareMsgIds);
			POString.MergeDuplicateStrings(rgsPOStrings);
			return rgsPOStrings;
		}

		/// <summary>
		/// Returns a string like "FieldWorks 4.0.1"
		/// </summary>
		/// <param name="sRoot"></param>
		/// <returns></returns>
		private static string GetFieldWorksVersion(string sRoot)
		{
			string sMajor = "?";
			string sMinor = "?";
			string sRevision = "?";
			string sFile = Path.Combine(sRoot, "Bld/GlobalInclude.xml");
			XmlDocument xdoc = new XmlDocument();
			xdoc.Load(sFile);
			// I would prefer to use SelectSingleNode, it it doesn't seem to work!
			foreach (XmlNode xn in xdoc.DocumentElement.FirstChild.ChildNodes)
			{
				if (xn.Name == "property" && xn is XmlElement)
				{
					XmlElement xel = xn as XmlElement;
					string sName = xel.GetAttribute("name");
					switch (sName)
					{
						case "FWMAJOR":		sMajor = xel.GetAttribute("value");		break;
						case "FWMINOR":		sMinor = xel.GetAttribute("value");		break;
						case "FWREVISION":	sRevision = xel.GetAttribute("value");	break;
					}
				}
			}
			return String.Format("FieldWorks {0}.{1}.{2}", sMajor, sMinor, sRevision);
		}

		/// <summary>
		/// Extract localizable strings from the C# (.resx) files in the source tree.
		/// </summary>
		/// <param name="sRoot"></param>
		/// <param name="rgsDirs"></param>
		/// <param name="fExcludeTE"></param>
		/// <param name="fExcludeFlex"></param>
		/// <param name="rgsPOStrings"></param>
		private static void ExtractFromResxFiles(string sRoot, List<string> rgsDirs,
			bool fExcludeTE, bool fExcludeFlex, List<POString> rgsPOStrings)
		{
			for (int iDir = 0; iDir < rgsDirs.Count; ++iDir)
			{
				string sRootDir = Path.Combine(sRoot, rgsDirs[iDir]);
				string[] rgsLocResxFiles = FindAllFiles(sRootDir, "*.*.resx");
				List<string> lssLocResx = new List<string>(rgsLocResxFiles.Length);
				for (int i = 0; i < rgsLocResxFiles.Length; ++i)
					lssLocResx.Add(rgsLocResxFiles[i].ToLower());
				string[] rgsResxFiles = FindAllFiles(sRootDir, "*.resx");
				for (int i = 0; i < rgsResxFiles.Length; ++i)
				{
					string sFile = rgsResxFiles[i].ToLower();
					if (fExcludeFlex)
					{
						if (sFile.IndexOf("\\lextext\\") >= 0)
							continue;
					}
					if (fExcludeTE)
					{
						if (sFile.IndexOf("\\te\\") >= 0)
							continue;
						if (sFile.IndexOf("\\tedll\\") >= 0)
							continue;
						if (sFile.IndexOf("\\teexe\\") >= 0)
							continue;
						if (sFile.IndexOf("\\scrfdo\\") >= 0)
							continue;
						if (sFile.IndexOf("\\scrimportcomponents\\") >= 0)
							continue;
						if (sFile.IndexOf("\\scripture\\") >= 0)
							continue;
						if (sFile.IndexOf("\\fdo\\scr") >= 0)
							continue;
					}
					if (!lssLocResx.Contains(sFile))
						ProcessResxFile(rgsResxFiles[i], sRoot, rgsPOStrings);
				}
				if (s_fBadStringValue)
				{
					throw new Exception("FIX THE INVALID RESX STRINGS!");
				}
			}
		}

		/// <summary>
		/// Verify that the given locale string represents a valid MS Windows XP locale.
		/// </summary>
		/// <param name="sLoc">locale string</param>
		/// <returns>true of sLoc is valid, otherwise false</returns>
		private static bool VerifyValidLocale(string sLoc)
		{
			CultureInfo[] rgci = CultureInfo.GetCultures(CultureTypes.AllCultures);
			for (int i = 0; i < rgci.Length; ++i)
			{
				if (rgci[i].Name == sLoc)
					return true;
			}
			return false;
		}

		/// <summary>
		/// Load a single resource (.resx) file, possibly merging in string values from a
		/// localized version of the same, and write the strings found therein to the output
		/// (.po) file.
		/// </summary>
		/// <param name="sResxFile">full pathname of the resource file</param>
		/// <param name="sRoot"></param>
		/// <param name="rgsPOStrings"></param>
		private static void ProcessResxFile(string sResxFile, string sRoot, List<POString> rgsPOStrings)
		{
			//int cPOStrings = rgsPOStrings.Count;
			XmlDocument xdoc = new XmlDocument();
			xdoc.Load(sResxFile);
			string sIgnore = sRoot.Replace('\\', '/');
			if (sIgnore.EndsWith("/"))
				sIgnore = sIgnore.Substring(0, sIgnore.Length - 1);
			foreach (XmlNode x in xdoc.DocumentElement.ChildNodes)
			{
				XmlElement xel = x as XmlElement;
				if (xel != null && xel.Name == "data")
				{
					string sName = xel.GetAttribute("name");
					string sType = xel.GetAttribute("type");
					string sMimeType = xel.GetAttribute("mimetype");
					string sValue = null;
					string sComment = null;
					if (!String.IsNullOrEmpty(sName) &&
						String.IsNullOrEmpty(sType) && String.IsNullOrEmpty(sMimeType))
					{
						if (!sName.StartsWith(">>") && IsTextName(sName))
						{
							for (int i = 0; i < xel.ChildNodes.Count; ++i)
							{
								if (xel.ChildNodes[i].Name == "value")
									sValue = xel.ChildNodes[i].InnerText;
								if (xel.ChildNodes[i].Name == "comment")
									sComment = xel.ChildNodes[i].InnerText;
							}
						}
					}
					if (!String.IsNullOrEmpty(sValue))
					{
						if (sValue.IndexOfAny("/\\".ToCharArray()) >= 0 && sValue.Trim().EndsWith(".htm"))
							continue;
						if (sComment != null && sComment.Trim().ToLower() == "do not translate")
							continue;
						if (String.IsNullOrEmpty(sValue.Trim()))
							continue;
						if (sValue.IndexOf("\\n") >= 0 ||
							sValue.IndexOf("\\r") >= 0 ||
							sValue.IndexOf("\\t") >= 0 ||
							sValue.IndexOf("\\\\") >= 0 ||
							sValue.IndexOf("\\\"") >= 0)
						{
							s_fBadStringValue = true;
							Console.WriteLine(
								string.Format("Backslash quoted character found for {0} in {1}",
								sName, sResxFile));
						}
						string[] rgsComment = null;
						if (!String.IsNullOrEmpty(sComment))
							rgsComment = sComment.Split(s_rgsNewline, StringSplitOptions.None);
						sValue = FixStringForEmbeddedQuotes(sValue);
						string[] rgsId = sValue.Split(s_rgsNewline, StringSplitOptions.None);
						string[] rgsStr = new string[1] { "" };
						POString pos = new POString(rgsComment, rgsId, rgsStr);
						string sPath = String.Format("{0}::{1}", sResxFile.Replace('\\', '/'), sName);
						if (sPath.StartsWith(sIgnore))
							sPath = sPath.Substring(sIgnore.Length);
						pos.AddAutoComment(sPath);
						rgsPOStrings.Add(pos);
					}
				}
			}
			//cPOStrings = rgsPOStrings.Count - cPOStrings;
			//Console.WriteLine(
			//    String.Format("{0} added {1} strings (possibly duplicates) to the POT file.",
			//    sResxFile, cPOStrings));
		}

		private static bool IsTextName(string sName)
		{
			if (sName.EndsWith(".Name") ||
				sName.EndsWith(".AccessibleName") ||
				sName.EndsWith(".AccessibleDescription"))
			{
				return false;
			}
			else
			{
				return true;
			}
		}

		/// <summary>
		/// Write the header to the PO file.
		/// </summary>
		/// <param name="swOut">output FileStream</param>
		/// <param name="sRoot"></param>
		/// <param name="sLocale"></param>
		private static void WritePoHeader(StreamWriter swOut, string sRoot, string sLocale)
		{
			swOut.WriteLine("");	// StreamWriter writes a BOM for UTF-8: put it on a line by itself.
			string sTime = DateTime.Now.ToLocalTime().ToString("o");
			string s = String.Format("Created from FieldWorks sources", sTime);
			POString.WriteComment(s, ' ', swOut);
			s = String.Format("Copyright (c) {0} SIL International.", DateTime.Now.Year);
			POString.WriteComment(s, ' ', swOut);
			POString.WriteComment("This file is distributed under the same license as the FieldWorks programs.",
				' ', swOut);
			POString.WriteComment("", ' ', swOut);
			//POString.WriteComment("fuzzy", ',', swOut);	the on-line translation site doesn't like this.
			swOut.Write("msgid ");
			POString.WriteQuotedLine("", false, swOut);
			swOut.Write("msgstr ");
			POString.WriteQuotedLine("", false, swOut);
			string sVersion = GetFieldWorksVersion(sRoot);
			POString.WriteQuotedLine(String.Format("Project-Id-Version: {0}", sVersion), true, swOut);
			POString.WriteQuotedLine("Report-Msgid-Bugs-To: FlexErrors@sil.org", true, swOut);
			POString.WriteQuotedLine(String.Format("POT-Creation-Date: {0}", sTime), true, swOut);
			POString.WriteQuotedLine("PO-Revision-Date: ", true, swOut);
			POString.WriteQuotedLine("Last-Translator: Full Name <email@address>", true, swOut);
			POString.WriteQuotedLine("Language-Team: Language <email@address>", true, swOut);
			POString.WriteQuotedLine("MIME-Version: 1.0", true, swOut);
			POString.WriteQuotedLine("Content-Type: text/plain; charset=UTF-8", true, swOut);
			POString.WriteQuotedLine("Content-Transfer-Encoding: 8bit", true, swOut);
			if (String.IsNullOrEmpty(sLocale))
			{
				POString.WriteQuotedLine("X-Poedit-Language: ", true, swOut);
				POString.WriteQuotedLine("X-Poedit-Country: ", true, swOut);
			}
			else
			{
				string[] rgsLocale = sLocale.Split(new char[] { '-', '_' });
				CultureInfo ci = new CultureInfo(rgsLocale[0]);
				string sLanguage = ci.EnglishName;
				string sCountry = String.Empty;
				string sVariant = String.Empty;
				if (rgsLocale.Length > 1)
				{
					RegionInfo ri = new RegionInfo(rgsLocale[1]);
					sCountry = ri.EnglishName;
				}
				if (rgsLocale.Length > 2)
				{
					ci = new CultureInfo(sLocale);
					sVariant = ci.EnglishName;
					int idx = sVariant.IndexOf(sLanguage);
					if (idx >= 0)
						sVariant = sVariant.Remove(idx, sLanguage.Length);
					idx = sVariant.IndexOf(sCountry);
					if (idx >= 0)
						sVariant = sVariant.Remove(idx, sCountry.Length);
					sVariant = sVariant.Replace('(', ' ');
					sVariant = sVariant.Replace(')', ' ');
					sVariant = sVariant.Trim(new char[] { ',', ' ', '\t', '\n', '\r' });
				}
				POString.WriteQuotedLine(String.Format("X-Poedit-Language: {0}", sLanguage), true, swOut);
				POString.WriteQuotedLine(String.Format("X-Poedit-Country: {0}", sCountry), true, swOut);
				if (!String.IsNullOrEmpty(sVariant))
					POString.WriteQuotedLine(String.Format("X-Poedit-Variant: {0}", sVariant), true, swOut);
			}
			swOut.WriteLine("");
		}

		/// <summary>
		/// Add backslashes as needed to quote quotation marks and backslashes.
		/// </summary>
		/// <param name="sValue">string value which needs backslashes added</param>
		/// <returns>revised string (or possibly the original one)</returns>
		private static string FixStringForEmbeddedQuotes(string sValue)
		{
			// remove any current quoting of the quotation marks, which obviously isn't
			// needed in the XML based RESX files.
			int idx = sValue.IndexOf("\\\"");
			while (idx >= 0)
			{
				if (idx == 0 || sValue[idx - 1] != '\\')
					sValue = sValue.Remove(idx, 1);
				else
					++idx;
				if (idx < sValue.Length)
					idx = sValue.IndexOf("\\\"", idx);
				else
					break;
			}
			idx = sValue.IndexOfAny(s_rgchQuoted);
			while (idx >= 0)
			{
				sValue = sValue.Insert(idx, "\\");
				if (idx + 2 >= sValue.Length)
					break;
				idx = sValue.IndexOfAny(s_rgchQuoted, idx + 2);
			}
			return sValue;
		}

		/// <summary>
		/// Find the root directory of the FieldWorks source tree.
		/// </summary>
		/// <returns>path to the sources root (Src) in the FieldWorks source tree</returns>
		private static string GetRootDir()
		{
			string defaultDir = Path.Combine(Environment.ExpandEnvironmentVariables("%FWROOT%"),
				"DistFiles");
			object rootDir = null;
			RegistryKey sRegKey = Registry.LocalMachine.OpenSubKey("Software\\SIL\\FieldWorks");
			if (sRegKey != null)
				rootDir = sRegKey.GetValue("RootCodeDir", defaultDir);
			if ((rootDir == null) || !(rootDir is string))
			{
				throw new ApplicationException("Cannot obtain FieldWorks root directory");
			}
			string sRootDir = (string)rootDir;
			int idx = sRootDir.ToLower().LastIndexOf("distfiles");
			if (idx == sRootDir.Length - 9)
			{
				sRootDir = sRootDir.Substring(0, idx);
				s_DistFiles = "distfiles/";
			}
			return sRootDir;
		}


		/// <summary>
		/// Merges the PO files.
		/// </summary>
		/// <param name="sMainFile">The s main file.</param>
		/// <param name="sNewFile">The s new file.</param>
		private static void MergePOFiles(string sMainFile, string sNewFile)
		{
			string sLoc = GetLocaleFromMsgFile(sMainFile);
			string sLoc2 = GetLocaleFromMsgFile(sNewFile);
			if (sLoc != sLoc2)
			{
				Console.WriteLine("Mismatched locales in the two merge files: {0} vs {1}",
					sLoc, sLoc2);
				throw new Exception("MISMATCHED LOCALES IN MERGE FILES");
			}
			POString posMainHeader;
			Dictionary<string, POString> dictMain = LoadPOFile(sMainFile, out posMainHeader);
			POString posNewHeader;
			Dictionary<string, POString> dictNew = LoadPOFile(sNewFile, out posNewHeader);
			if (dictNew.Count == 0)
			{
				Console.WriteLine("No translations found in new PO file!");
				throw new Exception("VOID NEW PO FILE");
			}
			DateTime now = DateTime.Now;
			string sTimeStamp = String.Format("{0:D4}{1:D2}{2:D2}{3:D2}{4:D2}{5:D2}",
				now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second);
			CheckCompatiblePOFiles(posMainHeader, posNewHeader);
			MergePOHeaders(posMainHeader, posNewHeader);
			int cReplaced = 0;
			int cAdded = 0;
			int cSame = 0;
			int cMissing = 0;
			string sLogFile = String.Format("Merge-{0}.log", sTimeStamp);
			using (StreamWriter swLog = new StreamWriter(sLogFile))
			{
				swLog.WriteLine(String.Format("Merging {0} into {1} on {2}", sNewFile, sMainFile, now.ToString()));
				using (Dictionary<string, POString>.Enumerator it = dictNew.GetEnumerator())
				{
					while (it.MoveNext())
					{
						if (dictMain.ContainsKey(it.Current.Key))
						{
							if (dictMain[it.Current.Key].MsgStrAsString() != it.Current.Value.MsgStrAsString())
							{
								swLog.WriteLine("");
								swLog.WriteLine(String.Format("OLD VALUE"));
								dictMain[it.Current.Key].Write(swLog);
								swLog.WriteLine(String.Format("NEW VALUE"));
								it.Current.Value.Write(swLog);
								dictMain[it.Current.Key] = it.Current.Value;
								++cReplaced;
							}
							else
							{
								++cSame;
							}
						}
						else if (!String.IsNullOrEmpty(it.Current.Value.MsgStrAsString()))
						{
							dictMain.Add(it.Current.Key, it.Current.Value);
							++cAdded;
						}
					}
				}
				cMissing = dictMain.Count - (cReplaced + cSame + cAdded);
				swLog.WriteLine("");
				swLog.WriteLine(String.Format("Merge: {0} replaced, {1} added, {2} same, {3} not in new",
					cReplaced, cAdded, cSame, cMissing));
				swLog.Close();
			}
			// Save the original main file, and write out a new main file.
			File.Copy(sMainFile, String.Format("{0}-{1}", sMainFile, sTimeStamp));
			using (StreamWriter swOut = new StreamWriter(sMainFile))
			{
				posMainHeader.Write(swOut);
				using (Dictionary<string, POString>.Enumerator it = dictMain.GetEnumerator())
				{
					while (it.MoveNext())
					{
						it.Current.Value.Write(swOut);
					}
				}
				swOut.Close();
			}
			Console.WriteLine("Merge: {0} replaced, {1} added, {2} same, {3} not in new",
				cReplaced, cAdded, cSame, cMissing);
		}

		/// <summary>
		/// Check the following lines in the PO file header for compatibility:
		///		MIME-Version:
		///		Content-Type:
		///		Content-Transfer-Encoding:
		///		X-Poedit-Language:
		///		X-Poedit-Country:
		/// </summary>
		/// <param name="posMainHeader"></param>
		/// <param name="posNewHeader"></param>
		private static void CheckCompatiblePOFiles(POString posMainHeader, POString posNewHeader)
		{
			CheckCompatiblePOHeader(posMainHeader, posNewHeader, "MIME Version", IsMIMEVersion);
			CheckCompatiblePOHeader(posMainHeader, posNewHeader, "Content Type", IsContentType);
			CheckCompatiblePOHeader(posMainHeader, posNewHeader, "Encoding", IsContentTransferEncoding);
			CheckCompatiblePOHeader(posMainHeader, posNewHeader, "Language", IsXPoeditLanguage);
			CheckCompatiblePOHeader(posMainHeader, posNewHeader, "Country", IsXPoeditCountry);
		}

		private static void CheckCompatiblePOHeader(POString posMainHeader, POString posNewHeader,
			string sHeader, Predicate<string> matchFn)
		{
			string sPo1 = posMainHeader.MsgStr.Find(matchFn);
			string sPo2 = posNewHeader.MsgStr.Find(matchFn);
			if (sPo1 == null || sPo2 == null || sPo1.ToLower() == sPo2.ToLower())
				return;
			int idx = sPo1.IndexOf(":");
			string s1 = sPo1.Substring(idx + 1).Trim();
			string s2 = sPo2.Substring(idx + 1).Trim();
			if (String.IsNullOrEmpty(s1) || String.IsNullOrEmpty(s2))
				return;
			if (s1.ToLower() == s2.ToLower())
				return;
			Console.WriteLine("WARNING: first file's {0} = \"{1}\", but second file's {0} = \"{2}\"",
				sHeader, s1, s2);
		}

		private static bool IsMIMEVersion(string s)
		{
			return s != null && s.ToLower().StartsWith("mime-version:");
		}
		private static bool IsContentType(string s)
		{
			return s != null && s.ToLower().StartsWith("content-type:");
		}
		private static bool IsContentTransferEncoding(string s)
		{
			return s != null && s.ToLower().StartsWith("content-transfer-encoding:");
		}
		private static bool IsXPoeditLanguage(string s)
		{
			return s != null && s.ToLower().StartsWith("x-poedit-language:");
		}
		private static bool IsXPoeditCountry(string s)
		{
			return s != null && s.ToLower().StartsWith("x-poedit-country:");
		}

		/// <summary>
		/// Merge information in the following header lines:
		///		Project-Id-Version:
		///		POT-Creation-Date:
		///		PO-Revision-Date:
		///		Last-Translator:
		///		Language-Team:
		/// </summary>
		/// <param name="posMainHeader"></param>
		/// <param name="posNewHeader"></param>
		private static void MergePOHeaders(POString posMainHeader, POString posNewHeader)
		{
			MergePOHeaderLines(posMainHeader, posNewHeader, "Project-Id-Version:", IsProjectIdVersion);
			MergePOHeaderLines(posMainHeader, posNewHeader, "POT-Creation-Date:", IsPOTCreationDate);
			MergePOHeaderLines(posMainHeader, posNewHeader, "PO-Revision-Date:", IsPORevisionDate);
			MergePOHeaderLines(posMainHeader, posNewHeader, "Last-Translator:", IsLastTranslator);
			MergePOHeaderLines(posMainHeader, posNewHeader, "Language-Team:", IsLanguageTeam);
		}

		private static void MergePOHeaderLines(POString posMainHeader, POString posNewHeader,
			string sHeaderTag, Predicate<string> matchFn)
		{
			string sPo2 = posNewHeader.MsgStr.Find(matchFn);
			if (sPo2 == null)
				return;		// no information in new file
			string s2 = sPo2.Substring(sHeaderTag.Length).Trim();
			if (String.IsNullOrEmpty(s2))
				return;		// no information in new file
			int idx = posMainHeader.MsgStr.FindIndex(matchFn);
			if (idx < 0)
			{
				posMainHeader.MsgStr.Add(sPo2);	// no information in old file: add new info at end.
				return;
			}
			string s1 = posMainHeader.MsgStr[idx].Substring(sHeaderTag.Length).Trim();
			if (s1.ToLower() == s2.ToLower())
				return;		// same information as before
			if (String.IsNullOrEmpty(s1))
			{
				// empty information in the old file: store information from new file
				posMainHeader.MsgStr[idx] = String.Format("{0} {1}\n", sHeaderTag, s2);
				return;
			}
			int ich = s1.ToLower().IndexOf(s2.ToLower());
			if (ich < 0)
			{
				// add the new info to the end of the old info
				posMainHeader.MsgStr[idx] = String.Format("{0} {1}; {2}\n", sHeaderTag, s1, s2);
			}
		}

		private static bool IsProjectIdVersion(string s)
		{
			return s != null && s.ToLower().StartsWith("project-id-version:");
		}
		private static bool IsPOTCreationDate(string s)
		{
			return s != null && s.ToLower().StartsWith("pot-creation-date:");
		}
		private static bool IsPORevisionDate(string s)
		{
			return s != null && s.ToLower().StartsWith("po-revision-date:");
		}
		private static bool IsLastTranslator(string s)
		{
			return s != null && s.ToLower().StartsWith("last-translator:");
		}
		private static bool IsLanguageTeam(string s)
		{
			return s != null && s.ToLower().StartsWith("language-team:");
		}

		/// <summary>
		/// Execute the --store operation.
		/// </summary>
		/// <param name="sMsgFile"></param>
		/// <param name="sRoot"></param>
		private static void StoreLocalizedStrings(string sMsgFile, string sRoot)
		{
			string sLoc = GetLocaleFromMsgFile(sMsgFile);
			string sEngFile = Path.Combine(sRoot,
				s_DistFiles + "Language Explorer/Configuration/strings-en.xml");
			string sNewFile = Path.Combine(sRoot,
				s_DistFiles + "Language Explorer/Configuration/strings-" + sLoc.Replace('-', '_') + ".xml");
			File.Delete(sNewFile);
			POString posHeader;
			Dictionary<string, POString> dictTrans = LoadPOFile(sMsgFile, out posHeader);
			if (dictTrans.Count == 0)
			{
				Console.WriteLine("No translations found in PO file!");
				throw new Exception("VOID PO FILE");
			}
			XmlDocument xdoc = new XmlDocument();
			xdoc.Load(sEngFile);
			TranslateStringsElements(xdoc.DocumentElement, dictTrans);
			StoreTranslatedAttributes(xdoc.DocumentElement, dictTrans);
			StoreTranslatedLiterals(xdoc.DocumentElement, dictTrans);
			StoreTranslatedContextHelp(xdoc.DocumentElement, dictTrans);
			xdoc.Save(sNewFile);
		}

		private static void CheckLocalizedStrings(string sMsgFile)
		{
			POString posHeader;
			Dictionary<string, POString> dictTrans = LoadPOFile(sMsgFile, out posHeader);
			if (dictTrans.Count == 0)
			{
				Console.WriteLine("No translations found in PO file!");
				throw new Exception("VOID PO FILE");
			}
			foreach (string sKey in dictTrans.Keys)
			{
				string sId = dictTrans[sKey].MsgIdAsString();
				string sMsg = dictTrans[sKey].MsgStrAsString();
				if (sId == sMsg || String.IsNullOrEmpty(sMsg))
					continue;
				List<string> rgsArgsOrig = FindAllArgumentMarkers(sId);
				List<string> rgsArgsTrans = FindAllArgumentMarkers(sMsg);
				bool fOk = true;
				if (rgsArgsOrig.Count != rgsArgsTrans.Count)
					fOk = false;
				for (int i = 0; fOk && i < rgsArgsOrig.Count; ++i)
				{
					fOk = rgsArgsOrig[i] == rgsArgsTrans[i];
				}
				if (!fOk)
				{
					Console.WriteLine("The translation for");
					Console.WriteLine("    \"{0}\"", sId);
					Console.WriteLine("        does not have the proper set of matching argument markers.");
					Console.WriteLine("    \"{0}\"", sMsg);
				}
			}
		}

		// {index[,alignment][:formatString]}
		static Regex s_regexArgMarker = new Regex("{[0-9]+(,[-+ 0-9]+)?(:[ 0-9a-zA-Z]+)?}",
			RegexOptions.Compiled|RegexOptions.CultureInvariant|RegexOptions.ExplicitCapture);

		private static List<string> FindAllArgumentMarkers(string sMsg)
		{
			MatchCollection matches = s_regexArgMarker.Matches(sMsg);
			List<string> rgsArgs = new List<string>(matches.Count);
			foreach (Match match in matches)
			{
				string sArg = sMsg.Substring(match.Index, match.Length);
				if (!rgsArgs.Contains(sArg))
					rgsArgs.Add(sArg);
			}
			rgsArgs.Sort();
			return rgsArgs;
		}

		private static Dictionary<string, POString> LoadPOFile(string sMsgFile, out POString posHeader)
		{
			using (StreamReader srIn = new StreamReader(sMsgFile, Encoding.UTF8))
			{
				Dictionary<string, POString> dictTrans = new Dictionary<string, POString>();
				posHeader = POString.ReadFromFile(srIn);
				POString pos = POString.ReadFromFile(srIn);
				while (pos != null)
				{
					if (!pos.HasEmptyMsgStr)
						dictTrans.Add(pos.MsgIdAsString(), pos);
					pos = POString.ReadFromFile(srIn);
				}
				srIn.Close();
				return dictTrans;
			}
		}

		/// <summary>
		/// This nicely recursive method replaces the English txt attribute values with the
		/// corresponding translated values if they exist.
		/// </summary>
		/// <param name="xel"></param>
		/// <param name="dictTrans"></param>
		private static void TranslateStringsElements(XmlElement xel,
			Dictionary<string, POString> dictTrans)
		{
			if (xel.Name == "string")
			{
				POString pos = null;
				string sEnglish = xel.GetAttribute("txt");
				if (dictTrans.TryGetValue(sEnglish, out pos))
				{
					string sTranslation = pos.MsgStrAsString();
					xel.SetAttribute("txt", sTranslation);
					xel.SetAttribute("English", sEnglish);
				}
			}
			foreach (XmlNode xn in xel.ChildNodes)
			{
				if (xn is XmlElement)
					TranslateStringsElements(xn as XmlElement, dictTrans);
			}
		}

		private static void StoreTranslatedAttributes(XmlElement xelRoot,
			Dictionary<string, POString> dictTrans)
		{
			XmlElement xelGroup = xelRoot.OwnerDocument.CreateElement("group");
			xelGroup.SetAttribute("id", "LocalizedAttributes");
			Dictionary<string, POString>.Enumerator en = dictTrans.GetEnumerator();
			while (en.MoveNext())
			{
				POString pos = en.Current.Value;
				string sValue = pos.MsgStrAsString();
				if (String.IsNullOrEmpty(sValue))
					continue;
				List<string> rgs = pos.AutoComments;
				if (rgs == null)
					continue;
				for (int i = 0; i < rgs.Count; ++i)
				{
					if (rgs[i] != null &&
						// handle bug in creating original POT file due to case sensitive search.
						(rgs[i].StartsWith("/") || rgs[i].StartsWith("file:///")) &&
						IsFromXmlAttribute(rgs[i]))
					{
						XmlElement xelString = xelRoot.OwnerDocument.CreateElement("string");
						xelString.SetAttribute("id", pos.MsgIdAsString());
						xelString.SetAttribute("txt", sValue);
						xelGroup.AppendChild(xelString);
						break;
					}
				}
			}
			xelRoot.AppendChild(xelGroup);
		}

		private static bool IsFromXmlAttribute(string sComment)
		{
			int idx = sComment.LastIndexOf("/");
			if (idx < 0 || sComment.Length == idx + 1)
				return false;
			if (sComment[idx + 1] != '@')
				return false;
			else
				return sComment.Length > idx + 2;
		}

		private static void StoreTranslatedLiterals(XmlElement xelRoot,
			Dictionary<string, POString> dictTrans)
		{
			XmlElement xelGroup = xelRoot.OwnerDocument.CreateElement("group");
			xelGroup.SetAttribute("id", "LocalizedLiterals");
			Dictionary<string, POString>.Enumerator en = dictTrans.GetEnumerator();
			while (en.MoveNext())
			{
				POString pos = en.Current.Value;
				string sValue = pos.MsgStrAsString();
				if (String.IsNullOrEmpty(sValue))
					continue;
				List<string> rgs = pos.AutoComments;
				if (rgs == null)
					continue;
				for (int i = 0; i < rgs.Count; ++i)
				{
					if (rgs[i] != null && rgs[i].StartsWith("/") && rgs[i].EndsWith("/lit"))
					{
						XmlElement xelString = xelRoot.OwnerDocument.CreateElement("string");
						xelString.SetAttribute("id", pos.MsgIdAsString());
						xelString.SetAttribute("txt", sValue);
						xelGroup.AppendChild(xelString);
						break;
					}
				}
			}
			xelRoot.AppendChild(xelGroup);
		}

		private static void StoreTranslatedContextHelp(XmlElement xelRoot,
			Dictionary<string, POString> dictTrans)
		{
			XmlElement xelGroup = xelRoot.OwnerDocument.CreateElement("group");
			xelGroup.SetAttribute("id", "LocalizedContextHelp");
			Dictionary<string, POString>.Enumerator en = dictTrans.GetEnumerator();
			while (en.MoveNext())
			{
				POString pos = en.Current.Value;
				string sValue = pos.MsgStrAsString();
				if (String.IsNullOrEmpty(sValue))
					continue;
				List<string> rgs = pos.AutoComments;
				if (rgs == null)
					continue;
				for (int i = 0; i < rgs.Count; ++i)
				{
					string sId = FindContextHelpId(rgs[i]);
					if (!String.IsNullOrEmpty(sId))
					{
						XmlElement xelString = xelRoot.OwnerDocument.CreateElement("string");
						xelString.SetAttribute("id", sId);
						xelString.SetAttribute("txt", sValue);
						xelGroup.AppendChild(xelString);
						break;
					}
				}
			}
			xelRoot.AppendChild(xelGroup);
		}

		private static string FindContextHelpId(string sComment)
		{
			const string ksContextMarker = "/ContextHelp.xml::/strings/item[@id=\"";
			if (sComment != null &&
				sComment.StartsWith("/"))
			{
				int idx = sComment.IndexOf(ksContextMarker);
				if (idx > 0)
				{
					string sId = sComment.Substring(idx + ksContextMarker.Length);
					int idxEnd = sId.IndexOf('"');
					if (idxEnd > 0)
						return sId.Remove(idxEnd);
				}
			}
			return null;
		}

		/// <summary>
		/// Derive the desired locale from the given message filename.
		/// </summary>
		/// <param name="sMsgFile"></param>
		/// <param name="srIn"></param>
		/// <returns></returns>
		private static string GetLocaleFromMsgFile(string sMsgFile)
		{
			// Try to obtain the locale from the filename.
			string sLoc1 = null;
			int idx = sMsgFile.LastIndexOfAny("\\/".ToCharArray());
			if (idx < 0)
				idx = 0;
			int idx1 = sMsgFile.IndexOf('.', idx);
			int idx2 = sMsgFile.LastIndexOf('.');
			if (idx1 != -1 && idx2 > idx1)
			{
				++idx1;
				int cch = idx2 - idx1;
				sLoc1 = sMsgFile.Substring(idx1, cch);
			}
			if (String.IsNullOrEmpty(sLoc1))
			{
				Console.WriteLine("ERROR: cannot determine locale from filename!");
				throw new Exception("CANNOT DETERMINE LOCALE");
			}
			return sLoc1;
		}

		/// <summary>
		/// Extract localizable strings from the XML configuration files in the distribution
		/// tree.
		/// </summary>
		/// <param name="sRoot"></param>
		/// <param name="rgsPOStrings"></param>
		private static void ExtractFromXmlConfigFiles(string sRoot, List<POString> rgsPOStrings)
		{
			// Get the list of configuration files to process.
			string sRootDir = Path.Combine(sRoot, s_DistFiles + "Language Explorer/Configuration");
			string[] rgsLocConfigFiles = FindAllFiles(sRootDir, "strings-*.xml");
			List<string> lssLocConfigFiles = new List<string>(rgsLocConfigFiles.Length);
			for (int i = 0; i < rgsLocConfigFiles.Length; ++i)
				lssLocConfigFiles.Add(rgsLocConfigFiles[i].ToLower());
			string[] rgsConfigFiles = FindAllFiles(sRootDir, "*.xml");
			List<string> lssConfigFiles = new List<string>(
				rgsConfigFiles.Length + 4 - lssLocConfigFiles.Count);
			for (int i = 0; i < rgsConfigFiles.Length; ++i)
			{
				if (!lssLocConfigFiles.Contains(rgsConfigFiles[i].ToLower()))
					lssConfigFiles.Add(rgsConfigFiles[i]);
			}
			sRootDir = Path.Combine(sRoot, s_DistFiles + "Parts");
			// Let's not include the auto-generated parts and layouts for now.
			//rgsConfigFiles = FindAllFiles(sRootDir, "*.xml");
			rgsConfigFiles = FindAllFiles(sRootDir, "Standard*.xml");
			for (int i = 0; i < rgsConfigFiles.Length; ++i)
				lssConfigFiles.Add(rgsConfigFiles[i]);
			// Process the configuration files.
			for (int i = 0; i < lssConfigFiles.Count; ++i)
				ProcessXmlConfigFile(lssConfigFiles[i], rgsPOStrings);
			ProcessXmlStringsFile(sRoot, rgsPOStrings);

		}

		/// <summary>
		/// Process one XML configuration file to extract localizable strings.
		/// </summary>
		/// <param name="sConfigFile"></param>
		/// <param name="rgsPOStrings"></param>
		private static void ProcessXmlConfigFile(string sConfigFile, List<POString> rgsPOStrings)
		{
			XmlDocument xdoc = new XmlDocument();
			xdoc.Load(sConfigFile);
			ProcessConfigElement(xdoc.DocumentElement, rgsPOStrings);
		}

		/// <summary>
		/// Process one XML element to extract localizable strings.
		/// </summary>
		/// <param name="xel"></param>
		/// <param name="rgsPOStrings"></param>
		private static void ProcessConfigElement(XmlElement xel, List<POString> rgsPOStrings)
		{
			if (xel.Name == "lit" && !String.IsNullOrEmpty(xel.InnerText.Trim()))
				StoreLiteralString(xel, rgsPOStrings);

			string sLabel = xel.GetAttribute("label");
			if (!String.IsNullOrEmpty(sLabel.Trim()) && sLabel.Trim() != "$label")
				StoreAttributeString(xel, "label", sLabel, rgsPOStrings);
			string sAbbr = xel.GetAttribute("abbr");
			if (!String.IsNullOrEmpty(sAbbr.Trim()))
				StoreAttributeString(xel, "abbr", sAbbr, rgsPOStrings);
			string sTitle = xel.GetAttribute("title");
			if (!String.IsNullOrEmpty(sTitle.Trim()))
				StoreAttributeString(xel, "title", sTitle, rgsPOStrings);
			sLabel = xel.GetAttribute("formlabel");
			if (!String.IsNullOrEmpty(sLabel.Trim()))
				StoreAttributeString(xel, "formlabel", sLabel, rgsPOStrings);
			sLabel = xel.GetAttribute("okbuttonlabel");
			if (!String.IsNullOrEmpty(sLabel.Trim()))
				StoreAttributeString(xel, "okbuttonlabel", sLabel, rgsPOStrings);
			sLabel = xel.GetAttribute("headerlabel");
			if (!String.IsNullOrEmpty(sLabel.Trim()))
				StoreAttributeString(xel, "headerlabel", sLabel, rgsPOStrings);
			string sAfter = xel.GetAttribute("after");
			if (!String.IsNullOrEmpty(sAfter.Trim()))
				StoreAttributeString(xel, "after", sAfter, rgsPOStrings);
			string sBefore = xel.GetAttribute("before");
			if (!String.IsNullOrEmpty(sBefore.Trim()))
				StoreAttributeString(xel, "before", sBefore, rgsPOStrings);
			string sTooltip = xel.GetAttribute("tooltip");
			if (!String.IsNullOrEmpty(sTooltip.Trim()))
				StoreAttributeString(xel, "tooltip", sTooltip, rgsPOStrings);

			string sEditor = xel.GetAttribute("editor");
			string sMessage = xel.GetAttribute("message");
			if (sEditor.Trim().ToLower() == "lit" && !String.IsNullOrEmpty(sMessage.Trim()))
				StoreAttributeString(xel, "message", sMessage, rgsPOStrings);

			if (xel.Name == "item" &&
				!String.IsNullOrEmpty(xel.InnerText.Trim()) &&
				xel.ParentNode.Name == "strings")
			{
				string sId = xel.GetAttribute("id");
				if (!String.IsNullOrEmpty(sId))
				{
					StoreLiteralString(xel, rgsPOStrings);
					string sCaption = xel.GetAttribute("caption");
					if (!String.IsNullOrEmpty(sCaption))
						StoreAttributeString(xel, "caption", sCaption, rgsPOStrings);
					sCaption = xel.GetAttribute("captionformat");
					if (!String.IsNullOrEmpty(sCaption))
						StoreAttributeString(xel, "captionformat", sCaption, rgsPOStrings);
				}
			}

			foreach (XmlNode xn in xel.ChildNodes)
			{
				if (xn is XmlElement)
					ProcessConfigElement(xn as XmlElement, rgsPOStrings);
			}
		}

		/// <summary>
		/// Store an attribute value with a comment giving its xpath location.
		/// </summary>
		/// <param name="xel"></param>
		/// <param name="sName"></param>
		/// <param name="sValue"></param>
		/// <param name="rgsPOStrings"></param>
		private static void StoreAttributeString(XmlElement xel, string sName, string sValue,
			List<POString> rgsPOStrings)
		{
			string sTranslate = xel.GetAttribute("translate");
			if (sTranslate.Trim().ToLower() == "do not translate")
				return;
			string sPath = ComputePathComment(xel, sName);
			string[] rgsComment = null;
			if (String.IsNullOrEmpty(sTranslate))
				rgsComment = new string[1] { sPath };
			else
				rgsComment = new string[2] { sPath, FixStringForEmbeddedQuotes(sTranslate) };
			POString pos = new POString(rgsComment,
				new string[1] { FixStringForEmbeddedQuotes(sValue) },
				new string[1] { "" });
			rgsPOStrings.Add(pos);
		}

		/// <summary>
		/// Compute an XPath like comment to store in the POT file.
		/// </summary>
		/// <param name="xel"></param>
		/// <param name="sName"></param>
		/// <returns></returns>
		private static string ComputePathComment(XmlElement xel, string sName)
		{
			StringBuilder bldr = new StringBuilder(sName);
			if (sName != null)
				bldr.Insert(0, "/@");
			while (xel != null)
			{
				if (xel.Name == "part" || xel.Name == "item")
				{
					string s = xel.GetAttribute("id");
					if (!String.IsNullOrEmpty(s))
					{
						bldr.Insert(0, String.Format("[@id=\"{0}\"]", s));
					}
					else
					{
						s = xel.GetAttribute("ref");
						if (!String.IsNullOrEmpty(s))
							bldr.Insert(0, String.Format("[@ref=\"{0}\"]", s));
					}
				}
				else if (xel.Name == "layout")
				{
					string s1 = xel.GetAttribute("class");
					string s2 = xel.GetAttribute("type");
					string s3 = xel.GetAttribute("name");
					bldr.Insert(0, String.Format("[\"{0}-{1}-{2}\"]", s1, s2, s3));
				}
				bldr.Insert(0, xel.Name);
				bldr.Insert(0, "/");
				XmlDocument xdoc = xel.ParentNode as XmlDocument;
				if (xdoc != null)
				{
					bldr.Insert(0, "::");
					string s = xdoc.BaseURI;
					int idx = s.ToLower().IndexOf("/language explorer/");
					if (idx >= 0)
					{
						s = s.Substring(idx);
					}
					else
					{
						idx = s.ToLower().IndexOf("/parts/");
						if (idx >= 0)
							s = s.Substring(idx);
					}
					bldr.Insert(0, s);
				}
				xel = xel.ParentNode as XmlElement;
			}
			return bldr.ToString();
		}

		/// <summary>
		/// Store the string for a &lt;lit&gt; element.
		/// </summary>
		/// <param name="xel"></param>
		/// <param name="rgsPOStrings"></param>
		private static void StoreLiteralString(XmlElement xel, List<POString> rgsPOStrings)
		{
			string sTranslate = xel.GetAttribute("translate");
			if (sTranslate.Trim().ToLower() == "do not translate")
				return;
			string sPath = ComputePathComment(xel, null);
			string[] rgsComment = null;
			if (String.IsNullOrEmpty(sTranslate))
				rgsComment = new string[1] { sPath };
			else
				rgsComment = new string[2] { sPath, FixStringForEmbeddedQuotes(sTranslate) };
			string sVal = FixStringForEmbeddedQuotes(xel.InnerText);
			string[] rgsValue = sVal.Split(s_rgsNewline, StringSplitOptions.None);
			POString pos = new POString(rgsComment, rgsValue, new string[1] { "" });
			rgsPOStrings.Add(pos);
		}

		/// <summary>
		/// Process the strings-en.xml file.
		/// </summary>
		/// <param name="sRoot"></param>
		/// <param name="rgsPOStrings"></param>
		private static void ProcessXmlStringsFile(string sRoot, List<POString> rgsPOStrings)
		{
			string sFile = Path.Combine(sRoot, s_DistFiles + "Language Explorer/Configuration/strings-en.xml");
			XmlDocument xdoc = new XmlDocument();
			xdoc.Load(sFile);
			ProcessStringsElement(xdoc.DocumentElement, rgsPOStrings);
		}

		/// <summary>
		/// Process one element from the strings-en.xml file.
		/// </summary>
		/// <param name="xmlElement"></param>
		/// <param name="rgsPOStrings"></param>
		private static void ProcessStringsElement(XmlElement xel, List<POString> rgsPOStrings)
		{
			if (xel.Name == "string")
			{
				string sTxt = FixStringForEmbeddedQuotes(xel.GetAttribute("txt"));
				if (!String.IsNullOrEmpty(sTxt.Trim()))
				{
					string sComment = xel.GetAttribute("translate");
					if (sComment.Trim().ToLower() != "do not translate")
					{
						string sPath = ComputeStringPathComment(xel);
						string[] rgsComments = null;
						if (!String.IsNullOrEmpty(sComment.Trim()))
							rgsComments = new string[2] { sComment, sPath };
						else
							rgsComments = new string[1] { sPath };
						POString pos = new POString(rgsComments,
							new string[1] { sTxt }, new string[1] { "" });
						rgsPOStrings.Add(pos);
					}
				}
			}
			foreach (XmlNode xn in xel.ChildNodes)
			{
				if (xn is XmlElement)
					ProcessStringsElement(xn as XmlElement, rgsPOStrings);
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="xel"></param>
		/// <returns></returns>
		private static string ComputeStringPathComment(XmlElement xel)
		{
			StringBuilder bldr = new StringBuilder("|");
			while (xel != null)
			{
				if (xel.Name == "string" || xel.Name == "group")
					bldr.Insert(0, String.Format("/{0}", xel.GetAttribute("id")));
				xel = xel.ParentNode as XmlElement;
			}
			bldr.Insert(0, "/|strings-en.xml::");
			return bldr.ToString();
		}

		/// <summary>
		/// Find all files in the given directory tree that match the given filename pattern.
		/// </summary>
		/// <param name="sRootDir"></param>
		/// <param name="sPattern"></param>
		/// <returns></returns>
		private static string[] FindAllFiles(string sRootDir, string sPattern)
		{
			return Directory.GetFiles(sRootDir, sPattern, SearchOption.AllDirectories);
		}

		/// <summary>
		/// Extract localizable strings from the C/C++ (.rc) files in the source tree.
		/// </summary>
		/// <param name="sRoot"></param>
		/// <param name="rgsDirs"></param>
		/// <param name="rgsPOStrings"></param>
		private static void ExtractFromRcFiles(string sRoot, List<string> rgsDirs,
			List<POString> rgsPOStrings)
		{
			// The method or operation is not yet implemented.
		}

		/// <summary>
		/// Create a test "localized" PO file.
		/// </summary>
		/// <param name="sOutputFile"></param>
		/// <param name="sRoot"></param>
		/// <param name="rgsDirs"></param>
		/// <param name="fExcludeTE"></param>
		/// <param name="fExcludeFlex"></param>
		/// <param name="sLocale"></param>
		private static void GenerateTestPOFile(string sOutputFile, string sRoot,
			List<string> rgsDirs, bool fExcludeTE, bool fExcludeFlex, string sLocale)
		{
			List<POString> rgPOStrings = ExtractLocalizableStrings(sRoot, rgsDirs, fExcludeTE,
				fExcludeFlex);
			StreamWriter swOut = null;
			try
			{
				swOut = new StreamWriter(sOutputFile, false, Encoding.UTF8);
				WritePoHeader(swOut, sRoot, sLocale);
				for (int i = 0; i < rgPOStrings.Count; ++i)
				{
					List<string> rgsId = rgPOStrings[i].MsgId;
					List<string> rgsStr = new List<string>(rgsId.Count);
					for (int j = 0; j < rgsId.Count; ++j)
						rgsStr.Add(MungeForTest(rgsId[j], sLocale));
					rgPOStrings[i].MsgStr = rgsStr;
					rgPOStrings[i].Write(swOut);
				}
			}
			finally
			{
				if (swOut != null)
					swOut.Close();
			}

		}

		/// <summary>
		/// Add and change characters in the input string.
		/// </summary>
		/// <param name="sInput"></param>
		/// <param name="sLocale"></param>
		/// <returns></returns>
		private static string MungeForTest(string sInput, string sLocale)
		{
			if (sInput == "en")
				return sLocale;
			// First, convert string to all uppercase.
			string s1 = sInput.ToUpper();
			if (s1.EndsWith(".CHM"))
				return s1;	// don't munge helpfile pathnames beyond uppercasing.
			StringBuilder bldr = new StringBuilder(s1);
			//// Second, double all vowels.
			//for (int i = bldr.Length - 1; i >= 0; --i)
			//{
			//    if ("AEIOU".IndexOf(bldr[i]) >= 0)
			//        bldr.Insert(i, bldr[i]);
			//}
			//if (bldr.ToString().EndsWith("Y"))	// trailing y is a vowel.
			//    bldr.Append("Y");
			// Third, add an @ to the beginning and ending of the string.
			for (int idx = 0; idx < bldr.Length; ++idx)
			{
				if (!Char.IsWhiteSpace(bldr[idx]))
				{
					bldr.Insert(idx, '@');
					break;
				}
			}
			for (int idx = bldr.Length - 1; idx >= 0; --idx)
			{
				if (!Char.IsWhiteSpace(bldr[idx]))
				{
					bldr.Insert(idx+1, '@');
					break;
				}
			}
			return bldr.ToString();
		}
	}
}

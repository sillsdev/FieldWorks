// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace SIL.FieldWorks.Build.Tasks.Localization
{
	/// <summary>
	/// Extract localizable strings from XML configuration files in the FieldWorks source tree into a POT file.
	/// This class was adapted from the LocaleStrings project in https://github.com/sillsdev/fwsupporttools
	/// </summary>
	public class XmlToPo : Task
	{
		/// <summary>
		/// These are the various ways that newlines may be represented in the input strings.
		/// </summary>
		private static readonly string[] NewlineChars = { "\r\n", "\n" };
		/// <summary>
		/// These are the values that must be escaped by a backslash in a quoted string.
		/// </summary>
		private static readonly char[] EscapableChars = { '"', '\\' };

		/// <summary>The root directory of the FW source tree</summary>
		[Required]
		public string FwRoot { get; set; }

		/// <summary>The output POT file</summary>
		[Required]
		public string Output { get; set; }

		/// <summary>
		/// Extract all the localizable strings from the XML configuration files in the FieldWorks distribution
		/// tree, creating a POT file for the FieldWorks suite.
		/// </summary>
		public override bool Execute()
		{
			var distFiles = Path.Combine(FwRoot, "DistFiles");
			Log.LogMessage($"Extracting strings from XML files under '{distFiles}' to the POT file '{Output}'.");
			var poStrings = ExtractLocalizableStrings(distFiles);
			using (var swOut = new StreamWriter(Output, false, Encoding.UTF8))
			{
				WritePotFile(swOut, FwRoot, poStrings);
			}
			return true;
		}

		/// <summary>
		/// Write a POT file to the given stream from the list of POString objects.
		/// </summary>
		internal static void WritePotFile(TextWriter swOut, string fwRoot, List<POString> poStrings)
		{
			WritePoHeader(swOut, fwRoot, string.Empty);
			foreach (var poString in poStrings.Where(poString => poString.Flags == null || !poString.Flags.Contains("fuzzy")))
			{
				poString.Write(swOut);
			}
		}

		/// <summary>
		/// Extract all the localizable strings from the XML configuration files in the FieldWorks distribution tree.
		/// </summary>
		/// <returns>Sorted and merged list of localizable strings</returns>
		private static List<POString> ExtractLocalizableStrings(string distFilesDir)
		{
			var poStrings = new List<POString>(1000);
			ExtractFromXmlConfigFiles(distFilesDir, poStrings);

			poStrings.Sort(POString.CompareMsgIds);
			POString.MergeDuplicateStrings(poStrings);
			return poStrings;
		}

		/// <summary>
		/// Returns a string like "FieldWorks 4.0.1"
		/// </summary>
		private static string GetFieldWorksVersion(string fwRoot)
		{
			var sMajor = "?";
			var sMinor = "?";
			var sRevision = "?";
			var sFile = Path.Combine(fwRoot, "Build/GlobalInclude.properties");
			if (fwRoot == "/home/testing/fw" && !File.Exists(sFile))
				return "FieldWorks 1.2.3";
			var xdoc = XDocument.Load(sFile);
			// ReSharper disable once PossibleNullReferenceException
			foreach (var elt in xdoc.Root.Elements())
			{
				switch (elt.Name.LocalName)
				{
					case "FWMAJOR": sMajor = elt.Value; break;
					case "FWMINOR": sMinor = elt.Value; break;
					case "FWREVISION": sRevision = elt.Value; break;
				}
			}
			return $"FieldWorks {sMajor}.{sMinor}.{sRevision}";
		}

		private static void WritePoHeader(TextWriter writer, string fwRoot, string locale)
		{
			writer.WriteLine("");   // StreamWriter writes a BOM for UTF-8: put it on a line by itself.
			var sTime = DateTime.Now.ToLocalTime().ToString("o");
			const string fromFwSources = "Created from FieldWorks sources";
			POString.WriteComment(fromFwSources, ' ', writer);
			var copyrightSIL = $"Copyright (c) {DateTime.Now.Year} SIL International";
			POString.WriteComment(copyrightSIL, ' ', writer);
			POString.WriteComment("This software is licensed under the LGPL, version 2.1 or later",
				' ', writer);
			POString.WriteComment("(http://www.gnu.org/licenses/lgpl-2.1.html)", ' ', writer);
			POString.WriteComment("", ' ', writer);
			//POString.WriteComment("fuzzy", ',', swOut);	the on-line translation site doesn't like this.
			writer.Write("msgid ");
			POString.WriteQuotedLine("", writer);
			writer.Write("msgstr ");
			POString.WriteQuotedLine("", writer);
			var sVersion = GetFieldWorksVersion(fwRoot);
			POString.WriteQuotedLine($@"Project-Id-Version: {sVersion}\n", writer);
			POString.WriteQuotedLine(@"Report-Msgid-Bugs-To: FlexErrors@sil.org\n", writer);
			POString.WriteQuotedLine($@"POT-Creation-Date: {sTime}\n", writer);
			POString.WriteQuotedLine(@"PO-Revision-Date: \n", writer);
			POString.WriteQuotedLine(@"Last-Translator: Full Name <email@address>\n", writer);
			POString.WriteQuotedLine(@"Language-Team: Language <email@address>\n", writer);
			POString.WriteQuotedLine(@"MIME-Version: 1.0\n", writer);
			POString.WriteQuotedLine(@"Content-Type: text/plain; charset=UTF-8\n", writer);
			POString.WriteQuotedLine(@"Content-Transfer-Encoding: 8bit\n", writer);
			if (string.IsNullOrEmpty(locale))
			{
				POString.WriteQuotedLine(@"X-Poedit-Language: \n", writer);
				POString.WriteQuotedLine(@"X-Poedit-Country: \n", writer);
			}
			else
			{
				var localeParts = locale.Split('-', '_');
				var ci = new CultureInfo(localeParts[0]);
				var englishName = ci.EnglishName;
				var country = string.Empty;
				var variant = string.Empty;
				if (localeParts.Length > 1)
				{
					var ri = new RegionInfo(localeParts[1]);
					country = ri.EnglishName;
				}
				if (localeParts.Length > 2)
				{
					ci = new CultureInfo(locale);
					variant = ci.EnglishName;
					var idx = variant.IndexOf(englishName, StringComparison.Ordinal);
					if (idx >= 0)
						variant = variant.Remove(idx, englishName.Length);
					idx = variant.IndexOf(country, StringComparison.Ordinal);
					if (idx >= 0)
						variant = variant.Remove(idx, country.Length);
					variant = variant.Replace('(', ' ');
					variant = variant.Replace(')', ' ');
					variant = variant.Trim(',', ' ', '\t', '\n', '\r');
				}
				POString.WriteQuotedLine($@"X-Poedit-Language: {englishName}\n", writer);
				POString.WriteQuotedLine($@"X-Poedit-Country: {country}\n", writer);
				if (!string.IsNullOrEmpty(variant))
					POString.WriteQuotedLine($@"X-Poedit-Variant: {variant}\n", writer);
			}
			writer.WriteLine("");
		}

		/// <summary>
		/// Extract localizable strings from the XML configuration files in the distribution
		/// tree.
		/// </summary>
		private static void ExtractFromXmlConfigFiles(string distFilesDir, List<POString> poStrings)
		{
			// Get the list of configuration files to process.
			var configDir = Path.Combine(distFilesDir, "Language Explorer/Configuration");
			var stringsXmlFiles = FindAllFiles(configDir, "strings-*.xml").ToList();
			var allConfigFiles = FindAllFiles(configDir, "*.xml").Where(f => !stringsXmlFiles.Contains(f)).ToList();
			allConfigFiles.AddRange(FindAllFiles(configDir, "*.fwlayout"));
			var partsDir = Path.Combine(distFilesDir, "Parts");
			// Standard*.xml excludes the auto-generated parts and layouts.
			allConfigFiles.AddRange(FindAllFiles(partsDir, "Standard*.xml"));

			// Process the configuration files.
			foreach (var configFile in allConfigFiles)
			{
				ProcessXmlConfigFile(configFile, distFilesDir, poStrings);
			}

			var dictConfigDir = Path.Combine(distFilesDir, "Language Explorer/DefaultConfigurations");
			if (Directory.Exists(dictConfigDir))
			{
				foreach (var dictConfigFile in FindAllFiles(dictConfigDir, "*.fwdictconfig"))
					ProcessFwDictConfigFile(dictConfigFile, distFilesDir, poStrings);
			}
		}

		/// <summary>
		/// Find all files in the given directory and its subdirectories that match the given filename pattern.
		/// </summary>
		private static string[] FindAllFiles(string rootDir, string pattern)
		{
			return Directory.GetFiles(rootDir, pattern, SearchOption.AllDirectories);
		}

		/// <summary>
		/// Process one XML configuration file to extract localizable strings.
		/// </summary>
		private static void ProcessXmlConfigFile(string configFile, string distFilesDir, List<POString> poStrings)
		{
			var xdoc = XDocument.Load(configFile);
			var autoCommentFilePath = ComputeAutoCommentFilePath(distFilesDir, configFile);
			ProcessConfigElement(xdoc.Root, autoCommentFilePath, poStrings);
		}

		/// <summary>
		/// Process one XML element to extract localizable strings.
		/// </summary>
		internal static void ProcessConfigElement(XElement xel, string autoCommentFilePath, List<POString> poStrings)
		{
			if (xel.Name == "lit" && !string.IsNullOrEmpty(xel.Value.Trim()))
				StoreLiteralString(xel, poStrings, autoCommentFilePath);

			var sLabel = xel.Attribute("label")?.Value;
			if (!string.IsNullOrEmpty(sLabel?.Trim()) && sLabel.Trim() != "$label")
				StoreAttributeString(xel, "label", sLabel, poStrings, autoCommentFilePath);
			var sAbbr = xel.Attribute("abbr")?.Value;
			if (!string.IsNullOrEmpty(sAbbr?.Trim()))
				StoreAttributeString(xel, "abbr", sAbbr, poStrings, autoCommentFilePath);
			var sTitle = xel.Attribute("title")?.Value;
			if (!string.IsNullOrEmpty(sTitle?.Trim()))
				StoreAttributeString(xel, "title", sTitle, poStrings, autoCommentFilePath);
			var formLabel = xel.Attribute("formlabel")?.Value;
			if (!string.IsNullOrEmpty(formLabel?.Trim()))
				StoreAttributeString(xel, "formlabel", formLabel, poStrings, autoCommentFilePath);
			var okLabel = xel.Attribute("okbuttonlabel")?.Value;
			if (!string.IsNullOrEmpty(okLabel?.Trim()))
				StoreAttributeString(xel, "okbuttonlabel", okLabel, poStrings, autoCommentFilePath);
			var headerLabel = xel.Attribute("headerlabel")?.Value;
			if (!string.IsNullOrEmpty(headerLabel?.Trim()))
				StoreAttributeString(xel, "headerlabel", headerLabel, poStrings, autoCommentFilePath);
			var ghostLabel = xel.Attribute("ghostLabel")?.Value;
			if (!string.IsNullOrEmpty(ghostLabel?.Trim()))
				StoreAttributeString(xel, "ghostLabel", ghostLabel, poStrings, autoCommentFilePath);
			var sAfter = xel.Attribute("after")?.Value;
			if (!string.IsNullOrEmpty(sAfter?.Trim()))
				StoreAttributeString(xel, "after", sAfter, poStrings, autoCommentFilePath);
			var sBefore = xel.Attribute("before")?.Value;
			if (!string.IsNullOrEmpty(sBefore?.Trim()))
				StoreAttributeString(xel, "before", sBefore, poStrings, autoCommentFilePath);
			var sTooltip = xel.Attribute("tooltip")?.Value;
			if (!string.IsNullOrEmpty(sTooltip?.Trim()))
				StoreAttributeString(xel, "tooltip", sTooltip, poStrings, autoCommentFilePath);

			var sEditor = xel.Attribute("editor")?.Value;
			var sMessage = xel.Attribute("message")?.Value;
			if (sEditor?.Trim().ToLower() == "lit" && !string.IsNullOrEmpty(sMessage?.Trim()))
				StoreAttributeString(xel, "message", sMessage, poStrings, autoCommentFilePath);

			if (xel.Name == "item" &&
				!string.IsNullOrEmpty(xel.Value.Trim()) &&
				xel.Parent?.Name == "strings")
			{
				var sId = xel.Attribute("id")?.Value;
				if (!string.IsNullOrEmpty(sId))
				{
					StoreLiteralString(xel, poStrings, autoCommentFilePath);
					var sCaption = xel.Attribute("caption")?.Value;
					if (!string.IsNullOrEmpty(sCaption))
						StoreAttributeString(xel, "caption", sCaption, poStrings, autoCommentFilePath);
					sCaption = xel.Attribute("captionformat")?.Value;
					if (!string.IsNullOrEmpty(sCaption))
						StoreAttributeString(xel, "captionformat", sCaption, poStrings, autoCommentFilePath);
				}
			}

			foreach (var elt in xel.Elements())
			{
				ProcessConfigElement(elt, autoCommentFilePath, poStrings);
			}
		}

		internal static string ComputeAutoCommentFilePath(string basePath, string filePath)
		{
			var ignore = basePath.Replace('\\', '/');
			if (ignore.EndsWith("/"))
				ignore = ignore.Remove(ignore.Length - 1);
			var path = filePath.Replace('\\', '/');
			if (path.StartsWith(ignore))
				path = path.Substring(ignore.Length);
			return path;
		}

		/// <summary>
		/// Add backslashes as needed to escape quotation marks and backslashes for po and pot files.
		/// </summary>
		private static string EscapeStringForPo(string unescaped)
		{
			// remove any current escaping of the quotation marks, which obviously isn't needed in the XML-based files.
			var idx = unescaped.IndexOf("\\\"", StringComparison.Ordinal);
			while (idx >= 0)
			{
				// make sure we aren't unescaping a '\' (REVIEW (Hasso) 2020.02: not sure why we're unescaping quotes but not backslashes)
				if (idx == 0 || unescaped[idx - 1] != '\\')
					unescaped = unescaped.Remove(idx, 1);
				else
					++idx;
				if (idx < unescaped.Length)
					idx = unescaped.IndexOf("\\\"", idx, StringComparison.Ordinal);
				else
					break;
			}
			idx = unescaped.IndexOfAny(EscapableChars);
			while (idx >= 0)
			{
				unescaped = unescaped.Insert(idx, "\\");
				if (idx + 2 >= unescaped.Length)
					break;
				idx = unescaped.IndexOfAny(EscapableChars, idx + 2);
			}
			return unescaped;
		}

		/// <summary>
		/// Store an attribute value with a comment giving its xpath location.
		/// </summary>
		private static void StoreAttributeString(XElement xel, string name, string value,
			List<POString> poStrings, string autoCommentFilePath)
		{
			var sTranslate = xel.Attribute("translate")?.Value;
			if (sTranslate?.Trim().ToLower() == "do not translate")
				return;
			var sPath = ComputePathComment(xel, name, autoCommentFilePath);
			var comments = string.IsNullOrEmpty(sTranslate)
				? new[] {sPath}
				: new[] {sPath, EscapeStringForPo(sTranslate)};
			var pos = new POString(comments, new[] {EscapeStringForPo(value)});
			poStrings.Add(pos);
		}

		/// <summary>
		/// Compute an XPath like comment to store in the POT file.
		/// </summary>
		internal static string ComputePathComment(XElement xel, string attributeName, string autoCommentFilePath)
		{
			var bldr = new StringBuilder(attributeName);
			if (attributeName != null)
				bldr.Insert(0, "/@");
			while (xel != null)
			{
				if (xel.Name == "part" || xel.Name == "item")
				{
					var s = xel.Attribute("id")?.Value;
					if (!string.IsNullOrEmpty(s))
					{
						bldr.Insert(0, $"[@id=\"{s}\"]");
					}
					else
					{
						s = xel.Attribute("ref")?.Value;
						if (!string.IsNullOrEmpty(s))
							bldr.Insert(0, $"[@ref=\"{s}\"]");
					}
				}
				else if (xel.Name == "layout")
				{
					var s1 = xel.Attribute("class")?.Value;
					var s2 = xel.Attribute("type")?.Value;
					var s3 = xel.Attribute("name")?.Value;
					bldr.Insert(0, $"[\"{s1}-{s2}-{s3}\"]");
				}
				bldr.Insert(0, xel.Name);
				bldr.Insert(0, "/");
				xel = xel.Parent;
			}
			bldr.Insert(0, "::");
			bldr.Insert(0, autoCommentFilePath);
			return bldr.ToString();
		}

		/// <summary>
		/// Store the string for a &lt;lit&gt; element.
		/// </summary>
		private static void StoreLiteralString(XElement xel, List<POString> poStrings, string autoCommentFilePath)
		{
			var sTranslate = xel.Attribute("translate")?.Value;
			if (sTranslate?.Trim().ToLower() == "do not translate")
				return;
			var sPath = ComputePathComment(xel, null, autoCommentFilePath);
			var comments = string.IsNullOrEmpty(sTranslate)
				? new[] {sPath}
				: new[] {sPath, EscapeStringForPo(sTranslate)};
			var value = EscapeStringForPo(xel.Value);
			var valueLines = value.Split(NewlineChars, StringSplitOptions.None);
			var pos = new POString(comments, valueLines);
			poStrings.Add(pos);
		}

		/// <summary>
		/// Process a FieldWorks .fwdictconfig file.
		/// </summary>
		private static void ProcessFwDictConfigFile(string configFile, string distFilesDir, List<POString> poStrings)
		{
			var xdoc = XDocument.Load(configFile);
			var filePathForComment = ComputeAutoCommentFilePath(distFilesDir, configFile);
			ProcessFwDictConfigElement(xdoc.Root, filePathForComment, poStrings);
		}

		/// <summary>
		/// Process one element from a FieldWorks .fwdictconfig file.  Recursively process any children.
		/// </summary>
		internal static void ProcessFwDictConfigElement(XElement xel, string filePathForComment, List<POString> poStrings)
		{
			if (!"SharedItems".Equals(xel.Parent?.Name.LocalName))
			{
				StoreFwDictAttributeString(xel, "name", filePathForComment, poStrings);
				StoreFwDictAttributeString(xel, "nameSuffix", filePathForComment, poStrings);
				StoreFwDictAttributeString(xel, "after", filePathForComment, poStrings);
				StoreFwDictAttributeString(xel, "before", filePathForComment, poStrings);
				StoreFwDictAttributeString(xel, "between", filePathForComment, poStrings);
			}
			foreach (var elt in xel.Elements())
			{
				ProcessFwDictConfigElement(elt, filePathForComment, poStrings);
			}
		}

		/// <summary>
		/// Store one attribute value from a FieldWorks .fwdictconfig file as a POString, adding it to the list.
		/// </summary>
		private static void StoreFwDictAttributeString(XElement xel, string name, string filePathForComment, List<POString> poStrings)
		{
			var value = xel.Attribute(name)?.Value;
			if (!string.IsNullOrEmpty(value?.Trim()))
				poStrings.Add(new POString(
					new[] {ComputeFwDictConfigPathComment(xel, filePathForComment, name)},
					new[] {EscapeStringForPo(value)}));
		}

		/// <summary>
		/// Compute a reduced path comment for one attribute from a FieldWorks .fwdictconfig file.
		/// </summary>
		private static string ComputeFwDictConfigPathComment(XElement xel, string filePathForComment, string attName)
		{
			var commentBuilder = new StringBuilder(filePathForComment);
			commentBuilder.Append("::/");
			var parentElt = xel.Parent;
			if (parentElt != null && attName == "name")
			{
				commentBuilder.Append($"/{parentElt.Name.LocalName}[@name='{parentElt.Attribute("name")?.Value}']");
			}
			commentBuilder.Append("/").Append(xel.Name.LocalName);
			if (attName != "name")
			{
				commentBuilder.Append($"[@name='{xel.Attribute("name")?.Value}']");
			}
			commentBuilder.Append("/@").Append(attName);
			return commentBuilder.ToString();
		}

	}
}

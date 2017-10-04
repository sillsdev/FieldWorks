// Copyright (c) 2015-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using FwBuildTasks;
using Microsoft.Build.Framework;

namespace SIL.FieldWorks.Build.Tasks.Localization
{
	/// <summary>
	/// This class exists to perform the work on one PO file.
	/// The main reason for having it is so that we can have member variables like CurrentFile,
	/// and so that data for one parallel task is quite separate from others.
	/// </summary>
	public class Localizer
	{
		private LocalizerOptions Options { get; set; }

		public readonly List<string> Errors = new List<string>();

		private string CurrentFile { get; set; }

		internal string Locale { get; set; }

		internal string Version { get; set; }
		internal string FileVersion { get; set; }
		internal string InformationVersion { get; set; }

		public void Initialize(string currentFile, LocalizerOptions options)
		{
			Options = options;
			CurrentFile = currentFile;
			var currentFileName = Path.GetFileName(currentFile);
			Locale = currentFileName.Substring(LocalizeFieldWorks.PoFileLeadIn.Length,
				currentFileName.Length - LocalizeFieldWorks.PoFileLeadIn.Length - LocalizeFieldWorks.PoFileExtension.Length);
		}

		internal void LogError(string message)
		{
			lock (Errors)
				Errors.Add(message);
		}

		public void ProcessFile()
		{
			try
			{
				if (Options.BuildSource)
				{
					if (!CheckForPoFileProblems())
						return;

					CreateStringsXml();

					CreateXmlMappingFromPo();
				}

				List<string> projectFolders;
				if (!GetProjectFolders(out projectFolders))
					return;

				using (var reader = new StreamReader(Options.AssemblyInfoPath, Encoding.UTF8))
				{
					while (!reader.EndOfStream)
					{
						var line = reader.ReadLine();
						if (line == null)
							continue;
						if (line.StartsWith("[assembly: AssemblyFileVersion"))
							FileVersion = ExtractVersion(line);
						else if (line.StartsWith("[assembly: AssemblyInformationalVersionAttribute"))
							InformationVersion = ExtractVersion(line);
						else if (line.StartsWith("[assembly: AssemblyVersion"))
							Version = ExtractVersion(line);
					}
					reader.Close();
				}
				if (string.IsNullOrEmpty(FileVersion))
					FileVersion = "0.0.0.0";
				if (string.IsNullOrEmpty(InformationVersion))
					InformationVersion = FileVersion;
				if (string.IsNullOrEmpty(Version))
					Version = FileVersion;

				//Parallel.ForEach(projectFolders, currentFolder =>
				foreach (var currentFolder in projectFolders)
				{
					var projectLocalizer = CreateProjectLocalizer(currentFolder,
						new ProjectLocalizerOptions(this, Options));
					projectLocalizer.ProcessProject();
				}
				//);
			}
			catch (Exception ex)
			{
				LogError($"Caught exception processing {Locale}: {ex.Message}");
			}
		}

		protected virtual ProjectLocalizer CreateProjectLocalizer(string folder, ProjectLocalizerOptions options)
		{
			return new ProjectLocalizer(folder, options);
		}

		private static string ExtractVersion(string line)
		{
			var start = line.IndexOf("\"", StringComparison.Ordinal);
			var end = line.LastIndexOf("\"", StringComparison.Ordinal);
			return line.Substring(start + 1, end - start - 1);
		}

		internal bool GetProjectFolders(out List<string> projectFolders)
		{
			var root = Options.SrcFolder;
			projectFolders = new List<string>();
			return CollectInterestingProjects(root, projectFolders);
		}

		/// <summary>
		/// Collect interesting projects...returning false if we find a bad one (with two projects).
		/// </summary>
		/// <param name="root"></param>
		/// <param name="projectFolderCollector"></param>
		/// <returns><c>true</c> if the <paramref name="root"/> directory and it's subdirectories
		/// contain exactly 0 or 1 .csproj files per directory, <c>false</c> if there are two or
		/// more projects.</returns>
		private bool CollectInterestingProjects(string root, List<string> projectFolderCollector)
		{
			if (root.EndsWith("Tests"))
				return true;
			switch (Path.GetFileName(root))
			{
				case "SidebarLibrary":
				case "obj":
				case "bin":
					return true;
			}
			foreach (var subfolder in Directory.EnumerateDirectories(root))
			{
				if (!CollectInterestingProjects(subfolder, projectFolderCollector))
					return false;
			}
			//for Mono 2.10.4, Directory.EnumerateFiles(...) seems to see only writeable files???
			//var projectFiles = Directory.EnumerateFiles(root, "*.csproj");
			var projectFiles = Directory.GetFiles(root, "*.csproj");
			if (projectFiles.Length > 1)
			{
				LogError("Error: folder " + root + " has multiple .csproj files.");
				return false;
			}
			if (projectFiles.Length == 1)
				projectFolderCollector.Add(root);
			return true;
		}

		private void CreateStringsXml()
		{
			Options.LogMessage(MessageImportance.Low, "Create StringsXml for {0}", CurrentFile);
			var input = Options.StringsEnPath;
			var output = Options.StringsXmlPath(Locale);
			StoreLocalizedStrings(input, output);
		}

		private void StoreLocalizedStrings(string sEngFile, string sNewFile)
		{
			if (File.Exists(sNewFile))
				File.Delete(sNewFile);
			PoString posHeader;
			var dictTrans = LoadPOFile(CurrentFile, out posHeader);
			if (dictTrans.Count == 0)
			{
				// Todo: test/convert
				Console.WriteLine("No translations found in PO file!");
				throw new Exception("VOID PO FILE");
			}
			var xdoc = new XmlDocument();
			xdoc.Load(sEngFile);
			TranslateStringsElements(xdoc.DocumentElement, dictTrans);
			StoreTranslatedAttributes(xdoc.DocumentElement, dictTrans);
			StoreTranslatedLiterals(xdoc.DocumentElement, dictTrans);
			StoreTranslatedContextHelp(xdoc.DocumentElement, dictTrans);
			xdoc.Save(sNewFile);
		}

		/// <summary>
		/// This nicely recursive method replaces the English txt attribute values with the
		/// corresponding translated values if they exist.
		/// </summary>
		/// <param name="xel"></param>
		/// <param name="dictTrans"></param>
		// Copied from bin/src/LocaleStrings/Program.cs. Should remove this functionality there eventually.
		private static void TranslateStringsElements(XmlElement xel,
			Dictionary<string, PoString> dictTrans)
		{
			if (xel.Name == "string")
			{
				PoString pos;
				var sEnglish = xel.GetAttribute("txt");
				if (dictTrans.TryGetValue(sEnglish, out pos))
				{
					var sTranslation = pos.MsgStrAsString();
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

		// Copied from bin/src/LocaleStrings/Program.cs. Should remove this functionality there eventually.
		private static void StoreTranslatedThing(XmlElement xelRoot,
			Dictionary<string, PoString> enStrings, string whatThing,
			Func<string, bool> skipCommentMethod,
			Func<PoString, string, string> idMethod)
		{
			var xelGroup = xelRoot.OwnerDocument.CreateElement("group");
			xelGroup.SetAttribute("id", whatThing);
			foreach (var current in enStrings)
			{
				var pos = current.Value;
				var sValue = pos.MsgStrAsString();
				if (string.IsNullOrEmpty(sValue))
					continue;
				var autoComments = pos.AutoComments;
				if (autoComments == null)
					continue;
				foreach (var comment in autoComments)
				{
					if (skipCommentMethod(comment))
						continue;

					var xelString = xelRoot.OwnerDocument.CreateElement("string");
					xelString.SetAttribute("id", idMethod(pos, comment));
					xelString.SetAttribute("txt", sValue);
					xelGroup.AppendChild(xelString);
					break;
				}
			}
			xelRoot.AppendChild(xelGroup);
		}

		private static void StoreTranslatedAttributes(XmlElement xelRoot,
			Dictionary<string, PoString> enStrings)
		{
			StoreTranslatedThing(xelRoot, enStrings, "LocalizedAttributes",
				comment => comment == null || (!comment.StartsWith("/") && !comment.StartsWith("file:///")) || !IsFromXmlAttribute(comment),
				(pos, comment) => pos.MsgIdAsString());
		}

		// Copied from bin/src/LocaleStrings/Program.cs. Should remove this functionality there eventually.
		private static void StoreTranslatedLiterals(XmlElement xelRoot,
			Dictionary<string, PoString> enStrings)
		{
			StoreTranslatedThing(xelRoot, enStrings, "LocalizedLiterals",
				comment => comment == null || !comment.StartsWith("/") || !comment.EndsWith("/lit"),
				(pos, comment) => pos.MsgIdAsString());
		}

		// Copied from bin/src/LocaleStrings/Program.cs. Should remove this functionality there eventually.
		private static void StoreTranslatedContextHelp(XmlElement xelRoot,
			Dictionary<string, PoString> enStrings)
		{
			StoreTranslatedThing(xelRoot, enStrings, "LocalizedContextHelp",
				comment => string.IsNullOrEmpty(FindContextHelpId(comment)),
				(pos1, comment) => FindContextHelpId(comment));
		}

		// Copied from bin/src/LocaleStrings/Program.cs. Should remove this functionality there eventually.
		private static string FindContextHelpId(string comment)
		{
			const string ksContextMarker = "/ContextHelp.xml::/strings/item[@id=\"";
			if (comment == null || !comment.StartsWith("/"))
				return null;

			var idx = comment.IndexOf(ksContextMarker);
			if (idx <= 0)
				return null;

			var sId = comment.Substring(idx + ksContextMarker.Length);
			var idxEnd = sId.IndexOf('"');
			return idxEnd > 0 ? sId.Remove(idxEnd) : null;
		}

		private static bool IsFromXmlAttribute(string sComment)
		{
			var idx = sComment.LastIndexOf("/");
			if (idx < 0 || sComment.Length == idx + 1)
				return false;
			if (sComment[idx + 1] != '@')
				return false;
			return sComment.Length > idx + 2;
		}

		private static Dictionary<string, PoString> LoadPOFile(string sMsgFile, out PoString posHeader)
		{
			using (var inputStream = new StreamReader(sMsgFile, Encoding.UTF8))
			{
				var dictTrans = new Dictionary<string, PoString>();
				posHeader = PoString.ReadFromFile(inputStream);
				var pos = PoString.ReadFromFile(inputStream);
				while (pos != null)
				{
					if (!pos.HasEmptyMsgStr && (pos.Flags == null || !pos.Flags.Contains("fuzzy")))
						dictTrans.Add(pos.MsgIdAsString(), pos);
					pos = PoString.ReadFromFile(inputStream);
				}
				inputStream.Close();
				return dictTrans;
			}
		}

		private void CreateXmlMappingFromPo()
		{
			Options.LogMessage(MessageImportance.Low, "Create XML mapping from PO for {0}", CurrentFile);
			var output = Options.XmlPoFilePath(Locale);
			Directory.CreateDirectory(Path.GetDirectoryName(output));
			var converter = new Po2XmlConverter { PoFilePath = CurrentFile, XmlFilePath = output };
			converter.Run();
		}

		private enum PoState
		{
			Start,		// beginning of file
			MsgId,		// msgid seen most recently
			MsgStr		// msgstr seen most recently
		}

		private bool CheckForPoFileProblems()
		{
			Options.LogMessage(MessageImportance.Low, "Checking for PO file problems for {0}", CurrentFile);
			var retval = true;
			var keys = new HashSet<string>();
			var state = PoState.Start;
			var currentId = string.Empty;
			var currentValue = string.Empty;
			foreach (var line in File.ReadLines(CurrentFile))
			{
				if (string.IsNullOrEmpty(line.Trim()) || line.StartsWith("#"))
					continue;
				// Check for translator using a look-alike character in place of digit 0 or 1 in string.Format control string.
				if (CheckForError(line, new Regex("{[oOlLiI]}"),
					"{0} contains a suspicious string ({1}) that is probably a mis-typed string substitution marker using a letter in place of digit 0 or 1"))
					retval = false;
				if (CheckForError(line, new Regex("[{}][0-9]{"),
					"{0} contains a suspicious string ({1}) that is probably a mis-typed string substitution marker with braces messed up"))
					retval = false;
				if (CheckForError(line, new Regex("}[0-9][{}]"),
					"{0} contains a suspicious string ({1}) that is probably a mis-typed string substitution marker with braces messed up"))
					retval = false;
				if (CheckForError(line, new Regex("^(msgid|msgstr)[^{]*[0-9]}"),
					"{0} contains a suspicious string in ({1}) that is probably a mis-typed string substitution marker with a missing opening brace"))
					retval = false;
				if (CheckForError(line, new Regex("{[0-9][^}]*$"),
					"{0} contains a suspicious string ({1}) that is probably a mis-typed string substitution marker with a missing closing brace"))
					retval = false;
				if (CheckForError(line, new Regex("^(msgid|msgstr)[ \\t]+[^\"]"),
					"{0} contains a suspicious line starting with ({1}) that is probably a key or value with missing required open quote"))
					retval = false;
				if (CheckForError(line, new Regex("^(msgid|msgstr)[ \\t]+\"[^\"]*$"),
					"{0} contains a suspicious line ({1}) that is probably a key or value with missing required closing quote"))
					retval = false;
				if (line.StartsWith("msgid"))
				{
					if (state == PoState.MsgStr)
					{
						// We've collected the full Id and Value, so check them.
						if (!CheckMsgidAndMsgstr(keys, currentId, currentValue))
							retval = false;
					}
					if (state == PoState.MsgId)
					{
						LogError($"{CurrentFile} contains a key with no corresponding value: ({currentId})");
						retval = false;
					}
					state = PoState.MsgId;
					currentId = ExtractMsgValue(line);
					currentValue = string.Empty;
				}
				else if (line.StartsWith("msgstr"))
				{
					currentValue = ExtractMsgValue(line);
					if (state != PoState.MsgId)
					{
						LogError($"{CurrentFile} contains a value with no corresponding key: ({currentValue})");
						retval = false;
					}
					state = PoState.MsgStr;
				}
				else
				{
					switch (state)
					{
						case PoState.MsgId:
							var id = ExtractMsgValue(line);
							if (!string.IsNullOrEmpty(id))
								currentId = currentId + id;
							break;
						case PoState.MsgStr:
							var val = ExtractMsgValue(line);
							if (!string.IsNullOrEmpty(val))
								currentValue = currentValue + val;
							break;
					}
				}
			}
			// We need to check the final msgid/msgstr pair.
			if (!CheckMsgidAndMsgstr(keys, currentId, currentValue))
				retval = false;
			return retval;
		}

		private bool CheckForError(string contents, Regex pattern, string message)
		{
			var matches = pattern.Matches(contents);
			if (matches.Count == 0)
				return false; // all is well.
			LogError(string.Format(message, CurrentFile, matches[0].Value));
			return true;
		}

		private static string ExtractMsgValue(string line)
		{
			var idxMin = line.IndexOf('"');
			var idxLim = line.LastIndexOf('"');
			if (idxMin < 0 || idxLim <= idxMin)
				return string.Empty;
			++idxMin;	//step past the quote
			return line.Substring(idxMin, idxLim - idxMin);
		}

		private bool CheckMsgidAndMsgstr(ISet<string> keys, string msgid, string msgstr)
		{
			// allow empty data without complaint
			if (string.IsNullOrEmpty(msgid) && string.IsNullOrEmpty(msgstr))
				return true;
			if (keys.Contains(msgid))
			{
				LogError($"{CurrentFile} contains a duplicate key: {msgid}");
				return false;
			}
			keys.Add(msgid);
			var argRegEx = new Regex("{[0-9]}");
			var maxArg = -1;
			foreach (Match idmatch in argRegEx.Matches(msgid))
				maxArg = Math.Max(maxArg, Convert.ToInt32(idmatch.Value[1]));
			foreach (Match strmatch in argRegEx.Matches(msgstr))
			{
				if (Convert.ToInt32(strmatch.Value[1]) <= maxArg)
					continue;

				LogError(
					$"{CurrentFile} contains a key/value pair where the value ({msgstr}) has more arguments than the key ({msgid})");
				return false;
			}
			return true;
		}

	}
}

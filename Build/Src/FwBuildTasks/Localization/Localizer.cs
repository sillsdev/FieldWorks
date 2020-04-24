// Copyright (c) 2015-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.Build.Framework;

namespace SIL.FieldWorks.Build.Tasks.Localization
{
	/// <summary>
	/// This class exists to perform the work for one locale.
	/// The main reason for having it is so that we can have member variables like CurrentLocaleDir,
	/// and so that data for one parallel task is quite separate from others.
	/// </summary>
	public class Localizer
	{
		private LocalizerOptions Options { get; set; }

		public readonly List<string> Errors = new List<string>();

		/// <summary>the directory of localizations currently being processed (includes the locale subdirectory)</summary>
		internal string CurrentLocaleDir { get; private set; } // REVIEW (Hasso) 2019.11: better name?

		internal string Locale { get; set; }

		internal string Version { get; set; }
		internal string FileVersion { get; set; }
		internal string InformationVersion { get; set; }

		public void Initialize(string currentDir, LocalizerOptions options)
		{
			Options = options;
			CurrentLocaleDir = currentDir;
			Locale = Path.GetFileName(currentDir);
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
				if (Options.CopyStringsXml)
				{
					CopyStringsXml();
				}

				if (!GetProjectFolders(out var projectFolders))
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

				foreach (var currentFolder in projectFolders)
				{
					var projectLocalizer = CreateProjectLocalizer(currentFolder,
						new ProjectLocalizerOptions(this, Options));
					projectLocalizer.ProcessProject();
				}
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

		private void CopyStringsXml()
		{
			var xmlFileName = string.Format(LocalizeFieldWorks.StringsXmlPattern, Locale);
			var localizedXmlPath = Options.StringsXmlPath(Locale);
			var localizedXmlSourcePath = Options.StringsXmlSourcePath(Locale);
			// ReSharper disable once AssignNullToNotNullAttribute - localizedXmlPath is in a valid directory
			Directory.CreateDirectory(Path.GetDirectoryName(localizedXmlPath));
			if (File.Exists(localizedXmlSourcePath))
			{
				// check for errors
				var xmlDoc = XDocument.Load(localizedXmlSourcePath);
				// ReSharper disable once AssignNullToNotNullAttribute -- there will always be a root
				var elts = xmlDoc.Root.XPathSelectElements("/strings/group/string");
				var hasErrors = false;
				foreach (var elt in elts)
				{
					if (CheckForErrors(localizedXmlSourcePath, elt.Attribute("txt")?.Value))
						hasErrors = true;
				}
				if (hasErrors)
					return;

				// copy
				File.Copy(localizedXmlSourcePath, localizedXmlPath, overwrite: true);
				Options.LogMessage(MessageImportance.Low, "copying {0} to {1}", xmlFileName, localizedXmlPath);
			}
			else
			{
				throw new FileNotFoundException($"{xmlFileName} not found", localizedXmlSourcePath);
			}
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


		/// <returns><c>true</c> if the given string has errors in string.Format variables</returns>
		internal bool CheckForErrors(string filename, string localizedText)
		{
			const string MistypedSubMarker1InFile0 =
				"{0} contains a suspicious string ({1}) that is probably a mis-typed string substitution marker ";
			if (string.IsNullOrEmpty(localizedText?.Trim()))
				return false; // an empty string has no errors
			// Check for translator using a look-alike character in place of digit 0 or 1 in string.Format control string.
			return CheckForError(filename, localizedText, new Regex("{[oOlLiI]}"),
					MistypedSubMarker1InFile0 + "using a letter in place of digit 0 or 1") ||
				CheckForError(filename, localizedText, new Regex("[{}][0-9]{1-2}{"), MistypedSubMarker1InFile0 + "with braces messed up") ||
				CheckForError(filename, localizedText, new Regex("}[0-9]{1-2}[{}]"), MistypedSubMarker1InFile0 + "with braces messed up") ||
				CheckForError(filename, localizedText, new Regex("{[^}]+{"), MistypedSubMarker1InFile0 + "with braces messed up") ||
				CheckForError(filename, localizedText, new Regex("}[^{]+}"), MistypedSubMarker1InFile0 + "with braces messed up") ||
				CheckForError(filename, localizedText, new Regex("^[^{]*[0-9]}"), MistypedSubMarker1InFile0 + "with a missing opening brace") ||
				CheckForError(filename, localizedText, new Regex("{[0-9][^}]*$"), MistypedSubMarker1InFile0 + "with a missing closing brace");
			// TODO (Hasso) 2019.12: if (!CheckMsgidAndMsgstr(englishText, localizedText)) return true;
		}

		/// <returns><c>true</c> if the given string matches the pattern (has errors)</returns>
		private bool CheckForError(string filename, string localizedText, Regex pattern,
			string message)
		{
			var matches = pattern.Matches(localizedText);
			if (matches.Count == 0)
				return false; // all is well.
			LogError(string.Format(message, filename, matches[0].Value));
			return true;
		}

		private bool CheckMsgidAndMsgstr(string msgid, string msgstr)
		{
			// allow empty data without complaint
			if (string.IsNullOrEmpty(msgid) && string.IsNullOrEmpty(msgstr))
				return true;

			var argRegEx = new Regex("{[0-9]}");
			var maxArg = -1;
			foreach (Match idmatch in argRegEx.Matches(msgid))
				maxArg = Math.Max(maxArg, Convert.ToInt32(idmatch.Value[1]));
			foreach (Match strmatch in argRegEx.Matches(msgstr))
			{
				if (Convert.ToInt32(strmatch.Value[1]) <= maxArg)
					continue;

				// TODO (Hasso) 2019.12: LogError(
				//	$"{CurrentFile} contains a key/value pair where the value ({msgstr}) has more arguments than the key ({msgid})");
				return false;
			}
			return true;
		}
	}
}

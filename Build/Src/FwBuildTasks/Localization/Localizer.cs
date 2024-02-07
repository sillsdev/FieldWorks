// Copyright (c) 2015-2023 SIL International
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
		internal string CurrentLocaleDir { get; private set; }

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

		public void Process()
		{
			try
			{
				if (Options.CopyStringsXml)
				{
					CopyStringsXml();
				}

				if (!GetProjectFolders(out var projectFolders))
				{
					return;
				}

				if (!string.IsNullOrEmpty(Options.InformationVersion))
				{
					ParseInformationVersion(Options.InformationVersion);
				}
				else
				{
					if (File.Exists(Options.AssemblyInfoPath))
					{
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
					}

					if (string.IsNullOrEmpty(FileVersion))
					{
						FileVersion = "0.0.0.0";
					}
					if (string.IsNullOrEmpty(InformationVersion))
					{
						InformationVersion = FileVersion;
					}
					if (string.IsNullOrEmpty(Version))
					{
						Version = FileVersion;
					}
				}

				foreach (var currentFolder in projectFolders)
				{
					var projectLocalizer = CreateProjectLocalizer(currentFolder,
						new ProjectLocalizerOptions(this, Options));
					projectLocalizer.ProcessProject();
				}
			}
			catch (Exception ex)
			{
				LogError($"Caught exception processing {Locale}: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
			}
		}

		internal void ParseInformationVersion(string infoVer)
		{
			InformationVersion = infoVer;
			var match = new Regex(@"(?<ver>\d+\.\d+(\.\d+)?)\D+(?<rev>\d+)?\D*").Match(infoVer);
			var revision = match.Groups["rev"];
			var lastPart = revision.Success ? $".{int.Parse(revision.Value)}" : string.Empty;
			FileVersion = Version = $"{match.Groups["ver"]}{lastPart}";
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
				var hasErrors = false;
				// ReSharper disable once AssignNullToNotNullAttribute -- there will always be a root
				foreach (var elt in xmlDoc.Root.XPathSelectElements("/strings//group/string"))
				{
					if (HasErrors(localizedXmlSourcePath, elt.Attribute("txt")?.Value))
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
			//for Mono 2.10.4, Directory.EnumerateFiles(...) seems to see only writable files???
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


		/// <returns><c>true</c> if the given string has errors in string.Format variables or other obvious places</returns>
		internal bool HasErrors(string filename, string localizedText, string originalText = null, string comment = null)
		{
			if (string.IsNullOrWhiteSpace(localizedText)){
				if (string.IsNullOrWhiteSpace(originalText))
					return false; // an empty string has no errors
				LogError($"{filename} contains an empty string as a translation for '{originalText.Substring(0, Math.Min(originalText.Length, 100))}'");
				return true;
			}
			const string mistypedSubMarker1InFile0 =
				"{0} contains a suspicious string ({1}) that is probably a mis-typed string substitution marker ";
			const string messedUpBraces1InFile0 = mistypedSubMarker1InFile0 + "with braces messed up";
			// Check for translator using a look-alike character in place of digit 0 or 1 in string.Format control string.
			return HasError(filename, localizedText, new Regex("{[oOlLiI]}"), mistypedSubMarker1InFile0 + "using a letter in place of digit 0 or 1") ||
				HasError(filename, localizedText, new Regex("[{}][0-9]{1-2}{"), messedUpBraces1InFile0) ||
				HasError(filename, localizedText, new Regex("}[0-9]{1-2}[{}]"), messedUpBraces1InFile0) ||
				HasError(filename, localizedText, new Regex("{[^}]+{"), messedUpBraces1InFile0) ||
				HasError(filename, localizedText, new Regex("}[^{]+}"), messedUpBraces1InFile0) ||
				HasError(filename, localizedText, new Regex("^[^{]*[0-9]}"), mistypedSubMarker1InFile0 + "with a missing opening brace") ||
				HasError(filename, localizedText, new Regex("{[0-9][^}]*$"), mistypedSubMarker1InFile0 + "with a missing closing brace") ||
				originalText != null &&
					(HasAddedOrRemovedFormatMarkers(filename, localizedText, originalText, comment) ||
					HasCorruptColorString(filename, localizedText, originalText));
		}

		/// <returns><c>true</c> if the given string matches the pattern (has errors)</returns>
		private bool HasError(string filename, string localizedText, Regex pattern, string message)
		{
			var matches = pattern.Matches(localizedText);
			if (matches.Count == 0)
				return false; // all is well.
			LogError(string.Format(message, filename, matches[0].Value));
			return true;
		}

		/// <returns><c>true</c> if the localized text has different formatting markers than the original text (has errors)</returns>
		private bool HasAddedOrRemovedFormatMarkers(string filename, string localizedText, string originalText, string comment)
		{
			const int verifiableArgs = 10;
			var argRegEx = new Regex("{[0-9]}");
			var originalHas = new bool[verifiableArgs];
			var localizedHas = new bool[verifiableArgs];

			foreach (Match match in argRegEx.Matches(originalText))
			{
				originalHas[match.Value[1] - '0'] = true;
			}

			foreach (Match match in argRegEx.Matches(localizedText))
			{
				localizedHas[match.Value[1] - '0'] = true;
			}

			for (var i = 0; i < verifiableArgs; i++)
			{
				// The original and localized texts should have the same arguments (unless an argument is optional)
				if (originalHas[i] != localizedHas[i] && (comment == null || !comment.Contains($"{{{i}}}") || !comment.Contains("optional")))
				{
					LogError($"{filename} contains a value ({localizedText}) that has different arguments than the original ({originalText})");
					return true;

				}
			}

			return false;
		}

		/// <returns><c>true</c> if the file is ColorStrings.resx and the trailing ",r,g,b" has changed from the original text</returns>
		private bool HasCorruptColorString(string filename, string localizedText, string originalText)
		{
			if (!new Regex(@"ColorStrings\...(...)?\.resx$").IsMatch(filename) || originalText == "Custom")
				return false;
			var origParts = originalText.Split(',');
			var locParts = localizedText.Split(',');

			// Four parts: RGB + localized name
			if (locParts.Length == 4)
			{
				var i = 1;
				for (; i <= 3; i++)
				{
					if (origParts[origParts.Length - i] != locParts[locParts.Length - i])
						break;
				}
				// i == 4 if we made it through R, G, and B without finding changes
				if (i == 4)
					return false;
			}
			LogError($"{filename} contains a color string '{localizedText}' whose RGB value is missing or doesn't match '{originalText}'");
			return true;
		}
	}
}

// Copyright (c) 2010-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Web;
using SIL.LCModel;
using SIL.PlatformUtilities;
using SIL.Reporting;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Class representing the arguments necessary for starting up a FieldWorks application
	/// (from the command line or from a URI).
	/// See the class comment on FwLinkArgs for details on how all the parts of hyperlinking work.
	/// </summary>
	[Serializable]
	public class FwAppArgs : FwLinkArgs
	{
		#region Command-line switch constants
		/// <summary>Command-line argument: Command-line usage help</summary>
		public const string kHelp = "help";
		/// <summary>Command-line argument: Culture abbreviation</summary>
		public const string kLocale = "locale";
		/// <summary>Command-line argument: The project name (or file)</summary>
		public const string kProject = "db";
		/// <summary>URI argument: The project name (or file)</summary>
		public const string kProjectUri = "database";
		/// <summary>Command-line argument: </summary>
		public const string kFlexConfigFile = "x";
		/// <summary>Command-line argument: The fwbackup file</summary>
		public const string kRestoreFile = "restore";
		/// <summary>Command-line argument: String indicating optional files to restore</summary>
		public const string kRestoreOptions = "include";
		/// <summary>Command-line argument: flag that keeps FW from showing UI.</summary>
		public const string kNoUserInterface = "noui";
		/// <summary>Command-line argument: flag that causes FW to come up in a server mode for other applications.</summary>
		public const string kAppServerMode = "appServerMode";
		/// <summary>Command-line argument: flag that tells FW to bring up a dialog to set an associated project.</summary>
		public const string kChooseProject = "chooseProject";
		#endregion

		#region Member variables
		#endregion

		#region Properties

		/// <summary>
		/// Gets the database/project name (possibly/probably a file path/name.
		/// Will never return null.
		/// </summary>
		public string Database { get; private set; } = string.Empty;

		/// <summary>
		/// Gets the type of the database/backend. (e.g. XML, MySql, etc.) Will never return null.
		/// </summary>
		public string DatabaseType { get; } = string.Empty;

		/// <summary>
		/// Gets the locale for the user interface.
		/// </summary>
		public string Locale { get; private set; } = string.Empty;

		/// <summary>
		/// Flag indicating whether to show help (if true, all other arguments can be ignored)
		/// </summary>
		public bool ShowHelp { get; private set; }

		/// <summary>
		/// Flag indicating whether or not to hide the UI.
		/// </summary>
		public bool NoUserInterface { get; private set; }

		/// <summary>
		/// Flag indicating whether or not to run in a server mode for other applications.
		/// </summary>
		public bool AppServerMode { get; private set; }

		/// <summary>
		/// Gets the config file. Will never return null.
		/// </summary>
		public string ConfigFile { get; private set; } = string.Empty;

		/// <summary>
		/// Gets the backup file used for a restore. Will never return null.
		/// </summary>
		public string BackupFile { get; private set; } = string.Empty;

		/// <summary>
		/// Gets the restore options user chose for creating RestoreProjectSettings. Will
		/// never return null.
		/// </summary>
		public string RestoreOptions { get; private set; } = string.Empty;

		/// <summary>
		/// Gets a value indicating whether this FwAppArgs also contains information pertaining
		/// to a link request.
		/// </summary>
		public bool HasLinkInformation => !string.IsNullOrEmpty(ToolName);

		/// <summary>
		/// Gets the full path of the file that the handle for a chosen project will be
		/// written to.
		/// </summary>
		public string ChooseProjectFile { get; private set; } = string.Empty;
		#endregion

		#region Constructor

		// NOTE: Don't make constructor overloads that take only varying numbers of string
		// parameters. That would conflict with the string params constructor causing a bunch
		// of stuff to break (which, thankfully, we have tests for)

		/// <summary />
		/// <param name="database">The name of the database.</param>
		/// <param name="toolName">Name/path of the tool or view within the specific application.</param>
		/// <param name="targetGuid">The GUID of the object which is the target of this link.</param>
		public FwAppArgs(string database, string toolName, Guid targetGuid) : base(toolName, targetGuid)
		{
			ProcessArg(kProject, database);
		}

		/// <summary />
		public FwAppArgs(params string[] rgArgs)
		{
			if (rgArgs.Length == 1 && rgArgs[0].StartsWith(kSilScheme + ":"))
			{
				// The only argument was a link so let FwLink parse it for us
				if (!InitializeFromUrl(rgArgs[0]))
					ShowHelp = true; // Badly formed link
				return;
			}

			// The command wasn't a link request.
			Dictionary<string, string> commandLineArgs = null;
			try
			{
				commandLineArgs = ParseCommandLine(rgArgs);
				if (commandLineArgs.ContainsKey(kHelp))
					ProcessArg(kHelp, string.Empty);
			}
			catch (ArgumentException argEx)
			{
				Logger.WriteEvent(argEx.Message);
				ShowHelp = true;
			}

			if (commandLineArgs == null || ShowHelp)
			{
				return; // Not much we can do
			}
			if (commandLineArgs.ContainsKey(kLink))
			{
				if (!InitializeFromUrl(commandLineArgs[kLink]))
				{
					ShowHelp = true;
				}
			}
			else
			{
				foreach (var kvp in commandLineArgs)
				{
					ProcessArg(kvp.Key, kvp.Value);
				}
			}
		}
		#endregion

		#region Overridden methods

		/// <summary>
		/// Adds the properties as named arguments in a format that can be used to produce a
		/// URI query.
		/// </summary>
		protected override void AddProperties(StringBuilder bldr)
		{
			bldr.AppendFormat("{0}={1}", kProjectUri, Database);
			base.AddProperties(bldr);
		}

		/// <summary>
		/// Some comparisons don't care about the content of the property table, so we provide a
		/// method similar to Equals but not quite as demanding.
		/// </summary>
		public override bool EssentiallyEquals(FwLinkArgs lnk)
		{
			var appArgs = lnk as FwAppArgs;
			return appArgs != null && base.EssentiallyEquals(lnk) && appArgs.Database == Database;
		}
		#endregion

		#region Command-line Handling Methods

		/// <summary>
		/// Parse the command line, and return the results in a hashtable. The hash key is the
		/// option tag. The hashtable value holds the parameter value.
		/// The value may be null for cases where the option tag is all there is (e.g.,
		/// -help). If an argument is supplied on the command line that does not match any
		/// switch, it is assumed to be the project name.
		///
		/// Current set of options:
		/// -app Application name (TE or FLEx)
		/// -db Database (BEP) Type (xml)
		/// -proj Project Name (usually a file name)
		/// -help (also -? and -h Command line usage help
		/// -link URL (as composed at some point by FwLink)
		/// -locale CultureAbbr
		/// </summary>
		/// <param name="rgArgs">Array of strings containing the command-line arguments. In
		/// general, the command-line will be parsed into whitespace-separated tokens, but
		/// double-quotes (") can be used to create a multiple-word token that will appear in
		/// the array as a single argument. One argument in this array can represent either a
		/// key, a value, or both (since whitespace is not required between keys and values.
		/// </param>
		/// <exception cref="T:ArgumentException">Incorrectly formed command line. Caller should
		/// alert user to correct command-line structure.
		/// </exception>
		private static Dictionary<string, string> ParseCommandLine(string[] rgArgs)
		{
			var dictArgs = new Dictionary<string, string>();
			if (rgArgs == null || rgArgs.Length == 0)
			{
				return dictArgs;
			}
			var sKey = string.Empty;
			var value = string.Empty;
			foreach (var sArg in rgArgs)
			{
				if (string.IsNullOrEmpty(sArg))
				{
					continue;
				}
				var iCurrChar = 0;
				if (sArg[iCurrChar] == '-' || Platform.IsWindows && sArg[iCurrChar] == '/') // Start of option
				{
					// Start of a new argument key
					if (!string.IsNullOrEmpty(value))
					{
						// Save the previous values to the previous key
						if (dictArgs.ContainsKey(sKey))
						{
							throw new ArgumentException(sKey + " was passed in more then once");
						}
						dictArgs[sKey] = value;
						value = string.Empty;
					}
					else if (sKey.Length > 0)
					{
						// Found a tag in the previous pass, but it has no argument, so save it in
						// the map with a value of an empty vector, before processing current tag.
						dictArgs.Add(sKey, string.Empty);
					}
					++iCurrChar; // Increment counter

					// The user may have just put an argument right next to the marker,
					// so we need to split the tag from the argument at this point.
					sKey = CommandLineSwitch(sArg, ref iCurrChar);
				}
				if (iCurrChar < sArg.Length)
				{
					// Check for a second value for the current key
					var newValue = sArg.Substring(iCurrChar);
					if (!string.IsNullOrEmpty(value))
					{
						throw new ArgumentException(newValue + " added as second value for " + sKey);
					}
					value = newValue;
					// There may not be a key, if this is the first argument, as when opening a file directly.
					if (sKey.Length == 0)
					{
						sKey = (Path.GetExtension(value) == LcmFileHelper.ksFwBackupFileExtension) ? kRestoreFile : kProject;
					}
				}
			}

			// Save final tag.
			if (sKey.Length > 0)
			{
				dictArgs.Add(sKey, value);
			}

			return dictArgs;
		}

		/// <summary>
		/// Checks given string argument, starting at position iCurrChar for one of the
		/// standard approved multi-character command line switches. If not found, it assumes
		/// this is a single-character switch.
		/// </summary>
		///
		/// <param name='sArg'>Command-line argument being processed</param>
		/// <param name='iCurrChar'>Zero-based index indicating first character in sArg to be
		/// considered when looking for switch. Typically, the initial value will be 1, since
		/// character 0 will usually be a - or /. This parameter is returned to the caller
		/// incremented by the number of characters in the switch.</param>
		private static string CommandLineSwitch(string sArg, ref int iCurrChar)
		{
			if (FindCommandLineArgument(sArg, ref iCurrChar, kAppServerMode))
			{
				return kAppServerMode;
			}
			if (FindCommandLineArgument(sArg, ref iCurrChar, kProject)) // (old) project name
			{
				return kProject;
			}
			if (FindCommandLineArgument(sArg, ref iCurrChar, "proj")) // project name
			{
				return kProject;
			}
			if (FindCommandLineArgument(sArg, ref iCurrChar, kHelp))
			{
				return kHelp;
			}
			if (FindCommandLineArgument(sArg, ref iCurrChar, kChooseProject))
			{
				return kChooseProject;
			}
			if (FindCommandLineArgument(sArg, ref iCurrChar, kLink))
			{
				return kLink;
			}
			if (FindCommandLineArgument(sArg, ref iCurrChar, kLocale))
			{
				return kLocale;
			}
			if (FindCommandLineArgument(sArg, ref iCurrChar, kRestoreFile))
			{
				return kRestoreFile;
			}
			if (FindCommandLineArgument(sArg, ref iCurrChar, kRestoreOptions))
			{
				return kRestoreOptions;
			}
			if (FindCommandLineArgument(sArg, ref iCurrChar, kNoUserInterface))
			{
				return kNoUserInterface;
			}
			if (sArg.Length > iCurrChar)
			{
				// It is a single character tag.
				var sKey = sArg.Substring(iCurrChar, 1).ToLowerInvariant();
				if (sKey == "?" || sKey == "h") // Variants of help.
				{
					++iCurrChar;
					return kHelp;
				}
				sKey = sArg.Substring(iCurrChar).ToLowerInvariant();
				iCurrChar += sArg.Length - 1;
				return sKey;
			}
			throw new ArgumentException();
		}

		/// <summary>
		/// Finds the specified argument key in the specified argument string.
		/// </summary>
		/// <param name="sArg">The argument string</param>
		/// <param name="iCurrChar">Index of the current character to start looking.</param>
		/// <param name="key">The argument to look for</param>
		private static bool FindCommandLineArgument(string sArg, ref int iCurrChar, string key)
		{
			if (string.Compare(sArg, iCurrChar, key, 0, key.Length, true) == 0)
			{
				iCurrChar += key.Length;
				return true;
			}
			return false;
		}

		#endregion

		#region Public methods

		/// <summary>
		/// Clears any link information from this FwAppArgs.
		/// </summary>
		public void ClearLinkInformation()
		{
			Tag = string.Empty;
			m_toolName = string.Empty;
			TargetGuid = Guid.Empty;
		}
		#endregion

		#region Private helper methods

		/// <summary>
		/// Initializes from the given URL.
		/// </summary>
		/// <param name="url">The URL.</param>
		/// <returns><c>true</c> if successfully initialized from the URL; <c>false</c>
		/// otherwise</returns>
		private bool InitializeFromUrl(string url)
		{
			try
			{
				if (!url.StartsWith(kFwUrlPrefix))
				{
					throw new UriFormatException("FieldWorks link must begin with " + kFwUrlPrefix);
				}
				var destination = new Uri(url);
				var decodedQuery = HttpUtility.UrlDecode(destination.Query);
				foreach (var pair in decodedQuery.Split('?', '&'))
				{
					var i = pair.IndexOf("=");
					if (i < 0)
					{
						continue;
					}
					var name = pair.Substring(0, i);
					var value = pair.Substring(i + 1);
					ProcessArg(name, value);
				}

				if (string.IsNullOrEmpty(Database))
				{
					throw new UriFormatException("FieldWorks link must include a project name.");
				}
			}
			catch (UriFormatException e)
			{
				Logger.WriteError(new Exception("Invalid link passed on FieldWorks command line: " + url, e));
				return false;
			}
			return true;
		}

		/// <summary>
		/// Processes a URL or command-line argument represented by the given name and value pair.
		/// </summary>
		private void ProcessArg(string name, string value)
		{
			Debug.Assert(value != null || name == kHelp);
			switch (name)
			{
				case kProject:
				case kProjectUri: Database = value; break;
				case kLocale: Locale = value; break;
				case kHelp: ShowHelp = true; break;
				case kChooseProject: ChooseProjectFile = value; break;
				case kFlexConfigFile: ConfigFile = value; break;
				case kRestoreFile: BackupFile = value; break;
				case kRestoreOptions: RestoreOptions = value; break;
				case kNoUserInterface: NoUserInterface = true; break;
				case kAppServerMode: AppServerMode = true; break;
				case kTag: Tag = value; break;
				case kTool: m_toolName = value; break;
				case kGuid:
					if (value != "null")
					{
						TargetGuid = new Guid(value);
					}
					break;
				default:
					LinkProperties.Add(new LinkProperty(name, Decode(value)));
					break;
			}
		}
		#endregion
	}
}
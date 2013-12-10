// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FwLinkArgs.cs
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Web;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Resources;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.Common.FwUtils
{
	#region FwLinkArgs class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// provides a message object specifically for asking a FieldWorks application
	/// to do various navigation activities.
	///
	/// Here is a summary of how the different pieces of linking work together.
	///
	/// For internal links, such as history, LinkListener creates an FwLinkArgs, and may store
	/// it (e.g., in the history stack). The current path for this in FLEx is that
	/// RecordView.UpdateContextHistory broadcasts AddContextToHistory with a new FwLinkArgs
	/// based on the current toolname and Guid. An instance of LinkListener is in the current
	/// list of colleagues and implements OnAddContextToHistory to update the history.
	/// LinkListener also implements OnHistoryBack (and forward), which call FollowActiveLink
	/// passing an FwLinkArgs to switch the active window.
	///
	/// For links which may address a different project, we use a subclass of FwLinkArgs,
	/// FwAppArgs, which adds information about the application and project. One way these
	/// get created is LinkListener.OnCopyLocationAsHyperlink. The resulting text may be
	/// pasted in other applications which understand hyperlinks, or in FLEx using the Paste
	/// Hyperlink command.
	///
	/// When such a link is clicked, execution begins in VwBaseVc.DoHotLinkAction. This executes
	/// an OS-level routine which tries to interpret the hyperlink. To make this work there
	/// must be a registry entry (created by the installer, but developers must do it by hand).
	/// The key HKEY_CLASSES_ROOT\silfw\shell\open\command must contain as its default value
	/// a string which is the path to FieldWorks.exe in quotes, followed by " %1". For example,
	///
	///     "C:\ww\Output\Debug\FieldWorks.exe" %1
	///
	/// In addition, the silfw key must have a default value of "URL:SILFW Protocol" and another
	/// string value called "URL Protocol" (no value). (Don't ask me why...Alistair may know.)
	///
	/// This initially results in a new instance of FieldWorks being started up with the URL
	/// as the command line. FieldWorks.Main() reconstitutes the FwAppArgs from the command-line
	/// data, and after a few checks passes the arguments to LaunchProject. If there isn't
	/// already an instance of FieldWorks running on that project, the new instance starts up
	/// on the appropriate app and project. The FwAppArgs created from the command line, with
	/// the tool and object, are passed to the application when created (currently only
	/// FwXApp does something with the passed-in FwAppArgs).
	///
	/// If there is already an instance running, this is detected in TryFindExistingProcess,
	/// which uses inter-process communication to invoke HandleOpenProjectRequest on each
	/// running instance of FieldWorks. This method is implemented in RemoteRequest,
	/// and if the project matches calls FieldWorks.KickOffAppFromOtherProcess. This takes
	/// various paths depending on which app is currently running. It often ends up activating
	/// a window and calling app.HandleIncomingLink() passing the FwAppArgs. This method,
	/// currently only implemented in FwXApp, activates the right tool and object.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[Serializable]
	public class FwLinkArgs
	{
		#region Constants
		/// <summary>Internet Access Protocol identifier that indicates that this is a FieldWorks link</summary>
		public const string kSilScheme = "silfw";
		/// <summary>Indicates that this link should be handled by the local computer</summary>
		public const string kLocalHost = "localhost";
		/// <summary>Command-line argument: URL string for a FieldWorks link</summary>
		public const string kLink = "link";
		/// <summary>Command-line argument: App-specific tool name for a FieldWorks link</summary>
		public const string kTool = "tool";
		/// <summary>Command-line argument: FDO object guid for a FieldWorks link</summary>
		public const string kGuid = "guid";
		/// <summary>Command-line argument: FDO object field tag for a FieldWorks link</summary>
		public const string kTag = "tag";
		/// <summary>Fieldworks link prefix</summary>
		public const string kFwUrlPrefix = kSilScheme + "://" + kLocalHost + "/" + kLink + "?";
		#endregion

		#region Member variables
		/// <summary></summary>
		protected string m_toolName = string.Empty;
		/// <summary></summary>
		protected string m_tag = string.Empty;
		private readonly List<Property> m_propertyTableEntries = new List<Property>();
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The name/path of the tool or view within the specific application. Will never be null.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ToolName
		{
			get
			{
				if (m_toolName == null)
					return "";
				return m_toolName;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The GUID of the object which is the target of this link.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Guid TargetGuid { get; protected set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Additional information to be included in the property table. Will never be null.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<Property> PropertyTableEntries
		{
			get { return m_propertyTableEntries; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// An additional tag to differentiate between other FwLinkArgs entries between the
		/// same core ApplicationName, Database, Guid values. Will never be null.
		/// (cf. LT-7847)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Tag
		{
			get
			{
				Debug.Assert(m_tag != null);
				return m_tag;
			}
		}
		#endregion  Properties

		#region Construction and Initialization
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FwLinkArgs"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected FwLinkArgs()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FwLinkArgs"/> class.
		/// </summary>
		/// <param name="toolName">Name/path of the tool or view within the specific application.</param>
		/// <param name="targetGuid">The GUID of the object which is the target of this link.</param>
		/// ------------------------------------------------------------------------------------
		public FwLinkArgs(string toolName, Guid targetGuid) : this(toolName, targetGuid, null)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FwLinkArgs"/> class.
		/// </summary>
		/// <param name="toolName">Name/path of the tool or view within the specific application.</param>
		/// <param name="targetGuid">The GUID of the object which is the target of this link.</param>
		/// <param name="tag">The tag.</param>
		/// ------------------------------------------------------------------------------------
		public FwLinkArgs(string toolName, Guid targetGuid, string tag) : this()
		{
			m_toolName = toolName;
			TargetGuid = targetGuid;
			m_tag = tag ?? string.Empty;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FwLinkArgs"/> class.
		/// </summary>
		/// <param name="url">a URL string like that produced by ToString().</param>
		/// ------------------------------------------------------------------------------------
		public FwLinkArgs(string url)
		{
			if (!url.StartsWith(kFwUrlPrefix))
				throw new ArgumentException(String.Format("unrecognized FwLinkArgs URL string: {0}", url));
			string query = HttpUtility.UrlDecode(url.Substring(23));
			string[] rgsProps = query.Split('&');
			foreach (string prop in rgsProps)
			{
				string[] propPair = prop.Split('=');
				if (propPair.Length != 2)
					throw new ArgumentException(String.Format("invalid FwLinkArgs URL string: {0}", url));
				switch (propPair[0])
				{
					case kTool:
						m_toolName = propPair[1];
						break;
					case kGuid:
						TargetGuid = new Guid(propPair[1]);
						break;
					case kTag:
						m_tag = propPair[1];
						break;
					default:
						PropertyTableEntries.Add(new Property(propPair[0], propPair[1]));
						break;
				}
			}
			if (String.IsNullOrEmpty(m_toolName) || TargetGuid == Guid.Empty || m_tag == null)
				throw new ArgumentException(String.Format("invalid FwLinkArgs URL string: {0}", url));

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copy the link-related information to a new FwLinkArgs. Currently this does NOT
		/// yield an FwAppArgs even if the recipient is one.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwLinkArgs CopyLinkArgs()
		{
			FwLinkArgs result = new FwLinkArgs(m_toolName, TargetGuid, m_tag);
			result.m_propertyTableEntries.AddRange(m_propertyTableEntries);
			return result;
		}
		#endregion

		#region overridden methods and helpers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a hash code for this instance.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override int GetHashCode()
		{
			return ToString().GetHashCode();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Some comparisons don't care about the content of the property table, so we provide a
		/// method similar to Equals but not quite as demanding.
		/// </summary>
		/// <param name="lnk">The link to compare.</param>
		/// ------------------------------------------------------------------------------------
		public virtual bool EssentiallyEquals(FwLinkArgs lnk)
		{
			if (lnk == null)
				return false;
			if (lnk == this)
				return true;
			if (lnk.ToolName != ToolName)
				return false;
			if (lnk.TargetGuid != TargetGuid)
				return false;
			// tag is optional, but if a tool uses it with content, it should consistently provide content.
			// therefore if one of these links does not have content, and the other does then
			// we'll assume they refer to the same link
			// (one link simply comes from a control with more knowledge then the other one)
			return lnk.Tag.Length == 0 || Tag.Length == 0 || lnk.Tag == Tag;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified <see cref="T:System.Object"/> is equal to this instance.
		/// </summary>
		/// <param name="obj">The <see cref="T:System.Object"/> to compare with this instance.</param>
		/// <returns>
		/// 	<c>true</c> if the specified <see cref="T:System.Object"/> is equal to this instance; otherwise, <c>false</c>.
		/// </returns>
		/// <exception cref="T:System.NullReferenceException">
		/// The <paramref name="obj"/> parameter is null.
		/// </exception>
		/// ------------------------------------------------------------------------------------
		public override bool Equals(object obj)
		{
			FwLinkArgs link = obj as FwLinkArgs;
			if (link == null)
				return false;
			if (link == this)
				return true;
			//just compare the URLs
			return (ToString() == link.ToString());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a URL corresponding to this link
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String"/> that represents this instance.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			UriBuilder uriBuilder = new UriBuilder(kSilScheme, kLocalHost);
			uriBuilder.Path = kLink;
			StringBuilder query = new StringBuilder();
			AddProperties(query);

			foreach (Property property in PropertyTableEntries)
				query.AppendFormat("&{0}={1}", property.name, Encode(property.value));

			//make it safe to represent as a url string (e.g., convert spaces)
			uriBuilder.Query = HttpUtility.UrlEncode(query.ToString());

			return uriBuilder.Uri.AbsoluteUri;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the properties as named arguments in a format that can be used to produce a
		/// URI query.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void AddProperties(StringBuilder bldr)
		{
			bldr.AppendFormat("{0}={1}&{2}={3}&{4}={5}", kTool, ToolName, kGuid,
				(TargetGuid == Guid.Empty) ? "null" : TargetGuid.ToString(), kTag, Tag);
		}
		#endregion

		#region Serialization
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add type info to the parameter if it's not a string
		/// </summary>
		/// <param name="o">The o.</param>
		/// ------------------------------------------------------------------------------------
		protected string Encode(object o)
		{
			switch(o.GetType().ToString())
			{
				default: throw new ArgumentException("Don't know how to serialize type of " + o.GetType());
				case "System.Boolean":
					return "bool:" + o;
				case "System.String":
					return (String)o;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Use the explicit type tag to parse parameters into the right type
		/// </summary>
		/// <param name="value">The value.</param>
		/// ------------------------------------------------------------------------------------
		protected object Decode(string value)
		{
			if(value.IndexOf("bool:") > -1)
			{
				value = value.Substring(5);
				return bool.Parse(value);
			}
			return HttpUtility.UrlDecode(value);
		}
		#endregion

		#region Static utility methods

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If  the database= and server= sections of the url string match up against the
		/// current project and server, set the server to empty (if it isn't already), and set
		/// the project to "this$".  This allows the project to be renamed without invalidating
		/// any internal crossreferences in the form of hyperlinks.  (See FWR-3437.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string FixSilfwUrlForCurrentProject(string url, string project, string server)
		{
			if (!url.StartsWith(kFwUrlPrefix))
				return url;
			string query = HttpUtility.UrlDecode(url.Substring(kFwUrlPrefix.Length));
			string[] properties = query.Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
			int idxDatabase = -1;
			int idxServer = -1;
			string urlDatabase = null;
			string urlServer = null;
			for (int i = 0; i < properties.Length; ++i)
			{
				if (properties[i].StartsWith("database="))
				{
					idxDatabase = i;
					urlDatabase = properties[i].Substring(9);
				}
				else if (properties[i].StartsWith("server="))
				{
					idxServer = i;
					urlServer = properties[i].Substring(7);
				}
			}
			if (idxServer < 0 || idxDatabase < 0)
				return url;
			if ((String.IsNullOrEmpty(urlServer) || urlServer == server) && urlDatabase == project)
			{
				properties[idxServer] = "server=";
				properties[idxDatabase] = "database=this$";
				string fixedUrl = kFwUrlPrefix + HttpUtility.UrlEncode(properties.ToString("&"));
				return fixedUrl;
			}
			else
			{
				return url;
			}
		}
		#endregion
	}
	#endregion

	#region FwAppArgs class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Class representing the arguments necessary for starting up a FieldWorks application
	/// (from the command line or from a URI).
	/// See the class comment on FwLinkArgs for details on how all the parts of hyperlinking work.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[Serializable]
	public class FwAppArgs : FwLinkArgs
	{
		#region Command-line switch constants
		/// <summary>Command-line argument: Command-line usage help</summary>
		public const string kHelp = "help";
		/// <summary>Command-line argument: Culture abbreviation</summary>
		public const string kLocale = "locale";
		/// <summary>Command-line argument: The application to start (te or flex)</summary>
		public const string kApp = "app";
		/// <summary>Command-line argument: The project name (or file)</summary>
		public const string kProject = "db";
		/// <summary>URI argument: The project name (or file)</summary>
		public const string kProjectUri = "database";
		/// <summary>Command-line argument: The server name</summary>
		public const string kServer = "s";
		/// <summary>URI argument: The server name</summary>
		public const string kServerUri = "server";
		/// <summary>Command-line argument: The database type</summary>
		public const string kDbType = "type";
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
		private string m_database = string.Empty;
		private string m_server = string.Empty;
		private string m_appName = string.Empty;
		private string m_appAbbrev = string.Empty;
		private string m_dbType = string.Empty;
		private string m_locale = string.Empty;
		private string m_configFile = string.Empty;
		private string m_backupFile = string.Empty;
		private string m_restoreOptions = string.Empty;
		private string m_chooseProjectFile = string.Empty;
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the database/project name (possibly/probably a file path/name.
		/// Will never return null.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Database
		{
			get
			{
				Debug.Assert(m_database != null);
				return m_database;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the server name (for a remote project).
		/// Will never return null.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Server
		{
			get
			{
				Debug.Assert(m_server != null);
				return m_server;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the type of the database/backend. (e.g. XML, MySql, etc.) Will never return null.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string DatabaseType
		{
			get
			{
				Debug.Assert(m_dbType != null);
				return m_dbType;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the lowercase application name (either "translation editor" or "language
		/// explorer"). Will never return null.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string AppName
		{
			get
			{
				Debug.Assert(m_appName != null);
				return m_appName;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the application abbreviation (either "te" or "flex"). Will never return null.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string AppAbbrev
		{
			get
			{
				Debug.Assert(m_appAbbrev != null);
				return m_appAbbrev;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the locale for the user interface.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Locale
		{
			get
			{
				Debug.Assert(m_locale != null);
				return m_locale;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Flag indicating whether to show help (if true, all other arguments can be ignored)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ShowHelp { get; private set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Flag indicating whether or not to hide the UI.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool NoUserInterface { get; private set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Flag indicating whether or not to run in a server mode for other applications.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool AppServerMode { get; private set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the config file. Will never return null.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ConfigFile
		{
			get
			{
				Debug.Assert(m_configFile != null);
				return m_configFile;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the backup file used for a restore. Will never return null.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string BackupFile
		{
			get
			{
				Debug.Assert(m_backupFile != null);
				return m_backupFile;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the restore options user chose for creating RestoreProjectSettings. Will
		/// never return null.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string RestoreOptions
		{
			get
			{
				Debug.Assert(m_restoreOptions != null);
				return m_restoreOptions;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this FwAppArgs also contains information pertaining
		/// to a link request.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool HasLinkInformation
		{
			get
			{
				bool hasInfo = !string.IsNullOrEmpty(ToolName);
				return hasInfo;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the full path of the file that the handle for a chosen project will be
		/// written to.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ChooseProjectFile
		{
			get { return m_chooseProjectFile; }
		}
		#endregion

		#region Constructor
		// NOTE: Don't make constructor overloads that take only varying numbers of string
		// parameters. That would conflict with the string params constructor causing a bunch
		// of stuff to break (which, thankfully, we have tests for)

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FwAppArgs"/> class.
		/// </summary>
		/// <param name="applicationNameOrAbbrev">Name or abbreviation of the application.</param>
		/// <param name="database">The name of the database.</param>
		/// <param name="server">The server (or null for a local database).</param>
		/// <param name="toolName">Name/path of the tool or view within the specific application.</param>
		/// <param name="targetGuid">The GUID of the object which is the target of this link.</param>
		/// ------------------------------------------------------------------------------------
		public FwAppArgs(string applicationNameOrAbbrev, string database, string server,
			string toolName, Guid targetGuid) : base(toolName, targetGuid)
		{
			ProcessArg(kApp, applicationNameOrAbbrev);
			ProcessArg(kProject, database);
			if (!string.IsNullOrEmpty(server))
				ProcessArg(kServer, server);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FwAppArgs"/> class.
		/// </summary>
		/// <param name="rgArgs">The command-line arguments.</param>
		/// ------------------------------------------------------------------------------------
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
				return; // Not much we can do

			if (commandLineArgs.ContainsKey(kLink))
			{
				if (!InitializeFromUrl(commandLineArgs[kLink]))
					ShowHelp = true;
			}
			else
			{
				foreach (var kvp in commandLineArgs)
					ProcessArg(kvp.Key, kvp.Value);
			}
		}
		#endregion

		#region Overridden methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the properties as named arguments in a format that can be used to produce a
		/// URI query.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void AddProperties(StringBuilder bldr)
		{
			bldr.AppendFormat("{0}={1}&{2}={3}&{4}={5}&", kApp, AppAbbrev, kProjectUri,
				Database, kServerUri, Server);
			base.AddProperties(bldr);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Some comparisons don't care about the content of the property table, so we provide a
		/// method similar to Equals but not quite as demanding.
		/// </summary>
		/// <param name="lnk">The link to compare.</param>
		/// ------------------------------------------------------------------------------------
		public override bool EssentiallyEquals(FwLinkArgs lnk)
		{
			FwAppArgs appArgs = lnk as FwAppArgs;
			if (appArgs == null || !base.EssentiallyEquals(lnk))
				return false;
			return (appArgs.AppAbbrev == AppAbbrev && appArgs.Database == Database &&
				appArgs.Server == Server);
		}
		#endregion

		#region Command-line Handling Methods
		/// -----------------------------------------------------------------------------------
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
		///	-----------------------------------------------------------------------------------
		private static Dictionary<string, string> ParseCommandLine(string[] rgArgs)
		{
			Dictionary<string, string> dictArgs = new Dictionary<string, string>();
			if (rgArgs == null || rgArgs.Length == 0)
				return dictArgs;

			string sKey = string.Empty;
			string value = string.Empty;

			foreach (string sArg in rgArgs)
			{
				if (string.IsNullOrEmpty(sArg))
					continue;

				int iCurrChar = 0;
#if __MonoCS__
				if (sArg[iCurrChar] == '-') // Start of option
				// Linux absolute paths begin with a slash
#else
				if (sArg[iCurrChar] == '-' || sArg[iCurrChar] == '/') // Start of option
#endif
				{
					// Start of a new argument key
					if (!String.IsNullOrEmpty(value))
					{
						// Save the previous values to the previous key
						if (dictArgs.ContainsKey(sKey))
							throw new ArgumentException(sKey + " was passed in more then once");
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
					string newValue = sArg.Substring(iCurrChar);
					if (!string.IsNullOrEmpty(value))
						throw new ArgumentException(newValue + " added as second value for " + sKey);
					value = newValue;
					// There may not be a key, if this is the first argument, as when opening a file directly.
					if (sKey.Length == 0)
					{
						sKey = (Path.GetExtension(value) == FdoFileHelper.ksFwBackupFileExtension) ? kRestoreFile : kProject;
					}
				}
			}

			// Save final tag.
			if (sKey.Length > 0)
				dictArgs.Add(sKey, value);

			return dictArgs;
		}

		/// -----------------------------------------------------------------------------------
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
		/// -----------------------------------------------------------------------------------
		private static string CommandLineSwitch(string sArg, ref int iCurrChar)
		{
			if (FindCommandLineArgument(sArg, ref iCurrChar, kAppServerMode))
				return kAppServerMode;
			if (FindCommandLineArgument(sArg, ref iCurrChar, kApp))
				return kApp;
			if (FindCommandLineArgument(sArg, ref iCurrChar, kProject)) // (old) project name
				return kProject;
			if (FindCommandLineArgument(sArg, ref iCurrChar, "proj")) // project name
				return kProject;
			if (FindCommandLineArgument(sArg, ref iCurrChar, kServer)) // server name
				return kServer;
			if (FindCommandLineArgument(sArg, ref iCurrChar, kDbType)) // database (BEP) type
				return kDbType;
			if (FindCommandLineArgument(sArg, ref iCurrChar, kHelp))
				return kHelp;
			if (FindCommandLineArgument(sArg, ref iCurrChar, kChooseProject))
				return kChooseProject;
			if (FindCommandLineArgument(sArg, ref iCurrChar, kLink))
				return kLink;
			if (FindCommandLineArgument(sArg, ref iCurrChar, kLocale))
				return kLocale;
			if (FindCommandLineArgument(sArg, ref iCurrChar, kRestoreFile))
				return kRestoreFile;
			if (FindCommandLineArgument(sArg, ref iCurrChar, kRestoreOptions))
				return kRestoreOptions;
			if (FindCommandLineArgument(sArg, ref iCurrChar, kNoUserInterface))
				return kNoUserInterface;

			if (sArg.Length > iCurrChar)
			{
				// It is a single character tag.
				string sKey = sArg.Substring(iCurrChar, 1).ToLowerInvariant();
				if (sKey == "?" || sKey == "h")	// Variants of help.
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the specified argument key in the specified argument string.
		/// </summary>
		/// <param name="sArg">The argument string</param>
		/// <param name="iCurrChar">Index of the current character to start looking.</param>
		/// <param name="key">The argument to look for</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
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
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clears any link information from this FwAppArgs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ClearLinkInformation()
		{
			m_tag = string.Empty;
			m_toolName = string.Empty;
			TargetGuid = Guid.Empty;
		}
		#endregion

		#region Private helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes from the given URL.
		/// </summary>
		/// <param name="url">The URL.</param>
		/// <returns><c>true</c> if successfully initialized from the URL; <c>false</c>
		/// otherwise</returns>
		/// ------------------------------------------------------------------------------------
		private bool InitializeFromUrl(string url)
		{
			try
			{
				if (!url.StartsWith(kFwUrlPrefix))
					throw new UriFormatException("FieldWorks link must begin with " + kFwUrlPrefix);

				Uri destination = new Uri(url);
				string decodedQuery = HttpUtility.UrlDecode(destination.Query);
				foreach (string pair in decodedQuery.Split('?', '&'))
				{
					int i = pair.IndexOf("=");
					if (i < 0)
						continue;
					string name = pair.Substring(0, i);
					string value = pair.Substring(i + 1);
					if (name == kServerUri && value.LastIndexOf('\\') >= 0)
					{
						// Attempt to handle old SQL Server links
						value = value.Substring(0, value.LastIndexOf('\\'));
						if (value == ".") // Old SQL Server on local host
							value = string.Empty;
					}
					ProcessArg(name, value);
				}

				if (String.IsNullOrEmpty(AppName))
					throw new UriFormatException("FieldWorks link must include an application identifier.");
				if (String.IsNullOrEmpty(Database))
					throw new UriFormatException("FieldWorks link must include a project name.");
			}
			catch (UriFormatException e)
			{
				Logger.WriteError(new Exception("Invalid link passed on FieldWorks command line: " + url, e));
				return false;
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes a URL or command-line argument represented by the given name and value pair.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="value">The value.</param>
		/// ------------------------------------------------------------------------------------
		private void ProcessArg(string name, string value)
		{
			Debug.Assert(value != null || name == kHelp);
			switch (name)
			{
				case kProject:
				case kProjectUri: m_database = value; break;
				case "c": // For historical purposes (even though it will probably never work)
				case kServer:
				case kServerUri: m_server = value; break;
				case kApp: SetAppNameAndAbbrev(value); break;
				case kDbType: m_dbType = value; break;
				case kLocale: m_locale = value; break;
				case kHelp: ShowHelp = true; break;
				case kChooseProject: m_chooseProjectFile = value; break;
				case kFlexConfigFile: m_configFile = value; break;
				case kRestoreFile: m_backupFile = value; break;
				case kRestoreOptions: m_restoreOptions = value; break;
				case kNoUserInterface: NoUserInterface = true; break;
				case kAppServerMode: AppServerMode = true; break;
				case kTag: m_tag = value; break;
				case kTool: m_toolName = value; break;
				case kGuid:
					if (value != "null")
						TargetGuid = new Guid(value);
					break;
				default:
					PropertyTableEntries.Add(new Property(name, Decode(value)));
					break;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the application name and abbreviation.
		/// </summary>
		/// <param name="value">The URL or command-line parameter value that is supposed to
		/// match one of the know FW application names or abbreviations.</param>
		/// ------------------------------------------------------------------------------------
		private void SetAppNameAndAbbrev(string value)
		{
			string appNameOrAbbrev = value.ToLowerInvariant();
			if (appNameOrAbbrev == FwUtils.ksTeAppName.ToLowerInvariant() ||
				appNameOrAbbrev == FwUtils.ksTeAbbrev.ToLowerInvariant())
			{
				m_appName = FwUtils.ksTeAppName.ToLowerInvariant();
				m_appAbbrev = FwUtils.ksTeAbbrev.ToLowerInvariant();
			}
			else if (appNameOrAbbrev == FwUtils.ksFlexAppName.ToLowerInvariant() ||
				appNameOrAbbrev == FwUtils.ksFlexAbbrev.ToLowerInvariant())
			{
				m_appName = FwUtils.ksFlexAppName.ToLowerInvariant();
				m_appAbbrev = FwUtils.ksFlexAbbrev.ToLowerInvariant();
			}
			else
				ShowHelp = true;
		}
		#endregion
	}
	#endregion
}

// Copyright (c) 2010-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Text;
using System.Web;

namespace SIL.FieldWorks.Common.FwUtils
{
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
		/// <summary>Command-line argument: LCM object guid for a FieldWorks link</summary>
		public const string kGuid = "guid";
		/// <summary>Command-line argument: LCM object field tag for a FieldWorks link</summary>
		public const string kTag = "tag";
		/// <summary>Fieldworks link prefix</summary>
		public const string kFwUrlPrefix = kSilScheme + "://" + kLocalHost + "/" + kLink + "?";
		#endregion

		#region Member variables
		/// <summary />
		protected string m_toolName = string.Empty;
		#endregion

		#region Properties
		/// <summary>
		/// The name/path of the tool or view within the specific application. Will never be null.
		/// </summary>
		public string ToolName => m_toolName ?? string.Empty;

		/// <summary>
		/// The GUID of the object which is the target of this link.
		/// </summary>
		public Guid TargetGuid { get; protected set; }

		/// <summary>
		/// Properties used by the link
		/// </summary>
		public List<LinkProperty> LinkProperties { get; } = new List<LinkProperty>();

		/// <summary>
		/// An additional tag to differentiate between other FwLinkArgs entries between the
		/// same core ApplicationName, Database, Guid values. Will never be null.
		/// (cf. LT-7847)
		/// </summary>
		public string Tag { get; protected set; } = string.Empty;
		#endregion  Properties

		#region Construction and Initialization

		/// <summary />
		protected FwLinkArgs()
		{
		}

		/// <summary />
		/// <param name="toolName">Name/path of the tool or view within the specific application.</param>
		/// <param name="targetGuid">The GUID of the object which is the target of this link.</param>
		/// <param name="tag">The tag.</param>
		public FwLinkArgs(string toolName, Guid targetGuid, string tag = null) : this()
		{
			m_toolName = toolName;
			TargetGuid = targetGuid;
			Tag = tag ?? string.Empty;
		}

		/// <summary />
		/// <param name="url">a URL string like that produced by ToString().</param>
		public FwLinkArgs(string url)
		{
			if (!url.StartsWith(kFwUrlPrefix))
			{
				throw new ArgumentException($"unrecognized FwLinkArgs URL string: {url}");
			}
			var query = HttpUtility.UrlDecode(url.Substring(23));
			var rgsProps = query.Split('&');
			foreach (var prop in rgsProps)
			{
				var propPair = prop.Split('=');
				if (propPair.Length != 2)
				{
					throw new ArgumentException($"invalid FwLinkArgs URL string: {url}");
				}
				switch (propPair[0])
				{
					case kTool:
						m_toolName = propPair[1];
						break;
					case kGuid:
						TargetGuid = new Guid(propPair[1]);
						break;
					case kTag:
						Tag = propPair[1];
						break;
					default:
						LinkProperties.Add(new LinkProperty(propPair[0], propPair[1]));
						break;
				}
			}
			if (string.IsNullOrEmpty(m_toolName) || TargetGuid == Guid.Empty || Tag == null)
			{
				throw new ArgumentException($"invalid FwLinkArgs URL string: {url}");
			}
		}

		/// <summary>
		/// Copy the link-related information to a new FwLinkArgs. Currently this does NOT
		/// yield an FwAppArgs even if the recipient is one.
		/// </summary>
		public FwLinkArgs CopyLinkArgs()
		{
			var result = new FwLinkArgs(m_toolName, TargetGuid, Tag);
			result.LinkProperties.AddRange(LinkProperties);
			return result;
		}
		#endregion

		#region overridden methods and helpers
		/// <summary>
		/// Returns a hash code for this instance.
		/// </summary>
		public override int GetHashCode()
		{
			return ToString().GetHashCode();
		}

		/// <summary>
		/// Some comparisons don't care about the content of the property table, so we provide a
		/// method similar to Equals but not quite as demanding.
		/// </summary>
		/// <param name="lnk">The link to compare.</param>
		public virtual bool EssentiallyEquals(FwLinkArgs lnk)
		{
			if (lnk == null)
			{
				return false;
			}
			if (lnk == this)
			{
				return true;
			}
			if (lnk.ToolName != ToolName)
			{
				return false;
			}
			if (lnk.TargetGuid != TargetGuid)
			{
				return false;
			}
			// tag is optional, but if a tool uses it with content, it should consistently provide content.
			// therefore if one of these links does not have content, and the other does then
			// we'll assume they refer to the same link
			// (one link simply comes from a control with more knowledge then the other one)
			return lnk.Tag.Length == 0 || Tag.Length == 0 || lnk.Tag == Tag;
		}

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
		public override bool Equals(object obj)
		{
			var link = obj as FwLinkArgs;
			if (link == null)
			{
				return false;
			}
			if (link == this)
			{
				return true;
			}
			//just compare the URLs
			return (ToString() == link.ToString());
		}

		/// <summary>
		/// Get a URL corresponding to this link
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String"/> that represents this instance.
		/// </returns>
		public override string ToString()
		{
			var uriBuilder = new UriBuilder(kSilScheme, kLocalHost)
			{
				Path = kLink
			};
			var query = new StringBuilder();
			AddProperties(query);

			foreach (var property in LinkProperties)
			{
				query.AppendFormat("&{0}={1}", property.Name, Encode(property.Value));
			}

			//make it safe to represent as a url string (e.g., convert spaces)
			uriBuilder.Query = HttpUtility.UrlEncode(query.ToString());

			return uriBuilder.Uri.AbsoluteUri;
		}

		/// <summary>
		/// Adds the properties as named arguments in a format that can be used to produce a
		/// URI query.
		/// </summary>
		protected virtual void AddProperties(StringBuilder bldr)
		{
			bldr.AppendFormat("{0}={1}&{2}={3}&{4}={5}", kTool, ToolName, kGuid, (TargetGuid == Guid.Empty) ? "null" : TargetGuid.ToString(), kTag, Tag);
		}
		#endregion

		#region Serialization
		/// <summary>
		/// Add type info to the parameter if it's not a string
		/// </summary>
		/// <param name="o">The o.</param>
		protected string Encode(object o)
		{
			switch (o.GetType().ToString())
			{
				default: throw new ArgumentException("Don't know how to serialize type of " + o.GetType());
				case "System.Boolean":
					return "bool:" + o;
				case "System.String":
					return (string)o;
			}
		}

		/// <summary>
		/// Use the explicit type tag to parse parameters into the right type
		/// </summary>
		/// <param name="value">The value.</param>
		protected object Decode(string value)
		{
			if (value.IndexOf("bool:") > -1)
			{
				value = value.Substring(5);
				return bool.Parse(value);
			}
			return HttpUtility.UrlDecode(value);
		}
		#endregion

		#region Static utility methods

		/// <summary>
		/// If  the database= and server= sections of the url string match up against the
		/// current project and server, set the server to empty (if it isn't already), and set
		/// the project to "this$".  This allows the project to be renamed without invalidating
		/// any internal crossreferences in the form of hyperlinks.  (See FWR-3437.)
		/// </summary>
		public static string FixSilfwUrlForCurrentProject(string url, string project)
		{
			if (!url.StartsWith(kFwUrlPrefix))
			{
				return url;
			}
			var query = HttpUtility.UrlDecode(url.Substring(kFwUrlPrefix.Length));
			var properties = query.Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
			var idxDatabase = -1;
			string urlDatabase = null;
			for (var i = 0; i < properties.Length; ++i)
			{
				if (properties[i].StartsWith("database="))
				{
					idxDatabase = i;
					urlDatabase = properties[i].Substring(9);
				}
			}
			if (idxDatabase < 0)
			{
				return url;
			}
			if (urlDatabase == project)
			{
				properties[idxDatabase] = "database=this$";
				var fixedUrl = kFwUrlPrefix + HttpUtility.UrlEncode(string.Join("&", properties));
				return fixedUrl;
			}
			return url;
		}
		#endregion
	}
}
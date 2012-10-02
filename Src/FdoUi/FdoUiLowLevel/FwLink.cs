// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FwLinkLink.cs
// Last reviewed:
//
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Reflection;
using System.IO;
using System.Web;
using System.Xml;
using System.Xml.Serialization;
using SIL.FieldWorks.Common.Utils;
using XCore;

namespace SIL.FieldWorks.FdoUi
{
	/// <summary>
	/// provides a message object specifically for asking FieldWorks applications
	/// (whether the current one or another one) to do various navigation activities.
	/// </summary>
	///
	[Serializable]
	public class FwLink
	{
		#region Data member

		protected string m_server;
		protected string m_database;
		protected string m_appName;
		protected string m_toolName;
		protected Guid m_targetGuid = Guid.Empty;
		protected List<Property> m_propertyTableEntries = new List<Property>();
		protected string m_tag = String.Empty;

		#endregion Data member

		#region Properties

		public string Server
		{
			get
			{
				return m_server;
			}
			set
			{
				m_server = value;
				//don't save the machine name if it's local. This will allow the db to be
				//opened on another machine w/out the links failing.
				if (m_server=="" || MiscUtils.IsServerLocal(m_server))
					m_server = @".\SILFW";
			}
		}

		public string Database
		{
			get { return m_database; }
		}

		public string ApplicationName
		{
			get { return m_appName; }
		}

		public string ToolName
		{
			get { return m_toolName; }
		}

		public Guid TargetGuid
		{
			get { return m_targetGuid; }
		}

		public List<Property> PropertyTableEntries
		{
			get { return m_propertyTableEntries; }
		}

		/// <summary>
		/// An additional tag to differentiate between other
		/// FwLink entries between the same core ApplicationName, Server, Database, Guid values.
		/// (cf. LT-7847)
		/// </summary>
		public string Tag
		{
			get { return m_tag; }
		}

		#endregion  Properties

		#region Construction and Initialization

		/// <summary>
		/// constructs a link based on a URL
		/// </summary>
		/// <param name="appName"></param>
		public FwLink(string url)
		{
			m_server = @".\SILFW";
			//url = HttpUtility.UrlDecode(url);
			Uri destination = new Uri(url);
			string decodedQuery = HttpUtility.UrlDecode(destination.Query);
			foreach(string pair in decodedQuery.Split(new char[]{'?','&'}))
			{
				int i = pair.IndexOf("=");
				if (i < 0)
					continue;
				string name = pair.Substring(0, i);
				string value = pair.Substring(i + 1);
				switch (name)
				{
					default:
						m_propertyTableEntries.Add(new Property(name, Decode(value)));
						break;
					case "tag":
						m_tag = value;
						break;
					case "app":
						m_appName = value;
						break;
					case "guid":
						if (value != "null")
							m_targetGuid = new Guid(value);
						break;
					case "hvo":
						Debug.Assert(false, "HVO usage is deprecated.");
						break;
					case "tool":
						m_toolName = value;
						break;
					case "server":
						Server = value;
						break;
					case "database":
						m_database = value;
						break;
				}
			}
		}

		/// <summary>
		/// constructs a link which just send you to the named application
		/// </summary>
		/// <param name="appName"></param>
		public FwLink(string application, string tool, Guid targetGuid, string server, string database)
		{
			m_appName = application;
			m_toolName = tool;
			m_targetGuid = targetGuid;
			Server = server;
			m_database = database;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="application"></param>
		/// <param name="tool"></param>
		/// <param name="targetGuid"></param>
		/// <param name="server"></param>
		/// <param name="database"></param>
		/// <param name="tag">a key that differentiates this link from others with the same core key values</param>
		public FwLink(string application, string tool, Guid targetGuid, string server, string database, string tag)
			: this(application, tool, targetGuid, server, database)
		{
			m_tag = tag;
		}

		/// <summary>
		/// Link that only works from within this app
		/// </summary>
		/// <param name="toolName"></param>
		/// <param name="targetGuid"></param>
		/// <param name="server"></param>
		/// <param name="database"></param>
		/// <returns></returns>
		public static FwLink Create(string toolName, Guid targetGuid, string server, string database)
		{
			return Create(toolName,targetGuid,server,database,"");
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="tag"></param>
		/// <returns></returns>
		public static FwLink Create(string toolName, Guid targetGuid, string server, string database, string tag)
		{
			return Create(System.Windows.Forms.Application.ProductName, toolName, targetGuid, server, database, tag);
		}

		/// <summary>
		/// Link that works across apps
		/// </summary>
		/// <param name="toolName"></param>
		/// <param name="targetGuid"></param>
		/// <param name="server"></param>
		/// <param name="database"></param>
		/// <returns></returns>
		public static FwLink Create(string appName, string toolName, Guid targetGuid, string server, string database)
		{
			return Create(appName, toolName, targetGuid, server, database, "");
		}

		public static FwLink Create(string appName, string toolName, Guid targetGuid, string server, string database, string tag)
		{
			return new FwLink(appName, toolName, targetGuid, server, database, tag);
		}

		#endregion Construction and Initialization


		public override int GetHashCode()
		{
			return ToString().GetHashCode();
		}

		/// <summary>
		/// Some comparisons don't care about the content of the property table, so we provide a method
		/// similar to Equals but not quite as demanding.
		/// </summary>
		/// <param name="lnk"></param>
		/// <returns></returns>
		public bool EssentiallyEquals(FwLink lnk)
		{
			if (lnk == null)
				return false;
			if (lnk == this)
				return true;
			if (lnk.m_appName != this.m_appName)
				return false;
			if (lnk.m_toolName != this.m_toolName)
				return false;
			if (lnk.m_server != this.m_server)
				return false;
			if (lnk.m_database != this.m_database)
				return false;
			if (lnk.m_targetGuid != this.m_targetGuid)
				return false;
			// tag is optional, but if a tool uses it with content, it should consistently provide content.
			// therefore if one of these links does not have content, and the other does then
			// we'll assume they refer to the same link
			// (one link simply comes from a control with more knowledge then the other one)
			return lnk.m_tag.Length == 0 || this.m_tag.Length == 0 || lnk.m_tag == this.m_tag;
		}

		public override bool Equals(object obj)
		{
			FwLink link = obj as FwLink;
			if (link == null)
				return false;
			if (link == this)
				return true;
			//just compare the URLs
			return (ToString() == link.ToString());
		}

		/// <summary>
		/// Get a URL corresponding to this link
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			UriBuilder uriBuilder = new UriBuilder("silfw", "localhost");
			StringBuilder query = new StringBuilder();
			uriBuilder.Path = "link";
			query.AppendFormat("app={0}&tool={1}&guid={2}&server={3}&database={4}&tag={5}",
				m_appName, // 0
				m_toolName, // 1
				(m_targetGuid == Guid.Empty) ? "null" : m_targetGuid.ToString(), // 2
				Server, // 3
				m_database,// 4
				m_tag); // 5


			foreach (Property property in m_propertyTableEntries)
			{
				query.AppendFormat("&{0}={1}",
					property.name, // 0
					Encode(property.value)); // 1
			}

			//make it safe to represent as a url string (e.g., convert spaces)
			uriBuilder.Query = HttpUtility.UrlEncode(query.ToString());

			return uriBuilder.Uri.AbsoluteUri;
		}

		/// <summary>
		/// Add type info to the parameter if it's not a string
		/// </summary>
		/// <param name="o"></param>
		/// <returns></returns>
		string Encode(object o)
		{
			switch(o.GetType().ToString())
			{
				default: throw new ArgumentException("Don't know how to serialize type of " + o.ToString());
				case "System.Boolean":
					return "bool:"+o.ToString();
				case "System.String":
					return (String)o;
			}
		}

		/// <summary>
		/// Use the explicit type tag to parse parameters into the right type
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		object Decode(string value)
		{
			if(value.IndexOf("bool:") > -1)
			{
				value = value.Substring(5);
				return bool.Parse(value);
			}
			return HttpUtility.UrlDecode(value);
		}

		/// <summary>
		///  Attempts to find or launch application which matches the url, and passes the link to that application.
		/// </summary>
		/// <param name="url"></param>
		/// <returns></returns>
		public static bool Activate(string url)
		{
			FwLink link = new FwLink(url);
			return link.Activate();
		}

		/// <summary>
		/// Attempts to find or launch application which matches this link, and passes the link to that application.
		/// </summary>
		/// <param name="link"></param>
		/// <returns>true if it successfully linked to a running application</returns>
		public bool Activate()
		{
			if (System.Windows.Forms.Application.ProductName == m_appName)
				throw new Exception(
					"Sorry, cannot call FwLink.Activate to a link within the same application.");
			if (ActivateExisting())
				return true;
			//launch the application
			LaunchTarget();
			return true;
		}

		/// <summary>
		/// Attempts to find an application which matches the link, and passes the link to that application.
		/// Does not attempt to launch new applications.
		/// </summary>
		/// <param name="link"></param>
		/// <returns>true if it successfully linked to a running application</returns>
		public bool ActivateExisting()
		{
			try
			{
				int port = 6000;
				//note that this will not fail, even if no one is listening on that channel
				FwLinkReceiver receiver =
					(FwLinkReceiver)Activator.GetObject(typeof(FwLinkReceiver),
						GetPortPath(port, ApplicationName));
				//but this test will fail if we did not really connect to a receiver
				receiver.Request(this);
				return true;
			}
			catch(Exception)
			{
				//nb: at the moment, we just assume that the failure was caused by the target application not running yet
			}
			return false;
		}

		/// <summary>
		/// the target does not seem to be running, so try and launch it
		/// </summary>
		protected void LaunchTarget()
		{
			Debug.Assert(m_server != null && m_server.Length >0);
			Debug.Assert(m_database != null && m_database.Length >0);

			Process process = new Process();
			process.StartInfo.Arguments = String.Format("-c \"{0}\" -db \"{1}\" -link \"{2}\"",
				Server.Replace(".", Environment.MachineName), // 0
				m_database, // 1
				ToString()); // 2
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.WorkingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase).Substring(6);
			process.StartInfo.FileName = "Flex.exe";
			try
			{
				process.Start();
			}
			catch (Exception)
			{
				System.Windows.Forms.MessageBox.Show(Strings.ksFlexNotInstalled, Strings.ksError,
					System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
			}
		}

		static  public string GetPortPath(int port, string appName)
		{
			return "tcp://localhost:" + port.ToString() + "/" + appName + "/receiver";
		}
	}
/*
 *
 * This is another encoding strategy example: convert to xml using the xmlseriarlizer, then convert that to a url. Should work, in principle.
 *
	public class UrlSerializer
	{
		public static object Deserialize(Type type, string url)
		{
			return Deserialize(type, new Uri(url));
		}

		public static object Deserialize(Type type, Uri url)
		{
			string ns = getNamespace(type);
			string typeName = type.Name;
			XmlSerializer ser = new XmlSerializer(type);
			StringBuilder xmlBuilder = new StringBuilder();
			StringWriter writer = new StringWriter();
			XmlTextWriter xmlWriter = new XmlTextWriter(writer);
			xmlWriter.Formatting = Formatting.Indented;
			xmlWriter.WriteStartElement(typeName, ns);
			string query = url.Query;
			string[] pairs = query.Split(new char[] {'?','&'});
			foreach(string pair in pairs)
			{
				string[] nv = pair.Split(new char[] {'='});
				if(nv.Length == 2)
				{
					string name = nv[0];
					string val = HttpUtility.UrlDecode(nv[1]);
					xmlWriter.WriteElementString(name, ns, val);
				}
			}
			xmlWriter.WriteEndElement();
			string xml = writer.ToString();
			StringReader reader = new StringReader(xml);
			return ser.Deserialize(reader);
		}

		public static string Serialize(object ob, string applicationPath)
		{
			XmlSerializer ser =  new XmlSerializer(ob.GetType());
			StringWriter writer = new StringWriter();
			XmlTextWriter xmlWriter = new XmlTextWriter(writer);
			xmlWriter.Formatting = Formatting.Indented;
			ser.Serialize(writer, ob);
			string xml = writer.ToString();
			XmlDocument doc = new XmlDocument();
			doc.InnerXml = xml;
			string ns = getNamespace(ob.GetType());
			//			if(applicationPath != null)
			//			{
			//				ns = ns.Replace("~", applicationPath.ToString());
			//			}

			//			UriBuilder builder = new UriBuilder(ns);
			StringBuilder query = new StringBuilder();
			foreach(XmlNode node in doc.DocumentElement.ChildNodes)
			{
				XmlElement element = node as XmlElement;
				if(element != null)
				{
					if(query.Length != 0) query.Append("&");
					string name = element.Name;
					string val = element.InnerText;;
					query.Append(name);
					query.Append("=");
					query.Append(HttpUtility.UrlEncode(val));
				}
			}
			//			builder.Query = query.ToString();
			//			return builder.Uri;

			if(!applicationPath .EndsWith("/")) applicationPath += "/";
			return applicationPath + ns + "?" + query;
		}

		private static string getNamespace(Type type)
		{
			object[] roots = type.GetCustomAttributes(typeof(XmlRootAttribute), true);
			if(roots.Length > 0)
			{
				XmlRootAttribute root = (XmlRootAttribute)roots[0];
				return root.Namespace;
			}
			else { return null; }
		}

	}*/
}

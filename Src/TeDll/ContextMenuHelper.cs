// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2008, SIL International. All Rights Reserved.
// <copyright from='2008' to='2008' company='SIL International'>
//		Copyright (c) 2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ContextMenuHelper.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// Implements a way to display context menus without using DotNetBar.  Currently duplicates a
// lot of initialization code found in the DotNetBar adapter code.  This work was motivated by
// TE-6901.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Windows.Forms;
using System.Xml;

using SIL.Utils;
using System.IO;
using XCore;
using System.Drawing;

namespace SIL.FieldWorks.TE
{
	/// <summary>
	/// This class provides methods to initialize context menus from a pair of XML configuration
	/// files, and to create context menus on demand given a name matching one found in those
	/// configuration files.
	/// </summary>
	class ContextMenuHelper
	{
		/// <summary>
		/// This stores resource managers defined in the configuration files.
		/// </summary>
		List<ResourceManager> m_rmlocalStrings = new List<ResourceManager>();

		/// <summary>
		/// This stores lists of ToolStripMenuItem objects for each context menu defined in the
		/// configuration files.  We don't store complete menus because the displayed menus may
		/// have spelling correction items added to them, so we create the actual ToolStripMenu
		/// objects on demand.
		/// </summary>
		Dictionary<string, List<ToolStripItem>> m_dictContextMenus = new Dictionary<string, List<ToolStripItem>>();

		/// <summary>
		/// This stores a menu item for each command actually used by a context menu.
		/// </summary>
		Dictionary<string, ToolStripMenuItem> m_dictMenuItems = new Dictionary<string, ToolStripMenuItem>();

		Dictionary<string, Image> m_dictImages = new Dictionary<string, Image>();
		/// <summary>
		/// This stores a back reference to the owner of this ContextMenuHelper.
		/// </summary>
		TeMainWnd m_mainWnd;

		/// <summary>
		/// Constructor.
		/// </summary>
		public ContextMenuHelper(TeMainWnd wnd)
		{
			m_mainWnd = wnd;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create the named context menu and return it to the caller.  This doesn't show the
		/// menu, it merely fills it in.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public ContextMenuStrip CreateContextMenu(string name)
		{
			ContextMenuStrip menu = new ContextMenuStrip();
			if (String.IsNullOrEmpty(name))
				return menu;
			List<ToolStripItem> list;
			if (m_dictContextMenus.TryGetValue(name, out list))
			{
				foreach (ToolStripItem item in list)
					menu.Items.Add(item);
			}
			return menu;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the set of context menus found in the two XML configuration files.
		/// </summary>
		/// <param name="generalFile">FieldWorks general configuration file</param>
		/// <param name="specificFile">application specific configuration file</param>
		/// ------------------------------------------------------------------------------------
		public void InitializeContextMenus(string generalFile, string specificFile)
		{
			XmlDocument xdocGeneral = new XmlDocument();
			xdocGeneral.PreserveWhitespace = false;
			xdocGeneral.Load(generalFile);

			XmlDocument xdocSpecific = new XmlDocument();
			xdocSpecific.PreserveWhitespace = false;
			xdocSpecific.Load(specificFile);

			ResourceManager rm = GetResourceMngr(xdocGeneral.SelectSingleNode("TMDef/resources/localizedstrings"));
			if (rm != null)
				m_rmlocalStrings.Add(rm);
			rm = GetResourceMngr(xdocSpecific.SelectSingleNode("TMDef/resources/localizedstrings"));
			if (rm != null)
				m_rmlocalStrings.Add(rm);
			ReadImagesResources(xdocGeneral.SelectSingleNode("TMDef/resources/imageList"));
			ReadImagesResources(xdocSpecific.SelectSingleNode("TMDef/resources/imageList"));

			foreach (XmlNode xnMenu in xdocGeneral.SelectNodes("TMDef/contextmenus/contextmenu"))
				InitializeContextMenu(xdocGeneral, xdocSpecific, xnMenu);
			foreach (XmlNode xnMenu in xdocSpecific.SelectNodes("TMDef/contextmenus/contextmenu"))
				InitializeContextMenu(xdocGeneral, xdocSpecific, xnMenu);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the specific context menu defined by xnMenu.
		/// </summary>
		/// <param name="xdocGeneral"></param>
		/// <param name="xdocSpecific"></param>
		/// <param name="xnMenu"></param>
		/// ------------------------------------------------------------------------------------
		private void InitializeContextMenu(XmlDocument xdocGeneral, XmlDocument xdocSpecific, XmlNode xnMenu)
		{
			string sMenuName = XmlUtils.GetAttributeValue(xnMenu, "name");
			if (String.IsNullOrEmpty(sMenuName))
				return;
			List<ToolStripItem> mnucmds = new List<ToolStripItem>();
			foreach (XmlNode xnItem in xnMenu.SelectNodes("item"))
			{
				string sName = XmlUtils.GetAttributeValue(xnItem, "name");
				string sCmdId = XmlUtils.GetAttributeValue(xnItem, "commandid");
				if (String.IsNullOrEmpty(sName) || String.IsNullOrEmpty(sCmdId))
					continue;
				bool fBeginGroup = XmlUtils.GetOptionalBooleanAttributeValue(xnItem, "begingroup", false);
				string xpath = String.Format("TMDef/commands/command[@id='{0}']", sCmdId);
				XmlNode xnCmd = xdocGeneral.SelectSingleNode(xpath);
				if (xnCmd == null)
					xnCmd = xdocSpecific.SelectSingleNode(xpath);
				if (xnCmd == null)
					continue;
				if (fBeginGroup)
					mnucmds.Add(new ToolStripSeparator());
				ToolStripMenuItem cmd = CreateContextMenuItem(xnCmd);
				if (cmd != null)
					mnucmds.Add(cmd);
			}
			if (mnucmds.Count > 0)
				m_dictContextMenus.Add(sMenuName, mnucmds);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find or create a single context menu item based on the contents of xnCmd.
		/// </summary>
		/// <param name="xnCmd"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private ToolStripMenuItem CreateContextMenuItem(XmlNode xnCmd)
		{
			string cmd = XmlUtils.GetAttributeValue(xnCmd, "id");
			if (cmd == null)
				return null;
			ToolStripMenuItem item;
			if (m_dictMenuItems.TryGetValue(cmd, out item))
				return item;

			string sText = GetResourceString(xnCmd, "text");
			string sContextMenuText = GetResourceString(xnCmd, "contextmenutext");
			if (!string.IsNullOrEmpty(sContextMenuText))
				sText = sContextMenuText;

			string sToolTip = GetResourceString(xnCmd, "tooltip");
			string sStatusMsg = GetResourceString(xnCmd, "statusmsg");
			if (String.IsNullOrEmpty(sStatusMsg))
				sStatusMsg = GetStringFromResource("kstidDefaultStatusBarMsg");

			string sMessage = XmlUtils.GetAttributeValue(xnCmd, "message");
			string shortcut = XmlUtils.GetAttributeValue(xnCmd, "shortcutkey");
			string imageLabel = XmlUtils.GetAttributeValue(xnCmd, "image");

			item = new ToolStripMenuItem();
			item.Text = sText;
			if (!String.IsNullOrEmpty(sToolTip) && sToolTip != sText)
				item.ToolTipText = sToolTip;
			item.Tag = sMessage;
			if (!String.IsNullOrEmpty(shortcut))
				item.ShortcutKeys = ParseShortcutKeys(shortcut.ToLowerInvariant());
			// If the command doesn't have an explicit image label, then use the command id as
			// the image label.
			if (imageLabel == null)
				imageLabel = cmd;
			Image image;
			if (m_dictImages.TryGetValue(imageLabel, out image) && image != null)
				item.Image = image;
			item.Click += new EventHandler(OnItemClicked);

			m_dictMenuItems.Add(cmd, item);
			return item;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The shortcut is defined in terms of the DotNetBar enum, not the standard .Net Keys
		/// enum.  Thus, parsing the string is a bit trickier.
		/// </summary>
		/// <param name="shortcut"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private Keys ParseShortcutKeys(string shortcut)
		{
			if (String.IsNullOrEmpty(shortcut))
				return Keys.None;
			else if (shortcut.StartsWith("ctrl"))
				return Keys.Control | ParseShortcutKeys(shortcut.Substring(4));
			else if (shortcut.StartsWith("alt"))
				return Keys.Alt | ParseShortcutKeys(shortcut.Substring(3));
			else if (shortcut.StartsWith("shift"))
				return Keys.Shift | ParseShortcutKeys(shortcut.Substring(5));
			else
				return (Keys)Enum.Parse(typeof(Keys), shortcut, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is the click handler shared by all the context menu items.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		void OnItemClicked(object sender, EventArgs e)
		{
			if (m_mainWnd == null || m_mainWnd.Mediator == null)
				return;
			ToolStripMenuItem item = sender as ToolStripMenuItem;
			if (item == null)
				return;
			string sMessage = item.Tag as string;
			if (String.IsNullOrEmpty(sMessage))
				return;
			m_mainWnd.Mediator.SendMessage(sMessage, item);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the resource string whose id is the value of the given attribute of xnCmd.
		/// </summary>
		/// <param name="xnCmd"></param>
		/// <param name="sAttrName"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private string GetResourceString(XmlNode xnCmd, string sAttrName)
		{
			string sId = XmlUtils.GetAttributeValue(xnCmd, sAttrName);
			if (sId != null)
				return GetStringFromResource(sId);
			else
				return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the string with the given resource id value from one of the defined resource
		/// managers.
		/// </summary>
		/// <param name="kstid"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private string GetStringFromResource(string kstid)
		{
			if (kstid == null || kstid.Trim() == string.Empty)
				return null;
			string localizedStr = null;
			for (int i = 0; i < m_rmlocalStrings.Count; i++)
			{
				localizedStr = m_rmlocalStrings[i].GetString(kstid);
				if (localizedStr != null)
					break;
			}
			if (String.IsNullOrEmpty(localizedStr))
				localizedStr = kstid;
			return localizedStr.Trim();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the resource manager specified by the node.
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected ResourceManager GetResourceMngr(XmlNode node)
		{
			string assemblyPath = XmlUtils.GetAttributeValue(node, "assemblyPath");
			string className = XmlUtils.GetAttributeValue(node, "class");
			if (assemblyPath == null || className == null)
				return null;

			Assembly assembly = GetAssembly(assemblyPath);
			return new ResourceManager(className, assembly);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a named assembly.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected Assembly GetAssembly(string assemblyName)
		{
			string codeBasePath = FileUtils.StripFilePrefix(Assembly.GetExecutingAssembly().CodeBase);
			string baseDir = Path.GetDirectoryName(codeBasePath);
			string assemblyPath = Path.Combine(baseDir, assemblyName);

			Assembly assembly;

			try
			{
				assembly = Assembly.LoadFrom(assemblyPath);
				if (assembly == null)
					throw new ApplicationException(); //will be caught and described in the catch
			}
			catch (Exception error)
			{
				throw new Exception("ContextMenuHelper could not load the DLL at: " +
					assemblyPath, error);
			}

			return assembly;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the images from the resource specified in the XML definition.
		/// </summary>
		/// <param name="node"></param>
		/// ------------------------------------------------------------------------------------
		private void ReadImagesResources(XmlNode node)
		{
			string assemblyPath = XmlUtils.GetAttributeValue(node, "assemblyPath");
			string className = XmlUtils.GetAttributeValue(node, "class");
			string field = XmlUtils.GetAttributeValue(node, "field");
			string labels = XmlUtils.GetAttributeValue(node, "labels");

			if (assemblyPath == null || className == null || field == null || labels == null)
				return;

			ImageList images = GetImageListFromResourceAssembly(assemblyPath, className, field);
			string[] imageLabels = labels.Split(new char[] { ',', '\r', '\n', '\t' });
			int i = 0;
			foreach (string label in imageLabels)
			{
				string trimmedLabel = label.Trim();
				if (trimmedLabel != string.Empty && i >= 0 && i < images.Images.Count)
					m_dictImages[trimmedLabel] = images.Images[i++];
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the named image list from the given resource assembly.
		/// </summary>
		/// <param name="assemblyName"></param>
		/// <param name="className"></param>
		/// <param name="field"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private ImageList GetImageListFromResourceAssembly(string assemblyName,
			string className, string field)
		{
			Assembly assembly = GetAssembly(assemblyName);

			// Instantiate an object of the class containing the image list we're after.
			object classIntance = assembly.CreateInstance(className);
			if (classIntance == null)
			{
				throw new Exception("ContextMenuHelper could not create the class: " +
					className + ".");
			}

			//Get the named ImageList
			FieldInfo fldInfo = classIntance.GetType().GetField(field,
				BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

			if (fldInfo == null)
			{
				throw new Exception("ContextMenuHelper could not find the field '" + field +
					"' in the class: " + className + ".");
			}

			return (ImageList)fldInfo.GetValue(classIntance);
		}
	}
}

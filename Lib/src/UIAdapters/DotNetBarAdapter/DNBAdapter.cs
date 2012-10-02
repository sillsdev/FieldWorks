using System;
using System.IO;
using System.Xml;
using System.Windows.Forms;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Resources;
using System.Text;
using Microsoft.Win32;
using DevComponents.DotNetBar;
using SIL.FieldWorks.Common.UIAdapters;
using XCore;
using System.Collections.Generic;

namespace SIL.FieldWorks.Common.UIAdapters
{
	#region CommandInfo Class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class CommandInfo
	{
		internal string Message = null;
		internal string TextId = null;
		internal string Text = "?Unknown?";
		internal string TextAltId = null;
		internal string TextAlt = null;
		internal string ContextMenuTextId = null;
		internal string ContextMenuText = null;
		internal string ToolTipId = null;
		internal string ToolTip = null;
		internal string CategoryId = null;
		internal string Category = null;
		internal string StatusMsgId = null;
		internal string StatusMsg = null;
		internal eShortcut ShortcutKey = eShortcut.None;
		internal Image Image = null;
	}

	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// The main class from which DotNetBar MenuAdapter and ToolBarAdapter classes are derived.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class TMAdapter : ITMAdapter
	{
		private const int kDockSiteHeight = 24;
		private const string kMainMenuName = "~MainMenu~";
		private const string kToolbarItemSuffix = "~ToolbarItem~";
		private const string kDateTimeRegEntry = "TMDefinitionFileDateTime";
		private const string kAdapterVerEntry = "AdapterAssemblyVersion";

		internal DotNetBarManager m_dnbMngr;
		internal Bar m_menuBar = null;

		// This is true while we are reading the XML block of context menus.
		protected bool m_readingContextMenuDef = false;

		// This contains the controls returned from applications who return controls from
		// their LoadControlContainerItem delegates. See the comments on the
		// HandleControlContainerLoadRequests method for an explaination of what this
		// hash table is for.
		protected Hashtable m_htControls = new Hashtable();

		protected bool m_allowUndocking = true;
		protected string m_settingsFilePrefix = null;
		protected string[] m_settingFiles = null;
		protected Hashtable m_htWndListIndices = null;
		protected ButtonItem m_menuCurrentlyPoppedUp = null;
		protected ArrayList m_menusWithShortcuts = new ArrayList();
		protected bool m_customizeMenuShowing = false;
		protected bool m_allowuUpdates = true;
		protected DateTime m_itemUpdateTime = DateTime.Now;
		protected Hashtable m_htBarLocations = new Hashtable();
		protected XmlTextReader m_xmlReader;
		protected Form m_parentForm;
		protected Mediator m_msgMediator;
		protected string m_appsRegKeyPath = @"Software\SIL\FieldWorks";

		// Stores the item on the View menu that's the parent for the list of
		// toolbars.
		protected ButtonItem m_toolbarListItem = null;

		// Stores the item on the view/toolbars menu that shows the toolbar customization dialog.
		protected ButtonItem m_toolbarCustomizeItem = null;

		// Stores the item on the Window menu that's at the bottom of the window list
		// when there are more than 9 windows shown in the list.
		protected ButtonItem m_moreWindowItem = null;

		// Stores the item on the Window menu that is the first item in the list
		// of an application's open windows.
		protected ButtonItem m_windowListItem = null;

		// Resource Manager for localized toolbar and menu strings.
		protected ArrayList m_rmlocalStrings = new ArrayList();

		// Resource Manager for localized strings for customization, etc.
		protected ResourceManager m_rmlocalMngmntStrings;

		// Stores the TMItemProperties tag field for items that have one.
		protected Hashtable m_htItemTags = new Hashtable();

		// Stores all the images until we're done reading all the command ids.
		protected Hashtable m_htImages = new Hashtable();

		// Stores all the commands (and related information). The keys for this hash
		// table are the command id strings from the XML definition file.
		protected Hashtable m_htCommandInfo = new Hashtable();

		// This hash table stores hash tables. The keys for this hash table are form
		// types. A set of commands (i.e. those stored in the m_htCommandInfo) is saved
		// for each type of m_parentForm, not for each m_parentForm.
		protected static Hashtable m_htFormTypeCommands = new Hashtable();

		protected Hashtable m_htSystemStringIds = new Hashtable();

		protected bool m_barReadFromSettingFile = false;
		protected Hashtable m_htSettingsFileLoaded = new Hashtable();
		protected bool m_allowPopupToBeCanceled = true;

		/// <summary>items that need to be removed from the menu when it closes.</summary>
		private List<string> m_subItemsToRemoveOnMenuClose = null;

		#region IToolBarAdapter Events
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Event fired when adding menu/toolbar items to a menu/toolbar. This allows
		/// delegates of this event to initialize properties of the menu/toolbar item such
		/// as its text, etc.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public event InitializeItemHandler InitializeItem;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Event fired when adding a menubar or toolbar to the toolbar container.
		/// This allows delegates of this event to initialize properties of the toolbar such
		/// as its text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public event InitializeBarHandler InitializeBar;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Event fired when adding a combobox toolbar item. This gives applications a chance
		/// to initialize a combobox item.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public event InitializeComboItemHandler InitializeComboItem;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Event fired when a control container item requests the control to contain.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public event LoadControlContainerItemHandler LoadControlContainerItem;

		#endregion

		#region Adapter initialization
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the specified parent form.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Initialize(Form parentForm, Control contentPanelContent,
			Mediator messageMediator, string appsRegKeyPath, string[] definitions)
		{
			Initialize(parentForm, messageMediator, appsRegKeyPath, definitions);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the specified parent form.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Initialize(Form parentForm, Control contentPanelContent,
			Mediator messageMediator, string[] definitions)
		{
			Initialize(parentForm, messageMediator, definitions);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="parentForm"></param>
		/// <param name="msgMediator"></param>
		/// <param name="definitions"></param>
		/// <param name="appsRegKeyPath">Registry key path (under HKCU) where application's
		/// settings are stored (default is "Software\SIL\FieldWorks").</param>
		/// <param name="fileSpecForUserSettings"></param>
		/// ------------------------------------------------------------------------------------
		public void Initialize(Form parentForm, Mediator msgMediator, string appsRegKeyPath, string[] definitions)
		{
			m_appsRegKeyPath = appsRegKeyPath;
			Initialize(parentForm, msgMediator, definitions);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="parentForm"></param>
		/// <param name="msgMediator"></param>
		/// <param name="definitions"></param>
		/// <param name="fileSpecForUserSettings"></param>
		/// ------------------------------------------------------------------------------------
		public void Initialize(Form parentForm, Mediator msgMediator, string[] definitions)
		{
			if (m_parentForm != null && m_parentForm == parentForm)
				return;

			m_parentForm = parentForm;
			MessageMediator = msgMediator;

			// Setup a DNB manager
			InitDNBManager(parentForm);

			// Read images, localized strings and command Ids.
			ReadResourcesAndCommands(definitions);

			m_dnbMngr.SuspendLayout = true;

			#region Do this stuff for Menus
			ReadMenuDefinitions(definitions);

			if (m_windowListItem != null && m_windowListItem.Parent != null)
				((ButtonItem)m_windowListItem.Parent).PopupClose += HandleWindowMenuClosing;

			if (m_toolbarListItem != null)
				m_toolbarListItem.PopupClose += HandleToolBarListMenuClosing;

			#endregion

			#region Do this stuff for Toolbars
			GetSettingFilesPrefix(definitions);
			CheckDefinitionDates(definitions);
			CheckAssemblyVersion();
			ReadToolbarDefinitions(definitions);
			LoadCustomBarsFromSettings();
			m_dnbMngr.SuspendLayout = false;

			PositionToolbars();
			AllowAppsToIntializeComboItems();

			// Setup things so we're notified whenever a customize menu opens and closes.
			InitializeCustomizeItemPopupHandlers();
			#endregion

			m_htImages = null;
			m_rmlocalStrings = null;

			Application.Idle += HandleItemUpdates;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Disposed event of the m_msgMediator control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void HandleMessageMediatorDisposed(object sender, EventArgs e)
		{
			m_allowuUpdates = false;
			MessageMediator = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a DotNetBar manager.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitDNBManager(Form parentForm)
		{
			// At this point there is no DNB manager for the specified form. Therefore
			// instantiate one now and add it to the hash table.
			m_dnbMngr = new DevComponents.DotNetBar.DotNetBarManager();
			m_dnbMngr.ParentForm = parentForm;
			m_dnbMngr.Style = eDotNetBarStyle.OfficeXP;
			m_dnbMngr.ThemeAware = true;
			m_dnbMngr.ShowShortcutKeysInToolTips = true;
			m_dnbMngr.MenuDropShadow = eMenuDropShadow.SystemDefault;
			m_dnbMngr.AlwaysShowFullMenus = true;

			// Setup a docking site on each of the form's sides.
			m_dnbMngr.TopDockSite = SetupDocSite(DockStyle.Top);
			m_dnbMngr.BottomDockSite = SetupDocSite(DockStyle.Bottom);
			m_dnbMngr.RightDockSite = SetupDocSite(DockStyle.Right);
			m_dnbMngr.LeftDockSite = SetupDocSite(DockStyle.Left);
			m_parentForm.Controls.Add(m_dnbMngr.RightDockSite);
			m_parentForm.Controls.Add(m_dnbMngr.LeftDockSite);
			m_parentForm.Controls.Add(m_dnbMngr.TopDockSite);
			m_parentForm.Controls.Add(m_dnbMngr.BottomDockSite);

			// This will make sure we deliver the proper localized string for
			// DotNetBar's internal strings (e.g. strings for their customize dialog).
			m_dnbMngr.LocalizeString += HandleGettingLocalizedString;

			ReadSystemStringIds();

			m_dnbMngr.PopupOpen += HandleMenuPopups;
			m_dnbMngr.PopupClose += HandleMenuPopupClose;
			m_dnbMngr.ItemClick += HandleItemClicks;
			m_dnbMngr.PopupContainerLoad += HandleItemsPopup;
			m_dnbMngr.UserCustomize += HandleUserCustomize;

			// This makes sure control container items get their control from the application.
			m_dnbMngr.ContainerLoadControl += HandleControlContainerLoadRequests;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method gets the combobox items and fires the InitializeComboItem event in
		/// case the application wants to initialize the combo box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void AllowAppsToIntializeComboItems()
		{
			if (InitializeComboItem == null || m_dnbMngr.Bars == null)
				return;

			foreach (Bar bar in m_dnbMngr.Bars)
			{
				if (bar.Items == null)
					continue;

				foreach (BaseItem item in bar.Items)
				{
					ComboBoxItem cboItem = item as ComboBoxItem;
					if (cboItem != null)
					{
						cboItem.ComboBoxEx.Items.Clear();
						InitializeComboItem(cboItem.Name, cboItem.ComboBoxEx as ComboBox);
					}
				}
			}
		}

		#endregion

		#region Misc. Setup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a DotNetBar docking site for a DotNetBar toolbar.
		/// </summary>
		/// <param name="dockStyle">The docking location on the form of the site.</param>
		/// <returns>A DotNetBar docking site.</returns>
		/// ------------------------------------------------------------------------------------
		private DockSite SetupDocSite(DockStyle dockStyle)
		{
			DockSite site = new DockSite();
			site.Dock = dockStyle;
			site.AccessibleRole = System.Windows.Forms.AccessibleRole.Window;
			site.BackColor = SystemColors.Control;
			site.BackgroundImageAlpha = ((System.Byte)(255));
			site.Name = "dnbDockSite" + dockStyle.ToString();
			site.TabStop = false;

			switch (dockStyle)
			{
				case DockStyle.Top:
					site.Location = new Point(0, 0);
					site.Size = new Size(m_parentForm.ClientSize.Width, kDockSiteHeight);
					break;

				case DockStyle.Bottom:
					site.Location = new System.Drawing.Point(0, m_parentForm.ClientSize.Height);
					site.Size = new System.Drawing.Size(m_parentForm.ClientSize.Width, 0);
					break;

				case DockStyle.Right:
					site.Size = new Size(0, m_parentForm.ClientSize.Height - kDockSiteHeight);
					site.Location = new Point(m_parentForm.ClientSize.Width, kDockSiteHeight);
					break;

				case DockStyle.Left:
					site.Location = new Point(0, kDockSiteHeight);
					site.Size = new Size(0, m_parentForm.ClientSize.Height - kDockSiteHeight);
					break;

				default:
					break;
			}

			return site;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method gets the customize items for all the toolbars and subscribes to the
		/// ExpandChange event so we know when customize menus open and close.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitializeCustomizeItemPopupHandlers()
		{
			ArrayList items = m_dnbMngr.GetItems(string.Empty, typeof(CustomizeItem));

			foreach (CustomizeItem item in items)
				item.ExpandChange += HandleCustomizeMenuPopup;
		}

		#endregion

		#region Misc. methods for loading toolbar defs. from settings file as well as saving settings
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the file location where settings files are stored.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private string SettingsFileLocation
		{
			get
			{
				// store settings files in the local application data folder
				string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
					+ Path.DirectorySeparatorChar + "SIL"
					+ Path.DirectorySeparatorChar + Application.ProductName;

				// Make sure the path exists
				Directory.CreateDirectory(path);

				return path;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Causes the adapter to save toolbar settings (e.g. user placement of toolbars).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SaveBarSettings()
		{
			string fileSpec = string.Empty;

			foreach (Bar bar in m_dnbMngr.Bars)
			{
				if (bar == m_dnbMngr.Bars[kMainMenuName])
					continue;

				try
				{
					string file = m_settingsFilePrefix + "." + bar.Name + ".xml";
					fileSpec = Path.Combine(SettingsFileLocation, file);

					// If the file exists, make sure it is not read-only. if the file does
					// not exist, make sure the settings folder exists before writing.
					if (File.Exists(fileSpec))
						File.SetAttributes(fileSpec, FileAttributes.Normal);
					else
						Directory.CreateDirectory(SettingsFileLocation);

					// Save the file and mark it read-only and hidden so it will not be
					// accidentally edited.
					bar.SaveDefinition(fileSpec);
					File.SetAttributes(fileSpec, FileAttributes.ReadOnly | FileAttributes.Hidden);
				}
				catch
				{
					Debug.WriteLine("\nError saving toolbar definition: " + fileSpec + "\n");
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Examines the XML definition files to find which one (if any) have the prefix
		/// used for storing toolbar settings.
		/// </summary>
		/// <param name="definitions"></param>
		/// ------------------------------------------------------------------------------------
		private void GetSettingFilesPrefix(string[] definitions)
		{
			XmlDocument xmlDef = new XmlDocument();
			xmlDef.PreserveWhitespace = false;

			foreach (string def in definitions)
			{
				if (def == null)
					continue;

				xmlDef.Load(def);
				XmlNode node = xmlDef.SelectSingleNode("TMDef/toolbars");
				if (node != null)
				{
					m_settingsFilePrefix = GetAttributeValue(node, "settingFilesPrefix");
					m_allowUndocking = GetBoolFromAttribute(node, "onmodalform", true);

					if (m_settingsFilePrefix != null)
					{
						m_settingFiles = Directory.GetFiles(SettingsFileLocation,
							m_settingsFilePrefix + ".*.xml");

						return;
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method checks the date and time of the TM definition files to see if they
		/// are newer than they were the last time the application was run. If one or more
		/// of them are newer, then all the toolbar settings files written by DotNetBar are
		/// deleted so the toolbars will be completely reconstructed from the TM definition
		/// files.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CheckDefinitionDates(string[] definitions)
		{
			DateTime newestDefDateTime = new DateTime(0);

			// Read the saved date/time from the registry.

			string dateString = (string)TMDefinitionDateKey.GetValue(kDateTimeRegEntry, string.Empty);
			DateTime savedDateTime = ParseDateTime(dateString);

			foreach (string def in definitions)
			{
				if (def == null || !File.Exists(def))
					continue;

				// Get the date/time of this definition file. If it is the newest one so
				// far, then save the date/time.
				DateTime fileDateTime = GetFileDateTime(def);
				if (fileDateTime > newestDefDateTime)
					newestDefDateTime = fileDateTime;

				// If this file is newer than the saved date/time then delete all of the
				// settings files.
				if (fileDateTime > savedDateTime)
					DeleteSettingsFiles();
			}

			// Save the date/time of the newest TM definition file.
			TMDefinitionDateKey.SetValue(kDateTimeRegEntry, FormatDateTime(newestDefDateTime));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Parse our custom date/time string. If the string is not valid, then
		/// return an empty date/time
		/// </summary>
		/// <param name="dateString">string to parse in the format of *YYYY-MM-DD HH:MM:SS
		/// where the time is in 24-hour</param>
		/// <returns>the date/time</returns>
		/// ------------------------------------------------------------------------------------
		private DateTime ParseDateTime(string dateString)
		{
			if (dateString.Length == 20 && dateString.Substring(0, 1) == "*")
			{
				try
				{
					int year = Int32.Parse(dateString.Substring(1, 4));
					int month = Int32.Parse(dateString.Substring(6, 2));
					int day = Int32.Parse(dateString.Substring(9, 2));
					int hour = Int32.Parse(dateString.Substring(12, 2));
					int minute = Int32.Parse(dateString.Substring(15, 2));
					int second = Int32.Parse(dateString.Substring(18, 2));
					return new DateTime(year, month, day, hour, minute, second);
				}
				catch{}
			}

			return new DateTime(0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Format a date/time in our custom format
		/// </summary>
		/// <param name="dt">date/time to format</param>
		/// <returns>a string representing the date/time (see ParseDateTime for format)</returns>
		/// ------------------------------------------------------------------------------------
		private string FormatDateTime(DateTime dt)
		{
			StringBuilder dateTimeString = new StringBuilder();

			dateTimeString.Append("*");
			dateTimeString.Append(dt.Year.ToString());
			dateTimeString.Append("-");
			dateTimeString.Append(dt.Month.ToString("d2"));
			dateTimeString.Append("-");
			dateTimeString.Append(dt.Day.ToString("d2"));
			dateTimeString.Append(" ");
			dateTimeString.Append(dt.Hour.ToString("d2"));
			dateTimeString.Append(":");
			dateTimeString.Append(dt.Minute.ToString("d2"));
			dateTimeString.Append(":");
			dateTimeString.Append(dt.Second.ToString("d2"));

			return dateTimeString.ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the date/time for a file in UTC. We do this to get the date/time in a
		/// consistent format without milliseconds.
		/// </summary>
		/// <param name="fileName">file name to get the date/time of</param>
		/// <returns>date/time of the file</returns>
		/// ------------------------------------------------------------------------------------
		private DateTime GetFileDateTime(string fileName)
		{
			DateTime fileTime = File.GetLastWriteTimeUtc(fileName);

			return new DateTime(fileTime.Year, fileTime.Month, fileTime.Day, fileTime.Hour,
				fileTime.Minute, fileTime.Second);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks if the current adapter assembly version has changed since the last time
		/// the application was run.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CheckAssemblyVersion()
		{
			// Read the saved version of the adapter assembly from the registry.
			string savedVersion = (string)TMDefinitionDateKey.GetValue(kAdapterVerEntry, "0");
			string currentVersion = this.GetType().Assembly.GetName().Version.ToString();

			// If the assembly versions do not match then delete the saved files.
			if (savedVersion != currentVersion)
				DeleteSettingsFiles();

			// Save the current adapter assembly version
			TMDefinitionDateKey.SetValue(kAdapterVerEntry, currentVersion);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deletes the toolbar settings files created by DotNetBar in the SaveSettings method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void DeleteSettingsFiles()
		{
			// Get the toolbar settings files written by DotNetBar.
			string[] settingsFiles =
				Directory.GetFiles(SettingsFileLocation, m_settingsFilePrefix + ".*.xml");

			// Delete the toolbar settings files.
			foreach (string file in settingsFiles)
			{
				File.SetAttributes(file, FileAttributes.Normal);
				File.Delete(file);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void LoadCustomBarsFromSettings()
		{
			for (int i = 0; m_settingFiles != null && i < m_settingFiles.Length; i++)
			{
				if (m_settingFiles[i] != null && File.Exists(m_settingFiles[i]))
				{
					Bar bar = new Bar();
					m_dnbMngr.Bars.Add(bar);
					try
					{
						// This should never throw an error. However, according to TE-6485
						// there is a case in which we've gotten here and the settings file
						// was empty. I have no idea how that could happen and it's a lucky
						// tester that will be able to reproduce the problem. The solution
						// here is to just throw away the custom toolbar if there is an
						// error caused by loading its definition file.
						bar.LoadDefinition(m_settingFiles[i]);
					}
					catch
					{
						// This should never happen.
						m_dnbMngr.Bars.Remove(bar);
						try
						{
							File.SetAttributes(m_settingFiles[i], FileAttributes.Normal);
							File.Delete(m_settingFiles[i]);
						}
						catch { }
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Positions the toolbars to the positions specified in the xml deffinition file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void PositionToolbars()
		{
			bool menuBarPresent = false;
			foreach (Bar bar in m_dnbMngr.Bars)
			{
				if (bar.MenuBar)
				{
					menuBarPresent = true;
					break;
				}
			}

			m_dnbMngr.SuspendLayout = true;

			foreach (Bar bar in m_dnbMngr.Bars)
			{
				if (bar.MenuBar || !m_htBarLocations.Contains(bar))
					continue;

				bool visible = bar.Visible;

				bar.DockSide = eDockSide.Top;
				Point barLoc = (Point)m_htBarLocations[bar];
				bar.DockLine = barLoc.Y + (menuBarPresent ? 1 : 0);

				//TODO: fix this to use the barLoc.X intelligently
				bar.DockOffset = 0; //barLoc.X;
				bar.RecalcLayout();

				if (!visible)
					bar.Hide();
			}

			m_dnbMngr.SuspendLayout = false;
		}

		#endregion

		#region Internal Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the registry key where the adapter stores the date/time of the newest TM
		/// definition file passed from an application.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private RegistryKey TMDefinitionDateKey
		{
			get
			{
				m_appsRegKeyPath = m_appsRegKeyPath.TrimEnd(new char[] {'\\'});
				return Registry.CurrentUser.CreateSubKey(
					m_appsRegKeyPath + @"\ToolBarAdapterVersions\" + m_settingsFilePrefix);
			}
		}

		#endregion

		#region ITMInterface Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the message mediator used by the TM adapter for message dispatch.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Mediator MessageMediator
		{
			get {return m_msgMediator;}
			set
			{
				if (m_msgMediator == value)
					return;
				if (m_msgMediator != null)
					m_msgMediator.Disposed -= HandleMessageMediatorDisposed;
				if (value != null)
					value.Disposed += HandleMessageMediatorDisposed;
				m_msgMediator = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not updates to toolbar items should
		/// take place during the application's idle cycles.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool AllowUpdates
		{
			get {return m_allowuUpdates;}
			set {m_allowuUpdates = value;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an array of toolbar property objects an application can use to send to a
		/// menu extender to use for display on a view menu allowing users to toggle the
		/// visibility of each toolbar.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public TMBarProperties[] BarInfoForViewMenu
		{
			get
			{
				int barCount = 0;
				foreach (Bar bar in m_dnbMngr.Bars)
				{
					if (!bar.MenuBar)
						barCount++;
				}

				if (barCount == 0)
					return null;

				TMBarProperties[] barInfo = new TMBarProperties[barCount];

				int index = 0;
				for (int i = 0; i < m_dnbMngr.Bars.Count; i++)
				{
					if (!m_dnbMngr.Bars[i].MenuBar)
						barInfo[index++] = GetBarProperties(m_dnbMngr.Bars[i].Name);
				}

				Array.Sort(barInfo, new ToolBarNameSorter());
				return barInfo;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private class ToolBarNameSorter : IComparer
		{
			int IComparer.Compare(object x, object y)
			{
				string item1 = ((TMBarProperties)x).Text;
				string item2 = ((TMBarProperties)y).Text;
				return((new CaseInsensitiveComparer()).Compare(item1, item2));
			}
		}

		#endregion

		#region Method & property for getting localized versions of string used by DotNetBar
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal string GetSystemStringsXml
		{
			get
			{
				Stream stream =	this.GetType().Assembly.GetManifestResourceStream(
					"SIL.FieldWorks.Common.UIAdapters.DNBSystemStrings.xml");
				StreamReader reader = new StreamReader(stream);
				string xmlData = reader.ReadToEnd();
				reader.Close();
				return xmlData;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reads the strings to localize what DotNetBar uses internally (e.g. on their
		/// customize toolbar dialog).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ReadSystemStringIds()
		{
			XmlDocument xmlDef = new XmlDocument();
			xmlDef.PreserveWhitespace = false;
			xmlDef.LoadXml(GetSystemStringsXml);
			XmlNode node = xmlDef.SelectSingleNode("dotnetbar/systemstringids");

			foreach (XmlAttribute attrib in node.Attributes)
				m_htSystemStringIds[attrib.Name] = attrib.Value;
		}

		#endregion

		#region Methods for reading resources and command section of definition
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="definitions"></param>
		/// ------------------------------------------------------------------------------------
		private void ReadResourcesAndCommands(string[] definitions)
		{
			XmlDocument xmlDef = new XmlDocument();

			foreach (string def in definitions)
			{
				if (def == null)
					continue;

				xmlDef.PreserveWhitespace = false;
				//xmlDef.LoadXml(def);
				xmlDef.Load(def);

				XmlNode node = xmlDef.SelectSingleNode("TMDef/resources");
				if (node == null || !node.HasChildNodes)
					return;

				node = node.FirstChild;
				while (node != null)
				{
					switch (node.Name)
					{
						case "imageList":
							// Get the images from the specified resource files.
							ReadImagesResources(node);
							break;

						case "localizedstrings":
							// Get the resource files containing the localized strings.
							ResourceManager rm = GetResourceMngr(node);
							if (rm != null)
								m_rmlocalStrings.Add(rm);
							break;

						case "systemstringids":
							// We assume this node will only be found in one of the definition files
							// but if it's found in more, then the last one loaded is the one used.
							m_rmlocalMngmntStrings = GetResourceMngr(node);
							break;
					}

					node = ReadOverJunk(node.NextSibling);
				}

				node = xmlDef.SelectSingleNode("TMDef/commands");
				ReadCommands(node);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the images from the resource specified in the XML definition.
		/// </summary>
		/// <param name="node"></param>
		/// ------------------------------------------------------------------------------------
		private void ReadImagesResources(XmlNode node)
		{
			string assemblyPath = GetAttributeValue(node, "assemblyPath");
			string className = GetAttributeValue(node, "class");
			string field = GetAttributeValue(node, "field");
			string labels = GetAttributeValue(node, "labels");

			if (assemblyPath == null || className == null || field == null || labels == null)
				return;

			ImageList images = GetImageListFromResourceAssembly(assemblyPath, className, field);
			string[] imageLabels = labels.Split(new char[] {',', '\r', '\n', '\t'});
			int i = 0;
			foreach (string label in imageLabels)
			{
				string trimmedLabel = label.Trim();
				if (trimmedLabel != string.Empty && i >= 0 && i < images.Images.Count)
					m_htImages[trimmedLabel] = images.Images[i++];
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
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
				throw new Exception("DotNetBar Adapter could not create the class: " +
					className + ".");
			}

			//Get the named ImageList
			FieldInfo fldInfo = classIntance.GetType().GetField(field,
				BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

			if (fldInfo == null)
			{
				throw new Exception("DotNetBar Adapter could not find the field '" + field +
					"' in the class: " + className + ".");
			}

			return (ImageList)fldInfo.GetValue(classIntance);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected ResourceManager GetResourceMngr(XmlNode node)
		{
			string assemblyPath = GetAttributeValue(node, "assemblyPath");
			string className = GetAttributeValue(node, "class");
			if (assemblyPath == null || className == null)
				return null;

			Assembly assembly = GetAssembly(assemblyPath);
			return new ResourceManager(className, assembly);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads a named assembly.
		/// </summary>
		/// <param name="assemblyName"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected Assembly GetAssembly(string assemblyName)
		{
			string baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase);
			// Note: CodeBase prepends "file:/", which must be removed.
			string assemblyPath = Path.Combine(baseDir.Substring(6), assemblyName);

			Assembly assembly = null;

			try
			{
				assembly = Assembly.LoadFrom(assemblyPath);
				if (assembly == null)
					throw new ApplicationException(); //will be caught and described in the catch
			}
			catch (Exception error)
			{
				throw new Exception("DotNetBar Adapter could not load the DLL at: " +
					assemblyPath, error);
			}

			return assembly;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reads the commands section of a definition file.
		/// </summary>
		/// <param name="node"></param>
		/// ------------------------------------------------------------------------------------
		protected void ReadCommands(XmlNode node)
		{
			XmlNode commandNode = node.FirstChild;

			while (commandNode != null)
			{
				string cmd = GetAttributeValue(commandNode, "id");
				if (cmd == null)
					continue;

				CommandInfo cmdInfo = new CommandInfo();
				cmdInfo.TextId = GetAttributeValue(commandNode, "text");
				cmdInfo.TextAltId = GetAttributeValue(commandNode, "textalt");
				cmdInfo.ContextMenuTextId = GetAttributeValue(commandNode, "contextmenutext");
				cmdInfo.CategoryId = GetAttributeValue(commandNode, "category");
				cmdInfo.ToolTipId = GetAttributeValue(commandNode, "tooltip");
				cmdInfo.StatusMsgId = GetAttributeValue(commandNode, "statusmsg");
				cmdInfo.Message = GetAttributeValue(commandNode, "message");
				string shortcut = GetAttributeValue(commandNode, "shortcutkey");
				string imageLabel =	GetAttributeValue(commandNode, "image");

				if (cmdInfo.TextId != null)
					cmdInfo.Text = GetStringFromResource(cmdInfo.TextId);

				if (cmdInfo.TextAltId != null)
					cmdInfo.TextAlt = GetStringFromResource(cmdInfo.TextAltId);

				if (cmdInfo.ContextMenuTextId != null)
					cmdInfo.ContextMenuText = GetStringFromResource(cmdInfo.ContextMenuTextId);

				if (cmdInfo.CategoryId != null)
					cmdInfo.Category = GetStringFromResource(cmdInfo.CategoryId);

				if (cmdInfo.ToolTipId != null)
					cmdInfo.ToolTip = GetStringFromResource(cmdInfo.ToolTipId);

				if (cmdInfo.StatusMsgId != null)
					cmdInfo.StatusMsg = GetStringFromResource(cmdInfo.ToolTipId);

				if (cmdInfo.StatusMsg == null || cmdInfo.StatusMsg == string.Empty)
					cmdInfo.StatusMsg = GetStringFromResource("kstidDefaultStatusBarMsg");

				if (shortcut != null)
					cmdInfo.ShortcutKey = (eShortcut)Enum.Parse(typeof(eShortcut), shortcut, true);

				// If the command doesn't have an explicit image label, then use
				// the command id as the image label.
				if (imageLabel == null)
					imageLabel = cmd;
				if (m_htImages[imageLabel] != null)
					cmdInfo.Image = (Image)m_htImages[imageLabel];

				m_htCommandInfo[cmd] = cmdInfo;

				commandNode = ReadOverJunk(commandNode.NextSibling);
			}
		}

		#endregion

		#region Methods for reading menu definitions and building menu items
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="definitions"></param>
		/// ------------------------------------------------------------------------------------
		private void ReadMenuDefinitions(string[] definitions)
		{
			XmlDocument xmlDef = new XmlDocument();

			foreach (string def in definitions)
			{
				if (def == null)
					continue;

				xmlDef.PreserveWhitespace = false;
				xmlDef.Load(def);
				XmlNode node = xmlDef.SelectSingleNode("TMDef/menus/item");
				if (node == null)
					continue;

				if (m_menuBar == null)
				{
					m_menuBar = new Bar(kMainMenuName);
					m_dnbMngr.Bars.Add(m_menuBar);
					m_menuBar.Name = kMainMenuName;
					m_menuBar.MenuBar = true;
					m_menuBar.DockSide = eDockSide.Top;
					m_menuBar.DockLine = 0;
					m_menuBar.LockDockPosition = true;
					m_menuBar.CanCustomize = false;
					m_menuBar.CanHide = false;
					m_menuBar.CanUndock = false;
					m_menuBar.ShowToolTips = false;
					m_menuBar.Stretch = true;
					m_menuBar.Visible = true;
					m_menuBar.AccessibleRole = AccessibleRole.MenuBar;
					m_menuBar.AccessibleName = m_menuBar.Text;
				}

				ReadMenuItems(node, m_menuBar, false);
				ReadContextMenus(xmlDef.SelectSingleNode("TMDef/contextmenus/contextmenu"));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Builds all context menus, adding them to the DNB manager's list of context menus.
		/// </summary>
		/// <param name="node"></param>
		/// ------------------------------------------------------------------------------------
		private void ReadContextMenus(XmlNode node)
		{
			m_dnbMngr.ContextMenus.Clear();

			m_readingContextMenuDef = true;

			while (node != null)
			{
				string name = GetAttributeValue(node, "name");
				if (name != null)
				{
					SilButtonItem cmnu = new SilButtonItem(name);
					ReadMenuItems(node.FirstChild, cmnu, true);
					if (cmnu.SubItems.Count > 0)
					{
						// Make sure that if a command has different text for
						// context menus then we account for it.
						foreach (ButtonItem subItem in cmnu.SubItems)
						{
							CommandInfo cmndInfo = GetCommandInfo(subItem);
							if (cmndInfo != null && cmndInfo.ContextMenuText != null)
								subItem.Text = cmndInfo.ContextMenuText;
						}

						m_dnbMngr.ContextMenus.Add(cmnu);
					}
				}

				node = ReadOverJunk(node.NextSibling);
			}

			m_readingContextMenuDef = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Recursively builds menus.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="parentItem"></param>
		/// <param name="readingContextMenus"></param>
		/// ------------------------------------------------------------------------------------
		private void ReadMenuItems(XmlNode node, object parentItem, bool readingContextMenus)
		{
			if (parentItem == null)
				return;

			while (node != null)
			{
				ButtonItem item = (ButtonItem)ReadSingleItem(node, true);

				// If we're reading context menus and the item was assigned a shortcut,
				// then get rid of it since context menus shouldn't have shortcuts.
				if (readingContextMenus && item.Shortcuts.Count > 0)
					item.Shortcuts.Clear();

				// If the item has a shortcut then we need to add it to the list of
				// menu items to get updated during the application's idle cycles.
				if (item.Shortcuts.Count > 0 && !m_menusWithShortcuts.Contains(item))
					m_menusWithShortcuts.Add(item);

				string insertBefore = GetAttributeValue(node, "insertbefore");
				string addTo = GetAttributeValue(node, "addto");
				bool cancelBeginGroup = GetBoolFromAttribute(node, "cancelbegingrouponfollowingitem");

				if (insertBefore != null)
					InsertMenuItem(item, insertBefore, cancelBeginGroup);
				else if (addTo != null)
					AddMenuItem(item, addTo);
				else
				{
					if (parentItem is Bar)
						((Bar)parentItem).Items.Add(item);
					else if (parentItem is ButtonItem)
						((ButtonItem)parentItem).SubItems.Add(item);
				}

				// Now read any subitems of the one just created.
				if (node.ChildNodes.Count > 0)
					ReadMenuItems(node.FirstChild, item, readingContextMenus);

				node = ReadOverJunk(node.NextSibling);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the item specified by refItemName and inserts the specified item before
		/// it in the collection to which refItemName belongs.
		/// </summary>
		/// <param name="item"></param>
		/// <param name="refItemName"></param>
		/// <param name="cancelBeginGroup">True if refItemName should no longer have it's
		/// BeginGroup property set.</param>
		/// ------------------------------------------------------------------------------------
		internal void InsertMenuItem(ButtonItem item, string refItemName, bool cancelBeginGroup)
		{
			if (m_menuBar == null || item == null || refItemName == null)
				return;

			ButtonItem refItem = m_menuBar.GetItem(refItemName) as ButtonItem;
			if (refItem == null)
				return;

			if (refItem.Parent is ButtonItem)
			{
				// Get the parent item of the item we're inserting before. Then
				// get the reference item's index in the parent's subitem collection.
				ButtonItem parentItem = refItem.Parent as ButtonItem;
				int i = parentItem.SubItems.IndexOf(refItem);
				parentItem.SubItems.Add(item, i);

				// The item we just added has now become the new first item in the group
				// so remove the begingroup setting on the item we just inserted before.
				// (The assumption is the cancelbegingrouponfollowingitem attribute is
				// only used with the insertbefore attribute and not insertafter.)
				if (cancelBeginGroup)
					refItem.BeginGroup = false;
			}
			else
			{
				// The reference item's parent is not another ButtonItem so the ref. item
				// must be on the main menu bar, so add the inserted item to the main menu.
				int i = m_menuBar.Items.IndexOf(refItem);
				m_menuBar.Items.Add(item, i);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the item specified by refItemName and Adds the specified item to the ref.
		/// item's collection of sub items.
		/// </summary>
		/// <param name="item"></param>
		/// <param name="refItemName"></param>
		/// ------------------------------------------------------------------------------------
		private void AddMenuItem(ButtonItem item, string refItemName)
		{
			if (m_menuBar == null || item == null || refItemName == null)
				return;

			ButtonItem refItem = m_menuBar.GetItem(refItemName) as ButtonItem;

			if (refItem != null)
				refItem.SubItems.Add(item);
			else
				m_menuBar.Items.Add(item);
		}

		#endregion

		#region Methods for reading toolbar definitions and building toolbars and their items
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Iterates through the toolbar definitions to read the XML.
		/// </summary>
		/// <param name="definitions">Array of XML strings toolbar definitions.</param>
		/// ------------------------------------------------------------------------------------
		private void ReadToolbarDefinitions(string[] definitions)
		{
			XmlDocument xmlDef = new XmlDocument();
			xmlDef.PreserveWhitespace = false;

			foreach (string def in definitions)
			{
				if (def == null || !File.Exists(def))
					continue;

				xmlDef.Load(def);
				XmlNode node = xmlDef.SelectSingleNode("TMDef/toolbars/toolbar");

				while (node != null)
				{
					ReadSingleToolbarDef(node);
					node = ReadOverJunk(node.NextSibling);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="node"></param>
		/// ------------------------------------------------------------------------------------
		private void ReadSingleToolbarDef(XmlNode node)
		{
			string barName = GetAttributeValue(node, "name");

			Bar bar = BarFromSettingFile(barName);
			m_barReadFromSettingFile = (bar != null);

			if (!m_barReadFromSettingFile)
				bar = MakeNewBar(node, barName);

			if (node.ChildNodes.Count > 0)
				ReadToolbarItems(node.FirstChild, bar);

			if (m_barReadFromSettingFile)
				return;

			bool visible = bar.Visible;

			// If customizing the bar is allowed then add the little button at the end
			// of the toolbar that pops-up a list of the items to make visible or not.
			if (bar.CanCustomize)
				bar.Items.Add(new CustomizeItem());

			// Add the bar to the manager and set its dock location, since that has
			// to be done after it's been added to the DotNetBar manager.
			m_dnbMngr.Bars.Add(bar);

			// This seems like a strange thing to do, but it seems as though when
			// a toolbar's visible property is set to false before adding it to the
			// manager, then it becomes visible. Therefore, hide it after adding it
			// to the manager if its visible property was false before it was added.
			if (!visible)
				bar.Hide();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If the toolbar has a settings file (i.e. a file that was saved when the
		/// application was last closed) then restore the toolbar from it.
		/// </summary>
		/// <param name="barName"></param>
		/// <returns>The restored toolbar or null if the toolbar doesn't have a settings
		/// file.</returns>
		/// ------------------------------------------------------------------------------------
		private Bar BarFromSettingFile(string barName)
		{
			string fileSpec = Path.Combine(SettingsFileLocation,
				m_settingsFilePrefix + "." + barName + ".xml");

			if (File.Exists(fileSpec))
			{
				try
				{
					Bar bar = new Bar(barName);
					m_dnbMngr.Bars.Add(bar);
					bar.LoadDefinition(fileSpec);

					// Now that we've read the bar specified by barName, remove it's
					// settings file name from the array storing all of the toolbar
					// setting files.
					for (int i = 0; m_settingFiles != null && i < m_settingFiles.Length; i++)
					{
						if (m_settingFiles[i] == fileSpec)
						{
							m_settingFiles[i] = null;
							break;
						}
					}

					return bar;
				}
				catch {}
			}

			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="node"></param>
		/// <param name="parentItem"></param>
		/// ------------------------------------------------------------------------------------
		private void ReadToolbarItems(XmlNode node, object parentItem)
		{
			while (node != null)
			{
				string name = GetAttributeValue(node, "name");

				BaseItem item = ReadSingleItem(node, false);

				// We assume that if we've restored from a settings file, we don't need to
				// add the item to any collection since it should already be in a toolbar.
				if (!m_barReadFromSettingFile)
				{
					if (parentItem is Bar)
						((Bar)parentItem).Items.Add(item);
					else if (parentItem is ButtonItem)
						((ButtonItem)parentItem).SubItems.Add(item);
				}

				if (node.ChildNodes.Count > 0)
					ReadToolbarItems(node.FirstChild, item);

				node = ReadOverJunk(node.NextSibling);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make a new toolbar and initialize it based on what's in the XML
		/// definition.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="barName"></param>
		/// ------------------------------------------------------------------------------------
		private Bar MakeNewBar(XmlNode node, string barName)
		{
			// Every bar must have a unique name so if we received a null or empty barName
			// then name it with a new guid.
			if (barName == null || barName == string.Empty)
				barName = Guid.NewGuid().ToString();

			Bar bar = new Bar();
			bar.Name = barName;
			bar.AccessibleName = barName;
			bar.MenuBar = false;
			bar.ShowToolTips = true;
			bar.CanCustomize = GetBoolFromAttribute(node, "allowcustomizing");
			bar.CanHide = true;
			bar.CanUndock = m_allowUndocking;
			bar.DockOffset = 0;
			bar.WrapItemsDock = false;
			bar.WrapItemsFloat = false;
			bar.ItemsContainer.Style = eDotNetBarStyle.OfficeXP;
			bar.GrabHandleStyle = eGrabHandleStyle.StripeFlat;

			Point barLoc = new Point(0, 0);
			barLoc.X = GetIntFromAttribute(node, "position", 0);
			barLoc.Y = GetIntFromAttribute(node, "row", m_dnbMngr.Bars.Count);
			m_htBarLocations[bar] = barLoc;

			string barText = GetAttributeValue(node, "text");
			barText = GetStringFromResource(barText);
			TMBarProperties barProps = new TMBarProperties(barName, barText, true,
				GetBoolFromAttribute(node, "visible", true), m_parentForm);

			if (InitializeBar != null)
				InitializeBar(ref barProps);

			barProps.Update = true;
			SetBarProperties(bar, barProps);

			return bar;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a toolbar item with the specified name. If the item exists, then return the
		/// one found in the DNB manager. Otherwise, create a new one.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected BaseItem GetToolbarItem(XmlNode node, string name)
		{
			BaseItem item;

			if (m_barReadFromSettingFile)
			{
				item = m_dnbMngr.GetItem(name, true);
				if (item != null)
					return item;
			}

			int type = GetIntFromAttribute(node, "type", 0);

			// Get nasty if the type in the XML definition is bad.
			if (type < 0 || type > 5)
				throw new Exception(type.ToString() + " is an invalid toolbar item type.");

			switch (type)
			{
				case 4:
					item = new ComboBoxItem(name);
					ComboBoxItem cboItem = item as ComboBoxItem;
					cboItem.ComboWidth = GetIntFromAttribute(node, "width", 25);

					bool ownerDraw = GetBoolFromAttribute(node, "dnboverridedrawing");
					cboItem.ComboBoxEx.DisableInternalDrawing = ownerDraw;

					// Setting the height to 1 will will force height to minimum (but it won't be 1).
					cboItem.ItemHeight = (!ownerDraw ? 1 : cboItem.ComboBoxEx.Font.Height + 1);
					break;

				case 5:
					item = new ControlContainerItem(name);
					item.CanCustomize = false;
					break;

				default:
					// The rest of the types are Button types with various ways of dealing
					// with items that have a popup component.
					item = new SilButtonItem(name);
				switch (type)
				{
					case 1: ((ButtonItem)item).PopupType = ePopupType.ToolBar; break;
					case 2:	((ButtonItem)item).PopupType = ePopupType.Menu;	break;
					case 3: ((ButtonItem)item).PopupType = ePopupType.Container; break;
				}

					break;
			}

			return item;
		}

		#endregion

		#region Methods for reading XML definition for single toolbar/menu item
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Read the information for a single menu item from the specified node.
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected BaseItem ReadSingleItem(XmlNode node, bool isMenuItem)
		{
			string name = GetAttributeValue(node, "name");

			// Give the item a guid for the name if one wasn't found in the XML def.
			if (name == null || name == string.Empty)
				name = Guid.NewGuid().ToString();

			// If the item is for a menu just make a ButtonItem since that's all we allow
			// on menus. Otherwise it must be a toolbar item, so go make the appropriate
			// type of toolbar item.
			BaseItem item =
				(isMenuItem ? new SilButtonItem(name) : GetToolbarItem(node, name));

			InitItem(node, item, name, isMenuItem);
			AddToCustomizationCollection(node, item, isMenuItem);
			return item;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method initializes a toolbar or menu item's properties.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="item">Item to be initialized.</param>
		/// <param name="name"></param>
		/// <param name="isMenuItem"></param>
		/// ------------------------------------------------------------------------------------
		private void InitItem(XmlNode node, BaseItem item, string name, bool isMenuItem)
		{
			string commandid = GetAttributeValue(node, "commandid");
			bool visible = GetBoolFromAttribute(node, "visible", true);
			bool begingroup = GetBoolFromAttribute(node, "begingroup");

			item.Tag = commandid;
			item.Name = name;
			item.AccessibleName = name;

			TMItemProperties itemProps = new TMItemProperties();
			itemProps.ParentForm = m_parentForm;
			itemProps.Name = name;
			itemProps.CommandId = commandid;
			itemProps.Enabled = true;

			// If settings are restored from a settings file, then we don't want to clobber
			// the item's begin group value by getting an item's default begin group value
			// from the XML definition file. This is because the user may have customized
			// his toolbar item.
			itemProps.BeginGroup = (m_barReadFromSettingFile ? item.BeginGroup : begingroup);

			// If settings are restored from a settings file, then we don't want to clobber
			// the item's visible state by getting an item's default visible state from the
			// XML definition file. This is because the user may have customized his toolbar
			// item to hide items.
			itemProps.Visible = (m_barReadFromSettingFile ? item.Visible : visible);

			CommandInfo cmdInfo = m_htCommandInfo[commandid] as CommandInfo;
			if (cmdInfo != null)
			{
				itemProps.Text = cmdInfo.Text;
				itemProps.Category = cmdInfo.Category;
				itemProps.Tooltip = cmdInfo.ToolTip;
				itemProps.Image = cmdInfo.Image;

				if (cmdInfo.ShortcutKey != eShortcut.None && isMenuItem)
					item.Shortcuts.Add(cmdInfo.ShortcutKey);
			}

			if (GetBoolFromAttribute(node, "toolbarlist"))
				m_toolbarListItem = (item as ButtonItem);

			if (GetBoolFromAttribute(node, "toolbarcustomizeitem"))
				m_toolbarCustomizeItem = (item as ButtonItem);

			if (GetBoolFromAttribute(node, "windowlist"))
				m_windowListItem = (item as ButtonItem);

			if (GetBoolFromAttribute(node, "morewindowsitem"))
				m_moreWindowItem = (item as ButtonItem);

			ComboBoxItem cboItem = item as ComboBoxItem;
			if (cboItem != null)
			{
				cboItem.ComboBoxEx.DisableInternalDrawing =	GetBoolFromAttribute(node, "dnboverridedrawing");
				cboItem.ComboBoxEx.PreventEnterBeep = true;
				cboItem.AccessibleRole = AccessibleRole.ComboBox;
				itemProps.Control = cboItem.ComboBoxEx as ComboBox;
			}

			// Let the application have a stab at initializing the item.
			if (InitializeItem != null)
				InitializeItem(ref itemProps);

			// Save all initializatons by updating the item.
			itemProps.Update = true;
			SetItemProps(item, itemProps);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// At this point, the item passed to this method has not been added to any toolbar or
		/// menu. Before doing so, we need to add it to the DNB Manager because that internally
		/// (to the DNB manager) assigns the item to its category and makes it available for
		/// customization purposes through DNB's customize dialog. But, only add an item if
		/// there is a category for the item and if the item is not a ControlContainerItem.
		/// We don't allow ControlContainerItems because it appears DotNetBar's customize
		/// dialog doesn't allow dragging new copies of ControlContainerItems to user-defined
		/// toolbars. And if they don't allow that, I don't want them in the list appearing
		/// like they should be able to be dragged to new toolbars. This means a DotNetBar
		/// manager may only have one (visible - See comments in
		/// HandleControlContainerLoadRequests) instance of application-defined toolbar items.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="item"></param>
		/// <param name="isMenuItem"></param>
		/// ------------------------------------------------------------------------------------
		private void AddToCustomizationCollection(XmlNode node, BaseItem item, bool isMenuItem)
		{
			if (!(item is ButtonItem) || item.Category == null ||
				item.Category == string.Empty || m_readingContextMenuDef)
			{
				return;
			}

			string replaceItemCmdId = GetAttributeValue(node, "replacecustomizeitem");
			bool addToCustomizeList = GetBoolFromAttribute(node, "customizeitem", true);

			// Only add menu items unless a toolbar item's XML definition contains one of the
			// attributes: customizeitem or replacecustomizeitem.
			if ((!isMenuItem && replaceItemCmdId == null) || !addToCustomizeList)
				return;

			// If this item should replace another in the customization item collection,
			// find the item it should replace and get rid of it.
			if (replaceItemCmdId != null)
			{
				for (int i = 0; i < m_dnbMngr.Items.Count; i++)
				{
					if (replaceItemCmdId == (m_dnbMngr.Items[i].Tag as string))
					{
						m_dnbMngr.Items.Remove(m_dnbMngr.Items[i]);
						break;
					}
				}
			}

			BaseItem itemCopy = item.Copy();

			// Check if the text for the copy should be different from the original's.
			CommandInfo cmdInfo = GetCommandInfo(item);
			if (cmdInfo.TextAlt != null)
				itemCopy.Text = cmdInfo.TextAlt;

			// If the item already exists (for some odd reason) in the manager's collection
			// then remove it first since the manager doesn't like two items with the same name.
			if (m_dnbMngr.Items.Contains(itemCopy.Name))
				m_dnbMngr.Items.Remove(itemCopy.Name);

			m_dnbMngr.Items.Add(itemCopy);
		}

		#endregion

		#region Event Handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This gets called when the DotNetBar requests a localized version of a string for
		/// displaying in places like the dialog that lets the user configure toolbars (e.g.
		/// show/hide toolbars, add new ones, etc.).
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void HandleGettingLocalizedString(object sender, LocalizeEventArgs e)
		{
			e.Handled = false;

			string kstid = (string)m_htSystemStringIds[e.Key];
			string localizedStr = m_rmlocalMngmntStrings.GetString(kstid).Trim();

			if (localizedStr != null && localizedStr != string.Empty)
			{
				e.LocalizedValue = localizedStr;
				e.Handled = true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method handles clicks on toolbar/menu items.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void HandleItemClicks(object sender, EventArgs e)
		{
			/// If there is a popup visible, our assumption is the item clicked on was on the
			/// popup so we need to close the popup. DNB doesn't always close the popup
			/// automatically for us. Therefore, we'll ensure it gets done.

			if (m_menuCurrentlyPoppedUp != null)
			{
				m_menuCurrentlyPoppedUp.ClosePopup();
				m_menuCurrentlyPoppedUp = null;
			}

			BaseItem item = sender as BaseItem;
			string message = GetItemsCommandMessage(item);

			// Ignore the click on the toolbar list item because it has sub menu items. It has
			// a command ID that's used by it's sub items and for that reason, message isn't
			// null. But when the item is the toolbar list item, we have to ignore the click
			// and that's why we test it explicitly here. Confusing?
			if (item == null || item == m_toolbarListItem || item.SystemItem ||
				message == string.Empty || m_msgMediator == null)
			{
				return;
			}

			if (item == m_toolbarCustomizeItem)
				m_dnbMngr.Customize();
			else
			{
				TMItemProperties itemProps = GetItemProps(item);

				// If the user clicked on one of the windows in the window list, then save the
				// index of that window in the item properties tag.
				if (m_windowListItem != null && (string)item.Tag == (string)m_windowListItem.Tag)
					GetWindowIndex((ButtonItem)item, itemProps);

				// If the user clicked on one of the toolbar items in the toolbar menu list
				// then save the toolbar's name in the tag property.
				if (item.Name.EndsWith(kToolbarItemSuffix))
					itemProps.Tag = item.Name.Replace(kToolbarItemSuffix, string.Empty);

				if (m_msgMediator.SendMessage(message, itemProps) && itemProps.Update)
					SetItemProperties(item.Name, itemProps);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// While the system is idle (or about to become so), this method will go through all
		/// the toolbar items and make sure they are enabled properly for the current view.
		/// Then it will go through the menu items with shortcuts to make sure they're enabled
		/// properly. (This is only done every 0.75 seconds.)
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void HandleItemUpdates(object sender, EventArgs e)
		{
			if (m_msgMediator == null || !AllowUpdates || m_customizeMenuShowing ||
				m_dnbMngr.Bars == null || DateTime.Now < m_itemUpdateTime.AddSeconds(0.75))
				return;

			m_itemUpdateTime = DateTime.Now;

			// Loop through all the toolbars and items on toolbars.
			foreach (Bar bar in m_dnbMngr.Bars)
			{
				if (bar.Items != null && !bar.MenuBar && bar.Visible)
				{
					foreach (BaseItem item in bar.Items)
						CallItemUpdateHandler(item);
				}
			}

			// Update menus with shortcut keys.
			for (int i = 0; i < m_menusWithShortcuts.Count; i++)
				CallItemUpdateHandler(m_menusWithShortcuts[i] as BaseItem);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Trap this event so we can clear the toolbar list item when it closes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void HandleToolBarListMenuClosing(object sender, EventArgs e)
		{
			ButtonItem item = sender as ButtonItem;

			if (item == null)
				return;

			// Make sure there aren't any items in the list. Then add an empty item so the
			// toolbar list item will have an arrow next to it when it becomes visible to
			// the user.
			for (int i = item.SubItems.Count - 1; i >= 0; i--)
			{
				if (item.SubItems[i] == m_toolbarCustomizeItem)
					continue;

				item.SubItems.RemoveAt(i);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clear out the window list after it's parent closes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void HandleWindowMenuClosing(object sender, EventArgs e)
		{
			ButtonItem wndListParent = sender as ButtonItem;
			if (wndListParent == null)
				return;

			int wndListIndex = wndListParent.SubItems.IndexOf(m_windowListItem);

			// Remove all the window list items (except the first one).
			for (int i = wndListParent.SubItems.Count - 1; i > wndListIndex; i--)
			{
				// Don't delete the "More Windows..." options.
				if (wndListParent.SubItems[i] == m_moreWindowItem)
					continue;

				wndListParent.SubItems.Remove(i);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles clicks on the drop-down portion of a popup button (i.e. a click on the
		/// arrow portion of a two-segmented toolbar button).
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void HandleItemsPopup(object sender, EventArgs e)
		{
			ButtonItem item = sender as ButtonItem;
			if (item == null || m_msgMediator == null)
				return;

			string message = GetItemsCommandMessage(item);

			if (message != string.Empty)
			{
				ToolBarPopupInfo popupInfo = new ToolBarPopupInfo(item.Name);
				if (m_msgMediator.SendMessage("DropDown" + message, popupInfo))
				{
					if (popupInfo.Control == null)
						return;

					// Load the returned control into the container provided by the popup item.
					PopupContainerControl container = item.PopupContainerControl as PopupContainerControl;
					container.Controls.Add(popupInfo.Control);
					popupInfo.Control.Location = container.ClientRectangle.Location;
					container.ClientSize = popupInfo.Control.Size;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method gets called whenever a customize menu (i.e. the menu that pops-up when
		/// clicking or hovering over the "Add or Remove Buttons" item) opens or closes. When
		/// the menu opens, a flag is set so item updates are not performed (in the
		/// HandleItemUpdates method). I found that when the menu popped-up and HandleItemUpdates
		/// iterated through the items, sometimes the iterating through the items seemed to
		/// grab some of the items on the customize menu. Iterating through the items the way
		/// I did in HandleItemUpdates should never have returned items that show up on the
		/// customize menu, but it happened and I think there's a DNB bug. This works around it.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void HandleCustomizeMenuPopup(object sender, EventArgs e)
		{
			CustomizeItem item = sender as CustomizeItem;
			if (item != null)
				m_customizeMenuShowing = item.Expanded;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This catches all item customize events and only cares about the ones where the
		/// user is adding custom toolbars. By default, when adding custom toolbars, the
		/// DotNetBar manager makes them floating and puts them in the upper left corner of
		/// the screen. I thought that was not very user friendly so I trap
		/// the addition of new toolbars and dock them to the top of the application window.
		/// </summary>
		/// <param name="sender">Item being customized.</param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void HandleUserCustomize(object sender, EventArgs e)
		{
			Bar bar = sender as Bar;

			if (bar != null && bar.Name == string.Empty && bar.CustomBar &&
				bar.Location.X == 0 && bar.Location.Y == 0 && bar.DockSide == eDockSide.None)
			{
				bar.Name = "tb" + bar.Text.Replace(" ", string.Empty);
				bar.DockLine = m_dnbMngr.Bars.Count;
				bar.DockSide = eDockSide.Top;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method gets called by the DotNetBar manager when it needs a control to load
		/// into a ControlContainerItem. The adapter then, fires the
		/// LoadControlContainerItem event to allow applications to give the adapter an
		/// application-defined control.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void HandleControlContainerLoadRequests(object sender, EventArgs e)
		{
			ControlContainerItem item = sender as ControlContainerItem;

			// Do nothing if the sender isn't a ControlContainerItem.
			if (item == null)
				return;

			// DotNetBar (DNB) is a little quirky with regard to controls on ControlContainerItems
			// and how it relates to it's need for category items. Here is what I've determined.
			// DNB needs a couple copies of each toolbar/menu item. One is for the item the user
			// sees on the toolbar, and the other is used for the customize menu (seen when clicking
			// on the "Add or Remove Buttons" button) and the customization dialog (in the commands
			// list by category). That means for ControlContainerItems, DNB will make at least two
			// calls to delegates of its ContainerLoadControl event, one for the item that shows up
			// on the toolbar and the other for customization purposes. Furthermore, it expects to
			// receive a new instance of the control for each call to ContainerLoadControl. It
			// won't work to send the same instance of a custom control for each
			// ContainerLoadControl event. So, for example, if I create a zoom combo
			// ControlContainerItem	and I subscribe to the ContainerLoadControl event, I would
			// have to make two copies of the zoom combo, each of which is returned when I get
			// the ContainerLoadControl events for the ControlContainerItem that hosts the zoom
			// combo box.
			//
			// Since it's unrealistic to expect an application using an adapter to understand all
			// this, we keep a hash table of controls (with the item name as the key) with an
			// entry for each ControlContainerItem in the DNB manager. If we receive a
			// ContainerLoadControl event from the DNB manager, check to see if the control for
			// that item is already in the hash table, if it's not, request a control from the
			// application using the adapter's LoadControlContainerItem event. Otherwise, copy
			// the item (that makes a deep copy of the item which means it also makes a copy of
			// the control the item contains) and return the copy's control instead of requesting
			// another instance from the application. Whew!
			if (m_htControls[item.Name] != null)
			{
				ControlContainerItem itemCopy = (ControlContainerItem)item.Copy();
				item.Control = itemCopy.Control;
			}
			else if (LoadControlContainerItem != null)
			{
				item.Control = LoadControlContainerItem(item.Name, null);
				m_htControls[item.Name] = item.Control;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When a menu item is popped-up, then cycle through the subitems and call update
		/// handlers.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void HandleMenuPopups(object sender, PopupOpenEventArgs e)
		{
			if (CancelMenuPopup())
			{
				e.Cancel = true;
				return;
			}

			ButtonItem item = sender as ButtonItem;

			// Item must be on a menu bar or menu or it must be a context menu.
			if (item == null || item.SystemItem || m_msgMediator == null ||
				(!item.IsOnMenu && !item.IsOnMenuBar && !m_dnbMngr.ContextMenus.Contains(item)))
			{
				return;
			}

			// If we're popping-up the toolbar list, then build it.
			if (item == m_toolbarListItem)
				BuildToolbarList();

			bool buildWindowList = false;
			foreach (ButtonItem subitem in item.SubItems)
			{
				if (subitem == m_windowListItem)
					buildWindowList = true;
				else
					CallItemUpdateHandler((BaseItem)subitem);
			}

			// Now that we've iterated through the subitems, if we found the window list
			// item or toolbar list item, build those lists.
			if (buildWindowList)
				BuildWindowList();

			// Turn off tooltips for context menus. Gets turned back on when popup is closed.
			if (m_dnbMngr.ContextMenus.Contains(item))
				m_dnbMngr.ShowToolTips = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether or not a menu should be allowed to be popped-up (See TE-6553).
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private bool CancelMenuPopup()
		{
			// Disallow popups if there is a modal form showing that is
			// a form other than the current parent form.
			if (!m_parentForm.Modal)
			{
				foreach (Form frm in Application.OpenForms)
				{
					if (frm.Modal)
						return true;
				}
			}

			// ...or if the current form is disabled.
			return (!m_parentForm.Enabled);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void BuildToolbarList()
		{
			TMBarProperties[] barProps = BarInfoForViewMenu;

			// The only subitem on the toolbar menu should be a customize item. Therefore,
			// when adding the items for each of the toolbars, we start at the end of the
			// list and keep inserting items at the beginning.
			for (int i = barProps.Length - 1; i >= 0; i--)
			{
				SilButtonItem item = new SilButtonItem();
				item.Name = barProps[i].Name + kToolbarItemSuffix;
				item.Text = barProps[i].Text;
				item.Checked = barProps[i].Visible;
				item.Tag = m_toolbarListItem.Tag;
				m_toolbarListItem.SubItems.Add(item, 0);
			}
		}
		#endregion

		#region Method for building window list
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="itemProps"></param>
		/// ------------------------------------------------------------------------------------
		private void BuildWindowList()
		{
			string message = GetItemsCommandMessage(m_windowListItem);
			if (message == string.Empty || m_msgMediator == null)
				return;

			// Call the update handler in order to get back the list of window items.
			TMItemProperties itemProps = GetItemProps(m_windowListItem);
			WindowListInfo wndListInfo = new WindowListInfo();
			wndListInfo.WindowListItemProperties = itemProps;

			if (!m_msgMediator.SendMessage("Update" + message, wndListInfo))
				return;

			if (wndListInfo == null || wndListInfo.WindowListItemProperties == null)
				return;

			ButtonItem wndListParent = m_windowListItem.Parent as ButtonItem;
			if (wndListParent == null)
				return;

			ArrayList wndList = wndListInfo.WindowListItemProperties.List;
			if (wndList == null || wndList.Count == 0 || !wndListInfo.WindowListItemProperties.Update)
				return;

			// The window list only allows up to 10 items (i.e. 0 - 9) so make the
			// "More Windows..." option visible when there are more than 10.
			m_moreWindowItem.Visible = (wndList.Count > 10);

			m_htWndListIndices = new Hashtable();

			// Add the first window list item.
			string text = wndList[0] as string;
			if (text != null)
			{
				m_htWndListIndices[m_windowListItem] = 0;
				m_windowListItem.Text = "&1 " + text;
				m_windowListItem.Checked = (wndListInfo.CheckedItemIndex == 0);
			}

			// Get the index of the first item in the window list.
			int wndListIndex = wndListParent.SubItems.IndexOf(m_windowListItem);

			// Add the rest of the window list items, up to 9 more.
			for (int i = 1; i < wndList.Count && i < 10; i++)
			{
				text = wndList[i] as string;
				if (text != null)
				{
					SilButtonItem newItem = new SilButtonItem(m_windowListItem.Name + i,
						"&" + (i + 1).ToString() + " " + text);
					m_htWndListIndices[newItem] = i;
					newItem.Tag = m_windowListItem.Tag;
					newItem.Checked = (wndListInfo.CheckedItemIndex == i);
					wndListParent.SubItems.Add(newItem, wndListIndex + i);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="item"></param>
		/// <param name="itemProps"></param>
		/// ------------------------------------------------------------------------------------
		protected void GetWindowIndex(ButtonItem item, TMItemProperties itemProps)
		{
			itemProps.Tag = (m_htWndListIndices[item] != null ? (int)m_htWndListIndices[item] : -1);
		}

		#endregion

		#region Method for calling update command handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is a helper method for the HandleMenuPopups and HandleItemUpdates methods. It
		/// accepts a single DNB item and calls it's update handler, if there is one.
		/// </summary>
		/// <param name="item"></param>
		/// ------------------------------------------------------------------------------------
		protected void CallItemUpdateHandler(BaseItem item)
		{
			string message = GetItemsCommandMessage(item);
			if (message == string.Empty || item.SystemItem || m_msgMediator == null)
				return;

			TMItemProperties itemProps = GetItemProps(item);

			// If the item being updated is one of the toolbar items in the toolbar menu
			// list then save the toolbar's name in the tag property.
			if (item.Name.EndsWith(kToolbarItemSuffix))
				itemProps.Tag = item.Name.Replace(kToolbarItemSuffix, string.Empty);

			// Call update method (e.g. OnUpdateEditCopy). If that method doesn't exist or
			// if all update methods return false, we check for the existence of the
			// command handler.
			if (m_msgMediator.SendMessage("Update" + message, itemProps))
				SetItemProps(item, itemProps);
			else
			{
				// If the item is a menu item with sub items, then don't disable it.
				// Menu items with sub items often don't have receivers so we shouldn't
				// assume it should be disabled just because it doesn't have a receiver.
				if ((item.IsOnMenuBar || item.IsOnMenu) && item.SubItems.Count > 0)
					return;

				// If the item is not a menuIf there's no receiver for this item then automatically disable it.
				item.Enabled = m_msgMediator.HasReceiver(message);
			}
		}

		#endregion

		#region Methods for Getting/Setting menu/toolbar item properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads a TMItemProperties object with several properties from a toolbar item.
		/// </summary>
		/// <param name="name">The Name of the item whose properties are being stored.</param>
		/// <returns>The properties of a menu or toolbar item.</returns>
		/// ------------------------------------------------------------------------------------
		public TMItemProperties GetItemProperties(string name)
		{
			return GetItemProps(m_dnbMngr.GetItem(name, true));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads a TMItemProperties object with several properties from a toolbar item.
		/// </summary>
		/// <param name="item">The item whose properties are being stored.</param>
		/// <returns>The properties of a menu or toolbar item.</returns>
		/// ------------------------------------------------------------------------------------
		private TMItemProperties GetItemProps(BaseItem item)
		{
			TMItemProperties itemProps = new TMItemProperties();

			// Set default values.
			itemProps.Update = false;
			itemProps.Name = string.Empty;
			itemProps.Text = string.Empty;
			itemProps.OriginalText = string.Empty;
			itemProps.Font = null;
			itemProps.Category = string.Empty;
			itemProps.Tooltip = string.Empty;
			itemProps.Enabled = false;
			itemProps.Visible = false;
			itemProps.Checked = false;
			itemProps.Image = null;
			itemProps.CommandId = null;
			itemProps.Control = null;
			itemProps.List = null;
			itemProps.ParentForm = m_parentForm;

			if (item == null)
				return itemProps;

			itemProps.Name = item.Name;
			itemProps.Text = item.Text;
			itemProps.CommandId = item.Tag as string;
			itemProps.Category = item.Category;
			itemProps.Tooltip = item.Tooltip;
			itemProps.Enabled = item.Enabled;
			itemProps.Visible = item.Visible;
			itemProps.BeginGroup = item.BeginGroup;
			itemProps.IsDisplayed = item.Displayed;
			itemProps.Tag = m_htItemTags[item];

			CommandInfo cmdInfo = GetCommandInfo(item);
			if (cmdInfo != null)
			{
				if (cmdInfo.Message != null)
					itemProps.Message = cmdInfo.Message;
				if (cmdInfo.Text != null)
					itemProps.OriginalText = cmdInfo.Text;
			}

			if (item is ButtonItem)
			{
				itemProps.Checked = ((ButtonItem)item).Checked;
				itemProps.Image = ((ButtonItem)item).Image;

				if (item is SilButtonItem)
					itemProps.Font = ((SilButtonItem)item).Font;
			}
			else if (item is ComboBoxItem)
			{
				ComboBoxItem cboItem = item as ComboBoxItem;
				itemProps.Control = cboItem.ComboBoxEx as ComboBox;
				if (cboItem.Items.Count > 0)
				{
					// Get all the combo items and save in the List property.
					itemProps.List = new ArrayList();
					for (int i = 0; i < cboItem.Items.Count; i++)
						itemProps.List.Add(cboItem.Items[i]);
				}
			}
			else if (item is ControlContainerItem)
				itemProps.Control = ((ControlContainerItem)item).Control;

			//REVIEW: should this return null if item is null?
			return itemProps;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets some of a toolbar item's properties. Note: if the Update property in the
		/// TMItemProperties object hasn't been set to true, no updating will occur.
		/// </summary>
		/// <param name="name">Name of item whose properties are being updated.</param>
		/// <param name="itemProps">The TMItemProperties containing the new property
		/// values for the toolbar item.</param>
		/// ------------------------------------------------------------------------------------
		public void SetItemProperties(string name, TMItemProperties itemProps)
		{
			SetItemProps(m_dnbMngr.GetItem(name, true), itemProps);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets some of a menu/toolbar item's properties. Note: if the Update property in the
		/// TMItemProperties object hasn't been set to true, no updating will occur. Items that
		/// can be set are: Text, Category, Tooltip, Enabled, Visible, BeginGroup, Checked,
		/// Image, List, Tag, and CommandId;
		/// </summary>
		/// <param name="item">The item whose properties are being updated.</param>
		/// <param name="itemProps">The TMItemProperties containing the new property
		/// values for the toolbar item.</param>
		/// ------------------------------------------------------------------------------------
		private void SetItemProps(BaseItem item, TMItemProperties itemProps)
		{
			if (item == null || !itemProps.Update)
				return;

			if (item.Tag as string != itemProps.CommandId)
			{
				item.Tag = itemProps.CommandId;

				// Since we just changed the command ID, we should change the item's
				// image if it's a button item and the image isn't already being
				// specified in the item properties.
				if (itemProps.Image == null && item is ButtonItem)
				{
					CommandInfo cmdInfo = GetCommandInfo(item);
					if (cmdInfo != null && cmdInfo.Image != null)
						((ButtonItem)item).Image = cmdInfo.Image;
				}
			}

			m_htItemTags[item] = itemProps.Tag;

			// Update all the changed fields only if necessary.
			if (item.Text != itemProps.Text)
				item.Text = itemProps.Text;

			SilButtonItem silButtonItem = item as SilButtonItem;
			if (silButtonItem != null)
				silButtonItem.Font = itemProps.Font;

			if (item.Category != itemProps.Category)
				item.Category = itemProps.Category;

			if (item.Tooltip != itemProps.Tooltip)
				item.Tooltip = itemProps.Tooltip;

			if (item.Enabled != itemProps.Enabled)
				item.Enabled = itemProps.Enabled;

			if (item.Visible != itemProps.Visible)
				item.Visible = itemProps.Visible;

			if (item.BeginGroup != itemProps.BeginGroup)
				item.BeginGroup = itemProps.BeginGroup;

			SetButtonItemSpecificProperties(item as ButtonItem, itemProps);
			SetComboItemSpecificProperties(item as ComboBoxItem, itemProps);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the properties for a ButtonItem type toolbar button.
		/// </summary>
		/// <param name="item">The item whose properties are being updated.</param>
		/// <param name="itemProps">The TMItemProperties containing the new property
		/// values for the toolbar item.</param>
		/// ------------------------------------------------------------------------------------
		private void SetButtonItemSpecificProperties(ButtonItem item,
			TMItemProperties itemProps)
		{
			if (item == null)
				return;

			if (item.Checked != itemProps.Checked)
				item.Checked = itemProps.Checked;

			if (itemProps.Image != item.Image && itemProps.Image != null)
				item.Image = itemProps.Image;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the items in the Items collection for a ComboBoxItem type toolbar item.
		/// </summary>
		/// <param name="item">The combo box item whose Items collection is be updated.</param>
		/// <param name="itemProps">The TMItemProperties containing the new property
		/// values for the toolbar item.</param>
		/// ------------------------------------------------------------------------------------
		private void SetComboItemSpecificProperties(ComboBoxItem item,
			TMItemProperties itemProps)
		{
			if (item == null)
				return;

			// First check if the lists are the same. If they are we don't want to
			// go to the trouble of rebuilding the list, especially since that will
			// cause unnecessary flicker.
			if (itemProps.List != null && itemProps.List.Count == item.Items.Count)
			{
				bool fAreSame = true;
				for (int i = 0; i < item.Items.Count || !fAreSame; i++)
					fAreSame = (item.Items[i] == itemProps.List[i]);

				if (fAreSame)
					return;
			}

			item.Items.Clear();

			// If there are item's in the list then upate the combobox item's
			// collection of items.
			if (itemProps.List != null && itemProps.List.Count > 0)
			{
				for (int i = 0; i < itemProps.List.Count; i++)
					item.Items.Add(itemProps.List[i]);
			}
		}

		#endregion

		#region Misc. Helper Methods and Property
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		internal CommandInfo GetCommandInfo(BaseItem item)
		{
			if (item != null)
			{
				string commandId = item.Tag as string;
				if (commandId != null)
					return m_htCommandInfo[commandId] as CommandInfo;
			}

			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an item's command handling message from the appropriate hash table entry.
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected string GetItemsCommandMessage(BaseItem item)
		{
			string message = string.Empty;

			CommandInfo cmdInfo = GetCommandInfo(item);
			if (cmdInfo != null && cmdInfo.Message != null)
				message = cmdInfo.Message;

			return message;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="kstid"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected string GetStringFromResource(string kstid)
		{
			if (kstid == null || kstid.Trim() == string.Empty)
				return null;

			string localizedStr = kstid;

			for (int i = 0; i < m_rmlocalStrings.Count; i++)
			{
				localizedStr = ((ResourceManager)m_rmlocalStrings[i]).GetString(kstid);
				if (localizedStr != null)
					break;
			}

			if (localizedStr == null || localizedStr == string.Empty)
				localizedStr = kstid;

			return localizedStr.Trim();
		}

		#endregion

		#region ITMAdapter AddMenuItem and RemoveSubitems implementation
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a new submenu item to the menu specified by parentItemName and inserts it
		/// before the item specified by insertBeforeItem. If insertBeforeItem is null, then
		/// the new submenu item is added to the end of parentItemName's menu collection.
		/// </summary>
		/// <param name="itemProps">Properties of the new menu item.</param>
		/// <param name="parentItemName">Name of the menu item that will be added to.</param>
		/// <param name="insertBeforeItem">Name of the submenu item before which the new
		/// menu item will be added.</param>
		/// ------------------------------------------------------------------------------------
		public void AddMenuItem(TMItemProperties itemProps, string parentItemName, string insertBeforeItem)
		{
			SilButtonItem item = new SilButtonItem();
			item.Name = itemProps.Name;
			itemProps.Update = true;
			SetItemProps(item, itemProps);

			// If an item to insert before isn't specified, then add the item to the item to the
			// parent item specified. Otherwise, insert before "insertBeforeItem".
			if (insertBeforeItem == null || insertBeforeItem.Trim() == string.Empty)
				AddMenuItem(item, parentItemName);
			else
				InsertMenuItem(item, insertBeforeItem, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a new menu item to a context menu specified by contextMenuName and inserts it
		/// before the item specified by insertBeforeItem. If insertBeforeItem is null, then
		/// the new menu item is added to the end of parentItemName's menu collection.
		/// </summary>
		/// <param name="itemProps">Properties of the new menu item.</param>
		/// <param name="contextMenuName">Name of the context menu to which the item is added.
		/// </param>
		/// <param name="insertBeforeItem">Name of the context menu item before which the new
		/// menu item will be added.</param>
		/// ------------------------------------------------------------------------------------
		public void AddContextMenuItem(TMItemProperties itemProps, string contextMenuName,
			string insertBeforeItem)
		{
			AddContextMenuItem(itemProps, contextMenuName, contextMenuName, insertBeforeItem);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a new menu item to a context menu specified by contextMenuName and inserts it
		/// before the item specified by insertBeforeItem. If insertBeforeItem is null, then
		/// the new menu item is added to the end of parentItemName's menu collection. This
		/// overload allows new menu items to be added as submenus to menus at the top level
		/// of the context menu. The parentMenuName can be the name of a menu item at any
		/// level within the hierarchy of the menus on the context menu.
		/// </summary>
		/// <param name="itemProps">Properties of the new menu item.</param>
		/// <param name="contextMenuName">Name of the context menu to which the item is added.
		/// </param>
		/// <param name="parentMenuName">Name of the menu item in the context menu under which
		/// the new item is added.</param>
		/// <param name="insertBeforeItem">Name of the context menu item before which the new
		/// menu item will be added.</param>
		/// ------------------------------------------------------------------------------------
		public void AddContextMenuItem(TMItemProperties itemProps, string contextMenuName,
			string parentMenuName, string insertBeforeItem)
		{
			ButtonItem contextMenu = null;

			// First, make sure we can find the context menu to which the item will be added.
			int i = m_dnbMngr.ContextMenus.IndexOf(contextMenuName);
			if (i >= 0)
				contextMenu = m_dnbMngr.ContextMenus[i] as ButtonItem;

			if (i < 0 || contextMenu == null)
				return;

			SilButtonItem item = new SilButtonItem();
			item.Name = itemProps.Name;
			itemProps.Update = true;
			SetItemProps(item, itemProps);

			// Get the menu under which the new menu item will be added.
			ButtonItem parentMenu = FindSubMenu(contextMenu, parentMenuName);
			Debug.Assert(parentMenu != null);
			if (parentMenu == null)
				return;

			// If an item to insert before isn't specified, then add the item to the item to the
			// parent item specified. Otherwise, insert before "insertBeforeItem".
			if (insertBeforeItem == null || insertBeforeItem.Trim() == string.Empty)
				parentMenu.SubItems.Add(item);
			else
			{
				i = parentMenu.SubItems.IndexOf(insertBeforeItem);
				if (i >= 0)
					parentMenu.SubItems.Insert(i, item);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Search recursively through the a menu and any submenus for the specified menu name.
		/// </summary>
		/// <param name="parentMenu">The parent menu whose subitems will be searched.</param>
		/// <param name="menuToFind">The specified menu name to find.</param>
		/// <returns>the menu item that was found, or null if not found</returns>
		/// ------------------------------------------------------------------------------------
		private ButtonItem FindSubMenu(ButtonItem parentMenu, string menuToFind)
		{
			if (parentMenu.Name == menuToFind)
				return parentMenu;

			int i = parentMenu.SubItems.IndexOf(menuToFind);
			if (i >= 0)
				return parentMenu.SubItems[i] as ButtonItem;

			// Didn't find the menuToFind, so search through any subitems of this item...
			foreach (ButtonItem item in parentMenu.SubItems)
			{
				ButtonItem foundItem = FindSubMenu(item, menuToFind);
				if (foundItem != null)
					return foundItem; // found it!
			}

			return null; // item not found
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes the specified item from the specified parent menu.
		/// </summary>
		/// <param name="parentItemName">The name of the item whose subitem will be removed.
		/// </param>
		/// <param name="name">subitem to remove from parent menu.</param>
		/// <remarks>Removing submenu items on a menu, while leaving the parent menu intact
		/// has not been tested (and is not currently needed).</remarks>
		/// ------------------------------------------------------------------------------------
		public void RemoveMenuItem(string parentItemName, string name)
		{
			ButtonItem parentItem = (ButtonItem)m_menuBar.GetItem(parentItemName);
			if (parentItem != null && parentItem.SubItems.Contains(name))
				parentItem.SubItems.Remove(name);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes all the subitems of the specified menu.
		/// </summary>
		/// <param name="parentItemName">The name of the item whose subitems will be removed.
		/// </param>
		/// ------------------------------------------------------------------------------------
		public void RemoveMenuSubItems(string parentItemName)
		{
			ButtonItem item = (ButtonItem)m_menuBar.GetItem(parentItemName);
			if (item != null)
				item.SubItems.Clear();
		}

		#endregion

		#region ITMAdpater popup and context menu implementation (and then some)
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the context menu for a specified control.
		/// </summary>
		/// <param name="ctrl">Control which is being assigned a context menu.</param>
		/// <param name="name">The name of the context menu to assign to the control.</param>
		/// ------------------------------------------------------------------------------------
		public void SetContextMenuForControl(Control ctrl, string name)
		{
			m_dnbMngr.SetContextMenuEx(ctrl, name);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Pops-up a menu so it shows like a context menu. If the item doesn't have any
		/// sub items, then this command is ignored.
		/// </summary>
		/// <param name="name">The name of the item to popup. The name could be the name of
		/// a menu off the application's menu bar, or one of the context menu's added to the
		/// menu adapter.</param>
		/// <param name="x">The X location (on the screen) where the menu is popped-up.</param>
		/// <param name="y">The Y location (on the screen) where the menu is popped-up.</param>
		/// ------------------------------------------------------------------------------------
		public void PopupMenu(string name, int x, int y)
		{
			m_menuCurrentlyPoppedUp = m_dnbMngr.GetItem(name) as ButtonItem;

			if (m_menuCurrentlyPoppedUp == null || m_menuCurrentlyPoppedUp.SubItems.Count == 0)
				m_menuCurrentlyPoppedUp = m_dnbMngr.ContextMenus[name] as ButtonItem;

			if (m_menuCurrentlyPoppedUp == null || m_menuCurrentlyPoppedUp.SubItems.Count == 0)
				return;

			m_menuCurrentlyPoppedUp.PopupMenu(x, y);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Pops-up a menu so it shows like a context menu. If the item doesn't have any
		/// sub items, then this command is ignored.
		/// </summary>
		/// <param name="name">The name of the item to popup. The name could be the name of
		/// a menu off the application's menu bar, or one of the context menu's added to the
		/// menu adapter.</param>
		/// <param name="x">The X location (on the screen) where the menu is popped-up.</param>
		/// <param name="y">The Y location (on the screen) where the menu is popped-up.</param>
		/// <param name="subItemsToRemoveOnClose"></param>
		/// ------------------------------------------------------------------------------------
		public void PopupMenu(string name, int x, int y, List<string> subItemsToRemoveOnClose)
		{
			m_subItemsToRemoveOnMenuClose = subItemsToRemoveOnClose;
			PopupMenu(name, x, y);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the adapter's parent form. This will not cause docking sites to be added to
		/// the form and DotNetBar manager. This solves TE-6793
		/// </summary>
		/// <param name="newForm">The new parent form.</param>
		/// <returns>The adapter's previous parent form.</returns>
		/// ------------------------------------------------------------------------------------
		public Form SetParentForm(Form newForm)
		{
			Form prevForm = m_parentForm;
			m_dnbMngr.ParentForm = m_parentForm = newForm;
			return prevForm;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When a popup menu is closed, begin showing tooltips again. Also, if there were
		/// items to remove from the menu, remove them from the menu.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void HandleMenuPopupClose(object sender, EventArgs e)
		{
			m_dnbMngr.ShowToolTips = true;

			if (sender != m_menuCurrentlyPoppedUp)
				return;

			if (m_subItemsToRemoveOnMenuClose != null && m_menuCurrentlyPoppedUp != null)
			{
				foreach (string menuName in m_subItemsToRemoveOnMenuClose)
				{
					int i = m_menuCurrentlyPoppedUp.SubItems.IndexOf(menuName);
					Debug.Assert(i >= 0);
					m_menuCurrentlyPoppedUp.SubItems.RemoveAt(i);
				}

				m_subItemsToRemoveOnMenuClose.Clear();
				m_subItemsToRemoveOnMenuClose = null;
			}
		}

		#endregion

		#region ITMAdpater methods for Getting/Setting toolbar properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the properties of a toolbar.
		/// </summary>
		/// <param name="name">Name of the toolbar whose properties are being requested.
		/// </param>
		/// <returns>The properties of the toolbar.</returns>
		/// ------------------------------------------------------------------------------------
		public TMBarProperties GetBarProperties(string name)
		{
			Bar bar = m_dnbMngr.Bars[name];

			return (bar == null ? null :
				new TMBarProperties(name, bar.Text, bar.Enabled, bar.Visible, m_parentForm));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the properties of the main menu bar.
		/// object.
		/// </summary>
		/// <param name="barProps">Properties used to modfy the toolbar item.</param>
		/// ------------------------------------------------------------------------------------
		public void SetBarProperties(TMBarProperties barProps)
		{
			foreach (Bar bar in m_dnbMngr.Bars)
			{
				if (bar.MenuBar)
					SetBarProperties(bar, barProps);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets properties for a menu bar or toolbar.
		/// </summary>
		/// <param name="name">Name of bar to update.</param>
		/// <param name="barProps">New properties of bar.</param>
		/// ------------------------------------------------------------------------------------
		public void SetBarProperties(string name, TMBarProperties barProps)
		{
			SetBarProperties(m_dnbMngr.Bars[name], barProps);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets properties for a menu bar or toolbar.
		/// </summary>
		/// <param name="bar">Bar to update.</param>
		/// <param name="barProps">New properties of bar.</param>
		/// ------------------------------------------------------------------------------------
		private void SetBarProperties(Bar bar, TMBarProperties barProps)
		{
			if (bar == null || barProps == null || !barProps.Update)
				return;

			if (barProps.Text != bar.Text)
				bar.Text = barProps.Text;

			if (barProps.Enabled != bar.Enabled)
				bar.Enabled = barProps.Enabled;

			if (barProps.Visible != bar.Visible)
			{
				if (barProps.Visible)
					bar.Show();
				else
					bar.Hide();
			}
		}

		#endregion

		#region Misc. ITMAdapter methods for tool bars
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the control contained within a control container toolbar item.
		/// </summary>
		/// <param name="name">Name of the control container item.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public Control GetBarItemControl(string name)
		{
			ControlContainerItem item = (ControlContainerItem)m_dnbMngr.GetItem(name);
			return (item == null ? null : item.Control);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Allows an application to force the hiding of a toolbar item's popup control.
		/// </summary>
		/// <param name="name">Name of item whose popup should be hidden.</param>
		/// ------------------------------------------------------------------------------------
		public void HideBarItemsPopup(string name)
		{
			try
			{
				((ButtonItem)m_dnbMngr.GetItem(name)).ClosePopup();
			}
			catch
			{
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Causes the adapter to show it's dialog for customizing toolbars.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ShowCustomizeDialog()
		{
			m_dnbMngr.Customize();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Allows an application to hide of a toolbar.
		/// </summary>
		/// <param name="name">Name of toolbar to hide.</param>
		/// ------------------------------------------------------------------------------------
		public void HideToolBar(string name)
		{
			try
			{
				m_dnbMngr.Bars[name].Hide();
			}
			catch
			{
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Allows an application to show of a toolbar.
		/// </summary>
		/// <param name="name">Name of toolbar to show.</param>
		/// ------------------------------------------------------------------------------------
		public void ShowToolBar(string name)
		{
			try
			{
				m_dnbMngr.Bars[name].Show();
			}
			catch
			{
			}
		}

		#endregion

		#region XML Helper Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Skips over white space and any other junk that's not considered an element or
		/// end element.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected XmlNode ReadOverJunk(XmlNode node)
		{
			while (node != null &&
				(node.NodeType == XmlNodeType.Whitespace || node.NodeType == XmlNodeType.Comment))
			{
				node = node.NextSibling;
			}

			return node;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an attribute's value from the specified node.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="attribute"></param>
		/// <returns>String value of the attribute or null if it cannot be found.</returns>
		/// ------------------------------------------------------------------------------------
		protected string GetAttributeValue(XmlNode node, string attribute)
		{
			if (node == null || node.Attributes[attribute] == null)
				return null;

			return node.Attributes.GetNamedItem(attribute).Value.Trim();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="node"></param>
		/// <param name="attrValue"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool GetBoolFromAttribute(XmlNode node, string attribute)
		{
			return GetBoolFromAttribute(node, attribute, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="node"></param>
		/// <param name="attrValue"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool GetBoolFromAttribute(XmlNode node, string attribute, bool defaultValue)
		{
			string val = GetAttributeValue(node, attribute);
			return (val == null ? defaultValue : val.ToLower() == "true");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="attrValue"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected int GetIntFromAttribute(XmlNode node, string attribute, int defaultValue)
		{
			string val = GetAttributeValue(node, attribute);
			return (val == null ? defaultValue : int.Parse(val));
		}

		#endregion
	}

	#region SilButtonItem Class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Creates a button whose font can specified independent of any other ButtonItems.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	class SilButtonItem : ButtonItem
	{
		#region Member variables
		// It is less than ideal to hard code the margin for images when the ButtonItem is
		// on a menu. However, we can find no property DotNetBar provides that gives us
		// that value. Argh!
		private const int kImageMargin = 29;

		/// <summary>font to be used in the button</summary>
		private Font m_font = null;
		#endregion

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor for SilButtonItem
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SilButtonItem()
			: base()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor for SilButtonItem
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SilButtonItem(string name)
			: base(name)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor for SilButtonItem
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SilButtonItem(string name, string text)
			: base(name, text)
		{
		}
		#endregion

		#region Public properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the font to be used for the text in the button.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Font Font
		{
			get { return m_font; }
			set
			{
				m_font = value;
				if (value != null)
				{
					FontBold = value.Bold;
					FontItalic = value.Italic;
					FontUnderline = value.Underline;
				}
			}
		}

		#endregion

		#region Overridden methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When the font for the item is not the default font, then calclate our height
		/// based on the height of the font, plus 10% more for some padding.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void RecalcSize()
		{
			base.RecalcSize();
			if (m_font != null)
				Size = new Size(base.Size.Width, m_font.Height + (int)(m_font.Height * 0.1));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override the paint to make this item very distict from other menu items.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void Paint(ItemPaintArgs e)
		{
			if (m_font == null)
			{
				base.Paint(e);
				return;
			}

			Color clrFore = e.Colors.ItemText;
			Rectangle rc = DisplayRectangle;
			SolidBrush br = new SolidBrush(e.Colors.MenuBackground);
			e.Graphics.FillRectangle(br, rc);

			if (!IsMouseOver)
			{
				if (IsOnMenu)
				{
					// Paint the background color for the strip at the far left (or
					// right when the UI language is RTL) that contains the menu's image.
					rc.Width = kImageMargin - 6;
					br.Color = e.Colors.MenuSide;
					e.Graphics.FillRectangle(br, rc);
				}
			}
			else
			{
				// Paint the background color for the menu item when the mouse is over it.
				rc.Inflate(-1, 0);
				rc.Width--;
				rc.Height--;
				clrFore = e.Colors.ItemHotText;
				br.Color = e.Colors.ItemHotBackground;
				e.Graphics.FillRectangle(br, rc);

				using (Pen pen = new Pen(e.Colors.ItemHotBorder, 1))
					e.Graphics.DrawRectangle(pen, rc);
			}

			br.Dispose();
			rc = DisplayRectangle;
			TextFormatFlags flags = TextFormatFlags.EndEllipsis;

			if (IsOnMenu)
			{
				Control container = ContainerControl as Control;
				bool rtl = (container != null &&
					container.RightToLeft == System.Windows.Forms.RightToLeft.Yes);

				// Bump the text over so it's not painted over the image margin
				// and it lines up with the text on other menu items that are
				// painted using the default paint event.
				rc.Width -= kImageMargin;

				if (rtl)
					flags |= TextFormatFlags.Right;
				else
				{
					rc.X += kImageMargin;
					flags |= TextFormatFlags.Left;
				}
			}

			TextRenderer.DrawText(e.Graphics, Text, m_font, rc, clrFore, flags);
		}

		#endregion
	}

	#endregion
}

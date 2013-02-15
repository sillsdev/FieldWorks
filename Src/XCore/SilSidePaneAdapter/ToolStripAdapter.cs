using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Windows.Forms;
using System.Xml;
using Microsoft.Win32;
using SIL.FieldWorks.Common.UIAdapters;
using XCore;

namespace SIL.FieldWorks.Common.UIAdapters
{
	#region CommandInfo Class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="Image is a reference")]
	public class CommandInfo
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
		internal Keys ShortcutKey = Keys.None;
		internal Image Image = null;
	}

	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// The main class from which DotNetBar MenuAdapter and ToolBarAdapter classes are derived.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[SuppressMessage("Gendarme.Rules.Correctness", "DisposableFieldsShouldBeDisposedRule",
		Justification="Fields are either references or add to parent's Control collection and disposed there")]
	public class TMAdapter : ITMAdapter, IDisposable
	{
		#region Member variables
		// Used to store initial information about a toolstrip (i.e. read from the
		// registry or the xml file when there is no registry entries for them).
		protected struct InitialBarProps
		{
			internal string Name;
			internal Point Location;
			internal bool Visible;
			internal string Side;
		}

		protected const string kMainMenuName = "~MainMenu~";
		protected const string kToolbarItemSuffix = "~ToolbarItem~";
		protected const int kMaxWindowListItems = 10;

		protected MenuStrip m_menuBar = null;

		// This is true while we are reading the XML block of context menus.
		protected bool m_readingContextMenuDef = false;

		protected bool m_allowUndocking = true;
		protected string m_settingsFilePrefix = null;
		protected string[] m_settingFiles = null;
		protected Hashtable m_htWndListIndices = null;
		protected ContextMenuStrip m_menuCurrentlyPoppedUp = null;

		// We need to set up a ToolStripItemCollection containing all menus that have shortcuts.
		// Menu items with shortcuts must be updated as frequently as toolbar items (which happen
		// every .75 seconds) because if they don't, a menu item can be invoked using its shortcut
		// when it shouldn't be allowed or vice versa.
		protected ToolStripItemCollection m_menusWithShortcuts =
			new ToolStripItemCollection(new ToolStrip(), new ToolStripItem[] { });

		protected bool m_allowuUpdates = true;
		protected DateTime m_itemUpdateTime = DateTime.Now;
		protected Form m_parentForm;
		protected ToolStripContainer m_tsContainer;
		protected ToolStripPanel m_tsPanel;
		protected Mediator m_msgMediator;
		protected RegistryKey m_appsRegKeyPath;
		protected Dictionary<ToolStripItem, ToolStripSeparator> m_separators = new Dictionary<ToolStripItem, ToolStripSeparator>();
		protected Dictionary<ToolStrip, Point> m_barLocations = new Dictionary<ToolStrip, Point>();
		// TODO: need to dispose m_tmItems, m_bars, m_toolBarMenuMap, m_contextMenus and possibly others
		protected Dictionary<string, ToolStripItem> m_tmItems = new Dictionary<string, ToolStripItem>();
		protected Dictionary<string, ToolStrip> m_bars = new Dictionary<string, ToolStrip>();

		// Keeps track of the tool bars which are currently displayed
		protected Dictionary<string, bool> m_displayedBars = new Dictionary<string, bool>();
		protected Dictionary<ToolStripMenuItem, ToolStrip> m_toolBarMenuMap = new Dictionary<ToolStripMenuItem, ToolStrip>();
		protected Dictionary<string, ContextMenuStrip> m_contextMenus = new Dictionary<string, ContextMenuStrip>();

		protected Dictionary<ToolStripDropDown, ToolStripSeparator> m_hiddenSeparators =
			new Dictionary<ToolStripDropDown, ToolStripSeparator>();

		// Stores the item on the View menu that's the parent for the list of
		// toolbars.
		protected ToolStripMenuItem m_toolbarListItem = null;

		// Stores the item on the Window menu that's at the bottom of the window list
		// when there are more than 9 windows shown in the list.
		protected ToolStripMenuItem m_moreWindowItem = null;

		// Stores the item on the Window menu that is the first item in the list
		// of an application's open windows.
		protected ToolStripMenuItem m_windowListItem = null;

		// Resource Manager for localized toolbar and menu strings.
		protected List<ResourceManager> m_rmlocalStrings = new List<ResourceManager>();

		// Resource Manager for localized strings for customization, etc.
		protected ResourceManager m_rmlocalMngmntStrings;

		// Stores the TMItemProperties tag field for items that have one.
		protected Hashtable m_htItemTags = new Hashtable();

		// Stores all the images until we're done reading all the command ids.
		protected Dictionary<string, Image> m_images = new Dictionary<string, Image>();

		// Stores all the commands (and related information). The keys for this hash
		// table are the command id strings from the XML definition file.
		protected Dictionary<string, CommandInfo> m_commandInfo = new Dictionary<string, CommandInfo>();

		// This hash table stores hash tables. The keys for this hash table are form
		// types. A set of commands (i.e. those stored in the m_commandInfo) is saved
		// for each type of m_parentForm, not for each m_parentForm.
		protected static Hashtable m_htFormTypeCommands = new Hashtable();

		protected bool m_barReadFromSettingFile = false;
		protected Hashtable m_htSettingsFileLoaded = new Hashtable();
		protected bool m_allowPopupToBeCanceled = true;
		#endregion

		#region Disposable stuff
		#if DEBUG
		/// <summary/>
		~TMAdapter()
		{
			Dispose(false);
		}
		#endif

		/// <summary/>
		public bool IsDisposed
		{
			get;
			private set;
		}

		/// <summary/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		protected virtual void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (fDisposing && !IsDisposed)
			{
				// dispose managed and unmanaged objects
				var disposable = m_appsRegKeyPath as IDisposable;
				if (disposable != null)
					disposable.Dispose();

			}
			IsDisposed = true;
		}
		#endregion

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
		///
		/// </summary>
		/// <param name="parentForm"></param>
		/// <param name="msgMediator"></param>
		/// <param name="definitions"></param>
		/// <param name="appsRegKeyPath">Registry key path (under HKCU) where application's
		/// settings are stored (default is "Software\SIL\FieldWorks").</param>
		/// ------------------------------------------------------------------------------------
		public void Initialize(Form parentForm, Mediator msgMediator, string appsRegKeyPath,
			string[] definitions)
		{
			Initialize(parentForm, null, msgMediator, appsRegKeyPath, definitions);
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
			Initialize(parentForm, null, msgMediator, definitions);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="parentForm"></param>
		/// <param name="contentControl"></param>
		/// <param name="msgMediator"></param>
		/// <param name="appsRegKeyPath">Registry key path (under HKCU) where application's
		/// settings are stored (default is "Software\SIL\FieldWorks").</param>
		/// <param name="definitions"></param>
		/// ------------------------------------------------------------------------------------
		public void Initialize(Form parentForm, Control contentControl,
			Mediator msgMediator, string appsRegKeyPath, string[] definitions)
		{
			if (appsRegKeyPath != null)
			{
				appsRegKeyPath = appsRegKeyPath.Trim();

				if (appsRegKeyPath.StartsWith("HKEY_CURRENT_USER\\"))
				{
					m_appsRegKeyPath = Registry.CurrentUser;
					appsRegKeyPath = appsRegKeyPath.Replace("HKEY_CURRENT_USER\\", string.Empty);
				}
				else if (appsRegKeyPath.StartsWith("HKEY_LOCAL_MACHINE\\"))
				{
					m_appsRegKeyPath = Registry.LocalMachine;
					appsRegKeyPath = appsRegKeyPath.Replace("HKEY_LOCAL_MACHINE\\", string.Empty);
				}
				else
					m_appsRegKeyPath = Registry.CurrentUser;

				if (appsRegKeyPath != string.Empty &&
					parentForm != null && parentForm.Name != string.Empty)
				{
					appsRegKeyPath += ("\\" + parentForm.GetType().Name + "ToolBars");
				}

				m_appsRegKeyPath = (appsRegKeyPath == string.Empty ? null :
					m_appsRegKeyPath.CreateSubKey(appsRegKeyPath));
			}

			Initialize(parentForm, contentControl, msgMediator, definitions);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="parentForm"></param>
		/// <param name="contentControl"></param>
		/// <param name="msgMediator"></param>
		/// <param name="definitions"></param>
		/// ------------------------------------------------------------------------------------
		public void Initialize(Form parentForm, Control contentControl,
			Mediator msgMediator, string[] definitions)
		{
			if (m_parentForm != null && m_parentForm == parentForm)
				return;

			m_parentForm = parentForm;
			MessageMediator = msgMediator;

			// Setup a ToolStripContainer
			InitToolStripContainer(parentForm, contentControl);

			// Read images, localized strings and command Ids.
			ReadResourcesAndCommands(definitions);

			#region Do this stuff for Menus
			ReadMenuDefinitions(definitions);

			if (m_windowListItem != null && m_windowListItem.OwnerItem != null &&
				m_windowListItem.OwnerItem is ToolStripMenuItem)
			{
				m_windowListItem.Click += HandleItemClicks;
				((ToolStripMenuItem)m_windowListItem.OwnerItem).DropDown.Opening +=
					HandleBuildingWindowList;
			}

			#endregion

			#region Do this stuff for Toolbars
			ReadToolbarDefinitions(definitions);
			AllowAppsToIntializeComboItems();

			// If we have a menu item to display the toolbars, then add menu items for each one.
			if (m_toolbarListItem != null)
				BuildToolbarList();

			if (m_tsContainer != null)
				m_tsContainer.BringToFront();
			else
				m_tsPanel.BringToFront();

			m_images = null;
			m_rmlocalStrings = null;
			m_parentForm.Shown += m_parentForm_Shown;

			#endregion
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Locate the toolbars when their parent form is shown.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void m_parentForm_Shown(object sender, EventArgs e)
		{
			m_parentForm.Shown -= m_parentForm_Shown;
			if (!LoadBarSettings())
				LoadDefaultToolbarLayout();

			Application.Idle += HandleToolBarItemUpdates;
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
		/// Creates a ToolStripContainer control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitToolStripContainer(Form parentForm, Control contentControl)
		{
			m_tsContainer = contentControl as ToolStripContainer;
			if (m_tsContainer != null)
				return;

			if (contentControl == null)
			{
				m_tsPanel = new ToolStripPanel();
				m_tsPanel.Dock = DockStyle.Top;
				parentForm.Controls.Add(m_tsPanel);
			}
			else
			{
				m_tsContainer = new ToolStripContainer();
				m_tsContainer.Dock = DockStyle.Fill;
				parentForm.Controls.Remove(contentControl);
				m_tsContainer.ContentPanel.Controls.Add(contentControl);
				parentForm.Controls.Add(m_tsContainer);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method gets the combobox items and fires the InitializeComboItem event in
		/// case the application wants to initialize the combo box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void AllowAppsToIntializeComboItems()
		{
			if (InitializeComboItem == null)
				return;

			foreach (ToolStripItem item in m_tmItems.Values)
			{
				ToolStripComboBox cboItem = item as ToolStripComboBox;
				if (cboItem != null)
				{
					cboItem.Items.Clear();
					InitializeComboItem(cboItem.Name, cboItem.ComboBox);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize list of available toolbars.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="ToolStripMenuItem is added to DropDownItems collection and disposed there")]
		private void BuildToolbarList()
		{
			m_toolBarMenuMap.Clear();
			TMBarProperties[] barProps = BarInfoForViewMenu;

			foreach (TMBarProperties bp in barProps)
			{
				ToolStripMenuItem item = ToolStripItemExtender.CreateMenuItem();
				item.Name = bp.Name + kToolbarItemSuffix;
				item.Text = bp.Text;
				item.Tag = m_toolbarListItem.Tag;
				m_toolbarListItem.DropDownItems.Add(item);
				item.Click += HandleItemClicks;

				ToolStrip bar;
				if (m_bars.TryGetValue(bp.Name, out bar))
					m_toolBarMenuMap[item] = bar;
			}
		}

		#endregion

		#region Misc. methods for loading and saving tool bar settings
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load tool bar settings from the registry, if available.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool LoadBarSettings()
		{
			if (m_appsRegKeyPath == null)
				return false;

			List<InitialBarProps> ibpList = new List<InitialBarProps>();

			foreach (ToolStrip bar in m_bars.Values)
			{
				int dx = (int)m_appsRegKeyPath.GetValue(bar.Name + "X", -1);
				if (dx == -1)
					return false;

				InitialBarProps ibp = new InitialBarProps();
				ibp.Name = bar.Name;
				ibp.Side = m_appsRegKeyPath.GetValue(bar.Name + "Side", "Top") as string;
				ibp.Location = new Point(dx,
					(int)m_appsRegKeyPath.GetValue(bar.Name + "Y", bar.Top));

				string strVal = m_appsRegKeyPath.GetValue(bar.Name + "Visible") as string;
				bool visible;
				ibp.Visible = (bool.TryParse(strVal, out visible) ? visible : false);
				m_displayedBars[bar.Name] = ibp.Visible;

				ibpList.Add(ibp);
			}

			// Sort the bar information read from the registry by visibility and location.
			ibpList.Sort(InitialBarPropsComparer);

			foreach (InitialBarProps sbp in ibpList)
			{
				ToolStrip bar = m_bars[sbp.Name];
				bar.Visible = sbp.Visible;
				bar.Location = sbp.Location;

				if (m_tsContainer == null)
					m_tsPanel.Controls.Add(bar);
				else
				{
					switch (sbp.Side)
					{
						case "Bottom": m_tsContainer.BottomToolStripPanel.Controls.Add(bar); break;
						case "Right": m_tsContainer.RightToolStripPanel.Controls.Add(bar); break;
						case "Left": m_tsContainer.LeftToolStripPanel.Controls.Add(bar); break;
						default: m_tsContainer.TopToolStripPanel.Controls.Add(bar); break;
					}
				}
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Causes the adapter to save toolbar settings (e.g. user placement of toolbars).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SaveBarSettings()
		{
			if (m_appsRegKeyPath == null)
				return;

			foreach (ToolStrip bar in m_bars.Values)
			{
				bool isBarDisplayed;
				if (!m_displayedBars.TryGetValue(bar.Name, out isBarDisplayed))
					isBarDisplayed = false;

				m_appsRegKeyPath.SetValue(bar.Name + "Visible", isBarDisplayed);
				m_appsRegKeyPath.SetValue(bar.Name + "X", bar.Left);
				m_appsRegKeyPath.SetValue(bar.Name + "Y", bar.Top);

				if (m_tsContainer != null)
				{
					if (bar.Parent == m_tsContainer.TopToolStripPanel)
						m_appsRegKeyPath.SetValue(bar.Name + "Side", "Top");
					else if (bar.Parent == m_tsContainer.LeftToolStripPanel)
						m_appsRegKeyPath.SetValue(bar.Name + "Side", "Left");
					else if (bar.Parent == m_tsContainer.RightToolStripPanel)
						m_appsRegKeyPath.SetValue(bar.Name + "Side", "Right");
					else if (bar.Parent == m_tsContainer.BottomToolStripPanel)
						m_appsRegKeyPath.SetValue(bar.Name + "Side", "Bottom");
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Positions the toolbars to the positions specified in the xml deffinition file.
		/// This method only gets called when tool bar locations and visibilities cannot be
		/// found in the registry.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void LoadDefaultToolbarLayout()
		{
			List<InitialBarProps> ibpList = new List<InitialBarProps>();

			foreach (ToolStrip bar in m_bars.Values)
			{
				InitialBarProps ibp = new InitialBarProps();
				ibp.Name = bar.Name;
				ibp.Location = bar.Location = m_barLocations[bar];
				ibp.Side = "Top";

				bool isBarDisplayed;
				ibp.Visible = (m_displayedBars.TryGetValue(bar.Name, out isBarDisplayed) ?
					isBarDisplayed : false);

				ibpList.Add(ibp);
			}

			// Sort bars by visibility and location.
			ibpList.Sort(InitialBarPropsComparer);

			// Now add the bars to the top panel, arranging them accordingly.
			int prevRow = 0;
			int dx = 0;
			foreach (InitialBarProps ibp in ibpList)
			{
				ToolStrip bar = m_bars[ibp.Name];
				bar.Visible = ibp.Visible;
				int barRow = bar.Top;

				if (barRow != prevRow)
				{
					prevRow = barRow;
					dx = 0;
				}

				bar.Top = (barRow * bar.Height);
				bar.Left = dx;
				if (m_tsContainer != null)
					m_tsContainer.TopToolStripPanel.Controls.Add(bar);
				else
					m_tsPanel.Controls.Add(bar);

				dx += (bar.Left + bar.Width);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares the locations and visibility of two toolstrip objects.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private int InitialBarPropsComparer(InitialBarProps x, InitialBarProps y)
		{
			if (x.Visible && !y.Visible)
				return -1;

			if (x.Side != y.Side)
				return y.Side.CompareTo(x.Side);

			if (!x.Visible && y.Visible)
				return 1;

			bool barsAreHoriz = (x.Side == "Top" || x.Side == "Bottom");

			if (x.Location.Y != y.Location.Y)
				return (x.Location.Y - y.Location.Y) * (barsAreHoriz ? 1 : -1);

			return (x.Location.X - y.Location.X) * (barsAreHoriz ? 1 : -1);
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
				{
					value.Disposed += HandleMessageMediatorDisposed;
					m_allowuUpdates = true;
				}

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
				List<TMBarProperties> barProps = new List<TMBarProperties>();
				foreach (ToolStrip bar in m_bars.Values)
					barProps.Add(GetBarProperties(bar.Name));

				barProps.Sort(CompareBarProps);
				return (barProps.Count == 0 ? null : barProps.ToArray());
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private int CompareBarProps(TMBarProperties x, TMBarProperties y)
		{
			return ((new CaseInsensitiveComparer()).Compare(x.Text, y.Text));
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
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="images is a referenec")]
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
					m_images[trimmedLabel] = images.Images[i++];
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
				throw new Exception("ToolStrip Adapter could not create the class: " +
					className + ".");
			}

			//Get the named ImageList
			FieldInfo fldInfo = classIntance.GetType().GetField(field,
				BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

			if (fldInfo == null)
			{
				throw new Exception("ToolStrip Adapter could not find the field '" + field +
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
			string baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			string assemblyPath = Path.Combine(baseDir, assemblyName);
			Assembly assembly = null;

			try
			{
				assembly = Assembly.LoadFrom(assemblyPath);
				if (assembly == null)
					throw new ApplicationException(); //will be caught and described in the catch
			}
			catch (Exception error)
			{
				throw new Exception("ToolStrip Adapter could not load the DLL at: " +
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

				cmdInfo.ShortcutKey = ParseShortcutKeyString(shortcut);

				// If the command doesn't have an explicit image label, then use
				// the command id as the image label.
				if (imageLabel == null)
					imageLabel = cmd;

				Image img;
				if (m_images.TryGetValue(imageLabel, out img))
					cmdInfo.Image = img;

				m_commandInfo[cmd] = cmdInfo;

				commandNode = ReadOverJunk(commandNode.NextSibling);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Parses the specified shortcut key string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static Keys ParseShortcutKeyString(string shortcut)
		{
			Keys keys = Keys.None;

			if (shortcut != null)
			{
				if (shortcut.Contains("Ctrl"))
					keys |= Keys.Control;
				if (shortcut.Contains("Alt"))
					keys |= Keys.Alt;
				if (shortcut.Contains("Shift"))
					keys |= Keys.Shift;

				shortcut = shortcut.Replace("Ctrl", string.Empty);
				shortcut = shortcut.Replace("Alt", string.Empty);
				shortcut = shortcut.Replace("Shift", string.Empty);
				shortcut = shortcut.Replace("Del", "Delete").Trim();

				if (shortcut != string.Empty)
				{
					try
					{
						keys |= (Keys)Enum.Parse(typeof(Keys), shortcut, true);
					}
					catch { }
				}
			}

			return keys;
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
					m_menuBar = new MenuStrip();
					m_menuBar.Name = kMainMenuName;
					m_menuBar.Dock = DockStyle.Top;
					m_menuBar.ShowItemToolTips = false;
					m_menuBar.Stretch = true;
					m_menuBar.Visible = true;
					m_menuBar.AccessibleRole = AccessibleRole.MenuBar;
					m_menuBar.AccessibleName = m_menuBar.Name;
					m_menuBar.ShowItemToolTips = false;
					m_parentForm.Controls.Add(m_menuBar);
					m_menuBar.BringToFront();
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
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="cmnu is added to m_contextMenus and disposed there")]
		private void ReadContextMenus(XmlNode node)
		{
			m_readingContextMenuDef = true;

			while (node != null)
			{
				string name = GetAttributeValue(node, "name");
				if (name != null)
				{
					ContextMenuStrip cmnu = ToolStripItemExtender.CreateContextMenu();
					cmnu.Name = name;
					cmnu.ShowItemToolTips = false;
					cmnu.Opening += HandleMenuOpening;
					cmnu.Opened += HandleMenuOpened;
					m_contextMenus[name] = cmnu;
					ReadMenuItems(node.FirstChild, cmnu, true);

					if (cmnu.Items.Count > 0)
					{
						// Make sure that if a command has different text for
						// context menus then we account for it.
						foreach (ToolStripItem item in cmnu.Items)
						{
							CommandInfo cmndInfo = GetCommandInfo(item);
							if (cmndInfo != null && cmndInfo.ContextMenuText != null)
								item.Text = cmndInfo.ContextMenuText;
						}
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
				ToolStripMenuItem item = (ToolStripMenuItem)ReadSingleItem(node, true);

				// If we're reading context menus and the item was assigned a shortcut,
				// then get rid of it since context menus shouldn't have shortcuts.
				if (readingContextMenus)
					item.ShortcutKeys = Keys.None;

				// If the item has a shortcut then we need to add it to the list of
				// menu items to get updated during the application's idle cycles.
				// See comment for this collection in the declarations.
				if (item.ShortcutKeys != Keys.None && !m_menusWithShortcuts.Contains(item))
					m_menusWithShortcuts.Add(item);

				string insertBefore = GetAttributeValue(node, "insertbefore");
				string addTo = GetAttributeValue(node, "addto");

				if (insertBefore != null)
				{
					if (GetBoolFromAttribute(node, "cancelbegingrouponfollowingitem"))
					{
						ToolStripItem refItem;
						if (m_tmItems.TryGetValue(insertBefore, out refItem))
							ToolStripItemExtender.SetBeginGroup(refItem, false);
					}

					InsertMenuItem(item, insertBefore);
				}
				else if (addTo != null)
					AddMenuItem(item, addTo);
				else
				{
					if (parentItem is MenuStrip)
						((MenuStrip)parentItem).Items.Add(item);
					else if (parentItem is ToolStripMenuItem)
						((ToolStripMenuItem)parentItem).DropDownItems.Add(item);
					else if (parentItem is ContextMenuStrip)
						((ContextMenuStrip)parentItem).Items.Add(item);
				}

				// This must be done after the item has been added to a item collection.
				item.Visible = GetBoolFromAttribute(node, "visible", true);

				// Now read any subitems of the one just created.
				if (node.ChildNodes.Count > 0)
					ReadMenuItems(node.FirstChild, item, readingContextMenus);
				else
					item.Click += HandleItemClicks;

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
		/// ------------------------------------------------------------------------------------
		internal void InsertMenuItem(ToolStripItem item, string refItemName)
		{
			if (m_menuBar == null || item == null)
				return;

			if (string.IsNullOrEmpty(refItemName))
			{
				m_menuBar.Items.Add(item);
				return;
			}

			ToolStripItem refItem;
			if (!m_tmItems.TryGetValue(refItemName, out refItem))
			{
				return;
#if !__MonoCS__ // TODO-Linux FWNX-314: this can currently happen because of incomplete UIAdapters-SilSidePane.dll - remove this !Mono block when FWNX-236 is completed
				//throw new ArgumentException("Referenced item '" + refItemName +
				//	"' not found trying to insert item '" + item.Name + "'.", "refItemName");
#endif
			}

			ToolStripItemExtender.InsertItemBefore(refItem, item);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the item specified by refItemName and Adds the specified item to the ref.
		/// item's collection of sub items.
		/// </summary>
		/// <param name="item"></param>
		/// <param name="refItemName"></param>
		/// ------------------------------------------------------------------------------------
		private void AddMenuItem(ToolStripMenuItem item, string refItemName)
		{
			if (m_menuBar == null || item == null)
				return;

			if (string.IsNullOrEmpty(refItemName))
				m_menuBar.Items.Add(item);
			else
			{
				ToolStripItem refItem;
				if (!m_tmItems.TryGetValue(refItemName, out refItem))
				{
					throw new ArgumentException("Referenced item '" + refItemName +
						"' not found trying to insert item '" +	item.Name + "'.", "refItemName");
				}

				if (refItem is ToolStripMenuItem)
					((ToolStripMenuItem)refItem).DropDownItems.Add(item);
			}
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
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="As part of creation bar gets added to collection and disposed there")]
		private void ReadSingleToolbarDef(XmlNode node)
		{
			string barName = GetAttributeValue(node, "name");
			ToolStrip bar = MakeNewBar(node, barName);

			if (node.ChildNodes.Count > 0)
				ReadToolbarItems(node.FirstChild, bar);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="ToolStripItem and CustomDropDown are references")]
		private void ReadToolbarItems(XmlNode node, object parentItem)
		{
			while (node != null)
			{
				ToolStripItem item = ReadSingleItem(node, false);

				if (parentItem is ToolStrip)
					((ToolStrip)parentItem).Items.Add(item);
				else if (parentItem is ToolStripDropDownItem)
				{
					ToolStripDropDownItem pItem = (ToolStripDropDownItem)parentItem;

					// If the parent item's drop-down type is a menu,
					// then make sure the text shows.
					if (pItem.DropDown is ToolStripDropDownMenu)
						item.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;

					if (!(item is ToolStripControlHost))
						pItem.DropDownItems.Add(item);
					else
					{
						// When we're a control host then put ourselves inside a CustomDropDown.
						CustomDropDown dropDown = new CustomDropDown();
						dropDown.AddHost(item as ToolStripControlHost);
						dropDown.AutoCloseWhenMouseLeaves = false;
						pItem.DropDown = dropDown;
					}
				}

				// This must be done after the item has been added to a item collection.
				item.Visible = GetBoolFromAttribute(node, "visible", true);

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
		private ToolStrip MakeNewBar(XmlNode node, string barName)
		{
			// Every bar must have a unique name so if we received a null or empty barName
			// then name it with a new guid.
			if (string.IsNullOrEmpty(barName))
				barName = Guid.NewGuid().ToString();

			ToolStrip bar = ToolStripItemExtender.CreateToolStrip();
			m_bars[barName] = bar;
			bar.Name = barName;
			bar.AccessibleName = barName;
			Point barLoc = new Point(0, 0);
			barLoc.X = GetIntFromAttribute(node, "position", 0);
			barLoc.Y = GetIntFromAttribute(node, "row", m_barLocations.Count);
			m_barLocations[bar] = barLoc;

			string barText = GetAttributeValue(node, "text");
			barText = GetStringFromResource(barText);
			TMBarProperties barProps = new TMBarProperties(barName, barText, true,
				GetBoolFromAttribute(node, "visible", true), m_parentForm);

			if (InitializeBar != null)
				InitializeBar(ref barProps);

			m_displayedBars[barName] = barProps.Visible;
			barProps.Update = true;
			SetBarProperties(bar, barProps);

			return bar;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a toolbar item with the specified name. If the item exists, return it.
		/// Otherwise, create a new one.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected ToolStripItem GetToolbarItem(XmlNode node, string name)
		{
			ToolStripItem item;

			if (m_tmItems.TryGetValue(name, out item))
				return item;

			int type = GetIntFromAttribute(node, "type", 0);

			if (type == 0)
				item = CreateNormalToolBarItem(node);
			else if (type >= 1 && type <= 3)
			{
				item = CreateDropDownToolBarItem(node, type);
				item.DisplayStyle = GetToolBarItemDisplayStyle(node);
			}
			else if (type == 4)
			{
				item = ToolStripItemExtender.CreateComboBox();
				item.Name = name;
				ToolStripComboBox cboItem = item as ToolStripComboBox;
				item.AutoSize = false;
				cboItem.Width = GetIntFromAttribute(node, "width", 25);
			}
			else if (type == 5)
				item = GetCustomControlHost(name, GetAttributeValue(node, "commandid"));
			else
				throw new Exception(type.ToString() + " is an invalid toolbar item type.");

			item.Name = name;
			return item;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a normal (in the sense that it's not a combo box, custom control, drop-down
		/// item, etc.) toolbar item.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private ToolStripItem CreateNormalToolBarItem(XmlNode node)
		{
			ToolStripItem item;

			// At this point, we need to know if we're creating an item that is found
			// on a ToolStrip or one that is found on the drop-down list of tool bar
			// item of type 1 (toolbar item whose drop-down is another toolbar) or 2
			// (toolbar item whose drop-down is a list of menu items). Therefore, it's
			// more important to know the type of the parent item than it is to know
			// the type of the current item. Check if this item's node has a parent. If
			// so, check if the parent node specifies a type. If it does and it's 2,
			// then the item to create must be a ToolStripMenuItem. Otherwise create
			// a ToolStripButton.
			XmlNode parentNode = node.ParentNode;
			if (parentNode != null && GetIntFromAttribute(parentNode, "type", 0) == 2)
				item = ToolStripItemExtender.CreateMenuItem();
			else
				item = ToolStripItemExtender.CreateButton();

			item.Click += HandleItemClicks;
			return item;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates one of three different types of drop-down toolbar items.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private ToolStripItem CreateDropDownToolBarItem(XmlNode node, int type)
		{
			ToolStripItem item;

			// True if the drop-down button is split into two segments, one for the arrow and
			// one for the icon. False if there is no behavioral distinction between the arrow
			// and icon portions of the button.
			bool split = GetBoolFromAttribute(node, "split", true);

			if (split)
			{
				item = ToolStripItemExtender.CreateSplitButton();
				((ToolStripSplitButton)item).ButtonClick += HandleItemClicks;
				((ToolStripSplitButton)item).DropDown.Closed += HandleToolBarItemDropDownClosed;

				if (type == 3)
					((ToolStripSplitButton)item).DropDownOpening += HandleToolBarItemDropDownOpened;
				else
					((ToolStripSplitButton)item).DropDownOpened += HandleToolBarItemDropDownOpened;
			}
			else
			{
				item = ToolStripItemExtender.CreateDropDownButton();
				((ToolStripDropDownButton)item).DropDown.Opened += HandleToolBarItemDropDownOpened;
				((ToolStripDropDownButton)item).DropDown.Closed += HandleToolBarItemDropDownClosed;
			}

			switch (type)
			{
				case 1:
					// Create a drop-down that will act like a drop-down toolbar.
					ToolStripDropDown dropDown = new ToolStripDropDown();
					dropDown.LayoutStyle = ToolStripLayoutStyle.StackWithOverflow;
					((ToolStripDropDownItem)item).DropDown = dropDown;
					break;

				case 2:
					// Create a drop-down that will act like a drop-down menu.
					ToolStripDropDownMenu dropDownMenu = new ToolStripDropDownMenu();
					dropDownMenu.ShowImageMargin = GetBoolFromAttribute(node, "showimagemargin", true);
					dropDownMenu.ShowCheckMargin = GetBoolFromAttribute(node, "showcheckmargin", false);
					((ToolStripDropDownItem)item).DropDown = dropDownMenu;
					break;

				case 3:
					// Create a drop-down for a custom control.
					CustomDropDown cdd = new CustomDropDown();
					cdd.AutoCloseWhenMouseLeaves = false;
					((ToolStripDropDownItem)item).DropDown = cdd;
					break;
			}

			item.DisplayStyle = ToolStripItemDisplayStyle.Image;
			return item;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a display style read from a toolbar item node.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private ToolStripItemDisplayStyle GetToolBarItemDisplayStyle(XmlNode node)
		{
			switch (GetAttributeValue(node, "style"))
			{
				case "textonly": return ToolStripItemDisplayStyle.Text;
				case "both": return ToolStripItemDisplayStyle.ImageAndText;
				default: return ToolStripItemDisplayStyle.Image;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a custom control and return it in a returned ToolStripControlHost.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private ToolStripControlHost GetCustomControlHost(string name, string commandId)
		{
			string tooltip = null;
			CommandInfo ci;
			if (m_commandInfo.TryGetValue(commandId, out ci))
				tooltip = ci.ToolTip;

			Control ctrl = (LoadControlContainerItem != null ?
				LoadControlContainerItem(name, tooltip) : null);

			if (ctrl == null)
			{
				ctrl = new Label();
				ctrl.Text = "Missing Control: " + name;
			}

			ToolStripControlHost host = ToolStripItemExtender.CreateControlHost(ctrl);
			host.AutoSize = false;
			host.Size = ctrl.Size;
			ctrl.Dock = DockStyle.Fill;
			host.Dock = DockStyle.Fill;
			host.Padding = Padding.Empty;
			host.Margin = Padding.Empty;
			return host;
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
		protected ToolStripItem ReadSingleItem(XmlNode node, bool isMenuItem)
		{
			string name = GetAttributeValue(node, "name");
			int displayType = GetIntFromAttribute(node, "displaytype", (isMenuItem ? 2 : 0));

			// Give the item a guid for the name if one wasn't found in the XML def.
			if (name == null || name == string.Empty)
				name = Guid.NewGuid().ToString();

			// If the item is for a menu just make a ToolStripMenuItem since that's all we allow
			// on menus. Otherwise it must be a toolbar item, so go make the appropriate
			// type of toolbar item.
			ToolStripItem item = (isMenuItem ?
				ToolStripItemExtender.CreateMenuItem() : GetToolbarItem(node, name));
			m_tmItems[name] = item;

			switch (displayType)
			{
				case 0: item.DisplayStyle = ToolStripItemDisplayStyle.Image; break;
				case 1: item.DisplayStyle = ToolStripItemDisplayStyle.Text; break;
				case 2: item.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText; break;
			}

			bool rightAligned = GetBoolFromAttribute(node, "rightaligned", false);
			item.Alignment = (rightAligned ? ToolStripItemAlignment.Right : ToolStripItemAlignment.Left);
			int leftMargin = GetIntFromAttribute(node, "leftmargin", item.Margin.Left);
			int topMargin = GetIntFromAttribute(node, "topmargin", item.Margin.Top);
			int rightMargin = GetIntFromAttribute(node, "rightmargin", item.Margin.Right);
			int bottomMargin = GetIntFromAttribute(node, "bottommargin", item.Margin.Bottom);
			item.Margin = new Padding(leftMargin, topMargin, rightMargin, bottomMargin);
			item.AutoSize = GetBoolFromAttribute(node, "autosize", item.AutoSize);
			item.Name = name;

			InitItem(node, item, name, isMenuItem);

			if (isMenuItem)
			{
				((ToolStripMenuItem)item).DropDown.Opening += HandleMenuOpening;
				((ToolStripMenuItem)item).DropDown.Opened += HandleMenuOpened;
			}

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
		private void InitItem(XmlNode node, ToolStripItem item, string name, bool isMenuItem)
		{
			string commandid = GetAttributeValue(node, "commandid");

			item.Tag = commandid;
			item.Name = name;
			item.AccessibleName = name;

			TMItemProperties itemProps = new TMItemProperties();
			itemProps.ParentForm = m_parentForm;
			itemProps.Name = name;
			itemProps.CommandId = commandid;
			itemProps.Enabled = true;
			itemProps.Visible = GetBoolFromAttribute(node, "visible", true);
			itemProps.BeginGroup = GetBoolFromAttribute(node, "begingroup", false);

			CommandInfo cmdInfo;
			if (m_commandInfo.TryGetValue(commandid, out cmdInfo))
			{
				itemProps.Text = cmdInfo.Text;
				itemProps.Category = cmdInfo.Category;
				itemProps.Tooltip = cmdInfo.ToolTip;
				itemProps.Image = cmdInfo.Image;

				if (cmdInfo.ShortcutKey != Keys.None && isMenuItem && item is ToolStripMenuItem)
					((ToolStripMenuItem)item).ShortcutKeys = cmdInfo.ShortcutKey;
			}

			if (GetBoolFromAttribute(node, "toolbarlist"))
				m_toolbarListItem = (item as ToolStripMenuItem);

			if (GetBoolFromAttribute(node, "windowlist"))
				m_windowListItem = (item as ToolStripMenuItem);

			if (GetBoolFromAttribute(node, "morewindowsitem"))
				m_moreWindowItem = (item as ToolStripMenuItem);

			ToolStripComboBox cboItem = item as ToolStripComboBox;
			if (cboItem != null)
			{
				cboItem.AccessibleRole = AccessibleRole.ComboBox;
				itemProps.Control = cboItem.ComboBox;
			}

			// Let the application have a stab at initializing the item.
			if (InitializeItem != null)
				InitializeItem(ref itemProps);

			// Save all initializatons by updating the item.
			itemProps.Update = true;
			SetItemProps(item, itemProps);
		}

		#endregion

		#region Item clicks and updates
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
				m_menuCurrentlyPoppedUp.Close();
				m_menuCurrentlyPoppedUp = null;
			}

			ToolStripItem item = sender as ToolStripItem;
			string message = GetItemsCommandMessage(item);

			if (item == null || message == string.Empty || m_msgMediator == null)
				return;

			TMItemProperties itemProps = GetItemProps(item);

			// If the user clicked on one of the windows in the window list, then save the
			// index of that window in the item properties tag.
			if (m_windowListItem != null && (string)item.Tag == (string)m_windowListItem.Tag)
				GetWindowIndex(item, itemProps);

			// If the user clicked on one of the toolbar items in the toolbar menu list
			// then save the toolbar's name in the tag property.
			if (item.Name.EndsWith(kToolbarItemSuffix))
				itemProps.Tag = item.Name.Replace(kToolbarItemSuffix, string.Empty);

			if (m_msgMediator.SendMessage(message, itemProps) && itemProps.Update)
				SetItemProperties(item.Name, itemProps);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// While the system is idle (or about to become so), this method will go through all
		/// the toolbar items and make sure they are enabled properly for the current view.
		/// Then it will go through the menu items with shortcuts to make sure they're enabled
		/// properly. (This is only done every 0.75 seconds.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void HandleToolBarItemUpdates(object sender, EventArgs e)
		{
			if (m_msgMediator == null || !AllowUpdates || DateTime.Now < m_itemUpdateTime.AddSeconds(0.75))
				return;

			m_itemUpdateTime = DateTime.Now;

			// Loop through all the toolbars and items on toolbars.
			foreach (ToolStrip bar in m_bars.Values)
			{
				bool isBarDisplayed;
				if (m_displayedBars.TryGetValue(bar.Name, out isBarDisplayed) && isBarDisplayed)
					CallUpdateHandlersForCollection(bar.Items);
			}

			// Menu items with shortcut keys must be updated as
			// often as toolbar buttons, so do that now.
			CallUpdateHandlersForCollection(m_menusWithShortcuts);
		}

		#endregion

		#region Handlers for tool bar drop-down items opening and closing
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles clicks on the drop-down portion of a popup button (i.e. a click on the
		/// arrow portion of a two-segmented toolbar button).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void HandleToolBarItemDropDownOpened(object sender, EventArgs e)
		{
			ToolStripDropDownItem item = sender as ToolStripDropDownItem;
			if (m_msgMediator == null || item == null)
				return;

			string message = GetItemsCommandMessage(item);

			if (!string.IsNullOrEmpty(message))
			{
				ToolBarPopupInfo popupInfo = new ToolBarPopupInfo(item.Name);
				if (m_msgMediator.SendMessage("DropDown" + message, popupInfo))
				{
					if (popupInfo.Control != null && item.DropDown is CustomDropDown)
						((CustomDropDown)item.DropDown).AddControl(popupInfo.Control);
				}
			}

			CallUpdateHandlersForCollection(item.DropDownItems);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Send a message to anyone who cares that the drop down is closing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void HandleToolBarItemDropDownClosed(object sender, EventArgs e)
		{
			ToolStripItem item = sender as ToolStripItem;
			string message = GetItemsCommandMessage(item);

			if (item != null && message != string.Empty && m_msgMediator != null)
			{
				TMItemProperties itemProps = GetItemProps(item);
				if (m_msgMediator.SendMessage("DropDownClosed" + message, itemProps) && itemProps.Update)
					SetItemProperties(item.Name, itemProps);
			}
		}

		#endregion

		#region Handler for building and removing the windows list
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the building window list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="ToolStripMenuItem gets added to collection and disposed there")]
		void HandleBuildingWindowList(object sender, CancelEventArgs e)
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

			ToolStripMenuItem wndListParent = m_windowListItem.OwnerItem as ToolStripMenuItem;
			if (wndListParent == null)
				return;

			ArrayList wndList = wndListInfo.WindowListItemProperties.List;
			if (wndList == null || wndList.Count == 0 || !wndListInfo.WindowListItemProperties.Update)
				return;

			// The window list only allows up to kMaxWindowListItems items (i.e. 0 - 9)
			// so make the "More Windows..." option visible when there are more.
			m_moreWindowItem.Visible = (wndList.Count > kMaxWindowListItems);

			// Before adding the window list items, first make sure
			// any old items are removed from the windows menu.
			ClearWindowListFromWindowMenu(m_windowListItem.OwnerItem as ToolStripMenuItem);

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
			int wndListIndex = wndListParent.DropDownItems.IndexOf(m_windowListItem);

			// Add the rest of the window list items, up to kMaxWindowListItems - 1 more.
			for (int i = 1; i < wndList.Count && i < kMaxWindowListItems; i++)
			{
				text = wndList[i] as string;
				if (text != null)
				{
					ToolStripMenuItem newItem = ToolStripItemExtender.CreateMenuItem();
					newItem.Text = "&" + (i + 1).ToString() + " " + text;
					newItem.Name = m_windowListItem.Name + i;
					m_htWndListIndices[newItem] = i;
					newItem.Tag = m_windowListItem.Tag;
					newItem.Checked = (wndListInfo.CheckedItemIndex == i);
					newItem.Click += HandleItemClicks;
					wndListParent.DropDownItems.Add(newItem);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void GetWindowIndex(ToolStripItem item, TMItemProperties itemProps)
		{
			itemProps.Tag = (m_htWndListIndices[item] != null ? (int)m_htWndListIndices[item] : -1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clear out the window list after it's parent closes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ClearWindowListFromWindowMenu(ToolStripMenuItem windowMenu)
		{
			if (windowMenu == null)
				return;

			int wndListIndex = windowMenu.DropDownItems.IndexOf(m_windowListItem);

			// Remove all the window list items (except the first one).
			for (int i = windowMenu.DropDownItems.Count - 1; i > wndListIndex; i--)
			{
				// Don't delete the "More Windows..." options.
				if (windowMenu.DropDownItems[i] != m_moreWindowItem)
				{
					windowMenu.DropDownItems[i].Click -= HandleItemClicks;
					windowMenu.DropDownItems.RemoveAt(i);
				}
			}
		}

		#endregion

		#region Handlers for meun/context menu opening and closing
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether or not a menu should be allowed to be popped-up (See TE-6553).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void HandleMenuOpening(object sender, CancelEventArgs e)
		{
			// Disallow popups if there is a modal form showing that is
			// a form other than the current parent form.
			if (!m_parentForm.Modal)
			{
				foreach (Form frm in Application.OpenForms)
				{
					if (frm.Modal)
					{
						e.Cancel = true;
						return;
					}
				}
			}

			// ...or if the current form is disabled.
			e.Cancel = (!m_parentForm.Enabled);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When a menu item is popped-up, then cycle through the subitems and call update
		/// handlers.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void HandleMenuOpened(object sender, EventArgs e)
		{
			ToolStripDropDown dropDown = sender as ToolStripDropDown;
			if (dropDown != null)
			{
				CallUpdateHandlersForCollection(dropDown.Items);
				HideInitialSeparators(dropDown);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether or not a menu should be allowed to be popped-up (See TE-6553).
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private bool CancelMenuOpen()
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
		/// Goes through the collection of ToolStripItems in the specified drop-down and hides
		/// the first visible item if it's a ToolStripSeparator. If the second (or beyond) item
		/// is also visible and a ToolStripSeparator, then it/they won't be hidden. We have
		/// our limits. If that becomes a problem, then ENHANCE: to cover that problem.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void HideInitialSeparators(ToolStripDropDown dropDown)
		{
			if (dropDown == null)
				return;

			// Find the first visible item in the drop-down. If it's a
			// ToolStripSeparator then make it invisible.
			foreach (ToolStripItem item in dropDown.Items)
			{
				if (item.Visible)
				{
					if (item is ToolStripSeparator)
					{
						m_hiddenSeparators[dropDown] = item as ToolStripSeparator;
						item.Visible = false;
					}

					break;
				}
			}

			dropDown.Closed += HandleMenuClosed;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the restoring visibility of ToolStripSeparators hidden in
		/// HideInitialSeparators.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void HandleMenuClosed(object sender, ToolStripDropDownClosedEventArgs e)
		{
			ToolStripDropDown dropDown = sender as ToolStripDropDown;
			if (dropDown != null)
			{
				ToolStripSeparator separator;
				if (m_hiddenSeparators.TryGetValue(dropDown, out separator))
					separator.Visible = true;

				dropDown.Closed -= HandleMenuClosed;
			}
		}

		#endregion

		#region Method for calling update command handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calls the update handlers for collection.
		/// </summary>
		/// <param name="collection">The collection.</param>
		/// ------------------------------------------------------------------------------------
		protected void CallUpdateHandlersForCollection(ToolStripItemCollection collection)
		{
			if (m_msgMediator == null)
				return;

			int max = collection.Count;

			for (int i = 0; i < max; i++)
			{
				CallItemUpdateHandler(collection[i]);

				// If the collection changed while enumerating it, then start over. This
				// could happen if one of the update handlers added or removed and item
				// or changed an item's BeginGroup property.
				if (collection.Count != max)
				{
					CallUpdateHandlersForCollection(collection);
					break;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is a helper method for the HandleMenuOpening and HandleItemUpdates methods.
		/// It calls the update handler for the specified ToolStripItem item.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void CallItemUpdateHandler(ToolStripItem item)
		{
			if (item is ToolStripSeparator)
				return;

			string message = GetItemsCommandMessage(item);
			if (message == string.Empty || m_msgMediator == null)
				return;

			bool isToolBarMenuItem = false;
			TMItemProperties itemProps = GetItemProps(item);

			// If the item being updated is one of the toolbar items in the toolbar menu
			// list then save the toolbar's name in the tag property.
			if (item.Name.EndsWith(kToolbarItemSuffix))
			{
				isToolBarMenuItem = true;
				itemProps.Tag = item.Name.Replace(kToolbarItemSuffix, string.Empty);
			}

			// Call update method (e.g. OnUpdateEditCopy). If that method doesn't exist or
			// if all update methods return false, we check for the existence of the
			// command handler.
			if (m_msgMediator.SendMessage("Update" + message, itemProps))
			{
				if (isToolBarMenuItem)
				{
					ToolStrip bar;
					if (m_toolBarMenuMap.TryGetValue((ToolStripMenuItem)item, out bar))
					{
						bool isBarDisplayed;
						itemProps.Checked =
							(m_displayedBars.TryGetValue(bar.Name, out isBarDisplayed) ?
							isBarDisplayed : false);
					}
				}

				SetItemProps(item, itemProps);
			}
			else
			{
				// If the item is a menu item with sub items, then don't disable it.
				// Menu items with sub items often don't have receivers so we shouldn't
				// assume it should be disabled just because it doesn't have a receiver.
				if (item is ToolStripMenuItem && ((ToolStripMenuItem)item).DropDownItems.Count > 0)
					return;

				// The item is a menu item, so automatically disable
				// it if it does not have a receiver.
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
			ToolStripItem item;
			return (m_tmItems.TryGetValue(name, out item) ? GetItemProps(item) : null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads a TMItemProperties object with several properties from a toolbar item.
		/// </summary>
		/// <param name="item">The item whose properties are being stored.</param>
		/// <returns>The properties of a menu or toolbar item.</returns>
		/// ------------------------------------------------------------------------------------
		private TMItemProperties GetItemProps(ToolStripItem item)
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
			itemProps.Enabled = item.Enabled;
			itemProps.Visible = item.Visible;
			itemProps.IsDisplayed = item.Visible;
			itemProps.Image = item.Image;
			itemProps.Font = item.Font;
			itemProps.Tooltip = item.ToolTipText;
			itemProps.Tag = m_htItemTags[item];
			itemProps.BeginGroup = ToolStripItemExtender.GetBeginGroup(item);

			CommandInfo cmdInfo = GetCommandInfo(item);
			if (cmdInfo != null)
			{
				if (cmdInfo.Message != null)
					itemProps.Message = cmdInfo.Message;
				if (cmdInfo.Text != null)
					itemProps.OriginalText = cmdInfo.Text;
			}

			if (item is ToolStripButton)
				itemProps.Checked = ((ToolStripButton)item).Checked;
			else if (item is ToolStripMenuItem)
				itemProps.Checked = ((ToolStripMenuItem)item).Checked;
			else if (item is CheckableSplitButton)
				itemProps.Checked = ((CheckableSplitButton)item).Checked;
			else if (item is ToolStripComboBox)
			{
				ToolStripComboBox cboItem = item as ToolStripComboBox;
				itemProps.Control = cboItem.ComboBox;
				if (cboItem.Items.Count > 0)
				{
					// Get all the combo items and save in the List property.
					itemProps.List = new ArrayList();
					for (int i = 0; i < cboItem.Items.Count; i++)
						itemProps.List.Add(cboItem.Items[i]);
				}
			}
			else if (item is ToolStripControlHost)
				itemProps.Control = ((ToolStripControlHost)item).Control;

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
			ToolStripItem item;
			if (m_tmItems.TryGetValue(name, out item))
				SetItemProps(item, itemProps);
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
		private void SetItemProps(ToolStripItem item, TMItemProperties itemProps)
		{
			if (item == null || !itemProps.Update)
				return;

			if (item.Tag as string != itemProps.CommandId)
			{
				item.Tag = itemProps.CommandId;

				// Since we just changed the command ID, we should change the item's
				// image if it's a button item and the image isn't already being
				// specified in the item properties.
				if (itemProps.Image == null && item is ToolStripItem)
				{
					CommandInfo cmdInfo = GetCommandInfo(item);
					if (cmdInfo != null && cmdInfo.Image != null)
						item.Image = cmdInfo.Image;
				}
			}

			m_htItemTags[item] = itemProps.Tag;

			// Update all the changed fields only if necessary.
			if (item.Text != itemProps.Text)
				item.Text = itemProps.Text;

			if (itemProps.Font != null)
				item.Font = itemProps.Font;

			if (item.ToolTipText != itemProps.Tooltip)
				item.ToolTipText = itemProps.Tooltip;

			if (item.Enabled != itemProps.Enabled)
				item.Enabled = itemProps.Enabled;

			if (item.Visible != itemProps.Visible)
				item.Visible = itemProps.Visible;

			if (item.Image != itemProps.Image)
				item.Image = itemProps.Image;

			ToolStripItemExtender.SetBeginGroup(item, itemProps.BeginGroup);

			if (item is ToolStripButton && ((ToolStripButton)item).Checked != itemProps.Checked)
				((ToolStripButton)item).Checked = itemProps.Checked;
			else if (item is ToolStripMenuItem && ((ToolStripMenuItem)item).Checked != itemProps.Checked)
				((ToolStripMenuItem)item).Checked = itemProps.Checked;
			else if (item is CheckableSplitButton && ((CheckableSplitButton)item).Checked != itemProps.Checked)
				((CheckableSplitButton)item).Checked = itemProps.Checked;

			if (item is ToolStripComboBox)
				SetComboItemSpecificProperties(item as ToolStripComboBox, itemProps);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the items in the Items collection for a ComboBoxItem type toolbar item.
		/// </summary>
		/// <param name="item">The combo box item whose Items collection is be updated.</param>
		/// <param name="itemProps">The TMItemProperties containing the new property
		/// values for the toolbar item.</param>
		/// ------------------------------------------------------------------------------------
		private void SetComboItemSpecificProperties(ToolStripComboBox cbo,
			TMItemProperties itemProps)
		{
			// First check if the lists are the same. If they are we don't want to
			// go to the trouble of rebuilding the list, especially since that will
			// cause unnecessary flicker.
			int maxStringLength = 0;
			int factor = 6;
			if (itemProps.List != null && itemProps.List.Count == cbo.Items.Count)
			{
				bool fAreSame = true;
				for (int i = 0; i < cbo.Items.Count || !fAreSame; i++)
				{
					if (cbo.Items[i].ToString().Length > maxStringLength)
					{
						maxStringLength = cbo.Items[i].ToString().Length;
					}
					fAreSame = (cbo.Items[i] == itemProps.List[i]);
				}
				if (maxStringLength > 0 && cbo.DropDownWidth < maxStringLength * factor)
					cbo.DropDownWidth = maxStringLength * factor;


				if (fAreSame)
					return;
			}

			cbo.Items.Clear();

			// If there are item's in the list then upate the combobox item's
			// collection of items.
			if (itemProps.List != null && itemProps.List.Count > 0)
			{
				for (int i = 0; i < itemProps.List.Count; i++)
					cbo.Items.Add(itemProps.List[i]);
			}

			//Set the DropDownWidth of the combo box so that is is wide enough to show
			//the text of all items in the list.
			maxStringLength = 0;
			for (int i = 0; i < cbo.Items.Count; i++)
			{
				if (cbo.Items[i].ToString().Length > maxStringLength)
				{
					maxStringLength = cbo.Items[i].ToString().Length;
				}
			}
			if (maxStringLength > 0 && cbo.DropDownWidth < maxStringLength * factor)
				cbo.DropDownWidth = maxStringLength * factor;

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
		internal CommandInfo GetCommandInfo(ToolStripItem item)
		{
			if (item != null)
			{
				string commandId = item.Tag as string;
				if (commandId != null)
				{
					CommandInfo cmdInfo;
					if (m_commandInfo.TryGetValue(commandId, out cmdInfo))
						return cmdInfo;
				}
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
		protected string GetItemsCommandMessage(ToolStripItem item)
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

			foreach (ResourceManager rm in m_rmlocalStrings)
			{
				localizedStr = rm.GetString(kstid);
				if (localizedStr != null)
					break;
			}

			return (localizedStr == null || localizedStr == string.Empty ?
				kstid : localizedStr.Trim());
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
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="ToolStripMenuItem gets added to collection and disposed there")]
		public void AddMenuItem(TMItemProperties itemProps, string parentItemName,
			string insertBeforeItem)
		{
			// Check if the specified command Id is in our dictionary of commands.
			// If not, we need to add one for this item.
			if (!m_commandInfo.ContainsKey(itemProps.CommandId))
			{
				// If there is no command Id, then assign a unique one.
				if (string.IsNullOrEmpty(itemProps.CommandId))
					itemProps.CommandId = Guid.NewGuid().ToString();

				CommandInfo ci = new CommandInfo();
				ci.Image = itemProps.Image;
				ci.Message = itemProps.Message;
				ci.Text = itemProps.Text;
				ci.ToolTip = itemProps.Tooltip;
				ci.Category = itemProps.Category;
				m_commandInfo[itemProps.CommandId] = ci;
			}

			ToolStripMenuItem item = ToolStripItemExtender.CreateMenuItem();
			item.Name = itemProps.Name;
			item.Tag = itemProps.CommandId;
			item.Click += HandleItemClicks;
			item.DropDown.Opening += HandleMenuOpening;
			item.DropDown.Opened += HandleMenuOpened;
			itemProps.Update = true;
			SetItemProps(item, itemProps);
			m_tmItems[item.Name] = item;

			// If an item to insert before isn't specified, then add the item to the
			// specified parent item. Otherwise, insert before "insertBeforeItem".
			if (insertBeforeItem == null || insertBeforeItem.Trim() == string.Empty)
				AddMenuItem(item, parentItemName);
			else
				InsertMenuItem(item, insertBeforeItem);
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
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification="See TODO-Linux comment")]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="ToolStripMenuItem gets added to collection and disposed there; items is reference")]
		public void AddContextMenuItem(TMItemProperties itemProps, string contextMenuName,
			string parentMenuName, string insertBeforeItem)
		{
			// First, make sure we can find the context menu to which the item will be added.
			ContextMenuStrip cmnu;
			if (!m_contextMenus.TryGetValue(contextMenuName, out cmnu))
				return;

			object parentMenu = cmnu;

			// If the context menu name and the parent menu name are the same, then the new
			// item will be added to the context menu strip. Otherwise, it will be added to
			// a sub menu item that's already on the context menu strip.
			if (contextMenuName != parentMenuName)
			{
				// Get the menu under which the new menu item will be added.
				// TODO-Linux: Find doesn't use the searchAllChildren parameter on Mono.
				ToolStripItem[] items = cmnu.Items.Find(parentMenuName, true);
				Debug.Assert(items.Length == 1);
				Debug.Assert(items[0] is ToolStripDropDownItem);
				parentMenu = items[0];
			}

			// Make the new item and set its properties.
			ToolStripMenuItem item = ToolStripItemExtender.CreateMenuItem();
			item.Name = itemProps.Name;
			item.AccessibleName = itemProps.Name;
			item.Click += HandleItemClicks;
			item.DropDown.Opening += HandleMenuOpening;
			item.DropDown.Opened += HandleMenuOpened;
			itemProps.Update = true;
			SetItemProps(item, itemProps);
			m_tmItems[item.Name] = item;

			// If an item to insert before isn't specified, then add the item to the
			// parent item specified. Otherwise, insert before "insertBeforeItem".
			if (insertBeforeItem == null || insertBeforeItem.Trim() == string.Empty)
			{
				if (parentMenu == cmnu)
					cmnu.Items.Add(item);
				else
					((ToolStripDropDownItem)parentMenu).DropDownItems.Add(item);

				return;
			}

			ToolStripItem beforeItem;
			if (m_tmItems.TryGetValue(insertBeforeItem, out beforeItem))
				ToolStripItemExtender.InsertItemBefore(beforeItem, item);
			else
			{
				Debug.Fail("Failure trying to insert '" + item.Name +
					"' before '" + insertBeforeItem + "'.");
			}
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
			ToolStripItem item;
			if (m_tmItems.TryGetValue(name, out item))
			{
				ToolStripDropDownItem parent = item.OwnerItem as ToolStripDropDownItem;
				if (parent != null)
				{
					if (item is ToolStripDropDownItem)
					{
						((ToolStripDropDownItem)item).DropDown.Opening -= HandleMenuOpening;
						((ToolStripDropDownItem)item).DropDown.Opened -= HandleMenuOpened;
						((ToolStripDropDownItem)item).DropDown.Closed -= HandleToolBarItemDropDownClosed;
					}

					item.Click -= HandleItemClicks;
					parent.DropDownItems.Remove(item);
					m_tmItems.Remove(name);
				}
			}
			else if (m_menuBar.Items.ContainsKey(name))
				m_menuBar.Items.RemoveByKey(name);
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
			ToolStripItem item;
			if (m_tmItems.TryGetValue(parentItemName, out item))
			{
				if (item is ToolStripDropDownItem)
				{
					foreach (ToolStripItem subMenuItem in ((ToolStripDropDownItem)item).DropDownItems)
						m_tmItems.Remove(subMenuItem.Name);

					((ToolStripDropDownItem)item).DropDownItems.Clear();
				}
			}
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
			Debug.Assert(ctrl != null);
			ContextMenuStrip cmnu;
			if (m_contextMenus.TryGetValue(name, out cmnu))
			{
				ctrl.ContextMenuStrip = cmnu;
				return;
			}

			Debug.Fail("Context menu '" + name + "' doesn't exist in the adapter.");
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
			if (m_contextMenus.TryGetValue(name, out m_menuCurrentlyPoppedUp) &&
				m_menuCurrentlyPoppedUp.Items.Count > 0)
			{
				RemoveContextMenuItems(m_menuCurrentlyPoppedUp);
				m_menuCurrentlyPoppedUp.Show(x, y);
			}
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
			PopupMenu(name, x, y);

			if (subItemsToRemoveOnClose != null && subItemsToRemoveOnClose.Count > 0)
			{
				ContextMenuStrip cmnu;
				if (m_contextMenus.TryGetValue(name, out cmnu))
					cmnu.Tag = subItemsToRemoveOnClose;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remove a list of context menu sub items found in the tag property of the specified
		/// context menu.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void RemoveContextMenuItems(ContextMenuStrip cmnu)
		{
			if (cmnu == null || !(cmnu.Tag is List<string>))
				return;

			foreach (string name in cmnu.Tag as List<string>)
			{
				if (cmnu.Items[name] != null)
				{
					cmnu.Items[name].Click -= HandleItemClicks;
					cmnu.Items.RemoveByKey(name);
				}
			}

			cmnu.Tag = null;
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
			m_parentForm = newForm;
			return prevForm;
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
			ToolStrip bar;
			if (m_bars.TryGetValue(name, out bar))
			{
				bool isBarDisplayed;
				if (!m_displayedBars.TryGetValue(bar.Name, out isBarDisplayed))
					isBarDisplayed = false;

				return new TMBarProperties(name, bar.Text, bar.Enabled, isBarDisplayed, m_parentForm);
			}

			return null;
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
			foreach (ToolStrip bar in m_bars.Values)
				SetBarProperties(bar, barProps);
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
			ToolStrip bar;
			if (m_bars.TryGetValue(name, out bar))
				SetBarProperties(bar, barProps);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets properties for a menu bar or toolbar.
		/// </summary>
		/// <param name="bar">Bar to update.</param>
		/// <param name="barProps">New properties of bar.</param>
		/// ------------------------------------------------------------------------------------
		private void SetBarProperties(ToolStrip bar, TMBarProperties barProps)
		{
			if (bar == null || barProps == null || !barProps.Update)
				return;

			if (barProps.Text != bar.Text)
				bar.Text = barProps.Text;

			if (barProps.Enabled != bar.Enabled)
				bar.Enabled = barProps.Enabled;

			bool isBarDisplayed;
			if (!m_displayedBars.TryGetValue(bar.Name, out isBarDisplayed))
				isBarDisplayed = false;

			if (barProps.Visible != isBarDisplayed)
			{
				if (barProps.Visible)
					ShowToolBar(bar.Name);
				else
					HideToolBar(bar.Name);
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
			ToolStripItem item;
			if (m_tmItems.TryGetValue(name, out item) && item is ToolStripControlHost)
				return ((ToolStripControlHost)item).Control;

			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Allows an application to force the hiding of a toolbar item's popup control.
		/// </summary>
		/// <param name="name">Name of item whose popup should be hidden.</param>
		/// ------------------------------------------------------------------------------------
		public void HideBarItemsPopup(string name)
		{
			ToolStripItem item;
			if (m_tmItems.TryGetValue(name, out item) && item is ToolStripDropDownItem)
				((ToolStripDropDownItem)item).DropDown.Close();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Causes the adapter to show it's dialog for customizing toolbars.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ShowCustomizeDialog()
		{
			// Not supported in this adapter.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Allows an application to hide a toolbar.
		/// </summary>
		/// <param name="name">Name of toolbar to hide.</param>
		/// ------------------------------------------------------------------------------------
		public void HideToolBar(string name)
		{
			try
			{
				m_bars[name].Hide();
				m_displayedBars[name] = false;

				if (VisibleToolStrips == 0)
				{
					m_bars[name].Parent.Hide();
				}
			}
			catch
			{
			}
		}

		/// <summary>
		/// Returns the number of VisibleToolStrips.
		/// </summary>
		protected int VisibleToolStrips
		{
			get
			{
				int visibleToolStrips = 0;
				foreach(var item in m_displayedBars)
				{
					if (item.Value)
						visibleToolStrips++;
				}

				return visibleToolStrips;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Allows an application to show a toolbar.
		/// </summary>
		/// <param name="name">Name of toolbar to show.</param>
		/// ------------------------------------------------------------------------------------
		public void ShowToolBar(string name)
		{
			try
			{
				if (VisibleToolStrips == 0)
				{
					m_bars[name].Parent.Show();
				}

				m_bars[name].Show();
				m_displayedBars[name] = true;
			}
			catch
			{
			}
		}

		#endregion

		#region Misc. ITMAdapter methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified Keys is a shortcut for a toolbar or menu item.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="isItemEnabled"><c>true</c> when the specified key is a shortcut key
		/// and its associated toolbar or menu item is enabled.</param>
		/// <returns>
		/// 	<c>true</c> if the specified key is a shortcut for a toolbar or menu item;
		/// 	otherwise, <c>false</c>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public bool IsShortcutKey(Keys key, ref bool isItemEnabled)
		{
			isItemEnabled = false;

			foreach (CommandInfo cmdInfo in m_commandInfo.Values)
			{
				if (cmdInfo.ShortcutKey == key)
				{
					// Call update method (e.g. OnUpdateEditCopy). If that method doesn't exist or
					// if all update methods return false, we check for the existence of the
					// command handler.
					TMItemProperties itemProps = new TMItemProperties();
					itemProps.Message = cmdInfo.Message;
					itemProps.ParentForm = m_parentForm;
					itemProps.Text = cmdInfo.Text;

					m_msgMediator.SendMessage("Update" + cmdInfo.Message, itemProps);
					isItemEnabled = itemProps.Enabled;

					return true;
				}
			}

			return false;
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

	#region ToolStripItemExtender class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[SuppressMessage("Gendarme.Rules.Correctness", "DisposableFieldsShouldBeDisposedRule",
		Justification="m_tsItem is a reference")]
	public class ToolStripItemExtender: IDisposable
	{
		private static Dictionary<ToolStripItem, ToolStripItemExtender> s_extenders =
			new Dictionary<ToolStripItem, ToolStripItemExtender>();

		private bool m_beginGroup = false;
		private ToolStripItem m_tsItem = null;
		private ToolStripSeparator m_separator = null;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ToolStripItemHelper"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private ToolStripItemExtender(ToolStripItem item)
		{
			m_tsItem = item;
			m_tsItem.VisibleChanged += HandleItemsVisiblilityChanged;
		}

		#region Disposable stuff
		#if DEBUG
		/// <summary/>
		~ToolStripItemExtender()
		{
			Dispose(false);
		}
		#endif

		/// <summary/>
		public bool IsDisposed { get; private set; }

		/// <summary/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		protected virtual void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (fDisposing && !IsDisposed)
			{
				// dispose managed and unmanaged objects
				if (m_separator != null)
					m_separator.Dispose();
			}
			m_separator = null;
			IsDisposed = true;
		}
		#endregion

		#region non static methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure a separator's visible state tracks with the ToolStripItem to which it's
		/// associated.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void HandleItemsVisiblilityChanged(object sender, EventArgs e)
		{
			if (m_separator != null)
				m_separator.Visible = m_tsItem.Visible;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not this item is preceded by a separator.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool BeginGroup
		{
			get { return m_beginGroup; }
			set
			{
				if (m_beginGroup == value)
					return;

				m_beginGroup = value;

				if (value)
					AddSeparator(m_tsItem.Owner);
				else
					RemoveSeparator(m_tsItem.Owner);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a separator to the specified ToolStrip before the ToolStripItem associated
		/// with this instance of the extender.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void AddSeparator(ToolStrip ts)
		{
			if (m_beginGroup && m_tsItem != null && ts != null)
			{
				m_separator = new ToolStripSeparator();
				int i = ts.Items.IndexOf(m_tsItem);
				Debug.Assert(i >= 0);
				ts.Items.Insert(i, m_separator);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes from the specified ToolStrip the separator for the ToolStripItem associated
		/// with this instance of the extender.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void RemoveSeparator(ToolStrip ts)
		{
			if (m_separator != null && ts != null)
			{
				ts.Items.Remove(m_separator);
				m_separator = null;
			}
		}

		#endregion

		#region Static methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the begin group property for the specified ToolStripItem.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void SetBeginGroup(ToolStripItem item, bool beginGroup)
		{
			ToolStripItemExtender extender;
			if (s_extenders.TryGetValue(item, out extender))
				extender.BeginGroup = beginGroup;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the begin group property for the specified ToolStripItem.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool GetBeginGroup(ToolStripItem item)
		{
			ToolStripItemExtender extender;
			return (s_extenders.TryGetValue(item, out extender) ? extender.BeginGroup : false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts a new item before the specified item.
		/// </summary>
		/// <param name="beforeItem">The before item.</param>
		/// <param name="insertItem">The insert item.</param>
		/// ------------------------------------------------------------------------------------
		public static void InsertItemBefore(ToolStripItem beforeItem, ToolStripItem insertItem)
		{
			Debug.Assert(beforeItem != null);
			Debug.Assert(insertItem != null);

			if (beforeItem.Owner != null)
			{
				int i = beforeItem.Owner.Items.IndexOf(beforeItem);
				Debug.Assert(i >= 0);

				if (GetBeginGroup(beforeItem))
					i--;

				Debug.Assert(i >= 0);
				beforeItem.Owner.Items.Insert(i, insertItem);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles adding an item's separator (if it has one) to its owning ToolStrip.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static void HandleItemAddedToOwner(object sender, ToolStripItemEventArgs e)
		{
			ToolStripItemExtender extender;
			if (s_extenders.TryGetValue(e.Item, out extender))
				extender.AddSeparator(sender as ToolStrip);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles removing an item's separator (if it has one) from its owning ToolStrip.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static void HandleItemRemovedFromOwner(object sender, ToolStripItemEventArgs e)
		{
			ToolStripItemExtender extender;
			if (s_extenders.TryGetValue(e.Item, out extender))
				extender.RemoveSeparator(sender as ToolStrip);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a tool strip.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification="See TODO-Linux comment")]
		public static ToolStrip CreateToolStrip()
		{
			ToolStrip item = new ToolStrip();
			// TODO-Linux: AllowItemReorder isn't implemented on Mono.
			item.AllowItemReorder = true;
			item.ItemAdded += HandleItemAddedToOwner;
			item.ItemRemoved += HandleItemRemovedFromOwner;
			item.AccessibleRole = AccessibleRole.ToolBar;
			return item;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a context menu.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static ContextMenuStrip CreateContextMenu()
		{
			ContextMenuStrip item = new ContextMenuStrip();
			item.ShowItemToolTips = false;
			item.ItemAdded += HandleItemAddedToOwner;
			item.ItemRemoved += HandleItemRemovedFromOwner;
			item.AccessibleRole = AccessibleRole.MenuPopup;
			return item;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the menu item.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static ToolStripMenuItem CreateMenuItem()
		{
			ToolStripMenuItem item = new ToolStripMenuItem();
			s_extenders[item] = new ToolStripItemExtender(item);
			item.DropDown.ShowItemToolTips = false;
			item.DropDown.ItemAdded += HandleItemAddedToOwner;
			item.DropDown.ItemRemoved += HandleItemRemovedFromOwner;
			item.AccessibleRole = AccessibleRole.MenuItem;
			return item;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a ToolStripButton.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static ToolStripButton CreateButton()
		{
			ToolStripButton item = new ToolStripButton();
			s_extenders[item] = new ToolStripItemExtender(item);
			item.AccessibleRole = AccessibleRole.PushButton;
			return item;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a ToolStripSplitButton.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static ToolStripSplitButton CreateSplitButton()
		{
			CheckableSplitButton item = new CheckableSplitButton();
			s_extenders[item] = new ToolStripItemExtender(item);
			item.DropDown.ShowItemToolTips = false;
			item.DropDown.ItemAdded += HandleItemAddedToOwner;
			item.DropDown.ItemRemoved += HandleItemRemovedFromOwner;
			item.AccessibleRole = AccessibleRole.SplitButton;
			return item as ToolStripSplitButton;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a ToolStripDropDownButton.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static ToolStripDropDownButton CreateDropDownButton()
		{
			ToolStripDropDownButton item = new ToolStripDropDownButton();
			s_extenders[item] = new ToolStripItemExtender(item);
			item.DropDown.ShowItemToolTips = true;
			item.DropDown.ItemAdded += HandleItemAddedToOwner;
			item.DropDown.ItemRemoved += HandleItemRemovedFromOwner;
			item.AccessibleRole = AccessibleRole.ButtonDropDown;
			return item;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a ToolStripComboBox.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static ToolStripComboBox CreateComboBox()
		{
			ToolStripComboBox item = new ToolStripComboBox();
			s_extenders[item] = new ToolStripItemExtender(item);
			item.AccessibleRole = AccessibleRole.ComboBox;
			return item;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a ToolStripControlHost.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static ToolStripControlHost CreateControlHost(Control ctrl)
		{
			Debug.Assert(ctrl != null);
			ToolStripControlHost item = new ToolStripControlHost(ctrl);
			s_extenders[item] = new ToolStripItemExtender(item);
			return item;
		}

		#endregion
	}

	#endregion

	#region CheckableSplitButton class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Subclass for a ToolStripSplitButton that provides a checked property. When checked,
	/// we have to draw the button ourselves.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class CheckableSplitButton : ToolStripSplitButton
	{
		private bool m_checked = false;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool Checked
		{
			get { return m_checked; }
			set
			{
				m_checked = value;
				Invalidate();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			if (!m_checked || Selected || DropDown.Visible)
				return;

			Rectangle rc = ButtonBounds;
			rc.Width += DropDownButtonBounds.Width;

			// Draw the background
			using (LinearGradientBrush br = new LinearGradientBrush(rc,
				ProfessionalColors.ButtonCheckedGradientBegin,
				ProfessionalColors.ButtonCheckedGradientEnd, LinearGradientMode.Vertical))
			{
				e.Graphics.FillRectangle(br, rc);
			}

			// Draw the borders
			using (Pen pen = new Pen(ProfessionalColors.ButtonPressedBorder))
			{
				// Draw border around left portion of split button.
				rc = ButtonBounds;
				rc.Height--;
				e.Graphics.DrawRectangle(pen, rc);

				// Draw border around right portion of split button.
				rc = DropDownButtonBounds;
				rc.X--;
				rc.Height--;
				e.Graphics.DrawRectangle(pen, rc);
			}

			// Draw arrow
			using (SolidBrush br = new SolidBrush(ForeColor))
			{
				rc = DropDownButtonBounds;
				Point[] pts = new Point[3];
				Point pt = new Point(
					rc.X + (int)((float)(rc.Width - 5) / 2),
					rc.Y + (int)((float)rc.Height / 2) - 1);

				pts[0] = pt;
				pts[1] = new Point(pt.X + 5, pt.Y);
				pt.X += 2 ;
				pt.Y += 3;
				pts[2] = pt;
				e.Graphics.FillPolygon(br, pts);
			}

			// Draw the image
			rc = ButtonBounds;
			while (rc.Width > Image.Width)
				rc.Inflate(-1, 0);

			while (rc.Height > Image.Height)
				rc.Inflate(0, -1);

			e.Graphics.DrawImage(Image, rc);
		}
	}

	#endregion
}

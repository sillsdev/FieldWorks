// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: XWindow.cs
// Authorship History: John Hatton
// Last reviewed:
//
// <remarks>
// </remarks>
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Xml;
using Microsoft.Win32;
using SIL.Utils;

namespace XCore
{
	/// <summary>
	/// XWindow is a window which is configured with XML file.
	/// </summary>
	public class XWindow : Form, IFWDisposable, IxCoreColleague, IxWindow
	{
		#region Data members
		/// <summary>
		/// Main CollapsingSplitContainer control for XWindow.
		/// It holds the Sidebar (m_sidebar) in its Panel1 (left side).
		/// It holds m_mainContentControl in Panel2, when m_recordBar is not showing.
		/// It holds another CollapsingSplitContainer (m_secondarySplitContainer) in Panel2,
		/// when the record list and the main control are both showing.
		///
		/// Controlling properties are:
		/// This is always true.
		/// property name="ShowSidebar" bool="true" persist="true"
		/// This is the splitter distance for the sidebar/secondary splitter pair of controls.
		/// property name="SidebarWidthGlobal" intValue="140" persist="true"
		/// This property is driven by the needs of the current main control, not the user.
		/// property name="ShowRecordList" bool="false" persist="true"
		/// This is the splitter distance for the record list/main content pair of controls.
		/// property name="RecordListWidthGlobal" intValue="200" persist="true"
		/// </summary>
		protected CollapsingSplitContainer m_mainSplitContainer;

		#region Left side control of m_mainSplitContainer
		/// <summary>
		/// This is currently always present.
		/// It replaces the temporary m_sideBarPlaceholderPanel panel.
		/// </summary>
		protected Control m_sidebar; // Dispose manually, if it has no parent control.
		// Temporary panel used for Designer. It gets replaced by the real m_sidebar early one.
		private Panel m_sideBarPlaceholderPanel;
		#endregion Left side controls of m_mainSplitContainer

		#region Right side control of m_mainSplitContainer
		/// <summary>
		/// This splitting control includes both the record bar (m_recordBar) and m_mainContentControl,
		/// when the
		/// </summary>
		private CollapsingSplitContainer m_secondarySplitContainer;
		/// <summary>
		/// Left side child of m_secondarySplitContainer.
		/// This is present or absent as determined by the configuration files.
		/// When it is 'absent', Panle1 is simply collapsed.
		/// </summary>
		private RecordBar m_recordBar; // Dispose manually, if it has no parent control.
		/// <summary>
		/// Temporary panel used for Designer. It gets replaced by the real m_mainContentControl, eventually.
		/// </summary>
		private Panel m_mainContentPlaceholderPanel;
		/// <summary>
		/// The real content control on the right of the main split container.
		/// </summary>
		/// <remarks>
		/// m_mainContentControl MUST implement the composite IxCoreContentControl interface.
		/// That interfaces has its own definition,
		/// but derives from IXCoreUserControl, and IxCoreColleague
		/// just to make sure all of those interfaces are present in the main content control.
		/// </remarks>
		protected Control m_mainContentControl; // Dispose manually, if it has no parent control.

		#endregion Right side control of m_mainSplitContainer

		protected bool m_persistWindowSize = true;
		protected Mediator m_mediator;
		protected PropertyTable m_propertyTable;
		protected Set<IUIAdapter> m_adapters = new Set<IUIAdapter>();
		protected ChoiceGroupCollection m_menusChoiceGroupCollection;
		protected ChoiceGroupCollection m_sidebarChoiceGroupCollection;
		protected ChoiceGroupCollection m_toolbarsChoiceGroupCollection;
		protected XmlNode m_windowConfigurationNode = null;
		private string m_configurationPath;
		protected IUIAdapter m_rebarAdapter;
		protected IUIAdapter m_sidebarAdapter;
		protected IUIAdapter m_menuBarAdapter;
		private ImageList builtInImages;
		protected static int kDefaultTreeBarWidth = 70;
		protected StatusBar m_bar = null;
		protected Dictionary<string, StatusBarPanel> m_statusPanels = new Dictionary<string, StatusBarPanel>(10);
		private StatusBarSizeGrip m_sizeGrip;
		protected ImageCollection m_smallImages = new ImageCollection(false);
		protected ImageCollection m_largeImages = new ImageCollection(true);
		private Timer m_widgetUpdateTimer;
		private const int WM_BROADCAST_ITEM_INQUEUE = 0x8000+0x77;	// wm_app + 0x77 : msg for mediator defered broadcast calls
		private IContainer components = null;
		// Used to count the number of times we've been asked to suspend Idle processing.
		private int m_cSuspendIdle = 0;
		private bool m_computingMinSize = false;
		private bool m_fullyInitialized = false; // Can't really be true, until the content control is added.
		string m_lastContentAssemblyPath;
		string m_lastContentClass;
		XmlNode m_lastContentClassNode;

		#endregion Data members

		#region Properties

		/// <summary>
		/// Gets an offset from the system's local application data folder.
		/// Subclasses should override this property
		/// if they want a folder within the base folder.
		/// </summary>
		protected virtual string LocalApplicationDataOffset
		{
			get { return ""; }
		}

		/// <summary>
		/// Gets the pathname of the "crash detector" file.
		/// The file is created at app startup, and deleted at app exit,
		/// so if it exists before being created, the app didn't close properly,
		/// and will start without using the saved settings.
		/// </summary>
		public string CrashOnStartupDetectorPathName
		{
			get
			{
				CheckDisposed();

				return Path.Combine(
					Path.Combine(
					System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
					LocalApplicationDataOffset),
					"CrashOnStartupDetector.tmp");
			}
		}

		/// <summary>
		/// Get the IContainer for this window.
		/// </summary>
		public IContainer MyContainer
		{
			get
			{
				CheckDisposed();
				return components;
			}
		}

		/// <summary>
		///	warning: will be null if the configuration did not specify any menus
		/// </summary>
		public ChoiceGroupCollection MenusChoiceGroupCollection
		{
			get
			{
				CheckDisposed();

				return m_menusChoiceGroupCollection;
			}
		}

		/// <summary>
		///	warning: will be null if the configuration did not specify any toolbars
		/// </summary>
		public ChoiceGroupCollection ToolbarsChoiceGroupCollection
		{
			get
			{
				CheckDisposed();

				return m_toolbarsChoiceGroupCollection;
			}
		}

		/// <summary>
		/// this allows the calling application to talk to the window.
		/// </summary>
		public Mediator Mediator
		{
			get
			{
				CheckDisposed();

				return m_mediator;
			}
		}

		#region IPropertyTableProvider Members

		public PropertyTable PropTable
		{
			get
			{
				CheckDisposed();

				return m_propertyTable;
			}
		}

		#endregion

		public IUIAdapter MenuAdapter
		{
			get
			{
				CheckDisposed();

				return m_menuBarAdapter;
			}
		}

		/// <summary>
		/// This should normally only be used for unit testing
		/// </summary>
		public Control CurrentContentControl
		{
			get
			{
				CheckDisposed();
				return m_mainContentControl;
			}
		}

		private IxCoreContentControl MainContentControlAsIxCoreContentControl
		{
			get { return m_mainContentControl as IxCoreContentControl; }
		}

//		private IXCoreUserControl MainContentControlAsIXCoreUserControl
//		{
//			get { return m_mainContentControl as IXCoreUserControl; }
//		}

		private IxCoreColleague MainContentControlAsIxCoreColleague
		{
			get { return m_mainContentControl as IxCoreColleague; }
		}

		public RecordBar TreeBarControl
		{
			get
			{
				CheckDisposed();
				return m_recordBar;
			}
		}
		public Control TreeStyleRecordList
		{
			get
			{
				CheckDisposed();
				return m_recordBar.TreeView;
			}
		}

		public Control ListStyleRecordList
		{
			get
			{
				CheckDisposed();
				return m_recordBar.ListView;
			}
		}

		#endregion Properties

		#region Ctrl+(Shift)+Tab methods

		/// <summary>
		/// The control currently in focus.
		/// </summary>
		/// <returns></returns>
		public static Control FocusedControl()
		{
			Control focusControl = Control.FromHandle(Win32.GetFocus());
			return focusControl;
		}

		/// <summary>
		/// Finds the first control of the given name under the parentControl.
		/// </summary>
		/// <param name="parentControl"></param>
		/// <param name="nameOfChildToFocus"></param>
		/// <returns></returns>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "controls contains references")]
		public static Control FindControl(Control parentControl, string nameOfChildToFocus)
		{
			Control firstControl = null;
			if (!String.IsNullOrEmpty(nameOfChildToFocus))
			{
				if (parentControl.Name == nameOfChildToFocus)
				{
					firstControl = parentControl;
				}
				else
				{
					Control[] controls = parentControl.Controls.Find(nameOfChildToFocus, true);
					firstControl = controls != null && controls.Length > 0 ? controls[0] : null;
				}
			}
			return firstControl;
		}

		/// <summary>
		/// Move focus to next/previous pane.
		/// 'Pane' here means:
		/// 1. the Sidebar (m_sidebar),
		/// 2. the record list, (if showing at all, m_recordBar),
		/// 3. the first or second control of a MultiPane, or a parent MultiPane.
		/// 4. the main content control, if is not a MultiPane, or 'focusedControl'
		/// is not contained in a MultiPane.
		/// </summary>
		/// <param name="fForward"></param>
		/// <returns>control in pane that got the focus.</returns>
		private Control MoveToNextPane(bool fForward)
		{
			Control focusedControl = FocusedControl();

			if (focusedControl == this)
				return null;

			Control indexControl = null;
			// We can make a complete collection of candidates here.
			// In all cases, we add m_sidebar, since it is always showing.
			// We may want to include m_recordBar, if it is also showing.
			// We then want one or more controls from the m_mainContentControl.
			// (Note: m_mainContentPlaceholderPanel will be replaced by m_mainContentControl in normal operations,
			// but is is theoretically possible it is a current option.)
			List<Control> targetCandidates = new List<Control>();
			// The m_sidebar is currently always showing (as of Jan 11, 2007),
			// but it may not be in the future, so add it is it is showing.
			if (m_mainSplitContainer.Panel1.Controls[0] == m_sidebar)
			{
				if (m_sidebar.ContainsFocus)
				{
					targetCandidates.Add(focusedControl);
					indexControl = focusedControl;
				}
				else
				{
					Control target = m_sidebar;
					foreach (Control child in m_sidebar.Controls)
					{
						if (child.Controls.Count > 0)
						{
							Control innerChild = child.Controls[0];
							if (innerChild is ListView)
							{
								if ((innerChild as ListView).SelectedItems.Count > 0)
								{
									target = innerChild;
									break;
								}
							}
						}
					}
					targetCandidates.Add(target);
				}
			}
			if (!m_secondarySplitContainer.Panel1Collapsed
				&& m_secondarySplitContainer.Panel1.Controls[0] == m_recordBar)
			{
				if (m_recordBar.ContainsFocus)
				{
					targetCandidates.Add(focusedControl);
					indexControl = focusedControl;
				}
				else
				{
					// It looks like the record list can deal with waht is selected,
					// so just toss in the whole thing.
					targetCandidates.Add(m_recordBar);
				}
			}

			if (!m_secondarySplitContainer.Panel2Collapsed
				&& m_secondarySplitContainer.Panel2.Controls[0] == m_mainContentControl)
			{
				// Now deal with the m_mainContentControl side of things.
				Control otherControl = (m_mainContentControl as IxCoreCtrlTabProvider).PopulateCtrlTabTargetCandidateList(targetCandidates);
				if (otherControl != null)
				{
					Debug.Assert(indexControl == null, "indexCntrol should have been null.");
					indexControl = otherControl;
				}
			}
			Debug.Assert(indexControl != null, "Couldn't find the focused control anywhere.");
			Debug.Assert(targetCandidates.Contains(indexControl));
			int srcIndex = targetCandidates.IndexOf(indexControl);
			int targetIndex = 0;
			if (fForward)
			{
				targetIndex = srcIndex + 1 < targetCandidates.Count ? srcIndex + 1 : 0;
			}
			else
			{
				targetIndex = srcIndex > 0 ? srcIndex - 1 : targetCandidates.Count - 1;
			}

			Control newFocusedControl = targetCandidates[targetIndex];
			newFocusedControl.Focus(); // Note: may result in Focusing in a subcontrol.
			return newFocusedControl;
		}

		#endregion  Ctrl+(Shift)+Tab methods

		#region Initialization

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="XCoreMainWnd"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public XWindow()
		{
			AccessibleName = GetType().Name;
			BootstrapPart1();

			m_smallImages.AddList(builtInImages,new[]{"default"}); //a question mark for when icons are missing
			m_largeImages.AddList(builtInImages, new[] { "default" }); //a question mark for when icons are missing
		}

		private void BootstrapPart1()
		{
			m_mediator = new Mediator();
			m_propertyTable = new PropertyTable(m_mediator);
			// No broadcasting until it has our handle (see OnHandleCreated)
			m_mediator.SpecificToOneMainWindow = true;

			InitializeComponent();
		}

		/// <summary>
		/// Note that the first image of the first list is the default image for
		/// cases where the specified image is not declared war is not found.
		/// </summary>
		/// <remarks> this method allows the containing application or a subclass to directly add images.
		/// We might want to retire this as unnecessary since images can now be added via the configuration file.</remarks>
		/// <param name="images"></param>
		/// <param name="labels"></param>
		public void AddSmallImageList(ImageList images, string[] labels)
		{
			CheckDisposed();

			m_smallImages.AddList(images, labels);
		}

		public void AddLargeImageList(ImageList images, string[] labels)
		{
			CheckDisposed();

			m_largeImages.AddList(images, labels);
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		protected void LoadResources(XmlNode configurationNode)
		{
			if (configurationNode == null)
				return;
			m_smallImages.AddList(configurationNode.SelectNodes("imageList[@size='small']"));
			m_largeImages.AddList(configurationNode.SelectNodes("imageList[@size='large']"));
			//if they forgot to say what size it is, stick it in the small list.
			m_smallImages.AddList(configurationNode.SelectNodes("imageList[@size=null]"));

			//make image list available from the property table
			PropTable.SetProperty("smallImages", m_smallImages, true);
			PropTable.SetPropertyPersistence("smallImages", false);
			PropTable.SetProperty("largeImages", m_largeImages, true);
			PropTable.SetPropertyPersistence("largeImages", false);
		}

		//the <defaultProperties> section of the configuration can be used to make defaults
		//which are different from the defaults that can be found in the code. That is,
		//Code should still set default values and not rely on someone including a default definition
		//in the configuration file.
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		protected void LoadDefaultProperties(XmlNode configurationNode)
		{
			if (configurationNode == null)
				return;

			foreach(XmlNode node in configurationNode.SelectNodes("property"))
			{
				string name = XmlUtils.GetManditoryAttributeValue(node, "name");
				string listId = XmlUtils.GetOptionalAttributeValue(node, "list");
				// get the settingsGroup for this property.
				PropertyTable.SettingsGroup settingsGroup = ChoiceGroup.GetSettingsGroup(node, PropertyTable.SettingsGroup.Undecided);
				/* These initial conditions are a conceptual weak spot in XCore... for example,
				 * who is listening to the broadcast that these properties have been changed? And are they set up yet
				 * such that they can receive the broadcast and do the right thing?
				 * For sure, changing this to some tool (XWorks) does not actually change the initial content control,
				 * said that if the default property is said to something other than the control that is set in the
				 * initial content control element, then the sidebar display (which *is* affected by this code)
				 * may show that one tool is selected but actually another one is. Something of a TODO...
				 */
				if (!string.IsNullOrEmpty(listId))
				{
					string listItemValue = XmlUtils.GetManditoryAttributeValue(node, "listItemValue");
					XmlNode listNode = configurationNode.SelectSingleNode("//lists/list[@id='" + listId + "']");
					if (listNode == null)
						throw new ConfigurationException("List not found", node);

					XmlNode itemNode = listNode.SelectSingleNode("item[@value='" + listItemValue + "']");
					if (itemNode == null)
						throw new ConfigurationException("list item not found", node);

					XmlNode parametersNode = itemNode.SelectSingleNode("parameters");//OK if this is null
					ChoiceGroup.ChooseSinglePropertyAtomicValue(m_mediator, m_propertyTable, listItemValue, parametersNode, name,
						settingsGroup);
				}
				else if(node.Attributes["bool"] != null)
				{
					m_propertyTable.SetDefault(
						name,
						XmlUtils.GetBooleanAttributeValue(node, "bool"),
						settingsGroup,
						true);
				}
				else if(node.Attributes["intValue"] != null)
				{
					m_propertyTable.SetDefault(
						name,
						XmlUtils.GetMandatoryIntegerAttributeValue(node, "intValue"),
						settingsGroup,
						true);
				}
					//this one allows us to just create an object on-the-fly and stick it directly in a property.
				else if(node.Attributes["assemblyPath"] != null)
				{
					m_propertyTable.SetDefault(
						name,
						DynamicLoader.CreateObject(node, m_mediator),
						settingsGroup,
						true);
					m_propertyTable.SetPropertyPersistence(name, false, settingsGroup);
				}
				else
				{
					// won't be null if a command line param was used to push in a value
					if (!m_propertyTable.PropertyExists(name))
					{
						m_propertyTable.SetDefault(
							name,
							XmlUtils.GetManditoryAttributeValue(node, "value"),
							settingsGroup,
							true);
					}
				}

				if(node.Attributes["persist"] != null)
				{
					m_propertyTable.SetPropertyPersistence(name, XmlUtils.GetBooleanAttributeValue(node, "persist"), settingsGroup);
				}
			}
		}

		/// <summary>
		/// Listeners are just colleagues which are always listening, but are not the content filling colleague.
		/// They include objects which launch dialog boxes in response to menu items.
		/// </summary>
		/// <param name="configurationNode"></param>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		protected void LoadListeners(XmlNode configurationNode)
		{
			if (configurationNode == null)
				return;
			foreach(XmlNode node in configurationNode.SelectNodes("listener"))
			{
				Object listener = DynamicLoader.CreateObject(node);
				// Note: It is up to the colleague to add itself to the mediator's list of colleagues.
				((IxCoreColleague)listener).Init(m_mediator, m_propertyTable, node.SelectSingleNode("parameters"));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads the UI from an XML document embedded in a stream
		/// </summary>
		/// <param name="configStream">Stream with XML document</param>
		/// ------------------------------------------------------------------------------------
		public void LoadUI(Stream configStream)
		{
			CheckDisposed();

			XmlDocument configuration = new XmlDocument();

			try
			{
				configuration.Load(configStream);
			}
			catch (Exception error)
			{
				ErrorReporter.ReportException(error, ApplicationRegistryKey,
					m_propertyTable.GetValue<IFeedbackInfoProvider>("FeedbackInfoProvider").SupportEmailAddress);
			}
			LoadUIFromXmlDocument(configuration, null);
		}

		/// <summary>
		/// Loads the configuration file at the specified path and resolves any nodes
		/// that need to get included from other files indicated by internal relative paths.
		/// </summary>
		/// <param name="configurationPath"></param>
		/// <returns></returns>
		public static XmlDocument LoadConfigurationWithIncludes(string configurationPath)
		{
			return LoadConfigurationWithIncludes(configurationPath, false);
		}

		/// <summary>
		/// Loads the configuration file at the specified path and resolves any nodes
		/// that need to get included from other files indicated by internal relative paths.
		/// </summary>
		/// <param name="configurationPath"></param>
		/// <param name="skipMissingFiles">if true, missing files are skipped rather than throwing exception</param>
		/// <returns></returns>
		public static XmlDocument LoadConfigurationWithIncludes(string configurationPath, bool skipMissingFiles)
		{
			XmlDocument configuration = new XmlDocument();

			configuration.Load(configurationPath);

			//process <include> elements
			SimpleResolver resolver = new SimpleResolver();

			//do includes relative to the dir of our config file
			resolver.BaseDirectory = System.IO.Path.GetDirectoryName(configurationPath);
			XmlIncluder includer = new XmlIncluder(resolver);
			includer.SkipMissingFiles = skipMissingFiles;
			includer.ProcessDom(configurationPath, configuration);
			return configuration;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads the UI from an XML file
		/// </summary>
		/// <param name="configurationPath">Path and name of an XML file</param>
		/// ------------------------------------------------------------------------------------
		public void LoadUI(string configurationPath)
		{
			CheckDisposed();
			m_configurationPath = configurationPath;

			XmlDocument configuration = XWindow.LoadConfigurationWithIncludes(configurationPath);

#if SAVEFULLCONFIGFILE
			configuration.Save(@"C:\xWindowFullConfig.xml");
#endif
			LoadUIFromXmlDocument(configuration, configurationPath);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load UI from an XML document
		/// </summary>
		/// <param name="configuration"></param>
		/// <param name="configurationPath"></param>
		/// ------------------------------------------------------------------------------------
		protected virtual void LoadUIFromXmlDocument(XmlDocument configuration, string configurationPath)
		{
			SuspendLayoutAll();

			bool wasCrashDuringPreviousStartup = File.Exists(CrashOnStartupDetectorPathName);
			if (!wasCrashDuringPreviousStartup)
			{
				// Create the crash detector file for next time.
				// Make sure the folder exists first.
				Directory.CreateDirectory(CrashOnStartupDetectorPathName.Substring(0, CrashOnStartupDetectorPathName.LastIndexOf(Path.DirectorySeparatorChar)));
				using (StreamWriter writer = File.CreateText(CrashOnStartupDetectorPathName))
					writer.Close();
			}

			m_windowConfigurationNode = configuration.SelectSingleNode("window");

			m_propertyTable.SetProperty("WindowConfiguration", m_windowConfigurationNode, true);
			m_propertyTable.SetPropertyPersistence("WindowConfiguration", false);

			SetApplicationName();

			//nb:some things might be sensitive to when this actually happens
			LoadDefaultProperties(m_windowConfigurationNode.SelectSingleNode("defaultProperties"));

			m_propertyTable.SetProperty("window", this, true);
			m_propertyTable.SetPropertyPersistence("window", false);
			var st = StringTable.Table; // Makes ure it is loaded.
			LoadResources(m_windowConfigurationNode.SelectSingleNode("resources"));

			//make the command set
			CommandSet commandset = new CommandSet(m_mediator);
			commandset.Init(m_windowConfigurationNode);
			m_mediator.Initialize(commandset);
			RestoreWindowSettings(wasCrashDuringPreviousStartup);
			Size restoreSize = Size;

			// Some of the listener initialization depends on PropertyTable initialization which
			// occurs in RestoreWindowSettings() above (e.g. LT-14150 re: 'sticky' spell checking).
			LoadListeners(m_windowConfigurationNode.SelectSingleNode("listeners"));

			// Note: This will throw an exception, if the Init method has already been called.
			// It is 'poor form' to try and add a colleague more than once,
			// even though we could cope with it.
			m_mediator.AddColleague(this);
			Assembly adaptorAssembly = GetAdapterAssembly();

			m_propertyTable.SetProperty("uiAdapter", adaptorAssembly, true);
			m_propertyTable.SetPropertyPersistence("uiAdapter", false);

			//add the menubar
			Control menubar;
			m_menusChoiceGroupCollection = MakeMajorUIPortion(
				adaptorAssembly,
				m_windowConfigurationNode,
				"menubar",
				"XCore.MenuAdapter",
				out menubar,
				out m_menuBarAdapter);

			if (menubar != null && menubar.Parent != null)
			{
				Control parent = menubar.Parent;
				if (parent.AccessibleName == null)
					parent.AccessibleName = "ParentOf" + menubar.AccessibleName;
			}

			//add the toolbar
			Control rebar;
			m_toolbarsChoiceGroupCollection = MakeMajorUIPortion(
				adaptorAssembly,
				m_windowConfigurationNode,
				"toolbars",
				"XCore.ReBarAdapter",
				out rebar,
				out m_rebarAdapter);

			if (rebar != null && rebar.Parent != null)
			{
				if (rebar.Parent.AccessibleName == null)
					rebar.Parent.AccessibleName = "ParentOf" + rebar.AccessibleName;
			}

			// Start of main layout.
			// Add the sidebar.
			m_sidebarChoiceGroupCollection = MakeMajorUIPortion(
				adaptorAssembly,
				m_windowConfigurationNode,
				"sidebar",
				"XCore.SidebarAdapter",
				out m_sidebar,
				out m_sidebarAdapter);
			m_sidebar.AccessibleName = "SideBar";
			// Remove m_sideBarPlaceholderPanel (a placeholder) and replace it with the real m_sidebar.
			m_sidebar.Dock = DockStyle.Fill;
			m_sidebar.TabStop = true;
			m_sidebar.TabIndex = 0;
			m_mainSplitContainer.FirstControl = m_sidebar;
			m_mainSplitContainer.Tag = "SidebarWidthGlobal";
			m_mainSplitContainer.Panel1MinSize = CollapsingSplitContainer.kCollapsedSize;
			m_mainSplitContainer.Panel1Collapsed = !m_propertyTable.GetBoolProperty("ShowSidebar", false); // Andy Black wants to collapse it for one of his XCore apps.
			m_mainSplitContainer.Panel2Collapsed = false; // Never collapse the main content control, plus optional record list.
			int sd = m_propertyTable.GetIntProperty("SidebarWidthGlobal", 140);
			if (!m_mainSplitContainer.Panel1Collapsed)
				SetSplitContainerDistance(m_mainSplitContainer, sd);
			if (m_sideBarPlaceholderPanel != null)
			{
				m_sideBarPlaceholderPanel.Dispose();
				m_sideBarPlaceholderPanel = null;
			}
			m_mainSplitContainer.FirstLabel = m_propertyTable.GetValue<string>("SidebarLabel");
			m_mainSplitContainer.SecondLabel = m_propertyTable.GetValue<string>("AllButSidebarLabel");

			// Maybe show the record list.
			m_recordBar.Dock = DockStyle.Fill;
			m_recordBar.TabStop = true;
			m_recordBar.TabIndex = 1;
			m_secondarySplitContainer.Panel1Collapsed = !m_propertyTable.GetBoolProperty("ShowRecordList", false);
			// Always show the main content control.
			m_secondarySplitContainer.Panel1MinSize = CollapsingSplitContainer.kCollapsedSize;
			m_secondarySplitContainer.Panel2Collapsed = false;
			m_secondarySplitContainer.Tag = "RecordListWidthGlobal";
			sd = m_propertyTable.GetIntProperty("RecordListWidthGlobal", 200);
			SetSplitContainerDistance(m_secondarySplitContainer, sd);
			m_secondarySplitContainer.FirstLabel = m_propertyTable.GetValue<string>("RecordListLabel");
			m_secondarySplitContainer.SecondLabel = m_propertyTable.GetValue<string>("MainContentLabel");
			// End of main layout.

			CreateStatusBar(m_windowConfigurationNode);

			// Add the content control
			// Note: We should be able to do it directly, since everything needed is in the default properties.
			SetInitialContentObject(m_windowConfigurationNode);
			m_sidebarAdapter.FinishInit();
			m_menuBarAdapter.FinishInit();

			// Some adapters modify the window size, so reset it to what was in the XML file.
			// Technically, this should assert that they are the same,
			// as it should now never call this code.
			if (restoreSize != Size)
			{
				// It will be the same as what is now in the file and the prop table,
				// so skip updating the table.
				m_persistWindowSize = false;
				Size = restoreSize;
				m_persistWindowSize = true;
			}

			if (File.Exists(CrashOnStartupDetectorPathName)) // Have to check again, because unit test check deletes it in the RestoreWindowSettings method.
				File.Delete(CrashOnStartupDetectorPathName);

			// this.ResumeLayout(); // Don't resume until after the maio content control is added.

			ClearRecordBarList();//sets up event handling
			ResumeLayoutAll();
		}

		private void SuspendLayoutAll()
		{
			m_fullyInitialized = false; // Can't really be true, until the content control is added.
			SuspendLayout();
			m_mainSplitContainer.SuspendLayout();
		}

		private void ResumeLayoutAll()
		{
			m_fullyInitialized = true;
			m_mainSplitContainer.ResumeLayout();
			ResumeLayout();
		}

		private Assembly GetAdapterAssembly()
		{
			Assembly adaptorAssembly = null;
			// load an adapter library from the same directory as the .dll we're running
			// We strip file:/ because that's not accepted by LoadFrom()
			var codeBasePath = FileUtils.StripFilePrefix(Assembly.GetExecutingAssembly().CodeBase);
			string baseDir = Path.GetDirectoryName(codeBasePath);

			string preferredLibrary = m_propertyTable.GetValue(
				"PreferredUILibrary", "xCoreOpenSourceAdapter.dll");

			try
			{
				adaptorAssembly = Assembly.LoadFrom(Path.Combine(baseDir, preferredLibrary));
			}
			catch
			{
				adaptorAssembly = Assembly.LoadFrom(
					Path.Combine(baseDir, "xCoreOpenSourceAdapter.dll"));
			}
			Debug.Assert(adaptorAssembly != null, "XCore Could not find an adapter library DLL to use.");
			return adaptorAssembly;
		}

		private void SetApplicationName()
		{
			string applicationName = XmlUtils.GetAttributeValue(m_windowConfigurationNode, "label", "application name?");
			ErrorReporter.AddProperty("Application", applicationName);
			m_propertyTable.SetProperty("applicationName", applicationName, true);
			m_propertyTable.SetPropertyPersistence("applicationName", false);
			UpdateCaptionBar();
		}

		/// <summary>
		/// regardless of the adapter, this returns a standard Windows forms context menu for cases where
		/// the caller really needs to access the contents of the menu, for example when turning the menu into hyperlinks.
		/// note that this menu will not actually do anything; the caller will have to hook the items up to real events.
		/// </summary>
		/// <param name="menuId"></param>
		/// <returns></returns>
		public ContextMenu GetWindowsFormsContextMenu(string menuId)
		{
			CheckDisposed();

			ChoiceGroup group=GetChoiceGroupForMenu(menuId);
			group.PopulateNow();
			ContextMenu m = new ContextMenu();
			foreach(ChoiceRelatedClass item in group)
			{
				if(item is ChoiceBase)
				{
					AdapterMenuItem mi = new AdapterMenuItem();//this is a simple wrapper which adds a tag
					mi.Text = (item.Label);
					mi.Tag = item;
					m.MenuItems.Add(mi);
				}
			}
			return m;
		}

		/// <summary>
		/// get the choice group corresponding to a menuid
		/// </summary>
		/// <param name="menuId"></param>
		/// <returns></returns>
		public ChoiceGroup GetChoiceGroupForMenu(string menuId)
		{
			CheckDisposed();

			XmlNode node = GetContextMenuNodeFromMenuId(menuId);
			return new ChoiceGroup(m_mediator, m_propertyTable, m_menuBarAdapter, node, null);
		}

		/// <summary>
		/// Get a context menu for the specified menu id, and in addition.
		/// </summary>
		/// <param name="menuId"></param>
		/// <param name="group"></param>
		/// <param name="actModal">don't return until the popup is closed</param>
		/// <returns></returns>
		public void ShowContextMenu(string menuId, Point location,
			TemporaryColleagueParameter temporaryColleagueParam,
			MessageSequencer sequencer)
		{
			ShowContextMenu(menuId, location, temporaryColleagueParam, sequencer, null);
		}

		/// <summary>
		/// Get a context menu for the specified menu id, and in addition.
		/// </summary>
		/// <param name="menuId"></param>
		/// <param name="group"></param>
		/// <param name="actModal">don't return until the popup is closed</param>
		/// <returns></returns>
		public void ShowContextMenu(string menuId, Point location,
			TemporaryColleagueParameter temporaryColleagueParam,
			MessageSequencer sequencer, Action<ContextMenuStrip> adjustMenu)
		{
			CheckDisposed();

			ChoiceGroup group = GetChoiceGroupForMenu(menuId);
			((IUIMenuAdapter)m_menuBarAdapter).ShowContextMenu(group, location, temporaryColleagueParam, sequencer, adjustMenu);
		}

		/// <summary>
		/// Make a menu from multiple menu definitions
		/// </summary>
		/// <param name="menuIds"></param>
		/// <param name="location"></param>
		/// <param name="temporaryColleagueParam">
		/// Optional object that holds a Mediator and an XCore colleague
		/// that is used to simulate the blocking of a modal menu, without it really being modal.
		/// That is, the menu will add the colleague to the Mediator before it shows, and then remove
		/// it after it closes.
		/// </param>
		/// <param name="sequencer">
		/// Optional object that holds a MessageSequencer obejct
		/// that is used to simulate the blocking of a modal menu, without it really being modal.
		/// That is, the menu will pause the MsgSeq before it shows, and then resume it
		/// it after it closes.
		/// </param>
		/// <returns></returns>
		public void ShowContextMenu(string[] menuIds, /*out ChoiceGroup group,*/ Point location,
			TemporaryColleagueParameter temporaryColleagueParam,
			MessageSequencer sequencer)
		{
			CheckDisposed();

			List<XmlNode> nodes = new List<XmlNode>(menuIds.Length);
			foreach (string m in menuIds)
			{
				if (m == null || m == "")
					continue; // that's ok
				XmlNode node = GetContextMenuNodeFromMenuId(m);
				nodes.Add(node);
			}
			ChoiceGroup group = new ChoiceGroup(m_mediator, m_propertyTable, m_menuBarAdapter, nodes, null);
			((IUIMenuAdapter)m_menuBarAdapter).ShowContextMenu(group, location, temporaryColleagueParam, sequencer);
		}

		/// <summary>
		/// returns the configuration node for the given menu id.
		/// </summary>
		/// <param name="m"></param>
		/// <returns></returns>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		public XmlNode GetContextMenuNodeFromMenuId(string menuId)
		{
			XmlNode node = m_windowConfigurationNode.SelectSingleNode(String.Format("//menu[@id='{0}']",
				XmlUtils.MakeSafeXmlAttribute(menuId)));
			if (node == null)
			{
				// Make up for weakness of XmlNode.SelectSingleNode.
				foreach (XmlNode xn in m_windowConfigurationNode.SelectNodes("//menu"))
				{
					string id = XmlUtils.GetAttributeValue(xn, "id");
					if (id == menuId)
					{
						node = xn;
						break;
					}
				}
			}
			if (node == null)
				throw new ConfigurationException(String.Format("Could not find a context menu for menu id '{0}'.", menuId));
			return node;
		}

		protected void CreateStatusBar(XmlNode windowConfigurationNode)
		{
			XmlNode configuration = windowConfigurationNode.SelectSingleNode("statusbar");
			if (configuration == null)
				return;
			try
			{
				StatusBar bar = new StatusBar();
				foreach(XmlNode part in configuration.SelectNodes("panel"))
				{
					StatusBarPanel panel;
					string id = XmlUtils.GetManditoryAttributeValue(part, "id");

					if (part.Attributes.GetNamedItem("assemblyPath") != null)
					{
						//load a custom status bar panel control (like the progress bar)
						panel = (StatusBarPanel) DynamicLoader.CreateObject(part, new object[] {bar});
					}
					else
					{
						panel = new StatusBarPanel();
					}
					m_propertyTable.SetProperty(id, panel, true);
					m_propertyTable.SetPropertyPersistence(id, false);

					string val = XmlUtils.GetOptionalAttributeValue(part, "width");
					if (val != null)
					{
						if(val == "Contents")
							panel.AutoSize = StatusBarPanelAutoSize.Contents;
						else
							panel.Width=int.Parse(val);
					}
					else
						panel.AutoSize = StatusBarPanelAutoSize.Spring;
					int width = XmlUtils.GetOptionalIntegerValue(part, "minwidth", 0);
					if (width > 0)
						panel.MinWidth = width;
					m_statusPanels.Add(id, panel);
					bar.Panels.Add(panel);
				}
				bar.SizingGrip = false;
				bar.ShowPanels = true;
				bar.Name = "StatusBar";
				bar.AccessibleName = "StatusBar";
				bar.Dock = DockStyle.Bottom;
				if (m_bar != null)
				{
					this.Controls.Remove(m_bar);
					m_bar.Dispose();
				}
				m_bar = bar;
				m_sizeGrip = new StatusBarSizeGrip(m_bar);
				this.Controls.Add(m_bar);
			}
			catch (Exception error)
			{
				throw new ConfigurationException("There was a problem reading the configuration for the status bar. " + error.Message, configuration, error);
			}
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "activeForm is a reference")]
		protected void RestoreWindowSettings(bool wasCrashDuringPreviousStartup)
		{
			string id = XmlUtils.GetAttributeValue(m_windowConfigurationNode,"settingsId");
			if (id == null)
				return;

			// Check for unit tests.
			if (id == "xWindowUnitTest" && wasCrashDuringPreviousStartup)
			{
				// Be ruthless for tests, since they don't remove the file after a successful run.
				File.Delete(CrashOnStartupDetectorPathName);
				wasCrashDuringPreviousStartup = false;
			}

			bool useExtantPrefs = Control.ModifierKeys != Keys.Shift; // Holding shift key means don't use extant preference file, no matter what.
			if (useExtantPrefs)
			{
				// Tentatively RestoreProperties, but first check for a previous crash...
				if (wasCrashDuringPreviousStartup && (System.Environment.ExpandEnvironmentVariables("%FwSkipSafeMode%") == "%FwSkipSafeMode%"))
				{
					// Note: The environment variable will not exist at all to get here.
					// A non-developer user.
					// Display a message box asking users if they
					// want to  revert to factory settings.
					this.ShowInTaskbar = true;
					var activeForm = Form.ActiveForm;
					if (activeForm == null)
						useExtantPrefs = (MessageBox.Show(Form.ActiveForm, xCoreStrings.SomethingWentWrongLastTime,
							id,
							MessageBoxButtons.YesNo,
							MessageBoxIcon.Question) != DialogResult.Yes);
					else
					{
						// Make sure as far as possible it comes up in front of any active window, including the splash screen.
						activeForm.Invoke((Action)(() =>
							useExtantPrefs = (MessageBox.Show(xCoreStrings.SomethingWentWrongLastTime,
								id,
								MessageBoxButtons.YesNo,
								MessageBoxIcon.Question) != DialogResult.Yes)));
					}
				}
			}
			if (useExtantPrefs)
			{
				RestoreProperties();
				// nb some subclasses will also reload database-specific properties later.
			}
			else
			{
				DiscardProperties();
			}

			if (m_propertyTable.PropertyExists("windowState"))
			{
				var state = m_propertyTable.GetValue<FormWindowState>("windowState");
				if (state != FormWindowState.Minimized)
				{
					WindowState = state;
				}
			}

			if (m_propertyTable.PropertyExists("windowLocation"))
			{
				Location = m_propertyTable.GetValue<Point>("windowLocation");
				//the location restoration only works if the window startposition is set to "manual"
				//because the window is not visible yet, and the location will be changed
				//when it is Show()n.
				StartPosition = FormStartPosition.Manual;
			}

			if (m_propertyTable.PropertyExists("windowSize"))
			{
				m_persistWindowSize = false;
				try
				{
					Size = m_propertyTable.GetValue<Size>("windowSize");
				}
				finally
				{
					m_persistWindowSize = true;
				}
			}
		}

		/// <summary>
		/// Restore properties persisted for the mediator.
		/// </summary>
		protected virtual void RestoreProperties()
		{
			m_propertyTable.RestoreFromFile(m_propertyTable.GlobalSettingsId);
		}

		/// <summary>
		/// If we don't RestoreProperties we may need to discard some information.
		/// For example if we are discarding a saved filter we need to discard the saved object sequences.
		/// </summary>
		protected virtual void DiscardProperties()
		{
			// Default is to do nothing (it's not necessary to actually delete the saved settings file).
		}

		/// <summary>
		/// sets up either the menubar, toolbar collection, or sidebar
		/// </summary>
		/// <param name="adaptorAssembly"></param>
		/// <param name="m_windowConfigurationNode"></param>
		/// <param name="elementName"></param>
		/// <param name="adapterClass"></param>
		/// <returns></returns>
		protected ChoiceGroupCollection MakeMajorUIPortion(Assembly adaptorAssembly, XmlNode m_windowConfigurationNode,
			string elementName, string adapterClass, out System.Windows.Forms.Control control, out IUIAdapter adapter)
		{
			adapter = null;
			//make the  adapter
			try
			{
				adapter = (IUIAdapter)adaptorAssembly.CreateInstance(adapterClass);
				m_adapters.Add(adapter);
			}
			catch (Exception e)
			{
				ErrorReporter.ReportException(e, ApplicationRegistryKey,
					m_propertyTable.GetValue<IFeedbackInfoProvider>("FeedbackInfoProvider").SupportEmailAddress);
			}

			Trace.Assert(adapter != null, "XCore could not create the adapter for " + adapterClass);

			control = adapter.Init(this, m_smallImages, m_largeImages, m_mediator, m_propertyTable);
			if (control != null)
			{
				control.Tag = adapter;
				// add an AccessibilityName
				if (control.AccessibleName == null)
					control.AccessibleName = elementName;
			}
			return MakeGroupSet(m_windowConfigurationNode, adapter, elementName);
		}

		/// <summary>
		/// sets up either the menubar, toolbar collection, or sidebar
		/// </summary>
		/// <param name="adaptorAssembly"></param>
		/// <param name="m_windowConfigurationNode"></param>
		/// <param name="elementName"></param>
		/// <param name="adapterClass"></param>
		/// <returns></returns>
		protected ChoiceGroupCollection MakeMajorUIPortion(Assembly adaptorAssembly, XmlNode m_windowConfigurationNode,
			string elementName, string adapterClass, out System.Windows.Forms.Control control)
		{
			IUIAdapter dummy;
			return MakeMajorUIPortion( adaptorAssembly, m_windowConfigurationNode,
				elementName,  adapterClass, out control, out dummy);
		}

		protected ChoiceGroupCollection MakeGroupSet(XmlNode m_windowConfigurationNode, IUIAdapter adapter, string elementName)
		{
			XmlNode configurationNode = m_windowConfigurationNode.SelectSingleNode(elementName);
			if (configurationNode== null)
				return null; //the configuration did not specify anything for this user interface thatelement
			ChoiceGroupCollection groupset = new ChoiceGroupCollection(m_mediator, m_propertyTable, adapter, configurationNode);
			groupset.Init();
			return groupset;
		}

		private void  SetInitialContentObject(XmlNode windowConfigurationNode)
		{
			//allow colleagues (i.e. a listener that has been installed, or, if there are none of those listening, then
			//our own "OnSetInitialContentObject" handler, to choose what our initial content control will be.
			m_mediator.SendMessage("SetInitialContentObject", windowConfigurationNode);
		}

		/// <summary>
		/// this is called by xWindow just before it sets the initial control which will actually
		/// take over the content area.  if no listener response to this message first, then this is called
		/// and we determine the controlled by looking at the "<contentClass/>" element in the configuration file.
		/// </summary>
		/// <remarks> this handler relies on the configuration file having a <contentClass/> element.</remarks>
		/// <param name="windowConfigurationNode"></param>
		/// <returns></returns>
		public bool OnSetInitialContentObject(object windowConfigurationNode)
		{
			CheckDisposed();

			XmlNode contentClassNode =  ((XmlNode)windowConfigurationNode).SelectSingleNode("contentClass");

			if(contentClassNode == null)
				throw new ArgumentException("xWindow.OnSetInitialContentObject called. The area listener should have been tried first and handled this. " + m_mediator.GetColleaguesDumpString());
			else
				ChangeContentObjectIfPossible(XmlUtils.GetAttributeValue(contentClassNode, "assemblyPath"),
					XmlUtils.GetAttributeValue(contentClassNode, "class"), contentClassNode);

			return true;	//we handled this.
		}

		public bool OnShowNotification(object notificationText)
		{
			CheckDisposed();

			using (NotifyWindow nw = new NotifyWindow((string)notificationText))
			{
				nw.SetDimensions(150, 150);
				nw.WaitTime = 4000;
				nw.Notify();
			}
			return true;	//we handled this.
		}

		#endregion Initialization

		#region Disposal

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged
		/// resources; <c>false</c> to release only unmanaged resources.
		/// </param>
		/// -----------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + ". ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
				ShutDownPart1();

			base.Dispose(disposing);

			if (disposing)
				ShutDownPart2();

			m_mainContentPlaceholderPanel = null;
			m_widgetUpdateTimer = null;
			m_mediator = null;
			//m_adapters = null;
			m_mainContentControl = null;
			m_recordBar = null;
			m_sidebar = null;
			//m_statusPanels = null;
			m_smallImages = null;
			m_largeImages = null;
		}

		/// <summary>
		/// This is the first of two parts in doing a warm boot/shut down.
		/// It is called before calling the base Dispose method in the Dispose system.
		/// </summary>
		private void ShutDownPart1()
		{

			if (m_mediator != null)
			{
				m_mediator.MainWindow = null;
				m_mediator.ProcessMessages = false;
				m_mediator.RemoveColleague(this);
			}
			if (m_widgetUpdateTimer != null)
			{
				m_widgetUpdateTimer.Stop();
				m_widgetUpdateTimer.Tick -= new System.EventHandler(this.WidgetUpdateTimer_Tick);
				m_widgetUpdateTimer.Dispose();
			}
			if (m_recordBar != null)
			{
				m_recordBar.TreeView.AfterSelect -= new TreeViewEventHandler(OnTreeBarAfterSelect);
				m_recordBar.ListView.SelectedIndexChanged -= new EventHandler(OnListBarSelect);
			}
			if (m_mainSplitContainer != null)
				m_mainSplitContainer.SplitterMoved -= new SplitterEventHandler(OnMainSplitterMoved);
			if (m_secondarySplitContainer != null)
				m_secondarySplitContainer.SplitterMoved -= new SplitterEventHandler(OnSecondarySplitterMoved);

			if (components != null)
			{
				components.Dispose();
			}

			if (m_adapters != null)
			{
				foreach (IUIAdapter adapter in m_adapters)
				{
					if (adapter is IDisposable)
						((IDisposable)adapter).Dispose();
				}
				m_adapters.Clear();
			}

			if (m_statusPanels != null)
			{
				StatusBarPanel[] tempArray = new StatusBarPanel[m_statusPanels.Values.Count];
				m_statusPanels.Values.CopyTo(tempArray, 0);
				m_statusPanels.Clear();

				// Ensure StatusBarPanel's are disposed to ensure Timer in
				// StatusBarProgressPanel is no longer running
				// Doing it backwards because Disposing fifo can cause DrawEvents
				// on aready disposed StatusBarPanels (on Windows)
				for (int i = tempArray.Length - 1; i >= 0; i--)
				{
					tempArray[i].Dispose();
				}
			}

			if (m_smallImages != null)
				m_smallImages.Dispose();
			if (m_largeImages != null)
				m_largeImages.Dispose();
		}

		/// <summary>
		/// This is the second of two parts in doing a warm boot/shut down.
		/// It is called after calling the base Dispose method in the Dispose system.
		/// </summary>
		private void ShutDownPart2()
		{

			if (m_mainContentPlaceholderPanel != null && m_mainContentPlaceholderPanel.Parent == null)
				m_mainContentPlaceholderPanel.Dispose();

			if (m_propertyTable != null)
			{
				m_propertyTable.Dispose();
			}

			// Get rid of the Mediator last,
			// so anyone else who wants to access it during shutdown can.
			// Review RandyR: Who would want to do that?
			if (m_mediator != null)
			{
				m_mediator.Dispose();
			}
		}

		#endregion

		#region Windows Form Designer generated code
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification="TabStop is not implemented on Mono")]
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(XWindow));
			this.builtInImages = new System.Windows.Forms.ImageList(this.components);
			this.m_mainSplitContainer = new XCore.CollapsingSplitContainer();
			this.m_mainContentPlaceholderPanel = new Panel();
			this.m_mainContentPlaceholderPanel.BackColor = Color.FromKnownColor(KnownColor.Window);
			this.m_secondarySplitContainer = new XCore.CollapsingSplitContainer();
			this.m_secondarySplitContainer.Panel2.BackColor = Color.FromKnownColor(KnownColor.Window);
			this.m_recordBar = new XCore.RecordBar();
			this.m_sideBarPlaceholderPanel = new Panel();
			this.m_widgetUpdateTimer = new System.Windows.Forms.Timer(this.components);
			this.m_mainSplitContainer.SuspendLayout();
			this.m_secondarySplitContainer.SuspendLayout();
			this.SuspendLayout();
			//
			// builtInImages
			//
			this.builtInImages.ImageSize = new System.Drawing.Size(16, 16);
			this.builtInImages.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("builtInImages.ImageStream")));
			this.builtInImages.TransparentColor = System.Drawing.Color.Transparent;
			//
			// m_mainSplitContainer
			//
			this.m_mainSplitContainer.AccessibleName = "xWindow.mainSplitContainer";
			this.m_mainSplitContainer.BackColor = System.Drawing.SystemColors.Control;
			this.m_mainSplitContainer.Dock = DockStyle.Fill;
			this.m_mainSplitContainer.FirstControl = this.m_sideBarPlaceholderPanel;
			this.m_mainSplitContainer.SecondControl = this.m_secondarySplitContainer;
			this.m_mainSplitContainer.Location = new System.Drawing.Point(0, 0);
			this.m_mainSplitContainer.Name = "m_mainSplitContainer";
			this.m_mainSplitContainer.Size = new System.Drawing.Size(873, 569);
			this.m_mainSplitContainer.TabStop = false;
			this.m_mainSplitContainer.TabIndex = 0;
			this.m_mainSplitContainer.FixedPanel = FixedPanel.Panel1;
			this.m_mainSplitContainer.SplitterDistance = 140;
			//
			// m_mainContentPlaceholderPanel
			//
			this.m_mainContentPlaceholderPanel.AccessibleName = "xWindow.contentPanel";
			this.m_mainContentPlaceholderPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_mainContentPlaceholderPanel.Name = "m_contentPanel";
			this.m_mainContentPlaceholderPanel.TabIndex = 1;
			//
			// m_secondarySplitContainer
			//
			this.m_secondarySplitContainer.AccessibleName = "xWindow.m_secondarySplitContainer";
			this.m_secondarySplitContainer.Panel1Collapsed = true;
			this.m_secondarySplitContainer.FirstControl = this.m_recordBar;
			this.m_secondarySplitContainer.SecondControl = this.m_mainContentPlaceholderPanel;
			this.m_secondarySplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_secondarySplitContainer.Location = new System.Drawing.Point(0, 0);
			this.m_secondarySplitContainer.Name = "m_secondarySplitContainer";
			this.m_secondarySplitContainer.Size = new System.Drawing.Size(728, 569);
			this.m_secondarySplitContainer.TabStop = false;
			this.m_secondarySplitContainer.TabIndex = 1;
			this.m_secondarySplitContainer.FixedPanel = FixedPanel.Panel1;
			this.m_secondarySplitContainer.SplitterDistance = 200;
			//
			// m_recordBar
			//
			this.m_recordBar.AccessibleName = "XCore.RecordBar";
			this.m_recordBar.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_recordBar.Name = "m_recordBar";
			this.m_recordBar.TabIndex = 1;
			//
			// m_sideBarPlaceholderPanel
			//
			this.m_sideBarPlaceholderPanel.AccessibleName = "xWindow.sideBarPanel";
			this.m_sideBarPlaceholderPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_sideBarPlaceholderPanel.Name = "m_sideBarPanel";
			this.m_sideBarPlaceholderPanel.TabIndex = 2;
			//
			// m_widgetUpdateTimer
			//
			this.m_widgetUpdateTimer.Interval = 1000;
			this.m_widgetUpdateTimer.Tick += new System.EventHandler(this.WidgetUpdateTimer_Tick);
			//
			// XWindow
			//
			this.AccessibleDescription = "The main window";
			this.AccessibleName = "The Window";
			this.AccessibleRole = System.Windows.Forms.AccessibleRole.Window;
			this.AutoScaleMode = AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(873, 569);
			this.Controls.Add(this.m_mainSplitContainer);
			this.KeyPreview = true;
			this.Name = "XWindow";
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
			this.Text = "XCoreMainWnd";
			this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.XWindow_KeyDown);
			this.Resize += new System.EventHandler(this.XWindow_Resize);
			this.Closing += new System.ComponentModel.CancelEventHandler(this.XWindow_Closing);
			this.Move += new System.EventHandler(this.XWindow_Move);
			this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.XWindow_KeyUp);
			this.Activated += new System.EventHandler(this.XWindow_Activated);
			this.m_mainSplitContainer.ResumeLayout(false);
			this.m_secondarySplitContainer.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		#region Overrides

		protected override void WndProc(ref Message m)
		{
			if (m.Msg == WM_BROADCAST_ITEM_INQUEUE)	// mediator queue message
			{
				m_mediator.ProcessItem();	// let the mediator service an item from the queue

				return;	// no need to pass on to base wndproc
			}

			base.WndProc(ref m);
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (!e.Handled && e.KeyCode == Keys.Tab && (e.Modifiers & Keys.Control) == Keys.Control)
			{
				// switch between panes LT-5431.
				Control focusedControl = MoveToNextPane(!e.Shift);
				e.Handled = focusedControl != null && focusedControl.ContainsFocus;	// Review: conflicts with TabControls (e.g. InterlinMaster).
			}
			// Note: e.Handled needs to be set to true before base.OnKeyDown(e) if we want to avoid further delegation.
			base.OnKeyDown(e);
		}

		protected override void OnLayout(LayoutEventArgs levent)
		{
			if (m_computingMinSize)
				return;

			if (!m_fullyInitialized)
				return;

			base.OnLayout(levent);
		}

		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);

			m_mediator.MainWindow = this;
			// Note: Now the mediator has our handle, so we need to call m_mediator.BroadcastPendingItems(). But
			// WndProc doesn't actually get the messages if we do this inside OnHandleCreated,
			// so for now we are leaving it to subclasses. For example, see FwApp.InitAndShowMainWindow().
		}

		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);

			// Finally, wire up these event handlers.
			// Doing it much earlier gets the poor things to handle events, but with sizes not quite right.
			m_secondarySplitContainer.SplitterMoved += new SplitterEventHandler(OnSecondarySplitterMoved);
			m_mainSplitContainer.SplitterMoved += new SplitterEventHandler(OnMainSplitterMoved);
		}

		protected override void OnLoad(EventArgs e)
		{
			// Fix the magic growing main window for 120dpi font operation by maintaining the same
			// size window as what we had saved last time.
			Size szOld = this.Size;
			base.OnLoad(e);
			if (this.Size != szOld)
				this.Size = szOld;
			SetSizingGripState();
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			SetSizingGripState();
		}

		/// <summary>
		/// Add/Remove sizing grip from the status bar.  See LT-9851. It seems there is a bug in the way that Windows draws the
		/// size grip on the status bar at 120dpi. It overlaps the the adjacent StatusBarPanel and can cut off text, so we
		/// draw it ourself.
		/// </summary>
		private void SetSizingGripState()
		{
			if (m_bar != null)
			{
				if (WindowState != FormWindowState.Maximized)
				{
					if (!m_bar.Panels.Contains(m_sizeGrip))
						m_bar.Panels.Add(m_sizeGrip);
				}
				else
			{
					m_bar.Panels.Remove(m_sizeGrip);
				}
			}
		}

		#endregion Overrides

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the application registry key.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual RegistryKey ApplicationRegistryKey
		{
			get
			{
				throw new NotImplementedException("This method needs to be overridden in subclasses");
			}
		}

		/// <summary>
		/// Call this for the duration of a block of code where we don't want idle events.
		/// (Note that various things outside our control may pump events and cause the
		/// timer that fires the idle events to be triggered when we are not idle, even in the
		/// middle of processing another event.) Call ResumeIdleProcessing when done.
		/// </summary>
		public void SuspendIdleProcessing()
		{
			CheckDisposed();

			m_cSuspendIdle++;
		}

		/// <summary>
		/// See SuspentIdleProcessing.
		/// </summary>
		public void ResumeIdleProcessing()
		{
			CheckDisposed();

			if (m_cSuspendIdle > 0)
				m_cSuspendIdle--;
		}

		/// <summary>
		/// Call this for the duration of a block of code outside of xWindow that might update
		/// the size of the window (OnCreateHandle, for instance) without regard to the Mediator
		/// PropertyTable. Call ResumeWindowSizing when done.
		/// </summary>
		public void SuspendWindowSizePersistence()
		{
			CheckDisposed();

			m_persistWindowSize = false;
		}

		/// <summary>
		/// See SuspentWindowSizing.
		/// </summary>
		public void ResumeWindowSizePersistence()
		{
			CheckDisposed();

			m_persistWindowSize = true;
		}

		/// <summary>
		/// Saves the property table (global) settings
		/// Subclasses can override to save local settings.
		/// </summary>
		public virtual void SaveSettings()
		{
			m_propertyTable.Save("", new string[0]);
		}

		/// <summary>
		/// Start the window almost from scratch.
		/// This is needed to fix the full refresh behavior of wiping out everything in the caches.
		/// </summary>
		protected void WarmBootPart1()
		{
			SaveSettings();

			// Disable the mediator from processing messages.
			if (m_mediator != null)
			{
				m_mediator.MainWindow = null;
				m_mediator.ProcessMessages = false;
				m_mediator.RemoveColleague(this);
			}
			// m_mainSplitContainer is the only control created and added to this.Controls in
			// InitializeComponents().  Get rid of all the other collected controls -- they'll be
			// recreated by LoadUIFromXmlDocument().
			this.SuspendLayout();

			List<Control> toRemove = new List<Control>();
			foreach (Control ctrl in Controls)
			{
				if (ctrl != m_mainSplitContainer)
					toRemove.Add(ctrl);
			}
			foreach (Control ctrl in toRemove)
			{
				Controls.Remove(ctrl);
				ctrl.Dispose();
			}

			// close all dialogs owned by the main window
			foreach (Form f in OwnedForms)
			{
				// Don't close the import wizard just because creating a custom field
				// causes a full refresh.  See LT-10193.
				if (f.Name != "LexImportWizard")
					f.Close();
			}

			// Clear all the controls created in LoadUIFromXmlDocument().
			m_rebarAdapter = null;
			m_sidebarAdapter = null;
			m_menuBarAdapter = null;
			m_adapters.Clear();
			m_sidebar = null;
			m_menusChoiceGroupCollection.Clear();
			m_menusChoiceGroupCollection = null;
			m_sidebarChoiceGroupCollection.Clear();
			m_sidebarChoiceGroupCollection = null;
			m_toolbarsChoiceGroupCollection.Clear();
			m_toolbarsChoiceGroupCollection = null;
			m_statusPanels.Clear();		// Refresh refills the status panels.

			// This is a patch - much like the one below on the mediator where it's checked for null.
			// If we (I) knew why this was getting called 'n' times and the value was null, I'd have fixed
			// it differently.
			if (m_mainContentControl != null)
			{
				m_mainContentControl.Dispose();
				m_mainContentControl = null;
			}
			// Finish destroying the old mediator, and create a new one.
			if (m_mediator != null)
			{
				// First, we need to get rid of any existing ToolStripManager object!  (See LT-6481)
				if (m_propertyTable.PropertyExists("ToolStripManager"))
				{
					m_propertyTable.SetPropertyDispose("ToolStripManager", true);
				}
				m_mediator.Dispose();
			}
			if (m_propertyTable != null)
			{
				m_propertyTable.Dispose();
			}
			// No broadcasting until it has our handle (see OnHandleCreated)
			m_mediator = new Mediator
			{
				SpecificToOneMainWindow = true
			};
			m_propertyTable = new PropertyTable(m_mediator);
			ResumeLayout();
		}

		/// <summary>
		/// Start the window almost from scratch.
		/// This is needed to fix the full refresh behavior of wiping out everything in the caches.
		/// Callers may need to stuff things in the Mediator or its PropertyTable.
		/// They should do that between calling WarmBootPart1 and this method.
		/// </summary>
		protected void WarmBootPart2()
		{
			//No. LoadUIFromXmlDocument(m_windowConfigurationNode.OwnerDocument, m_configurationPath);
			// Rebuild the entire XML doc to support installing/uninstalling extensions.
			LoadUI(m_configurationPath);
			//m_mediator.AllowCommandsToExecute = true;
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="FindForm() returns a reference")]
		public void SynchronizedOnIdleTime()
		{
			CheckDisposed();

			if (m_cSuspendIdle > 0)
				return;

			if (ActiveForm != this && OwnedForms.All(f => ActiveForm != f))
				return;

			UpdateControls();

			// call OnIdle () on any colleagues that implement it.
			m_mediator.SendMessage("Idle", null);
		}

		/// <summary>
		/// Update controls to a suitable state. This is done regularly during idle time while the window is active.
		/// Ideally it should be done once at startup (otherwise the toolbar may not appear, LT-12845) at startup,
		/// even if the window does NOT become active.
		/// </summary>
		public void UpdateControls()
		{
			// The sidebar and toolbar have special needs because things need to become disabled now and then.
			if (m_rebarAdapter != null)
				m_rebarAdapter.OnIdle();
			if (m_sidebarAdapter != null)
				m_sidebarAdapter.OnIdle();
		}

		#region IxCoreColleague implementation

		/// <summary>
		/// Initialize this has an IxCoreColleague
		/// </summary>
		/// <param name="mediator"></param>
		/// <param name="propertyTable"></param>
		/// <param name="configurationParameters"></param>
		public void Init(Mediator mediator, PropertyTable propertyTable, XmlNode configurationParameters)
		{
			throw new ArgumentException("The Constructor creates the Mediator and PropertyTable for this object.");
		}

		/// <summary>
		/// return an array of all of the objects which should
		/// 1) be queried when looking for someone to deliver a message to
		/// 2) be potential recipients of a broadcast
		/// </summary>
		/// <returns></returns>
		public virtual IxCoreColleague[] GetMessageTargets()
		{
			CheckDisposed();

			return new IxCoreColleague[]{this};
		}

		/// <summary>
		/// Should not be called if disposed.
		/// </summary>
		public bool ShouldNotCall
		{
			get { return IsDisposed; }
		}

		/// <summary>
		/// Mediator message handling Priority
		/// </summary>
		public virtual int Priority
		{
			get { return (int)ColleaguePriority.Medium; }
		}

		#endregion IxCoreColleague implementation

		#region XCORE Message Handlers

		/// <summary>
		/// Receives the broadcast message "PropertyChanged"
		/// </summary>
		public virtual void OnPropertyChanged(string name)
		{
			CheckDisposed();

			switch(name)
			{
				//gentle reader, don't trip up over the unfortunate naming here.
				//the property where we look for the XmlNode which defines the control is
				//named currentContentControlParameters. It would perhaps be better named
				//'currentContentControlConfiguration' or something.
				case "currentContentControl":
					using(new WaitCursor(this))
					{
						XmlNode controlNode = m_propertyTable.GetValue<XmlNode>("currentContentControlParameters");
						if (controlNode != null)
						{
							XmlNode dynLoaderNode = controlNode.SelectSingleNode("dynamicloaderinfo");
							if (dynLoaderNode == null)
								throw new ArgumentException("Required 'dynamicloaderinfo' XML node not found, while handling change to 'currentContentControl' property in xWindow.", "name");
							ChangeContentObjectIfPossible(XmlUtils.GetAttributeValue(dynLoaderNode, "assemblyPath"),
								XmlUtils.GetAttributeValue(dynLoaderNode, "class"),
								controlNode);
						}
					}
					break;

				case "DocumentName":
					UpdateCaptionBar();
					break;
				// Obsolete case "ShowSidebarControls": // Fall through.
				case "ShowSidebar": // Fall through.
				// Obsolete case "ShowTreeBar": // Fall through.
				case "ShowRecordList": // Replaces obsolete "ShowTreeBar".
					UpdateSidebarAndRecordBarDisplay(true);
					break;

				// Refresh the Writing System and Styles Combo Boxes
				case "WritingSystemHvo":
				case "BestStyleName":
					SynchronizedOnIdleTime();
					break;

				default:
					if(name.Length > 11 && name.Substring(0, 11) == "StatusPanel")
					{
						string panelName = name.Substring(11);
						if (m_statusPanels.ContainsKey(panelName))
						{
							StatusBarPanel panel = m_statusPanels[panelName];
							panel.Text = m_propertyTable.GetStringProperty(name, "");
						}
					}
					break;
			}
		}

		/// <summary>
		/// Called through FwXWindow.OnMasterRefresh (and perhaps eventually others),
		/// gives the window a chance to save anything in progress before the Refresh
		/// updates the cache.
		///
		/// Possibly this should be allowed to fail and abort the Refresh, if PrepareToGoAway fails?
		/// </summary>
		/// <param name="sender"></param>
		/// <returns></returns>
		public bool OnPrepareToRefresh(object sender)
		{
			CheckDisposed();

			// This can be null when the MasterRefresh is being processed.
			if (MainContentControlAsIxCoreContentControl == null)
				return false;

			MainContentControlAsIxCoreContentControl.PrepareToGoAway();

			return false; // others may want to check also.
		}

		#endregion XCORE Message Handlers

		#region splitters, sidebar, and tree bar stuff

		private void SetSplitContainerDistance(CollapsingSplitContainer splitCont, int pixels)
		{
			if (splitCont.SplitterDistance != pixels)
				splitCont.SplitterDistance = pixels;
		}

		/// <summary>
		/// handle the visibility and sizing of the sidebar and tree bars, along with their splitters.
		/// </summary>
		private void UpdateSidebarAndRecordBarDisplay(bool suspendAndResumeLayout)
		{
			//preserving the tree bar with turned out to be the tricky thing
			//here, we preserving it then setting it at the end of this method.
			//if you see that the tree bar with has negative 3, that is because
			//remember, its size is determined by (sidebar.width - barPanel.width) - width of a splitter
			//if it is invisible, then the sidebar and the bar panel have the same width, so you end up with
			//- the width of a splitter (-3)
			if (suspendAndResumeLayout)
				this.SuspendLayout();

			if (m_propertyTable.GetBoolProperty("ShowSidebar", true))
			{
				// Show side bar.
				if (m_mainSplitContainer.Panel1Collapsed)
					m_mainSplitContainer.Panel1Collapsed = false;
			}
			else
			{
				// Get rid of side bar.
				if (!m_mainSplitContainer.Panel1Collapsed)
					m_mainSplitContainer.Panel1Collapsed = true;
			}

			if (m_propertyTable.GetBoolProperty("ShowRecordList", false))
			{
				// Show Record List.
				if (m_secondarySplitContainer.Panel1Collapsed)
					m_secondarySplitContainer.Panel1Collapsed = false;
			}
			else
			{
				// Get rid of Record List.
				if (!m_secondarySplitContainer.Panel1Collapsed)
					m_secondarySplitContainer.Panel1Collapsed = true;
			}

			if (suspendAndResumeLayout)
				this.ResumeLayout();
		}

		protected void OnTreeBarAfterSelect(object sender, TreeViewEventArgs e)
		{
			m_propertyTable.SetProperty("SelectedTreeBarNode", e.Node, true);
			m_propertyTable.SetPropertyPersistence("SelectedTreeBarNode", false);
		}

		protected void OnListBarSelect( object sender, EventArgs e)
		{
			m_propertyTable.SetProperty("SelectedListBarNode",
				m_recordBar.ListView.SelectedItems.Count == 0 ? null : m_recordBar.ListView.SelectedItems[0],
				true);
			m_propertyTable.SetPropertyPersistence("SelectedListBarNode", false);
		}

		public void ClearRecordBarList()
		{
			CheckDisposed();

			//we really do want both of these handlers disconnected while clearing
			m_recordBar.TreeView.AfterSelect -= OnTreeBarAfterSelect;
			m_recordBar.ListView.SelectedIndexChanged -=OnListBarSelect;
			m_recordBar.Clear();
			m_recordBar.ListView.SelectedIndexChanged += OnListBarSelect;
			m_recordBar.TreeView.AfterSelect += OnTreeBarAfterSelect;
		}

		#endregion

		/// <summary>
		/// Create and install an object to fill the content area of the window, after asking the current
		/// content object if it is willing to go away.
		/// </summary>
		protected void ChangeContentObjectIfPossible(string contentAssemblyPath, string contentClass, XmlNode contentClassNode)
		{
			// This message often gets sent twice with the same values during startup. We save a LOT of time if
			// we don't throw one copy away and make another.
			if (m_mainContentControl != null && contentAssemblyPath == m_lastContentAssemblyPath
				&& contentClass == m_lastContentClass && contentClassNode == m_lastContentClassNode)
			{
				return;
			}

			if (m_mainContentControl != null)
			{
				// First, see if the existing content object is ready to go away.
				if (!MainContentControlAsIxCoreContentControl.PrepareToGoAway())
					return;

				// No broadcast even if it did change.
				m_propertyTable.SetProperty("currentContentControlObject", null, false);
				m_propertyTable.SetPropertyPersistence("currentContentControlObject", false);

				m_mediator.RemoveColleague(MainContentControlAsIxCoreColleague);
				foreach (IxCoreColleague icc in MainContentControlAsIxCoreColleague.GetMessageTargets())
					m_mediator.RemoveColleague(icc);

				// Dispose the current content object.
				//m_mainContentControl.Hide();
				//m_secondarySplitContainer.SecondControl = m_mainContentPlaceholderPanel;
				// Hide the first pane for sure so that MultiPane's internal splitter will be set
				// correctly.  See LT-6515.
				// No broadcast even if it did change.
				m_propertyTable.SetProperty("ShowRecordList", false, false);
				m_secondarySplitContainer.Panel1Collapsed = true;
				m_mainContentControl.Dispose(); // before we create the new one, it inactivates the Clerk, which the new one may want active.
				m_mainContentControl = null;
			}

			m_lastContentAssemblyPath = contentAssemblyPath;
			m_lastContentClass = contentClass;
			m_lastContentClassNode = contentClassNode;

			if (contentAssemblyPath != null)
			{
				// create the new content object
				try
				{
					m_secondarySplitContainer.Panel2.SuspendLayout();
					Control mainControl = (Control)DynamicLoader.CreateObject(contentAssemblyPath, contentClass);
					if (!(mainControl is IxCoreContentControl))
					{
						m_mainSplitContainer.SecondControl = m_mainContentPlaceholderPanel;
						throw new ApplicationException("XCore can only handle main controls which implement IxCoreContentControl. " + contentClass + " does not.");
					}
					mainControl.SuspendLayout();
					m_mainContentControl = mainControl;
					m_mainContentControl.Dock = DockStyle.Fill;
					m_mainContentControl.AccessibleDescription = "XXXXXXXXXXXX";
					m_mainContentControl.AccessibleName = contentClass;
					m_mainContentControl.TabStop = true;
					m_mainContentControl.TabIndex = 1;
					XmlNode parameters = null;
					if (contentClassNode != null)
						parameters = contentClassNode.SelectSingleNode("parameters");
					m_secondarySplitContainer.SetSecondCollapseZone(parameters);
					MainContentControlAsIxCoreColleague.Init(m_mediator, m_propertyTable, parameters);
					// We don't want it or any part of it drawn until we're done laying out.
					// Also, layout tends not to actually happen until we make it visible, which further helps avoid duplication,
					// and makes sure the user doesn't see any intermediate state.
					m_mainContentControl.Visible = false;
					m_secondarySplitContainer.SecondControl = m_mainContentControl;
					mainControl.ResumeLayout(false);
					var mainContentAsPostInit = m_mainContentControl as IPostLayoutInit;

					//this was added because the user may switch to a control through some UI vector that does not
					//first set the appropriate area. Doing this will lead to the appropriate area button being highlighted, and also
					//help other things which depend on the accuracy of this "areaChoice" property.
					m_propertyTable.SetProperty("currentContentControlObject", m_mainContentControl, true);
					m_propertyTable.SetPropertyPersistence("currentContentControlObject", false);
					m_propertyTable.SetProperty("areaChoice", MainContentControlAsIxCoreContentControl.AreaName, true);

					if (contentClassNode != null && contentClassNode.ParentNode != null)
						SetToolDefaultProperties(contentClassNode.ParentNode.SelectSingleNode("defaultProperties"));

					m_mainContentControl.BringToFront();
					m_secondarySplitContainer.Panel2.ResumeLayout();
					if (mainContentAsPostInit != null)
						mainContentAsPostInit.PostLayoutInit();
					m_mainContentControl.Visible = true;
					m_mainContentControl.Select();
				}
				catch (Exception error)
				{
					m_mainContentControl = null;
					m_secondarySplitContainer.SecondControl = m_mainContentPlaceholderPanel;
					m_secondarySplitContainer.Panel2.ResumeLayout();
					string s = "Something went wrong trying to create a " + contentClass + ".";
					ErrorReporter.ReportException(new ApplicationException(s, error),
						ApplicationRegistryKey, m_propertyTable.GetValue<IFeedbackInfoProvider>("FeedbackInfoProvider").SupportEmailAddress);
				}
			}
		}

		private void SetToolDefaultProperties(XmlNode configurationNode)
		{
			m_propertyTable.SetProperty("AllowInsertLinkToFile", true, true);	// default to allowing LinkedFiles links
			m_propertyTable.SetProperty("AllowShowNormalFields", true, true);

			if (configurationNode == null)
				return;

			// Let's get the overridden value from the toolConfiguration xml file ...
			LoadDefaultProperties(configurationNode);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Show the document name and application name in the caption bar.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void UpdateCaptionBar()
		{
			Text = String.Format("{0} - {1}",
				m_propertyTable.GetStringProperty("DocumentName", ""),
				m_propertyTable.GetStringProperty("applicationName", "application name???")); ;
		}

		#region Helper methods

		private int SaveSplitterDistance(CollapsingSplitContainer splitContainer)
		{
			string property = splitContainer.Tag as string;
			int defaultValue = property == "SidebarWidthGlobal" ? 140 : 200;
			int oldValue = m_propertyTable.GetIntProperty(property, defaultValue);
			int newValue = splitContainer.SplitterDistance;
			if (oldValue != newValue)
			{
				m_propertyTable.SetProperty(property, newValue, false);
			}

			return oldValue;
		}


		/// <summary>
		/// given a control configuration node, find the (parent) tool id.
		/// </summary>
		/// <param name="configurationNode"></param>
		/// <returns></returns>
		public static string GetToolIdFromControlConfiguration(XmlNode configurationNode)
		{
			XmlNode parentToolNode = configurationNode.SelectSingleNode(@"ancestor::tool");
			string toolId = XmlUtils.GetManditoryAttributeValue(parentToolNode, "value");
			return toolId;
		}

		#endregion Helper methods

		#region Windows Event handlers

		//review: don't know if this is the best event to start this
		private void XWindow_Activated(object sender, System.EventArgs e)
		{
			// This Idle event turned out to be wait to frequent...we ended up
			// chewing up an awful lot of cycles updating the screen unnecessarily
			//Application.Idle += new EventHandler(this.OnIdleTime);

			m_widgetUpdateTimer.Enabled = true;
		}

		private void XWindow_Resize(object sender, EventArgs e)
		{
			if (!m_persistWindowSize)
				return;

			//don't bother storing the size if we are maximize or minimize.
			//if we did, then when the user exits the application and then runs it again,
			//	then switches to the normal state, we would be switching to a bizarre size.
			if (WindowState == FormWindowState.Normal)
			{
				m_propertyTable.SetProperty("windowSize", Size, true);
			}
			// We do need to store the window state as well:  see LT-6602.
			// No broadcast even if it did change.
			m_propertyTable.SetProperty("windowState", WindowState, false);
		}

		private void XWindow_Move(object sender, EventArgs e)
		{
			//don't bother storing the location if we are maximized or minimized.
			//if we did, then when the user exits the application and then runs it again,
			//	then switches to the normal state, we would be switching to 0,0 or something.
			if (WindowState == FormWindowState.Normal)
			{
				m_propertyTable.SetProperty("windowLocation", Location, true);
			}
		}

		protected virtual void XWindow_Closing(object sender, CancelEventArgs e)
		{
			CheckDisposed();

			m_widgetUpdateTimer.Enabled = false;

			if (m_mediator.SendCancellableMessage("ConsideringClosing", this))
			{
				e.Cancel = true;
				m_widgetUpdateTimer.Enabled = true;
				return;
			}

			m_mediator.ProcessMessages = false;

			// There is no way it can be null, unless the window has been disposed,
			// and that is a bad bug, if it can still get here.
			//if (m_adapters != null)
			//{
			foreach (IUIAdapter adapter in m_adapters)
			{
				adapter.PersistLayout();
			}
			//}

			m_propertyTable.SetProperty("windowState", WindowState, false);
			string id = XmlUtils.GetAttributeValue(m_windowConfigurationNode, "settingsId");
			if (id != null)
			{
				SaveSettings();
			}

			m_secondarySplitContainer.Focus();
		}

		/// <summary>
		/// Note: The CollapsingSplitContainer class has its own event handler,
		/// which will be run, along with this one.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnMainSplitterMoved(object sender, SplitterEventArgs e)
		{
			if (!m_fullyInitialized)
				return;


			// By putting this in the PropertyTable, we persist this value so that
			// it will be used next time they run the application.
			// This is cheap, since it won't broadcast it anywhere.
			SaveSplitterDistance(m_mainSplitContainer);

			// No adjustment to the Panel2 stuff is needed.
			// The Panel2 side resets its min width, so we can never make it too narrow.
			// If it can be made too narrow, that indicates a bug in some other code,
			// so we should still not have to make any adjustment here.
			// If the specs ever call for the record list to be shrunk,
			// as we expand, then all bets are off, and this method will need to do the shrinking.
		}

		/// <summary>
		/// Note: The CollapsingSplitContainer class has its own event handler,
		/// which will be run, along with this one.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnSecondarySplitterMoved(object sender, SplitterEventArgs e)
		{
			if (!m_fullyInitialized)
				return;

			// By putting this in the PropertyTable, we persist this value so that
			// it will be used next time they run the application.
			CollapsingSplitContainer sc = m_secondarySplitContainer;
			SaveSplitterDistance(sc);

			// Re-adjust the main splitter Panel2 min size
			// to include m_secondarySplitContainer.SplitterDistance+SplitterWidth+Panel2 min size.
			int minMainPanel2Width =
				sc.SplitterDistance
				+ sc.SplitterWidth
				+ sc.Panel2MinSize;

			if (m_mainSplitContainer.Panel2MinSize != minMainPanel2Width)
				m_mainSplitContainer.Panel2MinSize = minMainPanel2Width;
		}

		private void XWindow_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			if (!e.Alt && !e.Control && !IsFunctionKey(e.KeyData))
				return;
			// if ((e.KeyData == Keys.Control) || (e.KeyData == Keys.Alt) )//didn't work
			if ((int)e.KeyData == 131089 )//just ctrl
				return;

			//HACK, because without this the CommandBar was not getting keyboard events
			//if((int)e.KeyData == 262162)	//just alt
			{
				if (((IUIMenuAdapter)m_menuBarAdapter).HandleAltKey(e, true))
				{
					e.Handled = true;
					return;
				}
			}

			//todo: we can only handle commands for now (not list items)
			foreach (System.Collections.DictionaryEntry entry in this.m_mediator.CommandSet)
			{
				Command c = (Command)entry.Value;
				if (e.KeyData == c.Shortcut)
				{
					UIItemDisplayProperties display = CommandChoice.QueryDisplayProperties(m_mediator,c,false, "foo");

					if (!display.Enabled)
						continue;	// may have multiple commands (in different areas) assigned same shortcut.

					c.InvokeCommand();
					e.Handled = true;
					e.SuppressKeyPress = true;	// needed to fix LT-6494.
					return;			// Handle only the first valid shortcut.  There should be only one!
				}
			}
		}

		private bool IsFunctionKey(Keys keys)
		{
			switch (keys)
			{
				case Keys.F1:
				case Keys.F2:
				case Keys.F3:
				case Keys.F4:
				case Keys.F5:
				case Keys.F6:
				case Keys.F7:
				case Keys.F8:
				case Keys.F9:
				case Keys.F10:
				case Keys.F11:
				case Keys.F12:
					return true;
			}
			return false;
		}

		private void XWindow_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			if (((IUIMenuAdapter)m_menuBarAdapter).HandleAltKey(e, false))
			{
				e.Handled = true;
				return;
			}
		}

		private void WidgetUpdateTimer_Tick(object sender, System.EventArgs e)
		{
			//this event happens on who knows what thread, so we need to just activate this method on the main thread
			//MethodInfo mi=this.GetType().GetMethod("SynchronizedOnIdleTime",BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance);
			//mi.Invoke(this, new object [] {});

			SynchronizedOnIdleTime();
			// This is done in SynchronizedOnIdleTime now, as I can't see why some controls should do idle processing, and others not do it.
			// m_mediator.SendMessage("Idle", this); //let listeners and other colleagues do something
		}

		#endregion Windows Event handlers
	}

	/// <summary>
	/// This is a size grip that can be added to a status bar. We use this instead of letting
	/// the status bar draw the size grip because the size grip can draw too large at 120dpi
	/// overlapping adjacent panels.
	/// </summary>
	class StatusBarSizeGrip : StatusBarPanel
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="StatusBarSizeGrip"/> class.
		/// </summary>
		/// <param name="bar">The status bar.</param>
		public StatusBarSizeGrip(StatusBar bar)
		{
			BorderStyle = StatusBarPanelBorderStyle.None;
			Style = System.Windows.Forms.StatusBarPanelStyle.OwnerDraw;
			bar.DrawItem += new StatusBarDrawItemEventHandler(bar_DrawItem);
			int width = 16;
			if (Application.RenderWithVisualStyles)
			{
				using (Graphics g = bar.CreateGraphics())
				{
					VisualStyleRenderer renderer = new VisualStyleRenderer(VisualStyleElement.Status.Gripper.Normal);
					Size sz = renderer.GetPartSize(g, ThemeSizeType.True);
					width = sz.Width;
				}
			}
			int widthT = width + SystemInformation.Border3DSize.Width * 2;
			// Can get widthT = 7 (< MinWidth = 10) on Mac Parallels, so we don't want to crash.
			Width = widthT >= MinWidth ? widthT : MinWidth;
		}

		void bar_DrawItem(object sender, StatusBarDrawItemEventArgs sbdevent)
		{
			if (sbdevent.Panel == this)
			{
				if (Application.RenderWithVisualStyles)
				{
					VisualStyleRenderer renderer = new VisualStyleRenderer(VisualStyleElement.Status.Gripper.Normal);
					Size partSz = renderer.GetPartSize(sbdevent.Graphics, ThemeSizeType.True);
					Rectangle rect = new Rectangle(sbdevent.Bounds.X + (sbdevent.Bounds.Width - partSz.Width),
						sbdevent.Bounds.Y + (sbdevent.Bounds.Height - partSz.Height), partSz.Width, partSz.Height);
					renderer.DrawBackground(sbdevent.Graphics, rect);
				}
				else
				{
					Rectangle rect = new Rectangle(sbdevent.Bounds.X + (sbdevent.Bounds.Width - 16),
						sbdevent.Bounds.Y + (sbdevent.Bounds.Height - 16), 16, 16);
					ControlPaint.DrawSizeGrip(sbdevent.Graphics, sbdevent.BackColor, rect);
				}
			}
		}
	}

	/// <summary>
	/// Interface main content controls may implement if they want to do more initialization after
	/// the main content control is laid out (e.g., when their true size is known).
	/// </summary>
	public interface IPostLayoutInit
	{
		void PostLayoutInit();
	}
}

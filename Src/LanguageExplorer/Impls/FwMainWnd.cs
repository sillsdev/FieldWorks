//#define RANDYTODOMERGEFILES
// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using IWshRuntimeLibrary;
using LanguageExplorer.Archiving;
using LanguageExplorer.Controls;
using Palaso.Code;
using SIL.CoreImpl;
using LanguageExplorer.Controls.SilSidePane;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.FieldWorks.Resources;
using SIL.Utils;
using SIL.Utils.FileDialog;
#if RANDYTODOMERGEFILES
using System.Xml;
using System.Collections.Specialized;
using File = System.IO.File;
#endif

namespace LanguageExplorer.Impls
{
	/// <summary>
	/// Main window class for FW/FLEx.
	/// </summary>
#if RANDYTODO
	/// <remarks>
	/// Main CollapsingSplitContainer control for XWindow.
	/// It holds the Sidebar (m_sidebar) in its Panel1 (left side).
	/// It holds m_mainContentControl in Panel2, when m_recordBar is not showing.
	/// It holds another CollapsingSplitContainer (m_secondarySplitContainer) in Panel2,
	/// when the record list and the main control are both showing.
	///
	/// Controlling properties are:
	/// This is the splitter distance for the sidebar/secondary splitter pair of controls.
	/// property name="SidebarWidthGlobal" intValue="140" persist="true"
	/// This property is driven by the needs of the current main control, not the user.
	/// property name="ShowRecordList" bool="false" persist="true"
	/// This is the splitter distance for the record list/main content pair of controls.
	/// property name="RecordListWidthGlobal" intValue="200" persist="true"
	///
	/// Event handlers expected to be managed by areas/tools that are ostsensibly global:
	///		1. printToolStripMenuItem : the active tool can enable this and add an event handler, if needed.
	///		2. exportToolStripMenuItem : the active tool can enable this and add an event handler, if needed.
	/// </remarks>
#endif
	internal sealed partial class FwMainWnd : Form, IFwMainWnd
	{
		// Used to count the number of times we've been asked to suspend Idle processing.
		private int _countSuspendIdleProcessing = 0;
		private SidePane _sidePane;
		/// <summary>
		///  Web browser to use in Linux
		/// </summary>
		private string _webBrowserProgramLinux = "firefox";
		private IAreaRepository _areaRepository;
		private IToolRepository _toolRepository;
		private ActiveViewHelper _viewHelper;
		private IArea _currentArea;
		private ITool _currentTool;
		private FwStyleSheet _stylesheet;
		private IPublisher _publisher;
		private ISubscriber _subscriber;
		private IFlexApp _flexApp;

		/// <summary>
		/// Create new instance of window.
		/// </summary>
		public FwMainWnd()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Create new instance of window.
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "IPropertyTable is disposed when closed.")]
		public FwMainWnd(IFlexApp flexApp, FwMainWnd wndCopyFrom, FwLinkArgs linkArgs)
			: this()
		{
			Guard.AssertThat(wndCopyFrom == null, "Support for the 'wndCopyFrom' is not yet implemented.");
			Guard.AssertThat(linkArgs == null, "Support for the 'linkArgs' is not yet implemented.");

			_flexApp = flexApp;

			AddCustomStatusBarPanels();

			_sendReceiveToolStripMenuItem.Enabled = FLExBridgeHelper.IsFlexBridgeInstalled();
			projectLocationsToolStripMenuItem.Enabled = FwRegistryHelper.FieldWorksRegistryKeyLocalMachine.CanWriteKey();
			archiveWithRAMPSILToolStripMenuItem.Enabled = ReapRamp.Installed;

			_viewHelper = new ActiveViewHelper(this);

			SetupPropertyTable();

			_stylesheet = new FwStyleSheet();
			_stylesheet.Init(Cache, Cache.LanguageProject.Hvo, LangProjectTags.kflidStyles);
			PropertyTable.SetProperty("FwStyleSheet", _stylesheet, false, true);

			_toolRepository = new ToolRepository();
			_areaRepository = new AreaRepository(_toolRepository);
			_areaRepository.InitializeFlexComponent(PropertyTable, Publisher, Subscriber);

			SetupOutlookBar();

			SetWindowTitle();
#if RANDYTODOMERGEFILES
			// Remove this when I'm done with project.
			// Load xml config files and save merged document.
			var configFilePath = Path.Combine(FwDirectoryFinder.CodeDirectory, @"Language Explorer", @"Configuration", @"Main.xml");
			XmlDocument mergedConfigDoc = new XmlDocument();
			mergedConfigDoc.Load(configFilePath);
			// Process <include> elements
			// Do includes relative to the dir of our config file
			var resolver = new SimpleResolver
			{
				BaseDirectory = Path.GetDirectoryName(configFilePath)
			};
			var includer = new XmlIncluder(resolver)
			{
				SkipMissingFiles = false
			};
			includer.ProcessDom(configFilePath, mergedConfigDoc);
			mergedConfigDoc.Save(@"C:\Users\Randy\Desktop\DevWork\NewDevWork\05_Remove use of main xml config files\Configuration\xWindowFullConfig.xml");
#endif
		}
#if RANDYTODOMERGEFILES
		/// <summary>
		/// Summary description for XmlIncluder.
		/// </summary>
		private class XmlIncluder
		{
			private readonly IResolvePath m_resolver;

			/// <summary></summary>
			/// <param name="resolver">An object which can convert directory
			///		references into actual physical directory paths.</param>
			internal XmlIncluder(IResolvePath resolver)
			{
				m_resolver = resolver;
			}

			/// <summary>
			/// True to allow missing include files to be ignored. This is useful with partial installations,
			/// for example, TE needs some of the FLEx config files to support change multiple spelling Dialog,
			/// but a lot of stuff can just be skipped (and therefore need not be installed).
			/// (Pathologically, a needed file may be missing and still ignored; can't help this for now.)
			/// </summary>
			internal bool SkipMissingFiles { get; set; }

			/// <summary>
			/// replace every "include" node in the document with the nodes that it references
			/// </summary>
			internal void ProcessDom(string parentPath, XmlDocument dom)
			{
				var cachedDoms = new Dictionary<string, XmlDocument>
				{
					{parentPath, dom}
				};
				ProcessDom(cachedDoms, null, dom);
				cachedDoms.Clear();
			}

			/// <summary>
			/// replace every "include" node in the document with the nodes that it references
			/// </summary>
			[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
				Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
			private void ProcessDom(Dictionary<string, XmlDocument> cachedDoms, string parentPath, XmlDocument dom)
			{
				XmlNode nodeForError = null;
				string baseFile = "";
				XmlNode baseNode = dom.SelectSingleNode("//includeBase");
				if (baseNode != null)
				{
					baseFile = XmlUtils.GetManditoryAttributeValue(baseNode, "path");
					//now that we have read it, remove it, so that it does not violate the schema of
					//the output file.
					baseNode.ParentNode.RemoveChild(baseNode);
				}

				try
				{
#if !__MonoCS__
					foreach (XmlNode includeNode in dom.SelectNodes("//include"))
					{
#else
				// TODO-Linux: work around for mono bug https://bugzilla.novell.com/show_bug.cgi?id=495693
				XmlNodeList includeList = dom.SelectNodes("//include");
				for(int j = includeList.Count - 1; j >= 0; --j)
				{
					XmlNode includeNode = includeList[j];
					if (includeNode == null)
						continue;
#endif
						nodeForError = includeNode;
						ReplaceNode(cachedDoms, parentPath, includeNode, baseFile);
					}
				}
				catch (Exception error)
				{
					throw new ApplicationException("Error while processing <include> element:" + nodeForError.OuterXml, error);
				}
			}

			/// <summary>
			/// </summary>
			/// <param name="includeNode">include" node, possibly containing "overrides" nodes</param>
			/// <returns>true if we processed an "overrides" node.</returns>
			[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
				Justification = "See TODO-Linux comment")]
			private static void HandleIncludeOverrides(XmlNode includeNode)
			{
				XmlNode parentNode = includeNode.ParentNode;
				XmlNode overridesNode = null;
				// find any "overrides" node
				foreach (XmlNode childNode in includeNode.ChildNodes)
				{
					// first skip over any XmlComment nodes.
					// TODO-Linux: System.Boolean System.Type::op_Equality(System.Type,System.Type)
					// is marked with [MonoTODO] and might not work as expected in 4.0.
					if (childNode.GetType() == typeof(XmlComment))
						continue;
					if (childNode.Name == "overrides")
						overridesNode = childNode;
				}
				if (overridesNode == null)
					return;
				// this is a group of overrides, so alter matched nodes accordingly.
				// treat the first three element parts (element and first attribute) as a node query key,
				// and subsequent attributes as subsitutions.
				foreach (XmlNode overrideNode in overridesNode.ChildNodes)
				{
					// TODO-Linux: System.Boolean System.Type::op_Equality(System.Type,System.Type)
					// is marked with [MonoTODO] and might not work as expected in 4.0.
					if (overrideNode.GetType() == typeof(XmlComment))
						continue;
					string elementKey = overrideNode.Name;
					string firstAttributeKey = overrideNode.Attributes[0].Name;
					string firstAttributeValue = overrideNode.Attributes[0].Value;
					string xPathToModifyElement = String.Format(".//{0}[@{1}='{2}']", elementKey, firstAttributeKey, firstAttributeValue);
					XmlNode elementToModify = parentNode.SelectSingleNode(xPathToModifyElement);
					if (elementToModify != null && elementToModify != overrideNode)
					{
						if (overrideNode.ChildNodes.Count > 0)
						{
							// replace the elementToModify with this overrideNode.
							XmlNode parentToModify = elementToModify.ParentNode;
							parentToModify.ReplaceChild(overrideNode.Clone(), elementToModify);
						}
						else
						{
							// just modify existing attributes or add new ones.
							foreach (XmlAttribute xaOverride in overrideNode.Attributes)
							{
								// the keyAttribute will be identical, so it won't change.
								XmlAttribute xaToModify = elementToModify.Attributes[xaOverride.Name];
								// if the attribute exists on the node we're modifying, alter it
								// otherwise add the new attribute.
								if (xaToModify != null)
									xaToModify.Value = xaOverride.Value;
								else
									elementToModify.Attributes.Append(xaOverride.Clone() as XmlAttribute);
							}
						}
					}
				}
			}

			/// <summary>
			/// replace the node with the node or nodes that it refers to
			/// </summary>
			private void ReplaceNode(Dictionary<string, XmlDocument> cachedDoms, string parentPath, XmlNode includeNode, string defaultPath)
			{
				string path;
				if (!string.IsNullOrEmpty(defaultPath))
					path = XmlUtils.GetOptionalAttributeValue(includeNode, "path", defaultPath);
				else
				{
					path = XmlUtils.GetOptionalAttributeValue(includeNode, "path");
					if (path == null || path.Trim().Length == 0)
						throw new ApplicationException(
							"The path attribute was missing and no default path was specified. " + Environment.NewLine
							+ includeNode.OuterXml);
				}
				XmlNode parentNode = includeNode.ParentNode;
				try
				{
					/* To support extensions, we need to see if 'path' starts with 'Extensions/* /'. (without the extra space following the '*'.)
					* If it does, then we will have to get any folders (the '*' wildcard)
					* and see if any of them have the specified file (at end of 'path'.
					*/
					StringCollection paths = new StringCollection();
					// The extension XML files should be stored in the data area, not in the code area.
					// This reduces the need for users to have administrative privileges.
					bool fExtension = false;
					string extensionBaseDir = null;
					if (path.StartsWith("Extensions") || path.StartsWith("extensions"))
					{
						// Extension <include> element,
						// which may have zero or more actual extensions.
						string extensionFileName = path.Substring(path.LastIndexOf("/") + 1);
						string pluginBaseDir = (parentPath == null) ? m_resolver.BaseDirectory : parentPath;
						extensionBaseDir = pluginBaseDir;
						string sBaseCode = FwDirectoryFinder.CodeDirectory;
						string sBaseData = FwDirectoryFinder.DataDirectory;
						if (extensionBaseDir.StartsWith(sBaseCode) && sBaseCode != sBaseData)
							extensionBaseDir = extensionBaseDir.Replace(sBaseCode, sBaseData);
						// JohnT: allow the Extensions directory not even to exist. Just means no extentions, as if empty.
						if (!Directory.Exists(extensionBaseDir + "/Extensions"))
							return;
						foreach (string extensionDir in Directory.GetDirectories(extensionBaseDir + "/Extensions"))
						{
							string extensionPathname = Path.Combine(extensionDir, extensionFileName);
							// Add to 'paths' collection, but only from 'Extensions' on.
							if (File.Exists(extensionPathname))
								paths.Add(extensionPathname.Substring(extensionPathname.IndexOf("Extensions")));
						}
						// Check for newer versions of the extension files in the
						// "Available Plugins" directory.  See LT-8051.
						UpdateExtensionFilesIfNeeded(paths, pluginBaseDir, extensionBaseDir);
						if (paths.Count == 0)
							return;
						fExtension = true;
					}
					else
					{
						// Standard, non-extension, <include> element.
						paths.Add(path);
					}

					/* Any fragments (extensions or standard) will be added before the <include>
					 * element. Aftwerwards, the <include> element will be removed.
					 */
					string query = XmlUtils.GetManditoryAttributeValue(includeNode, "query");
					foreach (string innerPath in paths)
					{
						XmlDocumentFragment fragment;
						if (innerPath == "$this")
						{
							fragment = CreateFragmentWithTargetNodes(query, includeNode.OwnerDocument);
						}
						else
						{
							fragment = GetTargetNodes(cachedDoms,
								fExtension ? extensionBaseDir : parentPath, innerPath, query);
						}
						if (fragment != null)
						{
							XmlNode node = includeNode.OwnerDocument.ImportNode(fragment, true);
							// Since we can't tell the index of includeNode,
							// always add the fluffed-up node before the include node to keep it/them in the original order.
							parentNode.InsertBefore(node, includeNode);
						}
					}
					// Handle any overrides.
					HandleIncludeOverrides(includeNode);
				}
				catch (Exception e)
				{
					// TODO-Linux: if you delete this exception block !check! flex still runs on linux.
					Console.WriteLine("Debug ReplaceNode error: {0}", e);
				}
				finally
				{
					// Don't want the original <include> element any more, no matter what.
					parentNode.RemoveChild(includeNode);
				}
			}

			/// <summary>
			/// Check for changed (or missing) files in the Extensions subdirectory (as
			/// compared to the corresponding "Available Plugins" subdirectory).
			/// </summary>
			private static void UpdateExtensionFilesIfNeeded(StringCollection paths, string pluginBaseDir,
				string extensionBaseDir)
			{
				if (paths.Count == 0)
					return;
				List<string> obsoletePaths = new List<string>();
				foreach (string extensionPath in paths)
				{
					string pluginPathname = Path.Combine(pluginBaseDir, extensionPath);
					pluginPathname = pluginPathname.Replace("Extensions", "Available Plugins");
					if (File.Exists(pluginPathname))
					{
						string extensionPathname = Path.Combine(extensionBaseDir, extensionPath);
						if (!FileUtils.AreFilesIdentical(pluginPathname, extensionPathname))
						{
							string extensionDir = Path.GetDirectoryName(extensionPathname);
							Directory.Delete(extensionDir, true);
							Directory.CreateDirectory(extensionDir);
							File.Copy(pluginPathname, extensionPathname);
							File.SetAttributes(extensionPathname, FileAttributes.Normal);
							// plug-ins usually have localization strings-XX.xml files.
							foreach (string pluginFile in Directory.GetFiles(Path.GetDirectoryName(pluginPathname), "strings-*.xml"))
							{
								string extensionFile = Path.Combine(extensionDir, Path.GetFileName(pluginFile));
								File.Copy(pluginFile, extensionFile);
								File.SetAttributes(extensionFile, FileAttributes.Normal);
							}
						}
					}
					else
					{
						obsoletePaths.Add(extensionPath);
					}
				}
				foreach (string badPath in obsoletePaths)
					paths.Remove(badPath);
			}

			/// <summary>
			/// get a group of nodes specified by and XPATH query and a file path
			/// </summary>
			/// <remarks> this is the "inner loop" where recursion happens so that files can include other files.</remarks>
			/// <returns></returns>
			private XmlDocumentFragment GetTargetNodes(Dictionary<string, XmlDocument> cachedDoms, string parentPath, string path, string query)
			{
				path = (parentPath == null) ? m_resolver.Resolve(path, SkipMissingFiles)
					: m_resolver.Resolve(parentPath, path, SkipMissingFiles);
				if (path == null)
					return null; // Only possible if m_fSkipMissingFiles is true.
				//path = m_resolver.Resolve(parentPath,path);
				XmlDocument document = null;
				if (!cachedDoms.ContainsKey(path))
				{
					if (SkipMissingFiles && !System.IO.File.Exists(path))
						return null;
					document = new XmlDocument();
					document.Load(path);
					cachedDoms.Add(path, document);
				}
				else
					document = cachedDoms[path];

				//enhance:protect against infinite recursion somehow
				//recurse so that that file itself can have <include/>s.
				ProcessDom(cachedDoms, System.IO.Path.GetDirectoryName(path), document);

				return CreateFragmentWithTargetNodes(query, document);
			}

			[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
				Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
			private static XmlDocumentFragment CreateFragmentWithTargetNodes(string query, XmlDocument document)
			{
				//find the nodes specified in the XML query
				XmlNodeList list = document.SelectNodes(query);
				XmlDocumentFragment fragment = document.CreateDocumentFragment();
				foreach (XmlNode node in list)
				{
					// We must clone the node, otherwise, AppendChild merely MOVES it,
					// modifying the document we have cached, and causing a repeat query for
					// the same element (or any of its children) to fail.
					fragment.AppendChild(node.Clone());
				}
				return fragment;
			}
		}
#endif

		private void SetupOutlookBar()
		{
			mainContainer.SuspendLayout();
			_sidePane = new SidePane(_leftPanel, SidePaneItemAreaStyle.List)
			{
				Dock = DockStyle.Fill,
				TabStop = true,
				TabIndex = 0
			};

			mainContainer.Tag = "SidebarWidthGlobal";
			mainContainer.Panel1MinSize = CollapsingSplitContainer.kCollapsedSize;
			mainContainer.Panel1Collapsed = false;
			mainContainer.Panel2Collapsed = false;
			var sd = PropertyTable.GetValue("SidebarWidthGlobal", 140);
			if (!mainContainer.Panel1Collapsed)
			{
				SetSplitContainerDistance(mainContainer, sd);
			}
			mainContainer.FirstLabel = PropertyTable.GetValue<string>("SidebarLabel");
			mainContainer.SecondLabel = PropertyTable.GetValue<string>("AllButSidebarLabel");
			// Add areas and tools to "_sidePane";
			foreach (var area in _areaRepository.AllAreasInOrder())
			{
				var tab = new Tab(StringTable.Table.LocalizeLiteralValue(area.UiName))
				{
					Icon = area.Icon,
					Tag = area,
					Name = area.MachineName
				};

				_sidePane.AddTab(tab);

				// Add tools for area.
				foreach (var tool in area.AllToolsInOrder)
				{
					var item = new Item(StringTable.Table.LocalizeLiteralValue(tool.UiName))
					{
						Icon = tool.Icon,
						Tag = tool,
						Name = tool.MachineName
					};
					_sidePane.AddItem(tab, item);
				}
			}

			// TODO: If no tool has been persisted, or persisted tool is not in persisted area, pick the default for persisted area.
			mainContainer.ResumeLayout(false);
		}

		void Tool_Clicked(Item itemClicked)
		{
			var clickedTool = (ITool) itemClicked.Tag;
			if (_currentTool == clickedTool)
			{
				return;  // Nothing to do.
			}
			if (_currentTool != null)
			{
				_currentTool.Deactivate(mainContainer, _menuStrip, toolStripContainer, _statusbar);
			}
			_currentTool = clickedTool;
			PropertyTable.SetProperty("ToolForAreaNamed_" + _currentArea.MachineName, _currentTool.MachineName, SettingsGroup.LocalSettings, false, false);
			_currentTool.Activate(mainContainer, _menuStrip, toolStripContainer, _statusbar);
		}

		void Area_Clicked(Tab tabClicked)
		{
			var clickedArea = (IArea)tabClicked.Tag;
			if (_currentArea == clickedArea)
			{
				return; // Nothing to do.
			}
			if (_currentArea != null)
			{
				_currentArea.Deactivate(mainContainer, _menuStrip, toolStripContainer, _statusbar);
			}
			_currentArea = clickedArea;
			PropertyTable.SetProperty("currentArea", _currentArea, SettingsGroup.LocalSettings, false, false);
			_currentArea.Activate(mainContainer, _menuStrip, toolStripContainer, _statusbar);
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "IPropertyTable is a reference")]
		private void SetupPropertyTable()
		{
			PubSubSystemFactory.CreatePubSubSystem(out _publisher, out _subscriber);

			PropertyTable = PropertyTableFactory.CreatePropertyTable(_publisher);
			PropertyTable.UserSettingDirectory = FdoFileHelper.GetConfigSettingsDir(Cache.ProjectId.ProjectFolder);
			PropertyTable.LocalSettingsId = "local";

			if (!Directory.Exists(PropertyTable.UserSettingDirectory))
			{
				Directory.CreateDirectory(PropertyTable.UserSettingDirectory);
			}
			PropertyTable.RestoreFromFile(PropertyTable.GlobalSettingsId);
			PropertyTable.RestoreFromFile(PropertyTable.LocalSettingsId);

			PropertyTable.SetProperty("App", _flexApp, SettingsGroup.BestSettings, false, false);
			PropertyTable.SetProperty("cache", Cache, SettingsGroup.BestSettings, false, false);
			PropertyTable.SetProperty("HelpTopicProvider", _flexApp, false, false);

		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "StatusBarTextBox and StatusBarProgressPanel are references")]
		private void AddCustomStatusBarPanels()
		{
			// Insert first, so it ends up last in the three that are inserted.
			_statusbar.Panels.Insert(3, new StatusBarTextBox(_statusbar));
			_statusbar.Panels[3].Name = @"statusBarPanelFilter";
			_statusbar.Panels[3].Text = @"Filter";
			_statusbar.Panels[3].MinWidth = 40;

			// Insert second, so it ends up in the middle of the three that are inserted.
			_statusbar.Panels.Insert(3, new StatusBarTextBox(_statusbar));
			_statusbar.Panels[3].Name = @"statusBarPanelSort";
			_statusbar.Panels[3].Text = @"Sort";
			_statusbar.Panels[3].MinWidth = 40;

			// Insert last, so it ends up first in the three that are inserted.
			_statusbar.Panels.Insert(3, new StatusBarProgressPanel(_statusbar));
			_statusbar.Panels[3].Name = @"statusBarPanelProgressBar";
			_statusbar.Panels[3].Text = @"ProgressBar";
			_statusbar.Panels[3].MinWidth = 150;
		}

		private static void SetSplitContainerDistance(SplitContainer splitCont, int pixels)
		{
			if (splitCont.SplitterDistance != pixels)
			{
				splitCont.SplitterDistance = pixels;
			}
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		public void SaveSettings()
		{
			// Have current IArea put any needed properties into the table.
#if RANDYTODO
			// Note: This covers what was done using: GlobalSettingServices.SaveSettings(Cache.ServiceLocator, m_propertyTable);
			// RR TODO: Delete GlobalSettingServices.SaveSettings(Cache.ServiceLocator, m_propertyTable);
#endif
			_currentArea.EnsurePropertiesAreCurrent();
			// first save global settings, ignoring database specific ones.
			PropertyTable.SaveGlobalSettings();
			// now save database specific settings.
			PropertyTable.SaveLocalSettings();
		}

		#region Implementation of IPropertyTableProvider

		/// <summary>
		/// Placement in the IPropertyTableProvider interface lets FwApp call PropertyTable.DoStuff.
		/// </summary>
		public IPropertyTable PropertyTable { get; private set; }

		#endregion

		#region Implementation of IFwMainWnd

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the active view of the window
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IRootSite ActiveView { get; private set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the focused control of the window
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Control FocusedControl
		{
			get
			{
				return FromHandle(Win32.GetFocus());
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the data object cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FdoCache Cache
		{
			get { return _flexApp.Cache; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create the client windows and add correspnding stuff to the sidebar, View menu,  etc.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void InitAndShowClient()
		{
			CheckDisposed();

			Show();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a Rectangle representing the position and size of the window in its
		/// normal (non-minimized, non-maximized) state.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Rectangle NormalStateDesktopBounds { get; private set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called just before a window syncronizes it's views with DB changes (e.g. when an
		/// undo or redo command is issued).
		/// </summary>
		/// <param name="sync">syncronization message</param>
		/// ------------------------------------------------------------------------------------
		public void PreSynchronize(SyncMsg sync)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when a window syncronizes it's views with DB changes (e.g. when an undo or
		/// redo command is issued).
		/// </summary>
		/// <param name="sync">syncronization message</param>
		/// <returns>true if successful; false results in RefreshAllWindows.</returns>
		/// ------------------------------------------------------------------------------------
		public bool Synchronize(SyncMsg sync)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when a window is finished being created and completely initialized.
		/// </summary>
		/// <returns>True if successful; false otherwise.  False should keep the main window
		/// from being shown/initialized (maybe even close the window if false is returned)
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public bool OnFinishedInit()
		{
			CheckDisposed();

#if RANDYTODO
			if (m_startupLink != null)
			{
				var commands = new List<string>
						{
							"AboutToFollowLink",
							"FollowLink"
						};
				var parms = new List<object>
						{
							null,
							m_startupLink
						};
				Publisher.Publish(commands, parms);
			}
			UpdateControls();
#endif
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the Find menu command.
		/// </summary>
		/// <param name="args">Arguments</param>
		/// <returns><c>true</c> if message handled, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		public bool OnEditFind(object args)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Prepare to refresh the main window and its IAreas and ITools.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void PrepareToRefresh()
		{
#if RANDYTODO
			// TODO (RandyR): Remove all of these comments, when XWorksViewBase or InterlinMaster code is put back in service.
			// the original code in otherMainWindow.PrepareToRefresh() did this: m_mediator.SendMessageToAllNow("PrepareToRefresh", null);
			// There are/were three impls of "OnPrepareToRefresh": XWindow, XWorksViewBase, and InterlinMaster
			// The IArea & ITool interfaces have "PrepareToRefresh()" methods now.
			// TODO: When the relevant IArea and/or ITool impls are developed that use either
			// TODO: XWorksViewBase or InterlinMaster code, then those area/tool impls will need to call the
			// TODO: "OnPrepareToRefresh" (renamed to simply ""PrepareToRefresh"") methods on those classes.
			// TODO: The 'XWindow class will need nothing done to it, since it is just going to be deleted.
#endif
			_currentArea.PrepareToRefresh();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finish refreshing the main window and its IAreas and ITools.
		/// </summary>
		/// <remarks>
		/// This should call Refresh on real window implementations,
		/// after everything else is done.</remarks>
		/// ------------------------------------------------------------------------------------
		public void FinishRefresh()
		{
			_currentArea.FinishRefresh();
			Refresh();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Refreshes all the views in this window and in all others in the app.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void RefreshAllViews()
		{
			// Susanna asked that refresh affect only the currently active project, which is
			// what the string and List variables below attempt to handle.  See LT-6444.
			var activeWnd = ActiveForm as IFwMainWnd;

			var allMainWindowsExceptActiveWindow = new List<IFwMainWnd>();
			foreach (var otherMainWindow in _flexApp.MainWindows.Where(mw => mw != activeWnd))
			{
				otherMainWindow.PrepareToRefresh();
				allMainWindowsExceptActiveWindow.Add(otherMainWindow);
			}

			// Now that all IFwMainWnds except currently active one have done basic refresh preparation,
			// have them all finish refreshing.
			foreach (var otherMainWindow in allMainWindowsExceptActiveWindow)
			{
				otherMainWindow.FinishRefresh();
			}

			// LT-3963: active IFwMainWnd changes as a result of a refresh.
			// Make sure focus doesn't switch to another FLEx application / window also
			// make sure the application focus isn't lost all together.
			// ALSO, after doing a refresh with just a single application / window,
			// the application would loose focus and you'd have to click into it to
			// get that back, this will reset that too.
			if (activeWnd != null)
			{
				// Refresh it last, so its saved settings get restored.
				activeWnd.FinishRefresh();
				var activeForm = activeWnd as Form;
				if (activeForm != null)
				{
					activeForm.Activate();
				}
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

			_countSuspendIdleProcessing++;
		}

		/// <summary>
		/// See SuspendIdleProcessing.
		/// </summary>
		public void ResumeIdleProcessing()
		{
			CheckDisposed();

			if (_countSuspendIdleProcessing > 0)
			{
				_countSuspendIdleProcessing--;
			}
		}

		#endregion

		#region Implementation of IPublisherProvider

		/// <summary>
		/// Get the IPublisher.
		/// </summary>
		public IPublisher Publisher
		{
			get { return _publisher; }
		}

		#endregion

		#region Implementation of ISubscriberProvider

		/// <summary>
		/// Get the ISubscriber.
		/// </summary>
		public ISubscriber Subscriber
		{
			get { return _subscriber; }
		}

		#endregion

		#region Implementation of IFWDisposable

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		[SuppressMessage("Gendarme.Rules.Design", "UseCorrectDisposeSignaturesRule",
			Justification = "Has to be protected in sealed class, since the superclass has it be protected.")]
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");

			if (IsDisposed)
			{
				return;
			}

			if (disposing)
			{
				if (components != null)
				{
					components.Dispose();
				}

				// TODO: Is this comment still relevant?
				// TODO: Seems like FLEx worked well with it in this place (in the original window) for a long time.
				// The removing of the window needs to happen later; after this main window is
				// already disposed of. This is needed for side-effects that require a running
				// message loop.
				_flexApp.FwManager.ExecuteAsync(_flexApp.RemoveWindow, this);

				if (PropertyTable != null)
				{
					PropertyTable.Dispose();
				}

				if (_sidePane != null)
				{
					_sidePane.Dispose();
				}
				if (_viewHelper != null)
				{
					_viewHelper.Dispose();
				}
			}

			PropertyTable = null;
			_sidePane = null;
			_viewHelper = null;
			_currentArea = null;
			_stylesheet = null;
			_publisher = null;
			_subscriber = null;
			_areaRepository = null;

			base.Dispose(disposing);
		}

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~FwMainWnd()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// This method throws an ObjectDisposedException if IsDisposed returns
		/// true.  This is the case where a method or property in an object is being
		/// used but the object itself is no longer valid.
		///
		/// This method should be added to all public properties and methods of this
		/// object and all other objects derived from it (extensive).
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(string.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		#endregion


		private void File_CloseWindow(object sender, EventArgs e)
		{
			Close();
		}

		private void Help_LanguageExplorer(object sender, EventArgs e)
		{
			var helpFile = _flexApp.HelpFile;
			try
			{
				// When the help window is closed it will return focus to the window that opened it (see MSDN
				// documentation for HtmlHelp()). We don't want to use the main window as the parent, because if
				// a modal dialog is visible, it will still return focus to the main window, allowing the main window
				// to perform some behaviors (such as refresh by pressing F5) while the modal dialog is visible,
				// which can be bad. So, we just create a dummy control and pass that in as the parent.
				Help.ShowHelp(new Control(), helpFile);
			}
			catch (Exception)
			{
				MessageBox.Show(this, string.Format(FrameworkStrings.ksCannotLaunchX, helpFile),
					FrameworkStrings.ksError);
			}
		}

		private void Help_Training(object sender, EventArgs e)
		{
			using (var process = new Process())
			{
				process.StartInfo.UseShellExecute = true;
				process.StartInfo.FileName = "http://wiki.lingtransoft.info/doku.php?id=tutorials:student_manual";
				process.Start();
				process.Close();
			}
		}

		private void Help_DemoMovies(object sender, EventArgs e)
		{
			try
			{
				var pathMovies = string.Format(FwDirectoryFinder.CodeDirectory +
					"{0}Language Explorer{0}Movies{0}Demo Movies.html",
					Path.DirectorySeparatorChar);

				OpenDocument<Win32Exception>(pathMovies, win32err =>
				{
					if (win32err.NativeErrorCode == 1155)
					{
						// The user has the movie files, but does not have a file association for .html files.
						// Try to launch Internet Explorer directly:
						using (Process.Start("IExplore.exe", pathMovies))
						{
						}
					}
					else
					{
						// User probably does not have movies. Try to launch the "no movies" web page:
						var pathNoMovies = String.Format(FwDirectoryFinder.CodeDirectory +
							"{0}Language Explorer{0}Movies{0}notfound.html",
							Path.DirectorySeparatorChar);

						OpenDocument<Win32Exception>(pathNoMovies, win32err2 =>
						{
							if (win32err2.NativeErrorCode == 1155)
							{
								// The user does not have a file association for .html files.
								// Try to launch Internet Explorer directly:
								using (Process.Start("IExplore.exe", pathNoMovies))
								{
								}
							}
							else
								throw win32err2;
						});
					}
				});
			}
			catch (Exception)
			{
				// Some other unforeseen error:
				MessageBox.Show(null, string.Format(FrameworkStrings.ksErrorCannotLaunchMovies,
					string.Format(FwDirectoryFinder.CodeDirectory + "{0}Language Explorer{0}Movies",
					Path.DirectorySeparatorChar)), FrameworkStrings.ksError);
			}
		}

		/// <summary>
		/// Uses Process.Start to run path. If running in Linux and path ends in .html or .htm,
		/// surrounds the path in double quotes and opens it with a web browser.
		/// </summary>
		/// <param name="path"></param>
		/// <param name="exceptionHandler"/>
		/// Delegate to run if an exception is thrown. Takes the exception as an argument.

		private void OpenDocument(string path, Action<Exception> exceptionHandler)
		{
			OpenDocument<Exception>(path, exceptionHandler);
		}

		/// <summary>
		/// Like OpenDocument(), but allowing specification of specific exception type T to catch.
		/// </summary>
		private void OpenDocument<T>(string path, Action<T> exceptionHandler) where T : Exception
		{
			try
			{
				if (MiscUtils.IsUnix && (path.EndsWith(".html") || path.EndsWith(".htm")))
				{
					using (Process.Start(_webBrowserProgramLinux, Enquote(path)))
					{
					}
				}
				else
				{
					using (Process.Start(path))
					{
					}
				}
			}
			catch (T e)
			{
				if (exceptionHandler != null)
					exceptionHandler(e);
			}
		}

		/// <summary>
		/// Returns str surrounded by double-quotes.
		/// This is useful for paths containing spaces in Linux.
		/// </summary>
		private static string Enquote(string str)
		{
			return "\"" + str + "\"";
		}

		private void Help_Technical_Notes_on_FieldWorks_Send_Receive(object sender, EventArgs e)
		{
			var path = string.Format(FwDirectoryFinder.CodeDirectory +
				"{0}Helps{0}Language Explorer{0}Training{0}Technical Notes on FieldWorks Send-Receive.pdf",
				Path.DirectorySeparatorChar);

			OpenDocument(path, err =>
			{
				MessageBox.Show(null, string.Format(FrameworkStrings.ksCannotLaunchX, path),
					FrameworkStrings.ksError);
			});
		}

		private void Help_ReportProblem(object sender, EventArgs e)
		{
			ErrorReporter.ReportProblem(FwRegistryHelper.FieldWorksRegistryKey, _flexApp.SupportEmailAddress, this);
		}

		private void Help_Make_a_Suggestion(object sender, EventArgs e)
		{
			ErrorReporter.MakeSuggestion(FwRegistryHelper.FieldWorksRegistryKey, "FLExDevteam@sil.org", this);
		}

		private void Help_About_Language_Explorer(object sender, EventArgs e)
		{
			using (var helpAboutWnd = new FwHelpAbout())
			{
				helpAboutWnd.ProductExecutableAssembly = Assembly.LoadFile(_flexApp.ProductExecutableFile);
				helpAboutWnd.ShowDialog();
			}
		}

		private void File_New_FieldWorks_Project(object sender, EventArgs e)
		{
			if (_flexApp.ActiveMainWindow != this)
				throw new InvalidOperationException("Unexpected active window for app.");
			_flexApp.FwManager.CreateNewProject();
		}

		private void File_Open(object sender, EventArgs e)
		{
			_flexApp.FwManager.ChooseLangProject();
		}

		private void File_FieldWorks_Project_Properties(object sender, EventArgs e)
		{
			// 'true' for either of these two menus,
			// but 'false' for fieldWorksProjectPropertiesToolStripMenuItem on the File menu.
			LaunchProjPropertiesDlg(sender == setUpWritingSystemsToolStripMenuItem || sender == setUpWritingSystemsToolStripMenuItem1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Launches the proj properties DLG.
		/// </summary>
		/// <param name="startOnWSPage">if set to <c>true</c> [start on WS page].</param>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "cache is disposed elsewhere.")]
		private void LaunchProjPropertiesDlg(bool startOnWSPage)
		{
			var cache = _flexApp.Cache;
			if (!SharedBackendServicesHelper.WarnOnOpeningSingleUserDialog(cache))
				return;

			var fDbRenamed = false;
			var sProject = cache.ProjectId.Name;
			var sLinkedFilesRootDir = cache.LangProject.LinkedFilesRootDir;
			using (var dlg = new FwProjPropertiesDlg(cache, _flexApp, _flexApp, FontHeightAdjuster.StyleSheetFromPropertyTable(PropertyTable)))
			{
				dlg.ProjectPropertiesChanged += ProjectProperties_Changed;
				if (startOnWSPage)
				{
					dlg.StartWithWSPage();
				}
				if (dlg.ShowDialog(this) != DialogResult.Abort)
				{
					// NOTE: This code is called, even if the user cancelled the dlg.
					fDbRenamed = dlg.ProjectNameChanged();
					if (fDbRenamed)
					{
						sProject = dlg.ProjectName;
					}
					var fFilesMoved = false;
					if (dlg.LinkedFilesChanged())
					{
						fFilesMoved = _flexApp.UpdateExternalLinks(sLinkedFilesRootDir);
					}
					// no need for any of these refreshes if entire window has been/will be
					// destroyed and recreated.
					if (!fDbRenamed && !fFilesMoved)
					{
						SetWindowTitle();
					}
				}
			}
			if (fDbRenamed)
			{
				_flexApp.FwManager.RenameProject(sProject);
			}
		}

		private void SetWindowTitle()
		{
			Text = string.Format("{0} - {1}",
				_flexApp.Cache.ProjectId.UiName,
				FwUtils.ksSuiteName);
		}

		private void ProjectProperties_Changed(object sender, EventArgs eventArgs)
		{
			// this event is fired before the Project Properties dialog is closed, so that we have a chance
			// to refresh everything before Paint events start getting fired, which can cause problems if
			// any writing systems are removed that a rootsite is currently displaying
			var dlg = (FwProjPropertiesDlg)sender;
			if (dlg.WritingSystemsChanged())
			{
				View_Refresh(sender, eventArgs);
			}
		}

		/// <summary>
		/// This is the one (and should be only) handler for the user Refresh command.
		/// Refresh wants to first clean up the cache, then give things like Clerks a
		/// chance to reload stuff (calling the old OnRefresh methods), then give
		/// windows a chance to redisplay themselves.
		/// </summary>
		/// <remarks>
		/// Areas/Tools can decide to disable the F5  menu and toolbar refresh, if they wish. One (GrammarSketchTool) is known to do that.
		/// </remarks>
		private void View_Refresh(object sender, EventArgs e)
		{
			RefreshAllViews();
		}

		private void File_Back_up_this_Project(object sender, EventArgs e)
		{
			SaveSettings();

			_flexApp.FwManager.BackupProject(this);
		}

		private void File_Restore_a_Project(object sender, EventArgs e)
		{
			_flexApp.FwManager.RestoreProject(_flexApp, this);
		}

		private void File_Project_Location(object sender, EventArgs e)
		{
			_flexApp.FwManager.FileProjectLocation(_flexApp, this);
		}

		private void File_Delete_Project(object sender, EventArgs e)
		{
			_flexApp.FwManager.DeleteProject(_flexApp, this);
		}

		private void File_Create_Shortcut_on_Desktop(object sender, EventArgs e)
		{
			var directory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
			if (!FileUtils.DirectoryExists(directory))
			{
				MessageBoxUtils.Show(string.Format(
					"Error: Cannot create project shortcut because destination directory '{0}' does not exist.",
					directory));
				return;
			}

			var applicationArguments = "-" + FwAppArgs.kProject + " \"" + _flexApp.Cache.ProjectId.Handle + "\"";
			var description = ResourceHelper.FormatResourceString(
				"kstidCreateShortcutLinkDescription", _flexApp.Cache.ProjectId.UiName,
				_flexApp.ApplicationName);

			if (MiscUtils.IsUnix)
			{
				var projectName = _flexApp.Cache.ProjectId.UiName;
				const string pathExtension = ".desktop";
				var launcherPath = Path.Combine(directory, projectName + pathExtension);

				// Choose a different name if already in use
				var tailNumber = 2;
				while (FileUtils.SimilarFileExists(launcherPath))
				{
					var tail = "-" + tailNumber;
					launcherPath = Path.Combine(directory, projectName + tail + pathExtension);
					tailNumber++;
				}

				const string applicationExecutablePath = "fieldworks-flex";
				const string iconPath = "fieldworks-flex";
				if (string.IsNullOrEmpty(applicationExecutablePath))
					return;
				var content = string.Format(
					"[Desktop Entry]{0}" +
					"Version=1.0{0}" +
					"Terminal=false{0}" +
					"Exec=" + applicationExecutablePath + " " + applicationArguments + "{0}" +
					"Icon=" + iconPath + "{0}" +
					"Type=Application{0}" +
					"Name=" + projectName + "{0}" +
					"Comment=" + description + "{0}", Environment.NewLine);

				// Don't write a BOM
				using (var launcher = FileUtils.OpenFileForWrite(launcherPath, new UTF8Encoding(false)))
				{
					launcher.Write(content);
					FileUtils.SetExecutable(launcherPath);
				}
			}
			else
			{
				WshShell shell = new WshShellClass();

				var filename = _flexApp.Cache.ProjectId.UiName;
				filename = Path.ChangeExtension(filename, "lnk");
				var linkPath = Path.Combine(directory, filename);

				var link = (IWshShortcut)shell.CreateShortcut(linkPath);
				if (link.FullName != linkPath)
				{
					var msg = string.Format(FrameworkStrings.ksCannotCreateShortcut,
						_flexApp.ProductExecutableFile + " " + applicationArguments);
					MessageBox.Show(ActiveForm, msg,
						FrameworkStrings.ksCannotCreateShortcutCaption, MessageBoxButtons.OK,
						MessageBoxIcon.Asterisk);
					return;
				}
				link.TargetPath = _flexApp.ProductExecutableFile;
				link.Arguments = applicationArguments;
				link.Description = description;
				link.IconLocation = link.TargetPath + ",0";
				link.Save();
			}
		}

		private void File_Archive_With_RAMP(object sender, EventArgs e)
		{
			// prompt the user to select or create a FieldWorks backup
			var filesToArchive = _flexApp.FwManager.ArchiveProjectWithRamp(_flexApp, this);

			// if there are no files to archive, return now.
			if((filesToArchive == null) || (filesToArchive.Count == 0))
				return;

			// show the RAMP dialog
			var ramp = new ReapRamp();
			ramp.ArchiveNow(this, MainMenuStrip.Font, Icon, filesToArchive, PropertyTable, _flexApp, Cache);
		}

		private void File_Page_Setup(object sender, EventArgs e)
		{
			throw new NotSupportedException("There was no code to support this menu in the original system.");
		}

		private void File_Translated_List_Content(object sender, EventArgs e)
		{
			string filename;
			// ActiveForm can go null (see FWNX-731), so cache its value, and check whether
			// we need to use 'this' instead (which might be a better idea anyway).
			var form = ActiveForm ?? this;
			using (var dlg = new OpenFileDialogAdapter())
			{
				dlg.CheckFileExists = true;
				dlg.RestoreDirectory = true;
				dlg.Title = ResourceHelper.GetResourceString("kstidOpenTranslatedLists");
				dlg.ValidateNames = true;
				dlg.Multiselect = false;
				dlg.Filter = ResourceHelper.FileFilter(FileFilterType.FieldWorksTranslatedLists);
				if (dlg.ShowDialog(form) != DialogResult.OK)
				{
					return;
				}
				filename = dlg.FileName;
			}
			using (new WaitCursor(form, true))
			{
				NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(Cache.ActionHandlerAccessor,
					() =>
					{
						using (var dlg = new ProgressDialogWithTask(this))
						{
							dlg.AllowCancel = true;
							dlg.Maximum = 200;
							dlg.Message = filename;
							dlg.RunTask(true, FdoCache.ImportTranslatedLists, filename, Cache);
						}
					});
			}
		}

		private void NewWindow_Clicked(object sender, EventArgs e)
		{
			SaveSettings();
			_flexApp.FwManager.OpenNewWindowForApp();
		}

		private void Help_Training_Writing_Systems(object sender, EventArgs e)
		{
			var pathnameToWritingSystemHelpFile = string.Format(FwDirectoryFinder.CodeDirectory +
				"{0}Language Explorer{0}Training{0}Technical Notes on Writing Systems.pdf",
				Path.DirectorySeparatorChar);

			OpenDocument(pathnameToWritingSystemHelpFile, err =>
			{
				MessageBox.Show(null, string.Format(FrameworkStrings.ksCannotShowX, pathnameToWritingSystemHelpFile),
					FrameworkStrings.ksError);
			});
		}

		private void Help_XLingPaper(object sender, EventArgs e)
		{
			var xLingPaperPathname = string.Format(FwDirectoryFinder.CodeDirectory + "{0}Helps{0}XLingPap{0}UserDoc.htm",
				Path.DirectorySeparatorChar);

			OpenDocument(xLingPaperPathname, err =>
			{
				MessageBox.Show(null, string.Format(FrameworkStrings.ksCannotShowX, xLingPaperPathname),
					FrameworkStrings.ksError);
			});
		}

		private void Edit_Cut(object sender, EventArgs e)
		{
			using (new DataUpdateMonitor(this, "EditCut"))
			{
				_viewHelper.ActiveView.EditingHelper.CutSelection();
			}
		}

		private void Edit_Copy(object sender, EventArgs e)
		{
			_viewHelper.ActiveView.EditingHelper.CopySelection();
		}

		private void Edit_Paste(object sender, EventArgs e)
		{
			string stUndo, stRedo;
			ResourceHelper.MakeUndoRedoLabels("kstidEditPaste", out stUndo, out stRedo);
			using (var undoHelper = new UndoableUnitOfWorkHelper(Cache.ServiceLocator.GetInstance<IActionHandler>(), stUndo, stRedo))
			using (new DataUpdateMonitor(this, "EditPaste"))
			{
				if (_viewHelper.ActiveView.EditingHelper.PasteClipboard())
				{
					undoHelper.RollBack = false;
				}
			}
		}

		private void EditMenu_Opening(object sender, EventArgs e)
		{
			var hasActiveView = _viewHelper.ActiveView != null;
			selectAllToolStripMenuItem.Enabled = hasActiveView;
			cutToolStripMenuItem.Enabled = (hasActiveView && _viewHelper.ActiveView.EditingHelper.CanCut());
			copyToolStripMenuItem.Enabled = (hasActiveView && _viewHelper.ActiveView.EditingHelper.CanCopy());
			pasteToolStripMenuItem.Enabled = (hasActiveView && _viewHelper.ActiveView.EditingHelper.CanPaste());
			pasteHyperlinkToolStripMenuItem.Enabled = (hasActiveView
				&& _viewHelper.ActiveView.EditingHelper is RootSiteEditingHelper
				&& ((RootSiteEditingHelper)_viewHelper.ActiveView.EditingHelper).CanPasteUrl());
#if RANDYTODO
			// TODO: Handle enabling/disabling other Edit menu/toolbar items, such as Undo & Redo.
#else
			// TODO: In the meantime, just go with disabled.
			undoToolStripMenuItem.Enabled = false;
			redoToolStripMenuItem.Enabled = false;
			undoToolStripButton.Enabled = false;
			redoToolStripButton.Enabled = false;
#endif
		}

		private void File_Export_Global(object sender, EventArgs e)
		{
			// This handles the general case if nobody else is handling it.
			// Other handlers:
			//		A. The notebook area does its version for all of its tools.
			//		B. The "grammarSketch" tool in the 'grammar" area does its own thing. (Same as below, but without AreCustomFieldsAProblem and ActivateUI.)
			// Not visible and thus, not enabled:
			//		A. Tools in "textsWords": complexConcordance, concordance, corpusStatistics, and interlinearEdit
			// Stuff that uses this code:
			//		A. lexicon area: all 8 tools
			//		B. textsWords area: Analyses, bulkEditWordforms, wordListConcordance (all use "concordanceWords" clerk, so can do export)
			//		C. grammar area: all tools, except grammarSketch,, which goes its own way
			//		D. lists area: all 27 tools
#if RANDYTODO
			// TODO: RecordClerk's "AreCustomFieldsAProblem" method will also need a new home: maybe FDO is a better place for it.
			// It's somewhat unfortunate that this bit of code knows what classes can have custom fields.
			// However, we put in code to prevent punctuation in custom field names at the same time as this check (which is therefore
			// for the benefit of older projects), so it should not be necessary to check any additional classes we allow to have them.
			if (AreCustomFieldsAProblem(new int[] { LexEntryTags.kClassId, LexSenseTags.kClassId, LexExampleSentenceTags.kClassId, MoFormTags.kClassId }))
				return true;
			using (var dlg = new ExportDialog())
			{
				dlg.InitializeFlexComponent(PropertyTable, Publisher, Subscriber);
				dlg.ShowDialog();
			}
#if RANDYTODO
			// TODO: This method is on RecordClerk, so figure out how to call it from here, if it is still needed at all.
			ActivateUI(true);
#endif
#else
			MessageBox.Show(this, @"Export not yet implemented. Stay tuned.", @"Export not ready", MessageBoxButtons.OK);
#endif
		}

		private void Edit_Paste_Hyperlink(object sender, EventArgs e)
		{
			if (_stylesheet == null)
			{
				_stylesheet = new FwStyleSheet();
				_stylesheet.Init(Cache, Cache.LanguageProject.Hvo, LangProjectTags.kflidStyles);
#if RANDYTODO
				// TODO: I (RandyR) don't think there is a reason to do this now,
				// unless there is some style UI widget on the toolbar (menu?) that needs to be updated.
				if (m_rebarAdapter is IUIAdapterForceRegenerate)
				{
					((IUIAdapterForceRegenerate)m_rebarAdapter).ForceFullRegenerate();
				}
#endif
			}
			((RootSiteEditingHelper)_viewHelper.ActiveView.EditingHelper).PasteUrl(_stylesheet);
		}

		private void Edit_Select_All(object sender, EventArgs e)
		{
#if RANDYTODO
/*
	Jason Naylor's expanded comment on potential issues.
	Things to keep in mind:

	"I think if anything my comment regards a potential design improvement that is
beyond the scope of this current change. You might want to have a quick look at the
SIL.FieldWorks.IText.StatisticsView and how it will play into this. You may find nothing
that needs to change. I wrote that class way back before I understood how many tentacles
the xWindow and other 'x' classes had. We needed a very simple view of data that wasn't
in any of our blessed RecordLists. I wrote this view with the idea that it would be tied
into the xBeast as little as possible. This was years ago.

	After your changes I expect that something like this would be easier to do, and easy to
make more functional. The fact that the ActiveView is an IRootSite factors in to what kind
of views we are able to have.

	Just giving you more to think about while you're working on these
very simple minor adjustments. ;)"
*/
#endif
			using (new WaitCursor(this))
			{
				if (DataUpdateMonitor.IsUpdateInProgress())
				{
					return;
				}
				_viewHelper.ActiveView.EditingHelper.SelectAll();
			}
		}

		#region Overrides of Form

		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Form.Load"/> event.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data. </param>
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			var currentArea = _areaRepository.GetPersistedOrDefaultArea();
			var currentTool = currentArea.GetPersistedOrDefaultToolForArea();
			_sidePane.TabClicked += Area_Clicked;
			_sidePane.ItemClicked += Tool_Clicked;
			// This call fires Area_Clicked and then Tool_Clicked to make sure the provided tab and item are both selected in the end.
			_sidePane.SelectItem(_sidePane.GetTabByName(currentArea.MachineName), currentTool.MachineName);
		}

		#endregion
	}
}

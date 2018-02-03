// Copyright (c) 2014-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using Gecko;
using Gecko.DOM;
using LanguageExplorer.DictionaryConfiguration;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FwCoreDlgControls;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Utils;
using SIL.Windows.Forms.HtmlBrowser;
using SIL.Xml;

namespace LanguageExplorer.Areas.Lexicon.DictionaryConfiguration
{
	/// <summary>
	/// This class handles the display of configured xhtml for a particular publication in a dynamically loadable XWorksView.
	/// </summary>
	internal class XhtmlDocView : ViewBase, IFindAndReplaceContext, IPostLayoutInit
	{
		private XWebBrowser m_mainView;
		private DictionaryPublicationDecorator m_pubDecorator;
		private string m_selectedObjectID = string.Empty;
		internal string m_configObjectName;
		private string m_currentConfigView; // used when this is a Dictionary view to store which view is active.
		private ToolStripMenuItem m_printMenu;

		/// <summary />
		internal XhtmlDocView(XElement configurationParametersElement, LcmCache cache, IRecordList recordList, ToolStripMenuItem printMenu)
			: base(configurationParametersElement, cache, recordList)
		{
			m_printMenu = printMenu;
			m_printMenu.Click += PrintMenu_Click;
			m_printMenu.Enabled = true;
		}

		#region Overrides of ViewBase
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				m_printMenu.Click -= PrintMenu_Click;
				m_printMenu.Enabled = false;
			}

			base.Dispose(disposing);

			m_printMenu = null;
		}

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public override void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			base.InitializeFlexComponent(flexComponentParameters);

			m_mainView = new XWebBrowser(XWebBrowser.BrowserType.GeckoFx)
			{
				Dock = DockStyle.Fill,
				Location = new Point(0, 0),
				IsWebBrowserContextMenuEnabled = false
			};
			ReadParameters();
			// Use update helper to help with optimizations and special cases for list loading
			using (new ListUpdateHelper(new ListUpdateHelperParameterObject { MyRecordList = MyRecordList }))
			{
				MyRecordList.UpdateOwningObjectIfNeeded();
			}
			Controls.Add(m_mainView);

			var browser = m_mainView.NativeBrowser as GeckoWebBrowser;
			if (browser != null)
			{
				var recordListId = XmlUtils.GetOptionalAttributeValue(m_configurationParametersElement, "clerk");
				if (recordListId == LexiconArea.Entries || recordListId == LexiconArea.AllReversalEntries)
				{
					browser.DomClick += OnDomClick;
					browser.DomKeyPress += OnDomKeyPress;
					browser.DocumentCompleted += OnDocumentCompleted;
					browser.DomMouseScroll += OnMouseWheel;
				}
			}
		}

		/// <summary>
		/// About to show, so finish initializing.
		/// </summary>
		public void FinishInitialization()
		{
			// retrieve persisted record list index and set it.
			var idx = PropertyTable.GetValue(MyRecordList.PersistedIndexProperty, SettingsGroup.LocalSettings, -1);
			var lim = MyRecordList.ListSize;
			if (idx >= 0 && idx < lim)
			{
				var idxOld = MyRecordList.CurrentIndex;
				try
				{
					MyRecordList.JumpToIndex(idx);
				}
				catch
				{
					if (lim > idxOld && lim > 0)
					{
						MyRecordList.JumpToIndex(idxOld >= 0 ? idxOld : 0);
					}
				}
			}

			ShowRecord();
			m_fullyInitialized = true;
		}

		private void OnMouseWheel(object sender, DomMouseEventArgs domMouseEventArgs)
		{
			var scrollDelta = domMouseEventArgs.Detail;
			var browser = (GeckoWebBrowser)m_mainView.NativeBrowser;
			if (scrollDelta < 0 && browser.Window.ScrollY == 0)
			{
				AddMoreEntriesToPage(true, (GeckoWebBrowser)m_mainView.NativeBrowser);
			}
			else if (browser.Window.ScrollY >= browser.Window.ScrollMaxY)
			{
				AddMoreEntriesToPage(false, (GeckoWebBrowser)m_mainView.NativeBrowser);
			}
		}

		/// <summary>
		/// Handle a key press in the web browser displaying the xhtml. [TODO: LT-xxxxx]
		/// </summary>
		private void OnDomKeyPress(object sender, DomKeyEventArgs e)
		{
			var browser = (GeckoWebBrowser) m_mainView.NativeBrowser;
			const int UP = 38;
			const int DOWN = 40;
			const int PAGEUP = 33;
			const int PAGEDOWN = 34;
			switch (e.KeyCode)
			{
				case UP:
				{
					if (browser.Window.ScrollY == 0)
					{
						AddMoreEntriesToPage(true, (GeckoWebBrowser)m_mainView.NativeBrowser);
					}
					break;
				}
				case DOWN:
				{
						if (browser.Window.ScrollY >= browser.Window.ScrollMaxY)
					{
						AddMoreEntriesToPage(false, (GeckoWebBrowser)m_mainView.NativeBrowser);
					}
					break;
				}
				case PAGEUP:
				{
					if (browser.Window.ScrollY == 0)
					{
						var currentPage = GetTopCurrentPageButton(browser.Document.Body);
						if (currentPage.PreviousSibling != null)
						{
							var itemIndex = int.Parse(((GeckoHtmlElement)currentPage.PreviousSibling).Attributes["endIndex"].NodeValue);
							MyRecordList.JumpToRecord(PublicationDecorator.GetEntriesToPublish(PropertyTable, MyRecordList.VirtualFlid)[itemIndex]);
						}
					}
					break;
				}
				case PAGEDOWN:
				{
					if (browser.Window.ScrollY >= browser.Window.ScrollMaxY)
					{
						var currentPage = GetTopCurrentPageButton(browser.Document.Body);
						if (currentPage.NextSibling != null)
						{
							var itemIndex = int.Parse(((GeckoHtmlElement)currentPage.NextSibling).Attributes["startIndex"].NodeValue);
							MyRecordList.JumpToRecord(PublicationDecorator.GetEntriesToPublish(PropertyTable, MyRecordList.VirtualFlid)[itemIndex]);
						}
					}
					break;
				}
				default:
					break;
			}
		}

		/// <summary>
		/// Check to see if the user needs to be alerted that JumpToRecord is not possible.
		/// </summary>
		/// <param name="argument">the hvo of the record</param>
		/// <returns></returns>
		public bool OnJumpToRecord(object argument)
		{
			var hvoTarget = (int)argument;
			if (hvoTarget <= 0 || PropertyTable.GetValue<string>(AreaServices.ToolChoice) !=
				AreaServices.LexiconDictionaryMachineName)
			{
				return false;
			}
			ExclusionReasonCode xrc;
			// Make sure we explain to the user in case hvoTarget is not visible due to
			// the current Publication layout or Configuration view.
			if (!IsObjectVisible(hvoTarget, out xrc))
			{
				AreaServices.GiveSimpleWarning(PropertyTable.GetValue<Form>("window"), PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider").HelpFile, xrc);
			}
			return false;
		}

		private bool IsObjectVisible(int hvoTarget, out ExclusionReasonCode xrc)
		{
			xrc = ExclusionReasonCode.NotExcluded;
			var objRepo = Cache.ServiceLocator.GetInstance<ICmObjectRepository>();
			Debug.Assert(objRepo.IsValidObjectId(hvoTarget), "Invalid hvoTarget!");
			if (!objRepo.IsValidObjectId(hvoTarget))
			{
				throw new ArgumentException("Unknown object.");
			}
			var entry = objRepo.GetObject(hvoTarget) as ILexEntry;
			Debug.Assert(entry != null, "HvoTarget is not a LexEntry!");
			if (entry == null)
			{
				throw new ArgumentException("Target is not a LexEntry.");
			}

			// Now we have our LexEntry
			// First deal with whether the active Publication excludes it.
			var m_currentPublication = PropertyTable.GetValue<string>("SelectedPublication", null);
			var publications = Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Select(p => p).FirstOrDefault(p => p.NameHierarchyString == m_currentPublication.ToString());
			//if the publications is null in case of Dictionary view selected as $$All Entries$$.
			if (publications != null && publications.NameHierarchyString != LanguageExplorerResources.AllEntriesPublication)
			{
				var currentPubPoss = publications;
				if (!entry.PublishIn.Contains(currentPubPoss))
				{
					xrc = ExclusionReasonCode.NotInPublication;
					return false;
				}
				// Second deal with whether the entry shouldn't be shown as a headword
				if (!entry.ShowMainEntryIn.Contains(currentPubPoss))
				{
					xrc = ExclusionReasonCode.ExcludedHeadword;
					return false;
				}
			}
			// Third deal with whether the entry shouldn't be shown as a minor entry.
			// commented out until conditions are clarified (LT-11447)
			var configuration = new DictionaryConfigurationModel(GetCurrentConfiguration(false), Cache);
			if (entry.EntryRefsOS.Count > 0 && !entry.PublishAsMinorEntry && configuration.IsRootBased)
			{
				xrc = ExclusionReasonCode.ExcludedMinorEntry;
				return false;
			}
			// If we get here, we should be able to display it.
			return true;
		}

		/// <summary>
		/// Handle a mouse click in the web browser displaying the xhtml.
		/// </summary>
		private void OnDomClick(object sender, DomMouseEventArgs e)
		{
			CloseContextMenuIfOpen();
			var browser = m_mainView.NativeBrowser as GeckoWebBrowser;
			var element = browser?.DomDocument.ElementFromPoint(e.ClientX, e.ClientY);
			if (element == null || element.TagName == "html")
			{
				return;
			}
			switch (e.Button)
			{
				case GeckoMouseButton.Left:
					if (HandleClickOnPageButton(MyRecordList, Cache.ServiceLocator.ObjectRepository, element))
					{
						return;
					}
					// Handle button clicks or select the entry represented by the current element.
					HandleDomLeftClick(MyRecordList, Cache.ServiceLocator.ObjectRepository, e, element);
					break;
				case GeckoMouseButton.Right:
					HandleDomRightClick(browser, e, element, new FlexComponentParameters(PropertyTable, Publisher, Subscriber), m_configObjectName, Cache, MyRecordList);
					break;
			}
		}

		/// <summary>
		/// Set the style attribute on the current entry to color the background after document completed.
		/// </summary>
		private void OnDocumentCompleted(object sender, EventArgs e)
		{
			var browser = m_mainView.NativeBrowser as GeckoWebBrowser;
			if (browser == null)
			{
				return;
			}
			CloseContextMenuIfOpen();
			SetActiveSelectedEntryOnView(browser);
			// Without this we show the entry count in the status bar the first time we open the Dictionary or Rev. Index.
			MyRecordList.SelectedRecordChanged(true, true);
		}

		/// <summary>
		/// Handle the user left clicking on the document view by jumping to an entry, playing a media element, or adjusting the view
		/// </summary>
		/// <remarks>internal so that it can be re-used by the XhtmlRecordDocView</remarks>
		internal static void HandleDomLeftClick(IRecordList recordList, ICmObjectRepository objectRepository, DomMouseEventArgs e, GeckoElement element)
		{
			var topLevelGuid = DictionaryConfigurationServices.GetHrefFromGeckoDomElement(element);
			if (topLevelGuid == Guid.Empty)
			{
				GeckoElement dummy;
				DictionaryConfigurationServices.GetClassListFromGeckoElement(element, out topLevelGuid, out dummy);
			}
			if (topLevelGuid != Guid.Empty)
			{
				var currentObj = recordList.CurrentObject;
				if (currentObj != null && currentObj.Guid == topLevelGuid)
				{
					// don't need to jump, we're already here...
					// unless this is a video link
					if (element is GeckoAnchorElement)
					{
						return; // don't handle the click; gecko will jump to the link
					}
				}
				else
				{
					var obj = objectRepository.GetObject(topLevelGuid);
					recordList.JumpToRecord(obj.Hvo);
				}
			}
			e.Handled = true;
		}

		private void AddMoreEntriesToPage(bool goingUp, GeckoWebBrowser browser)
		{
			var browserElement = browser.Document.Body;
			var entriesToPublish = PublicationDecorator.GetEntriesToPublish(PropertyTable, MyRecordList.VirtualFlid);
			// Right-to-Left for the overall layout is determined by Dictionary-Normal
			var dictionaryNormalStyle = new ExportStyleInfo(FontHeightAdjuster.StyleSheetFromPropertyTable(PropertyTable).Styles["Dictionary-Normal"]);
			var isNormalRightToLeft = dictionaryNormalStyle.DirectionIsRightToLeft == TriStateBool.triTrue; // default is LTR
			// Get the current page
			if (goingUp)
			{
				// Use the up/down info to select the adjacentPage
				Tuple<int, int> newCurPageRange;
				Tuple<int, int> newAdjPageRange;
				// Gecko xpath seems to be sensitive to namespaces, using * instead of span helps
				var currentPageButton = GetTopCurrentPageButton(browserElement);
				var adjacentPageButton = (GeckoHtmlElement)currentPageButton?.PreviousSibling;
				if (adjacentPageButton == null)
				{
					return;
				}
				var oldCurPageRange = new Tuple<int, int>(int.Parse(currentPageButton.Attributes["startIndex"].NodeValue), int.Parse(currentPageButton.Attributes["endIndex"].NodeValue));
				var oldAdjPageRange = new Tuple<int, int>(int.Parse(adjacentPageButton.Attributes["startIndex"].NodeValue), int.Parse(adjacentPageButton.Attributes["endIndex"].NodeValue));
				var settings = new GeneratorSettings(Cache, new ReadOnlyPropertyTable(PropertyTable), false, false, string.Empty, isNormalRightToLeft);
				var entries = ConfiguredXHTMLGenerator.GenerateNextFewEntries(PublicationDecorator, entriesToPublish, GetCurrentConfiguration(false), settings, oldCurPageRange,
					oldAdjPageRange, ConfiguredXHTMLGenerator.EntriesToAddCount, out newCurPageRange, out newAdjPageRange);
				// Load entries above the first entry
				foreach (var entry in entries)
				{
					var entryElement = browserElement.OwnerDocument.CreateHtmlElement("div");
					var entryDoc = XDocument.Parse(entry);
					foreach (var attribute in entryDoc.Root.Attributes())
					{
						entryElement.SetAttribute(attribute.Name.ToString(), attribute.Value);
					}
					entryElement.InnerHtml = string.Join("", entryDoc.Root.Elements().Select(x => x.ToString(SaveOptions.DisableFormatting)));
					// Get the div of the first entry element
					var before = browserElement.SelectFirst("*[contains(@class, 'entry')]");
					before.ParentElement.InsertBefore(entryElement, before);
				}
				ChangeHtmlForCurrentAndAdjacentButtons(newCurPageRange, newAdjPageRange, currentPageButton, true);
			}
			else
			{
				// Use the up/down info to select the adjacentPage
				Tuple<int, int> newCurrentPageRange;
				Tuple<int, int> newAdjPageRange;
				// Gecko xpath seems to be sensitive to namespaces, using * instead of span helps
				var currentPageButton = GetBottomCurrentPageButton(browserElement);
				var adjPage = (GeckoHtmlElement) currentPageButton?.NextSibling;
				if (adjPage == null)
				{
					return;
				}
				var currentPageRange = new Tuple<int, int>(int.Parse(currentPageButton.Attributes["startIndex"].NodeValue), int.Parse(currentPageButton.Attributes["endIndex"].NodeValue));
				var adjacentPageRange = new Tuple<int, int>(int.Parse(adjPage.Attributes["startIndex"].NodeValue), int.Parse(adjPage.Attributes["endIndex"].NodeValue));
				var settings = new GeneratorSettings(Cache, new ReadOnlyPropertyTable(PropertyTable), false, false, string.Empty, isNormalRightToLeft);
				var entries = ConfiguredXHTMLGenerator.GenerateNextFewEntries(PublicationDecorator, entriesToPublish, GetCurrentConfiguration(false), settings, currentPageRange,
					adjacentPageRange, ConfiguredXHTMLGenerator.EntriesToAddCount, out newCurrentPageRange, out newAdjPageRange);
				// Load entries above the lower navigation buttons
				foreach (var entry in entries)
				{
					var entryElement = browserElement.OwnerDocument.CreateHtmlElement("div"); var entryDoc = XDocument.Parse(entry);
					foreach (var attribute in entryDoc.Root.Attributes())
					{
						entryElement.SetAttribute(attribute.Name.ToString(), attribute.Value);
					}
					entryElement.InnerHtml = string.Join("", entryDoc.Root.Elements().Select(x => x.ToString(SaveOptions.DisableFormatting)));

					var buttonDiv = currentPageButton.ParentElement;
					buttonDiv.ParentNode.InsertBefore(entryElement, buttonDiv);
				}
				ChangeHtmlForCurrentAndAdjacentButtons(newCurrentPageRange, newAdjPageRange, currentPageButton, false);
			}
			m_mainView.Refresh();
		}

		private void ChangeHtmlForCurrentAndAdjacentButtons(Tuple<int, int> newCurrentPageRange, Tuple<int, int> newAdjacentPageRange, GeckoElement pageButtonElement, bool goingUp)
		{
			var currentPageTop = GetTopCurrentPageButton(pageButtonElement);
			var adjPageTop = goingUp ? (GeckoHtmlElement)currentPageTop.PreviousSibling : (GeckoHtmlElement)currentPageTop.NextSibling;
			var currentPageBottom = GetBottomCurrentPageButton(pageButtonElement);
			var adjPageBottom = goingUp ? (GeckoHtmlElement)currentPageBottom.PreviousSibling : (GeckoHtmlElement)currentPageBottom.NextSibling;
			currentPageTop.SetAttribute("startIndex", newCurrentPageRange.Item1.ToString());
			currentPageBottom.SetAttribute("startIndex", newCurrentPageRange.Item1.ToString());
			currentPageTop.SetAttribute("endIndex", newCurrentPageRange.Item2.ToString());
			currentPageBottom.SetAttribute("endIndex", newCurrentPageRange.Item2.ToString());
			if (newAdjacentPageRange != null)
			{
				adjPageTop.SetAttribute("startIndex", newAdjacentPageRange.Item1.ToString());
				adjPageBottom.SetAttribute("startIndex", newAdjacentPageRange.Item1.ToString());
				adjPageTop.SetAttribute("endIndex", newAdjacentPageRange.Item2.ToString());
				adjPageBottom.SetAttribute("endIndex", newAdjacentPageRange.Item2.ToString());
			}
			else
			{
				adjPageTop.Parent.RemoveChild(adjPageTop);
				adjPageBottom.Parent.RemoveChild(adjPageBottom);
			}
		}

		private static GeckoHtmlElement GetBottomCurrentPageButton(GeckoElement pageButtonElement)
		{
			// from the parent node select the second instance of the current page (the one with the id)
			return (GeckoHtmlElement)pageButtonElement.OwnerDocument.Body.SelectFirst("(//*[@class='pagebutton' and @id])[2]");
		}

		private static GeckoHtmlElement GetTopCurrentPageButton(GeckoElement element)
		{
			// The page with the id is the current page, select the first one on the page
			return (GeckoHtmlElement)element.OwnerDocument.Body.SelectFirst("//*[@class='pagebutton' and @id]");
		}

		private static bool HandleClickOnPageButton(IRecordList recordList, ICmObjectRepository objectRepository, GeckoElement element)
		{
			if (!element.HasAttribute("class") || !element.Attributes["class"].NodeValue.Equals("pagebutton"))
			{
				return false;
			}

			if (!element.HasAttribute("firstEntryGuid"))
			{
				throw new ArgumentException(@"The element passed to this method should have a firstEntryGuid.", nameof(element));
			}
			var firstEntryOnPage = element.Attributes["firstEntryGuid"].NodeValue;
			var obj = objectRepository.GetObject(new Guid(firstEntryOnPage));
			recordList.JumpToRecord(obj.Hvo);
			return true;
		}

		/// <summary>
		/// Pop up a menu to allow the user to start the document configuration dialog, and
		/// start the dialog at the configuration node indicated by the current element.
		/// </summary>
		/// <remarks>
		/// This is static so that the method can be shared with XhtmlRecordDocView.
		/// </remarks>
		internal static void HandleDomRightClick(GeckoWebBrowser browser, DomMouseEventArgs e,
			GeckoElement element, FlexComponentParameters flexComponentParameters, string configObjectName,
			LcmCache cache, IRecordList activeRecordList)
		{
			Guid topLevelGuid;
			GeckoElement entryElement;
			var classList = DictionaryConfigurationServices.GetClassListFromGeckoElement(element, out topLevelGuid, out entryElement);
			var localizedName = DictionaryConfigurationServices.GetDictionaryConfigurationType(flexComponentParameters.PropertyTable);
			var label = string.Format(AreaResources.ksConfigure, localizedName);
			s_contextMenu = new ContextMenuStrip();
			var item = new DisposableToolStripMenuItem(label);
			s_contextMenu.Items.Add(item);
			item.Click += RunConfigureDialogAt;
			item.Tag = new object[] { flexComponentParameters.PropertyTable, flexComponentParameters.Publisher, classList, topLevelGuid, flexComponentParameters.Subscriber, cache, activeRecordList };
			if (e.CtrlKey) // show hidden menu item for tech support
			{
				item = new DisposableToolStripMenuItem(AreaResources.ksInspect);
				s_contextMenu.Items.Add(item);
				item.Click += RunDiagnosticsDialogAt;
				item.Tag = new object[] { flexComponentParameters.PropertyTable, entryElement, topLevelGuid };
			}
			s_contextMenu.Show(browser, new Point(e.ClientX, e.ClientY));
			s_contextMenu.Closed += m_contextMenu_Closed;
			e.Handled = true;
		}

		/// <summary>
		/// Close and delete the context menu if it exists.  This appears to be needed (at least on Linux)
		/// if the user clicks somewhere in the browser other than the menu.
		/// </summary>
		internal static void CloseContextMenuIfOpen()
		{
			if (s_contextMenu == null)
			{
				return;
			}
			s_contextMenu.Close();
			DisposeContextMenu(null, null);
		}

		private static void m_contextMenu_Closed(object sender, ToolStripDropDownClosedEventArgs e)
		{
			Application.Idle += DisposeContextMenu;
		}

		private static void DisposeContextMenu(object sender, EventArgs e)
		{
			Application.Idle -= DisposeContextMenu;
			if (s_contextMenu == null)
			{
				return;
			}
			s_contextMenu.Dispose();
			s_contextMenu = null;
		}

		// Context menu exists just for one invocation (until idle).
		private static ContextMenuStrip s_contextMenu;

		private static void RunConfigureDialogAt(object sender, EventArgs e)
		{
			var item = (ToolStripMenuItem)sender;
			var tagObjects = (object[])item.Tag;
			var propertyTable = (IPropertyTable)tagObjects[0];
			var publisher = (IPublisher)tagObjects[1];
			var classList = tagObjects[2] as List<string>;
			var guid = (Guid)tagObjects[3];
			// 4 is used further down
			var cache = (LcmCache)tagObjects[5];
			var activeRecordList = (RecordList)tagObjects[6];
			bool refreshNeeded;
			var mainWindow = propertyTable.GetValue<IFwMainWnd>("window");
			using (var dlg = new DictionaryConfigurationDlg(propertyTable))
			{
				ICmObject current = null;
				if (guid != Guid.Empty && cache != null && cache.ServiceLocator.ObjectRepository.IsValidObjectId(guid))
				{
					current = cache.ServiceLocator.GetObject(guid);
				}
				else if (activeRecordList != null)
				{
					current = activeRecordList.CurrentObject;
				}
				var controller = new DictionaryConfigurationController(dlg, current);
				controller.InitializeFlexComponent(new FlexComponentParameters(propertyTable, publisher, (ISubscriber)tagObjects[4]));
				controller.SetStartingNode(classList);
				dlg.Text = string.Format(LexiconResources.ConfigureTitle, DictionaryConfigurationServices.GetDictionaryConfigurationType(propertyTable));
				dlg.HelpTopic = DictionaryConfigurationServices.GetConfigDialogHelpTopic(propertyTable);
				dlg.ShowDialog((IWin32Window)mainWindow);
				refreshNeeded = controller.MasterRefreshRequired;
			}
			if (refreshNeeded)
			{
				mainWindow.RefreshAllViews();
			}
		}

		private static void RunDiagnosticsDialogAt(object sender, EventArgs e)
		{
			var item = (ToolStripMenuItem)sender;
			var tagObjects = (object[])item.Tag;
			var propTable = (IPropertyTable)tagObjects[0];
			var element = (GeckoElement)tagObjects[1];
			var guid = (Guid)tagObjects[2];
			using (var dlg = new XmlDiagnosticsDlg(element, guid))
			{
				dlg.ShowDialog(propTable.GetValue<IWin32Window>("window"));
			}
		}

#endregion

		/// <summary>
		/// We wait until containing controls are laid out to try to set the content
		/// </summary>
		public void PostLayoutInit()
		{
			// Tell the record list it is active so it will update the list of entries. Pass false as we have no toolbar to update.
			MyRecordList.ActivateUI();
			// Update the entry list if necessary
			if(!MyRecordList.ListLoadingSuppressed && MyRecordList.RequestedLoadWhileSuppressed)
			{
				MyRecordList.UpdateList(true, true);
			}
			// Grab the selected publication and make sure that we grab a valid configuration for it.
			// In some cases (e.g where a user reset their local settings) the stored configuration may no longer
			// exist on disk.
			var validConfiguration = SetCurrentDictionaryPublicationLayout();
			if (string.IsNullOrEmpty(PropertyTable.GetValue("SuspendLoadingRecordUntilOnJumpToRecord", string.Empty)))
			{
				UpdateContent(PublicationDecorator, validConfiguration);
			}
		}

		private string SetCurrentDictionaryPublicationLayout()
		{
			var pubName = GetCurrentPublication();
			var currentConfiguration = GetCurrentConfiguration(false);
			var validConfiguration = GetValidConfigurationForPublication(pubName);
			if (validConfiguration != currentConfiguration)
			{
				SetCurrentConfiguration(validConfiguration, false);
			}
			return validConfiguration;
		}

#if RANDYTODO
		/// <summary>
		/// Populate the list of publications for the first dictionary titlebar menu.
		/// </summary>
		public bool OnDisplayPublications(object parameter, ref UIListDisplayProperties display)
		{
			List<string> inConfig;
			List<string> notInConfig;
			SplitPublicationsByConfiguration(Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS,
														GetCurrentConfiguration(false),
														out inConfig, out notInConfig);
			foreach(var pub in inConfig)
			{
				display.List.Add(pub, pub, null, null);
			}
			if(notInConfig.Any())
			{
				display.List.Add(new SeparatorItem());
				foreach(var pub in notInConfig)
				{
					display.List.Add(pub, pub, null, null);
				}
			}
			return true;
		}

		/// <summary>
		/// Populate a list of reversal index configurations for display in the reversal index configuration
		/// chooser drop-down list in the Reversal Indexes area.
		/// Omit the "All Reversal Indexes" item (LT-17170).
		/// </summary>
		public bool OnDisplayReversalIndexList(object parameter, ref UIListDisplayProperties display)
		{
			var handled = OnDisplayConfigurations(parameter, ref display);
			DictionaryConfigurationUtils.RemoveAllReversalChoiceFromList(ref display);
			return handled;
		}

		/// <summary>
		/// Populate the list of dictionary configuration views for the second dictionary titlebar menu.
		/// </summary>
		/// <remarks>The areaconfiguration.xml defines the "Configurations" menu and the XWorksViews event handling calls this</remarks>
		public bool OnDisplayConfigurations(object parameter, ref UIListDisplayProperties display)
		{
			IDictionary<string, string> hasPub;
			IDictionary<string, string> doesNotHavePub;
			var allConfigurations = DictionaryConfigurationUtils.GatherBuiltInAndUserConfigurations(Cache, m_configObjectName);
			SplitConfigurationsByPublication(allConfigurations,
														GetCurrentPublication(),
														out hasPub, out doesNotHavePub);
			// Add menu items that display the configuration name and send PropChanges with
			// the configuration path.
			foreach(var config in hasPub)
			{
				display.List.Add(config.Key, config.Value, null, null);
			}
			if(doesNotHavePub.Count > 0)
			{
				display.List.Add(new SeparatorItem());
				foreach(var config in doesNotHavePub)
				{
					display.List.Add(config.Key, config.Value, null, null);
				}
			}
			return true;
		}
#endif

		/// <summary>
		/// Read in the parameters to determine which sequence/collection we are editing.
		/// </summary>
		protected override void ReadParameters()
		{
			base.ReadParameters();
			var backColorName = XmlUtils.GetOptionalAttributeValue(m_configurationParametersElement, "backColor", "Window");
			BackColor = Color.FromName(backColorName);
			m_configObjectName = XmlUtils.GetOptionalAttributeValue(m_configurationParametersElement, "configureObjectName", null);
		}

		/// <summary>
		/// Handle the 'File Print...' menu item click (defined in the Lexicon areaConfiguration.xml)
		/// </summary>
		private void PrintMenu_Click(object sender, EventArgs e)
		{
			CloseContextMenuIfOpen(); // not sure if this is necessary or not
			PrintPage(m_mainView);
		}

		internal static void PrintPage(XWebBrowser browser)
		{
			var geckoBrowser = browser.NativeBrowser as GeckoWebBrowser;
			geckoBrowser?.Window.Print();
		}

		/// <summary>
		/// We aren't using the record list for this view so we override this method to do nothing.
		/// </summary>
		protected override void SetupDataContext()
		{
		}

		/// <summary>
		/// Handle the 'Edit Publications' menu item click (defined in the Lexicon areaConfiguration.xml)
		/// </summary>
		/// <param name="commandObject"></param>
		/// <returns></returns>
		public virtual bool OnJumpToTool(object commandObject)
		{
#if RANDYTODO
			var coreCommand = commandObject as Command;
			if(coreCommand != null)
			{
				var tool = XmlUtils.GetMandatoryAttributeValue(coreCommand.Parameters[0], "tool");
				if(tool != AreaServices.PublicationsEditMachineName)
					return false;

				var commands = new List<string>
										{
											"AboutToFollowLink",
											"FollowLink"
										};
				var parms = new List<object>
										{
											null,
											new FwLinkArgs(tool, Guid.Empty)
										};
				Publisher.Publish(commands, parms);
				return true;
			}
#endif
			return false;
		}

		/// <summary>
		/// All publications which the given configuration apply to will be placed in the inConfig collection.
		/// All publications which the configuration does not apply to will be placed in the notInConfig collection.
		/// </summary>
		internal void SplitPublicationsByConfiguration(ILcmOwningSequence<ICmPossibility> publications,
																	  string configurationPath,
																	  out List<string> inConfig,
																	  out List<string> notInConfig)
		{
			inConfig = new List<string>();
			notInConfig = new List<string>();
			var config = new DictionaryConfigurationModel(configurationPath, Cache);
			foreach(var pub in publications)
			{
				var name = pub.Name.UserDefaultWritingSystem.Text;
				if(config.AllPublications || config.Publications.Contains(name))
				{
					inConfig.Add(name);
				}
				else
				{
					notInConfig.Add(name);
				}
			}
		}

		/// <summary>
		/// All the configurations which apply to the given publication will be placed in the hasPub dictionary.
		/// All that do not apply to it will be placed in the doesNotHavePub dictionary.
		/// </summary>
		internal void SplitConfigurationsByPublication(IDictionary<string, string> configurations,
																	  string publication,
																	  out IDictionary<string, string> hasPub,
																	  out IDictionary<string, string> doesNotHavePub)
		{
			hasPub = new SortedDictionary<string, string>();
			doesNotHavePub = new SortedDictionary<string, string>();
			foreach(var config in configurations)
			{
				var model = new DictionaryConfigurationModel(config.Value, Cache);
				if(model.AllPublications || publication.Equals(LanguageExplorerResources.AllEntriesPublication)
												 || model.Publications.Contains(publication))
				{
					hasPub[config.Key] = config.Value;
				}
				else
				{
					doesNotHavePub[config.Key] = config.Value;
				}
			}
		}

		internal string GetValidPublicationForConfiguration(string configuration)
		{
			var currentPub = GetCurrentPublication();
			List<string> inConfinguration;
			List<string> notInConfinguration;
			SplitPublicationsByConfiguration(Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS,
														configuration,
														out inConfinguration, out notInConfinguration);
			return inConfinguration.Contains(currentPub) ? currentPub : inConfinguration.FirstOrDefault();
		}

		internal string GetValidConfigurationForPublication(string publication)
		{
			if(publication == LanguageExplorerResources.AllEntriesPublication)
				return GetCurrentConfiguration(false);
			var currentConfig = GetCurrentConfiguration(false);
			var allConfigurations = DictionaryConfigurationUtils.GatherBuiltInAndUserConfigurations(Cache,m_configObjectName);
			IDictionary<string, string> hasPub;
			IDictionary<string, string> doesNotHavePub;
			SplitConfigurationsByPublication(allConfigurations,
														publication,
														out hasPub, out doesNotHavePub);
			// If the current configuration is already valid use it otherwise return
			// the first valid one or null if no configurations have the publication.
			if(hasPub.Values.Contains(currentConfig))
			{
				return currentConfig;
			}
			return hasPub.Count > 0 ? hasPub.First().Value : null;
		}

		/// <summary>
		/// Receives the broadcast message "PropertyChanged"
		/// </summary>
		public void OnPropertyChanged(string name)
		{
			switch (name)
			{
				case "SelectedPublication":
					var pubDecorator = PublicationDecorator;
					var validConfiguration = SetCurrentDictionaryPublicationLayout();
					UpdateContent(pubDecorator, validConfiguration);
					break;
				case "DictionaryPublicationLayout":
				case "ReversalIndexPublicationLayout":
					var currentConfig = GetCurrentConfiguration(false);
					if (name == "ReversalIndexPublicationLayout")
					{
						DictionaryConfigurationUtils.SetReversalIndexGuidBasedOnReversalIndexConfiguration(PropertyTable, Cache);
					}
					var currentPublication = GetCurrentPublication();
					var validPublication = GetValidPublicationForConfiguration(currentConfig) ?? LanguageExplorerResources.AllEntriesPublication;
					if (validPublication != currentPublication)
					{
						PropertyTable.SetProperty("SelectedPublication", validPublication, true, true);
					}
					UpdateContent(PublicationDecorator, currentConfig);
					break;
				case "ActiveListSelectedObject":
					var browser = m_mainView.NativeBrowser as GeckoWebBrowser;
					if (browser != null)
					{
						RemoveStyleFromPreviousSelectedEntryOnView(browser);
						LoadPageIfNecessary(browser);
						MyRecordList.SelectedRecordChanged(true);
						SetActiveSelectedEntryOnView(browser);
					}
					break;
				default:
					// Not sure what other properties might change, but I'm not doing anything.
					break;
			}
		}

		private void LoadPageIfNecessary(GeckoWebBrowser browser)
		{
			var currentObjectHvo = MyRecordList.CurrentObjectHvo;
			var currentObjectIndex = Array.IndexOf(PublicationDecorator.GetEntriesToPublish(PropertyTable, MyRecordList.VirtualFlid), currentObjectHvo);
			// If the current item is not to be displayed (invalid, not in this publication) just quit
			if (currentObjectIndex < 0 || browser?.Document == null)
			{
				return;
			}
			var currentPage = GetTopCurrentPageButton(browser.Document.Body);
			if (currentPage == null)
			{
				return;
			}
			var currentPageRange = new Tuple<int, int>(int.Parse(currentPage.Attributes["startIndex"].NodeValue), int.Parse(currentPage.Attributes["endIndex"].NodeValue));
			if (currentObjectIndex < currentPageRange.Item1 || currentObjectIndex > currentPageRange.Item2)
			{
				OnMasterRefresh(this); // Reload the page
			}
		}

		/// <summary>
		/// Remove the style from the previously selected entry.
		/// </summary>
		private void RemoveStyleFromPreviousSelectedEntryOnView(GeckoWebBrowser browser)
		{
			if (string.IsNullOrEmpty(m_selectedObjectID))
			{
				return;
			}
			var prevSelectedByGuid = browser.Document.GetHtmlElementById("g" + m_selectedObjectID);
			if (prevSelectedByGuid != null)
			{
				RemoveClassFromHtmlElement(prevSelectedByGuid, DictionaryConfigurationServices.CurrentSelectedEntryClass);
			}
		}

		/// <summary>
		/// Set the style attribute on the current entry to color the background.
		/// </summary>
		private void SetActiveSelectedEntryOnView(GeckoWebBrowser browser)
		{
			if (MyRecordList.CurrentObject == null)
			{
				return;
			}

			if (MyRecordList.Id == "AllReversalEntries")
			{
				var reversalentry = MyRecordList.CurrentObject as IReversalIndexEntry;
				if (reversalentry == null)
				{
					return;
				}
				var writingSystem = Cache.ServiceLocator.WritingSystemManager.Get(reversalentry.ReversalIndex.WritingSystem);
				if (writingSystem == null)
				{
					return;
				}
				var currReversalWs = writingSystem.Id;
				var currentConfig = PropertyTable.GetValue("ReversalIndexPublicationLayout", string.Empty);
				var configuration = File.Exists(currentConfig) ? new DictionaryConfigurationModel(currentConfig, Cache) : null;
				if (configuration == null || configuration.WritingSystem != currReversalWs)
				{
					var newConfig = Path.Combine(DictionaryConfigurationServices.GetProjectConfigurationDirectory(PropertyTable), writingSystem.Id + LanguageExplorerConstants.DictionaryConfigurationFileExtension);
					PropertyTable.SetProperty("ReversalIndexPublicationLayout", File.Exists(newConfig) ? newConfig : null, true, true);
				}
			}
			var currentObjectGuid = MyRecordList.CurrentObject.Guid.ToString();
			var currSelectedByGuid = browser.Document.GetHtmlElementById("g" + currentObjectGuid);
			if (currSelectedByGuid == null)
			{
				return;
			}

			// Adjust active item to be lower down on the page.
			var currElementRect = currSelectedByGuid.GetBoundingClientRect();
			var currElementTop = currElementRect.Top + browser.Window.ScrollY;
			var currElementBottom = currElementRect.Bottom + browser.Window.ScrollY;
			var yPosition = currElementTop - (browser.Height / 4);

			// Scroll only if current element is not visible on browser window
			if (currElementTop < browser.Window.ScrollY || currElementBottom > (browser.Window.ScrollY + browser.Height))
			{
				browser.Window.ScrollTo(0, yPosition);
			}

			AddClassToHtmlElement(currSelectedByGuid, DictionaryConfigurationServices.CurrentSelectedEntryClass);
			m_selectedObjectID = currentObjectGuid;
		}

		#region Add/Remove GeckoHtmlElement Class

		/// <summary>
		/// Adds 'classToAdd' to the class attribute of 'element', preserving any existing classes.
		/// Changes nothing if 'classToAdd' is already present.
		/// </summary>
		private static void AddClassToHtmlElement(GeckoHtmlElement element, string classToAdd)
		{
			var classList = element.ClassName.Split(' ');
			if (classList.Length == 0)
			{
				element.ClassName = classToAdd;
				return;
			}
			if (classList.Contains(classToAdd))
			{
				return;
			}
			element.ClassName += " " + classToAdd;
		}

		/// <summary>
		/// Removes 'classToRemove' from the class attribute, preserving any other existing classes.
		/// Quietly does nothing if 'classToRemove' is not found.
		/// </summary>
		private static void RemoveClassFromHtmlElement(GeckoHtmlElement element, string classToRemove)
		{
			var classList = new List<string>();
			classList.AddRange(element.ClassName.Split(' '));
			classList.Remove(classToRemove);
			element.ClassName = string.Join(" ", classList);
		}

		#endregion

		public void OnMasterRefresh(object sender)
		{
			var currentConfig = GetCurrentConfiguration(false);
			var currentPublication = GetCurrentPublication();
			var validPublication = GetValidPublicationForConfiguration(currentConfig) ?? LanguageExplorerResources.AllEntriesPublication;
			if (currentPublication != LanguageExplorerResources.AllEntriesPublication && currentPublication != validPublication)
			{
				PropertyTable.SetProperty("SelectedPublication", validPublication, true, true);
			}
			UpdateContent(PublicationDecorator, currentConfig);
		}

#if RANDYTODO
		public virtual bool OnDisplayShowAllEntries(object commandObject, ref UIItemDisplayProperties display)
		{
			var pubName = GetCurrentPublication();
			display.Enabled = true;
			display.Checked = (LanguageExplorerResources.AllEntriesPublication == pubName);
			return true;
		}
#endif

		/// <summary>
		/// Implements the command that just does Find, without Replace.
		/// </summary>
		public bool OnFindAndReplaceText(object argument)
		{
			var geckoBrowser = m_mainView?.NativeBrowser as GeckoWebBrowser;
			geckoBrowser?.Window.Find(string.Empty, false, false, true, false, true, true);
			return true;
		}

#if RANDYTODO
		/// <summary>
		/// Enables the command that just does Find, without Replace.
		/// </summary>
		public virtual bool OnDisplayFindAndReplaceText(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = display.Visible = true;
			return true; //we've handled this
		}
#endif

		public bool OnShowAllEntries(object args)
		{
			PropertyTable.SetProperty("SelectedPublication", LanguageExplorerResources.AllEntriesPublication, true, true);
			return true;
		}

		private void UpdateContent(DictionaryPublicationDecorator publicationDecorator, string configurationFile)
		{
			SetInfoBarText();
			var htmlErrorMessage = LexiconResources.ksErrorDisplayingPublication;
			if (string.IsNullOrEmpty(configurationFile))
			{
				htmlErrorMessage = LexiconResources.NoConfigsMatchPub;
			}
			else
			{
				using (new WaitCursor(ParentForm))
				using (var progressDlg = new SIL.FieldWorks.Common.Controls.ProgressDialogWithTask(ParentForm))
				{
					progressDlg.AllowCancel = true;
					progressDlg.CancelLabelText = LexiconResources.ksCancelingPublicationLabel;
					progressDlg.Title = LexiconResources.ksPreparingPublicationDisplay;
					var xhtmlPath = progressDlg.RunTask(true, SaveConfiguredXhtmlAndDisplay, publicationDecorator, configurationFile) as string;
					if (xhtmlPath != null)
					{
						if (progressDlg.IsCanceling)
						{
							Publisher.Publish("SetToolFromName", AreaServices.LexiconEditMachineName);
						}
						else
						{
							m_mainView.Url = new Uri(xhtmlPath);
							m_mainView.Refresh(WebBrowserRefreshOption.Completely);
						}
						return;
					}
				}
			}
			m_mainView.DocumentText = $"<html><body>{htmlErrorMessage}</body></html>";
		}

		private object SaveConfiguredXhtmlAndDisplay(IThreadedProgress progress, object[] args)
		{
			if (args.Length != 2)
			{
				return null;
			}
			var publicationDecorator = (DictionaryPublicationDecorator)args[0];
			var configurationFile = (string)args[1];
			if (progress != null)
			{
				progress.Message = LexiconResources.ksObtainingEntriesToDisplay;
			}
			var configuration = new DictionaryConfigurationModel(configurationFile, Cache);
			publicationDecorator.Refresh();
			var entriesToPublish = publicationDecorator.GetEntriesToPublish(PropertyTable, MyRecordList.VirtualFlid);
#if DEBUG
			var start = DateTime.Now;
#endif
			if (progress != null)
			{
				progress.Minimum = 0;
				const int entryCount = ConfiguredXHTMLGenerator.EntriesPerPage;
				progress.Maximum = entryCount + 1 + entryCount / 100;
				progress.Position++;
			}
			var xhtmlPath = ConfiguredXHTMLGenerator.SavePreviewHtmlWithStyles(entriesToPublish, publicationDecorator, configuration, PropertyTable, Cache, MyRecordList, progress);
#if DEBUG
			var end = DateTime.Now;
			Debug.WriteLine($"saving xhtml/css took {end - start}");
#endif
			return xhtmlPath;
		}

		public string GetCurrentPublication()
		{
			// Returns the current publication and use '$$all_entries$$' if none has yet been set
			return PropertyTable.GetValue("SelectedPublication", LanguageExplorerResources.AllEntriesPublication);
		}

		internal string GetCurrentConfiguration(bool fUpdate)
		{
			return DictionaryConfigurationServices.GetCurrentConfiguration(PropertyTable, fUpdate);
		}

		private void SetCurrentConfiguration(string currentConfig, bool fUpdate)
		{
			DictionaryConfigurationServices.SetCurrentConfiguration(PropertyTable, currentConfig, fUpdate);
		}

		public DictionaryPublicationDecorator PublicationDecorator
		{
			get
			{
				if(m_pubDecorator == null)
				{
					m_pubDecorator = new DictionaryPublicationDecorator(Cache, MyRecordList.VirtualListPublisher, MyRecordList.VirtualFlid);
				}
				var pubName = GetCurrentPublication();
				if(LanguageExplorerResources.AllEntriesPublication == pubName)
				{
					// A null publication means show everything
					m_pubDecorator.Publication = null;
				}
				else
				{
					// look up the publication object

					var pub = Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.FirstOrDefault(item => item.Name.UserDefaultWritingSystem.Text == pubName);
					if (pub != null && pub != m_pubDecorator.Publication)
					{
						// change the publication if it is different from the current one
						m_pubDecorator.Publication = pub;
					}
				}
				return m_pubDecorator;
			}
		}

		private void SetConfigViewTitle()
		{
			var maxViewWidth = Width/2 - kSpaceForMenuButton;
			var allConfigurations = DictionaryConfigurationUtils.GatherBuiltInAndUserConfigurations(Cache, m_configObjectName);
			var currentConfig = GetCurrentConfiguration(false);
			var curViewName = allConfigurations.ContainsValue(currentConfig)
				? allConfigurations.First(item => item.Value == currentConfig).Key
				: allConfigurations.First().Key;
			// Limit length of View title to remaining available width
			curViewName = TrimToMaxPixelWidth(Math.Max(2, maxViewWidth), curViewName);
			var isReversalIndex = DictionaryConfigurationServices.GetDictionaryConfigurationType(PropertyTable) == LanguageExplorerResources.ReversalIndex;
			if (!isReversalIndex)
			{
				ResetSpacer(maxViewWidth, curViewName);
			}
			else
			{
				((IPaneBar)m_informationBar).Text = curViewName;
		}
		}

		/// <summary>
		/// Sets the text on the info bar that appears above the main view panel.
		/// The xml configuration file that this view is based on can configure the text in several ways.
		/// </summary>
		protected override void SetInfoBarText()
		{
			if (m_informationBar == null)
			{
				return;
			}
			var titleStr = GetBaseTitleStringFromConfig();
			if (titleStr == string.Empty)
			{
				base.SetInfoBarText();
				return;
			}
			// Set the configuration part of the title
			SetConfigViewTitle();
			//Set the publication part of the title
			var pubNameTitlePiece = GetCurrentPublication();
			if (pubNameTitlePiece == LanguageExplorerResources.AllEntriesPublication)
			{
				pubNameTitlePiece = AreaResources.ksAllEntries;
			}
			titleStr = pubNameTitlePiece + " " + titleStr;
			var isReversalIndex = DictionaryConfigurationServices.GetDictionaryConfigurationType(PropertyTable) == LanguageExplorerResources.ReversalIndex;
			if (isReversalIndex)
			{
				var maxViewWidth = Width / 2 - kSpaceForMenuButton;
				// Limit length of View title to remaining available width
				titleStr = TrimToMaxPixelWidth(Math.Max(2, maxViewWidth), titleStr);
				ResetSpacer(maxViewWidth, titleStr);
			}
			else
			{
				((IPaneBar)m_informationBar).Text = titleStr;
			}
		}

		private const int kSpaceForMenuButton = 26;


		protected override void OnSizeChanged(EventArgs e)
		{
			if (!m_fullyInitialized)
			{
				return;
			}
			base.OnSizeChanged(e);
			SetInfoBarText();
		}

		public string FindTabHelpId { get; private set; }
	}
}
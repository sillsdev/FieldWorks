// Copyright (c) 2014-2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using Gecko;
using Gecko.DOM;
using Palaso.UI.WindowsForms.HtmlBrowser;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// This class handles the display of configured xhtml for a particular publication in a dynamically loadable XWorksView.
	/// </summary>
	internal class XhtmlDocView : XWorksViewBase, IFindAndReplaceContext, IPostLayoutInit
	{
		private XWebBrowser m_mainView;
		private DictionaryPublicationDecorator m_pubDecorator;
		private string m_selectedObjectID = string.Empty;
		internal string m_configObjectName;

		public override void Init(Mediator mediator, XmlNode configurationParameters)
		{
			m_mediator = mediator;
			m_configurationParameters = configurationParameters;
			m_mainView = new XWebBrowser(XWebBrowser.BrowserType.GeckoFx);
			m_mainView.Dock = DockStyle.Fill;
			m_mainView.Location = new Point(0, 0);
			m_mainView.IsWebBrowserContextMenuEnabled = false;
			ReadParameters();
			// Use update helper to help with optimizations and special cases for list loading
			using(var luh = new RecordClerk.ListUpdateHelper(Clerk, Clerk.ListLoadingSuppressed))
			{
				m_mediator.AddColleague(this);
				Clerk.UpdateOwningObjectIfNeeded();
			}
			Controls.Add(m_mainView);

			var browser = m_mainView.NativeBrowser as GeckoWebBrowser;
			if (browser != null)
			{
				var clerk = XmlUtils.GetOptionalAttributeValue(configurationParameters, "clerk");
				if (clerk == "entries" || clerk == "AllReversalEntries")
				{
					browser.DomClick += OnDomClick;
					browser.DomKeyPress += OnDomKeyPress;
					browser.DocumentCompleted += OnDocumentCompleted;
					browser.DomMouseScroll += OnMouseWheel;
				}
			}
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
							Clerk.JumpToRecord(PublicationDecorator.GetEntriesToPublish(m_mediator, Clerk.VirtualFlid)[itemIndex]);
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
							Clerk.JumpToRecord(PublicationDecorator.GetEntriesToPublish(m_mediator, Clerk.VirtualFlid)[itemIndex]);
						}
					}
					break;
				}
				default:
					break;
			}
		}

		/// <summary>
		/// Handle a mouse click in the web browser displaying the xhtml.
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule", Justification = "element does NOT need to be disposed locally!")]
		private void OnDomClick(object sender, DomMouseEventArgs e)
		{
			CloseContextMenuIfOpen();
			var browser = m_mainView.NativeBrowser as GeckoWebBrowser;
			if (browser == null)
				return;
			var element = browser.DomDocument.ElementFromPoint(e.ClientX, e.ClientY);
			if (element == null || element.TagName == "html")
				return;
			if (e.Button == GeckoMouseButton.Left)
			{
				if (HandleClickOnPageButton(Clerk, element))
				{
					return;
				}
				// Handle button clicks or select the entry represented by the current element.
				HandleDomLeftClick(Clerk, e, element);
			}
			else if (e.Button == GeckoMouseButton.Right)
			{
				HandleDomRightClick(browser, e, element, m_mediator, m_configObjectName);
			}
		}

		/// <summary>
		/// Set the style attribute on the current entry to color the background after document completed.
		/// </summary>
		private void OnDocumentCompleted(object sender, EventArgs e)
		{
			var browser = m_mainView.NativeBrowser as GeckoWebBrowser;
			if (browser != null)
			{
				CloseContextMenuIfOpen();
				SetActiveSelectedEntryOnView(browser);
			}
		}

		/// <summary>
		/// Handle the user left clicking on the document view by jumping to an entry, playing a media element, or adjusting the view
		/// </summary>
		/// <remarks>internal so that it can be re-used by the XhtmlRecordDocView</remarks>
		internal static void HandleDomLeftClick(RecordClerk clerk, DomMouseEventArgs e, GeckoElement element)
		{
			GeckoElement dummy;
			var topLevelGuid = GetHrefFromGeckoDomElement(element);
			if (topLevelGuid == Guid.Empty)
				GetClassListFromGeckoElement(element, out topLevelGuid, out dummy);
			if (topLevelGuid != Guid.Empty)
			{
				var currentObj = clerk.CurrentObject;
				if (currentObj != null && currentObj.Guid == topLevelGuid)
				{
					// don't need to jump, we're already here...
					// unless this is a video link
					if (element is GeckoAnchorElement)
						return; // don't handle the click; gecko will jump to the link
				}
				else
				{
					clerk.JumpToRecord(topLevelGuid);
				}
			}
			e.Handled = true;
		}

		private void AddMoreEntriesToPage(bool goingUp, GeckoWebBrowser browser)
		{
			var browserElement = browser.Document.Body;
			var entriesToPublish = PublicationDecorator.GetEntriesToPublish(m_mediator, Clerk.VirtualFlid);
			// Right-to-Left for the overall layout is determined by Dictionary-Normal
			var dictionaryNormalStyle = new ExportStyleInfo(FontHeightAdjuster.StyleSheetFromMediator(m_mediator).Styles["Dictionary-Normal"]);
			var isNormalRightToLeft = dictionaryNormalStyle.DirectionIsRightToLeft == TriStateBool.triTrue; // default is LTR
			// Get the current page
			if (goingUp)
			{
				// Use the up/down info to select the adjacentPage
				Tuple<int, int> newCurPageRange;
				Tuple<int, int> newAdjPageRange;
				// Gecko xpath seems to be sensitive to namespaces, using * instead of span helps
				var currentPageButton = GetTopCurrentPageButton(browserElement);
				if(currentPageButton == null)
					return;
				var adjacentPageButton = (GeckoHtmlElement)currentPageButton.PreviousSibling;
				if (adjacentPageButton == null)
					return;
				var oldCurPageRange = new Tuple<int, int>(int.Parse(currentPageButton.Attributes["startIndex"].NodeValue), int.Parse(currentPageButton.Attributes["endIndex"].NodeValue));
				var oldAdjPageRange = new Tuple<int, int>(int.Parse(adjacentPageButton.Attributes["startIndex"].NodeValue), int.Parse(adjacentPageButton.Attributes["endIndex"].NodeValue));
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, false, false, "", isNormalRightToLeft);
				var entries = ConfiguredXHTMLGenerator.GenerateNextFewEntries(PublicationDecorator, entriesToPublish, GetCurrentConfiguration(false), settings, oldCurPageRange,
					oldAdjPageRange, ConfiguredXHTMLGenerator.EntriesToAddCount, out newCurPageRange, out newAdjPageRange);
				// Load entries above
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
				if (currentPageButton == null)
					throw new ArgumentException(@"No page buttons found in the document element is a part of", "element");
				var adjPage = (GeckoHtmlElement)currentPageButton.NextSibling;
				if (adjPage == null)
					return;
				var currentPageRange = new Tuple<int, int>(int.Parse(currentPageButton.Attributes["startIndex"].NodeValue), int.Parse(currentPageButton.Attributes["endIndex"].NodeValue));
				var adjacentPageRange = new Tuple<int, int>(int.Parse(adjPage.Attributes["startIndex"].NodeValue), int.Parse(adjPage.Attributes["endIndex"].NodeValue));
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, false, false, "", isNormalRightToLeft);
				var entries = ConfiguredXHTMLGenerator.GenerateNextFewEntries(PublicationDecorator, entriesToPublish, GetCurrentConfiguration(false), settings, currentPageRange,
					adjacentPageRange, ConfiguredXHTMLGenerator.EntriesToAddCount, out newCurrentPageRange, out newAdjPageRange);
				// Load entries above
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
			GeckoHtmlElement currentPageTop = GetTopCurrentPageButton(pageButtonElement);
			var adjPageTop = goingUp ? (GeckoHtmlElement)currentPageTop.PreviousSibling : (GeckoHtmlElement)currentPageTop.NextSibling;
			GeckoHtmlElement currentPageBottom = GetBottomCurrentPageButton(pageButtonElement);
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

		private static bool HandleClickOnPageButton(RecordClerk clerk, GeckoElement element)
		{
			if (element.HasAttribute("class") && element.Attributes["class"].NodeValue.Equals("pagebutton"))
			{
				if(!element.HasAttribute("firstEntryGuid"))
					throw new ArgumentException(@"The element passed to this method should have a firstEntryGuid.", "element");
				var firstEntryOnPage = element.Attributes["firstEntryGuid"].NodeValue;
				clerk.JumpToRecord(new Guid(firstEntryOnPage));
				return true;
			}
			return false;
		}

		/// <summary>
		/// Pop up a menu to allow the user to start the document configuration dialog, and
		/// start the dialog at the configuration node indicated by the current element.
		/// </summary>
		/// <remarks>
		/// This is static so that the method can be shared with XhtmlRecordDocView.
		/// </remarks>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "ToolStripMenuItems get added to m_contextMenu.Items; ContextMenuStrip is disposed in DisposeContextMenu()")]
		internal static void HandleDomRightClick(GeckoWebBrowser browser, DomMouseEventArgs e,
			GeckoElement element, Mediator mediator, string configObjectName)
		{
			Guid topLevelGuid;
			GeckoElement entryElement;
			var classList = GetClassListFromGeckoElement(element, out topLevelGuid, out entryElement);
			var label = string.Format(xWorksStrings.ksConfigure, configObjectName);
			s_contextMenu = new ContextMenuStrip();
			var item = new ToolStripMenuItem(label);
			s_contextMenu.Items.Add(item);
			item.Click += RunConfigureDialogAt;
			item.Tag = new object[] { mediator, classList, topLevelGuid };
			if (e.CtrlKey) // show hidden menu item for tech support
			{
				item = new ToolStripMenuItem(xWorksStrings.ksInspect);
				s_contextMenu.Items.Add(item);
				item.Click += RunDiagnosticsDialogAt;
				item.Tag = new object[] { mediator, entryElement, topLevelGuid };
			}
			s_contextMenu.Show(browser, new Point(e.ClientX, e.ClientY));
			s_contextMenu.Closed += m_contextMenu_Closed;
			e.Handled = true;
		}

		/// <summary>
		/// Returns the class hierarchy for a GeckoElement
		/// </summary>
		/// <remarks>LT-17213 Internal for use in DictionaryConfigurationDlg</remarks>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "elem does NOT need to be disposed locally!")]
		internal static List<string> GetClassListFromGeckoElement(GeckoElement element, out Guid topLevelGuid, out GeckoElement entryElement)
		{
			topLevelGuid = Guid.Empty;
			entryElement = element;
			var classList = new List<string>();
			if (entryElement.TagName == "body" || entryElement.TagName == "html")
				return classList;
			for (; entryElement != null; entryElement = entryElement.ParentElement)
			{
				var className = entryElement.GetAttribute("class");
				if (string.IsNullOrEmpty(className))
					continue;
				if (className == "letHead")
					break;
				classList.Insert(0, className);
				if (entryElement.TagName == "div" && entryElement.ParentElement.TagName == "body")
				{
					topLevelGuid = GetGuidFromGeckoDomElement(entryElement);
					break; // we have the element we want; continuing to loop will get its parent instead
				}
			}
			return classList;
		}

		internal static Guid GetHrefFromGeckoDomElement(GeckoElement element)
		{
			if (!element.HasAttribute("href"))
				return Guid.Empty;

			var hrefVal = element.GetAttribute("href");
			return !hrefVal.StartsWith("#g") ? Guid.Empty : new Guid(hrefVal.Substring(2));
		}

		private static Guid GetGuidFromGeckoDomElement(GeckoElement element)
		{
			if (!element.HasAttribute("id"))
				return Guid.Empty;

			var idVal = element.GetAttribute("id");
			return !idVal.StartsWith("g") ? Guid.Empty : new Guid(idVal.Substring(1));
		}

		/// <summary>
		/// Close and delete the context menu if it exists.  This appears to be needed (at least on Linux)
		/// if the user clicks somewhere in the browser other than the menu.
		/// </summary>
		internal static void CloseContextMenuIfOpen()
		{
			if (s_contextMenu != null)
			{
				s_contextMenu.Close();
				DisposeContextMenu(null, null);
			}
		}

		private static void m_contextMenu_Closed(object sender, ToolStripDropDownClosedEventArgs e)
		{
			Application.Idle += DisposeContextMenu;
		}

		private static void DisposeContextMenu(object sender, EventArgs e)
		{
			Application.Idle -= DisposeContextMenu;
			if (s_contextMenu != null)
			{
				s_contextMenu.Dispose();
				s_contextMenu = null;
			}
		}

		// Context menu exists just for one invocation (until idle).
		private static ContextMenuStrip s_contextMenu;

		private static void RunConfigureDialogAt(object sender, EventArgs e)
		{
			var item = (ToolStripMenuItem)sender;
			var tagObjects = (object[])item.Tag;
			var mediator = (Mediator)tagObjects[0];
			var classList = (List<string>)tagObjects[1];
			var guid = (Guid)tagObjects[2];
			bool refreshNeeded;
			using (var dlg = new DictionaryConfigurationDlg(mediator))
			{
				var cache = mediator.PropertyTable.GetValue("cache") as FdoCache;
				var clerk = mediator.PropertyTable.GetValue("ActiveClerk", null) as RecordClerk;
				ICmObject current = null;
				if (guid != Guid.Empty && cache != null)
					current = cache.ServiceLocator.GetObject(guid);
				else if (clerk != null)
					current = clerk.CurrentObject;
				var controller = new DictionaryConfigurationController(dlg, mediator, current);
				controller.SetStartingNode(classList);
				dlg.Text = String.Format(xWorksStrings.ConfigureTitle, DictionaryConfigurationListener.GetDictionaryConfigurationType(mediator));
				dlg.HelpTopic = DictionaryConfigurationListener.GetConfigDialogHelpTopic(mediator);
				dlg.ShowDialog(mediator.PropertyTable.GetValue("window") as IWin32Window);
				refreshNeeded = controller.MasterRefreshRequired;
			}
			if (refreshNeeded)
				mediator.SendMessage("MasterRefresh", null);
		}

		private static void RunDiagnosticsDialogAt(object sender, EventArgs e)
		{
			var item = (ToolStripMenuItem)sender;
			var tagObjects = (object[])item.Tag;
			var mediator = (Mediator)tagObjects[0];
			var element = (GeckoElement)tagObjects[1];
			var guid = (Guid)tagObjects[2];
			using (var dlg = new XmlDiagnosticsDlg(element, guid))
			{
				dlg.ShowDialog(mediator.PropertyTable.GetValue("window") as IWin32Window);
			}
		}

		public override int Priority
		{
			get { return (int)ColleaguePriority.High; }
		}

		/// <summary>
		/// We wait until containing controls are laid out to try to set the content
		/// </summary>
		public void PostLayoutInit()
		{
			// Tell the Clerk it is active so it will update the list of entries. Pass false as we have no toolbar to update.
			Clerk.ActivateUI(false);
			// Update the entry list if necessary
			if(!Clerk.ListLoadingSuppressed && Clerk.RequestedLoadWhileSuppressed)
			{
				Clerk.UpdateList(true, true);
			}
			// Grab the selected publication and make sure that we grab a valid configuration for it.
			// In some cases (e.g where a user reset their local settings) the stored configuration may no longer
			// exist on disk.
			var validConfiguration = SetCurrentDictionaryPublicationLayout();
			UpdateContent(PublicationDecorator, validConfiguration);
		}

		private string SetCurrentDictionaryPublicationLayout()
		{
			var pubName = GetCurrentPublication();
			var currentConfiguration = GetCurrentConfiguration(false);
			var validConfiguration = GetValidConfigurationForPublication(pubName);
			if(validConfiguration != currentConfiguration)
				SetCurrentConfiguration(validConfiguration, false);
			return validConfiguration;
		}

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

		/// <summary>
		/// Enable the 'File Print...' menu option for the Dictionary view
		/// </summary>
		public bool OnDisplayPrint(object parameter, UIItemDisplayProperties display)
		{
			display.Enabled = display.Visible = true;
			return true;
		}

		/// <summary>
		/// Handle the 'File Print...' menu item click (defined in the Lexicon areaConfiguration.xml)
		/// </summary>
		/// <param name="commandObject"></param>
		/// <returns></returns>
		public bool OnPrint(object commandObject)
		{
			CloseContextMenuIfOpen(); // not sure if this is necessary or not
			PrintPage(m_mainView);
			return true;
		}

		internal static void PrintPage(XWebBrowser browser)
		{
			var geckoBrowser = browser.NativeBrowser as GeckoWebBrowser;
			if (geckoBrowser == null)
				return;
			geckoBrowser.Window.Print();
		}

		/// <summary>
		/// Read in the parameters to determine which sequence/collection we are editing.
		/// </summary>
		protected override void ReadParameters()
		{
			base.ReadParameters();
			var backColorName = XmlUtils.GetOptionalAttributeValue(m_configurationParameters,
				"backColor", "Window");
			BackColor = Color.FromName(backColorName);
			m_configObjectName = XmlUtils.GetOptionalAttributeValue(m_configurationParameters, "configureObjectName", null);
		}

		/// <summary>
		/// We aren't using the record clerk for this view so we override this method to do nothing.
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
			var coreCommand = commandObject as Command;
			if(coreCommand != null)
			{
				var tool = XmlUtils.GetManditoryAttributeValue(coreCommand.Parameters[0], "tool");
				if(tool != "publicationsEdit")
					return false;

				var fwLink = new FwLinkArgs(tool, Guid.Empty);
				m_mediator.PostMessage("FollowLink", fwLink);
				return true;
			}
			return false;
		}

		/// <summary>
		/// All publications which the given configuration apply to will be placed in the inConfig collection.
		/// All publications which the configuration does not apply to will be placed in the notInConfig collection.
		/// </summary>
		internal void SplitPublicationsByConfiguration(IFdoOwningSequence<ICmPossibility> publications,
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
				if(model.AllPublications || publication.Equals(xWorksStrings.AllEntriesPublication)
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
			if(publication == xWorksStrings.AllEntriesPublication)
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
						DictionaryConfigurationUtils.SetReversalIndexGuidBasedOnReversalIndexConfiguration(m_mediator, Cache);
					var currentPublication = GetCurrentPublication();
					var validPublication = GetValidPublicationForConfiguration(currentConfig) ?? xWorksStrings.AllEntriesPublication;
					if (validPublication != currentPublication)
					{
						m_mediator.PropertyTable.SetProperty("SelectedPublication", validPublication, false);
					}
					UpdateContent(PublicationDecorator, currentConfig);
					break;
				case "ActiveClerkSelectedObject":
					var browser = m_mainView.NativeBrowser as GeckoWebBrowser;
					if (browser != null)
					{
						RemoveStyleFromPreviousSelectedEntryOnView(browser);
						LoadPageIfNecessary(browser);
						Clerk.SelectedRecordChanged(true);
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
			var currentObjectHvo = Clerk.CurrentObjectHvo;
			var currentObjectIndex = Array.IndexOf(PublicationDecorator.GetEntriesToPublish(m_mediator, Clerk.VirtualFlid), currentObjectHvo);
			if (currentObjectIndex < 0 || browser == null || browser.Document == null) // If the current item is not to be displayed (invalid, not in this publication) just quit
				return;
			var currentPage = GetTopCurrentPageButton(browser.Document.Body);
			if (currentPage == null)
				return;
			var currentPageRange = new Tuple<int, int>(int.Parse(currentPage.Attributes["startIndex"].NodeValue), int.Parse(currentPage.Attributes["endIndex"].NodeValue));
			if (currentObjectIndex < currentPageRange.Item1 || currentObjectIndex > currentPageRange.Item2)
			{
				OnMasterRefresh(this); // Reload the page
			}
		}

		/// <summary>
		/// Remove the style from the previously selected entry.
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule", Justification = "GeckoHtmlElement does NOT need to be disposed locally!")]
		private void RemoveStyleFromPreviousSelectedEntryOnView(GeckoWebBrowser browser)
		{
			if (string.IsNullOrEmpty(m_selectedObjectID))
			{
				return;
			}
			var prevSelectedByGuid = browser.Document.GetHtmlElementById("g" + m_selectedObjectID);
			if (prevSelectedByGuid != null)
			{
				prevSelectedByGuid.RemoveAttribute("style");
			}
		}

		/// <summary>
		/// Set the style attribute on the current entry to color the background.
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule", Justification = "GeckoHtmlElement does NOT need to be disposed locally!")]
		private void SetActiveSelectedEntryOnView(GeckoWebBrowser browser)
		{
			if (Clerk.CurrentObject == null)
				return;
			var currentObjectGuid = Clerk.CurrentObject.Guid.ToString();
			var currSelectedByGuid = browser.Document.GetHtmlElementById("g" + currentObjectGuid);
			if (currSelectedByGuid != null)
			{
				// Adjust active item to be lower down on the page.
				var currElementRect = currSelectedByGuid.GetBoundingClientRect();
				var currElementTop = currElementRect.Top + browser.Window.ScrollY;
				var currElementBottom = currElementRect.Bottom + browser.Window.ScrollY;
				var yPosition = currElementTop - (browser.Height / 4);

				// Scroll only if current element is not visible on browser window
				if (currElementTop < browser.Window.ScrollY || currElementBottom > (browser.Window.ScrollY + browser.Height))
					browser.Window.ScrollTo(0, yPosition);

				currSelectedByGuid.SetAttribute("style", "background-color:LightYellow");
				m_selectedObjectID = currentObjectGuid;
			}
		}

		/// <summary>
		/// Method which set the current writing system when selected in ConfigureReversalIndexDialog
		/// </summary>
		private void SetReversalIndexOnPropertyDlg() // REVIEW (Hasso) 2016.01: this seems to sabotage whatever is selected in the Config dialog
		{
			DictionaryConfigurationUtils.SetReversalIndexGuidBasedOnReversalIndexConfiguration(m_mediator, Cache);
		}

		public void OnMasterRefresh(object sender)
		{
			var currentConfig = GetCurrentConfiguration(false);
			var currentPublication = GetCurrentPublication();
			var validPublication = GetValidPublicationForConfiguration(currentConfig) ?? xWorksStrings.AllEntriesPublication;
			if (currentPublication != xWorksStrings.AllEntriesPublication && currentPublication != validPublication)
			{
				m_mediator.PropertyTable.SetProperty("SelectedPublication", validPublication, true);
			}
			UpdateContent(PublicationDecorator, currentConfig);
		}

		public virtual bool OnDisplayShowAllEntries(object commandObject, ref UIItemDisplayProperties display)
		{
			var pubName = GetCurrentPublication();
			display.Enabled = true;
			display.Checked = (xWorksStrings.AllEntriesPublication == pubName);
			return true;
		}

		/// <summary>
		/// Implements the command that just does Find, without Replace.
		/// </summary>
		public bool OnFindAndReplaceText(object argument)
		{
			using (var findDlg = new BasicFindDialog())
			{
				findDlg.FindNext += FindDlgFindNextHandler;
				findDlg.ShowDialog(this);
			}
			return true;
		}

		void FindDlgFindNextHandler(object sender, IBasicFindView view)
		{
			if (m_mainView != null)
			{
				var geckoBrowser = m_mainView.NativeBrowser as GeckoWebBrowser;
				var field = typeof(GeckoWebBrowser).GetField("WebBrowser", BindingFlags.Instance | BindingFlags.NonPublic);
				nsIWebBrowser browser = (nsIWebBrowser)field.GetValue(geckoBrowser);
				var browserFind = Xpcom.QueryInterface<nsIWebBrowserFind>(browser);
				browserFind.SetSearchStringAttribute(Icu.Normalize(view.SearchText, Icu.UNormalizationMode.UNORM_NFD));
				try
				{
					browserFind.SetWrapFindAttribute(true);
					browserFind.FindNext();
				}
				catch (Exception e)
				{
					view.StatusText = e.Message;
				}
			}
		}

		/// <summary>
		/// Enables the command that just does Find, without Replace.
		/// </summary>
		public virtual bool OnDisplayFindAndReplaceText(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = display.Visible = true;
			return true; //we've handled this
		}

		public bool OnShowAllEntries(object args)
		{
			m_mediator.PropertyTable.SetProperty("SelectedPublication", xWorksStrings.AllEntriesPublication);
			return true;
		}

		private void UpdateContent(DictionaryPublicationDecorator publicationDecorator, string configurationFile)
		{
			SetInfoBarText();
			var htmlErrorMessage = xWorksStrings.ksErrorDisplayingPublication;
			if (String.IsNullOrEmpty(configurationFile))
			{
				htmlErrorMessage = xWorksStrings.NoConfigsMatchPub;
			}
			else
			{
				using (new WaitCursor(ParentForm))
				using (var progressDlg = new Common.Controls.ProgressDialogWithTask(this.ParentForm))
				{
					progressDlg.AllowCancel = true;
					progressDlg.CancelLabelText = xWorksStrings.ksCancelingPublicationLabel;
					progressDlg.Title = xWorksStrings.ksPreparingPublicationDisplay;
					var xhtmlPath = progressDlg.RunTask(true, SaveConfiguredXhtmlAndDisplay, publicationDecorator, configurationFile) as string;
					if (xhtmlPath != null)
					{
						if (progressDlg.IsCanceling)
						{
							m_mediator.SendMessage("SetToolFromName", "lexiconEdit");
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
			m_mainView.DocumentText = String.Format("<html><body>{0}</body></html>", htmlErrorMessage);
		}

		private object SaveConfiguredXhtmlAndDisplay(IThreadedProgress progress, object[] args)
		{
			if (args.Length != 2)
				return null;
			var publicationDecorator = (DictionaryPublicationDecorator)args[0];
			var configurationFile = (string)args[1];
			if (progress != null)
				progress.Message = xWorksStrings.ksObtainingEntriesToDisplay;
			var configuration = new DictionaryConfigurationModel(configurationFile, Cache);
			publicationDecorator.Refresh();
			var entriesToPublish = publicationDecorator.GetEntriesToPublish(m_mediator, Clerk.VirtualFlid);
			var start = DateTime.Now;
			if (progress != null)
			{
				progress.Minimum = 0;
				var entryCount = ConfiguredXHTMLGenerator.EntriesPerPage;
				progress.Maximum = entryCount + 1 + entryCount / 100;
				progress.Position++;
			}
			var xhtmlPath = ConfiguredXHTMLGenerator.SavePreviewHtmlWithStyles(entriesToPublish, publicationDecorator, configuration, m_mediator, progress);
			var end = DateTime.Now;
			System.Diagnostics.Debug.WriteLine(string.Format("saving xhtml/css took {0}", end - start));
			return xhtmlPath;
		}

		/// <summary>
		/// Interpreting the xhtml, gecko doesn't load css files with a # character in it.  (probably because it carries meaning in a URL)
		/// It's probably safe to assume that : and ? characters would also cause problems.
		/// </summary>
		public static string MakeFilenameSafeForHtml(string name)
		{
			if (name == null)
				return String.Empty;
			return name.Replace('#', '-').Replace('?', '-').Replace(':', '-');
		}

		private string GetCurrentPublication()
		{
			// Returns the current publication and use '$$all_entries$$' if none has yet been set
			return m_mediator.PropertyTable.GetStringProperty("SelectedPublication",
																			  xWorksStrings.AllEntriesPublication);
		}

		private string GetCurrentConfiguration(bool fUpdate)
		{
			return DictionaryConfigurationListener.GetCurrentConfiguration(m_mediator, fUpdate);
		}

		private void SetCurrentConfiguration(string currentConfig, bool fUpdate)
		{
			DictionaryConfigurationListener.SetCurrentConfiguration(m_mediator, currentConfig, fUpdate);
		}

		public DictionaryPublicationDecorator PublicationDecorator
		{
			get
			{
				if(m_pubDecorator == null)
				{
					m_pubDecorator = new DictionaryPublicationDecorator(Cache, Clerk.VirtualListPublisher, Clerk.VirtualFlid);
				}
				var pubName = GetCurrentPublication();
				if(xWorksStrings.AllEntriesPublication == pubName)
				{
					// A null publication means show everything
					m_pubDecorator.Publication = null;
				}
				else
				{
					// look up the publication object

					var pub = (from item in Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS
									where item.Name.UserDefaultWritingSystem.Text == pubName
									select item).FirstOrDefault();
					if(pub != null && pub != m_pubDecorator.Publication)
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
			string curViewName;
			var currentConfig = GetCurrentConfiguration(false);
			if(allConfigurations.ContainsValue(currentConfig))
			{
				curViewName = allConfigurations.First(item => item.Value == currentConfig).Key;
			}
			else
			{
				curViewName = allConfigurations.First().Key;
			}
			// Limit length of View title to remaining available width
			curViewName = TrimToMaxPixelWidth(Math.Max(2, maxViewWidth), curViewName);
			var isReversalIndex = DictionaryConfigurationListener.GetDictionaryConfigurationType(m_mediator) == xWorksStrings.ReversalIndex;
			if (!isReversalIndex)
				ResetSpacer(maxViewWidth, curViewName);
			else
				((IPaneBar) m_informationBar).Text = curViewName;
		}

		/// <summary>
		/// Sets the text on the info bar that appears above the main view panel.
		/// The xml configuration file that this view is based on can configure the text in several ways.
		/// </summary>
		protected override void SetInfoBarText()
		{
			if(m_informationBar == null)
				return;
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
			if (pubNameTitlePiece == xWorksStrings.AllEntriesPublication)
				pubNameTitlePiece = xWorksStrings.ksAllEntries;
			titleStr = pubNameTitlePiece + " " + titleStr;
			var isReversalIndex = DictionaryConfigurationListener.GetDictionaryConfigurationType(m_mediator) == xWorksStrings.ReversalIndex;
			if (isReversalIndex)
			{
				var maxViewWidth = Width / 2 - kSpaceForMenuButton;
				// Limit length of View title to remaining available width
				titleStr = TrimToMaxPixelWidth(Math.Max(2, maxViewWidth), titleStr);
				ResetSpacer(maxViewWidth, titleStr);
			}
			else
				((IPaneBar) m_informationBar).Text = titleStr;
		}

		private const int kSpaceForMenuButton = 26;


		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			SetInfoBarText();
		}

		public string FindTabHelpId { get; private set; }
	}
}

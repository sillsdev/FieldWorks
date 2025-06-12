// Copyright (c) 2014-2022 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using Gecko;
using Gecko.DOM;
using SIL.CommandLineProcessing;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.FieldWorks.FwCoreDlgControls;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.IO;
using SIL.LCModel.Utils;
using SIL.Progress;
using SIL.Utils;
using SIL.Windows.Forms.HtmlBrowser;
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
		internal const string CurrentSelectedEntryClass = "currentSelectedEntry";
		private const string FieldWorksPrintLimitEnv = "FIELDWORKS_PRINT_LIMIT";

		private GeckoWebBrowser GeckoBrowser => (GeckoWebBrowser)m_mainView.NativeBrowser;

		public override void Init(Mediator mediator, PropertyTable propertyTable, XmlNode configurationParameters)
		{
			m_mediator = mediator;
			m_propertyTable = propertyTable;
			m_configurationParameters = configurationParameters;
			m_mainView = new XWebBrowser(XWebBrowser.BrowserType.GeckoFx);
			m_mainView.Dock = DockStyle.Fill;
			m_mainView.Location = new Point(0, 0);
			m_mainView.IsWebBrowserContextMenuEnabled = false;
			ReadParameters();
			// Use update helper to help with optimizations and special cases for list loading
			using(new RecordClerk.ListUpdateHelper(Clerk, Clerk.ListLoadingSuppressed))
			{
				m_mediator.AddColleague(this);
				Clerk.UpdateOwningObjectIfNeeded();
			}
			Controls.Add(m_mainView);

			// REVIEW (Hasso) 2021.05: when do we expect NativeBrowser not to be a GeckoWebBrowser?
			if (m_mainView.NativeBrowser is GeckoWebBrowser browser)
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
			var browser = GeckoBrowser;
			if (scrollDelta < 0 && browser.Window.ScrollY == 0)
			{
				AddMoreEntriesToPage(true, browser);
			}
			else if (browser.Window.ScrollY >= browser.Window.ScrollMaxY)
			{
				AddMoreEntriesToPage(false, browser);
			}
		}

		/// <summary>
		/// Handle a key press in the web browser displaying the xhtml. [TODO: LT-xxxxx]
		/// </summary>
		private void OnDomKeyPress(object sender, DomKeyEventArgs e)
		{
			var browser = GeckoBrowser;
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
						AddMoreEntriesToPage(true, browser);
					}
					break;
				}
				case DOWN:
				{
					if (browser.Window.ScrollY >= browser.Window.ScrollMaxY)
					{
						AddMoreEntriesToPage(false, browser);
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
							Clerk.JumpToRecord(PublicationDecorator.GetEntriesToPublish(m_propertyTable, Clerk.VirtualFlid)[itemIndex]);
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
							Clerk.JumpToRecord(PublicationDecorator.GetEntriesToPublish(m_propertyTable, Clerk.VirtualFlid)[itemIndex]);
						}
					}
					break;
				}
				default:
					break;
			}
		}

		/// <summary>
		/// Used to verify current content control so that Find Lexical Entry behaves differently
		/// in Dictionary View.
		/// </summary>
		private const string ksLexDictionary = "lexiconDictionary";

		/// <summary>
		/// Check to see if the user needs to be alerted that JumpToRecord is not possible.
		/// </summary>
		/// <param name="argument">the hvo of the record</param>
		/// <returns></returns>
		public bool OnJumpToRecord(object argument)
		{
			var hvoTarget = (int)argument;
			var currControl = m_propertyTable.GetStringProperty("currentContentControl", "");
			if (hvoTarget > 0 && currControl == ksLexDictionary)
			{
				DictionaryConfigurationController.ExclusionReasonCode xrc;
				// Make sure we explain to the user in case hvoTarget is not visible due to
				// the current Publication layout or Configuration view.
				if (!IsObjectVisible(hvoTarget, out xrc))
				{
					// Tell the user why we aren't jumping to his record
					GiveSimpleWarning(xrc);
				}
			}
			return false;
		}

		private void GiveSimpleWarning(DictionaryConfigurationController.ExclusionReasonCode xrc)
		{
			// Tell the user why we aren't jumping to his record
			var msg = xWorksStrings.ksSelectedEntryNotInDict;
			string caption;
			string reason;
			string shlpTopic;
			switch (xrc)
			{
				case DictionaryConfigurationController.ExclusionReasonCode.NotInPublication:
					caption = xWorksStrings.ksEntryNotPublished;
					reason = xWorksStrings.ksEntryNotPublishedReason;
					shlpTopic = "User_Interface/Menus/Edit/Find_a_lexical_entry.htm";		//khtpEntryNotPublished
					break;
				case DictionaryConfigurationController.ExclusionReasonCode.ExcludedHeadword:
					caption = xWorksStrings.ksMainNotShown;
					reason = xWorksStrings.ksMainNotShownReason;
					shlpTopic = "khtpMainEntryNotShown";
					break;
				case DictionaryConfigurationController.ExclusionReasonCode.ExcludedMinorEntry:
					caption = xWorksStrings.ksMinorNotShown;
					reason = xWorksStrings.ksMinorNotShownReason;
					shlpTopic = "khtpMinorEntryNotShown";
					break;
				default:
					throw new ArgumentException("Unknown ExclusionReasonCode");
			}
			msg = String.Format(msg, reason);
			// TODO-Linux: Help is not implemented on Mono
			MessageBox.Show(FindForm(), msg, caption, MessageBoxButtons.OK,
							MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, 0,
							m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider").HelpFile,
							HelpNavigator.Topic, shlpTopic);
		}

		private bool IsObjectVisible(int hvoTarget, out DictionaryConfigurationController.ExclusionReasonCode xrc)
		{
			xrc = DictionaryConfigurationController.ExclusionReasonCode.NotExcluded;
			var objRepo = Cache.ServiceLocator.GetInstance<ICmObjectRepository>();
			Debug.Assert(objRepo.IsValidObjectId(hvoTarget), "Invalid hvoTarget!");
			if (!objRepo.IsValidObjectId(hvoTarget))
				throw new ArgumentException("Unknown object.");
			var entry = objRepo.GetObject(hvoTarget) as ILexEntry;
			Debug.Assert(entry != null, "HvoTarget is not a LexEntry!");
			if (entry == null)
				throw new ArgumentException("Target is not a LexEntry.");

			// Now we have our LexEntry
			// First deal with whether the active Publication excludes it.
			var m_currentPublication = m_propertyTable.GetValue<string>("SelectedPublication", null);
			var publications = Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Select(p => p).Where(p => p.NameHierarchyString == m_currentPublication.ToString()).FirstOrDefault();
			//if the publications is null in case of Dictionary view selected as $$All Entries$$.
			if (publications != null && publications.NameHierarchyString != xWorksStrings.AllEntriesPublication)
			{
				var currentPubPoss = publications;
				if (!entry.PublishIn.Contains(currentPubPoss))
				{
					xrc = DictionaryConfigurationController.ExclusionReasonCode.NotInPublication;
					return false;
				}
				// Second deal with whether the entry shouldn't be shown as a headword
				if (!entry.ShowMainEntryIn.Contains(currentPubPoss))
				{
					xrc = DictionaryConfigurationController.ExclusionReasonCode.ExcludedHeadword;
					return false;
				}
			}
			// Third deal with whether the entry shouldn't be shown as a minor entry.
			// commented out until conditions are clarified (LT-11447)
			var configuration = new DictionaryConfigurationModel(GetCurrentConfiguration(false), Cache);
			if (entry.EntryRefsOS.Count > 0 && !entry.PublishAsMinorEntry && configuration.IsRootBased)
			{
				xrc = DictionaryConfigurationController.ExclusionReasonCode.ExcludedMinorEntry;
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
				HandleDomLeftClick(Clerk, m_propertyTable, e, element);
			}
			else if (e.Button == GeckoMouseButton.Right)
			{
				HandleDomRightClick(browser, e, element, m_propertyTable, m_mediator);
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
				// Without this we show the entry count in the status bar the first time we open the Dictionary or Rev. Index.
				Clerk.SelectedRecordChanged(true, true);
			}
		}

		/// <summary>
		/// Handle the user left clicking on the document view by jumping to an entry, playing a media element, or adjusting the view
		/// </summary>
		/// <remarks>internal so that it can be re-used by the XhtmlRecordDocView</remarks>
		internal static void HandleDomLeftClick(RecordClerk clerk, PropertyTable propertyTable, DomMouseEventArgs e, GeckoElement element)
		{
			// the destination is either the target of a link to another entry
			// or the entry being clicked (when the user clicks anywhere in an entry that is not currently selected)
			var destinationGuid = GetGuidFromEntryLink(element);
			if (destinationGuid == Guid.Empty)
				GetClassListFromGeckoElement(element, out destinationGuid, out _);

			// If we don't have a destination GUID, the user may have clicked a video player. We can't handle that,
			// and if we say we did, we will prevent the user from operating the video controls.
			if (destinationGuid == Guid.Empty)
				return;

			var currentObj = clerk.CurrentObject;
			if (currentObj != null && currentObj.Guid == destinationGuid)
			{
				// don't need to jump: we're already here. If this is an Anchor element, it's probably a link to a video;
				// return without setting e.Handled = true; Gecko will open the link
				if (element is GeckoAnchorElement)
					return;
			}
			else
			{
				var cache = propertyTable.GetValue<LcmCache>("cache");
				if (cache.ServiceLocator.GetInstance<ICmObjectRepository>().TryGetObject(destinationGuid, out var obj))
				{
					// Jump only if we need to; unnecessary refreshes prevent audio from playing when the user clicks an audio link (LT-19967)
					if (clerk.JumpToTargetWillChangeIndex(obj.Hvo))
						clerk.OnJumpToRecord(obj.Hvo);
					else if (element is GeckoAnchorElement)
						return;
				}
			}
			e.Handled = true;
		}

		private void AddMoreEntriesToPage(bool goingUp, GeckoWebBrowser browser)
		{
			var browserElement = browser.Document.Body;
			var entriesToPublish = PublicationDecorator.GetEntriesToPublish(m_propertyTable, Clerk.VirtualFlid);
			// Right-to-Left for the overall layout is determined by Dictionary-Normal
			var dictionaryNormalStyle = new ExportStyleInfo(FontHeightAdjuster.StyleSheetFromPropertyTable(m_propertyTable).Styles["Dictionary-Normal"]);
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
				var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, new ReadOnlyPropertyTable(m_propertyTable), false, false, "", isNormalRightToLeft);
				var entries = LcmXhtmlGenerator.GenerateNextFewEntries(PublicationDecorator, entriesToPublish, GetCurrentConfiguration(false), settings, oldCurPageRange,
					oldAdjPageRange, ConfiguredLcmGenerator.EntriesToAddCount, out newCurPageRange, out newAdjPageRange);
				// Load entries above the first entry
				foreach (var entry in entries)
				{
					var entryElement = browserElement.OwnerDocument.CreateHtmlElement("div");
					var entryDoc = XDocument.Parse(entry.ToString());
					foreach (var attribute in entryDoc.Root.Attributes())
					{
						entryElement.SetAttribute(attribute.Name.ToString(), attribute.Value);
					}
					entryElement.InnerHtml = string.Join("", entryDoc.Root.Elements().Select(x => x.ToString(SaveOptions.DisableFormatting)));
					// Get the div of the first entry element
					var before = browserElement.EvaluateXPath("*[contains(@class, 'entry')]").GetSingleNodeValue();
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
					return;
				var adjPage = (GeckoHtmlElement)currentPageButton.NextSibling;
				if (adjPage == null)
					return;
				var currentPageRange = new Tuple<int, int>(int.Parse(currentPageButton.Attributes["startIndex"].NodeValue), int.Parse(currentPageButton.Attributes["endIndex"].NodeValue));
				var adjacentPageRange = new Tuple<int, int>(int.Parse(adjPage.Attributes["startIndex"].NodeValue), int.Parse(adjPage.Attributes["endIndex"].NodeValue));
				var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, new ReadOnlyPropertyTable(m_propertyTable), false, false, "", isNormalRightToLeft);
				var entries = LcmXhtmlGenerator.GenerateNextFewEntries(PublicationDecorator, entriesToPublish, GetCurrentConfiguration(false), settings, currentPageRange,
					adjacentPageRange, ConfiguredLcmGenerator.EntriesToAddCount, out newCurrentPageRange, out newAdjPageRange);
				// Load entries above the lower navigation buttons
				foreach (var entry in entries)
				{
					var entryElement = browserElement.OwnerDocument.CreateHtmlElement("div"); var entryDoc = XDocument.Parse(entry.ToString());
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
			return (GeckoHtmlElement)pageButtonElement?.OwnerDocument?.Body?.EvaluateXPath("(//*[@class='pagebutton' and @id])[2]")?.GetSingleNodeValue();
		}

		private static GeckoHtmlElement GetTopCurrentPageButton(GeckoElement element)
		{
			// The page with the id is the current page, select the first one on the page
			return (GeckoHtmlElement)element?.OwnerDocument?.Body?.EvaluateXPath("//*[@class='pagebutton' and @id]")?.GetSingleNodeValue();
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
		internal static void HandleDomRightClick(GeckoWebBrowser browser, DomMouseEventArgs e, GeckoElement element, PropertyTable propertyTable, Mediator mediator)
		{
			Guid topLevelGuid;
			GeckoElement entryElement;
			var classList = GetClassListFromGeckoElement(element, out topLevelGuid, out entryElement);
			var localizedName = DictionaryConfigurationListener.GetDictionaryConfigurationType(propertyTable);
			var label = string.Format(xWorksStrings.ksConfigure, localizedName);
			s_contextMenu = new ContextMenuStrip();
			var item = new DisposableToolStripMenuItem(label);
			s_contextMenu.Items.Add(item);
			item.Click += RunConfigureDialogAt;
			item.Tag = new object[] { propertyTable, mediator, classList, topLevelGuid };
			if (e.CtrlKey) // show hidden menu item for tech support
			{
				item = new DisposableToolStripMenuItem(xWorksStrings.ksInspect);
				s_contextMenu.Items.Add(item);
				item.Click += RunDiagnosticsDialogAt;
				item.Tag = new object[] { propertyTable, entryElement, topLevelGuid };
			}
			s_contextMenu.Show(browser, new Point(e.ClientX, e.ClientY));
			s_contextMenu.Closed += m_contextMenu_Closed;
			e.Handled = true;
		}

		/// <summary>
		/// Returns the class hierarchy for a GeckoElement
		/// </summary>
		/// <remarks>LT-17213 Internal for use in DictionaryConfigurationDlg</remarks>
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

		/// <summary>
		/// Gets the GUID from a Gecko DOM Element that represents a link to an entry (or sense?).
		/// If the passed element is not a link, or if it a link to play a file, Guid.Empty is returned.
		/// </summary>
		/// <exception cref="FormatException">
		/// If the href happens to start with a GUID but have extra text at the end (this should happen only for audio writing systems,
		/// but we shouldn't be returning links for those. And throwing doesn't affect whether audio is played, anyway.
		/// </exception>
		/// <remarks>
		/// <see cref="ConfiguredLcmGenerator"/> generates subentry headwords in Audio Writing Systems
		/// as media links, *not* links to that entry (as other WS's are)
		/// </remarks>
		private static Guid GetGuidFromEntryLink(GeckoElement element)
		{
			// A link to somewhere must have an 'href' and must not have an 'onclick' attribute. If I recall correctly, we have to put an 'href' on
			// media links to get the hotlink hand to appear, but we don't want them to actually go anywhere, as this prevents playing the media.
			if (!element.HasAttribute("href") || element.HasAttribute("onclick"))
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
			var propertyTable = tagObjects[0] as PropertyTable;
			var mediator = tagObjects[1] as Mediator;
			var classList = tagObjects[2] as List<string>;
			var guid = (Guid)tagObjects[3];
			bool refreshNeeded;
			using (var dlg = new DictionaryConfigurationDlg(propertyTable))
			{
				var cache = propertyTable.GetValue<LcmCache>("cache");
				var clerk = propertyTable.GetValue<RecordClerk>("ActiveClerk", null);
				ICmObject current = null;
				if (guid != Guid.Empty && cache != null && cache.ServiceLocator.ObjectRepository.IsValidObjectId(guid))
					current = cache.ServiceLocator.GetObject(guid);
				else if (clerk != null)
					current = clerk.CurrentObject;
				var controller = new DictionaryConfigurationController(dlg, propertyTable, mediator, current);
				controller.SetStartingNode(classList);
				dlg.Text = String.Format(xWorksStrings.ConfigureTitle, DictionaryConfigurationListener.GetDictionaryConfigurationType(propertyTable));
				dlg.HelpTopic = DictionaryConfigurationListener.GetConfigDialogHelpTopic(propertyTable);
				dlg.ShowDialog(propertyTable.GetValue<IWin32Window>("window"));
				refreshNeeded = controller.MasterRefreshRequired;
			}
			if (refreshNeeded)
				mediator.SendMessage("MasterRefresh", null);
		}

		private static void RunDiagnosticsDialogAt(object sender, EventArgs e)
		{
			var item = (ToolStripMenuItem)sender;
			var tagObjects = (object[])item.Tag;
			var propTable = (PropertyTable)tagObjects[0];
			var element = (GeckoElement)tagObjects[1];
			var guid = (Guid)tagObjects[2];
			using (var dlg = new XmlDiagnosticsDlg(element, guid))
			{
				dlg.ShowDialog(propTable.GetValue<IWin32Window>("window"));
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
			if (string.IsNullOrEmpty(m_propertyTable.GetStringProperty("SuspendLoadingRecordUntilOnJumpToRecord", null)))
				UpdateContent(validConfiguration);
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
		public bool OnPrint(object commandObject)
		{
			const int defaultMaxEntriesFWCanPrint = 10000;
			CloseContextMenuIfOpen(); // not sure if this is necessary or not
			var areAllEntriesOnOnePage = m_mainView.NativeBrowser is GeckoWebBrowser browser &&
										 GetTopCurrentPageButton(browser.Document.Body) == null;
			var entryCount = PublicationDecorator.GetEntriesToPublish(m_propertyTable, Clerk.VirtualFlid).Length;
			var message = string.Format(xWorksStrings.promptGenerateAllEntriesBeforePrinting_ShowingXofX,
					LcmXhtmlGenerator.EntriesPerPage, entryCount);
			if (!areAllEntriesOnOnePage && MessageBox.Show(message, xWorksStrings.promptGenerateAllEntriesBeforePrinting,
				MessageBoxButtons.YesNo) == DialogResult.Yes)
			{
				if (!int.TryParse(Environment.GetEnvironmentVariable(FieldWorksPrintLimitEnv), out var maxEntriesFWCanPrint))
				{
					maxEntriesFWCanPrint = defaultMaxEntriesFWCanPrint;
				}

				if (entryCount > maxEntriesFWCanPrint)
				{
					GeneratePdfToPrint();
				}
				else
				{
					GenerateReloadAndPrint();
				}
			}
			else
			{
				PrintPage(m_mainView);
			}
			return true;
		}

		private void GeneratePdfToPrint()
		{
			const int pdfGenerationTimeout = 3600;
			const string html2PdfExe = "FieldWorksPdfMaker.exe";
			// Generate all entries to an xhtml file on disk
			var xhtmlPath = SaveConfiguredXhtmlWithProgress(GetCurrentConfiguration(false), true);
			if (xhtmlPath == null)
			{
				// the user canceled
				return;
			}
			// In the past, we have had difficulty generating large dictionaries and then printing from within FieldWorks (LT-20658, LT-20883).
			// Instead, generate a PDF and open it in the system viewer for the user to print.
			var pdfPrinterPath = Path.Combine(FileLocationUtilities.DirectoryOfTheApplicationExecutable, html2PdfExe);
			if (!RobustFile.Exists(pdfPrinterPath))
			{
				// FileNotFoundException will trigger the right reporting mechanism.
				// Normally, we don't localize exception messages, but this is one that users may be able to resolve themselves if they understand it.
				throw new FileNotFoundException(string.Format(xWorksStrings.MissingGeckofxHtmlToPdf, html2PdfExe), html2PdfExe);
			}
			var runner = new CommandLineRunner();
			var outputFile = Path.Combine(Path.GetTempPath(), $"FieldWorks_Print.{DateTime.Now:yyyy-MM-dd.HHmm}.pdf");
			ExecutionResult result;
			using (new WaitCursor(ParentForm))
			{
				result = runner.Start(pdfPrinterPath, $"\"{xhtmlPath}\" \"{outputFile}\" --graphite --reduce-memory", Encoding.UTF8, string.Empty,
					pdfGenerationTimeout, new NullProgress(), line => Debug.WriteLine($"DEBUG GeckofxHtmlToPdf report line: '{line}'"));
			}
			if (result.ExitCode != 0)
			{
				// Including StandardOutput because GeckofxHtmlToPdf puts the useful information in StandardOutput.
				new SilErrorReportingAdapter(Form.ActiveForm, m_propertyTable).ReportNonFatalException(new Exception(
					$"Error generating PDF for printing:{Environment.NewLine}{result.StandardError}{Environment.NewLine}{result.StandardOutput}"));
			}
			else if (result.DidTimeOut || !RobustFile.Exists(outputFile))
			{
				MessageBox.Show(xWorksStrings.SomethingWentWrongTryingToPrintDict, xWorksStrings.ksErrorCaption);
			}
			else
			{
				// Open the PDF in the system viewer. The user can print from there.
				Process.Start(outputFile);
			}
		}

		private void GenerateReloadAndPrint()
		{
			// Generate all entries
			UpdateContent(GetCurrentConfiguration(false), true);

			// The Control.Refresh command to load the newly-generated page returns before it is finished,
			// but then fails if the print dialog is opened too soon. Printing on idle solves this.
			void PrintAfterRefresh(object sender, EventArgs args)
			{
				Application.Idle -= PrintAfterRefresh;
				// The user may become impatient and cancel; don't try to print if this happens
				if (!IsDisposed)
				{
					// Trying to print immediately on idle on Linux leads to a COMException.
					// Trying to print some dictionaries on Windows prints only the first page.
					// This dialog will be shown when the full dictionary view display is complete,
					// so when it is closed, the Print dialog will open, and it should work properly.
					// There is probably a way to block the Print until a thread gets done, but we don't
					// have time to research that, and this solves the problem, and it's not a high-use
					// feature so we can live with the extra dialog.
					MessageBox.Show(xWorksStrings.FinishedGeneratingEntries);
					try
					{
						PrintPage(m_mainView);
					}
					catch (COMException)
					{
						// Swallow the exception because the solution is to generate a PDF for the user to print. Tell the user how:
						MessageBox.Show(string.Format(xWorksStrings.COMExceptionPrintingLargeDictionary, FieldWorksPrintLimitEnv),
							xWorksStrings.ksErrorCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			}
			Application.Idle += PrintAfterRefresh;
		}

		internal static void PrintPage(XWebBrowser browser)
		{
			var geckoBrowser = browser.NativeBrowser as GeckoWebBrowser;
			geckoBrowser?.Window.Print();
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
				var tool = XmlUtils.GetMandatoryAttributeValue(coreCommand.Parameters[0], "tool");
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
					var validConfiguration = SetCurrentDictionaryPublicationLayout();
					UpdateContent(validConfiguration);
					break;
				case "DictionaryPublicationLayout":
				case "ReversalIndexPublicationLayout":
					var currentConfig = GetCurrentConfiguration(false);
					if (name == "ReversalIndexPublicationLayout")
						DictionaryConfigurationUtils.SetReversalIndexGuidBasedOnReversalIndexConfiguration(m_propertyTable, Cache);
					var currentPublication = GetCurrentPublication();
					var validPublication = GetValidPublicationForConfiguration(currentConfig) ?? xWorksStrings.AllEntriesPublication;
					if (validPublication != currentPublication)
					{
						m_propertyTable.SetProperty("SelectedPublication", validPublication, false);
					}
					UpdateContent(currentConfig);
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
			var currentObjectIndex = Array.IndexOf(PublicationDecorator.GetEntriesToPublish(m_propertyTable, Clerk.VirtualFlid), currentObjectHvo);
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
		private void RemoveStyleFromPreviousSelectedEntryOnView(GeckoWebBrowser browser)
		{
			if (string.IsNullOrEmpty(m_selectedObjectID))
			{
				return;
			}
			var prevSelectedByGuid = browser.Document.GetHtmlElementById("g" + m_selectedObjectID);
			if (prevSelectedByGuid != null)
			{
				RemoveClassFromHtmlElement(prevSelectedByGuid, CurrentSelectedEntryClass);
			}
		}

		/// <summary>
		/// Set the style attribute on the current entry to color the background.
		/// </summary>
		private void SetActiveSelectedEntryOnView(GeckoWebBrowser browser)
		{
			if (Clerk.CurrentObject == null)
				return;

			if (Clerk.Id == "AllReversalEntries")
			{
				var reversalentry = Clerk.CurrentObject as IReversalIndexEntry;
				if (reversalentry == null)
					return;
				var writingSystem = Cache.ServiceLocator.WritingSystemManager.Get(reversalentry.ReversalIndex.WritingSystem);
				if (writingSystem == null)
					return;
				var currReversalWs = writingSystem.Id;
				var currentConfig = m_propertyTable.GetStringProperty("ReversalIndexPublicationLayout", string.Empty);
				var configuration = File.Exists(currentConfig) ? new DictionaryConfigurationModel(currentConfig, Cache) : null;
				if (configuration == null || configuration.WritingSystem != currReversalWs)
				{
					var newConfig = Path.Combine(DictionaryConfigurationListener.GetProjectConfigurationDirectory(m_propertyTable),
						writingSystem.Id + DictionaryConfigurationModel.FileExtension);
					m_propertyTable.SetProperty("ReversalIndexPublicationLayout", File.Exists(newConfig) ? newConfig : null, true);
				}
			}
			var currentObjectGuid = Clerk.CurrentObject.Guid.ToString();
			var currSelectedByGuid = browser.Document.GetHtmlElementById("g" + currentObjectGuid);
			if (currSelectedByGuid == null)
				return;

			// Adjust active item to be lower down on the page.
			var currElementRect = currSelectedByGuid.GetBoundingClientRect();
			var currElementTop = currElementRect.Top + browser.Window.ScrollY;
			var currElementBottom = currElementRect.Bottom + browser.Window.ScrollY;
			var yPosition = currElementTop - browser.Height / 4.0;

			// Scroll only if current element is not visible on browser window
			if (currElementTop < browser.Window.ScrollY || currElementBottom > browser.Window.ScrollY + browser.Height)
				browser.Window.ScrollTo(0, (int)yPosition);

			AddClassToHtmlElement(currSelectedByGuid, CurrentSelectedEntryClass);
			m_selectedObjectID = currentObjectGuid;
		}

		#region Add/Remove GeckoHtmlElement Class

		private const string Space = " ";

		/// <summary>
		/// Adds 'classToAdd' to the class attribute of 'element', preserving any existing classes.
		/// Changes nothing if 'classToAdd' is already present.
		/// </summary>
		private void AddClassToHtmlElement(GeckoHtmlElement element, string classToAdd)
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
		private void RemoveClassFromHtmlElement(GeckoHtmlElement element, string classToRemove)
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
			var validPublication = GetValidPublicationForConfiguration(currentConfig) ?? xWorksStrings.AllEntriesPublication;
			if (currentPublication != xWorksStrings.AllEntriesPublication && currentPublication != validPublication)
			{
				m_propertyTable.SetProperty("SelectedPublication", validPublication, true);
			}
			UpdateContent(currentConfig);
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
			if (m_mainView != null)
			{
				var geckoBrowser = m_mainView.NativeBrowser as GeckoWebBrowser;
				if (geckoBrowser != null)
				{
					geckoBrowser.Window.Find(string.Empty, false, false, true, false, true, true);
				}
			}
			return true;
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
			m_propertyTable.SetProperty("SelectedPublication", xWorksStrings.AllEntriesPublication, true);
			return true;
		}

		private void UpdateContent(string configurationFile, bool allOnOnePage = false)
		{
			SetInfoBarText();
			var htmlErrorMessage = xWorksStrings.ksErrorDisplayingPublication;
			if (string.IsNullOrEmpty(configurationFile))
			{
				htmlErrorMessage = xWorksStrings.NoConfigsMatchPub;
			}
			else
			{
				var xhtmlPath = SaveConfiguredXhtmlWithProgress(configurationFile, allOnOnePage);
				if (xhtmlPath != null)
				{
					m_mainView.Navigate(new Uri(xhtmlPath));
					return;
				}
			}
			m_mainView.DocumentText = $"<html><body>{htmlErrorMessage}</body></html>";
		}

		private string SaveConfiguredXhtmlWithProgress(string configurationFile, bool allOnePage = false)
		{
			using (new WaitCursor(ParentForm))
			using (var progressDlg = new Common.Controls.ProgressDialogWithTask(ParentForm))
			{
				progressDlg.AllowCancel = true;
				progressDlg.CancelLabelText = xWorksStrings.ksCancelingPublicationLabel;
				progressDlg.Title = xWorksStrings.ksPreparingPublicationDisplay;
				if (progressDlg.RunTask(true, SaveConfiguredXhtml, PublicationDecorator, configurationFile, allOnePage) is string xhtmlPath)
				{
					if (progressDlg.IsCanceling)
					{
						m_mediator.SendMessage("SetToolFromName", "lexiconEdit");
					}
					else
					{
						return xhtmlPath;
					}
				}
			}

			return null;
		}

		private object SaveConfiguredXhtml(IThreadedProgress progress, object[] args)
		{
			if (args.Length != 3)
				return null;
			var publicationDecorator = (DictionaryPublicationDecorator)args[0];
			var configurationFile = (string)args[1];
			var allOnOnePage = (bool)args[2];
			if (progress != null)
				progress.Message = xWorksStrings.ksObtainingEntriesToDisplay;
			var configuration = new DictionaryConfigurationModel(configurationFile, Cache);
			publicationDecorator.Refresh();
			var entriesToPublish = publicationDecorator.GetEntriesToPublish(m_propertyTable, Clerk.VirtualFlid);
			var start = DateTime.Now;
			var entriesPerPage = allOnOnePage ? entriesToPublish.Length : LcmXhtmlGenerator.EntriesPerPage;
			if (progress != null)
			{
				progress.Minimum = 0;
				progress.Maximum = entriesPerPage + 1 + entriesPerPage / 100;
				progress.Position++;
			}
			var xhtmlPath = LcmXhtmlGenerator.SavePreviewHtmlWithStyles(entriesToPublish, publicationDecorator, configuration, m_propertyTable,
				progress, entriesPerPage);
			var end = DateTime.Now;
			Debug.WriteLine($"saving xhtml/css took {end - start}");
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
			return m_propertyTable.GetStringProperty("SelectedPublication",
			 xWorksStrings.AllEntriesPublication);
		}

		private string GetCurrentConfiguration(bool fUpdate)
		{
			return DictionaryConfigurationListener.GetCurrentConfiguration(m_propertyTable, fUpdate);
		}

		private void SetCurrentConfiguration(string currentConfig, bool fUpdate)
		{
			DictionaryConfigurationListener.SetCurrentConfiguration(m_propertyTable, currentConfig, fUpdate);
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
			var isReversalIndex = DictionaryConfigurationListener.GetDictionaryConfigurationType(m_propertyTable) == xWorksStrings.ReversalIndex;
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
			var isReversalIndex = DictionaryConfigurationListener.GetDictionaryConfigurationType(m_propertyTable) == xWorksStrings.ReversalIndex;
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

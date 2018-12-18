// Copyright (c) 2003-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.XMLViews;
using LanguageExplorer.DictionaryConfiguration;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.DomainServices;
using SIL.Xml;

namespace LanguageExplorer.Areas
{
	/// <summary>
	/// XmlDocView is a view that shows a complete list as a single view.
	/// A RecordList class does most of the work of managing the list and current object.
	///	list management and navigation is entirely(?) handled by the
	/// RecordList.
	///
	/// The actual view of each object is specified by a child <jtview></jtview> node
	/// of the view node. This specifies how to display an individual list item.
	/// </summary>
	internal class XmlDocView : ViewBase, IFindAndReplaceContext, IPostLayoutInit
	{
		// the root HVO.
		protected int m_hvoOwner;
		/// <summary>
		/// Object currently being edited.
		/// </summary>
		protected ICmObject m_currentObject;
		protected int m_currentIndex = -1;
		protected XmlSeqView m_mainView;
		protected string m_configObjectName;
		private string m_titleStr; // Helps avoid running through SetInfoBarText 4x!
		private string m_currentPublication;
		private string m_currentConfigView; // used when this is a Dictionary view to store which view is active.
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		#region Consruction and disposal
		/// <summary />
		public XmlDocView()
		{
			m_fullyInitialized = false;
			InitializeComponent();
			AccNameDefault = "XmlDocView";
		}

		public XmlDocView(XElement configurationParametersElement, LcmCache cache, IRecordList recordList)
			: base(configurationParametersElement, cache, recordList)
		{
		}

		#region TitleBar Layout Menu

#if RANDYTODO
		/// <summary>
		/// Populate the list of layout views for the second dictionary titlebar menu.
		/// </summary>
		public bool OnDisplayLayouts(object parameter, ref UIListDisplayProperties display)
		{
			var layoutList = GatherBuiltInAndUserLayouts();
			foreach (var view in layoutList)
			{
				display.List.Add(view.Item1, view.Item2, null, null, true);
			}
			return true;
		}
#endif

		private IEnumerable<Tuple<string, string>> GatherBuiltInAndUserLayouts()
		{
			var layoutList = new List<Tuple<string, string>>();
			layoutList.AddRange(GetBuiltInLayouts(PropertyTable.GetValue<XElement>("currentContentControlParameters", null)));
			var builtInLayoutList = new List<string>();
			builtInLayoutList.AddRange(from layout in layoutList select layout.Item2);
			var userLayouts = m_mainView.Vc.LayoutCache.LayoutInventory.GetLayoutTypes();
			layoutList.AddRange(GetUserDefinedDictLayouts(builtInLayoutList, userLayouts));
			return layoutList;
		}

		private static IEnumerable<Tuple<string, string>> GetBuiltInLayouts(XElement configNode)
		{
			var configLayouts = XmlUtils.FindElement(configNode, "configureLayouts");
			// The configureLayouts node doesn't always exist!
			if (configLayouts == null)
			{
				return new List<Tuple<string, string>>();
			}
			var layouts = configLayouts.Elements();
			return ExtractLayoutsFromLayoutTypeList(layouts);
		}

		private static IEnumerable<Tuple<string, string>> ExtractLayoutsFromLayoutTypeList(IEnumerable<XElement> layouts)
		{
			return layouts.Select(layout => new Tuple<string, string>(XmlUtils.GetOptionalAttributeValue(layout, "label"), XmlUtils.GetOptionalAttributeValue(layout, "layout")));
		}

		private static IEnumerable<Tuple<string, string>> GetUserDefinedDictLayouts(IEnumerable<string> builtInLayouts, IEnumerable<XElement> layouts)
		{
			var allUserLayoutTypes = ExtractLayoutsFromLayoutTypeList(layouts);
			var result = new List<Tuple<string, string>>();
			// This part prevents getting Reversal Index layouts or Notebook layouts in our (Dictionary) menu.
			result.AddRange(allUserLayoutTypes.Where(layout => builtInLayouts.Any(builtIn => builtIn == BaseLayoutName(layout.Item2))));
			return result;
		}

		private static string BaseLayoutName(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				return string.Empty;
			}
			// Find out if this layout name has a hashmark (#) in it. Return the part before it.
			var parts = name.Split(Inventory.kcMarkLayoutCopy);
			var result = parts.Length > 1 ? parts[0] : name;
			return result;
		}

		#endregion

		/// <summary>
		/// Receives the broadcast message "PropertyChanged"
		/// </summary>
		public void OnPropertyChanged(string name)
		{
			switch (name)
			{
				case "SelectedPublication":
					var pubDecorator = GetPubDecorator();
					if (pubDecorator != null)
					{
						var pubName = GetSelectedPublication();
						if (LanguageExplorerResources.AllEntriesPublication == pubName)
						{   // A null publication means show everything
							pubDecorator.Publication = null;
							m_mainView.RefreshDisplay();
						}
						else
						{   // look up the publication object
							var pub = (Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Where(item => item.Name.UserDefaultWritingSystem.Text == pubName)).FirstOrDefault();
							if (pub != null && pub != pubDecorator.Publication)
							{   // change the publication if it is different from the current one
								pubDecorator.Publication = pub;
								m_mainView.RefreshDisplay();
							}
						}
					}
					break;
				case "DictionaryPublicationLayout":
					var layout = GetSelectedConfigView();
					m_mainView.Vc.ResetTables(layout);
					m_mainView.RefreshDisplay();
					break;
				default:
					// Not sure what other properties might change, but I'm not doing anything.
					break;
			}
		}


		public DictionaryPublicationDecorator GetPubDecorator()
		{
			var sda = m_mainView.DataAccess;
			while (sda != null && !(sda is DictionaryPublicationDecorator) && sda is DomainDataByFlidDecoratorBase)
			{
				sda = ((DomainDataByFlidDecoratorBase)sda).BaseSda;
			}
			return (DictionaryPublicationDecorator)sda;
		}

		// Return CmPossibility if any alternative matches SelectedPublication.
		// If we don't have any record of what publication is selected (typically, first-time startup),
		// pick the first one as a default.
		// If the selected one is not found (typically it is $$all_entries$$), or there are none (pathological), return null.
		private ICmPossibility Publication
		{
			get
			{
				// We don't want to use GetSelectedPublication here because it supplies a default,
				// and we want to treat that case specially.
				var pubName = PropertyTable.GetValue<string>("SelectedPublication");
				if (pubName == null)
				{
					return Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Count > 0 ? Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS[0] : null;
				}
				var pub = Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.FirstOrDefault(item => IsDesiredPublication(item, pubName));
				return pub;
			}
		}

		private static bool IsDesiredPublication(ICmPossibility item, string name)
		{
			return item.Name.AvailableWritingSystemIds.Any(ws => item.Name.get_String(ws).Text == name);
		}

		private string GetSelectedConfigView()
		{
			var sLayoutType = PropertyTable.GetValue("DictionaryPublicationLayout", string.Empty);
			if (string.IsNullOrEmpty(sLayoutType))
			{
				sLayoutType = "publishStem";
			}
			return sLayoutType;
		}

		private string GetSelectedPublication()
		{
			// Sometimes we just want the string value which might be '$$all_entries$$'
			return PropertyTable.GetValue("SelectedPublication", LanguageExplorerResources.AllEntriesPublication);
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				Subscriber.Unsubscribe("RecordListOwningObjChanged", RecordListOwningObjChanged_Message_Handler);
				DisposeTooltip();
				components?.Dispose();
			}
			m_currentObject = null;

			base.Dispose(disposing);
		}

		#endregion // Consruction and disposal

		#region Other methods

		protected override void SetInfoBarText()
		{
			if (m_informationBar == null)
			{
				return;
			}
			var context = XmlUtils.GetOptionalAttributeValue(m_configurationParametersElement, "persistContext", "");
			// SetInfoBarText() was getting run about 4 times just creating one XmlDocView!
			// To prevent that, add the following guards:
			if (m_titleStr != null && NoReasonToChangeTitle(context))
			{
				return;
			}
			var titleStr = GetBaseTitleStringFromConfig();
			var fBaseCalled = false;
			if (titleStr == string.Empty)
			{
				base.SetInfoBarText();
				fBaseCalled = true;
				// (EricP) For some reason I can't provide an IPaneBar get-accessor to return
				// the new Text value. If it's desirable to allow TitleFormat to apply to
				// MyrecordList.CurrentObject, then we either have to duplicate what the
				// base.SetInfoBarText() does here, or get the string set by the base.
				// for now, let's just return.
				if (titleStr == string.Empty)
				{
					return;
				}
			}
			if (context == "Dict")
			{
				m_currentPublication = GetSelectedPublication();
				m_currentConfigView = GetSelectedConfigView();
				titleStr = MakePublicationTitlePart(titleStr);
				SetConfigViewTitle();
			}
			// If we have a format attribute, format the title accordingly.
			var sFmt = XmlUtils.GetOptionalAttributeValue(m_configurationParametersElement, "TitleFormat");
			if (sFmt != null)
			{
				titleStr = string.Format(sFmt, titleStr);
			}
			// If we find that the title is something like ClassifiedDictionary ({SelectedPublication})
			// replace the {} with the name of the selected publication.
			// Enhance: by removing the Debug.Fail, this could be made to insert the value of any
			// property in the mediator's property table.
			var propertyFinder = new Regex(@"\{([^\}]+)\}");
			var match = propertyFinder.Match(titleStr);
			if (match.Success)
			{
				string replacement;
				if (match.Groups[1].Value == "SelectedPublication")
				{
					replacement = GetSelectedPublication();
					if (replacement == LanguageExplorerResources.AllEntriesPublication)
					{
						replacement = AreaResources.ksAllEntries;
					}
				}
				else
				{
					Debug.Fail(@"Unexpected <> value in title string: " + match.Groups[0].Value);
					// This might be useful one day?
					replacement = PropertyTable.GetValue<string>(match.Groups[0].Value);
				}
				if (replacement != null)
				{
					titleStr = propertyFinder.Replace(titleStr, replacement);
				}
			}
			// if we haven't already set the text through the base,
			// or if we had some formatting to do, then set the infoBar text.
			if (!fBaseCalled || sFmt != null)
			{
				((IPaneBar)m_informationBar).Text = titleStr;
			}
			m_titleStr = titleStr;
		}

		#region Dictionary View TitleBar stuff

		private const int kSpaceForMenuButton = 26;

		private void SetConfigViewTitle()
		{
			if (string.IsNullOrEmpty(m_currentConfigView))
			{
				return;
			}
			var maxLayoutViewWidth = Width / 2 - kSpaceForMenuButton;
			var result = GatherBuiltInAndUserLayouts();
			var curViewName = FindViewNameInList(result);
			// Limit length of View title to remaining available width
			curViewName = TrimToMaxPixelWidth(Math.Max(2, maxLayoutViewWidth), curViewName);
			ResetSpacer(maxLayoutViewWidth, curViewName);
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			if (!m_fullyInitialized)
			{
				return;
			}
			base.OnSizeChanged(e);
			m_titleStr = null;
			SetInfoBarText();
		}

		private string FindViewNameInList(IEnumerable<Tuple<string, string>> layoutList)
		{
			var result = string.Empty;
			foreach (var tuple in layoutList.Where(tuple => tuple.Item2 == m_currentConfigView))
			{
				result = tuple.Item1;
				break;
			}
			return result;
		}

		private string MakePublicationTitlePart(string titleStr)
		{
			// titleStr to start is localized equivalent of 'Entries'
			// Limit length of Publication title to half of available width
			var maxPublicationTitleWidth = Math.Max(2, Width / 2 - kSpaceForMenuButton);
			if (string.IsNullOrEmpty(m_currentPublication) || m_currentPublication == LanguageExplorerResources.AllEntriesPublication)
			{
				m_currentPublication = LanguageExplorerResources.AllEntriesPublication;
				titleStr = AreaResources.ksAllEntries;
				// Limit length of Publication title to half of available width
				titleStr = TrimToMaxPixelWidth(maxPublicationTitleWidth, titleStr);
			}
			else
			{
				titleStr = string.Format(AreaResources.ksPublicationEntries, GetPublicationName(), titleStr);
				titleStr = TrimToMaxPixelWidth(maxPublicationTitleWidth, titleStr);
			}
			return titleStr;
		}

		private string GetPublicationName()
		{
			return Publication?.Name?.BestAnalysisAlternative == null ? "***" : Publication.Name.BestAnalysisAlternative.Text;
		}

		private bool NoReasonToChangeTitle(string context)
		{
			switch (context)
			{
				case "Reversal":
					return !IsCurrentReversalWsChanged();
				case "Dict":
					return !IsCurrentPublicationChanged() && !IsCurrentConfigViewChanged();
				default:
					// No need to change anything; dump out!
					return true;
			}
		}

		private bool IsCurrentReversalWsChanged()
		{
			if (m_currentObject == null)
			{
				return true;
			}
			var wsName = GetSafeWsName();
			return m_currentPublication == null || m_currentPublication != wsName;
		}

		private string GetSafeWsName()
		{
			if (m_currentObject != null && m_currentObject.IsValidObject)
			{
				return WritingSystemServices.GetReversalIndexEntryWritingSystem(Cache, m_currentObject.Hvo, Cache.LangProject.CurrentAnalysisWritingSystems[0]).LanguageName;
			}
			return m_hvoOwner < 1 ? string.Empty : WritingSystemServices.GetReversalIndexWritingSystems(Cache, m_hvoOwner, false)[0].LanguageName;
		}

		private bool IsCurrentPublicationChanged()
		{
			var newPub = GetSelectedPublication();
			return newPub != m_currentPublication;
		}

		private bool IsCurrentConfigViewChanged()
		{
			var newView = GetSelectedConfigView();
			return newView != m_currentConfigView;
		}

		#endregion

		/// <summary>
		/// Read in the parameters to determine which sequence/collection we are editing.
		/// </summary>
		protected override void ReadParameters()
		{
			base.ReadParameters();
			var backColorName = XmlUtils.GetOptionalAttributeValue(m_configurationParametersElement, "backColor", "Window");
			BackColor = Color.FromName(backColorName);
			m_configObjectName = StringTable.Table.LocalizeAttributeValue(XmlUtils.GetOptionalAttributeValue(m_configurationParametersElement, "configureObjectName", null));
			Debug.Assert(!string.IsNullOrWhiteSpace(m_configObjectName), "Add 'configureObjectName' attribute value to parameters.");
		}

		public virtual bool OnRecordNavigation(object argument)
		{
			if (!m_fullyInitialized || RecordNavigationInfo.GetSendingList(argument) != MyRecordList)
			{
				// Don't pretend to have handled it if it is not fully initialzed, or isn't our record list.
				return false;
			}
			// persist record list's CurrentIndex in a db specific way
#if RANDYTODO
			TODO: // As of 21JUL17 nobody cares about that 'propName' changing, so skip the broadcast.
#endif
			var propName = MyRecordList.PersistedIndexProperty;
			PropertyTable.SetProperty(propName, MyRecordList.CurrentIndex, true, settingsGroup: SettingsGroup.LocalSettings);
			MyRecordList.SuppressSaveOnChangeRecord = (argument as RecordNavigationInfo).SuppressSaveOnChangeRecord;
			using (new WaitCursor(this))
			{
				try
				{
					ShowRecord();
				}
				finally
				{
					MyRecordList.SuppressSaveOnChangeRecord = false;
				}
			}
			return true;    //we handled this.
		}

		protected override void OnMouseClick(MouseEventArgs e)
		{
			if ((ModifierKeys & Keys.Control) == Keys.Control)
			{
				TryToJumpToSelection(e.Location);
			}
			else if (e.Button == MouseButtons.Right)
			{
				var sel = m_mainView.GetSelectionAtPoint(e.Location, false);
				if (sel == null)
				{
					return;
				}
				if (XmlUtils.FindElement(m_configurationParametersElement, "configureLayouts") == null)
				{
					return; // view is not configurable, don't show menu option.
				}
				int hvo, tag, ihvo, cpropPrevious;
				IVwPropertyStore propStore;
				sel.PropInfo(false, 0, out hvo, out tag, out ihvo, out cpropPrevious, out propStore);
				string nodePath = null;
				if (propStore != null)
				{
					nodePath = propStore.get_StringProperty((int)FwTextPropType.ktptBulNumTxtBef);
				}
				if (string.IsNullOrEmpty(nodePath))
				{
					// may be a literal string, where we can get it from the string itself.
					ITsString tss;
					int ich, ws;
					bool fAssocPrev;
					sel.TextSelInfo(false, out tss, out ich, out fAssocPrev, out hvo, out tag, out ws);
					nodePath = tss.get_Properties(0).GetStrPropValue((int)FwTextPropType.ktptBulNumTxtBef);
				}
				if (m_contextMenu == null)
				{
					var label = string.IsNullOrEmpty(nodePath) ? string.Format(AreaResources.ksConfigure, m_configObjectName) : string.Format(AreaResources.ksConfigureIn, nodePath.Split(':')[3], m_configObjectName);
					m_contextMenu = new ContextMenuStrip();
					var item = new DisposableToolStripMenuItem(label);
					m_contextMenu.Items.Add(item);
					item.Click += RunConfigureDialogAt;
					item.Tag = nodePath;
					m_contextMenu.Show(m_mainView, e.Location);
					m_contextMenu.Closed += m_contextMenu_Closed;
				}
			}
			else
			{
				base.OnMouseClick(e); // be nice to do this anyway, but it does undesirable highlighting.
			}
		}

		private void m_contextMenu_Closed(object sender, ToolStripDropDownClosedEventArgs e)
		{
			Application.Idle += DisposeContextMenu;
		}

		private void DisposeContextMenu(object sender, EventArgs e)
		{
#if RANDYTODO_TEST_Application_Idle
// TODO: Remove when finished sorting out idle issues.
Debug.WriteLine($"Start: Application.Idle run at: '{DateTime.Now:HH:mm:ss.ffff}': on '{GetType().Name}'.");
#endif
			Application.Idle -= DisposeContextMenu;
			if (m_contextMenu != null)
			{
				m_contextMenu.Dispose();
				m_contextMenu = null;
			}
#if RANDYTODO_TEST_Application_Idle
// TODO: Remove when finished sorting out idle issues.
Debug.WriteLine($"End: Application.Idle run at: '{DateTime.Now:HH:mm:ss.ffff}': on '{GetType().Name}'.");
#endif
		}

		// Context menu exists just for one invocation (until idle).
		private ContextMenuStrip m_contextMenu;

		private void RunConfigureDialogAt(object sender, EventArgs e)
		{
			var item = (ToolStripMenuItem)sender;
			var nodePath = (string)item.Tag;
			RunConfigureDialog(nodePath);
		}

		private void TryToJumpToSelection(Point where)
		{
			var obj = SubitemClicked(where, MyRecordList.ListItemsClass);
			if (obj != null && obj.Hvo != MyRecordList.CurrentObjectHvo)
			{
				MyRecordList.JumpToRecord(obj.Hvo);
			}
		}

		/// <summary>
		/// Return the most specific object identified by a click at the specified position.
		/// An object is considered indicated if it is or has an owner of the specified class.
		/// The object must be different from the outermost indicated object in the selection.
		/// </summary>
		internal ICmObject SubitemClicked(Point where, int clsid)
		{
			var adjuster = m_currentConfigView != null && m_currentConfigView.StartsWith("publishRoot")
				? (IPreferedTargetAdjuster)new MainEntryFromSubEntryTargetAdjuster()
				: new NullTargetAdjuster();
			return SubitemClicked(where, clsid, m_mainView, Cache, MyRecordList, adjuster);
		}

		private ToolTip m_tooltip;
		private Point m_lastActiveLocation;

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			// don't try to update the tooltip by getting a selection while painting the view; leads to recursive expansion of lazy boxes.
			// also don't try and update the tooltip if we don't have a record list yet
			if (m_mainView.MouseMoveSuppressed || MyRecordList == null)
			{
				return;
			}
			var item = SubitemClicked(e.Location, MyRecordList.ListItemsClass);
			if (item == null || item.Hvo == MyRecordList.CurrentObjectHvo)
			{
				if (m_tooltip != null)
				{
					m_tooltip.Active = false;
				}
			}
			else
			{
				if (m_tooltip == null)
				{
					m_tooltip = new ToolTip
					{
						InitialDelay = 10,
						ReshowDelay = 10
					};
					m_tooltip.SetToolTip(m_mainView, AreaResources.ksCtrlClickJumpTooltip);
				}
				if (m_tooltip.Active)
				{
					var relLocation = e.Location;
					if (Math.Abs(m_lastActiveLocation.X - e.X) > 20 || Math.Abs(m_lastActiveLocation.Y - e.Y) > 10)
					{
						m_tooltip.Show(AreaResources.ksCtrlClickJumpTooltip, m_mainView, relLocation.X, relLocation.Y, 2000);
						m_lastActiveLocation = e.Location;
					}
				}
				else
				{
					m_lastActiveLocation = e.Location;
				}
				m_tooltip.Active = true;
			}
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			DisposeTooltip();
			base.OnMouseLeave(e);
		}

		private void DisposeTooltip()
		{
			if (m_tooltip == null)
			{
				return;
			}
			m_tooltip.Active = false;
			m_tooltip.Dispose();
			m_tooltip = null;
		}

		/// <summary>
		/// Return an item of the specified class that is indicated by a click at the specified position,
		/// but only if it is part of a different object also of that class.
		/// </summary>
		internal static ICmObject SubitemClicked(Point where, int clsid, SimpleRootSite view, LcmCache cache, ISortItemProvider sortItemProvider, IPreferedTargetAdjuster adjuster)
		{
			var sel = view.GetSelectionAtPoint(where, false);
			if (sel == null)
			{
				return null;
			}
			var rcPrimary = view.GetPrimarySelRect(sel);
			var selRect = new Rectangle(rcPrimary.left, rcPrimary.top, rcPrimary.right - rcPrimary.left, rcPrimary.bottom - rcPrimary.top);
			selRect.Inflate(8, 2);
			if (!selRect.Contains(where))
			{
				return null; // off somewhere in white space, tooltip is confusing
			}
			var helper = SelectionHelper.Create(sel, view);
			var levels = helper.GetLevelInfo(SelLimitType.Anchor);
			ICmObject firstMatch = null;
			ICmObject lastMatch = null;
			foreach (var info in levels)
			{
				var hvo = info.hvo;
				if (!cache.ServiceLocator.IsValidObjectId(hvo))
				{
					continue; // may be some invalid numbers in there
				}
				var obj = cache.ServiceLocator.GetObject(hvo);
				var target = GetTarget(obj, clsid);
				if (target == null)
				{
					continue; // nothing interesting at this level.
				}
				lastMatch = target; // last one we've seen.
				if (firstMatch == null)
				{
					firstMatch = target; // first one we've seen
				}
			}
			firstMatch = adjuster.AdjustTarget(firstMatch);
			if (firstMatch == lastMatch)
			{
				return null; // the only object we can find to jump to is the top-level one we clicked inside. A jump would go nowhere.
			}
			if (sortItemProvider.IndexOf(firstMatch.Hvo) != -1)
			{
				return firstMatch;  // it's a link to a top-level item in the list, we can jump
			}
			// Enhance JohnT: we'd like to be able to jump to the parent entry, if target is a subentry.
			// That's tricky, because this is generic code, and finding the right object requires domain knowledge.
			// For now I'm putting a special case in. At some point we could move this into a helper that could be configured by XML.
			if (!(firstMatch is ILexSense))
			{
				return null;
			}
			firstMatch = ((ILexSense)firstMatch).Entry;
			return sortItemProvider.IndexOf(firstMatch.Hvo) != -1 ? firstMatch : null;
		}

		static ICmObject GetTarget(ICmObject obj, int clsid)
		{
			return obj.ClassID == clsid ? obj : obj.OwnerOfClass(clsid);
		}

		private void RecordListOwningObjChanged_Message_Handler(object newValue)
		{
			if (m_mainView == null)
			{
				return;
			}
			if (MyRecordList.OwningObject == null)
			{
				//this happens, for example, when they user sets a filter on the
				//list we are dependent on, but no records are selected by the filter.
				//thus, we now do not have an object to get records out of,
				//so we need to just show a blank list.
				m_hvoOwner = -1;
			}
			else
			{
				m_hvoOwner = MyRecordList.OwningObject.Hvo;
				m_mainView.ResetRoot(m_hvoOwner);
				SetInfoBarText();
			}
		}

		/// <summary>
		/// By default this returns RecordList.CurrentIndex. However, when we are using a decorator
		/// for the view, we may need to adjust the index.
		/// </summary>
		private int AdjustedRecordListIndex()
		{
			var sda = m_mainView.DataAccess as ISilDataAccessManaged;
			if (sda == null || sda == MyRecordList.VirtualListPublisher)
			{
				return MyRecordList.CurrentIndex; // no tricks.
			}
			if (MyRecordList.CurrentObjectHvo == 0)
			{
				return -1;
			}
			var items = sda.VecProp(m_hvoOwner, m_madeUpFieldIdentifier);
			// Search for the indicated item, working back from the place we expect it to be.
			// This is efficient, because usually only a few items are filtered and it will be close.
			// Also, currently the decorator only removes items, so we won't find it at a larger index.
			// Finally, if there are duplicates, we will find the one closest to the expected position.
			var target = MyRecordList.CurrentObjectHvo;
			var index = Math.Min(MyRecordList.CurrentIndex, items.Length - 1);
			while (index >= 0 && items[index] != target)
			{
				index--;
			}
			if (index < 0 && sda.get_VecSize(m_hvoOwner, m_madeUpFieldIdentifier) > 0)
			{
				return 0; // can we do better? The object selected in other views is hidden in this.
			}
			return index;
		}

		protected override void ShowRecord()
		{
			var currentIndex = AdjustedRecordListIndex();
			// See if it is showing the same record, as before.
			if (m_currentObject != null && MyRecordList.CurrentObject != null && m_currentIndex == currentIndex && m_currentObject.Hvo == MyRecordList.CurrentObject.Hvo)
			{
				SetInfoBarText();
				return;
			}
			// See if the main owning object has changed.
			if (MyRecordList.OwningObject != null && MyRecordList.OwningObject.Hvo != m_hvoOwner)
			{
				m_hvoOwner = MyRecordList.OwningObject.Hvo;
				m_mainView.ResetRoot(m_hvoOwner);
			}
			m_currentObject = MyRecordList.CurrentObject;
			m_currentIndex = currentIndex;
			//add our current state to the history system
			PropertyTable.GetValue<LinkHandler>(LanguageExplorerConstants.LinkHandler).AddLinkToHistory(new FwLinkArgs(PropertyTable.GetValue<string>(AreaServices.ToolChoice), MyRecordList.CurrentObject?.Guid ?? Guid.Empty));
			SelectAndScrollToCurrentRecord();
			base.ShowRecord();
		}

		/// <summary>
		/// Check to see if the user needs to be alerted that JumpToRecord is not possible.
		/// </summary>
		public bool OnCheckJump(object argument)
		{
			var hvoTarget = (int)argument;
			var toolChoice = PropertyTable.GetValue<string>(AreaServices.ToolChoice);
			// Currently this (LT-11447) only applies to Dictionary view
			if (hvoTarget <= 0 || toolChoice != AreaServices.LexiconDictionaryMachineName)
			{
				return true;
			}
			ExclusionReasonCode xrc;
			// Make sure we explain to the user in case hvoTarget is not visible due to
			// the current Publication layout or Configuration view.
			if (!IsObjectVisible(hvoTarget, out xrc))
			{
				AreaServices.GiveSimpleWarning(PropertyTable.GetValue<Form>(FwUtils.window), PropertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider).HelpFile, xrc);
			}
			return true;
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
			if (m_currentPublication != LanguageExplorerResources.AllEntriesPublication)
			{
				var currentPubPoss = Publication;
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
			if (entry.EntryRefsOS.Any() && !entry.PublishAsMinorEntry && IsRootBasedView)
			{
				xrc = ExclusionReasonCode.ExcludedMinorEntry;
				return false;
			}
			// If we get here, we should be able to display it.
			return true;
		}

		private const string ksRootBasedPrefix = "publishRoot";

		protected bool IsRootBasedView
		{
			get
			{
				if (string.IsNullOrEmpty(m_currentConfigView))
				{
					return false;
				}
				return m_currentConfigView.Split(new[] { "#" }, StringSplitOptions.None)[0] == ksRootBasedPrefix;
			}
		}

		/// <summary>
		/// Ensure that we have the current record selected and visible in the window.  See LT-9109.
		/// </summary>
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			if (m_mainView?.RootBox != null && !m_mainView.PaintInProgress && !m_mainView.LayoutInProgress && m_mainView.RootBox.Selection == null)
			{
				SelectAndScrollToCurrentRecord();
			}
		}

		/// <summary>
		/// Pass command (from RecordEditView) on to printing view.
		/// </summary>
		public void PrintFromDetail(PrintDocument pd, int recordHvo)
		{
			m_mainView.PrintFromDetail(pd, recordHvo);
		}

		private void SelectAndScrollToCurrentRecord()
		{
			// Scroll the display to the given record.  (See LT-927 for explanation.)
			try
			{
				const int levelFlid = 0;
				var indexes = new List<int>();
				var currentIndex = AdjustedRecordListIndex();
				indexes.Add(currentIndex);
				// Suppose it is the fifth subrecord of the second subrecord of the ninth main record.
				// At this point, indexes holds 4, 1, 8. That is, like information for MakeSelection,
				// it holds the indexes we want to select from innermost to outermost.
				var rootb = (m_mainView as IVwRootSite).RootBox;
				if (rootb == null)
				{
					return;
				}
				var idx = currentIndex;
				if (idx < 0)
				{
					return;
				}
				// Review JohnT: is there a better way to obtain the needed rgvsli[]?
				var sel = rootb.Selection;
				if (sel != null)
				{
					// skip moving the selection if it's already in the right record.
					var clevels = sel.CLevels(false);
					if (clevels >= indexes.Count)
					{
						for (var ilevel = indexes.Count - 1; ilevel >= 0; ilevel--)
						{
							int hvoObj, tag, ihvo, cpropPrevious;
							IVwPropertyStore vps;
							sel.PropInfo(false, clevels - indexes.Count + ilevel, out hvoObj, out tag, out ihvo, out cpropPrevious, out vps);
							if (ihvo != indexes[ilevel] || tag != levelFlid)
							{
								break;
							}
							if (ilevel != 0)
							{
								continue;
							}
							// selection is already in the right object, just make sure it's visible.
							(m_mainView as IVwRootSite).ScrollSelectionIntoView(sel, VwScrollSelOpts.kssoDefault);
							return;
						}
					}
				}
				var rgvsli = new SelLevInfo[indexes.Count];
				for (var i = 0; i < indexes.Count; i++)
				{
					rgvsli[i].ihvo = indexes[i];
					rgvsli[i].tag = levelFlid;
				}
				rgvsli[rgvsli.Length - 1].tag = m_madeUpFieldIdentifier;
				rootb.MakeTextSelInObj(0, rgvsli.Length, rgvsli, rgvsli.Length, rgvsli, false, false, false, true, true);
				m_mainView.ScrollSelectionIntoView(rootb.Selection, VwScrollSelOpts.kssoBoth);
				// It's a pity this next step is needed!
				rootb.Activate(VwSelectionState.vssEnabled);
			}
			catch
			{
				// not much we can do to handle errors, but don't let the program die just
				// because the display hasn't yet been laid out, so selections can't fully be
				// created and displayed.
			}
		}

		/// <summary>
		/// We wait until containing controls are laid out to try to scroll our selection into view,
		/// because it depends somewhat on the window being the true size.
		/// </summary>
		public void PostLayoutInit()
		{
			var rootb = (m_mainView as IVwRootSite).RootBox;
			if (rootb?.Selection != null)
			{
				(m_mainView as IVwRootSite).ScrollSelectionIntoView(rootb.Selection, VwScrollSelOpts.kssoBoth);
			}
		}

		protected override void SetupDataContext()
		{
			TriggerMessageBoxIfAppropriate();
			using (new WaitCursor(this))
			{
				MyRecordList.ActivateUI();
				// Enhance JohnT: could use logic similar to RecordView.InitBase to load persisted list contents (filtered and sorted).
				if (MyRecordList.RequestedLoadWhileSuppressed)
				{
					MyRecordList.UpdateList(false);
				}
				m_madeUpFieldIdentifier = MyRecordList.VirtualFlid;
				if (MyRecordList.OwningObject != null)
				{
					m_hvoOwner = MyRecordList.OwningObject.Hvo;
				}
				MyRecordList.IsDefaultSort = false;
				// Create the main view
				// Review JohnT: should it be m_configurationParametersElement or .FirstChild?
				var app = PropertyTable.GetValue<IFlexApp>(LanguageExplorerConstants.App);
				m_mainView = new XmlSeqView(Cache, m_hvoOwner, m_madeUpFieldIdentifier, m_configurationParametersElement, MyRecordList.VirtualListPublisher, app, Publication);
				m_mainView.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
				m_mainView.Dock = DockStyle.Fill;
				m_mainView.Cache = Cache;
				m_mainView.SelectionChangedEvent += OnSelectionChanged;
				m_mainView.MouseClick += m_mainView_MouseClick;
				m_mainView.MouseMove += m_mainView_MouseMove;
				m_mainView.MouseLeave += m_mainView_MouseLeave;
				// This makes selections visible.
				m_mainView.ShowRangeSelAfterLostFocus = true;
				// If the rootsite doesn't have a rootbox, we can't display the record!
				// Calling ShowRecord() from InitBase() sets the state to appear that we have
				// displayed (and scrolled), even though we haven't.  See LT-7588 for the effect.
				m_mainView.MakeRoot();
				SetupStylesheet();
				Controls.Add(m_mainView);
				if (Controls.Count == 2)
				{
					Controls.SetChildIndex(m_mainView, 1);
					m_mainView.BringToFront();
				}
				m_fullyInitialized = true; // Review JohnT: was this really the crucial last step?
			}
		}

		private void m_mainView_MouseLeave(object sender, EventArgs e)
		{
			DisposeTooltip();
		}

		private void m_mainView_MouseMove(object sender, MouseEventArgs e)
		{
			OnMouseMove(e);
		}

		private void m_mainView_MouseClick(object sender, MouseEventArgs e)
		{
			OnMouseClick(e);
		}

		protected override void SetupStylesheet()
		{
			var ss = StyleSheet;
			if (ss != null)
			{
				m_mainView.StyleSheet = ss;
			}
		}

		private LcmStyleSheet StyleSheet => FwUtils.StyleSheetFromPropertyTable(PropertyTable);

		/// <summary>
		///	invoked when our XmlDocView selection changes.
		/// </summary>
		public void OnSelectionChanged(object sender, FwObjectSelectionEventArgs e)
		{
			// paranoid sanity check.
			Debug.Assert(e.Hvo != 0);
			if (e.Hvo == 0)
			{
				return;
			}
			MyRecordList.ViewChangedSelectedRecord(e);
			// Change it if it's actually changed.
			SetInfoBarText();
		}

		/// <summary>
		/// Launch the configure dialog.
		/// </summary>
		internal void ConfigureXmlDocView_Clicked(object sender, EventArgs e)
		{
			RunConfigureDialog(string.Empty);
		}

		private void RunConfigureDialog(string nodePath)
		{
			using (var dlg = new XmlDocConfigureDlg())
			{
				var sProp = XmlUtils.GetOptionalAttributeValue(m_configurationParametersElement, "layoutProperty", "DictionaryPublicationLayout");
				var mainWindow = PropertyTable.GetValue<IFwMainWnd>(FwUtils.window);
				dlg.SetConfigDlgInfo(m_configurationParametersElement, Cache, StyleSheet, mainWindow, PropertyTable, Publisher, sProp);
				dlg.SetActiveNode(nodePath);
				if (dlg.ShowDialog(this) == DialogResult.OK)
				{
					m_mainView.ResetTables(PropertyTable.GetValue<string>(sProp));
					SelectAndScrollToCurrentRecord();
				}
				if (dlg.MasterRefreshRequired)
				{
					mainWindow.RefreshAllViews();
				}
			}
		}

		/// <summary>
		/// Initialize this.
		/// </summary>
		/// <remarks> subclasses must call this from their Init.
		/// This was done, rather than providing an Init() here in the normal way,
		/// to drive home the point that the subclass must set m_fullyInitialized
		/// to true when it is fully initialized.</remarks>
		protected void InitBase()
		{
			Debug.Assert(m_fullyInitialized == false, "No way we are fully initialized yet!");
			ReadParameters();
			SetupDataContext();
			ShowRecord();
		}

		/// In some initialization paths (e.g., when a child of a MultiPane), we don't have
		/// a containing form by the time we get initialized. Try again when our parent is set.
		protected override void OnParentChanged(EventArgs e)
		{
			base.OnParentChanged(e);
			if (m_mainView != null && m_mainView.StyleSheet == null)
			{
				SetupStylesheet();
			}
		}

		#endregion // Other methods

		#region Overrides of ViewBase

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public override void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			base.InitializeFlexComponent(flexComponentParameters);

			InitBase();
			Subscriber.Subscribe("RecordListOwningObjChanged", RecordListOwningObjChanged_Message_Handler);
		}

		#endregion

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.SuspendLayout();
			//
			// RecordView
			//
			this.Name = "RecordView";
			this.Size = new System.Drawing.Size(752, 150);
			this.ResumeLayout(false);

		}
		#endregion

		/// <summary>
		/// Enables the menu that does Find and Replace.
		/// </summary>
		internal bool CanUseReplaceText()
		{
			return !m_mainView.ReadOnlyView;
		}

		public string FindTabHelpId => XmlUtils.GetOptionalAttributeValue(m_configurationParametersElement, "findHelpId", null);
	}
}
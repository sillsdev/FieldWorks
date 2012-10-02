// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: RecordView.cs
// Responsibility: WordWorks
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;
using System.Xml;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FwCoreDlgs;
using XCore;
using SIL.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Common.FwUtils;
using System.Drawing.Printing;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// XmlDocView is a view that shows a complete list as a single view.
	/// A RecordClerk class does most of the work of managing the list and current object.
	///	list management and navigation is entirely(?) handled by the
	/// RecordClerk.
	///
	/// The actual view of each object is specified by a child <jtview></jtview> node
	/// of the view node. This specifies how to display an individual list item.
	/// </summary>
	public class XmlDocView : XWorksViewBase, IFindAndReplaceContext, IPostLayoutInit
	{
		protected int m_hvoOwner; // the root HVO.
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
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ViewManager"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public XmlDocView()
		{
			m_fullyInitialized = false;

			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();


			base.AccNameDefault = "XmlDocView";		// default accessibility name
		}

		#region TitleBar Layout Menu

		/// <summary>
		/// Populate the list of layout views for the second dictionary titlebar menu.
		/// </summary>
		/// <param name="parameter">The parameter.</param>
		/// <param name="display">The display.</param>
		/// <returns></returns>
		public bool OnDisplayLayouts(object parameter, ref UIListDisplayProperties display)
		{
			var layoutList = GatherBuiltInAndUserLayouts();
			foreach (var view in layoutList)
			{
				display.List.Add(view.Item1, view.Item2, null, null, true);
			}
			return true;
		}

		private IEnumerable<Tuple<string, string>> GatherBuiltInAndUserLayouts()
		{
			var layoutList = new List<Tuple<string, string>>();
			var configNode = m_mediator.PropertyTable.GetValue("currentContentControlParameters", null);
			layoutList.AddRange(GetBuiltInLayouts((XmlNode)configNode));
			var builtInLayoutList = new List<string>();
			builtInLayoutList.AddRange(from layout in layoutList select layout.Item2);
			var userLayouts = m_mainView.Vc.LayoutCache.LayoutInventory.GetLayoutTypes();
			layoutList.AddRange(GetUserDefinedDictLayouts(builtInLayoutList, userLayouts));
			return layoutList;
		}

		private static IEnumerable<Tuple<string, string>> GetBuiltInLayouts(XmlNode configNode)
		{
			var configLayouts = XmlUtils.FindNode(configNode, "configureLayouts");
			// The configureLayouts node doesn't always exist!
			if (configLayouts != null)
			{
				var layouts = configLayouts.ChildNodes;
				return ExtractLayoutsFromLayoutTypeList(layouts.Cast<XmlNode>());
			}
				return new List<Tuple<string, string>>();
		}

		private static IEnumerable<Tuple<string, string>> ExtractLayoutsFromLayoutTypeList(IEnumerable<XmlNode> layouts)
		{
			return from XmlNode layout in layouts
				   select new Tuple<string, string>(XmlUtils.GetAttributeValue(layout, "label"),
													XmlUtils.GetAttributeValue(layout, "layout"));
		}

		private static IEnumerable<Tuple<string, string>> GetUserDefinedDictLayouts(
			IEnumerable<string> builtInLayouts,
			IEnumerable<XmlNode> layouts)
		{
			var allUserLayoutTypes = ExtractLayoutsFromLayoutTypeList(layouts);
			var result = new List<Tuple<string, string>>();
			// This part prevents getting Reversal Index layouts or Notebook layouts in our (Dictionary) menu.
			result.AddRange(from layout in allUserLayoutTypes
							where builtInLayouts.Any(builtIn => builtIn == BaseLayoutName(layout.Item2))
							select layout);
			return result;
		}

		private static string BaseLayoutName(string name)
		{
			if (String.IsNullOrEmpty(name))
				return String.Empty;
			// Find out if this layout name has a hashmark (#) in it. Return the part before it.
			var parts = name.Split(Inventory.kcMarkLayoutCopy);
			var result = parts.Length > 1 ? parts[0] : name;
			return result;
		}

		#endregion

		/// <summary>
		/// Populate the list of publications for the first dictionary titlebar menu.
		/// </summary>
		/// <param name="parameter">The parameter.</param>
		/// <param name="display">The display.</param>
		/// <returns></returns>
		public bool OnDisplayPublications(object parameter, ref UIListDisplayProperties display)
		{
			foreach (var pub in Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS)
			{
				var name = pub.Name.UserDefaultWritingSystem.Text;
				display.List.Add(name, name, null, null, true);
			}
			return true;
		}

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
						if (kallEntriesSelectedPublicationValue == pubName)
						{   // A null publication means show everything
							pubDecorator.Publication = null;
							m_mainView.RefreshDisplay();
						}
						else
						{   // look up the publication object
							var pub = (from item in Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS
									   where item.Name.UserDefaultWritingSystem.Text == pubName
									   select item).FirstOrDefault();
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
			return;
		}

		// A string the user is very unlikely to choose as the name of a publication,
		// stored in the property table as the value of SelectedPublication when
		// the separate All Entries menu item is chosen.
		const string kallEntriesSelectedPublicationValue = "$$all_entries$$";

		public virtual bool OnDisplayShowAllEntries(object commandObject, ref UIItemDisplayProperties display)
		{
			var pubName = GetSelectedPublication();
			display.Enabled = true;
			display.Checked = (kallEntriesSelectedPublicationValue == pubName);
			return true;
		}

		public bool OnShowAllEntries(object args)
		{
			m_mediator.PropertyTable.SetProperty("SelectedPublication", kallEntriesSelectedPublicationValue);
			return true;
		}

		public DictionaryPublicationDecorator GetPubDecorator()
		{
			var sda = m_mainView.DataAccess;
			while (sda != null && !(sda is DictionaryPublicationDecorator) && sda is DomainDataByFlidDecoratorBase)
				sda = ((DomainDataByFlidDecoratorBase) sda).BaseSda;
			return sda as DictionaryPublicationDecorator;
		}

		ICmPossibility Publication
		{
			get
			{
				var pubName = GetSelectedPublication();
				var pub = (from item in Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS
					where item.Name.UserDefaultWritingSystem.Text == pubName
					select item).FirstOrDefault();
				return pub;
			}
		}

		private string GetSelectedConfigView()
		{
			string sLayoutType = null;
			if (m_mediator != null && m_mediator.PropertyTable != null)
			{
				sLayoutType = m_mediator.PropertyTable.GetStringProperty("DictionaryPublicationLayout",
					String.Empty);
			}
			if (String.IsNullOrEmpty(sLayoutType))
				sLayoutType = "publishStem";
			return sLayoutType;
		}

		private string GetSelectedPublication()
		{
			// Sometimes we just want the string value which might be '$$all_entries$$'
			return m_mediator.PropertyTable.GetStringProperty("SelectedPublication",
				kallEntriesSelectedPublicationValue);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged
		/// resources; <c>false</c> to release only unmanaged resources.
		/// </param>
		/// -----------------------------------------------------------------------------------
		protected override void Dispose( bool disposing )
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if( disposing )
			{
				DisposeTooltip();
				if(components != null)
					components.Dispose();
			}
			m_currentObject = null;

			base.Dispose( disposing );
		}

		#endregion // Consruction and disposal

		#region Properties

		private Control TitleBar
		{
			// XmlDocView probably isn't supposed to know how to get this...
			// but I need it.
			get { return m_informationBar.Controls[0]; }
		}

		private Font TitleBarFont
		{
			get { return TitleBar.Font; }
		}

		#endregion Properties

		#region Other methods

		protected override void SetInfoBarText()
		{
			if (m_informationBar == null)
				return;

			var context = XmlUtils.GetOptionalAttributeValue(m_configurationParameters, "persistContext", "");
			// SetInfoBarText() was getting run about 4 times just creating one XmlDocView!
			// To prevent that, add the following guards:
			if (m_titleStr != null && NoReasonToChangeTitle(context))
				return;
			string titleStr = "";
			// See if we have an AlternativeTitle string table id for an alternate title.
			string titleId = XmlUtils.GetAttributeValue(m_configurationParameters,
				"altTitleId");
			if (titleId != null)
			{
				titleStr = StringTbl.GetString(titleId, "AlternativeTitles");
			}
			else if (Clerk.OwningObject != null)
			{

				if (XmlUtils.GetBooleanAttributeValue(m_configurationParameters,
					"ShowOwnerShortname"))
				{
					titleStr = Clerk.OwningObject.ShortName;
				}
			}

			bool fBaseCalled = false;
			if (titleStr == string.Empty)
			{
				base.SetInfoBarText();
				fBaseCalled = true;
				//				titleStr = ((IPaneBar)m_informationBar).Text;	// can't get to work.
				// (EricP) For some reason I can't provide an IPaneBar get-accessor to return
				// the new Text value. If it's desirable to allow TitleFormat to apply to
				// Clerk.CurrentObject, then we either have to duplicate what the
				// base.SetInfoBarText() does here, or get the string set by the base.
				// for now, let's just return.
				if (titleStr == null || titleStr == string.Empty)
					return;
			}
			switch (context)
			{
				case "Dict":
					m_currentPublication = GetSelectedPublication();
					m_currentConfigView = GetSelectedConfigView();
					titleStr = MakePublicationTitlePart(titleStr);
					SetConfigViewTitle();
					break;
				case "Reversal":
					m_currentPublication = GetSafeWsName();
					titleStr = String.Format(xWorksStrings.ksXReversalIndex, m_currentPublication, titleStr);
					break;
				default:
					// Some other type like Notebook; drop through.
					break;
			}

			// If we have a format attribute, format the title accordingly.
			string sFmt = XmlUtils.GetAttributeValue(m_configurationParameters,
				"TitleFormat");
			if (sFmt != null)
			{
				titleStr = String.Format(sFmt, titleStr);
			}

			// if we haven't already set the text through the base,
			// or if we had some formatting to do, then set the infoBar text.
			if (!fBaseCalled || sFmt != null)
				((IPaneBar)m_informationBar).Text = titleStr;
			m_titleStr = titleStr;
		}

		#region Dictionary View TitleBar stuff

		private void ResetSpacer(int spacerWidth, string activeLayoutName)
		{
			var bar = TitleBar;
			if (bar is Panel && bar.Controls.Count > 1)
			{
				var cctrls = bar.Controls.Count;
				bar.Controls[cctrls - 1].Width = spacerWidth;
				bar.Controls[cctrls - 1].Text = activeLayoutName;
			}
		}

		private const int kSpaceForMenuButton = 26;

		private void SetConfigViewTitle()
		{
			if (!String.IsNullOrEmpty(m_currentConfigView))
			{
				var maxLayoutViewWidth = Width/2 - kSpaceForMenuButton;
				var result = GatherBuiltInAndUserLayouts();
				var curViewName = FindViewNameInList(result);
				// Limit length of View title to remaining available width
				curViewName = TrimToMaxPixelWidth(Math.Max(2, maxLayoutViewWidth), curViewName);
				ResetSpacer(maxLayoutViewWidth, curViewName);
			}
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			m_titleStr = null;
			SetInfoBarText();
		}

		private string FindViewNameInList(IEnumerable<Tuple<string, string>> layoutList)
		{
			var result = "";
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
			var maxPublicationTitleWidth = Math.Max(2, Width/2 - kSpaceForMenuButton);
			if (String.IsNullOrEmpty(m_currentPublication) ||
				m_currentPublication == kallEntriesSelectedPublicationValue)
			{
				m_currentPublication = kallEntriesSelectedPublicationValue;
				titleStr = xWorksStrings.ksAllEntries;
				// Limit length of Publication title to half of available width
				titleStr = TrimToMaxPixelWidth(maxPublicationTitleWidth, titleStr);
			}
			else
			{
				titleStr = String.Format(xWorksStrings.ksPublicationEntries,
					Publication.Name.BestAnalysisAlternative.Text, titleStr);
				titleStr = TrimToMaxPixelWidth(maxPublicationTitleWidth, titleStr);
			}
			return titleStr;
		}

		private int GetWidthOfStringInPixels(string sInput)
		{
			using (var g = Graphics.FromHwnd(Handle))
			{
				return Convert.ToInt32(g.MeasureString(sInput, TitleBarFont).Width);
			}
		}

		private const string kEllipsis = "...";
		private string TrimToMaxPixelWidth(int pixelWidthAllowed, string sToTrim)
		{
			int sPixelWidth;
			int charsAllowed;

			if (sToTrim.Length == 0)
				return sToTrim;

			sPixelWidth = GetWidthOfStringInPixels(sToTrim);
			var avgPxPerChar = sPixelWidth / Convert.ToSingle(sToTrim.Length);
			charsAllowed = Convert.ToInt32(pixelWidthAllowed / avgPxPerChar);
			if (charsAllowed < 5)
				return String.Empty;
			return sPixelWidth < pixelWidthAllowed ? sToTrim : sToTrim.Substring(0, charsAllowed-4) + kEllipsis;
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
				return true;
			var wsName = GetSafeWsName();
			return m_currentPublication == null || m_currentPublication != wsName;
		}

		private string GetSafeWsName()
		{
			if (m_currentObject == null || !m_currentObject.IsValidObject)
			{
				if (m_hvoOwner < 1)
					return String.Empty;
				return WritingSystemServices.GetReversalIndexWritingSystems(
					Cache, m_hvoOwner, false)[0].LanguageName;
			}
			return WritingSystemServices.GetReversalIndexEntryWritingSystem(
				Cache,
				m_currentObject.Hvo,
				Cache.LangProject.CurrentAnalysisWritingSystems[0]).LanguageName;
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
			string backColorName = XmlUtils.GetOptionalAttributeValue(m_configurationParameters,
				"backColor", "Window");
			BackColor = Color.FromName(backColorName);
			m_configObjectName = XmlUtils.GetLocalizedAttributeValue(m_mediator.StringTbl,
				m_configurationParameters, "configureObjectName", null);
		}

		public virtual bool OnRecordNavigation(object argument)
		{
			CheckDisposed();

			if (!m_fullyInitialized
				|| RecordNavigationInfo.GetSendingClerk(argument) != Clerk) // Don't pretend to have handled it if it isn't our clerk.
				return false;

			// persist Clerk's CurrentIndex in a db specific way
			string propName = Clerk.PersistedIndexProperty;
			m_mediator.PropertyTable.SetProperty(propName, Clerk.CurrentIndex, PropertyTable.SettingsGroup.LocalSettings);
			m_mediator.PropertyTable.SetPropertyPersistence(propName, true, PropertyTable.SettingsGroup.LocalSettings);

			Clerk.SuppressSaveOnChangeRecord = (argument as RecordNavigationInfo).SuppressSaveOnChangeRecord;
			using (WaitCursor wc = new WaitCursor(this))
			{
				//DateTime dt0 = DateTime.Now;
				try
				{
					ShowRecord();
				}
				finally
				{
					Clerk.SuppressSaveOnChangeRecord = false;
				}
				//DateTime dt1 = DateTime.Now;
				//TimeSpan ts = TimeSpan.FromTicks(dt1.Ticks - dt0.Ticks);
				//Debug.WriteLine("XmlDocView.OnRecordNavigation(): ShowRecord() took " + ts.ToString() + " at " + dt1.ToString());
			}
			return true;	//we handled this.
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
					return;
				if (XmlUtils.FindNode(m_configurationParameters, "configureLayouts") == null)
					return; // view is not configurable, don't show menu option.

				int hvo, tag, ihvo, cpropPrevious;
				IVwPropertyStore propStore;
				sel.PropInfo(false, 0, out hvo, out tag, out ihvo, out cpropPrevious, out propStore);
				string nodePath = null;
				if (propStore != null)
				{
					nodePath = propStore.get_StringProperty((int) FwTextPropType.ktptBulNumTxtBef);
				}
				if (string.IsNullOrEmpty(nodePath))
				{
					// may be a literal string, where we can get it from the string itself.
					ITsString tss;
					int ich, ws;
					bool fAssocPrev;
					sel.TextSelInfo(false, out tss, out ich, out fAssocPrev, out hvo, out tag, out ws);
					nodePath = tss.get_Properties(0).GetStrPropValue((int) FwTextPropType.ktptBulNumTxtBef);
				}
				string label;
				if (string.IsNullOrEmpty(nodePath))
					label = String.Format(xWorksStrings.ksConfigure, m_configObjectName);
				else
					label = string.Format(xWorksStrings.ksConfigureIn, nodePath.Split(':')[3], m_configObjectName);
				var m_contextMenu = new ContextMenuStrip();
				var item = new ToolStripMenuItem(label);
				m_contextMenu.Items.Add(item);
				item.Click += RunConfigureDialogAt;
				item.Tag = nodePath;
				m_contextMenu.Show(m_mainView, e.Location);
				m_contextMenu.Closed += m_contextMenu_Closed;
			}
			else
			{
				base.OnMouseClick(e); // be nice to do this anyway, but it does undesirable highlighting.
			}
		}

		void m_contextMenu_Closed(object sender, ToolStripDropDownClosedEventArgs e)
		{
			Application.Idle += DisposeContextMenu;
		}

		void DisposeContextMenu(object sender, EventArgs e)
		{
			Application.Idle -= DisposeContextMenu;
			if (m_contextMenu != null)
			{
				m_contextMenu.Dispose();
				m_contextMenu = null;
			}
		}

		// Context menu exists just for one invocation (until idle).
		private ContextMenuStrip m_contextMenu;

		void RunConfigureDialogAt(object sender, EventArgs e)
		{
			var item = (ToolStripMenuItem) sender;
			var nodePath = (string) item.Tag;
			RunConfigureDialog(nodePath);
		}

		private void TryToJumpToSelection(Point where)
		{
			var obj = SubitemClicked(where, Clerk.ListItemsClass);
			if (obj != null && obj.Hvo != Clerk.CurrentObjectHvo)
			Clerk.JumpToRecord(obj.Hvo);
		}

		/// <summary>
		/// Return the most specific object identified by a click at the specified position.
		/// An object is considered indicated if it is or has an owner of the specified class.
		/// The object must be different from the outermost indicated object in the selection.
		/// </summary>
		internal ICmObject SubitemClicked(Point where, int clsid)
		{
			return SubitemClicked(where, clsid, m_mainView, Cache);
		}

		private ToolTip m_tooltip;
		private Point m_lastActiveLocation;

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			// don't try to update the tooltip by getting a selection while painting the view; leads to recursive expansion of lazy boxes.
			if (m_mainView.MouseMoveSuppressed)
				return;
			var item = SubitemClicked(e.Location, Clerk.ListItemsClass);
			if (item == null || item.Hvo == Clerk.CurrentObjectHvo)
			{
				if (m_tooltip != null)
					m_tooltip.Active = false;
			}
			else
			{
				if (m_tooltip == null)
				{
					m_tooltip = new ToolTip();
					m_tooltip.InitialDelay = 10;
					m_tooltip.ReshowDelay = 10;
					m_tooltip.SetToolTip(m_mainView, xWorksStrings.ksCtrlClickJumpTooltip);
				}
				if (m_tooltip.Active)
				{
					var relLocation = e.Location;
					if (Math.Abs(m_lastActiveLocation.X - e.X) > 20 || Math.Abs(m_lastActiveLocation.Y - e.Y) > 10)
					{
						m_tooltip.Show(xWorksStrings.ksCtrlClickJumpTooltip, m_mainView, relLocation.X, relLocation.Y, 2000);
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
				return;
			m_tooltip.Active = false;
			m_tooltip.Dispose();
			m_tooltip = null;
		}

		/// <summary>
		/// Return an item of the specified class that is indicated by a click at the specified position,
		/// but only if it is part of a different object also of that class.
		/// </summary>
		internal static ICmObject SubitemClicked(Point where, int clsid, SimpleRootSite view, FdoCache cache)
		{
			var sel = view.GetSelectionAtPoint(where, false);
			if (sel == null)
				return null;
			Rect rcPrimary = view.GetPrimarySelRect(sel);
			Rectangle selRect = new Rectangle(rcPrimary.left, rcPrimary.top, rcPrimary.right - rcPrimary.left, rcPrimary.bottom - rcPrimary.top);
			selRect.Inflate(8,2);
			if (!selRect.Contains(where))
				return null; // off somewhere in white space, tooltip is confusing
			var helper = SelectionHelper.Create(sel, view);
			var levels = helper.GetLevelInfo(SelectionHelper.SelLimitType.Anchor);
			ICmObject firstMatch = null;
			ICmObject lastMatch = null;
			foreach (var info in levels)
			{
				int hvo = info.hvo;
				if (!cache.ServiceLocator.IsValidObjectId(hvo))
					continue; // may be some invalid numbers in there
				var obj = cache.ServiceLocator.GetObject(hvo);
				var target = GetTarget(obj, clsid);
				if (target == null)
					continue; // nothing interesting at this level.
				lastMatch = target; // last one we've seen.
				if (firstMatch == null)
					firstMatch = target; // first one we've seen
			}
			if (firstMatch != lastMatch)
				return firstMatch;
			return null;
		}

		static ICmObject GetTarget(ICmObject obj, int clsid)
		{
			if (obj.ClassID == clsid)
				return obj;
			if (obj.OwnerOfClass(clsid) != null)
				return obj.OwnerOfClass(clsid);
			return null;
		}

		public bool OnClerkOwningObjChanged(object sender)
		{
			CheckDisposed();

			if (this.Clerk != sender || (m_mainView == null))
				return false;

			if (Clerk.OwningObject == null)
			{
				//this happens, for example, when they user sets a filter on the
				//list we are dependent on, but no records are selected by the filter.
				//thus, we now do not have an object to get records out of,
				//so we need to just show a blank list.
				this.m_hvoOwner = -1;
			}
			else
			{
				m_hvoOwner = Clerk.OwningObject.Hvo;
				m_mainView.ResetRoot(m_hvoOwner);
				SetInfoBarText();
			}
			return false; //allow others clients of this clerk to know about it as well.
		}

		/// <summary>
		/// By default this returns Clerk.CurrentIndex. However, when we are using a decorator
		/// for the view, we may need to adjust the index.
		/// </summary>
		int AdjustedClerkIndex()
		{
			var sda = m_mainView.DataAccess as ISilDataAccessManaged;
			if (sda == null || sda == Clerk.VirtualListPublisher)
				return Clerk.CurrentIndex; // no tricks.
			if (Clerk.CurrentObjectHvo == 0)
				return -1;
			var items = sda.VecProp(m_hvoOwner, m_fakeFlid);
			// Search for the indicated item, working back from the place we expect it to be.
			// This is efficient, because usually only a few items are filtered and it will be close.
			// Also, currently the decorator only removes items, so we won't find it at a larger index.
			// Finally, if there are duplicates, we will find the one closest to the expected position.
			int target = Clerk.CurrentObjectHvo;
			int index = Math.Min(Clerk.CurrentIndex, items.Length - 1);
			while (index >= 0 && items[index] != target)
				index--;
			if (index < 0 && sda.get_VecSize(m_hvoOwner, m_fakeFlid) > 0)
				return 0; // can we do better? The object selected in other views is hidden in this.
			return index;
		}

		protected override void ShowRecord()
		{
			RecordClerk clerk = Clerk;

			var currentIndex = AdjustedClerkIndex();

			// See if it is showing the same record, as before.
			if (m_currentObject != null && clerk.CurrentObject != null
				&& m_currentIndex == currentIndex
				&& m_currentObject.Hvo == clerk.CurrentObject.Hvo)
			{
				SetInfoBarText();
				return;
			}

			// See if the main owning object has changed.
			if (clerk.OwningObject.Hvo != m_hvoOwner)
			{
				m_hvoOwner = clerk.OwningObject.Hvo;
				m_mainView.ResetRoot(m_hvoOwner);
			}

			m_currentObject = clerk.CurrentObject;
			m_currentIndex = currentIndex;
			//add our current state to the history system
			string toolName = m_mediator.PropertyTable.GetStringProperty(
				"currentContentControl", "");
			Guid guid = Guid.Empty;
			if (clerk.CurrentObject != null)
				guid = clerk.CurrentObject.Guid;
			m_mediator.SendMessage("AddContextToHistory", new FwLinkArgs(toolName, guid), false);

			SelectAndScrollToCurrentRecord();
			base.ShowRecord();
		}

		private enum ExclusionReasonCode
		{
			NotExcluded,
			NotInPublication,
			ExcludedHeadword,
			ExcludedMinorEntry
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
		public bool OnCheckJump(object argument)
		{
			var hvoTarget = (int)argument;
			var currControl = m_mediator.PropertyTable.GetStringProperty("currentContentControl", "");
			// Currently this (LT-11447) only applies to Dictionary view
			if (hvoTarget > 0 && currControl == ksLexDictionary)
			{
				ExclusionReasonCode xrc;
				// Make sure we explain to the user in case hvoTarget is not visible due to
				// the current Publication layout or Configuration view.
				if (!IsObjectVisible(hvoTarget, out xrc))
				{
					// Tell the user why we aren't jumping to his record
					GiveSimpleWarning(xrc);
				}
			}
			return true;
		}

		private void GiveSimpleWarning(ExclusionReasonCode xrc)
		{
			// Tell the user why we aren't jumping to his record
			var msg = xWorksStrings.ksSelectedEntryNotInDict;
			string caption;
			string reason;
			string shlpTopic;
			switch (xrc)
			{
				case ExclusionReasonCode.NotInPublication:
					caption = xWorksStrings.ksEntryNotPublished;
					reason = xWorksStrings.ksEntryNotPublishedReason;
					shlpTopic = "khtpEntryNotPublished";
					break;
				case ExclusionReasonCode.ExcludedHeadword:
					caption = xWorksStrings.ksMainNotShown;
					reason = xWorksStrings.ksMainNotShownReason;
					shlpTopic = "khtpMainEntryNotShown";
					break;
				case ExclusionReasonCode.ExcludedMinorEntry:
					caption = xWorksStrings.ksMinorNotShown;
					reason = xWorksStrings.ksMinorNotShownReason;
					shlpTopic = "khtpMinorEntryNotShown";
					break;
				default:
					throw new ArgumentException("Unknown ExclusionReasonCode");
			}
			msg = String.Format(msg, reason);
			MessageBox.Show(FindForm(), msg, caption, MessageBoxButtons.OK,
							MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, 0,
							m_mediator.HelpTopicProvider.HelpFile,
							HelpNavigator.Topic, shlpTopic);
		}

		private bool IsObjectVisible(int hvoTarget, out ExclusionReasonCode xrc)
		{
			xrc = ExclusionReasonCode.NotExcluded;
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
			if (m_currentPublication != kallEntriesSelectedPublicationValue)
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
			if (entry.EntryRefsOS.Count > 0 && !entry.PublishAsMinorEntry && IsRootBasedView)
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
				if (String.IsNullOrEmpty(m_currentConfigView))
					return false;

				return m_currentConfigView.Split(
					new[] {"#"}, StringSplitOptions.None)[0] == ksRootBasedPrefix;
			}
		}

		/// <summary>
		/// Ensure that we have the current record selected and visible in the window.  See LT-9109.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			if (m_mainView != null && m_mainView.RootBox != null && !m_mainView.PaintInProgress && !m_mainView.LayoutInProgress
				&& m_mainView.RootBox.Selection == null)
			{
				SelectAndScrollToCurrentRecord();
			}
		}

		/// <summary>
		/// Pass command (from RecordEditView) on to printing view.
		/// </summary>
		/// <param name="pd">PrintDocument</param>
		/// <param name="recordHvo"></param>
		public void PrintFromDetail(PrintDocument pd, int recordHvo)
		{
			m_mainView.PrintFromDetail(pd, recordHvo);
		}

		private void SelectAndScrollToCurrentRecord()
		{
			// Scroll the display to the given record.  (See LT-927 for explanation.)
			try
			{
				RecordClerk clerk = Clerk;
				int levelFlid = 0;
				var indexes = new List<int>();
				if (Clerk is SubitemRecordClerk)
				{
					var subitemClerk = Clerk as SubitemRecordClerk;
					levelFlid = subitemClerk.SubitemFlid;
					if (subitemClerk.Subitem != null)
					{
						// There's a subitem. See if we can select it.
						var item = subitemClerk.Subitem;
						while (item.OwningFlid == levelFlid)
						{
							indexes.Add(item.OwnOrd);
							item = item.Owner;
						}
					}
				}
				var currentIndex = AdjustedClerkIndex();
				indexes.Add(currentIndex);
				// Suppose it is the fifth subrecord of the second subrecord of the ninth main record.
				// At this point, indexes holds 4, 1, 8. That is, like information for MakeSelection,
				// it holds the indexes we want to select from innermost to outermost.
				IVwRootBox rootb = (m_mainView as IVwRootSite).RootBox;
				if (rootb != null)
				{
					int idx = currentIndex;
					if (idx < 0)
						return;
					// Review JohnT: is there a better way to obtain the needed rgvsli[]?
					IVwSelection sel = rootb.Selection;
					if (sel != null)
					{
						// skip moving the selection if it's already in the right record.
						int clevels = sel.CLevels(false);
						if (clevels >= indexes.Count)
						{
							for (int ilevel = indexes.Count - 1; ilevel >= 0; ilevel--)
							{
								int hvoObj, tag, ihvo, cpropPrevious;
								IVwPropertyStore vps;
								sel.PropInfo(false, clevels - indexes.Count + ilevel, out hvoObj, out tag, out ihvo,
									out cpropPrevious, out vps);
								if (ihvo != indexes[ilevel] || tag != levelFlid)
									break;
								if (ilevel == 0)
								{
									// selection is already in the right object, just make sure it's visible.
									(m_mainView as IVwRootSite).ScrollSelectionIntoView(sel,
										VwScrollSelOpts.kssoDefault);
									return;
								}
							}
						}
					}
					var rgvsli = new SelLevInfo[indexes.Count];
					for (int i = 0; i < indexes.Count; i++)
					{
						rgvsli[i].ihvo = indexes[i];
						rgvsli[i].tag = levelFlid;
					}
					rgvsli[rgvsli.Length-1].tag = m_fakeFlid;
					rootb.MakeTextSelInObj(0, rgvsli.Length, rgvsli, rgvsli.Length, rgvsli, false, false, false, true, true);
					m_mainView.ScrollSelectionIntoView(rootb.Selection, VwScrollSelOpts.kssoBoth);

					// It's a pity this next step is needed!
					rootb.Activate(VwSelectionState.vssEnabled);
				}
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
			IVwRootBox rootb = (m_mainView as IVwRootSite).RootBox;
			if (rootb != null && rootb.Selection != null)
				(m_mainView as IVwRootSite).ScrollSelectionIntoView(rootb.Selection, VwScrollSelOpts.kssoBoth);
		}

		protected override void SetupDataContext()
		{
			TriggerMessageBoxIfAppropriate();
			System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;

			//m_flid = RecordClerk.GetFlidOfVectorFromName(m_vectorName, Cache, out m_owningObject);
			RecordClerk clerk = Clerk;
			clerk.ActivateUI(false);
			// Enhance JohnT: could use logic similar to RecordView.InitBase to load persisted list contents (filtered and sorted).
			if (clerk.RequestedLoadWhileSuppressed)
				clerk.UpdateList(false);
			m_fakeFlid = clerk.VirtualFlid;
			m_hvoOwner = clerk.OwningObject.Hvo;
			if (!clerk.SetCurrentFromRelatedClerk())
			{
				// retrieve persisted clerk index and set it.
				int idx = m_mediator.PropertyTable.GetIntProperty(clerk.PersistedIndexProperty, -1,
					PropertyTable.SettingsGroup.LocalSettings);
				if (idx >= 0 && !clerk.HasEmptyList)
				{
					int idxOld = clerk.CurrentIndex;
					try
					{
						clerk.JumpToIndex(idx);
					}
					catch
					{
						clerk.JumpToIndex(idxOld >= 0 ? idxOld : 0);
					}
				}
				clerk.SelectedRecordChanged(false);
			}

			clerk.IsDefaultSort = false;

			// Create the main view

			// Review JohnT: should it be m_configurationParameters or .FirstChild?
			IApp app = (IApp)m_mediator.PropertyTable.GetValue("App");
			m_mainView = new XmlSeqView(Cache, m_hvoOwner, m_fakeFlid, m_configurationParameters, Clerk.VirtualListPublisher, app,
				Publication);
			m_mainView.Init(m_mediator, m_configurationParameters); // Required call to xCore.Colleague.
			m_mainView.Dock = DockStyle.Fill;
			m_mainView.Cache = Cache;
			m_mainView.SelectionChangedEvent +=
				new FwSelectionChangedEventHandler(OnSelectionChanged);
			m_mainView.MouseClick += m_mainView_MouseClick;
			m_mainView.MouseMove += m_mainView_MouseMove;
			m_mainView.MouseLeave += m_mainView_MouseLeave;
			m_mainView.ShowRangeSelAfterLostFocus = true;	// This makes selections visible.
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

		void m_mainView_MouseLeave(object sender, EventArgs e)
		{
			DisposeTooltip();
		}

		void m_mainView_MouseMove(object sender, MouseEventArgs e)
		{
			OnMouseMove(e);
		}

		void m_mainView_MouseClick(object sender, MouseEventArgs e)
		{
			OnMouseClick(e);
		}

		protected override void SetupStylesheet()
		{
			FwStyleSheet ss = StyleSheet;
			if (ss != null)
				m_mainView.StyleSheet = ss;
		}

		private FwStyleSheet StyleSheet
		{
			get
			{
				return FontHeightAdjuster.StyleSheetFromMediator(m_mediator);
			}
		}

		/// <summary>
		///	invoked when our XmlDocView selection changes.
		/// </summary>
		/// <param name="sender">unused</param>
		/// <param name="e">the event arguments</param>
		public void OnSelectionChanged(object sender, FwObjectSelectionEventArgs e)
		{
			CheckDisposed();

			// paranoid sanity check.
			Debug.Assert(e.Hvo != 0);
			if (e.Hvo == 0)
				return;
			Clerk.ViewChangedSelectedRecord(e, m_mainView.RootBox.Selection);
			// Change it if it's actually changed.
			SetInfoBarText();
		}

		/// <summary>
		/// The configure dialog may be launched any time this tool is active.
		/// Its name is derived from the name of the tool.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayConfigureXmlDocView(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			if (m_configObjectName == null || m_configObjectName == "")
			{
				display.Enabled = display.Visible = false;
				return true;
			}
			display.Enabled = true;
			display.Visible = true;
			// Enhance JohnT: make this configurable. We'd like to use the 'label' attribute of the 'tool'
			// element, but we don't have it, only the two-level-down 'parameters' element
			// so use "configureObjectName" parameter for now.
			// REVIEW: FOR LOCALIZABILITY, SHOULDN'T THE "..." BE PART OF THE SOURCE FOR display.Text?
			display.Text = String.Format(display.Text, m_configObjectName + "...");
			return true; //we've handled this
		}

		/// <summary>
		/// Launch the configure dialog.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <returns></returns>
		public bool OnConfigureXmlDocView(object commandObject)
		{
			CheckDisposed();

			RunConfigureDialog("");
			return true; // we handled it
		}

		private void RunConfigureDialog(string nodePath)
		{
			using (XmlDocConfigureDlg dlg = new XmlDocConfigureDlg())
			{
				string sProp = XmlUtils.GetOptionalAttributeValue(m_configurationParameters, "layoutProperty");
				if (String.IsNullOrEmpty(sProp))
					sProp = "DictionaryPublicationLayout";
				dlg.SetConfigDlgInfo(m_configurationParameters, Cache, StyleSheet,
					FindForm() as IMainWindowDelegateCallbacks, m_mediator, sProp);
				dlg.SetActiveNode(nodePath);
				if (dlg.ShowDialog(this) == DialogResult.OK)
				{
					string sNewLayout = m_mediator.PropertyTable.GetStringProperty(sProp, null);
					m_mainView.ResetTables(sNewLayout);
					SelectAndScrollToCurrentRecord();
				}
				if (dlg.MasterRefreshRequired)
					m_mediator.SendMessage("MasterRefresh", null);
			}
		}

		/// <summary>
		/// Initialize this as an IxCoreColleague
		/// </summary>
		/// <remarks> subclasses must call this from their Init.
		/// This was done, rather than providing an Init() here in the normal way,
		/// to drive home the point that the subclass must set m_fullyInitialized
		/// to true when it is fully initialized.</remarks>
		/// <param name="mediator"></param>
		/// <param name="configurationParameters"></param>
		protected void InitBase(Mediator mediator, XmlNode configurationParameters)
		{
			Debug.Assert(m_fullyInitialized == false, "No way we are fully initialized yet!");

			m_mediator = mediator;
			base.m_configurationParameters = configurationParameters;

			ReadParameters();

			m_mediator.AddColleague(this);

			m_mediator.PropertyTable.SetProperty("ShowRecordList", false);

			SetupDataContext();
			ShowRecord();
		}

		/// In some initialization paths (e.g., when a child of a MultiPane), we don't have
		/// a containing form by the time we get initialized. Try again when our parent is set.
		protected override void OnParentChanged(EventArgs e)
		{
			base.OnParentChanged(e);
			if (m_mainView != null && m_mainView.StyleSheet == null)
				SetupStylesheet();
		}

		#endregion // Other methods

		#region IxCoreColleague implementation

		public override void Init(Mediator mediator, XmlNode configurationParameters)
		{
			CheckDisposed();

			InitBase(mediator, configurationParameters);
		}

		/// <summary>
		/// subclasses should override if they have more targets
		/// </summary>
		/// <returns></returns>
		protected override void GetMessageAdditionalTargets(List<IxCoreColleague> collector)
		{
			collector.Add(m_mainView);
		}

		#endregion // IxCoreColleague implementation

		#region Component Designer generated code
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
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
		///	see if it makes sense to provide the "delete record" command now.
		///	Currently we don't support this in document view, except for reversal entries.
		///	If we decide to support it, we will need to do additional work
		///	(cf LT-1222) to ensure that
		///		(a) The clerk's idea of the current entry corresponds to where the selection is
		///		(b) After deleting it, the clerk's list gets updated.
		///	The former is not happening because we haven't written a SelectionChange method
		///	to notice the selection in the view and change the clerk to match.
		///	Not sure why the clerk's list isn't being updated...it may be only a problem
		///	for homographs.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public bool OnDisplayDeleteRecord(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			display.Enabled = false;
			// Don't claim to have handled it if the clerk is holding reversal entries.
			return !(Clerk.Id == "reversalEntries");
		}

		/// <summary>
		/// Implements the command that just does Find, without Replace.
		/// </summary>
		public bool OnFindAndReplaceText(object argument)
		{
			return ((IFwMainWnd)ParentForm).OnEditFind(argument);
		}

		/// <summary>
		/// Enables the command that just does Find, without Replace.
		/// </summary>
		public virtual bool OnDisplayFindAndReplaceText(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = display.Visible = (ParentForm is FwXWindow);
			return true; //we've handled this
		}

		/// <summary>
		/// Implements the command that just does Find, without Replace.
		/// </summary>
		public bool OnReplaceText(object argument)
		{
			return ((FwXWindow)ParentForm).OnEditReplace(argument);
		}

		/// <summary>
		/// Enables the command that just does Find, without Replace.
		/// </summary>
		public virtual bool OnDisplayReplaceText(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = !m_mainView.ReadOnlyView;
			return true; //we've handled this
		}

		/// <summary>
		/// If this gets called (which it never should), just say we did it, unless we are in the context of reversal entries.
		/// In the case of reversal entries, we say we did not do it, so the record clerk deals with it.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <returns></returns>
		public bool OnDeleteRecord(object commandObject)
		{
			CheckDisposed();

			if (Clerk.Id == "reversalEntries")
			{
				return false; // Let the clerk do it.
			}
			else
			{
				Debug.Assert(false);
			}
			return true;
		}

		public string FindTabHelpId
		{
			get { return XmlUtils.GetOptionalAttributeValue(m_configurationParameters, "findHelpId", null); }
		}
	}
}

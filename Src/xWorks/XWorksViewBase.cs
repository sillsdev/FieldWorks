// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: RecordView.cs
// Responsibility: WordWorks
// Last reviewed:
//
// <remarks>
// </remarks>
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Controls;

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
	public abstract class XWorksViewBase : MainUserControl, IMainContentControl, IPaneBarUser
	{
		#region Enumerations

		public enum TreebarAvailability {Required, Optional, NotAllowed, NotMyBusiness};

		#endregion Enumerations

		#region Event declaration

		#endregion Event declaration

		#region Data members
#if !RANDYTODO
		/// <summary>
		/// Get rid of this member.
		/// </summary>
		protected XmlNode m_configurationParameters;
#endif
		/// <summary>
		/// Optional information bar above the main control.
		/// </summary>
		protected UserControl m_informationBar;
		/// <summary>
		/// Name of the vector we are editing.
		/// </summary>
		protected string m_vectorName;
		/// <summary/>
		protected int m_fakeFlid; // the list
		/// <summary>
		/// This is used to keep us from responding to messages that we get while
		/// we are still trying to get initialized.
		/// </summary>
		protected bool m_fullyInitialized;
		/// <summary>
		/// tell whether the tree bar is required, optional, or not allowed for this view
		/// </summary>
		protected TreebarAvailability m_treebarAvailability;
#if RANDYTODO
			// TODO: Block for now, to not have xWorks take a new dependency on LanguageExplorer
			// TODO: Expected disposition:
			//	TODO: 1. Move this file into LanguageExplorer, and/or
			//	TODO: 2. Use interface instead of actual MultiPane class.

		/// <summary>
		/// Last known parent that is a MultiPane.
		/// </summary>
		private MultiPane m_mpParent;
#endif
		///// <summary>
		///// Right-click menu for deleting Custom lists.
		///// </summary>
		//private ContextMenuStrip m_contextMenu;
		///// <summary>
		///// Keeps track of when the context menu last closed.
		///// </summary>
		//private long m_ticksWhenContextMenuClosed = 0;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		/// <summary>
		/// Caches the RecordClerk.
		/// </summary>
		private RecordClerk m_clerk;

		/// <summary>
		/// Sometimes an active clerk (eg., in a view) is repurposed (eg., in a dialog for printing).
		/// When finished, clerk.BecomeInactive() is called, but that causes records not to be shown
		/// in the active view. This gaurd prevents that.
		/// </summary>
		private bool m_haveActiveClerk = false;

		#endregion Data members

		#region Implementation of IPropertyTableProvider

		/// <summary>
		/// Placement in the IPropertyTableProvider interface lets FwApp call IPropertyTable.DoStuff.
		/// </summary>
		public IPropertyTable PropertyTable { get; private set; }

		#endregion

		#region Implementation of IPublisherProvider

		/// <summary>
		/// Get the IPublisher.
		/// </summary>
		public IPublisher Publisher { get; private set; }

		#endregion

		#region Implementation of ISubscriberProvider

		/// <summary>
		/// Get the ISubscriber.
		/// </summary>
		public ISubscriber Subscriber { get; private set; }

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="propertyTable">Interface to a property table.</param>
		/// <param name="publisher">Interface to the publisher.</param>
		/// <param name="subscriber">Interface to the subscriber.</param>
		public virtual void InitializeFlexComponent(IPropertyTable propertyTable, IPublisher publisher, ISubscriber subscriber)
		{
			FlexComponentCheckingService.CheckInitializationValues(propertyTable, publisher, subscriber, PropertyTable, Publisher, Subscriber);

			PropertyTable = propertyTable;
			Publisher = publisher;
			Subscriber = subscriber;
		}

		#endregion

		#region Consruction and disposal
		/// <summary>
		/// Initializes a new instance of the <see cref="XWorksViewBase"/> class.
		/// </summary>
		protected XWorksViewBase()
		{
			m_fullyInitialized = false;

			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();


			AccNameDefault = "XWorksViewBase";		// default accessibility name
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
				if(components != null)
					components.Dispose();
				if (ExistingClerk != null && !m_haveActiveClerk)
					ExistingClerk.BecomeInactive();
#if RANDYTODO
			// TODO: Block for now, to not have xWorks take a new dependency on LanguageExplorer
			// TODO: Expected disposition:
			//	TODO: 1. Move this file into LanguageExplorer, and/or
			//	TODO: 2. Use interface instead of actual MultiPane class.

				if (m_mpParent != null)
					m_mpParent.ShowFirstPaneChanged -= mp_ShowFirstPaneChanged;
#endif
			}
			m_informationBar = null; // Should be disposed automatically, since it is in the Controls collection.
#if RANDYTODO
			// TODO: Block for now, to not have xWorks take a new dependency on LanguageExplorer
			// TODO: Expected disposition:
			//	TODO: 1. Move this file into LanguageExplorer, and/or
			//	TODO: 2. Use interface instead of actual MultiPane class.

			m_mpParent = null;
#endif

			base.Dispose( disposing );
		}

		#endregion // Consruction and disposal

		#region Properties

		/// <summary>
		/// FDO cache.
		/// </summary>
		protected FdoCache Cache
		{
			get
			{
				return PropertyTable.GetValue<FdoCache>("cache");
			}
		}

		/// <summary>
		/// This is used in at least one place to determine if we have an existing
		/// clerk without creating one if there isn't. This is used in logic to prevent
		/// loading and sorting the record list twice.
		/// </summary>
		protected internal RecordClerk ExistingClerk
		{
			get
			{
				if (m_clerk != null)
					return m_clerk;
				if (PropertyTable == null)
					return null; // Avoids a null reference exception, if there is no property table at all.
				m_haveActiveClerk = false;
				m_clerk = RecordClerk.FindClerk(PropertyTable, m_vectorName);
				if (m_clerk != null && m_clerk.IsActiveInGui)
					m_haveActiveClerk = true;
				return m_clerk;
			}
		}

		internal RecordClerk CreateClerk(bool loadList)
		{
#if RANDYTODO
			var clerk = RecordClerkFactory.CreateClerk(PropertyTable, Publisher, Subscriber, loadList);
			clerk.Editable = XmlUtils.GetOptionalBooleanAttributeValue(m_configurationParameters, "allowInsertDeleteRecord", true);
			return clerk;
#else
			return null;
#endif
		}

		/// <summary>
		/// Get/Set the Clerk used by the view.
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public RecordClerk Clerk
		{
			get
			{
				return m_clerk = ExistingClerk ?? CreateClerk(true);
			}
			set
			{
				// allow parent controls to pass in the Clerk we want this control to use.
				m_vectorName = value != null ? value.Id : "";
				m_clerk = value;
			}
		}

		public IPaneBar MainPaneBar
		{
			get
			{
				CheckDisposed();

				if (m_informationBar != null)
					return m_informationBar as IPaneBar;
#if RANDYTODO
			// TODO: Block while PaneBarContainer is being moved to LanguageExplorer.
			// TODO: Re-enable when this code gets moved, so this project has no dependency on LanguageExplorer.
				if (Parent is PaneBarContainer)
					return (Parent as PaneBarContainer).PaneBar;
#endif

				return null;
			}
			set
			{
				CheckDisposed();

				// suppressInfoBar
				m_informationBar = value as UserControl;
			}
		}

		#endregion Properties

		#region IMainContentControl implementation

		/// <summary>
		/// From IMainContentControl
		/// </summary>
		/// <returns>true if ok to go away</returns>
		public virtual bool PrepareToGoAway()
		{
			CheckDisposed();

			return true;
		}

		public string AreaName
		{
			get
			{
				CheckDisposed();

#if RANDYTODO
				// TODO: a lot will need to be supplied by area/tool that was in the xml.
				return XmlUtils.GetOptionalAttributeValue( m_configurationParameters, "area", "unknown");
#else
				return PropertyTable.GetValue<string>("area");
#endif
			}
		}

		#endregion // IMainContentControl implementation

		#region ICtrlTabProvider implementation

		public virtual Control PopulateCtrlTabTargetCandidateList(List<Control> targetCandidates)
		{
			if (targetCandidates == null)
				throw new ArgumentNullException("targetCandidates");

			targetCandidates.Add(this);

			return ContainsFocus ? this : null;
		}

		#endregion  ICtrlTabProvider implementation

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

		#region Other methods

		protected virtual void AddPaneBar()
		{
		}

		private const string kEllipsis = "...";
		protected string TrimToMaxPixelWidth(int pixelWidthAllowed, string sToTrim)
		{
			int sPixelWidth;
			int charsAllowed;

			if(sToTrim.Length == 0)
				return sToTrim;

			sPixelWidth = GetWidthOfStringInPixels(sToTrim);
			var avgPxPerChar = sPixelWidth / Convert.ToSingle(sToTrim.Length);
			charsAllowed = Convert.ToInt32(pixelWidthAllowed / avgPxPerChar);
			if(charsAllowed < 5)
				return String.Empty;
			return sPixelWidth < pixelWidthAllowed ? sToTrim : sToTrim.Substring(0, charsAllowed-4) + kEllipsis;
		}

		private int GetWidthOfStringInPixels(string sInput)
		{
			using(var g = Graphics.FromHwnd(Handle))
			{
				return Convert.ToInt32(g.MeasureString(sInput, TitleBarFont).Width);
			}
		}

		protected Control TitleBar
		{
			get { return m_informationBar.Controls[0]; }
		}

		protected Font TitleBarFont
		{
			get { return TitleBar.Font; }
		}

		protected void ResetSpacer(int spacerWidth, string activeLayoutName)
		{
			var bar = TitleBar;
			if(bar is Panel && bar.Controls.Count > 1)
			{
				var cctrls = bar.Controls.Count;
				bar.Controls[cctrls - 1].Width = spacerWidth;
				bar.Controls[cctrls - 1].Text = activeLayoutName;
			}
		}

		protected string GetBaseTitleStringFromConfig()
		{
			string titleStr = "";
			// See if we have an AlternativeTitle string table id for an alternate title.
			string titleId = XmlUtils.GetAttributeValue(m_configurationParameters,
																	  "altTitleId");
			if(titleId != null)
			{
				titleStr = StringTable.Table.GetString(titleId, "AlternativeTitles");
				if(Clerk.OwningObject != null &&
					XmlUtils.GetBooleanAttributeValue(m_configurationParameters, "ShowOwnerShortname"))
				{
					// Originally this option was added to enable the Reversal Index title bar to show
					// which reversal index was being shown.
					titleStr = string.Format(xWorksStrings.ksXReversalIndex, Clerk.OwningObject.ShortName,
													 titleStr);
				}
			}
			else if(Clerk.OwningObject != null)
			{
				if(XmlUtils.GetBooleanAttributeValue(m_configurationParameters,
																 "ShowOwnerShortname"))
					titleStr = Clerk.OwningObject.ShortName;
			}
			return titleStr;
		}

		/// <summary>
		/// When our parent changes, we may need to re-evaluate whether to show our info bar.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnParentChanged(EventArgs e)
		{
			base.OnParentChanged (e);

			if (Parent == null)
				return;

#if RANDYTODO
			// TODO: Block for now, to not have xWorks take a new dependency on LanguageExplorer
			// TODO: Expected disposition:
			//	TODO: 1. Move this file into LanguageExplorer, and/or
			//	TODO: 2. Use interface instead of actual MultiPane class.

			var mp = Parent as MultiPane ?? Parent.Parent as MultiPane;

			if (mp == null)
				return;

			string suppress = XmlUtils.GetOptionalAttributeValue(m_configurationParameters, "suppressInfoBar", "false");
			if (suppress == "ifNotFirst")
			{
				mp.ShowFirstPaneChanged += mp_ShowFirstPaneChanged;
				m_mpParent = mp;
				mp_ShowFirstPaneChanged(mp, new EventArgs());
			}
#endif
		}

		/// <summary>
		/// Read in the parameters to determine which sequence/collection we are editing.
		/// </summary>
		protected virtual void ReadParameters()
		{
			XmlNode node = ToolConfiguration.GetClerkNodeFromToolParamsNode(m_configurationParameters);
			// Set the clerk id if the parent control hasn't already set it.
			if (String.IsNullOrEmpty(m_vectorName))
				m_vectorName = ToolConfiguration.GetIdOfTool(node);
		}

		protected virtual void ShowRecord()
		{
			SetInfoBarText();
		}

		protected abstract void SetupDataContext();

		protected virtual void SetupStylesheet()
		{
			// Do nothing here.
		}

		/// <summary>
		/// Sets the title string to an appropriate default when nothing is specified in the xml configuration for the view
		/// </summary>
		protected virtual void SetInfoBarText()
		{
			if (m_informationBar == null)
				return;
			string className = StringTable.Table.GetString("No Record", "Misc");
			if (Clerk.CurrentObject != null)
			{
				string typeName = Clerk.CurrentObject.GetType().Name;
				if (Clerk.CurrentObject is ICmPossibility)
				{
					var possibility = Clerk.CurrentObject as ICmPossibility;
					className = possibility.ItemTypeName();
				}
				else
				{
					className = StringTable.Table.GetString(typeName, "ClassNames");
				}
				if (className == "*" + typeName + "*")
				{
					className = typeName;
				}
			}
			else
			{
				string emptyTitleId = XmlUtils.GetAttributeValue(m_configurationParameters, "emptyTitleId");
				if (!String.IsNullOrEmpty(emptyTitleId))
				{
					string titleStr;
					XmlViewsUtils.TryFindString("EmptyTitles", emptyTitleId, out titleStr);
					if (titleStr != "*" + emptyTitleId + "*")
						className = titleStr;
					Clerk.UpdateStatusBarRecordNumber(titleStr);
				}
			}
			// This code:  ((IPaneBar)m_informationBar).Text = className;
			// causes about 47 of the following exceptions when executed in Flex.
			// First-chance exception at 0x4ed9b280 in Flex.exe: 0xC0000005: Access violation writing location 0x00f90004.
			// The following code doesn't cause the exception, but neither one actually sets the Text to className,
			// so something needs to be changed somewhere. It doesn't enter "override string Text" in PaneBar.cs
			(m_informationBar as IPaneBar).Text = className;
		}

		#endregion Other methods

		#region Event handlers

		private void mp_ShowFirstPaneChanged(object sender, EventArgs e)
		{
#if RANDYTODO
			// TODO: Reinstate MultiPane use once this code is relocated in LanguageExplorer.
			// TODO: Or perhaps better, use an interface.
			var mpSender = (MultiPane) sender;

			bool fWantInfoBar = (this == mpSender.FirstVisibleControl);
			if (fWantInfoBar && m_informationBar == null)
			{
				AddPaneBar();
				if (m_informationBar != null)
					SetInfoBarText();
			}
			else if (m_informationBar != null && !fWantInfoBar)
			{
				Controls.Remove((UserControl)m_informationBar);
				m_informationBar.Dispose();
				m_informationBar = null;
			}
#endif
		}

		private void ReloadListsArea()
		{
			Publisher.Publish("ReloadAreaTools", "lists");
		}

		private void DoDeleteCustomListCmd(ICmPossibilityList curList)
		{
			UndoableUnitOfWorkHelper.Do(xWorksStrings.ksUndoDeleteCustomList, xWorksStrings.ksRedoDeleteCustomList,
										Cache.ActionHandlerAccessor, () => new DeleteCustomList(Cache).Run(curList));
		}

		#endregion Event handlers

#if RANDYTODO
		public bool OnDisplayShowTreeBar(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			display.Enabled = (m_treebarAvailability == TreebarAvailability.Optional);
			return true;//we handled this, no need to ask anyone else.
		}

		public bool OnDisplayExport(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();
			// In order for this menu to be visible and enabled it has to be in the correct area (lexicon)
			// and the right tool(s).
			// Tools that allow this menu, as far as I (RickM) can tell, as of 10 Aug 2007:
			// (areaChoice == "lexicon" or "words" or "grammar" or "lists"

			RecordClerk clerk = Clerk;
			string areaChoice = m_propertyTable.GetValue<string>("areaChoice");
			//uncomment the following line if we need to turn on or off the Export menu item
			//for specific tools in the various areas of the application.
			//string toolChoice = m_mediator.PropertyTable.GetStringProperty("ToolForAreaNamed_lexicon", null);
			//string toolChoice = m_mediator.PropertyTable.GetStringProperty("grammarSketch_grammar", null);
			bool inFriendlyTerritory = (areaChoice == "lexicon"
#if RANDYTODO
			// TODO: The "notebook" area uses its own dlg. See: RecordClerk's method: OnExport
#endif
				|| areaChoice == "notebook"
#if RANDYTODO
			// TODO: These "textsWords" tools use the "concordanceWords" Clerk, so can handle the File_Export menu:
			// TODO: Analyses, bulkEditWordforms, and wordListConcordance.
			// TODO: These tools in the "textsWords" do not support the File_Export menu, so it is not visible for them:
			// TODO: complexConcordance, concordance, corpusStatistics, interlinearEdit
#endif
				|| (areaChoice == "textsWords" && clerk.Id == "concordanceWords")
#if RANDYTODO
			// TODO: The "grammarSketch" tool in the "grammar" area uses its own dlg. See: GrammarSketchHtmlViewer's method: OnExport
			// TODO: All other "grammar" area tools use the basic dlg and worry about some custom lexicon properties.
#endif
				|| areaChoice == "grammar"
				|| areaChoice == "lists");
			if (inFriendlyTerritory)
				display.Enabled = display.Visible = true;
			else
				display.Enabled = display.Visible = false;

			return true;
		}

		public bool OnDisplayAddCustomField(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			// No, as it can be using a config node that is correctly set to false, and we really wanted some other node,
			// in order to see the menu in the main Lexicon Edit tool.
			// bool fEditable = XmlUtils.GetOptionalBooleanAttributeValue(m_configurationParameters, "editable", true);

			// In order for this menu to be visible and enabled it has to be in the correct area (lexicon)
			// and the right tool(s).
			// Tools that allow this menu, as far as I (RandyR) can tell, as of 24 May 2007:
			// "lexiconEdit", "bulkEditEntries", "lexiconBrowse", and "bulkEditSenses"
			// I searched through JIRA to see if there was an offical list, and couldn't find such a list.
			// The old code tried to fish out some 'editable' attr from the xml file,
			// but in some contexts in switching tools in the Lexicon area, the config file was for the dictionary preview
			// control, which was set to 'false'. That makes sense, since the view itself isn't editable.
			// No: if (areaChoice == "lexicon" && fEditable && (m_vectorName == "entries" || m_vectorName == "AllSenses"))
			string toolChoice = m_propertyTable.GetValue("currentContentControl", string.Empty);
			string areaChoice = m_propertyTable.GetValue("areaChoice", string.Empty);
			bool inFriendlyTerritory = false;
			switch (areaChoice)
			{
				case "lexicon":
					inFriendlyTerritory = toolChoice == "lexiconEdit" || toolChoice == "bulkEditEntriesOrSenses" ||
										  toolChoice == "lexiconBrowse";
					break;
				case "notebook":
					inFriendlyTerritory = toolChoice == "notebookEdit" || toolChoice == "notebookBrowse";
					break;
			}

			display.Enabled = display.Visible = inFriendlyTerritory;
			return true;
		}
#endif

		public bool OnAddCustomField(object argument)
		{
			CheckDisposed();

			if (SharedBackendServices.AreMultipleApplicationsConnected(Cache))
			{
				MessageBoxUtils.Show(ParentForm, xWorksStrings.ksCustomFieldsCanNotBeAddedDueToOtherAppsText,
					xWorksStrings.ksCustomFieldsCanNotBeAddedDueToOtherAppsCaption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return true;
			}

			AddCustomFieldDlg.LocationType locationType = AddCustomFieldDlg.LocationType.Lexicon;
			string areaChoice = PropertyTable.GetValue("areaChoice", string.Empty);
			switch (areaChoice)
			{
				case "lexicon":
					locationType = AddCustomFieldDlg.LocationType.Lexicon;
					break;
				case "notebook":
					locationType = AddCustomFieldDlg.LocationType.Notebook;
					break;
			}
			using (var dlg = new AddCustomFieldDlg(PropertyTable, Publisher, locationType))
			{
				if (dlg.ShowCustomFieldWarning(this))
					dlg.ShowDialog(this);
			}

			return true;	// handled
		}

#if RANDYTODO
		public bool OnDisplayConfigureList(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			// In order for this menu to be visible and enabled it has to be in the correct area (lists)
			var areaChoice = m_propertyTable.GetValue("areaChoice", string.Empty);
			var inFriendlyTerritory = false;
			switch (areaChoice)
			{
				case "lists":
					inFriendlyTerritory = true;
					break;
			}

			display.Enabled = display.Visible = inFriendlyTerritory;
			return true;
		}
#endif

		public bool OnConfigureList(object argument)
		{
			CheckDisposed();

			if (Clerk != null && Clerk.OwningObject != null && (Clerk.OwningObject is ICmPossibilityList))
				using (var dlg = new ConfigureListDlg(PropertyTable, Publisher, (ICmPossibilityList) Clerk.OwningObject))
					dlg.ShowDialog(this);

			return true;	// handled
		}

#if RANDYTODO
		public bool OnDisplayAddCustomList(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			// In order for this menu to be visible and enabled it has to be in the correct area (lists)
			var areaChoice = m_propertyTable.GetValue("areaChoice", string.Empty);
			var inFriendlyTerritory = false;
			switch (areaChoice)
			{
				case "lists":
					inFriendlyTerritory = true;
					break;
			}

			display.Enabled = display.Visible = inFriendlyTerritory;
			return true;
		}
#endif

		public bool OnAddCustomList(object argument)
		{
			CheckDisposed();

			using (var dlg = new AddListDlg(PropertyTable, Publisher))
				dlg.ShowDialog(this);

			return true;	// handled
		}

#if RANDYTODO
		public bool OnDisplayDeleteCustomList(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			// In order for this menu to be visible and enabled it has to be in the correct area (lists)
			var areaChoice = m_propertyTable.GetValue("areaChoice", string.Empty);
			var inFriendlyTerritory = false;
			switch (areaChoice)
			{
				case "lists":
					// Is currently selected list a Custom list?
					if (Clerk == null || Clerk.OwningObject == null || !(Clerk.OwningObject is ICmPossibilityList))
						break; // handled, but not a valid selection
					var possList = Clerk.OwningObject as ICmPossibilityList;
					if (possList.Owner == null)
						inFriendlyTerritory = true; // a Custom list
					break;
			}

			display.Enabled = display.Visible = inFriendlyTerritory;
			return true;
		}
#endif

		public bool OnDeleteCustomList(object argument)
		{
			CheckDisposed();

			// Get currently selected list
			if (Clerk == null || Clerk.OwningObject == null || !(Clerk.OwningObject is ICmPossibilityList))
				return true; // handled, but not a valid selection
			var listToDelete = Clerk.OwningObject as ICmPossibilityList;
			DoDeleteCustomListCmd(listToDelete);
			ReloadListsArea(); // Redisplay lists without this one

			return true;	// handled
		}
	}
}

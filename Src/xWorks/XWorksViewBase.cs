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
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using XCore;
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
	/// <remarks>
	/// IxCoreContentControl includes IxCoreColleague now,
	/// so only IxCoreContentControl needs to be declared here.
	/// </remarks>
	public abstract class XWorksViewBase : XCoreUserControl, IxCoreContentControl, IPaneBarUser
	{
		#region Enumerations

		public enum TreebarAvailability {Required, Optional, NotAllowed, NotMyBusiness};

		#endregion Enumerations

		#region Event declaration

		#endregion Event declaration

		#region Data members
		/// <summary>
		/// Optional information bar above the main control.
		/// </summary>
		protected UserControl m_informationBar;
		/// <summary>
		/// Name of the vector we are editing.
		/// </summary>
		protected string m_vectorName;
		/// <summary>
		///
		/// </summary>
		protected int m_fakeFlid; // the list
		/// <summary>
		/// Mediator that passes off messages.
		/// </summary>
		protected Mediator m_mediator;
		/// <summary>
		/// This is used to keep us from responding to messages that we get while
		/// we are still trying to get initialized.
		/// </summary>
		protected bool m_fullyInitialized;
		/// <summary>
		/// tell whether the tree bar is required, optional, or not allowed for this view
		/// </summary>
		protected TreebarAvailability m_treebarAvailability;
		/// <summary>
		/// Last known parent that is a MultiPane.
		/// </summary>
		private MultiPane m_mpParent;
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
				if (m_mediator != null && !m_mediator.IsDisposed)
					m_mediator.RemoveColleague(this);
				if (m_mpParent != null)
					m_mpParent.ShowFirstPaneChanged -= mp_ShowFirstPaneChanged;
			}
			m_mediator = null;
			m_informationBar = null; // Should be disposed automatically, since it is in the Controls collection.
			m_mpParent = null;

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
				return (FdoCache)m_mediator.PropertyTable.GetValue("cache");
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
				if (m_mediator == null)
					return null; // Avoids a null reference exception, if there is no mediator at all.
				m_haveActiveClerk = false;
				m_clerk = RecordClerk.FindClerk(m_mediator, m_vectorName);
				if (m_clerk != null && m_clerk.IsActiveInGui)
					m_haveActiveClerk = true;
				return m_clerk;
			}
		}

		internal RecordClerk CreateClerk(bool loadList)
		{
			var clerk = RecordClerkFactory.CreateClerk(m_mediator, m_configurationParameters, loadList);
			clerk.Editable = XmlUtils.GetOptionalBooleanAttributeValue(m_configurationParameters, "allowInsertDeleteRecord", true);
			return clerk;
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

		/// <summary>
		/// a look up table for getting the correct version of strings that the user will see.
		/// </summary>
		public StringTable StringTbl
		{
			get
			{
				CheckDisposed();

				return m_mediator.StringTbl;
			}
		}

		public IPaneBar MainPaneBar
		{
			get
			{
				CheckDisposed();

				if (m_informationBar != null)
					return m_informationBar as IPaneBar;
				if (Parent is PaneBarContainer)
					return (Parent as PaneBarContainer).PaneBar;

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

		#region IxCoreColleague implementation

		public abstract void Init(Mediator mediator, XmlNode configurationParameters);

		/// <summary>
		/// return an array of all of the objects which should
		/// 1) be queried when looking for someone to deliver a message to
		/// 2) be potential recipients of a broadcast
		/// </summary>
		/// <returns></returns>
		public IxCoreColleague[] GetMessageTargets()
		{
			CheckDisposed();

			//note: we just let navigation commands go straight to the clerk, which will then send us a message.

			/*
			 * important note about messages and record clerk: see the top of the recordClerk.cs file.
			*/
			var targets = new List<IxCoreColleague>();
			// HACK: This needs to be controlled better, since this method is being called,
			// while the object is being disposed, which causes the call to the Clerk property to crash
			// with a null reference exception on the mediator.
			if (m_mediator != null)
			{
				// Additional targets are typically child windows that should have a chance to intercept
				// messages before the Clerk sees them.
				GetMessageAdditionalTargets(targets);
				targets.Add(Clerk);
				targets.Add(this);
			}
			return targets.ToArray();
		}

		/// <summary>
		/// subclasses should override if they have more targets, and add to the list.
		/// </summary>
		/// <returns></returns>
		protected virtual void GetMessageAdditionalTargets(List<IxCoreColleague> collector)
		{
		}

		/// <summary>
		/// Should not be called if disposed.
		/// </summary>
		public bool ShouldNotCall
		{
			get { return IsDisposed; }
		}

		public abstract int Priority { get; }

		#endregion // IxCoreColleague implementation

		#region IxCoreContentControl implementation

		/// <summary>
		/// From IxCoreContentControl
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

				return XmlUtils.GetOptionalAttributeValue( m_configurationParameters, "area", "unknown");
			}
		}

		#endregion // IxCoreContentControl implementation

		#region IxCoreCtrlTabProvider implementation

		public virtual Control PopulateCtrlTabTargetCandidateList(List<Control> targetCandidates)
		{
			if (targetCandidates == null)
				throw new ArgumentNullException("targetCandidates");

			targetCandidates.Add(this);

			return ContainsFocus ? this : null;
		}

		#endregion  IxCoreCtrlTabProvider implementation

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
				titleStr = StringTbl.GetString(titleId, "AlternativeTitles");
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
			string className = StringTbl.GetString("No Record", "Misc");
			if (Clerk.CurrentObject != null)
			{
				string typeName = Clerk.CurrentObject.GetType().Name;
				if (Clerk.CurrentObject is ICmPossibility)
				{
					var possibility = Clerk.CurrentObject as ICmPossibility;
					className = possibility.ItemTypeName(StringTbl);
				}
				else
				{
					className = StringTbl.GetString(typeName, "ClassNames");
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
					XmlViewsUtils.TryFindString(StringTbl, "EmptyTitles", emptyTitleId, out titleStr);
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
		}

		private void ReloadListsArea()
		{
			m_mediator.SendMessage("ReloadAreaTools", "lists");
		}

		private void DoDeleteCustomListCmd(ICmPossibilityList curList)
		{
			UndoableUnitOfWorkHelper.Do(xWorksStrings.ksUndoDeleteCustomList, xWorksStrings.ksRedoDeleteCustomList,
										Cache.ActionHandlerAccessor, () => new DeleteCustomList(Cache).Run(curList));
		}

		#endregion Event handlers

		#region IxCoreColleague Event handlers

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
			string areaChoice = m_mediator.PropertyTable.GetStringProperty("areaChoice", null);
			//uncomment the following line if we need to turn on or off the Export menu item
			//for specific tools in the various areas of the application.
			//string toolChoice = m_mediator.PropertyTable.GetStringProperty("ToolForAreaNamed_lexicon", null);
			string toolChoice = m_mediator.PropertyTable.GetStringProperty("grammarSketch_grammar", null);
			bool inFriendlyTerritory = (areaChoice == "lexicon"
				|| areaChoice == "notebook"
				|| clerk.Id == "concordanceWords"
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
			string toolChoice = m_mediator.PropertyTable.GetStringProperty("currentContentControl", string.Empty);
			string areaChoice = m_mediator.PropertyTable.GetStringProperty("areaChoice", string.Empty);
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

		public bool OnAddCustomField(object argument)
		{
			CheckDisposed();

			// Only allow adding custom fields when a single client is connected.
			if (Cache.NumberOfRemoteClients > 1 || (Cache.ProjectId.IsLocal && Cache.NumberOfRemoteClients > 0))
			{
				MessageBoxUtils.Show(ParentForm, xWorksStrings.ksCustomFieldsCanNotBeAddedDueToRemoteClientsText,
					xWorksStrings.ksCustomFieldsCanNotBeAddedDueToRemoteClientsCaption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return true;
			}
			if (SharedBackendServices.AreMultipleApplicationsConnected(Cache))
			{
				MessageBoxUtils.Show(ParentForm, xWorksStrings.ksCustomFieldsCanNotBeAddedDueToOtherAppsText,
					xWorksStrings.ksCustomFieldsCanNotBeAddedDueToOtherAppsCaption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return true;
			}

			AddCustomFieldDlg.LocationType locationType = AddCustomFieldDlg.LocationType.Lexicon;
			string areaChoice = m_mediator.PropertyTable.GetStringProperty("areaChoice", string.Empty);
			switch (areaChoice)
			{
				case "lexicon":
					locationType = AddCustomFieldDlg.LocationType.Lexicon;
					break;
				case "notebook":
					locationType = AddCustomFieldDlg.LocationType.Notebook;
					break;
			}
			using (var dlg = new AddCustomFieldDlg(m_mediator, locationType))
			{
				if (dlg.ShowCustomFieldWarning(this))
					dlg.ShowDialog(this);
			}

			return true;	// handled
		}

		public bool OnDisplayConfigureList(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			// In order for this menu to be visible and enabled it has to be in the correct area (lists)
			var areaChoice = m_mediator.PropertyTable.GetStringProperty("areaChoice", string.Empty);
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

		public bool OnConfigureList(object argument)
		{
			CheckDisposed();

			if (Clerk != null && Clerk.OwningObject != null && (Clerk.OwningObject is ICmPossibilityList))
				using (var dlg = new ConfigureListDlg(m_mediator, (ICmPossibilityList) Clerk.OwningObject))
					dlg.ShowDialog(this);

			return true;	// handled
		}

		public bool OnDisplayAddCustomList(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			// In order for this menu to be visible and enabled it has to be in the correct area (lists)
			var areaChoice = m_mediator.PropertyTable.GetStringProperty("areaChoice", string.Empty);
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

		public bool OnAddCustomList(object argument)
		{
			CheckDisposed();

			using (var dlg = new AddListDlg(m_mediator))
				dlg.ShowDialog(this);

			return true;	// handled
		}

		public bool OnDisplayDeleteCustomList(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			// In order for this menu to be visible and enabled it has to be in the correct area (lists)
			var areaChoice = m_mediator.PropertyTable.GetStringProperty("areaChoice", string.Empty);
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

		#endregion IxCoreColleague Event handlers
	}
}

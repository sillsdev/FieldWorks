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
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.Xml;
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
				if (ExistingClerk != null)
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
				m_clerk = RecordClerk.FindClerk(m_mediator, m_vectorName);
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
				if (m_clerk == null)
					m_clerk = RecordClerk.FindClerk(m_mediator, m_vectorName) ?? CreateClerk(true);
				return m_clerk;
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

		//protected override void OnMouseDown(MouseEventArgs e)
		//{
		//    if (e.Button == MouseButtons.Right)
		//    {
		//        var areaChoice = m_mediator.PropertyTable.GetStringProperty("areaChoice", string.Empty);
		//        if (areaChoice != "lists")
		//            return;
		//        // Get currently selected list
		//        if (Clerk == null || Clerk.OwningObject == null || !(Clerk.OwningObject is ICmPossibilityList))
		//            return;
		//        MakeContextMenuIfAppropriate(e, Clerk.OwningObject as ICmPossibilityList);
		//    }
		//    base.OnMouseDown(e);
		//}

		//private void MakeContextMenuIfAppropriate(MouseEventArgs e, ICmPossibilityList curList)
		//{
		//    if (DateTime.Now.Ticks - m_ticksWhenContextMenuClosed > 50000) // 5ms!
		//    {
		//        // Consider bringing up another menu only if we weren't already showing one.
		//        // The above time test seems to be the only way to find out whether this click closed the last one.
		//        if (curList != null)
		//        {
		//            m_contextMenu = MakeListContextMenu(curList);
		//            m_contextMenu.Closed += new ToolStripDropDownClosedEventHandler(m_contextMenu_Closed);
		//            m_contextMenu.Show(this, e.X, e.Y);
		//        }
		//    }
		//}

		//private ContextMenuStrip MakeListContextMenu(ICmPossibilityList curList)
		//{
		//    var menu = new ContextMenuStrip();

		//    // Menu item allowing the user to delete a Custom list.
		//    var delCustomListCmd = new ListMenuItem(xWorksStrings.ksDeleteListMenuItem, curList);
		//    delCustomListCmd.Click += new EventHandler(delList_Click);
		//    menu.Items.Add(delCustomListCmd);

		//    return menu;
		//}

		///// <summary>
		///// Invoked when the user chooses the "Delete Custom List" menu item.
		///// Sender is a ListMenuItem indicating the selected List area list.
		///// </summary>
		///// <param name="sender"></param>
		///// <param name="e"></param>
		//void delList_Click(object sender, EventArgs e)
		//{
		//    var curList = (sender as ListMenuItem).List;
		//    DoDeleteCustomListCmd(curList);
		//    ReloadListsArea(); // Redisplay lists without this one
		//}

		private void ReloadListsArea()
		{
			m_mediator.SendMessage("ReloadAreaTools", "lists");
		}

		private void DoDeleteCustomListCmd(ICmPossibilityList curList)
		{
			UndoableUnitOfWorkHelper.Do(xWorksStrings.ksUndoDeleteCustomList, xWorksStrings.ksRedoDeleteCustomList,
										Cache.ActionHandlerAccessor, () => new DeleteCustomList(Cache).Run(curList));
		}

		//private void m_contextMenu_Closed(object sender, ToolStripDropDownClosedEventArgs e)
		//{
		//    m_ticksWhenContextMenuClosed = DateTime.Now.Ticks;
		//}

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
			// RecordClerk clerk = Clerk; // CS0219
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
				dlg.ShowDialog();

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
					dlg.ShowDialog();

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
				dlg.ShowDialog();

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

	//class ListMenuItem : ToolStripMenuItem
	//{
	//    readonly ICmPossibilityList m_list;
	//    public ListMenuItem(string label, ICmPossibilityList clickedList)
	//        : base(label)
	//    {
	//        m_list = clickedList;
	//    }

	//    /// <summary>
	//    /// The selected list that was clicked on.
	//    /// </summary>
	//    public ICmPossibilityList List
	//    {
	//        get { return m_list; }
	//    }
	//}
}

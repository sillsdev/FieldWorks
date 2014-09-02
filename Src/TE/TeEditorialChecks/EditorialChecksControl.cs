// Copyright (c) 2008-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: EditorialChecksControl.cs
// Responsibility: TE Team


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.UIAdapters;
using SIL.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using ControlExtenders;
using Microsoft.Win32;
using XCore;
using SIL.FieldWorks.FwCoreDlgs;
using SILUBS.SharedScrUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.CoreImpl;

namespace SIL.FieldWorks.TE.TeEditorialChecks
{
	#region EditorialChecksControl class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// The dockable control for the Editorial Checks
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class EditorialChecksControl : CheckControl, IxCoreColleague
	{
		/// <summary>Event raised when the user clicks on the "Run Checks" button</summary>
		public event EventHandler RunChecksClick;
		/// <summary>Event raised when the user clicks on the "Show Checks" button</summary>
		public event EventHandler ShowChecksClick;

		#region Member variables
		private ScrChecksDataSource m_chkDataSource;
		private FdoCache m_cache;
		private IHelpTopicProvider m_helpTopicProvider;
		private IApp m_app;
		private FilteredScrBooks m_bookFilter;
		private SortedList<ScrCheckKey, IScriptureCheck> m_scrCheckList;
		private EditorialChecksRenderingsControl m_checkErrorsList;
		private ToolTip m_nodeTip;
		private TreeNode m_nodeMouseOver;
		private static Image s_ignoreImage;
		private static Image s_dontIgnoreImage;
		private int m_dxButtonGap;
		private int m_dyButtonGap;
		private int m_buttonPanelHeight1;
		private int m_buttonPanelHeight2;
		private int m_buttonPanelHeight3;
		private bool m_adjustingButtonPanel = false;
		private ITMAdapter m_tmAdapter;
		private Rectangle m_chkNameTextRect;
		private ToolStripButton m_tbbIgnore;
		private ToolStripButton m_tbbIgnoreWAnnotation;
		private ToolStripButton m_tbbInconsistencies;
		private ToolStripButton m_tbbEditAnnotations;
		private ScrChkTreeNode m_nodeLastRightClickedOn = null;
		#endregion

		#region Construction
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:EditorialChecksControl"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public EditorialChecksControl()
		{
			InitializeComponent();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:EditorialChecksControl"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="app">The app.</param>
		/// <param name="bookFilter">The book filter.</param>
		/// <param name="sCaption">The caption to use when this control is displayed as a
		/// floating window</param>
		/// <param name="sProject">The name of the current project</param>
		/// <param name="tmAdapter">TMAdapter for the main window</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// ------------------------------------------------------------------------------------
		public EditorialChecksControl(FdoCache cache, IApp app, FilteredScrBooks bookFilter,
			string sCaption, string sProject, ITMAdapter tmAdapter,
			IHelpTopicProvider helpTopicProvider)
			: base(sCaption, sProject)
		{
			InitializeComponent();

			lblCheckName.Text = string.Empty;

			m_dxButtonGap = m_btnApplyFilter.Left - m_btnRunChecks.Right;
			m_dyButtonGap = pnlButtons.Padding.Top;
			m_buttonPanelHeight1 = pnlButtons.Height;
			m_buttonPanelHeight2 = m_buttonPanelHeight1 + m_btnRunChecks.Height + m_dyButtonGap;
			m_buttonPanelHeight3 = m_buttonPanelHeight2 + m_btnRunChecks.Height + m_dyButtonGap;

			m_helpTopicProvider = helpTopicProvider;
			m_app = app;
			// It's important to subscribe to this event after the previous three
			// heights are saved. Therefore, this event should not be subscribed
			// to in designer (i.e. InitializeComponent()). This fixes TE-6653.
			pnlButtons.ClientSizeChanged += pnlButtons_ClientSizeChanged;

			m_cache = cache;
			m_chkDataSource = new ScrChecksDataSource(cache, FwDirectoryFinder.TeStylesPath, FwDirectoryFinder.LegacyWordformingCharOverridesFile);

			m_bookFilter = bookFilter;
			m_bookFilter.FilterChanged += OnBookFilterChanged;
			CreateCheckingToolbar(tmAdapter);
			m_nodeTip = new ToolTip();

			m_ComboBox.HideDropDownWhenComboTextChanges = false;

			// Set the minimum allowable with for the
			// control will be based on the widest button.
			int minWidth = m_btnRunChecks.Width;
			minWidth = Math.Max(minWidth, m_btnApplyFilter.Width);
			MinimumSize = new Size(Math.Max(minWidth, m_btnHelp.Width), MinimumSize.Height);

			if (tmAdapter != null)
			{
				if (tmAdapter.MessageMediator != null)
					tmAdapter.MessageMediator.AddColleague(this);

				tmAdapter.SetContextMenuForControl(this, "cmnuEditorialChecksTree");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="ToolStripSeparator gets added to m_ToolStrip.Items and disposed there")]
		private void CreateCheckingToolbar(ITMAdapter tmAdapter)
		{
			if (tmAdapter == null)
				return;

			m_tmAdapter = tmAdapter;

			m_sepShowOnlyAtTop = new ToolStripSeparator();
			m_ToolStrip.Items.Insert(0, m_sepShowOnlyAtTop);

			AddToolStripButton(tmAdapter.GetItemProperties("mnuStatusEditAnnotation"),
				"ScrChecksEditAnnotation");

			m_tbbEditAnnotations = m_ToolStrip.Items[0] as ToolStripButton;
			m_ToolStrip.Items.Insert(0, new ToolStripSeparator());

			AddToolStripButton(tmAdapter.GetItemProperties("mnuStatusInconsistency"),
				"ScrChecksInconsistency");

			m_tbbInconsistencies = m_ToolStrip.Items[0] as ToolStripButton;
			s_dontIgnoreImage = TeResourceHelper.UnignoredInconsistency;

			AddToolStripButton(tmAdapter.GetItemProperties("mnuStatusIgnoredWAnnotation"),
				"ScrChecksIgnoredWAnnotation");

			m_tbbIgnoreWAnnotation = m_ToolStrip.Items[0] as ToolStripButton;

			AddToolStripButton(tmAdapter.GetItemProperties("mnuStatusIgnored"),
				"ScrChecksIgnored");

			m_tbbIgnore = m_ToolStrip.Items[0] as ToolStripButton;
			s_ignoreImage = TeResourceHelper.IgnoredInconsistency;

			OnCheckErrorsListReferenceChanged(null, CheckingError.Empty);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the tool strip button with the specified item properties. The msgId is used
		/// for the name and the button's click event uses that name as the message to send.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void AddToolStripButton(TMItemProperties itemProps, string msgId)
		{
			if (itemProps == null)
				return;

			AddToolStripButton(0, itemProps.Image, itemProps.Tooltip);
			m_ToolStrip.Items[0].Name = msgId;
			m_ToolStrip.Items[0].Tag = itemProps;

			if (m_tmAdapter.MessageMediator != null)
			{
				m_ToolStrip.Items[0].Click += delegate(object sender, EventArgs e)
				{
					ToolStripButton button = sender as ToolStripButton;
					if (button != null && button.Tag is TMItemProperties)
						m_tmAdapter.MessageMediator.SendMessage(button.Name, button.Tag);
				};
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the control's text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string Text
		{
			get { return base.Text; }
			set
			{
				base.Text = value;

				// Since it's the text property of the panel containing the tree control
				// is the one being referenced in the ToolStripControlComboBox control
				// when the ChecksControl is docked to the top, then make sure its
				// text property is set as well.
				if (pnlOuter != null)
					pnlOuter.Text = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the image used for ignored inconsistencies.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Image IgnoredInconsistenciesImage
		{
			get { return s_ignoreImage; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the image used for inconsistencies.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Image InconsistenciesImage
		{
			get { return s_dontIgnoreImage; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the EditorialChecksRenderingsControl associated with this
		/// EditorialChecksControl.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public EditorialChecksRenderingsControl CheckErrorsList
		{
			get { return m_checkErrorsList; }
			set
			{
				if (m_checkErrorsList == value)
					return;

				if (m_checkErrorsList != null)
				{
					m_checkErrorsList.ReferenceChanged -= OnCheckErrorsListReferenceChanged;
					m_checkErrorsList.ErrorListContentChanged -= OnCheckErrorsListErrorListContentChanged;
				}

				m_checkErrorsList = value;

				if (m_checkErrorsList != null)
				{
					m_checkErrorsList.ReferenceChanged += OnCheckErrorsListReferenceChanged;
					m_checkErrorsList.ErrorListContentChanged += OnCheckErrorsListErrorListContentChanged;
					m_checkErrorsList.ValidCharsLoadException += ReportLoadException;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void OnCheckErrorsListErrorListContentChanged(object sender, CheckingError error)
		{
			OnCheckErrorsListReferenceChanged(sender, error);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void OnCheckErrorsListReferenceChanged(object sender, CheckingError error)
		{
			if (error == null)
				return;

			m_tbbIgnore.Enabled = m_tbbIgnoreWAnnotation.Enabled =
				(error != CheckingError.Empty && error.MyNote.ResolutionStatus == NoteStatus.Open);

			m_tbbInconsistencies.Enabled =
				(error != CheckingError.Empty && error.MyNote.ResolutionStatus == NoteStatus.Closed);

			m_tbbEditAnnotations.Enabled =
				(error != CheckingError.Empty && error.MyNote.ResolutionStatus == NoteStatus.Closed);
		}

		#endregion

		#region Overriden methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Control.HandleCreated"/> event.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);

			try
			{
				using (NonUndoableUnitOfWorkHelper unitOfWork = new NonUndoableUnitOfWorkHelper(
					m_cache.ServiceLocator.GetInstance<IActionHandler>()))
				{
					m_scrCheckList = InstalledScriptureChecks.GetChecks(m_chkDataSource);
					unitOfWork.RollBack = false;
				}
			}
			catch (ApplicationException exception)
			{
				// REVIEW: what do we do in this case?
				Logger.WriteEvent("Got exception loading Scripture checks: ");
				Logger.WriteError(exception);
				MessageBox.Show(exception.Message, m_app.ApplicationName,
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			FillAvailableChecksTree(m_scrCheckList);

			if (m_checkErrorsList != null)
			{
				OnErrorsUpdated();
				m_btnApplyFilter.Enabled = false;
			}

			if (m_persistence != null)
				OnLoadSettings(m_persistence.SettingsKey);
		}

		#endregion

		#region Checks Tree methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the FilterChanged event of the m_bookFilter control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		void OnBookFilterChanged(object sender, EventArgs e)
		{
			// Update the list of checks when the book filter changes.
			RefreshCheckTree();

			if (m_checkErrorsList != null)
				m_checkErrorsList.LoadCheckingErrors(IdsOfSelectedChecks);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ReadSelectedChecksForNode(TreeNode parentNode)
		{
			if (parentNode is ScrChkTreeNode && m_persistence != null)
				((ScrChkTreeNode)parentNode).ReadCheckedStateFromReg(m_persistence.SettingsKey);

			foreach (TreeNode node in parentNode.Nodes)
				ReadSelectedChecksForNode(node);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnHandleDestroyed(EventArgs e)
		{
			if (m_availableChecksTree.Nodes.Count > 0)
				WriteSelectedChecksForNode(m_availableChecksTree.Nodes[0]);

			if (m_tmAdapter.MessageMediator != null)
				m_tmAdapter.MessageMediator.RemoveColleague(this);

			base.OnHandleDestroyed(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void WriteSelectedChecksForNode(TreeNode parentNode)
		{
			if (parentNode is ScrChkTreeNode && m_persistence != null)
				((ScrChkTreeNode)parentNode).WriteCheckedStateToReg(m_persistence.SettingsKey);

			foreach (TreeNode node in parentNode.Nodes)
				WriteSelectedChecksForNode(node);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fills the Scripture checks tree control under groups or categories for the
		/// Scripture checks. Our initial implementation will put everything in one group at
		/// the top level of the tree.
		/// </summary>
		/// <param name="scrCheckList">list of available Scripture checks</param>
		/// ------------------------------------------------------------------------------------
		protected void FillAvailableChecksTree(SortedList<ScrCheckKey, IScriptureCheck> scrCheckList)
		{
			m_availableChecksTree.BeginUpdate();
			m_availableChecksTree.Nodes.Clear();
			foreach (KeyValuePair<ScrCheckKey, IScriptureCheck> scrCheckEntry in scrCheckList)
			{
				string group = scrCheckEntry.Value.CheckGroup;
				if (string.IsNullOrEmpty(group))
					group = "Other";

				TreeNode groupNode = GetNamedGroup(group, true);

				// Get the list of books and the last time the test was run for each.
				// This will also get the earliest of those times to use as the node's text.
				string[] bookChkInfo;
				DateTime lastRun =
					GetLastRunInfoForCheck(scrCheckEntry.Value.CheckId, out bookChkInfo);

				// Add the Scripture check name to the tree view.
				groupNode.Nodes.Add(new ScrChkTreeNode(scrCheckEntry.Key.Name,
					lastRun, bookChkInfo, scrCheckEntry.Value));
			}

			if (m_availableChecksTree.Nodes.Count > 0)
				ReadSelectedChecksForNode(m_availableChecksTree.Nodes[0]);

			m_availableChecksTree.EndUpdate();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the earliest date and time the specified check was run.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private DateTime GetLastRunInfoForCheck(Guid checkId, out string[] bookChkInfo)
		{
			List<string> bookChkInfoList = new List<string>();
			bookChkInfo = new string[] { };

			if (m_bookFilter.BookIds == null)
				return DateTime.MinValue;

			IFdoOwningSequence<IScrBookAnnotations> booksAnnotations =
				m_cache.LangProject.TranslatedScriptureOA.BookAnnotationsOS;

			string fmtTip =
				TeResourceHelper.GetResourceString("kstidScrChecksTreeNodeTipFormat");
			string fmtLastRunDate =
				TeResourceHelper.GetResourceString("kstidScrCheckRunDateTimeFormat");
			string fmtLastRunNever =
				TeResourceHelper.GetResourceString("kstidScrCheckNeverRunMsg");

			DateTime overallLastRun = DateTime.MaxValue;

			// Go through the books in the filter.
			foreach (int bookId in m_bookFilter.BookIds)
			{
				DateTime lastRun = DateTime.MinValue;
				string lastRunText = fmtLastRunNever;
				IScrBookAnnotations annotations = booksAnnotations[bookId - 1];

				// Is there any history of this check having been run on this book?
				if (annotations.ChkHistRecsOC.Count == 0)
					overallLastRun = DateTime.MinValue;
				else
				{
					// Go through the records of each time this test was run for
					// this book and get the date and time of the last run. While
					// finding that time, also keep track of the earliest time the
					// check was run for all books.
					foreach (IScrCheckRun scr in annotations.ChkHistRecsOC)
					{
						if (scr.CheckId == checkId)
						{
							if (overallLastRun > scr.RunDate && overallLastRun != DateTime.MinValue)
								overallLastRun = scr.RunDate;

							if (lastRun < scr.RunDate)
								lastRun = scr.RunDate;
						}
					}

					if (lastRun > DateTime.MinValue)
						lastRunText = string.Format(fmtLastRunDate, lastRun);
				}

				bookChkInfoList.Add(string.Format(fmtTip,
					m_bookFilter.GetBookByOrd(bookId).BestUIName, lastRunText));
			}

			bookChkInfo = bookChkInfoList.ToArray();
			return (overallLastRun == DateTime.MaxValue ? DateTime.MinValue : overallLastRun);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the tree node corresponding to the named group.
		/// </summary>
		/// <param name="groupName">Name of the group to find.</param>
		/// <param name="addIfNotFound">if set to <c>true</c> adds the requested group if not
		/// found.</param>
		/// <returns>The group tree node or null if not found</returns>
		/// ------------------------------------------------------------------------------------
		protected TreeNode GetNamedGroup(string groupName, bool addIfNotFound)
		{
			groupName = StringUtils.GetUiString(ScrFdoResources.ResourceManager, groupName);
			TreeNode[] matchingGroups = m_availableChecksTree.Nodes.Find(groupName, false);
			if (matchingGroups.Length > 0)
			{
				Debug.Assert(matchingGroups.Length == 1);
				return matchingGroups[0];
			}
			if (addIfNotFound)
			{
				TreeNode group = m_availableChecksTree.Nodes.Insert(NodeToInsert(groupName),
					groupName, groupName);
				group.Expand();
				return group;
			}
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines the index to insert the node in the tree view.
		/// </summary>
		/// <param name="groupName">Name of the group.</param>
		/// <returns>0-based index to insert the group name in the tree view</returns>
		/// ------------------------------------------------------------------------------------
		private int NodeToInsert(string groupName)
		{
			if (groupName.Equals("Basic"))
			{
				// The "Basic" groups of checks must always go at the beginning.
				return 0;
			}
			else if (groupName.Equals("Other"))
			{
				// Other should always be put at the end of the list
				return m_availableChecksTree.Nodes.Count;
			}
			else
			{
				int insertNode = 0;
				// Scan through until we find the place where the node should be inserted in the tree.
				for (int iNode = 0; iNode < m_availableChecksTree.Nodes.Count; iNode++)
				{
					// If we find the "Other" node...
					if (m_availableChecksTree.Nodes[iNode].Name == "Other")
					{
						// we want to insert before it.
						insertNode = iNode;
						break;
					}
					// Continue searching tree nodes if the contents of the scanned node is
					// "Basic" or is alphabetically after the current.
					else if (m_availableChecksTree.Nodes[iNode].Name == "Basic" ||
						string.Compare(groupName, m_availableChecksTree.Nodes[iNode].Name) > 0)
					{
						insertNode = iNode + 1;
						continue;
					}
					else
					{
						// We need to insert before this node
						insertNode = iNode;
						break;
					}
				}

				return insertNode;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the list of checks (of type IScriptureCheck) the user selected.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private IScriptureCheck[] SelectedChecks
		{
			get
			{
				TreeNode[] checkedNodes =
					m_availableChecksTree.GetNodesWithState(TriStateTreeView.CheckState.Checked);

				List<IScriptureCheck> checkList = new List<IScriptureCheck>();
				foreach (TreeNode node in checkedNodes)
				{
					ScrChkTreeNode sctn = node as ScrChkTreeNode;
					if (sctn != null && sctn.ScrCheck != null)
						checkList.Add(sctn.ScrCheck);
				}

				return checkList.ToArray();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the list of checkIds of the checks selected by the user.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<Guid> IdsOfSelectedChecks
		{
			get
			{
				IScriptureCheck[] selectedChecks = SelectedChecks;
				List<Guid> ids = new List<Guid>();
				foreach (IScriptureCheck check in selectedChecks)
					ids.Add(check.CheckId);

				return ids;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Refreshes the check nodes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void RefreshCheckNodes(TreeNode node)
		{
			foreach (TreeNode childNode in node.Nodes)
			{
				ScrChkTreeNode sctn = childNode as ScrChkTreeNode;
				if (sctn == null)
					RefreshCheckNodes(childNode);
				else
				{
					string[] bookChkInfo;
					sctn.LastRun = GetLastRunInfoForCheck(sctn.ScrCheck.CheckId, out bookChkInfo);
					sctn.TipBookList = bookChkInfo;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Forces the checks tree to update it's last run date/times.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void RefreshCheckTree()
		{
			if (m_availableChecksTree.Nodes.Count > 0)
			{
				m_availableChecksTree.BeginUpdate();
				RefreshCheckNodes(m_availableChecksTree.Nodes[0]);
				m_availableChecksTree.EndUpdate();
			}

			RefreshHistoryPane(null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override int PreferredHeight
		{
			get
			{
				int nodeCount = m_availableChecksTree.GetNodeCount(true);
				if (nodeCount == 0)
					return pnlButtons.Height + 20;

				// We're assuming the nodes are all of uniform height.
				// Add one node for extra padding.
				Rectangle rc = m_availableChecksTree.Nodes[0].Bounds;
				return ++nodeCount * rc.Height + pnlButtons.Height;
			}
		}

		#endregion

		#region Event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Occurs when the list of check results is updated.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void OnErrorsUpdated()
		{
			// Need UOW since may delete irrelevant errors - like errors related to invalid characters that
			// were added through the right-click menu option.
			NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(m_cache.ServiceLocator.ActionHandler, () =>
			{
				IFdoOwningSequence<IScrBookAnnotations> booksAnnotations =
					m_cache.LangProject.TranslatedScriptureOA.BookAnnotationsOS;

				foreach (IScrBookAnnotations annotations in booksAnnotations)
				{
					foreach (IScrScriptureNote note in annotations.NotesOS)
					{
						if ((int)note.ResolutionStatus == (int)CheckingStatus.StatusEnum.Irrelevant)
							m_cache.DomainDataByFlid.DeleteObj(note.Hvo);
					}
				}
			});

			if (m_checkErrorsList != null)
				m_checkErrorsList.LoadCheckingErrors(IdsOfSelectedChecks);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the run checks button is clicked.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void OnRunChecks(object sender, EventArgs e)
		{
			if (!AreAllSelectedChecksRunable)
				return;

			if (m_bookFilter.BookCount == 0)
			{
				m_btnRunChecks.Enabled = false;
				return;
			}

			if (RunChecksClick != null)
				RunChecksClick(this, EventArgs.Empty);

			if (m_ComboBox.DropDown.Visible)
				m_ComboBox.HideDropDown();

			using (var unitOfWork =
					new NonUndoableUnitOfWorkHelper(m_cache.ServiceLocator.GetInstance<IActionHandler>()))
			{
				Logger.WriteEvent("Running editorial checks");
				using (new WaitCursor(Parent))
				{
					foreach (int bookId in m_bookFilter.BookIds)
					{
						m_chkDataSource.GetText(bookId, 0);
						foreach (IScriptureCheck check in SelectedChecks)
							m_chkDataSource.RunCheck(check);
					}

					m_btnApplyFilter.Enabled = false;
					unitOfWork.RollBack = false;
				}
			}
			OnErrorsUpdated();
			RefreshCheckTree();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies some of the checks, that if they are selected in the tree, that the user
		/// has provided settings required for the check.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool AreAllSelectedChecksRunable
		{
			get
			{
				// Make sure that if the characters check is selected,
				// we have some valid characters.
				ScrChkTreeNode node = GetCheckNodeIfSelected(StandardCheckIds.kguidCharacters);
				if (node != null)
				{
					VerifyCharactersCheckCanRun();

					if (IsValidCharIncomplete)
						EnsureCheckIsUnselected(StandardCheckIds.kguidCharacters);

					if (SelectedChecks.Length == 0)
					{
						m_btnRunChecks.Enabled = false;
						return false;
					}
				}

				// Make sure that if the matched pairs check is selected, we have some.
				node = GetCheckNodeIfSelected(StandardCheckIds.kguidMatchedPairs);
				if (node != null)
				{
					VerifyMatchedPairCheckCanRun();

					if (IsMatchedPairsListEmpty)
						EnsureCheckIsUnselected(StandardCheckIds.kguidMatchedPairs);

					if (SelectedChecks.Length == 0)
					{
						m_btnRunChecks.Enabled = false;
						return false;
					}
				}

				// Make sure that if the punctuation check is selected,
				// we have some punc. patterns.
				node = GetCheckNodeIfSelected(StandardCheckIds.kguidPunctuation);
				if (node != null)
				{
					VerifyPunctuationCheckCanRun();

					if (IsPunctuationPatternsEmpty)
						EnsureCheckIsUnselected(StandardCheckIds.kguidPunctuation);

					if (SelectedChecks.Length == 0)
					{
						m_btnRunChecks.Enabled = false;
						return false;
					}
				}

				// Make sure that if the punctuation check is selected,
				// we have some punc. patterns.
				node = GetCheckNodeIfSelected(StandardCheckIds.kguidQuotations);
				if (node != null)
				{
					VerifyQuotationMarksCheckCanRun();

					if (AreQuotationMarksEmpty)
						EnsureCheckIsUnselected(StandardCheckIds.kguidQuotations);

					if (SelectedChecks.Length == 0)
					{
						m_btnRunChecks.Enabled = false;
						return false;
					}
				}

				return true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when show results button is pressed.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_btnApplyFilter_Click(object sender, EventArgs e)
		{
			if (ShowChecksClick != null)
				ShowChecksClick(this, EventArgs.Empty);

			if (m_ComboBox.DropDown.Visible)
				m_ComboBox.HideDropDown();

			using (new WaitCursor(Parent))
				OnErrorsUpdated();

			m_btnApplyFilter.Enabled = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the help button is clicked.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void OnHelpClicked(object sender, EventArgs e)
		{
			if (m_ComboBox.DropDown.Visible)
				m_ComboBox.HideDropDown();

			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtpScrChecks");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called after the checked checks changed.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.Windows.Forms.TreeViewEventArgs"/> instance
		/// containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void OnAfterTreeNodeChecked(object sender, TreeViewEventArgs e)
		{
			// When the characters check is selected but no valid characters have been
			// specified, then tell the user and make sure the check is unselected if
			// the user decides not to add any at this time.
			ScrChkTreeNode node = e.Node as ScrChkTreeNode;
			if (node != null)
			{
				TriStateTreeView tv = m_availableChecksTree as TriStateTreeView;
				if (tv.GetChecked(node) == TriStateTreeView.CheckState.Checked)
				{
					if (node.ScrCheck.CheckId == StandardCheckIds.kguidCharacters)
						VerifyCharactersCheckCanRun();
					else if (node.ScrCheck.CheckId == StandardCheckIds.kguidMatchedPairs)
						VerifyMatchedPairCheckCanRun();
					else if (node.ScrCheck.CheckId == StandardCheckIds.kguidPunctuation)
						VerifyPunctuationCheckCanRun();
					else if (node.ScrCheck.CheckId == StandardCheckIds.kguidQuotations)
						VerifyQuotationMarksCheckCanRun();
				}
			}

			// Get all the nodes that are checked.
			TreeNode[] checkedNodes = m_availableChecksTree.GetNodesOfTypeWithState(
				typeof(ScrChkTreeNode), TriStateTreeView.CheckState.Checked);

			// Update the enabled state of the buttons accordingly.
			m_btnApplyFilter.Enabled = true;
			m_btnRunChecks.Enabled = (checkedNodes.Length > 0) && (m_bookFilter.BookCount > 0);

			// Create the text for the drop-down when the control is docked to the top.
			string separator = TeResourceHelper.GetResourceString("kstidScrChkNameSeparator");
			StringBuilder bldr = new StringBuilder();
			for (int i = 0; i < checkedNodes.Length; i++)
			{
				if (checkedNodes[i] is ScrChkTreeNode)
				{
					bldr.Append(((ScrChkTreeNode)checkedNodes[i]).CheckName);

					if (i < checkedNodes.Length - 1)
						bldr.Append(separator);
				}
			}

			// Check if the list of selected checks changed. Update the panel's text.
			// The only place this text is displayed is in the combo box and the combo
			// box is only displayed when this control is docked to the top. We set the
			// panel's text because it is the control specified for the combo's
			// DropDownControl, which is the control from which the combo gets its text.
			if (pnlOuter.Text != bldr.ToString())
			{
				pnlOuter.Text = bldr.ToString();
				if (m_checkErrorsList != null)
					m_checkErrorsList.IsStale = true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that there are enough letters in the valid characters list. If not, then
		/// the characters check cannot run and should not be allowed to be checked. If the
		/// list does not contain enough letters, give the user the opportunity to add
		/// characters to the list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void VerifyCharactersCheckCanRun()
		{
			// Check if the valid characters list contains enough letters
			if (!IsValidCharIncomplete)
				return;

			// The valid characters list does not contain enough letters and the characters check is about to be
			// checked, so display the Valid Characters dialog.
			string msg = TeResourceHelper.GetResourceString("kstidIncompleteValidCharsForCharCheckMsg");
			if (MessageBox.Show(msg, m_app.ApplicationName,
				MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
			{
				OnValidCharacters(null);
			}

			if (IsValidCharIncomplete)
				EnsureCheckIsUnselected(StandardCheckIds.kguidCharacters);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that there are matched pairs defined in the vern. writing system. If not,
		/// then the matched pairs check cannot run and should not be allowed to be checked.
		/// If the list is empty, give the user the opportunity to add some to the list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void VerifyMatchedPairCheckCanRun()
		{
			// Check if the matched pairs list is empty
			if (!IsMatchedPairsListEmpty)
				return;

			// The list is empty and the matched pairs check is about to be checked,
			// so display a dialog asking the user if he would like to add some.
			string msg = TeResourceHelper.GetResourceString("kstidNoMatchedPairsForCheckMsg");
			if (MessageBox.Show(msg, m_app.ApplicationName,
				MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
			{
				OnMatchedPairs(null);
			}

			if (IsMatchedPairsListEmpty)
				EnsureCheckIsUnselected(StandardCheckIds.kguidMatchedPairs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that there are punctuation patterns defined in the vern. writing system.
		/// If not, then the punctuation check cannot run and should not be allowed to be
		/// checked. If the list is empty, give the user the opportunity to add some to the
		/// list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void VerifyPunctuationCheckCanRun()
		{
			// Check if the punctuation patterns list is empty
			if (!IsPunctuationPatternsEmpty)
				return;

			// The list is empty and the punctuation check is about to be checked,
			// so display a dialog asking the user if he would like to add some.
			string msg = TeResourceHelper.GetResourceString("kstidNoPuncPatternsForCheckMsg");
			if (MessageBox.Show(msg, m_app.ApplicationName,
				MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
			{
				OnPunctuationPatterns(null);
			}

			if (IsPunctuationPatternsEmpty)
				EnsureCheckIsUnselected(StandardCheckIds.kguidPunctuation);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that there are quotation marks defined in the vern. writing system.
		/// If not, then the punctuation check cannot run and should not be allowed to be
		/// checked. If the list is empty, give the user the opportunity to add some to the
		/// list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void VerifyQuotationMarksCheckCanRun()
		{
			if (!AreQuotationMarksEmpty)
				return;

			string msg = TeResourceHelper.GetResourceString("kstidNoQuotationMarksForCheckMsg");
			if (MessageBox.Show(msg, m_app.ApplicationName,
				MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
			{
				OnQuotationMarks(null);
			}

			if (AreQuotationMarksEmpty)
				EnsureCheckIsUnselected(StandardCheckIds.kguidQuotations);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ensures the specified check is not selected in the checks tree.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void EnsureCheckIsUnselected(Guid checkId)
		{
			// Find the characters check node among the selected nodes.
			ScrChkTreeNode node = GetCheckNodeIfSelected(checkId);

			// Now make sure we have the character's check node and that it's unselected.
			// If chrCheckNode is null, it must mean it's already unselected.
			if (node != null)
			{
				TriStateTreeView tv = m_availableChecksTree as TriStateTreeView;
				tv.AfterCheck -= OnAfterTreeNodeChecked;
				tv.SetChecked(node, TriStateTreeView.CheckState.Unchecked);
				tv.AfterCheck += OnAfterTreeNodeChecked;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return the tree node for the specified check.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private ScrChkTreeNode GetCheckNodeIfSelected(Guid checkId)
		{
			TreeNode[] checkedNodes =
				m_availableChecksTree.GetNodesWithState(TriStateTreeView.CheckState.Checked);

			// Find the check node among the selected nodes.
			foreach (TreeNode node in checkedNodes)
			{
				ScrChkTreeNode sctn = node as ScrChkTreeNode;
				if (sctn != null && sctn.ScrCheck.CheckId == checkId)
					return sctn;
			}

			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return whether the valid char list contains 3 or less letters.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool IsValidCharIncomplete
		{
			get
			{
				ValidCharacters valChars = ValidCharacters.Load(CurrVernWritingSystem, ReportLoadException, FwDirectoryFinder.LegacyWordformingCharOverridesFile);
				return valChars.WordformingLetterCount <= 3;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether or not there are any matched pairs specified.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool IsMatchedPairsListEmpty
		{
			get
			{
				IWritingSystem ws = CurrVernWritingSystem;
				if (ws == null)
					return true;

				MatchedPairList pairs = MatchedPairList.Load(ws.MatchedPairs,
					ws.DisplayLabel);
				return (pairs == null || pairs.Count == 0);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether or not there are any punctuation patterns specified.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool IsPunctuationPatternsEmpty
		{
			get
			{
				IWritingSystem ws = CurrVernWritingSystem;
				if (ws == null)
					return true;

				PuncPatternsList patterns = PuncPatternsList.Load(ws.PunctuationPatterns,
					ws.DisplayLabel);
				return (patterns == null || patterns.Count == 0);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether or not the first level quotation marks are both
		/// specified.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool AreQuotationMarksEmpty
		{
			get
			{
				IWritingSystem ws = CurrVernWritingSystem;
				if (ws == null)
					return true;

				QuotationMarksList qmarks = QuotationMarksList.Load(ws.QuotationMarks,
					ws.DisplayLabel);
				foreach (QuotationMarks qm in qmarks.QMarksList)
				{
					if (string.IsNullOrEmpty(qm.Opening) || string.IsNullOrEmpty(qm.Closing))
						return true;
				}

				return false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the current vernacular writing system from the cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private IWritingSystem CurrVernWritingSystem
		{
			get
			{
				return m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the mouse moves over the checks tree. When the mouse moves over a
		/// node that is a scripture check, a tooltip will be displayed that shows the
		/// description of the check and the list of books (in the book filter) with the
		/// last time the check was run for each of the books.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void OnChecksTreeMouseMove(object sender, MouseEventArgs e)
		{
			TreeViewHitTestInfo htInfo = m_availableChecksTree.HitTest(e.Location);
			ScrChkTreeNode node = htInfo.Node as ScrChkTreeNode;

			if (node == null)
			{
				m_nodeMouseOver = null;
				m_nodeTip.Hide(m_availableChecksTree);
				return;
			}

			if (m_nodeMouseOver == node)
				return;

			string tip = InstalledScriptureChecks.GetCheckProperty(ScrFdoResources.ResourceManager,
				node.ScrCheck.CheckId, "Description", node.ScrCheck.Description);

			m_nodeTip.Show(tip, m_availableChecksTree, node.Bounds.X + 20, node.Bounds.Bottom);
			m_nodeMouseOver = node;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void OnAvailableChecksTreeMouseLeave(object sender, EventArgs e)
		{
			if (m_nodeTip != null)
			{
				// When tree node text is too wide to be completely displayed in the width
				// of the tree, hovering over the node will popup a tooltip-like thing that
				// shows all the text in the node. When this happens and the mouse is over
				// that tooltip-like thing, the system thinks it has just left the tree
				// control. In that case, we really don't want to treat it as though it
				// has. Therefore, only hide the tooltip full of books/check times when
				// the mouse has really left the rectangular area of the tree control.
				Point pt = MousePosition;
				pt = m_availableChecksTree.PointToClient(pt);
				if (!m_availableChecksTree.ClientRectangle.Contains(pt))
					m_nodeTip.Hide(m_availableChecksTree);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnDockUndockBegin(object sender, DockingEventArgs e)
		{
			ChecksViewWrapper wrapper = Parent as ChecksViewWrapper;
			bool initalActivation = (wrapper != null && wrapper.InitialActivation);

			if (m_persistence != null && !initalActivation)
				OnSaveSettings(m_persistence.SettingsKey);

			base.OnDockUndockBegin(sender, e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void RefreshControlsAfterDocking()
		{
			base.RefreshControlsAfterDocking();

			int margin = (Floaty.DockMode == DockStyle.Top || Floaty.DockMode == DockStyle.Bottom ? 3 : 0);

			m_btnRunChecks.Left = margin;
			m_btnHelp.Margin = new Padding(m_btnHelp.Margin.Left,
				m_btnHelp.Margin.Top, margin, m_btnHelp.Margin.Bottom);

			string settingName;
			bool vertical = false;
			switch (Floaty.DockMode)
			{
				case DockStyle.Top:
					settingName = "HistoryPaneRatioDockedTop";
					vertical = true;
					break;
				case DockStyle.None: settingName = "HistoryPaneRatioUndocked"; break;
				default: settingName = "HistoryPaneRatioDockedSide"; break;
			}
			LoadHistoryPaneInfo(settingName, vertical);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void OnUndocked(object sender, EventArgs e)
		{
			base.OnUndocked(sender, e);
			OnDocking(sender, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines how many rows the buttons at the bottom of the control will occupy and
		/// arranges them if the calculated values are not the actual values.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void pnlButtons_ClientSizeChanged(object sender, EventArgs e)
		{
			if (m_adjustingButtonPanel)
				return;

			m_adjustingButtonPanel = true;
			int availWidth = pnlButtons.ClientSize.Width;

			Point applyBtnLoc;
			Point helpBtnLoc;
			int newPnlHeight;

			// Find the needed width to accodate one row of buttons.
			int neededWidth = m_btnRunChecks.Width + m_btnApplyFilter.Width +
				m_btnHelp.Width + (m_dxButtonGap * 2);

			if (neededWidth <= availWidth)
			{
				// Arrange the buttons all in one row.
				newPnlHeight = m_buttonPanelHeight1;
				helpBtnLoc =
					new Point(availWidth - m_btnHelp.Width - m_btnHelp.Margin.Right, m_btnRunChecks.Top);

				applyBtnLoc = new Point(m_btnRunChecks.Right + m_dxButtonGap, m_btnRunChecks.Top);
			}
			else
			{
				// Find the needed width to accomodate two rows of buttons.
				neededWidth = m_btnRunChecks.Width + m_btnApplyFilter.Width + m_dxButtonGap;

				if (neededWidth <= availWidth)
				{
					// Arrange the buttons in two rows.
					newPnlHeight = m_buttonPanelHeight2;
					helpBtnLoc = new Point(m_btnRunChecks.Left, m_btnRunChecks.Bottom + m_dyButtonGap);
					applyBtnLoc = new Point(m_btnRunChecks.Right + m_dxButtonGap, m_btnRunChecks.Top);
				}
				else
				{
					// Arrange the buttons in three rows.
					newPnlHeight = m_buttonPanelHeight3;
					applyBtnLoc = new Point(m_btnRunChecks.Left, m_btnRunChecks.Bottom + m_dyButtonGap);
					helpBtnLoc = new Point(
						m_btnRunChecks.Left, applyBtnLoc.Y + m_btnApplyFilter.Height + m_dyButtonGap);
				}
			}

			// If the calculated new help button location is different
			// from the actual location, then move the help button.
			if (!helpBtnLoc.Equals(m_btnHelp.Location))
				m_btnHelp.Location = helpBtnLoc;

			// If the calculated apply filter button location is different
			// from the actual location, then move the apply filter button.
			if (!applyBtnLoc.Equals(m_btnApplyFilter.Location))
				m_btnApplyFilter.Location = applyBtnLoc;

			// If the calculated new height of the buttons panel is different
			// from the actual height, then set the height of the panel.
			if (pnlButtons.Height != newPnlHeight)
			{
				pnlButtons.Height = newPnlHeight;

				// Calculate the new top for the buttons panel and if it's different
				// from the actual top, then set the top of the panel.
				pnlButtons.Top = pnlOuter.ClientSize.Height - pnlButtons.Height;
			}

			m_adjustingButtonPanel = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void lblHistory_SizeChanged(object sender, EventArgs e)
		{
			AdjustCheckNameDisplayRectangle();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Keep track of the node on which the user has right clicked
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_availableChecksTree_MouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right)
			{
				TreeViewHitTestInfo tvhti = m_availableChecksTree.HitTest(e.Location);
				m_nodeLastRightClickedOn = tvhti.Node as ScrChkTreeNode;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Update the check history pane.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_availableChecksTree_AfterSelect(object sender, TreeViewEventArgs e)
		{
			RefreshHistoryPane(e.Node as ScrChkTreeNode);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Refreshes the history pane using the check from the specified node. If the
		/// specified node is null, then the currently selected node in the tree is chosen. If
		/// there is no current node or the current node isn't a check (e.g. the "Basic" node)
		/// then the history pane shows nothing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void RefreshHistoryPane(ScrChkTreeNode currNode)
		{
			if (currNode == null)
				currNode = m_availableChecksTree.SelectedNode as ScrChkTreeNode;

			if (currNode == null)
			{
				lblCheckName.Text = string.Empty;
				txtHistory.Text = string.Empty;
				return;
			}

			lblCheckName.Text = InstalledScriptureChecks.GetCheckProperty(ScrFdoResources.ResourceManager,
				currNode.ScrCheck.CheckId, "Name", currNode.ScrCheck.CheckName);

			StringBuilder bldr = new StringBuilder();
			foreach (string bookRunInfo in currNode.TipBookList)
				bldr.AppendLine(bookRunInfo);

			txtHistory.Text = bldr.ToString();
			AdjustCheckNameDisplayRectangle();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void AdjustCheckNameDisplayRectangle()
		{
			using (Graphics g = lblCheckName.CreateGraphics())
			{
				Size sz = TextRenderer.MeasureText(lblCheckName.Text, lblCheckName.Font);
				m_chkNameTextRect = lblCheckName.ClientRectangle;
				m_chkNameTextRect.Height = sz.Height * (lblCheckName.Width < sz.Width ? 2 : 1) + 2;
				m_chkNameTextRect.Y += 3;
				lblCheckName.Height = m_chkNameTextRect.Height + 8;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the NodeMouseClick event of the m_availableChecksTree control to select a
		/// node when it is right-clicked.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.Windows.Forms.TreeNodeMouseClickEventArgs"/>
		/// instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_availableChecksTree_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
		{
			if (e.Button == MouseButtons.Right)
			{
				m_availableChecksTree.SelectedNode = e.Node;
				return;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void lblCheckName_Paint(object sender, PaintEventArgs e)
		{
			Rectangle rc = lblCheckName.ClientRectangle;
			e.Graphics.FillRectangle(SystemBrushes.Window, rc);

			if (string.IsNullOrEmpty(lblCheckName.Text))
				return;

			// Draw the check name.
			TextRenderer.DrawText(e.Graphics, lblCheckName.Text, lblCheckName.Font,
				m_chkNameTextRect, lblCheckName.ForeColor, TextFormatFlags.EndEllipsis |
				TextFormatFlags.WordBreak);

			rc.Y = rc.Bottom - 4;
			rc.Height = 1;

			// Draw the line under the check name.
			using (LinearGradientBrush br = new LinearGradientBrush(rc,
				SystemColors.WindowText, SystemColors.Window, 0f))
			{
				e.Graphics.FillRectangle(br, rc);
			}
		}

		#endregion

		#region Saving/Loading settings
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Save misc. view settings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnLoadSettings(RegistryKey key)
		{
			base.OnLoadSettings(key);

			if (m_availableChecksTree.Nodes.Count == 0)
				return;

			string value = key.GetValue("SelectedCheck", null) as string;
			if (string.IsNullOrEmpty(value))
			{
				m_availableChecksTree.SelectedNode = m_availableChecksTree.Nodes[0];
				return;
			}

			Guid checkId = new Guid(value);

			// Get all the checked ScrChkTreeNodes
			List<TreeNode> nodes = new List<TreeNode>(m_availableChecksTree.GetNodesOfTypeWithState(
				typeof(ScrChkTreeNode), TriStateTreeView.CheckState.Checked));

			// Add to it, all the unchecked ScrChkTreeNodes
			nodes.AddRange(m_availableChecksTree.GetNodesOfTypeWithState(
				typeof(ScrChkTreeNode), TriStateTreeView.CheckState.Unchecked));

			// Find the node corresponding to the check id that was found in the registry.
			foreach (TreeNode node in nodes)
			{
				if (((ScrChkTreeNode)node).ScrCheck.CheckId == checkId)
				{
					m_availableChecksTree.SelectedNode = node;
					return;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Save misc. view settings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnSaveSettings(RegistryKey key)
		{
			base.OnSaveSettings(key);

			if (Floaty == null)
				return;

			if (Floaty.DockMode == DockStyle.Top)
				SaveHistoryPaneInfo(key, "HistoryPaneRatioDockedTop", true);
			else if (Floaty.DockMode == DockStyle.None)
				SaveHistoryPaneInfo(key, "HistoryPaneRatioUndocked", false);
			else
				SaveHistoryPaneInfo(key, "HistoryPaneRatioDockedSide", false);

			ScrChkTreeNode node = m_availableChecksTree.SelectedNode as ScrChkTreeNode;
			if (node != null)
				key.SetValue("SelectedCheck", node.ScrCheck.CheckId);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SaveHistoryPaneInfo(RegistryKey key, string settingName, bool IsVertical)
		{
			if (key == null)
				return;

			float ratio = (float)splitContainer.SplitterDistance /
				(float)(IsVertical ? splitContainer.Width : splitContainer.Height);

			if (ratio < 1.0f)
				key.SetValue(settingName, ratio);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void LoadHistoryPaneInfo(string settingName, bool isVertical)
		{
			if (m_persistence == null)
				return;

			string value = m_persistence.SettingsKey.GetValue(settingName, null) as string;
			float ratio;
			if (!float.TryParse(value, out ratio) || ratio >= 1.0f)
				ratio = 0.66f;

			int distance = (int)((isVertical ?
				splitContainer.Width : splitContainer.Height) * ratio);

			if (distance >= splitContainer.Panel1MinSize)
				splitContainer.SplitterDistance = distance;
		}

		#endregion

		#region Message handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Display help for the selected Check
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnEditorialChecksHelp(object args)
		{
			string guidStr = m_nodeLastRightClickedOn.ScrCheck.CheckId.ToString();
			ShowHelp.ShowHelpTopic(m_helpTopicProvider,
				(m_nodeLastRightClickedOn == null) ? "khtpScrChecksOverview" :
				"khtpScrChecks_" + guidStr.ToUpper().Replace("-", string.Empty));

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Show the scripture properties dialog for the chapter/verse check.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnChapterVerseNumbers(object args)
		{
			if (m_tmAdapter == null || m_tmAdapter.MessageMediator == null)
				return false;

			// Let the main window handle showing the dialog.
			m_tmAdapter.MessageMediator.SendMessage("ScripturePropertiesForCVCheck", null);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine whether or not the menu item for the scripture properties dialog
		/// (chapter/verse numbers tab) dialog box should be visible.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateChapterVerseNumbers(object args)
		{
			return UpdateCheckContextMenu(args as TMItemProperties,
				StandardCheckIds.kguidChapterVerse);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Display the valid characters dialog box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnValidCharacters(object args)
		{
			if (ValidCharactersDlg.RunDialog(m_cache, m_app, this, m_helpTopicProvider))
			{
				m_cache.ServiceLocator.WritingSystemManager.Save();
				if (IsValidCharIncomplete)
					EnsureCheckIsUnselected(StandardCheckIds.kguidCharacters);
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine whether or not the menu item for the valid characters dialog box should be
		/// visible.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateValidCharacters(object args)
		{
			return UpdateCheckContextMenu(args as TMItemProperties,
				StandardCheckIds.kguidCharacters);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Display the punctuation dialog box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnMatchedPairs(object args)
		{
			ShowPunctuationDialog(StandardCheckIds.kguidMatchedPairs);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine whether or not the menu item for the punctuation dialog box should be
		/// visible.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateMatchedPairs(object args)
		{
			return UpdateCheckContextMenu(args as TMItemProperties,
				StandardCheckIds.kguidMatchedPairs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Display the punctuation dialog box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnPunctuationPatterns(object args)
		{
			ShowPunctuationDialog(StandardCheckIds.kguidPunctuation);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine whether or not the menu item for the punctuation dialog box should be
		/// visible.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdatePunctuationPatterns(object args)
		{
			return UpdateCheckContextMenu(args as TMItemProperties,
				StandardCheckIds.kguidPunctuation);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Display the punctuation dialog box (quotation marks tab).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnQuotationMarks(object args)
		{
			ShowPunctuationDialog(StandardCheckIds.kguidQuotations);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine whether or not the menu item for the quotation marks dialog box (which
		/// is really the punctuation dialog) should be visible.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateQuotationMarks(object args)
		{
			return UpdateCheckContextMenu(args as TMItemProperties,
				StandardCheckIds.kguidQuotations);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Display the punctuation dialog box, using the specified guid to determine what
		/// tab on the dialog is initially brought to front.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ShowPunctuationDialog(Guid initialTab)
		{
			if (PunctuationDlg.RunDialog(m_cache, m_app, this, m_helpTopicProvider, initialTab))
				m_cache.ServiceLocator.WritingSystemManager.Save();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine whether or not the specified context menu is visible for the specified
		/// check.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool UpdateCheckContextMenu(TMItemProperties itemProps, Guid checkId)
		{
			if (itemProps == null)
				return false;

			ScrChkTreeNode node = m_availableChecksTree.SelectedNode as ScrChkTreeNode;
			itemProps.Visible = (node != null && node.ScrCheck.CheckId == checkId);
			itemProps.Enabled = true;
			itemProps.Update = true;
			return true;
		}

		#endregion

		#region IxCoreColleague Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IxCoreColleague[] GetMessageTargets()
		{
			return new IxCoreColleague[] { this };
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
		public int Priority
		{
			get { return (int)ColleaguePriority.Medium; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Not used in TE.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Init(Mediator mediator, System.Xml.XmlNode configurationParameters)
		{
		}

		#endregion

		#region Error-handling helpers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the support e-mail address (for TE).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static string SupportEmailAddress
		{
			get { return TeResourceHelper.GetResourceString("kstidSupportEmail"); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reports a ValidCharacters load exception.
		/// </summary>
		/// <param name="e">The exception.</param>
		/// ------------------------------------------------------------------------------------
		void ReportLoadException(ArgumentException e)
		{
			ErrorReporter.ReportException(e, m_app.SettingsKey, SupportEmailAddress, ParentForm, false);
		}
		#endregion
	}

	#endregion

	#region ScrChkTreeNode class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class ScrChkTreeNode : TreeNode
	{
		private DateTime m_lastRun = DateTime.MinValue;
		private IScriptureCheck m_scrCheck;
		private string[] m_tipBookList;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:ScrChkTreeNode"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal ScrChkTreeNode(string checkName, DateTime lastRun, string[] tipBookList,
			IScriptureCheck scrCheck)
		{
			Name = checkName;
			m_lastRun = lastRun;
			m_scrCheck = scrCheck;
			m_tipBookList = tipBookList;
			Text = ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the name of the check. This is the same value as the node's Name.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal string CheckName
		{
			get { return base.Name; }
			set
			{
				base.Name = value;
				Text = ToString();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the last run.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal DateTime LastRun
		{
			get { return m_lastRun; }
			set
			{
				m_lastRun = value;
				Text = ToString();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the list of books and the last time the check was run for each.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string[] TipBookList
		{
			get { return m_tipBookList; }
			set { m_tipBookList = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the check.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal IScriptureCheck ScrCheck
		{
			get { return m_scrCheck; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Writes to the registry the checked state of check associated with this node.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void WriteCheckedStateToReg(RegistryKey settingsKey)
		{
			TriStateTreeView tv = TreeView as TriStateTreeView;

			if (settingsKey != null && tv != null)
			{
				bool isChecked =
					((tv.GetChecked(this) & TriStateTreeView.CheckState.Checked) == TriStateTreeView.CheckState.Checked);

				settingsKey.SetValue(m_scrCheck.CheckId.ToString(), isChecked);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reads from the registry the checked state of check associated with this node.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ReadCheckedStateFromReg(RegistryKey settingsKey)
		{
			TriStateTreeView tv = TreeView as TriStateTreeView;

			if (settingsKey != null && tv != null)
			{
				string value = settingsKey.GetValue(m_scrCheck.CheckId.ToString(), "False") as string;
				bool boolValue = true;
				bool.TryParse(value, out boolValue);
				tv.SetChecked(this, (boolValue ?
					TriStateTreeView.CheckState.Checked : TriStateTreeView.CheckState.Unchecked));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			string date;

			if (m_lastRun == DateTime.MinValue)
				date = TeResourceHelper.GetResourceString("kstidScrCheckNeverRunMsg");
			else
			{
				string fmtDate =
					TeResourceHelper.GetResourceString("kstidScrCheckRunDateTimeFormat");
				date = string.Format(fmtDate, m_lastRun.ToString());
			}

			string fmtName = TeResourceHelper.GetResourceString("kstidScrCheckNameFormat");
			return string.Format(fmtName, Name, date);
		}
	}

	#endregion
}

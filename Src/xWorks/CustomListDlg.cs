// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: CustomListDlg.cs
// Responsibility: GordonM
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;
using XCore;
using SIL.FieldWorks.FDO;
using System.Collections.Generic;
using SIL.CoreImpl;

namespace SIL.FieldWorks.XWorks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Dialog for adding TopicListEditor-like custom lists to a Fieldworks project.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class CustomListDlg : Form, IFWDisposable
	{
		protected string s_helpTopic = "khtpCustomLists";
		private HelpProvider m_helpProvider;
		protected readonly Mediator m_mediator;
		private FdoCache m_cache;
		protected bool m_finSetup;
		protected LabeledMultiStringControl m_lmscListName;
		protected LabeledMultiStringControl m_lmscDescription;
		private IVwStylesheet m_stylesheet;
		protected List<IWritingSystem> m_uiWss;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:CustomListDlg"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public CustomListDlg(Mediator mediator)
		{
			m_finSetup = true;
			InitializeComponent();
			m_mediator = mediator;
			m_btnOK.Enabled = false;

			InitializeWSCombo();
			InitializeDisplayByCombo();
		}

		private void CustomListDlg_Load(object sender, EventArgs e)
		{
			if (m_mediator != null && m_mediator.HelpTopicProvider != null)
				InitializeHelpProvider();
			m_stylesheet = FontHeightAdjuster.StyleSheetFromMediator(m_mediator);
			InitializeMultiStringControls();
			InitializeDialogFields();
			m_finSetup = false;
		}

		protected virtual void InitializeDialogFields()
		{
		}

		/// <summary>
		/// Used by subclass ConfigureList to keep the user from changing a list marked
		/// as closed.
		/// </summary>
		protected void DisableChanges()
		{
			m_lmscListName.Enabled = false;
			m_chkBoxHierarchy.Enabled = false;
			m_chkBoxSortBy.Enabled = false;
			m_chkBoxDuplicate.Enabled = false;
			m_wsCombo.Enabled = false;
			m_displayByCombo.Enabled = false;
			m_lmscDescription.Enabled = false;
			m_closedListLabel.Visible = true;

			// Want to be able to click OK, but there'll never be any changes.
			EnableOKButton(true);
		}

		protected void EnableOKButton(bool fenable)
		{
			m_btnOK.Enabled = fenable;
		}

		protected void InitializeMultiStringControls()
		{
			// protected for testing. N.B. if testing, set test cache first!
			m_uiWss = GetUiWritingSystemAndEnglish();
			//m_delta = 0;
			m_lmscListName = ReplaceTextBoxWithMultiStringBox(m_tboxListName,
				m_stylesheet);
			m_lmscDescription = ReplaceTextBoxWithMultiStringBox(m_tboxDescription,
				m_stylesheet);
		}

		/// <summary>
		/// Gets the current User Writing System and (if it's not English)
		/// also gets the English Writing System.
		/// </summary>
		/// <returns></returns>
		protected List<IWritingSystem> GetUiWritingSystemAndEnglish()
		{
			// Protected for testing
			Debug.Assert(Cache != null, "Can't install languages without a cache!");
			var wsMgr = Cache.ServiceLocator.WritingSystemManager;
			var userWs = wsMgr.UserWritingSystem;
			var result = new List<IWritingSystem> {userWs};
			if (userWs.LanguageSubtag.Code == "en")
				return result;

			// If English is not the DefaultUserWs, add it to the list.
			IWritingSystem engWs;
			if (wsMgr.TryGet("en", out engWs))
				result.Add(engWs);
			return result;
		}

		private LabeledMultiStringControl ReplaceTextBoxWithMultiStringBox(TextBox tb,
			IVwStylesheet stylesheet)
		{
			Debug.Assert(Cache != null, "Need a cache to setup a MultiStringBox.");
			tb.Hide();
			if (m_uiWss.Count == 0)
				return null;
			var ms = new LabeledMultiStringControl(Cache, m_uiWss, stylesheet);
			ms.Location = tb.Location;
			ms.Width = tb.Width;
			ms.Anchor = tb.Anchor;
			ms.AccessibleName = tb.AccessibleName;

			// Grow the dialog and move all lower controls down to make room.
			Controls.Remove(tb);
			ms.TabIndex = tb.TabIndex;	// assume the same tab order as the 'designed' control
			Controls.Add(ms);
			FontHeightAdjuster.GrowDialogAndAdjustControls(this, ms.Height - tb.Height, ms);
			return ms;
		}

		protected static void LoadAllMultiAlternatives(IMultiAccessorBase multiField,
			LabeledMultiStringControl destination)
		{
			var cws = destination.NumberOfWritingSystems;
			for (var i = 0; i < cws; i++)
			{
				var curWs = destination.Ws(i);
				if (curWs <= 0)
					continue;
				int actualWs;
				ITsString tssStr;
				if (!multiField.TryWs(curWs, out actualWs, out tssStr))
					continue;
				destination.SetValue(curWs, tssStr);
			}
		}

		protected static void SetAllMultiAlternatives(IMultiAccessorBase multiField,
			LabeledMultiStringControl source)
		{
			var cws = source.NumberOfWritingSystems;
			for (var i = 0; i < cws; i++)
			{
				var curWs = source.Ws(i);
				multiField.set_String(curWs, source.Value(curWs));
			}
		}

		/// <summary>
		/// Initializes the Writing Systems combo box and sets the selection to the 1st item.
		/// </summary>
		private void InitializeWSCombo()
		{
			m_wsCombo.Items.Add(new IdAndString<int>(WritingSystemServices.kwsAnals,
				xWorksStrings.AllAnalysisWs));
			m_wsCombo.Items.Add(new IdAndString<int>(WritingSystemServices.kwsVerns,
				xWorksStrings.AllVernacularWs));
			m_wsCombo.Items.Add(new IdAndString<int>(WritingSystemServices.kwsAnalVerns,
				xWorksStrings.AllAnalysisVernacularWs));
			m_wsCombo.Items.Add(new IdAndString<int>(WritingSystemServices.kwsVernAnals,
				xWorksStrings.AllVernacularAnalysisWs));
			m_wsCombo.SelectedIndex = 0;
			m_wsCombo.LostFocus += new EventHandler(m_wsCombo_LostFocus);
		}

		void m_wsCombo_LostFocus(object sender, EventArgs e)
		{
			var newIndex = m_wsCombo.FindString(m_wsCombo.Text);
			if (newIndex < 0)
				newIndex = 0;
			m_wsCombo.SelectedIndex = newIndex;
		}

		/// <summary>
		/// Initializes the "Display items in the list by" combo box and
		/// sets the selection to the 1st item.
		/// </summary>
		private void InitializeDisplayByCombo()
		{
			m_displayByCombo.Items.Add(new IdAndString<PossNameType>(PossNameType.kpntName,
				xWorksStrings.ksName));
			m_displayByCombo.Items.Add(new IdAndString<PossNameType>(PossNameType.kpntNameAndAbbrev,
				xWorksStrings.ksAbbrevName));
			m_displayByCombo.Items.Add(new IdAndString<PossNameType>(PossNameType.kpntAbbreviation,
				xWorksStrings.ksAbbreviation));
			m_displayByCombo.SelectedIndex = 0;
			m_displayByCombo.LostFocus += new EventHandler(m_displayByCombo_LostFocus);
		}

		private void m_displayByCombo_LostFocus(object sender, EventArgs e)
		{
			int newIndex = -1;
			// Can't use FindString() because it uses StartsWith instead of Equals in its
			// search.  See FWR-3436.
			for (int i = 0; i < m_displayByCombo.Items.Count; ++i)
			{
				if (m_displayByCombo.Items[i].ToString() == m_displayByCombo.Text)
				{
					newIndex = i;
					break;
				}
			}
			if (newIndex < 0)
				newIndex = 0;
			m_displayByCombo.SelectedIndex = newIndex;
		}

		/// <summary>
		/// Initializes the help provider for the Help... button.
		/// N.B. This method requires a mediator to function
		/// </summary>
		private void InitializeHelpProvider()
		{
			m_helpProvider = new HelpProvider { HelpNamespace = m_mediator.HelpTopicProvider.HelpFile };
			m_helpProvider.SetHelpKeyword(this, m_mediator.HelpTopicProvider.GetHelpString(s_helpTopic));
			m_helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
			m_helpProvider.SetShowHelp(this, true);
		}

		protected bool IsListNameEmpty
		{
			get
			{
				var cws = m_lmscListName.NumberOfWritingSystems;
				for (var i = 0; i < cws; i++)
				{
					if (!String.IsNullOrEmpty(m_lmscListName.Value(m_lmscListName.Ws(i)).Text))
						return false;
				}
				return true;
			}
		}

		/// <summary>
		/// Searches existing CmPossibilityLists for a Name alternative that matches one in the
		/// dialog's listName MultiStringControl.
		/// </summary>
		protected virtual bool IsListNameDuplicated
		{
			get
			{
				var repo = Cache.ServiceLocator.GetInstance<ICmPossibilityListRepository>();
				var wsf = Cache.WritingSystemFactory;
				var cws = m_lmscListName.NumberOfWritingSystems;
				for (var i = 0; i < cws; i++)
				{
					var curWs = m_lmscListName.Ws(i);
					var emptyStr = Cache.TsStrFactory.EmptyString(curWs).Text;
					var lmscName = m_lmscListName.Value(curWs).Text;
					if (repo.AllInstances().Where(
						list => list.Name.get_String(curWs).Text != emptyStr
							&& list.Name.get_String(curWs).Text == lmscName).Any())
						return true;
				}
				return false;
			}
		}

		private void m_btnOK_Click(object sender, EventArgs e)
		{
			if (m_mediator == null)
				return; // to save any changes requires a mediator and cache
			if (IsListNameEmpty)
			{
				MessageBox.Show(xWorksStrings.ksProvideValidListName, xWorksStrings.ksNoListName,
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}
			// Fixes FWR-2564 Crash typing in combo boxes.
			// This should fire m_wsCombo_LostFocus and m_displayByCombo_LostFocus events
			m_btnOK.Focus();
			DoOKAction();
		}

		/// <summary>
		/// Subclasses define what OK button does.
		/// </summary>
		protected virtual void DoOKAction()
		{
			ReloadListsArea();
		}

		private void ReloadListsArea()
		{
			m_mediator.SendMessage("ReloadAreaTools", "lists");
		}

		/// <summary>
		/// FDO cache. Use setter ONLY in tests
		/// </summary>
		protected FdoCache Cache
		{
			get
			{
				if (m_mediator == null && m_cache == null)
					return null;
				if (m_cache == null)
					m_cache = (FdoCache) m_mediator.PropertyTable.GetValue("cache");
				return m_cache;
			}
			set
			{
				m_cache = value;
			}
		}

		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			if (m_mediator == null)
				return;
			ShowHelp.ShowHelpTopic(m_mediator.HelpTopicProvider, s_helpTopic);
		}

		#region Implementation of IFWDisposable

		/// <summary>
		/// This method throws an ObjectDisposedException if IsDisposed returns true.
		/// This is the case where a method or property in an object is being used but
		/// the object itself is no longer valid.
		/// This method should be added to all public properties and methods of this
		/// object and all other objects derived from it (extensive).
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				if (components != null)
					components.Dispose();
				if (m_helpProvider != null)
					m_helpProvider.Dispose();
			}
			m_helpProvider = null;
			base.Dispose(disposing);
		}

		#endregion

		#region Public Properties

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the text in the "Name of List" text box for a particular ws.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void SetListName(ITsString listName, int ws)
		{
			m_lmscListName.SetValue(ws, listName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the text in the "Name of List" text box for a particular ws.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected ITsString GetListName(int ws)
		{
			return m_lmscListName.Value(ws);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the text in the "Description" text box for a particular ws.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void SetDescription(ITsString description, int ws)
		{
			m_lmscDescription.SetValue(ws, description);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the text in the "Description" text box for a particular ws.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected ITsString GetDescription(int ws)
		{
			return m_lmscDescription.Value(ws);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the selected WritingSystemServices Id from the writing system combo box.
		/// Can also set the currently selected item when editing properties of an existing list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int SelectedWs
		{
			get { return ((IdAndString<int>)m_wsCombo.SelectedItem).Id; }
			set
			{
				for (var i = 0; i < m_wsCombo.Items.Count; i++)
				{
					if (((IdAndString<int>)m_wsCombo.Items[i]).Id != value)
						continue;
					m_wsCombo.SelectedIndex = i;
					break;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the selected PossNameType from the "Display items in the list by" combo box.
		/// Can also set the currently selected item when editing properties of an existing list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public PossNameType DisplayBy
		{
			get { return ((IdAndString<PossNameType>)m_displayByCombo.SelectedItem).Id; }
			set { m_displayByCombo.SelectedIndex = (int)value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets whether the "Support hierarchy" checkbox is checked or not.
		/// Can also set an existing value when editing properties of an existing list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool SupportsHierarchy
		{
			get { return m_chkBoxHierarchy.Checked; }
			set { m_chkBoxHierarchy.Checked = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets whether the "Sort items by name" checkbox is checked or not.
		/// Can also set an existing value when editing properties of an existing list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool SortByName
		{
			get { return m_chkBoxSortBy.Checked; }
			set { m_chkBoxSortBy.Checked = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets whether the "Allow duplicate item names and abbrev." checkbox is checked or not.
		/// Can also set an existing value when editing properties of an existing list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool AllowDuplicate
		{
			get { return m_chkBoxDuplicate.Checked; }
			set { m_chkBoxDuplicate.Checked = value; }
		}

		#endregion

		#region Protected Changed Handlers

		protected virtual void m_wsCombo_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (m_finSetup)
				return;
			// editing subclass needs to know if this changed.
		}

		protected virtual void m_displayByCombo_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (m_finSetup)
				return;
			// editing subclass needs to know if this changed.
		}

		protected virtual void m_chkBoxHierarchy_CheckedChanged(object sender, EventArgs e)
		{
			if (m_finSetup)
				return;
			// editing subclass needs to know if this changed.
		}

		protected virtual void m_chkBoxSortBy_CheckedChanged(object sender, EventArgs e)
		{
			if (m_finSetup)
				return;
			// editing subclass needs to know if this changed.
		}

		protected virtual void m_chkBoxDuplicate_CheckedChanged(object sender, EventArgs e)
		{
			if (m_finSetup)
				return;
			// editing subclass needs to know if this changed.
		}

		#endregion
	}

	public class AddListDlg : CustomListDlg
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:AddListDlg"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public AddListDlg(Mediator mediator) :
			base(mediator)
		{
			s_helpTopic = "khtpNewCustomList";
		}

		protected override void DoOKAction()
		{
			if (IsListNameEmpty)
			{
				MessageBox.Show(xWorksStrings.ksProvideValidListName, xWorksStrings.ksNoListName,
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}
			var fCreated = CreateList();
			if (!fCreated)
				return;
			base.DoOKAction(); // refresh Lists area (to include new list)
		}

		protected override void InitializeDialogFields()
		{
			base.InitializeDialogFields();
			// OK button is enabled, but DoOKAction needs to test whether
			// the List Name contains anything or not.
			EnableOKButton(true);
		}

		/// <summary>
		/// Creates the new Custom list.
		/// </summary>
		/// <returns>true if a list was created.</returns>
		private bool CreateList()
		{
			if (m_mediator == null || Cache == null)
				throw new ArgumentException("Don't call this without a mediator and a cache.");
			if (IsListNameEmpty)
				// shouldn't ever get here because OK btn isn't enabled until name has a non-empty value
				throw new ArgumentException("Please provide a valid list name.");

			// This checks that we aren't creating a list with the same name as another list
			// but it doesn't always look like it because the name in the list and on FLEx (in Lists area)
			// aren't necessarily the same (e.g. Text Chart Markers is actually Chart Markers in the file).
			// This will likely get taken care of by a data migration to change internal list names.
			if (IsListNameDuplicated)
			{
				MessageBox.Show(xWorksStrings.ksChooseAnotherListName, xWorksStrings.ksDuplicateName,
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return false;
			}
			UndoableUnitOfWorkHelper.Do(xWorksStrings.ksUndoCreateList, xWorksStrings.ksRedoCreateList,
				Cache.ActionHandlerAccessor, () =>
				{
					var ws = Cache.DefaultUserWs; // get default ws
					var listName = m_lmscListName.Value(ws);
					var newList = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().CreateUnowned(
						listName.Text, ws);
					SetAllMultiAlternatives(newList.Name, m_lmscListName);

					// Set various properties of CmPossibilityList
					newList.DisplayOption = (int)DisplayBy;
					newList.PreventDuplicates = !AllowDuplicate;
					newList.IsSorted = SortByName;
					var wss = SelectedWs;
					newList.WsSelector = wss;
					if (wss == WritingSystemServices.kwsVerns || wss == WritingSystemServices.kwsVernAnals)
						newList.IsVernacular = true;
					else
						newList.IsVernacular = false;
					newList.Depth = 1;
					if (SupportsHierarchy)
						newList.Depth = 127;
					SetAllMultiAlternatives(newList.Description, m_lmscDescription);
				});
			return true;
		}
	}

	public class ConfigureListDlg : CustomListDlg
	{

		#region MemberData

		/// <summary>
		/// We are editing the properties of a list, this is it.
		/// </summary>
		private ICmPossibilityList m_curList;

		/// <summary>
		/// 'true' if we have made changes to an existing list's properties,
		/// 'false' if no changes have been made.
		/// </summary>
		private bool m_fchangesMade;

		private bool m_fnameChanged;
		private bool m_fhierarchyChanged;
		private bool m_fsortChanged;
		private bool m_fduplicateChanged;
		private bool m_fwsChanged;
		private bool m_fdisplayByChanged;
		private bool m_fdescriptionChanged;

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:ConfigureListDlg"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ConfigureListDlg(Mediator mediator, ICmPossibilityList possList) :
			base(mediator)
		{
			m_curList = possList;
			m_fchangesMade = false;
			Text = xWorksStrings.ksConfigureList;
			s_helpTopic = "khtpConfigureList";
		}

		protected override void InitializeDialogFields()
		{
			//m_curList is set in constructor
			if (m_curList.IsClosed)
				DisableChanges();
			LoadAllMultiAlternatives(m_curList.Name, m_lmscListName);
			LoadAllMultiAlternatives(m_curList.Description, m_lmscDescription);
			DisplayBy = (PossNameType)m_curList.DisplayOption;
			SortByName = m_curList.IsSorted;
			AllowDuplicate = !m_curList.PreventDuplicates;
			SupportsHierarchy = m_curList.Depth > 1;
			SetWritingSystemCombo();
			ResetAllFlags();
			// We want the OK button enabled, even though it won't do anything unless changes are made.
			EnableOKButton(true);
		}

		private void ResetAllFlags()
		{
			m_fchangesMade = false;
			m_fnameChanged = false;
			m_fhierarchyChanged = false;
			m_fsortChanged = false;
			m_fduplicateChanged = false;
			m_fwsChanged = false;
			m_fdisplayByChanged = false;
			m_fdescriptionChanged = false;
		}

		private void SetWritingSystemCombo()
		{
			var curWs = m_curList.WsSelector;
			switch (curWs)
			{
				case WritingSystemServices.kwsAnal:
				case WritingSystemServices.kwsAnals:
					SelectedWs = WritingSystemServices.kwsAnals;
					break;
				case WritingSystemServices.kwsVern:
				case WritingSystemServices.kwsVerns:
					SelectedWs = WritingSystemServices.kwsVerns;
					break;
				case WritingSystemServices.kwsLim:
					SelectedWs = WritingSystemServices.kwsAnals;
					break;
				default:
					SelectedWs = m_curList.WsSelector;
					break;
			}
		}

		protected override void m_chkBoxHierarchy_CheckedChanged(object sender, EventArgs e)
		{
			if (m_finSetup || m_curList == null)
				return;
			m_fhierarchyChanged = SupportsHierarchy != (m_curList.Depth > 1);
			CheckFlags();
		}

		protected override void m_chkBoxSortBy_CheckedChanged(object sender, EventArgs e)
		{
			if (m_finSetup || m_curList == null)
				return;
			m_fsortChanged = SortByName != m_curList.IsSorted;
			CheckFlags();
		}

		protected override void m_chkBoxDuplicate_CheckedChanged(object sender, EventArgs e)
		{
			if (m_finSetup || m_curList == null)
				return;
			m_fduplicateChanged = AllowDuplicate == m_curList.PreventDuplicates;
			CheckFlags();
		}

		protected override void m_wsCombo_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (m_finSetup || m_curList == null)
				return;
			m_fwsChanged = SelectedWs != m_curList.WsSelector;
			CheckFlags();
		}

		protected override void m_displayByCombo_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (m_finSetup || m_curList == null)
				return;
			m_fdisplayByChanged = ((int) DisplayBy) != m_curList.DisplayOption;
			CheckFlags();
		}

		private void CheckFlags()
		{
			m_fchangesMade = m_fnameChanged | m_fhierarchyChanged | m_fsortChanged | m_fduplicateChanged
							 | m_fwsChanged | m_fdisplayByChanged | m_fdescriptionChanged;
		}

		protected override void DoOKAction()
		{
			// LabeledMultiStringControls don't seem to have a valid TextChanged Event, so we have to simulate one.
			m_fnameChanged = HasListNameChanged(m_curList);
			m_fdescriptionChanged = HasDescriptionChanged(m_curList);
			CheckFlags();
			if (!m_fchangesMade)
				return; // Nothing to do!
			if (m_fnameChanged && IsListNameDuplicated)
			{
				MessageBox.Show(xWorksStrings.ksChooseAnotherListName, xWorksStrings.ksDuplicateName,
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			UndoableUnitOfWorkHelper.Do(xWorksStrings.ksUndoConfigureList, xWorksStrings.ksRedoConfigureList,
				Cache.ActionHandlerAccessor, () =>
				{
					var wsToUse = Cache.DefaultAnalWs;
					m_curList.WsSelector = SelectedWs;
					if (SelectedWs == WritingSystemServices.kwsVerns || SelectedWs == WritingSystemServices.kwsVernAnals)
						m_curList.IsVernacular = true;
					else
						m_curList.IsVernacular = false;
					SetAllMultiAlternatives(m_curList.Name, m_lmscListName);
					SetAllMultiAlternatives(m_curList.Description, m_lmscDescription);
					m_curList.DisplayOption = (int)DisplayBy;
					m_curList.IsSorted = SortByName;
					m_curList.PreventDuplicates = !AllowDuplicate;
					m_curList.Depth = SupportsHierarchy ? 127 : 1;
				});
				ResetAllFlags();
			base.DoOKAction(); // resets Lists area (to incorporate changes just made)
		}

		/// <summary>
		/// Searches existing CmPossibilityLists for a Name alternative that matches one in the
		/// dialog's listName MultiStringControl, but isn't in our 'being edited' list.
		/// </summary>
		protected override bool IsListNameDuplicated
		{
			get
			{
				var repo = Cache.ServiceLocator.GetInstance<ICmPossibilityListRepository>();
				var wsf = Cache.WritingSystemFactory;
				var cws = m_lmscListName.NumberOfWritingSystems;
				for (var i = 0; i < cws; i++)
				{
					var curWs = m_lmscListName.Ws(i);
					var emptyStr = Cache.TsStrFactory.EmptyString(curWs).Text;
					var lmscName = m_lmscListName.Value(curWs).Text;
					if (repo.AllInstances().Where(
						list => list != m_curList
							&& list.Name.get_String(curWs).Text != emptyStr
							&& list.Name.get_String(curWs).Text == lmscName).Any())
						return true;
				}
				return false;
			}
		}

		private bool HasDescriptionChanged(ICmMajorObject list)
		{
			var oldstrings = list.Description;
			return HasMsContentChanged(oldstrings, m_lmscDescription);
		}

		private bool HasListNameChanged(ICmMajorObject list)
		{
			var oldstrings = list.Name;
			return HasMsContentChanged(oldstrings, m_lmscListName);
		}

		private static bool HasMsContentChanged(IMultiAccessorBase oldStrings, LabeledMultiStringControl msControl)
		{
			var cws = msControl.NumberOfWritingSystems;
			for (var i = 0; i < cws; i++)
			{
				var curWs = msControl.Ws(i);
				//if (oldStrings.get_String(curWs).Text != Cache.TsStrFactory.EmptyString(curWs).Text
				//    && oldStrings.get_String(curWs).Text != msControl.Value(curWs).Text)
				//    return true;
				if (oldStrings.get_String(curWs).Text != msControl.Value(curWs).Text)
					return true;
			}
			return false;
		}
	}
}
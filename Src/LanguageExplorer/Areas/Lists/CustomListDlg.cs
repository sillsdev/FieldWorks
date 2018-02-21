// Copyright (c) 2010-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.Controls.DetailControls;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.Areas.Lists
{
	/// <summary>
	/// Dialog for adding TopicListEditor-like custom lists to a Fieldworks project.
	/// </summary>
	public partial class CustomListDlg : Form
	{
		protected string s_helpTopic = "khtpCustomLists";
		private HelpProvider m_helpProvider;
		protected readonly IPropertyTable m_propertyTable;
		protected readonly IPublisher m_publisher;
		protected LabeledMultiStringControl m_lmscListName;
		protected LabeledMultiStringControl m_lmscDescription;
		private IVwStylesheet m_stylesheet;
		protected List<CoreWritingSystemDefinition> m_uiWss;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:CustomListDlg"/> class.
		/// </summary>
		public CustomListDlg(IPropertyTable propertyTable, IPublisher publisher, LcmCache cache)
		{
			Guard.AgainstNull(propertyTable, nameof(propertyTable));
			Guard.AgainstNull(publisher, nameof(publisher));
			Guard.AgainstNull(cache, nameof(cache));

			InitializeComponent();
			StartPosition = FormStartPosition.CenterParent;
			m_propertyTable = propertyTable;
			m_publisher = publisher;
			Cache = cache;
			m_btnOK.Enabled = false;

			InitializeWSCombo();
			InitializeDisplayByCombo();
		}

		private void CustomListDlg_Load(object sender, EventArgs e)
		{
			if (m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider") != null)
			{
				InitializeHelpProvider();
			}
			m_stylesheet = FwUtils.StyleSheetFromPropertyTable(m_propertyTable);
			InitializeMultiStringControls();
			InitializeDialogFields();
			// Register "changed" event handlers after loading so they don't fire as we populate the dialog
			m_wsCombo.SelectedIndexChanged += m_wsCombo_SelectedIndexChanged;
			m_displayByCombo.SelectedIndexChanged += m_displayByCombo_SelectedIndexChanged;
			m_chkBoxHierarchy.CheckedChanged += m_chkBoxHierarchy_CheckedChanged;
			m_chkBoxSortBy.CheckedChanged += m_chkBoxSortBy_CheckedChanged;
			m_chkBoxDuplicate.CheckedChanged += m_chkBoxDuplicate_CheckedChanged;

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
		protected List<CoreWritingSystemDefinition> GetUiWritingSystemAndEnglish()
		{
			// Protected for testing
			Debug.Assert(Cache != null, "Can't install languages without a cache!");
			var wsMgr = Cache.ServiceLocator.WritingSystemManager;
			var userWs = wsMgr.UserWritingSystem;
			var result = new List<CoreWritingSystemDefinition> {userWs};
			if (userWs.Language.Code == "en")
			{
				return result;
			}

			// If English is not the DefaultUserWs, add it to the list.
			CoreWritingSystemDefinition engWs;
			if (wsMgr.TryGet("en", out engWs))
			{
				result.Add(engWs);
			}
			return result;
		}

		private LabeledMultiStringControl ReplaceTextBoxWithMultiStringBox(TextBox tb, IVwStylesheet stylesheet)
		{
			Debug.Assert(Cache != null, "Need a cache to setup a MultiStringBox.");
			tb.Hide();
			if (m_uiWss.Count == 0)
				return null;
			var ms = new LabeledMultiStringControl(Cache, m_uiWss, stylesheet)
			{
				Location = tb.Location,
				Width = tb.Width,
				Anchor = tb.Anchor,
				AccessibleName = tb.AccessibleName
			};

			// Grow the dialog and move all lower controls down to make room.
			Controls.Remove(tb);
			ms.TabIndex = tb.TabIndex;	// assume the same tab order as the 'designed' control
			Controls.Add(ms);
			FontHeightAdjuster.GrowDialogAndAdjustControls(this, ms.Height - tb.Height, ms);
			return ms;
		}

		protected static void LoadAllMultiAlternatives(IMultiAccessorBase multiField, LabeledMultiStringControl destination)
		{
			var cws = destination.NumberOfWritingSystems;
			for (var i = 0; i < cws; i++)
			{
				var curWs = destination.Ws(i);
				if (curWs <= 0)
				{
					continue;
				}
				int actualWs;
				ITsString tssStr;
				if (!multiField.TryWs(curWs, out actualWs, out tssStr))
				{
					continue;
				}
				destination.SetValue(curWs, tssStr);
			}
		}

		protected static void SetAllMultiAlternatives(IMultiAccessorBase multiField, LabeledMultiStringControl source)
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
			m_wsCombo.Items.Add(new IdAndString<int>(WritingSystemServices.kwsAnals, AreaResources.AllAnalysisWs));
			m_wsCombo.Items.Add(new IdAndString<int>(WritingSystemServices.kwsVerns, AreaResources.AllVernacularWs));
			m_wsCombo.Items.Add(new IdAndString<int>(WritingSystemServices.kwsAnalVerns, AreaResources.AllAnalysisVernacularWs));
			m_wsCombo.Items.Add(new IdAndString<int>(WritingSystemServices.kwsVernAnals, AreaResources.AllVernacularAnalysisWs));
			m_wsCombo.SelectedIndex = 0;
			m_wsCombo.LostFocus += m_wsCombo_LostFocus;
		}

		void m_wsCombo_LostFocus(object sender, EventArgs e)
		{
			var newIndex = m_wsCombo.FindString(m_wsCombo.Text);
			if (newIndex < 0)
			{
				newIndex = 0;
			}
			m_wsCombo.SelectedIndex = newIndex;
		}

		/// <summary>
		/// Initializes the "Display items in the list by" combo box and
		/// sets the selection to the 1st item.
		/// </summary>
		private void InitializeDisplayByCombo()
		{
			m_displayByCombo.Items.Add(new IdAndString<PossNameType>(PossNameType.kpntName, ListResources.ksName));
			m_displayByCombo.Items.Add(new IdAndString<PossNameType>(PossNameType.kpntNameAndAbbrev, ListResources.ksAbbrevName));
			m_displayByCombo.Items.Add(new IdAndString<PossNameType>(PossNameType.kpntAbbreviation, ListResources.ksAbbreviation));
			m_displayByCombo.SelectedIndex = 0;
			m_displayByCombo.LostFocus += m_displayByCombo_LostFocus;
		}

		private void m_displayByCombo_LostFocus(object sender, EventArgs e)
		{
			var newIndex = -1;
			// Can't use FindString() because it uses StartsWith instead of Equals in its
			// search.  See FWR-3436.
			for (var i = 0; i < m_displayByCombo.Items.Count; ++i)
			{
				if (m_displayByCombo.Items[i].ToString() == m_displayByCombo.Text)
				{
					newIndex = i;
					break;
				}
			}

			if (newIndex < 0)
			{
				newIndex = 0;
			}
			m_displayByCombo.SelectedIndex = newIndex;
		}

		/// <summary>
		/// Initializes the help provider for the Help... button.
		/// N.B. This method requires a mediator to function
		/// </summary>
		private void InitializeHelpProvider()
		{
			m_helpProvider = new HelpProvider
			{
				HelpNamespace = m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider").HelpFile
			};
			m_helpProvider.SetHelpKeyword(this, m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider").GetHelpString(s_helpTopic));
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
					if (!string.IsNullOrEmpty(m_lmscListName.Value(m_lmscListName.Ws(i)).Text))
					{
						return false;
					}
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
				var cws = m_lmscListName.NumberOfWritingSystems;
				for (var i = 0; i < cws; i++)
				{
					var curWs = m_lmscListName.Ws(i);
					var emptyStr = TsStringUtils.EmptyString(curWs).Text;
					var lmscName = m_lmscListName.Value(curWs).Text;
					if (repo.AllInstances().Any(list =>
						list.Name.get_String(curWs).Text != emptyStr && list.Name.get_String(curWs).Text == lmscName))
					{
						return true;
					}
				}
				return false;
			}
		}

		private void m_btnOK_Click(object sender, EventArgs e)
		{
			if (m_publisher == null)
			{
				return; // to save any changes requires a mediator and cache
			}
			if (IsListNameEmpty)
			{
				MessageBox.Show(ListResources.ksProvideValidListName, ListResources.ksNoListName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
		}

		/// <summary>
		/// LCM cache.
		/// </summary>
		protected LcmCache Cache { get; }

		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			if (m_publisher == null)
			{
				return;
			}
			ShowHelp.ShowHelpTopic(m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"), s_helpTopic);
		}

		#region Implementation of IDisposable

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			if (disposing)
			{
				components?.Dispose();
				m_helpProvider?.Dispose();
			}
			m_helpProvider = null;
			base.Dispose(disposing);
		}

		#endregion

		#region Public Properties

		/// <summary>
		/// Set the text in the "Name of List" text box for a particular ws.
		/// </summary>
		protected void SetListName(ITsString listName, int ws)
		{
			m_lmscListName.SetValue(ws, listName);
		}

		/// <summary>
		/// Get the text in the "Name of List" text box for a particular ws.
		/// </summary>
		protected ITsString GetListName(int ws)
		{
			return m_lmscListName.Value(ws);
		}

		/// <summary>
		/// Set the text in the "Description" text box for a particular ws.
		/// </summary>
		protected void SetDescription(ITsString description, int ws)
		{
			m_lmscDescription.SetValue(ws, description);
		}

		/// <summary>
		/// Get the text in the "Description" text box for a particular ws.
		/// </summary>
		protected ITsString GetDescription(int ws)
		{
			return m_lmscDescription.Value(ws);
		}

		/// <summary>
		/// Gets the selected WritingSystemServices Id from the writing system combo box.
		/// Can also set the currently selected item when editing properties of an existing list.
		/// </summary>
		public int SelectedWs
		{
			get { return ((IdAndString<int>)m_wsCombo.SelectedItem).Id; }
			set
			{
				for (var i = 0; i < m_wsCombo.Items.Count; i++)
				{
					if (((IdAndString<int>) m_wsCombo.Items[i]).Id != value)
					{
						continue;
					}
					m_wsCombo.SelectedIndex = i;
					break;
				}
			}
		}

		/// <summary>
		/// Gets the selected PossNameType from the "Display items in the list by" combo box.
		/// Can also set the currently selected item when editing properties of an existing list.
		/// </summary>
		public PossNameType DisplayBy
		{
			get { return ((IdAndString<PossNameType>)m_displayByCombo.SelectedItem).Id; }
			set { m_displayByCombo.SelectedIndex = (int)value; }
		}

		/// <summary>
		/// Gets whether the "Support hierarchy" checkbox is checked or not.
		/// Can also set an existing value when editing properties of an existing list.
		/// </summary>
		public bool SupportsHierarchy
		{
			get { return m_chkBoxHierarchy.Checked; }
			set { m_chkBoxHierarchy.Checked = value; }
		}

		/// <summary>
		/// Gets whether the "Sort items by name" checkbox is checked or not.
		/// Can also set an existing value when editing properties of an existing list.
		/// </summary>
		public bool SortByName
		{
			get { return m_chkBoxSortBy.Checked; }
			set { m_chkBoxSortBy.Checked = value; }
		}

		/// <summary>
		/// Gets whether the "Allow duplicate item names and abbrev." checkbox is checked or not.
		/// Can also set an existing value when editing properties of an existing list.
		/// </summary>
		public bool AllowDuplicate
		{
			get { return m_chkBoxDuplicate.Checked; }
			set { m_chkBoxDuplicate.Checked = value; }
		}

		#endregion

		#region Protected Changed Handlers

		protected virtual void m_wsCombo_SelectedIndexChanged(object sender, EventArgs e)
		{
			// editing subclass needs to know if this changed.
		}

		protected virtual void m_displayByCombo_SelectedIndexChanged(object sender, EventArgs e)
		{
			// editing subclass needs to know if this changed.
		}

		protected virtual void m_chkBoxHierarchy_CheckedChanged(object sender, EventArgs e)
		{
			// editing subclass needs to know if this changed.
		}

		protected virtual void m_chkBoxSortBy_CheckedChanged(object sender, EventArgs e)
		{
			// editing subclass needs to know if this changed.
		}

		protected virtual void m_chkBoxDuplicate_CheckedChanged(object sender, EventArgs e)
		{
			// editing subclass needs to know if this changed.
		}

		#endregion
	}
}
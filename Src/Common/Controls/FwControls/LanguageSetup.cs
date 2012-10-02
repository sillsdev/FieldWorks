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
// File: LanguageSetup.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Resources;
using System.Diagnostics;

using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for LanguageSetup.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class LanguageSetup : UserControl, IFWDisposable
	{
		#region Public Events
		/// <summary>Fired when an EventHandler event exists.</summary>
		[Category("Custom")]
		[Description("Occurs when the language name has changed.")]
		public event EventHandler LanguageNameChanged;
		#endregion

		#region Designer Added Member Variables

		private System.ComponentModel.IContainer components;
		private System.Windows.Forms.TextBox txtCurrentLangName;
		private System.Windows.Forms.Label lblCurrentEthCodeValue;

		/// <summary></summary>
		protected FwOverrideComboBox cboLookup;
		/// <summary></summary>
		protected System.Windows.Forms.Button btnFind;
		/// <summary></summary>
		protected System.Windows.Forms.TextBox txtFindPattern;
		/// <summary></summary>
		protected System.Windows.Forms.Label lblOtherNamesList;
		/// <summary></summary>
		protected System.Windows.Forms.ListView lvFindResult;
		private ToolTip toolTip1;
		#endregion

		#region Member Data
		private string m_initialTarget; // If set, performs initial search for this language.

		/// <summary>
		/// Overriding testing instances of this class should set
		/// this so the DB connection doesn't get created.
		/// </summary>
		protected bool m_testing;
		/// <summary></summary>
		protected string m_lastSearchText;
		/// <summary></summary>
		protected TrapEnterFilter m_messageFilter;
		/// <summary>
		/// This flag is used to keep track of how this control is called.  If it's from
		/// a 'modify' state, then allow the Ethnoluge name to change with out selecting
		/// information from controls on the dlg.
		/// </summary>
		private bool m_EnteredAsModify;

		// Database column numbers for these fields.
		private const int kLangNameCol = 1;
		private const int kCountryCol = 3;
		private const int kEthnoCodeCol = 5;

		// Sort fields
		private const string kLangNameSort = " order by 2, 4";
		private const string kCountrySort = " order by 4, 2";
		private const string kEthnoCodeSort = "order by 6, 2, 4";

		private const string kSqlFindLangNames =
			"select * from dbo.fnGetLanguageNamesLike('{0}', NULL) {1}";
		private const string kSqlFindLangsInCountry =
			"select * from dbo.fnGetLanguagesInCountry('{0}', NULL) {1}";
		private const string kSqlFindLangsForEthnoCode =
			"select * from dbo.fnGetLanguagesForIso('{0}') {1}";
		private const string kSqlGetOtherNames =
			"select * from dbo.fnGetOtherLanguageNames('{0}') " +
			"order by IsPrimaryName desc, LangName";

		private SqlConnection m_dbConnection;
		private SqlCommand m_sqlCommand;
		private string m_currentSortBy = kLangNameSort;

		// These functions should be in this string array in the order they match
		// their corresponding items in the search by combo. box.
		private string[] m_dbFunctions = new string[] {kSqlFindLangNames,
														  kSqlFindLangsInCountry, kSqlFindLangsForEthnoCode};

		#endregion

		#region Construction and Destruction
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="LanguageSetup"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public LanguageSetup()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// Set the default search by to language name.
			cboLookup.SelectedIndex = 0;

			// Save the text in these 2 labels in case we need to restore it sometime
			// during the user's interaction with this control.
			lblOtherNamesList.Tag = lblOtherNamesList.Text;
			lblCurrentEthCodeValue.Tag = lblCurrentEthCodeValue.Text;

			lblOtherNamesList.Text = "";
			lblCurrentEthCodeValue.Text = "";

			// This is neede before we can set the inital EthCode.
			if (!m_testing)
			{
				string sConnection = "Server=" + MiscUtils.LocalServerName +
					"; Database=Ethnologue; User ID=FWDeveloper; " +
					"Password=careful; Pooling=false;";

				m_dbConnection = new SqlConnection(sConnection);
				m_dbConnection.Open();
				m_sqlCommand = m_dbConnection.CreateCommand();
			}

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
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

			if (disposing)
			{
				if (components != null)
					components.Dispose();

				if (m_sqlCommand != null)
					m_sqlCommand.Dispose();
				if (m_dbConnection != null)
					m_dbConnection.Dispose();

				if (m_messageFilter != null)
					Application.RemoveMessageFilter(m_messageFilter);
			}

			m_messageFilter = null;
			m_sqlCommand = null;
			m_dbConnection = null;
			components = null;

			base.Dispose( disposing );
		}
		#endregion

		#region Component Designer generated code
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.Windows.Forms.Label lblLookup;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LanguageSetup));
			System.Windows.Forms.Label lblFindPattern;
			System.Windows.Forms.ColumnHeader hdrLanguageName;
			System.Windows.Forms.ColumnHeader hdrCountry;
			System.Windows.Forms.ColumnHeader hdrEthnologueCode;
			System.Windows.Forms.GroupBox grpCurrentLang;
			System.Windows.Forms.Label lblCurrentEthCode;
			System.Windows.Forms.Label lblCurrentLangName;
			System.Windows.Forms.Label lblOtherNames;
			System.Windows.Forms.Label lblInstructions1;
			System.Windows.Forms.Label lblInstructions2;
			System.Windows.Forms.HelpProvider helpProvider1;
			this.txtFindPattern = new System.Windows.Forms.TextBox();
			this.btnFind = new System.Windows.Forms.Button();
			this.lvFindResult = new System.Windows.Forms.ListView();
			this.lblCurrentEthCodeValue = new System.Windows.Forms.Label();
			this.txtCurrentLangName = new System.Windows.Forms.TextBox();
			this.lblOtherNamesList = new System.Windows.Forms.Label();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.cboLookup = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			lblLookup = new System.Windows.Forms.Label();
			lblFindPattern = new System.Windows.Forms.Label();
			hdrLanguageName = new System.Windows.Forms.ColumnHeader();
			hdrCountry = new System.Windows.Forms.ColumnHeader();
			hdrEthnologueCode = new System.Windows.Forms.ColumnHeader();
			grpCurrentLang = new System.Windows.Forms.GroupBox();
			lblCurrentEthCode = new System.Windows.Forms.Label();
			lblCurrentLangName = new System.Windows.Forms.Label();
			lblOtherNames = new System.Windows.Forms.Label();
			lblInstructions1 = new System.Windows.Forms.Label();
			lblInstructions2 = new System.Windows.Forms.Label();
			helpProvider1 = new System.Windows.Forms.HelpProvider();
			grpCurrentLang.SuspendLayout();
			this.SuspendLayout();
			//
			// lblLookup
			//
			resources.ApplyResources(lblLookup, "lblLookup");
			lblLookup.Name = "lblLookup";
			helpProvider1.SetShowHelp(lblLookup, ((bool)(resources.GetObject("lblLookup.ShowHelp"))));
			//
			// txtFindPattern
			//
			this.txtFindPattern.AcceptsReturn = true;
			resources.ApplyResources(this.txtFindPattern, "txtFindPattern");
			helpProvider1.SetHelpString(this.txtFindPattern, resources.GetString("txtFindPattern.HelpString"));
			this.txtFindPattern.Name = "txtFindPattern";
			helpProvider1.SetShowHelp(this.txtFindPattern, ((bool)(resources.GetObject("txtFindPattern.ShowHelp"))));
			this.txtFindPattern.Enter += new System.EventHandler(this.txtFindPattern_Enter);
			this.txtFindPattern.Leave += new System.EventHandler(this.txtFindPattern_Leave);
			this.txtFindPattern.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtFindPattern_KeyPress);
			this.txtFindPattern.TextChanged += new System.EventHandler(this.txtFindPattern_TextChanged);
			//
			// btnFind
			//
			resources.ApplyResources(this.btnFind, "btnFind");
			helpProvider1.SetHelpString(this.btnFind, resources.GetString("btnFind.HelpString"));
			this.btnFind.Name = "btnFind";
			helpProvider1.SetShowHelp(this.btnFind, ((bool)(resources.GetObject("btnFind.ShowHelp"))));
			this.btnFind.Click += new System.EventHandler(this.btnFind_Click);
			//
			// lblFindPattern
			//
			resources.ApplyResources(lblFindPattern, "lblFindPattern");
			lblFindPattern.Name = "lblFindPattern";
			helpProvider1.SetShowHelp(lblFindPattern, ((bool)(resources.GetObject("lblFindPattern.ShowHelp"))));
			//
			// lvFindResult
			//
			resources.ApplyResources(this.lvFindResult, "lvFindResult");
			this.lvFindResult.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
			hdrLanguageName,
			hdrCountry,
			hdrEthnologueCode});
			this.lvFindResult.FullRowSelect = true;
			helpProvider1.SetHelpString(this.lvFindResult, resources.GetString("lvFindResult.HelpString"));
			this.lvFindResult.HideSelection = false;
			this.lvFindResult.MultiSelect = false;
			this.lvFindResult.Name = "lvFindResult";
			helpProvider1.SetShowHelp(this.lvFindResult, ((bool)(resources.GetObject("lvFindResult.ShowHelp"))));
			this.lvFindResult.UseCompatibleStateImageBehavior = false;
			this.lvFindResult.View = System.Windows.Forms.View.Details;
			this.lvFindResult.Enter += new System.EventHandler(this.lvFindResult_Enter);
			this.lvFindResult.SelectedIndexChanged += new System.EventHandler(this.lvFindResult_SelectedIndexChanged);
			this.lvFindResult.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.lvFindResult_ColumnClick);
			//
			// hdrLanguageName
			//
			resources.ApplyResources(hdrLanguageName, "hdrLanguageName");
			//
			// hdrCountry
			//
			resources.ApplyResources(hdrCountry, "hdrCountry");
			//
			// hdrEthnologueCode
			//
			resources.ApplyResources(hdrEthnologueCode, "hdrEthnologueCode");
			//
			// grpCurrentLang
			//
			grpCurrentLang.Controls.Add(lblCurrentEthCode);
			grpCurrentLang.Controls.Add(this.lblCurrentEthCodeValue);
			grpCurrentLang.Controls.Add(lblCurrentLangName);
			grpCurrentLang.Controls.Add(this.txtCurrentLangName);
			grpCurrentLang.Controls.Add(lblOtherNames);
			grpCurrentLang.Controls.Add(this.lblOtherNamesList);
			grpCurrentLang.FlatStyle = System.Windows.Forms.FlatStyle.System;
			resources.ApplyResources(grpCurrentLang, "grpCurrentLang");
			grpCurrentLang.Name = "grpCurrentLang";
			helpProvider1.SetShowHelp(grpCurrentLang, ((bool)(resources.GetObject("grpCurrentLang.ShowHelp"))));
			grpCurrentLang.TabStop = false;
			//
			// lblCurrentEthCode
			//
			resources.ApplyResources(lblCurrentEthCode, "lblCurrentEthCode");
			lblCurrentEthCode.BackColor = System.Drawing.SystemColors.Control;
			lblCurrentEthCode.Name = "lblCurrentEthCode";
			helpProvider1.SetShowHelp(lblCurrentEthCode, ((bool)(resources.GetObject("lblCurrentEthCode.ShowHelp"))));
			//
			// lblCurrentEthCodeValue
			//
			resources.ApplyResources(this.lblCurrentEthCodeValue, "lblCurrentEthCodeValue");
			helpProvider1.SetHelpString(this.lblCurrentEthCodeValue, resources.GetString("lblCurrentEthCodeValue.HelpString"));
			this.lblCurrentEthCodeValue.Name = "lblCurrentEthCodeValue";
			helpProvider1.SetShowHelp(this.lblCurrentEthCodeValue, ((bool)(resources.GetObject("lblCurrentEthCodeValue.ShowHelp"))));
			this.lblCurrentEthCodeValue.Tag = "(Unknown)";
			//
			// lblCurrentLangName
			//
			resources.ApplyResources(lblCurrentLangName, "lblCurrentLangName");
			lblCurrentLangName.Name = "lblCurrentLangName";
			helpProvider1.SetShowHelp(lblCurrentLangName, ((bool)(resources.GetObject("lblCurrentLangName.ShowHelp"))));
			//
			// txtCurrentLangName
			//
			helpProvider1.SetHelpString(this.txtCurrentLangName, resources.GetString("txtCurrentLangName.HelpString"));
			resources.ApplyResources(this.txtCurrentLangName, "txtCurrentLangName");
			this.txtCurrentLangName.Name = "txtCurrentLangName";
			helpProvider1.SetShowHelp(this.txtCurrentLangName, ((bool)(resources.GetObject("txtCurrentLangName.ShowHelp"))));
			this.txtCurrentLangName.TextChanged += new System.EventHandler(this.txtCurrentLangName_TextChanged);
			//
			// lblOtherNames
			//
			resources.ApplyResources(lblOtherNames, "lblOtherNames");
			lblOtherNames.Name = "lblOtherNames";
			helpProvider1.SetShowHelp(lblOtherNames, ((bool)(resources.GetObject("lblOtherNames.ShowHelp"))));
			//
			// lblOtherNamesList
			//
			resources.ApplyResources(this.lblOtherNamesList, "lblOtherNamesList");
			helpProvider1.SetHelpString(this.lblOtherNamesList, resources.GetString("lblOtherNamesList.HelpString"));
			this.lblOtherNamesList.Name = "lblOtherNamesList";
			helpProvider1.SetShowHelp(this.lblOtherNamesList, ((bool)(resources.GetObject("lblOtherNamesList.ShowHelp"))));
			this.lblOtherNamesList.UseMnemonic = false;
			this.lblOtherNamesList.Paint += new System.Windows.Forms.PaintEventHandler(this.lblOtherNamesList_Paint);
			//
			// lblInstructions1
			//
			resources.ApplyResources(lblInstructions1, "lblInstructions1");
			lblInstructions1.Name = "lblInstructions1";
			helpProvider1.SetShowHelp(lblInstructions1, ((bool)(resources.GetObject("lblInstructions1.ShowHelp"))));
			//
			// lblInstructions2
			//
			resources.ApplyResources(lblInstructions2, "lblInstructions2");
			lblInstructions2.Name = "lblInstructions2";
			helpProvider1.SetShowHelp(lblInstructions2, ((bool)(resources.GetObject("lblInstructions2.ShowHelp"))));
			//
			// cboLookup
			//
			this.cboLookup.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			helpProvider1.SetHelpString(this.cboLookup, resources.GetString("cboLookup.HelpString"));
			resources.ApplyResources(this.cboLookup, "cboLookup");
			this.cboLookup.Items.AddRange(new object[] {
			resources.GetString("cboLookup.Items"),
			resources.GetString("cboLookup.Items1"),
			resources.GetString("cboLookup.Items2")});
			this.cboLookup.Name = "cboLookup";
			helpProvider1.SetShowHelp(this.cboLookup, ((bool)(resources.GetObject("cboLookup.ShowHelp"))));
			this.cboLookup.Enter += new System.EventHandler(this.cboLookup_Enter);
			this.cboLookup.SelectedIndexChanged += new System.EventHandler(this.cboLookup_SelectedIndexChanged);
			//
			// LanguageSetup
			//
			this.Controls.Add(this.btnFind);
			this.Controls.Add(this.txtFindPattern);
			this.Controls.Add(this.cboLookup);
			this.Controls.Add(lblInstructions2);
			this.Controls.Add(lblInstructions1);
			this.Controls.Add(grpCurrentLang);
			this.Controls.Add(this.lvFindResult);
			this.Controls.Add(lblFindPattern);
			this.Controls.Add(lblLookup);
			this.Name = "LanguageSetup";
			helpProvider1.SetShowHelp(this, ((bool)(resources.GetObject("$this.ShowHelp"))));
			resources.ApplyResources(this, "$this");
			grpCurrentLang.ResumeLayout(false);
			grpCurrentLang.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		#region Properties

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the 'modify' start state property.  This is used for Ethnoluge
		/// name modification.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool StartedInModifyState
		{
			get
			{
				CheckDisposed();

				return m_EnteredAsModify;
			}
			set
			{
				CheckDisposed();

				m_EnteredAsModify = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the current language name.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string LanguageName
		{
			get
			{
				CheckDisposed();

				return txtCurrentLangName.Text.Trim();
			}
			set
			{
				CheckDisposed();

				txtCurrentLangName.Text = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the current ethnologue code.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string EthnologueCode
		{
			get
			{
				CheckDisposed();

				if (LanguageNotFound || // be paranoid and test for "(none)" as well.
					lblCurrentEthCodeValue.Text == (string)lblCurrentEthCodeValue.Tag)
					return "";
				else
					return lblCurrentEthCodeValue.Text;
			}
			set
			{
				CheckDisposed();

				if (value != null)
					value = value.Trim();

				if (value == null || value == string.Empty)
				{
					// Restore the current ethnologue code with "Unknown" (or other
					// localized version thereof) and clear the other names field.
					lblCurrentEthCodeValue.Text = (string)lblCurrentEthCodeValue.Tag;
					lblOtherNamesList.Text = string.Empty;
					toolTip1.SetToolTip(lblOtherNamesList, string.Empty);
				}
				else
				{
					// Set the ethnologue field and get the other language names from
					// the DB.
					lblCurrentEthCodeValue.Text = value;
					LoadOtherNames();
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the value of the "Language Not Found" check box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool LanguageNotFound
		{
			get
			{
				CheckDisposed();

				// The last item is always "None of the above" unless the list is (otherwise) empty.
				if (StartedInModifyState)
					return lblCurrentEthCodeValue.Text == (string)lblCurrentEthCodeValue.Tag;
				else if (lvFindResult.SelectedIndices.Count == 0)
					return true;
				else if (lvFindResult.SelectedIndices[0] == lvFindResult.Items.Count - 1)
					return true;
				else
					return false;
			}
		}
		#endregion

		#region Overrides
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnHandleDestroyed(EventArgs e)
		{
			base.OnHandleDestroyed(e);

			if (m_dbConnection != null)
				m_dbConnection.Close();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Do the initial search when we bring up the control
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad (e);
			DoInitialSearch();
		}
		#endregion

		#region Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is called just before ShowDialog. It sets up the target string as a language
		/// name and performs an initial search for this language as the dialog comes up.
		/// </summary>
		/// <param name="target">The target.</param>
		/// ------------------------------------------------------------------------------------
		public void PerformInitialSearch(string target)
		{
			CheckDisposed();

			m_initialTarget = target;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal void StartFind()
		{
			CheckDisposed();

			btnFind_Click(null, null);
		}
		#endregion

		#region Event Delegates
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void lvFindResult_ColumnClick(object sender, ColumnClickEventArgs e)
		{
			if (lvFindResult.Items.Count == 0)
				return;

			// Figure out what the new sort order should be based on the column clicked on.
			string newSortBy = string.Empty;
			switch (e.Column)
			{
				case 0: newSortBy = kLangNameSort; break;
				case 1: newSortBy = kCountrySort; break;
				case 2: newSortBy = kEthnoCodeSort; break;
			}

			// Don't bother reloading the data if the sort order didn't change.
			if (m_currentSortBy == newSortBy)
				return;

			m_currentSortBy = newSortBy;

			// Save the currently selected item.
			string langName = string.Empty;
			string country = string.Empty;
			string ethnoCode = string.Empty;
			if (lvFindResult.SelectedItems.Count > 0)
			{
				langName = lvFindResult.SelectedItems[0].Text;
				country = lvFindResult.SelectedItems[0].SubItems[1].Text;
				ethnoCode = lvFindResult.SelectedItems[0].SubItems[2].Text;
			}
			LoadList(m_lastSearchText);

			if (langName == string.Empty)
				return;

			// Restore the item that was selected before the sort.
			foreach (ListViewItem lvi in lvFindResult.Items)
			{
				if (lvi.Text == langName && lvi.SubItems[1].Text == country &&
					lvi.SubItems[2].Text == ethnoCode)
				{
					lvFindResult.EnsureVisible(lvi.Index);
					lvi.Selected = true;
					break;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void txtCurrentLangName_TextChanged(object sender, System.EventArgs e)
		{
			if (LanguageNotFound)
			{
				// We saved in the 'tag' fields the original text that should be something
				// like "(unknown)" and "(none)".
				lblCurrentEthCodeValue.Text = (string)lblCurrentEthCodeValue.Tag;
				lblOtherNamesList.Text = (string)lblOtherNamesList.Tag;
			}
			if (LanguageNameChanged != null)
				LanguageNameChanged(this, new EventArgs());
		}

		/// <summary>
		/// Fix the settings for either a search failure or clicking on the "None of the above"
		/// item.
		/// </summary>
		protected void SettingsForNoLanguage()
		{
			if (cboLookup.SelectedIndex == 0)
			{
				txtCurrentLangName.Text = txtFindPattern.Text.Trim();
			}
			else
			{
				txtCurrentLangName.Text = "";
				// And shift the focus to the Name edit box.
				txtCurrentLangName.Focus();
			}
			lblCurrentEthCodeValue.Text = (string)lblCurrentEthCodeValue.Tag;
			lblOtherNamesList.Text = (string)lblOtherNamesList.Tag;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected void lvFindResult_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (lvFindResult.SelectedIndices.Count > 0 &&
				lvFindResult.SelectedItems[0] != null &&
				lvFindResult.SelectedIndices[0] != lvFindResult.Items.Count - 1)
			{
				LanguageName = lvFindResult.SelectedItems[0].Text;
				EthnologueCode = lvFindResult.SelectedItems[0].SubItems[2].Text;
			}
			else if (lvFindResult.SelectedIndices.Count == 0 ||
				lvFindResult.SelectedIndices[0] == lvFindResult.Items.Count - 1)
			{
				SettingsForNoLanguage();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Restarts the timer everytime the text box text changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void txtFindPattern_TextChanged(object sender, System.EventArgs e)
		{
			btnFind.Enabled = (txtFindPattern.Text.Trim() != string.Empty);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void txtFindPattern_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
		{
			if (e.KeyChar == 0x0D && txtFindPattern.Text.Trim() != string.Empty)
				StartFind();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void txtFindPattern_Enter(object sender, System.EventArgs e)
		{
			m_messageFilter = new TrapEnterFilter(this);
			Application.AddMessageFilter(m_messageFilter);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void txtFindPattern_Leave(object sender, System.EventArgs e)
		{
			Application.RemoveMessageFilter(m_messageFilter);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When we get here, it means there was a long enough pause while typing in the search
		/// for text box that a search should be kicked off.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected void btnFind_Click(object sender, System.EventArgs e)
		{
			string searchFor = txtFindPattern.Text.Trim();

			// Don't bother with the DB search if the search text is the same as the
			// previous text or if it's empty.
			if (m_lastSearchText != searchFor && searchFor != string.Empty)
			{
				m_currentSortBy = kLangNameSort;	// order by Name, Country.
				m_lastSearchText = searchFor;
				if(cboLookup.SelectedIndex == 0 && LanguageName == string.Empty)
					LanguageName = searchFor;
				LoadList(searchFor);
			}
		}

		/// <summary>
		/// Do the initial search requested if m_initialTarget has been set.
		/// </summary>
		protected void DoInitialSearch()
		{
			if (m_initialTarget == null)
				return;
			txtFindPattern.Text = m_initialTarget;
			LoadList(m_initialTarget, false);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void cboLookup_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			m_lastSearchText = string.Empty;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// We want to have the focus initially in txtFindPattern. We do it here and then
		/// remove the event handler. We can't change the Tab order because then Shift-Tab
		/// goes to the Help button instead of "Search for" field (TE-753)
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void cboLookup_Enter(object sender, System.EventArgs e)
		{
			txtFindPattern.Focus();
			cboLookup.Enter -= new System.EventHandler(cboLookup_Enter);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void lblOtherNamesList_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
		{
			e.Graphics.FillRectangle(SystemBrushes.Control, lblOtherNamesList.ClientRectangle);

			StringFormat sf = new StringFormat(StringFormat.GenericTypographic);
			sf.Trimming = StringTrimming.EllipsisWord;

			e.Graphics.DrawString(lblOtherNamesList.Text, lblOtherNamesList.Font,
				SystemBrushes.ControlText, lblOtherNamesList.ClientRectangle, sf);
		}

		#endregion

		#region List loader methods
		/// <summary>
		/// Select the first exact match in the language column, or if none found, select the
		/// first item.
		/// </summary>
		protected virtual void SelectClosestMatch()
		{
			if (cboLookup.SelectedIndex == 0)
			{
				// Linear search for the first exact (but case-insensitive) match in the first
				// column (Language).
				string searchFor = txtFindPattern.Text.Trim().ToLower();
				for (int i = 0; i < lvFindResult.Items.Count; ++i)
				{
					if (lvFindResult.Items[i].SubItems[0].Text.ToLower() == searchFor)
					{
						lvFindResult.Items[i].Selected = true;
						lvFindResult.Items[i].EnsureVisible();
						return;
					}
				}
			}
			lvFindResult.Items[0].Selected = true;
			lvFindResult.Items[0].EnsureVisible();
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="searchText"></param>
		/// ------------------------------------------------------------------------------------
		protected virtual void LoadList(string searchText)
		{
			LoadList(searchText, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fill the controls based on the current search, and (if requested) display an error
		/// message if not found.
		/// </summary>
		/// <param name="searchText"></param>
		/// <param name="fShowErrorMessage"></param>
		/// ------------------------------------------------------------------------------------
		protected virtual void LoadList(string searchText, bool fShowErrorMessage)
		{
			lvFindResult.Items.Clear();

			// Don't bother loading the list view if there's nothing to search for.
			if (searchText == null || searchText.Trim() == string.Empty)
				return;

			using (new WaitCursor(this))
			{
				// Get the results back from the DB.
				SqlDataReader dbReader = GetSearchResultSet(searchText);
				if (dbReader != null)
				{
					while (dbReader.Read())
					{
						try
						{
							ListViewItem lvi = new ListViewItem(new string[] {
																				 dbReader.GetString(kLangNameCol),
																				 dbReader.GetString(kCountryCol),
																				 dbReader.GetString(kEthnoCodeCol)});

							lvFindResult.Items.Add(lvi);
						}
						catch
						{
						}
					}

					dbReader.Close();
				}
				try
				{
					ResourceManager resources =
						new ResourceManager("SIL.FieldWorks.Common.Controls.FwControls",
						System.Reflection.Assembly.GetExecutingAssembly());
					if (lvFindResult.Items.Count > 0)
					{
						// Select the most promising item in the list.
						SelectClosestMatch();
						LanguageName = lvFindResult.SelectedItems[0].Text;
						EthnologueCode = lvFindResult.SelectedItems[0].SubItems[2].Text;
						ListViewItem lvi = new ListViewItem(new string[]
						{ resources.GetString("kstidLangNoneAbove"), "", "" } );
						lvFindResult.Items.Add(lvi);
					}
					else
					{
						// Enter a language name searched for as a default into the language name box.
						SettingsForNoLanguage();
						// And give an explanatory message, if wanted.
						if (fShowErrorMessage)
						{
							string msg = string.Format(resources.GetString("kstidLangNotFound"),
								Environment.NewLine);
							string caption = resources.GetString("kstidLangNotFoundCaption");
							MessageBox.Show(this, msg, caption);
						}
					}
				}
				catch
				{
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///	 This routine replaces known SQL special characters with escaped characters.
		///	 Currently this is only the apos (').
		/// </summary>
		/// <param name="text"></param>
		/// <returns>string</returns>
		/// ------------------------------------------------------------------------------------
		private string SQLSafeString(string text)
		{
			string safeData = text;
			int startPos = 0;
			int foundPos = 0;
			while (foundPos >= 0)
			{
				foundPos = safeData.IndexOf("'", startPos);
				if (foundPos >= 0)
				{
					safeData = safeData.Insert(foundPos, "'");
					startPos = foundPos + 2;	// skip found and one added
				}
			}
			return safeData;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="searchText"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private SqlDataReader GetSearchResultSet(string searchText)
		{
			// If the combo box doesn't specify what type of search the user wants, then
			// don't bother trying.
			if (cboLookup.SelectedIndex < 0)
				return null;

			// The search by combo. box determines what DB function we call.
			string dbFunction = m_dbFunctions[cboLookup.SelectedIndex];

			m_sqlCommand.CommandText = string.Format(dbFunction,
				SQLSafeString(searchText), m_currentSortBy);

			// Wrapping this with a try block just incase it's possible to have searchText
			//	that isn't valid.  IE, causes an exception in the ExecuteReader code.
			SqlDataReader reader = null;
			try
			{
				reader = m_sqlCommand.ExecuteReader(System.Data.CommandBehavior.Default);
			}
			catch{}

			return reader;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void LoadOtherNames()
		{
			// Don't load other names if we don't have a valid Ethnologue code.
			if (lblCurrentEthCodeValue.Text == string.Empty ||
				lblCurrentEthCodeValue.Text == (string)lblCurrentEthCodeValue.Tag)
			{
				return;
			}

			lblOtherNamesList.Text = string.Empty;
			toolTip1.SetToolTip(lblOtherNamesList, string.Empty);

			m_sqlCommand.CommandText = string.Format(kSqlGetOtherNames,
				lblCurrentEthCodeValue.Text);

			// Get the names from the DB.
			SqlDataReader reader =
				m_sqlCommand.ExecuteReader(System.Data.CommandBehavior.Default);

			// Don't bother doing anything if no rows were returned.
			if (!reader.HasRows)
			{
				reader.Close();
				return;
			}

			System.Text.StringBuilder names = new System.Text.StringBuilder(string.Empty);
			int langCol = reader.GetOrdinal("LangName");

			while (reader.Read())
			{
				try
				{
					string langName = reader.GetString(langCol);

					if (langName != LanguageName)
						names.Append(langName + ", ");
				}
				catch
				{
				}
			}

			reader.Close();

			// Put the names in the other name's field, removing the last comma and space.
			lblOtherNamesList.Text = (names.Length > 0 ?
				names.ToString(0, names.Length - 2) : (string)lblOtherNamesList.Tag);
			toolTip1.SetToolTip(lblOtherNamesList, lblOtherNamesList.Text);
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When we enter the list view we have to set the focused item to match the selected
		/// item.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void lvFindResult_Enter(object sender, System.EventArgs e)
		{
			if (lvFindResult.SelectedItems.Count > 0)
				lvFindResult.SelectedItems[0].Focused = true;
		}

		#region Message filter for handling Enter key
		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Message filter for handling Enter key
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		protected internal class TrapEnterFilter : IMessageFilter
		{
			private LanguageSetup m_parent;

			/// ------------------------------------------------------------------------------------
			/// <summary>
			///
			/// </summary>
			/// <param name="parent"></param>
			/// ------------------------------------------------------------------------------------
			public TrapEnterFilter(LanguageSetup parent)
			{
				this.m_parent = parent;
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			///
			/// </summary>
			/// <param name="m"></param>
			/// <returns></returns>
			/// ------------------------------------------------------------------------------------
			public bool PreFilterMessage(ref Message m)
			{
				// Blocks all the messages relating to the left mouse button.
				if (m.Msg == (int)Win32.WinMsgs.WM_KEYDOWN)
				{
					if (((Keys)(int)m.WParam) == Keys.Enter && m_parent != null)
					{
						m_parent.StartFind();
						return true;
					}
				}
				return false;
			}
		}
		#endregion
	}
}

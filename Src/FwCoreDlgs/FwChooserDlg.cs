// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FwChooserDlg.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Controls;
using SIL.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.Common.COMInterfaces;
using XCore;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for FwChooserDlg.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FwChooserDlg : Form, IFWDisposable, ISettings, ICmPossibilitySupplier
	{
		#region Data members
		/// <summary>The possibility list used to populate the tree</summary>
		protected ICmPossibilityList m_list;
		/// <summary></summary>
		protected FdoCache m_cache;
		/// <summary></summary>
		protected IProjectSpecificSettingsKeyProvider m_projSettingsKey;

		/// <summary>The sequence to which selected list items should be added</summary>
		protected List<int> m_initiallySelectedHvos = new List<int>();

		// Help info
		/// <summary>The Help Topic provider</summary>
		protected IHelpTopicProvider m_helptopicProvider;
		string m_helpTopicKey;

		/// <summary></summary>
		protected Button btnOk;
		/// <summary></summary>
		protected Button btnCancel;
		/// <summary></summary>
		protected Button btnHelp;
		/// <summary></summary>
		protected ChooserTreeView tvPossibilities;
		/// <summary></summary>
		private Persistence m_persistence;
		/// <summary></summary>
		protected Label lblInfo;
		private System.ComponentModel.IContainer components;

		#endregion

		#region Constructor/Destructor/Initialization
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="FwChooserDlg"/> class when opened in
		/// Designer in VS.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private FwChooserDlg()
		{
			InitializeComponent();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="FwChooserDlg"/> class.
		/// </summary>
		/// <param name="list">The possibility list used to populate the tree</param>
		/// <param name="initiallySelectedHvos">The sequence of HVOs of initially selected
		/// possibilities</param>
		/// <param name="helptopicProvider">object that knows how to serve up help topics</param>
		/// <param name="sHelpTopicKey">Topic to display if user clicks Help button (can be
		/// specific to the possibility list being displayed)</param>
		/// <param name="projSettingsKey">The project settings key.</param>
		/// ------------------------------------------------------------------------------------
		public FwChooserDlg(ICmPossibilityList list, int[] initiallySelectedHvos,
			IHelpTopicProvider helptopicProvider, string sHelpTopicKey,
			IProjectSpecificSettingsKeyProvider projSettingsKey) : this()
		{
			if (initiallySelectedHvos == null)
				throw new ArgumentNullException("initiallySelectedHvos");
			if (list == null)
				throw new ArgumentNullException("list");
			m_list = list;
			m_cache = m_list.Cache;
			m_initiallySelectedHvos = new List<int>(initiallySelectedHvos);
			m_helptopicProvider = helptopicProvider;
			m_helpTopicKey = sHelpTopicKey;
			m_projSettingsKey = projSettingsKey;

			SetTitle();
			tvPossibilities.Load(m_list, m_initiallySelectedHvos, SelectedPossibilitiesLabel);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the label control in which the tree control will display the names of the
		/// checked possibilities.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual Label SelectedPossibilitiesLabel
		{
			get { return lblInfo; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Key to topic to display if user clicks Help button (can be specific to the
		/// possibility list being displayed)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string HelpTopicKey
		{
			set
			{
				CheckDisposed();
				m_helpTopicKey = value;
			}
		}

		#endregion

		#region Disposal
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged
		/// resources; <c>false</c> to release only unmanaged resources.
		/// </param>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (disposing)
			{
				if (components != null)
					components.Dispose();

				if (m_persistence != null)
					m_persistence.Dispose();
			}

			m_persistence = null;
			base.Dispose(disposing);
		}

		#endregion

		#region Windows Form Designer generated code
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FwChooserDlg));
			this.tvPossibilities = new SIL.FieldWorks.Common.Controls.ChooserTreeView();
			this.btnOk = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.btnHelp = new System.Windows.Forms.Button();
			this.m_persistence = new SIL.FieldWorks.Common.Controls.Persistence(this.components);
			this.lblInfo = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.m_persistence)).BeginInit();
			this.SuspendLayout();
			//
			// tvPossibilities
			//
			resources.ApplyResources(this.tvPossibilities, "tvPossibilities");
			this.tvPossibilities.ItemHeight = 16;
			this.tvPossibilities.Name = "tvPossibilities";
			//
			// btnOk
			//
			resources.ApplyResources(this.btnOk, "btnOk");
			this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnOk.Name = "btnOk";
			//
			// btnCancel
			//
			resources.ApplyResources(this.btnCancel, "btnCancel");
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Name = "btnCancel";
			//
			// btnHelp
			//
			resources.ApplyResources(this.btnHelp, "btnHelp");
			this.btnHelp.Name = "btnHelp";
			this.btnHelp.Click += new System.EventHandler(this.btnHelp_Click);
			//
			// m_persistence
			//
			this.m_persistence.Parent = this;
			this.m_persistence.SaveSettings += new SIL.FieldWorks.Common.Controls.Persistence.Settings(this.m_persistence_SaveSettings);
			this.m_persistence.LoadSettings += new SIL.FieldWorks.Common.Controls.Persistence.Settings(this.m_persistence_LoadSettings);
			//
			// lblInfo
			//
			resources.ApplyResources(this.lblInfo, "lblInfo");
			this.lblInfo.AutoEllipsis = true;
			this.lblInfo.BackColor = System.Drawing.Color.Transparent;
			this.lblInfo.Name = "lblInfo";
			//
			// FwChooserDlg
			//
			this.AcceptButton = this.btnOk;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnCancel;
			this.Controls.Add(this.lblInfo);
			this.Controls.Add(this.btnHelp);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnOk);
			this.Controls.Add(this.tvPossibilities);
			this.DoubleBuffered = true;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FwChooserDlg";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			((System.ComponentModel.ISupportInitialize)(this.m_persistence)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion

		#region Miscellaneous methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the dialog title based on the list name
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void SetTitle()
		{
			Text = string.Format(ResourceHelper.GetResourceString("kstidChooserDlgTitle"),
				m_list.Name.UserDefaultWritingSystem.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the sequence of selected possibilities
		/// </summary>
		/// <param name="listToUpdate">Sequence of possibilities. All possibilities that may be in
		/// this list get deleted and the new ones added.</param>
		/// <returns>True if the sequence was changed, false otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public bool GetPossibilities(IFdoReferenceSequence<ICmPossibility> listToUpdate)
		{
			CheckDisposed();

			Debug.Assert(listToUpdate != null);
			Guid[] origGuids = listToUpdate.ToGuidArray();

			// Clear the list first.
			listToUpdate.Clear();

			bool fAllItemsAreTheSame = true;
			int i = 0;
			foreach (ICmPossibility newPoss in tvPossibilities.SelectedPossibilities)
			{
				listToUpdate.Add(newPoss);
				fAllItemsAreTheSame &= (i < origGuids.Length && origGuids[i++] != newPoss.Guid);
			}

			return (!fAllItemsAreTheSame || i != origGuids.Length);
		}

		#endregion

		#region Event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Display Help
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void btnHelp_Click(object sender, System.EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helptopicProvider, m_helpTopicKey);
		}

		#endregion

		#region Persistence methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load the persistence settings for the chooser dialog.
		/// </summary>
		/// <param name="key">location in registry from which the settings will be loaded</param>
		/// ------------------------------------------------------------------------------------
		private void m_persistence_LoadSettings(Microsoft.Win32.RegistryKey key)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Save the persistence settings for the chooser dialog.
		/// </summary>
		/// <param name="key">location in registry where the settings will be saved</param>
		/// ------------------------------------------------------------------------------------
		private void m_persistence_SaveSettings(Microsoft.Win32.RegistryKey key)
		{
		}

		#endregion

		#region ISettings Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Save our choices history and checkbox settings
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void SaveSettingsNow()
		{
			CheckDisposed();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the registry key for this dialog.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual RegistryKey SettingsKey
		{
			get
			{
				CheckDisposed();
				return m_projSettingsKey.ProjectSpecificSettingsKey.CreateSubKey(@"List Chooser");
			}
		}

		#endregion

		#region ICmPossibilitySupplier Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the Chooser dialog to allow the user to select a single possibility.
		/// </summary>
		/// <remarks>Used for filtering</remarks>
		/// <param name="list">The possibility list used to populate the tree</param>
		/// <param name="initialSelected">The possibility to check initially, or null to
		/// show with no initial selection</param>
		/// <returns>A single chosen CmPossibility, or null if user cancels or chooses
		/// nothing.</returns>
		/// ------------------------------------------------------------------------------------
		public ICmPossibility GetPossibility(ICmPossibilityList list, ICmPossibility initialSelected)
		{
			//CheckDisposed();

			//m_initiallySelectedHvos = new List<int>(1);
			//m_initiallySelectedHvos.Add(hvoPoss);

			//if (list != (CmPossibilityList)m_list)
			//{
			//    m_list = list;
			//    SetTitle();
			//}

			//tvPossibilities.Load(m_list, m_initiallySelectedHvos);
			//if (ShowDialog() != DialogResult.OK)
			//    return 0;

			//List<int> newHvos = tvPossibilities.SelectedHvos;
			//return (newHvos != null && newHvos.Count > 0 ? newHvos[0] : 0);

			// REVIEW: Currently, the chooser tree doesn't allow the user to select
			// only one possibility. If this method and single node selection is ever
			// necessary, then this method will need to be rewritten.
			return null;
		}

		#endregion
	}
}

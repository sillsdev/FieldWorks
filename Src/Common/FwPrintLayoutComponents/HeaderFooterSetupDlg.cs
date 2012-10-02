// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: HeaderFooterSetupDlg.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.Resources;
using System.Reflection;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using SIL.FieldWorks.Common.RootSites;
using XCore;

namespace SIL.FieldWorks.Common.PrintLayout
{
	#region HeaderFooterSetupDlg class
	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for HeaderFooterSetupDlg.
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	public class HeaderFooterSetupDlg : Form, IFWDisposable
	{
		#region Member variables
		/// <summary></summary>
		protected HFSetView m_hfsvFirstTop;
		/// <summary></summary>
		protected HFSetView m_hfsvFirstBottom;
		/// <summary></summary>
		protected HFSetView m_hfsvEvenTop;
		/// <summary></summary>
		protected HFSetView m_hfsvEvenBottom;
		/// <summary></summary>
		protected HFSetView m_hfsvOddTop;
		/// <summary></summary>
		protected HFSetView m_hfsvOddBottom;
		/// <summary></summary>
		protected System.Windows.Forms.ListBox m_lstBoxName;
		/// <summary></summary>
		protected System.Windows.Forms.TextBox m_txtBoxDescription;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Panel m_pnlEvenPage;
		/// <summary>Required designer variable.</summary>
		private System.ComponentModel.Container components = null;
		private System.Windows.Forms.Panel panel2;
		private System.Windows.Forms.Panel panel4;
		private FdoCache m_cache;
		private ICmMajorObject m_HfSetOwner;
		private IPublication m_pub;
		private IPubDivision m_pubDiv;
		private IHelpTopicProvider m_helpProvider;
		private List<string> m_protectedHFSets;
		private CheckBox m_chkFirstSameAsOdd;
		private CheckBox m_chkEvenSameAsOdd;
		private Label m_lblOddPage;
		private Label m_lblEvenPage;
		private Panel m_pnlFirstTop;
		private Panel m_pnlFirstBottom;
		private Panel m_pnlEvenTop;
		private Panel m_pnlEvenBottom;
		private Panel m_pnlOddPage;
		private Panel m_pnlOddBottom;
		private Panel m_pnlOddTop;
		private Button m_btnDelete;
		private List<int> m_modifiedHFSets = new List<int>();
		#endregion

		#region Constructor/initialization/destructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructs a dialog for choosing, creating, modifying, and deleting Header/Footer
		/// sets
		/// </summary>
		/// <param name="cache">Cache</param>
		/// <param name="pub">The current publication</param>
		/// <param name="helpProvider">Object that gives access to help</param>
		/// <param name="protectedHFSets">List of names of H/F sets that cannot be deleted</param>
		/// <param name="hfOwner">The owner of the H/F set.</param>
		/// ------------------------------------------------------------------------------------
		public HeaderFooterSetupDlg(FdoCache cache, IPublication pub,
			IHelpTopicProvider helpProvider, List<string> protectedHFSets, ICmMajorObject hfOwner)
		{
			m_cache = cache;
			m_HfSetOwner = hfOwner;
			m_pub = pub;
			m_pubDiv = m_pub.DivisionsOS[0];
			m_helpProvider = helpProvider;
			m_protectedHFSets = protectedHFSets;
			Initialize();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the dialog
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void Initialize()
		{
			InitializeComponent();

			UpdateNameBox(m_pubDiv.HFSetOA.Name);
			ReplacePanelsWithHFSetViews();

			// Don't do this until after the listbox has been populated, because if the
			// values of the Checked properties are changed, the resulting code assumes the
			// list box already contains PubHFSets.
			m_chkEvenSameAsOdd.Checked = !m_pubDiv.DifferentEvenHF;
			m_chkFirstSameAsOdd.Checked = !m_pubDiv.DifferentFirstHF;

			// If publication is right-bound, reverse the location of the controls
			if (!m_pub.IsLeftBound)
			{
				Point temp = m_lblEvenPage.Location;
				m_lblEvenPage.Location = m_lblOddPage.Location;
				m_lblOddPage.Location = temp;

				temp = m_pnlEvenPage.Location;
				m_pnlEvenPage.Location = m_pnlOddPage.Location;
				m_pnlOddPage.Location = temp;
			}
		}

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
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
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			CloseRootBoxes();
			m_cache = null;
			m_HfSetOwner = null;
			m_pub = null;
			m_pubDiv = null;

			base.Dispose( disposing );
		}
		#endregion

		#region Windows Form Designer generated code
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			System.Windows.Forms.Button m_btnAdd;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HeaderFooterSetupDlg));
			System.Windows.Forms.Button m_btnModify;
			System.Windows.Forms.Button m_btnOk;
			System.Windows.Forms.Button m_btnCancel;
			System.Windows.Forms.Button m_btnHelp;
			System.Windows.Forms.Label m_lblDescription;
			System.Windows.Forms.TabControl tabControl1;
			System.Windows.Forms.TabPage m_tpFirst;
			System.Windows.Forms.Panel m_pnlFirstPage;
			System.Windows.Forms.Label m_lblFirstPage;
			System.Windows.Forms.TabPage m_tpOddEven;
			System.Windows.Forms.Panel panel3;
			System.Windows.Forms.GroupBox m_grpBoxEdit;
			SIL.FieldWorks.Common.Controls.LineControl lineControl1;
			this.m_pnlFirstTop = new System.Windows.Forms.Panel();
			this.m_chkFirstSameAsOdd = new System.Windows.Forms.CheckBox();
			this.m_pnlFirstBottom = new System.Windows.Forms.Panel();
			this.panel2 = new System.Windows.Forms.Panel();
			this.m_pnlEvenPage = new System.Windows.Forms.Panel();
			this.m_pnlEvenTop = new System.Windows.Forms.Panel();
			this.m_chkEvenSameAsOdd = new System.Windows.Forms.CheckBox();
			this.m_pnlEvenBottom = new System.Windows.Forms.Panel();
			this.m_pnlOddPage = new System.Windows.Forms.Panel();
			this.m_pnlOddBottom = new System.Windows.Forms.Panel();
			this.m_pnlOddTop = new System.Windows.Forms.Panel();
			this.m_lblOddPage = new System.Windows.Forms.Label();
			this.m_lblEvenPage = new System.Windows.Forms.Label();
			this.panel4 = new System.Windows.Forms.Panel();
			this.m_btnDelete = new System.Windows.Forms.Button();
			this.m_lstBoxName = new System.Windows.Forms.ListBox();
			this.panel1 = new System.Windows.Forms.Panel();
			this.m_txtBoxDescription = new System.Windows.Forms.TextBox();
			m_btnAdd = new System.Windows.Forms.Button();
			m_btnModify = new System.Windows.Forms.Button();
			m_btnOk = new System.Windows.Forms.Button();
			m_btnCancel = new System.Windows.Forms.Button();
			m_btnHelp = new System.Windows.Forms.Button();
			m_lblDescription = new System.Windows.Forms.Label();
			tabControl1 = new System.Windows.Forms.TabControl();
			m_tpFirst = new System.Windows.Forms.TabPage();
			m_pnlFirstPage = new System.Windows.Forms.Panel();
			m_lblFirstPage = new System.Windows.Forms.Label();
			m_tpOddEven = new System.Windows.Forms.TabPage();
			panel3 = new System.Windows.Forms.Panel();
			m_grpBoxEdit = new System.Windows.Forms.GroupBox();
			lineControl1 = new SIL.FieldWorks.Common.Controls.LineControl();
			tabControl1.SuspendLayout();
			m_tpFirst.SuspendLayout();
			m_pnlFirstPage.SuspendLayout();
			m_tpOddEven.SuspendLayout();
			this.m_pnlEvenPage.SuspendLayout();
			this.m_pnlOddPage.SuspendLayout();
			m_grpBoxEdit.SuspendLayout();
			this.panel1.SuspendLayout();
			this.SuspendLayout();
			//
			// m_btnAdd
			//
			resources.ApplyResources(m_btnAdd, "m_btnAdd");
			m_btnAdd.Name = "m_btnAdd";
			m_btnAdd.Click += new System.EventHandler(this.m_btnAdd_Click);
			//
			// m_btnModify
			//
			resources.ApplyResources(m_btnModify, "m_btnModify");
			m_btnModify.Name = "m_btnModify";
			m_btnModify.Click += new System.EventHandler(this.m_btnModify_Click);
			//
			// m_btnOk
			//
			resources.ApplyResources(m_btnOk, "m_btnOk");
			m_btnOk.Name = "m_btnOk";
			m_btnOk.Click += new System.EventHandler(this.m_btnOk_Click);
			//
			// m_btnCancel
			//
			m_btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(m_btnCancel, "m_btnCancel");
			m_btnCancel.Name = "m_btnCancel";
			//
			// m_btnHelp
			//
			resources.ApplyResources(m_btnHelp, "m_btnHelp");
			m_btnHelp.Name = "m_btnHelp";
			m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// m_lblDescription
			//
			resources.ApplyResources(m_lblDescription, "m_lblDescription");
			m_lblDescription.Name = "m_lblDescription";
			//
			// tabControl1
			//
			tabControl1.Controls.Add(m_tpFirst);
			tabControl1.Controls.Add(m_tpOddEven);
			resources.ApplyResources(tabControl1, "tabControl1");
			tabControl1.Name = "tabControl1";
			tabControl1.SelectedIndex = 0;
			//
			// m_tpFirst
			//
			m_tpFirst.Controls.Add(m_pnlFirstPage);
			m_tpFirst.Controls.Add(m_lblFirstPage);
			m_tpFirst.Controls.Add(this.panel2);
			resources.ApplyResources(m_tpFirst, "m_tpFirst");
			m_tpFirst.Name = "m_tpFirst";
			//
			// m_pnlFirstPage
			//
			m_pnlFirstPage.BackColor = System.Drawing.SystemColors.Window;
			m_pnlFirstPage.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			m_pnlFirstPage.Controls.Add(this.m_pnlFirstTop);
			m_pnlFirstPage.Controls.Add(this.m_chkFirstSameAsOdd);
			m_pnlFirstPage.Controls.Add(this.m_pnlFirstBottom);
			resources.ApplyResources(m_pnlFirstPage, "m_pnlFirstPage");
			m_pnlFirstPage.Name = "m_pnlFirstPage";
			//
			// m_pnlFirstTop
			//
			resources.ApplyResources(this.m_pnlFirstTop, "m_pnlFirstTop");
			this.m_pnlFirstTop.Name = "m_pnlFirstTop";
			//
			// m_chkFirstSameAsOdd
			//
			resources.ApplyResources(this.m_chkFirstSameAsOdd, "m_chkFirstSameAsOdd");
			this.m_chkFirstSameAsOdd.Name = "m_chkFirstSameAsOdd";
			this.m_chkFirstSameAsOdd.CheckedChanged += new System.EventHandler(this.m_chkFirstSameAsOdd_CheckedChanged);
			//
			// m_pnlFirstBottom
			//
			resources.ApplyResources(this.m_pnlFirstBottom, "m_pnlFirstBottom");
			this.m_pnlFirstBottom.Name = "m_pnlFirstBottom";
			//
			// m_lblFirstPage
			//
			resources.ApplyResources(m_lblFirstPage, "m_lblFirstPage");
			m_lblFirstPage.Name = "m_lblFirstPage";
			//
			// panel2
			//
			this.panel2.BackColor = System.Drawing.Color.Silver;
			resources.ApplyResources(this.panel2, "panel2");
			this.panel2.Name = "panel2";
			//
			// m_tpOddEven
			//
			m_tpOddEven.Controls.Add(this.m_pnlEvenPage);
			m_tpOddEven.Controls.Add(this.m_pnlOddPage);
			m_tpOddEven.Controls.Add(this.m_lblOddPage);
			m_tpOddEven.Controls.Add(this.m_lblEvenPage);
			m_tpOddEven.Controls.Add(panel3);
			m_tpOddEven.Controls.Add(this.panel4);
			resources.ApplyResources(m_tpOddEven, "m_tpOddEven");
			m_tpOddEven.Name = "m_tpOddEven";
			//
			// m_pnlEvenPage
			//
			this.m_pnlEvenPage.BackColor = System.Drawing.SystemColors.Window;
			this.m_pnlEvenPage.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_pnlEvenPage.Controls.Add(this.m_pnlEvenTop);
			this.m_pnlEvenPage.Controls.Add(this.m_chkEvenSameAsOdd);
			this.m_pnlEvenPage.Controls.Add(this.m_pnlEvenBottom);
			resources.ApplyResources(this.m_pnlEvenPage, "m_pnlEvenPage");
			this.m_pnlEvenPage.Name = "m_pnlEvenPage";
			//
			// m_pnlEvenTop
			//
			resources.ApplyResources(this.m_pnlEvenTop, "m_pnlEvenTop");
			this.m_pnlEvenTop.Name = "m_pnlEvenTop";
			//
			// m_chkEvenSameAsOdd
			//
			resources.ApplyResources(this.m_chkEvenSameAsOdd, "m_chkEvenSameAsOdd");
			this.m_chkEvenSameAsOdd.Name = "m_chkEvenSameAsOdd";
			this.m_chkEvenSameAsOdd.CheckedChanged += new System.EventHandler(this.m_chkEvenSameAsOdd_CheckedChanged);
			//
			// m_pnlEvenBottom
			//
			resources.ApplyResources(this.m_pnlEvenBottom, "m_pnlEvenBottom");
			this.m_pnlEvenBottom.Name = "m_pnlEvenBottom";
			//
			// m_pnlOddPage
			//
			this.m_pnlOddPage.BackColor = System.Drawing.SystemColors.Window;
			this.m_pnlOddPage.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_pnlOddPage.Controls.Add(this.m_pnlOddBottom);
			this.m_pnlOddPage.Controls.Add(this.m_pnlOddTop);
			resources.ApplyResources(this.m_pnlOddPage, "m_pnlOddPage");
			this.m_pnlOddPage.Name = "m_pnlOddPage";
			//
			// m_pnlOddBottom
			//
			resources.ApplyResources(this.m_pnlOddBottom, "m_pnlOddBottom");
			this.m_pnlOddBottom.Name = "m_pnlOddBottom";
			//
			// m_pnlOddTop
			//
			resources.ApplyResources(this.m_pnlOddTop, "m_pnlOddTop");
			this.m_pnlOddTop.Name = "m_pnlOddTop";
			//
			// m_lblOddPage
			//
			resources.ApplyResources(this.m_lblOddPage, "m_lblOddPage");
			this.m_lblOddPage.Name = "m_lblOddPage";
			//
			// m_lblEvenPage
			//
			resources.ApplyResources(this.m_lblEvenPage, "m_lblEvenPage");
			this.m_lblEvenPage.Name = "m_lblEvenPage";
			//
			// panel3
			//
			panel3.BackColor = System.Drawing.Color.Silver;
			resources.ApplyResources(panel3, "panel3");
			panel3.Name = "panel3";
			//
			// panel4
			//
			this.panel4.BackColor = System.Drawing.Color.Silver;
			resources.ApplyResources(this.panel4, "panel4");
			this.panel4.Name = "panel4";
			//
			// m_grpBoxEdit
			//
			m_grpBoxEdit.Controls.Add(m_btnModify);
			m_grpBoxEdit.Controls.Add(this.m_btnDelete);
			m_grpBoxEdit.Controls.Add(m_btnAdd);
			m_grpBoxEdit.Controls.Add(this.m_lstBoxName);
			resources.ApplyResources(m_grpBoxEdit, "m_grpBoxEdit");
			m_grpBoxEdit.Name = "m_grpBoxEdit";
			m_grpBoxEdit.TabStop = false;
			//
			// m_btnDelete
			//
			resources.ApplyResources(this.m_btnDelete, "m_btnDelete");
			this.m_btnDelete.Name = "m_btnDelete";
			this.m_btnDelete.Click += new System.EventHandler(this.m_btnDelete_Click);
			//
			// m_lstBoxName
			//
			resources.ApplyResources(this.m_lstBoxName, "m_lstBoxName");
			this.m_lstBoxName.Name = "m_lstBoxName";
			this.m_lstBoxName.SelectedIndexChanged += new System.EventHandler(this.m_lstBoxName_SelectedIndexChanged);
			//
			// panel1
			//
			this.panel1.Controls.Add(m_lblDescription);
			this.panel1.Controls.Add(tabControl1);
			this.panel1.Controls.Add(lineControl1);
			this.panel1.Controls.Add(this.m_txtBoxDescription);
			resources.ApplyResources(this.panel1, "panel1");
			this.panel1.Name = "panel1";
			//
			// lineControl1
			//
			lineControl1.BackColor = System.Drawing.Color.Transparent;
			resources.ApplyResources(lineControl1, "lineControl1");
			lineControl1.ForeColor2 = System.Drawing.Color.Transparent;
			lineControl1.LinearGradientMode = System.Drawing.Drawing2D.LinearGradientMode.Horizontal;
			lineControl1.Name = "lineControl1";
			//
			// m_txtBoxDescription
			//
			resources.ApplyResources(this.m_txtBoxDescription, "m_txtBoxDescription");
			this.m_txtBoxDescription.Name = "m_txtBoxDescription";
			this.m_txtBoxDescription.ReadOnly = true;
			//
			// HeaderFooterSetupDlg
			//
			this.AcceptButton = m_btnOk;
			resources.ApplyResources(this, "$this");
			this.CancelButton = m_btnCancel;
			this.Controls.Add(m_grpBoxEdit);
			this.Controls.Add(this.panel1);
			this.Controls.Add(m_btnHelp);
			this.Controls.Add(m_btnCancel);
			this.Controls.Add(m_btnOk);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "HeaderFooterSetupDlg";
			this.ShowInTaskbar = false;
			tabControl1.ResumeLayout(false);
			m_tpFirst.ResumeLayout(false);
			m_pnlFirstPage.ResumeLayout(false);
			m_tpOddEven.ResumeLayout(false);
			this.m_pnlEvenPage.ResumeLayout(false);
			this.m_pnlOddPage.ResumeLayout(false);
			m_grpBoxEdit.ResumeLayout(false);
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			this.ResumeLayout(false);

		}
		#endregion

		#region Private methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Refreshes the display of the preview
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void UpdatePreView()
		{
			IPubHFSet hfset = CurrentHFSet;
			// description
			m_txtBoxDescription.Text = hfset.Description.Text;

			UpdateFirstPagePreview();

			UpdateEvenPagePreview();

			// odd page
			if (m_hfsvOddTop != null)
			{
				m_hfsvOddTop.Header = hfset.DefaultHeaderOA.Hvo;
				m_hfsvOddTop.MakeRoot();
				m_hfsvOddTop.PerformLayout();
			}
			if (m_hfsvOddBottom != null)
			{
				m_hfsvOddBottom.Header = hfset.DefaultFooterOA.Hvo;
				m_hfsvOddBottom.MakeRoot();
				m_hfsvOddBottom.PerformLayout();
			}

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Refreshes the display of the first page preview
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void UpdateFirstPagePreview()
		{
			IPubHFSet hfset = CurrentHFSet;
			if (m_hfsvFirstTop != null)
			{
				m_hfsvFirstTop.Header = (m_chkFirstSameAsOdd.Checked ?
					hfset.DefaultHeaderOA.Hvo :
					hfset.FirstHeaderOA.Hvo);
				m_hfsvFirstTop.MakeRoot();
				m_hfsvFirstTop.PerformLayout();
			}
			if (m_hfsvFirstBottom != null)
			{
				m_hfsvFirstBottom.Header = (m_chkFirstSameAsOdd.Checked ?
					hfset.DefaultFooterOA.Hvo :
					hfset.FirstFooterOA.Hvo);
				m_hfsvFirstBottom.MakeRoot();
				m_hfsvFirstBottom.PerformLayout();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Refreshes the display of the even page preview
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void UpdateEvenPagePreview()
		{
			IPubHFSet hfset = CurrentHFSet;
			if (m_hfsvEvenTop != null)
			{
				m_hfsvEvenTop.Header = (m_chkEvenSameAsOdd.Checked ?
					hfset.DefaultHeaderOA.Hvo :
					hfset.EvenHeaderOA.Hvo);
				m_hfsvEvenTop.MakeRoot();
				m_hfsvEvenTop.PerformLayout();
			}
			if (m_hfsvEvenBottom != null)
			{
				m_hfsvEvenBottom.Header = (m_chkEvenSameAsOdd.Checked ?
					hfset.DefaultFooterOA.Hvo :
					hfset.EvenFooterOA.Hvo);
				m_hfsvEvenBottom.MakeRoot();
				m_hfsvEvenBottom.PerformLayout();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the current Header Footer Set that is selected
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private IPubHFSet CurrentHFSet
		{
			get { return m_lstBoxName.SelectedItem as IPubHFSet; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the list of Header/Footer sets
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void UpdateNameBox(string nameToSelect)
		{
			m_lstBoxName.Items.Clear();

			foreach (IPubHFSet hfSet in m_HfSetOwner.HeaderFooterSetsOC)
				m_lstBoxName.Items.Add(hfSet);

			if (m_lstBoxName.Items.Count > 0)
			{
				int i = (string.IsNullOrEmpty(nameToSelect) ?
					m_lstBoxName.Items.Count - 1 : m_lstBoxName.FindString(nameToSelect));

				m_lstBoxName.SelectedIndex = (i >= 0 ? i : 0);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Replaces all the header/footer panels with HFSetView's initialized with the correct
		/// H/F info
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ReplacePanelsWithHFSetViews()
		{
			IPubHFSet hfSet = CurrentHFSet;

			m_hfsvFirstTop = MakeHFSetView(m_pnlFirstTop, hfSet.FirstHeaderOA, 1);
			m_hfsvFirstBottom = MakeHFSetView(m_pnlFirstBottom, hfSet.FirstFooterOA, 1);
			m_hfsvEvenTop = MakeHFSetView(m_pnlEvenTop, hfSet.EvenHeaderOA, 2);
			m_hfsvEvenBottom = MakeHFSetView(m_pnlEvenBottom, hfSet.EvenFooterOA, 2);
			m_hfsvOddTop = MakeHFSetView(m_pnlOddTop, hfSet.DefaultHeaderOA, 3);
			m_hfsvOddBottom = MakeHFSetView(m_pnlOddBottom, hfSet.DefaultFooterOA, 3);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a header/footer set view for the given information.
		/// </summary>
		/// <param name="pnl"></param>
		/// <param name="toDisplay"></param>
		/// <param name="pageNum"></param>
		/// ------------------------------------------------------------------------------------
		private HFSetView MakeHFSetView(Panel pnl, IPubHeader toDisplay, int pageNum)
		{
			HFDialogsPageInfo info = new HFDialogsPageInfo(m_pub.IsLeftBound);
			info.PageNumber = pageNum;
			HFSetupDlgVC vc = new HFSetupDlgVC(info,
				m_cache.DefaultVernWs, DateTime.Now, m_cache);
			HFSetView view = new HFSetView(m_cache.DomainDataByFlid, vc, toDisplay.Hvo);
			view.Dock = DockStyle.Fill;
			pnl.Controls.Add(view);
			return view;
		}
		#endregion

		#region Event Handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Close any open rootboxes
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnClosing(CancelEventArgs e)
		{
			base.OnClosing (e);
			CloseRootBoxes();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Close any rootboxes that were made.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void CloseRootBoxes()
		{
			m_hfsvFirstTop.CloseRootBox();
			m_hfsvFirstBottom.CloseRootBox();
			m_hfsvEvenTop.CloseRootBox();
			m_hfsvEvenBottom.CloseRootBox();
			m_hfsvOddTop.CloseRootBox();
			m_hfsvOddBottom.CloseRootBox();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the add button is pressed
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected void m_btnAdd_Click(object sender, System.EventArgs e)
		{
			IPubHFSet HFSet = m_cache.ServiceLocator.GetInstance<IPubHFSetFactory>().Create();
			m_HfSetOwner.HeaderFooterSetsOC.Add(HFSet);
			IPubHeaderFactory phFactory = m_cache.ServiceLocator.GetInstance<IPubHeaderFactory>();
			HFSet.DefaultFooterOA = phFactory.Create();
			HFSet.DefaultHeaderOA = phFactory.Create();
			HFSet.FirstFooterOA = phFactory.Create();
			HFSet.FirstHeaderOA = phFactory.Create();
			HFSet.EvenFooterOA = phFactory.Create();
			HFSet.EvenHeaderOA = phFactory.Create();

			using (HeaderFooterModifyDlg dlg = new HeaderFooterModifyDlg(m_cache, HFSet as IPubHFSet, m_pub, m_helpProvider))
			{
				if (dlg.ShowDialog() == DialogResult.OK)
				{
					UpdateNameBox(null);
					UpdatePreView();
				}
				else
				{
					// We don't want a new PubHFSet after all...
					m_cache.DomainDataByFlid.DeleteObj(HFSet.Hvo);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the modify button is pressed
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected void m_btnModify_Click(object sender, System.EventArgs e)
		{
			using (HeaderFooterModifyDlg dlg = new HeaderFooterModifyDlg(m_cache, CurrentHFSet,
					   m_pub, m_helpProvider))
			{
				if (dlg.ShowDialog() == DialogResult.OK)
				{
					if (!m_modifiedHFSets.Contains(CurrentHFSet.Hvo))
						m_modifiedHFSets.Add(CurrentHFSet.Hvo);

					UpdatePreView();
					UpdateNameBox(CurrentHFSet.Name);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the delete button is pressed
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected void m_btnDelete_Click(object sender, System.EventArgs e)
		{
			IPubHFSet hfSet = CurrentHFSet;
			int i = m_lstBoxName.SelectedIndex;
			i = (i > 0) ? (i - 1) : i + 1;
			m_lstBoxName.SelectedItem = m_lstBoxName.Items[i];
			m_lstBoxName.Items.Remove(hfSet);
			// Application.DoEvents();	// no noticeable issues when this is not called
			m_HfSetOwner.HeaderFooterSetsOC.Remove(hfSet);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the preview when the first page same as odd is checked
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_chkFirstSameAsOdd_CheckedChanged(object sender, System.EventArgs e)
		{
			UpdateFirstPagePreview();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the preview when the even page same as odd is checked
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_chkEvenSameAsOdd_CheckedChanged(object sender, System.EventArgs e)
		{
			UpdateEvenPagePreview();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the preview when the user selects a different set
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_lstBoxName_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			UpdatePreView();

			// If the selected set is built-in, don't allow the user to delete it.
			IPubHFSet currentSet = CurrentHFSet;
			m_btnDelete.Enabled = (m_lstBoxName.Items.Count > 1 &&
				(m_protectedHFSets == null || !m_protectedHFSets.Contains(currentSet.Name)));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the OK button press
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_btnOk_Click(object sender, System.EventArgs e)
		{
			IPubHFSet currentSet = CurrentHFSet;
			IPubHFSet pubSet = m_pubDiv.HFSetOA;
			m_pubDiv.DifferentEvenHF = !m_chkEvenSameAsOdd.Checked;
			m_pubDiv.DifferentFirstHF = !m_chkFirstSameAsOdd.Checked;

			// Copy the Header / Footer Sets to the Publication
			pubSet.Name = currentSet.Name;
			pubSet.CloneDetails(currentSet);
			Close();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the Help button press
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_btnHelp_Click(object sender, System.EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpProvider, "khtpHeaderFooterSetup");
		}
		#endregion

		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Indicates whether the Header/footer set having the specified hvo was modified
		/// during the course of this dialog.
		/// </summary>
		/// <param name="hfHvo">The Hvo of the H/F set in question</param>
		/// <returns><c>true</c> if it was modified; <c>false</c> otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public bool HFSetWasModified(int hfHvo)
		{
			CheckDisposed();
			return m_modifiedHFSets.Contains(hfHvo);
		}

		#endregion
	}
	#endregion

	#region HFSetupDlgVC class
	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// This is the view constructor for the views in the Header/Footer setup dialog
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	public class HFSetupDlgVC : HeaderFooterVc
	{
		private ResourceManager m_resources;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new HFSetupDlgVC
		/// </summary>
		/// <param name="page">The PageInfo for use in creating the view</param>
		/// <param name="wsDefault">ID of default writing system</param>
		/// <param name="printDateTime">Printing date/time</param>
		/// <param name="cache">The cache.</param>
		/// ------------------------------------------------------------------------------------
		public HFSetupDlgVC(IPageInfo page, int wsDefault, DateTime printDateTime, FdoCache cache)
			: base(page, wsDefault, printDateTime, cache)
		{
			m_resources = new ResourceManager(
				"SIL.FieldWorks.Common.PrintLayout.FwPrintLayoutStrings",
				Assembly.GetExecutingAssembly());
		}

		#region Private methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private ITsString CreateTss(string text)
		{
			return m_cache.TsStrFactory.MakeString(text, DefaultWs);
		}
		#endregion

		#region Overrides of HeaderFooterVc
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// overrides the display method to make everything read-only
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="hvo">id of a PubHeader object</param>
		/// <param name="frag">Constant (ignored)</param>
		/// ------------------------------------------------------------------------------------
		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{

			// make everything read-only
			vwenv.set_IntProperty((int)FwTextPropType.ktptEditable,
				(int)FwTextPropVar.ktpvEnum,
				(int)TptEditable.ktptNotEditable);
			base.Display(vwenv, hvo, frag);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the page number
		/// </summary>
		/// <returns>An ITsString with the page number</returns>
		/// ------------------------------------------------------------------------------------
		public override ITsString PageNumber
		{
			get
			{
				return CreateTss(m_page.PageNumber.ToString());
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the first reference on the page
		/// </summary>
		/// <returns>An ITsString with the first reference on the page</returns>
		/// ------------------------------------------------------------------------------------
		public override ITsString FirstReference
		{
			get
			{
				return CreateTss(m_resources.GetString("kstidFirstReference"));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the last reference on the page
		/// </summary>
		/// <returns>An ITsString with the last reference for the page</returns>
		/// ------------------------------------------------------------------------------------
		public override ITsString LastReference
		{
			get
			{
				return CreateTss(m_resources.GetString("kstidEndReference"));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the publication title
		/// </summary>
		/// <returns>An ITsString with the publicatin title</returns>
		/// ------------------------------------------------------------------------------------
		public override ITsString PublicationTitle
		{
			get
			{
				return CreateTss(m_resources.GetString("kstidPubTitle"));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the division name
		/// </summary>
		/// <returns>An ITsString with the division name</returns>
		/// ------------------------------------------------------------------------------------
		public override ITsString DivisionName
		{
			get
			{
				return CreateTss(m_resources.GetString("kstidDivName"));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the total number of pages
		/// </summary>
		/// <returns>An ITsString with the total number of pages</returns>
		/// ------------------------------------------------------------------------------------
		public override ITsString TotalPages
		{
			get
			{
				return CreateTss(m_resources.GetString("kstidPageCount"));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a ITsString that represents a pseudo-page reference. Current implementation
		/// returns "Matthew".
		/// </summary>
		/// <returns>An ITsString that represents a pseudo-page reference</returns>
		/// ------------------------------------------------------------------------------------
		public override ITsString PageReference
		{
			get
			{
				return CreateTss(m_resources.GetString("kstidPageReference"));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the print date for the publication
		/// </summary>
		/// <returns>An ITsString with the print date</returns>
		/// ------------------------------------------------------------------------------------
		public override ITsString PrintDate
		{
			get
			{
				return CreateTss(System.DateTime.Now.ToShortDateString());
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the language project name
		/// </summary>
		/// <returns>An ITsString with the project name</returns>
		/// ------------------------------------------------------------------------------------
		public override ITsString ProjectName
		{
			get
			{
				return CreateTss(m_resources.GetString("kstidProjectName"));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the book name
		/// </summary>
		/// <returns>An ITsString with the book name</returns>
		/// ------------------------------------------------------------------------------------
		public override ITsString BookName
		{
			get
			{
				return CreateTss(m_resources.GetString("kstidBookName"));
			}
		}
		#endregion
	}
	#endregion
}

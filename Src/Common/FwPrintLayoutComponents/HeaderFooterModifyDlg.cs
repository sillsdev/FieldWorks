// Copyright (c) 2004-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: HeaderFooterModifyDlg.cs
// Responsibility: TeTeam

using System;
using System.Resources;
using System.Reflection;
using System.Drawing;
using System.ComponentModel;
using System.Windows.Forms;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Utils;
using SIL.CoreImpl;
using XCore;

namespace SIL.FieldWorks.Common.PrintLayout
{
	#region HeaderFooterModifyDlg class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for HeaderFooterModifyDlg.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class HeaderFooterModifyDlg : Form, IFWDisposable
	{
		#region Member variables
		/// <summary></summary>
		protected HFSetView m_hfsvHeader;
		/// <summary></summary>
		protected HFSetView m_hfsvFooter;
		/// <summary></summary>
		protected HFSetView m_lastFocusedView;
		/// <summary></summary>
		protected IPubHFSet m_curHFSet;

		private IPublication m_pub;
		private IHelpTopicProvider m_helpProvider;
		private CachePair m_caches;

		private string m_innerHeaderText;
		private string m_innerFooterText;
		private string m_outerHeaderText;
		private string m_outerFooterText;

		// In an in-memory cache, we can 'create' a totally fake object by just inventing
		// an id number.
		const int khvoPHFSet = 1234567890;
		const int kflidDefaultHeaderName = 46046999;
		const int kflidDefaultFooterName = 46046998;
		const int kflidFirstHeaderName = 46046997;
		const int kflidFirstFooterName = 46046996;
		const int kflidEvenHeaderName = 46046995;
		const int kflidEvenFooterName = 46046994;
		int m_hvoDefaultHeaderSec;
		int m_hvoDefaultFooterSec;
		int m_hvoFirstHeaderSec;
		int m_hvoFirstFooterSec;
		int m_hvoEvenHeaderSec;
		int m_hvoEvenFooterSec;

		//		private string m_savePoint;
		private System.Windows.Forms.TabPage m_tbFirstPage;
		private System.Windows.Forms.TabPage m_tbEvenPage;
		private System.Windows.Forms.TabPage m_tbOddPage;
		private System.Windows.Forms.TabControl m_tbControl;
		private System.Windows.Forms.Panel m_pnlTabPage;
		private System.Windows.Forms.Panel m_pnlHeader;
		private System.Windows.Forms.Panel m_pnlFooter;
		private System.Windows.Forms.TextBox m_txtBoxDescription;
		private System.Windows.Forms.TextBox m_txtBoxName;
		private Button m_btnOK;
		private Label m_lblRightFooter;
		private Label m_lblLeftFooter;
		private Label m_lblRightHeader;
		private Label m_lblLeftHeader;
		private System.ComponentModel.IContainer components;
		#endregion

		#region Constructor/destructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create the dialog for modifying headers and footers
		/// </summary>
		/// <param name="cache">the cache</param>
		/// <param name="curHFSet">publication header/footer set</param>
		/// <param name="pub">publication (used to determine if the publication is right-bound
		/// or left-bound to determine placement of objects and dialog labels in dialog</param>
		/// <param name="helpProvider">provides context-senstive help for this dialog</param>
		/// ------------------------------------------------------------------------------------
		public HeaderFooterModifyDlg(FdoCache cache, IPubHFSet curHFSet, IPublication pub,
			IHelpTopicProvider helpProvider)
		{
			m_caches = new CachePair();
			m_caches.MainCache = cache;
			m_caches.CreateSecCache();
			m_curHFSet = curHFSet;
			m_pub = pub;
			m_helpProvider = helpProvider;
			LoadRealDataIntoSec();

			InitializeComponent();

			m_innerHeaderText = m_lblLeftHeader.Text;
			m_innerFooterText = m_lblLeftFooter.Text;
			m_outerHeaderText = m_lblRightHeader.Text;
			m_outerFooterText = m_lblRightFooter.Text;

			ChangePanelsToViews();
			m_lastFocusedView = m_hfsvHeader;
			m_txtBoxDescription.Text = m_curHFSet.Description.Text;
			m_txtBoxName.Text = m_curHFSet.Name;
			UpdateOKButton();
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing"></param>
		/// ------------------------------------------------------------------------------------
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
				if (m_caches != null)
					m_caches.Dispose();
				if (m_hfsvHeader != null)
					m_hfsvHeader.Dispose();
				if (m_hfsvFooter != null)
					m_hfsvFooter.Dispose();
			}
			m_lastFocusedView = null;
			m_hfsvHeader = null;
			m_hfsvFooter = null;

			base.Dispose( disposing );
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HeaderFooterModifyDlg));
			System.Windows.Forms.Button m_btnCancel;
			System.Windows.Forms.Button m_btnHelp;
			System.Windows.Forms.Panel panel1;
			System.Windows.Forms.Button m_btnBookName;
			System.Windows.Forms.ImageList imageList1;
			System.Windows.Forms.Button m_btnProjectName;
			System.Windows.Forms.Button m_btnPrintDate;
			System.Windows.Forms.Button m_btnPageRef;
			System.Windows.Forms.Button m_btnDivName;
			System.Windows.Forms.Button m_btnPubTitle;
			System.Windows.Forms.Button m_btnPageCount;
			System.Windows.Forms.Button m_btnEndRef;
			System.Windows.Forms.Button m_btnFirstRef;
			System.Windows.Forms.Button m_btnPageNumber;
			System.Windows.Forms.Label m_lblCenterFooter;
			System.Windows.Forms.Label m_lblCenterHeader;
			System.Windows.Forms.ToolTip m_headerFooterTips;
			System.Windows.Forms.GroupBox groupBox1;
			System.Windows.Forms.Label label2;
			System.Windows.Forms.Label label1;
			this.m_btnOK = new System.Windows.Forms.Button();
			this.m_tbControl = new System.Windows.Forms.TabControl();
			this.m_tbFirstPage = new System.Windows.Forms.TabPage();
			this.m_pnlTabPage = new System.Windows.Forms.Panel();
			this.m_lblRightFooter = new System.Windows.Forms.Label();
			this.m_lblLeftFooter = new System.Windows.Forms.Label();
			this.m_lblRightHeader = new System.Windows.Forms.Label();
			this.m_lblLeftHeader = new System.Windows.Forms.Label();
			this.m_pnlHeader = new System.Windows.Forms.Panel();
			this.m_pnlFooter = new System.Windows.Forms.Panel();
			this.m_tbEvenPage = new System.Windows.Forms.TabPage();
			this.m_tbOddPage = new System.Windows.Forms.TabPage();
			this.m_txtBoxDescription = new System.Windows.Forms.TextBox();
			this.m_txtBoxName = new System.Windows.Forms.TextBox();
			m_btnCancel = new System.Windows.Forms.Button();
			m_btnHelp = new System.Windows.Forms.Button();
			panel1 = new System.Windows.Forms.Panel();
			m_btnBookName = new System.Windows.Forms.Button();
			imageList1 = new System.Windows.Forms.ImageList(this.components);
			m_btnProjectName = new System.Windows.Forms.Button();
			m_btnPrintDate = new System.Windows.Forms.Button();
			m_btnPageRef = new System.Windows.Forms.Button();
			m_btnDivName = new System.Windows.Forms.Button();
			m_btnPubTitle = new System.Windows.Forms.Button();
			m_btnPageCount = new System.Windows.Forms.Button();
			m_btnEndRef = new System.Windows.Forms.Button();
			m_btnFirstRef = new System.Windows.Forms.Button();
			m_btnPageNumber = new System.Windows.Forms.Button();
			m_lblCenterFooter = new System.Windows.Forms.Label();
			m_lblCenterHeader = new System.Windows.Forms.Label();
			m_headerFooterTips = new System.Windows.Forms.ToolTip(this.components);
			groupBox1 = new System.Windows.Forms.GroupBox();
			label2 = new System.Windows.Forms.Label();
			label1 = new System.Windows.Forms.Label();
			this.m_tbControl.SuspendLayout();
			this.m_tbFirstPage.SuspendLayout();
			this.m_pnlTabPage.SuspendLayout();
			panel1.SuspendLayout();
			groupBox1.SuspendLayout();
			this.SuspendLayout();
			//
			// m_btnOK
			//
			resources.ApplyResources(this.m_btnOK, "m_btnOK");
			this.m_btnOK.Name = "m_btnOK";
			this.m_btnOK.Click += new System.EventHandler(this.m_btnOK_Click);
			//
			// m_btnCancel
			//
			m_btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(m_btnCancel, "m_btnCancel");
			m_btnCancel.Name = "m_btnCancel";
			m_btnCancel.Click += new System.EventHandler(this.m_btnCancel_Click);
			//
			// m_btnHelp
			//
			resources.ApplyResources(m_btnHelp, "m_btnHelp");
			m_btnHelp.Name = "m_btnHelp";
			m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// m_tbControl
			//
			this.m_tbControl.Controls.Add(this.m_tbFirstPage);
			this.m_tbControl.Controls.Add(this.m_tbEvenPage);
			this.m_tbControl.Controls.Add(this.m_tbOddPage);
			resources.ApplyResources(this.m_tbControl, "m_tbControl");
			this.m_tbControl.Name = "m_tbControl";
			this.m_tbControl.SelectedIndex = 0;
			this.m_tbControl.SelectedIndexChanged += new System.EventHandler(this.m_tbControl_SelectedIndexChanged);
			//
			// m_tbFirstPage
			//
			this.m_tbFirstPage.Controls.Add(this.m_pnlTabPage);
			resources.ApplyResources(this.m_tbFirstPage, "m_tbFirstPage");
			this.m_tbFirstPage.Name = "m_tbFirstPage";
			//
			// m_pnlTabPage
			//
			this.m_pnlTabPage.Controls.Add(panel1);
			this.m_pnlTabPage.Controls.Add(this.m_lblRightFooter);
			this.m_pnlTabPage.Controls.Add(m_lblCenterFooter);
			this.m_pnlTabPage.Controls.Add(this.m_lblLeftFooter);
			this.m_pnlTabPage.Controls.Add(this.m_lblRightHeader);
			this.m_pnlTabPage.Controls.Add(m_lblCenterHeader);
			this.m_pnlTabPage.Controls.Add(this.m_lblLeftHeader);
			this.m_pnlTabPage.Controls.Add(this.m_pnlHeader);
			this.m_pnlTabPage.Controls.Add(this.m_pnlFooter);
			resources.ApplyResources(this.m_pnlTabPage, "m_pnlTabPage");
			this.m_pnlTabPage.Name = "m_pnlTabPage";
			//
			// panel1
			//
			panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			panel1.Controls.Add(m_btnBookName);
			panel1.Controls.Add(m_btnProjectName);
			panel1.Controls.Add(m_btnPrintDate);
			panel1.Controls.Add(m_btnPageRef);
			panel1.Controls.Add(m_btnDivName);
			panel1.Controls.Add(m_btnPubTitle);
			panel1.Controls.Add(m_btnPageCount);
			panel1.Controls.Add(m_btnEndRef);
			panel1.Controls.Add(m_btnFirstRef);
			panel1.Controls.Add(m_btnPageNumber);
			resources.ApplyResources(panel1, "panel1");
			panel1.Name = "panel1";
			//
			// m_btnBookName
			//
			resources.ApplyResources(m_btnBookName, "m_btnBookName");
			m_btnBookName.ImageList = imageList1;
			m_btnBookName.Name = "m_btnBookName";
			m_headerFooterTips.SetToolTip(m_btnBookName, resources.GetString("m_btnBookName.ToolTip"));
			m_btnBookName.Click += new System.EventHandler(this.m_btnBookName_Click);
			//
			// imageList1
			//
			imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
			imageList1.TransparentColor = System.Drawing.Color.Magenta;
			imageList1.Images.SetKeyName(0, "");
			imageList1.Images.SetKeyName(1, "");
			imageList1.Images.SetKeyName(2, "");
			imageList1.Images.SetKeyName(3, "");
			imageList1.Images.SetKeyName(4, "");
			imageList1.Images.SetKeyName(5, "");
			imageList1.Images.SetKeyName(6, "");
			imageList1.Images.SetKeyName(7, "");
			imageList1.Images.SetKeyName(8, "");
			imageList1.Images.SetKeyName(9, "");
			//
			// m_btnProjectName
			//
			resources.ApplyResources(m_btnProjectName, "m_btnProjectName");
			m_btnProjectName.ImageList = imageList1;
			m_btnProjectName.Name = "m_btnProjectName";
			m_headerFooterTips.SetToolTip(m_btnProjectName, resources.GetString("m_btnProjectName.ToolTip"));
			m_btnProjectName.Click += new System.EventHandler(this.m_btnProjectName_Click);
			//
			// m_btnPrintDate
			//
			resources.ApplyResources(m_btnPrintDate, "m_btnPrintDate");
			m_btnPrintDate.ImageList = imageList1;
			m_btnPrintDate.Name = "m_btnPrintDate";
			m_headerFooterTips.SetToolTip(m_btnPrintDate, resources.GetString("m_btnPrintDate.ToolTip"));
			m_btnPrintDate.Click += new System.EventHandler(this.m_btnPrintDate_Click);
			//
			// m_btnPageRef
			//
			resources.ApplyResources(m_btnPageRef, "m_btnPageRef");
			m_btnPageRef.ImageList = imageList1;
			m_btnPageRef.Name = "m_btnPageRef";
			m_headerFooterTips.SetToolTip(m_btnPageRef, resources.GetString("m_btnPageRef.ToolTip"));
			m_btnPageRef.Click += new System.EventHandler(this.m_btnPageRef_Click);
			//
			// m_btnDivName
			//
			resources.ApplyResources(m_btnDivName, "m_btnDivName");
			m_btnDivName.ImageList = imageList1;
			m_btnDivName.Name = "m_btnDivName";
			m_headerFooterTips.SetToolTip(m_btnDivName, resources.GetString("m_btnDivName.ToolTip"));
			m_btnDivName.Click += new System.EventHandler(this.m_btnDivName_Click);
			//
			// m_btnPubTitle
			//
			resources.ApplyResources(m_btnPubTitle, "m_btnPubTitle");
			m_btnPubTitle.ImageList = imageList1;
			m_btnPubTitle.Name = "m_btnPubTitle";
			m_headerFooterTips.SetToolTip(m_btnPubTitle, resources.GetString("m_btnPubTitle.ToolTip"));
			m_btnPubTitle.Click += new System.EventHandler(this.m_btnPubTitle_Click);
			//
			// m_btnPageCount
			//
			resources.ApplyResources(m_btnPageCount, "m_btnPageCount");
			m_btnPageCount.ImageList = imageList1;
			m_btnPageCount.Name = "m_btnPageCount";
			m_headerFooterTips.SetToolTip(m_btnPageCount, resources.GetString("m_btnPageCount.ToolTip"));
			m_btnPageCount.Click += new System.EventHandler(this.m_btnPageCount_Click);
			//
			// m_btnEndRef
			//
			resources.ApplyResources(m_btnEndRef, "m_btnEndRef");
			m_btnEndRef.ImageList = imageList1;
			m_btnEndRef.Name = "m_btnEndRef";
			m_headerFooterTips.SetToolTip(m_btnEndRef, resources.GetString("m_btnEndRef.ToolTip"));
			m_btnEndRef.Click += new System.EventHandler(this.m_btnEndRef_Click);
			//
			// m_btnFirstRef
			//
			resources.ApplyResources(m_btnFirstRef, "m_btnFirstRef");
			m_btnFirstRef.ImageList = imageList1;
			m_btnFirstRef.Name = "m_btnFirstRef";
			m_headerFooterTips.SetToolTip(m_btnFirstRef, resources.GetString("m_btnFirstRef.ToolTip"));
			m_btnFirstRef.Click += new System.EventHandler(this.m_btnFirstRef_Click);
			//
			// m_btnPageNumber
			//
			resources.ApplyResources(m_btnPageNumber, "m_btnPageNumber");
			m_btnPageNumber.ImageList = imageList1;
			m_btnPageNumber.Name = "m_btnPageNumber";
			m_headerFooterTips.SetToolTip(m_btnPageNumber, resources.GetString("m_btnPageNumber.ToolTip"));
			m_btnPageNumber.Click += new System.EventHandler(this.m_btnPageNumber_Click);
			//
			// m_lblRightFooter
			//
			resources.ApplyResources(this.m_lblRightFooter, "m_lblRightFooter");
			this.m_lblRightFooter.Name = "m_lblRightFooter";
			//
			// m_lblCenterFooter
			//
			resources.ApplyResources(m_lblCenterFooter, "m_lblCenterFooter");
			m_lblCenterFooter.Name = "m_lblCenterFooter";
			//
			// m_lblLeftFooter
			//
			resources.ApplyResources(this.m_lblLeftFooter, "m_lblLeftFooter");
			this.m_lblLeftFooter.Name = "m_lblLeftFooter";
			//
			// m_lblRightHeader
			//
			resources.ApplyResources(this.m_lblRightHeader, "m_lblRightHeader");
			this.m_lblRightHeader.Name = "m_lblRightHeader";
			//
			// m_lblCenterHeader
			//
			resources.ApplyResources(m_lblCenterHeader, "m_lblCenterHeader");
			m_lblCenterHeader.Name = "m_lblCenterHeader";
			//
			// m_lblLeftHeader
			//
			resources.ApplyResources(this.m_lblLeftHeader, "m_lblLeftHeader");
			this.m_lblLeftHeader.Name = "m_lblLeftHeader";
			//
			// m_pnlHeader
			//
			this.m_pnlHeader.BackColor = System.Drawing.SystemColors.Window;
			this.m_pnlHeader.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			resources.ApplyResources(this.m_pnlHeader, "m_pnlHeader");
			this.m_pnlHeader.Name = "m_pnlHeader";
			//
			// m_pnlFooter
			//
			this.m_pnlFooter.BackColor = System.Drawing.SystemColors.Window;
			this.m_pnlFooter.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			resources.ApplyResources(this.m_pnlFooter, "m_pnlFooter");
			this.m_pnlFooter.Name = "m_pnlFooter";
			//
			// m_tbEvenPage
			//
			resources.ApplyResources(this.m_tbEvenPage, "m_tbEvenPage");
			this.m_tbEvenPage.Name = "m_tbEvenPage";
			//
			// m_tbOddPage
			//
			resources.ApplyResources(this.m_tbOddPage, "m_tbOddPage");
			this.m_tbOddPage.Name = "m_tbOddPage";
			//
			// groupBox1
			//
			groupBox1.Controls.Add(this.m_txtBoxDescription);
			groupBox1.Controls.Add(label2);
			groupBox1.Controls.Add(label1);
			groupBox1.Controls.Add(this.m_txtBoxName);
			resources.ApplyResources(groupBox1, "groupBox1");
			groupBox1.Name = "groupBox1";
			groupBox1.TabStop = false;
			//
			// m_txtBoxDescription
			//
			resources.ApplyResources(this.m_txtBoxDescription, "m_txtBoxDescription");
			this.m_txtBoxDescription.Name = "m_txtBoxDescription";
			//
			// label2
			//
			resources.ApplyResources(label2, "label2");
			label2.Name = "label2";
			//
			// label1
			//
			resources.ApplyResources(label1, "label1");
			label1.Name = "label1";
			//
			// m_txtBoxName
			//
			resources.ApplyResources(this.m_txtBoxName, "m_txtBoxName");
			this.m_txtBoxName.Name = "m_txtBoxName";
			this.m_txtBoxName.TextChanged += new System.EventHandler(this.m_txtBoxName_TextChanged);
			//
			// HeaderFooterModifyDlg
			//
			this.AcceptButton = this.m_btnOK;
			resources.ApplyResources(this, "$this");
			this.CancelButton = m_btnCancel;
			this.Controls.Add(groupBox1);
			this.Controls.Add(this.m_tbControl);
			this.Controls.Add(m_btnHelp);
			this.Controls.Add(this.m_btnOK);
			this.Controls.Add(m_btnCancel);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "HeaderFooterModifyDlg";
			this.ShowInTaskbar = false;
			this.m_tbControl.ResumeLayout(false);
			this.m_tbFirstPage.ResumeLayout(false);
			this.m_pnlTabPage.ResumeLayout(false);
			panel1.ResumeLayout(false);
			groupBox1.ResumeLayout(false);
			groupBox1.PerformLayout();
			this.ResumeLayout(false);

		}
		#endregion

		#region Overridden methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);
			m_hfsvHeader.RootBox.MakeSimpleSel(true, true, false, true);
			m_hfsvFooter.RootBox.MakeSimpleSel(true, true, false, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnClosing(CancelEventArgs e)
		{
			base.OnClosing(e);

			m_hfsvHeader.CloseRootBox();
			m_hfsvFooter.CloseRootBox();
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the text for the left header text label.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private string LeftHeaderText
		{
			get
			{
				if (m_hfsvHeader.PageNumber % 2 == 1)
					return m_pub.IsLeftBound ? m_innerHeaderText : m_outerHeaderText;
				else
					return m_pub.IsLeftBound ? m_outerHeaderText : m_innerHeaderText;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the text for the right header text label.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private string RightHeaderText
		{
			get
			{
				if (m_hfsvHeader.PageNumber % 2 == 1)
					return m_pub.IsLeftBound ? m_outerHeaderText : m_innerHeaderText;
				else
					return m_pub.IsLeftBound ? m_innerHeaderText : m_outerHeaderText;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the text for the left footer text label.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private string LeftFooterText
		{
			get
			{
				if (m_hfsvFooter.PageNumber % 2 == 1)
					return m_pub.IsLeftBound ? m_innerFooterText : m_outerFooterText;
				else
					return m_pub.IsLeftBound ? m_outerFooterText : m_innerFooterText;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the text for the right footer text label.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private string RightFooterText
		{
			get
			{
				if (m_hfsvFooter.PageNumber % 2 == 1)
					return m_pub.IsLeftBound ? m_outerFooterText : m_innerFooterText;
				else
					return m_pub.IsLeftBound ? m_innerFooterText : m_outerFooterText;
			}
		}
		#endregion

		#region Event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Update the dialog when a tab is selected.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void m_tbControl_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			// clear all of the tabs
			m_tbFirstPage.Controls.Clear();
			m_tbEvenPage.Controls.Clear();
			m_tbOddPage.Controls.Clear();
			m_hfsvHeader.PageNumber = m_tbControl.SelectedIndex + 1;
			m_hfsvFooter.PageNumber = m_tbControl.SelectedIndex + 1;

			// Set up the header/footer dialog labels depending on the binding of the publication
			m_lblLeftHeader.Text = LeftHeaderText;
			m_lblRightHeader.Text = RightHeaderText;
			m_lblLeftFooter.Text = LeftFooterText;
			m_lblRightFooter.Text = RightFooterText;

			switch (m_tbControl.SelectedIndex)
			{
				case 0:
					// First page
					m_tbFirstPage.Controls.Add(m_pnlTabPage);
					m_hfsvHeader.Header = m_hvoFirstHeaderSec;
					m_hfsvFooter.Header = m_hvoFirstFooterSec;
					break;
				case 1:
					// Even page
					m_tbEvenPage.Controls.Add(m_pnlTabPage);
					m_hfsvHeader.Header = m_hvoEvenHeaderSec;
					m_hfsvFooter.Header = m_hvoEvenFooterSec;
					break;
				case 2:
					// Odd page
					m_tbOddPage.Controls.Add(m_pnlTabPage);
					m_hfsvHeader.Header = m_hvoDefaultHeaderSec;
					m_hfsvFooter.Header = m_hvoDefaultFooterSec;
					break;
			}
			m_hfsvHeader.PerformLayout();
			m_hfsvFooter.PerformLayout();

			m_hfsvHeader.RootBox.MakeSimpleSel(true, true, false, true);
			m_hfsvFooter.RootBox.MakeSimpleSel(true, true, false, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a click on the "page number" button: add "page number" object replacement
		/// character to the header/footer text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void m_btnPageNumber_Click(object sender, System.EventArgs e)
		{
			AddORCToText(HeaderFooterVc.PageNumberGuid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a click on the "first reference" button: add "first reference" object replacement
		/// character to the header/footer text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void m_btnFirstRef_Click(object sender, System.EventArgs e)
		{
			AddORCToText(HeaderFooterVc.FirstReferenceGuid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a click on the "end reference" button: add "end reference" object replacement
		/// character to the header/footer text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void m_btnEndRef_Click(object sender, System.EventArgs e)
		{
			AddORCToText(HeaderFooterVc.LastReferenceGuid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a click on the "page count" button: add "page count" object replacement
		/// character to the header/footer text representing the total number of pages.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void m_btnPageCount_Click(object sender, System.EventArgs e)
		{
			AddORCToText(HeaderFooterVc.TotalPagesGuid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a click on the "publication title" button: add "publication title" object
		/// replacement character to the header/footer text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void m_btnPubTitle_Click(object sender, System.EventArgs e)
		{
			AddORCToText(HeaderFooterVc.PublicationTitleGuid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a click on the "division name" button: add "division name" object
		/// replacement character to the header/footer text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void m_btnDivName_Click(object sender, System.EventArgs e)
		{
			AddORCToText(HeaderFooterVc.DivisionNameGuid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a click on the "page reference" button: add "page reference" object
		/// replacement character to the header/footer text representing the references on the
		/// page(s).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void m_btnPageRef_Click(object sender, System.EventArgs e)
		{
			AddORCToText(HeaderFooterVc.PageReferenceGuid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a click on the "print date" button: add "print date" object replacement
		/// character to the header/footer text representing the date the document is printed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void m_btnPrintDate_Click(object sender, System.EventArgs e)
		{
			AddORCToText(HeaderFooterVc.PrintDateGuid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a click on the "project name" button: add "project name" object replacement
		/// character to the header/footer text representing the name of the project.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_btnProjectName_Click(object sender, System.EventArgs e)
		{
			AddORCToText(HeaderFooterVc.ProjectNameGuid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a click on the "book name" button: add "book name" object replacement
		/// character to the header/footer text representing the name of the book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_btnBookName_Click(object sender, System.EventArgs e)
		{
			AddORCToText(HeaderFooterVc.BookNameGuid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a click on the "OK" button: Save the settings from this dialog from
		/// secondary cache to the real cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void m_btnOK_Click(object sender, System.EventArgs e)
		{
			m_curHFSet.Name = m_txtBoxName.Text;
			m_curHFSet.Description = m_caches.MainCache.TsStrFactory.MakeString(
				m_txtBoxDescription.Text,
				m_caches.MainCache.WritingSystemFactory.UserWs);
			StoreFromSecToReal();		// Get the data from the secondary cache.
			DialogResult = DialogResult.OK;

			Close();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a click on the "Cancel" button: Ignore changes made in this dialog.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_btnCancel_Click(object sender, System.EventArgs e)
		{
			// Nothing to do, since all modifications are now in either local controls or the
			// secondary cache, both of which disappear automatically without disturbing the
			// state of the universe.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a click on the "Help" button: bring up context-sensitive help for this dialog.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void m_btnHelp_Click(object sender, System.EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpProvider, "khtpModifyHeaderFooterSet");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Stores the last view that had focus.  This allows us to refer to that view when
		/// inserting text into a selection
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void view_LostFocus(object sender, EventArgs e)
		{
			m_lastFocusedView = sender as HFSetView;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle an event when text for the name of the header/footer set is entered.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_txtBoxName_TextChanged(object sender, System.EventArgs e)
		{
			UpdateOKButton();
		}
		#endregion

		#region Private methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load the data from m_curHFSet into the secondary cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void LoadRealDataIntoSec()
		{
			IVwCacheDa cda = (IVwCacheDa) m_caches.DataAccess;

			m_caches.Map(khvoPHFSet, m_curHFSet.Hvo);
			ITsString tssDesc = m_curHFSet.Description;
			cda.CacheStringProp(khvoPHFSet,
				PubHFSetTags.kflidDescription,
				tssDesc);
			string sName = m_curHFSet.Name;
			if (sName == null)
				sName = "";
			cda.CacheUnicodeProp(khvoPHFSet,
				PubHFSetTags.kflidName,
				sName, sName.Length);

			ITsString tssInside;
			ITsString tssCenter;
			ITsString tssOutside;

			m_hvoDefaultHeaderSec = m_caches.FindOrCreateSecAnalysis(
				m_curHFSet.DefaultHeaderOA.Hvo,
				PubHeaderTags.kClassId, khvoPHFSet,
				PubHFSetTags.kflidDefaultHeader,
				"DefaultHeader", kflidDefaultHeaderName);
			tssInside = m_curHFSet.DefaultHeaderOA.InsideAlignedText;
			cda.CacheStringProp(m_hvoDefaultHeaderSec,
				PubHeaderTags.kflidInsideAlignedText,
				tssInside);
			tssCenter = m_curHFSet.DefaultHeaderOA.CenteredText;
			cda.CacheStringProp(m_hvoDefaultHeaderSec,
				PubHeaderTags.kflidCenteredText,
				tssCenter);
			tssOutside = m_curHFSet.DefaultHeaderOA.OutsideAlignedText;
			cda.CacheStringProp(m_hvoDefaultHeaderSec,
				PubHeaderTags.kflidOutsideAlignedText,
				tssOutside);

			m_hvoDefaultFooterSec = m_caches.FindOrCreateSecAnalysis(
				m_curHFSet.DefaultFooterOA.Hvo,
				PubHeaderTags.kClassId,
				khvoPHFSet,
				PubHFSetTags.kflidDefaultFooter,
				"DefaultFooter",
				kflidDefaultFooterName);
			tssInside = m_curHFSet.DefaultFooterOA.InsideAlignedText;
			cda.CacheStringProp(m_hvoDefaultFooterSec,
				PubHeaderTags.kflidInsideAlignedText,
				tssInside);
			tssCenter = m_curHFSet.DefaultFooterOA.CenteredText;
			cda.CacheStringProp(m_hvoDefaultFooterSec,
				PubHeaderTags.kflidCenteredText,
				tssCenter);
			tssOutside = m_curHFSet.DefaultFooterOA.OutsideAlignedText;
			cda.CacheStringProp(m_hvoDefaultFooterSec,
				PubHeaderTags.kflidOutsideAlignedText,
				tssOutside);

			m_hvoFirstHeaderSec = m_caches.FindOrCreateSecAnalysis(m_curHFSet.FirstHeaderOA.Hvo,
				PubHeaderTags.kClassId,
				khvoPHFSet,
				PubHFSetTags.kflidFirstHeader,
				"FirstHeader",
				kflidFirstHeaderName);
			tssInside = m_curHFSet.FirstHeaderOA.InsideAlignedText;
			cda.CacheStringProp(m_hvoFirstHeaderSec,
				PubHeaderTags.kflidInsideAlignedText,
				tssInside);
			tssCenter = m_curHFSet.FirstHeaderOA.CenteredText;
			cda.CacheStringProp(m_hvoFirstHeaderSec,
				PubHeaderTags.kflidCenteredText,
				tssCenter);
			tssOutside = m_curHFSet.FirstHeaderOA.OutsideAlignedText;
			cda.CacheStringProp(m_hvoFirstHeaderSec,
				PubHeaderTags.kflidOutsideAlignedText,
				tssOutside);

			m_hvoFirstFooterSec = m_caches.FindOrCreateSecAnalysis(m_curHFSet.FirstFooterOA.Hvo,
				PubHeaderTags.kClassId,
				khvoPHFSet,
				PubHFSetTags.kflidFirstFooter,
				"FirstFooter",
				kflidFirstFooterName);
			tssInside = m_curHFSet.FirstFooterOA.InsideAlignedText;
			cda.CacheStringProp(m_hvoFirstFooterSec,
				PubHeaderTags.kflidInsideAlignedText,
				tssInside);
			tssCenter = m_curHFSet.FirstFooterOA.CenteredText;
			cda.CacheStringProp(m_hvoFirstFooterSec,
				PubHeaderTags.kflidCenteredText,
				tssCenter);
			tssOutside = m_curHFSet.FirstFooterOA.OutsideAlignedText;
			cda.CacheStringProp(m_hvoFirstFooterSec,
				PubHeaderTags.kflidOutsideAlignedText,
				tssOutside);

			m_hvoEvenHeaderSec = m_caches.FindOrCreateSecAnalysis(m_curHFSet.EvenHeaderOA.Hvo,
				PubHeaderTags.kClassId,
				khvoPHFSet,
				PubHFSetTags.kflidEvenHeader,
				"EvenHeader",
				kflidEvenHeaderName);
			tssInside = m_curHFSet.EvenHeaderOA.InsideAlignedText;
			cda.CacheStringProp(m_hvoEvenHeaderSec,
				PubHeaderTags.kflidInsideAlignedText,
				tssInside);
			tssCenter = m_curHFSet.EvenHeaderOA.CenteredText;
			cda.CacheStringProp(m_hvoEvenHeaderSec,
				PubHeaderTags.kflidCenteredText,
				tssCenter);
			tssOutside = m_curHFSet.EvenHeaderOA.OutsideAlignedText;
			cda.CacheStringProp(m_hvoEvenHeaderSec,
				PubHeaderTags.kflidOutsideAlignedText,
				tssOutside);

			m_hvoEvenFooterSec = m_caches.FindOrCreateSecAnalysis(m_curHFSet.EvenFooterOA.Hvo,
				PubHeaderTags.kClassId,
				khvoPHFSet,
				PubHFSetTags.kflidEvenFooter,
				"EvenFooter",
				kflidEvenFooterName);
			tssInside = m_curHFSet.EvenFooterOA.InsideAlignedText;
			cda.CacheStringProp(m_hvoEvenFooterSec,
				PubHeaderTags.kflidInsideAlignedText,
				tssInside);
			tssCenter = m_curHFSet.EvenFooterOA.CenteredText;
			cda.CacheStringProp(m_hvoEvenFooterSec,
				PubHeaderTags.kflidCenteredText,
				tssCenter);
			tssOutside = m_curHFSet.EvenFooterOA.OutsideAlignedText;
			cda.CacheStringProp(m_hvoEvenFooterSec,
				PubHeaderTags.kflidOutsideAlignedText,
				tssOutside);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Store the data from the secondary (in-memory) cache in the database object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void StoreFromSecToReal()
		{
			ISilDataAccess sda = m_caches.DataAccess;

			// m_curHFSet.Description and m_curHFSet.Name are handled separately, not through
			// the secondary cache.

			m_curHFSet.DefaultHeaderOA.InsideAlignedText =
				sda.get_StringProp(m_hvoDefaultHeaderSec,
				PubHeaderTags.kflidInsideAlignedText);

			m_curHFSet.DefaultHeaderOA.CenteredText =
				sda.get_StringProp(m_hvoDefaultHeaderSec,
				PubHeaderTags.kflidCenteredText);

			m_curHFSet.DefaultHeaderOA.OutsideAlignedText =
				sda.get_StringProp(m_hvoDefaultHeaderSec,
				PubHeaderTags.kflidOutsideAlignedText);

			m_curHFSet.DefaultFooterOA.InsideAlignedText =
				sda.get_StringProp(m_hvoDefaultFooterSec,
				PubHeaderTags.kflidInsideAlignedText);

			m_curHFSet.DefaultFooterOA.CenteredText =
				sda.get_StringProp(m_hvoDefaultFooterSec,
				PubHeaderTags.kflidCenteredText);

			m_curHFSet.DefaultFooterOA.OutsideAlignedText =
				sda.get_StringProp(m_hvoDefaultFooterSec,
				PubHeaderTags.kflidOutsideAlignedText);

			m_curHFSet.FirstHeaderOA.InsideAlignedText =
				sda.get_StringProp(m_hvoFirstHeaderSec,
				PubHeaderTags.kflidInsideAlignedText);

			m_curHFSet.FirstHeaderOA.CenteredText =
				sda.get_StringProp(m_hvoFirstHeaderSec,
				PubHeaderTags.kflidCenteredText);

			m_curHFSet.FirstHeaderOA.OutsideAlignedText =
				sda.get_StringProp(m_hvoFirstHeaderSec,
				PubHeaderTags.kflidOutsideAlignedText);

			m_curHFSet.FirstFooterOA.InsideAlignedText =
				sda.get_StringProp(m_hvoFirstFooterSec,
				PubHeaderTags.kflidInsideAlignedText);

			m_curHFSet.FirstFooterOA.CenteredText =
				sda.get_StringProp(m_hvoFirstFooterSec,
				PubHeaderTags.kflidCenteredText);

			m_curHFSet.FirstFooterOA.OutsideAlignedText =
				sda.get_StringProp(m_hvoFirstFooterSec,
				PubHeaderTags.kflidOutsideAlignedText);

			m_curHFSet.EvenHeaderOA.InsideAlignedText =
				sda.get_StringProp(m_hvoEvenHeaderSec,
				PubHeaderTags.kflidInsideAlignedText);

			m_curHFSet.EvenHeaderOA.CenteredText =
				sda.get_StringProp(m_hvoEvenHeaderSec,
				PubHeaderTags.kflidCenteredText);

			m_curHFSet.EvenHeaderOA.OutsideAlignedText =
				sda.get_StringProp(m_hvoEvenHeaderSec,
				PubHeaderTags.kflidOutsideAlignedText);

			m_curHFSet.EvenFooterOA.InsideAlignedText =
				sda.get_StringProp(m_hvoEvenFooterSec,
				PubHeaderTags.kflidInsideAlignedText);

			m_curHFSet.EvenFooterOA.CenteredText =
				sda.get_StringProp(m_hvoEvenFooterSec,
				PubHeaderTags.kflidCenteredText);

			m_curHFSet.EvenFooterOA.OutsideAlignedText =
				sda.get_StringProp(m_hvoEvenFooterSec,
				PubHeaderTags.kflidOutsideAlignedText);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the ok button's enabled state
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void UpdateOKButton()
		{
			m_btnOK.Enabled = (m_txtBoxName.Text != null && m_txtBoxName.Text != string.Empty);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Changes all the header/footer panels to be a HFSetView with the correct info
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ChangePanelsToViews()
		{
			m_hfsvHeader = ChangePanelToView(m_pnlHeader, m_hvoFirstHeaderSec);
			m_hfsvFooter = ChangePanelToView(m_pnlFooter, m_hvoFirstFooterSec);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Changes a header/footer panel to be a HFSetView with the correct info
		/// </summary>
		/// <param name="pan"></param>
		/// <param name="hvoToDisplay"></param>
		/// ------------------------------------------------------------------------------------
		private HFSetView ChangePanelToView(Panel pan, int hvoToDisplay)
		{
			HFDialogsPageInfo info = new HFDialogsPageInfo(m_pub.IsLeftBound);
			info.PageNumber = 1;
			HFModifyDlgVC vc = new HFModifyDlgVC(info,
				m_caches.MainCache.DefaultVernWs,
				DateTime.Now, m_caches.MainCache);
			HFSetView view = new HFSetView(m_caches.DataAccess, vc, hvoToDisplay);
			view.MakeRoot();
			view.Dock = DockStyle.Fill;
			view.LostFocus += new EventHandler(view_LostFocus);
			pan.Controls.Add(view);
			return view;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates an ORC and places it into the last view that was focused
		/// </summary>
		/// <param name="guid">The Guid for the ORC</param>
		/// ------------------------------------------------------------------------------------
		private void AddORCToText(Guid guid)
		{
			m_lastFocusedView.RootBox.Selection.ReplaceWithTsString(
				TsStringUtils.CreateOrcFromGuid(guid,
				FwObjDataTypes.kodtContextString, m_caches.MainCache.DefaultUserWs));
			m_lastFocusedView.Focus();
		}

		#endregion
	}
	#endregion

	#region HFModifyDlgVC class
	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// This is the view constructor for the views in the Header/Footer setup dialog
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	public class HFModifyDlgVC : HeaderFooterVc
	{
		private ResourceManager m_resources;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new HFSetupDlgVC
		/// </summary>
		/// <param name="page">The PageInfo for use in creating the view</param>
		/// <param name="wsDefault">ID of default writing system</param>
		/// <param name="printDateTime">print date/time</param>
		/// <param name="cache">The cache.</param>
		/// ------------------------------------------------------------------------------------
		public HFModifyDlgVC(IPageInfo page, int wsDefault, DateTime printDateTime, FdoCache cache)
			: base(page, wsDefault, printDateTime, cache)
		{
			m_resources = new ResourceManager(
				"SIL.FieldWorks.Common.PrintLayout.FwPrintLayoutStrings",
				Assembly.GetExecutingAssembly());
		}

		#region Private methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a ITsString with the specified text and a light gray background
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private ITsString CreateTss(string text)
		{
			ITsIncStrBldr strBuilder = TsIncStrBldrClass.Create();
			strBuilder.SetIntPropValues((int)FwTextPropType.ktptWs,
				(int)FwTextPropVar.ktpvDefault, m_cache.DefaultUserWs);
			strBuilder.SetIntPropValues((int)FwTextPropType.ktptBackColor,
				(int)FwTextPropVar.ktpvDefault, (int)ColorUtil.ConvertColorToBGR(Color.LightGray));
			strBuilder.Append(text);
			return strBuilder.GetString();
		}
		#endregion

		#region Overrides of HeaderFooterVc
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
				return CreateTss(m_resources.GetString("kstidLabelPageNumber"));
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
				return CreateTss(m_resources.GetString("kstidLabelFirstReference"));
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
				return CreateTss(m_resources.GetString("kstidLabelLastReference"));
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
				return CreateTss(m_resources.GetString("kstidLabelPublicationTitle"));
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
				return CreateTss(m_resources.GetString("kstidLabelDivisionName"));
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
				return CreateTss(m_resources.GetString("kstidLabelTotalPages"));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a string that represents the "reference" of the contents of the page (e.g.,
		/// in TE, this would be somethinglike "Mark 2,3").
		/// </summary>
		/// <returns>An ITsString with the page reference</returns>
		/// ------------------------------------------------------------------------------------
		public override ITsString PageReference
		{
			get
			{
				return CreateTss(m_resources.GetString("kstidLabelPageReference"));
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
				return CreateTss(m_resources.GetString("kstidLabelPrintDate"));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the project name for the publication
		/// </summary>
		/// <returns>An ITsString with the project name</returns>
		/// ------------------------------------------------------------------------------------
		public override ITsString ProjectName
		{
			get
			{
				return CreateTss(m_resources.GetString("kstidLabelProjectName"));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the book name for the publication
		/// </summary>
		/// <returns>An ITsString with the book name</returns>
		/// ------------------------------------------------------------------------------------
		public override ITsString BookName
		{
			get
			{
				return CreateTss(m_resources.GetString("kstidLabelBookName"));
			}
		}
		#endregion
	}
	#endregion
}

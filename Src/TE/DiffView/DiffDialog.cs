// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DiffDialog.cs
// Responsibility: TE Team

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.UIAdapters;
using SIL.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Resources;
using XCore;
using SIL.FieldWorks.FDO.Infrastructure;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// The "Compare and Merge Saved and Current Versions" dialog
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DiffDialog : Form, IFWDisposable, IxCoreColleague, ISettings, IMessageFilter
	{
		#region Member variables
		private bool m_firstTimeLayout = true;

		private ActiveViewHelper m_viewHelper;
		/// <summary></summary>
		internal DifferenceList m_differences;
		/// <summary></summary>
		internal DiffViewWrapper m_diffViewWrapper;
		/// <summary></summary>
		protected Mediator m_msgMediator;
		/// <summary></summary>
		protected int m_editMark;
		/// <summary></summary>
		protected bool m_editMode;
		/// <summary></summary>
		protected int m_preMarkUndoSeqCount;
		/// <summary></summary>
		protected int m_preMarkRedoSeqCount;
		/// <summary></summary>
		protected int m_hMark;
		/// <summary>
		/// Controls whether to collapse the Undo stack on closing. If we are part of an even larger
		/// collapsable task, we should not do so.
		/// </summary>
		private readonly bool m_fDoCollapseUndo;
		/// <summary></summary>
		protected BookMerger m_bookMerger;
		/// <summary></summary>
		protected ComboBox m_cboZoomPercent = null;
		/// <summary></summary>
		protected FdoCache m_cache;
		private IApp m_app;
		private readonly IHelpTopicProvider m_helpTopicProvider;
		private ITMAdapter m_tmAdapter = null;
		private System.ComponentModel.IContainer components;

		private IScripture m_scr;
		private IVwStylesheet m_stylesheet;
		private SIL.FieldWorks.Common.Controls.Persistence persistence;
		private UndoRedoDropDown m_UndoRedoDropDown;
		private Button btnRevertToOld;
		private Button btnKeepCurrent;
		private Button btnNext;
		private Button btnEdit;
		private Button btnPrev;
		private Label lblCurrentDiffType;
		private Label lblRevDiffType;
		private Label lblDiffCount;
		private TableLayoutPanel tableLayoutPanel; // See OnHandleCreated() for use of this variable
		private Label lblSavedVersion;
		private Label lblCurrent;
		private Label lblReference;
		private bool m_messageFilterInstalled = false;
		internal RegistryFloatSetting ZoomFactorDraft { get; private set; }
		internal RegistryFloatSetting ZoomFactorFootnote { get; private set; }
		#endregion

		#region Construction/Destruction
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="DiffDialog"/> class.
		/// </summary>
		/// <param name="bookMerger">The book merger.</param>
		/// <param name="cache">The cache.</param>
		/// <param name="stylesheet">The stylesheet.</param>
		/// <param name="zoomFactorDraft">The zoom percentage to be used for the "draft" (i.e.,
		/// main Scripture) view</param>
		/// <param name="zoomFactorFootnote">The zoom percentage to be used for the "footnote"
		/// view</param>
		/// <param name="app">The app.</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// ------------------------------------------------------------------------------------
		public DiffDialog(BookMerger bookMerger, FdoCache cache, IVwStylesheet stylesheet,
			float zoomFactorDraft, float zoomFactorFootnote, IApp app, IHelpTopicProvider helpTopicProvider)
			: this(bookMerger, cache, stylesheet, zoomFactorDraft, zoomFactorFootnote, true, app, helpTopicProvider)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="DiffDialog"/> class.
		/// </summary>
		/// <param name="bookMerger">The book merger.</param>
		/// <param name="cache">The cache.</param>
		/// <param name="stylesheet">The stylesheet.</param>
		/// <param name="zoomFactorDraft">The zoom percentage to be used for the "draft" (i.e.,
		/// main Scripture) view</param>
		/// <param name="zoomFactorFootnote">The zoom percentage to be used for the "footnote"
		/// view</param>
		/// <param name="fDoCollapseUndo">true if we want to collapse to a single Undo item on close.</param>
		/// <param name="app">The app.</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// ------------------------------------------------------------------------------------
		public DiffDialog(BookMerger bookMerger, FdoCache cache, IVwStylesheet stylesheet,
			float zoomFactorDraft, float zoomFactorFootnote, bool fDoCollapseUndo, IApp app,
			IHelpTopicProvider helpTopicProvider)
		{
			Debug.Assert(cache != null);
			m_viewHelper = new ActiveViewHelper(this);
			m_fDoCollapseUndo = fDoCollapseUndo;
			m_app = app;
			m_helpTopicProvider = helpTopicProvider;

			// Required for Windows Form Designer support
			InitializeComponent();

			// just as fallback in case the Designer replaced FwContainer with Container
			// in InitializeComponent()...
			if (!(components is FwContainer))
				components = new FwContainer(components);

			// the last column of the table layout manager in the last row should have the
			// width of the scroll bar
			TableLayoutPanel tablePanel = tableLayoutPanel.GetControlFromPosition(0, 3)
				as TableLayoutPanel;
			tablePanel.ColumnStyles[3].Width = SystemInformation.VerticalScrollBarWidth -
				SystemInformation.FixedFrameBorderSize.Width;
			tablePanel = tableLayoutPanel.GetControlFromPosition(1, 3)
							as TableLayoutPanel;
			tablePanel.ColumnStyles[3].Width = SystemInformation.VerticalScrollBarWidth -
				SystemInformation.FixedFrameBorderSize.Width;

			m_msgMediator = new Mediator();
			m_msgMediator.AddColleague(this);
			components.Add(m_msgMediator);

			m_bookMerger = bookMerger;
			m_differences = bookMerger.Differences;
			m_cache = cache;
			m_scr = m_cache.LangProject.TranslatedScriptureOA;
			m_stylesheet = stylesheet;
			ZoomFactorDraft = new RegistryFloatSetting(SettingsKey, "ZoomFactorDiffDialogDraft", zoomFactorDraft);
			ZoomFactorFootnote = new RegistryFloatSetting(SettingsKey, "ZoomFactorDiffDialogFootnote", zoomFactorFootnote);

			// Don't start out in edit mode
			m_editMode = false;

			// If the diff is a comparison of the current against a normal saved version, then
			// change the label text.
			IScrDraft draft = (IScrDraft)m_bookMerger.BookRev.Owner;
			if (draft.Type == ScrDraftType.SavedVersion)
			{
				lblSavedVersion.Text = string.Format(TeDiffViewResources.kstidSavedVersion,
					draft.Description);
			}
			else
				lblSavedVersion.Text = string.Format(lblSavedVersion.Text, draft.Description);

			CreateUndoMark();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Set a mark in the undo stack so individual actions done in the diff dialog can be
		/// undone. Later, when closing the dialog, all actions will be rolled up into a single
		/// undoable action.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected virtual void CreateUndoMark()
		{
			m_preMarkUndoSeqCount = m_cache.ActionHandlerAccessor.UndoableSequenceCount;
			m_preMarkRedoSeqCount = m_cache.ActionHandlerAccessor.RedoableSequenceCount;
			if (m_fDoCollapseUndo)
				m_hMark = m_cache.ActionHandlerAccessor.Mark();
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
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				if (m_messageFilterInstalled)
				{
					Application.RemoveMessageFilter(this);
					m_messageFilterInstalled = false;
				}
				// We don't want to call Controls.Clear() here, because that causes the controls
				// to change size (and fire the events for it...). It causes CreateMenuAndToolbars()
				// to be called, so we end up with two toolbar adapters, but we dispose only one,
				// so eventually it tries to access the already disposed mediator.
				//Controls.Clear();

				// No, since m_bookMerger owns it.
				// if (m_differences != null)
				//	m_differences.Dispose();
				if (m_viewHelper != null)
					m_viewHelper.Dispose();

				if (m_msgMediator != null)
				{
					m_msgMediator.RemoveColleague(this);
					// m_msgMediator gets disposed from calling components.Dispose() below
				}
				if (m_tmAdapter != null)
				{
					m_tmAdapter.InitializeComboItem -= InitializeToolBarCombos;
					m_tmAdapter.Dispose();
				}

				ZoomFactorDraft.Dispose();
				ZoomFactorFootnote.Dispose();

				if(components != null)
				{
					components.Dispose();
				}
			}

			// Deal with unmanaged stuff here.
			m_viewHelper = null;
			m_tmAdapter = null;
			m_differences = null; // Just null it, since m_bookMerger owns it and will dispose it.
			m_msgMediator = null;
			m_bookMerger = null; // Client gave it, so it has to dispose it.
			m_cache = null;
			m_scr = null;
			m_stylesheet = null;

			base.Dispose(disposing);
		}

		#endregion

		#region Toolbar/Menu Setup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Construct the toolbars
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CreateMenuAndToolbars()
		{
			Debug.Assert(m_tmAdapter == null);
			m_tmAdapter = AdapterHelper.CreateTMAdapter();

			// This will always be null when running tests. Otherwise, it will only be null if
			// The UIAdapter.dll couldn't be found... or null for some other reason. :o)
			if (m_tmAdapter == null)
				return;

			// Use this to initialize combo box items.
			m_tmAdapter.InitializeComboItem += InitializeToolBarCombos;

			string[] def = new string[] {FwDirectoryFinder.CodeDirectory +
				"/Translation Editor/Configuration/DiffViewTMDefinition.xml"};
			m_tmAdapter.AllowUpdates = true;
			m_tmAdapter.Initialize(this, m_msgMediator, def);
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
			this.components = new System.ComponentModel.Container();
			System.Windows.Forms.Button btnClose;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DiffDialog));
			System.Windows.Forms.ImageList imageList1;
			System.Windows.Forms.Button btnHelp;
			System.Windows.Forms.Panel pnlCloseHelp;
			System.Windows.Forms.TableLayoutPanel tableLayoutRevision;
			System.Windows.Forms.TableLayoutPanel tableLayoutCurrent;
			this.btnRevertToOld = new System.Windows.Forms.Button();
			this.btnKeepCurrent = new System.Windows.Forms.Button();
			this.btnNext = new System.Windows.Forms.Button();
			this.btnEdit = new System.Windows.Forms.Button();
			this.btnPrev = new System.Windows.Forms.Button();
			this.lblDiffCount = new System.Windows.Forms.Label();
			this.lblRevDiffType = new System.Windows.Forms.Label();
			this.lblCurrentDiffType = new System.Windows.Forms.Label();
			this.lblReference = new System.Windows.Forms.Label();
			this.lblSavedVersion = new System.Windows.Forms.Label();
			this.lblCurrent = new System.Windows.Forms.Label();
			this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.persistence = new SIL.FieldWorks.Common.Controls.Persistence(this.components);
			btnClose = new System.Windows.Forms.Button();
			imageList1 = new System.Windows.Forms.ImageList(this.components);
			btnHelp = new System.Windows.Forms.Button();
			pnlCloseHelp = new System.Windows.Forms.Panel();
			tableLayoutRevision = new System.Windows.Forms.TableLayoutPanel();
			tableLayoutCurrent = new System.Windows.Forms.TableLayoutPanel();
			pnlCloseHelp.SuspendLayout();
			tableLayoutRevision.SuspendLayout();
			tableLayoutCurrent.SuspendLayout();
			this.tableLayoutPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.persistence)).BeginInit();
			this.SuspendLayout();
			//
			// btnClose
			//
			resources.ApplyResources(btnClose, "btnClose");
			btnClose.BackColor = System.Drawing.SystemColors.Control;
			btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			btnClose.ImageList = imageList1;
			btnClose.Name = "btnClose";
			btnClose.UseVisualStyleBackColor = true;
			btnClose.Click += new System.EventHandler(this.btnClose_Click);
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
			//
			// btnHelp
			//
			resources.ApplyResources(btnHelp, "btnHelp");
			btnHelp.BackColor = System.Drawing.SystemColors.Control;
			btnHelp.ImageList = imageList1;
			btnHelp.Name = "btnHelp";
			btnHelp.UseVisualStyleBackColor = true;
			btnHelp.Click += new System.EventHandler(this.btnHelp_Click);
			//
			// btnRevertToOld
			//
			resources.ApplyResources(this.btnRevertToOld, "btnRevertToOld");
			this.btnRevertToOld.ImageList = imageList1;
			this.btnRevertToOld.Name = "btnRevertToOld";
			this.btnRevertToOld.Click += new System.EventHandler(this.btnRevertToOld_Click);
			//
			// btnKeepCurrent
			//
			resources.ApplyResources(this.btnKeepCurrent, "btnKeepCurrent");
			this.btnKeepCurrent.ImageList = imageList1;
			this.btnKeepCurrent.Name = "btnKeepCurrent";
			this.btnKeepCurrent.Click += new System.EventHandler(this.btnKeepCurrent_Click);
			//
			// btnNext
			//
			resources.ApplyResources(this.btnNext, "btnNext");
			this.btnNext.ImageList = imageList1;
			this.btnNext.Name = "btnNext";
			this.btnNext.Click += new System.EventHandler(this.btnNext_Click);
			//
			// btnEdit
			//
			resources.ApplyResources(this.btnEdit, "btnEdit");
			this.btnEdit.BackColor = System.Drawing.SystemColors.Control;
			this.btnEdit.ImageList = imageList1;
			this.btnEdit.Name = "btnEdit";
			this.btnEdit.UseVisualStyleBackColor = true;
			this.btnEdit.Click += new System.EventHandler(this.btnEdit_click);
			//
			// btnPrev
			//
			resources.ApplyResources(this.btnPrev, "btnPrev");
			this.btnPrev.ImageList = imageList1;
			this.btnPrev.Name = "btnPrev";
			this.btnPrev.Click += new System.EventHandler(this.btnPrev_Click);
			//
			// pnlCloseHelp
			//
			tableLayoutCurrent.SetColumnSpan(pnlCloseHelp, 2);
			pnlCloseHelp.Controls.Add(btnClose);
			pnlCloseHelp.Controls.Add(btnHelp);
			resources.ApplyResources(pnlCloseHelp, "pnlCloseHelp");
			pnlCloseHelp.Name = "pnlCloseHelp";
			//
			// tableLayoutRevision
			//
			resources.ApplyResources(tableLayoutRevision, "tableLayoutRevision");
			tableLayoutRevision.Controls.Add(this.lblDiffCount, 0, 2);
			tableLayoutRevision.Controls.Add(this.lblRevDiffType, 0, 0);
			tableLayoutRevision.Controls.Add(this.btnRevertToOld, 2, 1);
			tableLayoutRevision.Controls.Add(this.btnPrev, 0, 1);
			tableLayoutRevision.Name = "tableLayoutRevision";
			//
			// lblDiffCount
			//
			resources.ApplyResources(this.lblDiffCount, "lblDiffCount");
			this.lblDiffCount.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			tableLayoutRevision.SetColumnSpan(this.lblDiffCount, 3);
			this.lblDiffCount.Name = "lblDiffCount";
			//
			// lblRevDiffType
			//
			resources.ApplyResources(this.lblRevDiffType, "lblRevDiffType");
			this.lblRevDiffType.BackColor = System.Drawing.Color.Honeydew;
			this.lblRevDiffType.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			tableLayoutRevision.SetColumnSpan(this.lblRevDiffType, 3);
			this.lblRevDiffType.ForeColor = System.Drawing.SystemColors.InfoText;
			this.lblRevDiffType.MinimumSize = new System.Drawing.Size(0, 27);
			this.lblRevDiffType.Name = "lblRevDiffType";
			this.lblRevDiffType.Paint += new System.Windows.Forms.PaintEventHandler(this.lblRevDiffType_Paint);
			//
			// tableLayoutCurrent
			//
			resources.ApplyResources(tableLayoutCurrent, "tableLayoutCurrent");
			tableLayoutCurrent.Controls.Add(this.lblCurrentDiffType, 0, 0);
			tableLayoutCurrent.Controls.Add(this.btnKeepCurrent, 0, 1);
			tableLayoutCurrent.Controls.Add(this.btnEdit, 1, 1);
			tableLayoutCurrent.Controls.Add(this.btnNext, 2, 1);
			tableLayoutCurrent.Controls.Add(pnlCloseHelp, 1, 2);
			tableLayoutCurrent.Controls.Add(this.lblReference, 0, 2);
			tableLayoutCurrent.Name = "tableLayoutCurrent";
			//
			// lblCurrentDiffType
			//
			resources.ApplyResources(this.lblCurrentDiffType, "lblCurrentDiffType");
			this.lblCurrentDiffType.BackColor = System.Drawing.Color.Honeydew;
			this.lblCurrentDiffType.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			tableLayoutCurrent.SetColumnSpan(this.lblCurrentDiffType, 3);
			this.lblCurrentDiffType.ForeColor = System.Drawing.SystemColors.InfoText;
			this.lblCurrentDiffType.MinimumSize = new System.Drawing.Size(0, 27);
			this.lblCurrentDiffType.Name = "lblCurrentDiffType";
			this.lblCurrentDiffType.Paint += new System.Windows.Forms.PaintEventHandler(this.lblCurrentDiffType_Paint);
			//
			// lblReference
			//
			this.lblReference.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			resources.ApplyResources(this.lblReference, "lblReference");
			this.lblReference.Name = "lblReference";
			//
			// lblSavedVersion
			//
			this.lblSavedVersion.AutoEllipsis = true;
			resources.ApplyResources(this.lblSavedVersion, "lblSavedVersion");
			this.lblSavedVersion.Name = "lblSavedVersion";
			//
			// lblCurrent
			//
			this.lblCurrent.BackColor = System.Drawing.SystemColors.Control;
			resources.ApplyResources(this.lblCurrent, "lblCurrent");
			this.lblCurrent.Name = "lblCurrent";
			//
			// tableLayoutPanel
			//
			resources.ApplyResources(this.tableLayoutPanel, "tableLayoutPanel");
			this.tableLayoutPanel.Controls.Add(this.lblCurrent, 1, 0);
			this.tableLayoutPanel.Controls.Add(this.lblSavedVersion, 0, 0);
			this.tableLayoutPanel.Controls.Add(tableLayoutCurrent, 1, 3);
			this.tableLayoutPanel.Controls.Add(tableLayoutRevision, 0, 3);
			this.tableLayoutPanel.Name = "tableLayoutPanel";
			//
			// persistence
			//
			this.persistence.Parent = this;
			this.persistence.LoadSettings += new SIL.FieldWorks.Common.Controls.Persistence.Settings(this.OnLoadSettings);
			this.persistence.SaveSettings += new SIL.FieldWorks.Common.Controls.Persistence.Settings(this.OnSaveSettings);
			//
			// DiffDialog
			//
			this.AcceptButton = btnClose;
			resources.ApplyResources(this, "$this");
			this.AccessibleRole = System.Windows.Forms.AccessibleRole.Dialog;
			this.CancelButton = btnClose;
			this.Controls.Add(this.tableLayoutPanel);
			this.MinimizeBox = false;
			this.Name = "DiffDialog";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			pnlCloseHelp.ResumeLayout(false);
			tableLayoutRevision.ResumeLayout(false);
			tableLayoutRevision.PerformLayout();
			tableLayoutCurrent.ResumeLayout(false);
			tableLayoutCurrent.PerformLayout();
			this.tableLayoutPanel.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.persistence)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the application.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal IApp App
		{
			get
			{
				CheckDisposed();
				return m_app;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the dialog's active view helper object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ActiveViewHelper ActiveViewHelper
		{
			get
			{
				CheckDisposed();
				return m_viewHelper;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a reference to the zoom combo box on the toolbar.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ComboBox ZoomComboBox
		{
			get
			{
				CheckDisposed();
				return m_cboZoomPercent;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the ActiveViewHelper's active view. This is virtual so tests can override it.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual IRootSite ActiveView
		{
			get
			{
				CheckDisposed();
				return ActiveViewHelper.ActiveView;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get's the dialog's active editing helper object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public EditingHelper ActiveEditingHelper
		{
			get
			{
				CheckDisposed();

				if (m_viewHelper != null &&  m_viewHelper.ActiveView != null)
					return m_viewHelper.ActiveView.EditingHelper;

				return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the value of the Enabled property for the Next button.
		/// </summary>
		/// <remarks>Probably useful only for testing</remarks>
		/// ------------------------------------------------------------------------------------
		public bool NextButtonEnabled
		{
			get
			{
				CheckDisposed();
				return btnNext.Enabled;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the value of the Enabled property for the Previous button.
		/// </summary>
		/// <remarks>Probably useful only for testing</remarks>
		/// ------------------------------------------------------------------------------------
		public bool PreviousButtonEnabled
		{
			get
			{
				CheckDisposed();
				return btnPrev.Enabled;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the value of the Enabled property for the Restore Original button.
		/// </summary>
		/// <remarks>Probably useful only for testing</remarks>
		/// ------------------------------------------------------------------------------------
		public bool RestoreOriginalButtonEnabled
		{
			get
			{
				CheckDisposed();
				return btnRevertToOld.Enabled;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the value of the Enabled property for the Reviewed button.
		/// </summary>
		/// <remarks>Probably useful only for testing</remarks>
		/// ------------------------------------------------------------------------------------
		public bool ReviewedButtonEnabled
		{
			get
			{
				CheckDisposed();
				return btnKeepCurrent.Enabled;
			}
		}
		#endregion

		#region Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Hooks up needed event handlers and sets the mediator for the given view.
		/// </summary>
		/// <param name="view">The view.</param>
		/// ------------------------------------------------------------------------------------
		internal void RegisterView(SimpleRootSite view)
		{
			view.Enter += OnActiveViewChanged;
			view.Mediator = m_msgMediator;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the difference in both views and bring it into view.
		/// </summary>
		/// <param name="diff">difference to seek</param>
		/// ------------------------------------------------------------------------------------
		internal void ScrollToDiff(Difference diff)
		{
			CheckDisposed();

			// Things aren't yet fully initialized, so quit and we'll get back here later.
			if (diff == null ||
				m_diffViewWrapper.CurrentDiffView == null ||
				m_diffViewWrapper.RevisionDiffView == null)
				return;

			if (diff.DiffType == DifferenceType.SectionAddedToCurrent ||
				diff.DiffType == DifferenceType.SectionHeadAddedToCurrent)
			{
				// For Section*AddedToCurrent diff, the Current pane will highlight the
				// entire section (or head), the Revision pane will show a paragraph location.
				IScrSection sectionCurr = diff.GetFirstSection(false);
				m_diffViewWrapper.CurrentDiffView.ScrollToSectionDiff(sectionCurr.IndexInOwner);
				m_diffViewWrapper.RevisionDiffView.ScrollToParaDiff(diff.ParaRev, diff.IchMinRev);
			}
			else if (diff.DiffType == DifferenceType.SectionMissingInCurrent ||
				diff.DiffType == DifferenceType.SectionHeadMissingInCurrent)
			{
				// For Section*MissingInCurrent diff, the Revsion pane will highlight the
				//  entire section.
				IScrSection sectionRev = diff.GetFirstSection(true);
				m_diffViewWrapper.RevisionDiffView.ScrollToSectionDiff(sectionRev.IndexInOwner);
				if (diff.DiffType == DifferenceType.SectionMissingInCurrent)
				{
					// Since the para hvo for the current may have been changed by deleting
					//  sections, we will find a new insert index every time, and scroll to that section
					m_diffViewWrapper.CurrentDiffView.ScrollToSectionDiff(
						m_bookMerger.GetCurrSectionInsertIndex(diff.RefEnd));
				}
				else if (diff.DiffType == DifferenceType.SectionHeadMissingInCurrent)
				{
					// the Current pane will show a paragraph location.
					m_diffViewWrapper.CurrentDiffView.ScrollToParaDiff(diff.ParaCurr, diff.IchMinCurr);
				}
			}
			else
			{
				// Paragraph Diff case.
				// TODO: For ParagraphMissingInCurrent differences: Someday when we can
				// show a paragraph insertion point, we will need to call the book merger to get
				// the correct insertion point because it's possible that the hvoCurr paragraph
				// may have been deleted, e.g. m_bookMerger.GetCurrParaInsertIndex(diff)
				m_diffViewWrapper.CurrentDiffView.ScrollToParaDiff(diff.ParaCurr, diff.IchMinCurr);
				m_diffViewWrapper.RevisionDiffView.ScrollToParaDiff(diff.ParaRev, diff.IchMinRev);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Start editing the current diff view
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void StartEditMode()
		{
			CheckDisposed();

			m_diffViewWrapper.CurrentDiffView.Editable = true;
			m_diffViewWrapper.CurrentDiffView.RefreshDisplay();
			m_diffViewWrapper.CurrentDiffView.Focus();
			if (m_diffViewWrapper.CurrentDiffFootnoteView != null)
			{
				m_diffViewWrapper.CurrentDiffFootnoteView.Editable = true;
				m_diffViewWrapper.CurrentDiffFootnoteView.RefreshDisplay();
			}
			m_editMark = m_cache.ActionHandlerAccessor.Mark();
			// User shouldn't normally see this undo task, but it is required for the undo
			// of all the editing to work.
			using (UndoableUnitOfWorkHelper undoHelper =
					new UndoableUnitOfWorkHelper(m_cache.ActionHandlerAccessor, "Start Edit"))
			{
				m_cache.ActionHandlerAccessor.AddAction(new UndoMajorDifferenceAction(m_bookMerger));
				undoHelper.RollBack = false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// End editing the current diff view
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void EndEditMode()
		{
			CheckDisposed();

			string undo;
			string redo;
			TeResourceHelper.MakeUndoRedoLabels("kstidDiffDlgEditUndoRedo", out undo, out redo);
			m_cache.ActionHandlerAccessor.CollapseToMark(m_editMark, undo, redo);
			m_diffViewWrapper.CurrentDiffView.Editable = false;
			m_diffViewWrapper.CurrentDiffView.RefreshDisplay();
			if (m_diffViewWrapper.CurrentDiffFootnoteView != null)
			{
				m_diffViewWrapper.CurrentDiffFootnoteView.Editable = false;
				m_diffViewWrapper.CurrentDiffFootnoteView.RefreshDisplay();
			}
		}

//		/// -----------------------------------------------------------------------------------
//		/// <summary>
//		/// Use a revision book to find the current book that corresponds to it.
//		/// </summary>
//		/// -----------------------------------------------------------------------------------
//		private IScrBook[] FindCurrentBooksFromRevision(IScrBook[] booksRev)
//		{
//			Debug.Assert(booksRev != null);
//			IScrBook[] booksCurr = new IScrBook[booksRev.Length];
//			for (int i = 0; i < booksRev.Length; i++)
//			{
//				booksCurr[i] = m_scr.FindBook(booksRev[i].CanonicalNum);
//				Debug.Assert(booksCurr[i] != null);
//			}
//			return booksCurr;
//		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void RefreshStatus()
		{
			CheckDisposed();

			// update buttons status
			btnPrev.Enabled = (!m_editMode && m_differences != null &&
				m_differences.IsPrevAvailable);
			btnNext.Enabled = (!m_editMode && m_differences != null &&
				m_differences.Count > 0 && m_differences.IsNextAvailable);
			btnRevertToOld.Enabled = (!m_editMode && m_differences != null &&
				m_differences.CurrentDifference != null);
			btnKeepCurrent.Enabled = (!m_editMode && m_differences != null &&
				m_differences.CurrentDifference != null);

			// update the difference number (we display the index 1-based)
			int curNum = m_differences.CurrentDifferenceIndex + 1;

			lblDiffCount.Text =
				string.Format(TeResourceHelper.GetResourceString("kstidDiffStatusBarMsg"),
				curNum, m_differences.Count);

			// update the the title bar and reference status bar.
			Difference currDiff = m_differences.CurrentDifference;
			if (currDiff == null)
			{
				// When there are no more differences, reset the difference type labels
				// and reference.
				lblCurrentDiffType.Text = string.Empty;
				lblRevDiffType.Text = string.Empty;
				lblReference.Text = string.Empty;
				return;
			}
			IScrBook book = m_scr.FindBook(currDiff.RefStart.Book);
			string reference = ScrReference.MakeReferenceString(book == null ? "?" : book.BestUIName,
				currDiff.RefStart, currDiff.RefEnd, m_scr.ChapterVerseSepr, m_scr.Bridge);
			lblReference.Text = reference;

			// update the text describing the difference types
			bool fCurrIsNewer = (((IScrDraft)m_bookMerger.BookRev.Owner).Type == ScrDraftType.SavedVersion);
			string currentDiffText = CalcLblText(currDiff, true, fCurrIsNewer);
			string revDiffText = CalcLblText(currDiff, false, fCurrIsNewer);

			// if the Current side is described, don't put unknown diff type as the text for the Revision
			if ((currentDiffText.CompareTo(string.Empty) != 0) &&
				!currentDiffText.Contains("Unknown DiffType") &&
				revDiffText.Contains("Unknown DiffType"))
			{
				revDiffText = string.Empty;
			}

			// if the Revision side is described, don't put unknown diff type as the text for the Current
			if ((revDiffText.CompareTo(string.Empty) != 0) &&
				!revDiffText.Contains("Unknown DiffType") &&
				currentDiffText.Contains("Unknown DiffType"))
			{
				currentDiffText = string.Empty;
			}

			lblCurrentDiffType.Text = currentDiffText;
			lblRevDiffType.Text = revDiffText;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calculates the diff type of the label text.
		/// </summary>
		/// <param name="baseDiff">the base difference</param>
		/// <param name="fIsCurr">true if the label is for the Current pane; false if for the
		/// Revision</param>
		/// <param name="fCurrIsNewer">if set to <c>true</c> the Current pane is the newer text
		/// </param>
		/// <returns>the text of the label</returns>
		/// ------------------------------------------------------------------------------------
		internal static string CalcLblText(Difference baseDiff, bool fIsCurr, bool fCurrIsNewer)
		{
			DifferenceType diffType = baseDiff.DiffType;
			IScrTxtPara para = (fIsCurr) ? baseDiff.ParaCurr : baseDiff.ParaRev;

			string labelText = string.Empty;
			if ((diffType & DifferenceType.ParagraphStyleDifference) != 0)
			{
				// paragraph style difference
				ITsTextProps props = para.StyleRules;
				if (props != null)
				{
					labelText = AppendLabel(labelText,
						props.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
				}
				else
					labelText = AppendLabel(labelText, TeDiffViewResources.kstidMissingParaStyle);
			}
			else if ((diffType & DifferenceType.CharStyleDifference) != 0)
			{
				string styleName = (fIsCurr) ? baseDiff.StyleNameCurr : baseDiff.StyleNameRev;
				if (styleName == null)
					styleName = ResourceHelper.DefaultParaCharsStyleName;
				labelText = AppendLabel(labelText, styleName);
			}
			else if ((diffType & DifferenceType.WritingSystemDifference) != 0)
			{
				string wsName = (fIsCurr) ? baseDiff.WsNameCurr : baseDiff.WsNameRev;
				labelText = AppendLabel(labelText, wsName);
			}
			else if ((diffType & DifferenceType.MultipleCharStyleDifferences) != 0)
			{
				labelText = AppendLabel(labelText, TeDiffViewResources.kstidMultipleStyleDiffs);
			}
			else if ((diffType & DifferenceType.MultipleWritingSystemDifferences) != 0)
			{
				labelText = AppendLabel(labelText, TeDiffViewResources.kstidMultipleWritingSystemDiffs);
			}

			// The remaining labels are for "added/deleted/changed" types of differences;
			// for these differences, we put a label ONLY on the pane with the 'newer' text.
			// If we are in the 'older' pane, quit now.
			if (fCurrIsNewer != fIsCurr)
				return labelText;

			DifferenceType compositeSubdiffType = GetCombinedSubDiffType(baseDiff);
			diffType = compositeSubdiffType | baseDiff.DiffType;

			if ((diffType & DifferenceType.TextDifference) != 0)
			{
				// normal text difference; append to the style label, if any
				labelText = AppendLabel(labelText, TeDiffViewResources.kstidTextChanged);
			}

			if ((diffType & DifferenceType.FootnoteDifference) != 0)
			{
				string sFootnoteDiffLabel = string.Empty;
				Debug.Assert(baseDiff.HasORCSubDiffs,
					"Found a footnote subdifference whose owning diff does not have subdiffs for ORCs");
				sFootnoteDiffLabel = GetFootnoteDifferenceText(baseDiff, fIsCurr, fCurrIsNewer); // add info from my ORC subdiffs.

				// footnote differences; append to the style label
				labelText = AppendLabel(labelText, sFootnoteDiffLabel);
			}

			if ((diffType & DifferenceType.FootnoteMissingInCurrent) != 0)
			{
				// Footnote missing in current difference
				labelText = AppendLabel(labelText, fIsCurr ? TeDiffViewResources.kstidFootnoteDeleted
					: TeDiffViewResources.kstidFootnoteAdded);
			}
			if ((diffType & DifferenceType.FootnoteAddedToCurrent) != 0)
			{
				// Footnote missing in revision difference
				labelText = AppendLabel(labelText, fIsCurr ? TeDiffViewResources.kstidFootnoteAdded
					: TeDiffViewResources.kstidFootnoteDeleted);
			}

			if ((diffType & DifferenceType.PictureAddedToCurrent) != 0)
			{
				// Picture missing in current difference
				labelText = AppendLabel(labelText, fIsCurr ? TeDiffViewResources.kstidPictureAdded
					: TeDiffViewResources.kstidPictureMissing);
			}
			if ((diffType & DifferenceType.PictureMissingInCurrent) != 0)
			{
				// Picture missing in current difference
				labelText = AppendLabel(labelText, fIsCurr ? TeDiffViewResources.kstidPictureMissing
					: TeDiffViewResources.kstidPictureAdded);
			}

			if ((diffType & DifferenceType.VerseMissingInCurrent) != 0)
			{
				// Verse missing in current difference
				labelText = fIsCurr ? TeDiffViewResources.kstidVerseDeleted
					: TeDiffViewResources.kstidVerseAdded;
			}
			else if ((diffType & DifferenceType.VerseAddedToCurrent) != 0)
			{
				// Verse missing in revision difference
				labelText = fIsCurr ? TeDiffViewResources.kstidVerseAdded
					: TeDiffViewResources.kstidVerseDeleted;
			}

			if ((diffType & DifferenceType.ParagraphMissingInCurrent) != 0)
			{
				// Paragraph missing in current difference
				labelText = fIsCurr ? TeDiffViewResources.kstidParagraphDeleted
					: TeDiffViewResources.kstidParagraphAdded;
			}
			else if ((diffType & DifferenceType.ParagraphAddedToCurrent) != 0)
			{
				// Paragraph missing in revision difference
				labelText = fIsCurr ? TeDiffViewResources.kstidParagraphAdded
					: TeDiffViewResources.kstidParagraphDeleted;
			}

			if ((diffType & DifferenceType.StanzaBreakMissingInCurrent) != 0)
			{
				// Stanza break missing in current difference
				labelText = fIsCurr ? TeDiffViewResources.kstidStanzaBreakDeleted
					: TeDiffViewResources.kstidStanzaBreakAdded;
			}
			else if ((diffType & DifferenceType.StanzaBreakAddedToCurrent) != 0)
			{
				// Stanza break missing in revision difference
				labelText = fIsCurr ? TeDiffViewResources.kstidStanzaBreakAdded
					: TeDiffViewResources.kstidStanzaBreakDeleted;
			}

			if ((diffType & DifferenceType.SectionAddedToCurrent) != 0)
			{
				if (baseDiff.SectionsCurr.Count() > 1)
					labelText = fIsCurr ? TeDiffViewResources.kstidSectionsAdded
						: TeDiffViewResources.kstidSectionsDeleted;
				else
					labelText = fIsCurr ? TeDiffViewResources.kstidSectionAdded
						: TeDiffViewResources.kstidSectionDeleted;
			}
			else if ((diffType & DifferenceType.SectionMissingInCurrent) != 0)
			{
				if (baseDiff.SectionsRev.Count() > 1)
					labelText = fIsCurr ? TeDiffViewResources.kstidSectionsDeleted
						: TeDiffViewResources.kstidSectionsAdded;
				else
					labelText = fIsCurr ? TeDiffViewResources.kstidSectionDeleted
						: TeDiffViewResources.kstidSectionAdded;
			}

			if ((diffType & DifferenceType.SectionHeadAddedToCurrent) != 0)
			{
				// Section split in Current difference
				labelText = fIsCurr ? TeDiffViewResources.kstidSectionHeadAdded
					: TeDiffViewResources.kstidSectionHeadDeleted;
			}
			else if ((diffType & DifferenceType.SectionHeadMissingInCurrent) != 0)
			{
				// Section merged in Current difference
				labelText = fIsCurr ? TeDiffViewResources.kstidSectionHeadDeleted
					: TeDiffViewResources.kstidSectionHeadAdded;
			}

			if ((diffType & DifferenceType.SectionAddedToCurrent) != 0 && baseDiff.HasParaSubDiffs)
			{
				Debug.Assert(baseDiff.SubDiffsForParas[0].DiffType == DifferenceType.VerseMoved);
				//sMovedDiffLabel = string.Format(TeDiffViewResources.fmt,
				//	CalcLblText((Difference)diff.subDiffs[0], fIsCurr));  "Verse 10 moved"
				string sMovedDiffLabel = baseDiff.SubDiffsForParas.Count == 1 ?
					TeDiffViewResources.kstidVerseMoved : TeDiffViewResources.kstidVersesMoved;
				labelText = AppendLabel(labelText, sMovedDiffLabel);
			}

			if ((diffType & DifferenceType.ParagraphSplitInCurrent) != 0 ||
				(diffType & DifferenceType.ParagraphMergedInCurrent) != 0 ||
				(diffType & DifferenceType.ParagraphStructureChange) != 0)
			{
				if ((diffType & DifferenceType.ParagraphAddedToCurrent) != 0 &&
					(diffType & DifferenceType.ParagraphMissingInCurrent) != 0)
				{
					labelText = AppendLabel(labelText,
						CreateParaStructLabel(baseDiff, fIsCurr, fCurrIsNewer, compositeSubdiffType));
				}
				else
					labelText = CreateParaStructLabel(baseDiff, fIsCurr, fCurrIsNewer, compositeSubdiffType);
			}

			// if we didn't find a difference we know about then just put some text in saying
			// that we don't know about the current difference.
			if (labelText == string.Empty)
				labelText = "Unknown DiffType";
			return labelText;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the text describing the footnote difference.
		/// </summary>
		/// <param name="diff">The diff.</param>
		/// <param name="fIsCurr">if set to <c>true</c> label is for the Current.</param>
		/// <param name="fCurrIsNewer">if set to <c>true</c> the Current pane is the newer text
		/// </param>
		/// <returns>text describing the footnote difference.</returns>
		/// ------------------------------------------------------------------------------------
		private static string GetFootnoteDifferenceText(Difference diff, bool fIsCurr, bool fCurrIsNewer)
		{
			string sFootnoteDiffLabel;
			if (diff.SubDiffsForORCs.Count == 1)
			{
				sFootnoteDiffLabel = string.Format(TeDiffViewResources.kstidFootnoteDifference,
					CalcLblText(diff.SubDiffsForORCs[0], fIsCurr, fCurrIsNewer));
			}
			else
				sFootnoteDiffLabel = TeDiffViewResources.kstidFootnoteDifferences;
			return sFootnoteDiffLabel;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a label for a paragraph structure change difference (including paragraph
		/// merges and splits).
		/// </summary>
		/// <param name="baseDiff">The base difference.</param>
		/// <param name="fIsCurr">if set to <c>true</c> label describes Current; if <c>false</c>
		/// label describes Revision.</param>
		/// <param name="fCurrIsNewer">if set to <c>true</c> if Current is in the newer pane.</param>
		/// <param name="combinedDiffType">Type of the combined subdiffs.</param>
		/// <returns>
		/// a text description of the complex paragraph change/split or merge difference
		/// </returns>
		/// text description of paragraph structure change difference
		/// ------------------------------------------------------------------------------------
		private static string CreateParaStructLabel(Difference baseDiff, bool fIsCurr, bool fCurrIsNewer,
			DifferenceType combinedDiffType)
		{
			// If the difference is not for the side that is more recent...
			if ((fIsCurr && !fCurrIsNewer) || (!fIsCurr && fCurrIsNewer))
				return string.Empty; // we don't want to create a label--return an empty string.

			DifferenceType baseDiffType = baseDiff.DiffType;
			if ((baseDiffType & DifferenceType.ParagraphSplitInCurrent) != 0)
			{
				// Paragraph break added in current
				return ComplexDiffLabel(fIsCurr, combinedDiffType, fIsCurr ?
					TeDiffViewResources.kstidParagraphSplit : TeDiffViewResources.kstidParagraphMerged);
			}
			if ((baseDiffType & DifferenceType.ParagraphMergedInCurrent) != 0)
			{
				// Paragraphs merged in current
				if ((combinedDiffType & DifferenceType.ParagraphStyleDifference) == 0)
				{
					StringBuilder sbCurrCombinedText = new StringBuilder();
					StringBuilder sbRevCombinedText = new StringBuilder();
					foreach (Difference subDiff in baseDiff.SubDiffsForParas)
					{
						if (subDiff.ParaCurr != null)
							sbCurrCombinedText.Append(subDiff.ParaCurr.Contents.Text);
						if (subDiff.ParaRev != null)
							sbRevCombinedText.Append(subDiff.ParaRev.Contents.Text);
					}
					sbCurrCombinedText.Replace(" ", string.Empty);
					sbRevCombinedText.Replace(" ", string.Empty);

					if (sbCurrCombinedText.ToString() == sbRevCombinedText.ToString())
						return fIsCurr ? TeDiffViewResources.kstidParagraphMerged : TeDiffViewResources.kstidParagraphSplit;
				}
				return ComplexDiffLabel(fIsCurr, combinedDiffType, TeDiffViewResources.kstidParagraphStructureChange);
			}
			if ((baseDiffType & DifferenceType.ParagraphStructureChange) != 0)
			{
				return ComplexDiffLabel(fIsCurr, combinedDiffType, TeDiffViewResources.kstidParagraphStructureChange);
			}
			return string.Empty;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a suitable label to describe a complex difference. Sometimes a paragraph
		/// structure change or a split or merge difference contains only certain
		/// types of subdifferences, in which case a better label can be used to describe the
		/// difference as a whole.
		/// </summary>
		/// <param name="fIsCurr">flag indicating whether the label to generate describes the
		/// difference from the perspective of the current version or the saved or imported
		/// version.</param>
		/// <param name="combinedDiffType">The combined difference type of all subdiffs.</param>
		/// <param name="sMinorSubdiffsLabel">The default label to use if no subdiffs are
		/// found that would make this change more complex.</param>
		/// ------------------------------------------------------------------------------------
		private static string ComplexDiffLabel(bool fIsCurr, DifferenceType combinedDiffType,
			string sMinorSubdiffsLabel)
		{
			switch (combinedDiffType)
			{
				case DifferenceType.ParagraphAddedToCurrent:
					return fIsCurr ? TeDiffViewResources.kstidParagraphAdded : TeDiffViewResources.kstidParagraphDeleted;
				case DifferenceType.ParagraphMissingInCurrent:
					return fIsCurr ? TeDiffViewResources.kstidParagraphDeleted : TeDiffViewResources.kstidParagraphAdded;
				case DifferenceType.TextDifference:
					return TeDiffViewResources.kstidParagraphStructureAndTextChanges;
				case DifferenceType.NoDifference:
				case DifferenceType.ParagraphStyleDifference:
				case DifferenceType.ParagraphStyleDifference | DifferenceType.ParagraphMergedInCurrent:
				case DifferenceType.ParagraphStyleDifference | DifferenceType.ParagraphSplitInCurrent:
					return sMinorSubdiffsLabel;
				default:
					return TeDiffViewResources.kstidComplexChanges;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Searches for paragraph added or missing in the sub differences.
		/// </summary>
		/// <param name="diff">The root difference.</param>
		/// <returns>type of the difference if a para added or missing diff is found or
		/// NoDifference</returns>
		/// ------------------------------------------------------------------------------------
		private static DifferenceType GetCombinedSubDiffType(Difference diff)
		{
			DifferenceType type = DifferenceType.NoDifference;
			if (diff.HasParaSubDiffs)
			{
				foreach (Difference subDiff in diff.SubDiffsForParas)
					type |= subDiff.DiffType;
			}
			return type;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Append text to a label. If there is already text existing then separate it
		/// with a comma and some space
		/// </summary>
		/// <param name="label"></param>
		/// <param name="added"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private static string AppendLabel(string label, string added)
		{
			return label == string.Empty ? added : label + ", " + added;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the next difference.
		/// </summary>
		/// <returns>The next difference, or <c>null</c> if none.</returns>
		/// ------------------------------------------------------------------------------------
		internal Difference GetNextDifference()
		{
			CheckDisposed();

			if (!NextButtonEnabled)
				return null;

			// get the next difference that will be returned.
			Difference diff = m_differences.MoveNext();
			RefreshStatus();

			// return the next difference
			return diff;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the previous difference.
		/// </summary>
		/// <returns>The previous difference, or <c>null</c> if none.</returns>
		/// ------------------------------------------------------------------------------------
		internal Difference GetPrevDifference()
		{
			CheckDisposed();

			if (!PreviousButtonEnabled)
				return null;

			// get the previous difference to be returned
			Difference diff = m_differences.MovePrev();
			RefreshStatus();

			// return the previous difference
			return diff;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Recursively look at all the controls belonging to the specified control and close
		/// the root box for controls of type IRootSite. Ideally an IRootSite control will
		/// close its root box in its OnHandleDestroyed event, but sometimes IRootSite controls
		/// are created but never shown, which means their handle is never created, thus
		/// never destroyed.
		/// </summary>
		/// <param name="ctrl"></param>
		/// ------------------------------------------------------------------------------------
		private void CloseRootBoxes(Control ctrl)
		{
			if (ctrl != null && ctrl is IRootSite)
				((IRootSite)ctrl).CloseRootBox();

			foreach (Control childControl in ctrl.Controls)
				CloseRootBoxes(childControl);
		}

		#endregion

		#region Toolbar Mediator Event Handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the Previous Difference toolbar button
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdatePreviousDifference(object args)
		{
			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps != null)
			{
				itemProps.Enabled = (!m_editMode && m_differences != null &&
					m_differences.IsPrevAvailable);
				itemProps.Update = true;
				return true;
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Previous Difference toolbar button press
		/// </summary>
		/// <param name="args"></param>
		/// <returns><c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnPreviousDifference(object args)
		{
			using (new WaitCursor(this))
			{
				Difference oldDiff = m_differences.CurrentDifference;
				Difference newDiff = GetPrevDifference();
				DisplayDifference(oldDiff, newDiff);
				return true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Re-Center (splitter) toolbar button press
		/// </summary>
		/// <param name="args"></param>
		/// <returns><c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnReCenter(object args)
		{
			m_diffViewWrapper.ReCenter();
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the Previous Difference toolbar button
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateNextDifference(object args)
		{
			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps != null)
			{
				itemProps.Enabled = (!m_editMode && m_differences != null &&
					m_differences.Count > 0 && m_differences.IsNextAvailable);
				itemProps.Update = true;
				return true;
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Next Difference toolbar button press
		/// </summary>
		/// <param name="args"></param>
		/// <returns><c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnNextDifference(object args)
		{
			using (new WaitCursor(this))
			{
				Difference oldDiff = m_differences.CurrentDifference;
				Difference newDiff = GetNextDifference();
				DisplayDifference(oldDiff, newDiff);
				return true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Move the difference highlight from the old difference to the new one.
		/// </summary>
		/// <param name="oldDiff"></param>
		/// <param name="newDiff"></param>
		/// ------------------------------------------------------------------------------------
		private void MoveDifferenceHighlight(Difference oldDiff, Difference newDiff)
		{
			// Remove the old highlight from the revision and current panes.
			if (oldDiff != null)
			{
				// Differences which involve changes in paragraph structure do NOT need to
				// notify for the root diff, because that info is repeated in the first subdiff

				// if the old difference doesn't involve changes in paragraph structure...
				if ((oldDiff.DiffType & DifferenceType.ParagraphMergedInCurrent) == 0 &&
					(oldDiff.DiffType & DifferenceType.ParagraphSplitInCurrent) == 0 &&
					(oldDiff.DiffType & DifferenceType.ParagraphStructureChange) == 0)
				{
					// Notify for the root diff
					NotifyOfHighlightChange(oldDiff);
				}

				if (oldDiff.HasORCSubDiffs)
				{
					foreach (Difference subDiff in oldDiff.SubDiffsForORCs)
						NotifyOfHighlightChange(subDiff);
				}
				if (oldDiff.HasParaSubDiffs)
				{
					foreach (Difference subDiff in oldDiff.SubDiffsForParas)
						NotifyOfHighlightChange(subDiff);
				}
			}

			// Highlight the new difference in the revision and current panes.
			if (newDiff != null)
			{
				// if the new difference doesn't involve changes in paragraph structure...
				if ((newDiff.DiffType & DifferenceType.ParagraphMergedInCurrent) == 0 &&
					(newDiff.DiffType & DifferenceType.ParagraphSplitInCurrent) == 0 &&
					(newDiff.DiffType & DifferenceType.ParagraphStructureChange) == 0)
				{
					// Notify for the root diff
					NotifyOfHighlightChange(newDiff);
				}

				if (newDiff.HasORCSubDiffs)
				{
					foreach (Difference subDiff in newDiff.SubDiffsForORCs)
						NotifyOfHighlightChange(subDiff);
				}
				if (newDiff.HasParaSubDiffs)
				{
					foreach (Difference subDiff in newDiff.SubDiffsForParas)
						NotifyOfHighlightChange(subDiff);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Notify the current and revision paragraphs stored in the given difference that the
		/// highlight needs to change.
		/// </summary>
		/// <param name="diff">Either the former or new current difference</param>
		/// ------------------------------------------------------------------------------------
		private void NotifyOfHighlightChange(Difference diff)
		{
			if (diff.SectionsCurr != null)
			{
				foreach (IScrSection section in diff.SectionsCurr)
					NotifySection(section, m_diffViewWrapper.CurrentDiffView.AllRootBoxes());
			}
			if (diff.SectionsRev != null)
			{
				foreach (IScrSection section in diff.SectionsRev)
					NotifySection(section, m_diffViewWrapper.RevisionDiffView.AllRootBoxes());
			}

			if (diff.ParaRev != null)
			{
				if (diff.ParaRev.Owner is IStFootnote)
					NotifyParagraph(diff.ParaRev, m_diffViewWrapper.RevisionDiffFootnoteView.AllRootBoxes());
				else
					NotifyParagraph(diff.ParaRev, m_diffViewWrapper.RevisionDiffView.AllRootBoxes());
			}

			if (diff.ParaCurr != null)
			{
				if (diff.ParaCurr.Owner is IStFootnote)
					NotifyParagraph(diff.ParaCurr, m_diffViewWrapper.CurrentDiffFootnoteView.AllRootBoxes());
				else
					NotifyParagraph(diff.ParaCurr, m_diffViewWrapper.CurrentDiffView.AllRootBoxes());
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Notifies a section that needs to have the highlighting refreshed.
		/// </summary>
		/// <param name="section"></param>
		/// <param name="rootBoxes"></param>
		/// ------------------------------------------------------------------------------------
		private void NotifySection(IScrSection section, List<IVwRootBox> rootBoxes)
		{
			// Section may have been deleted and no longer be valid
			if (section.IsValidObject)
				foreach (var rootBox in rootBoxes)
					rootBox.PropChanged(section.Owner.Hvo, ScrBookTags.kflidSections, section.IndexInOwner, 1, 1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Notifies a paragraph that needs to have its highlighting refreshed.
		/// </summary>
		/// <param name="para"></param>
		/// <param name="rootBoxes"></param>
		/// ------------------------------------------------------------------------------------
		private void NotifyParagraph(IScrTxtPara para, List<IVwRootBox> rootBoxes)
		{
			// Paragraph may have been deleted and no longer be valid
			if (para.IsValidObject)
				foreach (var rootBox in rootBoxes)
					rootBox.PropChanged(para.Hvo, StParaTags.kflidStyleRules, 0, 1, 1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Notifies a paragraph that needs to have its highlighting refreshed.
		/// </summary>
		/// <param name="para"></param>
		/// <param name="rootBox"></param>
		/// ------------------------------------------------------------------------------------
		private void NotifyParagraph(IScrTxtPara para, IVwRootBox rootBox)
		{
			// Paragraph may have been deleted and no longer be valid
			if (para.IsValidObject && rootBox != null)
				rootBox.PropChanged(para.Hvo, StParaTags.kflidStyleRules, 0, 1, 1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the File/Close menu command.
		/// </summary>
		/// <param name="args">Menu item or toolbar item</param>
		/// <returns><c>true</c> because we handled this command</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnFileClose(object args)
		{
			btnClose_Click(this, EventArgs.Empty);
			return true;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Handle the Edit/Copy menu command.
		/// </summary>
		/// <param name="args">The arguments</param>
		/// <returns><c>true</c> if message handled, otherwise <c>false</c>.</returns>
		/// <remarks>Formerly <c>AfVwRootSite::CmdEditCopy1</c></remarks>
		/// -----------------------------------------------------------------------------------
		protected bool OnEditCopy(object args)
		{
			if (DataUpdateMonitor.IsUpdateInProgress())
				return true;

			using (new WaitCursor(this)) // creates a wait cursor and makes it active until the end of the method.
			{
				return ActiveEditingHelper.CopySelection();
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Handle the Edit/Cut menu command.
		/// </summary>
		/// <param name="args">The arguments</param>
		/// <returns><c>true</c> if message handled, otherwise <c>false</c>.</returns>
		/// <remarks>Formerly <c>AfVwRootSite::CmdEditCut1</c></remarks>
		/// -----------------------------------------------------------------------------------
		protected bool OnEditCut(object args)
		{
			if (DataUpdateMonitor.IsUpdateInProgress())
				return false;

			using (new DataUpdateMonitor(this, "EditCut"))
				return ActiveEditingHelper.CutSelection();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Handle the Edit/Paste menu command.
		/// </summary>
		/// <param name="args">The arguments</param>
		/// <returns><c>true</c> if message handled, otherwise <c>false</c>.</returns>
		/// <remarks>Formerly <c>AfVwRootSite::CmdEditPaste1</c></remarks>
		/// -----------------------------------------------------------------------------------
		protected bool OnEditPaste(object args)
		{
			if (DataUpdateMonitor.IsUpdateInProgress())
				return true; //discard this event

			string stUndo, stRedo;
			ResourceHelper.MakeUndoRedoLabels("kstidEditPaste", out stUndo, out stRedo);
			using (UndoableUnitOfWorkHelper undoHelper = new UndoableUnitOfWorkHelper(
				m_cache.ServiceLocator.GetInstance<IActionHandler>(), stUndo, stRedo))
			using (new DataUpdateMonitor(this, "EditPaste"))
			{
				if (ActiveEditingHelper.PasteClipboard())
					undoHelper.RollBack = false;
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Disables/enables the Edit/Undo menu item
		/// </summary>
		/// <param name="args">Toolbar item</param>
		/// <returns><c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		protected virtual bool OnUpdateEditUndo(object args)
		{
			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps != null)
			{
				itemProps.Enabled = m_cache.ActionHandlerAccessor.CanUndo() && !m_editMode &&
					(m_cache.ActionHandlerAccessor.UndoableSequenceCount > m_preMarkUndoSeqCount);

				itemProps.Update = true;
				return true;
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called to enable or disable the Edit/Copy menu item
		/// </summary>
		/// <param name="args">The menu item</param>
		/// <returns><c>true</c> if handled, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateEditCopy(object args)
		{
			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps != null)
			{
				itemProps.Enabled = ActiveEditingHelper != null && ActiveEditingHelper.CanCopy();
				itemProps.Update = true;
				return true;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called to enable or disable the Edit/Cut menu item
		/// </summary>
		/// <param name="args">The menu item</param>
		/// <returns><c>true</c> if handled, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateEditCut(object args)
		{
			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps != null)
			{
				itemProps.Enabled = ActiveEditingHelper != null && ActiveEditingHelper.CanCut();
				itemProps.Update = true;
				return true;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called to enable or disable the Edit/Paste menu item
		/// </summary>
		/// <param name="args">The menu item</param>
		/// <returns><c>true</c> if handled, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateEditPaste(object args)
		{
			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps != null)
			{
				itemProps.Enabled = ActiveEditingHelper != null && ActiveEditingHelper.CanPaste();
				itemProps.Update = true;
				return true;
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This function will undo the last changes done in the diff view.
		/// This function is executed when the user clicks the undo menu item.
		/// </summary>
		/// <param name="args">Unused</param>
		/// <returns><c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		protected virtual bool OnEditUndo(object args)
		{
			if (m_cache.ActionHandlerAccessor.CanUndo() &&
				(m_cache.ActionHandlerAccessor.UndoableSequenceCount > m_preMarkUndoSeqCount))
			{
				if (m_tmAdapter != null)
					m_tmAdapter.HideBarItemsPopup("tbbUndo");

				UndoRedo(true, true);
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This function will drop down the undo list box
		/// </summary>
		/// <param name="args"></param>
		/// <returns><c>true</c> if handled, otherwise <c>false</c></returns>
		/// ------------------------------------------------------------------------------------
		protected virtual bool OnDropDownEditUndo(object args)
		{
			ToolBarPopupInfo popupInfo = args as ToolBarPopupInfo;
			if (popupInfo == null)
				return false;

			m_UndoRedoDropDown = new UndoRedoDropDown(
				ResourceHelper.GetResourceString("kstidUndo1Action"),
				ResourceHelper.GetResourceString("kstidUndoMultipleActions"),
				ResourceHelper.GetResourceString("kstidUndoRedoCancel"));

			for (int i = m_cache.ActionHandlerAccessor.UndoableSequenceCount - 1;
				i >= m_preMarkUndoSeqCount; i--)
			{
				string undoText = m_cache.ActionHandlerAccessor.GetUndoTextN(i);
				Debug.Assert(!String.IsNullOrEmpty(undoText));
				m_UndoRedoDropDown.Actions.Add(undoText.Replace("&", String.Empty));
			}

			m_UndoRedoDropDown.ItemClick +=
				new UndoRedoDropDown.ClickEventHandler(OnUndoDropDownClicked);

			m_UndoRedoDropDown.AdjustHeight();
			popupInfo.Control = m_UndoRedoDropDown;
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Disables/enables the Edit/Redo menu item
		/// </summary>
		/// <param name="args">Toolbar item</param>
		/// <returns><c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateEditRedo(object args)
		{
			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps != null)
			{
				itemProps.Enabled = m_cache.ActionHandlerAccessor.CanRedo() && !m_editMode &&
					m_cache.ActionHandlerAccessor.RedoableSequenceCount > m_preMarkRedoSeqCount;
				itemProps.Update = true;
				return true;
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This function will Redo the last changes done in the diff view.
		/// This function is executed when the user clicks the redo menu item.
		/// </summary>
		/// <param name="args">Unused</param>
		/// <returns><c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnEditRedo(object args)
		{
			if (m_cache.ActionHandlerAccessor.CanRedo() &&
				m_cache.ActionHandlerAccessor.RedoableSequenceCount > m_preMarkRedoSeqCount)
			{
				if (m_tmAdapter != null)
					m_tmAdapter.HideBarItemsPopup("tbbRedo");

				UndoRedo(false, true);
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This function will drop down the redo list box
		/// </summary>
		/// <param name="args"></param>
		/// <returns><c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnDropDownEditRedo(object args)
		{
			ToolBarPopupInfo popupInfo = args as ToolBarPopupInfo;
			if (popupInfo == null)
				return false;

			m_UndoRedoDropDown = new UndoRedoDropDown(
				ResourceHelper.GetResourceString("kstidRedo1Action"),
				ResourceHelper.GetResourceString("kstidRedoMultipleActions"),
				ResourceHelper.GetResourceString("kstidUndoRedoCancel"));

			for (int i = 0; i < m_cache.ActionHandlerAccessor.RedoableSequenceCount; i++)
			{
				string redoText = m_cache.ActionHandlerAccessor.GetRedoTextN(i);
				Debug.Assert(!String.IsNullOrEmpty(redoText));
				m_UndoRedoDropDown.Actions.Add(redoText.Replace("&", String.Empty));
			}

			m_UndoRedoDropDown.ItemClick += OnRedoDropDownClicked;

			m_UndoRedoDropDown.AdjustHeight();
			popupInfo.Control = m_UndoRedoDropDown;
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="fUndo"><c>true</c>to perform an undo, or <c>false</c> to perform
		/// a redo.</param>
		/// <param name="fDoRefresh">True to do a refresh on the view, false otherwise</param>
		/// ------------------------------------------------------------------------------------
		private void UndoRedo(bool fUndo, bool fDoRefresh)
		{
			IActionHandler ah = m_cache.ServiceLocator.GetInstance<IActionHandler>();
			if ((fUndo && ah.CanUndo()) || (!fUndo && ah.CanRedo()))
			{
				// start hour glass
				using(new WaitCursor(this))
				{
					Difference oldDiff = m_differences.CurrentDifference;
					UndoResult ures;
					if (fUndo)
					{
						ures = ah.Undo();

						// Since we did an undo, we need to make the redo button clickable
						m_preMarkRedoSeqCount = 0;
					}
					else
						ures = ah.Redo();

					Debug.Assert(ures == UndoResult.kuresSuccess, "Undo/Redo failed!");

					if (fDoRefresh)
					{
						m_diffViewWrapper.RefreshAllViews();
						MoveDifferenceHighlight(oldDiff, m_differences.CurrentDifference);
						ScrollToDiff(m_differences.CurrentDifference);

						// App should only be null in tests
						if (App != null)
						{
							App.Synchronize(ures == UndoResult.kuresSuccess ?
								SyncMsg.ksyncUndoRedo : SyncMsg.ksyncFullRefresh);
						}

						m_diffViewWrapper.Focus();
						RefreshStatus();
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Recalculate the differences.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void RecalculateDiffs()
		{
			// Clear the old highlight from the revision view before going on
			Difference diff = m_bookMerger.Differences.CurrentDifference;
			if (diff != null)
				NotifyParagraph(diff.ParaRev, m_diffViewWrapper.RevisionDiffView.AllRootBoxes());

			m_bookMerger.RecalculateDifferences(this);

			// Attempt to move to a difference for the verse that was last marked as the
			// current difference.
			if (diff != null)
			{
				Difference newDiff;
				Difference lastDiff = null;
				newDiff = m_bookMerger.Differences.MoveFirst();
				while (newDiff != null)
				{
					lastDiff = newDiff;
					if ((newDiff.RefStart == diff.RefStart && newDiff.ParaRev == diff.ParaRev)
						|| newDiff.RefStart > diff.RefStart)
					{
						// We found a diff that was the same or was close
						break;
					}
					newDiff = m_bookMerger.Differences.MoveNext();
				}
				// If we don't find a diff that has the same reference of a reference that is
				// bigger then just use the diff of the last one we found
				DisplayDifference(null, lastDiff);
			}

			RefreshStatus();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the value in the zoom combo box with the active view's zoom percentage.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateViewZoom(object args)
		{
			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps != null && m_cboZoomPercent != null)
			{
				if (!m_cboZoomPercent.Focused)
				{
					itemProps.Enabled = true;
					m_cboZoomPercent.Enabled = true;
					itemProps.Update = true;
				}

				return true;
			}

			return false;
		}

		#endregion

		#region Zoom handling stuff
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This delegate gets called by the toolbar adapter in order to allow us to initialize
		/// combo boxes. Use it to initialize the zoom and styles combos.
		/// </summary>
		/// <param name="name">Name of toolbar item.</param>
		/// <param name="combo">ComboBox to initialize.</param>
		/// ------------------------------------------------------------------------------------
		private void InitializeToolBarCombos(string name, ComboBox combo)
		{
			Debug.Assert(name == "tbbZoom");
			m_cboZoomPercent = combo;
			m_cboZoomPercent.KeyPress +=
				new System.Windows.Forms.KeyPressEventHandler(this.ZoomKeyPress);
			m_cboZoomPercent.LostFocus +=
				new System.EventHandler(this.ZoomLostFocus);
			m_cboZoomPercent.SelectionChangeCommitted +=
				new System.EventHandler(this.ZoomSelectionChanged);

			m_cboZoomPercent.DropDownStyle = ComboBoxStyle.DropDown;

			m_cboZoomPercent.Items.Add(PercentString(50));
			m_cboZoomPercent.Items.Add(PercentString(75));
			m_cboZoomPercent.Items.Add(PercentString(100));
			m_cboZoomPercent.Items.Add(PercentString(150));
			m_cboZoomPercent.Items.Add(PercentString(200));
			m_cboZoomPercent.Items.Add(PercentString(300));
			m_cboZoomPercent.Items.Add(PercentString(400));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Build a string to represent a percentage (50 -> 50%).
		/// </summary>
		/// <param name="percent">percent value to build a string for</param>
		/// <returns>a string representation of the percentage</returns>
		/// ------------------------------------------------------------------------------------
		private string PercentString(int percent)
		{
			return String.Format(ResourceHelper.GetResourceString("ksPercentage"),
				percent.ToString());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the zoom percentage
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int ZoomPercentage
		{
			get
			{
				CheckDisposed();
				return (int)Math.Round(m_diffViewWrapper.Zoom * 100);
			}
			set
			{
				CheckDisposed();

				float zoomAmt = value / 100.0f;
				m_diffViewWrapper.Zoom = zoomAmt;
				if (m_cboZoomPercent != null)
					m_cboZoomPercent.Text = ZoomString;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the string that should be displayed in the zoom combo box (i.e. has the
		/// zoom percentage number with the following percent sign, or whatever the localized
		/// version of that is).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ZoomString
		{
			get
			{
				CheckDisposed();
				return string.Format(TeDiffViewResources.kstidZoom, ZoomPercentage);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// User has changed the zoom percentage with the drop down list.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">An <see cref="EventArgs"/> that contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void ZoomSelectionChanged(object sender, EventArgs e)
		{
			ZoomChanged();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// User has pressed a key in the zoom percentage control
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">An <see cref="EventArgs"/> that contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void ZoomKeyPress(object sender, KeyPressEventArgs e)
		{
			// Check if the enter key was pressed.
			if (e.KeyChar == '\r')
				ZoomChanged();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The zoom control has lost focus
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">An <see cref="EventArgs"/> that contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void ZoomLostFocus(object sender, EventArgs e)
		{
			ZoomChanged();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void ZoomChanged()
		{
			int newZoomValue;

			using (new WaitCursor(this))
			{
				if (ZoomTextValid(out newZoomValue) && newZoomValue != ZoomPercentage)
				{
					ZoomPercentage = newZoomValue;
					((Control)ActiveView).Focus();
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Validate the zoom text
		/// </summary>
		/// <param name="newVal">The new value if valid; otherwise the previous value</param>
		/// <returns><c>true</c> if valid; otherwise <c>false</c></returns>
		/// ------------------------------------------------------------------------------------
		private bool ZoomTextValid(out int newVal)
		{
			// This should only be null during test running.
			if (m_cboZoomPercent == null)
			{
				newVal = 100;
				return true;
			}

			// If the text is typed from the keyboard then use the Text property. Otherwise
			// use the SelectedItem property since it will reflect the item chosen from the
			// drop-down portion of the combo box.
			string s = (m_cboZoomPercent.SelectedItem == null ?
				m_cboZoomPercent.Text :	((string)m_cboZoomPercent.SelectedItem));

			s = s.Trim(new char[] {' ', '%'});

			try
			{
				newVal = Convert.ToInt32(s);

				// Limit the valid zoom percentage to between 25% and 1000%
				if (newVal < 25)
					newVal = 25;
				if (newVal > 1000)
					newVal = 1000;
			}
			catch
			{
				newVal = ZoomPercentage;
				return false;
			}

			return true;
		}
		#endregion

		#region IMessageFilter implementation
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Filter messages to get keyboard input.  Since the FwTextBox is a panel, it does
		/// not receive keyboard input that also happens to be hot keys.  This filter will
		/// catch all keyboard input and pass it to the root site.
		/// </summary>
		/// <param name="m"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		bool IMessageFilter.PreFilterMessage(ref Message m)
		{
			CheckDisposed();

			if (m.Msg == (int)Win32.WinMsgs.WM_CHAR)
			{
				// don't grab tab characters.
				if (m.WParam == (System.IntPtr)Win32.VirtualKeycodes.VK_TAB)
				{
					SelectNextControl(this.ActiveControl, ModifierKeys != Keys.Shift, true, true, true);
					return true;
				}

				// An ESC key will cause the parent's form to close.
				if (m.WParam == (System.IntPtr)Win32.VirtualKeycodes.VK_ESCAPE)
				{
					if (CancelButton != null)
					{
						CancelButton.PerformClick();
						return true;
					}
				}
			}
			return false;
		}
		#endregion

		#region UndoRedoDropDown Event Delegates
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// User clicked an item in the Undo drop down
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="iClicked">Zero-based index of clicked item</param>
		/// ------------------------------------------------------------------------------------
		private void OnUndoDropDownClicked(object sender, int iClicked)
		{
			if (m_tmAdapter != null)
				m_tmAdapter.HideBarItemsPopup("tbbUndo");

			for (int i = 0; i <= iClicked; i++)
				UndoRedo(true, i == iClicked);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// User clicked an item in the redo drop down
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="iClicked">Zero-based index of clicked item</param>
		/// ------------------------------------------------------------------------------------
		private void OnRedoDropDownClicked(object sender, int iClicked)
		{
			if (m_tmAdapter != null)
				m_tmAdapter.HideBarItemsPopup("tbbRedo");

			for (int i = 0; i <= iClicked; i++)
				UndoRedo(false, i == iClicked);
		}

		#endregion

		#region Overridden Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the Layout event.
		/// </summary>
		/// <param name="levent">The <see cref="T:System.Windows.Forms.LayoutEventArgs"/> instance
		/// containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnLayout(LayoutEventArgs levent)
		{
			// The window's size and location used to be set in the persistence object's
			// EndInit method (which gets called before PerformLayout). However, that was
			// too early because when PerformLayout is called, auto scaling takes place
			// which adjusts the size and position if the form was designed in a different
			// screen DPI from that in which the program is running, thus causing the
			// persisted (in the registry) window size and location to be changed every
			// time the application starts up (See TE-6984).
			if (m_firstTimeLayout && persistence != null &&
				levent.AffectedControl == this && levent.AffectedProperty == "Visible" &&
				!DesignMode)
			{
				persistence.LoadWindowPosition();
				m_firstTimeLayout = false;
			}

			base.OnLayout(levent);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a message filter to handle tab characters
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnActivated(EventArgs e)
		{
			if (!m_messageFilterInstalled && !DesignMode)
			{
				m_messageFilterInstalled = true;
				Application.AddMessageFilter(this);
			}
			base.OnActivated(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remove the message filter when the dialog loses focus
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnLostFocus(EventArgs e)
		{
			if (m_messageFilterInstalled && !DesignMode)
			{
				Application.RemoveMessageFilter(this);
				m_messageFilterInstalled = false;
			}
			base.OnLostFocus(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the SizeChanged event for this dialog.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged (e);

#if __MonoCS__
			if (m_diffViewWrapper == null)
				return;

			m_diffViewWrapper.FixupColumnHeights();
#endif
			RefreshAllHighlighting();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Refreshes all highlighting (revision and current draft diff views, and revision and
		/// current footnote diff views.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void RefreshAllHighlighting()
		{
			if (m_diffViewWrapper == null)
				return;

			// if there is a footnote selection, refresh highlighting and scroll to it.
			// Before we refresh the highlighting, we must save the selection helper because
			// it is cleared when we refresh the highlighting and the selection is not in the view.
			if (m_diffViewWrapper.CurrentDiffFootnoteView != null &&
				m_diffViewWrapper.CurrentDiffFootnoteView.Visible &&
				m_diffViewWrapper.CurrentDiffFootnoteView.EditingHelper.CurrentSelection != null)
			{
				m_diffViewWrapper.CurrentDiffFootnoteView.ScrollSelectionIntoView(m_diffViewWrapper.CurrentDiffFootnoteView.RootBox.Selection,
					VwScrollSelOpts.kssoDefault);
				SelectionHelper selHelper = null;
				if (m_diffViewWrapper.CurrentDiffFootnoteView.EditingHelper.CurrentSelection != null)
					selHelper = new SelectionHelper(m_diffViewWrapper.CurrentDiffFootnoteView.EditingHelper.CurrentSelection);
				RefreshDiffViewHighlighting(m_diffViewWrapper.CurrentDiffFootnoteView.EditingHelper.CurrentSelection);
				if (selHelper != null)
					selHelper.SetSelection(m_diffViewWrapper.CurrentDiffFootnoteView, true, true);
			}
			if (m_diffViewWrapper.RevisionDiffFootnoteView != null &&
				m_diffViewWrapper.RevisionDiffFootnoteView.Visible &&
				m_diffViewWrapper.RevisionDiffFootnoteView.EditingHelper.CurrentSelection != null)
			{
				m_diffViewWrapper.RevisionDiffFootnoteView.ScrollSelectionIntoView(
					m_diffViewWrapper.RevisionDiffFootnoteView.RootBox.Selection,
					VwScrollSelOpts.kssoDefault);
				SelectionHelper selHelper = null;
				if (m_diffViewWrapper.RevisionDiffFootnoteView.EditingHelper.CurrentSelection != null)
					selHelper = new SelectionHelper(m_diffViewWrapper.RevisionDiffFootnoteView.EditingHelper.CurrentSelection);
				RefreshDiffViewHighlighting(m_diffViewWrapper.RevisionDiffFootnoteView.EditingHelper.CurrentSelection);
				if (selHelper != null)
					selHelper.SetSelection(m_diffViewWrapper.RevisionDiffFootnoteView, true, true);
			}

			// Refresh highlighted text when window is resized.
			if (m_diffViewWrapper.CurrentDiffView != null &&
				m_diffViewWrapper.CurrentDiffView.EditingHelper.CurrentSelection != null)
			{
				RefreshDiffViewHighlighting(m_diffViewWrapper.CurrentDiffView.EditingHelper.CurrentSelection);
			}

			if (m_diffViewWrapper.RevisionDiffView != null &&
				m_diffViewWrapper.RevisionDiffView.EditingHelper.CurrentSelection != null)
			{
				RefreshDiffViewHighlighting(m_diffViewWrapper.RevisionDiffView.EditingHelper.CurrentSelection);
			}

			// Scroll selections from draft and revision into view.
			ScrollToDiff(m_differences.CurrentDifference);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Refresh highlighted text in diff view.
		/// </summary>
		/// <param name="selHelper">selection used to get hvo of paragraph to notify for
		/// refreshing diff highlight</param>
		/// ------------------------------------------------------------------------------------
		private void RefreshDiffViewHighlighting(SelectionHelper selHelper)
		{
			if (selHelper != null)
			{
				int paraIndex = selHelper.GetLevelForTag(StTextTags.kflidParagraphs);
				IScrTxtPara para = m_cache.ServiceLocator.GetInstance<IScrTxtParaRepository>()
					.GetObject(selHelper.LevelInfo[paraIndex].hvo);
				NotifyParagraph(para, selHelper.RootSite.RootBox);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When the dialog is created, add the diff views to it.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);

			// Make two Diff views, for Current and Revision
			m_differences.MoveFirst(); // make sure first item is active in list.
			IScrBook bookRev = m_bookMerger.BookRev;
			IScrBook bookCurr = m_bookMerger.BookCurr;

			DiffViewScrProxy currentInfo = new DiffViewScrProxy(this, "Current", bookCurr, false);
			DiffViewScrProxy revisionInfo = new DiffViewScrProxy(this, "Revision", bookRev, true);
			DiffViewFootnoteProxy currFnInfo = new DiffViewFootnoteProxy(this,
				"Current Footnote", bookCurr, false);
			DiffViewFootnoteProxy revisionFnInfo = new DiffViewFootnoteProxy(this,
				"Revision Footnote", bookRev, true);

			// Add ourself to the components to advertise our services
			components.Add(this);

			// NOTE: we use a tableLayoutPanel so that we can add all the other controls
			// like buttons and textboxes in Design mode. The tableLayoutPanel should have
			// exactly the same number of rows and columns as the real SplitGrid should
			// have later on (i.e. 4 rows and 2 columns). The DiffViewWrapper then uses
			// the controls in the first and lost row of the tableLayoutPanel and sticks them
			// in the SplitGrid. The other two middle rows it fills with the DiffView
			// and Footnote view.

			// Make the top draft wrapper to contain the two diff views
			m_diffViewWrapper = new DiffViewWrapper("Diff View Wrapper", this, m_cache,
				m_stylesheet, currentInfo, revisionInfo, currFnInfo, revisionFnInfo, tableLayoutPanel);
			m_diffViewWrapper.Site = Site;
			Controls.Remove(tableLayoutPanel);
			Controls.Add(m_diffViewWrapper);
			Controls.SetChildIndex(m_diffViewWrapper, 0);
			m_diffViewWrapper.Visible = true;

			if (!MiscUtils.RunningTests && !DesignMode)
			{
				CreateMenuAndToolbars();
				m_diffViewWrapper.BringToFront();
			}

#if __MonoCS__
			m_diffViewWrapper.FixupHostedControlVisiblity();
#endif
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the maximum tab index number in the dialog
		/// </summary>
		/// <param name="parent">parent control</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private int MaxTabIndex(Control parent)
		{
			int maxTabIndex = 0;

			if (parent.Controls == null)
				return 0;

			foreach (Control child in parent.Controls)
			{
				// If this child has a larger max, then save it.
				maxTabIndex = Math.Max(child.TabIndex, maxTabIndex);
				// If any of the child windows has a larger max, then save it.
				maxTabIndex = Math.Max(MaxTabIndex(child), maxTabIndex);
			}
			return maxTabIndex;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Form.Load"></see> event.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"></see> that contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			if (!DesignMode)
				RefreshStatus();

			if (m_cboZoomPercent != null)
				m_cboZoomPercent.Text = ZoomString;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Form.Shown"></see> event.
		/// </summary>
		/// <param name="e">A <see cref="T:System.EventArgs"></see> that contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);

			m_diffViewWrapper.ActivateView();
			m_diffViewWrapper.RevisionDiffView.Refresh();

			DisplayFootnoteDifferences(m_differences.CurrentDifference);
			ScrollToDiff(m_differences.CurrentDifference);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the Close button
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
		{
			Application.RemoveMessageFilter(this);

			if (m_differences.Count > 0 && App != null)
			{
				DialogResult result = MessageBox.Show(this,
					string.Format(TeResourceHelper.GetResourceString("kstidExitDiffMsg"),
					m_differences.Count), App.ApplicationName, MessageBoxButtons.YesNo);
				if (result == DialogResult.No)
				{
					e.Cancel = true;
					return;
				}
			}

			// If Edit mode is turned on, then turn it off before exiting or bad things may happen.
			if (m_editMode)
			{
				btnEdit_click(null, null);
				// Filter was getting installed again by btn click processing, just added another
				// remove here.
				Application.RemoveMessageFilter(this);
			}

			// Collapse all the undo tasks created while in the diff dlg. into a single
			// undo task.
			if (m_cache.ActionHandlerAccessor.UndoableSequenceCount > m_preMarkUndoSeqCount && m_fDoCollapseUndo)
			{
				string stUndo, stRedo;
				TeResourceHelper.MakeUndoRedoLabels("kstidMergeDifferences",
					out stUndo, out stRedo);
				m_cache.ActionHandlerAccessor.CollapseToMark(m_hMark, stUndo, stRedo);
			}
			else if (m_fDoCollapseUndo)
			{
				// No tasks were created after the mark so just delete the mark.
				m_cache.ActionHandlerAccessor.DiscardToMark(m_hMark);
			}

			// Save the settings for the toolbar in case the user moved the toolbar around.
			if (m_tmAdapter != null)
				m_tmAdapter.SaveBarSettings();

			base.OnClosing(e);

			// Close the rootboxes when closing, solves the 86 problem.
			CloseRootBoxes(this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the active view changed.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void OnActiveViewChanged(object sender, EventArgs e)
		{
			// The active view changed, so we have to update the displayed zoom value
			if (m_cboZoomPercent != null && sender is FwRootSite)
			{
				m_cboZoomPercent.Text = string.Format(TeDiffViewResources.kstidZoom,
					(int)Math.Round(((FwRootSite)sender).Zoom * 100));
			}
		}

		#endregion

		#region IxCoreColleague Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="mediator"></param>
		/// <param name="configurationParameters"></param>
		/// ------------------------------------------------------------------------------------
		public void Init(Mediator mediator, System.Xml.XmlNode configurationParameters)
		{
			CheckDisposed();

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the possible message targets, i.e. the view(s) we are showing
		/// </summary>
		/// <returns>Message targets</returns>
		/// ------------------------------------------------------------------------------------
		public IxCoreColleague[] GetMessageTargets()
		{
			CheckDisposed();

			// return list of view windows with focused window being the first one
			List<IxCoreColleague> targets = new List<IxCoreColleague>();
			targets.Add(m_diffViewWrapper);
			targets.Add(this);
			return targets.ToArray();
		}

		/// <summary>
		/// Should not be called if disposed.
		/// </summary>
		public bool ShouldNotCall
		{
			get { return IsDisposed; }
		}

		/// <summary>
		/// Mediator message processing priority
		/// </summary>
		public int Priority
		{
			get { return (int)ColleaguePriority.Medium; }
		}

		#endregion

		#region Control Event Delegates
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a click on the Previous button
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void btnPrev_Click(object sender, System.EventArgs e)
		{
			OnPreviousDifference(null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a click on the Next button
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void btnNext_Click(object sender, System.EventArgs e)
		{
			OnNextDifference(null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a click on the Edit button
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void btnEdit_click(object sender, EventArgs e)
		{
			using (new WaitCursor(this))
			{
				// If already in edit mode then go out of edit mode, recalculate the difference list
				// and refresh the curr diff view.
				if (m_editMode)
				{
					RecalculateDiffs();
					EndEditMode();
					btnEdit.Text = (string)btnEdit.Tag;
				}
				// Enter edit mode. Disable all controls so nothing else can be done except editing.
				else
				{
					// Save the original text in the button's tag property. Then change it to
					// say "End Edit" (in English, that is).
					btnEdit.Tag = btnEdit.Text;
					btnEdit.Text = TeResourceHelper.GetResourceString("kstidEndEdit");
					StartEditMode();
				}

				m_editMode = !m_editMode;
				RefreshStatus();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a click on the Revert To Old button
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void btnRevertToOld_Click(object sender, EventArgs e)
		{
			Difference oldDiff = m_differences.CurrentDifference;
			string undo, redo;
			TeResourceHelper.MakeUndoRedoLabels("kstidDiffDlgRevertToOldUndoRedo", out undo, out redo);
			using (UndoableUnitOfWorkHelper undoHelper = new UndoableUnitOfWorkHelper(
				m_cache.ServiceLocator.GetInstance<IActionHandler>(), undo, redo))
			using (new WaitCursor(this))
			{
				RevertToOld();

				undoHelper.RollBack = false;
			}

			// Make sure we update the highlighting after the PropChanges have been fired from FDO
			// so that we don't get conflicting updates trying to update the hightlighting before
			// the PropChanges from FDO have fired. (FWR-2932)
			RefreshStatus();

			// ReplaceCurrentWithRevision removes an item from the difference list.
			// Because of that, the current difference index ends up pointing to the next
			// difference in the list.
			Difference newDiff = m_differences.CurrentDifference;
			DisplayDifference(oldDiff, newDiff);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reverts the current difference to the old revision.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void RevertToOld()
		{
			Difference diff = m_differences.CurrentDifference;
			if (m_bookMerger.IsDataLossDifference(diff) && !AllowUseThisVersionToLoseData())
				return;

			m_diffViewWrapper.CurrentDiffView.SuspendLayout();
			if (m_diffViewWrapper.CurrentDiffFootnoteView != null)
				m_diffViewWrapper.CurrentDiffFootnoteView.SuspendLayout();
			try
			{
				m_cache.ActionHandlerAccessor.AddAction(new UndoMajorDifferenceAction(m_bookMerger));
				m_bookMerger.ReplaceCurrentWithRevision(diff);
			}
			finally
			{
				m_diffViewWrapper.CurrentDiffView.ResumeLayout();
				if (m_diffViewWrapper.CurrentDiffFootnoteView != null)
					m_diffViewWrapper.CurrentDiffFootnoteView.ResumeLayout();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the data loss diff confirmation message and asks the user to confirm whether
		/// to proceed with the action which will result in losing data by deleting what is in
		/// the current version.
		/// </summary>
		/// <returns><c>true</c> if the user confirms desire to delete dat from the current
		/// version; <c>false</c> otherwise.</returns>
		/// <remarks>Virtual to allow overriding for sake of testing</remarks>
		/// ------------------------------------------------------------------------------------
		protected virtual bool AllowUseThisVersionToLoseData()
		{
			return MessageBox.Show(this,
				TeResourceHelper.GetResourceString("kstidConfirmUseThisVersion"),
				TeResourceHelper.GetResourceString("kstidConfirmUseThisVersionCaption"),
				MessageBoxButtons.YesNo) == DialogResult.Yes;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a click on the Keep Current button
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void btnKeepCurrent_Click(object sender, EventArgs e)
		{
			Difference oldDiff = m_differences.CurrentDifference;
			string undo, redo;
			TeResourceHelper.MakeUndoRedoLabels("kstidDiffDlgKeepCurrentUndoRedo", out undo, out redo);
			using(UndoableUnitOfWorkHelper undoHelper = new UndoableUnitOfWorkHelper(
				m_cache.ServiceLocator.GetInstance<IActionHandler>(), undo, redo))
			using (new WaitCursor(this))
			{
				KeepCurrent();

				undoHelper.RollBack = false;
			}

			// Make sure we update the highlighting after the PropChanges have been fired from FDO
			// so that we don't get conflicting updates trying to update the hightlighting before
			// the PropChanges from FDO have fired. (FWR-2932)
			RefreshStatus();

			// MarkDifferenceAsReviewed removes an item from the difference list.
			// Because of that, the current difference index ends up pointing to
			// the next difference in the list.
			Difference diff = m_differences.CurrentDifference;
			DisplayDifference(oldDiff, diff);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Marks the current difference as reviewed
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void KeepCurrent()
		{
			m_cache.ActionHandlerAccessor.AddAction(new UndoMajorDifferenceAction(m_bookMerger));
			// remove current difference from list and advance to the next difference.
			m_bookMerger.MarkDifferenceAsReviewed(m_differences.CurrentDifference);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Close the Diff View dialog.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void btnClose_Click(object sender, System.EventArgs e)
		{
			Close();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Highlight the new difference, display (and highlight) any footnotes that are part of
		/// the difference, and scroll it into view.
		/// </summary>
		/// <param name="diffOld">The previously highlighted difference</param>
		/// <param name="diffNew">The new difference to display as current</param>
		/// ------------------------------------------------------------------------------------
		private void DisplayDifference(Difference diffOld, Difference diffNew)
		{
			if (diffNew != null)
			{
				DisplayFootnoteDifferences(diffNew);
				ScrollToDiff(diffNew);
			}

			MoveDifferenceHighlight(diffOld, diffNew);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// See if the new difference has any footnotes, and if so display the footnote panes
		/// and scroll to the first footnotes.
		/// </summary>
		/// <param name="diff">The new difference to display as current</param>
		/// ------------------------------------------------------------------------------------
		private void DisplayFootnoteDifferences(Difference diff)
		{
			Debug.Assert(diff != null);

			// Determine the first real footnote subdiff for each view, if any
			Difference footnoteDiffRev = m_bookMerger.GetFirstFootnoteDiff(diff, false);
			Difference footnoteDiffCurr = m_bookMerger.GetFirstFootnoteDiff(diff, true);

			// if either view has a sub-diff for a footnote ...
			if (footnoteDiffRev != null || footnoteDiffCurr != null)
			{
				// Show the footnote view, if it isn't already displayed
				m_diffViewWrapper.IsFootnoteRowVisible = true;

				// It may well be that one view has a footnote, the other does not.
				// When not, we want to scroll to a footnote adjacent to where the missing footnote would be.
				// TODO: TE-4610 Fix the following kludge:
				//	If a footnote is missing, this kludge gives both Scroll calls the one existing
				//	footnote diff, which attempts to scroll the other footnote view to
				//	the same footnote index, which is sometimes close to the desired location.
				if (footnoteDiffRev == null)
					footnoteDiffRev = footnoteDiffCurr;
				if (footnoteDiffCurr == null)
					footnoteDiffCurr = footnoteDiffRev;

				// Scroll to the first "subdiff" footnote in the revision
				m_diffViewWrapper.RevisionDiffFootnoteView.ScrollToFootnotePara(footnoteDiffRev);
				// Scroll to the first "subdiff" footnote in the current
				m_diffViewWrapper.CurrentDiffFootnoteView.ScrollToFootnotePara(footnoteDiffCurr);
			}
			else
			{
				// Hide the footnote view, if it is displayed
				if (m_diffViewWrapper.IsFootnoteRowVisible)
				{
					m_diffViewWrapper.IsFootnoteRowVisible = false;
					m_diffViewWrapper.RevisionDiffView.Focus();
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnViewToolBar(object args)
		{
			if (m_tmAdapter == null)
				return false;

			TMBarProperties barProps = m_tmAdapter.GetBarProperties("tbDiffView");

			if (barProps != null)
			{
				if (barProps.Visible)
					m_tmAdapter.HideToolBar("tbDiffView");
				else
					m_tmAdapter.ShowToolBar("tbDiffView");

				return true;
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateViewToolBar(object args)
		{
			if (m_tmAdapter == null)
				return false;

			TMBarProperties barProps = m_tmAdapter.GetBarProperties("tbDiffView");
			TMItemProperties itemProps = args as TMItemProperties;

			if (barProps != null && itemProps != null)
			{
				itemProps.Checked = barProps.Visible;
				itemProps.Update = true;
				return true;
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Save the persistence settings for the diff dialog.
		/// </summary>
		/// <param name="key">location in registry where the settings will be saved</param>
		/// ------------------------------------------------------------------------------------
		private void OnSaveSettings(RegistryKey key)
		{
			if (App == null)
				return; // running tests

			if (m_diffViewWrapper.CurrentDiffView != null)
				ZoomFactorDraft.Value = m_diffViewWrapper.CurrentDiffView.Zoom;

			if (m_diffViewWrapper.CurrentDiffFootnoteView != null)
				ZoomFactorFootnote.Value = m_diffViewWrapper.CurrentDiffFootnoteView.Zoom;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load the persistence settings for the diff dialog.
		/// </summary>
		/// <param name="key">location in registry from which the settings will be loaded</param>
		/// ------------------------------------------------------------------------------------
		private void OnLoadSettings(RegistryKey key)
		{
			// Nothing to do. The registry settings are loaded in the constructor.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Display help for this dialog.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void btnHelp_Click(object sender, System.EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtpArchiveReviewDifferences");
		}
		#endregion

		#region Other event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Paint the current label.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void lblCurrentDiffType_Paint(object sender, PaintEventArgs e)
		{
			DrawDiffTypeLabel((Label)sender, e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Draw the revision label.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void lblRevDiffType_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
		{
			DrawDiffTypeLabel((Label)sender, e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Helps draw the labels.
		/// </summary>
		/// <param name="lbl">the label to be painted</param>
		/// <param name="e">the paint event args</param>
		/// ------------------------------------------------------------------------------------
		private void DrawDiffTypeLabel(Label lbl, PaintEventArgs e)
		{
			e.Graphics.FillRectangle(new SolidBrush(lbl.BackColor), e.ClipRectangle);
			if (m_differences == null || m_differences.CurrentDifference == null)
				return;

			DifferenceType currDiff = m_differences.CurrentDifference.DiffType;

			// Draw a character or paragraph icon if needed
			if ((currDiff & DifferenceType.CharStyleDifference) != 0 ||
				(currDiff & DifferenceType.MultipleCharStyleDifferences) != 0)
			{
				e.Graphics.DrawImage(ResourceHelper.CharStyleIcon, 5, 3);
			}
			else if ((currDiff & DifferenceType.ParagraphStyleDifference) != 0)
				e.Graphics.DrawImage(ResourceHelper.ParaStyleIcon, 5, 3);

			// draw text
			e.Graphics.DrawString(lbl.Text, SystemInformation.MenuFont,
				new SolidBrush(lbl.ForeColor), 22, 4);
		}
		#endregion

		#region ISettings Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SaveSettingsNow()
		{
			CheckDisposed();

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Registry key where user settings are saved.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		public RegistryKey SettingsKey
		{
			get
			{
				CheckDisposed();

				if (App != null)
				{
					if (App.ActiveMainWindow is IFwMainWndSettings)
						return ((IFwMainWndSettings)App.ActiveMainWindow).MainWndSettingsKey;
					return App.SettingsKey;
				}
				return FwRegistryHelper.FieldWorksRegistryKey;
			}
		}

		#endregion
	}
}

// Copyright (c) 2002-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DummyDraftViewForm.cs
// Responsibility: TE Team

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;
using Microsoft.Win32;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO.Infrastructure.Impl;
using SIL.FieldWorks.Resources;
using SIL.Utils;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.TE
{
	#region DummyDraftViewForm
	/// <summary>
	/// Dummy form for a <see cref="DraftView"/>, so that we can create a view
	/// </summary>
	public class DummyDraftViewForm : Form, IFWDisposable, ISettings
	{
		private System.ComponentModel.IContainer components;
		private DummyDraftView m_draftView;
		private SIL.FieldWorks.Common.Controls.Persistence m_Persistence;
		private FdoCache m_cache;
		private FwStyleSheet m_styleSheet;
		private System.Windows.Forms.MainMenu m_mainMenu;
		private System.Windows.Forms.MenuItem menuItem1;
		private System.Windows.Forms.MenuItem m_mnuInsertBook;
		/// <summary>public for testing</summary>
		public System.Windows.Forms.MenuItem m_mnuInsertBookOT;
		/// <summary>public for testing</summary>
		public System.Windows.Forms.MenuItem m_mnuInsertBookNT;
		private IVwRootBox m_rootb;
		// Reset to true, if we make the cache.
		// If we make it, then we dispose it.
		private bool m_isNewCache = false;

		#region Constructor, Dispose, Generated code
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="DummyDraftViewForm"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private DummyDraftViewForm()
		{
			InitializeComponent();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:DummyDraftViewForm"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// ------------------------------------------------------------------------------------
		public DummyDraftViewForm(FdoCache cache): this()
		{
			Cache = cache;
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
		/// -----------------------------------------------------------------------------------
		protected override void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			base.Dispose(fDisposing);

			if (fDisposing)
			{
				if(components != null)
					components.Dispose();
				if (m_draftView != null)
					m_draftView.Dispose();
				if (m_isNewCache && (m_cache != null))
					m_cache.Dispose(); // Only if we made it.
				if (m_Persistence != null)
					m_Persistence.Dispose();
			}
			m_cache = null;
			m_draftView = null;
			m_styleSheet = null;
			m_rootb = null;
			m_Persistence = null;
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
			this.components = new FwContainer();
			this.m_Persistence = new SIL.FieldWorks.Common.Controls.Persistence(this.components);
			this.m_mainMenu = new System.Windows.Forms.MainMenu(this.components);
			this.menuItem1 = new System.Windows.Forms.MenuItem();
			this.m_mnuInsertBook = new System.Windows.Forms.MenuItem();
			this.m_mnuInsertBookOT = new System.Windows.Forms.MenuItem();
			this.m_mnuInsertBookNT = new System.Windows.Forms.MenuItem();
			((System.ComponentModel.ISupportInitialize)(this.m_Persistence)).BeginInit();
			this.SuspendLayout();
			//
			// m_Persistence
			//
			this.m_Persistence.Parent = this;
			//
			// m_mainMenu
			//
			this.m_mainMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
			this.menuItem1});
			//
			// menuItem1
			//
			this.menuItem1.Index = 0;
			this.menuItem1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
			this.m_mnuInsertBook});
			this.menuItem1.Text = "Insert";
			//
			// m_mnuInsertBook
			//
			this.m_mnuInsertBook.Index = 0;
			this.m_mnuInsertBook.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
			this.m_mnuInsertBookOT,
			this.m_mnuInsertBookNT});
			this.m_mnuInsertBook.Text = "Book";
			//
			// m_mnuInsertBookOT
			//
			this.m_mnuInsertBookOT.Index = 0;
			this.m_mnuInsertBookOT.Text = "Old Testament";
			//
			// m_mnuInsertBookNT
			//
			this.m_mnuInsertBookNT.Index = 1;
			this.m_mnuInsertBookNT.Text = "New Testament";
			//
			// DummyDraftViewForm
			//
			this.AutoScaleMode = AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(292, 273);
			this.Menu = this.m_mainMenu;
			this.Name = "DummyDraftViewForm";
			this.Text = "DummyDraftViewForm";
			((System.ComponentModel.ISupportInitialize)(this.m_Persistence)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion

		#region ISettings methods
		///-------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the parent's SettingsKey if parent implements ISettings, otherwise null.
		/// </summary>
		///-------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "We're returning an object")]
		public RegistryKey SettingsKey
		{
			get
			{
				CheckDisposed();
				using (var regKey = FwRegistryHelper.FieldWorksRegistryKey)
				{
					return regKey.CreateSubKey("DraftViewTest");
				}
			}
		}

		///-------------------------------------------------------------------------------------
		/// <summary>
		/// Save the persisted settings now.
		/// </summary>
		///-------------------------------------------------------------------------------------
		public void SaveSettingsNow()
		{
			CheckDisposed();

			m_Persistence.SaveSettingsNow(this);
		}

		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Allows setting the rootbox on the draft view so that it can be mocked.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IVwRootBox RootBox
		{
			get
			{
				CheckDisposed();
				return m_rootb;
			}
			set
			{
				CheckDisposed();
				m_rootb = value;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets and Sets the Fdo cache
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public FdoCache Cache
		{
			get
			{
				CheckDisposed();

				return m_cache;
			}
			set
			{
				CheckDisposed();

				m_cache = value;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets the draft view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public DummyDraftView DraftView
		{
			get
			{
				CheckDisposed();
				return m_draftView;
			}
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Allows mocking the style sheet
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwStyleSheet StyleSheet
		{
			get
			{
				CheckDisposed();

				if (m_styleSheet == null)
				{
					m_styleSheet = new FwStyleSheet();

					ILangProject lgproj = Cache.LangProject;
					IScripture scripture = lgproj.TranslatedScriptureOA;
					m_styleSheet.Init(Cache, scripture.Hvo, ScriptureTags.kflidStyles, ResourceHelper.DefaultParaCharsStyleName);
				}
				return m_styleSheet;
			}
			set { m_styleSheet = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CreateDraftView()
		{
			CheckDisposed();

			CreateDraftView(Cache);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Creates and opens a draft view in a form. Loads scripture from the DB.
		/// </summary>
		/// <param name="cache">The cache, believe it or not</param>
		/// -----------------------------------------------------------------------------------
		public void CreateDraftView(FdoCache cache)
		{
			CheckDisposed();

			CreateDraftView(cache, false);
		}
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Creates and opens a draft view in a form. Loads scripture from the DB.
		/// </summary>
		/// <param name="cache">The cache, believe it or not</param>
		/// <param name="fIsBackTrans"><c>true</c> if the draft view is supposed to represent
		/// a BT</param>
		/// -----------------------------------------------------------------------------------
		public void CreateDraftView(FdoCache cache, bool fIsBackTrans)
		{
			CheckDisposed();

			CreateDraftView(cache, fIsBackTrans, true);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Creates and opens a draft view in a form. Loads scripture from the DB.
		/// </summary>
		/// <param name="cache">The cache, believe it or not</param>
		/// <param name="fIsBackTrans"><c>true</c> if the draft view is supposed to represent
		/// a BT</param>
		/// <param name="fMakeRoot"><c>true</c> if the root should be constructed; <c>false</c>
		/// if the caller will be responsible for making the rootbox later.</param>
		/// -----------------------------------------------------------------------------------
		public void CreateDraftView(FdoCache cache, bool fIsBackTrans, bool fMakeRoot)
		{
			CheckDisposed();

			Cache = cache;
			m_draftView = CreateDummyDraftView(fIsBackTrans, fMakeRoot);
			Controls.Add(m_draftView);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Creates 2 sync'd draftviews in a form. Loads scripture from the DB.
		/// </summary>
		/// <param name="cache">The cache, believe it or not</param>
		/// -----------------------------------------------------------------------------------
		public RootSiteGroup CreateSyncDraftView(FdoCache cache)
		{
			CheckDisposed();

			Cache = cache;
			RootSiteGroup group = new RootSiteGroup(cache, (int)TeViewGroup.Scripture);

			DummyDraftView draft1 = CreateDummyDraftView(false, false);
			m_draftView = CreateDummyDraftView(false, false);
			group.AddToSyncGroup(draft1);
			group.AddToSyncGroup(m_draftView);
			group.ScrollingController = m_draftView;

			draft1.MakeRoot();
			draft1.Visible = true;
			m_draftView.MakeRoot();
			m_draftView.Visible = true;

			Controls.Add(group);
			group.Show();
			return group;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the dummy draft view.
		/// </summary>
		/// <param name="fForBT"><c>true</c> if the draft view is supposed to represent a BT
		/// </param>
		/// <param name="fMakeRoot"><c>true</c> if the root should be constructed; <c>false</c>
		/// if the caller will be responsible for making the rootbox later.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private DummyDraftView CreateDummyDraftView(bool fForBT, bool fMakeRoot)
		{
			DummyDraftView draftView = new DummyDraftView(Cache, fForBT, Handle.ToInt32());
			draftView.Anchor = AnchorStyles.Top | AnchorStyles.Left |
				AnchorStyles.Right | AnchorStyles.Bottom;
			draftView.Dock = DockStyle.Fill;
			draftView.Name = "draftView";
			draftView.StyleSheet = StyleSheet;
			if (m_rootb != null)
				draftView.RootBox = m_rootb;

			if (fMakeRoot)
			{
				draftView.MakeRoot();
				draftView.Visible = true;
				draftView.ActivateView();
			}

			return draftView;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Delete the registry subkey to allow for clean test
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public void DeleteRegistryKey()
		{
			CheckDisposed();

			try
			{
				FwRegistryHelper.FieldWorksRegistryKey.DeleteSubKeyTree("DraftViewTest");
			}
			catch
			{
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Scroll the DraftView to the end.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public void ScrollToEnd()
		{
			CheckDisposed();

			m_draftView.GoToEnd();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get visibility of IP
		/// </summary>
		/// <returns>Returns <c>true</c> if selection is visible</returns>
		/// -----------------------------------------------------------------------------------
		public bool IsSelectionVisible()
		{
			CheckDisposed();

			return m_draftView.IsSelectionVisible(null);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get the Y positon off the draft view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public int YPosition
		{
			get
			{
				CheckDisposed();

				return m_draftView.AutoScrollPosition.Y;
			}
		}
	}
	#endregion

	#region DummyDraftViewVC
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyDraftViewVC : DraftViewVc
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the DraftViewVc class
		/// </summary>
		/// <param name="target">target of the view (printer or draft)</param>
		/// <param name="filterInstance">Number used to make filters unique for each main
		/// window</param>
		/// <param name="styleSheet">Optional stylesheet. Null is okay if this view constructor
		/// promises never to try to display a back translation</param>
		/// <param name="fShowInTable">True to show in table, otherwise false</param>
		/// ------------------------------------------------------------------------------------
		public DummyDraftViewVC(TeStVc.LayoutViewTarget target, int filterInstance,
			IVwStylesheet styleSheet, bool fShowInTable)
			: base(target, filterInstance, styleSheet, fShowInTable)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="rghvo"></param>
		/// <param name="chvo"></param>
		/// <param name="hvoParent"></param>
		/// <param name="tag"></param>
		/// <param name="frag"></param>
		/// <param name="ihvoMin"></param>
		/// ------------------------------------------------------------------------------------
		public override void LoadDataFor(IVwEnv vwenv, int[] rghvo, int chvo, int hvoParent,
			int tag, int frag, int ihvoMin)
		{
			CheckDisposed();

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether to display the text in a table.
		/// </summary>
		/// <value><c>true</c> to display text in a table; otherwise, <c>false</c>.</value>
		/// ------------------------------------------------------------------------------------
		public bool DisplayInTable
		{
			get
			{
				CheckDisposed();
				return m_fDisplayInTable;
			}
			set
			{
				CheckDisposed();
				m_fDisplayInTable = value;
			}
		}
	}
	#endregion
}

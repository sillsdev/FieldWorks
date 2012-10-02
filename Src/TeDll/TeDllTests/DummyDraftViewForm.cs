// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2002, SIL International. All Rights Reserved.
// <copyright from='2002' to='2002' company='SIL International'>
//		Copyright (c) 2002, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DummyDraftViewForm.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Reflection;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.TE
{
	#region DummyDraftViewForm
	/// <summary>
	/// Dummy form for a <see cref="DraftView"/>, so that we can create a view
	/// </summary>
	public class DummyDraftViewForm : Form, IFWDisposable, ISettings, IControlCreator
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
		public DummyDraftViewForm()
		{
			InitializeComponent();

			ParagraphCounterManager.ParagraphCounterType = typeof(TeParaCounter);
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
		protected override void Dispose( bool fDisposing )
		{
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
			this.m_Persistence.DefaultKeyPath = "Software\\SIL\\FieldWorks\\DraftViewTest";
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
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
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
		public Microsoft.Win32.RegistryKey SettingsKey
		{
			get
			{
				CheckDisposed();

				return Registry.CurrentUser.CreateSubKey(m_Persistence.DefaultKeyPath);
			}
		}

		///***********************************************************************************
		/// <summary>
		/// Gets a window creation option.
		/// </summary>
		/// <value>By default, returns false</value>
		///***********************************************************************************
		[Browsable(false)]
		public bool KeepWindowSizePos
		{
			get
			{
				CheckDisposed();

				return false;
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

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get the server and name to be used for establishing a database connection. First
		/// try to get this info based on the command-line arguments. If not given, try using
		/// the info in the registry. Otherwise, just get the first FW database on the local
		/// server.
		/// </summary>
		/// <returns>A new FdoCache, based on options, or null, if not found.</returns>
		/// <remarks>This method was originally taken from FwApp.cs</remarks>
		/// -----------------------------------------------------------------------------------
		private FdoCache GetCache()
		{
			m_isNewCache = false;
			if (Cache == null)
			{
				Dictionary<string, string> cacheOptions = new Dictionary<string, string>();
				cacheOptions.Add("c", MiscUtils.LocalServerName);
				cacheOptions.Add("db", "TestLangProj");
				FdoCache cache = null;
				cache = FdoCache.Create(cacheOptions);
				// Make sure we don't call InstallLanguage during tests.
				cache.LanguageWritingSystemFactoryAccessor.BypassInstall = true;
				m_isNewCache = true;
				return cache;
			}
			else
				return Cache;
		}

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
					m_styleSheet.Init(Cache, scripture.Hvo,
						(int)Scripture.ScriptureTags.kflidStyles);
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

			CreateDraftView(GetCache());
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

			ScriptureChangeWatcher.Create(m_cache);
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

			ScriptureChangeWatcher.Create(m_cache);
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
			int c = draftView.BookFilter.BookCount;	// make sure the book filter gets created now.
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

			draftView.TeEditingHelper.InTestMode = true;	// turn off some processing for tests
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
				RegistryKey key = Registry.CurrentUser.CreateSubKey("Software\\SIL\\FieldWorks");
				key.DeleteSubKeyTree("DraftViewTest");
			}
			catch(Exception)
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

		#region IControlCreator Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the control based on the create info.
		/// </summary>
		/// <param name="sender">The caller.</param>
		/// <param name="createInfo">The create info previously specified by the client.</param>
		/// <returns>The newly created control.</returns>
		/// ------------------------------------------------------------------------------------
		Control IControlCreator.Create(object sender, object createInfo)
		{
			if (createInfo is DraftViewCreateInfo || createInfo is ChecksDraftViewCreateInfo)
			{
				bool fBackTrans = false;
				string name;
				bool fEditable;
				bool fKeyTermsDraftView = (createInfo is ChecksDraftViewCreateInfo);
				if (fKeyTermsDraftView)
				{
					ChecksDraftViewCreateInfo keyInfo = (ChecksDraftViewCreateInfo)createInfo;
					name = keyInfo.Name;
					fEditable = keyInfo.IsEditable;
				}
				else
				{
					DraftViewCreateInfo info = (DraftViewCreateInfo)createInfo;
					fBackTrans = info.IsBackTrans;
					fEditable = info.IsEditable;
					name = info.Name;
				}

				DummyDraftView draftView = new DummyDraftView(Cache, fBackTrans, Handle.ToInt32());
				draftView.Name = name;
				draftView.Editable = fEditable;
				draftView.TheViewWrapper = sender as ViewWrapper;
				int c = draftView.BookFilter.BookCount;	// make sure the book filter gets created now.

				if (m_rootb != null)
					draftView.RootBox = m_rootb;
				draftView.TeEditingHelper.InTestMode = true;	// turn off some processing for tests
				m_draftView = draftView;
				return draftView;
			}
			else if (createInfo is KeyTermRenderingsCreateInfo)
			{
				KeyTermRenderingsControl ktRenderingsCtrl = new KeyTermRenderingsControl(m_cache, null);
				ktRenderingsCtrl.Parent = this;
				return ktRenderingsCtrl;
			}

			return null;
		}

		#endregion
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

	#region DummyDraftView
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Dummy <see cref="DraftView"/> for testing purposes that allows accessing protected
	/// members.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyDraftView: DraftView
	{
		private class DummyGraphicsManager : GraphicsManager
		{
			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="T:DummyGraphicsManager"/> class.
			/// </summary>
			/// <param name="parent">The parent.</param>
			/// --------------------------------------------------------------------------------
			public DummyGraphicsManager(Control parent): base(parent)
			{
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Sets the VwGraphics object.
			/// </summary>
			/// <param name="vwGraphics">The VwGraphics object.</param>
			/// --------------------------------------------------------------------------------
			public void SetVwGraphics(IVwGraphicsWin32 vwGraphics)
			{
				m_vwGraphics = vwGraphics;
			}
		}

		private EditingHelper m_EditingHelperForTesting;
		private DraftViewVc m_draftViewVcForTesting;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Default constructor
		/// </summary>
		/// <param name="cache">The cache</param>
		/// <param name="fIsBackTrans"><c>true</c> if the draft view is supposed to represent
		/// a BT</param>
		/// <param name="filterInstance"></param>
		/// ------------------------------------------------------------------------------------
		public DummyDraftView(FdoCache cache, bool fIsBackTrans, int filterInstance) :
			base(cache, fIsBackTrans, fIsBackTrans, false, false,
				TeViewType.DraftView | (fIsBackTrans ? TeViewType.BackTranslation : TeViewType.Scripture),
			filterInstance)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the graphics manager.
		/// </summary>
		/// <returns>A new graphics manager.</returns>
		/// <remarks>We do this in a method for testing.</remarks>
		/// ------------------------------------------------------------------------------------
		protected override GraphicsManager CreateGraphicsManager()
		{
			return new DummyGraphicsManager(this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the FDO cache
		/// </summary>
		/// <value>A <see cref="T:SIL.FieldWorks.FDO.FdoCache"/></value>
		/// ------------------------------------------------------------------------------------
		public override FdoCache Cache
		{
			get { return base.Cache; }
			set
			{
				base.Cache = value;

				// add virtual bookfilter property to the cache
				if (value != null &&
					FilteredScrBooks.GetFilterInstance(value, FilterInstance) == null)
				{
					new FilteredScrBooks(value, FilterInstance);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override DraftViewVc CreateDraftViewVC()
		{
			CheckDisposed();

			if (m_draftViewVcForTesting != null)
				return m_draftViewVcForTesting;

			return new DummyDraftViewVC(TeStVc.LayoutViewTarget.targetDraft, FilterInstance,
				m_styleSheet, m_fShowInTable);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets a value indicating whether this instance is back translation.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is back translation; otherwise, <c>false</c>.
		/// </value>
		/// ------------------------------------------------------------------------------------
		public bool IsBackTranslation
		{
			set
			{
				CheckDisposed();
				m_contentType = value ? StVc.ContentTypes.kctSimpleBT : StVc.ContentTypes.kctNormal;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the view constructor.
		/// </summary>
		/// <value>The view constructor.</value>
		/// ------------------------------------------------------------------------------------
		public DraftViewVc ViewConstructor
		{
			get
			{
				CheckDisposed();
				return m_draftViewVc;
			}
			set
			{
				CheckDisposed();
				m_draftViewVc = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the view constructor for testing.
		/// </summary>
		/// <value>The view constructor for testing.</value>
		/// ------------------------------------------------------------------------------------
		public DraftViewVc ViewConstructorForTesting
		{
			set
			{
				CheckDisposed();
				m_draftViewVcForTesting = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes <see cref="SimpleRootSite.m_rootb"/> for testing. This allows mocking the
		/// rootbox.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public new IVwRootBox RootBox
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes <see cref="GraphicsManager.VwGraphics"/> for testing. This allows mocking the
		/// graphics object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IVwGraphics Graphics
		{
			get
			{
				CheckDisposed();
				return m_graphicsManager.VwGraphics;
			}
			set
			{
				CheckDisposed();
				((DummyGraphicsManager)m_graphicsManager).SetVwGraphics(value as IVwGraphicsWin32);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calls the OnLayout method which usually gets called when the window is shown.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CallOnLayout()
		{
			CheckDisposed();

			m_dxdLayoutWidth = kForceLayout;
			OnLayout(new LayoutEventArgs(this, ""));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void TurnOnHeightEstimator()
		{
			CheckDisposed();

			PropertyInfo heightEstimator = m_draftViewVc.GetType().GetProperty("HeightEstimator");
			heightEstimator.SetValue(m_draftViewVc, this, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes <see cref="SIL.FieldWorks.TE.TeEditingHelper.HandleMouseDown()"/> for
		/// testing Insert Verse Numbers mode.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void InsertVerseNumber()
		{
			CheckDisposed();
			TeEditingHelper.HandleMouseDown();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes <see cref="TeEditingHelper.InsertVerseNumber(SIL.FieldWorks.Common.RootSites.SelectionHelper)"/>
		/// for testing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void InsertVerseNumber(SelectionHelper sel)
		{
			CheckDisposed();

			TeEditingHelper.InsertVerseNumber(sel);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes <see cref="TeEditingHelper.InsertChapterNumber"/> for testing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void InsertChapterNumber()
		{
			CheckDisposed();

			TeEditingHelper.InsertChapterNumber();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Navigate to the beginning of any Scripture element: Title, Section Head, or Section
		/// Content.
		/// </summary>
		/// <param name="tag">Indicates whether selection should be made in the title, section
		/// Heading or section Content</param>
		/// <param name="book">The 0-based index of the Scripture book in which to put the
		/// insertion point</param>
		/// <param name="section">The 0-based index of the Scripture section in which to put the
		/// insertion point. Ignored if tag is <see cref="ScrBook.ScrBookTags.kflidTitle"/>
		/// </param>
		/// ------------------------------------------------------------------------------------
		public new void SetInsertionPoint(int tag, int book, int section)
		{
			CheckDisposed();

			base.SetInsertionPoint(tag, book, section);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Navigate to the beginning of any Scripture element: Title, Section Head, or Section
		/// Content.
		/// </summary>
		/// <param name="tag">Indicates whether selection should be made in the title, section
		/// Heading or section Content</param>
		/// <param name="book">The 0-based index of the Scripture book in which to put the
		/// insertion point</param>
		/// <param name="section">The 0-based index of the Scripture section in which to put the
		/// insertion point. Ignored if tag is <see cref="ScrBook.ScrBookTags.kflidTitle"/>
		/// </param>
		/// <param name="paragraph">The 0-based index of the paragraph which to put the
		/// insertion point.</param>
		/// ------------------------------------------------------------------------------------
		public new void SetInsertionPoint(int tag, int book, int section, int paragraph)
		{
			CheckDisposed();

			base.SetInsertionPoint(tag, book, section, paragraph);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check for presence of proper paragraph properties.
		/// </summary>
		/// <param name="vwsel">[out] The selection</param>
		/// <param name="hvoText">[out] The HVO</param>
		/// <param name="tagText">[out] The tag</param>
		/// <param name="vqvps">[out] The paragraph properties</param>
		/// <param name="ihvoAnchor">[out] Start index of selection</param>
		/// <param name="ihvoEnd">[out] End index of selection</param>
		/// <returns>Return <c>false</c> if neither selection nor paragraph property. Otherwise
		/// return <c>true</c>.</returns>
		/// ------------------------------------------------------------------------------------
		public bool IsParagraphProps(out IVwSelection vwsel, out int hvoText,
			out int tagText, out IVwPropertyStore[] vqvps, out int ihvoAnchor, out int ihvoEnd)
		{
			CheckDisposed();

			vwsel = null;
			hvoText = 0;
			tagText = 0;
			vqvps = null;
			ihvoAnchor = 0;
			ihvoEnd = 0;

			return EditingHelper.IsParagraphProps(out vwsel, out hvoText, out tagText, out vqvps,
				out ihvoAnchor, out ihvoEnd);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Handle a key press.
		/// </summary>
		/// <param name="keyChar">The pressed character key</param>
		/// <param name="fCalledFromKeyDown">true if this method gets called from OnKeyDown
		/// (to handle Delete)</param>
		/// -----------------------------------------------------------------------------------
		public void HandleKeyPress(char keyChar, bool fCalledFromKeyDown)
		{
			CheckDisposed();

			using (new HoldGraphics(this))
			{
				EditingHelper.HandleKeyPress(keyChar, fCalledFromKeyDown, ModifierKeys,
					m_graphicsManager.VwGraphics);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes InsertFootnote to testing
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ScrFootnote InsertFootnote(SelectionHelper helper)
		{
			CheckDisposed();

			return InsertFootnote(helper, ScrStyleNames.NormalFootnoteParagraph);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes InsertFootnote to testing
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ScrFootnote InsertFootnote(SelectionHelper helper, string paraStyleName)
		{
			CheckDisposed();

			int dummyValue;
			return TeEditingHelper.InsertFootnote(helper, paraStyleName,
				out dummyValue);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes <see cref="TeEditingHelper.ResetParagraphStyle"/> to testing
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ResetParagraphStyle()
		{
			CheckDisposed();

			TeEditingHelper.ResetParagraphStyle();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Simulate clicking a Insert Book menu item
		/// </summary>
		/// <param name="nBook">Ordinal number of the book to insert (i.e. one-based book
		/// number).</param>
		/// ------------------------------------------------------------------------------------
		public IScrBook InsertBook(int nBook)
		{
			CheckDisposed();

			return TeEditingHelper.InsertBook(nBook);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the OnKeyPress method for testing
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		public new void OnKeyPress(KeyPressEventArgs e)
		{
			CheckDisposed();

			base.OnKeyPress(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the OnKeyDown method for testing
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		public new void OnKeyDown(KeyEventArgs e)
		{
			CheckDisposed();

			base.OnKeyDown (e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the ApplyStyle method for testing
		/// </summary>
		/// <param name="styleName">Style name</param>
		/// ------------------------------------------------------------------------------------
		public void ApplyStyle(string styleName)
		{
			CheckDisposed();

			EditingHelper.ApplyStyle(styleName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Enum of options for simulating SelectionIsFootnoteMarker()
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public enum CheckFootnoteMkr
		{
			/// <summary>Call regular rootsite code</summary>
			CallBaseClass,
			/// <summary>Simulate being over a footnote reference</summary>
			SimulateFootnote,
			/// <summary>Simulate being over normal text</summary>
			SimulateNormalText
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This member variable defines the simulation of SelectionIsFootnoteMarker()
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public CheckFootnoteMkr m_checkFootnoteMkr = CheckFootnoteMkr.CallBaseClass; //default

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Simulates the check for selection being on a footnote marker
		/// </summary>
		/// <returns><c>True</c>if current selection is on a footnote</returns>
		/// ------------------------------------------------------------------------------------
		protected override bool SelectionIsFootnoteMarker(IVwSelection vwsel)
		{
			switch (m_checkFootnoteMkr)
			{
				case CheckFootnoteMkr.SimulateFootnote:
					return true;
				case CheckFootnoteMkr.SimulateNormalText:
					return false;
				case CheckFootnoteMkr.CallBaseClass:
				default:
					// process in FwRootSite
					return base.SelectionIsFootnoteMarker(vwsel);
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Refreshes the Display :)
		/// NOTE: this removes the book filter to show all books in the DB
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public override void RefreshDisplay()
		{
			CheckDisposed();

			if (m_rootb == null || m_rootb.Site == null)
				return;

			// Save where the selection is so we can try to restore it after reconstructing.
			// we can't use EditingHelper.CurrentSelection here because the scroll position
			// of the selection may have changed.
			SelectionHelper selHelper = SelectionHelper.Create(this);

			BookFilter.ShowAllBooks();
			// Rebuild the display... the drastic way.
			m_rootb.Reconstruct();

			if (selHelper != null)
				selHelper.RestoreSelectionAndScrollPos();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calculate height for books and sections. The real <see cref="DraftView"/> class uses
		/// its Group to figure out the height of things, but this dummy doesn't have a group
		/// and we don't care about accurate estimates, so just do something simple.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="frag"></param>
		/// <param name="dxAvailWidth"></param>
		/// <returns>The estimated height for the specified hvo in paragraphs</returns>
		/// ------------------------------------------------------------------------------------
		public override int EstimateHeight(int hvo, int frag, int dxAvailWidth)
		{
			CheckDisposed();

			switch((ScrFrags)frag)
			{
				case ScrFrags.kfrBook:
					// The height of a book is the sum of the heights of the sections in the book
					int[] sections = m_fdoCache.GetVectorProperty(hvo,
						(int)ScrBook.ScrBookTags.kflidSections, false);
					int bookHeight = 0;
					foreach (int secHvo in sections)
						bookHeight += EstimateHeight(secHvo, (int)ScrFrags.kfrSection, dxAvailWidth);
					return bookHeight;
				case ScrFrags.kfrSection:
					try
					{
						return base.EstimateHeight(hvo, frag, dxAvailWidth);
					}
					catch
					{
						// Some errors can happen during test teardown that don't happen in real
						// life, so just ignore them.
						return 0;
					}
				default:
					throw new Exception("Unexpected fragment");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="trans"></param>
		/// <param name="status"></param>
		/// ------------------------------------------------------------------------------------
		public void SetTransStatus(ICmTranslation trans, BackTranslationStatus status)
		{
			CheckDisposed();

			SetBackTranslationStatus(trans, status);
		}

//		/// ------------------------------------------------------------------------------------
//		/// <summary>
//		/// Simulates poping up the context menu
//		/// </summary>
//		/// ------------------------------------------------------------------------------------
//		public void SimulateContextMenuPopup()
//		{
//			CheckDisposed();
//
//			base.OnContextMenuPopup(null, new EventArgs());
//		}
//
//		/// ------------------------------------------------------------------------------------
//		/// <summary>
//		/// Exposes the Delete Footnote context menu item for testing
//		/// </summary>
//		/// ------------------------------------------------------------------------------------
//		public MenuItem DeleteFootnoteMenuItem
//		{
//			get {CheckDisposed(); return m_mnuDeleteFootnote; }
//		}
//
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calls the Delete Footnote context menu item
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void OnDeleteFootnote()
		{
			CheckDisposed();

			OnDeleteFootnote(null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CallNextUnfinishedBackTrans()
		{
			CheckDisposed();

			OnBackTranslationNextUnfinished(null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CallPrevUnfinishedBackTrans()
		{
			CheckDisposed();

			OnBackTranslationPrevUnfinished(null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Simulates a Mouse Down event.
		/// </summary>
		/// <param name="point">The point.</param>
		/// ------------------------------------------------------------------------------------
		public void CallMouseDown(Point point)
		{
			CheckDisposed();

			Rectangle rcSrcRoot;
			Rectangle rcDstRoot;
			GetCoordRects(out rcSrcRoot, out rcDstRoot);

			CallMouseDown(point, rcSrcRoot, rcDstRoot);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the editing helper for testing.
		/// </summary>
		/// <value>The editing helper for testing.</value>
		/// ------------------------------------------------------------------------------------
		public EditingHelper EditingHelperForTesting
		{
			set
			{
				m_EditingHelperForTesting = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Provide a TE specific implementation of the EditingHelper
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override EditingHelper EditingHelper
		{
			get
			{
				if (m_EditingHelperForTesting != null)
				{
					if (m_editingHelper != m_EditingHelperForTesting && m_editingHelper != null)
						m_editingHelper.Dispose();
					m_editingHelper = m_EditingHelperForTesting;
				}
				return base.EditingHelper;
			}
		}
	}
	#endregion
}

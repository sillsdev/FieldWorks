// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: SimpleRootSiteTests_Other.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using NUnit.Framework;
using NMock;
using NMock.Constraints;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;

namespace SIL.FieldWorks.Common.RootSites.SimpleRootSiteTests
{
	#region OverriddenEditingHelper
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Simple EditingHelper that lets us paste things even when the view is invisible.
	/// </summary>
	/// ------------------------------------------------------------------------------------
	public class OverriddenEditingHelper : EditingHelper
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:OverriddenEditingHelper"/> class.
		/// </summary>
		/// <param name="callbacks">The callbacks.</param>
		/// ------------------------------------------------------------------------------------
		public OverriddenEditingHelper(IEditingCallbacks callbacks)
			: base(callbacks)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if pasting of text from the clipboard is possible.
		/// </summary>
		/// <returns>
		/// Returns <c>true</c> if pasting is possible.
		/// </returns>
		/// <remarks>Formerly <c>AfVwRootSite::CanPaste()</c>.</remarks>
		/// ------------------------------------------------------------------------------------
		public override bool CanPaste()
		{
			CheckDisposed();
			if (Callbacks != null && Callbacks.EditedRootBox != null && CurrentSelection != null)
			{
				IVwSelection vwsel = CurrentSelection.Selection;
				// CanFormatChar is true only if the selected text is editable.
				if (vwsel != null && vwsel.CanFormatChar)
					return ClipboardContainsString();
			}
			return false;
		}
	}
	#endregion

	#region SimpleViewVc
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Simple view constructor.
	/// </summary>
	/// ------------------------------------------------------------------------------------
	public class SimpleViewVc : VwBaseVc
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="hvo"></param>
		/// <param name="frag"></param>
		/// ------------------------------------------------------------------------------------
		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			CheckDisposed();

			switch(frag)
			{
				case 1: //The root is an StText, display paragraphs not lazily.
					vwenv.AddObjVecItems((int)StText.StTextTags.kflidParagraphs, this, 2);
					break;
				case 2: // StTxtPara, display contents
					vwenv.AddStringProp((int)StTxtPara.StTxtParaTags.kflidContents, null);
					break;
				default:
					throw new ApplicationException("Unexpected frag in SimpleViewVc");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load data needed to display the specified objects using the specified fragment.
		/// This is called before attempting to Display an item that has been listed for lazy
		/// display using AddLazyItems. It may be used to load the necessary data into the
		/// DataAccess object.
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
			// we do nothing in our test
		}
	}
	#endregion SimpleViewVc

	#region ClassyRootSite
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ClassyRootSite : SimpleRootSite
	{
		internal VwInsertDiffParaResponse m_OnInsertDiffParasResponse;
		private FdoCache m_fdoCache;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:ClassyRootSite"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// ------------------------------------------------------------------------------------
		public ClassyRootSite(FdoCache cache)
		{
			m_fdoCache = cache;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Helper used for processing editing requests.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override EditingHelper EditingHelper
		{
			get
			{
				CheckDisposed();
				if (m_editingHelper == null)
					m_editingHelper = new OverriddenEditingHelper(this);
				return m_editingHelper;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This tests whether the class has a cache (in the common RootSite subclass) or
		/// (in this base class) whether it has a ws. This is often used to determine whether
		/// we are sufficiently initialized to go ahead with some operation that may get called
		/// prematurely by something in the .NET framework.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public override bool GotCacheOrWs
		{
			get { return m_fdoCache != null; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Call the OnLayout methods
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CallLayout()
		{
			CheckDisposed();

			OnLayout(new LayoutEventArgs(this, string.Empty));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Method called from views code to deal with complex pastes, overridden for testing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override VwInsertDiffParaResponse OnInsertDiffParas(IVwRootBox prootb,
			ITsTextProps ttpDest, int cPara, ITsTextProps[] ttpSrc, ITsString[] tssParas,
			ITsString tssTrailing)
		{
			 return m_OnInsertDiffParasResponse;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make a root box and initialize it with a view constructor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void MakeRoot(int rootHvo)
		{
			CheckDisposed();

			if (m_fdoCache == null)
				return;

			base.MakeRoot();

			m_rootb = VwRootBoxClass.Create();
			m_rootb.SetSite(this);

			// Set up a new view constructor.
			SimpleViewVc basicViewVc = new SimpleViewVc();
			basicViewVc.DefaultWs = m_fdoCache.DefaultVernWs;

			m_rootb.DataAccess = m_fdoCache.MainCacheAccessor;
			m_rootb.SetRootObject(rootHvo, basicViewVc, 1, m_styleSheet);
		}
	}
	#endregion ClassyRootSite

	#region SimpleRootSiteTests_Other
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Other tests for SimpleRootSite
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class SimpleRootSiteTests_Other : ScrInMemoryFdoTestBase
	{
		#region Data members
		/// <summary>A simple rootsite for testing</summary>
		public ClassyRootSite m_basicView;
		/// <summary>Root object displayed in our view</summary>
		protected int m_frag = 1;
		protected IScrBook m_gen;
		protected int m_hvoRoot;
		#endregion

		#region Setup & Teardown
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Create a new basic view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[SetUp]
		public override void Initialize()
		{
			CheckDisposed();
			base.Initialize();

			FwStyleSheet styleSheet = new FwStyleSheet();
			styleSheet.Init(Cache, m_scr.Hvo, (int)Scripture.ScriptureTags.kflidStyles);

			Debug.Assert(m_basicView == null, "m_basicView is not null.");
			m_basicView = new ClassyRootSite(Cache);
			m_basicView.Visible = false;
			m_basicView.StyleSheet = styleSheet;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called to make the test data for the tests
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void  CreateTestData()
		{
			base.CreateTestData();
			m_scrInMemoryCache.InitializeScripture();
			m_gen = m_scrInMemoryCache.AddBookToMockedScripture(1, "Gen");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shuts down the view
		/// </summary>
		/// <remarks>This method is called after each test</remarks>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void Exit()
		{
			CheckDisposed();
			m_basicView.Dispose();
			m_basicView = null;
			base.Exit();
		}

		#region IDisposable override
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_basicView != null)
					m_basicView.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_basicView = null;

			base.Dispose(disposing);
		}
		#endregion IDisposable override
		#endregion

		#region helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set up the test form.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void ShowForm()
		{
			// We don't actually want to show it, but we need to force the view to create the root
			// box and lay it out so that various test stuff can happen properly.
			m_basicView.Width = 300;
			m_basicView.Height = 300;
			m_basicView.MakeRoot(m_hvoRoot);
			m_basicView.CallLayout();
		}
		#endregion

		#region Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// (TE-5172) Cut and paste operation that includes a section boundary:
		/// range selection starts in the middle of section 0 and ends in the middle of section 1.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void PasteSelectionSpanningTwoSections()
		{
			StText title = m_scrInMemoryCache.AddTitleToMockedBook(m_gen.Hvo, "paragraph with main title style");
			m_hvoRoot = title.Hvo;

			// The IntroParagraph style in a book title would be illegal in TE, but we're not TE.
			// We're just a test that needs two paragraphs with different styles.
			StTxtPara titlePara2 = m_scrInMemoryCache.AddParaToMockedText(m_hvoRoot, ScrStyleNames.IntroParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(titlePara2, "paragraph with different style",
				m_inMemoryCache.Cache.DefaultVernWs);

			ShowForm();

			// Make a selection from the top of the view to the bottom.
			IVwSelection sel0 = m_basicView.RootBox.MakeSimpleSel(true, false, false, false);
			IVwSelection sel1 = m_basicView.RootBox.MakeSimpleSel(false, false, false, false);
			m_basicView.RootBox.MakeRangeSelection(sel0, sel1, true);

			// Copy the selection and then paste it over the existing selection.
			// This is an illegal paste, so the paste will fail.
			// However, we expect the contents to remain intact.
			Assert.IsTrue(m_basicView.EditingHelper.CopySelection());
			// Select just a single word in the text to confirm the paste operation better.
			sel0.GrowToWord();
			m_basicView.m_OnInsertDiffParasResponse = VwInsertDiffParaResponse.kidprFail;
			m_inMemoryCache.MockActionHandler.Strict = true;
			m_inMemoryCache.MockActionHandler.Expect("BeginUndoTask", "&Undo Paste", "&Redo Paste");
			// We don't really CARE whether we get these additional Begin/EndUndoTask calls, but we do, so we have to 'expect' them.
			// There is only one EndUndoTask because we intentionally make the paste fail (by having the mock return kidprFail).
			m_inMemoryCache.MockActionHandler.Expect("BeginUndoTask", "&Undo Typing", "&Redo Typing");
			m_inMemoryCache.MockActionHandler.Expect("EndUndoTask");
			//m_inMemoryCache.MockActionHandler.Expect("AddAction", new IsAnything());
			m_inMemoryCache.MockActionHandler.Expect("Rollback", 0);
			m_basicView.EditingHelper.PasteClipboard(false);

			m_inMemoryCache.MockActionHandler.Verify();
			// GrowToWord causes a Char Property Engine to be created, and somehow the test runner fails if we don't
			// shut the factory down.
			m_inMemoryCache.Cache.LanguageWritingSystemFactoryAccessor.Shutdown();
		}
		#endregion
	}
	#endregion
}

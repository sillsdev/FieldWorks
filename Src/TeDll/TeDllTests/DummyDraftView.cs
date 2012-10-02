// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2002' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DummyDraftView.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Reflection;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.Utils;

namespace SIL.FieldWorks.TE
{
	#region DummyTeEditingHelper
	internal class TestTeEditingHelper : TeEditingHelper
	{
		internal bool m_DeferSelectionUntilEndOfUOW = false;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="DummyTeEditingHelper"/> class.
		/// </summary>
		/// <param name="callbacks">The callbacks.</param>
		/// <param name="cache">The cache.</param>
		/// <param name="filterInstance">The filter instance.</param>
		/// <param name="viewType">Type of the view.</param>
		/// <param name="app">The app.</param>
		/// ------------------------------------------------------------------------------------
		internal TestTeEditingHelper(IEditingCallbacks callbacks, FdoCache cache, int filterInstance,
			TeViewType viewType, IApp app) : base(callbacks, cache, filterInstance, viewType, app)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a flag indicating whether to defer setting a selection until the end of the
		/// Unit of Work.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override bool DeferSelectionUntilEndOfUOW
		{
			get
			{
				return m_DeferSelectionUntilEndOfUOW;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this instance can reduce selection to ip at top.
		/// In tests, always returns true
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override bool CanReduceSelectionToIpAtTop
		{
			get { return true; }
		}

		///// ------------------------------------------------------------------------------------
		///// <summary>
		///// Gets a value indicating whether this instance is current selection out of date.
		///// For testing purposes we use the stored selection if we have one.
		///// </summary>
		///// ------------------------------------------------------------------------------------
		//protected override bool IsCurrentSelectionOutOfDate
		//{
		//    get
		//    {
		//        return (m_currentSelection == null);
		//    }
		//}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the TeViewType of this view
		/// </summary>
		/// <remarks>Normally this is a readonly field, but for tests we don't get a chance to
		/// set it to the right thing before the editing helper is created, so we provide
		/// this handy-dandy setter to force it.</remarks>
		/// ------------------------------------------------------------------------------------
		internal TeViewType NewViewType
		{
			set
			{
				ReflectionHelper.SetField(this, "m_viewType", value);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if pasting of text from the clipboard is possible.
		/// </summary>
		/// <returns>
		/// Returns <c>true</c> if pasting is possible.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override bool CanPaste()
		{
			return true;
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
	public class DummyDraftView : DraftView
	{
		#region class DummyGraphicsManager
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
		#endregion

		#region Data members
		private DraftViewVc m_draftViewVcForTesting;
		internal SelectionHelper RequestedSelectionAtEndOfUow = null;
		#endregion

		#region Constructor
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
			base(cache, filterInstance, null, null, true, fIsBackTrans, fIsBackTrans,
			TeViewType.DraftView | (fIsBackTrans ? TeViewType.BackTranslation : TeViewType.Scripture),
			cache.DefaultAnalWs, null)
		{
		}
		#endregion

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
		/// Internal version of CreateEditingHelper allows subclasses to override to create a
		/// subclass of TeEditingHelper without repeating the common initialization code.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override TeEditingHelper CreateEditingHelper_Internal()
		{
			TestTeEditingHelper helper = new TestTeEditingHelper(this, Cache, m_filterInstance, m_viewType, m_app);
			helper.ContentType = ContentType;
			return helper;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a view constructor suitable for this kind of view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override TeStVc CreateViewConstructor()
		{
			CheckDisposed();

			if (m_draftViewVcForTesting != null)
				return m_draftViewVcForTesting;

			return new DummyDraftViewVC(TeStVc.LayoutViewTarget.targetDraft, FilterInstance,
				m_styleSheet, m_fShowInTable);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets value of filter instance - used to make filters unique per main window.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal int FilterInstance
		{
			get
			{
				CheckDisposed();
				return m_filterInstance;
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
				return (DraftViewVc)m_vc;
			}
			set
			{
				CheckDisposed();
				m_vc = value;
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

			PropertyInfo heightEstimator = m_vc.GetType().GetProperty("HeightEstimator");
			heightEstimator.SetValue(m_vc, this, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes <see cref="T:SIL.FieldWorks.TE.TeEditingHelper.HandleMouseDown"/> for
		/// testing Insert Verse Numbers mode.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void InsertVerseNumber()
		{
			CheckDisposed();
			TeEditingHelper.InsertVerseNumber();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes TeEditingHelper.InsertVerseNumber for testing.
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
		/// insertion point. Ignored if tag is <see cref="ScrBookTags.kflidTitle"/>
		/// </param>
		/// <returns>The selection helper object used to make move the IP.</returns>
		/// ------------------------------------------------------------------------------------
		public new SelectionHelper SetInsertionPoint(int tag, int book, int section)
		{
			CheckDisposed();

			return base.SetInsertionPoint(tag, book, section);
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
		/// insertion point. Ignored if tag is <see cref="ScrBookTags.kflidTitle"/>
		/// </param>
		/// <param name="paragraph">The 0-based index of the paragraph which to put the
		/// insertion point.</param>
		/// <returns>The selection helper object used to make move the IP.</returns>
		/// ------------------------------------------------------------------------------------
		public new SelectionHelper SetInsertionPoint(int tag, int book, int section, int paragraph)
		{
			CheckDisposed();

			return base.SetInsertionPoint(tag, book, section, paragraph);
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

			return EditingHelper.IsParagraphProps(out vwsel, out hvoText, out tagText, out vqvps,
				out ihvoAnchor, out ihvoEnd);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Handle a key press.
		/// </summary>
		/// <param name="keyChar">The pressed character key</param>
		/// -----------------------------------------------------------------------------------
		public void HandleKeyPress(char keyChar)
		{
			CheckDisposed();

			using (new HoldGraphics(this))
			{
				EditingHelper.HandleKeyPress(keyChar, ModifierKeys);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes InsertFootnote to testing
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IScrFootnote InsertFootnote(SelectionHelper helper)
		{
			CheckDisposed();

			return InsertFootnote(helper, ScrStyleNames.NormalFootnoteParagraph);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes InsertFootnote to testing
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IScrFootnote InsertFootnote(SelectionHelper helper, string paraStyleName)
		{
			CheckDisposed();

			int dummyValue;
			return (IScrFootnote)TeEditingHelper.InsertFootnote(helper, paraStyleName,
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

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Refreshes the Display :)
		/// NOTE: this removes the book filter to show all books in the DB
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public override bool RefreshDisplay()
		{
			CheckDisposed();

			if (m_rootb == null || m_rootb.Site == null)
				return false;

			// Save where the selection is so we can try to restore it after reconstructing.
			// we can't use EditingHelper.CurrentSelection here because the scroll position
			// of the selection may have changed.
			SelectionHelper selHelper = SelectionHelper.Create(this);

			BookFilter.ShowAllBooks();
			// Rebuild the display... the drastic way.
			m_rootb.Reconstruct();

			if (selHelper != null)
				selHelper.RestoreSelectionAndScrollPos();
			return false;
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
					IScrBook book = m_fdoCache.ServiceLocator.GetInstance<IScrBookRepository>().GetObject(hvo);
					// The height of a book is the sum of the heights of the sections in the book
					int bookHeight = 0;
					foreach (IScrSection section in book.SectionsOS)
						bookHeight += EstimateHeight(section.Hvo, (int)ScrFrags.kfrSection, dxAvailWidth);
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
		/// Sets the back translation status.
		/// </summary>
		/// <param name="trans">The translation.</param>
		/// <param name="status">The status.</param>
		/// ------------------------------------------------------------------------------------
		public void SetTransStatus(ICmTranslation trans, BackTranslationStatus status)
		{
			CheckDisposed();

			SetBackTranslationStatus(trans, status);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calls the Delete Footnote context menu item
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CallDeleteFootnote()
		{
			CheckDisposed();

			((TeEditingHelper)m_editingHelper).OnDeleteFootnoteAux();
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

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// In this simple implementation, we just record the information about the requested
		/// selection.
		/// </summary>
		/// <param name="rootb">The rootbox</param>
		/// <param name="ihvoRoot">Index of root element</param>
		/// <param name="cvlsi">count of levels</param>
		/// <param name="rgvsli">levels</param>
		/// <param name="tagTextProp">tag or flid of property containing the text (TsString)</param>
		/// <param name="cpropPrevious">number of previous occurrences of the text property</param>
		/// <param name="ich">character offset into the text</param>
		/// <param name="wsAlt">The id of the writing system for the selection.</param>
		/// <param name="fAssocPrev">Flag indicating whether to associate the insertion point
		/// with the preceding character or the following character</param>
		/// <param name="selProps">The selection properties.</param>
		/// --------------------------------------------------------------------------------
		public override void RequestSelectionAtEndOfUow(IVwRootBox rootb, int ihvoRoot,
			int cvlsi, SelLevInfo[] rgvsli, int tagTextProp, int cpropPrevious, int ich, int wsAlt,
			bool fAssocPrev, ITsTextProps selProps)
		{
			Assert.AreEqual(RootBox, rootb);
			Assert.IsNull(RequestedSelectionAtEndOfUow);

			RequestedSelectionAtEndOfUow = new SelectionHelper();
			RequestedSelectionAtEndOfUow.RootSite = this;
			RequestedSelectionAtEndOfUow.IhvoRoot = ihvoRoot;
			RequestedSelectionAtEndOfUow.NumberOfLevels = cvlsi;
			RequestedSelectionAtEndOfUow.SetLevelInfo(SelectionHelper.SelLimitType.Anchor, rgvsli);
			RequestedSelectionAtEndOfUow.TextPropId = tagTextProp;
			RequestedSelectionAtEndOfUow.NumberOfPreviousProps = cpropPrevious;
			RequestedSelectionAtEndOfUow.IchAnchor = ich;
			RequestedSelectionAtEndOfUow.Ws = wsAlt;
			RequestedSelectionAtEndOfUow.AssocPrev = fAssocPrev;

			RootBox.DestroySelection(); // Need to act like real program in this regard
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If we need to make a selection, but we can't because edits haven't been updated in
		/// the view, this method requests creation of a selection after the unit of work is
		/// complete. It will also scroll the selection into view.
		/// Derived classes should implement this if they have any hope of supporting multi-
		/// paragraph editing.
		/// </summary>
		/// <param name="helper">The selection to restore</param>
		/// ------------------------------------------------------------------------------------
		public override void RequestVisibleSelectionAtEndOfUow(SelectionHelper helper)
		{
			Assert.IsNull(RequestedSelectionAtEndOfUow);

			RequestedSelectionAtEndOfUow = helper;

			RootBox.DestroySelection(); // Need to act like real program in this regard
		}
	}
	#endregion
}

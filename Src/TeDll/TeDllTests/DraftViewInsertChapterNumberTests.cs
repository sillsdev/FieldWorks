// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2003' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DraftViewInsertChapterNumberTests.cs
// Responsibility: TE Team
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using NMock;
using NMock.Constraints;
using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.TE.DraftViews
{
	#region DummyGraphics class
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Implement IVwGraphicsWin32 so that we can store the HDC that gets passed in and
	/// later return it.
	/// </summary>
	/// ------------------------------------------------------------------------------------
	public class DummyGraphics : IVwGraphicsWin32, IFWDisposable
	{
		private IntPtr m_hdc = IntPtr.Zero;

		#region IDisposable & Co. implementation
		// Region last reviewed: never

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

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed = false;

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~DummyGraphics()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

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
		protected virtual void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			// see comment on Init() why we don't call FreeCoTaskMem
			//Marshal.FreeCoTaskMem(m_hdc);
			m_hdc = IntPtr.Zero;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		#region IVwGraphicsWin32 Members

		/// --------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// --------------------------------------------------------------------------------
		public void GetSuperscriptHeightRatio(out int numerator, out int denominator)
		{
			CheckDisposed();

			// TODO:  Add DummyGraphics.GetSuperscriptHeightRatio implementation
			numerator = 2;
			denominator = 3;
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// --------------------------------------------------------------------------------
		public void GetSuperscriptYOffsetRatio(out int numerator, out int denominator)
		{
			CheckDisposed();

			// TODO:  Add DummyGraphics.GetSuperscriptYOffsetRatio implementation
			numerator = 1;
			denominator = 3;
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// --------------------------------------------------------------------------------
		public void GetSubscriptHeightRatio(out int numerator, out int denominator)
		{
			CheckDisposed();

			// TODO:  Add DummyGraphics.GetSubscriptHeightRatio implementation
			numerator = 2;
			denominator = 3;
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// --------------------------------------------------------------------------------
		public void GetSubscriptYOffsetRatio(out int numerator, out int denominator)
		{
			CheckDisposed();

			// TODO:  Add DummyGraphics.GetSubscriptYOffsetRatio implementation
			numerator = 1;
			denominator = 3;
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// --------------------------------------------------------------------------------
		public void PopClipRect()
		{
			CheckDisposed();

			// TODO:  Add DummyGraphics.PopClipRect implementation
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets a BackColor
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public int BackColor
		{
			set
			{
				CheckDisposed();
				/* TODO:  Add DummyGraphics.set_BackColor implementation */
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets a ForeColor
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public int ForeColor
		{
			set
			{
				CheckDisposed();
				/* TODO:  Add DummyGraphics.set_ForeColor implementation */
			}
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="hdc"></param>
		/// --------------------------------------------------------------------------------
		public void SetMeasureDc(System.IntPtr hdc)
		{
			CheckDisposed();

			// TODO:  Add DummyGraphics.SetMeasureDc implementation
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		/// --------------------------------------------------------------------------------
		public int XUnitsPerInch
		{
			get
			{
				CheckDisposed();
				return 96;
			}
			set
			{
				CheckDisposed();
			}
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		/// --------------------------------------------------------------------------------
		public LgCharRenderProps FontCharProperties
		{
			get
			{
				CheckDisposed();
				return new LgCharRenderProps();
			}
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="xLeft"></param>
		/// <param name="yTop"></param>
		/// <param name="xRight"></param>
		/// <param name="yBottom"></param>
		/// --------------------------------------------------------------------------------
		public void DrawLine(int xLeft, int yTop, int xRight, int yBottom)
		{
			CheckDisposed();

			// TODO:  Add DummyGraphics.DrawLine implementation
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="cvpnt"></param>
		/// <param name="_rgvpnt"></param>
		/// --------------------------------------------------------------------------------
		public void DrawPolygon(int cvpnt, System.Drawing.Point[] _rgvpnt)
		{
			CheckDisposed();

			// TODO:  Add DummyGraphics.DrawPolygon implementation
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="rcClip"></param>
		/// --------------------------------------------------------------------------------
		public void PushClipRect(Rect rcClip)
		{
			CheckDisposed();

			// TODO:  Add DummyGraphics.PushClipRect implementation
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="cch"></param>
		/// <param name="_rgch"></param>
		/// <param name="xStretch"></param>
		/// --------------------------------------------------------------------------------
		public void DrawText(int x, int y, int cch, string _rgch, int xStretch)
		{
			CheckDisposed();

			// TODO:  Add DummyGraphics.DrawText implementation
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="nTableId"></param>
		/// <param name="_cbTableSz"></param>
		/// <returns></returns>
		/// --------------------------------------------------------------------------------
		public string GetFontData(int nTableId, out int _cbTableSz)
		{
			CheckDisposed();

			// TODO:  Add DummyGraphics.GetFontData implementation
			_cbTableSz = 0;
			return null;
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="_xLeft"></param>
		/// <param name="_yTop"></param>
		/// <param name="_xRight"></param>
		/// <param name="_yBottom"></param>
		/// --------------------------------------------------------------------------------
		public void GetClipRect(out int _xLeft, out int _yTop, out int _xRight, out int _yBottom)
		{
			CheckDisposed();

			// TODO:  Add DummyGraphics.GetClipRect implementation
			_xLeft = 0;
			_yTop = 0;
			_xRight = 0;
			_yBottom = 0;
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="_rcClip"></param>
		/// --------------------------------------------------------------------------------
		public void SetClipRect(ref Rect _rcClip)
		{
			CheckDisposed();

			// TODO:  Add DummyGraphics.SetClipRect implementation
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// --------------------------------------------------------------------------------
		public void ReleaseDC()
		{
			CheckDisposed();

			// TODO:  Add DummyGraphics.ReleaseDC implementation
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="chw"></param>
		/// <param name="nPoint"></param>
		/// <param name="_xRet"></param>
		/// <param name="_yRet"></param>
		/// --------------------------------------------------------------------------------
		public void XYFromGlyphPoint(int chw, int nPoint, out int _xRet, out int _yRet)
		{
			CheckDisposed();

			// TODO:  Add DummyGraphics.XYFromGlyphPoint implementation
			_xRet = 0;
			_yRet = 0;
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="nTableId"></param>
		/// <param name="_cbTableSz"></param>
		/// <param name="_rgch"></param>
		/// <param name="cchMax"></param>
		/// --------------------------------------------------------------------------------
		public void GetFontDataRgch(int nTableId, out int _cbTableSz, ArrayPtr _rgch, int cchMax)
		{
			CheckDisposed();

			// TODO:  Add DummyGraphics.GetFontDataRgch implementation
			_cbTableSz = 0;
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		/// --------------------------------------------------------------------------------
		public int GetFontEmSquare()
		{
			CheckDisposed();

			// TODO:  Add DummyGraphics.GetFontEmSquare implementation
			return 0;
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		/// --------------------------------------------------------------------------------
		public System.IntPtr GetDeviceContext()
		{
			CheckDisposed();

			return m_hdc;
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="_chrp"></param>
		/// --------------------------------------------------------------------------------
		public void SetupGraphics(ref LgCharRenderProps _chrp)
		{
			CheckDisposed();

			// TODO:  Add DummyGraphics.SetupGraphics implementation
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="cch"></param>
		/// <param name="_rgch"></param>
		/// <param name="ich"></param>
		/// <param name="xStretch"></param>
		/// <returns></returns>
		/// --------------------------------------------------------------------------------
		public int GetTextLeadWidth(int cch, string _rgch, int ich, int xStretch)
		{
			CheckDisposed();

			// TODO:  Add DummyGraphics.GetTextLeadWidth implementation
			return 0;
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="xLeft"></param>
		/// <param name="xRight"></param>
		/// <param name="y"></param>
		/// <param name="dyHeight"></param>
		/// <param name="cdx"></param>
		/// <param name="_rgdx"></param>
		/// <param name="_dxStart"></param>
		/// --------------------------------------------------------------------------------
		public void DrawHorzLine(int xLeft, int xRight, int y, int dyHeight, int cdx, int[] _rgdx, ref int _dxStart)
		{
			CheckDisposed();

			// TODO:  Add DummyGraphics.DrawHorzLine implementation
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="xLeft"></param>
		/// <param name="yTop"></param>
		/// <param name="xRight"></param>
		/// <param name="yBottom"></param>
		/// --------------------------------------------------------------------------------
		public void DrawRectangle(int xLeft, int yTop, int xRight, int yBottom)
		{
			CheckDisposed();

			// TODO:  Add DummyGraphics.DrawRectangle implementation
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="_pic"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="cx"></param>
		/// <param name="cy"></param>
		/// <param name="xSrc"></param>
		/// <param name="ySrc"></param>
		/// <param name="cxSrc"></param>
		/// <param name="cySrc"></param>
		/// <param name="_rcWBounds"></param>
		/// --------------------------------------------------------------------------------
		public void RenderPicture(stdole.IPicture _pic, int x, int y, int cx, int cy, int xSrc, int ySrc, int cxSrc, int cySrc, ref Rect _rcWBounds)
		{
			CheckDisposed();

			// TODO:  Add DummyGraphics.RenderPicture implementation
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		/// --------------------------------------------------------------------------------
		public int FontDescent
		{
			get
			{
				CheckDisposed();
				return 6;
			}
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		/// --------------------------------------------------------------------------------
		public int YUnitsPerInch
		{
			get
			{
				CheckDisposed();
				return 96;
			}
			set
			{
				CheckDisposed();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a FontAscent
		/// </summary>
		/// <value></value>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public int FontAscent
		{
			get
			{
				CheckDisposed();
				return 12;
			}
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="cch"></param>
		/// <param name="_rgch"></param>
		/// <param name="_x"></param>
		/// <param name="_y"></param>
		/// --------------------------------------------------------------------------------
		public void GetTextExtent(int cch, string _rgch, out int _x, out int _y)
		{
			CheckDisposed();

			// TODO:  Add DummyGraphics.GetTextExtent implementation
			_x = 0;
			_y = 0;
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="hdc"></param>
		/// --------------------------------------------------------------------------------
		public void Initialize(System.IntPtr hdc)
		{
			CheckDisposed();

			// Don't call FreeCoTaskMem. We don't know where we got m_hdc from (probably
			// by calling Graphics.GetHdc()), but either way the instance that created it
			// is responsible for cleaning it up. If we do it here we get warnings
			// (HEAP[nunit.exe]: Invalid Address specified to RtlFreeHeap) when running
			// in NUnit GUI.
			//if (m_hdc != IntPtr.Zero)
			//    Marshal.FreeCoTaskMem(m_hdc);
			m_hdc = hdc;
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="chw"></param>
		/// <param name="_sBoundingWidth"></param>
		/// <param name="_yBoundingHeight"></param>
		/// <param name="_xBoundingX"></param>
		/// <param name="_yBoundingY"></param>
		/// <param name="_xAdvanceX"></param>
		/// <param name="_yAdvanceY"></param>
		/// --------------------------------------------------------------------------------
		public void GetGlyphMetrics(int chw, out int _sBoundingWidth, out int _yBoundingHeight, out int _xBoundingX, out int _yBoundingY, out int _xAdvanceX, out int _yAdvanceY)
		{
			CheckDisposed();

			// TODO:  Add DummyGraphics.GetGlyphMetrics implementation
			_sBoundingWidth = 0;
			_yBoundingHeight = 0;
			_xBoundingX = 0;
			_yBoundingY = 0;
			_xAdvanceX = 0;
			_yAdvanceY = 0;
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="cch"></param>
		/// <param name="_rgchw"></param>
		/// <param name="uOptions"></param>
		/// <param name="_rect"></param>
		/// <param name="_rgdx"></param>
		/// --------------------------------------------------------------------------------
		public void DrawTextExt(int x, int y, int cch, string _rgchw, uint uOptions, ref Rect _rect, int _rgdx)
		{
			CheckDisposed();

			// TODO:  Add DummyGraphics.DrawTextExt implementation
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="xLeft"></param>
		/// <param name="yTop"></param>
		/// <param name="xRight"></param>
		/// <param name="yBottom"></param>
		/// --------------------------------------------------------------------------------
		public void InvertRect(int xLeft, int yTop, int xRight, int yBottom)
		{
			CheckDisposed();

			// TODO:  Add DummyGraphics.InvertRect implementation
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="_bData"></param>
		/// <param name="cbData"></param>
		/// <returns></returns>
		/// --------------------------------------------------------------------------------
		public stdole.IPicture MakePicture(byte[] _bData, int cbData)
		{
			CheckDisposed();

			// TODO:  Add DummyGraphics.MakePicture implementation
			return null;
		}

		#endregion
	}
	#endregion

	#region InsertChapterNumberTests
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Unit tests for inserting chapter numbers in DraftView. These tests use mock objects and
	/// so don't require a real database.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class InsertChapterNumberTests : ScrInMemoryFdoTestBase
	{
		#region Member variables
		private DummyDraftView m_draftView;
		private DynamicMock m_styleSheet;
		private DynamicMock m_rootBox;
		private DynamicMock m_vwGraphics;
		private DynamicMock m_selHelper;
		private object[] m_PropInfoArgs;
		private string[] m_PropInfoTypes;
		private object[] m_TextSelInfoArgsAnchor;
		private object[] m_TextSelInfoArgsEnd;
		private string[] m_TextSelInfoTypes;
		private object[] m_LocationArgs;
		private string[] m_LocationTypes;
		private object[] m_AllSelEndInfoArgs;
		private string[] m_AllSelEndInfoTypes;
		private const int kflidSectionContent = (int)ScrSection.ScrSectionTags.kflidContent;
		private const int kflidParaContent = (int)StTxtPara.StTxtParaTags.kflidContents;
		#endregion

		#region Setup and Teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Suite setup
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			CheckDisposed();
			base.FixtureSetup();

			string intType = "System.Int32";
			string intRef = intType + "&";
			string boolType = typeof(bool).FullName;
			string boolRef = boolType + "&";
			string rectType = "SIL.FieldWorks.Common.Utils.Rect";
			string rectRef = rectType + "&";

			m_PropInfoArgs = new object[] { false, 0, null, null, null, null, null };
			m_PropInfoTypes =
				new string[] { boolType, intType, intRef, intRef, intRef, intRef,
								 "SIL.FieldWorks.Common.COMInterfaces.IVwPropertyStore&" };
			m_TextSelInfoArgsAnchor = new object[] { false, null, null, null, null, null, null };
			m_TextSelInfoArgsEnd = new object[] { true, null, null, null, null, null, null };
			m_TextSelInfoTypes =
				new string[] { boolType, "SIL.FieldWorks.Common.COMInterfaces.ITsString&", intRef,
								 boolRef, intRef, intRef, intRef};

			m_LocationArgs =
				new object[] { new IsAnything(), new IsAnything(), new IsAnything(), null,
								 null, null, null };
			m_LocationTypes =
				new string[] { typeof(IVwGraphics).FullName, rectType, rectType, rectRef,
								 rectRef, boolRef, boolRef };

			m_AllSelEndInfoArgs =
				new object[] { new IsAnything(), null, 3, new IsAnything(), null, null,
								 null, null, null, null };
			m_AllSelEndInfoTypes =
				new string[] { boolType, intRef, intType, "SIL.FieldWorks.Common.COMInterfaces.ArrayPtr",
								 intRef, intRef, intRef, intRef, boolRef,
								 "SIL.FieldWorks.Common.COMInterfaces.ITsTextProps&" };

			m_selHelper = new DynamicMock(typeof(SelectionHelper));
			m_selHelper.Strict = true;
			m_selHelper.SetupResult("NumberOfLevels", 4);
			m_selHelper.SetupResult("RestoreSelectionAndScrollPos", true);
			SelectionHelper.s_mockedSelectionHelper = (SelectionHelper)m_selHelper.MockInstance;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureTearDown]
		public void FixtureTeardown()
		{
			SelectionHelper.s_mockedSelectionHelper = null;
		}

		#region IDisposable override

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
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_draftView != null)
					m_draftView.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_draftView = null;
			m_styleSheet = null;
			m_rootBox = null;
			m_vwGraphics = null;
			m_selHelper = null;
			m_PropInfoArgs = null;
			m_PropInfoTypes = null;
			m_TextSelInfoArgsAnchor = null;
			m_TextSelInfoTypes = null;
			m_LocationArgs = null;
			m_LocationTypes = null;
			m_AllSelEndInfoArgs = null;
			m_AllSelEndInfoTypes = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Create a new draft view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[SetUp]
		public override void Initialize()
		{
			CheckDisposed();
			base.Initialize();

			m_styleSheet = new DynamicMock(typeof(FwStyleSheet));
			m_styleSheet.Strict = true;

			BTInsertVerseAndFootnoteTests.InitializeVwSelection(m_selHelper);

			Debug.Assert(m_draftView == null, "m_draftView is not null.");
			//if (m_draftView != null)
			//	m_draftView.Dispose();
			m_draftView = new DummyDraftView(Cache, false, 0);
			m_draftView.RootBox = SetupRootBox();
			m_draftView.Graphics = SetupGraphics();
			m_draftView.MakeRoot();
			m_draftView.StyleSheet = (FwStyleSheet)m_styleSheet.MockInstance;
			m_draftView.ActivateView();
			m_draftView.TeEditingHelper.InTestMode = true;
			m_rootBox.Strict = true;

			SelLevInfo[] selLevInfo = new SelLevInfo[4];
			selLevInfo[3].tag = m_draftView.BookFilter.Tag;
			selLevInfo[2].tag = (int)ScrBook.ScrBookTags.kflidSections;
			selLevInfo[2].ihvo = 0;
			selLevInfo[0].ihvo = 0;
			selLevInfo[0].tag = (int)StText.StTextTags.kflidParagraphs;
			m_selHelper.SetupResult("GetLevelInfo", selLevInfo,
				new Type[] { typeof(SelectionHelper.SelLimitType)});
			m_selHelper.SetupResult("LevelInfo", selLevInfo);
			m_selHelper.Expect("SetSelection", false);
			m_selHelper.Expect("SetSelection", true);
			m_selHelper.ExpectAndReturn("ReduceSelectionToIp",
				m_selHelper.MockInstance, SelectionHelper.SelLimitType.Top, false, true);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Close the draft view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[TearDown]
		public override void Exit()
		{
			CheckDisposed();

			m_draftView.Dispose();
			m_draftView = null;
			m_rootBox = null;
			m_styleSheet = null;
			m_vwGraphics = null;

			base.Exit();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Setup the mock rootbox
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private IVwRootBox SetupRootBox()
		{
			m_rootBox = new DynamicMock(typeof(IVwRootBox));
			m_rootBox.SetupResult("SetSite", null, typeof(IVwRootSite));
			m_rootBox.SetupResult("DataAccess", m_inMemoryCache.CacheAccessor);
			m_rootBox.SetupResult("SetRootObject", null, typeof(int), typeof(IVwViewConstructor),
				typeof(int), typeof(IVwStylesheet));
			m_rootBox.SetupResult("Height", 200); // JT: arbitrary values.
			m_rootBox.SetupResult("Width", 200);
			m_rootBox.SetupResult("Site", m_draftView);
			m_rootBox.Ignore("Close");
			m_rootBox.Ignore("FlashInsertionPoint");
			m_rootBox.SetupResult("GetRootObject", null,
				new string[] {typeof(int).FullName + "&", typeof(IVwViewConstructor).FullName + "&", typeof(int).FullName + "&", typeof(IVwStylesheet).FullName + "&"},
				new object[] {0, null, 0, null});

			return (IVwRootBox)m_rootBox.MockInstance;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Setup the mock graphics object
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private IVwGraphics SetupGraphics()
		{
			m_vwGraphics = new DynamicMock(typeof(IVwGraphicsWin32), "MockIVwGraphics",
				typeof(DummyGraphics));
			m_vwGraphics.AdditionalReferences = new string[] { "TeDllTests.dll" };
			m_vwGraphics.Strict = true;
			// JT: this doesn't seem to get used? I had instead to fix the implementation of
			// get_XUnitsPerInch in DummyGraphics.
			m_vwGraphics.SetupResult("XUnitsPerInch", 96);
			m_vwGraphics.SetupResult("YUnitsPerInch", 96);

			return (IVwGraphics)m_vwGraphics.MockInstance;
		}
		#endregion

		#region Setup Helper functions
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up expected values for simple selection and action handler
		/// </summary>
		/// <param name="para">The paragraph.</param>
		/// <param name="hvoSection">The hvo of the section.</param>
		/// <param name="hvoBook">The hvo of the book.</param>
		/// <param name="ich">The character offset of the IP in the para.</param>
		/// <returns>Mock selection</returns>
		/// ------------------------------------------------------------------------------------
		private DynamicMock SetupInsertChapterSelection(StTxtPara para, int hvoSection,
			int hvoBook, int ich)
		{
			return SetupInsertChapterSelection(para, 0, hvoSection, 0, hvoBook, ich);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up expected values for selection and action handler
		/// </summary>
		/// <param name="para">The paragraph.</param>
		/// <param name="iPara">The index of the para.</param>
		/// <param name="hvoSection">The hvo of the section.</param>
		/// <param name="iSection">The index of the section.</param>
		/// <param name="hvoBook">The hvo of the book.</param>
		/// <param name="ich">The character offset of the IP in the para.</param>
		/// <returns>Mock selection</returns>
		/// ------------------------------------------------------------------------------------
		private DynamicMock SetupInsertChapterSelection(StTxtPara para, int iPara,
			int hvoSection, int iSection, int hvoBook, int ich)
		{
			int hvoPara = para.Hvo;

			DynamicMock sel = new DynamicMock(typeof(IVwSelection));
			sel.Strict = true;
			sel.SetupResult("IsValid", true);
			sel.SetupResult("SelType", VwSelType.kstText);
			sel.SetupResult("IsRange", false);

			sel.ExpectAndReturn("Commit", true);
			m_rootBox.SetupResult("Selection", sel.MockInstance);

			// set up expected calls to action handler
			m_selHelper.SetupResult("Selection", sel.MockInstance);
			SelLevInfo[] info = new SelLevInfo[4];
			info[0] = new SelLevInfo();
			info[0].tag = (int)StText.StTextTags.kflidParagraphs;
			info[0].hvo = hvoPara;
			info[0].ihvo = iPara;
			info[1] = new SelLevInfo();
			info[1].tag = (int)ScrSection.ScrSectionTags.kflidContent;
			info[1].hvo = Cache.LangProject.Hvo; // hvo of text. Who cares?
			info[2] = new SelLevInfo();
			info[2].tag = (int)ScrBook.ScrBookTags.kflidSections;
			info[2].hvo = hvoSection;
			info[2].ihvo = iSection;
			info[3] = new SelLevInfo();
			info[3].tag = m_draftView.BookFilter.Tag;
			info[3].hvo = hvoBook;
			m_selHelper.SetupResult("GetLevelInfo", info,
				new Type[] { typeof(SelectionHelper.SelLimitType)});
			m_selHelper.SetupResult("LevelInfo", info);
			m_selHelper.SetupResultForParams("GetTextPropId",
				(int)StText.StTextTags.kflidParagraphs, SelectionHelper.SelLimitType.Anchor);

			sel.SetupResultForParams("TextSelInfo", null, m_TextSelInfoArgsEnd, m_TextSelInfoTypes,
				new object[] { true, para.Contents.UnderlyingTsString, ich, false, hvoPara,
					kflidParaContent, 0 });
			sel.SetupResultForParams("TextSelInfo", null, m_TextSelInfoArgsAnchor, m_TextSelInfoTypes,
				new object[] { false, para.Contents.UnderlyingTsString, ich, false, hvoPara,
					kflidParaContent, 0 });
			m_selHelper.ExpectAndReturn("GetTextPropId", (int)StTxtPara.StTxtParaTags.kflidContents, new object[] { SelectionHelper.SelLimitType.Top });
			m_selHelper.SetupResultForParams("GetTss", para.Contents.UnderlyingTsString,
				new object[] { SelectionHelper.SelLimitType.Anchor });
			m_selHelper.SetupResult("IsValid", true);

			return sel;
		}
		#endregion

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a chapter number at a valid position (middle of paragraph).
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void MiddleOfParagraph()
		{
			CheckDisposed();

			// JohnT: I made GetIch() return a result consistent with the old IchAnchor test setup,
			// but I think it should really be something larger, since the selection is being set up with ich 5.
			// (That I think gets adjusted to a word boundary, though.)
			m_selHelper.SetupResult("IchAnchor", 0);
			m_selHelper.SetupResultForParams("GetIch", 0, new object[] {SelectionHelper.SelLimitType.Anchor});

			m_selHelper.Expect("IchAnchor", 7);

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_draftView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
			StTxtPara para = SetupParagraph(section.Hvo, "1", null);
			section.AdjustReferences();
			DynamicMock sel = SetupInsertChapterSelection(para, section.Hvo, book.Hvo, 5);

			m_draftView.InsertChapterNumber();
			sel.Verify();
			m_rootBox.Verify();
			m_selHelper.Verify();

			ITsString tss = m_inMemoryCache.CacheAccessor.get_StringProp(para.Hvo,
				kflidParaContent);
			Assert.AreEqual("1This 2is a test paragraph.  It has more \"stuff\" in it. \"I wish,\" said John.",
				tss.Text);
			ITsTextProps tstp = tss.get_Properties(2);
			Assert.AreEqual(ScrStyleNames.ChapterNumber,
				tstp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			Assert.AreEqual(4, tss.RunCount);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a chapter number in the first word of a paragraph.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InFirstWordOfParagraph()
		{
			CheckDisposed();

			m_selHelper.SetupResult("IchAnchor", 0);
			m_selHelper.SetupResultForParams("GetIch", 0, new object[] { SelectionHelper.SelLimitType.Anchor });

			m_selHelper.Expect("IchAnchor", 1);

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_draftView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
			StTxtPara para = SetupParagraph(section.Hvo, "2", "2");
			section.AdjustReferences();

			section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
			para = SetupParagraph(section.Hvo, null, null);
			section.AdjustReferences();

			DynamicMock sel = SetupInsertChapterSelection(para, section.Hvo, book.Hvo, 2);

			m_draftView.InsertChapterNumber();
			sel.Verify();
			m_rootBox.Verify();
			m_selHelper.Verify();

			ITsString tss = m_inMemoryCache.CacheAccessor.get_StringProp(para.Hvo,
				kflidParaContent);
			Assert.AreEqual("1This is a test paragraph.  It has more \"stuff\" in it. \"I wish,\" said John.",
				tss.Text);
			ITsTextProps tstp = tss.get_Properties(0);
			Assert.AreEqual(ScrStyleNames.ChapterNumber,
				tstp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			Assert.AreEqual(2, tss.RunCount);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a chapter number in the first word of a paragraph in the second
		/// section.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InFirstWordOfParagraphSecondSection()
		{
			CheckDisposed();

			m_selHelper.SetupResult("IchAnchor", 0);
			m_selHelper.SetupResultForParams("GetIch", 0, new object[] { SelectionHelper.SelLimitType.Anchor });
			// set up the selection helper to put us in the second section
			SelLevInfo[] selLevInfo = new SelLevInfo[4];
			selLevInfo[3].tag = m_draftView.BookFilter.Tag;
			selLevInfo[2].tag = (int)ScrBook.ScrBookTags.kflidSections;
			selLevInfo[2].ihvo = 1;
			selLevInfo[0].ihvo = 0;
			selLevInfo[0].tag = (int)StText.StTextTags.kflidParagraphs;
			m_selHelper.SetupResult("LevelInfo", selLevInfo);
			m_selHelper.Expect("IchAnchor", 1);

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_draftView.BookFilter.Insert(0, book.Hvo);

			IScrSection section1 = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
			SetupParagraph(section1.Hvo, "1", "1");
			section1.AdjustReferences();

			IScrSection section2 = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
			StTxtPara para = SetupParagraph(section2.Hvo, null, null);
			section2.AdjustReferences();

			DynamicMock sel = SetupInsertChapterSelection(para, 0, section2.Hvo, 1, book.Hvo, 2);

			m_draftView.InsertChapterNumber();
			sel.Verify();
			m_rootBox.Verify();
			m_selHelper.Verify();

			ITsString tss = m_inMemoryCache.CacheAccessor.get_StringProp(para.Hvo,
				kflidParaContent);
			Assert.AreEqual("2This is a test paragraph.  It has more \"stuff\" in it. \"I wish,\" said John.",
				tss.Text);
			ITsTextProps tstp = tss.get_Properties(0);
			Assert.AreEqual(ScrStyleNames.ChapterNumber,
				tstp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			Assert.AreEqual(2, tss.RunCount);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a chapter number at a valid position (end of paragraph).
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void EndOfParagraph()
		{
			CheckDisposed();

			m_selHelper.ExpectAndReturn("IchAnchor", kParagraphText.Length + 2);

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(38, "Zechariah");
			m_draftView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);

			// mock up a selection in a para at ich 0
			StTxtPara para = SetupParagraph(section.Hvo, "1", "1");
			section.AdjustReferences();

			DynamicMock sel = SetupInsertChapterSelection(para, section.Hvo, book.Hvo,
				kParagraphText.Length + 2);

			m_selHelper.Expect("IchAnchor", kParagraphText.Length + 3);

			m_draftView.InsertChapterNumber();
			sel.Verify();
			m_rootBox.Verify();
			m_selHelper.Verify();

			ITsString tss = m_inMemoryCache.CacheAccessor.get_StringProp(para.Hvo,
				kflidParaContent);
			Assert.AreEqual("11This is a test paragraph.  It has more \"stuff\" in it. \"I wish,\" said John.2",
				tss.Text);
			Assert.AreEqual(4, tss.RunCount);
			ITsTextProps tstp = tss.get_Properties(3);
			Assert.AreEqual(ScrStyleNames.ChapterNumber,
				tstp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a chapter number at a valid position (in the second para of a
		/// section).
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InSectionNotFirstPara()
		{
			CheckDisposed();

			m_selHelper.SetupResult("IchAnchor", 0);
			m_selHelper.SetupResultForParams("GetIch", 0, new object[] { SelectionHelper.SelLimitType.Anchor });
			m_selHelper.Expect("IchAnchor", 1); //where IchAnchor is set after our insertion

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_draftView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);

			// create the first paragraph
			StTxtPara para = SetupParagraph(section.Hvo, "1", "1");

			// add a second para to the section
			StTxtPara para2 = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, "Paragraph");
			m_scrInMemoryCache.AddRunToMockedPara(para2, "Another paragraph.", null);
			section.AdjustReferences();

			// mock up a selection in second para at ich 0
			DynamicMock sel = SetupInsertChapterSelection(para2, 1, section.Hvo, 0, book.Hvo, 0);

			// do it
			m_draftView.InsertChapterNumber();

			// verify the mocked objects
			sel.Verify();
			m_rootBox.Verify();
			m_selHelper.Verify();

			// verify that chapter number "2" was inserted
			ITsString tss = m_inMemoryCache.CacheAccessor.get_StringProp(para2.Hvo,
				kflidParaContent);
			Assert.AreEqual("2Another paragraph.", tss.Text);
			Assert.AreEqual(2, tss.RunCount);
			ITsTextProps tstp = tss.get_Properties(0);
			Assert.AreEqual(ScrStyleNames.ChapterNumber,
				tstp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a chapter number at a valid position (start of section, para, book).
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void EmptyBook()
		{
			CheckDisposed();

			m_selHelper.SetupResult("IchAnchor", 0);
			m_selHelper.SetupResultForParams("GetIch", 0, new object[] { SelectionHelper.SelLimitType.Anchor });
			m_selHelper.Expect("IchAnchor", 1);

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_draftView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, "Paragraph");
			section.AdjustReferences();

			// mock up a selection in a para at ich 0
			DynamicMock sel = SetupInsertChapterSelection(para, section.Hvo, book.Hvo, 0);

			m_draftView.InsertChapterNumber();
			sel.Verify();
			m_rootBox.Verify();
			m_selHelper.Verify();

			ITsString tss = m_inMemoryCache.CacheAccessor.get_StringProp(para.Hvo,
				kflidParaContent);
			Assert.AreEqual("1", tss.Text);
			Assert.AreEqual(1, tss.RunCount);
			ITsTextProps tstp = tss.get_Properties(0);
			Assert.AreEqual(ScrStyleNames.ChapterNumber,
				tstp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting in the middle of a word to see if it moves to a word boundary.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void MiddleOfWord()
		{
			CheckDisposed();

			m_selHelper.SetupResult("IchAnchor", 0);
			m_selHelper.SetupResultForParams("GetIch", 0, new object[] { SelectionHelper.SelLimitType.Anchor });
			m_selHelper.Expect("IchAnchor", 6);


			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_draftView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
			StTxtPara para = SetupParagraph(section.Hvo, null, null);
			section.AdjustReferences();

			DynamicMock sel = SetupInsertChapterSelection(para, section.Hvo, book.Hvo, 6);

			m_draftView.InsertChapterNumber();
			sel.Verify();
			m_rootBox.Verify();
			m_selHelper.Verify();

			ITsString tss = m_inMemoryCache.CacheAccessor.get_StringProp(para.Hvo,
				kflidParaContent);
			Assert.AreEqual("This 2is a test paragraph.  It has more \"stuff\" in it. \"I wish,\" said John.",
				tss.Text);
			ITsTextProps tstp = tss.get_Properties(1);
			Assert.AreEqual(ScrStyleNames.ChapterNumber,
				tstp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			Assert.AreEqual(3, tss.RunCount);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests limiting the chapter number to the max for the book.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void OutOfRangeChapterNum()
		{
			CheckDisposed();

			m_selHelper.SetupResult("IchAnchor", 0);
			m_selHelper.SetupResultForParams("GetIch", 0, new object[] { SelectionHelper.SelLimitType.Anchor });
			m_selHelper.Expect("IchAnchor", 2); //where IchAnchor is set after our insertion

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_draftView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);

			// create the first paragraph
			StTxtPara para = SetupParagraph(section.Hvo, "40", "1");

			// add a second para to the section
			StTxtPara para2 = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, "Paragraph");
			m_scrInMemoryCache.AddRunToMockedPara(para2, "Another paragraph.", null);
			section.AdjustReferences();

			// mock up a selection in second para at ich 0
			DynamicMock sel = SetupInsertChapterSelection(para2, 1, section.Hvo, 0, book.Hvo, 0);

			// do it
			m_draftView.InsertChapterNumber();

			// verify the mocked objects
			sel.Verify();
			m_rootBox.Verify();
			m_selHelper.Verify();

			// verify that chapter number "40" was inserted, not "41"!
			ITsString tss = m_inMemoryCache.CacheAccessor.get_StringProp(para2.Hvo,
				kflidParaContent);
			Assert.AreEqual("40Another paragraph.", tss.Text);
			Assert.AreEqual(2, tss.RunCount);
			ITsTextProps tstp = tss.get_Properties(0);
			Assert.AreEqual(ScrStyleNames.ChapterNumber,
				tstp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
		}
	}
	#endregion

	#region BTInsertChapterNumberTests
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Unit tests for inserting chapter numbers in Back translation. These tests use mock
	/// objects and so don't require a real database.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class BTInsertChapterNumberTests : BaseTest
	{
		#region Member variables
		private DummyDraftView m_btView; // mimick back translation view using DummyDraftView
		private IScripture m_scripture;
		private ScrInMemoryFdoCache m_inMemoryCache;
		private DynamicMock m_styleSheet;
		private DynamicMock m_rootBox;
		private DynamicMock m_vwGraphics;
		private DynamicMock m_selHelper;
		private DynamicMock m_sel;
		private object[] m_PropInfoArgs;
		private string[] m_PropInfoTypes;
		private object[] m_TextSelInfoArgs;
		private string[] m_TextSelInfoTypes;
		private object[] m_LocationArgs;
		private string[] m_LocationTypes;
		private object[] m_AllSelEndInfoArgs;
		private string[] m_AllSelEndInfoTypes;
		private const int kflidSectionContent = (int)ScrSection.ScrSectionTags.kflidContent;
		private const int kflidParaContent = (int)StTxtPara.StTxtParaTags.kflidContents;
		private const int kflidTrans = (int)CmTranslation.CmTranslationTags.kflidTranslation;
		#endregion

		#region Setup and Teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Suite setup
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureSetup()
		{
			CheckDisposed();
			base.FixtureSetup();

			string intType = "System.Int32";
			string intRef = intType + "&";
			string boolType = typeof(bool).FullName;
			string boolRef = boolType + "&";
			string rectType = "SIL.FieldWorks.Common.Utils.Rect";
			string rectRef = rectType + "&";

			m_PropInfoArgs = new object[] { false, 0, null, null, null, null, null };
			m_PropInfoTypes =
				new string[] { boolType, intType, intRef, intRef, intRef, intRef,
								 "SIL.FieldWorks.Common.COMInterfaces.IVwPropertyStore&" };
			m_TextSelInfoArgs = new object[] { true, null, null, null, null, null, null };
			m_TextSelInfoTypes =
				new string[] { boolType, "SIL.FieldWorks.Common.COMInterfaces.ITsString&", intRef,
								 boolRef, intRef, intRef, intRef};

			m_LocationArgs =
				new object[] { new IsAnything(), new IsAnything(), new IsAnything(), null,
								 null, null, null };
			m_LocationTypes =
				new string[] { typeof(IVwGraphics).FullName, rectType, rectType, rectRef,
								 rectRef, boolRef, boolRef };

			m_AllSelEndInfoArgs =
				new object[] { new IsAnything(), null, 3, new IsAnything(), null, null,
								 null, null, null, null };
			m_AllSelEndInfoTypes =
				new string[] { boolType, intRef, intType, "SIL.FieldWorks.Common.COMInterfaces.ArrayPtr",
								 intRef, intRef, intRef, intRef, boolRef,
								 "SIL.FieldWorks.Common.COMInterfaces.ITsTextProps&" };

			m_selHelper = new DynamicMock(typeof(SelectionHelper));
			m_selHelper.Strict = true;
			m_selHelper.SetupResult("RestoreSelectionAndScrollPos", true);
			SelectionHelper.s_mockedSelectionHelper = (SelectionHelper)m_selHelper.MockInstance;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureTearDown]
		public void FixtureTeardown()
		{
			SelectionHelper.s_mockedSelectionHelper = null;
		}

		#region IDisposable override

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
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_btView != null)
					m_btView.Dispose();
				if (m_inMemoryCache != null)
					m_inMemoryCache.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_btView = null; // mimick back translation view using DummyDraftView
			m_scripture = null; //m_scripture = null; // Why not set it to null?
			m_inMemoryCache = null;
			m_styleSheet = null; // m_styleSheet = null; // Why not set it to null?
			m_rootBox = null;
			m_vwGraphics = null;
			m_selHelper = null;
			m_sel = null;
			m_PropInfoArgs = null;
			m_PropInfoTypes = null;
			m_TextSelInfoArgs = null;
			m_TextSelInfoTypes = null;
			m_LocationArgs = null;
			m_LocationTypes = null;
			m_AllSelEndInfoArgs = null;
			m_AllSelEndInfoTypes = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Create a dummy Back Translation view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[SetUp]
		public void Init()
		{
			CheckDisposed();

			Debug.Assert(m_inMemoryCache == null, "m_inMemoryCache is not null.");
			//if (m_inMemoryCache != null)
			//	m_inMemoryCache.Dispose();
			m_inMemoryCache = ScrInMemoryFdoCache.Create();
			m_inMemoryCache.InitializeLangProject();
			m_inMemoryCache.InitializeScripture();

			m_scripture = m_inMemoryCache.Cache.LangProject.TranslatedScriptureOA;
			m_inMemoryCache.InitializeActionHandler();

			m_styleSheet = new DynamicMock(typeof(FwStyleSheet));
			m_styleSheet.Strict = true;

			BTInsertVerseAndFootnoteTests.InitializeVwSelection(m_selHelper);

			Debug.Assert(m_btView == null, "m_btView is not null.");
			//if (m_btView != null)
			//	m_btView.Dispose();
			m_btView = new DummyDraftView(m_inMemoryCache.Cache, true, 0);
			m_btView.RootBox = SetupRootBox();
			m_btView.Graphics = SetupGraphics();
			m_btView.MakeRoot();
			m_btView.StyleSheet = (FwStyleSheet)m_styleSheet.MockInstance;
			m_btView.ActivateView();
			m_btView.TeEditingHelper.InTestMode = true;
			m_rootBox.Strict = true;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Close the draft view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[TearDown]
		public void CleanUp()
		{
			CheckDisposed();

			m_btView.Dispose();
			m_btView = null;
			m_inMemoryCache.Dispose();
			m_inMemoryCache = null;
			m_scripture = null;
			m_styleSheet = null;
			m_rootBox = null;
			m_vwGraphics = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Setup the mock rootbox
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private IVwRootBox SetupRootBox()
		{
			m_rootBox = new DynamicMock(typeof(IVwRootBox));
			m_rootBox.SetupResult("SetSite", null, typeof(IVwRootSite));
			m_rootBox.SetupResult("DataAccess", m_inMemoryCache.CacheAccessor);
			m_rootBox.SetupResult("SetRootObject", null, typeof(int), typeof(IVwViewConstructor),
				typeof(int), typeof(IVwStylesheet));
			m_rootBox.SetupResult("Height", 200); // JT: arbitrary values.
			m_rootBox.SetupResult("Width", 200);
			m_rootBox.SetupResult("Site", m_btView);
			m_rootBox.Ignore("Close");
			m_rootBox.Ignore("FlashInsertionPoint");
			m_rootBox.SetupResult("GetRootObject", null,
				new string[] {typeof(int).FullName + "&", typeof(IVwViewConstructor).FullName + "&", typeof(int).FullName + "&", typeof(IVwStylesheet).FullName + "&"},
				new object[] {0, null, 0, null});

			return (IVwRootBox)m_rootBox.MockInstance;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Setup the mock graphics object
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private IVwGraphics SetupGraphics()
		{
			//			m_vwGraphics = new DynamicMock(typeof(IVwGraphics));
			// JT: following three statements replace the one above, following the exmple
			// Eberhard set in InsertChapterNumberTests.
			m_vwGraphics = new DynamicMock(typeof(IVwGraphics), "MockIVwGraphics",
				typeof(DummyGraphics));
			m_vwGraphics.AdditionalReferences = new string[] { "TeDllTests.dll" };
			m_vwGraphics.Strict = true;
			m_vwGraphics.SetupResult("XUnitsPerInch", 96);
			m_vwGraphics.SetupResult("YUnitsPerInch", 96);

			return (IVwGraphics)m_vwGraphics.MockInstance;
		}
		#endregion

		#region Setup Helper functions
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up expected values for mocked back translation selection etc.
		/// </summary>
		/// <param name="hvoTrans"></param>
		/// <returns>Mock selection</returns>
		/// ------------------------------------------------------------------------------------
		private DynamicMock SetupMockedBtSelection(int hvoTrans)
		{
			DynamicMock sel = new DynamicMock(typeof(IVwSelection));
			sel.Strict = true;

			sel.ExpectAndReturn("Commit", true);
			sel.SetupResult("IsValid", true);

			m_selHelper.ExpectAndReturn("ReduceSelectionToIp",
				m_selHelper.MockInstance, SelectionHelper.SelLimitType.Top, false, true);

			ITsPropsBldr builder = TsPropsBldrClass.Create();
			ITsTextProps selProps = builder.GetTextProps();
			m_selHelper.ExpectAndReturn("SelProps", selProps);
			m_selHelper.Expect(3, "AssocPrev", true);
			m_selHelper.Ignore("SetSelection");
			m_selHelper.SetupResult("Selection", sel.MockInstance);
			m_selHelper.ExpectAndReturn("GetTextPropId", (int)CmTranslation.CmTranslationTags.kflidTranslation, new object[] { SelectionHelper.SelLimitType.Top });

			return sel;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set up the mocked selection helper with needed level info
		/// </summary>
		/// <param name="hvoPara">Hvo of paragraph</param>
		/// <param name="hvoTrans">Hvo of back translation</param>
		/// <param name="hvoBook">Hvo of book</param>
		/// <param name="hvoSection">Hvo of section</param>
		/// <param name="wsTrans">Writing system of translation</param>
		/// ------------------------------------------------------------------------------------
		private void SetupSelHelperLevelInfo(int hvoPara, int hvoTrans, int hvoBook,
			int hvoSection, int wsTrans)
		{
			SelLevInfo[] selLevInfo = new SelLevInfo[5];
			selLevInfo[4].tag = m_btView.BookFilter.Tag;
			selLevInfo[4].hvo = hvoBook;
			selLevInfo[3].tag = (int)ScrBook.ScrBookTags.kflidSections;
			selLevInfo[3].ihvo = 0;
			selLevInfo[3].hvo = hvoSection;
			selLevInfo[2].ihvo = 0;
			selLevInfo[2].tag = (int)ScrSection.ScrSectionTags.kflidContent;
			selLevInfo[2].hvo = m_inMemoryCache.Cache.GetOwnerOfObject(hvoPara);
			selLevInfo[1].ihvo = 0;
			selLevInfo[1].hvo = hvoPara;
			selLevInfo[1].tag = (int)StText.StTextTags.kflidParagraphs;
			selLevInfo[0].ihvo = 0;
			selLevInfo[0].hvo = hvoTrans;
			selLevInfo[0].ws = 0; //not useful, but that's what actual selLevelInfo[0] has
			selLevInfo[0].tag = -1; //not useful, but that's what actual selLevelInfo[0] has
			m_selHelper.SetupResult("GetLevelInfo", selLevInfo,
				new Type[] { typeof(SelectionHelper.SelLimitType)});
			m_selHelper.SetupResult("LevelInfo", selLevInfo);
			m_selHelper.SetupResult("NumberOfLevels", 5);
		}

		private void SetupSelForChapterInsert(int ichInitial, ICmTranslation trans, int wsBT)
		{
			m_selHelper.SetupResult("IchAnchor", ichInitial);
			m_selHelper.SetupResult("IsValid", true);

			ITsString tssTrans = trans.Translation.GetAlternative(wsBT).UnderlyingTsString;
			m_sel.SetupResult("SelType", VwSelType.kstText);
			m_sel.SetupResultForParams("TextSelInfo", null, m_TextSelInfoArgs, m_TextSelInfoTypes,
				new object[] { true, tssTrans, ichInitial, false, trans.Hvo, kflidTrans, wsBT });
			m_sel.SetupResultForParams("TextSelInfo", null, new object[] { false, null, null, null, null, null, null },
				m_TextSelInfoTypes, new object[] { false, tssTrans, ichInitial, false, trans.Hvo, kflidTrans, wsBT });
			m_sel.SetupResult("IsRange", false);

			m_rootBox.SetupResult("Selection", m_sel.MockInstance);
		}

		private void SetupSelRangeForChapterInsert(int ichInitial, int ichEnd,
			ICmTranslation trans, int wsBT)
		{
			m_selHelper.SetupResult("IchAnchor", ichInitial);
			m_selHelper.SetupResult("IchEnd", ichEnd);
			m_selHelper.SetupResult("IsValid", true);

			ITsString tssTrans = trans.Translation.GetAlternative(wsBT).UnderlyingTsString;
			m_sel.SetupResult("SelType", VwSelType.kstText);
			m_sel.SetupResultForParams("TextSelInfo", null, m_TextSelInfoArgs, m_TextSelInfoTypes,
				new object[] { true, tssTrans, Math.Min(ichInitial, ichEnd), false, trans.Hvo, kflidTrans, wsBT });
			m_sel.SetupResultForParams("TextSelInfo", null, new object[] { false, null, null, null, null, null, null },
				m_TextSelInfoTypes, new object[] { false, tssTrans, Math.Max(ichInitial, ichEnd), false, trans.Hvo, kflidTrans, wsBT });
			m_sel.SetupResult("IsRange", true);

			m_rootBox.SetupResult("Selection", m_sel.MockInstance);
		}
		#endregion

		#region Tests with vernacular: (C1)(V1)text (V2)text
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test attempts to insert chapter number into a back translation paragraph when the
		/// IP is at the start of the BT (and vernacular has chapter and verse at start).
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InsertChapter_VernC1V1_BtTxt_IpAtStart()
		{
			CheckDisposed();

			IScrBook book = m_inMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_inMemoryCache.AddSectionToMockedBook(book.Hvo);

			// Construct a parent paragraph
			StTxtPara parentPara = m_inMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "1", ScrStyleNames.ChapterNumber);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "1", ScrStyleNames.VerseNumber);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "uno ", null);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "2", ScrStyleNames.VerseNumber);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "dos ", null);
			section.AdjustReferences();

			// Construct the initial back translation
			int wsBT = m_inMemoryCache.Cache.DefaultAnalWs;
			ICmTranslation trans = m_inMemoryCache.AddBtToMockedParagraph(parentPara, wsBT);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, "one two", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara.Hvo, trans.Hvo, book.Hvo, section.Hvo, wsBT);
			m_sel = SetupMockedBtSelection(trans.Hvo);

			// Set IP and Insert first chapter number
			SetupSelForChapterInsert(0, trans, wsBT); //set IP at the start
			m_btView.InsertChapterNumber();

			// Verify the result. We expect to see chapter 1 at "one"
			ITsString tssResult = m_inMemoryCache.CacheAccessor.get_MultiStringAlt(trans.Hvo,
				kflidTrans, wsBT);
			Assert.AreEqual("1one two", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			Assert.AreEqual(2, tssResult.RunCount);
			AssertEx.RunIsCorrect(tssResult, 0, "1", "Chapter Number", wsBT);
			AssertEx.RunIsCorrect(tssResult, 1, "one two", null, wsBT);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test attempts to insert chapter number into a back translation paragraph when the
		/// IP is on the second word of the BT (and vernacular has chapter and verse at start).
		/// Because this is an invalid place to insert a chapter number, nothing should happen.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InsertChapter_VernC1V1_BtTxt_IpAtSecondWord()
		{
			CheckDisposed();

			IScrBook book = m_inMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_inMemoryCache.AddSectionToMockedBook(book.Hvo);

			// Construct a parent paragraph
			StTxtPara parentPara = m_inMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "1", ScrStyleNames.ChapterNumber);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "1", ScrStyleNames.VerseNumber);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "uno ", null);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "2", ScrStyleNames.VerseNumber);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "dos ", null);
			section.AdjustReferences();

			// Construct the initial back translation
			int wsBT = m_inMemoryCache.Cache.DefaultAnalWs;
			ICmTranslation trans = m_inMemoryCache.AddBtToMockedParagraph(parentPara, wsBT);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, "one two", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara.Hvo, trans.Hvo, book.Hvo, section.Hvo, wsBT);
			m_sel = SetupMockedBtSelection(trans.Hvo);

			// Set IP on second verse and insert first verse number
			SetupSelForChapterInsert(4, trans, wsBT); //set IP at start of second word
			m_btView.InsertChapterNumber();

			// Verify the result. We expect nothing to happen.
			ITsString tssResult = m_inMemoryCache.CacheAccessor.get_MultiStringAlt(trans.Hvo,
				kflidTrans, wsBT);
			Assert.AreEqual("one two", tssResult.Text);
			Assert.AreEqual(1, tssResult.RunCount);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test attempts to insert chapter number into a back translation paragraph when the
		/// IP is at the beginning of the BT and the BT has a bogus initial verse number
		/// (and vernacular has chapter and verse at start).
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InsertChapter_VernC1V1_BtV3_IpATStart()
		{
			CheckDisposed();

			IScrBook book = m_inMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_inMemoryCache.AddSectionToMockedBook(book.Hvo);

			// Construct a parent paragraph
			StTxtPara parentPara = m_inMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "1", ScrStyleNames.ChapterNumber);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "1", ScrStyleNames.VerseNumber);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "uno ", null);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "2", ScrStyleNames.VerseNumber);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "dos ", null);
			section.AdjustReferences();

			// Construct the initial back translation
			int wsBT = m_inMemoryCache.Cache.DefaultAnalWs;
			ICmTranslation trans = m_inMemoryCache.AddBtToMockedParagraph(parentPara, wsBT);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, "3", ScrStyleNames.VerseNumber);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, "one two", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara.Hvo, trans.Hvo, book.Hvo, section.Hvo, wsBT);
			m_sel = SetupMockedBtSelection(trans.Hvo);

			// Set IP at beginning of BT, at the bogus verse number
			SetupSelForChapterInsert(0, trans, wsBT);

			m_btView.InsertChapterNumber();

			// Verify the result. We expect to see V3 updated to C1.
			ITsString tssResult = m_inMemoryCache.CacheAccessor.get_MultiStringAlt(trans.Hvo,
				kflidTrans, wsBT);
			Assert.AreEqual("1one two", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			Assert.AreEqual(2, tssResult.RunCount);
			AssertEx.RunIsCorrect(tssResult, 0, "1", "Chapter Number", wsBT);
			AssertEx.RunIsCorrect(tssResult, 1, "one two", null, wsBT);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test attempts to insert chapter number into a back translation paragraph when the
		/// IP is at the beginning of the BT that has chapter 2 at the start.
		/// The vernacular has chapter 1 and verse 1 at start.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InsertChapter_VernC1V1_BtC2_IpAtStart()
		{
			CheckDisposed();

			IScrBook book = m_inMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_inMemoryCache.AddSectionToMockedBook(book.Hvo);

			// Construct a parent paragraph
			StTxtPara parentPara = m_inMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "1", ScrStyleNames.ChapterNumber);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "1", ScrStyleNames.VerseNumber);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "uno ", null);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "2", ScrStyleNames.VerseNumber);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "dos ", null);
			section.AdjustReferences();

			// Construct the initial back translation
			int wsBT = m_inMemoryCache.Cache.DefaultAnalWs;
			ICmTranslation trans = m_inMemoryCache.AddBtToMockedParagraph(parentPara, wsBT);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, "2", ScrStyleNames.ChapterNumber);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, "one two", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara.Hvo, trans.Hvo, book.Hvo, section.Hvo, wsBT);
			m_sel = SetupMockedBtSelection(trans.Hvo);

			// Set IP at the beginning of BT and try to insert chapter number
			SetupSelForChapterInsert(0, trans, wsBT);
			m_btView.InsertChapterNumber();

			// Verify the result. We expect to see C2 updated to C1.
			ITsString tssResult = m_inMemoryCache.CacheAccessor.get_MultiStringAlt(trans.Hvo,
				kflidTrans, wsBT);
			Assert.AreEqual("1one two", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			Assert.AreEqual(2, tssResult.RunCount);
			AssertEx.RunIsCorrect(tssResult, 0, "1", "Chapter Number", wsBT);
			AssertEx.RunIsCorrect(tssResult, 1, "one two", null, wsBT);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test attempts to insert chapter number into a back translation paragraph when the
		/// IP is in the first word of the BT that has chapter 2 at the start.
		/// The vernacular has chapter 1 and verse 1 at start.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InsertChapter_VernC1V1_BtC2Txt_IpInFirstWord()
		{
			CheckDisposed();

			IScrBook book = m_inMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_inMemoryCache.AddSectionToMockedBook(book.Hvo);

			// Construct a parent paragraph
			StTxtPara parentPara = m_inMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "1", ScrStyleNames.ChapterNumber);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "1", ScrStyleNames.VerseNumber);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "uno ", null);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "2", ScrStyleNames.VerseNumber);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "dos ", null);
			section.AdjustReferences();

			// Construct the initial back translation
			int wsBT = m_inMemoryCache.Cache.DefaultAnalWs;
			ICmTranslation trans = m_inMemoryCache.AddBtToMockedParagraph(parentPara, wsBT);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, "2", ScrStyleNames.ChapterNumber);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, "one two", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara.Hvo, trans.Hvo, book.Hvo, section.Hvo, wsBT);
			m_sel = SetupMockedBtSelection(trans.Hvo);

			// Set IP in the first word of BT and try to insert chapter number
			SetupSelForChapterInsert(3, trans, wsBT);
			m_btView.InsertChapterNumber();

			// Verify the result. We expect to see C2 updated to C1.
			ITsString tssResult = m_inMemoryCache.CacheAccessor.get_MultiStringAlt(trans.Hvo,
				kflidTrans, wsBT);
			Assert.AreEqual("1one two", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			Assert.AreEqual(2, tssResult.RunCount);
			AssertEx.RunIsCorrect(tssResult, 0, "1", "Chapter Number", wsBT);
			AssertEx.RunIsCorrect(tssResult, 1, "one two", null, wsBT);
		}
		#endregion

		#region Tests with vernacular: (C1)text (V2)text
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test inserting chapter number into a back translation paragraph when the
		/// IP is near the beginning of the BT.
		/// The vernacular has chapter 1 at start with implied verse number.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InsertChapter_VernC1_BtTxt_IpInFirstWord()
		{
			CheckDisposed();

			IScrBook book = m_inMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_inMemoryCache.AddSectionToMockedBook(book.Hvo);

			// Construct a parent paragraph
			StTxtPara parentPara = m_inMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "1", ScrStyleNames.ChapterNumber);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "uno ", null);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "2", ScrStyleNames.VerseNumber);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "dos ", null);
			section.AdjustReferences();

			// Construct the initial back translation
			int wsBT = m_inMemoryCache.Cache.DefaultAnalWs;
			ICmTranslation trans = m_inMemoryCache.AddBtToMockedParagraph(parentPara, wsBT);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, "one two", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara.Hvo, trans.Hvo, book.Hvo, section.Hvo, wsBT);
			m_sel = SetupMockedBtSelection(trans.Hvo);

			// Set IP in "one" near the beginning of BT
			SetupSelForChapterInsert(2, trans, wsBT);
			m_btView.InsertChapterNumber();

			// Verify the result. We expect C1 to be inserted at the start.
			ITsString tssResult = m_inMemoryCache.CacheAccessor.get_MultiStringAlt(trans.Hvo,
				kflidTrans, wsBT);
			Assert.AreEqual("1one two", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			Assert.AreEqual(2, tssResult.RunCount);
			AssertEx.RunIsCorrect(tssResult, 0, "1", "Chapter Number", wsBT);
			AssertEx.RunIsCorrect(tssResult, 1, "one two", null, wsBT);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test attempts to insert chapter number into a back translation paragraph when the
		/// IP is at the beginning of the BT. A bogus chapter exists later in the BT.
		/// The vernacular has chapter 1 at start with implied verse number.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InsertChapter_VernC1_BtTxtBogusChapter_IpAtStart()
		{
			CheckDisposed();

			IScrBook book = m_inMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_inMemoryCache.AddSectionToMockedBook(book.Hvo);

			// Construct a parent paragraph
			StTxtPara parentPara = m_inMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "1", ScrStyleNames.ChapterNumber);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "uno ", null);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "2", ScrStyleNames.VerseNumber);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "dos ", null);
			section.AdjustReferences();

			// Construct the initial back translation
			int wsBT = m_inMemoryCache.Cache.DefaultAnalWs;
			ICmTranslation trans = m_inMemoryCache.AddBtToMockedParagraph(parentPara, wsBT);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, "one ", null);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, "2", ScrStyleNames.ChapterNumber);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, "two", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara.Hvo, trans.Hvo, book.Hvo, section.Hvo, wsBT);
			m_sel = SetupMockedBtSelection(trans.Hvo);

			// Set IP at beginning of BT
			SetupSelForChapterInsert(0, trans, wsBT);
			m_btView.InsertChapterNumber();

			// Verify the result. We expect C1 to be inserted at the start.
			ITsString tssResult = m_inMemoryCache.CacheAccessor.get_MultiStringAlt(trans.Hvo,
				kflidTrans, wsBT);
			Assert.AreEqual("1one 2two", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			Assert.AreEqual(4, tssResult.RunCount);
			AssertEx.RunIsCorrect(tssResult, 0, "1", "Chapter Number", wsBT);
			AssertEx.RunIsCorrect(tssResult, 1, "one ", null, wsBT);
			AssertEx.RunIsCorrect(tssResult, 2, "2", "Chapter Number", wsBT);
			AssertEx.RunIsCorrect(tssResult, 3, "two", null, wsBT);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test attempts to insert chapter number into a back translation paragraph when the
		/// IP is in front of chapter number of the BT.
		/// The vernacular has chapter 1 at start with implied verse number.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		[Ignore("Analyst isn't sure we want to delete bogus chapter numbers in BT when user clicks Insert Chapter Number")]
		public void DeleteBogusChapter__VernC1Txt_BtTxtC2_IpAfterC2()
		{
			CheckDisposed();

			IScrBook book = m_inMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_inMemoryCache.AddSectionToMockedBook(book.Hvo);

			// Construct a parent paragraph
			StTxtPara parentPara = m_inMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "1", ScrStyleNames.ChapterNumber);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "uno ", null);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "2", ScrStyleNames.VerseNumber);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "dos ", null);
			section.AdjustReferences();

			// Construct the initial back translation
			int wsBT = m_inMemoryCache.Cache.DefaultAnalWs;
			ICmTranslation trans = m_inMemoryCache.AddBtToMockedParagraph(parentPara, wsBT);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, "one", null);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, "2", ScrStyleNames.ChapterNumber);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, "one two", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara.Hvo, trans.Hvo, book.Hvo, section.Hvo, wsBT);
			m_sel = SetupMockedBtSelection(trans.Hvo);

			// Set IP at chapter number in BT
			SetupSelForChapterInsert(4, trans, wsBT);
			m_btView.InsertChapterNumber();

			// Verify the result.
			ITsString tssResult = m_inMemoryCache.CacheAccessor.get_MultiStringAlt(trans.Hvo,
				kflidTrans, wsBT);
			Assert.AreEqual("one two", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			Assert.AreEqual(1, tssResult.RunCount);
			AssertEx.RunIsCorrect(tssResult, 0, "one two", null, wsBT);
		}
		#endregion

		#region Tests with vernacular: (V2)text (V3)text
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test attempts to insert chapter number into a back translation paragraph when the
		/// IP is at the beginning of the BT that begins with chapter 2.
		/// The vernacular has verses two and three, but no chapter number.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		[Ignore("Analyst isn't sure we want to delete bogus chapter numbers in BT when user clicks Insert Chapter Number")]
		public void DeleteBogusChapter__VernV2TxtV3_BtC2Txt_IpAtStart()
		{
			CheckDisposed();

			IScrBook book = m_inMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_inMemoryCache.AddSectionToMockedBook(book.Hvo);

			// Construct a parent paragraph
			StTxtPara parentPara = m_inMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "2", ScrStyleNames.VerseNumber);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "dos ", null);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "3", ScrStyleNames.VerseNumber);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "tres ", null);
			section.AdjustReferences();

			// Construct the initial back translation
			int wsBT = m_inMemoryCache.Cache.DefaultAnalWs;
			ICmTranslation trans = m_inMemoryCache.AddBtToMockedParagraph(parentPara, wsBT);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, "2", ScrStyleNames.ChapterNumber);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, "two three", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara.Hvo, trans.Hvo, book.Hvo, section.Hvo, wsBT);
			m_sel = SetupMockedBtSelection(trans.Hvo);

			// Set IP at the beginning of BT and insert chapter number
			SetupSelForChapterInsert(0, trans, wsBT);
			m_btView.InsertChapterNumber();

			// Verify the result. We expect...
			ITsString tssResult = m_inMemoryCache.CacheAccessor.get_MultiStringAlt(trans.Hvo,
				kflidTrans, wsBT);
			Assert.AreEqual("two three", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			Assert.AreEqual(1, tssResult.RunCount);
			AssertEx.RunIsCorrect(tssResult, 0, "two three", null, wsBT);
		}
		#endregion

		#region Tests with vernacular: text (C2)(V1)text
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test attempts to insert chapter number into a back translation paragraph when the
		/// IP is on the first word of the BT that is all text.
		/// The vernacular begins with text and is followed by (C2)(V1)text.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InsertChapter_VernTxtC2V1_BtTxt_IpInFirstWord()
		{
			CheckDisposed();

			IScrBook book = m_inMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_inMemoryCache.AddSectionToMockedBook(book.Hvo);

			// Construct a parent paragraph
			StTxtPara parentPara = m_inMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "principio ", null);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "2", ScrStyleNames.ChapterNumber);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "1", ScrStyleNames.VerseNumber);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "uno ", null);
			section.AdjustReferences();

			// Construct the initial back translation
			int wsBT = m_inMemoryCache.Cache.DefaultAnalWs;
			ICmTranslation trans = m_inMemoryCache.AddBtToMockedParagraph(parentPara, wsBT);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, "first one", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara.Hvo, trans.Hvo, book.Hvo, section.Hvo, wsBT);
			m_sel = SetupMockedBtSelection(trans.Hvo);

			// Set IP on second verse and insert first verse number
			SetupSelForChapterInsert(1, trans, wsBT); //set IP on second word
			m_btView.InsertChapterNumber();

			// Verify the result. We expect no change.
			ITsString tssResult = m_inMemoryCache.CacheAccessor.get_MultiStringAlt(trans.Hvo,
				kflidTrans, wsBT);
			Assert.AreEqual("first one", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			AssertEx.RunIsCorrect(tssResult, 0, "first one", null, wsBT);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test attempts to insert chapter number into a back translation paragraph when the
		/// IP is on the second word of the BT that is all text.
		/// The vernacular begins with text and is followed by (C2)(V1)text.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InsertChapter_VernTxtC2V1_BtTxt_IpInSecondWord()
		{
			CheckDisposed();

			IScrBook book = m_inMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_inMemoryCache.AddSectionToMockedBook(book.Hvo);

			// Construct a parent paragraph
			StTxtPara parentPara = m_inMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "principio ", null);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "2", ScrStyleNames.ChapterNumber);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "1", ScrStyleNames.VerseNumber);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "uno ", null);
			section.AdjustReferences();

			// Construct the initial back translation
			int wsBT = m_inMemoryCache.Cache.DefaultAnalWs;
			ICmTranslation trans = m_inMemoryCache.AddBtToMockedParagraph(parentPara, wsBT);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, "first one", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara.Hvo, trans.Hvo, book.Hvo, section.Hvo, wsBT);
			m_sel = SetupMockedBtSelection(trans.Hvo);

			// Set IP in second word and insert chapter number
			SetupSelForChapterInsert(8, trans, wsBT);
			m_btView.InsertChapterNumber();

			// Verify the result. We expect C2 to be inserted before "one".
			ITsString tssResult = m_inMemoryCache.CacheAccessor.get_MultiStringAlt(trans.Hvo,
				kflidTrans, wsBT);
			Assert.AreEqual("first 2one", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			Assert.AreEqual(3, tssResult.RunCount);
			AssertEx.RunIsCorrect(tssResult, 0, "first ", null, wsBT);
			AssertEx.RunIsCorrect(tssResult, 1, "2", "Chapter Number", wsBT);
			AssertEx.RunIsCorrect(tssResult, 2, "one", null, wsBT);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test attempts to insert chapter number into a back translation paragraph when the
		/// IP is just after a mid-para verse in the BT.
		/// The vernacular begins with text and is followed by (C2)(V1)text.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InsertChapter_VernTxtC2V1_BtTxtV3_IpAfterV3()
		{
			CheckDisposed();

			IScrBook book = m_inMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_inMemoryCache.AddSectionToMockedBook(book.Hvo);

			// Construct a parent paragraph
			StTxtPara parentPara = m_inMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "principio ", null);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "2", ScrStyleNames.ChapterNumber);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "1", ScrStyleNames.VerseNumber);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "uno ", null);
			section.AdjustReferences();

			// Construct the initial back translation
			int wsBT = m_inMemoryCache.Cache.DefaultAnalWs;
			ICmTranslation trans = m_inMemoryCache.AddBtToMockedParagraph(parentPara, wsBT);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, "first ", null);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, "3", ScrStyleNames.VerseNumber);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, "one", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara.Hvo, trans.Hvo, book.Hvo, section.Hvo, wsBT);
			m_sel = SetupMockedBtSelection(trans.Hvo);

			// Set IP after V3
			SetupSelForChapterInsert(7, trans, wsBT);

			m_btView.InsertChapterNumber();

			// Verify the result. We expect V3 to be updated to C2.
			ITsString tssResult = m_inMemoryCache.CacheAccessor.get_MultiStringAlt(trans.Hvo,
				kflidTrans, wsBT);
			Assert.AreEqual("first 2one", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			Assert.AreEqual(3, tssResult.RunCount);
			AssertEx.RunIsCorrect(tssResult, 0, "first ", null, wsBT);
			AssertEx.RunIsCorrect(tssResult, 1, "2", "Chapter Number", wsBT);
			AssertEx.RunIsCorrect(tssResult, 2, "one", null, wsBT);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test attempts to insert chapter number into a back translation paragraph when the
		/// IP is just after a mid-para chapter in the Bt.
		/// The vernacular begins with text and is followed by (C2)(V1)text.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InsertChapter_VernTxtC2V1_BtTxtC3_IpAfterC3()
		{
			CheckDisposed();

			IScrBook book = m_inMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_inMemoryCache.AddSectionToMockedBook(book.Hvo);

			// Construct a parent paragraph
			StTxtPara parentPara = m_inMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "principio ", null);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "2", ScrStyleNames.ChapterNumber);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "1", ScrStyleNames.VerseNumber);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "uno ", null);
			section.AdjustReferences();

			// Construct the initial back translation
			int wsBT = m_inMemoryCache.Cache.DefaultAnalWs;
			ICmTranslation trans = m_inMemoryCache.AddBtToMockedParagraph(parentPara, wsBT);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, "first ", null);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, "3", ScrStyleNames.ChapterNumber);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, "one", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara.Hvo, trans.Hvo, book.Hvo, section.Hvo, wsBT);
			m_sel = SetupMockedBtSelection(trans.Hvo);

			// Set IP after C3
			SetupSelForChapterInsert(7, trans, wsBT);
			m_btView.InsertChapterNumber();

			// Verify the result.  We expect C3 to be updated to C2.
			ITsString tssResult = m_inMemoryCache.CacheAccessor.get_MultiStringAlt(trans.Hvo,
				kflidTrans, wsBT);
			Assert.AreEqual("first 2one", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			Assert.AreEqual(3, tssResult.RunCount);
			AssertEx.RunIsCorrect(tssResult, 0, "first ", null, wsBT);
			AssertEx.RunIsCorrect(tssResult, 1, "2", "Chapter Number", wsBT);
			AssertEx.RunIsCorrect(tssResult, 2, "one", null, wsBT);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test attempts to insert chapter number into a back translation paragraph when the
		/// IP is between the mid-para chapter 3 and verse 3 of the BT.
		/// The vernacular begins with text and is followed by (C2)(V1)text.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InsertChapter_VernTxtC2V1_BtTxtC3V3_IpBetweenC3V3()
		{
			CheckDisposed();

			IScrBook book = m_inMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_inMemoryCache.AddSectionToMockedBook(book.Hvo);

			// Construct a parent paragraph
			StTxtPara parentPara = m_inMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "principio ", null);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "2", ScrStyleNames.ChapterNumber);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "1", ScrStyleNames.VerseNumber);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "uno ", null);
			section.AdjustReferences();

			// Construct the initial back translation
			int wsBT = m_inMemoryCache.Cache.DefaultAnalWs;
			ICmTranslation trans = m_inMemoryCache.AddBtToMockedParagraph(parentPara, wsBT);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, "first ", null);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, "3", ScrStyleNames.ChapterNumber);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, "3", ScrStyleNames.VerseNumber);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, "one", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara.Hvo, trans.Hvo, book.Hvo, section.Hvo, wsBT);
			m_sel = SetupMockedBtSelection(trans.Hvo);

			// Set IP
			SetupSelForChapterInsert(7, trans, wsBT); //set IP between existing chapter and verse number
			m_btView.InsertChapterNumber();

			// Verify the result. We expect C3V3 to be updated to C2
			ITsString tssResult = m_inMemoryCache.CacheAccessor.get_MultiStringAlt(trans.Hvo,
				kflidTrans, wsBT);
			Assert.AreEqual("first 2one", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			Assert.AreEqual(3, tssResult.RunCount);
			AssertEx.RunIsCorrect(tssResult, 0, "first ", null, wsBT);
			AssertEx.RunIsCorrect(tssResult, 1, "2", "Chapter Number", wsBT);
			AssertEx.RunIsCorrect(tssResult, 2, "one", null, wsBT);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test attempts to insert chapter number into a back translation paragraph when the
		/// IP is before the mid-para chapter 3 and verse 3 of the BT.
		/// The vernacular begins with text and is followed by (C2)(V1)text.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InsertChapter_VernTxtC2V1_BtTxtC3V3_IpBeforeC3V3()
		{
			CheckDisposed();

			IScrBook book = m_inMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_inMemoryCache.AddSectionToMockedBook(book.Hvo);

			// Construct a parent paragraph
			StTxtPara parentPara = m_inMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "principio ", null);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "2", ScrStyleNames.ChapterNumber);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "1", ScrStyleNames.VerseNumber);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "uno ", null);
			section.AdjustReferences();

			// Construct the initial back translation
			int wsBT = m_inMemoryCache.Cache.DefaultAnalWs;
			ICmTranslation trans = m_inMemoryCache.AddBtToMockedParagraph(parentPara, wsBT);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, "first ", null);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, "3", ScrStyleNames.ChapterNumber);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, "3", ScrStyleNames.VerseNumber);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, "one", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara.Hvo, trans.Hvo, book.Hvo, section.Hvo, wsBT);
			m_sel = SetupMockedBtSelection(trans.Hvo);

			// Set IP
			SetupSelForChapterInsert(6, trans, wsBT); //set IP before existing chapter and verse number
			m_btView.InsertChapterNumber();

			// Verify the result. We expect C3V3 to be updated to C2.
			ITsString tssResult = m_inMemoryCache.CacheAccessor.get_MultiStringAlt(trans.Hvo,
				kflidTrans, wsBT);
			Assert.AreEqual("first 2one", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			Assert.AreEqual(3, tssResult.RunCount);
			AssertEx.RunIsCorrect(tssResult, 0, "first ", null, wsBT);
			AssertEx.RunIsCorrect(tssResult, 1, "2", "Chapter Number", wsBT);
			AssertEx.RunIsCorrect(tssResult, 2, "one", null, wsBT);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test attempts to insert chapter number into a back translation paragraph when the
		/// IP is after the mid-para chapter 3 and verse 3 of the BT.
		/// The vernacular begins with text and is followed by (C2)(V1)text.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InsertChapter_VernTxtC2V1_BtTxtC3V3_IpAfterC3V3()
		{
			CheckDisposed();

			IScrBook book = m_inMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_inMemoryCache.AddSectionToMockedBook(book.Hvo);

			// Construct a parent paragraph
			StTxtPara parentPara = m_inMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "principio ", null);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "2", ScrStyleNames.ChapterNumber);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "1", ScrStyleNames.VerseNumber);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "uno ", null);
			section.AdjustReferences();

			// Construct the initial back translation
			int wsBT = m_inMemoryCache.Cache.DefaultAnalWs;
			ICmTranslation trans = m_inMemoryCache.AddBtToMockedParagraph(parentPara, wsBT);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, "first ", null);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, "3", ScrStyleNames.ChapterNumber);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, "3", ScrStyleNames.VerseNumber);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, "one", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara.Hvo, trans.Hvo, book.Hvo, section.Hvo, wsBT);
			m_sel = SetupMockedBtSelection(trans.Hvo);

			// Set IP
			SetupSelForChapterInsert(8, trans, wsBT); //set IP after existing chapter and verse number
			m_btView.InsertChapterNumber();

			// Verify the result. We expect C3V3 to be updated to C2..
			ITsString tssResult = m_inMemoryCache.CacheAccessor.get_MultiStringAlt(trans.Hvo,
				kflidTrans, wsBT);
			Assert.AreEqual("first 2one", tssResult.Text);
			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			Assert.AreEqual(3, tssResult.RunCount);
			AssertEx.RunIsCorrect(tssResult, 0, "first ", null, wsBT);
			AssertEx.RunIsCorrect(tssResult, 1, "2", "Chapter Number", wsBT);
			AssertEx.RunIsCorrect(tssResult, 2, "one", null, wsBT);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test attempts to insert chapter number into a back translation paragraph when the
		/// IP is between chapter and verse number at the start of the BT.
		/// The vernacular begins with text and is followed by (C2)(V1)text.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InsertChapter_VernTxtC2V1_BtC3V1Txt_IpBetweenC3V1()
		{
			CheckDisposed();

			IScrBook book = m_inMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_inMemoryCache.AddSectionToMockedBook(book.Hvo);

			// Construct a parent paragraph
			StTxtPara parentPara = m_inMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "principio ", null);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "2", ScrStyleNames.ChapterNumber);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "1", ScrStyleNames.VerseNumber);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "uno ", null);
			section.AdjustReferences();

			// Construct the initial back translation
			int wsBT = m_inMemoryCache.Cache.DefaultAnalWs;
			ICmTranslation trans = m_inMemoryCache.AddBtToMockedParagraph(parentPara, wsBT);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, "3", ScrStyleNames.ChapterNumber);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, "1", ScrStyleNames.VerseNumber);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, "one", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara.Hvo, trans.Hvo, book.Hvo, section.Hvo, wsBT);
			m_sel = SetupMockedBtSelection(trans.Hvo);

			// Set IP
			SetupSelForChapterInsert(1, trans, wsBT); //set IP between chapter and verse
			m_btView.InsertChapterNumber();

			// Verify the result. We expect no change.
			ITsString tssResult = m_inMemoryCache.CacheAccessor.get_MultiStringAlt(trans.Hvo,
				kflidTrans, wsBT);
			Assert.AreEqual("31one", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			Assert.AreEqual(3, tssResult.RunCount);
			AssertEx.RunIsCorrect(tssResult, 0, "3", "Chapter Number", wsBT);
			AssertEx.RunIsCorrect(tssResult, 1, "1", "Verse Number", wsBT);
			AssertEx.RunIsCorrect(tssResult, 2, "one", null, wsBT);
		}


		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test attempts to insert chapter number into a back translation paragraph when the
		/// IP is after text and before a space before the mid-para chapter and verse numbers.
		/// The vernacular begins with text and is followed by (C2)(V1)text.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InsertChapter_VernTxtC2V1_BtTxtC3V1_IpAtSpaceBeforeC3()
		{
			CheckDisposed();

			IScrBook book = m_inMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_inMemoryCache.AddSectionToMockedBook(book.Hvo);

			// Construct a parent paragraph
			StTxtPara parentPara = m_inMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "principio ", null);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "2", ScrStyleNames.ChapterNumber);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "1", ScrStyleNames.VerseNumber);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "uno ", null);
			section.AdjustReferences();

			// Construct the initial back translation
			int wsBT = m_inMemoryCache.Cache.DefaultAnalWs;
			ICmTranslation trans = m_inMemoryCache.AddBtToMockedParagraph(parentPara, wsBT);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, "text ", null);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, "3", ScrStyleNames.ChapterNumber);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, "1", ScrStyleNames.VerseNumber);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, "one", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara.Hvo, trans.Hvo, book.Hvo, section.Hvo, wsBT);
			m_sel = SetupMockedBtSelection(trans.Hvo);

			// Set IP at the end of "text", with a following space and chapter number
			SetupSelForChapterInsert(4, trans, wsBT);
			m_btView.InsertChapterNumber();

			// Verify the result. We expect C3V1 should be updated to C2.
			ITsString tssResult = m_inMemoryCache.CacheAccessor.get_MultiStringAlt(trans.Hvo,
				kflidTrans, wsBT);
			Assert.AreEqual("text 2one", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			Assert.AreEqual(3, tssResult.RunCount);
			AssertEx.RunIsCorrect(tssResult, 0, "text ", null, wsBT);
			AssertEx.RunIsCorrect(tssResult, 1, "2", "Chapter Number", wsBT);
			AssertEx.RunIsCorrect(tssResult, 2, "one", null, wsBT);
		}
		#endregion

		#region Tests with vernacular: text (C2)text
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test attempts to insert chapter number into a back translation paragraph when the
		/// IP is in the first word of the BT that is all text.
		/// The vernacular begins with text and is followed by (C2)text.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InsertChapter_VernTxtC2_BtTxt_IpInFirstWord()
		{
			CheckDisposed();

			IScrBook book = m_inMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_inMemoryCache.AddSectionToMockedBook(book.Hvo);

			// Construct a parent paragraph
			StTxtPara parentPara = m_inMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "principio ", null);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "2", ScrStyleNames.ChapterNumber);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "uno ", null);
			section.AdjustReferences();

			// Construct the initial back translation
			int wsBT = m_inMemoryCache.Cache.DefaultAnalWs;
			ICmTranslation trans = m_inMemoryCache.AddBtToMockedParagraph(parentPara, wsBT);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, "first one", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara.Hvo, trans.Hvo, book.Hvo, section.Hvo, wsBT);
			m_sel = SetupMockedBtSelection(trans.Hvo);

			// Set IP in first word
			SetupSelForChapterInsert(1, trans, wsBT);

			m_btView.InsertChapterNumber();

			// Verify the result. We expect no change.
			ITsString tssResult = m_inMemoryCache.CacheAccessor.get_MultiStringAlt(trans.Hvo,
				kflidTrans, wsBT);
			Assert.AreEqual("first one", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test inserts chapter number into a back translation paragraph when the
		/// IP is in the second word of the BT that is all text.
		/// The vernacular begins with text and is followed by (C2)text.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InsertChapter_VernTxtC2_BtTxt_IpInSecondWord()
		{
			CheckDisposed();

			IScrBook book = m_inMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_inMemoryCache.AddSectionToMockedBook(book.Hvo);

			// Construct a parent paragraph
			StTxtPara parentPara = m_inMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "principio ", null);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "2", ScrStyleNames.ChapterNumber);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "uno ", null);
			section.AdjustReferences();

			// Construct the initial back translation
			int wsBT = m_inMemoryCache.Cache.DefaultAnalWs;
			ICmTranslation trans = m_inMemoryCache.AddBtToMockedParagraph(parentPara, wsBT);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, "first one", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara.Hvo, trans.Hvo, book.Hvo, section.Hvo, wsBT);
			m_sel = SetupMockedBtSelection(trans.Hvo);

			SetupSelForChapterInsert(8, trans, wsBT); //set IP in second word

			m_btView.InsertChapterNumber();

			// Verify the result. We expect C2 to be inserted before the second word.
			ITsString tssResult = m_inMemoryCache.CacheAccessor.get_MultiStringAlt(trans.Hvo,
				kflidTrans, wsBT);
			Assert.AreEqual("first 2one", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			Assert.AreEqual(3, tssResult.RunCount);
			AssertEx.RunIsCorrect(tssResult, 0, "first ", null, wsBT);
			AssertEx.RunIsCorrect(tssResult, 1, "2", "Chapter Number", wsBT);
			AssertEx.RunIsCorrect(tssResult, 2, "one", null, wsBT);
		}
		#endregion

		#region Tests with initially empty BT
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting one chapter number into an empty back translation paragraph.
		/// The first verse in the vernacular is at start of the paragraph.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void EmptyBt_VernV3Txt_BtEmpty_IpAtStart()
		{
			CheckDisposed();

			IScrBook book = m_inMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_inMemoryCache.AddSectionToMockedBook(book.Hvo);

			// Construct a parent paragraph
			StTxtPara parentPara = m_inMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "3", ScrStyleNames.VerseNumber);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "three.", null);
			section.AdjustReferences();

			// Construct the initial back translation
			int wsBT = m_inMemoryCache.Cache.DefaultAnalWs;
			ICmTranslation trans = m_inMemoryCache.AddBtToMockedParagraph(parentPara, wsBT);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, string.Empty, null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara.Hvo, trans.Hvo, book.Hvo, section.Hvo, wsBT);
			m_sel = SetupMockedBtSelection(trans.Hvo);

			// Attempt to insert chapter number into empty back translation
			SetupSelForChapterInsert(0, trans, wsBT); // Insert at ich 0
			m_btView.InsertChapterNumber();

			m_sel.Verify();
			m_rootBox.Verify();

			// We expect nothing to happen because vern doesn't have a chapter number.
			ITsString tssResult = m_inMemoryCache.CacheAccessor.get_MultiStringAlt(trans.Hvo,
				kflidTrans, wsBT);

			Assert.IsNull(tssResult.Text);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting one chapter number into an empty back translation paragraph.
		/// The first chapter in the vernacular is at start of the paragraph.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void EmptyBt_VernC1Txt_BtEmpty_IpAtStart()
		{
			CheckDisposed();

			IScrBook book = m_inMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_inMemoryCache.AddSectionToMockedBook(book.Hvo);

			// Construct a parent paragraph
			StTxtPara parentPara = m_inMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "1", ScrStyleNames.ChapterNumber);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "three.", null);
			section.AdjustReferences();

			// Construct the initial back translation
			int wsBT = m_inMemoryCache.Cache.DefaultAnalWs;
			ICmTranslation trans = m_inMemoryCache.AddBtToMockedParagraph(parentPara, wsBT);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, string.Empty, null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara.Hvo, trans.Hvo, book.Hvo, section.Hvo, wsBT);
			m_sel = SetupMockedBtSelection(trans.Hvo);

			// Insert chapter number into empty back translation
			SetupSelForChapterInsert(0, trans, wsBT); // Insert at ich 0
			m_btView.InsertChapterNumber();

			// Verify the result. We expect C1 to be inserted into the empty BT.
			ITsString tssResult = m_inMemoryCache.CacheAccessor.get_MultiStringAlt(trans.Hvo,
				kflidTrans, wsBT);
			Assert.AreEqual("1", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			AssertEx.RunIsCorrect(tssResult, 0, "1", "Chapter Number", wsBT);
		}
		#endregion

		#region Tests attempting to insert already existing chapter numbers
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests attempting to insert an already-existing chapter number in the back
		/// translation, when the IP is at the start.
		/// The vernacular begins with (C3) followed by text.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void ExistingChapter_VernC3Txt_BtC3Txt_IpAtStart()
		{
			CheckDisposed();

			IScrBook book = m_inMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_inMemoryCache.AddSectionToMockedBook(book.Hvo);

			// Construct a parent paragraph
			StTxtPara parentPara = m_inMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "3", ScrStyleNames.ChapterNumber);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "three.", null);
			section.AdjustReferences();

			// Construct the initial back translation
			int wsBT = m_inMemoryCache.Cache.DefaultAnalWs;
			ICmTranslation trans = m_inMemoryCache.AddBtToMockedParagraph(parentPara, wsBT);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, "3", ScrStyleNames.ChapterNumber);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, "tres.", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara.Hvo, trans.Hvo, book.Hvo, section.Hvo, wsBT);
			m_sel = SetupMockedBtSelection(trans.Hvo);

			SetupSelForChapterInsert(0, trans, wsBT); // IP at start of para
			m_btView.InsertChapterNumber();

			// Verify the result. We don't expect to see any changes.
			ITsString tssResult = m_inMemoryCache.CacheAccessor.get_MultiStringAlt(trans.Hvo,
				kflidTrans, wsBT);
			Assert.AreEqual("3tres.", tssResult.Text);

			m_sel.Verify();
			m_rootBox.Verify();

			Assert.AreEqual(2, tssResult.RunCount);
			AssertEx.RunIsCorrect(tssResult, 0, "3", "Chapter Number", wsBT);
			AssertEx.RunIsCorrect(tssResult, 1, "tres.", null, wsBT);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests attempting to insert an already existing chapter number in the back
		/// translation, when the Ip is at the end.
		/// The vernacular begins with (C3) followed by text.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void ExistingChapter_VernC3Txt_BtC3Txt_IpAtEnd()
		{
			CheckDisposed();

			IScrBook book = m_inMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_inMemoryCache.AddSectionToMockedBook(book.Hvo);

			// Construct a parent paragraph
			StTxtPara parentPara = m_inMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "3", ScrStyleNames.ChapterNumber);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "three.", null);
			section.AdjustReferences();

			// Construct the initial back translation
			int wsBT = m_inMemoryCache.Cache.DefaultAnalWs;
			ICmTranslation trans = m_inMemoryCache.AddBtToMockedParagraph(parentPara, wsBT);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, "3", ScrStyleNames.ChapterNumber);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, "tres.", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara.Hvo, trans.Hvo, book.Hvo, section.Hvo, wsBT);
			m_sel = SetupMockedBtSelection(trans.Hvo);

			// Set IP and attempt to insert a chapter reference at the end of the BT string
			SetupSelForChapterInsert(6, trans, wsBT);
			m_btView.InsertChapterNumber();

			// Verify the result. We don't expect to see any changes.
			ITsString tssResult = m_inMemoryCache.CacheAccessor.get_MultiStringAlt(trans.Hvo,
				kflidTrans, wsBT);
			Assert.AreEqual("3tres.", tssResult.Text);

			m_sel.Verify();
			m_rootBox.Verify();

			Assert.AreEqual(2, tssResult.RunCount);
			AssertEx.RunIsCorrect(tssResult, 0, "3", "Chapter Number", wsBT);
			AssertEx.RunIsCorrect(tssResult, 1, "tres.", null, wsBT);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests attempting to insert a chapter number at the end of a back translation
		/// when a mid-para chapter number is already present earlier in the BT.
		/// The vernacular begins with text follwed by (C3) and more text.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void ExistingChapter_VernTxtC3Txt_BtTxtC3Txt_IpAtEnd()
		{
			CheckDisposed();

			IScrBook book = m_inMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_inMemoryCache.AddSectionToMockedBook(book.Hvo);

			// Construct a parent paragraph
			StTxtPara parentPara = m_inMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "One. ", null);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "3", ScrStyleNames.ChapterNumber);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "three.", null);
			section.AdjustReferences();

			// Construct the initial back translation
			int wsBT = m_inMemoryCache.Cache.DefaultAnalWs;
			ICmTranslation trans = m_inMemoryCache.AddBtToMockedParagraph(parentPara, wsBT);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, "Uno. ", null);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, "3", ScrStyleNames.ChapterNumber);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, "tres tres.", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara.Hvo, trans.Hvo, book.Hvo, section.Hvo, wsBT);
			m_sel = SetupMockedBtSelection(trans.Hvo);

			// Set IP and attempt to insert a chapter number at the end of the BT string.
			SetupSelForChapterInsert(16, trans, wsBT);
			m_btView.InsertChapterNumber();

			// Verify the result. We don't expect to see any changes.
			ITsString tssResult = m_inMemoryCache.CacheAccessor.get_MultiStringAlt(trans.Hvo,
				kflidTrans, wsBT);
			Assert.AreEqual("Uno. 3tres tres.", tssResult.Text);

			m_sel.Verify();
			m_rootBox.Verify();

			Assert.AreEqual(3, tssResult.RunCount);
			AssertEx.RunIsCorrect(tssResult, 0, "Uno. ", null, wsBT);
			AssertEx.RunIsCorrect(tssResult, 1, "3", "Chapter Number", wsBT);
			AssertEx.RunIsCorrect(tssResult, 2, "tres tres.", null, wsBT);
		}
		#endregion

		#region Tests with a range selection
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test attempts to insert chapter number into a back translation paragraph when we
		/// have a range selection in the BT.
		/// The vernacular begins with (C1) (V1) followed by text.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void RangeSel_VernC1V1Txt_BtTxt()
		{
			CheckDisposed();

			IScrBook book = m_inMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_inMemoryCache.AddSectionToMockedBook(book.Hvo);

			// Construct a parent paragraph
			StTxtPara parentPara = m_inMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "1", ScrStyleNames.ChapterNumber);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "1", ScrStyleNames.VerseNumber);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "uno ", null);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "2", ScrStyleNames.VerseNumber);
			m_inMemoryCache.AddRunToMockedPara(parentPara, "dos ", null);
			section.AdjustReferences();

			// Construct the initial back translation
			int wsBT = m_inMemoryCache.Cache.DefaultAnalWs;
			ICmTranslation trans = m_inMemoryCache.AddBtToMockedParagraph(parentPara, wsBT);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, "one two", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara.Hvo, trans.Hvo, book.Hvo, section.Hvo, wsBT);
			m_sel = SetupMockedBtSelection(trans.Hvo);

			// Set range selection from in the second word to in the first word
			SetupSelRangeForChapterInsert(5, 2, trans, wsBT);

			m_btView.InsertChapterNumber();

			// Verify the result. We expect to see chapter 1 inserted at the start.
			ITsString tssResult = m_inMemoryCache.CacheAccessor.get_MultiStringAlt(trans.Hvo,
				kflidTrans, wsBT);
			Assert.AreEqual("1one two", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			Assert.AreEqual(2, tssResult.RunCount);
			AssertEx.RunIsCorrect(tssResult, 0, "1", "Chapter Number", wsBT);
			AssertEx.RunIsCorrect(tssResult, 1, "one two", null, wsBT);
		}
		#endregion
	}
	#endregion
}

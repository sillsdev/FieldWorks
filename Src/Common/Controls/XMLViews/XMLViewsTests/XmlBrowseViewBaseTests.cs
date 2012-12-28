// Copyright (c) 2012, SIL International. All Rights Reserved.
//
// Distributable under the terms of either the Common Public License or the
// GNU Lesser General Public License, as specified in the LICENSING.txt file.
//
// Original author: MarkS 2012-11-05 XmlBrowseViewBaseTests.cs

using System;
using System.Drawing;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.Utils;

namespace XMLViewsTests
{
	/// <summary/>
	public class FakeBrowseViewer : BrowseViewer
	{
		/// <summary/>
		public FakeBrowseViewer()
		{
			m_scrollBar = new VScrollBar();
			m_configureButton = new Button();
			m_lvHeader = new DhListView(this);
			m_scrollContainer = new BrowseViewScroller(this);

			// When running FieldWorks, the constructor eventually creates an XmlBrowseView and calls AddControl() with it,
			// and adds m_scrollContainer to Controls. Model this so the .Dispose methods can behave the same way when
			// testing as when running FieldWorks.
			Controls.Add(m_scrollContainer);
			m_xbv = new FakeXmlBrowseViewBase(this);
			AddControl(m_xbv);
		}
	}

	/// <summary/>
	public class FakeXmlBrowseViewBase : XmlBrowseViewBase
	{
		/// <summary/>
		public int m_rowCount = 3;

		/// <summary/>
		internal override int RowCount
		{
			get
			{
				return m_rowCount;
			}
		}

		/// <summary/>
		public FakeXmlBrowseViewBase(BrowseViewer bv)
		{
			m_bv = bv;
			m_rootb = new FakeRootBox();
			((FakeRootBox)m_rootb).m_xmlBrowseViewBase = this;
		}

		/// <summary>Unit test helper</summary>
		public void SetRootBox(IVwRootBox newRootBox)
		{
			m_rootb = newRootBox;
		}

		/// <summary>Unit test helper</summary>
		public Size GetScrollRange()
		{
			return ScrollRange;
		}

		/// <summary/>
		public class FakeRootBox : IVwRootBox
		{
			/// <summary/>
			public XmlBrowseViewBase m_xmlBrowseViewBase;

			/// <summary>
			/// null unless manually set
			/// </summary>
			private int? m_height = null;

			#region IVwRootBox methods
			/// <summary>
			/// Height of FakeRootBox, unless changed by test code
			/// </summary>
			public int Height
			{
				set
				{
					m_height = value;
				}

				get
				{
					if (m_height != null)
						return (int)m_height;

					// Measurement when running Flex for single-line row
					int measuredRowHeight = 25;
					return m_xmlBrowseViewBase.RowCount * measuredRowHeight;
				}
			}

			/// <summary/>
			public ISilDataAccess DataAccess
			{
				get;
				set;
			}

			/// <summary/>
			public IVwOverlay Overlay
			{
				get;
				set;
			}

			/// <summary/>
			public IVwSelection Selection
			{
				get { throw new NotImplementedException();}
			}

			/// <summary/>
			public VwSelectionState SelectionState
			{
				get { throw new NotImplementedException();}
			}

			/// <summary/>
			public int Width
			{
				get { throw new NotImplementedException();}
			}

			/// <summary/>
			public IVwRootSite Site
			{
				get { throw new NotImplementedException();}
			}

			/// <summary/>
			public IVwStylesheet Stylesheet
			{
				get { throw new NotImplementedException();}
			}

			/// <summary/>
			public int XdPos
			{
				get { throw new NotImplementedException();}
			}

			/// <summary/>
			public IVwSynchronizer Synchronizer
			{
				get { throw new NotImplementedException();}
			}

			/// <summary/>
			public int MaxParasToScan
			{
				get;
				set;
			}

			/// <summary/>
			public bool IsCompositionInProgress
			{
				get { throw new NotImplementedException();}
			}

			/// <summary/>
			public bool IsPropChangedInProgress
			{
				get { throw new NotImplementedException();}
			}

			/// <summary/>
			public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
			{
				throw new NotImplementedException();
			}

			/// <summary/>
			public void SetSite(IVwRootSite _vrs)
			{
				throw new NotImplementedException();
			}

			/// <summary/>
			public void SetRootObjects(int[] _rghvo, IVwViewConstructor[] _rgpvwvc, int[] _rgfrag, IVwStylesheet _ss, int chvo)
			{
				throw new NotImplementedException();
			}

			/// <summary/>
			public void SetRootObject(int hvo, IVwViewConstructor _vwvc, int frag, IVwStylesheet _ss)
			{
				throw new NotImplementedException();
			}

			/// <summary/>
			public void SetRootVariant(object v, IVwStylesheet _ss, IVwViewConstructor _vwvc, int frag)
			{
				throw new NotImplementedException();
			}

			/// <summary/>
			public void SetRootString(ITsString _tss, IVwStylesheet _ss, IVwViewConstructor _vwvc, int frag)
			{
				throw new NotImplementedException();
			}

			/// <summary/>
			public object GetRootVariant()
			{
				throw new NotImplementedException();
			}

			/// <summary/>
			public void Serialize(System.Runtime.InteropServices.ComTypes.IStream _strm)
			{
				throw new NotImplementedException();
			}

			/// <summary/>
			public void Deserialize(System.Runtime.InteropServices.ComTypes.IStream _strm)
			{
				throw new NotImplementedException();
			}

			/// <summary/>
			public void WriteWpx(System.Runtime.InteropServices.ComTypes.IStream _strm)
			{
				throw new NotImplementedException();
			}

			/// <summary/>
			public void DestroySelection()
			{
				throw new NotImplementedException();
			}

			/// <summary/>
			public IVwSelection MakeTextSelection(int ihvoRoot, int cvlsi, SelLevInfo[] _rgvsli, int tagTextProp, int cpropPrevious, int ichAnchor, int ichEnd, int ws, bool fAssocPrev, int ihvoEnd, ITsTextProps _ttpIns, bool fInstall)
			{
				throw new NotImplementedException();
			}

			/// <summary/>
			public IVwSelection MakeRangeSelection(IVwSelection _selAnchor, IVwSelection _selEnd, bool fInstall)
			{
				throw new NotImplementedException();
			}

			/// <summary/>
			public IVwSelection MakeSimpleSel(bool fInitial, bool fEdit, bool fRange, bool fInstall)
			{
				throw new NotImplementedException();
			}

			/// <summary/>
			public IVwSelection MakeTextSelInObj(int ihvoRoot, int cvsli, SelLevInfo[] _rgvsli, int cvsliEnd, SelLevInfo[] _rgvsliEnd, bool fInitial, bool fEdit, bool fRange, bool fWholeObj, bool fInstall)
			{
				throw new NotImplementedException();
			}

			/// <summary/>
			public IVwSelection MakeSelInObj(int ihvoRoot, int cvsli, SelLevInfo[] _rgvsli, int tag, bool fInstall)
			{
				throw new NotImplementedException();
			}

			/// <summary/>
			public IVwSelection MakeSelAt(int xd, int yd, Rect rcSrc, Rect rcDst, bool fInstall)
			{
				throw new NotImplementedException();
			}

			/// <summary/>
			public IVwSelection MakeSelInBox(IVwSelection _selInit, bool fEndPoint, int iLevel, int iBox, bool fInitial, bool fRange, bool fInstall)
			{
				throw new NotImplementedException();
			}

			/// <summary/>
			public bool get_IsClickInText(int xd, int yd, Rect rcSrc, Rect rcDst)
			{
				throw new NotImplementedException();
			}

			/// <summary/>
			public bool get_IsClickInObject(int xd, int yd, Rect rcSrc, Rect rcDst, out int _odt)
			{
				throw new NotImplementedException();
			}

			/// <summary/>
			public bool get_IsClickInOverlayTag(int xd, int yd, Rect rcSrc1, Rect rcDst1, out int _iGuid, out string _bstrGuids, out Rect _rcTag, out Rect _rcAllTags, out bool _fOpeningTag)
			{
				throw new NotImplementedException();
			}

			/// <summary/>
			public void OnTyping(IVwGraphics _vg, string bstrInput, VwShiftStatus ss, ref int _wsPending)
			{
				throw new NotImplementedException();
			}

			/// <summary/>
			public void DeleteRangeIfComplex(IVwGraphics _vg, out bool _fWasComplex)
			{
				throw new NotImplementedException();
			}

			/// <summary/>
			public void OnChar(int chw)
			{
				throw new NotImplementedException();
			}

			/// <summary/>
			public void OnSysChar(int chw)
			{
				throw new NotImplementedException();
			}

			/// <summary/>
			public int OnExtendedKey(int chw, VwShiftStatus ss, int nFlags)
			{
				throw new NotImplementedException();
			}

			/// <summary/>
			public void FlashInsertionPoint()
			{
				throw new NotImplementedException();
			}

			/// <summary/>
			public void MouseDown(int xd, int yd, Rect rcSrc, Rect rcDst)
			{
				throw new NotImplementedException();
			}

			/// <summary/>
			public void MouseDblClk(int xd, int yd, Rect rcSrc, Rect rcDst)
			{
				throw new NotImplementedException();
			}

			/// <summary/>
			public void MouseMoveDrag(int xd, int yd, Rect rcSrc, Rect rcDst)
			{
				throw new NotImplementedException();
			}

			/// <summary/>
			public void MouseDownExtended(int xd, int yd, Rect rcSrc, Rect rcDst)
			{
				throw new NotImplementedException();
			}

			/// <summary/>
			public void MouseUp(int xd, int yd, Rect rcSrc, Rect rcDst)
			{
				throw new NotImplementedException();
			}

			/// <summary/>
			public void Activate(VwSelectionState vss)
			{
				throw new NotImplementedException();
			}

			/// <summary/>
			public VwPrepDrawResult PrepareToDraw(IVwGraphics _vg, Rect rcSrc, Rect rcDst)
			{
				throw new NotImplementedException();
			}

			/// <summary/>
			public void DrawRoot(IVwGraphics _vg, Rect rcSrc, Rect rcDst, bool fDrawSel)
			{
				throw new NotImplementedException();
			}

			/// <summary/>
			public void Layout(IVwGraphics _vg, int dxsAvailWidth)
			{
				throw new NotImplementedException();
			}

			/// <summary/>
			public void InitializePrinting(IVwPrintContext _vpc)
			{
				throw new NotImplementedException();
			}

			/// <summary/>
			public int GetTotalPrintPages(IVwPrintContext _vpc)
			{
				throw new NotImplementedException();
			}

			/// <summary/>
			public void PrintSinglePage(IVwPrintContext _vpc, int nPageNo)
			{
				throw new NotImplementedException();
			}

			/// <summary/>
			public bool LoseFocus()
			{
				throw new NotImplementedException();
			}

			/// <summary/>
			public void Close()
			{

			}

			/// <summary/>
			public void Reconstruct()
			{
				throw new NotImplementedException();
			}

			/// <summary/>
			public void OnStylesheetChange()
			{
				throw new NotImplementedException();
			}

			/// <summary/>
			public void DrawingErrors(IVwGraphics _vg)
			{
				throw new NotImplementedException();
			}

			/// <summary/>
			public void SetTableColWidths(VwLength[] _rgvlen, int cvlen)
			{
				throw new NotImplementedException();
			}

			/// <summary/>
			public bool IsDirty()
			{
				throw new NotImplementedException();
			}

			/// <summary/>
			public void GetRootObject(out int _hvo, out IVwViewConstructor _pvwvc, out int _frag, out IVwStylesheet _pss)
			{
				throw new NotImplementedException();
			}

			/// <summary/>
			public void DrawRoot2(IVwGraphics _vg, Rect rcSrc, Rect rcDst, bool fDrawSel, int ysTop, int dysHeight)
			{
				throw new NotImplementedException();
			}

			/// <summary/>
			public void SetKeyboardForWs(ILgWritingSystem _ws, ref string _bstrActiveKeymanKbd, ref int _nActiveLangId, ref int _hklActive, ref bool _fSelectLangPending)
			{
				throw new NotImplementedException();
			}

			/// <summary/>
			public bool DoSpellCheckStep()
			{
				throw new NotImplementedException();
			}

			/// <summary/>
			public bool IsSpellCheckComplete()
			{
				throw new NotImplementedException();
			}

			/// <summary/>
			public void RestartSpellChecking()
			{
				throw new NotImplementedException();
			}
			#endregion IVwRootBox methods
		}
	}

	/// <summary/>
	[TestFixture]
	public class XmlBrowseViewBaseTests : MemoryOnlyBackendProviderTestBase
	{
		private FakeXmlBrowseViewBase m_view;

		/// <summary/>
		[SetUp]
		public void SetUp()
		{
			var bv = new FakeBrowseViewer();
			m_view = bv.m_xbv as FakeXmlBrowseViewBase;

			ConfigureScrollBars();
		}

		private void ConfigureScrollBars()
		{
			// AdjustControls ends up getting called when running Flex
			ReflectionHelper.CallMethod(m_view.m_bv, "AdjustControls");

			var sizeRequestedBySRSUpdateScrollRange = new Size(m_view.AutoScrollMinSize.Width, m_view.GetScrollRange().Height);
			m_view.ScrollMinSize = sizeRequestedBySRSUpdateScrollRange;
		}

		/// <summary/>
		[TearDown]
		public void TearDown()
		{
			m_view.m_bv.Dispose();
		}

		/// <summary>
		/// Unit test helper to avoided repeated code
		/// </summary>
		private int CalculateMaxScrollBarHeight(int maxUserReachable, int largeChange)
		{
			// http://msdn.microsoft.com/en-us/library/vstudio/system.windows.forms.scrollbar.maximum
			return maxUserReachable + largeChange - 1;
		}

		/// <summary/>
		[Test]
		public void ScrollPosition_SetToSensibleValue_Allowed()
		{
			m_view.m_rowCount = 100;
			ConfigureScrollBars();

			var input = new Point(0, 0);
			var expected = new Point(0, -input.Y);
			m_view.ScrollPosition = input;
			var output = m_view.ScrollPosition;
			Assert.That(output, Is.EqualTo(expected));
			Assert.That(m_view.m_bv.ScrollBar.Value, Is.EqualTo(-expected.Y));

			input = new Point(10, 10);
			expected = new Point(0, -input.Y);
			m_view.ScrollPosition = input;
			output = m_view.ScrollPosition;
			Assert.That(output, Is.EqualTo(expected));
			Assert.That(m_view.m_bv.ScrollBar.Value, Is.EqualTo(-expected.Y));
		}

		/// <summary/>
		[Test]
		public void ScrollPosition_SetToTooLargeValue_Limited()
		{
			var input = new Point(200, 200);
			Assert.That(200, Is.GreaterThan(m_view.ScrollPositionMaxUserReachable), "Unit test bad assumption");

			int desiredMaxUserReachable = m_view.ScrollPositionMaxUserReachable;
			var expected = new Point(0, -desiredMaxUserReachable);

			m_view.ScrollPosition = input;
			var output = m_view.ScrollPosition;

			Assert.That(output, Is.EqualTo(expected));
			Assert.That(output.Y, Is.EqualTo(-m_view.ScrollPositionMaxUserReachable), "Should have been set to the max value that could be set");
			Assert.That(m_view.m_bv.ScrollBar.Value, Is.EqualTo(-expected.Y), "Should have been set to the max value that could be set");
			Assert.That(m_view.m_bv.ScrollBar.Value, Is.EqualTo(m_view.ScrollPositionMaxUserReachable), "Should have been set to the max value that could be set");
		}

		/// <summary/>
		[Test]
		public void ScrollPosition_SetToNegativeValue_ResetToTop()
		{
			m_view.m_rowCount = 100;
			ConfigureScrollBars();

			// Starting at 0, trying to scroll up to -200

			var input = new Point(-200, -200);
			var expected = new Point(0, 0);
			m_view.ScrollPosition = input;
			var output = m_view.ScrollPosition;
			Assert.That(output, Is.EqualTo(expected));
			Assert.That(m_view.m_bv.ScrollBar.Value, Is.EqualTo(-expected.Y));

			// Continuing from 0, trying to scroll down to 10

			input = new Point(10, 10);
			expected = new Point(0, -input.Y);
			m_view.ScrollPosition = input;
			output = m_view.ScrollPosition;
			Assert.That(output, Is.EqualTo(expected));
			Assert.That(m_view.m_bv.ScrollBar.Value, Is.EqualTo(-expected.Y));

			// Continuing from 10, trying to scroll up to -200

			input = new Point(-200, -200);
			expected = new Point(0, 0);
			m_view.ScrollPosition = input;
			output = m_view.ScrollPosition;
			Assert.That(output, Is.EqualTo(expected));
			Assert.That(m_view.m_bv.ScrollBar.Value, Is.EqualTo(-expected.Y));
		}

		/// <remarks/>
		[Test]
		public void ScrollBar_SensibleLargeChange()
		{
			int desiredLargeChangeValue = m_view.ClientHeight - m_view.MeanRowHeight;
			Assert.That(m_view.m_bv.ScrollBar.LargeChange, Is.EqualTo(desiredLargeChangeValue));
		}

		/// <summary>
		/// </summary>
		[Test]
		public void ScrollBar_SensibleLargeChangeWhenNoRows()
		{
			m_view.m_rowCount = 0;
			ConfigureScrollBars();
			int desiredLargeChangeValue = m_view.ClientHeight - m_view.MeanRowHeight;
			Assert.That(m_view.m_bv.ScrollBar.LargeChange, Is.EqualTo(desiredLargeChangeValue));
		}

		/// <summary/>
		[Test]
		public void ScrollBar_SensibleLargeChange_AfterLazyExpansion()
		{
			m_view.m_rowCount = 10000;
			int aSensibleRowHeight = 25;
			// Content not fully expanded to begin with
			(m_view.RootBox as FakeXmlBrowseViewBase.FakeRootBox).Height = aSensibleRowHeight * (m_view.RowCount - 500);
			ConfigureScrollBars();

			// We'll assume we are working with a display for records that is at least bigger than the average height of a record
			Assert.That(m_view.ClientHeight, Is.GreaterThan(m_view.MeanRowHeight));

			int contentHeight = m_view.RootBox.Height;
			Assert.That(contentHeight, Is.EqualTo(aSensibleRowHeight * (m_view.RowCount - 500)), "Unit test not set up right");

			int desiredLargeChangeValue = m_view.ClientHeight - m_view.MeanRowHeight;
			Assert.That(m_view.m_bv.ScrollBar.LargeChange, Is.EqualTo(desiredLargeChangeValue), "Incorrect LargeChange before expansion");

			// Lazy boxes expand
			(m_view.RootBox as FakeXmlBrowseViewBase.FakeRootBox).Height = aSensibleRowHeight * (m_view.RowCount - 0);
			var expandedSizeRequestedBySRSUpdateScrollRange = new Size(m_view.AutoScrollMinSize.Width, m_view.GetScrollRange().Height);
			m_view.ScrollMinSize = expandedSizeRequestedBySRSUpdateScrollRange;

			desiredLargeChangeValue = m_view.ClientHeight - m_view.MeanRowHeight;
			Assert.That(m_view.m_bv.ScrollBar.LargeChange, Is.EqualTo(desiredLargeChangeValue), "Incorrect LargeChange after expansion");
		}


		/// <summary/>
		[Test]
		public void ScrollBar_SensibleSmallChange()
		{
			Assert.That(m_view.m_bv.ScrollBar.SmallChange, Is.EqualTo(m_view.MeanRowHeight));
		}

		/// <summary/>
		[Test]
		public void ScrollBar_SensibleSmallChangeWhenZeroRows()
		{
			m_view.m_rowCount = 0;
			ConfigureScrollBars();
			Assert.That(m_view.m_bv.ScrollBar.SmallChange, Is.EqualTo(m_view.MeanRowHeight));
		}

		/// <summary/>
		[Test]
		public void ScrollBar_SensibleSmallChange_AfterLazyExpansion()
		{
			m_view.m_rowCount = 10000;
			int aSensibleRowHeight = 25;
			// Content not fully expanded to begin with
			(m_view.RootBox as FakeXmlBrowseViewBase.FakeRootBox).Height = aSensibleRowHeight * (m_view.RowCount - 500);
			ConfigureScrollBars();

			// We'll assume we are working with a display for records that is at least bigger than the average height of a record
			Assert.That(m_view.ClientHeight, Is.GreaterThan(m_view.MeanRowHeight));

			int contentHeight = m_view.RootBox.Height;
			Assert.That(contentHeight, Is.EqualTo(aSensibleRowHeight * (m_view.RowCount - 500)), "Unit test not set up right");

			int initialMeanRowHeight = m_view.MeanRowHeight;
			Assert.That(m_view.m_bv.ScrollBar.SmallChange, Is.EqualTo(m_view.MeanRowHeight), "Incorrect SmallChange before expansion");

			// Lazy boxes expand
			(m_view.RootBox as FakeXmlBrowseViewBase.FakeRootBox).Height = aSensibleRowHeight * (m_view.RowCount - 0);
			var expandedSizeRequestedBySRSUpdateScrollRange = new Size(m_view.AutoScrollMinSize.Width, m_view.GetScrollRange().Height);
			m_view.ScrollMinSize = expandedSizeRequestedBySRSUpdateScrollRange;
			Assert.That(initialMeanRowHeight, Is.Not.EqualTo(m_view.MeanRowHeight), "Unit test not set up right");

			Assert.That(m_view.m_bv.ScrollBar.SmallChange, Is.EqualTo(m_view.MeanRowHeight), "Incorrect SmallChange after expansion");
		}

		/// <summary>
		/// When height of all rows is less than m_view.ClientHeight
		/// </summary>
		[Test]
		public void ScrollBar_HasSensibleMaximumWhenFewRows()
		{
			m_view.m_rowCount = 2;
			ConfigureScrollBars();

			// Display of records is definitely bigger than the height of the content
			Assert.That(m_view.ClientHeight, Is.GreaterThan(m_view.MeanRowHeight * (m_view.RowCount + 1)), "Unit test not set up right");

			int desiredMaxUserReachable = m_view.ScrollPositionMaxUserReachable;
			int desiredMaxScrollbarHeight = CalculateMaxScrollBarHeight(desiredMaxUserReachable, m_view.m_bv.ScrollBar.LargeChange);
			Assert.That(m_view.m_bv.ScrollBar.Maximum, Is.EqualTo(desiredMaxScrollbarHeight));
		}

		/// <summary>
		/// Many rows higher than the m_view.ClientHeight.
		/// An ideal maximum would allow scrolling through so all rows are shown, and not any blank space at the end (unless
		/// the m_view.ClientHeight is taller than the RootBox.Height).
		/// </summary>
		[Test]
		public void ScrollBar_HasSensibleMaximumWhenManyRows()
		{
			m_view.m_rowCount = 10000;
			ConfigureScrollBars();

			// We'll assume we are working with a display of records that is at least bigger than the average height of a record
			Assert.That(m_view.ClientHeight, Is.GreaterThan(m_view.MeanRowHeight), "Bad unit test assumption");

			Assert.That(m_view.RootBox.Height, Is.GreaterThan(m_view.ClientHeight), "Unit test not set up right");

			int desiredMaxUserReachable = m_view.ScrollPositionMaxUserReachable;

			int desiredMaxScrollbarHeight = CalculateMaxScrollBarHeight(desiredMaxUserReachable, m_view.m_bv.ScrollBar.LargeChange);
			Assert.That(m_view.m_bv.ScrollBar.Maximum, Is.EqualTo(desiredMaxScrollbarHeight));
		}

		/// <summary>
		/// Would probably be acceptable with either the normal formula or 0.
		/// </summary>
		[Test]
		public void ScrollBar_HasSensibleMaximumWhenZeroRows()
		{
			m_view.m_rowCount = 0;
			ConfigureScrollBars();

			Assert.That(m_view.RootBox.Height, Is.EqualTo(0), "Mistake in unit test");
			Assert.That(m_view.MeanRowHeight, Is.EqualTo(0), "Mistake in unit test");
			int desiredMaxUserReachable = m_view.ScrollPositionMaxUserReachable;

			int desiredMaxScrollbarHeight = CalculateMaxScrollBarHeight(desiredMaxUserReachable, m_view.m_bv.ScrollBar.LargeChange);

			Assert.That(m_view.m_bv.ScrollBar.Maximum, Is.EqualTo(desiredMaxScrollbarHeight));
		}


		/// <summary/>
		[Test]
		public void MeanRowHeight_Normal()
		{
			// Measurement when running Flex for single-line row
			int expectedHeight = 25;
			Assert.That(m_view.MeanRowHeight, Is.EqualTo(expectedHeight));
		}

		/// <summary/>
		[Test]
		public void MeanRowHeight_WhenNoRows()
		{
			m_view.m_rowCount = 0;
			int expectedHeight = 0;
			Assert.That(m_view.MeanRowHeight, Is.EqualTo(expectedHeight));
		}

		/// <summary/>
		[Test]
		public void MeanRowHeight_WhenNullRootBox()
		{
			m_view.SetRootBox(null);
			int expectedHeight = 0;
			Assert.That(m_view.MeanRowHeight, Is.EqualTo(expectedHeight));
		}

		/// <summary/>
		[Test]
		public void ScrollPositionMaxUserReachable_WhenManyRows()
		{
			m_view.m_rowCount = 10000;
			ConfigureScrollBars();

			int contentHeight = m_view.RootBox.Height;
			int desiredMaxUserReachable = contentHeight - m_view.ClientHeight;

			Assert.That(m_view.ScrollPositionMaxUserReachable, Is.EqualTo(desiredMaxUserReachable));
		}

		/// <summary/>
		[Test]
		public void ScrollPositionMaxUserReachable_WhenFewRows()
		{
			m_view.m_rowCount = 2;
			ConfigureScrollBars();

			Assert.That(m_view.ClientHeight, Is.GreaterThan((m_view.RowCount + 1) * m_view.MeanRowHeight), "Unit test not set up right");
			int desiredMaxUserReachable = 0;
			Assert.That(m_view.ScrollPositionMaxUserReachable, Is.EqualTo(desiredMaxUserReachable));
		}

		/// <summary/>
		[Test]
		public void ScrollPositionMaxUserReachable_WhenNoRows()
		{
			m_view.m_rowCount = 0;
			ConfigureScrollBars();

			int contentHeight = m_view.RootBox.Height;
			Assert.That(contentHeight, Is.EqualTo(0), "Problem with unit test");

			Assert.That(m_view.ScrollPositionMaxUserReachable, Is.EqualTo(0));
		}

		/// <summary>
		/// Test Scrollbar response to lazy expansion of content.
		/// </summary>
		[Test]
		public void LazyExpansion_UpdatesMaximums()
		{
			m_view.m_rowCount = 10000;
			int aSensibleRowHeight = 25;
			// Content not fully expanded to begin with
			(m_view.RootBox as FakeXmlBrowseViewBase.FakeRootBox).Height = aSensibleRowHeight * (m_view.RowCount - 500);
			ConfigureScrollBars();

			// We'll assume we are working with a display for records that is at least bigger than the average height of a record
			Assert.That(m_view.ClientHeight, Is.GreaterThan(m_view.MeanRowHeight));

			int contentHeight = m_view.RootBox.Height;
			Assert.That(contentHeight, Is.EqualTo(aSensibleRowHeight * (m_view.RowCount - 500)), "Unit test not set up right");
			int desiredMaxUserReachable = contentHeight - m_view.ClientHeight;
			int desiredMaxScrollbarHeight = CalculateMaxScrollBarHeight(desiredMaxUserReachable, m_view.m_bv.ScrollBar.LargeChange);

			Assert.That(m_view.ScrollPositionMaxUserReachable, Is.EqualTo(desiredMaxUserReachable));
			Assert.That(m_view.m_bv.ScrollBar.Maximum, Is.EqualTo(desiredMaxScrollbarHeight));

			// Lazy boxes expand
			(m_view.RootBox as FakeXmlBrowseViewBase.FakeRootBox).Height = aSensibleRowHeight * (m_view.RowCount - 0);
			var expandedSizeRequestedBySRSUpdateScrollRange = new Size(m_view.AutoScrollMinSize.Width, m_view.GetScrollRange().Height);
			m_view.ScrollMinSize = expandedSizeRequestedBySRSUpdateScrollRange;

			Assert.That(m_view.MeanRowHeight, Is.EqualTo(aSensibleRowHeight), "Expecting MeanRowHeight to be aSensibleRowHeight by now.");

			contentHeight = m_view.RootBox.Height;
			desiredMaxUserReachable = contentHeight - m_view.ClientHeight;
			desiredMaxScrollbarHeight = CalculateMaxScrollBarHeight(desiredMaxUserReachable, m_view.m_bv.ScrollBar.LargeChange);

			// Both ScrollBar.Maximum and the setting-bounds of XmlBrowseViewBase.ScrollPosition should reflect an
			// increased RootBox size.
			Assert.That(m_view.ScrollPositionMaxUserReachable, Is.EqualTo(desiredMaxUserReachable));
			Assert.That(m_view.m_bv.ScrollBar.Maximum, Is.EqualTo(desiredMaxScrollbarHeight));
		}

		/// <summary/>
		[Test]
		public void ScrollRange_IsDesiredScrollBarMaxWhenManyRows()
		{
			m_view.m_rowCount = 10000;
			ConfigureScrollBars();
			int desiredMaxUserReachable = m_view.ScrollPositionMaxUserReachable;
			int desiredMaxScrollbarHeight = CalculateMaxScrollBarHeight(desiredMaxUserReachable, m_view.m_bv.ScrollBar.LargeChange);
			int output = m_view.GetScrollRange().Height;
			Assert.That(output, Is.EqualTo(desiredMaxScrollbarHeight));
		}

		/// <summary/>
		[Test]
		public void ScrollRange_IsDesiredScrollBarMaxWhenFewRows()
		{
			m_view.m_rowCount = 2;
			ConfigureScrollBars();
			int desiredMaxUserReachable = m_view.ScrollPositionMaxUserReachable;
			int desiredMaxScrollbarHeight = CalculateMaxScrollBarHeight(desiredMaxUserReachable, m_view.m_bv.ScrollBar.LargeChange);
			int output = m_view.GetScrollRange().Height;
			Assert.That(output, Is.EqualTo(desiredMaxScrollbarHeight));
		}

		/// <summary/>
		[Test]
		public void ScrollRange_IsDesiredScrollBarMaxWhenZeroRows()
		{
			m_view.m_rowCount = 0;
			ConfigureScrollBars();
			int desiredMaxUserReachable = m_view.ScrollPositionMaxUserReachable;
			int desiredMaxScrollbarHeight = CalculateMaxScrollBarHeight(desiredMaxUserReachable, m_view.m_bv.ScrollBar.LargeChange);
			int output = m_view.GetScrollRange().Height;
			Assert.That(output, Is.EqualTo(desiredMaxScrollbarHeight));
		}

		/// <summary/>
		[Test]
		public void ScrollMinSize_SettingSetsScrollBarMaximumToSame()
		{
			int height = 456;
			m_view.ScrollMinSize = new Size(123, height);
			Assert.That(m_view.m_bv.ScrollBar.Maximum, Is.EqualTo(height));
		}
	}
}
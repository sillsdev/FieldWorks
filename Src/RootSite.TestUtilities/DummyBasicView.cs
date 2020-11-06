// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using FieldWorks.TestUtilities;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.KernelInterfaces;

namespace RootSite.TestUtilities
{
	/// <summary>
	/// Implementation of a basic view for testing, similar to DraftView
	/// </summary>
	internal sealed class DummyBasicView : SIL.FieldWorks.Common.RootSites.RootSite
	{
		#region Data members
		/// <summary>Text for the first and third test paragraph (English)</summary>
		internal const string kFirstParaEng = "This is the first test paragraph.";
		/// <summary>Text for the second and fourth test paragraph (English).</summary>
		/// <remarks>This text needs to be shorter than the text for the first para!</remarks>
		internal const string kSecondParaEng = "This is the 2nd test paragraph.";
		private int m_hvoRoot;
		private int m_flid;
		private Point m_scrollPosition = new Point(0, 0);
		#endregion

		#region Constructor, Dispose, InitializeComponent

		/// <summary />
		internal DummyBasicView() : base(null)
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
		}

		/// <summary />
		internal DummyBasicView(int hvoRoot, int flid) : base(null)
		{
			m_hvoRoot = hvoRoot;
			m_flid = flid;

			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			base.Dispose(disposing);

			if (disposing)
			{
				var disposable = ViewConstructor as IDisposable;
				disposable?.Dispose();
			}
			ViewConstructor = null;
			SelectionHelper = null;
		}

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.SuspendLayout();
			//
			// DummyBasicView
			//
			this.Name = "DummyBasicView";
			this.ResumeLayout(false);

		}
		#endregion
		#endregion

		#region Event handling methods
		/// <summary>
		/// Activates the view
		/// </summary>
		internal void ActivateView()
		{
			PerformLayout();
			Show();
			Focus();
		}

		/// <summary>
		/// Simulate a scroll to end
		/// </summary>
		public override void ScrollToEnd()
		{
			base.ScrollToEnd();
			RootBox.MakeSimpleSel(false, true, false, true);
			PerformLayout();
		}

		/// <summary>
		/// Simulate scrolling to top
		/// </summary>
		public override void ScrollToTop()
		{
			base.ScrollToTop();
			// The actual DraftView code for handling Ctrl-Home doesn't contain this method call.
			// The call to CallOnExtendedKey() in OnKeyDown() handles setting the IP.
			RootBox.MakeSimpleSel(true, true, false, true);
		}

		/// <summary>
		/// Calls the OnKeyDown method.
		/// </summary>
		internal void CallOnKeyDown(KeyEventArgs e)
		{
			OnKeyDown(e);
		}
		#endregion

		#region Overrides of Control methods
		/// <summary>
		/// Focus got set to the draft view
		/// </summary>
		protected override void OnGotFocus(EventArgs e)
		{
			base.OnGotFocus(e);

			if (DesignMode || !m_fRootboxMade)
			{
				return;
			}
			if (SelectionHelper != null)
			{
				SelectionHelper.SetSelection(this);
				SelectionHelper = null;
			}
		}

		/// <summary>
		/// For AdjustScrollRangeTestHScroll we need the dummy to allow horizontal scrolling.
		/// </summary>
		protected override bool WantHScroll => true;

		/// <summary>
		/// Recompute the layout
		/// </summary>
		protected override void OnLayout(LayoutEventArgs levent)
		{
			if (!SkipLayout)
			{
				base.OnLayout(levent);
			}
		}
		#endregion

		/// <summary>
		/// Call the OnLayout methods
		/// </summary>
		internal void CallLayout()
		{
			OnLayout(new LayoutEventArgs(this, string.Empty));
		}

		/// <summary>
		/// Exposes the OnKillFocus method to testing.
		/// </summary>
		internal void KillFocus(Control newWindow)
		{
			OnKillFocus(newWindow, true);
		}

		#region Overrides of RootSite
		/// <summary>
		/// Since we don't really show this window, it doesn't have a working AutoScrollPosition;
		/// but to test making the selection visible, we have to remember what the view tries to
		/// change it to.
		/// </summary>
		public override Point ScrollPosition
		{
			get => m_scrollPosition;
			set => m_scrollPosition = new Point(-value.X, -value.Y);
		}

		/// <summary>
		/// Makes a root box and initializes it with appropriate data
		/// </summary>
		internal void MakeRoot(int hvoRoot, int flid)
		{
			MakeRoot(hvoRoot, flid, 1);
		}

		/// <summary>
		/// Makes a root box and initializes it with appropriate data
		/// </summary>
		/// <param name="hvoRoot">Hvo of the root object</param>
		/// <param name="flid">Flid in which hvoRoot contains a sequence of StTexts</param>
		/// <param name="frag">Fragment for view constructor</param>
		internal void MakeRoot(int hvoRoot, int flid, int frag)
		{
			if (m_cache == null || DesignMode)
			{
				return;
			}
			base.MakeRoot();

			// Set up a new view constructor.
			ViewConstructor = CreateVc(flid);
			ViewConstructor.DefaultWs = m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle;

			RootBox.DataAccess = m_cache.DomainDataByFlid;
			RootBox.SetRootObject(hvoRoot, ViewConstructor, frag, m_styleSheet);

			m_fRootboxMade = true;
			m_dxdLayoutWidth = kForceLayout;
			// Don't try to draw until we get OnSize and do layout.

			// Added this to keep from Asserting if the user tries to scroll the draft window
			// before clicking into it to place the insertion point.
			try
			{
				RootBox.MakeSimpleSel(true, true, false, true);
			}
			catch (COMException)
			{
				// We ignore failures since the text window may be empty, in which case making a
				// selection is impossible.
			}
		}

		/// <summary>
		/// Creates the view constructor.
		/// </summary>
		private VwBaseVc CreateVc(int flid)
		{
			return new DummyBasicViewVc(MyDisplayType, flid);
		}

		/// <summary>
		/// Makes a root box and initializes it with appropriate data
		/// </summary>
		public override void MakeRoot()
		{
			MakeRoot(m_hvoRoot, m_flid);
		}

		/// <summary>
		/// Creates a new DummyEditingHelper
		/// </summary>
		protected override EditingHelper CreateEditingHelper()
		{
			return new DummyEditingHelper(m_cache, this);
		}
		#endregion

		#region Properties
		/// <summary>
		/// Gets or sets the type of boxes to display: lazy or non-lazy or both
		/// </summary>
		internal DisplayType MyDisplayType { get; set; }

		/// <summary>
		/// Gets the draft view's selection helper object
		/// </summary>
		internal SelectionHelper SelectionHelper { get; private set; }

		/// <summary>
		/// Gets or sets a flag if OnLayout should be skipped.
		/// </summary>
		internal bool SkipLayout { get; set; } = false;

		/// <summary>
		/// Gets the view constructor.
		/// </summary>
		internal VwBaseVc ViewConstructor { get; private set; }
		#endregion

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
		internal bool IsParagraphProps(out IVwSelection vwsel, out int hvoText, out int tagText, out IVwPropertyStore[] vqvps, out int ihvoAnchor, out int ihvoEnd)
		{
			vwsel = null;
			hvoText = 0;
			tagText = 0;
			vqvps = null;
			ihvoAnchor = 0;
			ihvoEnd = 0;

			return EditingHelper.IsParagraphProps(out vwsel, out hvoText, out tagText, out vqvps, out ihvoAnchor, out ihvoEnd);
		}

		/// <summary>
		/// Get the view selection and paragraph properties.
		/// </summary>
		/// <param name="vwsel">[out] The selection</param>
		/// <param name="hvoText">[out] The HVO</param>
		/// <param name="tagText">[out] The tag</param>
		/// <param name="vqvps">[out] The paragraph properties</param>
		/// <param name="ihvoFirst">[out] Start index of selection</param>
		/// <param name="ihvoLast">[out] End index of selection</param>
		/// <param name="vqttp">[out] The style rules</param>
		/// <returns>Return false if there is neither a selection nor a paragraph property.
		/// Otherwise return true.</returns>
		internal bool GetParagraphProps(out IVwSelection vwsel, out int hvoText,
			out int tagText, out IVwPropertyStore[] vqvps, out int ihvoFirst, out int ihvoLast, out ITsTextProps[] vqttp)
		{
			return EditingHelper.GetParagraphProps(out vwsel, out hvoText, out tagText, out vqvps, out ihvoFirst, out ihvoLast, out vqttp);
		}

		/// <summary>
		/// Provides access to <see cref="SimpleRootSite.GetCoordRects"/>.
		/// </summary>
		internal new void GetCoordRects(out Rectangle rcSrcRoot, out Rectangle rcDstRoot)
		{
			rcSrcRoot = Rectangle.Empty;
			rcDstRoot = Rectangle.Empty;
			base.GetCoordRects(out rcSrcRoot, out rcDstRoot);
		}

		/// <summary>
		/// Provides access to <see cref="SimpleRootSite.AdjustScrollRange1"/>.
		/// </summary>
		internal bool AdjustScrollRange(int dxdSize, int dxdPosition, int dydSize, int dydPosition)
		{
			return AdjustScrollRange1(dxdSize, dxdPosition, dydSize, dydPosition);
		}

		/// <summary>
		/// Provides access to <see cref="ScrollableControl.VScroll"/>
		/// </summary>
		internal new bool VScroll
		{
			get => base.VScroll;
			set => base.VScroll = value;
		}

		/// <summary>
		/// Provides access to <see cref="ScrollableControl.HScroll"/>
		/// </summary>
		internal new bool HScroll
		{
			get => base.HScroll;
			set => base.HScroll = value;
		}

		/// <summary>
		/// Gets the height of the selection.
		/// </summary>
		internal int SelectionHeight
		{
			get
			{
				var nLineHeight = 0;
				using (new HoldGraphics(this))
				{
					GetCoordRects(out var rcSrcRoot, out var rcDstRoot);
					var vwsel = RootBox.Selection;
					if (vwsel != null)
					{
						vwsel.Location(m_graphicsManager.VwGraphics, rcSrcRoot, rcDstRoot, out var rdIP, out _, out _, out _);
						nLineHeight = rdIP.bottom - rdIP.top;
					}
				}

				return nLineHeight;
			}
		}

		/// <summary>
		/// Gets the width of the selection.
		/// </summary>
		internal int SelectionWidth
		{
			get
			{
				var nSelWidth = 0;
				using (new HoldGraphics(this))
				{
					GetCoordRects(out var rcSrcRoot, out var rcDstRoot);
					var vwsel = RootBox.Selection;
					if (vwsel != null)
					{
						vwsel.Location(m_graphicsManager.VwGraphics, rcSrcRoot, rcDstRoot, out var rdIP, out _, out _, out _);
						nSelWidth = rdIP.right - rdIP.left;
					}
				}

				return nSelWidth;
			}
		}

		/// <summary>
		/// Provides access to EditingHelper.ApplyStyle
		/// </summary>
		internal void ApplyStyle(string sStyleToApply)
		{
			EditingHelper.ApplyStyle(sStyleToApply);
		}

		/// <summary>
		/// Provides access to <see cref="SimpleRootSite.OnMouseDown"/>
		/// </summary>
		internal void CallOnMouseDown(MouseEventArgs e)
		{
			OnMouseDown(e);
		}

		/// <summary />
		internal void SetRootBox(IVwRootBox rootb)
		{
			RootBox = rootb;
		}
	}
}
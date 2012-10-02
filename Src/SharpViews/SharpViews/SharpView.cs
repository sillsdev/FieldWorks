using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.SharpViews.FieldWorksReplacements;
using SIL.FieldWorks.SharpViews.Selections;

namespace SIL.FieldWorks.SharpViews
{
	/// <summary>
	/// This is a control holding a RootBox. The idea is to keep it as simple as possible, since being a real control it is hard to test.
	/// Methods should merely delegate to something testable, except to the extent that work is needed to create real graphics objects.
	/// </summary>
	public partial class SharpView : ScrollableControl, ISharpViewSite
	{
		private RootBox m_root;
		private Timer m_Timer; // used to flash the insertion point.
		/// <summary>
		/// Get or set the root box which this view displays.
		/// </summary>
		public RootBox Root
		{
			get { return m_root; }
			set
			{
				if (m_root != null)
					m_root.SizeChanged -= m_root_SizeChanged;
				m_root = value;
				if (m_root != null)
				{
					m_root.Site = this;
					m_root.SizeChanged += m_root_SizeChanged;
				}
				m_lastLayoutWidth = 0;
				PerformLayout();
				Invalidate();
			}
		}

		/// <summary>
		/// Non-private only to allow an internal subclass to initialize it differently.
		/// </summary>
		internal ILgWritingSystemFactory m_wsf;

		/// <summary>
		/// The WSF this view uses. Eventually this should be settable. Better yet it should not require one.
		/// </summary>
		public virtual ILgWritingSystemFactory WritingSystemFactory
		{
			get
			{
				if (m_wsf == null)
					m_wsf = new DefaultWritingSystemFactory();
				return m_wsf;
			}
		}

		void m_root_SizeChanged(object sender, RootBox.RootSizeChangedEventArgs e)
		{
			if (m_root != null)
				SetScrollRange(m_root.Width, m_root.Height);
		}

		private int m_lastLayoutWidth;
		public SharpView()
		{
			InitializeComponent();
			m_Timer = new Timer(this.components) {Interval = 500};
			m_Timer.Tick += new EventHandler(OnTimer);
			HScroll = false; // by default we don't scroll horizontally.
			AllowDrop = true;
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			if (disposing && m_root != null)
				m_root.SizeChanged -= m_root_SizeChanged;

			base.Dispose(disposing);
		}

		/// <summary>
		/// Exposed only so the SharpViewFdo subclass can initialize it slightly differently.
		/// </summary>
		internal IRendererFactory m_rendererFactory;

		public virtual IRendererFactory RendererFactory
		{
			get
			{
				if (m_rendererFactory == null)
					m_rendererFactory = new RendererFactory(WritingSystemFactory);
				return m_rendererFactory;
			}
		}

		protected override void DestroyHandle()
		{
			if (m_Timer != null)
			{
				m_Timer.Stop();
				m_Timer.Tick -= new EventHandler(OnTimer);
				m_Timer.Dispose();
				m_Timer = null;
			}

			base.DestroyHandle();
		}
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Flash the insertion point if appropriate.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected void OnTimer(object sender, EventArgs e)
		{
			if (m_root != null && Focused)
				m_root.FlashInsertionPoint();
		}

		protected override void OnGotFocus(EventArgs e)
		{
			if (m_Timer != null)
				m_Timer.Start();
			base.OnGotFocus(e);
		}

		protected override void OnLostFocus(EventArgs e)
		{
			if (m_Timer != null)
				m_Timer.Stop();
			base.OnLostFocus(e);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			if (Root == null || m_lastLayoutWidth == 0)
				return;
			using (var gm = new GraphicsManager(this, e.Graphics))
			{
				Root.Paint(gm.VwGraphics, PaintArg(gm.VwGraphics));
			}
		}

		public void ScrollToShowSelection()
		{
			if (m_root == null)
				return;
			using (var gm = new GraphicsManager(this))
			{
				int dx, dy;
				Root.ScrollToShowSelection(gm.VwGraphics, PaintArg(gm.VwGraphics), ClientRectangle, out dx, out dy);
				ScrollBy(dx, dy);
			}

		}

		// Increase the scroll offsets by dx, dy (if positive).
		private void ScrollBy(int dx, int dy)
		{
			AutoScrollPosition = new Point(-AutoScrollPosition.X - dx, -AutoScrollPosition.Y - dy);
		}

		protected override void OnLayout(LayoutEventArgs e)
		{
			DoLayout();
			// It's very important to do this AFTER SetScrollRange (now called from Root.Layout). Once we've set the scroll range,
			// calling the base method will adjust our size, if we need to make room for a scroll bar.
			// (This may cause another, recursive call to OnLayout!)
			// If we call base.OnLayout first, the scroll bar won't appear or disappear until the NEXT
			// event that causes a layout.
			base.OnLayout(e);
			// This usually does nothing, since m_lastLayoutWidth == LayoutWidth.
			// However, if base.OnLayout() CHANGED the layout width, for example by inserting a scroll bar,
			// we need to do our internal layout over again.
			DoLayout();
		}

		private void DoLayout()
		{
			int layoutWidth = LayoutWidth;
			if (Root == null || layoutWidth <= 0 || m_lastLayoutWidth == layoutWidth)
				return;

			using (var gm = new GraphicsManager(this, null))
			{
				Root.Layout(LayoutArg(gm));
			}
			m_lastLayoutWidth = layoutWidth;
			//SetScrollRange(Root.Width, Root.Height);
			Invalidate();
		}

		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			base.OnKeyPress(e);
			var insertionPoint = Root.Selection as InsertionPoint;
			if (insertionPoint != null)
			{
				switch (e.KeyChar)
				{
					case (char) Keys.Back:
						insertionPoint.Backspace();
						break;
					case (char) Keys.Enter:
						insertionPoint.InsertLineBreak();
						break;
					default:
						insertionPoint.InsertText(e.KeyChar.ToString());
						break;
				}
			}
			// After we've made any delayed selection changes, scroll so the new selection can be seen.
			PerformAfterNotifications(ScrollToShowSelection);
		}

		public void OnDelete()
		{
			Root.OnDelete();
			// After we've made any delayed selection changes, scroll so the new selection can be seen.
			PerformAfterNotifications(ScrollToShowSelection);
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			if (Root == null || m_lastLayoutWidth == 0)
				return;
			using (var gm = new GraphicsManager(this))
			{
				m_root.OnMouseDown(e, ModifierKeys, gm.VwGraphics, PaintArg(gm.VwGraphics));
			}
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			if (Root == null || m_lastLayoutWidth == 0)
				return;
			using (var gm = new GraphicsManager(this))
			{
				var sel = m_root.Selection;
				m_root.OnMouseMove(e, ModifierKeys, gm.VwGraphics, PaintArg(gm.VwGraphics));
				if (sel != m_root.Selection)
					Update(); // need to show the updated selection before we process more movements to get continuous drag effect.
			}
		}

		protected override void OnDragEnter(DragEventArgs drgevent)
		{
			// I don't think we need to support the DragEnter event, apart from our own implementation of it.
			//base.OnDragEnter(drgevent);
			using (var gm = new GraphicsManager(this))
			{
				var location = PointToClient(new Point(drgevent.X, drgevent.Y));
				Root.OnDragEnter(drgevent, location, gm.VwGraphics, PaintArg(gm.VwGraphics));
			}
		}

		/// <summary>
		/// We treat a drag over just like a drag enter.
		/// </summary>
		/// <param name="drgevent"></param>
		protected override void OnDragOver(DragEventArgs drgevent)
		{
			OnDragEnter(drgevent);
		}

		protected override void OnDragLeave(EventArgs e)
		{
			Root.OnDragLeave();
		}

		protected override void OnQueryContinueDrag(QueryContinueDragEventArgs qcdevent)
		{
			base.OnQueryContinueDrag(qcdevent); // call before root method, determines whether drop or cancel
			Root.OnQueryContinueDrag(qcdevent);
		}

		protected override void OnDragDrop(DragEventArgs drgevent)
		{
			// I don't think we need to support the DragDrop event, apart from our own implementation of it.
			//base.OnDragDrop(drgevent);
			using (var gm = new GraphicsManager(this))
			{
				var location = PointToClient(new Point(drgevent.X, drgevent.Y));
				Root.OnDragDrop(drgevent, location, gm.VwGraphics, PaintArg(gm.VwGraphics));
			}
		}

		int LayoutWidth
		{
			get { return this.ClientRectangle.Width - 2 * Indent; }
		}

		private const int Indent = 2;
		private const int TopMargin = 2;

		LayoutInfo LayoutArg(GraphicsManager gm)
		{
			int dpiX = (int) Math.Round(gm.DpiX);
			int dpiY = (int) Math.Round(gm.DpiY);
			return new LayoutInfo(Indent, TopMargin, dpiX, dpiY, ClientSize.Width - Indent*2, gm.VwGraphics, RendererFactory);
		}

		internal PaintTransform PaintArg(IVwGraphics vg)
		{
			return new PaintTransform(Indent, TopMargin, vg.XUnitsPerInch, vg.YUnitsPerInch,
				-AutoScrollPosition.X, -AutoScrollPosition.Y, vg.XUnitsPerInch, vg.YUnitsPerInch);
		}

		/// <summary>
		/// Given that the root box is sizeX layout units wide and sizeY layout units high, set the
		/// AutoScrollMinSize to allow scrolling an appropriate distance, and if necessary adjust the
		/// AutoScrollPosition.
		/// </summary>
		/// <param name="sizeX"></param>
		/// <param name="sizeY"></param>
		internal void SetScrollRange(int sizeX, int sizeY)
		{
			// Todo JohnT: requires some further adjustment if we are zoomed or painting at a different resolution
			// from layout (or vertically oriented, if we do that).
			// Review JohnT: do we need to do anything about the autoscrollposition, or will that
			// automatically be changed if necessary?
			var width = 0; // sizeX + 2 * Indent, if we want horizontal scrolling.
			AutoScrollMinSize = new Size(width, sizeY + 2 * TopMargin + 4);
			// Following line is necessary so that the DisplayRectangle gets updated.
			// This calls PerformLayout().
			// Todo: Mono (JohnT): comparison with SimpleRootSite suggests something else might be needed in Mono
			AdjustFormScrollbars(HScroll || VScroll);
		}

		#region Implementation of ISharpViewSite

		/// <summary>
		/// Returns the information needed for figuring out where to draw (or invalidate) things.
		/// </summary>
		public IGraphicsHolder DrawingInfo
		{
			get { return new GraphicsHolder(this, (vg) => PaintArg(vg)); }
		}

		/// <summary>
		/// Do an invalidate, relative to the top left of the root box.
		/// Todo Zoom: allow for differing dpi.
		/// </summary>
		public void InvalidateInRoot(Rectangle rect)
		{
			var rect1 = rect;
			rect1.Offset(AutoScrollPosition.X + Indent, AutoScrollPosition.Y + TopMargin);
			Invalidate(rect1);
		}

		/// <summary>
		/// The current simple implementation is suitable for clients not using UOW.
		/// Enhance JohnT: When we support UOW, we need to save the actions if one is in
		/// progress, and do them all when it is complete.
		/// </summary>
		public void PerformAfterNotifications(Action task)
		{
			task();
		}

		// Invalidate(Rectangle) interface member is inherited.

		#endregion
	}
}

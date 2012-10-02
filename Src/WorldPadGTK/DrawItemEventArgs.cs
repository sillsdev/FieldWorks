/*
// DrawItemEventArgs.cs
// User: Jean-Marc Giffin at 5:10 P 10/07/2008

using System;
using Gtk;
using Gdk;

namespace SIL.FieldWorks.Common.Controls
{
	// JMG: Stubs

	public class DrawItemEventArgs : EventArgs
	{
		TreeIter iter;
		int itemIndex = -1;
		private Gdk.Drawable drawable;
		private Gtk.Widget widget;
		private Gdk.Rectangle backgroundArea, cellArea, exposeArea;
		private CellRendererState cellFlags;

		public int ItemIndex
		{
				get
				{
						return itemIndex;
				}
		}

		public System.Drawing.Graphics Graphics
		{
				get
				{
						return Gtk.DotNet.Graphics.FromDrawable( this.Drawable );
				}
		}

		public Gdk.Drawable Drawable {
				get {
						return drawable;
				}
		}

		public Gtk.Widget Widget {
				get {
						return widget;
				}
		}

		public Gdk.Rectangle BackgroundArea {
				get {
						return backgroundArea;
				}
		}

		public Gdk.Rectangle CellArea {
				get {
						return cellArea;
				}
		}

		public Gdk.Rectangle ExposeArea {
				get {
						return exposeArea;
				}
		}

		public CellRendererState CellFlags {
				get {
						return cellFlags;
				}
		}

		public TreeIter Iter {
				get {
						return iter;
				}
				internal set {
						iter = value;
				}
		}

		public DrawItemEventArgs( int ItemIndex, TreeIter iter, Gdk.Drawable window, Widget widget, Gdk.Rectangle background_area, Gdk.Rectangle cell_area, Gdk.Rectangle expose_area, CellRendererState flags )
		{
			this.itemIndex  = ItemIndex;
			this.drawable   = window;
			this.widget     = widget;
			this.iter       = iter;
			this.backgroundArea = background_area;
			this.cellArea   = cell_area;
			this.exposeArea = expose_area;
			this.cellFlags  = flags;

		}
	}
}
*/
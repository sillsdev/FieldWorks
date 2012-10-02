/*
 *    FmtBdrDlgPController.cs
 *
 *    <purpose>
 *
 *    Andrew Weaver - 2008-05-01
 *
 *    $Id$
 */

using System;
using System.IO;
using Gdk;
using Gtk;
using Glade;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.FwCoreDlgWidgets;

namespace SIL.FieldWorks.WorldPad
{
	public class FmtBdrDlgPController : DialogController
	{
		[Widget] private Gtk.Dialog kridFmtBdrDlgP;
		[Widget] private BorderWidget kctidFmtBdrDlgDiag;
		// No border
		[Widget] private Gtk.CheckButton kctidFmtBdrDlgNoneP;
		// All borders
		[Widget] private Gtk.CheckButton kctidFmtBdrDlgAll;
		// Left border
		[Widget] private Gtk.CheckButton kctidFmtBdrDlgLeading;
		// Right border
		[Widget] private Gtk.CheckButton kctidFmtBdrDlgTrailing;
		// Top border
		[Widget] private Gtk.CheckButton kctidFmtBdrDlgTop;
		// Bottom border
		[Widget] private Gtk.CheckButton kctidFmtBdrDlgBottom;
		// Color dialog
		[Widget] private Gtk.ColorButton kctidColor;
		// Width ComboBoxEntry
		[Widget] private Gtk.ComboBox kctidFmtBdrDlgWidth;
		[Widget] private Gtk.Container borderWidgets;
		private FmtBdrDlgPModel model_;

		private Gdk.Pixmap pixmap;
		private Gdk.Pixbuf diagPixbuf;
		private Gdk.GC drawGC;
		private Gdk.GC eraseGC;

		private int m_indicatorWidth;
		private Gdk.Color m_indicatorColor;
		private int m_maxIndicatorWidth;  // An odd number >= max width
		private bool anotherHandlerIsExecuting;

		private const int WIDEST_BORDER = 6000;
		private const int POINTS_PER_INCH = 72;
		private const int PIXELS_PER_INCH = 96;
		private const int MILLIPOINTS = 1000;
		private const string BORDER_IMAGE = "FmtBdrDlgDiag.png";

		/// <summary>
		/// The controller class for the Format -> Border dialog.
		/// </summary>
		/// <param name="model">
		/// A <see cref="FmtBdrDlgPModel"/>, which is the model for Format -> Border.
		/// </param>
		public FmtBdrDlgPController(FmtBdrDlgPModel model) : base("kridFmtBdrDlgP", model)
		{
			Console.WriteLine("FmtBdrDlgPController.ctor invoked");
			model_ = model;

			kctidColor.Color = model_.BorderColor;
			kctidFmtBdrDlgDiag.Color = kctidColor.Color;
			kctidFmtBdrDlgTop.Active = model_.Top;
			kctidFmtBdrDlgAll.Active = model_.All;
			kctidFmtBdrDlgBottom.Active = model_.Bottom;
			kctidFmtBdrDlgLeading.Active = model_.Leading;
			kctidFmtBdrDlgNoneP.Active = model_.None;
			kctidFmtBdrDlgTrailing.Active = model_.Trailing;
			kctidFmtBdrDlgWidth.Active = model_.BorderWidth;

			/*
			m_maxIndicatorWidth = WIDEST_BORDER * PIXELS_PER_INCH / MILLIPOINTS / POINTS_PER_INCH;
			if (m_maxIndicatorWidth % 2 == 0)
				m_maxIndicatorWidth++;
			*/
		}

		/// <value>
		/// The widgets contained within the dialog.
		/// </value>
		public override Gtk.Container Widgets
		{
			get
			{
				return borderWidgets;
			}
		}

		/// <summary>
		/// Handler for when the border color is set.
		/// </summary>
		private void on_kctidColor_color_set(object obj, EventArgs args)
		{
			Console.WriteLine("FmtBdrDlgPController.on_kctidColor_colorset invoked.");
			kctidFmtBdrDlgDiag.Color = kctidColor.Color;
			kctidFmtBdrDlgDiag.DrawBorders(true);
		}

		/// <summary>
		/// Commit changes to the model.
		/// </summary>
		protected override void Commit() {
			model_.BorderColor = kctidColor.Color;
			model_.All = kctidFmtBdrDlgAll.Active;
			model_.None = kctidFmtBdrDlgNoneP.Active;
			model_.Top = kctidFmtBdrDlgTop.Active;
			model_.Bottom = kctidFmtBdrDlgBottom.Active;
			model_.Leading = kctidFmtBdrDlgLeading.Active;
			model_.Trailing = kctidFmtBdrDlgTrailing.Active;
			model_.BorderWidth = kctidFmtBdrDlgWidth.Active;
		}

		/// <summary>
		/// Handler for when the width changes.
		/// </summary>
		private void on_kctidFmtBdrDlgWidth_changed(object obj, EventArgs args)
		{
			Console.WriteLine("FmtBdrDlgPController.OnWidthChange invoked.");
			try
			{
			  kctidFmtBdrDlgDiag.Width = int.Parse(kctidFmtBdrDlgWidth.ActiveText);
				kctidFmtBdrDlgDiag.DrawBorders(true);
			}
			catch (Exception e)
			{
				kctidFmtBdrDlgDiag.Width = 1;
			}
		}

		/// <summary>
		/// Handler for the toggling of the None button. It will assure that all the other
		/// pieces are in sync, ie. uncheck all the boxes, assure that "All" is not also
		/// pressed, etc.
		/// </summary>
		private void on_kctidFmtBdrDlgNoneP_toggled(object obj, EventArgs args)
		{

			Console.WriteLine("on_kctidFmtBdrDlgNoneP_toggled() invoked"
							  + "(anotherHandlerIsExecuting: {0})", anotherHandlerIsExecuting);

			if (anotherHandlerIsExecuting)
				return;

			anotherHandlerIsExecuting = true;

			if (kctidFmtBdrDlgNoneP.Active)
			{
				kctidFmtBdrDlgAll.Active = false;
				kctidFmtBdrDlgLeading.Active = false;
				kctidFmtBdrDlgTrailing.Active = false;
				kctidFmtBdrDlgTop.Active = false;
				kctidFmtBdrDlgBottom.Active = false;

				try
				{
					kctidFmtBdrDlgDiag.DrawBorders(false, false, false, false);
				}
				catch { }
			}
			else
			{
				// Don't allow click to toggle button "off"
				kctidFmtBdrDlgNoneP.Active = true;
			}

			anotherHandlerIsExecuting = false;
		}

		/// <summary>
		/// Handler for the toggling of the None button. It will assure that all the other
		/// pieces are in sync, ie. check all the boxes, assure that "None" is not also
		/// pressed, etc.
		/// </summary>
		private void on_kctidFmtBdrDlgAll_toggled(object obj, EventArgs args)
		{

			Console.WriteLine("on_kctidFmtBdrDlgAll_toggled() invoked (anotherHandlerIsExecuting: {0})", anotherHandlerIsExecuting);
			if (anotherHandlerIsExecuting)
				return;

			anotherHandlerIsExecuting = true;

			if (kctidFmtBdrDlgAll.Active)
			{
				kctidFmtBdrDlgNoneP.Active = false;
				kctidFmtBdrDlgLeading.Active = true;
				kctidFmtBdrDlgTrailing.Active = true;
				kctidFmtBdrDlgTop.Active = true;
				kctidFmtBdrDlgBottom.Active = true;

				try
				{
					kctidFmtBdrDlgDiag.DrawBorders(true, true, true, true);
				}
				catch { }
			}
			else
			{
				// Don't allow click to toggle button "off"
				kctidFmtBdrDlgAll.Active = true;
			}

			anotherHandlerIsExecuting = false;

		}

		/// <summary>
		/// Handler for when any of the Left, Right, Top, Bottom checkboxes are changed.
		/// Updates the preview, and also presses the "None" or "All" button as appropriate.
		/// </summary>
		private void on_checkLTRB_toggled(object obj, EventArgs args)
		{

			Console.WriteLine("on_checkLTRB_toggled() invoked (anotherHandlerIsExecuting: {0})", anotherHandlerIsExecuting);

			if (anotherHandlerIsExecuting)
				return;

			anotherHandlerIsExecuting = true;

			if (kctidFmtBdrDlgDiag == null)
				throw new Exception("kctidFmtBdrDlgDiag is null.");

			try {
				kctidFmtBdrDlgDiag.DrawBorders(kctidFmtBdrDlgLeading.Active,
											   kctidFmtBdrDlgTop.Active,
											   kctidFmtBdrDlgTrailing.Active,
											   kctidFmtBdrDlgBottom.Active);
			}
			catch {}

			if (AllActive())
			{
				kctidFmtBdrDlgNoneP.Active = false;
				kctidFmtBdrDlgAll.Active = true;
			}
			else if (NoneActive())
			{
				kctidFmtBdrDlgNoneP.Active = true;
				kctidFmtBdrDlgAll.Active = false;
			}
			else
			{
				kctidFmtBdrDlgNoneP.Active = false;
				kctidFmtBdrDlgAll.Active = false;
			}

			anotherHandlerIsExecuting = false;

		}

		/// <summary>
		/// Check if all checkboxes are active.
		/// </summary>
		/// <returns>
		/// A <see cref="System.Boolean"/>, true if all checkboxes are active, false otherwise.
		/// </returns>
		private bool AllActive()
		{

			Console.WriteLine("AllActive() invoked");
			return (kctidFmtBdrDlgLeading.Active && kctidFmtBdrDlgTrailing.Active &&
				kctidFmtBdrDlgTop.Active && kctidFmtBdrDlgBottom.Active);
		}

		/// <summary>
		/// Check if no checkboxes are active.
		/// </summary>
		/// <returns>
		/// A <see cref="System.Boolean"/>, true if no checkboxes are active, false otherwise.
		/// </returns>
		private bool NoneActive()
		{

			Console.WriteLine("NoneActive() invoked");
			return !(kctidFmtBdrDlgLeading.Active || kctidFmtBdrDlgTrailing.Active ||
				kctidFmtBdrDlgTop.Active || kctidFmtBdrDlgBottom.Active);
		}

		/// <summary>
		/// Draw the left border.
		/// </summary>
		/// <param name="gc">
		/// A <see cref="Gdk.GC"/>. the Graphics Context to draw within.
		/// </param>
		/// <param name="indicatorWidth">
		/// A <see cref="System.Int32"/>, the width of the border.
		/// </param>
		private void DrawLeftBorder(Gdk.GC gc, int indicatorWidth)
		{
			Console.WriteLine("DrawLeftBorder() invoked");

			int x = 20 + indicatorWidth / 2;

			pixmap.DrawLine(gc, x, 21, x, 160);

			return;
		}

		/// <summary>
		/// Draw the top border.
		/// </summary>
		/// <param name="gc">
		/// A <see cref="Gdk.GC"/>. the Graphics Context to draw within.
		/// </param>
		/// <param name="indicatorWidth">
		/// A <see cref="System.Int32"/>, the width of the border.
		/// </param>
		private void DrawTopBorder(Gdk.GC gc, int indicatorWidth)
		{
			Console.WriteLine("DrawTopBorder() invoked");

			int y = 21 + indicatorWidth / 2;

			pixmap.DrawLine(gc, 20, y, 148, y);

			return;
		}

		/// <summary>
		/// Draw the right border.
		/// </summary>
		/// <param name="gc">
		/// A <see cref="Gdk.GC"/>. the Graphics Context to draw within.
		/// </param>
		/// <param name="indicatorWidth">
		/// A <see cref="System.Int32"/>, the width of the border.
		/// </param>
		private void DrawRightBorder(Gdk.GC gc, int indicatorWidth)
		{
			Console.WriteLine("DrawRightBorder() invoked");

			int x = 148 - indicatorWidth / 2;

			pixmap.DrawLine(gc, x, 21, x, 160);

			return;
		}

		/// <summary>
		/// Draw the bottom border.
		/// </summary>
		/// <param name="gc">
		/// A <see cref="Gdk.GC"/>. the Graphics Context to draw within.
		/// </param>
		/// <param name="indicatorWidth">
		/// A <see cref="System.Int32"/>, the width of the border.
		/// </param>
		private void DrawBottomBorder(Gdk.GC gc, int indicatorWidth)
		{
			Console.WriteLine("DrawBottomBorder() invoked");

			int y = 160 - indicatorWidth / 2;

			pixmap.DrawLine(gc, 20, y, 148, y);

			return;
		}
	}
}

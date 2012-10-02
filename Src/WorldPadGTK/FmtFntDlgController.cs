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
	public class FmtFntDlgController : DialogController
	{
		[Widget] Gtk.Button kctidHelp;
		[Widget] Gtk.ComboBox kctidFfdFont;
		[Widget] Gtk.ComboBox kctidFfdSize;
		[Widget] Gtk.ColorButton kctidFfdForeClr;
		[Widget] Gtk.ColorButton kctidFfdBackClr;
		[Widget] Gtk.ComboBox kctidFfdUnder;
		[Widget] Gtk.ColorButton kctidFfdUnderClr;
		[Widget] Gtk.CheckButton kctidFfdBold;
		[Widget] Gtk.CheckButton kctidFfdItalic;
		[Widget] Gtk.CheckButton kctidFfdSuper;
		[Widget] Gtk.CheckButton kctidFfdSub;
		[Widget] Gtk.Button kctidFfdFeatures;
		[Widget] FontPreviewWidget kctidFfdPreview;

		private Gdk.Pixmap pixmap;
		private Gdk.Pixbuf diagPixbuf;
		private Gdk.GC drawGC;
		private Gdk.GC eraseGC;
		private FmtFntDlgModel model_;

		private int m_indicatorWidth;
		private Gdk.Color m_indicatorColor;
		private int m_maxIndicatorWidth;  // An odd number >= max width
		private bool anotherHandlerIsExecuting;

		private const int WIDEST_BORDER = 6000;
		private const int POINTS_PER_INCH = 72;
		private const int PIXELS_PER_INCH = 96;
		private const int MILLIPOINTS = 1000;
		private const string BORDER_IMAGE = "FmtBdrDlgDiag.png";

		public FmtFntDlgController(FmtFntDlgModel model) : base("kridFmtFntDlg", model)
		{
			Console.WriteLine("FmtBdrDlgPController.ctor invoked");
			model_ = model;
			kctidFfdBackClr.Color = model_.BackgroundColor;
			kctidFfdBold.Active = model_.Bold;
			kctidFfdFont.Active = model_.Font;
			kctidFfdForeClr.Color = model_.FontColor;
			kctidFfdItalic.Active = model_.Italic;
			kctidFfdSize.Active = model_.Size;
			kctidFfdSub.Active = model_.Subscript;
			kctidFfdSuper.Active = model_.Superscript;
			kctidFfdUnder.Active = model_.UnderlineStyle;
			kctidFfdUnderClr.Color = model_.UnderlineColor;

			/*
			m_maxIndicatorWidth = WIDEST_BORDER * PIXELS_PER_INCH / MILLIPOINTS / POINTS_PER_INCH;
			if (m_maxIndicatorWidth % 2 == 0)
				m_maxIndicatorWidth++;
			*/
		}

		private void on_kctidColor_color_set(object obj, EventArgs args)
		{
		}

		protected override void Commit()
		{
			model_.BackgroundColor = kctidFfdBackClr.Color;
			model_.Bold = kctidFfdBold.Active;
			model_.Font = kctidFfdFont.Active;
			model_.FontColor = kctidFfdForeClr.Color;
			model_.Italic = kctidFfdItalic.Active;
			model_.Size = kctidFfdSize.Active;
			model_.Subscript = kctidFfdSub.Active;
			model_.Superscript = kctidFfdSuper.Active;
			model_.UnderlineColor = kctidFfdUnderClr.Color;
			model_.UnderlineStyle = kctidFfdUnder.Active;
		}

		private void on_kctidFmtBdrDlgWidth_changed(object obj, EventArgs args)
		{
		}

		private void on_kctidFfdFeatures_clicked(object obj, EventArgs args)
		{
		}

		private void on_kctidFfdFont_changed(object obj, EventArgs args)
		{
		}

		private void on_kctidFfdSize_changed(object obj, EventArgs args)
		{
		}

		private void on_kctidFfdForeClr_color_set(object obj, EventArgs args)
		{
		}

		private void on_kctidFfdBackClr_color_set(object obj, EventArgs args)
		{
		}

		private void on_kctidFfdUnderClr_color_set(object obj, EventArgs args)
		{
		}

		private void on_kctidFfdBold_toggled(object obj, EventArgs args)
		{
		}

		private void on_kctidFfdItalic_toggled(object obj, EventArgs args)
		{
		}

		private void on_kctidFfdSuper_toggled(object obj, EventArgs args)
		{
		}

		private void on_kctidFfdSub_toggled(object obj, EventArgs args)
		{
		}

		private void on_kctidFfdOffset_changed(object obj, EventArgs args)
		{
		}

		private void on_kctidFfdOffsetSpin_value_changed(object obj, EventArgs args)
		{
		}
	}
}

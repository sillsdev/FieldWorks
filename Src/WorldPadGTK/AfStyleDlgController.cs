/*
 *    AfStyleDlgController.cs
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
	public class AfStyleDlgController : DialogController
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
		[Widget] private Gtk.VBox paraTabSite;
		[Widget] private Gtk.VBox bulNumTabSite;
		[Widget] private Gtk.VBox borderTabSite;

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

		public AfStyleDlgController(IDialogModel model) : base("kridAfStyleDlg", model)
		{
			Console.WriteLine("FmtBdrDlgPController.ctor invoked");

			DialogFactory df = new DialogFactory(null);
			DialogController para = df.CreateDialog(DialogFactory.DialogType.Paragraph);
			DialogController bulNum = df.CreateDialog(DialogFactory.DialogType.BulletNumbering);
			DialogController border = df.CreateDialog(DialogFactory.DialogType.Borders);
			AddWidgets(para, paraTabSite);
#if false
			AddWidgets(bulNum, bulNumTabSite);  //TODO: Reinstate when BulNum tab is fixed
#endif
			AddWidgets(border, borderTabSite);

			m_maxIndicatorWidth = WIDEST_BORDER * PIXELS_PER_INCH / MILLIPOINTS / POINTS_PER_INCH;
			if (m_maxIndicatorWidth % 2 == 0)
				m_maxIndicatorWidth++;
		}

		private void AddWidgets(DialogController dialog, Gtk.Container container)
		{
			if (dialog == null)
				throw new Exception("Dialog is null");
			if (dialog.Widgets == null)
				throw new Exception("Dialog's Widgets are null");
			if (container == null)
				throw new Exception("The Container is null");
			dialog.Widgets.Reparent(container);
		}

		private void on_kctidColor_color_set(object obj, EventArgs args)
		{
			Console.WriteLine("FmtBdrDlgPController.on_kctidColor_colorset invoked.");
			kctidFmtBdrDlgDiag.Color = kctidColor.Color;
			kctidFmtBdrDlgDiag.DrawBorders(true);
		}

		protected override void Commit() {
		}

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

				kctidFmtBdrDlgDiag.DrawBorders(false, false, false, false);
			}
			else
			{
				// Don't allow click to toggle button "off"
				kctidFmtBdrDlgNoneP.Active = true;
			}

			anotherHandlerIsExecuting = false;
		}

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

				kctidFmtBdrDlgDiag.DrawBorders(true, true, true, true);
			}
			else
			{
				// Don't allow click to toggle button "off"
				kctidFmtBdrDlgAll.Active = true;
			}

			anotherHandlerIsExecuting = false;

		}

		private void on_checkLTRB_toggled(object obj, EventArgs args)
		{

			Console.WriteLine("on_checkLTRB_toggled() invoked (anotherHandlerIsExecuting: {0})", anotherHandlerIsExecuting);

			if (anotherHandlerIsExecuting)
				return;

			anotherHandlerIsExecuting = true;

			if (kctidFmtBdrDlgDiag == null)
				throw new Exception("kctidFmtBdrDlgDiag is null.");

			kctidFmtBdrDlgDiag.DrawBorders(kctidFmtBdrDlgLeading.Active, kctidFmtBdrDlgTop.Active,
									 kctidFmtBdrDlgTrailing.Active, kctidFmtBdrDlgBottom.Active);
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

		private bool AllActive()
		{

			Console.WriteLine("AllActive() invoked");
			return (kctidFmtBdrDlgLeading.Active && kctidFmtBdrDlgTrailing.Active &&
				kctidFmtBdrDlgTop.Active && kctidFmtBdrDlgBottom.Active);
		}

		private bool NoneActive()
		{

			Console.WriteLine("NoneActive() invoked");
			return !(kctidFmtBdrDlgLeading.Active || kctidFmtBdrDlgTrailing.Active ||
				kctidFmtBdrDlgTop.Active || kctidFmtBdrDlgBottom.Active);
		}

		private void DrawLeftBorder(Gdk.GC gc, int indicatorWidth)
		{
			Console.WriteLine("DrawLeftBorder() invoked");

			int x = 20 + indicatorWidth / 2;

			pixmap.DrawLine(gc, x, 21, x, 160);

			return;
		}

		private void DrawTopBorder(Gdk.GC gc, int indicatorWidth)
		{
			Console.WriteLine("DrawTopBorder() invoked");

			int y = 21 + indicatorWidth / 2;

			pixmap.DrawLine(gc, 20, y, 148, y);

			return;
		}

		private void DrawRightBorder(Gdk.GC gc, int indicatorWidth)
		{
			Console.WriteLine("DrawRightBorder() invoked");

			int x = 148 - indicatorWidth / 2;

			pixmap.DrawLine(gc, x, 21, x, 160);

			return;
		}

		private void DrawBottomBorder(Gdk.GC gc, int indicatorWidth)
		{
			Console.WriteLine("DrawBottomBorder() invoked");

			int y = 160 - indicatorWidth / 2;

			pixmap.DrawLine(gc, 20, y, 148, y);

			return;
		}

		private void on_kctidFgCbBasedOn_changed(object obj, EventArgs args)
		{
		}

		private void on_kctidFgCbParaNextStyle_changed(object obj, EventArgs args)
		{
		}

		// ****************************
	// Handlers for Font tab events
	// ****************************

	private void on_kctidFfdOffsetSpin_value_changed(object obj, EventArgs args)
	{
		Console.WriteLine("StyleDlg.on_kctidFfdOffsetSpin_value_changed() invoked");
	}

	private void on_kctidFfdBold_toggled(object obj, EventArgs args)
	{
		Console.WriteLine("StyleDlg.on_kctidFfdBold_toggled() invoked");
	}

	private void on_kctidFfdItalic_toggled(object obj, EventArgs args)
	{
		Console.WriteLine("StyleDlg.on_kctidFfdItalic_toggled() invoked");
	}

	private void on_kctidFfdSuper_toggled(object obj, EventArgs args)
	{
		Console.WriteLine("StyleDlg.on_kctidFfdSuper_toggled() invoked");
	}

	private void on_kctidFfdSub_toggled(object obj, EventArgs args)
	{
		Console.WriteLine("StyleDlg.on_kctidFfdSub_toggled() invoked");
	}

	private void on_kctidFfdForeClr_color_set(object obj, EventArgs args)
	{
		Console.WriteLine("StyleDlg.on_kctidFfdForeClr_color_set() invoked");
	}

	private void on_kctidFfdBackClr_color_set(object obj, EventArgs args)
	{
		Console.WriteLine("StyleDlg.on_kctidFfdBackClr_color_set() invoked");
	}

	private void on_kctidFfdUnderClr_color_set(object obj, EventArgs args)
	{
		Console.WriteLine("StyleDlg.on_kctidFfdUnderClr_color_set() invoked");
	}

	private void on_kctidFfdFeatures_clicked(object obj, EventArgs args)
	{
		Console.WriteLine("StyleDlg.on_kctidFfdFeatures_clicked() invoked");
	}

	private void on_kctidFfdFont_changed(object obj, EventArgs args)
	{
		Console.WriteLine("StyleDlg.on_kctidFfdFont_changed() invoked");
	}

	private void on_kctidFfdSize_changed(object obj, EventArgs args)
	{
		Console.WriteLine("StyleDlg.on_kctidFfdSize_changed() invoked");
	}

	private void on_kctidFfdUnder_changed(object obj, EventArgs args)
	{
		Console.WriteLine("StyleDlg.on_kctidFfdUnder_changed() invoked");
	}

	private void on_kctidFfdOffset_changed(object obj, EventArgs args)
	{
		Console.WriteLine("StyleDlg.on_kctidFfdOffset_changed() invoked");
	}

	// *********************************
	// Handlers for Paragraph tab events
	// *********************************

	private void on_kctidFpCbLineSpace_changed(object obj, EventArgs args)
	{
		Console.WriteLine("StyleDlg.on_kctidFpCbLineSpace_changed() invoked");
	}

	private void on_kctidFpCbDirection_changed(object obj, EventArgs args)
	{
		Console.WriteLine("StyleDlg.on_kctidFpCbDirection_changed() invoked");
	}

	private void on_kctidFpCbAlign_changed(object obj, EventArgs args)
	{
		Console.WriteLine("StyleDlg.on_kctidFpCbAlign_changed() invoked");
	}

	private void on_kctidFpCbSpec_changed(object obj, EventArgs args)
	{
		Console.WriteLine("StyleDlg.on_kctidFpCbSpec_changed() invoked");
	}

	private void on_kctidFpSpSpacAft_value_changed(object obj, EventArgs args)
	{
		Console.WriteLine("StyleDlg.on_kctidFpSpSpacAft_value_changed() invoked");
	}

	private void on_kctidFpSpLineSpaceAt_value_changed(object obj, EventArgs args)
	{
		Console.WriteLine("StyleDlg.on_kctidFpSpLineSpaceAt_value_changed() invoked");
	}

	private void on_kctidFpSpIndLft_value_changed(object obj, EventArgs args)
	{
		Console.WriteLine("StyleDlg.on_kctidFpSpIndLft_value_changed() invoked");
	}

	private void on_kctidFpSpSpacBef_value_changed(object obj, EventArgs args)
	{
		Console.WriteLine("StyleDlg.on_kctidFpSpSpacBef_value_changed() invoked");
	}

	private void on_kctidFpSpIndRt_value_changed(object obj, EventArgs args)
	{
		Console.WriteLine("StyleDlg.on_kctidFpSpIndRt_value_changed() invoked");
	}

	private void on_kctidFpSpSpIndBy_value_changed(object obj, EventArgs args)
	{
		Console.WriteLine("StyleDlg.on_kctidFpSpSpIndBy_value_changed() invoked");
	}

	private void on_kctidFpCbBkgrnd_color_set(object obj, EventArgs args)
	{
		Console.WriteLine("StyleDlg.on_kctidFpCbBkgrnd_color_set() invoked");
	}

	// *******************************************
	// Handlers for Bullets & Numbering tab events
	// *******************************************

	private void on_kctidFbnPbFont_clicked(object obj, EventArgs args)
	{
		Console.WriteLine("StyleDlg.on_kctidFbnPbFont_clicked() invoked");
	}

	private void on_kctidFbnSpStartAt_value_changed(object obj, EventArgs args)
	{
		Console.WriteLine("StyleDlg.on_kctidFbnSpStartAt_value_changed() invoked");
	}

	private void on_kctidFbnCxStartAt_toggled(object obj, EventArgs args)
	{
		Console.WriteLine("StyleDlg.on_kctidFbnCxStartAt_toggled() invoked");

	}

	private void on_kctidFbnCbNumber_changed(object obj, EventArgs args)
	{
		Console.WriteLine("StyleDlg.on_kctidFbnCbNumber_changed() invoked");
	}

	private void on_kctidFbnCbBullet_changed(object obj, EventArgs args)
	{
		Console.WriteLine("StyleDlg.on_kctidFbnCbBullet_changed() invoked");
	}

	private void on_kctidFbnRbNotAList_toggled(object obj, EventArgs args)
	{
		Console.WriteLine("StyleDlg.on_kctidFbnRbNotAList_toggled() invoked");
	}

	private void on_kctidFbnRbBullet_toggled(object obj, EventArgs args)
	{
		Console.WriteLine("StyleDlg.on_kctidFbnRbBullet_toggled() invoked");
	}

	private void on_kctidFbnRbNumber_toggled(object obj, EventArgs args)
	{
		Console.WriteLine("StyleDlg.on_kctidFbnRbNumber_toggled() invoked");
	}

	private void on_kctidFbnRbUnspecified_toggled(object obj, EventArgs args)
	{
		Console.WriteLine("StyleDlg.on_kctidFbnRbUnspecified_toggled() invoked");
	}

	private void ChangeBulletWidgetsSensitiveState(bool sensitive)
	{
		Console.WriteLine("StyleDlg.ChangeBulletWidgetsSensitiveState() invoked");
	}

	private void ChangeNumberWidgetsSensitiveState(bool sensitive)
	{
		Console.WriteLine("StyleDlg.ChangeNumberWidgetsSensitiveState() invoked");
	}

	}
}

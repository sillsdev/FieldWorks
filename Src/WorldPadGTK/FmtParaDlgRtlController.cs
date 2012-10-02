/*
 *    FmtParaDlgRtlController.cs
 *
 *    <purpose>
 *
 *    Andrew Weaver - 2008-05-01
 *
 *    $Id$
 */

using System;
using System.IO;
using Gtk;
using Glade;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FwCoreDlgWidgets;

namespace SIL.FieldWorks.WorldPad
{
	public class FmtParaDlgRtlController : DialogController
	{
		public const string CENTIMETRE = "cm";
		public const string MILLIMETRE = "mm";
		public const string INCHES = "''";
		public const string POINTS = "pt";
		public const double INCHES_PER_CENTIMETRE = 0.39;
		public const double INCHES_PER_PIXEL = 50;

		[Widget] private Gtk.Dialog kridFmtParaDlgRtl;
		[Widget] private Gtk.Container paraWidgets;
		[Widget] private ParaPreviewWidget kctidFpPreview;
		[Widget] private Gtk.SpinButton kctidFpSpIndLft;
		[Widget] private Gtk.SpinButton kctidFpSpIndRt;
		[Widget] private Gtk.SpinButton kctidFpSpSpacBef;
		[Widget] private Gtk.SpinButton kctidFpSpSpacAft;
		[Widget] private Gtk.ComboBox kctidFpCbDirection;
		[Widget] private Gtk.ComboBox kctidFpCbAlign;
		[Widget] private Gtk.ColorButton kctidFpCbBkgrnd;
		[Widget] private Gtk.ComboBox kctidFpCbSpec;
		[Widget] private Gtk.SpinButton kctidFpSpSpIndBy;
		[Widget] private Gtk.ComboBox kctidFpCbLineSpace;
		[Widget] private Gtk.SpinButton kctidFpSpLineSpaceAt;
		private string input;
		private FmtParaDlgRtlModel model_;

		public FmtParaDlgRtlController(FmtParaDlgRtlModel model) : base("kridFmtParaDlgRtl", model)
		{
			Console.WriteLine("FmtParaDlgRtlController.ctor invoked");
			model_ = model;

			//Utils.CreateComboBox(kctidFpCbDirection, FmtParaDlgRtlModel.DIRECTIONS);
			//kctidFpCbDirection.AppendText("Hello");
			Utils.CreateComboBox(kctidFpCbAlign, FmtParaDlgRtlModel.ALIGNMENTS);
			Utils.CreateComboBox(kctidFpCbLineSpace, FmtParaDlgRtlModel.SPACINGS);
			Utils.CreateComboBox(kctidFpCbSpec, FmtParaDlgRtlModel.INDENTATIONS);
			kctidFpCbBkgrnd.Color = new Gdk.Color(255, 255, 255);
			kctidFpPreview.BackgroundColor = kctidFpCbBkgrnd.Color;

			kctidFpCbAlign.Active = (int)model_.Alignment;
			kctidFpCbBkgrnd.Color = model_.BackgroundColor;
			kctidFpPreview.BackgroundColor = kctidFpCbBkgrnd.Color;
			//kctidFpCbDirection.Active = model_.RightToLeft ? 1 : 0;
			kctidFpCbLineSpace.Active = (int)model_.Spacing;
			kctidFpSpIndLft.Value = model_.LeftIndentation;
			kctidFpSpIndRt.Value = model_.RightIndentation;
			kctidFpSpSpacBef.Value = model_.BeforeSpacing;
			kctidFpSpSpacAft.Value = model_.AfterSpacing;
			kctidFpCbSpec.Active = (int)model_.Indentation;
			AfterValueChange();
		}

		protected override void Commit()
		{
			model_.Alignment = (FmtParaDlgRtlModel.AlignmentType)kctidFpCbAlign.Active;
			model_.BackgroundColor = kctidFpCbBkgrnd.Color;
			//model_.RightToLeft = kctidFpCbDirection.Active == 1 ? true : false;
			model_.Spacing = (FmtParaDlgRtlModel.SpacingType)kctidFpCbLineSpace.Active;
			model_.LeftIndentation = kctidFpSpIndLft.Value;
			model_.RightIndentation = kctidFpSpIndRt.Value;
			model_.BeforeSpacing = kctidFpSpSpacBef.Value;
			model_.AfterSpacing = kctidFpSpSpacAft.Value;
			model_.Indentation = (FmtParaDlgRtlModel.IndentationType)kctidFpCbSpec.Active;
		}

		public FmtParaDlgRtlModel.AlignmentType GetAlignmentType(int type)
		{
			return FmtParaDlgRtlModel.AlignmentType.Centered;
		}

		public override Gtk.Container Widgets
		{
			get
			{
				return paraWidgets;
			}
		}

		private void on_kctidFpSpIndLft_value_changed(object obj, EventArgs args)
		{
			Console.WriteLine("FmtParaDlgRtlController.on_kctidFpSpIndLft_value_changed invoked");
			kctidFpPreview.LeftIndentation = (int)(kctidFpSpIndLft.Value * INCHES_PER_PIXEL);
			AfterValueChange();
		}

		private void on_kctidFpSpIndLft_changed(object obj, EventArgs args)
		{
			Console.WriteLine("FmtParaDlgRtlController.on_kctidFpSpIndLft_changed invoked");
		}

		private void on_kctidFpSpIndRt_value_changed(object obj, EventArgs args)
		{
			Console.WriteLine("FmtParaDlgRtlController.on_kctidFpSpIndRt_value_changed invoked");
			kctidFpPreview.RightIndentation = (int)(kctidFpSpIndRt.Value * INCHES_PER_PIXEL);
			AfterValueChange();
		}

		private void on_kctidFpSpIndRt_changed(object obj, EventArgs args)
		{
			Console.WriteLine("FmtParaDlgRtlController.on_kctidFpSpIndRt_changed invoked");
		}

		private void on_kctidFpSpSpacAft_value_changed(object obj, EventArgs args)
		{
			Console.WriteLine("FmtParaDlgRtlController.on_kctidFpSpSpacAft_value_changed invoked");
			kctidFpPreview.AfterSpacing = (int)kctidFpSpSpacAft.Value;
			AfterValueChange();
		}

		private void on_kctidFpSpSpacBef_value_changed(object obj, EventArgs args)
		{
			Console.WriteLine("FmtParaDlgRtlController.on_kctidFpSpSpacBef_value_changed invoked");
			kctidFpPreview.BeforeSpacing = (int)kctidFpSpSpacBef.Value;
			AfterValueChange();
		}

		private void on_kctidFpCbLineSpace_changed(object obj, EventArgs args) {
			Console.WriteLine("FmtParaDlgRtlController.on_kctidFpCbLineSpace_changed invoked");
			string line = kctidFpCbLineSpace.ActiveText;
			if (line == "1.5")
				kctidFpPreview.Spacing = SIL.FieldWorks.FwCoreDlgWidgets.ParaPreviewWidget.SpacingType.OneAndHalf;
			else if (line == "Double")
				kctidFpPreview.Spacing = SIL.FieldWorks.FwCoreDlgWidgets.ParaPreviewWidget.SpacingType.Double;
			else
				kctidFpPreview.Spacing = SIL.FieldWorks.FwCoreDlgWidgets.ParaPreviewWidget.SpacingType.Single;
			AfterValueChange();
		}

		private void on_kctidFpCbDirection_changed(object obj, EventArgs args) {
			/*
			string dir = kctidFpCbDirection.ActiveText;
			if (dir == "Left to Right")
				kctidFpPreview.RightToLeft = false;
			else
				kctidFpPreview.RightToLeft = true;
			AfterValueChange();
			*/
		}

		private void on_kctidFpCbAlign_changed(object obj, EventArgs args) {
			string align = kctidFpCbAlign.ActiveText;
			if (align == "Unspecified")
				kctidFpPreview.Alignment
					= SIL.FieldWorks.FwCoreDlgWidgets.ParaPreviewWidget.AlignmentType.Unspecified;
			else if (align == "Leading")
				kctidFpPreview.Alignment
					= SIL.FieldWorks.FwCoreDlgWidgets.ParaPreviewWidget.AlignmentType.Leading;
			else if (align == "Centered")
				kctidFpPreview.Alignment
					= SIL.FieldWorks.FwCoreDlgWidgets.ParaPreviewWidget.AlignmentType.Centered;
			else if (align == "Right")
				kctidFpPreview.Alignment
					= SIL.FieldWorks.FwCoreDlgWidgets.ParaPreviewWidget.AlignmentType.Right;
			else if (align == "Trailing")
				kctidFpPreview.Alignment
					= SIL.FieldWorks.FwCoreDlgWidgets.ParaPreviewWidget.AlignmentType.Trailing;
			else if (align == "Justified")
				kctidFpPreview.Alignment
					= SIL.FieldWorks.FwCoreDlgWidgets.ParaPreviewWidget.AlignmentType.Justified;
			else
				kctidFpPreview.Alignment
					= SIL.FieldWorks.FwCoreDlgWidgets.ParaPreviewWidget.AlignmentType.Left;
			AfterValueChange();
		}

		private void on_kctidFpCbBkgrnd_color_set(object obj, EventArgs args) {
			Console.WriteLine("FmtParaDlgRtlController.on_kctidFpCbBkgrnd_color_set invoked");
			kctidFpPreview.BackgroundColor = kctidFpCbBkgrnd.Color;
			AfterValueChange();
		}

		private void on_kctidFpCbSpec_changed(object obj, EventArgs args) {
			Console.WriteLine("FmtParaDlgRtlController.on_kctidFpCbSpec_changed invoked");
			string indent = kctidFpCbSpec.ActiveText;
			if (indent == "First Line")
				kctidFpPreview.Indentation
					= SIL.FieldWorks.FwCoreDlgWidgets.ParaPreviewWidget.IndentationType.FirstLine;
			else if (indent == "Hanging")
				kctidFpPreview.Indentation
					= SIL.FieldWorks.FwCoreDlgWidgets.ParaPreviewWidget.IndentationType.Hanging;
			else
				kctidFpPreview.Indentation
					= SIL.FieldWorks.FwCoreDlgWidgets.ParaPreviewWidget.IndentationType.None;
			AfterValueChange();
		}

		private void on_kctidFpSpSpIndBy_value_changed(object obj, EventArgs args) {
			Console.WriteLine("FmtParaDlgRtlController.on_kctidFpSpSpIndBy_value_changed invoked");
			kctidFpPreview.SpecialIndentation = (int)(kctidFpSpSpIndBy.Value * INCHES_PER_PIXEL);
			AfterValueChange();
		}

		private void on_kctidFpSpLineSpaceAt_value_changed(object obj, EventArgs args) {
				Console.WriteLine("FmtParaDlgRtlController.on_kctidFpSpLineSpaceAt_value_changed invoked");
		}

		/*
		private string Convert(string input)
		{
			if (input.EndsWith(CENTIMETRE))
			{
				input = input.Remove(input.Length - CENTIMETRE.Length, CENTIMETRE.Length);
				try {
					input = double.Parse(input) * INCHES_PER_CENTIMETRE + INCHES;
				} catch (Exception e) { }
			}
			else if (input.EndsWith(MILLIMETRE))
			{
				input = input.Remove(input.Length - MILLIMETRE.Length, MILLIMETRE.Length);
				try {
					input = double.Parse(input) * INCHES_PER_CENTIMETRE * 0.1 + INCHES;
					ToInches(kctidFpSpIndRt);
				} catch (Exception e) { }
			}
			return input;
		}
		*/

		private void AfterValueChange() {
			kctidFpPreview.Redraw();
		}
	}
}

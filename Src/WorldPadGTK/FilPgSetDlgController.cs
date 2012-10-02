/*
 *    FilPgSetDlgController.cs
 *
 *    <purpose>
 *
 *    Andrew Weaver - 2008-05-01
 *
 *
 *    $Id$
 */

using System;
using System.IO;
using Gtk;
using Glade;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.Framework;

namespace SIL.FieldWorks.WorldPad
{
	/// <summary>
	/// The Controller for the File -> Page Setup Dialog.
	/// </summary>
	public class FilPgSetDlgController : DialogController
	{
		[Widget] private Gtk.SpinButton kcidFilPgSetMLS;
		[Widget] private Gtk.SpinButton kcidFilPgSetMRS;
		[Widget] private Gtk.SpinButton kcidFilPgSetMTS;
		[Widget] private Gtk.SpinButton kcidFilPgSetMBS;
		[Widget] private Gtk.SpinButton kcidFilPgSetEHS;
		[Widget] private Gtk.SpinButton kcidFilPgSetEFS;
		[Widget] private Gtk.ComboBox kcidFilPgSetSize;
		[Widget] private Gtk.SpinButton kcidFilPgSetWS;
		[Widget] private Gtk.SpinButton kcidFilPgSetHS;
		[Widget] private Gtk.RadioButton kcidFilPgSetPort;
		[Widget] private Gtk.RadioButton kcidFilPgSetLand;
		[Widget] private Gtk.Image kridFilPgSetPort;
		[Widget] private Gtk.CheckButton kcidFilPgSetShowHdr;
		[Widget] private Gtk.Entry kcidFilPgSetHdE;
		[Widget] private Gtk.Entry kcidFilPgSetFtE;
		[Widget] private Gtk.Button kcidFilPgSetFont;
		private FilPgSetDlgModel model_;
		private bool updatePaperSize_;

		/// <summary>
		/// Construct the FilPgSetDlgController, using a FilPgSetDlgModel model to hold
		/// the information. It also gets the information from the model and sets defaults.
		/// </summary>
		/// <param name="model">
		/// A <see cref="FilPgSetDlgModel"/>
		/// </param>
		public FilPgSetDlgController(FilPgSetDlgModel model) : base("kridFilPgSetDlg", model)
		{
			model_ = model;
			kcidFilPgSetMLS.Value = model_.LeftMargins;
			kcidFilPgSetMRS.Value = model_.RightMargins;
			kcidFilPgSetMTS.Value = model_.TopMargins;
			kcidFilPgSetMBS.Value = model_.BottomMargins;
			kcidFilPgSetEHS.Value = model_.HeaderFromEdge;
			kcidFilPgSetEFS.Value = model_.FooterFromEdge;
			kcidFilPgSetWS.Value = model_.PaperWidth;
			kcidFilPgSetHS.Value = model_.PaperHeight;
			kcidFilPgSetPort.Active = model_.Portrait;
			kcidFilPgSetLand.Active = !model_.Portrait;
			kcidFilPgSetHdE.Text = model_.Header;
			kcidFilPgSetFtE.Text = model_.Footnote;
			kcidFilPgSetShowHdr.Active = model_.ShowHeaderOnFirstPage;
			Utils.CreateComboBox(kcidFilPgSetSize, model_.GetPaperTypeNames());
			kcidFilPgSetSize.Active = (int)model_.PaperSize;
			updatePaperSize_ = true;
		}

		/// <summary>
		/// Commit all the changes to the model.
		/// </summary>
		protected override void Commit() {
			model_.LeftMargins = kcidFilPgSetMLS.Value;
			model_.RightMargins = kcidFilPgSetMRS.Value;
			model_.TopMargins = kcidFilPgSetMTS.Value;
			model_.BottomMargins = kcidFilPgSetMBS.Value;
			model_.HeaderFromEdge = kcidFilPgSetEHS.Value;
			model_.FooterFromEdge = kcidFilPgSetEFS.Value;
			model_.PaperSize = (FilPgSetDlgModel.PaperType)kcidFilPgSetSize.Active;
			model_.PaperWidth = kcidFilPgSetWS.Value;
			model_.PaperHeight = kcidFilPgSetHS.Value;
			model_.Portrait = kcidFilPgSetPort.Active;
			model_.Header = kcidFilPgSetHdE.Text;
			model_.Footnote = kcidFilPgSetFtE.Text;
			model_.ShowHeaderOnFirstPage = kcidFilPgSetShowHdr.Active;
		}

		/// <summary>
		/// Handler for the "Default" button. Set all values to their defaults.
		/// </summary>
		private void on_kcidFilPgSetDef_clicked(object obj, EventArgs args)
		{
			Console.WriteLine("FilPgSetDlgController.on_kcidFilPgSetDef_clicked invoked");
			kcidFilPgSetMLS.Value = FilPgSetDlgModel.DEFAULT_MARGINS;
			kcidFilPgSetMRS.Value = FilPgSetDlgModel.DEFAULT_MARGINS;
			kcidFilPgSetMTS.Value = FilPgSetDlgModel.DEFAULT_MARGINS;
			kcidFilPgSetMBS.Value = FilPgSetDlgModel.DEFAULT_MARGINS;
			kcidFilPgSetEHS.Value = FilPgSetDlgModel.DEFAULT_MARGINS;
			kcidFilPgSetEFS.Value = FilPgSetDlgModel.DEFAULT_MARGINS;
			kcidFilPgSetSize.Active = 0;
			kcidFilPgSetWS.Value = 0;
			kcidFilPgSetHS.Value = 0;
			kcidFilPgSetPort.Active = FilPgSetDlgModel.DEFAULT_PORTRAIT;
			kcidFilPgSetFtE.Text = "";
			kcidFilPgSetHdE.Text = "";
			kcidFilPgSetShowHdr.Active = FilPgSetDlgModel.DEAFULT_SHOW_HEADER_ON_FIRST_PAGE;
		}

		/// <summary>Handler for when the value of Margins: Left changes </summary>
		private void on_kcidFilPgSetMLS_value_changed(object obj, EventArgs args)
		{
		}

		/// <summary>Handler for when the value of Margins: Right changes </summary>
		private void on_kcidFilPgSetMRS_value_changed(object obj, EventArgs args)
		{
		}

		/// <summary>Handler for when the value of Margins: Top changes </summary>
		private void on_kcidFilPgSetMTS_value_changed(object obj, EventArgs args)
		{
		}

		/// <summary>Handler for when the value of Margins: Bottom changes </summary>
		private void on_kcidFilPgSetMBS_value_changed(object obj, EventArgs args)
		{
		}

		/// <summary>Handler for when the value of Header from Edge changes </summary>
		private void on_kcidFilPgSetEHS_value_changed(object obj, EventArgs args)
		{
		}

		/// <summary>Handler for when the value of Footer from Edge changes </summary>
		private void on_kcidFilPgSetEFS_value_changed(object obj, EventArgs args)
		{
		}

		/// <summary>
		/// Handler for when Portrait/Landscape is toggled. Swaps the height and the width.
		/// </summary>
		private void on_kcidFilPgSetPort_toggled(object obj, EventArgs args)
		{
			Console.WriteLine("FilPgSetDlgController.on_kcidFilPgSetPort_toggled invoked.");
			updatePaperSize_ = false;
			SwapWidthHeight();
			updatePaperSize_ = true;
		}

		/// <summary>Handler for when the Width value changes </summary>
		private void on_kcidFilPgSetWS_value_changed(object obj, EventArgs args)
		{
			Console.WriteLine("FilPgSetDlgController.on_kcidFilPgSetWS_value_changed invoked");

			if (updatePaperSize_)
			{
				SetToCustom();
			}
		}

		/// <summary>Handler for when the Height value changes </summary>
		private void on_kcidFilPgSetHS_value_changed(object obj, EventArgs args)
		{
			Console.WriteLine("FilPgSetDlgController.on_kcidFilPgSetHS_value_changed invoked");

			if (updatePaperSize_)
			{
				SetToCustom();
			}
		}

		/// <summary>Sets the Paper Size to Custom</summary>
		private void SetToCustom()
		{
			kcidFilPgSetSize.Active = FilPgSetDlgModel.PaperTypes.Length - 1;
		}

		/// <summary>
		/// Handler for the when the Paper Size changes. Sets all the other widgets to
		/// appropriately reflect the change of choice.
		/// </summary>
		private void on_kcidFilPgSetSize_changed(object obj, EventArgs args)
		{
			Console.WriteLine("FilPgSetDlgController.on_kcidFilPgSetSize_changed invoked");

			FilPgSetDlgModel.PaperType active = (FilPgSetDlgModel.PaperType)kcidFilPgSetSize.Active;
			if (active != FilPgSetDlgModel.PaperType.Custom && active != (FilPgSetDlgModel.PaperType)(-1))
			{
				updatePaperSize_ = false;
				kcidFilPgSetWS.Value = FilPgSetDlgModel.PaperTypes[(int)active].Width;
				kcidFilPgSetHS.Value = FilPgSetDlgModel.PaperTypes[(int)active].Height;
				if (kcidFilPgSetLand.Active)
					SwapWidthHeight();
			}
			updatePaperSize_ = true;
		}

		/// <summary>Helper method for when Portrait/Landscape is toggled.</summary>
		private void SwapWidthHeight()
		{
			double temp = kcidFilPgSetWS.Value;
			kcidFilPgSetWS.Value = kcidFilPgSetHS.Value;
			kcidFilPgSetHS.Value = temp;
		}
	}
}

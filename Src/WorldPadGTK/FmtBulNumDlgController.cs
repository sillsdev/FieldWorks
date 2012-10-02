/*
 *    FmtBulNumDlgController.cs
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

namespace SIL.FieldWorks.WorldPad
{
	public class FmtBulNumDlgController : DialogController
	{
		[Widget] private Gtk.Dialog kridFmtBulNumDlg;
		[Widget] private Gtk.Container bulNumWidgets;
		// None
		[Widget] private Gtk.RadioButton kctidFbnRbNotAList;
		// Bullet
		[Widget] private Gtk.RadioButton kctidFbnRbBullet;
		[Widget] private Gtk.ComboBox kctidFbnCbBullet;
		// Number
		[Widget] private Gtk.RadioButton kctidFbnRbNumber;
		[Widget] private Gtk.Label kctidFbnNumSch;
		[Widget] private Gtk.CheckButton kctidFbnCxStartAt;
		[Widget] private Gtk.Entry kctidFbnEdTxtBef;
		[Widget] private Gtk.Entry kctidFbnEdTxtAft;
		// Unspecified
		[Widget] private Gtk.RadioButton kctidFbnRbUnspecified;

		// TODO Remove comment when missing file is added.
		// private FmtBulNumDlgModel model_;

		// TODO Remove comment when missing file is added.
		public FmtBulNumDlgController(/*FmtBulNumDlgModel*/ IDialogModel model) : base("kridFmtBulNumDlg", model)
		{
			// TODO Remove comment when missing file is added.
			// model_ = model;
		}

		public override Gtk.Container Widgets
		{
			get
			{
				return bulNumWidgets;
			}
		}

		protected override void Commit() { }

		private void on_kctidFbnRbBullet_toggled(object obj, EventArgs args)
		{
			Console.WriteLine("FmtBulNumDlgController.on_kctidFbnCbBullet_toggled invoked");
			if (kctidFbnRbBullet.Active)
			{
				// TODO Remove comment when missing file is added.
				// model_.Type = FmtBulNumDlgModel.BulNumType.Bullet;
				// Stopped work here, because Andrew told me not to continue
			}
		}

		private void on_kctidFbnRbNumber_toggled(object obj, EventArgs args)
		{
			Console.WriteLine("FmtBulNumDlgController.on_kctidFbnCbNumber_toggled invoked");
			// TODO Remove comment when missing file is added.
			//	if (kctidFbnRbNumber.Active)
					//model_.Type = FmtBulNumDlgModel.BulNumType.Number;
		}

		private void on_kctidFbnRbNotAList_toggled(object obj, EventArgs args)
		{
			Console.WriteLine("FmtBulNumDlgController.on_kctidFbnRbNotAList_toggled invoked");
			// TODO Remove comment when missing file is added.
			// if (kctidFbnRbNotAList.Active)
				//model_.Type = FmtBulNumDlgModel.BulNumType.None;
		}

		private void on_kctidFbnRbUnspecified_toggled(object obj, EventArgs args)
		{
			Console.WriteLine("FmtBulNumDlgController.on_kctidFbnRbUnspecified_toggled invoked");
			// TODO Remove comment when missing file is added.
			// if (kctidFbnRbUnspecified.Active)
				//model_.Type = FmtBulNumDlgModel.BulNumType.Unspecifed;
		}

		private void on_kctidFbnPbFont_clicked(object obj, EventArgs args)
		{
			Console.WriteLine("FmtBulNumDlgController.on_kctidFbnPbFont_clicked invoked");
		}

		private void on_kctidFbnCbBullet_changed(object obj, EventArgs args)
		{
			Console.WriteLine("FmtBulNumDlgController.on_kctidFbnCbBullet_changed invoked");
		}

		private void on_kctidFbnCbNumber_changed(object obj, EventArgs args)
		{
			Console.WriteLine("FmtBulNumDlgController.on_kctidFbnCbNumber_changed invoked");
		}

		private void on_kctidFbnCxStartAt_toggled(object obj, EventArgs args)
		{
			Console.WriteLine("FmtBulNumDlgController.on_kctidFbnCxStartAt_toggled invoked");
		}

		private void on_kctidFbnSpStartAt_value_changed(object obj, EventArgs args)
		{
			Console.WriteLine("FmtBulNumDlgController.on_kctidFbnSpStartAt_value_changed invoked");
		}
	}
}

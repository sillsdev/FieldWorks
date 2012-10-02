/*
 *    OldWritingSystemsDlgController.cs
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
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.Framework;

namespace SIL.FieldWorks.WorldPad
{
	public class OldWritingSystemsDlgController : DialogController
	{
		[Widget] private Gtk.Dialog kridOldWritingSystemsDlg;
		[Widget] private Gtk.TreeView kctidWritingSystems;
		[Widget] private Gtk.Entry kctidWsName;
		[Widget] private Gtk.Entry kctidWsCode;
		[Widget] private Gtk.Entry kctidWsDescrip;
		private OldWritingSystemsDlgModel model_;
		private FmtWrtSysDlgModel.Language[] languages_;
		private int selection_;

		public OldWritingSystemsDlgController(OldWritingSystemsDlgModel model) : base("kridOldWritingSystemsDlg", model)
		{
			Console.WriteLine("OldWritingSystemsDlgController.ctor invoked");
			model_ = model;

			kctidWritingSystems.Selection.Changed += on_kctidWritingSystems_selection_changed;
			languages_ = new FmtWrtSysDlgModel.Language[model_.Languages.Length];
			model_.Languages.CopyTo(languages_, 0);
			selection_ = 0;
			TreeStore store = Utils.CreateSimpleTreeView(kctidWritingSystems, OldWritingSystemsDlgModel.GetLanguageNames());
			Utils.SelectInTreeView(kctidWritingSystems, store, selection_.ToString());
		}

		protected override void Commit()
		{
			for (int i = 0; i < languages_.Length; i++)
			{
				model_.Languages[i].Name = languages_[i].Name;
				model_.Languages[i].Description = languages_[i].Description;
				model_.Languages[i].Code = languages_[i].Code;
			}
		}

		private void on_kctidWritingSystems_selection_changed(object obj, EventArgs args)
		{
			Console.WriteLine("OldWritingSystemsDlgController.on_kctidWritingSystems_selection_changed invoked");

			TreePath[] path = kctidWritingSystems.Selection.GetSelectedRows();
			if (path.Length > 0)
			{
				selection_ = int.Parse(path[0].ToString());
				FmtWrtSysDlgModel.Language lang = languages_[selection_];
				kctidWsName.Text = lang.Name;
				kctidWsDescrip.Text = lang.Description;
				kctidWsCode.Text = lang.Code;
			}
		}

		private void on_kctidWsName_changed(object obj, EventArgs args) {
			languages_[selection_].Name = kctidWsName.Text;
		}

		private void on_kctidWsDescrip_changed(object obj, EventArgs args) {
			languages_[selection_].Description = kctidWsDescrip.Text;
		}

		private void on_kctidWsCode_changed(object obj, EventArgs args) {
			languages_[selection_].Code = kctidWsCode.Text;
		}
	}
}

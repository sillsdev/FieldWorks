/*
 *    FmtWrtSysDlgController.cs
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
	public class FmtWrtSysDlgController : DialogController
	{
		[Widget] private Gtk.Dialog kridFmtWrtSysDlg;
		[Widget] private Gtk.TreeView kctidFmtWritingSystems;
		private FmtWrtSysDlgModel model_;

		public FmtWrtSysDlgController(FmtWrtSysDlgModel model) : base("kridFmtWrtSysDlg", model)
		{
			Console.WriteLine("FmtWrtSysDlgController.ctor invoked");
			model_ = model;

/*
			TreeStore store = new TreeStore(typeof(string));
			for (int i = 0; i < model_.Languages.Length; i++)
				store.AppendValues(model_.Languages[i].Name);

			kctidFmtWritingSystems.Model = store;
			TreeViewColumn col = new TreeViewColumn("Languages", new Gtk.CellRendererText (), "text", 0);
			kctidFmtWritingSystems.AppendColumn(col);
			*/
			TreeStore store = Utils.CreateSimpleTreeView(kctidFmtWritingSystems, FmtWrtSysDlgModel.GetLanguageNames());
			Utils.SelectInTreeView(kctidFmtWritingSystems, store, model_.Selection.ToString());
		}

		protected override void Commit() {
			TreePath[] tp = kctidFmtWritingSystems.Selection.GetSelectedRows();
			model_.Selection = tp[0].Indices[0];
		}
	}
}

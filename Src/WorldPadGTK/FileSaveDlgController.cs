/*
 *    FileSaveDlgController.cs
 *
 *    <purpose>
 *
 *    Jean-Marc Giffin - 2008-07-14
 *
 *    $Id$
 */

using System;
using System.IO;
using Glade;
using Gtk;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.WorldPad
{
	public class FileSaveDlgController : DialogController
	{
		[Widget] private Gtk.FileChooserDialog fileSaveDlg;

		private FileSaveModel model_;
		private IWorldPadDocModel worldPadDocModel_;

		public FileSaveDlgController(FileSaveModel model, IWorldPadDocModel worldPadDocModel) : base("fileSaveDlg", model)
		{
			if (model == null)
				throw new Exception("MODEL WAS NULL! (FileSave)");
			model_ = model;
			worldPadDocModel_ = worldPadDocModel;
			FileFilter filter = new FileFilter();
			filter.AddPattern("*.wpt");
			filter.AddPattern("*.wpx");
			fileSaveDlg.Filter = filter;

			Console.WriteLine("You have {0} open!", worldPadDocModel_.FileName);
			string path = Path.GetFullPath(Utils2.GetFilesPath(worldPadDocModel_.FileName));
			string file = Utils2.GetFileWithoutPath(worldPadDocModel_.FileName);
			Console.WriteLine("FILEEE:::: {0}", file);
			bool folderSet = fileSaveDlg.SetCurrentFolder(path);
			bool fileSet = fileSaveDlg.SetFilename(file);
		}

		public override string DialogFile() {
			return "glade/save.glade";
		}

		protected override void Commit() {
			Console.WriteLine("Commiting FileSave");
			model_.Filepath = fileSaveDlg.CurrentFolder;
			model_.Filename = fileSaveDlg.Filename;
			// Document Controller Save
			TextWriter tw = new StreamWriter(model_.Filename);
			tw.WriteLine(DateTime.Now);
			tw.Close();
		}

		protected override void on_dialog_response(object obj, ResponseArgs args)
		{
			Console.WriteLine("FileOpenDlgController.on_fileSaveDlg_response invoked");
			bool done = true;
			if (args.ResponseId == ResponseType.Ok)
			{
				Console.WriteLine("fileSaveDlg.Filename: {0}", fileSaveDlg.Filename);
				if (File.Exists(fileSaveDlg.Filename)) {
					Console.WriteLine("EXISTS");
					string warning = "Are you sure you want to overwrite this file?";
					MessageDialog md = new MessageDialog(null, DialogFlags.DestroyWithParent,
														 MessageType.Question,
														 ButtonsType.YesNo, warning);
					ResponseType answer = (ResponseType)md.Run();
					if (answer == ResponseType.No)
						done = false;
					md.Destroy();
				}
			}
			if (done)
			{
				Commit();
				fileSaveDlg.Hide();
			}
		}
	}
}

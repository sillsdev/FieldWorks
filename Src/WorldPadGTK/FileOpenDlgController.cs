/*
 *    FileOpenDlgController.cs
 *
 *    <purpose>
 *
 *    Andrew Weaver - 2008-05-01
 *
 *    $Id$
 *
 * UNCESSARY TO COMMENT
 */

using System;
using System.IO;
using Glade;
using Gtk;
using SIL.FieldWorks.Common.Framework;

namespace SIL.FieldWorks.WorldPad
{
	/// <summary>
	/// Probably not used currently.
	/// </summary>
	public class FileOpenDlgController : DialogController
	{
		public ResponseType ResponseId
		{ get { return responseId; } }

		public string Filename
		{ get { return filename; } }

		[Widget] private Gtk.FileChooserDialog fileOpenDlg;

		private ResponseType responseId;
		private string filename;
		private IWorldPadAppModel appModel;
		private IWorldPadAppController appController;
		private IWorldPadDocController docController;
		private FileOpenModel model_;

		private static FileOpenDlgController fileOpenDlgController = null;

		// Private constructor
		public FileOpenDlgController(FileOpenModel model) : base("fileOpenDlg", model)
		{
			if (model == null)
				throw new Exception("MODEL WAS NULL! (FileOpen)");
			model_ = model;
			FileFilter filter = new FileFilter();
			filter.AddPattern("*.wpt");
			filter.AddPattern("*.wpx");
			fileOpenDlg.Filter = filter;

			responseId = ResponseType.None;
			filename = null;
			string path = Path.GetFullPath(".");
			if (model.IsFilepathSet())
				path = model.Filepath;
			bool folderSet = fileOpenDlg.SetCurrentFolder(path);
			Console.WriteLine("folderSet: {0}", folderSet);
		}

		public IWorldPadAppController AppController
		{
			set
			{
				appController = value;
			}
		}

		public IWorldPadDocController DocController
		{
			set
			{
				docController = value;
			}
		}

		public override string DialogFile() {
			return DIALOGS;
		}

		protected override void Commit() {
			Console.WriteLine("Commiting FileOpen");
			model_.Filepath = fileOpenDlg.CurrentFolder;
			filename = fileOpenDlg.Filename;
			if (appController != null)
				appController.FileOpen(filename);
			if (docController != null)
				docController.FileOpen(filename);
		}

		protected override void on_dialog_response(object obj, ResponseArgs args)
		{
			Console.WriteLine("FileOpenDlgController.on_fileOpenDlg_response invoked");

			Console.WriteLine("Response Id: {0}", args.ResponseId);

			responseId = args.ResponseId;

			if (args.ResponseId == ResponseType.Ok)
			{
				Console.WriteLine("fileOpenDlg.Filename: {0}", fileOpenDlg.Filename);
				Commit();
			}

			fileOpenDlg.Hide();
		}
	}
}

/*
 *    WorldPadAppController.cs
 *
 *    <purpose>
 *
 *    Andrew Weaver - 2008-05-01
 *
 *    $Id$
 */

using System;
using System.Collections;
using Gtk;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FwCoreDlgs;

namespace SIL.FieldWorks.WorldPad
{
	public class WorldPadAppController : IWorldPadAppController
	{
		public const string DEFAULT_DOCUMENT = "hello.wpx";
		public const string HELP = "--help";
		public const string ABOUT =
			  "SIL WorldPad 1.0.0, word-processor supporting complex scripts\n"
			+ "Copyright 2000-2008 SIL International\n"
			+ "http://www.sil.org/\n"
			+ "\n"
			+ "Usage:\n"
			+ "  worldpad [--help] [wpx_file...]\n"
			+ "\n"
			+ "Options:\n"
			+ "  --help Show this message";

		private IWorldPadAppModel appModel;
		private WpFindReplaceDlgController wpFindReplaceDlgController;
		private OptionsDlgController optionsDlgController;
		private OldWritingSystemsDlgController oldWritingSystemsDlgController;
		private FwHelpAbout helpAboutDlgController;
		private ArrayList docControllers = new ArrayList();

		public WorldPadAppController()
		{ // TODO: Is this constructor needed?
			Console.WriteLine("WorldPadAppController() invoked");
		}

		public WorldPadAppController(IWorldPadAppModel appModel, string[] documents)
		{
			Console.WriteLine("WorldPadAppController.ctor invoked");

			this.appModel = appModel;

			Application.Init();

			appModel.Init();

			//IWorldPadDocModel docModel = appModel.AddDoc();

			IWorldPadDocModel docModel;
			bool openDocs = true;

			if (documents.Length < 1)
			{
				Console.WriteLine("=== Setting to default");
				documents = new string[1];
				documents[0] = DEFAULT_DOCUMENT;
			}

			foreach (string doc in documents)
			{
				if (doc == HELP && openDocs)
				{
					openDocs = false;
					Console.WriteLine(ABOUT);
				}
			}

			if (openDocs)
			{
				try
				{
					docModel = appModel.AddDoc(documents[0]);
					for (int i = 1; i < documents.Length; i++)
					{
						FileOpen(documents[i]);
					}
				}
				catch (Exception e)
				{
					try
					{
					// The input file may have failed to deserialize.
					Console.WriteLine("WorldPadAppController.ctor(). An exception occurred: "+e.Message);
					// Load a blank document // TODO: (MarkS) Maybe only load a blank document if no other documents are yet loaded. But if no document is loaded, probably do load a blank document so the error message has a familiar environment, to the user, on top of which to appear. In fact, if there is no document and the only thing showing is the Gtk.MessageDialog, at least the way I specified now, then it doesn't even have an entry in the Gnome task bar.
					docModel = appModel.AddDoc(Utils.GetProgramDirectory() + DEFAULT_DOCUMENT);
					// Show an error message about what happened.
					// TODO gtk message dialog correctly
					string errorMessage = "WorldPadGTK: An error occurred while opening the file. Perhaps the file is partly corrupted. Error message: " + e.Message;
					Gtk.MessageDialog errorDlg = new Gtk.MessageDialog(null,Gtk.DialogFlags.Modal, Gtk.MessageType.Error, Gtk.ButtonsType.Ok, errorMessage);
					errorDlg.Run();
					errorDlg.Show();
					}
					catch (Exception e2)
					{
						string errorMessage = "WorldPadGTK: An error occurred while trying to load a blank document and report a previous loading error about loading a file, which might be partly corrupted. \nThis error: " + e2.Message +" \nPrevious error: " + e.Message;
						Gtk.MessageDialog errorDlg = new Gtk.MessageDialog(null,Gtk.DialogFlags.Modal, Gtk.MessageType.Error, Gtk.ButtonsType.Ok, errorMessage);
						errorDlg.Run();
						// TODO we are in a weird state. What do we do?
						return;//perhaps or perhaps not: docModel = null;
					}
				}

				docControllers.Add(new WorldPadDocController(docModel, this));
				Application.Run();
			}
		}

		public void Init()
		{
			Console.WriteLine("WorldPadAppController.Init() invoked");
		}

		public void FileNew()
		{
			Console.WriteLine("WorldPadAppController.FileNew() invoked");

			//IWorldPadDocModel docModel = appModel.AddDoc();
			IWorldPadDocModel docModel = appModel.AddDoc("default.wpt");

			docControllers.Add(new WorldPadDocController(docModel, this));
		}

		public void FileOpen(string filename)
		{
			Console.WriteLine("WorldpadAppController.FileOpen({0}) invoked", filename);

			IWorldPadDocModel docModel = appModel.AddDoc(filename);
			docControllers.Add(new WorldPadDocController(docModel, this));
		}

		// TODO: The following three methods will not be needed if doc "owns" the dialog
		private void on_FindNextClicked(object obj, EventArgs args)
		{
			Console.WriteLine("WorldPadAppController.on_FindNextClicked() invoked");

			// TODO: Send the request to the frontmost document window
		}

		private void on_ReplaceClicked(object obj, EventArgs args)
		{
			Console.WriteLine("WorldPadAppController.on_ReplaceClicked() invoked");

			// TODO: Send the request to the frontmost document window
		}

		private void on_ReplaceAllClicked(object obj, EventArgs args)
		{
			Console.WriteLine("WorldPadAppController.on_ReplaceAllClicked() invoked");

			// TODO: Send the request to the frontmost document window
		}
		// TODO: End of redundant methods (see above)

		public void Quit()
		{
			Console.WriteLine("WorldPadAppController.Quit() invoked");

			Application.Quit();
		}

		public void FileClose(IWorldPadDocController docController)
		{
			Console.WriteLine("WorldPadAppController.FileClose() invoked");

			// TODO: We first need to determine which window is being closed!
			//docControllers.RemoveAt(0);
			docControllers.Remove(docController);

			if (docControllers.Count < 1)
			{
				this.Quit();
			}
		}


		public void HideOptionsDlg()
		{
			Console.WriteLine("WorldPadAppController.HideOptionsDlg() invoked");

			optionsDlgController.Hide();
		}

		public void HideOldWritingSystemsDlg()
		{
			Console.WriteLine("WorldPadAppController.HideOldWritingSystemsDlg() invoked");

			oldWritingSystemsDlgController.Hide();
		}

		public void HideHelpAboutDlg()
		{
			Console.WriteLine("WorldPadAppController.HideHelpAboutDlg() invoked");

			helpAboutDlgController.Hide();
		}
	}
}

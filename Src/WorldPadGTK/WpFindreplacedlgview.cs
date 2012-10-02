/*
 *    wpfindreplacedlgview.cs
 *
 *    <purpose>
 *
 *    Andrew Weaver - 2008-05-01
 *
 *    $Id: wpfindreplacedlgview.cs,v 1.1 2008-05-01 18:26:22 weavera Exp $
 */

using System;
using System.IO;
using Gtk;
using Glade;

namespace SIL.FieldWorks.WorldPad
{
	public class WpFindReplaceDlgView
	{
		[Widget]
		private Gtk.Dialog FindReplaceDlg;
		[Widget]
		private Gtk.Notebook notebook1;

		private IWorldPadAppModel model;
		private IWorldPadAppController controller;

		// TODO: These will not be needed if doc becomes the dialog's "owner"
		public event EventHandler FindNextClicked;
		public event EventHandler ReplaceClicked;
		public event EventHandler ReplaceAllClicked;

		// TODO: Might this be better being "owned" by the DocController?
		public WpFindReplaceDlgView(IWorldPadAppController controller,
			IWorldPadAppModel model)
		{
			/*// Note: "using" will automatically release resources
			using (FileStream stream =
				new FileStream("GladeInterface.glade", FileMode.Open))
			{
				// Note: use of "stream" constructor
				Glade.XML gxml = new Glade.XML(stream, "FindReplaceDlg", null);
				gxml.Autoconnect(this);
			}*/

			Glade.XML gxml =
				new Glade.XML("glade/dialogs.glade", "FindReplaceDlg", null);
			gxml.Autoconnect(this);

			notebook1.Page = 1;  // Causes "Replace" tab to be frontmost

			/*FindReplaceDlg.Icon = new Gdk.Pixbuf("WorldPad.ico");*/
		}

		public void Init()
		{
			Console.WriteLine("WpFindReplaceDlgView.Init() invoked");
		}

		public void Show(IWorldPadDocView parent)
		{
			Console.WriteLine("WpFindReplaceDlgView.Show() invoked");

			// Note: Needed for GTK+ to honour WindowPosition.CenterOnParent.
			FindReplaceDlg.TransientFor = parent.Window;

			FindReplaceDlg.Show();
		}

		// TODO: If doc becomes the dialog's "owner", this will merely call
		//       a DocController method
		private void on_FindNextButton_clicked(object obj, EventArgs args)
		{
			Console.WriteLine("FindNextButton was clicked");

			if (FindNextClicked != null)
				FindNextClicked(this, new EventArgs());
		}

		// TODO: If doc becomes the dialog's "owner", this will merely call
		//       a DocController method
		private void on_ReplaceButton_clicked(object obj, EventArgs args)
		{
			Console.WriteLine("ReplaceButton was clicked");

			if (ReplaceClicked != null)
				ReplaceClicked(this, new EventArgs());
		}

		// TODO: If doc becomes the dialog's "owner", this will merely call
		//       a DocController method
		private void on_ReplaceAllButton_clicked(object obj, EventArgs args)
		{
			Console.WriteLine("ReplaceAllButton was clicked");

			if (ReplaceAllClicked != null)
				ReplaceAllClicked(this, new EventArgs());
		}

		// This event is always fired when attempting to dismiss the dialog
		private void on_FindReplaceDlg_response(object obj, ResponseArgs args)
		{
			Console.WriteLine("A response event has occurred");

			if (args.ResponseId != ResponseType.Help)
				FindReplaceDlg.Hide();
		}

		// This event fires after a response event
		private void on_FindReplaceDlg_delete_event(object obj, DeleteEventArgs args)
		{
			Console.WriteLine("A delete event has occurred");

			args.RetVal = true;  // Prevents the dialog from closing
		}
/*
		// TODO: How to dispose the resources?
		protected override void Dispose(bool disposing) {

			if (disposing) {

				gxml.Finalize();  // gxml must be an instance variable!

			}
		}
*/
	}
}

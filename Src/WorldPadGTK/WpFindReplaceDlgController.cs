/*
 *    WpFindReplaceDlgController.cs
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
	public class WpFindReplaceDlgController : DialogController
	{
		[Widget] private Gtk.Dialog FindReplaceDlg;
		[Widget] private Gtk.Notebook notebook1;

		// TODO: These will not be needed if doc becomes the dialog's "owner"
		public event EventHandler FindNextClicked;
		public event EventHandler ReplaceClicked;
		public event EventHandler ReplaceAllClicked;

		// TODO: Might this be better being "owned" by the DocController?
		public WpFindReplaceDlgController(IDialogModel model) : base("FindReplaceDlg", model)
		{
			//notebook1.Page = 1;  // Causes "Replace" tab to be frontmost
		}

		public override string DialogFile()
		{
			return DIALOGS;
		}

		public void Init()
		{
			Console.WriteLine("WpFindReplaceDlgController.Init() invoked");
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

		protected override void Commit() { }
	}
}

/*
 *    HelpAboutDlgController.cs
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

namespace SIL.FieldWorks.WorldPad
{
	public class HelpAboutDlgController : DialogController
	{
		[Widget]
		private Gtk.Dialog kridHelpAboutDlg;

		public HelpAboutDlgController(IDialogModel model) : base("kridHelpAboutDlg", model)
		{
		}

		protected override void Commit() { }
	}
}

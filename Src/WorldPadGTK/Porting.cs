using System;

namespace System.Windows.Forms
{
	// This interface must be implemented by all classes that contain a Gtk.Window
	// or a Gtk.Dialog.
	public interface IWin32Window  // TODO: change the name of this interface?
	{
		Gtk.Window Window { get; }
	}

	public enum DialogResult  // Note: This would normally be in System.Windows.Forms
	{
		Abort,
		Cancel,
		Ignore,
		No,
		None,
		OK,
		Retry,
		Yes
	}
/*
	public enum ResponseType  // Note: This would normally be in Gtk
	{
		// All members of the Gtk.ResponseType enumeration have values less than zero
		Cancel = Gtk.ResponseType.Cancel,
		DeleteEvent = Gtk.ResponseType.DeleteEvent,
		No = Gtk.ResponseType.No,
		Ok = Gtk.ResponseType.Ok,
		Yes = Gtk.ResponseType.Yes,
		// Additional members for compatibility with System.Windows.Forms.DialogResult
		Abort = 1,
		Ignore,
		Retry
	}
*/
}

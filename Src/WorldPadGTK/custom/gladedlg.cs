using System;
using Gtk;
using Glade;

public class GladeDlg {

	[Widget]
	public Gtk.Dialog dialog1;

	public GladeDlg() {

		Console.WriteLine("Entering GladeDlg.ctor");

		GladeApp.CustomWidgetFound += on_custom_widget_found;

		Glade.XML gxml = new Glade.XML("custom.glade", "dialog1", null);
		gxml.Autoconnect(this);

		Console.WriteLine("Exiting GladeDlg.ctor");

	}

	public Gtk.Widget on_custom_widget_found(XML xml, string func_name, string name,
			string string1, string string2, int int1, int int2) {

		Console.WriteLine("Entering GladeDlg.on_custom_widget_found");

		Gtk.Label substitute = new Gtk.Label("Read me!");
		substitute.Realized += on_substitute_realized;
		substitute.Show();

		GladeApp.CustomWidgetFound -= on_custom_widget_found;

		Console.WriteLine("Exiting GladeDlg.on_custom_widget_found");

		return substitute;

	}

	public void on_substitute_realized(object o, EventArgs args) {

		Console.WriteLine("Entering GladeDlg.on_substitute_realized");

		Console.WriteLine("Exiting GladeDlg.on_substitute_realized");

	}
}

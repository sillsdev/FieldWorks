using System;
using Gtk;
using Glade;

public class GladeWin {

	public GladeWin() {

		Console.WriteLine("Entering GladeWin.ctor");

		GladeApp.CustomWidgetFound += on_custom_widget_found;

		Glade.XML gxml = new Glade.XML("custom.glade", "window1", null);
		gxml.Autoconnect(this);

		Console.WriteLine("Exiting GladeWin.ctor");

	}

	public Gtk.Widget on_custom_widget_found(XML xml, string func_name, string name,
			string string1, string string2, int int1, int int2) {

		Console.WriteLine("Entering GladeWin.on_custom_widget_found");

		Gtk.Button substitute = new Gtk.Button("Click me!");
		substitute.Clicked += on_substitute_clicked;
		substitute.Show();

		GladeApp.CustomWidgetFound -= on_custom_widget_found;

		Console.WriteLine("Exiting GladeWin.on_custom_widget_found");

		return substitute;

	}

	public void on_substitute_clicked(object o, EventArgs args) {

		Console.WriteLine("Entering GladeWin.on_substitute_clicked");

		GladeDlg dlg = new GladeDlg();
		dlg.dialog1.Run();
		dlg.dialog1.Destroy();

		Console.WriteLine("Exiting GladeWin.on_substitute_clicked");

	}

	public void on_window1_delete_event(object o, DeleteEventArgs args) {

		Application.Quit();

		args.RetVal = true;  // prevents further handlers from getting the event

	}
}

using System;
using System.IO;
using Gtk;
using Glade;

public class GladeApp {

	public static void Main(string[] args) {

		new GladeApp(args);

	}

	public GladeApp(string[] args) {

		Application.Init();

		XML.SetCustomHandler(new XMLCustomWidgetHandler(on_custom_widget_found));
		// Note: the above method is deprecated, but its replacement (below)
		//       cannot yet be used (requires GTK# 2.4 or later).
		//		XML.CustomHandler = new XMLCustomWidgetHandler(on_custom_widget_found);

		// Note: "using" will automatically release resources
		using (FileStream stream = new FileStream("custom.glade",
				FileMode.Open)) {
			// Note: use of "stream" constructor
			Glade.XML gxml = new Glade.XML(stream, "window1", null);
			gxml.Autoconnect(this);
		}

		Application.Run();

	}

	public Gtk.Widget on_custom_widget_found(XML xml, string func_name, string name,
			string string1, string string2, int int1, int int2) {

		Gtk.Button substitute = new Gtk.Button("Click me!");
		substitute.Clicked += on_substitute_clicked;
		substitute.Show();
		return substitute;

	}

	public void on_substitute_clicked(object o, EventArgs args) {

		Console.WriteLine("substitute was clicked");

	}
/*
	public void on_fwFindReplaceDlg_delete_event(object o, DeleteEventArgs args) {

		Application.Quit();

		args.RetVal = true;  // prevents further handlers from getting the event

	}

*/
}

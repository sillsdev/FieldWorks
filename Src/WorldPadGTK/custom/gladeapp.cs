using System;  // Included only for 'Console.WriteLine' debugging statements
using Gtk;
using Glade;

public class GladeApp {

	public static event Glade.XMLCustomWidgetHandler CustomWidgetFound;

	public static void Main(string[] args) {

		new GladeApp(args);

	}

	public GladeApp(string[] args) {

		Console.WriteLine("Entering GladeApp.ctor");

		Application.Init();

		XML.CustomHandler = new Glade.XMLCustomWidgetHandler(on_custom_widget_found);

		GladeWin w = new GladeWin();

		Application.Run();

		Console.WriteLine("Exiting GladeApp.ctor");

	}

	public Gtk.Widget on_custom_widget_found(XML xml, string func_name, string name,
			string string1, string string2, int int1, int int2) {

		Console.WriteLine("Entering GladeApp.on_custom_widget_found");

		Gtk.Widget substitute = null;

		if (CustomWidgetFound != null) {
			substitute = CustomWidgetFound(xml, func_name, name, string1, string2, int1, int2);
		}

		Console.WriteLine("Exiting GladeApp.on_custom_widget_found");

		return substitute;

	}
}

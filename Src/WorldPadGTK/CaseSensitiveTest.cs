
using System;
using Gtk;

namespace SIL.FieldWorks
{
	public interface IWorldPadDocView
	{
		Gtk.Window Window { get; }
	}

	public class CaseSensitiveTest : IWorldPadDocView
	{
		public static void Main(string[] args)
		{
			new GladeApp(args);
		}

		private Window w;

		public Gtk.Window Window
		{
			get { return w; }
		}

		public CaseSensitiveTest(string[] args)
		{
			Application.Init();

			w = new Window("Dialog Viewer");
			Button b = new Button("Hit me!");

			w.DeleteEvent += new DeleteEventHandler(Window_Delete);
			b.Clicked += new EventHandler(Button_Clicked);

			w.Add(b);
			w.SetDefaultSize(200, 100);
			w.ShowAll();

			Application.Run();
		}

		private void Window_Delete(object obj, DeleteEventArgs args)
		{
			//Console.WriteLine("A delete event (main window) has occurred");

			Application.Quit();
			args.RetVal = true;
		}

		private void Button_Clicked(object obj, EventArgs args)
		{
			//Console.WriteLine("A clicked event has occurred");

			OptionsDlg dlg = new OptionsDlg(new OptionsDlgModel());
			/*if (dlg.ShowDialog(this) == DialogResult.OK)*/
			/*DialogResult result = */dlg.Show/*Dialog*/(this);
			/*if (result == DialogResult.OK)
			{
				ExportUsfm export = new ExportUsfm(dlg.FileName, m_cache, m_bookFilter);
				export.MarkupSystem =
					dlg.ExportParatextMarkup ? MarkupType.Paratext : MarkupType.Toolbox;
				export.ExportInterleavedBackTranslation =
					dlg.ExportInterleavedBackTranslation;
				export.Run();
			}
			else
			{
				Console.WriteLine("DialogResult: {0}", result);
			}*/
		}
	}
}

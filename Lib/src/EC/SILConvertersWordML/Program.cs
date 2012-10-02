using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;

namespace SILConvertersWordML
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			if (Properties.Settings.Default.RecentFiles == null)
				Properties.Settings.Default.RecentFiles = new System.Collections.Specialized.StringCollection();

			if (Properties.Settings.Default.ConverterMappingRecentFiles == null)
				Properties.Settings.Default.ConverterMappingRecentFiles = new System.Collections.Specialized.StringCollection();

			myTimer.Tick += new EventHandler(TimerEventProcessor);
			myTimer.Interval = 500;    // half a second

			if ((args != null) && (args.Length > 0))
			{
				FileNames = args;
				myTimer.Start();
			}

			m_aForm = new FontsStylesForm();
			Application.Run(m_aForm);
		}

		public static FontsStylesForm m_aForm = null;
		public static string[] FileNames = null;
		public static System.Windows.Forms.Timer myTimer = new System.Windows.Forms.Timer();

		private static void TimerEventProcessor(Object myObject, EventArgs myEventArgs)
		{
			myTimer.Stop();
			if ((FileNames != null) && (FileNames.Length > 0))
				m_aForm.OpenDocuments(FileNames);
		}

		public static bool IsOnlyOneDoc
		{
			get { return (FileNames.Length == 1); }
		}
	}
}
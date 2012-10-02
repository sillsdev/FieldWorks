using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace DChartHelper
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

			if (Properties.Settings.Default.RecentCharts == null)
				Properties.Settings.Default.RecentCharts = new System.Collections.Specialized.StringCollection();

			Properties.Settings.Default.Save();

			if( args.Length == 0 )
				Application.Run(new DiscourseChartForm());
			else
				Application.Run(new DiscourseChartForm(args[0]));
		}
	}
}
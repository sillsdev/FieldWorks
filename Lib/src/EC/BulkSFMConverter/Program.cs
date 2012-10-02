using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SFMConv
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			bool bNeedToSave = false;
			if (Properties.Settings.Default.RecentFiles == null)
			{
				Properties.Settings.Default.RecentFiles = new System.Collections.Specialized.StringCollection();
				bNeedToSave = true;
			}

			if (Properties.Settings.Default.RecentFilesCodePages == null)
			{
				Properties.Settings.Default.RecentFilesCodePages = new System.Collections.Specialized.StringCollection();
				bNeedToSave = true;
			}

			if (Properties.Settings.Default.ConverterMappingRecentFiles == null)
			{
				Properties.Settings.Default.ConverterMappingRecentFiles = new System.Collections.Specialized.StringCollection();
				bNeedToSave = true;
			}

			if (bNeedToSave)
				Properties.Settings.Default.Save();

			m_aForm = new SCConvForm();
			Application.Run(m_aForm);
		}

		public static SCConvForm m_aForm = null;
	}
}
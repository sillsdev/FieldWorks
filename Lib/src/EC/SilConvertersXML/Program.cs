using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;                        // Path
using System.Reflection;                // for Assembly

namespace SilConvertersXML
{
	static class Program
	{
		public static XMLViewForm m_aForm = null;
		private static bool m_bModified = false;

		const int cnMaxRecentFiles = 15;
		const string strDirtySuffix = " *";

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

			if (Properties.Settings.Default.RecentFilters == null)
				Properties.Settings.Default.RecentFilters = new System.Collections.Specialized.StringCollection();

			m_aForm = new XMLViewForm();

			if (args.Length > 0)
			{
				string strFileSpecToOpen = (string)args[0];
				if (!File.Exists(strFileSpecToOpen))
				{
					PrintUsage();
					return;
				}

				m_aForm.OpenDocument(strFileSpecToOpen);

				if (args.Length > 1)
				{
					string strSwitch = args[1];
					if (strSwitch != "/cm")
					{
						PrintUsage();
						return;
					}
					else if (args.Length > 2)
					{
						string strConverterMappingFile2Use = args[2];
						if (!File.Exists(strConverterMappingFile2Use))
						{
							if (File.Exists(Application.UserAppDataPath + @"\" + strConverterMappingFile2Use))
								strConverterMappingFile2Use = Application.UserAppDataPath + @"\" + strConverterMappingFile2Use;
							else
							{
								PrintUsage();
								return;
							}
						}

						m_aForm.LoadConverterMappingFile(strConverterMappingFile2Use);
						string strOutputFileSpec = strFileSpecToOpen;
						if (args.Length > 3)
							strOutputFileSpec = args[3];
						m_aForm.ProcessAndSave(false, strOutputFileSpec);
						return;
					}
				}
			}

			Application.Run(m_aForm);
		}

		public static void PrintUsage()
		{
			Assembly assy = Assembly.GetExecutingAssembly();
			string strUsage = String.Format("Usage: {1} <xmlfile2open>.xml (/cm <converterMappingFile2Use>.xcm) (<xmlfile2save>.xml){0}{0} where the xml file (i.e. <xmlfile2open>.xml) will be automatically converted if you provide the '/cm' switch and the converter mapping file (i.e. <converterMappingFile2Use>.xcm). If you want to save it with a different name, you can optionally provide the <xmlfile2save>.xml parameter",
				Environment.NewLine, assy.ManifestModule.Name);
			Console.WriteLine(strUsage);
			MessageBox.Show(strUsage, XMLViewForm.cstrCaption);
		}

		public static void AddFilenameToTitle(string strFileSpec)
		{
			if (strFileSpec == null)
			{
				m_aForm.Text = XMLViewForm.cstrCaption;
			}
			else
			{
				string strTitleName = Path.GetFileName(strFileSpec);
				m_aForm.Text = String.Format("{0} -- {1}", XMLViewForm.cstrCaption, strTitleName);
				Modified = Modified;    // causes the dirty symbol to show up if needed

				// add this filename to the list of recently used files
				foreach (string strRecentFile in Properties.Settings.Default.RecentFiles)
					if (strRecentFile.ToLowerInvariant() == strFileSpec.ToLowerInvariant())
					{
						Properties.Settings.Default.RecentFiles.Remove(strRecentFile);
						break;
					}

				if (Properties.Settings.Default.RecentFiles.Count > cnMaxRecentFiles)
					Properties.Settings.Default.RecentFiles.RemoveAt(cnMaxRecentFiles);

				Properties.Settings.Default.RecentFiles.Insert(0, strFileSpec);
				Properties.Settings.Default.Save();
			}
		}

		public static void AddRecentXPathExpression(string strXPathExpression)
		{
			foreach (string strRecentExpression in Properties.Settings.Default.RecentFilters)
				if (strRecentExpression.ToLowerInvariant() == strXPathExpression.ToLowerInvariant())
				{
					Properties.Settings.Default.RecentFilters.Remove(strRecentExpression);
					break;
				}

			if (Properties.Settings.Default.RecentFilters.Count > cnMaxRecentFiles)
				Properties.Settings.Default.RecentFilters.RemoveAt(cnMaxRecentFiles);

			Properties.Settings.Default.RecentFilters.Insert(0, strXPathExpression);
			Properties.Settings.Default.Save();
		}

		public static bool Modified
		{
			get { return m_bModified; }
			set
			{
				m_bModified = value;
				if (m_aForm != null)
				{
					string strTitle = m_aForm.Text;
					if (m_bModified)
					{
						if (strTitle.IndexOf(strDirtySuffix) == -1)
							strTitle += strDirtySuffix;
					}
					else
					{
						if (strTitle.IndexOf(strDirtySuffix) != -1)
							strTitle = strTitle.Substring(0, strTitle.Length - strDirtySuffix.Length);
					}

					m_aForm.Text = strTitle;
				}
			}
		}
	}
}
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Text;                  // Encoding
using System.Diagnostics;           // for Debug
using System.Drawing;               // Rectangle
using System.IO;                    // Path
using System.Runtime.InteropServices;   // DLLImport
using ECInterfaces;
using SilEncConverters40;

namespace TECkit_Mapping_Editor
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

			// create the form and *Run* it
			Encoding enc = Encoding.UTF8;
			m_aForm = new TECkitMapEditorForm(enc);

			if( Properties.Settings.Default.RecentFiles == null )
				Properties.Settings.Default.RecentFiles = new System.Collections.Specialized.StringCollection();

			// wait until we have a reference to the form before doing the initial
			//  "open" or "new" (which might result in the form's title being set
			//  below, which requires the m_aForm reference--i.e. moved here from the TMEF ctor)
			bool bSomethingOpened = false;
			if (args.Length > 0)
				bSomethingOpened = m_aForm.OpenDocument(args[0]);

			if( !bSomethingOpened ) // sometimes the open fails, so we have to have a fallback
				m_aForm.NewDocument();

			// start a timer to save and compile after 1/2 a second of stopped typing
			myTimer.Tick += new EventHandler(TimerEventProcessor);
			myTimer.Interval = 2500;    // every second
			myTimer.Start();

			Application.Run(m_aForm);

			// clean up timer (just in case)
			myTimer.Stop();
		}

		const string cstrFormTitleDefault = "TECkit Mapping Editor";
		const string cstrFormTitleDefaultCodePointForm = "Character Map";
		const string cstrFormTitleEpilogueLhs = " for Left-hand side Font";
		const string cstrFormTitleEpilogueRhs = " for Right-hand side Font";
		const string strDirtySuffix = " *";
		const int nMaxRecentFiles = 15;

		public static void AddFilenameToTitle(string strFilename)
		{
			if (strFilename == null)
			{
				m_aForm.Text = cstrFormTitleDefault;
				m_aForm.m_formDisplayUnicodeNamesLhs.Text = cstrFormTitleDefaultCodePointForm + cstrFormTitleEpilogueLhs;
				m_aForm.m_formDisplayUnicodeNamesRhs.Text = cstrFormTitleDefaultCodePointForm + cstrFormTitleEpilogueRhs;
			}
			else
			{
				string strTitleName = Path.GetFileName(strFilename);
				m_aForm.Text = String.Format("{0} -- {1}", cstrFormTitleDefault, strTitleName);
				m_aForm.m_formDisplayUnicodeNamesLhs.Text = String.Format("{0} for Left-hand side -- {1}", cstrFormTitleDefaultCodePointForm, strTitleName);
				m_aForm.m_formDisplayUnicodeNamesRhs.Text = String.Format("{0} for Right-hand side -- {1}", cstrFormTitleDefaultCodePointForm, strTitleName);
				Modified = Modified;    // causes the dirty symbol to show up if needed

				// add this filename to the list of recently used files
				if (Properties.Settings.Default.RecentFiles.Contains(strFilename))
					Properties.Settings.Default.RecentFiles.Remove(strFilename);
				else if (Properties.Settings.Default.RecentFiles.Count > nMaxRecentFiles)
					Properties.Settings.Default.RecentFiles.RemoveAt(nMaxRecentFiles);

				Properties.Settings.Default.RecentFiles.Insert(0, strFilename);
				Properties.Settings.Default.Save();
			}
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

		static bool m_bModified = false;
		static bool m_bFirstTime = true;
		static TECkitMapEditorForm m_aForm = null;
		public static System.Windows.Forms.Timer myTimer = new System.Windows.Forms.Timer();

		private static void TimerEventProcessor(Object myObject, EventArgs myEventArgs)
		{
			if (m_bFirstTime)
			{
				if ((m_aForm == null) || (m_aForm.m_formDisplayUnicodeNamesLhs == null) || (m_aForm.m_formDisplayUnicodeNamesRhs == null))
					return; // wait until they exist...

				m_aForm.DisplayUnicodeCodePointForms();

				m_aForm.richTextBoxMapEditor.Focus();
				m_aForm.UpdateStatusBar();
				m_bFirstTime = false;
			}

			myTimer.Stop();

			if( m_aForm.DoAutoCompile() )
				m_aForm.SaveTempAndCompile();
		}

		internal static void SetBoundsClue(string cstrClueHeader, Rectangle rectBounds)
		{
			m_aForm.SetBoundsClue(cstrClueHeader, rectBounds);
		}

		internal static void AddStringToEditor(string str)
		{
			m_aForm.AddStringToEditor(str);
		}

		internal static void AddCharToSampleBox(char ch, bool bLhs)
		{
			m_aForm.AddCharToSampleBox(ch, bLhs);
		}

		internal static void RestartTimer()
		{
			myTimer.Stop();
			myTimer.Enabled = true;
		}

		internal static void LaunchProgram(string strProgram, string strArguments)
		{
			try
			{
				Process myProcess = new Process();

				myProcess.StartInfo.FileName = strProgram;
				myProcess.StartInfo.Arguments = strArguments;
				myProcess.Start();
			}
			catch { }    // we tried...
		}

		internal static Font GetSafeFont(string strFontName, float emSize)
		{
			Font font = null;
			try
			{
				font = new Font(strFontName, emSize);
			}
			catch
			{
				// sometimes, certain fonts only support Bold or Italic...
				try
				{
					font = new Font(strFontName, emSize, FontStyle.Bold);
				}
				catch
				{
					try
					{
						font = new Font(strFontName, emSize, FontStyle.Italic);
					}
					catch
					{
						// Otherwise, we have no idea what to do...
						MessageBox.Show(String.Format("'{0}' is an unsupported font!", strFontName), TECkitMapEditorForm.cstrCaption);
						font = System.Drawing.SystemFonts.DefaultFont;
					}
				}
			}
			return font;
		}

		[DllImport("TECkit_Compiler_x86", SetLastError = true)]
		static extern unsafe byte* TECkit_GetTECkitName(UInt32 usv);

		internal static unsafe string GetUnicodeName(char ch)
		{
			UInt32 usv = ch;
			byte* pszUnicodeName = TECkit_GetTECkitName(usv);
			byte[] baUnicodeName = ECNormalizeData.ByteStarToByteArr(pszUnicodeName);
			string strUnicodeName = Encoding.ASCII.GetString(baUnicodeName);
			return strUnicodeName;
		}
	}
}
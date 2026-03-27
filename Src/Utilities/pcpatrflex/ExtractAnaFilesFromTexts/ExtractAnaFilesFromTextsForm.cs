// Copyright (c) 2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using Microsoft.Win32;
using SIL.DisambiguateInFLExDB;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainServices;
using SIL.PrepFLExDB;
using SIL.WritingSystems;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SIL.PcPatrFLEx
{
	public partial class ExtractAnaFilesFromTextsForm : Form
	{
		public LcmCache Cache { get; set; }

		private IList<IText> Texts { get; set; }
		private FLExDBExtractor Extractor { get; set; }
		private Font AnalysisFont { get; set; }

		public static string m_strRegKey = "Software\\SIL\\ExtractAnaFilesFromTexts";
		const string m_strLastSelectedTexts = "LastSelectedTexts";
		const string m_strLocationX = "LocationX";
		const string m_strLocationY = "LocationY";
		const string m_strSizeHeight = "SizeHeight";
		const string m_strSizeWidth = "SizeWidth";
		const string m_strWindowState = "WindowState";

		public Rectangle RectNormal { get; set; }

		public string LastSelectedTexts { get; set; }

		private RegistryKey regkey;

		public ExtractAnaFilesFromTextsForm()
		{
			InitializeComponent();
			btnExtract.Enabled = false;

			//try
			//{
				regkey = Registry.CurrentUser.OpenSubKey(m_strRegKey);
				if (regkey != null)
				{
					Cursor.Current = Cursors.WaitCursor;
					Application.DoEvents();
					RetrieveRegistryInfo();
					regkey.Close();
					DesktopBounds = RectNormal;
					WindowState = WindowState;
					StartPosition = FormStartPosition.Manual;
					if (Cache != null && Cache.LangProject.Texts.Count > 0)
						btnExtract.Enabled = true;
					else
						btnExtract.Enabled = false;
					Cursor.Current = Cursors.Default;
				}
			//}
			//catch (Exception e)
			//{
			//	throw (e);
			//}
		}

		void RetrieveRegistryInfo()
		{
			// Window location
			int iX = (int)regkey.GetValue(m_strLocationX, 100);
			int iY = (int)regkey.GetValue(m_strLocationY, 100);
			int iWidth = (int)regkey.GetValue(m_strSizeWidth, 809); // 1228);
			int iHeight = (int)regkey.GetValue(m_strSizeHeight, 670); // 947);
			RectNormal = new Rectangle(iX, iY, iWidth, iHeight);
			// Set form properties
			WindowState = (FormWindowState)regkey.GetValue(m_strWindowState, 0);
			// selected texts
			LastSelectedTexts = (string)regkey.GetValue(m_strLastSelectedTexts, " ");
		}

		public void SaveRegistryInfo()
		{
			regkey = Registry.CurrentUser.OpenSubKey(m_strRegKey, true);
			if (regkey == null)
			{
				regkey = Registry.CurrentUser.CreateSubKey(m_strRegKey);
			}

			// Window position and location
			regkey.SetValue(m_strWindowState, (int)WindowState);
			regkey.SetValue(m_strLocationX, RectNormal.X);
			regkey.SetValue(m_strLocationY, RectNormal.Y);
			regkey.SetValue(m_strSizeWidth, RectNormal.Width);
			regkey.SetValue(m_strSizeHeight, RectNormal.Height);

			// selected texts
			var sb = new StringBuilder();
			foreach (var text in lbTexts.SelectedItems)
			{
				var selectedText = text as IText;
				if (selectedText == null)
					continue;
				sb.Append(selectedText.Guid);
				sb.Append(" ");
			}
			regkey.SetValue(m_strLastSelectedTexts, sb.ToString());
			regkey.Close();
		}

		private static Font CreateFont(CoreWritingSystemDefinition wsDef)
		{
			float fontSize = (wsDef.DefaultFontSize == 0) ? 10 : wsDef.DefaultFontSize;
			var fStyle = FontStyle.Regular;
			if (wsDef.DefaultFontFeatures.Contains("Bold"))
			{
				fStyle |= FontStyle.Bold;
			}
			if (wsDef.DefaultFontFeatures.Contains("Italic"))
			{
				fStyle |= FontStyle.Italic;
			}
			return new Font(wsDef.DefaultFontName, fontSize, fStyle);
		}

		private void EnsureDatabaseHasBeenPrepped()
		{
			var preparer = new Preparer(Cache, false);
			preparer.AddPCPATRList();
			preparer.AddPCPATRSenseCustomField();
		}

		public void PrepareForm()
		{
			if (Cache != null)
			{
				EnsureDatabaseHasBeenPrepped();
				Extractor = new FLExDBExtractor(Cache);
				AnalysisFont = CreateFont(Cache.LanguageProject.DefaultAnalysisWritingSystem);
				lbTexts.Font = AnalysisFont;
				FillTextsListBox();
				SetLastSelectedItems();
			}
		}

		private void SetLastSelectedItems()
		{
			if (!String.IsNullOrEmpty(LastSelectedTexts) && LastSelectedTexts.Length > 1)
			{
				lbTexts.ClearSelected();
				var selectedTexts = LastSelectedTexts.Split(' ');
				var texts = lbTexts.Items;
				for (int i = 0; i < lbTexts.Items.Count; i++)
				{
					var itext = lbTexts.Items[i] as IText;
					foreach (string sGuid in selectedTexts)
					{
						if ((itext.Guid.ToString()).Equals(sGuid))
						{
							lbTexts.SetSelected(i, true);
							break;
						}
					}
				}
			}
		}

		private void FillTextsListBox()
		{
			Texts = Cache.LanguageProject.Texts
				.Where(t => t.ContentsOA != null)
				.Cast<IText>()
				.OrderBy(t => t.ShortName)
				.ToList();
			lbTexts.DataSource = Texts;
			if (Texts.Count > 0)
				btnExtract.Enabled = true;
		}

		private void OnFormClosing(object sender, EventArgs e)
		{
			SaveRegistryInfo();
		}

		private string GetAnaForm(IText selectedTextToShow)
		{
			var sb = new StringBuilder();
			var contents = selectedTextToShow.ContentsOA;
			IList<IStPara> paragraphs = contents.ParagraphsOS;
			foreach (IStPara para in paragraphs)
			{
				var paraUse = para as IStTxtPara;
				if (paraUse != null)
				{
					foreach (var segment in paraUse.SegmentsOS)
					{
						var ana = Extractor.ExtractTextSegmentAsANA(segment);
						if (ana.Length == 0)
						{
							MessageBox.Show(
								"Warning! No ANA value was found for segment='"
									+ segment.GetBaselineText(0).Text
									+ "'.  Will skip it."
							);
							continue;
						}
						sb.Append(ana.Substring(0, ana.Length - 1)); // skip final extra nl
																	 // Now add period so PcPatr will treat it as an end of a sentence
						sb.Append("\\n .\n\n");
					}
				}
			}
			return sb.ToString();
		}

		protected override void OnMove(EventArgs ea)
		{
			base.OnMove(ea);

			if (WindowState == FormWindowState.Normal)
				RectNormal = DesktopBounds;
		}

		protected override void OnResize(EventArgs ea)
		{
			base.OnResize(ea);

			if (WindowState == FormWindowState.Normal)
				RectNormal = DesktopBounds;
		}

		private void ExtractAnaFromSelectedTexts_Click(object sender, EventArgs e)
		{
			Cursor.Current = Cursors.WaitCursor;
			Application.DoEvents();
			foreach (var text in lbTexts.SelectedItems)
			{
				var selectedText = text as IText;
				if (selectedText == null)
					continue;
				string ana = GetAnaForm(selectedText);
				var textName = selectedText.Name.BestAnalysisVernacularAlternative.Text;
				if (String.IsNullOrEmpty(textName))
					textName = "text" + selectedText.Guid.ToString();
				textName = MakeValidFileName(textName);
				string anaFile = Path.Combine(Path.GetTempPath(), textName + ".ana");
				File.WriteAllText(anaFile, ana);
			}
			Cursor.Current = Cursors.Default;
		}

		// Following taken from https://stackoverflow.com/questions/309485/c-sharp-sanitize-file-name
		private static string MakeValidFileName(string name)
		{
			string invalidChars = System.Text.RegularExpressions.Regex.Escape(
				new string(System.IO.Path.GetInvalidFileNameChars())
			);
			string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

			return System.Text.RegularExpressions.Regex
				.Replace(name, invalidRegStr, "_")
				.TrimEnd('.');
		}
	}
}

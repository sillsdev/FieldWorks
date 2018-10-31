// Copyright (c) 2009-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>
	/// This dialog presents the user with a choice of the available anthropology category
	/// lists.
	/// </summary>
	public partial class FwCheckAnthroListDlg : Form
	{
		/// <summary>choice value for custom, user defined list</summary>
		public const int kralUserDef = -3;
		/// <summary>choice value for standard OCM list</summary>
		public const int kralOCM = -2;
		/// <summary>choice value for enhanced OCM list ("FRAME")</summary>
		public const int kralFRAME = -1;
		private string m_sDescription;
		private IHelpTopicProvider m_helpTopicProvider;
		private string m_helpTopic = "khtpFwCheckAnthroListDlg";

		/// <summary />
		public FwCheckAnthroListDlg()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Initialize the dialog.
		/// </summary>
		public void SetValues(bool fHaveOCM, bool fHaveFRAME, List<string> rgsAnthroFiles, IHelpTopicProvider helpTopicProvider)
		{
			m_radioOCM.Enabled = fHaveOCM;
			m_radioFRAME.Enabled = fHaveFRAME;
			m_helpTopicProvider = helpTopicProvider;

			m_radioOther.Checked = false;
			if (rgsAnthroFiles.Count == 0)
			{
				m_radioOther.Enabled = false;
				m_radioOther.Visible = false;
				m_cbOther.Enabled = false;
				m_cbOther.Visible = false;
				var diff = m_btnOK.Location.Y - m_cbOther.Location.Y;
				Size = new Size(Width, Height - diff);
			}
			else
			{
				foreach (var anthroFile in rgsAnthroFiles)
				{
					m_cbOther.Items.Add(anthroFile);
				}
				m_cbOther.SelectedIndex = 0;
			}
		}

		/// <summary>
		/// Set the description string provided by the caller.
		/// </summary>
		public void SetDescription(string sDescription)
		{
			m_sDescription = sDescription;
		}

		/// <summary>
		/// Override to allow setting the description text.
		/// </summary>
		protected override void OnHandleCreated(EventArgs e)
		{
			if (!string.IsNullOrEmpty(m_sDescription))
			{
				m_tbDescription.Text = m_sDescription;
			}
			base.OnHandleCreated(e);
		}

		/// <summary>
		/// Get the choice made by the user.
		/// </summary>
		public int GetChoice()
		{
			if (m_radioFRAME.Checked)
			{
				return kralFRAME;
			}
			if (m_radioOCM.Checked)
			{
				return kralOCM;
			}
			if (m_radioCustom.Checked)
			{
				return kralUserDef;
			}
			Debug.Assert(m_radioOther.Checked);
			return m_cbOther.SelectedIndex;
		}

		private void m_btnOK_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			Close();
		}

		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "UserHelpFile", m_helpTopic);
		}

		private void FrameTextClick(object sender, EventArgs e)
		{
			m_radioFRAME.Checked = true;
		}

		private void OcmTextClick(object sender, EventArgs e)
		{
			m_radioOCM.Checked = true;
		}

		private void CustomTextClick(object sender, EventArgs e)
		{
			m_radioCustom.Checked = true;
		}

		/// <summary>
		/// Pops up a dialog to ask the user how they want to initialize their anthro lists.
		/// Returns a string indicating what file to load, or null if the user selected the custom (make your own) option
		/// </summary>
		public static string PickAnthroList(string description, IHelpTopicProvider helpTopicProvider)
		{
			// Figure out what lists are available (in {FW}/Templates/*.xml).
			var fHaveOCM = false;
			var fHaveFRAME = false;
			var rgsAnthroFiles = new List<string>();
			var rgsXmlFiles = Directory.GetFiles(FwDirectoryFinder.TemplateDirectory, "*.xml", SearchOption.TopDirectoryOnly);
			string sFile;
			foreach (var xmlFile in rgsXmlFiles)
			{
				sFile = Path.GetFileName(xmlFile);
				if (Path.GetFileName(sFile) == "OCM.xml")
				{
					fHaveOCM = true;
				}
				else if (Path.GetFileName(sFile) == "OCM-Frame.xml")
				{
					fHaveFRAME = true;
				}
				else if (sFile != "NewLangProj.xml" && IsAnthroList(xmlFile))
				{
					rgsAnthroFiles.Add(sFile);
				}
			}

			// display a dialog for the user to select a list.
			sFile = null;
			if (fHaveOCM || fHaveFRAME || rgsAnthroFiles.Count > 0)
			{
				using (var dlg = new FwCheckAnthroListDlg())
				{
					dlg.SetValues(fHaveOCM, fHaveFRAME, rgsAnthroFiles, helpTopicProvider);
					if (!string.IsNullOrEmpty(description))
					{
						dlg.SetDescription(description);
					}
					var res = dlg.ShowDialog();
					if (res == DialogResult.OK)
					{
						var nChoice = dlg.GetChoice();
						switch (nChoice)
						{
							case kralUserDef:
								break;
							case kralOCM:
								sFile = Path.Combine(FwDirectoryFinder.TemplateDirectory, "OCM.xml");
								break;
							case kralFRAME:
								sFile = Path.Combine(FwDirectoryFinder.TemplateDirectory, "OCM-Frame.xml");
								break;
							default:
								Debug.Assert(nChoice >= 0 && nChoice < rgsAnthroFiles.Count);
								sFile = Path.Combine(FwDirectoryFinder.TemplateDirectory, rgsAnthroFiles[nChoice]);
								break;
						}
					}
				}
			}
			return sFile;
		}

		private static bool IsAnthroList(string sFilePath)
		{
			if (!File.Exists(sFilePath))
			{
				return false;
			}
			using (TextReader rdr = new StreamReader(sFilePath, Encoding.UTF8))
			{
				try
				{
					for (int i = 0; i < 5; ++i)
					{
						string sLine = rdr.ReadLine();
						if (sLine == null)
							break;
						if (sLine.Contains("<AnthroList>"))
							return true;
					}
				}
				catch
				{
					// must not have been UTF-8...
				}
				return false;
			}
		}
	}
}
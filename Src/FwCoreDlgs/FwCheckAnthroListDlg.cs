// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FwCheckAnthroList.cs
// Responsibility: mcconnel
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using SIL.FieldWorks.FDO;
using System.IO;
using SIL.Utils;
using System.Diagnostics;
using SIL.FieldWorks.FDO.Infrastructure;
using System.Xml;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Application.ApplicationServices;
using SIL.FieldWorks.Common.FwUtils;
using XCore;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This dialog presents the user with a choice of the available anthropology category
	/// lists.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
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
		private String m_helpTopic = "khtpFwCheckAnthroListDlg";

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FwCheckAnthroList"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwCheckAnthroListDlg()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Initialize the dialog.
		/// </summary>
		public void SetValues(bool fHaveOCM, bool fHaveFRAME, List<string> rgsAnthroFiles,
			IHelpTopicProvider helpTopicProvider)
		{
			m_radioOCM.Enabled = fHaveOCM;
			m_radioFRAME.Enabled = fHaveFRAME;
			m_helpTopicProvider = helpTopicProvider;

			if (m_radioFRAME.Enabled)
			{
				m_radioFRAME.Checked = true;
				m_radioOCM.Checked = false;
				m_radioCustom.Checked = false;
			}
			else if (m_radioOCM.Enabled)
			{
				m_radioFRAME.Checked = false;
				m_radioOCM.Checked = true;
				m_radioCustom.Checked = false;
			}
			else
			{
				m_radioFRAME.Checked = false;
				m_radioOCM.Checked = false;
				m_radioCustom.Checked = true;
			}
			m_radioOther.Checked = false;
			if (rgsAnthroFiles.Count == 0)
			{
				m_radioOther.Enabled = false;
				m_radioOther.Visible = false;
				m_cbOther.Enabled = false;
				m_cbOther.Visible = false;
				int diff = m_btnOK.Location.Y - m_cbOther.Location.Y;
				this.Size = new Size(this.Width, this.Height - diff);
			}
			else
			{
				for (int i = 0; i < rgsAnthroFiles.Count; ++i)
					m_cbOther.Items.Add(rgsAnthroFiles[i]);
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
			if (!String.IsNullOrEmpty(m_sDescription))
				m_tbDescription.Text = m_sDescription;
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
			else if (m_radioOCM.Checked)
			{
				return kralOCM;
			}
			else if (m_radioCustom.Checked)
			{
				return kralUserDef;
			}
			else
			{
				Debug.Assert(m_radioOther.Checked);
				return m_cbOther.SelectedIndex;
			}
		}

		private void m_radioFRAME_CheckedChanged(object sender, EventArgs e)
		{
			if (m_radioFRAME.Checked)
			{
				m_radioOCM.Checked = false;
				m_radioCustom.Checked = false;
				m_radioOther.Checked = false;
			}
		}

		private void m_radioOCM_CheckedChanged(object sender, EventArgs e)
		{
			if (m_radioOCM.Checked)
			{
				m_radioFRAME.Checked = false;
				m_radioCustom.Checked = false;
				m_radioOther.Checked = false;
			}
		}

		private void m_radioCustom_CheckedChanged(object sender, EventArgs e)
		{
			if (m_radioCustom.Checked)
			{
				m_radioFRAME.Checked = false;
				m_radioOCM.Checked = false;
				m_radioOther.Checked = false;
			}
		}

		private void m_radioOther_CheckedChanged(object sender, EventArgs e)
		{
			if (m_radioOther.Checked)
			{
				m_radioFRAME.Checked = false;
				m_radioOCM.Checked = false;
				m_radioCustom.Checked = false;
			}
		}

		private void m_btnOK_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.OK;
			Close();
		}

		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "UserHelpFile", m_helpTopic);
		}

	}

	/// <summary>
	/// This replaces C++ code that checks for the existence of the anthropology list,
	/// and initializes it if it's not there.
	/// </summary>
	public class FwCheckAnthroList1
	{
		string m_sDescription = null;

		/// <summary>
		/// Set the description for the dialog, if a dialog is needed.
		/// </summary>
		public string Description
		{
			set { m_sDescription = value; }
		}

		/// <summary>
		/// Check whether the anthropology list exists.  If not, initialize it, popping
		/// up a dialog to ask the user how he wants it done.
		/// Returning true indicates an anthro list was actually loaded during first-time initialization.
		/// </summary>
		public bool CheckAnthroList(ILangProject proj, Form parent, int wsDefault, IHelpTopicProvider helpTopicProvider)
		{
			// Don't bother loading the list into a memory-only project.  These are used for
			// testing, and don't want to be slowed down by disk accesses.
			if (proj.Cache.ProjectId.Type == FDOBackendProviderType.kMemoryOnly)
				return false;

			// 1. Determine whether or not the Anthropology List has been initialized.
			if (proj.AnthroListOA != null && proj.AnthroListOA.Name.StringCount > 0)
			{
				if (proj.AnthroListOA.ItemClsid == 0 || proj.AnthroListOA.Depth == 0)
				{
					proj.Cache.DomainDataByFlid.BeginNonUndoableTask();
					proj.AnthroListOA.ItemClsid = CmAnthroItemTags.kClassId;
					proj.AnthroListOA.Depth = 127;
					proj.Cache.DomainDataByFlid.EndNonUndoableTask();
				}
				return false;
				// The Anthropology List may still be empty, but it's initialized!
			}
			// 2. Figure out what lists are available (in {FW}/Templates/*.xml).

			string sFilePattern = Path.Combine(DirectoryFinder.TemplateDirectory, "*.xml");
			bool fHaveOCM = false;
			bool fHaveFRAME = false;
			List<string> rgsAnthroFiles = new List<string>();
			string[] rgsXmlFiles = Directory.GetFiles(DirectoryFinder.TemplateDirectory, "*.xml", SearchOption.TopDirectoryOnly);
			string sFile;
			for (int i = 0; i < rgsXmlFiles.Length; ++i)
			{
				sFile = Path.GetFileName(rgsXmlFiles[i]);
				if (Path.GetFileName(sFile) == "OCM.xml")
					fHaveOCM = true;
				else if (Path.GetFileName(sFile) == "OCM-Frame.xml")
					fHaveFRAME = true;
				else if (sFile != "NewLangProj.xml" && IsAnthroList(rgsXmlFiles[i]))
					rgsAnthroFiles.Add(sFile);
			}

			// 3. display a dialog for the user to select a list.

			sFile = null;
			if (fHaveOCM || fHaveFRAME || rgsAnthroFiles.Count > 0)
			{
				using (FwCheckAnthroListDlg dlg = new FwCheckAnthroListDlg())
				{
					dlg.SetValues(fHaveOCM, fHaveFRAME, rgsAnthroFiles, helpTopicProvider);
					if (!String.IsNullOrEmpty(m_sDescription))
						dlg.SetDescription(m_sDescription);
					//EnableRelatedWindows(hwnd, false);
					DialogResult res = dlg.ShowDialog(parent);
					//EnableRelatedWindows(hwnd, true);
					if (res == DialogResult.OK)
					{
						int nChoice = dlg.GetChoice();
						switch (nChoice)
						{
							case FwCheckAnthroListDlg.kralUserDef:
								break;
							case FwCheckAnthroListDlg.kralOCM:
								sFile = Path.Combine(DirectoryFinder.TemplateDirectory, "OCM.xml");
								break;
							case FwCheckAnthroListDlg.kralFRAME:
								sFile = Path.Combine(DirectoryFinder.TemplateDirectory, "OCM-Frame.xml");
								break;
							default:
								Debug.Assert(nChoice >= 0 && nChoice < rgsAnthroFiles.Count);
								sFile = Path.Combine(DirectoryFinder.TemplateDirectory, rgsAnthroFiles[nChoice]);
								break;
						}
					}
				}
			}

			// 4. Load the selected list, or initialize properly for a User-defined (empty) list.

			using (new WaitCursor(parent))
			{
				if (String.IsNullOrEmpty(sFile))
				{
					proj.Cache.DomainDataByFlid.BeginNonUndoableTask();
					proj.AnthroListOA.Name.set_String(wsDefault, FwCoreDlgs.ksAnthropologyCategories);
					proj.AnthroListOA.Abbreviation.set_String(wsDefault, FwCoreDlgs.ksAnth);
					proj.AnthroListOA.ItemClsid = CmAnthroItemTags.kClassId;
					proj.AnthroListOA.Depth = 127;
					proj.Cache.DomainDataByFlid.EndNonUndoableTask();
				}
				else
				{
					XmlList xlist = new XmlList();
					xlist.ImportList(proj, "AnthroList", sFile, null);
				}
			}

			// 5. create the corresponding overlays if the list is not empty.

			ICmOverlay over = null;
			foreach (ICmOverlay x in proj.OverlaysOC)
			{
				if (x.PossListRA == proj.AnthroListOA)
				{
					over = x;
					break;
				}
			}
			if (over != null)
			{
				proj.Cache.DomainDataByFlid.BeginNonUndoableTask();
				foreach (ICmPossibility poss in proj.AnthroListOA.PossibilitiesOS)
				{
					over.PossItemsRC.Add(poss);
					AddSubPossibilitiesToOverlay(over, poss);
				}
				proj.Cache.DomainDataByFlid.EndNonUndoableTask();
			}
			return true;
		}

		private void AddSubPossibilitiesToOverlay(ICmOverlay over, ICmPossibility poss)
		{
			foreach (ICmPossibility sub in poss.SubPossibilitiesOS)
			{
				over.PossItemsRC.Add(sub);
				AddSubPossibilitiesToOverlay(over, sub);
			}
		}

		private bool IsAnthroList(string sFilePath)
		{
			if (!File.Exists(sFilePath))
				return false;
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

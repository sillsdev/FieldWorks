// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ExportTbxDialog.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using Microsoft.Win32;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;

using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using XCore;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Dialog for exporting Toolbox USFM
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class ExportTbxDialog : ExportUsfmDialog
	{
		#region Member variables
		private IScripture m_scr;
		/// <summary>Base string value for back translation label.</summary>
		private string m_defaultBtLabel = string.Empty;
		/// <summary>Helps set up the dialog.</summary>
		private TeImportExportFileDialog m_fileDialog = null;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ExportUsfmDialog"/> class.
		/// </summary>
		/// <param name="cache">database cache</param>
		/// <param name="filter">book filter to display which books we will export</param>
		/// <param name="appKey">location of registry</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// <param name="app">The application</param>
		public ExportTbxDialog(FdoCache cache, FilteredScrBooks filter, RegistryKey appKey,
			IHelpTopicProvider helpTopicProvider, IApp app)
			: base(cache, filter, appKey, MarkupType.Toolbox, helpTopicProvider, app)
		{
			InitializeComponent();
			// Save default label for BT control
			m_defaultBtLabel = chkBackTranslation.Text;

			m_fileDialog = new TeImportExportFileDialog(m_cache.ProjectId.Name, FileType.ToolBox);
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise,
		/// false.</param>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
				if (m_fileDialog != null)
				{
					m_fileDialog.Dispose();
					m_fileDialog = null;
				}
			}
			base.Dispose(disposing);
		}

		#region Public Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether export is split by book.
		/// </summary>
		/// <value><c>true</c> if export is split by book; otherwise, <c>false</c>.</value>
		/// ------------------------------------------------------------------------------------
		public bool ExportSplitByBook
		{
			get
			{
				CheckDisposed();
				return rdoOneFilePerbook.Checked;
			}
		}
		#endregion

		#region Overridden properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the help topic key.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override string HelpTopic
		{
			get { return "khtpExportTbxUSFM"; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether back translations are enabled.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if back translations enabled; otherwise, <c>false</c>.
		/// </value>
		/// ------------------------------------------------------------------------------------
		protected override bool BackTranslationsEnabled
		{
			get { return chkBackTranslation.Enabled; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether user has requested export of back translation data.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public override bool ExportBackTranslationDomain
		{
			get
			{
				CheckDisposed();
				return chkBackTranslation.Checked;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether user has requested export of Scripture.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public override bool ExportScriptureDomain
		{
			get
			{
				CheckDisposed();

				return chkScripture.Checked;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if our state requires only one BT writing system. For Toolbox export, if
		/// user is exporting Scripture, we can interleave any number of BTs with it, but if
		/// exporting a BT by itself, it doesn't make sense to interleave other BTs with it, so
		/// only one is permitted.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override bool CurrentStateRequiresOnlyOneBtWs
		{
			get
			{
				return (!chkScripture.Checked);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When the user specified exporting to a single file, this returns that file name
		/// (including the full path). When the user specified exporting one file per book,
		/// this returns the base implementation (i.e., the folder path where those files are
		/// written.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string OutputSpec
		{
			get
			{
				CheckDisposed();
				return rdoOneFilePerbook.Checked ? base.OutputSpec : txtOutputFile.Text;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the output directory.
		/// </summary>
		/// <remarks>This is the folder that should get created just before closing this dialog
		/// and starting the export process. In the case of a single file export, this is the
		/// folder that contains the file.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		protected override string OutputDirectory
		{
			get
			{
				return (rdoSingleFile.Checked) ? Path.GetDirectoryName(txtOutputFile.Text) :
					base.OutputDirectory;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the control which should receive focus if there is a problem creating the
		/// output directory.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override TextBox TxtOutputSpec
		{
			get { return (rdoSingleFile.Checked) ? txtOutputFile : base.TxtOutputSpec; }
		}
		#endregion

		#region Overridden methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Saves the registry settings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void SaveRegistrySettings()
		{
			base.SaveRegistrySettings();

			// Set values for what to export for interleaved export (Toolbox).
			// Review: Can we remove Interleaved from the names below?
			m_regGroup.SetBoolValue("ToolboxInterleavedIncludeScripture", chkScripture.Checked);
			m_regGroup.SetBoolValue("ToolboxInterleavedIncludeBackTrans", chkBackTranslation.Checked);
			m_regGroup.SetBoolValue("ToolboxOneFilePerBook", rdoOneFilePerbook.Checked);
			m_regGroup.SetStringValue("ToolboxOutputSpec", (rdoOneFilePerbook.Checked ?
				txtOutputFolder.Text : txtOutputFile.Text));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Final preparation before starting the export. Overridden to set
		/// ConvertCVDigitsOnExport value and to create folder in the case where
		/// export is to a single file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void PrepareToExport()
		{
			if (m_scr.UseScriptDigits)
				m_scr.ConvertCVDigitsOnExport = chkArabicNumbers.Checked;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure that the output folder or file name is valid
		/// </summary>
		/// <returns>true if the output is valid, else false</returns>
		/// ------------------------------------------------------------------------------------
		protected override bool OkayToProceedWithExport
		{
			get
			{
				if (!base.OkayToProceedWithExport)
					return false;

				if (rdoSingleFile.Checked)
				{
					// for a single file, make sure the output file is valid.
					string sFile = Path.GetFileName(txtOutputFile.Text);
					if (sFile == string.Empty)
					{
						string msg = string.Format(TeResourceHelper.GetResourceString(
						"kstidExportUSFMMissingFileName"));
						MessageBox.Show(msg, m_app.ApplicationName, MessageBoxButtons.OK,
							MessageBoxIcon.Warning);
						txtOutputFile.Focus();
						return false;
					}
				}
				return true;
			}
		}
		#endregion

		#region Event Handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the File browse button to locate a file to write to.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void btnFileBrowse_Click(object sender, EventArgs e)
		{
			if (m_fileDialog.ShowSaveDialog(txtOutputFile.Text, this) == DialogResult.OK)
				txtOutputFile.Text = m_fileDialog.FileName;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Load event of the ExportTbxDialog control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void ExportTbxDialog_Load(object sender, EventArgs e)
		{
			m_scr = m_cache.LangProject.TranslatedScriptureOA;

			// Set values for what to export for interleaved export (Toolbox).
			chkScripture.Checked = m_regGroup.GetBoolValue("ToolboxInterleavedIncludeScripture", true);

			// If the current settings are for Arabic digits then don't need to allow the user
			// the option to export them as Arabic.
			m_scr = m_cache.LangProject.TranslatedScriptureOA;
			if (!m_scr.UseScriptDigits)
				chkArabicNumbers.Enabled = false;
			else
				chkArabicNumbers.Checked = m_scr.ConvertCVDigitsOnExport;

			// Set the "output to" (folder or file) radio buttons.
			bool splitByBook = m_regGroup.GetBoolValue("ToolboxOneFilePerBook", true);
			rdoOneFilePerbook.Checked = splitByBook;
			rdoSingleFile.Checked = !splitByBook;

			if (splitByBook)
			{
				txtOutputFolder.Text = m_regGroup.GetStringValue("ToolboxOutputSpec",
					m_fileDialog.DefaultFolder);
				txtOutputFile.Text = Path.Combine(txtOutputFolder.Text, m_fileDialog.DefaultFileName);
			}
			else
			{
				txtOutputFile.Text = m_regGroup.GetStringValue("ToolboxOutputSpec",
					m_fileDialog.DefaultFilePath);
				txtOutputFolder.Text = Path.GetDirectoryName(txtOutputFile.Text);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a checked changed for the radio buttons.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void OutputToCheckedChanged(object sender, System.EventArgs e)
		{
			lblOutputFolder.Enabled = rdoOneFilePerbook.Checked;
			txtOutputFolder.Enabled = rdoOneFilePerbook.Checked;
			btnFolderBrowse.Enabled = rdoOneFilePerbook.Checked;
			fileNameSchemeCtrl.Enabled = rdoOneFilePerbook.Checked;
			lblOutputFile.Enabled = !rdoOneFilePerbook.Checked;
			txtOutputFile.Enabled = !rdoOneFilePerbook.Checked;
			btnFileBrowse.Enabled = !rdoOneFilePerbook.Checked;

			txtOutputFolder_TextChanged(sender, e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the TextChanged event of the txtOutputFile control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void txtOutputFile_TextChanged(object sender, EventArgs e)
		{
			if (toolTipInvalidChar.Active)
				toolTipInvalidChar.Active = false;

			int i = txtOutputFile.Text.IndexOfAny(Path.GetInvalidPathChars());
			if (i >= 0)
				HandleInvalidPathChar(txtOutputFile, i);
			else
				base.txtOutputFolder_TextChanged(sender, e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If the Scripture checkbox is changed, update the interleaved labels and update
		/// the OK button (enabling it only if at least one domain selected for export).
		/// Also update the BT writing systems that are checked.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void chkScripture_CheckedChanged(object sender, EventArgs e)
		{
			UpdateInterleavedLabels();
			WhatToExportChanged(sender, e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If the BackTrans checkbox is changed, update the interleaved labels and update
		/// the OK button (enabling it only if at least one domain selected for export).
		/// Make writing system check list box visible when exporting back translations.
		/// Also update the BT writing systems that are checked.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void chkBackTranslation_CheckedChanged(object sender, System.EventArgs e)
		{
			chklbWritingSystems.Enabled = chkBackTranslation.Checked &&
				chklbWritingSystems.Items.Count > 1;
			chkScripture_CheckedChanged(sender, e);
		}
		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Update the interleaved labels to reflect the current state of the check boxes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void UpdateInterleavedLabels()
		{
			// If Scripture is checked, then mark back translation as interleaved
			chkBackTranslation.Text = m_defaultBtLabel;
			if (ExportScriptureDomain)
				chkBackTranslation.Text += " " + TeResourceHelper.GetResourceString("kstidExportUSFMInterleavedLabel");
		}
		#endregion
	}
}

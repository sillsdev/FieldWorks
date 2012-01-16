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
// File: ExportPtxDialog.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using XCore;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Dialog for exporting Partext 6 USFM
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class ExportPtxDialog : ExportUsfmDialog
	{
		#region Data members
		/// <summary>Output folder for the vernacular project</summary>
		protected string m_OutputFolder;
		/// <summary>Output folder for the back translation project</summary>
		protected string m_BTOutputFolder;
		/// <summary></summary>
		protected bool m_overwriteProject = false;
		/// <summary></summary>
		protected string m_paratextProjFolder;
		/// <summary></summary>
		protected List<string> m_nonEditableP6Projects = new List<string>();
		/// <summary><c>true</c> if user modified suffix in vernacular</summary>
		protected bool m_fModifiedSuffix;
		/// <summary><c>true</c> if user modified suffix in back translation</summary>
		protected bool m_fBTModifiedSuffix;
		/// <summary>This only gets set to false if the user chooses an existing Paratext
		/// project which does NOT store the project data files in a subfolder called the
		/// same thing as the short name.</summary>
		protected bool m_fAppendShortNameToFolder = true;
		/// <summary>The Paratext project short name for Scripture domain</summary>
		protected string m_shortName;
		/// <summary>The Paratext project short name for Scripture domain</summary>
		protected string m_BTshortName;
		/// <summary>file naming scheme for vernacular projects</summary>
		protected FileNameFormat m_fileNameScheme = new FileNameFormat();
		/// <summary>file naming scheme for back translation projects</summary>
		protected FileNameFormat m_BTfileNameScheme = new FileNameFormat();

		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:ExportUsfmDialog"/> class.
		/// </summary>
		/// <param name="cache">database cache</param>
		/// <param name="filter">book filter to display which books we will export</param>
		/// <param name="appKey">location of registry</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// <param name="app">The application</param>
		/// ------------------------------------------------------------------------------------
		public ExportPtxDialog(FdoCache cache, FilteredScrBooks filter, RegistryKey appKey,
			IHelpTopicProvider helpTopicProvider, IApp app)
			: base(cache, filter, appKey, MarkupType.Paratext, helpTopicProvider, app)
		{
			InitializeComponent();
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
			get { return "khtpExportPtxUSFM"; }
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
			get	{ return rdoBackTranslation.Enabled; }
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
				return rdoBackTranslation.Checked;
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
				return rdoScripture.Checked;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines which export domain radio button is selected. Used only for Paratext
		/// export.
		/// </summary>
		/// <returns>value representing which domain to export: 0 = Scripture,
		/// 1 = Back Translation</returns>
		/// ------------------------------------------------------------------------------------
		protected int WhichDomainToExport
		{
			get { return rdoBackTranslation.Checked ? 1 : 0; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the default suffix for Paratext 6 based on the export domain.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override string DefaultSuffix
		{
			get { return ExportScriptureDomain ? m_shortName : m_BTshortName; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Strips the short name, if present, from the end of the given output folder name,
		/// unless the currently selected project is one that uses non-conventional naming.
		/// </summary>
		/// <param name="value">The value (must not be null).</param>
		/// <returns>The folder path with the ShortName sub-folder removed</returns>
		/// ------------------------------------------------------------------------------------
		private string StripShortNameFromOutputFolder(string value)
		{
			Debug.Assert(value != null);
			if (m_fAppendShortNameToFolder &&
				value.ToLower().EndsWith(Path.DirectorySeparatorChar + ShortName.ToLower()))
			{
				return value.Substring(0, value.Length - ShortName.Length - 1);
			}
			return value;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the current parent output folder based on the export domain.
		/// </summary>
		/// <remarks>Use this property rather than the value of txtOutputFolder.Text to refer
		/// to the base folder. This derived class adds an additional project-specific subfolder
		/// to the folder name displayed in the text box control.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		protected override string BaseOutputFolder
		{
			get { return ExportScriptureDomain ? m_OutputFolder : m_BTOutputFolder; }
			set
			{
				if (ExportScriptureDomain)
					m_OutputFolder = StripShortNameFromOutputFolder(value);
				else
					m_BTOutputFolder = StripShortNameFromOutputFolder(value);

				CalculateDisplayOutputFolder();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a string which can be added as a prefix to a registry setting to indicate the
		/// type of export it pertains to.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override string RegistrySettingType
		{
			get
			{
				return base.RegistrySettingType +
					(rdoBackTranslation.Checked ? "BT" : string.Empty);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether it's okay to proceed with export.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override bool OkayToProceedWithExport
		{
			get
			{
				if (!base.OkayToProceedWithExport)
					return false;

				if (cboShortName.FindStringExact(cboShortName.Text) >= 0)
				{
					if (MessageBox.Show(this, string.Format(
						TeResourceHelper.GetResourceString("kstidExportPT6Overwrite"), ShortName),
						m_app.ApplicationName, MessageBoxButtons.YesNo) == DialogResult.No)
					{
						return false;
					}
					m_overwriteProject = true;
				}
				else if (m_nonEditableP6Projects.Contains(ShortName))
				{
					MessageBox.Show(this, string.Format(
						TeResourceHelper.GetResourceString("kstidExportPT6ProjReadOnly"), ShortName),
						m_app.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
					return false;
				}

				// If Paratext directory does not exist.
				if (!Directory.Exists(m_paratextProjFolder))
				{
					// Attempt to create directory.
					try
					{
						Directory.CreateDirectory(m_paratextProjFolder);
					}
					catch (Exception)
					{
						// We will not be able to create Paratext settings files, so clear out the
						// folder name.
						m_paratextProjFolder = null;
					}
				}
				return true;
			}
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

			m_regGroup.SetIntValue("ParatextOneDomainExportWhat", WhichDomainToExport);
			m_regGroup.SetStringValue(RegistrySettingType + "ShortName", ShortName);
			m_regGroup.SetStringValue(RegistrySettingType + "OutputSpec", BaseOutputFolder);
		}
		#endregion

		#region Event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Load event of the ExportPtxDialog control.
		/// </summary>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnLoad(EventArgs e)
		{

			// set up default short name
			m_shortName = m_regGroup.GetStringValue(base.RegistrySettingType + "ShortName", null);
			m_BTshortName = m_regGroup.GetStringValue(base.RegistrySettingType + "BT" + "ShortName", null);
			if (string.IsNullOrEmpty(m_shortName) || string.IsNullOrEmpty(m_BTshortName))
			{
				foreach (IScrImportSet importSet in m_cache.LangProject.TranslatedScriptureOA.ImportSettingsOC)
				{
					if (!string.IsNullOrEmpty(importSet.ParatextScrProj))
					{
						if (string.IsNullOrEmpty(m_shortName))
							m_shortName = importSet.ParatextScrProj;
						if (string.IsNullOrEmpty(m_BTshortName))
							m_BTshortName = importSet.ParatextBTProj;
						if (importSet.Name.UserDefaultWritingSystem.Text == ScriptureTags.kDefaultImportSettingsName)
							break;
					}
				}
			}
			if (m_shortName == null)
			{
				IWritingSystem wsVern = m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;
				string abbr = wsVern.Abbreviation;
				if (abbr != null && !abbr.Contains("***"))
					m_shortName = abbr.Trim();
				else
					m_shortName = wsVern.Abbreviation.Trim();
			}
			else
				m_shortName = m_shortName.Trim();

			// Paratext 6 requires short name to be between 3 and 5 characters in length.
			if (m_shortName.Length < 3)
				m_shortName = m_shortName.PadRight(3, '_');
			if (m_shortName.Length > 5)
				m_shortName = m_shortName.Substring(0, 5);

			if (string.IsNullOrEmpty(m_BTshortName))
				m_BTshortName = m_shortName.Substring(0, 3) + "BT";

			// Set values for what to export for one domain export (Paratext).
			switch (m_regGroup.GetIntValue("ParatextOneDomainExportWhat", 0))
			{
				case 0:
				default:
					// Temporarily load the BT settings, so when we "switch" to Scripture,
					// the BT settings will be saved in our internal object.
					LoadFileNameSchemeControl(base.RegistrySettingType + "BT", m_BTshortName);
					m_BTfileNameScheme = fileNameSchemeCtrl.FileNameFormat;
					rdoScripture.Select();

					base.OnLoad(e);
					m_fileNameScheme = fileNameSchemeCtrl.FileNameFormat;
					break;
				case 1:
					// Temporarily load the regular (vern) settings, so when we "switch" to
					// back translation, the vern settings will be saved in our internal object.
					LoadFileNameSchemeControl(base.RegistrySettingType, m_shortName);
					m_fileNameScheme = fileNameSchemeCtrl.FileNameFormat;
					rdoBackTranslation.Select();

					base.OnLoad(e);
					m_BTfileNameScheme = fileNameSchemeCtrl.FileNameFormat;
					break;
			}

			// Get the output file or folder specification.
			SCRIPTUREOBJECTSLib.ISCScriptureText3 paraTextSO = null;
			try
			{
				paraTextSO = new SCRIPTUREOBJECTSLib.SCScriptureTextClass();
			}
			catch
			{
				// Ignore: Paratext not installed
			}
			if (paraTextSO != null)
			{
				m_paratextProjFolder = paraTextSO.SettingsDirectory;
				if (m_paratextProjFolder != null)
				{
					m_paratextProjFolder = m_paratextProjFolder.Trim(Path.DirectorySeparatorChar,
						Path.AltDirectorySeparatorChar);
				}
				string[] shortNames = ParatextHelper.GetParatextShortNames(m_cache.ThreadHelper, paraTextSO);
				if (shortNames != null)
				{
					foreach (string shortName in shortNames)
					{
						bool fIsEditable = true;
						try
						{
							paraTextSO.Load(shortName);
							fIsEditable = (paraTextSO.Editable != 0);
						}
						catch
						{
							// Paratext settings file is probably bogus, so we regard it as editable (i.e., we can overwrite it).
						}
						if (fIsEditable)
							cboShortName.Items.Add(shortName);
						else
							m_nonEditableP6Projects.Add(shortName);
					}
				}

				// The following is an attempt to keep us from looking like idiots by making
				// the default project name a non-editable project.
				int i = 1;
				while (m_nonEditableP6Projects.Contains(m_shortName) && i < 1000)
					m_shortName = "MP" + i++;
				i = 1;
				while (m_nonEditableP6Projects.Contains(m_BTshortName) && i < 1000)
					m_BTshortName = "BT" + i++;
			}
			else
			{
				// Paratext is not installed or Paratext directory does not exist.
				// We default the output path to "C:\My Paratext Projects". However, this directory
				// might not exist and/or we might have no permissions to write there. We attempt to
				// create this folder now. If we fail, then we disable the Short Name control and
				// won't bother writing the Paratext settings files.
				// REVIEW: this comment seems to be out of date. We might crash if we have an
				// invalid directory (see FWNX-828).
				m_paratextProjFolder = DirectoryFinder.MyParatextProjectsDirectory;
			}

			cboShortName.Text = ShortName;
			m_OutputFolder = m_regGroup.GetStringValue("ParatextOutputSpec", m_paratextProjFolder);
			m_BTOutputFolder = m_regGroup.GetStringValue("ParatextBTOutputSpec", m_paratextProjFolder);

			UpdateDisplay();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the resource string id for the Paratext export dialog folder browser prompt.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override string StidExportDlgFolderBrowserPrompt
		{
			get { return "kstidExportPtxDlgFolderBrowserPrompt"; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If the Scripture/BackTrans radio button is changed, update the OK button (enabling
		/// it only if at least one domain selected for export). Also update the BT writing
		/// systems control enabled state and which writing systems are checked.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected void rdoScripture_CheckedChanged(object sender, EventArgs e)
		{
			if (rdoScripture.Checked)
			{
				// Save current settings for back translation domain
				m_BTfileNameScheme = fileNameSchemeCtrl.FileNameFormat;
				m_fBTModifiedSuffix = fileNameSchemeCtrl.UserModifiedSuffix;
				fileNameSchemeCtrl.FileNameFormat = m_fileNameScheme;
				fileNameSchemeCtrl.UserModifiedSuffix = m_fModifiedSuffix;
				cboShortName.Text = m_shortName;
			}
			else
			{
				// Save current settings for Scripture domain
				m_fileNameScheme = fileNameSchemeCtrl.FileNameFormat;
				m_fModifiedSuffix = fileNameSchemeCtrl.UserModifiedSuffix;
				fileNameSchemeCtrl.FileNameFormat = m_BTfileNameScheme;
				fileNameSchemeCtrl.UserModifiedSuffix = m_fBTModifiedSuffix;
				cboShortName.Text = m_BTshortName;
			}

			chklbWritingSystems.Enabled = rdoBackTranslation.Checked &&
				chklbWritingSystems.Items.Count > 1;
			WhatToExportChanged(sender, e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the TextChanged event of the cboShortName control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		protected void cboShortName_TextChanged(object sender, EventArgs e)
		{
			if (!ValidShortName(cboShortName.Text))
			{
				cboShortName.TextChanged -= cboShortName_TextChanged;
				cboShortName.Text = ShortName;
				cboShortName.SelectAll();
				cboShortName.TextChanged += cboShortName_TextChanged;
				System.Media.SystemSounds.Beep.Play();
				return;
			}

			// Even though Resharper thinks this check is redundant, if we unconditionally change the text,
			// the user's text selection in the edit portion of the combo control will be lost unnecessarily.
			if (cboShortName.Text != cboShortName.Text.Trim())
				cboShortName.Text = cboShortName.Text.Trim();
			int nSelectionStart = cboShortName.SelectionStart;
			if (cboShortName.Text.Length > 3 && cboShortName.Text != cboShortName.Text.TrimEnd('_'))
			{
				cboShortName.Text = cboShortName.Text.TrimEnd('_');
				// restore selection
				if (nSelectionStart > cboShortName.Text.Length)
					nSelectionStart = cboShortName.Text.Length;
				cboShortName.SelectionStart = nSelectionStart;
			}

			if (!m_fAppendShortNameToFolder &&
				!m_fUserModifiedFolder &&
				!cboShortName.Items.Contains(cboShortName.Text))
			{
				// User had selected a weird paratext project that wasn't using conventional
				// naming approach. Now that they're changing the short name to some non-existent
				// project, let's set them back to using the normal naming conventions.
				m_fAppendShortNameToFolder = true;
				BaseOutputFolder = m_paratextProjFolder;
				m_fUserModifiedFolder = false;
			}

			if (ExportScriptureDomain)
				m_shortName = cboShortName.Text;
			else
				m_BTshortName = cboShortName.Text;

			UpdateDisplay();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Valididates the short name, making sure it doesn't have any illegal path characters.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool ValidShortName(string shortName)
		{
			return (shortName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the displayed output folder and the file suffix.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void UpdateDisplay()
		{
			// Update the name of the output folder.
			CalculateDisplayOutputFolder();

			// Determine if the project exists already and select it.
			int shortNameIndex = cboShortName.FindStringExact(cboShortName.Text);
			if (shortNameIndex >= 0)
			{
				int start = cboShortName.SelectionStart;
				int length = cboShortName.SelectionLength;
				cboShortName.SelectedIndex = shortNameIndex;
				cboShortName.SelectionStart = start;
				cboShortName.SelectionLength = length;
				fileNameSchemeCtrl.Enabled = false;
			}
			else
				fileNameSchemeCtrl.Enabled = true;

			// If the user modified the suffix the Suffix in the control doesn't change!
			fileNameSchemeCtrl.Suffix = cboShortName.Text;
			if (ExportScriptureDomain)
				m_fileNameScheme.m_fileSuffix = fileNameSchemeCtrl.Suffix;
			else
				m_BTfileNameScheme.m_fileSuffix = fileNameSchemeCtrl.Suffix;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Leave event of the cboShortName control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		protected void cboShortName_Leave(object sender, EventArgs e)
		{
			// Paratext 6 requires short name to be between 3 and 5 characters in length.
			if (cboShortName.Text.Length < 3)
				cboShortName.Text = cboShortName.Text.PadRight(3, '_');
			UpdateDisplay();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the SelectedIndexChanged event of the cboShortName control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected void cboShortName_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			if (!m_fUserModifiedFolder && cboShortName.SelectedIndex >= 0)
			{
				try
				{
					SCRIPTUREOBJECTSLib.ISCScriptureText3 paraTextSO = new SCRIPTUREOBJECTSLib.SCScriptureTextClass();
					paraTextSO.Load(ShortName);
					string folder = paraTextSO.Directory;
					m_fAppendShortNameToFolder = (folder.EndsWith(Path.DirectorySeparatorChar + ShortName));
					BaseOutputFolder = folder;
					m_fUserModifiedFolder = false;
					CalculateDisplayOutputFolder();

					FileNameFormat currentFileNameScheme = ExportScriptureDomain ? m_fileNameScheme :
						m_BTfileNameScheme;
					currentFileNameScheme.m_filePrefix = paraTextSO.FileNamePrePart;
					currentFileNameScheme.m_schemeFormat =
						FileNameFormat.GetSchemeFormatFromParatextForm(paraTextSO.FileNameForm);
					m_fileNameScheme.m_fileSuffix = Path.GetFileNameWithoutExtension(paraTextSO.FileNamePostPart);
					//if (ExportScriptureDomain)
					//    m_BTfileNameScheme.m_fileSuffix = m_fileNameScheme.m_fileSuffix.Substring(0, 3) + "BT";
					currentFileNameScheme.m_fileExtension = Path.GetExtension(paraTextSO.FileNamePostPart);

					// set file name scheme control with properties that check the export domain.
					fileNameSchemeCtrl.ClearUserModifiedNameScheme();
					fileNameSchemeCtrl.Prefix = FileNamePrefix;
					fileNameSchemeCtrl.Scheme = FileNameScheme;
					fileNameSchemeCtrl.Suffix = FileNameSuffix;
					fileNameSchemeCtrl.Extension = FileNameExtension;
				}
				catch
				{
					// Ignore Paratext Load errors. Project will be overwritten.
				}
			}
		}
		#endregion

		#region Private properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the file name prefix.
		/// </summary>
		/// <value>The file name prefix.</value>
		/// ------------------------------------------------------------------------------------
		private string FileNamePrefix
		{
			get
			{
				return ExportScriptureDomain ? m_fileNameScheme.m_filePrefix :
					m_BTfileNameScheme.m_filePrefix;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the file name scheme.
		/// </summary>
		/// <value>The file name scheme.</value>
		/// ------------------------------------------------------------------------------------
		private string FileNameScheme
		{
			get
			{
				return FileNameFormat.GetUiSchemeFormat(MarkupType.Paratext,
					ExportScriptureDomain ? m_fileNameScheme.m_schemeFormat :
					m_BTfileNameScheme.m_schemeFormat);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the file name suffix.
		/// </summary>
		/// <value>The file name suffix.</value>
		/// ------------------------------------------------------------------------------------
		private string FileNameSuffix
		{
			get
			{
				return ExportScriptureDomain ? m_fileNameScheme.m_fileSuffix :
					m_BTfileNameScheme.m_fileSuffix;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the file name extension.
		/// </summary>
		/// <value>The file name extension.</value>
		/// ------------------------------------------------------------------------------------
		private string FileNameExtension
		{
			get
			{
				return ExportScriptureDomain ? m_fileNameScheme.m_fileExtension :
					m_BTfileNameScheme.m_fileExtension;
			}
		}
		#endregion

		#region Public properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether user has already conconfirmed that an existing
		/// project should be overwritten.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool OverwriteConfirmed
		{
			get
			{
				CheckDisposed();
				return m_overwriteProject;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the short name of the Paratext project based on the export domain.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ShortName
		{
			get
			{
				CheckDisposed();
				return ExportScriptureDomain ? m_shortName : m_BTshortName;
			}
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the Paratext Project Folder (could be either the real one, if Paratext is
		/// installed, or our best guess as to what it might be some day if the user should
		/// decide to install it).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ParatextProjectFolder
		{
			get { return m_paratextProjFolder; }
		}
		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calculates and sets the value of the output folder actually displayed in the
		/// control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CalculateDisplayOutputFolder()
		{
			string outputFolder = BaseOutputFolder;
			if (outputFolder != null && outputFolder.Length > 0)
			{
				if (m_fAppendShortNameToFolder)
					txtOutputFolder.Text = Path.Combine(outputFolder, ShortName);
				else
					txtOutputFolder.Text = outputFolder;
			}
			else
				txtOutputFolder.Text = string.Empty;
		}

		#endregion
	}
}

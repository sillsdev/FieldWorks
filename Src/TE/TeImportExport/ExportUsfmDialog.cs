// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2004' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ExportUsfmDialog.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.IO;
using System.Text;
using Microsoft.Win32;
using System.Diagnostics;

using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.FDO.Cellar;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for ExportUsfmDialog.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ExportUsfmDialog : Form, IFWDisposable
	{
		#region Member variables
		/// <summary>database cache</summary>
		protected FdoCache m_cache;
		private bool m_fChangingCheckState = false;
		private MarkupType m_markupType;
		/// <summary>Registry group for loading/saving settings</summary>
		protected RegistryGroup m_regGroup;
		private IContainer components;
		/// <summary></summary>
		protected Button btnFolderBrowse;
		private System.Windows.Forms.Label lblBooks;
		/// <summary>Checkbox list of back translation writing systems</summary>
		protected System.Windows.Forms.CheckedListBox chklbWritingSystems;
		/// <summary></summary>
		protected Button btnOk;
		/// <summary></summary>
		protected Label lblOutputFolder;
		/// <summary></summary>
		protected TextBox txtOutputFolder;
		/// <summary></summary>
		protected CheckBox chkNotes;
		/// <summary></summary>
		protected FileNameSchemeCtrl fileNameSchemeCtrl;
		/// <summary></summary>
		protected Panel pnlExportWhat;
		/// <summary></summary>
		protected Button btnMappings;
		/// <summary></summary>
		protected Button btnCancel;
		/// <summary></summary>
		protected Button m_btnHelp;
		/// <summary></summary>
		protected GroupBox grpOutputTo;
		/// <summary></summary>
		protected ToolTip toolTipInvalidChar;
		/// <summary></summary>
		protected GroupBox grpExportWhat;
		/// <summary>Flag indicating whether user modified the output folder (set in this base class,
		/// but used only in some subclasses)</summary>
		protected bool m_fUserModifiedFolder = false;
		#endregion

		#region Constructors/Destructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ExportUsfmDialog"/> class. This default
		/// constructor is required for Designer to allow derived forms.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ExportUsfmDialog()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ExportUsfmDialog"/> class.
		/// </summary>
		/// <param name="cache">database cache</param>
		/// <param name="filter">book filter to display which books we will export</param>
		/// <param name="appKey">location of registry</param>
		/// <param name="markup">type of markup format for export:
		/// Paratext (one domain, non-interleaved) OR Toolbox (optionally interleaved)</param>
		/// ------------------------------------------------------------------------------------
		public ExportUsfmDialog(FdoCache cache, FilteredScrBooks filter, RegistryKey appKey,
			MarkupType markup) : this()
		{
			m_cache = cache;
			m_markupType = markup; // let dialog know if this is for Paratext or Toolbox
			if (appKey != null) // might be null in tests - in this case derived class has to provide a m_regGroup
				m_regGroup = new RegistryGroup(appKey, "ExportUsfmSettings");

			// Display books and markup labels
			string filtered = (filter.AllBooks ? TeResourceHelper.GetResourceString("kstidExportDlgUnfiltered") :
				TeResourceHelper.GetResourceString("kstidExportDlgFiltered"));
			string booksToExport = GetExportedBooksStr(filter);
			if (filter.BookCount == 1)
			{
				lblBooks.Text = string.Format(TeResourceHelper.GetResourceString("kstidBooksToExportSingularForm"),
					filtered, booksToExport);
			}
			else
			{
				lblBooks.Text = string.Format(lblBooks.Text, filter.BookCount, filtered, booksToExport);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a string of book names in the filter.
		/// </summary>
		/// <param name="filter">book filter</param>
		/// <returns>string with comma-delimited list of books in the filter</returns>
		/// ------------------------------------------------------------------------------------
		private string GetExportedBooksStr(FilteredScrBooks filter)
		{
			StringBuilder filteredBooks = new StringBuilder();
			if (filter.BookCount > 3)
				return string.Empty;

			// Append all scripture books in filter to string
			for (int bookIndex = 0; bookIndex < filter.BookCount; bookIndex++)
			{
				if (bookIndex > 0)
					filteredBooks.Append(", ");
				filteredBooks.Append(((ScrBookRef)filter.GetBook(bookIndex).BookIdRA).UIBookName);
			}

			return string.Format(TeResourceHelper.GetResourceString("kstidExportDlgMultipleBooks"),
				filteredBooks.ToString());
		}

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged
		/// resources; <c>false</c> to release only unmanaged resources.
		/// </param>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose( bool disposing )
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}
		#endregion

		#region Windows Form Designer generated code
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ExportUsfmDialog));
			this.btnMappings = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.m_btnHelp = new System.Windows.Forms.Button();
			this.grpOutputTo = new System.Windows.Forms.GroupBox();
			this.txtOutputFolder = new System.Windows.Forms.TextBox();
			this.fileNameSchemeCtrl = new SIL.FieldWorks.TE.FileNameSchemeCtrl();
			this.lblOutputFolder = new System.Windows.Forms.Label();
			this.btnFolderBrowse = new System.Windows.Forms.Button();
			this.btnOk = new System.Windows.Forms.Button();
			this.chkNotes = new System.Windows.Forms.CheckBox();
			this.chklbWritingSystems = new System.Windows.Forms.CheckedListBox();
			this.lblBooks = new System.Windows.Forms.Label();
			this.grpExportWhat = new System.Windows.Forms.GroupBox();
			this.pnlExportWhat = new System.Windows.Forms.Panel();
			this.toolTipInvalidChar = new System.Windows.Forms.ToolTip(this.components);
			this.grpOutputTo.SuspendLayout();
			this.grpExportWhat.SuspendLayout();
			this.SuspendLayout();
			//
			// btnMappings
			//
			resources.ApplyResources(this.btnMappings, "btnMappings");
			this.btnMappings.Name = "btnMappings";
			//
			// btnCancel
			//
			resources.ApplyResources(this.btnCancel, "btnCancel");
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Name = "btnCancel";
			//
			// m_btnHelp
			//
			resources.ApplyResources(this.m_btnHelp, "m_btnHelp");
			this.m_btnHelp.Name = "m_btnHelp";
			this.m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// grpOutputTo
			//
			resources.ApplyResources(this.grpOutputTo, "grpOutputTo");
			this.grpOutputTo.Controls.Add(this.txtOutputFolder);
			this.grpOutputTo.Controls.Add(this.fileNameSchemeCtrl);
			this.grpOutputTo.Controls.Add(this.lblOutputFolder);
			this.grpOutputTo.Controls.Add(this.btnFolderBrowse);
			this.grpOutputTo.Name = "grpOutputTo";
			this.grpOutputTo.TabStop = false;
			//
			// txtOutputFolder
			//
			resources.ApplyResources(this.txtOutputFolder, "txtOutputFolder");
			this.txtOutputFolder.Name = "txtOutputFolder";
			this.txtOutputFolder.TextChanged += new System.EventHandler(this.txtOutputFolder_TextChanged);
			//
			// fileNameSchemeCtrl
			//
			this.fileNameSchemeCtrl.Extension = "";
			resources.ApplyResources(this.fileNameSchemeCtrl, "fileNameSchemeCtrl");
			this.fileNameSchemeCtrl.Name = "fileNameSchemeCtrl";
			this.fileNameSchemeCtrl.Prefix = "";
			this.fileNameSchemeCtrl.Scheme = "";
			this.fileNameSchemeCtrl.Suffix = "";
			this.fileNameSchemeCtrl.UserModifiedSuffix = false;
			//
			// lblOutputFolder
			//
			resources.ApplyResources(this.lblOutputFolder, "lblOutputFolder");
			this.lblOutputFolder.Name = "lblOutputFolder";
			//
			// btnFolderBrowse
			//
			resources.ApplyResources(this.btnFolderBrowse, "btnFolderBrowse");
			this.btnFolderBrowse.Name = "btnFolderBrowse";
			this.btnFolderBrowse.Click += new System.EventHandler(this.btnFolderBrowse_Click);
			//
			// btnOk
			//
			resources.ApplyResources(this.btnOk, "btnOk");
			this.btnOk.Name = "btnOk";
			this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
			//
			// chkNotes
			//
			resources.ApplyResources(this.chkNotes, "chkNotes");
			this.chkNotes.Name = "chkNotes";
			this.chkNotes.UseVisualStyleBackColor = true;
			this.chkNotes.CheckedChanged += new System.EventHandler(this.txtOutputFolder_TextChanged);
			//
			// chklbWritingSystems
			//
			resources.ApplyResources(this.chklbWritingSystems, "chklbWritingSystems");
			this.chklbWritingSystems.Name = "chklbWritingSystems";
			this.chklbWritingSystems.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.chklbWritingSystems_ItemCheck);
			//
			// lblBooks
			//
			resources.ApplyResources(this.lblBooks, "lblBooks");
			this.lblBooks.Name = "lblBooks";
			//
			// grpExportWhat
			//
			resources.ApplyResources(this.grpExportWhat, "grpExportWhat");
			this.grpExportWhat.Controls.Add(this.pnlExportWhat);
			this.grpExportWhat.Controls.Add(this.chkNotes);
			this.grpExportWhat.Controls.Add(this.chklbWritingSystems);
			this.grpExportWhat.Controls.Add(this.btnMappings);
			this.grpExportWhat.Name = "grpExportWhat";
			this.grpExportWhat.TabStop = false;
			//
			// pnlExportWhat
			//
			resources.ApplyResources(this.pnlExportWhat, "pnlExportWhat");
			this.pnlExportWhat.Name = "pnlExportWhat";
			//
			// toolTipInvalidChar
			//
			this.toolTipInvalidChar.Active = false;
			this.toolTipInvalidChar.AutomaticDelay = 0;
			this.toolTipInvalidChar.AutoPopDelay = 0;
			this.toolTipInvalidChar.InitialDelay = 0;
			this.toolTipInvalidChar.IsBalloon = true;
			this.toolTipInvalidChar.ReshowDelay = 0;
			//
			// ExportUsfmDialog
			//
			this.AcceptButton = this.btnOk;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.btnCancel;
			this.Controls.Add(this.grpExportWhat);
			this.Controls.Add(this.grpOutputTo);
			this.Controls.Add(this.lblBooks);
			this.Controls.Add(this.m_btnHelp);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnOk);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ExportUsfmDialog";
			this.ShowInTaskbar = false;
			this.grpOutputTo.ResumeLayout(false);
			this.grpOutputTo.PerformLayout();
			this.grpExportWhat.ResumeLayout(false);
			this.grpExportWhat.PerformLayout();
			this.ResumeLayout(false);

		}
		#endregion

		#region Misc. Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Saves the registry settings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void SaveRegistrySettings()
		{
			string type = RegistrySettingType;

			m_regGroup.SetBoolValue(type + "InterleavedIncludeNotes", chkNotes.Checked);
			m_regGroup.SetStringValue(type + "FilePrefix", fileNameSchemeCtrl.Prefix);
			m_regGroup.SetStringValue(type + "FileScheme", fileNameSchemeCtrl.Scheme);
			m_regGroup.SetStringValue(type + "FileSuffix", fileNameSchemeCtrl.Suffix);
			m_regGroup.SetStringValue(type + "FileExtension", fileNameSchemeCtrl.Extension);

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads the file name scheme control.
		/// </summary>
		/// <param name="type">String which is added as a prefix to registry settings to
		/// indicate the type of export it pertains to</param>
		/// <param name="defaultSuffix">The default suffix</param>
		/// ------------------------------------------------------------------------------------
		protected void LoadFileNameSchemeControl(string type, string defaultSuffix)
		{
			fileNameSchemeCtrl.Suffix = m_regGroup.GetStringValue(type + "FileSuffix", defaultSuffix);
			if (fileNameSchemeCtrl.Suffix != defaultSuffix)
				fileNameSchemeCtrl.UserModifiedSuffix = true;
			fileNameSchemeCtrl.Prefix = m_regGroup.GetStringValue(type + "FilePrefix", string.Empty);
			// For Scheme and Extension, the control will automatically use the correct defaults
			// if we pass null (i.e., if no value is retrieved from the registry).
			fileNameSchemeCtrl.Scheme = m_regGroup.GetStringValue(type + "FileScheme", null);
			fileNameSchemeCtrl.Extension = m_regGroup.GetStringValue(type + "FileExtension", null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the invalid path character.
		/// </summary>
		/// <param name="textbox">The textbox containing a directory or file name.</param>
		/// <param name="iInvalidChar">The character index of the invalid character in the
		/// textbox.</param>
		/// ------------------------------------------------------------------------------------
		protected void HandleInvalidPathChar(TextBox textbox, int iInvalidChar)
		{
			toolTipInvalidChar.Active = false;
			// Remove the invalid character
			textbox.Text = textbox.Text.Remove(iInvalidChar, 1);

			// Reset selection in textbox so it doesn't move back to the start of the output file name.
			textbox.SelectionStart = iInvalidChar < textbox.Text.Length - 1 ?
				iInvalidChar : iInvalidChar - 1;

			// Display a tooltip with a list of invalid path characters
			string msg = string.Format(TeResourceHelper.GetResourceString("kstidInvalidPathCharToolTip"),
				InvalidPathCharString);
			toolTipInvalidChar.Active = true;
			toolTipInvalidChar.SetToolTip(textbox, msg); // Seems dumb to have this here, but without it, the first time, the tooltip displays wrong.
			toolTipInvalidChar.Show(msg, textbox, textbox.Width / 2, textbox.Height, 5000);
		}
		#endregion

		#region Event Handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Load event of the ExportUsfmDialog control.
		/// </summary>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			if (DesignMode)
				return;

			// Get file format settings.
			LoadFileNameSchemeControl(RegistrySettingType, DefaultSuffix);

			// Populate checked list box with currently used analysis writing systems
			foreach (int hvoWs in m_cache.GetUsedScriptureBackTransWs())
				chklbWritingSystems.Items.Add(new LgWritingSystem(m_cache, hvoWs));
			if (chklbWritingSystems.Items.Count > 0)
			{
				chklbWritingSystems.SetItemChecked(0, true); // select default analysis ws
				// Display writing systems checked list box only if back translations are enabled.
				chklbWritingSystems.Enabled = ExportBackTranslationDomain;
				UpdateBtWsChecks();
			}
			else
			{
				chklbWritingSystems.Enabled = false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the Folder browse button to locate a folder to write to.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void btnFolderBrowse_Click(object sender, System.EventArgs e)
		{
			using (FolderBrowserDialog dlg = new FolderBrowserDialog())
			{
				dlg.SelectedPath = BaseOutputFolder;
				dlg.Description = TeResourceHelper.GetResourceString(StidExportDlgFolderBrowserPrompt);
				dlg.ShowNewFolderButton = true;
				if (dlg.ShowDialog() == DialogResult.OK)
				{
					BaseOutputFolder = dlg.SelectedPath;
					m_fUserModifiedFolder = true;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the resource string id for the export dialog folder browser prompt.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual string StidExportDlgFolderBrowserPrompt
		{
			get { return "kstidExportDlgFolderBrowserPrompt"; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure that an output location has been specified
		/// </summary>
		/// <returns>true if the output location is specified, else false</returns>
		/// ------------------------------------------------------------------------------------
		protected virtual bool ValidateOutput()
		{
			return (OutputSpec != string.Empty);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a click on the Ok button.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void btnOk_Click(object sender, System.EventArgs e)
		{
			if (!OkayToProceedWithExport)
				return;

			// Make sure the path exists and is valid
			string sOutputDirectory = OutputDirectory;
			if (!Directory.Exists(sOutputDirectory))
			{
				try
				{
					Directory.CreateDirectory(sOutputDirectory);
				}
				catch (Exception error)
				{
					MessageBox.Show(error.Message, Application.ProductName,
						MessageBoxButtons.OK, MessageBoxIcon.Warning);
					TxtOutputSpec.Focus();
				}
			}

			PrepareToExport();

			if (m_regGroup != null)
				SaveRegistrySettings();

			DialogResult = DialogResult.OK;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Final preparation before starting the export. Default behavior is a no-op
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void PrepareToExport()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a click on the Help button
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_btnHelp_Click(object sender, System.EventArgs e)
		{
			ShowHelp.ShowHelpTopic(FwApp.App, HelpTopic);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check for invalid characters in the path and update the OK button.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void txtOutputFolder_TextChanged(object sender, System.EventArgs e)
		{
			// Only check for invalid characters in the output folder text box if it is the sender...
			if (sender == txtOutputFolder)
			{
				if (toolTipInvalidChar.Active)
					toolTipInvalidChar.Active = false;

				int i = txtOutputFolder.Text.IndexOfAny(Path.GetInvalidPathChars());
				if (i >= 0)
				{
					HandleInvalidPathChar(txtOutputFolder, i);
				}
			}

			btnOk.Enabled = AtleastOneDomainSelected &&
					(!ExportBackTranslationDomain || RequestedAnalWs.Length >= 1)
					&& ValidateOutput();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If the options of what to export changed, update the BT writing systems that are
		/// checked and update the the OK button (enabling it only if at least one domain
		/// selected for export).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void WhatToExportChanged(object sender, EventArgs e)
		{
			UpdateBtWsChecks();
			txtOutputFolder_TextChanged(sender, e);

			//temporary: if neither Scripture nor BackTrans is checked, disable the notes checkbox
			chkNotes.Enabled = (ExportScriptureDomain || BackTranslationsEnabled ||
				m_markupType == MarkupType.Paratext);
			if (!chkNotes.Enabled)
				chkNotes.Checked = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the ItemCheck event of the chklbWritingSystems control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.Windows.Forms.ItemCheckEventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void chklbWritingSystems_ItemCheck(object sender, ItemCheckEventArgs e)
		{
			if (m_fChangingCheckState)
				return;

			// Don't allow the only checked WS to be unchecked
			if (e.NewValue == CheckState.Unchecked &&
				chklbWritingSystems.CheckedIndices.Count == 1 &&
				chklbWritingSystems.CheckedIndices[0] == e.Index)
			{
				e.NewValue = CheckState.Checked;
				return;
			}

			// If our state requires only one BT writing system then clear all the other
			// writing systems
			if (CurrentStateRequiresOnlyOneBtWs)
			{
				m_fChangingCheckState = true;
				ClearWsExceptIndex(e.Index);
				m_fChangingCheckState = false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the back translation writing system checks.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void UpdateBtWsChecks()
		{
			// nothing to do if BT is not requested
			if (!ExportBackTranslationDomain)
				return;

			int cChk = chklbWritingSystems.CheckedIndices.Count;

			// if only one BT is required, clear any extras
			if (CurrentStateRequiresOnlyOneBtWs && cChk > 1)
				ClearWsExceptIndex(chklbWritingSystems.CheckedIndices[0]);

			// Be sure at least one WS is checked
			if (cChk == 0)
				SetDefaultBtWs();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clears the checks of the writing systems except the specified index.
		/// </summary>
		/// <param name="index">The index to exclude (-1 to clear all check boxes)</param>
		/// ------------------------------------------------------------------------------------
		private void ClearWsExceptIndex(int index)
		{
			m_fChangingCheckState = true;
			for (int i = 0; i < chklbWritingSystems.Items.Count; i++)
			{
				if (i != index)
					chklbWritingSystems.SetItemChecked(i, false);
			}
			m_fChangingCheckState = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets a default BT writing system checkbox, if possible.
		/// Precondition: No writing system is currently checked.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetDefaultBtWs()
		{
			Debug.Assert(chklbWritingSystems.CheckedIndices.Count == 0);

			// Normally the DefaultAnalWS is the one we want to check.
			// If a BT exists in the DefaultAnalWS, that WS is the first in our list. But
			//  possibly it is not in our list.
			// We will set the first item in the list, to cover either case.
			if (chklbWritingSystems.Items.Count > 0)
			{
				m_fChangingCheckState = true;
				chklbWritingSystems.SetItemChecked(0, true);
				m_fChangingCheckState = false;
			}
		}
		#endregion

		#region Properties that must be overridden (theoretically abstract, but Designer can't handle deriving from an abstract form)
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the help topic key.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual string HelpTopic
		{
			get { throw new NotImplementedException("Derived class must implement"); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether back translations are enabled.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if back translations enabled; otherwise, <c>false</c>.
		/// </value>
		/// ------------------------------------------------------------------------------------
		protected virtual bool BackTranslationsEnabled
		{
			get { throw new NotImplementedException("Derived class must implement"); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether user has requested export of Scripture.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual bool ExportScriptureDomain
		{
			get
			{
				CheckDisposed();
				throw new NotImplementedException("Derived class must implement");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether user has requested export of back translation data.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual bool ExportBackTranslationDomain
		{
			get
			{
				CheckDisposed();
				throw new NotImplementedException("Derived class must implement");
			}
		}
		#endregion

		#region Private properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets string containing a space-delimited list of invalid path characters.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private string InvalidPathCharString
		{
			get
			{
				StringBuilder strBldr = new StringBuilder();
				foreach (char invalidChar in Path.GetInvalidPathChars())
				{
					strBldr.Append(invalidChar);
					strBldr.Append(' ');
				}
				return strBldr.ToString();
			}
		}
		#endregion

		#region Protected Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if our state requires only one BT writing system
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual bool CurrentStateRequiresOnlyOneBtWs
		{
			get { return true; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the abbreviation for the user's vernacular writing system.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected virtual string DefaultSuffix
		{
			get
			{
				LgWritingSystem ws = new LgWritingSystem(m_cache, m_cache.DefaultVernWs);
				return (ws.Abbreviation == null) ? string.Empty : ws.Abbreviation;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the base output folder.
		/// </summary>
		/// <remarks>Use this property rather than the value of txtOutputFolder.Text to refer
		/// to the base folder. Derived classes may add an additional project-specific subfolder
		/// to the folder name displayed in the text box control. This property is used to
		/// determine the folder location to display in the browse dialog
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		protected virtual string BaseOutputFolder
		{
			get { return txtOutputFolder.Text; }
			set { txtOutputFolder.Text = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the output directory.
		/// </summary>
		/// <remarks>Use this property rather than the value of txtOutputFolder.Text to refer
		/// to the folder that should actually get created just before closing this dialog and
		/// starting the export process. Derived classes may override this if the export is to
		/// a file in some other location (see Toolbox override).
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		protected virtual string OutputDirectory
		{
			get { return txtOutputFolder.Text; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the control which should receive focus if there is a problem creating the
		/// output directory.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual TextBox TxtOutputSpec
		{
			get { return txtOutputFolder; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether it's okay to proceed with export.
		/// </summary>
		/// <remarks>Subclasses can override to perform additional validation when OK button
		/// is pressed. If overridden, must call base implementation first.</remarks>
		/// ------------------------------------------------------------------------------------
		protected virtual bool OkayToProceedWithExport
		{
			get
			{
				// Make sure that a valid file or output folder has been selected.
				if (FwApp.App == null)
					return false;
				Debug.Assert(ValidateOutput());
				try
				{
					Path.GetFullPath(OutputSpec);
				}
				catch (Exception e)
				{
					MessageBox.Show(e.Message, Application.ProductName, MessageBoxButtons.OK,
						MessageBoxIcon.Warning);
					TxtOutputSpec.Focus();
					return false;
				}
				return true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a string which can be added as a prefix to a registry setting to indicate the
		/// type of export it pertains to.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual string RegistrySettingType
		{
			get { return m_markupType.ToString(); }
		}
		#endregion

		#region Public Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the requested analysis writing systems which are checked in the list box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int[] RequestedAnalWs
		{
			get
			{
				CheckDisposed();

				int[] requestedAnalWs = new int[chklbWritingSystems.CheckedItems.Count];
				for (int i = 0; i < chklbWritingSystems.CheckedItems.Count; i++)
					requestedAnalWs[i] = ((LgWritingSystem)chklbWritingSystems.CheckedItems[i]).Hvo;
				return requestedAnalWs;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When the user specified exporting to a single file, this returns that file name
		/// (including the full path). When the user specified exporting one file per book,
		/// this returns the full path where those files are written.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual string OutputSpec
		{
			get
			{
				CheckDisposed();
				return txtOutputFolder.Text;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve the dialog setting for the markup system. The value is true for Paratext
		/// and false for toolbox/shoebox
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ExportParatextMarkup
		{
			get
			{
				CheckDisposed();
				return m_markupType == MarkupType.Paratext;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets whether at least one domain is selected for export.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool AtleastOneDomainSelected
		{
			get
			{
				CheckDisposed();
				return ExportScriptureDomain || ExportBackTranslationDomain || ExportNotesDomain;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve the dialog setting for exporting annotations.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ExportNotesDomain
		{
			get
			{
				CheckDisposed();
				return chkNotes.Checked;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the file name format.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FileNameFormat FileNameFormat
		{
			get
			{
				CheckDisposed();

				return fileNameSchemeCtrl.FileNameFormat;
			}
		}
		#endregion
	}
}

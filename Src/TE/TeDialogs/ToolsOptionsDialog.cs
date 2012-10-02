// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ToolsOptionsDialog.cs
// Responsibility: Edge
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.IO;
using System.Drawing;
using System.Globalization;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Resources;
using System.Diagnostics;
using Microsoft.Win32;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.Drawing;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.TE
{
	#region ToolsOptionsDialog class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for ToolsOptionsDialog.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ToolsOptionsDialog : Form, IFWDisposable
	{
		#region Member Data
		// Dialog tabs
		private const int kDraftViewTab = 0;
		private const int kGeneralTab = 1;
		private const int kStylesTab = 2;
		private const int kInterfaceTab = 3;
		private const int kAdvancedTab = 4;

		// Experimental features
		//private const int kSendReceiveSyncMsgs = 0;
		// The values here correspond to the indexes of the corresponding items in m_cboExperimentalFeatures.
		// NOTE: Vertical view is only available in DEBUG builds, needs to be highest index
		private const int kVerticalDraftView = 2;
		private const int kXhtmlExport = 1;
		private const int kInterlinearBackTranslation = 0;

		private readonly bool m_origInterLinearBTValue;

		private System.Windows.Forms.TabControl tabOptions;
		/// <summary> </summary>
		protected System.Windows.Forms.TabPage tabPageView;
		/// <summary> </summary>
		protected System.Windows.Forms.CheckBox m_chkPromptEmptyParas;
		/// <summary> </summary>
		protected System.Windows.Forms.CheckBox m_chkMarkerlessFootnoteIcons;
		/// <summary> </summary>
		protected System.Windows.Forms.CheckBox m_chkSynchFootnoteScroll;
		/// <summary> </summary>
		protected System.Windows.Forms.TabPage tabPageStyles;
		/// <summary> </summary>
		protected System.Windows.Forms.RadioButton rdoBasicStyles;
		/// <summary> </summary>
		protected System.Windows.Forms.RadioButton rdoAllStyles;
		/// <summary> </summary>
		protected System.Windows.Forms.RadioButton rdoCustomList;
		/// <summary> </summary>
		protected System.Windows.Forms.CheckBox chkShowUserDefined;
		/// <summary> </summary>
		protected FwOverrideComboBox cboStyleLevel;
		private System.ComponentModel.IContainer components;
		private System.Windows.Forms.CheckBox m_chkShowFormatMarks;
		private System.Windows.Forms.CheckBox m_chkStartLibronixWithTE;
		private System.Windows.Forms.CheckBox m_chkDisplayBackupReminder;
		private System.Windows.Forms.TabPage tabPageAdvanced;
		private System.Windows.Forms.TextBox m_textBoxBackupPath;
		private System.Windows.Forms.ToolTip toolTip1;
		private FwOverrideComboBox m_cboMeasurement;
		private System.Windows.Forms.TabPage tabPageGeneral;
		private System.Windows.Forms.TabPage tabPageInterface;
		private GroupBox grpCustom;
		private CheckedListBox m_cboExperimentalFeatures;
		private Label m_lblNoTestFeatures;
		private SIL.FieldWorks.Common.Widgets.UserInterfaceChooser m_userInterfaceChooser;
		private Label label2;
		private Label labelBackupDir;
		private Label label3;
		private RadioButton m_rdoUseOnlyWSsInThisProj;
		private RadioButton m_rdoPromptForNewWs;
		private GroupBox grpPastingWs;
		/// <summary></summary>
		/// <remarks>protected because of testing</remarks>
		protected Button btnOK;
		#endregion

		#region Constructors/Destructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ToolsOptionsDialog"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ToolsOptionsDialog()
		{
			InitializeComponent();

			// Get registry settings for the "Draft View" tab of the dialog.
			m_chkPromptEmptyParas.Checked = Options.ShowEmptyParagraphPromptsSetting;
			m_chkMarkerlessFootnoteIcons.Checked = Options.ShowMarkerlessIconsSetting;
			m_chkShowFormatMarks.Checked = Options.ShowFormatMarksSetting;
			m_chkSynchFootnoteScroll.Checked = Options.FootnoteSynchronousScrollingSetting;
			m_rdoPromptForNewWs.Checked = Options.ShowPasteWsChoice;
			m_rdoUseOnlyWSsInThisProj.Checked = !m_rdoPromptForNewWs.Checked;

			// Load strings and get registry settings for the "Styles" tab of the dialog.
			// check the radio button for the "show these styles" buttons
			switch(Options.ShowTheseStylesSetting)
			{
				case Options.ShowTheseStyles.All:
					rdoAllStyles.Checked = true;
					break;
				case Options.ShowTheseStyles.Basic:
					rdoBasicStyles.Checked = true;
					break;
				case Options.ShowTheseStyles.Custom:
					rdoCustomList.Checked = true;
					break;
			}
			// enable/disable the custom style controls
			rdoCustomList_CheckedChanged(rdoCustomList, null);
			// add style levels for Styles tab
			cboStyleLevel.Items.Add(DlgResources.ResourceString("kstidStyleLevelBasic"));
			cboStyleLevel.Items.Add(DlgResources.ResourceString("kstidStyleLevelIntermediate"));
			cboStyleLevel.Items.Add(DlgResources.ResourceString("kstidStyleLevelAdvanced"));
			cboStyleLevel.Items.Add(DlgResources.ResourceString("kstidStyleLevelExpert"));
			// set the style level combo box selection
			switch(Options.ShowStyleLevelSetting)
			{
				case Options.StyleLevel.Basic:
					cboStyleLevel.SelectedIndex = 0;
					break;
				case Options.StyleLevel.Intermediate:
					cboStyleLevel.SelectedIndex = 1;
					break;
				case Options.StyleLevel.Advanced:
					cboStyleLevel.SelectedIndex = 2;
					break;
				case Options.StyleLevel.Expert:
					cboStyleLevel.SelectedIndex = 3;
					break;
			}
			chkShowUserDefined.Checked = Options.ShowUserDefinedStylesSetting;

			// Get registry settings for the "General" tab of the dialog.
			FwApp app = FwApp.App;
			m_chkStartLibronixWithTE.Checked = app.AutoStartLibronix;
			m_chkDisplayBackupReminder.Checked = Options.ShowImportBackupSetting;
			m_cboMeasurement.SelectedIndex = FwRegistrySettings.MeasurementUnitSetting;

			// Get registry settings for the "Advanced" tab of the dialog.
			// set the default backup directory
			if (FwApp.App != null)
				m_textBoxBackupPath.Text = FwRegistrySettings.BackupDirectorySetting;
			m_userInterfaceChooser.Init(Options.UserInterfaceWritingSystem);

			// Use the following code block to set the checked values for experimental features.
			// Currently, there are no experimental features that can be turned on/off through the
			// Tools/Options dialog.
#if DEBUG
			m_cboExperimentalFeatures.SetItemChecked(kVerticalDraftView, Options.UseVerticalDraftView);
#endif
			m_origInterLinearBTValue = Options.UseInterlinearBackTranslation;
			m_cboExperimentalFeatures.SetItemChecked(kInterlinearBackTranslation, m_origInterLinearBTValue);
			m_cboExperimentalFeatures.SetItemChecked(kXhtmlExport, Options.UseXhtmlExport);
#if !DEBUG
			m_cboExperimentalFeatures.Items.RemoveAt(kVerticalDraftView);
#endif
			// Execute this block if there are no experimental features (or none we want the user to see in a release build).
			//{
			//    m_cboExperimentalFeatures.Visible = false;
			//    m_lblNoTestFeatures.Visible = true;
			//}
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
			System.Windows.Forms.GroupBox groupBox1;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ToolsOptionsDialog));
			System.Windows.Forms.GroupBox groupBox3;
			System.Windows.Forms.Label lbMeasurement;
			System.Windows.Forms.Label lblStyleNote;
			System.Windows.Forms.Label lblShowStyles;
			System.Windows.Forms.Label lblStyleLevel;
			System.Windows.Forms.Button m_btnBrowse;
			System.Windows.Forms.Button btnCancel;
			System.Windows.Forms.Button btnHelp;
			System.Windows.Forms.GroupBox groupBox2;
			System.Windows.Forms.Label label1;
			this.m_chkSynchFootnoteScroll = new System.Windows.Forms.CheckBox();
			this.m_chkShowFormatMarks = new System.Windows.Forms.CheckBox();
			this.m_chkMarkerlessFootnoteIcons = new System.Windows.Forms.CheckBox();
			this.m_chkPromptEmptyParas = new System.Windows.Forms.CheckBox();
			this.m_lblNoTestFeatures = new System.Windows.Forms.Label();
			this.m_cboExperimentalFeatures = new System.Windows.Forms.CheckedListBox();
			this.labelBackupDir = new System.Windows.Forms.Label();
			this.btnOK = new System.Windows.Forms.Button();
			this.tabOptions = new System.Windows.Forms.TabControl();
			this.tabPageView = new System.Windows.Forms.TabPage();
			this.tabPageGeneral = new System.Windows.Forms.TabPage();
			this.grpPastingWs = new System.Windows.Forms.GroupBox();
			this.m_rdoUseOnlyWSsInThisProj = new System.Windows.Forms.RadioButton();
			this.m_rdoPromptForNewWs = new System.Windows.Forms.RadioButton();
			this.label3 = new System.Windows.Forms.Label();
			this.m_chkStartLibronixWithTE = new System.Windows.Forms.CheckBox();
			this.m_chkDisplayBackupReminder = new System.Windows.Forms.CheckBox();
			this.tabPageStyles = new System.Windows.Forms.TabPage();
			this.rdoCustomList = new System.Windows.Forms.RadioButton();
			this.rdoAllStyles = new System.Windows.Forms.RadioButton();
			this.rdoBasicStyles = new System.Windows.Forms.RadioButton();
			this.grpCustom = new System.Windows.Forms.GroupBox();
			this.cboStyleLevel = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.chkShowUserDefined = new System.Windows.Forms.CheckBox();
			this.tabPageInterface = new System.Windows.Forms.TabPage();
			this.label2 = new System.Windows.Forms.Label();
			this.m_userInterfaceChooser = new SIL.FieldWorks.Common.Widgets.UserInterfaceChooser();
			this.m_cboMeasurement = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.tabPageAdvanced = new System.Windows.Forms.TabPage();
			this.m_textBoxBackupPath = new System.Windows.Forms.TextBox();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			groupBox1 = new System.Windows.Forms.GroupBox();
			groupBox3 = new System.Windows.Forms.GroupBox();
			lbMeasurement = new System.Windows.Forms.Label();
			lblStyleNote = new System.Windows.Forms.Label();
			lblShowStyles = new System.Windows.Forms.Label();
			lblStyleLevel = new System.Windows.Forms.Label();
			m_btnBrowse = new System.Windows.Forms.Button();
			btnCancel = new System.Windows.Forms.Button();
			btnHelp = new System.Windows.Forms.Button();
			groupBox2 = new System.Windows.Forms.GroupBox();
			label1 = new System.Windows.Forms.Label();
			groupBox1.SuspendLayout();
			groupBox3.SuspendLayout();
			groupBox2.SuspendLayout();
			this.tabOptions.SuspendLayout();
			this.tabPageView.SuspendLayout();
			this.tabPageGeneral.SuspendLayout();
			this.grpPastingWs.SuspendLayout();
			this.tabPageStyles.SuspendLayout();
			this.grpCustom.SuspendLayout();
			this.tabPageInterface.SuspendLayout();
			this.tabPageAdvanced.SuspendLayout();
			this.SuspendLayout();
			//
			// groupBox1
			//
			groupBox1.Controls.Add(this.m_chkSynchFootnoteScroll);
			resources.ApplyResources(groupBox1, "groupBox1");
			groupBox1.Name = "groupBox1";
			groupBox1.TabStop = false;
			//
			// m_chkSynchFootnoteScroll
			//
			resources.ApplyResources(this.m_chkSynchFootnoteScroll, "m_chkSynchFootnoteScroll");
			this.m_chkSynchFootnoteScroll.Name = "m_chkSynchFootnoteScroll";
			//
			// groupBox3
			//
			groupBox3.Controls.Add(this.m_chkShowFormatMarks);
			groupBox3.Controls.Add(this.m_chkMarkerlessFootnoteIcons);
			groupBox3.Controls.Add(this.m_chkPromptEmptyParas);
			resources.ApplyResources(groupBox3, "groupBox3");
			groupBox3.Name = "groupBox3";
			groupBox3.TabStop = false;
			//
			// m_chkShowFormatMarks
			//
			resources.ApplyResources(this.m_chkShowFormatMarks, "m_chkShowFormatMarks");
			this.m_chkShowFormatMarks.Name = "m_chkShowFormatMarks";
			//
			// m_chkMarkerlessFootnoteIcons
			//
			resources.ApplyResources(this.m_chkMarkerlessFootnoteIcons, "m_chkMarkerlessFootnoteIcons");
			this.m_chkMarkerlessFootnoteIcons.Name = "m_chkMarkerlessFootnoteIcons";
			//
			// m_chkPromptEmptyParas
			//
			resources.ApplyResources(this.m_chkPromptEmptyParas, "m_chkPromptEmptyParas");
			this.m_chkPromptEmptyParas.Name = "m_chkPromptEmptyParas";
			//
			// lbMeasurement
			//
			resources.ApplyResources(lbMeasurement, "lbMeasurement");
			lbMeasurement.Name = "lbMeasurement";
			//
			// lblStyleNote
			//
			resources.ApplyResources(lblStyleNote, "lblStyleNote");
			lblStyleNote.Name = "lblStyleNote";
			//
			// lblShowStyles
			//
			resources.ApplyResources(lblShowStyles, "lblShowStyles");
			lblShowStyles.Name = "lblShowStyles";
			//
			// lblStyleLevel
			//
			resources.ApplyResources(lblStyleLevel, "lblStyleLevel");
			lblStyleLevel.Name = "lblStyleLevel";
			//
			// m_btnBrowse
			//
			resources.ApplyResources(m_btnBrowse, "m_btnBrowse");
			m_btnBrowse.Name = "m_btnBrowse";
			m_btnBrowse.Click += new System.EventHandler(this.m_btnBrowse_Click);
			//
			// btnCancel
			//
			resources.ApplyResources(btnCancel, "btnCancel");
			btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			btnCancel.Name = "btnCancel";
			//
			// btnHelp
			//
			resources.ApplyResources(btnHelp, "btnHelp");
			btnHelp.Name = "btnHelp";
			btnHelp.Click += new System.EventHandler(this.btnHelp_Click);
			//
			// groupBox2
			//
			groupBox2.Controls.Add(this.m_lblNoTestFeatures);
			groupBox2.Controls.Add(this.m_cboExperimentalFeatures);
			resources.ApplyResources(groupBox2, "groupBox2");
			groupBox2.Name = "groupBox2";
			groupBox2.TabStop = false;
			//
			// m_lblNoTestFeatures
			//
			resources.ApplyResources(this.m_lblNoTestFeatures, "m_lblNoTestFeatures");
			this.m_lblNoTestFeatures.Name = "m_lblNoTestFeatures";
			//
			// m_cboExperimentalFeatures
			//
			this.m_cboExperimentalFeatures.BackColor = System.Drawing.SystemColors.Window;
			this.m_cboExperimentalFeatures.FormattingEnabled = true;
			this.m_cboExperimentalFeatures.Items.AddRange(new object[] {
			resources.GetString("m_cboExperimentalFeatures.Items"),
			resources.GetString("m_cboExperimentalFeatures.Items1"),
			resources.GetString("m_cboExperimentalFeatures.Items2")});
			resources.ApplyResources(this.m_cboExperimentalFeatures, "m_cboExperimentalFeatures");
			this.m_cboExperimentalFeatures.Name = "m_cboExperimentalFeatures";
			//
			// label1
			//
			resources.ApplyResources(label1, "label1");
			label1.Name = "label1";
			//
			// labelBackupDir
			//
			resources.ApplyResources(this.labelBackupDir, "labelBackupDir");
			this.labelBackupDir.Name = "labelBackupDir";
			//
			// btnOK
			//
			resources.ApplyResources(this.btnOK, "btnOK");
			this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnOK.Name = "btnOK";
			this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
			//
			// tabOptions
			//
			this.tabOptions.Controls.Add(this.tabPageView);
			this.tabOptions.Controls.Add(this.tabPageGeneral);
			this.tabOptions.Controls.Add(this.tabPageStyles);
			this.tabOptions.Controls.Add(this.tabPageInterface);
			this.tabOptions.Controls.Add(this.tabPageAdvanced);
			this.tabOptions.HotTrack = true;
			resources.ApplyResources(this.tabOptions, "tabOptions");
			this.tabOptions.Name = "tabOptions";
			this.tabOptions.SelectedIndex = 0;
			//
			// tabPageView
			//
			this.tabPageView.Controls.Add(groupBox1);
			this.tabPageView.Controls.Add(groupBox3);
			resources.ApplyResources(this.tabPageView, "tabPageView");
			this.tabPageView.Name = "tabPageView";
			this.tabPageView.UseVisualStyleBackColor = true;
			//
			// tabPageGeneral
			//
			this.tabPageGeneral.Controls.Add(this.grpPastingWs);
			this.tabPageGeneral.Controls.Add(this.m_chkStartLibronixWithTE);
			this.tabPageGeneral.Controls.Add(this.m_chkDisplayBackupReminder);
			resources.ApplyResources(this.tabPageGeneral, "tabPageGeneral");
			this.tabPageGeneral.Name = "tabPageGeneral";
			this.tabPageGeneral.UseVisualStyleBackColor = true;
			//
			// grpPastingWs
			//
			this.grpPastingWs.Controls.Add(this.m_rdoUseOnlyWSsInThisProj);
			this.grpPastingWs.Controls.Add(this.m_rdoPromptForNewWs);
			this.grpPastingWs.Controls.Add(this.label3);
			resources.ApplyResources(this.grpPastingWs, "grpPastingWs");
			this.grpPastingWs.Name = "grpPastingWs";
			this.grpPastingWs.TabStop = false;
			//
			// m_rdoUseOnlyWSsInThisProj
			//
			resources.ApplyResources(this.m_rdoUseOnlyWSsInThisProj, "m_rdoUseOnlyWSsInThisProj");
			this.m_rdoUseOnlyWSsInThisProj.Name = "m_rdoUseOnlyWSsInThisProj";
			this.m_rdoUseOnlyWSsInThisProj.UseVisualStyleBackColor = true;
			//
			// m_rdoPromptForNewWs
			//
			resources.ApplyResources(this.m_rdoPromptForNewWs, "m_rdoPromptForNewWs");
			this.m_rdoPromptForNewWs.Checked = true;
			this.m_rdoPromptForNewWs.Name = "m_rdoPromptForNewWs";
			this.m_rdoPromptForNewWs.TabStop = true;
			this.m_rdoPromptForNewWs.UseVisualStyleBackColor = true;
			//
			// label3
			//
			this.label3.AllowDrop = true;
			resources.ApplyResources(this.label3, "label3");
			this.label3.Name = "label3";
			//
			// m_chkStartLibronixWithTE
			//
			resources.ApplyResources(this.m_chkStartLibronixWithTE, "m_chkStartLibronixWithTE");
			this.m_chkStartLibronixWithTE.Name = "m_chkStartLibronixWithTE";
			//
			// m_chkDisplayBackupReminder
			//
			resources.ApplyResources(this.m_chkDisplayBackupReminder, "m_chkDisplayBackupReminder");
			this.m_chkDisplayBackupReminder.Name = "m_chkDisplayBackupReminder";
			//
			// tabPageStyles
			//
			this.tabPageStyles.Controls.Add(lblStyleNote);
			this.tabPageStyles.Controls.Add(this.rdoCustomList);
			this.tabPageStyles.Controls.Add(this.rdoAllStyles);
			this.tabPageStyles.Controls.Add(this.rdoBasicStyles);
			this.tabPageStyles.Controls.Add(lblShowStyles);
			this.tabPageStyles.Controls.Add(this.grpCustom);
			resources.ApplyResources(this.tabPageStyles, "tabPageStyles");
			this.tabPageStyles.Name = "tabPageStyles";
			this.tabPageStyles.UseVisualStyleBackColor = true;
			//
			// rdoCustomList
			//
			resources.ApplyResources(this.rdoCustomList, "rdoCustomList");
			this.rdoCustomList.Name = "rdoCustomList";
			this.rdoCustomList.CheckedChanged += new System.EventHandler(this.rdoCustomList_CheckedChanged);
			//
			// rdoAllStyles
			//
			resources.ApplyResources(this.rdoAllStyles, "rdoAllStyles");
			this.rdoAllStyles.Name = "rdoAllStyles";
			//
			// rdoBasicStyles
			//
			resources.ApplyResources(this.rdoBasicStyles, "rdoBasicStyles");
			this.rdoBasicStyles.Name = "rdoBasicStyles";
			//
			// grpCustom
			//
			this.grpCustom.Controls.Add(lblStyleLevel);
			this.grpCustom.Controls.Add(this.cboStyleLevel);
			this.grpCustom.Controls.Add(this.chkShowUserDefined);
			resources.ApplyResources(this.grpCustom, "grpCustom");
			this.grpCustom.Name = "grpCustom";
			this.grpCustom.TabStop = false;
			//
			// cboStyleLevel
			//
			this.cboStyleLevel.AllowSpaceInEditBox = false;
			this.cboStyleLevel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			resources.ApplyResources(this.cboStyleLevel, "cboStyleLevel");
			this.cboStyleLevel.Name = "cboStyleLevel";
			//
			// chkShowUserDefined
			//
			resources.ApplyResources(this.chkShowUserDefined, "chkShowUserDefined");
			this.chkShowUserDefined.Name = "chkShowUserDefined";
			//
			// tabPageInterface
			//
			this.tabPageInterface.Controls.Add(this.label2);
			this.tabPageInterface.Controls.Add(label1);
			this.tabPageInterface.Controls.Add(this.m_userInterfaceChooser);
			this.tabPageInterface.Controls.Add(lbMeasurement);
			this.tabPageInterface.Controls.Add(this.m_cboMeasurement);
			resources.ApplyResources(this.tabPageInterface, "tabPageInterface");
			this.tabPageInterface.Name = "tabPageInterface";
			this.tabPageInterface.UseVisualStyleBackColor = true;
			//
			// label2
			//
			resources.ApplyResources(this.label2, "label2");
			this.label2.Name = "label2";
			//
			// m_userInterfaceChooser
			//
			resources.ApplyResources(this.m_userInterfaceChooser, "m_userInterfaceChooser");
			this.m_userInterfaceChooser.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_userInterfaceChooser.FormattingEnabled = true;
			this.m_userInterfaceChooser.Name = "m_userInterfaceChooser";
			this.m_userInterfaceChooser.Sorted = true;
			//
			// m_cboMeasurement
			//
			this.m_cboMeasurement.AllowSpaceInEditBox = false;
			this.m_cboMeasurement.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			resources.ApplyResources(this.m_cboMeasurement, "m_cboMeasurement");
			this.m_cboMeasurement.Items.AddRange(new object[] {
			resources.GetString("m_cboMeasurement.Items"),
			resources.GetString("m_cboMeasurement.Items1"),
			resources.GetString("m_cboMeasurement.Items2")});
			this.m_cboMeasurement.Name = "m_cboMeasurement";
			//
			// tabPageAdvanced
			//
			this.tabPageAdvanced.Controls.Add(groupBox2);
			this.tabPageAdvanced.Controls.Add(m_btnBrowse);
			this.tabPageAdvanced.Controls.Add(this.m_textBoxBackupPath);
			this.tabPageAdvanced.Controls.Add(this.labelBackupDir);
			resources.ApplyResources(this.tabPageAdvanced, "tabPageAdvanced");
			this.tabPageAdvanced.Name = "tabPageAdvanced";
			this.tabPageAdvanced.UseVisualStyleBackColor = true;
			//
			// m_textBoxBackupPath
			//
			resources.ApplyResources(this.m_textBoxBackupPath, "m_textBoxBackupPath");
			this.m_textBoxBackupPath.Name = "m_textBoxBackupPath";
			//
			// ToolsOptionsDialog
			//
			this.AcceptButton = this.btnOK;
			resources.ApplyResources(this, "$this");
			this.CancelButton = btnCancel;
			this.Controls.Add(btnHelp);
			this.Controls.Add(btnCancel);
			this.Controls.Add(this.btnOK);
			this.Controls.Add(this.tabOptions);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ToolsOptionsDialog";
			this.ShowInTaskbar = false;
			groupBox1.ResumeLayout(false);
			groupBox3.ResumeLayout(false);
			groupBox2.ResumeLayout(false);
			groupBox2.PerformLayout();
			this.tabOptions.ResumeLayout(false);
			this.tabPageView.ResumeLayout(false);
			this.tabPageGeneral.ResumeLayout(false);
			this.grpPastingWs.ResumeLayout(false);
			this.grpPastingWs.PerformLayout();
			this.tabPageStyles.ResumeLayout(false);
			this.tabPageStyles.PerformLayout();
			this.grpCustom.ResumeLayout(false);
			this.grpCustom.PerformLayout();
			this.tabPageInterface.ResumeLayout(false);
			this.tabPageInterface.PerformLayout();
			this.tabPageAdvanced.ResumeLayout(false);
			this.tabPageAdvanced.PerformLayout();
			this.ResumeLayout(false);

		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calculates a maximum style level to display based on the options settings that
		/// have been made in toos/options/style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static int MaxStyleLevel
		{
			get
			{
				switch (Options.ShowTheseStylesSetting)
				{
					case Options.ShowTheseStyles.All:
						return int.MaxValue;

					case Options.ShowTheseStyles.Basic:
						return 0;

					case Options.ShowTheseStyles.Custom:
					{
						switch (Options.ShowStyleLevelSetting)
						{
							case Options.StyleLevel.Basic:
								return 0;
							case Options.StyleLevel.Intermediate:
								return 1;
							case Options.StyleLevel.Advanced:
								return 2;
							case Options.StyleLevel.Expert:
								return 3;
							default:
								return 0;
						}
					}
					default:
						return 0;
				}
			}
		}
		#endregion

		#region Event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the OK button click event.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected void btnOK_Click(object sender, System.EventArgs e)
		{
			// Save values to the registry for the "Draft View" tab.
			Options.ShowEmptyParagraphPromptsSetting = m_chkPromptEmptyParas.Checked;
			Options.ShowMarkerlessIconsSetting = m_chkMarkerlessFootnoteIcons.Checked;
			Options.ShowFormatMarksSetting = m_chkShowFormatMarks.Checked;
			Options.FootnoteSynchronousScrollingSetting = m_chkSynchFootnoteScroll.Checked;
			Options.ShowPasteWsChoice = m_rdoPromptForNewWs.Checked;

			// Save values to the registry for the "Styles" tab.
			if (rdoAllStyles.Checked)
				Options.ShowTheseStylesSetting = Options.ShowTheseStyles.All;
			else if (rdoBasicStyles.Checked)
				Options.ShowTheseStylesSetting = Options.ShowTheseStyles.Basic;
			else if (rdoCustomList.Checked)
				Options.ShowTheseStylesSetting = Options.ShowTheseStyles.Custom;

			string s = (string)cboStyleLevel.SelectedItem;
			if (s == DlgResources.ResourceString("kstidStyleLevelBasic"))
				Options.ShowStyleLevelSetting = Options.StyleLevel.Basic;
			else if (s == DlgResources.ResourceString("kstidStyleLevelIntermediate"))
				Options.ShowStyleLevelSetting = Options.StyleLevel.Intermediate;
			else if (s == DlgResources.ResourceString("kstidStyleLevelAdvanced"))
				Options.ShowStyleLevelSetting = Options.StyleLevel.Advanced;
			else if (s == DlgResources.ResourceString("kstidStyleLevelExpert"))
				Options.ShowStyleLevelSetting = Options.StyleLevel.Expert;

			Options.ShowUserDefinedStylesSetting = chkShowUserDefined.Checked;

			// Save values to the registry for the "General" tab.
			FwApp app = FwApp.App;
			app.AutoStartLibronix = m_chkStartLibronixWithTE.Checked;
			Options.ShowImportBackupSetting = m_chkDisplayBackupReminder.Checked;
			FwRegistrySettings.MeasurementUnitSetting = m_cboMeasurement.SelectedIndex;

			string sNewUserWs = m_userInterfaceChooser.NewUserWs;
			if (Options.UserInterfaceWritingSystem != sNewUserWs)
			{
				CultureInfo ci = MiscUtils.GetCultureForWs(sNewUserWs);
				if (ci != null)
				{
					FormLanguageSwitchSingleton.Instance.ChangeCurrentThreadUICulture(ci);
					FormLanguageSwitchSingleton.Instance.ChangeLanguage(this);
					Options.UserInterfaceWritingSystem = sNewUserWs;
				}
			}

			// Save values to the registry for the "Advanced" tab.
			bool backupDirExists = ConfirmBackupDirectoryExists(m_textBoxBackupPath.Text, true);
			// Only set the directory if it already exists or could be created.
			if (backupDirExists && FwApp.App != null)
				FwRegistrySettings.BackupDirectorySetting = m_textBoxBackupPath.Text;

			// Use the following code block to set registry values for experimental features.
			// Currently, there are no experimental features that can be turned on/off through the
			// Tools/Options dialog.
#if DEBUG
			Options.UseVerticalDraftView = m_cboExperimentalFeatures.GetItemChecked(kVerticalDraftView);
#endif
			Options.UseInterlinearBackTranslation = m_cboExperimentalFeatures.GetItemChecked(kInterlinearBackTranslation);
			Options.UseXhtmlExport = m_cboExperimentalFeatures.GetItemChecked(kXhtmlExport);
			Options.AddErrorReportingInfo();

			if (Options.UseInterlinearBackTranslation != m_origInterLinearBTValue)
			{
				string msg =
					"You have changed the setting for enabling or disabling the" + Environment.NewLine +
					"Experimental feature for Segmented Back Translation. For this" + Environment.NewLine +
					"change to take effect, you must restart Translation Editor.";

				MessageBox.Show(this, msg, Application.ProductName,
					MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Open the help window when the help button is pressed.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void btnHelp_Click(object sender, System.EventArgs e)
		{
			string helpTopicKey = null;
			switch (tabOptions.SelectedIndex)
			{
				case kDraftViewTab:
					helpTopicKey = "khtpOptionsDraftView";
					break;
				case kStylesTab:
					helpTopicKey = "khtpOptionsStyle";
					break;
				case kGeneralTab:
					helpTopicKey = "khtpOptionsGeneral";
					break;
				case kInterfaceTab:
					helpTopicKey = "khtpOptionsInterface";
					break;
				case kAdvancedTab:
					helpTopicKey = "khtpOptionsAdvanced";
					break;
			}

			ShowHelp.ShowHelpTopic(FwApp.App, helpTopicKey);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a click on the Browse button used to change the default backup directory.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_btnBrowse_Click(object sender, System.EventArgs e)
		{
			using (FolderBrowserDialog fldrBrowse = new FolderBrowserDialog())
			{
				fldrBrowse.ShowNewFolderButton = true;
				fldrBrowse.Description = DlgResources.ResourceString("kstidBrowseForBackupDir");;

				bool backupDirExists = ConfirmBackupDirectoryExists(m_textBoxBackupPath.Text, false);

				// if the directory exists which is typed in the text box...
				if (backupDirExists)
					fldrBrowse.SelectedPath = m_textBoxBackupPath.Text;
				else
				{
					// check the last directory used in the registry. If it exists, begin looking
					// here.
					if (ConfirmBackupDirectoryExists(FwRegistrySettings.BackupDirectorySetting, false))
						fldrBrowse.SelectedPath = FwRegistrySettings.BackupDirectorySetting;

						// Otherwise, begin looking on the root directory of the C:\ drive
					else
						fldrBrowse.SelectedPath = "C:\\";
				}

				// if directory selected, set path to it.
				if (fldrBrowse.ShowDialog() == DialogResult.OK)
					m_textBoxBackupPath.Text = fldrBrowse.SelectedPath;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a check changed event for the custom list radio button.  The controls under
		/// the custom area need to be enabled or disabled when the radio button is on or off.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void rdoCustomList_CheckedChanged(object sender, System.EventArgs e)
		{
			grpCustom.Enabled = rdoCustomList.Checked;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle an event when mouse enters the text box to display the whole path if it is
		/// truncated. (Tooltip not currently displayed)
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_textBoxBackupPath_MouseEnter(object sender, System.EventArgs e)
		{
			Graphics graphics = m_textBoxBackupPath.CreateGraphics();

			if (m_textBoxBackupPath.ClientSize.Width + 12 <
				graphics.MeasureString(m_textBoxBackupPath.Text, m_textBoxBackupPath.Font).Width)
			{
				toolTip1.SetToolTip(m_textBoxBackupPath, m_textBoxBackupPath.Text);
				toolTip1.AutoPopDelay = 3500;
				toolTip1.InitialDelay = 1000;
			}
			else
				toolTip1.SetToolTip(m_textBoxBackupPath, null);

			graphics.Dispose();
		}
		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verify that the backup directory exists. If it doesn't exist, attempt to create it.
		/// </summary>
		/// <param name="directory">directory to check</param>
		/// <param name="createDirectory">If true, will attempt to create directory. If false
		/// it will only check the existence of the directory.</param>
		/// <returns>true if the current backup directory exists or could be created;
		/// false otherwise</returns>
		/// ------------------------------------------------------------------------------------
		private bool ConfirmBackupDirectoryExists(string directory, bool createDirectory)
		{
			// Confirm that directory exists or can be created before changing the
			// default backup directory.
			bool backupDirExists;
			if (Directory.Exists(directory))
				backupDirExists = true;
			else
			{
				// Attempt to create a directory if it doesn't exist?
				if (createDirectory)
				{
					// Attempt to create the directoy if it doesn't exist yet.
					try
					{
						Directory.CreateDirectory(directory);
						backupDirExists = true;
					}
					catch
					{
						backupDirExists = false;
					}
				}
				else
					backupDirExists = false;
			}
			return backupDirExists;
		}
		#endregion
	}
	#endregion
}

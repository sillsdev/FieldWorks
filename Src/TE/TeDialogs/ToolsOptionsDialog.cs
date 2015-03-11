// Copyright (c) 2004-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ToolsOptionsDialog.cs
// Responsibility: TE Team

using System;
using System.Globalization;
using System.ComponentModel;
using System.Windows.Forms;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.Controls;
using SIL.Utils;
using SIL.FieldWorks.Common.FwUtils;
using XCore;
using SIL.CoreImpl;

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
		private const int kVerticalDraftView = 3;
		private const int kTranslateUnsQuestions = 2;
		private const int kXhtmlExport = 1;
		private const int kInterlinearBackTranslation = 0;

		private readonly bool m_origInterLinearBTValue;

		private TabControl tabOptions;
		/// <summary> </summary>
		protected TabPage tabPageView;
		/// <summary> </summary>
		protected CheckBox m_chkPromptEmptyParas;
		/// <summary> </summary>
		protected CheckBox m_chkMarkerlessFootnoteIcons;
		/// <summary> </summary>
		protected CheckBox m_chkSynchFootnoteScroll;
		/// <summary> </summary>
		protected TabPage tabPageStyles;
		/// <summary> </summary>
		protected RadioButton rdoBasicStyles;
		/// <summary> </summary>
		protected RadioButton rdoAllStyles;
		/// <summary> </summary>
		protected RadioButton rdoCustomList;
		/// <summary> </summary>
		protected CheckBox chkShowUserDefined;
		/// <summary> </summary>
		protected FwOverrideComboBox cboStyleLevel;
		private IContainer components;
		private IApp m_app;
		private IHelpTopicProvider m_helpTopicProvider;
		private CheckBox m_chkShowFormatMarks;
		private CheckBox m_chkStartLibronixWithTE;
		private TabPage tabPageAdvanced;
		private FwOverrideComboBox m_cboMeasurement;
		private TabPage tabPageGeneral;
		private TabPage tabPageInterface;
		private GroupBox grpCustom;
		private CheckedListBox m_cboExperimentalFeatures;
		private Label m_lblNoTestFeatures;
		private Common.Widgets.UserInterfaceChooser m_userInterfaceChooser;
		private Label label2;
		/// <summary></summary>
		/// <remarks>protected because of testing</remarks>
		protected Button btnOK;
		internal bool m_failedToConnectToService;

		/// <summary>
		/// If we're interacting with the user writing system, we need to work through a
		/// writing system manager.
		/// </summary>
		protected WritingSystemManager m_wsManager;
		#endregion

		#region Constructors/Destructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ToolsOptionsDialog"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private ToolsOptionsDialog()
		{
			InitializeComponent();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ToolsOptionsDialog"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ToolsOptionsDialog(IApp app, IHelpTopicProvider helpTopicProvider,
			WritingSystemManager wsManager) : this()
		{
			m_app = app;
			m_helpTopicProvider = helpTopicProvider;
			m_wsManager = wsManager;

			// Get registry settings for the "Draft View" tab of the dialog.
			m_chkPromptEmptyParas.Checked = Options.ShowEmptyParagraphPromptsSetting;
			m_chkMarkerlessFootnoteIcons.Checked = Options.ShowMarkerlessIconsSetting;
			m_chkShowFormatMarks.Checked = Options.ShowFormatMarksSetting;
			m_chkSynchFootnoteScroll.Checked = Options.FootnoteSynchronousScrollingSetting;

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
			m_chkStartLibronixWithTE.Checked = Options.AutoStartLibronix;
			m_cboMeasurement.SelectedIndex = FwRegistrySettings.MeasurementUnitSetting;

			// Configure the same way as Flex for consistency.  (See FWR-1997.)
			string sUserWs = m_wsManager.UserWritingSystem.Id;
			m_userInterfaceChooser.Init(sUserWs);

			// Use the following code block to set the checked values for experimental features.
#if DEBUG
			m_cboExperimentalFeatures.SetItemChecked(kVerticalDraftView, Options.UseVerticalDraftView);
#endif
			m_origInterLinearBTValue = Options.UseInterlinearBackTranslation;
			m_cboExperimentalFeatures.SetItemChecked(kInterlinearBackTranslation, m_origInterLinearBTValue);
			m_cboExperimentalFeatures.SetItemChecked(kXhtmlExport, Options.UseXhtmlExport);
			m_cboExperimentalFeatures.SetItemChecked(kTranslateUnsQuestions, Options.ShowTranslateUnsQuestions);
#if !DEBUG
			// The vertical view is only available in Debug mode
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

#if DEBUG
		/// <summary/>
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			base.Dispose(disposing);
		}
#endif
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
			System.Windows.Forms.GroupBox groupBox1;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ToolsOptionsDialog));
			System.Windows.Forms.GroupBox groupBox3;
			System.Windows.Forms.Label lbMeasurement;
			System.Windows.Forms.Label lblStyleNote;
			System.Windows.Forms.Label lblShowStyles;
			System.Windows.Forms.Label lblStyleLevel;
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
			this.btnOK = new System.Windows.Forms.Button();
			this.tabOptions = new System.Windows.Forms.TabControl();
			this.tabPageView = new System.Windows.Forms.TabPage();
			this.tabPageGeneral = new System.Windows.Forms.TabPage();
			this.m_chkStartLibronixWithTE = new System.Windows.Forms.CheckBox();
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
			groupBox1 = new System.Windows.Forms.GroupBox();
			groupBox3 = new System.Windows.Forms.GroupBox();
			lbMeasurement = new System.Windows.Forms.Label();
			lblStyleNote = new System.Windows.Forms.Label();
			lblShowStyles = new System.Windows.Forms.Label();
			lblStyleLevel = new System.Windows.Forms.Label();
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
			resources.GetString("m_cboExperimentalFeatures.Items2"),
			resources.GetString("m_cboExperimentalFeatures.Items3")});
			resources.ApplyResources(this.m_cboExperimentalFeatures, "m_cboExperimentalFeatures");
			this.m_cboExperimentalFeatures.Name = "m_cboExperimentalFeatures";
			//
			// label1
			//
			resources.ApplyResources(label1, "label1");
			label1.Name = "label1";
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
			this.tabPageGeneral.Controls.Add(this.m_chkStartLibronixWithTE);
			resources.ApplyResources(this.tabPageGeneral, "tabPageGeneral");
			this.tabPageGeneral.Name = "tabPageGeneral";
			this.tabPageGeneral.UseVisualStyleBackColor = true;
			//
			// m_chkStartLibronixWithTE
			//
			resources.ApplyResources(this.m_chkStartLibronixWithTE, "m_chkStartLibronixWithTE");
			this.m_chkStartLibronixWithTE.Name = "m_chkStartLibronixWithTE";
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
			resources.ApplyResources(this.tabPageAdvanced, "tabPageAdvanced");
			this.tabPageAdvanced.Name = "tabPageAdvanced";
			this.tabPageAdvanced.UseVisualStyleBackColor = true;
			//
			// ToolsOptionsDialog
			//
			this.AcceptButton = this.btnOK;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
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
			this.tabPageStyles.ResumeLayout(false);
			this.tabPageStyles.PerformLayout();
			this.grpCustom.ResumeLayout(false);
			this.grpCustom.PerformLayout();
			this.tabPageInterface.ResumeLayout(false);
			this.tabPageInterface.PerformLayout();
			this.tabPageAdvanced.ResumeLayout(false);
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
			Options.AutoStartLibronix = m_chkStartLibronixWithTE.Checked;
			FwRegistrySettings.MeasurementUnitSetting = m_cboMeasurement.SelectedIndex;

			// Use the following code block to set registry values for experimental features.
			// Currently, there are no experimental features that can be turned on/off through the
			// Tools/Options dialog.
#if DEBUG
			Options.UseVerticalDraftView = m_cboExperimentalFeatures.GetItemChecked(kVerticalDraftView);
#endif
			Options.UseInterlinearBackTranslation = m_cboExperimentalFeatures.GetItemChecked(kInterlinearBackTranslation);
			Options.UseXhtmlExport = m_cboExperimentalFeatures.GetItemChecked(kXhtmlExport);
			// The UNS translation feature is only available if the required files are present.
			if (m_cboExperimentalFeatures.Items.Count > kTranslateUnsQuestions)
				Options.ShowTranslateUnsQuestions = m_cboExperimentalFeatures.GetItemChecked(kTranslateUnsQuestions);
			Options.AddErrorReportingInfo();

			if (Options.UseInterlinearBackTranslation != m_origInterLinearBTValue)
			{
				string msg =
					"You have changed the setting for enabling or disabling the" + Environment.NewLine +
					"Experimental feature for Segmented Back Translation. For this" + Environment.NewLine +
					"change to take effect, you must restart Translation Editor.";

				MessageBox.Show(this, msg, m_app.ApplicationName,
					MessageBoxButtons.OK, MessageBoxIcon.Information);
			}

			// IMPORTANT: This has to be the last thing we do because changing the UI locale
			// will reload the controls in the dialog box and lose any changes the user made.
			string sUserWs = m_wsManager.UserWritingSystem.Id;
			string sNewUserWs = m_userInterfaceChooser.NewUserWs;
			if (sUserWs != sNewUserWs)
			{
				CultureInfo ci = MiscUtils.GetCultureForWs(sNewUserWs);
				if (ci != null)
				{
					FormLanguageSwitchSingleton.Instance.ChangeCurrentThreadUICulture(ci);
					FormLanguageSwitchSingleton.Instance.ChangeLanguage(this);
#if __MonoCS__
					// Mono leaves the wait cursor on, unlike .Net itself.
					Cursor.Current = Cursors.Default;
#endif
				}
				Options.UserInterfaceWritingSystem = sNewUserWs;
				//The writing system the user selects for the user interface may not be loaded yet into the project
				//database. Therefore we need to check this first and if it is not we need to load it.
				CoreWritingSystemDefinition ws;
				m_wsManager.GetOrSet(sNewUserWs, out ws);
				m_wsManager.UserWritingSystem = ws;
			}

			// DON'T ADD CODE HERE (see above)
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

			ShowHelp.ShowHelpTopic(m_helpTopicProvider, helpTopicKey);
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
		#endregion

	}
	#endregion
}

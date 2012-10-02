// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: WritingSystemWizard.cs
// Responsibility: TomB
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;

using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Resources;
using ECInterfaces;
using SilEncConverters31;
using SIL.FieldWorks.Common.Utils;


namespace SIL.FieldWorks.FwCoreDlgs
{
	#region WritingSystemWizard dialog
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for WritingSystemWizard.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[ProgId("FwCoreDlgs.WritingSystemWizard")]
	// Key attribute to hide the "clutter" from System.Windows.Forms.Form
	[ClassInterface(ClassInterfaceType.None)]
	[GuidAttribute("D72BFAE4-70CC-4640-87C0-63FC5D590D40")]
	[ComVisible(true)]
	public class WritingSystemWizard : WizardDialog, IWritingSystemWizard
	{
		#region Data members

		private SIL.FieldWorks.Common.Controls.LanguageSetup languageSetup;
		private System.Windows.Forms.CheckBox chkUsesIPA;
		private System.Windows.Forms.Button btnAdvanced;
		private SIL.FieldWorks.FwCoreDlgControls.RegionVariantControl m_regionVariantControl;
		private System.Windows.Forms.Label lblMoreInfoDesc;
		private SIL.FieldWorks.FwCoreDlgControls.KeyboardControl m_KeyboardControl;
		private System.Windows.Forms.Button btnEncodingConverterNew;
		private FwOverrideComboBox cbEncodingConverter;
		private System.Windows.Forms.Label lblEncodingConverter;
		private System.Windows.Forms.Label lblConverterDesc;
		private System.Windows.Forms.RadioButton radioRTL;
		private System.Windows.Forms.RadioButton radioLTR;
		private SIL.FieldWorks.FwCoreDlgControls.LocaleMenuButton m_localeMenuButton;
		private SIL.FieldWorks.FwCoreDlgControls.DefaultFontsControl m_defaultFontsControl;

		private ILgWritingSystemFactory m_wsf; // writing system factory.
		private int m_UserWs;

		private LanguageDefinition m_langDef;
		private IHelpTopicProvider m_helpTopicProvider;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
		private System.Windows.Forms.TextBox m_ShortWsName;
		private System.Windows.Forms.HelpProvider helpProvider1;
		private string m_AdvancedButtonText;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="WritingSystemWizard"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public WritingSystemWizard()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			NextButtonEnabled = false;
			AcceptButton = null;
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
				if (components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the dialog as a modal dialog
		/// </summary>
		/// <returns>A DialogResult value</returns>
		/// ------------------------------------------------------------------------------------
		public int DisplayDialog()
		{
			CheckDisposed();

			DialogResult result = base.ShowDialog();
			return (int)result;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the dialog with what it needs.
		/// </summary>
		/// <param name="wsf">writing system factory</param>
		/// <param name="helpTopicProvider">Dialog properties for help topics</param>
		/// ------------------------------------------------------------------------------------
		public void Init(ILgWritingSystemFactory wsf, IHelpTopicProvider helpTopicProvider)
		{
			CheckDisposed();

			m_wsf = wsf;
			m_UserWs = m_wsf.UserWs;
			IWritingSystem writingSystem = WritingSystemClass.Create();
			writingSystem.WritingSystemFactory = wsf;
			m_langDef = new LanguageDefinition(writingSystem);
			// Create LocaleParts
			Icu.InitIcuDataDir();
			m_regionVariantControl.LangDef = m_langDef;

			m_helpTopicProvider = helpTopicProvider;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is called just before ShowDialog. It sets up the target string as a language
		/// name and performs an initial search for this language as the dialog comes up.
		/// </summary>
		/// <param name="target"></param>
		/// ------------------------------------------------------------------------------------
		public void PerformInitialSearch(string target)
		{
			CheckDisposed();

			languageSetup.PerformInitialSearch(target);
		}

		/// <summary>
		/// Retrieve the created writing system object.
		/// </summary>
		/// <returns></returns>
		public IWritingSystem WritingSystem()
		{
			CheckDisposed();

			return m_langDef.WritingSystem;
		}

		#region Windows Form Designer generated code
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(WritingSystemWizard));
			System.Windows.Forms.TabPage tabPage1;
			System.Windows.Forms.Label lblIdentifyLanguage;
			System.Windows.Forms.Label lblAbbrev;
			System.Windows.Forms.Label lblWsAbbrevInstructions;
			System.Windows.Forms.Label lblWritingSystemAbbrev;
			System.Windows.Forms.TabPage tabPage2;
			System.Windows.Forms.Label lblRegionVariantInfo;
			System.Windows.Forms.Label lblRegionVariantInfoDesc;
			System.Windows.Forms.TabPage tabPage4;
			System.Windows.Forms.Label label2;
			System.Windows.Forms.Label lblFinishOrBack;
			System.Windows.Forms.Label lblKeyboardDesc;
			System.Windows.Forms.Label lblKeyboard;
			System.Windows.Forms.Label lblDirectionDesc;
			System.Windows.Forms.Label lblDirection;
			System.Windows.Forms.Label lblDefaultFonts;
			System.Windows.Forms.Label lblSimilarWSDesc;
			System.Windows.Forms.Label lblSimilarWS;
			System.Windows.Forms.TabPage tabPage3;
			System.Windows.Forms.Label label1;
			System.Windows.Forms.Label lblFontDesc;
			this.languageSetup = new SIL.FieldWorks.Common.Controls.LanguageSetup();
			this.lblMoreInfoDesc = new System.Windows.Forms.Label();
			this.m_regionVariantControl = new SIL.FieldWorks.FwCoreDlgControls.RegionVariantControl();
			this.btnAdvanced = new System.Windows.Forms.Button();
			this.chkUsesIPA = new System.Windows.Forms.CheckBox();
			this.m_ShortWsName = new System.Windows.Forms.TextBox();
			this.lblConverterDesc = new System.Windows.Forms.Label();
			this.btnEncodingConverterNew = new System.Windows.Forms.Button();
			this.cbEncodingConverter = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.lblEncodingConverter = new System.Windows.Forms.Label();
			this.m_KeyboardControl = new SIL.FieldWorks.FwCoreDlgControls.KeyboardControl();
			this.m_defaultFontsControl = new SIL.FieldWorks.FwCoreDlgControls.DefaultFontsControl();
			this.radioRTL = new System.Windows.Forms.RadioButton();
			this.radioLTR = new System.Windows.Forms.RadioButton();
			this.m_localeMenuButton = new SIL.FieldWorks.FwCoreDlgControls.LocaleMenuButton();
			this.helpProvider1 = new System.Windows.Forms.HelpProvider();
			tabPage1 = new System.Windows.Forms.TabPage();
			lblIdentifyLanguage = new System.Windows.Forms.Label();
			lblAbbrev = new System.Windows.Forms.Label();
			lblWsAbbrevInstructions = new System.Windows.Forms.Label();
			lblWritingSystemAbbrev = new System.Windows.Forms.Label();
			tabPage2 = new System.Windows.Forms.TabPage();
			lblRegionVariantInfo = new System.Windows.Forms.Label();
			lblRegionVariantInfoDesc = new System.Windows.Forms.Label();
			tabPage4 = new System.Windows.Forms.TabPage();
			label2 = new System.Windows.Forms.Label();
			lblFinishOrBack = new System.Windows.Forms.Label();
			lblKeyboardDesc = new System.Windows.Forms.Label();
			lblKeyboard = new System.Windows.Forms.Label();
			lblDirectionDesc = new System.Windows.Forms.Label();
			lblDirection = new System.Windows.Forms.Label();
			lblDefaultFonts = new System.Windows.Forms.Label();
			lblSimilarWSDesc = new System.Windows.Forms.Label();
			lblSimilarWS = new System.Windows.Forms.Label();
			tabPage3 = new System.Windows.Forms.TabPage();
			label1 = new System.Windows.Forms.Label();
			lblFontDesc = new System.Windows.Forms.Label();
			this.tabSteps.SuspendLayout();
			tabPage1.SuspendLayout();
			tabPage2.SuspendLayout();
			tabPage4.SuspendLayout();
			tabPage3.SuspendLayout();
			this.SuspendLayout();
			//
			// panSteps
			//
			resources.ApplyResources(this.panSteps, "panSteps");
			//
			// lblSteps
			//
			resources.ApplyResources(this.lblSteps, "lblSteps");
			//
			// m_btnBack
			//
			this.helpProvider1.SetHelpString(this.m_btnBack, resources.GetString("m_btnBack.HelpString"));
			resources.ApplyResources(this.m_btnBack, "m_btnBack");
			this.helpProvider1.SetShowHelp(this.m_btnBack, ((bool)(resources.GetObject("m_btnBack.ShowHelp"))));
			//
			// m_btnCancel
			//
			this.helpProvider1.SetHelpString(this.m_btnCancel, resources.GetString("m_btnCancel.HelpString"));
			resources.ApplyResources(this.m_btnCancel, "m_btnCancel");
			this.helpProvider1.SetShowHelp(this.m_btnCancel, ((bool)(resources.GetObject("m_btnCancel.ShowHelp"))));
			//
			// m_btnNext
			//
			this.helpProvider1.SetHelpString(this.m_btnNext, resources.GetString("m_btnNext.HelpString"));
			resources.ApplyResources(this.m_btnNext, "m_btnNext");
			this.helpProvider1.SetShowHelp(this.m_btnNext, ((bool)(resources.GetObject("m_btnNext.ShowHelp"))));
			//
			// m_btnHelp
			//
			this.helpProvider1.SetHelpString(this.m_btnHelp, resources.GetString("m_btnHelp.HelpString"));
			resources.ApplyResources(this.m_btnHelp, "m_btnHelp");
			this.helpProvider1.SetShowHelp(this.m_btnHelp, ((bool)(resources.GetObject("m_btnHelp.ShowHelp"))));
			this.m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// tabSteps
			//
			this.tabSteps.Controls.Add(tabPage1);
			this.tabSteps.Controls.Add(tabPage2);
			this.tabSteps.Controls.Add(tabPage3);
			this.tabSteps.Controls.Add(tabPage4);
			resources.ApplyResources(this.tabSteps, "tabSteps");
			//
			// tabPage1
			//
			tabPage1.Controls.Add(this.languageSetup);
			tabPage1.Controls.Add(lblIdentifyLanguage);
			resources.ApplyResources(tabPage1, "tabPage1");
			tabPage1.Name = "tabPage1";
			this.helpProvider1.SetShowHelp(tabPage1, ((bool)(resources.GetObject("tabPage1.ShowHelp"))));
			//
			// languageSetup
			//
			this.languageSetup.EthnologueCode = "";
			this.languageSetup.LanguageName = "";
			resources.ApplyResources(this.languageSetup, "languageSetup");
			this.languageSetup.Name = "languageSetup";
			this.helpProvider1.SetShowHelp(this.languageSetup, ((bool)(resources.GetObject("languageSetup.ShowHelp"))));
			this.languageSetup.StartedInModifyState = false;
			this.languageSetup.LanguageNameChanged += new System.EventHandler(this.languageSetup_LanguageNameChanged);
			//
			// lblIdentifyLanguage
			//
			resources.ApplyResources(lblIdentifyLanguage, "lblIdentifyLanguage");
			lblIdentifyLanguage.Name = "lblIdentifyLanguage";
			this.helpProvider1.SetShowHelp(lblIdentifyLanguage, ((bool)(resources.GetObject("lblIdentifyLanguage.ShowHelp"))));
			//
			// lblAbbrev
			//
			resources.ApplyResources(lblAbbrev, "lblAbbrev");
			lblAbbrev.Name = "lblAbbrev";
			this.helpProvider1.SetShowHelp(lblAbbrev, ((bool)(resources.GetObject("lblAbbrev.ShowHelp"))));
			//
			// lblWsAbbrevInstructions
			//
			resources.ApplyResources(lblWsAbbrevInstructions, "lblWsAbbrevInstructions");
			lblWsAbbrevInstructions.Name = "lblWsAbbrevInstructions";
			this.helpProvider1.SetShowHelp(lblWsAbbrevInstructions, ((bool)(resources.GetObject("lblWsAbbrevInstructions.ShowHelp"))));
			//
			// lblWritingSystemAbbrev
			//
			resources.ApplyResources(lblWritingSystemAbbrev, "lblWritingSystemAbbrev");
			lblWritingSystemAbbrev.Name = "lblWritingSystemAbbrev";
			this.helpProvider1.SetShowHelp(lblWritingSystemAbbrev, ((bool)(resources.GetObject("lblWritingSystemAbbrev.ShowHelp"))));
			//
			// tabPage2
			//
			tabPage2.Controls.Add(lblRegionVariantInfo);
			tabPage2.Controls.Add(this.lblMoreInfoDesc);
			tabPage2.Controls.Add(this.m_regionVariantControl);
			tabPage2.Controls.Add(this.btnAdvanced);
			tabPage2.Controls.Add(this.chkUsesIPA);
			tabPage2.Controls.Add(lblRegionVariantInfoDesc);
			tabPage2.Controls.Add(this.m_ShortWsName);
			tabPage2.Controls.Add(lblAbbrev);
			tabPage2.Controls.Add(lblWsAbbrevInstructions);
			tabPage2.Controls.Add(lblWritingSystemAbbrev);
			resources.ApplyResources(tabPage2, "tabPage2");
			tabPage2.Name = "tabPage2";
			this.helpProvider1.SetShowHelp(tabPage2, ((bool)(resources.GetObject("tabPage2.ShowHelp"))));
			//
			// lblRegionVariantInfo
			//
			resources.ApplyResources(lblRegionVariantInfo, "lblRegionVariantInfo");
			lblRegionVariantInfo.Name = "lblRegionVariantInfo";
			this.helpProvider1.SetShowHelp(lblRegionVariantInfo, ((bool)(resources.GetObject("lblRegionVariantInfo.ShowHelp"))));
			//
			// lblMoreInfoDesc
			//
			resources.ApplyResources(this.lblMoreInfoDesc, "lblMoreInfoDesc");
			this.lblMoreInfoDesc.Name = "lblMoreInfoDesc";
			this.helpProvider1.SetShowHelp(this.lblMoreInfoDesc, ((bool)(resources.GetObject("lblMoreInfoDesc.ShowHelp"))));
			//
			// m_regionVariantControl
			//
			this.m_regionVariantControl.BackColor = System.Drawing.SystemColors.Control;
			this.m_regionVariantControl.LangDef = null;
			resources.ApplyResources(this.m_regionVariantControl, "m_regionVariantControl");
			this.m_regionVariantControl.Name = "m_regionVariantControl";
			this.m_regionVariantControl.PropDlg = false;
			this.helpProvider1.SetShowHelp(this.m_regionVariantControl, ((bool)(resources.GetObject("m_regionVariantControl.ShowHelp"))));
			this.m_regionVariantControl.VariantName = "";
			//
			// btnAdvanced
			//
			this.helpProvider1.SetHelpString(this.btnAdvanced, resources.GetString("btnAdvanced.HelpString"));
			resources.ApplyResources(this.btnAdvanced, "btnAdvanced");
			this.btnAdvanced.Name = "btnAdvanced";
			this.helpProvider1.SetShowHelp(this.btnAdvanced, ((bool)(resources.GetObject("btnAdvanced.ShowHelp"))));
			this.btnAdvanced.Click += new System.EventHandler(this.btnAdvanced_Click);
			//
			// chkUsesIPA
			//
			resources.ApplyResources(this.chkUsesIPA, "chkUsesIPA");
			this.helpProvider1.SetHelpString(this.chkUsesIPA, resources.GetString("chkUsesIPA.HelpString"));
			this.chkUsesIPA.Name = "chkUsesIPA";
			this.helpProvider1.SetShowHelp(this.chkUsesIPA, ((bool)(resources.GetObject("chkUsesIPA.ShowHelp"))));
			this.chkUsesIPA.CheckedChanged += new System.EventHandler(this.chkUsesIPA_CheckedChanged);
			//
			// lblRegionVariantInfoDesc
			//
			resources.ApplyResources(lblRegionVariantInfoDesc, "lblRegionVariantInfoDesc");
			lblRegionVariantInfoDesc.Name = "lblRegionVariantInfoDesc";
			this.helpProvider1.SetShowHelp(lblRegionVariantInfoDesc, ((bool)(resources.GetObject("lblRegionVariantInfoDesc.ShowHelp"))));
			//
			// m_ShortWsName
			//
			this.helpProvider1.SetHelpString(this.m_ShortWsName, resources.GetString("m_ShortWsName.HelpString"));
			resources.ApplyResources(this.m_ShortWsName, "m_ShortWsName");
			this.m_ShortWsName.Name = "m_ShortWsName";
			this.helpProvider1.SetShowHelp(this.m_ShortWsName, ((bool)(resources.GetObject("m_ShortWsName.ShowHelp"))));
			this.m_ShortWsName.TextChanged += new System.EventHandler(this.m_ShortWsName_TextChanged);
			//
			// tabPage4
			//
			tabPage4.Controls.Add(label2);
			tabPage4.Controls.Add(lblFinishOrBack);
			tabPage4.Controls.Add(this.lblConverterDesc);
			tabPage4.Controls.Add(this.btnEncodingConverterNew);
			tabPage4.Controls.Add(this.cbEncodingConverter);
			tabPage4.Controls.Add(this.lblEncodingConverter);
			tabPage4.Controls.Add(this.m_KeyboardControl);
			tabPage4.Controls.Add(lblKeyboardDesc);
			tabPage4.Controls.Add(lblKeyboard);
			resources.ApplyResources(tabPage4, "tabPage4");
			tabPage4.Name = "tabPage4";
			this.helpProvider1.SetShowHelp(tabPage4, ((bool)(resources.GetObject("tabPage4.ShowHelp"))));
			//
			// label2
			//
			resources.ApplyResources(label2, "label2");
			label2.Name = "label2";
			this.helpProvider1.SetShowHelp(label2, ((bool)(resources.GetObject("label2.ShowHelp"))));
			//
			// lblFinishOrBack
			//
			resources.ApplyResources(lblFinishOrBack, "lblFinishOrBack");
			lblFinishOrBack.Name = "lblFinishOrBack";
			this.helpProvider1.SetShowHelp(lblFinishOrBack, ((bool)(resources.GetObject("lblFinishOrBack.ShowHelp"))));
			//
			// lblConverterDesc
			//
			resources.ApplyResources(this.lblConverterDesc, "lblConverterDesc");
			this.lblConverterDesc.Name = "lblConverterDesc";
			this.helpProvider1.SetShowHelp(this.lblConverterDesc, ((bool)(resources.GetObject("lblConverterDesc.ShowHelp"))));
			//
			// btnEncodingConverterNew
			//
			this.helpProvider1.SetHelpString(this.btnEncodingConverterNew, resources.GetString("btnEncodingConverterNew.HelpString"));
			resources.ApplyResources(this.btnEncodingConverterNew, "btnEncodingConverterNew");
			this.btnEncodingConverterNew.Name = "btnEncodingConverterNew";
			this.helpProvider1.SetShowHelp(this.btnEncodingConverterNew, ((bool)(resources.GetObject("btnEncodingConverterNew.ShowHelp"))));
			this.btnEncodingConverterNew.Click += new System.EventHandler(this.btnEncodingConverterNew_Click);
			//
			// cbEncodingConverter
			//
			this.cbEncodingConverter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.helpProvider1.SetHelpString(this.cbEncodingConverter, resources.GetString("cbEncodingConverter.HelpString"));
			resources.ApplyResources(this.cbEncodingConverter, "cbEncodingConverter");
			this.cbEncodingConverter.Name = "cbEncodingConverter";
			this.helpProvider1.SetShowHelp(this.cbEncodingConverter, ((bool)(resources.GetObject("cbEncodingConverter.ShowHelp"))));
			this.cbEncodingConverter.Sorted = true;
			this.cbEncodingConverter.SelectedIndexChanged += new System.EventHandler(this.cbEncodingConverter_SelectedIndexChanged);
			//
			// lblEncodingConverter
			//
			resources.ApplyResources(this.lblEncodingConverter, "lblEncodingConverter");
			this.lblEncodingConverter.Name = "lblEncodingConverter";
			this.helpProvider1.SetShowHelp(this.lblEncodingConverter, ((bool)(resources.GetObject("lblEncodingConverter.ShowHelp"))));
			//
			// m_KeyboardControl
			//
			this.m_KeyboardControl.LangDef = null;
			resources.ApplyResources(this.m_KeyboardControl, "m_KeyboardControl");
			this.m_KeyboardControl.Name = "m_KeyboardControl";
			this.helpProvider1.SetShowHelp(this.m_KeyboardControl, ((bool)(resources.GetObject("m_KeyboardControl.ShowHelp"))));
			//
			// lblKeyboardDesc
			//
			resources.ApplyResources(lblKeyboardDesc, "lblKeyboardDesc");
			lblKeyboardDesc.Name = "lblKeyboardDesc";
			this.helpProvider1.SetShowHelp(lblKeyboardDesc, ((bool)(resources.GetObject("lblKeyboardDesc.ShowHelp"))));
			//
			// lblKeyboard
			//
			resources.ApplyResources(lblKeyboard, "lblKeyboard");
			lblKeyboard.Name = "lblKeyboard";
			this.helpProvider1.SetShowHelp(lblKeyboard, ((bool)(resources.GetObject("lblKeyboard.ShowHelp"))));
			//
			// lblDirectionDesc
			//
			resources.ApplyResources(lblDirectionDesc, "lblDirectionDesc");
			lblDirectionDesc.Name = "lblDirectionDesc";
			this.helpProvider1.SetShowHelp(lblDirectionDesc, ((bool)(resources.GetObject("lblDirectionDesc.ShowHelp"))));
			//
			// lblDirection
			//
			resources.ApplyResources(lblDirection, "lblDirection");
			lblDirection.Name = "lblDirection";
			this.helpProvider1.SetShowHelp(lblDirection, ((bool)(resources.GetObject("lblDirection.ShowHelp"))));
			//
			// lblDefaultFonts
			//
			resources.ApplyResources(lblDefaultFonts, "lblDefaultFonts");
			lblDefaultFonts.Name = "lblDefaultFonts";
			this.helpProvider1.SetShowHelp(lblDefaultFonts, ((bool)(resources.GetObject("lblDefaultFonts.ShowHelp"))));
			//
			// lblSimilarWSDesc
			//
			resources.ApplyResources(lblSimilarWSDesc, "lblSimilarWSDesc");
			lblSimilarWSDesc.Name = "lblSimilarWSDesc";
			this.helpProvider1.SetShowHelp(lblSimilarWSDesc, ((bool)(resources.GetObject("lblSimilarWSDesc.ShowHelp"))));
			//
			// lblSimilarWS
			//
			resources.ApplyResources(lblSimilarWS, "lblSimilarWS");
			lblSimilarWS.Name = "lblSimilarWS";
			this.helpProvider1.SetShowHelp(lblSimilarWS, ((bool)(resources.GetObject("lblSimilarWS.ShowHelp"))));
			//
			// tabPage3
			//
			tabPage3.Controls.Add(label1);
			tabPage3.Controls.Add(lblFontDesc);
			tabPage3.Controls.Add(this.m_defaultFontsControl);
			tabPage3.Controls.Add(this.radioRTL);
			tabPage3.Controls.Add(this.radioLTR);
			tabPage3.Controls.Add(lblDirectionDesc);
			tabPage3.Controls.Add(lblDirection);
			tabPage3.Controls.Add(lblDefaultFonts);
			tabPage3.Controls.Add(this.m_localeMenuButton);
			tabPage3.Controls.Add(lblSimilarWSDesc);
			tabPage3.Controls.Add(lblSimilarWS);
			resources.ApplyResources(tabPage3, "tabPage3");
			tabPage3.Name = "tabPage3";
			this.helpProvider1.SetShowHelp(tabPage3, ((bool)(resources.GetObject("tabPage3.ShowHelp"))));
			//
			// label1
			//
			resources.ApplyResources(label1, "label1");
			label1.Name = "label1";
			this.helpProvider1.SetShowHelp(label1, ((bool)(resources.GetObject("label1.ShowHelp"))));
			//
			// lblFontDesc
			//
			resources.ApplyResources(lblFontDesc, "lblFontDesc");
			lblFontDesc.Name = "lblFontDesc";
			this.helpProvider1.SetShowHelp(lblFontDesc, ((bool)(resources.GetObject("lblFontDesc.ShowHelp"))));
			//
			// m_defaultFontsControl
			//
			this.m_defaultFontsControl.DefaultHeadingFont = "";
			this.m_defaultFontsControl.DefaultNormalFont = "";
			this.m_defaultFontsControl.DefaultPublicationFont = "";
			this.m_defaultFontsControl.LangDef = null;
			resources.ApplyResources(this.m_defaultFontsControl, "m_defaultFontsControl");
			this.m_defaultFontsControl.Name = "m_defaultFontsControl";
			this.helpProvider1.SetShowHelp(this.m_defaultFontsControl, ((bool)(resources.GetObject("m_defaultFontsControl.ShowHelp"))));
			//
			// radioRTL
			//
			resources.ApplyResources(this.radioRTL, "radioRTL");
			this.helpProvider1.SetHelpString(this.radioRTL, resources.GetString("radioRTL.HelpString"));
			this.radioRTL.Name = "radioRTL";
			this.helpProvider1.SetShowHelp(this.radioRTL, ((bool)(resources.GetObject("radioRTL.ShowHelp"))));
			//
			// radioLTR
			//
			this.radioLTR.Checked = true;
			resources.ApplyResources(this.radioLTR, "radioLTR");
			this.helpProvider1.SetHelpString(this.radioLTR, resources.GetString("radioLTR.HelpString"));
			this.radioLTR.Name = "radioLTR";
			this.helpProvider1.SetShowHelp(this.radioLTR, ((bool)(resources.GetObject("radioLTR.ShowHelp"))));
			this.radioLTR.TabStop = true;
			this.radioLTR.CheckedChanged += new System.EventHandler(this.radioLTR_CheckedChanged);
			//
			// m_localeMenuButton
			//
			this.m_localeMenuButton.DisplayLocaleId = null;
			this.helpProvider1.SetHelpString(this.m_localeMenuButton, resources.GetString("m_localeMenuButton.HelpString"));
			resources.ApplyResources(this.m_localeMenuButton, "m_localeMenuButton");
			this.m_localeMenuButton.Name = "m_localeMenuButton";
			this.m_localeMenuButton.SelectedLocaleId = null;
			this.helpProvider1.SetShowHelp(this.m_localeMenuButton, ((bool)(resources.GetObject("m_localeMenuButton.ShowHelp"))));
			//
			// WritingSystemWizard
			//
			this.AcceptButton = null;
			resources.ApplyResources(this, "$this");
			this.Name = "WritingSystemWizard";
			this.helpProvider1.SetShowHelp(this, ((bool)(resources.GetObject("$this.ShowHelp"))));
			this.ShowInTaskbar = false;
			this.StepNames = new string[] {
		resources.GetString("$this.StepNames"),
		resources.GetString("$this.StepNames1"),
		resources.GetString("$this.StepNames2"),
		resources.GetString("$this.StepNames3")};
			this.StepPageCount = 4;
			this.Closing += new System.ComponentModel.CancelEventHandler(this.WritingSystemWizard_Closing);
			this.tabSteps.ResumeLayout(false);
			tabPage1.ResumeLayout(false);
			tabPage2.ResumeLayout(false);
			tabPage2.PerformLayout();
			tabPage4.ResumeLayout(false);
			tabPage3.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		#region Event Delegates
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void languageSetup_LanguageNameChanged(object sender, System.EventArgs e)
		{
			m_langDef.LocaleName = languageSetup.LanguageName;
			if (languageSetup.LanguageName != string.Empty)
			{
				NextButtonEnabled = true;
				AcceptButton = m_btnNext;
			}
			else
			{
				NextButtonEnabled = false;
				AcceptButton = null;
			}
		}
		#endregion

		/// <summary>
		/// Return the first cch characters of the input (or as many as are available).
		/// </summary>
		/// <param name="input"></param>
		/// <param name="cch"></param>
		/// <returns></returns>
		protected string LeftSubstring(string input, int cch)
		{
			return input.Substring(0, Math.Min(input.Length, cch));
		}

		#region Overrides of WizardDialog

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Check if we can go to next tab.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected override bool ValidToGoForward()
		{
			if (CurrentStepNumber != 1)
				return true;

			// Don't leave the region/variant page if we don't have valid data.
			if (!m_regionVariantControl.CheckValid())
				return false;

			string caption = FwCoreDlgs.kstidNwsCaption;
			string strLoc = m_langDef.WritingSystem.IcuLocale;

			// Catch case where we are going to overwrite an existing writing system in the Db.
			if (m_langDef.IsWritingSystemInDb())
			{
				ILgWritingSystemFactory wsf = m_langDef.XmlWritingSystem.WritingSystem.WritingSystemFactory;
				int defWs = wsf.UserWs;
				int ws = wsf.GetWsFromStr(strLoc);
				IWritingSystem qws = wsf.get_EngineOrNull(ws);
				string strDispName = qws.get_UiName(defWs);

				string msg = string.Format(FwCoreDlgs.kstidCantOverwriteWsInDb,
					strDispName, strLoc, Environment.NewLine, m_langDef.DisplayName);
				MessageBox.Show(msg, caption, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return false;
			}

			// Catch case where we are going to overwrite an existing LD.xml file.
			// This should be avoided by the callers to this dialog, but just in case, we'll
			// handle it here as well.
			if (m_langDef.IsLocaleInLanguagesDir())
			{
				string msg = string.Format(FwCoreDlgs.kstidLocaleAlreadyInLanguages,
					m_langDef.DisplayName, m_langDef.WritingSystem.IcuLocale, Environment.NewLine);
				DialogResult dr = MessageBox.Show(msg, FwCoreDlgs.ksWsAlreadyExists, MessageBoxButtons.YesNo,
					MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
				// If the user cancels, we don't leave the dialog.
				if (dr == DialogResult.Yes)
				{
					// We need to load the existing LD.xml file and then write it out to the
					// database, overwriting the original writing system. We then close the wizard.
					try
					{
						m_langDef = null;
						LanguageDefinitionFactory ldf = new LanguageDefinitionFactory();
						m_langDef = ldf.InitializeFromXml(m_wsf, strLoc) as LanguageDefinition;
						Debug.Assert(m_langDef != null);
						if (m_langDef != null)
						{
							m_langDef.SaveWritingSystem(strLoc);
						}
						DialogResult = DialogResult.OK;
						Visible = false;
					}
					catch
					{
						MessageBox.Show(FwCoreDlgs.kstidErrorSavingWs, caption,
							MessageBoxButtons.OK, MessageBoxIcon.Error);
						DialogResult = DialogResult.Cancel;
						Visible = false;
					}
				}
				return false;
			}
			return true;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Save the changes we made to the LD.xml file and the database.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected override void OnFinishButton()
		{
			using (new WaitCursor(this))
			{
				base.OnFinishButton();

				try
				{
					// Let go of ICU resource memory mapping so we can update ICU files.
					m_langDef.ReleaseRootRb();

					//If ICU has a set of ExemplarCharacters for this language then load it.
					ExemplarCharactersHelper.TryLoadValidCharsIfEmpty(m_langDef,
						LgIcuCharPropEngineClass.Create());

					// Save all changes and exit normally.
					m_langDef.Serialize();

					m_langDef.SaveWritingSystem(m_langDef.WritingSystem.IcuLocale);
				}
				catch (Exception ex)
				{
					// The exception message is likely something like this:
					// Access to the path "C:\Program Files\FieldWorks\languages\fr.xml" is denied.
					System.Text.StringBuilder sb = new System.Text.StringBuilder(ex.Message);
					sb.Append(System.Environment.NewLine);
					sb.Append(FwCoreDlgs.kstidErrorInstallingWS);
					MessageBox.Show(this, sb.ToString(),
						FwCoreDlgs.kstidCannotInstallWS);
					DialogResult = DialogResult.Cancel;
				}
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Cancel the wizard, cleaning up whatever is needed.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected override void OnCancelButton()
		{
			base.OnCancelButton();
			// Let go of ICU resource memory mapping so we can update ICU files.
			m_langDef.ReleaseRootRb();
			m_langDef.ReleaseLangDefRb();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Moving backwards one tab.  Don't leave Next button disabled unnecessarily.
		/// (See LT-9016.)
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected override void OnBackButton()
		{
			base.OnBackButton();
			switch(tabSteps.SelectedIndex)
			{
				case 0:
					if (String.IsNullOrEmpty(languageSetup.LanguageName))
					{
						NextButtonEnabled = false;
						AcceptButton = null;
					}
					else
					{
						NextButtonEnabled = true;
						AcceptButton = m_btnNext;
					}
					break;
				case 1:
					NextButtonEnabled = m_ShortWsName.Text.Length > 0;
					break;
				case 2:
					NextButtonEnabled = true;
					break;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Moving from one tab to another.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected override void OnNextButton()
		{
			base.OnNextButton();
			switch(tabSteps.SelectedIndex)
			{
				case 1: // moving from language ID to 'Distinguish writing system'
					// Make up a default abbreviation if empty.
					if (m_ShortWsName.Text == "")
						m_ShortWsName.Text = LeftSubstring(m_langDef.LocaleName, 3);
					// This has side effects of setting the locale abbr. It also checks for null
					// and empty.
					m_langDef.SetEthnologueCode(languageSetup.EthnologueCode,
						languageSetup.LanguageName.Trim());
					m_langDef.WritingSystem.set_Name(m_UserWs,
						languageSetup.LanguageName.Trim());

					break;

				case 2:
					try
					{
						// Try to get an initial value, but don't overwrite a possible
						// setting!  See LT-4045.
						string icuLocale = m_langDef.XmlWritingSystem.ICULocale.str;
						int lcid = Icu.GetLCID(icuLocale);
						// ICU can return lcids with the sublangid field set to zero
						// ("neutral").  However, Windows requires at least "default" (1**10)
						// in that field in order to switch keyboards.
						if ((uint)lcid < 1024)
							lcid += 1024;
						if (m_langDef.WritingSystem.Locale == 0 && lcid != 0)
							m_langDef.WritingSystem.Locale = lcid;
					}
					catch {}

					break;

				case 3: // Moving from Appearance step to Input step
					m_KeyboardControl.InitLanguageCombo();
					m_KeyboardControl.InitKeymanCombo();
					LoadAvailableConverters();
					break;
			}
		}

		private void m_ShortWsName_TextChanged(object sender, System.EventArgs e)
		{
			m_langDef.WritingSystem.set_Abbr(m_UserWs, m_ShortWsName.Text.Trim());
			NextButtonEnabled = m_ShortWsName.Text.Length > 0;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// This is called when the whole dialog loads. We use the SelectedIndexChanged
		/// thing to load each form as it becomes visible.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			// Initialize Page 2: abbreviation, region, variant.
			btnAdvanced.Image = ResourceHelper.MoreButtonDoubleArrowIcon;
			m_AdvancedButtonText = btnAdvanced.Text;
			m_langDef.FullCodeChanged += new EventHandler(m_langDef_FullCodeChanged);

			// Initialize Page 3: locale, fonts, and direction
			m_localeMenuButton.Text = FwCoreDlgs.kstidNone;
			this.m_localeMenuButton.LocaleSelected +=
				new System.EventHandler(this.m_localeMenuButton_LocaleSelected);
			m_defaultFontsControl.LangDef = m_langDef;
			m_defaultFontsControl.DefaultNormalFont = "Times New Roman";
			m_langDef.WritingSystem.DefaultSerif = "Times New Roman";
			m_defaultFontsControl.DefaultHeadingFont = "Arial";
			m_langDef.WritingSystem.DefaultSansSerif = "Arial";
			m_defaultFontsControl.DefaultPublicationFont = "Charis SIL";
			m_langDef.WritingSystem.DefaultBodyFont = "Charis SIL";

			// Initialize Page 4: keyboards and encoding converter.
			m_langDef.WritingSystem.Locale = 0;		// Start off with "Invalid Keyboard".
			m_KeyboardControl.LangDef = m_langDef;
		}
		#endregion //Overrides of WizardDialog

		private void btnAdvanced_Click(object sender, System.EventArgs e)
		{
			btnAdvanced.Text = FwCoreDlgs.kstidWsPropsCloseAdvancedButtonText;
			btnAdvanced.Image = ResourceHelper.LessButtonDoubleArrowIcon;
			btnAdvanced.Click -= new EventHandler(btnAdvanced_Click);
			btnAdvanced.Click += new EventHandler(btnCloseAdvanced_Click);
			m_regionVariantControl.Visible = true;
			lblMoreInfoDesc.Visible = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Show fewer options.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void btnCloseAdvanced_Click(object sender, System.EventArgs e)
		{
			btnAdvanced.Text = m_AdvancedButtonText;
			btnAdvanced.Image = ResourceHelper.MoreButtonDoubleArrowIcon;
			btnAdvanced.Click += new EventHandler(btnAdvanced_Click);
			btnAdvanced.Click -= new EventHandler(btnCloseAdvanced_Click);
			m_regionVariantControl.Visible = false;
			lblMoreInfoDesc.Visible = false;
		}

		private void chkUsesIPA_CheckedChanged(object sender, System.EventArgs e)
		{
			// Enhance: verify that SILDoulosIPA is installed...what if not?
			// TODO: The font should be "Doulos SIL" once it becomes available.
			string strIPAFont = "Doulos SIL";
			if (chkUsesIPA.Checked)
			{
				// First, set the IPA font in the writing system, and in the comboboxes on the
				// next wizard page.
				m_langDef.WritingSystem.DefaultSerif = strIPAFont;
				m_langDef.WritingSystem.DefaultSansSerif = strIPAFont;
				m_defaultFontsControl.DefaultNormalFont = strIPAFont;
				m_defaultFontsControl.DefaultHeadingFont = strIPAFont;

				m_langDef.LocaleVariant = "Phonetic";
				m_langDef.VariantAbbr = "X_ETIC";
				m_regionVariantControl.VariantName = "Phonetic";

				// TODO: enter something suitable as the default keyboard
				// verify that it is installed.
				// Shriek if it isn't.
				m_langDef.WritingSystem.RightToLeft = false;
			}
			else
			{
				if (m_langDef.LocaleVariant == "Phonetic")
				{
					m_langDef.LocaleVariant = "";
					m_regionVariantControl.VariantName = "";
				}
				if (m_langDef.VariantAbbr == "X_ETIC")
					m_langDef.VariantAbbr = "";
				if (m_defaultFontsControl.DefaultNormalFont == strIPAFont)
				{
					m_defaultFontsControl.DefaultNormalFont = "Times New Roman";
					m_langDef.WritingSystem.DefaultSerif = "Times New Roman";
				}
				if (m_defaultFontsControl.DefaultHeadingFont == strIPAFont)
				{
					m_defaultFontsControl.DefaultHeadingFont = "Arial";
					m_langDef.WritingSystem.DefaultSansSerif = "Arial";
				}
			}
		}

		private void m_localeMenuButton_LocaleSelected(object sender, EventArgs e)
		{
			m_langDef.BaseLocale = m_localeMenuButton.SelectedLocaleId;
			int lcid = Icu.GetLCID(m_langDef.BaseLocale);
			m_langDef.WritingSystem.Locale = lcid;
			StoreInheritedCollation();
		}

		private void StoreInheritedCollation()
		{
			ICollation coll = null;
			int ccoll = m_langDef.WritingSystem.CollationCount;
			if (ccoll > 0)
				coll = m_langDef.WritingSystem.get_Collation(0);
			if (coll == null)
			{
				coll = CollationClass.Create();
				m_langDef.WritingSystem.set_Collation(0, coll);
			}
			coll.LoadIcuRules(m_langDef.BaseLocale);
		}

		/// <summary>
		/// When the full code changes, we may need to disable the base locale button,
		/// which is allowed only if the locale is a custom one.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void m_langDef_FullCodeChanged(object sender, EventArgs e)
		{
			m_localeMenuButton.SetupForSimilarLocale(m_langDef.CurrentFullLocale(),
				m_langDef.RootRb);
		}

		private void radioLTR_CheckedChanged(object sender, System.EventArgs e)
		{
			m_langDef.WritingSystem.RightToLeft = radioRTL.Checked;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Show New Converter Dialog.  Copied from FwCoreDlgs.WritingSystemPropertiesDialog.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void btnEncodingConverterNew_Click(object sender, System.EventArgs e)
		{
			EncConverters converters = new EncConverters();
			if (converters == null)
			{
				MessageBox.Show(this, FwCoreDlgs.kstidErrorAccessingEncConverters,
					ResourceHelper.GetResourceString("kstidCannotModifyWS"));
				return;
			}
			Debug.Assert(converters != null);

			// Make a set of the writing systems currently in use.
			// As of 12/2/06, the only real user of wsInUse
			// Only cared about the legMapping, and treated
			// wsInUse as a Set.
			// Dictionary<string, string> wsInUse = new Dictionary<string, string>();
			Set<string> wsInUse = new Set<string>();
			IWritingSystem ws;
			int wsUser = m_wsf.UserWs;
			int cws = m_wsf.NumberOfWs;

			using (ArrayPtr ptr = MarshalEx.ArrayToNative(cws, typeof(int)))
			{
				m_wsf.GetWritingSystems(ptr, cws);
				int[] vws = (int[])MarshalEx.NativeToArray(ptr, cws, typeof(int));

				for (int iws = 0; iws < cws; iws++)
				{
					if (vws[iws] == 0)
						continue;
					ws = m_wsf.get_EngineOrNull(vws[iws]);
					if (ws == null)
						continue;
					string legMapping = ws.LegacyMapping;
					if (legMapping == null)
						continue;
					wsInUse.Add(legMapping);
				}
			}

			try
			{
				string prevEC = cbEncodingConverter.Text;
				using (AddCnvtrDlg dlg = new AddCnvtrDlg(m_helpTopicProvider, null,
					cbEncodingConverter.Text, null, false))
				{
					dlg.ShowDialog();

					// Regenerate the encoding converters list and reload it in the combobox
					// to reflect any changes from the dialog.
					LoadAvailableConverters();

					// Either select the new one or select the old one
					if (dlg.DialogResult == DialogResult.OK && !String.IsNullOrEmpty(dlg.SelectedConverter))
						cbEncodingConverter.SelectedItem = dlg.SelectedConverter;
					else if (cbEncodingConverter.Items.Count > 0)
						cbEncodingConverter.SelectedItem = prevEC; // preserve selection if possible
				}
			}
			catch (Exception ex)
			{
				System.Text.StringBuilder sb = new System.Text.StringBuilder(ex.Message);
				sb.Append(System.Environment.NewLine);
				sb.Append(FwCoreDlgs.kstidErrorAccessingEncConverters);
				MessageBox.Show(this, sb.ToString(),
					ResourceHelper.GetResourceString("kstidCannotModifyWS"));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load the Available Encoding Converters.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void LoadAvailableConverters()
		{
			string strSel = FwCoreDlgs.kstidNone;

			try
			{
				// Remember existing selection if one exists.  See LT-4045.
				if (cbEncodingConverter.Items.Count > 0)
					strSel = (string)cbEncodingConverter.SelectedItem;
				EncConverters encConverters = new EncConverters();
				cbEncodingConverter.Items.Clear();
				cbEncodingConverter.Items.Add(strSel);
				foreach (string convName in encConverters.Keys)
					cbEncodingConverter.Items.Add(convName);
				cbEncodingConverter.SelectedItem = strSel;
			}
			catch (Exception e)
			{
				Debug.WriteLine(e.Message);
				cbEncodingConverter.Items.Clear();
				cbEncodingConverter.Items.Add(strSel);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle changing the selection in the encoding converter combobox.
		/// Copied from FwCoreDlgs.WritingSystemPropertiesDialog.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void cbEncodingConverter_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			if (cbEncodingConverter.SelectedIndex >= 0)
			{
				string strNone = FwCoreDlgs.kstidNone;
				string str = cbEncodingConverter.Text;
				if (str == strNone)
					str = null;
				m_langDef.WritingSystem.LegacyMapping = str;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Closing event of the WritingSystemWizard control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.ComponentModel.CancelEventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void WritingSystemWizard_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			// Make sure we release COM when the dialog closes.
			this.m_langDef.ReleaseRootRb();
			this.m_langDef.ReleaseLangDefRb();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the m_btnHelp control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_btnHelp_Click(object sender, System.EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider,
				"khtpWsWizardStep" + (tabSteps.SelectedIndex + 1));
		}
	}
	#endregion //WritingSystemWizard dialog

	#region IWritingSystemWizard interface
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Interface for writing system properties dialog
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[ComVisible(true)]
	[Guid("E5E15BFD-0FED-4699-B16C-AFA88367234A")]
	public interface IWritingSystemWizard
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the dialog as a modal dialog
		/// </summary>
		/// <returns>A DialogResult value</returns>
		/// ------------------------------------------------------------------------------------
		int DisplayDialog();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// <param name="wsf">writing system factory</param>
		/// <param name="helpTopicProvider">Dialog properties for help topics</param>
		/// ------------------------------------------------------------------------------------
		void Init(ILgWritingSystemFactory wsf, IHelpTopicProvider helpTopicProvider);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve the writing system.
		/// </summary>
		/// <returns>An IWritingSystem object</returns>
		/// ------------------------------------------------------------------------------------
		IWritingSystem WritingSystem();
	}
	#endregion //IWritingSystemWizard interface
}

// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2004' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ScriptureProperties.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.Diagnostics;
using System.Text;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO.Infrastructure;
using SILUBS.SharedScrUtils;
using XCore;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for ScriptureProperties.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ScriptureProperties : Form, IFWDisposable
	{
		#region Member variables
		/// <summary>Index of the tab for user properties account</summary>
		public const int kChapterVerseNumTab = 0;
		/// <summary>Index of the tab for normal footnotes</summary>
		public const int kFootnotesTab = 1;

		private FdoCache m_cache;
		private IHelpTopicProvider m_helpTopicProvider;
		private IScripture m_scr;
		private IVwStylesheet m_styleSheet;
		private TabControl tabControl1;
		private TabPage tpgChapVrsNumbers;
		private TabPage tpgFootnotes;
		private RadioButton m_btnLatinNumbers;
		private RadioButton m_btnScriptNumbers;
		private FwOverrideComboBox m_cboScriptLanguages;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private Container components = null;
		private CheckBox m_chkUseLatinNumsOnExport;

		/// <summary>maintains a map of language names to base digits.  The key is the language
		/// name and the values are the base digits ('0' character) for the language.</summary>
		private Dictionary<string, char> m_languageMap = new Dictionary<string, char>();
		private Dictionary<char, string> m_baseDigitMap = new Dictionary<char, string>();
		private Label lblRefSepr;
		private Label lblChapterVerseSep;
		private Label lblVerseBridge;
		private Label lblVerseSep;
		private TextBox m_txtRefSep;
		private TextBox m_txtChapterVerseSep;
		private TextBox m_txtVerseSep;
		private TextBox m_txtVerseBridge;
		private FootnotePropertiesSelector fpsFootnoteOptions;
		private GroupBox grpGeneralFootnotes;
		private GroupBox grpCrossRefFootnotes;
		private FootnotePropertiesSelector fpsCrossRefOptions;
		private RadioButton opnCombined;
		private RadioButton opnSeparate;
		private bool m_fCombineFootnotes;
		private Label label1;
		private FwOverrideComboBox m_cboVersification;
		private IRootSite m_rootSite;
		#endregion

		#region Constructors/Destructors/Init
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ScriptureProperties"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="styleSheet">The style sheet.</param>
		/// <param name="rootSite">The active view (or the draft view if the footnote view is
		/// active).</param>
		/// <param name="showFootnoteTab">True to show the footnote tab. Otherwise the
		/// footnote tab will be hidden.</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// ------------------------------------------------------------------------------------
		public ScriptureProperties(FdoCache cache, IVwStylesheet styleSheet, IRootSite rootSite,
			bool showFootnoteTab, IHelpTopicProvider helpTopicProvider)
		{
			m_cache = cache;
			m_helpTopicProvider = helpTopicProvider;
			m_styleSheet = styleSheet;
			m_scr = cache.LangProject.TranslatedScriptureOA;
			m_rootSite = rootSite;

			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			if (!showFootnoteTab)
				tabControl1.TabPages.Remove(tpgFootnotes);

			fpsCrossRefOptions.SiblingPropertiesSelector = fpsFootnoteOptions;
			fpsFootnoteOptions.SiblingPropertiesSelector = fpsCrossRefOptions;
			m_fCombineFootnotes = m_scr.CrossRefsCombinedWithFootnotes;
			opnCombined.Checked = m_fCombineFootnotes;
			opnSeparate.Checked = !m_fCombineFootnotes;
			UpdateCombinedFootnotes();

			FillScriptLanguages();
			FillVersificationSchemes();
			UpdateFootnoteTabs();

			if (m_scr.UseScriptDigits)
			{
				m_btnScriptNumbers.Checked = true;
				m_chkUseLatinNumsOnExport.Checked =
					m_scr.ConvertCVDigitsOnExport;
				string language = m_baseDigitMap[(char)m_scr.ScriptDigitZero];
				m_cboScriptLanguages.SelectedIndex =
					m_cboScriptLanguages.FindString(language);
			}
			else
			{
				m_btnLatinNumbers.Checked = true;
				m_chkUseLatinNumsOnExport.Checked = false;
				m_chkUseLatinNumsOnExport.Enabled = false;
			}

			// Fill in the reference separator fields to edit
			m_txtRefSep.Text = m_scr.RefSepr;
			m_txtChapterVerseSep.Text = m_scr.ChapterVerseSepr;
			m_txtVerseSep.Text = m_scr.VerseSepr;
			m_txtVerseBridge.Text = m_scr.Bridge;

			// Make the option button for footnotes and cross references invisible.
			fpsFootnoteOptions.ShowSequenceButton = false;
			fpsCrossRefOptions.ShowSequenceButton = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="id"></param>
		/// <param name="baseChar"></param>
		/// ------------------------------------------------------------------------------------
		private void LoadOneLanguage(string id, char baseChar)
		{
			string language = DlgResources.ResourceString(id);
			Debug.Assert(language != null);
			m_languageMap.Add(language, baseChar);
			m_baseDigitMap.Add(baseChar, language);
			m_cboScriptLanguages.Items.Add(language);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fills in the language map hash table and the combo box
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void FillScriptLanguages()
		{
			LoadOneLanguage("kstidArabicIndic", '\u0660');
			LoadOneLanguage("kstidExtArabicIndic", '\u06f0');
			LoadOneLanguage("kstidDevanagari", '\u0966');
			LoadOneLanguage("kstidBengali", '\u09e6');
			LoadOneLanguage("kstidGurmukhi", '\u0a66');
			LoadOneLanguage("kstidGujarati", '\u0ae6');
			LoadOneLanguage("kstidOriya", '\u0b66');
			// Tamil is not supported (does not have a 0 digit defined) so it is currently disabled.
//			LoadOneLanguage("kstidTamil", '\u0be6');
			LoadOneLanguage("kstidTelugu", '\u0c66');
			LoadOneLanguage("kstidKannada", '\u0ce6');
			LoadOneLanguage("kstidMalayalam", '\u0d66');
			LoadOneLanguage("kstidThai", '\u0e50');
			LoadOneLanguage("kstidLao", '\u0ed0');
			LoadOneLanguage("kstidTibetan", '\u0f20');
			LoadOneLanguage("kstidMyanmar", '\u1040');

			// Ethiopic is not supported in Uniscribe so it is currently disabled.
//			LoadOneLanguage("kstidEthiopic", '\u1369');
			LoadOneLanguage("kstidKhmer", '\u17e0');
			LoadOneLanguage("kstidMongolian", '\u1810');

			// TODO: Once the database has a setting, set the default language
			m_cboScriptLanguages.SelectedIndex = 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fills in the versification system combo box
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void FillVersificationSchemes()
		{
			for (int iVers = 1; iVers <= 6; iVers++)
			{
				ScrVers vers = (ScrVers)iVers;
				m_cboVersification.Items.Add(TeResourceHelper.GetResourceString(
					"kstid" + vers.ToString() + "Versification"));
				if (m_scr.Versification == vers)
					m_cboVersification.SelectedIndex = iVers - 1;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		/// ------------------------------------------------------------------------------------
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
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
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
			System.Windows.Forms.GroupBox grpReference;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ScriptureProperties));
			System.Windows.Forms.GroupBox groupBox1;
			System.Windows.Forms.Button m_btnOK;
			System.Windows.Forms.Button m_btnCancel;
			System.Windows.Forms.Button m_btnHelp;
			this.m_txtVerseBridge = new System.Windows.Forms.TextBox();
			this.m_txtVerseSep = new System.Windows.Forms.TextBox();
			this.m_txtChapterVerseSep = new System.Windows.Forms.TextBox();
			this.m_txtRefSep = new System.Windows.Forms.TextBox();
			this.lblVerseSep = new System.Windows.Forms.Label();
			this.lblVerseBridge = new System.Windows.Forms.Label();
			this.lblChapterVerseSep = new System.Windows.Forms.Label();
			this.lblRefSepr = new System.Windows.Forms.Label();
			this.m_cboScriptLanguages = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.m_btnScriptNumbers = new System.Windows.Forms.RadioButton();
			this.m_btnLatinNumbers = new System.Windows.Forms.RadioButton();
			this.m_chkUseLatinNumsOnExport = new System.Windows.Forms.CheckBox();
			this.label1 = new System.Windows.Forms.Label();
			this.m_cboVersification = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.tabControl1 = new System.Windows.Forms.TabControl();
			this.tpgChapVrsNumbers = new System.Windows.Forms.TabPage();
			this.tpgFootnotes = new System.Windows.Forms.TabPage();
			this.opnCombined = new System.Windows.Forms.RadioButton();
			this.opnSeparate = new System.Windows.Forms.RadioButton();
			this.grpCrossRefFootnotes = new System.Windows.Forms.GroupBox();
			this.fpsCrossRefOptions = new SIL.FieldWorks.TE.FootnotePropertiesSelector();
			this.grpGeneralFootnotes = new System.Windows.Forms.GroupBox();
			this.fpsFootnoteOptions = new SIL.FieldWorks.TE.FootnotePropertiesSelector();
			grpReference = new System.Windows.Forms.GroupBox();
			groupBox1 = new System.Windows.Forms.GroupBox();
			m_btnOK = new System.Windows.Forms.Button();
			m_btnCancel = new System.Windows.Forms.Button();
			m_btnHelp = new System.Windows.Forms.Button();
			grpReference.SuspendLayout();
			groupBox1.SuspendLayout();
			this.tabControl1.SuspendLayout();
			this.tpgChapVrsNumbers.SuspendLayout();
			this.tpgFootnotes.SuspendLayout();
			this.grpCrossRefFootnotes.SuspendLayout();
			this.grpGeneralFootnotes.SuspendLayout();
			this.SuspendLayout();
			//
			// grpReference
			//
			resources.ApplyResources(grpReference, "grpReference");
			grpReference.Controls.Add(this.m_txtVerseBridge);
			grpReference.Controls.Add(this.m_txtVerseSep);
			grpReference.Controls.Add(this.m_txtChapterVerseSep);
			grpReference.Controls.Add(this.m_txtRefSep);
			grpReference.Controls.Add(this.lblVerseSep);
			grpReference.Controls.Add(this.lblVerseBridge);
			grpReference.Controls.Add(this.lblChapterVerseSep);
			grpReference.Controls.Add(this.lblRefSepr);
			grpReference.Name = "grpReference";
			grpReference.TabStop = false;
			//
			// m_txtVerseBridge
			//
			resources.ApplyResources(this.m_txtVerseBridge, "m_txtVerseBridge");
			this.m_txtVerseBridge.Name = "m_txtVerseBridge";
			this.m_txtVerseBridge.TextChanged += new System.EventHandler(this.Edit_TextChanged);
			//
			// m_txtVerseSep
			//
			resources.ApplyResources(this.m_txtVerseSep, "m_txtVerseSep");
			this.m_txtVerseSep.Name = "m_txtVerseSep";
			this.m_txtVerseSep.TextChanged += new System.EventHandler(this.Edit_TextChanged);
			//
			// m_txtChapterVerseSep
			//
			resources.ApplyResources(this.m_txtChapterVerseSep, "m_txtChapterVerseSep");
			this.m_txtChapterVerseSep.Name = "m_txtChapterVerseSep";
			this.m_txtChapterVerseSep.TextChanged += new System.EventHandler(this.Edit_TextChanged);
			//
			// m_txtRefSep
			//
			resources.ApplyResources(this.m_txtRefSep, "m_txtRefSep");
			this.m_txtRefSep.Name = "m_txtRefSep";
			this.m_txtRefSep.TextChanged += new System.EventHandler(this.Edit_TextChanged);
			//
			// lblVerseSep
			//
			resources.ApplyResources(this.lblVerseSep, "lblVerseSep");
			this.lblVerseSep.Name = "lblVerseSep";
			//
			// lblVerseBridge
			//
			resources.ApplyResources(this.lblVerseBridge, "lblVerseBridge");
			this.lblVerseBridge.Name = "lblVerseBridge";
			//
			// lblChapterVerseSep
			//
			resources.ApplyResources(this.lblChapterVerseSep, "lblChapterVerseSep");
			this.lblChapterVerseSep.Name = "lblChapterVerseSep";
			//
			// lblRefSepr
			//
			resources.ApplyResources(this.lblRefSepr, "lblRefSepr");
			this.lblRefSepr.Name = "lblRefSepr";
			//
			// groupBox1
			//
			resources.ApplyResources(groupBox1, "groupBox1");
			groupBox1.Controls.Add(this.m_cboScriptLanguages);
			groupBox1.Controls.Add(this.m_btnScriptNumbers);
			groupBox1.Controls.Add(this.m_btnLatinNumbers);
			groupBox1.Controls.Add(this.m_chkUseLatinNumsOnExport);
			groupBox1.Name = "groupBox1";
			groupBox1.TabStop = false;
			//
			// m_cboScriptLanguages
			//
			this.m_cboScriptLanguages.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			resources.ApplyResources(this.m_cboScriptLanguages, "m_cboScriptLanguages");
			this.m_cboScriptLanguages.Name = "m_cboScriptLanguages";
			this.m_cboScriptLanguages.Sorted = true;
			this.m_cboScriptLanguages.SelectedIndexChanged += new System.EventHandler(this.m_cboScriptLanguages_SelectedIndexChanged);
			//
			// m_btnScriptNumbers
			//
			resources.ApplyResources(this.m_btnScriptNumbers, "m_btnScriptNumbers");
			this.m_btnScriptNumbers.Name = "m_btnScriptNumbers";
			//
			// m_btnLatinNumbers
			//
			resources.ApplyResources(this.m_btnLatinNumbers, "m_btnLatinNumbers");
			this.m_btnLatinNumbers.Name = "m_btnLatinNumbers";
			this.m_btnLatinNumbers.CheckedChanged += new System.EventHandler(this.m_btnLatinNumbers_CheckedChanged);
			//
			// m_chkUseLatinNumsOnExport
			//
			resources.ApplyResources(this.m_chkUseLatinNumsOnExport, "m_chkUseLatinNumsOnExport");
			this.m_chkUseLatinNumsOnExport.Name = "m_chkUseLatinNumsOnExport";
			//
			// m_btnOK
			//
			resources.ApplyResources(m_btnOK, "m_btnOK");
			m_btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			m_btnOK.Name = "m_btnOK";
			m_btnOK.Click += new System.EventHandler(this.m_btnOK_Click);
			//
			// m_btnCancel
			//
			resources.ApplyResources(m_btnCancel, "m_btnCancel");
			m_btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			m_btnCancel.Name = "m_btnCancel";
			//
			// m_btnHelp
			//
			resources.ApplyResources(m_btnHelp, "m_btnHelp");
			m_btnHelp.Name = "m_btnHelp";
			m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// label1
			//
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			//
			// m_cboVersification
			//
			this.m_cboVersification.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			resources.ApplyResources(this.m_cboVersification, "m_cboVersification");
			this.m_cboVersification.Name = "m_cboVersification";
			//
			// tabControl1
			//
			resources.ApplyResources(this.tabControl1, "tabControl1");
			this.tabControl1.Controls.Add(this.tpgChapVrsNumbers);
			this.tabControl1.Controls.Add(this.tpgFootnotes);
			this.tabControl1.HotTrack = true;
			this.tabControl1.Name = "tabControl1";
			this.tabControl1.SelectedIndex = 0;
			//
			// tpgChapVrsNumbers
			//
			this.tpgChapVrsNumbers.Controls.Add(this.label1);
			this.tpgChapVrsNumbers.Controls.Add(this.m_cboVersification);
			this.tpgChapVrsNumbers.Controls.Add(grpReference);
			this.tpgChapVrsNumbers.Controls.Add(groupBox1);
			resources.ApplyResources(this.tpgChapVrsNumbers, "tpgChapVrsNumbers");
			this.tpgChapVrsNumbers.Name = "tpgChapVrsNumbers";
			//
			// tpgFootnotes
			//
			this.tpgFootnotes.Controls.Add(this.opnCombined);
			this.tpgFootnotes.Controls.Add(this.opnSeparate);
			this.tpgFootnotes.Controls.Add(this.grpCrossRefFootnotes);
			this.tpgFootnotes.Controls.Add(this.grpGeneralFootnotes);
			resources.ApplyResources(this.tpgFootnotes, "tpgFootnotes");
			this.tpgFootnotes.Name = "tpgFootnotes";
			//
			// opnCombined
			//
			resources.ApplyResources(this.opnCombined, "opnCombined");
			this.opnCombined.Name = "opnCombined";
			this.opnCombined.UseVisualStyleBackColor = true;
			this.opnCombined.CheckedChanged += new System.EventHandler(this.opnCombined_CheckedChanged);
			//
			// opnSeparate
			//
			resources.ApplyResources(this.opnSeparate, "opnSeparate");
			this.opnSeparate.Checked = true;
			this.opnSeparate.Name = "opnSeparate";
			this.opnSeparate.TabStop = true;
			this.opnSeparate.UseVisualStyleBackColor = true;
			//
			// grpCrossRefFootnotes
			//
			this.grpCrossRefFootnotes.Controls.Add(this.fpsCrossRefOptions);
			resources.ApplyResources(this.grpCrossRefFootnotes, "grpCrossRefFootnotes");
			this.grpCrossRefFootnotes.Name = "grpCrossRefFootnotes";
			this.grpCrossRefFootnotes.TabStop = false;
			//
			// fpsCrossRefOptions
			//
			resources.ApplyResources(this.fpsCrossRefOptions, "fpsCrossRefOptions");
			this.fpsCrossRefOptions.Name = "fpsCrossRefOptions";
			this.fpsCrossRefOptions.ShowSequenceButton = false;
			//
			// grpGeneralFootnotes
			//
			this.grpGeneralFootnotes.Controls.Add(this.fpsFootnoteOptions);
			resources.ApplyResources(this.grpGeneralFootnotes, "grpGeneralFootnotes");
			this.grpGeneralFootnotes.Name = "grpGeneralFootnotes";
			this.grpGeneralFootnotes.TabStop = false;
			//
			// fpsFootnoteOptions
			//
			resources.ApplyResources(this.fpsFootnoteOptions, "fpsFootnoteOptions");
			this.fpsFootnoteOptions.Name = "fpsFootnoteOptions";
			this.fpsFootnoteOptions.ShowSequenceButton = false;
			//
			// ScriptureProperties
			//
			this.AcceptButton = m_btnOK;
			resources.ApplyResources(this, "$this");
			this.CancelButton = m_btnCancel;
			this.Controls.Add(m_btnHelp);
			this.Controls.Add(m_btnCancel);
			this.Controls.Add(m_btnOK);
			this.Controls.Add(this.tabControl1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ScriptureProperties";
			this.ShowInTaskbar = false;
			grpReference.ResumeLayout(false);
			grpReference.PerformLayout();
			groupBox1.ResumeLayout(false);
			this.tabControl1.ResumeLayout(false);
			this.tpgChapVrsNumbers.ResumeLayout(false);
			this.tpgFootnotes.ResumeLayout(false);
			this.tpgFootnotes.PerformLayout();
			this.grpCrossRefFootnotes.ResumeLayout(false);
			this.grpGeneralFootnotes.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		#region Event Handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a selection change in the script languages combo box
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_cboScriptLanguages_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			// when the selection changes, make sure that script languages radio button
			// is activated.
			m_btnScriptNumbers.Checked = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the OK button click
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_btnOK_Click(object sender, EventArgs e)
		{
			using (new WaitCursor(this))
			{
				string undo, redo;
				TeResourceHelper.MakeUndoRedoLabels("kstidUndoRedoScriptureProperties",
					out undo, out redo);

				UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(undo, redo,
					m_cache.ServiceLocator.GetInstance<IActionHandler>(), () =>
					{
						ProcessScriptNumberSettings();
						ProcessRefFormattingSettings();
						ProcessVersificationSettings();
						ProcessFootnoteSettings();
					});
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the script number settings.
		/// </summary>
		/// <returns><c>true</c> if script digits changed, <c>false</c> if nothing changed.</returns>
		/// ------------------------------------------------------------------------------------
		private bool ProcessScriptNumberSettings()
		{
			// get the settings from the dialog.
			bool useScriptNumbers = m_btnScriptNumbers.Checked;
			bool convertToLatinOnExport = m_chkUseLatinNumsOnExport.Checked;
			string languageName = (string)m_cboScriptLanguages.SelectedItem;
			char baseChar =
				useScriptNumbers ? m_languageMap[languageName] : '\0';

			// store the settings in the database
			if (m_scr.UseScriptDigits != useScriptNumbers ||
				m_scr.ScriptDigitZero != baseChar)
			{
				m_scr.UseScriptDigits = useScriptNumbers;
				if (useScriptNumbers)
				{
					m_scr.ScriptDigitZero = baseChar;
					m_scr.ConvertCVDigitsOnExport =
						convertToLatinOnExport;
				}
				else
				{
					m_scr.ScriptDigitZero = 0;
					m_scr.ConvertCVDigitsOnExport = false;
				}

				ConvertChapterVerseNumbers();
				return true;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the reference formatting characters settings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ProcessRefFormattingSettings()
		{
			// Save the reference formatting characters. If the bridge character changes, then
			// update all of the verse bridges in scripture
			bool rtl = m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.RightToLeftScript;
			string rtlMarker = rtl ? "\u200f" : string.Empty;

			string oldBridge = m_scr.Bridge;
			m_scr.RefSepr = m_txtRefSep.Text;
			m_scr.ChapterVerseSepr = m_txtChapterVerseSep.Text;
			m_scr.VerseSepr = m_txtVerseSep.Text;
			m_scr.Bridge = m_txtVerseBridge.Text;
			if (oldBridge != m_scr.Bridge)
				UpdateVerseBridges(oldBridge);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the versification settings.
		/// </summary>
		/// <returns><c>true</c> if versification settings changed, <c>false</c> if nothing
		/// changed.</returns>
		/// ------------------------------------------------------------------------------------
		private bool ProcessVersificationSettings()
		{
			ScrVers selectedVers =
				(ScrVers)(m_cboVersification.SelectedIndex + 1);

			if (m_scr.Versification != selectedVers)
			{
				m_scr.Versification = selectedVers;
				return true;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the footnote settings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ProcessFootnoteSettings()
		{
			bool fSettingsChanged =
				m_scr.RestartFootnoteSequence != fpsFootnoteOptions.RestartFootnoteSequence ||
				m_scr.FootnoteMarkerType != fpsFootnoteOptions.FootnoteMarkerType ||
				m_scr.FootnoteMarkerSymbol != fpsFootnoteOptions.FootnoteMarkerSymbol ||
				m_scr.DisplayFootnoteReference != fpsFootnoteOptions.ShowScriptureReference ||
				m_scr.CrossRefsCombinedWithFootnotes != m_fCombineFootnotes ||
				m_scr.DisplaySymbolInFootnote != fpsFootnoteOptions.ShowCustomSymbol;

			if (fpsCrossRefOptions.Enabled)
			{
				fSettingsChanged |=
					m_scr.CrossRefMarkerType != fpsCrossRefOptions.FootnoteMarkerType ||
					m_scr.CrossRefMarkerSymbol != fpsCrossRefOptions.FootnoteMarkerSymbol ||
					m_scr.DisplayCrossRefReference != fpsCrossRefOptions.ShowScriptureReference ||
					m_scr.DisplaySymbolInCrossRef != fpsCrossRefOptions.ShowCustomSymbol;
			}

			if (fSettingsChanged)
			{
				// If the resequence option has changed then the footnotes in the database
				// need to be recalculated.
				if (m_scr.RestartFootnoteSequence != fpsFootnoteOptions.RestartFootnoteSequence)
					m_scr.RestartFootnoteSequence = fpsFootnoteOptions.RestartFootnoteSequence;

				if (m_scr.FootnoteMarkerType != fpsFootnoteOptions.FootnoteMarkerType)
					m_scr.FootnoteMarkerType = fpsFootnoteOptions.FootnoteMarkerType;
				if (m_scr.FootnoteMarkerSymbol != fpsFootnoteOptions.FootnoteMarkerSymbol)
					m_scr.FootnoteMarkerSymbol = fpsFootnoteOptions.FootnoteMarkerSymbol;
				if (m_scr.DisplayFootnoteReference != fpsFootnoteOptions.ShowScriptureReference)
					m_scr.DisplayFootnoteReference = fpsFootnoteOptions.ShowScriptureReference;
				if (m_scr.DisplaySymbolInFootnote != fpsFootnoteOptions.ShowCustomSymbol)
					m_scr.DisplaySymbolInFootnote = fpsFootnoteOptions.ShowCustomSymbol;

				if (m_scr.CrossRefsCombinedWithFootnotes != m_fCombineFootnotes)
					m_scr.CrossRefsCombinedWithFootnotes = m_fCombineFootnotes;

				if (fpsCrossRefOptions.Enabled)
				{
					if (m_scr.CrossRefMarkerType != fpsCrossRefOptions.FootnoteMarkerType)
						m_scr.CrossRefMarkerType = fpsCrossRefOptions.FootnoteMarkerType;
					if (m_scr.CrossRefMarkerSymbol != fpsCrossRefOptions.FootnoteMarkerSymbol)
						m_scr.CrossRefMarkerSymbol = fpsCrossRefOptions.FootnoteMarkerSymbol;
					if (m_scr.DisplayCrossRefReference != fpsCrossRefOptions.ShowScriptureReference)
						m_scr.DisplayCrossRefReference = fpsCrossRefOptions.ShowScriptureReference;
					if (m_scr.DisplaySymbolInCrossRef != fpsCrossRefOptions.ShowCustomSymbol)
						m_scr.DisplaySymbolInCrossRef = fpsCrossRefOptions.ShowCustomSymbol;
				}

				//// Now restore the selection in the given root site
				//if (selHelper != null)
				//    selHelper.RestoreSelectionAndScrollPos();

				//// Now restore the selection in the active root site. We do this separately
				//// so that the IP gets put back in the footnote pane. We don't need this for
				//// undo/redo.
				//if (fInFootnotePane && activeSelHelper != null)
				//    activeSelHelper.RestoreSelectionAndScrollPos();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a change in the latin numbers check box
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_btnLatinNumbers_CheckedChanged(object sender, System.EventArgs e)
		{
			if (m_btnLatinNumbers.Checked)
			{
				m_chkUseLatinNumsOnExport.Enabled = false;
				m_chkUseLatinNumsOnExport.Checked = false;
				m_cboScriptLanguages.Enabled = false;
			}
			else
			{
				m_chkUseLatinNumsOnExport.Enabled = true;
				m_cboScriptLanguages.Enabled = true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Open the help window when the help button is pressed.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_btnHelp_Click(object sender, System.EventArgs e)
		{
			string helpTopicKey = null;
			switch (tabControl1.SelectedIndex)
			{
				case kChapterVerseNumTab:
					helpTopicKey = "khtpScrProp-ChapterVerseNum";
					break;
				case kFootnotesTab:
					helpTopicKey = "khtpScrProp-Footnotes";
					break;
			}

			ShowHelp.ShowHelpTopic(m_helpTopicProvider, helpTopicKey);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When the text changes in any of the edit boxes, make sure that it has valid
		/// punctuation characters
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void Edit_TextChanged(object sender, System.EventArgs e)
		{
			TextBox textBox = sender as TextBox;
			if (textBox == null)
				return;

			string text = textBox.Text;
			if (text != string.Empty)
			{
				// If the new text is not punctuation, revert to the old version.
				if (!Char.IsPunctuation(text[0]))
				{
					text = textBox.Tag as string;
					if (text != null)
					{
						MiscUtils.ErrorBeep();
						textBox.Text = text;
						textBox.SelectAll();
					}
					return;
				}
			}

			// Save the new good text in case it needs to be reverted
			textBox.Tag = text;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the CheckedChanged event of the opnCombined control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void opnCombined_CheckedChanged(object sender, EventArgs e)
		{
			UpdateCombinedFootnotes();
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the dialog tab for this dialog.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int DialogTab
		{
			set
			{
				CheckDisposed();
				tabControl1.SelectedIndex = value;
			}
			get
			{
				CheckDisposed();
				return tabControl1.SelectedIndex;
			}
		}
		#endregion

		#region Other methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the footnote and cross reference tabs
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void UpdateFootnoteTabs()
		{
			fpsFootnoteOptions.Initialize(m_cache, m_styleSheet, m_scr.FootnoteMarkerType,
				m_scr.FootnoteMarkerSymbol, m_scr.DisplayFootnoteReference,
				m_scr.DisplaySymbolInFootnote, m_helpTopicProvider);

			fpsCrossRefOptions.Initialize(m_cache, m_styleSheet, m_scr.CrossRefMarkerType,
				m_scr.CrossRefMarkerSymbol, m_scr.DisplayCrossRefReference,
				m_scr.DisplaySymbolInCrossRef, m_helpTopicProvider);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When Script number options have changed, all of the chapter and verse numbers in the
		/// database will be updated.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void ConvertChapterVerseNumbers()
		{
			// Show a progress dialog for this operation
			using (ProgressDialogWithTask progressDlg = new ProgressDialogWithTask(this, m_cache.ThreadHelper))
			{
				progressDlg.Minimum = 0;
				progressDlg.Maximum = m_scr.ScriptureBooksOS.Count;
				progressDlg.Title = DlgResources.ResourceString("kstidConvertChapterVerseNumbersCaption");
				progressDlg.AllowCancel = false;

				progressDlg.RunTask(ConvertChapterVerseNumbers);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Converts the chapter verse numbers.
		/// </summary>
		/// <param name="progressDlg">The progress DLG.</param>
		/// <param name="parameters">The parameters.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private object ConvertChapterVerseNumbers(IThreadedProgress progressDlg, params object[] parameters)
		{
			char zeroDigit = (m_scr.UseScriptDigits ? (char)m_scr.ScriptDigitZero : '0');

			ILgCharacterPropertyEngine charEngine = m_cache.ServiceLocator.UnicodeCharProps;

			foreach (IScrBook book in m_scr.ScriptureBooksOS)
			{
				// update the status with the book name.
				progressDlg.Message =
					string.Format(DlgResources.ResourceString("kstidConvertChapterVerseNumbersMessage"),
						book.BestUIName);

				foreach (IScrSection section in book.SectionsOS)
				{
					foreach (IScrTxtPara para in section.ContentOA.ParagraphsOS)
						ConvertChapterVerseNumbers(para, zeroDigit, charEngine);
				}

				progressDlg.Step(0);
			}

			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Converts chapter and verse numbers in the given paragraph.
		/// </summary>
		/// <param name="para">Paragraph to be converted</param>
		/// <param name="zeroDigit">Character representing zero for chapter/verse numbers</param>
		/// <param name="charEngine">Unicode character properties engine</param>
		/// <returns>true if chapter or verse numbers were changed in paragraph</returns>
		/// <remarks>Return value is only used for testing.  Also, method is made virtual so
		/// test class can override it.  Allows testing to limit amount of processing for sake of
		/// time.</remarks>
		/// ------------------------------------------------------------------------------------
		protected virtual bool ConvertChapterVerseNumbers(IScrTxtPara para, char zeroDigit,
			ILgCharacterPropertyEngine charEngine)
		{
			ITsString tss = para.Contents;
			ITsStrBldr tssBldr = tss.GetBldr();
			int cRun = tss.RunCount;
			bool numbersFound = false;
			for (int i = 0; i < cRun; i++)
			{
				TsRunInfo tri;
				ITsTextProps ttp = tss.FetchRunInfo(i, out tri);
				IStStyle style = m_scr.FindStyle(ttp);
				if (style != null &&
					(style.Function == FunctionValues.Verse ||
					style.Function == FunctionValues.Chapter) && tri.ichMin < tri.ichLim)
				{
					numbersFound = true;
					string runChars = tss.GetChars(tri.ichMin, tri.ichLim);
					StringBuilder strBldr = new StringBuilder(runChars.Length);
					foreach (char c in runChars)
					{
						if (charEngine.get_IsNumber(c))
							strBldr.Append((char) (zeroDigit + charEngine.get_NumericValue(c)));
						else
							strBldr.Append(c);
					}
					tssBldr.Replace(tri.ichMin, tri.ichLim, strBldr.ToString(), ttp);
				}
			}
			if (numbersFound)
				para.Contents = tssBldr.GetString();
			return numbersFound;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When the verse bridge character changes, this will update all of the verse bridges
		/// in scripture.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void UpdateVerseBridges(string oldBridge)
		{
			// Show a progress dialog for this operation
			using (ProgressDialogWithTask progressDlg = new ProgressDialogWithTask(this, m_cache.ThreadHelper))
			{
				progressDlg.Minimum = 0;
				progressDlg.Maximum = m_scr.ScriptureBooksOS.Count;
				progressDlg.Title = DlgResources.ResourceString("kstidUpdateVerseBridges");
				progressDlg.AllowCancel = false;

				progressDlg.RunTask(UpdateVerseBridges, oldBridge);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the verse bridges.
		/// </summary>
		/// <param name="progressDlg">The progress dialog.</param>
		/// <param name="parameters">The parameters.</param>
		/// <returns>Always null.</returns>
		/// ------------------------------------------------------------------------------------
		private object UpdateVerseBridges(IThreadedProgress progressDlg, object[] parameters)
		{
			Debug.Assert(parameters.Length == 1);
			string oldBridge = (string)parameters[0];
			string newBridge = m_scr.Bridge;

			foreach (IScrBook book in m_scr.ScriptureBooksOS)
			{
				progressDlg.Message =
					string.Format(DlgResources.ResourceString("kstidUpdateVerseBridgesInBook"),
					book.Name.UserDefaultWritingSystem.Text);

				foreach (IScrSection section in book.SectionsOS)
					foreach (IScrTxtPara para in section.ContentOA.ParagraphsOS)
						UpdateVerseBridgesInParagraph(para, oldBridge, newBridge);

				progressDlg.Step(0);
			}

			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Scans a specified paragaph for runs with the verse number style. Then checks those
		/// runs for verse bridges.
		/// </summary>
		/// <param name="para">The paragraph to scan for runs containing verse bridges.</param>
		/// <param name="oldBridge">the old bridge sequence to replace</param>
		/// <param name="newBridge">The new verse bridge string.</param>
		/// ------------------------------------------------------------------------------------
		private void UpdateVerseBridgesInParagraph(IScrTxtPara para, string oldBridge, string newBridge)
		{
			int styleNameProp = (int)FwTextPropType.ktptNamedStyle;
			ITsString paraContents = para.Contents;
			ITsStrBldr bldr = null;

			// Iterate through the runs in the paragraph.
			for (int i = 0; i < paraContents.RunCount; i++)
			{
				// Get the run info and check to see if it is a verse number
				TsRunInfo runInfo;
				ITsTextProps props = paraContents.FetchRunInfo(i, out runInfo);

				// Check if the run is a verse number run.
				if (props.GetStrPropValue(styleNameProp) == ScrStyleNames.VerseNumber)
				{
					// Look to see if this verse is a bridge. If so, then reform it
					// with the new verse bridge string.
					string oldVerseText = paraContents.get_RunText(i);
					int bridgeIndex = oldVerseText.IndexOf(oldBridge, StringComparison.Ordinal);
					if (bridgeIndex != -1)
					{
						string newVerseText = oldVerseText.Replace(oldBridge, newBridge);

						// get a builder for the paragraph if one has not been gotten yet.
						if (bldr == null)
							bldr = paraContents.GetBldr();

						// Save the verse number text in the paragraph
						bldr.Replace(runInfo.ichMin, runInfo.ichLim, newVerseText, props);
					}
				}
			}

			// If any run was found to contain a verse bridge, then rewrite the paragraph.
			if (bldr != null)
				para.Contents = bldr.GetString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the combined footnotes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void UpdateCombinedFootnotes()
		{
			m_fCombineFootnotes = opnCombined.Checked;
			grpCrossRefFootnotes.Enabled = !m_fCombineFootnotes;
			fpsCrossRefOptions.CombinedFootnotes = m_fCombineFootnotes;
			fpsFootnoteOptions.CombinedFootnotes = m_fCombineFootnotes;

			// Update the status of the general footnotes if we need to change the
			// enabled state of the sequencial radio button
			fpsFootnoteOptions.UpdateEnabledStates();
			fpsCrossRefOptions.UpdateEnabledStates();
		}
		#endregion
	}
}

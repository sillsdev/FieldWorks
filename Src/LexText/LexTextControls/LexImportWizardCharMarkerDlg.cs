using System;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.Framework;	// for FwApp

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// Summary description for LexImportWizardCharMarkerDlg.
	/// </summary>
	public class LexImportWizardCharMarkerDlg : Form, IFWDisposable
	{
		private System.Windows.Forms.Label lblBeginMarker;
		private System.Windows.Forms.Label lblEndMarker;
		private System.Windows.Forms.TextBox tbBeginMarker;
		private System.Windows.Forms.TextBox tbEndMarker;
		private System.Windows.Forms.ComboBox cbLangDesc;
		private System.Windows.Forms.ComboBox cbStyle;
		private System.Windows.Forms.Button btnAdd;
		private System.Windows.Forms.Button btnStyles;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.GroupBox groupBox2;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
		private System.Windows.Forms.Label lblDescription;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label5;

		private Sfm2Xml.ClsInFieldMarker m_inlineMarker;
		private Hashtable m_uiLangs;
		private System.Windows.Forms.Button btnOK;
		private System.Windows.Forms.Button btnCancel;
		private FDO.FdoCache m_cache;
		private Hashtable m_existingBeginMarkers;
		private Hashtable m_existingEndMarkers;
		private Hashtable m_existingElementNames;

		private bool isValidEndMarker;
		private bool isValidBeginMarker;
		private Button buttonHelp;
		//private bool isValidElementName;

		private const string s_helpTopic = "khtpImportCharacterMapping";
		private RadioButton radioEndWithField;
		private RadioButton radioEndWithWord;
		private Label lblEndRadio;
		private System.Windows.Forms.HelpProvider helpProvider;

		private char[] delim = new char[] { ' ' };

		public LexImportWizardCharMarkerDlg()
		{
			isValidEndMarker = false;
			isValidBeginMarker = false;
			//isValidElementName = false;
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			m_inlineMarker = new Sfm2Xml.ClsInFieldMarker();
			HideOKBtn();	// see if it needs to be visible or not

			this.helpProvider = new System.Windows.Forms.HelpProvider();
			this.helpProvider.HelpNamespace = FwApp.App.HelpFile;
			this.helpProvider.SetHelpNavigator(this, System.Windows.Forms.HelpNavigator.Topic);
			this.helpProvider.SetHelpKeyword(this, FwApp.App.GetHelpString(s_helpTopic, 0));
			this.helpProvider.SetShowHelp(this, true);
		}

		private void InitWithIFM( Sfm2Xml.ClsInFieldMarker ifm)
		{
			HideOKBtn();	// see if it needs to be visible or not
			m_inlineMarker = ifm;
		}
		public string NoChange
		{
			get
			{
				CheckDisposed();
				return LexTextControls.ksNoChange;
			}
		}

		public void SetExistingBeginMarkers(Hashtable markers)
		{
			CheckDisposed();

			m_existingBeginMarkers = markers;
		}

		public void SetExistingEndMarkers(Hashtable markers)
		{
			CheckDisposed();

			m_existingEndMarkers = markers;
		}

		public void SetExistingElementNames(Hashtable names)
		{
			CheckDisposed();

			m_existingElementNames = names;
		}

		public void Init(Sfm2Xml.ClsInFieldMarker ifm, Hashtable uiLangsHT, FDO.FdoCache cache)
		{
			CheckDisposed();

			if (ifm == null)
				ifm = new Sfm2Xml.ClsInFieldMarker();

			m_inlineMarker = ifm;
			m_uiLangs = uiLangsHT;
			m_cache = cache;

			// ====================================================================
			// Set the language descriptor combo box.  This is a DropList so that
			// the entered text can't be different from the contents of the list.
			// If the user wants a new language descriptor they have to add one.

			cbLangDesc.Items.Add(NoChange);
			cbLangDesc.SelectedItem = NoChange;

			foreach (DictionaryEntry lang in m_uiLangs)
			{
				Sfm2Xml.LanguageInfoUI langInfo = lang.Value as Sfm2Xml.LanguageInfoUI;
				// make sure there is only one entry for each writing system (especially 'ignore')
				if (cbLangDesc.FindStringExact(langInfo.ToString()) < 0)
				{
					cbLangDesc.Items.Add(langInfo);
					if (langInfo.FwName == m_inlineMarker.Language)
						cbLangDesc.SelectedItem = langInfo;
				}
			}

			InitializeStylesComboBox();

			HideOKBtn();	// see if it needs to be visible or not
		}

		private void InitializeStylesComboBox()
		{
			// ====================================================================
			// Set the Style combo box.  This one can have a style that isn't defined
			// yet.  If so, it will show in a different color.
			// (This list only shows the Character styles.)
			cbStyle.Items.Clear();
			cbStyle.Items.Add(NoChange);
			cbStyle.SelectedItem = NoChange;
			cbStyle.Text = NoChange;

			FdoOwningCollection<IStStyle> oc = m_cache.LangProject.LexDbOA.StylesOC;
			if (oc == null || oc.Count < 1)
				System.Diagnostics.Debug.WriteLine("No style info retrieved from the cache.");

			foreach (StStyle style in oc)
			{
				if (StyleType.kstCharacter == style.Type)
				{
					int pos = cbStyle.Items.Add(style.Name);
					if (style.Name == m_inlineMarker.Style)
					{
						cbStyle.SelectedIndex = pos;
						cbStyle.SelectedItem = style.Name;
					}
				}
			}

			// if there's a Style in the Marker - select it or set it as unique, otherwise select noChange
			if (!m_inlineMarker.HasStyle)
			{
				cbStyle.SelectedItem = NoChange;
			}
			else
			{
				int foundPos = cbStyle.FindStringExact(m_inlineMarker.Style);
				if (foundPos >= 0)
				{
					// select the item in the combo that matches
					cbStyle.SelectedItem = m_inlineMarker.Style;
				}
				else
				{
					// just put it in the text box and set the color
					cbStyle.Text = m_inlineMarker.Style;
					cbStyle.ForeColor = System.Drawing.Color.Blue;
				}
				if (cbStyle.Text.Trim().Length == 0)
				{
					cbStyle.SelectedItem = NoChange;
					cbStyle.Text = NoChange;
				}
			}
		}

		/// <summary>
		/// Use the hash function of each object to see if the passed in marker
		/// is different from the current dlg values.
		/// </summary>
		/// <returns></returns>
		public bool IFMChanged()
		{
			CheckDisposed();

			Sfm2Xml.ClsInFieldMarker current = IFM();
			return current.GetHashCode() == m_inlineMarker.GetHashCode();
		}

		public Sfm2Xml.ClsInFieldMarker IFM()
		{
			CheckDisposed();

			string style = cbStyle.Text;
			if (style == NoChange)
				style = "";	// use empty string, not the "<No Change>" text

			string lang = cbLangDesc.Text;
			if (lang == NoChange)
				lang = "";	// use empty string, not the "<No Change>" text

			bool fHaveEndMarker = tbEndMarker.Text.Trim().Length > 0;
			bool fIgnore = lang.Length + style.Length == 0;

			// get the xmlLang value
			string xmlLangValue = "Unknown";
			Sfm2Xml.LanguageInfoUI langUI = m_uiLangs[lang] as Sfm2Xml.LanguageInfoUI;
			if (langUI != null)
				xmlLangValue = langUI.ClsLanguage.XmlLang;

			return new Sfm2Xml.ClsInFieldMarker(tbBeginMarker.Text.Trim(),
				tbEndMarker.Text.Trim(), radioEndWithWord.Checked && !fHaveEndMarker,
				radioEndWithField.Checked && !fHaveEndMarker,
				lang, xmlLangValue, style, fIgnore);
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

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LexImportWizardCharMarkerDlg));
			this.lblBeginMarker = new System.Windows.Forms.Label();
			this.lblEndMarker = new System.Windows.Forms.Label();
			this.tbBeginMarker = new System.Windows.Forms.TextBox();
			this.tbEndMarker = new System.Windows.Forms.TextBox();
			this.cbLangDesc = new System.Windows.Forms.ComboBox();
			this.cbStyle = new System.Windows.Forms.ComboBox();
			this.btnAdd = new System.Windows.Forms.Button();
			this.btnStyles = new System.Windows.Forms.Button();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.lblEndRadio = new System.Windows.Forms.Label();
			this.radioEndWithField = new System.Windows.Forms.RadioButton();
			this.radioEndWithWord = new System.Windows.Forms.RadioButton();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.label4 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.btnOK = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.lblDescription = new System.Windows.Forms.Label();
			this.buttonHelp = new System.Windows.Forms.Button();
			this.groupBox1.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.SuspendLayout();
			//
			// lblBeginMarker
			//
			resources.ApplyResources(this.lblBeginMarker, "lblBeginMarker");
			this.lblBeginMarker.Name = "lblBeginMarker";
			//
			// lblEndMarker
			//
			resources.ApplyResources(this.lblEndMarker, "lblEndMarker");
			this.lblEndMarker.Name = "lblEndMarker";
			//
			// tbBeginMarker
			//
			resources.ApplyResources(this.tbBeginMarker, "tbBeginMarker");
			this.tbBeginMarker.Name = "tbBeginMarker";
			this.tbBeginMarker.TextChanged += new System.EventHandler(this.tbBeginMarker_TextChanged);
			//
			// tbEndMarker
			//
			resources.ApplyResources(this.tbEndMarker, "tbEndMarker");
			this.tbEndMarker.Name = "tbEndMarker";
			this.tbEndMarker.TextChanged += new System.EventHandler(this.tbEndMarker_TextChanged);
			//
			// cbLangDesc
			//
			resources.ApplyResources(this.cbLangDesc, "cbLangDesc");
			this.cbLangDesc.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cbLangDesc.Name = "cbLangDesc";
			//
			// cbStyle
			//
			resources.ApplyResources(this.cbStyle, "cbStyle");
			this.cbStyle.Name = "cbStyle";
			this.cbStyle.SelectionChangeCommitted += new System.EventHandler(this.cbStyle_SelectionChangeCommitted);
			//
			// btnAdd
			//
			resources.ApplyResources(this.btnAdd, "btnAdd");
			this.btnAdd.Name = "btnAdd";
			this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
			//
			// btnStyles
			//
			resources.ApplyResources(this.btnStyles, "btnStyles");
			this.btnStyles.Name = "btnStyles";
			this.btnStyles.Click += new System.EventHandler(this.btnStyles_Click);
			//
			// groupBox1
			//
			this.groupBox1.Controls.Add(this.lblEndRadio);
			this.groupBox1.Controls.Add(this.radioEndWithField);
			this.groupBox1.Controls.Add(this.radioEndWithWord);
			resources.ApplyResources(this.groupBox1, "groupBox1");
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.TabStop = false;
			//
			// lblEndRadio
			//
			resources.ApplyResources(this.lblEndRadio, "lblEndRadio");
			this.lblEndRadio.Name = "lblEndRadio";
			//
			// radioEndWithField
			//
			resources.ApplyResources(this.radioEndWithField, "radioEndWithField");
			this.radioEndWithField.Name = "radioEndWithField";
			this.radioEndWithField.TabStop = true;
			this.radioEndWithField.UseVisualStyleBackColor = true;
			this.radioEndWithField.CheckedChanged += new System.EventHandler(this.radioEndWithField_CheckedChanged);
			//
			// radioEndWithWord
			//
			resources.ApplyResources(this.radioEndWithWord, "radioEndWithWord");
			this.radioEndWithWord.Name = "radioEndWithWord";
			this.radioEndWithWord.TabStop = true;
			this.radioEndWithWord.UseVisualStyleBackColor = true;
			this.radioEndWithWord.CheckedChanged += new System.EventHandler(this.radioEndWithWord_CheckedChanged);
			//
			// groupBox2
			//
			this.groupBox2.Controls.Add(this.label4);
			this.groupBox2.Controls.Add(this.label5);
			this.groupBox2.Controls.Add(this.cbStyle);
			this.groupBox2.Controls.Add(this.btnStyles);
			resources.ApplyResources(this.groupBox2, "groupBox2");
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.TabStop = false;
			//
			// label4
			//
			resources.ApplyResources(this.label4, "label4");
			this.label4.Name = "label4";
			//
			// label5
			//
			resources.ApplyResources(this.label5, "label5");
			this.label5.Name = "label5";
			//
			// btnOK
			//
			resources.ApplyResources(this.btnOK, "btnOK");
			this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnOK.Name = "btnOK";
			//
			// btnCancel
			//
			resources.ApplyResources(this.btnCancel, "btnCancel");
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Name = "btnCancel";
			//
			// lblDescription
			//
			resources.ApplyResources(this.lblDescription, "lblDescription");
			this.lblDescription.Name = "lblDescription";
			//
			// buttonHelp
			//
			resources.ApplyResources(this.buttonHelp, "buttonHelp");
			this.buttonHelp.Name = "buttonHelp";
			this.buttonHelp.UseVisualStyleBackColor = true;
			this.buttonHelp.Click += new System.EventHandler(this.buttonHelp_Click);
			//
			// LexImportWizardCharMarkerDlg
			//
			this.AcceptButton = this.btnOK;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.btnCancel;
			this.Controls.Add(this.buttonHelp);
			this.Controls.Add(this.tbEndMarker);
			this.Controls.Add(this.tbBeginMarker);
			this.Controls.Add(this.lblDescription);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnOK);
			this.Controls.Add(this.btnAdd);
			this.Controls.Add(this.cbLangDesc);
			this.Controls.Add(this.lblEndMarker);
			this.Controls.Add(this.lblBeginMarker);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.groupBox2);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.Name = "LexImportWizardCharMarkerDlg";
			this.Load += new System.EventHandler(this.LexImportWizardCharMarkerDlg_Load);
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.groupBox2.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		private void LexImportWizardCharMarkerDlg_Load(object sender, System.EventArgs e)
		{
			tbBeginMarker.Text = m_inlineMarker.Begin;
			if (m_inlineMarker.End.Count > 0)
				tbEndMarker.Text = m_inlineMarker.EndListToString();
			radioEndWithField.Checked = m_inlineMarker.EndWithField;
			radioEndWithWord.Checked = m_inlineMarker.EndWithWord;
			if (m_inlineMarker.End.Count == 0 &&
				!m_inlineMarker.EndWithField &&
				!m_inlineMarker.EndWithWord)
			{
				// Susanna wants this as the default, and .NET 2005 insists on checking
				// the other one without being asked.
				radioEndWithField.Checked = true;
			}
			cbLangDesc.Text = m_inlineMarker.Language;
			cbStyle.Text = m_inlineMarker.Style;
			if (cbStyle.Text.Trim().Length == 0)
				cbStyle.Text = NoChange;	// needed for Style, but not for LangDesc.
		}

		private void EnableRadioEndButtons()
		{
			if (tbEndMarker.Text.Trim().Length > 0)
			{
				lblEndRadio.Enabled = false;
				radioEndWithWord.Enabled = false;
				radioEndWithField.Enabled = false;
			}
			else
			{
				lblEndRadio.Enabled = true;
				radioEndWithWord.Enabled = true;
				radioEndWithField.Enabled = true;
			}
		}

		private void btnAdd_Click(object sender, System.EventArgs e)
		{
			LexImportWizardLanguage dlg = new LexImportWizardLanguage(m_cache, m_uiLangs);
			if (dlg.ShowDialog(this) == DialogResult.OK)
			{
				string langDesc, ws, ec, icu;
				// retrieve the new WS information from the dlg
				dlg.GetCurrentLangInfo(out langDesc, out ws, out ec, out icu);

				// now put the lang info into the language list view
				if (LexImportWizard.Wizard().AddLanguage(langDesc, ws, ec, icu))
				{
					// this was added to the list of languages, so add it to the dlg and select it
					Sfm2Xml.LanguageInfoUI langInfo = new Sfm2Xml.LanguageInfoUI(langDesc, ws, ec, icu);
					if (cbLangDesc.FindStringExact(langInfo.ToString()) < 0)
					{
						cbLangDesc.Items.Add(langInfo);
					}
					cbLangDesc.SelectedItem = langInfo;
				}
			}
		}

		private void btnStyles_Click(object sender, System.EventArgs e)
		{
			XCore.Mediator med = null;
			LexImportWizard wiz = LexImportWizard.Wizard();
			if (wiz != null)
				med = wiz.Mediator;
			if (med == null)
			{
				// See LT-9100 and LT-9266.  Apparently this condition can happen.
				MessageBox.Show(LexTextControls.ksCannotSoTryAgain, LexTextControls.ksInternalProblem,
					MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			med.SendMessage("FormatStyle", null);
			// The "styles" dialog can trigger a refresh, which disposes of the mediator on the
			// main window, creating a new one.  See LT-7275.
			// The accessor gets a new one if the old one has been disposed.
			if (med.IsDisposed)
				med = wiz.Mediator;
			// We also need to re-initialize the combobox list, since the set of character
			// styles may have changed.
			InitializeStylesComboBox();
		}

		//private void tbElementName_TextChanged(object sender, EventArgs e)
		//{
		//    ValidateElementName();
		//    HideOKBtn();
		//}

		private void tbBeginMarker_TextChanged(object sender, EventArgs e)
		{
			ValidateBeginMarkerText();
			if (isValidBeginMarker)
				ValidateEndMarkers();		// make sure the end markers are still valid with this change

			HideOKBtn();
		}

		private void tbEndMarker_TextChanged(object sender, EventArgs e)
		{
			ValidateEndMarkers();
			EnableRadioEndButtons();
			HideOKBtn();
		}

		private void cbStyle_SelectionChangeCommitted(object sender, EventArgs e)
		{
			// if a selection is made from the list, go back to regular color
			System.Drawing.Color sysTextColor = cbLangDesc.ForeColor;
			if (cbStyle.ForeColor != sysTextColor)
				cbStyle.ForeColor = sysTextColor;
		}

		#region Data validation helper routines

		//private bool ValidateElementName()
		//{
		//    // can't be the same as any current element names
		//    isValidElementName = true;
		//    if (m_existingElementNames == null)
		//        return isValidBeginMarker;

		//    string name = tbElementName.Text;
		//    isValidElementName = !(name.Length == 0 || m_existingElementNames.ContainsKey(name));
		//    return isValidElementName;
		//}
		private bool ValidateBeginMarkerText()
		{
			// can't be the same as any current begin or end markers
			isValidBeginMarker = true;
			if (m_existingBeginMarkers == null || m_existingEndMarkers == null)
				return isValidBeginMarker;

			string marker = tbBeginMarker.Text;
			isValidBeginMarker = !(marker.Length == 0 || m_existingBeginMarkers.ContainsKey(marker) || m_existingEndMarkers.ContainsKey(marker));
			return isValidBeginMarker;
		}
		private bool ValidateEndMarkers()
		{
			// can't already exist as a begin marker or be equal to the begin marker for this one
			isValidEndMarker = true;

			if (m_existingBeginMarkers == null)
				return isValidEndMarker;

			if (tbEndMarker.Text.Trim().Length == 0)
			{
				isValidEndMarker = false;
			}
			else
			{
				ArrayList list = new ArrayList();
				Sfm2Xml.STATICS.SplitString(tbEndMarker.Text.Trim(), delim, ref list);
				foreach (string s in list)
				{
					if (m_existingBeginMarkers.ContainsKey(s) || tbBeginMarker.Text == s)
					{
						isValidEndMarker = false;
						break;
					}
				}
			}
			return isValidEndMarker;
		}

		private bool HasValidBeginMarker()
		{
			return tbBeginMarker.Text.Trim().Length > 0 && isValidBeginMarker;
		}

		private bool HasValidEndMarker()
		{
			if (tbEndMarker.Text.Trim().Length > 0)
				return isValidEndMarker;
			else
				return radioEndWithWord.Checked || radioEndWithField.Checked;
		}

		private bool HasValidData()
		{
			return HasValidBeginMarker() && HasValidEndMarker();
		}

		#endregion

		#region Helper methods for GUI details: color, enabling, ...

		private void HideOKBtn()
		{
			Color highlightColor = Color.Red;
			bool hide = !HasValidData();
			if (btnOK.Enabled == hide)
			{
				btnOK.Enabled = !hide;
//				msgAlreadyUsed.Visible = hide;
			}

			UpdateControlColor(lblBeginMarker, isValidBeginMarker, highlightColor);
			UpdateControlColor(lblEndMarker, HasValidEndMarker(), highlightColor);
		}
		private void UpdateControlColor(Control ctrl, bool valid, Color errColor)
		{
			// Susanna didn't like the colors.  :-) :-(
			//if (valid == false)
			//{
			//    if (ctrl.ForeColor != errColor)
			//        ctrl.ForeColor = errColor;
			//}
			//else
			//{
			//    if (ctrl.ForeColor != SystemColors.ControlText)
			//        ctrl.ForeColor = SystemColors.ControlText;
			//}
		}

		#endregion

		private void buttonHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(FwApp.App, s_helpTopic);
		}

		private void radioEndWithWord_CheckedChanged(object sender, EventArgs e)
		{
			HideOKBtn();
		}

		private void radioEndWithField_CheckedChanged(object sender, EventArgs e)
		{
			HideOKBtn();
		}
	}
}

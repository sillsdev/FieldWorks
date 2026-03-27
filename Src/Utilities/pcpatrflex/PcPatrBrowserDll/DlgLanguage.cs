using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

namespace SIL.PcPatrBrowser
{
	/// <summary>
	/// Summary description for DlgLanguage.
	/// </summary>
	public class DlgLanguage : System.Windows.Forms.Form
	{
		private LanguageInfo m_language;

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox tbLanguageName;
		private System.Windows.Forms.GroupBox gbFonts;
		private System.Windows.Forms.Button btnGlossFont;
		private System.Windows.Forms.Button btnLexFont;
		private System.Windows.Forms.Button btnNTFont;
		private System.Windows.Forms.Label lblGlossFont;
		private System.Windows.Forms.Label lblLexFont;
		private System.Windows.Forms.Label lblNTFont;
		private System.Windows.Forms.Label lblGlossFont1;
		private System.Windows.Forms.Label lblLexFont1;
		private System.Windows.Forms.Label lblNTFont1;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.Button btnOK;
		private System.Windows.Forms.CheckBox cbRTL;
		private System.Windows.Forms.TextBox tbSepChar;
		private System.Windows.Forms.Label label2;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public DlgLanguage(LanguageInfo language)
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			m_language = language;
			LanguageName = language.LanguageName;
			NTInfo = language.NTInfo;
			LexInfo = language.LexInfo;
			GlossInfo = language.GlossInfo;
			cbRTL.Checked = language.UseRTL;
			tbSepChar.Text = language.DecompChar;
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(
				typeof(DlgLanguage)
			);
			this.label1 = new System.Windows.Forms.Label();
			this.tbLanguageName = new System.Windows.Forms.TextBox();
			this.gbFonts = new System.Windows.Forms.GroupBox();
			this.btnGlossFont = new System.Windows.Forms.Button();
			this.btnLexFont = new System.Windows.Forms.Button();
			this.btnNTFont = new System.Windows.Forms.Button();
			this.lblGlossFont = new System.Windows.Forms.Label();
			this.lblLexFont = new System.Windows.Forms.Label();
			this.lblNTFont = new System.Windows.Forms.Label();
			this.lblGlossFont1 = new System.Windows.Forms.Label();
			this.lblLexFont1 = new System.Windows.Forms.Label();
			this.lblNTFont1 = new System.Windows.Forms.Label();
			this.btnCancel = new System.Windows.Forms.Button();
			this.btnOK = new System.Windows.Forms.Button();
			this.cbRTL = new System.Windows.Forms.CheckBox();
			this.tbSepChar = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.gbFonts.SuspendLayout();
			this.SuspendLayout();
			//
			// label1
			//
			this.label1.Location = new System.Drawing.Point(16, 16);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(48, 16);
			this.label1.TabIndex = 0;
			this.label1.Text = "N&ame:";
			//
			// tbLanguageName
			//
			this.tbLanguageName.Location = new System.Drawing.Point(80, 16);
			this.tbLanguageName.Name = "tbLanguageName";
			this.tbLanguageName.Size = new System.Drawing.Size(312, 20);
			this.tbLanguageName.TabIndex = 1;
			this.tbLanguageName.Text = "textBox1";
			//
			// gbFonts
			//
			this.gbFonts.Controls.Add(this.btnGlossFont);
			this.gbFonts.Controls.Add(this.btnLexFont);
			this.gbFonts.Controls.Add(this.btnNTFont);
			this.gbFonts.Controls.Add(this.lblGlossFont);
			this.gbFonts.Controls.Add(this.lblLexFont);
			this.gbFonts.Controls.Add(this.lblNTFont);
			this.gbFonts.Controls.Add(this.lblGlossFont1);
			this.gbFonts.Controls.Add(this.lblLexFont1);
			this.gbFonts.Controls.Add(this.lblNTFont1);
			this.gbFonts.Location = new System.Drawing.Point(8, 48);
			this.gbFonts.Name = "gbFonts";
			this.gbFonts.Size = new System.Drawing.Size(456, 136);
			this.gbFonts.TabIndex = 2;
			this.gbFonts.TabStop = false;
			this.gbFonts.Text = "Parse Tree Fonts";
			//
			// btnGlossFont
			//
			this.btnGlossFont.Location = new System.Drawing.Point(344, 96);
			this.btnGlossFont.Name = "btnGlossFont";
			this.btnGlossFont.Size = new System.Drawing.Size(88, 24);
			this.btnGlossFont.TabIndex = 17;
			this.btnGlossFont.Text = "&Gloss Font...";
			this.btnGlossFont.Click += new System.EventHandler(this.btnGlossFont_Click);
			//
			// btnLexFont
			//
			this.btnLexFont.Location = new System.Drawing.Point(344, 64);
			this.btnLexFont.Name = "btnLexFont";
			this.btnLexFont.Size = new System.Drawing.Size(88, 24);
			this.btnLexFont.TabIndex = 14;
			this.btnLexFont.Text = "&Lex Font...";
			this.btnLexFont.Click += new System.EventHandler(this.btnLexFont_Click);
			//
			// btnNTFont
			//
			this.btnNTFont.Location = new System.Drawing.Point(344, 32);
			this.btnNTFont.Name = "btnNTFont";
			this.btnNTFont.Size = new System.Drawing.Size(88, 24);
			this.btnNTFont.TabIndex = 11;
			this.btnNTFont.Text = "&NT Font...";
			this.btnNTFont.Click += new System.EventHandler(this.btnNTFont_Click);
			//
			// lblGlossFont
			//
			this.lblGlossFont.Location = new System.Drawing.Point(144, 96);
			this.lblGlossFont.Name = "lblGlossFont";
			this.lblGlossFont.Size = new System.Drawing.Size(192, 16);
			this.lblGlossFont.TabIndex = 16;
			this.lblGlossFont.Text = "GlossFont";
			//
			// lblLexFont
			//
			this.lblLexFont.Location = new System.Drawing.Point(144, 64);
			this.lblLexFont.Name = "lblLexFont";
			this.lblLexFont.Size = new System.Drawing.Size(192, 16);
			this.lblLexFont.TabIndex = 13;
			this.lblLexFont.Text = "LexFont";
			//
			// lblNTFont
			//
			this.lblNTFont.Location = new System.Drawing.Point(144, 32);
			this.lblNTFont.Name = "lblNTFont";
			this.lblNTFont.Size = new System.Drawing.Size(192, 16);
			this.lblNTFont.TabIndex = 10;
			this.lblNTFont.Text = "NTFont";
			//
			// lblGlossFont1
			//
			this.lblGlossFont1.Location = new System.Drawing.Point(24, 96);
			this.lblGlossFont1.Name = "lblGlossFont1";
			this.lblGlossFont1.Size = new System.Drawing.Size(112, 16);
			this.lblGlossFont1.TabIndex = 15;
			this.lblGlossFont1.Text = "Glosses:";
			//
			// lblLexFont1
			//
			this.lblLexFont1.Location = new System.Drawing.Point(24, 64);
			this.lblLexFont1.Name = "lblLexFont1";
			this.lblLexFont1.Size = new System.Drawing.Size(112, 16);
			this.lblLexFont1.TabIndex = 12;
			this.lblLexFont1.Text = "Lexical items:";
			//
			// lblNTFont1
			//
			this.lblNTFont1.Location = new System.Drawing.Point(24, 32);
			this.lblNTFont1.Name = "lblNTFont1";
			this.lblNTFont1.Size = new System.Drawing.Size(112, 16);
			this.lblNTFont1.TabIndex = 9;
			this.lblNTFont1.Text = "Non-terminal nodes:";
			//
			// btnCancel
			//
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Location = new System.Drawing.Point(376, 248);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(80, 24);
			this.btnCancel.TabIndex = 7;
			this.btnCancel.Text = "Cancel";
			//
			// btnOK
			//
			this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnOK.Location = new System.Drawing.Point(288, 248);
			this.btnOK.Name = "btnOK";
			this.btnOK.Size = new System.Drawing.Size(80, 24);
			this.btnOK.TabIndex = 6;
			this.btnOK.Text = "OK";
			//
			// cbRTL
			//
			this.cbRTL.Location = new System.Drawing.Point(8, 192);
			this.cbRTL.Name = "cbRTL";
			this.cbRTL.Size = new System.Drawing.Size(312, 16);
			this.cbRTL.TabIndex = 8;
			this.cbRTL.Text = "Use &right-to-left orientation in parse tree and interlinear";
			//
			// tbSepChar
			//
			this.tbSepChar.Location = new System.Drawing.Point(264, 216);
			this.tbSepChar.MaxLength = 1;
			this.tbSepChar.Name = "tbSepChar";
			this.tbSepChar.Size = new System.Drawing.Size(16, 20);
			this.tbSepChar.TabIndex = 9;
			this.tbSepChar.Text = "-";
			this.tbSepChar.WordWrap = false;
			//
			// label2
			//
			this.label2.Location = new System.Drawing.Point(8, 216);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(256, 16);
			this.label2.TabIndex = 10;
			this.label2.Text = "&Morpheme Decomposition Separation Character:";
			//
			// DlgLanguage
			//
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(472, 280);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.tbSepChar);
			this.Controls.Add(this.cbRTL);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnOK);
			this.Controls.Add(this.gbFonts);
			this.Controls.Add(this.tbLanguageName);
			this.Controls.Add(this.label1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "DlgLanguage";
			this.Text = "Edit Language Information";
			this.gbFonts.ResumeLayout(false);
			this.ResumeLayout(false);
		}
		#endregion

		private void btnNTFont_Click(object sender, System.EventArgs e)
		{
			MyFontInfo mfi = m_language.NTInfo;
			doFont(ref mfi, lblNTFont);
		}

		private void btnLexFont_Click(object sender, System.EventArgs e)
		{
			MyFontInfo mfi = m_language.LexInfo;
			doFont(ref mfi, lblLexFont);
		}

		private void btnGlossFont_Click(object sender, System.EventArgs e)
		{
			MyFontInfo mfi = m_language.GlossInfo;
			doFont(ref mfi, lblGlossFont);
		}

		void doFont(ref MyFontInfo mfi, Label lbl)
		{
			FontDialog fontdlg = new FontDialog();
			fontdlg.ShowEffects = true;
			fontdlg.ShowColor = true;
			fontdlg.Font = mfi.Font;
			fontdlg.Color = mfi.Color;
			if (fontdlg.ShowDialog() == DialogResult.OK)
			{
				// set the values
				mfi.Font = fontdlg.Font;
				mfi.Color = fontdlg.Color;
				lbl.Text = mfi.Font.Name;
			}
		}

		/// <summary>
		/// Gets/sets NT info.
		/// </summary>
		public MyFontInfo NTInfo
		{
			get { return m_language.NTInfo; }
			set
			{
				m_language.NTInfo = value;
				lblNTFont.Text = m_language.NTInfo.Font.Name;
			}
		}

		/// <summary>
		/// Gets/sets Lex info.
		/// </summary>
		public MyFontInfo LexInfo
		{
			get { return m_language.LexInfo; }
			set
			{
				m_language.LexInfo = value;
				lblLexFont.Text = m_language.LexInfo.Font.Name;
			}
		}

		/// <summary>
		/// Gets/sets Gloss info.
		/// </summary>
		public MyFontInfo GlossInfo
		{
			get { return m_language.GlossInfo; }
			set
			{
				m_language.GlossInfo = value;
				lblGlossFont.Text = m_language.GlossInfo.Font.Name;
			}
		}

		/// <summary>
		/// Gets/sets language name.
		/// </summary>
		public string LanguageName
		{
			get { return tbLanguageName.Text; }
			set { tbLanguageName.Text = value; }
		}

		/// <summary>
		/// Gets/sets use right-to-left orientation.
		/// </summary>
		public bool UseRTL
		{
			get { return cbRTL.Checked; }
			set { cbRTL.Checked = value; }
		}

		/// <summary>
		/// Gets/sets morpheme decomposition separation character.
		/// </summary>
		public string DecompChar
		{
			get { return tbSepChar.Text; }
			set { tbSepChar.Text = value; }
		}
	}
}

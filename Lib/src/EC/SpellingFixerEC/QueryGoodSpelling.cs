using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Runtime.InteropServices;   // for ComVisible
using SilEncConverters31;

namespace SpellingFixerEC
{
	/// <summary>
	/// Summary description for QueryGoodSpelling.
	/// </summary>
	internal class QueryGoodSpelling : System.Windows.Forms.Form
	{
		private IContainer components;
		private System.Windows.Forms.Label label1 = null;
		private EcTextBox textBoxBadWord = null;
		private System.Windows.Forms.Label label2 = null;
		private EcTextBox textBoxReplacement = null;
		private System.Windows.Forms.Button buttonOK = null;
		private System.Windows.Forms.Button buttonCancel = null;
		private string m_strGoodWord;
		private System.Windows.Forms.Label labelUniCodes;
		private System.Windows.Forms.Button buttonDelete;
		private System.Windows.Forms.TextBox labelOriginalReason;
		private System.Windows.Forms.Label labelOrigReasonLabel;
		private TableLayoutPanel tableLayoutPanel;
		private HelpProvider helpProvider;
		private ToolTip toolTip;
		private string m_strBadWord;    // in case we change it

		public QueryGoodSpelling(Font font)
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			textBoxBadWord.Font = font;
			textBoxReplacement.Font = font;
			labelOriginalReason.Font = font;

			helpProvider.SetHelpString(this.textBoxBadWord, Properties.Resources.textBoxBadWordHelp);
			helpProvider.SetHelpString(this.textBoxReplacement, Properties.Resources.textBoxReplacementHelp);
		}

		public DialogResult ShowDialog(string strBadWord, string strReplacement, string strOriginalWord, bool bShowDelete)
		{
			textBoxBadWord.Text = strBadWord;
			textBoxReplacement.Text = strReplacement;

			textBoxReplacement.Focus();
			textBoxReplacement.SelectAll();

			this.buttonDelete.Visible = bShowDelete;

			if (strBadWord != strReplacement)
			{
				this.Text = "Existing Replacement Rule";
				if (!String.IsNullOrEmpty(strOriginalWord))
				{
					this.labelOrigReasonLabel.Visible = true;
					this.labelOriginalReason.Visible = true;
					this.labelOriginalReason.Text = strOriginalWord;
				}
			}

			return base.ShowDialog();
		}

		public string   GoodSpelling
		{
			get { return m_strGoodWord; }
		}

		public string   BadSpelling
		{
			get { return m_strBadWord; }
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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(QueryGoodSpelling));
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.buttonOK = new System.Windows.Forms.Button();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.labelUniCodes = new System.Windows.Forms.Label();
			this.buttonDelete = new System.Windows.Forms.Button();
			this.labelOriginalReason = new System.Windows.Forms.TextBox();
			this.labelOrigReasonLabel = new System.Windows.Forms.Label();
			this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.textBoxBadWord = new EcTextBox();
			this.textBoxReplacement = new EcTextBox();
			this.helpProvider = new System.Windows.Forms.HelpProvider();
			this.toolTip = new System.Windows.Forms.ToolTip(this.components);
			this.tableLayoutPanel.SuspendLayout();
			this.SuspendLayout();
			//
			// label1
			//
			this.label1.Anchor = System.Windows.Forms.AnchorStyles.Right;
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(28, 6);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(69, 13);
			this.label1.TabIndex = 0;
			this.label1.Text = "Bad Spelling:";
			//
			// label2
			//
			this.label2.Anchor = System.Windows.Forms.AnchorStyles.Right;
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(24, 32);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(73, 13);
			this.label2.TabIndex = 2;
			this.label2.Text = "Replacement:";
			//
			// buttonOK
			//
			this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonOK.Location = new System.Drawing.Point(237, 244);
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.Size = new System.Drawing.Size(61, 23);
			this.buttonOK.TabIndex = 3;
			this.buttonOK.Text = "OK";
			this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
			//
			// buttonCancel
			//
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.Location = new System.Drawing.Point(304, 244);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(61, 23);
			this.buttonCancel.TabIndex = 4;
			this.buttonCancel.Text = "Cancel";
			this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
			//
			// labelUniCodes
			//
			this.labelUniCodes.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.tableLayoutPanel.SetColumnSpan(this.labelUniCodes, 4);
			this.labelUniCodes.Dock = System.Windows.Forms.DockStyle.Fill;
			this.helpProvider.SetHelpString(this.labelUniCodes, "This area shows the Unicode code point values for the characters in the box above" +
					" which has focus. You can use this to see hidden characters (e.g. zero width joi" +
					"ner).");
			this.labelUniCodes.Location = new System.Drawing.Point(3, 55);
			this.labelUniCodes.Margin = new System.Windows.Forms.Padding(3);
			this.labelUniCodes.Name = "labelUniCodes";
			this.labelUniCodes.Padding = new System.Windows.Forms.Padding(3);
			this.helpProvider.SetShowHelp(this.labelUniCodes, true);
			this.labelUniCodes.Size = new System.Drawing.Size(362, 157);
			this.labelUniCodes.TabIndex = 5;
			this.labelUniCodes.Text = "labelUniCodes";
			//
			// buttonDelete
			//
			this.buttonDelete.DialogResult = System.Windows.Forms.DialogResult.Abort;
			this.buttonDelete.Location = new System.Drawing.Point(3, 244);
			this.buttonDelete.Name = "buttonDelete";
			this.buttonDelete.Size = new System.Drawing.Size(67, 23);
			this.buttonDelete.TabIndex = 6;
			this.buttonDelete.Text = "Delete";
			this.buttonDelete.Visible = false;
			this.buttonDelete.Click += new System.EventHandler(this.buttonDelete_Click);
			//
			// labelOriginalReason
			//
			this.tableLayoutPanel.SetColumnSpan(this.labelOriginalReason, 3);
			this.labelOriginalReason.Dock = System.Windows.Forms.DockStyle.Fill;
			this.labelOriginalReason.Location = new System.Drawing.Point(103, 218);
			this.labelOriginalReason.Name = "labelOriginalReason";
			this.labelOriginalReason.ReadOnly = true;
			this.labelOriginalReason.Size = new System.Drawing.Size(262, 20);
			this.labelOriginalReason.TabIndex = 7;
			this.labelOriginalReason.Visible = false;
			//
			// labelOrigReasonLabel
			//
			this.labelOrigReasonLabel.Anchor = System.Windows.Forms.AnchorStyles.Right;
			this.labelOrigReasonLabel.AutoSize = true;
			this.labelOrigReasonLabel.Location = new System.Drawing.Point(3, 221);
			this.labelOrigReasonLabel.Name = "labelOrigReasonLabel";
			this.labelOrigReasonLabel.Size = new System.Drawing.Size(94, 13);
			this.labelOrigReasonLabel.TabIndex = 8;
			this.labelOrigReasonLabel.Text = "added while fixing:";
			this.labelOrigReasonLabel.Visible = false;
			//
			// tableLayoutPanel
			//
			this.tableLayoutPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.tableLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.tableLayoutPanel.ColumnCount = 4;
			this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel.Controls.Add(this.labelOriginalReason, 1, 3);
			this.tableLayoutPanel.Controls.Add(this.textBoxBadWord, 1, 0);
			this.tableLayoutPanel.Controls.Add(this.label2, 0, 1);
			this.tableLayoutPanel.Controls.Add(this.labelUniCodes, 0, 2);
			this.tableLayoutPanel.Controls.Add(this.buttonDelete, 0, 4);
			this.tableLayoutPanel.Controls.Add(this.textBoxReplacement, 1, 1);
			this.tableLayoutPanel.Controls.Add(this.buttonOK, 2, 4);
			this.tableLayoutPanel.Controls.Add(this.label1, 0, 0);
			this.tableLayoutPanel.Controls.Add(this.labelOrigReasonLabel, 0, 3);
			this.tableLayoutPanel.Controls.Add(this.buttonCancel, 3, 4);
			this.tableLayoutPanel.Location = new System.Drawing.Point(12, 12);
			this.tableLayoutPanel.Name = "tableLayoutPanel";
			this.tableLayoutPanel.RowCount = 5;
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel.Size = new System.Drawing.Size(368, 270);
			this.tableLayoutPanel.TabIndex = 9;
			//
			// textBoxBadWord
			//
			this.tableLayoutPanel.SetColumnSpan(this.textBoxBadWord, 3);
			this.textBoxBadWord.Dock = System.Windows.Forms.DockStyle.Fill;
			this.helpProvider.SetHelpString(this.textBoxBadWord, "");
			this.textBoxBadWord.Location = new System.Drawing.Point(103, 3);
			this.textBoxBadWord.Name = "textBoxBadWord";
			this.helpProvider.SetShowHelp(this.textBoxBadWord, true);
			this.textBoxBadWord.Size = new System.Drawing.Size(262, 20);
			this.textBoxBadWord.TabIndex = 1;
			this.textBoxBadWord.Text = "textBoxBadWord";
			this.toolTip.SetToolTip(this.textBoxBadWord, "Contains the bad spelling form");
			this.textBoxBadWord.GotFocus += new System.EventHandler(this.textBoxBadWord_GotFocus);
			this.textBoxBadWord.TextChanged += new System.EventHandler(this.textBoxBadWord_TextChanged);
			//
			// textBoxReplacement
			//
			this.tableLayoutPanel.SetColumnSpan(this.textBoxReplacement, 3);
			this.textBoxReplacement.Dock = System.Windows.Forms.DockStyle.Fill;
			this.helpProvider.SetHelpString(this.textBoxReplacement, "");
			this.textBoxReplacement.Location = new System.Drawing.Point(103, 29);
			this.textBoxReplacement.Name = "textBoxReplacement";
			this.helpProvider.SetShowHelp(this.textBoxReplacement, true);
			this.textBoxReplacement.Size = new System.Drawing.Size(262, 20);
			this.textBoxReplacement.TabIndex = 1;
			this.textBoxReplacement.Text = "textBoxReplacement";
			this.toolTip.SetToolTip(this.textBoxReplacement, "Contains the good spelling form (i.e. the replacment for when the bad spelling fo" +
					"rm occurs)");
			this.textBoxReplacement.GotFocus += new System.EventHandler(this.textBoxReplacement_GotFocus);
			this.textBoxReplacement.TextChanged += new System.EventHandler(this.textBoxReplacement_TextChanged);
			//
			// QueryGoodSpelling
			//
			this.AcceptButton = this.buttonOK;
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(392, 294);
			this.Controls.Add(this.tableLayoutPanel);
			this.HelpButton = true;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "QueryGoodSpelling";
			this.Text = "Fix Spelling";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.QueryGoodSpelling_FormClosing);
			this.tableLayoutPanel.ResumeLayout(false);
			this.tableLayoutPanel.PerformLayout();
			this.ResumeLayout(false);

		}
		#endregion

		private void buttonOK_Click(object sender, System.EventArgs e)
		{
			m_strGoodWord = this.textBoxReplacement.Text;
			m_strBadWord = this.textBoxBadWord.Text;
			this.Close();
		}

		private void QueryGoodSpelling_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (String.IsNullOrEmpty(m_strBadWord) && (DialogResult == DialogResult.OK))
			{
				MessageBox.Show("You can't have an empty Bad Spelling form. Click Cancel if you didn't mean to add a rule.", SpellingFixerEC.cstrCaption);
				e.Cancel = true;
			}
		}

		private void buttonCancel_Click(object sender, System.EventArgs e)
		{
			// this.DialogResult = DialogResult.Cancel;
			this.Close();
		}

		private void textBoxBadWord_GotFocus(object sender, EventArgs e)
		{
			UpdateUniCodes(this.textBoxBadWord.Text);
		}

		private void textBoxReplacement_GotFocus(object sender, EventArgs e)
		{
			UpdateUniCodes(this.textBoxReplacement.Text);
		}

		private void UpdateUniCodes(string strInputString)
		{
			int nLenString = strInputString.Length;

			string strWhole = null, strPiece = null, strUPiece = null;
			foreach(char ch in strInputString)
			{
				if( ch == 0 )   // sometimes it's null (esp. for utf32)
					strPiece = "nul (u0000)  ";
				else
				{
					strUPiece = String.Format("{0:X}", (int)ch);

					// left pad with 0's (there may be a better way to do this, but
					//  I don't know what it is)
					while(strUPiece.Length < 4)  strUPiece = "0" + strUPiece;

					strPiece = String.Format("{0:#} (u{1,4})  ", ch, strUPiece);
				}
				strWhole += strPiece;
			}

			this.labelUniCodes.Text = strWhole;
		}

		private void textBoxBadWord_TextChanged(object sender, EventArgs e)
		{
			UpdateUniCodes(this.textBoxBadWord.Text);
		}

		private void textBoxReplacement_TextChanged(object sender, EventArgs e)
		{
			UpdateUniCodes(this.textBoxReplacement.Text);
		}

		private void buttonDelete_Click(object sender, System.EventArgs e)
		{
			this.Close();
		}
	}
}

// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: Form1.cs
// Responsibility: TE Team
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.IO;
using Microsoft.Win32;
using EncCnvtrs;

namespace AddToEncRepository
{
	/// <summary>
	/// Summary description for Form1.
	/// </summary>
	public class AddConverter : System.Windows.Forms.Form
	{
		private EncConverters m_ec;
		private System.Windows.Forms.OpenFileDialog ofDlg;
		private System.Windows.Forms.Button btnClose;
		private System.Windows.Forms.Button btnAdd;
		private System.Windows.Forms.Button btnFindMapping;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox txtMapping;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.TextBox txtConvName;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.TextBox txtFont;
		private System.Windows.Forms.FontDialog fntDlg;
		private System.Windows.Forms.Button btnFont;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.ComboBox cboConvType;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.ComboBox cboProcessType;
		private System.Windows.Forms.Button btnConverterInfo;
		private System.Windows.Forms.Button txtTest;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label lblRepositoryFile;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="Form1"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public AddConverter()
		{
			InitializeComponent();

			m_ec = new EncConverters();

			lblRepositoryFile.Text = EncConverters.GetRepositoryFileName();

			cboConvType.Items.AddRange(new object[] {
				ConvType.Legacy_to_from_Legacy,
				ConvType.Legacy_to_from_Unicode,
				ConvType.Legacy_to_Legacy,
				ConvType.Legacy_to_Unicode,
				ConvType.Unicode_to_from_Legacy,
				ConvType.Unicode_to_from_Unicode,
				ConvType.Unicode_to_Legacy,
				ConvType.Unicode_to_Unicode});

			cboProcessType.Items.AddRange(new object[] {
				ProcessTypeFlags.CodePageConversion,
				ProcessTypeFlags.DontKnow,
				ProcessTypeFlags.ICUConverter,
				ProcessTypeFlags.ICUTransliteration,
				ProcessTypeFlags.NonUnicodeEncodingConversion,
				ProcessTypeFlags.Transliteration,
				ProcessTypeFlags.UnicodeEncodingConversion});

			cboConvType.SelectedItem = ConvType.Legacy_to_from_Unicode;
			cboProcessType.SelectedItem = ProcessTypeFlags.UnicodeEncodingConversion;
		}

		private void AddToRepository()
		{
//
//
//
//
//
//
//
//			string strFontName = "SILDoulous IPA93";
//			encodingName = "SIL-IPA93-2002";
//			GetECs.AddFont(strFontName, 42, encodingName);
//
//			strFontName = "SILManuscript IPA93";
//			GetECs.AddFont(strFontName, 42, encodingName);
//
//			strFontName = "SILSophia IPA93";
//			GetECs.AddFont(strFontName, 42, encodingName);
//
//			strMapName = "UniDevanagri<>UniIPA";
//			GetECs.AddConversionMap(strMapName,strMapPath + "UDev2UIpa.tec",
//				ConvType.Unicode_to_from_Unicode, EncConverters.strTypeSILtec,becomes,"Unicode IPA",
//				ProcessTypeFlags.Transliteration);
//
//			strFontName = "Doulous SIL";
//			GetECs.AddUnicodeFontEncoding(strFontName, "Unicode IPA");
//			GetECs.AddUnicodeFontEncoding(strFontName, "Unicode Greek");
//
//			GetECs.AddFontMapping("IPA93","SILDoulous IPA93","Doulous SIL");
//
//			GetECs.AddConversionMap("Annapurna<>IPA93",null,ConvType.Legacy_to_from_Legacy,
//				EncConverters.strTypeSILcomp,null,null,ProcessTypeFlags.Transliteration);
//
//			GetECs.AddCompoundConverterStep("Annapurna<>IPA93","Annapurna",true,NormalizeFlags.None);
//			GetECs.AddCompoundConverterStep("Annapurna<>IPA93",strMapName,true,NormalizeFlags.FullyComposed);
//			GetECs.AddCompoundConverterStep("Annapurna<>IPA93","IPA93",false,NormalizeFlags.FullyDecomposed);
//
//			strMapName = "Devanagri<>Latin(ICU)";
//			GetECs.AddConversionMap(strMapName,"Devanagari-Latin", ConvType.Unicode_to_from_Unicode,
//				"ICU.trans","Unicode Devanagari",null,ProcessTypeFlags.ICUTransliteration);
//
//			strMapName = "UTF-8<>UTF-16";
//			GetECs.Add(strMapName,"UTF-8",ConvType.Unicode_to_from_Unicode,
//				null,null,ProcessTypeFlags.ICUConverter);
//
//			GetECs.Add(strMapName,"65001",ConvType.Unicode_to_from_Unicode,
//				null,null,ProcessTypeFlags.CodePageConversion);
//
//			GetECs.AddConversionMap(strMapName,"EncodingFormConversionRequest",
//				ConvType.Unicode_to_from_Unicode,EncConverters.strTypeSILtecForm,null,null,
//				ProcessTypeFlags.DontKnow);
//
//			try
//			{
//				GetECs.RemoveAlias("SIL-ANNAPURNA_05");
//			}
//			catch(COMException e)
//			{
//				MessageBox.Show(e.Message);
//			}
//			try
//			{
//				// this *should* fail because we've already removed it by the alias name
//				GetECs.Remove("Annapurna");
//			}
//				// what isn't this working???
//			catch(COMException e)
//			{
//				MessageBox.Show(e.Message);
//				// System.Diagnostics.Debug.Assert(e.);
//				// Assert(e == ErrStatus.NoAliasName);
//			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged
		/// resources; <c>false</c> to release only unmanaged resources.
		/// </param>
		/// -----------------------------------------------------------------------------------
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			this.ofDlg = new System.Windows.Forms.OpenFileDialog();
			this.btnClose = new System.Windows.Forms.Button();
			this.btnAdd = new System.Windows.Forms.Button();
			this.btnFindMapping = new System.Windows.Forms.Button();
			this.label2 = new System.Windows.Forms.Label();
			this.txtMapping = new System.Windows.Forms.TextBox();
			this.label3 = new System.Windows.Forms.Label();
			this.txtConvName = new System.Windows.Forms.TextBox();
			this.btnFont = new System.Windows.Forms.Button();
			this.label4 = new System.Windows.Forms.Label();
			this.txtFont = new System.Windows.Forms.TextBox();
			this.fntDlg = new System.Windows.Forms.FontDialog();
			this.cboConvType = new System.Windows.Forms.ComboBox();
			this.label5 = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.cboProcessType = new System.Windows.Forms.ComboBox();
			this.btnConverterInfo = new System.Windows.Forms.Button();
			this.txtTest = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.lblRepositoryFile = new System.Windows.Forms.Label();
			this.SuspendLayout();
			//
			// ofDlg
			//
			this.ofDlg.DefaultExt = "tec";
			this.ofDlg.Filter = "Compiled TECkit (*.tec)|*.tec|TECkit Mapping (*.map)|*.map|CC Table (*.cc, *.cct)" +
				"|*.cc;*.cct|All Files (*.*)|*.*";
			this.ofDlg.Title = "Find Conversion Mapping File";
			//
			// btnClose
			//
			this.btnClose.Anchor = (System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right);
			this.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnClose.Location = new System.Drawing.Point(384, 248);
			this.btnClose.Name = "btnClose";
			this.btnClose.TabIndex = 15;
			this.btnClose.Text = "Close";
			this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
			//
			// btnAdd
			//
			this.btnAdd.Anchor = (System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left);
			this.btnAdd.Location = new System.Drawing.Point(8, 248);
			this.btnAdd.Name = "btnAdd";
			this.btnAdd.Size = new System.Drawing.Size(112, 23);
			this.btnAdd.TabIndex = 12;
			this.btnAdd.Text = "A&dd To Repository";
			this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
			//
			// btnFindMapping
			//
			this.btnFindMapping.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right);
			this.btnFindMapping.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.btnFindMapping.Location = new System.Drawing.Point(433, 64);
			this.btnFindMapping.Name = "btnFindMapping";
			this.btnFindMapping.Size = new System.Drawing.Size(24, 20);
			this.btnFindMapping.TabIndex = 2;
			this.btnFindMapping.Text = "...";
			this.btnFindMapping.Click += new System.EventHandler(this.btnFindMapping_Click);
			//
			// label2
			//
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(8, 48);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(125, 13);
			this.label2.TabIndex = 0;
			this.label2.Text = "Converter &Mapping File:";
			//
			// txtMapping
			//
			this.txtMapping.Anchor = ((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right);
			this.txtMapping.Location = new System.Drawing.Point(8, 64);
			this.txtMapping.Name = "txtMapping";
			this.txtMapping.Size = new System.Drawing.Size(424, 20);
			this.txtMapping.TabIndex = 1;
			this.txtMapping.Text = "";
			//
			// label3
			//
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(8, 96);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(193, 13);
			this.label3.TabIndex = 3;
			this.label3.Text = "Converter &Name (i.e. Friendly Name):";
			//
			// txtConvName
			//
			this.txtConvName.Anchor = ((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right);
			this.txtConvName.Location = new System.Drawing.Point(8, 112);
			this.txtConvName.Name = "txtConvName";
			this.txtConvName.Size = new System.Drawing.Size(424, 20);
			this.txtConvName.TabIndex = 4;
			this.txtConvName.Text = "";
			//
			// btnFont
			//
			this.btnFont.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right);
			this.btnFont.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.btnFont.Location = new System.Drawing.Point(433, 160);
			this.btnFont.Name = "btnFont";
			this.btnFont.Size = new System.Drawing.Size(24, 20);
			this.btnFont.TabIndex = 7;
			this.btnFont.Text = "...";
			this.btnFont.Click += new System.EventHandler(this.btnFont_Click);
			//
			// label4
			//
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(8, 144);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(164, 13);
			this.label4.TabIndex = 5;
			this.label4.Text = "&Font Associated with Converter:";
			//
			// txtFont
			//
			this.txtFont.Anchor = ((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right);
			this.txtFont.Location = new System.Drawing.Point(8, 160);
			this.txtFont.Name = "txtFont";
			this.txtFont.Size = new System.Drawing.Size(424, 20);
			this.txtFont.TabIndex = 6;
			this.txtFont.Text = "";
			//
			// fntDlg
			//
			this.fntDlg.AllowScriptChange = false;
			this.fntDlg.AllowSimulations = false;
			this.fntDlg.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
			this.fntDlg.FontMustExist = true;
			this.fntDlg.MaxSize = 12;
			this.fntDlg.MinSize = 12;
			this.fntDlg.ShowEffects = false;
			//
			// cboConvType
			//
			this.cboConvType.Location = new System.Drawing.Point(8, 208);
			this.cboConvType.Name = "cboConvType";
			this.cboConvType.Size = new System.Drawing.Size(200, 21);
			this.cboConvType.TabIndex = 9;
			//
			// label5
			//
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(8, 192);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(85, 13);
			this.label5.TabIndex = 8;
			this.label5.Text = "C&onverter Type:";
			//
			// label6
			//
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(224, 192);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(76, 13);
			this.label6.TabIndex = 10;
			this.label6.Text = "&Process Type:";
			//
			// cboProcessType
			//
			this.cboProcessType.Location = new System.Drawing.Point(224, 208);
			this.cboProcessType.Name = "cboProcessType";
			this.cboProcessType.Size = new System.Drawing.Size(200, 21);
			this.cboProcessType.TabIndex = 11;
			//
			// btnConverterInfo
			//
			this.btnConverterInfo.Anchor = (System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left);
			this.btnConverterInfo.Location = new System.Drawing.Point(128, 248);
			this.btnConverterInfo.Name = "btnConverterInfo";
			this.btnConverterInfo.Size = new System.Drawing.Size(96, 23);
			this.btnConverterInfo.TabIndex = 13;
			this.btnConverterInfo.Text = "Converter Info.";
			this.btnConverterInfo.Click += new System.EventHandler(this.btnConverterInfo_Click);
			//
			// txtTest
			//
			this.txtTest.Anchor = (System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left);
			this.txtTest.Location = new System.Drawing.Point(232, 248);
			this.txtTest.Name = "txtTest";
			this.txtTest.Size = new System.Drawing.Size(96, 23);
			this.txtTest.TabIndex = 14;
			this.txtTest.Text = "&Test Converter";
			this.txtTest.Click += new System.EventHandler(this.txtTest_Click);
			//
			// label1
			//
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(8, 8);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(83, 13);
			this.label1.TabIndex = 16;
			this.label1.Text = "Repository File:";
			//
			// lblRepositoryFile
			//
			this.lblRepositoryFile.Anchor = ((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right);
			this.lblRepositoryFile.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.lblRepositoryFile.Location = new System.Drawing.Point(8, 24);
			this.lblRepositoryFile.Name = "lblRepositoryFile";
			this.lblRepositoryFile.Size = new System.Drawing.Size(448, 16);
			this.lblRepositoryFile.TabIndex = 17;
			this.lblRepositoryFile.Text = "#";
			//
			// AddConverter
			//
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.CancelButton = this.btnClose;
			this.ClientSize = new System.Drawing.Size(472, 278);
			this.Controls.AddRange(new System.Windows.Forms.Control[] {
																		  this.lblRepositoryFile,
																		  this.label1,
																		  this.txtTest,
																		  this.btnConverterInfo,
																		  this.label6,
																		  this.label5,
																		  this.label4,
																		  this.label3,
																		  this.label2,
																		  this.cboProcessType,
																		  this.cboConvType,
																		  this.btnFont,
																		  this.txtFont,
																		  this.txtConvName,
																		  this.btnFindMapping,
																		  this.txtMapping,
																		  this.btnAdd,
																		  this.btnClose});
			this.Name = "AddConverter";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Thrown Together Tool For Adding to Encoding Repository";
			this.ResumeLayout(false);

		}
		#endregion

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[STAThread]
		static void Main()
		{
			Application.Run(new AddConverter());
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// -----------------------------------------------------------------------------------
		private void btnFindMapping_Click(object sender, System.EventArgs e)
		{
			txtMapping.Text = txtMapping.Text.Trim();

			if (txtMapping.Text != string.Empty)
				ofDlg.FileName = txtMapping.Text;

			if (ofDlg.ShowDialog(this) == DialogResult.OK)
				txtMapping.Text = ofDlg.FileName;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// -----------------------------------------------------------------------------------
		private void btnFont_Click(object sender, System.EventArgs e)
		{
			if (txtFont.Text != string.Empty)
				fntDlg.Font = new Font(txtFont.Text.Trim(), 12);

			if (fntDlg.ShowDialog(this) == DialogResult.OK)
				txtFont.Text = fntDlg.Font.Name;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// -----------------------------------------------------------------------------------
		private void btnAdd_Click(object sender, System.EventArgs e)
		{
			m_ec.Add(txtConvName.Text, txtMapping.Text, (ConvType)cboConvType.SelectedItem,
				string.Empty, string.Empty,
				(ProcessTypeFlags)cboProcessType.SelectedItem);

			m_ec.AddFont(txtFont.Text, 0, string.Empty);

			MessageBox.Show(this, "'" + txtConvName.Text + "' has been added to repository.");
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// -----------------------------------------------------------------------------------
		private void btnClose_Click(object sender, System.EventArgs e)
		{
			Close();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// -----------------------------------------------------------------------------------
		private void btnConverterInfo_Click(object sender, System.EventArgs e)
		{
			if (m_ec.Count == 0)
			{
				MessageBox.Show(this, "No Converters for which to show information.");
				return;
			}

			frmConverterInfo frm = new frmConverterInfo(m_ec);
			frm.ShowDialog(this);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// -----------------------------------------------------------------------------------
		private void txtTest_Click(object sender, System.EventArgs e)
		{
			TestConversion testDlg = new TestConversion(m_ec);
			testDlg.ShowDialog(this);
		}
	}
}

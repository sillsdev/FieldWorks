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
// File: TestConversion.cs
// Responsibility: DavidO
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
using System.IO;
using System.Text;
using EncCnvtrs;

namespace AddToEncRepository
{
	/// <summary>
	/// Summary description for TestConversion.
	/// </summary>
	public class TestConversion : System.Windows.Forms.Form
	{
		private EncConverters m_ec;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.TextBox txtInput;
		private System.Windows.Forms.Splitter splitter1;
		private System.Windows.Forms.TextBox txtOutput;
		private System.Windows.Forms.Label lblOriginal;
		private System.Windows.Forms.Label lblConverted;
		private System.Windows.Forms.ComboBox cboConverters;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Button btnConvert;
		private System.Windows.Forms.Button btnBrowse;
		private System.Windows.Forms.OpenFileDialog ofDlg;
		private System.Windows.Forms.Label lblOrigFile;
		private System.Windows.Forms.ToolTip toolTip1;
		private System.ComponentModel.IContainer components;

		#region Constructor, Destructor and initialization
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TestConversion"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public TestConversion(EncConverters converters)
		{
			InitializeComponent();

			lblOrigFile.Text = string.Empty;
			m_ec = converters;

			LoadConverterNames();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void LoadConverterNames()
		{
			cboConverters.Items.Clear();
			cboConverters.Text = string.Empty;

			foreach (string mapping in m_ec.Mappings)
				cboConverters.Items.Add(mapping);

			if (cboConverters.Items.Count > 0)
				cboConverters.SelectedIndex = 0;
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
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#endregion

		#region Windows Form Designer generated code
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.panel1 = new System.Windows.Forms.Panel();
			this.txtOutput = new System.Windows.Forms.TextBox();
			this.splitter1 = new System.Windows.Forms.Splitter();
			this.txtInput = new System.Windows.Forms.TextBox();
			this.btnConvert = new System.Windows.Forms.Button();
			this.lblOriginal = new System.Windows.Forms.Label();
			this.lblConverted = new System.Windows.Forms.Label();
			this.cboConverters = new System.Windows.Forms.ComboBox();
			this.label7 = new System.Windows.Forms.Label();
			this.btnBrowse = new System.Windows.Forms.Button();
			this.ofDlg = new System.Windows.Forms.OpenFileDialog();
			this.lblOrigFile = new System.Windows.Forms.Label();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.panel1.SuspendLayout();
			this.SuspendLayout();
			//
			// panel1
			//
			this.panel1.Controls.AddRange(new System.Windows.Forms.Control[] {
																				 this.txtOutput,
																				 this.splitter1,
																				 this.txtInput});
			this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel1.Location = new System.Drawing.Point(4, 24);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(608, 326);
			this.panel1.TabIndex = 1;
			//
			// txtOutput
			//
			this.txtOutput.Dock = System.Windows.Forms.DockStyle.Fill;
			this.txtOutput.Location = new System.Drawing.Point(306, 0);
			this.txtOutput.Multiline = true;
			this.txtOutput.Name = "txtOutput";
			this.txtOutput.ReadOnly = true;
			this.txtOutput.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.txtOutput.Size = new System.Drawing.Size(302, 326);
			this.txtOutput.TabIndex = 1;
			this.txtOutput.Text = "";
			this.txtOutput.WordWrap = false;
			//
			// splitter1
			//
			this.splitter1.Location = new System.Drawing.Point(301, 0);
			this.splitter1.Name = "splitter1";
			this.splitter1.Size = new System.Drawing.Size(5, 326);
			this.splitter1.TabIndex = 9;
			this.splitter1.TabStop = false;
			this.splitter1.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.splitter1_SplitterMoved);
			//
			// txtInput
			//
			this.txtInput.Dock = System.Windows.Forms.DockStyle.Left;
			this.txtInput.Font = new System.Drawing.Font("Arial Unicode MS", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.txtInput.Multiline = true;
			this.txtInput.Name = "txtInput";
			this.txtInput.ReadOnly = true;
			this.txtInput.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.txtInput.Size = new System.Drawing.Size(301, 326);
			this.txtInput.TabIndex = 0;
			this.txtInput.Text = "";
			this.txtInput.WordWrap = false;
			//
			// btnConvert
			//
			this.btnConvert.Anchor = (System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right);
			this.btnConvert.Enabled = false;
			this.btnConvert.Location = new System.Drawing.Point(504, 360);
			this.btnConvert.Name = "btnConvert";
			this.btnConvert.TabIndex = 3;
			this.btnConvert.Text = "&Convert";
			this.btnConvert.Click += new System.EventHandler(this.btnConvert_Click);
			//
			// lblOriginal
			//
			this.lblOriginal.AutoSize = true;
			this.lblOriginal.Location = new System.Drawing.Point(8, 8);
			this.lblOriginal.Name = "lblOriginal";
			this.lblOriginal.Size = new System.Drawing.Size(47, 13);
			this.lblOriginal.TabIndex = 4;
			this.lblOriginal.Text = "Original:";
			//
			// lblConverted
			//
			this.lblConverted.AutoSize = true;
			this.lblConverted.Location = new System.Drawing.Point(314, 8);
			this.lblConverted.Name = "lblConverted";
			this.lblConverted.Size = new System.Drawing.Size(56, 13);
			this.lblConverted.TabIndex = 6;
			this.lblConverted.Text = "Converted";
			//
			// cboConverters
			//
			this.cboConverters.Anchor = ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right);
			this.cboConverters.Location = new System.Drawing.Point(116, 359);
			this.cboConverters.Name = "cboConverters";
			this.cboConverters.Size = new System.Drawing.Size(196, 21);
			this.cboConverters.TabIndex = 1;
			//
			// label7
			//
			this.label7.Anchor = (System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left);
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(8, 362);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(112, 13);
			this.label7.TabIndex = 0;
			this.label7.Text = "&Available Converters:";
			//
			// btnBrowse
			//
			this.btnBrowse.Anchor = (System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right);
			this.btnBrowse.Location = new System.Drawing.Point(352, 360);
			this.btnBrowse.Name = "btnBrowse";
			this.btnBrowse.Size = new System.Drawing.Size(120, 23);
			this.btnBrowse.TabIndex = 2;
			this.btnBrowse.Text = "&Specify Input File...";
			this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
			//
			// ofDlg
			//
			this.ofDlg.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
			//
			// lblOrigFile
			//
			this.lblOrigFile.Location = new System.Drawing.Point(56, 8);
			this.lblOrigFile.Name = "lblOrigFile";
			this.lblOrigFile.Size = new System.Drawing.Size(240, 13);
			this.lblOrigFile.TabIndex = 5;
			this.lblOrigFile.Text = "#";
			//
			// TestConversion
			//
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(616, 390);
			this.Controls.AddRange(new System.Windows.Forms.Control[] {
																		  this.btnBrowse,
																		  this.cboConverters,
																		  this.label7,
																		  this.lblConverted,
																		  this.lblOriginal,
																		  this.btnConvert,
																		  this.panel1,
																		  this.lblOrigFile});
			this.DockPadding.Bottom = 40;
			this.DockPadding.Left = 4;
			this.DockPadding.Right = 4;
			this.DockPadding.Top = 24;
			this.Name = "TestConversion";
			this.Text = "Test Conversion";
			this.panel1.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		/// -----------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="encoding"></param>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		private System.Text.Encoding TranslateEncoding(EncCnvtrs.EncodingForm encoding)
		{
			System.Text.Encoding dotNetEncoding = System.Text.Encoding.Default;

			switch (encoding)
			{
				case EncCnvtrs.EncodingForm.Unspecified:
				case EncCnvtrs.EncodingForm.LegacyString:
				case EncCnvtrs.EncodingForm.LegacyBytes:
					dotNetEncoding = System.Text.Encoding.ASCII;
					break;

				case EncCnvtrs.EncodingForm.UTF16BE:
				case EncCnvtrs.EncodingForm.UTF32BE:
					dotNetEncoding =  System.Text.Encoding.BigEndianUnicode;
					break;

				case EncCnvtrs.EncodingForm.UTF16:
				case EncCnvtrs.EncodingForm.UTF32:
					dotNetEncoding = System.Text.Encoding.Unicode;
					break;

				case EncCnvtrs.EncodingForm.UTF8Bytes:
				case EncCnvtrs.EncodingForm.UTF8String:
					dotNetEncoding = System.Text.Encoding.UTF8;
					break;

				default:
					break;
			}

			return dotNetEncoding;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="converterName"></param>
		/// <param name="outputFileName"></param>
		/// <param name="outEnc"></param>
		/// <param name="inputFileName"></param>
		/// <param name="inEnc"></param>
		/// <param name="directionForward"></param>
		/// -----------------------------------------------------------------------------------
		public void DoFileConvert(EncConverter ec, string inputFilename)
		{
			// open the input and output files using the given encoding formats
			StreamReader reader = new StreamReader(inputFilename,
				TranslateEncoding(ec.EncodingIn), true);

			reader.BaseStream.Seek(0, SeekOrigin.Begin);

			// read the lines of the input file, (optionally convert,) and write them out.
			string sOutput = string.Empty, sInput;

			StringBuilder sbIn = new StringBuilder();
			StringBuilder sbOut = new StringBuilder();

			while (reader.Peek() > -1)
			{
				sInput = reader.ReadLine();

				sbIn.Append(sInput);
				sbIn.Append(new char[] {(char)0x0D, (char)0x0A});

				if (sInput == string.Empty || sInput.StartsWith(@"\_sh ") || sInput.StartsWith(@"\id "))
				{
					sOutput = sInput;
					sbOut.Append(sOutput);
					sbOut.Append(new char[] {(char)0x0D, (char)0x0A});
				}
				else
				{
					sOutput = ConvertedLine(ec, sInput);
					sbOut.Append(sOutput);
					sbOut.Append(new char[] {(char)0x0D, (char)0x0A});
				}
			}

			reader.Close();

			txtInput.Text = sbIn.ToString();
			txtOutput.Text = sbOut.ToString();

			for (int i = 0; i < m_ec.Count; i++)
			{
				if (((EncConverter)m_ec[i]).Name == ec.Name)
					txtOutput.Font = new Font(m_ec.Fonts[i], 12);
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="ec"></param>
		/// <param name="sInput"></param>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		private string ConvertedLine(EncConverter ec, string sInput)
		{
			string marker = string.Empty;
			string data = sInput;

			if (sInput.StartsWith(@"\"))
			{
				int i = sInput.IndexOf(" ");
				if (i >= 0)
				{
					marker = sInput.Substring(0, i);
					data = sInput.Substring(i);
				}
			}

			return marker + ec.Convert(data);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// -----------------------------------------------------------------------------------
		private void btnConvert_Click(object sender, System.EventArgs e)
		{
			if (cboConverters.Items.Count > 0 && cboConverters.SelectedIndex >= 0)
				DoFileConvert((EncConverter)m_ec[cboConverters.SelectedItem], ofDlg.FileName);

			cboConverters.Focus();
			btnConvert.Enabled = false;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// -----------------------------------------------------------------------------------
		private void splitter1_SplitterMoved(object sender, System.Windows.Forms.SplitterEventArgs e)
		{
			lblConverted.Left = panel1.Left + splitter1.Left + splitter1.Width + 4;
			lblOrigFile.Width = splitter1.Left - 60;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// -----------------------------------------------------------------------------------
		private void btnBrowse_Click(object sender, System.EventArgs e)
		{
			if (ofDlg.ShowDialog() == DialogResult.Cancel)
				ofDlg.FileName = string.Empty;
			else
			{
				lblOrigFile.Text = ofDlg.FileName;
				this.toolTip1.SetToolTip(lblOrigFile, ofDlg.FileName);
				btnConvert.Enabled = true;
			}
		}
	}
}

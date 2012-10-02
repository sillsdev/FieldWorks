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
// File: ConverterInfo.cs
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
using EncCnvtrs;

namespace AddToEncRepository
{
	/// <summary>
	/// Summary description for ConverterInfo.
	/// </summary>
	public class frmConverterInfo : System.Windows.Forms.Form
	{
		private EncConverters m_ec;

		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.ListView lvConverterInfo;
		private System.Windows.Forms.ColumnHeader columnHeader1;
		private System.Windows.Forms.ColumnHeader columnHeader2;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.ComboBox cboConverters;
		private System.Windows.Forms.Button btnRemove;
		private System.Windows.Forms.ToolTip toolTip1;
		private System.ComponentModel.IContainer components;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ConverterInfo"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public frmConverterInfo(EncConverters converters)
		{
			InitializeComponent();

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
			else
				lvConverterInfo.Items.Clear();

			cboConverters.Focus();
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
			this.button1 = new System.Windows.Forms.Button();
			this.lvConverterInfo = new System.Windows.Forms.ListView();
			this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
			this.columnHeader2 = new System.Windows.Forms.ColumnHeader();
			this.cboConverters = new System.Windows.Forms.ComboBox();
			this.label7 = new System.Windows.Forms.Label();
			this.btnRemove = new System.Windows.Forms.Button();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.SuspendLayout();
			//
			// button1
			//
			this.button1.Anchor = (System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right);
			this.button1.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.button1.Location = new System.Drawing.Point(400, 248);
			this.button1.Name = "button1";
			this.button1.TabIndex = 3;
			this.button1.Text = "Close";
			this.button1.Click += new System.EventHandler(this.button1_Click);
			//
			// lvConverterInfo
			//
			this.lvConverterInfo.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
																							  this.columnHeader1,
																							  this.columnHeader2});
			this.lvConverterInfo.Dock = System.Windows.Forms.DockStyle.Fill;
			this.lvConverterInfo.FullRowSelect = true;
			this.lvConverterInfo.Location = new System.Drawing.Point(4, 4);
			this.lvConverterInfo.MultiSelect = false;
			this.lvConverterInfo.Name = "lvConverterInfo";
			this.lvConverterInfo.Size = new System.Drawing.Size(488, 234);
			this.lvConverterInfo.TabIndex = 4;
			this.lvConverterInfo.View = System.Windows.Forms.View.Details;
			//
			// columnHeader1
			//
			this.columnHeader1.Text = "Converter Property";
			this.columnHeader1.Width = 120;
			//
			// columnHeader2
			//
			this.columnHeader2.Text = "Value";
			this.columnHeader2.Width = 300;
			//
			// cboConverters
			//
			this.cboConverters.Anchor = ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right);
			this.cboConverters.Location = new System.Drawing.Point(116, 248);
			this.cboConverters.Name = "cboConverters";
			this.cboConverters.Size = new System.Drawing.Size(168, 21);
			this.cboConverters.TabIndex = 1;
			this.cboConverters.SelectedIndexChanged += new System.EventHandler(this.cboConverters_SelectedIndexChanged);
			//
			// label7
			//
			this.label7.Anchor = (System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left);
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(8, 251);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(112, 13);
			this.label7.TabIndex = 0;
			this.label7.Text = "&Available Converters:";
			//
			// btnRemove
			//
			this.btnRemove.Anchor = (System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right);
			this.btnRemove.Location = new System.Drawing.Point(304, 248);
			this.btnRemove.Name = "btnRemove";
			this.btnRemove.TabIndex = 2;
			this.btnRemove.Text = "&Remove";
			this.toolTip1.SetToolTip(this.btnRemove, "Removes a converter from the repository");
			this.btnRemove.Click += new System.EventHandler(this.btnRemove_Click);
			//
			// frmConverterInfo
			//
			this.AcceptButton = this.button1;
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.CancelButton = this.button1;
			this.ClientSize = new System.Drawing.Size(496, 278);
			this.Controls.AddRange(new System.Windows.Forms.Control[] {
																		  this.btnRemove,
																		  this.cboConverters,
																		  this.label7,
																		  this.lvConverterInfo,
																		  this.button1});
			this.DockPadding.Bottom = 40;
			this.DockPadding.Left = 4;
			this.DockPadding.Right = 4;
			this.DockPadding.Top = 4;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "frmConverterInfo";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Converter Info.";
			this.ResumeLayout(false);

		}
		#endregion

		/// -----------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// -----------------------------------------------------------------------------------
		private void cboConverters_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			lvConverterInfo.Items.Clear();

			if (cboConverters.Items.Count == 0 || cboConverters.SelectedIndex < 0)
				return;

			EncConverter ec =  (EncConverter)m_ec[cboConverters.SelectedItem];

			ListViewItem lvi;

			lvi = new ListViewItem(new string[] {"CodePageInput", ec.CodePageInput.ToString()});
			lvConverterInfo.Items.Add(lvi);
			lvi = new ListViewItem(new string[] {"CodePageOutput", ec.CodePageOutput.ToString()});
			lvConverterInfo.Items.Add(lvi);
			lvi = new ListViewItem(new string[] {"Type", ec.ConversionType.ToString()});
			lvConverterInfo.Items.Add(lvi);
			lvi = new ListViewItem(new string[] {"Identifier", ec.ConverterIdentifier});
			lvConverterInfo.Items.Add(lvi);
			lvi = new ListViewItem(new string[] {"DirectionForward", ec.DirectionForward.ToString()});
			lvConverterInfo.Items.Add(lvi);
			lvi = new ListViewItem(new string[] {"EncodingIn", ec.EncodingIn.ToString()});
			lvConverterInfo.Items.Add(lvi);
			lvi = new ListViewItem(new string[] {"EncodingOut", ec.EncodingOut.ToString()});
			lvConverterInfo.Items.Add(lvi);
			lvi = new ListViewItem(new string[] {"ImplementType", ec.ImplementType});
			lvConverterInfo.Items.Add(lvi);
			lvi = new ListViewItem(new string[] {"ProcessType", ec.ProcessType.ToString()});
			lvConverterInfo.Items.Add(lvi);
			lvi = new ListViewItem(new string[] {"LeftEncodingID", ec.LeftEncodingID});
			lvConverterInfo.Items.Add(lvi);
			lvi = new ListViewItem(new string[] {"RightEncodingID", ec.RightEncodingID});
			lvConverterInfo.Items.Add(lvi);
			lvi = new ListViewItem(new string[] {"NormalizeOutput", ec.NormalizeOutput.ToString()});
			lvConverterInfo.Items.Add(lvi);
			lvi = new ListViewItem(new string[] {"ProgramID", ec.ProgramID});
			lvConverterInfo.Items.Add(lvi);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// -----------------------------------------------------------------------------------
		private void button1_Click(object sender, System.EventArgs e)
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
		private void btnRemove_Click(object sender, System.EventArgs e)
		{
			if (cboConverters.Items.Count == 0 || cboConverters.SelectedIndex < 0)
				return;

			m_ec.Remove(cboConverters.SelectedItem);

			MessageBox.Show(this, "'" + cboConverters.SelectedItem +
				"' has been removed from repository.");

			LoadConverterNames();
		}
	}
}

// Copyright (c) 2004-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: SymbolChooser.cs
// Responsibility: TE Team

using System;
using System.Drawing;
using System.Windows.Forms;
using SIL.FieldWorks.Common.Framework;
using SIL.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.COMInterfaces;
using XCore;

namespace SIL.FieldWorks.TE
{
	/// <summary>
	/// Summary description for SymbolChooser.
	/// </summary>
	public class SymbolChooserDlg : Form, IFWDisposable
	{
		private IHelpTopicProvider m_helpTopicProvider;
		private System.Windows.Forms.Panel panel1;
		private SIL.FieldWorks.Common.Controls.CharacterGrid charGrid;
		private System.Windows.Forms.Label lblFontName;
		private Button btnHelp;
		private Button btnCancel;
		private Button btnOK;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="SymbolChooserDlg"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private SymbolChooserDlg()
		{
			InitializeComponent();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="SymbolChooserDlg"/> class by passing
		/// the font used in the glyph grid.
		/// </summary>
		/// <param name="font">Font used in the glyph grid.</param>
		/// <param name="cpe">An ILgCharacterPropertyEngine. Set this to null to use the
		/// .Net methods for determining whether or not a codepoint should be added to
		/// the grid.</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// ------------------------------------------------------------------------------------
		public SymbolChooserDlg(Font font, ILgCharacterPropertyEngine cpe, IHelpTopicProvider helpTopicProvider)
			: this()
		{
			m_helpTopicProvider = helpTopicProvider;
			charGrid.CharPropEngine = cpe;
			charGrid.Font = font;
			lblFontName.Text = font.Name;
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

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged
		/// resources; <c>false</c> to release only unmanaged resources.
		/// </param>
		/// -----------------------------------------------------------------------------------
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

		#region Windows Form Designer generated code
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			System.Windows.Forms.Label label1;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SymbolChooserDlg));
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
			this.btnHelp = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.btnOK = new System.Windows.Forms.Button();
			this.panel1 = new System.Windows.Forms.Panel();
			this.charGrid = new SIL.FieldWorks.Common.Controls.CharacterGrid();
			this.lblFontName = new System.Windows.Forms.Label();
			label1 = new System.Windows.Forms.Label();
			this.panel1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.charGrid)).BeginInit();
			this.SuspendLayout();
			//
			// label1
			//
			resources.ApplyResources(label1, "label1");
			label1.BackColor = System.Drawing.Color.Transparent;
			label1.Name = "label1";
			//
			// btnHelp
			//
			resources.ApplyResources(this.btnHelp, "btnHelp");
			this.btnHelp.Name = "btnHelp";
			this.btnHelp.Click += new System.EventHandler(this.btnHelp_Click);
			//
			// btnCancel
			//
			resources.ApplyResources(this.btnCancel, "btnCancel");
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Name = "btnCancel";
			//
			// btnOK
			//
			resources.ApplyResources(this.btnOK, "btnOK");
			this.btnOK.Name = "btnOK";
			this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
			//
			// panel1
			//
			resources.ApplyResources(this.panel1, "panel1");
			this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.panel1.Controls.Add(this.charGrid);
			this.panel1.Name = "panel1";
			//
			// charGrid
			//
			resources.ApplyResources(this.charGrid, "charGrid");
			this.charGrid.AllowUserToAddRows = false;
			this.charGrid.AllowUserToDeleteRows = false;
			this.charGrid.AllowUserToResizeColumns = false;
			this.charGrid.AllowUserToResizeRows = false;
			this.charGrid.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.charGrid.ColumnHeadersVisible = false;
			this.charGrid.Cursor = System.Windows.Forms.Cursors.Default;
			dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
			dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Window;
			dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.ControlText;
			dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.charGrid.DefaultCellStyle = dataGridViewCellStyle1;
			this.charGrid.MultiSelect = false;
			this.charGrid.Name = "charGrid";
			this.charGrid.ReadOnly = true;
			this.charGrid.RowHeadersVisible = false;
			this.charGrid.ShowCellToolTips = false;
			this.charGrid.VirtualMode = true;
			this.charGrid.DoubleClick += new System.EventHandler(this.charGrid_DoubleClick);
			//
			// lblFontName
			//
			resources.ApplyResources(this.lblFontName, "lblFontName");
			this.lblFontName.BackColor = System.Drawing.Color.Transparent;
			this.lblFontName.Name = "lblFontName";
			//
			// SymbolChooserDlg
			//
			this.AcceptButton = this.btnOK;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.btnCancel;
			this.Controls.Add(this.lblFontName);
			this.Controls.Add(label1);
			this.Controls.Add(this.btnHelp);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnOK);
			this.Controls.Add(this.panel1);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "SymbolChooserDlg";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.panel1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.charGrid)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the chosen symbol.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ChosenSymbol
		{
			get
			{
				CheckDisposed();
				return charGrid.CurrentCharacter;
			}
			set
			{
				CheckDisposed();
				charGrid.CurrentCharacter = (string.IsNullOrEmpty(value) ? "*" : value);
			}
		}
		#endregion

		#region Overridden Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			Show();
		}
		#endregion

		#region Event Handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void btnOK_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			Close();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Display help for this dialog.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtpSymbolChoose");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void charGrid_DoubleClick(object sender, EventArgs e)
		{
			btnOK_Click(null, null);
			DialogResult = DialogResult.OK;
		}
		#endregion
	}
}

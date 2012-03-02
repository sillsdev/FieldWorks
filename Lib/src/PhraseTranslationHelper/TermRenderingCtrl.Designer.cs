// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TermRenderingCtrl.cs
// Responsibility: bogle
// ---------------------------------------------------------------------------------------------
namespace SILUBS.PhraseTranslationHelper
{
	partial class TermRenderingCtrl
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.Windows.Forms.ToolStripMenuItem mnuSetAsDefault;
			System.Windows.Forms.ToolStripMenuItem mnuAddRendering;
			this.m_lblKeyTermColHead = new System.Windows.Forms.Label();
			this.m_lbRenderings = new System.Windows.Forms.ListBox();
			this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.mnuDeleteRendering = new System.Windows.Forms.ToolStripMenuItem();
			mnuSetAsDefault = new System.Windows.Forms.ToolStripMenuItem();
			mnuAddRendering = new System.Windows.Forms.ToolStripMenuItem();
			this.contextMenuStrip.SuspendLayout();
			this.SuspendLayout();
			//
			// mnuSetAsDefault
			//
			mnuSetAsDefault.Name = "mnuSetAsDefault";
			mnuSetAsDefault.Size = new System.Drawing.Size(198, 22);
			mnuSetAsDefault.Text = "&Set as default rendering";
			mnuSetAsDefault.Click += new System.EventHandler(this.mnuSetAsDefault_Click);
			//
			// mnuAddRendering
			//
			mnuAddRendering.Name = "mnuAddRendering";
			mnuAddRendering.Size = new System.Drawing.Size(198, 22);
			mnuAddRendering.Text = "&Add rendering...";
			mnuAddRendering.Click += new System.EventHandler(this.mnuAddRendering_Click);
			//
			// m_lblKeyTermColHead
			//
			this.m_lblKeyTermColHead.Dock = System.Windows.Forms.DockStyle.Top;
			this.m_lblKeyTermColHead.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold);
			this.m_lblKeyTermColHead.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.m_lblKeyTermColHead.Location = new System.Drawing.Point(0, 0);
			this.m_lblKeyTermColHead.Name = "m_lblKeyTermColHead";
			this.m_lblKeyTermColHead.Size = new System.Drawing.Size(148, 20);
			this.m_lblKeyTermColHead.TabIndex = 1;
			this.m_lblKeyTermColHead.Text = "#";
			this.m_lblKeyTermColHead.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			// m_lbRenderings
			//
			this.m_lbRenderings.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_lbRenderings.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
			this.m_lbRenderings.FormattingEnabled = true;
			this.m_lbRenderings.IntegralHeight = false;
			this.m_lbRenderings.Location = new System.Drawing.Point(0, 20);
			this.m_lbRenderings.Name = "m_lbRenderings";
			this.m_lbRenderings.Size = new System.Drawing.Size(148, 18);
			this.m_lbRenderings.Sorted = true;
			this.m_lbRenderings.TabIndex = 2;
			this.m_lbRenderings.MouseUp += new System.Windows.Forms.MouseEventHandler(this.m_lbRenderings_MouseUp);
			this.m_lbRenderings.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.m_lbRenderings_DrawItem);
			this.m_lbRenderings.Resize += new System.EventHandler(this.m_lbRenderings_Resize);
			this.m_lbRenderings.SelectedIndexChanged += new System.EventHandler(this.m_lbRenderings_SelectedIndexChanged);
			this.m_lbRenderings.MouseDown += new System.Windows.Forms.MouseEventHandler(this.m_lbRenderings_MouseDown);
			//
			// contextMenuStrip
			//
			this.contextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			mnuSetAsDefault,
			mnuAddRendering,
			this.mnuDeleteRendering});
			this.contextMenuStrip.Name = "contextMenuStrip1";
			this.contextMenuStrip.Size = new System.Drawing.Size(199, 70);
			this.contextMenuStrip.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenuStrip_Opening);
			//
			// mnuDeleteRendering
			//
			this.mnuDeleteRendering.Name = "mnuDeleteRendering";
			this.mnuDeleteRendering.Size = new System.Drawing.Size(198, 22);
			this.mnuDeleteRendering.Text = "&Delete this rendering";
			this.mnuDeleteRendering.Click += new System.EventHandler(this.mnuDeleteRendering_Click);
			//
			// TermRenderingCtrl
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.Controls.Add(this.m_lbRenderings);
			this.Controls.Add(this.m_lblKeyTermColHead);
			this.Margin = new System.Windows.Forms.Padding(0);
			this.MinimumSize = new System.Drawing.Size(100, 40);
			this.Name = "TermRenderingCtrl";
			this.Size = new System.Drawing.Size(148, 38);
			this.contextMenuStrip.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Label m_lblKeyTermColHead;
		private System.Windows.Forms.ListBox m_lbRenderings;
		private System.Windows.Forms.ContextMenuStrip contextMenuStrip;
		private System.Windows.Forms.ToolStripMenuItem mnuDeleteRendering;
	}
}

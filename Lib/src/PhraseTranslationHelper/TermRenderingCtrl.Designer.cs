// ---------------------------------------------------------------------------------------------
// Copyright (c) 2011-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: TermRenderingCtrl.cs
// Responsibility: bogle
// ---------------------------------------------------------------------------------------------
using System.Diagnostics.CodeAnalysis;

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
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + ". ****** ");
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
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="Controls get added to Controls collection and disposed there")]
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.Windows.Forms.ToolStripMenuItem mnuSetAsDefault;
			System.Windows.Forms.ToolStripMenuItem mnuAddRendering;
			System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
			this.m_lblKeyTermColHead = new System.Windows.Forms.Label();
			this.m_lbRenderings = new System.Windows.Forms.ListBox();
			this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.mnuDeleteRendering = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuLookUpTermC = new System.Windows.Forms.ToolStripMenuItem();
			this.headerContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.mnuLookUpTermH = new System.Windows.Forms.ToolStripMenuItem();
			mnuSetAsDefault = new System.Windows.Forms.ToolStripMenuItem();
			mnuAddRendering = new System.Windows.Forms.ToolStripMenuItem();
			toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.contextMenuStrip.SuspendLayout();
			this.headerContextMenuStrip.SuspendLayout();
			this.SuspendLayout();
			//
			// mnuSetAsDefault
			//
			mnuSetAsDefault.Image = global::SILUBS.PhraseTranslationHelper.Properties.Resources.check_circle;
			mnuSetAsDefault.Name = "mnuSetAsDefault";
			mnuSetAsDefault.Size = new System.Drawing.Size(198, 22);
			mnuSetAsDefault.Text = "&Set as default rendering";
			mnuSetAsDefault.Click += new System.EventHandler(this.mnuSetAsDefault_Click);
			//
			// mnuAddRendering
			//
			mnuAddRendering.Image = global::SILUBS.PhraseTranslationHelper.Properties.Resources._1321382935_plus;
			mnuAddRendering.Name = "mnuAddRendering";
			mnuAddRendering.Size = new System.Drawing.Size(198, 22);
			mnuAddRendering.Text = "&Add rendering...";
			mnuAddRendering.Click += new System.EventHandler(this.mnuAddRendering_Click);
			//
			// toolStripSeparator1
			//
			toolStripSeparator1.Name = "toolStripSeparator1";
			toolStripSeparator1.Size = new System.Drawing.Size(195, 6);
			//
			// m_lblKeyTermColHead
			//
			this.m_lblKeyTermColHead.ContextMenuStrip = this.headerContextMenuStrip;
			this.m_lblKeyTermColHead.Dock = System.Windows.Forms.DockStyle.Top;
			this.m_lblKeyTermColHead.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold);
			this.m_lblKeyTermColHead.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.m_lblKeyTermColHead.Location = new System.Drawing.Point(0, 0);
			this.m_lblKeyTermColHead.Name = "m_lblKeyTermColHead";
			this.m_lblKeyTermColHead.Size = new System.Drawing.Size(100, 20);
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
			this.m_lbRenderings.Size = new System.Drawing.Size(100, 20);
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
			this.mnuDeleteRendering,
			toolStripSeparator1,
			this.mnuLookUpTermC});
			this.contextMenuStrip.Name = "contextMenuStrip1";
			this.contextMenuStrip.Size = new System.Drawing.Size(199, 98);
			this.contextMenuStrip.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenuStrip_Opening);
			//
			// mnuDeleteRendering
			//
			this.mnuDeleteRendering.Image = global::SILUBS.PhraseTranslationHelper.Properties.Resources._1321385905_dialog_close;
			this.mnuDeleteRendering.Name = "mnuDeleteRendering";
			this.mnuDeleteRendering.Size = new System.Drawing.Size(198, 22);
			this.mnuDeleteRendering.Text = "&Delete this rendering";
			this.mnuDeleteRendering.Click += new System.EventHandler(this.mnuDeleteRendering_Click);
			//
			// mnuLookUpTermC
			//
			this.mnuLookUpTermC.Image = global::SILUBS.PhraseTranslationHelper.Properties.Resources._1330980033_search_button;
			this.mnuLookUpTermC.Name = "mnuLookUpTermC";
			this.mnuLookUpTermC.Size = new System.Drawing.Size(198, 22);
			this.mnuLookUpTermC.Text = "Find &Term in {0}";
			this.mnuLookUpTermC.Click += new System.EventHandler(this.LookUpTermInHostApplicaton);
			//
			// headerContextMenuStrip
			//
			this.headerContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.mnuLookUpTermH});
			this.headerContextMenuStrip.Name = "contextMenuStrip1";
			this.headerContextMenuStrip.Size = new System.Drawing.Size(159, 26);
			//
			// mnuLookUpTermH
			//
			this.mnuLookUpTermH.Image = global::SILUBS.PhraseTranslationHelper.Properties.Resources._1330980033_search_button;
			this.mnuLookUpTermH.Name = "mnuLookUpTermH";
			this.mnuLookUpTermH.Size = new System.Drawing.Size(158, 22);
			this.mnuLookUpTermH.Text = "Find &Term in {0}";
			this.mnuLookUpTermH.Click += new System.EventHandler(this.LookUpTermInHostApplicaton);
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
			this.Size = new System.Drawing.Size(100, 40);
			this.contextMenuStrip.ResumeLayout(false);
			this.headerContextMenuStrip.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Label m_lblKeyTermColHead;
		private System.Windows.Forms.ListBox m_lbRenderings;
		private System.Windows.Forms.ContextMenuStrip contextMenuStrip;
		private System.Windows.Forms.ToolStripMenuItem mnuDeleteRendering;
		private System.Windows.Forms.ToolStripMenuItem mnuLookUpTermC;
		private System.Windows.Forms.ContextMenuStrip headerContextMenuStrip;
		private System.Windows.Forms.ToolStripMenuItem mnuLookUpTermH;
	}
}

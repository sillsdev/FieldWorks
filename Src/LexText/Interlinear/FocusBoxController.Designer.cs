using System;
namespace SIL.FieldWorks.IText
{
	partial class FocusBoxController
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + ". ******************");
			if (disposing)
			{
				if (components != null)
					components.Dispose();
				if (m_sandbox != null)
					((IDisposable)m_sandbox).Dispose();
			}
			components = null;
			m_sandbox = null;
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FocusBoxController));
			this.panelSidebar = new System.Windows.Forms.Panel();
			this.btnLinkNextWord = new System.Windows.Forms.Button();
			this.panelControlBar = new System.Windows.Forms.Panel();
			this.btnConfirmChangesForWholeText = new System.Windows.Forms.Button();
			this.btnUndoChanges = new System.Windows.Forms.Button();
			this.btnBreakPhrase = new System.Windows.Forms.Button();
			this.btnMenu = new System.Windows.Forms.Button();
			this.btnConfirmChanges = new System.Windows.Forms.Button();
			this.panelSandbox = new System.Windows.Forms.Panel();
			this.toolTip = new System.Windows.Forms.ToolTip(this.components);
			this.panelSidebar.SuspendLayout();
			this.panelControlBar.SuspendLayout();
			this.SuspendLayout();
			//
			// panelSidebar
			//
			this.panelSidebar.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.panelSidebar.BackColor = System.Drawing.Color.Transparent;
			this.panelSidebar.Controls.Add(this.btnLinkNextWord);
			this.panelSidebar.Location = new System.Drawing.Point(104, 0);
			this.panelSidebar.Name = "panelSidebar";
			this.panelSidebar.Size = new System.Drawing.Size(23, 32);
			this.panelSidebar.TabIndex = 1;
			//
			// btnLinkNextWord
			//
			this.btnLinkNextWord.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.btnLinkNextWord.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btnLinkNextWord.BackgroundImage")));
			this.btnLinkNextWord.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
			this.btnLinkNextWord.Enabled = false;
			this.btnLinkNextWord.FlatAppearance.BorderSize = 0;
			this.btnLinkNextWord.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.btnLinkNextWord.Location = new System.Drawing.Point(0, 1);
			this.btnLinkNextWord.Name = "btnLinkNextWord";
			this.btnLinkNextWord.Size = new System.Drawing.Size(20, 16);
			this.btnLinkNextWord.TabIndex = 0;
			this.btnLinkNextWord.TabStop = false;
			this.btnLinkNextWord.UseVisualStyleBackColor = true;
			this.btnLinkNextWord.Visible = false;
			this.btnLinkNextWord.Click += new System.EventHandler(this.btnLinkNextWord_Click);
			//
			// panelControlBar
			//
			this.panelControlBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.panelControlBar.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.panelControlBar.BackColor = System.Drawing.SystemColors.ControlLight;
			this.panelControlBar.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panelControlBar.Controls.Add(this.btnConfirmChangesForWholeText);
			this.panelControlBar.Controls.Add(this.btnUndoChanges);
			this.panelControlBar.Controls.Add(this.btnBreakPhrase);
			this.panelControlBar.Controls.Add(this.btnMenu);
			this.panelControlBar.Controls.Add(this.btnConfirmChanges);
			this.panelControlBar.Location = new System.Drawing.Point(0, 14);
			this.panelControlBar.MinimumSize = new System.Drawing.Size(104, 18);
			this.panelControlBar.Name = "panelControlBar";
			this.panelControlBar.Size = new System.Drawing.Size(104, 18);
			this.panelControlBar.TabIndex = 2;
			//
			// btnConfirmChangesForWholeText
			//
			this.btnConfirmChangesForWholeText.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnConfirmChangesForWholeText.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btnConfirmChangesForWholeText.BackgroundImage")));
			this.btnConfirmChangesForWholeText.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
			this.btnConfirmChangesForWholeText.FlatAppearance.BorderSize = 0;
			this.btnConfirmChangesForWholeText.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.btnConfirmChangesForWholeText.Location = new System.Drawing.Point(66, 0);
			this.btnConfirmChangesForWholeText.MaximumSize = new System.Drawing.Size(16, 16);
			this.btnConfirmChangesForWholeText.MinimumSize = new System.Drawing.Size(16, 16);
			this.btnConfirmChangesForWholeText.Name = "btnConfirmChangesForWholeText";
			this.btnConfirmChangesForWholeText.Size = new System.Drawing.Size(16, 16);
			this.btnConfirmChangesForWholeText.TabIndex = 4;
			this.btnConfirmChangesForWholeText.TabStop = false;
			this.btnConfirmChangesForWholeText.UseVisualStyleBackColor = true;
			this.btnConfirmChangesForWholeText.Click += new System.EventHandler(this.btnConfirmChangesForWholeText_Click);
			//
			// btnUndoChanges
			//
			this.btnUndoChanges.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnUndoChanges.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btnUndoChanges.BackgroundImage")));
			this.btnUndoChanges.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
			this.btnUndoChanges.Enabled = false;
			this.btnUndoChanges.FlatAppearance.BorderSize = 0;
			this.btnUndoChanges.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.btnUndoChanges.Location = new System.Drawing.Point(44, 0);
			this.btnUndoChanges.MaximumSize = new System.Drawing.Size(16, 16);
			this.btnUndoChanges.MinimumSize = new System.Drawing.Size(16, 16);
			this.btnUndoChanges.Name = "btnUndoChanges";
			this.btnUndoChanges.Size = new System.Drawing.Size(16, 16);
			this.btnUndoChanges.TabIndex = 1;
			this.btnUndoChanges.TabStop = false;
			this.btnUndoChanges.UseVisualStyleBackColor = true;
			this.btnUndoChanges.Visible = false;
			this.btnUndoChanges.Click += new System.EventHandler(this.btnUndoChanges_Click);
			//
			// btnBreakPhrase
			//
			this.btnBreakPhrase.Anchor = System.Windows.Forms.AnchorStyles.Top;
			this.btnBreakPhrase.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btnBreakPhrase.BackgroundImage")));
			this.btnBreakPhrase.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
			this.btnBreakPhrase.Enabled = false;
			this.btnBreakPhrase.FlatAppearance.BorderSize = 0;
			this.btnBreakPhrase.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.btnBreakPhrase.Location = new System.Drawing.Point(22, 0);
			this.btnBreakPhrase.Name = "btnBreakPhrase";
			this.btnBreakPhrase.Size = new System.Drawing.Size(16, 16);
			this.btnBreakPhrase.TabIndex = 3;
			this.btnBreakPhrase.TabStop = false;
			this.btnBreakPhrase.UseVisualStyleBackColor = true;
			this.btnBreakPhrase.Visible = false;
			this.btnBreakPhrase.Click += new System.EventHandler(this.btnBreakPhrase_Click);
			//
			// btnMenu
			//
			this.btnMenu.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btnMenu.BackgroundImage")));
			this.btnMenu.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
			this.btnMenu.FlatAppearance.BorderSize = 0;
			this.btnMenu.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.btnMenu.Location = new System.Drawing.Point(0, 0);
			this.btnMenu.MinimumSize = new System.Drawing.Size(16, 16);
			this.btnMenu.Name = "btnMenu";
			this.btnMenu.Size = new System.Drawing.Size(16, 16);
			this.btnMenu.TabIndex = 0;
			this.btnMenu.TabStop = false;
			this.btnMenu.UseVisualStyleBackColor = true;
			this.btnMenu.Click += new System.EventHandler(this.btnMenu_Click);
			//
			// btnConfirmChanges
			//
			this.btnConfirmChanges.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnConfirmChanges.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btnConfirmChanges.BackgroundImage")));
			this.btnConfirmChanges.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
			this.btnConfirmChanges.FlatAppearance.BorderSize = 0;
			this.btnConfirmChanges.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.btnConfirmChanges.Location = new System.Drawing.Point(87, 0);
			this.btnConfirmChanges.MinimumSize = new System.Drawing.Size(16, 16);
			this.btnConfirmChanges.Name = "btnConfirmChanges";
			this.btnConfirmChanges.Size = new System.Drawing.Size(16, 16);
			this.btnConfirmChanges.TabIndex = 2;
			this.btnConfirmChanges.TabStop = false;
			this.btnConfirmChanges.UseVisualStyleBackColor = true;
			this.btnConfirmChanges.Click += new System.EventHandler(this.btnConfirmChanges_Click);
			//
			// panelSandbox
			//
			this.panelSandbox.AutoSize = true;
			this.panelSandbox.BackColor = System.Drawing.SystemColors.Control;
			this.panelSandbox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panelSandbox.Location = new System.Drawing.Point(0, 0);
			this.panelSandbox.Margin = new System.Windows.Forms.Padding(0, 0, 0, 3);
			this.panelSandbox.MinimumSize = new System.Drawing.Size(104, 16);
			this.panelSandbox.Name = "panelSandbox";
			this.panelSandbox.Size = new System.Drawing.Size(104, 17);
			this.panelSandbox.TabIndex = 3;
			//
			// toolTip
			//
			this.toolTip.Popup += new System.Windows.Forms.PopupEventHandler(this.toolTip_Popup);
			//
			// FocusBoxController
			//
			this.AccessibleName = "FocusBoxController";
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.BackColor = System.Drawing.SystemColors.Window;
			this.Controls.Add(this.panelSidebar);
			this.Controls.Add(this.panelControlBar);
			this.Controls.Add(this.panelSandbox);
			this.Cursor = System.Windows.Forms.Cursors.Arrow;
			this.Name = "FocusBoxController";
			this.Size = new System.Drawing.Size(224, 57);
			this.toolTip.SetToolTip(this, " Focus Box");
			this.panelSidebar.ResumeLayout(false);
			this.panelControlBar.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Panel panelSidebar;
		private System.Windows.Forms.Panel panelControlBar;
		private System.Windows.Forms.Panel panelSandbox;
		private System.Windows.Forms.Button btnLinkNextWord;
		private System.Windows.Forms.Button btnConfirmChanges;
		private System.Windows.Forms.Button btnUndoChanges;
		private System.Windows.Forms.Button btnMenu;
		private System.Windows.Forms.Button btnBreakPhrase;
		private System.Windows.Forms.ToolTip toolTip;
		private System.Windows.Forms.Button btnConfirmChangesForWholeText;
	}
}

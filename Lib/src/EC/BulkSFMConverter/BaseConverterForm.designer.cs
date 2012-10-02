namespace SFMConv
{
	partial class BaseConverterForm
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

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BaseConverterForm));
			this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.labelInputString = new System.Windows.Forms.Label();
			this.textBoxInput = new System.Windows.Forms.TextBox();
			this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.changeFontToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
			this.undoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.cutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.copyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.pasteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.deleteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.selectAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.right2LeftToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.labelForwardConversion = new System.Windows.Forms.Label();
			this.textBoxConverted = new System.Windows.Forms.TextBox();
			this.labelForwardCodePoints = new System.Windows.Forms.Label();
			this.labelInputCodePoints = new System.Windows.Forms.Label();
			this.buttonNextWord = new System.Windows.Forms.Button();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.buttonReplaceAll = new System.Windows.Forms.Button();
			this.buttonReplace = new System.Windows.Forms.Button();
			this.toolTip = new System.Windows.Forms.ToolTip(this.components);
			this.checkBoxSkipIdenticalForms = new System.Windows.Forms.CheckBox();
			this.buttonRefresh = new System.Windows.Forms.Button();
			this.buttonDebug = new System.Windows.Forms.Button();
			this.tableLayoutPanelButtons = new System.Windows.Forms.TableLayoutPanel();
			this.fontDialog = new System.Windows.Forms.FontDialog();
			this.tableLayoutPanelDebugRefresh = new System.Windows.Forms.TableLayoutPanel();
			this.tableLayoutPanel.SuspendLayout();
			this.contextMenuStrip.SuspendLayout();
			this.tableLayoutPanelButtons.SuspendLayout();
			this.tableLayoutPanelDebugRefresh.SuspendLayout();
			this.SuspendLayout();
			//
			// tableLayoutPanel
			//
			this.tableLayoutPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.tableLayoutPanel.AutoSize = true;
			this.tableLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.tableLayoutPanel.ColumnCount = 3;
			this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
			this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 60F));
			this.tableLayoutPanel.Controls.Add(this.labelInputString, 0, 0);
			this.tableLayoutPanel.Controls.Add(this.textBoxInput, 1, 0);
			this.tableLayoutPanel.Controls.Add(this.labelForwardConversion, 0, 1);
			this.tableLayoutPanel.Controls.Add(this.textBoxConverted, 1, 1);
			this.tableLayoutPanel.Controls.Add(this.labelForwardCodePoints, 2, 1);
			this.tableLayoutPanel.Controls.Add(this.labelInputCodePoints, 2, 0);
			this.tableLayoutPanel.Location = new System.Drawing.Point(12, 12);
			this.tableLayoutPanel.Name = "tableLayoutPanel";
			this.tableLayoutPanel.Padding = new System.Windows.Forms.Padding(3);
			this.tableLayoutPanel.RowCount = 2;
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.tableLayoutPanel.Size = new System.Drawing.Size(619, 74);
			this.tableLayoutPanel.TabIndex = 0;
			//
			// labelInputString
			//
			this.labelInputString.Anchor = System.Windows.Forms.AnchorStyles.Right;
			this.labelInputString.AutoSize = true;
			this.labelInputString.Location = new System.Drawing.Point(6, 13);
			this.labelInputString.Name = "labelInputString";
			this.labelInputString.Size = new System.Drawing.Size(72, 13);
			this.labelInputString.TabIndex = 0;
			this.labelInputString.Text = "&Found match:";
			//
			// textBoxInput
			//
			this.textBoxInput.ContextMenuStrip = this.contextMenuStrip;
			this.textBoxInput.Dock = System.Windows.Forms.DockStyle.Fill;
			this.textBoxInput.Font = new System.Drawing.Font("Arial Unicode MS", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.textBoxInput.Location = new System.Drawing.Point(84, 6);
			this.textBoxInput.Multiline = true;
			this.textBoxInput.Name = "textBoxInput";
			this.textBoxInput.Size = new System.Drawing.Size(208, 28);
			this.textBoxInput.TabIndex = 1;
			this.toolTip.SetToolTip(this.textBoxInput, "This is the word from the document being checked");
			this.textBoxInput.TextChanged += new System.EventHandler(this.textBoxInput_TextChanged);
			//
			// contextMenuStrip
			//
			this.contextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.changeFontToolStripMenuItem,
			this.toolStripSeparator4,
			this.undoToolStripMenuItem,
			this.toolStripSeparator1,
			this.cutToolStripMenuItem,
			this.copyToolStripMenuItem,
			this.pasteToolStripMenuItem,
			this.deleteToolStripMenuItem,
			this.toolStripSeparator2,
			this.selectAllToolStripMenuItem,
			this.toolStripSeparator3,
			this.right2LeftToolStripMenuItem});
			this.contextMenuStrip.Name = "contextMenuStrip";
			this.contextMenuStrip.Size = new System.Drawing.Size(200, 204);
			this.contextMenuStrip.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenuStrip_Opening);
			//
			// changeFontToolStripMenuItem
			//
			this.changeFontToolStripMenuItem.Name = "changeFontToolStripMenuItem";
			this.changeFontToolStripMenuItem.Size = new System.Drawing.Size(199, 22);
			this.changeFontToolStripMenuItem.Text = "Change &Font";
			this.changeFontToolStripMenuItem.ToolTipText = "Click here to change the display font for this text box";
			this.changeFontToolStripMenuItem.Click += new System.EventHandler(this.changeFontToolStripMenuItem_Click);
			//
			// toolStripSeparator4
			//
			this.toolStripSeparator4.Name = "toolStripSeparator4";
			this.toolStripSeparator4.Size = new System.Drawing.Size(196, 6);
			//
			// undoToolStripMenuItem
			//
			this.undoToolStripMenuItem.Name = "undoToolStripMenuItem";
			this.undoToolStripMenuItem.Size = new System.Drawing.Size(199, 22);
			this.undoToolStripMenuItem.Text = "&Undo";
			this.undoToolStripMenuItem.Click += new System.EventHandler(this.undoToolStripMenuItem_Click);
			//
			// toolStripSeparator1
			//
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(196, 6);
			//
			// cutToolStripMenuItem
			//
			this.cutToolStripMenuItem.Name = "cutToolStripMenuItem";
			this.cutToolStripMenuItem.Size = new System.Drawing.Size(199, 22);
			this.cutToolStripMenuItem.Text = "Cut";
			this.cutToolStripMenuItem.Click += new System.EventHandler(this.cutToolStripMenuItem_Click);
			//
			// copyToolStripMenuItem
			//
			this.copyToolStripMenuItem.Name = "copyToolStripMenuItem";
			this.copyToolStripMenuItem.Size = new System.Drawing.Size(199, 22);
			this.copyToolStripMenuItem.Text = "Copy";
			this.copyToolStripMenuItem.Click += new System.EventHandler(this.copyToolStripMenuItem_Click);
			//
			// pasteToolStripMenuItem
			//
			this.pasteToolStripMenuItem.Name = "pasteToolStripMenuItem";
			this.pasteToolStripMenuItem.Size = new System.Drawing.Size(199, 22);
			this.pasteToolStripMenuItem.Text = "Paste";
			this.pasteToolStripMenuItem.Click += new System.EventHandler(this.pasteToolStripMenuItem_Click);
			//
			// deleteToolStripMenuItem
			//
			this.deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
			this.deleteToolStripMenuItem.Size = new System.Drawing.Size(199, 22);
			this.deleteToolStripMenuItem.Text = "Delete";
			this.deleteToolStripMenuItem.Click += new System.EventHandler(this.deleteToolStripMenuItem_Click);
			//
			// toolStripSeparator2
			//
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(196, 6);
			//
			// selectAllToolStripMenuItem
			//
			this.selectAllToolStripMenuItem.Name = "selectAllToolStripMenuItem";
			this.selectAllToolStripMenuItem.Size = new System.Drawing.Size(199, 22);
			this.selectAllToolStripMenuItem.Text = "Select All";
			this.selectAllToolStripMenuItem.Click += new System.EventHandler(this.selectAllToolStripMenuItem_Click);
			//
			// toolStripSeparator3
			//
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			this.toolStripSeparator3.Size = new System.Drawing.Size(196, 6);
			//
			// right2LeftToolStripMenuItem
			//
			this.right2LeftToolStripMenuItem.CheckOnClick = true;
			this.right2LeftToolStripMenuItem.Name = "right2LeftToolStripMenuItem";
			this.right2LeftToolStripMenuItem.Size = new System.Drawing.Size(199, 22);
			this.right2LeftToolStripMenuItem.Text = "&Right to left reading order";
			this.right2LeftToolStripMenuItem.Click += new System.EventHandler(this.right2LeftToolStripMenuItem_Click);
			//
			// labelForwardConversion
			//
			this.labelForwardConversion.Anchor = System.Windows.Forms.AnchorStyles.Right;
			this.labelForwardConversion.AutoSize = true;
			this.labelForwardConversion.Location = new System.Drawing.Point(6, 47);
			this.labelForwardConversion.Name = "labelForwardConversion";
			this.labelForwardConversion.Size = new System.Drawing.Size(72, 13);
			this.labelForwardConversion.TabIndex = 4;
			this.labelForwardConversion.Text = "&Replace with:";
			//
			// textBoxConverted
			//
			this.textBoxConverted.ContextMenuStrip = this.contextMenuStrip;
			this.textBoxConverted.Dock = System.Windows.Forms.DockStyle.Fill;
			this.textBoxConverted.Font = new System.Drawing.Font("Arial Unicode MS", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.textBoxConverted.Location = new System.Drawing.Point(84, 40);
			this.textBoxConverted.Multiline = true;
			this.textBoxConverted.Name = "textBoxConverted";
			this.textBoxConverted.Size = new System.Drawing.Size(208, 28);
			this.textBoxConverted.TabIndex = 5;
			this.toolTip.SetToolTip(this.textBoxConverted, "This box shows the result of the conversion");
			this.textBoxConverted.TextChanged += new System.EventHandler(this.textBoxConverted_TextChanged);
			//
			// labelForwardCodePoints
			//
			this.labelForwardCodePoints.AutoSize = true;
			this.labelForwardCodePoints.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.labelForwardCodePoints.Dock = System.Windows.Forms.DockStyle.Fill;
			this.labelForwardCodePoints.Location = new System.Drawing.Point(298, 40);
			this.labelForwardCodePoints.Margin = new System.Windows.Forms.Padding(3);
			this.labelForwardCodePoints.Name = "labelForwardCodePoints";
			this.labelForwardCodePoints.Padding = new System.Windows.Forms.Padding(3);
			this.labelForwardCodePoints.Size = new System.Drawing.Size(315, 28);
			this.labelForwardCodePoints.TabIndex = 8;
			//
			// labelInputCodePoints
			//
			this.labelInputCodePoints.AutoSize = true;
			this.labelInputCodePoints.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.labelInputCodePoints.Dock = System.Windows.Forms.DockStyle.Fill;
			this.labelInputCodePoints.Location = new System.Drawing.Point(298, 6);
			this.labelInputCodePoints.Margin = new System.Windows.Forms.Padding(3);
			this.labelInputCodePoints.Name = "labelInputCodePoints";
			this.labelInputCodePoints.Padding = new System.Windows.Forms.Padding(3);
			this.labelInputCodePoints.Size = new System.Drawing.Size(315, 28);
			this.labelInputCodePoints.TabIndex = 6;
			//
			// buttonNextWord
			//
			this.buttonNextWord.Anchor = System.Windows.Forms.AnchorStyles.Top;
			this.buttonNextWord.AutoSize = true;
			this.buttonNextWord.Location = new System.Drawing.Point(5, 5);
			this.buttonNextWord.Margin = new System.Windows.Forms.Padding(5);
			this.buttonNextWord.Name = "buttonNextWord";
			this.buttonNextWord.Size = new System.Drawing.Size(75, 23);
			this.buttonNextWord.TabIndex = 3;
			this.buttonNextWord.Text = "&Skip";
			this.toolTip.SetToolTip(this.buttonNextWord, "Click here to skip converting this word and go to the next word in the document");
			this.buttonNextWord.UseVisualStyleBackColor = true;
			this.buttonNextWord.Click += new System.EventHandler(this.buttonNextWord_Click);
			//
			// buttonCancel
			//
			this.buttonCancel.Anchor = System.Windows.Forms.AnchorStyles.Top;
			this.buttonCancel.AutoSize = true;
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.Location = new System.Drawing.Point(260, 5);
			this.buttonCancel.Margin = new System.Windows.Forms.Padding(5);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(75, 23);
			this.buttonCancel.TabIndex = 0;
			this.buttonCancel.Text = "&Cancel";
			this.toolTip.SetToolTip(this.buttonCancel, "Click here to turn off single step mode and dismiss the dialog box (the conversio" +
					"n will continue)");
			this.buttonCancel.UseVisualStyleBackColor = true;
			this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
			//
			// buttonReplaceAll
			//
			this.buttonReplaceAll.Anchor = System.Windows.Forms.AnchorStyles.Top;
			this.buttonReplaceAll.AutoSize = true;
			this.buttonReplaceAll.Location = new System.Drawing.Point(175, 5);
			this.buttonReplaceAll.Margin = new System.Windows.Forms.Padding(5);
			this.buttonReplaceAll.Name = "buttonReplaceAll";
			this.buttonReplaceAll.Size = new System.Drawing.Size(75, 23);
			this.buttonReplaceAll.TabIndex = 1;
			this.buttonReplaceAll.Text = "Replace &All";
			this.toolTip.SetToolTip(this.buttonReplaceAll, "Click here to replace all the words in the document with the results of the conve" +
					"rsion");
			this.buttonReplaceAll.UseVisualStyleBackColor = true;
			this.buttonReplaceAll.Click += new System.EventHandler(this.buttonReplaceAll_Click);
			//
			// buttonReplace
			//
			this.buttonReplace.Anchor = System.Windows.Forms.AnchorStyles.Top;
			this.buttonReplace.AutoSize = true;
			this.buttonReplace.Location = new System.Drawing.Point(90, 5);
			this.buttonReplace.Margin = new System.Windows.Forms.Padding(5);
			this.buttonReplace.Name = "buttonReplace";
			this.buttonReplace.Size = new System.Drawing.Size(75, 23);
			this.buttonReplace.TabIndex = 9;
			this.buttonReplace.Text = "Re&place";
			this.toolTip.SetToolTip(this.buttonReplace, "Click here to replace the text in the document with the text in the \'Replace with" +
					"\' box");
			this.buttonReplace.UseVisualStyleBackColor = true;
			this.buttonReplace.Click += new System.EventHandler(this.buttonReplace_Click);
			//
			// checkBoxSkipIdenticalForms
			//
			this.checkBoxSkipIdenticalForms.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.checkBoxSkipIdenticalForms.AutoSize = true;
			this.checkBoxSkipIdenticalForms.Location = new System.Drawing.Point(18, 103);
			this.checkBoxSkipIdenticalForms.Name = "checkBoxSkipIdenticalForms";
			this.checkBoxSkipIdenticalForms.Size = new System.Drawing.Size(121, 17);
			this.checkBoxSkipIdenticalForms.TabIndex = 10;
			this.checkBoxSkipIdenticalForms.Text = "&Skip Identical Forms";
			this.toolTip.SetToolTip(this.checkBoxSkipIdenticalForms, "Check this box to avoid showing the dialog box when the result of the conversion " +
					"is the same as the input string (i.e. no change)");
			this.checkBoxSkipIdenticalForms.UseVisualStyleBackColor = true;
			//
			// buttonRefresh
			//
			this.buttonRefresh.Dock = System.Windows.Forms.DockStyle.Fill;
			this.buttonRefresh.Location = new System.Drawing.Point(3, 32);
			this.buttonRefresh.Name = "buttonRefresh";
			this.buttonRefresh.Size = new System.Drawing.Size(87, 25);
			this.buttonRefresh.TabIndex = 0;
			this.buttonRefresh.Text = "Refre&sh";
			this.toolTip.SetToolTip(this.buttonRefresh, "Click here to re-run the conversion processes (e.g. if you changed the underlying" +
					" conversion table)");
			this.buttonRefresh.UseVisualStyleBackColor = true;
			this.buttonRefresh.Click += new System.EventHandler(this.buttonRefresh_Click);
			//
			// buttonDebug
			//
			this.buttonDebug.Dock = System.Windows.Forms.DockStyle.Fill;
			this.buttonDebug.Location = new System.Drawing.Point(3, 3);
			this.buttonDebug.Name = "buttonDebug";
			this.buttonDebug.Size = new System.Drawing.Size(87, 23);
			this.buttonDebug.TabIndex = 1;
			this.buttonDebug.Text = "&Debug";
			this.toolTip.SetToolTip(this.buttonDebug, "Click here to re-run the conversions and show feedback at each step of the conver" +
					"sion process");
			this.buttonDebug.UseVisualStyleBackColor = true;
			this.buttonDebug.Click += new System.EventHandler(this.buttonDebug_Click);
			//
			// tableLayoutPanelButtons
			//
			this.tableLayoutPanelButtons.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.tableLayoutPanelButtons.ColumnCount = 4;
			this.tableLayoutPanelButtons.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanelButtons.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanelButtons.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanelButtons.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanelButtons.Controls.Add(this.buttonNextWord, 0, 0);
			this.tableLayoutPanelButtons.Controls.Add(this.buttonReplace, 1, 0);
			this.tableLayoutPanelButtons.Controls.Add(this.buttonReplaceAll, 2, 0);
			this.tableLayoutPanelButtons.Controls.Add(this.buttonCancel, 3, 0);
			this.tableLayoutPanelButtons.Location = new System.Drawing.Point(13, 126);
			this.tableLayoutPanelButtons.Name = "tableLayoutPanelButtons";
			this.tableLayoutPanelButtons.RowCount = 1;
			this.tableLayoutPanelButtons.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanelButtons.Size = new System.Drawing.Size(341, 34);
			this.tableLayoutPanelButtons.TabIndex = 1;
			//
			// fontDialog
			//
			this.fontDialog.AllowScriptChange = false;
			this.fontDialog.ShowColor = true;
			//
			// tableLayoutPanelDebugRefresh
			//
			this.tableLayoutPanelDebugRefresh.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.tableLayoutPanelDebugRefresh.ColumnCount = 1;
			this.tableLayoutPanelDebugRefresh.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanelDebugRefresh.Controls.Add(this.buttonRefresh, 0, 1);
			this.tableLayoutPanelDebugRefresh.Controls.Add(this.buttonDebug, 0, 0);
			this.tableLayoutPanelDebugRefresh.Location = new System.Drawing.Point(538, 100);
			this.tableLayoutPanelDebugRefresh.Name = "tableLayoutPanelDebugRefresh";
			this.tableLayoutPanelDebugRefresh.RowCount = 2;
			this.tableLayoutPanelDebugRefresh.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanelDebugRefresh.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanelDebugRefresh.Size = new System.Drawing.Size(93, 60);
			this.tableLayoutPanelDebugRefresh.TabIndex = 13;
			//
			// BaseConverterForm
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(648, 172);
			this.Controls.Add(this.tableLayoutPanelDebugRefresh);
			this.Controls.Add(this.checkBoxSkipIdenticalForms);
			this.Controls.Add(this.tableLayoutPanelButtons);
			this.Controls.Add(this.tableLayoutPanel);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "BaseConverterForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
			this.Text = "SIL Converters";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.BaseConverterForm_FormClosing);
			this.tableLayoutPanel.ResumeLayout(false);
			this.tableLayoutPanel.PerformLayout();
			this.contextMenuStrip.ResumeLayout(false);
			this.tableLayoutPanelButtons.ResumeLayout(false);
			this.tableLayoutPanelButtons.PerformLayout();
			this.tableLayoutPanelDebugRefresh.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		protected internal System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
		protected internal System.Windows.Forms.Label labelInputString;
		protected internal System.Windows.Forms.TextBox textBoxInput;
		protected internal System.Windows.Forms.Label labelForwardConversion;
		protected internal System.Windows.Forms.TextBox textBoxConverted;
		protected internal System.Windows.Forms.Button buttonNextWord;
		protected internal System.Windows.Forms.Label labelInputCodePoints;
		protected internal System.Windows.Forms.ToolTip toolTip;
		protected internal System.Windows.Forms.Button buttonCancel;
		protected internal System.Windows.Forms.Button buttonReplaceAll;
		protected internal System.Windows.Forms.Label labelForwardCodePoints;
		protected internal System.Windows.Forms.Button buttonReplace;
		protected internal System.Windows.Forms.TableLayoutPanel tableLayoutPanelButtons;
		protected internal System.Windows.Forms.FontDialog fontDialog;
		private System.Windows.Forms.ToolStripMenuItem changeFontToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem right2LeftToolStripMenuItem;
		protected internal System.Windows.Forms.ContextMenuStrip contextMenuStrip;
		private System.Windows.Forms.ToolStripMenuItem undoToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripMenuItem cutToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem copyToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem pasteToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem deleteToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripMenuItem selectAllToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
		private System.Windows.Forms.CheckBox checkBoxSkipIdenticalForms;
		protected internal System.Windows.Forms.TableLayoutPanel tableLayoutPanelDebugRefresh;
		private System.Windows.Forms.Button buttonRefresh;
		private System.Windows.Forms.Button buttonDebug;


	}
}
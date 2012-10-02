namespace SilEncConverters40
{
	partial class AutoConfigDialog
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AutoConfigDialog));
			this.tabControl = new System.Windows.Forms.TabControl();
			this.tabPageAbout = new System.Windows.Forms.TabPage();
			this.webBrowserHelp = new System.Windows.Forms.WebBrowser();
			this.tabPageSetup = new System.Windows.Forms.TabPage();
			this.tabPageTestArea = new System.Windows.Forms.TabPage();
			this.tableLayoutPanelTestPage = new System.Windows.Forms.TableLayoutPanel();
			this.labelInstructions = new System.Windows.Forms.Label();
			this.labelInput = new System.Windows.Forms.Label();
			this.labelOutput = new System.Windows.Forms.Label();
			this.buttonTest = new System.Windows.Forms.Button();
			this.checkBoxTestReverse = new System.Windows.Forms.CheckBox();
			this.richTextBoxHexOutput = new System.Windows.Forms.RichTextBox();
			this.richTextBoxHexInput = new System.Windows.Forms.RichTextBox();
			this.contextMenuStripTestBoxes = new System.Windows.Forms.ContextMenuStrip(this.components);
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
			this.tabPageAdvanced = new System.Windows.Forms.TabPage();
			this.tableLayoutPanelTransductionTypes = new System.Windows.Forms.TableLayoutPanel();
			this.labelRightEncodingName = new System.Windows.Forms.Label();
			this.comboBoxEncodingNamesRhs = new System.Windows.Forms.ComboBox();
			this.labelFriendlyName = new System.Windows.Forms.Label();
			this.textBoxFriendlyName = new System.Windows.Forms.TextBox();
			this.labelLeftEncodingName = new System.Windows.Forms.Label();
			this.comboBoxEncodingNamesLhs = new System.Windows.Forms.ComboBox();
			this.labelTransductionType = new System.Windows.Forms.Label();
			this.flowLayoutPanelTransductionTypes = new System.Windows.Forms.FlowLayoutPanel();
			this.checkBoxUnicodeEncodingConversion = new System.Windows.Forms.CheckBox();
			this.checkBoxTransliteration = new System.Windows.Forms.CheckBox();
			this.checkBoxICUTransliteration = new System.Windows.Forms.CheckBox();
			this.checkBoxICURegularExpression = new System.Windows.Forms.CheckBox();
			this.checkBoxICUConverter = new System.Windows.Forms.CheckBox();
			this.checkBoxCodePage = new System.Windows.Forms.CheckBox();
			this.checkBoxNonUnicodeEncodingConversion = new System.Windows.Forms.CheckBox();
			this.checkBoxSpellingFixerProject = new System.Windows.Forms.CheckBox();
			this.checkBoxPythonScript = new System.Windows.Forms.CheckBox();
			this.checkBoxPerlExpression = new System.Windows.Forms.CheckBox();
			this.checkBoxSpare1 = new System.Windows.Forms.CheckBox();
			this.checkBoxSpare2 = new System.Windows.Forms.CheckBox();
			this.labelProperties = new System.Windows.Forms.Label();
			this.dataGridViewProperties = new System.Windows.Forms.DataGridView();
			this.ColumnKey = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.ColumnValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.buttonApply = new System.Windows.Forms.Button();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.buttonOK = new System.Windows.Forms.Button();
			this.toolTip = new System.Windows.Forms.ToolTip(this.components);
			this.buttonSaveInRepository = new System.Windows.Forms.Button();
			this.helpProvider = new System.Windows.Forms.HelpProvider();
			this.fontDialog = new System.Windows.Forms.FontDialog();
			this.ecTextBoxInput = new SilEncConverters40.EcTextBox();
			this.ecTextBoxOutput = new SilEncConverters40.EcTextBox();
			this.tabControl.SuspendLayout();
			this.tabPageAbout.SuspendLayout();
			this.tabPageTestArea.SuspendLayout();
			this.tableLayoutPanelTestPage.SuspendLayout();
			this.contextMenuStripTestBoxes.SuspendLayout();
			this.tabPageAdvanced.SuspendLayout();
			this.tableLayoutPanelTransductionTypes.SuspendLayout();
			this.flowLayoutPanelTransductionTypes.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.dataGridViewProperties)).BeginInit();
			this.SuspendLayout();
			//
			// tabControl
			//
			this.tabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.tabControl.Controls.Add(this.tabPageAbout);
			this.tabControl.Controls.Add(this.tabPageSetup);
			this.tabControl.Controls.Add(this.tabPageTestArea);
			this.tabControl.Controls.Add(this.tabPageAdvanced);
			this.tabControl.Location = new System.Drawing.Point(12, 12);
			this.tabControl.Name = "tabControl";
			this.tabControl.SelectedIndex = 0;
			this.tabControl.Size = new System.Drawing.Size(610, 426);
			this.tabControl.TabIndex = 1;
			this.tabControl.Selected += new System.Windows.Forms.TabControlEventHandler(this.tabControl_Selected);
			//
			// tabPageAbout
			//
			this.tabPageAbout.Controls.Add(this.webBrowserHelp);
			this.tabPageAbout.Location = new System.Drawing.Point(4, 22);
			this.tabPageAbout.Name = "tabPageAbout";
			this.tabPageAbout.Padding = new System.Windows.Forms.Padding(3);
			this.tabPageAbout.Size = new System.Drawing.Size(602, 400);
			this.tabPageAbout.TabIndex = 0;
			this.tabPageAbout.Text = "About";
			this.tabPageAbout.UseVisualStyleBackColor = true;
			//
			// webBrowserHelp
			//
			this.webBrowserHelp.Dock = System.Windows.Forms.DockStyle.Fill;
			this.webBrowserHelp.Location = new System.Drawing.Point(3, 3);
			this.webBrowserHelp.MinimumSize = new System.Drawing.Size(20, 20);
			this.webBrowserHelp.Name = "webBrowserHelp";
			this.webBrowserHelp.Size = new System.Drawing.Size(596, 394);
			this.webBrowserHelp.TabIndex = 0;
			this.webBrowserHelp.Url = new System.Uri("", System.UriKind.Relative);
			//
			// tabPageSetup
			//
			this.tabPageSetup.Location = new System.Drawing.Point(4, 22);
			this.tabPageSetup.Name = "tabPageSetup";
			this.tabPageSetup.Padding = new System.Windows.Forms.Padding(3);
			this.tabPageSetup.Size = new System.Drawing.Size(602, 400);
			this.tabPageSetup.TabIndex = 1;
			this.tabPageSetup.Text = "Setup";
			this.tabPageSetup.UseVisualStyleBackColor = true;
			//
			// tabPageTestArea
			//
			this.tabPageTestArea.Controls.Add(this.tableLayoutPanelTestPage);
			this.tabPageTestArea.Location = new System.Drawing.Point(4, 22);
			this.tabPageTestArea.Name = "tabPageTestArea";
			this.tabPageTestArea.Padding = new System.Windows.Forms.Padding(3);
			this.tabPageTestArea.Size = new System.Drawing.Size(602, 400);
			this.tabPageTestArea.TabIndex = 2;
			this.tabPageTestArea.Text = "Test Area";
			this.tabPageTestArea.UseVisualStyleBackColor = true;
			//
			// tableLayoutPanelTestPage
			//
			this.tableLayoutPanelTestPage.ColumnCount = 5;
			this.tableLayoutPanelTestPage.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanelTestPage.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 65F));
			this.tableLayoutPanelTestPage.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanelTestPage.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 35F));
			this.tableLayoutPanelTestPage.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanelTestPage.Controls.Add(this.labelInstructions, 0, 0);
			this.tableLayoutPanelTestPage.Controls.Add(this.labelInput, 0, 1);
			this.tableLayoutPanelTestPage.Controls.Add(this.labelOutput, 0, 4);
			this.tableLayoutPanelTestPage.Controls.Add(this.buttonTest, 2, 3);
			this.tableLayoutPanelTestPage.Controls.Add(this.checkBoxTestReverse, 4, 3);
			this.tableLayoutPanelTestPage.Controls.Add(this.richTextBoxHexOutput, 1, 5);
			this.tableLayoutPanelTestPage.Controls.Add(this.richTextBoxHexInput, 1, 2);
			this.tableLayoutPanelTestPage.Controls.Add(this.ecTextBoxInput, 1, 1);
			this.tableLayoutPanelTestPage.Controls.Add(this.ecTextBoxOutput, 1, 4);
			this.tableLayoutPanelTestPage.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanelTestPage.Location = new System.Drawing.Point(3, 3);
			this.tableLayoutPanelTestPage.Name = "tableLayoutPanelTestPage";
			this.tableLayoutPanelTestPage.RowCount = 6;
			this.tableLayoutPanelTestPage.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanelTestPage.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
			this.tableLayoutPanelTestPage.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
			this.tableLayoutPanelTestPage.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanelTestPage.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
			this.tableLayoutPanelTestPage.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
			this.tableLayoutPanelTestPage.Size = new System.Drawing.Size(596, 394);
			this.tableLayoutPanelTestPage.TabIndex = 0;
			//
			// labelInstructions
			//
			this.labelInstructions.AutoSize = true;
			this.tableLayoutPanelTestPage.SetColumnSpan(this.labelInstructions, 5);
			this.labelInstructions.Location = new System.Drawing.Point(3, 0);
			this.labelInstructions.Name = "labelInstructions";
			this.labelInstructions.Size = new System.Drawing.Size(301, 13);
			this.labelInstructions.TabIndex = 0;
			this.labelInstructions.Text = "To try out this processor, type something below and click Test.";
			//
			// labelInput
			//
			this.labelInput.Anchor = System.Windows.Forms.AnchorStyles.Right;
			this.labelInput.AutoSize = true;
			this.labelInput.Location = new System.Drawing.Point(11, 50);
			this.labelInput.Name = "labelInput";
			this.labelInput.Size = new System.Drawing.Size(34, 13);
			this.labelInput.TabIndex = 1;
			this.labelInput.Text = "&Input:";
			//
			// labelOutput
			//
			this.labelOutput.Anchor = System.Windows.Forms.AnchorStyles.Right;
			this.labelOutput.AutoSize = true;
			this.labelOutput.Location = new System.Drawing.Point(3, 255);
			this.labelOutput.Name = "labelOutput";
			this.labelOutput.Size = new System.Drawing.Size(42, 13);
			this.labelOutput.TabIndex = 2;
			this.labelOutput.Text = "Output:";
			//
			// buttonTest
			//
			this.helpProvider.SetHelpString(this.buttonTest, "Click this button to test the configured converter with the data in the Input box" +
					"");
			this.buttonTest.Location = new System.Drawing.Point(278, 192);
			this.buttonTest.Name = "buttonTest";
			this.helpProvider.SetShowHelp(this.buttonTest, true);
			this.buttonTest.Size = new System.Drawing.Size(75, 23);
			this.buttonTest.TabIndex = 4;
			this.buttonTest.Text = "&Test";
			this.buttonTest.UseVisualStyleBackColor = true;
			this.buttonTest.Click += new System.EventHandler(this.buttonTest_Click);
			//
			// checkBoxTestReverse
			//
			this.checkBoxTestReverse.AutoSize = true;
			this.helpProvider.SetHelpString(this.checkBoxTestReverse, "Check this box to try the reverse direction for the conversion");
			this.checkBoxTestReverse.Location = new System.Drawing.Point(481, 192);
			this.checkBoxTestReverse.Name = "checkBoxTestReverse";
			this.helpProvider.SetShowHelp(this.checkBoxTestReverse, true);
			this.checkBoxTestReverse.Size = new System.Drawing.Size(111, 17);
			this.checkBoxTestReverse.TabIndex = 5;
			this.checkBoxTestReverse.Text = "&Reverse Direction";
			this.checkBoxTestReverse.UseVisualStyleBackColor = true;
			this.checkBoxTestReverse.CheckedChanged += new System.EventHandler(this.checkBoxReverse_CheckedChanged);
			//
			// richTextBoxHexOutput
			//
			this.tableLayoutPanelTestPage.SetColumnSpan(this.richTextBoxHexOutput, 4);
			this.richTextBoxHexOutput.Dock = System.Windows.Forms.DockStyle.Fill;
			this.richTextBoxHexOutput.Location = new System.Drawing.Point(51, 309);
			this.richTextBoxHexOutput.Name = "richTextBoxHexOutput";
			this.richTextBoxHexOutput.ReadOnly = true;
			this.richTextBoxHexOutput.Size = new System.Drawing.Size(542, 82);
			this.richTextBoxHexOutput.TabIndex = 6;
			this.richTextBoxHexOutput.Text = "";
			//
			// richTextBoxHexInput
			//
			this.tableLayoutPanelTestPage.SetColumnSpan(this.richTextBoxHexInput, 4);
			this.richTextBoxHexInput.Dock = System.Windows.Forms.DockStyle.Fill;
			this.richTextBoxHexInput.Location = new System.Drawing.Point(51, 104);
			this.richTextBoxHexInput.Name = "richTextBoxHexInput";
			this.richTextBoxHexInput.ReadOnly = true;
			this.richTextBoxHexInput.Size = new System.Drawing.Size(542, 82);
			this.richTextBoxHexInput.TabIndex = 6;
			this.richTextBoxHexInput.Text = "";
			//
			// contextMenuStripTestBoxes
			//
			this.contextMenuStripTestBoxes.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
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
			this.contextMenuStripTestBoxes.Name = "contextMenuStrip";
			this.contextMenuStripTestBoxes.Size = new System.Drawing.Size(200, 204);
			this.contextMenuStripTestBoxes.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenuStrip_Opening);
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
			// tabPageAdvanced
			//
			this.tabPageAdvanced.Controls.Add(this.tableLayoutPanelTransductionTypes);
			this.tabPageAdvanced.Location = new System.Drawing.Point(4, 22);
			this.tabPageAdvanced.Name = "tabPageAdvanced";
			this.tabPageAdvanced.Padding = new System.Windows.Forms.Padding(3);
			this.tabPageAdvanced.Size = new System.Drawing.Size(602, 400);
			this.tabPageAdvanced.TabIndex = 3;
			this.tabPageAdvanced.Text = "Advanced";
			this.tabPageAdvanced.UseVisualStyleBackColor = true;
			//
			// tableLayoutPanelTransductionTypes
			//
			this.tableLayoutPanelTransductionTypes.ColumnCount = 2;
			this.tableLayoutPanelTransductionTypes.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanelTransductionTypes.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanelTransductionTypes.Controls.Add(this.labelRightEncodingName, 0, 2);
			this.tableLayoutPanelTransductionTypes.Controls.Add(this.comboBoxEncodingNamesRhs, 1, 2);
			this.tableLayoutPanelTransductionTypes.Controls.Add(this.labelFriendlyName, 0, 0);
			this.tableLayoutPanelTransductionTypes.Controls.Add(this.textBoxFriendlyName, 1, 0);
			this.tableLayoutPanelTransductionTypes.Controls.Add(this.labelLeftEncodingName, 0, 1);
			this.tableLayoutPanelTransductionTypes.Controls.Add(this.comboBoxEncodingNamesLhs, 1, 1);
			this.tableLayoutPanelTransductionTypes.Controls.Add(this.labelTransductionType, 0, 3);
			this.tableLayoutPanelTransductionTypes.Controls.Add(this.flowLayoutPanelTransductionTypes, 1, 3);
			this.tableLayoutPanelTransductionTypes.Controls.Add(this.labelProperties, 0, 4);
			this.tableLayoutPanelTransductionTypes.Controls.Add(this.dataGridViewProperties, 1, 4);
			this.tableLayoutPanelTransductionTypes.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanelTransductionTypes.Location = new System.Drawing.Point(3, 3);
			this.tableLayoutPanelTransductionTypes.Name = "tableLayoutPanelTransductionTypes";
			this.tableLayoutPanelTransductionTypes.RowCount = 5;
			this.tableLayoutPanelTransductionTypes.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanelTransductionTypes.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanelTransductionTypes.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanelTransductionTypes.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.tableLayoutPanelTransductionTypes.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.tableLayoutPanelTransductionTypes.Size = new System.Drawing.Size(596, 394);
			this.tableLayoutPanelTransductionTypes.TabIndex = 4;
			//
			// labelRightEncodingName
			//
			this.labelRightEncodingName.Anchor = System.Windows.Forms.AnchorStyles.Right;
			this.labelRightEncodingName.AutoSize = true;
			this.labelRightEncodingName.Location = new System.Drawing.Point(3, 60);
			this.labelRightEncodingName.Name = "labelRightEncodingName";
			this.labelRightEncodingName.Size = new System.Drawing.Size(114, 13);
			this.labelRightEncodingName.TabIndex = 6;
			this.labelRightEncodingName.Text = "&Right Encoding Name:";
			//
			// comboBoxEncodingNamesRhs
			//
			this.comboBoxEncodingNamesRhs.Dock = System.Windows.Forms.DockStyle.Fill;
			this.comboBoxEncodingNamesRhs.FormattingEnabled = true;
			this.helpProvider.SetHelpString(this.comboBoxEncodingNamesRhs, "Enter or modify the name of the right-hand side encoding");
			this.comboBoxEncodingNamesRhs.Location = new System.Drawing.Point(123, 56);
			this.comboBoxEncodingNamesRhs.Name = "comboBoxEncodingNamesRhs";
			this.helpProvider.SetShowHelp(this.comboBoxEncodingNamesRhs, true);
			this.comboBoxEncodingNamesRhs.Size = new System.Drawing.Size(470, 21);
			this.comboBoxEncodingNamesRhs.TabIndex = 7;
			this.comboBoxEncodingNamesRhs.SelectedIndexChanged += new System.EventHandler(this.SomethingChanged);
			//
			// labelFriendlyName
			//
			this.labelFriendlyName.Anchor = System.Windows.Forms.AnchorStyles.Right;
			this.labelFriendlyName.AutoSize = true;
			this.labelFriendlyName.Location = new System.Drawing.Point(40, 6);
			this.labelFriendlyName.Name = "labelFriendlyName";
			this.labelFriendlyName.Size = new System.Drawing.Size(77, 13);
			this.labelFriendlyName.TabIndex = 0;
			this.labelFriendlyName.Text = "&Friendly Name:";
			//
			// textBoxFriendlyName
			//
			this.textBoxFriendlyName.Dock = System.Windows.Forms.DockStyle.Fill;
			this.helpProvider.SetHelpString(this.textBoxFriendlyName, "Enter or modify the name by which this converter is known");
			this.textBoxFriendlyName.Location = new System.Drawing.Point(123, 3);
			this.textBoxFriendlyName.Name = "textBoxFriendlyName";
			this.helpProvider.SetShowHelp(this.textBoxFriendlyName, true);
			this.textBoxFriendlyName.Size = new System.Drawing.Size(470, 20);
			this.textBoxFriendlyName.TabIndex = 3;
			this.toolTip.SetToolTip(this.textBoxFriendlyName, "Enter a user-friendly name for this converter (e.g. \"SIL IPA93<>UNICODE\")");
			this.textBoxFriendlyName.TextChanged += new System.EventHandler(this.SomethingChanged);
			//
			// labelLeftEncodingName
			//
			this.labelLeftEncodingName.Anchor = System.Windows.Forms.AnchorStyles.Right;
			this.labelLeftEncodingName.AutoSize = true;
			this.labelLeftEncodingName.Location = new System.Drawing.Point(10, 33);
			this.labelLeftEncodingName.Name = "labelLeftEncodingName";
			this.labelLeftEncodingName.Size = new System.Drawing.Size(107, 13);
			this.labelLeftEncodingName.TabIndex = 4;
			this.labelLeftEncodingName.Text = "&Left Encoding Name:";
			//
			// comboBoxEncodingNamesLhs
			//
			this.comboBoxEncodingNamesLhs.Dock = System.Windows.Forms.DockStyle.Fill;
			this.comboBoxEncodingNamesLhs.FormattingEnabled = true;
			this.helpProvider.SetHelpString(this.comboBoxEncodingNamesLhs, "Enter or modify the name of the left-hand side encoding");
			this.comboBoxEncodingNamesLhs.Location = new System.Drawing.Point(123, 29);
			this.comboBoxEncodingNamesLhs.Name = "comboBoxEncodingNamesLhs";
			this.helpProvider.SetShowHelp(this.comboBoxEncodingNamesLhs, true);
			this.comboBoxEncodingNamesLhs.Size = new System.Drawing.Size(470, 21);
			this.comboBoxEncodingNamesLhs.TabIndex = 5;
			this.comboBoxEncodingNamesLhs.SelectedIndexChanged += new System.EventHandler(this.SomethingChanged);
			//
			// labelTransductionType
			//
			this.labelTransductionType.Anchor = System.Windows.Forms.AnchorStyles.Right;
			this.labelTransductionType.AutoSize = true;
			this.labelTransductionType.Location = new System.Drawing.Point(13, 152);
			this.labelTransductionType.Name = "labelTransductionType";
			this.labelTransductionType.Size = new System.Drawing.Size(104, 13);
			this.labelTransductionType.TabIndex = 10;
			this.labelTransductionType.Text = "Transduction Types:";
			//
			// flowLayoutPanelTransductionTypes
			//
			this.flowLayoutPanelTransductionTypes.Controls.Add(this.checkBoxUnicodeEncodingConversion);
			this.flowLayoutPanelTransductionTypes.Controls.Add(this.checkBoxTransliteration);
			this.flowLayoutPanelTransductionTypes.Controls.Add(this.checkBoxICUTransliteration);
			this.flowLayoutPanelTransductionTypes.Controls.Add(this.checkBoxICURegularExpression);
			this.flowLayoutPanelTransductionTypes.Controls.Add(this.checkBoxICUConverter);
			this.flowLayoutPanelTransductionTypes.Controls.Add(this.checkBoxCodePage);
			this.flowLayoutPanelTransductionTypes.Controls.Add(this.checkBoxNonUnicodeEncodingConversion);
			this.flowLayoutPanelTransductionTypes.Controls.Add(this.checkBoxSpellingFixerProject);
			this.flowLayoutPanelTransductionTypes.Controls.Add(this.checkBoxPythonScript);
			this.flowLayoutPanelTransductionTypes.Controls.Add(this.checkBoxPerlExpression);
			this.flowLayoutPanelTransductionTypes.Controls.Add(this.checkBoxSpare1);
			this.flowLayoutPanelTransductionTypes.Controls.Add(this.checkBoxSpare2);
			this.flowLayoutPanelTransductionTypes.Dock = System.Windows.Forms.DockStyle.Fill;
			this.flowLayoutPanelTransductionTypes.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
			this.flowLayoutPanelTransductionTypes.Location = new System.Drawing.Point(123, 83);
			this.flowLayoutPanelTransductionTypes.Name = "flowLayoutPanelTransductionTypes";
			this.flowLayoutPanelTransductionTypes.Size = new System.Drawing.Size(470, 151);
			this.flowLayoutPanelTransductionTypes.TabIndex = 9;
			//
			// checkBoxUnicodeEncodingConversion
			//
			this.checkBoxUnicodeEncodingConversion.AutoSize = true;
			this.checkBoxUnicodeEncodingConversion.Location = new System.Drawing.Point(3, 3);
			this.checkBoxUnicodeEncodingConversion.Name = "checkBoxUnicodeEncodingConversion";
			this.checkBoxUnicodeEncodingConversion.Size = new System.Drawing.Size(170, 17);
			this.checkBoxUnicodeEncodingConversion.TabIndex = 0;
			this.checkBoxUnicodeEncodingConversion.Text = "&Unicode Encoding Conversion";
			this.checkBoxUnicodeEncodingConversion.UseVisualStyleBackColor = true;
			this.checkBoxUnicodeEncodingConversion.CheckedChanged += new System.EventHandler(this.SomethingChanged);
			//
			// checkBoxTransliteration
			//
			this.checkBoxTransliteration.AutoSize = true;
			this.checkBoxTransliteration.Location = new System.Drawing.Point(3, 26);
			this.checkBoxTransliteration.Name = "checkBoxTransliteration";
			this.checkBoxTransliteration.Size = new System.Drawing.Size(92, 17);
			this.checkBoxTransliteration.TabIndex = 1;
			this.checkBoxTransliteration.Text = "&Transliteration";
			this.checkBoxTransliteration.UseVisualStyleBackColor = true;
			this.checkBoxTransliteration.CheckedChanged += new System.EventHandler(this.SomethingChanged);
			//
			// checkBoxICUTransliteration
			//
			this.checkBoxICUTransliteration.AutoSize = true;
			this.checkBoxICUTransliteration.Location = new System.Drawing.Point(3, 49);
			this.checkBoxICUTransliteration.Name = "checkBoxICUTransliteration";
			this.checkBoxICUTransliteration.Size = new System.Drawing.Size(113, 17);
			this.checkBoxICUTransliteration.TabIndex = 2;
			this.checkBoxICUTransliteration.Text = "&ICU Transliteration";
			this.checkBoxICUTransliteration.UseVisualStyleBackColor = true;
			this.checkBoxICUTransliteration.CheckedChanged += new System.EventHandler(this.SomethingChanged);
			//
			// checkBoxICURegularExpression
			//
			this.checkBoxICURegularExpression.AutoSize = true;
			this.checkBoxICURegularExpression.Location = new System.Drawing.Point(3, 72);
			this.checkBoxICURegularExpression.Name = "checkBoxICURegularExpression";
			this.checkBoxICURegularExpression.Size = new System.Drawing.Size(138, 17);
			this.checkBoxICURegularExpression.TabIndex = 3;
			this.checkBoxICURegularExpression.Text = "ICU Regular &Expression";
			this.checkBoxICURegularExpression.UseVisualStyleBackColor = true;
			this.checkBoxICURegularExpression.CheckedChanged += new System.EventHandler(this.SomethingChanged);
			//
			// checkBoxICUConverter
			//
			this.checkBoxICUConverter.AutoSize = true;
			this.checkBoxICUConverter.Location = new System.Drawing.Point(3, 95);
			this.checkBoxICUConverter.Name = "checkBoxICUConverter";
			this.checkBoxICUConverter.Size = new System.Drawing.Size(93, 17);
			this.checkBoxICUConverter.TabIndex = 4;
			this.checkBoxICUConverter.Text = "ICU &Converter";
			this.checkBoxICUConverter.UseVisualStyleBackColor = true;
			this.checkBoxICUConverter.CheckedChanged += new System.EventHandler(this.SomethingChanged);
			//
			// checkBoxCodePage
			//
			this.checkBoxCodePage.AutoSize = true;
			this.checkBoxCodePage.Location = new System.Drawing.Point(3, 118);
			this.checkBoxCodePage.Name = "checkBoxCodePage";
			this.checkBoxCodePage.Size = new System.Drawing.Size(79, 17);
			this.checkBoxCodePage.TabIndex = 5;
			this.checkBoxCodePage.Text = "Code Pa&ge";
			this.checkBoxCodePage.UseVisualStyleBackColor = true;
			this.checkBoxCodePage.CheckedChanged += new System.EventHandler(this.SomethingChanged);
			//
			// checkBoxNonUnicodeEncodingConversion
			//
			this.checkBoxNonUnicodeEncodingConversion.AutoSize = true;
			this.checkBoxNonUnicodeEncodingConversion.Location = new System.Drawing.Point(179, 3);
			this.checkBoxNonUnicodeEncodingConversion.Name = "checkBoxNonUnicodeEncodingConversion";
			this.checkBoxNonUnicodeEncodingConversion.Size = new System.Drawing.Size(193, 17);
			this.checkBoxNonUnicodeEncodingConversion.TabIndex = 6;
			this.checkBoxNonUnicodeEncodingConversion.Text = "&Non-Unicode Encoding Conversion";
			this.checkBoxNonUnicodeEncodingConversion.UseVisualStyleBackColor = true;
			this.checkBoxNonUnicodeEncodingConversion.CheckedChanged += new System.EventHandler(this.SomethingChanged);
			//
			// checkBoxSpellingFixerProject
			//
			this.checkBoxSpellingFixerProject.AutoSize = true;
			this.checkBoxSpellingFixerProject.Location = new System.Drawing.Point(179, 26);
			this.checkBoxSpellingFixerProject.Name = "checkBoxSpellingFixerProject";
			this.checkBoxSpellingFixerProject.Size = new System.Drawing.Size(124, 17);
			this.checkBoxSpellingFixerProject.TabIndex = 7;
			this.checkBoxSpellingFixerProject.Text = "Spelling Fixer Pro&ject";
			this.checkBoxSpellingFixerProject.UseVisualStyleBackColor = true;
			this.checkBoxSpellingFixerProject.CheckedChanged += new System.EventHandler(this.SomethingChanged);
			//
			// checkBoxPythonScript
			//
			this.checkBoxPythonScript.AutoSize = true;
			this.checkBoxPythonScript.Location = new System.Drawing.Point(179, 49);
			this.checkBoxPythonScript.Name = "checkBoxPythonScript";
			this.checkBoxPythonScript.Size = new System.Drawing.Size(89, 17);
			this.checkBoxPythonScript.TabIndex = 8;
			this.checkBoxPythonScript.Text = "&Python Script";
			this.checkBoxPythonScript.UseVisualStyleBackColor = true;
			this.checkBoxPythonScript.CheckedChanged += new System.EventHandler(this.SomethingChanged);
			//
			// checkBoxPerlExpression
			//
			this.checkBoxPerlExpression.AutoSize = true;
			this.checkBoxPerlExpression.Location = new System.Drawing.Point(179, 72);
			this.checkBoxPerlExpression.Name = "checkBoxPerlExpression";
			this.checkBoxPerlExpression.Size = new System.Drawing.Size(98, 17);
			this.checkBoxPerlExpression.TabIndex = 9;
			this.checkBoxPerlExpression.Text = "Perl E&xpression";
			this.checkBoxPerlExpression.UseVisualStyleBackColor = true;
			this.checkBoxPerlExpression.CheckedChanged += new System.EventHandler(this.SomethingChanged);
			//
			// checkBoxSpare1
			//
			this.checkBoxSpare1.AutoSize = true;
			this.checkBoxSpare1.Location = new System.Drawing.Point(179, 95);
			this.checkBoxSpare1.Name = "checkBoxSpare1";
			this.checkBoxSpare1.Size = new System.Drawing.Size(138, 17);
			this.checkBoxSpare1.TabIndex = 10;
			this.checkBoxSpare1.Text = "Spare &1 (user-definable)";
			this.checkBoxSpare1.UseVisualStyleBackColor = true;
			this.checkBoxSpare1.CheckedChanged += new System.EventHandler(this.SomethingChanged);
			//
			// checkBoxSpare2
			//
			this.checkBoxSpare2.AutoSize = true;
			this.checkBoxSpare2.Location = new System.Drawing.Point(179, 118);
			this.checkBoxSpare2.Name = "checkBoxSpare2";
			this.checkBoxSpare2.Size = new System.Drawing.Size(138, 17);
			this.checkBoxSpare2.TabIndex = 11;
			this.checkBoxSpare2.Text = "Spare &2 (user-definable)";
			this.checkBoxSpare2.UseVisualStyleBackColor = true;
			this.checkBoxSpare2.CheckedChanged += new System.EventHandler(this.SomethingChanged);
			//
			// labelProperties
			//
			this.labelProperties.Anchor = System.Windows.Forms.AnchorStyles.Right;
			this.labelProperties.AutoSize = true;
			this.labelProperties.Location = new System.Drawing.Point(14, 309);
			this.labelProperties.Name = "labelProperties";
			this.labelProperties.Size = new System.Drawing.Size(103, 13);
			this.labelProperties.TabIndex = 15;
			this.labelProperties.Text = "Converter Properties";
			//
			// dataGridViewProperties
			//
			this.dataGridViewProperties.AllowUserToAddRows = false;
			this.dataGridViewProperties.AllowUserToDeleteRows = false;
			this.dataGridViewProperties.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.dataGridViewProperties.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
			this.ColumnKey,
			this.ColumnValue});
			this.dataGridViewProperties.Dock = System.Windows.Forms.DockStyle.Fill;
			this.dataGridViewProperties.Location = new System.Drawing.Point(123, 240);
			this.dataGridViewProperties.Name = "dataGridViewProperties";
			this.dataGridViewProperties.ReadOnly = true;
			this.dataGridViewProperties.RowHeadersVisible = false;
			this.dataGridViewProperties.Size = new System.Drawing.Size(470, 151);
			this.dataGridViewProperties.TabIndex = 16;
			//
			// ColumnKey
			//
			this.ColumnKey.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.ColumnKey.HeaderText = "Property Key";
			this.ColumnKey.Name = "ColumnKey";
			this.ColumnKey.ReadOnly = true;
			this.ColumnKey.Width = 92;
			//
			// ColumnValue
			//
			this.ColumnValue.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.ColumnValue.HeaderText = "Property Value";
			this.ColumnValue.Name = "ColumnValue";
			this.ColumnValue.ReadOnly = true;
			this.ColumnValue.Width = 101;
			//
			// buttonApply
			//
			this.buttonApply.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonApply.Enabled = false;
			this.helpProvider.SetHelpString(this.buttonApply, "Click this button to apply the configured values for this converter");
			this.buttonApply.Location = new System.Drawing.Point(547, 444);
			this.buttonApply.Name = "buttonApply";
			this.helpProvider.SetShowHelp(this.buttonApply, true);
			this.buttonApply.Size = new System.Drawing.Size(75, 23);
			this.buttonApply.TabIndex = 2;
			this.buttonApply.Text = "&Apply";
			this.buttonApply.UseVisualStyleBackColor = true;
			this.buttonApply.Click += new System.EventHandler(this.buttonApply_Click);
			//
			// buttonCancel
			//
			this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.helpProvider.SetHelpString(this.buttonCancel, "Click this button to cancel this dialog");
			this.buttonCancel.Location = new System.Drawing.Point(466, 444);
			this.buttonCancel.Name = "buttonCancel";
			this.helpProvider.SetShowHelp(this.buttonCancel, true);
			this.buttonCancel.Size = new System.Drawing.Size(75, 23);
			this.buttonCancel.TabIndex = 3;
			this.buttonCancel.Text = "&Cancel";
			this.buttonCancel.UseVisualStyleBackColor = true;
			//
			// buttonOK
			//
			this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.helpProvider.SetHelpString(this.buttonOK, "Click this button to accept the configured values for this converter");
			this.buttonOK.Location = new System.Drawing.Point(385, 444);
			this.buttonOK.Name = "buttonOK";
			this.helpProvider.SetShowHelp(this.buttonOK, true);
			this.buttonOK.Size = new System.Drawing.Size(75, 23);
			this.buttonOK.TabIndex = 4;
			this.buttonOK.Text = "&OK";
			this.buttonOK.UseVisualStyleBackColor = true;
			this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
			//
			// buttonSaveInRepository
			//
			this.buttonSaveInRepository.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.buttonSaveInRepository.Location = new System.Drawing.Point(12, 444);
			this.buttonSaveInRepository.Name = "buttonSaveInRepository";
			this.buttonSaveInRepository.Size = new System.Drawing.Size(129, 23);
			this.buttonSaveInRepository.TabIndex = 5;
			this.buttonSaveInRepository.Text = "&Save In Repository";
			this.buttonSaveInRepository.UseVisualStyleBackColor = true;
			this.buttonSaveInRepository.Visible = false;
			this.buttonSaveInRepository.Click += new System.EventHandler(this.buttonSaveInRepository_Click);
			//
			// fontDialog
			//
			this.fontDialog.AllowScriptChange = false;
			this.fontDialog.ShowColor = true;
			//
			// ecTextBoxInput
			//
			this.tableLayoutPanelTestPage.SetColumnSpan(this.ecTextBoxInput, 4);
			this.ecTextBoxInput.ContextMenuStrip = this.contextMenuStripTestBoxes;
			this.ecTextBoxInput.Dock = System.Windows.Forms.DockStyle.Fill;
			this.ecTextBoxInput.Location = new System.Drawing.Point(51, 16);
			this.ecTextBoxInput.Multiline = true;
			this.ecTextBoxInput.Name = "ecTextBoxInput";
			this.ecTextBoxInput.Size = new System.Drawing.Size(542, 82);
			this.ecTextBoxInput.TabIndex = 3;
			this.ecTextBoxInput.TextChanged += new System.EventHandler(this.ecTextBoxInput_TextChanged);
			//
			// ecTextBoxOutput
			//
			this.tableLayoutPanelTestPage.SetColumnSpan(this.ecTextBoxOutput, 4);
			this.ecTextBoxOutput.ContextMenuStrip = this.contextMenuStripTestBoxes;
			this.ecTextBoxOutput.Dock = System.Windows.Forms.DockStyle.Fill;
			this.ecTextBoxOutput.Location = new System.Drawing.Point(51, 221);
			this.ecTextBoxOutput.Multiline = true;
			this.ecTextBoxOutput.Name = "ecTextBoxOutput";
			this.ecTextBoxOutput.ReadOnly = true;
			this.ecTextBoxOutput.Size = new System.Drawing.Size(542, 82);
			this.ecTextBoxOutput.TabIndex = 3;
			//
			// AutoConfigDialog
			//
			this.AcceptButton = this.buttonOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonCancel;
			this.ClientSize = new System.Drawing.Size(634, 479);
			this.Controls.Add(this.buttonSaveInRepository);
			this.Controls.Add(this.buttonOK);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonApply);
			this.Controls.Add(this.tabControl);
			this.HelpButton = true;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(486, 500);
			this.Name = "AutoConfigDialog";
			this.Text = "AutoConfigDialog";
			this.tabControl.ResumeLayout(false);
			this.tabPageAbout.ResumeLayout(false);
			this.tabPageTestArea.ResumeLayout(false);
			this.tableLayoutPanelTestPage.ResumeLayout(false);
			this.tableLayoutPanelTestPage.PerformLayout();
			this.contextMenuStripTestBoxes.ResumeLayout(false);
			this.tabPageAdvanced.ResumeLayout(false);
			this.tableLayoutPanelTransductionTypes.ResumeLayout(false);
			this.tableLayoutPanelTransductionTypes.PerformLayout();
			this.flowLayoutPanelTransductionTypes.ResumeLayout(false);
			this.flowLayoutPanelTransductionTypes.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.dataGridViewProperties)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		public System.Windows.Forms.TabControl tabControl;
		public System.Windows.Forms.TabPage tabPageAbout;
		public System.Windows.Forms.WebBrowser webBrowserHelp;
		public System.Windows.Forms.TabPage tabPageSetup;
		public System.Windows.Forms.TabPage tabPageTestArea;
		public System.Windows.Forms.TableLayoutPanel tableLayoutPanelTestPage;
		public System.Windows.Forms.Label labelInstructions;
		public System.Windows.Forms.Label labelInput;
		public System.Windows.Forms.Label labelOutput;
		public System.Windows.Forms.Button buttonTest;
		public System.Windows.Forms.CheckBox checkBoxTestReverse;
		public System.Windows.Forms.RichTextBox richTextBoxHexOutput;
		public System.Windows.Forms.RichTextBox richTextBoxHexInput;
		public System.Windows.Forms.Button buttonApply;
		public System.Windows.Forms.Button buttonCancel;
		public System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.TabPage tabPageAdvanced;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanelTransductionTypes;
		private System.Windows.Forms.Label labelFriendlyName;
		private System.Windows.Forms.TextBox textBoxFriendlyName;
		private System.Windows.Forms.ToolTip toolTip;
		private System.Windows.Forms.Label labelLeftEncodingName;
		private System.Windows.Forms.ComboBox comboBoxEncodingNamesLhs;
		private System.Windows.Forms.Label labelRightEncodingName;
		private System.Windows.Forms.ComboBox comboBoxEncodingNamesRhs;
		private System.Windows.Forms.CheckBox checkBoxUnicodeEncodingConversion;
		private System.Windows.Forms.CheckBox checkBoxTransliteration;
		private System.Windows.Forms.CheckBox checkBoxICUTransliteration;
		private System.Windows.Forms.CheckBox checkBoxICURegularExpression;
		private System.Windows.Forms.CheckBox checkBoxICUConverter;
		private System.Windows.Forms.CheckBox checkBoxCodePage;
		private System.Windows.Forms.CheckBox checkBoxNonUnicodeEncodingConversion;
		private System.Windows.Forms.CheckBox checkBoxSpellingFixerProject;
		private System.Windows.Forms.CheckBox checkBoxPythonScript;
		private System.Windows.Forms.CheckBox checkBoxPerlExpression;
		private System.Windows.Forms.CheckBox checkBoxSpare1;
		private System.Windows.Forms.CheckBox checkBoxSpare2;
		private System.Windows.Forms.FlowLayoutPanel flowLayoutPanelTransductionTypes;
		private System.Windows.Forms.Label labelTransductionType;
		private System.Windows.Forms.Label labelProperties;
		private System.Windows.Forms.DataGridView dataGridViewProperties;
		protected System.Windows.Forms.Button buttonSaveInRepository;
		private System.Windows.Forms.DataGridViewTextBoxColumn ColumnKey;
		private System.Windows.Forms.DataGridViewTextBoxColumn ColumnValue;
		private SilEncConverters40.EcTextBox ecTextBoxInput;
		private SilEncConverters40.EcTextBox ecTextBoxOutput;
		protected System.Windows.Forms.HelpProvider helpProvider;
		protected internal System.Windows.Forms.ContextMenuStrip contextMenuStripTestBoxes;
		private System.Windows.Forms.ToolStripMenuItem changeFontToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
		private System.Windows.Forms.ToolStripMenuItem undoToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripMenuItem cutToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem copyToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem pasteToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem deleteToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripMenuItem selectAllToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
		private System.Windows.Forms.ToolStripMenuItem right2LeftToolStripMenuItem;
		protected internal System.Windows.Forms.FontDialog fontDialog;
	}
}
namespace TECkit_Mapping_Editor
{
	partial class TECkitMapEditorForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TECkitMapEditorForm));
			this.splitContainer1 = new System.Windows.Forms.SplitContainer();
			this.textBoxCompilerResults = new System.Windows.Forms.TextBox();
			this.richTextBoxMapEditor = new System.Windows.Forms.RichTextBox();
			this.menuStrip = new System.Windows.Forms.MenuStrip();
			this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.newToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.revertTolastSavedCopyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator8 = new System.Windows.Forms.ToolStripSeparator();
			this.recentFilesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.compileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.autoCompileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
			this.addToSystemRepositoryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator();
			this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.undoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
			this.cutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.copyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.clearToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
			this.findToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.findNextToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.replaceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator9 = new System.Windows.Forms.ToolStripSeparator();
			this.toggleCodePointToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.unicodeValuesWindowToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.setSampleDataFontToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.setConvertedDataFontToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.openTECkitDocToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.tableLayoutPanelSamples = new System.Windows.Forms.TableLayoutPanel();
			this.labelSampleLhs = new System.Windows.Forms.Label();
			this.textBoxSampleReverse = new System.Windows.Forms.TextBox();
			this.labelRhsSample = new System.Windows.Forms.Label();
			this.labelRoundtrip = new System.Windows.Forms.Label();
			this.textBoxSample = new System.Windows.Forms.TextBox();
			this.textBoxSampleForward = new System.Windows.Forms.TextBox();
			this.dataGridViewCodePointValues = new System.Windows.Forms.DataGridView();
			this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.dataGridViewTextBoxColumn5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.toolTip = new System.Windows.Forms.ToolTip(this.components);
			this.fontDialog = new System.Windows.Forms.FontDialog();
			this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
			this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
			this.statusStrip = new System.Windows.Forms.StatusStrip();
			this.toolStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.helpProvider = new System.Windows.Forms.HelpProvider();
			this.splitContainer1.Panel1.SuspendLayout();
			this.splitContainer1.Panel2.SuspendLayout();
			this.splitContainer1.SuspendLayout();
			this.menuStrip.SuspendLayout();
			this.tableLayoutPanel.SuspendLayout();
			this.tableLayoutPanelSamples.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.dataGridViewCodePointValues)).BeginInit();
			this.statusStrip.SuspendLayout();
			this.SuspendLayout();
			//
			// splitContainer1
			//
			this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer1.Location = new System.Drawing.Point(3, 3);
			this.splitContainer1.Name = "splitContainer1";
			this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
			//
			// splitContainer1.Panel1
			//
			this.splitContainer1.Panel1.Controls.Add(this.textBoxCompilerResults);
			//
			// splitContainer1.Panel2
			//
			this.splitContainer1.Panel2.Controls.Add(this.richTextBoxMapEditor);
			this.splitContainer1.Size = new System.Drawing.Size(636, 381);
			this.splitContainer1.SplitterDistance = 44;
			this.splitContainer1.TabIndex = 0;
			//
			// textBoxCompilerResults
			//
			this.textBoxCompilerResults.Cursor = System.Windows.Forms.Cursors.Hand;
			this.textBoxCompilerResults.Dock = System.Windows.Forms.DockStyle.Fill;
			this.textBoxCompilerResults.Location = new System.Drawing.Point(0, 0);
			this.textBoxCompilerResults.Multiline = true;
			this.textBoxCompilerResults.Name = "textBoxCompilerResults";
			this.textBoxCompilerResults.ReadOnly = true;
			this.textBoxCompilerResults.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.textBoxCompilerResults.Size = new System.Drawing.Size(636, 44);
			this.textBoxCompilerResults.TabIndex = 0;
			this.toolTip.SetToolTip(this.textBoxCompilerResults, "This pane displays compiler errors. You can click on a line to jump to the corres" +
					"ponding error line in the map");
			this.textBoxCompilerResults.WordWrap = false;
			this.textBoxCompilerResults.MouseClick += new System.Windows.Forms.MouseEventHandler(this.textBoxCompilerResults_JumpToCompilerError);
			//
			// richTextBoxMapEditor
			//
			this.richTextBoxMapEditor.AcceptsTab = true;
			this.richTextBoxMapEditor.AutoWordSelection = true;
			this.richTextBoxMapEditor.Dock = System.Windows.Forms.DockStyle.Fill;
			this.richTextBoxMapEditor.EnableAutoDragDrop = true;
			this.richTextBoxMapEditor.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.richTextBoxMapEditor.HideSelection = false;
			this.richTextBoxMapEditor.Location = new System.Drawing.Point(0, 0);
			this.richTextBoxMapEditor.Name = "richTextBoxMapEditor";
			this.richTextBoxMapEditor.ShowSelectionMargin = true;
			this.richTextBoxMapEditor.Size = new System.Drawing.Size(636, 333);
			this.richTextBoxMapEditor.TabIndex = 0;
			this.richTextBoxMapEditor.Text = "";
			this.richTextBoxMapEditor.WordWrap = false;
			this.richTextBoxMapEditor.MouseClick += new System.Windows.Forms.MouseEventHandler(this.richTextBoxMapEditor_MouseClick);
			this.richTextBoxMapEditor.SelectionChanged += new System.EventHandler(this.richTextBoxMapEditor_SelectionChanged);
			this.richTextBoxMapEditor.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.richTextBoxMapEditor_PreviewKeyDown);
			this.richTextBoxMapEditor.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.richTextBoxMapEditor_KeyPress);
			this.richTextBoxMapEditor.TextChanged += new System.EventHandler(this.richTextBoxMapEditor_TextChanged);
			//
			// menuStrip
			//
			this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.fileToolStripMenuItem,
			this.editToolStripMenuItem,
			this.viewToolStripMenuItem,
			this.helpToolStripMenuItem});
			this.menuStrip.Location = new System.Drawing.Point(0, 0);
			this.menuStrip.Name = "menuStrip";
			this.menuStrip.Size = new System.Drawing.Size(642, 24);
			this.menuStrip.TabIndex = 2;
			this.menuStrip.Text = "menuStrip";
			//
			// fileToolStripMenuItem
			//
			this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.newToolStripMenuItem,
			this.openToolStripMenuItem,
			this.revertTolastSavedCopyToolStripMenuItem,
			this.toolStripSeparator8,
			this.recentFilesToolStripMenuItem,
			this.toolStripSeparator1,
			this.saveToolStripMenuItem,
			this.saveAsToolStripMenuItem,
			this.toolStripSeparator2,
			this.compileToolStripMenuItem,
			this.autoCompileToolStripMenuItem,
			this.toolStripSeparator6,
			this.addToSystemRepositoryToolStripMenuItem,
			this.toolStripSeparator7,
			this.exitToolStripMenuItem});
			this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
			this.fileToolStripMenuItem.Size = new System.Drawing.Size(35, 20);
			this.fileToolStripMenuItem.Text = "&File";
			this.fileToolStripMenuItem.DropDownOpening += new System.EventHandler(this.fileToolStripMenuItem_DropDownOpening);
			//
			// newToolStripMenuItem
			//
			this.newToolStripMenuItem.Name = "newToolStripMenuItem";
			this.newToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
			this.newToolStripMenuItem.Size = new System.Drawing.Size(199, 22);
			this.newToolStripMenuItem.Text = "&New";
			this.newToolStripMenuItem.ToolTipText = "Open a new map file";
			this.newToolStripMenuItem.Click += new System.EventHandler(this.newToolStripMenuItem_Click);
			//
			// openToolStripMenuItem
			//
			this.openToolStripMenuItem.Name = "openToolStripMenuItem";
			this.openToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
			this.openToolStripMenuItem.Size = new System.Drawing.Size(199, 22);
			this.openToolStripMenuItem.Text = "&Open ...";
			this.openToolStripMenuItem.ToolTipText = "Open an existing map file";
			this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
			//
			// revertTolastSavedCopyToolStripMenuItem
			//
			this.revertTolastSavedCopyToolStripMenuItem.Name = "revertTolastSavedCopyToolStripMenuItem";
			this.revertTolastSavedCopyToolStripMenuItem.Size = new System.Drawing.Size(199, 22);
			this.revertTolastSavedCopyToolStripMenuItem.Text = "Revert to &last saved copy";
			this.revertTolastSavedCopyToolStripMenuItem.ToolTipText = "Close this file and open the last version saved";
			this.revertTolastSavedCopyToolStripMenuItem.Click += new System.EventHandler(this.revertTolastSavedCopyToolStripMenuItem_Click);
			//
			// toolStripSeparator8
			//
			this.toolStripSeparator8.Name = "toolStripSeparator8";
			this.toolStripSeparator8.Size = new System.Drawing.Size(196, 6);
			//
			// recentFilesToolStripMenuItem
			//
			this.recentFilesToolStripMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.recentFilesToolStripMenuItem.Name = "recentFilesToolStripMenuItem";
			this.recentFilesToolStripMenuItem.Size = new System.Drawing.Size(199, 22);
			this.recentFilesToolStripMenuItem.Text = "Recent &Files";
			this.recentFilesToolStripMenuItem.ToolTipText = "Click here to open a recently used map";
			//
			// toolStripSeparator1
			//
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(196, 6);
			//
			// saveToolStripMenuItem
			//
			this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
			this.saveToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
			this.saveToolStripMenuItem.Size = new System.Drawing.Size(199, 22);
			this.saveToolStripMenuItem.Text = "&Save";
			this.saveToolStripMenuItem.ToolTipText = "Save this map file";
			this.saveToolStripMenuItem.Click += new System.EventHandler(this.saveToolStripMenuItem_Click);
			//
			// saveAsToolStripMenuItem
			//
			this.saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
			this.saveAsToolStripMenuItem.Size = new System.Drawing.Size(199, 22);
			this.saveAsToolStripMenuItem.Text = "Save &As...";
			this.saveAsToolStripMenuItem.ToolTipText = "Save this map file with a new name";
			this.saveAsToolStripMenuItem.Click += new System.EventHandler(this.saveAsToolStripMenuItem_Click);
			//
			// toolStripSeparator2
			//
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(196, 6);
			//
			// compileToolStripMenuItem
			//
			this.compileToolStripMenuItem.Name = "compileToolStripMenuItem";
			this.compileToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.K)));
			this.compileToolStripMenuItem.Size = new System.Drawing.Size(199, 22);
			this.compileToolStripMenuItem.Text = "Com&pile";
			this.compileToolStripMenuItem.ToolTipText = "Compile this map file";
			this.compileToolStripMenuItem.Click += new System.EventHandler(this.compileToolStripMenuItem_Click);
			//
			// autoCompileToolStripMenuItem
			//
			this.autoCompileToolStripMenuItem.Checked = true;
			this.autoCompileToolStripMenuItem.CheckOnClick = true;
			this.autoCompileToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
			this.autoCompileToolStripMenuItem.Name = "autoCompileToolStripMenuItem";
			this.autoCompileToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F5;
			this.autoCompileToolStripMenuItem.Size = new System.Drawing.Size(199, 22);
			this.autoCompileToolStripMenuItem.Text = "A&uto-Compile";
			this.autoCompileToolStripMenuItem.ToolTipText = "Automatically compile the file after changes (using a temporary .Tec filename)";
			this.autoCompileToolStripMenuItem.Click += new System.EventHandler(this.autoCompileToolStripMenuItem_Click);
			//
			// toolStripSeparator6
			//
			this.toolStripSeparator6.Name = "toolStripSeparator6";
			this.toolStripSeparator6.Size = new System.Drawing.Size(196, 6);
			//
			// addToSystemRepositoryToolStripMenuItem
			//
			this.addToSystemRepositoryToolStripMenuItem.Name = "addToSystemRepositoryToolStripMenuItem";
			this.addToSystemRepositoryToolStripMenuItem.Size = new System.Drawing.Size(199, 22);
			this.addToSystemRepositoryToolStripMenuItem.Text = "Add to System &Repository";
			this.addToSystemRepositoryToolStripMenuItem.ToolTipText = "Add this mapping to the system repository";
			this.addToSystemRepositoryToolStripMenuItem.Click += new System.EventHandler(this.addToSystemRepositoryToolStripMenuItem_Click);
			//
			// toolStripSeparator7
			//
			this.toolStripSeparator7.Name = "toolStripSeparator7";
			this.toolStripSeparator7.Size = new System.Drawing.Size(196, 6);
			//
			// exitToolStripMenuItem
			//
			this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
			this.exitToolStripMenuItem.Size = new System.Drawing.Size(199, 22);
			this.exitToolStripMenuItem.Text = "Exit";
			this.exitToolStripMenuItem.ToolTipText = "Exit the application";
			this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
			//
			// editToolStripMenuItem
			//
			this.editToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.undoToolStripMenuItem,
			this.toolStripSeparator4,
			this.cutToolStripMenuItem,
			this.copyToolStripMenuItem,
			this.clearToolStripMenuItem,
			this.toolStripSeparator5,
			this.findToolStripMenuItem,
			this.findNextToolStripMenuItem,
			this.replaceToolStripMenuItem,
			this.toolStripSeparator9,
			this.toggleCodePointToolStripMenuItem});
			this.editToolStripMenuItem.Name = "editToolStripMenuItem";
			this.editToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
			this.editToolStripMenuItem.Text = "&Edit";
			//
			// undoToolStripMenuItem
			//
			this.undoToolStripMenuItem.Name = "undoToolStripMenuItem";
			this.undoToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Z)));
			this.undoToolStripMenuItem.Size = new System.Drawing.Size(262, 22);
			this.undoToolStripMenuItem.Text = "&Undo";
			this.undoToolStripMenuItem.ToolTipText = "Undo last map change";
			this.undoToolStripMenuItem.Click += new System.EventHandler(this.undoToolStripMenuItem_Click);
			//
			// toolStripSeparator4
			//
			this.toolStripSeparator4.Name = "toolStripSeparator4";
			this.toolStripSeparator4.Size = new System.Drawing.Size(259, 6);
			//
			// cutToolStripMenuItem
			//
			this.cutToolStripMenuItem.Name = "cutToolStripMenuItem";
			this.cutToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.X)));
			this.cutToolStripMenuItem.Size = new System.Drawing.Size(262, 22);
			this.cutToolStripMenuItem.Text = "Cut";
			this.cutToolStripMenuItem.ToolTipText = "Cut selected text";
			this.cutToolStripMenuItem.Click += new System.EventHandler(this.cutToolStripMenuItem_Click);
			//
			// copyToolStripMenuItem
			//
			this.copyToolStripMenuItem.Name = "copyToolStripMenuItem";
			this.copyToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
			this.copyToolStripMenuItem.Size = new System.Drawing.Size(262, 22);
			this.copyToolStripMenuItem.Text = "Copy";
			this.copyToolStripMenuItem.ToolTipText = "Copy selected text to Clipboard";
			this.copyToolStripMenuItem.Click += new System.EventHandler(this.copyToolStripMenuItem_Click);
			//
			// clearToolStripMenuItem
			//
			this.clearToolStripMenuItem.Name = "clearToolStripMenuItem";
			this.clearToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.A)));
			this.clearToolStripMenuItem.Size = new System.Drawing.Size(262, 22);
			this.clearToolStripMenuItem.Text = "Select &All";
			this.clearToolStripMenuItem.ToolTipText = "Select all map file text";
			this.clearToolStripMenuItem.Click += new System.EventHandler(this.clearToolStripMenuItem_Click);
			//
			// toolStripSeparator5
			//
			this.toolStripSeparator5.Name = "toolStripSeparator5";
			this.toolStripSeparator5.Size = new System.Drawing.Size(259, 6);
			//
			// findToolStripMenuItem
			//
			this.findToolStripMenuItem.Name = "findToolStripMenuItem";
			this.findToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.F)));
			this.findToolStripMenuItem.Size = new System.Drawing.Size(262, 22);
			this.findToolStripMenuItem.Text = "&Find...";
			this.findToolStripMenuItem.ToolTipText = "Bring up Find/Replace dialog";
			this.findToolStripMenuItem.Click += new System.EventHandler(this.findToolStripMenuItem_Click);
			//
			// findNextToolStripMenuItem
			//
			this.findNextToolStripMenuItem.Name = "findNextToolStripMenuItem";
			this.findNextToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F3;
			this.findNextToolStripMenuItem.Size = new System.Drawing.Size(262, 22);
			this.findNextToolStripMenuItem.Text = "Find &Next";
			this.findNextToolStripMenuItem.Click += new System.EventHandler(this.findNextToolStripMenuItem_Click);
			//
			// replaceToolStripMenuItem
			//
			this.replaceToolStripMenuItem.Name = "replaceToolStripMenuItem";
			this.replaceToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.H)));
			this.replaceToolStripMenuItem.Size = new System.Drawing.Size(262, 22);
			this.replaceToolStripMenuItem.Text = "&Replace...";
			this.replaceToolStripMenuItem.ToolTipText = "Bring up Find/Replace dialog";
			this.replaceToolStripMenuItem.Click += new System.EventHandler(this.findToolStripMenuItem_Click);
			//
			// toolStripSeparator9
			//
			this.toolStripSeparator9.Name = "toolStripSeparator9";
			this.toolStripSeparator9.Size = new System.Drawing.Size(259, 6);
			//
			// toggleCodePointToolStripMenuItem
			//
			this.toggleCodePointToolStripMenuItem.Name = "toggleCodePointToolStripMenuItem";
			this.toggleCodePointToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.X)));
			this.toggleCodePointToolStripMenuItem.Size = new System.Drawing.Size(262, 22);
			this.toggleCodePointToolStripMenuItem.Text = "&Convert code point to character";
			this.toggleCodePointToolStripMenuItem.ToolTipText = "Convert the preceding 3-4 digits in the Left or Right-side Sample boxes to corres" +
				"ponding character value (e.g. \'65\' or \'0x41\' or \'0041\' becomes \'A\')";
			this.toggleCodePointToolStripMenuItem.Click += new System.EventHandler(this.toggleCodePointToolStripMenuItem_Click);
			//
			// viewToolStripMenuItem
			//
			this.viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.unicodeValuesWindowToolStripMenuItem,
			this.toolStripSeparator3,
			this.setSampleDataFontToolStripMenuItem,
			this.setConvertedDataFontToolStripMenuItem});
			this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
			this.viewToolStripMenuItem.Size = new System.Drawing.Size(41, 20);
			this.viewToolStripMenuItem.Text = "&View";
			this.viewToolStripMenuItem.DropDownOpening += new System.EventHandler(this.viewToolStripMenuItem_DropDownOpening);
			//
			// unicodeValuesWindowToolStripMenuItem
			//
			this.unicodeValuesWindowToolStripMenuItem.Checked = true;
			this.unicodeValuesWindowToolStripMenuItem.CheckOnClick = true;
			this.unicodeValuesWindowToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
			this.unicodeValuesWindowToolStripMenuItem.Name = "unicodeValuesWindowToolStripMenuItem";
			this.unicodeValuesWindowToolStripMenuItem.Size = new System.Drawing.Size(234, 22);
			this.unicodeValuesWindowToolStripMenuItem.Text = "&Display code point values window";
			this.unicodeValuesWindowToolStripMenuItem.ToolTipText = "Show or Disable the Code Point Values Window";
			this.unicodeValuesWindowToolStripMenuItem.Click += new System.EventHandler(this.unicodeValuesWindowToolStripMenuItem_Click);
			//
			// toolStripSeparator3
			//
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			this.toolStripSeparator3.Size = new System.Drawing.Size(231, 6);
			//
			// setSampleDataFontToolStripMenuItem
			//
			this.setSampleDataFontToolStripMenuItem.Name = "setSampleDataFontToolStripMenuItem";
			this.setSampleDataFontToolStripMenuItem.Size = new System.Drawing.Size(234, 22);
			this.setSampleDataFontToolStripMenuItem.Text = "Configure &Left-side font";
			this.setSampleDataFontToolStripMenuItem.ToolTipText = "Click to configure the font to be used to display the left-hand side encoding";
			this.setSampleDataFontToolStripMenuItem.Click += new System.EventHandler(this.setSampleDataFontToolStripMenuItem_Click);
			//
			// setConvertedDataFontToolStripMenuItem
			//
			this.setConvertedDataFontToolStripMenuItem.Name = "setConvertedDataFontToolStripMenuItem";
			this.setConvertedDataFontToolStripMenuItem.Size = new System.Drawing.Size(234, 22);
			this.setConvertedDataFontToolStripMenuItem.Text = "Configure &Right-side font";
			this.setConvertedDataFontToolStripMenuItem.ToolTipText = "Click to configure the font to be used to display the right-hand side encoding";
			this.setConvertedDataFontToolStripMenuItem.Click += new System.EventHandler(this.setConvertedDataFontToolStripMenuItem_Click);
			//
			// helpToolStripMenuItem
			//
			this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.openTECkitDocToolStripMenuItem});
			this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
			this.helpToolStripMenuItem.Size = new System.Drawing.Size(40, 20);
			this.helpToolStripMenuItem.Text = "&Help";
			//
			// openTECkitDocToolStripMenuItem
			//
			this.openTECkitDocToolStripMenuItem.Name = "openTECkitDocToolStripMenuItem";
			this.openTECkitDocToolStripMenuItem.Size = new System.Drawing.Size(207, 22);
			this.openTECkitDocToolStripMenuItem.Text = "&Open TECkit documentation";
			this.openTECkitDocToolStripMenuItem.ToolTipText = "Open the TECkit map syntax document";
			this.openTECkitDocToolStripMenuItem.Click += new System.EventHandler(this.openHelpDocumentToolStripMenuItem_Click);
			//
			// tableLayoutPanel
			//
			this.tableLayoutPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.tableLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.tableLayoutPanel.ColumnCount = 1;
			this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel.Controls.Add(this.splitContainer1, 0, 0);
			this.tableLayoutPanel.Controls.Add(this.tableLayoutPanelSamples, 0, 1);
			this.tableLayoutPanel.Controls.Add(this.dataGridViewCodePointValues, 0, 2);
			this.tableLayoutPanel.Location = new System.Drawing.Point(0, 24);
			this.tableLayoutPanel.Name = "tableLayoutPanel";
			this.tableLayoutPanel.RowCount = 3;
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 75F));
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
			this.tableLayoutPanel.Size = new System.Drawing.Size(642, 625);
			this.tableLayoutPanel.TabIndex = 3;
			//
			// tableLayoutPanelSamples
			//
			this.tableLayoutPanelSamples.AutoSize = true;
			this.tableLayoutPanelSamples.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.tableLayoutPanelSamples.ColumnCount = 2;
			this.tableLayoutPanelSamples.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanelSamples.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanelSamples.Controls.Add(this.labelSampleLhs, 0, 0);
			this.tableLayoutPanelSamples.Controls.Add(this.textBoxSampleReverse, 1, 2);
			this.tableLayoutPanelSamples.Controls.Add(this.labelRhsSample, 0, 1);
			this.tableLayoutPanelSamples.Controls.Add(this.labelRoundtrip, 0, 2);
			this.tableLayoutPanelSamples.Controls.Add(this.textBoxSample, 1, 0);
			this.tableLayoutPanelSamples.Controls.Add(this.textBoxSampleForward, 1, 1);
			this.tableLayoutPanelSamples.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanelSamples.Location = new System.Drawing.Point(3, 390);
			this.tableLayoutPanelSamples.Name = "tableLayoutPanelSamples";
			this.tableLayoutPanelSamples.RowCount = 3;
			this.tableLayoutPanelSamples.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanelSamples.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanelSamples.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanelSamples.Size = new System.Drawing.Size(636, 102);
			this.tableLayoutPanelSamples.TabIndex = 0;
			//
			// labelSampleLhs
			//
			this.labelSampleLhs.Anchor = System.Windows.Forms.AnchorStyles.Right;
			this.labelSampleLhs.AutoSize = true;
			this.labelSampleLhs.Location = new System.Drawing.Point(10, 10);
			this.labelSampleLhs.Name = "labelSampleLhs";
			this.labelSampleLhs.Size = new System.Drawing.Size(88, 13);
			this.labelSampleLhs.TabIndex = 0;
			this.labelSampleLhs.Text = "&Left-side Sample:";
			this.toolTip.SetToolTip(this.labelSampleLhs, "Enter a sample of data for the left-hand side of the conversion to see the code p" +
					"oints visible in the Code Points pane (you can also double-click this lable to s" +
					"et the font)");
			this.labelSampleLhs.DoubleClick += new System.EventHandler(this.labelSampleLhs_DoubleClick);
			//
			// textBoxSampleReverse
			//
			this.textBoxSampleReverse.Dock = System.Windows.Forms.DockStyle.Fill;
			this.textBoxSampleReverse.Font = new System.Drawing.Font("Arial Unicode MS", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.textBoxSampleReverse.Location = new System.Drawing.Point(104, 71);
			this.textBoxSampleReverse.Name = "textBoxSampleReverse";
			this.textBoxSampleReverse.ReadOnly = true;
			this.textBoxSampleReverse.Size = new System.Drawing.Size(529, 28);
			this.textBoxSampleReverse.TabIndex = 5;
			this.toolTip.SetToolTip(this.textBoxSampleReverse, "This window shows the round-trip conversion");
			this.textBoxSampleReverse.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.textBox_PreviewKeyDown);
			this.textBoxSampleReverse.GotFocus += new System.EventHandler(this.textBoxSampleReverse_UpdateUnicodeChars);
			this.textBoxSampleReverse.MouseUp += new System.Windows.Forms.MouseEventHandler(this.textBoxSampleReverse_UpdateUnicodeChars);
			//
			// labelRhsSample
			//
			this.labelRhsSample.Anchor = System.Windows.Forms.AnchorStyles.Right;
			this.labelRhsSample.AutoSize = true;
			this.labelRhsSample.Location = new System.Drawing.Point(3, 44);
			this.labelRhsSample.Name = "labelRhsSample";
			this.labelRhsSample.Size = new System.Drawing.Size(95, 13);
			this.labelRhsSample.TabIndex = 2;
			this.labelRhsSample.Text = "&Right-side Sample:";
			this.toolTip.SetToolTip(this.labelRhsSample, "Enter a sample of data for the right-hand side of the conversion to see the code " +
					"points visible in the Code Points pane (you can also double-click this lable to " +
					"set the font)");
			this.labelRhsSample.DoubleClick += new System.EventHandler(this.labelRhsSample_DoubleClick);
			//
			// labelRoundtrip
			//
			this.labelRoundtrip.Anchor = System.Windows.Forms.AnchorStyles.Right;
			this.labelRoundtrip.AutoSize = true;
			this.labelRoundtrip.Location = new System.Drawing.Point(39, 78);
			this.labelRoundtrip.Name = "labelRoundtrip";
			this.labelRoundtrip.Size = new System.Drawing.Size(59, 13);
			this.labelRoundtrip.TabIndex = 4;
			this.labelRoundtrip.Text = "R&ound-trip:";
			this.toolTip.SetToolTip(this.labelRoundtrip, "This pane shows the result of a round-trip conversion");
			//
			// textBoxSample
			//
			this.textBoxSample.AllowDrop = true;
			this.textBoxSample.Dock = System.Windows.Forms.DockStyle.Fill;
			this.textBoxSample.Font = new System.Drawing.Font("Arial Unicode MS", 11.25F);
			this.textBoxSample.Location = new System.Drawing.Point(104, 3);
			this.textBoxSample.Name = "textBoxSample";
			this.textBoxSample.Size = new System.Drawing.Size(529, 28);
			this.textBoxSample.TabIndex = 1;
			this.toolTip.SetToolTip(this.textBoxSample, "Enter some sample data here (you can use Alt+X after a numeric code point value t" +
					"o convert the number to the corresponding character)");
			this.textBoxSample.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.textBox_PreviewKeyDown);
			this.textBoxSample.GotFocus += new System.EventHandler(this.textBoxSample_UpdateUnicodeValues);
			this.textBoxSample.MouseUp += new System.Windows.Forms.MouseEventHandler(this.textBoxSample_UpdateUnicodeValues);
			this.textBoxSample.TextChanged += new System.EventHandler(this.textBoxSample_TextChanged);
			//
			// textBoxSampleForward
			//
			this.textBoxSampleForward.AllowDrop = true;
			this.textBoxSampleForward.Dock = System.Windows.Forms.DockStyle.Fill;
			this.textBoxSampleForward.Font = new System.Drawing.Font("Arial Unicode MS", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.textBoxSampleForward.Location = new System.Drawing.Point(104, 37);
			this.textBoxSampleForward.Name = "textBoxSampleForward";
			this.textBoxSampleForward.Size = new System.Drawing.Size(529, 28);
			this.textBoxSampleForward.TabIndex = 3;
			this.toolTip.SetToolTip(this.textBoxSampleForward, "This window shows the sample after conversion in the forward direction");
			this.textBoxSampleForward.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.textBox_PreviewKeyDown);
			this.textBoxSampleForward.GotFocus += new System.EventHandler(this.textBoxSampleForward_UpdateUnicodeValues);
			this.textBoxSampleForward.MouseUp += new System.Windows.Forms.MouseEventHandler(this.textBoxSampleForward_UpdateUnicodeValues);
			this.textBoxSampleForward.TextChanged += new System.EventHandler(this.textBoxSampleForward_TextChanged);
			//
			// dataGridViewCodePointValues
			//
			this.dataGridViewCodePointValues.AllowUserToAddRows = false;
			this.dataGridViewCodePointValues.AllowUserToDeleteRows = false;
			this.dataGridViewCodePointValues.AllowUserToOrderColumns = true;
			this.dataGridViewCodePointValues.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.dataGridViewCodePointValues.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.dataGridViewCodePointValues.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
			this.dataGridViewTextBoxColumn1,
			this.dataGridViewTextBoxColumn2,
			this.dataGridViewTextBoxColumn3,
			this.dataGridViewTextBoxColumn4,
			this.dataGridViewTextBoxColumn5});
			this.dataGridViewCodePointValues.Cursor = System.Windows.Forms.Cursors.Hand;
			this.dataGridViewCodePointValues.Dock = System.Windows.Forms.DockStyle.Fill;
			this.dataGridViewCodePointValues.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
			this.dataGridViewCodePointValues.Location = new System.Drawing.Point(3, 498);
			this.dataGridViewCodePointValues.MinimumSize = new System.Drawing.Size(370, 100);
			this.dataGridViewCodePointValues.Name = "dataGridViewCodePointValues";
			this.dataGridViewCodePointValues.ReadOnly = true;
			this.dataGridViewCodePointValues.RowHeadersVisible = false;
			this.dataGridViewCodePointValues.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
			this.dataGridViewCodePointValues.Size = new System.Drawing.Size(636, 124);
			this.dataGridViewCodePointValues.TabIndex = 5;
			this.dataGridViewCodePointValues.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridViewCodePointValues_CellClick);
			//
			// dataGridViewTextBoxColumn1
			//
			this.dataGridViewTextBoxColumn1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCellsExceptHeader;
			this.dataGridViewTextBoxColumn1.HeaderText = "Hex";
			this.dataGridViewTextBoxColumn1.MinimumWidth = 40;
			this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
			this.dataGridViewTextBoxColumn1.ReadOnly = true;
			this.dataGridViewTextBoxColumn1.Width = 40;
			//
			// dataGridViewTextBoxColumn2
			//
			this.dataGridViewTextBoxColumn2.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCellsExceptHeader;
			this.dataGridViewTextBoxColumn2.HeaderText = "Dec";
			this.dataGridViewTextBoxColumn2.MinimumWidth = 40;
			this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
			this.dataGridViewTextBoxColumn2.ReadOnly = true;
			this.dataGridViewTextBoxColumn2.Width = 40;
			//
			// dataGridViewTextBoxColumn3
			//
			this.dataGridViewTextBoxColumn3.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCellsExceptHeader;
			this.dataGridViewTextBoxColumn3.HeaderText = "Unicode Name";
			this.dataGridViewTextBoxColumn3.MinimumWidth = 120;
			this.dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
			this.dataGridViewTextBoxColumn3.ReadOnly = true;
			this.dataGridViewTextBoxColumn3.Width = 120;
			//
			// dataGridViewTextBoxColumn4
			//
			this.dataGridViewTextBoxColumn4.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCellsExceptHeader;
			this.dataGridViewTextBoxColumn4.HeaderText = "U Value";
			this.dataGridViewTextBoxColumn4.MinimumWidth = 68;
			this.dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
			this.dataGridViewTextBoxColumn4.ReadOnly = true;
			this.dataGridViewTextBoxColumn4.Width = 68;
			//
			// dataGridViewTextBoxColumn5
			//
			this.dataGridViewTextBoxColumn5.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCellsExceptHeader;
			this.dataGridViewTextBoxColumn5.HeaderText = "Chars";
			this.dataGridViewTextBoxColumn5.MinimumWidth = 50;
			this.dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
			this.dataGridViewTextBoxColumn5.ReadOnly = true;
			this.dataGridViewTextBoxColumn5.Width = 50;
			//
			// toolTip
			//
			this.toolTip.AutoPopDelay = 20000;
			this.toolTip.InitialDelay = 100;
			this.toolTip.ReshowDelay = 100;
			this.toolTip.UseAnimation = false;
			this.toolTip.UseFading = false;
			//
			// fontDialog
			//
			this.fontDialog.Font = new System.Drawing.Font("Arial Unicode MS", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.fontDialog.FontMustExist = true;
			this.fontDialog.ShowColor = true;
			this.fontDialog.ShowEffects = false;
			this.fontDialog.ShowHelp = true;
			//
			// saveFileDialog
			//
			this.saveFileDialog.DefaultExt = "map";
			this.saveFileDialog.FileName = "Untitled";
			this.saveFileDialog.Filter = "TECkit map files (*.map)|*.map";
			this.saveFileDialog.Title = "Save TECkit map file";
			//
			// openFileDialog
			//
			this.openFileDialog.Filter = "TECkit map files (*.map)|*.map";
			this.openFileDialog.Title = "Open TECkit map file";
			//
			// statusStrip
			//
			this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.toolStripStatusLabel});
			this.statusStrip.Location = new System.Drawing.Point(0, 652);
			this.statusStrip.Name = "statusStrip";
			this.statusStrip.Size = new System.Drawing.Size(642, 22);
			this.statusStrip.TabIndex = 4;
			//
			// toolStripStatusLabel
			//
			this.toolStripStatusLabel.Name = "toolStripStatusLabel";
			this.toolStripStatusLabel.Size = new System.Drawing.Size(0, 17);
			//
			// TECkitMapEditorForm
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(642, 674);
			this.Controls.Add(this.statusStrip);
			this.Controls.Add(this.tableLayoutPanel);
			this.Controls.Add(this.menuStrip);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "TECkitMapEditorForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
			this.Text = "TECkit Mapping Editor";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.TECkitMapEditorForm_FormClosing);
			this.ResizeEnd += new System.EventHandler(this.TECkitMapEditorForm_ResizeEnd);
			this.splitContainer1.Panel1.ResumeLayout(false);
			this.splitContainer1.Panel1.PerformLayout();
			this.splitContainer1.Panel2.ResumeLayout(false);
			this.splitContainer1.ResumeLayout(false);
			this.menuStrip.ResumeLayout(false);
			this.menuStrip.PerformLayout();
			this.tableLayoutPanel.ResumeLayout(false);
			this.tableLayoutPanel.PerformLayout();
			this.tableLayoutPanelSamples.ResumeLayout(false);
			this.tableLayoutPanelSamples.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.dataGridViewCodePointValues)).EndInit();
			this.statusStrip.ResumeLayout(false);
			this.statusStrip.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.SplitContainer splitContainer1;
		private System.Windows.Forms.TextBox textBoxCompilerResults;
		internal System.Windows.Forms.RichTextBox richTextBoxMapEditor;
		private System.Windows.Forms.MenuStrip menuStrip;
		private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem newToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripMenuItem compileToolStripMenuItem;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
		private System.Windows.Forms.TextBox textBoxSample;
		private System.Windows.Forms.ToolTip toolTip;
		private System.Windows.Forms.TextBox textBoxSampleForward;
		private System.Windows.Forms.TextBox textBoxSampleReverse;
		private System.Windows.Forms.ToolStripMenuItem viewToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem unicodeValuesWindowToolStripMenuItem;
		private System.Windows.Forms.FontDialog fontDialog;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
		private System.Windows.Forms.ToolStripMenuItem setSampleDataFontToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem setConvertedDataFontToolStripMenuItem;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanelSamples;
		private System.Windows.Forms.Label labelSampleLhs;
		private System.Windows.Forms.Label labelRhsSample;
		private System.Windows.Forms.Label labelRoundtrip;
		private System.Windows.Forms.ToolStripMenuItem autoCompileToolStripMenuItem;
		private System.Windows.Forms.SaveFileDialog saveFileDialog;
		private System.Windows.Forms.OpenFileDialog openFileDialog;
		private System.Windows.Forms.ToolStripMenuItem saveAsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem undoToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
		private System.Windows.Forms.ToolStripMenuItem cutToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem copyToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem clearToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
		private System.Windows.Forms.ToolStripMenuItem findToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem openTECkitDocToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator6;
		private System.Windows.Forms.ToolStripMenuItem addToSystemRepositoryToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator7;
		private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator8;
		private System.Windows.Forms.ToolStripMenuItem recentFilesToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem replaceToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator9;
		private System.Windows.Forms.ToolStripMenuItem toggleCodePointToolStripMenuItem;
		private System.Windows.Forms.StatusStrip statusStrip;
		private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel;
		private System.Windows.Forms.ToolStripMenuItem findNextToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem revertTolastSavedCopyToolStripMenuItem;
		private System.Windows.Forms.HelpProvider helpProvider;
		private System.Windows.Forms.DataGridView dataGridViewCodePointValues;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn4;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn5;

	}
}

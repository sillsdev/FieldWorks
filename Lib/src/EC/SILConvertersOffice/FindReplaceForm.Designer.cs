namespace SILConvertersOffice
{
	partial class FindReplaceForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FindReplaceForm));
			this.buttonReplaceAll = new System.Windows.Forms.Button();
			this.buttonReplace = new System.Windows.Forms.Button();
			this.buttonFindNext = new System.Windows.Forms.Button();
			this.checkBoxMatchCase = new System.Windows.Forms.CheckBox();
			this.comboBoxReplaceWith = new System.Windows.Forms.ComboBox();
			this.labelReplaceWith = new System.Windows.Forms.Label();
			this.comboBoxFindWhat = new System.Windows.Forms.ComboBox();
			this.labelFindWhat = new System.Windows.Forms.Label();
			this.buttonExpressionBuilder = new System.Windows.Forms.Button();
			this.contextMenuStripExprBuilder = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.MatchAnyCharacterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.match0OrMoreTimesAsManyAsPossibleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.match0Or1TimesButPreferOneTimeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.match0OrMoreTimesAsFewAsPossibleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.match1OrMoreTimesAsManyAsPossibleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.match1OrMoreTimesAsFewAsPossibleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.nMatchExactlyNTimesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.nmMatchBetweenNAndMTimesAsManyAsPossibleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.rParagraphCharacterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.matchAtTheBeginningOfALineToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.matchAtTheEndOfALineToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.bMatchIfTheCurrentPositionIsAWordBoundaryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.bMatchIfTheCurrentPositionIsNotAWordBoundaryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.dMatchAnyNumberOrDecimalDigitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.dMatchAnyCharacterThatIsNotADecimalDigitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.sMatchAWhiteSpaceCharacterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.sMatchANonwhiteSpaceCharacterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.patternMatchAnyOneCharacterFromTheSetToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.aBMatchesEitherAOrBToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.uhhhhMatchTheCharacterWithTheHexValueHhhhToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.regularExpressionHelpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.ecTextBoxFindWhat = new SilEncConverters31.EcTextBox();
			this.ecTextBoxReplaceWith = new SilEncConverters31.EcTextBox();
			this.helpProvider = new System.Windows.Forms.HelpProvider();
			this.progressBar = new System.Windows.Forms.ProgressBar();
			this.backgroundWorker = new System.ComponentModel.BackgroundWorker();
			this.contextMenuStripExprBuilder.SuspendLayout();
			this.SuspendLayout();
			//
			// buttonReplaceAll
			//
			this.buttonReplaceAll.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.buttonReplaceAll.Location = new System.Drawing.Point(256, 96);
			this.buttonReplaceAll.Name = "buttonReplaceAll";
			this.buttonReplaceAll.Size = new System.Drawing.Size(75, 23);
			this.buttonReplaceAll.TabIndex = 7;
			this.buttonReplaceAll.Text = "Replace &All";
			this.buttonReplaceAll.UseVisualStyleBackColor = true;
			this.buttonReplaceAll.Click += new System.EventHandler(this.buttonReplaceAll_Click);
			//
			// buttonReplace
			//
			this.buttonReplace.Location = new System.Drawing.Point(175, 96);
			this.buttonReplace.Name = "buttonReplace";
			this.buttonReplace.Size = new System.Drawing.Size(75, 23);
			this.buttonReplace.TabIndex = 6;
			this.buttonReplace.Text = "&Replace";
			this.buttonReplace.UseVisualStyleBackColor = true;
			this.buttonReplace.Click += new System.EventHandler(this.buttonReplace_Click);
			//
			// buttonFindNext
			//
			this.buttonFindNext.Location = new System.Drawing.Point(94, 96);
			this.buttonFindNext.Name = "buttonFindNext";
			this.buttonFindNext.Size = new System.Drawing.Size(75, 23);
			this.buttonFindNext.TabIndex = 5;
			this.buttonFindNext.Text = "&Find Next";
			this.buttonFindNext.UseVisualStyleBackColor = true;
			this.buttonFindNext.Click += new System.EventHandler(this.buttonFindNext_Click);
			//
			// checkBoxMatchCase
			//
			this.checkBoxMatchCase.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.checkBoxMatchCase.AutoSize = true;
			this.checkBoxMatchCase.Location = new System.Drawing.Point(438, 100);
			this.checkBoxMatchCase.Name = "checkBoxMatchCase";
			this.checkBoxMatchCase.Size = new System.Drawing.Size(82, 17);
			this.checkBoxMatchCase.TabIndex = 8;
			this.checkBoxMatchCase.Text = "Matc&h case";
			this.checkBoxMatchCase.UseVisualStyleBackColor = true;
			this.checkBoxMatchCase.CheckedChanged += new System.EventHandler(this.checkBoxMatchCase_CheckedChanged);
			//
			// comboBoxReplaceWith
			//
			this.comboBoxReplaceWith.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.comboBoxReplaceWith.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxReplaceWith.Font = new System.Drawing.Font("Arial Unicode MS", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.comboBoxReplaceWith.FormattingEnabled = true;
			this.comboBoxReplaceWith.Location = new System.Drawing.Point(93, 47);
			this.comboBoxReplaceWith.Name = "comboBoxReplaceWith";
			this.comboBoxReplaceWith.Size = new System.Drawing.Size(427, 29);
			this.comboBoxReplaceWith.TabIndex = 10;
			this.comboBoxReplaceWith.SelectedIndexChanged += new System.EventHandler(this.comboBoxReplaceWith_SelectedIndexChanged);
			//
			// labelReplaceWith
			//
			this.labelReplaceWith.AutoSize = true;
			this.labelReplaceWith.Location = new System.Drawing.Point(15, 56);
			this.labelReplaceWith.Name = "labelReplaceWith";
			this.labelReplaceWith.Size = new System.Drawing.Size(72, 13);
			this.labelReplaceWith.TabIndex = 2;
			this.labelReplaceWith.Text = "Replace w&ith:";
			//
			// comboBoxFindWhat
			//
			this.comboBoxFindWhat.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.comboBoxFindWhat.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxFindWhat.Font = new System.Drawing.Font("Arial Unicode MS", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.comboBoxFindWhat.FormattingEnabled = true;
			this.comboBoxFindWhat.Location = new System.Drawing.Point(93, 12);
			this.comboBoxFindWhat.Name = "comboBoxFindWhat";
			this.comboBoxFindWhat.Size = new System.Drawing.Size(427, 29);
			this.comboBoxFindWhat.TabIndex = 9;
			this.comboBoxFindWhat.SelectedIndexChanged += new System.EventHandler(this.comboBoxFindWhat_SelectedIndexChanged);
			//
			// labelFindWhat
			//
			this.labelFindWhat.AutoSize = true;
			this.labelFindWhat.Location = new System.Drawing.Point(28, 17);
			this.labelFindWhat.Name = "labelFindWhat";
			this.labelFindWhat.Size = new System.Drawing.Size(56, 13);
			this.labelFindWhat.TabIndex = 0;
			this.labelFindWhat.Text = "Fi&nd what:";
			//
			// buttonExpressionBuilder
			//
			this.buttonExpressionBuilder.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonExpressionBuilder.ContextMenuStrip = this.contextMenuStripExprBuilder;
			this.buttonExpressionBuilder.Location = new System.Drawing.Point(525, 12);
			this.buttonExpressionBuilder.Name = "buttonExpressionBuilder";
			this.buttonExpressionBuilder.Size = new System.Drawing.Size(23, 23);
			this.buttonExpressionBuilder.TabIndex = 4;
			this.buttonExpressionBuilder.Text = ">";
			this.buttonExpressionBuilder.UseVisualStyleBackColor = true;
			this.buttonExpressionBuilder.Click += new System.EventHandler(this.buttonExpressionBuilder_Click);
			//
			// contextMenuStripExprBuilder
			//
			this.contextMenuStripExprBuilder.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.MatchAnyCharacterToolStripMenuItem,
			this.match0OrMoreTimesAsManyAsPossibleToolStripMenuItem,
			this.match0Or1TimesButPreferOneTimeToolStripMenuItem,
			this.match0OrMoreTimesAsFewAsPossibleToolStripMenuItem,
			this.match1OrMoreTimesAsManyAsPossibleToolStripMenuItem,
			this.match1OrMoreTimesAsFewAsPossibleToolStripMenuItem,
			this.nMatchExactlyNTimesToolStripMenuItem,
			this.nmMatchBetweenNAndMTimesAsManyAsPossibleToolStripMenuItem,
			this.toolStripSeparator1,
			this.rParagraphCharacterToolStripMenuItem,
			this.matchAtTheBeginningOfALineToolStripMenuItem,
			this.matchAtTheEndOfALineToolStripMenuItem,
			this.bMatchIfTheCurrentPositionIsAWordBoundaryToolStripMenuItem,
			this.bMatchIfTheCurrentPositionIsNotAWordBoundaryToolStripMenuItem,
			this.dMatchAnyNumberOrDecimalDigitToolStripMenuItem,
			this.dMatchAnyCharacterThatIsNotADecimalDigitToolStripMenuItem,
			this.sMatchAWhiteSpaceCharacterToolStripMenuItem,
			this.sMatchANonwhiteSpaceCharacterToolStripMenuItem,
			this.toolStripSeparator2,
			this.patternMatchAnyOneCharacterFromTheSetToolStripMenuItem,
			this.aBMatchesEitherAOrBToolStripMenuItem,
			this.uhhhhMatchTheCharacterWithTheHexValueHhhhToolStripMenuItem,
			this.toolStripSeparator3,
			this.regularExpressionHelpToolStripMenuItem});
			this.contextMenuStripExprBuilder.Name = "contextMenuStripExprBuilder";
			this.contextMenuStripExprBuilder.Size = new System.Drawing.Size(351, 484);
			this.contextMenuStripExprBuilder.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.contextMenuStripExprBuilder_ItemClicked);
			//
			// MatchAnyCharacterToolStripMenuItem
			//
			this.MatchAnyCharacterToolStripMenuItem.Name = "MatchAnyCharacterToolStripMenuItem";
			this.MatchAnyCharacterToolStripMenuItem.Size = new System.Drawing.Size(350, 22);
			this.MatchAnyCharacterToolStripMenuItem.Text = ". Match any single character";
			//
			// match0OrMoreTimesAsManyAsPossibleToolStripMenuItem
			//
			this.match0OrMoreTimesAsManyAsPossibleToolStripMenuItem.Name = "match0OrMoreTimesAsManyAsPossibleToolStripMenuItem";
			this.match0OrMoreTimesAsManyAsPossibleToolStripMenuItem.Size = new System.Drawing.Size(350, 22);
			this.match0OrMoreTimesAsManyAsPossibleToolStripMenuItem.Text = "* Match 0 or more times, as many as possible";
			//
			// match0Or1TimesButPreferOneTimeToolStripMenuItem
			//
			this.match0Or1TimesButPreferOneTimeToolStripMenuItem.Name = "match0Or1TimesButPreferOneTimeToolStripMenuItem";
			this.match0Or1TimesButPreferOneTimeToolStripMenuItem.Size = new System.Drawing.Size(350, 22);
			this.match0Or1TimesButPreferOneTimeToolStripMenuItem.Text = "? Match 0 or 1 times, but prefer one time";
			//
			// match0OrMoreTimesAsFewAsPossibleToolStripMenuItem
			//
			this.match0OrMoreTimesAsFewAsPossibleToolStripMenuItem.Name = "match0OrMoreTimesAsFewAsPossibleToolStripMenuItem";
			this.match0OrMoreTimesAsFewAsPossibleToolStripMenuItem.Size = new System.Drawing.Size(350, 22);
			this.match0OrMoreTimesAsFewAsPossibleToolStripMenuItem.Text = "*? Match 0 or more times, as few as possible";
			//
			// match1OrMoreTimesAsManyAsPossibleToolStripMenuItem
			//
			this.match1OrMoreTimesAsManyAsPossibleToolStripMenuItem.Name = "match1OrMoreTimesAsManyAsPossibleToolStripMenuItem";
			this.match1OrMoreTimesAsManyAsPossibleToolStripMenuItem.Size = new System.Drawing.Size(350, 22);
			this.match1OrMoreTimesAsManyAsPossibleToolStripMenuItem.Text = "+ Match 1 or more times, as many as possible";
			//
			// match1OrMoreTimesAsFewAsPossibleToolStripMenuItem
			//
			this.match1OrMoreTimesAsFewAsPossibleToolStripMenuItem.Name = "match1OrMoreTimesAsFewAsPossibleToolStripMenuItem";
			this.match1OrMoreTimesAsFewAsPossibleToolStripMenuItem.Size = new System.Drawing.Size(350, 22);
			this.match1OrMoreTimesAsFewAsPossibleToolStripMenuItem.Text = "+? Match 1 or more times, as few as possible";
			//
			// nMatchExactlyNTimesToolStripMenuItem
			//
			this.nMatchExactlyNTimesToolStripMenuItem.Name = "nMatchExactlyNTimesToolStripMenuItem";
			this.nMatchExactlyNTimesToolStripMenuItem.Size = new System.Drawing.Size(350, 22);
			this.nMatchExactlyNTimesToolStripMenuItem.Text = "{n} Match exactly n times";
			//
			// nmMatchBetweenNAndMTimesAsManyAsPossibleToolStripMenuItem
			//
			this.nmMatchBetweenNAndMTimesAsManyAsPossibleToolStripMenuItem.Name = "nmMatchBetweenNAndMTimesAsManyAsPossibleToolStripMenuItem";
			this.nmMatchBetweenNAndMTimesAsManyAsPossibleToolStripMenuItem.Size = new System.Drawing.Size(350, 22);
			this.nmMatchBetweenNAndMTimesAsManyAsPossibleToolStripMenuItem.Text = "{n,m} Match between n and m times, as many as possible";
			//
			// toolStripSeparator1
			//
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(347, 6);
			//
			// rParagraphCharacterToolStripMenuItem
			//
			this.rParagraphCharacterToolStripMenuItem.Name = "rParagraphCharacterToolStripMenuItem";
			this.rParagraphCharacterToolStripMenuItem.Size = new System.Drawing.Size(350, 22);
			this.rParagraphCharacterToolStripMenuItem.Text = "\\r Paragraph Character";
			//
			// matchAtTheBeginningOfALineToolStripMenuItem
			//
			this.matchAtTheBeginningOfALineToolStripMenuItem.Name = "matchAtTheBeginningOfALineToolStripMenuItem";
			this.matchAtTheBeginningOfALineToolStripMenuItem.Size = new System.Drawing.Size(350, 22);
			this.matchAtTheBeginningOfALineToolStripMenuItem.Text = "^ Match at the beginning of a line";
			//
			// matchAtTheEndOfALineToolStripMenuItem
			//
			this.matchAtTheEndOfALineToolStripMenuItem.Name = "matchAtTheEndOfALineToolStripMenuItem";
			this.matchAtTheEndOfALineToolStripMenuItem.Size = new System.Drawing.Size(350, 22);
			this.matchAtTheEndOfALineToolStripMenuItem.Text = "$ Match at the end of a line";
			//
			// bMatchIfTheCurrentPositionIsAWordBoundaryToolStripMenuItem
			//
			this.bMatchIfTheCurrentPositionIsAWordBoundaryToolStripMenuItem.Name = "bMatchIfTheCurrentPositionIsAWordBoundaryToolStripMenuItem";
			this.bMatchIfTheCurrentPositionIsAWordBoundaryToolStripMenuItem.Size = new System.Drawing.Size(350, 22);
			this.bMatchIfTheCurrentPositionIsAWordBoundaryToolStripMenuItem.Text = "\\b Match if the current position is a word boundary";
			//
			// bMatchIfTheCurrentPositionIsNotAWordBoundaryToolStripMenuItem
			//
			this.bMatchIfTheCurrentPositionIsNotAWordBoundaryToolStripMenuItem.Name = "bMatchIfTheCurrentPositionIsNotAWordBoundaryToolStripMenuItem";
			this.bMatchIfTheCurrentPositionIsNotAWordBoundaryToolStripMenuItem.Size = new System.Drawing.Size(350, 22);
			this.bMatchIfTheCurrentPositionIsNotAWordBoundaryToolStripMenuItem.Text = "\\B Match if the current position is not a word boundary";
			//
			// dMatchAnyNumberOrDecimalDigitToolStripMenuItem
			//
			this.dMatchAnyNumberOrDecimalDigitToolStripMenuItem.Name = "dMatchAnyNumberOrDecimalDigitToolStripMenuItem";
			this.dMatchAnyNumberOrDecimalDigitToolStripMenuItem.Size = new System.Drawing.Size(350, 22);
			this.dMatchAnyNumberOrDecimalDigitToolStripMenuItem.Text = "\\d Match any number or decimal digit";
			//
			// dMatchAnyCharacterThatIsNotADecimalDigitToolStripMenuItem
			//
			this.dMatchAnyCharacterThatIsNotADecimalDigitToolStripMenuItem.Name = "dMatchAnyCharacterThatIsNotADecimalDigitToolStripMenuItem";
			this.dMatchAnyCharacterThatIsNotADecimalDigitToolStripMenuItem.Size = new System.Drawing.Size(350, 22);
			this.dMatchAnyCharacterThatIsNotADecimalDigitToolStripMenuItem.Text = "\\D Match any character that is not a decimal digit";
			//
			// sMatchAWhiteSpaceCharacterToolStripMenuItem
			//
			this.sMatchAWhiteSpaceCharacterToolStripMenuItem.Name = "sMatchAWhiteSpaceCharacterToolStripMenuItem";
			this.sMatchAWhiteSpaceCharacterToolStripMenuItem.Size = new System.Drawing.Size(350, 22);
			this.sMatchAWhiteSpaceCharacterToolStripMenuItem.Text = "\\s Match a white space character";
			//
			// sMatchANonwhiteSpaceCharacterToolStripMenuItem
			//
			this.sMatchANonwhiteSpaceCharacterToolStripMenuItem.Name = "sMatchANonwhiteSpaceCharacterToolStripMenuItem";
			this.sMatchANonwhiteSpaceCharacterToolStripMenuItem.Size = new System.Drawing.Size(350, 22);
			this.sMatchANonwhiteSpaceCharacterToolStripMenuItem.Text = "\\S Match a non-white space character";
			//
			// toolStripSeparator2
			//
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(347, 6);
			//
			// patternMatchAnyOneCharacterFromTheSetToolStripMenuItem
			//
			this.patternMatchAnyOneCharacterFromTheSetToolStripMenuItem.Name = "patternMatchAnyOneCharacterFromTheSetToolStripMenuItem";
			this.patternMatchAnyOneCharacterFromTheSetToolStripMenuItem.Size = new System.Drawing.Size(350, 22);
			this.patternMatchAnyOneCharacterFromTheSetToolStripMenuItem.Text = "[pattern] Match any one character from the set";
			//
			// aBMatchesEitherAOrBToolStripMenuItem
			//
			this.aBMatchesEitherAOrBToolStripMenuItem.Name = "aBMatchesEitherAOrBToolStripMenuItem";
			this.aBMatchesEitherAOrBToolStripMenuItem.Size = new System.Drawing.Size(350, 22);
			this.aBMatchesEitherAOrBToolStripMenuItem.Text = "| \'A|B\' matches either A or B";
			//
			// uhhhhMatchTheCharacterWithTheHexValueHhhhToolStripMenuItem
			//
			this.uhhhhMatchTheCharacterWithTheHexValueHhhhToolStripMenuItem.Name = "uhhhhMatchTheCharacterWithTheHexValueHhhhToolStripMenuItem";
			this.uhhhhMatchTheCharacterWithTheHexValueHhhhToolStripMenuItem.Size = new System.Drawing.Size(350, 22);
			this.uhhhhMatchTheCharacterWithTheHexValueHhhhToolStripMenuItem.Text = "\\uhhhh Match the character with the hex value hhhh";
			//
			// toolStripSeparator3
			//
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			this.toolStripSeparator3.Size = new System.Drawing.Size(347, 6);
			//
			// regularExpressionHelpToolStripMenuItem
			//
			this.regularExpressionHelpToolStripMenuItem.Name = "regularExpressionHelpToolStripMenuItem";
			this.regularExpressionHelpToolStripMenuItem.Size = new System.Drawing.Size(350, 22);
			this.regularExpressionHelpToolStripMenuItem.Text = "Regular Expression &Help";
			this.regularExpressionHelpToolStripMenuItem.Click += new System.EventHandler(this.regularExpressionHelpToolStripMenuItem_Click);
			//
			// buttonCancel
			//
			this.buttonCancel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.buttonCancel.Location = new System.Drawing.Point(337, 96);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(75, 23);
			this.buttonCancel.TabIndex = 11;
			this.buttonCancel.Text = "&Close";
			this.buttonCancel.UseVisualStyleBackColor = true;
			this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
			//
			// ecTextBoxFindWhat
			//
			this.ecTextBoxFindWhat.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.ecTextBoxFindWhat.Font = new System.Drawing.Font("Arial Unicode MS", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.ecTextBoxFindWhat.Location = new System.Drawing.Point(93, 12);
			this.ecTextBoxFindWhat.Name = "ecTextBoxFindWhat";
			this.ecTextBoxFindWhat.Size = new System.Drawing.Size(408, 29);
			this.ecTextBoxFindWhat.TabIndex = 1;
			this.ecTextBoxFindWhat.WordWrap = false;
			this.ecTextBoxFindWhat.TextChanged += new System.EventHandler(this.ecTextBox_TextChanged);
			//
			// ecTextBoxReplaceWith
			//
			this.ecTextBoxReplaceWith.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.ecTextBoxReplaceWith.Font = new System.Drawing.Font("Arial Unicode MS", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.ecTextBoxReplaceWith.Location = new System.Drawing.Point(93, 47);
			this.ecTextBoxReplaceWith.Name = "ecTextBoxReplaceWith";
			this.ecTextBoxReplaceWith.Size = new System.Drawing.Size(408, 29);
			this.ecTextBoxReplaceWith.TabIndex = 3;
			this.ecTextBoxReplaceWith.WordWrap = false;
			this.ecTextBoxReplaceWith.TextChanged += new System.EventHandler(this.ecTextBox_TextChanged);
			//
			// progressBar
			//
			this.progressBar.Location = new System.Drawing.Point(12, 136);
			this.progressBar.Name = "progressBar";
			this.progressBar.Size = new System.Drawing.Size(536, 23);
			this.progressBar.Step = 1;
			this.progressBar.TabIndex = 12;
			this.progressBar.Visible = false;
			//
			// backgroundWorker
			//
			this.backgroundWorker.WorkerReportsProgress = true;
			this.backgroundWorker.WorkerSupportsCancellation = true;
			this.backgroundWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorker_DoWork);
			this.backgroundWorker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorker_RunWorkerCompleted);
			this.backgroundWorker.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.backgroundWorker_ProgressChanged);
			//
			// FindReplaceForm
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(560, 171);
			this.Controls.Add(this.progressBar);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.checkBoxMatchCase);
			this.Controls.Add(this.ecTextBoxFindWhat);
			this.Controls.Add(this.ecTextBoxReplaceWith);
			this.Controls.Add(this.buttonExpressionBuilder);
			this.Controls.Add(this.buttonReplaceAll);
			this.Controls.Add(this.buttonReplace);
			this.Controls.Add(this.buttonFindNext);
			this.Controls.Add(this.comboBoxReplaceWith);
			this.Controls.Add(this.labelReplaceWith);
			this.Controls.Add(this.comboBoxFindWhat);
			this.Controls.Add(this.labelFindWhat);
			this.HelpButton = true;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FindReplaceForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
			this.Text = "Find/Replace";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FindReplaceForm_FormClosing);
			this.contextMenuStripExprBuilder.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button buttonReplaceAll;
		private System.Windows.Forms.Button buttonReplace;
		private System.Windows.Forms.Button buttonFindNext;
		private System.Windows.Forms.CheckBox checkBoxMatchCase;
		private System.Windows.Forms.ComboBox comboBoxReplaceWith;
		private System.Windows.Forms.Label labelReplaceWith;
		private System.Windows.Forms.ComboBox comboBoxFindWhat;
		private System.Windows.Forms.Label labelFindWhat;
		private System.Windows.Forms.Button buttonExpressionBuilder;
		private System.Windows.Forms.ContextMenuStrip contextMenuStripExprBuilder;
		private System.Windows.Forms.ToolStripMenuItem MatchAnyCharacterToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem match0OrMoreTimesAsManyAsPossibleToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem match0Or1TimesButPreferOneTimeToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem match0OrMoreTimesAsFewAsPossibleToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem match1OrMoreTimesAsManyAsPossibleToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem match1OrMoreTimesAsFewAsPossibleToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem nMatchExactlyNTimesToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem nmMatchBetweenNAndMTimesAsManyAsPossibleToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripMenuItem matchAtTheBeginningOfALineToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem bMatchIfTheCurrentPositionIsAWordBoundaryToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem dMatchAnyNumberOrDecimalDigitToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem dMatchAnyCharacterThatIsNotADecimalDigitToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem matchAtTheEndOfALineToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem bMatchIfTheCurrentPositionIsNotAWordBoundaryToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem sMatchAWhiteSpaceCharacterToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem sMatchANonwhiteSpaceCharacterToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripMenuItem patternMatchAnyOneCharacterFromTheSetToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem uhhhhMatchTheCharacterWithTheHexValueHhhhToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem aBMatchesEitherAOrBToolStripMenuItem;
		private SilEncConverters31.EcTextBox ecTextBoxReplaceWith;
		private SilEncConverters31.EcTextBox ecTextBoxFindWhat;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
		private System.Windows.Forms.ToolStripMenuItem regularExpressionHelpToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem rParagraphCharacterToolStripMenuItem;
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.HelpProvider helpProvider;
		private System.Windows.Forms.ProgressBar progressBar;
		private System.ComponentModel.BackgroundWorker backgroundWorker;
	}
}
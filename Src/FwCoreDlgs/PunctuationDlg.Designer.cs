using SIL.FieldWorks.Common.Controls;
namespace SIL.FieldWorks.FwCoreDlgs
{
	partial class PunctuationDlg
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PunctuationDlg));
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle14 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle13 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle12 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle16 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle15 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle9 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle10 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle11 = new System.Windows.Forms.DataGridViewCellStyle();
			this.pnlButtons = new System.Windows.Forms.Panel();
			this.btnOk = new System.Windows.Forms.Button();
			this.btnHelp = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.tabPunctuation = new System.Windows.Forms.TabControl();
			this.tpgMatchedPairs = new System.Windows.Forms.TabPage();
			this.pnlMatchedPairsGrid = new SIL.FieldWorks.Common.Controls.FwPanel();
			this.gridMatchedPairs = new SIL.FieldWorks.Common.Controls.FwBasicGrid();
			this.colOpen = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colClose = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colClosedByPara = new System.Windows.Forms.DataGridViewCheckBoxColumn();
			this.panel1 = new System.Windows.Forms.Panel();
			this.btnRemoveMatchedPair = new System.Windows.Forms.Button();
			this.btnAddMatchedPair = new System.Windows.Forms.Button();
			this.tpgQuotations = new System.Windows.Forms.TabPage();
			this.lblNumberLevels = new System.Windows.Forms.Label();
			this.chkParaContinuation = new System.Windows.Forms.CheckBox();
			this.grpParaCont = new System.Windows.Forms.GroupBox();
			this.grpContinuationType = new System.Windows.Forms.GroupBox();
			this.rbRequireAll = new System.Windows.Forms.RadioButton();
			this.rbInnermost = new System.Windows.Forms.RadioButton();
			this.rbOutermost = new System.Windows.Forms.RadioButton();
			this.grpContinuationMark = new System.Windows.Forms.GroupBox();
			this.rbClosing = new System.Windows.Forms.RadioButton();
			this.rbOpening = new System.Windows.Forms.RadioButton();
			this.spinLevels = new System.Windows.Forms.NumericUpDown();
			this.lblQuotationMarksList = new System.Windows.Forms.Label();
			this.cboQuotationLangs = new System.Windows.Forms.ComboBox();
			this.pnlQMarks = new SIL.FieldWorks.Common.Controls.FwPanel();
			this.gridQMarks = new SIL.FieldWorks.Common.Controls.FwBasicGrid();
			this.tpgPatterns = new System.Windows.Forms.TabPage();
			this.gridPatterns = new SIL.FieldWorks.Common.Controls.FwBasicGrid();
			this.colPattern = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colContext = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colPatternCount = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colPatternValid = new System.Windows.Forms.DataGridViewCheckBoxColumn();
			this.m_lblWsName = new System.Windows.Forms.Label();
			this.contextCtrl = new SIL.FieldWorks.FwCoreDlgs.CharContextCtrl();
			this.colLevel = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colOpeningQMark = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colClosingQMark = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.pnlButtons.SuspendLayout();
			this.tabPunctuation.SuspendLayout();
			this.tpgMatchedPairs.SuspendLayout();
			this.pnlMatchedPairsGrid.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.gridMatchedPairs)).BeginInit();
			this.panel1.SuspendLayout();
			this.tpgQuotations.SuspendLayout();
			this.grpParaCont.SuspendLayout();
			this.grpContinuationType.SuspendLayout();
			this.grpContinuationMark.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.spinLevels)).BeginInit();
			this.pnlQMarks.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.gridQMarks)).BeginInit();
			this.tpgPatterns.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.gridPatterns)).BeginInit();
			this.SuspendLayout();
			//
			// pnlButtons
			//
			this.pnlButtons.BackColor = System.Drawing.Color.Transparent;
			this.pnlButtons.Controls.Add(this.btnOk);
			this.pnlButtons.Controls.Add(this.btnHelp);
			this.pnlButtons.Controls.Add(this.btnCancel);
			resources.ApplyResources(this.pnlButtons, "pnlButtons");
			this.pnlButtons.Name = "pnlButtons";
			//
			// btnOk
			//
			resources.ApplyResources(this.btnOk, "btnOk");
			this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnOk.Name = "btnOk";
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
			this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
			//
			// tabPunctuation
			//
			resources.ApplyResources(this.tabPunctuation, "tabPunctuation");
			this.tabPunctuation.Controls.Add(this.tpgMatchedPairs);
			this.tabPunctuation.Controls.Add(this.tpgQuotations);
			this.tabPunctuation.Controls.Add(this.tpgPatterns);
			this.tabPunctuation.Name = "tabPunctuation";
			this.tabPunctuation.SelectedIndex = 0;
			//
			// tpgMatchedPairs
			//
			this.tpgMatchedPairs.Controls.Add(this.pnlMatchedPairsGrid);
			this.tpgMatchedPairs.Controls.Add(this.panel1);
			resources.ApplyResources(this.tpgMatchedPairs, "tpgMatchedPairs");
			this.tpgMatchedPairs.Name = "tpgMatchedPairs";
			this.tpgMatchedPairs.UseVisualStyleBackColor = true;
			//
			// pnlMatchedPairsGrid
			//
			this.pnlMatchedPairsGrid.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.pnlMatchedPairsGrid.ClipTextForChildControls = true;
			this.pnlMatchedPairsGrid.ControlReceivingFocusOnMnemonic = null;
			this.pnlMatchedPairsGrid.Controls.Add(this.gridMatchedPairs);
			resources.ApplyResources(this.pnlMatchedPairsGrid, "pnlMatchedPairsGrid");
			this.pnlMatchedPairsGrid.DoubleBuffered = true;
			this.pnlMatchedPairsGrid.MnemonicGeneratesClick = false;
			this.pnlMatchedPairsGrid.Name = "pnlMatchedPairsGrid";
			this.pnlMatchedPairsGrid.PaintExplorerBarBackground = false;
			//
			// gridMatchedPairs
			//
			this.gridMatchedPairs.AllowUserToResizeRows = false;
			this.gridMatchedPairs.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
			this.gridMatchedPairs.BackgroundColor = System.Drawing.SystemColors.Window;
			this.gridMatchedPairs.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.gridMatchedPairs.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
			this.gridMatchedPairs.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.gridMatchedPairs.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
			this.colOpen,
			this.colClose,
			this.colClosedByPara});
			dataGridViewCellStyle14.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle14.BackColor = System.Drawing.SystemColors.Window;
			dataGridViewCellStyle14.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle14.ForeColor = System.Drawing.SystemColors.ControlText;
			dataGridViewCellStyle14.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(215)))), ((int)(((byte)(166)))));
			dataGridViewCellStyle14.SelectionForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle14.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.gridMatchedPairs.DefaultCellStyle = dataGridViewCellStyle14;
			resources.ApplyResources(this.gridMatchedPairs, "gridMatchedPairs");
			this.gridMatchedPairs.DrawSelectedCellFocusRect = true;
			this.gridMatchedPairs.GridColor = System.Drawing.SystemColors.GrayText;
			this.gridMatchedPairs.Name = "gridMatchedPairs";
			this.gridMatchedPairs.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
			this.gridMatchedPairs.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.gridMatchedPairs.ShowCellToolTips = false;
			this.gridMatchedPairs.StandardTab = true;
			this.gridMatchedPairs.VirtualMode = true;
			this.gridMatchedPairs.CellMouseLeave += new System.Windows.Forms.DataGridViewCellEventHandler(this.HandleGridCellMouseLeave);
			this.gridMatchedPairs.ColumnHeaderMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.gridMatchedPairs_ColumnHeaderMouseClick);
			this.gridMatchedPairs.RowHeightInfoNeeded += new System.Windows.Forms.DataGridViewRowHeightInfoNeededEventHandler(this.HandleRowHeightInfoNeeded);
			this.gridMatchedPairs.CellValueNeeded += new System.Windows.Forms.DataGridViewCellValueEventHandler(this.gridMatchedPairs_CellValueNeeded);
			this.gridMatchedPairs.RowsAdded += new System.Windows.Forms.DataGridViewRowsAddedEventHandler(this.gridMatchedPairs_RowsAdded);
			this.gridMatchedPairs.CellMouseEnter += new System.Windows.Forms.DataGridViewCellEventHandler(this.gridMatchedPairs_CellMouseEnter);
			this.gridMatchedPairs.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.gridMatchedPairs_CellEndEdit);
			this.gridMatchedPairs.CellValuePushed += new System.Windows.Forms.DataGridViewCellValueEventHandler(this.gridMatchedPairs_CellValuePushed);
			this.gridMatchedPairs.EditingControlShowing += new System.Windows.Forms.DataGridViewEditingControlShowingEventHandler(this.HandleEditingControlShowing);
			this.gridMatchedPairs.RowsRemoved += new System.Windows.Forms.DataGridViewRowsRemovedEventHandler(this.gridMatchedPairs_RowsRemoved);
			//
			// colOpen
			//
			resources.ApplyResources(this.colOpen, "colOpen");
			this.colOpen.MaxInputLength = 1;
			this.colOpen.Name = "colOpen";
			//
			// colClose
			//
			resources.ApplyResources(this.colClose, "colClose");
			this.colClose.MaxInputLength = 1;
			this.colClose.Name = "colClose";
			//
			// colClosedByPara
			//
			this.colClosedByPara.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			dataGridViewCellStyle13.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle13.NullValue = false;
			dataGridViewCellStyle13.Padding = new System.Windows.Forms.Padding(7, 0, 0, 0);
			this.colClosedByPara.DefaultCellStyle = dataGridViewCellStyle13;
			resources.ApplyResources(this.colClosedByPara, "colClosedByPara");
			this.colClosedByPara.Name = "colClosedByPara";
			this.colClosedByPara.Resizable = System.Windows.Forms.DataGridViewTriState.False;
			this.colClosedByPara.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
			//
			// panel1
			//
			this.panel1.Controls.Add(this.btnRemoveMatchedPair);
			this.panel1.Controls.Add(this.btnAddMatchedPair);
			resources.ApplyResources(this.panel1, "panel1");
			this.panel1.Name = "panel1";
			//
			// btnRemoveMatchedPair
			//
			resources.ApplyResources(this.btnRemoveMatchedPair, "btnRemoveMatchedPair");
			this.btnRemoveMatchedPair.Name = "btnRemoveMatchedPair";
			this.btnRemoveMatchedPair.Click += new System.EventHandler(this.btnRemoveMatchedPair_Click);
			//
			// btnAddMatchedPair
			//
			resources.ApplyResources(this.btnAddMatchedPair, "btnAddMatchedPair");
			this.btnAddMatchedPair.Name = "btnAddMatchedPair";
			this.btnAddMatchedPair.Click += new System.EventHandler(this.btnAddMatchedPair_Click);
			//
			// tpgQuotations
			//
			this.tpgQuotations.Controls.Add(this.lblNumberLevels);
			this.tpgQuotations.Controls.Add(this.chkParaContinuation);
			this.tpgQuotations.Controls.Add(this.grpParaCont);
			this.tpgQuotations.Controls.Add(this.spinLevels);
			this.tpgQuotations.Controls.Add(this.lblQuotationMarksList);
			this.tpgQuotations.Controls.Add(this.cboQuotationLangs);
			this.tpgQuotations.Controls.Add(this.pnlQMarks);
			resources.ApplyResources(this.tpgQuotations, "tpgQuotations");
			this.tpgQuotations.Name = "tpgQuotations";
			this.tpgQuotations.UseVisualStyleBackColor = true;
			//
			// lblNumberLevels
			//
			resources.ApplyResources(this.lblNumberLevels, "lblNumberLevels");
			this.lblNumberLevels.Name = "lblNumberLevels";
			//
			// chkParaContinuation
			//
			resources.ApplyResources(this.chkParaContinuation, "chkParaContinuation");
			this.chkParaContinuation.BackColor = System.Drawing.Color.Transparent;
			this.chkParaContinuation.Checked = true;
			this.chkParaContinuation.CheckState = System.Windows.Forms.CheckState.Checked;
			this.chkParaContinuation.Name = "chkParaContinuation";
			this.chkParaContinuation.UseVisualStyleBackColor = false;
			this.chkParaContinuation.CheckedChanged += new System.EventHandler(this.chkParaContinuation_CheckedChanged);
			//
			// grpParaCont
			//
			resources.ApplyResources(this.grpParaCont, "grpParaCont");
			this.grpParaCont.Controls.Add(this.grpContinuationType);
			this.grpParaCont.Controls.Add(this.grpContinuationMark);
			this.grpParaCont.Name = "grpParaCont";
			this.grpParaCont.TabStop = false;
			//
			// grpContinuationType
			//
			this.grpContinuationType.Controls.Add(this.rbRequireAll);
			this.grpContinuationType.Controls.Add(this.rbInnermost);
			this.grpContinuationType.Controls.Add(this.rbOutermost);
			resources.ApplyResources(this.grpContinuationType, "grpContinuationType");
			this.grpContinuationType.Name = "grpContinuationType";
			this.grpContinuationType.TabStop = false;
			//
			// rbRequireAll
			//
			resources.ApplyResources(this.rbRequireAll, "rbRequireAll");
			this.rbRequireAll.Checked = true;
			this.rbRequireAll.Name = "rbRequireAll";
			this.rbRequireAll.TabStop = true;
			this.rbRequireAll.UseVisualStyleBackColor = true;
			//
			// rbInnermost
			//
			resources.ApplyResources(this.rbInnermost, "rbInnermost");
			this.rbInnermost.Name = "rbInnermost";
			this.rbInnermost.UseVisualStyleBackColor = true;
			//
			// rbOutermost
			//
			resources.ApplyResources(this.rbOutermost, "rbOutermost");
			this.rbOutermost.Name = "rbOutermost";
			this.rbOutermost.UseVisualStyleBackColor = true;
			//
			// grpContinuationMark
			//
			this.grpContinuationMark.Controls.Add(this.rbClosing);
			this.grpContinuationMark.Controls.Add(this.rbOpening);
			resources.ApplyResources(this.grpContinuationMark, "grpContinuationMark");
			this.grpContinuationMark.Name = "grpContinuationMark";
			this.grpContinuationMark.TabStop = false;
			//
			// rbClosing
			//
			resources.ApplyResources(this.rbClosing, "rbClosing");
			this.rbClosing.Name = "rbClosing";
			this.rbClosing.UseVisualStyleBackColor = true;
			//
			// rbOpening
			//
			resources.ApplyResources(this.rbOpening, "rbOpening");
			this.rbOpening.Checked = true;
			this.rbOpening.Name = "rbOpening";
			this.rbOpening.TabStop = true;
			this.rbOpening.UseVisualStyleBackColor = true;
			//
			// spinLevels
			//
			resources.ApplyResources(this.spinLevels, "spinLevels");
			this.spinLevels.Maximum = new decimal(new int[] {
			5,
			0,
			0,
			0});
			this.spinLevels.Minimum = new decimal(new int[] {
			1,
			0,
			0,
			0});
			this.spinLevels.Name = "spinLevels";
			this.spinLevels.Value = new decimal(new int[] {
			1,
			0,
			0,
			0});
			this.spinLevels.ValueChanged += new System.EventHandler(this.spinLevels_ValueChanged);
			//
			// lblQuotationMarksList
			//
			resources.ApplyResources(this.lblQuotationMarksList, "lblQuotationMarksList");
			this.lblQuotationMarksList.Name = "lblQuotationMarksList";
			//
			// cboQuotationLangs
			//
			this.cboQuotationLangs.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawVariable;
			this.cboQuotationLangs.DropDownHeight = 300;
			this.cboQuotationLangs.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			resources.ApplyResources(this.cboQuotationLangs, "cboQuotationLangs");
			this.cboQuotationLangs.Name = "cboQuotationLangs";
			this.cboQuotationLangs.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.cboQuotationLangs_DrawItem);
			this.cboQuotationLangs.MeasureItem += new System.Windows.Forms.MeasureItemEventHandler(this.cboQuotationLangs_MeasureItem);
			this.cboQuotationLangs.SelectionChangeCommitted += new System.EventHandler(this.cboQuotationLangs_SelectionChangeCommitted);
			//
			// pnlQMarks
			//
			resources.ApplyResources(this.pnlQMarks, "pnlQMarks");
			this.pnlQMarks.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.pnlQMarks.ClipTextForChildControls = true;
			this.pnlQMarks.ControlReceivingFocusOnMnemonic = null;
			this.pnlQMarks.Controls.Add(this.gridQMarks);
			this.pnlQMarks.DoubleBuffered = true;
			this.pnlQMarks.MnemonicGeneratesClick = false;
			this.pnlQMarks.Name = "pnlQMarks";
			this.pnlQMarks.PaintExplorerBarBackground = false;
			this.pnlQMarks.TabStop = true;
			//
			// gridQMarks
			//
			this.gridQMarks.AllowUserToAddRows = false;
			this.gridQMarks.AllowUserToDeleteRows = false;
			this.gridQMarks.AllowUserToResizeRows = false;
			this.gridQMarks.BackgroundColor = System.Drawing.SystemColors.Window;
			this.gridQMarks.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.gridQMarks.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.gridQMarks.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
			this.colLevel,
			this.colOpeningQMark,
			this.colClosingQMark});
			dataGridViewCellStyle12.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle12.BackColor = System.Drawing.SystemColors.Window;
			dataGridViewCellStyle12.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle12.ForeColor = System.Drawing.SystemColors.ControlText;
			dataGridViewCellStyle12.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(215)))), ((int)(((byte)(166)))));
			dataGridViewCellStyle12.SelectionForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle12.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.gridQMarks.DefaultCellStyle = dataGridViewCellStyle12;
			resources.ApplyResources(this.gridQMarks, "gridQMarks");
			this.gridQMarks.DrawSelectedCellFocusRect = true;
			this.gridQMarks.Name = "gridQMarks";
			this.gridQMarks.RowHeadersVisible = false;
			this.gridQMarks.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.gridQMarks.ShowCellToolTips = false;
			this.gridQMarks.StandardTab = true;
			this.gridQMarks.VirtualMode = true;
			this.gridQMarks.CellMouseLeave += new System.Windows.Forms.DataGridViewCellEventHandler(this.HandleGridCellMouseLeave);
			this.gridQMarks.RowHeightInfoNeeded += new System.Windows.Forms.DataGridViewRowHeightInfoNeededEventHandler(this.HandleRowHeightInfoNeeded);
			this.gridQMarks.CellValueNeeded += new System.Windows.Forms.DataGridViewCellValueEventHandler(this.gridQMarks_CellValueNeeded);
			this.gridQMarks.CellMouseEnter += new System.Windows.Forms.DataGridViewCellEventHandler(this.gridQMarks_CellMouseEnter);
			this.gridQMarks.CellValuePushed += new System.Windows.Forms.DataGridViewCellValueEventHandler(this.gridQMarks_CellValuePushed);
			this.gridQMarks.EditingControlShowing += new System.Windows.Forms.DataGridViewEditingControlShowingEventHandler(this.HandleEditingControlShowing);
			//
			// tpgPatterns
			//
			this.tpgPatterns.Controls.Add(this.contextCtrl);
			this.tpgPatterns.Controls.Add(this.gridPatterns);
			resources.ApplyResources(this.tpgPatterns, "tpgPatterns");
			this.tpgPatterns.Name = "tpgPatterns";
			this.tpgPatterns.UseVisualStyleBackColor = true;
			//
			// gridPatterns
			//
			this.gridPatterns.AllowUserToAddRows = false;
			this.gridPatterns.AllowUserToDeleteRows = false;
			this.gridPatterns.AllowUserToResizeRows = false;
			this.gridPatterns.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
			this.gridPatterns.BackgroundColor = System.Drawing.SystemColors.Window;
			this.gridPatterns.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.gridPatterns.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
			this.gridPatterns.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.gridPatterns.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
			this.colPattern,
			this.colContext,
			this.colPatternCount,
			this.colPatternValid});
			dataGridViewCellStyle16.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle16.BackColor = System.Drawing.SystemColors.Window;
			dataGridViewCellStyle16.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle16.ForeColor = System.Drawing.SystemColors.ControlText;
			dataGridViewCellStyle16.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(215)))), ((int)(((byte)(166)))));
			dataGridViewCellStyle16.SelectionForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle16.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.gridPatterns.DefaultCellStyle = dataGridViewCellStyle16;
			this.gridPatterns.DrawSelectedCellFocusRect = true;
			this.gridPatterns.GridColor = System.Drawing.SystemColors.GrayText;
			resources.ApplyResources(this.gridPatterns, "gridPatterns");
			this.gridPatterns.Name = "gridPatterns";
			this.gridPatterns.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
			this.gridPatterns.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.gridPatterns.ShowCellToolTips = false;
			this.gridPatterns.StandardTab = true;
			this.gridPatterns.VirtualMode = true;
			this.gridPatterns.CellMouseLeave += new System.Windows.Forms.DataGridViewCellEventHandler(this.HandleGridCellMouseLeave);
			this.gridPatterns.ColumnHeaderMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.gridPatterns_ColumnHeaderMouseClick);
			this.gridPatterns.RowHeightInfoNeeded += new System.Windows.Forms.DataGridViewRowHeightInfoNeededEventHandler(this.HandleRowHeightInfoNeeded);
			this.gridPatterns.CellValueNeeded += new System.Windows.Forms.DataGridViewCellValueEventHandler(this.gridPatterns_CellValueNeeded);
			this.gridPatterns.CellMouseEnter += new System.Windows.Forms.DataGridViewCellEventHandler(this.gridPatterns_CellMouseEnter);
			this.gridPatterns.CellValuePushed += new System.Windows.Forms.DataGridViewCellValueEventHandler(this.gridPatterns_CellValuePushed);
			//
			// colPattern
			//
			this.colPattern.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			resources.ApplyResources(this.colPattern, "colPattern");
			this.colPattern.Name = "colPattern";
			this.colPattern.ReadOnly = true;
			//
			// colContext
			//
			this.colContext.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			resources.ApplyResources(this.colContext, "colContext");
			this.colContext.Name = "colContext";
			this.colContext.ReadOnly = true;
			//
			// colPatternCount
			//
			this.colPatternCount.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			resources.ApplyResources(this.colPatternCount, "colPatternCount");
			this.colPatternCount.Name = "colPatternCount";
			this.colPatternCount.ReadOnly = true;
			//
			// colPatternValid
			//
			this.colPatternValid.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			dataGridViewCellStyle15.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle15.NullValue = System.Windows.Forms.CheckState.Indeterminate;
			dataGridViewCellStyle15.Padding = new System.Windows.Forms.Padding(7, 0, 0, 0);
			this.colPatternValid.DefaultCellStyle = dataGridViewCellStyle15;
			resources.ApplyResources(this.colPatternValid, "colPatternValid");
			this.colPatternValid.Name = "colPatternValid";
			this.colPatternValid.Resizable = System.Windows.Forms.DataGridViewTriState.False;
			this.colPatternValid.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
			this.colPatternValid.ThreeState = true;
			//
			// m_lblWsName
			//
			resources.ApplyResources(this.m_lblWsName, "m_lblWsName");
			this.m_lblWsName.Name = "m_lblWsName";
			//
			// contextCtrl
			//
			this.contextCtrl.DisplayedListName = "punctuation patterns";
			this.contextCtrl.InitialDirectoryForFileScan = global::SIL.FieldWorks.FwCoreDlgs.FwCoreDlgs.kstidOpen;
			resources.ApplyResources(this.contextCtrl, "contextCtrl");
			this.contextCtrl.Name = "contextCtrl";
			this.contextCtrl.ScanMsgLabelText = "To see occurrences, click Scan.";
			this.contextCtrl.GetContextInfo += new SIL.FieldWorks.FwCoreDlgs.CharContextCtrl.GetContextInfoHandler(this.contextCtrl_GetContextInfo);
			this.contextCtrl.BeforeTextTokenSubStringsLoaded += new SIL.FieldWorks.FwCoreDlgs.CharContextCtrl.BeforeTextTokenSubStringsLoadedHandler(this.contextCtrl_BeforeTextTokenSubStringsLoaded);
			this.contextCtrl.TextTokenSubStringsLoaded += new SIL.FieldWorks.FwCoreDlgs.CharContextCtrl.TextTokenSubStringsLoadedHandler(this.FillPatternGrid);
			//
			// colLevel
			//
			this.colLevel.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			dataGridViewCellStyle9.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
			this.colLevel.DefaultCellStyle = dataGridViewCellStyle9;
			resources.ApplyResources(this.colLevel, "colLevel");
			this.colLevel.Name = "colLevel";
			this.colLevel.ReadOnly = true;
			this.colLevel.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			//
			// colOpeningQMark
			//
			this.colOpeningQMark.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			dataGridViewCellStyle10.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
			this.colOpeningQMark.DefaultCellStyle = dataGridViewCellStyle10;
			this.colOpeningQMark.FillWeight = 50F;
			resources.ApplyResources(this.colOpeningQMark, "colOpeningQMark");
			this.colOpeningQMark.Name = "colOpeningQMark";
			this.colOpeningQMark.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			//
			// colClosingQMark
			//
			this.colClosingQMark.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			dataGridViewCellStyle11.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
			this.colClosingQMark.DefaultCellStyle = dataGridViewCellStyle11;
			this.colClosingQMark.FillWeight = 50F;
			resources.ApplyResources(this.colClosingQMark, "colClosingQMark");
			this.colClosingQMark.Name = "colClosingQMark";
			this.colClosingQMark.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			//
			// PunctuationDlg
			//
			this.AcceptButton = this.btnOk;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.m_lblWsName);
			this.Controls.Add(this.pnlButtons);
			this.Controls.Add(this.tabPunctuation);
			this.DoubleBuffered = true;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "PunctuationDlg";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
			this.pnlButtons.ResumeLayout(false);
			this.tabPunctuation.ResumeLayout(false);
			this.tpgMatchedPairs.ResumeLayout(false);
			this.pnlMatchedPairsGrid.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.gridMatchedPairs)).EndInit();
			this.panel1.ResumeLayout(false);
			this.tpgQuotations.ResumeLayout(false);
			this.tpgQuotations.PerformLayout();
			this.grpParaCont.ResumeLayout(false);
			this.grpContinuationType.ResumeLayout(false);
			this.grpContinuationType.PerformLayout();
			this.grpContinuationMark.ResumeLayout(false);
			this.grpContinuationMark.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.spinLevels)).EndInit();
			this.pnlQMarks.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.gridQMarks)).EndInit();
			this.tpgPatterns.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.gridPatterns)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Panel pnlButtons;
		private System.Windows.Forms.Button btnOk;
		private System.Windows.Forms.Button btnHelp;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.TabControl tabPunctuation;
		private System.Windows.Forms.TabPage tpgMatchedPairs;
		private System.Windows.Forms.TabPage tpgPatterns;
		private System.Windows.Forms.Button btnAddMatchedPair;
		private System.Windows.Forms.Button btnRemoveMatchedPair;
		private System.Windows.Forms.TabPage tpgQuotations;
		private FwBasicGrid gridMatchedPairs;
		private System.Windows.Forms.Panel panel1;
		private SIL.FieldWorks.Common.Controls.FwPanel pnlMatchedPairsGrid;
		private FwBasicGrid gridPatterns;
		private CharContextCtrl contextCtrl;
		private System.Windows.Forms.Label m_lblWsName;
		private System.Windows.Forms.Label lblQuotationMarksList;
		private System.Windows.Forms.ComboBox cboQuotationLangs;
		private FwBasicGrid gridQMarks;
		private System.Windows.Forms.NumericUpDown spinLevels;
		private FwPanel pnlQMarks;
		private System.Windows.Forms.DataGridViewTextBoxColumn colOpen;
		private System.Windows.Forms.DataGridViewTextBoxColumn colClose;
		private System.Windows.Forms.DataGridViewCheckBoxColumn colClosedByPara;
		private System.Windows.Forms.DataGridViewTextBoxColumn colPattern;
		private System.Windows.Forms.DataGridViewTextBoxColumn colContext;
		private System.Windows.Forms.DataGridViewTextBoxColumn colPatternCount;
		private System.Windows.Forms.DataGridViewCheckBoxColumn colPatternValid;
		private System.Windows.Forms.GroupBox grpContinuationType;
		private System.Windows.Forms.CheckBox chkParaContinuation;
		private System.Windows.Forms.RadioButton rbOpening;
		private System.Windows.Forms.RadioButton rbOutermost;
		private System.Windows.Forms.RadioButton rbInnermost;
		private System.Windows.Forms.RadioButton rbRequireAll;
		private System.Windows.Forms.GroupBox grpContinuationMark;
		private System.Windows.Forms.RadioButton rbClosing;
		private System.Windows.Forms.GroupBox grpParaCont;
		private System.Windows.Forms.Label lblNumberLevels;
		private System.Windows.Forms.DataGridViewTextBoxColumn colLevel;
		private System.Windows.Forms.DataGridViewTextBoxColumn colOpeningQMark;
		private System.Windows.Forms.DataGridViewTextBoxColumn colClosingQMark;
	}
}
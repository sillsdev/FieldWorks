using System.Diagnostics.CodeAnalysis;
namespace SIL.FieldWorks.FwCoreDlgs
{
	partial class ValidCharactersDlg
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
		// TODO-Linux: VirtualMode is not supported on Mono. TabStop is not implemented.
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification="See TODO-Linux comment")]
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ValidCharactersDlg));
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle7 = new System.Windows.Forms.DataGridViewCellStyle();
			this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.dataGridViewTextBoxColumn5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.dataGridViewTextBoxColumn6 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.dataGridViewTextBoxColumn7 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.btnOk = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.btnHelp = new System.Windows.Forms.Button();
			this.panel2 = new System.Windows.Forms.Panel();
			this.m_lblWsName = new System.Windows.Forms.Label();
			this.splitContainerOuter = new System.Windows.Forms.SplitContainer();
			this.tabCtrlAddFrom = new System.Windows.Forms.TabControl();
			this.tabBasedOn = new System.Windows.Forms.TabPage();
			this.rdoLanguageFile = new System.Windows.Forms.RadioButton();
			this.btnBrowseLangFile = new System.Windows.Forms.Button();
			this.label2 = new System.Windows.Forms.Label();
			this.txtLanguageFile = new System.Windows.Forms.TextBox();
			this.btnSimilarWs = new SIL.FieldWorks.FwCoreDlgControls.LocaleMenuButton();
			this.rdoSimilarWs = new System.Windows.Forms.RadioButton();
			this.panel1 = new System.Windows.Forms.Panel();
			this.tabManual = new System.Windows.Forms.TabPage();
			this.rbUnicodeValue = new System.Windows.Forms.RadioButton();
			this.grpUnicodeValue = new System.Windows.Forms.GroupBox();
			this.lblUnicodeValue = new System.Windows.Forms.Label();
			this.txtUnicodeValue = new SIL.FieldWorks.Common.Widgets.FwTextBox();
			this.rbCharRange = new System.Windows.Forms.RadioButton();
			this.rbSingleChar = new System.Windows.Forms.RadioButton();
			this.grpCharRange = new System.Windows.Forms.GroupBox();
			this.lblLastCharCode = new System.Windows.Forms.Label();
			this.lblFirstCharCode = new System.Windows.Forms.Label();
			this.lblRangeMsg = new System.Windows.Forms.Label();
			this.lblFirstChar = new System.Windows.Forms.Label();
			this.txtLastChar = new SIL.FieldWorks.Common.Widgets.FwTextBox();
			this.lblLastChar = new System.Windows.Forms.Label();
			this.txtFirstChar = new SIL.FieldWorks.Common.Widgets.FwTextBox();
			this.grpSingle = new System.Windows.Forms.GroupBox();
			this.lblSingle = new System.Windows.Forms.Label();
			this.txtManualCharEntry = new SIL.FieldWorks.Common.Widgets.FwTextBox();
			this.tabData = new System.Windows.Forms.TabPage();
			this.contextCtrl = new SIL.FieldWorks.FwCoreDlgs.CharContextCtrl();
			this.gridCharInventory = new System.Windows.Forms.DataGridView();
			this.colChar = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colCharCode = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colCount = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colStatus = new System.Windows.Forms.DataGridViewCheckBoxColumn();
			this.tabUnicode = new System.Windows.Forms.TabPage();
			this.label3 = new System.Windows.Forms.Label();
			this.splitValidCharsOuter = new System.Windows.Forms.SplitContainer();
			this.splitValidCharsInner = new System.Windows.Forms.SplitContainer();
			this.pnlWordForming = new SIL.FieldWorks.Common.Controls.FwPanel();
			this.chrGridWordForming = new SIL.FieldWorks.Common.Controls.CharacterGrid();
			this.hlblWordForming = new SIL.FieldWorks.Common.Controls.HeaderLabel();
			this.pnlOther = new SIL.FieldWorks.Common.Controls.FwPanel();
			this.chrGridOther = new SIL.FieldWorks.Common.Controls.CharacterGrid();
			this.hlblOther = new SIL.FieldWorks.Common.Controls.HeaderLabel();
			this.pnlMoveButtons = new System.Windows.Forms.Panel();
			this.btnTreatAsPunct = new System.Windows.Forms.Button();
			this.btnTreatAsWrdForming = new System.Windows.Forms.Button();
			this.pnlNumbers = new SIL.FieldWorks.Common.Controls.FwPanel();
			this.chrGridNumbers = new SIL.FieldWorks.Common.Controls.CharacterGrid();
			this.hlblNumbers = new SIL.FieldWorks.Common.Controls.HeaderLabel();
			this.label4 = new System.Windows.Forms.Label();
			this.cboSortOrder = new System.Windows.Forms.ComboBox();
			this.btnAddCharacters = new System.Windows.Forms.Button();
			this.btnRemoveAll = new System.Windows.Forms.Button();
			this.btnRemoveChar = new System.Windows.Forms.Button();
			this.lblValidChars = new System.Windows.Forms.Label();
			this.m_tooltip = new System.Windows.Forms.ToolTip(this.components);
			this.panel2.SuspendLayout();
			this.splitContainerOuter.Panel1.SuspendLayout();
			this.splitContainerOuter.Panel2.SuspendLayout();
			this.splitContainerOuter.SuspendLayout();
			this.tabCtrlAddFrom.SuspendLayout();
			this.tabBasedOn.SuspendLayout();
			this.tabManual.SuspendLayout();
			this.grpUnicodeValue.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.txtUnicodeValue)).BeginInit();
			this.grpCharRange.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.txtLastChar)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.txtFirstChar)).BeginInit();
			this.grpSingle.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.txtManualCharEntry)).BeginInit();
			this.tabData.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.gridCharInventory)).BeginInit();
			this.tabUnicode.SuspendLayout();
			this.splitValidCharsOuter.Panel1.SuspendLayout();
			this.splitValidCharsOuter.Panel2.SuspendLayout();
			this.splitValidCharsOuter.SuspendLayout();
			this.splitValidCharsInner.Panel1.SuspendLayout();
			this.splitValidCharsInner.Panel2.SuspendLayout();
			this.splitValidCharsInner.SuspendLayout();
			this.pnlWordForming.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.chrGridWordForming)).BeginInit();
			this.pnlOther.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.chrGridOther)).BeginInit();
			this.pnlMoveButtons.SuspendLayout();
			this.pnlNumbers.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.chrGridNumbers)).BeginInit();
			this.SuspendLayout();
			//
			// dataGridViewTextBoxColumn1
			//
			this.dataGridViewTextBoxColumn1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.dataGridViewTextBoxColumn1.DataPropertyName = "Character";
			resources.ApplyResources(this.dataGridViewTextBoxColumn1, "dataGridViewTextBoxColumn1");
			this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
			this.dataGridViewTextBoxColumn1.ReadOnly = true;
			this.dataGridViewTextBoxColumn1.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
			//
			// dataGridViewTextBoxColumn2
			//
			this.dataGridViewTextBoxColumn2.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.dataGridViewTextBoxColumn2.DataPropertyName = "CharacterCodes";
			resources.ApplyResources(this.dataGridViewTextBoxColumn2, "dataGridViewTextBoxColumn2");
			this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
			this.dataGridViewTextBoxColumn2.ReadOnly = true;
			this.dataGridViewTextBoxColumn2.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
			//
			// dataGridViewTextBoxColumn3
			//
			this.dataGridViewTextBoxColumn3.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
			this.dataGridViewTextBoxColumn3.DataPropertyName = "Count";
			resources.ApplyResources(this.dataGridViewTextBoxColumn3, "dataGridViewTextBoxColumn3");
			this.dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
			this.dataGridViewTextBoxColumn3.ReadOnly = true;
			this.dataGridViewTextBoxColumn3.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
			//
			// dataGridViewTextBoxColumn4
			//
			this.dataGridViewTextBoxColumn4.DataPropertyName = "Reference";
			dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			this.dataGridViewTextBoxColumn4.DefaultCellStyle = dataGridViewCellStyle1;
			this.dataGridViewTextBoxColumn4.HeaderText = global::SIL.FieldWorks.FwCoreDlgs.FwCoreDlgs.kstidOpen;
			this.dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
			this.dataGridViewTextBoxColumn4.ReadOnly = true;
			resources.ApplyResources(this.dataGridViewTextBoxColumn4, "dataGridViewTextBoxColumn4");
			//
			// dataGridViewTextBoxColumn5
			//
			this.dataGridViewTextBoxColumn5.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
			this.dataGridViewTextBoxColumn5.DataPropertyName = "Before";
			dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
			this.dataGridViewTextBoxColumn5.DefaultCellStyle = dataGridViewCellStyle2;
			this.dataGridViewTextBoxColumn5.HeaderText = global::SIL.FieldWorks.FwCoreDlgs.FwCoreDlgs.kstidOpen;
			this.dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
			this.dataGridViewTextBoxColumn5.ReadOnly = true;
			//
			// dataGridViewTextBoxColumn6
			//
			this.dataGridViewTextBoxColumn6.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
			this.dataGridViewTextBoxColumn6.DataPropertyName = "Character";
			dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
			dataGridViewCellStyle3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))));
			this.dataGridViewTextBoxColumn6.DefaultCellStyle = dataGridViewCellStyle3;
			this.dataGridViewTextBoxColumn6.HeaderText = global::SIL.FieldWorks.FwCoreDlgs.FwCoreDlgs.kstidOpen;
			this.dataGridViewTextBoxColumn6.Name = "dataGridViewTextBoxColumn6";
			this.dataGridViewTextBoxColumn6.ReadOnly = true;
			resources.ApplyResources(this.dataGridViewTextBoxColumn6, "dataGridViewTextBoxColumn6");
			//
			// dataGridViewTextBoxColumn7
			//
			this.dataGridViewTextBoxColumn7.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
			this.dataGridViewTextBoxColumn7.DataPropertyName = "After";
			dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			this.dataGridViewTextBoxColumn7.DefaultCellStyle = dataGridViewCellStyle4;
			this.dataGridViewTextBoxColumn7.HeaderText = global::SIL.FieldWorks.FwCoreDlgs.FwCoreDlgs.kstidOpen;
			this.dataGridViewTextBoxColumn7.Name = "dataGridViewTextBoxColumn7";
			this.dataGridViewTextBoxColumn7.ReadOnly = true;
			resources.ApplyResources(this.dataGridViewTextBoxColumn7, "dataGridViewTextBoxColumn7");
			//
			// btnOk
			//
			resources.ApplyResources(this.btnOk, "btnOk");
			this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnOk.Name = "btnOk";
			this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
			//
			// btnCancel
			//
			resources.ApplyResources(this.btnCancel, "btnCancel");
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Name = "btnCancel";
			//
			// btnHelp
			//
			resources.ApplyResources(this.btnHelp, "btnHelp");
			this.btnHelp.Name = "btnHelp";
			this.btnHelp.Click += new System.EventHandler(this.btnHelp_Click);
			//
			// panel2
			//
			this.panel2.Controls.Add(this.btnOk);
			this.panel2.Controls.Add(this.btnHelp);
			this.panel2.Controls.Add(this.btnCancel);
			resources.ApplyResources(this.panel2, "panel2");
			this.panel2.Name = "panel2";
			//
			// m_lblWsName
			//
			resources.ApplyResources(this.m_lblWsName, "m_lblWsName");
			this.m_lblWsName.Name = "m_lblWsName";
			//
			// splitContainerOuter
			//
			resources.ApplyResources(this.splitContainerOuter, "splitContainerOuter");
			this.splitContainerOuter.Name = "splitContainerOuter";
			//
			// splitContainerOuter.Panel1
			//
			this.splitContainerOuter.Panel1.Controls.Add(this.tabCtrlAddFrom);
			//
			// splitContainerOuter.Panel2
			//
			this.splitContainerOuter.Panel2.Controls.Add(this.splitValidCharsOuter);
			this.splitContainerOuter.Panel2.Controls.Add(this.label4);
			this.splitContainerOuter.Panel2.Controls.Add(this.cboSortOrder);
			this.splitContainerOuter.Panel2.Controls.Add(this.btnAddCharacters);
			this.splitContainerOuter.Panel2.Controls.Add(this.btnRemoveAll);
			this.splitContainerOuter.Panel2.Controls.Add(this.btnRemoveChar);
			this.splitContainerOuter.Panel2.Controls.Add(this.lblValidChars);
			this.splitContainerOuter.TabStop = false;
			//
			// tabCtrlAddFrom
			//
			this.tabCtrlAddFrom.Controls.Add(this.tabBasedOn);
			this.tabCtrlAddFrom.Controls.Add(this.tabManual);
			this.tabCtrlAddFrom.Controls.Add(this.tabData);
			this.tabCtrlAddFrom.Controls.Add(this.tabUnicode);
			resources.ApplyResources(this.tabCtrlAddFrom, "tabCtrlAddFrom");
			this.tabCtrlAddFrom.Name = "tabCtrlAddFrom";
			this.tabCtrlAddFrom.SelectedIndex = 0;
			this.tabCtrlAddFrom.SelectedIndexChanged += new System.EventHandler(this.tabControlAddFrom_SelectedIndexChanged);
			this.tabCtrlAddFrom.ClientSizeChanged += new System.EventHandler(this.tabCtrlAddFrom_ClientSizeChanged);
			//
			// tabBasedOn
			//
			this.tabBasedOn.Controls.Add(this.rdoLanguageFile);
			this.tabBasedOn.Controls.Add(this.btnBrowseLangFile);
			this.tabBasedOn.Controls.Add(this.label2);
			this.tabBasedOn.Controls.Add(this.txtLanguageFile);
			this.tabBasedOn.Controls.Add(this.btnSimilarWs);
			this.tabBasedOn.Controls.Add(this.rdoSimilarWs);
			this.tabBasedOn.Controls.Add(this.panel1);
			resources.ApplyResources(this.tabBasedOn, "tabBasedOn");
			this.tabBasedOn.Name = "tabBasedOn";
			this.tabBasedOn.UseVisualStyleBackColor = true;
			//
			// rdoLanguageFile
			//
			resources.ApplyResources(this.rdoLanguageFile, "rdoLanguageFile");
			this.rdoLanguageFile.AutoEllipsis = true;
			this.rdoLanguageFile.Name = "rdoLanguageFile";
			this.rdoLanguageFile.UseVisualStyleBackColor = true;
			this.rdoLanguageFile.CheckedChanged += new System.EventHandler(this.BasedOnRadioButton_CheckedChanged);
			//
			// btnBrowseLangFile
			//
			resources.ApplyResources(this.btnBrowseLangFile, "btnBrowseLangFile");
			this.btnBrowseLangFile.Name = "btnBrowseLangFile";
			this.btnBrowseLangFile.UseVisualStyleBackColor = true;
			this.btnBrowseLangFile.Click += new System.EventHandler(this.btnBrowseLangFile_Click);
			//
			// label2
			//
			resources.ApplyResources(this.label2, "label2");
			this.label2.Name = "label2";
			//
			// txtLanguageFile
			//
			resources.ApplyResources(this.txtLanguageFile, "txtLanguageFile");
			this.txtLanguageFile.Name = "txtLanguageFile";
			this.txtLanguageFile.TextChanged += new System.EventHandler(this.tabControlAddFrom_SelectedIndexChanged);
			this.txtLanguageFile.Enter += new System.EventHandler(this.txtLanguageFile_Enter);
			//
			// btnSimilarWs
			//
			resources.ApplyResources(this.btnSimilarWs, "btnSimilarWs");
			this.btnSimilarWs.DisplayLocaleId = null;
			this.btnSimilarWs.Image = global::SIL.FieldWorks.FwCoreDlgs.Properties.Resources.ButtonDropDownArrow;
			this.btnSimilarWs.Name = "btnSimilarWs";
			this.btnSimilarWs.SelectedLocaleId = null;
			this.btnSimilarWs.UseVisualStyleBackColor = true;
			this.btnSimilarWs.LocaleSelected += new System.EventHandler(this.btnSimilarWs_LocaleSelected);
			//
			// rdoSimilarWs
			//
			this.rdoSimilarWs.AutoEllipsis = true;
			resources.ApplyResources(this.rdoSimilarWs, "rdoSimilarWs");
			this.rdoSimilarWs.Checked = true;
			this.rdoSimilarWs.Name = "rdoSimilarWs";
			this.rdoSimilarWs.TabStop = true;
			this.rdoSimilarWs.UseVisualStyleBackColor = true;
			this.rdoSimilarWs.TextChanged += new System.EventHandler(this.rdoSimilarWs_TextChanged);
			this.rdoSimilarWs.CheckedChanged += new System.EventHandler(this.BasedOnRadioButton_CheckedChanged);
			//
			// panel1
			//
			resources.ApplyResources(this.panel1, "panel1");
			this.panel1.Name = "panel1";
			//
			// tabManual
			//
			this.tabManual.Controls.Add(this.rbUnicodeValue);
			this.tabManual.Controls.Add(this.grpUnicodeValue);
			this.tabManual.Controls.Add(this.rbCharRange);
			this.tabManual.Controls.Add(this.rbSingleChar);
			this.tabManual.Controls.Add(this.grpCharRange);
			this.tabManual.Controls.Add(this.grpSingle);
			resources.ApplyResources(this.tabManual, "tabManual");
			this.tabManual.Name = "tabManual";
			this.tabManual.UseVisualStyleBackColor = true;
			//
			// rbUnicodeValue
			//
			resources.ApplyResources(this.rbUnicodeValue, "rbUnicodeValue");
			this.rbUnicodeValue.Name = "rbUnicodeValue";
			this.rbUnicodeValue.UseVisualStyleBackColor = true;
			this.rbUnicodeValue.CheckedChanged += new System.EventHandler(this.HandleCheckedChanged);
			//
			// grpUnicodeValue
			//
			resources.ApplyResources(this.grpUnicodeValue, "grpUnicodeValue");
			this.grpUnicodeValue.Controls.Add(this.lblUnicodeValue);
			this.grpUnicodeValue.Controls.Add(this.txtUnicodeValue);
			this.grpUnicodeValue.Name = "grpUnicodeValue";
			this.grpUnicodeValue.TabStop = false;
			//
			// lblUnicodeValue
			//
			resources.ApplyResources(this.lblUnicodeValue, "lblUnicodeValue");
			this.lblUnicodeValue.Name = "lblUnicodeValue";
			//
			// txtUnicodeValue
			//
			this.txtUnicodeValue.AcceptsReturn = false;
			this.txtUnicodeValue.AdjustStringHeight = true;
			resources.ApplyResources(this.txtUnicodeValue, "txtUnicodeValue");
			this.txtUnicodeValue.BackColor = System.Drawing.SystemColors.Window;
			this.txtUnicodeValue.controlID = null;
			this.txtUnicodeValue.HasBorder = true;
			this.txtUnicodeValue.Name = "txtUnicodeValue";
			this.txtUnicodeValue.SuppressEnter = false;
			this.txtUnicodeValue.WordWrap = false;
			this.txtUnicodeValue.TextChanged += new System.EventHandler(this.txtUnicodeValue_TextChanged);
			this.txtUnicodeValue.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtUnicodeValue_KeyPress);
			//
			// rbCharRange
			//
			resources.ApplyResources(this.rbCharRange, "rbCharRange");
			this.rbCharRange.Name = "rbCharRange";
			this.rbCharRange.UseVisualStyleBackColor = true;
			this.rbCharRange.CheckedChanged += new System.EventHandler(this.HandleCheckedChanged);
			//
			// rbSingleChar
			//
			resources.ApplyResources(this.rbSingleChar, "rbSingleChar");
			this.rbSingleChar.Checked = true;
			this.rbSingleChar.Name = "rbSingleChar";
			this.rbSingleChar.TabStop = true;
			this.rbSingleChar.UseVisualStyleBackColor = true;
			this.rbSingleChar.CheckedChanged += new System.EventHandler(this.HandleCheckedChanged);
			//
			// grpCharRange
			//
			resources.ApplyResources(this.grpCharRange, "grpCharRange");
			this.grpCharRange.Controls.Add(this.lblLastCharCode);
			this.grpCharRange.Controls.Add(this.lblFirstCharCode);
			this.grpCharRange.Controls.Add(this.lblRangeMsg);
			this.grpCharRange.Controls.Add(this.lblFirstChar);
			this.grpCharRange.Controls.Add(this.txtLastChar);
			this.grpCharRange.Controls.Add(this.lblLastChar);
			this.grpCharRange.Controls.Add(this.txtFirstChar);
			this.grpCharRange.Name = "grpCharRange";
			this.grpCharRange.TabStop = false;
			//
			// lblLastCharCode
			//
			resources.ApplyResources(this.lblLastCharCode, "lblLastCharCode");
			this.lblLastCharCode.Name = "lblLastCharCode";
			//
			// lblFirstCharCode
			//
			resources.ApplyResources(this.lblFirstCharCode, "lblFirstCharCode");
			this.lblFirstCharCode.Name = "lblFirstCharCode";
			//
			// lblRangeMsg
			//
			resources.ApplyResources(this.lblRangeMsg, "lblRangeMsg");
			this.lblRangeMsg.AutoEllipsis = true;
			this.lblRangeMsg.Name = "lblRangeMsg";
			//
			// lblFirstChar
			//
			resources.ApplyResources(this.lblFirstChar, "lblFirstChar");
			this.lblFirstChar.Name = "lblFirstChar";
			//
			// txtLastChar
			//
			this.txtLastChar.AcceptsReturn = false;
			this.txtLastChar.AdjustStringHeight = true;
			resources.ApplyResources(this.txtLastChar, "txtLastChar");
			this.txtLastChar.BackColor = System.Drawing.SystemColors.Window;
			this.txtLastChar.controlID = null;
			this.txtLastChar.HasBorder = true;
			this.txtLastChar.Name = "txtLastChar";
			this.txtLastChar.SuppressEnter = false;
			this.txtLastChar.WordWrap = false;
			this.txtLastChar.TextChanged += new System.EventHandler(this.txtLastChar_TextChanged);
			this.txtLastChar.Leave += new System.EventHandler(this.HandleCharTextBoxLeave);
			this.txtLastChar.Enter += new System.EventHandler(this.HandleCharTextBoxEnter);
			//
			// lblLastChar
			//
			resources.ApplyResources(this.lblLastChar, "lblLastChar");
			this.lblLastChar.Name = "lblLastChar";
			//
			// txtFirstChar
			//
			this.txtFirstChar.AcceptsReturn = false;
			this.txtFirstChar.AdjustStringHeight = true;
			this.txtFirstChar.BackColor = System.Drawing.SystemColors.Window;
			this.txtFirstChar.controlID = null;
			resources.ApplyResources(this.txtFirstChar, "txtFirstChar");
			this.txtFirstChar.HasBorder = true;
			this.txtFirstChar.Name = "txtFirstChar";
			this.txtFirstChar.SuppressEnter = false;
			this.txtFirstChar.WordWrap = false;
			this.txtFirstChar.TextChanged += new System.EventHandler(this.txtFirstChar_TextChanged);
			this.txtFirstChar.Leave += new System.EventHandler(this.HandleCharTextBoxLeave);
			this.txtFirstChar.Enter += new System.EventHandler(this.HandleCharTextBoxEnter);
			//
			// grpSingle
			//
			resources.ApplyResources(this.grpSingle, "grpSingle");
			this.grpSingle.Controls.Add(this.lblSingle);
			this.grpSingle.Controls.Add(this.txtManualCharEntry);
			this.grpSingle.Name = "grpSingle";
			this.grpSingle.TabStop = false;
			//
			// lblSingle
			//
			resources.ApplyResources(this.lblSingle, "lblSingle");
			this.lblSingle.Name = "lblSingle";
			//
			// txtManualCharEntry
			//
			this.txtManualCharEntry.AcceptsReturn = false;
			this.txtManualCharEntry.AdjustStringHeight = true;
			resources.ApplyResources(this.txtManualCharEntry, "txtManualCharEntry");
			this.txtManualCharEntry.BackColor = System.Drawing.SystemColors.Window;
			this.txtManualCharEntry.controlID = null;
			this.txtManualCharEntry.HasBorder = true;
			this.txtManualCharEntry.Name = "txtManualCharEntry";
			this.txtManualCharEntry.SuppressEnter = false;
			this.txtManualCharEntry.WordWrap = false;
			this.txtManualCharEntry.TextChanged += new System.EventHandler(this.txtManualCharEntry_TextChanged);
			this.txtManualCharEntry.Leave += new System.EventHandler(this.HandleCharTextBoxLeave);
			this.txtManualCharEntry.Enter += new System.EventHandler(this.HandleCharTextBoxEnter);
			//
			// tabData
			//
			this.tabData.Controls.Add(this.contextCtrl);
			this.tabData.Controls.Add(this.gridCharInventory);
			resources.ApplyResources(this.tabData, "tabData");
			this.tabData.Name = "tabData";
			this.tabData.UseVisualStyleBackColor = true;
			//
			// contextCtrl
			//
			this.contextCtrl.DisplayedListName = "characters";
			this.contextCtrl.InitialDirectoryForFileScan = global::SIL.FieldWorks.FwCoreDlgs.FwCoreDlgs.kstidOpen;
			resources.ApplyResources(this.contextCtrl, "contextCtrl");
			this.contextCtrl.Name = "contextCtrl";
			this.contextCtrl.ScanMsgLabelText = "For a list of characters currently in use, click Scan.";
			this.contextCtrl.GetContextInfo += new SIL.FieldWorks.FwCoreDlgs.CharContextCtrl.GetContextInfoHandler(this.contextCtrl_GetContextInfo);
			this.contextCtrl.TextTokenSubStringsLoaded += new SIL.FieldWorks.FwCoreDlgs.CharContextCtrl.TextTokenSubStringsLoadedHandler(this.contextCtrl_TextTokenSubStringsLoaded);
			//
			// gridCharInventory
			//
			this.gridCharInventory.AllowUserToAddRows = false;
			this.gridCharInventory.AllowUserToDeleteRows = false;
			this.gridCharInventory.AllowUserToResizeRows = false;
			this.gridCharInventory.BackgroundColor = System.Drawing.SystemColors.Window;
			this.gridCharInventory.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.gridCharInventory.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.gridCharInventory.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
			this.colChar,
			this.colCharCode,
			this.colCount,
			this.colStatus});
			resources.ApplyResources(this.gridCharInventory, "gridCharInventory");
			this.gridCharInventory.MultiSelect = false;
			this.gridCharInventory.Name = "gridCharInventory";
			this.gridCharInventory.RowHeadersVisible = false;
			this.gridCharInventory.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.gridCharInventory.VirtualMode = true;
			this.gridCharInventory.ColumnHeaderMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.gridCharInventory_ColumnHeaderMouseClick);
			this.gridCharInventory.CellValueNeeded += new System.Windows.Forms.DataGridViewCellValueEventHandler(this.gridCharInventory_CellValueNeeded);
			this.gridCharInventory.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.gridCharInventory_CellFormatting);
			this.gridCharInventory.CellPainting += new System.Windows.Forms.DataGridViewCellPaintingEventHandler(this.gridCharInventory_CellPainting);
			this.gridCharInventory.CellValuePushed += new System.Windows.Forms.DataGridViewCellValueEventHandler(this.gridCharInventory_CellValuePushed);
			//
			// colChar
			//
			this.colChar.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.colChar.DataPropertyName = "Character";
			resources.ApplyResources(this.colChar, "colChar");
			this.colChar.Name = "colChar";
			this.colChar.ReadOnly = true;
			this.colChar.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
			//
			// colCharCode
			//
			this.colCharCode.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.colCharCode.DataPropertyName = "CharacterCodes";
			resources.ApplyResources(this.colCharCode, "colCharCode");
			this.colCharCode.Name = "colCharCode";
			this.colCharCode.ReadOnly = true;
			this.colCharCode.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
			//
			// colCount
			//
			this.colCount.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
			this.colCount.DataPropertyName = "Count";
			resources.ApplyResources(this.colCount, "colCount");
			this.colCount.Name = "colCount";
			this.colCount.ReadOnly = true;
			this.colCount.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
			//
			// colStatus
			//
			this.colStatus.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
			this.colStatus.DataPropertyName = "IsValid";
			resources.ApplyResources(this.colStatus, "colStatus");
			this.colStatus.Name = "colStatus";
			this.colStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
			//
			// tabUnicode
			//
			this.tabUnicode.Controls.Add(this.label3);
			resources.ApplyResources(this.tabUnicode, "tabUnicode");
			this.tabUnicode.Name = "tabUnicode";
			this.tabUnicode.UseVisualStyleBackColor = true;
			//
			// label3
			//
			resources.ApplyResources(this.label3, "label3");
			this.label3.Name = "label3";
			//
			// splitValidCharsOuter
			//
			resources.ApplyResources(this.splitValidCharsOuter, "splitValidCharsOuter");
			this.splitValidCharsOuter.Name = "splitValidCharsOuter";
			//
			// splitValidCharsOuter.Panel1
			//
			this.splitValidCharsOuter.Panel1.Controls.Add(this.splitValidCharsInner);
			//
			// splitValidCharsOuter.Panel2
			//
			this.splitValidCharsOuter.Panel2.Controls.Add(this.pnlNumbers);
			this.splitValidCharsOuter.TabStop = false;
			//
			// splitValidCharsInner
			//
			resources.ApplyResources(this.splitValidCharsInner, "splitValidCharsInner");
			this.splitValidCharsInner.Name = "splitValidCharsInner";
			//
			// splitValidCharsInner.Panel1
			//
			this.splitValidCharsInner.Panel1.Controls.Add(this.pnlWordForming);
			//
			// splitValidCharsInner.Panel2
			//
			this.splitValidCharsInner.Panel2.Controls.Add(this.pnlOther);
			this.splitValidCharsInner.Panel2.Controls.Add(this.pnlMoveButtons);
			this.splitValidCharsInner.TabStop = false;
			//
			// pnlWordForming
			//
			this.pnlWordForming.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.pnlWordForming.ClipTextForChildControls = true;
			this.pnlWordForming.ControlReceivingFocusOnMnemonic = null;
			this.pnlWordForming.Controls.Add(this.chrGridWordForming);
			this.pnlWordForming.Controls.Add(this.hlblWordForming);
			resources.ApplyResources(this.pnlWordForming, "pnlWordForming");
			this.pnlWordForming.DoubleBuffered = true;
			this.pnlWordForming.MnemonicGeneratesClick = false;
			this.pnlWordForming.Name = "pnlWordForming";
			this.pnlWordForming.PaintExplorerBarBackground = false;
			//
			// chrGridWordForming
			//
			resources.ApplyResources(this.chrGridWordForming, "chrGridWordForming");
			this.chrGridWordForming.AllowUserToAddRows = false;
			this.chrGridWordForming.AllowUserToDeleteRows = false;
			this.chrGridWordForming.AllowUserToResizeColumns = false;
			this.chrGridWordForming.AllowUserToResizeRows = false;
			this.chrGridWordForming.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.chrGridWordForming.ColumnHeadersVisible = false;
			this.chrGridWordForming.Cursor = System.Windows.Forms.Cursors.Default;
			dataGridViewCellStyle5.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
			dataGridViewCellStyle5.BackColor = System.Drawing.SystemColors.Window;
			dataGridViewCellStyle5.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle5.ForeColor = System.Drawing.SystemColors.ControlText;
			dataGridViewCellStyle5.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle5.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle5.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.chrGridWordForming.DefaultCellStyle = dataGridViewCellStyle5;
			this.chrGridWordForming.LoadCharactersFromFont = false;
			this.chrGridWordForming.MultiSelect = false;
			this.chrGridWordForming.Name = "chrGridWordForming";
			this.chrGridWordForming.ReadOnly = true;
			this.chrGridWordForming.RowHeadersVisible = false;
			this.chrGridWordForming.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
			this.chrGridWordForming.ShowCellToolTips = false;
			this.chrGridWordForming.StandardTab = true;
			this.chrGridWordForming.VirtualMode = true;
			this.chrGridWordForming.DoubleClick += new System.EventHandler(this.btnRemoveChar_Click);
			this.chrGridWordForming.CharacterChanged += new SIL.FieldWorks.Common.Controls.CharacterGrid.CharacterChangedHandler(this.HandleCharGridCharacterChanged);
			//
			// hlblWordForming
			//
			this.hlblWordForming.ClipTextForChildControls = true;
			this.hlblWordForming.ControlReceivingFocusOnMnemonic = this.chrGridWordForming;
			resources.ApplyResources(this.hlblWordForming, "hlblWordForming");
			this.hlblWordForming.MnemonicGeneratesClick = false;
			this.hlblWordForming.Name = "hlblWordForming";
			this.hlblWordForming.ShowWindowBackgroudOnTopAndRightEdge = true;
			//
			// pnlOther
			//
			this.pnlOther.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.pnlOther.ClipTextForChildControls = true;
			this.pnlOther.ControlReceivingFocusOnMnemonic = null;
			this.pnlOther.Controls.Add(this.chrGridOther);
			this.pnlOther.Controls.Add(this.hlblOther);
			resources.ApplyResources(this.pnlOther, "pnlOther");
			this.pnlOther.DoubleBuffered = true;
			this.pnlOther.MnemonicGeneratesClick = false;
			this.pnlOther.Name = "pnlOther";
			this.pnlOther.PaintExplorerBarBackground = false;
			//
			// chrGridOther
			//
			resources.ApplyResources(this.chrGridOther, "chrGridOther");
			this.chrGridOther.AllowUserToAddRows = false;
			this.chrGridOther.AllowUserToDeleteRows = false;
			this.chrGridOther.AllowUserToResizeColumns = false;
			this.chrGridOther.AllowUserToResizeRows = false;
			this.chrGridOther.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.chrGridOther.ColumnHeadersVisible = false;
			this.chrGridOther.Cursor = System.Windows.Forms.Cursors.Default;
			dataGridViewCellStyle6.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
			dataGridViewCellStyle6.BackColor = System.Drawing.SystemColors.Window;
			dataGridViewCellStyle6.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle6.ForeColor = System.Drawing.SystemColors.ControlText;
			dataGridViewCellStyle6.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle6.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle6.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.chrGridOther.DefaultCellStyle = dataGridViewCellStyle6;
			this.chrGridOther.LoadCharactersFromFont = false;
			this.chrGridOther.MultiSelect = false;
			this.chrGridOther.Name = "chrGridOther";
			this.chrGridOther.ReadOnly = true;
			this.chrGridOther.RowHeadersVisible = false;
			this.chrGridOther.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
			this.chrGridOther.ShowCellToolTips = false;
			this.chrGridOther.StandardTab = true;
			this.chrGridOther.VirtualMode = true;
			this.chrGridOther.CharacterChanged += new SIL.FieldWorks.Common.Controls.CharacterGrid.CharacterChangedHandler(this.HandleCharGridCharacterChanged);
			//
			// hlblOther
			//
			this.hlblOther.ClipTextForChildControls = true;
			this.hlblOther.ControlReceivingFocusOnMnemonic = this.chrGridOther;
			resources.ApplyResources(this.hlblOther, "hlblOther");
			this.hlblOther.MnemonicGeneratesClick = false;
			this.hlblOther.Name = "hlblOther";
			this.hlblOther.ShowWindowBackgroudOnTopAndRightEdge = true;
			//
			// pnlMoveButtons
			//
			this.pnlMoveButtons.Controls.Add(this.btnTreatAsPunct);
			this.pnlMoveButtons.Controls.Add(this.btnTreatAsWrdForming);
			resources.ApplyResources(this.pnlMoveButtons, "pnlMoveButtons");
			this.pnlMoveButtons.Name = "pnlMoveButtons";
			//
			// btnTreatAsPunct
			//
			resources.ApplyResources(this.btnTreatAsPunct, "btnTreatAsPunct");
			this.btnTreatAsPunct.Name = "btnTreatAsPunct";
			this.m_tooltip.SetToolTip(this.btnTreatAsPunct, resources.GetString("btnTreatAsPunct.ToolTip"));
			this.btnTreatAsPunct.UseVisualStyleBackColor = true;
			this.btnTreatAsPunct.Click += new System.EventHandler(this.HandleTreatAsClick);
			//
			// btnTreatAsWrdForming
			//
			this.btnTreatAsWrdForming.AllowDrop = true;
			resources.ApplyResources(this.btnTreatAsWrdForming, "btnTreatAsWrdForming");
			this.btnTreatAsWrdForming.Name = "btnTreatAsWrdForming";
			this.m_tooltip.SetToolTip(this.btnTreatAsWrdForming, resources.GetString("btnTreatAsWrdForming.ToolTip"));
			this.btnTreatAsWrdForming.UseVisualStyleBackColor = true;
			this.btnTreatAsWrdForming.Click += new System.EventHandler(this.HandleTreatAsClick);
			//
			// pnlNumbers
			//
			this.pnlNumbers.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.pnlNumbers.ClipTextForChildControls = true;
			this.pnlNumbers.ControlReceivingFocusOnMnemonic = null;
			this.pnlNumbers.Controls.Add(this.chrGridNumbers);
			this.pnlNumbers.Controls.Add(this.hlblNumbers);
			resources.ApplyResources(this.pnlNumbers, "pnlNumbers");
			this.pnlNumbers.DoubleBuffered = true;
			this.pnlNumbers.MnemonicGeneratesClick = false;
			this.pnlNumbers.Name = "pnlNumbers";
			this.pnlNumbers.PaintExplorerBarBackground = false;
			//
			// chrGridNumbers
			//
			resources.ApplyResources(this.chrGridNumbers, "chrGridNumbers");
			this.chrGridNumbers.AllowUserToAddRows = false;
			this.chrGridNumbers.AllowUserToDeleteRows = false;
			this.chrGridNumbers.AllowUserToResizeColumns = false;
			this.chrGridNumbers.AllowUserToResizeRows = false;
			this.chrGridNumbers.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.chrGridNumbers.ColumnHeadersVisible = false;
			this.chrGridNumbers.Cursor = System.Windows.Forms.Cursors.Default;
			dataGridViewCellStyle7.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
			dataGridViewCellStyle7.BackColor = System.Drawing.SystemColors.Window;
			dataGridViewCellStyle7.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle7.ForeColor = System.Drawing.SystemColors.ControlText;
			dataGridViewCellStyle7.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle7.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle7.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.chrGridNumbers.DefaultCellStyle = dataGridViewCellStyle7;
			this.chrGridNumbers.LoadCharactersFromFont = false;
			this.chrGridNumbers.MultiSelect = false;
			this.chrGridNumbers.Name = "chrGridNumbers";
			this.chrGridNumbers.ReadOnly = true;
			this.chrGridNumbers.RowHeadersVisible = false;
			this.chrGridNumbers.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
			this.chrGridNumbers.ShowCellToolTips = false;
			this.chrGridNumbers.StandardTab = true;
			this.chrGridNumbers.VirtualMode = true;
			this.chrGridNumbers.CharacterChanged += new SIL.FieldWorks.Common.Controls.CharacterGrid.CharacterChangedHandler(this.HandleCharGridCharacterChanged);
			//
			// hlblNumbers
			//
			this.hlblNumbers.ClipTextForChildControls = true;
			this.hlblNumbers.ControlReceivingFocusOnMnemonic = this.chrGridNumbers;
			resources.ApplyResources(this.hlblNumbers, "hlblNumbers");
			this.hlblNumbers.MnemonicGeneratesClick = false;
			this.hlblNumbers.Name = "hlblNumbers";
			this.hlblNumbers.ShowWindowBackgroudOnTopAndRightEdge = true;
			//
			// label4
			//
			resources.ApplyResources(this.label4, "label4");
			this.label4.Name = "label4";
			//
			// cboSortOrder
			//
			resources.ApplyResources(this.cboSortOrder, "cboSortOrder");
			this.cboSortOrder.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cboSortOrder.FormattingEnabled = true;
			this.cboSortOrder.Items.AddRange(new object[] {
			resources.GetString("cboSortOrder.Items")});
			this.cboSortOrder.Name = "cboSortOrder";
			//
			// btnAddCharacters
			//
			this.btnAddCharacters.Image = global::SIL.FieldWorks.FwCoreDlgs.Properties.Resources.FwCopyRight;
			resources.ApplyResources(this.btnAddCharacters, "btnAddCharacters");
			this.btnAddCharacters.Name = "btnAddCharacters";
			this.btnAddCharacters.UseVisualStyleBackColor = true;
			this.btnAddCharacters.Click += new System.EventHandler(this.btnAddCharacters_Click);
			//
			// btnRemoveAll
			//
			resources.ApplyResources(this.btnRemoveAll, "btnRemoveAll");
			this.btnRemoveAll.Name = "btnRemoveAll";
			this.btnRemoveAll.Click += new System.EventHandler(this.btnRemoveAll_Click);
			//
			// btnRemoveChar
			//
			resources.ApplyResources(this.btnRemoveChar, "btnRemoveChar");
			this.btnRemoveChar.Name = "btnRemoveChar";
			this.btnRemoveChar.Click += new System.EventHandler(this.btnRemoveChar_Click);
			//
			// lblValidChars
			//
			resources.ApplyResources(this.lblValidChars, "lblValidChars");
			this.lblValidChars.AutoEllipsis = true;
			this.lblValidChars.Name = "lblValidChars";
			//
			// ValidCharactersDlg
			//
			this.AcceptButton = this.btnOk;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnCancel;
			this.Controls.Add(this.m_lblWsName);
			this.Controls.Add(this.splitContainerOuter);
			this.Controls.Add(this.panel2);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ValidCharactersDlg";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
			this.panel2.ResumeLayout(false);
			this.splitContainerOuter.Panel1.ResumeLayout(false);
			this.splitContainerOuter.Panel2.ResumeLayout(false);
			this.splitContainerOuter.Panel2.PerformLayout();
			this.splitContainerOuter.ResumeLayout(false);
			this.tabCtrlAddFrom.ResumeLayout(false);
			this.tabBasedOn.ResumeLayout(false);
			this.tabBasedOn.PerformLayout();
			this.tabManual.ResumeLayout(false);
			this.tabManual.PerformLayout();
			this.grpUnicodeValue.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.txtUnicodeValue)).EndInit();
			this.grpCharRange.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.txtLastChar)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.txtFirstChar)).EndInit();
			this.grpSingle.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.txtManualCharEntry)).EndInit();
			this.tabData.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.gridCharInventory)).EndInit();
			this.tabUnicode.ResumeLayout(false);
			this.tabUnicode.PerformLayout();
			this.splitValidCharsOuter.Panel1.ResumeLayout(false);
			this.splitValidCharsOuter.Panel2.ResumeLayout(false);
			this.splitValidCharsOuter.ResumeLayout(false);
			this.splitValidCharsInner.Panel1.ResumeLayout(false);
			this.splitValidCharsInner.Panel2.ResumeLayout(false);
			this.splitValidCharsInner.ResumeLayout(false);
			this.pnlWordForming.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.chrGridWordForming)).EndInit();
			this.pnlOther.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.chrGridOther)).EndInit();
			this.pnlMoveButtons.ResumeLayout(false);
			this.pnlNumbers.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.chrGridNumbers)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button btnOk;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.Button btnHelp;
		private System.Windows.Forms.TabControl tabCtrlAddFrom;
		private System.Windows.Forms.TabPage tabBasedOn;
		private System.Windows.Forms.TabPage tabManual;
		private System.Windows.Forms.Button btnRemoveChar;
		private System.Windows.Forms.ComboBox cboSortOrder;
		private System.Windows.Forms.Label lblValidChars;
		private System.Windows.Forms.TabPage tabData;
		private System.Windows.Forms.TabPage tabUnicode;
		private SIL.FieldWorks.Common.Controls.CharacterGrid chrGridWordForming;
		private System.Windows.Forms.TextBox txtLanguageFile;
		private System.Windows.Forms.RadioButton rdoLanguageFile;
		private System.Windows.Forms.RadioButton rdoSimilarWs;
		private System.Windows.Forms.Button btnBrowseLangFile;
		private System.Windows.Forms.Button btnRemoveAll;
		private System.Windows.Forms.Button btnAddCharacters;
		private System.Windows.Forms.Panel panel1;
		private SIL.FieldWorks.Common.Widgets.FwTextBox txtManualCharEntry;
		private System.Windows.Forms.Label lblSingle;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.DataGridView gridCharInventory;
		private System.Windows.Forms.SplitContainer splitContainerOuter;
		private System.Windows.Forms.Panel panel2;
		private System.Windows.Forms.Label lblRangeMsg;
		private SIL.FieldWorks.Common.Widgets.FwTextBox txtLastChar;
		private SIL.FieldWorks.Common.Widgets.FwTextBox txtFirstChar;
		private System.Windows.Forms.GroupBox grpSingle;
		private System.Windows.Forms.Label lblFirstChar;
		private System.Windows.Forms.Label lblLastChar;
		private System.Windows.Forms.GroupBox grpCharRange;
		private System.Windows.Forms.Label lblFirstCharCode;
		private System.Windows.Forms.Label lblLastCharCode;
		private System.Windows.Forms.RadioButton rbSingleChar;
		private System.Windows.Forms.RadioButton rbCharRange;
		private SIL.FieldWorks.FwCoreDlgControls.LocaleMenuButton btnSimilarWs;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn4;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn5;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn6;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn7;
		private CharContextCtrl contextCtrl;
		private System.Windows.Forms.Label m_lblWsName;
		private System.Windows.Forms.DataGridViewTextBoxColumn colChar;
		private System.Windows.Forms.DataGridViewTextBoxColumn colCharCode;
		private System.Windows.Forms.DataGridViewTextBoxColumn colCount;
		private System.Windows.Forms.DataGridViewCheckBoxColumn colStatus;
		private SIL.FieldWorks.Common.Controls.CharacterGrid chrGridOther;
		private SIL.FieldWorks.Common.Controls.CharacterGrid chrGridNumbers;
		private System.Windows.Forms.RadioButton rbUnicodeValue;
		private System.Windows.Forms.GroupBox grpUnicodeValue;
		private System.Windows.Forms.Label lblUnicodeValue;
		private SIL.FieldWorks.Common.Widgets.FwTextBox txtUnicodeValue;
		private System.Windows.Forms.Label label4;
		private SIL.FieldWorks.Common.Controls.FwPanel pnlWordForming;
		private SIL.FieldWorks.Common.Controls.FwPanel pnlOther;
		private SIL.FieldWorks.Common.Controls.HeaderLabel hlblOther;
		private SIL.FieldWorks.Common.Controls.HeaderLabel hlblWordForming;
		private SIL.FieldWorks.Common.Controls.FwPanel pnlNumbers;
		private SIL.FieldWorks.Common.Controls.HeaderLabel hlblNumbers;
		private System.Windows.Forms.Button btnTreatAsPunct;
		private System.Windows.Forms.Button btnTreatAsWrdForming;
		private System.Windows.Forms.SplitContainer splitValidCharsOuter;
		private System.Windows.Forms.SplitContainer splitValidCharsInner;
		private System.Windows.Forms.Panel pnlMoveButtons;
		private System.Windows.Forms.ToolTip m_tooltip;
	}
}

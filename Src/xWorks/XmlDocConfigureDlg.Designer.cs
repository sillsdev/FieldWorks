using System.Diagnostics.CodeAnalysis;

namespace SIL.FieldWorks.XWorks
{
	partial class XmlDocConfigureDlg
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
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (disposing && (components != null))
			{
				m_cfgParentNode.ConfigureNowClicked -= m_lnkConfigureNow_LinkClicked;
				m_cfgSenses.SensesBtnClicked -= m_cfgSenses_SensesBtnClicked;
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification = "TODO-Linux: LinkLabel.TabStop is missing from Mono")]
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(XmlDocConfigureDlg));
			this.label2 = new System.Windows.Forms.Label();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.m_tvParts = new System.Windows.Forms.TreeView();
			this.m_btnMoveUp = new System.Windows.Forms.Button();
			this.m_btnMoveDown = new System.Windows.Forms.Button();
			this.m_btnDuplicate = new System.Windows.Forms.Button();
			this.m_btnRemove = new System.Windows.Forms.Button();
			this.m_btnRestoreDefaults = new System.Windows.Forms.Button();
			this.m_btnOK = new System.Windows.Forms.Button();
			this.m_btnCancel = new System.Windows.Forms.Button();
			this.m_btnHelp = new System.Windows.Forms.Button();
			this.panel1 = new System.Windows.Forms.Panel();
			this.m_linkConfigureHomograph = new System.Windows.Forms.LinkLabel();
			this.m_cfgSenses = new SIL.FieldWorks.FwCoreDlgControls.ConfigSenseLayout();
			m_cfgSenses.SizeChanged += new System.EventHandler(m_cfgSenses_SizeChanged);
			this.m_cfgParentNode = new SIL.FieldWorks.FwCoreDlgControls.ConfigParentNode();
			this.m_btnBeforeStyles = new System.Windows.Forms.Button();
			this.m_lblBeforeStyle = new System.Windows.Forms.Label();
			this.m_cbBeforeStyle = new System.Windows.Forms.ComboBox();
			this.m_chkComplexFormsAsParagraphs = new System.Windows.Forms.CheckBox();
			this.m_chkShowSingleGramInfoFirst = new System.Windows.Forms.CheckBox();
			this.m_tbPanelHeader = new System.Windows.Forms.TextBox();
			this.m_btnMoveItemDown = new System.Windows.Forms.Button();
			this.m_btnMoveItemUp = new System.Windows.Forms.Button();
			this.m_lvItems = new System.Windows.Forms.ListView();
			this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
			this.m_lblItemsList = new System.Windows.Forms.Label();
			this.m_btnStyles = new System.Windows.Forms.Button();
			this.m_lblCharStyle = new System.Windows.Forms.Label();
			this.m_cbCharStyle = new System.Windows.Forms.ComboBox();
			this.m_lblContext = new System.Windows.Forms.Label();
			this.m_lblAfter = new System.Windows.Forms.Label();
			this.m_lblBetween = new System.Windows.Forms.Label();
			this.m_lblBefore = new System.Windows.Forms.Label();
			this.m_tbAfter = new System.Windows.Forms.TextBox();
			this.m_tbBetween = new System.Windows.Forms.TextBox();
			this.m_tbBefore = new System.Windows.Forms.TextBox();
			this.m_chkDisplayWsAbbrs = new System.Windows.Forms.CheckBox();
			this.m_chkDisplayData = new System.Windows.Forms.CheckBox();
			this.m_lblPanel = new System.Windows.Forms.Label();
			this.m_lblViewType = new System.Windows.Forms.Label();
			this.m_cbDictType = new System.Windows.Forms.ComboBox();
			this.m_btnSetAll = new System.Windows.Forms.Button();
			this.m_linkManageViews = new System.Windows.Forms.LinkLabel();
			this.panel1.SuspendLayout();
			this.SuspendLayout();
			//
			// label2
			//
			resources.ApplyResources(this.label2, "label2");
			this.label2.ForeColor = System.Drawing.Color.Blue;
			this.label2.Name = "label2";
			//
			// groupBox1
			//
			resources.ApplyResources(this.groupBox1, "groupBox1");
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.TabStop = false;
			//
			// m_tvParts
			//
			resources.ApplyResources(this.m_tvParts, "m_tvParts");
			this.m_tvParts.CheckBoxes = true;
			this.m_tvParts.HideSelection = false;
			this.m_tvParts.Name = "m_tvParts";
			this.m_tvParts.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.m_tvParts_AfterSelect);
			//
			// m_btnMoveUp
			//
			resources.ApplyResources(this.m_btnMoveUp, "m_btnMoveUp");
			this.m_btnMoveUp.Name = "m_btnMoveUp";
			this.m_btnMoveUp.Image = Resources.Images.arrowup;
			this.m_btnMoveUp.UseVisualStyleBackColor = true;
			this.m_btnMoveUp.Click += new System.EventHandler(this.m_btnMoveUp_Click);
			//
			// m_btnMoveDown
			//
			resources.ApplyResources(this.m_btnMoveDown, "m_btnMoveDown");
			this.m_btnMoveDown.Name = "m_btnMoveDown";
			this.m_btnMoveDown.UseVisualStyleBackColor = true;
			this.m_btnMoveDown.Image = Resources.Images.arrowdown;
			this.m_btnMoveDown.Click += new System.EventHandler(this.m_btnMoveDown_Click);
			//
			// m_btnDuplicate
			//
			resources.ApplyResources(this.m_btnDuplicate, "m_btnDuplicate");
			this.m_btnDuplicate.Name = "m_btnDuplicate";
			this.m_btnDuplicate.UseVisualStyleBackColor = true;
			this.m_btnDuplicate.Click += new System.EventHandler(this.OnDuplicateClick);
			//
			// m_btnRemove
			//
			resources.ApplyResources(this.m_btnRemove, "m_btnRemove");
			this.m_btnRemove.Name = "m_btnRemove";
			this.m_btnRemove.UseVisualStyleBackColor = true;
			this.m_btnRemove.Click += new System.EventHandler(this.m_btnRemove_Click);
			//
			// m_btnRestoreDefaults
			//
			resources.ApplyResources(this.m_btnRestoreDefaults, "m_btnRestoreDefaults");
			this.m_btnRestoreDefaults.Name = "m_btnRestoreDefaults";
			this.m_btnRestoreDefaults.UseVisualStyleBackColor = true;
			this.m_btnRestoreDefaults.Click += new System.EventHandler(this.m_btnRestoreDefaults_Click);
			//
			// m_btnOK
			//
			resources.ApplyResources(this.m_btnOK, "m_btnOK");
			this.m_btnOK.Name = "m_btnOK";
			this.m_btnOK.UseVisualStyleBackColor = true;
			this.m_btnOK.Click += new System.EventHandler(this.m_btnOK_Click);
			//
			// m_btnCancel
			//
			resources.ApplyResources(this.m_btnCancel, "m_btnCancel");
			this.m_btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btnCancel.Name = "m_btnCancel";
			this.m_btnCancel.UseVisualStyleBackColor = true;
			//
			// m_btnHelp
			//
			resources.ApplyResources(this.m_btnHelp, "m_btnHelp");
			this.m_btnHelp.Name = "m_btnHelp";
			this.m_btnHelp.UseVisualStyleBackColor = true;
			this.m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// panel1
			//
			resources.ApplyResources(this.panel1, "panel1");
			this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.panel1.Controls.Add(this.m_linkConfigureHomograph);
			this.panel1.Controls.Add(this.m_cfgSenses);
			this.panel1.Controls.Add(this.m_cfgParentNode);
			this.panel1.Controls.Add(this.m_btnBeforeStyles);
			this.panel1.Controls.Add(this.m_lblBeforeStyle);
			this.panel1.Controls.Add(this.m_cbBeforeStyle);
			this.panel1.Controls.Add(this.m_chkComplexFormsAsParagraphs);
			this.panel1.Controls.Add(this.m_chkShowSingleGramInfoFirst);
			this.panel1.Controls.Add(this.m_tbPanelHeader);
			this.panel1.Controls.Add(this.m_btnMoveItemDown);
			this.panel1.Controls.Add(this.m_btnMoveItemUp);
			this.panel1.Controls.Add(this.m_lvItems);
			this.panel1.Controls.Add(this.m_lblItemsList);
			this.panel1.Controls.Add(this.m_btnStyles);
			this.panel1.Controls.Add(this.m_lblCharStyle);
			this.panel1.Controls.Add(this.m_cbCharStyle);
			this.panel1.Controls.Add(this.m_lblContext);
			this.panel1.Controls.Add(this.m_lblAfter);
			this.panel1.Controls.Add(this.m_lblBetween);
			this.panel1.Controls.Add(this.m_lblBefore);
			this.panel1.Controls.Add(this.m_tbAfter);
			this.panel1.Controls.Add(this.m_tbBetween);
			this.panel1.Controls.Add(this.m_tbBefore);
			this.panel1.Controls.Add(this.m_chkDisplayWsAbbrs);
			this.panel1.Controls.Add(this.m_chkDisplayData);
			this.panel1.Controls.Add(this.m_lblPanel);
			this.panel1.MaximumSize = new System.Drawing.Size(362, 1009);
			this.panel1.MinimumSize = new System.Drawing.Size(362, 440);
			this.panel1.Name = "panel1";
			//
			// m_linkConfigureHomograph
			//
			resources.ApplyResources(this.m_linkConfigureHomograph, "m_linkConfigureHomograph");
			this.m_linkConfigureHomograph.Name = "m_linkConfigureHomograph";
			this.m_linkConfigureHomograph.TabStop = true;
			this.m_linkConfigureHomograph.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.m_linkConfigureHomograph_LinkClicked);
			//
			// m_cfgSenses
			//
			resources.ApplyResources(this.m_cfgSenses, "m_cfgSenses");
			this.m_cfgSenses.AfterNumber = "";
			this.m_cfgSenses.BeforeNumber = "";
			this.m_cfgSenses.BoldSenseNumber = System.Windows.Forms.CheckState.Indeterminate;
			//this.m_cfgSenses.EmbedSenses = true;
			this.m_cfgSenses.ItalicSenseNumber = System.Windows.Forms.CheckState.Indeterminate;
			this.m_cfgSenses.Name = "m_cfgSenses";
			this.m_cfgSenses.NumberSingleSense = false;
			this.m_cfgSenses.SenseParaStyle = null;
			//this.m_cfgSenses.SingleSenseStyle = null;
			//
			// m_cfgParentNode
			//
			resources.ApplyResources(this.m_cfgParentNode, "m_cfgParentNode");
			this.m_cfgParentNode.Name = "m_cfgParentNode";
			//
			// m_btnBeforeStyles
			//
			resources.ApplyResources(this.m_btnBeforeStyles, "m_btnBeforeStyles");
			this.m_btnBeforeStyles.Name = "m_btnBeforeStyles";
			this.m_btnBeforeStyles.UseVisualStyleBackColor = true;
			this.m_btnBeforeStyles.Click += new System.EventHandler(this.m_btnBeforeStyles_Click);
			//
			// m_lblBeforeStyle
			//
			resources.ApplyResources(this.m_lblBeforeStyle, "m_lblBeforeStyle");
			this.m_lblBeforeStyle.Name = "m_lblBeforeStyle";
			//
			// m_cbBeforeStyle
			//
			resources.ApplyResources(this.m_cbBeforeStyle, "m_cbBeforeStyle");
			this.m_cbBeforeStyle.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cbBeforeStyle.FormattingEnabled = true;
			this.m_cbBeforeStyle.Name = "m_cbBeforeStyle";
			//
			// m_chkComplexFormsAsParagraphs
			//
			resources.ApplyResources(this.m_chkComplexFormsAsParagraphs, "m_chkComplexFormsAsParagraphs");
			this.m_chkComplexFormsAsParagraphs.Name = "m_chkComplexFormsAsParagraphs";
			this.m_chkComplexFormsAsParagraphs.UseVisualStyleBackColor = true;
			//
			// m_chkShowSingleGramInfoFirst
			//
			resources.ApplyResources(this.m_chkShowSingleGramInfoFirst, "m_chkShowSingleGramInfoFirst");
			this.m_chkShowSingleGramInfoFirst.Name = "m_chkShowSingleGramInfoFirst";
			this.m_chkShowSingleGramInfoFirst.UseVisualStyleBackColor = true;
			//
			// m_tbPanelHeader
			//
			this.m_tbPanelHeader.BackColor = System.Drawing.SystemColors.Control;
			this.m_tbPanelHeader.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.m_tbPanelHeader.CausesValidation = false;
			resources.ApplyResources(this.m_tbPanelHeader, "m_tbPanelHeader");
			this.m_tbPanelHeader.Name = "m_tbPanelHeader";
			this.m_tbPanelHeader.ReadOnly = true;
			this.m_tbPanelHeader.TabStop = false;
			//
			// m_btnMoveItemDown
			//
			resources.ApplyResources(this.m_btnMoveItemDown, "m_btnMoveItemDown");
			this.m_btnMoveItemDown.Name = "m_btnMoveItemDown";
			this.m_btnMoveItemDown.Image = Resources.Images.arrowdown;
			this.m_btnMoveItemDown.UseVisualStyleBackColor = true;
			//
			// m_btnMoveItemUp
			//
			resources.ApplyResources(this.m_btnMoveItemUp, "m_btnMoveItemUp");
			this.m_btnMoveItemUp.Name = "m_btnMoveItemUp";
			this.m_btnMoveItemUp.Image = Resources.Images.arrowup;
			this.m_btnMoveItemUp.UseVisualStyleBackColor = true;
			//
			// m_lvItems
			//
			resources.ApplyResources(this.m_lvItems, "m_lvItems");
			this.m_lvItems.Activation = System.Windows.Forms.ItemActivation.OneClick;
			this.m_lvItems.AutoArrange = false;
			this.m_lvItems.CheckBoxes = true;
			this.m_lvItems.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
			this.columnHeader1});
			this.m_lvItems.FullRowSelect = true;
			this.m_lvItems.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
			this.m_lvItems.HideSelection = false;
			this.m_lvItems.MultiSelect = false;
			this.m_lvItems.Name = "m_lvItems";
			this.m_lvItems.ShowGroups = false;
			this.m_lvItems.UseCompatibleStateImageBehavior = false;
			this.m_lvItems.View = System.Windows.Forms.View.Details;
			//
			// columnHeader1
			//
			resources.ApplyResources(this.columnHeader1, "columnHeader1");
			//
			// m_lblItemsList
			//
			resources.ApplyResources(this.m_lblItemsList, "m_lblItemsList");
			this.m_lblItemsList.Name = "m_lblItemsList";
			//
			// m_btnStyles
			//
			resources.ApplyResources(this.m_btnStyles, "m_btnStyles");
			this.m_btnStyles.Name = "m_btnStyles";
			this.m_btnStyles.UseVisualStyleBackColor = true;
			this.m_btnStyles.Click += new System.EventHandler(this.m_btnStyles_Click);
			//
			// m_lblCharStyle
			//
			resources.ApplyResources(this.m_lblCharStyle, "m_lblCharStyle");
			this.m_lblCharStyle.Name = "m_lblCharStyle";
			//
			// m_cbCharStyle
			//
			resources.ApplyResources(this.m_cbCharStyle, "m_cbCharStyle");
			this.m_cbCharStyle.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cbCharStyle.FormattingEnabled = true;
			this.m_cbCharStyle.Name = "m_cbCharStyle";
			//
			// m_lblContext
			//
			resources.ApplyResources(this.m_lblContext, "m_lblContext");
			this.m_lblContext.Name = "m_lblContext";
			//
			// m_lblAfter
			//
			resources.ApplyResources(this.m_lblAfter, "m_lblAfter");
			this.m_lblAfter.Name = "m_lblAfter";
			//
			// m_lblBetween
			//
			resources.ApplyResources(this.m_lblBetween, "m_lblBetween");
			this.m_lblBetween.Name = "m_lblBetween";
			//
			// m_lblBefore
			//
			resources.ApplyResources(this.m_lblBefore, "m_lblBefore");
			this.m_lblBefore.Name = "m_lblBefore";
			//
			// m_tbAfter
			//
			resources.ApplyResources(this.m_tbAfter, "m_tbAfter");
			this.m_tbAfter.HideSelection = false;
			this.m_tbAfter.Name = "m_tbAfter";
			//
			// m_tbBetween
			//
			resources.ApplyResources(this.m_tbBetween, "m_tbBetween");
			this.m_tbBetween.HideSelection = false;
			this.m_tbBetween.Name = "m_tbBetween";
			//
			// m_tbBefore
			//
			resources.ApplyResources(this.m_tbBefore, "m_tbBefore");
			this.m_tbBefore.ForeColor = System.Drawing.SystemColors.WindowText;
			this.m_tbBefore.HideSelection = false;
			this.m_tbBefore.Name = "m_tbBefore";
			//
			// m_chkDisplayWsAbbrs
			//
			resources.ApplyResources(this.m_chkDisplayWsAbbrs, "m_chkDisplayWsAbbrs");
			this.m_chkDisplayWsAbbrs.Name = "m_chkDisplayWsAbbrs";
			this.m_chkDisplayWsAbbrs.UseVisualStyleBackColor = true;
			//
			// m_chkDisplayData
			//
			resources.ApplyResources(this.m_chkDisplayData, "m_chkDisplayData");
			this.m_chkDisplayData.Name = "m_chkDisplayData";
			this.m_chkDisplayData.UseVisualStyleBackColor = true;
			this.m_chkDisplayData.CheckedChanged += new System.EventHandler(this.m_chkDisplayData_CheckedChanged);
			//
			// m_lblPanel
			//
			resources.ApplyResources(this.m_lblPanel, "m_lblPanel");
			this.m_lblPanel.Name = "m_lblPanel";
			//
			// m_lblViewType
			//
			resources.ApplyResources(this.m_lblViewType, "m_lblViewType");
			this.m_lblViewType.Name = "m_lblViewType";
			//
			// m_cbDictType
			//
			resources.ApplyResources(this.m_cbDictType, "m_cbDictType");
			this.m_cbDictType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cbDictType.FormattingEnabled = true;
			this.m_cbDictType.Name = "m_cbDictType";
			this.m_cbDictType.SelectedIndexChanged += new System.EventHandler(this.m_cbDictType_SelectedIndexChanged);
			//
			// m_btnSetAll
			//
			resources.ApplyResources(this.m_btnSetAll, "m_btnSetAll");
			this.m_btnSetAll.Name = "m_btnSetAll";
			this.m_btnSetAll.TabStop = false;
			this.m_btnSetAll.UseVisualStyleBackColor = true;
			this.m_btnSetAll.Click += new System.EventHandler(this.m_btnSetAll_Click);
			//
			// m_linkManageViews
			//
			resources.ApplyResources(this.m_linkManageViews, "m_linkManageViews");
			this.m_linkManageViews.Name = "m_linkManageViews";
			this.m_linkManageViews.TabStop = true;
			this.m_linkManageViews.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.m_linkManageViews_LinkClicked);
			//
			// XmlDocConfigureDlg
			//
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btnCancel;
			this.Controls.Add(this.m_linkManageViews);
			this.Controls.Add(this.m_btnSetAll);
			this.Controls.Add(this.m_cbDictType);
			this.Controls.Add(this.m_lblViewType);
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.m_btnHelp);
			this.Controls.Add(this.m_btnCancel);
			this.Controls.Add(this.m_btnOK);
			this.Controls.Add(this.m_btnRestoreDefaults);
			this.Controls.Add(this.m_btnRemove);
			this.Controls.Add(this.m_btnDuplicate);
			this.Controls.Add(this.m_btnMoveDown);
			this.Controls.Add(this.m_btnMoveUp);
			this.Controls.Add(this.m_tvParts);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.label2);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "XmlDocConfigureDlg";
			this.ShowIcon = false;
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.XmlDocConfigureDlg_FormClosed);
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		void m_cfgSenses_SizeChanged(object sender, System.EventArgs e)
		{

		}

		#endregion

		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.TreeView m_tvParts;
		private System.Windows.Forms.Button m_btnMoveUp;
		private System.Windows.Forms.Button m_btnMoveDown;
		private System.Windows.Forms.Button m_btnDuplicate;
		private System.Windows.Forms.Button m_btnRemove;
		private System.Windows.Forms.Button m_btnRestoreDefaults;
		private System.Windows.Forms.Button m_btnOK;
		private System.Windows.Forms.Button m_btnCancel;
		private System.Windows.Forms.Button m_btnHelp;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.CheckBox m_chkDisplayData;
		private System.Windows.Forms.Label m_lblPanel;
		private System.Windows.Forms.TextBox m_tbBefore;
		private System.Windows.Forms.CheckBox m_chkDisplayWsAbbrs;
		private System.Windows.Forms.Label m_lblAfter;
		private System.Windows.Forms.Label m_lblBetween;
		private System.Windows.Forms.Label m_lblBefore;
		private System.Windows.Forms.TextBox m_tbAfter;
		private System.Windows.Forms.TextBox m_tbBetween;
		private System.Windows.Forms.Label m_lblContext;
		private System.Windows.Forms.Label m_lblCharStyle;
		private System.Windows.Forms.ComboBox m_cbCharStyle;
		private System.Windows.Forms.Button m_btnStyles;
		private System.Windows.Forms.Label m_lblItemsList;
		private System.Windows.Forms.ListView m_lvItems;
		private System.Windows.Forms.Button m_btnMoveItemDown;
		private System.Windows.Forms.Button m_btnMoveItemUp;
		private System.Windows.Forms.Label m_lblViewType;
		private System.Windows.Forms.ComboBox m_cbDictType;
		private System.Windows.Forms.Button m_btnSetAll;
		private System.Windows.Forms.TextBox m_tbPanelHeader;
		private System.Windows.Forms.CheckBox m_chkShowSingleGramInfoFirst;
		private System.Windows.Forms.CheckBox m_chkComplexFormsAsParagraphs;
		private System.Windows.Forms.ColumnHeader columnHeader1;
		private System.Windows.Forms.Button m_btnBeforeStyles;
		private System.Windows.Forms.Label m_lblBeforeStyle;
		private System.Windows.Forms.ComboBox m_cbBeforeStyle;
		private SIL.FieldWorks.FwCoreDlgControls.ConfigParentNode m_cfgParentNode;
		private SIL.FieldWorks.FwCoreDlgControls.ConfigSenseLayout m_cfgSenses;
		private System.Windows.Forms.LinkLabel m_linkManageViews;
		private System.Windows.Forms.LinkLabel m_linkConfigureHomograph;
	}
}

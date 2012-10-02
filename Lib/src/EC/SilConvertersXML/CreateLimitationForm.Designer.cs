namespace SilConvertersXML
{
	partial class CreateLimitationForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CreateLimitationForm));
			this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.labelAttrName = new System.Windows.Forms.Label();
			this.labelXPath = new System.Windows.Forms.Label();
			this.labelFilter = new System.Windows.Forms.Label();
			this.textBoxFilter = new System.Windows.Forms.TextBox();
			this.textBoxName = new System.Windows.Forms.TextBox();
			this.textBoxXPath = new System.Windows.Forms.TextBox();
			this.flowLayoutPanelConstraintType = new System.Windows.Forms.FlowLayoutPanel();
			this.radioButtonPresenceOnly = new System.Windows.Forms.RadioButton();
			this.radioButtonSpecificValue = new System.Windows.Forms.RadioButton();
			this.radioButtonPreviousConstraint = new System.Windows.Forms.RadioButton();
			this.radioButtonManuallyEntered = new System.Windows.Forms.RadioButton();
			this.radioButtonAbsence = new System.Windows.Forms.RadioButton();
			this.labelConstraintType = new System.Windows.Forms.Label();
			this.checkedListBox = new System.Windows.Forms.CheckedListBox();
			this.buttonOK = new System.Windows.Forms.Button();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.helpProvider = new System.Windows.Forms.HelpProvider();
			this.toolTip = new System.Windows.Forms.ToolTip(this.components);
			this.tableLayoutPanel.SuspendLayout();
			this.flowLayoutPanelConstraintType.SuspendLayout();
			this.SuspendLayout();
			//
			// tableLayoutPanel
			//
			this.tableLayoutPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.tableLayoutPanel.ColumnCount = 4;
			this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 60F));
			this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
			this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel.Controls.Add(this.labelAttrName, 0, 1);
			this.tableLayoutPanel.Controls.Add(this.labelXPath, 0, 0);
			this.tableLayoutPanel.Controls.Add(this.labelFilter, 0, 4);
			this.tableLayoutPanel.Controls.Add(this.textBoxFilter, 1, 4);
			this.tableLayoutPanel.Controls.Add(this.textBoxName, 1, 1);
			this.tableLayoutPanel.Controls.Add(this.textBoxXPath, 1, 0);
			this.tableLayoutPanel.Controls.Add(this.flowLayoutPanelConstraintType, 1, 2);
			this.tableLayoutPanel.Controls.Add(this.labelConstraintType, 0, 2);
			this.tableLayoutPanel.Controls.Add(this.checkedListBox, 1, 3);
			this.tableLayoutPanel.Location = new System.Drawing.Point(0, 0);
			this.tableLayoutPanel.Name = "tableLayoutPanel";
			this.tableLayoutPanel.RowCount = 5;
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 60F));
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel.Size = new System.Drawing.Size(541, 388);
			this.tableLayoutPanel.TabIndex = 0;
			//
			// labelAttrName
			//
			this.labelAttrName.Anchor = System.Windows.Forms.AnchorStyles.Right;
			this.labelAttrName.AutoSize = true;
			this.labelAttrName.Location = new System.Drawing.Point(4, 32);
			this.labelAttrName.Name = "labelAttrName";
			this.labelAttrName.Size = new System.Drawing.Size(38, 13);
			this.labelAttrName.TabIndex = 2;
			this.labelAttrName.Text = "&Name:";
			//
			// labelXPath
			//
			this.labelXPath.Anchor = System.Windows.Forms.AnchorStyles.Right;
			this.labelXPath.AutoSize = true;
			this.labelXPath.Location = new System.Drawing.Point(3, 6);
			this.labelXPath.Name = "labelXPath";
			this.labelXPath.Size = new System.Drawing.Size(39, 13);
			this.labelXPath.TabIndex = 5;
			this.labelXPath.Text = "&XPath:";
			//
			// labelFilter
			//
			this.labelFilter.Anchor = System.Windows.Forms.AnchorStyles.Right;
			this.labelFilter.AutoSize = true;
			this.labelFilter.Location = new System.Drawing.Point(10, 368);
			this.labelFilter.Name = "labelFilter";
			this.labelFilter.Size = new System.Drawing.Size(32, 13);
			this.labelFilter.TabIndex = 10;
			this.labelFilter.Text = "Filter:";
			//
			// textBoxFilter
			//
			this.tableLayoutPanel.SetColumnSpan(this.textBoxFilter, 3);
			this.textBoxFilter.Dock = System.Windows.Forms.DockStyle.Fill;
			this.helpProvider.SetHelpString(this.textBoxFilter, "This box shows the syntax of the filter based on the configured items above");
			this.textBoxFilter.Location = new System.Drawing.Point(48, 365);
			this.textBoxFilter.Name = "textBoxFilter";
			this.textBoxFilter.ReadOnly = true;
			this.helpProvider.SetShowHelp(this.textBoxFilter, true);
			this.textBoxFilter.Size = new System.Drawing.Size(490, 20);
			this.textBoxFilter.TabIndex = 11;
			this.toolTip.SetToolTip(this.textBoxFilter, "This box shows the filter syntax of the selected item");
			//
			// textBoxName
			//
			this.tableLayoutPanel.SetColumnSpan(this.textBoxName, 3);
			this.textBoxName.Dock = System.Windows.Forms.DockStyle.Fill;
			this.helpProvider.SetHelpString(this.textBoxName, "This box shows the name of the constraining item (attribute or element)");
			this.textBoxName.Location = new System.Drawing.Point(48, 29);
			this.textBoxName.Name = "textBoxName";
			this.textBoxName.ReadOnly = true;
			this.helpProvider.SetShowHelp(this.textBoxName, true);
			this.textBoxName.Size = new System.Drawing.Size(490, 20);
			this.textBoxName.TabIndex = 3;
			this.toolTip.SetToolTip(this.textBoxName, "The element or attribute name whose presence will be required by this filter");
			//
			// textBoxXPath
			//
			this.tableLayoutPanel.SetColumnSpan(this.textBoxXPath, 3);
			this.textBoxXPath.Dock = System.Windows.Forms.DockStyle.Fill;
			this.helpProvider.SetHelpString(this.textBoxXPath, "This box shows the XPath syntax for the node in the XML schema that you selected");
			this.textBoxXPath.Location = new System.Drawing.Point(48, 3);
			this.textBoxXPath.Name = "textBoxXPath";
			this.textBoxXPath.ReadOnly = true;
			this.helpProvider.SetShowHelp(this.textBoxXPath, true);
			this.textBoxXPath.Size = new System.Drawing.Size(490, 20);
			this.textBoxXPath.TabIndex = 4;
			this.toolTip.SetToolTip(this.textBoxXPath, "The XPath value to the checked node");
			//
			// flowLayoutPanelConstraintType
			//
			this.tableLayoutPanel.SetColumnSpan(this.flowLayoutPanelConstraintType, 3);
			this.flowLayoutPanelConstraintType.Controls.Add(this.radioButtonPresenceOnly);
			this.flowLayoutPanelConstraintType.Controls.Add(this.radioButtonSpecificValue);
			this.flowLayoutPanelConstraintType.Controls.Add(this.radioButtonPreviousConstraint);
			this.flowLayoutPanelConstraintType.Controls.Add(this.radioButtonManuallyEntered);
			this.flowLayoutPanelConstraintType.Controls.Add(this.radioButtonAbsence);
			this.flowLayoutPanelConstraintType.Dock = System.Windows.Forms.DockStyle.Fill;
			this.flowLayoutPanelConstraintType.Location = new System.Drawing.Point(48, 55);
			this.flowLayoutPanelConstraintType.Name = "flowLayoutPanelConstraintType";
			this.flowLayoutPanelConstraintType.Size = new System.Drawing.Size(490, 54);
			this.flowLayoutPanelConstraintType.TabIndex = 13;
			//
			// radioButtonPresenceOnly
			//
			this.radioButtonPresenceOnly.AutoSize = true;
			this.radioButtonPresenceOnly.Location = new System.Drawing.Point(3, 3);
			this.radioButtonPresenceOnly.Name = "radioButtonPresenceOnly";
			this.radioButtonPresenceOnly.Size = new System.Drawing.Size(70, 17);
			this.radioButtonPresenceOnly.TabIndex = 0;
			this.radioButtonPresenceOnly.TabStop = true;
			this.radioButtonPresenceOnly.Text = "&Presence";
			this.toolTip.SetToolTip(this.radioButtonPresenceOnly, "Filter based on the presence of the item (i.e. records which have the item (attri" +
					"bute or element) will match the filter constraint)");
			this.radioButtonPresenceOnly.UseVisualStyleBackColor = true;
			this.radioButtonPresenceOnly.CheckedChanged += new System.EventHandler(this.radioButton_CheckedChanged);
			//
			// radioButtonSpecificValue
			//
			this.radioButtonSpecificValue.AutoSize = true;
			this.radioButtonSpecificValue.Location = new System.Drawing.Point(79, 3);
			this.radioButtonSpecificValue.Name = "radioButtonSpecificValue";
			this.radioButtonSpecificValue.Size = new System.Drawing.Size(137, 17);
			this.radioButtonSpecificValue.TabIndex = 1;
			this.radioButtonSpecificValue.TabStop = true;
			this.radioButtonSpecificValue.Text = "&Limit to specific value(s)";
			this.toolTip.SetToolTip(this.radioButtonSpecificValue, "Filter based on a specific value of the item (i.e. match records for which the at" +
					"tribute or element has the specific value(s) chosen from the list below)");
			this.radioButtonSpecificValue.UseVisualStyleBackColor = true;
			this.radioButtonSpecificValue.CheckedChanged += new System.EventHandler(this.radioButtonSpecificValue_CheckedChanged);
			//
			// radioButtonPreviousConstraint
			//
			this.radioButtonPreviousConstraint.AutoSize = true;
			this.radioButtonPreviousConstraint.Location = new System.Drawing.Point(222, 3);
			this.radioButtonPreviousConstraint.Name = "radioButtonPreviousConstraint";
			this.radioButtonPreviousConstraint.Size = new System.Drawing.Size(115, 17);
			this.radioButtonPreviousConstraint.TabIndex = 2;
			this.radioButtonPreviousConstraint.TabStop = true;
			this.radioButtonPreviousConstraint.Text = "Previous &constraint";
			this.toolTip.SetToolTip(this.radioButtonPreviousConstraint, "Use a previously created constraint (no checking is done, though, so make sure it" +
					" makes sense)");
			this.radioButtonPreviousConstraint.UseVisualStyleBackColor = true;
			this.radioButtonPreviousConstraint.CheckedChanged += new System.EventHandler(this.radioButtonPreviousConstraint_CheckedChanged);
			//
			// radioButtonManuallyEntered
			//
			this.radioButtonManuallyEntered.AutoSize = true;
			this.radioButtonManuallyEntered.Location = new System.Drawing.Point(343, 3);
			this.radioButtonManuallyEntered.Name = "radioButtonManuallyEntered";
			this.radioButtonManuallyEntered.Size = new System.Drawing.Size(106, 17);
			this.radioButtonManuallyEntered.TabIndex = 3;
			this.radioButtonManuallyEntered.TabStop = true;
			this.radioButtonManuallyEntered.Text = "&Manually entered";
			this.toolTip.SetToolTip(this.radioButtonManuallyEntered, "Use a manually entered constraint (no checking is done, though, so make sure it m" +
					"akes sense)");
			this.radioButtonManuallyEntered.UseVisualStyleBackColor = true;
			this.radioButtonManuallyEntered.CheckedChanged += new System.EventHandler(this.radioButtonManuallyEntered_CheckedChanged);
			//
			// radioButtonAbsence
			//
			this.radioButtonAbsence.AutoSize = true;
			this.radioButtonAbsence.Location = new System.Drawing.Point(3, 26);
			this.radioButtonAbsence.Name = "radioButtonAbsence";
			this.radioButtonAbsence.Size = new System.Drawing.Size(67, 17);
			this.radioButtonAbsence.TabIndex = 4;
			this.radioButtonAbsence.TabStop = true;
			this.radioButtonAbsence.Text = "&Absence";
			this.toolTip.SetToolTip(this.radioButtonAbsence, "Filter based on the absence of the item (i.e. records which lack the item (attrib" +
					"ute or element) will match the filter constraint)");
			this.radioButtonAbsence.UseVisualStyleBackColor = true;
			this.radioButtonAbsence.CheckedChanged += new System.EventHandler(this.radioButton_CheckedChanged);
			//
			// labelConstraintType
			//
			this.labelConstraintType.Anchor = System.Windows.Forms.AnchorStyles.Right;
			this.labelConstraintType.AutoSize = true;
			this.labelConstraintType.Location = new System.Drawing.Point(8, 75);
			this.labelConstraintType.Name = "labelConstraintType";
			this.labelConstraintType.Size = new System.Drawing.Size(34, 13);
			this.labelConstraintType.TabIndex = 14;
			this.labelConstraintType.Text = "&Type:";
			//
			// checkedListBox
			//
			this.checkedListBox.CheckOnClick = true;
			this.tableLayoutPanel.SetColumnSpan(this.checkedListBox, 3);
			this.checkedListBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.checkedListBox.FormattingEnabled = true;
			this.checkedListBox.Location = new System.Drawing.Point(48, 115);
			this.checkedListBox.Name = "checkedListBox";
			this.checkedListBox.Size = new System.Drawing.Size(490, 244);
			this.checkedListBox.TabIndex = 15;
			this.checkedListBox.ThreeDCheckBoxes = true;
			this.checkedListBox.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.checkedListBox_PreviewKeyDown);
			this.checkedListBox.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.checkedListBox_ItemCheck);
			this.checkedListBox.MouseDown += new System.Windows.Forms.MouseEventHandler(this.checkedListBox_MouseDown);
			this.checkedListBox.SelectedValueChanged += new System.EventHandler(this.checkedListBox_SelectedValueChanged);
			//
			// buttonOK
			//
			this.buttonOK.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
			this.buttonOK.AutoSize = true;
			this.buttonOK.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.buttonOK.Location = new System.Drawing.Point(202, 397);
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.Size = new System.Drawing.Size(61, 23);
			this.buttonOK.TabIndex = 0;
			this.buttonOK.Text = "&Add Filter";
			this.buttonOK.UseVisualStyleBackColor = true;
			this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
			//
			// buttonCancel
			//
			this.buttonCancel.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.Location = new System.Drawing.Point(279, 397);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(60, 23);
			this.buttonCancel.TabIndex = 1;
			this.buttonCancel.Text = "Cancel";
			this.buttonCancel.UseVisualStyleBackColor = true;
			this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
			//
			// CreateLimitationForm
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.ClientSize = new System.Drawing.Size(541, 430);
			this.Controls.Add(this.tableLayoutPanel);
			this.Controls.Add(this.buttonOK);
			this.Controls.Add(this.buttonCancel);
			this.HelpButton = true;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "CreateLimitationForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Add Filter";
			this.tableLayoutPanel.ResumeLayout(false);
			this.tableLayoutPanel.PerformLayout();
			this.flowLayoutPanelConstraintType.ResumeLayout(false);
			this.flowLayoutPanelConstraintType.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.Label labelAttrName;
		private System.Windows.Forms.TextBox textBoxName;
		private System.Windows.Forms.TextBox textBoxXPath;
		private System.Windows.Forms.Label labelXPath;
		private System.Windows.Forms.HelpProvider helpProvider;
		private System.Windows.Forms.ToolTip toolTip;
		private System.Windows.Forms.Label labelFilter;
		private System.Windows.Forms.TextBox textBoxFilter;
		private System.Windows.Forms.RadioButton radioButtonPresenceOnly;
		private System.Windows.Forms.RadioButton radioButtonSpecificValue;
		private System.Windows.Forms.FlowLayoutPanel flowLayoutPanelConstraintType;
		private System.Windows.Forms.Label labelConstraintType;
		private System.Windows.Forms.RadioButton radioButtonPreviousConstraint;
		private System.Windows.Forms.RadioButton radioButtonManuallyEntered;
		private System.Windows.Forms.CheckedListBox checkedListBox;
		private System.Windows.Forms.RadioButton radioButtonAbsence;
	}
}
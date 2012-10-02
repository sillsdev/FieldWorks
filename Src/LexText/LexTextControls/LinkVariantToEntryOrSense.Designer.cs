namespace SIL.FieldWorks.LexText.Controls
{
	partial class LinkVariantToEntryOrSense
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LinkVariantToEntryOrSense));
			this.lblCreateEntry = new System.Windows.Forms.Label();
			this.tcVariantTypes = new SIL.FieldWorks.Common.Widgets.TreeCombo();
			this.lblVariantType = new System.Windows.Forms.Label();
			this.panel1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_tbForm)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_fwTextBoxBottomMsg)).BeginInit();
			this.SuspendLayout();
			//
			// grplbl
			//
			resources.ApplyResources(this.grplbl, "grplbl");
			//
			// groupBox1
			//
			this.groupBox1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			resources.ApplyResources(this.groupBox1, "groupBox1");
			//
			// btnClose
			//
			resources.ApplyResources(this.btnClose, "btnClose");
			//
			// btnOK
			//
			resources.ApplyResources(this.btnOK, "btnOK");
			this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
			//
			// btnInsert
			//
			resources.ApplyResources(this.btnInsert, "btnInsert");
			//
			// btnHelp
			//
			resources.ApplyResources(this.btnHelp, "btnHelp");
			//
			// panel1
			//
			resources.ApplyResources(this.panel1, "panel1");
			//
			// matchingEntries
			//
			resources.ApplyResources(this.matchingEntries, "matchingEntries");
			//
			// m_formLabel
			//
			resources.ApplyResources(this.m_formLabel, "m_formLabel");
			//
			// m_tbForm
			//
			resources.ApplyResources(this.m_tbForm, "m_tbForm");
			//
			// m_cbWritingSystems
			//
			resources.ApplyResources(this.m_cbWritingSystems, "m_cbWritingSystems");
			//
			// label1
			//
			resources.ApplyResources(this.label1, "label1");
			//
			// m_fwTextBoxBottomMsg
			//
			resources.ApplyResources(this.m_fwTextBoxBottomMsg, "m_fwTextBoxBottomMsg");
			//
			// label2
			//
			resources.ApplyResources(this.label2, "label2");
			//
			// lblCreateEntry
			//
			resources.ApplyResources(this.lblCreateEntry, "lblCreateEntry");
			this.lblCreateEntry.Name = "lblCreateEntry";
			//
			// tcVariantTypes
			//
			this.tcVariantTypes.AdjustStringHeight = true;
			resources.ApplyResources(this.tcVariantTypes, "tcVariantTypes");
			this.tcVariantTypes.DropDownWidth = 120;
			this.tcVariantTypes.DroppedDown = false;
			this.tcVariantTypes.Name = "tcVariantTypes";
			this.tcVariantTypes.SelectedNode = null;
			this.tcVariantTypes.StyleSheet = null;
			//
			// lblVariantType
			//
			resources.ApplyResources(this.lblVariantType, "lblVariantType");
			this.lblVariantType.Name = "lblVariantType";
			//
			// LinkVariantToEntryOrSense
			//
			resources.ApplyResources(this, "$this");
			this.Controls.Add(this.lblCreateEntry);
			this.Controls.Add(this.tcVariantTypes);
			this.Controls.Add(this.lblVariantType);
			this.helpProvider.SetHelpNavigator(this, ((System.Windows.Forms.HelpNavigator)(resources.GetObject("$this.HelpNavigator"))));
			this.Name = "LinkVariantToEntryOrSense";
			this.helpProvider.SetShowHelp(this, ((bool)(resources.GetObject("$this.ShowHelp"))));
			this.Tag = "";
			this.Controls.SetChildIndex(this.lblVariantType, 0);
			this.Controls.SetChildIndex(this.tcVariantTypes, 0);
			this.Controls.SetChildIndex(this.lblCreateEntry, 0);
			this.Controls.SetChildIndex(this.m_fwTextBoxBottomMsg, 0);
			this.Controls.SetChildIndex(this.btnInsert, 0);
			this.Controls.SetChildIndex(this.grplbl, 0);
			this.Controls.SetChildIndex(this.groupBox1, 0);
			this.Controls.SetChildIndex(this.btnClose, 0);
			this.Controls.SetChildIndex(this.btnOK, 0);
			this.Controls.SetChildIndex(this.btnHelp, 0);
			this.Controls.SetChildIndex(this.panel1, 0);
			this.Controls.SetChildIndex(this.matchingEntries, 0);
			this.Controls.SetChildIndex(this.m_cbWritingSystems, 0);
			this.Controls.SetChildIndex(this.label1, 0);
			this.Controls.SetChildIndex(this.label2, 0);
			this.panel1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_tbForm)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_fwTextBoxBottomMsg)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label lblCreateEntry;
		private SIL.FieldWorks.Common.Widgets.TreeCombo tcVariantTypes;
		private System.Windows.Forms.Label lblVariantType;
	}
}

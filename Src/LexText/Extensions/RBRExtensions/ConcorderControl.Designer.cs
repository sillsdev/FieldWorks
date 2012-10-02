namespace RBRExtensions
{
	partial class ConcorderControl
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
			if (disposing)
			{
				if (components != null)
					components.Dispose();
			}
			m_mediator = null;

			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConcorderControl));
			this.m_lUsedBy = new System.Windows.Forms.Label();
			this.m_lFind = new System.Windows.Forms.Label();
			this.m_cbUsedBy = new System.Windows.Forms.ComboBox();
			this.m_cbFind = new System.Windows.Forms.ComboBox();
			this.m_splitContainer = new System.Windows.Forms.SplitContainer();
			this.panel1 = new System.Windows.Forms.Panel();
			this.panel2 = new System.Windows.Forms.Panel();
			this.m_splitContainer.Panel1.SuspendLayout();
			this.m_splitContainer.Panel2.SuspendLayout();
			this.m_splitContainer.SuspendLayout();
			this.panel1.SuspendLayout();
			this.panel2.SuspendLayout();
			this.SuspendLayout();
			//
			// m_lUsedBy
			//
			resources.ApplyResources(this.m_lUsedBy, "m_lUsedBy");
			this.m_lUsedBy.Name = "m_lUsedBy";
			//
			// m_lFind
			//
			resources.ApplyResources(this.m_lFind, "m_lFind");
			this.m_lFind.Name = "m_lFind";
			//
			// m_cbUsedBy
			//
			this.m_cbUsedBy.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cbUsedBy.FormattingEnabled = true;
			resources.ApplyResources(this.m_cbUsedBy, "m_cbUsedBy");
			this.m_cbUsedBy.Name = "m_cbUsedBy";
			this.m_cbUsedBy.SelectedIndexChanged += new System.EventHandler(this.m_cbUsedBy_SelectedIndexChanged);
			//
			// m_cbFind
			//
			this.m_cbFind.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cbFind.FormattingEnabled = true;
			resources.ApplyResources(this.m_cbFind, "m_cbFind");
			this.m_cbFind.Name = "m_cbFind";
			this.m_cbFind.SelectedIndexChanged += new System.EventHandler(this.m_cbFind_SelectedIndexChanged);
			//
			// m_splitContainer
			//
			resources.ApplyResources(this.m_splitContainer, "m_splitContainer");
			this.m_splitContainer.Name = "m_splitContainer";
			//
			// m_splitContainer.Panel1
			//
			this.m_splitContainer.Panel1.BackColor = System.Drawing.SystemColors.Window;
			this.m_splitContainer.Panel1.Controls.Add(this.panel1);
			//
			// m_splitContainer.Panel2
			//
			this.m_splitContainer.Panel2.BackColor = System.Drawing.SystemColors.Window;
			this.m_splitContainer.Panel2.Controls.Add(this.panel2);
			this.m_splitContainer.TabStop = false;
			//
			// panel1
			//
			this.panel1.BackColor = System.Drawing.SystemColors.Control;
			this.panel1.Controls.Add(this.m_lFind);
			this.panel1.Controls.Add(this.m_cbFind);
			resources.ApplyResources(this.panel1, "panel1");
			this.panel1.Name = "panel1";
			//
			// panel2
			//
			this.panel2.BackColor = System.Drawing.SystemColors.Control;
			this.panel2.Controls.Add(this.m_cbUsedBy);
			this.panel2.Controls.Add(this.m_lUsedBy);
			resources.ApplyResources(this.panel2, "panel2");
			this.panel2.Name = "panel2";
			//
			// ConcorderControl
			//
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.m_splitContainer);
			this.MinimumSize = new System.Drawing.Size(360, 150);
			this.Name = "ConcorderControl";
			this.m_splitContainer.Panel1.ResumeLayout(false);
			this.m_splitContainer.Panel2.ResumeLayout(false);
			this.m_splitContainer.ResumeLayout(false);
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			this.panel2.ResumeLayout(false);
			this.panel2.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.SplitContainer m_splitContainer;
		private System.Windows.Forms.Label m_lUsedBy;
		private System.Windows.Forms.Label m_lFind;
		private System.Windows.Forms.ComboBox m_cbUsedBy;
		private System.Windows.Forms.ComboBox m_cbFind;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Panel panel2;
	}
}

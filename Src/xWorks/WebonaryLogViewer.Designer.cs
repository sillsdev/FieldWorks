namespace SIL.FieldWorks.XWorks
{
	partial class WebonaryLogViewer
	{
		private System.ComponentModel.IContainer components = null;

		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(WebonaryLogViewer));
			this.mainTableLayout = new System.Windows.Forms.TableLayoutPanel();
			this.buttonPanel = new System.Windows.Forms.FlowLayoutPanel();
			this.saveLogButton = new System.Windows.Forms.Button();
			this.logEntryView = new System.Windows.Forms.DataGridView();
			this.filterBox = new System.Windows.Forms.ComboBox();
			this.mainTableLayout.SuspendLayout();
			this.buttonPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.logEntryView)).BeginInit();
			this.SuspendLayout();
			// 
			// mainTableLayout
			// 
			resources.ApplyResources(this.mainTableLayout, "mainTableLayout");
			this.mainTableLayout.Controls.Add(this.buttonPanel, 0, 2);
			this.mainTableLayout.Controls.Add(this.logEntryView, 0, 1);
			this.mainTableLayout.Controls.Add(this.filterBox, 0, 0);
			this.mainTableLayout.Name = "mainTableLayout";
			// 
			// buttonPanel
			// 
			this.buttonPanel.Controls.Add(this.saveLogButton);
			resources.ApplyResources(this.buttonPanel, "buttonPanel");
			this.buttonPanel.Name = "buttonPanel";
			// 
			// saveLogButton
			// 
			resources.ApplyResources(this.saveLogButton, "saveLogButton");
			this.saveLogButton.Name = "saveLogButton";
			this.saveLogButton.UseVisualStyleBackColor = true;
			// 
			// logEntryView
			// 
			this.logEntryView.AllowUserToAddRows = false;
			this.logEntryView.AllowUserToDeleteRows = false;
			this.logEntryView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			resources.ApplyResources(this.logEntryView, "logEntryView");
			this.logEntryView.Name = "logEntryView";
			this.logEntryView.ReadOnly = true;
			// 
			// filterBox
			// 
			this.filterBox.FormattingEnabled = true;
			resources.ApplyResources(this.filterBox, "filterBox");
			this.filterBox.Name = "filterBox";
			// 
			// WebonaryLogViewer
			// 
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.mainTableLayout);
			this.MinimizeBox = false;
			this.Name = "WebonaryLogViewer";
			this.ShowIcon = false;
			this.TopMost = true;
			this.mainTableLayout.ResumeLayout(false);
			this.buttonPanel.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.logEntryView)).EndInit();
			this.ResumeLayout(false);

		}
	  private System.Windows.Forms.TableLayoutPanel mainTableLayout;
	  private System.Windows.Forms.FlowLayoutPanel buttonPanel;
	  private System.Windows.Forms.Button saveLogButton;
	  private System.Windows.Forms.DataGridView logEntryView;
	  private System.Windows.Forms.ComboBox filterBox;
   }
}
namespace SIL.FieldWorks.XWorks
{
	partial class DictionaryConfigurationManagerDlg
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DictionaryConfigurationManagerDlg));
			this.mainTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.explanationLabel = new System.Windows.Forms.Label();
			this.configurationsGroupBox = new System.Windows.Forms.GroupBox();
			this.publicationsGroupBox = new System.Windows.Forms.GroupBox();
			this.configurationsTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.configurationsExplanationLabel = new System.Windows.Forms.Label();
			this.copyButton = new System.Windows.Forms.Button();
			this.removeButton = new System.Windows.Forms.Button();
			this.configurationsListView = new System.Windows.Forms.ListView();
			this.publicationsTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.publicationsExplanationLabel = new System.Windows.Forms.Label();
			this.publicationsListView = new System.Windows.Forms.ListView();
			this.closeButton = new System.Windows.Forms.Button();
			this.helpButton = new System.Windows.Forms.Button();
			this.buttonTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.mainTableLayoutPanel.SuspendLayout();
			this.configurationsGroupBox.SuspendLayout();
			this.publicationsGroupBox.SuspendLayout();
			this.configurationsTableLayoutPanel.SuspendLayout();
			this.publicationsTableLayoutPanel.SuspendLayout();
			this.buttonTableLayoutPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// mainTableLayoutPanel
			// 
			resources.ApplyResources(this.mainTableLayoutPanel, "mainTableLayoutPanel");
			this.mainTableLayoutPanel.Controls.Add(this.explanationLabel, 0, 0);
			this.mainTableLayoutPanel.Controls.Add(this.buttonTableLayoutPanel, 1, 2);
			this.mainTableLayoutPanel.Controls.Add(this.configurationsGroupBox, 0, 1);
			this.mainTableLayoutPanel.Controls.Add(this.publicationsGroupBox, 1, 1);
			this.mainTableLayoutPanel.Name = "mainTableLayoutPanel";
			// 
			// explanationLabel
			// 
			resources.ApplyResources(this.explanationLabel, "explanationLabel");
			this.mainTableLayoutPanel.SetColumnSpan(this.explanationLabel, 2);
			this.explanationLabel.Name = "explanationLabel";
			// 
			// configurationsGroupBox
			// 
			this.configurationsGroupBox.Controls.Add(this.configurationsTableLayoutPanel);
			resources.ApplyResources(this.configurationsGroupBox, "configurationsGroupBox");
			this.configurationsGroupBox.Name = "configurationsGroupBox";
			this.configurationsGroupBox.TabStop = false;
			// 
			// publicationsGroupBox
			// 
			this.publicationsGroupBox.Controls.Add(this.publicationsTableLayoutPanel);
			resources.ApplyResources(this.publicationsGroupBox, "publicationsGroupBox");
			this.publicationsGroupBox.Name = "publicationsGroupBox";
			this.publicationsGroupBox.TabStop = false;
			// 
			// configurationsTableLayoutPanel
			// 
			resources.ApplyResources(this.configurationsTableLayoutPanel, "configurationsTableLayoutPanel");
			this.configurationsTableLayoutPanel.Controls.Add(this.configurationsListView, 0, 1);
			this.configurationsTableLayoutPanel.Controls.Add(this.removeButton, 1, 2);
			this.configurationsTableLayoutPanel.Controls.Add(this.copyButton, 1, 1);
			this.configurationsTableLayoutPanel.Controls.Add(this.configurationsExplanationLabel, 0, 0);
			this.configurationsTableLayoutPanel.Name = "configurationsTableLayoutPanel";
			// 
			// configurationsExplanationLabel
			// 
			resources.ApplyResources(this.configurationsExplanationLabel, "configurationsExplanationLabel");
			this.configurationsTableLayoutPanel.SetColumnSpan(this.configurationsExplanationLabel, 2);
			this.configurationsExplanationLabel.Name = "configurationsExplanationLabel";
			// 
			// copyButton
			// 
			resources.ApplyResources(this.copyButton, "copyButton");
			this.copyButton.Name = "copyButton";
			this.copyButton.UseVisualStyleBackColor = true;
			// 
			// removeButton
			// 
			resources.ApplyResources(this.removeButton, "removeButton");
			this.removeButton.Name = "removeButton";
			this.removeButton.UseVisualStyleBackColor = true;
			// 
			// configurationsListView
			// 
			resources.ApplyResources(this.configurationsListView, "configurationsListView");
			this.configurationsListView.FullRowSelect = true;
			this.configurationsListView.HideSelection = false;
			this.configurationsListView.LabelEdit = true;
			this.configurationsListView.MultiSelect = false;
			this.configurationsListView.Name = "configurationsListView";
			this.configurationsTableLayoutPanel.SetRowSpan(this.configurationsListView, 3);
			this.configurationsListView.UseCompatibleStateImageBehavior = false;
			this.configurationsListView.View = System.Windows.Forms.View.List;
			// 
			// publicationsTableLayoutPanel
			// 
			resources.ApplyResources(this.publicationsTableLayoutPanel, "publicationsTableLayoutPanel");
			this.publicationsTableLayoutPanel.Controls.Add(this.publicationsListView, 0, 1);
			this.publicationsTableLayoutPanel.Controls.Add(this.publicationsExplanationLabel, 0, 0);
			this.publicationsTableLayoutPanel.Name = "publicationsTableLayoutPanel";
			// 
			// publicationsExplanationLabel
			// 
			resources.ApplyResources(this.publicationsExplanationLabel, "publicationsExplanationLabel");
			this.publicationsExplanationLabel.Name = "publicationsExplanationLabel";
			// 
			// publicationsListView
			// 
			this.publicationsListView.CheckBoxes = true;
			resources.ApplyResources(this.publicationsListView, "publicationsListView");
			this.publicationsListView.Name = "publicationsListView";
			this.publicationsTableLayoutPanel.SetRowSpan(this.publicationsListView, 3);
			this.publicationsListView.UseCompatibleStateImageBehavior = false;
			this.publicationsListView.View = System.Windows.Forms.View.List;
			// 
			// closeButton
			// 
			this.closeButton.DialogResult = System.Windows.Forms.DialogResult.OK;
			resources.ApplyResources(this.closeButton, "closeButton");
			this.closeButton.Name = "closeButton";
			this.closeButton.UseVisualStyleBackColor = true;
			// 
			// helpButton
			// 
			resources.ApplyResources(this.helpButton, "helpButton");
			this.helpButton.Name = "helpButton";
			this.helpButton.UseVisualStyleBackColor = true;
			// 
			// buttonTableLayoutPanel
			// 
			resources.ApplyResources(this.buttonTableLayoutPanel, "buttonTableLayoutPanel");
			this.mainTableLayoutPanel.SetColumnSpan(this.buttonTableLayoutPanel, 4);
			this.buttonTableLayoutPanel.Controls.Add(this.helpButton, 2, 0);
			this.buttonTableLayoutPanel.Controls.Add(this.closeButton, 1, 0);
			this.buttonTableLayoutPanel.Name = "buttonTableLayoutPanel";
			// 
			// DictionaryConfigurationManagerDlg
			// 
			this.AcceptButton = this.closeButton;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.mainTableLayoutPanel);
			this.Name = "DictionaryConfigurationManagerDlg";
			this.ShowIcon = false;
			this.mainTableLayoutPanel.ResumeLayout(false);
			this.mainTableLayoutPanel.PerformLayout();
			this.configurationsGroupBox.ResumeLayout(false);
			this.publicationsGroupBox.ResumeLayout(false);
			this.configurationsTableLayoutPanel.ResumeLayout(false);
			this.configurationsTableLayoutPanel.PerformLayout();
			this.publicationsTableLayoutPanel.ResumeLayout(false);
			this.publicationsTableLayoutPanel.PerformLayout();
			this.buttonTableLayoutPanel.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel mainTableLayoutPanel;
		private System.Windows.Forms.Label explanationLabel;
		private System.Windows.Forms.TableLayoutPanel buttonTableLayoutPanel;
		private System.Windows.Forms.Button helpButton;
		private System.Windows.Forms.Button closeButton;
		private System.Windows.Forms.GroupBox configurationsGroupBox;
		private System.Windows.Forms.TableLayoutPanel configurationsTableLayoutPanel;
		public System.Windows.Forms.ListView configurationsListView;
		public System.Windows.Forms.Button removeButton;
		public System.Windows.Forms.Button copyButton;
		private System.Windows.Forms.Label configurationsExplanationLabel;
		private System.Windows.Forms.GroupBox publicationsGroupBox;
		private System.Windows.Forms.TableLayoutPanel publicationsTableLayoutPanel;
		public System.Windows.Forms.ListView publicationsListView;
		private System.Windows.Forms.Label publicationsExplanationLabel;
	}
}
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
			this.configurationsListLabel = new System.Windows.Forms.Label();
			this.publicationsListLabel = new System.Windows.Forms.Label();
			this.copyButton = new System.Windows.Forms.Button();
			this.removeButton = new System.Windows.Forms.Button();
			this.buttonTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.helpButton = new System.Windows.Forms.Button();
			this.closeButton = new System.Windows.Forms.Button();
			this.configurationsListView = new System.Windows.Forms.ListView();
			this.publicationsListView = new System.Windows.Forms.ListView();
			this.configurationsExplanationLabel = new System.Windows.Forms.Label();
			this.publicationsExplanationLabel = new System.Windows.Forms.Label();
			this.mainTableLayoutPanel.SuspendLayout();
			this.buttonTableLayoutPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// mainTableLayoutPanel
			// 
			resources.ApplyResources(this.mainTableLayoutPanel, "mainTableLayoutPanel");
			this.mainTableLayoutPanel.Controls.Add(this.explanationLabel, 0, 0);
			this.mainTableLayoutPanel.Controls.Add(this.configurationsListLabel, 0, 1);
			this.mainTableLayoutPanel.Controls.Add(this.publicationsListLabel, 3, 1);
			this.mainTableLayoutPanel.Controls.Add(this.copyButton, 1, 3);
			this.mainTableLayoutPanel.Controls.Add(this.removeButton, 1, 4);
			this.mainTableLayoutPanel.Controls.Add(this.buttonTableLayoutPanel, 3, 6);
			this.mainTableLayoutPanel.Controls.Add(this.configurationsListView, 0, 3);
			this.mainTableLayoutPanel.Controls.Add(this.publicationsListView, 3, 3);
			this.mainTableLayoutPanel.Controls.Add(this.configurationsExplanationLabel, 0, 2);
			this.mainTableLayoutPanel.Controls.Add(this.publicationsExplanationLabel, 3, 2);
			this.mainTableLayoutPanel.Name = "mainTableLayoutPanel";
			// 
			// explanationLabel
			// 
			resources.ApplyResources(this.explanationLabel, "explanationLabel");
			this.mainTableLayoutPanel.SetColumnSpan(this.explanationLabel, 4);
			this.explanationLabel.Name = "explanationLabel";
			// 
			// configurationsListLabel
			// 
			resources.ApplyResources(this.configurationsListLabel, "configurationsListLabel");
			this.configurationsListLabel.Name = "configurationsListLabel";
			// 
			// publicationsListLabel
			// 
			resources.ApplyResources(this.publicationsListLabel, "publicationsListLabel");
			this.publicationsListLabel.Name = "publicationsListLabel";
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
			// buttonTableLayoutPanel
			// 
			resources.ApplyResources(this.buttonTableLayoutPanel, "buttonTableLayoutPanel");
			this.mainTableLayoutPanel.SetColumnSpan(this.buttonTableLayoutPanel, 4);
			this.buttonTableLayoutPanel.Controls.Add(this.helpButton, 1, 0);
			this.buttonTableLayoutPanel.Controls.Add(this.closeButton, 0, 0);
			this.buttonTableLayoutPanel.Name = "buttonTableLayoutPanel";
			// 
			// helpButton
			// 
			resources.ApplyResources(this.helpButton, "helpButton");
			this.helpButton.Name = "helpButton";
			this.helpButton.UseVisualStyleBackColor = true;
			// 
			// closeButton
			// 
			this.closeButton.DialogResult = System.Windows.Forms.DialogResult.OK;
			resources.ApplyResources(this.closeButton, "closeButton");
			this.closeButton.Name = "closeButton";
			this.closeButton.UseVisualStyleBackColor = true;
			// 
			// configurationsListView
			// 
			resources.ApplyResources(this.configurationsListView, "configurationsListView");
			this.configurationsListView.FullRowSelect = true;
			this.configurationsListView.HideSelection = false;
			this.configurationsListView.LabelEdit = true;
			this.configurationsListView.MultiSelect = false;
			this.configurationsListView.Name = "configurationsListView";
			this.mainTableLayoutPanel.SetRowSpan(this.configurationsListView, 3);
			this.configurationsListView.UseCompatibleStateImageBehavior = false;
			this.configurationsListView.View = System.Windows.Forms.View.List;
			this.configurationsListView.KeyUp += new System.Windows.Forms.KeyEventHandler(this.ConfigurationsListViewKeyUp);
			// 
			// publicationsListView
			// 
			this.publicationsListView.CheckBoxes = true;
			resources.ApplyResources(this.publicationsListView, "publicationsListView");
			this.publicationsListView.Name = "publicationsListView";
			this.mainTableLayoutPanel.SetRowSpan(this.publicationsListView, 3);
			this.publicationsListView.UseCompatibleStateImageBehavior = false;
			this.publicationsListView.View = System.Windows.Forms.View.List;
			// 
			// configurationsExplanationLabel
			// 
			resources.ApplyResources(this.configurationsExplanationLabel, "configurationsExplanationLabel");
			this.configurationsExplanationLabel.Name = "configurationsExplanationLabel";
			// 
			// publicationsExplanationLabel
			// 
			resources.ApplyResources(this.publicationsExplanationLabel, "publicationsExplanationLabel");
			this.publicationsExplanationLabel.Name = "publicationsExplanationLabel";
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
			this.buttonTableLayoutPanel.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel mainTableLayoutPanel;
		private System.Windows.Forms.Label explanationLabel;
		private System.Windows.Forms.Label configurationsListLabel;
		private System.Windows.Forms.Label publicationsListLabel;
		public System.Windows.Forms.Button copyButton;
		public System.Windows.Forms.Button removeButton;
		public System.Windows.Forms.ListView configurationsListView;
		private System.Windows.Forms.TableLayoutPanel buttonTableLayoutPanel;
		private System.Windows.Forms.Button helpButton;
		private System.Windows.Forms.Button closeButton;
		public System.Windows.Forms.ListView publicationsListView;
		private System.Windows.Forms.Label configurationsExplanationLabel;
		private System.Windows.Forms.Label publicationsExplanationLabel;
	}
}
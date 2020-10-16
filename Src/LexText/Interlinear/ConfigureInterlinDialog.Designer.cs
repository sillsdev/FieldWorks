using System.ComponentModel;
using System.Windows.Forms;
using SIL.Windows.Forms.HtmlBrowser;

namespace SIL.FieldWorks.IText
{
	partial class ConfigureInterlinDialog
	{
		private IContainer components = null;

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources =
				new System.ComponentModel.ComponentResourceManager(
					typeof(ConfigureInterlinDialog));
			this.mainLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.description = new System.Windows.Forms.Label();
			this.buttonLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
			this.helpButton = new System.Windows.Forms.Button();
			this.cancelButton = new System.Windows.Forms.Button();
			this.okButton = new System.Windows.Forms.Button();
			this.mainBrowser = new SIL.Windows.Forms.HtmlBrowser.XWebBrowser();
			this.mainLayoutPanel.SuspendLayout();
			this.buttonLayoutPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// mainLayoutPanel
			// 
			resources.ApplyResources(this.mainLayoutPanel, "mainLayoutPanel");
			this.mainLayoutPanel.Controls.Add(this.mainBrowser, 0, 1);
			this.mainLayoutPanel.Controls.Add(this.description, 0, 0);
			this.mainLayoutPanel.Controls.Add(this.buttonLayoutPanel, 0, 2);
			this.mainLayoutPanel.Name = "mainLayoutPanel";
			// 
			// description
			// 
			resources.ApplyResources(this.description, "description");
			this.description.Name = "description";
			// 
			// buttonLayoutPanel
			// 
			resources.ApplyResources(this.buttonLayoutPanel, "buttonLayoutPanel");
			this.buttonLayoutPanel.Controls.Add(this.helpButton);
			this.buttonLayoutPanel.Controls.Add(this.cancelButton);
			this.buttonLayoutPanel.Controls.Add(this.okButton);
			this.buttonLayoutPanel.Name = "buttonLayoutPanel";
			// 
			// helpButton
			// 
			resources.ApplyResources(this.helpButton, "helpButton");
			this.helpButton.BackColor = System.Drawing.SystemColors.Control;
			this.helpButton.Name = "helpButton";
			this.helpButton.UseVisualStyleBackColor = false;
			// 
			// cancelButton
			// 
			resources.ApplyResources(this.cancelButton, "cancelButton");
			this.cancelButton.BackColor = System.Drawing.SystemColors.Control;
			this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.cancelButton.Name = "cancelButton";
			this.cancelButton.UseVisualStyleBackColor = false;
			// 
			// okButton
			// 
			resources.ApplyResources(this.okButton, "okButton");
			this.okButton.BackColor = System.Drawing.SystemColors.Control;
			this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.okButton.Name = "okButton";
			this.okButton.UseVisualStyleBackColor = false;
			// 
			// mainBrowser
			// 
			resources.ApplyResources(this.mainBrowser, "mainBrowser");
			this.mainBrowser.IsWebBrowserContextMenuEnabled = false;
			this.mainBrowser.Name = "mainBrowser";
			this.mainBrowser.Url = new System.Uri("about:blank", System.UriKind.Absolute);
			// 
			// ConfigureInterlinDialog
			// 
			resources.ApplyResources(this, "$this");
			this.Controls.Add(this.mainLayoutPanel);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ConfigureInterlinDialog";
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
			this.mainLayoutPanel.ResumeLayout(false);
			this.mainLayoutPanel.PerformLayout();
			this.buttonLayoutPanel.ResumeLayout(false);
			this.ResumeLayout(false);
		}
		#endregion

		private TableLayoutPanel mainLayoutPanel;
		private XWebBrowser mainBrowser;
		private Label description;
		private FlowLayoutPanel buttonLayoutPanel;
		private Button helpButton;
		private Button cancelButton;
		private Button okButton;
	}
}
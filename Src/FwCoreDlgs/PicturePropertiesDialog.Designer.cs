// Copyright (c) 2004-2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.ComponentModel;
using System.Windows.Forms;
using SIL.FieldWorks.Common.Widgets;
using SIL.Windows.Forms.ImageToolbox;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Dialog for editing picture properties
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class PicturePropertiesDialog : Form
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.Windows.Forms.Label lblFile;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PicturePropertiesDialog));
			System.Windows.Forms.Button btnCancel;
			SIL.Windows.Forms.ImageToolbox.PalasoImage palasoImage1 = new SIL.Windows.Forms.ImageToolbox.PalasoImage();
			SIL.Windows.Forms.ClearShare.Metadata metadata1 = new SIL.Windows.Forms.ClearShare.Metadata();
			this.m_btnHelp = new System.Windows.Forms.Button();
			this.m_tooltip = new System.Windows.Forms.ToolTip(this.components);
			this.m_grpFileLocOptions = new System.Windows.Forms.GroupBox();
			this.m_btnBrowseDest = new System.Windows.Forms.Button();
			this.m_txtDestination = new System.Windows.Forms.TextBox();
			this.m_lblDestination = new System.Windows.Forms.Label();
			this.m_rbLeave = new System.Windows.Forms.RadioButton();
			this.m_rbMove_rbSaveAs = new System.Windows.Forms.RadioButton();
			this.m_rbCopy_rbSave = new System.Windows.Forms.RadioButton();
			this.panelFileName = new System.Windows.Forms.Panel();
			this.txtFileName = new System.Windows.Forms.TextBox();
			this.panelButtons = new System.Windows.Forms.Panel();
			this.imageToolbox = new SIL.Windows.Forms.ImageToolbox.ImageToolboxControl();
			this.lblSourcePath = new System.Windows.Forms.Label();
			lblFile = new System.Windows.Forms.Label();
			btnCancel = new System.Windows.Forms.Button();
			m_btnOK = new System.Windows.Forms.Button();
			lblFileName = new System.Windows.Forms.Label();
			this.m_grpFileLocOptions.SuspendLayout();
			this.panelFileName.SuspendLayout();
			this.panelButtons.SuspendLayout();
			this.SuspendLayout();
			// 
			// lblFile
			// 
			resources.ApplyResources(lblFile, "lblFile");
			lblFile.Name = "lblFile";
			// 
			// btnCancel
			// 
			resources.ApplyResources(btnCancel, "btnCancel");
			btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			btnCancel.Name = "btnCancel";
			// 
			// m_btnHelp
			// 
			resources.ApplyResources(this.m_btnHelp, "m_btnHelp");
			this.m_btnHelp.Name = "m_btnHelp";
			this.m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			// 
			// m_grpFileLocOptions
			// 
			resources.ApplyResources(this.m_grpFileLocOptions, "m_grpFileLocOptions");
			this.m_grpFileLocOptions.Controls.Add(lblFileName);
			this.m_grpFileLocOptions.Controls.Add(this.txtFileName);
			this.m_grpFileLocOptions.Controls.Add(this.m_btnBrowseDest);
			this.m_grpFileLocOptions.Controls.Add(this.m_txtDestination);
			this.m_grpFileLocOptions.Controls.Add(this.m_lblDestination);
			this.m_grpFileLocOptions.Controls.Add(this.m_rbLeave);
			this.m_grpFileLocOptions.Controls.Add(this.m_rbMove_rbSaveAs);
			this.m_grpFileLocOptions.Controls.Add(this.m_rbCopy_rbSave);
			this.m_grpFileLocOptions.Name = "m_grpFileLocOptions";
			this.m_grpFileLocOptions.TabStop = false;
			// 
			// m_btnBrowseDest
			// 
			resources.ApplyResources(this.m_btnBrowseDest, "m_btnBrowseDest");
			this.m_btnBrowseDest.Name = "m_btnBrowseDest";
			this.m_btnBrowseDest.UseVisualStyleBackColor = true;
			this.m_btnBrowseDest.Click += new System.EventHandler(this.m_btnBrowseDest_Click);
			// 
			// m_txtDestination
			// 
			resources.ApplyResources(this.m_txtDestination, "m_txtDestination");
			this.m_txtDestination.Name = "m_txtDestination";
			// 
			// m_lblDestination
			// 
			resources.ApplyResources(this.m_lblDestination, "m_lblDestination");
			this.m_lblDestination.Name = "m_lblDestination";
			// 
			// m_rbLeave
			// 
			resources.ApplyResources(this.m_rbLeave, "m_rbLeave");
			this.m_rbLeave.Name = "m_rbLeave";
			this.m_rbLeave.UseVisualStyleBackColor = true;
			this.m_rbLeave.CheckedChanged += new System.EventHandler(this.HandleLocationCheckedChanged);
			// 
			// m_rbMove_rbSaveAs
			// 
			resources.ApplyResources(this.m_rbMove_rbSaveAs, "m_rbMove_rbSaveAs");
			this.m_rbMove_rbSaveAs.Name = "m_rbMove_rbSaveAs";
			this.m_rbMove_rbSaveAs.UseVisualStyleBackColor = true;
			this.m_rbMove_rbSaveAs.CheckedChanged += new System.EventHandler(this.HandleLocationCheckedChanged);
			// 
			// m_rbCopy_rbSave
			// 
			resources.ApplyResources(this.m_rbCopy_rbSave, "m_rbCopy_rbSave");
			this.m_rbCopy_rbSave.Checked = true;
			this.m_rbCopy_rbSave.Name = "m_rbCopy_rbSave";
			this.m_rbCopy_rbSave.TabStop = true;
			this.m_rbCopy_rbSave.UseVisualStyleBackColor = true;
			this.m_rbCopy_rbSave.CheckedChanged += new System.EventHandler(this.HandleLocationCheckedChanged);
			// 
			// panelFileName
			// 
			resources.ApplyResources(this.panelFileName, "panelFileName");
			this.panelFileName.Controls.Add(this.lblSourcePath);
			this.panelFileName.Controls.Add(lblFile);
			this.panelFileName.ForeColor = System.Drawing.SystemColors.ControlText;
			this.panelFileName.Name = "panelFileName";
			// 
			// txtFileName
			// 
			resources.ApplyResources(this.txtFileName, "txtFileName");
			this.txtFileName.Name = "txtFileName";
			// 
			// panelButtons
			// 
			resources.ApplyResources(this.panelButtons, "panelButtons");
			this.panelButtons.Controls.Add(btnCancel);
			this.panelButtons.Controls.Add(this.m_btnHelp);
			this.panelButtons.Controls.Add(m_btnOK);
			this.panelButtons.Name = "panelButtons";
			// 
			// m_btnOK
			// 
			resources.ApplyResources(m_btnOK, "m_btnOK");
			m_btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			m_btnOK.Name = "m_btnOK";
			// 
			// imageToolbox
			// 
			resources.ApplyResources(this.imageToolbox, "imageToolbox");
			this.imageToolbox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.imageToolbox.EditMetadataActionOverride = null;
			palasoImage1.Image = null;
			metadata1.AttributionUrl = null;
			metadata1.CollectionName = null;
			metadata1.CollectionUri = null;
			metadata1.CopyrightNotice = "";
			metadata1.Creator = null;
			metadata1.License = null;
			palasoImage1.Metadata = metadata1;
			palasoImage1.MetadataLocked = false;
			this.imageToolbox.ImageInfo = palasoImage1;
			this.imageToolbox.ImageLoadingExceptionReporter = null;
			this.imageToolbox.InitialSearchString = null;
			this.imageToolbox.Name = "imageToolbox";
			this.imageToolbox.SearchLanguage = "en";
			this.imageToolbox.ImageChanged += new System.EventHandler(this.ImageToolbox_ImageChanged);
			this.imageToolbox.MetadataChanged += new System.EventHandler(this.ImageToolbox_MetadataChanged);
			this.imageToolbox.Enter += new System.EventHandler(ImageToolbox_Enter);
			this.imageToolbox.Leave += new System.EventHandler(ImageToolbox_Leave);
			// 
			// lblFileName
			// 
			resources.ApplyResources(lblFileName, "lblFileName");
			this.lblFileName.Name = "lblFileName";
			// 
			// lblSourcePath
			// 
			resources.ApplyResources(this.lblSourcePath, "lblSourcePath");
			this.lblSourcePath.Name = "lblSourcePath";
			// 
			// PicturePropertiesDialog
			// 
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = btnCancel;
			this.Controls.Add(this.imageToolbox);
			this.Controls.Add(this.m_grpFileLocOptions);
			this.Controls.Add(this.panelFileName);
			this.Controls.Add(this.panelButtons);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "PicturePropertiesDialog";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.m_grpFileLocOptions.ResumeLayout(false);
			this.m_grpFileLocOptions.PerformLayout();
			this.panelFileName.ResumeLayout(false);
			this.panelFileName.PerformLayout();
			this.panelButtons.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		private IContainer components;
		private Button m_btnOK;
		private Button m_btnHelp;
		private LabeledMultiStringControl m_lmscCaption;
		private ToolTip m_tooltip;
		private GroupBox m_grpFileLocOptions;
		private RadioButton m_rbMove_rbSaveAs;
		private RadioButton m_rbCopy_rbSave;
		private RadioButton m_rbLeave;
		private TextBox m_txtDestination;
		private Label m_lblDestination;
		private Button m_btnBrowseDest;
		private Panel panelFileName;
		private Panel panelButtons;

		private TextBox txtFileName;

		private ImageToolboxControl imageToolbox;
		private Label lblFileName;
		private Label lblSourcePath;
	}
}

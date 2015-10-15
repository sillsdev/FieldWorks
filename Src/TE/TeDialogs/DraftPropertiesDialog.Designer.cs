// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.TE
{
	partial class DraftPropertiesDialog
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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DraftPropertiesDialog));
			this.m_cbProtected = new System.Windows.Forms.CheckBox();
			this.m_tbDescription = new System.Windows.Forms.TextBox();
			this.btnOk = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.pictVersionType = new System.Windows.Forms.PictureBox();
			this.label1 = new System.Windows.Forms.Label();
			this.m_imageListTypeIcons = new System.Windows.Forms.ImageList(this.components);
			this.label4 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.lblCreatedDate = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.pictVersionType)).BeginInit();
			this.SuspendLayout();
			//
			// m_cbProtected
			//
			resources.ApplyResources(this.m_cbProtected, "m_cbProtected");
			this.m_cbProtected.Name = "m_cbProtected";
			this.m_cbProtected.UseVisualStyleBackColor = true;
			//
			// m_tbDescription
			//
			resources.ApplyResources(this.m_tbDescription, "m_tbDescription");
			this.m_tbDescription.Name = "m_tbDescription";
			//
			// btnOk
			//
			resources.ApplyResources(this.btnOk, "btnOk");
			this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnOk.Name = "btnOk";
			this.btnOk.UseVisualStyleBackColor = true;
			//
			// btnCancel
			//
			resources.ApplyResources(this.btnCancel, "btnCancel");
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			//
			// pictVersionType
			//
			this.pictVersionType.ErrorImage = null;
			resources.ApplyResources(this.pictVersionType, "pictVersionType");
			this.pictVersionType.Name = "pictVersionType";
			this.pictVersionType.TabStop = false;
			//
			// label1
			//
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			//
			// m_imageListTypeIcons
			//
			this.m_imageListTypeIcons.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("m_imageListTypeIcons.ImageStream")));
			this.m_imageListTypeIcons.TransparentColor = System.Drawing.Color.White;
			this.m_imageListTypeIcons.Images.SetKeyName(0, "ImportedVersion.png");
			this.m_imageListTypeIcons.Images.SetKeyName(1, "SavedVersion.png");
			//
			// label4
			//
			resources.ApplyResources(this.label4, "label4");
			this.label4.Name = "label4";
			//
			// label5
			//
			resources.ApplyResources(this.label5, "label5");
			this.label5.Name = "label5";
			//
			// lblCreatedDate
			//
			resources.ApplyResources(this.lblCreatedDate, "lblCreatedDate");
			this.lblCreatedDate.Name = "lblCreatedDate";
			//
			// label3
			//
			resources.ApplyResources(this.label3, "label3");
			this.label3.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.label3.Name = "label3";
			//
			// label2
			//
			resources.ApplyResources(this.label2, "label2");
			this.label2.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.label2.Name = "label2";
			//
			// DraftPropertiesDialog
			//
			this.AcceptButton = this.btnOk;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnCancel;
			this.Controls.Add(this.lblCreatedDate);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.pictVersionType);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnOk);
			this.Controls.Add(this.m_tbDescription);
			this.Controls.Add(this.m_cbProtected);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "DraftPropertiesDialog";
			this.ShowIcon = false;
			((System.ComponentModel.ISupportInitialize)(this.pictVersionType)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.CheckBox m_cbProtected;
		private System.Windows.Forms.TextBox m_tbDescription;
		private System.Windows.Forms.Button btnOk;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.PictureBox pictVersionType;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.ImageList m_imageListTypeIcons;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label lblCreatedDate;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label2;
	}
}
// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ConfirmOverlappingFileReplace.cs
// Responsibility: TomB
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ScriptureUtils;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for ConfirmOverlappingFileReplace.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ConfirmOverlappingFileReplaceDialog : System.Windows.Forms.Form,
		IOverlappingFileResolver
	{
		#region Data members
		private ScrImportFileInfo m_file1;
		private string m_filePath1;
		private ScrImportFileInfo m_file2;
		private string m_filePath2;

		private string m_formatByteCount;
		private string m_formatModifiedDate;
		private System.Windows.Forms.ImageList m_fileImages;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.Button btnHelp;
		private System.Windows.Forms.Button btnOk;
		private System.Windows.Forms.RadioButton optUse2;
		private System.Windows.Forms.RadioButton optUse1;
		private System.Windows.Forms.Label lblFilePath2;
		private System.Windows.Forms.Label lblReferenceRange2;
		private System.Windows.Forms.Label lblFileSize2;
		private System.Windows.Forms.Label lblFileModified2;
		private System.Windows.Forms.Label lblFileModified1;
		private System.Windows.Forms.Label lblFileSize1;
		private System.Windows.Forms.Label lblReferenceRange1;
		private System.Windows.Forms.Label lblFilePath1;
		private PictureBox pictureBox1;
		private PictureBox pictureBox2;
		private PictureBox pictureBox3;
		private System.ComponentModel.IContainer components;
		#endregion

		#region Construction and Destruction
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the ConfirmOverlappingFileReplace class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ConfirmOverlappingFileReplaceDialog()
		{
			InitializeComponent();
			m_formatByteCount = lblFileSize1.Text;
			m_formatModifiedDate = lblFileModified1.Text;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged
		/// resources; <c>false</c> to release only unmanaged resources.
		/// </param>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose( bool disposing )
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}
		#endregion

		#region Windows Form Designer generated code
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConfirmOverlappingFileReplaceDialog));
			System.Windows.Forms.Label label1;
			System.Windows.Forms.Label label8;
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.pictureBox2 = new System.Windows.Forms.PictureBox();
			this.pictureBox3 = new System.Windows.Forms.PictureBox();
			this.m_fileImages = new System.Windows.Forms.ImageList(this.components);
			this.lblFilePath2 = new System.Windows.Forms.Label();
			this.lblReferenceRange2 = new System.Windows.Forms.Label();
			this.lblFileSize2 = new System.Windows.Forms.Label();
			this.lblFileModified2 = new System.Windows.Forms.Label();
			this.lblFileModified1 = new System.Windows.Forms.Label();
			this.lblFileSize1 = new System.Windows.Forms.Label();
			this.lblReferenceRange1 = new System.Windows.Forms.Label();
			this.lblFilePath1 = new System.Windows.Forms.Label();
			this.btnOk = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.btnHelp = new System.Windows.Forms.Button();
			this.optUse2 = new System.Windows.Forms.RadioButton();
			this.optUse1 = new System.Windows.Forms.RadioButton();
			label1 = new System.Windows.Forms.Label();
			label8 = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox3)).BeginInit();
			this.SuspendLayout();
			//
			// pictureBox1
			//
			resources.ApplyResources(this.pictureBox1, "pictureBox1");
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.TabStop = false;
			//
			// label1
			//
			resources.ApplyResources(label1, "label1");
			label1.Name = "label1";
			//
			// pictureBox2
			//
			resources.ApplyResources(this.pictureBox2, "pictureBox2");
			this.pictureBox2.Name = "pictureBox2";
			this.pictureBox2.TabStop = false;
			//
			// pictureBox3
			//
			resources.ApplyResources(this.pictureBox3, "pictureBox3");
			this.pictureBox3.Name = "pictureBox3";
			this.pictureBox3.TabStop = false;
			//
			// label8
			//
			resources.ApplyResources(label8, "label8");
			label8.Name = "label8";
			//
			// m_fileImages
			//
			this.m_fileImages.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("m_fileImages.ImageStream")));
			this.m_fileImages.TransparentColor = System.Drawing.Color.Magenta;
			this.m_fileImages.Images.SetKeyName(0, "");
			this.m_fileImages.Images.SetKeyName(1, "");
			//
			// lblFilePath2
			//
			resources.ApplyResources(this.lblFilePath2, "lblFilePath2");
			this.lblFilePath2.Name = "lblFilePath2";
			this.lblFilePath2.Paint += new System.Windows.Forms.PaintEventHandler(this.lblFileName_Paint);
			//
			// lblReferenceRange2
			//
			resources.ApplyResources(this.lblReferenceRange2, "lblReferenceRange2");
			this.lblReferenceRange2.Name = "lblReferenceRange2";
			//
			// lblFileSize2
			//
			resources.ApplyResources(this.lblFileSize2, "lblFileSize2");
			this.lblFileSize2.Name = "lblFileSize2";
			//
			// lblFileModified2
			//
			resources.ApplyResources(this.lblFileModified2, "lblFileModified2");
			this.lblFileModified2.Name = "lblFileModified2";
			//
			// lblFileModified1
			//
			resources.ApplyResources(this.lblFileModified1, "lblFileModified1");
			this.lblFileModified1.Name = "lblFileModified1";
			//
			// lblFileSize1
			//
			resources.ApplyResources(this.lblFileSize1, "lblFileSize1");
			this.lblFileSize1.Name = "lblFileSize1";
			//
			// lblReferenceRange1
			//
			resources.ApplyResources(this.lblReferenceRange1, "lblReferenceRange1");
			this.lblReferenceRange1.Name = "lblReferenceRange1";
			//
			// lblFilePath1
			//
			resources.ApplyResources(this.lblFilePath1, "lblFilePath1");
			this.lblFilePath1.Name = "lblFilePath1";
			this.lblFilePath1.Paint += new System.Windows.Forms.PaintEventHandler(this.lblFileName_Paint);
			//
			// btnOk
			//
			this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
			resources.ApplyResources(this.btnOk, "btnOk");
			this.btnOk.Name = "btnOk";
			//
			// btnCancel
			//
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(this.btnCancel, "btnCancel");
			this.btnCancel.Name = "btnCancel";
			//
			// btnHelp
			//
			resources.ApplyResources(this.btnHelp, "btnHelp");
			this.btnHelp.Name = "btnHelp";
			this.btnHelp.Click += new System.EventHandler(this.btnHelp_Click);
			//
			// optUse2
			//
			resources.ApplyResources(this.optUse2, "optUse2");
			this.optUse2.Name = "optUse2";
			//
			// optUse1
			//
			resources.ApplyResources(this.optUse1, "optUse1");
			this.optUse1.Name = "optUse1";
			//
			// ConfirmOverlappingFileReplaceDialog
			//
			this.AcceptButton = this.btnOk;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.btnCancel;
			this.Controls.Add(this.optUse1);
			this.Controls.Add(this.optUse2);
			this.Controls.Add(this.btnHelp);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnOk);
			this.Controls.Add(label8);
			this.Controls.Add(this.lblFileModified1);
			this.Controls.Add(this.lblFileSize1);
			this.Controls.Add(this.lblReferenceRange1);
			this.Controls.Add(this.lblFilePath1);
			this.Controls.Add(this.lblFileModified2);
			this.Controls.Add(this.lblFileSize2);
			this.Controls.Add(this.lblReferenceRange2);
			this.Controls.Add(this.lblFilePath2);
			this.Controls.Add(this.pictureBox3);
			this.Controls.Add(this.pictureBox2);
			this.Controls.Add(label1);
			this.Controls.Add(this.pictureBox1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Name = "ConfirmOverlappingFileReplaceDialog";
			this.Load += new System.EventHandler(this.ConfirmOverlappingFileReplaceDialog_Load);
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox3)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion

		#region Event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void ConfirmOverlappingFileReplaceDialog_Load(object sender, System.EventArgs e)
		{
			if (DesignMode)
				return;

			// fill in the images
			pictureBox1.Image = m_fileImages.Images[0];
			pictureBox2.Image = pictureBox3.Image = m_fileImages.Images[1];

			// fill in file1 information
			FileInfo fileInfo1 = new FileInfo(m_file1.FileName);
			optUse1.Text = fileInfo1.Name;
			lblFilePath1.Text = String.Empty;
			m_filePath1 = fileInfo1.DirectoryName;
			lblFileModified1.Text = string.Format(m_formatModifiedDate,
				fileInfo1.LastWriteTime.ToString("g"));
			lblReferenceRange1.Text = m_file1.ReferenceRangeAsString;
			lblFileSize1.Text = string.Format(m_formatByteCount, fileInfo1.Length);

			// fill in file2 information
			FileInfo fileInfo2 = new FileInfo(m_file2.FileName);
			optUse2.Text = fileInfo2.Name;
			lblFilePath2.Text = String.Empty;
			m_filePath2 = fileInfo2.DirectoryName;
			lblFileModified2.Text = string.Format(m_formatModifiedDate,
				fileInfo2.LastWriteTime.ToString("g"));
			lblReferenceRange2.Text = m_file2.ReferenceRangeAsString;
			lblFileSize2.Text = string.Format(m_formatByteCount, fileInfo2.Length);

			// The default selection is to choose file 1.
			optUse1.Checked = true;
		}

		/// -------------------------------------------------------------------------------
		/// <summary>
		/// This event handler will make sure the file paths are displayed with the
		/// EllipsisPath StringTrimming type.
		/// </summary>
		/// -------------------------------------------------------------------------------
		private void lblFileName_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
		{
			Label lbl = sender as Label;
			// Better be a label.
			if (lbl == null)
				return;

			string sText;
			if (lbl == lblFilePath1)
				sText = m_filePath1;
			else if (lbl == lblFilePath2)
				sText = m_filePath2;
			else
				return;

			if (sText == null || sText == String.Empty)
				return;

			// Draw the string without wrapping and so the filename is the very last thing
			// to get chopped out when text won't fit into a column. Also draw it left
			// aligned (or right with a RTL interface) and vertically
			// centered.
			StringFormat sf = new StringFormat(StringFormatFlags.NoWrap);
			sf.Trimming = StringTrimming.EllipsisPath;
			sf.Alignment = StringAlignment.Near;
			sf.LineAlignment =  StringAlignment.Center;

			e.Graphics.DrawString(sText, lbl.Font, new SolidBrush(lbl.ForeColor),
				lbl.ClientRectangle, sf);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Display help for this dialog.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void btnHelp_Click(object sender, System.EventArgs e)
		{
			ShowHelp.ShowHelpTopic(FwApp.App, "khtpOverlappingFiles");
		}
		#endregion

		#region Private properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return the file that is to be removed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private ScrImportFileInfo FileToRemove
		{
			get
			{
				if (optUse1.Checked)
					return m_file2;
				else
					return m_file1;
			}
		}
		#endregion

		#region IOverlappingFileResolver members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Display the Overlapping files dialog to allow the user to decide which file to
		/// remove from a pair of files which have overlapping references.
		/// </summary>
		/// <param name="file1">file info 1</param>
		/// <param name="file2">file info 2</param>
		/// <returns>The file to remove</returns>
		/// ------------------------------------------------------------------------------------
		public ScrImportFileInfo ChooseFileToRemove(ScrImportFileInfo file1, ScrImportFileInfo file2)
		{
			m_file1 = file1;
			m_file2 = file2;
			try
			{
				if (ShowDialog() == DialogResult.OK)
					return FileToRemove;
			}
			finally
			{
				m_file1 = null;
				m_file2 = null;
			}
			throw new CancelException("Import fCanceled by user.");
		}
		#endregion
	}
}

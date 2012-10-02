// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: Form1.cs
// Responsibility: DavidO
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.IO;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.Resources;
using ICSharpCode.SharpZipLib.Zip;

namespace ZipToResx
{
	/// <summary>
	/// Summary description for Form1.
	/// </summary>
	public class Form1 : System.Windows.Forms.Form
	{
		private System.Windows.Forms.TextBox txtZipFile;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox txtResxFile;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.TextBox txtResxTag;
		private System.Windows.Forms.Button btnMakeResx;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.OpenFileDialog OFDlg;
		private System.Windows.Forms.Button btnBrowseForZip;
		private System.Windows.Forms.Button btnBrowseForResx;
		private System.Windows.Forms.SaveFileDialog SFDlg;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="Form1"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public Form1()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged
		/// resources; <c>false</c> to release only unmanaged resources.
		/// </param>
		/// -----------------------------------------------------------------------------------
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			this.txtZipFile = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.btnBrowseForZip = new System.Windows.Forms.Button();
			this.btnBrowseForResx = new System.Windows.Forms.Button();
			this.label2 = new System.Windows.Forms.Label();
			this.txtResxFile = new System.Windows.Forms.TextBox();
			this.label3 = new System.Windows.Forms.Label();
			this.txtResxTag = new System.Windows.Forms.TextBox();
			this.btnMakeResx = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.OFDlg = new System.Windows.Forms.OpenFileDialog();
			this.SFDlg = new System.Windows.Forms.SaveFileDialog();
			this.SuspendLayout();
			//
			// txtZipFile
			//
			this.txtZipFile.Location = new System.Drawing.Point(136, 19);
			this.txtZipFile.Name = "txtZipFile";
			this.txtZipFile.Size = new System.Drawing.Size(211, 20);
			this.txtZipFile.TabIndex = 0;
			this.txtZipFile.Text = "";
			//
			// label1
			//
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 22);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(45, 13);
			this.label1.TabIndex = 1;
			this.label1.Text = "Zip File:";
			//
			// btnBrowseForZip
			//
			this.btnBrowseForZip.Location = new System.Drawing.Point(351, 19);
			this.btnBrowseForZip.Name = "btnBrowseForZip";
			this.btnBrowseForZip.Size = new System.Drawing.Size(67, 20);
			this.btnBrowseForZip.TabIndex = 2;
			this.btnBrowseForZip.Text = "Browse...";
			this.btnBrowseForZip.Click += new System.EventHandler(this.BrowseClick);
			//
			// btnBrowseForResx
			//
			this.btnBrowseForResx.Location = new System.Drawing.Point(351, 47);
			this.btnBrowseForResx.Name = "btnBrowseForResx";
			this.btnBrowseForResx.Size = new System.Drawing.Size(67, 20);
			this.btnBrowseForResx.TabIndex = 5;
			this.btnBrowseForResx.Text = "Browse...";
			this.btnBrowseForResx.Click += new System.EventHandler(this.BrowseClick);
			//
			// label2
			//
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(12, 50);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(55, 13);
			this.label2.TabIndex = 4;
			this.label2.Text = "Resx File:";
			//
			// txtResxFile
			//
			this.txtResxFile.Location = new System.Drawing.Point(136, 47);
			this.txtResxFile.Name = "txtResxFile";
			this.txtResxFile.Size = new System.Drawing.Size(211, 20);
			this.txtResxFile.TabIndex = 3;
			this.txtResxFile.Text = "";
			//
			// label3
			//
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(12, 77);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(53, 13);
			this.label3.TabIndex = 7;
			this.label3.Text = "Resx Tag";
			//
			// txtResxTag
			//
			this.txtResxTag.Location = new System.Drawing.Point(136, 74);
			this.txtResxTag.Name = "txtResxTag";
			this.txtResxTag.Size = new System.Drawing.Size(211, 20);
			this.txtResxTag.TabIndex = 6;
			this.txtResxTag.Text = "";
			//
			// btnMakeResx
			//
			this.btnMakeResx.Location = new System.Drawing.Point(125, 123);
			this.btnMakeResx.Name = "btnMakeResx";
			this.btnMakeResx.Size = new System.Drawing.Size(104, 25);
			this.btnMakeResx.TabIndex = 8;
			this.btnMakeResx.Text = "Make Resx File";
			this.btnMakeResx.Click += new System.EventHandler(this.btnMakeResx_Click);
			//
			// btnCancel
			//
			this.btnCancel.Location = new System.Drawing.Point(235, 123);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(104, 25);
			this.btnCancel.TabIndex = 9;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
			//
			// OFDlg
			//
			this.OFDlg.DefaultExt = "*.zip";
			this.OFDlg.Filter = "Zip files (*.zip)|*.zip|All files (*.*)|*.*";
			this.OFDlg.Title = "Browse for Zip file";
			//
			// SFDlg
			//
			this.SFDlg.DefaultExt = "resx";
			this.SFDlg.Filter = "Resx files (*.resx)|*.resx";
			this.SFDlg.Title = "Specify Resx file";
			//
			// Form1
			//
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(464, 166);
			this.Controls.AddRange(new System.Windows.Forms.Control[] {
																		  this.btnCancel,
																		  this.btnMakeResx,
																		  this.label3,
																		  this.label2,
																		  this.label1,
																		  this.txtResxTag,
																		  this.btnBrowseForResx,
																		  this.txtResxFile,
																		  this.btnBrowseForZip,
																		  this.txtZipFile});
			this.Name = "Form1";
			this.Text = "Form1";
			this.ResumeLayout(false);

		}
		#endregion

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[STAThread]
		static void Main(string[] args)
		{
			if (args != null && args.Length > 0)
			{
				if (args[0] == "/u" || args[0] == "-u")
				{
					if (args.Length < 3)
					{
						System.Console.WriteLine("Usage: /u <resxfile> <unzipdir> - unpacks the zipfile that is in resx to unzipdir");
						return;
					}

					string zipfile = args[1];
					string location;
					if (args.Length == 2)
						location = Path.GetDirectoryName(zipfile);
					else
						location = args[2];
					UnpackFile(zipfile, location);
					return;
				}
				else if (args[0] == "/s" || args[0] =="-s")
				{
					if (args.Length < 2)
					{
						System.Console.WriteLine("/s <resxfile> <dir> - restores the contents of resxfile to dir");
						return;
					}

					string resxfile = args[1];
					string location;
					if (args.Length == 2)
						location = Path.GetDirectoryName(resxfile);
					else
						location = args[2];
					Unstream(resxfile, location);
					return;
				}
				else if (args[0] == "/t" || args[0] == "-t")
				{
					if (args.Length < 2)
					{
						System.Console.WriteLine("/t <resxfile>  - tests the resxfile");
						return;
					}

					string resxfile = args[1];
					if (!ZipFileOk(resxfile))
						System.Console.WriteLine("Zipped file is corrupt");
					return;
				}
			}


			Application.Run(new Form1());
		}

		private void btnMakeResx_Click(object sender, System.EventArgs e)
		{
			if (txtResxFile.Text.Length == 0 || txtZipFile.Text.Length == 0 ||
				txtResxTag.Text.Length == 0)
			{
				MessageBox.Show("You have left some of the fields empty. FIX THEM!!!");
				return;
			}

			if (!ZipFileOk(txtZipFile.Text))
			{
				MessageBox.Show("Zip file corrupt - try zipping with different tool!");
				return;
			}
			FileStream fs = new FileStream(txtZipFile.Text, System.IO.FileMode.Open);
			BinaryReader br = new BinaryReader(fs, System.Text.Encoding.ASCII);
			byte[] buff = br.ReadBytes((int)fs.Length);
			fs.Close();

			ResXResourceWriter resource = new ResXResourceWriter(txtResxFile.Text);
			resource.AddResource(txtResxTag.Text, buff);
			resource.Generate();
			resource.Close();

			MessageBox.Show("You're done, so quit the program already.");
		}

		private void BrowseClick(object sender, System.EventArgs e)
		{
			if (sender == btnBrowseForResx)
			{
				if (SFDlg.ShowDialog() != DialogResult.Cancel)
					txtResxFile.Text = SFDlg.FileName;
			}
			else
			{
				if (OFDlg.ShowDialog() != DialogResult.Cancel)
					txtZipFile.Text = OFDlg.FileName;
			}
		}

		private void btnCancel_Click(object sender, System.EventArgs e)
		{
			this.Close();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check the zip file and see if we could unzip it or if there are any errors.
		/// </summary>
		/// <param name="fileName">Name of zip file</param>
		/// <returns><c>true</c> if zip file is ok, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		private static bool ZipFileOk(string fileName)
		{
			try
			{
				FileStream fs = new FileStream(fileName, System.IO.FileMode.Open);
				ZipInputStream zipStream = new ZipInputStream(fs);

				ZipEntry zipEntry;				while ((zipEntry = zipStream.GetNextEntry()) != null)
				{					byte[] data = new byte[2048];
					while (true)
					{						int size = zipStream.Read(data, 0, data.Length);						if (size > 0)
						{						}
						else
						{							break;						}					}				}				zipStream.Close();
				fs.Close();
			}
			catch(Exception e)
			{
				System.Console.Error.WriteLine("Got exception: {0} while testing {1}",
					e.Message, fileName);
				return false;
			}
			return true;
		}

		private static void UnpackFile(string packedProject, string unpackLocation)
		{
			try
			{
				// Create our test folder below the temp folder.
				Directory.CreateDirectory(unpackLocation + @"\");
			}
			catch
			{
			}

			try
			{
				// Read the file and unpack it
				FileStream fs = new FileStream(packedProject, System.IO.FileMode.Open);
				ZipInputStream zipStream = new ZipInputStream(fs);

				ZipEntry zipEntry;				while ((zipEntry = zipStream.GetNextEntry()) != null)
				{					string directoryName = Path.GetDirectoryName(zipEntry.Name);					string fileName      = Path.GetFileName(zipEntry.Name);
					// create directory					DirectoryInfo currDir = Directory.CreateDirectory(Path.Combine(unpackLocation, directoryName));
					if (fileName != null && fileName.Length != 0)
					{						FileInfo fi = new FileInfo(Path.Combine(currDir.FullName, fileName));						FileStream streamWriter = fi.Create();						int size = 2048;						byte[] data = new byte[2048];
						while (true)
						{							size = zipStream.Read(data, 0, data.Length);							if (size > 0)
							{								streamWriter.Write(data, 0, size);							}
							else
							{								break;							}						}
						streamWriter.Close();						fi.LastWriteTime = zipEntry.DateTime;					}				}				zipStream.Close();
				fs.Close();
			}
			catch(Exception e)
			{
				System.Console.Error.WriteLine("Got exception: {0} while unpacking {1}",
					e.Message, packedProject);
			}
		}

		private static void Unstream(string resxFile, string unpackLocation)
		{
			// Read the binary data from the resource file
			ResXResourceReader resource = new ResXResourceReader(resxFile);
			foreach (DictionaryEntry entry in resource)
			{
				string filename = (string)entry.Key;
				object val = entry.Value;
				if (val is byte[])
				{
					byte[] content = (byte[])val;

					// Write the file to our temp test folder.
					string fsName = Path.ChangeExtension(Path.Combine(unpackLocation, filename), ".zip");
					BinaryWriter bw =
						new BinaryWriter(new FileStream(fsName, System.IO.FileMode.Create),
						System.Text.Encoding.ASCII);

					bw.Write(content);
					bw.Flush();
					bw.Close();
				}
			}
		}

	}
}

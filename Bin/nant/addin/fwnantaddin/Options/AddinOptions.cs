using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.CommandBars;

namespace FwNantAddin2
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for AddinOptions.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class AddinOptions : System.Windows.Forms.UserControl, IDTToolsOptionsPage
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
		private System.Windows.Forms.ListBox baseDirectories;
		private System.Windows.Forms.Label lblBuildFile;
		private System.Windows.Forms.Label lblBaseDirs;
		private System.Windows.Forms.TextBox buildFile;
		private System.Windows.Forms.Button addBaseDir;
		private System.Windows.Forms.Button removeBaseDir;
		private System.Windows.Forms.TextBox edtAddBaseDir;
		private System.Windows.Forms.Button chooseDir;
		private System.Windows.Forms.Button moveUp;
		private System.Windows.Forms.Button moveDown;
		private DTE m_dte;
		private Label lblNantPath;
		private TextBox edtNantPath;
		private Button btnChooserNant;
		private OpenFileDialog openFileDialog;
		private FolderBrowserDialog folderBrowserDialog;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:AddinOptions"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public AddinOptions()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			lock (this)
			{
				edtNantPath.Text = Settings.Default.NantPath;
				buildFile.Text = Settings.Default.BuildFile;
				baseDirectories.Items.Clear();
				if (Settings.Default.BaseDirectories != null)
				{
					foreach (string s in Settings.Default.BaseDirectories)
						baseDirectories.Items.Add(s);
				}
				else
					Settings.Default.BaseDirectories = new StringCollection();

				if (baseDirectories.Items.Count == 0)
					baseDirectories.Items.Add(@"c:\fw");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">if set to <c>true</c> [disposing].</param>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Component Designer generated code
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			this.buildFile = new System.Windows.Forms.TextBox();
			this.lblBuildFile = new System.Windows.Forms.Label();
			this.baseDirectories = new System.Windows.Forms.ListBox();
			this.lblBaseDirs = new System.Windows.Forms.Label();
			this.addBaseDir = new System.Windows.Forms.Button();
			this.removeBaseDir = new System.Windows.Forms.Button();
			this.edtAddBaseDir = new System.Windows.Forms.TextBox();
			this.chooseDir = new System.Windows.Forms.Button();
			this.moveUp = new System.Windows.Forms.Button();
			this.moveDown = new System.Windows.Forms.Button();
			this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
			this.lblNantPath = new System.Windows.Forms.Label();
			this.edtNantPath = new System.Windows.Forms.TextBox();
			this.btnChooserNant = new System.Windows.Forms.Button();
			this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
			this.SuspendLayout();
			//
			// buildFile
			//
			this.buildFile.Location = new System.Drawing.Point(16, 32);
			this.buildFile.Name = "buildFile";
			this.buildFile.Size = new System.Drawing.Size(296, 20);
			this.buildFile.TabIndex = 1;
			//
			// lblBuildFile
			//
			this.lblBuildFile.Location = new System.Drawing.Point(16, 8);
			this.lblBuildFile.Name = "lblBuildFile";
			this.lblBuildFile.Size = new System.Drawing.Size(296, 23);
			this.lblBuildFile.TabIndex = 0;
			this.lblBuildFile.Text = "Buildfile (may have relative path)";
			this.lblBuildFile.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
			//
			// baseDirectories
			//
			this.baseDirectories.Location = new System.Drawing.Point(16, 96);
			this.baseDirectories.Name = "baseDirectories";
			this.baseDirectories.Size = new System.Drawing.Size(296, 69);
			this.baseDirectories.TabIndex = 5;
			this.baseDirectories.DoubleClick += new System.EventHandler(this.OnEditBaseDir);
			//
			// lblBaseDirs
			//
			this.lblBaseDirs.Location = new System.Drawing.Point(16, 56);
			this.lblBaseDirs.Name = "lblBaseDirs";
			this.lblBaseDirs.Size = new System.Drawing.Size(296, 16);
			this.lblBaseDirs.TabIndex = 2;
			this.lblBaseDirs.Text = "Base directories";
			this.lblBaseDirs.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
			//
			// addBaseDir
			//
			this.addBaseDir.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.addBaseDir.Location = new System.Drawing.Point(320, 72);
			this.addBaseDir.Name = "addBaseDir";
			this.addBaseDir.Size = new System.Drawing.Size(75, 23);
			this.addBaseDir.TabIndex = 6;
			this.addBaseDir.Text = "&Add";
			this.addBaseDir.Click += new System.EventHandler(this.OnAddBaseDir);
			//
			// removeBaseDir
			//
			this.removeBaseDir.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.removeBaseDir.Location = new System.Drawing.Point(320, 96);
			this.removeBaseDir.Name = "removeBaseDir";
			this.removeBaseDir.Size = new System.Drawing.Size(75, 23);
			this.removeBaseDir.TabIndex = 7;
			this.removeBaseDir.Text = "&Remove";
			this.removeBaseDir.Click += new System.EventHandler(this.OnRemoveBaseDir);
			//
			// edtAddBaseDir
			//
			this.edtAddBaseDir.Location = new System.Drawing.Point(16, 72);
			this.edtAddBaseDir.Name = "edtAddBaseDir";
			this.edtAddBaseDir.Size = new System.Drawing.Size(264, 20);
			this.edtAddBaseDir.TabIndex = 3;
			//
			// chooseDir
			//
			this.chooseDir.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.chooseDir.Location = new System.Drawing.Point(290, 72);
			this.chooseDir.Name = "chooseDir";
			this.chooseDir.Size = new System.Drawing.Size(22, 20);
			this.chooseDir.TabIndex = 4;
			this.chooseDir.Text = "...";
			this.chooseDir.Click += new System.EventHandler(this.OnSelectBaseDir);
			//
			// moveUp
			//
			this.moveUp.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.moveUp.Location = new System.Drawing.Point(320, 120);
			this.moveUp.Name = "moveUp";
			this.moveUp.Size = new System.Drawing.Size(75, 23);
			this.moveUp.TabIndex = 8;
			this.moveUp.Text = "&Up";
			this.moveUp.Click += new System.EventHandler(this.OnMoveUp);
			//
			// moveDown
			//
			this.moveDown.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.moveDown.Location = new System.Drawing.Point(320, 144);
			this.moveDown.Name = "moveDown";
			this.moveDown.Size = new System.Drawing.Size(75, 23);
			this.moveDown.TabIndex = 9;
			this.moveDown.Text = "&Down";
			this.moveDown.Click += new System.EventHandler(this.OnMoveDown);
			//
			// lblNantPath
			//
			this.lblNantPath.AutoSize = true;
			this.lblNantPath.Location = new System.Drawing.Point(16, 172);
			this.lblNantPath.Name = "lblNantPath";
			this.lblNantPath.Size = new System.Drawing.Size(68, 13);
			this.lblNantPath.TabIndex = 10;
			this.lblNantPath.Text = "Path to NAnt";
			//
			// edtNantPath
			//
			this.edtNantPath.Location = new System.Drawing.Point(16, 189);
			this.edtNantPath.Name = "edtNantPath";
			this.edtNantPath.Size = new System.Drawing.Size(264, 20);
			this.edtNantPath.TabIndex = 11;
			//
			// btnChooserNant
			//
			this.btnChooserNant.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.btnChooserNant.Location = new System.Drawing.Point(290, 189);
			this.btnChooserNant.Name = "btnChooserNant";
			this.btnChooserNant.Size = new System.Drawing.Size(22, 20);
			this.btnChooserNant.TabIndex = 12;
			this.btnChooserNant.Text = "...";
			this.btnChooserNant.Click += new System.EventHandler(this.OnSelectNantPath);
			//
			// openFileDialog
			//
			this.openFileDialog.AddExtension = false;
			this.openFileDialog.FileName = "nant.exe";
			this.openFileDialog.Filter = "NAnt|nant.exe";
			this.openFileDialog.Title = "Chose NAnt path";
			this.openFileDialog.FileOk += new System.ComponentModel.CancelEventHandler(this.OnNAntPathOk);
			//
			// AddinOptions
			//
			this.Controls.Add(this.edtNantPath);
			this.Controls.Add(this.lblNantPath);
			this.Controls.Add(this.lblBuildFile);
			this.Controls.Add(this.buildFile);
			this.Controls.Add(this.lblBaseDirs);
			this.Controls.Add(this.edtAddBaseDir);
			this.Controls.Add(this.btnChooserNant);
			this.Controls.Add(this.chooseDir);
			this.Controls.Add(this.baseDirectories);
			this.Controls.Add(this.addBaseDir);
			this.Controls.Add(this.removeBaseDir);
			this.Controls.Add(this.moveUp);
			this.Controls.Add(this.moveDown);
			this.Name = "AddinOptions";
			this.Size = new System.Drawing.Size(416, 231);
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		#region Implementation of IDTToolsOptionsPage interface

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the properties.
		/// </summary>
		/// <param name="propertiesObject">The properties object.</param>
		/// ------------------------------------------------------------------------------------------
		void IDTToolsOptionsPage.GetProperties(ref object propertiesObject)
		{
			propertiesObject = null;
		}


		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Called after handle to options page was created.
		/// </summary>
		/// <param name="dte">The DTE.</param>
		/// ------------------------------------------------------------------------------------------
		void IDTToolsOptionsPage.OnAfterCreated(DTE dte)
		{
			m_dte = dte;
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Called when options dialog was cancelled.
		/// </summary>
		/// ------------------------------------------------------------------------------------------
		void IDTToolsOptionsPage.OnCancel()
		{
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Called when Help button is pressed.
		/// </summary>
		/// ------------------------------------------------------------------------------------------
		void IDTToolsOptionsPage.OnHelp()
		{
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Called when options dialog gets closed with OK button.
		/// </summary>
		/// ------------------------------------------------------------------------------------------
		void IDTToolsOptionsPage.OnOK()
		{
			try
			{
				Settings.Default.NantPath = edtNantPath.Text;
				Settings.Default.BuildFile = buildFile.Text;
				Settings.Default.BaseDirectories.Clear();
				foreach (string s in baseDirectories.Items)
					Settings.Default.BaseDirectories.Add(s);
				Settings.Default.Save();
			}
			catch(Exception e)
			{
				System.Diagnostics.Debug.WriteLine(e.Message);
			}
		}

		#endregion

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Called to add a new base directory
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------------
		private void OnAddBaseDir(object sender, EventArgs e)
		{
			if (edtAddBaseDir.Text.Length > 0)
			{
				string dir = Path.GetFullPath(Path.Combine(edtAddBaseDir.Text, @"."));
				if (!baseDirectories.Items.Contains(dir))
				{
					if (Directory.Exists(dir))
						baseDirectories.Items.Add(dir);
					else
						MessageBox.Show(string.Format("Directory {0} doesn't exist", dir));
				}
				edtAddBaseDir.Clear();
			}
			edtAddBaseDir.Focus();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called to remove a base directory
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void OnRemoveBaseDir(object sender, System.EventArgs e)
		{
			if (baseDirectories.SelectedIndex > -1)
			{
				baseDirectories.Items.RemoveAt(baseDirectories.SelectedIndex);
				edtAddBaseDir.Clear();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Modify the base directory
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void OnEditBaseDir(object sender, System.EventArgs e)
		{
			edtAddBaseDir.Text = (string)baseDirectories.SelectedItem;
			edtAddBaseDir.Focus();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// User clicked on a base directory
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void OnSelectBaseDir(object sender, System.EventArgs e)
		{
			if (edtAddBaseDir.Text.Length > 0)
				folderBrowserDialog.SelectedPath = edtAddBaseDir.Text;
			else
				folderBrowserDialog.SelectedPath = @"c:\";
			DialogResult result = folderBrowserDialog.ShowDialog();
			if (result == DialogResult.OK)
			{
				edtAddBaseDir.Text = folderBrowserDialog.SelectedPath;
				OnAddBaseDir(null, null);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Move the base directory up one line.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void OnMoveUp(object sender, System.EventArgs e)
		{
			if (baseDirectories.SelectedIndex > 0)
			{
				string dir = (string)baseDirectories.SelectedItem;
				int index = baseDirectories.SelectedIndex;
				baseDirectories.Items.RemoveAt(index);
				baseDirectories.Items.Insert(index-1, dir);
				baseDirectories.SelectedIndex = index-1;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Move the base directory down one line
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void OnMoveDown(object sender, System.EventArgs e)
		{
			if (baseDirectories.SelectedIndex > -1 &&
				baseDirectories.SelectedIndex < baseDirectories.Items.Count - 1)
			{
				string dir = (string)baseDirectories.SelectedItem;
				int index = baseDirectories.SelectedIndex;
				baseDirectories.Items.RemoveAt(index);
				baseDirectories.Items.Insert(index+1, dir);
				baseDirectories.SelectedIndex = index+1;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the buildfile.
		/// </summary>
		/// <value>The buildfile.</value>
		/// ------------------------------------------------------------------------------------
		public string Buildfile
		{
			get { return buildFile.Text; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the base directories.
		/// </summary>
		/// <value>The base directories.</value>
		/// ------------------------------------------------------------------------------------
		public string[] BaseDirectories
		{
			get
			{
				string[] dirs = new string[baseDirectories.Items.Count];
				int i = 0;
				foreach(string str in baseDirectories.Items)
					dirs[i++] = str;
				return dirs;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the user presses the select directory button to choose the path to
		/// NAnt.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void OnSelectNantPath(object sender, EventArgs e)
		{
			if (edtNantPath.Text.Length > 0)
				openFileDialog.InitialDirectory = edtNantPath.Text;
			else
				openFileDialog.InitialDirectory = @"c:\fw\bin\nant\bin";
			DialogResult result = openFileDialog.ShowDialog();
			if (result == DialogResult.OK)
				edtNantPath.Text = Path.GetDirectoryName(openFileDialog.FileName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called to check the path the user selected in the NAnt path dialog
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.ComponentModel.CancelEventArgs"/> instance
		/// containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void OnNAntPathOk(object sender, CancelEventArgs e)
		{
			if (!File.Exists(Path.Combine(Path.GetDirectoryName(openFileDialog.FileName),
				"nant.exe")))
			{
				MessageBox.Show("NAnt.exe doesn't exist in directory\n" +
					Path.GetDirectoryName(openFileDialog.FileName), "Can't find nant.exe");
				e.Cancel = true;
			}

		}
	}
}

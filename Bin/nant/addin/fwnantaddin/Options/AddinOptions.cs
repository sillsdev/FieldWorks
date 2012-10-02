using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using EnvDTE;

namespace FwNantAddin2
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for AddinOptions.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class AddinOptions : UserControl, IDTToolsOptionsPage
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private Container components = new Container();
		private ListBox baseDirectories;
		private TextBox buildFile;
		private Button addBaseDir;
		private Button removeBaseDir;
		private TextBox edtAddBaseDir;
		private Button chooseDir;
		private Button moveUp;
		private Button moveDown;
		private DTE m_dte;
		private TextBox edtNantPath;
		private Button btnChooserNant;
		private OpenFileDialog openFileDialog;
		private TextBox edtTargetFramework;
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
				edtTargetFramework.Text = string.IsNullOrEmpty(Settings.Default.TargetFramework) ?
					"mono-3.5" : Settings.Default.TargetFramework;
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
			Label lblBuildFile;
			Label lblBaseDirs;
			Label lblNantPath;
			Label label1;
			this.buildFile = new TextBox();
			this.baseDirectories = new ListBox();
			this.addBaseDir = new Button();
			this.removeBaseDir = new Button();
			this.edtAddBaseDir = new TextBox();
			this.chooseDir = new Button();
			this.moveUp = new Button();
			this.moveDown = new Button();
			this.folderBrowserDialog = new FolderBrowserDialog();
			this.edtNantPath = new TextBox();
			this.btnChooserNant = new Button();
			this.openFileDialog = new OpenFileDialog();
			this.edtTargetFramework = new TextBox();
			lblBuildFile = new Label();
			lblBaseDirs = new Label();
			lblNantPath = new Label();
			label1 = new Label();
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
			lblBuildFile.Location = new System.Drawing.Point(16, 8);
			lblBuildFile.Name = "lblBuildFile";
			lblBuildFile.Size = new System.Drawing.Size(296, 23);
			lblBuildFile.TabIndex = 0;
			lblBuildFile.Text = "Buildfile (may have relative path)";
			lblBuildFile.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
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
			lblBaseDirs.Location = new System.Drawing.Point(16, 56);
			lblBaseDirs.Name = "lblBaseDirs";
			lblBaseDirs.Size = new System.Drawing.Size(296, 16);
			lblBaseDirs.TabIndex = 2;
			lblBaseDirs.Text = "Base directories";
			lblBaseDirs.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
			//
			// addBaseDir
			//
			this.addBaseDir.FlatStyle = FlatStyle.System;
			this.addBaseDir.Location = new System.Drawing.Point(320, 72);
			this.addBaseDir.Name = "addBaseDir";
			this.addBaseDir.Size = new System.Drawing.Size(75, 23);
			this.addBaseDir.TabIndex = 6;
			this.addBaseDir.Text = "&Add";
			this.addBaseDir.Click += new System.EventHandler(this.OnAddBaseDir);
			//
			// removeBaseDir
			//
			this.removeBaseDir.FlatStyle = FlatStyle.System;
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
			this.chooseDir.FlatStyle = FlatStyle.System;
			this.chooseDir.Location = new System.Drawing.Point(290, 72);
			this.chooseDir.Name = "chooseDir";
			this.chooseDir.Size = new System.Drawing.Size(22, 20);
			this.chooseDir.TabIndex = 4;
			this.chooseDir.Text = "...";
			this.chooseDir.Click += new System.EventHandler(this.OnSelectBaseDir);
			//
			// moveUp
			//
			this.moveUp.FlatStyle = FlatStyle.System;
			this.moveUp.Location = new System.Drawing.Point(320, 120);
			this.moveUp.Name = "moveUp";
			this.moveUp.Size = new System.Drawing.Size(75, 23);
			this.moveUp.TabIndex = 8;
			this.moveUp.Text = "&Up";
			this.moveUp.Click += new System.EventHandler(this.OnMoveUp);
			//
			// moveDown
			//
			this.moveDown.FlatStyle = FlatStyle.System;
			this.moveDown.Location = new System.Drawing.Point(320, 144);
			this.moveDown.Name = "moveDown";
			this.moveDown.Size = new System.Drawing.Size(75, 23);
			this.moveDown.TabIndex = 9;
			this.moveDown.Text = "&Down";
			this.moveDown.Click += new System.EventHandler(this.OnMoveDown);
			//
			// lblNantPath
			//
			lblNantPath.AutoSize = true;
			lblNantPath.Location = new System.Drawing.Point(16, 172);
			lblNantPath.Name = "lblNantPath";
			lblNantPath.Size = new System.Drawing.Size(68, 13);
			lblNantPath.TabIndex = 10;
			lblNantPath.Text = "Path to NAnt";
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
			this.btnChooserNant.FlatStyle = FlatStyle.System;
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
			// label1
			//
			label1.AutoSize = true;
			label1.Location = new System.Drawing.Point(19, 216);
			label1.Name = "label1";
			label1.Size = new System.Drawing.Size(288, 13);
			label1.TabIndex = 13;
			label1.Text = "Target Framework (see NAnt.exe.config for possible values)";
			//
			// edtTargetFramework
			//
			this.edtTargetFramework.Location = new System.Drawing.Point(16, 233);
			this.edtTargetFramework.Name = "edtTargetFramework";
			this.edtTargetFramework.Size = new System.Drawing.Size(296, 20);
			this.edtTargetFramework.TabIndex = 14;
			//
			// AddinOptions
			//
			this.Controls.Add(this.edtTargetFramework);
			this.Controls.Add(label1);
			this.Controls.Add(this.edtNantPath);
			this.Controls.Add(lblNantPath);
			this.Controls.Add(lblBuildFile);
			this.Controls.Add(this.buildFile);
			this.Controls.Add(lblBaseDirs);
			this.Controls.Add(this.edtAddBaseDir);
			this.Controls.Add(this.btnChooserNant);
			this.Controls.Add(this.chooseDir);
			this.Controls.Add(this.baseDirectories);
			this.Controls.Add(this.addBaseDir);
			this.Controls.Add(this.removeBaseDir);
			this.Controls.Add(this.moveUp);
			this.Controls.Add(this.moveDown);
			this.Name = "AddinOptions";
			this.Size = new System.Drawing.Size(416, 273);
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
				Settings.Default.TargetFramework = edtTargetFramework.Text;
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
		private void OnRemoveBaseDir(object sender, EventArgs e)
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
		private void OnEditBaseDir(object sender, EventArgs e)
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
		private void OnSelectBaseDir(object sender, EventArgs e)
		{
			folderBrowserDialog.SelectedPath = edtAddBaseDir.Text.Length > 0 ? edtAddBaseDir.Text : @"c:\";
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
		private void OnMoveUp(object sender, EventArgs e)
		{
			if (baseDirectories.SelectedIndex > 0)
			{
				var dir = (string)baseDirectories.SelectedItem;
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
		private void OnMoveDown(object sender, EventArgs e)
		{
			if (baseDirectories.SelectedIndex > -1 &&
				baseDirectories.SelectedIndex < baseDirectories.Items.Count - 1)
			{
				var dir = (string)baseDirectories.SelectedItem;
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
				var dirs = new string[baseDirectories.Items.Count];
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
			openFileDialog.InitialDirectory = edtNantPath.Text.Length > 0 ? edtNantPath.Text : @"c:\fw\Bin\nant\bin";
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

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
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.Diagnostics;
using System.Xml;
using System.IO;


namespace P4Helper
{
	/// <summary>
	/// Summary description for Form1.
	/// </summary>
	public class P4HelperForm : System.Windows.Forms.Form
	{
		protected PerforceUtils m_p4;
		protected System.Collections.Specialized.StringCollection m_skipDirs;
		protected System.Collections.Specialized.StringCollection m_skipNameParts;

		protected XmlDocument m_configDocument;


		private System.Windows.Forms.ColumnHeader FileName;
		private System.Windows.Forms.ColumnHeader Folder;
		private System.Windows.Forms.ColumnHeader ModifiedDate;
		private System.Windows.Forms.MainMenu mainMenu1;
		private System.Windows.Forms.MenuItem menuItem1;
		private System.Windows.Forms.MenuItem menuItem2;
		private System.Windows.Forms.MenuItem mnuRefresh;
		private System.Windows.Forms.ListView m_list;
		private System.Windows.Forms.ColumnHeader m_attributesColumn;
		private System.Windows.Forms.ImageList imageList1;
		private System.Windows.Forms.MenuItem mnuRefreshP4;
		private System.Windows.Forms.MenuItem mnuCheckOut;
		private System.Windows.Forms.TextBox m_console;
		private System.Windows.Forms.ToolBar toolBar1;
		private System.Windows.Forms.ToolBarButton btnEdit;
		private System.Windows.Forms.ToolBarButton btnAdd;
		private System.Windows.Forms.ToolBarButton btnRefresh;
		private System.Windows.Forms.ToolBarButton btnRefreshP4;
		private System.Windows.Forms.ToolBarButton tbbSep1;
		private System.Windows.Forms.Splitter splitter1;
		private System.Windows.Forms.ContextMenu m_contextMenu;
		private System.Windows.Forms.MenuItem mnuReadOnly;
		private System.Windows.Forms.MenuItem mnuExplorer;
		private System.Windows.Forms.Timer m_timerStartup;
		private System.Windows.Forms.MenuItem mnuAddToP4;
		private System.Windows.Forms.MenuItem mnuDiff;
		private System.Windows.Forms.MenuItem menuItem3;
		private System.Windows.Forms.MenuItem mnuDelete;
		private System.Windows.Forms.MenuItem mnuChkOut;
		private System.Windows.Forms.MenuItem menuItem5;
		private System.Windows.Forms.MenuItem mnExcludeFile;
		private System.Windows.Forms.MenuItem mnuExcludeDir;
		private System.Windows.Forms.MenuItem menuItem4;
		private System.Windows.Forms.MenuItem menuItem6;
		private System.Windows.Forms.MenuItem mnuSubmitDefault;
		private System.Windows.Forms.ToolBarButton btnValidityCheck;
		private System.ComponentModel.IContainer components;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="Form1"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public P4HelperForm()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			m_p4 = new PerforceUtils(m_console);

			m_skipDirs = new System.Collections.Specialized.StringCollection();
			m_skipNameParts = new System.Collections.Specialized.StringCollection();

			LoadConfiguration ();
			LoadFileList();

			m_timerStartup.Enabled= true;
		}

		protected void LoadConfiguration ()
		{
			m_configDocument= new XmlDocument();
			try
			{
				m_configDocument.Load(ConfigFilePath);
				UseConfiguration();

			}
			catch(FileNotFoundException )
			{
				MessageBox.Show("The configuration file was not found at "+ConfigFilePath+".  Creating a new one.");
				BootstrapConfiguration ();
			}
		}

		private void UseConfiguration()
		{
			m_skipDirs.Clear();
			foreach(XmlNode node in  m_configDocument.SelectNodes("//skipDirs/directory"))
			{
				m_skipDirs.Add(node.Attributes["path"].Value );
			}
			m_skipNameParts.Clear();
			foreach(XmlNode node in  m_configDocument.SelectNodes("//skipNameParts/part"))
			{
				m_skipNameParts.Add(node.Attributes["text"].Value );
			}
		}

		protected string ConfigFilePath
		{
			get
			{
				return System.Environment.ExpandEnvironmentVariables(@"%fwroot%/P4HelperConfig.xml");
			}
		}
		protected void BootstrapConfiguration ()
		{
			m_skipDirs.AddRange(new string[] {"output", "obj", "debug", "ww-conchx", "bin", "cominterfaces", "generated", "templates", "modeltesting"});

			m_skipNameParts.AddRange(new string[] {"copy (","copy of", ".user", "thumbs.db", "b4", "generated", ".suo", ".log", ".exe", ".dll", ".ncb", "dbversion"});
			m_skipNameParts.AddRange(new string[] {"cellar.cs", "featsys.cs","langproj.cs","ling.cs","notebk.cs","scripture.cs"});

			SetupConfigurationDocument ();
			foreach(string path in m_skipDirs)
			{
				AddDirectoryPath(path);
			}
			foreach(string pattern in m_skipNameParts)
			{
				AddNamePart(pattern);
			}
			SaveConfiguration();
		}

		protected void SaveConfiguration ()
		{
			m_configDocument.Save(ConfigFilePath);
		}

		protected void SetupConfigurationDocument ()
		{
			m_configDocument = new XmlDocument();
			XmlElement root = (XmlElement)m_configDocument.AppendChild(m_configDocument.CreateElement("P4HelperConfig"));
			XmlElement skips = (XmlElement)root.AppendChild(m_configDocument.CreateElement("skipDirs"));
			XmlElement parts = (XmlElement)root.AppendChild(m_configDocument.CreateElement("skipNameParts"));
		}

		protected void AddDirectoryPath (string path)
		{
			path= path.ToLower();
			XmlElement skips = (XmlElement)m_configDocument.SelectSingleNode("P4HelperConfig/skipDirs");
			XmlElement directory = (XmlElement)skips.AppendChild(m_configDocument.CreateElement("directory"));
			XmlAttribute attribute = directory.Attributes.Append(m_configDocument.CreateAttribute("path"));
			attribute.Value = path;
		}

		protected void AddNamePart (string pattern)
		{
			XmlElement parts = (XmlElement)m_configDocument.SelectSingleNode("P4HelperConfig/skipNameParts");
			XmlElement part = (XmlElement)parts.AppendChild(m_configDocument.CreateElement("part"));
			XmlAttribute attribute = part.Attributes.Append(m_configDocument.CreateAttribute("text"));
			attribute.Value = pattern;
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
			this.components = new System.ComponentModel.Container();
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(P4HelperForm));
			this.m_list = new System.Windows.Forms.ListView();
			this.Folder = new System.Windows.Forms.ColumnHeader();
			this.FileName = new System.Windows.Forms.ColumnHeader();
			this.ModifiedDate = new System.Windows.Forms.ColumnHeader();
			this.m_attributesColumn = new System.Windows.Forms.ColumnHeader();
			this.m_contextMenu = new System.Windows.Forms.ContextMenu();
			this.mnuChkOut = new System.Windows.Forms.MenuItem();
			this.mnuAddToP4 = new System.Windows.Forms.MenuItem();
			this.menuItem5 = new System.Windows.Forms.MenuItem();
			this.mnuExplorer = new System.Windows.Forms.MenuItem();
			this.mnuDiff = new System.Windows.Forms.MenuItem();
			this.menuItem4 = new System.Windows.Forms.MenuItem();
			this.mnExcludeFile = new System.Windows.Forms.MenuItem();
			this.mnuExcludeDir = new System.Windows.Forms.MenuItem();
			this.menuItem6 = new System.Windows.Forms.MenuItem();
			this.menuItem3 = new System.Windows.Forms.MenuItem();
			this.mnuDelete = new System.Windows.Forms.MenuItem();
			this.mnuReadOnly = new System.Windows.Forms.MenuItem();
			this.imageList1 = new System.Windows.Forms.ImageList(this.components);
			this.mainMenu1 = new System.Windows.Forms.MainMenu();
			this.menuItem1 = new System.Windows.Forms.MenuItem();
			this.mnuCheckOut = new System.Windows.Forms.MenuItem();
			this.mnuSubmitDefault = new System.Windows.Forms.MenuItem();
			this.menuItem2 = new System.Windows.Forms.MenuItem();
			this.mnuRefresh = new System.Windows.Forms.MenuItem();
			this.mnuRefreshP4 = new System.Windows.Forms.MenuItem();
			this.m_console = new System.Windows.Forms.TextBox();
			this.toolBar1 = new System.Windows.Forms.ToolBar();
			this.btnEdit = new System.Windows.Forms.ToolBarButton();
			this.btnAdd = new System.Windows.Forms.ToolBarButton();
			this.tbbSep1 = new System.Windows.Forms.ToolBarButton();
			this.btnRefresh = new System.Windows.Forms.ToolBarButton();
			this.btnRefreshP4 = new System.Windows.Forms.ToolBarButton();
			this.splitter1 = new System.Windows.Forms.Splitter();
			this.m_timerStartup = new System.Windows.Forms.Timer(this.components);
			this.btnValidityCheck = new System.Windows.Forms.ToolBarButton();
			this.SuspendLayout();
			//
			// m_list
			//
			this.m_list.AllowDrop = true;
			this.m_list.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
																					 this.Folder,
																					 this.FileName,
																					 this.ModifiedDate,
																					 this.m_attributesColumn});
			this.m_list.ContextMenu = this.m_contextMenu;
			this.m_list.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_list.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this.m_list.LargeImageList = this.imageList1;
			this.m_list.Location = new System.Drawing.Point(0, 28);
			this.m_list.Name = "m_list";
			this.m_list.Size = new System.Drawing.Size(760, 402);
			this.m_list.SmallImageList = this.imageList1;
			this.m_list.Sorting = System.Windows.Forms.SortOrder.Ascending;
			this.m_list.TabIndex = 0;
			this.m_list.View = System.Windows.Forms.View.Details;
			//
			// Folder
			//
			this.Folder.Text = "Folder";
			this.Folder.Width = 200;
			//
			// FileName
			//
			this.FileName.Text = "Name";
			this.FileName.Width = 200;
			//
			// ModifiedDate
			//
			this.ModifiedDate.Text = "Date Modified";
			this.ModifiedDate.Width = 120;
			//
			// m_attributesColumn
			//
			this.m_attributesColumn.Text = "Attributes";
			//
			// m_contextMenu
			//
			this.m_contextMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																						  this.mnuChkOut,
																						  this.mnuAddToP4,
																						  this.menuItem5,
																						  this.mnuExplorer,
																						  this.mnuDiff,
																						  this.menuItem4,
																						  this.mnExcludeFile,
																						  this.mnuExcludeDir,
																						  this.menuItem6,
																						  this.menuItem3});
			this.m_contextMenu.Popup += new System.EventHandler(this.m_contextMenu_Popup);
			//
			// mnuChkOut
			//
			this.mnuChkOut.Index = 0;
			this.mnuChkOut.Text = "Check Out";
			this.mnuChkOut.Click += new System.EventHandler(this.mnuCheckOut_Click);
			//
			// mnuAddToP4
			//
			this.mnuAddToP4.Index = 1;
			this.mnuAddToP4.Text = "Add to P4";
			this.mnuAddToP4.Click += new System.EventHandler(this.mnuAddToP4_Click);
			//
			// menuItem5
			//
			this.menuItem5.Index = 2;
			this.menuItem5.Text = "-";
			//
			// mnuExplorer
			//
			this.mnuExplorer.Index = 3;
			this.mnuExplorer.Text = "Explorer To Here";
			this.mnuExplorer.Click += new System.EventHandler(this.mnuExplorer_Click);
			//
			// mnuDiff
			//
			this.mnuDiff.Index = 4;
			this.mnuDiff.Text = "Diff with Depot";
			this.mnuDiff.Click += new System.EventHandler(this.mnuDiff_Click);
			//
			// menuItem4
			//
			this.menuItem4.Index = 5;
			this.menuItem4.Text = "-";
			//
			// mnExcludeFile
			//
			this.mnExcludeFile.Index = 6;
			this.mnExcludeFile.Text = "Exclude File";
			this.mnExcludeFile.Click += new System.EventHandler(this.mnExcludeFile_Click);
			//
			// mnuExcludeDir
			//
			this.mnuExcludeDir.Index = 7;
			this.mnuExcludeDir.Text = "Exclude Directory";
			this.mnuExcludeDir.Click += new System.EventHandler(this.mnuExcludeDir_Click);
			//
			// menuItem6
			//
			this.menuItem6.Index = 8;
			this.menuItem6.Text = "-";
			//
			// menuItem3
			//
			this.menuItem3.Index = 9;
			this.menuItem3.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																					  this.mnuDelete,
																					  this.mnuReadOnly});
			this.menuItem3.Text = "Caution";
			//
			// mnuDelete
			//
			this.mnuDelete.Index = 0;
			this.mnuDelete.Text = "Delete from Depot";
			this.mnuDelete.Click += new System.EventHandler(this.mnuDelete_Click);
			//
			// mnuReadOnly
			//
			this.mnuReadOnly.Index = 1;
			this.mnuReadOnly.Text = "Set to ReadOnly";
			this.mnuReadOnly.Click += new System.EventHandler(this.mnuReadOnly_Click);
			//
			// imageList1
			//
			this.imageList1.ImageSize = new System.Drawing.Size(16, 16);
			this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
			this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
			//
			// mainMenu1
			//
			this.mainMenu1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																					  this.menuItem1,
																					  this.menuItem2});
			//
			// menuItem1
			//
			this.menuItem1.Index = 0;
			this.menuItem1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																					  this.mnuCheckOut,
																					  this.mnuSubmitDefault});
			this.menuItem1.Text = "&File";
			//
			// mnuCheckOut
			//
			this.mnuCheckOut.Index = 0;
			this.mnuCheckOut.Shortcut = System.Windows.Forms.Shortcut.F4;
			this.mnuCheckOut.Text = "Check Out...";
			this.mnuCheckOut.Click += new System.EventHandler(this.mnuCheckOut_Click);
			//
			// mnuSubmitDefault
			//
			this.mnuSubmitDefault.Index = 1;
			this.mnuSubmitDefault.Text = "Submit &Default...";
			this.mnuSubmitDefault.Click += new System.EventHandler(this.mnuSubmitDefault_Click);
			//
			// menuItem2
			//
			this.menuItem2.Index = 1;
			this.menuItem2.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																					  this.mnuRefresh,
																					  this.mnuRefreshP4});
			this.menuItem2.Text = "&View";
			//
			// mnuRefresh
			//
			this.mnuRefresh.Index = 0;
			this.mnuRefresh.Shortcut = System.Windows.Forms.Shortcut.F5;
			this.mnuRefresh.Text = "&Refresh";
			this.mnuRefresh.Click += new System.EventHandler(this.mnuRefresh_Click);
			//
			// mnuRefreshP4
			//
			this.mnuRefreshP4.Index = 1;
			this.mnuRefreshP4.Shortcut = System.Windows.Forms.Shortcut.F9;
			this.mnuRefreshP4.Text = "&Query P4 Status";
			this.mnuRefreshP4.Click += new System.EventHandler(this.mnuRefreshP4_Click);
			//
			// m_console
			//
			this.m_console.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.m_console.HideSelection = false;
			this.m_console.Location = new System.Drawing.Point(0, 433);
			this.m_console.Multiline = true;
			this.m_console.Name = "m_console";
			this.m_console.ReadOnly = true;
			this.m_console.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.m_console.Size = new System.Drawing.Size(760, 128);
			this.m_console.TabIndex = 1;
			this.m_console.Text = "";
			this.m_console.WordWrap = false;
			//
			// toolBar1
			//
			this.toolBar1.Buttons.AddRange(new System.Windows.Forms.ToolBarButton[] {
																						this.btnEdit,
																						this.btnAdd,
																						this.tbbSep1,
																						this.btnRefresh,
																						this.btnRefreshP4,
																						this.btnValidityCheck});
			this.toolBar1.DropDownArrows = true;
			this.toolBar1.ImageList = this.imageList1;
			this.toolBar1.Location = new System.Drawing.Point(0, 0);
			this.toolBar1.Name = "toolBar1";
			this.toolBar1.ShowToolTips = true;
			this.toolBar1.Size = new System.Drawing.Size(760, 28);
			this.toolBar1.TabIndex = 2;
			this.toolBar1.ButtonClick += new System.Windows.Forms.ToolBarButtonClickEventHandler(this.toolBar1_ButtonClick);
			//
			// btnEdit
			//
			this.btnEdit.ImageIndex = 2;
			this.btnEdit.ToolTipText = "Open the selected file(s) for editting";
			//
			// btnAdd
			//
			this.btnAdd.Enabled = false;
			this.btnAdd.ImageIndex = 3;
			this.btnAdd.ToolTipText = "Add the selected file(s) to Perforce";
			//
			// tbbSep1
			//
			this.tbbSep1.Style = System.Windows.Forms.ToolBarButtonStyle.Separator;
			//
			// btnRefresh
			//
			this.btnRefresh.ImageIndex = 4;
			this.btnRefresh.ToolTipText = "Reload the list of files from your hard drive";
			//
			// btnRefreshP4
			//
			this.btnRefreshP4.ImageIndex = 5;
			this.btnRefreshP4.ToolTipText = "Reload file from your hard drive, then figure out which ones are open in P4";
			//
			// splitter1
			//
			this.splitter1.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.splitter1.Location = new System.Drawing.Point(0, 430);
			this.splitter1.Name = "splitter1";
			this.splitter1.Size = new System.Drawing.Size(760, 3);
			this.splitter1.TabIndex = 3;
			this.splitter1.TabStop = false;
			//
			// m_timerStartup
			//
			this.m_timerStartup.Tick += new System.EventHandler(this.m_timerStartup_Tick);
			//
			// btnValidityCheck
			//
			this.btnValidityCheck.ImageIndex = 6;
			//
			// P4HelperForm
			//
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(760, 561);
			this.Controls.Add(this.m_list);
			this.Controls.Add(this.splitter1);
			this.Controls.Add(this.m_console);
			this.Controls.Add(this.toolBar1);
			this.Menu = this.mainMenu1;
			this.Name = "P4HelperForm";
			this.Text = "P4Helper";
			this.Closing += new System.ComponentModel.CancelEventHandler(this.P4HelperForm_Closing);
			this.ResumeLayout(false);

		}
		#endregion

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[STAThread]
		static void Main()
		{
			Application.Run(new P4HelperForm());
		}

		private void mnuRefresh_Click(object sender, System.EventArgs e)
		{
			LoadFileList();
		}

		protected void LoadFileList ()
		{
			System.Windows.Forms.Cursor oldCursor = System.Windows.Forms.Cursor.Current;
			System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;

			m_list.Items.Clear();
			ProcessDirectory(RootDirectory);

			System.Windows.Forms.Cursor.Current =  oldCursor;
		}


		// Process all files in the directory passed in, and recurse on any directories
		// that are found to process the files they contain
		protected void ProcessDirectory(string targetDirectory)
		{
			if(!IncludeDirectory(targetDirectory))
				return;

			// Process the list of files found in the directory
			string [] fileEntries = Directory.GetFiles(targetDirectory);
			foreach(string fileName in fileEntries)
				ProcessFile(fileName);

			// Recurse into subdirectories of this directory
			string [] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
			foreach(string subdirectory in subdirectoryEntries)
				ProcessDirectory(subdirectory);
		}

		protected void ProcessFile(string path)
		{
			if(!(IncludeAttributes(path) && IncludeName(System.IO.Path.GetFileName(path))))
				return;

			ListViewItem item = new ListViewItem(GetColumns(path),0);
			m_list.Items.Add(item);
			item.Tag = path;
		}

		protected string[] GetColumns (String path)
		{
			return new string[] {	Path.GetDirectoryName(path),
									Path.GetFileName(path),
									GetModifiedString(path),
									GetAttrString(path)		};
		}

		protected string GetAttrString(string path)
		{
			System.IO.FileAttributes attributes = System.IO.File.GetAttributes(path);
			string result= "";
			if(0 !=(attributes & System.IO.FileAttributes.ReadOnly))
				result+="R";
			if(0 !=(attributes & System.IO.FileAttributes.Archive))
				result+="A";

			return result;
		}

		protected string GetModifiedString(string path)
		{
			DateTime when =System.IO.File.GetLastWriteTime(path);

			if(when.Date == DateTime.Today)
				return System.IO.File.GetLastWriteTime(path).ToShortTimeString();

			return System.IO.File.GetLastWriteTime(path).ToShortDateString();
		}
		protected string RootDirectory
		{
			get
			{
				return System.Environment.ExpandEnvironmentVariables("%fwroot%");
			}
		}

		protected Boolean IncludedType(string extension)
		{
			return true;
		}

		protected Boolean IncludeName(string name)
		{
			foreach(string s in m_skipNameParts)
			{
				if(name.ToLower().IndexOf(s.ToLower())>=0)
					return false;
			}
			return true;
		}

		protected Boolean IncludeDirectory(string directory)
		{
			string stem = System.IO.Path.GetFileName(directory);
			return !(m_skipDirs.Contains(stem.ToLower()) ||	//leaf directory
				m_skipDirs.Contains(directory.ToLower()));	//whole thing
		}

		protected Boolean IncludeAttributes(string path)
		{
			System.IO.FileAttributes attributes = System.IO.File.GetAttributes(path);
			return 0 ==(attributes & System.IO.FileAttributes.ReadOnly);
		}



		private void mnuRefreshP4_Click(object sender, System.EventArgs e)
		{
			LoadFileList();

			System.Collections.Specialized.StringCollection opened = new System.Collections.Specialized.StringCollection();
			opened.AddRange(m_p4.GetP4OpenFiles ());

			foreach(ListViewItem item in m_list.Items)
			{
				if(opened.Contains( (string)item.Tag))
				{
					item.ImageIndex = 1;
					item.ForeColor = System.Drawing.Color.Blue;
				}
			}
		}

		private void mnuCheckOut_Click(object sender, System.EventArgs e)
		{
			if(m_list.SelectedItems.Count == 0)
				return;



			m_p4.CheckOut(GetSelectedFilesString());
		}

		private void toolBar1_ButtonClick(object sender, System.Windows.Forms.ToolBarButtonClickEventArgs e)
		{
			if(e.Button ==btnRefresh  )
			{
				mnuRefresh_Click(sender, e);
			}
			else if(e.Button ==btnRefreshP4 )
			{
				mnuRefreshP4_Click(sender, e);
			}
			else if(e.Button ==btnEdit )
			{
				mnuCheckOut_Click(sender, e);
			}
			else if(e.Button ==btnAdd)
			{
				mnuRefreshP4_Click(sender, e);
			}
			else if(e.Button ==btnValidityCheck)
			{
				ValidityCheck();
			}
		}

		//do a grep of various files for common John Hatton mistakes
		private void ValidityCheck ()
		{
			Grep.Grepper searcher = new Grep.Grepper();
			searcher.IgnoreCase= true;
			searcher.LineNumbers=false;
			searcher.Recursive = true;
			searcher.RootDirectory=System.Environment.ExpandEnvironmentVariables(@"%fwroot%\src");

			XmlNodeList list =m_configDocument.SelectNodes("//warningPatterns/pattern");

			foreach(XmlNode node in list)
			{
				m_p4.WriteToConsole(node.Attributes["msg"].Value);
				searcher.Files = node.Attributes["filePattern"].Value;
				searcher.RegEx=node.Attributes["expression"].Value;
				m_p4.WriteToConsole( searcher.Search());
			}

		}

		private void mnuReadOnly_Click(object sender, System.EventArgs e)
		{
			foreach(ListViewItem item in m_list.SelectedItems)
			{
				SetReadOnly((string)item.Tag);
			}
			LoadFileList();
		}

		protected void SetReadOnly(string path)
		{
			System.IO.FileInfo fi = new FileInfo(path);
			//fi.Attributes = 0;
			fi.Attributes |=  System.IO.FileAttributes.ReadOnly;
		}

		private void m_contextMenu_Popup(object sender, System.EventArgs e)
		{

		}

		private void mnuExplorer_Click(object sender, System.EventArgs e)
		{
			if(m_list.SelectedItems.Count == 0)
				return;

			string path = (string)m_list.SelectedItems[0].Tag;
			path = Path.GetDirectoryName(path);

			Process p = new Process();
			p.StartInfo.CreateNoWindow  = true;
			p.StartInfo.UseShellExecute = false;
			p.StartInfo.Arguments = path;
			p.StartInfo.RedirectStandardOutput = true;
			p.StartInfo.RedirectStandardError = true;
			p.StartInfo.FileName = "explorer ";
			p.Start();

		}

		private void m_timerStartup_Tick(object sender, System.EventArgs e)
		{
			m_timerStartup.Enabled= false;
//			if(!m_p4.IsP4Connected())
//				m_p4.ConnectSecurePort();
		}

		private void mnuDiff_Click(object sender, System.EventArgs e)
		{
			m_p4.Diff(GetSelectedFilesString());
		}

		private void mnuAddToP4_Click(object sender, System.EventArgs e)
		{
			m_p4.Add(GetSelectedFilesString());

		}

		protected string GetSelectedFilesString()
		{
			string files = "";
			foreach(ListViewItem item in m_list.SelectedItems)
			{
				files += "\"" +(string)item.Tag + "\" ";
			}
			return files;
		}

		private void mnuDelete_Click(object sender, System.EventArgs e)
		{
			m_p4.Delete(GetSelectedFilesString());

		}

		private void P4HelperForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			SaveConfiguration ();
		}

		private void mnExcludeFile_Click(object sender, System.EventArgs e)
		{
			foreach(ListViewItem item in m_list.SelectedItems)
			{
				AddNamePart(Path.GetFileName((string)item.Tag));
			}
			UseConfiguration ();
			LoadFileList();
		}

		private void mnuExcludeDir_Click(object sender, System.EventArgs e)
		{
			foreach(ListViewItem item in m_list.SelectedItems)
			{
				AddDirectoryPath(Path.GetDirectoryName((string)item.Tag));
			}
			UseConfiguration ();
			LoadFileList();

		}

		private void mnuSubmitDefault_Click(object sender, System.EventArgs e)
		{
			m_p4.SubmitDefault();
		}
	}
}

using System;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Drawing.Text;
using System.Drawing;                   // for Font
using Microsoft.Win32;                  // for RegistryKey
using System.IO;                        // for File
using System.Diagnostics;               // for Debug
using System.Runtime.InteropServices;   // for ComVisible
using System.Reflection;                // for Assembly
using System.Text;                      // for Encoding
using System.Data;                      // for DataTable
using ECInterfaces;
using SilEncConverters40;

namespace SpellingFixerEC
{
	/// <summary>
	/// Summary description for LoginSF.
	/// </summary>
	[ComVisible(false)]
	internal class LoginSF : System.Windows.Forms.Form
	{
		private System.Windows.Forms.CheckedListBox checkedListBoxProjects;
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.TextBox textBoxNewProjectName;
		private System.Windows.Forms.Button buttonAddNewProject;
		private System.Windows.Forms.CheckBox checkBoxUnicode;
		private System.Windows.Forms.Label labelFont;
		private System.Windows.Forms.ListBox listBoxFontSize;
		private System.Windows.Forms.GroupBox groupBoxNewProject;
		private System.Windows.Forms.Label labelName;
		private System.Windows.Forms.ComboBox comboBoxFont;
		private System.Windows.Forms.Label labelFontSize;
		private System.Windows.Forms.ToolTip toolTips;
		private System.Windows.Forms.MenuItem menuItemDelete;
		private System.Windows.Forms.MenuItem menuItemClick;
		private System.Windows.Forms.ContextMenu contextMenu;
		private System.Windows.Forms.Label labelCP;
		private System.Windows.Forms.TextBox textBoxCP;
		private System.ComponentModel.IContainer components;

		private bool    m_bLegacy;
		private int     m_cp = 1252;
		private Font    m_font;
		private string  m_strConverterSpec;
		private string  m_strEncConverterName;
		private string  m_strWordBoundaryDelimiter;
		public  const string    cstrAddNewProjectButtonText = "&Add New Project";
		public  const string    cstrAddNewProjectButtonToolTipText = "Click to add new project";
		public  const string    cstrNewProjectGroupText = "New Project";
		private const string    cstrProjectMemoryKey = @"SOFTWARE\SIL\SilEncConverters40\SpellingFixerEC";
		private System.Windows.Forms.Label labelInstructions;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox textBoxWordBoundaryDelimiter;
		private System.Windows.Forms.MenuItem menuItemEdit;
		private System.Windows.Forms.MenuItem menuItemDeleteAll;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox textBoxAddlPunctuation;
		private string  m_strNonWordCharacters;
		private string  cstrProjectMostRecentProject = "MostRecentProject";

		public LoginSF()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			this.checkedListBoxProjects.ContextMenu = this.contextMenu;

			EncConverters aECs = new EncConverters();
			EncConverters myECs = aECs.FilterByProcessType(SpellingFixerEC.SFProcessType);

			// disable the list box (and the OK button) if there's nothing in it
			this.buttonOK.Enabled = this.checkedListBoxProjects.Visible = (myECs.Count > 0);

			string strPartialName;
			foreach(IEncConverter aEC in myECs.Values)
				if( (strPartialName = PartialName(aEC.Name)) != null )
					checkedListBoxProjects.Items.Add(strPartialName);

			// decide which one to select
			int nIndex = checkedListBoxProjects.Items.Count - 1;
			try
			{
				RegistryKey keyLastSFProject = Registry.CurrentUser.OpenSubKey(cstrProjectMemoryKey);
				nIndex = checkedListBoxProjects.FindString((string)keyLastSFProject.GetValue(cstrProjectMostRecentProject));
			}
			catch {}

			// TODO: also, add a new ctor to allow choosing the project programmatically.
			if( nIndex != -1 )
				checkedListBoxProjects.SetItemChecked(nIndex,true);
			else
				// if there are no items in the list, then make the "Add New Project" the default button
				this.AcceptButton = this.buttonAddNewProject;

			// populate the font name combo box with all the fonts installed (but only if they
			//  do Regular style). That is, on my system, I choose the "Aharoni" (some sort of
			//  hebrew font) as a test and the creation of the "Font" object below failed,
			//  because I was using the ctor that takes only the name and size, but that font
			//  doesn't have a 'Regular' style (which is the default for that ctor). If I
			//  wanted to add a query for the font 'style' to this dialog box as well (c.f.
			//  Word's font dialog box), then this restriction could be removed, but... I'll
			//  wait until someone complains.
			InstalledFontCollection installedFontCollection = new InstalledFontCollection();
			FontFamily[] fontFamilies = installedFontCollection.Families;
			foreach(FontFamily fontFamily in fontFamilies)
				if( fontFamily.IsStyleAvailable(FontStyle.Regular) )
					comboBoxFont.Items.Add(fontFamily.Name);

			// start with some defaults.
			this.listBoxFontSize.SelectedItem = "14";
			this.textBoxCP.Text = "1252";
			this.textBoxWordBoundaryDelimiter.Text = SpellingFixerEC.cstrDefaultWordBoundaryDelimiter;

			// start out pessimmistic
			DialogResult = DialogResult.Cancel;
		}

		public Font FontToUse
		{
			get { return m_font; }
		}

		public int CpToUse
		{
			get { return m_cp; }
		}

		public bool IsLegacy
		{
			get { return m_bLegacy; }
		}

		public string ConverterSpec
		{
			get { return m_strConverterSpec; }
		}

		public string EncConverterName
		{
			get { return m_strEncConverterName; }
		}

		public string WordBoundaryDelimiter
		{
			get { return m_strWordBoundaryDelimiter; }
		}

		public string Punctuation
		{
			get { return m_strNonWordCharacters; }
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
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

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(LoginSF));
			this.checkedListBoxProjects = new System.Windows.Forms.CheckedListBox();
			this.buttonOK = new System.Windows.Forms.Button();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.labelName = new System.Windows.Forms.Label();
			this.textBoxNewProjectName = new System.Windows.Forms.TextBox();
			this.checkBoxUnicode = new System.Windows.Forms.CheckBox();
			this.buttonAddNewProject = new System.Windows.Forms.Button();
			this.groupBoxNewProject = new System.Windows.Forms.GroupBox();
			this.textBoxAddlPunctuation = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.textBoxWordBoundaryDelimiter = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.labelFont = new System.Windows.Forms.Label();
			this.comboBoxFont = new System.Windows.Forms.ComboBox();
			this.labelFontSize = new System.Windows.Forms.Label();
			this.labelCP = new System.Windows.Forms.Label();
			this.textBoxCP = new System.Windows.Forms.TextBox();
			this.listBoxFontSize = new System.Windows.Forms.ListBox();
			this.toolTips = new System.Windows.Forms.ToolTip(this.components);
			this.contextMenu = new System.Windows.Forms.ContextMenu();
			this.menuItemClick = new System.Windows.Forms.MenuItem();
			this.menuItemDelete = new System.Windows.Forms.MenuItem();
			this.menuItemDeleteAll = new System.Windows.Forms.MenuItem();
			this.menuItemEdit = new System.Windows.Forms.MenuItem();
			this.labelInstructions = new System.Windows.Forms.Label();
			this.groupBoxNewProject.SuspendLayout();
			this.SuspendLayout();
			//
			// checkedListBoxProjects
			//
			this.checkedListBoxProjects.Cursor = System.Windows.Forms.Cursors.Hand;
			this.checkedListBoxProjects.Location = new System.Drawing.Point(16, 16);
			this.checkedListBoxProjects.Name = "checkedListBoxProjects";
			this.checkedListBoxProjects.Size = new System.Drawing.Size(360, 154);
			this.checkedListBoxProjects.TabIndex = 0;
			this.checkedListBoxProjects.ThreeDCheckBoxes = true;
			this.toolTips.SetToolTip(this.checkedListBoxProjects, "List of existing projects");
			this.checkedListBoxProjects.DoubleClick += new System.EventHandler(this.checkedListBoxProjects_DoubleClick);
			this.checkedListBoxProjects.SelectedIndexChanged += new System.EventHandler(this.checkedListBoxProjects_SelectedIndexChanged);
			this.checkedListBoxProjects.MouseUp += new System.Windows.Forms.MouseEventHandler(this.checkedListBoxProjects_MouseUp);
			//
			// buttonOK
			//
			this.buttonOK.Location = new System.Drawing.Point(115, 464);
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.Size = new System.Drawing.Size(75, 24);
			this.buttonOK.TabIndex = 11;
			this.buttonOK.Text = "OK";
			this.toolTips.SetToolTip(this.buttonOK, "Click to use the checked project");
			this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
			//
			// buttonCancel
			//
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.Location = new System.Drawing.Point(203, 464);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(75, 24);
			this.buttonCancel.TabIndex = 12;
			this.buttonCancel.Text = "Cancel";
			this.toolTips.SetToolTip(this.buttonCancel, "Click to cancel this operation");
			this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
			//
			// labelName
			//
			this.labelName.Location = new System.Drawing.Point(32, 216);
			this.labelName.Name = "labelName";
			this.labelName.Size = new System.Drawing.Size(56, 23);
			this.labelName.TabIndex = 1;
			this.labelName.Text = "&Name:";
			//
			// textBoxNewProjectName
			//
			this.textBoxNewProjectName.Location = new System.Drawing.Point(88, 216);
			this.textBoxNewProjectName.Name = "textBoxNewProjectName";
			this.textBoxNewProjectName.Size = new System.Drawing.Size(272, 20);
			this.textBoxNewProjectName.TabIndex = 2;
			this.textBoxNewProjectName.Text = "";
			this.toolTips.SetToolTip(this.textBoxNewProjectName, "Enter the name of a new projects (e.g. \'Hindi\')");
			this.textBoxNewProjectName.TextChanged += new System.EventHandler(this.textBoxNewProjectName_TextChanged);
			//
			// checkBoxUnicode
			//
			this.checkBoxUnicode.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.checkBoxUnicode.Checked = true;
			this.checkBoxUnicode.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBoxUnicode.Cursor = System.Windows.Forms.Cursors.Hand;
			this.checkBoxUnicode.Location = new System.Drawing.Point(16, 120);
			this.checkBoxUnicode.Name = "checkBoxUnicode";
			this.checkBoxUnicode.Size = new System.Drawing.Size(72, 24);
			this.checkBoxUnicode.TabIndex = 7;
			this.checkBoxUnicode.Text = "&Unicode:";
			this.toolTips.SetToolTip(this.checkBoxUnicode, "Check this if the data is Unicode-encoded (otherwise, it is Legacy-encoded)");
			this.checkBoxUnicode.CheckedChanged += new System.EventHandler(this.checkBoxUnicode_CheckedChanged);
			//
			// buttonAddNewProject
			//
			this.buttonAddNewProject.Location = new System.Drawing.Point(116, 224);
			this.buttonAddNewProject.Name = "buttonAddNewProject";
			this.buttonAddNewProject.Size = new System.Drawing.Size(128, 23);
			this.buttonAddNewProject.TabIndex = 10;
			this.buttonAddNewProject.Text = "&Add New Project";
			this.toolTips.SetToolTip(this.buttonAddNewProject, "Click to add new project");
			this.buttonAddNewProject.Click += new System.EventHandler(this.buttonAddNewProject_Click);
			//
			// groupBoxNewProject
			//
			this.groupBoxNewProject.Controls.Add(this.textBoxAddlPunctuation);
			this.groupBoxNewProject.Controls.Add(this.label2);
			this.groupBoxNewProject.Controls.Add(this.textBoxWordBoundaryDelimiter);
			this.groupBoxNewProject.Controls.Add(this.label1);
			this.groupBoxNewProject.Controls.Add(this.labelFont);
			this.groupBoxNewProject.Controls.Add(this.comboBoxFont);
			this.groupBoxNewProject.Controls.Add(this.labelFontSize);
			this.groupBoxNewProject.Controls.Add(this.checkBoxUnicode);
			this.groupBoxNewProject.Controls.Add(this.labelCP);
			this.groupBoxNewProject.Controls.Add(this.textBoxCP);
			this.groupBoxNewProject.Controls.Add(this.listBoxFontSize);
			this.groupBoxNewProject.Controls.Add(this.buttonAddNewProject);
			this.groupBoxNewProject.Location = new System.Drawing.Point(16, 184);
			this.groupBoxNewProject.Name = "groupBoxNewProject";
			this.groupBoxNewProject.Size = new System.Drawing.Size(360, 264);
			this.groupBoxNewProject.TabIndex = 9;
			this.groupBoxNewProject.TabStop = false;
			this.groupBoxNewProject.Text = "New Project";
			//
			// textBoxAddlPunctuation
			//
			this.textBoxAddlPunctuation.Location = new System.Drawing.Point(232, 184);
			this.textBoxAddlPunctuation.Name = "textBoxAddlPunctuation";
			this.textBoxAddlPunctuation.Size = new System.Drawing.Size(112, 20);
			this.textBoxAddlPunctuation.TabIndex = 14;
			this.textBoxAddlPunctuation.Text = "";
			this.toolTips.SetToolTip(this.textBoxAddlPunctuation, "Enter any additional punctuation or whitespace characters needed for this languag" +
				"e, separated by spaces");
			//
			// label2
			//
			this.label2.Location = new System.Drawing.Point(16, 184);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(208, 24);
			this.label2.TabIndex = 13;
			this.label2.Text = "Additional &punctuation and whitespace:";
			//
			// textBoxWordBoundaryDelimiter
			//
			this.textBoxWordBoundaryDelimiter.Location = new System.Drawing.Point(232, 152);
			this.textBoxWordBoundaryDelimiter.Name = "textBoxWordBoundaryDelimiter";
			this.textBoxWordBoundaryDelimiter.Size = new System.Drawing.Size(24, 20);
			this.textBoxWordBoundaryDelimiter.TabIndex = 12;
			this.textBoxWordBoundaryDelimiter.Text = SpellingFixerEC.cstrDefaultWordBoundaryDelimiter;
			this.toolTips.SetToolTip(this.textBoxWordBoundaryDelimiter, "Enter the character(s) to use as a word boundary delimiter (e.g. with a delimiter" +
				" of \"#\", you can enter the \"bad spelling\" words like: #car#, which will only mat" +
				"ch if the search string is \"car\", but not \"cars\" or \"sportscar\")");
			this.textBoxWordBoundaryDelimiter.TextChanged += new System.EventHandler(this.textBoxWordBoundaryDelimiter_TextChanged);
			//
			// label1
			//
			this.label1.Location = new System.Drawing.Point(16, 154);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(136, 16);
			this.label1.TabIndex = 11;
			this.label1.Text = "&Word boundary delimiter:";
			//
			// labelFont
			//
			this.labelFont.Location = new System.Drawing.Point(16, 64);
			this.labelFont.Name = "labelFont";
			this.labelFont.TabIndex = 3;
			this.labelFont.Text = "&Font:";
			//
			// comboBoxFont
			//
			this.comboBoxFont.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxFont.Location = new System.Drawing.Point(24, 88);
			this.comboBoxFont.Name = "comboBoxFont";
			this.comboBoxFont.Size = new System.Drawing.Size(232, 21);
			this.comboBoxFont.TabIndex = 4;
			this.toolTips.SetToolTip(this.comboBoxFont, "Choose the font to be used for displaying the words whose spelling is to be corre" +
				"cted (e.g. \'Arial Unicode MS\')");
			this.comboBoxFont.SelectedIndexChanged += new System.EventHandler(this.comboBoxFont_SelectedIndexChanged);
			//
			// labelFontSize
			//
			this.labelFontSize.Location = new System.Drawing.Point(264, 64);
			this.labelFontSize.Name = "labelFontSize";
			this.labelFontSize.Size = new System.Drawing.Size(64, 23);
			this.labelFontSize.TabIndex = 5;
			this.labelFontSize.Text = "Font &Size:";
			//
			// labelCP
			//
			this.labelCP.Location = new System.Drawing.Point(120, 120);
			this.labelCP.Name = "labelCP";
			this.labelCP.Size = new System.Drawing.Size(80, 23);
			this.labelCP.TabIndex = 8;
			this.labelCP.Text = "&Code page:";
			this.labelCP.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.labelCP.Visible = false;
			//
			// textBoxCP
			//
			this.textBoxCP.Location = new System.Drawing.Point(200, 120);
			this.textBoxCP.Name = "textBoxCP";
			this.textBoxCP.Size = new System.Drawing.Size(56, 20);
			this.textBoxCP.TabIndex = 9;
			this.textBoxCP.Text = "";
			this.toolTips.SetToolTip(this.textBoxCP, "Enter the code page used by this legacy font");
			this.textBoxCP.Visible = false;
			this.textBoxCP.TextChanged += new System.EventHandler(this.textBoxCP_TextChanged);
			//
			// listBoxFontSize
			//
			this.listBoxFontSize.Items.AddRange(new object[] {
																 "8",
																 "9",
																 "10",
																 "10.5",
																 "11",
																 "12",
																 "14",
																 "16",
																 "18",
																 "20",
																 "22",
																 "24",
																 "26",
																 "28",
																 "36",
																 "48",
																 "72"});
			this.listBoxFontSize.Location = new System.Drawing.Point(272, 88);
			this.listBoxFontSize.Name = "listBoxFontSize";
			this.listBoxFontSize.ScrollAlwaysVisible = true;
			this.listBoxFontSize.Size = new System.Drawing.Size(72, 82);
			this.listBoxFontSize.TabIndex = 6;
			this.toolTips.SetToolTip(this.listBoxFontSize, "Choose the font size");
			this.listBoxFontSize.SelectedIndexChanged += new System.EventHandler(this.listBoxFontSize_SelectedIndexChanged);
			//
			// toolTips
			//
			this.toolTips.AutoPopDelay = 30000;
			this.toolTips.InitialDelay = 500;
			this.toolTips.ReshowDelay = 100;
			//
			// contextMenu
			//
			this.contextMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																						this.menuItemClick,
																						this.menuItemDelete,
																						this.menuItemDeleteAll,
																						this.menuItemEdit});
			//
			// menuItemClick
			//
			this.menuItemClick.DefaultItem = true;
			this.menuItemClick.Index = 0;
			this.menuItemClick.Text = "&Click";
			this.menuItemClick.Click += new System.EventHandler(this.menuItemClick_Click);
			//
			// menuItemDelete
			//
			this.menuItemDelete.Index = 1;
			this.menuItemDelete.Text = "&Delete";
			this.menuItemDelete.Click += new System.EventHandler(this.menuItemDelete_Click);
			//
			// menuItemDeleteAll
			//
			this.menuItemDeleteAll.Index = 2;
			this.menuItemDeleteAll.Text = "Delete &All";
			this.menuItemDeleteAll.Click += new System.EventHandler(this.menuItemDeleteAll_Click);
			//
			// menuItemEdit
			//
			this.menuItemEdit.Index = 3;
			this.menuItemEdit.Text = "&Edit";
			this.menuItemEdit.Click += new System.EventHandler(this.menuItemEdit_Click);
			//
			// labelInstructions
			//
			this.labelInstructions.Location = new System.Drawing.Point(32, 64);
			this.labelInstructions.Name = "labelInstructions";
			this.labelInstructions.Size = new System.Drawing.Size(336, 64);
			this.labelInstructions.TabIndex = 13;
			this.labelInstructions.Text = "Fill in the details below and click the Add New Project button.";
			//
			// LoginSF
			//
			this.AcceptButton = this.buttonOK;
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.CancelButton = this.buttonCancel;
			this.ClientSize = new System.Drawing.Size(392, 502);
			this.Controls.Add(this.checkedListBoxProjects);
			this.Controls.Add(this.labelName);
			this.Controls.Add(this.textBoxNewProjectName);
			this.Controls.Add(this.groupBoxNewProject);
			this.Controls.Add(this.buttonOK);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.labelInstructions);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "LoginSF";
			this.Text = "Choose or Add New Fix Spelling Project";
			this.groupBoxNewProject.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		internal string FullName(string strProjName)
		{
			return SpellingFixerEC.cstrSFConverterPrefix + strProjName;
		}

		internal string PartialName(string strFullName)
		{
			int nPrefixLen = SpellingFixerEC.cstrSFConverterPrefix.Length;
			if(     (strFullName.Length > nPrefixLen)
				&&  (strFullName.Substring(0, nPrefixLen) == SpellingFixerEC.cstrSFConverterPrefix) )
				return strFullName.Substring(nPrefixLen,strFullName.Length - nPrefixLen);
			return null;
		}

		internal static void CreateCCTable(string strCCTableSpec, string strEncConverterName, string strPunctuation, string strCustomCode, bool bUnicode)
		{
			CreateCCTable(new FileStream(strCCTableSpec, FileMode.Create), strEncConverterName, strPunctuation, strCustomCode, bUnicode);
		}

		internal static void CreateCCTable(FileStream fs, string strEncConverterName, string strPunctuation, string strCustomCode, bool bUnicode)
		{
			// write out the header lines.
			StreamWriter sw = new StreamWriter(fs);
			CreateCCTable(sw, strEncConverterName, strPunctuation, strCustomCode, bUnicode);
			sw.Flush();
			sw.Close();
		}

		internal static string cstrLastHeaderLine
		{
			get { return "c Last Header Line: DON'T modify the table beyond this point (or your changes may be overwritten)"; }
		}

		internal static void CreateCCTable(StreamWriter sw, string strEncConverterName, string strPunctuation, string strCustomCode, bool bUnicode)
		{
			// write out the header lines.
			string strVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
			sw.WriteLine(String.Format("c This cc table was created by SpellingFixerEC.dll v{0} on {1}.",strVersion, DateTime.Now.ToShortDateString()));
			sw.WriteLine(String.Format("c It can be accessed as the '{0}' EncConverter", strEncConverterName));
			sw.WriteLine("c If you know how to program CC, you can add special processing between the");
			sw.WriteLine("c 'start custom changes' and 'end custom changes' comments below.");
			string strBeginStmt = "begin >";
			if( bUnicode )
				strBeginStmt += " utf8";    // this is needed to interpret multi-byte UTF8 strings as a single character (e.g. in the 'ws' store)
			sw.WriteLine(strBeginStmt);
			sw.WriteLine(String.Format("    store(ws) {0} endstore", strPunctuation));
			sw.WriteLine("");
			sw.WriteLine("c +----------start custom changes----------+");
			if (!String.IsNullOrEmpty(strCustomCode))
			{
				sw.Write(strCustomCode);
			}
			sw.WriteLine("c +----------end custom changes----------+");
			sw.WriteLine("");
			sw.WriteLine(cstrLastHeaderLine);
		}

		internal static void ReWriteCCTableHeader(string strCCTableSpec, string strPunctuation, Encoding enc)
		{
			// Open the CC table that has the mappings put them in a new file
			//  while re-writing the first part of the header
			if( (strCCTableSpec != null) && File.Exists(strCCTableSpec) )
			{
				const string strTempExt = ".new";
				// get a stream writer for these encoding and append
				StreamReader sr = new StreamReader(strCCTableSpec,enc);
				StreamWriter sw = new StreamWriter(strCCTableSpec + strTempExt, false, enc);

				// this is for version 1.2.0.0
				string strVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

				// copy the read stuff to the output and update the 'ws' store line
				for(string line = sr.ReadLine(); line != null; line = sr.ReadLine())
				{
					if( line.IndexOf("c This cc table was created by SpellingFixerEC.dll v") != -1 )
						line = String.Format("c This cc table was created by SpellingFixerEC.dll v{0} on {1}.",strVersion, DateTime.Now.ToShortDateString());
					else if( line.IndexOf("store(ws)") != -1 )
						line = String.Format("    store(ws) {0} endstore", strPunctuation);

					sw.WriteLine(line);

					// stop when we get past the end of the header
					if( line == cstrLastHeaderLine )
						break;
				}

				sw.Flush();
				sw.Close();
				sr.Close();

				string strBackupFilename = strCCTableSpec + ".bak";
				if( File.Exists(strBackupFilename) )
					File.Delete(strBackupFilename);
				File.Move(strCCTableSpec,strBackupFilename);
				File.Move(strCCTableSpec + strTempExt, strCCTableSpec);
			}
		}

		private void EnableAddNewProjectButton()
		{
			this.buttonAddNewProject.Enabled = (
				(this.textBoxNewProjectName.Text != "")
				&&  ((this.comboBoxFont.SelectedItem != null) && (this.comboBoxFont.SelectedItem.ToString() != ""))
				&&  ((this.listBoxFontSize.SelectedItem != null) && (this.listBoxFontSize.SelectedItem.ToString() != ""))
				&&  ((this.checkBoxUnicode.Checked) || (this.textBoxCP.Text != ""))
				&&  (!String.IsNullOrEmpty(this.textBoxWordBoundaryDelimiter.Text)) );
		}

		private bool    m_bUserDefinePunctuation = false;

		private string DecodePunctuationForCC(string strPunctuation)
		{
			if( String.IsNullOrEmpty(strPunctuation) )
				return null;

			else
			{
				// initialize it so that *we* take care of delimiting the punctuation
				m_bUserDefinePunctuation = false;

				// the first chunk of this should be the fixed punctuation
				int nIndex = strPunctuation.IndexOf(SpellingFixerEC.GetDefaultPunctuation);
				if ((nIndex == 0) && (strPunctuation.Length <= SpellingFixerEC.GetDefaultPunctuation.Length))
				{
					// if this is all there is, then the 'decoded' string is nothing.
					return null;
				}
				else
				{
					// pre-v3
					nIndex = strPunctuation.IndexOf(SpellingFixerEC.cstrDefaultPunctuationAndWhitespace);
				if( nIndex == 0 )
				{
					// if this is all there is, then the 'decoded' string is nothing.
						int nLength = SpellingFixerEC.cstrDefaultPunctuationAndWhitespace.Length;
						if (strPunctuation.Length <= nLength)
						return null;

					// otherwise, process only the extra
						strPunctuation = strPunctuation.Substring(nLength);
						if (strPunctuation.IndexOf(SpellingFixerEC.cstrV3DefaultPunctuationAndWhitespaceAdds) == 0)
						{
							nLength = SpellingFixerEC.cstrV3DefaultPunctuationAndWhitespaceAdds.Length;
							if (strPunctuation.Length <= nLength)
								return null;
							strPunctuation = strPunctuation.Substring(nLength + 1);
						}
						else
							strPunctuation = strPunctuation.Substring(1);
				}
				else
				{
					m_bUserDefinePunctuation = true;
					return strPunctuation;  // in this case, the user is responsible for delimiting the string him/herself
				}
			}
			}

			return DecodePunctuationForCCEx(strPunctuation);
		}

		private string DecodePunctuationForCCEx(string strPunctuation)
		{
			string strRet = null;
			string [] astrDelimitedChars = strPunctuation.Split(new char [] {' '});

			// each string should be in the form 'X', where X is the punctuation
			foreach(string strDelimitedChar in astrDelimitedChars)
			{
				if(     (strDelimitedChar.IndexOfAny(new char [] {'\'', '\"' }) != -1)
					&&  (strDelimitedChar.Length > 2) )
					strRet += strDelimitedChar.Substring(1,strDelimitedChar.Length - 2);
				else
					strRet += strDelimitedChar;

				strRet += ' ';
			}

			if( !String.IsNullOrEmpty(strRet) )
				strRet = strRet.Substring(0,strRet.Length - 1);

			return strRet;
		}

		private string EncodePunctuationForCC(string strPunctuation)
		{
			string strRet = null;

			if( m_bUserDefinePunctuation )
				return strPunctuation;

			else if( !String.IsNullOrEmpty(strPunctuation) )
			{
				string [] astrChars = strPunctuation.Split(new char [] {' '});
				foreach(string strChar in astrChars)
				{
					if (SpellingFixerEC.GetDefaultPunctuation.IndexOf(strChar) != -1)
					{
						MessageBox.Show(String.Format("There's no need to add the {0} character as Additional Punctuation because it's there by default,\r\nas are these: {1}",
							strChar, DecodePunctuationForCCEx(SpellingFixerEC.GetDefaultPunctuation)), SpellingFixerEC.cstrCaption);
						return null;
					}
					strRet += '\'' + strChar + "\' ";
				}
				if( !String.IsNullOrEmpty(strRet) )
					strRet = strRet.Substring(0,strRet.Length - 1);

				return SpellingFixerEC.GetDefaultPunctuation + ' ' + strRet;
			}

			return SpellingFixerEC.GetDefaultPunctuation;
		}

		internal static string GetMapTableFolderPath
		{
			get
			{
				string strMapsTableDir = Util.GetSpecialFolderPath(Environment.SpecialFolder.CommonApplicationData);
				strMapsTableDir += EncConverters.strDefMapsTablesPath;

				if (!Directory.Exists(strMapsTableDir))
					Directory.CreateDirectory(strMapsTableDir);

				return strMapsTableDir;
			}
		}

		private void buttonAddNewProject_Click(object sender, System.EventArgs e)
		{
			// in case the Add New project button was default, change it to the OK button.
			this.AcceptButton = this.buttonOK;

			// get the delimiter for word boundaries and disallow the /"/ character
			m_strWordBoundaryDelimiter = this.textBoxWordBoundaryDelimiter.Text;
			if( m_strWordBoundaryDelimiter.IndexOf('"') != -1 )
			{
				MessageBox.Show("Can't use the double-quote character for the word boundary delimiter",SpellingFixerEC.cstrCaption);
				return;
			}

			bool bRewriteCCTable = false;

			string strPunctuation = EncodePunctuationForCC(this.textBoxAddlPunctuation.Text);
			if(strPunctuation == null )
				return; // it had a bad character

			else if( strPunctuation != m_strNonWordCharacters )
			{
				// this means the file must be re-written
				bRewriteCCTable = true;
				m_strNonWordCharacters = strPunctuation;
			}

			if( (!m_bLegacy) != this.checkBoxUnicode.Checked )
			{
				m_bLegacy = !this.checkBoxUnicode.Checked;
				bRewriteCCTable = true;
			}

			// check for existing EncConverter with this same project information
			string strCCTableSpec = null;
			string strPartialName = this.textBoxNewProjectName.Text;
			string strEncConverterName = FullName(strPartialName);
			EncConverters aECs = new EncConverters();
			IEncConverter aEC = aECs[strEncConverterName];
			if( aEC != null )
			{
				// if we're *not* in edit mode
				if( this.buttonAddNewProject.Text == cstrAddNewProjectButtonText )
				{
					if( MessageBox.Show(String.Format("A project already exists by the name {0}. Click 'Yes' to overwrite", this.textBoxNewProjectName.Text),SpellingFixerEC.cstrCaption, MessageBoxButtons.YesNoCancel) == DialogResult.Yes)
					{
						// take it out of the check box list
						checkedListBoxProjects.Items.Remove(strPartialName);
						strCCTableSpec = aEC.ConverterIdentifier;
						if( File.Exists(strCCTableSpec) )
						{
							File.Delete(strCCTableSpec);
							strCCTableSpec = null;
						}

						// remove the existing one and we'll add a new one next
						aECs.Remove(aEC.Name);
						aEC = null;
					}
					else
						return;
				}
				else    // edit mode
				{
					if( MessageBox.Show(String.Format("Do you want to update the '{0}' project?", this.textBoxNewProjectName.Text),SpellingFixerEC.cstrCaption, MessageBoxButtons.YesNoCancel) == DialogResult.Yes)
					{
						// take it out of the check box list
						checkedListBoxProjects.Items.Remove(strPartialName);

						// save the spec so that we don't make one below
						strCCTableSpec = aEC.ConverterIdentifier;

						// the remove the converter since we'll add it back again next
						aECs.Remove(aEC.Name);
						aEC = null;
					}
					else
					{
						// reset this in case we were just editing it.
						ResetNewProjectLook();
						return;
					}
				}
			}

			// if we're aren't using the old cc table, then...
			if( strCCTableSpec == null )
			{
				// now add it (put it in the normal 'MapsTables' folder in \pf\cf\sil\...)
				string strMapsTableDir = GetMapTableFolderPath;
				strCCTableSpec = strMapsTableDir + @"\" + strEncConverterName + ".cct";
				if( File.Exists(strCCTableSpec) )
				{
					// the converter doesn't exist, but a file with the name we would have
					//  given it does... ask the user if they want to overwrite it.
					// TODO: this doesn't allow for complete flexibility. It might be nicer to
					//  allow for any arbitrary name, but not if noone complains.
					if( MessageBox.Show(String.Format("A file exists by the name {0}. Click 'Yes' to overwrite", strCCTableSpec),SpellingFixerEC.cstrCaption, MessageBoxButtons.YesNoCancel) == DialogResult.Yes)
					{
						File.Delete(strCCTableSpec);
#if WriteOnAdd
// if the user goes to add the first record, it'd be better if the file didn't exist because now we do some
//  preliminary testing whether the CC table already changes a word before adding a new one, but this causes
//  a non-trapable error if there are no rules in the file. So just create it when it is actually needed
						CreateCCTable(strCCTableSpec,strEncConverterName,m_strNonWordCharacters, !this.m_bLegacy);
#endif
					}
				}
#if WriteOnAdd
				else
					CreateCCTable(strCCTableSpec,strEncConverterName,m_strNonWordCharacters, !this.m_bLegacy);
#endif
				bRewriteCCTable = false;
			}

			// now add the EncConverter
			// TODO: EncConverters needs a new interface to get the defining encodingID from
			//  a FontName (so we can use it in this call) just like we can 'try' to get the
			//  code page given a font name (see 'CodePage' below)
			ConvType eConvType = (m_bLegacy)
				? ConvType.Legacy_to_Legacy : ConvType.Unicode_to_Unicode;

			aECs.Add(strEncConverterName,strCCTableSpec,eConvType,null,null,SpellingFixerEC.SFProcessType);

			Font font = null;
			try
			{
				font = new Font(comboBoxFont.SelectedItem.ToString(),Convert.ToSingle(listBoxFontSize.SelectedItem));
			}
			catch
			{
				MessageBox.Show("Couldn't create the selected font. Contact support");
				return;
			}

			// add this 'displaying font' information to the converter as properties/attributes
			ECAttributes aECAttrs = aECs.Attributes(strEncConverterName,AttributeType.Converter);
			aECAttrs.Add(SpellingFixerEC.cstrAttributeFontToUse,font.Name);
			aECAttrs.Add(SpellingFixerEC.cstrAttributeFontSizeToUse, font.Size);
			aECAttrs.Add(SpellingFixerEC.cstrAttributeWordBoundaryDelimiter, m_strWordBoundaryDelimiter);
			aECAttrs.Add(SpellingFixerEC.cstrAttributeNonWordChars, m_strNonWordCharacters);

			// if it's not Unicode, then we need a code page in order to convert from wide to
			//  narrow (when writing to the file).
			int cp = 0;
			if( m_bLegacy )
			{
				// try to get the code page from EncConverters
				try
				{
					cp = aECs.CodePage(font.Name);
				}
				catch
				{
					// if it fails, it means we don't have a mapping, so add one here.
					// TODO: it would be nice to have an encoding, but I'm loath to query
					//  the user for it here since it isn't extremely relevant to this app.
					cp = Convert.ToInt32(this.textBoxCP.Text);
					aECs.AddFont(font.Name,cp,null);
				}
			}

			if(bRewriteCCTable)    // we are going to continue using the old file... so we must re-write it.
			{
				// if it was legacy encoded, then we need to convert the data to narrow using
				//  the code page the user specified (or we got out of the repository)
				Encoding enc = null;
				if( m_bLegacy )
				{
					if (cp == EncConverters.cnSymbolFontCodePage)
						cp = EncConverters.cnIso8859_1CodePage;
					enc = Encoding.GetEncoding(cp);
				}
				else
					enc = new UTF8Encoding();

				DataTable   myTable;
				if( SpellingFixerEC.InitializeDataTableFromCCTable(strCCTableSpec, enc, m_strWordBoundaryDelimiter, out myTable) )
				{
					ReWriteCCTableHeader(strCCTableSpec,m_strNonWordCharacters,enc);
					SpellingFixerEC.AppendCCTableFromDataTable(strCCTableSpec, enc, m_strWordBoundaryDelimiter, m_strNonWordCharacters, myTable);
				}
			}

			// finally, add the new project to the now-visible checkbox list
			this.buttonOK.Enabled = checkedListBoxProjects.Visible = true;
			ClearClickedItems();
			checkedListBoxProjects.Items.Add(strPartialName,CheckState.Checked);

			// reset this in case we were just editing it.
			ResetNewProjectLook();
		}

		private void ResetNewProjectLook()
		{
			this.groupBoxNewProject.Text = cstrNewProjectGroupText;
			this.buttonAddNewProject.Text = cstrAddNewProjectButtonText;
			this.toolTips.SetToolTip(this.buttonAddNewProject, cstrAddNewProjectButtonToolTipText);
			this.textBoxNewProjectName.ReadOnly = false;
		}

		private void ClearClickedItems()
		{
			foreach(int indexChecked in checkedListBoxProjects.CheckedIndices)
				checkedListBoxProjects.SetItemCheckState(indexChecked,CheckState.Unchecked);
		}

		private void buttonOK_Click(object sender, System.EventArgs e)
		{
			// When the OK button is clicked, it means the user is choosing the project in
			//  the project checkbox list. So use *that* information only to fill in the
			//  member variables of what the caller needs (i.e. beware that it isn't
			//  necessarily true that we added a project during this instantiation, so don't
			//  depend on the internal variables (e.g. m_font, etc.) having something
			//  meaningful)
			Debug.Assert( checkedListBoxProjects.Visible );
			CheckedListBox.CheckedItemCollection aCheckedItems = checkedListBoxProjects.CheckedItems;

			// should only be once checked item
			Debug.Assert(aCheckedItems.Count == 1);
			if( aCheckedItems.Count != 1 )
				return;
			string strEncConverterName = aCheckedItems[0].ToString();

			if( LoadProject(strEncConverterName) )
			{
				DialogResult = DialogResult.OK;
				this.Close();
			}
		}

		internal bool LoadProject(string strProjectName)
		{
			// get the EncConverter that should have been added above by 'AddNewProject' button
			EncConverters aECs = new EncConverters();
			IEncConverter aEC = aECs[FullName(strProjectName)];
			if( aEC != null )
			{
				m_strEncConverterName = aEC.Name;
				m_strConverterSpec = aEC.ConverterIdentifier;
				ECAttributes aECAttrs = aECs.Attributes(aEC.Name,AttributeType.Converter);
				string strFontName = aECAttrs[SpellingFixerEC.cstrAttributeFontToUse];
				string sFontSize = aECAttrs[SpellingFixerEC.cstrAttributeFontSizeToUse];
				m_strWordBoundaryDelimiter = aECAttrs[SpellingFixerEC.cstrAttributeWordBoundaryDelimiter];
				m_strNonWordCharacters = aECAttrs[SpellingFixerEC.cstrAttributeNonWordChars];

				// new in 1.2 (so it might not exist)
				if( m_strNonWordCharacters == null )
					m_strNonWordCharacters = SpellingFixerEC.GetDefaultPunctuation;

				// if this was added (without having been made on this system), then
				//  these properties doesn't get added automatically. Must go to edit mode!
				if((strFontName == null)
					||  (sFontSize == null)
					||  (m_strWordBoundaryDelimiter == null) )
				{
					MessageBox.Show("It looks like this project was added to the repository incorrectly because it's missing some important properties. You'll need to edit it again to set the font, and other values.");
					DoEdit(aECs, aEC, strProjectName, strFontName, sFontSize);

					// make the "Update" button the default button
					this.AcceptButton = this.buttonAddNewProject;
					return false;
				}

				float fFontSize = (float)0.0;
				try
				{
					fFontSize = (float)Convert.ToSingle(sFontSize);
				}
				catch {}

				if( (strFontName != "") && (fFontSize != 0.0) )
					m_font = new Font(strFontName,fFontSize);

				m_bLegacy = (aEC.ConversionType == ConvType.Legacy_to_Legacy);
				if( m_bLegacy )
					m_cp = aECs.CodePage(strFontName);

				RegistryKey keyLastSFProject = Registry.CurrentUser.CreateSubKey(cstrProjectMemoryKey);
				keyLastSFProject.SetValue(cstrProjectMostRecentProject,strProjectName);
				return true;
			}

			return false;
		}

		private void buttonCancel_Click(object sender, System.EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			this.Close();
		}

		private void textBoxNewProjectName_TextChanged(object sender, System.EventArgs e)
		{
			// check to see if the Add button should be enable.
			EnableAddNewProjectButton();
		}

		private void comboBoxFont_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			// check to see if the Add button should be enable.
			EnableAddNewProjectButton();
		}

		private void listBoxFontSize_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			// check to see if the Add button should be enable.
			EnableAddNewProjectButton();
		}

		private void checkBoxUnicode_CheckedChanged(object sender, System.EventArgs e)
		{
			// check to see if the Add button should be enable.
			EnableAddNewProjectButton();
			bool bIsLegacy = !this.checkBoxUnicode.Checked;
			this.labelCP.Visible = this.textBoxCP.Visible = bIsLegacy;

			// if it's a legacy encoding, then see if the repository already has a cp for
			//  this font
			if( bIsLegacy )
			{
				EncConverters aECs = new EncConverters();
				int cp = (this.textBoxCP.Text != "") ? Convert.ToInt32(this.textBoxCP.Text) : 1252;
				try
				{
					if( (comboBoxFont.SelectedItem != null) && (comboBoxFont.SelectedItem.ToString() != "") )
						cp = aECs.CodePage(comboBoxFont.SelectedItem.ToString());
				}
				catch {}
				this.textBoxCP.Text = cp.ToString();
			}
		}

		private void textBoxCP_TextChanged(object sender, System.EventArgs e)
		{
			// check to see if the Add button should be enable.
			EnableAddNewProjectButton();
		}

		private void checkedListBoxProjects_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			int nIndex = checkedListBoxProjects.SelectedIndex;
			ClearClickedItems();
			if( nIndex != -1 )
				checkedListBoxProjects.SetItemCheckState(nIndex,CheckState.Checked);
		}

		// get the point at which the right mouse button was clicked (for subsequent pop-up
		//  menu processing)
		private Point m_ptRightClicked;
		private void checkedListBoxProjects_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			// don't want now that I support double-click if( e.Button == MouseButtons.Right )
			m_ptRightClicked = new Point(e.X,e.Y);
		}

		private void menuItemClick_Click(object sender, System.EventArgs e)
		{
			int nIndex = checkedListBoxProjects.IndexFromPoint(m_ptRightClicked);
			if( nIndex >= 0 )
			{
				ClearClickedItems();
				checkedListBoxProjects.SetItemChecked(nIndex,true);
			}
		}

		private void menuItemDelete_Click(object sender, System.EventArgs e)
		{
			int nIndex = checkedListBoxProjects.IndexFromPoint(m_ptRightClicked);
			if( nIndex >= 0 )
			{
				string strProjectName = checkedListBoxProjects.Items[nIndex].ToString();
				if( MessageBox.Show(String.Format("Are you sure you want to delete the '{0}' project?",strProjectName), SpellingFixerEC.cstrCaption, MessageBoxButtons.YesNoCancel) == DialogResult.Yes)
				{
					EncConverters aECs = new EncConverters();
					IEncConverter aEC = aECs[FullName(strProjectName)];
					if( aEC != null )
					{
						DialogResult res = MessageBox.Show(String.Format("Do you also want to delete the associated CC table '{0}'?", aEC.ConverterIdentifier), SpellingFixerEC.cstrCaption, MessageBoxButtons.YesNoCancel);
						if( res == DialogResult.Yes)
						{
							if( File.Exists(aEC.ConverterIdentifier) )
								File.Delete(aEC.ConverterIdentifier);
						}
						else if( res == DialogResult.Cancel )
							return;

						// remove it from the repository as well.
						aECs.Remove(aEC.Name);
					}

					checkedListBoxProjects.Items.Remove(strProjectName);

					if( this.checkedListBoxProjects.Items.Count == 0 )
						this.AcceptButton = this.buttonAddNewProject;
					EnableAddNewProjectButton();
				}
			}
		}

		private void menuItemEdit_Click(object sender, System.EventArgs e)
		{
			// Provide a way to edit the info (in case the user wants to change the
			//  font, size, or delimter.
			int nIndex = checkedListBoxProjects.IndexFromPoint(m_ptRightClicked);
			if( nIndex >= 0 )
			{
				string strProjectName = checkedListBoxProjects.Items[nIndex].ToString();
				EncConverters aECs = new EncConverters();
				IEncConverter aEC = aECs[FullName(strProjectName)];
				if( aEC != null )
				{
					m_strEncConverterName = aEC.Name;
					m_strConverterSpec = aEC.ConverterIdentifier;
					ECAttributes aECAttrs = aECs.Attributes(aEC.Name,AttributeType.Converter);

					string strFontName = aECAttrs[SpellingFixerEC.cstrAttributeFontToUse];
					string sFontSize = aECAttrs[SpellingFixerEC.cstrAttributeFontSizeToUse];
					m_strWordBoundaryDelimiter = aECAttrs[SpellingFixerEC.cstrAttributeWordBoundaryDelimiter];
					m_strNonWordCharacters = aECAttrs[SpellingFixerEC.cstrAttributeNonWordChars];

					DoEdit(aECs, aEC, strProjectName, strFontName, sFontSize);
				}

				// make the "Update" button the default button
				this.AcceptButton = this.buttonAddNewProject;
			}
		}

		private void DoEdit(EncConverters aECs, IEncConverter aEC, string strProjectName, string strFontName, string sFontSize)
		{
			this.textBoxNewProjectName.Text = strProjectName;
			this.textBoxWordBoundaryDelimiter.Text = m_strWordBoundaryDelimiter;

			// new in 1.2 (so it might not exist)
			this.textBoxAddlPunctuation.Text = this.DecodePunctuationForCC(m_strNonWordCharacters);

			this.listBoxFontSize.SelectedItem = sFontSize;
			this.comboBoxFont.SelectedItem = strFontName;

			m_bLegacy = (aEC.ConversionType == ConvType.Legacy_to_Legacy);
			this.checkBoxUnicode.Checked = !m_bLegacy;
			if( m_bLegacy )
			{
				this.labelCP.Visible = this.textBoxCP.Visible = true;
				this.textBoxCP.Text = aECs.CodePage(strFontName).ToString();
			}

			// update the "Add New Project" button to say "Update Project"
			this.groupBoxNewProject.Text = "Edit Project Settings";
			this.buttonAddNewProject.Text = "Update &Project";
			this.toolTips.SetToolTip(this.buttonAddNewProject, "Click to update the project information");
			this.textBoxNewProjectName.ReadOnly = true;
			EnableAddNewProjectButton();
		}

		private void menuItemDeleteAll_Click(object sender, System.EventArgs e)
		{
			// verify first!
			if( MessageBox.Show("Are you sure you want to delete all the existing projects?", SpellingFixerEC.cstrCaption, MessageBoxButtons.YesNoCancel) == DialogResult.Yes)
			{
				EncConverters aECs = new EncConverters();
				foreach(string strProjectName in checkedListBoxProjects.Items)
				{
					IEncConverter aEC = aECs[FullName(strProjectName)];
					if( aEC != null )
					{
						// if they do this, then don't bother querying about saving the files.
						if( File.Exists(aEC.ConverterIdentifier) )
							File.Delete(aEC.ConverterIdentifier);

						// remove it from the repository as well.
						aECs.Remove(aEC.Name);
					}
				}

				// now remove them from the checkbox list
				while( checkedListBoxProjects.Items.Count > 0 )
					checkedListBoxProjects.Items.RemoveAt(0);

				this.buttonOK.Enabled = this.checkedListBoxProjects.Visible = false;
				this.AcceptButton = this.buttonAddNewProject;
			}
		}

		// double click means edit
		private void checkedListBoxProjects_DoubleClick(object sender, EventArgs e)
		{
			this.menuItemEdit_Click(sender,e);
		}

		private void textBoxWordBoundaryDelimiter_TextChanged(object sender, EventArgs e)
		{
			EnableAddNewProjectButton();
		}
	}
}
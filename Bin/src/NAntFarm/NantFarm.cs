using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Diagnostics;

using System.IO;
using System.Xml;
using System.Xml.XPath;
using Microsoft.Win32;	// Registry

namespace NantTargetProject
{
	/// <summary>
	/// Summary description for NantProjectForm1.
	/// </summary>
	public class NantProjectForm1 : System.Windows.Forms.Form
	{
		private XmlDocument _nantProjects;
		private XmlNamespaceManager _nantFieldWorksNamespaceManager;

		private class Nant
		{
		}

		private class ControlTextDefaults
		{
			public const string commandLineBox = "nant";
			public class listView1_Config
			{
				private ArrayList sortList;
				private const string Column_TargetName = "Target name";
				private const string Column_Buildfile = "Build file";
				private const string Column_Project = "Project";
				private const string Column_Description = "Description";
				private const string Column_Dependencies = "Dependencies";

				public listView1_Config()
				{
					// first create dummy entries for each column
					string [] unsortedList = {Column_TargetName,
											  Column_Buildfile,
											  Column_Project,
											  Column_Description,
											  Column_Dependencies};
					this.sortList = new ArrayList();
					sortList.AddRange( unsortedList );

					// now sort them
					sortList[ (int) ColumnOrder.TargetName ] = Column_TargetName;
					sortList[ (int) ColumnOrder.Project ] =  Column_Project;
					sortList[ (int) ColumnOrder.Description ] = Column_Description;
					sortList[ (int) ColumnOrder.Dependencies ] = Column_Dependencies;
					sortList[ (int) ColumnOrder.Buildfile ] = Column_Buildfile;

				}

				public enum ColumnOrder
				{
					TargetName = 0,
					Project,
					Description,
					Dependencies,
					Buildfile
				}


				// REVISIT: should we implement our own ILIST for ColumnHeadings?
				public ArrayList ColumnHeadings
				{
					get
					{
						return( this.sortList );
					}
				}
			}

		}

		NantProjectForm1.ControlTextDefaults.listView1_Config listView1_Settings;
		private System.Windows.Forms.ListView listView1;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Panel panel2;
		private System.Windows.Forms.ComboBox commandLineBox;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Panel panel3;
		private System.Windows.Forms.Splitter splitter1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.CheckedListBox buildFlagsChecklistBox;
		private System.Windows.Forms.ComboBox actionComboBox;
		private System.Windows.Forms.ComboBox configurationComboBox;
		private System.Windows.Forms.ToolTip toolTip1;
		private System.Windows.Forms.Button resetCmdLineButton;
		private System.Windows.Forms.Button copyCmdLineButton;
		private System.Windows.Forms.GroupBox commandLineGroup;
		private System.Windows.Forms.TextBox buildOptionInfoBox;
		private System.Windows.Forms.TabControl tabControl1;
		private System.Windows.Forms.TabPage basicInfoTabPage;
		private System.Windows.Forms.TabPage dependencyTabPage;
		private System.Windows.Forms.TabPage dependentsTabPage;
		private System.Windows.Forms.TextBox selectedTargetNameTextBox;
		private System.Windows.Forms.TextBox targetProjectTextBox;
		private System.Windows.Forms.Label projectLabel;
		private System.Windows.Forms.Label targetLabel;
		private System.Windows.Forms.GroupBox targetHeaderGroupBox;
		private System.Windows.Forms.TextBox targetInfoTextBox;
		private System.Windows.Forms.TextBox selectedTargetDescriptionTextBox;
		private System.Windows.Forms.Label targetDescriptionLabel;
		private System.Windows.Forms.GroupBox findListViewItemGroupBox;
		private System.Windows.Forms.TextBox findListViewItemInputTextBox;
		private System.Windows.Forms.Button listView1GoToItemResetButton;
		private System.Windows.Forms.TabPage callTargetTabPage;
		private System.Windows.Forms.ListView callsListView;
		private System.Windows.Forms.ColumnHeader Target;
		private System.Windows.Forms.TabPage callerTargetsTabPage;
		private System.Windows.Forms.ColumnHeader Relationship;
		private System.Windows.Forms.ColumnHeader Order;
		private System.ComponentModel.IContainer components;

		public NantProjectForm1(XmlDocument projects)
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
			// ASSERT private arguments are not null
			this._nantProjects = projects;
			this._nantFieldWorksNamespaceManager = nsmgr_Init(this._nantProjects);

			this.ListView1_Initialize();
			this.resetCommandLineText();
			this.LoadCommandLineOptions();
		}

		private XmlNamespaceManager nsmgr_Init(XmlDocument projectCollection)
		{
			XPathNavigator nav = projectCollection.CreateNavigator();
			XmlNamespaceManager context = new XmlNamespaceManager(nav.NameTable);
			// establish default namespace
			context.AddNamespace("", FieldWorksSettings.FieldWorksNamespace);
			// bind FieldWorks prefix
			context.AddNamespace(FieldWorksSettings.FieldWorksNamespacePrefix, FieldWorksSettings.FieldWorksNamespace);
			return context;
		}

		private void ListView1_Initialize()
		{
			// ASSERT private arguments are not null
			if (this._nantProjects == null || this.listView1 == null)
				throw (new ArgumentNullException());

			this.ListView1_LoadHeaders();
			this.ListView1_LoadItems();
			// Setup Item Sorter
			this.listView1.ListViewItemSorter = new ListViewItemComparer(0);

		}

		private void ListView1_LoadHeaders()
		{
			// ASSERT private arguments are not null
			if (this._nantProjects == null || this.listView1 == null)
				throw (new ArgumentNullException());

			// ASSERT that we haven't already setup these Columns
			if (this.listView1_Settings != null || this.listView1.Columns.Count > 0)
				throw (new ArgumentException("We've already setup listView Columns."));

			// initialize listView1 settings
			this.listView1_Settings = new NantProjectForm1.ControlTextDefaults.listView1_Config();

			// REVISIT: we should put all the header settings in ControlTextDefaults.listView1_Config();
			// add Column Header
			ColumnHeader header1 = new ColumnHeader();  // Target
			ColumnHeader header2 = new ColumnHeader();	// Project
			ColumnHeader header3 = new ColumnHeader();	// Description
			ColumnHeader header4 = new ColumnHeader();  // Depends
			ColumnHeader header5 = new ColumnHeader();  // BuildFile

			// Set the text, alignment and width for each column header.
			header1.Text = this.listView1_Settings.ColumnHeadings[ 0 ].ToString();
			header1.TextAlign = HorizontalAlignment.Left;
			header1.Width = 175;

			// Set the text, alignment and width for each column header.
			header2.Text = this.listView1_Settings.ColumnHeadings[ 1 ].ToString();
			header2.TextAlign = HorizontalAlignment.Left;
			header2.Width = 125;

			// Set the text, alignment and width for each column header.
			header3.Text = this.listView1_Settings.ColumnHeadings[ 2 ].ToString();
			header3.TextAlign = HorizontalAlignment.Left;
			header3.Width = 300;

			// Set the text, alignment and width for each column header.
			header4.Text = this.listView1_Settings.ColumnHeadings[ 3 ].ToString();
			header4.TextAlign = HorizontalAlignment.Left;
			header4.Width = 300;

			// Set the text, alignment and width for each column header.
			header5.Text = this.listView1_Settings.ColumnHeadings[ 4 ].ToString();
			header5.TextAlign = HorizontalAlignment.Left;
			header5.Width = 300;

			// Add the headers to the ListView control.
			this.listView1.Columns.Add(header1);
			this.listView1.Columns.Add(header2);
			this.listView1.Columns.Add(header3);
			this.listView1.Columns.Add(header4);
			this.listView1.Columns.Add(header5);

		}

		private void ListView1_LoadItems()
		{
			// ASSERT private arguments are not null
			if (this._nantProjects == null || this.listView1 == null)
				throw (new ArgumentNullException());

			// ASSERT that we have setup colum headers, but no items.
			if (this.listView1.Columns.Count == 0 || this.listView1.Items.Count > 0)
				throw (new ArgumentException());

			// Apply xpath Project filter(s)
			// 1) Include all targets that are not in Special Targets
			// const string xpathFilter = "//target[ not (ancestor::project[@name='Targets']) ]";
			// const string xpathFilter = "//target[ descendant::call or @depends ]";
			// const string xpathFilter = "//target[ not (descendant::call) ]";

			// add all targets that are not in SpecialTarget File;
			//const string xpathFilter = "//target[ not (ancestor::project[@name='"
			//										+ FieldWorksSettings.SpecialTargets.ProjectName +"']) ]";
			string xpathFilter = FieldWorksSettings.FormatXpathForFieldWorksNameSpace("//{0}:target");

			// next get all target names in projects
			XmlNodeList nantTargets = _nantProjects.SelectNodes(xpathFilter, this._nantFieldWorksNamespaceManager);

			listView1.BeginUpdate();

			foreach (XmlNode targetNode in nantTargets)
			{
				// REVISIT: we should tie the item ordering and xpath info to ControlTextDefaults.listView1_Config class
				string targetName = targetNode.Attributes.GetNamedItem("name").Value;
				Debug.Assert(targetName == targetName.Trim(), "target=\"" + targetName + "\"" +
					" contains leading or trailing whitespace. NantFarm may not be able to find all dependents of this target.");
				ListViewItem item = new ListViewItem(targetName);

				//Get the Project associated with this Target
				item.SubItems.Add( targetNode.ParentNode.Attributes.GetNamedItem("name").Value );

				string [] attributes = {"description","depends"}; // scope issue?
				foreach (string attr in attributes)
				{
					XmlNode targetAttrib = targetNode.Attributes.GetNamedItem( attr );

					if(targetAttrib != null)
						item.SubItems.Add(targetAttrib.Value);
					else
						item.SubItems.Add("");
				}
				//Get Build File associated with this Target
				item.SubItems.Add( targetNode.ParentNode.Attributes.GetNamedItem("file").Value );

				listView1.Items.Add(item);
			}

			listView1.EndUpdate();

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
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(NantProjectForm1));
			this.listView1 = new System.Windows.Forms.ListView();
			this.panel1 = new System.Windows.Forms.Panel();
			this.findListViewItemGroupBox = new System.Windows.Forms.GroupBox();
			this.listView1GoToItemResetButton = new System.Windows.Forms.Button();
			this.findListViewItemInputTextBox = new System.Windows.Forms.TextBox();
			this.targetInfoTextBox = new System.Windows.Forms.TextBox();
			this.panel2 = new System.Windows.Forms.Panel();
			this.commandLineGroup = new System.Windows.Forms.GroupBox();
			this.resetCmdLineButton = new System.Windows.Forms.Button();
			this.copyCmdLineButton = new System.Windows.Forms.Button();
			this.buildFlagsChecklistBox = new System.Windows.Forms.CheckedListBox();
			this.commandLineBox = new System.Windows.Forms.ComboBox();
			this.label2 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.actionComboBox = new System.Windows.Forms.ComboBox();
			this.configurationComboBox = new System.Windows.Forms.ComboBox();
			this.buildOptionInfoBox = new System.Windows.Forms.TextBox();
			this.panel3 = new System.Windows.Forms.Panel();
			this.tabControl1 = new System.Windows.Forms.TabControl();
			this.basicInfoTabPage = new System.Windows.Forms.TabPage();
			this.dependencyTabPage = new System.Windows.Forms.TabPage();
			this.dependentsTabPage = new System.Windows.Forms.TabPage();
			this.callTargetTabPage = new System.Windows.Forms.TabPage();
			this.callsListView = new System.Windows.Forms.ListView();
			this.Target = new System.Windows.Forms.ColumnHeader();
			this.Relationship = new System.Windows.Forms.ColumnHeader();
			this.Order = new System.Windows.Forms.ColumnHeader();
			this.callerTargetsTabPage = new System.Windows.Forms.TabPage();
			this.targetHeaderGroupBox = new System.Windows.Forms.GroupBox();
			this.targetDescriptionLabel = new System.Windows.Forms.Label();
			this.selectedTargetDescriptionTextBox = new System.Windows.Forms.TextBox();
			this.selectedTargetNameTextBox = new System.Windows.Forms.TextBox();
			this.projectLabel = new System.Windows.Forms.Label();
			this.targetLabel = new System.Windows.Forms.Label();
			this.targetProjectTextBox = new System.Windows.Forms.TextBox();
			this.splitter1 = new System.Windows.Forms.Splitter();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.panel1.SuspendLayout();
			this.findListViewItemGroupBox.SuspendLayout();
			this.panel2.SuspendLayout();
			this.commandLineGroup.SuspendLayout();
			this.panel3.SuspendLayout();
			this.tabControl1.SuspendLayout();
			this.basicInfoTabPage.SuspendLayout();
			this.callTargetTabPage.SuspendLayout();
			this.targetHeaderGroupBox.SuspendLayout();
			this.SuspendLayout();
			//
			// listView1
			//
			this.listView1.Alignment = System.Windows.Forms.ListViewAlignment.Default;
			this.listView1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.listView1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.listView1.FullRowSelect = true;
			this.listView1.HideSelection = false;
			this.listView1.LabelWrap = false;
			this.listView1.Location = new System.Drawing.Point(0, 0);
			this.listView1.MultiSelect = false;
			this.listView1.Name = "listView1";
			this.listView1.Size = new System.Drawing.Size(472, 366);
			this.listView1.Sorting = System.Windows.Forms.SortOrder.Ascending;
			this.listView1.TabIndex = 0;
			this.listView1.View = System.Windows.Forms.View.Details;
			this.listView1.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.ColumnClick);
			this.listView1.SelectedIndexChanged += new System.EventHandler(this.listView1_SelectedIndexChanged);
			//
			// panel1
			//
			this.panel1.AutoScroll = true;
			this.panel1.Controls.Add(this.listView1);
			this.panel1.Controls.Add(this.findListViewItemGroupBox);
			this.panel1.Dock = System.Windows.Forms.DockStyle.Left;
			this.panel1.Location = new System.Drawing.Point(0, 0);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(472, 422);
			this.panel1.TabIndex = 2;
			this.panel1.Paint += new System.Windows.Forms.PaintEventHandler(this.panel1_Paint);
			//
			// findListViewItemGroupBox
			//
			this.findListViewItemGroupBox.Controls.Add(this.listView1GoToItemResetButton);
			this.findListViewItemGroupBox.Controls.Add(this.findListViewItemInputTextBox);
			this.findListViewItemGroupBox.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.findListViewItemGroupBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.findListViewItemGroupBox.Location = new System.Drawing.Point(0, 366);
			this.findListViewItemGroupBox.Name = "findListViewItemGroupBox";
			this.findListViewItemGroupBox.Size = new System.Drawing.Size(472, 56);
			this.findListViewItemGroupBox.TabIndex = 1;
			this.findListViewItemGroupBox.TabStop = false;
			this.findListViewItemGroupBox.Text = "GoTo Item";
			//
			// listView1GoToItemResetButton
			//
			this.listView1GoToItemResetButton.Location = new System.Drawing.Point(360, 22);
			this.listView1GoToItemResetButton.Name = "listView1GoToItemResetButton";
			this.listView1GoToItemResetButton.TabIndex = 1;
			this.listView1GoToItemResetButton.Text = "Reset";
			this.listView1GoToItemResetButton.Click += new System.EventHandler(this.listView1GoToItemResetButton_Click);
			//
			// findListViewItemInputTextBox
			//
			this.findListViewItemInputTextBox.Location = new System.Drawing.Point(15, 21);
			this.findListViewItemInputTextBox.Name = "findListViewItemInputTextBox";
			this.findListViewItemInputTextBox.Size = new System.Drawing.Size(337, 20);
			this.findListViewItemInputTextBox.TabIndex = 0;
			this.findListViewItemInputTextBox.Text = "";
			this.findListViewItemInputTextBox.TextChanged += new System.EventHandler(this.findListViewItemInputTextBox_TextChanged);
			//
			// targetInfoTextBox
			//
			this.targetInfoTextBox.AcceptsReturn = true;
			this.targetInfoTextBox.AcceptsTab = true;
			this.targetInfoTextBox.BackColor = System.Drawing.SystemColors.Info;
			this.targetInfoTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.targetInfoTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.targetInfoTextBox.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.targetInfoTextBox.Location = new System.Drawing.Point(0, 0);
			this.targetInfoTextBox.Multiline = true;
			this.targetInfoTextBox.Name = "targetInfoTextBox";
			this.targetInfoTextBox.ReadOnly = true;
			this.targetInfoTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.targetInfoTextBox.Size = new System.Drawing.Size(377, 290);
			this.targetInfoTextBox.TabIndex = 3;
			this.targetInfoTextBox.Text = "(No target selected.)";
			this.targetInfoTextBox.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
			//
			// panel2
			//
			this.panel2.Controls.Add(this.commandLineGroup);
			this.panel2.Dock = System.Windows.Forms.DockStyle.Top;
			this.panel2.Location = new System.Drawing.Point(0, 0);
			this.panel2.Name = "panel2";
			this.panel2.Size = new System.Drawing.Size(864, 72);
			this.panel2.TabIndex = 1;
			//
			// commandLineGroup
			//
			this.commandLineGroup.Controls.Add(this.resetCmdLineButton);
			this.commandLineGroup.Controls.Add(this.copyCmdLineButton);
			this.commandLineGroup.Controls.Add(this.buildFlagsChecklistBox);
			this.commandLineGroup.Controls.Add(this.commandLineBox);
			this.commandLineGroup.Controls.Add(this.label2);
			this.commandLineGroup.Controls.Add(this.label1);
			this.commandLineGroup.Controls.Add(this.actionComboBox);
			this.commandLineGroup.Controls.Add(this.configurationComboBox);
			this.commandLineGroup.Controls.Add(this.buildOptionInfoBox);
			this.commandLineGroup.Location = new System.Drawing.Point(11, 1);
			this.commandLineGroup.Name = "commandLineGroup";
			this.commandLineGroup.Size = new System.Drawing.Size(837, 64);
			this.commandLineGroup.TabIndex = 9;
			this.commandLineGroup.TabStop = false;
			this.commandLineGroup.Text = "groupBox1";
			//
			// resetCmdLineButton
			//
			this.resetCmdLineButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.resetCmdLineButton.Location = new System.Drawing.Point(128, 38);
			this.resetCmdLineButton.Name = "resetCmdLineButton";
			this.resetCmdLineButton.TabIndex = 7;
			this.resetCmdLineButton.Text = "Reset";
			this.toolTip1.SetToolTip(this.resetCmdLineButton, "Reset command line");
			this.resetCmdLineButton.Click += new System.EventHandler(this.resetCmdLineButton_Click);
			//
			// copyCmdLineButton
			//
			this.copyCmdLineButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.copyCmdLineButton.Location = new System.Drawing.Point(208, 38);
			this.copyCmdLineButton.Name = "copyCmdLineButton";
			this.copyCmdLineButton.Size = new System.Drawing.Size(104, 23);
			this.copyCmdLineButton.TabIndex = 8;
			this.copyCmdLineButton.Text = "CopyToClipboard";
			this.toolTip1.SetToolTip(this.copyCmdLineButton, "Copy command line to clipboard");
			this.copyCmdLineButton.Click += new System.EventHandler(this.copyCmdLineButton_Click);
			//
			// buildFlagsChecklistBox
			//
			this.buildFlagsChecklistBox.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.buildFlagsChecklistBox.Location = new System.Drawing.Point(432, 8);
			this.buildFlagsChecklistBox.Name = "buildFlagsChecklistBox";
			this.buildFlagsChecklistBox.Size = new System.Drawing.Size(136, 49);
			this.buildFlagsChecklistBox.TabIndex = 5;
			this.toolTip1.SetToolTip(this.buildFlagsChecklistBox, "build flags");
			this.buildFlagsChecklistBox.SelectedIndexChanged += new System.EventHandler(this.buildFlagsChecklistBox_SelectedIndexChanged);
			this.buildFlagsChecklistBox.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.buildFlagsChecklistBox_ItemCheck);
			//
			// commandLineBox
			//
			this.commandLineBox.Location = new System.Drawing.Point(11, 16);
			this.commandLineBox.Name = "commandLineBox";
			this.commandLineBox.Size = new System.Drawing.Size(301, 21);
			this.commandLineBox.TabIndex = 1;
			this.commandLineBox.Text = "nant";
			this.toolTip1.SetToolTip(this.commandLineBox, "nant [<configuration> <action> flags] target");
			this.commandLineBox.TextChanged += new System.EventHandler(this.commandLineBox_TextChanged);
			this.commandLineBox.SelectedIndexChanged += new System.EventHandler(this.commandLineBox_SelectedIndexChanged);
			//
			// label2
			//
			this.label2.BackColor = System.Drawing.SystemColors.Control;
			this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.label2.Location = new System.Drawing.Point(354, 0);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(72, 16);
			this.label2.TabIndex = 6;
			this.label2.Text = "Build options";
			//
			// label1
			//
			this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.label1.Location = new System.Drawing.Point(11, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(113, 16);
			this.label1.TabIndex = 2;
			this.label1.Text = "Command Line Box";
			this.label1.Click += new System.EventHandler(this.label1_Click);
			//
			// actionComboBox
			//
			this.actionComboBox.Location = new System.Drawing.Point(328, 38);
			this.actionComboBox.Name = "actionComboBox";
			this.actionComboBox.Size = new System.Drawing.Size(96, 21);
			this.actionComboBox.TabIndex = 4;
			this.actionComboBox.Text = "(action)";
			this.toolTip1.SetToolTip(this.actionComboBox, "build action");
			this.actionComboBox.SelectedIndexChanged += new System.EventHandler(this.actionComboBox_SelectedIndexChanged);
			//
			// configurationComboBox
			//
			this.configurationComboBox.Location = new System.Drawing.Point(328, 16);
			this.configurationComboBox.Name = "configurationComboBox";
			this.configurationComboBox.Size = new System.Drawing.Size(96, 21);
			this.configurationComboBox.TabIndex = 3;
			this.configurationComboBox.Text = "(configuration)";
			this.toolTip1.SetToolTip(this.configurationComboBox, "build configuration");
			this.configurationComboBox.SelectedIndexChanged += new System.EventHandler(this.configurationComboBox_SelectedIndexChanged);
			//
			// buildOptionInfoBox
			//
			this.buildOptionInfoBox.AcceptsReturn = true;
			this.buildOptionInfoBox.BackColor = System.Drawing.Color.LightSteelBlue;
			this.buildOptionInfoBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.buildOptionInfoBox.Location = new System.Drawing.Point(576, 9);
			this.buildOptionInfoBox.Multiline = true;
			this.buildOptionInfoBox.Name = "buildOptionInfoBox";
			this.buildOptionInfoBox.ReadOnly = true;
			this.buildOptionInfoBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.buildOptionInfoBox.Size = new System.Drawing.Size(256, 48);
			this.buildOptionInfoBox.TabIndex = 10;
			this.buildOptionInfoBox.Text = "(select build option for description)";
			this.toolTip1.SetToolTip(this.buildOptionInfoBox, "Description for selected build-option.");
			//
			// panel3
			//
			this.panel3.AutoScroll = true;
			this.panel3.AutoScrollMargin = new System.Drawing.Size(200, 0);
			this.panel3.AutoScrollMinSize = new System.Drawing.Size(200, 0);
			this.panel3.Controls.Add(this.tabControl1);
			this.panel3.Controls.Add(this.targetHeaderGroupBox);
			this.panel3.Controls.Add(this.splitter1);
			this.panel3.Controls.Add(this.panel1);
			this.panel3.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel3.Location = new System.Drawing.Point(0, 72);
			this.panel3.Name = "panel3";
			this.panel3.Size = new System.Drawing.Size(864, 422);
			this.panel3.TabIndex = 3;
			//
			// tabControl1
			//
			this.tabControl1.Controls.Add(this.basicInfoTabPage);
			this.tabControl1.Controls.Add(this.dependencyTabPage);
			this.tabControl1.Controls.Add(this.dependentsTabPage);
			this.tabControl1.Controls.Add(this.callTargetTabPage);
			this.tabControl1.Controls.Add(this.callerTargetsTabPage);
			this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tabControl1.Location = new System.Drawing.Point(477, 104);
			this.tabControl1.Name = "tabControl1";
			this.tabControl1.SelectedIndex = 0;
			this.tabControl1.Size = new System.Drawing.Size(387, 318);
			this.tabControl1.TabIndex = 5;
			this.toolTip1.SetToolTip(this.tabControl1, "Tab Pages display information about the selected target.");
			this.tabControl1.SelectedIndexChanged += new System.EventHandler(this.tabControl1_SelectedIndexChanged);
			//
			// basicInfoTabPage
			//
			this.basicInfoTabPage.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.basicInfoTabPage.Controls.Add(this.targetInfoTextBox);
			this.basicInfoTabPage.Location = new System.Drawing.Point(4, 22);
			this.basicInfoTabPage.Name = "basicInfoTabPage";
			this.basicInfoTabPage.Size = new System.Drawing.Size(379, 292);
			this.basicInfoTabPage.TabIndex = 0;
			this.basicInfoTabPage.Text = "Basic Info";
			this.basicInfoTabPage.ToolTipText = "Show some basic information for selected target.";
			//
			// dependencyTabPage
			//
			this.dependencyTabPage.Location = new System.Drawing.Point(4, 22);
			this.dependencyTabPage.Name = "dependencyTabPage";
			this.dependencyTabPage.Size = new System.Drawing.Size(379, 292);
			this.dependencyTabPage.TabIndex = 1;
			this.dependencyTabPage.Text = "Total Dependencies";
			this.dependencyTabPage.ToolTipText = "Show all the targets that the selected target depends upon for a build.";
			//
			// dependentsTabPage
			//
			this.dependentsTabPage.Location = new System.Drawing.Point(4, 22);
			this.dependentsTabPage.Name = "dependentsTabPage";
			this.dependentsTabPage.Size = new System.Drawing.Size(379, 292);
			this.dependentsTabPage.TabIndex = 2;
			this.dependentsTabPage.Text = "Total Dependents";
			this.dependentsTabPage.ToolTipText = "Show all the targets that depend upon selected target for their build.";
			//
			// callTargetTabPage
			//
			this.callTargetTabPage.Controls.Add(this.callsListView);
			this.callTargetTabPage.Location = new System.Drawing.Point(4, 22);
			this.callTargetTabPage.Name = "callTargetTabPage";
			this.callTargetTabPage.Size = new System.Drawing.Size(379, 292);
			this.callTargetTabPage.TabIndex = 3;
			this.callTargetTabPage.Text = "Calls";
			this.callTargetTabPage.ToolTipText = "Show all the targets that the selected target (may possibly) call.";
			//
			// callsListView
			//
			this.callsListView.Alignment = System.Windows.Forms.ListViewAlignment.Default;
			this.callsListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
																							this.Target,
																							this.Relationship,
																							this.Order});
			this.callsListView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.callsListView.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.callsListView.FullRowSelect = true;
			this.callsListView.HideSelection = false;
			this.callsListView.LabelWrap = false;
			this.callsListView.Location = new System.Drawing.Point(0, 0);
			this.callsListView.MultiSelect = false;
			this.callsListView.Name = "callsListView";
			this.callsListView.Size = new System.Drawing.Size(379, 292);
			this.callsListView.TabIndex = 1;
			this.callsListView.View = System.Windows.Forms.View.Details;
			this.callsListView.SelectedIndexChanged += new System.EventHandler(this.callsListView_SelectedIndexChanged);
			//
			// Target
			//
			this.Target.Text = "Target";
			this.Target.Width = 212;
			//
			// Relationship
			//
			this.Relationship.Text = "Relationship";
			this.Relationship.Width = 74;
			//
			// Order
			//
			this.Order.Text = "Order";
			this.Order.Width = 42;
			//
			// callerTargetsTabPage
			//
			this.callerTargetsTabPage.Location = new System.Drawing.Point(4, 22);
			this.callerTargetsTabPage.Name = "callerTargetsTabPage";
			this.callerTargetsTabPage.Size = new System.Drawing.Size(379, 292);
			this.callerTargetsTabPage.TabIndex = 4;
			this.callerTargetsTabPage.Text = "Callers";
			//
			// targetHeaderGroupBox
			//
			this.targetHeaderGroupBox.Controls.Add(this.targetDescriptionLabel);
			this.targetHeaderGroupBox.Controls.Add(this.selectedTargetDescriptionTextBox);
			this.targetHeaderGroupBox.Controls.Add(this.selectedTargetNameTextBox);
			this.targetHeaderGroupBox.Controls.Add(this.projectLabel);
			this.targetHeaderGroupBox.Controls.Add(this.targetLabel);
			this.targetHeaderGroupBox.Controls.Add(this.targetProjectTextBox);
			this.targetHeaderGroupBox.Dock = System.Windows.Forms.DockStyle.Top;
			this.targetHeaderGroupBox.Location = new System.Drawing.Point(477, 0);
			this.targetHeaderGroupBox.Name = "targetHeaderGroupBox";
			this.targetHeaderGroupBox.Size = new System.Drawing.Size(387, 104);
			this.targetHeaderGroupBox.TabIndex = 8;
			this.targetHeaderGroupBox.TabStop = false;
			//
			// targetDescriptionLabel
			//
			this.targetDescriptionLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.targetDescriptionLabel.Location = new System.Drawing.Point(2, 42);
			this.targetDescriptionLabel.Name = "targetDescriptionLabel";
			this.targetDescriptionLabel.Size = new System.Drawing.Size(64, 13);
			this.targetDescriptionLabel.TabIndex = 12;
			this.targetDescriptionLabel.Text = "Description";
			//
			// selectedTargetDescriptionTextBox
			//
			this.selectedTargetDescriptionTextBox.AcceptsReturn = true;
			this.selectedTargetDescriptionTextBox.BackColor = System.Drawing.Color.LightSteelBlue;
			this.selectedTargetDescriptionTextBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.selectedTargetDescriptionTextBox.Location = new System.Drawing.Point(72, 40);
			this.selectedTargetDescriptionTextBox.Multiline = true;
			this.selectedTargetDescriptionTextBox.Name = "selectedTargetDescriptionTextBox";
			this.selectedTargetDescriptionTextBox.ReadOnly = true;
			this.selectedTargetDescriptionTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.selectedTargetDescriptionTextBox.Size = new System.Drawing.Size(296, 32);
			this.selectedTargetDescriptionTextBox.TabIndex = 11;
			this.selectedTargetDescriptionTextBox.Text = "";
			this.toolTip1.SetToolTip(this.selectedTargetDescriptionTextBox, "Description for selected target");
			//
			// selectedTargetNameTextBox
			//
			this.selectedTargetNameTextBox.BackColor = System.Drawing.Color.MistyRose;
			this.selectedTargetNameTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.selectedTargetNameTextBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.selectedTargetNameTextBox.Location = new System.Drawing.Point(72, 14);
			this.selectedTargetNameTextBox.Name = "selectedTargetNameTextBox";
			this.selectedTargetNameTextBox.ReadOnly = true;
			this.selectedTargetNameTextBox.Size = new System.Drawing.Size(206, 20);
			this.selectedTargetNameTextBox.TabIndex = 4;
			this.selectedTargetNameTextBox.Text = "";
			this.toolTip1.SetToolTip(this.selectedTargetNameTextBox, "Name of selected Target");
			//
			// projectLabel
			//
			this.projectLabel.Location = new System.Drawing.Point(28, 81);
			this.projectLabel.Name = "projectLabel";
			this.projectLabel.Size = new System.Drawing.Size(40, 13);
			this.projectLabel.TabIndex = 6;
			this.projectLabel.Text = "Project";
			//
			// targetLabel
			//
			this.targetLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.targetLabel.Location = new System.Drawing.Point(24, 16);
			this.targetLabel.Name = "targetLabel";
			this.targetLabel.Size = new System.Drawing.Size(40, 13);
			this.targetLabel.TabIndex = 7;
			this.targetLabel.Text = "Target";
			//
			// targetProjectTextBox
			//
			this.targetProjectTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.targetProjectTextBox.Location = new System.Drawing.Point(72, 79);
			this.targetProjectTextBox.Name = "targetProjectTextBox";
			this.targetProjectTextBox.ReadOnly = true;
			this.targetProjectTextBox.Size = new System.Drawing.Size(206, 20);
			this.targetProjectTextBox.TabIndex = 5;
			this.targetProjectTextBox.Text = "";
			//
			// splitter1
			//
			this.splitter1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.splitter1.Location = new System.Drawing.Point(472, 0);
			this.splitter1.Name = "splitter1";
			this.splitter1.Size = new System.Drawing.Size(5, 422);
			this.splitter1.TabIndex = 4;
			this.splitter1.TabStop = false;
			//
			// NantProjectForm1
			//
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(864, 494);
			this.Controls.Add(this.panel3);
			this.Controls.Add(this.panel2);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "NantProjectForm1";
			this.Text = "NAnt Farm (FieldWorks Target Viewer)";
			this.TransparencyKey = System.Drawing.Color.Magenta;
			this.Load += new System.EventHandler(this.NantProjectForm1_Load);
			this.panel1.ResumeLayout(false);
			this.findListViewItemGroupBox.ResumeLayout(false);
			this.panel2.ResumeLayout(false);
			this.commandLineGroup.ResumeLayout(false);
			this.panel3.ResumeLayout(false);
			this.tabControl1.ResumeLayout(false);
			this.basicInfoTabPage.ResumeLayout(false);
			this.callTargetTabPage.ResumeLayout(false);
			this.targetHeaderGroupBox.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		private string[] SplitDependencyString(string depends)
		{
			depends = depends.Trim();
			// split on (adjacent) comma(s) and any leading/trailing whitespace
			Regex dependsRegex = new Regex(@"\s*,,*\s*");
			return dependsRegex.Split( depends );
		}

		// REVISIT: we assume that all named (direct) dependents in
		// buildFiles have corresponding <target> definitions
		private ArrayList GetFullDependentList(string targetName)
		{
			ArrayList fullDependentList = new ArrayList();

			if ( targetName  == string.Empty )
				return fullDependentList;

			// create a nextTarget stack from which we will add only unique dependencies to fullDependsList;
			Queue nextTargets = new Queue();
			XmlNode targetNode = null;

			nextTargets.Enqueue( targetName );
			string nextTargetName = "";

			do
			{
				nextTargetName = (string) nextTargets.Dequeue();

				if( nextTargetName == null || nextTargetName == string.Empty)
					throw (new ArgumentNullException());

				// find this targetNode (assume there is only one such target node)
				targetNode = _nantProjects.SelectSingleNode( FieldWorksSettings.FormatXpathForFieldWorksNameSpace("//{0}:target[@name='" + nextTargetName + "']"),
					this._nantFieldWorksNamespaceManager);

				if (targetNode == null)
					throw (new ArgumentNullException());

				// Get Direct Dependents for this targetNode
				ArrayList directDependents = new ArrayList();
				directDependents.AddRange( this.GetDirectDependents( nextTargetName ) );

				if (directDependents.Count > 0)
				{
					// Add dependencies to dependency stack
					foreach (string dependent in directDependents )
					{
						// Process only the dependents that we haven't already found
						if ( fullDependentList.Contains( dependent ) == false)
						{
							nextTargets.Enqueue( dependent );
							fullDependentList.Add( dependent );
						}
					}
				}

			} while (nextTargets.Count > 0);

			return fullDependentList;
		}

		// REVISIT: we currently assume that all named (direct) dependencies in
		// buildFiles have corresponding <target> definitions
		private ArrayList GetFullDependencyList(string targetName)
		{
			ArrayList fullDependsList = new ArrayList();
			if ( targetName  == string.Empty )
				return fullDependsList;

			// create a nextTarget stack from which we will add only unique dependencies to fullDependsList;
			Queue nextTargets = new Queue();
			XmlNode targetNode = null;

			nextTargets.Enqueue( targetName );
			string nextTargetName = "";

			do
			{
				nextTargetName = (string) nextTargets.Dequeue();

				if( nextTargetName == null || nextTargetName == string.Empty)
					throw (new ArgumentNullException());

				// find this targetNode (assume there is only one such target node)
				targetNode = _nantProjects.SelectSingleNode(FieldWorksSettings.FormatXpathForFieldWorksNameSpace("//{0}:target[@name='" + nextTargetName + "']"),
					this._nantFieldWorksNamespaceManager);

				if (targetNode == null)
					throw (new ArgumentNullException());

				// Get Dependencies for this targetNode
				string depends = string.Empty;
				XmlNode targetAttrib = targetNode.Attributes.GetNamedItem( "depends" );

				if(targetAttrib != null)
				{
					depends = targetAttrib.Value;
					// Add dependencies to dependency stack
					foreach (string dependency in this.SplitDependencyString( depends ) )
					{
						// Skip empty dependencies
						if( dependency == null || dependency == string.Empty)
							continue;
						// Process only the dependencies that we haven't already found
						if ( fullDependsList.Contains( dependency ) == false)
						{
							nextTargets.Enqueue( dependency );
							fullDependsList.Add( dependency );
						}
					}
				}

			} while (nextTargets.Count > 0);

			return fullDependsList;
		}

		private ArrayList GetDirectDependents( string target )
		{
			ArrayList directDependents = new ArrayList();

			if (target == null || target == string.Empty)
			{
				return directDependents;
			}

			// find targetNodes containing the target in the depends attribute
			XmlNodeList targetNodes = _nantProjects.SelectNodes(FieldWorksSettings.FormatXpathForFieldWorksNameSpace("//{0}:target[ contains(@depends,'" + target + "') ]"),
				this._nantFieldWorksNamespaceManager);

			if ( targetNodes == null || targetNodes.Count == 0)
			{
				return directDependents;
			}

			foreach (XmlNode node in targetNodes)
			{
				string targetName = node.Attributes.GetNamedItem( "name" ).Value;
				string depends = node.Attributes.GetNamedItem( "depends" ).Value;

				ArrayList dependencies = new ArrayList();
				dependencies.AddRange( 	this.SplitDependencyString( depends ) );

				if ( dependencies.Contains ( target ) )
				{
					directDependents.Add(targetName);
				}

			}

			return directDependents;

		}

		// Utility function to get attribute values for a node associated with a target
		private ArrayList GetAttributeValues(string xpathSelectNodes, string attributeName)
		{
			ArrayList attributeValues = new ArrayList();

			// find selected nodes
			XmlNodeList nodes = _nantProjects.SelectNodes(xpathSelectNodes, this._nantFieldWorksNamespaceManager);

			if ( nodes == null || nodes.Count == 0)
			{
				return attributeValues;
			}

			foreach (XmlNode node in nodes)
			{
				string attributeValue = node.Attributes.GetNamedItem( attributeName ).Value;
				attributeValues.Add( attributeValue );
			}

			return attributeValues;
		}

		/// <summary>
		/// GetDirectCalls
		/// </summary>
		/// <param name="target"></param>
		/// <returns>ArrayList: Returns the targets that (can be) directly called by the (input) target. </returns>
		private ArrayList GetDirectCalls( string target )
		{
			//string xpathSelectCallNodes = "//target[@name='"+ target +"']/call";
			//string xpathSelectCallNodes = "//call[(ancestor::target[@name='"+ target +"']]";
			string xpathSelectCallNodes = FieldWorksSettings.FormatXpathForFieldWorksNameSpace("//{0}:target[@name='"+ target +"']//{0}:call");
			string attributeName = "target";
			ArrayList directTargetCalls = this.GetAttributeValues(xpathSelectCallNodes, attributeName);

			return directTargetCalls;
		}

		/// <summary>
		/// GetDirectCallers
		/// </summary>
		/// <param name="target"></param>
		/// <returns>ArrayList: Returns the targets that (can) directly call the (input) target. </returns>
		private ArrayList GetDirectCallers( string target )
		{
			string xpathSelectParents = FieldWorksSettings.FormatXpathForFieldWorksNameSpace("//{0}:target[descendant::{0}:call [@target='" + target + "']]");
			string attributeName = "name";
			ArrayList directCallers = this.GetAttributeValues(xpathSelectParents, attributeName);

			return directCallers;
		}

		private string GetDirectDependentsString ( string target )
		{
			string directDependentStr = "Direct Dependents upon target( " + target +" ):\r\n";
			ArrayList directDependents = this.GetDirectDependents( target );

			if (directDependents == null || directDependents.Count == 0)
			{
				directDependentStr += "\t(none)\r\n";
				return directDependentStr;
			}

			directDependents.Sort();
			foreach (string dependent in directDependents)
			{
				directDependentStr += "\t" + dependent + "\r\n";
			}
			return directDependentStr;
		}

		private string GetDirectDependenciesString( string depends )
		{
			string directDependenciesString = "Direct dependencies:\r\n";

			ArrayList directDependencies = new ArrayList();
			directDependencies.AddRange( this.SplitDependencyString( depends ));

			if( depends == null || depends == string.Empty || directDependencies.Count == 0)
			{
				directDependenciesString += "\t(none)\r\n";
				return directDependenciesString;
			}
			directDependencies.Sort();

			foreach( string dependency in directDependencies )
			{
				directDependenciesString += "\t" + dependency;
				directDependenciesString += "\r\n";
			}

			return directDependenciesString;
		}

		private string GetFullDependentsString(string target)
		{
			string fullDependentsString = "";

			StringArrayList fullListOfDependents = new StringArrayList();
			StringArrayList ancestorDependents = new StringArrayList();
			ArrayList directDependents = new ArrayList();
			directDependents.AddRange( this.GetDirectDependents( target ) );

			fullListOfDependents.AddRange( this.GetFullDependentList( target ) );
			fullDependentsString += "Total Dependents (Direct & Ancestor):\r\n";

			if (fullListOfDependents.Count > 0)
			{
				fullListOfDependents.Sort();
				int maxStrLength = fullListOfDependents.MaxStringLength();
				int columnLength = maxStrLength + 5;
				foreach( string dependent in fullListOfDependents )
				{
					String formattedStr = dependent;
					if( directDependents.Contains( dependent ))
					{
						string strFormat = "{0,-" + columnLength + "}{1}";
						formattedStr = String.Format(strFormat,	formattedStr, "(direct)");
					}
					else
					{
						ancestorDependents.Add( dependent );
					}
					fullDependentsString += "\t" + formattedStr;
					fullDependentsString += "\r\n";
				}
			}
			else
			{
				fullDependentsString += "\t(none)\r\n";
			}
			return fullDependentsString;
		}

		private string GetFullDependenciesString( string target, string depends )
		{
			string fullDependenciesString = "";

			StringArrayList fullListOfDependencies = new StringArrayList();
			StringArrayList derivedDependencies = new StringArrayList();
			ArrayList directDependencies = new ArrayList();
			directDependencies.AddRange( this.SplitDependencyString( depends ));

			fullListOfDependencies.AddRange(this.GetFullDependencyList( target ));
			fullDependenciesString += "Total Dependencies (Derived + Direct):\r\n";

			if (fullListOfDependencies.Count > 0)
			{
				fullListOfDependencies.Sort();
				int maxStrLength = fullListOfDependencies.MaxStringLength();
				int columnLength = maxStrLength + 5;
				foreach( string dependency in fullListOfDependencies )
				{
					String formattedStr = dependency;
					if( directDependencies.Contains( dependency ))
					{
						string strFormat = "{0,-" + columnLength + "}{1}";
						formattedStr = String.Format(strFormat,	formattedStr, "(direct)");
					}
					else
					{
						derivedDependencies.Add( dependency );
					}
					fullDependenciesString += "\t" + formattedStr;
					fullDependenciesString += "\r\n";
				}
			}
			else
			{
				fullDependenciesString += "\t(none)\r\n";
			}
			return fullDependenciesString;
		}

		private void listView1_SelectedItem_UpdateTargetInfoBox()
		{
			// Confirm user has selected only one target
			if( this.listView1.SelectedItems.Count > 1 )
				throw (new ArgumentOutOfRangeException());

			const int dependsIndex = (int) NantProjectForm1.ControlTextDefaults.listView1_Config.ColumnOrder.Dependencies;
			const int bldfileIndex = (int) NantProjectForm1.ControlTextDefaults.listView1_Config.ColumnOrder.Buildfile;

			// Save the selected Nant target

			ListView.SelectedListViewItemCollection selectedTargets  = this.listView1.SelectedItems;

			string targetInfoTextBoxString = "";

			//REVISIT: currently we only should support one selected target.
			foreach ( ListViewItem item in selectedTargets)
			{
				// targetInfoTextBoxString += "---( " + item.SubItems[projIndex].Text + " / " + item.Text + " )--";

				// Update the text in this.targetInfoTextBox.
				//if (item.SubItems[descIndex].Text != "")
				//{
				//	targetInfoTextBoxString += "\r\n[ " + item.SubItems[descIndex].Text + " ]";
				//}
				// targetInfoTextBoxString += "\r\n";

				ListViewItem selectedTarget = this.listView1.SelectedItems[0];

				// get direct depenDENCIES
				targetInfoTextBoxString += this.GetDirectDependenciesString( item.SubItems[dependsIndex].Text );

				if ( item.SubItems[dependsIndex].Text != "" )
				{
					// get full dependencies
					//targetInfoTextBoxString += this.GetFullDependenciesString( item.Text,
					//	item.SubItems[dependsIndex].Text );
				}

				// get direct depentDENTS
				targetInfoTextBoxString += this.GetDirectDependentsString( item.Text );

				// get full depentDENTS
				// targetInfoTextBoxString += this.GetFullDependentsString( item.Text );

				targetInfoTextBoxString += "\r\n";
				targetInfoTextBoxString += "[Buildfile: " + item.SubItems[bldfileIndex].Text + " ]\r\n";
			}
			this.targetInfoTextBox.Text = targetInfoTextBoxString;
		}

		private void resetCommandLineText()
		{
			this.commandLineBox.Text = NantProjectForm1.ControlTextDefaults.commandLineBox; // default command
			this.copyCmdLineButton.Enabled = true;
		}

		private void UpdateCommandLineBox()
		{
			string commandLineStr = "";

			//at least reset ComboBox Line to default
			this.resetCommandLineText();
			commandLineStr = this.commandLineBox.Text;

			const string targetNotSelected = "(select target)";
			string configuration = "";
			string action = "";
			string flags = "";
			string target = "";

			if (this.configurationComboBox.SelectedItem != null)
			{
				configuration = this.configurationComboBox.SelectedItem.ToString();
				commandLineStr += " " + configuration;
			}

			if (this.actionComboBox.SelectedItem != null)
			{
				action = this.actionComboBox.SelectedItem.ToString();
				commandLineStr += " " + action;
			}

			foreach (object checkedItem in this.buildFlagsChecklistBox.CheckedItems)
			{
				flags += checkedItem.ToString() + " ";
				commandLineStr += " " + checkedItem.ToString();
			}

			if (this.listView1.SelectedItems.Count != 0)
			{
				target = this.listView1.SelectedItems[0].Text;
				commandLineStr += " " + target;
				this.copyCmdLineButton.Enabled = true;
			}
			else
			{
				// leave an indication that the user should select a target, if configuration or action is selected.
				if (configuration != string.Empty || action != string.Empty || flags != string.Empty)
				{
					target = targetNotSelected;
					commandLineStr += " " + target;
					this.copyCmdLineButton.Enabled = false;
				}
			}

			this.commandLineBox.Text = commandLineStr;
		}

		private void listView1_SelectedItem_UpdateNantCommandline()
		{
			this.UpdateCommandLineBox();
		}

		private void listView1_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			const int projIndex = (int) NantProjectForm1.ControlTextDefaults.listView1_Config.ColumnOrder.Project;
			const int descIndex = (int) NantProjectForm1.ControlTextDefaults.listView1_Config.ColumnOrder.Description;

			this.listView1_SelectedItem_UpdateNantCommandline();

			// update target name TextBox & project name & description
			if( this.listView1.SelectedItems.Count != 0)
			{
				this.selectedTargetNameTextBox.Text = this.listView1.SelectedItems[0].Text;
				this.targetProjectTextBox.Text = this.listView1.SelectedItems[0].SubItems[projIndex].Text;
				this.selectedTargetDescriptionTextBox.Text = this.listView1.SelectedItems[0].SubItems[descIndex].Text;
			}

			// Load TabControl Page
			this.tabControl1_LoadCurrentPage();
		}

		// ColumnClick event handler.
		private void ColumnClick(object o, ColumnClickEventArgs e)
		{
			//ASSERT that ListViewItemSorter is set to ListViewItemComparer;
			if (this.listView1.ListViewItemSorter.ToString().EndsWith("+ListViewItemComparer") == false)
				throw (new ArgumentException(this.listView1.ListViewItemSorter.ToString()));

			// Set the ListViewItemSorter property to a new ListViewItemComparer
			// object. Setting this property immediately sorts the
			// ListView using the ListViewItemComparer object.
			if (e.Column != ((ListViewItemComparer)this.listView1.ListViewItemSorter).PrevColumnClicked() )
			{
				this.listView1.ListViewItemSorter = new ListViewItemComparer(e.Column);
			}
			else
			{
				((ListViewItemComparer)this.listView1.ListViewItemSorter).ReverseSortDirection();
				this.listView1.Sort();
			}
			if (this.listView1.SelectedItems.Count > 0)
			{
				this.listView1.SelectedItems[0].EnsureVisible();
			}
		}

		// Implements the manual sorting of items by columns.
		class ListViewItemComparer : IComparer
		{
			private int col;
			private bool reverse;
			public ListViewItemComparer()
			{
				col = 0;
				reverse = false;
			}
			public ListViewItemComparer(int column)
			{
				col = column;
				reverse = false;
			}
			public int Compare(object x, object y)
			{
				int comparisonValue;

				comparisonValue = String.Compare(((ListViewItem)x).SubItems[col].Text,
												 ((ListViewItem)y).SubItems[col].Text);
				if (reverse == true)
				{
					comparisonValue = (comparisonValue * -1);
				}
				return comparisonValue;
			}
			public int PrevColumnClicked()
			{
				return col;
			}
			public void ReverseSortDirection()
			{
				this.reverse = !(this.reverse);
			}
		}

		private void FillCommandLineComboBox( System.Windows.Forms.ComboBox comboBox, string xpath )
		{
			if( xpath == null || xpath == string.Empty)
				throw (new ArgumentNullException());

			XmlNodeList nodes = this._nantProjects.SelectNodes(xpath, this._nantFieldWorksNamespaceManager);

			foreach (XmlNode node in nodes)
			{
				string itemName = node.Attributes.GetNamedItem("name").Value;
				comboBox.Items.Add( itemName );
			}
			comboBox.Sorted = true;
		}

		private void FillCommandLineFlagsListBox( System.Windows.Forms.CheckedListBox checkedBox, string xpath)
		{
			if( xpath == null || xpath == string.Empty)
				throw (new ArgumentNullException());

			XmlNodeList nodes = this._nantProjects.SelectNodes(xpath, this._nantFieldWorksNamespaceManager);

			foreach (XmlNode node in nodes)
			{
				string itemName = node.Attributes.GetNamedItem("name").Value;
				checkedBox.Items.Add( itemName );
			}
			checkedBox.Sorted = false;
		}

		private void LoadCommandLineOptions ()
		{
			string configurationsXPath = FieldWorksSettings.FormatXpathForFieldWorksNameSpace("/{0}:target[{0}:property[@name='build-type']]");
			string actionsXPath = FieldWorksSettings.FormatXpathForFieldWorksNameSpace("/{0}:target[{0}:property[@name='build-action']]");
			//REVISIT we probably shouldn't remove flags just because they have 'depends' attribute
			//probably should see whether depends upon configuration or action nodes(?).
			string flagsXPath = FieldWorksSettings.FormatXpathForFieldWorksNameSpace("/{0}:target[not( {0}:property[@name='build-action'])"
									   + " and not( {0}:property[@name='build-type'])"
									   + " and not( @depends )"
									   + " and not( descendant::{0}:call )]");
			// "/target[not( property[@name='build-action']) and not(property[@name='build-type']) ]"

			this.FillCommandLineComboBox( this.configurationComboBox,
										  FieldWorksSettings.SpecialTargets.ProjectXPath
										  + configurationsXPath );

			this.FillCommandLineComboBox( this.actionComboBox,
										  FieldWorksSettings.SpecialTargets.ProjectXPath
										  + actionsXPath );

			// fill Flags Checkbox
			this.FillCommandLineFlagsListBox( this.buildFlagsChecklistBox,
											  FieldWorksSettings.SpecialTargets.ProjectXPath
											  + flagsXPath );

		}

		private void callsListView_LoadItems(ArrayList targetList, ArrayList relationsList, bool fInOrder)
		{

			if (targetList == null || relationsList == null)
				throw (new ArgumentNullException("targetList or relationsList is NULL"));

			if (targetList.Count != relationsList.Count)
				throw (new ArgumentException("targetList.Count != relationsList.Count"));

			//Clear the items for callsListView
			this.callsListView.Items.Clear();

			// ASSERT private arguments are not null
			if (this._nantProjects == null || this.callsListView == null)
				throw (new ArgumentNullException());

			// ASSERT that we have setup colum headers, but no items.
			if (this.callsListView.Columns.Count == 0 || this.callsListView.Items.Count > 0)
				throw (new ArgumentException("this.callsListView.Columns.Count == 0 || this.listView1.Items.Count > 0"));

			if (this.listView1.SelectedItems.Count == 0)
				throw (new ArgumentNullException("this.listView1.SelectedItems.Count == 0"));

			callsListView.BeginUpdate();

			int index = 0;
			foreach (string itemStr in targetList)
			{
				ListViewItem item = new ListViewItem( itemStr );

				//Get Build File associated with this Target
				item.SubItems.Add( relationsList[ index ].ToString() );

				if ( fInOrder )
				{
					item.SubItems.Add( index.ToString() );
				}
				else
				{
					item.SubItems.Add("");
				}

				callsListView.Items.Add(item);
				index++;
			}

			callsListView.EndUpdate();
		}

		private void panel1_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
		{

		}

		private void textBox1_TextChanged(object sender, System.EventArgs e)
		{

		}

		private void NantProjectForm1_Load(object sender, System.EventArgs e)
		{

		}

		private void splitter1_SplitterMoved(object sender, System.Windows.Forms.SplitterEventArgs e)
		{

		}

		private void comboBox1_SelectedIndexChanged(object sender, System.EventArgs e)
		{

		}

		private void label1_Click(object sender, System.EventArgs e)
		{

		}

		private void commandLineBox_TextChanged(object sender, System.EventArgs e)
		{

		}

		private void UpdateCmdLineInfoBox( string target )
		{
			// find selected target
			string xpath = FieldWorksSettings.FormatXpathForFieldWorksNameSpace(FieldWorksSettings.SpecialTargets.ProjectXPath + "/{0}:target[@name='" + target + "']");
			XmlNode selectedNode = this._nantProjects.SelectSingleNode( xpath, this._nantFieldWorksNamespaceManager );
			string targetDescription = "";

			if (selectedNode != null)
			{
				XmlNode targetAttrib = selectedNode.Attributes.GetNamedItem("description");

				if ( targetAttrib != null )
				{
					targetDescription = targetAttrib.Value;
				}
			}

			this.buildOptionInfoBox.Text = "---( " + target + " )---\r\n" + targetDescription;
		}

		private void configurationComboBox_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			if( this.configurationComboBox.SelectedItem != null)
			{
				this.UpdateCmdLineInfoBox( this.configurationComboBox.SelectedItem.ToString() );
				this.UpdateCommandLineBox();
			}
		}

		private void actionComboBox_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			if ( this.actionComboBox.SelectedItem != null)
			{
				this.UpdateCmdLineInfoBox( this.actionComboBox.SelectedItem.ToString() );
				this.UpdateCommandLineBox();
			}
		}

		private void resetCmdLineOptions()
		{
			this.configurationComboBox.SelectedItem = null;
			this.configurationComboBox.Text = "(configuration)";
			this.actionComboBox.SelectedItem = null;
			this.actionComboBox.Text = "(action)";
			this.buildFlagsChecklistBox.SelectedItem = null;
			// uncheck all checked boxes
			foreach (int index in this.buildFlagsChecklistBox.CheckedIndices)
			{
				this.buildFlagsChecklistBox.SetItemCheckState(index, CheckState.Unchecked);
			}
			this.buildOptionInfoBox.Text = "(select build option for description)";
		}

		private void resetCmdLineButton_Click(object sender, System.EventArgs e)
		{
			this.resetCmdLineOptions();
			this.resetCommandLineText();
		}

		private void copyCmdLineButton_Click(object sender, System.EventArgs e)
		{
			if ( this.commandLineBox.Text != string.Empty)
			{
				System.Windows.Forms.Clipboard.SetDataObject( this.commandLineBox.Text );
				// Add to commandLineBox history
				this.commandLineBoxUpdateHistory( this.commandLineBox.Text );
			}

		}

		private void commandLineBoxUpdateHistory( string commandlineText )
		{
			if (commandlineText == string.Empty)
				return;

			int foundItemIndex = this.commandLineBox.FindStringExact( commandlineText );
			if( foundItemIndex == 0)
			{
				return;
			}

			// if commandLineBox has history of this Text, then move it to the top
			if( foundItemIndex != -1)
			{
				// remove from list so we can add it back at the top
				this.commandLineBox.Items.RemoveAt( foundItemIndex );
			}

			this.commandLineBox.Items.Insert(0, commandlineText );

			// Reset SelectedIndex to 0, but first disable event handler for SelectedIndex
			// REVISIT: disabling event handlers appears to be a hacked way of doing this (?)
			this.commandLineBox.SelectedIndexChanged -= new System.EventHandler(this.commandLineBox_SelectedIndexChanged);
			this.commandLineBox.SelectedIndex = 0;
			this.commandLineBox.SelectedIndexChanged += new System.EventHandler(this.commandLineBox_SelectedIndexChanged);
		}

		private void buildFlagsChecklistBox_ItemCheck(object sender, System.Windows.Forms.ItemCheckEventArgs e)
		{
			// Force check box to new value
			if (e.NewValue != e.CurrentValue)
			{
				// first disable this event handler (else we get Stack Overflow).
				// REVISIT: is there a better way of making sure that all checked items
				// get written to the commandLineBox?  This seems like a hack.
				this.buildFlagsChecklistBox.ItemCheck -= new System.Windows.Forms.ItemCheckEventHandler(this.buildFlagsChecklistBox_ItemCheck);

				// now change state
				this.buildFlagsChecklistBox.SetItemCheckState(e.Index, e.NewValue);

				// now renable event handler
				this.buildFlagsChecklistBox.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.buildFlagsChecklistBox_ItemCheck);

				// Finally, update other controls
				this.UpdateCommandLineBox();
			}
		}

		private void buildFlagsChecklistBox_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			if ( this.buildFlagsChecklistBox.SelectedItem != null)
			{
				this.UpdateCmdLineInfoBox( this.buildFlagsChecklistBox.SelectedItem.ToString() );
			}
		}

		private void tabControl1_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			if(this.tabControl1.SelectedTab == this.callTargetTabPage
				|| this.tabControl1.SelectedTab == this.callerTargetsTabPage)
			{
				this.tabControl1.SelectedTab.Controls.Add( this.callsListView );
			}
			else
			{
				this.tabControl1.SelectedTab.Controls.Add( this.targetInfoTextBox );
			}

			this.tabControl1_LoadCurrentPage();
		}
		/// <summary>
		///
		/// </summary>
		/// <param name="baseTarget"></param>
		/// <param name="callList"></param>
		/// <returns></returns>
		ArrayList getCallRelationshipList(string baseTarget, ArrayList callList)
		{
			ArrayList relationsList = new ArrayList();

			if (baseTarget == string.Empty)
				throw (new ArgumentNullException("baseTarget is string.Empty"));

			if (callList == null)
				throw (new ArgumentNullException("callList is null"));

			//REVISIT(EricP): currently only returns a blanklist of callList.Count();
			// eventually create list or hashtable that identifies call as "conditional"
			// or "unconditional".
			relationsList.AddRange( callList );

			for (int i = 0; i < callList.Count; i++)
			{
				relationsList[i] = "";
			}

			return relationsList;
		}

		ArrayList getCallerRelationshipList(string baseTarget, ArrayList callerList)
		{
			//REVISIT(EricP): this will return a blank list the size of callerList
			return (this.getCallRelationshipList(baseTarget, callerList));
		}

		private void tabControl1_LoadCurrentPage()
		{
			if (this.listView1.SelectedItems.Count == 0)
				return;

			// REVISIT:  We should let each page do their own filling of this information
			// in an object oriented manner.
			if(this.tabControl1.SelectedTab == this.basicInfoTabPage)
			{
				this.basicInfoTabPage_LoadPage();
			}
			else if (this.tabControl1.SelectedTab == this.dependencyTabPage)
			{
				this.dependencyTabPage_LoadPage();
			}
			else if (this.tabControl1.SelectedTab == this.dependentsTabPage)
			{
				this.dependentsTabPage_LoadPage();
			}
			else if (this.tabControl1.SelectedTab == this.callTargetTabPage)
			{
				// next get all (direct) target calls for target
				ArrayList directCalls = this.GetDirectCalls(this.listView1.SelectedItems[0].Text);
				ArrayList relations = this.getCallRelationshipList(this.listView1.SelectedItems[0].Text, directCalls);
				this.callsListView_LoadItems(directCalls, relations, true);
			}
			else if (this.tabControl1.SelectedTab == this.callerTargetsTabPage)
			{
				// next get all (direct) target callers for target
				ArrayList directCallers = this.GetDirectCallers(this.listView1.SelectedItems[0].Text);
				ArrayList relations = this.getCallerRelationshipList(this.listView1.SelectedItems[0].Text, directCallers);
				this.callsListView_LoadItems(directCallers, relations, false);
			}
		}

		private void basicInfoTabPage_LoadPage()
		{

			this.listView1_SelectedItem_UpdateTargetInfoBox();

		}

		private void dependencyTabPage_LoadPage()
		{
			if ( this.listView1.SelectedItems.Count == 0 )
				return;

			const int dependsIndex = (int) NantProjectForm1.ControlTextDefaults.listView1_Config.ColumnOrder.Dependencies;

			string target = this.listView1.SelectedItems[0].Text;
			string depends = this.listView1.SelectedItems[0].SubItems[dependsIndex].Text;

			// get full dependencies
			this.targetInfoTextBox.Text = this.GetFullDependenciesString( target, depends );

		}

		private void dependentsTabPage_LoadPage()
		{
			if ( this.listView1.SelectedItems.Count == 0 )
				return;

			string target = this.listView1.SelectedItems[0].Text;

			// get full dependents
			this.targetInfoTextBox.Text = this.GetFullDependentsString( target );
		}

		private void commandLineBox_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			// Reset commandline options
			this.resetCmdLineOptions();
		}

		private void findListViewItemInputTextBox_TextChanged(object sender, System.EventArgs e)
		{
			string lookupString = this.findListViewItemInputTextBox.Text;
			ListViewItem itemMatched = null;

			//trim lookup string
			lookupString = lookupString.Trim();

			// REVISIT: should we try to optimize this search?
			// Find (closest) entry in Items
			foreach (ListViewItem item in this.listView1.Items)
			{
				if ( item.Text == lookupString )
				{
					itemMatched = item;
					break;
				}
				else if ( item.Text.StartsWith( lookupString )  || item.Text.ToLower().StartsWith( lookupString.ToLower() ) )
				{
					if ( itemMatched == null )
					{
						itemMatched = item;
					}
					// See if this item is higher in the alphabet
					// REVISIT: Should this comparison be configurable?
					else if ( item.Text.CompareTo( itemMatched.Text ) < 0 )
					{
						itemMatched = item;
					}
				}
			}

			if (itemMatched != null)
			{
				itemMatched.Selected = true;
				itemMatched.EnsureVisible();
			}
		}

		private void listView1GoToItemResetButton_Click(object sender, System.EventArgs e)
		{
			this.findListViewItemInputTextBox.Text = "";
		}

		private void callsListView_SelectedIndexChanged(object sender, System.EventArgs e)
		{

		}

	}

	public class FieldWorksSettings
	{
		public const string FieldWorksRegistryKey = @"Software\SIL\FieldWorks"; // HKLM\Software\SIL\FieldWorks   RootCodeDir
		public const string DistributionDirectoryRegistryVariable = "RootCodeDir";
		public const string BuildDirectory = @"Bld";
		public const string FieldWorksNamespace = "http://fieldworks.sil.org/nant/fwnant.xsd"; // xmlns=\"http://fieldworks.sil.org/nant/fwnant.xsd"
		public const string FieldWorksNamespacePrefix = "fw";
		public class SpecialTargets
		{
			public const string ProjectName = "Targets";
			public const string ProjectXPath = "/*/" + FieldWorksNamespacePrefix + ":project[@name='" + SpecialTargets.ProjectName + "']"; // root for Special Targets
		}

		/// <summary>
		/// replace instances of {0} in an xpath with FieldWorksNamespacePrefix
		/// </summary>
		/// <param name="xpath"></param>
		/// <returns>formatted string</returns>
		public static string FormatXpathForFieldWorksNameSpace(string xpath)
		{
			return String.Format(xpath, FieldWorksSettings.FieldWorksNamespacePrefix);
		}
	}

	class Run
	{
		[STAThread]
		static void Main(string[] args)
		{
			// Load Nant Projects
			string distDir = null;
			RegistryKey key = Registry.LocalMachine.OpenSubKey(FieldWorksSettings.FieldWorksRegistryKey);
			if (key != null)
			{
				distDir = key.GetValue(FieldWorksSettings.DistributionDirectoryRegistryVariable) as string;
				key.Close();
			}
			else
			{
				MessageBox.Show("FieldWorks key (" + FieldWorksSettings.FieldWorksRegistryKey +
					") not found in system registry.", "NantFarm");
				return;
			}
			if (distDir == null)
			{
				MessageBox.Show("Registry (" + key.Name + ") does not contain value for root directory variable (" +
					FieldWorksSettings.DistributionDirectoryRegistryVariable + ")", "NantFarm");
				return;
			}

			// after finding the distfiles directory, we need to go up a level to find the base directory.
			distDir = distDir.Trim();
			if (distDir.EndsWith(@"\"))
			{
				distDir = distDir.TrimEnd(new char[] {'\\'});
			}
			string bldDirPath = distDir.Substring(0, distDir.LastIndexOf(@"\") + 1) + FieldWorksSettings.BuildDirectory;
			DirectoryInfo bldDirectory = new DirectoryInfo( bldDirPath );

			ArrayList _nantProjects = new ArrayList();
			ArrayList bldFiles = new ArrayList();

			bldFiles.AddRange( bldDirectory.GetFiles( "*.build" ) );
			FileInfo[] xmlFiles = bldDirectory.GetFiles("*.xml");
			// exclude backup files.
			ArrayList nonBackupXmlFiles = new ArrayList(xmlFiles.Length);
			foreach (FileInfo fi in xmlFiles)
			{
				// only allow files that have a period followed by 3 characters (eg. ".xml")
				if (fi.Extension.Length == 4)
					nonBackupXmlFiles.Add(fi);
			}
			bldFiles.AddRange(nonBackupXmlFiles);

			// Create XmlDocument by merging all project xmls
			XmlDocument nantMergedProjects = new XmlDocument();
			XmlNode container = nantMergedProjects.CreateElement("ProjectCollection");
			nantMergedProjects.AppendChild(container);

			foreach (FileInfo file in bldFiles)
			{
				XmlDocument doc = new XmlDocument();
				doc.PreserveWhitespace = false;
				doc.Load(file.FullName);

				// add file.FullName to project node attribute
				// Add a new attribute to the collection.
				XmlAttributeCollection attrColl = doc.DocumentElement.Attributes;

				XmlAttribute attr = doc.CreateAttribute("file");
				attr.Value = file.FullName;
				attrColl.SetNamedItem(attr);

				//Import the project node from into the master list document.
				XmlNode imported = nantMergedProjects.ImportNode(doc.DocumentElement, true);
				// push project into tree
				nantMergedProjects.DocumentElement.AppendChild(imported);

			}

			//Console.WriteLine(nantMergedProjects.InnerXml);
			//Application.Run(new NantProjForm(nantMergedProjects));
			Application.Run(new NantTargetProject.NantProjectForm1(nantMergedProjects));
		}
	}
	public class StringArrayList : ArrayList
	{
		public int MaxStringLength()
		{
			int maxlength = 0;
			foreach (string str in this)
			{
				if (str.Length > maxlength)
					maxlength = str.Length;
			}
			return maxlength;
		}
	}
}

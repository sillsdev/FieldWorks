using System;
using System.Drawing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.Diagnostics;
using Microsoft.Win32;                  // for RegistryKey
using System.IO;                        // for FileInfo
using System.Resources;                 // for ResourceManager
using ECInterfaces;                     // for IEncConverter
using System.Text;                      // for Encoding

namespace SilEncConverters40
{
	/// <summary>
	/// Checked list box of IEncConverter objects in the repository for the user to choose from
	/// </summary>
	public class SelectConverter : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Label labelInstruction;
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.Button buttonOK;
		private System.ComponentModel.IContainer components;

		protected string        m_strConverterName = null;
		private System.Windows.Forms.ToolTip toolTips;
		protected ConvType      m_eConversionTypeFilter = ConvType.Unknown;
		private System.Windows.Forms.GroupBox groupBoxOptions;
		private System.Windows.Forms.CheckBox checkBoxReverse;
		private System.Windows.Forms.RadioButton radioButtonNone;
		private System.Windows.Forms.RadioButton radioButtonFullyComposed;
		private System.Windows.Forms.RadioButton radioButtonFullyDecomposed;
		private System.Windows.Forms.Label labelNormalizationType;
		private System.Windows.Forms.CheckBox checkBoxDebug;
		private System.Windows.Forms.Button buttonLaunchOptionsInstaller;
		protected Dictionary<string, string>    m_mapLbItems2Tooltips = new Dictionary<string,string>();

		static private string strInstallerLocationRegKey    = @"SOFTWARE\SIL\SilEncConverters40\Installer";
		static private string strInstallerPathKey           = "InstallerPath";
		private System.Windows.Forms.Button buttonCreateNew;
		private System.Windows.Forms.ContextMenu contextMenu;
		private System.Windows.Forms.MenuItem menuItemEdit;
		private System.Windows.Forms.MenuItem menuItemDelete;
		private System.Windows.Forms.ListBox listBoxExistingConverters;
		private TableLayoutPanel tableLayoutPanel1;
		private CheckBox checkBoxShowTooltips;
		private HelpProvider helpProvider;
		private Label labelCodePageInput;
		private TextBox textBoxCodePageInput;
		private TextBox textBoxCodePageOutput;
		private Label labelCodePageOutput;
		private FlowLayoutPanel flowLayoutPanelCodePage;
		private Button buttonPreview;
		private TextBox textBoxDataPreview;

		protected byte[] m_byPreviewData = null;
		private MenuItem menuItemTest;
		protected internal ContextMenuStrip contextMenuStripPreview;
		private ToolStripMenuItem changeFontToolStripMenuItem;
		private ToolStripSeparator toolStripSeparator1;
		private ToolStripMenuItem copyToolStripMenuItem;
		private ToolStripSeparator toolStripSeparator2;
		private ToolStripMenuItem selectAllToolStripMenuItem;
		private ToolStripSeparator toolStripSeparator3;
		private ToolStripMenuItem right2LeftToolStripMenuItem;
		protected internal FontDialog fontDialog;
		protected string m_strPreviewData = null;
		private Timer timerTooltip;
		protected string m_strFontName = null;

		public SelectConverter(EncConverters aECs, ConvType eConversionTypeFilter,
			string strChooseConverterDialogTitle, byte[] abyPreviewData, string strFontName)
		{
			m_byPreviewData = abyPreviewData;

			InitSelectConverter(aECs, eConversionTypeFilter, strChooseConverterDialogTitle, strFontName);

			// hide the preview box until requested
			textBoxDataPreview.Hide();
			tableLayoutPanel1.RowCount = 4;
		}

		public SelectConverter(EncConverters aECs, ConvType eConversionTypeFilter,
			string strChooseConverterDialogTitle, string strPreviewData, string strFontName)
		{
			m_strPreviewData = strPreviewData;

			InitSelectConverter(aECs, eConversionTypeFilter, strChooseConverterDialogTitle, strFontName);

			if (String.IsNullOrEmpty(strPreviewData))
				buttonPreview.Visible = false;

			// hide the preview box until requested
			textBoxDataPreview.Hide();
			tableLayoutPanel1.RowCount = 4;
		}

		public void InitSelectConverter(EncConverters aECs, ConvType eConversionTypeFilter,
			string strChooseConverterDialogTitle, string strFontName)
		{
			DirectableEncConverter.EncConverters = aECs;
			m_eConversionTypeFilter = eConversionTypeFilter;

			InitializeComponent();

			// update the title bar with any potential text strings given by the client
			if (!String.IsNullOrEmpty(strChooseConverterDialogTitle))
				Text = strChooseConverterDialogTitle;

			if (!String.IsNullOrEmpty(strFontName))
				textBoxDataPreview.Font = CreateFontSafe(strFontName);

			// and indicate whether it is being filtered or not.
			if (m_eConversionTypeFilter != ConvType.Unknown)
				Text += String.Format(" <with filter: {0}>", m_eConversionTypeFilter.ToString());

			// Force the ToolTip text to be displayed whether or not the form is active.
			toolTips.ShowAlways = true;

			// enable the "Launch Options Installer" button (if the NRSI setup program is installed...
			//  it's not visible otherwise)
			RegistryKey keyInstallLocation = Registry.LocalMachine.OpenSubKey(strInstallerLocationRegKey, false);
			if( keyInstallLocation != null )
			{
				string strInstallPath = (string)keyInstallLocation.GetValue(strInstallerPathKey);
				buttonLaunchOptionsInstaller.Visible = (!String.IsNullOrEmpty(strInstallPath) && File.Exists(strInstallPath));
			}

			InitializeConverterList();

			this.listBoxExistingConverters.ContextMenu = this.contextMenu;

			RegistryKey keyLastTooltipState = Registry.LocalMachine.OpenSubKey(EncConverters.SEC_ROOT_KEY, false);
			if (keyLastTooltipState != null)
				checkBoxShowTooltips.Checked = (bool)((string)keyLastTooltipState.GetValue(EncConverters.strShowToolTipsStateKey, "True") == "True");

			helpProvider.SetHelpString(textBoxCodePageInput, Properties.Resources.CodePageHelpString);
			helpProvider.SetHelpString(textBoxCodePageOutput, Properties.Resources.CodePageHelpString);
			helpProvider.SetHelpString(textBoxDataPreview, Properties.Resources.PreviewBoxHelpString);
		}

		// the creation of a Font can throw an exception if, for example, you try to construct one with
		//  the default style 'Regular' when the font itself doesn't have a Regular style. So this method
		//  can be called to create one and it'll try different styles if it fails.
		protected int cnDefaultFontSize = 14;
		protected Font CreateFontSafe(string strFontName)
		{
			Font font = null;
			try
			{
				font = new Font(strFontName, cnDefaultFontSize);
			}
			catch
			{
				font = textBoxDataPreview.Font;
			}
			return font;
		}

		void InitializeConverterList()
		{
			// clear previous contents (if any)
			listBoxExistingConverters.Items.Clear();

			// put the names of the existing converters into the checked list box (unchecked)
			//  and filter them if the user is requesting only certain kinds (e.g. if they
			//  ask for Unicode(<)>Unicode, then only put in those converters which have the
			//  correct type on both sides (i.e. Unicode (left) and Unicode(right)).
			foreach (IEncConverter aEC in DirectableEncConverter.EncConverters.Values)
			{
				if(     (m_eConversionTypeFilter == ConvType.Unknown)
					||  (EncConverters.IsConvTypeCompariable(aEC.ConversionType, m_eConversionTypeFilter)) )
				{
					m_mapLbItems2Tooltips[aEC.Name] = aEC.ToString();
					this.listBoxExistingConverters.Items.Add(aEC.Name);
				}
			}
		}

		void UpdateToolTip(Control ctrl, string strTip)
		{
			this.toolTips.SetToolTip(ctrl,strTip);
		}

		// get the IEncConverter corresponding to the selected converter name
		public  IEncConverter   IEncConverter
		{
			get
			{
				IEncConverter aEC = null;
				if( !String.IsNullOrEmpty(m_strConverterName) )
				{
					aEC = DirectableEncConverter.EncConverters[m_strConverterName];

					// set the options too
					aEC.DirectionForward = !this.checkBoxReverse.Checked;
					aEC.Debug = this.checkBoxDebug.Checked;
					if( this.radioButtonFullyComposed.Checked )
						aEC.NormalizeOutput = NormalizeFlags.FullyComposed;
					else if( this.radioButtonFullyDecomposed.Checked )
						aEC.NormalizeOutput = NormalizeFlags.FullyDecomposed;
					else
						aEC.NormalizeOutput = NormalizeFlags.None;

					// don't get the code page attributes just yet (or they may show an error dialog)
					// moved to "FormClosing"
					// change: do get them as they are legitimate (but don't thru errors)
					if (textBoxCodePageInput.Visible)
					{
						try
						{
							aEC.CodePageInput = Convert.ToInt32(textBoxCodePageInput.Text);
						}
						catch { }    // don't care
					}

					if (textBoxCodePageOutput.Visible)
					{
						try
						{
							aEC.CodePageOutput = Convert.ToInt32(textBoxCodePageOutput.Text);
						}
						catch { }    // don't care
					}
				}

				return aEC;
			}
		}

		protected int ProcessCodePage(string strCodePageValue)
		{
			int nCodePage = 0;
			try
			{
				if (!String.IsNullOrEmpty(strCodePageValue))
					nCodePage = Convert.ToInt32(strCodePageValue);
			}
			catch (Exception ex)
			{
				MessageBox.Show(String.Format("'{0}' is not a valid code page number", strCodePageValue), EncConverters.cstrCaption);
				throw ex;
			}
			return nCodePage;
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SelectConverter));
			this.labelInstruction = new System.Windows.Forms.Label();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.buttonOK = new System.Windows.Forms.Button();
			this.toolTips = new System.Windows.Forms.ToolTip(this.components);
			this.checkBoxReverse = new System.Windows.Forms.CheckBox();
			this.labelNormalizationType = new System.Windows.Forms.Label();
			this.checkBoxDebug = new System.Windows.Forms.CheckBox();
			this.buttonCreateNew = new System.Windows.Forms.Button();
			this.radioButtonFullyComposed = new System.Windows.Forms.RadioButton();
			this.radioButtonFullyDecomposed = new System.Windows.Forms.RadioButton();
			this.radioButtonNone = new System.Windows.Forms.RadioButton();
			this.buttonLaunchOptionsInstaller = new System.Windows.Forms.Button();
			this.checkBoxShowTooltips = new System.Windows.Forms.CheckBox();
			this.textBoxCodePageInput = new System.Windows.Forms.TextBox();
			this.textBoxCodePageOutput = new System.Windows.Forms.TextBox();
			this.buttonPreview = new System.Windows.Forms.Button();
			this.groupBoxOptions = new System.Windows.Forms.GroupBox();
			this.flowLayoutPanelCodePage = new System.Windows.Forms.FlowLayoutPanel();
			this.labelCodePageInput = new System.Windows.Forms.Label();
			this.labelCodePageOutput = new System.Windows.Forms.Label();
			this.contextMenu = new System.Windows.Forms.ContextMenu();
			this.menuItemEdit = new System.Windows.Forms.MenuItem();
			this.menuItemTest = new System.Windows.Forms.MenuItem();
			this.menuItemDelete = new System.Windows.Forms.MenuItem();
			this.listBoxExistingConverters = new System.Windows.Forms.ListBox();
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.textBoxDataPreview = new System.Windows.Forms.TextBox();
			this.contextMenuStripPreview = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.changeFontToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.copyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.selectAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.right2LeftToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.helpProvider = new System.Windows.Forms.HelpProvider();
			this.fontDialog = new System.Windows.Forms.FontDialog();
			this.timerTooltip = new System.Windows.Forms.Timer(this.components);
			this.groupBoxOptions.SuspendLayout();
			this.flowLayoutPanelCodePage.SuspendLayout();
			this.tableLayoutPanel1.SuspendLayout();
			this.contextMenuStripPreview.SuspendLayout();
			this.SuspendLayout();
			//
			// labelInstruction
			//
			this.labelInstruction.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.tableLayoutPanel1.SetColumnSpan(this.labelInstruction, 4);
			this.labelInstruction.Location = new System.Drawing.Point(3, 0);
			this.labelInstruction.Name = "labelInstruction";
			this.labelInstruction.Size = new System.Drawing.Size(439, 23);
			this.labelInstruction.TabIndex = 0;
			this.labelInstruction.Text = "Choose an existing converter from the list below or click Add New to add a new on" +
				"e:";
			this.labelInstruction.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			// buttonCancel
			//
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.helpProvider.SetHelpString(this.buttonCancel, "Click this button to cancel the selection of a converter and return to the callin" +
					"g program");
			this.buttonCancel.Location = new System.Drawing.Point(448, 364);
			this.buttonCancel.Name = "buttonCancel";
			this.helpProvider.SetShowHelp(this.buttonCancel, true);
			this.buttonCancel.Size = new System.Drawing.Size(97, 23);
			this.buttonCancel.TabIndex = 4;
			this.buttonCancel.Text = "&Cancel";
			this.toolTips.SetToolTip(this.buttonCancel, "Cancel selection");
			this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
			//
			// buttonOK
			//
			this.buttonOK.Enabled = false;
			this.helpProvider.SetHelpString(this.buttonOK, "Click this button to choose the selected converter and return to the calling prog" +
					"ram");
			this.buttonOK.Location = new System.Drawing.Point(367, 364);
			this.buttonOK.Name = "buttonOK";
			this.helpProvider.SetShowHelp(this.buttonOK, true);
			this.buttonOK.Size = new System.Drawing.Size(75, 23);
			this.buttonOK.TabIndex = 5;
			this.buttonOK.Text = "&OK";
			this.toolTips.SetToolTip(this.buttonOK, "Click here to choose the selected converter");
			this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
			//
			// toolTips
			//
			this.toolTips.AutomaticDelay = 50;
			this.toolTips.AutoPopDelay = 30000;
			this.toolTips.InitialDelay = 1000;
			this.toolTips.ReshowDelay = 500;
			//
			// checkBoxReverse
			//
			this.checkBoxReverse.Enabled = false;
			this.helpProvider.SetHelpString(this.checkBoxReverse, "Check this option if the reverse conversion is to be performed");
			this.checkBoxReverse.Location = new System.Drawing.Point(26, 24);
			this.checkBoxReverse.Name = "checkBoxReverse";
			this.helpProvider.SetShowHelp(this.checkBoxReverse, true);
			this.checkBoxReverse.Size = new System.Drawing.Size(148, 36);
			this.checkBoxReverse.TabIndex = 0;
			this.checkBoxReverse.Text = "&Reverse direction (for bidirectional converters)";
			this.toolTips.SetToolTip(this.checkBoxReverse, "Convert in the reverse direction");
			this.checkBoxReverse.CheckedChanged += new System.EventHandler(this.checkBoxReverse_CheckedChanged);
			//
			// labelNormalizationType
			//
			this.labelNormalizationType.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.labelNormalizationType.Enabled = false;
			this.labelNormalizationType.Location = new System.Drawing.Point(210, 24);
			this.labelNormalizationType.Name = "labelNormalizationType";
			this.labelNormalizationType.Size = new System.Drawing.Size(100, 16);
			this.labelNormalizationType.TabIndex = 4;
			this.labelNormalizationType.Text = "Normalize Output:";
			this.toolTips.SetToolTip(this.labelNormalizationType, "Controls the normalization of the converted data.");
			//
			// checkBoxDebug
			//
			this.checkBoxDebug.Enabled = false;
			this.helpProvider.SetHelpString(this.checkBoxDebug, "This option allows you to troubleshoot the selected converter with respect to the" +
					" data being send to and received from the underlying conversion engine");
			this.checkBoxDebug.Location = new System.Drawing.Point(26, 58);
			this.checkBoxDebug.Name = "checkBoxDebug";
			this.helpProvider.SetShowHelp(this.checkBoxDebug, true);
			this.checkBoxDebug.Size = new System.Drawing.Size(104, 24);
			this.checkBoxDebug.TabIndex = 5;
			this.checkBoxDebug.Text = "&Debug";
			this.toolTips.SetToolTip(this.checkBoxDebug, "Show debug information during conversion");
			//
			// buttonCreateNew
			//
			this.helpProvider.SetHelpString(this.buttonCreateNew, "Click this button to add a new converter to the list (including existing map file" +
					"s)");
			this.buttonCreateNew.Location = new System.Drawing.Point(3, 364);
			this.buttonCreateNew.Name = "buttonCreateNew";
			this.helpProvider.SetShowHelp(this.buttonCreateNew, true);
			this.buttonCreateNew.Size = new System.Drawing.Size(75, 23);
			this.buttonCreateNew.TabIndex = 8;
			this.buttonCreateNew.Text = "&Add New";
			this.toolTips.SetToolTip(this.buttonCreateNew, "Add a new converter to the list (including existing map files)");
			this.buttonCreateNew.Click += new System.EventHandler(this.buttonCreateNew_Click);
			//
			// radioButtonFullyComposed
			//
			this.radioButtonFullyComposed.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.radioButtonFullyComposed.Enabled = false;
			this.helpProvider.SetHelpString(this.radioButtonFullyComposed, "The output of the conversion is returned to the client application in Unicode Nor" +
					"malization Form \'Fully Composed\'");
			this.radioButtonFullyComposed.Location = new System.Drawing.Point(226, 88);
			this.radioButtonFullyComposed.Name = "radioButtonFullyComposed";
			this.helpProvider.SetShowHelp(this.radioButtonFullyComposed, true);
			this.radioButtonFullyComposed.Size = new System.Drawing.Size(116, 24);
			this.radioButtonFullyComposed.TabIndex = 3;
			this.radioButtonFullyComposed.Text = "Fully Co&mposed";
			this.toolTips.SetToolTip(this.radioButtonFullyComposed, "Output of the conversion is returned in Unicode Normalization Form \'Fully Compose" +
					"d\'");
			//
			// radioButtonFullyDecomposed
			//
			this.radioButtonFullyDecomposed.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.radioButtonFullyDecomposed.Enabled = false;
			this.helpProvider.SetHelpString(this.radioButtonFullyDecomposed, "The output of the conversion is returned to the client application in Unicode Nor" +
					"malization Form \'Fully Decomposed\'");
			this.radioButtonFullyDecomposed.Location = new System.Drawing.Point(226, 64);
			this.radioButtonFullyDecomposed.Name = "radioButtonFullyDecomposed";
			this.helpProvider.SetShowHelp(this.radioButtonFullyDecomposed, true);
			this.radioButtonFullyDecomposed.Size = new System.Drawing.Size(124, 24);
			this.radioButtonFullyDecomposed.TabIndex = 2;
			this.radioButtonFullyDecomposed.Text = "&Fully Decomposed";
			this.toolTips.SetToolTip(this.radioButtonFullyDecomposed, "Return data in Unicode Normalization Form \'Fully Decomposed\'");
			//
			// radioButtonNone
			//
			this.radioButtonNone.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.radioButtonNone.Checked = true;
			this.radioButtonNone.Enabled = false;
			this.helpProvider.SetHelpString(this.radioButtonNone, "The output of the conversion is returned to the client application without change" +
					"");
			this.radioButtonNone.Location = new System.Drawing.Point(226, 40);
			this.radioButtonNone.Name = "radioButtonNone";
			this.helpProvider.SetShowHelp(this.radioButtonNone, true);
			this.radioButtonNone.Size = new System.Drawing.Size(64, 24);
			this.radioButtonNone.TabIndex = 1;
			this.radioButtonNone.TabStop = true;
			this.radioButtonNone.Text = "&None";
			this.toolTips.SetToolTip(this.radioButtonNone, "Output of the conversion is returned without change");
			//
			// buttonLaunchOptionsInstaller
			//
			this.helpProvider.SetHelpString(this.buttonLaunchOptionsInstaller, "Click this button to launch the Converter Options Installer");
			this.buttonLaunchOptionsInstaller.Location = new System.Drawing.Point(165, 364);
			this.buttonLaunchOptionsInstaller.Name = "buttonLaunchOptionsInstaller";
			this.helpProvider.SetShowHelp(this.buttonLaunchOptionsInstaller, true);
			this.buttonLaunchOptionsInstaller.Size = new System.Drawing.Size(122, 23);
			this.buttonLaunchOptionsInstaller.TabIndex = 7;
			this.buttonLaunchOptionsInstaller.Text = "Con&verter Installer";
			this.toolTips.SetToolTip(this.buttonLaunchOptionsInstaller, "Launch the Converter Options Installer");
			this.buttonLaunchOptionsInstaller.Visible = false;
			this.buttonLaunchOptionsInstaller.Click += new System.EventHandler(this.buttonLaunchOptionsInstaller_Click);
			//
			// checkBoxShowTooltips
			//
			this.checkBoxShowTooltips.AutoSize = true;
			this.checkBoxShowTooltips.Checked = true;
			this.checkBoxShowTooltips.CheckState = System.Windows.Forms.CheckState.Checked;
			this.helpProvider.SetHelpString(this.checkBoxShowTooltips, "Uncheck this box to disable the display of converter information ToolTips");
			this.checkBoxShowTooltips.Location = new System.Drawing.Point(448, 3);
			this.checkBoxShowTooltips.Name = "checkBoxShowTooltips";
			this.helpProvider.SetShowHelp(this.checkBoxShowTooltips, true);
			this.checkBoxShowTooltips.Size = new System.Drawing.Size(97, 17);
			this.checkBoxShowTooltips.TabIndex = 10;
			this.checkBoxShowTooltips.Text = "&Show ToolTips";
			this.toolTips.SetToolTip(this.checkBoxShowTooltips, "Show ToolTips when checked");
			this.checkBoxShowTooltips.UseVisualStyleBackColor = true;
			this.checkBoxShowTooltips.CheckedChanged += new System.EventHandler(this.checkBoxShowTooltips_CheckedChanged);
			//
			// textBoxCodePageInput
			//
			this.textBoxCodePageInput.Location = new System.Drawing.Point(3, 16);
			this.textBoxCodePageInput.Name = "textBoxCodePageInput";
			this.textBoxCodePageInput.Size = new System.Drawing.Size(80, 20);
			this.textBoxCodePageInput.TabIndex = 7;
			this.toolTips.SetToolTip(this.textBoxCodePageInput, "this field shows the code page used for the legacy data that is the input to the " +
					"conversion. Typical values are \'0\' (default system code page), \'42\' (symbol font" +
					"s), \'1252\' (English operating systems)");
			this.textBoxCodePageInput.Visible = false;
			this.textBoxCodePageInput.TextChanged += new System.EventHandler(this.textBoxCodePageInput_TextChanged);
			//
			// textBoxCodePageOutput
			//
			this.textBoxCodePageOutput.Location = new System.Drawing.Point(3, 55);
			this.textBoxCodePageOutput.Name = "textBoxCodePageOutput";
			this.textBoxCodePageOutput.Size = new System.Drawing.Size(80, 20);
			this.textBoxCodePageOutput.TabIndex = 7;
			this.toolTips.SetToolTip(this.textBoxCodePageOutput, "this field shows the code page used for the legacy data that is the output of the" +
					" conversion. Typical values are \'0\' (default system code page), \'42\' (symbol fon" +
					"ts), \'1252\' (English operating systems)");
			this.textBoxCodePageOutput.Visible = false;
			this.textBoxCodePageOutput.TextChanged += new System.EventHandler(this.textBoxCodePageOutput_TextChanged);
			//
			// buttonPreview
			//
			this.buttonPreview.Enabled = false;
			this.helpProvider.SetHelpString(this.buttonPreview, "Click this button to open a preview window showing the result of the conversion w" +
					"ith the selected converter.");
			this.buttonPreview.Location = new System.Drawing.Point(84, 364);
			this.buttonPreview.Name = "buttonPreview";
			this.helpProvider.SetShowHelp(this.buttonPreview, true);
			this.buttonPreview.Size = new System.Drawing.Size(75, 23);
			this.buttonPreview.TabIndex = 11;
			this.buttonPreview.Text = "&Preview >>";
			this.toolTips.SetToolTip(this.buttonPreview, "Click this button to see a preview pane below (not supported by all client progra" +
					"ms)");
			this.buttonPreview.UseVisualStyleBackColor = true;
			this.buttonPreview.Click += new System.EventHandler(this.buttonPreview_Click);
			//
			// groupBoxOptions
			//
			this.tableLayoutPanel1.SetColumnSpan(this.groupBoxOptions, 5);
			this.groupBoxOptions.Controls.Add(this.flowLayoutPanelCodePage);
			this.groupBoxOptions.Controls.Add(this.checkBoxDebug);
			this.groupBoxOptions.Controls.Add(this.labelNormalizationType);
			this.groupBoxOptions.Controls.Add(this.radioButtonFullyComposed);
			this.groupBoxOptions.Controls.Add(this.radioButtonFullyDecomposed);
			this.groupBoxOptions.Controls.Add(this.radioButtonNone);
			this.groupBoxOptions.Controls.Add(this.checkBoxReverse);
			this.groupBoxOptions.Dock = System.Windows.Forms.DockStyle.Fill;
			this.groupBoxOptions.Location = new System.Drawing.Point(3, 230);
			this.groupBoxOptions.Name = "groupBoxOptions";
			this.groupBoxOptions.Size = new System.Drawing.Size(542, 128);
			this.groupBoxOptions.TabIndex = 6;
			this.groupBoxOptions.TabStop = false;
			this.groupBoxOptions.Text = "Conversion Options";
			//
			// flowLayoutPanelCodePage
			//
			this.flowLayoutPanelCodePage.Controls.Add(this.labelCodePageInput);
			this.flowLayoutPanelCodePage.Controls.Add(this.textBoxCodePageInput);
			this.flowLayoutPanelCodePage.Controls.Add(this.labelCodePageOutput);
			this.flowLayoutPanelCodePage.Controls.Add(this.textBoxCodePageOutput);
			this.flowLayoutPanelCodePage.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
			this.flowLayoutPanelCodePage.Location = new System.Drawing.Point(405, 19);
			this.flowLayoutPanelCodePage.Name = "flowLayoutPanelCodePage";
			this.flowLayoutPanelCodePage.Size = new System.Drawing.Size(131, 100);
			this.flowLayoutPanelCodePage.TabIndex = 8;
			//
			// labelCodePageInput
			//
			this.labelCodePageInput.AutoSize = true;
			this.labelCodePageInput.Location = new System.Drawing.Point(3, 0);
			this.labelCodePageInput.Name = "labelCodePageInput";
			this.labelCodePageInput.Size = new System.Drawing.Size(87, 13);
			this.labelCodePageInput.TabIndex = 6;
			this.labelCodePageInput.Text = "&Input Code Page";
			this.labelCodePageInput.Visible = false;
			//
			// labelCodePageOutput
			//
			this.labelCodePageOutput.AutoSize = true;
			this.labelCodePageOutput.Location = new System.Drawing.Point(3, 39);
			this.labelCodePageOutput.Name = "labelCodePageOutput";
			this.labelCodePageOutput.Size = new System.Drawing.Size(95, 13);
			this.labelCodePageOutput.TabIndex = 6;
			this.labelCodePageOutput.Text = "Output Code Pa&ge";
			this.labelCodePageOutput.Visible = false;
			//
			// contextMenu
			//
			this.contextMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
			this.menuItemEdit,
			this.menuItemTest,
			this.menuItemDelete});
			//
			// menuItemEdit
			//
			this.menuItemEdit.Index = 0;
			this.menuItemEdit.Text = "&Edit";
			this.menuItemEdit.Click += new System.EventHandler(this.menuItemEdit_Click);
			//
			// menuItemTest
			//
			this.menuItemTest.Index = 1;
			this.menuItemTest.Text = "&Test";
			this.menuItemTest.Click += new System.EventHandler(this.menuItemTest_Click);
			//
			// menuItemDelete
			//
			this.menuItemDelete.Index = 2;
			this.menuItemDelete.Text = "&Delete";
			this.menuItemDelete.Click += new System.EventHandler(this.menuItemDelete_Click);
			//
			// listBoxExistingConverters
			//
			this.tableLayoutPanel1.SetColumnSpan(this.listBoxExistingConverters, 5);
			this.listBoxExistingConverters.Dock = System.Windows.Forms.DockStyle.Fill;
			this.helpProvider.SetHelpString(this.listBoxExistingConverters, "This list shows all of the converters currently in the system repository");
			this.listBoxExistingConverters.Location = new System.Drawing.Point(3, 26);
			this.listBoxExistingConverters.Name = "listBoxExistingConverters";
			this.helpProvider.SetShowHelp(this.listBoxExistingConverters, true);
			this.listBoxExistingConverters.Size = new System.Drawing.Size(542, 186);
			this.listBoxExistingConverters.Sorted = true;
			this.listBoxExistingConverters.TabIndex = 9;
			this.listBoxExistingConverters.MouseUp += new System.Windows.Forms.MouseEventHandler(this.listBoxExistingConverters_MouseUp);
			this.listBoxExistingConverters.SelectedIndexChanged += new System.EventHandler(this.listBoxExistingConverters_SelectedIndexChanged);
			this.listBoxExistingConverters.DoubleClick += new System.EventHandler(this.listBoxExistingConverters_DoubleClick);
			this.listBoxExistingConverters.MouseMove += new System.Windows.Forms.MouseEventHandler(this.listBoxExistingConverters_MouseMove);
			//
			// tableLayoutPanel1
			//
			this.tableLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.tableLayoutPanel1.ColumnCount = 5;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 81F));
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.Controls.Add(this.labelInstruction, 0, 0);
			this.tableLayoutPanel1.Controls.Add(this.buttonCancel, 4, 3);
			this.tableLayoutPanel1.Controls.Add(this.buttonOK, 3, 3);
			this.tableLayoutPanel1.Controls.Add(this.buttonCreateNew, 0, 3);
			this.tableLayoutPanel1.Controls.Add(this.listBoxExistingConverters, 0, 1);
			this.tableLayoutPanel1.Controls.Add(this.groupBoxOptions, 0, 2);
			this.tableLayoutPanel1.Controls.Add(this.checkBoxShowTooltips, 4, 0);
			this.tableLayoutPanel1.Controls.Add(this.buttonPreview, 1, 3);
			this.tableLayoutPanel1.Controls.Add(this.textBoxDataPreview, 0, 4);
			this.tableLayoutPanel1.Controls.Add(this.buttonLaunchOptionsInstaller, 2, 3);
			this.tableLayoutPanel1.Location = new System.Drawing.Point(12, 12);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.RowCount = 5;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 70F));
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 30F));
			this.tableLayoutPanel1.Size = new System.Drawing.Size(548, 478);
			this.tableLayoutPanel1.TabIndex = 10;
			//
			// textBoxDataPreview
			//
			this.tableLayoutPanel1.SetColumnSpan(this.textBoxDataPreview, 5);
			this.textBoxDataPreview.ContextMenuStrip = this.contextMenuStripPreview;
			this.textBoxDataPreview.Dock = System.Windows.Forms.DockStyle.Fill;
			this.textBoxDataPreview.Font = new System.Drawing.Font("Arial Unicode MS", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.textBoxDataPreview.Location = new System.Drawing.Point(3, 393);
			this.textBoxDataPreview.Multiline = true;
			this.textBoxDataPreview.Name = "textBoxDataPreview";
			this.textBoxDataPreview.ReadOnly = true;
			this.textBoxDataPreview.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.textBoxDataPreview.Size = new System.Drawing.Size(542, 82);
			this.textBoxDataPreview.TabIndex = 12;
			//
			// contextMenuStripPreview
			//
			this.contextMenuStripPreview.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.changeFontToolStripMenuItem,
			this.toolStripSeparator1,
			this.copyToolStripMenuItem,
			this.toolStripSeparator2,
			this.selectAllToolStripMenuItem,
			this.toolStripSeparator3,
			this.right2LeftToolStripMenuItem});
			this.contextMenuStripPreview.Name = "contextMenuStrip";
			this.contextMenuStripPreview.Size = new System.Drawing.Size(200, 110);
			//
			// changeFontToolStripMenuItem
			//
			this.changeFontToolStripMenuItem.Name = "changeFontToolStripMenuItem";
			this.changeFontToolStripMenuItem.Size = new System.Drawing.Size(199, 22);
			this.changeFontToolStripMenuItem.Text = "Change &Font";
			this.changeFontToolStripMenuItem.ToolTipText = "Click here to change the display font for this text box";
			this.changeFontToolStripMenuItem.Click += new System.EventHandler(this.changeFontToolStripMenuItem_Click);
			//
			// toolStripSeparator1
			//
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(196, 6);
			//
			// copyToolStripMenuItem
			//
			this.copyToolStripMenuItem.Name = "copyToolStripMenuItem";
			this.copyToolStripMenuItem.Size = new System.Drawing.Size(199, 22);
			this.copyToolStripMenuItem.Text = "Copy";
			this.copyToolStripMenuItem.Click += new System.EventHandler(this.copyToolStripMenuItem_Click);
			//
			// toolStripSeparator2
			//
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(196, 6);
			//
			// selectAllToolStripMenuItem
			//
			this.selectAllToolStripMenuItem.Name = "selectAllToolStripMenuItem";
			this.selectAllToolStripMenuItem.Size = new System.Drawing.Size(199, 22);
			this.selectAllToolStripMenuItem.Text = "Select All";
			this.selectAllToolStripMenuItem.Click += new System.EventHandler(this.selectAllToolStripMenuItem_Click);
			//
			// toolStripSeparator3
			//
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			this.toolStripSeparator3.Size = new System.Drawing.Size(196, 6);
			//
			// right2LeftToolStripMenuItem
			//
			this.right2LeftToolStripMenuItem.CheckOnClick = true;
			this.right2LeftToolStripMenuItem.Name = "right2LeftToolStripMenuItem";
			this.right2LeftToolStripMenuItem.Size = new System.Drawing.Size(199, 22);
			this.right2LeftToolStripMenuItem.Text = "&Right to left reading order";
			this.right2LeftToolStripMenuItem.Click += new System.EventHandler(this.right2LeftToolStripMenuItem_Click);
			//
			// fontDialog
			//
			this.fontDialog.AllowScriptChange = false;
			this.fontDialog.ShowColor = true;
			//
			// timerTooltip
			//
			this.timerTooltip.Interval = 500;
			this.timerTooltip.Tick += new System.EventHandler(this.timerTooltip_Tick);
			//
			// SelectConverter
			//
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(572, 502);
			this.Controls.Add(this.tableLayoutPanel1);
			this.HelpButton = true;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "SelectConverter";
			this.Text = "Select Converter";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.dlgSelectConverter_FormClosing);
			this.groupBoxOptions.ResumeLayout(false);
			this.flowLayoutPanelCodePage.ResumeLayout(false);
			this.flowLayoutPanelCodePage.PerformLayout();
			this.tableLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel1.PerformLayout();
			this.contextMenuStripPreview.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		private void buttonCreateNew_Click(object sender, System.EventArgs e)
		{
			// in case the user cancels, we don't want to remember "past glory"
			m_strConverterName = null;

			bool bTooltipActive = toolTips.Active;
			toolTips.Active = false;
			IEncConverter aEC = null;
			if (    DirectableEncConverter.EncConverters.AutoConfigure(m_eConversionTypeFilter, ref m_strConverterName)
				&&  (m_strConverterName != null)
				&&  ((aEC = DirectableEncConverter.EncConverters[m_strConverterName]) != null))
			{
				if( this.listBoxExistingConverters.Items.Contains(m_strConverterName) )
					this.listBoxExistingConverters.Items.Remove(m_strConverterName);

				// add this new one to the listbox and make sure it's visible
				int nIndex = this.listBoxExistingConverters.Items.Add(m_strConverterName);
				this.listBoxExistingConverters.SelectedIndex = nIndex;

				// make it visible
				this.listBoxExistingConverters.TopIndex = nIndex;

				RevaluateButtonState();
				m_mapLbItems2Tooltips[aEC.Name] = aEC.ToString();
			}
			toolTips.Active = bTooltipActive;
		}

		private void buttonOK_Click(object sender, System.EventArgs e)
		{
			Debug.Assert(this.listBoxExistingConverters.SelectedIndex >= 0);
			this.DialogResult = DialogResult.OK;
			this.Close();
		}

		private void buttonCancel_Click(object sender, System.EventArgs e)
		{
			m_strConverterName = null;  // if they cancel, then we don't have one!
			this.DialogResult = DialogResult.Cancel;
			this.Close();
		}

		protected void RevaluateButtonState()
		{
			int nIndex = this.listBoxExistingConverters.SelectedIndex;
			this.buttonPreview.Enabled = this.buttonOK.Enabled = (nIndex >= 0);

			// if it's exactly 1, then enable 'OK' and set the 'last selected' name.
			if( this.buttonOK.Enabled )
			{
				m_strConverterName = (string)listBoxExistingConverters.SelectedItem;
				if (DirectableEncConverter.EncConverters.ContainsKey(m_strConverterName))    // should exist, but sometimes, doesn't!
				{
					IEncConverter aEC = DirectableEncConverter.EncConverters[m_strConverterName];
					Debug.Assert(aEC != null);

					// enable the options
					this.checkBoxDebug.Enabled =
						this.labelNormalizationType.Enabled =
						this.radioButtonNone.Enabled =
						this.radioButtonFullyComposed.Enabled =
						this.radioButtonFullyDecomposed.Enabled = true;

					// the reverse box is only enabled if the converter is bi-directional
					this.checkBoxReverse.Enabled = !EncConverters.IsUnidirectional(aEC.ConversionType);

					// the check state is dependent on the converter configuration.
					this.checkBoxReverse.Checked = !aEC.DirectionForward;
					this.checkBoxDebug.Checked = aEC.Debug;
					switch(aEC.NormalizeOutput)
					{
						case NormalizeFlags.FullyComposed:
							this.radioButtonFullyComposed.Checked = true;
							break;
						case NormalizeFlags.FullyDecomposed:
							this.radioButtonFullyDecomposed.Checked = true;
							break;
						case NormalizeFlags.None:
						default:
							this.radioButtonNone.Checked = true;
							break;
					};

					// indicate the code pages we're going to use for legacy encodings so that the
					//  user can change it if needed.
					DirectableEncConverter aDEC = new DirectableEncConverter(aEC);
					UpdateCodePageDetails(aDEC);
					UpdateDataPreview(aDEC);
				}
			}
			else
			{
				m_strConverterName = null;
				this.checkBoxReverse.Enabled =
				this.checkBoxDebug.Enabled =
				this.labelNormalizationType.Enabled =
				this.radioButtonNone.Enabled =
				this.radioButtonFullyComposed.Enabled =
				this.radioButtonFullyDecomposed.Enabled =
				labelCodePageInput.Visible = textBoxCodePageInput.Visible =
				labelCodePageOutput.Visible = textBoxCodePageOutput.Visible = false;
			}
		}

		protected void UpdateCodePageDetails(DirectableEncConverter aDEC)
		{
			// indicate the code pages we're going to use for legacy encodings so that the
			//  user can change it if needed.
			m_bTurnOffTextChangedEvents = true;
			textBoxCodePageInput.Text = aDEC.CodePageInput.ToString();
			textBoxCodePageOutput.Text = aDEC.CodePageOutput.ToString();
			m_bTurnOffTextChangedEvents = false;

			// turn them all invisible to start with (so they get visiblized in the correct order)
			textBoxCodePageInput.Visible = labelCodePageInput.Visible =
				textBoxCodePageOutput.Visible = labelCodePageOutput.Visible = false;
			if (aDEC.IsLhsLegacy)
				textBoxCodePageInput.Visible = labelCodePageInput.Visible = true;
			if (aDEC.IsRhsLegacy)
				textBoxCodePageOutput.Visible = labelCodePageOutput.Visible = true;
		}

		private void listBoxExistingConverters_DoubleClick(object sender, EventArgs e)
		{
			if( this.listBoxExistingConverters.SelectedIndex >= 0 )
				this.buttonOK_Click(sender,e);
		}

		protected int m_nLastTooltipDisplayedIndex = ListBox.NoMatches;

		private void listBoxExistingConverters_MouseMove(object sender, MouseEventArgs e)
		{
			int nIndex = this.listBoxExistingConverters.IndexFromPoint(e.X,e.Y);
			if (nIndex != m_nLastTooltipDisplayedIndex)
			{
				m_nLastTooltipDisplayedIndex = nIndex;
				toolTips.Hide(listBoxExistingConverters);
				if (nIndex != ListBox.NoMatches)
				{
					timerTooltip.Stop();
					timerTooltip.Start();
				}
			}
		}

		private void timerTooltip_Tick(object sender, EventArgs e)
		{
			try
			{
				if (m_nLastTooltipDisplayedIndex != ListBox.NoMatches)
				{
					string strKey = (string)this.listBoxExistingConverters.Items[m_nLastTooltipDisplayedIndex];

					string strDescription;
					if (m_mapLbItems2Tooltips.TryGetValue(strKey, out strDescription))
						UpdateToolTip(this.listBoxExistingConverters, strDescription);
				}
			}
			finally
			{
				timerTooltip.Stop();
			}
		}

		private void buttonLaunchOptionsInstaller_Click(object sender, System.EventArgs e)
		{
			// launch the Setup program (short-cut to add new converters)
			RegistryKey keyInstallLocation = Registry.LocalMachine.OpenSubKey(strInstallerLocationRegKey, false);
			if( keyInstallLocation != null )
			{
				string strInstallPath = (string)keyInstallLocation.GetValue(strInstallerPathKey);
				if(!String.IsNullOrEmpty(strInstallPath) && File.Exists(strInstallPath))
				{
					bool bTooltipActive = toolTips.Active;
					toolTips.Active = false;
					LaunchProgram(strInstallPath, null);
					toolTips.Active = bTooltipActive;

					// we have to requery the xml file because something might have changed.
					DirectableEncConverter.EncConverters.Reinitialize();
					InitializeConverterList();
				}
			}
		}

		static protected void LaunchProgram(string strProgram, string strArguments)
		{
			try
			{
				Process myProcess = new Process();

				myProcess.StartInfo.FileName = strProgram;
				myProcess.StartInfo.Arguments = strArguments;
				myProcess.Start();
				myProcess.WaitForExit();    // wait until finished, so we can reinitialize when done add/removing
			}
			catch {}    // we tried...
		}

		// get the point at which the right mouse button was clicked (for subsequent pop-up
		//  menu processing)
		private Point m_ptRightClicked;
		private void listBoxExistingConverters_MouseUp(object sender, MouseEventArgs e)
		{
			m_ptRightClicked = new Point(e.X,e.Y);
		}

		private void menuItemEdit_Click(object sender, System.EventArgs e)
		{
			int nIndex = listBoxExistingConverters.IndexFromPoint(m_ptRightClicked);
			if( nIndex >= 0 )
			{
				// select only the edit'ing item
				listBoxExistingConverters.SelectedIndex = nIndex;

				// check for this item in the repository collection (should exist, but sometimes, doesn't!)
				m_strConverterName = listBoxExistingConverters.Items[nIndex].ToString();
				if (DirectableEncConverter.EncConverters.ContainsKey(m_strConverterName))
				{
					IEncConverter aEC = DirectableEncConverter.EncConverters[m_strConverterName];
					Debug.Assert(aEC != null);

					// get the name (but if it's a temporary name, then just start from scratch (with no name)
					string strFriendlyName = aEC.Name;
					if (strFriendlyName.IndexOf(EncConverters.cstrTempConverterPrefix) == 0)
						strFriendlyName = null;

					bool bTooltipActive = toolTips.Active;
					toolTips.Active = false;
					if (DirectableEncConverter.EncConverters.AutoConfigureEx(aEC, aEC.ConversionType, ref strFriendlyName, aEC.LeftEncodingID, aEC.RightEncodingID))
					{
						// since even the name could theoretically change, remove the existing one and ...
						if( m_mapLbItems2Tooltips.ContainsKey(m_strConverterName) )
							m_mapLbItems2Tooltips.Remove(m_strConverterName);
						if( this.listBoxExistingConverters.Items.Contains(m_strConverterName) )
							this.listBoxExistingConverters.Items.Remove(m_strConverterName);

						// ... add the new one (under a possible new name)
						Debug.Assert(!String.IsNullOrEmpty(strFriendlyName));
						m_strConverterName = strFriendlyName;
						nIndex = this.listBoxExistingConverters.Items.Add(m_strConverterName);
						this.listBoxExistingConverters.SelectedIndex = nIndex;

						// make it visible
						this.listBoxExistingConverters.TopIndex = nIndex;

						// also update the tooltip and fixup the button state
						// (but first re-get the converter since (for some types, e.g. CmpdEncConverter)
						//  it might be totally different.
						aEC = DirectableEncConverter.EncConverters[m_strConverterName];
						if (aEC != null)
							m_mapLbItems2Tooltips[m_strConverterName] = aEC.ToString();
						RevaluateButtonState();
					}
					toolTips.Active = bTooltipActive;
				}
			}
		}

		private void menuItemDelete_Click(object sender, System.EventArgs e)
		{
			int nIndex = listBoxExistingConverters.IndexFromPoint(m_ptRightClicked);
			if( nIndex >= 0 )
			{
				bool bTooltipActive = toolTips.Active;
				toolTips.Active = false;
				// get the name and confirm the deletion
				string strName = (string)this.listBoxExistingConverters.Items[nIndex];
				if( MessageBox.Show(String.Format("Are you sure you want to delete the following converter? {1}{1}{0}", strName, Environment.NewLine), EncConverters.cstrCaption, MessageBoxButtons.YesNoCancel) == DialogResult.Yes )
				{
					listBoxExistingConverters.Items.RemoveAt(nIndex);

					// remove it from the repository as well
					DirectableEncConverter.EncConverters.Remove(strName);

					// the reevaluate the button state.
					RevaluateButtonState();
				}
				toolTips.Active = bTooltipActive;
			}
		}

		private void listBoxExistingConverters_SelectedIndexChanged(object sender, EventArgs e)
		{
			RevaluateButtonState();
		}

		private void checkBoxShowTooltips_CheckedChanged(object sender, EventArgs e)
		{
			toolTips.Active = this.checkBoxShowTooltips.Checked;
			try
			{
				RegistryKey keyLastTooltipState = Registry.LocalMachine.CreateSubKey(EncConverters.SEC_ROOT_KEY);
				if (keyLastTooltipState != null)
					keyLastTooltipState.SetValue(EncConverters.strShowToolTipsStateKey, this.checkBoxShowTooltips.Checked);
			}
			catch { }   // it might fail on some systems. this isn't that important that we even need to bother the user about it.
		}

		private void checkBoxReverse_CheckedChanged(object sender, EventArgs e)
		{
			IEncConverter aEC = IEncConverter;
			if (aEC != null)
			{
				DirectableEncConverter aDEC = new DirectableEncConverter(aEC);
				UpdateCodePageDetails(aDEC);
				UpdateDataPreview(aDEC);
			}
		}

		private void dlgSelectConverter_FormClosing(object sender, FormClosingEventArgs e)
		{
			// before going away, set the CodePage values (so the caller will have them to use)
			IEncConverter aEC = IEncConverter;
			if (aEC != null)
			{
				try
				{
					DirectableEncConverter aDEC = new DirectableEncConverter(aEC);
					aDEC.CodePageInput = ProcessCodePage(textBoxCodePageInput.Text);
					aDEC.CodePageOutput = ProcessCodePage(textBoxCodePageOutput.Text);
				}
				catch
				{
					e.Cancel = true;
				}
			}
		}

		protected void UpdateDataPreview(DirectableEncConverter aDEC)
		{
			// if we're doing preview (i.e. the preview pain in the fifth row of the table layout is visible)...
			if (tableLayoutPanel1.RowCount == 5)
			{
				// ... and if we're doing byte data (which could be UTF-8)
				string strPreviewData;
				if (m_byPreviewData != null)
				{
					EncodingForm eOrigForm = aDEC.GetEncConverter.EncodingIn;
					if (aDEC.IsLhsLegacy)
						aDEC.GetEncConverter.EncodingIn = EncodingForm.LegacyBytes;
					else
						aDEC.GetEncConverter.EncodingIn = EncodingForm.UTF8Bytes;

					strPreviewData = EncConverters.ByteArrToBytesString(m_byPreviewData);

					// this might throw up, so don't let it crash the program.
					textBoxDataPreview.Text = CallSafeConvert(aDEC, strPreviewData);

					aDEC.GetEncConverter.EncodingIn = eOrigForm;
				}
				else if (!String.IsNullOrEmpty(m_strPreviewData))
				{
					// ... otherwise, the user has already converted it (if legacy) to wide
					//  and we should just show her what she gets.
					textBoxDataPreview.Text = CallSafeConvert(aDEC, m_strPreviewData);
				}

				// if we weren't given a font name by the client, then let's see if the repository has a suggestion
				if (String.IsNullOrEmpty(m_strFontName))
				{
					string strLhsName, strRhsName;
					if (DirectableEncConverter.EncConverters.GetFontMappingFromMapping(aDEC.Name, out strLhsName, out strRhsName))
					{
						bool bDirForward = aDEC.GetEncConverter.DirectionForward;
						textBoxDataPreview.Font = CreateFontSafe((bDirForward) ? strRhsName : strLhsName);
					}
				}
			}
		}

		protected string CallSafeConvert(DirectableEncConverter aDEC, string strPreviewData)
		{
			string strOutput = null;
			try
			{
				// this might throw up, so don't let it crash the program.
				strOutput = aDEC.Convert(strPreviewData);
			}
			catch (Exception ex)
			{
				MessageBox.Show(String.Format("Unable to convert data for preview because: '{0}'", ex.Message), EncConverters.cstrCaption);
			}
			return strOutput;
		}

		private void buttonPreview_Click(object sender, EventArgs e)
		{
			string strPreviewButtonLabel = buttonPreview.Text;
			string strPreviewButtonLabelPostfix = null;
			if (strPreviewButtonLabel[strPreviewButtonLabel.Length - 1] == '>')
			{
				// open the data preview window
				textBoxDataPreview.Show();
				tableLayoutPanel1.RowCount = 5;
				strPreviewButtonLabelPostfix = "<<";

				// if there is a converter already selected...
				IEncConverter aEC = IEncConverter;
				if (aEC != null)
				{
					DirectableEncConverter aDEC = new DirectableEncConverter(aEC);
					UpdateDataPreview(aDEC);
				}
			}
			else
			{
				// close the data preview window
				textBoxDataPreview.Hide();
				tableLayoutPanel1.RowCount = 4;
				strPreviewButtonLabelPostfix = ">>";
			}

			buttonPreview.Text = strPreviewButtonLabel.Substring(0, strPreviewButtonLabel.Length - 2) + strPreviewButtonLabelPostfix;
		}

		protected bool m_bTurnOffTextChangedEvents = false;

		private void textBoxCodePageInput_TextChanged(object sender, EventArgs e)
		{
			if (m_bTurnOffTextChangedEvents)
				return;

			try
			{
				string strCodePage = textBoxCodePageInput.Text;
				if (strCodePage == "42")    // symbol code page
					strCodePage = "28591";  // use ISO 8859_1 instead

				int nCP = Convert.ToInt32(strCodePage);
				Encoding enc = Encoding.GetEncoding(nCP);

				// if either of the above don't throw an exception, then potentially recalculate the Data Preview
				IEncConverter aEC = IEncConverter;
				if (aEC != null)
				{
					DirectableEncConverter aDEC = new DirectableEncConverter(aEC);
					UpdateDataPreview(aDEC);
				}
			}
			catch
			{
			}
		}

		private void textBoxCodePageOutput_TextChanged(object sender, EventArgs e)
		{
			if (m_bTurnOffTextChangedEvents)
				return;

			try
			{
				string strCodePage = textBoxCodePageInput.Text;
				if (strCodePage == "42")    // symbol code page
					strCodePage = "28591";  // use ISO 8859_1 instead

				int nCP = Convert.ToInt32(strCodePage);
				Encoding enc = Encoding.GetEncoding(nCP);

				// if either of the above don't throw an exception, then potentially recalculate the Data Preview
				IEncConverter aEC = IEncConverter;
				if (aEC != null)
				{
					DirectableEncConverter aDEC = new DirectableEncConverter(aEC);
					UpdateDataPreview(aDEC);
				}
			}
			catch
			{
			}
		}

		private void menuItemTest_Click(object sender, EventArgs e)
		{
			int nIndex = listBoxExistingConverters.IndexFromPoint(m_ptRightClicked);
			if (nIndex >= 0)
			{
				bool bTooltipActive = toolTips.Active;
				toolTips.Active = false;

				// get the name
				m_strConverterName = (string)this.listBoxExistingConverters.Items[nIndex];
				if (DirectableEncConverter.EncConverters.ContainsKey(m_strConverterName))
				{
					EncConverters aECs = DirectableEncConverter.EncConverters;
					IEncConverter aEC = aECs[m_strConverterName];
					Debug.Assert(aEC != null);

					IEncConverterConfig aECC = aEC.Configurator;
					if (aECC != null)
					{
						string strTestData;
						if (m_byPreviewData != null)
							strTestData = Encoding.Default.GetString(m_byPreviewData);
						else if (!String.IsNullOrEmpty(m_strPreviewData))
							strTestData = m_strPreviewData;
						else
							strTestData = "Test Data";

						try
						{
							aECC.DisplayTestPage(aECs, m_strConverterName, aEC.ConverterIdentifier, aEC.ConversionType, strTestData);
						}
						catch { }
					}
					else
					{
						MessageBox.Show("This converter type doesn't support the test feature.", EncConverters.cstrCaption);
					}
				}

				toolTips.Active = bTooltipActive;
			}
		}

		private void changeFontToolStripMenuItem_Click(object sender, EventArgs e)
		{
			fontDialog.Font = textBoxDataPreview.Font;
			if (fontDialog.ShowDialog() == DialogResult.OK)
				textBoxDataPreview.Font = fontDialog.Font;
		}

		private void copyToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (textBoxDataPreview.SelectionLength == 0)
				textBoxDataPreview.SelectAll();
			textBoxDataPreview.Copy();
		}

		private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
		{
			textBoxDataPreview.SelectAll();
		}

		private void right2LeftToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ToolStripMenuItem aMenuItem = (ToolStripMenuItem)sender;
			textBoxDataPreview.RightToLeft = (aMenuItem.Checked) ? RightToLeft.Yes : RightToLeft.No;
		}
	}
}

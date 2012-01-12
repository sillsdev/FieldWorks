using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Resources;
using System.Reflection;
using ECInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SilEncConverters31;
using System.Diagnostics;

using SIL.Utils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Resources;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for CnvtrPropertiesCtrl.
	/// </summary>
	/// <remarks>Public so we can test it</remarks>
	/// ------------------------------------------------------------------------------------
	public class CnvtrPropertiesCtrl : UserControl, IFWDisposable
	{
		// Note: several of these controls are public in order to facilitate testing.
		// Few of them are actually required by other classes
		/// <summary></summary>
		public FwOverrideComboBox cboConversion;
		/// <summary>name of the converter</summary>
		public TextBox txtName;
		/// <summary></summary>
		public Button btnMapFile;
		/// <summary></summary>
		public TextBox txtMapFile;
		/// <summary></summary>
		public FwOverrideComboBox cboConverter;
		/// <summary></summary>
		public FwOverrideComboBox cboSpec;
		private OpenFileDialog ofDlg = new OpenFileDialog();

		/// <summary>Event handler when settings for a converter change.</summary>
		public event EventHandler ConverterFileChanged;
		/// <summary>Event handler for updating a changed list of encoding converters.</summary>
		public event EventHandler ConverterListChanged;
		// Raised when a new converter is added or a modified one saved.
		// The client should generally select the saved converter in the list.
		/// <summary>Event handler for a saved encoding converters.</summary>
		public event EventHandler ConverterSaved;

		private bool m_fOnlyUnicode;
		/// <summary>The Encoding Converter may have changed and may need to be saved.</summary>
		private bool m_fConverterChanged = false;
		private IApp m_app;

		/// <summary>
		/// Is the loaded converter supported? True if no conv is loaded. Basically, we're just trying to
		/// find out if we should prevent the user from changing a converter that we are unable to modify.
		/// </summary>
		public bool m_supportedConverter = true;
		private bool m_selectingMapping = false;
		private int m_mappingWidth;

		/// <summary>List of encoding converters in the list box which are not yet defined.</summary>
		private Dictionary<string,EncoderInfo> m_undefinedList = new Dictionary<string,EncoderInfo>();

		/// <summary>The actual specs string that will be used to initialize a new converter.</summary>
		public string m_specs;
		EncConverters m_encConverters;

		private HelpProvider helpProvider1;
		private Label lblSpecs;
		private Button btnModify;
		private Label lblConversion;
		private Label lblName;
		private Label lblConverter;
		private Button btnMore;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the application.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IApp Application
		{
			set { m_app = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the converters.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public EncConverters Converters
		{
			get
			{
				CheckDisposed();
				return m_encConverters;
			}
			set
			{
				CheckDisposed();
				m_encConverters = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the current converter name.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ConverterName
		{
			get
			{
				CheckDisposed();
				return txtName.Text;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether the encoding converter may have changed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ConverterChanged
		{
			get { return DesignMode || m_fConverterChanged || m_undefinedList.ContainsKey(ConverterName); }
			set { m_fConverterChanged = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:CnvtrPropertiesCtrl"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public CnvtrPropertiesCtrl()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			m_mappingWidth = txtMapFile.Width;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If true, show and create only Unicode converters (both to and to/from).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool OnlyUnicode
		{
			get
			{
				CheckDisposed();
				return m_fOnlyUnicode;
			}
			set
			{
				CheckDisposed();

				m_fOnlyUnicode = value;
				CnvtrPropertiesCtrl_Load(null, null);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the list of undefined encoding converters.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal Dictionary<string, EncoderInfo> UndefinedConverters
		{
			set { m_undefinedList = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");

			if (disposing && !IsDisposed)
			{
				if (components != null)
					components.Dispose();

				if (ofDlg != null)
					ofDlg.Dispose();
			}
			ofDlg = null;
			components = null;
			base.Dispose(disposing);
		}

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CnvtrPropertiesCtrl));
			this.lblSpecs = new System.Windows.Forms.Label();
			this.helpProvider1 = new HelpProvider();
			this.lblConversion = new System.Windows.Forms.Label();
			this.lblName = new System.Windows.Forms.Label();
			this.lblConverter = new System.Windows.Forms.Label();
			this.cboConversion = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.txtName = new System.Windows.Forms.TextBox();
			this.btnMapFile = new System.Windows.Forms.Button();
			this.txtMapFile = new System.Windows.Forms.TextBox();
			this.cboConverter = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.cboSpec = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.btnModify = new System.Windows.Forms.Button();
			this.btnMore = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// lblSpecs
			//
			resources.ApplyResources(this.lblSpecs, "lblSpecs");
			this.lblSpecs.Name = "lblSpecs";
			this.helpProvider1.SetShowHelp(this.lblSpecs, ((bool)(resources.GetObject("lblSpecs.ShowHelp"))));
			//
			// lblConversion
			//
			resources.ApplyResources(this.lblConversion, "lblConversion");
			this.lblConversion.Name = "lblConversion";
			this.helpProvider1.SetShowHelp(this.lblConversion, ((bool)(resources.GetObject("lblConversion.ShowHelp"))));
			//
			// lblName
			//
			resources.ApplyResources(this.lblName, "lblName");
			this.lblName.Name = "lblName";
			this.helpProvider1.SetShowHelp(this.lblName, ((bool)(resources.GetObject("lblName.ShowHelp"))));
			//
			// lblConverter
			//
			resources.ApplyResources(this.lblConverter, "lblConverter");
			this.lblConverter.Name = "lblConverter";
			this.helpProvider1.SetShowHelp(this.lblConverter, ((bool)(resources.GetObject("lblConverter.ShowHelp"))));
			//
			// cboConversion
			//
			this.cboConversion.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.helpProvider1.SetHelpString(this.cboConversion, resources.GetString("cboConversion.HelpString"));
			resources.ApplyResources(this.cboConversion, "cboConversion");
			this.cboConversion.Name = "cboConversion";
			this.helpProvider1.SetShowHelp(this.cboConversion, ((bool)(resources.GetObject("cboConversion.ShowHelp"))));
			this.cboConversion.SelectedIndexChanged += new System.EventHandler(this.cboConversion_SelectedIndexChanged);
			//
			// txtName
			//
			resources.ApplyResources(this.txtName, "txtName");
			this.helpProvider1.SetHelpString(this.txtName, resources.GetString("txtName.HelpString"));
			this.txtName.Name = "txtName";
			this.helpProvider1.SetShowHelp(this.txtName, ((bool)(resources.GetObject("txtName.ShowHelp"))));
			this.txtName.TextChanged += new System.EventHandler(this.txtName_TextChanged);
			//
			// btnMapFile
			//
			resources.ApplyResources(this.btnMapFile, "btnMapFile");
			this.btnMapFile.Name = "btnMapFile";
			this.helpProvider1.SetShowHelp(this.btnMapFile, ((bool)(resources.GetObject("btnMapFile.ShowHelp"))));
			this.btnMapFile.Click += new System.EventHandler(this.btnMapFile_Click);
			//
			// txtMapFile
			//
			resources.ApplyResources(this.txtMapFile, "txtMapFile");
			this.txtMapFile.Name = "txtMapFile";
			this.helpProvider1.SetShowHelp(this.txtMapFile, ((bool)(resources.GetObject("txtMapFile.ShowHelp"))));
			this.txtMapFile.TextChanged += new System.EventHandler(this.txtMapFile_TextChanged);
			//
			// cboConverter
			//
			this.cboConverter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.helpProvider1.SetHelpString(this.cboConverter, resources.GetString("cboConverter.HelpString"));
			resources.ApplyResources(this.cboConverter, "cboConverter");
			this.cboConverter.Name = "cboConverter";
			this.helpProvider1.SetShowHelp(this.cboConverter, ((bool)(resources.GetObject("cboConverter.ShowHelp"))));
			this.cboConverter.SelectedIndexChanged += new System.EventHandler(this.cboConverter_SelectedIndexChanged);
			//
			// cboSpec
			//
			resources.ApplyResources(this.cboSpec, "cboSpec");
			this.cboSpec.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.helpProvider1.SetHelpString(this.cboSpec, resources.GetString("cboSpec.HelpString"));
			this.cboSpec.Name = "cboSpec";
			this.helpProvider1.SetShowHelp(this.cboSpec, ((bool)(resources.GetObject("cboSpec.ShowHelp"))));
			this.cboSpec.Sorted = true;
			this.cboSpec.SelectedIndexChanged += new System.EventHandler(this.cboSpec_SelectedIndexChanged);
			//
			// btnModify
			//
			resources.ApplyResources(this.btnModify, "btnModify");
			this.btnModify.Name = "btnModify";
			this.btnModify.UseVisualStyleBackColor = true;
			this.btnModify.Click += new System.EventHandler(this.btnModify_Click);
			//
			// btnMore
			//
			resources.ApplyResources(this.btnMore, "btnMore");
			this.btnMore.Name = "btnMore";
			this.btnMore.UseVisualStyleBackColor = true;
			this.btnMore.Click += new System.EventHandler(this.btnMore_Click);
			//
			// CnvtrPropertiesCtrl
			//
			this.Controls.Add(this.btnMore);
			this.Controls.Add(this.btnModify);
			this.Controls.Add(this.cboSpec);
			this.Controls.Add(this.lblConverter);
			this.Controls.Add(this.cboConverter);
			this.Controls.Add(this.lblConversion);
			this.Controls.Add(this.lblName);
			this.Controls.Add(this.lblSpecs);
			this.Controls.Add(this.cboConversion);
			this.Controls.Add(this.txtName);
			this.Controls.Add(this.btnMapFile);
			this.Controls.Add(this.txtMapFile);
			this.Name = "CnvtrPropertiesCtrl";
			this.helpProvider1.SetShowHelp(this, ((bool)(resources.GetObject("$this.ShowHelp"))));
			resources.ApplyResources(this, "$this");
			this.Load += new System.EventHandler(this.CnvtrPropertiesCtrl_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the SelectedIndexChanged event of the cboConverter control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void cboConverter_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			txtMapFile.Text = "";
			List<FileFilterType> fileTypes = new List<FileFilterType>();
			fileTypes.Add(FileFilterType.AllFiles);
			if (cboConverter.SelectedIndex == -1)
			{
				// Nothing is selected; this happens initially and can also happen if the user
				// selects a mapping of a type we don't recognize. We display the filename box
				// and file chooser with *.* as the file type since we have no idea what it
				// should be.
				this.ofDlg.DefaultExt = "";
				this.ofDlg.Filter = ResourceHelper.BuildFileFilter(fileTypes);
				this.ofDlg.Title = AddConverterResources.kstrUnspecifiedTitle;
				SetReadyToGiveMapFile();
			}
			else // based on the selected item in cboConverter, load their spec options
			{
				switch (((CnvtrTypeComboItem)cboConverter.SelectedItem).Type)
				{
					case ConverterType.ktypeRegEx:
						//this.helpProvider1.SetHelpString(this.txtMapFile, AddConverterResources.kstrCCHelp);
						//this.helpProvider1.SetShowHelp(this.txtMapFile, true);
						//this.helpProvider1.SetHelpString(this.btnMapFile,
						//    AddConverterResources.kstrFindMapping);
						//this.ofDlg.DefaultExt = "cct";
						//this.ofDlg.Filter = AddConverterResources.kstrCCFileFilter;
						//this.ofDlg.Title = AddConverterResources.kstrCCTitle;
						SetReadyToGiveRegEx();
						break;
					case ConverterType.ktypeCC:
						this.helpProvider1.SetHelpString(this.txtMapFile, AddConverterResources.kstrCCHelp);
						this.helpProvider1.SetShowHelp(this.txtMapFile, true);
						this.helpProvider1.SetHelpString(this.btnMapFile,
							AddConverterResources.kstrFindMapping);
						this.ofDlg.DefaultExt = "cct";
						fileTypes.Insert(0, FileFilterType.AllCCTable);
						this.ofDlg.Filter = ResourceHelper.BuildFileFilter(fileTypes);
						this.ofDlg.Title = AddConverterResources.kstrCCTitle;
						SetReadyToGiveMapFile();
						break;
					case ConverterType.ktypeTecKitTec:
						this.helpProvider1.SetHelpString(this.txtMapFile,
							AddConverterResources.kstrTecHelp);
						this.helpProvider1.SetShowHelp(this.txtMapFile, true);
						this.helpProvider1.SetHelpString(this.btnMapFile,
							AddConverterResources.kstrFindMapping);
						this.ofDlg.DefaultExt = "tec";
						fileTypes.Insert(0, FileFilterType.TECkitCompiled);
						this.ofDlg.Filter = ResourceHelper.BuildFileFilter(fileTypes);
						this.ofDlg.Title = AddConverterResources.kstrTecTitle;
						SetReadyToGiveMapFile();
						break;
					case ConverterType.ktypeTecKitMap:
						this.helpProvider1.SetHelpString(this.txtMapFile,
							AddConverterResources.kstrTecHelp);
						this.helpProvider1.SetShowHelp(this.txtMapFile, true);
						this.helpProvider1.SetHelpString(this.btnMapFile,
							AddConverterResources.kstrFindMapping);
						this.ofDlg.DefaultExt = "map";
						fileTypes.Insert(0, FileFilterType.TECkitMapping);
						this.ofDlg.Filter = ResourceHelper.BuildFileFilter(fileTypes);
						this.ofDlg.Title = AddConverterResources.kstrTecTitle;
						SetReadyToGiveMapFile();
						break;
					case ConverterType.ktypeCodePage:
						this.helpProvider1.SetHelpString(this.cboSpec,
							AddConverterResources.kstrCPHelp);
						this.helpProvider1.SetShowHelp(this.cboSpec, true);
						SetReadyToGiveCodePage();
						// Fill in combo items. This list should not change, so it's fine to do it
						// once and save it.
						cboSpec.BeginUpdate();
						cboSpec.Items.Clear();
						ILgCodePageEnumerator lcpe = LgCodePageEnumeratorClass.Create();
						lcpe.Init();
						int codePage = 0;
						string codePageName = "";
						for (; ; )
						{
							lcpe.Next(out codePage, out codePageName);
							if (codePage == 0)
								break;
							cboSpec.Items.Add(new CnvtrSpecComboItem(String.Format(FwCoreDlgs.ksCodePageDisplay,
								codePageName, codePage), codePage.ToString()));
						}
						cboSpec.EndUpdate();
						break;
					case ConverterType.ktypeIcuConvert:
						this.helpProvider1.SetHelpString(this.cboSpec,
							AddConverterResources.kstrIcuConvHelp);
						this.helpProvider1.SetShowHelp(this.cboSpec, true);
						SetReadyToGiveSpec();
						// fill in combo items.
						cboSpec.BeginUpdate();
						cboSpec.Items.Clear();
						ILgIcuConverterEnumerator lice = LgIcuConverterEnumeratorClass.Create();
						int cconv = lice.Count;

						try
						{
							for (int i = 0; i < cconv; i++)
							{
								string name = lice.get_ConverterName(i);
								string id = lice.get_ConverterId(i);
								if (!String.IsNullOrEmpty(name))
									cboSpec.Items.Add(new CnvtrSpecComboItem(name, id));
							}
						}
						catch (Exception ee)
						{
							Debug.Assert(m_app != null, "Bet you wish you set the Application property!");
							Debug.WriteLine(ee.Message);
							MessageBox.Show(String.Format(AddConverterDlgStrings.kstidICUErrorText, Environment.NewLine, m_app.ApplicationName),
								AddConverterDlgStrings.kstidICUErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
						}
						cboSpec.EndUpdate();
						break;
					case ConverterType.ktypeIcuTransduce:
						this.helpProvider1.SetHelpString(this.cboSpec,
							AddConverterResources.kstrIcuTransHelp);
						this.helpProvider1.SetShowHelp(this.cboSpec, true);
						SetReadyToGiveSpec();
						// fill in combo items.
						cboSpec.BeginUpdate();
						cboSpec.Items.Clear();
						ILgIcuTransliteratorEnumerator lite = LgIcuTransliteratorEnumeratorClass.Create();
						int ctrans = lite.Count;
						for (int i = 0; i < ctrans; i++)
						{
							string name = lite.get_TransliteratorName(i);
							string id = lite.get_TransliteratorId(i);
							cboSpec.Items.Add(new CnvtrSpecComboItem(name, id));
						}
						cboSpec.EndUpdate();
						break;
					default:
						this.helpProvider1.SetHelpString(this.cboSpec,
							AddConverterResources.kstrGenericHelp);
						this.helpProvider1.SetShowHelp(this.cboSpec, true);
						Debug.Assert(false, "Invalid main converter type");
						break;
				}
				m_fConverterChanged = true;
			}
			// Lets pre-populate cboConversion for them, if we aren't loading
			if (!m_selectingMapping)
			{
				ConvType setType = ConvType.Unicode_to_Unicode;

				if (!OnlyUnicode)
				{
					switch (((CnvtrTypeComboItem)cboConverter.SelectedItem).Type)
					{
						case ConverterType.ktypeCC:
							setType = ConvType.Legacy_to_Unicode;
							break;
						case ConverterType.ktypeIcuConvert:
							setType = ConvType.Legacy_to_from_Unicode;
							break;
						case ConverterType.ktypeIcuTransduce:
							setType = ConvType.Unicode_to_from_Unicode;
							break;
						case ConverterType.ktypeTecKitTec:
						case ConverterType.ktypeTecKitMap:
						case ConverterType.ktypeCodePage:
							setType = ConvType.Legacy_to_from_Unicode;
							break;
					}
				}
				else if (((CnvtrTypeComboItem)cboConverter.SelectedItem).Type != ConverterType.ktypeCC)
				{
					// if we are in UtU mode, prepopulate all as UtfU
					setType = ConvType.Unicode_to_from_Unicode;
				}

				SetConverterType(setType);
			}
			m_fConverterChanged = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the type of the converter in the combobox for the currently selected encoding
		/// converter.
		/// </summary>
		/// <param name="setConvType">Type of the selected converter.</param>
		/// ------------------------------------------------------------------------------------
		private void SetConverterType(ConvType setConvType)
		{
			for (int i = 0; i < cboConversion.Items.Count; i++)
			{
				if (((CnvtrDataComboItem)cboConversion.Items[i]).Type == setConvType)
				{
					cboConversion.SelectedIndex = i;
					break;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Occurs on loading the Properties Tab
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		public void CnvtrPropertiesCtrl_Load(object sender, System.EventArgs e)
		{
			CheckDisposed();

			// This is a fall-back if the creator does not have a converters object.
			// It is generally preferable for the creator to make one and pass it in.
			// Multiple EncConverters objects are problematical because they don't all get
			// updated when something changes.
			// JohnT: note that this ALWAYS happens at least once, because the Load event happens during
			// the main dialog's InitializeComponent method, before it sets the encConverters
			// of the control.
			if (m_encConverters == null)
				m_encConverters = new SilEncConverters31.EncConverters();
			cboConverter.Items.Clear();
			cboConverter.Items.Add(new CnvtrTypeComboItem(AddConverterResources.kstrCc, ConverterType.ktypeCC, EncConverters.strTypeSILcc));
			if (!m_fOnlyUnicode)
				cboConverter.Items.Add(new CnvtrTypeComboItem(AddConverterResources.kstrIcuConv, ConverterType.ktypeIcuConvert, EncConverters.strTypeSILicuConv));
			cboConverter.Items.Add(new CnvtrTypeComboItem(AddConverterResources.kstrIcuTransduce, ConverterType.ktypeIcuTransduce, EncConverters.strTypeSILicuTrans));
			cboConverter.Items.Add(new CnvtrTypeComboItem(AddConverterResources.kstrTecTec, ConverterType.ktypeTecKitTec, EncConverters.strTypeSILtec));
			cboConverter.Items.Add(new CnvtrTypeComboItem(AddConverterResources.kstrTecMap, ConverterType.ktypeTecKitMap, EncConverters.strTypeSILmap));
			cboConverter.Items.Add(new CnvtrTypeComboItem(AddConverterResources.kstrRegExpIcu, ConverterType.ktypeRegEx, EncConverters.strTypeSILicuRegex));
			if (!m_fOnlyUnicode)
				cboConverter.Items.Add(new CnvtrTypeComboItem(AddConverterResources.kstrCodePage, ConverterType.ktypeCodePage, EncConverters.strTypeSILcp));

			cboConversion.Items.Clear();
			if (OnlyUnicode)
			{
				cboConversion.Items.Add(new CnvtrDataComboItem(AddConverterResources.kstrUnicode_to_from_Unicode,
					ConvType.Unicode_to_from_Unicode));
				cboConversion.Items.Add(new CnvtrDataComboItem(AddConverterResources.kstrUnicode_to_Unicode,
					ConvType.Unicode_to_Unicode));
			}
			else
			{
				cboConversion.Items.Add(new CnvtrDataComboItem(AddConverterResources.kstrLegacy_to_from_Legacy,
					ConvType.Legacy_to_from_Legacy));
				cboConversion.Items.Add(new CnvtrDataComboItem(AddConverterResources.kstrLegacy_to_from_Unicode,
					ConvType.Legacy_to_from_Unicode));
				cboConversion.Items.Add(new CnvtrDataComboItem(AddConverterResources.kstrUnicode_to_from_Legacy,
					ConvType.Unicode_to_from_Legacy));
				cboConversion.Items.Add(new CnvtrDataComboItem(AddConverterResources.kstrUnicode_to_from_Unicode,
					ConvType.Unicode_to_from_Unicode));

				cboConversion.Items.Add(new CnvtrDataComboItem(AddConverterResources.kstrLegacy_to_Unicode,
					ConvType.Legacy_to_Unicode));
				cboConversion.Items.Add(new CnvtrDataComboItem(AddConverterResources.kstrLegacy_to_Legacy,
					ConvType.Legacy_to_Legacy));
				cboConversion.Items.Add(new CnvtrDataComboItem(AddConverterResources.kstrUnicode_to_Legacy,
					ConvType.Unicode_to_Legacy));
				cboConversion.Items.Add(new CnvtrDataComboItem(AddConverterResources.kstrUnicode_to_Unicode,
					ConvType.Unicode_to_Unicode));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the btnMapFile control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void btnMapFile_Click(object sender, System.EventArgs e)
		{
			txtMapFile.Text = txtMapFile.Text.Trim();

			if (txtMapFile.Text != string.Empty)
				ofDlg.FileName = txtMapFile.Text;

			if (ofDlg.ShowDialog(this) == DialogResult.OK)
				txtMapFile.Text = ofDlg.FileName;

			m_specs = txtMapFile.Text;
			m_fConverterChanged = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the SelectedIndexChanged event of the cboSpec control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void cboSpec_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			if (cboSpec.SelectedItem != null)
				m_specs = ((CnvtrSpecComboItem)cboSpec.SelectedItem).Specs;
			m_fConverterChanged = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Triggered when the text has been changed
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected void txtMapFile_TextChanged(object sender, System.EventArgs e)
		{
			m_specs = txtMapFile.Text;
			m_fConverterChanged = true;
			RaiseConverterFileChanged();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the TextChanged event of the txtName control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void txtName_TextChanged(object sender, System.EventArgs e)
		{
			m_fConverterChanged = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the string mapped to the type for the converter.
		/// </summary>
		/// <param name="type">The type of the encoder, e.g. CC table.</param>
		/// <returns>string for the combobox, or string.empty if type not found</returns>
		/// ------------------------------------------------------------------------------------
		private string GetConverterStringForType(ConverterType type)
		{
			for (int iConverter = 0; iConverter < cboConverter.Items.Count; iConverter++)
			{
				if (((CnvtrTypeComboItem)cboConverter.Items[iConverter]).Type == type)
					return ((CnvtrTypeComboItem)cboConverter.Items[iConverter]).ImplementType;
			}

			return string.Empty;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// In the standard usage, this is called from the selection changed event
		/// handler of the combo box that shows the installed mappings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SelectMapping(string mapname)
		{
			CheckDisposed();

			m_selectingMapping = true;

			txtName.Text = mapname;
			IEncConverter conv = m_encConverters[mapname];
			ConvType convType;
			string implType;
			EncoderInfo undefinedEncoder = null; // in case the current selection is not fully defined

			if (conv != null)
			{
				convType = conv.ConversionType;
				implType = conv.ImplementType;
			}
			else
			{
				if (m_undefinedList.TryGetValue(mapname, out undefinedEncoder))
				{
					convType = undefinedEncoder.m_fromToType;
					implType = GetConverterStringForType(undefinedEncoder.m_method);
				}
				else // Passed an invalid mapname. And yes, it does happen occasionally...
					return;
			}


			// Find and select the appropriate item in cboConversion
			bool fMatchedConvType = false;
			for (int i = 0; i < cboConversion.Items.Count; ++i)
			{
				if (((CnvtrDataComboItem)cboConversion.Items[i]).Type == convType)
				{
					fMatchedConvType = true;
					cboConversion.SelectedIndex = i;
					break;
				}
			}
			if (!fMatchedConvType)
				cboConversion.SelectedIndex = -1;

			// Use the implement type to figure which line in typeCombo to select.
			// Making a selection there enables the right specs controls.
			m_supportedConverter = false;
			for (int i = 0; i < cboConverter.Items.Count; ++i)
			{
				if (((CnvtrTypeComboItem)cboConverter.Items[i]).ImplementType == implType)
				{
					m_supportedConverter = true;
					cboConverter.SelectedIndex = i;
					break;
				}
			}
			if (!m_supportedConverter)
			{
				// Review SusannaI(JohnT): should we put up a dialog?
				// CameronB: No. For now we'll just grey out the fields
				cboConverter.SelectedIndex = -1;
			}
			// Retrieve this AFTER paths that set cboConverter.SelectedIndex, which has a side-effect
			// (indirectly through setting txtMapFile.Text) of clearing m_specs.
			m_specs = undefinedEncoder != null ? undefinedEncoder.m_fileName : conv.ConverterIdentifier;

			if (m_supportedConverter)
			{
				// Fill in whatever specs control is visible.
				switch (((CnvtrTypeComboItem)cboConverter.SelectedItem).Type)
				{
				case ConverterType.ktypeIcuConvert:
				case ConverterType.ktypeIcuTransduce:
				case ConverterType.ktypeCodePage:
					// If it's a combo, try to select the item that corresponds to the old specs.
					bool fMatchedSpecs = false;
					for (int i = 0; i < cboSpec.Items.Count; ++i)
					{
						// Note that SilEncConverters31 seems to convert specs to lower case
						// but the names we get from ICU often include upper case.
						if (((CnvtrSpecComboItem)cboSpec.Items[i]).Specs.ToUpperInvariant() == m_specs.ToUpperInvariant())
						{
							cboSpec.SelectedIndex = i;
							fMatchedSpecs = true;
							break;
						}
					}
					if (!fMatchedSpecs)
					{
						// Review SusannaI(JohnT): should we put up a dialog?
						cboSpec.SelectedIndex = -1; // nothing selected.
					}
					break;
				default:
					// If it's just a box the user can type in, just copy the old specs there.
					// This applies for TecKit, CC Table, and RegEx converters.
					txtMapFile.Text = m_specs;
					break;
				}
			}
			else
			{
				// This must be in an if-else because cboConverter.SelectedItem
				// is null if the converter is unsupported -- CameronB
				txtMapFile.Text = m_specs;
			}

			m_selectingMapping = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the converter list changed event.
		/// </summary>
		/// <param name="ea">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void OnConverterListChanged(EventArgs ea)
		{
			if (ConverterListChanged != null)
				ConverterListChanged(this, ea);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Something that happened in our control changed the list of mappings.
		/// Notify anyone who cares.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void RaiseListChanged()
		{
			OnConverterListChanged(new EventArgs());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// A new converter has been added or a modified one saved.
		/// The default handler just notifies delegates.
		/// </summary>
		/// <param name="ea"></param>
		/// ------------------------------------------------------------------------------------
		protected virtual void OnConverterSaved(EventArgs ea)
		{
			if (ConverterSaved != null)
				ConverterSaved(this, ea);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// We saved a new or modified mapping, notify anyone who cares.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void RaiseConverterSaved()
		{
			OnConverterSaved(new EventArgs());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// A converter has been modified. The default handler just notifies delegates.
		/// </summary>
		/// <param name="ea"></param>
		/// ------------------------------------------------------------------------------------
		protected virtual void OnConverterFileChanged(EventArgs ea)
		{
			if (ConverterFileChanged != null)
				ConverterFileChanged(this, ea);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The coverter has changed. Notify anyone who cares.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void RaiseConverterFileChanged()
		{
			OnConverterFileChanged(new EventArgs());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the SelectedIndexChanged event of the cboConversion control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void cboConversion_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			m_fConverterChanged = true;
		}

		private void SetReadyToGiveRegEx()
		{
			lblSpecs.Text = AddConverterResources.kstrRegExp;
			txtMapFile.Visible = true;
			txtMapFile.Width = txtName.Width; // Lengthen the field
			btnMapFile.Visible = false; // Hide the button
			cboSpec.Visible = false;
		}

		private void SetReadyToGiveMapFile()
		{
			lblSpecs.Text = AddConverterResources.kstrMapFileLabel;
			txtMapFile.Visible = true;
			txtMapFile.Width = m_mappingWidth; // Shorten the field
			btnMapFile.Visible = true; // Show the button
			cboSpec.Visible = false;
		}

		private void SetReadyToGiveSpec()
		{
			lblSpecs.Text = AddConverterResources.kstMappingName;
			txtMapFile.Visible = false;
			btnMapFile.Visible = false;
			cboSpec.Visible = true;
		}

		private void SetReadyToGiveCodePage()
		{
			lblSpecs.Text = AddConverterResources.kstrCodePageLabel;
			txtMapFile.Visible = false;
			btnMapFile.Visible = false;
			cboSpec.Visible = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the btnMore control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void btnMore_Click(object sender, EventArgs e)
		{
			Control myParentCtrl = Parent;
			while (myParentCtrl != null && !(myParentCtrl is AddCnvtrDlg))
				myParentCtrl = myParentCtrl.Parent;

			if (myParentCtrl != null)
			{
				AddCnvtrDlg myParent = myParentCtrl as AddCnvtrDlg;
				myParent.launchAddTransduceProcessorDlg();
			}
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Enable/Disable everything in the pane
		/// </summary>
		/// <param name="toState">True if enabling, False if disabling.</param>
		/// ------------------------------------------------------------------------------------
		public void EnableEntirePane(bool toState)
		{
			//Enabled = toState;
			if (lblSpecs.Enabled != toState)
			{
				lblSpecs.Enabled = toState;
				lblName.Enabled = toState;
				lblConversion.Enabled = toState;
				lblConverter.Enabled = toState;
				cboConversion.Enabled = toState;
				cboConverter.Enabled = toState;
				cboSpec.Enabled = toState;
				txtMapFile.Enabled = toState;
				txtName.Enabled = toState;
				btnMapFile.Enabled = toState;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the states.
		/// </summary>
		/// <param name="existingConvs"><c>true</c> if enabling existing converters.</param>
		/// <param name="installedConverter"><c>true</c> if encoding converter is installed.</param>
		/// ------------------------------------------------------------------------------------
		internal void SetStates(bool existingConvs, bool installedConverter)
		{
			EnableEntirePane(m_supportedConverter && existingConvs);
			btnModify.Visible = !m_supportedConverter && existingConvs && installedConverter;
			btnMore.Visible = !installedConverter;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the btnModify control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void btnModify_Click(object sender, EventArgs e)
		{
			// call the v2.2 interface to "AutoConfigure" a converter
			string strFriendlyName = ConverterName;
			IEncConverter aEC = m_encConverters[strFriendlyName];
#if AUTOCONFIGUREEX_AVAILABLE
			if (m_encConverters.AutoConfigureEx(aEC, aEC.ConversionType, ref strFriendlyName, aEC.LeftEncodingID, aEC.RightEncodingID))
#else
			if (AutoConfigureEx(aEC, aEC.ConversionType, ref strFriendlyName, aEC.LeftEncodingID, aEC.RightEncodingID))
#endif
			{
				Control myParentCtrl = Parent;
				while (myParentCtrl != null && !(myParentCtrl is AddCnvtrDlg))
					myParentCtrl = myParentCtrl.Parent;

				if (myParentCtrl != null)
				{
					AddCnvtrDlg myParent = myParentCtrl as AddCnvtrDlg;

					myParent.m_outsideDlgChangedCnvtrs = true;
					myParent.RefreshListBox();
					if (!String.IsNullOrEmpty(strFriendlyName))
						myParent.SelectedConverter = strFriendlyName;
				}
			}
		}

#if AUTOCONFIGUREEX_AVAILABLE
#else
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Automatically configures.
		/// </summary>
		/// <param name="rIEncConverter">The encoding converter.</param>
		/// <param name="eConversionTypeFilter">The conversion type filter.</param>
		/// <param name="strFriendlyName">Friendly name of the string.</param>
		/// <param name="strLhsEncodingID">.</param>
		/// <param name="strRhsEncodingID">.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private bool AutoConfigureEx
			(
			IEncConverter rIEncConverter,
			ConvType eConversionTypeFilter,
			ref string strFriendlyName,
			string strLhsEncodingID,
			string strRhsEncodingID
			)
		{

			const string strTempConverterPrefix = "Temporary converter: ";
			try
			{
				// get the configuration interface for this type
				IEncConverterConfig rConfigurator = rIEncConverter.Configurator;

				// call its Configure method to do the UI
				if (rConfigurator.Configure(m_encConverters, strFriendlyName, eConversionTypeFilter, strLhsEncodingID, strRhsEncodingID))
				{
					// if this is just a temporary converter (i.e. it isn't being added permanentally to the
					//  repository), then just make up a name so the caller can use it.
					if (!rConfigurator.IsInRepository)
					{
						DateTime dt = DateTime.Now;
						strFriendlyName = String.Format(strTempConverterPrefix + "id: '{0}', created on '{1}' at '{2}'", rConfigurator.ConverterIdentifier, dt.ToLongDateString(), dt.ToLongTimeString());

						// in this case, the Configurator didn't update the name
						rIEncConverter.Name = strFriendlyName;

						// one final thing missing: for this 'client', we have to put it into the 'this' collection
						AddToCollection(rIEncConverter, strFriendlyName);
					}
					else
					{
						// else, if it was in the repository, then it should also be (have been) updated in
						//  the collection already, so just get its name so we can return it.
						strFriendlyName = rConfigurator.ConverterFriendlyName;
					}

					return true;
				}
				else if (rConfigurator.IsInRepository && !String.IsNullOrEmpty(rConfigurator.ConverterFriendlyName))
				{
					// if the user added it to the repository and then *cancelled* it (i.e. so Configure
					//  returns false), then it *still* is in the repository and we should therefore return
					//  true.
					strFriendlyName = rConfigurator.ConverterFriendlyName;
					return true;
				}
			}
			catch
			{
#if DEBUG
				throw;
#endif
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the converter to the collection.
		/// </summary>
		/// <param name="rConverter">The converter.</param>
		/// <param name="converterName">Name of the converter.</param>
		/// ------------------------------------------------------------------------------------
		private void AddToCollection(IEncConverter rConverter, string converterName)
		{
			// now add it to the 'this' collection
			// converterName.ToLower(); // this does nothing anyway, so get rid of it

			// no sense in allowing this to be added if it already exists because it'll always
			//  be hidden.
			if (m_encConverters.ContainsKey(converterName))
			{
				//				IEncConverter ecTmp = (IEncConverter)base[converterName];
				m_encConverters.Remove(converterName); // always overwrite existing ones.
				//				Marshal.ReleaseComObject(ecTmp);
			}

			m_encConverters.Add(converterName, rConverter);
		}
#endif
	}
}

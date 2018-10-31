// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using ECInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Controls.FileDialog;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SilEncConverters40;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary />
	public class CnvtrPropertiesCtrl : UserControl
	{
		// Note: several of these controls are public in order to facilitate testing.
		// Few of them are actually required by other classes
		/// <summary></summary>
		public FwOverrideComboBox cboConversion;
		/// <summary>name of the converter</summary>
		public TextBox txtName;
		/// <summary />
		public Button btnMapFile;
		/// <summary />
		public TextBox txtMapFile;
		/// <summary />
		public FwOverrideComboBox cboConverter;
		/// <summary />
		public FwOverrideComboBox cboSpec;
		private OpenFileDialogAdapter ofDlg = new OpenFileDialogAdapter();

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
		private bool m_fConverterChanged;
		private IApp m_app;

		/// <summary>
		/// Is the loaded converter supported? True if no conv is loaded. Basically, we're just trying to
		/// find out if we should prevent the user from changing a converter that we are unable to modify.
		/// </summary>
		public bool m_supportedConverter = true;
		private bool m_selectingMapping;
		private int m_mappingWidth;

		/// <summary>List of encoding converters in the list box which are not yet defined.</summary>
		private Dictionary<string,EncoderInfo> m_undefinedList = new Dictionary<string,EncoderInfo>();

		/// <summary>The actual specs string that will be used to initialize a new converter.</summary>
		public string m_specs;
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
		private System.ComponentModel.Container components;

		/// <summary>
		/// Sets the application.
		/// </summary>
		public IApp Application
		{
			set { m_app = value; }
		}

		/// <summary>
		/// Gets or sets the converters.
		/// </summary>
		public EncConverters Converters { get; set; }

		/// <summary>
		/// Gets the current converter name.
		/// </summary>
		public string ConverterName => txtName.Text;

		/// <summary>
		/// Gets or sets a value indicating whether the encoding converter may have changed.
		/// </summary>
		public bool ConverterChanged
		{
			get { return DesignMode || m_fConverterChanged || m_undefinedList.ContainsKey(ConverterName); }
			set { m_fConverterChanged = value; }
		}

		/// <summary />
		public CnvtrPropertiesCtrl()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			m_mappingWidth = txtMapFile.Width;
		}

		/// <summary>
		/// If true, show and create only Unicode converters (both to and to/from).
		/// </summary>
		public bool OnlyUnicode
		{
			get
			{
				return m_fOnlyUnicode;
			}
			set
			{
				m_fOnlyUnicode = value;
				CnvtrPropertiesCtrl_Load(null, null);
			}
		}

		/// <summary>
		/// Sets the list of undefined encoding converters.
		/// </summary>
		internal Dictionary<string, EncoderInfo> UndefinedConverters
		{
			set { m_undefinedList = value; }
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				components?.Dispose();
				ofDlg?.Dispose();
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

		/// <summary>
		/// This provides a reasonable sort order for Code Pages supported as encodings.
		/// </summary>
		private int CompareEncInfo(System.Text.EncodingInfo x, System.Text.EncodingInfo y)
		{
			// EncodingInfo.DisplayName is marked with MonoTODO since it simply returns Name,
			// but this doesn't matter in this case.
			var c = x.DisplayName.ToLowerInvariant().CompareTo(y.DisplayName.ToLowerInvariant());
			if (c == 0)
			{
				c = x.CodePage - y.CodePage;
			}
			return c;
		}

		/// <summary>
		/// Handles the SelectedIndexChanged event of the cboConverter control.
		/// </summary>
		private void cboConverter_SelectedIndexChanged(object sender, EventArgs e)
		{
			txtMapFile.Text = "";
			var fileTypes = new List<FileFilterType>
			{
				FileFilterType.AllFiles
			};
			if (cboConverter.SelectedIndex == -1)
			{
				// Nothing is selected; this happens initially and can also happen if the user
				// selects a mapping of a type we don't recognize. We display the filename box
				// and file chooser with *.* as the file type since we have no idea what it
				// should be.
				ofDlg.DefaultExt = "";
				ofDlg.Filter = ResourceHelper.BuildFileFilter(fileTypes);
				ofDlg.Title = AddConverterResources.kstrUnspecifiedTitle;
				SetReadyToGiveMapFile();
			}
			else // based on the selected item in cboConverter, load their spec options
			{
				switch (((CnvtrTypeComboItem)cboConverter.SelectedItem).Type)
				{
					case ConverterType.ktypeRegEx:
						SetReadyToGiveRegEx();
						break;
					case ConverterType.ktypeCC:
						helpProvider1.SetHelpString(txtMapFile, AddConverterResources.kstrCCHelp);
						helpProvider1.SetShowHelp(txtMapFile, true);
						helpProvider1.SetHelpString(btnMapFile, AddConverterResources.kstrFindMapping);
						ofDlg.DefaultExt = "cct";
						fileTypes.Insert(0, FileFilterType.AllCCTable);
						ofDlg.Filter = ResourceHelper.BuildFileFilter(fileTypes);
						ofDlg.Title = AddConverterResources.kstrCCTitle;
						SetReadyToGiveMapFile();
						break;
					case ConverterType.ktypeTecKitTec:
						helpProvider1.SetHelpString(txtMapFile, AddConverterResources.kstrTecHelp);
						helpProvider1.SetShowHelp(txtMapFile, true);
						helpProvider1.SetHelpString(btnMapFile, AddConverterResources.kstrFindMapping);
						ofDlg.DefaultExt = "tec";
						fileTypes.Insert(0, FileFilterType.TECkitCompiled);
						ofDlg.Filter = ResourceHelper.BuildFileFilter(fileTypes);
						ofDlg.Title = AddConverterResources.kstrTecTitle;
						SetReadyToGiveMapFile();
						break;
					case ConverterType.ktypeTecKitMap:
						helpProvider1.SetHelpString(txtMapFile, AddConverterResources.kstrTecHelp);
						helpProvider1.SetShowHelp(txtMapFile, true);
						helpProvider1.SetHelpString(btnMapFile, AddConverterResources.kstrFindMapping);
						ofDlg.DefaultExt = "map";
						fileTypes.Insert(0, FileFilterType.TECkitMapping);
						ofDlg.Filter = ResourceHelper.BuildFileFilter(fileTypes);
						ofDlg.Title = AddConverterResources.kstrTecTitle;
						SetReadyToGiveMapFile();
						break;
					case ConverterType.ktypeCodePage:
						helpProvider1.SetHelpString(cboSpec, AddConverterResources.kstrCPHelp);
						helpProvider1.SetShowHelp(cboSpec, true);
						SetReadyToGiveCodePage();
						// Fill in combo items. This list should not change, so it's fine to do it
						// once and save it.
						cboSpec.BeginUpdate();
						cboSpec.Items.Clear();
						// we'll cheat and take advantage of our knowledge that CodePage converters
						// are built using C# System.Text.Encoding related classes.
						var encodings = new List<System.Text.EncodingInfo>();
						encodings.AddRange(System.Text.Encoding.GetEncodings());
						encodings.Sort(CompareEncInfo);
						foreach (var enc in encodings)
						{
							// TODO-Linux: EncodingInfo.DisplayName simply returns Name on Mono.
							// Need to review if this is sufficient here.
							cboSpec.Items.Add(new CnvtrSpecComboItem(string.Format(FwCoreDlgs.ksCodePageDisplay, enc.DisplayName, enc.CodePage), enc.CodePage.ToString()));
						}
						cboSpec.EndUpdate();
						break;
					case ConverterType.ktypeIcuConvert:
						helpProvider1.SetHelpString(cboSpec, AddConverterResources.kstrIcuConvHelp);
						helpProvider1.SetShowHelp(cboSpec, true);
						SetReadyToGiveSpec();
						// fill in combo items.
						cboSpec.BeginUpdate();
						cboSpec.Items.Clear();
						try
						{
							foreach (var idAndName in LCModel.Core.Text.Icu.GetConverterIdsAndNames())
							{
								if (!string.IsNullOrEmpty(idAndName.Name))
								{
									cboSpec.Items.Add(new CnvtrSpecComboItem(idAndName.Name, idAndName.Id));
								}
							}
						}
						catch (Exception ee)
						{
							Debug.Assert(m_app != null, "Bet you wish you set the Application property!");
							Debug.WriteLine(ee.Message);
							MessageBox.Show(string.Format(AddConverterDlgStrings.kstidICUErrorText, Environment.NewLine, m_app.ApplicationName),
								AddConverterDlgStrings.kstidICUErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
						}
						cboSpec.EndUpdate();
						break;
					case ConverterType.ktypeIcuTransduce:
						helpProvider1.SetHelpString(cboSpec, AddConverterResources.kstrIcuTransHelp);
						helpProvider1.SetShowHelp(cboSpec, true);
						SetReadyToGiveSpec();
						// fill in combo items.
						cboSpec.BeginUpdate();
						cboSpec.Items.Clear();
						foreach (var idAndName in LCModel.Core.Text.Icu.GetTransliteratorIdsAndNames())
						{
							cboSpec.Items.Add(new CnvtrSpecComboItem(idAndName.Name, idAndName.Id));
						}
						cboSpec.EndUpdate();
						break;
					default:
						helpProvider1.SetHelpString(cboSpec, AddConverterResources.kstrGenericHelp);
						helpProvider1.SetShowHelp(cboSpec, true);
						Debug.Assert(false, "Invalid main converter type");
						break;
				}
				m_fConverterChanged = true;
			}
			// Lets pre-populate cboConversion for them, if we aren't loading
			if (!m_selectingMapping)
			{
				var setType = ConvType.Unicode_to_Unicode;
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
					// if we are in UtU mode, pre-populate all as UtfU
					setType = ConvType.Unicode_to_from_Unicode;
				}

				SetConverterType(setType);
			}
			m_fConverterChanged = true;
		}

		/// <summary>
		/// Sets the type of the converter in the combobox for the currently selected encoding
		/// converter.
		/// </summary>
		private void SetConverterType(ConvType setConvType)
		{
			for (var i = 0; i < cboConversion.Items.Count; i++)
			{
				if (((CnvtrDataComboItem)cboConversion.Items[i]).Type == setConvType)
				{
					cboConversion.SelectedIndex = i;
					break;
				}
			}
		}

		/// <summary>
		/// Occurs on loading the Properties Tab
		/// </summary>
		public void CnvtrPropertiesCtrl_Load(object sender, EventArgs e)
		{
			// This is a fall-back if the creator does not have a converters object.
			// It is generally preferable for the creator to make one and pass it in.
			// Multiple EncConverters objects are problematical because they don't all get
			// updated when something changes.
			// JohnT: note that this ALWAYS happens at least once, because the Load event happens during
			// the main dialog's InitializeComponent method, before it sets the encConverters
			// of the control.
			if (Converters == null)
			{
				Converters = new EncConverters();
			}
			cboConverter.Items.Clear();
			cboConverter.Items.Add(new CnvtrTypeComboItem(AddConverterResources.kstrCc, ConverterType.ktypeCC, EncConverters.strTypeSILcc));
			if (!m_fOnlyUnicode)
			{
				cboConverter.Items.Add(new CnvtrTypeComboItem(AddConverterResources.kstrIcuConv, ConverterType.ktypeIcuConvert, EncConverters.strTypeSILicuConv));
			}
			cboConverter.Items.Add(new CnvtrTypeComboItem(AddConverterResources.kstrIcuTransduce, ConverterType.ktypeIcuTransduce, EncConverters.strTypeSILicuTrans));
			cboConverter.Items.Add(new CnvtrTypeComboItem(AddConverterResources.kstrTecTec, ConverterType.ktypeTecKitTec, EncConverters.strTypeSILtec));
			cboConverter.Items.Add(new CnvtrTypeComboItem(AddConverterResources.kstrTecMap, ConverterType.ktypeTecKitMap, EncConverters.strTypeSILmap));
			cboConverter.Items.Add(new CnvtrTypeComboItem(AddConverterResources.kstrRegExpIcu, ConverterType.ktypeRegEx, EncConverters.strTypeSILicuRegex));
			if (!m_fOnlyUnicode)
			{
				cboConverter.Items.Add(new CnvtrTypeComboItem(AddConverterResources.kstrCodePage, ConverterType.ktypeCodePage, EncConverters.strTypeSILcp));
			}
			cboConversion.Items.Clear();
			if (OnlyUnicode)
			{
				cboConversion.Items.Add(new CnvtrDataComboItem(AddConverterResources.kstrUnicode_to_from_Unicode, ConvType.Unicode_to_from_Unicode));
				cboConversion.Items.Add(new CnvtrDataComboItem(AddConverterResources.kstrUnicode_to_Unicode, ConvType.Unicode_to_Unicode));
			}
			else
			{
				cboConversion.Items.Add(new CnvtrDataComboItem(AddConverterResources.kstrLegacy_to_from_Legacy, ConvType.Legacy_to_from_Legacy));
				cboConversion.Items.Add(new CnvtrDataComboItem(AddConverterResources.kstrLegacy_to_from_Unicode, ConvType.Legacy_to_from_Unicode));
				cboConversion.Items.Add(new CnvtrDataComboItem(AddConverterResources.kstrUnicode_to_from_Legacy, ConvType.Unicode_to_from_Legacy));
				cboConversion.Items.Add(new CnvtrDataComboItem(AddConverterResources.kstrUnicode_to_from_Unicode, ConvType.Unicode_to_from_Unicode));
				cboConversion.Items.Add(new CnvtrDataComboItem(AddConverterResources.kstrLegacy_to_Unicode, ConvType.Legacy_to_Unicode));
				cboConversion.Items.Add(new CnvtrDataComboItem(AddConverterResources.kstrLegacy_to_Legacy, ConvType.Legacy_to_Legacy));
				cboConversion.Items.Add(new CnvtrDataComboItem(AddConverterResources.kstrUnicode_to_Legacy, ConvType.Unicode_to_Legacy));
				cboConversion.Items.Add(new CnvtrDataComboItem(AddConverterResources.kstrUnicode_to_Unicode, ConvType.Unicode_to_Unicode));
			}
		}

		/// <summary>
		/// Handles the Click event of the btnMapFile control.
		/// </summary>
		private void btnMapFile_Click(object sender, EventArgs e)
		{
			txtMapFile.Text = txtMapFile.Text.Trim();
			if (txtMapFile.Text != string.Empty)
			{
				ofDlg.FileName = txtMapFile.Text;
			}
			if (ofDlg.ShowDialog(this) == DialogResult.OK)
			{
				txtMapFile.Text = ofDlg.FileName;
			}
			m_specs = txtMapFile.Text;
			m_fConverterChanged = true;
		}

		/// <summary>
		/// Handles the SelectedIndexChanged event of the cboSpec control.
		/// </summary>
		private void cboSpec_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (cboSpec.SelectedItem != null)
			{
				m_specs = ((CnvtrSpecComboItem)cboSpec.SelectedItem).Specs;
			}
			m_fConverterChanged = true;
		}

		/// <summary>
		/// Triggered when the text has been changed
		/// </summary>
		protected void txtMapFile_TextChanged(object sender, EventArgs e)
		{
			m_specs = txtMapFile.Text;
			m_fConverterChanged = true;
			RaiseConverterFileChanged();
		}

		/// <summary>
		/// Handles the TextChanged event of the txtName control.
		/// </summary>
		private void txtName_TextChanged(object sender, EventArgs e)
		{
			m_fConverterChanged = true;
		}

		/// <summary>
		/// Gets the string mapped to the type for the converter.
		/// </summary>
		/// <param name="type">The type of the encoder, e.g. CC table.</param>
		/// <returns>string for the combobox, or string.empty if type not found</returns>
		private string GetConverterStringForType(ConverterType type)
		{
			foreach (var item in cboConverter.Items)
			{
				if (((CnvtrTypeComboItem)item).Type == type)
				{
					return ((CnvtrTypeComboItem)item).ImplementType;
				}
			}

			return string.Empty;
		}

		/// <summary>
		/// In the standard usage, this is called from the selection changed event
		/// handler of the combo box that shows the installed mappings.
		/// </summary>
		public void SelectMapping(string mapname)
		{
			m_selectingMapping = true;
			txtName.Text = mapname;
			var conv = Converters[mapname];
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
				else
				{
					// Passed an invalid mapname. And yes, it does happen occasionally...
					return;
				}
			}
			// Find and select the appropriate item in cboConversion
			var fMatchedConvType = false;
			for (var i = 0; i < cboConversion.Items.Count; ++i)
			{
				if (((CnvtrDataComboItem)cboConversion.Items[i]).Type == convType)
				{
					fMatchedConvType = true;
					cboConversion.SelectedIndex = i;
					break;
				}
			}
			if (!fMatchedConvType)
			{
				cboConversion.SelectedIndex = -1;
			}
			// Use the implement type to figure which line in typeCombo to select.
			// Making a selection there enables the right specs controls.
			m_supportedConverter = false;
			for (var i = 0; i < cboConverter.Items.Count; ++i)
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
					var fMatchedSpecs = false;
					for (var i = 0; i < cboSpec.Items.Count; ++i)
					{
						// Note that EncConverters seems to convert specs to lower case
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

		/// <summary>
		/// Raises the converter list changed event.
		/// </summary>
		protected virtual void OnConverterListChanged(EventArgs ea)
		{
			ConverterListChanged?.Invoke(this, ea);
		}

		/// <summary>
		/// Something that happened in our control changed the list of mappings.
		/// Notify anyone who cares.
		/// </summary>
		public void RaiseListChanged()
		{
			OnConverterListChanged(new EventArgs());
		}

		/// <summary>
		/// A new converter has been added or a modified one saved.
		/// The default handler just notifies delegates.
		/// </summary>
		protected virtual void OnConverterSaved(EventArgs ea)
		{
			ConverterSaved?.Invoke(this, ea);
		}

		/// <summary>
		/// A converter has been modified. The default handler just notifies delegates.
		/// </summary>
		protected virtual void OnConverterFileChanged(EventArgs ea)
		{
			ConverterFileChanged?.Invoke(this, ea);
		}

		/// <summary>
		/// The converter has changed. Notify anyone who cares.
		/// </summary>
		public void RaiseConverterFileChanged()
		{
			OnConverterFileChanged(new EventArgs());
		}

		/// <summary>
		/// Handles the SelectedIndexChanged event of the cboConversion control.
		/// </summary>
		private void cboConversion_SelectedIndexChanged(object sender, EventArgs e)
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

		/// <summary>
		/// Handles the Click event of the btnMore control.
		/// </summary>
		private void btnMore_Click(object sender, EventArgs e)
		{
			var myParentCtrl = Parent;
			while (myParentCtrl != null && !(myParentCtrl is AddCnvtrDlg))
			{
				myParentCtrl = myParentCtrl.Parent;
			}
			if (myParentCtrl != null)
			{
				var myParent = (AddCnvtrDlg)myParentCtrl;
				myParent.launchAddTransduceProcessorDlg();
			}
		}

		/// <summary>
		/// Enable/Disable everything in the pane
		/// </summary>
		/// <param name="toState">True if enabling, False if disabling.</param>
		public void EnableEntirePane(bool toState)
		{
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

		/// <summary>
		/// Sets the states.
		/// </summary>
		/// <param name="existingConvs"><c>true</c> if enabling existing converters.</param>
		/// <param name="installedConverter"><c>true</c> if encoding converter is installed.</param>
		internal void SetStates(bool existingConvs, bool installedConverter)
		{
			EnableEntirePane(m_supportedConverter && existingConvs);
			btnModify.Visible = !m_supportedConverter && existingConvs && installedConverter;
			btnMore.Visible = !installedConverter;
		}

		/// <summary>
		/// Handles the Click event of the btnModify control.
		/// </summary>
		private void btnModify_Click(object sender, EventArgs e)
		{
			// call the v2.2 interface to "AutoConfigure" a converter
			var strFriendlyName = ConverterName;
			var aEC = Converters[strFriendlyName];
#if AUTOCONFIGUREEX_AVAILABLE
			if (m_encConverters.AutoConfigureEx(aEC, aEC.ConversionType, ref strFriendlyName, aEC.LeftEncodingID, aEC.RightEncodingID))
#else
			if (AutoConfigureEx(aEC, aEC.ConversionType, ref strFriendlyName, aEC.LeftEncodingID, aEC.RightEncodingID))
#endif
			{
				var myParentCtrl = Parent;
				while (myParentCtrl != null && !(myParentCtrl is AddCnvtrDlg))
				{
					myParentCtrl = myParentCtrl.Parent;
				}
				if (myParentCtrl != null)
				{
					var myParent = (AddCnvtrDlg)myParentCtrl;
					myParent.m_outsideDlgChangedCnvtrs = true;
					myParent.RefreshListBox();
					if (!string.IsNullOrEmpty(strFriendlyName))
					{
						myParent.SelectedConverter = strFriendlyName;
					}
				}
			}
		}

#if AUTOCONFIGUREEX_AVAILABLE
#else
		/// <summary>
		/// Automatically configures.
		/// </summary>
		/// <param name="rIEncConverter">The encoding converter.</param>
		/// <param name="eConversionTypeFilter">The conversion type filter.</param>
		/// <param name="strFriendlyName">Friendly name of the string.</param>
		/// <param name="strLhsEncodingID">.</param>
		/// <param name="strRhsEncodingID">.</param>
		private bool AutoConfigureEx(IEncConverter rIEncConverter, ConvType eConversionTypeFilter, ref string strFriendlyName, string strLhsEncodingID, string strRhsEncodingID)
		{
			const string strTempConverterPrefix = "Temporary converter: ";
			try
			{
				// get the configuration interface for this type
				var rConfigurator = rIEncConverter.Configurator;
				// call its Configure method to do the UI
				if (rConfigurator.Configure(Converters, strFriendlyName, eConversionTypeFilter, strLhsEncodingID, strRhsEncodingID))
				{
					// if this is just a temporary converter (i.e. it isn't being added permanently to the
					// repository), then just make up a name so the caller can use it.
					if (!rConfigurator.IsInRepository)
					{
						var dt = DateTime.Now;
						strFriendlyName = strTempConverterPrefix + $"id: '{rConfigurator.ConverterIdentifier}', created on '{dt.ToLongDateString()}' at '{dt.ToLongTimeString()}'";

						// in this case, the Configurator didn't update the name
						rIEncConverter.Name = strFriendlyName;

						// one final thing missing: for this 'client', we have to put it into the 'this' collection
						AddToCollection(rIEncConverter, strFriendlyName);
					}
					else
					{
						// else, if it was in the repository, then it should also be (have been) updated in
						// the collection already, so just get its name so we can return it.
						strFriendlyName = rConfigurator.ConverterFriendlyName;
					}

					return true;
				}
				if (rConfigurator.IsInRepository && !string.IsNullOrEmpty(rConfigurator.ConverterFriendlyName))
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

		/// <summary>
		/// Adds the converter to the collection.
		/// </summary>
		private void AddToCollection(IEncConverter rConverter, string converterName)
		{
			// No sense in allowing this to be added if it already exists because it'll always
			// be hidden.
			if (Converters.ContainsKey(converterName))
			{
				// always overwrite existing ones.
				Converters.Remove(converterName);
			}

			Converters.Add(converterName, rConverter);
		}
#endif
	}
}
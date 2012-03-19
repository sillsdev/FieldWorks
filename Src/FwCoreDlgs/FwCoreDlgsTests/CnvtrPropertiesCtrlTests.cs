using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Windows.Forms;
using NUnit.Framework;

using ECInterfaces;
using SilEncConverters40;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.FieldWorks.Test.TestUtils;
using SIL.Utils;

namespace AddConverterDlgTests
{
	#region Dummy CnvtrPropertiesCtrl & AddCnvtrDlg
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// <remarks>Though most of this code is located in AddCnvtrDlg, because the buttons
	/// and listbox are there, we're testing it in here because this Properties Tab is
	/// where you're expected to add/configure the Converters</remarks>
	/// ----------------------------------------------------------------------------------------
	public class DummyAddCnvtrDlg : AddCnvtrDlg
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DummyAddCnvtrDlg()
			: base(null, null, null, null, false)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override this for testing without UI
		/// </summary>
		/// <param name="sMessage"></param>
		/// <param name="sTitle"></param>
		/// <param name="buttons"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected override DialogResult ShowMessage(string sMessage, string sTitle,
			MessageBoxButtons buttons)
		{
			return DialogResult.OK;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the last error message displayed
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ErrorMsg
		{
			get { return ErrorMsg; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the remove button.
		/// </summary>
		/// <value>The remove button.</value>
		/// ------------------------------------------------------------------------------------
		public Button btnAdd
		{
			get
			{
				CheckDisposed();
				return btnAdd;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the remove button.
		/// </summary>
		/// <value>The remove button.</value>
		/// ------------------------------------------------------------------------------------
		public Button btnCopy
		{
			get
			{
				CheckDisposed();
				return btnCopy;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the add save button.
		/// </summary>
		/// <value>The add save button.</value>
		/// ------------------------------------------------------------------------------------
		public Button btnDelete
		{
			get
			{
				CheckDisposed();
				return btnDelete;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Helper method to enable us to set a mapping file for testing purposes
		/// </summary>
		/// <param name="sMapping"></param>
		/// ------------------------------------------------------------------------------------
		public void SetMappingFile(string sMapping)
		{
			CheckDisposed();

			m_cnvtrPropertiesCtrl.txtMapFile.Text = sMapping;
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyCnvtrPropertiesCtrl : CnvtrPropertiesCtrl
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Helper method to enable us to set a mapping file for testing purposes
		/// </summary>
		/// <param name="sMapping"></param>
		/// ------------------------------------------------------------------------------------
		public void SetMappingFile(string sMapping)
		{
			CheckDisposed();

			txtMapFile.Text = sMapping;
			txtMapFile_TextChanged(null, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Helper method to enable us to set cboConverter for testing purposes
		/// </summary>
		/// <param name="setTo">Type that we are setting the Converter Type combo to</param>
		/// ------------------------------------------------------------------------------------
		public void setCboConverter(ConverterType setTo)
		{
			for (int i = 0; i < cboConverter.Items.Count; i++)
			{
				if (((CnvtrTypeComboItem)cboConverter.Items[i]).Type == setTo)
				{
					cboConverter.SelectedIndex = i;
					break;
				}
				else
					cboConverter.SelectedIndex = 0;
			}
		}
	}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Class for testing the CnvtrPropertiesCtrl
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="Unit test. m_myDlg gets disposed in FixtureTearDown method.")]
	public class CnvtrPropertiesControlTests : BaseTest
	{
		private DummyAddCnvtrDlg m_myDlg;
		private DummyCnvtrPropertiesCtrl m_myCtrl;
		private string m_ccFileName;
		private string m_mapFileName;
		private string m_bogusFileName;

		#region Setup & Teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates and loads a set of dummy converters to test the
		/// <see cref="CnvtrPropertiesCtrl"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			var encConverters = new SilEncConverters40.EncConverters();
			string[] ccFileContents = {"'c' > 'C'"};
			m_ccFileName = CreateTempFile(ccFileContents, "cct");
			encConverters.AddConversionMap("ZZZTestCC", m_ccFileName,
				ConvType.Legacy_to_Unicode, "SIL.cc", "", "",
				ProcessTypeFlags.UnicodeEncodingConversion);

			string[] mapFileContents = {
										   "EncodingName	'ZZZText'",
										   "DescriptiveName	'Silly test file'",
										   "ByteDefault		'?'",
										   "UniDefault		replacement_character",
										   "0x80	<>	euro_sign"
									   };
			m_mapFileName = CreateTempFile(mapFileContents, "map");
			encConverters.AddConversionMap("ZZZTestMap", m_mapFileName,
				ConvType.Legacy_to_from_Unicode, "SIL.map", "", "",
				ProcessTypeFlags.UnicodeEncodingConversion);

			// TODO: Should test a legitimate compiled TecKit file by embedding a zipped
			// up one in the resources for testing purposes.

			// This is a randomly chosen ICU converter. The test may break when we reduce the set of
			// ICU converters we ship.
			encConverters.AddConversionMap("ZZZTestICU", "ISO-8859-1",
				ConvType.Legacy_to_from_Unicode, "ICU.conv", "", "",
				ProcessTypeFlags.ICUConverter);

			// Add a 1-step compound converter, which won't be any of the types our dialog
			// recognizes for now.
			encConverters.AddCompoundConverterStep("ZZZTestCompound", "ZZZTestCC", true,
				NormalizeFlags.None);

			encConverters.Remove("BogusTecKitFile");	// shouldn't exist, but...

			m_myDlg = new DummyAddCnvtrDlg();
			m_myCtrl = new DummyCnvtrPropertiesCtrl();
			m_myCtrl.Converters = encConverters;
			// Load all the mappings after the dummy mappings are added, so the Converter
			// Mapping File combo box won't contain obsolete versions of the mappings referring
			// to old temp files from a previous run of the tests.q
			m_myCtrl.CnvtrPropertiesCtrl_Load(null, null);
#if !QUIET
			Console.WriteLine("Installed mappings after test setup:");
			foreach (var name in encConverters.Mappings)
			{
				var conv = encConverters[name];
				Console.WriteLine("    {0} ({1})", name, conv == null ? "null" : conv.GetType().ToString());
			}
#endif
		}

		/// <summary>
		/// Clean up after running all the tests.
		/// </summary>
		public override void FixtureTeardown()
		{
			SilEncConverters40.EncConverters encConverters;
			// Dispose managed resources here.
			if (m_myCtrl != null)
			{
				encConverters = m_myCtrl.Converters;
				m_myCtrl.Dispose();
				m_myCtrl = null;
			}
			else
			{
				encConverters = new SilEncConverters40.EncConverters();
			}
			if (m_myDlg != null)
			{
				m_myDlg.Dispose();
				m_myDlg = null;
			}
			// Delete any temp files that have been created.
			if (!String.IsNullOrEmpty(m_ccFileName))
			{
				File.Delete(m_ccFileName);
				m_ccFileName = null;
			}
			if (!String.IsNullOrEmpty(m_mapFileName))
			{
				File.Delete(m_mapFileName);
				m_mapFileName = null;
			}
			if (!String.IsNullOrEmpty(m_bogusFileName))
			{
				File.Delete(m_bogusFileName);
				m_mapFileName = null;
			}
			// Remove any encoding converters that were added for the tests.
			encConverters.Remove("ZZZTestCC");
			encConverters.Remove("ZZZText");
			encConverters.Remove("ZZZTestMap");
			encConverters.Remove("ZZZTestICU");
			encConverters.Remove("ZZZTestCompound");
			encConverters.Remove("BogusTecKitFile");	// shouldn't exist, but...
#if !QUIET
			Console.WriteLine("Installed mappings after test teardown:");
			foreach (var name in encConverters.Mappings)
				Console.WriteLine("    {0}", name);
#endif
		}
		#endregion

		#region Tests
		// Admittedly, we certainly could test for many more scenarios. Some could be the
		// Autosave, Add, Copy, and Delete functions, but we really don't need to.
		// (The majority of these functionalities will be caught by SILEncConv tests)

		// Other items that we could test for (in AddCnvtrDlg):
		// btnAdd - field contents (mostly empty, and no install)
		// btnCopy - field contents or existing converter
		// btnDelete - non-existing converter
		// autosave - detects name field changes
		// autosave - detects converter changes
		// autosave - detects spec changes
		// autosave - detects map file changes
		// autosave - detects name field changes
		// autosave - warning for invalid mapping file
		// copy - overwrite warning

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// CC Mapping table test
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SelectMapping_CCMappingTable()
		{
			m_myCtrl.SelectMapping("ZZZTestCC");
			Assert.IsTrue(m_myCtrl.cboConverter.SelectedItem is CnvtrTypeComboItem, "Should be able to select ZZZTestCC");
			Assert.AreEqual(ConverterType.ktypeCC, ((CnvtrTypeComboItem)m_myCtrl.cboConverter.SelectedItem).Type, "Selected converter should be CC for ZZZTestCC");
			Assert.IsFalse(m_myCtrl.cboSpec.Visible, "Converter specifier ComboBox should not be visible for ZZZTestCC");
			Assert.IsTrue(m_myCtrl.btnMapFile.Visible, "Map file chooser Button should be visible for ZZZTestCC");
			Assert.IsTrue(m_myCtrl.txtMapFile.Visible, "Map file TextBox should be visible for ZZZTestCC");
			Assert.AreEqual(m_ccFileName, m_myCtrl.txtMapFile.Text, "TextBox and member variable should have same value for ZZZTestCC");
			Assert.IsTrue(m_myCtrl.cboConversion.SelectedItem is CnvtrDataComboItem, "Conversion type should be selected for ZZZTestCC");
			Assert.AreEqual(ConvType.Legacy_to_Unicode, ((CnvtrDataComboItem)m_myCtrl.cboConversion.SelectedItem).Type, "Conversion type should be Legacy_to_Unicode for ZZZTestCC");
			Assert.AreEqual("ZZZTestCC", m_myCtrl.txtName.Text, "Displayed converter should be ZZZTestCC");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test a TecKit mapping file
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SelectMapping_TecKitMapTable()
		{
			m_myCtrl.SelectMapping("ZZZTestMap");
			Assert.IsTrue(m_myCtrl.cboConverter.SelectedItem is CnvtrTypeComboItem, "Should be able to select ZZZTestMap");
			Assert.AreEqual(ConverterType.ktypeTecKitMap, ((CnvtrTypeComboItem)m_myCtrl.cboConverter.SelectedItem).Type, "Selected converter should be TecKit/Map for ZZZTestMap");
			Assert.IsFalse(m_myCtrl.cboSpec.Visible, "Converter specifier ComboBox should not be visible for ZZZTestMap");
			Assert.IsTrue(m_myCtrl.btnMapFile.Visible, "Map file chooser Button should be visible for ZZZTestMap");
			Assert.IsTrue(m_myCtrl.txtMapFile.Visible, "Map file TextBox should be visible for ZZZTestMap");
			Assert.AreEqual(m_mapFileName, m_myCtrl.txtMapFile.Text, "TextBox and member variable should have same value for ZZZTestMap");
			Assert.IsTrue(m_myCtrl.cboConversion.SelectedItem is CnvtrDataComboItem, "Conversion type should be selected for ZZZTestMap");
			Assert.AreEqual(ConvType.Legacy_to_from_Unicode,
				((CnvtrDataComboItem)m_myCtrl.cboConversion.SelectedItem).Type, "Conversion type should be Legacy_to_from_Unicode for ZZZTestMap");
			Assert.AreEqual("ZZZTestMap", m_myCtrl.txtName.Text, "Displayed converter should be ZZZTestMap");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test a conversion using a standard ICU mapping
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SelectMapping_IcuConversion()
		{
			m_myCtrl.SelectMapping("ZZZTestICU");
			Assert.IsTrue(m_myCtrl.cboConverter.SelectedItem is CnvtrTypeComboItem, "Should be able to select ZZZTestICU");
			Assert.AreEqual(ConverterType.ktypeIcuConvert, ((CnvtrTypeComboItem)m_myCtrl.cboConverter.SelectedItem).Type, "Selected item should be ICU converter for ZZZTestICU");
			Assert.IsTrue(m_myCtrl.cboSpec.Visible, "ComboBox for Specifying Converter should be visible for ZZZTestICU");
			Assert.IsFalse(m_myCtrl.btnMapFile.Visible, "Button for selecting map file should not be visible for ZZZTestICU");
			Assert.IsFalse(m_myCtrl.txtMapFile.Visible, "TextBox for displaying map file should not be visible for ZZZTestICU");
			Assert.IsTrue(m_myCtrl.cboSpec.SelectedItem is CnvtrSpecComboItem, "A Converter spec should be selected for ZZZTestICU");
			// This is a randomly chosen ICU converter. The test may break when we reduce the set of
			// ICU converters we ship.
			Assert.AreEqual("ISO-8859-1", ((CnvtrSpecComboItem)m_myCtrl.cboSpec.SelectedItem).Specs, "Selected spec should be ISO-8859-1 for ZZZTestICU");
			Assert.IsTrue(m_myCtrl.cboConversion.SelectedItem is CnvtrDataComboItem, "Conversion type should be selected for ZZZTestICU");
			Assert.AreEqual(ConvType.Legacy_to_from_Unicode,
				((CnvtrDataComboItem)m_myCtrl.cboConversion.SelectedItem).Type, "Selected Conversion type should be Legacy_to_from_Unicode for IZZZTestICU");
			Assert.AreEqual("ZZZTestICU", m_myCtrl.txtName.Text, "Displayed converter should be ZZZTestICU");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test a compound mapping file
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SelectMapping_Compound()
		{
			// This is a type we don't recognize.
			m_myCtrl.SelectMapping("ZZZTestCompound");
			Assert.AreEqual(-1, m_myCtrl.cboConverter.SelectedIndex, "Should NOT be able to select ZZZTestCompound");
			Assert.IsFalse(m_myCtrl.cboSpec.Visible, "ComboBox for Specifying Converter should not be visible for ZZZTestCompound");
			Assert.IsTrue(m_myCtrl.btnMapFile.Visible, "Button for selecting map file should not be visible for ZZZTestCompound");
			Assert.IsTrue(m_myCtrl.txtMapFile.Visible, "TextBox for displaying map file should not be visible for ZZZTestCompound");
			Assert.AreEqual("ZZZTestCompound", m_myCtrl.txtName.Text, "Displayed converter should be ZZZTestCompound");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test to make sure that we are loading the correct items for each choice.
		/// NOTE: Testing every option could take a very long time, so lets just test
		/// the number of items that were loaded.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SelectMapping_CboSpecListedItems()
		{
			// It doesn't really matter which one we've loaded, just load one
			m_myCtrl.SelectMapping("ZZZTestMap");

			m_myCtrl.setCboConverter(ConverterType.ktypeCC);
			Assert.IsFalse(m_myCtrl.cboSpec.Visible);
			Assert.IsTrue(m_myCtrl.btnMapFile.Visible);
			Assert.IsTrue(m_myCtrl.txtMapFile.Visible);

			m_myCtrl.setCboConverter(ConverterType.ktypeIcuConvert); // produces 27, but may change slightly in future versions
			Assert.IsTrue(20 < m_myCtrl.cboSpec.Items.Count);
			Assert.IsTrue(m_myCtrl.cboSpec.Visible);
			Assert.IsFalse(m_myCtrl.btnMapFile.Visible);
			Assert.IsFalse(m_myCtrl.txtMapFile.Visible);

			m_myCtrl.setCboConverter(ConverterType.ktypeIcuTransduce); // produces 183, but may change slightly in future versions
			Assert.IsTrue(170 < m_myCtrl.cboSpec.Items.Count);
			Assert.IsTrue(m_myCtrl.cboSpec.Visible);
			Assert.IsFalse(m_myCtrl.btnMapFile.Visible);
			Assert.IsFalse(m_myCtrl.txtMapFile.Visible);

			m_myCtrl.setCboConverter(ConverterType.ktypeTecKitTec);
			Assert.IsFalse(m_myCtrl.cboSpec.Visible);
			Assert.IsTrue(m_myCtrl.btnMapFile.Visible);
			Assert.IsTrue(m_myCtrl.txtMapFile.Visible);

			m_myCtrl.setCboConverter(ConverterType.ktypeTecKitMap);
			Assert.IsFalse(m_myCtrl.cboSpec.Visible);
			Assert.IsTrue(m_myCtrl.btnMapFile.Visible);
			Assert.IsTrue(m_myCtrl.txtMapFile.Visible);

			m_myCtrl.setCboConverter(ConverterType.ktypeCodePage); // produces 148 on Vista, and 50-some odd on XP
			Assert.IsTrue(25 < m_myCtrl.cboSpec.Items.Count);
			Assert.IsTrue(m_myCtrl.cboSpec.Visible);
			Assert.IsFalse(m_myCtrl.btnMapFile.Visible);
			Assert.IsFalse(m_myCtrl.txtMapFile.Visible);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test to make sure that we are prepopulating cboConversion correctly.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SelectMapping_PrepopulateCboConversion()
		{
			// It doesn't really matter which one we've loaded
			m_myCtrl.SelectMapping("ZZZTestMap");

			// During the testing portion below, we will test two things:
			// 1) That the cboConverter selected an item properly
			// 2) That cboConversion was prepopulated properly

			m_myCtrl.setCboConverter(ConverterType.ktypeCC);
			Assert.AreEqual(ConverterType.ktypeCC,
				((CnvtrTypeComboItem)m_myCtrl.cboConverter.SelectedItem).Type, "Selected CC type properly");
			Assert.AreEqual(ConvType.Legacy_to_Unicode,
				((CnvtrDataComboItem)m_myCtrl.cboConversion.SelectedItem).Type, "CC type defaults to Legacy_to_Unicode");

			m_myCtrl.setCboConverter(ConverterType.ktypeIcuConvert);
			Assert.AreEqual(ConverterType.ktypeIcuConvert,
				((CnvtrTypeComboItem)m_myCtrl.cboConverter.SelectedItem).Type, "Selected ICU Converter type properly");
			Assert.AreEqual(ConvType.Legacy_to_from_Unicode,
				((CnvtrDataComboItem)m_myCtrl.cboConversion.SelectedItem).Type, "ICU Converter type defaults to Legacy_to_from_Unicode");

			m_myCtrl.setCboConverter(ConverterType.ktypeIcuTransduce);
			Assert.AreEqual(ConverterType.ktypeIcuTransduce,
				((CnvtrTypeComboItem)m_myCtrl.cboConverter.SelectedItem).Type, "Selected ICU Transducer type properly");
			Assert.AreEqual(ConvType.Unicode_to_from_Unicode,
				((CnvtrDataComboItem)m_myCtrl.cboConversion.SelectedItem).Type, "ICU Transducer type defaults to Legacy_to_from_Unicode");

			m_myCtrl.setCboConverter(ConverterType.ktypeTecKitTec);
			Assert.AreEqual(ConverterType.ktypeTecKitTec,
				((CnvtrTypeComboItem)m_myCtrl.cboConverter.SelectedItem).Type, "Selected TecKit/Tec type properly");
			Assert.AreEqual(ConvType.Legacy_to_from_Unicode,
				((CnvtrDataComboItem)m_myCtrl.cboConversion.SelectedItem).Type, "TecKit/Tec type defaults to Legacy_to_from_Unicode");

			m_myCtrl.setCboConverter(ConverterType.ktypeTecKitMap);
			Assert.AreEqual(ConverterType.ktypeTecKitMap,
				((CnvtrTypeComboItem)m_myCtrl.cboConverter.SelectedItem).Type, "Selected TecKit/Map type properly");
			Assert.AreEqual(ConvType.Legacy_to_from_Unicode,
				((CnvtrDataComboItem)m_myCtrl.cboConversion.SelectedItem).Type, "TecKit/Map type defaults to Legacy_to_from_Unicode");

			m_myCtrl.setCboConverter(ConverterType.ktypeCodePage);
			Assert.AreEqual(ConverterType.ktypeCodePage,
				((CnvtrTypeComboItem)m_myCtrl.cboConverter.SelectedItem).Type, "Selected CodePage type properly");
			Assert.AreEqual(ConvType.Legacy_to_from_Unicode,
				((CnvtrDataComboItem)m_myCtrl.cboConversion.SelectedItem).Type, "CodePage type defaults to Legacy_to_from_Unicode");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test a bogus compiled TecKit file. Should fail with nice error message, not crash.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SelectMapping_BogusCompiledTecKitFile()
		{
			m_bogusFileName = CreateTempFile(new string[] { "bogus contents" }, "tec");

			// This is a type we don't recognize.
			m_myDlg.m_cnvtrPropertiesCtrl.txtName.Text = "BogusTecKitFile";

			int i;
			for (i = 0; i < m_myDlg.m_cnvtrPropertiesCtrl.cboConverter.Items.Count; ++i)
			{
				if (((CnvtrTypeComboItem)m_myDlg.m_cnvtrPropertiesCtrl.cboConverter.Items[i]).Type == ConverterType.ktypeTecKitTec)
				{
					m_myDlg.m_cnvtrPropertiesCtrl.cboConverter.SelectedIndex = i;
					break;
				}
			}
			Assert.IsTrue(i < m_myDlg.m_cnvtrPropertiesCtrl.cboConverter.Items.Count, "Should find a TecKitTec type converter listed.");
			for (i = 0; i < m_myDlg.m_cnvtrPropertiesCtrl.cboConversion.Items.Count; ++i)
			{
				if (((CnvtrDataComboItem)m_myDlg.m_cnvtrPropertiesCtrl.cboConversion.Items[i]).Type == ConvType.Legacy_to_Unicode)
				{
					m_myDlg.m_cnvtrPropertiesCtrl.cboConversion.SelectedIndex = i;
					break;
				}
			}
			Assert.IsTrue(i < m_myDlg.m_cnvtrPropertiesCtrl.cboConversion.Items.Count, "Should find a Legacy_to_Unicode conversion listed.");

			m_myDlg.SetMappingFile(m_bogusFileName);

			Assert.IsFalse(m_myDlg.InstallConverter(), "Should not be able to install bogus compiled TecKit file.");
			// This may not be testing what we want it to test...
			// Might want make an assert on the error message that is produced!
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests for a "successful" save when nothing is changed
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AutoSave_ValidButUnchanged()
		{
			m_myDlg.m_cnvtrPropertiesCtrl.SelectMapping("ZZZTestCC");
			m_myDlg.SetUnchanged();
			Assert.IsTrue(m_myDlg.AutoSave());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests for a successful save when converter is valid
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("Fails about half of the time -- CameronB")]
		public void AutoSave_ValidContents()
		{
			m_myDlg.m_cnvtrPropertiesCtrl.SelectMapping("ZZZTestICU");
			m_myDlg.SetUnchanged();
			m_myDlg.m_cnvtrPropertiesCtrl.txtName.Text = "ZZZTestRenamedICU";
			Assert.IsTrue(m_myDlg.AutoSave());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests for failure when converter cannot save successfully
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("Fails: producing object null message")]
		public void AutoSave_InvalidContents()
		{
			m_myDlg.m_cnvtrPropertiesCtrl.SelectMapping("ZZZTestMap");
			m_myDlg.SetUnchanged();
			m_myDlg.m_cnvtrPropertiesCtrl.cboSpec.Text = "NotValid";
			Assert.IsFalse(m_myDlg.AutoSave());
		}
		#endregion

		/// <summary>
		/// Create a temporary file with the given data and file type extension.
		/// </summary>
		string CreateTempFile(string[] data, string filetype)
		{
			string fileTmp = Path.GetTempFileName();	// actually creates file on disk.
			string filename = fileTmp;
			if (!String.IsNullOrEmpty(filetype))
			{
				filename = Path.ChangeExtension(fileTmp, filetype);
				File.Move(fileTmp, filename);
			}
			using (var file = new StreamWriter(filename, false, System.Text.Encoding.ASCII))
			{
				foreach (var line in data)
					file.WriteLine(line);
			}
			return filename;
		}
	}
}

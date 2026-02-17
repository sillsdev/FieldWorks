// Copyright (c) 2015-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using System.Windows.Forms;
using NUnit.Framework;

using ECInterfaces;
using SilEncConverters40;
using SIL.FieldWorks.FwCoreDlgs;

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
	public class CnvtrPropertiesControlTests
	{
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
		[OneTimeSetUp]
		public void FixtureSetup()
		{
			string[] ccFileContents = {"'c' > 'C'"};
			m_ccFileName = CreateTempFile(ccFileContents, "cct");

			string[] mapFileContents = {
										   "EncodingName	'ZZZUnitTestText'",
										   "DescriptiveName	'Silly test file'",
										   "ByteDefault		'?'",
										   "UniDefault		replacement_character",
										   "0x80	<>	euro_sign"
									   };
			m_mapFileName = CreateTempFile(mapFileContents, "map");
			m_myCtrl = new DummyCnvtrPropertiesCtrl();
			m_myCtrl.UndefinedConverters = new System.Collections.Generic.Dictionary<string, EncoderInfo>
			{
				{ "ZZZUnitTestCC", new EncoderInfo("ZZZUnitTestCC", ConverterType.ktypeCC, m_ccFileName, ConvType.Legacy_to_Unicode) },
				{ "ZZZUnitTestMap", new EncoderInfo("ZZZUnitTestMap", ConverterType.ktypeTecKitMap, m_mapFileName, ConvType.Legacy_to_from_Unicode) },
				{ "ZZZUnitTestICU", new EncoderInfo("ZZZUnitTestICU", ConverterType.ktypeIcuConvert, "ISO-8859-1", ConvType.Legacy_to_from_Unicode) },
				// Use an unknown ConverterType value so it won't match any known ImplementType.
				{ "ZZZUnitTestCompound", new EncoderInfo("ZZZUnitTestCompound", (ConverterType)9999, "", ConvType.Legacy_to_from_Unicode) },
			};
			// Populate UI comboboxes without touching EncConverters.
			m_myCtrl.CnvtrPropertiesCtrl_Load(null, null);
		}

		/// <summary>
		/// Clean up after running all the tests.
		/// </summary>
		[OneTimeTearDown]
		public void FixtureTeardown()
		{
			if (m_myCtrl != null)
			{
				m_myCtrl.Dispose();
				m_myCtrl = null;
			}

			try
			{
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
					m_bogusFileName = null;
				}
			}
			catch
			{
				// for some reason deleting the temporary files occasionally fails - not sure
				// why. If this happens we just ignore it and continue.
			}
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
			m_myCtrl.SelectMapping("ZZZUnitTestCC");
			Assert.That(m_myCtrl.cboConverter.SelectedItem is CnvtrTypeComboItem, Is.True, "Should be able to select ZZZUnitTestCC");
			Assert.That(((CnvtrTypeComboItem)m_myCtrl.cboConverter.SelectedItem).Type, Is.EqualTo(ConverterType.ktypeCC), "Selected converter should be CC for ZZZUnitTestCC");
			Assert.That(m_myCtrl.cboSpec.Visible, Is.False, "Converter specifier ComboBox should not be visible for ZZZUnitTestCC");
			Assert.That(m_myCtrl.btnMapFile.Visible, Is.True, "Map file chooser Button should be visible for ZZZUnitTestCC");
			Assert.That(m_myCtrl.txtMapFile.Visible, Is.True, "Map file TextBox should be visible for ZZZUnitTestCC");
			Assert.That(m_myCtrl.txtMapFile.Text, Is.EqualTo(m_ccFileName), "TextBox and member variable should have same value for ZZZUnitTestCC");
			Assert.That(m_myCtrl.cboConversion.SelectedItem is CnvtrDataComboItem, Is.True, "Conversion type should be selected for ZZZUnitTestCC");
			Assert.That(((CnvtrDataComboItem)m_myCtrl.cboConversion.SelectedItem).Type, Is.EqualTo(ConvType.Legacy_to_Unicode), "Conversion type should be Legacy_to_Unicode for ZZZUnitTestCC");
			Assert.That(m_myCtrl.txtName.Text, Is.EqualTo("ZZZUnitTestCC"), "Displayed converter should be ZZZUnitTestCC");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test a TecKit mapping file
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SelectMapping_TecKitMapTable()
		{
			m_myCtrl.SelectMapping("ZZZUnitTestMap");
			Assert.That(m_myCtrl.cboConverter.SelectedItem is CnvtrTypeComboItem, Is.True, "Should be able to select ZZZUnitTestMap");
			Assert.That(((CnvtrTypeComboItem)m_myCtrl.cboConverter.SelectedItem).Type, Is.EqualTo(ConverterType.ktypeTecKitMap), "Selected converter should be TecKit/Map for ZZZUnitTestMap");
			Assert.That(m_myCtrl.cboSpec.Visible, Is.False, "Converter specifier ComboBox should not be visible for ZZZUnitTestMap");
			Assert.That(m_myCtrl.btnMapFile.Visible, Is.True, "Map file chooser Button should be visible for ZZZUnitTestMap");
			Assert.That(m_myCtrl.txtMapFile.Visible, Is.True, "Map file TextBox should be visible for ZZZUnitTestMap");
			Assert.That(m_myCtrl.txtMapFile.Text, Is.EqualTo(m_mapFileName), "TextBox and member variable should have same value for ZZZUnitTestMap");
			Assert.That(m_myCtrl.cboConversion.SelectedItem is CnvtrDataComboItem, Is.True, "Conversion type should be selected for ZZZUnitTestMap");
			Assert.That(((CnvtrDataComboItem)m_myCtrl.cboConversion.SelectedItem).Type, Is.EqualTo(ConvType.Legacy_to_from_Unicode), "Conversion type should be Legacy_to_from_Unicode for ZZZUnitTestMap");
			Assert.That(m_myCtrl.txtName.Text, Is.EqualTo("ZZZUnitTestMap"), "Displayed converter should be ZZZUnitTestMap");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test a conversion using a standard ICU mapping
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SelectMapping_IcuConversion()
		{
			m_myCtrl.SelectMapping("ZZZUnitTestICU");
			Assert.That(m_myCtrl.cboConverter.SelectedItem is CnvtrTypeComboItem, Is.True, "Should be able to select ZZZUnitTestICU");
			Assert.That(((CnvtrTypeComboItem)m_myCtrl.cboConverter.SelectedItem).Type, Is.EqualTo(ConverterType.ktypeIcuConvert), "Selected item should be ICU converter for ZZZUnitTestICU");
			Assert.That(m_myCtrl.cboSpec.Visible, Is.True, "ComboBox for Specifying Converter should be visible for ZZZUnitTestICU");
			Assert.That(m_myCtrl.btnMapFile.Visible, Is.False, "Button for selecting map file should not be visible for ZZZUnitTestICU");
			Assert.That(m_myCtrl.txtMapFile.Visible, Is.False, "TextBox for displaying map file should not be visible for ZZZUnitTestICU");
			Assert.That(m_myCtrl.cboSpec.SelectedItem is CnvtrSpecComboItem, Is.True, "A Converter spec should be selected for ZZZUnitTestICU");
			// This is a randomly chosen ICU converter. The test may break when we reduce the set of
			// ICU converters we ship.
			Assert.That(((CnvtrSpecComboItem)m_myCtrl.cboSpec.SelectedItem).Specs, Is.EqualTo("ISO-8859-1"), "Selected spec should be ISO-8859-1 for ZZZUnitTestICU");
			Assert.That(m_myCtrl.cboConversion.SelectedItem is CnvtrDataComboItem, Is.True, "Conversion type should be selected for ZZZUnitTestICU");
			Assert.That(((CnvtrDataComboItem)m_myCtrl.cboConversion.SelectedItem).Type, Is.EqualTo(ConvType.Legacy_to_from_Unicode), "Selected Conversion type should match the value stored for ZZZUnitTestICU");
			Assert.That(m_myCtrl.txtName.Text, Is.EqualTo("ZZZUnitTestICU"), "Displayed converter should be ZZZUnitTestICU");
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
			m_myCtrl.SelectMapping("ZZZUnitTestCompound");
			Assert.That(m_myCtrl.cboConverter.SelectedIndex, Is.EqualTo(-1), "Should NOT be able to select ZZZUnitTestCompound");
			Assert.That(m_myCtrl.cboSpec.Visible, Is.False, "ComboBox for Specifying Converter should not be visible for ZZZUnitTestCompound");
			Assert.That(m_myCtrl.btnMapFile.Visible, Is.True, "Button for selecting map file should be visible for ZZZUnitTestCompound");
			Assert.That(m_myCtrl.txtMapFile.Visible, Is.True, "TextBox for displaying map file should be visible for ZZZUnitTestCompound");
			Assert.That(m_myCtrl.txtName.Text, Is.EqualTo("ZZZUnitTestCompound"), "Displayed converter should be ZZZUnitTestCompound");
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
			m_myCtrl.SelectMapping("ZZZUnitTestMap");

			m_myCtrl.setCboConverter(ConverterType.ktypeCC);
			Assert.That(m_myCtrl.cboSpec.Visible, Is.False);
			Assert.That(m_myCtrl.btnMapFile.Visible, Is.True);
			Assert.That(m_myCtrl.txtMapFile.Visible, Is.True);

			m_myCtrl.setCboConverter(ConverterType.ktypeIcuConvert); // produces 27, but may change slightly in future versions
			Assert.That(20 < m_myCtrl.cboSpec.Items.Count, Is.True);
			Assert.That(m_myCtrl.cboSpec.Visible, Is.True);
			Assert.That(m_myCtrl.btnMapFile.Visible, Is.False);
			Assert.That(m_myCtrl.txtMapFile.Visible, Is.False);

			m_myCtrl.setCboConverter(ConverterType.ktypeIcuTransduce); // produces 183, but may change slightly in future versions
			Assert.That(170 < m_myCtrl.cboSpec.Items.Count, Is.True);
			Assert.That(m_myCtrl.cboSpec.Visible, Is.True);
			Assert.That(m_myCtrl.btnMapFile.Visible, Is.False);
			Assert.That(m_myCtrl.txtMapFile.Visible, Is.False);

			m_myCtrl.setCboConverter(ConverterType.ktypeTecKitTec);
			Assert.That(m_myCtrl.cboSpec.Visible, Is.False);
			Assert.That(m_myCtrl.btnMapFile.Visible, Is.True);
			Assert.That(m_myCtrl.txtMapFile.Visible, Is.True);

			m_myCtrl.setCboConverter(ConverterType.ktypeTecKitMap);
			Assert.That(m_myCtrl.cboSpec.Visible, Is.False);
			Assert.That(m_myCtrl.btnMapFile.Visible, Is.True);
			Assert.That(m_myCtrl.txtMapFile.Visible, Is.True);

			m_myCtrl.setCboConverter(ConverterType.ktypeCodePage); // produces 148 on Vista, and 50-some odd on XP
			Assert.That(25 < m_myCtrl.cboSpec.Items.Count, Is.True);
			Assert.That(m_myCtrl.cboSpec.Visible, Is.True);
			Assert.That(m_myCtrl.btnMapFile.Visible, Is.False);
			Assert.That(m_myCtrl.txtMapFile.Visible, Is.False);
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
			m_myCtrl.SelectMapping("ZZZUnitTestMap");

			// During the testing portion below, we will test two things:
			// 1) That the cboConverter selected an item properly
			// 2) That cboConversion was prepopulated properly

			m_myCtrl.setCboConverter(ConverterType.ktypeCC);
			Assert.That(((CnvtrTypeComboItem)m_myCtrl.cboConverter.SelectedItem).Type, Is.EqualTo(ConverterType.ktypeCC), "Selected CC type properly");
			Assert.That(((CnvtrDataComboItem)m_myCtrl.cboConversion.SelectedItem).Type, Is.EqualTo(ConvType.Legacy_to_Unicode), "CC type defaults to Legacy_to_Unicode");

			m_myCtrl.setCboConverter(ConverterType.ktypeIcuConvert);
			Assert.That(((CnvtrTypeComboItem)m_myCtrl.cboConverter.SelectedItem).Type, Is.EqualTo(ConverterType.ktypeIcuConvert), "Selected ICU Converter type properly");
			Assert.That(((CnvtrDataComboItem)m_myCtrl.cboConversion.SelectedItem).Type, Is.EqualTo(ConvType.Legacy_to_from_Unicode), "ICU Converter type defaults to Legacy_to_from_Unicode");

			m_myCtrl.setCboConverter(ConverterType.ktypeIcuTransduce);
			Assert.That(((CnvtrTypeComboItem)m_myCtrl.cboConverter.SelectedItem).Type, Is.EqualTo(ConverterType.ktypeIcuTransduce), "Selected ICU Transducer type properly");
			Assert.That(((CnvtrDataComboItem)m_myCtrl.cboConversion.SelectedItem).Type, Is.EqualTo(ConvType.Unicode_to_from_Unicode), "ICU Transducer type defaults to Legacy_to_from_Unicode");

			m_myCtrl.setCboConverter(ConverterType.ktypeTecKitTec);
			Assert.That(((CnvtrTypeComboItem)m_myCtrl.cboConverter.SelectedItem).Type, Is.EqualTo(ConverterType.ktypeTecKitTec), "Selected TecKit/Tec type properly");
			Assert.That(((CnvtrDataComboItem)m_myCtrl.cboConversion.SelectedItem).Type, Is.EqualTo(ConvType.Legacy_to_from_Unicode), "TecKit/Tec type defaults to Legacy_to_from_Unicode");

			m_myCtrl.setCboConverter(ConverterType.ktypeTecKitMap);
			Assert.That(((CnvtrTypeComboItem)m_myCtrl.cboConverter.SelectedItem).Type, Is.EqualTo(ConverterType.ktypeTecKitMap), "Selected TecKit/Map type properly");
			Assert.That(((CnvtrDataComboItem)m_myCtrl.cboConversion.SelectedItem).Type, Is.EqualTo(ConvType.Legacy_to_from_Unicode), "TecKit/Map type defaults to Legacy_to_from_Unicode");

			m_myCtrl.setCboConverter(ConverterType.ktypeCodePage);
			Assert.That(((CnvtrTypeComboItem)m_myCtrl.cboConverter.SelectedItem).Type, Is.EqualTo(ConverterType.ktypeCodePage), "Selected CodePage type properly");
			Assert.That(((CnvtrDataComboItem)m_myCtrl.cboConversion.SelectedItem).Type, Is.EqualTo(ConvType.Legacy_to_from_Unicode), "CodePage type defaults to Legacy_to_from_Unicode");
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

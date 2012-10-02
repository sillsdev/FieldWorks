using System;
using System.Diagnostics;
using System.Windows.Forms;
using NUnit.Framework;

using ECInterfaces;
using SilEncConverters31;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.Common.Utils;

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
		private string m_errorMsg;

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
			m_errorMsg = sMessage;
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
	public class CnvtrPropertiesControlTests : BaseTest
	{
		private TempSFFileMaker m_fileMaker;
		private DummyAddCnvtrDlg m_myDlg;
		private DummyCnvtrPropertiesCtrl m_myCtrl;
		private string m_ccFileName;
		private string m_mapFileName;

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

			m_fileMaker = new TempSFFileMaker();
			m_myDlg = new DummyAddCnvtrDlg();
			m_myCtrl = new DummyCnvtrPropertiesCtrl();

			SilEncConverters31.EncConverters encConverters = new SilEncConverters31.EncConverters();

			string[] ccFileContents = {"'c' > 'C'"};
			m_ccFileName = m_fileMaker.CreateFileNoID(ccFileContents, "cct");
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
			m_mapFileName = m_fileMaker.CreateFileNoID(mapFileContents, "map");
			encConverters.AddConversionMap("ZZZTestMap", m_mapFileName,
				ConvType.Legacy_to_from_Unicode, "SIL.map", "", "",
				ProcessTypeFlags.UnicodeEncodingConversion);

			// TODO: Should test a legitimate compiled TecKit file by embedding a zipped
			// up one in the resources for testing purposes.

			// This is a randomly chosen ICU converter. The test may break when we reduce the set of
			// ICU converters we ship.
			encConverters.AddConversionMap("ZZZTestICU", "ISO-8859-1",
				ConvType.Legacy_to_from_Unicode, "ICU.conv", "", "",
				ProcessTypeFlags.UnicodeEncodingConversion);

			// Add a 1-step compound converter, which won't be any of the types our dialog
			// recognizes for now.
			encConverters.AddCompoundConverterStep("ZZZTestCompound", "ZZZTestCC", true,
				NormalizeFlags.None);

			// Load all the mappings after the dummy mappings are added, so the Converter
			// Mapping File combo box won't contain obsolete versions of the mappings referring
			// to old temp files from a previous run of the tests.
			m_myCtrl.CnvtrPropertiesCtrl_Load(null, null);
			encConverters.Remove("Bogus");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Executes in two distinct scenarios.
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_fileMaker != null)
					m_fileMaker.Dispose();
				if (m_myCtrl != null)
					m_myCtrl.Dispose();
				if (m_myDlg != null)
					m_myDlg.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_fileMaker = null;
			m_myCtrl = null;
			m_myDlg = null;
			m_ccFileName = null;
			m_mapFileName = null;

			base.Dispose(disposing);
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
			Assert.IsTrue(m_myCtrl.cboConverter.SelectedItem is CnvtrTypeComboItem);
			Assert.AreEqual(ConverterType.ktypeCC, ((CnvtrTypeComboItem)m_myCtrl.cboConverter.SelectedItem).Type);
			Assert.IsFalse(m_myCtrl.cboSpec.Visible);
			Assert.IsTrue(m_myCtrl.btnMapFile.Visible);
			Assert.IsTrue(m_myCtrl.txtMapFile.Visible);
			Assert.AreEqual(m_myCtrl.txtMapFile.Text, m_ccFileName);
			Assert.IsTrue(m_myCtrl.cboConversion.SelectedItem is CnvtrDataComboItem);
			Assert.AreEqual(ConvType.Legacy_to_Unicode, ((CnvtrDataComboItem)m_myCtrl.cboConversion.SelectedItem).Type);
			Assert.AreEqual("ZZZTestCC", m_myCtrl.txtName.Text);
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
			Assert.IsTrue(m_myCtrl.cboConverter.SelectedItem is CnvtrTypeComboItem);
			Assert.AreEqual(ConverterType.ktypeTecKitMap, ((CnvtrTypeComboItem)m_myCtrl.cboConverter.SelectedItem).Type);
			Assert.IsFalse(m_myCtrl.cboSpec.Visible);
			Assert.IsTrue(m_myCtrl.btnMapFile.Visible);
			Assert.IsTrue(m_myCtrl.txtMapFile.Visible);
			Assert.AreEqual(m_mapFileName, m_myCtrl.txtMapFile.Text);
			Assert.IsTrue(m_myCtrl.cboConversion.SelectedItem is CnvtrDataComboItem);
			Assert.AreEqual(ConvType.Legacy_to_from_Unicode, ((CnvtrDataComboItem)m_myCtrl.cboConversion.SelectedItem).Type);
			Assert.AreEqual("ZZZTestMap", m_myCtrl.txtName.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test a conversion using a standard ICU mapping
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SelectMapping_IcuConversion()
		{
			// Now try one that uses a combo.
			m_myCtrl.SelectMapping("ZZZTestICU");
			Assert.IsTrue(m_myCtrl.cboConverter.SelectedItem is CnvtrTypeComboItem);
			Assert.AreEqual(ConverterType.ktypeIcuConvert, ((CnvtrTypeComboItem)m_myCtrl.cboConverter.SelectedItem).Type);
			Assert.IsTrue(m_myCtrl.cboSpec.Visible);
			Assert.IsFalse(m_myCtrl.btnMapFile.Visible);
			Assert.IsFalse(m_myCtrl.txtMapFile.Visible);
			Assert.IsTrue(m_myCtrl.cboSpec.SelectedItem is CnvtrSpecComboItem);
			// This is a randomly chosen ICU converter. The test may break when we reduce the set of
			// ICU converters we ship.
			Assert.AreEqual("ISO-8859-1", ((CnvtrSpecComboItem)m_myCtrl.cboSpec.SelectedItem).Specs);
			Assert.IsTrue(m_myCtrl.cboConversion.SelectedItem is CnvtrDataComboItem);
			Assert.AreEqual(ConvType.Legacy_to_from_Unicode,
				((CnvtrDataComboItem)m_myCtrl.cboConversion.SelectedItem).Type);
			Assert.AreEqual("ZZZTestICU", m_myCtrl.txtName.Text);
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
			Assert.AreEqual(-1, m_myCtrl.cboConverter.SelectedIndex);
			Assert.IsFalse(m_myCtrl.cboSpec.Visible);
			Assert.IsTrue(m_myCtrl.btnMapFile.Visible);
			Assert.IsTrue(m_myCtrl.txtMapFile.Visible);
			Assert.AreEqual("ZZZTestCompound", m_myCtrl.txtName.Text);
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
				((CnvtrTypeComboItem)m_myCtrl.cboConverter.SelectedItem).Type);
			Assert.AreEqual(ConvType.Legacy_to_Unicode,
				((CnvtrDataComboItem)m_myCtrl.cboConversion.SelectedItem).Type);

			m_myCtrl.setCboConverter(ConverterType.ktypeIcuConvert);
			Assert.AreEqual(ConverterType.ktypeIcuConvert,
				((CnvtrTypeComboItem)m_myCtrl.cboConverter.SelectedItem).Type);
			Assert.AreEqual(ConvType.Legacy_to_from_Unicode,
				((CnvtrDataComboItem)m_myCtrl.cboConversion.SelectedItem).Type);

			m_myCtrl.setCboConverter(ConverterType.ktypeIcuTransduce);
			Assert.AreEqual(ConverterType.ktypeIcuTransduce,
				((CnvtrTypeComboItem)m_myCtrl.cboConverter.SelectedItem).Type);
			Assert.AreEqual(ConvType.Unicode_to_from_Unicode,
				((CnvtrDataComboItem)m_myCtrl.cboConversion.SelectedItem).Type);

			m_myCtrl.setCboConverter(ConverterType.ktypeTecKitTec);
			Assert.AreEqual(ConverterType.ktypeTecKitTec,
				((CnvtrTypeComboItem)m_myCtrl.cboConverter.SelectedItem).Type);
			Assert.AreEqual(ConvType.Legacy_to_from_Unicode,
				((CnvtrDataComboItem)m_myCtrl.cboConversion.SelectedItem).Type);

			m_myCtrl.setCboConverter(ConverterType.ktypeTecKitMap);
			Assert.AreEqual(ConverterType.ktypeTecKitMap,
				((CnvtrTypeComboItem)m_myCtrl.cboConverter.SelectedItem).Type);
			Assert.AreEqual(ConvType.Legacy_to_from_Unicode,
				((CnvtrDataComboItem)m_myCtrl.cboConversion.SelectedItem).Type);

			m_myCtrl.setCboConverter(ConverterType.ktypeCodePage);
			Assert.AreEqual(ConverterType.ktypeCodePage,
				((CnvtrTypeComboItem)m_myCtrl.cboConverter.SelectedItem).Type);
			Assert.AreEqual(ConvType.Legacy_to_from_Unicode,
				((CnvtrDataComboItem)m_myCtrl.cboConversion.SelectedItem).Type);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test a bogus compiled TecKit file. Should fail with nice error message, not crash.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SelectMapping_BogusCompiledTecKitFile()
		{
			string sFileName = m_fileMaker.CreateFileNoID(new string[] { "bogus contents" }, "tec");

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
			Assert.IsTrue(i < m_myDlg.m_cnvtrPropertiesCtrl.cboConverter.Items.Count);
			for (i = 0; i < m_myDlg.m_cnvtrPropertiesCtrl.cboConversion.Items.Count; ++i)
			{
				if (((CnvtrDataComboItem)m_myDlg.m_cnvtrPropertiesCtrl.cboConversion.Items[i]).Type == ConvType.Legacy_to_Unicode)
				{
					m_myDlg.m_cnvtrPropertiesCtrl.cboConversion.SelectedIndex = i;
					break;
				}
			}
			Assert.IsTrue(i < m_myDlg.m_cnvtrPropertiesCtrl.cboConversion.Items.Count);

			m_myDlg.SetMappingFile(sFileName);

			Assert.IsFalse(m_myDlg.InstallConverter());
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
	}
}

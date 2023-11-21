// Copyright (c) 2021 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.TestUtilities;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	// ReSharper disable InconsistentNaming
	/// <summary>
	/// Tests the generation of XHTML or JSON from uSFM. As of 2021-05, only tables are supported, and the generator automatically detects
	/// the presence of USFM at the beginning of any single-line text field.
	/// </summary>
	[TestFixture]
	public class ConfiguredLcmUsfmGeneratorTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase, IDisposable
	{
		private const string XPathToUSFMField = "/div[@class='lexentry']/span[@class='usfm-field']";
		private const string XPathToTitle = XPathToUSFMField + "/table/caption/span";
		private const string XPathToRow = XPathToUSFMField + "/table/tbody/tr";
		private const string XPathToCell = XPathToRow + "/td/span";

		private const string TR = @"\tr";
		private const string TH = @"\th";
		private const string TC = @"\tc";

		private const string StyleBigRed = "@style='color:#DE0000;font-size:1.5em;'";

		private const string USFMFieldName = "USFM Field";
		private CustomFieldForTest m_usfmField;
		private int m_wsEn;

		private ConfigurableDictionaryNode m_configNode;
		private ConfiguredLcmGenerator.GeneratorSettings m_settings;

		private PropertyTable m_propertyTable;
		private FwXApp m_application;
		private FwXWindow m_window;

		[OneTimeSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			FwRegistrySettings.Init();
			m_application = new MockFwXApp(new MockFwManager { Cache = Cache }, null, null);
			var configFilePath = Path.Combine(FwDirectoryFinder.CodeDirectory, m_application.DefaultConfigurationPathname);
			m_window = new MockFwXWindow(m_application, configFilePath);
			((MockFwXWindow)m_window).Init(Cache); // initializes Mediator values
			m_propertyTable = m_window.PropTable;

			m_configNode = CreateInterestingEntryNode();
			m_settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);

			m_wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
			m_usfmField = new CustomFieldForTest(Cache, USFMFieldName, Cache.MetaDataCacheAccessor.GetClassId("LexEntry"),
				m_wsEn, CellarPropertyType.String, Guid.Empty);
		}

		[OneTimeTearDown]
		public override void FixtureTeardown()
		{
			Dispose();
			FwRegistrySettings.Release();
			base.FixtureTeardown();
		}

		#region disposal
		public bool IsDisposed { get; private set; }

		protected virtual void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (disposing && !IsDisposed)
			{
				m_usfmField?.Dispose();
				m_application?.Dispose();
				m_window?.Dispose();
				m_propertyTable?.Dispose();
			}
			IsDisposed = true;
		}

		~ConfiguredLcmUsfmGeneratorTests()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// GC.SuppressFinalize takes this object off the finalization queue
			// and prevents finalization code for this object from executing a second time.
			GC.SuppressFinalize(this);
		}
		#endregion disposal

		internal ILexEntry CreateInterestingLexEntry(string usfm)
		{
			return CreateInterestingLexEntry(TsStringUtils.MakeString(usfm, m_wsEn));
		}

		internal ILexEntry CreateInterestingLexEntry(ITsString usfm)
		{
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			Cache.MainCacheAccessor.SetString(entry.Hvo, m_usfmField.Flid, usfm);
			return entry;
		}

		internal ConfigurableDictionaryNode CreateInterestingEntryNode()
		{
			var customFieldNode = new ConfigurableDictionaryNode
			{
				FieldDescription = USFMFieldName,
				IsCustomField = true,
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "en" })
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { customFieldNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			return mainEntryNode;
		}

		[Test]
		public void NoUSFM_GeneratesPlainText()
		{
			const string plainText = "Plain Text";
			var entry = CreateInterestingLexEntry(plainText);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entry, m_configNode, null, m_settings).ToString();
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				XPathToUSFMField + "/span[@lang='en' and text()='" + plainText + "']", 1);
			AssertThatXmlIn.String(result).HasNoMatchForXpath("//table");
			AssertIsGood(result);
		}

		[Test]
		public void NoLeadingUSFM_GeneratesPlainText()
		{
			const string plainText = "Plain Text\n\\d ignore me";
			var entry = CreateInterestingLexEntry(plainText);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entry, m_configNode, null, m_settings).ToString();
			AssertThatXmlIn.String(result).HasNoMatchForXpath("//table");
			AssertIsGood(result);
		}

		[Test]
		public void LeadingTitle_GeneratesTable()
		{
			const string title = "table title";
			const string titleUSFM = @"\d " + title;
			var entry = CreateInterestingLexEntry(titleUSFM);

			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entry, m_configNode, null, m_settings).ToString();
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				XPathToTitle + "[@lang='en' and text()='" + title + "']", 1);
			AssertIsGood(result);
		}

		[Test]
		public void LeadingTableRow_GeneratesTable()
		{
			var entry = CreateInterestingLexEntry("\\tr\n");
			// SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entry, m_configNode, null, m_settings).ToString();
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(XPathToUSFMField + "/table", 1);
			AssertIsGood(result);
		}

		[Test]
		public void TitleAndTableRow_GeneratesBoth()
		{
			var entry = CreateInterestingLexEntry(@"\d title \tr \tc ");
			// SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entry, m_configNode, null, m_settings).ToString();
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(XPathToUSFMField + "/table", 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				XPathToTitle + "[@lang='en' and text()='title']", 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(XPathToRow, 1);
			AssertIsGood(result);
		}

		[Test]
		public void NoGapNoContentTitleAndRow_DoesNotThrow()
		{
			// table caption and row markup with no whitespace between
			var almostTable = $"\\d{TR}";
			var entry = CreateInterestingLexEntry(almostTable);
			var result = string.Empty;
			// SUT
			Assert.DoesNotThrow(() => result = (ConfiguredLcmGenerator.GenerateContentForEntry(entry, m_configNode, null, m_settings)).ToString());

			// Verify that the field is in the results
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(XPathToUSFMField, 1);
			AssertThatXmlIn.String(result).HasNoMatchForXpath("//table", message: "invalid opening USFM shouldn't trigger USFM generation");
			AssertIsGood(result);
		}

		[Test]
		public void WhitespaceOnlyBetweenTitleAndRow_DoesNotThrow()
		{
			// table caption and row markup with no content
			var almostTable = $"\\d \t{TR}";
			var entry = CreateInterestingLexEntry(almostTable);
			var result = string.Empty;
			// SUT
			Assert.DoesNotThrow(() => result = (ConfiguredLcmGenerator.GenerateContentForEntry(entry, m_configNode, null, m_settings)).ToString());

			// Verify that the partially-typed table is in the results
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(XPathToTitle + "[text()='" + TR + "']", 1);
			AssertIsGood(result);
		}

		[Test]
		public void MissingSpaces_NoCells()
		{
			// table caption and row markup with no whitespace between
			var almostTable = $"{TR} {TH}1content {TC}2content";
			var entry = CreateInterestingLexEntry(almostTable);
			var result = string.Empty;
			// SUT
			Assert.DoesNotThrow(() => result = (ConfiguredLcmGenerator.GenerateContentForEntry(entry, m_configNode, null, m_settings)).ToString());

			// Verify that the field is in the results
			AssertThatXmlIn.String(result).HasNoMatchForXpath("//th");
			AssertThatXmlIn.String(result).HasNoMatchForXpath("//tc");
			Assert.That(result, Does.Not.Contain("/>"));
		}

		[Test]
		public void ManyRowsAndCells()
		{
			const string a1 = "one, eh?";
			const string a2 = "<3";
			const string a3 = "a3";
			const string b1 = "oh, B1";
			const string b2 = "to be";
			const string b3 = "!2b";
			const string c1 = "See one?";
			const string c2 = "sea, too";
			const string c3 = "c3";
			var entry = CreateInterestingLexEntry($"{TR} {TC}1 {a1}\t{TC}2 {a2} \t \r\n{TC}3  {a3} {TR}" +
				$"\t{TC}1 {b1} {TC}2 {b2} {TC}3 {b3} {TR}      {TC}1 {c1} {TC}2 {c2} {TC}3 {c3}");
			// SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entry, m_configNode, null, m_settings).ToString();
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(XPathToUSFMField + "/table", 1);
			AssertThatXmlIn.String(result).HasNoMatchForXpath(XPathToTitle);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(XPathToRow, 3);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(XPathToCell, 9);
			const string xpathToA1 = XPathToCell + "[@lang='en' and text()='" + a1 + "']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(xpathToA1, 1);
			const string xpathToA2Plus = XPathToRow + "/td[span[text()='" + a1 + "']]/following-sibling::td";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(xpathToA2Plus, 2);
			const string xpathToA2 = xpathToA2Plus + "[span[text()='" + a2 + "']]";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(xpathToA2, 1);
			const string xpathToA3 = xpathToA2 + "/following-sibling::td[span[text()='" + a3 + "']]";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(xpathToA3, 1);
			const string xpathToB = XPathToRow + "/td[span[text()='" + b1 +
						"']]/following-sibling::td[span[text()='" + b2 +
						"']]/following-sibling::td[span[text()='" + b3 + "']]";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(xpathToB, 1);
			const string xpathToC = XPathToRow + "/td[span[text()='" + c1 +
						"']]/following-sibling::td[span[text()='" + c2 +
						"']]/following-sibling::td[span[text()='" + c3 + "']]";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(xpathToC, 1);
			AssertIsGood(result);
		}

		[Test]
		public void EmptyCell()
		{
			const string a1 = "emp";
			const string a3 = "ty";
			var entry = CreateInterestingLexEntry($"{TR} {TC}1 {a1}\t{TC}2 {TC}3  {a3}");
			// SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entry, m_configNode, null, m_settings).ToString();
			const string xpathToA = XPathToRow + "/td[span[@lang='en' and text()='" + a1 +
									"']]/following-sibling::td[not(node())]/following-sibling::td[span[text()='" + a3 + "']]";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(xpathToA, 1);
			Assert.That(result, Does.Not.Contain("/>"));
			Assert.That(result, Does.Not.Contain("Invalid USFM"));
		}

		[Test]
		public void EmptyCells()
		{
			var entry = CreateInterestingLexEntry($"{TR} {TC}1   {TC}2\r\n{TC}3\t{TC}4 ");
			// SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entry, m_configNode, null, m_settings).ToString();
			const string xpathToA = XPathToRow +
				"/td[not(node())]/following-sibling::td[not(node())]/following-sibling::td[not(node())]/following-sibling::td[not(node())]";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(xpathToA, 1);
			AssertIsGood(result);
		}

		[Test]
		public void TableHeading_GeneratesTableHeader()
		{
			const string a1h = "ahead";
			const string a2c = "tail?";
			const string b1c = "foot";
			const string b2h = "not normally expected, but not forbidden";
			var entry = CreateInterestingLexEntry($"{TR} {TH}1 {a1h}  {TC}2\r{a2c}\n{TR} {TC}1 {b1c} {TH}2 {b2h}");
			// SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entry, m_configNode, null, m_settings).ToString();
			const string xpathToA = XPathToRow + "/th[span[@lang='en' and text()='" + a1h +
						"']]/following-sibling::td[span[@lang='en' and text()='" + a2c + "']]";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(xpathToA, 1);
			const string xpathToB = XPathToRow + "/td[span[@lang='en' and text()='" + b1c +
						"']]/following-sibling::th[span[@lang='en' and text()='" + b2h + "']]";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(xpathToB, 1);
			AssertIsGood(result);
		}

		[Test]
		public void TableCellAlignment()
		{
			const string a1 = "head";
			const string a2 = "tail";
			const string b1 = "foot";
			const string b2 = "nose";
			const string c1 = "knee";
			const string c2 = "toes";
			const string d1 = "eyes";
			const string d2 = "ears";
			var entry = CreateInterestingLexEntry($"{TR} {TH}r1 {a1}  {TH}2\r{a2}\n{TR} {TC}1 {b1} {TC}r2 {b2} " +
												  $"{TR} {TH}c1 {c1}  {TH}l2\r{c2}\n{TR} {TC}l1 {d1} {TC}c2 {d2}");
			// SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entry, m_configNode, null, m_settings).ToString();
			const string xpathToA = XPathToRow + "/th[@style='text-align: right;' and span[@lang='en' and text()='" + a1 +
									"']]/following-sibling::th[not(@style) and span[@lang='en' and text()='" + a2 + "']]";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(xpathToA, 1);
			const string xpathToB = XPathToRow + "/td[not(@style) and span[@lang='en' and text()='" + b1 +
									"']]/following-sibling::td[@style='text-align: right;' and span[@lang='en' and text()='" + b2 + "']]";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(xpathToB, 1);
			const string xpathToC = XPathToRow + "/th[@style='text-align: center;' and span[@lang='en' and text()='" + c1 +
									"']]/following-sibling::th[@style='text-align: left;' and span[@lang='en' and text()='" + c2 + "']]";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(xpathToC, 1);
			const string xpathToD = XPathToRow + "/td[@style='text-align: left;' and span[@lang='en' and text()='" + d1 +
									"']]/following-sibling::td[@style='text-align: center;' and span[@lang='en' and text()='" + d2 + "']]";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(xpathToD, 1);
			AssertIsGood(result);
		}

		[Test]
		public void MultipleTables()
		{
			const string title1 = "first";
			const string la1 = "loner";
			const string title2 = "second";
			const string za1 = "Hu is on first";
			const string za2 = "Watt's the name of the guy on second";
			const string zb1 = "Ida Nou";
			const string zb2 = "home, sweet home";
			const string loneCell = "untitled table";
			var entry = CreateInterestingLexEntry(
				$@"\d {title1} {TR} {TC}1 {la1} \d {title2} {TR} {TH}1 {za1} {TH}2 {za2} {TR} {TC}1 {zb1} {TC}2 {zb2} \d {TR} {TH}1 {loneCell}");

			// SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entry, m_configNode, null, m_settings).ToString();
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(XPathToUSFMField + "/table", 3);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(XPathToTitle, 2);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(XPathToRow, 4); // 1 + 2 + 1 = 4
			AssertIsGood(result);
		}

		[TestCase(1, 2)]
		[TestCase(2, 4)]
		[TestCase(3, 7)]
		public void CellRange(int min, int lim)
		{
			var entry = CreateInterestingLexEntry($"{TR} {TC}{min}-{lim} home on the range");
			// SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entry, m_configNode, null, m_settings).ToString();
			// +1 because the range includes both ends
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath($"{XPathToRow}/td[@colspan='{lim - min + 1}']", 1);
			AssertIsGood(result);
		}

		[TestCase("")]
		[TestCase("4")]
		[TestCase("3-3")]
		[TestCase("2-1")]
		[TestCase("4-1")]
		public void CellRange_None_NoColSpan(string range)
		{
			const string content = "seldom";
			var entry = CreateInterestingLexEntry($"{TR} {TC}{range} {content}");
			// SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entry, m_configNode, null, m_settings).ToString();
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath($"{XPathToRow}/td[not(@colspan)]/span[text()='{content}']", 1);
			AssertIsGood(result);
		}

		[Test]
		public void BadUSFM_RowWithoutCells()
		{
			const string junk = "no cells";
			var entry = CreateInterestingLexEntry($"{TR} {junk}");
			// SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entry, m_configNode, null, m_settings).ToString();
			var expected = string.Format(xWorksStrings.InvalidUSFM_TextAfterTR, junk);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(XPathToUSFMField +
				$"//span[{StyleBigRed} and text() = '{expected}']", 1);
			Assert.That(result, Does.Not.Contain("/>"));
			Assert.That(result, Does.EndWith("</div>"));
		}

		[Test]
		public void BadUSFM_RowWithTextBeforeCells()
		{
			const string junk = "oops";
			var entry = CreateInterestingLexEntry($"{TR} {junk} {TC}1 data");
			// SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entry, m_configNode, null, m_settings).ToString();

			var expected = string.Format(xWorksStrings.InvalidUSFM_TextAfterTR, junk);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(XPathToUSFMField +
				$"//span[{StyleBigRed} and text() = '{expected}']", 1);
			Assert.That(result, Does.Not.Contain("/>"));
			Assert.That(result, Does.EndWith("</div>"));
		}

		[TestCase(TR + " " + TC, XPathToRow + "/td")]
		[TestCase(TR + " " + TH + "1", XPathToRow + "/th")]
		public void BadUSFM_PartiallyTyped_NoErrors(string usfm, string xpath)
		{
			var entry = CreateInterestingLexEntry(usfm);
			// SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entry, m_configNode, null, m_settings).ToString();
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(xpath, 1);
			Assert.That(result, Does.EndWith("</div>"));
			AssertIsGood(result);
		}

		[Test]
		public void BadUSFM_PartiallyTyped_NoErrors([Values(@"\t", TC, TH + "1", TH + "l", TC + "3-", TH + "7-11")] string marker)
		{
			var entry = CreateInterestingLexEntry($@"{TR} {marker}\th2 exists");
			// SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entry, m_configNode, null, m_settings).ToString();
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(XPathToUSFMField +
				$"//span[{StyleBigRed} and text()='{marker}']", 1);
			Assert.That(result, Does.EndWith("</div>"));
			AssertIsGood(result);
		}

		[Test]
		public void BadUSFM_PartiallyTyped_NoError()
		{
			var entry = CreateInterestingLexEntry(TR + @" \t");
			// SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entry, m_configNode, null, m_settings).ToString();
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(XPathToUSFMField +
				"//span[" + StyleBigRed + @" and text()='\t']", 1);
			Assert.That(result, Does.EndWith("</div>"));
			AssertIsGood(result);
		}

		[Test]
		public void MistypedMarker([Values("tre", "tcp", "t", "td", "tl", "t2", "h", "th-2", "th2-", "doh")] string misMark)
		{
			const string cellBefore = "before";
			const string cellAfter = "after";
			var cellWithBadMark = $@"in \{misMark} between";
			var entry = CreateInterestingLexEntry($"{TR} {TC} {cellBefore} {TC} {cellWithBadMark} {TC} {cellAfter}");
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entry, m_configNode, null, m_settings).ToString();
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(XPathToCell, 3);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(XPathToCell + "[text()='" + cellBefore + "']", 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(XPathToCell + "[text()='" + cellAfter + "']", 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath($"{XPathToCell}[text()='{cellWithBadMark}']", 1);
			AssertIsGood(result);
		}

		private static void AssertIsGood(string xhtml)
		{
			Assert.That(xhtml, Does.Not.Contain("/>"));
			Assert.That(xhtml, Does.Not.Contain("Invalid USFM"));
		}
	}
}

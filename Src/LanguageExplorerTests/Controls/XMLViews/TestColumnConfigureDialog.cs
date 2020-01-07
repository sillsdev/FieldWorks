// Copyright (c) 2010-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using System.Xml.Linq;
using LanguageExplorer.Controls.XMLViews;
using LanguageExplorer.TestUtilities;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorerTests.Controls.XMLViews
{
	/// <summary />
	[TestFixture]
	public class TestColumnConfigureDialog
	{
		private FlexComponentParameters _flexComponentParameters;
		private LcmCache m_cache;

		[SetUp]
		public void SetUp()
		{
			_flexComponentParameters = TestSetupServices.SetupTestTriumvirate();
			var st = StringTable.Table; // Make sure it is loaded.
			m_cache = LcmCache.CreateCacheWithNewBlankLangProj(new TestProjectId(BackendProviderType.kMemoryOnly, null), "en", "en", "en", new DummyLcmUI(), FwDirectoryFinder.LcmDirectories, new LcmSettings());
			_flexComponentParameters.PropertyTable.SetProperty("cache", m_cache);
		}

		[TearDown]
		public void TearDown()
		{
			TestSetupServices.DisposeTrash(_flexComponentParameters);
			_flexComponentParameters = null;
			m_cache.Dispose();
			m_cache = null;
		}

		#region AfterMovingItemArrowsAreNotImproperlyDisabled

		/// <summary>
		/// FWNX-313: flex configure column arrows inappropriately disabling
		/// After moving a current columns item up or down, the up and down buttons
		/// should not both be disabled.
		/// <seealso cref="AfterMovingItemArrowsAreNotImproperlyDisabled_OnUp"/>
		/// </summary>
		[Test]
		public void AfterMovingItemArrowsAreNotImproperlyDisabled_OnDown()
		{
			using (var window = CreateColumnConfigureDialog())
			{
				window.Show();
				window.currentList.Items[1].Selected = true;
				window.moveDownButton.PerformClick();
				Assert.True(window.moveUpButton.Enabled, "Up button should not be disabled after moving an item down.");
			}
		}

		/// <see cref="AfterMovingItemArrowsAreNotImproperlyDisabled_OnDown"/>
		[Test]
		public void AfterMovingItemArrowsAreNotImproperlyDisabled_OnUp()
		{
			using (var window = CreateColumnConfigureDialog())
			{
				window.Show();
				window.currentList.Items[1].Selected = true;
				window.moveUpButton.PerformClick();
				Assert.True(window.moveDownButton.Enabled, "Down button should not be disabled after moving an item up.");
			}
		}
		#endregion

		#region WsCombo selection tests
		/// <summary/>
		[Test]
		[Category("ByHand")]
		public void AnalysisVernacularWsSetsWsComboToAnalysis()
		{
			using (var window = CreateColumnConfigureDialog("<root></root>", "<root><column layout=\"EntryHeadwordForEntry\" label=\"Headword\" ws=\"$ws=analysis vernacular\" cansortbylength=\"true\" visibility=\"always\" /></root>"))
			{
				window.Show();
				window.optionsList.Items[0].Selected = true;
				window.addButton.PerformClick();
				Assert.AreEqual(((WsComboItem)window.wsCombo.SelectedItem).Id, "analysis", "Default analysis should be selected for 'analysis vernacular' ws");
			}
		}
		#endregion

		private ColumnConfigureDialog CreateColumnConfigureDialog()
		{
			// Create window and populate currentColumns with a few items.
			const string currentColumns_data = "<root><column layout=\"EntryHeadwordForEntry\" label=\"Headword\" ws=\"$ws=vernacular\" width=\"72000\" sortmethod=\"FullSortKey\" cansortbylength=\"true\" visibility=\"always\" /><column layout=\"LexemeFormForEntry\" label=\"Lexeme Form\" common=\"true\" width=\"72000\" ws=\"$ws=vernacular\" sortmethod=\"MorphSortKey\" cansortbylength=\"true\" visibility=\"always\" transduce=\"LexEntry.LexemeForm.Form\" transduceCreateClass=\"MoStemAllomorph\" /><column layout=\"GlossesForSense\" label=\"Glosses\" multipara=\"true\" width=\"72000\" ws=\"$ws=analysis\" transduce=\"LexSense.Gloss\" cansortbylength=\"true\" visibility=\"always\" /><column layout=\"GrammaticalInfoFullForSense\" headerlabel=\"Grammatical Info.\" chooserFilter=\"external\" label=\"Grammatical Info. (Full)\" multipara=\"true\" width=\"72000\" visibility=\"always\"><dynamicloaderinfo assemblyPath=\"LanguageExplorer.dll\" class=\"LanguageExplorer.LcmUi.PosFilter\" /></column></root>";
			return CreateColumnConfigureDialog(currentColumns_data, "<root></root>");
		}

		private ColumnConfigureDialog CreateColumnConfigureDialog(string currentColumnsData, string possibleColumnsData)
		{
			// Create window and populate currentColumns with a few items.
			var currentColumns_document = XDocument.Parse(currentColumnsData);
			var currentColumns = currentColumns_document.Root.Elements().ToList();
			var possibleColumns_document = XDocument.Parse(possibleColumnsData);
			var possibleColumns = possibleColumns_document.Root.Elements().ToList();
			var window = new ColumnConfigureDialog(possibleColumns, currentColumns, _flexComponentParameters.PropertyTable);
			window.FinishInitialization();
			return window;
		}
	}
}
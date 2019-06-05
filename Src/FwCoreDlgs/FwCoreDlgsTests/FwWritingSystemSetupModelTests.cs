// Copyright (c) 2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Rhino.Mocks;
using SIL.Extensions;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainServices;
using SIL.Windows.Forms.WritingSystems;
using SIL.WritingSystems;
using SIL.WritingSystems.Tests;
using Is = NUnit.Framework.Is;

namespace SIL.FieldWorks.FwCoreDlgs
{
	class FwWritingSystemSetupModelTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{

		[SetUp]
		public void SetUp()
		{
		}

		[Test]
		public void CanCreateModel()
		{
			var container = new TestWSContainer(new [] {"en"});
			// ReSharper disable once ObjectCreationAsStatement
			Assert.DoesNotThrow(() => new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular));
		}

		[Test]
		public void SelectionForSpecialCombo_HasDefaultScriptAndRegion_GivesScriptRegionVariant()
		{
			var container = new TestWSContainer(new[] { "en-Latn-US" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			Assert.AreEqual(WritingSystemSetupModel.SelectionsForSpecialCombo.ScriptRegionVariant, testModel.CurrentWsSetupModel.SelectionForSpecialCombo);
		}

		[Test]
		public void SelectionForSpecialCombo_ChangesOnSelectionChange_GivesScriptRegionVariant()
		{
			var container = new TestWSContainer(new[] { "en", "en-Kore-US" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			Assert.AreEqual(WritingSystemSetupModel.SelectionsForSpecialCombo.None, testModel.CurrentWsSetupModel.SelectionForSpecialCombo);
			testModel.SelectWs("en-Kore-US");
			Assert.AreEqual(WritingSystemSetupModel.SelectionsForSpecialCombo.ScriptRegionVariant, testModel.CurrentWsSetupModel.SelectionForSpecialCombo);
		}

		[Test]
		public void SelectionForSpecialCombo_LockedForDefaultEnglish()
		{
			var container = new TestWSContainer(new[] { "en", "de" });
			string expectedErrorMessage = string.Format(FwCoreDlgs.kstidCantChangeEnglishSRV, "English");
			string errorMessage = null;
			var wssModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular)
			{
				ShowMessageBox = text => { errorMessage = text; }
			}.CurrentWsSetupModel;
			wssModel.CurrentScriptCode = "Cyrilic";
			Assert.AreEqual("Latn", wssModel.CurrentScriptCode, "script code should be reset to Latin");
			Assert.AreEqual(WritingSystemSetupModel.SelectionsForSpecialCombo.None, wssModel.SelectionForSpecialCombo, "script");
			Assert.AreEqual("en", wssModel.CurrentLanguageTag, "script");
			Assert.AreEqual(expectedErrorMessage, errorMessage, "script");
			errorMessage = null; // reset for next test
			wssModel.CurrentRegion = "GB";
			Assert.AreEqual("", wssModel.CurrentRegion, "region");
			Assert.AreEqual(WritingSystemSetupModel.SelectionsForSpecialCombo.None, wssModel.SelectionForSpecialCombo, "region");
			Assert.AreEqual("en", wssModel.CurrentLanguageTag, "region");
			Assert.AreEqual(expectedErrorMessage, errorMessage, "region");
			errorMessage = null; // reset for next test
			wssModel.CurrentIsVoice = true;
			Assert.False(wssModel.CurrentIsVoice, "voice");
			Assert.AreEqual(WritingSystemSetupModel.SelectionsForSpecialCombo.None, wssModel.SelectionForSpecialCombo, "voice");
			Assert.AreEqual("en", wssModel.CurrentLanguageTag, "voice");
			Assert.AreEqual(expectedErrorMessage, errorMessage, "voice");
			errorMessage = null; // reset for next test
			wssModel.CurrentIpaStatus = IpaStatusChoices.Ipa;
			Assert.AreEqual(IpaStatusChoices.NotIpa, wssModel.CurrentIpaStatus);
			Assert.AreEqual(WritingSystemSetupModel.SelectionsForSpecialCombo.None, wssModel.SelectionForSpecialCombo, "IPA");
			Assert.AreEqual("en", wssModel.CurrentLanguageTag, "IPA");
			Assert.AreEqual(expectedErrorMessage, errorMessage, "IPA");
			errorMessage = null; // reset for next test
			wssModel.CurrentVariant = "x-xqax"; // or something like that
			Assert.IsEmpty(wssModel.CurrentVariant, "Variants");
			Assert.AreEqual(WritingSystemSetupModel.SelectionsForSpecialCombo.None, wssModel.SelectionForSpecialCombo, "Variants");
			Assert.AreEqual("en", wssModel.CurrentLanguageTag, "Variants");
			Assert.AreEqual(expectedErrorMessage, errorMessage, "Variants");
		}

		[Test]
		public void AdvancedConfiguration_NonCustomLangScriptRegion_IsDisabled()
		{
			var container = new TestWSContainer(new[] { "en-Latn-US" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			Assert.IsFalse(testModel.ShowAdvancedScriptRegionVariantView, "Model should not show advanced view for normal data");
		}

		[Test]
		public void AdvancedConfiguration_CustomScript_IsEnabled()
		{
			var container = new TestWSContainer(new[] { "en-Qaaa-x-CustomSc" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			Assert.IsTrue(testModel.ShowAdvancedScriptRegionVariantView, "Model should show advanced view for Custom script");
		}

		[Test]
		public void AdvancedConfiguration_CustomRegion_IsEnabled()
		{
			var container = new TestWSContainer(new[] { "en-QM-x-CustomRg" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			Assert.IsTrue(testModel.ShowAdvancedScriptRegionVariantView, "Model should show advanced view for Custom script");
		}

		[Test]
		public void AdvancedConfiguration_CustomLanguage_IsEnabled()
		{
			var container = new TestWSContainer(new[] { "Qaa-x-CustomLa" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			Assert.IsTrue(testModel.ShowAdvancedScriptRegionVariantView, "Model should show advanced view for Custom script");
		}

		[Test]
		public void AdvancedConfiguration_StandardAndPrivateUse_IsEnabled()
		{
			var container = new TestWSContainer(new[] { "fr-fonipa-x-special" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			Assert.IsTrue(testModel.ShowAdvancedScriptRegionVariantView, "Model should show advanced view when there are multiple variants");
		}

		[Test]
		public void AdvancedConfiguration_AllPrivateUse_IsNotEnabled()
		{
			var container = new TestWSContainer(new[] { "fr-x-special-extra" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			Assert.IsFalse(testModel.ShowAdvancedScriptRegionVariantView, "Model should show advanced view when there are multiple variants");
		}

		[Test]
		public void AdvancedConfiguration_ClearingAdvanced_ShowsWarning_ClearsCustomContent()
		{
			var container = new TestWSContainer(new[] { "fr-Qaaa-QM-fonipa-x-Cust-CM-extra" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			bool confirmClearCalled = false;
			testModel.ConfirmClearAdvanced = () =>
			{
				confirmClearCalled = true;
				return true;
			};
			Assert.IsTrue(testModel.ShowAdvancedScriptRegionVariantView, "should be advanced to start");
			testModel.ShowAdvancedScriptRegionVariantView = false;
			Assert.IsTrue(confirmClearCalled);
			Assert.IsNull(testModel.CurrentWsSetupModel.CurrentRegionTag);
			Assert.IsFalse(testModel.CurrentWsSetupModel.CurrentIso15924Script.IsPrivateUse);
		}

		[Test]
		public void AdvancedConfiguration_NonGraphiteFont_GraphiteFontOptionsAreDisabled()
		{
			var englishWithDefaultScript = new CoreWritingSystemDefinition("en");
			var notGraphite = new FontDefinition("Calibre");
			notGraphite.Engines = FontEngines.None;
			englishWithDefaultScript.Fonts.Add(notGraphite);
			var container = new TestWSContainer(new [] { englishWithDefaultScript });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			Assert.IsFalse(testModel.EnableGraphiteFontOptions, "Non Graphite fonts should not have the EnableGraphiteFontOptions available");
		}

		[TestCase("en", false)]
		[TestCase("en-Arab", true)]
		[TestCase("en-Qaaa-x-Mark", true)]
		public void AdvancedConfiguration_AdvancedScriptRegionVariantCheckboxVisible(string languageTag, bool expectedResult)
		{
			var container = new TestWSContainer(new[] { languageTag });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			Assert.AreEqual(expectedResult, testModel.ShowAdvancedScriptRegionVariantCheckBox);
		}

		[Test]
		public void AdvancedConfiguration_GraphiteFont_GraphiteFontOptionsAreEnabled()
		{
			var englishWithDefaultScript = new CoreWritingSystemDefinition("en");
			var notGraphite = new FontDefinition("SIL Charis");
			notGraphite.Engines &= FontEngines.Graphite;
			englishWithDefaultScript.Fonts.Add(notGraphite);
			var container = new TestWSContainer(new[] { englishWithDefaultScript });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			Assert.IsTrue(testModel.EnableGraphiteFontOptions, "Graphite fonts should have the EnableGraphiteFontOptions available");
		}

		[Test]
		public void AdvancedConfiguration_NoDefaultFont_GraphiteFontOptionsAreDisabled()
		{
			var container = new TestWSContainer(new[] { "en" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			Assert.IsFalse(testModel.EnableGraphiteFontOptions, "EnableGraphiteFeatures should not be available without a default font");
		}

		[TestCase("en", new[] { "en" }, false)] // Can't move the only item anywhere
		[TestCase("fr", new[] { "fr", "en" }, false)] // Can't move the top item up
		[TestCase("en", new[] { "fr", "en" }, true)] // Can move an item up if there is one above it
		public void WritingSystemList_MoveUp_CanMoveUp(string toMove, string[] options, bool expectedResult)
		{
			var container = new TestWSContainer(options);
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.SelectWs(toMove);
			Assert.AreEqual(expectedResult, testModel.CanMoveUp());
		}

		[TestCase("en", new[] { "en" }, false)] // Can't move the only item anywhere
		[TestCase("fr", new[] { "fr", "en" }, true)] // Can move an item down if it isn't at the bottom
		[TestCase("en", new[] { "fr", "en" }, false)] // Can't move the bottom item down
		public void WritingSystemList_MoveUp_CanMoveDown(string toMove, string[] options, bool expectedResult)
		{
			var container = new TestWSContainer(options);
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.SelectWs(toMove);
			Assert.AreEqual(expectedResult, testModel.CanMoveDown());
		}

		[Test]
		public void WritingSystemList_RightClickMenuItems_ChangeWithSelection()
		{
			var container = new TestWSContainer(new[] { "es", "fr" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			var menu = testModel.GetRightClickMenuItems().Select(item => item.MenuText);
			CollectionAssert.AreEqual(new[] { "Merge...", "Delete Spanish" }, menu);
			testModel.SelectWs("fr");
			menu = testModel.GetRightClickMenuItems().Select(item => item.MenuText);
			CollectionAssert.AreEqual(new[] { "Merge...", "Delete French" }, menu);
		}

		[TestCase(FwWritingSystemSetupModel.ListType.Vernacular, true)]
		[TestCase(FwWritingSystemSetupModel.ListType.Analysis, false)]
		public void WritingSystemList_RightClickMenuItems_CannotMergeOrDeleteEnglishAnalyWs(FwWritingSystemSetupModel.ListType type, bool canDelete)
		{
			var container = new TestWSContainer(new[] { "en", "fr" }, new[] { "en", "fr" });
			var testModel = new FwWritingSystemSetupModel(container, type);
			var menu = testModel.GetRightClickMenuItems();
			Assert.That(menu.Count, Is.EqualTo(1));
			Assert.That(menu.First().IsEnabled, Is.EqualTo(canDelete), "English can be deleted from the Vernacular but not the Analysis WS List");
			Assert.That(menu.First().MenuText, Is.StringMatching("Delete English"));
		}

		[Test]
		public void WritingSystemList_RightClickMenuItems_NoMergeForSingleWs()
		{
			var container = new TestWSContainer(new[] { "fr" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			var menu = testModel.GetRightClickMenuItems();
			Assert.That(menu.Count, Is.EqualTo(1));
			Assert.IsFalse(menu.First().IsEnabled);
			Assert.That(menu.First().MenuText, Is.StringMatching("Delete French"));
		}

		[Test]
		public void WritingSystemList_AddMenuItems_ChangeWithSelection()
		{
			var container = new TestWSContainer(new [] { "en", "fr" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			var addMenu = testModel.GetAddMenuItems().Select(item => item.MenuText);
			CollectionAssert.AreEqual(new [] { "Add IPA for English", "Add Audio for English", "Add variation of English", "Add new language..." }, addMenu);
			testModel.SelectWs("fr");
			addMenu = testModel.GetAddMenuItems().Select(item => item.MenuText);
			CollectionAssert.AreEqual(new[] { "Add IPA for French", "Add Audio for French", "Add variation of French", "Add new language..." }, addMenu);
		}

		[Test]
		public void WritingSystemList_AddMenuItems_DoesNotOfferExistingOption()
		{
			var container = new TestWSContainer(new [] { "auc" }, new[] { "en", "en-fonipa", "en-Zxxx-x-audio" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Analysis);
			var addMenu = testModel.GetAddMenuItems().Select(item => item.MenuText);
			CollectionAssert.AreEqual(new[] { "Add variation of English", "Add new language..." }, addMenu);
		}

		[Test]
		public void WritingSystemList_AddMenuItems_DoesNotOfferIpaWhenIpaSelected()
		{
			var container = new TestWSContainer(new[] { "en-fonipa", "en", "en-Zxxx-x-audio" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			var addMenu = testModel.GetAddMenuItems().Select(item => item.MenuText);
			CollectionAssert.AreEqual(new[] { "Add variation of English", "Add new language..." }, addMenu);
		}

		[Test]
		public void WritingSystemList_MoveUp_ItemMoved()
		{
			var container = new TestWSContainer(new[] { "fr", "en" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			CollectionAssert.AreEqual(new[] {"fr", "en"}, testModel.WorkingList.Select(ws => ws.WorkingWs.LanguageTag));
			testModel.SelectWs("en");
			// SUT
			testModel.MoveUp();
			CollectionAssert.AreEqual(new [] { "en", "fr" }, testModel.WorkingList.Select(ws => ws.WorkingWs.LanguageTag));
		}

		[Test]
		public void WritingSystemList_ToggleInCurrentList_ToggleWorks()
		{
			var container = new TestWSContainer(new[] { "fr", "en" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			CollectionAssert.AreEqual(new[] { true, true }, testModel.WorkingList.Select(ws => ws.InCurrentList));
			testModel.SelectWs("en");
			// SUT
			testModel.ToggleInCurrentList();
			CollectionAssert.AreEqual(new[] { true, false }, testModel.WorkingList.Select(ws => ws.InCurrentList));
		}

		[Test]
		public void WritingSystemList_MoveDown_ItemMoved()
		{
			var container = new TestWSContainer(new[] { "fr", "en" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			CollectionAssert.AreEqual(new[] { "fr", "en" }, testModel.WorkingList.Select(ws => ws.WorkingWs.LanguageTag));
			testModel.SelectWs("fr");
			// SUT
			testModel.MoveDown();
			CollectionAssert.AreEqual(new[] { "en", "fr" }, testModel.WorkingList.Select(ws => ws.WorkingWs.LanguageTag));
		}

		[TestCase("en", new[] { "fr", "en" }, false)] // Can't merge English
		[TestCase("fr", new[] { "fr" }, false)] // Can't merge if there is no other writing system in the list
		[TestCase("fr", new[] { "fr", "en" }, true)] // Can merge if there is more than one
		public void WritingSystemList_CanMerge(string toMerge, string[] options, bool expectedResult)
		{
			foreach(var type in new[] { FwWritingSystemSetupModel.ListType.Analysis, FwWritingSystemSetupModel.ListType.Vernacular })
			{
				var container = new TestWSContainer(options, options);
				var testModel = new FwWritingSystemSetupModel(container, type);
				testModel.SelectWs(toMerge);
				Assert.AreEqual(expectedResult, testModel.CanMerge());
			}
		}

		[Test]
		public void WritingSystemList_CanMerge_CantMergeNewWs(
			[Values(FwWritingSystemSetupModel.ListType.Analysis, FwWritingSystemSetupModel.ListType.Vernacular)]
			FwWritingSystemSetupModel.ListType listType,
			[Values("Audio", "variation")] string variantType) // test only Audio and Variation because IPA requires the Cache
		{
			var wss = new[] { "de" };
			var container = new TestWSContainer(wss, wss);
			var testModel = new FwWritingSystemSetupModel(container, listType);
			var addMenuItems = testModel.GetAddMenuItems();
			addMenuItems.First(item => item.MenuText.Contains(variantType)).ClickHandler.Invoke(this, new EventArgs());
			Assert.AreEqual(false, testModel.CanMerge());
		}

		[TestCase(FwWritingSystemSetupModel.ListType.Analysis, "en", new[] { "fr", "en" }, false)] // Can't delete English from the Analysis list
		[TestCase(FwWritingSystemSetupModel.ListType.Analysis, "en-GB", new[] { "en-GB", "en" }, true)] // Can delete variants of English
		[TestCase(FwWritingSystemSetupModel.ListType.Analysis, "en-fonipa", new[] { "en-fonipa", "en" }, true)] // Can delete variants of English
		[TestCase(FwWritingSystemSetupModel.ListType.Analysis, "en-Zxxx-x-audio", new[] { "en-Zxxx-x-audio", "en" }, true)] // "		"
		[TestCase(FwWritingSystemSetupModel.ListType.Analysis, "fr", new[] { "fr" }, false)] // Can't delete the only writing system in the list
		[TestCase(FwWritingSystemSetupModel.ListType.Analysis, "fr", new[] { "fr", "en" }, true)] // Can delete if there is more than one
		[TestCase(FwWritingSystemSetupModel.ListType.Vernacular, "en", new[] { "fr", "en" }, true)] // Can delete English from the Vernacular list
		[TestCase(FwWritingSystemSetupModel.ListType.Vernacular, "fr", new[] { "fr" }, false)] // Can't delete the only writing system in the list
		[TestCase(FwWritingSystemSetupModel.ListType.Vernacular, "fr", new[] { "fr", "en" }, true)] // Can delete if there is more than one
		public void WritingSystemList_CanDelete(FwWritingSystemSetupModel.ListType type, string toRemove, string[] options, bool expectedResult)
		{
			var container = new TestWSContainer(options, options);
			var testModel = new FwWritingSystemSetupModel(container, type);
			testModel.SelectWs(toRemove);
			Assert.AreEqual(expectedResult, testModel.CanDelete());
		}

		[Test]
		public void WritingSystemList_CanDelete_CanDeleteDuplicateEnglishAnalysis()
		{
			var wss = new[] { "en" };
			var container = new TestWSContainer(wss, wss);
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Analysis);
			var addMenuItems = testModel.GetAddMenuItems();
			addMenuItems.First(item => item.MenuText.Contains("variation")).ClickHandler.Invoke(this, new EventArgs());
			Assert.AreEqual("en", testModel.CurrentWsSetupModel.CurrentLanguageTag);
			Assert.AreEqual(true, testModel.CanDelete(), "should be able to delete the newly-created English [in]variant");
			testModel.SelectWs(0);
			Assert.AreEqual(false, testModel.CanDelete(), "should not be able to delete the original English");
		}

		[Test]
		public void WritingSystemList_IsListValid_FalseIfNoCurrentItems()
		{
			var container = new TestWSContainer(new [] { "en" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.ToggleInCurrentList();
			Assert.IsFalse(testModel.IsListValid);
		}

		[Test]
		public void WritingSystemList_IsListValid_TrueIfOneCurrentItem()
		{
			var container = new TestWSContainer(new [] { "en" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			Assert.IsTrue(testModel.IsListValid);
		}

		[Test]
		public void WritingSystemList_IsListValid_FalseIfDuplicateItem()
		{
			var container = new TestWSContainer(new[] { "en", "en" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			Assert.IsFalse(testModel.IsListValid);
		}

		[Test]
		public void WritingSystemList_IsAtLeastOneSelected_FalseIfNoCurrentItems()
		{
			var container = new TestWSContainer(new [] { "en" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.ToggleInCurrentList();
			Assert.IsFalse(testModel.IsAtLeastOneSelected);
		}

		[Test]
		public void WritingSystemList_IsAtLeastOneSelected_TrueIfOneCurrentItem()
		{
			var container = new TestWSContainer(new [] { "en" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			Assert.IsTrue(testModel.IsAtLeastOneSelected);
		}

		[Test]
		public void WritingSystemList_FirstDuplicateWs_NullIfNoDuplicates()
		{
			var container = new TestWSContainer(new [] { "en" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			Assert.IsNull(testModel.FirstDuplicateWs);
		}

		[Test]
		public void WritingSystemList_FirstDuplicateWs()
		{
			var container = new TestWSContainer(new[] { "en", "en" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			Assert.AreEqual("English", testModel.FirstDuplicateWs);
		}

		[Test]
		public void WritingSystemList_CurrentList_StaysStable()
		{
			var container = new TestWSContainer(new[] { "en", "fr", "en-Zxxx-x-audio" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			Assert.AreEqual("English", testModel.WorkingList[0].WorkingWs.DisplayLabel);
			testModel.SelectWs("en-Zxxx-x-audio");
			Assert.AreEqual("English", testModel.WorkingList[0].WorkingWs.DisplayLabel);
		}

		[Test]
		public void MergeTargets_SkipsNewAndCurrent()
		{
			var container = new TestWSContainer(new[] { "en", "fr" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			var addMenuItems = testModel.GetAddMenuItems();
			// Add an audio writing system because it doesn't currently require a cache to create properly
			addMenuItems.First(item => item.MenuText.Contains("Audio")).ClickHandler.Invoke(this, new EventArgs());
			testModel.SelectWs("en");
			// SUT
			var mergeTargets = testModel.MergeTargets;
			Assert.That(mergeTargets.Count(), Is.EqualTo(1));
			Assert.That(mergeTargets.First().WorkingWs.LanguageTag, Is.StringMatching("fr"));
		}

		[Test]
		public void WritingSystemList_AddItems_AddAudio_CustomNameUsed()
		{
			var container = new TestWSContainer(new[] { "en", "fr" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.LanguageName = "Testing";
			var addMenuItems = testModel.GetAddMenuItems();
			// SUT
			// Add an audio writing system because it currently doesn't require a cache to create properly
			addMenuItems.First(item => item.MenuText.Contains("Audio")).ClickHandler.Invoke(this, new EventArgs());
			Assert.That(testModel.CurrentWsSetupModel.CurrentLanguageTag, Is.EqualTo("en-Zxxx-x-audio"));
			Assert.That(testModel.LanguageName, Is.StringMatching("Testing"));
		}

		[Test]
		public void WritingSystemList_AddItems_AddVariation_CustomNameUsed()
		{
			var container = new TestWSContainer(new[] { "en", "fr" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.LanguageName = "Testing";
			var origEnIndex = testModel.CurrentWritingSystemIndex;
			var addMenuItems = testModel.GetAddMenuItems();
			// SUT
			// Add a variation writing system because it doesn't currently require a cache to create properly
			addMenuItems.First(item => item.MenuText.Contains("variation")).ClickHandler.Invoke(this, new EventArgs());
			Assert.That(testModel.CurrentWritingSystemIndex, Is.Not.EqualTo(origEnIndex));
			Assert.That(testModel.LanguageName, Is.StringMatching("Testing"));
		}

		[Test]
		public void WritingSystemList_AddItems_AddVariation_AddAfterSelected()
		{
			var container = new TestWSContainer(new[] { "en", "fr" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			var origEnIndex = testModel.CurrentWritingSystemIndex;
			var addMenuItems = testModel.GetAddMenuItems();
			// SUT
			// Creating an IPA WS currently requires a Cache. Test "Create a new variation"
			addMenuItems.First(item => item.MenuText.Contains("variation")).ClickHandler.Invoke(this, new EventArgs());
			Assert.That(testModel.CurrentWritingSystemIndex, Is.EqualTo(origEnIndex + 1));
		}

		[Test]
		public void WritingSystemList_AddItems_AddAudio_AddAfterSelected()
		{
			var container = new TestWSContainer(new[] { "en", "fr" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			var origEnIndex = testModel.CurrentWritingSystemIndex;
			var addMenuItems = testModel.GetAddMenuItems();
			// SUT
			// Add an audio writing system because it currently doesn't require a cache to create properly
			addMenuItems.First(item => item.MenuText.Contains("Audio")).ClickHandler.Invoke(this, new EventArgs());
			Assert.That(testModel.CurrentWritingSystemIndex, Is.EqualTo(origEnIndex + 1));
			CollectionAssert.AreEqual(new [] { "en", "en-Zxxx-x-audio", "fr"}, testModel.WorkingList.Select(ws => ws.WorkingWs.LanguageTag));
		}

		[Test]
		public void Model_NewWritingSystemAddedInManagerAndList()
		{
			// Set up mocks to verify wsManager save behavior
			var mockWsManager = MockRepository.GenerateMock<IWritingSystemManager>();
			mockWsManager.Expect(manager => manager.Replace(Arg<CoreWritingSystemDefinition>.Is.Anything)).WhenCalled(a => { }).Repeat.Once();

			var container = new TestWSContainer(new[] { "en" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular, mockWsManager);
			// no-op handling of importing lists for new writing system
			testModel.ImportListForNewWs = import => { };
			var french = new CoreWritingSystemDefinition("fr");
			testModel.WorkingList.Add(new WSListItemModel(true, null, french));

			testModel.Save();

			Assert.That(2, Is.EqualTo(container.VernacularWritingSystems.Count));
			mockWsManager.AssertWasCalled(manager => manager.Replace(french));
		}

		[Test]
		public void Model_ChangedWritingSystemIdSetInManager()
		{
			// Set up mocks to verify wsManager save behavior
			var mockWsManager = MockRepository.GenerateMock<IWritingSystemManager>();
			mockWsManager.Expect(manager => manager.Replace(Arg<CoreWritingSystemDefinition>.Is.Anything)).WhenCalled(a => { }).Repeat.Once();

			var container = new TestWSContainer(new[] { "es", "fr" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular, mockWsManager);
			var enWs = container.VernacularWritingSystems.First();
			testModel.ShowChangeLanguage = ShowChangeLanguage;
			testModel.ChangeLanguage();
			testModel.Save();

			Assert.That(2, Is.EqualTo(container.VernacularWritingSystems.Count));
			mockWsManager.AssertWasCalled(manager => manager.Replace(enWs));
		}

		[Test]
		public void Model_ChangesContainerOnlyOnSave()
		{
			// Set up mocks to verify wsManager save behavior
			var mockWsManager = MockRepository.GenerateMock<IWritingSystemManager>();
			mockWsManager.Expect(manager => manager.Save()).WhenCalled(a => { }).Repeat.Once();

			var container = new TestWSContainer(new[] {"fr", "fr-FR", "fr-Zxxx-x-audio"});
			var testModel = new FwWritingSystemSetupModel(container,
				FwWritingSystemSetupModel.ListType.Vernacular, mockWsManager);
			// Start changing stuff like crazy
			testModel.CurrentWsSetupModel.CurrentAbbreviation = "free.";
			testModel.CurrentWsSetupModel.CurrentCollationRulesType = "CustomSimple";
			testModel.CurrentWsSetupModel.CurrentCollationRules = "Z z Y y X x";
			// verify that the container WorkingWs defs have not changed
			Assert.AreEqual("fr", container.VernacularWritingSystems.First().Abbreviation);
			Assert.AreEqual("standard",
				container.VernacularWritingSystems.First().DefaultCollationType);
			Assert.IsNull(container.VernacularWritingSystems.First().DefaultCollation);
			testModel.Save();
			// verify that the container WorkingWs defs have changed
			mockWsManager.VerifyAllExpectations();
			Assert.AreEqual("free.", container.VernacularWritingSystems.First().Abbreviation);
			Assert.NotNull(container.VernacularWritingSystems.First().DefaultCollation);
			Assert.AreEqual("Z z Y y X x", ((SimpleRulesCollationDefinition) container.VernacularWritingSystems.First().DefaultCollation).SimpleRules);
		}

		[Test]
		public void Model_WritingSystemListUpdated_CalledOnChange()
		{
			var writingSystemListUpdatedCalled = false;
			var mockWsManager = MockRepository.GenerateMock<IWritingSystemManager>();

			var container = new TestWSContainer(new[] { "fr", "fr-FR", "fr-Zxxx-x-audio" });
			var testModel = new FwWritingSystemSetupModel(container,
				FwWritingSystemSetupModel.ListType.Vernacular, mockWsManager);
			testModel.WritingSystemListUpdated += (sender, args) =>
			{
				writingSystemListUpdatedCalled = true;
			};
			// Make a change that should notify listeners (refresh the lexicon view to move ws labels for instance)
			testModel.MoveDown();
			testModel.Save();
			Assert.True(writingSystemListUpdatedCalled, "WritingSystemListUpdated should have been called after this change");
		}

		[TestCase(FwWritingSystemSetupModel.ListType.Vernacular)]
		[TestCase(FwWritingSystemSetupModel.ListType.Analysis)]
		public void WritingSystemTitle_ChangesByType(FwWritingSystemSetupModel.ListType type)
		{
			var container = new TestWSContainer(new [] { "en" }, new [] { "fr" });
			var testModel = new FwWritingSystemSetupModel(container, type);
			Assert.That(testModel.Title, Is.StringContaining(string.Format("{0} Writing System Properties", type)));
		}

		[Test]
		public void LanguageName_ChangesAllRelated()
		{
			var container = new TestWSContainer(new[] { "en", "en-GB", "en-fonipa" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.SelectWs("en-GB");
			var newLangName = "Ingrish";
			testModel.LanguageName = newLangName;
			Assert.AreEqual(newLangName, testModel.CurrentWsSetupModel.CurrentLanguageName);
			Assert.That(testModel.LanguageCode, Is.Not.StringContaining("qaa"),
				"Changing the name should not change the language to private use");
			testModel.SelectWs("en");
			Assert.AreEqual(newLangName, testModel.CurrentWsSetupModel.CurrentLanguageName);
			Assert.That(testModel.CurrentWsSetupModel.CurrentLanguageTag, Is.StringMatching("en"));
			Assert.That(testModel.LanguageCode, Is.Not.StringContaining("qaa"),
				"Changing the name should not change the language to private use");
			testModel.SelectWs("en-fonipa");
			Assert.AreEqual(newLangName, testModel.CurrentWsSetupModel.CurrentLanguageName);
			Assert.That(testModel.LanguageCode, Is.Not.StringContaining("qaa"),
				"Changing the name should not change the language to private use");
		}

		[Test]
		public void LanguageName_DoesNotChangeUnRelated()
		{
			var container = new TestWSContainer(new[] { "fr", "en-GB", "en-fonipa" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.SelectWs("en-GB");
			var newLangName = "Ingrish";
			testModel.LanguageName = newLangName;
			Assert.AreEqual(newLangName, testModel.CurrentWsSetupModel.CurrentLanguageName);
			testModel.SelectWs("fr");
			Assert.AreEqual("French", testModel.CurrentWsSetupModel.CurrentLanguageName);
		}

		[Test]
		public void WritingSystemName_ChangesOnSwitch()
		{
			var container = new TestWSContainer(new[] { "fr", "en-GB", "en-fonipa" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.SelectWs("en-GB");
			Assert.AreEqual("English (United Kingdom)", testModel.WritingSystemName);
			testModel.SelectWs("en-fonipa");
			Assert.AreEqual("English (International Phonetic Alphabet)", testModel.WritingSystemName);
		}

		[Test]
		public void RightToLeft_ChangesOnSwitch()
		{
			var container = new TestWSContainer(new[] { "fr", "en-GB", "en-fonipa" });
			var fr = container.CurrentVernacularWritingSystems.First();
			fr.RightToLeftScript = true;
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.SelectWs("fr");
			Assert.IsTrue(testModel.CurrentWsSetupModel.CurrentRightToLeftScript);
			testModel.SelectWs("en-fonipa");
			Assert.IsFalse(testModel.CurrentWsSetupModel.CurrentRightToLeftScript);
		}

		[Test]
		public void CurrentWritingSystemIndex_IntiallyZero()
		{
			var container = new TestWSContainer(new[] { "fr", "en-GB", "en-fonipa" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			Assert.AreEqual(0, testModel.CurrentWritingSystemIndex);
		}

		[TestCase(new[] {"en", "fr"}, "en", 0)]
		[TestCase(new[] {"en", "fr"}, "fr", 1)]
		public void CurrentWritingSystemIndex(string[] list, string current, int index)
		{
			var container = new TestWSContainer(list);
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.SelectWs(current);
			Assert.AreEqual(testModel.CurrentWritingSystemIndex, index);
		}

		[Test]
		public void ChangeLanguage_ChangesAllRelated()
		{
			var container = new TestWSContainer(new[] { "fr", "fr-Arab", "fr-GB", "fr-fonipa", "es" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.SelectWs("fr-GB");
			testModel.ShowChangeLanguage = ShowChangeLanguage;
			testModel.ChangeLanguage();
			Assert.AreEqual("TestName", testModel.CurrentWsSetupModel.CurrentLanguageName);
			Assert.AreEqual("TestName", testModel.LanguageName);
			testModel.SelectWs("auc-fonipa");
			Assert.AreEqual("TestName", testModel.CurrentWsSetupModel.CurrentLanguageName);
			testModel.SelectWs("auc-Arab");
			Assert.AreEqual("TestName", testModel.CurrentWsSetupModel.CurrentLanguageName);
			testModel.SelectWs("es");
			Assert.AreEqual("Spanish", testModel.CurrentWsSetupModel.CurrentLanguageName);
		}

		[Test]
		public void ChangeLanguage_DoesNotChangeIfWouldCreateDuplicate()
		{
			var container = new TestWSContainer(new[] { "es", "es-PR", "es-fonipa", "auc" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.SelectWs("es-PR");
			string errorMessage = null;
			testModel.ShowMessageBox = text => { errorMessage = text; };
			testModel.ShowChangeLanguage = ShowChangeLanguage;
			testModel.ChangeLanguage();
			Assert.AreEqual("Spanish", testModel.CurrentWsSetupModel.CurrentLanguageName);
			testModel.SelectWs("es");
			Assert.AreEqual("Spanish", testModel.CurrentWsSetupModel.CurrentLanguageName);
			StringAssert.Contains("writing system already exists", errorMessage);
		}

		[Test]
		public void ChangeLanguage_DoesNotChangEnglish()
		{
			var container = new TestWSContainer(new[] { "en", "en-GB", "en-fonipa" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.SelectWs("en-GB");
			string errorMessage = null;
			testModel.ShowMessageBox = text => { errorMessage = text; };
			testModel.ShowChangeLanguage = ShowChangeLanguage;
			testModel.ChangeLanguage();
			Assert.AreEqual("English", testModel.CurrentWsSetupModel.CurrentLanguageName);
			Assert.AreEqual("en-GB", testModel.CurrentWsSetupModel.CurrentLanguageTag);
			Assert.AreEqual(FwCoreDlgs.kstidCantChangeEnglishWS, errorMessage);
		}

		[Test]
		public void ChangeLanguage_ChangingDefaultVernacularWorks()
		{
			var langProj = Cache.LangProject;
			var wsManager = Cache.ServiceLocator.WritingSystemManager;
			var entryFactory = Cache.ServiceLocator.GetInstance<ILexEntryFactory>();
			IMoMorphType stem = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphStem);
			entryFactory.Create(stem, TsStringUtils.MakeString("form1", Cache.DefaultVernWs), "gloss1", new SandboxGenericMSA());

			Cache.ActionHandlerAccessor.EndUndoTask();
			var testModel = new FwWritingSystemSetupModel(langProj, FwWritingSystemSetupModel.ListType.Vernacular, wsManager, Cache);
			testModel.SelectWs("fr");
			testModel.ShowChangeLanguage = ShowChangeLanguage;
			testModel.ChangeLanguage();
			Assert.AreEqual("TestName", testModel.CurrentWsSetupModel.CurrentLanguageName);
			Assert.DoesNotThrow(() => testModel.Save());
			Assert.AreEqual("auc", langProj.CurVernWss);
		}

		/// <summary>Simulates the user changing the language to Waorani "TestName" (auc)</summary>
		private static bool ShowChangeLanguage(out LanguageInfo info)
		{
			info = new LanguageInfo { DesiredName = "TestName", ThreeLetterTag = "auc", LanguageTag = "auc" };
			return true;
		}

		[Test]
		public void LanguageCode_ChangesOnSwitch()
		{
			var container = new TestWSContainer(new[] { "en", "en-GB", "fr-fonipa" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.SelectWs("en-GB");
			Assert.AreEqual("eng", testModel.LanguageCode);
			testModel.SelectWs("fr-fonipa");
			Assert.AreEqual("fra", testModel.LanguageCode);
		}

		[Test]
		public void EthnologueLink_UsesLanguageCode()
		{
			var container = new TestWSContainer(new[] { "fr-Arab-GB-fonipa-x-bogus" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			StringAssert.EndsWith("fra", testModel.EthnologueLabel, "Label didn't end with language code");
			StringAssert.EndsWith("fra", testModel.EthnologueLink, "Link didn't end with language code");
		}

		[Test]
		public void EthnologueLink_ChangesOnSwitch()
		{
			var container = new TestWSContainer(new[] { "en", "fr" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			StringAssert.EndsWith("eng", testModel.EthnologueLabel, "Label didn't end with language code");
			StringAssert.EndsWith("eng", testModel.EthnologueLink, "Link didn't end with language code");
			testModel.SelectWs("fr");
			StringAssert.EndsWith("fra", testModel.EthnologueLabel, "Label didn't end with language code");
			StringAssert.EndsWith("fra", testModel.EthnologueLink, "Link didn't end with language code");
		}

		[Test]
		public void Converters_NoEncodingConverters_ReturnsListWithOnlyNone()
		{
			var container = new TestWSContainer(new[] { "en", "fr" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.EncodingConverterKeys = () => new string[] { };
			var converters = testModel.GetEncodingConverters();
			Assert.AreEqual(1, converters.Count);
			Assert.That(converters.First(), Is.StringMatching("\\<None\\>"));
		}

		[Test]
		public void Converters_ModifyEncodingConverters_Ok()
		{
			var container = new TestWSContainer(new[] { "en", "en-GB", "en-fonipa", "fr" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.EncodingConverterKeys = () => { return new [] {"Test2", "Test"}; };
			testModel.ShowModifyEncodingConverters = TestShowModifyConverters;
			testModel.ModifyEncodingConverters();
			Assert.That(testModel.CurrentLegacyConverter, Is.StringMatching("Test"));
		}

		[Test]
		public void Converters_ModifyEncodingConverters_CancelLeavesOriginal()
		{
			var container = new TestWSContainer(new[] { "en", "en-GB", "en-fonipa", "fr" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.EncodingConverterKeys = () => { return new[] { "Test2", "Test" }; };
			testModel.ShowModifyEncodingConverters = TestShowModifyConvertersReturnFalse;
			testModel.CurrentLegacyConverter = "Test2";
			testModel.ModifyEncodingConverters();
			Assert.That(testModel.CurrentLegacyConverter, Is.StringMatching("Test2"));
		}

		[Test]
		public void Converters_ModifyEncodingConverters_CancelSetsToNullIfOldConverterMissing()
		{
			var container = new TestWSContainer(new[] { "en", "en-GB", "en-fonipa", "fr" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.CurrentLegacyConverter = "Test2";
			testModel.EncodingConverterKeys = () => { return new string [] { }; };
			testModel.ShowModifyEncodingConverters = TestShowModifyConvertersReturnFalse;
			testModel.ModifyEncodingConverters();
			Assert.IsNullOrEmpty(testModel.CurrentLegacyConverter);
		}

		[Test]
		public void NumberingSystem_ChangingCurrentNumberingSystemDefinition_Works()
		{
			var container = new TestWSContainer(new[] { "en" });
			container.VernacularWritingSystems.First().NumberingSystem = NumberingSystemDefinition.CreateCustomSystem("abcdefghij");
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			// Verify that the custom system returns custom digits
			Assert.That(testModel.CurrentWsSetupModel.CurrentNumberingSystemDefinition.IsCustom, Is.True);
			Assert.That(testModel.CurrentWsSetupModel.CurrentNumberingSystemDefinition.Digits, Is.StringMatching("abcdefghij"));
			// Test switching to default switches back to default digits
			testModel.CurrentWsSetupModel.CurrentNumberingSystemDefinition = NumberingSystemDefinition.Default;
			Assert.That(testModel.CurrentWsSetupModel.CurrentNumberingSystemDefinition.Digits, Is.StringMatching("0123456789"));
		}

		[Test]
		public void CurrentWsListChanged_NoChanges_Returns_False()
		{
			var container = new TestWSContainer(new[] { "en", "fr", "en-Zxxx-x-audio" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			Assert.IsFalse(testModel.CurrentWsListChanged);
		}

		[Test]
		public void CurrentWsListChanged_MoveSelectedDown_Returns_True()
		{
			var container = new TestWSContainer(new[] { "en", "fr", "en-Zxxx-x-audio" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.MoveDown();
			Assert.True(testModel.CurrentWsListChanged);
		}

		[Test]
		public void CurrentWsListChanged_MoveSelectedUp_Returns_True()
		{
			var container = new TestWSContainer(new[] { "en", "fr", "en-Zxxx-x-audio" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.SelectWs("fr");
			testModel.MoveUp();
			Assert.True(testModel.CurrentWsListChanged);
		}

		[Test]
		public void CurrentWsListChanged_MoveUnSelectedDown_Returns_False()
		{
			var container = new TestWSContainer(new[] { "en", "fr", "en-Zxxx-x-audio" }, null, new[] { "en", "en-Zxxx-x-audio" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.SelectWs("fr");
			testModel.MoveDown();
			Assert.False(testModel.CurrentWsListChanged);
		}

		[Test]
		public void CurrentWsListChanged_MoveUnSelectedUp_Returns_False()
		{
			var container = new TestWSContainer(new[] { "en", "fr", "en-Zxxx-x-audio" }, null, new[] { "en", "en-Zxxx-x-audio" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.SelectWs("fr");
			testModel.MoveUp();
			Assert.False(testModel.CurrentWsListChanged);
		}

		[Test]
		public void CurrentWsListChanged_AddNew_Returns_True()
		{
			var container = new TestWSContainer(new[] { "en", "fr" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.GetAddMenuItems().First(item => item.MenuText.Contains("Audio")).ClickHandler.Invoke(this, new EventArgs());
			Assert.True(testModel.CurrentWsListChanged);
		}

		[Test]
		public void CurrentWsListChanged_UnSelectItem_Returns_True()
		{
			var container = new TestWSContainer(new[] { "en", "fr", "en-Zxxx-x-audio" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.ToggleInCurrentList();
			Assert.IsTrue(testModel.CurrentWsListChanged);
		}

		[Test]
		public void CurrentWsListChanged_SelectItem_Returns_True()
		{
			var container = new TestWSContainer(new[] { "en", "fr", "en-Zxxx-x-audio" }, null, new[] { "en", "en-Zxxx-x-audio" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.SelectWs("fr");
			testModel.ToggleInCurrentList();
			Assert.IsTrue(testModel.CurrentWsListChanged);
		}

		[Test]
		public void CurrentWsListChanged_DeleteSelected_Returns_True()
		{
			var container = new TestWSContainer(new[] { "en", "fr", "en-Zxxx-x-audio" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.ConfirmDeleteWritingSystem = label => { return true; };
			var menu = testModel.GetRightClickMenuItems();
			menu.First(ws => ws.MenuText.Contains("Delete")).ClickHandler(this, EventArgs.Empty);
			Assert.IsTrue(testModel.CurrentWsListChanged);
		}

		/// <summary>
		/// Proves that the delete option should work during the New Project Wizard use of this model/dialog
		/// </summary>
		[Test]
		public void CurrentWsListChanged_DeleteSelected_SaveDoesNotCrashWithNoCache()
		{
			var container = new TestWSContainer(new[] { "en", "fr" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular, new WritingSystemManager());
			testModel.ConfirmDeleteWritingSystem = label => { return true; };
			var menu = testModel.GetRightClickMenuItems();
			menu.First(ws => ws.MenuText.Contains("Delete")).ClickHandler(this, EventArgs.Empty);
			Assert.DoesNotThrow(() => testModel.Save());
			Assert.That(container.VernacularWritingSystems.Count, Is.EqualTo(1));
		}

		/// <summary>
		/// Proves that the merge option should work during the New Project Wizard use of this model/dialog
		/// </summary>
		/// <remarks>The user would never want to do this, but just in case, don't crash.</remarks>
		[Test]
		public void CurrentWsListChanged_MergeSelected_SaveDoesNotCrashWithNoCache()
		{
			var container = new TestWSContainer(new[] { "es", "fr" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular, new WritingSystemManager());
			testModel.ConfirmDeleteWritingSystem = label => { return true; };
			testModel.ConfirmMergeWritingSystem = (string merge, out CoreWritingSystemDefinition tag) => {
				tag = container.CurrentVernacularWritingSystems.First();
				return true;
			};
			var menu = testModel.GetRightClickMenuItems();
			menu.First(ws => ws.MenuText.Contains("Merge")).ClickHandler(this, EventArgs.Empty);
			Assert.DoesNotThrow(() => testModel.Save());
			Assert.That(container.VernacularWritingSystems.Count, Is.EqualTo(1));
		}

		[Test]
		public void CurrentWsListChanged_DeleteUnSelected_Returns_False()
		{
			var container = new TestWSContainer(new[] { "en", "fr", "en-Zxxx-x-audio" }, null, new[] { "en", "en-Zxxx-x-audio" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.SelectWs("fr");
			testModel.ConfirmDeleteWritingSystem = label => { return true; };
			var menu = testModel.GetRightClickMenuItems();
			menu.First(ws => ws.MenuText.Contains("Delete")).ClickHandler(this, EventArgs.Empty);
			Assert.IsFalse(testModel.CurrentWsListChanged);
		}

		[Test]
		public void CurrentWsListChanged_RemoveUnSelected_Returns_False()
		{
			var container = new TestWSContainer(new[] { "en", "fr", "en-Zxxx-x-audio" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			Assert.IsFalse(testModel.CurrentWsListChanged);
		}

		[Test]
		public void CurrentWsListChanged_TopVernIsHomographWs_UncheckedWarnsAndSetsNew()
		{
			SetupHomographLanguagesInCache();
			Cache.ActionHandlerAccessor.EndUndoTask();
			var testModel = new FwWritingSystemSetupModel(Cache.LangProject, FwWritingSystemSetupModel.ListType.Vernacular, Cache.ServiceLocator.WritingSystemManager, Cache);
			var warningShown = false;
			testModel.ShouldChangeHomographWs = ws => { warningShown = true; return true; };
			testModel.ToggleInCurrentList();
			testModel.Save();
			Assert.IsTrue(warningShown, "No homograph ws changed warning shown.");
			Assert.AreEqual(Cache.LangProject.HomographWs, "fr", "Homograph ws not changed.");
		}

		[Test]
		public void CurrentWsListChanged_TopVernIsHomographWs_UncheckedWarnsAndDoesNotSetOnNo()
		{
			SetupHomographLanguagesInCache();
			Cache.ActionHandlerAccessor.EndUndoTask();
			var testModel = new FwWritingSystemSetupModel(Cache.LangProject, FwWritingSystemSetupModel.ListType.Vernacular, Cache.ServiceLocator.WritingSystemManager, Cache);
			var warningShown = false;
			testModel.ShouldChangeHomographWs = ws => { warningShown = true; return false; };
			testModel.ToggleInCurrentList();
			testModel.Save();
			Assert.IsTrue(warningShown, "No homograph ws changed warning shown.");
			Assert.AreEqual(Cache.LangProject.HomographWs, "en", "Homograph ws should not have been changed.");
		}

		[Test]
		public void CurrentWsListChanged_TopVernIsHomographWs_MovedDownWarnsAndSetsNew()
		{
			SetupHomographLanguagesInCache();
			Cache.ActionHandlerAccessor.EndUndoTask();
			var testModel = new FwWritingSystemSetupModel(Cache.LangProject, FwWritingSystemSetupModel.ListType.Vernacular, Cache.ServiceLocator.WritingSystemManager, Cache);
			var warningShown = false;
			testModel.ShouldChangeHomographWs = ws => { warningShown = true; return true; };
			testModel.MoveDown();
			testModel.Save();
			Assert.IsTrue(warningShown, "No homograph ws changed warning shown.");
			Assert.AreEqual(Cache.LangProject.HomographWs, "fr", "Homograph ws not changed.");
		}

		[Test]
		public void CurrentWsListChanged_TopVernIsHomographWs_NewWsAddedAbove_WarnsAndSetsNew()
		{
			SetupHomographLanguagesInCache();
			Cache.ActionHandlerAccessor.EndUndoTask();
			var testModel = new FwWritingSystemSetupModel(Cache.LangProject, FwWritingSystemSetupModel.ListType.Vernacular, Cache.ServiceLocator.WritingSystemManager, Cache);
			var warningShown = false;
			testModel.ShouldChangeHomographWs = ws => { warningShown = true; return true; };
			testModel.ImportListForNewWs = import => { };
			var addMenuItems = testModel.GetAddMenuItems();
			// Add an audio writing system because it currently doesn't require a cache to create properly
			addMenuItems.First(item => item.MenuText.Contains("Audio")).ClickHandler.Invoke(this, new EventArgs());
			testModel.MoveUp(); // move the audio writing system up. It should be first now.
			testModel.Save();
			Assert.IsTrue(warningShown, "No homograph ws changed warning shown.");
			Assert.AreEqual(Cache.LangProject.HomographWs, "en-Zxxx-x-audio", "Homograph ws not changed.");
		}

		[Test]
		public void CurrentWsListChanged_TopVernIsNotHomographWs_UncheckedWarnsAndSetsNew()
		{
			SetupHomographLanguagesInCache();
			Assert.AreEqual("en fr", Cache.LangProject.VernWss, "Test data setup incorrect, english should be first followed by french");
			Assert.AreEqual("en fr", Cache.LangProject.CurVernWss, "Test data setup incorrect, english should be first followed by french");
			Cache.LangProject.HomographWs = "fr";
			Cache.ActionHandlerAccessor.EndUndoTask();
			var testModel = new FwWritingSystemSetupModel(Cache.LangProject, FwWritingSystemSetupModel.ListType.Vernacular, Cache.ServiceLocator.WritingSystemManager, Cache);
			var warningShown = false;
			testModel.ShouldChangeHomographWs = ws => { warningShown = true; return true; };
			testModel.SelectWs("fr");
			testModel.ToggleInCurrentList();
			testModel.Save();
			Assert.IsTrue(warningShown, "No homograph ws changed warning shown.");
			Assert.AreEqual(Cache.LangProject.HomographWs, "en", "Homograph ws not changed.");
		}

		[Test]
		public void CurrentWsListChanged_CurrentVernacularList_ToggleSaved()
		{
			SetupHomographLanguagesInCache();
			Cache.ActionHandlerAccessor.EndUndoTask();
			var testModel = new FwWritingSystemSetupModel(Cache.LangProject, FwWritingSystemSetupModel.ListType.Vernacular, Cache.ServiceLocator.WritingSystemManager, Cache);
			testModel.SelectWs("fr");
			testModel.ToggleInCurrentList();
			testModel.Save();
			Assert.That(Cache.LangProject.CurVernWss, Is.EqualTo("en"), "French should have been removed from the CurrentVernacular list on Save");
		}

		[Test]
		public void CurrentWsListChanged_CurrentVernacularList_ToggleSavedWithOtherChange()
		{
			SetupHomographLanguagesInCache();
			Cache.ActionHandlerAccessor.EndUndoTask();
			var testModel = new FwWritingSystemSetupModel(Cache.LangProject, FwWritingSystemSetupModel.ListType.Vernacular, Cache.ServiceLocator.WritingSystemManager, Cache);
			testModel.SelectWs("fr");
			testModel.CurrentWsSetupModel.CurrentAbbreviation = "fra";
			testModel.ToggleInCurrentList();
			testModel.Save();
			Assert.That(Cache.LangProject.CurVernWss, Is.EqualTo("en"), "French should have been removed from the CurrentVernacular list on Save");
		}

		[Test]
		public void Save_LastWsStaysUnselected_ChangesAreSaved()
		{
			SetupHomographLanguagesInCache();
			var it = GetOrCreateWs("it");
			Cache.LangProject.VernacularWritingSystems.Add(it); // available, but not selected
			Cache.LangProject.HomographWs = "fr"; // so that the HomographWs doesn't change when we deselect en
			Cache.ActionHandlerAccessor.EndUndoTask();
			var testModel = new FwWritingSystemSetupModel(Cache.LangProject, FwWritingSystemSetupModel.ListType.Vernacular, Cache.ServiceLocator.WritingSystemManager, Cache);
			CollectionAssert.AreEqual(new[] { "en", "fr", "it" }, testModel.WorkingList.Select(ws => ws.OriginalWs.LanguageTag));
			CollectionAssert.AreEqual(new[] { true, true, false }, testModel.WorkingList.Select(ws => ws.InCurrentList));
			testModel.ToggleInCurrentList();
			CollectionAssert.AreEqual(new[] { false, true, false }, testModel.WorkingList.Select(ws => ws.InCurrentList));
			// SUT
			testModel.Save();
			Assert.AreEqual("fr", Cache.LangProject.CurVernWss, "Only French should remain selected after save");
		}

		/// <summary>
		/// Adds en and fr to Current Vernacular and sets en as Homograph WS.
		/// The client must call <c>Cache.ActionHandlerAccessor.EndUndoTask()</c> after this and any additional setup.
		/// </summary>
		private void SetupHomographLanguagesInCache()
		{
			var en = GetOrCreateWs("en");
			var fr = GetOrCreateWs("fr");
			Cache.LangProject.CurrentVernacularWritingSystems.Clear();
			Cache.LangProject.CurrentVernacularWritingSystems.Add(en);
			Cache.LangProject.CurrentVernacularWritingSystems.Add(fr);
			Cache.LangProject.VernacularWritingSystems.Clear();
			Cache.LangProject.VernacularWritingSystems.AddRange(Cache.LangProject.CurrentVernacularWritingSystems);
			Cache.LangProject.HomographWs = "en";
		}

		private CoreWritingSystemDefinition GetOrCreateWs(string code)
		{
			CoreWritingSystemDefinition ws;
			if (!Cache.ServiceLocator.WritingSystemManager.TryGet(code, out ws))
			{
				ws = Cache.ServiceLocator.WritingSystemManager.Create(code);
			}
			Cache.ServiceLocator.WritingSystemManager.Set(ws);
			return ws;
		}

		private bool TestShowModifyConverters(string originalconverter, out string selectedconverter)
		{
			selectedconverter = "Test";
			return true;
		}

		private bool TestShowModifyConvertersReturnFalse(string originalconverter, out string selectedconverter)
		{
			selectedconverter = null;
			return false;
		}

		internal class TestWSContainer : IWritingSystemContainer
		{
			private IWritingSystemContainer _writingSystemContainerImplementation;

			private List<CoreWritingSystemDefinition> _vernacular = new List<CoreWritingSystemDefinition>();
			private List<CoreWritingSystemDefinition> _analysis = new List<CoreWritingSystemDefinition>();
			private List<CoreWritingSystemDefinition> _curVern = new List<CoreWritingSystemDefinition>();
			private List<CoreWritingSystemDefinition> _curAnaly = new List<CoreWritingSystemDefinition>();

			public TestWSContainer(string[] vernacular, string[] analysis = null, string[] curVern = null, string[] curAnaly = null)
			{
				foreach (var lang in vernacular)
				{
					var ws = new CoreWritingSystemDefinition(lang) { Id = lang };
					_vernacular.Add(ws);
					if (curVern == null)
					{
						_curVern.Add(ws);
					}
				}

				if (analysis != null)
				{
					foreach (var lang in analysis)
					{
						var ws = new CoreWritingSystemDefinition(lang) { Id = lang };
						_analysis.Add(ws);
						if (curAnaly == null)
						{
							_curAnaly.Add(ws);
						}
					}
				}

				if (curVern != null)
				{
					foreach (var lang in curVern)
					{
						_curVern.Add(new CoreWritingSystemDefinition(lang) { Id = lang });
					}
				}
				if (curAnaly != null)
				{
					foreach (var lang in curAnaly)
					{
						_curAnaly.Add(new CoreWritingSystemDefinition(lang) { Id = lang });
					}
				}
				Repo = new TestLdmlInXmlWritingSystemRepository();
			}

			public TestWSContainer(CoreWritingSystemDefinition[] vernacular)
			{
				_vernacular.AddRange(vernacular);
			}

			public void AddToCurrentAnalysisWritingSystems(CoreWritingSystemDefinition ws)
			{
				throw new System.NotImplementedException();
			}

			public void AddToCurrentVernacularWritingSystems(CoreWritingSystemDefinition ws)
			{
				throw new System.NotImplementedException();
			}

			public IEnumerable<CoreWritingSystemDefinition> AllWritingSystems { get; }
			public ICollection<CoreWritingSystemDefinition> AnalysisWritingSystems => _analysis;

			public ICollection<CoreWritingSystemDefinition> VernacularWritingSystems => _vernacular;
			public IList<CoreWritingSystemDefinition> CurrentAnalysisWritingSystems => _curAnaly;
			public IList<CoreWritingSystemDefinition> CurrentVernacularWritingSystems => _curVern;
			public IList<CoreWritingSystemDefinition> CurrentPronunciationWritingSystems { get; }
			public CoreWritingSystemDefinition DefaultAnalysisWritingSystem { get; set; }
			public CoreWritingSystemDefinition DefaultVernacularWritingSystem { get; set; }
			public CoreWritingSystemDefinition DefaultPronunciationWritingSystem { get; }
			/// <summary>
			/// Test repo
			/// </summary>
			public IWritingSystemRepository Repo { get; set; }
		}
	}
}

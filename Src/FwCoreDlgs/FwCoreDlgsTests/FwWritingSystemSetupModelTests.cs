// Copyright (c) 2019-2021 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Xml;
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
using XCore;

namespace SIL.FieldWorks.FwCoreDlgs
{
	internal class FwWritingSystemSetupModelTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{

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
				ShowMessageBox = (text, isResponseRequested) => { errorMessage = text; Assert.False(isResponseRequested); return false; }
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
			Assert.That(testModel.CurrentWsSetupModel.CurrentRegionTag, Is.Null);
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
			CollectionAssert.AreEqual(new[] { "Merge...", "Update Spanish", "Hide Spanish", "Delete Spanish" }, menu);
			testModel.SelectWs("fr");
			menu = testModel.GetRightClickMenuItems().Select(item => item.MenuText);
			CollectionAssert.AreEqual(new[] { "Merge...", "Update French", "Hide French", "Delete French" }, menu);
		}

		[TestCase(FwWritingSystemSetupModel.ListType.Vernacular, true)]
		[TestCase(FwWritingSystemSetupModel.ListType.Analysis, false)]
		public void WritingSystemList_RightClickMenuItems_CannotMergeOrDeleteEnglishAnalyWs(FwWritingSystemSetupModel.ListType type, bool canDelete)
		{
			var container = new TestWSContainer(new[] { "en", "fr" }, new[] { "en", "fr" });
			var testModel = new FwWritingSystemSetupModel(container, type);
			var menu = testModel.GetRightClickMenuItems();
			Assert.That(!menu.Any(m => m.MenuText.Contains("Merge")));
			Assert.That(menu.First(m => m.MenuText.StartsWith("Hide")).IsEnabled, Is.EqualTo(canDelete), "English can be hidden from the Vernacular but not the Analysis WS List");
			Assert.That(menu.First(m => m.MenuText.StartsWith("Hide")).MenuText, /* REVIEW (Hasso) contain? */ Is.EqualTo("Hide English"));
			Assert.That(menu.First(m => m.MenuText.StartsWith("Delete")).IsEnabled, Is.EqualTo(canDelete), "English can be deleted from the Vernacular but not the Analysis WS List");
			Assert.That(menu.First(m => m.MenuText.StartsWith("Delete")).MenuText, /* REVIEW (Hasso) contain? */ Is.EqualTo("Delete English"));
		}

		[Test]
		public void WritingSystemList_RightClickMenuItems_NoMergeForSingleWs()
		{
			var container = new TestWSContainer(new[] { "fr" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			var menu = testModel.GetRightClickMenuItems();
			Assert.That(menu.Count, Is.EqualTo(3));
			Assert.IsFalse(menu.First(m => m.MenuText.StartsWith("Hide")).IsEnabled);
			Assert.That(menu.First(m => m.MenuText.StartsWith("Hide")).MenuText, /* REVIEW (Hasso) contain? */ Is.EqualTo("Hide French"));
			Assert.IsFalse(menu.First(m => m.MenuText.StartsWith("Delete")).IsEnabled);
			Assert.That(menu.First(m => m.MenuText.StartsWith("Delete")).MenuText, /* REVIEW (Hasso) contain? */ Is.EqualTo("Delete French"));
		}

		[Test]
		public void WritingSystemList_RightClickMenuItems_NewWs()
		{
			var container = new TestWSContainer(new[] { "es" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			var french = new CoreWritingSystemDefinition("fr");
			testModel.WorkingList.Add(new WSListItemModel(true, null, french));
			testModel.SelectWs("fr");
			var menu = testModel.GetRightClickMenuItems();
			Assert.That(menu.First(m => m.MenuText.StartsWith("Update")).IsEnabled, Is.False, "Update should be disabled");
			Assert.That(menu.First(m => m.MenuText.StartsWith("Hide")).IsEnabled, Is.False, "Hide should be disabled");
		}

		[Test]
		public void WritingSystemList_RightClickMenuItems_ExistingWs()
		{
			var container = new TestWSContainer(new[] { "es", "fr" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			var menu = testModel.GetRightClickMenuItems();
			Assert.That(menu.First(m => m.MenuText.StartsWith("Update")).IsEnabled, Is.True, "Update should be enabled");
			Assert.That(menu.First(m => m.MenuText.StartsWith("Hide")).IsEnabled, Is.True, "Hide should be enabled");
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
		public void WritingSystemList_AddMenuItems_AddLanguageWarnsForVernacular()
		{
			bool warned = false;
			var container = new TestWSContainer(new[] { "en" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.AddNewVernacularLanguageWarning = () =>
			{
				warned = true;
				return false;
			};
			var addLanguageMenu = testModel.GetAddMenuItems().First(item => item.MenuText.Contains("Add new language"));
			addLanguageMenu.ClickHandler.Invoke(null, null); // 'click' on the menu item
			Assert.IsTrue(warned, "Warning not displayed.");
		}

		[Test]
		public void WritingSystemList_AddMenuItems_AddLanguageDoesNotWarnForAnalysis()
		{
			bool warned = false;
			var container = new TestWSContainer(new[] { "en" }, new []{ "fr" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Analysis);
			testModel.AddNewVernacularLanguageWarning = () =>
			{
				warned = true;
				return false;
			};
			testModel.ShowChangeLanguage = (out LanguageInfo info) =>
			{
				info = null;
				return false;
			};
			var addLanguageMenu = testModel.GetAddMenuItems().First(item => item.MenuText.Contains("Add new language"));
			addLanguageMenu.ClickHandler.Invoke(null, null); // 'click' on the menu item
			Assert.IsFalse(warned, "Warning incorrectly displayed.");
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
			var container = new TestWSContainer(new[] { "en-fonipa", "en" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			var addMenu = testModel.GetAddMenuItems().Select(item => item.MenuText);
			CollectionAssert.AreEqual(new[] { "Add Audio for English", "Add variation of English", "Add new language..." }, addMenu);
		}

		[Test]
		public void WritingSystemList_AddMenuItems_ShowHiddenWritingSystemsWithCache()
		{
			var testModel = new FwWritingSystemSetupModel(Cache.LangProject, FwWritingSystemSetupModel.ListType.Analysis,
				Cache.ServiceLocator.WritingSystemManager, Cache);
			var addMenu = testModel.GetAddMenuItems().Select(item => item.MenuText);
			CollectionAssert.AreEqual(new []
			{
				"Add IPA for English", "Add Audio for English", "Add variation of English", "Add new language...", "View hidden Writing Systems..."
			}, addMenu);

			SetupHomographLanguagesInCache();
			testModel = new FwWritingSystemSetupModel(Cache.LangProject, FwWritingSystemSetupModel.ListType.Vernacular,
				Cache.ServiceLocator.WritingSystemManager, Cache);
			testModel.SelectWs("fr");
			addMenu = testModel.GetAddMenuItems().Select(item => item.MenuText);
			CollectionAssert.AreEqual(new[]
			{
				"Add IPA for French", "Add Audio for French", "Add variation of French", "Add new language...", "View hidden Writing Systems..."
			}, addMenu);
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

		[Test]
		public void MoveItem_NewOrderSaved()
		{
			SetupHomographLanguagesInCache();
			Cache.ActionHandlerAccessor.EndUndoTask();
			var langProj = Cache.LangProject;
			Assert.AreEqual("en fr", langProj.VernWss, "setup problem");
			var testModel = new FwWritingSystemSetupModel(langProj, FwWritingSystemSetupModel.ListType.Vernacular,
				Cache.ServiceLocator.WritingSystemManager, Cache)
				{ ShouldChangeHomographWs = ws => true };
			testModel.MoveDown();
			testModel.Save();
			Assert.AreEqual("fr en", langProj.CurVernWss, "current");
			Assert.AreEqual("fr en", langProj.VernWss, "all");
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
			Assert.That(testModel.FirstDuplicateWs, Is.Null);
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
			var mergeTargets = testModel.MergeTargets.ToArray();
			Assert.That(mergeTargets.Length, Is.EqualTo(1));
			Assert.That(mergeTargets[0].WorkingWs.LanguageTag, /* REVIEW (Hasso) contain? */ Is.EqualTo("fr"));
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
			Assert.That(testModel.LanguageName, /* REVIEW (Hasso) contain? */ Is.EqualTo("Testing"));
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
			Assert.That(testModel.LanguageName, /* REVIEW (Hasso) contain? */ Is.EqualTo("Testing"));
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
			Assert.That(container.VernacularWritingSystems.First().DefaultCollation, Is.Null);
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

		[Test]
		public void Model_WritingSystemChanged_CalledOnAbbrevChange()
		{
			var writingSystemChanged = false;
			var mockWsManager = MockRepository.GenerateMock<IWritingSystemManager>();

			var container = new TestWSContainer(new[] { "fr" });
			var testModel = new FwWritingSystemSetupModel(container,
				FwWritingSystemSetupModel.ListType.Vernacular, mockWsManager);
			testModel.WritingSystemUpdated += (sender, args) =>
			{
				writingSystemChanged = true;
			};
			// Make a change that should notify listeners (refresh the lexicon view for instance)
			testModel.CurrentWsSetupModel.CurrentAbbreviation = "fra";
			testModel.Save();
			Assert.True(writingSystemChanged, "WritingSystemUpdated should have been called after this change");
		}

		[Test]
		public void Model_WritingSystemChanged_CalledOnWsIdChange()
		{
			var writingSystemChanged = false;
			var mockWsManager = MockRepository.GenerateMock<IWritingSystemManager>();

			var container = new TestWSContainer(new[] { "fr" });
			var testModel = new FwWritingSystemSetupModel(container,
				FwWritingSystemSetupModel.ListType.Vernacular, mockWsManager);
			testModel.WritingSystemUpdated += (sender, args) =>
			{
				writingSystemChanged = true;
			};
			// Make a change that should notify listeners (refresh the lexicon view for instance)
			testModel.CurrentWsSetupModel.CurrentRegion = "US";
			testModel.Save();
			Assert.True(writingSystemChanged, "WritingSystemUpdated should have been called after this change");
		}

		[Test]
		public void Model_WritingSystemChanged_NotCalledOnIrrelevantChange()
		{
			var writingSystemChanged = false;
			var mockWsManager = MockRepository.GenerateMock<IWritingSystemManager>();

			var container = new TestWSContainer(new[] { "fr" });
			var testModel = new FwWritingSystemSetupModel(container,
				FwWritingSystemSetupModel.ListType.Vernacular, mockWsManager);
			testModel.WritingSystemUpdated += (sender, args) =>
			{
				writingSystemChanged = true;
			};
			// Make a change that should notify listeners (refresh the lexicon view for instance)
			// ReSharper disable once StringLiteralTypo - Leave me alone ReSharper, it's French!
			testModel.CurrentWsSetupModel.CurrentSpellCheckingId = "aucun";
			testModel.Save();
			Assert.False(writingSystemChanged, "WritingSystemUpdated should not have been called after this change");
		}

		[TestCase(FwWritingSystemSetupModel.ListType.Vernacular)]
		[TestCase(FwWritingSystemSetupModel.ListType.Analysis)]
		public void WritingSystemTitle_ChangesByType(FwWritingSystemSetupModel.ListType type)
		{
			var container = new TestWSContainer(new [] { "en" }, new [] { "fr" });
			var testModel = new FwWritingSystemSetupModel(container, type);
			Assert.That(testModel.Title, Does.Contain(string.Format("{0} Writing System Properties", type)));
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
			Assert.That(testModel.LanguageCode, Does.Not.Contain("qaa"),
				"Changing the name should not change the language to private use");
			testModel.SelectWs("en");
			Assert.AreEqual(newLangName, testModel.CurrentWsSetupModel.CurrentLanguageName);
			Assert.That(testModel.CurrentWsSetupModel.CurrentLanguageTag, /* REVIEW (Hasso) contain? */ Is.EqualTo("en"));
			Assert.That(testModel.LanguageCode, Does.Not.Contain("qaa"),
				"Changing the name should not change the language to private use");
			testModel.SelectWs("en-fonipa");
			Assert.AreEqual(newLangName, testModel.CurrentWsSetupModel.CurrentLanguageName);
			Assert.That(testModel.LanguageCode, Does.Not.Contain("qaa"),
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

		[TestCase(true)]
		[TestCase(false)]
		public void ChangeLanguage_WarnsBeforeCreatingDuplicate(bool userWantsToChangeAnyway)
		{
			var container = new TestWSContainer(new[] { "es", "es-PR", "es-fonipa", "auc" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.SelectWs("es-PR");
			string errorMessage = null;
			testModel.ShowMessageBox = (text, isResponseRequested) => { errorMessage = text; Assert.True(isResponseRequested); return userWantsToChangeAnyway; };
			testModel.ShowChangeLanguage = ShowChangeLanguage;

			// SUT
			testModel.ChangeLanguage();

			if (userWantsToChangeAnyway)
			{
				Assert.AreEqual("TestName", testModel.CurrentWsSetupModel.CurrentLanguageName);
				testModel.SelectWs("auc-fonipa");
				Assert.AreEqual("TestName", testModel.CurrentWsSetupModel.CurrentLanguageName, "all WS's for the language should change");
			}
			else
			{
				Assert.AreEqual("Spanish", testModel.CurrentWsSetupModel.CurrentLanguageName);
				testModel.SelectWs("es");
				Assert.AreEqual("Spanish", testModel.CurrentWsSetupModel.CurrentLanguageName, "other WS's shouldn't have changed, either");
				testModel.SelectWs("es-fonipa");
				Assert.AreEqual("Spanish", testModel.CurrentWsSetupModel.CurrentLanguageName, "variant WS's shouldn't have changed, either");
			}
			StringAssert.Contains("This project already has a writing system with the language code", errorMessage);
		}

		[Test]
		public void ChangeLanguage_WarnsBeforeCreatingDuplicate_FunnyOldScript()
		{
			var container = new TestWSContainer(new[] { "es", "auc-Grek" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.SelectWs("es");
			string errorMessage = null;
			testModel.ShowMessageBox = (text, isResponseRequested) => { errorMessage = text; Assert.True(isResponseRequested); return true; };
			testModel.ShowChangeLanguage = ShowChangeLanguage;

			// SUT
			testModel.ChangeLanguage();

			Assert.AreEqual("TestName", testModel.CurrentWsSetupModel.CurrentLanguageName);
			testModel.SelectWs("auc");
			Assert.AreEqual("TestName", testModel.CurrentWsSetupModel.CurrentLanguageName, "the code should have changed");
			StringAssert.Contains("This project already has a writing system with the language code", errorMessage);
		}

		[Test]
		public void ChangeLanguage_WarnsBeforeCreatingDuplicate_FunnyNewScript()
		{
			var container = new TestWSContainer(new[] { "es", "ja" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.SelectWs("es");
			string errorMessage = null;
			testModel.ShowMessageBox = (text, isResponseRequested) => { errorMessage = text; Assert.True(isResponseRequested); return true; };
			const string tagWithScript = "ja-Brai";
			const string desiredName = "Braille for Japanese";
			testModel.ShowChangeLanguage = (out LanguageInfo info) =>
			{
				info = new LanguageInfo { DesiredName = desiredName, LanguageTag = tagWithScript };
				return true;
			};

			// SUT
			testModel.ChangeLanguage();

			Assert.AreEqual(desiredName, testModel.CurrentWsSetupModel.CurrentLanguageName);
			testModel.SelectWs(tagWithScript);
			Assert.AreEqual(desiredName, testModel.CurrentWsSetupModel.CurrentLanguageName, "the code should have changed");
			StringAssert.Contains("This project already has a writing system with the language code", errorMessage);
		}

		[Test]
		public void ChangeLanguage_DoesNotChangEnglish()
		{
			var container = new TestWSContainer(new[] { "en", "en-GB", "en-fonipa" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.SelectWs("en-GB");
			string errorMessage = null;
			testModel.ShowMessageBox = (text, isResponseRequested) => { errorMessage = text; Assert.False(isResponseRequested); return false; };
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

		/// <summary>
		/// When changing the language code on a WS, any selected script or region should remain,
		/// but if the default for the old language had been selected, then the default for the new language should become selected.
		/// </summary>
		[TestCase("fr", "es", "es")] // French to Spanish (both Latin by default)
		[TestCase("fr-CA", "en", "en-CA")] // Canadian French to English retains Canadian region
		[TestCase("fr", "el", "el")] // French to Greek changes Latin to Greek script (Latin defaults dropped)
		[TestCase("el", "ja", "ja")] // Greek to Japanese changes Greek to Japanese script (non-Latin defaults dropped)
		[TestCase("el-Latn", "ja", "ja-Latn")] // Nondefault scripts retained
		[TestCase("el-Latn", "es", "es")] // Nondefault script is the default for the new language: no redundant Script code
		public void ChangeLanguage_CorrectScriptSelected(string oldWs, string newLang, string expectedWs)
		{
			var container = new TestWSContainer(new[] { oldWs });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.SelectWs(oldWs);
			testModel.ShowChangeLanguage = (out LanguageInfo info) =>
			{
				info = new LanguageInfo { LanguageTag = newLang };
				return true;
			};

			// SUT
			testModel.ChangeLanguage();
			Assert.AreEqual(expectedWs, testModel.CurrentWsSetupModel.CurrentLanguageTag);
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
			Assert.That(converters.First(), /* REVIEW (Hasso) contain? */ Is.EqualTo("<None>"));
		}

		[Test]
		public void Converters_ModifyEncodingConverters_Ok()
		{
			var container = new TestWSContainer(new[] { "en", "en-GB", "en-fonipa", "fr" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.EncodingConverterKeys = () => { return new [] {"Test2", "Test"}; };
			testModel.ShowModifyEncodingConverters = TestShowModifyConverters;
			testModel.ModifyEncodingConverters();
			Assert.That(testModel.CurrentLegacyConverter, /* REVIEW (Hasso) contain? */ Is.EqualTo("Test"));
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
			Assert.That(testModel.CurrentLegacyConverter, /* REVIEW (Hasso) contain? */ Is.EqualTo("Test2"));
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
			Assert.That(testModel.CurrentLegacyConverter, Is.Null.Or.Empty);
		}

		[Test]
		public void NumberingSystem_ChangingCurrentNumberingSystemDefinition_Works()
		{
			var container = new TestWSContainer(new[] { "en" });
			container.VernacularWritingSystems.First().NumberingSystem = NumberingSystemDefinition.CreateCustomSystem("abcdefghij");
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			// Verify that the custom system returns custom digits
			Assert.That(testModel.CurrentWsSetupModel.CurrentNumberingSystemDefinition.IsCustom, Is.True);
			Assert.That(testModel.CurrentWsSetupModel.CurrentNumberingSystemDefinition.Digits, /* REVIEW (Hasso) contain? */ Is.EqualTo("abcdefghij"));
			// Test switching to default switches back to default digits
			testModel.CurrentWsSetupModel.CurrentNumberingSystemDefinition = NumberingSystemDefinition.Default;
			Assert.That(testModel.CurrentWsSetupModel.CurrentNumberingSystemDefinition.Digits, /* REVIEW (Hasso) contain? */ Is.EqualTo("0123456789"));
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
		public void CurrentWsListChanged_HideSelected_Returns_True()
		{
			var container = new TestWSContainer(new[] { "en", "fr", "en-Zxxx-x-audio" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			var menu = testModel.GetRightClickMenuItems();
			menu.First(item => item.MenuText.Contains("Hide")).ClickHandler(this, EventArgs.Empty);
			Assert.IsTrue(testModel.CurrentWsListChanged);
		}

		[Test]
		public void CurrentWsListChanged_DeleteSelected_Returns_True()
		{
			var container = new TestWSContainer(new[] { "en", "fr", "en-Zxxx-x-audio" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.ConfirmDeleteWritingSystem = label => true;
			var menu = testModel.GetRightClickMenuItems();
			menu.First(item => item.MenuText.Contains("Delete")).ClickHandler(this, EventArgs.Empty);
			Assert.IsTrue(testModel.CurrentWsListChanged);
		}

		/// <summary>
		/// Proves that the delete option should work during the New Project Wizard use of this model/dialog
		/// </summary>
		[Test]
		public void DeleteSelected_SaveDoesNotCrashWithNoCache()
		{
			var container = new TestWSContainer(new[] { "en", "fr" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular, new WritingSystemManager());
			testModel.ConfirmDeleteWritingSystem = label => true;
			var menu = testModel.GetRightClickMenuItems();
			menu.First(item => item.MenuText.Contains("Delete")).ClickHandler(this, EventArgs.Empty);
			Assert.DoesNotThrow(() => testModel.Save());
			Assert.That(container.VernacularWritingSystems.Count, Is.EqualTo(1));
		}

		/// <summary>
		/// Proves that the merge option should work during the New Project Wizard use of this model/dialog
		/// </summary>
		/// <remarks>The user would never want to do this, but just in case, don't crash.</remarks>
		[Test]
		public void MergeSelected_SaveDoesNotCrashWithNoCache()
		{
			var container = new TestWSContainer(new[] { "es", "fr" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular, new WritingSystemManager());
			testModel.ConfirmMergeWritingSystem = (string merge, out CoreWritingSystemDefinition tag) => {
				tag = container.CurrentVernacularWritingSystems.First();
				return true;
			};
			var menu = testModel.GetRightClickMenuItems();
			menu.First(item => item.MenuText.Contains("Merge")).ClickHandler(this, EventArgs.Empty);
			Assert.DoesNotThrow(() => testModel.Save());
			Assert.That(container.VernacularWritingSystems.Count, Is.EqualTo(1));
		}

		[Test]
		public void CurrentWsListChanged_HideUnSelected_Returns_False()
		{
			var container = new TestWSContainer(new[] { "en", "fr", "en-Zxxx-x-audio" }, null, new[] { "en", "en-Zxxx-x-audio" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.SelectWs("fr");
			var menu = testModel.GetRightClickMenuItems();
			menu.First(item => item.MenuText.Contains("Hide")).ClickHandler(this, EventArgs.Empty);
			Assert.IsFalse(testModel.CurrentWsListChanged);
		}

		[Test]
		public void CurrentWsListChanged_DeleteUnSelected_Returns_False()
		{
			var container = new TestWSContainer(new[] { "en", "fr", "en-Zxxx-x-audio" }, null, new[] { "en", "en-Zxxx-x-audio" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.SelectWs("fr");
			testModel.ConfirmDeleteWritingSystem = label => true;
			var menu = testModel.GetRightClickMenuItems();
			menu.First(item => item.MenuText.Contains("Delete")).ClickHandler(this, EventArgs.Empty);
			Assert.IsFalse(testModel.CurrentWsListChanged);
		}

		[Test]
		public void Delete_UserRepents_NoChange()
		{
			var wsList = new[] { "en", "fr", "en-Zxxx-x-audio" };
			var container = new TestWSContainer(wsList);
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular)
			{
				ConfirmDeleteWritingSystem = label => false
			};
			var menu = testModel.GetRightClickMenuItems();
			menu.First(item => item.MenuText.Contains("Delete")).ClickHandler(this, EventArgs.Empty);
			Assert.IsFalse(testModel.CurrentWsListChanged);
			Assert.That(testModel.WorkingList.Select(li => li.WorkingWs.Id), Is.EquivalentTo(wsList));
			Assert.That(testModel.WorkingList.All(li => li.InCurrentList));
		}

		[Test]
		public void TopVernIsHomographWs_UncheckedWarnsAndSetsNew()
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
		public void TopVernIsHomographWs_UncheckedWarnsAndDoesNotSetOnNo()
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
		public void TopVernIsHomographWs_MovedDownWarnsAndSetsNew()
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
		public void TopVernIsHomographWs_NewWsAddedAbove_WarnsAndSetsNew()
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
		public void TopVernIsNotHomographWs_UncheckedWarnsAndSetsNew()
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
		public void CurrentVernacularList_ToggleSaved()
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
		public void CurrentVernacularList_ToggleSavedWithOtherChange()
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
			var it = GetOrSetWs("it");
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

		[Test]
		public void HiddenWsModel_AllCtorArgsPassed()
		{
			ShowChangeLanguage(out var addedInfo);
			var addedWs = GetOrSetWs(addedInfo.LanguageTag);
			var deletedWs = GetOrSetWs("doa");
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			entry.SummaryDefinition.set_String(addedWs.Handle, "Hidden Text");
			entry.SummaryDefinition.set_String(deletedWs.Handle, "Deletable Text");
			Cache.LangProject.AnalysisWritingSystems.Add(deletedWs);
			Cache.ActionHandlerAccessor.EndUndoTask();

			var wasDlgShown = false;
			var testModel = new FwWritingSystemSetupModel(Cache.LangProject, FwWritingSystemSetupModel.ListType.Analysis, Cache.ServiceLocator.WritingSystemManager, Cache)
			{
				ConfirmDeleteWritingSystem = label => true,
				ShowChangeLanguage = ShowChangeLanguage,
				// When clicking "View hidden...", verify that the ViewHiddenWSModel is set up as expected
				ViewHiddenWritingSystems = model =>
				{
					// Already-to-be-shown WS's are not listed as "hidden"
					Assert.False(model.Items.Any(i => i.WS.Equals(addedWs)), $"{addedWs.DisplayLabel} is not quite 'hidden' anymore");

					// Already-to-be-deleted WS's are labeled as such
					var deletedItem = model.Items.First(i => i.WS.Equals(deletedWs));
					Assert.That(deletedItem.ToString(), Does.EndWith(string.Format(FwCoreDlgs.XWillBeDeleted, deletedWs.DisplayLabel)));

					wasDlgShown = true;
				}
			};

			// Add the hidden WS before viewing hidden WS's
			testModel.GetAddMenuItems().First(item => item.MenuText.Contains("Add new language")).ClickHandler.Invoke(null, null);
			// Delete the deleted WS before viewing hidden WS's
			testModel.SelectWs(deletedWs.LanguageTag);
			testModel.GetRightClickMenuItems().First(item => item.MenuText.Contains("Delete")).ClickHandler.Invoke(null, null);

			// SUT: when we view hidden WS's, we assert that the model was constructed as we expected
			testModel.GetAddMenuItems().First(item => item.MenuText.Contains("View hidden")).ClickHandler(this, EventArgs.Empty);

			Assert.True(wasDlgShown, nameof(wasDlgShown));
		}

		[Test]
		public void HiddenWsShown()
		{
			var testModel = new FwWritingSystemSetupModel(Cache.LangProject, FwWritingSystemSetupModel.ListType.Vernacular, Cache.ServiceLocator.WritingSystemManager, Cache)
			{
				ViewHiddenWritingSystems = model =>
					model.Items.Add(new HiddenWSListItemModel(GetOrSetWs("hid"), false) { WillAdd = true })
			};
			// Other tests may have saved the list with different WS's; start with that's already there.
			var expectedList = testModel.WorkingList.Select(li => li.OriginalWs.Id).ToList();
			expectedList.Add("hid");

			// SUT
			testModel.GetAddMenuItems().First(item => item.MenuText.Contains("View hidden")).ClickHandler(this, EventArgs.Empty);

			Assert.True(testModel.CurrentWsListChanged, "Showing a WS changes the 'current' showing list");
			Assert.That(testModel.WorkingList.Select(li => li.OriginalWs.Id), Is.EquivalentTo(expectedList));
			var shown = testModel.WorkingList[testModel.CurrentWritingSystemIndex];
			Assert.AreEqual("hid", shown.WorkingWs.Id);
			Assert.True(shown.InCurrentList, "Shown WS should be fully shown");
		}

		[Test]
		public void Save_HiddenWsDeleted_WsDeleted()
		{
			using (var mediator = new Mediator())
			{
				var deleteListener = new WSDeletedListener(mediator);
				var ws = GetOrSetWs("doa");
				Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create().CitationForm.set_String(ws.Handle, "some data");
				Cache.ActionHandlerAccessor.EndUndoTask();
				var testModel = new FwWritingSystemSetupModel(Cache.LangProject, FwWritingSystemSetupModel.ListType.Vernacular,
					Cache.ServiceLocator.WritingSystemManager, Cache, mediator)
				{
					ViewHiddenWritingSystems = model =>
						model.Items.Add(new HiddenWSListItemModel(ws, false) { WillDelete = true })
				};

				// SUT: Delete using the View hidden... dlg, then save
				testModel.GetAddMenuItems().First(item => item.MenuText.Contains("View hidden")).ClickHandler(this, EventArgs.Empty);
				testModel.Save();

				CollectionAssert.AreEqual(new[] {"doa"}, deleteListener.DeletedWSs);
				Assert.That(WritingSystemServices.FindAllWritingSystemsWithText(Cache), Is.Not.Contains(ws.Handle));
			}
		}

		[TestCase(FwWritingSystemSetupModel.ListType.Vernacular, "fr")]
		[TestCase(FwWritingSystemSetupModel.ListType.Analysis, "tpi")]
		public void Save_DeletedWs_WsDeleted(FwWritingSystemSetupModel.ListType type, string wsId)
		{
			using (var mediator = new Mediator())
			{
				var deleteListener = new WSDeletedListener(mediator);
				SetUpProjectWithData();
				Cache.ActionHandlerAccessor.EndUndoTask();
				var wasDeleteConfirmed = false;
				var testModel = new FwWritingSystemSetupModel(Cache.LangProject, type, Cache.ServiceLocator.WritingSystemManager, Cache, mediator)
				{
					ConfirmDeleteWritingSystem = label =>
					{
						wasDeleteConfirmed = true;
						return true;
					}
				};
				testModel.SelectWs(wsId);

				// SUT: click Delete, then save
				testModel.GetRightClickMenuItems().First(item => item.MenuText.Contains("Delete")).ClickHandler(this, EventArgs.Empty);
				testModel.Save();


				Assert.True(wasDeleteConfirmed, "should confirm delete");
				AssertEnglishDataIntact();
				if (type == FwWritingSystemSetupModel.ListType.Vernacular)
				{
					Assert.AreEqual("en", Cache.LangProject.CurVernWss, "Only English should remain selected after save");
					Assert.AreEqual("en", Cache.LangProject.VernWss, "Only English should remain after save");
					AssertTokPisinDataIntact();
				}
				else
				{
					Assert.AreEqual("en", Cache.LangProject.CurAnalysisWss, "Only English should remain selected after save");
					Assert.AreEqual("en", Cache.LangProject.AnalysisWss, "Only English should remain after save");
					AssertFrenchDataIntact();
				}
				CollectionAssert.AreEqual(new[] {wsId}, deleteListener.DeletedWSs);
				Assert.That(WritingSystemServices.FindAllWritingSystemsWithText(Cache), Is.Not.Contains(GetOrSetWs(wsId).Handle));
			}
		}

		[Test]
		public void Save_DeletedWs_ExistsInOtherList_WsHidden(
			[Values(FwWritingSystemSetupModel.ListType.Vernacular, FwWritingSystemSetupModel.ListType.Analysis)]
			FwWritingSystemSetupModel.ListType type)
		{
			using (var mediator = new Mediator())
			{
				var deleteListener = new WSDeletedListener(mediator);
				SetupHomographLanguagesInCache();
				var fr = GetOrSetWs("fr");
				Cache.LangProject.AnalysisWritingSystems.Add(fr);
				var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				entry.Comment.set_String(fr.Handle, "commentary");
				Cache.ActionHandlerAccessor.EndUndoTask();
				var wasDeleteConfirmed = false;
				var testModel = new FwWritingSystemSetupModel(Cache.LangProject, type, Cache.ServiceLocator.WritingSystemManager, Cache, mediator)
				{
					ConfirmDeleteWritingSystem = label =>
					{
						wasDeleteConfirmed = true;
						return true;
					}
				};
				testModel.SelectWs("fr");

				// SUT: click Delete, then save
				testModel.GetRightClickMenuItems().First(item => item.MenuText.Contains("Delete")).ClickHandler(this, EventArgs.Empty);
				testModel.Save();

				Assert.False(wasDeleteConfirmed, "shouldn't confirm 'deleting' a WS that will only be hidden");
				AssertOnlyEnglishInList(type);
				CollectionAssert.IsEmpty(deleteListener.DeletedWSs);
				var comment = entry.Comment.get_String(fr.Handle);
				Assert.AreEqual(fr.Handle, comment.get_WritingSystemAt(0));
				Assert.AreEqual("commentary", comment.Text);
			}
		}

		[TestCase(FwWritingSystemSetupModel.ListType.Vernacular, "fr")]
		[TestCase(FwWritingSystemSetupModel.ListType.Analysis, "tpi")]
		public void Save_HiddenWs_WsHidden(FwWritingSystemSetupModel.ListType type, string wsId)
		{
			using (var mediator = new Mediator())
			{
				var deleteListener = new WSDeletedListener(mediator);
				SetUpProjectWithData();
				Cache.ActionHandlerAccessor.EndUndoTask();
				var testModel = new FwWritingSystemSetupModel(Cache.LangProject, type, Cache.ServiceLocator.WritingSystemManager, Cache, mediator);
				testModel.SelectWs(wsId);

				// SUT: click Hide, then save
				testModel.GetRightClickMenuItems().First(item => item.MenuText.Contains("Hide")).ClickHandler(this, EventArgs.Empty);
				testModel.Save();

				AssertOnlyEnglishInList(type);
				CollectionAssert.IsEmpty(deleteListener.DeletedWSs);
				AssertProjectDataIntact();
			}
		}

		[TestCase(FwWritingSystemSetupModel.ListType.Vernacular, "fr")]
		[TestCase(FwWritingSystemSetupModel.ListType.Analysis, "tpi")]
		public void Save_WsDeletedRestoredAndHidden_WsHidden(FwWritingSystemSetupModel.ListType type, string wsId)
		{
			using (var mediator = new Mediator())
			{
				var deleteListener = new WSDeletedListener(mediator);
				SetUpProjectWithData();
				Cache.ActionHandlerAccessor.EndUndoTask();
				var testModel = new FwWritingSystemSetupModel(Cache.LangProject, type, Cache.ServiceLocator.WritingSystemManager, Cache, mediator)
				{
					AddNewVernacularLanguageWarning = () => true,
					ConfirmDeleteWritingSystem = label => true,
					ShowChangeLanguage = (out LanguageInfo info) =>
					{
						info = new LanguageInfo { LanguageTag = wsId };
						return true;
					}
				};

				// Delete French, then add it back
				testModel.SelectWs(wsId);
				testModel.GetRightClickMenuItems().First(item => item.MenuText.Contains("Delete")).ClickHandler(this, EventArgs.Empty);
				testModel.GetAddMenuItems().First(item => item.MenuText.Contains("Add new language")).ClickHandler.Invoke(null, null);
				testModel.SelectWs(wsId);

				// SUT: click Hide, then save
				testModel.GetRightClickMenuItems().First(item => item.MenuText.Contains("Hide")).ClickHandler(this, EventArgs.Empty);
				testModel.Save();

				AssertOnlyEnglishInList(type);
				CollectionAssert.IsEmpty(deleteListener.DeletedWSs);
				AssertProjectDataIntact();
			}
		}

		[TestCase(FwWritingSystemSetupModel.ListType.Vernacular, "fr")]
		[TestCase(FwWritingSystemSetupModel.ListType.Analysis, "tpi")]
		public void Save_WsDeletedAndRestored_NoChange(FwWritingSystemSetupModel.ListType type, string wsId)
		{
			using (var mediator = new Mediator())
			{
				var ws = GetOrSetWs(wsId);
				var deleteListener = new WSDeletedListener(mediator);
				SetUpProjectWithData();
				Cache.ActionHandlerAccessor.EndUndoTask();
				var testModel = new FwWritingSystemSetupModel(Cache.LangProject, type, Cache.ServiceLocator.WritingSystemManager, Cache, mediator)
				{
					AddNewVernacularLanguageWarning = () => true,
					ConfirmDeleteWritingSystem = label => true,
					ShowChangeLanguage = (out LanguageInfo info) =>
					{
						// Play nicely with other tests by keeping the Language Name the same
						info = new LanguageInfo { LanguageTag = ws.LanguageTag, DesiredName = ws.LanguageName };
						return true;
					}
				};

				// Delete French, then add it back
				testModel.SelectWs(wsId);
				testModel.GetRightClickMenuItems().First(item => item.MenuText.Contains("Delete")).ClickHandler(this, EventArgs.Empty);
				testModel.GetAddMenuItems().First(item => item.MenuText.Contains("Add new language")).ClickHandler.Invoke(null, null);

				// SUT
				testModel.Save();

				Assert.AreEqual("en fr", Cache.LangProject.CurVernWss, "Both should remain selected after save");
				Assert.AreEqual("en fr", Cache.LangProject.VernWss, "Both should remain after save");
				Assert.AreEqual("en tpi", Cache.LangProject.CurAnalysisWss, "Both should remain selected after save");
				Assert.AreEqual("en tpi", Cache.LangProject.AnalysisWss, "Both should remain after save");
				CollectionAssert.IsEmpty(deleteListener.DeletedWSs);
				AssertProjectDataIntact();
			}
		}

		/// <summary>
		/// Sets up the project with data in French (Vernacular), Tok Pisin (Analysis), and English (both)
		/// </summary>
		private void SetUpProjectWithData()
		{
			// set up Writing Systems
			SetupHomographLanguagesInCache();
			var en = GetOrSetWs("en");
			var fr = GetOrSetWs("fr");
			var tp = GetOrSetWs("tpi");
			var aws = Cache.LangProject.AnalysisWritingSystems;
			aws.Clear();
			aws.Add(en);
			aws.Add(tp);
			Cache.LangProject.CurrentAnalysisWritingSystems.Clear();
			Cache.LangProject.CurrentAnalysisWritingSystems.AddRange(aws);

			// set up data
			foreach (var oldEntry in Cache.LangProject.LexDbOA.Entries.ToArray())
			{
				oldEntry.Delete();
			}
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			entry.CitationForm.set_String(en.Handle, "enForm");
			entry.CitationForm.set_String(fr.Handle, "frHeadword");
			var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			entry.SensesOS.Add(sense);
			sense.Gloss.set_String(en.Handle, "enSense");
			sense.Gloss.set_String(tp.Handle, "tpiSense");

			// TODO (Hasso) 2021.03: test reversal indexes and interlinear texts?
		}

		private void AssertProjectDataIntact()
		{
			AssertEnglishDataIntact();
			AssertFrenchDataIntact();
			AssertTokPisinDataIntact();
		}

		private void AssertEnglishDataIntact()
		{
			var en = GetOrSetWs("en").Handle;
			var entry = Cache.LangProject.LexDbOA.Entries.First();
			Assert.AreEqual("enForm", entry.CitationForm.get_String(en).Text);
			Assert.AreEqual("enSense", entry.SensesOS.First().Gloss.get_String(en).Text);
		}

		private void AssertFrenchDataIntact()
		{
			var fr = GetOrSetWs("fr").Handle;
			var entry = Cache.LangProject.LexDbOA.Entries.First();
			Assert.AreEqual("frHeadword", entry.CitationForm.get_String(fr).Text);
		}

		private void AssertTokPisinDataIntact()
		{
			var tpi = GetOrSetWs("tpi").Handle;
			var entry = Cache.LangProject.LexDbOA.Entries.First();
			Assert.AreEqual("tpiSense", entry.SensesOS.First().Gloss.get_String(tpi).Text);
		}

		private void AssertOnlyEnglishInList(FwWritingSystemSetupModel.ListType type)
		{
			if (type == FwWritingSystemSetupModel.ListType.Vernacular)
			{
				Assert.AreEqual("en", Cache.LangProject.CurVernWss, "Only English should remain selected after save");
				Assert.AreEqual("en", Cache.LangProject.VernWss, "Only English should remain after save");
			}
			else
			{
				Assert.AreEqual("en", Cache.LangProject.CurAnalysisWss, "Only English should remain selected after save");
				Assert.AreEqual("en", Cache.LangProject.AnalysisWss, "Only English should remain after save");
			}
		}

		[Test]
		public void SpellingDictionary_DefaultIdGenerated()
		{
			var container = new TestWSContainer(new[] {"auc-Latn-PR"});
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular, new WritingSystemManager());
			var spellingDict = testModel.SpellingDictionary;
			Assert.That(spellingDict.Id, Is.Null.Or.Empty);
		}

		[Test]
		public void SpellingDictionary_CanSetToEmpty()
		{
			var container = new TestWSContainer(new[] { "fr" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular, new WritingSystemManager());
			Assert.DoesNotThrow(() => testModel.SpellingDictionary = null);
			Assert.That(testModel.SpellingDictionary.Id, Is.Null.Or.Empty);
		}

		[Test]
		public void GetSpellingDictionaryComboBoxItems_HasDefaultForWs()
		{
			var container = new TestWSContainer(new[] { "auc-Latn-PR" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular, new WritingSystemManager());
			var menuItems = testModel.GetSpellingDictionaryComboBoxItems();
			var constructedItem = menuItems.FirstOrDefault(item => item.Id == "auc_Latn_PR");
			Assert.NotNull(constructedItem, "A default item matching the ws id should be in the list.");
		}

		[Test]
		public void MergeWritingSystemTest_MergeWorks()
		{
			Cache.LangProject.CurrentAnalysisWritingSystems.Clear();
			var gb = GetOrSetWs("en-GB");
			var en = GetOrSetWs("en");
			var fr = GetOrSetWs("fr");
			Cache.LangProject.CurrentAnalysisWritingSystems.Add(gb);
			Cache.LangProject.CurrentAnalysisWritingSystems.Add(en);
			Cache.LangProject.CurrentVernacularWritingSystems.Add(fr);
			var entryFactory = Cache.ServiceLocator.GetInstance<ILexEntryFactory>();
			var entry = entryFactory.Create();
			entry.SummaryDefinition.set_String(gb.Handle, "Queens English");
			Cache.ActionHandlerAccessor.EndUndoTask();
			var container = new TestWSContainer(new[] { "fr" }, new [] {"en-GB", "en"});
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Analysis, Cache.ServiceLocator.WritingSystemManager, Cache);
			testModel.ConfirmMergeWritingSystem = (string merge, out CoreWritingSystemDefinition tag) =>
			{
				tag = container.CurrentAnalysisWritingSystems.First(ws => ws.Id == "en");
				return true;
			};
			testModel.SelectWs("en-GB");
			testModel.GetRightClickMenuItems().First(item => item.MenuText == "Merge...").ClickHandler.Invoke(null, null);
			testModel.Save();
			Assert.That(entry.SummaryDefinition.get_String(en.Handle).Text, Does.StartWith("Queens English"));
		}

		/// <summary>
		/// Adds en and fr to Current Vernacular and sets en as Homograph WS.
		/// The client must call <c>Cache.ActionHandlerAccessor.EndUndoTask()</c> after this and any additional setup.
		/// </summary>
		private void SetupHomographLanguagesInCache()
		{
			var en = GetOrSetWs("en");
			var fr = GetOrSetWs("fr");
			Cache.LangProject.CurrentVernacularWritingSystems.Clear();
			Cache.LangProject.CurrentVernacularWritingSystems.Add(en);
			Cache.LangProject.CurrentVernacularWritingSystems.Add(fr);
			Cache.LangProject.VernacularWritingSystems.Clear();
			Cache.LangProject.VernacularWritingSystems.AddRange(Cache.LangProject.CurrentVernacularWritingSystems);
			Cache.LangProject.HomographWs = "en";
		}

		private CoreWritingSystemDefinition GetOrSetWs(string code)
		{
			Cache.ServiceLocator.WritingSystemManager.GetOrSet(code, out var ws);
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

		[SuppressMessage("ReSharper", "InconsistentNaming")]
		[SuppressMessage("ReSharper", "UnassignedGetOnlyAutoProperty")]
		internal class TestWSContainer : IWritingSystemContainer
		{
			private readonly List<CoreWritingSystemDefinition> _vernacular = new List<CoreWritingSystemDefinition>();
			private readonly List<CoreWritingSystemDefinition> _analysis = new List<CoreWritingSystemDefinition>();
			private readonly List<CoreWritingSystemDefinition> _curVern = new List<CoreWritingSystemDefinition>();
			private readonly List<CoreWritingSystemDefinition> _curAnaly = new List<CoreWritingSystemDefinition>();

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
				throw new NotImplementedException();
			}

			public void AddToCurrentVernacularWritingSystems(CoreWritingSystemDefinition ws)
			{
				throw new NotImplementedException();
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

		private class WSDeletedListener : IxCoreColleague
		{
			public List<string> DeletedWSs { get; } = new List<string>();

			public WSDeletedListener(Mediator mediator)
			{
				Init(mediator, null, null);
			}

			public void OnWritingSystemDeleted(object param)
			{
				DeletedWSs.AddRange((string[])param);
			}

			public void Init(Mediator mediator, PropertyTable propertyTable, XmlNode configurationParameters)
			{
				mediator.AddColleague(this);
			}

			public IxCoreColleague[] GetMessageTargets()
			{
				return new IxCoreColleague[] { this };
			}

			public bool ShouldNotCall => false;
			public int Priority => (int)ColleaguePriority.High;
		}
	}
}

// Copyright (c) 2019-2021 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Xml;
using NUnit.Framework;
using Moq;
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
			Assert.That(testModel.CurrentWsSetupModel.SelectionForSpecialCombo, Is.EqualTo(WritingSystemSetupModel.SelectionsForSpecialCombo.ScriptRegionVariant));
		}

		[Test]
		public void SelectionForSpecialCombo_ChangesOnSelectionChange_GivesScriptRegionVariant()
		{
			var container = new TestWSContainer(new[] { "en", "en-Kore-US" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			Assert.That(testModel.CurrentWsSetupModel.SelectionForSpecialCombo, Is.EqualTo(WritingSystemSetupModel.SelectionsForSpecialCombo.None));
			testModel.SelectWs("en-Kore-US");
			Assert.That(testModel.CurrentWsSetupModel.SelectionForSpecialCombo, Is.EqualTo(WritingSystemSetupModel.SelectionsForSpecialCombo.ScriptRegionVariant));
		}

		[Test]
		public void SelectionForSpecialCombo_LockedForDefaultEnglish()
		{
			var container = new TestWSContainer(new[] { "en", "de" });
			string expectedErrorMessage = string.Format(FwCoreDlgs.kstidCantChangeEnglishSRV, "English");
			string errorMessage = null;
			var wssModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular)
			{
				ShowMessageBox = (text, isResponseRequested) => { errorMessage = text; Assert.That(isResponseRequested, Is.False); return false; }
			}.CurrentWsSetupModel;
			wssModel.CurrentScriptCode = "Cyrilic";
			Assert.That(wssModel.CurrentScriptCode, Is.EqualTo("Latn"), "script code should be reset to Latin");
			Assert.That(wssModel.SelectionForSpecialCombo, Is.EqualTo(WritingSystemSetupModel.SelectionsForSpecialCombo.None), "script");
			Assert.That(wssModel.CurrentLanguageTag, Is.EqualTo("en"), "script");
			Assert.That(errorMessage, Is.EqualTo(expectedErrorMessage), "script");
			errorMessage = null; // reset for next test
			wssModel.CurrentRegion = "GB";
			Assert.That(wssModel.CurrentRegion, Is.EqualTo(""), "region");
			Assert.That(wssModel.SelectionForSpecialCombo, Is.EqualTo(WritingSystemSetupModel.SelectionsForSpecialCombo.None), "region");
			Assert.That(wssModel.CurrentLanguageTag, Is.EqualTo("en"), "region");
			Assert.That(errorMessage, Is.EqualTo(expectedErrorMessage), "region");
			errorMessage = null; // reset for next test
			wssModel.CurrentIsVoice = true;
			Assert.That(wssModel.CurrentIsVoice, Is.False, "voice");
			Assert.That(wssModel.SelectionForSpecialCombo, Is.EqualTo(WritingSystemSetupModel.SelectionsForSpecialCombo.None), "voice");
			Assert.That(wssModel.CurrentLanguageTag, Is.EqualTo("en"), "voice");
			Assert.That(errorMessage, Is.EqualTo(expectedErrorMessage), "voice");
			errorMessage = null; // reset for next test
			wssModel.CurrentIpaStatus = IpaStatusChoices.Ipa;
			Assert.That(wssModel.CurrentIpaStatus, Is.EqualTo(IpaStatusChoices.NotIpa));
			Assert.That(wssModel.SelectionForSpecialCombo, Is.EqualTo(WritingSystemSetupModel.SelectionsForSpecialCombo.None), "IPA");
			Assert.That(wssModel.CurrentLanguageTag, Is.EqualTo("en"), "IPA");
			Assert.That(errorMessage, Is.EqualTo(expectedErrorMessage), "IPA");
			errorMessage = null; // reset for next test
			wssModel.CurrentVariant = "x-xqax"; // or something like that
			Assert.That(wssModel.CurrentVariant, Is.Empty, "Variants");
			Assert.That(wssModel.SelectionForSpecialCombo, Is.EqualTo(WritingSystemSetupModel.SelectionsForSpecialCombo.None), "Variants");
			Assert.That(wssModel.CurrentLanguageTag, Is.EqualTo("en"), "Variants");
			Assert.That(errorMessage, Is.EqualTo(expectedErrorMessage), "Variants");
		}

		[Test]
		public void AdvancedConfiguration_NonCustomLangScriptRegion_IsDisabled()
		{
			var container = new TestWSContainer(new[] { "en-Latn-US" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			Assert.That(testModel.ShowAdvancedScriptRegionVariantView, Is.False, "Model should not show advanced view for normal data");
		}

		[Test]
		public void AdvancedConfiguration_CustomScript_IsEnabled()
		{
			var container = new TestWSContainer(new[] { "en-Qaaa-x-CustomSc" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			Assert.That(testModel.ShowAdvancedScriptRegionVariantView, Is.True, "Model should show advanced view for Custom script");
		}

		[Test]
		public void AdvancedConfiguration_CustomRegion_IsEnabled()
		{
			var container = new TestWSContainer(new[] { "en-QM-x-CustomRg" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			Assert.That(testModel.ShowAdvancedScriptRegionVariantView, Is.True, "Model should show advanced view for Custom script");
		}

		[Test]
		public void AdvancedConfiguration_CustomLanguage_IsEnabled()
		{
			var container = new TestWSContainer(new[] { "Qaa-x-CustomLa" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			Assert.That(testModel.ShowAdvancedScriptRegionVariantView, Is.True, "Model should show advanced view for Custom script");
		}

		[Test]
		public void AdvancedConfiguration_StandardAndPrivateUse_IsEnabled()
		{
			var container = new TestWSContainer(new[] { "fr-fonipa-x-special" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			Assert.That(testModel.ShowAdvancedScriptRegionVariantView, Is.True, "Model should show advanced view when there are multiple variants");
		}

		[Test]
		public void AdvancedConfiguration_AllPrivateUse_IsNotEnabled()
		{
			var container = new TestWSContainer(new[] { "fr-x-special-extra" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			Assert.That(testModel.ShowAdvancedScriptRegionVariantView, Is.False, "Model should show advanced view when there are multiple variants");
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
			Assert.That(testModel.ShowAdvancedScriptRegionVariantView, Is.True, "should be advanced to start");
			testModel.ShowAdvancedScriptRegionVariantView = false;
			Assert.That(confirmClearCalled, Is.True);
			Assert.That(testModel.CurrentWsSetupModel.CurrentRegionTag, Is.Null);
			Assert.That(testModel.CurrentWsSetupModel.CurrentIso15924Script.IsPrivateUse, Is.False);
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
			Assert.That(testModel.EnableGraphiteFontOptions, Is.False, "Non Graphite fonts should not have the EnableGraphiteFontOptions available");
		}

		[TestCase("en", false)]
		[TestCase("en-Arab", true)]
		[TestCase("en-Qaaa-x-Mark", true)]
		public void AdvancedConfiguration_AdvancedScriptRegionVariantCheckboxVisible(string languageTag, bool expectedResult)
		{
			var container = new TestWSContainer(new[] { languageTag });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			Assert.That(testModel.ShowAdvancedScriptRegionVariantCheckBox, Is.EqualTo(expectedResult));
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
			Assert.That(testModel.EnableGraphiteFontOptions, Is.True, "Graphite fonts should have the EnableGraphiteFontOptions available");
		}

		[Test]
		public void AdvancedConfiguration_NoDefaultFont_GraphiteFontOptionsAreDisabled()
		{
			var container = new TestWSContainer(new[] { "en" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			Assert.That(testModel.EnableGraphiteFontOptions, Is.False, "EnableGraphiteFeatures should not be available without a default font");
		}

		[TestCase("en", new[] { "en" }, false)] // Can't move the only item anywhere
		[TestCase("fr", new[] { "fr", "en" }, false)] // Can't move the top item up
		[TestCase("en", new[] { "fr", "en" }, true)] // Can move an item up if there is one above it
		public void WritingSystemList_MoveUp_CanMoveUp(string toMove, string[] options, bool expectedResult)
		{
			var container = new TestWSContainer(options);
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.SelectWs(toMove);
			Assert.That(testModel.CanMoveUp(), Is.EqualTo(expectedResult));
		}

		[TestCase("en", new[] { "en" }, false)] // Can't move the only item anywhere
		[TestCase("fr", new[] { "fr", "en" }, true)] // Can move an item down if it isn't at the bottom
		[TestCase("en", new[] { "fr", "en" }, false)] // Can't move the bottom item down
		public void WritingSystemList_MoveUp_CanMoveDown(string toMove, string[] options, bool expectedResult)
		{
			var container = new TestWSContainer(options);
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.SelectWs(toMove);
			Assert.That(testModel.CanMoveDown(), Is.EqualTo(expectedResult));
		}

		[Test]
		public void WritingSystemList_RightClickMenuItems_ChangeWithSelection()
		{
			var container = new TestWSContainer(new[] { "es", "fr" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			var menu = testModel.GetRightClickMenuItems().Select(item => item.MenuText);
			Assert.That(menu, Is.EqualTo(new[] { "Merge...", "Update Spanish", "Hide Spanish", "Delete Spanish" }));
			testModel.SelectWs("fr");
			menu = testModel.GetRightClickMenuItems().Select(item => item.MenuText);
			Assert.That(menu, Is.EqualTo(new[] { "Merge...", "Update French", "Hide French", "Delete French" }));
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
			Assert.That(menu.First(m => m.MenuText.StartsWith("Hide")).IsEnabled, Is.False);
			Assert.That(menu.First(m => m.MenuText.StartsWith("Hide")).MenuText, /* REVIEW (Hasso) contain? */ Is.EqualTo("Hide French"));
			Assert.That(menu.First(m => m.MenuText.StartsWith("Delete")).IsEnabled, Is.False);
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
			Assert.That(addMenu, Is.EqualTo(new [] { "Add IPA for English", "Add Audio for English", "Add variation of English", "Add new language..." }));
			testModel.SelectWs("fr");
			addMenu = testModel.GetAddMenuItems().Select(item => item.MenuText);
			Assert.That(addMenu, Is.EqualTo(new[] { "Add IPA for French", "Add Audio for French", "Add variation of French", "Add new language..." }));
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
			Assert.That(warned, Is.True, "Warning not displayed.");
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
			Assert.That(warned, Is.False, "Warning incorrectly displayed.");
		}

		[Test]
		public void WritingSystemList_AddMenuItems_DoesNotOfferExistingOption()
		{
			var container = new TestWSContainer(new [] { "auc" }, new[] { "en", "en-fonipa", "en-Zxxx-x-audio" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Analysis);
			var addMenu = testModel.GetAddMenuItems().Select(item => item.MenuText);
			Assert.That(addMenu, Is.EqualTo(new[] { "Add variation of English", "Add new language..." }));
		}

		[Test]
		public void WritingSystemList_AddMenuItems_DoesNotOfferIpaWhenIpaSelected()
		{
			var container = new TestWSContainer(new[] { "en-fonipa", "en" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			var addMenu = testModel.GetAddMenuItems().Select(item => item.MenuText);
			Assert.That(addMenu, Is.EqualTo(new[] { "Add Audio for English", "Add variation of English", "Add new language..." }));
		}

		[Test]
		public void WritingSystemList_AddMenuItems_ShowHiddenWritingSystemsWithCache()
		{
			var testModel = new FwWritingSystemSetupModel(Cache.LangProject, FwWritingSystemSetupModel.ListType.Analysis,
				Cache.ServiceLocator.WritingSystemManager, Cache);
			var addMenu = testModel.GetAddMenuItems().Select(item => item.MenuText);
			Assert.That(addMenu, Is.EqualTo(new []
			{
				"Add IPA for English", "Add Audio for English", "Add variation of English", "Add new language...", "View hidden Writing Systems..."
			}));

			SetupHomographLanguagesInCache();
			testModel = new FwWritingSystemSetupModel(Cache.LangProject, FwWritingSystemSetupModel.ListType.Vernacular,
				Cache.ServiceLocator.WritingSystemManager, Cache);
			testModel.SelectWs("fr");
			addMenu = testModel.GetAddMenuItems().Select(item => item.MenuText);
			Assert.That(addMenu, Is.EqualTo(new[]
			{
				"Add IPA for French", "Add Audio for French", "Add variation of French", "Add new language...", "View hidden Writing Systems..."
			}));
		}

		[Test]
		public void WritingSystemList_MoveUp_ItemMoved()
		{
			var container = new TestWSContainer(new[] { "fr", "en" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			Assert.That(testModel.WorkingList.Select(ws => ws.WorkingWs.LanguageTag), Is.EqualTo(new[] {"fr", "en"}));
			testModel.SelectWs("en");
			// SUT
			testModel.MoveUp();
			Assert.That(testModel.WorkingList.Select(ws => ws.WorkingWs.LanguageTag), Is.EqualTo(new [] { "en", "fr" }));
		}

		[Test]
		public void WritingSystemList_ToggleInCurrentList_ToggleWorks()
		{
			var container = new TestWSContainer(new[] { "fr", "en" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			Assert.That(testModel.WorkingList.Select(ws => ws.InCurrentList), Is.EqualTo(new[] { true, true }));
			testModel.SelectWs("en");
			// SUT
			testModel.ToggleInCurrentList();
			Assert.That(testModel.WorkingList.Select(ws => ws.InCurrentList), Is.EqualTo(new[] { true, false }));
		}

		[Test]
		public void WritingSystemList_MoveDown_ItemMoved()
		{
			var container = new TestWSContainer(new[] { "fr", "en" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			Assert.That(testModel.WorkingList.Select(ws => ws.WorkingWs.LanguageTag), Is.EqualTo(new[] { "fr", "en" }));
			testModel.SelectWs("fr");
			// SUT
			testModel.MoveDown();
			Assert.That(testModel.WorkingList.Select(ws => ws.WorkingWs.LanguageTag), Is.EqualTo(new[] { "en", "fr" }));
		}

		[Test]
		public void MoveItem_NewOrderSaved()
		{
			SetupHomographLanguagesInCache();
			Cache.ActionHandlerAccessor.EndUndoTask();
			var langProj = Cache.LangProject;
			Assert.That(langProj.VernWss, Is.EqualTo("en fr"), "setup problem");
			var testModel = new FwWritingSystemSetupModel(langProj, FwWritingSystemSetupModel.ListType.Vernacular,
				Cache.ServiceLocator.WritingSystemManager, Cache)
				{ ShouldChangeHomographWs = ws => true };
			testModel.MoveDown();
			testModel.Save();
			Assert.That(langProj.CurVernWss, Is.EqualTo("fr en"), "current");
			Assert.That(langProj.VernWss, Is.EqualTo("fr en"), "all");
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
				Assert.That(testModel.CanMerge(), Is.EqualTo(expectedResult));
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
			Assert.That(testModel.CanMerge(), Is.EqualTo(false));
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
			Assert.That(testModel.CanDelete(), Is.EqualTo(expectedResult));
		}

		[Test]
		public void WritingSystemList_CanDelete_CanDeleteDuplicateEnglishAnalysis()
		{
			var wss = new[] { "en" };
			var container = new TestWSContainer(wss, wss);
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Analysis);
			var addMenuItems = testModel.GetAddMenuItems();
			addMenuItems.First(item => item.MenuText.Contains("variation")).ClickHandler.Invoke(this, new EventArgs());
			Assert.That(testModel.CurrentWsSetupModel.CurrentLanguageTag, Is.EqualTo("en"));
			Assert.That(testModel.CanDelete(), Is.EqualTo(true), "should be able to delete the newly-created English [in]variant");
			testModel.SelectWs(0);
			Assert.That(testModel.CanDelete(), Is.EqualTo(false), "should not be able to delete the original English");
		}

		[Test]
		public void WritingSystemList_IsListValid_FalseIfNoCurrentItems()
		{
			var container = new TestWSContainer(new [] { "en" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.ToggleInCurrentList();
			Assert.That(testModel.IsListValid, Is.False);
		}

		[Test]
		public void WritingSystemList_IsListValid_TrueIfOneCurrentItem()
		{
			var container = new TestWSContainer(new [] { "en" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			Assert.That(testModel.IsListValid, Is.True);
		}

		[Test]
		public void WritingSystemList_IsListValid_FalseIfDuplicateItem()
		{
			var container = new TestWSContainer(new[] { "en", "en" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			Assert.That(testModel.IsListValid, Is.False);
		}

		[Test]
		public void WritingSystemList_IsAtLeastOneSelected_FalseIfNoCurrentItems()
		{
			var container = new TestWSContainer(new [] { "en" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.ToggleInCurrentList();
			Assert.That(testModel.IsAtLeastOneSelected, Is.False);
		}

		[Test]
		public void WritingSystemList_IsAtLeastOneSelected_TrueIfOneCurrentItem()
		{
			var container = new TestWSContainer(new [] { "en" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			Assert.That(testModel.IsAtLeastOneSelected, Is.True);
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
			Assert.That(testModel.FirstDuplicateWs, Is.EqualTo("English"));
		}

		[Test]
		public void WritingSystemList_CurrentList_StaysStable()
		{
			var container = new TestWSContainer(new[] { "en", "fr", "en-Zxxx-x-audio" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			Assert.That(testModel.WorkingList[0].WorkingWs.DisplayLabel, Is.EqualTo("English"));
			testModel.SelectWs("en-Zxxx-x-audio");
			Assert.That(testModel.WorkingList[0].WorkingWs.DisplayLabel, Is.EqualTo("English"));
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
			Assert.That(testModel.WorkingList.Select(ws => ws.WorkingWs.LanguageTag), Is.EqualTo(new [] { "en", "en-Zxxx-x-audio", "fr"}));
		}

		[Test]
		public void Model_NewWritingSystemAddedInManagerAndList()
		{
			// Set up mocks to verify wsManager save behavior
			var mockWsManager = new Mock<IWritingSystemManager>();
			mockWsManager.Setup(manager => manager.Replace(It.IsAny<CoreWritingSystemDefinition>()));

			var container = new TestWSContainer(new[] { "en" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular, mockWsManager.Object);
			// no-op handling of importing lists for new writing system
			testModel.ImportListForNewWs = import => { };
			var french = new CoreWritingSystemDefinition("fr");
			testModel.WorkingList.Add(new WSListItemModel(true, null, french));

			testModel.Save();

			Assert.That(2, Is.EqualTo(container.VernacularWritingSystems.Count));
			mockWsManager.Verify(manager => manager.Replace(french), Times.Once);
		}

		[Test]
		public void Model_ChangedWritingSystemIdSetInManager()
		{
			// Set up mocks to verify wsManager save behavior
			var mockWsManager = new Mock<IWritingSystemManager>();
			mockWsManager.Setup(manager => manager.Replace(It.IsAny<CoreWritingSystemDefinition>()));

			var container = new TestWSContainer(new[] { "es", "fr" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular, mockWsManager.Object);
			var enWs = container.VernacularWritingSystems.First();
			testModel.ShowChangeLanguage = ShowChangeLanguage;
			testModel.ChangeLanguage();
			testModel.Save();

			Assert.That(2, Is.EqualTo(container.VernacularWritingSystems.Count));
			mockWsManager.Verify(manager => manager.Replace(enWs), Times.Once);
		}

		[Test]
		public void Model_ChangesContainerOnlyOnSave()
		{
			// Set up mocks to verify wsManager save behavior
			var mockWsManager = new Mock<IWritingSystemManager>();
			mockWsManager.Setup(manager => manager.Save());

			var container = new TestWSContainer(new[] {"fr", "fr-FR", "fr-Zxxx-x-audio"});
			var testModel = new FwWritingSystemSetupModel(container,
				FwWritingSystemSetupModel.ListType.Vernacular, mockWsManager.Object);
			// Start changing stuff like crazy
			testModel.CurrentWsSetupModel.CurrentAbbreviation = "free.";
			testModel.CurrentWsSetupModel.CurrentCollationRulesType = "CustomSimple";
			testModel.CurrentWsSetupModel.CurrentCollationRules = "Z z Y y X x";
			// verify that the container WorkingWs defs have not changed
			Assert.That(container.VernacularWritingSystems.First().Abbreviation, Is.EqualTo("fr"));
			Assert.That(container.VernacularWritingSystems.First().DefaultCollationType, Is.EqualTo("standard"));
			Assert.That(container.VernacularWritingSystems.First().DefaultCollation, Is.Null);
			testModel.Save();
			// verify that the container WorkingWs defs have changed
			mockWsManager.Verify(manager => manager.Save(), Times.Once);
			Assert.That(container.VernacularWritingSystems.First().Abbreviation, Is.EqualTo("free."));
			Assert.That(container.VernacularWritingSystems.First().DefaultCollation, Is.Not.Null);
			Assert.That(((SimpleRulesCollationDefinition) container.VernacularWritingSystems.First().DefaultCollation).SimpleRules, Is.EqualTo("Z z Y y X x"));
		}

		[Test]
		public void Model_WritingSystemListUpdated_CalledOnChange()
		{
			var writingSystemListUpdatedCalled = false;
			var mockWsManager = new Mock<IWritingSystemManager>();

			var container = new TestWSContainer(new[] { "fr", "fr-FR", "fr-Zxxx-x-audio" });
			var testModel = new FwWritingSystemSetupModel(container,
				FwWritingSystemSetupModel.ListType.Vernacular, mockWsManager.Object);
			testModel.WritingSystemListUpdated += (sender, args) =>
			{
				writingSystemListUpdatedCalled = true;
			};
			// Make a change that should notify listeners (refresh the lexicon view to move ws labels for instance)
			testModel.MoveDown();
			testModel.Save();
			Assert.That(writingSystemListUpdatedCalled, Is.True, "WritingSystemListUpdated should have been called after this change");
		}

		[Test]
		public void Model_WritingSystemChanged_CalledOnAbbrevChange()
		{
			var writingSystemChanged = false;
			var mockWsManager = new Mock<IWritingSystemManager>();

			var container = new TestWSContainer(new[] { "fr" });
			var testModel = new FwWritingSystemSetupModel(container,
				FwWritingSystemSetupModel.ListType.Vernacular, mockWsManager.Object);
			testModel.WritingSystemUpdated += (sender, args) =>
			{
				writingSystemChanged = true;
			};
			// Make a change that should notify listeners (refresh the lexicon view for instance)
			testModel.CurrentWsSetupModel.CurrentAbbreviation = "fra";
			testModel.Save();
			Assert.That(writingSystemChanged, Is.True, "WritingSystemUpdated should have been called after this change");
		}

		[Test]
		public void Model_WritingSystemChanged_CalledOnWsIdChange()
		{
			var writingSystemChanged = false;
			var mockWsManager = new Mock<IWritingSystemManager>();

			var container = new TestWSContainer(new[] { "fr" });
			var testModel = new FwWritingSystemSetupModel(container,
				FwWritingSystemSetupModel.ListType.Vernacular, mockWsManager.Object);
			testModel.WritingSystemUpdated += (sender, args) =>
			{
				writingSystemChanged = true;
			};
			// Make a change that should notify listeners (refresh the lexicon view for instance)
			testModel.CurrentWsSetupModel.CurrentRegion = "US";
			testModel.Save();
			Assert.That(writingSystemChanged, Is.True, "WritingSystemUpdated should have been called after this change");
		}

		[Test]
		public void Model_WritingSystemChanged_NotCalledOnIrrelevantChange()
		{
			var writingSystemChanged = false;
			var mockWsManager = new Mock<IWritingSystemManager>();

			var container = new TestWSContainer(new[] { "fr" });
			var testModel = new FwWritingSystemSetupModel(container,
				FwWritingSystemSetupModel.ListType.Vernacular, mockWsManager.Object);
			testModel.WritingSystemUpdated += (sender, args) =>
			{
				writingSystemChanged = true;
			};
			// Make a change that should notify listeners (refresh the lexicon view for instance)
			// ReSharper disable once StringLiteralTypo - Leave me alone ReSharper, it's French!
			testModel.CurrentWsSetupModel.CurrentSpellCheckingId = "aucun";
			testModel.Save();
			Assert.That(writingSystemChanged, Is.False, "WritingSystemUpdated should not have been called after this change");
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
			Assert.That(testModel.CurrentWsSetupModel.CurrentLanguageName, Is.EqualTo(newLangName));
			Assert.That(testModel.LanguageCode, Does.Not.Contain("qaa"),
				"Changing the name should not change the language to private use");
			testModel.SelectWs("en");
			Assert.That(testModel.CurrentWsSetupModel.CurrentLanguageName, Is.EqualTo(newLangName));
			Assert.That(testModel.CurrentWsSetupModel.CurrentLanguageTag, /* REVIEW (Hasso) contain? */ Is.EqualTo("en"));
			Assert.That(testModel.LanguageCode, Does.Not.Contain("qaa"),
				"Changing the name should not change the language to private use");
			testModel.SelectWs("en-fonipa");
			Assert.That(testModel.CurrentWsSetupModel.CurrentLanguageName, Is.EqualTo(newLangName));
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
			Assert.That(testModel.CurrentWsSetupModel.CurrentLanguageName, Is.EqualTo(newLangName));
			testModel.SelectWs("fr");
			Assert.That(testModel.CurrentWsSetupModel.CurrentLanguageName, Is.EqualTo("French"));
		}

		[Test]
		public void WritingSystemName_ChangesOnSwitch()
		{
			var container = new TestWSContainer(new[] { "fr", "en-GB", "en-fonipa" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.SelectWs("en-GB");
			Assert.That(testModel.WritingSystemName, Is.EqualTo("English (United Kingdom)"));
			testModel.SelectWs("en-fonipa");
			Assert.That(testModel.WritingSystemName, Is.EqualTo("English (International Phonetic Alphabet)"));
		}

		[Test]
		public void RightToLeft_ChangesOnSwitch()
		{
			var container = new TestWSContainer(new[] { "fr", "en-GB", "en-fonipa" });
			var fr = container.CurrentVernacularWritingSystems.First();
			fr.RightToLeftScript = true;
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.SelectWs("fr");
			Assert.That(testModel.CurrentWsSetupModel.CurrentRightToLeftScript, Is.True);
			testModel.SelectWs("en-fonipa");
			Assert.That(testModel.CurrentWsSetupModel.CurrentRightToLeftScript, Is.False);
		}

		[Test]
		public void CurrentWritingSystemIndex_IntiallyZero()
		{
			var container = new TestWSContainer(new[] { "fr", "en-GB", "en-fonipa" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			Assert.That(testModel.CurrentWritingSystemIndex, Is.EqualTo(0));
		}

		[TestCase(new[] {"en", "fr"}, "en", 0)]
		[TestCase(new[] {"en", "fr"}, "fr", 1)]
		public void CurrentWritingSystemIndex(string[] list, string current, int index)
		{
			var container = new TestWSContainer(list);
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.SelectWs(current);
			Assert.That(index, Is.EqualTo(testModel.CurrentWritingSystemIndex));
		}

		[Test]
		public void ChangeLanguage_ChangesAllRelated()
		{
			var container = new TestWSContainer(new[] { "fr", "fr-Arab", "fr-GB", "fr-fonipa", "es" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.SelectWs("fr-GB");
			testModel.ShowChangeLanguage = ShowChangeLanguage;
			testModel.ChangeLanguage();
			Assert.That(testModel.CurrentWsSetupModel.CurrentLanguageName, Is.EqualTo("TestName"));
			Assert.That(testModel.LanguageName, Is.EqualTo("TestName"));
			testModel.SelectWs("auc-fonipa");
			Assert.That(testModel.CurrentWsSetupModel.CurrentLanguageName, Is.EqualTo("TestName"));
			testModel.SelectWs("auc-Arab");
			Assert.That(testModel.CurrentWsSetupModel.CurrentLanguageName, Is.EqualTo("TestName"));
			testModel.SelectWs("es");
			Assert.That(testModel.CurrentWsSetupModel.CurrentLanguageName, Is.EqualTo("Spanish"));
		}

		[TestCase(true)]
		[TestCase(false)]
		public void ChangeLanguage_WarnsBeforeCreatingDuplicate(bool userWantsToChangeAnyway)
		{
			var container = new TestWSContainer(new[] { "es", "es-PR", "es-fonipa", "auc" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.SelectWs("es-PR");
			string errorMessage = null;
			testModel.ShowMessageBox = (text, isResponseRequested) => { errorMessage = text; Assert.That(isResponseRequested, Is.True); return userWantsToChangeAnyway; };
			testModel.ShowChangeLanguage = ShowChangeLanguage;

			// SUT
			testModel.ChangeLanguage();

			if (userWantsToChangeAnyway)
			{
				Assert.That(testModel.CurrentWsSetupModel.CurrentLanguageName, Is.EqualTo("TestName"));
				testModel.SelectWs("auc-fonipa");
				Assert.That(testModel.CurrentWsSetupModel.CurrentLanguageName, Is.EqualTo("TestName"), "all WS's for the language should change");
			}
			else
			{
				Assert.That(testModel.CurrentWsSetupModel.CurrentLanguageName, Is.EqualTo("Spanish"));
				testModel.SelectWs("es");
				Assert.That(testModel.CurrentWsSetupModel.CurrentLanguageName, Is.EqualTo("Spanish"), "other WS's shouldn't have changed, either");
				testModel.SelectWs("es-fonipa");
				Assert.That(testModel.CurrentWsSetupModel.CurrentLanguageName, Is.EqualTo("Spanish"), "variant WS's shouldn't have changed, either");
			}
			Assert.That(errorMessage, Does.Contain("This project already has a writing system with the language code"));
		}

		[Test]
		public void ChangeLanguage_WarnsBeforeCreatingDuplicate_FunnyOldScript()
		{
			var container = new TestWSContainer(new[] { "es", "auc-Grek" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.SelectWs("es");
			string errorMessage = null;
			testModel.ShowMessageBox = (text, isResponseRequested) => { errorMessage = text; Assert.That(isResponseRequested, Is.True); return true; };
			testModel.ShowChangeLanguage = ShowChangeLanguage;

			// SUT
			testModel.ChangeLanguage();

			Assert.That(testModel.CurrentWsSetupModel.CurrentLanguageName, Is.EqualTo("TestName"));
			testModel.SelectWs("auc");
			Assert.That(testModel.CurrentWsSetupModel.CurrentLanguageName, Is.EqualTo("TestName"), "the code should have changed");
			Assert.That(errorMessage, Does.Contain("This project already has a writing system with the language code"));
		}

		[Test]
		public void ChangeLanguage_WarnsBeforeCreatingDuplicate_FunnyNewScript()
		{
			var container = new TestWSContainer(new[] { "es", "ja" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.SelectWs("es");
			string errorMessage = null;
			testModel.ShowMessageBox = (text, isResponseRequested) => { errorMessage = text; Assert.That(isResponseRequested, Is.True); return true; };
			const string tagWithScript = "ja-Brai";
			const string desiredName = "Braille for Japanese";
			testModel.ShowChangeLanguage = (out LanguageInfo info) =>
			{
				info = new LanguageInfo { DesiredName = desiredName, LanguageTag = tagWithScript };
				return true;
			};

			// SUT
			testModel.ChangeLanguage();

			Assert.That(testModel.CurrentWsSetupModel.CurrentLanguageName, Is.EqualTo(desiredName));
			testModel.SelectWs(tagWithScript);
			Assert.That(testModel.CurrentWsSetupModel.CurrentLanguageName, Is.EqualTo(desiredName), "the code should have changed");
			Assert.That(errorMessage, Does.Contain("This project already has a writing system with the language code"));
		}

		[Test]
		public void ChangeLanguage_DoesNotChangEnglish()
		{
			var container = new TestWSContainer(new[] { "en", "en-GB", "en-fonipa" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.SelectWs("en-GB");
			string errorMessage = null;
			testModel.ShowMessageBox = (text, isResponseRequested) => { errorMessage = text; Assert.That(isResponseRequested, Is.False); return false; };
			testModel.ShowChangeLanguage = ShowChangeLanguage;
			testModel.ChangeLanguage();
			Assert.That(testModel.CurrentWsSetupModel.CurrentLanguageName, Is.EqualTo("English"));
			Assert.That(testModel.CurrentWsSetupModel.CurrentLanguageTag, Is.EqualTo("en-GB"));
			Assert.That(errorMessage, Is.EqualTo(FwCoreDlgs.kstidCantChangeEnglishWS));
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
			Assert.That(testModel.CurrentWsSetupModel.CurrentLanguageName, Is.EqualTo("TestName"));
			Assert.DoesNotThrow(() => testModel.Save());
			Assert.That(langProj.CurVernWss, Is.EqualTo("auc"));
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
			Assert.That(testModel.CurrentWsSetupModel.CurrentLanguageTag, Is.EqualTo(expectedWs));
		}

		[Test]
		public void LanguageCode_ChangesOnSwitch()
		{
			var container = new TestWSContainer(new[] { "en", "en-GB", "fr-fonipa" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.SelectWs("en-GB");
			Assert.That(testModel.LanguageCode, Is.EqualTo("eng"));
			testModel.SelectWs("fr-fonipa");
			Assert.That(testModel.LanguageCode, Is.EqualTo("fra"));
		}

		[Test]
		public void EthnologueLink_UsesLanguageCode()
		{
			var container = new TestWSContainer(new[] { "fr-Arab-GB-fonipa-x-bogus" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			Assert.That(testModel.EthnologueLabel, Does.EndWith("fra"), "Label didn't end with language code");
			Assert.That(testModel.EthnologueLink, Does.EndWith("fra"), "Link didn't end with language code");
		}

		[Test]
		public void EthnologueLink_ChangesOnSwitch()
		{
			var container = new TestWSContainer(new[] { "en", "fr" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			Assert.That(testModel.EthnologueLabel, Does.EndWith("eng"), "Label didn't end with language code");
			Assert.That(testModel.EthnologueLink, Does.EndWith("eng"), "Link didn't end with language code");
			testModel.SelectWs("fr");
			Assert.That(testModel.EthnologueLabel, Does.EndWith("fra"), "Label didn't end with language code");
			Assert.That(testModel.EthnologueLink, Does.EndWith("fra"), "Link didn't end with language code");
		}

		[Test]
		public void Converters_NoEncodingConverters_ReturnsListWithOnlyNone()
		{
			var container = new TestWSContainer(new[] { "en", "fr" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.EncodingConverterKeys = () => new string[] { };
			var converters = testModel.GetEncodingConverters();
			Assert.That(converters.Count, Is.EqualTo(1));
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
			Assert.That(testModel.CurrentWsListChanged, Is.False);
		}

		[Test]
		public void CurrentWsListChanged_MoveSelectedDown_Returns_True()
		{
			var container = new TestWSContainer(new[] { "en", "fr", "en-Zxxx-x-audio" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.MoveDown();
			Assert.That(testModel.CurrentWsListChanged, Is.True);
		}

		[Test]
		public void CurrentWsListChanged_MoveSelectedUp_Returns_True()
		{
			var container = new TestWSContainer(new[] { "en", "fr", "en-Zxxx-x-audio" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.SelectWs("fr");
			testModel.MoveUp();
			Assert.That(testModel.CurrentWsListChanged, Is.True);
		}

		[Test]
		public void CurrentWsListChanged_MoveUnSelectedDown_Returns_False()
		{
			var container = new TestWSContainer(new[] { "en", "fr", "en-Zxxx-x-audio" }, null, new[] { "en", "en-Zxxx-x-audio" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.SelectWs("fr");
			testModel.MoveDown();
			Assert.That(testModel.CurrentWsListChanged, Is.False);
		}

		[Test]
		public void CurrentWsListChanged_MoveUnSelectedUp_Returns_False()
		{
			var container = new TestWSContainer(new[] { "en", "fr", "en-Zxxx-x-audio" }, null, new[] { "en", "en-Zxxx-x-audio" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.SelectWs("fr");
			testModel.MoveUp();
			Assert.That(testModel.CurrentWsListChanged, Is.False);
		}

		[Test]
		public void CurrentWsListChanged_AddNew_Returns_True()
		{
			var container = new TestWSContainer(new[] { "en", "fr" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.GetAddMenuItems().First(item => item.MenuText.Contains("Audio")).ClickHandler.Invoke(this, new EventArgs());
			Assert.That(testModel.CurrentWsListChanged, Is.True);
		}

		[Test]
		public void CurrentWsListChanged_UnSelectItem_Returns_True()
		{
			var container = new TestWSContainer(new[] { "en", "fr", "en-Zxxx-x-audio" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.ToggleInCurrentList();
			Assert.That(testModel.CurrentWsListChanged, Is.True);
		}

		[Test]
		public void CurrentWsListChanged_SelectItem_Returns_True()
		{
			var container = new TestWSContainer(new[] { "en", "fr", "en-Zxxx-x-audio" }, null, new[] { "en", "en-Zxxx-x-audio" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.SelectWs("fr");
			testModel.ToggleInCurrentList();
			Assert.That(testModel.CurrentWsListChanged, Is.True);
		}

		[Test]
		public void CurrentWsListChanged_HideSelected_Returns_True()
		{
			var container = new TestWSContainer(new[] { "en", "fr", "en-Zxxx-x-audio" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			var menu = testModel.GetRightClickMenuItems();
			menu.First(item => item.MenuText.Contains("Hide")).ClickHandler(this, EventArgs.Empty);
			Assert.That(testModel.CurrentWsListChanged, Is.True);
		}

		[Test]
		public void CurrentWsListChanged_DeleteSelected_Returns_True()
		{
			var container = new TestWSContainer(new[] { "en", "fr", "en-Zxxx-x-audio" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.ConfirmDeleteWritingSystem = label => true;
			var menu = testModel.GetRightClickMenuItems();
			menu.First(item => item.MenuText.Contains("Delete")).ClickHandler(this, EventArgs.Empty);
			Assert.That(testModel.CurrentWsListChanged, Is.True);
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
			Assert.That(testModel.CurrentWsListChanged, Is.False);
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
			Assert.That(testModel.CurrentWsListChanged, Is.False);
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
			Assert.That(testModel.CurrentWsListChanged, Is.False);
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
			Assert.That(warningShown, Is.True, "No homograph ws changed warning shown.");
			Assert.That(Cache.LangProject.HomographWs, Is.EqualTo("fr"), "Homograph ws not changed.");
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
			Assert.That(warningShown, Is.True, "No homograph ws changed warning shown.");
			Assert.That(Cache.LangProject.HomographWs, Is.EqualTo("en"), "Homograph ws should not have been changed.");
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
			Assert.That(warningShown, Is.True, "No homograph ws changed warning shown.");
			Assert.That(Cache.LangProject.HomographWs, Is.EqualTo("fr"), "Homograph ws not changed.");
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
			Assert.That(warningShown, Is.True, "No homograph ws changed warning shown.");
			Assert.That(Cache.LangProject.HomographWs, Is.EqualTo("en-Zxxx-x-audio"), "Homograph ws not changed.");
		}

		[Test]
		public void TopVernIsNotHomographWs_UncheckedWarnsAndSetsNew()
		{
			SetupHomographLanguagesInCache();
			Assert.That(Cache.LangProject.VernWss, Is.EqualTo("en fr"), "Test data setup incorrect, english should be first followed by french");
			Assert.That(Cache.LangProject.CurVernWss, Is.EqualTo("en fr"), "Test data setup incorrect, english should be first followed by french");
			Cache.LangProject.HomographWs = "fr";
			Cache.ActionHandlerAccessor.EndUndoTask();
			var testModel = new FwWritingSystemSetupModel(Cache.LangProject, FwWritingSystemSetupModel.ListType.Vernacular, Cache.ServiceLocator.WritingSystemManager, Cache);
			var warningShown = false;
			testModel.ShouldChangeHomographWs = ws => { warningShown = true; return true; };
			testModel.SelectWs("fr");
			testModel.ToggleInCurrentList();
			testModel.Save();
			Assert.That(warningShown, Is.True, "No homograph ws changed warning shown.");
			Assert.That(Cache.LangProject.HomographWs, Is.EqualTo("en"), "Homograph ws not changed.");
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
			Assert.That(testModel.WorkingList.Select(ws => ws.OriginalWs.LanguageTag), Is.EqualTo(new[] { "en", "fr", "it" }));
			Assert.That(testModel.WorkingList.Select(ws => ws.InCurrentList), Is.EqualTo(new[] { true, true, false }));
			testModel.ToggleInCurrentList();
			Assert.That(testModel.WorkingList.Select(ws => ws.InCurrentList), Is.EqualTo(new[] { false, true, false }));
			// SUT
			testModel.Save();
			Assert.That(Cache.LangProject.CurVernWss, Is.EqualTo("fr"), "Only French should remain selected after save");
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
					Assert.That(model.Items.Any(i => i.WS.Equals(addedWs)), Is.False, $"{addedWs.DisplayLabel} is not quite 'hidden' anymore");

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

			Assert.That(wasDlgShown, Is.True, nameof(wasDlgShown));
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

			Assert.That(testModel.CurrentWsListChanged, Is.True, "Showing a WS changes the 'current' showing list");
			Assert.That(testModel.WorkingList.Select(li => li.OriginalWs.Id), Is.EquivalentTo(expectedList));
			var shown = testModel.WorkingList[testModel.CurrentWritingSystemIndex];
			Assert.That(shown.WorkingWs.Id, Is.EqualTo("hid"));
			Assert.That(shown.InCurrentList, Is.True, "Shown WS should be fully shown");
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

				Assert.That(deleteListener.DeletedWSs, Is.EqualTo(new[] {"doa"}));
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


				Assert.That(wasDeleteConfirmed, Is.True, "should confirm delete");
				AssertEnglishDataIntact();
				if (type == FwWritingSystemSetupModel.ListType.Vernacular)
				{
					Assert.That(Cache.LangProject.CurVernWss, Is.EqualTo("en"), "Only English should remain selected after save");
					Assert.That(Cache.LangProject.VernWss, Is.EqualTo("en"), "Only English should remain after save");
					AssertTokPisinDataIntact();
				}
				else
				{
					Assert.That(Cache.LangProject.CurAnalysisWss, Is.EqualTo("en"), "Only English should remain selected after save");
					Assert.That(Cache.LangProject.AnalysisWss, Is.EqualTo("en"), "Only English should remain after save");
					AssertFrenchDataIntact();
				}
				Assert.That(deleteListener.DeletedWSs, Is.EqualTo(new[] {wsId}));
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

				Assert.That(wasDeleteConfirmed, Is.False, "shouldn't confirm 'deleting' a WS that will only be hidden");
				AssertOnlyEnglishInList(type);
				Assert.That(deleteListener.DeletedWSs, Is.Empty);
				var comment = entry.Comment.get_String(fr.Handle);
				Assert.That(comment.get_WritingSystemAt(0), Is.EqualTo(fr.Handle));
				Assert.That(comment.Text, Is.EqualTo("commentary"));
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
				Assert.That(deleteListener.DeletedWSs, Is.Empty);
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
				Assert.That(deleteListener.DeletedWSs, Is.Empty);
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

				Assert.That(Cache.LangProject.CurVernWss, Is.EqualTo("en fr"), "Both should remain selected after save");
				Assert.That(Cache.LangProject.VernWss, Is.EqualTo("en fr"), "Both should remain after save");
				Assert.That(Cache.LangProject.CurAnalysisWss, Is.EqualTo("en tpi"), "Both should remain selected after save");
				Assert.That(Cache.LangProject.AnalysisWss, Is.EqualTo("en tpi"), "Both should remain after save");
				Assert.That(deleteListener.DeletedWSs, Is.Empty);
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
			Assert.That(entry.CitationForm.get_String(en).Text, Is.EqualTo("enForm"));
			Assert.That(entry.SensesOS.First().Gloss.get_String(en).Text, Is.EqualTo("enSense"));
		}

		private void AssertFrenchDataIntact()
		{
			var fr = GetOrSetWs("fr").Handle;
			var entry = Cache.LangProject.LexDbOA.Entries.First();
			Assert.That(entry.CitationForm.get_String(fr).Text, Is.EqualTo("frHeadword"));
		}

		private void AssertTokPisinDataIntact()
		{
			var tpi = GetOrSetWs("tpi").Handle;
			var entry = Cache.LangProject.LexDbOA.Entries.First();
			Assert.That(entry.SensesOS.First().Gloss.get_String(tpi).Text, Is.EqualTo("tpiSense"));
		}

		private void AssertOnlyEnglishInList(FwWritingSystemSetupModel.ListType type)
		{
			if (type == FwWritingSystemSetupModel.ListType.Vernacular)
			{
				Assert.That(Cache.LangProject.CurVernWss, Is.EqualTo("en"), "Only English should remain selected after save");
				Assert.That(Cache.LangProject.VernWss, Is.EqualTo("en"), "Only English should remain after save");
			}
			else
			{
				Assert.That(Cache.LangProject.CurAnalysisWss, Is.EqualTo("en"), "Only English should remain selected after save");
				Assert.That(Cache.LangProject.AnalysisWss, Is.EqualTo("en"), "Only English should remain after save");
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
			Assert.That(constructedItem, Is.Not.Null, "A default item matching the ws id should be in the list.");
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

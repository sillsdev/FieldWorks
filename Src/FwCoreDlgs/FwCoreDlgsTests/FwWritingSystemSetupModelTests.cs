using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.Core.WritingSystems;
using SIL.Windows.Forms.WritingSystems;
using SIL.WritingSystems;
using SIL.WritingSystems.Tests;
using Rhino;
using Rhino.Mocks;
using Rhino.Mocks.Constraints;
using SIL.FieldWorks.Common.Controls;
using Is = NUnit.Framework.Is;

namespace SIL.FieldWorks.FwCoreDlgs
{
	class FwWritingSystemSetupModelTests
	{

		[SetUp]
		public void SetUp()
		{
		}

		[Test]
		public void CanCreateModel()
		{
			var container = new TestWSContainer(new [] {"en"});
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
			var container = new TestWSContainer(new[] { "en", "fr" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			var menu = testModel.GetRightClickMenuItems().Select(item => item.MenuText);
			CollectionAssert.AreEqual(new[] { "Merge...", "Delete English" }, menu);
			testModel.SelectWs("fr");
			menu = testModel.GetRightClickMenuItems().Select(item => item.MenuText);
			CollectionAssert.AreEqual(new[] { "Merge...", "Delete French" }, menu);
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
			CollectionAssert.AreEqual(new [] { "Add IPA input system for English", "Add Audio input system for English", "Add new dialect of English", "Add new language..." }, addMenu);
			testModel.SelectWs("fr");
			addMenu = testModel.GetAddMenuItems().Select(item => item.MenuText);
			CollectionAssert.AreEqual(new[] { "Add IPA input system for French", "Add Audio input system for French", "Add new dialect of French", "Add new language..." }, addMenu);
		}

		[Test]
		public void WritingSystemList_AddMenuItems_DoesNotOfferExistingOption()
		{
			var container = new TestWSContainer(new[] { "en", "en-fonipa", "en-Zxxx-x-audio" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			var addMenu = testModel.GetAddMenuItems().Select(item => item.MenuText);
			CollectionAssert.AreEqual(new[] { "Add new dialect of English", "Add new language..." }, addMenu);
		}

		[Test]
		public void WritingSystemList_AddMenuItems_DoesNotOfferIpaWhenIpaSelected()
		{
			var container = new TestWSContainer(new[] { "en-fonipa", "en", "en-Zxxx-x-audio" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			var addMenu = testModel.GetAddMenuItems().Select(item => item.MenuText);
			CollectionAssert.AreEqual(new[] { "Add new dialect of English", "Add new language..." }, addMenu);
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
		public void WritingSystemList_ChangeCurrentStatus_StatusChanged()
		{
			var container = new TestWSContainer(new[] { "fr", "en" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			Assert.AreEqual(true, testModel.WorkingList[0].InCurrentList);
			// SUT
			testModel.ChangeCurrentStatus();
			Assert.AreEqual(false, testModel.WorkingList[0].InCurrentList);
		}

		[TestCase("en", new[] { "en" }, false)] // Can't merge if there is no other writing system in the list
		[TestCase("fr", new[] { "fr", "en" }, true)] // Can merge if there is more than one
		public void WritingSystemList_CanMerge(string toMove, string[] options, bool expectedResult)
		{
			var container = new TestWSContainer(options);
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.SelectWs(toMove);
			Assert.AreEqual(expectedResult, testModel.CanMerge());
		}

		[TestCase("en", new[] { "en" }, false)] // Can't merge if there is no other writing system in the list
		[TestCase("fr", new[] { "fr", "en" }, true)] // Can merge if there is more than one
		public void WritingSystemList_CanDelete(string toMove, string[] options, bool expectedResult)
		{
			var container = new TestWSContainer(options);
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.SelectWs(toMove);
			Assert.AreEqual(expectedResult, testModel.CanDelete());
		}

		[Test]
		public void WritingSystemList_IsListValid_FalseIfNoCurrentItems()
		{
			var container = new TestWSContainer(new [] { "en" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.ChangeCurrentStatus();
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
			// Add an audio writing system because it currently doesn't require a cache to create properly
			addMenuItems.First(item => item.MenuText.Contains("Audio")).ClickHandler.Invoke(this, new EventArgs());
			testModel.SelectWs("en");
			// SUT
			var mergeTargets = testModel.MergeTargets;
			Assert.That(mergeTargets.Count(), Is.EqualTo(1));
			Assert.That(mergeTargets.First().WorkingWs.LanguageTag, Is.StringMatching("fr"));
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
		public void Model_ChangedWritingSystemSetInManager()
		{
			// Set up mocks to verify wsManager save behavior
			var mockWsManager = MockRepository.GenerateMock<IWritingSystemManager>();
			mockWsManager.Expect(manager => manager.Set(Arg<CoreWritingSystemDefinition>.Is.Anything)).WhenCalled(a => { }).Repeat.Once();

			var container = new TestWSContainer(new[] { "en", "fr" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular, mockWsManager);
			var enWs = container.VernacularWritingSystems.First();
			testModel.CurrentWsSetupModel.CurrentDefaultFontName = "Test SIL";
			testModel.Save();

			Assert.That(2, Is.EqualTo(container.VernacularWritingSystems.Count));
			mockWsManager.AssertWasCalled(manager => manager.Set(enWs));
		}

		[Test]
		public void Model_ChangesContainerOnlyOnSave()
		{
			// Set up mocks to verify wsManager save behavior
			var mockWsManager = MockRepository.GenerateMock<IWritingSystemManager>();
			mockWsManager.Expect(manager => manager.Set(Arg<CoreWritingSystemDefinition>.Is.Anything)).WhenCalled(a => { }).Repeat.Once();

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
			testModel.SelectWs("en");
			Assert.AreEqual(newLangName, testModel.CurrentWsSetupModel.CurrentLanguageName);
			testModel.SelectWs("en-fonipa");
			Assert.AreEqual(newLangName, testModel.CurrentWsSetupModel.CurrentLanguageName);
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
			var container = new TestWSContainer(new[] { "en", "en-Arab", "en-GB", "en-fonipa", "fr" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.SelectWs("en-GB");
			testModel.ShowChangeLanguage = ShowChangeLanguage;
			testModel.ChangeLanguage();
			Assert.AreEqual("TestName", testModel.CurrentWsSetupModel.CurrentLanguageName);
			Assert.AreEqual("TestName", testModel.LanguageName);
			testModel.SelectWs("auc-fonipa");
			Assert.AreEqual("TestName", testModel.CurrentWsSetupModel.CurrentLanguageName);
			testModel.SelectWs("auc-Arab");
			Assert.AreEqual("TestName", testModel.CurrentWsSetupModel.CurrentLanguageName);
			testModel.SelectWs("fr");
			Assert.AreEqual("French", testModel.CurrentWsSetupModel.CurrentLanguageName);
		}

		[Test]
		public void ChangeLanguage_DoesNotChangeIfWouldCreateDuplicate()
		{
			var container = new TestWSContainer(new[] { "en", "en-GB", "en-fonipa", "auc" });
			var testModel = new FwWritingSystemSetupModel(container, FwWritingSystemSetupModel.ListType.Vernacular);
			testModel.SelectWs("en-GB");
			testModel.ShowChangeLanguage = ShowChangeLanguage;
			testModel.ChangeLanguage();
			Assert.AreEqual("English", testModel.CurrentWsSetupModel.CurrentLanguageName);
			testModel.SelectWs("en");
			Assert.AreEqual("English", testModel.CurrentWsSetupModel.CurrentLanguageName);
		}

		private bool ShowChangeLanguage(out LanguageInfo info)
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
					var ws = new CoreWritingSystemDefinition(lang);
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
						var ws = new CoreWritingSystemDefinition(lang);
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
						_curVern.Add(new CoreWritingSystemDefinition(lang));
					}
				}
				if (curAnaly != null)
				{
					foreach (var lang in curAnaly)
					{
						_curAnaly.Add(new CoreWritingSystemDefinition(lang));
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

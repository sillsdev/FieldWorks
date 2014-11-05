// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Palaso.IO;
using Palaso.Linq;
using Palaso.TestUtilities;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.FDOTests;
using NUnit.Framework;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="Cache is a reference")]
	class DictionaryConfigurationMigratorTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private DictionaryConfigurationMigrator m_migrator = null;
		private Mediator m_mediator = null;
		private FwXApp m_application;
		private string m_configFilePath;
		private MockFwXWindow m_window;
		private FwStyleSheet m_styleSheet;
		private StyleInfoTable m_owningTable;

		[TestFixtureSetUp]
		protected void Init()
		{
			FwRegistrySettings.Init();
			m_application = new MockFwXApp(new MockFwManager { Cache = Cache }, null, null);
			m_configFilePath = Path.Combine(FwDirectoryFinder.CodeDirectory, m_application.DefaultConfigurationPathname);
			m_window = new MockFwXWindow(m_application, m_configFilePath);
			m_window.Init(Cache); // initializes Mediator values
			m_mediator = m_window.Mediator;
			m_window.LoadUI(m_configFilePath); // actually loads UI here; needed for non-null stylesheet

			m_styleSheet = FontHeightAdjuster.StyleSheetFromMediator(m_mediator);
			m_owningTable = new StyleInfoTable("AbbySomebody", (IWritingSystemManager)Cache.WritingSystemFactory);

			m_migrator = new DictionaryConfigurationMigrator(m_mediator);
		}

		[TestFixtureTearDown]
		protected void TearDown()
		{
			m_window.Dispose();
			m_application.Dispose();
			m_mediator.Dispose();
			FwRegistrySettings.Release();
		}

		///<summary/>
		[Test]
		public void ConvertLayoutTreeNodeToConfigNode_BeforeAfterAndBetweenWork()
		{
			ConfigurableDictionaryNode configNode = null;
			var oldNode = new XmlDocConfigureDlg.LayoutTreeNode { After = "]", Between = ",", Before = "["};
			Assert.DoesNotThrow(() => configNode = m_migrator.ConvertLayoutTreeNodeToConfigNode(oldNode));
			Assert.AreEqual(configNode.After, oldNode.After, "After not migrated");
			Assert.AreEqual(configNode.Between, oldNode.Between, "Between not migrated");
			Assert.AreEqual(configNode.Before, oldNode.Before, "Before not migrated");
		}

		///<summary/>
		[Test]
		public void ConvertLayoutTreeNodeToConfigNode_StyleWorks()
		{
			ConfigurableDictionaryNode configNode = null;
			var oldNode = new XmlDocConfigureDlg.LayoutTreeNode { StyleName = "Dictionary-Headword"};
			Assert.DoesNotThrow(() => configNode = m_migrator.ConvertLayoutTreeNodeToConfigNode(oldNode));
			Assert.AreEqual(configNode.Style, oldNode.StyleName, "Style not migrated");
		}

		///<summary/>
		[Test]
		public void ConvertLayoutTreeNodeToConfigNode_MainEntryAndMinorEntryWork()
		{
			var oldMainNode = new XmlDocConfigureDlg.LayoutTreeNode { Label = "Main Entry", ClassName = "LexEntry"};
			var oldMinorNode = new XmlDocConfigureDlg.LayoutTreeNode { Label = "Minor Entry", ClassName = "LexEntry" };
			ConfigurableDictionaryNode configNode = null;
			Assert.DoesNotThrow(() => configNode = m_migrator.ConvertLayoutTreeNodeToConfigNode(oldMainNode));
			Assert.AreEqual(configNode.Label, oldMainNode.Label, "Label Main Entry root node was not migrated");
			Assert.DoesNotThrow(() => configNode = m_migrator.ConvertLayoutTreeNodeToConfigNode(oldMinorNode));
			Assert.AreEqual(configNode.Label, oldMinorNode.Label, "Label for Minor Entry root node was not migrated");
		}

		///<summary/>
		[Test]
		public void ConvertLayoutTreeNodeToConfigNode_IsEnabledWorks()
		{
			var tickedNode = new XmlDocConfigureDlg.LayoutTreeNode { Checked = true };
			var untickedNode = new XmlDocConfigureDlg.LayoutTreeNode { Checked = false };
			ConfigurableDictionaryNode configNode = null;
			Assert.DoesNotThrow(() => configNode = m_migrator.ConvertLayoutTreeNodeToConfigNode(tickedNode));
			Assert.AreEqual(configNode.IsEnabled, tickedNode.Checked, "Checked node in old tree did not set IsEnabled correctly after migration");
			Assert.DoesNotThrow(() => configNode = m_migrator.ConvertLayoutTreeNodeToConfigNode(untickedNode));
			Assert.AreEqual(configNode.IsEnabled, untickedNode.Checked, "Unchecked node in old tree did not set IsEnabled correctly after migration");
		}

		///<summary/>
		[Test]
		public void ConvertLayoutTreeNodeToConfigNode_WritingSystemOptionsAnalysisTypeWorks()
		{
			var nodeWithWs = new XmlDocConfigureDlg.LayoutTreeNode { WsType = "analysis", WsLabel = "analysis"};
			ConfigurableDictionaryNode configNode = null;
			Assert.DoesNotThrow(() => configNode = m_migrator.ConvertLayoutTreeNodeToConfigNode(nodeWithWs));
			Assert.NotNull(configNode.DictionaryNodeOptions, "No DictionaryNodeOptions were created for a treenode with a writing system");
			Assert.IsTrue(configNode.DictionaryNodeOptions is DictionaryNodeWritingSystemOptions, "Writing system options node not created");
			var wsOpts = configNode.DictionaryNodeOptions as DictionaryNodeWritingSystemOptions;
			Assert.AreEqual(wsOpts.WsType, DictionaryNodeWritingSystemOptions.WritingSystemType.Analysis);
			Assert.IsNotNull(wsOpts.Options, "analysis choice did not result in any options being created.");
		}

		///<summary/>
		[Test]
		public void ConvertLayoutTreeNodeToConfigNode_WritingSystemOptionsVernacularTypeWorks()
		{
			var nodeWithWs = new XmlDocConfigureDlg.LayoutTreeNode { WsType = "vernacular", WsLabel = "vernacular" };
			ConfigurableDictionaryNode configNode = null;
			Assert.DoesNotThrow(() => configNode = m_migrator.ConvertLayoutTreeNodeToConfigNode(nodeWithWs));
			Assert.NotNull(configNode.DictionaryNodeOptions, "No DictionaryNodeOptions were created for a treenode with a writing system");
			Assert.IsTrue(configNode.DictionaryNodeOptions is DictionaryNodeWritingSystemOptions, "Writing system options node not created");
			var wsOpts = configNode.DictionaryNodeOptions as DictionaryNodeWritingSystemOptions;
			Assert.AreEqual(wsOpts.WsType, DictionaryNodeWritingSystemOptions.WritingSystemType.Vernacular);
			Assert.IsNotNull(wsOpts.Options, "vernacular choice did not result in any options being created.");
		}

		///<summary/>
		[Test]
		public void ConvertLayoutTreeNodeToConfigNode_WritingSystemOptionsVernacularAnalysisTypeWorks()
		{
			var nodeWithWs = new XmlDocConfigureDlg.LayoutTreeNode { WsType = "vernacular analysis", WsLabel = "vernacular" };
			ConfigurableDictionaryNode configNode = null;
			Assert.DoesNotThrow(() => configNode = m_migrator.ConvertLayoutTreeNodeToConfigNode(nodeWithWs));
			Assert.NotNull(configNode.DictionaryNodeOptions, "No DictionaryNodeOptions were created for a treenode with a writing system");
			Assert.IsTrue(configNode.DictionaryNodeOptions is DictionaryNodeWritingSystemOptions, "Writing system options node not created");
			var wsOpts = configNode.DictionaryNodeOptions as DictionaryNodeWritingSystemOptions;
			Assert.AreEqual(wsOpts.WsType, DictionaryNodeWritingSystemOptions.WritingSystemType.Both);
			Assert.IsNotNull(wsOpts.Options, "vernacular analysis choice did not result in any options being created.");
			Assert.IsNotNull(wsOpts.Options.Find(option => option.IsEnabled && option.Id == "vernacular"), "vernacular choice was not migrated.");
		}

		///<summary/>
		[Test]
		public void ConvertLayoutTreeNodeToConfigNode_WritingSystemOptionsPronunciationTypeWorks()
		{
			var nodeWithWs = new XmlDocConfigureDlg.LayoutTreeNode { WsType = "pronunciation", WsLabel = "pronunciation" };
			ConfigurableDictionaryNode configNode = null;
			Assert.DoesNotThrow(() => configNode = m_migrator.ConvertLayoutTreeNodeToConfigNode(nodeWithWs));
			Assert.NotNull(configNode.DictionaryNodeOptions, "No DictionaryNodeOptions were created for a treenode with a writing system");
			Assert.IsTrue(configNode.DictionaryNodeOptions is DictionaryNodeWritingSystemOptions, "Writing system options node not created");
			var wsOpts = configNode.DictionaryNodeOptions as DictionaryNodeWritingSystemOptions;
			Assert.AreEqual(wsOpts.WsType, DictionaryNodeWritingSystemOptions.WritingSystemType.Pronunciation);
			Assert.IsNotNull(wsOpts.Options, "pronunciation choice did not result in any options being created.");
			Assert.IsNotNull(wsOpts.Options.Find(option => option.IsEnabled && option.Id == "pronunciation"), "pronunciation choice was not migrated.");
		}

		///<summary/>
		[Test]
		public void ConvertLayoutTreeNodeToConfigNode_WritingSystemOptionsAnalysisVernacularTypeWorks()
		{
			var nodeWithWs = new XmlDocConfigureDlg.LayoutTreeNode { WsType = "analysis vernacular", WsLabel = "analysis" };
			ConfigurableDictionaryNode configNode = null;
			Assert.DoesNotThrow(() => configNode = m_migrator.ConvertLayoutTreeNodeToConfigNode(nodeWithWs));
			Assert.NotNull(configNode.DictionaryNodeOptions, "No DictionaryNodeOptions were created for a treenode with a writing system");
			Assert.IsTrue(configNode.DictionaryNodeOptions is DictionaryNodeWritingSystemOptions, "Writing system options node not created");
			var wsOpts = configNode.DictionaryNodeOptions as DictionaryNodeWritingSystemOptions;
			Assert.AreEqual(wsOpts.WsType, DictionaryNodeWritingSystemOptions.WritingSystemType.Both);
			Assert.IsNotNull(wsOpts.Options, "analysis vernacular choice did not result in any options being created.");
			Assert.IsNotNull(wsOpts.Options.Find(option => option.IsEnabled && option.Id == "analysis"), "analysis choice was not migrated.");
		}

		///<summary/>
		[Test]
		public void ConvertLayoutTreeNodeToConfigNode_WritingSystemOptionsVernacularSingleLanguageWorks()
		{
			var nodeWithWs = new XmlDocConfigureDlg.LayoutTreeNode { WsType = "vernacular", WsLabel = "fr" };
			ConfigurableDictionaryNode configNode = null;
			Assert.DoesNotThrow(() => configNode = m_migrator.ConvertLayoutTreeNodeToConfigNode(nodeWithWs));
			Assert.NotNull(configNode.DictionaryNodeOptions, "No DictionaryNodeOptions were created for a treenode with a writing system");
			Assert.IsTrue(configNode.DictionaryNodeOptions is DictionaryNodeWritingSystemOptions, "Writing system options node not created");
			var wsOpts = configNode.DictionaryNodeOptions as DictionaryNodeWritingSystemOptions;
			Assert.AreEqual(wsOpts.WsType, DictionaryNodeWritingSystemOptions.WritingSystemType.Vernacular);
			Assert.IsNotNull(wsOpts.Options, "French choice did not result in any options being created.");
			Assert.IsNotNull(wsOpts.Options.Find(option => option.IsEnabled && option.Id == "fr"), "French choice was not migrated.");
		}

		///<summary/>
		[Test]
		public void ConvertLayoutTreeNodeToConfigNode_WritingSystemOptionsTwoLanguagesWork()
		{
			var nodeWithWs = new XmlDocConfigureDlg.LayoutTreeNode { WsType = "vernacular", WsLabel = "fr, hi" };
			ConfigurableDictionaryNode configNode = null;
			Assert.DoesNotThrow(() => configNode = m_migrator.ConvertLayoutTreeNodeToConfigNode(nodeWithWs));
			Assert.NotNull(configNode.DictionaryNodeOptions, "No DictionaryNodeOptions were created for a treenode with a writing system");
			Assert.IsTrue(configNode.DictionaryNodeOptions is DictionaryNodeWritingSystemOptions, "Writing system options node not created");
			var wsOpts = configNode.DictionaryNodeOptions as DictionaryNodeWritingSystemOptions;
			Assert.AreEqual(wsOpts.WsType, DictionaryNodeWritingSystemOptions.WritingSystemType.Vernacular);
			Assert.IsNotNull(wsOpts.Options, "two languages did not result in ws options being created");
			Assert.IsNotNull(wsOpts.Options.Find(option => option.IsEnabled && option.Id == "fr"), "French choice was not migrated.");
			Assert.IsNotNull(wsOpts.Options.Find(option => option.IsEnabled && option.Id == "hi"), "hi choice was not migrated.");
		}

		///<summary/>
		[Test]
		public void ConvertLayoutTreeNodeToConfigNode_WritingSystemOptionsWsAbbreviationWorks()
		{
			var nodeWithWs = new XmlDocConfigureDlg.LayoutTreeNode { WsType = "vernacular", WsLabel = "fr", ShowWsLabels = true };
			ConfigurableDictionaryNode configNode = null;

			Assert.DoesNotThrow(() => configNode = m_migrator.ConvertLayoutTreeNodeToConfigNode(nodeWithWs));
			Assert.NotNull(configNode.DictionaryNodeOptions, "No DictionaryNodeOptions were created for a treenode with a writing system");
			Assert.IsTrue(configNode.DictionaryNodeOptions is DictionaryNodeWritingSystemOptions, "Writing system options node not created");
			var wsOpts = configNode.DictionaryNodeOptions as DictionaryNodeWritingSystemOptions;
			Assert.IsTrue(wsOpts.DisplayWritingSystemAbbreviations, "ShowWsLabels true value did not convert into DisplayWritingSystemAbbreviation");
			nodeWithWs.ShowWsLabels = false;

			Assert.DoesNotThrow(() => configNode = m_migrator.ConvertLayoutTreeNodeToConfigNode(nodeWithWs));
			wsOpts = configNode.DictionaryNodeOptions as DictionaryNodeWritingSystemOptions;
			Assert.IsFalse(wsOpts.DisplayWritingSystemAbbreviations, "ShowWsLabels false value did not convert into DisplayWritingSystemAbbreviation");
		}

		///<summary/>
		[Test]
		public void ConvertLayoutTreeNodeToConfigNode_ListOptionsEnabledLexRelationWorks()
		{
			const string enabledGuid = "+a0000000-1000-b000-2000-c00000000000";
			var nodeWithSequence = new XmlDocConfigureDlg.LayoutTreeNode { LexRelType = "entry", RelTypeList = LexReferenceInfo.CreateListFromStorageString(enabledGuid) };
			ConfigurableDictionaryNode configNode = null;

			Assert.DoesNotThrow(() => configNode = m_migrator.ConvertLayoutTreeNodeToConfigNode(nodeWithSequence));
			Assert.NotNull(configNode.DictionaryNodeOptions, "No DictionaryNodeOptions were created for a treenode with a LexReferenceInfo");
			Assert.IsTrue(configNode.DictionaryNodeOptions is DictionaryNodeListOptions, "List system options node not created");
			var lexRelationOptions = configNode.DictionaryNodeOptions as DictionaryNodeListOptions;
			Assert.AreEqual(lexRelationOptions.Options.Count, 1);
			Assert.AreEqual(lexRelationOptions.Options[0].Id, enabledGuid.Substring(1));
			Assert.IsTrue(lexRelationOptions.Options[0].IsEnabled);
		}

		///<summary/>
		[Test]
		public void ConvertLayoutTreeNodeToConfigNode_ListOptionsDisabledLexRelationWorks()
		{
			const string disabledGuid = "-a0000000-1000-b000-2000-c00000000000";
			var nodeWithSequence = new XmlDocConfigureDlg.LayoutTreeNode { LexRelType = "entry", RelTypeList = LexReferenceInfo.CreateListFromStorageString(disabledGuid) };
			ConfigurableDictionaryNode configNode = null;

			Assert.DoesNotThrow(() => configNode = m_migrator.ConvertLayoutTreeNodeToConfigNode(nodeWithSequence));
			Assert.NotNull(configNode.DictionaryNodeOptions, "No DictionaryNodeOptions were created for a treenode with a LexReferenceInfo");
			Assert.IsTrue(configNode.DictionaryNodeOptions is DictionaryNodeListOptions, "List system options node not created");
			var lexRelationOptions = configNode.DictionaryNodeOptions as DictionaryNodeListOptions;
			Assert.AreEqual(lexRelationOptions.Options.Count, 1);
			Assert.AreEqual(lexRelationOptions.Options[0].Id, disabledGuid.Substring(1));
			Assert.IsFalse(lexRelationOptions.Options[0].IsEnabled);
		}

		///<summary>Test that a list with two guids migrates both items and keeps their order</summary>
		[Test]
		public void ConvertLayoutTreeNodeToConfigNode_ListOptionsMultipleItemsWorks()
		{
			const string enabledGuid = "b0000000-2000-b000-2000-c00000000000";
			const string disabledGuid = "a0000000-1000-b000-2000-c00000000000";
			var guidList = String.Format("+{0},-{1}", enabledGuid, disabledGuid);
			var nodeWithSequence = new XmlDocConfigureDlg.LayoutTreeNode { LexRelType = "entry", RelTypeList = LexReferenceInfo.CreateListFromStorageString(guidList) };
			ConfigurableDictionaryNode configNode = null;

			Assert.DoesNotThrow(() => configNode = m_migrator.ConvertLayoutTreeNodeToConfigNode(nodeWithSequence));
			Assert.NotNull(configNode.DictionaryNodeOptions, "No DictionaryNodeOptions were created for a treenode with a LexReferenceInfo");
			Assert.IsTrue(configNode.DictionaryNodeOptions is DictionaryNodeListOptions, "List system options node not created");
			var lexRelationOptions = configNode.DictionaryNodeOptions as DictionaryNodeListOptions;
			Assert.AreEqual(lexRelationOptions.Options.Count, 2);
			Assert.AreEqual(lexRelationOptions.Options[0].Id, enabledGuid);
			Assert.IsTrue(lexRelationOptions.Options[0].IsEnabled);
			Assert.AreEqual(lexRelationOptions.Options[1].Id, disabledGuid);
			Assert.IsFalse(lexRelationOptions.Options[1].IsEnabled);
		}

		///<summary/>
		[Test]
		public void ConvertLayoutTreeNodeToConfigNode_ListOptionsEnabledLexEntryTypeWorks()
		{
			const string enabledGuid = "+a0000000-1000-b000-2000-c00000000000";
			var nodeWithSequence = new XmlDocConfigureDlg.LayoutTreeNode { EntryType = "sense", EntryTypeList = ItemTypeInfo.CreateListFromStorageString(enabledGuid) };
			ConfigurableDictionaryNode configNode = null;

			Assert.DoesNotThrow(() => configNode = m_migrator.ConvertLayoutTreeNodeToConfigNode(nodeWithSequence));
			Assert.NotNull(configNode.DictionaryNodeOptions, "No DictionaryNodeOptions were created for the treenode");
			Assert.IsTrue(configNode.DictionaryNodeOptions is DictionaryNodeListOptions, "List system options node not created");
			var lexRelationOptions = configNode.DictionaryNodeOptions as DictionaryNodeListOptions;
			Assert.AreEqual(lexRelationOptions.Options.Count, 1);
			Assert.AreEqual(lexRelationOptions.Options[0].Id, enabledGuid.Substring(1));
			Assert.IsTrue(lexRelationOptions.Options[0].IsEnabled);
		}

		///<summary/>
		[Test]
		public void ConvertLayoutTreeNodeToConfigNode_ListOptionsDisabledLexEntryTypeWorks()
		{
			const string disabledGuid = "-a0000000-1000-b000-2000-c00000000000";
			var nodeWithSequence = new XmlDocConfigureDlg.LayoutTreeNode { EntryType = "variant", EntryTypeList = ItemTypeInfo.CreateListFromStorageString(disabledGuid) };
			ConfigurableDictionaryNode configNode = null;

			Assert.DoesNotThrow(() => configNode = m_migrator.ConvertLayoutTreeNodeToConfigNode(nodeWithSequence));
			Assert.NotNull(configNode.DictionaryNodeOptions, "No DictionaryNodeOptions were created for the treenode");
			Assert.IsTrue(configNode.DictionaryNodeOptions is DictionaryNodeListOptions, "List system options node not created");
			var lexRelationOptions = configNode.DictionaryNodeOptions as DictionaryNodeListOptions;
			Assert.AreEqual(lexRelationOptions.Options.Count, 1);
			Assert.AreEqual(lexRelationOptions.Options[0].Id, disabledGuid.Substring(1));
			Assert.IsFalse(lexRelationOptions.Options[0].IsEnabled);
		}

		/// <summary>
		/// A XmlDocConfigureDlg.LayoutTreeNode.Label includes the suffix. A ConfigurableDictionaryNode.Label does not.
		/// </summary>
		[Test]
		public void ConvertLayoutTreeNodeToConfigNode_DupStringInfoIsConverted()
		{
			var duplicateNode = new XmlDocConfigureDlg.LayoutTreeNode { DupString = "1", IsDuplicate = true, Label = "A b c (1)" };
			ConfigurableDictionaryNode configNode = null;
			Assert.DoesNotThrow(() => configNode = m_migrator.ConvertLayoutTreeNodeToConfigNode(duplicateNode));
			Assert.IsTrue(configNode.IsDuplicate, "Duplicate node not marked as duplicate.");
			Assert.AreEqual(duplicateNode.DupString, configNode.LabelSuffix, "number appended to old duplicates not migrated to label suffix");
			Assert.That(configNode.Label, Is.EqualTo("A b c"), "should not have a suffix on ConfigurableDictionaryNode.Label");

			var originalNode = new XmlDocConfigureDlg.LayoutTreeNode { IsDuplicate = false };
			Assert.DoesNotThrow(() => configNode = m_migrator.ConvertLayoutTreeNodeToConfigNode(originalNode));
			Assert.IsFalse(configNode.IsDuplicate, "node should not have been marked as a duplicate");
			Assert.IsTrue(String.IsNullOrEmpty(configNode.LabelSuffix), "suffix should be empty.");
		}

		/// <summary>
		/// A XmlDocConfigureDlg.LayoutTreeNode that is a duplicate of a duplicate will have a DupString of the form "1-2" but
		/// a Label of the form "Foo (2)", where "1" was the DupString of the first duplicate.
		/// </summary>
		[Test]
		public void ConvertLayoutTreeNodeToConfigNode_DupStringInfoIsConvertedForDuplicateOfDuplicate()
		{
			var duplicateNode = new XmlDocConfigureDlg.LayoutTreeNode { DupString = "1-2", IsDuplicate = true, Label = "A b c (2)" };
			ConfigurableDictionaryNode configNode = null;
			Assert.DoesNotThrow(() => configNode = m_migrator.ConvertLayoutTreeNodeToConfigNode(duplicateNode));
			Assert.IsTrue(configNode.IsDuplicate, "Duplicate node not marked as duplicate.");
			Assert.AreEqual("2", configNode.LabelSuffix, "incorrect suffix migrated");
			Assert.That(configNode.Label, Is.EqualTo("A b c"), "should not have a suffix on ConfigurableDictionaryNode.Label");
		}

		///<summary/>
		[Test]
		public void ConvertLayoutTreeNodeToConfigNode_ChildrenAreAdded()
		{
			var parentNode = new XmlDocConfigureDlg.LayoutTreeNode { Label = "Parent" };
			var childNode = new XmlDocConfigureDlg.LayoutTreeNode { Label = "Child" };
			parentNode.Nodes.Add(childNode);
			ConfigurableDictionaryNode configNode = null;

			Assert.DoesNotThrow(() => configNode = m_migrator.ConvertLayoutTreeNodeToConfigNode(parentNode));
			Assert.AreEqual(configNode.Label, parentNode.Label);
			Assert.IsNotNull(configNode.Children);
			Assert.AreEqual(configNode.Children.Count, 1);
			Assert.AreEqual(configNode.Children[0].Label, childNode.Label);
		}

		///<summary/>
		[Test]
		public void ConvertLayoutTreeNodeToConfigNode_SenseNumberStyleIsAddedAndUsed()
		{
			var senseNumberNode = new XmlDocConfigureDlg.LayoutTreeNode
			{
				Label = "Parent",
				NumFont = "arial",
				NumStyle = "bold -italic",
				ShowSenseConfig = true
			};
			ConfigurableDictionaryNode configNode = null;
			const string styleName = "Dictionary-SenseNumber";
			var senseStyle = m_styleSheet.FindStyle(styleName);
			Assert.IsNull(senseStyle, "Sense number should not exist before conversion for a valid test.");

			Assert.DoesNotThrow(() => configNode = m_migrator.ConvertLayoutTreeNodeToConfigNode(senseNumberNode));
			Assert.AreEqual(((DictionaryNodeSenseOptions)configNode.DictionaryNodeOptions).NumberStyle, styleName);
			senseStyle = m_styleSheet.FindStyle(styleName);
			Assert.IsNotNull(senseStyle, "Sense number should have been created by the migrator.");
			var usefulStyle = m_styleSheet.Styles[styleName];
			Assert.IsTrue(usefulStyle.DefaultCharacterStyleInfo.Bold.Value, "bold was not turned on in the created style.");
			Assert.IsFalse(usefulStyle.DefaultCharacterStyleInfo.Italic.Value, "italic was not turned off in the created style.");
			Assert.AreEqual(usefulStyle.DefaultCharacterStyleInfo.FontName.Value, "arial", "arial font not used");
			DeleteStyleSheet(styleName);
		}

		///<summary/>
		[Test]
		public void ConvertLayoutTreeNodeToConfigNode_SenseConfigsWithDifferingStylesMakeTwoStyles()
		{
			var senseNumberNode = new XmlDocConfigureDlg.LayoutTreeNode
			{
				Label = "Parent",
				NumFont = "arial",
				NumStyle = "bold -italic",
				ShowSenseConfig = true
			};
			var senseNumberNode2 = new XmlDocConfigureDlg.LayoutTreeNode
			{
				Label = "Parent",
				NumFont = "arial",
				NumStyle = "bold",
				ShowSenseConfig = true
			};
			ConfigurableDictionaryNode configNode = null;
			const string styleName = "Dictionary-SenseNumber";
			const string styleName2 = "Dictionary-SenseNumber-2";
			var senseStyle = m_styleSheet.FindStyle(styleName);
			var senseStyle2 = m_styleSheet.FindStyle(styleName2);
			Assert.IsNull(senseStyle, "Sense number style should not exist before conversion for a valid test.");
			Assert.IsNull(senseStyle2, "Second sense number style should not exist before conversion for a valid test.");

			Assert.DoesNotThrow(() => configNode = m_migrator.ConvertLayoutTreeNodeToConfigNode(senseNumberNode));
			Assert.AreEqual(((DictionaryNodeSenseOptions)configNode.DictionaryNodeOptions).NumberStyle, styleName);
			Assert.DoesNotThrow(() => configNode = m_migrator.ConvertLayoutTreeNodeToConfigNode(senseNumberNode2));
			Assert.AreEqual(((DictionaryNodeSenseOptions)configNode.DictionaryNodeOptions).NumberStyle, styleName2);
			senseStyle = m_styleSheet.FindStyle(styleName);
			senseStyle2 = m_styleSheet.FindStyle(styleName2);
			Assert.IsNotNull(senseStyle, "Sense number should have been created by the migrator.");
			Assert.IsNotNull(senseStyle2, "Sense number should have been created by the migrator.");
			var usefulStyle = m_styleSheet.Styles[styleName];
			Assert.IsTrue(usefulStyle.DefaultCharacterStyleInfo.Bold.Value, "bold was not turned on in the created style.");
			Assert.IsFalse(usefulStyle.DefaultCharacterStyleInfo.Italic.Value, "italic was not turned off in the created style.");
			Assert.AreEqual(usefulStyle.DefaultCharacterStyleInfo.FontName.Value, "arial", "arial font not used");
			usefulStyle = m_styleSheet.Styles[styleName2];
			Assert.IsTrue(usefulStyle.DefaultCharacterStyleInfo.Bold.Value, "bold was not turned on in the created style.");
			Assert.IsFalse(usefulStyle.DefaultCharacterStyleInfo.Italic.ValueIsSet, "italic should not have been set in the created style.");
			Assert.AreEqual(usefulStyle.DefaultCharacterStyleInfo.FontName.Value, "arial", "arial font not used");
			DeleteStyleSheet(styleName);
			DeleteStyleSheet(styleName2);
		}

		///<summary/>
		[Test]
		public void ConvertLayoutTreeNodeToConfigNode_AllDifferentNumStylesResultInNewStyleSheets()
		{
			var senseNumberOptions = new [] { "-bold -italic", "bold italic", "bold", "italic", "-bold italic", "bold -italic" };
			var senseNumberNode = new XmlDocConfigureDlg.LayoutTreeNode
			{
				Label = "Parent",
				NumFont = "arial",
				ShowSenseConfig = true
			};
			ConfigurableDictionaryNode configNode = null;
			const string styleName = "Dictionary-SenseNumber";
			var lastStyleName = String.Format("Dictionary-SenseNumber-{0}", 1 + senseNumberOptions.Length);
			var senseStyle = m_styleSheet.FindStyle(styleName);
			var senseStyle2 = m_styleSheet.FindStyle(lastStyleName);
			Assert.IsNull(senseStyle, "Sense number style should not exist before conversion for a valid test.");
			Assert.IsNull(senseStyle2, "Second sense number style should not exist before conversion for a valid test.");

			Assert.DoesNotThrow(() => configNode = m_migrator.ConvertLayoutTreeNodeToConfigNode(senseNumberNode));
			Assert.AreEqual(((DictionaryNodeSenseOptions)configNode.DictionaryNodeOptions).NumberStyle, styleName);
			foreach(var option in senseNumberOptions)
			{
				senseNumberNode.NumStyle = option;
				Assert.DoesNotThrow(() => configNode = m_migrator.ConvertLayoutTreeNodeToConfigNode(senseNumberNode));
			}
			Assert.AreEqual(((DictionaryNodeSenseOptions)configNode.DictionaryNodeOptions).NumberStyle, lastStyleName);
			DeleteStyleSheet(styleName);
			for(var i = 2; i < 2 + senseNumberOptions.Length; i++) // Delete all the created dictionary styles
				DeleteStyleSheet(String.Format("Dictionary-SenseNumber-{0}", i));
		}

		///<summary/>
		[Test]
		public void ConvertLayoutTreeNodeToConfigNode_AllDifferentNumStylesMatchThemselves()
		{
			var senseNumberOptions = new[] { "-bold -italic", "bold italic", "bold", "italic", "-bold italic", "bold -italic", "" };
			var senseNumberNode = new XmlDocConfigureDlg.LayoutTreeNode
			{
				Label = "Parent",
				NumFont = "arial",
				ShowSenseConfig = true
			};
			ConfigurableDictionaryNode configNode = null;
			const string styleName = "Dictionary-SenseNumber";
			var senseStyle = m_styleSheet.FindStyle(styleName);
			const string styleName2 = "Dictionary-SenseNumber-2";
			var senseStyle2 = m_styleSheet.FindStyle(styleName2);
			Assert.IsNull(senseStyle, "Sense number style should not exist before conversion for a valid test.");
			Assert.IsNull(senseStyle2, "A second sense number style should not exist before conversion for a valid test.");

			foreach(var option in senseNumberOptions)
			{
				senseNumberNode.NumStyle = option;
				Assert.DoesNotThrow(() => configNode = m_migrator.ConvertLayoutTreeNodeToConfigNode(senseNumberNode));
				Assert.DoesNotThrow(() => configNode = m_migrator.ConvertLayoutTreeNodeToConfigNode(senseNumberNode));
				senseStyle2 = m_styleSheet.FindStyle(styleName2);
				DeleteStyleSheet(styleName);
				Assert.IsNull(senseStyle2, "A duplicate sense number style should not have been created converting the same node twice.");
			}
		}

		///<summary/>
		[Test]
		public void ConvertLayoutTreeNodeToConfigNode_SenseConfigsWithDifferentFontsMakeTwoStyles()
		{
			var senseNumberNode = new XmlDocConfigureDlg.LayoutTreeNode
			{
				Label = "Parent",
				NumFont = "arial",
				ShowSenseConfig = true
			};
			var senseNumberNode2 = new XmlDocConfigureDlg.LayoutTreeNode
			{
				Label = "Parent",
				NumFont = "notarial",
				ShowSenseConfig = true
			};
			ConfigurableDictionaryNode configNode = null;
			const string styleName = "Dictionary-SenseNumber";
			const string styleName2 = "Dictionary-SenseNumber-2";
			var senseStyle = m_styleSheet.FindStyle(styleName);
			var senseStyle2 = m_styleSheet.FindStyle(styleName2);
			Assert.IsNull(senseStyle, "Sense number style should not exist before conversion for a valid test.");
			Assert.IsNull(senseStyle2, "Second sense number style should not exist before conversion for a valid test.");

			Assert.DoesNotThrow(() => configNode = m_migrator.ConvertLayoutTreeNodeToConfigNode(senseNumberNode));
			Assert.AreEqual(((DictionaryNodeSenseOptions)configNode.DictionaryNodeOptions).NumberStyle, styleName);
			Assert.DoesNotThrow(() => configNode = m_migrator.ConvertLayoutTreeNodeToConfigNode(senseNumberNode2));
			Assert.AreEqual(((DictionaryNodeSenseOptions)configNode.DictionaryNodeOptions).NumberStyle, styleName2);
			senseStyle = m_styleSheet.FindStyle(styleName);
			senseStyle2 = m_styleSheet.FindStyle(styleName2);
			Assert.IsNotNull(senseStyle, "Sense number should have been created by the migrator.");
			Assert.IsNotNull(senseStyle2, "Sense number should have been created by the migrator.");
			var usefulStyle = m_styleSheet.Styles[styleName];
			Assert.AreEqual(usefulStyle.DefaultCharacterStyleInfo.FontName.Value, "arial", "arial font not used");
			usefulStyle = m_styleSheet.Styles[styleName2];
			Assert.AreEqual(usefulStyle.DefaultCharacterStyleInfo.FontName.Value, "notarial", "notarial font not used in second style");
			DeleteStyleSheet(styleName);
			DeleteStyleSheet(styleName2);
		}

		///<summary/>
		[Test]
		public void ConvertLayoutTreeNodeToConfigNode_SenseOptionsAreMigrated()
		{
			var senseNumberNode = new XmlDocConfigureDlg.LayoutTreeNode
			{
				Label = "Parent",
				Number = "(%O)",
				NumberSingleSense = true,
				ShowSenseConfig = true
			};
			ConfigurableDictionaryNode configNode = null;
			Assert.DoesNotThrow(() => configNode = m_migrator.ConvertLayoutTreeNodeToConfigNode(senseNumberNode));
			var senseOptions = configNode.DictionaryNodeOptions as DictionaryNodeSenseOptions;
			Assert.NotNull(senseOptions);
			Assert.IsTrue(senseOptions.NumberEvenASingleSense);
			Assert.AreEqual("(", senseOptions.BeforeNumber);
			Assert.AreEqual(")", senseOptions.AfterNumber);
			Assert.AreEqual("%O", senseOptions.NumberingStyle);
			DeleteStyleSheet("Dictionary-SenseNumber");
		}

		///<summary/>
		[Test]
		public void CopyNewDefaultsIntoConvertedModel_FieldDescriptionIsMigrated()
		{
			var convertedParentNode = new ConfigurableDictionaryNode { Label = "Parent" };
			var convertedChildNode = new ConfigurableDictionaryNode { Label = "Child" };
			convertedParentNode.Children = new List<ConfigurableDictionaryNode> { convertedChildNode };
			var convertedModel = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { convertedParentNode } };
			const string parentField = "ParentDescription";
			var baseParentNode = new ConfigurableDictionaryNode { Label = "Parent", FieldDescription = parentField};
			const string childField = "ChildDescription";
			var baseChildNode = new ConfigurableDictionaryNode { Label = "Child", FieldDescription = childField };
			baseParentNode.Children = new List<ConfigurableDictionaryNode> { baseChildNode };
			var baseModel = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { baseParentNode } };

			Assert.DoesNotThrow(() => m_migrator.CopyNewDefaultsIntoConvertedModel(convertedModel, baseModel));
			Assert.AreEqual(convertedModel.Parts[0].FieldDescription, parentField, "Field description for parent node not migrated");
			Assert.AreEqual(convertedModel.Parts[0].Children[0].FieldDescription, childField, "Field description for child not migrated");
		}

		///<summary/>
		[Test]
		public void CopyNewDefaultsIntoConvertedModel_CSSClassOverrideIsMigrated()
		{
			var convertedParentNode = new ConfigurableDictionaryNode { Label = "Parent" };
			var convertedChildNode = new ConfigurableDictionaryNode { Label = "Child" };
			convertedParentNode.Children = new List<ConfigurableDictionaryNode> { convertedChildNode };
			var convertedModel = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { convertedParentNode } };
			const string parentOverride = "dad";
			var baseParentNode = new ConfigurableDictionaryNode { Label = "Parent", CSSClassNameOverride = parentOverride };
			const string childOverride = "johnboy";
			var baseChildNode = new ConfigurableDictionaryNode { Label = "Child", CSSClassNameOverride = childOverride };
			baseParentNode.Children = new List<ConfigurableDictionaryNode> { baseChildNode };
			var baseModel = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { baseParentNode } };

			Assert.DoesNotThrow(() => m_migrator.CopyNewDefaultsIntoConvertedModel(convertedModel, baseModel));
			Assert.AreEqual(convertedModel.Parts[0].CSSClassNameOverride, parentOverride, "CssClassNameOverride for parent node not migrated");
			Assert.AreEqual(convertedModel.Parts[0].Children[0].CSSClassNameOverride, childOverride, "CssClassNameOverride for child not migrated");
		}

		///<summary/>
		[Test]
		public void CopyNewDefaultsIntoConvertedModel_CopyOfNodeGetsValueFromBase()
		{
			var convertedParentNode = new ConfigurableDictionaryNode { Label = "Parent" };
			// make convertedChildNode look like a copy of a Child node which is not represented in the test.
			var convertedChildNode = new ConfigurableDictionaryNode { Label = "Child", LabelSuffix = "1"};
			convertedParentNode.Children = new List<ConfigurableDictionaryNode> { convertedChildNode };
			var convertedModel = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { convertedParentNode } };
			const string parentField = "ParentDescription";
			var baseParentNode = new ConfigurableDictionaryNode { Label = "Parent", FieldDescription = parentField };
			const string childField = "ChildDescription";
			var baseChildNode = new ConfigurableDictionaryNode { Label = "Child", FieldDescription = childField };
			baseParentNode.Children = new List<ConfigurableDictionaryNode> { baseChildNode };
			var baseModel = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { baseParentNode } };

			Assert.DoesNotThrow(() => m_migrator.CopyNewDefaultsIntoConvertedModel(convertedModel, baseModel));
			Assert.AreEqual(convertedModel.Parts[0].Children[0].FieldDescription, childField, "Field description for copy of child not migrated");
		}

		///<summary/>
		[Test]
		public void CopyNewDefaultsIntoConvertedModel_TwoCopiesBothGetValueFromBase()
		{
			var convertedParentNode = new ConfigurableDictionaryNode { Label = "Parent" };
			var convertedChildNode = new ConfigurableDictionaryNode { Label = "Child" };
			var convertedChildNodeCopy1 = new ConfigurableDictionaryNode { Label = "Child", LabelSuffix = "1" };
			var convertedChildNodeCopy2 = new ConfigurableDictionaryNode { Label = "Child", LabelSuffix = "2" };
			convertedParentNode.Children = new List<ConfigurableDictionaryNode> { convertedChildNode, convertedChildNodeCopy1, convertedChildNodeCopy2 };
			var convertedModel = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { convertedParentNode } };
			const string parentField = "ParentDescription";
			var baseParentNode = new ConfigurableDictionaryNode { Label = "Parent", FieldDescription = parentField };
			const string childField = "ChildDescription";
			var baseChildNode = new ConfigurableDictionaryNode { Label = "Child", FieldDescription = childField };
			baseParentNode.Children = new List<ConfigurableDictionaryNode> { baseChildNode };
			var baseModel = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { baseParentNode } };

			Assert.DoesNotThrow(() => m_migrator.CopyNewDefaultsIntoConvertedModel(convertedModel, baseModel));
			Assert.AreEqual(convertedModel.Parts[0].Children.Count, 3, "The copied children did not get migrated");
			Assert.AreEqual(convertedModel.Parts[0].Children[0].FieldDescription, childField, "Field description for copy of child not migrated");
			Assert.AreEqual(convertedModel.Parts[0].Children[1].FieldDescription, childField, "Field description for copy of child not migrated");
			Assert.AreEqual(convertedModel.Parts[0].Children[2].FieldDescription, childField, "Field description for copy of child not migrated");
		}

		///<summary/>
		[Test]
		public void CopyNewDefaultsIntoConvertedModel_NewNodeFromBaseIsMerged()
		{
			var convertedParentNode = new ConfigurableDictionaryNode { Label = "Parent" };
			var convertedChildNode = new ConfigurableDictionaryNode { Label = "Child" };
			convertedParentNode.Children = new List<ConfigurableDictionaryNode> { convertedChildNode };
			var convertedModel = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { convertedParentNode } };
			var baseParentNode = new ConfigurableDictionaryNode { Label = "Parent" };
			var baseChildNode = new ConfigurableDictionaryNode { Label = "Child" };
			var baseChildNodeTwo = new ConfigurableDictionaryNode { Label = "Child2" };
			baseParentNode.Children = new List<ConfigurableDictionaryNode> { baseChildNode, baseChildNodeTwo };
			var baseModel = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { baseParentNode } };

			Assert.DoesNotThrow(() => m_migrator.CopyNewDefaultsIntoConvertedModel(convertedModel, baseModel));
			Assert.AreEqual(convertedModel.Parts[0].Children.Count, 2, "New node from base was not merged");
			Assert.AreEqual(convertedModel.Parts[0].Children[0].Label, "Child", "new node inserted out of order");
			Assert.AreEqual(convertedModel.Parts[0].Children[1].Label, "Child2", "New node from base was not merged properly");
		}

		///<summary/>
		[Test]
		public void CopyNewDefaultsIntoConvertedModel_OrderFromOldModelIsRetained()
		{
			var convertedParentNode = new ConfigurableDictionaryNode { Label = "Parent" };
			var convertedChildNode = new ConfigurableDictionaryNode { Label = "Child" };
			var convertedChildNodeTwo = new ConfigurableDictionaryNode { Label = "Child2" };
			convertedParentNode.Children = new List<ConfigurableDictionaryNode> { convertedChildNodeTwo, convertedChildNode };
			var convertedModel = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { convertedParentNode } };
			var baseParentNode = new ConfigurableDictionaryNode { Label = "Parent" };
			var baseChildNode = new ConfigurableDictionaryNode { Label = "Child" };
			var baseChildNodeTwo = new ConfigurableDictionaryNode { Label = "Child2" };
			baseParentNode.Children = new List<ConfigurableDictionaryNode> { baseChildNode, baseChildNodeTwo };
			var baseModel = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { baseParentNode } };

			Assert.DoesNotThrow(() => m_migrator.CopyNewDefaultsIntoConvertedModel(convertedModel, baseModel));
			Assert.AreEqual(convertedModel.Parts[0].Children.Count, 2, "Nodes incorrectly merged");
			Assert.AreEqual(convertedModel.Parts[0].Children[0].Label, convertedChildNodeTwo.Label, "order of old model was not retained");
			Assert.AreEqual(convertedModel.Parts[0].Children[1].Label, convertedChildNode.Label, "Nodes incorrectly merged");
		}

		///<summary/>
		[Test]
		public void CopyNewDefaultsIntoConvertedModel_UnmatchedNodeFromOldModelIsCustom()
		{
			var convertedParentNode = new ConfigurableDictionaryNode { Label = "Parent" };
			var customNode = new ConfigurableDictionaryNode { Label = "Custom" };
			var oldChild = new ConfigurableDictionaryNode { Label = "Child" };
			convertedParentNode.Children = new List<ConfigurableDictionaryNode> { customNode, oldChild };
			var convertedModel = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { convertedParentNode } };
			var baseParentNode = new ConfigurableDictionaryNode { Label = "Parent" };
			var baseChildNode = new ConfigurableDictionaryNode { Label = "Child" };
			baseParentNode.Children = new List<ConfigurableDictionaryNode> { baseChildNode };
			var baseModel = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { baseParentNode } };

			Assert.DoesNotThrow(() => m_migrator.CopyNewDefaultsIntoConvertedModel(convertedModel, baseModel));
			Assert.AreEqual(convertedModel.Parts[0].Children.Count, 2, "Nodes incorrectly merged");
			Assert.AreEqual(convertedModel.Parts[0].Children[0].Label, customNode.Label, "order of old model was not retained");
			Assert.IsFalse(oldChild.IsCustomField, "Child node which is matched should not be a custom field");
			Assert.IsTrue(customNode.IsCustomField, "The unmatched 'Custom' node should have been marked as a custom field");
			Assert.AreEqual(customNode.Label, customNode.FieldDescription, "Custom nodes' Labels and Fields should match");
		}

		///<summary/>
		[Test]
		public void CopyNewDefaultsIntoConvertedModel_NestedCustomFieldsAreAllMarked()
		{
			var convertedParentNode = new ConfigurableDictionaryNode { Label = "Parent" };
			var customNode = new ConfigurableDictionaryNode { Label = "Custom" };
			var oldChild = new ConfigurableDictionaryNode { Label = "Child" };
			convertedParentNode.Children = new List<ConfigurableDictionaryNode> { customNode, oldChild };
			var customChild = new ConfigurableDictionaryNode { Label = "Custom Child" };
			customNode.Children = new List<ConfigurableDictionaryNode> { customChild };
			var convertedModel = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { convertedParentNode } };
			var baseParentNode = new ConfigurableDictionaryNode { Label = "Parent" };
			var baseChildNode = new ConfigurableDictionaryNode { Label = "Child" };
			baseParentNode.Children = new List<ConfigurableDictionaryNode> { baseChildNode };
			var baseModel = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { baseParentNode } };

			Assert.DoesNotThrow(() => m_migrator.CopyNewDefaultsIntoConvertedModel(convertedModel, baseModel));
			Assert.AreEqual(convertedModel.Parts[0].Children.Count, 2, "Nodes incorrectly merged");
			Assert.AreEqual(convertedModel.Parts[0].Children[0].Label, customNode.Label, "order of old model was not retained");
			Assert.IsFalse(oldChild.IsCustomField, "Child node which is matched should not be a custom field");
			Assert.IsTrue(customNode.IsCustomField, "The unmatched 'Custom' node should have been marked as a custom field");
			Assert.IsTrue(customChild.IsCustomField, "Children of Custom nodes should also be Custom.");
			Assert.AreEqual(customNode.Label, customNode.FieldDescription, "Custom nodes' Labels and Fields should match");
			Assert.AreEqual(customChild.Label, customChild.FieldDescription, "Custom nodes' Labels and Fields should match");
		}

		#region Split Referenced Complex Forms
		/// <remarks>
		/// It takes 0.2 seconds to save this model to a temp file and load it
		/// It takes 0.01 seconds to load and clean Root from xml for testing
		/// It takes 0.02 seconds to save the merged model
		/// It saves 0.002 seconds per AssertThatXmlIn.File to use a sparse Root model rather than using the full one
		/// It takes 0.27 seconds (approx) to run the longest test involving this model
		/// </remarks>
		private const string ConvertedModelXml = DictionaryConfigurationModelTests.XmlOpenTagsThruRoot + @"
			<ConfigurationItem name=""Main Entry"">
				<ConfigurationItem name=""Senses"" between=""--TESTING--"" isEnabled=""true"">
					<ConfigurationItem name=""Referenced Complex Forms"" between=""--TESTING--"" isEnabled=""true"">
						<ComplexFormOptions list=""complex"" displayEachComplexFormInParagraph=""true"">
							<Option isEnabled=""true"" id=""a0000000-dd15-4a03-9032-b40faaa9a754""/>
						</ComplexFormOptions>
					</ConfigurationItem>
				</ConfigurationItem>
			</ConfigurationItem>
			<ConfigurationItem name=""Minor Entry"" isEnabled=""true"">
				<ConfigurationItem name=""Senses"" between=""--TESTING--"" isEnabled=""true"">
					<ConfigurationItem name=""Referenced Complex Forms"" between=""--TESTING--"" isEnabled=""true""/>
				</ConfigurationItem>
				<ConfigurationItem name=""Referenced Complex Forms"" between=""--TESTING--"" isEnabled=""true"">
					<ComplexFormOptions list=""complex"" displayEachComplexFormInParagraph=""true"">
						<Option isEnabled=""true"" id=""a0000000-dd15-4a03-9032-b40faaa9a754""/>
					</ComplexFormOptions>
					<ConfigurationItem name=""Complex Form Type"" between=""--TESTING--"" isEnabled=""true"">
						<ConfigurationItem name=""Reverse Abbreviation"" between=""--TESTING--"" isEnabled=""true"">
							<WritingSystemOptions writingSystemType=""analysis"">
								<Option id=""analysis"" isEnabled=""true""/>
							</WritingSystemOptions>
						</ConfigurationItem>
					</ConfigurationItem>
					<ConfigurationItem name=""Complex Form"" between=""--TESTING--"" isEnabled=""true""/>
					<ConfigurationItem name=""Grammatical Info."" between=""--TESTING--"" style=""Dictionary-Contrasting"" isEnabled=""true"">
						<ConfigurationItem name=""Gram Info (Name)"" between=""--TESTING--"" isEnabled=""false""/>
					</ConfigurationItem>
					<ConfigurationItem name=""Definition (or Gloss)"" between=""--TESTING--"" isEnabled=""true"">
						<WritingSystemOptions writingSystemType=""analysis"">
							<Option id=""all analysis"" isEnabled=""true""/>
						</WritingSystemOptions>
					</ConfigurationItem>
					<ConfigurationItem name=""Comment"" between=""--TESTING--""/>
					<ConfigurationItem name=""Summary Definition"" between=""--TESTING--"" isEnabled=""true""/>
					<ConfigurationItem name=""Example Sentences"" between=""--TESTING--"">
						<ConfigurationItem name=""Example"" between=""--TESTING--"" style=""Dictionary-Vernacular"">
							<WritingSystemOptions writingSystemType=""vernacular"" displayWSAbreviation=""false"">
								<Option id=""all vernacular"" isEnabled=""true""/>
							</WritingSystemOptions>
						</ConfigurationItem>
						<ConfigurationItem name=""Translations"" between=""--TESTING--"" isEnabled=""true"">
							<ConfigurationItem name=""Translation"" between=""--TESTING--"" isEnabled=""true"">
								<WritingSystemOptions writingSystemType=""analysis"" displayWSAbreviation=""false"">
									<Option id=""all analysis"" isEnabled=""true""/>
								</WritingSystemOptions>
							</ConfigurationItem>
						</ConfigurationItem>
					</ConfigurationItem>
				</ConfigurationItem>
			</ConfigurationItem>"
			+ DictionaryConfigurationModelTests.XmlCloseTagsFromRoot;

		// "Labels Removed" include those that may be re-added under Senses
		private static readonly string[] RcfLabelsRemovedFromBoth = { "Grammatical Info.", "Definition (or Gloss)", "Examples" };
		private static readonly string[] RcfLabelsRemovedFromOrcfs = { "Subentry Under Reference" };
		private static readonly string[] RcfLabelsRemovedFromSubentries = { "Complex Form", "Comment", "Example Sentences" };
		private static readonly string[] RcfLabelsMovedToSenses = { "Grammatical Info.", "Definition (or Gloss)", "Examples" };
		private const string FormattableSuffix = "/ConfigurationItem[@name='{0}']";
		private const string ChildOfEntryXpath = "/DictionaryConfiguration/ConfigurationItem" + FormattableSuffix;
		private const string GrandchildOfEntryXpath = "/DictionaryConfiguration/ConfigurationItem/ConfigurationItem" + FormattableSuffix;

		[Test]
		public void CopyNewDefaultsIntoConvertedModel_ReferencedComplexFormsSplitInRoot()
		{
			using (var convertedModelFile = new TempFile(new[] {ConvertedModelXml}))
			{
				var convertedModelPath = convertedModelFile.Path;
				var convertedModel = new DictionaryConfigurationModel(convertedModelPath, Cache);
				var baseModel = GetSparseRootForSplitRcfTests();

				// SUT
				m_migrator.CopyNewDefaultsIntoConvertedModel(convertedModel, baseModel);
				convertedModel.Save();
				AssertThatXmlIn.File(convertedModelPath).HasNoMatchForXpath(String.Format(ChildOfEntryXpath, "Referenced Complex Forms"), print:false);
				AssertThatXmlIn.File(convertedModelPath).HasNoMatchForXpath(String.Format(GrandchildOfEntryXpath, "Referenced Complex Forms"), print:false);
				AssertThatXmlIn.File(convertedModelPath).HasSpecifiedNumberOfMatchesForXpath(
					String.Format(ChildOfEntryXpath, "Other Referenced Complex Forms"), 1, false);
				AssertThatXmlIn.File(convertedModelPath).HasSpecifiedNumberOfMatchesForXpath( // Search for 3: 2 we split + 1 under minor->subentries
					String.Format(GrandchildOfEntryXpath, "Other Referenced Complex Forms"), 3, false);
				AssertThatXmlIn.File(convertedModelPath).HasSpecifiedNumberOfMatchesForXpath(
					String.Format(GrandchildOfEntryXpath, "Other Referenced Complex Forms' and @between='--TESTING--"), 2, false);
				AssertThatXmlIn.File(convertedModelPath).HasSpecifiedNumberOfMatchesForXpath(String.Format(ChildOfEntryXpath, "Subentries"), 1, false);
				AssertThatXmlIn.File(convertedModelPath).HasSpecifiedNumberOfMatchesForXpath(String.Format(GrandchildOfEntryXpath, "Subentries"), 2, false);
				AssertThatXmlIn.File(convertedModelPath).HasSpecifiedNumberOfMatchesForXpath(
					String.Format(GrandchildOfEntryXpath, "Subentries' and @between='--TESTING--"), 2, false);
				// REVIEW (Hasso) 2014.10: should these specific assertions be moved to new tests?
				foreach (var node in convertedModel.Parts)
					ActOnNodeAndDescendents(node, AssertNeitherCustomNorDuplicate);
				// TODO "missing" nodes under senses merged from model properly
				// TODO: original 'senses' untouched
				// TODO: "missing" nodes under Subentries and ORCF's merged properly
				// TODO: Reverse Abbreviation renamed to Abbreviation (redundant to "no custom" check?)
			}
		}

		[Test]
		public void CopyNewDefaultsIntoConvertedModel_ReferencedComplexFormsNotSplitInStem()
		{
			using (var convertedModelFile = new TempFile(new[] {ConvertedModelXml}))
			{
				var convertedModelPath = convertedModelFile.Path;
				var convertedModel = new DictionaryConfigurationModel(convertedModelPath, Cache);
				var baseModelPath = Path.Combine(DictionaryConfigurationListener.GetDefaultConfigurationDirectory(m_mediator),
					"Stem" + DictionaryConfigurationModel.FileExtension);
				var baseModel = new DictionaryConfigurationModel(baseModelPath, Cache);

				// SUT
				m_migrator.CopyNewDefaultsIntoConvertedModel(convertedModel, baseModel);
				convertedModel.Save();
				AssertThatXmlIn.File(convertedModelPath).HasSpecifiedNumberOfMatchesForXpath(
					String.Format(ChildOfEntryXpath, "Referenced Complex Forms' and @between='--TESTING--"), 1, false);
				AssertThatXmlIn.File(convertedModelPath).HasSpecifiedNumberOfMatchesForXpath(
					String.Format(GrandchildOfEntryXpath, "Referenced Complex Forms' and @between='--TESTING--"), 2, false);
				AssertThatXmlIn.File(convertedModelPath).HasNoMatchForXpath(String.Format(ChildOfEntryXpath, "Other Referenced Complex Forms"), print:false);
				AssertThatXmlIn.File(convertedModelPath).HasNoMatchForXpath(String.Format(GrandchildOfEntryXpath, "Other Referenced Complex Forms"), print:false);
				AssertThatXmlIn.File(convertedModelPath).HasNoMatchForXpath(String.Format(ChildOfEntryXpath, "Subentries"), print:false);
				AssertThatXmlIn.File(convertedModelPath).HasNoMatchForXpath(String.Format(GrandchildOfEntryXpath, "Subentries"), print:false);
			}
		}

		[Test]
		public void SplitReferencedComplexFormsIntoSubentriesAndOthers_SplitsSimpleNode()
		{
			var parentNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode>(new [] { new ConfigurableDictionaryNode
				{
					Label = "Referenced Complex Forms",
					After = "AfterText",
					IsDuplicate = false, // yes, this looks silly, but it makes for a rigorous test
					LabelSuffix = "Suffix",
					DictionaryNodeOptions = new DictionaryNodeComplexFormOptions(),
					Children = new List<ConfigurableDictionaryNode>()
				}})
			};
			parentNode.Children[0].Parent = parentNode;

			// SUT
			Assert.DoesNotThrow(() => m_migrator.SplitReferencedComplexFormsIntoSubentriesAndOthers(parentNode.Children[0], null));
			Assert.AreEqual(2, parentNode.Children.Count, "'Referenced Complex Forms' should have been split");
			Assert.AreEqual("Other Referenced Complex Forms", parentNode.Children[0].Label, "Should have Other RCF's");
			Assert.AreEqual("Subentries", parentNode.Children[1].Label, "Should have Subentries");
			Assert.IsNull(parentNode.Children[0].DictionaryNodeOptions, "Options should have been removed from Other RCF's");
			Assert.IsInstanceOf(typeof(DictionaryNodeComplexFormOptions), parentNode.Children[1].DictionaryNodeOptions,
				"Options should have been copied to Subentries");
			foreach (var node in parentNode.Children)
			{
				Assert.AreEqual("AfterText", node.After, "After text should have been copied to {0}", node.DisplayLabel);
				Assert.False(node.IsDuplicate, "The original node was not a duplicate; neither should {0} be", node.DisplayLabel);
				Assert.AreEqual("Suffix", node.LabelSuffix, "The Label Suffix should have been copied to {0}", node.DisplayLabel);
			}
		}

		[Test]
		public void SplitReferencedComplexFormsIntoSubentriesAndOthers_ProperlyRelocatesNodesUnderSenses()
		{
			using (var convertedModelFile = new TempFile(new[] {ConvertedModelXml}))
			{
				var convertedModelPath = convertedModelFile.Path;
				var convertedModel = new DictionaryConfigurationModel(convertedModelPath, Cache);
				var baseModel = GetSparseRootForSplitRcfTests();

				var minorEntryNode = convertedModel.Parts[1];
				var minorEntryRcfNode = minorEntryNode.Children.Find(node => node.Label.Equals("Referenced Complex Forms"));
				var baseMinorEntrySubentSenseNode =
					baseModel.Parts[1].Children.Find(node => node.Label.Equals("Subentries")).Children.Find(node => node.Label.Equals("Senses"));

				// SUT
				m_migrator.SplitReferencedComplexFormsIntoSubentriesAndOthers(minorEntryRcfNode, baseMinorEntrySubentSenseNode);
				ActOnNodeAndDescendents(minorEntryNode, AssertNeitherCustomNorDuplicate);
				// minorEntry -> assert that its only children are Senses, Subentries, Other RCF's
				Assert.AreEqual(3, minorEntryNode.Children.Count, "Minor Entry should have three children");
				Assert.AreEqual(1, minorEntryNode.Children.Count(node => node.Label.Equals("Senses")), "Senses node should not have vanished");
				Assert.AreEqual(1, minorEntryNode.Children.Count(node => node.Label.Equals("Other Referenced Complex Forms")),
					"Should have created Other Referenced Complex Forms node");
				Assert.AreEqual(1, minorEntryNode.Children.Count(node => node.Label.Equals("Subentries")), "Should have created Subentries node");

				minorEntryRcfNode = minorEntryNode.Children.Find(node => node.Label.Equals("Other Referenced Complex Forms"));
				var minorEntrySubentNode = minorEntryNode.Children.Find(node => node.Label.Equals("Subentries"));
				// MinorEntry->RCF's: Assert that its children don't include
				//						Grammatical Info., Definition (or Gloss), Examples, Example Sentences, Complex Form, or Comment
				foreach (var label in RcfLabelsRemovedFromBoth.Concat(RcfLabelsRemovedFromOrcfs))
					Assert.AreEqual(0, minorEntryRcfNode.Children.Count(node => node.Label.Equals(label)),
						String.Format("Node '{0}' should have been removed from Other Referenced Complex Forms's children", label));
				// Subentries: Assert that its children include Senses but not [re]moved nodes
				foreach (var label in RcfLabelsRemovedFromBoth.Concat(RcfLabelsRemovedFromSubentries))
					Assert.AreEqual(0, minorEntrySubentNode.Children.Count(node => node.Label.Equals(label)),
						String.Format("Node '{0}' should have been removed from Subentries's children", label));
				var subentriesSenses = minorEntrySubentNode.Children.FirstOrDefault(node => node.Label.Equals("Senses"));
				Assert.IsNotNull(subentriesSenses, "Senses should have been added as a child of Subentries");
				// Subentries->Senses: Assert that its children include Grammatical Info., Definition (or Gloss), and Examples, all w/ TESTING tag
				foreach (var label in RcfLabelsMovedToSenses)
				{
					var subentSensesChildrenWithLabel = subentriesSenses.Children.FindAll(node => node.Label.Equals(label));
					Assert.AreEqual(1, subentSensesChildrenWithLabel.Count,
						String.Format("Minor Entry->Subentries->Senses should have one child node named '{0}'", label));
					Assert.AreEqual("--TESTING--", subentSensesChildrenWithLabel[0].Between,
						String.Format("Node 'Minor Entry->Subentries->Senses->{0}' should originate from the migrated model", label));
				}
			}
		}

		[Test]
		public void SplitReferencedComplexFormsIntoSubentriesAndOthers_DuplicateRcfsWorks()
		{
			using (var convertedModelFile = new TempFile(new[] { ConvertedModelXml }))
			{
				var convertedModelPath = convertedModelFile.Path;
				var convertedModel = new DictionaryConfigurationModel(convertedModelPath, Cache);
				var baseModel = GetSparseRootForSplitRcfTests();

				var minorEntryNode = convertedModel.Parts[1];
				var minorEntryRcfNode1 = minorEntryNode.Children.Find(node => node.Label.Equals("Referenced Complex Forms"));
				var minorEntryRcfNode2 = minorEntryRcfNode1.DuplicateAmongSiblings();
				minorEntryRcfNode2.LabelSuffix = "CustomSuffix";
				ActOnNodeAndDescendents(minorEntryRcfNode1, node => node.Before = "One");
				ActOnNodeAndDescendents(minorEntryRcfNode2, node => node.After = "Two");

				// SUT
				m_migrator.CopyNewDefaultsIntoConvertedModel(convertedModel, baseModel);

				Assert.AreEqual(2, minorEntryNode.Children.Count(node => node.Label.Equals("Other Referenced Complex Forms")),
					"There should be precisely two 'Other Referenced Complex Forms' nodes; one for each original 'Referenced Complex Forms'");
				Assert.AreEqual(2, minorEntryNode.Children.Count(node => node.Label.Equals("Subentries")),
					"There should be precisely two 'Subentries' nodes; one for each original 'Referenced Complex Forms'");
				var orcf1 = minorEntryNode.Children.FirstOrDefault(node => node.Label.Equals("Other Referenced Complex Forms") && !node.IsDuplicate);
				var orcf2 = minorEntryNode.Children.FirstOrDefault(node => node.Label.Equals("Other Referenced Complex Forms") && node.IsDuplicate);
				var subentries1 = minorEntryNode.Children.FirstOrDefault(node => node.Label.Equals("Subentries") && !node.IsDuplicate);
				var subentries2 = minorEntryNode.Children.FirstOrDefault(node => node.Label.Equals("Subentries") && node.IsDuplicate);
				Assert.NotNull(orcf1, "One 'Other Referenced Complex Forms' node should be marked as original");
				Assert.NotNull(orcf2, "One 'Other Referenced Complex Forms' node should be marked as a duplicate");
				Assert.NotNull(subentries1, "One 'Subentries' node should be marked as original");
				Assert.NotNull(subentries2, "One 'Subentries' node should be marked as a duplicate");
				Assert.IsNullOrEmpty(orcf1.LabelSuffix, "The Original 'Other Referenced Complex Forms' node should not have a suffix");
				Assert.AreEqual("CustomSuffix", orcf2.LabelSuffix, "The Duplicate 'Other Referenced Complex Forms' node should have a LabelSuffix");
				Assert.IsNullOrEmpty(subentries1.LabelSuffix, "The Original 'Subentries' node should not have a suffix");
				Assert.AreEqual("CustomSuffix", subentries2.LabelSuffix, "The Duplicate 'Subentries' node should have a LabelSuffix");

				foreach (var child in new[] { orcf1, orcf2, subentries1, subentries2 }.SelectMany(node => node.Children))
				{
					AssertNeitherCustomNorDuplicate(child);
				}

				ActOnNodeAndDescendents(subentries1, node => Assert.AreNotEqual("Two", node.After,
					"The original Subentries node should not have any of the duplicate's children: "
					+ DictionaryConfigurationMigrator.BuildPathStringFromNode(node)));
				ActOnNodeAndDescendents(subentries2, node => Assert.AreNotEqual("One", node.Before,
					"The duplicate Subentries node should not have any of the original's children: "
					+ DictionaryConfigurationMigrator.BuildPathStringFromNode(node)));

				var assertBeforeIsOne = new Action<ConfigurableDictionaryNode>(node => Assert.AreEqual("One", node.Before,
					String.Format("'{0}'.Before should be 'One'", DictionaryConfigurationMigrator.BuildPathStringFromNode(node))));
				var assertAfterIsTwo = new Action<ConfigurableDictionaryNode>(node => Assert.AreEqual("Two", node.After,
					String.Format("'{0}'.After should be 'Two'", DictionaryConfigurationMigrator.BuildPathStringFromNode(node))));

				assertBeforeIsOne(orcf1);
				assertAfterIsTwo(orcf2);
				assertBeforeIsOne(subentries1);
				assertAfterIsTwo(subentries2);

				Assert.AreEqual(1, subentries1.Children.Count(node => node.Label.Equals("Senses")),
					"The original Subentries node should have exactly one child Senses");
				Assert.AreEqual(1, subentries2.Children.Count(node => node.Label.Equals("Senses")),
					"The duplicate Subentries node should have exactly one child Senses");
				var subent1Senses = subentries1.Children.Find(node => node.Label.Equals("Senses"));
				var subent2Senses = subentries2.Children.Find(node => node.Label.Equals("Senses"));
				Assert.AreNotSame(subent1Senses, subent2Senses, "Each Subentries node should have its own child Senses");
				foreach (var movedNode in subent1Senses.Children.FindAll(node => RcfLabelsMovedToSenses.Contains(node.Label)))
					assertBeforeIsOne(movedNode);
				foreach (var movedNode in subent2Senses.Children.FindAll(node => RcfLabelsMovedToSenses.Contains(node.Label)))
					assertAfterIsTwo(movedNode);
			}
		}

		[Test]
		public void SplitReferencedComplexFormsIntoSubentriesAndOthers_DuplicateNodeMovedUnderSensesWorks()
		{
			using (var convertedModelFile = new TempFile(new[] { ConvertedModelXml }))
			{
				var convertedModelPath = convertedModelFile.Path;
				var convertedModel = new DictionaryConfigurationModel(convertedModelPath, Cache);
				var baseModel = GetSparseRootForSplitRcfTests();

				var minorEntryNode = convertedModel.Parts[1];
				var minorEntryRcfNode = minorEntryNode.Children.Find(node => node.Label.Equals("Referenced Complex Forms"));
				var examplesNode1 = minorEntryRcfNode.Children.Find(node => node.Label.Equals("Example Sentences"));
				var examplesNode2 = examplesNode1.DuplicateAmongSiblings();
				examplesNode2.LabelSuffix = "CustomSuffix";
				var baseMinorEntrySubentSenseNode =
					baseModel.Parts[1].Children.Find(node => node.Label.Equals("Subentries")).Children.Find(node => node.Label.Equals("Senses"));

				// SUT
				m_migrator.SplitReferencedComplexFormsIntoSubentriesAndOthers(minorEntryRcfNode, baseMinorEntrySubentSenseNode);

				minorEntryRcfNode = minorEntryNode.Children.Find(node => node.Label.Equals("Other Referenced Complex Forms"));
				var subentriesNode = minorEntryNode.Children.Find(node => node.Label.Equals("Subentries"));

				Assert.False(minorEntryRcfNode.Children.Any(node => node.Label.Equals("Examples")),
					"Examples belong only under Senses. Example Sentences should not have been renamed here.");
				Assert.False(subentriesNode.Children.Any(node => new[] {"Examples", "Example Sentences"}.Contains(node.Label)),
					"Example[ Sentence]s of Subentries belong under their Senses.");

				var subentriesSensesNodes = subentriesNode.Children.FindAll(node => node.Label.Equals("Senses"));
				Assert.AreEqual(1, subentriesSensesNodes.Count, "Senses should not have been duplicated");
				var examplesNodes = subentriesSensesNodes[0].Children.FindAll(node => new[] {"Examples", "Example Sentences"}.Contains(node.Label));
				Assert.AreEqual(2, examplesNodes.Count, "There should be 2 Examples: the two from the converted model");
				Assert.AreEqual(2, examplesNodes.Count(node => node.Label.Equals("Examples")), "Both Examples nodes should have been renamed");
				Assert.AreEqual(2, examplesNodes.Count(node => node.Between.Equals("--TESTING--")),
					"Both Examples nodes should be from the converted model");
				Assert.AreEqual(1, examplesNodes.Count(node => !node.IsDuplicate && node.LabelSuffix == null),
					"One of the Examples nodes should be the original");
				Assert.AreEqual(1, examplesNodes.Count(node => node.IsDuplicate && node.LabelSuffix.Equals("CustomSuffix")),
					"One of the Examples nodes should be the duplicate");
			}
		}

		/// <summary>load Root and clean up for quicker testing of SplitReferencedComplexFormsIntoSubentriesAndOthers</summary>
		private DictionaryConfigurationModel GetSparseRootForSplitRcfTests()
		{
			var rootPath = Path.Combine(DictionaryConfigurationListener.GetDefaultConfigurationDirectory(m_mediator),
				"Root" + DictionaryConfigurationModel.FileExtension);
			var root = new DictionaryConfigurationModel(rootPath, Cache);
			root.Parts[0].Children.RemoveAll(node => !node.Label.Equals("Senses"));
			root.Parts[1].Children.RemoveAll(node => !(new[] { "Senses", "Other Referenced Complex Forms", "Subentries" }.Contains(node.Label)));
			return root;
		}

		private static void ActOnNodeAndDescendents(ConfigurableDictionaryNode node, Action<ConfigurableDictionaryNode> action)
		{
			action(node);
			if (node.Children != null)
				foreach (var child in node.Children)
					ActOnNodeAndDescendents(child, action);
		}

		private static void AssertNeitherCustomNorDuplicate(ConfigurableDictionaryNode node)
		{
			var nodePath = DictionaryConfigurationMigrator.BuildPathStringFromNode(node); // TODO pH 2014.10: time test vs. simple DisplayLabel
			Assert.False(node.IsCustomField, String.Format("Node '{0}' should not be custom", nodePath));
			Assert.False(node.IsDuplicate,  String.Format("Node '{0}' should not be a duplicate", nodePath));
			Assert.IsNullOrEmpty(node.LabelSuffix,  String.Format("Non-duplicate node '{0}' should not have a suffix", nodePath));
		}
		#endregion Split Referenced Complex Forms

		///<summary/>
		[Test]
		public void DictionaryConfigsNeedMigrating_ReturnsFalseIfNewConfigsExist()
		{
			var newDictConfigLoc = Path.Combine(FdoFileHelper.GetConfigSettingsDir(Path.GetDirectoryName(Cache.ProjectId.Path)), "Dictionary");
			Directory.CreateDirectory(newDictConfigLoc);
			File.AppendAllText(Path.Combine(newDictConfigLoc, "SomeConfig" + DictionaryConfigurationModel.FileExtension), "Foo");
			Assert.That(!m_migrator.DictionaryConfigsNeedMigrating(), "If current configs exist no migration should be needed."); // SUT
			Palaso.IO.DirectoryUtilities.DeleteDirectoryRobust(newDictConfigLoc);
		}

		///<summary/>
		[Test]
		public void DictionaryConfigsNeedMigrating_ReturnsFalseIfNoNewConfigsAndNoOldConfigs()
		{
			var newDictConfigLoc = Path.Combine(FdoFileHelper.GetConfigSettingsDir(Path.GetDirectoryName(Cache.ProjectId.Path)), "Dictionary");
			Directory.CreateDirectory(newDictConfigLoc);
			Directory.EnumerateFiles(newDictConfigLoc).ForEach(File.Delete);
			Assert.That(!m_migrator.DictionaryConfigsNeedMigrating(), "With no new or old configs no migration should be needed."); // SUT
			Palaso.IO.DirectoryUtilities.DeleteDirectoryRobust(newDictConfigLoc);
		}

		///<summary/>
		[Test]
		public void DictionaryConfigsNeedMigrating_ReturnsTrueIfNoNewConfigsAndOneOldConfig()
		{
			var configSettingsDir = FdoFileHelper.GetConfigSettingsDir(Path.GetDirectoryName(Cache.ProjectId.Path));
			var newDictConfigLoc = Path.Combine(configSettingsDir, "Dictionary");
			Directory.CreateDirectory(newDictConfigLoc);
			Directory.EnumerateFiles(newDictConfigLoc).ForEach(File.Delete);
			var tempFwLayoutPath = Path.Combine(configSettingsDir, "SomeConfig.fwlayout");
			using(var tempFwLayout = Palaso.IO.TempFile.WithFilename(tempFwLayoutPath))
			{
				File.AppendAllText(tempFwLayoutPath, "LayoutFoo");
				Assert.That(m_migrator.DictionaryConfigsNeedMigrating(), "There is an old config, a migration is needed."); // SUT
			}
			Palaso.IO.DirectoryUtilities.DeleteDirectoryRobust(newDictConfigLoc);
		}

		///<summary/>
		[Test]
		public void ReversalConfigsNeedMigrating_ReturnsTrueIfAny()
		{
			var newReversalConfigLoc = Path.Combine(FdoFileHelper.GetConfigSettingsDir(Path.GetDirectoryName(Cache.ProjectId.Path)), "Reversals");
			Directory.CreateDirectory(newReversalConfigLoc);
			File.AppendAllText(Path.Combine(newReversalConfigLoc, "SomeConfig" + DictionaryConfigurationModel.FileExtension), "Bar");
			Assert.That(!m_migrator.ReversalConfigsNeedMigrating()); // SUT
		}

		///<summary/>
		[Test]
		public void ReversalConfigsNeedMigrating_ReturnsFalseIfNone()
		{
			var newReversalConfigLoc = Path.Combine(FdoFileHelper.GetConfigSettingsDir(Path.GetDirectoryName(Cache.ProjectId.Path)), "Reversals");
			Directory.CreateDirectory(newReversalConfigLoc);
			Directory.EnumerateFiles(newReversalConfigLoc).ForEach(File.Delete);
			Assert.That(!m_migrator.ReversalConfigsNeedMigrating(), "Horray, you've implemented the method!"); // SUT
		}

		private void DeleteStyleSheet(string styleName)
		{
			var style = m_styleSheet.FindStyle(styleName);
			if(style != null)
			{
				m_styleSheet.Delete(style.Hvo);
			}
		}
	}
}

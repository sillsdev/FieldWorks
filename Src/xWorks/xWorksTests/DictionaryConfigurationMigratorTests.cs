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
		private DictionaryConfigurationMigrator m_migrator;
		private Mediator m_mediator;
		private FwXApp m_application;
		private string m_configFilePath;
		private MockFwXWindow m_window;
		private FwStyleSheet m_styleSheet;
		private StyleInfoTable m_owningTable;

		// Set up Custom Fields at the Fixture level, since disposing one in one test disposes them all in all tests
		private const string CustomFieldChangedLabel = "Custom Label";
		private const string CustomFieldOriginalName = "Custom Name";
		private const string CustomFieldUnchangedNameAndLabel = "Custom";
		private IDisposable m_cf1, m_cf2;

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

			m_cf1 = new CustomFieldForTest(Cache, CustomFieldChangedLabel, CustomFieldOriginalName, Cache.MetaDataCacheAccessor.GetClassId("LexEntry"),
				CellarPropertyType.ReferenceCollection, Guid.Empty);
			m_cf2 = new CustomFieldForTest(Cache, CustomFieldUnchangedNameAndLabel, Cache.MetaDataCacheAccessor.GetClassId("LexEntry"), 0,
					CellarPropertyType.ReferenceCollection, Guid.Empty);

		}

		[TestFixtureTearDown]
		protected void TearDown()
		{
			m_cf1.Dispose();
			m_cf2.Dispose();
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

		///<summary>Subentries node should have "Display .. in a Paragraph" checked (LT-15834).</summary>
		[Test]
		public void ConvertLayoutTreeNodeToConfigNode_DisplaySubentriesInParagraph()
		{
			const string disabledGuid = "-a0000000-1000-b000-2000-c00000000000";
			var node = new MockLayoutTreeNode
			{
				m_partName = "LexEntry-Jt-RootSubentriesConfig",
				EntryType = "complex",
				EntryTypeList = ItemTypeInfo.CreateListFromStorageString(disabledGuid)
			};

			var configNode = m_migrator.ConvertLayoutTreeNodeToConfigNode(node);
			Assert.NotNull(configNode.DictionaryNodeOptions, "No DictionaryNodeOptions were created");

			Assert.IsTrue(configNode.DictionaryNodeOptions is DictionaryNodeComplexFormOptions, "wrong type");
			var options = configNode.DictionaryNodeOptions as DictionaryNodeComplexFormOptions;
			Assert.IsTrue(options.DisplayEachComplexFormInAParagraph, "Did not set");
		}

		///<summary>Root-based Minor Entry - Components should use character styles. See LT-15834.</summary>
		[Test]
		public void ConvertLayoutTreeNodeToConfigNode_ComponentUsesCharStyles()
		{
			var node = new MockLayoutTreeNode { m_partName = "LexEntry-Jt-StemMinorComponentsConfig" };

			// SUT
			var configNode = m_migrator.ConvertLayoutTreeNodeToConfigNode(node);
			Assert.That(configNode.StyleType, Is.EqualTo(ConfigurableDictionaryNode.StyleTypes.Character), "Need to use character styles for Component");
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

		/// <summary>
		/// In some cases (Minor Entry, Subsubentry), a descendent of a duplicate will be flagged as a duplicate and given a DupString. Fortunately,
		/// these differ in that the DupString is not included in the Label. In this case, misleading Duplicate info should be discarded.
		/// If a node has both false and true duplicate information, the true information comes last, in the form "1.0-1"; this is handled correctly
		/// and is tested by <see cref="ConvertLayoutTreeNodeToConfigNode_DupStringInfoIsConvertedForDuplicateOfDuplicate"/>.
		/// </summary>
		[Test]
		public void ConvertLayoutTreeNodeToConfigNode_DupStringInfoIsDiscardedForFalseDuplicate()
		{
			var duplicateNode = new XmlDocConfigureDlg.LayoutTreeNode { DupString = "1.0", IsDuplicate = true, Label = "A b c D e f" };
			// SUT
			var configNode = m_migrator.ConvertLayoutTreeNodeToConfigNode(duplicateNode);
			Assert.IsFalse(configNode.IsDuplicate, "Node incorrectly marked as a duplicate.");
			Assert.IsNullOrEmpty(configNode.LabelSuffix, "suffix incorrectly migrated");
			Assert.AreEqual("A b c D e f", configNode.Label, "should not have a suffix on ConfigurableDictionaryNode.Label");
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
			var customNode = new ConfigurableDictionaryNode { Label = CustomFieldUnchangedNameAndLabel };
			var oldChild = new ConfigurableDictionaryNode { Label = "Child" };
			convertedParentNode.Children = new List<ConfigurableDictionaryNode> { customNode, oldChild };
			var convertedModel = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { convertedParentNode } };
			var baseParentNode = new ConfigurableDictionaryNode { Label = "Parent", FieldDescription = "LexEntry" };
			var baseChildNode = new ConfigurableDictionaryNode { Label = "Child" };
			baseParentNode.Children = new List<ConfigurableDictionaryNode> { baseChildNode };
			var baseModel = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { baseParentNode } };

			m_migrator.CopyNewDefaultsIntoConvertedModel(convertedModel, baseModel);
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
			var customNode = new ConfigurableDictionaryNode { Label = CustomFieldUnchangedNameAndLabel };
			var oldChild = new ConfigurableDictionaryNode { Label = "Child" };
			convertedParentNode.Children = new List<ConfigurableDictionaryNode> { customNode, oldChild };
			var customChild = new ConfigurableDictionaryNode { Label = "Custom Child" };
			customNode.Children = new List<ConfigurableDictionaryNode> { customChild };
			var convertedModel = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { convertedParentNode } };
			var baseParentNode = new ConfigurableDictionaryNode { Label = "Parent", FieldDescription = "LexEntry" };
			var baseChildNode = new ConfigurableDictionaryNode { Label = "Child" };
			baseParentNode.Children = new List<ConfigurableDictionaryNode> { baseChildNode };
			var baseModel = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { baseParentNode } };

			m_migrator.CopyNewDefaultsIntoConvertedModel(convertedModel, baseModel);
			Assert.AreEqual(convertedModel.Parts[0].Children.Count, 2, "Nodes incorrectly merged");
			Assert.AreEqual(convertedModel.Parts[0].Children[0].Label, customNode.Label, "order of old model was not retained");
			Assert.IsFalse(oldChild.IsCustomField, "Child node which is matched should not be a custom field");
			Assert.IsTrue(customNode.IsCustomField, "The unmatched 'Custom' node should have been marked as a custom field");
			Assert.IsTrue(customChild.IsCustomField, "Children of Custom nodes should also be Custom.");
			Assert.AreEqual(customNode.Label, customNode.FieldDescription, "Custom nodes' Labels and Fields should match");
			Assert.AreEqual(customChild.Label, customChild.FieldDescription, "Custom nodes' Labels and Fields should match");
		}

		///<summary/>
		[Test]
		public void CopyNewDefaultsIntoConvertedModel_RelabeledCustomFieldsNamesAreMigrated()
		{
			var customNode = new ConfigurableDictionaryNode { Label = CustomFieldChangedLabel };
			var oldChild = new ConfigurableDictionaryNode { Label = "Child" };
			var convertedParentNode = new ConfigurableDictionaryNode
			{
				Label = "Parent", Children = new List<ConfigurableDictionaryNode> { customNode, oldChild }
			};
			var convertedModel = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { convertedParentNode } };
			var baseChildNode = new ConfigurableDictionaryNode { Label = "Child" };
			var baseParentNode = new ConfigurableDictionaryNode
			{
				Label = "Parent", FieldDescription = "LexEntry", Children = new List<ConfigurableDictionaryNode> { baseChildNode }
			};
			var baseModel = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { baseParentNode } };

			m_migrator.CopyNewDefaultsIntoConvertedModel(convertedModel, baseModel);
			Assert.AreEqual(convertedModel.Parts[0].Children[0].Label, customNode.Label, "label was not retained");
			Assert.IsTrue(customNode.IsCustomField, "The unmatched 'Custom' node should have been marked as a custom field");
			Assert.AreEqual(CustomFieldOriginalName, customNode.FieldDescription, "Custom node's Field should have been loaded from the Cache");
		}

		///<summary>
		/// If a standard node in the Dictionary Configuration has a custom child,
		/// but that node doesn't have any Custom Fields in the lexical database, do *not* throw.
		/// </summary>
		[Test]
		public void CopyNewDefaultsIntoConvertedModel_CustomFieldInStrangePlaceDoesNotThrow()
		{
			var convertedParentNode = new ConfigurableDictionaryNode { Label = "Parent" };
			var customNode = new ConfigurableDictionaryNode { Label = "Truly Custom" };
			var oldChild = new ConfigurableDictionaryNode { Label = "Child" };
			convertedParentNode.Children = new List<ConfigurableDictionaryNode> { customNode, oldChild };
			var convertedModel = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { convertedParentNode } };
			var baseParentNode = new ConfigurableDictionaryNode { Label = "Parent", FieldDescription = "LexReference" };
			var baseChildNode = new ConfigurableDictionaryNode { Label = "Child" };
			baseParentNode.Children = new List<ConfigurableDictionaryNode> { baseChildNode };
			var baseModel = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { baseParentNode } };

			Assert.DoesNotThrow(() => m_migrator.CopyNewDefaultsIntoConvertedModel(convertedModel, baseModel)); // TODO pH 2014.12: check that an error was reported
			Assert.AreEqual(convertedModel.Parts[0].Children.Count, 2, "Nodes incorrectly merged");
			Assert.AreEqual(convertedModel.Parts[0].Children[0].Label, customNode.Label, "order of old model was not retained");
			Assert.IsFalse(oldChild.IsCustomField, "Child node which is matched should not be a custom field");
			Assert.IsTrue(customNode.IsCustomField, "The unmatched 'Custom' node should have been marked as a custom field");
			Assert.AreEqual(customNode.Label, customNode.FieldDescription, "Custom nodes' Labels and Fields should match");
		}

		#region Minor Entry Componenents Referenced Entries Tests
		private const string HwBefore = "H.before";
		private const string GlsBefore = "G.before";
		private const string HwAfter = "H.after";
		private const string GlsAfter = "G.after";
		private const string HwBetween = "H.between";
		private const string GlsBetween = "G.between";

		private DictionaryConfigurationModel BuildConvertedReferenceEntryNodes(bool enableHeadword,
			bool enableSummaryDef, bool enableSenseHeadWord, bool enableGloss)
		{
			var headWord = new ConfigurableDictionaryNode { Label = "Referenced Headword", IsEnabled = enableHeadword, Before = HwBefore};
			var summaryDef = new ConfigurableDictionaryNode { Label = "Summary Definition", IsEnabled = enableSummaryDef, Before = GlsBefore};
			var senseHeadWord = new ConfigurableDictionaryNode { Label = "Referenced Sense Headword", IsEnabled = enableSenseHeadWord, Between = HwBetween, After = HwAfter };
			var gloss = new ConfigurableDictionaryNode { Label = "Gloss", IsEnabled = enableGloss, Between = GlsBetween, After = GlsAfter};
			var referencedEntriesNode = new ConfigurableDictionaryNode
			{
				Label = "Referenced Entries",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { headWord, summaryDef, senseHeadWord, gloss }
			};
			var componentsNode = new ConfigurableDictionaryNode
			{
				Label = "Components",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { referencedEntriesNode }
			};
			var minorEntryNode = new ConfigurableDictionaryNode
			{
				Label = "Minor Entry",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { componentsNode }
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> {minorEntryNode});

			return new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { minorEntryNode }, Version = -1 };
		}

		private DictionaryConfigurationModel BuildCurrentDefaultReferenceEntryNodes(bool enableHeadWord,
			bool enableGloss)
		{
			var headWord = new ConfigurableDictionaryNode { Label = "Referenced Headword", IsEnabled = enableHeadWord, FieldDescription = "HeadWord"};
			var gloss = new ConfigurableDictionaryNode { Label = "Gloss (or Summary Definition)", IsEnabled = enableGloss, FieldDescription = "DefinitionOrGloss"};
			var referencedEntriesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ConfigReferencedEntries",
				Label = "Referenced Entries",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { headWord, gloss }
			};
			var componentsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ComplexFormEntryRefs",
				Label = "Components",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { referencedEntriesNode }
			};
			var minorEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Label = "Minor Entry",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { componentsNode }
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { minorEntryNode });

			return new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { minorEntryNode } };
		}

		///<summary/>
		[Test]
		public void CopyNewDefaultsIntoConvertedModel_MinorEntryComponentsReferencedEntriesChanged()
		{
			const string refEntriesPath = "//ConfigurationItem[@name='Minor Entry']/ConfigurationItem[@name='Components']/ConfigurationItem[@name='Referenced Entries']/";
			using(var convertedModelFile = new TempFile())
			{
				var convertedMinorEntry = BuildConvertedReferenceEntryNodes(true, true, true, true);
				convertedMinorEntry.FilePath = convertedModelFile.Path;
				var defaultMinorEntry = BuildCurrentDefaultReferenceEntryNodes(true, true);

				m_migrator.CopyNewDefaultsIntoConvertedModel(convertedMinorEntry, defaultMinorEntry);
				convertedMinorEntry.Save();
				AssertThatXmlIn.File(convertedModelFile.Path).HasNoMatchForXpath(refEntriesPath + "ConfigurationItem[@name='Referenced Sense Headword']");
				AssertThatXmlIn.File(convertedModelFile.Path).HasNoMatchForXpath(refEntriesPath + "ConfigurationItem[@name='Gloss']");
				AssertThatXmlIn.File(convertedModelFile.Path).HasNoMatchForXpath(refEntriesPath + "ConfigurationItem[@name='Summary Definition']");
				AssertThatXmlIn.File(convertedModelFile.Path).HasSpecifiedNumberOfMatchesForXpath(refEntriesPath + "ConfigurationItem[@name='Referenced Headword']", 1);
				AssertThatXmlIn.File(convertedModelFile.Path).HasSpecifiedNumberOfMatchesForXpath(refEntriesPath + "ConfigurationItem[@name='Gloss (or Summary Definition)']", 1);
			}
		}

		/// <summary>
		/// In most cases, the 'Minor Entry->Components->Referenced Entries' node has a child 'Referenced Sense Headword' that needs to be merged
		/// with 'Referenced Headword'; however, in one case, that child's name is 'Referenced Sense'. This tests that special case.
		/// </summary>
		[Test]
		public void CopyNewDefaultsIntoConvertedModel_MinorEntryComponentsReferencedEntriesChangedInSpecialCase()
		{
			const string refEntriesPath = "//ConfigurationItem[@name='Minor Entry']/ConfigurationItem[@name='Components']/ConfigurationItem[@name='Referenced Entries']/";
			using(var convertedModelFile = new TempFile())
			{
				var convertedMinorEntry = BuildConvertedReferenceEntryNodes(true, true, true, true);
				convertedMinorEntry.Parts[0].Children[0].Children[0].Children.First(child => child.Label == "Referenced Sense Headword").Label
																											= "Referenced Sense";
				convertedMinorEntry.FilePath = convertedModelFile.Path;
				var defaultMinorEntry = BuildCurrentDefaultReferenceEntryNodes(true, true);

				m_migrator.CopyNewDefaultsIntoConvertedModel(convertedMinorEntry, defaultMinorEntry);
				convertedMinorEntry.Save();
				AssertThatXmlIn.File(convertedModelFile.Path).HasNoMatchForXpath(refEntriesPath + "ConfigurationItem[@name='Referenced Sense']");
				AssertThatXmlIn.File(convertedModelFile.Path).HasNoMatchForXpath(refEntriesPath + "ConfigurationItem[@name='Referenced Sense Headword']");
				AssertThatXmlIn.File(convertedModelFile.Path).HasSpecifiedNumberOfMatchesForXpath(refEntriesPath + "ConfigurationItem[@name='Referenced Headword']", 1);
			}
		}

		///<summary/>
		[Test]
		public void CopyNewDefaultsIntoConvertedModel_MinorEntryComponentsBeforeAfterBetweenMigrated()
		{
			using(var convertedModelFile = new TempFile())
			{
				var convertedMinorEntry = BuildConvertedReferenceEntryNodes(true, true, true, true);
				convertedMinorEntry.FilePath = convertedModelFile.Path;
				var defaultMinorEntry = BuildCurrentDefaultReferenceEntryNodes(true, true);

				m_migrator.CopyNewDefaultsIntoConvertedModel(convertedMinorEntry, defaultMinorEntry);
				string cssResults = null;
				Assert.DoesNotThrow(()=>cssResults = CssGenerator.GenerateCssFromConfiguration(convertedMinorEntry, m_mediator));
				Assert.That(cssResults, Is.StringContaining(HwBefore));
				Assert.That(cssResults, Is.StringContaining(HwBetween));
				Assert.That(cssResults, Is.StringContaining(HwAfter));
				Assert.That(cssResults, Is.StringContaining(GlsBefore));
				Assert.That(cssResults, Is.StringContaining(GlsBetween));
				Assert.That(cssResults, Is.StringContaining(GlsAfter));
			}
		}

		///<summary/>
		[Test]
		public void CopyNewDefaultsIntoConvertedModel_MinorEntryComponentsHeadwordChecksMigrated()
		{
			const string refEntriesPath = "//ConfigurationItem[@name='Minor Entry']/ConfigurationItem[@name='Components']/ConfigurationItem[@name='Referenced Entries']/";
			using(var convertedModelFile = new TempFile())
			{
				var convertedMinorEntry = BuildConvertedReferenceEntryNodes(true, false, false, false);
				convertedMinorEntry.FilePath = convertedModelFile.Path;
				var defaultMinorEntry = BuildCurrentDefaultReferenceEntryNodes(false, false);
				m_migrator.CopyNewDefaultsIntoConvertedModel(convertedMinorEntry, defaultMinorEntry);
				convertedMinorEntry.Save();
				AssertThatXmlIn.File(convertedModelFile.Path).HasSpecifiedNumberOfMatchesForXpath(refEntriesPath + "ConfigurationItem[@name='Referenced Headword' and @isEnabled='true']", 1);

				convertedMinorEntry = BuildConvertedReferenceEntryNodes(false, false, false, false);
				convertedMinorEntry.FilePath = convertedModelFile.Path;
				defaultMinorEntry = BuildCurrentDefaultReferenceEntryNodes(true, false);
				m_migrator.CopyNewDefaultsIntoConvertedModel(convertedMinorEntry, defaultMinorEntry);
				convertedMinorEntry.Save();
				AssertThatXmlIn.File(convertedModelFile.Path).HasSpecifiedNumberOfMatchesForXpath(refEntriesPath + "ConfigurationItem[@name='Referenced Headword' and @isEnabled='false']", 1);

				convertedMinorEntry = BuildConvertedReferenceEntryNodes(false, false, true, false);
				convertedMinorEntry.FilePath = convertedModelFile.Path;
				defaultMinorEntry = BuildCurrentDefaultReferenceEntryNodes(false, false);
				m_migrator.CopyNewDefaultsIntoConvertedModel(convertedMinorEntry, defaultMinorEntry);
				convertedMinorEntry.Save();
				AssertThatXmlIn.File(convertedModelFile.Path).HasSpecifiedNumberOfMatchesForXpath(refEntriesPath + "ConfigurationItem[@name='Referenced Headword' and @isEnabled='true']", 1);
			}
		}

		///<summary/>
		[Test]
		public void CopyNewDefaultsIntoConvertedModel_MinorEntryComponentsGlossChecksMigrated()
		{
			const string refEntriesPath = "//ConfigurationItem[@name='Minor Entry']/ConfigurationItem[@name='Components']/ConfigurationItem[@name='Referenced Entries']/";
			using(var convertedModelFile = new TempFile())
			{
				var convertedMinorEntry = BuildConvertedReferenceEntryNodes(false, true, false, false);
				convertedMinorEntry.FilePath = convertedModelFile.Path;
				var defaultMinorEntry = BuildCurrentDefaultReferenceEntryNodes(false, false);
				m_migrator.CopyNewDefaultsIntoConvertedModel(convertedMinorEntry, defaultMinorEntry);
				convertedMinorEntry.Save();
				AssertThatXmlIn.File(convertedModelFile.Path).HasSpecifiedNumberOfMatchesForXpath(refEntriesPath + "ConfigurationItem[@name='Gloss (or Summary Definition)' and @isEnabled='true']", 1);

				convertedMinorEntry = BuildConvertedReferenceEntryNodes(false, false, false, false);
				convertedMinorEntry.FilePath = convertedModelFile.Path;
				defaultMinorEntry = BuildCurrentDefaultReferenceEntryNodes(false, true);
				m_migrator.CopyNewDefaultsIntoConvertedModel(convertedMinorEntry, defaultMinorEntry);
				convertedMinorEntry.Save();
				AssertThatXmlIn.File(convertedModelFile.Path).HasSpecifiedNumberOfMatchesForXpath(refEntriesPath + "ConfigurationItem[@name='Gloss (or Summary Definition)' and @isEnabled='false']", 1);

				convertedMinorEntry = BuildConvertedReferenceEntryNodes(false, false, false, true);
				convertedMinorEntry.FilePath = convertedModelFile.Path;
				defaultMinorEntry = BuildCurrentDefaultReferenceEntryNodes(false, false);
				m_migrator.CopyNewDefaultsIntoConvertedModel(convertedMinorEntry, defaultMinorEntry);
				convertedMinorEntry.Save();
				AssertThatXmlIn.File(convertedModelFile.Path).HasSpecifiedNumberOfMatchesForXpath(refEntriesPath + "ConfigurationItem[@name='Gloss (or Summary Definition)' and @isEnabled='true']", 1);
			}
		}

		#endregion

		///<summary/>
		[Test]
		public void DictionaryConfigsNeedMigrating_ReturnsFalseIfNewConfigsExist()
		{
			var newDictConfigLoc = Path.Combine(FdoFileHelper.GetConfigSettingsDir(Path.GetDirectoryName(Cache.ProjectId.Path)), "Dictionary");
			Directory.CreateDirectory(newDictConfigLoc);
			File.AppendAllText(Path.Combine(newDictConfigLoc, "SomeConfig" + DictionaryConfigurationModel.FileExtension), "Foo");
			Assert.That(!m_migrator.DictionaryConfigsNeedMigrating(), "If current configs exist no migration should be needed."); // SUT
			DirectoryUtilities.DeleteDirectoryRobust(newDictConfigLoc);
		}

		///<summary/>
		[Test]
		public void DictionaryConfigsNeedMigrating_ReturnsFalseIfNoNewConfigsAndNoOldConfigs()
		{
			var newDictConfigLoc = Path.Combine(FdoFileHelper.GetConfigSettingsDir(Path.GetDirectoryName(Cache.ProjectId.Path)), "Dictionary");
			Directory.CreateDirectory(newDictConfigLoc);
			Directory.EnumerateFiles(newDictConfigLoc).ForEach(File.Delete);
			Assert.That(!m_migrator.DictionaryConfigsNeedMigrating(), "With no new or old configs no migration should be needed."); // SUT
			DirectoryUtilities.DeleteDirectoryRobust(newDictConfigLoc);
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
			using(TempFile.WithFilename(tempFwLayoutPath))
			{
				File.AppendAllText(tempFwLayoutPath, "LayoutFoo");
				Assert.That(m_migrator.DictionaryConfigsNeedMigrating(), "There is an old config, a migration is needed."); // SUT
			}
			DirectoryUtilities.DeleteDirectoryRobust(newDictConfigLoc);
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

		#region Helper
		private void DeleteStyleSheet(string styleName)
		{
			var style = m_styleSheet.FindStyle(styleName);
			if (style != null)
			{
				m_styleSheet.Delete(style.Hvo);
			}
		}

		public class MockLayoutTreeNode : XmlDocConfigureDlg.LayoutTreeNode
		{
			public string m_partName;

			public override string PartName
			{
				get { return m_partName; }
			}
		}
		#endregion Helper
	}
}

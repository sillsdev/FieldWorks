// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;
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
		public void ConvertLayoutTreeNodeToConfigNode_DupStringInfoIsConverted()
		{
			var duplicateNode = new XmlDocConfigureDlg.LayoutTreeNode { DupString = "1", IsDuplicate = true };
			ConfigurableDictionaryNode configNode = null;
			Assert.DoesNotThrow(() => configNode = m_migrator.ConvertLayoutTreeNodeToConfigNode(duplicateNode));
			Assert.IsTrue(configNode.IsDuplicate, "Duplicate node not marked as duplicate.");
			Assert.AreEqual(configNode.LabelSuffix, duplicateNode.DupString, "number appended to old duplicates not migrated to label suffix");

			var originalNode = new XmlDocConfigureDlg.LayoutTreeNode { IsDuplicate = false };
			Assert.DoesNotThrow(() => configNode = m_migrator.ConvertLayoutTreeNodeToConfigNode(originalNode));
			Assert.IsFalse(configNode.IsDuplicate, "node should not have been marked as a duplicate");
			Assert.IsTrue(String.IsNullOrEmpty(configNode.LabelSuffix), "suffix should be empty.");
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
		public void CopyNewDefaultsIntoConvertedModel_FieldFromMatchIsUsed()
		{
			var oldParentNode = new ConfigurableDictionaryNode { Label = "Parent" };
			var oldChildNode = new ConfigurableDictionaryNode { Label = "Child" };
			oldParentNode.Children = new List<ConfigurableDictionaryNode> { oldChildNode };
			var oldModel = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { oldParentNode } };
			const string parentField = "ParentDescription";
			var baseParentNode = new ConfigurableDictionaryNode { Label = "Parent", FieldDescription = parentField};
			const string childField = "ChildDescription";
			var baseChildNode = new ConfigurableDictionaryNode { Label = "Child", FieldDescription = childField };
			baseParentNode.Children = new List<ConfigurableDictionaryNode> { baseChildNode };
			var baseModel = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { baseParentNode } };

			Assert.DoesNotThrow(() => m_migrator.CopyNewDefaultsIntoConvertedModel(oldModel, baseModel));
			Assert.AreEqual(oldModel.Parts[0].FieldDescription, parentField, "Field description for parent node not migrated");
			Assert.AreEqual(oldModel.Parts[0].Children[0].FieldDescription, childField, "Field description for child not migrated");
		}

		///<summary/>
		[Test]
		public void CopyNewDefaultsIntoConvertedModel_CopyOfNodeGetsValueFromBase()
		{
			var oldParentNode = new ConfigurableDictionaryNode { Label = "Parent" };
			// make oldChildNode look like a copy of a Child node which is not represented in the test.
			var oldChildNode = new ConfigurableDictionaryNode { Label = "Child", LabelSuffix = "1"};
			oldParentNode.Children = new List<ConfigurableDictionaryNode> { oldChildNode };
			var oldModel = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { oldParentNode } };
			const string parentField = "ParentDescription";
			var baseParentNode = new ConfigurableDictionaryNode { Label = "Parent", FieldDescription = parentField };
			const string childField = "ChildDescription";
			var baseChildNode = new ConfigurableDictionaryNode { Label = "Child", FieldDescription = childField };
			baseParentNode.Children = new List<ConfigurableDictionaryNode> { baseChildNode };
			var baseModel = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { baseParentNode } };

			Assert.DoesNotThrow(() => m_migrator.CopyNewDefaultsIntoConvertedModel(oldModel, baseModel));
			Assert.AreEqual(oldModel.Parts[0].Children[0].FieldDescription, childField, "Field description for copy of child not migrated");
		}

		///<summary/>
		[Test]
		public void CopyNewDefaultsIntoConvertedModel_TwoCopiesBothGetValueFromBase()
		{
			var oldParentNode = new ConfigurableDictionaryNode { Label = "Parent" };
			var oldChildNode = new ConfigurableDictionaryNode { Label = "Child" };
			var oldChildNodeCopy1 = new ConfigurableDictionaryNode { Label = "Child", LabelSuffix = "1" };
			var oldChildNodeCopy2 = new ConfigurableDictionaryNode { Label = "Child", LabelSuffix = "2" };
			oldParentNode.Children = new List<ConfigurableDictionaryNode> { oldChildNode, oldChildNodeCopy1, oldChildNodeCopy2 };
			var oldModel = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { oldParentNode } };
			const string parentField = "ParentDescription";
			var baseParentNode = new ConfigurableDictionaryNode { Label = "Parent", FieldDescription = parentField };
			const string childField = "ChildDescription";
			var baseChildNode = new ConfigurableDictionaryNode { Label = "Child", FieldDescription = childField };
			baseParentNode.Children = new List<ConfigurableDictionaryNode> { baseChildNode };
			var baseModel = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { baseParentNode } };

			Assert.DoesNotThrow(() => m_migrator.CopyNewDefaultsIntoConvertedModel(oldModel, baseModel));
			Assert.AreEqual(oldModel.Parts[0].Children.Count, 3, "The copied children did not get migrated");
			Assert.AreEqual(oldModel.Parts[0].Children[0].FieldDescription, childField, "Field description for copy of child not migrated");
			Assert.AreEqual(oldModel.Parts[0].Children[1].FieldDescription, childField, "Field description for copy of child not migrated");
			Assert.AreEqual(oldModel.Parts[0].Children[2].FieldDescription, childField, "Field description for copy of child not migrated");
		}

		///<summary/>
		[Test]
		public void CopyNewDefaultsIntoConvertedModel_NewNodeFromBaseIsMerged()
		{
			var oldParentNode = new ConfigurableDictionaryNode { Label = "Parent" };
			var oldChildNode = new ConfigurableDictionaryNode { Label = "Child" };
			oldParentNode.Children = new List<ConfigurableDictionaryNode> { oldChildNode };
			var oldModel = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { oldParentNode } };
			var baseParentNode = new ConfigurableDictionaryNode { Label = "Parent" };
			var baseChildNode = new ConfigurableDictionaryNode { Label = "Child" };
			var baseChildNodeTwo = new ConfigurableDictionaryNode { Label = "Child2" };
			baseParentNode.Children = new List<ConfigurableDictionaryNode> { baseChildNode, baseChildNodeTwo };
			var baseModel = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { baseParentNode } };

			Assert.DoesNotThrow(() => m_migrator.CopyNewDefaultsIntoConvertedModel(oldModel, baseModel));
			Assert.AreEqual(oldModel.Parts[0].Children.Count, 2, "New node from base was not merged");
			Assert.AreEqual(oldModel.Parts[0].Children[0].Label, "Child", "new node inserted out of order");
			Assert.AreEqual(oldModel.Parts[0].Children[1].Label, "Child2", "New node from base was not merged properly");
		}

		///<summary/>
		[Test]
		public void CopyNewDefaultsIntoConvertedModel_OrderFromOldModelIsRetained()
		{
			var oldParentNode = new ConfigurableDictionaryNode { Label = "Parent" };
			var oldChildNode = new ConfigurableDictionaryNode { Label = "Child" };
			var oldChildNodeTwo = new ConfigurableDictionaryNode { Label = "Child2" };
			oldParentNode.Children = new List<ConfigurableDictionaryNode> { oldChildNodeTwo, oldChildNode };
			var oldModel = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { oldParentNode } };
			var baseParentNode = new ConfigurableDictionaryNode { Label = "Parent" };
			var baseChildNode = new ConfigurableDictionaryNode { Label = "Child" };
			var baseChildNodeTwo = new ConfigurableDictionaryNode { Label = "Child2" };
			baseParentNode.Children = new List<ConfigurableDictionaryNode> { baseChildNode, baseChildNodeTwo };
			var baseModel = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { baseParentNode } };

			Assert.DoesNotThrow(() => m_migrator.CopyNewDefaultsIntoConvertedModel(oldModel, baseModel));
			Assert.AreEqual(oldModel.Parts[0].Children.Count, 2, "Nodes incorrectly merged");
			Assert.AreEqual(oldModel.Parts[0].Children[0].Label, oldChildNodeTwo.Label, "order of old model was not retained");
			Assert.AreEqual(oldModel.Parts[0].Children[1].Label, oldChildNode.Label, "Nodes incorrectly merged");
		}

		///<summary/>
		[Test]
		public void CopyNewDefaultsIntoConvertedModel_UnmatchedNodeFromOldModelIsCustom()
		{
			var oldParentNode = new ConfigurableDictionaryNode { Label = "Parent" };
			var customNode = new ConfigurableDictionaryNode { Label = "Custom" };
			var oldChild = new ConfigurableDictionaryNode { Label = "Child" };
			oldParentNode.Children = new List<ConfigurableDictionaryNode> { customNode, oldChild };
			var oldModel = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { oldParentNode } };
			var baseParentNode = new ConfigurableDictionaryNode { Label = "Parent" };
			var baseChildNode = new ConfigurableDictionaryNode { Label = "Child" };
			baseParentNode.Children = new List<ConfigurableDictionaryNode> { baseChildNode };
			var baseModel = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { baseParentNode } };

			Assert.DoesNotThrow(() => m_migrator.CopyNewDefaultsIntoConvertedModel(oldModel, baseModel));
			Assert.AreEqual(oldModel.Parts[0].Children.Count, 2, "Nodes incorrectly merged");
			Assert.AreEqual(oldModel.Parts[0].Children[0].Label, customNode.Label, "order of old model was not retained");
			Assert.IsFalse(oldChild.IsCustomField, "Child node which is matched should not be a custom field");
			Assert.IsTrue(customNode.IsCustomField, "The unmatched 'Custom' node should have been marked as a custom field");
		}
	}
}

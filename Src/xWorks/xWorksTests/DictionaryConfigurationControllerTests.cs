// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.XWorks.DictionaryDetailsView;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	[TestFixture]
	class DictionaryConfigurationControllerTests : MemoryOnlyBackendProviderTestBase
	{
		private DictionaryConfigurationModel m_model;

		[SetUp]
		public void Setup()
		{
			m_model = new DictionaryConfigurationModel();
		}

		[TearDown]
		public void TearDown()
		{

		}

		/// <summary>
		/// This test verifies that PopulateTreeView builds a TreeView that has the same structure as the model it is based on
		/// </summary>
		[Test]
		public void PopulateTreeViewBuildsRightNumberOfNodes()
		{
			using (var testView = new TestConfigurableDictionaryView())
			{
				m_model.Parts = new List<ConfigurableDictionaryNode> { BuildTestPartTree(2, 5) };

				var dcc = new DictionaryConfigurationController {View = testView, _model = m_model};

				//SUT
				dcc.PopulateTreeView(null);

				ValidateTreeForm(2, 5, dcc.View.TreeControl.Tree);
			}
		}

		private void ValidateTreeForm(int levels, int nodeCount, TreeView treeView)
		{
			var validationCount = 0;
			var validationLevels = 0;
			CalculateTreeInfo(ref validationLevels, ref validationCount, treeView.Nodes);
			Assert.AreEqual(levels, validationLevels, "Tree hierarchy incorrect");
			Assert.AreEqual(nodeCount, validationCount, "Tree node count incorrect");
		}

		private void CalculateTreeInfo(ref int levels, ref int count, TreeNodeCollection nodes)
		{
			if(nodes == null || nodes.Count < 1)
			{
				return;
			}
			++levels;
			foreach(TreeNode node in nodes)
			{
				++count;
				if(node.Nodes.Count > 0)
				{
					CalculateTreeInfo(ref levels, ref count, node.Nodes);
				}
			}
		}

		/// <summary>
		/// Builds a test tree of ConfigurableDictionary nodes with the given numbers of levels and nodes
		/// the structure of the tree is TODO: say what we did once this test code stabalizes
		/// </summary>
		/// <param name="numberOfLevels"></param>
		/// <param name="numberOfNodes"></param>
		/// <returns></returns>
		private ConfigurableDictionaryNode BuildTestPartTree(int numberOfLevels, int numberOfNodes)
		{
			if(numberOfLevels < 1)
			{
				throw new ArgumentException("You wanted less than one level in the hierarchy.  Really?");
			}

			if(numberOfNodes < numberOfLevels)
			{
				throw new ArgumentException("You asked for more levels in the hierarchy then nodes in the tree; how did you expect me to do that?");
			}
			ConfigurableDictionaryNode rootNode = null;
			ConfigurableDictionaryNode workingNode = null;
			var children = new List<ConfigurableDictionaryNode>();
			for(var i = 0; i < numberOfLevels; ++i)
			{
				if(workingNode == null)
				{
					workingNode = rootNode = new ConfigurableDictionaryNode { Label = "root" };
					continue;
				}
				children = new List<ConfigurableDictionaryNode>();
				workingNode.Children = children;
				workingNode = new ConfigurableDictionaryNode { Label = "level" + i };
				children.Add(workingNode);
			}
			// Add remaining desired nodes at the bottom
			for(var i = 0; i < numberOfNodes - numberOfLevels; ++i)
			{
				children.Add(new ConfigurableDictionaryNode { Label = "extraChild" + i });
			}
			return rootNode;
		}

		/// <summary/>
		[Test]
		public void FindTreeNode_ThrowsOnNullArgument()
		{
			var node = new ConfigurableDictionaryNode();
			using (var treeView = new TreeView())
			{
				var collection = treeView.Nodes;
				// SUT
				Assert.Throws<ArgumentNullException>(() => DictionaryConfigurationController.FindTreeNode(null, collection));
				Assert.Throws<ArgumentNullException>(() => DictionaryConfigurationController.FindTreeNode(node, null));
				Assert.Throws<ArgumentNullException>(() => DictionaryConfigurationController.FindTreeNode(null, null));
			}
		}

		/// <summary/>
		[Test]
		public void FindTreeNode_CanFindRoot()
		{
			var node = new ConfigurableDictionaryNode();
			using (var treeView = new TreeView())
			{
				var treeNode = new TreeNode();
				treeNode.Tag = node;
				treeView.Nodes.Add(treeNode);
				treeView.TopNode = treeNode;
				// SUT
				var returnedTreeNode = DictionaryConfigurationController.FindTreeNode(node, treeView.Nodes);
				Assert.That(returnedTreeNode.Tag, Is.EqualTo(node));
			}
		}

		/// <summary/>
		[Test]
		public void FindTreeNode_CanFindChild()
		{
			var node = new ConfigurableDictionaryNode();
			using (var treeView = new TreeView())
			{
				var treeNode = new TreeNode();
				treeNode.Tag = node;
				treeView.Nodes.Add(new TreeNode());
				// Adding a decoy tree node first
				treeView.Nodes[0].Nodes.Add(new TreeNode());
				treeView.Nodes[0].Nodes.Add(treeNode);
				// SUT
				var returnedTreeNode = DictionaryConfigurationController.FindTreeNode(node, treeView.Nodes);
				Assert.That(returnedTreeNode.Tag, Is.EqualTo(node));
			}
		}

		/// <summary/>
		[Test]
		public void FindTreeNode_ReturnsNullIfNotFound()
		{
			var node = new ConfigurableDictionaryNode();
			using (var treeView = new TreeView())
			{
				// Decoys
				treeView.Nodes.Add(new TreeNode());
				treeView.Nodes[0].Nodes.Add(new TreeNode());
				// SUT
				var returnedTreeNode = DictionaryConfigurationController.FindTreeNode(node, treeView.Nodes);
				Assert.That(returnedTreeNode, Is.EqualTo(null));
			}
		}

		/// <summary/>
		[Test]
		public void CreateAndAddTreeNodeForNode_ThrowsOnNullNodeArgument()
		{
			var controller = new DictionaryConfigurationController();
			var parentNode = new ConfigurableDictionaryNode();
			// SUT
			Assert.Throws<ArgumentNullException>(() => controller.CreateAndAddTreeNodeForNode(parentNode, null));
			Assert.Throws<ArgumentNullException>(() => controller.CreateAndAddTreeNodeForNode(null, null));
		}

		/// <summary/>
		[Test]
		public void CreateAndAddTreeNodeForNode_CanAddRoot()
		{
			var controller = new DictionaryConfigurationController();
			var node = new ConfigurableDictionaryNode();
			using (var dummyView = new TestConfigurableDictionaryView())
			{
				controller.View = dummyView;
				// SUT
				controller.CreateAndAddTreeNodeForNode(null, node);
				Assert.That(controller.View.TreeControl.Tree.Nodes.Count, Is.EqualTo(1), "No TreeNode was added");
				Assert.That(controller.View.TreeControl.Tree.Nodes[0].Tag, Is.EqualTo(node), "New TreeNode's tag does not match");
			}
		}

		/// <summary/>
		[Test]
		public void CreateAndAddTreeNodeForNode_SetsCheckbox()
		{
			var controller = new DictionaryConfigurationController();
			var enabledNode = new ConfigurableDictionaryNode {IsEnabled = true};
			var disabledNode = new ConfigurableDictionaryNode {IsEnabled = false};

			using (var dummyView = new TestConfigurableDictionaryView())
			{
				controller.View = dummyView;
				// SUT
				controller.CreateAndAddTreeNodeForNode(null, enabledNode);
				controller.CreateAndAddTreeNodeForNode(null, disabledNode);

				Assert.That(controller.View.TreeControl.Tree.Nodes[0].Checked, Is.EqualTo(true));
				Assert.That(controller.View.TreeControl.Tree.Nodes[1].Checked, Is.EqualTo(false));
			}
		}

		/// <summary/>
		[Test]
		public void CreateTreeOfTreeNodes_ThrowsOnNullNodeArgument()
		{
			var controller = new DictionaryConfigurationController();
			var parentNode = new ConfigurableDictionaryNode();
			// SUT
			Assert.Throws<ArgumentNullException>(() => controller.CreateTreeOfTreeNodes(parentNode, null));
			Assert.Throws<ArgumentNullException>(() => controller.CreateTreeOfTreeNodes(null, null));
		}

		/// <summary/>
		[Test]
		public void CreateTreeOfTreeNodes_CanCreateOneLevelTree()
		{
			using (var view = new TestConfigurableDictionaryView())
			{
				var controller = new DictionaryConfigurationController() {View = view};
				var rootNode = new ConfigurableDictionaryNode() {Label = "0", Children = new List<ConfigurableDictionaryNode>()};
				// SUT
				controller.CreateTreeOfTreeNodes(null, new List<ConfigurableDictionaryNode> { rootNode });

				BasicTreeNodeVerification(controller, rootNode);
			}
		}

		private TreeNode BasicTreeNodeVerification(DictionaryConfigurationController controller, ConfigurableDictionaryNode rootNode)
		{
			Assert.That(controller.View.TreeControl.Tree.Nodes[0].Tag, Is.EqualTo(rootNode), "root TreeNode does not corresponded to expected dictionary configuration node");
			Assert.That(controller.View.TreeControl.Tree.Nodes.Count, Is.EqualTo(1), "Did not expect more than one root TreeNode");
			var rootTreeNode = controller.View.TreeControl.Tree.Nodes[0];
			VerifyTreeNodeHierarchy(rootTreeNode);
			Assert.That(rootTreeNode.Nodes.Count, Is.EqualTo(rootNode.Children.Count), "root treenode does not have expected number of descendants");
			return rootTreeNode;
		}

		/// <summary/>
		[Test]
		public void CreateTreeOfTreeNodes_CanCreateTwoLevelTree()
		{
			using (var view = new TestConfigurableDictionaryView())
			{
				var controller = new DictionaryConfigurationController() { View = view };
				var rootNode = new ConfigurableDictionaryNode() { Label = "0", Children = new List<ConfigurableDictionaryNode>() };
				AddChildrenToNode(rootNode, 3);
				// SUT
				controller.CreateTreeOfTreeNodes(null, new List<ConfigurableDictionaryNode> { rootNode });

				var rootTreeNode = BasicTreeNodeVerification(controller, rootNode);
				string errorMessage = "Should not have made any third-level children that did not exist in the dictionary configuration node hierarchy";
				for (int i = 0; i < 3; i++)
					Assert.That(rootTreeNode.Nodes[i].Nodes.Count, Is.EqualTo(rootNode.Children[i].Children.Count), errorMessage); // ie 0
			}
		}

		/// <summary>
		/// Will create tree with one root node having two child nodes.
		/// The first of those second-level children will have 2 children,
		/// and the second of the second-level children will have 3 children.
		/// </summary>
		[Test]
		public void CreateTreeOfTreeNodes_CanCreateThreeLevelTree()
		{
			using (var view = new TestConfigurableDictionaryView())
			{
				var controller = new DictionaryConfigurationController() { View = view };
				var rootNode = new ConfigurableDictionaryNode() {Label = "0", Children = new List<ConfigurableDictionaryNode>()};
				AddChildrenToNode(rootNode, 2);
				AddChildrenToNode(rootNode.Children[0], 2);
				AddChildrenToNode(rootNode.Children[1], 3);

				// SUT
				controller.CreateTreeOfTreeNodes(null, new List<ConfigurableDictionaryNode> { rootNode });

				var rootTreeNode = BasicTreeNodeVerification(controller, rootNode);
				string errorMessage = "Did not make correct number of third-level children";
				Assert.That(rootTreeNode.Nodes[0].Nodes.Count, Is.EqualTo(rootNode.Children[0].Children.Count), errorMessage); // ie 2
				Assert.That(rootTreeNode.Nodes[1].Nodes.Count, Is.EqualTo(rootNode.Children[1].Children.Count), errorMessage); // ie 3
				string errorMessage2 = "Should not have made any fourth-level children that did not exist in the dictionary configuration node hierarchy.";
				for (int i = 0; i < 2; i++)
					Assert.That(rootTreeNode.Nodes[0].Nodes[i].Nodes.Count, Is.EqualTo(rootNode.Children[0].Children[i].Children.Count), errorMessage2); // ie 0
				for (int i = 0; i < 3; i++)
					Assert.That(rootTreeNode.Nodes[1].Nodes[i].Nodes.Count, Is.EqualTo(rootNode.Children[1].Children[i].Children.Count), errorMessage2); // ie 0
			}
		}

		[Test]
		public void ReadAlternateDictionaryChoices_MissingUserLocationIsCreated()
		{
			var testDefaultFolder =
				Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())).FullName;
			var userFolderName = Path.GetRandomFileName();
			var testUserFolder = Path.Combine(Path.GetTempPath(), userFolderName);
			var controller = new DictionaryConfigurationController();
			// SUT
			Assert.DoesNotThrow(()=>controller.ReadAlternateDictionaryChoices(testDefaultFolder, testUserFolder), "A missing User location should not throw.");
			Assert.IsTrue(Directory.Exists(testUserFolder), "A missing user configuration folder should be created.");
		}

		[Test]
		public void ReadAlternateDictionaryChoices_NoUserFilesUsesDefaults()
		{
			var testDefaultFolder =
				Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
			using(var writer = new StreamWriter(Path.Combine(testDefaultFolder.FullName, "default.xml")))
			{
				writer.Write("test");
			}
			var testUserFolder =
				Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
			var controller = new DictionaryConfigurationController();
			// SUT
			var choices = controller.ReadAlternateDictionaryChoices(testDefaultFolder.FullName, testUserFolder.FullName);
			Assert.IsTrue(choices.Count == 1, "xml configuration file in default directory was not read");
		}

		[Test]
		public void ReadAlternateDictionaryChoices_BothDefaultsAndUserFilesAppear()
		{
			var testDefaultFolder =
				Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
			using(var writer = new StreamWriter(Path.Combine(testDefaultFolder.FullName, "default.xml")))
			{
				writer.Write("test");
			}
			var testUserFolder =
				Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
			using(var writer = new StreamWriter(Path.Combine(testUserFolder.FullName, "user.xml")))
			{
				writer.Write("usertest");
			}
			var controller = new DictionaryConfigurationController();
			// SUT
			var choices = controller.ReadAlternateDictionaryChoices(testDefaultFolder.FullName, testUserFolder.FullName);
			Assert.IsTrue(choices.Count == 2, "One of the configuration files was not listed");
		}

		[Test]
		public void ReadAlternateDictionaryChoices_UserFilesOfSameNameAsDefaultGetOneEntry()
		{
			var testDefaultFolder =
				Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
			using(var writer = new StreamWriter(Path.Combine(testDefaultFolder.FullName, "Root.xml")))
			{
				writer.Write("test");
			}
			var testUserFolder =
				Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
			using(var writer = new StreamWriter(Path.Combine(testUserFolder.FullName, "Root.xml")))
			{
				writer.Write("usertest");
			}
			var controller = new DictionaryConfigurationController();
			// SUT
			var choices = controller.ReadAlternateDictionaryChoices(testDefaultFolder.FullName, testUserFolder.FullName);
			Assert.IsTrue(choices.Count == 1, "Only the user configuration should be listed");
			Assert.IsTrue(choices["Root"].Contains(testUserFolder.FullName), "The default overrode the user configuration.");
		}

		/// <summary/>
		private void AddChildrenToNode(ConfigurableDictionaryNode node, int numberOfChildren)
		{
			for(int childIndex = 0; childIndex < numberOfChildren; childIndex++)
			{
				var child = new ConfigurableDictionaryNode()
				{
					Label = node.Label + "." + childIndex,
					Children = new List<ConfigurableDictionaryNode>(),
					Parent = node
				};
				node.Children.Add(child);
			}
		}

		/// <summary>
		/// Verify that all descendants of treeNode are associated with
		/// ConfigurableDictionaryNode objects with labels that match
		/// the hierarchy that they are found in the TreeNode.
		/// </summary>
		private void VerifyTreeNodeHierarchy(TreeNode treeNode)
		{
			var label = ((ConfigurableDictionaryNode)treeNode.Tag).Label;
			for(int childIndex = 0; childIndex < treeNode.Nodes.Count; childIndex++)
			{
				var child = treeNode.Nodes[childIndex];
				var childLabel = ((ConfigurableDictionaryNode)child.Tag).Label;
				var expectedChildLabel = label + "." + childIndex;
				Assert.That(childLabel, Is.EqualTo(expectedChildLabel), "TreeNode child has associated configuration dictionary node with wrong label");
				VerifyTreeNodeHierarchy(child);
			}
		}

		/// <summary/>
		[Test]
		public void CanReorder_ThrowsOnNullArgument()
		{
			// SUT
			Assert.Throws<ArgumentNullException>(() => DictionaryConfigurationController.CanReorder(null, DictionaryConfigurationController.Direction.Up));
		}

		/// <summary/>
		[Test]
		public void CanReorder_CantMoveUpFirstNode()
		{
			var rootNode = new ConfigurableDictionaryNode() { Label = "root", Children = new List<ConfigurableDictionaryNode>() };
			AddChildrenToNode(rootNode, 2);
			var firstChild = rootNode.Children[0];
			// SUT
			Assert.That(DictionaryConfigurationController.CanReorder(firstChild, DictionaryConfigurationController.Direction.Up), Is.False, "Shouldn't be able to move up the first child");
		}

		/// <summary/>
		[Test]
		public void CanReorder_CanMoveDownFirstNode()
		{
			var rootNode = new ConfigurableDictionaryNode() { Label = "root", Children = new List<ConfigurableDictionaryNode>() };
			AddChildrenToNode(rootNode, 2);
			var firstChild = rootNode.Children[0];
			// SUT
			Assert.That(DictionaryConfigurationController.CanReorder(firstChild, DictionaryConfigurationController.Direction.Down), Is.True, "Should be able to move down the first child");
		}

		/// <summary/>
		[Test]
		public void CanReorder_CanMoveUpSecondNode()
		{
			var rootNode = new ConfigurableDictionaryNode() { Label = "root", Children = new List<ConfigurableDictionaryNode>() };
			AddChildrenToNode(rootNode, 2);
			var secondChild = rootNode.Children[1];
			// SUT
			Assert.That(DictionaryConfigurationController.CanReorder(secondChild, DictionaryConfigurationController.Direction.Up), Is.True, "Should be able to move up the second child");
		}

		/// <summary/>
		[Test]
		public void CanReorder_CantMoveDownLastNode()
		{
			var rootNode = new ConfigurableDictionaryNode() { Label = "root", Children = new List<ConfigurableDictionaryNode>() };
			AddChildrenToNode(rootNode, 2);
			var lastChild = rootNode.Children[1];
			// SUT
			Assert.That(DictionaryConfigurationController.CanReorder(lastChild, DictionaryConfigurationController.Direction.Down), Is.False, "Shouldn't be able to move down the last child");
		}

		/// <summary/>
		[Test]
		public void CanReorder_CantReorderRootNodes()
		{
			var rootNode = new ConfigurableDictionaryNode() { Label = "root", Children = new List<ConfigurableDictionaryNode>() };

			// SUT
			Assert.That(DictionaryConfigurationController.CanReorder(rootNode, DictionaryConfigurationController.Direction.Up), Is.False, "Should not be able to reorder a root node");
			Assert.That(DictionaryConfigurationController.CanReorder(rootNode, DictionaryConfigurationController.Direction.Down), Is.False, "Should not be able to reorder a root node");
		}

		/// <summary/>
		[Test]
		public void Reorder_ThrowsOnNullArgument()
		{
			var controller = new DictionaryConfigurationController();
			// SUT
			Assert.Throws<ArgumentNullException>(() => controller.Reorder(null, DictionaryConfigurationController.Direction.Up));
		}

		/// <summary/>
		[Test]
		public void Reorder_ThrowsIfCantReorder()
		{
			var controller = new DictionaryConfigurationController();
			var rootNode = new ConfigurableDictionaryNode() { Label = "root", Children = new List<ConfigurableDictionaryNode>() };
			AddChildrenToNode(rootNode, 2);
			var firstChild = rootNode.Children[0];
			var secondChild = rootNode.Children[1];
			AddChildrenToNode(firstChild, 1);
			var grandChild = firstChild.Children[0];
			// SUT
			Assert.Throws<ArgumentOutOfRangeException>(() => controller.Reorder(firstChild, DictionaryConfigurationController.Direction.Up));
			Assert.Throws<ArgumentOutOfRangeException>(() => controller.Reorder(secondChild, DictionaryConfigurationController.Direction.Down));
			Assert.Throws<ArgumentOutOfRangeException>(() => controller.Reorder(grandChild, DictionaryConfigurationController.Direction.Up), "Can't move a node with no siblings");
		}

		/// <summary/>
		[Test]
		public void Reorder_ReordersSiblings()
		{
			var movingChildOriginalPosition = 1;
			var movingChildExpectedPosition = 0;
			var directionToMoveChild = DictionaryConfigurationController.Direction.Up;

			MoveSiblingAndVerifyPosition(movingChildOriginalPosition, movingChildExpectedPosition, directionToMoveChild);

			movingChildOriginalPosition = 0;
			movingChildExpectedPosition = 1;
			directionToMoveChild = DictionaryConfigurationController.Direction.Down;

			MoveSiblingAndVerifyPosition(movingChildOriginalPosition, movingChildExpectedPosition, directionToMoveChild);
		}

		[Test]
		public void GetProjectConfigLocationForPath_AlreadyProjectLocNoChange()
		{
			using(var mockMediator = new MockMediator(Cache))
			{
				var mediator = mockMediator.Mediator;
				var projectPath = Path.Combine(Path.Combine(DirectoryFinder.GetConfigSettingsDir(Cache.ProjectId.ProjectFolder), "Test"), "test.xml");
				//SUT
				var controller = new DictionaryConfigurationController();
				var result = controller.GetProjectConfigLocationForPath(projectPath, mediator);
				Assert.AreEqual(result, projectPath);
			}
		}

		[Test]
		public void GetProjectConfigLocationForPath_DefaultLocResultsInProjectPath()
		{
			var defaultPath = Path.Combine(Path.Combine(DirectoryFinder.DefaultConfigurations, "Test"), "test.xml");
			using(var mockMediator = new MockMediator(Cache))
			{
				var mediator = mockMediator.Mediator;
				//SUT
				var controller = new DictionaryConfigurationController();
				Assert.IsFalse(defaultPath.StartsWith(DirectoryFinder.GetConfigSettingsDir(Cache.ProjectId.ProjectFolder)));
				var result = controller.GetProjectConfigLocationForPath(defaultPath, mediator);
				Assert.IsTrue(result.StartsWith(DirectoryFinder.GetConfigSettingsDir(Cache.ProjectId.ProjectFolder)));
				Assert.IsTrue(result.EndsWith(Path.Combine("Test", "test.xml")));
			}
		}
		
		[Test]
		public void GetCustomFieldsForType_NoCustomFieldsGivesEmptyList()
		{
			CollectionAssert.IsEmpty(DictionaryConfigurationController.GetCustomFieldsForType(Cache, "LexEntry"));
		}

		[Test]
		public void GetCustomFieldsForType_EntryCustomFieldIsRepresented()
		{
				using(var cf = new CustomFieldForTest(Cache, "CustomString", Cache.MetaDataCacheAccessor.GetClassId("LexEntry"), 0,
									 CellarPropertyType.MultiString, Guid.Empty))
			{
				var customFieldNodes = DictionaryConfigurationController.GetCustomFieldsForType(Cache,
																														  "LexEntry");
				CollectionAssert.IsNotEmpty(customFieldNodes);
				Assert.IsTrue(customFieldNodes[0].Label == "CustomString");
			}
		}

		[Test]
		public void GetCustomFieldsForType_SenseCustomFieldIsRepresented()
		{
			using(var cf = new CustomFieldForTest(Cache, "CustomCollection", Cache.MetaDataCacheAccessor.GetClassId("LexSense"), 0,
								 CellarPropertyType.ReferenceCollection, Guid.Empty))
			{
				var customFieldNodes = DictionaryConfigurationController.GetCustomFieldsForType(Cache,
																														  "LexSense");
				CollectionAssert.IsNotEmpty(customFieldNodes);
				Assert.IsTrue(customFieldNodes[0].Label == "CustomCollection");
			}
		}

		[Test]
		public void GetCustomFieldsForType_MorphCustomFieldIsRepresented()
		{
			using(var cf = new CustomFieldForTest(Cache, "CustomCollection", Cache.MetaDataCacheAccessor.GetClassId("MoForm"), 0,
								 CellarPropertyType.ReferenceCollection, Guid.Empty))
			{
				var customFieldNodes = DictionaryConfigurationController.GetCustomFieldsForType(Cache,
																														  "MoForm");
				CollectionAssert.IsNotEmpty(customFieldNodes);
				Assert.IsTrue(customFieldNodes[0].Label == "CustomCollection");
			}
		}

		[Test]
		public void GetCustomFieldsForType_ExampleCustomFieldIsRepresented()
		{
			using(var cf = new CustomFieldForTest(Cache, "CustomCollection", Cache.MetaDataCacheAccessor.GetClassId("LexExampleSentence"), 0,
								 CellarPropertyType.ReferenceCollection, Guid.Empty))
			{
				var customFieldNodes = DictionaryConfigurationController.GetCustomFieldsForType(Cache,
																														  "LexExampleSentence");
				CollectionAssert.IsNotEmpty(customFieldNodes);
				Assert.IsTrue(customFieldNodes[0].Label == "CustomCollection");
			}
		}

		[Test]
		public void GetCustomFieldsForType_MultipleFieldsAreReturned()
		{
			using(var cf = new CustomFieldForTest(Cache, "CustomCollection", Cache.MetaDataCacheAccessor.GetClassId("LexSense"), 0,
								 CellarPropertyType.ReferenceCollection, Guid.Empty))
			using(var cf2 = new CustomFieldForTest(Cache, "CustomString", Cache.MetaDataCacheAccessor.GetClassId("LexSense"), 0,
								 CellarPropertyType.ReferenceCollection, Guid.Empty))
			{
				var customFieldNodes = DictionaryConfigurationController.GetCustomFieldsForType(Cache,
																														  "LexSense");
				CollectionAssert.IsNotEmpty(customFieldNodes);
				CollectionAssert.AllItemsAreUnique(customFieldNodes);
				Assert.IsTrue(customFieldNodes.Count == 2, "Incorrect number of nodes created from the custom fields.");
				Assert.IsTrue(customFieldNodes[0].Label == "CustomCollection");
				Assert.IsTrue(customFieldNodes[1].Label == "CustomString");
			}
		}

		private void MoveSiblingAndVerifyPosition(int movingChildOriginalPosition, int movingChildExpectedPosition,
			DictionaryConfigurationController.Direction directionToMoveChild)
		{
			using (var view = new TestConfigurableDictionaryView())
			{
				var controller = new DictionaryConfigurationController() { View = view, _model = m_model };
				var rootNode = new ConfigurableDictionaryNode() {Label = "root", Children = new List<ConfigurableDictionaryNode>()};
				AddChildrenToNode(rootNode, 2);
				var movingChild = rootNode.Children[movingChildOriginalPosition];
				var otherChild = rootNode.Children[movingChildExpectedPosition];
				m_model.Parts = new List<ConfigurableDictionaryNode>() {rootNode};
				// SUT
				controller.Reorder(movingChild, directionToMoveChild);
				Assert.That(rootNode.Children[movingChildExpectedPosition], Is.EqualTo(movingChild), "movingChild should have been moved");
				Assert.That(rootNode.Children[movingChildOriginalPosition], Is.Not.EqualTo(movingChild), "movingChild should not still be in original position");
				Assert.That(rootNode.Children[movingChildOriginalPosition], Is.EqualTo(otherChild), "unexpected child in original movingChild position");
				Assert.That(rootNode.Children.Count, Is.EqualTo(2), "unexpected number of reordered siblings");
			}
		}

		private sealed class MockMediator : IDisposable
		{
			private MockFwXApp application;
			private MockFwXWindow window;

			public Mediator Mediator { get; set; }

			public MockMediator(FdoCache cache)
			{
				var manager = new MockFwManager { Cache = cache };
				FwRegistrySettings.Init(); // Sets up fake static registry values for the MockFwXApp to use
				application = new MockFwXApp(manager, null, null);
				window = new MockFwXWindow(application, Path.GetTempFileName());
				window.Init(cache); // initializes Mediator values
				Mediator = window.Mediator;
			}

			public void Dispose()
			{
				application.Dispose();
				window.Dispose();
				Mediator.Dispose();
			}
		}

		[Test]
		public void MergeCustomFieldsIntoDictionaryModel_NewFieldsAreAdded()
		{
			using(var cf2 = new CustomFieldForTest(Cache, "CustomString",
															Cache.MetaDataCacheAccessor.GetClassId("LexEntry"), 0,
															CellarPropertyType.ReferenceCollection, Guid.Empty))
			{
				var model = new DictionaryConfigurationModel();
				model.Parts = new List<ConfigurableDictionaryNode>
					{
						new ConfigurableDictionaryNode { Label = "Main Entry", FieldDescription = "LexEntry" }
					};
				//SUT
				DictionaryConfigurationController.MergeCustomFieldsIntoDictionaryModel(Cache, model);
				Assert.IsNotNull(model.Parts[0].Children, "Custom Field did not add to children");
				CollectionAssert.IsNotEmpty(model.Parts[0].Children, "Custom Field did not add to children");
				Assert.AreEqual(model.Parts[0].Children[0].Label, "CustomString");
				Assert.AreEqual(model.Parts[0].Children[0].FieldDescription, "CustomString");
				Assert.AreEqual(model.Parts[0].Children[0].IsCustomField, true);
			}
		}

		[Test]
		public void MergeCustomFieldsIntoDictionaryModel_FieldsAreNotDuplicated()
		{
			using(var cf2 = new CustomFieldForTest(Cache, "CustomString",
															Cache.MetaDataCacheAccessor.GetClassId("LexEntry"), 0,
															CellarPropertyType.ReferenceCollection, Guid.Empty))
			{
				var model = new DictionaryConfigurationModel();
				var customNode = new ConfigurableDictionaryNode()
				{
					Label = "CustomString",
					FieldDescription = "CustomString",
					IsCustomField = true
				};
				var entryNode = new ConfigurableDictionaryNode
				{
					Label = "Main Entry",
					FieldDescription = "LexEntry",
					Children = new List<ConfigurableDictionaryNode> { customNode }
				};
				model.Parts = new List<ConfigurableDictionaryNode> { entryNode };

				//SUT
				DictionaryConfigurationController.MergeCustomFieldsIntoDictionaryModel(Cache, model);
				Assert.AreEqual(1, model.Parts[0].Children.Count, "Only the existing custom field node should be present");
			}
		}

		[Test]
		public void MergeCustomFieldsIntoDictionaryModel_DeletedFieldsAreRemoved()
		{
			var model = new DictionaryConfigurationModel();
			var customNode = new ConfigurableDictionaryNode()
			{
				Label = "CustomString",
				FieldDescription = "CustomString",
				IsCustomField = true
			};
			var entryNode = new ConfigurableDictionaryNode
			{
				Label = "Main Entry",
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { customNode }
			};
			model.Parts = new List<ConfigurableDictionaryNode> { entryNode };

			//SUT
			DictionaryConfigurationController.MergeCustomFieldsIntoDictionaryModel(Cache, model);
			Assert.AreEqual(0, model.Parts[0].Children.Count, "The custom field in the model should have been removed since it isn't in the project(cache)");
		}

		[Test]
		public void MergeCustomFieldsIntoDictionaryModel_ExampleCustomFieldIsRepresented()
		{
			using(var cf = new CustomFieldForTest(Cache, "CustomCollection",
														  Cache.MetaDataCacheAccessor.GetClassId("LexExampleSentence"), 0,
														  CellarPropertyType.ReferenceCollection, Guid.Empty))
			{
				var model = new DictionaryConfigurationModel();
				var examplesNode = new ConfigurableDictionaryNode()
				{
					Label = "Example Sentences",
					FieldDescription = "Examples"
				};
				var senseNode = new ConfigurableDictionaryNode()
				{
					Label = "Senses",
					FieldDescription = "Senses",
					Children = new List<ConfigurableDictionaryNode> { examplesNode }
				};
				var entryNode = new ConfigurableDictionaryNode
				{
					Label = "Main Entry",
					FieldDescription = "LexEntry",
					Children = new List<ConfigurableDictionaryNode> { senseNode }
				};
				model.Parts = new List<ConfigurableDictionaryNode> { entryNode };

				//SUT
				DictionaryConfigurationController.MergeCustomFieldsIntoDictionaryModel(Cache, model);
				Assert.AreEqual(1, examplesNode.Children.Count, "Custom field should have been added to ExampleSentence");
			}
		}

		[Test]
		public void MergeCustomFieldsIntoModel_MergeWithDefaultRootModelDoesNotThrow()
		{
			using(var cf = new CustomFieldForTest(Cache, "CustomCollection",
														  Cache.MetaDataCacheAccessor.GetClassId("LexExampleSentence"), 0,
														  CellarPropertyType.ReferenceCollection, Guid.Empty))
			{
				var model = new DictionaryConfigurationModel(Path.Combine(DirectoryFinder.DefaultConfigurations, Path.Combine("Dictionary", "Root.xml")), Cache);

				//SUT
				Assert.DoesNotThrow(()=>DictionaryConfigurationController.MergeCustomFieldsIntoDictionaryModel(Cache, model));
			}
		}

		private sealed class TestConfigurableDictionaryView : IDictionaryConfigurationView, IDisposable
		{
			private DictionaryConfigurationTreeControl m_treeControl = new DictionaryConfigurationTreeControl();

			public DictionaryConfigurationTreeControl TreeControl
			{
				get { return m_treeControl; }
			}

			public DetailsView DetailsView { set; private get; }
			public string PreviewData { set; private get; }

			public void Redraw()
			{
				;
			}

			public void SetChoices(IEnumerable<string> choices)
			{
				;
			}

			public void Dispose()
			{
				m_treeControl.Dispose();
			}
#pragma warning disable 67
			public event EventHandler ManageViews;

			public event EventHandler SaveModel;

			public event SwitchViewEvent SwitchView;
#pragma warning restore 67
		}

		/// <summary>
		/// Creates and removes a custom field, designed for using statement
		/// </summary>
		private sealed class CustomFieldForTest : IDisposable
		{
			private FieldDescription m_customField;
			public CustomFieldForTest(FdoCache cache, string customFieldName, int classId, int ws,
											  CellarPropertyType fieldType, Guid listGuid)
			{
				m_customField = new FieldDescription(cache)
				{
					Userlabel = customFieldName,
					HelpString = string.Empty,
					Class = classId,
					ListRootId = listGuid,
					Type = fieldType,
				};
				m_customField.UpdateCustomField();
			}

			public void Dispose()
			{
				m_customField.MarkForDeletion = true;
				m_customField.UpdateCustomField();
			}
		}
	}
}

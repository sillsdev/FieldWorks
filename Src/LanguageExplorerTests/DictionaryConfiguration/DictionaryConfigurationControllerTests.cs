// Copyright (c) 2014-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer;
using LanguageExplorer.Areas;
using LanguageExplorer.Controls.XMLViews;
using LanguageExplorer.DictionaryConfiguration;
using LanguageExplorer.TestUtilities;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.IO;
using SIL.LCModel;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using FileUtils = SIL.LCModel.Utils.FileUtils;

namespace LanguageExplorerTests.DictionaryConfiguration
{
	[TestFixture]
	public class DictionaryConfigurationControllerTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private FlexComponentParameters _flexComponentParameters;
		private IPropertyTable _propertyTable;
		private const string _field = "LexEntry";
		private DictionaryConfigurationModel _model;
		private const int AnalysisWsId = -5;

		#region Setup and Teardown

		public override void TestSetup()
		{
			base.TestSetup();

			_model = new DictionaryConfigurationModel();
			_flexComponentParameters = TestSetupServices.SetupEverything(Cache);
			_propertyTable = _flexComponentParameters.PropertyTable;
			// Add styles to the stylesheet to prevent intermittent unit test failures setting the selected index in the Styles Combobox
			var styles = FwUtils.StyleSheetFromPropertyTable(_propertyTable).Styles;
			styles.Add(new BaseStyleInfo { Name = "Dictionary-Normal", IsParagraphStyle = true });
			styles.Add(new BaseStyleInfo { Name = "Dictionary-Headword", IsParagraphStyle = false });
			styles.Add(new BaseStyleInfo { Name = "Bulleted List", IsParagraphStyle = true });
		}

		public override void TestTearDown()
		{
			try
			{
				TestSetupServices.DisposeTrash(_flexComponentParameters);
				_flexComponentParameters = null;
				_propertyTable = null;
				_model = null;
			}
			catch (Exception err)
			{
				throw new Exception($"Error in running {GetType().Name} TestTearDown method.", err);
			}
			finally
			{
				base.TestTearDown();
			}
		}

		#endregion Setup and Teardown

		/// <summary>
		/// Deletes any files resulting from model saves by the controller in the tests
		/// </summary>
		private static void DeleteConfigurationTestModelFiles(DictionaryConfigurationController dcc)
		{
			foreach (var model in dcc._dictionaryConfigurations)
			{
				if (File.Exists(model.FilePath) && !model.FilePath.StartsWith(FwDirectoryFinder.DefaultConfigurations))
				{
					// I believe that moving the file before deleting will avoid problems that crop up as a result
					// of the File.Delete call returning before the file is actually removed by the OS
					var pathToTempForDeletion = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
					File.Move(model.FilePath, pathToTempForDeletion);
					File.Delete(pathToTempForDeletion);
				}
			}
			var dcFolder = Directory.GetParent(DictionaryConfigurationServices.TestDataPath);
			var tempConfigurationSettings = Path.Combine(dcFolder.FullName, "ConfigurationSettings");
			if (Directory.Exists(tempConfigurationSettings))
			{
				RobustIO.DeleteDirectoryAndContents(tempConfigurationSettings);
			}

		}

		/// <summary>
		/// This test verifies that PopulateTreeView builds a TreeView that has the same structure as the model it is based on
		/// </summary>
		[Test]
		public void PopulateTreeViewBuildsRightNumberOfNodes()
		{
			using (var testView = new TestConfigurableDictionaryView())
			{
				_model.Parts = new List<ConfigurableDictionaryNode>
				{
					BuildTestPartTree(2, 5)
				};
				var dcc = new DictionaryConfigurationController { View = testView, _model = _model };
				//SUT
				dcc.PopulateTreeView();
				ValidateTreeForm(2, 5, dcc.View.TreeControl.Tree);
			}
		}

		private static void ValidateTreeForm(int levels, int nodeCount, TreeView treeView)
		{
			var validationCount = 0;
			var validationLevels = 0;
			CalculateTreeInfo(ref validationLevels, ref validationCount, treeView.Nodes);
			Assert.AreEqual(levels, validationLevels, "Tree hierarchy incorrect");
			Assert.AreEqual(nodeCount, validationCount, "Tree node count incorrect");
		}

		private static void CalculateTreeInfo(ref int levels, ref int count, TreeNodeCollection nodes)
		{
			if (nodes == null || nodes.Count < 1)
			{
				return;
			}
			++levels;
			foreach (TreeNode node in nodes)
			{
				++count;
				if (node.Nodes.Count > 0)
				{
					CalculateTreeInfo(ref levels, ref count, node.Nodes);
				}
			}
		}

		/// <summary>
		/// Builds a test tree of ConfigurableDictionary nodes with the given numbers of levels and nodes
		/// the structure of the tree is TODO: say what we did once this test code stabilizes
		/// </summary>
		private ConfigurableDictionaryNode BuildTestPartTree(int numberOfLevels, int numberOfNodes)
		{
			if (numberOfLevels < 1)
			{
				throw new ArgumentException("You wanted less than one level in the hierarchy.  Really?");
			}
			if (numberOfNodes < numberOfLevels)
			{
				throw new ArgumentException("You asked for more levels in the hierarchy then nodes in the tree; how did you expect me to do that?");
			}
			ConfigurableDictionaryNode rootNode = null;
			ConfigurableDictionaryNode workingNode = null;
			var children = new List<ConfigurableDictionaryNode>();
			for (var i = 0; i < numberOfLevels; ++i)
			{
				if (workingNode == null)
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
			for (var i = 0; i < numberOfNodes - numberOfLevels; ++i)
			{
				children.Add(new ConfigurableDictionaryNode { Label = "extraChild" + i });
			}
			return rootNode;
		}

		/// <summary />
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

		/// <summary />
		[Test]
		public void FindTreeNode_CanFindRoot()
		{
			var node = new ConfigurableDictionaryNode();
			using (var treeView = new TreeView())
			{
				var treeNode = new TreeNode { Tag = node };
				treeView.Nodes.Add(treeNode);
				treeView.TopNode = treeNode;
				// SUT
				var returnedTreeNode = DictionaryConfigurationController.FindTreeNode(node, treeView.Nodes);
				Assert.That(returnedTreeNode.Tag, Is.EqualTo(node));
			}
		}

		/// <summary />
		[Test]
		public void FindTreeNode_CanFindChild()
		{
			var node = new ConfigurableDictionaryNode();
			using (var treeView = new TreeView())
			{
				var treeNode = new TreeNode { Tag = node };
				treeView.Nodes.Add(new TreeNode());
				// Adding a decoy tree node first
				treeView.Nodes[0].Nodes.Add(new TreeNode());
				treeView.Nodes[0].Nodes.Add(treeNode);
				// SUT
				var returnedTreeNode = DictionaryConfigurationController.FindTreeNode(node, treeView.Nodes);
				Assert.That(returnedTreeNode.Tag, Is.EqualTo(node));
			}
		}

		/// <summary />
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

		/// <summary />
		[Test]
		public void CreateAndAddTreeNodeForNode_ThrowsOnNullNodeArgument()
		{
			var controller = new DictionaryConfigurationController();
			var parentNode = new ConfigurableDictionaryNode();
			// SUT
			Assert.Throws<ArgumentNullException>(() => controller.CreateAndAddTreeNodeForNode(parentNode, null));
			Assert.Throws<ArgumentNullException>(() => controller.CreateAndAddTreeNodeForNode(null, null));
		}

		/// <summary />
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

		/// <summary />
		[Test]
		public void CreateAndAddTreeNodeForNode_SetsCheckbox()
		{
			var controller = new DictionaryConfigurationController();
			var enabledNode = new ConfigurableDictionaryNode { IsEnabled = true };
			var disabledNode = new ConfigurableDictionaryNode { IsEnabled = false };
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

		/// <summary />
		[Test]
		public void CreateTreeOfTreeNodes_ThrowsOnNullNodeArgument()
		{
			var controller = new DictionaryConfigurationController();
			var parentNode = new ConfigurableDictionaryNode();
			// SUT
			Assert.Throws<ArgumentNullException>(() => controller.CreateTreeOfTreeNodes(parentNode, null));
			Assert.Throws<ArgumentNullException>(() => controller.CreateTreeOfTreeNodes(null, null));
		}

		/// <summary />
		[Test]
		public void CreateTreeOfTreeNodes_CanCreateOneLevelTree()
		{
			using (var view = new TestConfigurableDictionaryView())
			{
				var controller = new DictionaryConfigurationController { View = view };
				var rootNode = new ConfigurableDictionaryNode { Label = "0", Children = new List<ConfigurableDictionaryNode>() };
				// SUT
				controller.CreateTreeOfTreeNodes(null, new List<ConfigurableDictionaryNode> { rootNode });
				BasicTreeNodeVerification(controller, rootNode);
			}
		}

		/// <summary />
		[Test]
		public void CreateTreeOfTreeNodes_CanCreateTwoLevelTree()
		{
			using (var view = new TestConfigurableDictionaryView())
			{
				var controller = new DictionaryConfigurationController { View = view };
				var rootNode = new ConfigurableDictionaryNode { Label = "0", Children = new List<ConfigurableDictionaryNode>() };
				AddChildrenToNode(rootNode, 3);
				// SUT
				controller.CreateTreeOfTreeNodes(null, new List<ConfigurableDictionaryNode> { rootNode });
				var rootTreeNode = BasicTreeNodeVerification(controller, rootNode);
				const string errorMessage = "Should not have made any third-level children that did not exist in the dictionary configuration node hierarchy";
				for (var i = 0; i < 3; i++)
				{
					Assert.That(rootTreeNode.Nodes[i].Nodes.Count, Is.EqualTo(rootNode.Children[i].Children.Count), errorMessage); // ie 0
				}
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
				var controller = new DictionaryConfigurationController { View = view };
				var rootNode = new ConfigurableDictionaryNode { Label = "0", Children = new List<ConfigurableDictionaryNode>() };
				AddChildrenToNode(rootNode, 2);
				AddChildrenToNode(rootNode.Children[0], 2);
				AddChildrenToNode(rootNode.Children[1], 3);
				// SUT
				controller.CreateTreeOfTreeNodes(null, new List<ConfigurableDictionaryNode> { rootNode });
				var rootTreeNode = BasicTreeNodeVerification(controller, rootNode);
				const string errorMessage = "Did not make correct number of third-level children";
				Assert.That(rootTreeNode.Nodes[0].Nodes.Count, Is.EqualTo(rootNode.Children[0].Children.Count), errorMessage); // ie 2
				Assert.That(rootTreeNode.Nodes[1].Nodes.Count, Is.EqualTo(rootNode.Children[1].Children.Count), errorMessage); // ie 3
				const string errorMessage2 = "Should not have made any fourth-level children that did not exist in the dictionary configuration node hierarchy.";
				for (var i = 0; i < 2; i++)
				{
					Assert.That(rootTreeNode.Nodes[0].Nodes[i].Nodes.Count, Is.EqualTo(rootNode.Children[0].Children[i].Children.Count), errorMessage2); // ie 0
				}
				for (var i = 0; i < 3; i++)
				{
					Assert.That(rootTreeNode.Nodes[1].Nodes[i].Nodes.Count, Is.EqualTo(rootNode.Children[1].Children[i].Children.Count), errorMessage2); // ie 0
				}
			}
		}

		[Test]
		public void CreateTreeOfTreeNodes_PrefersReferencedChildren()
		{
			using (var view = new TestConfigurableDictionaryView())
			{
				var controller = new DictionaryConfigurationController { View = view };
				var rootNode = new ConfigurableDictionaryNode { Label = "0", Children = new List<ConfigurableDictionaryNode>() };
				var refdNode = new ConfigurableDictionaryNode { Label = "R", Parent = rootNode, Children = new List<ConfigurableDictionaryNode>() };
				rootNode.ReferencedNode = refdNode;
				AddChildrenToNode(rootNode, 2);
				AddChildrenToNode(refdNode, 4);
				// SUT
				controller.CreateTreeOfTreeNodes(null, new List<ConfigurableDictionaryNode> { rootNode });
				var rootTreeNode = BasicTreeNodeVerification(controller, rootNode);
				Assert.That(rootTreeNode.Nodes[0].Nodes.Count, Is.EqualTo(rootNode.ReferencedNode.Children[0].Children.Count), "Should not have made any third-level children that did not exist in the dictionary configuration node hierarchy");
			}
		}

		[Test]
		public void ListDictionaryConfigurationChoices_MissingUserLocationIsCreated()
		{
			var testDefaultFolder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())).FullName;
			var userFolderName = Path.GetRandomFileName();
			var testUserFolder = Path.Combine(Path.GetTempPath(), userFolderName);
			try
			{
				// SUT
				Assert.DoesNotThrow(() => DictionaryConfigurationServices.ListDictionaryConfigurationChoices(testDefaultFolder, testUserFolder), "A missing User location should not throw.");
				Assert.IsTrue(Directory.Exists(testUserFolder), "A missing user configuration folder should be created.");
			}
			finally
			{
				RobustIO.DeleteDirectoryAndContents(testDefaultFolder);
				RobustIO.DeleteDirectoryAndContents(testUserFolder);
			}
		}

		[Test]
		public void ListDictionaryConfigurationChoices_NoUserFilesUsesDefaults()
		{
			var testDefaultFolder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
			try
			{
				using (var writer = new StreamWriter(string.Concat(Path.Combine(testDefaultFolder.FullName, "default"), LanguageExplorerConstants.DictionaryConfigurationFileExtension)))
				{
					writer.Write("test");
				}
				var testUserFolder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
				// SUT
				var choices = DictionaryConfigurationServices.ListDictionaryConfigurationChoices(testDefaultFolder.FullName, testUserFolder.FullName);
				Assert.IsTrue(choices.Count == 1, "xml configuration file in default directory was not read");
			}
			finally
			{
				RobustIO.DeleteDirectoryAndContents(testDefaultFolder.FullName);
			}
		}

		[Test]
		public void ListDictionaryConfigurationChoices_BothDefaultsAndUserFilesAppear()
		{
			var testDefaultFolder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
			var testUserFolder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
			try
			{
				using (var writer = new StreamWriter(string.Concat(Path.Combine(testDefaultFolder.FullName, "default"), LanguageExplorerConstants.DictionaryConfigurationFileExtension)))
				{
					writer.Write("test");
				}
				using (var writer = new StreamWriter(string.Concat(Path.Combine(testUserFolder.FullName, "user"), LanguageExplorerConstants.DictionaryConfigurationFileExtension)))
				{
					writer.Write("usertest");
				}
				// SUT
				var choices = DictionaryConfigurationServices.ListDictionaryConfigurationChoices(testDefaultFolder.FullName, testUserFolder.FullName);
				Assert.IsTrue(choices.Count == 2, "One of the configuration files was not listed");
			}
			finally
			{
				RobustIO.DeleteDirectoryAndContents(testDefaultFolder.FullName);
				RobustIO.DeleteDirectoryAndContents(testUserFolder.FullName);
			}
		}

		[Test]
		public void ListDictionaryConfigurationChoices_UserFilesOfSameNameAsDefaultGetOneEntry()
		{
			var testDefaultFolder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
			var testUserFolder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
			try
			{
				using (var writer = new StreamWriter(string.Concat(Path.Combine(testDefaultFolder.FullName, "Root"), LanguageExplorerConstants.DictionaryConfigurationFileExtension)))
				{
					writer.Write("test");
				}
				using (var writer = new StreamWriter(string.Concat(Path.Combine(testUserFolder.FullName, "Root"), LanguageExplorerConstants.DictionaryConfigurationFileExtension)))
				{
					writer.Write("usertest");
				}
				// SUT
				var choices = DictionaryConfigurationServices.ListDictionaryConfigurationChoices(testDefaultFolder.FullName, testUserFolder.FullName);
				Assert.IsTrue(choices.Count == 1, "Only the user configuration should be listed");
				Assert.IsTrue(choices[0].Contains(testUserFolder.FullName), "The default overrode the user configuration.");
			}
			finally
			{
				RobustIO.DeleteDirectoryAndContents(testDefaultFolder.FullName);
				RobustIO.DeleteDirectoryAndContents(testUserFolder.FullName);
			}
		}

		[Test]
		public void GetListOfDictionaryConfigurationLabels_ListsLabels()
		{
			var testDefaultFolder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
			var testUserFolder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
			try
			{
				_model.Label = "configurationALabel";
				_model.FilePath = string.Concat(Path.Combine(testDefaultFolder.FullName, "configurationA"), LanguageExplorerConstants.DictionaryConfigurationFileExtension);
				_model.Save();
				_model.Label = "configurationBLabel";
				_model.FilePath = string.Concat(Path.Combine(testUserFolder.FullName, "configurationB"), LanguageExplorerConstants.DictionaryConfigurationFileExtension);
				_model.Save();
				// SUT
				var labels = DictionaryConfigurationServices.GetDictionaryConfigurationLabels(Cache, testDefaultFolder.FullName, testUserFolder.FullName);
				Assert.Contains("configurationALabel", labels.Keys, "missing a label");
				Assert.Contains("configurationBLabel", labels.Keys, "missing a label");
				Assert.That(labels.Count, Is.EqualTo(2), "unexpected label count");
				Assert.That(labels["configurationALabel"].FilePath, Is.StringContaining(string.Concat("configurationA", LanguageExplorerConstants.DictionaryConfigurationFileExtension)), "missing a file name");
				Assert.That(labels["configurationBLabel"].FilePath, Is.StringContaining(string.Concat("configurationB", LanguageExplorerConstants.DictionaryConfigurationFileExtension)), "missing a file name");
			}
			finally
			{
				RobustIO.DeleteDirectoryAndContents(testDefaultFolder.FullName);
				RobustIO.DeleteDirectoryAndContents(testUserFolder.FullName);
			}
		}

		/// <summary />
		private static void AddChildrenToNode(ConfigurableDictionaryNode node, int numberOfChildren)
		{
			for (var childIndex = 0; childIndex < numberOfChildren; childIndex++)
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

		private static ConfigurableDictionaryNode AddGroupingNodeToNode(ConfigurableDictionaryNode rootNode, int index, int groupChildren)
		{
			var groupNode = new ConfigurableDictionaryNode
			{
				Label = rootNode.Label + "-group" + index,
				Children = new List<ConfigurableDictionaryNode>(),
				Parent = rootNode,
				DictionaryNodeOptions = new DictionaryNodeGroupingOptions()
			};
			AddChildrenToNode(groupNode, groupChildren);
			rootNode.Children.Insert(index, groupNode);
			return groupNode;
		}

		private static TreeNode BasicTreeNodeVerification(DictionaryConfigurationController controller, ConfigurableDictionaryNode rootNode)
		{
			Assert.That(controller.View.TreeControl.Tree.Nodes[0].Tag, Is.EqualTo(rootNode), "root TreeNode does not corresponded to expected dictionary configuration node");
			Assert.That(controller.View.TreeControl.Tree.Nodes.Count, Is.EqualTo(1), "Did not expect more than one root TreeNode");
			var rootTreeNode = controller.View.TreeControl.Tree.Nodes[0];
			VerifyTreeNodeHierarchy(rootTreeNode);
			// A SharedItem's Children should be configurable under its Master Parent and nowhere else
			var childrenCount = rootNode.IsSubordinateParent ? 0 : rootNode.ReferencedOrDirectChildren.Count;
			Assert.That(rootTreeNode.Nodes.Count, Is.EqualTo(childrenCount), "root treenode does not have expected number of descendants");
			return rootTreeNode;
		}

		/// <summary>
		/// Verify that all descendants of treeNode are associated with
		/// ConfigurableDictionaryNode objects with labels that match
		/// the hierarchy that they are found in the TreeNode.
		/// </summary>
		private static void VerifyTreeNodeHierarchy(TreeNode treeNode)
		{
			var configNode = (ConfigurableDictionaryNode)treeNode.Tag;
			var labelPrefix = configNode.ReferencedNode == null ? configNode.Label : configNode.ReferencedNode.Label;
			for (var childIndex = 0; childIndex < treeNode.Nodes.Count; childIndex++)
			{
				var child = treeNode.Nodes[childIndex];
				var childLabel = ((ConfigurableDictionaryNode)child.Tag).Label;
				var expectedChildLabel = labelPrefix + "." + childIndex;
				Assert.That(childLabel, Is.EqualTo(expectedChildLabel), "TreeNode child has associated configuration dictionary node with wrong label");
				VerifyTreeNodeHierarchy(child);
			}
		}

		/// <summary />
		[Test]
		public void CanReorder_ThrowsOnNullArgument()
		{
			// SUT
			Assert.Throws<ArgumentNullException>(() => DictionaryConfigurationController.CanReorder(null, Direction.Up));
		}

		/// <summary />
		[Test]
		public void CanReorder_CantMoveUpFirstNode()
		{
			var rootNode = new ConfigurableDictionaryNode() { Label = "root", Children = new List<ConfigurableDictionaryNode>() };
			AddChildrenToNode(rootNode, 2);
			var firstChild = rootNode.Children[0];
			// SUT
			Assert.That(DictionaryConfigurationController.CanReorder(firstChild, Direction.Up), Is.False, "Shouldn't be able to move up the first child");
		}

		/// <summary />
		[Test]
		public void CanReorder_CanMoveDownFirstNode()
		{
			var rootNode = new ConfigurableDictionaryNode() { Label = "root", Children = new List<ConfigurableDictionaryNode>() };
			AddChildrenToNode(rootNode, 2);
			var firstChild = rootNode.Children[0];
			// SUT
			Assert.That(DictionaryConfigurationController.CanReorder(firstChild, Direction.Down), Is.True, "Should be able to move down the first child");
		}

		/// <summary />
		[Test]
		public void CanReorder_CanMoveUpSecondNode()
		{
			var rootNode = new ConfigurableDictionaryNode() { Label = "root", Children = new List<ConfigurableDictionaryNode>() };
			AddChildrenToNode(rootNode, 2);
			var secondChild = rootNode.Children[1];
			// SUT
			Assert.That(DictionaryConfigurationController.CanReorder(secondChild, Direction.Up), Is.True, "Should be able to move up the second child");
		}

		/// <summary />
		[Test]
		public void CanReorder_CantMoveDownLastNode()
		{
			var rootNode = new ConfigurableDictionaryNode() { Label = "root", Children = new List<ConfigurableDictionaryNode>() };
			AddChildrenToNode(rootNode, 2);
			var lastChild = rootNode.Children[1];
			// SUT
			Assert.That(DictionaryConfigurationController.CanReorder(lastChild, Direction.Down), Is.False, "Shouldn't be able to move down the last child");
		}

		/// <summary />
		[Test]
		public void CanReorder_CantReorderRootNodes()
		{
			var rootNode = new ConfigurableDictionaryNode() { Label = "root", Children = new List<ConfigurableDictionaryNode>() };
			// SUT
			Assert.That(DictionaryConfigurationController.CanReorder(rootNode, Direction.Up), Is.False, "Should not be able to reorder a root node");
			Assert.That(DictionaryConfigurationController.CanReorder(rootNode, Direction.Down), Is.False, "Should not be able to reorder a root node");
		}

		/// <summary />
		[Test]
		public void Reorder_ThrowsOnNullArgument()
		{
			var controller = new DictionaryConfigurationController();
			// SUT
			Assert.Throws<ArgumentNullException>(() => controller.Reorder(null, Direction.Up));
		}

		/// <summary />
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
			Assert.Throws<ArgumentOutOfRangeException>(() => controller.Reorder(firstChild, Direction.Up));
			Assert.Throws<ArgumentOutOfRangeException>(() => controller.Reorder(secondChild, Direction.Down));
			Assert.Throws<ArgumentOutOfRangeException>(() => controller.Reorder(grandChild, Direction.Up), "Can't move a node with no siblings");
		}

		/// <summary />
		[Test]
		[TestCase(1, 0, 0)] // Move sibling from index 1 up one
		[TestCase(0, 1, 1)] // Move sibling from index 0 down one
		public void Reorder_ReordersSiblings(int movingChildOriginalPos, int movingChildExpectedPos, int direction)
		{
			var directionToMove = direction == 0 ? Direction.Up : Direction.Down;
			using (var view = new TestConfigurableDictionaryView())
			{
				var controller = new DictionaryConfigurationController { View = view, _model = _model };
				var rootNode = new ConfigurableDictionaryNode { Label = "root", Children = new List<ConfigurableDictionaryNode>() };
				AddChildrenToNode(rootNode, 2);
				var movingChild = rootNode.Children[movingChildOriginalPos];
				var otherChild = rootNode.Children[movingChildExpectedPos];
				_model.Parts = new List<ConfigurableDictionaryNode> { rootNode };
				// SUT
				controller.Reorder(movingChild, directionToMove);
				Assert.That(rootNode.Children[movingChildExpectedPos], Is.EqualTo(movingChild), "movingChild should have been moved");
				Assert.That(rootNode.Children[movingChildOriginalPos], Is.Not.EqualTo(movingChild), "movingChild should not still be in original position");
				Assert.That(rootNode.Children[movingChildOriginalPos], Is.EqualTo(otherChild), "unexpected child in original movingChild position");
				Assert.That(rootNode.Children.Count, Is.EqualTo(2), "unexpected number of reordered siblings");
			}
		}

		/// <summary />
		[Test]
		[TestCase(0, 0, 0, 1)] // move child from 0 down into group with no children
		[TestCase(2, 0, 0, 0)] // move child from 2 up into group with no children
		[TestCase(0, 0, 1, 1)] // move child from 0 down into group with existing child
		[TestCase(2, 1, 1, 0)] // move child from 2 up into group with existing child
		public void Reorder_ChildrenMoveIntoGroupingNodes(int movingChildOriginalPos, int expectedIndexUnderGroup, int groupChildren, int direction)
		{
			var directionToMove = direction == 0 ? Direction.Up : Direction.Down;
			using (var view = new TestConfigurableDictionaryView())
			{
				var controller = new DictionaryConfigurationController { View = view, _model = _model };
				var rootNode = new ConfigurableDictionaryNode { Label = "root", Children = new List<ConfigurableDictionaryNode>() };
				AddChildrenToNode(rootNode, 2);
				var groupNode = AddGroupingNodeToNode(rootNode, 1, groupChildren);
				var movingChild = rootNode.Children[movingChildOriginalPos];
				_model.Parts = new List<ConfigurableDictionaryNode> { rootNode };
				// SUT
				controller.Reorder(movingChild, directionToMove);
				Assert.AreEqual(1 + groupChildren, groupNode.Children.Count, "child not moved under the grouping node");
				Assert.That(groupNode.Children[expectedIndexUnderGroup], Is.EqualTo(movingChild), "movingChild should have been moved");
				Assert.AreEqual(2, rootNode.Children.Count, "movingChild should not still be under original parent");
				Assert.AreEqual(movingChild.Parent, groupNode, "moved child did not have its parent updated");
			}
		}

		/// <summary />
		[Test]
		[TestCase(0, 0, 0)] // move child from group index 0 up above the group
		[TestCase(1, 1, 1)] // move child from group index 1 down below the group
		public void Reorder_ChildrenMoveOutOfGroupingNodes(int movingChildOriginalPos, int expectedIndexUnderParent, int direction)
		{
			var directionToMove = direction == 0 ? Direction.Up : Direction.Down;

			using (var view = new TestConfigurableDictionaryView())
			{
				var controller = new DictionaryConfigurationController { View = view, _model = _model };
				var rootNode = new ConfigurableDictionaryNode { Label = "root", Children = new List<ConfigurableDictionaryNode>() };
				var groupNode = AddGroupingNodeToNode(rootNode, 0, 2); // add two children under the group
				var movingChild = groupNode.Children[movingChildOriginalPos];
				_model.Parts = new List<ConfigurableDictionaryNode> { rootNode };
				// SUT
				controller.Reorder(movingChild, directionToMove);
				Assert.AreEqual(2, rootNode.Children.Count, "child not moved out of the grouping node");
				Assert.That(rootNode.Children[expectedIndexUnderParent], Is.EqualTo(movingChild), "movingChild should have been moved");
				Assert.AreEqual(1, groupNode.Children.Count, "movingChild should not still be under the grouping node");
				Assert.AreEqual(movingChild.Parent, rootNode, "moved child did not have its parent updated");
			}
		}

		/// <summary />
		[Test]
		public void Reorder_GroupWontMoveIntoGroupingNodes([Values(0, 1)]int direction)
		{
			var directionToMove = direction == 0 ? Direction.Up : Direction.Down;
			using (var view = new TestConfigurableDictionaryView())
			{
				var controller = new DictionaryConfigurationController { View = view, _model = _model };
				var rootNode = new ConfigurableDictionaryNode { Label = "root", Children = new List<ConfigurableDictionaryNode>() };
				AddGroupingNodeToNode(rootNode, 0, 0);
				var middleGroupNode = AddGroupingNodeToNode(rootNode, 1, 0);
				AddGroupingNodeToNode(rootNode, 2, 0);
				_model.Parts = new List<ConfigurableDictionaryNode> { rootNode };
				// SUT
				controller.Reorder(middleGroupNode, directionToMove);
				Assert.AreEqual(3, rootNode.Children.Count, "Root has too few children, group must have moved into a group");
			}
		}

		[Test]
		public void GetProjectConfigLocationForPath_AlreadyProjectLocNoChange()
		{
			using (var testView = new TestConfigurableDictionaryView())
			{
				_propertyTable.SetProperty(AreaServices.ToolChoice, AreaServices.ReversalEditCompleteMachineName);
				var projectPath = Path.Combine(LcmFileHelper.GetConfigSettingsDir(Cache.ProjectId.ProjectFolder), "Test", "test" + ReversalIndexServices.ConfigFileExtension);
				//SUT
				var controller = new DictionaryConfigurationController {View = testView, _model = _model };
				controller.InitializeFlexComponent(_flexComponentParameters);
				var result = controller.GetProjectConfigLocationForPath(projectPath);
				Assert.AreEqual(result, projectPath);
			}
		}

		[Test]
		public void GetProjectConfigLocationForPath_DefaultLocResultsInProjectPath()
		{
			var defaultPath = string.Concat(Path.Combine(Path.Combine(FwDirectoryFinder.DefaultConfigurations, "Test"), "test"), ReversalIndexServices.ConfigFileExtension);
			using (var testView = new TestConfigurableDictionaryView())
			{
				_propertyTable.SetProperty(AreaServices.ToolChoice, AreaServices.ReversalEditCompleteMachineName);
				//SUT
				var controller = new DictionaryConfigurationController { View = testView, _model = _model };
				controller.InitializeFlexComponent(_flexComponentParameters);
				Assert.IsFalse(defaultPath.StartsWith(LcmFileHelper.GetConfigSettingsDir(Cache.ProjectId.ProjectFolder)));
				var result = controller.GetProjectConfigLocationForPath(defaultPath);
				Assert.IsTrue(result.StartsWith(LcmFileHelper.GetConfigSettingsDir(Cache.ProjectId.ProjectFolder)));
				Assert.IsTrue(result.EndsWith(string.Concat(Path.Combine("Test", "test"), ReversalIndexServices.ConfigFileExtension)));
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
			using (new CustomFieldForTest(Cache, "CustomString", Cache.MetaDataCacheAccessor.GetClassId("LexEntry"), AnalysisWsId, CellarPropertyType.MultiString, Guid.Empty))
			{
				var customFieldNodes = DictionaryConfigurationController.GetCustomFieldsForType(Cache, "LexEntry");
				CollectionAssert.IsNotEmpty(customFieldNodes);
				Assert.IsTrue(customFieldNodes[0].Label == "CustomString");
			}
		}

		[Test]
		public void GetCustomFieldsForType_PossibilityListFieldGetsChildren()
		{
			using (new CustomFieldForTest(Cache, "CustomListItem", Cache.MetaDataCacheAccessor.GetClassId("LexEntry"), 0, CellarPropertyType.OwningAtomic, Cache.LanguageProject.LocationsOA.Guid))
			{
				var customFieldNodes = DictionaryConfigurationController.GetCustomFieldsForType(Cache, "LexEntry");
				CollectionAssert.IsNotEmpty(customFieldNodes, "The custom field configuration node was not inserted for a PossibilityListReference");
				Assert.AreEqual(customFieldNodes[0].Label, "CustomListItem", "Custom field did not get inserted correctly.");
				var cfChildren = customFieldNodes[0].Children;
				CollectionAssert.IsNotEmpty(cfChildren, "ListItem Child nodes not created");
				Assert.AreEqual(2, cfChildren.Count, "custom list type nodes should get a child for Name and Abbreviation");
				Assert.IsNullOrEmpty(cfChildren[0].After, "Child nodes should have no After space");
				CollectionAssert.IsNotEmpty(cfChildren.Where(t => t.Label == "Name" && !t.IsCustomField), "No standard Name node found on custom possibility list reference");
				CollectionAssert.IsNotEmpty(cfChildren.Where(t => t.Label == "Abbreviation" && !t.IsCustomField), "No standard Abbreviation node found on custom possibility list reference");
				var wsOptions = cfChildren[0].DictionaryNodeOptions as DictionaryNodeWritingSystemOptions;
				Assert.IsNotNull(wsOptions, "No writing system node on possibility list custom node");
				CollectionAssert.IsNotEmpty(wsOptions.Options.Where(o => o.IsEnabled), "No default writing system added.");
			}
		}

		[Test]
		public void GetCustomFieldsForType_SenseCustomFieldIsRepresented()
		{
			using (new CustomFieldForTest(Cache, "CustomCollection", Cache.MetaDataCacheAccessor.GetClassId("LexSense"), 0, CellarPropertyType.ReferenceCollection, Guid.Empty))
			{
				var customFieldNodes = DictionaryConfigurationController.GetCustomFieldsForType(Cache, "LexSense");
				CollectionAssert.IsNotEmpty(customFieldNodes);
				Assert.IsTrue(customFieldNodes[0].Label == "CustomCollection");
			}
		}

		[Test]
		public void GetCustomFieldsForType_MorphCustomFieldIsRepresented()
		{
			using (new CustomFieldForTest(Cache, "CustomCollection", Cache.MetaDataCacheAccessor.GetClassId("MoForm"), 0, CellarPropertyType.ReferenceCollection, Guid.Empty))
			{
				var customFieldNodes = DictionaryConfigurationController.GetCustomFieldsForType(Cache, "MoForm");
				CollectionAssert.IsNotEmpty(customFieldNodes);
				Assert.IsTrue(customFieldNodes[0].Label == "CustomCollection");
			}
		}

		[Test]
		public void GetCustomFieldsForType_ExampleCustomFieldIsRepresented()
		{
			using (new CustomFieldForTest(Cache, "CustomCollection", Cache.MetaDataCacheAccessor.GetClassId("LexExampleSentence"), 0, CellarPropertyType.ReferenceCollection, Guid.Empty))
			{
				var customFieldNodes = DictionaryConfigurationController.GetCustomFieldsForType(Cache, "LexExampleSentence");
				CollectionAssert.IsNotEmpty(customFieldNodes);
				Assert.IsTrue(customFieldNodes[0].Label == "CustomCollection");
			}
		}

		[Test]
		public void GetCustomFieldsForType_MultipleFieldsAreReturned()
		{
			using (new CustomFieldForTest(Cache, "CustomCollection", Cache.MetaDataCacheAccessor.GetClassId("LexSense"), 0, CellarPropertyType.ReferenceCollection, Guid.Empty))
			using (new CustomFieldForTest(Cache, "CustomString", Cache.MetaDataCacheAccessor.GetClassId("LexSense"), 0, CellarPropertyType.ReferenceCollection, Guid.Empty))
			{
				var customFieldNodes = DictionaryConfigurationController.GetCustomFieldsForType(Cache, "LexSense");
				CollectionAssert.IsNotEmpty(customFieldNodes);
				CollectionAssert.AllItemsAreUnique(customFieldNodes);
				Assert.IsTrue(customFieldNodes.Count == 2, "Incorrect number of nodes created from the custom fields.");
				Assert.IsTrue(customFieldNodes[0].Label == "CustomCollection");
				Assert.IsTrue(customFieldNodes[1].Label == "CustomString");
			}
		}

		[Test]
		public void GetCustomFieldsForType_SenseOrEntry()
		{
			using (new CustomFieldForTest(Cache, "CustomCollection", Cache.MetaDataCacheAccessor.GetClassId("LexSense"), 0, CellarPropertyType.ReferenceCollection, Guid.Empty))
			using (new CustomFieldForTest(Cache, "CustomString", Cache.MetaDataCacheAccessor.GetClassId("LexEntry"), 0, CellarPropertyType.ReferenceCollection, Guid.Empty))
			{
				var customFieldNodes = DictionaryConfigurationController.GetCustomFieldsForType(Cache, "SenseOrEntry");
				Assert.AreEqual(customFieldNodes, DictionaryConfigurationController.GetCustomFieldsForType(Cache, "ISenseOrEntry"));
				CollectionAssert.IsNotEmpty(customFieldNodes);
				CollectionAssert.AllItemsAreUnique(customFieldNodes);
				Assert.IsTrue(customFieldNodes.Count == 2, "Incorrect number of nodes created from the custom fields.");
				Assert.IsTrue(customFieldNodes[0].Label == "CustomCollection");
				Assert.IsTrue(customFieldNodes[1].Label == "CustomString");
			}
		}

		[Test]
		public void GetCustomFieldsForType_InterfacesAndReferencesAreAliased()
		{
			using (new CustomFieldForTest(Cache, "CustomString", Cache.MetaDataCacheAccessor.GetClassId("LexEntry"), AnalysisWsId, CellarPropertyType.MultiString, Guid.Empty))
			{
				var customFieldNodes = DictionaryConfigurationController.GetCustomFieldsForType(Cache, "ILexEntry");
				CollectionAssert.IsNotEmpty(customFieldNodes);
				Assert.IsTrue(customFieldNodes[0].Label == "CustomString");
				customFieldNodes = DictionaryConfigurationController.GetCustomFieldsForType(Cache, "LexEntryRef");
				Assert.AreEqual(customFieldNodes, DictionaryConfigurationController.GetCustomFieldsForType(Cache, "ILexEntryRef"));
				CollectionAssert.IsNotEmpty(customFieldNodes);
				Assert.IsTrue(customFieldNodes[0].Label == "CustomString");
			}
		}

		/// <summary>
		/// Ensure the string that displays the publications associated with the current dictionary configuration is correct.
		/// </summary>
		[Test]
		public void GetThePublicationsForTheCurrentConfiguration()
		{
			var controller = new DictionaryConfigurationController { _model = _model };
			//ensure this is handled gracefully when the publications have not been initialized.
			Assert.AreEqual(controller.AffectedPublications, DictionaryConfigurationStrings.ksNone1);
			_model.Publications = new List<string> { "A" };
			Assert.AreEqual(controller.AffectedPublications, "A");
			_model.Publications = new List<string> { "A", "B" };
			Assert.AreEqual(controller.AffectedPublications, "A, B");
		}

		[Test]
		public void DisplaysAllPublicationsIfSet()
		{
			var controller = new DictionaryConfigurationController { _model = _model };
			_model.Publications = new List<string> { "A", "B" };
			_model.AllPublications = true;
			Assert.That(controller.AffectedPublications, Is.EqualTo("All publications"), "Show that it's all-publications if so.");
		}

		[Test]
		public void MergeCustomFieldsIntoDictionaryModel_NewFieldsAreAdded()
		{
			using (new CustomFieldForTest(Cache, "CustomString", Cache.MetaDataCacheAccessor.GetClassId("LexEntry"), WritingSystemServices.GetMagicWsIdFromName("analysis vernacular"), CellarPropertyType.MultiString, Guid.Empty))
			{
				var model = new DictionaryConfigurationModel
				{
					Parts = new List<ConfigurableDictionaryNode>
					{
						new ConfigurableDictionaryNode { Label = "Main Entry", FieldDescription = "LexEntry" }
					}
				};
				//SUT
				DictionaryConfigurationController.MergeCustomFieldsIntoDictionaryModel(model, Cache);
				var children = model.Parts[0].Children;
				Assert.IsNotNull(children, "Custom Field did not add to children");
				CollectionAssert.IsNotEmpty(children, "Custom Field did not add to children");
				var cfNode = children[0];
				Assert.AreEqual(cfNode.Label, "CustomString");
				Assert.AreEqual(cfNode.FieldDescription, "CustomString");
				Assert.AreEqual(cfNode.IsCustomField, true);
				Assert.AreSame(model.Parts[0], cfNode.Parent, "improper Parent set");
				var wsOptions = cfNode.DictionaryNodeOptions as DictionaryNodeWritingSystemOptions;
				Assert.NotNull(wsOptions, "WritingSystemOptions not added");
				Assert.AreEqual(wsOptions.WsType, WritingSystemType.Both, "WritingSystemOptions is the wrong type");
				CollectionAssert.IsNotEmpty(wsOptions.Options.Where(o => o.IsEnabled), "WsOptions not populated with any choices");
			}
		}

		[Test]
		public void MergeCustomFieldsIntoDictionaryModel_FieldsAreNotDuplicated()
		{
			using (new CustomFieldForTest(Cache, "CustomString", Cache.MetaDataCacheAccessor.GetClassId("LexEntry"), AnalysisWsId, CellarPropertyType.ReferenceCollection, Guid.Empty))
			{
				var model = new DictionaryConfigurationModel();
				var customNode = new ConfigurableDictionaryNode()
				{
					Label = "CustomString",
					FieldDescription = "CustomString",
					IsCustomField = true,
					DictionaryNodeOptions = new DictionaryNodeWritingSystemOptions
					{
						DisplayWritingSystemAbbreviations = true,
						Options = new List<DictionaryNodeOption>
						{
							new DictionaryNodeOption() { Id = "en", IsEnabled = true }
						}
					}
				};
				var entryNode = new ConfigurableDictionaryNode
				{
					Label = "Main Entry",
					FieldDescription = "LexEntry",
					Children = new List<ConfigurableDictionaryNode> { customNode }
				};
				model.Parts = new List<ConfigurableDictionaryNode> { entryNode };
				CssGeneratorTests.PopulateFieldsForTesting(model);
				//SUT
				DictionaryConfigurationController.MergeCustomFieldsIntoDictionaryModel(model, Cache);
				Assert.AreEqual(1, model.Parts[0].Children.Count, "Only the existing custom field node should be present");
				var wsOptions = model.Parts[0].Children[0].DictionaryNodeOptions as DictionaryNodeWritingSystemOptions;
				Assert.NotNull(wsOptions, "Writing system options lost in merge");
				Assert.IsTrue(wsOptions.DisplayWritingSystemAbbreviations, "WsAbbreviation lost in merge");
				Assert.AreEqual("en", wsOptions.Options[0].Id);
				Assert.IsTrue(wsOptions.Options[0].IsEnabled, "Selected writing system lost in merge");
			}
		}

		[Test]
		public void UpdateWsOptions_OrderAndCheckMaintained()
		{
			CoreWritingSystemDefinition wsEs;
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("es", out wsEs);
			Cache.ServiceLocator.WritingSystems.AddToCurrentVernacularWritingSystems(wsEs);
			var model = new DictionaryConfigurationModel();
			var customNode = new ConfigurableDictionaryNode()
			{
				Label = "CustomString",
				FieldDescription = "CustomString",
				IsCustomField = true,
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguageswithDisplayWsAbbrev(
					new[] { "ch", "fr", "en" }, WritingSystemType.Both)
			};
			var entryNode = new ConfigurableDictionaryNode
			{
				Label = "Main Entry",
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { customNode }
			};
			model.Parts = new List<ConfigurableDictionaryNode> { entryNode };
			CssGeneratorTests.PopulateFieldsForTesting(model);
			//SUT
			DictionaryConfigurationController.UpdateWsOptions((DictionaryNodeWritingSystemOptions)customNode.DictionaryNodeOptions, Cache);
			Assert.AreEqual(1, model.Parts[0].Children.Count, "Only the existing custom field node should be present");
			var wsOptions = model.Parts[0].Children[0].DictionaryNodeOptions as DictionaryNodeWritingSystemOptions;
			Assert.NotNull(wsOptions, "Writing system options lost in merge");
			Assert.IsTrue(wsOptions.DisplayWritingSystemAbbreviations, "WsAbbreviation lost in merge");
			Assert.AreEqual("fr", wsOptions.Options[0].Id, "Writing system not removed, or order not maintained");
			Assert.IsTrue(wsOptions.Options[0].IsEnabled, "Selected writing system lost in merge");
			Assert.AreEqual("en", wsOptions.Options[1].Id);
			Assert.IsTrue(wsOptions.Options[1].IsEnabled, "Selected writing system lost in merge");
			Assert.AreEqual("es", wsOptions.Options[2].Id, "New writing system was not added");
		}

		[Test]
		public void UpdateWsOptions_ChecksAtLeastOne()
		{
			var model = new DictionaryConfigurationModel();
			var customNode = new ConfigurableDictionaryNode()
			{
				Label = "CustomString",
				FieldDescription = "CustomString",
				IsCustomField = true,
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new string[0]) // start without any WS's
			};
			var entryNode = new ConfigurableDictionaryNode
			{
				Label = "Main Entry",
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { customNode }
			};
			model.Parts = new List<ConfigurableDictionaryNode> { entryNode };
			CssGeneratorTests.PopulateFieldsForTesting(model);
			//SUT
			DictionaryConfigurationController.UpdateWsOptions((DictionaryNodeWritingSystemOptions)customNode.DictionaryNodeOptions, Cache);
			var wsOptions = (DictionaryNodeWritingSystemOptions)model.Parts[0].Children[0].DictionaryNodeOptions;
			Assert.IsTrue(wsOptions.Options.Any(ws => ws.IsEnabled), "At least one WS should be enabled");
		}

		[Test]
		public void MergeCustomFieldsIntoDictionaryModel_NewFieldsOnSharedNodesAreAddedToSharedItemsExclusively()
		{
			// "Shared Shared" node tests that Custom Fields are merged into SharedItems whose (sharing) parents are themselves under SharedItems
			var sharedsharedSubsubsNode = new ConfigurableDictionaryNode
			{
				Label = "SharedsharedSubsubs",
				FieldDescription = "Subentries"
			};
			var subSubsNode = new ConfigurableDictionaryNode
			{
				Label = "Subsubs",
				FieldDescription = "Subentries",
				ReferenceItem = "SharedsharedSubsubs",
				Children = new List<ConfigurableDictionaryNode>()
			};
			var sharedSubsNode = new ConfigurableDictionaryNode
			{
				Label = "SharedSubs",
				FieldDescription = "Subentries",
				Children = new List<ConfigurableDictionaryNode> { subSubsNode }
			};
			var masterParentSubsNode = new ConfigurableDictionaryNode
			{
				Label = "Subs",
				FieldDescription = "Subentries",
				ReferenceItem = "SharedSubs",
				Children = new List<ConfigurableDictionaryNode>()
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Label = "Main Entry",
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { masterParentSubsNode }
			};
			var model = new DictionaryConfigurationModel
			{
				Parts = new List<ConfigurableDictionaryNode> { mainEntryNode },
				SharedItems = new List<ConfigurableDictionaryNode> { sharedSubsNode, sharedsharedSubsubsNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(model);
			using (new CustomFieldForTest(Cache, "CustomString", Cache.MetaDataCacheAccessor.GetClassId("LexEntry"), 0, CellarPropertyType.ReferenceCollection, Guid.Empty))
			{
				//SUT
				DictionaryConfigurationController.MergeCustomFieldsIntoDictionaryModel(model, Cache);
				Assert.AreSame(masterParentSubsNode, model.Parts[0].Children[0], "Custom Field should be added at the end");
				Assert.IsEmpty(masterParentSubsNode.Children, "Custom Field should not have been added to the Referring Node");
				Assert.AreSame(subSubsNode, sharedSubsNode.Children[0], "Custom Field should be added at the end");
				Assert.IsEmpty(subSubsNode.Children, "Custom Field should not have been added to the Referring Node");
				Assert.AreEqual(2, sharedSubsNode.Children.Count, "Custom Field was not added to Subentries");
				var customNode = sharedSubsNode.Children[1];
				Assert.AreEqual(customNode.Label, "CustomString");
				Assert.AreEqual(customNode.FieldDescription, "CustomString");
				Assert.AreEqual(customNode.IsCustomField, true);
				Assert.AreSame(sharedSubsNode, customNode.Parent, "improper Parent set");
				// Validate double-shared node:
				Assert.NotNull(sharedsharedSubsubsNode.Children, "Shared shared Subsubs should have children");
				Assert.AreEqual(1, sharedsharedSubsubsNode.Children.Count, "One child: the Custom Field");
				customNode = sharedsharedSubsubsNode.Children[0];
				Assert.AreEqual(customNode.Label, "CustomString");
				Assert.AreEqual(customNode.FieldDescription, "CustomString");
				Assert.AreEqual(customNode.IsCustomField, true);
				Assert.AreSame(sharedsharedSubsubsNode, customNode.Parent, "improper Parent set");
			}
		}

		[Test]
		public void MergeCustomFieldsIntoDictionaryModel_WorksUnderGroupingNodes()
		{
			using (new CustomFieldForTest(Cache, "CustomString", Cache.MetaDataCacheAccessor.GetClassId("LexEntry"), WritingSystemServices.GetMagicWsIdFromName("analysis vernacular"), CellarPropertyType.MultiString, Guid.Empty))
			{
				var model = new DictionaryConfigurationModel
				{
					Parts = new List<ConfigurableDictionaryNode>
					{
						new ConfigurableDictionaryNode
						{
							Label = "Main Entry", FieldDescription = "LexEntry",
							Children = new List<ConfigurableDictionaryNode>
							{
								new ConfigurableDictionaryNode
								{
									Label = "Grouping Node", FieldDescription = "Group",
									DictionaryNodeOptions = new DictionaryNodeGroupingOptions(),
									Children = new List<ConfigurableDictionaryNode>
									{
										new ConfigurableDictionaryNode { Label = "CustomString", FieldDescription = "CustomString", IsCustomField = true },
										new ConfigurableDictionaryNode { Label = "OldCustomField", FieldDescription = "OldCustomField", IsCustomField = true }
									}
								}
							}
						}
					}
				};
				CssGeneratorTests.PopulateFieldsForTesting(model);
				//SUT
				DictionaryConfigurationController.MergeCustomFieldsIntoDictionaryModel(model, Cache);
				var children = model.Parts[0].Children;
				Assert.AreEqual(1, children.Count, "The only node under Main Entry should be Grouping Node (the Custom Field already under Grouping Node should not be dup'd under ME");
				var group = children[0];
				children = group.Children;
				Assert.IsNotNull(children, "GroupingNode should still have children");
				Assert.AreEqual(1, children.Count, "One CF under Grouping Node should have been retained, the other deleted");
				var customNode = children[0];
				Assert.AreEqual("CustomString", customNode.Label);
				Assert.AreEqual("CustomString", customNode.FieldDescription);
				Assert.True(customNode.IsCustomField);
				Assert.AreSame(group, customNode.Parent, "improper Parent set");
			}
		}

		[Test]
		public void MergeCustomFieldsIntoDictionaryModel_DeletedFieldsAreRemoved()
		{
			var model = new DictionaryConfigurationModel();
			var customNode = new ConfigurableDictionaryNode
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
			DictionaryConfigurationController.MergeCustomFieldsIntoDictionaryModel(model, Cache);
			Assert.AreEqual(0, model.Parts[0].Children.Count, "The custom field in the model should have been removed since it isn't in the project(cache)");
		}

		[Test]
		public void MergeCustomFieldsIntoDictionaryModel_DuplicateCustomFieldsAreNotRemoved()
		{
			using (new CustomFieldForTest(Cache, "CustomString", Cache.MetaDataCacheAccessor.GetClassId("LexSense"), -1, CellarPropertyType.String, Guid.Empty))
			{
				var model = new DictionaryConfigurationModel();
				var customNode = new ConfigurableDictionaryNode { FieldDescription = "CustomString", IsCustomField = true };
				var duplicateCustomNode = new ConfigurableDictionaryNode { Label = "CustomString", LabelSuffix = "1", FieldDescription = "CustomString", IsCustomField = true, IsDuplicate = true };
				var sensesNode = new ConfigurableDictionaryNode
				{
					Label = "Senses",
					FieldDescription = "SensesOS",
					Children = new List<ConfigurableDictionaryNode> { customNode, duplicateCustomNode }
				};
				var entryNode = new ConfigurableDictionaryNode
				{
					Label = "Main Entry",
					FieldDescription = "LexEntry",
					Children = new List<ConfigurableDictionaryNode> { sensesNode }
				};
				model.Parts = new List<ConfigurableDictionaryNode> { entryNode };
				CssGeneratorTests.PopulateFieldsForTesting(model);
				//SUT
				DictionaryConfigurationController.MergeCustomFieldsIntoDictionaryModel(model, Cache);
				Assert.AreEqual(2, model.Parts[0].Children[0].Children.Count, "The Duplicate custom field should be retained");
			}
		}

		[Test]
		public void MergeCustomFieldsIntoDictionaryModel_DeletedFieldsOnCollectionsAreRemoved()
		{
			var model = new DictionaryConfigurationModel();
			var customNode = new ConfigurableDictionaryNode { FieldDescription = "CustomString", IsCustomField = true };
			var sensesNode = new ConfigurableDictionaryNode
			{
				Label = "Senses",
				FieldDescription = "SensesOS",
				Children = new List<ConfigurableDictionaryNode> { customNode }
			};
			var entryNode = new ConfigurableDictionaryNode
			{
				Label = "Main Entry",
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode }
			};
			model.Parts = new List<ConfigurableDictionaryNode> { entryNode };
			CssGeneratorTests.PopulateFieldsForTesting(model);
			//SUT
			DictionaryConfigurationController.MergeCustomFieldsIntoDictionaryModel(model, Cache);
			Assert.AreEqual(0, model.Parts[0].Children[0].Children.Count, "The custom field in the model should have been removed since it isn't in the project(cache)");
		}

		[Test]
		public void MergecustomFieldsIntoModel_RefTypesUseOwningEntry()
		{
			var variantFormsNode = new ConfigurableDictionaryNode { FieldDescription = "VariantFormEntryBackRefs" };
			var entryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { variantFormsNode }
			};
			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { entryNode } };
			CssGeneratorTests.PopulateFieldsForTesting(model);
			using (new CustomFieldForTest(Cache, "CustomCollection", Cache.MetaDataCacheAccessor.GetClassId("LexEntry"), 0,
				CellarPropertyType.ReferenceCollection, Guid.Empty))
			{
				DictionaryConfigurationController.MergeCustomFieldsIntoDictionaryModel(model, Cache); // SUT
				Assert.AreEqual(1, variantFormsNode.Children.Count);
				var customNode = variantFormsNode.Children[0];
				Assert.AreEqual("OwningEntry", customNode.FieldDescription);
				Assert.AreEqual("CustomCollection", customNode.SubField);
			}
		}

		[Test]
		public void MergeCustomFieldsIntoDictionaryModel_ExampleCustomFieldIsRepresented()
		{
			using (new CustomFieldForTest(Cache, "CustomCollection", Cache.MetaDataCacheAccessor.GetClassId("LexExampleSentence"), 0, CellarPropertyType.ReferenceCollection, Guid.Empty))
			{
				var examplesNode = new ConfigurableDictionaryNode
				{
					Label = "Example Sentences",
					FieldDescription = "ExamplesOS"
				};
				var subsensesNode = new ConfigurableDictionaryNode
				{
					Label = "Subsenses",
					FieldDescription = "SensesOS",
					ReferenceItem = "SharedSenses"
				};
				var senseNode = new ConfigurableDictionaryNode
				{
					Label = "Senses",
					FieldDescription = "SensesOS",
					Children = new List<ConfigurableDictionaryNode> { examplesNode, subsensesNode }
				};
				var entryNode = new ConfigurableDictionaryNode
				{
					Label = "Main Entry",
					FieldDescription = "LexEntry",
					Children = new List<ConfigurableDictionaryNode> { senseNode }
				};
				var sharedExamplesNode = examplesNode.DeepCloneUnderSameParent();
				var subsensesSharedItem = new ConfigurableDictionaryNode
				{
					Label = "SharedSenses",
					FieldDescription = "SensesOS",
					Children = new List<ConfigurableDictionaryNode> { sharedExamplesNode }
				};
				var model = new DictionaryConfigurationModel
				{
					Parts = new List<ConfigurableDictionaryNode> { entryNode },
					SharedItems = new List<ConfigurableDictionaryNode> { subsensesSharedItem }
				};
				CssGeneratorTests.PopulateFieldsForTesting(model);
				//SUT
				DictionaryConfigurationController.MergeCustomFieldsIntoDictionaryModel(model, Cache);
				Assert.AreEqual(1, examplesNode.Children.Count, "Custom field should have been added to ExampleSentence");
				Assert.AreEqual(1, sharedExamplesNode.Children.Count, "Custom field should have been added to shared ExampleSentence");
			}
		}

		[Test]
		public void MergeCustomFieldsIntoModel_MergeWithDefaultRootModelDoesNotThrow()
		{
			using (new CustomFieldForTest(Cache, "CustomCollection", Cache.MetaDataCacheAccessor.GetClassId("LexExampleSentence"), 0, CellarPropertyType.ReferenceCollection, Guid.Empty))
			{
				var model = new DictionaryConfigurationModel(string.Concat(Path.Combine(FwDirectoryFinder.DefaultConfigurations, "Dictionary", "Root"), LanguageExplorerConstants.DictionaryConfigurationFileExtension), Cache);
				//SUT
				Assert.DoesNotThrow(() => DictionaryConfigurationController.MergeCustomFieldsIntoDictionaryModel(model, Cache));
			}
		}

		[Test]
		public void GetDefaultEntryForType_ReturnsNullWhenNoLexEntriesForDictionary()
		{
			//make sure cache has no LexEntry objects
			Assert.True(!Cache.ServiceLocator.GetInstance<ILexEntryRepository>().AllInstances().Any());
			// SUT
			Assert.IsNull(DictionaryConfigurationController.GetDefaultEntryForType("Dictionary", Cache));
		}

		[Test]
		public void GetDefaultEntryForType_ReturnsEntryWithoutHeadwordIfNoItemsHaveHeadwordsForDictionary()
		{
			var entryWithoutHeadword = CreateLexEntryWithoutHeadword();
			// SUT
			Assert.AreEqual(DictionaryConfigurationController.GetDefaultEntryForType("Dictionary", Cache), entryWithoutHeadword);
		}

		[Test]
		public void GetDefaultEntryForType_ReturnsFirstItemWithHeadword()
		{
			CreateLexEntryWithoutHeadword();
			var entryWithHeadword = CreateLexEntryWithHeadword();
			// SUT
			Assert.AreEqual(DictionaryConfigurationController.GetDefaultEntryForType("Dictionary", Cache), entryWithHeadword);
		}

		[Test]
		public void EnableNodeAndDescendants_EnablesNodeWithNoChildren()
		{
			var node = new ConfigurableDictionaryNode { IsEnabled = false };
			Assert.DoesNotThrow(() => DictionaryConfigurationController.EnableNodeAndDescendants(node));
			Assert.IsTrue(node.IsEnabled);
		}

		[Test]
		public void DisableNodeAndDescendants_UnchecksNodeWithNoChildren()
		{
			var node = new ConfigurableDictionaryNode { IsEnabled = true };
			Assert.DoesNotThrow(() => DictionaryConfigurationController.DisableNodeAndDescendants(node));
			Assert.IsFalse(node.IsEnabled);
		}

		[Test]
		public void EnableNodeAndDescendants_ChecksToGrandChildren()
		{
			var grandchild = new ConfigurableDictionaryNode { IsEnabled = false };
			var child = new ConfigurableDictionaryNode { IsEnabled = false, Children = new List<ConfigurableDictionaryNode> { grandchild } };
			var node = new ConfigurableDictionaryNode { IsEnabled = false, Children = new List<ConfigurableDictionaryNode> { child } };
			Assert.DoesNotThrow(() => DictionaryConfigurationController.EnableNodeAndDescendants(node));
			Assert.IsTrue(node.IsEnabled);
			Assert.IsTrue(child.IsEnabled);
			Assert.IsTrue(grandchild.IsEnabled);
		}

		[Test]
		public void DisableNodeAndDescendants_UnChecksGrandChildren()
		{
			var grandchild = new ConfigurableDictionaryNode { IsEnabled = true };
			var child = new ConfigurableDictionaryNode { IsEnabled = true, Children = new List<ConfigurableDictionaryNode> { grandchild } };
			var node = new ConfigurableDictionaryNode { IsEnabled = true, Children = new List<ConfigurableDictionaryNode> { child } };
			Assert.DoesNotThrow(() => DictionaryConfigurationController.DisableNodeAndDescendants(node));
			Assert.IsFalse(node.IsEnabled);
			Assert.IsFalse(child.IsEnabled);
			Assert.IsFalse(grandchild.IsEnabled);
		}

#if RANDYTODO
		// TODO: Test fails for some reason.
		[Test]
		public void SaveModelHandler_SavesUpdatedFilePath() // LT-15898
		{
			using (var testView = new TestConfigurableDictionaryView())
			{
				FileUtils.EnsureDirectoryExists(DictionaryConfigurationServices.GetProjectConfigurationDirectory(Cache, "Dictionary"));
				var controller = new DictionaryConfigurationController
				{
					_model = new DictionaryConfigurationModel
					{
						FilePath = Path.Combine(DictionaryConfigurationServices.GetDefaultConfigurationDirectory("Dictionary"), "SomeConfigurationFileName")
					},
					View = testView
				};
				_propertyTable.SetProperty(AreaServices.ToolChoice, AreaServices.LexiconDictionaryMachineName);
				controller._dictionaryConfigurations = new List<DictionaryConfigurationModel> { controller._model };
				controller.InitializeFlexComponent(_flexComponentParameters);
				// SUT
				controller.SaveModel();
				var savedPath = _flexComponentParameters.PropertyTable.GetValue<string>("DictionaryPublicationLayout", null);
				var projectConfigsPath = LcmFileHelper.GetConfigSettingsDir(Cache.ProjectId.ProjectFolder);
				Assert.AreEqual(controller._model.FilePath, savedPath, "Should have saved the path to the selected Configuration Model");
				StringAssert.StartsWith(projectConfigsPath, savedPath, "Path should be in the project's folder");
				StringAssert.EndsWith("SomeConfigurationFileName", savedPath, "Incorrect configuration saved");
				DeleteConfigurationTestModelFiles(controller);
			}
		}
#endif

		private ILexEntry CreateLexEntryWithoutHeadword()
		{
			return Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
		}

		private ILexEntry CreateLexEntryWithHeadword()
		{
			var entryWithHeadword = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			var wsFr = Cache.WritingSystemFactory.GetWsFromStr("fr");
			entryWithHeadword.CitationForm.set_String(wsFr, TsStringUtils.MakeString("Headword", wsFr));
			return entryWithHeadword;
		}

		[Test]
		public void PopulateTreeView_NewProjectDoesNotCrash_DoesNotGeneratesContent()
		{
			var formNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ReversalForm",
				Label = "Form",
				DictionaryNodeOptions = new DictionaryNodeWritingSystemOptions
				{
					WsType = WritingSystemType.Reversal,
					Options = new List<DictionaryNodeOption>
					{
						new DictionaryNodeOption { Id = "en", IsEnabled = true,}
					},
					DisplayWritingSystemAbbreviations = false
				}
			};
			var reversalNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { formNode },
				FieldDescription = "ReversalIndexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(reversalNode);
			using (var testView = new TestConfigurableDictionaryView())
			{
				_model.Parts = new List<ConfigurableDictionaryNode> { reversalNode };
				var dcc = new DictionaryConfigurationController
				{
					View = testView,
					_model = _model,
					_previewEntry = DictionaryConfigurationController.GetDefaultEntryForType("Reversal Index", Cache)
				};
				CreateALexEntry(Cache);
				Assert.AreEqual(0, Cache.LangProject.LexDbOA.ReversalIndexesOC.Count, "Should have not a Reversal Index at this point");
				// But actually a brand new project contains an empty ReversalIndex
				// for the analysisWS, so create one for our test here.
				CreateDefaultReversalIndex();
				//SUT
				dcc.PopulateTreeView();
				Assert.IsNullOrEmpty(testView.PreviewData, "Should not have created a preview");
				Assert.AreEqual(1, Cache.LangProject.LexDbOA.ReversalIndexesOC.Count);
				Assert.AreEqual("en", Cache.LangProject.LexDbOA.ReversalIndexesOC.First().WritingSystem);
			}
		}

		private void CreateDefaultReversalIndex()
		{
			var aWs = Cache.DefaultAnalWs;
			var riRepo = Cache.ServiceLocator.GetInstance<IReversalIndexRepository>();
			riRepo.FindOrCreateIndexForWs(aWs);
		}

		private static void CreateALexEntry(LcmCache cache)
		{
			var factory = cache.ServiceLocator.GetInstance<ILexEntryFactory>();
			var entry = factory.Create();
			var wsEn = cache.WritingSystemFactory.GetWsFromStr("en");
			var wsFr = cache.WritingSystemFactory.GetWsFromStr("fr");
			entry.CitationForm.set_String(wsFr, TsStringUtils.MakeString("mot", wsFr));
			var senseFactory = cache.ServiceLocator.GetInstance<ILexSenseFactory>();
			var sense = senseFactory.Create();
			entry.SensesOS.Add(sense);
			sense.Gloss.set_String(wsEn, TsStringUtils.MakeString("word", wsEn));
		}

		[Test]
		public void MakingAChangeAndSavingSetsRefreshRequiredFlag()
		{
			var headwordNode = new ConfigurableDictionaryNode();
			var entryNode = new ConfigurableDictionaryNode { Children = new List<ConfigurableDictionaryNode> { headwordNode } };
			_model.Parts = new List<ConfigurableDictionaryNode> { entryNode };
			CssGeneratorTests.PopulateFieldsForTesting(_model);
			using (var testView = new TestConfigurableDictionaryView())
			{
				var entryWithHeadword = CreateLexEntryWithHeadword();
				_propertyTable.SetProperty(AreaServices.ToolChoice, AreaServices.LexiconDictionaryMachineName);
				Cache.ProjectId.Path = DictionaryConfigurationServices.TestDataPath;
				var dcc = new DictionaryConfigurationController(testView, entryWithHeadword);
				dcc.InitializeFlexComponent(_flexComponentParameters);
				//SUT
				dcc.View.TreeControl.Tree.TopNode.Checked = false;
				((TestConfigurableDictionaryView)dcc.View).DoSaveModel();
				Assert.IsTrue(dcc.MasterRefreshRequired, "Should have saved changes and required a Master Refresh");
				DeleteConfigurationTestModelFiles(dcc);
			}
		}

		[Test]
		public void MakingAChangeWithoutSavingDoesNotSetRefreshRequiredFlag()
		{
			var headwordNode = new ConfigurableDictionaryNode();
			var entryNode = new ConfigurableDictionaryNode { Children = new List<ConfigurableDictionaryNode> { headwordNode } };
			_model.Parts = new List<ConfigurableDictionaryNode> { entryNode };
			CssGeneratorTests.PopulateFieldsForTesting(_model);
			using (var testView = new TestConfigurableDictionaryView())
			{
				var entryWithHeadword = CreateLexEntryWithHeadword();
				_propertyTable.SetProperty(AreaServices.ToolChoice, AreaServices.LexiconDictionaryMachineName);
				Cache.ProjectId.Path = DictionaryConfigurationServices.TestDataPath;
				var dcc = new DictionaryConfigurationController(testView, entryWithHeadword);
				dcc.InitializeFlexComponent(_flexComponentParameters);
				//SUT
				dcc.View.TreeControl.Tree.TopNode.Checked = false;
				Assert.IsFalse(dcc.MasterRefreshRequired, "Should not have saved changes--user did not click OK or Apply");
				DeleteConfigurationTestModelFiles(dcc);
			}
		}

		[Test]
		public void MakingNoChangeAndSavingDoesNotSetRefreshRequiredFlag()
		{
			var headwordNode = new ConfigurableDictionaryNode();
			var entryNode = new ConfigurableDictionaryNode { Children = new List<ConfigurableDictionaryNode> { headwordNode } };
			_model.Parts = new List<ConfigurableDictionaryNode> { entryNode };
			CssGeneratorTests.PopulateFieldsForTesting(_model);
			using (var testView = new TestConfigurableDictionaryView())
			{
				var entryWithHeadword = CreateLexEntryWithHeadword();
				_propertyTable.SetProperty(AreaServices.ToolChoice, AreaServices.LexiconDictionaryMachineName);
				Cache.ProjectId.Path = DictionaryConfigurationServices.TestDataPath;
				var dcc = new DictionaryConfigurationController(testView, entryWithHeadword);
				dcc.InitializeFlexComponent(_flexComponentParameters);
				//SUT
				((TestConfigurableDictionaryView)dcc.View).DoSaveModel();
				Assert.IsFalse(dcc.MasterRefreshRequired, "Should not have saved changes--none to save");
				DeleteConfigurationTestModelFiles(dcc);
			}
		}

		[Test]
		public void SetStartingNode_SelectsCorrectNode()
		{
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				Label = "Headword",
				CSSClassNameOverride = "mainheadword",
				IsEnabled = true
			};
			var summaryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SummaryDefinition",
				Label = "Summary Definition",
				IsEnabled = false
			};
			var restrictionsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Restrictions",
				Label = "Restrictions (Entry)",
				IsEnabled = true
			};
			var defglossNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "DefinitionOrGloss",
				Label = "Definition (or Gloss)",
				IsEnabled = true
			};
			var exampleNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Example",
				Label = "Example",
				IsEnabled = true
			};
			var typeAbbrNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Abbreviation",
				Label = "Abbreviation",
				IsEnabled = true
			};
			var typeNameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Name",
				Label = "Name",
				IsEnabled = true
			};
			var transTypeNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "TypeRA",
				Label = "Type",
				CSSClassNameOverride = "type",
				IsEnabled = false,
				Children = new List<ConfigurableDictionaryNode> { typeAbbrNode, typeNameNode }
			};
			var translationNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Translation",
				Label = "Translation",
				IsEnabled = true
			};
			var translationsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "TranslationsOC",
				Label = "Translations",
				CSSClassNameOverride = "translations",
				IsEnabled = false,
				Children = new List<ConfigurableDictionaryNode> { transTypeNode, translationNode }
			};
			var referenceNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Reference",
				Label = "Reference",
				IsEnabled = false
			};
			var examplesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ExamplesOS",
				Label = "Examples",
				CSSClassNameOverride = "examples",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { exampleNode, translationsNode, referenceNode }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				Label = "Senses",
				CSSClassNameOverride = "senses",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { defglossNode, examplesNode },
			};
			var entryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Label = "Main Entry",
				CSSClassNameOverride = "entry",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { headwordNode, summaryNode, restrictionsNode, sensesNode },
			};
			_model.Parts = new List<ConfigurableDictionaryNode> { entryNode };
			CssGeneratorTests.PopulateFieldsForTesting(_model);
			using (var testView = new TestConfigurableDictionaryView())
			{
				var dcc = new DictionaryConfigurationController { View = testView, _model = _model };
				dcc.CreateTreeOfTreeNodes(null, _model.Parts);
				//SUT
				var treeNode = dcc.View.TreeControl.Tree.SelectedNode;
				Assert.IsNull(treeNode, "No TreeNode should be selected to start out with");
				dcc.SetStartingNode(null);
				treeNode = dcc.View.TreeControl.Tree.SelectedNode;
				Assert.IsNull(treeNode, "Passing a null class list should not find a TreeNode (and should not crash either)");
				dcc.SetStartingNode(new List<string>());
				treeNode = dcc.View.TreeControl.Tree.SelectedNode;
				Assert.IsNull(treeNode, "Passing an empty class list should not find a TreeNode");
				dcc.SetStartingNode(new List<string> { "something", "invalid" });
				treeNode = dcc.View.TreeControl.Tree.SelectedNode;
				Assert.IsNull(treeNode, "Passing a totally invalid class list should not find a TreeNode");
				dcc.SetStartingNode(new List<string> { "entry", "senses", "sensecontent", "sense", "random", "nonsense" });
				treeNode = dcc.View.TreeControl.Tree.SelectedNode;
				Assert.IsNotNull(treeNode, "Passing a partially valid class list should find a TreeNode");
				Assert.AreSame(sensesNode, treeNode.Tag, "Passing a partially valid class list should find the best node possible");
				// Starting here we need to Unset the controller's SelectedNode to keep from getting false positives
				ClearSelectedNode(dcc);
				dcc.SetStartingNode(new List<string> { "entry", "mainheadword" });
				treeNode = dcc.View.TreeControl.Tree.SelectedNode;
				Assert.IsNotNull(treeNode, "entry/mainheadword should find a TreeNode");
				Assert.AreSame(headwordNode, treeNode.Tag, "entry/mainheadword should find the right TreeNode");
				Assert.AreEqual(headwordNode.Label, treeNode.Text, "The TreeNode for entry/mainheadword should have the right Text");
				ClearSelectedNode(dcc);
				dcc.SetStartingNode(new List<string> { "entry " + DictionaryConfigurationServices.CurrentSelectedEntryClass, "mainheadword" });
				treeNode = dcc.View.TreeControl.Tree.SelectedNode;
				Assert.IsNotNull(treeNode, "entry/mainheadword should find a TreeNode, even if this is the selected entry");
				Assert.AreSame(headwordNode, treeNode.Tag, "entry/mainheadword should find the right TreeNode");
				Assert.AreEqual(headwordNode.Label, treeNode.Text, "The TreeNode for entry/mainheadword should have the right Text");
				ClearSelectedNode(dcc);
				dcc.SetStartingNode(new List<string> { "entry", "senses", "sensecontent", "sense", "definitionorgloss" });
				treeNode = dcc.View.TreeControl.Tree.SelectedNode;
				Assert.IsNotNull(treeNode, "entry//definitionorgloss should find a TreeNode");
				Assert.AreSame(defglossNode, treeNode.Tag, "entry//definitionorgloss should find the right TreeNode");
				Assert.AreEqual(defglossNode.Label, treeNode.Text, "The TreeNode for entry//definitionorgloss should have the right Text");
				ClearSelectedNode(dcc);
				dcc.SetStartingNode(new List<string> { "entry", "senses", "sensecontent", "sense", "examples", "example", "translations", "translation", "translation" });
				treeNode = dcc.View.TreeControl.Tree.SelectedNode;
				Assert.IsNotNull(treeNode, "entry//translation should find a TreeNode");
				Assert.AreSame(translationNode, treeNode.Tag, "entry//translation should find the right TreeNode");
				Assert.AreEqual(translationNode.Label, treeNode.Text, "The TreeNode for entry//translation should have the right Text");
			}
		}

		private static void ClearSelectedNode(DictionaryConfigurationController dcc)
		{
			dcc.View.TreeControl.Tree.SelectedNode = null;
		}

		[Test]
		public void FindStartingConfigNode_FindsSharedNodes()
		{
			var subsubsensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				ReferenceItem = "SharedSubsenses"
			};
			var subSensesSharedItem = new ConfigurableDictionaryNode
			{
				Label = "SharedSubsenses",
				FieldDescription = "SensesOS",
				Children = new List<ConfigurableDictionaryNode> { subsubsensesNode }
			};
			var subSensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				ReferenceItem = "SharedSubsenses"
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				Children = new List<ConfigurableDictionaryNode> { subSensesNode }
			};
			var entryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				CSSClassNameOverride = "entry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(DictionaryConfigurationModelTests.CreateSimpleSharingModel(entryNode, subSensesSharedItem));
			var node = DictionaryConfigurationController.FindConfigNode(entryNode, new List<string>
			{
				"entry",
				"senses",
				"sensecontent",
				"sense",
				"senses mainentrysubsenses",
				"sensecontent",
				"sense mainentrysubsense",
				"senses mainentrysubsenses",
				"sensecontent",
				"sensenumber"
			});
			Assert.AreSame(subsubsensesNode, node, "Sense Numbers are configured on the node itself, not its ReferencedOrDirectChildren.{0}Expected: {1}{0}But got:  {2}", Environment.NewLine,
				DictionaryConfigurationServices.BuildPathStringFromNode(subsubsensesNode), DictionaryConfigurationServices.BuildPathStringFromNode(node));
		}

		[Test]
		public void EnsureValidStylesInModelRemovesMissingStyles()
		{
			var sharedKid = new ConfigurableDictionaryNode { Style = "bad" };
			var sharedNode = new ConfigurableDictionaryNode { Children = new List<ConfigurableDictionaryNode> { sharedKid } };
			var senseNode = new ConfigurableDictionaryNode
			{
				Label = "Senses",
				FieldDescription = "SensesOS",
				IsEnabled = true,
				DictionaryNodeOptions = new DictionaryNodeSenseOptions
				{
					DisplayEachSenseInAParagraph = true,
					NumberStyle = "Green-Dictionary-SenseNumber",
					NumberingStyle = "%d",
					NumberEvenASingleSense = false,
					ShowSharedGrammarInfoFirst = true
				},
				StyleType = StyleTypes.Paragraph,
				Style = "Orange-Sense-Paragraph"
			};
			var entryNode = new ConfigurableDictionaryNode
			{
				Label = "Entry",
				FieldDescription = "LexEntry",
				IsEnabled = true,
				Style = "Dictionary-Continuation",
				Children = new List<ConfigurableDictionaryNode> { senseNode }
			};
			var model = DictionaryConfigurationModelTests.CreateSimpleSharingModel(entryNode, sharedNode);
			DictionaryConfigurationController.EnsureValidStylesInModel(model, Cache);
			//SUT
			Assert.IsNull(entryNode.Style, "Missing style should be removed.");
			Assert.IsNull(senseNode.Style, "Missing style should be removed.");
			Assert.IsNull(sharedKid.Style, "Missing style should be removed.");
			Assert.IsNull(((DictionaryNodeSenseOptions)senseNode.DictionaryNodeOptions).NumberStyle, "Missing style should be removed.");
		}

		[Test]
		public void CheckNewAndDeleteddVariantTypes()
		{
			var minorEntryVariantNode = new ConfigurableDictionaryNode
			{
				Label = "Minor Entry (Variants)",
				FieldDescription = "LexEntry",
				IsEnabled = true,
				DictionaryNodeOptions = new DictionaryNodeListOptions
				{
					ListId = ListIds.Variant,
					Options = new List<DictionaryNodeOption>
					{
						new DictionaryNodeOption { Id="01234567-89ab-cdef-0123-456789abcdef", IsEnabled = true },
						new DictionaryNodeOption { Id="024b62c9-93b3-41a0-ab19-587a0030219a", IsEnabled = true },
						new DictionaryNodeOption { Id="4343b1ef-b54f-4fa4-9998-271319a6d74c", IsEnabled = true },
						new DictionaryNodeOption { Id="01d4fbc1-3b0c-4f52-9163-7ab0d4f4711c", IsEnabled = true },
					},
				},
			};
			var variantsNode = new ConfigurableDictionaryNode
			{
				Label = "Variant Forms",
				FieldDescription = "VariantFormEntryBackRefs",
				IsEnabled = true,
				DictionaryNodeOptions = new DictionaryNodeListOptions
				{
					ListId = ListIds.Variant,
					Options = new List<DictionaryNodeOption>
					{
						new DictionaryNodeOption { Id="01234567-89ab-cdef-0123-456789abcdef", IsEnabled = true },
						new DictionaryNodeOption { Id="024b62c9-93b3-41a0-ab19-587a0030219a", IsEnabled = true },
						new DictionaryNodeOption { Id="4343b1ef-b54f-4fa4-9998-271319a6d74c", IsEnabled = true },
						new DictionaryNodeOption { Id="01d4fbc1-3b0c-4f52-9163-7ab0d4f4711c", IsEnabled = true }
					}
				}
			};
			var entryNode = new ConfigurableDictionaryNode
			{
				Label = "Main Entry",
				FieldDescription = "LexEntry",
				IsEnabled = true,
				Style = "Dictionary-Normal",
				Children = new List<ConfigurableDictionaryNode> { variantsNode }
			};
			var model = new DictionaryConfigurationModel
			{
				FilePath = "/no/such/file",
				Version = 0,
				Label = "Root",
				Parts = new List<ConfigurableDictionaryNode> { entryNode, minorEntryVariantNode },
			};
			var newType = CreateNewVariantType("Absurd Variant");
			// SUT
			try
			{
				DictionaryConfigurationController.MergeTypesIntoDictionaryModel(model, Cache);
				var opts1 = ((DictionaryNodeListOptions)variantsNode.DictionaryNodeOptions).Options;
				// We have options for the standard seven variant types (including the last three shown above, plus one for the
				// new type we added, plus one for the "No Variant Type" pseudo-type for a total of eight.
				Assert.AreEqual(9, opts1.Count, "Properly merged variant types to options list in major entry child node");
				Assert.AreEqual(newType.Guid.ToString(), opts1[7].Id, "New type appears near end of options list in major entry child node");
				Assert.AreEqual("b0000000-c40e-433e-80b5-31da08771344", opts1[8].Id, "'No Variant Type' type appears at end of options list in major entry child node");
				var opts2 = ((DictionaryNodeListOptions)minorEntryVariantNode.DictionaryNodeOptions).Options;
				Assert.AreEqual(9, opts2.Count, "Properly merged variant types to options list in minor entry top node");
				Assert.AreEqual(newType.Guid.ToString(), opts2[7].Id, "New type appears near end of options list in minor entry top node");
				Assert.AreEqual("b0000000-c40e-433e-80b5-31da08771344", opts2[8].Id, "'No Variant Type' type appears near end of options list in minor entry top node");
			}
			finally
			{
				// Don't mess up other unit tests with an extra variant type.
				RemoveNewVariantType(newType);
			}
		}

		[Test]
		public void CheckNewAndDeletedReferenceTypes()
		{
			var lexicalRelationNode = new ConfigurableDictionaryNode
			{
				Label = "Lexical Relations",
				FieldDescription = "LexSenseReferences",
				IsEnabled = true,
				DictionaryNodeOptions = new DictionaryNodeListOptions
				{
					ListId = ListIds.Sense,
					Options = new List<DictionaryNodeOption>
					{
						new DictionaryNodeOption { Id="0b5b04c8-3900-4537-9eec-1346d10507d7", IsEnabled = true },
						new DictionaryNodeOption { Id="1ac9f08e-ed72-4775-a18e-3b1330da8618", IsEnabled = true },
						new DictionaryNodeOption { Id="854fc2a8-c0e0-4b72-8611-314a21467fe4", IsEnabled = true }
					},
				},
			};
			var senseNode = new ConfigurableDictionaryNode
			{
				Label = "Senses",
				FieldDescription = "SensesOS",
				IsEnabled = true,
				DictionaryNodeOptions = new DictionaryNodeSenseOptions
				{
					DisplayEachSenseInAParagraph = true,
					NumberingStyle = "%d",
					NumberEvenASingleSense = false,
					ShowSharedGrammarInfoFirst = true
				},
				Children = new List<ConfigurableDictionaryNode> { lexicalRelationNode }
			};
			var entryNode = new ConfigurableDictionaryNode
			{
				Label = "Main Entry",
				FieldDescription = "LexEntry",
				IsEnabled = true,
				Style = "Dictionary-Normal",
				Children = new List<ConfigurableDictionaryNode> { senseNode }
			};
			var model = new DictionaryConfigurationModel
			{
				FilePath = "/no/such/file",
				Version = 0,
				Label = "Root",
				Parts = new List<ConfigurableDictionaryNode> { entryNode },
			};
			var newType = MakeRefType("Part", null, (int)LexRefTypeTags.MappingTypes.kmtSenseCollection);
			// SUT
			try
			{
				DictionaryConfigurationController.MergeTypesIntoDictionaryModel(model, Cache);
				var opts1 = ((DictionaryNodeListOptions)lexicalRelationNode.DictionaryNodeOptions).Options;
				Assert.AreEqual(1, opts1.Count, "Improper number of reference types on lexical relation node");
				Assert.AreEqual(newType.Guid.ToString(), opts1[0].Id, "New type should appear in the list in lexical relation node");
			}
			finally
			{
				// Don't mess up other unit tests with an extra reference type.
				RemoveNewReferenceType(newType);
			}
		}

		[Test]
		public void CheckAsymmetricReferenceType()
		{
			var lexicalRelationNode = new ConfigurableDictionaryNode
			{
				Label = "Lexical Relations",
				FieldDescription = "LexSenseReferences",
				IsEnabled = true,
				DictionaryNodeOptions = new DictionaryNodeListOptions
				{
					ListId = ListIds.Sense,
					Options = new List<DictionaryNodeOption>
					{
						new DictionaryNodeOption { Id="0b5b04c8-3900-4537-9eec-1346d10507d7", IsEnabled = true },
						new DictionaryNodeOption { Id="1ac9f08e-ed72-4775-a18e-3b1330da8618", IsEnabled = true },
						new DictionaryNodeOption { Id="854fc2a8-c0e0-4b72-8611-314a21467fe4", IsEnabled = true }
					},
				},
			};
			var senseNode = new ConfigurableDictionaryNode
			{
				Label = "Senses",
				FieldDescription = "SensesOS",
				IsEnabled = true,
				DictionaryNodeOptions = new DictionaryNodeSenseOptions
				{
					DisplayEachSenseInAParagraph = true,
					NumberingStyle = "%d",
					NumberEvenASingleSense = false,
					ShowSharedGrammarInfoFirst = true
				},
				Children = new List<ConfigurableDictionaryNode> { lexicalRelationNode }
			};
			var entryNode = new ConfigurableDictionaryNode
			{
				Label = "Main Entry",
				FieldDescription = "LexEntry",
				IsEnabled = true,
				Style = "Dictionary-Normal",
				Children = new List<ConfigurableDictionaryNode> { senseNode }
			};
			var model = new DictionaryConfigurationModel
			{
				FilePath = "/no/such/file",
				Version = 0,
				Label = "Root",
				Parts = new List<ConfigurableDictionaryNode> { entryNode },
			};
			var senseTreeType = MakeRefType("Part", null, (int)LexRefTypeTags.MappingTypes.kmtSenseTree);
			var senseUnidirectionalType = MakeRefType("Part", null, (int)LexRefTypeTags.MappingTypes.kmtSenseUnidirectional);
			var entryUnidirectionalType = MakeRefType("Part", null, (int)LexRefTypeTags.MappingTypes.kmtEntryUnidirectional);
			var senseAsymmetricPairType = MakeRefType("Part", null, (int)LexRefTypeTags.MappingTypes.kmtSenseAsymmetricPair);
			var entryAsymmetricPairType = MakeRefType("Part", null, (int)LexRefTypeTags.MappingTypes.kmtEntryAsymmetricPair);
			var entryOrSenseAsymmetricPairType = MakeRefType("Part", null, (int)LexRefTypeTags.MappingTypes.kmtEntryOrSenseAsymmetricPair);
			var entryOrSenseTreeType = MakeRefType("Part", null, (int)LexRefTypeTags.MappingTypes.kmtEntryOrSenseTree);
			var entryOrSenseUnidirectionalType = MakeRefType("Part", null, (int)LexRefTypeTags.MappingTypes.kmtEntryOrSenseUnidirectional);
			var entryTreeType = MakeRefType("Part", null, (int)LexRefTypeTags.MappingTypes.kmtEntryTree);
			// SUT
			try
			{
				DictionaryConfigurationController.MergeTypesIntoDictionaryModel(model, Cache);
				var opts1 = ((DictionaryNodeListOptions)lexicalRelationNode.DictionaryNodeOptions).Options;
				Assert.AreEqual(18, opts1.Count, "The new tree reftype should have added 2 options, the rest should have been removed.");
				Assert.AreEqual(senseTreeType.Guid + ":f", opts1[0].Id, "The sense tree type should have added the first option with :f appended to the guid.");
				Assert.AreEqual(senseTreeType.Guid + ":r", opts1[1].Id, "The sense tree type should have added the second option with :r appended to the guid.");
				Assert.AreEqual(senseUnidirectionalType.Guid + ":f", opts1[2].Id, "The sense unidirectional type should have added the first option with :f appended to the guid.");
				Assert.AreEqual(senseUnidirectionalType.Guid + ":r", opts1[3].Id, "The sense unidirectional type should have added the second option with :r appended to the guid.");
				Assert.AreEqual(entryUnidirectionalType.Guid + ":f", opts1[4].Id, "The entry unidirectional type should have added the first option with :f appended to the guid.");
				Assert.AreEqual(entryUnidirectionalType.Guid + ":r", opts1[5].Id, "The entry unidirectional type should have added the second option with :r appended to the guid.");
				Assert.AreEqual(senseAsymmetricPairType.Guid + ":f", opts1[6].Id, "The sense asymmetric pair type should have added the first option with :f appended to the guid.");
				Assert.AreEqual(senseAsymmetricPairType.Guid + ":r", opts1[7].Id, "The sense asymmetric pair type should have added the second option with :r appended to the guid.");
				Assert.AreEqual(entryAsymmetricPairType.Guid + ":f", opts1[8].Id, "The entry asymmetric pair type should have added the first option with :f appended to the guid.");
				Assert.AreEqual(entryAsymmetricPairType.Guid + ":r", opts1[9].Id, "The entry asymmetric pair type should have added the second option with :r appended to the guid.");
				Assert.AreEqual(entryOrSenseAsymmetricPairType.Guid + ":f", opts1[10].Id, "The entry or sense asymmetric pair type should have added the first option with :f appended to the guid.");
				Assert.AreEqual(entryOrSenseAsymmetricPairType.Guid + ":r", opts1[11].Id, "The entry or sense asymmetric pair type should have added the second option with :r appended to the guid.");
				Assert.AreEqual(entryOrSenseTreeType.Guid + ":f", opts1[12].Id, "The entry or sense tree type should have added the first option with :f appended to the guid.");
				Assert.AreEqual(entryOrSenseTreeType.Guid + ":r", opts1[13].Id, "The entry or sense tree type should have added the second option with :r appended to the guid.");
				Assert.AreEqual(entryOrSenseUnidirectionalType.Guid + ":f", opts1[14].Id, "The entry or sense unidirectional type should have added the first option with :f appended to the guid.");
				Assert.AreEqual(entryOrSenseUnidirectionalType.Guid + ":r", opts1[15].Id, "The entry or sense unidirectional type should have added the second option with :r appended to the guid.");
				Assert.AreEqual(entryTreeType.Guid + ":f", opts1[16].Id, "The entry tree type should have added the first option with :f appended to the guid.");
				Assert.AreEqual(entryTreeType.Guid + ":r", opts1[17].Id, "The entry tree type should have added the second option with :r appended to the guid.");
			}
			finally
			{
				// Don't mess up other unit tests with an extra reference type.
				RemoveNewReferenceType(senseTreeType);
				RemoveNewReferenceType(senseUnidirectionalType);
				RemoveNewReferenceType(entryUnidirectionalType);
				RemoveNewReferenceType(senseAsymmetricPairType);
				RemoveNewReferenceType(entryAsymmetricPairType);
				RemoveNewReferenceType(entryOrSenseAsymmetricPairType);
				RemoveNewReferenceType(entryOrSenseTreeType);
				RemoveNewReferenceType(entryOrSenseUnidirectionalType);
				RemoveNewReferenceType(entryTreeType);
			}
		}

		[Test]
		public void CheckCrossReferenceType()
		{
			var crossReferencesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MinimalLexReferences",
				DictionaryNodeOptions = new DictionaryNodeListOptions
				{
					ListId = ListIds.Entry,
					Options = new List<DictionaryNodeOption>
					{
						new DictionaryNodeOption { Id="0b5b04c8-3900-4537-9eec-1346d10507d7", IsEnabled = true },
						new DictionaryNodeOption { Id="1ac9f08e-ed72-4775-a18e-3b1330da8618", IsEnabled = true },
						new DictionaryNodeOption { Id="854fc2a8-c0e0-4b72-8611-314a21467fe4", IsEnabled = true }
					}
				}
			};
			var entryNode = new ConfigurableDictionaryNode
			{
				Label = "Main Entry",
				FieldDescription = "LexEntry",
				IsEnabled = true,
				Style = "Dictionary-Normal",
				Children = new List<ConfigurableDictionaryNode> { crossReferencesNode }
			};
			var model = new DictionaryConfigurationModel
			{
				FilePath = "/no/such/file",
				Version = 0,
				Label = "Root",
				Parts = new List<ConfigurableDictionaryNode> { entryNode },
			};
			var entryAsymmetricPairType = MakeRefType("Compare", null, (int)LexRefTypeTags.MappingTypes.kmtEntryUnidirectional);
			// SUT
			try
			{
				DictionaryConfigurationController.MergeTypesIntoDictionaryModel(model, Cache);
				var opts1 = ((DictionaryNodeListOptions)crossReferencesNode.DictionaryNodeOptions).Options;
				Assert.AreEqual(2, opts1.Count, "The new tree reftype should have added 2 options.");
				Assert.AreEqual(entryAsymmetricPairType.Guid + ":f", opts1[0].Id, "The entry asymmetric pair type should have added the first option with :f appended to the guid.");
				Assert.AreEqual(entryAsymmetricPairType.Guid + ":r", opts1[1].Id, "The entry asymmetric pair type should have added the second option with :r appended to the guid.");
			}
			finally
			{
				// Don't mess up other unit tests with an extra reference type.
				RemoveNewReferenceType(entryAsymmetricPairType);
			}
		}

		[Test]
		public void CheckNewAndDeletedNoteTypes()
		{
			const string disabledButValid = "7ad06e7d-15d1-42b0-ae19-9c05b7c0b181";
			const string enabledAndValid = "30115b33-608a-4506-9f9c-2457cab4f4a8";
			const string doesNotExist = "bad50bad-5050-5000-baad-badbadbadbad";
			var noteNode = new ConfigurableDictionaryNode
			{
				Label = "Extended Note",
				FieldDescription = "ExtendedNoteOS",
				DictionaryNodeOptions = new DictionaryNodeListOptions
				{
					ListId = ListIds.Note,
					Options = new List<DictionaryNodeOption>
					{
						new DictionaryNodeOption { Id = disabledButValid, IsEnabled = false },
						new DictionaryNodeOption { Id = enabledAndValid, IsEnabled = true },
						new DictionaryNodeOption { Id = doesNotExist, IsEnabled = true }
					},
				},
			};
			var senseNode = new ConfigurableDictionaryNode
			{
				Label = "Senses",
				FieldDescription = "SensesOS",
				Children = new List<ConfigurableDictionaryNode> { noteNode }
			};
			var entryNode = new ConfigurableDictionaryNode
			{
				Label = "Main Entry",
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { senseNode }
			};
			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { entryNode } };
			// SUT
			DictionaryConfigurationController.MergeTypesIntoDictionaryModel(model, Cache);
			var opts = ((DictionaryNodeListOptions)noteNode.DictionaryNodeOptions).Options;
			Assert.AreEqual(6, opts.Count, "Didn't merge properly (or more shipping note types have been added)");
			var validOption = opts.FirstOrDefault(opt => opt.Id == disabledButValid);
			Assert.NotNull(validOption, "A valid option has been removed");
			Assert.False(validOption.IsEnabled, "This option should remain disabled");
			validOption = opts.FirstOrDefault(opt => opt.Id == enabledAndValid);
			Assert.NotNull(validOption, "Another valid option has been removed");
			Assert.True(validOption.IsEnabled, "This option should remain enabled");
			Assert.That(opts.Any(opt => opt.Id == XmlViewsUtils.GetGuidForUnspecifiedExtendedNoteType().ToString()), "Unspecified Type not added");
			Assert.That(opts.All(opt => opt.Id != doesNotExist), "Bad Type should have been removed");
		}

		[Test]
		public void CheckExtendedNote_Null_DoesNotCrash()
		{
			var noteNode = new ConfigurableDictionaryNode
			{
				Label = "Extended Note",
				FieldDescription = "ExtendedNoteOS",
				DictionaryNodeOptions = new DictionaryNodeListOptions
				{
					ListId = ListIds.Note
				}
			};
			var senseNode = new ConfigurableDictionaryNode
			{
				Label = "Senses",
				FieldDescription = "SensesOS",
				Children = new List<ConfigurableDictionaryNode> { noteNode }
			};
			var entryNode = new ConfigurableDictionaryNode
			{
				Label = "Main Entry",
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { senseNode }
			};
			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { entryNode } };
			Cache.LangProject.LexDbOA.ExtendedNoteTypesOA = null;
			// SUT
			DictionaryConfigurationController.MergeTypesIntoDictionaryModel(model, Cache);
			var opts = ((DictionaryNodeListOptions)noteNode.DictionaryNodeOptions).Options;
			Assert.AreEqual(0, opts.Count, "Extended Note generated without any crash");
		}

		[Test]
		public void ShareNodeAsReference()
		{
			var configNodeChild = new ConfigurableDictionaryNode { Label = "child", FieldDescription = "someField" };
			var configNode = new ConfigurableDictionaryNode
			{
				FieldDescription = _field,
				Label = "parent",
				Children = new List<ConfigurableDictionaryNode> { configNodeChild }
			};
			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { configNode } };
			DictionaryConfigurationModel.SpecifyParentsAndReferences(model.Parts);
			// SUT
			DictionaryConfigurationController.ShareNodeAsReference(model.SharedItems, configNode);
			Assert.AreEqual(1, model.Parts.Count, "should still be 1 part");
			Assert.AreEqual(1, model.SharedItems.Count, "Should be 1 shared item");
			Assert.AreSame(configNode, model.Parts[0]);
			var sharedItem = model.SharedItems[0];
			Assert.AreEqual(_field, configNode.FieldDescription, "Part's field");
			Assert.AreEqual(_field, sharedItem.FieldDescription, "Shared Item's field");
			Assert.AreEqual("shared" + CssGenerator.GetClassAttributeForConfig(configNode), CssGenerator.GetClassAttributeForConfig(sharedItem));
			Assert.That(sharedItem.IsEnabled, "shared items are always enabled (for configurability)");
			Assert.AreSame(configNode, sharedItem.Parent, "The original owner should be the 'master parent'");
			Assert.AreSame(sharedItem, configNode.ReferencedNode, "The ReferencedNode should be the SharedItem");
			Assert.NotNull(configNode.ReferencedNode, "part should store a reference to the shared item in memory");
			Assert.NotNull(configNode.ReferenceItem, "part should store the name of the shared item");
			Assert.AreEqual(sharedItem.Label, configNode.ReferenceItem, "Part should store the name of the shared item");
			sharedItem.Children.ForEach(child => Assert.AreSame(sharedItem, child.Parent));
		}

		[Test]
		public void ShareNodeAsReference_PreventsDuplicateSharedItemLabel()
		{
			var configNodeChild = new ConfigurableDictionaryNode { Label = "child", FieldDescription = "someField" };
			var configNode = new ConfigurableDictionaryNode
			{
				FieldDescription = _field,
				Label = "parent",
				Children = new List<ConfigurableDictionaryNode> { configNodeChild }
			};
			var preextantSharedNode = new ConfigurableDictionaryNode { Label = "Sharedparent" };
			var model = DictionaryConfigurationModelTests.CreateSimpleSharingModel(configNode, preextantSharedNode);
			DictionaryConfigurationModel.SpecifyParentsAndReferences(model.Parts, model.SharedItems);
			// SUT
			Assert.Throws<ArgumentException>(() => DictionaryConfigurationController.ShareNodeAsReference(model.SharedItems, configNode));
		}

		[Test]
		public void ShareNodeAsReference_PreventsDuplicateSharedItemCssClass()
		{
			var configNodeChild = new ConfigurableDictionaryNode { Label = "child", FieldDescription = "someField" };
			var configNode = new ConfigurableDictionaryNode
			{
				FieldDescription = _field,
				Label = "parent",
				Children = new List<ConfigurableDictionaryNode> { configNodeChild }
			};
			var preextantSharedNode = new ConfigurableDictionaryNode { CSSClassNameOverride = string.Format("shared{0}", _field).ToLower() };
			var model = DictionaryConfigurationModelTests.CreateSimpleSharingModel(configNode, preextantSharedNode);
			DictionaryConfigurationModel.SpecifyParentsAndReferences(model.Parts, model.SharedItems);
			// SUT
			Assert.Throws<ArgumentException>(() => DictionaryConfigurationController.ShareNodeAsReference(model.SharedItems, configNode));
		}

		[Test]
		public void ShareNodeAsReference_DoesntShareNodeOfSameTypeAsPreextantSharedNode()
		{
			var configNodeChild = new ConfigurableDictionaryNode { Label = "child", FieldDescription = "someField" };
			var configNode = new ConfigurableDictionaryNode
			{
				FieldDescription = _field,
				Label = "parent",
				Children = new List<ConfigurableDictionaryNode> { configNodeChild }
			};
			var preextantSharedNode = new ConfigurableDictionaryNode { FieldDescription = _field, Parent = new ConfigurableDictionaryNode() };
			var model = DictionaryConfigurationModelTests.CreateSimpleSharingModel(configNode, preextantSharedNode);
			DictionaryConfigurationModel.SpecifyParentsAndReferences(model.Parts, model.SharedItems);
			// SUT
			DictionaryConfigurationController.ShareNodeAsReference(model.SharedItems, configNode);
			Assert.AreEqual(1, model.SharedItems.Count, "Should be only the preextant shared item");
			Assert.AreSame(preextantSharedNode, model.SharedItems[0], "Should be only the preextant shared item");
		}

		[Test]
		public void ShareNodeAsReference_PreventSharingSharedNode()
		{
			var configNode = new ConfigurableDictionaryNode { ReferencedNode = new ConfigurableDictionaryNode() };
			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { configNode } };
			// SUT
			Assert.Throws<InvalidOperationException>(() => DictionaryConfigurationController.ShareNodeAsReference(model.SharedItems, configNode));
		}

		[Test]
		public void ShareNodeAsReference_DoesntShareChildlessNode()
		{
			var configNode = new ConfigurableDictionaryNode { FieldDescription = _field, Label = "parent" };
			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { configNode } };
			// SUT
			DictionaryConfigurationController.ShareNodeAsReference(model.SharedItems, configNode);
			Assert.IsEmpty(model.SharedItems);
			configNode.Children = new List<ConfigurableDictionaryNode>();
			// SUT
			DictionaryConfigurationController.ShareNodeAsReference(model.SharedItems, configNode);
			Assert.IsEmpty(model.SharedItems);
		}

		private ILexEntryType CreateNewVariantType(string name)
		{
			ILexEntryType poss = null;
			NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(Cache.ActionHandlerAccessor, () =>
			{
				var fact = Cache.ServiceLocator.GetInstance<ILexEntryTypeFactory>();
				poss = fact.Create();
				Cache.LangProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS.Add(poss);
				poss.Name.SetAnalysisDefaultWritingSystem(name);
			});
			return poss;
		}

		private ILexRefType MakeRefType(string name, string reverseName, int mapType)
		{
			ILexRefType result = null;
			NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(Cache.ActionHandlerAccessor, () =>
			{
				if (Cache.LangProject.LexDbOA.ReferencesOA == null)
				{
					Cache.LangProject.LexDbOA.ReferencesOA = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
				}
				result = Cache.ServiceLocator.GetInstance<ILexRefTypeFactory>().Create();
				Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.Add(result);
				result.Name.AnalysisDefaultWritingSystem = AnalysisTss(name);
				if (reverseName != null)
				{
					result.ReverseName.AnalysisDefaultWritingSystem = AnalysisTss(reverseName);
				}
				result.MappingType = mapType;
			});
			return result;
		}

		private ITsString AnalysisTss(string form)
		{
			return TsStringUtils.MakeString(form, Cache.DefaultAnalWs);
		}

		private void RemoveNewVariantType(ILexEntryType newType)
		{
			NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(Cache.ActionHandlerAccessor, () =>
			{
				Cache.LangProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS.Remove(newType);
			});
		}

		private void RemoveNewReferenceType(ILexRefType newType)
		{
			NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(Cache.ActionHandlerAccessor, () =>
			{
				Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.Remove(newType);
			});
		}

		private static readonly string[] subsenseClassListArray = { "entry", "senses", "sensecontent", "sense", "senses mainentrysubsenses", "sensecontent", "sense mainentrysubsense" };

		[Test]
		public void SetStartingNode_WorksWithReferencedSubsenseNode()
		{
			var subSensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				Label = "Subsenses",
				ReferenceItem = "MainEntrySubsenses"
			};
			var subGlossNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Gloss"
			};
			var referencedConfigNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				Children = new List<ConfigurableDictionaryNode> { subGlossNode },
				Label = "MainEntrySubsenses"
			};
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				Label = "Headword",
				CSSClassNameOverride = "mainheadword"
			};
			var glossNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Gloss"
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				Label = "Senses",
				CSSClassNameOverride = "senses",
				Children = new List<ConfigurableDictionaryNode> { glossNode, subSensesNode }
			};
			var entryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Label = "Main Entry",
				CSSClassNameOverride = "entry",
				Children = new List<ConfigurableDictionaryNode> { headwordNode, sensesNode },
			};
			_model.Parts = new List<ConfigurableDictionaryNode> { entryNode };
			_model.SharedItems = new List<ConfigurableDictionaryNode> { referencedConfigNode };
			CssGeneratorTests.PopulateFieldsForTesting(_model);
			var subSenseGloss = subsenseClassListArray.ToList();
			subSenseGloss.Add("gloss");
			var subSenseUndefined = subsenseClassListArray.ToList();
			subSenseUndefined.Add("undefined");
			using (var testView = new TestConfigurableDictionaryView())
			{
				var dcc = new DictionaryConfigurationController { View = testView, _model = _model };
				dcc.CreateTreeOfTreeNodes(null, _model.Parts);
				//Test normal case first
				dcc.SetStartingNode(new List<string> { "entry", "senses", "sensecontent", "sense", "gloss" });
				var treeNode = dcc.View.TreeControl.Tree.SelectedNode;
				Assert.IsNotNull(treeNode, "Passing a valid class list should find a TreeNode");
				Assert.AreSame(glossNode, treeNode.Tag, "Passing a valid class list should find the node");
				//SUT
				ClearSelectedNode(dcc);
				dcc.SetStartingNode(subSenseGloss);
				treeNode = dcc.View.TreeControl.Tree.SelectedNode;
				Assert.IsNotNull(treeNode, "Passing a valid class list should find a TreeNode");
				Assert.AreSame(subGlossNode, treeNode.Tag, "Passing a valid class list should even find the node in a referenced node");
				ClearSelectedNode(dcc);
				dcc.SetStartingNode(subSenseUndefined);
				treeNode = dcc.View.TreeControl.Tree.SelectedNode;
				Assert.IsNotNull(treeNode, "invalid field should still find a TreeNode");
				Assert.AreSame(subSensesNode, treeNode.Tag, "'undefined' field should find the closest TreeNode");
			}
		}

		private static readonly string[] subentryClassListArray = { "entry", "subentries mainentrysubentries", "subentry mainentrysubentry" };

		[Test]
		public void SetStartingNode_WorksWithReferencedSubentryNode()
		{
			var subentriesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Subentries",
				ReferenceItem = "MainEntrySubentries"
			};
			var subGlossNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Gloss"
			};
			var referencedConfigNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Subentries",
				CSSClassNameOverride = "mainentrysubentries",
				Children = new List<ConfigurableDictionaryNode> { subGlossNode },
				Label = "MainEntrySubentries"
			};
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				Label = "Headword",
				CSSClassNameOverride = "mainheadword"
			};
			var entryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Label = "Main Entry",
				CSSClassNameOverride = "entry",
				Children = new List<ConfigurableDictionaryNode> { headwordNode, subentriesNode },
			};
			_model.Parts = new List<ConfigurableDictionaryNode> { entryNode };
			_model.SharedItems = new List<ConfigurableDictionaryNode> { referencedConfigNode };
			CssGeneratorTests.PopulateFieldsForTesting(_model);
			var subentryGloss = subentryClassListArray.ToList();
			subentryGloss.Add("gloss");
			var subentryUndefined = subentryClassListArray.ToList();
			subentryUndefined.Add("undefined");
			var subentriesClassList = subentryClassListArray.ToList();
			subentriesClassList.RemoveAt(subentriesClassList.Count - 1);
			using (var testView = new TestConfigurableDictionaryView())
			{
				var dcc = new DictionaryConfigurationController { View = testView, _model = _model };
				dcc.CreateTreeOfTreeNodes(null, _model.Parts);
				//SUT
				dcc.SetStartingNode(subentryGloss);
				var treeNode = dcc.View.TreeControl.Tree.SelectedNode;
				Assert.IsNotNull(treeNode, "Passing a valid class list should find a TreeNode");
				Assert.AreSame(subGlossNode, treeNode.Tag, "Passing a valid class list should even find the node in a referenced node");
				ClearSelectedNode(dcc);
				dcc.SetStartingNode(subentryUndefined);
				treeNode = dcc.View.TreeControl.Tree.SelectedNode;
				Assert.IsNotNull(treeNode, "invalid field should still find a TreeNode");
				Assert.AreSame(subentriesNode, treeNode.Tag, "'undefined' field should find the closest TreeNode");
				ClearSelectedNode(dcc);
				dcc.SetStartingNode(subentriesClassList);
				treeNode = dcc.View.TreeControl.Tree.SelectedNode;
				Assert.IsNotNull(treeNode, "should find main Subentries node");
				Assert.AreSame(subentriesNode, treeNode.Tag, "Passing a valid class list should find it");
			}
		}

		[Test]
		public void CheckBoxEnableForVariantInflectionalType()
		{
			var minorEntryVariantNode = new ConfigurableDictionaryNode
			{
				Label = "Minor Entry (Variants)",
				FieldDescription = "LexEntry",
				DictionaryNodeOptions = new DictionaryNodeListOptions
				{
					ListId = ListIds.Variant
				}
			};
			var subentriesNode = new ConfigurableDictionaryNode { FieldDescription = "Subentries" };
			var variantsNode = new ConfigurableDictionaryNode
			{
				Label = "Variant Forms",
				FieldDescription = "VariantFormEntryBackRefs",
				DictionaryNodeOptions = new DictionaryNodeListOptions
				{
					ListId = ListIds.Variant
				}
			};
			var variantsInflectionalNode = new ConfigurableDictionaryNode
			{
				Label = "Variant Forms (Inflectional-Variants)",
				LabelSuffix = "Inflectional-Variants",
				FieldDescription = "VariantFormEntryBackRefs",
				IsDuplicate = true,
				DictionaryNodeOptions = new DictionaryNodeListOptions
				{
					ListId = ListIds.Variant
				}
			};
			var entryNode = new ConfigurableDictionaryNode
			{
				Label = "Main Entry",
				FieldDescription = "LexEntry",
				Style = "Dictionary-Normal",
				Children = new List<ConfigurableDictionaryNode> { subentriesNode, variantsNode, variantsInflectionalNode }
			};
			var model = new DictionaryConfigurationModel
			{
				Label = "Hybrid",
				Parts = new List<ConfigurableDictionaryNode> { entryNode, minorEntryVariantNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(model);
			var let = CreateNewVariantType("Absurd Variant");
			try
			{
				var normTypeGuid = let.Guid.ToString();
				var inflTypeGuid = Cache.LangProject.LexDbOA.VariantEntryTypesOA.ReallyReallyAllPossibilities.First(poss => poss.Name.AnalysisDefaultWritingSystem.Text == "Past Variant").Guid.ToString();
				// SUT
				DictionaryConfigurationController.MergeTypesIntoDictionaryModel(model, Cache);
				var inflOpts = ((DictionaryNodeListOptions)variantsInflectionalNode.DictionaryNodeOptions).Options;
				Assert.AreEqual(9, inflOpts.Count, "Should have merged all variant types into options list in Main Entry > Inflectional Variants");
				Assert.AreEqual(normTypeGuid, inflOpts[7].Id, "New type should appear near end of options list in Inflectional Variants node");
				Assert.IsFalse(inflOpts[7].IsEnabled, "New type should be false under Inflectional Variants beacuse it is a normal variant type");
				Assert.AreEqual(inflTypeGuid, inflOpts[5].Id, "Past Variant is not in its expected location");
				Assert.IsTrue(inflOpts[5].IsEnabled, "Past variant should enabled because of Inflectional");
				var normOpts = ((DictionaryNodeListOptions)variantsNode.DictionaryNodeOptions).Options;
				Assert.AreEqual(9, normOpts.Count, "Should have merged all variant types into options list in Main Entry > Variants");
				Assert.AreEqual(normTypeGuid, normOpts[7].Id, "New type should near end of options list in Main Entry > Variants");
				Assert.IsTrue(normOpts[7].IsEnabled, "New type should be true beacuse it is normal variant type");
				Assert.AreEqual(inflTypeGuid, normOpts[5].Id, "Past Variant is not in its expected location");
				Assert.IsFalse(normOpts[5].IsEnabled, "Past variant should not enabled because of Inflectional");
				var minorOpts = ((DictionaryNodeListOptions)minorEntryVariantNode.DictionaryNodeOptions).Options;
				Assert.AreEqual(9, minorOpts.Count, "should have merged all variant types into options list in minor entry top node");
				Assert.That(minorOpts.All(opt => opt.IsEnabled), "Should have enabled all (new) variant types in options list in minor entry top node");
			}
			finally
			{
				// Don't mess up other unit tests with an extra variant type.
				RemoveNewVariantType(let);
			}
		}

		private sealed class TestConfigurableDictionaryView : IDictionaryConfigurationView, IDisposable
		{
			public DictionaryConfigurationTreeControl TreeControl { get; } = new DictionaryConfigurationTreeControl();

			public IDictionaryDetailsView DetailsView { set; private get; }
			public string PreviewData { set; internal get; }

			public void Redraw()
			{ }

			public void HighlightContent(ConfigurableDictionaryNode configNode, IFwMetaDataCacheManaged metaDataCacheAccessor)
			{ }

			public void SetChoices(IEnumerable<DictionaryConfigurationModel> choices)
			{ }

			public void ShowPublicationsForConfiguration(string publications)
			{ }

			public void SelectConfiguration(DictionaryConfigurationModel configuration)
			{ }

			public void DoSaveModel()
			{
				SaveModel?.Invoke(null, null);
			}

			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			private void Dispose(bool disposing)
			{
				Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + " ******");
				if (disposing)
				{
					if (DetailsView != null && !DetailsView.IsDisposed)
					{
						DetailsView.Dispose();
					}
					TreeControl.Dispose();
				}
			}

			~TestConfigurableDictionaryView()
			{
				Dispose(false);
			}

			public void Close() { }

			public event EventHandler SaveModel;

#pragma warning disable 67
			public event EventHandler ManageConfigurations;

			public event SwitchConfigurationEvent SwitchConfiguration;
#pragma warning restore 67
		}
	}
}
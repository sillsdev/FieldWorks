// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using NUnit.Framework;

namespace SIL.FieldWorks.XWorks
{
	public class ConfigurableDictionaryNodeTests
	{
		[Test]
		public void ChildlessCanDeepClone()
		{
			var parent = new ConfigurableDictionaryNode();
			var child = new ConfigurableDictionaryNode() { After = "after", IsEnabled = true, Parent = parent };
			parent.Children = new List<ConfigurableDictionaryNode> { child };
			// SUT
			var clone = child.DeepCloneUnderSameParent();
			VerifyDuplication(clone, child);
		}

		[Test]
		public void CanDeepClone()
		{
			var parent = new ConfigurableDictionaryNode();
			var child = new ConfigurableDictionaryNode() { After = "after", IsEnabled = true, Parent = parent };
			var grandchild = new ConfigurableDictionaryNode() { Before = "childBefore", Parent = child };
			parent.Children = new List<ConfigurableDictionaryNode> { child };
			child.Children = new List<ConfigurableDictionaryNode> { grandchild };
			// SUT
			var clone = child.DeepCloneUnderSameParent();
			VerifyDuplication(clone, child);
		}

		private static void VerifyDuplication(ConfigurableDictionaryNode clone, ConfigurableDictionaryNode node)
		{
			Assert.That(clone.Parent, Is.EqualTo(node.Parent));
			Assert.That(clone.Parent, Is.SameAs(node.Parent));
			VerifyDuplicationInner(clone, node);
		}

		private static void VerifyDuplicationInner(ConfigurableDictionaryNode clone, ConfigurableDictionaryNode node)
		{
			Assert.That(clone.FieldDescription, Is.EqualTo(node.FieldDescription));
			Assert.That(clone.Style, Is.EqualTo(node.Style));
			Assert.That(clone.Before, Is.EqualTo(node.Before));
			Assert.That(clone.After, Is.EqualTo(node.After));
			Assert.That(clone.Between, Is.EqualTo(node.Between));
			Assert.That(clone.DictionaryNodeOptions, Is.EqualTo(node.DictionaryNodeOptions));
			Assert.That(clone.IsEnabled, Is.EqualTo(node.IsEnabled));
			Assert.That(clone.Label, Is.EqualTo(node.Label));

			VerifyDuplicationList(clone.Children, node.Children, clone);
		}

		internal static void VerifyDuplicationList(List<ConfigurableDictionaryNode> clone, List<ConfigurableDictionaryNode> list,
			ConfigurableDictionaryNode cloneParent)
		{
			if (list == null)
			{
				Assert.IsNull(clone);
				return;
			}

			Assert.That(clone.Count, Is.EqualTo(list.Count));
			for (int childIndex = 0; childIndex < list.Count; childIndex++)
			{
				Assert.That(clone[childIndex].Label, Is.EqualTo(list[childIndex].Label));
				VerifyDuplicationInner(clone[childIndex], list[childIndex]);
				Assert.That(clone[childIndex], Is.Not.SameAs(list[childIndex]), "Didn't deep-clone");
				Assert.That(clone[childIndex].Parent, Is.SameAs(cloneParent), "cloned children were not re-parented within deep-cloned object");
				if (cloneParent != null)
				{
					Assert.That(clone[childIndex].Parent, Is.Not.SameAs(list[childIndex].Parent),
						"Cloned children should be pointing to different parent nodes than the original");
				}
			}
		}

		[Test]
		public void DuplicateIsPutAmongSiblings()
		{
			var parent = new ConfigurableDictionaryNode();
			var childA = new ConfigurableDictionaryNode() { After = "after", IsEnabled = true, Parent = parent };
			var grandchildA = new ConfigurableDictionaryNode() { Before = "childBefore", Parent = childA };
			childA.Children = new List<ConfigurableDictionaryNode>() { grandchildA };
			var childB = new ConfigurableDictionaryNode() { After = "nodeBAfter", Parent = parent };
			parent.Children = new List<ConfigurableDictionaryNode>() { childA, childB };

			// SUT
			var duplicate = childA.DuplicateAmongSiblings();
			VerifyDuplication(duplicate, childA);
			Assert.That(parent.Children.Count, Is.EqualTo(3), "should have increased");
			Assert.That(parent.Children.Contains(duplicate), Is.True, "duplicate should be listed among siblings, added to the parent's list of children");
		}

		[Test]
		public void DuplicatesAreMarkedAsSuch()
		{
			var parent = new ConfigurableDictionaryNode() { Children = new List<ConfigurableDictionaryNode>() };
			var node = new ConfigurableDictionaryNode() { Parent = parent };
			parent.Children.Add(node);
			Assert.That(node.IsDuplicate, Is.False);

			// SUT
			var duplicate = node.DuplicateAmongSiblings();
			Assert.That(duplicate.IsDuplicate, Is.True);
			Assert.That(node.IsDuplicate, Is.False, "Original should not have been marked as a duplicate.");
		}

		[Test]
		public void DuplicatesHaveUniqueLabelSuffixes()
		{
			var parent = new ConfigurableDictionaryNode() { Children = new List<ConfigurableDictionaryNode>() };
			var nodeToDuplicateLabel = "node";
			var nodeToDuplicate = new ConfigurableDictionaryNode() { Parent = parent, Label = nodeToDuplicateLabel, LabelSuffix = null};
			var otherNodeA = new ConfigurableDictionaryNode() { Parent = parent, Label = "node", LabelSuffix = "1" };
			var otherNodeB = new ConfigurableDictionaryNode() { Parent = parent, Label = "node", LabelSuffix = "B" };
			parent.Children.Add(nodeToDuplicate);
			parent.Children.Add(otherNodeA);
			parent.Children.Add(otherNodeB);

			// SUT
			var duplicate = nodeToDuplicate.DuplicateAmongSiblings();
			Assert.That(parent.Children.FindAll(node => node.LabelSuffix == nodeToDuplicate.LabelSuffix).Count, Is.EqualTo(1), "Should not have any more nodes with the original label suffix. Was the duplicate node's label suffix not changed?");
			Assert.That(parent.Children.FindAll(node => node.LabelSuffix == duplicate.LabelSuffix).Count, Is.EqualTo(1), "The duplicate node was not given a unique label suffix among the siblings.");
			Assert.That(nodeToDuplicate.Label, Is.EqualTo(nodeToDuplicateLabel), "should not have changed original node label");
			Assert.That(nodeToDuplicate.LabelSuffix, Is.Null, "should not have changed original node label suffix");
		}

		[Test]
		public void DuplicateIsPutImmediatelyAfterOriginal()
		{
			var parent = new ConfigurableDictionaryNode();
			var nodeA = new ConfigurableDictionaryNode() { Parent = parent };
			var nodeB = new ConfigurableDictionaryNode() { Parent = parent };
			parent.Children = new List<ConfigurableDictionaryNode>() { nodeA, nodeB };
			Assert.That(parent.Children[0], Is.SameAs(nodeA));

			// SUT
			var duplicate = nodeA.DuplicateAmongSiblings();
			Assert.That(parent.Children[1], Is.SameAs(duplicate), "duplicate node should be placed immediately after duplicated node");
			Assert.That(parent.Children[2], Is.SameAs(nodeB), "second node in original list did not move into expected position");
		}

		[Test]
		public void DuplicateLastItemDoesNotThrow()
		{
			var parent = new ConfigurableDictionaryNode();
			var nodeA = new ConfigurableDictionaryNode() { Parent = parent };
			var nodeB = new ConfigurableDictionaryNode() { Parent = parent };
			parent.Children = new List<ConfigurableDictionaryNode>() { nodeA, nodeB };

			// SUT
			Assert.DoesNotThrow(() => nodeB.DuplicateAmongSiblings(), "problem with edge case");
		}

		[Test]
		public void CanDuplicateRootNode()
		{
			var rootNodeA = new ConfigurableDictionaryNode() { Parent = null, Before="beforeA" };
			var rootNodeB = new ConfigurableDictionaryNode() { Parent = null };
			var rootNodes = new List<ConfigurableDictionaryNode>() { rootNodeA, rootNodeB };

			// SUT
			var duplicate = rootNodeA.DuplicateAmongSiblings(rootNodes);
			Assert.That(rootNodes.Count, Is.EqualTo(3), "should have more nodes now");
			Assert.That(rootNodes.Contains(duplicate), "duplicate isn't among expected list of nodes");
			VerifyDuplication(duplicate, rootNodeA);
		}

		[Test]
		public void CanUnlink()
		{
			var parent = new ConfigurableDictionaryNode() { Children = new List<ConfigurableDictionaryNode>(), Parent = null };
			var node = new ConfigurableDictionaryNode() { Children = new List<ConfigurableDictionaryNode>(), Parent = parent };
			parent.Children.Add(node);
			// SUT
			node.UnlinkFromParent();
			Assert.That(parent.Children.Count, Is.EqualTo(0), "Parent should not link to unlinked child");
			Assert.That(node.Parent, Is.Null, "Node should not still claim the original parent");
		}

		/// <summary>
		/// Can unlink a node twice in a row, or if a node is already at the root of a hierarchy.
		/// </summary>
		[Test]
		public void CanUnlinkTwice()
		{
			var parent = new ConfigurableDictionaryNode() { Children = new List<ConfigurableDictionaryNode>(), Parent = null };
			var node = new ConfigurableDictionaryNode() { Children = new List<ConfigurableDictionaryNode>(), Parent = parent };
			parent.Children.Add(node);
			node.UnlinkFromParent();
			Assert.That(node.Parent, Is.Null); // node is now at the root of a hierarchy
			// SUT
			Assert.DoesNotThrow(() => node.UnlinkFromParent());
		}

		[Test]
		public void CanChangeSuffix()
		{
			var parent = new ConfigurableDictionaryNode() { Children = new List<ConfigurableDictionaryNode>(), Parent = null };

			var originallabel = "originalLabel";
			var node = new ConfigurableDictionaryNode() { Parent = parent, Label = originallabel, LabelSuffix = "orig"};
			parent.Children.Add(node);

			var newSuffix = "new";
			// SUT
			node.ChangeSuffix(newSuffix);
			Assert.That(node.LabelSuffix, Is.EqualTo(newSuffix), "suffix was not updated");
			Assert.That(node.Label, Is.EqualTo(originallabel), "should not have changed label");
		}

		[Test]
		public void CanAddInitialSuffix()
		{
			var parent = new ConfigurableDictionaryNode() { Children = new List<ConfigurableDictionaryNode>(), Parent = null };

			var originallabel = "originalLabel";
			var node = new ConfigurableDictionaryNode() { Parent = parent, Label = originallabel, LabelSuffix = null };
			parent.Children.Add(node);

			var newSuffix = "new";
			// SUT
			node.ChangeSuffix(newSuffix);
			Assert.That(node.LabelSuffix, Is.EqualTo(newSuffix), "suffix was not updated");
			Assert.That(node.Label, Is.EqualTo(originallabel), "should not have changed label");
		}

		[Test]
		public void ReportSuccessfulChangedSuffix()
		{
			var parent = new ConfigurableDictionaryNode() { Children = new List<ConfigurableDictionaryNode>(), Parent = null };
			var node = new ConfigurableDictionaryNode() { Parent = parent, Label = "originalLabel",LabelSuffix = "blah" };
			parent.Children.Add(node);

			// SUT
			var result = node.ChangeSuffix("new");
			Assert.That(result, Is.True);
		}

		[Test]
		public void CantHaveTwoSiblingsWithSameNonNullSuffix()
		{
			var parent = new ConfigurableDictionaryNode() { Children = new List<ConfigurableDictionaryNode>(), Parent = null };
			var originalLabel = "originalLabel";
			var originalSuffix = "originalSuffix";
			var node = new ConfigurableDictionaryNode() { Parent = parent, Label = originalLabel, LabelSuffix = originalSuffix};
			var otherNode = new ConfigurableDictionaryNode() { Parent = parent, Label = originalLabel, LabelSuffix = "otherSuffix"};
			parent.Children.Add(node);
			parent.Children.Add(otherNode);

			// SUT
			var result = node.ChangeSuffix(otherNode.LabelSuffix);
			Assert.That(result, Is.False, "Should have reported failure to change suffix");
			Assert.That(node.LabelSuffix, Is.EqualTo(originalSuffix), "Should not have changed suffix");
		}

		[Test]
		public void CanRequestChangingSuffixToSameSuffix()
		{
			var parent = new ConfigurableDictionaryNode() { Children = new List<ConfigurableDictionaryNode>(), Parent = null };
			var originalLabel = "originalLabel";
			var originalSuffix = "blah";
			var node = new ConfigurableDictionaryNode() { Parent = parent, Label = originalLabel, LabelSuffix = originalSuffix };
			parent.Children.Add(node);

			// SUT
			var result = node.ChangeSuffix(originalSuffix);
			Assert.That(result, Is.True, "Report success when requesting a suffix that is already the suffix");
			Assert.That(node.LabelSuffix, Is.EqualTo(originalSuffix), "Should not have changed suffix");
		}

		[Test]
		public void CanChangeSuffixOfRootNode()
		{
			var rootNode = new ConfigurableDictionaryNode() { Parent = null, Label = "rootNode",LabelSuffix = "orig" };
			var rootNodes = new List<ConfigurableDictionaryNode>() { rootNode };

			// SUT
			var result = rootNode.ChangeSuffix("new", rootNodes);
			Assert.That(result, Is.True, "allow changing suffix of root");
			Assert.That(rootNode.LabelSuffix, Is.EqualTo("new"), "failed to change suffix");
		}

		[Test]
		public void Equals_SameLabelsAndSuffixesAreEqual()
		{
			var firstNode = new ConfigurableDictionaryNode { Label = "same" };
			var secondNode = new ConfigurableDictionaryNode { Label = "same" };
			Assert.That(firstNode.LabelSuffix, Is.Null);
			Assert.That(secondNode.LabelSuffix, Is.Null);

			// SUT
			Assert.AreEqual(firstNode, secondNode);

			firstNode.LabelSuffix = "suffix";
			secondNode.LabelSuffix = "suffix";
			// SUT
			Assert.AreEqual(firstNode, secondNode);
		}

		[Test]
		public void Equals_OneParentNullAreNotEqual()
		{
			var firstNode = new ConfigurableDictionaryNode { Label = "same" };
			var secondNode = new ConfigurableDictionaryNode { Label = "same", Parent = firstNode };

			Assert.AreNotEqual(firstNode, secondNode);
			secondNode.Parent = null;
			firstNode.Parent = secondNode;
			Assert.AreNotEqual(firstNode, secondNode);
		}

		[Test]
		public void Equals_DifferentParentsAreNotEqual()
		{
			var firstParent = new ConfigurableDictionaryNode { Label = "firstParent" };
			var secondParent = new ConfigurableDictionaryNode { Label = "secondParent" };
			var firstNode = new ConfigurableDictionaryNode { Label = "same", Parent = firstParent };
			var secondNode = new ConfigurableDictionaryNode { Label = "same", Parent = secondParent };

			Assert.AreNotEqual(firstNode, secondNode);
		}

		[Test]
		public void Equals_DifferentLabelsAreNotEqual()
		{
			var firstNode = new ConfigurableDictionaryNode { Label = "same" };
			var secondNode = new ConfigurableDictionaryNode { Label = "different" };

			Assert.AreNotEqual(firstNode, secondNode);
		}

		[Test]
		public void Equals_DifferentSuffixesAreNotEqual()
		{
			var firstNode = new ConfigurableDictionaryNode { Label="label", LabelSuffix = "same" };
			var secondNode = new ConfigurableDictionaryNode { Label="label", LabelSuffix = "different" };

			Assert.AreNotEqual(firstNode, secondNode);
		}

		[Test]
		public void Equals_DifferentLabelsAndSuffixesAreNotEqual()
		{
			var firstNode = new ConfigurableDictionaryNode { Label = "same", LabelSuffix = "suffixA"};
			var secondNode = new ConfigurableDictionaryNode { Label = "different", LabelSuffix = "suffixB"};

			Assert.AreNotEqual(firstNode, secondNode);
		}

		[Test]
		public void Equals_SameLabelsAndSameParentsAreEqual()
		{
			var parentNode = new ConfigurableDictionaryNode { Label = "Parent" };
			var firstNode = new ConfigurableDictionaryNode { Label = "same", Parent = parentNode, LabelSuffix = null};
			var secondNode = new ConfigurableDictionaryNode { Label = "same", Parent = parentNode,LabelSuffix = null};

			Assert.AreEqual(firstNode, secondNode);
		}

		[Test]
		public void Equals_DifferentLabelsAndSameParentsAreNotEqual()
		{
			var parentNode = new ConfigurableDictionaryNode { Label = "Parent" };
			var firstNode = new ConfigurableDictionaryNode { Label = "same", Parent = parentNode, LabelSuffix = null};
			var secondNode = new ConfigurableDictionaryNode { Label = "different", Parent = parentNode, LabelSuffix = null};

			Assert.AreNotEqual(firstNode, secondNode);
		}

		[Test]
		public void Equals_DifferentSuffixesAndSameParentsAreNotEqual()
		{
			var parentNode = new ConfigurableDictionaryNode { Label = "Parent" };
			var firstNode = new ConfigurableDictionaryNode { Label="label", LabelSuffix = "same", Parent = parentNode };
			var secondNode = new ConfigurableDictionaryNode { Label="label", LabelSuffix = "different", Parent = parentNode };

			Assert.AreNotEqual(firstNode, secondNode);
		}

		[Test]
		public void HasCorrectDisplayLabel()
		{
			var nodeWithNullSuffix = new ConfigurableDictionaryNode() {Label = "label", LabelSuffix = null};
			// SUT
			Assert.That(nodeWithNullSuffix.DisplayLabel, Is.EqualTo("label"), "DisplayLabel should omit parentheses and suffix if suffix is null");

			var nodeWithSuffix = new ConfigurableDictionaryNode() { Label = "label2", LabelSuffix = "suffix2" };
			// SUT
			Assert.That(nodeWithSuffix.DisplayLabel, Is.EqualTo("label2 (suffix2)"), "DisplayLabel should include suffix");
		}
	}
}

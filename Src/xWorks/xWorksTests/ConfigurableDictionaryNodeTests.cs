// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.FDOTests;

namespace SIL.FieldWorks.XWorks
{
	public class ConfigurableDictionaryNodeTests
	{
		[Test]
		public void ChildlessCanDeepClone()
		{
			var parent = new ConfigurableDictionaryNode();
			var child = new ConfigurableDictionaryNode() { After = "after", IsEnabled = true, Parent = parent };
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
			child.Children = new List<ConfigurableDictionaryNode>() { grandchild };
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
			Assert.That(clone.Label, Is.EqualTo(node.Label));
			Assert.That(clone.Before, Is.EqualTo(node.Before));
			Assert.That(clone.After, Is.EqualTo(node.After));
			Assert.That(clone.Between, Is.EqualTo(node.Between));
			Assert.That(clone.DictionaryNodeOptions, Is.EqualTo(node.DictionaryNodeOptions));
			Assert.That(clone.IsEnabled, Is.EqualTo(node.IsEnabled));

			if (node.Children != null)
			{
				Assert.That(clone.Children.Count, Is.EqualTo(node.Children.Count));
				for (int childIndex = 0; childIndex < node.Children.Count; childIndex++)
				{
					VerifyDuplicationInner(clone.Children[childIndex], node.Children[childIndex]);
					Assert.That(clone.Children[childIndex], Is.Not.SameAs(node.Children[childIndex]), "Didn't deep-clone");
					Assert.That(clone.Children[childIndex].Parent, Is.SameAs(clone), "cloned children were not re-parented within deep-cloned object");
					Assert.That(clone.Children[childIndex].Parent, Is.Not.SameAs(node.Children[childIndex].Parent), "Cloned children should be pointing to different parent nodes than the original");
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
	}
}

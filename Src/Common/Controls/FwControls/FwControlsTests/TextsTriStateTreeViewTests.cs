// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;
using NUnit.Framework;

#pragma warning disable 1591 // no XML comments needed in tests
namespace SIL.FieldWorks.Common.Controls
{
	[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "MethodName_TestName is standard for tests")]
	public class TextsTriStateTreeViewTests
	{
		private TreeNode m_bibleNode, m_testamentNode, m_bookNode;
		private const string ksVersesText = "1:1-4:12";
		private const string ksFootnoteText = "1:1-4:12 Footnote(3:14)";

		[Test]
		public void ExpandToBooks_ExpandsBibleAndTestamentsButNotBooks()
		{
			using (var treeView = CreateViewWithEmptyBook())
			{
				treeView.ExpandToBooks();
				Assert.True(m_bibleNode.IsExpanded, "Bible should be expanded");
				Assert.True(m_testamentNode.IsExpanded, "Testaments should be expanded");
				Assert.False(m_bookNode.IsExpanded, "Books should not be expanded");
			}
		}

		[Test]
		public void ExpandToBooks_DoesNotFillInVerses()
		{
			using (var treeView = CreateViewWithEmptyBook())
			{
				treeView.ExpandToBooks();
				Assert.AreEqual(1, m_bookNode.Nodes.Count, "The only node under Book should be the dummy node");
				Assert.IsInstanceOf<int>(m_bookNode.Tag, "Placeholder int Tag should not have been replaced");
				var subNode = m_bookNode.Nodes[0];
				Assert.AreEqual(TextsTriStateTreeView.ksDummyName, subNode.Text, "Incorrect Text");
				Assert.AreEqual(TextsTriStateTreeView.ksDummyName, subNode.Name, "Incorrect Name");
			}
		}

		[Test]
		public void ExpandBook_FillsInVerses()
		{
			using (CreateViewWithEmptyBook())
			{
				m_bookNode.Expand();
				Assert.AreEqual(2, m_bookNode.Nodes.Count, "Both Verses and Footnote should have been added");
				Assert.IsInstanceOf<DummyBook>(m_bookNode.Tag, "The Tag should have been replaced with a Book");
				Assert.AreEqual(ksVersesText, m_bookNode.Nodes[0].Text, "The Verses node should be first");
				Assert.AreEqual(ksFootnoteText, m_bookNode.Nodes[1].Text, "The Footnote node should be second");
			}
		}

		private TextsTriStateTreeView CreateViewWithEmptyBook()
		{
			var treeView = new TestTextsTriStateTreeView();
			var dummyVersesNode = new TreeNode(TextsTriStateTreeView.ksDummyName) { Name = TextsTriStateTreeView.ksDummyName };
			m_bookNode = new TreeNode("II Hezekiah", new[] { dummyVersesNode }) { Name = "Book", Tag = 7 };
			m_testamentNode = new TreeNode(FwControls.kstidOtNode, new[] { m_bookNode }) { Name = "Testament" };
			m_bibleNode = new TreeNode(FwControls.kstidBibleNode, new[] { m_testamentNode }) { Name = "Bible" };
			treeView.Nodes.Add(m_bibleNode);
			EnableEventHandling(treeView);
			return treeView;
		}

		/// <remarks>
		/// These tests test whether events are firing at the proper time. Windows Forms Events fire only if Handle is created for the Control.
		/// Reading AccessibilityObject has a side effect of creating Handle.
		/// </remarks>
		[SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "get AccessibilityObject has side effects")]
		private static void EnableEventHandling(Control control)
		{
			Assert.NotNull(control.AccessibilityObject);
			Assert.True(control.IsHandleCreated, "Handle not created; tests are invalid");
		}

#region private classes
		private class TestTextsTriStateTreeView : TextsTriStateTreeView
		{
			protected override bool FillInBookChildren(TreeNode bookNode)
			{
				bookNode.Tag = new DummyBook();
				bookNode.Nodes.Clear();
				bookNode.Nodes.AddRange(new[] { new TreeNode(ksVersesText), new TreeNode(ksFootnoteText) });
				return true;
			}
		}

		private class DummyBook {} // Didn't feel like implementing or even mocking IScrBook
#endregion private classes
	}
}

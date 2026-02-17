// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Reflection;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

#pragma warning disable 1591 // no XML comments needed in tests
namespace SIL.FieldWorks.IText
{
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
				Assert.That(m_bibleNode.IsExpanded, Is.True, "Bible should be expanded");
				Assert.That(m_testamentNode.IsExpanded, Is.True, "Testaments should be expanded");
				Assert.That(m_bookNode.IsExpanded, Is.False, "Books should not be expanded");
			}
		}

		[Test]
		public void ExpandToBooks_DoesNotFillInVerses()
		{
			using (var treeView = CreateViewWithEmptyBook())
			{
				treeView.ExpandToBooks();
				Assert.That(m_bookNode.Nodes.Count, Is.EqualTo(1), "The only node under Book should be the dummy node");
				Assert.That(m_bookNode.Tag, Is.InstanceOf<int>(), "Placeholder int Tag should not have been replaced");
				var subNode = m_bookNode.Nodes[0];
				Assert.That(subNode.Text, Is.EqualTo(TextsTriStateTreeView.ksDummyName), "Incorrect Text");
				Assert.That(subNode.Name, Is.EqualTo(TextsTriStateTreeView.ksDummyName), "Incorrect Name");
			}
		}

		[Test]
		public void ExpandBook_FillsInVerses()
		{
			using (CreateViewWithEmptyBook())
			{
				m_bookNode.Expand();
				Assert.That(m_bookNode.Nodes.Count, Is.EqualTo(2), "Both Verses and Footnote should have been added");
				Assert.That(m_bookNode.Tag, Is.InstanceOf<DummyBook>(), "The Tag should have been replaced with a Book");
				Assert.That(m_bookNode.Nodes[0].Text, Is.EqualTo(ksVersesText), "The Verses node should be first");
				Assert.That(m_bookNode.Nodes[1].Text, Is.EqualTo(ksFootnoteText), "The Footnote node should be second");
			}
		}

		private TextsTriStateTreeView CreateViewWithEmptyBook()
		{
			var treeView = new TestTextsTriStateTreeView();
			var dummyVersesNode = new TreeNode(TextsTriStateTreeView.ksDummyName) { Name = TextsTriStateTreeView.ksDummyName };
			m_bookNode = new TreeNode("II Hezekiah", new[] { dummyVersesNode }) { Name = "Book", Tag = 7 };
			m_testamentNode = new TreeNode(ITextStrings.kstidOtNode, new[] { m_bookNode }) { Name = "Testament" };
			m_bibleNode = new TreeNode(ITextStrings.kstidBibleNode, new[] { m_testamentNode }) { Name = "Bible" };
			treeView.Nodes.Add(m_bibleNode);
			EnableEventHandling(treeView);
			return treeView;
		}

		/// <remarks>
		/// These tests test whether events are firing at the proper time. Windows Forms Events fire only if Handle is created for the Control.
		/// Reading AccessibilityObject has a side effect of creating Handle.
		/// </remarks>
		private static void EnableEventHandling(Control control)
		{
			Assert.That(control.AccessibilityObject, Is.Not.Null);
			Assert.That(control.IsHandleCreated, Is.True, "Handle not created; tests are invalid");
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

	[TestFixture]
	public class TextsTriStateTreeViewDisposeTests : MemoryOnlyBackendProviderTestBase
	{
		[Test]
		public void Dispose_ClearsCacheReferences()
		{
			var view = new TextsTriStateTreeView();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
				Cache.LanguageProject.TranslatedScriptureOA = Cache.ServiceLocator
					.GetInstance<IScriptureFactory>()
					.Create());
			view.Cache = Cache;

			var cacheField = typeof(TextsTriStateTreeView).GetField("m_cache",
				BindingFlags.Instance | BindingFlags.NonPublic);
			var stylesheetField = typeof(TextsTriStateTreeView).GetField("m_scriptureStylesheet",
				BindingFlags.Instance | BindingFlags.NonPublic);
			var scrField = typeof(TextsTriStateTreeView).GetField("m_scr",
				BindingFlags.Instance | BindingFlags.NonPublic);

			Assert.That(cacheField?.GetValue(view), Is.Not.Null, "Expected cache to be set.");
			Assert.That(stylesheetField?.GetValue(view), Is.Not.Null, "Expected stylesheet to be set.");
			Assert.That(scrField?.GetValue(view), Is.Not.Null, "Expected scripture to be set.");

			view.Dispose();

			Assert.That(cacheField?.GetValue(view), Is.Null, "Expected cache to be cleared.");
			Assert.That(stylesheetField?.GetValue(view), Is.Null, "Expected stylesheet to be cleared.");
			Assert.That(scrField?.GetValue(view), Is.Null, "Expected scripture to be cleared.");
		}
	}
}

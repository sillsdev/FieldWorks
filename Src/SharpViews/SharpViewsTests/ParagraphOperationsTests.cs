using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.SharpViews.Builders;
using SIL.FieldWorks.SharpViews.Hookups;
using SIL.FieldWorks.SharpViews.Selections;

namespace SIL.FieldWorks.SharpViews.SharpViewsTests
{
	/// <summary>
	/// Test the default implementation of ParagraphOperations
	/// </summary>
	[TestFixture]
	public class ParagraphOperationsTests : GraphicsTestBase
	{
		private ITsStrFactory m_tsf;
		MockWsf m_wsf = new MockWsf();
		private int m_wsEng;
		private int m_wsFrn;

		[SetUp]
		public void MySetup()
		{
			Setup();
			m_tsf = TsStrFactoryClass.Create();
			m_wsEng = m_wsf.GetWsFromStr("en");
			m_wsFrn = m_wsf.GetWsFromStr("fr");
		}

		[Test]
		public void SendingInsertLineBreak()
		{
			var owner = new MockData1(55, 77);
			var child1 = new MockData1(55, 77);
			var child2 = new MockData1(55, 77);
			owner.InsertIntoObjSeq1(0, child1);
			owner.InsertIntoObjSeq1(1, child2);
			child1.SimpleThree = "Hello World. ";
			child2.SimpleThree = "This is a test.";

			var styles = new AssembledStyles();
			var root = new RootBoxFdo(styles);
			var layoutInfo = HookupTests.MakeLayoutInfo(int.MaxValue/2, m_gm.VwGraphics, 55);
			SetupFakeRootSite(root);
			var po = new MockSendParagraphOperations();
			root.Builder.Show(Display.Of(() => owner.ObjSeq1).Using((bldr, md) => bldr.AddString(() => md.SimpleThree, 55))
				.EditParagraphsUsing(po));
			Assert.That(po.Hookup, Is.Not.Null);
			root.Layout(layoutInfo);
			var para = (ParaBox) root.FirstBox;
			var ip = para.SelectAtEnd();
			ip.InsertLineBreak();
			Assert.That(po.InsertFollowingParagraphIp, Is.EqualTo(ip), "should have called InsertFollowingParagraph with right IP");
			Assert.That(po.InsertFollowingParagraphActionPerformed, Is.False, "make selection action performed too soon");
			((MockSite)root.Site).DoPendingAfterNotificationTasks();
			Assert.That(po.InsertFollowingParagraphActionPerformed, Is.True, "make selection action not performed or not saved");

			po.InsertFollowingParagraphIp = null;
			ip = para.SelectAtStart();
			ip.InsertLineBreak();
			Assert.That(po.InsertFollowingParagraphIp, Is.Null);
			Assert.That(po.InsertPrecedingParagraphIp, Is.EqualTo(ip), "should have called InsertPrecedingParagraph with right IP");
			Assert.That(po.InsertPrecedingParagraphActionPerformed, Is.False, "make selection action performed too soon");
			((MockSite)root.Site).DoPendingAfterNotificationTasks();
			Assert.That(po.InsertPrecedingParagraphActionPerformed, Is.True, "make selection action not performed or not saved");
			// Todo: test sending InsertLineBreak when selection mid-paragraph, also empty paragraph.
		}

		// This was an attempt at testing the receiving of InsertLineBreak, without depending on all the
		// infrastructure that sets up paragraph sequences and hookups. It was too difficult to get right.
		//[Test]
		//public void ReceivingInsertLineBreaks()
		//{
		//    var list = new List<MockData1>();
		//    var item1 = new MockData1(55, 77);
		//    var item2 = new MockData1(55, 77);
		//    list.Add(item1);
		//    list.Add(item2);
		//    var po = new MockReceiveParagraphOperations();
		//    po.List = list;
		//    var hookup = new MockSequenceHookup(hookup1 => DummyEvent += hookup1.PropChanged,
		//                                        hookup1 => DummyEvent -= hookup1.PropChanged);
		//    po.Hookup = hookup;
		//    var itemHookup = new ItemHookup(item1,null);
		//    hookup.InsertChildHookup(itemHookup, 0);
		//    hookup.InsertChildHookup(new ItemHookup(item2, null), 1);
		//    var stringHookup = new LiteralStringParaHookup(item1, null);
		//    itemHookup.InsertChildHookup(stringHookup, 0);
		//    item1.SimpleThree = "Hello world";
		//    var ip = new InsertionPoint(stringHookup, "Hello world".Length, true);
		//    Action makeSelection;
		//    po.InsertFollowingParagraph(ip, out makeSelection);
		//    Assert.That(list, Has.Count.EqualTo(3), "should insert a new para object");
		//    Assert.That(list[0], Is.EqualTo(item1));
		//    Assert.That(list[2], Is.EqualTo(item2));
		//    Assert.That(list[1], Is.Not.Null);
		//    // Verify that a selection is established at the start of the new paragraph.
		//    // We need to make an item hookup for the new item in the appropriate place in the main hookup,
		//    var styles = new AssembledStyles();
		//    var root = new RootBox(styles);
		//    var itemNew = list[1];
		//    ParaBox para = MakePara(hookup, styles, itemNew, "something", root);
		//    // and then call makeSelection;
		//    makeSelection();
		//    Assert.That(root.Selection, Is.AssignableTo(typeof(InsertionPoint)));
		//    var ipNew = (InsertionPoint)root.Selection;
		//    Assert.That(ipNew.Para, Is.EqualTo(para));

		//    // Now we have an IP at the start of the following paragraph, we're all set to test insert at start.
		//    po.InsertPrecedingParagraph(ipNew, out makeSelection);
		//    Assert.That(list, Has.Count.EqualTo(4), "should insert a new para object");
		//    Assert.That(list[0], Is.EqualTo(item1));
		//    Assert.That(list[2], Is.EqualTo(itemNew));
		//    Assert.That(list[3], Is.EqualTo(item2));
		//    Assert.That(list[1], Is.Not.Null);
		//    var itemNew2 = list[1];
		//    ParaBox para2 = MakePara(hookup, styles, itemNew, "new contents", root);
		//    makeSelection(); // actually does nothing in this case.
		//    Assert.That(root.Selection, Is.AssignableTo(typeof(InsertionPoint)));
		//    ipNew = (InsertionPoint)root.Selection;
		//    Assert.That(ipNew.Para, Is.EqualTo(para));

		//    // Now try insert in middle.
		//    ipNew = (InsertionPoint)SelectionBuilder.In(hookup)[2].Offset(4).Install();
		//    po.SplitParagraph(ipNew, out makeSelection);
		//    Assert.That(list, Has.Count.EqualTo(5), "should insert a new para object");
		//    Assert.That(list[0], Is.EqualTo(item1));
		//    Assert.That(list[2], Is.EqualTo(itemNew));
		//    Assert.That(list[4], Is.EqualTo(item2));
		//    Assert.That(list[3], Is.Not.Null);
		//    var itemNew3 = list[3];
		//    ParaBox para3 = MakePara(hookup, styles, itemNew, "contents", root);
		//    makeSelection();
		//    Assert.That(root.Selection, Is.AssignableTo(typeof(InsertionPoint)));
		//    ipNew = (InsertionPoint)root.Selection;
		//    Assert.That(ipNew.Para, Is.EqualTo(para3));
		//}

		/// <summary>
		/// This test is a bit of an integration test, since for it to work right, a lot of ParaBox,
		/// ViewBuilder, SelectionBuilder, InsertionPoint, and various hookups have to work right, in
		/// addition to the ParagraphOperations methods we are really trying to test. But (see above)
		/// it's much messier to set up for testing this object in relative isolation.
		/// </summary>
		[Test]
		public void CombinedParaOperations()
		{
			// Set up a simple multi-para layout using the ObjSeq2 and SimpleThree properties of MockData1
			AssembledStyles styles = new AssembledStyles();
			RootBox root = new RootBoxFdo(styles);
			var owner = new MockData1();
			var p1 = new MockData1(10,11);
			var p2 = new MockData1(10,11);
			p1.SimpleThree = "Hello World";
			p2.SimpleThree = "This is a test";
			owner.ObjSeq2.Add(p1);
			owner.ObjSeq2.Add(p2);
			var po = new MockReceiveParagraphOperations();
			root.Builder.Show(Display.Of(() => owner.ObjSeq2)
				.Using((builder, para) => builder.AddString(() => para.SimpleThree, 10))
				.EditParagraphsUsing(po));
			SetupFakeRootSite(root);
			var layoutInfo = HookupTests.MakeLayoutInfo(int.MaxValue / 2, m_gm.VwGraphics, 55);
			root.Layout(layoutInfo);
			var ip = root.FirstBox.SelectAtEnd();

			// Insert at end of paragraph.
			ip.InsertLineBreak();
			Assert.That(owner.ObjSeq2.Count, Is.EqualTo(3));
			Assert.That(owner.ObjSeq2[0], Is.EqualTo(p1));
			Assert.That(owner.ObjSeq2[2], Is.EqualTo(p2));
			var p3 = owner.ObjSeq2[1]; // empty inserted paragraph.
			((MockSite)root.Site).DoPendingAfterNotificationTasks();
			ip = (InsertionPoint) root.Selection;
			Assert.That(root.FirstBox.Next.Next, Is.Not.Null);
			Assert.That(ip.StringPosition, Is.EqualTo(0));
			Assert.That(ip.Para, Is.EqualTo(root.FirstBox.Next));

			// Insert at start of paragraph. To make it unambiguously the start, give p3 some data,
			// and since that might eventually destroy the selection, reset it.
			var rootHookup = ip.Hookup.ParentHookup.ParentHookup; // StringHookup, ItemHookup, Sequence
			p3.SimpleThree = "First insert";
			ip = (InsertionPoint)SelectionBuilder.In(rootHookup)[1].Offset(0).Install();
			ip.InsertLineBreak();
			Assert.That(owner.ObjSeq2.Count, Is.EqualTo(4));
			Assert.That(owner.ObjSeq2[0], Is.EqualTo(p1));
			Assert.That(owner.ObjSeq2[2], Is.EqualTo(p3));
			Assert.That(owner.ObjSeq2[3], Is.EqualTo(p2));
			var p4 = owner.ObjSeq2[1]; // empty inserted paragraph (before p3).
			Assert.That(root.FirstBox.Next.Next.Next, Is.Not.Null);
			((MockSite)root.Site).DoPendingAfterNotificationTasks();
			ip = (InsertionPoint)root.Selection;
			Assert.That(ip.StringPosition, Is.EqualTo(0));
			Assert.That(ip.Para, Is.EqualTo(root.FirstBox.Next.Next));

			// Split a paragraph.
			var oldObjects = owner.ObjSeq2.ToArray();
			ip = (InsertionPoint)SelectionBuilder.In(rootHookup)[2].Offset("First ".Length).Install();
			ip.InsertLineBreak();
			Assert.That(owner.ObjSeq2.Count, Is.EqualTo(5));
			Assert.That(owner.ObjSeq2[1], Is.EqualTo(oldObjects[1]));
			Assert.That(owner.ObjSeq2[2], Is.EqualTo(oldObjects[2]));
			Assert.That(owner.ObjSeq2[4], Is.EqualTo(oldObjects[3])); // insert between 2 and 3
			var p5 = owner.ObjSeq2[3]; // inserted paragraph.
			((MockSite)root.Site).DoPendingAfterNotificationTasks();
			ip = (InsertionPoint)root.Selection;
			Assert.That(ip.StringPosition, Is.EqualTo(0));
			Assert.That(ip.Para, Is.EqualTo(root.FirstBox.Next.Next.Next));
			Assert.That(owner.ObjSeq2[2].SimpleThree, Is.EqualTo("First "));
			Assert.That(owner.ObjSeq2[3].SimpleThree, Is.EqualTo("insert"));

			// Combine two paragraphs by backspace at start of line.
			oldObjects = owner.ObjSeq2.ToArray();
			ip.Backspace();
			Assert.That(owner.ObjSeq2.Count, Is.EqualTo(4));
			Assert.That(owner.ObjSeq2[1], Is.EqualTo(oldObjects[1]));
			Assert.That(owner.ObjSeq2[2], Is.EqualTo(oldObjects[2]));
			Assert.That(owner.ObjSeq2[3], Is.EqualTo(oldObjects[4])); // delete item 3
			((MockSite)root.Site).DoPendingAfterNotificationTasks();
			ip = (InsertionPoint)root.Selection;
			Assert.That(ip.StringPosition, Is.EqualTo("First ".Length));
			Assert.That(ip.Para, Is.EqualTo(root.FirstBox.Next.Next));
			Assert.That(owner.ObjSeq2[2].SimpleThree, Is.EqualTo("First insert"));
		}

		private ParaBox MakePara(MockSequenceHookup hookup, AssembledStyles styles, MockData1 itemNew, string contents, RootBox root)
		{
			var newItemHookup = new ItemHookup(itemNew, null);
			hookup.InsertChildHookup(newItemHookup, 1);
			// Give that hookup a stringhookup child, connected to a paragraph and root box.
			var clientRuns = new List<ClientRun>();
			itemNew.SimpleThree = contents;
			var run = new StringClientRun(contents, styles);
			clientRuns.Add(run);
			var source = new TextSource(clientRuns, null);
			var para = new ParaBox(styles, source);
			run.Hookup = new LiteralStringParaHookup(itemNew, para);
			newItemHookup.InsertChildHookup(run.Hookup, 0);
			root.AddBox(para);
			return para;
		}

		private void SetupFakeRootSite(RootBox root)
		{
			PaintTransform ptrans = new PaintTransform(2, 4, 96, 100, 0, 10, 120, 128);
			MockSite site = new MockSite();
			site.m_transform = ptrans;
			site.m_vwGraphics = m_gm.VwGraphics;
			root.Site = site;
		}
		private event EventHandler<EventArgs> DummyEvent;
	}

	/// <summary>
	///  Mocks ParagraphOperations for purposes of testing sending the message
	/// </summary>
	class MockSendParagraphOperations : ParagraphOperations<MockData1>
	{
		public InsertionPoint InsertFollowingParagraphIp { get; set; }
		public bool InsertFollowingParagraphActionPerformed { get; set; }
		public override bool InsertFollowingParagraph(InsertionPoint ip, out Action makeSelection)
		{
			InsertFollowingParagraphIp = ip;
			makeSelection = () => InsertFollowingParagraphActionPerformed = true;
			return true;
		}

		public InsertionPoint InsertPrecedingParagraphIp { get; set; }
		public bool InsertPrecedingParagraphActionPerformed { get; set; }
		public override bool InsertPrecedingParagraph(InsertionPoint ip, out Action makeSelection)
		{
			InsertPrecedingParagraphIp = ip;
			makeSelection = () => InsertPrecedingParagraphActionPerformed = true;
			return true;
		}
	}
	/// <summary>
	///  Mocks ParagraphOperations for purposes of testing receiving the message
	/// </summary>
	class MockReceiveParagraphOperations : ParagraphOperations<MockData1>
	{
		public override void SetString(MockData1 destination, string val)
		{
			destination.SimpleThree = val;
		}
	}

	class MockSequenceHookup: SequenceHookup<MockData1>
	{
		public MockSequenceHookup(Action<IReceivePropChanged> hookEvent, Action<IReceivePropChanged> unhookEvent)
			: base(null, null, null, hookEvent, unhookEvent)
		{
		}
	}
}

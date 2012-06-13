using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.SharpViews.Builders;
using SIL.FieldWorks.SharpViews.Hookups;
using SIL.FieldWorks.SharpViews.Selections;

namespace SIL.FieldWorks.SharpViews.SharpViewsTests
{
	/// <summary>
	/// These tests depend on FDO and test operations on 'real' FDO objects.
	/// </summary>
	[TestFixture]
	public class StTextTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		// This is common to GraphicsTestBase, but it's more useful here to inherit the FDO stuff.
		internal GraphicsManager m_gm;
		internal Graphics m_graphics;
		[SetUp]
		public void Setup()
		{
			Bitmap bmp = new Bitmap(200, 100);
			m_graphics = Graphics.FromImage(bmp);

			m_gm = new GraphicsManager(null, m_graphics);
		}

		[TearDown]
		public void Teardown()
		{
			m_gm.Dispose();
			m_gm = null;
			m_graphics.Dispose();
		}
		/// <summary>
		/// This test is a bit of an integration test, since for it to work right, a lot of ParaBox,
		/// ViewBuilder, SelectionBuilder, InsertionPoint, and various hookups have to work right, in
		/// addition to the ParagraphOperations methods we are really trying to test. It also functions
		/// as an initial test for some of these operations at the FDO level.
		/// Compare this test with ParagraphOperationsTests.CombinedParaOperations.
		/// </summary>
		[Test]
		public void CombinedParaOperationsFdo()
		{
			// Set up a simple multi-para layout using the ObjSeq2 and SimpleThree properties of MockData1
			AssembledStyles styles = new AssembledStyles();
			var root = new RootBoxFdo(styles);
			var text = Cache.ServiceLocator.GetInstance<ITextFactory>().Create();
			//Cache.LangProject.TextsOC.Add(text);
			var owner = Cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
			text.ContentsOA = owner;
			IStTxtPara p1 = MakePara(owner, "Hello World");
			var p2 = MakePara(owner,"This is a test");
			m_actionHandler.EndUndoTask(); // Finish settin up data, we want to control from now on.

			var po = new StTextParagraphOperations(owner);
			root.Builder.Show(Display.Of(() => owner.ParagraphsOS).Using((builder, para) => builder.AddString(()
				=> ((IStTxtPara)para).Contents)).EditParagraphsUsing(po));
			SetupFakeRootSite(root);
			var layoutInfo = HookupTests.MakeLayoutInfo(int.MaxValue / 2, m_gm.VwGraphics, 55);
			root.Layout(layoutInfo);
			var ip = root.FirstBox.SelectAtEnd();

			// Insert at end of paragraph.
			m_actionHandler.BeginUndoTask("undo insert line break", "redo insert");
			ip.InsertLineBreak();
			m_actionHandler.EndUndoTask();
			Assert.That(owner.ParagraphsOS.Count, Is.EqualTo(3));
			Assert.That(owner.ParagraphsOS[0], Is.EqualTo(p1));
			Assert.That(owner.ParagraphsOS[2], Is.EqualTo(p2));
			var p3 = (IStTxtPara)owner.ParagraphsOS[1]; // empty inserted paragraph.
			Assert.That(p3.Contents.Length, Is.EqualTo(0));
			Assert.That(p3.Contents.get_WritingSystem(0), Is.EqualTo(Cache.DefaultVernWs));
			((MockSite)root.Site).DoPendingAfterNotificationTasks();
			ip = (InsertionPoint)root.Selection;
			Assert.That(root.FirstBox.Next.Next, Is.Not.Null);
			Assert.That(ip.StringPosition, Is.EqualTo(0));
			Assert.That(ip.Para, Is.EqualTo(root.FirstBox.Next));

			// Insert at start of paragraph. To make it unambiguously the start, give p3 some data,
			// and since that might eventually destroy the selection, reset it.
			var rootHookup = ip.Hookup.ParentHookup.ParentHookup; // StringHookup, ItemHookup, Sequence
			UndoableUnitOfWorkHelper.Do("adjust contents", "redo", m_actionHandler,
				() => p3.Contents = Cache.TsStrFactory.MakeString("First insert", Cache.DefaultVernWs));
			ip = (InsertionPoint)SelectionBuilder.In(rootHookup)[1].Offset(0).Install();
			m_actionHandler.BeginUndoTask("undo insert line break", "redo insert");
			ip.InsertLineBreak();
			m_actionHandler.EndUndoTask();
			Assert.That(owner.ParagraphsOS.Count, Is.EqualTo(4));
			Assert.That(owner.ParagraphsOS[0], Is.EqualTo(p1));
			Assert.That(owner.ParagraphsOS[2], Is.EqualTo(p3));
			Assert.That(owner.ParagraphsOS[3], Is.EqualTo(p2));
			var p4 = (IStTxtPara)owner.ParagraphsOS[1]; // empty inserted paragraph (before p3).
			Assert.That(root.FirstBox.Next.Next.Next, Is.Not.Null);
			Assert.That(p4.Contents.get_WritingSystem(0), Is.EqualTo(Cache.DefaultVernWs));
			((MockSite)root.Site).DoPendingAfterNotificationTasks();
			ip = (InsertionPoint)root.Selection;
			Assert.That(ip.StringPosition, Is.EqualTo(0));
			Assert.That(ip.Para, Is.EqualTo(root.FirstBox.Next.Next));

			// Split a paragraph.
			var oldObjects = owner.ParagraphsOS.ToArray();
			ip = (InsertionPoint)SelectionBuilder.In(rootHookup)[2].Offset("First ".Length).Install();
			m_actionHandler.BeginUndoTask("undo insert line break", "redo insert");
			ip.InsertLineBreak();
			m_actionHandler.EndUndoTask();
			Assert.That(owner.ParagraphsOS.Count, Is.EqualTo(5));
			Assert.That(owner.ParagraphsOS[1], Is.EqualTo(oldObjects[1]));
			Assert.That(owner.ParagraphsOS[2], Is.EqualTo(oldObjects[2]));
			Assert.That(owner.ParagraphsOS[4], Is.EqualTo(oldObjects[3])); // insert between 2 and 3
			var p5 = (IStTxtPara)owner.ParagraphsOS[3]; // inserted paragraph.
			((MockSite)root.Site).DoPendingAfterNotificationTasks();
			ip = (InsertionPoint)root.Selection;
			Assert.That(ip.StringPosition, Is.EqualTo(0));
			Assert.That(ip.Para, Is.EqualTo(root.FirstBox.Next.Next.Next));
			Assert.That(((IStTxtPara)owner.ParagraphsOS[2]).Contents.Text, Is.EqualTo("First "));
			Assert.That(((IStTxtPara)owner.ParagraphsOS[3]).Contents.Text, Is.EqualTo("insert"));

			// Combine two paragraphs by backspace at start of line.
			oldObjects = owner.ParagraphsOS.ToArray();
			m_actionHandler.BeginUndoTask("undo backspace", "redo backspace");
			ip.Backspace();
			m_actionHandler.EndUndoTask();
			Assert.That(owner.ParagraphsOS.Count, Is.EqualTo(4));
			Assert.That(owner.ParagraphsOS[1], Is.EqualTo(oldObjects[1]));
			Assert.That(owner.ParagraphsOS[2], Is.EqualTo(oldObjects[2]));
			Assert.That(owner.ParagraphsOS[3], Is.EqualTo(oldObjects[4])); // delete item 3
			((MockSite)root.Site).DoPendingAfterNotificationTasks();
			ip = (InsertionPoint)root.Selection;
			Assert.That(ip.StringPosition, Is.EqualTo("First ".Length));
			Assert.That(ip.Para, Is.EqualTo(root.FirstBox.Next.Next));
			Assert.That(((IStTxtPara)owner.ParagraphsOS[2]).Contents.Text, Is.EqualTo("First insert"));
		}

		private IStTxtPara MakePara(IStText owner, string contents)
		{
			var paraFactory = Cache.ServiceLocator.GetInstance<IStTxtParaFactory>();
			var p1 = paraFactory.Create();
			owner.ParagraphsOS.Add(p1);
			p1.Contents = Cache.TsStrFactory.MakeString(contents, Cache.DefaultVernWs);
			return p1;
		}

		private void SetupFakeRootSite(RootBox root)
		{
			PaintTransform ptrans = new PaintTransform(2, 4, 96, 100, 0, 10, 120, 128);
			MockSite site = new MockSite();
			site.m_transform = ptrans;
			site.m_vwGraphics = m_gm.VwGraphics;
			root.Site = site;
		}
	}
}

// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// REVIEW PROBE (not part of any shipped fix) -- exists only to determine, empirically,
// what happens when a ComplexConcPatternVc-hosted PatternView is edited via the same
// low-level ReplaceWithTsString bypass used by the phon-rule-formula-readonly repro.
// ComplexConcControl leaves PatternView.ReadOnlyView = false, so this bypass is not even
// needed in production -- a plain keystroke reaches OnKeyPress first, but ReplaceWithTsString
// is exactly what IME composition or drag-and-drop would call, same as the phon-rule case.

using System.Windows.Forms;
using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Infrastructure;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.LexText.Controls;
using XCore;

namespace SIL.FieldWorks.IText
{
	[TestFixture]
	public class ComplexConcPatternVcDirectEditProbeTests : MemoryOnlyBackendProviderTestBase
	{
		private Mediator m_mediator;
		private PropertyTable m_propertyTable;
		private TestPatternView m_view;

		public override void TestSetup()
		{
			base.TestSetup();
			m_mediator = new Mediator();
			m_propertyTable = new PropertyTable(m_mediator);
			m_propertyTable.SetProperty("cache", Cache, false);
		}

		public override void TestTearDown()
		{
			if (m_view != null)
			{
				m_view.Dispose();
				m_view = null;
			}
			if (m_propertyTable != null)
			{
				m_propertyTable.Dispose();
				m_propertyTable = null;
			}
			if (m_mediator != null)
			{
				m_mediator.Dispose();
				m_mediator = null;
			}
			base.TestTearDown();
		}

		private class NullPatternControl : IPatternControl
		{
			public object GetContext(SelectionHelper sel) => null;
			public object GetContext(SelectionHelper sel, SelectionHelper.SelLimitType limit) => null;
			public object GetItem(SelectionHelper sel, SelectionHelper.SelLimitType limit) => null;
			public int GetItemContextIndex(object ctxt, object obj) => -1;
			public SelLevInfo[] GetLevelInfo(object ctxt, int index) => null;
			public int GetContextCount(object ctxt) => 0;
			public object GetNextContext(object ctxt) => null;
			public object GetPrevContext(object ctxt) => null;
			public int GetFlid(object ctxt) => 0;
		}

		private class TestPatternView : PatternView
		{
			public void CallLayout()
			{
				OnLayout(new LayoutEventArgs(this, string.Empty));
			}
		}

		/// <summary>
		/// Probe: a bare word node (no Form/Gloss/Category/InflFeatures) renders as a single
		/// line whose only content is the computed "Type: Word" text, bound to the fake tag
		/// ComplexConcPatternVc.ktagType on the synthetic (negative-hvo, non-domain) word node.
		/// ComplexConcControl leaves ReadOnlyView = false and ComplexConcPatternVc never marks
		/// this fragment ktptNotEditable, so -- unlike the phon-rule fix -- nothing blocks a
		/// direct low-level edit here at all, not even OnKeyPress (which only blocks WM_CHAR,
		/// not ReplaceWithTsString). This probe records what actually happens.
		/// </summary>
		[Test]
		public void ReplaceWithTsString_OnWordNodeTypeLine_RecordsWhatHappens()
		{
			var root = new ComplexConcGroupNode();
			var wordNode = new ComplexConcWordNode();
			root.Children.Add(wordNode);
			var model = new ComplexConcPatternModel(Cache, root);

			var vc = new ComplexConcPatternVc(Cache, m_propertyTable);
			var view = new TestPatternView { Cache = Cache, Visible = false, Width = 300, Height = 60 };
			view.Init(m_mediator, m_propertyTable, model.Root.Hvo, new NullPatternControl(), vc,
				ComplexConcPatternVc.kfragPattern, model.DataAccess);
			view.CallLayout();
			m_view = view;

			var levels = new[]
			{
				new SelLevInfo { tag = ComplexConcPatternSda.ktagChildren, ihvo = 0 }
			};
			IVwSelection sel = view.RootBox.MakeTextSelInObj(0, levels.Length, levels,
				ComplexConcPatternVc.ktagType, null, true, false, false, /* fWholeObj */ true, /* fInstall */ true);
			Assert.That(sel, Is.Not.Null,
				"could not construct a selection over the word node's Type line -- fixture/path assumption is wrong");

			ITsString replacement = TsStringUtils.MakeString("HACKED", Cache.DefaultUserWs);

			// No try/catch: if this throws, that IS the finding (an unhandled exception from a
			// direct edit on an unmarked, unaudited ComplexConcPatternVc fragment). If it does
			// not throw, the test passes and the assertions below record what changed instead.
			UndoableUnitOfWorkHelper.Do("undo", "redo", Cache.LangProject,
				() => sel.ReplaceWithTsString(replacement));

			Assert.Pass("No exception was thrown by ReplaceWithTsString on the word node's Type line.");
		}
	}
}

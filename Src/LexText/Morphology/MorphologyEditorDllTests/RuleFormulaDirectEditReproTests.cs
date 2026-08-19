// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

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

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	/// <summary>
	/// Reproduces the "phonological-rule formula cells are directly editable" bug
	/// (Docs/bugs/phon-rule-direct-editing.md) at the level that matters: does an edit that
	/// lands via the rootsite's own selection/text-replacement API (i.e. NOT via
	/// PatternView.OnKeyPress, which only filters WM_CHAR) actually corrupt the referenced
	/// PhPhoneme's real, project-wide Name?
	///
	/// This drives a real IVwRootBox (the managed Views engine) hosted by a real
	/// RegRuleFormulaControl-equivalent PatternView/RegRuleFormulaVc pair against a real
	/// in-memory LcmCache, then calls IVwSelection.ReplaceWithTsString directly -- the same
	/// low-level entry point IME composition or drag-and-drop would use, and one that
	/// PatternView.OnKeyPress never sees because it only reacts to Windows key events.
	/// </summary>
	[TestFixture]
	public class RuleFormulaDirectEditReproTests : MemoryOnlyBackendProviderTestBase
	{
		private Mediator m_mediator;
		private PropertyTable m_propertyTable;

		public override void TestSetup()
		{
			base.TestSetup();
			m_mediator = new Mediator();
			m_propertyTable = new PropertyTable(m_mediator);
			m_propertyTable.SetProperty("cache", Cache, false);
		}

		public override void TestTearDown()
		{
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

		/// <summary>Minimal no-op IPatternControl -- sufficient because we never drive
		/// selection through the chooser/insert/delete UI in this test; we only need
		/// PatternView's selection-changed handler not to crash when we install a
		/// selection directly.</summary>
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

		/// <summary>Exposes the protected layout hook so the view can be laid out headlessly,
		/// exactly like RootSiteTests' DummyBasicView.CallLayout().</summary>
		private class TestPatternView : PatternView
		{
			public void CallLayout()
			{
				OnLayout(new LayoutEventArgs(this, string.Empty));
			}
		}

		private IPhPhoneme CreatePhoneme(string name)
		{
			IPhPhoneme p = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				Cache.LangProject.PhonologicalDataOA.PhonemeSetsOS.Add(
					Cache.ServiceLocator.GetInstance<IPhPhonemeSetFactory>().Create());
				p = Cache.ServiceLocator.GetInstance<IPhPhonemeFactory>().Create();
				Cache.LangProject.PhonologicalDataOA.PhonemeSetsOS[0].PhonemesOC.Add(p);
				p.Name.SetVernacularDefaultWritingSystem(name);
			});
			return p;
		}

		/// <summary>
		/// Builds a real regular-rule RHS whose left context is a single phoneme, hosts it in a
		/// live PatternView/RegRuleFormulaVc pair, and returns the phoneme plus the live view.
		/// </summary>
		private (IPhPhoneme phoneme, TestPatternView view) BuildLiveRuleFormulaView(string phonemeName)
		{
			IPhPhoneme phoneme = CreatePhoneme(phonemeName);
			IPhSegRuleRHS rhs = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var rule = Cache.ServiceLocator.GetInstance<IPhRegularRuleFactory>().Create();
				Cache.LangProject.PhonologicalDataOA.PhonRulesOS.Add(rule);
				rhs = Cache.ServiceLocator.GetInstance<IPhSegRuleRHSFactory>().Create();
				rule.RightHandSidesOS.Add(rhs);
				var segCtxt = Cache.ServiceLocator.GetInstance<IPhSimpleContextSegFactory>().Create();
				rhs.LeftContextOA = segCtxt;
				segCtxt.FeatureStructureRA = phoneme;
			});

			var vc = new RegRuleFormulaVc(Cache, m_propertyTable);
			var view = new TestPatternView { Cache = Cache, Visible = false, Width = 300, Height = 60 };
			view.Init(m_mediator, m_propertyTable, rhs.Hvo, new NullPatternControl(), vc, RegRuleFormulaVc.kfragRHS,
				Cache.MainCacheAccessor);
			view.CallLayout();
			return (phoneme, view);
		}

		/// <summary>
		/// Selects the whole displayed phoneme (via its object path from the RHS root, bypassing
		/// any WM_CHAR-level filtering entirely -- PatternView.OnKeyPress is never invoked here)
		/// and replaces its text directly through IVwSelection.ReplaceWithTsString, exactly the
		/// kind of call an IME composition commit or a drag-and-drop would make.
		/// </summary>
		[Test]
		public void ReplaceWithTsString_OnPhonemeTerminalUnit_BypassesOnKeyPress_AndShouldNotRenameThePhoneme()
		{
			var (phoneme, view) = BuildLiveRuleFormulaView("p");

			var levels = new[]
			{
				new SelLevInfo { tag = PhSimpleContextSegTags.kflidFeatureStructure, ihvo = 0 },
				new SelLevInfo { tag = PhSegRuleRHSTags.kflidLeftContext, ihvo = 0 }
			};
			IVwSelection sel = view.RootBox.MakeTextSelInObj(0, levels.Length, levels, 0, null,
				true, false, false, /* fWholeObj */ true, /* fInstall */ true);
			Assert.That(sel, Is.Not.Null,
				"could not construct a selection over the phoneme's terminal-unit display -- fixture/path assumption is wrong");

			ITsString corrupted = TsStringUtils.MakeString("CORRUPTED", Cache.DefaultVernWs);

			// This call never goes through PatternView.OnKeyPress -- it is the rootsite's own
			// low-level text-replacement API, exactly what bypasses the WM_CHAR filter. It is
			// wrapped in a UOW only because the change-tracking infrastructure requires one for
			// any edit to commit at all; IME composition and drag-and-drop land inside a UOW
			// supplied by the real editing helper, not by PatternView.OnKeyPress.
			UndoableUnitOfWorkHelper.Do("undo", "redo", phoneme, () => sel.ReplaceWithTsString(corrupted));

			string nameAfter = phoneme.Name.VernacularDefaultWritingSystem.Text;
			Assert.That(nameAfter, Is.EqualTo("p"),
				"an edit that bypassed PatternView.OnKeyPress altered the real PhPhoneme.Name " +
				"(got '" + nameAfter + "') -- this is the project-wide-rename data corruption " +
				"described in Docs/bugs/phon-rule-direct-editing.md");
		}
	}
}

// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// Reproduction and regression coverage for the Complex Concordance pattern-builder crash.
// ComplexConcControl and the phonological rule formula editor share PatternView/
// PatternVcBase. ComplexConcPatternVc has no UpdateProp override, so an edit that reaches
// the view engine without passing through PatternView.OnKeyPress (IME composition,
// drag-and-drop, or any direct IVwSelection.ReplaceWithTsString call) falls through to
// VwBaseVc.UpdateProp, which throws NotImplementedException. Unlike the sibling rule-formula
// bug, ComplexConcPatternVc binds no real domain fields via AddStringAltMember (verified by
// inspection: zero occurrences in ComplexConcPatternVc.cs), so this is a crash, not a silent
// corruption/rename.
//
// These tests drive a real IVwRootBox (PatternView/ComplexConcPatternVc) against a real
// in-memory LcmCache and call IVwSelection.ReplaceWithTsString directly -- the same low-level
// entry point IME composition or drag-and-drop would use, and one PatternView.OnKeyPress never
// sees because it only reacts to Windows key events, not to ReplaceWithTsString.
using System.Collections.Generic;
using System.Reflection;
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
using FS = System.Collections.Generic.Dictionary<SIL.LCModel.IFsFeatDefn, object>;

namespace SIL.FieldWorks.IText
{
	[TestFixture]
	public class ComplexConcPatternVcDirectEditTests : MemoryOnlyBackendProviderTestBase
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

			public void SimulateKeyPress(char ch)
			{
				OnKeyPress(new KeyPressEventArgs(ch));
			}

			public void SimulateKeyDown(Keys key)
			{
				OnKeyDown(new KeyEventArgs(key));
			}

			public bool TestAllowDisplaySelection => AllowDisplaySelection;
		}

		private IPartOfSpeech CreatePartOfSpeech(string name, string abbr)
		{
			IPartOfSpeech pos = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				if (Cache.LangProject.PartsOfSpeechOA == null)
					Cache.LangProject.PartsOfSpeechOA = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
				pos = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create();
				Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Add(pos);
				pos.Name.SetAnalysisDefaultWritingSystem(name);
				pos.Abbreviation.SetAnalysisDefaultWritingSystem(abbr);
			});
			return pos;
		}

		private ICmPossibility CreateTag(string name, string abbr)
		{
			ICmPossibility tag = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				if (Cache.LangProject.TextMarkupTagsOA == null)
					Cache.LangProject.TextMarkupTagsOA = Cache.LangProject.GetDefaultTextTagList();
				tag = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
				Cache.LangProject.TextMarkupTagsOA.PossibilitiesOS.Add(tag);
				tag.Name.SetAnalysisDefaultWritingSystem(name);
				tag.Abbreviation.SetAnalysisDefaultWritingSystem(abbr);
			});
			return tag;
		}

		private IFsClosedFeature CreateClosedFeature(string name, out IFsSymFeatVal value)
		{
			IFsClosedFeature feat = null;
			IFsSymFeatVal val = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				IFsFeatureSystem featSys = Cache.LanguageProject.MsFeatureSystemOA;
				feat = Cache.ServiceLocator.GetInstance<IFsClosedFeatureFactory>().Create();
				featSys.FeaturesOC.Add(feat);
				feat.Name.SetAnalysisDefaultWritingSystem(name);
				feat.Abbreviation.SetAnalysisDefaultWritingSystem(name);
				val = Cache.ServiceLocator.GetInstance<IFsSymFeatValFactory>().Create();
				feat.ValuesOC.Add(val);
				val.Name.SetAnalysisDefaultWritingSystem("v1");
				val.Abbreviation.SetAnalysisDefaultWritingSystem("v1");
			});
			value = val;
			return feat;
		}

		/// <summary>
		/// Builds a live PatternView/ComplexConcPatternVc pair over a one-child pattern and
		/// returns the view plus the model root (so callers can add children before use).
		/// </summary>
		private (ComplexConcPatternModel model, TestPatternView view) BuildView()
		{
			var model = new ComplexConcPatternModel(Cache);
			var vc = new ComplexConcPatternVc(Cache, m_propertyTable);
			var view = new TestPatternView { Cache = Cache, Visible = false, Width = 300, Height = 60 };
			view.Init(m_mediator, m_propertyTable, model.Root.Hvo, new NullPatternControl(), vc,
				ComplexConcPatternVc.kfragPattern, model.DataAccess);
			m_view = view;
			return (model, view);
		}

		/// <summary>
		/// Selects the fragment rendered under fake tag <paramref name="tag"/> on the child
		/// node at <paramref name="childIndex"/>, via IVwRootBox.MakeTextSelection with
		/// <paramref name="tag"/> passed as tagTextProp -- the same call PatternView itself uses
		/// (PatternView.SelectLeftBoundary/SelectRightBoundary) to target one specific fake-tag
		/// property on an object, as opposed to the whole object's rendering.
		///
		/// MakeTextSelInObj (the API the original version of this helper used) does NOT take a
		/// tag argument at all: its signature is (ihvoRoot, cvsli, rgvsli, cvsliEnd, rgvsliEnd,
		/// fInitial, fEdit, fRange, fWholeObj, fInstall). The previous version of this helper
		/// passed `tag` into the cvsliEnd slot and set fWholeObj: true, which per the documented
		/// contract (Views.cs "If fWholeObject is true, these arguments are not used") made that
		/// argument dead and made every call select the same thing: the whole child object's
		/// rendering, anchored at its outermost boundary glyph. Nine of the eleven "fragment
		/// angle" tests that used it were therefore the same selection in disguise -- see
		/// MakeSelOnFragment_DiscriminatesBetweenFragments_OnTheSameNode below for the proof this
		/// version actually targets the requested tag.
		/// </summary>
		private static IVwSelection MakeSelOnFragment(TestPatternView view, int childIndex, int tag, int ich = 0)
		{
			var levels = new[]
			{
				new SelLevInfo { tag = ComplexConcPatternSda.ktagChildren, ihvo = childIndex }
			};
			return view.RootBox.MakeTextSelection(0, levels.Length, levels, tag, 0, ich, ich, 0, false, -1, null, true);
		}

		/// <summary>
		/// Fails the test with a diagnostic if <paramref name="sel"/> does not actually target
		/// <paramref name="expectedTag"/> -- proof, not assumption, that a fragment test is
		/// exercising the fragment it claims to.
		/// </summary>
		private static void AssertSelectionTargets(IVwSelection sel, int expectedTag)
		{
			ITsString tss;
			int ich, hvo, tag, ws;
			bool fAssocPrev;
			sel.TextSelInfo(false, out tss, out ich, out fAssocPrev, out hvo, out tag, out ws);
			Assert.That(tag, Is.EqualTo(expectedTag),
				$"selection did not target the requested tag {expectedTag}; got tag {tag} (text '{tss?.Text}')");
		}

		private void AttemptEdit(IVwSelection sel, string replacementText, int ws)
		{
			ITsString replacement = TsStringUtils.MakeString(replacementText, ws);
			UndoableUnitOfWorkHelper.Do("undo", "redo", Cache.LangProject, () => sel.ReplaceWithTsString(replacement));
		}

		/// <summary>
		/// Proof that MakeSelOnFragment actually discriminates between fragments, rather than
		/// resolving to the same selection regardless of the requested tag (the bug an
		/// adversarial review found in this helper's first version, which used
		/// IVwRootBox.MakeTextSelInObj with fWholeObj: true -- see MakeSelOnFragment's doc
		/// comment). One node with three populated fields is selected by three different tags;
		/// each selection must land on its own distinct tag and text, not all collapse onto one
		/// (e.g. the node's outermost boundary glyph).
		/// </summary>
		[Test]
		public void MakeSelOnFragment_DiscriminatesBetweenFragments_OnTheSameNode()
		{
			var (model, view) = BuildView();
			var wordNode = new ComplexConcWordNode
			{
				Form = TsStringUtils.MakeString("myform", Cache.DefaultVernWs),
				Gloss = TsStringUtils.MakeString("myGloss", Cache.DefaultAnalWs)
			};
			model.Root.Children.Add(wordNode);
			view.CallLayout();

			IVwSelection selType = MakeSelOnFragment(view, 0, ComplexConcPatternVc.ktagType);
			IVwSelection selForm = MakeSelOnFragment(view, 0, ComplexConcPatternVc.ktagForm);
			IVwSelection selGloss = MakeSelOnFragment(view, 0, ComplexConcPatternVc.ktagGloss);

			AssertSelectionTargets(selType, ComplexConcPatternVc.ktagType);
			AssertSelectionTargets(selForm, ComplexConcPatternVc.ktagForm);
			AssertSelectionTargets(selGloss, ComplexConcPatternVc.ktagGloss);

			ITsString tss; int ich, hvo, tag, ws; bool fAssocPrev;
			selType.TextSelInfo(false, out tss, out ich, out fAssocPrev, out hvo, out tag, out ws);
			Assert.That(tss.Text, Is.EqualTo("Type: Word"));
			selForm.TextSelInfo(false, out tss, out ich, out fAssocPrev, out hvo, out tag, out ws);
			Assert.That(tss.Text, Is.EqualTo("Form: myform"));
			selGloss.TextSelInfo(false, out tss, out ich, out fAssocPrev, out hvo, out tag, out ws);
			Assert.That(tss.Text, Is.EqualTo("Gloss: myGloss"));
		}

		// ------------------------------------------------------------------
		// Angle 1: breadth of the crash across the fragments ComplexConcPatternVc renders.
		// Each of these encodes the DESIRED end state (no crash, content unchanged) and must
		// fail against current code, which throws NotImplementedException instead.
		// ------------------------------------------------------------------

		[Test]
		public void ReplaceWithTsString_OnWordNodeTypeLine_DoesNotThrow()
		{
			var (model, view) = BuildView();
			var wordNode = new ComplexConcWordNode();
			model.Root.Children.Add(wordNode);
			view.CallLayout();

			IVwSelection sel = MakeSelOnFragment(view, 0, ComplexConcPatternVc.ktagType);
			Assert.That(sel, Is.Not.Null, "could not construct a selection over the Type line");
			AssertSelectionTargets(sel, ComplexConcPatternVc.ktagType);

			Assert.DoesNotThrow(() => AttemptEdit(sel, "HACKED", Cache.DefaultUserWs),
				"a direct edit on the word node's Type line must not crash the view engine");
		}

		[Test]
		public void ReplaceWithTsString_OnWordNodeFormLine_DoesNotThrow_AndFormUnchanged()
		{
			var (model, view) = BuildView();
			var wordNode = new ComplexConcWordNode { Form = TsStringUtils.MakeString("original", Cache.DefaultVernWs) };
			model.Root.Children.Add(wordNode);
			view.CallLayout();

			IVwSelection sel = MakeSelOnFragment(view, 0, ComplexConcPatternVc.ktagForm);
			Assert.That(sel, Is.Not.Null, "could not construct a selection over the Form line");
			AssertSelectionTargets(sel, ComplexConcPatternVc.ktagForm);

			Assert.DoesNotThrow(() => AttemptEdit(sel, "HACKED", Cache.DefaultVernWs),
				"a direct edit on the word node's Form line must not crash the view engine");
			Assert.That(wordNode.Form.Text, Is.EqualTo("original"),
				"the synthetic pattern node's Form must not be mutated by a discarded edit");
		}

		[Test]
		public void ReplaceWithTsString_OnMorphNodeGlossLine_DoesNotThrow_AndGlossUnchanged()
		{
			var (model, view) = BuildView();
			var morphNode = new ComplexConcMorphNode { Gloss = TsStringUtils.MakeString("original-gloss", Cache.DefaultAnalWs) };
			model.Root.Children.Add(morphNode);
			view.CallLayout();

			IVwSelection sel = MakeSelOnFragment(view, 0, ComplexConcPatternVc.ktagGloss);
			Assert.That(sel, Is.Not.Null, "could not construct a selection over the Gloss line");
			AssertSelectionTargets(sel, ComplexConcPatternVc.ktagGloss);

			Assert.DoesNotThrow(() => AttemptEdit(sel, "HACKED", Cache.DefaultAnalWs),
				"a direct edit on the morph node's Gloss line must not crash the view engine");
			Assert.That(morphNode.Gloss.Text, Is.EqualTo("original-gloss"),
				"the synthetic pattern node's Gloss must not be mutated by a discarded edit");
		}

		[Test]
		public void ReplaceWithTsString_OnMorphNodeEntryLine_DoesNotThrow_AndEntryUnchanged()
		{
			var (model, view) = BuildView();
			var morphNode = new ComplexConcMorphNode { Entry = TsStringUtils.MakeString("original-entry", Cache.DefaultVernWs) };
			model.Root.Children.Add(morphNode);
			view.CallLayout();

			IVwSelection sel = MakeSelOnFragment(view, 0, ComplexConcPatternVc.ktagEntry);
			Assert.That(sel, Is.Not.Null, "could not construct a selection over the Entry line");
			AssertSelectionTargets(sel, ComplexConcPatternVc.ktagEntry);

			Assert.DoesNotThrow(() => AttemptEdit(sel, "HACKED", Cache.DefaultVernWs),
				"a direct edit on the morph node's Entry line must not crash the view engine");
			Assert.That(morphNode.Entry.Text, Is.EqualTo("original-entry"),
				"the synthetic pattern node's Entry must not be mutated by a discarded edit");
		}

		[Test]
		public void ReplaceWithTsString_OnMorphNodeCategoryLine_DoesNotThrow_AndRealPartOfSpeechUnrenamed()
		{
			IPartOfSpeech noun = CreatePartOfSpeech("noun", "N");
			var (model, view) = BuildView();
			var morphNode = new ComplexConcMorphNode { Category = noun };
			model.Root.Children.Add(morphNode);
			view.CallLayout();

			IVwSelection sel = MakeSelOnFragment(view, 0, ComplexConcPatternVc.ktagCategory);
			Assert.That(sel, Is.Not.Null, "could not construct a selection over the Category line");
			AssertSelectionTargets(sel, ComplexConcPatternVc.ktagCategory);

			Assert.DoesNotThrow(() => AttemptEdit(sel, "HACKED", Cache.DefaultAnalWs),
				"a direct edit on the morph node's Category line must not crash the view engine");
			// This is the specific check for the bug doc's claim that this is a crash, not a
			// Bug-1-style corruption: the category line displays a REAL, shared IPartOfSpeech's
			// Abbreviation, so if this bug were the same class as Bug 1, a botched edit here
			// could rename it project-wide. Confirm it does not.
			Assert.That(noun.Abbreviation.BestAnalysisAlternative.Text, Is.EqualTo("N"),
				"an edit attempt on the Category line must not rename the real, shared PartOfSpeech");
		}

		[Test]
		public void ReplaceWithTsString_OnMorphNodeInflLine_DoesNotThrow()
		{
			IFsSymFeatVal value;
			IFsClosedFeature feature = CreateClosedFeature("num", out value);
			var (model, view) = BuildView();
			var morphNode = new ComplexConcMorphNode
			{
				InflFeatures = { { feature, new ClosedFeatureValue(value, false) } }
			};
			model.Root.Children.Add(morphNode);
			view.CallLayout();

			IVwSelection sel = MakeSelOnFragment(view, 0, ComplexConcPatternVc.ktagInfl);
			Assert.That(sel, Is.Not.Null, "could not construct a selection over the Infl Features header line");
			AssertSelectionTargets(sel, ComplexConcPatternVc.ktagInfl);

			Assert.DoesNotThrow(() => AttemptEdit(sel, "HACKED", Cache.DefaultAnalWs),
				"a direct edit on the morph node's Infl Features header line must not crash the view engine");
		}

		[Test]
		public void ReplaceWithTsString_OnTagNodeTagLine_DoesNotThrow_AndRealTagUnrenamed()
		{
			ICmPossibility tag = CreateTag("Noun Phrase", "NP");
			var (model, view) = BuildView();
			var tagNode = new ComplexConcTagNode { Tag = tag };
			model.Root.Children.Add(tagNode);
			view.CallLayout();

			IVwSelection sel = MakeSelOnFragment(view, 0, ComplexConcPatternVc.ktagTag);
			Assert.That(sel, Is.Not.Null, "could not construct a selection over the Tag line");
			AssertSelectionTargets(sel, ComplexConcPatternVc.ktagTag);

			Assert.DoesNotThrow(() => AttemptEdit(sel, "HACKED", Cache.DefaultAnalWs),
				"a direct edit on the tag node's Tag line must not crash the view engine");
			Assert.That(tag.Abbreviation.BestAnalysisAlternative.Text, Is.EqualTo("NP"),
				"an edit attempt on the Tag line must not rename the real, shared CmPossibility");
		}

		[Test]
		public void ReplaceWithTsString_OnOrNode_DoesNotThrow()
		{
			var (model, view) = BuildView();
			model.Root.Children.Add(new ComplexConcOrNode());
			model.Root.Children.Add(new ComplexConcWordBdryNode());
			view.CallLayout();

			IVwSelection sel = MakeSelOnFragment(view, 0, PatternVcBase.ktagInnerNonBoundary);
			Assert.That(sel, Is.Not.Null, "could not construct a selection over the OR literal");
			AssertSelectionTargets(sel, PatternVcBase.ktagInnerNonBoundary);

			Assert.DoesNotThrow(() => AttemptEdit(sel, "HACKED", Cache.DefaultUserWs),
				"a direct edit on the OR literal must not crash the view engine");
		}

		[Test]
		public void ReplaceWithTsString_OnWordBoundaryNode_DoesNotThrow()
		{
			var (model, view) = BuildView();
			model.Root.Children.Add(new ComplexConcOrNode());
			model.Root.Children.Add(new ComplexConcWordBdryNode());
			view.CallLayout();

			IVwSelection sel = MakeSelOnFragment(view, 1, PatternVcBase.ktagInnerNonBoundary);
			Assert.That(sel, Is.Not.Null, "could not construct a selection over the '#' literal");
			AssertSelectionTargets(sel, PatternVcBase.ktagInnerNonBoundary);

			Assert.DoesNotThrow(() => AttemptEdit(sel, "HACKED", Cache.DefaultUserWs),
				"a direct edit on the word-boundary '#' literal must not crash the view engine");
		}

		[Test]
		public void ReplaceWithTsString_OnNodeMaximum_DoesNotThrow_AndMaximumUnchanged()
		{
			var (model, view) = BuildView();
			var wordNode = new ComplexConcWordNode { Minimum = 0, Maximum = 3 };
			model.Root.Children.Add(wordNode);
			view.CallLayout();

			IVwSelection sel = MakeSelOnFragment(view, 0, PatternVcBase.ktagRightNonBoundary);
			Assert.That(sel, Is.Not.Null, "could not construct a selection over the max-quantifier line");
			AssertSelectionTargets(sel, PatternVcBase.ktagRightNonBoundary);

			Assert.DoesNotThrow(() => AttemptEdit(sel, "9", Cache.DefaultUserWs),
				"a direct edit on the max-quantifier line must not crash the view engine");
			Assert.That(wordNode.Maximum, Is.EqualTo(3),
				"the synthetic pattern node's Maximum must not be mutated by a discarded edit");
		}

		[Test]
		public void ReplaceWithTsString_OnNodeMinimum_DoesNotThrow_AndMinimumUnchanged()
		{
			var (model, view) = BuildView();
			var wordNode = new ComplexConcWordNode { Minimum = 0, Maximum = 3 };
			model.Root.Children.Add(wordNode);
			view.CallLayout();

			IVwSelection sel = MakeSelOnFragment(view, 0, PatternVcBase.ktagRightBoundary);
			Assert.That(sel, Is.Not.Null, "could not construct a selection over the min-quantifier line");
			AssertSelectionTargets(sel, PatternVcBase.ktagRightBoundary);

			Assert.DoesNotThrow(() => AttemptEdit(sel, "9", Cache.DefaultUserWs),
				"a direct edit on the min-quantifier line must not crash the view engine");
			Assert.That(wordNode.Minimum, Is.EqualTo(0),
				"the synthetic pattern node's Minimum must not be mutated by a discarded edit");
		}

		// ------------------------------------------------------------------
		// Angle 2: is the crash reachable through PatternView's own input handling (keystrokes),
		// or only through paths that bypass it (IME composition, drag-and-drop, or any other
		// direct ReplaceWithTsString caller)? PatternView.OnKeyPress unconditionally sets
		// e.Handled = true and returns without calling base.OnKeyPress for anything but
		// Backspace/Delete, so ordinary WM_CHAR-driven typing never reaches the engine at all.
		// This test is expected to PASS today: it documents that the keystroke path is already
		// safe, which is what makes the ReplaceWithTsString bypass above the actual bug.
		// ------------------------------------------------------------------

		[Test]
		public void SimulateTyping_ViaOnKeyPress_DoesNotReachEngine_AndDoesNotCrash()
		{
			var (model, view) = BuildView();
			var wordNode = new ComplexConcWordNode { Form = TsStringUtils.MakeString("original", Cache.DefaultVernWs) };
			model.Root.Children.Add(wordNode);
			view.CallLayout();

			IVwSelection sel = MakeSelOnFragment(view, 0, ComplexConcPatternVc.ktagForm);
			Assert.That(sel, Is.Not.Null, "could not construct a selection over the Form line");
			sel.Install();

			Assert.DoesNotThrow(() => view.SimulateKeyPress('x'),
				"a plain keystroke must not crash PatternView, regardless of the ComplexConcPatternVc.UpdateProp gap");
			Assert.That(wordNode.Form.Text, Is.EqualTo("original"),
				"a plain keystroke must not reach the engine and alter content -- PatternView.OnKeyPress swallows it before that");
		}

		// ------------------------------------------------------------------
		// Angle 3: insert/delete must keep working. PatternView.OnKeyDown raises
		// RemoveItemsRequested for the Delete key; this must survive whatever fix is applied.
		// ------------------------------------------------------------------

		// ------------------------------------------------------------------
		// Ablation evidence for the fix's layers.
		// ------------------------------------------------------------------

		/// <summary>
		/// Confirms ComplexConcPatternVc's SetNotEditable calls actually take effect: a
		/// selection over a fake-tag fragment must not be editable, independent of whether
		/// UpdateProp would otherwise absorb an edit there.
		/// </summary>
		[Test]
		public void SelectionOverFormLine_IsNotEditable()
		{
			var (model, view) = BuildView();
			var wordNode = new ComplexConcWordNode { Form = TsStringUtils.MakeString("original", Cache.DefaultVernWs) };
			model.Root.Children.Add(wordNode);
			view.CallLayout();

			IVwSelection sel = MakeSelOnFragment(view, 0, ComplexConcPatternVc.ktagForm);
			Assert.That(sel, Is.Not.Null);
			AssertSelectionTargets(sel, ComplexConcPatternVc.ktagForm);
			Assert.That(sel.IsEditable, Is.False,
				"the Form line's fragment must be marked ktptNotEditable, not merely absorbed by UpdateProp");
		}

		/// <summary>
		/// PatternVcBase.OpenSingleLinePile/CloseSingleLinePile add a 1-char zero-width-space
		/// boundary run (tag ktagLeftBoundary/ktagRightBoundary, frag kfragZeroWidthSpace) around
		/// single-line piles, with no ktptEditable marking at all -- an unmarked gap shared by
		/// both PatternVcBase subclasses, found by mutation testing: with UpdateProp removed and
		/// every other guard left in place, an edit on this run still threw
		/// NotImplementedException, because nothing else was rejecting it at the selection layer.
		/// This test fails without the ktptEditable marking added to OpenSingleLinePile/
		/// CloseSingleLinePile (verified by temporarily reverting it) and passes with it.
		/// </summary>
		[Test]
		public void SelectionOverZeroWidthBoundaryRun_IsNotEditable()
		{
			var (model, view) = BuildView();
			model.Root.Children.Add(new ComplexConcOrNode());
			view.CallLayout();

			// The 1-char ZWSP boundary run precedes OR in the same paragraph, at ich=0.
			IVwSelection sel = MakeSelOnFragment(view, 0, PatternVcBase.ktagLeftBoundary, 0);
			Assert.That(sel, Is.Not.Null, "could not construct a selection over the zero-width-space boundary run");
			AssertSelectionTargets(sel, PatternVcBase.ktagLeftBoundary);
			Assert.That(sel.IsEditable, Is.False,
				"the zero-width-space boundary run (PatternVcBase.OpenSingleLinePile) must be marked ktptNotEditable");
		}

		/// <summary>
		/// ComplexConcControl.Designer.cs must wire up the pattern-builder view as read-only
		/// (this is the categorical fix for the IME-composition/keyboard-controller-registration
		/// path named in the bug report, distinct from the per-fragment ktptEditable markings).
		/// </summary>
		[Test]
		public void ComplexConcControl_WiresViewAsReadOnly()
		{
			using (var control = new ComplexConcControl())
			{
				var viewField = typeof(ComplexConcControl).GetField("m_view", BindingFlags.NonPublic | BindingFlags.Instance);
				Assert.That(viewField, Is.Not.Null, "test assumption: ComplexConcControl has a private m_view field");
				var view = (PatternView) viewField.GetValue(control);
				Assert.That(view.ReadOnlyView, Is.True,
					"ComplexConcControl must wire up its PatternView with ReadOnlyView = true");
			}
		}

		/// <summary>
		/// SimpleRootSite.ReadOnlyView's setter forces AcceptsReturn = AcceptsTab = false when set
		/// to true (SimpleRootSite.cs), which happens AFTER the Designer's own explicit
		/// AcceptsReturn = true / AcceptsTab = false assignments in InitializeComponent's
		/// generated-code ordering. This pins the actual, real behaviour change: AcceptsTab was
		/// already false before this fix (Designer-set, independent of ReadOnlyView) so Tab
		/// navigation out of the pane is unchanged; AcceptsReturn flips from true to false, which
		/// is new. Since PatternView.OnKeyPress already swallows Return either way (it is not
		/// Backspace/Delete), the observable difference is only where the key is disposed of: it
		/// used to reach the control and be silently swallowed there; now IsInputKey(Return)
		/// returns false and the key is never delivered to the control at all, so it is processed
		/// as an ordinary dialog/navigation key by whatever contains this pane. This control is
		/// hosted as a Words-area tool pane (DistFiles/.../Concordance/toolConfiguration.xml), not
		/// inside a modal dialog with an AcceptButton, so no default-button activation is expected
		/// in practice -- but that is unverified live; see the review doc.
		/// </summary>
		[Test]
		public void ComplexConcControl_AcceptsTabUnchanged_AcceptsReturnNowFalse()
		{
			using (var control = new ComplexConcControl())
			{
				var viewField = typeof(ComplexConcControl).GetField("m_view", BindingFlags.NonPublic | BindingFlags.Instance);
				var view = (PatternView) viewField.GetValue(control);

				Assert.That(view.AcceptsTab, Is.False,
					"AcceptsTab was already false in the Designer before this fix; it must still be false");
				Assert.That(view.AcceptsReturn, Is.False,
					"ReadOnlyView = true forces AcceptsReturn to false, overriding the Designer's " +
					"explicit AcceptsReturn = true -- this is the real behaviour change, not Tab");
			}
		}

		/// <summary>
		/// PatternView.AllowDisplaySelection must be overridden to stay true even when
		/// ReadOnlyView is true, or the pattern-builder's chooser-driven selection highlight
		/// would disappear once ReadOnlyView is turned on (SimpleRootSite suppresses Activate()
		/// by default for read-only views).
		/// </summary>
		[Test]
		public void AllowDisplaySelection_IsTrue_WhenRootsiteIsReadOnly()
		{
			var (model, view) = BuildView();
			view.ReadOnlyView = true;

			Assert.That(view.TestAllowDisplaySelection, Is.True,
				"the selection must still be shown even though the rootsite is read-only");
		}

		[Test]
		public void DeleteKey_StillRaisesRemoveItemsRequested_WhenRootsiteIsReadOnly()
		{
			var (model, view) = BuildView();
			model.Root.Children.Add(new ComplexConcWordNode());
			view.CallLayout();
			view.ReadOnlyView = true;

			bool removeRequested = false;
			view.RemoveItemsRequested += (sender, e) => removeRequested = true;

			view.SimulateKeyDown(Keys.Delete);

			Assert.That(removeRequested, Is.True,
				"Delete must still raise RemoveItemsRequested now that ComplexConcControl wires the view as ReadOnlyView = true");
		}
	}
}

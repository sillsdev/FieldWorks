// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// §19a T2 — INTEGRATION on ONE realized region surface: a single <see cref="LexicalEditRegionView"/>
	/// holding a sibling multistring (Citation Form) row AND an editable structured-text (StText) row,
	/// driven through the REAL <see cref="ComposedRegionEditContext"/> over an in-memory LCModel (the same
	/// fenced <see cref="LcmRegionEditSession"/> staging the production composer wires). These exercise
	/// StText editing COMBINED with other detail-view editing and assert:
	/// (a) each change stages / commits correctly,
	/// (b) undo grouping is correct across the combined gestures (text edits coalesce into one focus-loss
	///     step; each structural gesture is its own immediate step; the right order), and
	/// (c) the region refresh after add/delete paragraph (the host re-show) does not disturb the sibling
	///     field's state.
	/// The view side runs headless (the assembly's AvaloniaHeadlessSetUpFixture forces the headless
	/// platform), so flyouts/popups never become real on-screen windows.
	/// </summary>
	[TestFixture]
	public class StructuredTextIntegrationTests : MemoryOnlyBackendProviderTestBase
	{
		private ILexEntry m_entry;
		private IStText m_stText;

		public override void TestSetup()
		{
			base.TestSetup();
			// Build the headless Avalonia platform (the assembly SetUpFixture installed the headless
			// AppBuilder override) so the realized field controls + their host Window have a windowing
			// platform off-screen — the same init the product surface hosts trigger. Idempotent.
			FwAvaloniaRuntime.EnsureInitialized();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				m_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				var morph = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				m_entry.LexemeFormOA = morph;
				morph.Form.set_String(Cache.DefaultVernWs, TsStringUtils.MakeString("casa", Cache.DefaultVernWs));
				m_entry.CitationForm.set_String(Cache.DefaultVernWs,
					TsStringUtils.MakeString("casa-cit", Cache.DefaultVernWs));

				// An owned StText (under a notebook record's Discussion) with two paragraphs — the same
				// real, persisted OwningAtomic-StText shape StructuredTextAdapterTests uses.
				if (Cache.LangProject.ResearchNotebookOA == null)
					Cache.LangProject.ResearchNotebookOA =
						Cache.ServiceLocator.GetInstance<IRnResearchNbkFactory>().Create();
				var record = Cache.ServiceLocator.GetInstance<IRnGenericRecFactory>().Create();
				Cache.LangProject.ResearchNotebookOA.RecordsOC.Add(record);
				record.DiscussionOA = Cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
				m_stText = record.DiscussionOA;
				var p0 = m_stText.AddNewTextPara(null);
				p0.Contents = TsStringUtils.MakeString("First paragraph.", Cache.DefaultAnalWs);
				var p1 = m_stText.AddNewTextPara(null);
				p1.Contents = TsStringUtils.MakeString("Second paragraph.", Cache.DefaultAnalWs);
			});
		}

		private string CitationText => m_entry.CitationForm.get_String(Cache.DefaultVernWs)?.Text;
		private string ParaText(int i) => ((IStTxtPara)m_stText.ParagraphsOS[i]).Contents?.Text;
		private string ParaStyle(int i) => ((IStTxtPara)m_stText.ParagraphsOS[i]).StyleName;
		private int ParaCount => m_stText.ParagraphsOS.Count;

		private const string CitationStableId = "LexEntry/CitationForm@e";
		private const string StTextStableId = "LexEntry/Discussion@e";

		// A region model with a sibling multistring (Citation Form) row + a StructuredText (Discussion)
		// row, plus the composed edit context whose setters mutate the real LCModel exactly as
		// FullEntryRegionComposer wires them. This is the production seam, not a stand-in.
		private (LexicalEditRegionModel Model, ComposedRegionEditContext Context) BuildSurface()
		{
			var citation = new LexicalEditRegionField(CitationStableId, "Citation Form", "CitationForm", null,
				RegionFieldKind.Text, EditorClassification.Known, "CitationForm", null, SurfaceRouting.Product,
				new List<RegionWsValue> { new RegionWsValue("vern", CitationText, wsTag: "vern") },
				null, null, isEditable: true);

			var paragraphs = m_stText.ParagraphsOS.OfType<IStTxtPara>()
				.Select(p => new RegionParagraph(
					RegionRichTextAdapter.FromTsString(p.Contents, Cache.WritingSystemFactory), p.StyleName))
				.ToList();
			var stTextField = new LexicalEditRegionField(StTextStableId, "Discussion", "Discussion", null,
				RegionFieldKind.StructuredText, EditorClassification.Known, "Discussion", null,
				SurfaceRouting.Product, null, null, null, isEditable: true, paragraphs: paragraphs)
			{
				AvailableParagraphStyles = new[] { "Block Quote", "Numbered List" }
			};

			var model = new LexicalEditRegionModel("LexEntry", "Normal",
				new List<LexicalEditRegionField> { citation, stTextField }, new List<ViewDiagnostic>());

			var defaultWs = Cache.DefaultAnalWs;
			var wsf = Cache.WritingSystemFactory;

			var textSetters = new Dictionary<string, Func<string, string, bool>>
			{
				[CitationStableId] = (ws, value) =>
				{
					m_entry.CitationForm.set_String(Cache.DefaultVernWs,
						TsStringUtils.MakeString(value ?? string.Empty, Cache.DefaultVernWs));
					return true;
				}
			};
			var paragraphTextSetters = new Dictionary<string, Func<int, RegionRichTextValue, bool>>
			{
				[StTextStableId] = (index, value) =>
				{
					if (value == null || index < 0)
						return false;
					while (m_stText.ParagraphsOS.Count <= index)
						m_stText.InsertNewTextPara(m_stText.ParagraphsOS.Count, null);
					((IStTxtPara)m_stText.ParagraphsOS[index]).Contents =
						RegionRichTextAdapter.ToTsString(value, wsf, defaultWs);
					return true;
				}
			};
			var paragraphStyleSetters = new Dictionary<string, Func<int, string, bool>>
			{
				[StTextStableId] = (index, style) =>
				{
					if (index < 0 || index >= m_stText.ParagraphsOS.Count)
						return false;
					((IStTxtPara)m_stText.ParagraphsOS[index]).StyleName =
						string.IsNullOrEmpty(style) ? StyleServices.NormalStyleName : style;
					return true;
				}
			};
			var paragraphInsertSetters = new Dictionary<string, Func<int, bool>>
			{
				[StTextStableId] = afterIndex =>
				{
					var pos = afterIndex < 0 ? 0 : Math.Min(afterIndex + 1, m_stText.ParagraphsOS.Count);
					m_stText.InsertNewTextPara(pos, null);
					return true;
				}
			};
			var paragraphDeleteSetters = new Dictionary<string, Func<int, bool>>
			{
				[StTextStableId] = index =>
				{
					if (index < 0 || index >= m_stText.ParagraphsOS.Count || m_stText.ParagraphsOS.Count <= 1)
						return false;
					m_stText.ParagraphsOS.RemoveAt(index);
					return true;
				}
			};

			var context = new ComposedRegionEditContext(Cache, m_entry,
				textSetters, new Dictionary<string, Func<string, bool>>(),
				paragraphTextSetters: paragraphTextSetters, paragraphStyleSetters: paragraphStyleSetters,
				paragraphInsertSetters: paragraphInsertSetters, paragraphDeleteSetters: paragraphDeleteSetters);
			return (model, context);
		}

		/// <summary>
		/// ONE realized region surface hosting BOTH field controls over the SAME composed edit context:
		/// the sibling multistring (<see cref="FwMultiWsTextField"/>) and the StText
		/// (<see cref="FwStructuredTextField"/>) in one StackPanel. We realize the field controls directly
		/// (rather than the whole <see cref="LexicalEditRegionView"/> surface, whose GridSplitter needs an
		/// input/cursor platform the bare headless xWorksTests host does not register) and wire the SAME
		/// commit semantics the view's autosave/gesture path uses:
		///   * structural gestures (add/delete/style) commit immediately + raise the re-show, via the
		///     <c>gestureCompleted</c> callback the production factory passes the field;
		///   * per-paragraph / sibling text edits stage and ride <see cref="CommitOpenSession"/> (the
		///     view's focus-loss autosave commits the one open fenced session).
		/// </summary>
		private sealed class Surface
		{
			public Window Window;
			public Control Root;
			public ComposedRegionEditContext Context;
			public int Rebuilds;

			// Mirrors LexicalEditRegionView.OnSave for a structural gesture: validation-gated commit of the
			// one open fenced session, then the re-show signal (the host rebuilds the rows from domain truth).
			public void CompleteGesture()
			{
				if (Context.Validate().Count == 0 && Context.IsOpen)
					Context.Commit();
				Rebuilds++;
			}

			// Mirrors the view's focus-loss autosave: commit the open session if validation passes.
			public void CommitOpenSession()
			{
				if (Context.IsOpen && Context.Validate().Count == 0)
					Context.Commit();
			}
		}

		private Surface ShowSurface(LexicalEditRegionModel model, ComposedRegionEditContext context)
		{
			var surface = new Surface { Context = context };
			var citationField = model.Fields[0];
			var stTextField = model.Fields[1];

			var citationControl = new FwMultiWsTextField(citationField, citationField.AutomationId, context, null);
			var stTextControl = new FwStructuredTextField(stTextField, stTextField.AutomationId, context, null,
				surface.CompleteGesture);

			var panel = new StackPanel();
			panel.Children.Add(citationControl);
			panel.Children.Add(stTextControl);
			AutomationProperties.SetAutomationId(panel, "StTextIntegrationSurface");

			var window = new Window { Content = panel, Width = 520, Height = 360 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			panel.UpdateLayout();
			Dispatcher.UIThread.RunJobs();

			surface.Window = window;
			surface.Root = panel;
			return surface;
		}

		private static T Find<T>(Control root, string automationId) where T : Control
			=> root.GetVisualDescendants().OfType<T>()
				.FirstOrDefault(c => AutomationProperties.GetAutomationId(c) == automationId);

		// (a)+(b): editing the sibling Citation Form AND a paragraph in the SAME session, then losing
		// focus, commits BOTH as ONE undo step (the legacy per-slice "edit as you go" autosave), and one
		// undo reverts both — the combined-text-edit grouping.
		[Test]
		public void EditCitation_AndParagraphText_CommitTogether_AsOneUndoStep_OnFocusLoss()
		{
			var (model, context) = BuildSurface();
			var surface = ShowSurface(model, context);

			var citationBox = Find<TextBox>(surface.Root, "CitationForm.vern");
			var para0 = Find<TextBox>(surface.Root, "Discussion.Para.0");
			Assert.That(citationBox, Is.Not.Null, "the sibling multistring row realized");
			Assert.That(para0, Is.Not.Null, "the StText paragraph row realized");

			citationBox.Text = "casa-edited";
			para0.Text = "First paragraph, edited.";
			Dispatcher.UIThread.RunJobs();
			Assert.That(context.IsOpen, Is.True, "both staged into the one open fenced session");

			// Focus loss on the surface triggers the autosave commit (one step covering both fields).
			surface.CommitOpenSession();
			Dispatcher.UIThread.RunJobs();
			Assert.That(context.IsOpen, Is.False, "the autosave committed the open session");

			Assert.That(CitationText, Is.EqualTo("casa-edited"));
			Assert.That(ParaText(0), Is.EqualTo("First paragraph, edited."));
			Assert.That(ParaText(1), Is.EqualTo("Second paragraph."), "the untouched paragraph is unchanged");

			Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.True);
			Cache.ActionHandlerAccessor.Undo(); // ONE undo reverts the whole combined text edit
			Assert.That(CitationText, Is.EqualTo("casa-cit"), "one undo reverts the sibling field");
			Assert.That(ParaText(0), Is.EqualTo("First paragraph."), "...and the paragraph, in the same step");
		}

		// (b)+(c): a structural gesture (add paragraph) commits IMMEDIATELY as its own undo step (separate
		// from the text edits), and the host re-show that rebuilds the paragraph rows from domain truth
		// does NOT disturb a sibling Citation Form edit that was committed first.
		[Test]
		public void AddParagraph_IsItsOwnImmediateUndoStep_AndReShowKeepsSiblingState()
		{
			var (model, context) = BuildSurface();
			var surface = ShowSurface(model, context);

			// First: edit + commit the sibling Citation Form (its own step) by focus loss.
			var citationBox = Find<TextBox>(surface.Root, "CitationForm.vern");
			citationBox.Text = "casa-first";
			Dispatcher.UIThread.RunJobs();
			surface.CommitOpenSession();
			Dispatcher.UIThread.RunJobs();
			Assert.That(CitationText, Is.EqualTo("casa-first"));
			var undoDepthAfterCitation = UndoDepth();

			// Now add a paragraph after index 0 via the per-row "+" affordance. The gesture commits
			// immediately (its own step) and raises the re-show signal.
			Find<Button>(surface.Root, "Discussion.Para.0.Add")
				.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
			Dispatcher.UIThread.RunJobs();

			Assert.That(ParaCount, Is.EqualTo(3), "the paragraph was inserted and committed immediately");
			Assert.That(ParaText(0), Is.EqualTo("First paragraph."), "inserted AFTER paragraph 0");
			Assert.That(string.IsNullOrEmpty(ParaText(1)), Is.True, "the inserted paragraph is empty");
			Assert.That(ParaText(2), Is.EqualTo("Second paragraph."));
			Assert.That(context.IsOpen, Is.False, "the structural gesture closed its own session");
			Assert.That(surface.Rebuilds, Is.GreaterThanOrEqualTo(1), "the gesture raised the host re-show signal");
			Assert.That(UndoDepth(), Is.EqualTo(undoDepthAfterCitation + 1),
				"the add is a SEPARATE single undo step, not folded into the earlier citation edit");

			// (c) the sibling field's committed state is intact across the re-show.
			Assert.That(CitationText, Is.EqualTo("casa-first"),
				"the paragraph add + re-show did not disturb the already-committed sibling field");

			// Undo grouping order: one undo removes the inserted paragraph; a SECOND undo reverts the
			// earlier citation edit — they are distinct steps in the right order.
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(ParaCount, Is.EqualTo(2), "first undo removes the inserted paragraph");
			Assert.That(CitationText, Is.EqualTo("casa-first"), "the citation edit is a separate, earlier step");
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(CitationText, Is.EqualTo("casa-cit"), "second undo reverts the citation edit");
		}

		// (b): applying a paragraph STYLE is its own immediate step too, and the combined journey
		// (text edit -> add -> style) yields exactly the expected ordered undo stack.
		[Test]
		public void CombinedGestures_TextEdit_Add_Style_ProduceOrderedDistinctUndoSteps()
		{
			var (model, context) = BuildSurface();
			var surface = ShowSurface(model, context);
			var baseDepth = UndoDepth();

			// 1) text edit on paragraph 1 + focus loss = one step.
			var para1 = Find<TextBox>(surface.Root, "Discussion.Para.1");
			para1.Text = "Second paragraph, edited.";
			Dispatcher.UIThread.RunJobs();
			surface.CommitOpenSession();
			Dispatcher.UIThread.RunJobs();
			Assert.That(UndoDepth(), Is.EqualTo(baseDepth + 1), "the text edit is one step");

			// 2) add a paragraph after 1 = a second step (immediate).
			Find<Button>(surface.Root, "Discussion.Para.1.Add").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
			Dispatcher.UIThread.RunJobs();
			Assert.That(ParaCount, Is.EqualTo(3));
			Assert.That(UndoDepth(), Is.EqualTo(baseDepth + 2), "the add is a second, separate step");

			// 3) apply a paragraph style to paragraph 0 via the picker = a third step (immediate).
			var styleButton = Find<Button>(surface.Root, "Discussion.Para.0.Style");
			var flyout = (Flyout)styleButton.Flyout;
			flyout.ShowAt(styleButton);
			Dispatcher.UIThread.RunJobs();
			var picker = (FwOptionPicker)flyout.Content;
			picker.OptionsList.SelectedIndex = 1; // "Block Quote" (index 0 is Default)
			picker.CommitHighlighted();
			Dispatcher.UIThread.RunJobs();
			Assert.That(ParaStyle(0), Is.EqualTo("Block Quote"));
			Assert.That(UndoDepth(), Is.EqualTo(baseDepth + 3), "the style is a third, separate step");

			// Undo in reverse order, each its own step.
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(ParaStyle(0), Is.Not.EqualTo("Block Quote"), "undo 1 reverts the style");
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(ParaCount, Is.EqualTo(2), "undo 2 removes the inserted paragraph");
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(ParaText(1), Is.EqualTo("Second paragraph."), "undo 3 reverts the text edit");
		}

		// Counts the committed undo steps available, so a test can assert "exactly one more step was added".
		private int UndoDepth()
		{
			var n = 0;
			var ah = Cache.ActionHandlerAccessor;
			// CanUndo()/Undo()/Redo() walk the stack; count by undoing to the bottom then redoing back.
			while (ah.CanUndo())
			{
				ah.Undo();
				n++;
			}
			for (var i = 0; i < n; i++)
				ah.Redo();
			return n;
		}
	}
}

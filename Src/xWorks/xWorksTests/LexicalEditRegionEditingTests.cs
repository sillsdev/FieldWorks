// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.Seams;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Tasks 6.8/6.10 — the first editable slice runs through real LCModel seams: a fenced edit
	/// session whose commit is ONE step on the single global undo stack legacy surfaces share
	/// (cross-framework Ctrl+Z by construction), cancel rolls everything back, and the validation
	/// seam gates empty lexeme forms. Task 3.15 — the refresh controller follows the real
	/// PropChanged bus, holding refreshes while this surface's own session is open.
	/// </summary>
	[TestFixture]
	public class LexicalEditRegionEditingTests : MemoryOnlyBackendProviderTestBase
	{
		private ILexEntry m_entry;

		public override void TestSetup()
		{
			base.TestSetup();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				m_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				var morph = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				m_entry.LexemeFormOA = morph;
				morph.Form.set_String(Cache.DefaultVernWs, TsStringUtils.MakeString("casa", Cache.DefaultVernWs));
				var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				m_entry.SensesOS.Add(sense);
				sense.Gloss.set_String(Cache.DefaultAnalWs, TsStringUtils.MakeString("house", Cache.DefaultAnalWs));
			});
		}

		private string LexemeText => m_entry.LexemeFormOA.Form.get_String(Cache.DefaultVernWs).Text;

		private string GlossText => m_entry.SensesOS[0].Gloss.get_String(Cache.DefaultAnalWs).Text;

		// The edit-context seam keys fixed slices by Field name; tests address fields the same way
		// the view does — through a region field object.
		internal static SIL.FieldWorks.Common.FwAvalonia.Region.LexicalEditRegionField F(string field)
			=> new SIL.FieldWorks.Common.FwAvalonia.Region.LexicalEditRegionField(
				"test/" + field, field, field, null,
				SIL.FieldWorks.Common.FwAvalonia.Region.RegionFieldKind.Text,
				SIL.FieldWorks.Common.FwAvalonia.ViewDefinition.EditorClassification.Known,
				null, null, SIL.FieldWorks.Common.FwAvalonia.ViewDefinition.SurfaceRouting.Product,
				null, null, null);

		[Test]
		public void Commit_MultiFieldEdit_IsOneStepOnTheGlobalUndoStack()
		{
			var context = new LexicalEditRegionEditContext(m_entry, Cache);

			Assert.That(context.TrySetText(F("Form"), "vern", "perro"), Is.True);
			Assert.That(context.TrySetText(F("Gloss"), "anal", "dog"), Is.True);
			Assert.That(context.IsOpen, Is.True, "the fenced session opens on the first staged edit");
			context.Commit();
			Assert.That(context.IsOpen, Is.False);

			Assert.That(LexemeText, Is.EqualTo("perro"));
			Assert.That(GlossText, Is.EqualTo("dog"));

			// One global undo step covers both fields, on the same action handler legacy surfaces use.
			Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.True);
			Cache.ActionHandlerAccessor.Undo(); // one undo = the whole region edit
			Assert.That(LexemeText, Is.EqualTo("casa"), "one undo reverts every field of the session");
			Assert.That(GlossText, Is.EqualTo("house"));

			Cache.ActionHandlerAccessor.Redo();
			Assert.That(LexemeText, Is.EqualTo("perro"), "redo replays the whole session step");
			Assert.That(GlossText, Is.EqualTo("dog"));
		}

		[Test]
		public void Cancel_RollsBackEveryStagedEdit_AndLeavesNoUndoStep()
		{
			var canUndoBefore = Cache.ActionHandlerAccessor.CanUndo();
			var context = new LexicalEditRegionEditContext(m_entry, Cache);

			context.TrySetText(F("Form"), "vern", "perro");
			context.TrySetText(F("Gloss"), "anal", "dog");
			context.Cancel();

			Assert.That(LexemeText, Is.EqualTo("casa"), "cancel rolls back all staged edits");
			Assert.That(GlossText, Is.EqualTo("house"));
			Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.EqualTo(canUndoBefore),
				"a cancelled session leaves nothing on the undo stack");

			// Idempotence: a second cancel/commit is a safe no-op.
			context.Cancel();
			context.Commit();
		}

		[Test]
		public void Commit_RichStyledFormEdit_RefreshesDisplayedEntry_AndIsOneUndoStep()
		{
			var refreshes = 0;
			using (new AvaloniaRegionRefreshController(
				Cache, () => m_entry, () => false, () => refreshes++, new RefreshCoordinator()))
			{
				var context = new LexicalEditRegionEditContext(m_entry, Cache);
				var rich = RegionRichTextEditAlgorithms.FromRuns("perro", new[]
				{
					new RegionTextRun("per", Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Id),
					new RegionTextRun("ro", Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Id,
						namedStyle: "Emphasis")
				});

				Assert.That(context.TrySetRichText(F("Form"), "vern", rich), Is.True);
				context.Commit();

				var stored = m_entry.LexemeFormOA.Form.get_String(Cache.DefaultVernWs);
				Assert.That(stored.Text, Is.EqualTo("perro"));
				Assert.That(stored.RunCount, Is.EqualTo(2), "rich staging preserves run boundaries");
				Assert.That(stored.get_Properties(1)
					.GetStrPropValue((int)FwTextPropType.ktptNamedStyle), Is.EqualTo("Emphasis"));
				Assert.That(refreshes, Is.GreaterThanOrEqualTo(1),
					"a styled rich commit on the displayed entry must signal refresh through the shared PropChanged bus");
			}

			Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.True,
				"the rich commit lands on the same global undo stack legacy surfaces use");
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(LexemeText, Is.EqualTo("casa"),
				"one undo reverts the whole rich commit in one shared step");
		}

		[Test]
		public void TrySetOption_MorphType_ResolvesByGuid_AndCommits()
		{
			var morphTypes = Cache.LangProject.LexDbOA.MorphTypesOA.ReallyReallyAllPossibilities
				.OfType<IMoMorphType>()
				.ToList();
			Assert.That(morphTypes.Count, Is.GreaterThanOrEqualTo(2), "fixture project ships morph types");
			var target = morphTypes.First(mt => mt != m_entry.LexemeFormOA.MorphTypeRA);

			var context = new LexicalEditRegionEditContext(m_entry, Cache);
			Assert.That(context.TrySetOption(F("MorphType"), target.Guid.ToString()), Is.True);
			context.Commit();

			Assert.That(m_entry.LexemeFormOA.MorphTypeRA, Is.EqualTo(target));
		}

		[Test]
		public void TrySetOption_RejectsUnknownKeysAndFields_WithoutOpeningASession()
		{
			var context = new LexicalEditRegionEditContext(m_entry, Cache);
			Assert.That(context.TrySetOption(F("MorphType"), "not-a-guid"), Is.False);
			Assert.That(context.TrySetOption(F("MorphType"), System.Guid.NewGuid().ToString()), Is.False,
				"a guid that is not a morph type must not stage");
			Assert.That(context.TrySetOption(F("Gloss"), "x"), Is.False);
			Assert.That(context.IsOpen, Is.False, "rejected edits must not open the fence");
		}

		// ITEM 1: editing a single field opens the fenced session with a field-specific undo label
		// (e.g. "Undo change to Gloss"), mirroring the legacy per-slice "Undo change to {field}" labels,
		// instead of the generic "Undo Edit Entry".
		[Test]
		public void TrySetText_Gloss_OpensSessionWithFieldSpecificUndoLabel()
		{
			var context = new LexicalEditRegionEditContext(m_entry, Cache);
			try
			{
				Assert.That(context.TrySetText(F("Gloss"), "anal", "dog"), Is.True);
				Assert.That(context.IsOpen, Is.True);
			}
			finally
			{
				// Commit so the fenced task lands on the undo stack (GetUndoText reads the committed
				// top, not an in-flight task); also guarantees no open task leaks into the next test.
				context.Commit();
			}

			var expected = string.Format(CultureInfo.CurrentCulture,
				FwAvaloniaStrings.UndoChangeToFormat, "Gloss");
			Assert.That(Cache.ActionHandlerAccessor.GetUndoText(), Does.Contain("Gloss"),
				"the committed session's undo label names the field being edited");
			Assert.That(Cache.ActionHandlerAccessor.GetUndoText(), Is.EqualTo(expected),
				"the field-specific 'Undo change to {0}' label is used, not the generic one");
			Assert.That(Cache.ActionHandlerAccessor.GetUndoText(),
				Is.Not.EqualTo(FwAvaloniaStrings.UndoEditEntry));
		}

		// ITEM 1: the label is fixed at the first staged edit (the field that opens the session names
		// it); a later edit to a different field in the SAME session does not relabel it.
		[Test]
		public void FirstStagedField_FixesTheUndoLabel_ForTheWholeSession()
		{
			var context = new LexicalEditRegionEditContext(m_entry, Cache);
			try
			{
				Assert.That(context.TrySetText(F("Form"), "vern", "perro"), Is.True);
				Assert.That(context.TrySetText(F("Gloss"), "anal", "dog"), Is.True);
			}
			finally
			{
				context.Commit();
			}

			// The first staged field was Form, so the whole one-step session is labelled by Form,
			// not relabelled by the later Gloss edit.
			var expected = string.Format(CultureInfo.CurrentCulture,
				FwAvaloniaStrings.UndoChangeToFormat, "Form");
			Assert.That(Cache.ActionHandlerAccessor.GetUndoText(), Is.EqualTo(expected),
				"the first staged field names the label; a later same-session edit does not relabel it");
		}

		// ITEM 1: the batch/bulk path that opens the session with NO single field keeps the generic
		// "Undo Edit Entry" label (the field-specific labels are only for single-field edits).
		[Test]
		public void Stage_WithoutAFieldLabel_KeepsTheGenericUndoLabel()
		{
			var context = new LexicalEditRegionEditContext(m_entry, Cache);
			try
			{
				Assert.That(context.Stage(() =>
				{
					m_entry.CitationForm.set_String(Cache.DefaultVernWs,
						TsStringUtils.MakeString("batch", Cache.DefaultVernWs));
					return true;
				}), Is.True);
			}
			finally
			{
				context.Commit();
			}

			Assert.That(Cache.ActionHandlerAccessor.GetUndoText(), Is.EqualTo(FwAvaloniaStrings.UndoEditEntry),
				"a session opened with no single field uses the generic label, not a field-specific one");
		}

		[Test]
		public void Validate_RequiresLexemeOrCitationForm()
		{
			var context = new LexicalEditRegionEditContext(m_entry, Cache);
			Assert.That(context.Validate(), Is.Empty);

			context.TrySetText(F("Form"), "vern", "");
			Assert.That(context.Validate(), Is.EqualTo(new[] { FwAvaloniaStrings.LexemeFormRequired }),
				"emptying the lexeme form trips the required-field rule");

			context.TrySetText(F("Form"), "vern", "gato");
			Assert.That(context.Validate(), Is.Empty);
			context.Cancel();
		}

		// Finding-3: writing-system identity is the unique IETF tag (ws.Id), never only the
		// user-editable Abbreviation — an unmatched abbreviation must not silently write to the
		// default writing system.
		[Test]
		public void TrySetText_AddressesTheWritingSystemByIetfTag()
		{
			var container = Cache.ServiceLocator.WritingSystems;
			CoreWritingSystemDefinition second = null;
			string originalAbbrev = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				Cache.ServiceLocator.WritingSystemManager.GetOrSet("es", out second);
				container.AddToCurrentVernacularWritingSystems(second);
				originalAbbrev = second.Abbreviation;
				second.Abbreviation = "Spa"; // the row gutter label, not the identity
			});
			try
			{
				var context = new LexicalEditRegionEditContext(m_entry, Cache);
				Assert.That(context.TrySetText(F("Form"), second.Id, "kasa"), Is.True);
				context.Commit();

				Assert.That(m_entry.LexemeFormOA.Form.get_String(second.Handle).Text, Is.EqualTo("kasa"),
					"the IETF tag addresses its own alternative");
				Assert.That(LexemeText, Is.EqualTo("casa"),
					"the default alternative must not absorb an edit addressed to another writing system");
				Cache.ActionHandlerAccessor.Undo();
			}
			finally
			{
				NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
				{
					second.Abbreviation = originalAbbrev;
					container.VernacularWritingSystems.Remove(second);
					container.CurrentVernacularWritingSystems.Remove(second);
				});
			}
		}

		// Review round 2: an entirely unknown ws key must be rejected (no session, no write) instead
		// of silently landing on the DEFAULT alternative — matching ComposedRegionEditContext. Only
		// real ids/abbreviations and the legacy "vern"/"anal" first-slice aliases resolve.
		[Test]
		public void TrySetText_UnknownWsKey_IsRejectedWithoutOpeningASession()
		{
			var context = new LexicalEditRegionEditContext(m_entry, Cache);

			Assert.That(context.TrySetText(F("Form"), "no-such-ws", "perro"), Is.False,
				"an unknown ws key must not write to the default alternative");
			Assert.That(context.TrySetText(F("Gloss"), "xkcd", "dog"), Is.False);
			Assert.That(context.IsOpen, Is.False, "a rejected key must not open the fenced session");
			Assert.That(LexemeText, Is.EqualTo("casa"), "nothing was written");

			// The legacy aliases keep resolving to the defaults (the fixed first-slice definition).
			Assert.That(context.TrySetText(F("Form"), "vern", "perro"), Is.True);
			Assert.That(context.TrySetText(F("Gloss"), "anal", "dog"), Is.True);
			context.Cancel();
		}

		[Test]
		public void RefreshController_ExternalEditToDisplayedEntry_TriggersRefresh()
		{
			var refreshes = 0;
			using (new AvaloniaRegionRefreshController(
				Cache, () => m_entry, () => false, () => refreshes++, new RefreshCoordinator()))
			{
				NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
					m_entry.CitationForm.set_String(Cache.DefaultVernWs,
						TsStringUtils.MakeString("edited elsewhere", Cache.DefaultVernWs)));

				Assert.That(refreshes, Is.GreaterThanOrEqualTo(1),
					"a legacy-side edit to the displayed entry must reach the Avalonia surface through the real PropChanged bus");
			}
		}

		[Test]
		public void RefreshController_EditToAnotherEntry_DoesNotRefresh()
		{
			ILexEntry other = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
				other = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create());

			var refreshes = 0;
			using (new AvaloniaRegionRefreshController(
				Cache, () => m_entry, () => false, () => refreshes++, new RefreshCoordinator()))
			{
				NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
					other.CitationForm.set_String(Cache.DefaultVernWs,
						TsStringUtils.MakeString("unrelated", Cache.DefaultVernWs)));

				Assert.That(refreshes, Is.EqualTo(0), "changes outside the displayed entry are not relevant");
			}
		}

		[Test]
		public void RefreshController_HoldsRefreshWhileEditing_DeliversOnCompletion()
		{
			var editing = true;
			var refreshes = 0;
			using (var controller = new AvaloniaRegionRefreshController(
				Cache, () => m_entry, () => editing, () => refreshes++, new RefreshCoordinator()))
			{
				NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
					m_entry.CitationForm.set_String(Cache.DefaultVernWs,
						TsStringUtils.MakeString("raced", Cache.DefaultVernWs)));
				Assert.That(refreshes, Is.EqualTo(0), "refreshes are held while the surface's own session is open");

				// The production completion path (RecordEditView.OnAvaloniaRegionEditCompleted):
				// drop the held delivery and request ONE coalesced refresh covering both the
				// completed edit and anything held during it.
				editing = false;
				controller.DiscardHeldRefresh();
				controller.RequestRefresh();
				Assert.That(refreshes, Is.EqualTo(1), "the held refresh is delivered once on edit completion");
			}
		}

		[Test]
		public void RefreshController_Dispose_StopsListening()
		{
			var refreshes = 0;
			var controller = new AvaloniaRegionRefreshController(
				Cache, () => m_entry, () => false, () => refreshes++, new RefreshCoordinator());
			controller.Dispose();

			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
				m_entry.CitationForm.set_String(Cache.DefaultVernWs,
					TsStringUtils.MakeString("after dispose", Cache.DefaultVernWs)));
			Assert.That(refreshes, Is.EqualTo(0));
		}
	}

	/// <summary>
	/// Sections 6/7 — the COMPLETE lexical edit view: `FullEntryRegionComposer` walks the live
	/// compiled `LexEntry/Normal` layout across objects (entry → lexeme form → senses), emits
	/// headers/indentation, hides empty ifdata fields, binds every editable field to LCModel by
	/// metadata, and edits commit through the fenced session as one global undo step.
	/// </summary>
	[TestFixture]
	public class FullEntryRegionComposerTests : MemoryOnlyBackendProviderTestBase
	{
		private ILexEntry m_entry;

		public override void TestSetup()
		{
			base.TestSetup();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				m_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				var morph = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				m_entry.LexemeFormOA = morph;
				morph.Form.set_String(Cache.DefaultVernWs, TsStringUtils.MakeString("casa", Cache.DefaultVernWs));
				m_entry.CitationForm.set_String(Cache.DefaultVernWs, TsStringUtils.MakeString("casa-cit", Cache.DefaultVernWs));
				var senseFactory = Cache.ServiceLocator.GetInstance<ILexSenseFactory>();
				for (var i = 0; i < 2; i++)
				{
					var sense = senseFactory.Create();
					m_entry.SensesOS.Add(sense);
					sense.Gloss.set_String(Cache.DefaultAnalWs,
						TsStringUtils.MakeString("gloss " + (i + 1), Cache.DefaultAnalWs));
				}
			});
		}

		[Test]
		public void Compose_WalksTheFullCompiledLayout_AcrossObjects()
		{
			var composed = FullEntryRegionComposer.Compose(m_entry, Cache);
			Assert.That(composed, Is.Not.Null, "the shipped layouts must compose");
			var fields = composed.Model.Fields;

			Assert.That(fields.Count, Is.GreaterThan(10),
				$"the complete view is far richer than the 3-field first slice (got {fields.Count})");
			Assert.That(fields.Any(f => f.Field == "CitationForm" && f.Kind == RegionFieldKind.Text), Is.True,
				"entry-level fields come from the LexEntry layout");
			Assert.That(fields.Any(f => f.Field == "Form" && f.Kind == RegionFieldKind.Text), Is.True,
				"the lexeme form crosses into the MoForm object's layout");
			Assert.That(fields.Any(f => f.Field == "MorphType" && f.Kind == RegionFieldKind.Chooser
					&& f.Options.Count >= 2), Is.True,
				"the morph-type chooser survives in the composed view with LCModel options");

			var glossFields = fields.Where(f => f.Field == "Gloss" && f.Kind == RegionFieldKind.Text).ToList();
			Assert.That(glossFields.Count, Is.EqualTo(2), "one gloss row per sense");
			Assert.That(glossFields.Select(f => f.StableId).Distinct().Count(), Is.EqualTo(2),
				"each sense's gloss binds its own object");
			Assert.That(glossFields.All(f => f.Indent > 0), Is.True, "sense fields are indented");

			Assert.That(fields.Any(f => f.Kind == RegionFieldKind.Header && f.Field == "Senses"), Is.True,
				"the senses sequence renders a section header");
		}

		[Test]
		public void Compose_GrammaticalInfo_IsEditablePosChooser_AndSetterFindsOrCreatesMsa()
		{
			IPartOfSpeech noun = null, verb = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var posFactory = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>();
				noun = posFactory.Create();
				Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Add(noun);
				noun.Name.set_String(Cache.DefaultAnalWs, TsStringUtils.MakeString("Noun", Cache.DefaultAnalWs));
				verb = posFactory.Create();
				Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Add(verb);
				verb.Name.set_String(Cache.DefaultAnalWs, TsStringUtils.MakeString("Verb", Cache.DefaultAnalWs));

				var msa = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
				m_entry.MorphoSyntaxAnalysesOC.Add(msa);
				msa.PartOfSpeechRA = noun;
				m_entry.SensesOS[0].MorphoSyntaxAnalysisRA = msa;
			});

			var composed = FullEntryRegionComposer.Compose(m_entry, Cache);
			var gramInfo = composed.Model.Fields.SingleOrDefault(f =>
				f.Field == "MorphoSyntaxAnalysis" && f.Kind == RegionFieldKind.Chooser
				&& f.ObjectHvo == m_entry.SensesOS[0].Hvo);

			Assert.That(gramInfo, Is.Not.Null, "the sense Grammatical Info slice composes as an editable POS chooser");
			Assert.That(gramInfo.IsEditable, Is.True);
			Assert.That(gramInfo.SelectedOptionKey, Is.EqualTo(noun.Guid.ToString()),
				"the current MSA's part of speech is pre-selected");
			Assert.That(gramInfo.Options.Any(o => o.Key == verb.Guid.ToString()), Is.True,
				"the project's parts of speech are offered as options");

			// Choosing a different POS find-or-creates the matching MSA on the entry and assigns it to THIS sense.
			Assert.That(composed.EditContext.TrySetOption(gramInfo, verb.Guid.ToString()), Is.True);
			composed.EditContext.Commit();

			var resultMsa = m_entry.SensesOS[0].MorphoSyntaxAnalysisRA as IMoStemMsa;
			Assert.That(resultMsa, Is.Not.Null, "the sense still has a stem MSA after the edit");
			Assert.That(resultMsa.PartOfSpeechRA, Is.EqualTo(verb),
				"the sense's grammatical info now points at the chosen part of speech");
		}

		// GAP 2: the legacy Lexeme Form slice's button is its slice TREE-NODE MENU — MoForm-Detail-
		// AsLexemeForm (MorphologyParts.xml:219-221) binds menu="mnuDataTree-LexemeForm"
		// (DataTreeInclude.xml:336-341: Show in Concordance / Swap with Allomorph / Convert to
		// Affix Process/Allomorph), not a chooser launcher. The composed Lexeme Form text row must
		// carry that binding so the view can draw its hover gear and the host can show the SAME
		// xCore menu — data-driven from `menu=`, not hardcoded to "LexemeForm".
		[Test]
		public void Compose_LexemeFormRow_CarriesItsLegacySliceMenuBinding()
		{
			var composed = FullEntryRegionComposer.Compose(m_entry, Cache);
			var lexemeForm = composed.Model.Fields.Single(f => f.Field == "Form"
				&& f.Kind == RegionFieldKind.Text && f.ObjectHvo == m_entry.LexemeFormOA.Hvo);

			Assert.That(lexemeForm.Label,
				Is.EqualTo(StringTable.Table.LocalizeAttributeValue("Lexeme Form")),
				"labels resolve through the StringTable lane used by layout composition");
			Assert.That(lexemeForm.MenuId, Is.EqualTo("mnuDataTree-LexemeForm"),
				"the layout's slice menu rides the row — it feeds the hover gear AND label right-click");
			Assert.That(lexemeForm.ContextMenuId, Is.EqualTo("mnuDataTree-LexemeFormContext"),
				"the in-string context menu binding survives alongside");
		}

		[Test]
		public void Compose_RichTextReplacement_PreservesStringTableLabels_AndFwAvaloniaMessageLane()
		{
			var composed = FullEntryRegionComposer.Compose(m_entry, Cache);
			var lexemeForm = composed.Model.Fields.Single(f => f.Field == "Form"
				&& f.Kind == RegionFieldKind.Text && f.ObjectHvo == m_entry.LexemeFormOA.Hvo);

			Assert.That(lexemeForm.Label,
				Is.EqualTo(StringTable.Table.LocalizeAttributeValue("Lexeme Form")),
				"field labels continue to resolve through StringTable after rich-text row projection");
			Assert.That(FwAvaloniaStrings.LexemeFormRequired, Is.Not.Null.And.Not.Empty,
				"product-facing validation message text remains sourced from the Avalonia localization accessor");
		}

		[Test]
		public void TestLangProj_WritingSystemStore_ContainsRtlAndKhmerFixtures()
		{
			var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
			while (dir != null && !File.Exists(Path.Combine(dir.FullName,
				"TestLangProj", "WritingSystemStore", "ar-IQ.ldml")))
				dir = dir.Parent;

			Assert.That(dir, Is.Not.Null,
				"expected to find TestLangProj/WritingSystemStore while walking up from the test directory");
			var wsStore = Path.Combine(dir.FullName, "TestLangProj", "WritingSystemStore");

			var rtlPath = Path.Combine(wsStore, "ar-IQ.ldml");
			var khmerPath = Path.Combine(wsStore, "km.ldml");

			Assert.That(File.Exists(rtlPath), Is.True, "RTL scenario fixture must exist");
			Assert.That(File.Exists(khmerPath), Is.True, "Khmer complex-script fixture must exist");

			var rtl = File.ReadAllText(rtlPath);
			var khmer = File.ReadAllText(khmerPath);
			Assert.That(rtl, Does.Contain("right-to-left"));
			Assert.That(khmer, Does.Contain("type=\"km\""));
			Assert.That(khmer, Does.Contain("Khmr"),
				"Khmer fixture declares the Khmer script for automation/manual scenario setup");
		}

		// Review finding A: compiled definitions are memoized per (class, layout) for the lifetime
		// of the loaded sources — a repeat compose serves every layout from the memo instead of
		// rebuilding and re-fingerprinting the ~300KB parts snapshot per object per compose.
		[Test]
		public void Compose_RepeatCompose_ServesCompiledLayoutsFromTheMemo()
		{
			Assert.That(FullEntryRegionComposer.Compose(m_entry, Cache), Is.Not.Null,
				"priming compose populates the (class, layout) memo");
			var compilesAfterFirst = FullEntryRegionComposer.SnapshotCompileCount;
			Assert.That(compilesAfterFirst, Is.GreaterThan(0), "the first compose really compiled");

			var second = FullEntryRegionComposer.Compose(m_entry, Cache);
			Assert.That(second, Is.Not.Null);
			Assert.That(second.Model.Fields, Is.Not.Empty, "the memoized models still compose fully");
			Assert.That(FullEntryRegionComposer.SnapshotCompileCount, Is.EqualTo(compilesAfterFirst),
				"a repeat compose must not rebuild any layout snapshot");
		}

		[Test]
		public void Compose_HidesEmptyIfDataFields_AndShowsThemOnceFilled()
		{
			var before = FullEntryRegionComposer.Compose(m_entry, Cache).Model.Fields;
			Assert.That(before.Any(f => f.Field == "Bibliography"), Is.False,
				"an empty ifdata field (Bibliography) is hidden, matching legacy");

			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
				m_entry.Bibliography.set_String(Cache.DefaultAnalWs,
					TsStringUtils.MakeString("Smith 1999", Cache.DefaultAnalWs)));

			var after = FullEntryRegionComposer.Compose(m_entry, Cache).Model.Fields;
			Assert.That(after.Any(f => f.Field == "Bibliography"), Is.True,
				"the field appears once it has data");
		}

		// Parity update: the legacy enumComboBox is a CLOSED combo over the layout's stringList
		// labels (EnumComboSlice) — legacy is EDITABLE. The importer now carries the
		// <deParams><stringList> ids/group onto the node, so the composer renders an EDITABLE
		// option Chooser fed by that localized list (FullEntryRegionComposer.WalkEnumCombo +
		// EditorKindMap). The stored enum integer is the 0-based index into the ids; committing a
		// valid option index sets the property, while the closed combo rejects any out-of-range
		// index so it can never persist an invalid enum value. The Allomorph Status slice
		// (Morphology.fwlayout AsLexemeFormBasic over MoForm-Detail-AllomorphStatus) is the
		// enumComboBox the entry walk reaches; it is backed by the IsAbstract boolean flid.
		[Test]
		public void Compose_EnumComboBox_ComposesAsEditableStringListChooser()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
				m_entry.LexemeFormOA.IsAbstract = true);

			var composed = FullEntryRegionComposer.Compose(m_entry, Cache);
			var row = composed.Model.Fields.Single(f => f.Field == "IsAbstract"
				&& f.ObjectHvo == m_entry.LexemeFormOA.Hvo);

			Assert.That(row.Kind, Is.EqualTo(RegionFieldKind.EnumCombo),
				"§19e: the enumComboBox composes as the dedicated closed enum-combo kind (a non-editable "
				+ "drop-down), not the search/flyout chooser — so a free-form enum value can never be typed in");
			Assert.That(row.IsEditable, Is.True, "legacy enum combos are editable");
			Assert.That(row.Options, Is.Not.Null.And.Not.Empty,
				"the localized stringList labels are exposed as the chooser options");
			Assert.That(row.Options.Select(o => o.Key),
				Is.EquivalentTo(row.Options.Select((o, i) => i.ToString(CultureInfo.InvariantCulture))),
				"option keys are the 0-based enum indices the stored int maps to");
			Assert.That(row.SelectedOptionKey, Is.EqualTo("1"),
				"the stored enum int (IsAbstract == true -> 1) selects the matching option");

			// An out-of-range index is rejected by the closed combo — no invalid enum persists.
			Assert.That(composed.EditContext.TrySetOption(row,
				row.Options.Count.ToString(CultureInfo.InvariantCulture)), Is.False,
				"an out-of-range option index is rejected so no invalid enum value can persist");

			// Committing a valid in-range option index sets the enum int.
			Assert.That(composed.EditContext.TrySetOption(row, "0"), Is.True,
				"a valid option index commits through the registered setter");
			composed.EditContext.Commit();
			Assert.That(m_entry.LexemeFormOA.IsAbstract, Is.False,
				"option index 0 wrote the enum int back through the boolean-backed flid");
		}

		// Multi-run/styled content IS editable as plain-text-over-preserved-runs: the plain-text
		// setter rejects rich rows (so it can never flatten them), while a keystroke replays the
		// untouched runs around the edit through the rich setter, preserving every run's metadata.
		[Test]
		public void Compose_RichAlternative_StaysEditable_AndAKeystrokePreservesUntouchedRuns()
		{
			// Plain single-run content stays editable.
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
				m_entry.Bibliography.set_String(Cache.DefaultAnalWs,
					TsStringUtils.MakeString("Smith 1999", Cache.DefaultAnalWs)));
			var plain = FullEntryRegionComposer.Compose(m_entry, Cache).Model.Fields
				.Single(f => f.Field == "Bibliography");
			Assert.That(plain.IsEditable, Is.True, "plain single-run content keeps the text editor");

			// Now make the alternative rich: two runs in different writing systems.
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var bldr = TsStringUtils.MakeIncStrBldr();
				bldr.SetIntPropValues((int)FwTextPropType.ktptWs,
					(int)FwTextPropVar.ktpvDefault, Cache.DefaultAnalWs);
				bldr.Append("Smith ");
				bldr.SetIntPropValues((int)FwTextPropType.ktptWs,
					(int)FwTextPropVar.ktpvDefault, Cache.DefaultVernWs);
				bldr.Append("1999");
				m_entry.Bibliography.set_String(Cache.DefaultAnalWs, bldr.GetString());
			});

			var composed = FullEntryRegionComposer.Compose(m_entry, Cache);
			var rich = composed.Model.Fields.Single(f => f.Field == "Bibliography");
			Assert.That(rich.Values.Any(v => v.Value == "Smith 1999"), Is.True,
				"the rich content still displays as text");
			Assert.That(rich.Values.Single().RichText, Is.Not.Null,
				"the shared value projection now preserves the rich-run metadata for the owned editor");
			Assert.That(rich.Values.Single().RequiresRichEditor, Is.True,
				"the row advertises that the plain-text lane must not edit it");
			Assert.That(rich.IsEditable, Is.True,
				"rich content is now editable through the run-aware editor path");
			Assert.That(composed.EditContext.TrySetText(rich,
				Cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Id, "flattened"),
				Is.False, "the plain-text setter must reject rich rows so it cannot flatten them");

			var updated = RegionRichTextEditAlgorithms.ApplyPlainTextEdit(
				rich.Values.Single().RichText, "Smith 2001");
			Assert.That(composed.EditContext.TrySetRichText(rich,
				Cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Id, updated), Is.True);
			composed.EditContext.Commit();

			var stored = m_entry.Bibliography.get_String(Cache.DefaultAnalWs);
			Assert.That(stored.Text, Is.EqualTo("Smith 2001"));
			Assert.That(stored.RunCount, Is.EqualTo(2), "the rich edit must preserve run boundaries");
			Assert.That(TsStringUtils.GetWsOfRun(stored, 0), Is.EqualTo(Cache.DefaultAnalWs));
			Assert.That(TsStringUtils.GetWsOfRun(stored, 1), Is.EqualTo(Cache.DefaultVernWs));
		}

		[Test]
		public void Build_RichLexemeForm_StaysEditable_AndRoundTripsRunMetadata()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var bldr = TsStringUtils.MakeIncStrBldr();
				bldr.SetIntPropValues((int)FwTextPropType.ktptWs,
					(int)FwTextPropVar.ktpvDefault, Cache.DefaultVernWs);
				bldr.Append("ផ្ទះ");
				bldr.SetIntPropValues((int)FwTextPropType.ktptWs,
					(int)FwTextPropVar.ktpvDefault, Cache.DefaultAnalWs);
				bldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Emphasis");
				bldr.Append(" house");
				m_entry.LexemeFormOA.Form.set_String(Cache.DefaultVernWs, bldr.GetString());
			});

			var model = LexicalEditRegionBuilder.Build(m_entry, Cache);
			var form = model.Fields.Single(f => f.Field == "Form");

			Assert.That(form.IsEditable, Is.True,
				"the first-slice builder now exposes rich TsString fields through the run-aware editor path");
			Assert.That(form.Values.Single().RichText, Is.Not.Null);
			Assert.That(form.Values.Single().RichText.Runs.Count, Is.EqualTo(2));
			Assert.That(form.Values.Single().RichText.Runs[0].Text, Is.EqualTo("ផ្ទះ"));
			Assert.That(form.Values.Single().RichText.Runs[1].NamedStyle, Is.EqualTo("Emphasis"));

			var context = new LexicalEditRegionEditContext(m_entry, Cache);
			Assert.That(context.TrySetText(form, form.Values.Single().WsTag, "flattened"), Is.False,
				"the plain-text setter must reject rich rows so it cannot flatten the TsString");

			var edited = RegionRichTextEditAlgorithms.ApplyPlainTextEdit(form.Values.Single().RichText,
				"ផ្ទះថ្មី house");
			Assert.That(context.TrySetRichText(form, form.Values.Single().WsTag, edited), Is.True);
			context.Commit();

			var stored = m_entry.LexemeFormOA.Form.get_String(Cache.DefaultVernWs);
			Assert.That(stored.Text, Is.EqualTo("ផ្ទះថ្មី house"));
			Assert.That(stored.RunCount, Is.EqualTo(2));
			Assert.That(TsStringUtils.GetWsOfRun(stored, 0), Is.EqualTo(Cache.DefaultVernWs));
			Assert.That(TsStringUtils.GetWsOfRun(stored, 1), Is.EqualTo(Cache.DefaultAnalWs));
 			Assert.That(stored.get_Properties(1)
				.GetStrPropValue((int)FwTextPropType.ktptNamedStyle), Is.EqualTo("Emphasis"));

			var roundTripped = RegionRichTextAdapter.ToTsString(form.Values.Single().RichText,
				Cache.WritingSystemFactory, Cache.DefaultVernWs);
			Assert.That(roundTripped.Text, Is.EqualTo("ផ្ទះ house"));
			Assert.That(roundTripped.RunCount, Is.EqualTo(2),
				"the neutral rich-text projection must round-trip back to a TsString without flattening runs");
			Assert.That(TsStringUtils.GetWsOfRun(roundTripped, 0), Is.EqualTo(Cache.DefaultVernWs));
			Assert.That(TsStringUtils.GetWsOfRun(roundTripped, 1), Is.EqualTo(Cache.DefaultAnalWs));
			Assert.That(roundTripped.get_Properties(1)
				.GetStrPropValue((int)FwTextPropType.ktptNamedStyle), Is.EqualTo("Emphasis"));
		}

		// §19c: an external-link ORC is NO LONGER a blanket read-only block — it composes EDITABLE (the
		// link is insert/edit/delete here, the ORC is deletable), and a rich write-back round-trips the
		// link's ObjData losslessly through the adapter.
		[Test]
		public void Build_RichLexemeForm_WithLinkObjData_ComposesEditable_AndRichWriteBackRoundTripsTheLink()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var text = TsStringUtils.MakeString("link", Cache.DefaultVernWs);
				var bldr = text.GetBldr();
				bldr.SetStrPropValue(0, bldr.Length, (int)FwTextPropType.ktptObjData,
					((char)FwObjDataTypes.kodtExternalPathName) + "https://software.sil.org/fieldworks");
				m_entry.LexemeFormOA.Form.set_String(Cache.DefaultVernWs, bldr.GetString());
			});

			var model = LexicalEditRegionBuilder.Build(m_entry, Cache);
			var form = model.Fields.Single(f => f.Field == "Form");
			Assert.That(form.IsEditable, Is.True,
				"a link ORC is editable (§19c): no longer a blanket read-only block");
			var value = form.Values.Single();
			Assert.That(value.CanEditRichText, Is.True);
			Assert.That(value.RichText.Runs.Single().OrcKind, Is.EqualTo(RegionOrcKind.ExternalLink));
			Assert.That(value.RichText.Runs.Single().HyperlinkUrl,
				Is.EqualTo("https://software.sil.org/fieldworks"));

			var context = new LexicalEditRegionEditContext(m_entry, Cache);
			Assert.That(context.TrySetRichText(form, value.WsTag, value.RichText), Is.True,
				"a link-bearing value writes back through the rich seam");
			context.Commit();

			var stored = m_entry.LexemeFormOA.Form.get_String(Cache.DefaultVernWs);
			var objData = stored.get_Properties(0).GetStrPropValue((int)FwTextPropType.ktptObjData);
			Assert.That(objData, Is.Not.Null.And.Length.GreaterThan(0),
				"the link's ObjData round-trips back into the stored TsString");
			Assert.That(objData[0], Is.EqualTo((char)FwObjDataTypes.kodtExternalPathName));
			Assert.That(objData.Substring(1), Is.EqualTo("https://software.sil.org/fieldworks"));
		}

		// §19c T4 (real-cache workflow): edit a run-bearing field → apply a named character style over a
		// span → retag another span's writing system → insert a hyperlink → commit → recompose → verify the
		// run props (ktptNamedStyle, ktptWs) and the link ObjData all round-tripped through the adapter, as
		// ONE undoable step on the global stack.
		[Test]
		public void Workflow_StyleSpan_RetagSpan_InsertLink_RoundTripsThroughTheComposedContext()
		{
			// 'fr' must EXIST as a known writing system (so the per-run retag target resolves), but it must
			// NOT be added to the current ANALYSIS writing systems — doing so would give the Bibliography
			// multistring a second value row and break the single-value assertions below. A run's writing
			// system is independent of the field's displayed-WS list, so GetOrSet alone is the right target.
			string frTag = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				Cache.ServiceLocator.WritingSystemManager.GetOrSet("fr", out var fr);
				frTag = fr.Id;
				m_entry.Bibliography.set_String(Cache.DefaultAnalWs,
					TsStringUtils.MakeString("alpha beta gamma", Cache.DefaultAnalWs));
			});

			var region = FullEntryRegionComposer.Compose(m_entry, Cache);
			var field = region.Model.Fields.Single(f => f.Field == "Bibliography");
			Assert.That(field.IsEditable, Is.True);
			var wsTag = field.Values.Single().WsTag;
			var rich = field.Values.Single().RichText;

			// Apply "Emphasis" over "alpha" (0..5), link "beta" (6..10), retag "gamma" (11..16) to French.
			rich = RegionRichTextEditAlgorithms.ApplySpanNamedStyle(rich, 0, 5, "Emphasis");
			Assert.That(region.EditContext.TrySetRichText(field, wsTag, rich), Is.True);
			rich = RegionRichTextEditAlgorithms.RetagSpanWritingSystem(rich, 11, 16, frTag);
			Assert.That(region.EditContext.TrySetRichText(field, wsTag, rich), Is.True);
			rich = RegionRichTextEditAlgorithms.ApplyHyperlink(rich, 6, 10, "https://software.sil.org");
			Assert.That(region.EditContext.TrySetRichText(field, wsTag, rich), Is.True);

			region.EditContext.Commit();

			// Recompose from the committed cache and verify every run prop round-tripped.
			var reopened = FullEntryRegionComposer.Compose(m_entry, Cache).Model.Fields
				.Single(f => f.Field == "Bibliography").Values.Single().RichText;
			Assert.That(reopened.PlainText, Is.EqualTo("alpha beta gamma"));
			var styled = string.Concat(reopened.Runs.Where(r => r.NamedStyle == "Emphasis").Select(r => r.Text));
			Assert.That(styled, Is.EqualTo("alpha"), "the named style round-tripped on its span");
			var retagged = string.Concat(reopened.Runs.Where(r => r.WritingSystemTag == frTag).Select(r => r.Text));
			Assert.That(retagged, Is.EqualTo("gamma"), "the ws retag round-tripped on its span");
			var linkRun = reopened.Runs.Single(r => r.OrcKind == RegionOrcKind.ExternalLink);
			Assert.That(linkRun.Text, Is.EqualTo("beta"));
			Assert.That(linkRun.HyperlinkUrl, Is.EqualTo("https://software.sil.org"),
				"the link ObjData round-tripped");

			// One undo reverts the whole gesture sequence (one fenced session).
			Cache.ActionHandlerAccessor.Undo();
			var afterUndo = FullEntryRegionComposer.Compose(m_entry, Cache).Model.Fields
				.Single(f => f.Field == "Bibliography").Values.Single().RichText;
			Assert.That(afterUndo.Runs.Any(r => r.OrcKind == RegionOrcKind.ExternalLink), Is.False,
				"one undo reverts the whole §19c gesture session");
		}

		// DATA-SAFETY (Phase 1, test a): a run carrying a TsString property the RegionTextRun model
		// does NOT round-trip (here ktptForeColor) would be SILENTLY DROPPED on the first keystroke
		// (the plain-text-changed edit skips the lossless RichXml fast-path and replays only the
		// supported props). The value is held READ-ONLY rather than corrupting on edit; full fidelity
		// stays in the classic view.
		[Test]
		public void Compose_RunWithUnsupportedProperty_ComposesReadOnly_SoAnEditCannotDropIt()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var text = TsStringUtils.MakeString("coloured", Cache.DefaultAnalWs);
				var bldr = text.GetBldr();
				// A foreground colour is preserved in the rich XML for display but is NOT one of the
				// run properties the adapter re-emits, so an edit would drop it.
				bldr.SetIntPropValues(0, bldr.Length, (int)FwTextPropType.ktptForeColor,
					(int)FwTextPropVar.ktpvDefault, 0x0000FF);
				m_entry.Bibliography.set_String(Cache.DefaultAnalWs, bldr.GetString());
			});

			var rich = FullEntryRegionComposer.Compose(m_entry, Cache).Model.Fields
				.Single(f => f.Field == "Bibliography");
			Assert.That(rich.Values.Single().RichText.LossyProperties, Is.True,
				"the run's foreground colour is not round-tripped by the run model, so it is lossy");
			Assert.That(rich.Values.Single().CanEditRichText, Is.False,
				"a lossy value must be held read-only so a keystroke cannot drop the colour");
			Assert.That(rich.IsEditable, Is.False,
				"the composed row is read-only when its only value cannot be edited safely");

			// The lossless RichXml still drives full-fidelity display: round-tripping the unedited
			// value reproduces the original TsString, colour and all (test c, lossy variant).
			var roundTripped = RegionRichTextAdapter.ToTsString(rich.Values.Single().RichText,
				Cache.WritingSystemFactory, Cache.DefaultAnalWs);
			Assert.That(roundTripped.get_Properties(0)
				.GetIntPropValues((int)FwTextPropType.ktptForeColor, out _), Is.EqualTo(0x0000FF),
				"the lossless RichXml round-trip preserves the colour for display");
			// The matching headless test that the lossy value RENDERS a read-only editor with the
			// not-editable-here tooltip lives in FwAvaloniaTests (the Avalonia rendering lane):
			// LexicalEditRegionEditingTests.LossyValue_RendersReadOnly_WithTooltip.
		}

		// DATA-SAFETY (Phase 1, test b): a multi-run value carrying ONLY supported properties
		// (named style + per-run WS + bold) STAYS editable, and a mid-run keystroke preserves every
		// untouched run's properties through the full FromTsString -> edit -> ToTsString round-trip.
		[Test]
		public void Edit_MidRunKeystroke_OnSupportedMultiRunValue_PreservesEveryUntouchedRunProperty()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var bldr = TsStringUtils.MakeIncStrBldr();
				// Run 0: analysis WS, named style + bold.
				bldr.SetIntPropValues((int)FwTextPropType.ktptWs,
					(int)FwTextPropVar.ktpvDefault, Cache.DefaultAnalWs);
				bldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Emphasis");
				bldr.SetIntPropValues((int)FwTextPropType.ktptBold,
					(int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
				bldr.Append("Smith");
				// Run 1: vernacular WS, plain (a different WS forces a run boundary).
				bldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, null);
				bldr.SetIntPropValues((int)FwTextPropType.ktptBold, -1, -1);
				bldr.SetIntPropValues((int)FwTextPropType.ktptWs,
					(int)FwTextPropVar.ktpvDefault, Cache.DefaultVernWs);
				bldr.Append(" 1999");
				m_entry.Bibliography.set_String(Cache.DefaultAnalWs, bldr.GetString());
			});

			var rich = FullEntryRegionComposer.Compose(m_entry, Cache).Model.Fields
				.Single(f => f.Field == "Bibliography");
			Assert.That(rich.Values.Single().RichText.LossyProperties, Is.False,
				"named style + per-run WS + bold are all round-tripped, so the value is not lossy");
			Assert.That(rich.IsEditable, Is.True, "a value with only supported props stays editable");

			// Mid-run keystroke INSIDE run 1 ("1999" -> "19999"): legacy insertion attaches to the
			// preceding (containing) run, so run 0's style/bold must survive untouched.
			var value = rich.Values.Single();
			var edited = RegionRichTextEditAlgorithms.ApplyPlainTextEdit(value.RichText, "Smith 19999");

			var composed = FullEntryRegionComposer.Compose(m_entry, Cache);
			var richField = composed.Model.Fields.Single(f => f.Field == "Bibliography");
			Assert.That(composed.EditContext.TrySetRichText(richField,
				richField.Values.Single().WsTag, edited), Is.True);
			composed.EditContext.Commit();

			var stored = m_entry.Bibliography.get_String(Cache.DefaultAnalWs);
			Assert.That(stored.Text, Is.EqualTo("Smith 19999"));
			Assert.That(stored.RunCount, Is.EqualTo(2), "the keystroke kept the run boundary");
			// Run 0 untouched: WS, named style and bold all survive the edit round-trip.
			Assert.That(stored.get_RunText(0), Is.EqualTo("Smith"));
			Assert.That(TsStringUtils.GetWsOfRun(stored, 0), Is.EqualTo(Cache.DefaultAnalWs));
			Assert.That(stored.get_Properties(0)
				.GetStrPropValue((int)FwTextPropType.ktptNamedStyle), Is.EqualTo("Emphasis"));
			Assert.That(stored.get_Properties(0)
				.GetIntPropValues((int)FwTextPropType.ktptBold, out _),
				Is.EqualTo((int)FwTextToggleVal.kttvForceOn), "run 0's bold survived the mid-run edit");
			// Run 1 carries the edit and keeps its own WS.
			Assert.That(stored.get_RunText(1), Is.EqualTo(" 19999"));
			Assert.That(TsStringUtils.GetWsOfRun(stored, 1), Is.EqualTo(Cache.DefaultVernWs));
		}

		// PHASE 2 (test e): applying bold over a selected span, then committing through the edit
		// context, stores a TsString whose covered span is bold while the rest stays plain — i.e.
		// ApplySpanFormatting's new bold run round-trips through ToTsString into the domain.
		[Test]
		public void Edit_ApplySpanFormatting_BoldOverSpan_RoundTripsThroughToTsString()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				// A single plain run "important note".
				m_entry.Bibliography.set_String(Cache.DefaultAnalWs,
					TsStringUtils.MakeString("important note", Cache.DefaultAnalWs));
			});

			var composedBefore = FullEntryRegionComposer.Compose(m_entry, Cache);
			var richField = composedBefore.Model.Fields.Single(f => f.Field == "Bibliography");
			var value = richField.Values.Single();
			Assert.That(value.RichText.LossyProperties, Is.False);

			// Bold the first word "important" (indices 0..9).
			var formatted = RegionRichTextEditAlgorithms.ApplySpanFormatting(value.RichText, 0, 9,
				RegionRunFormat.Bold, true);
			Assert.That(formatted.PlainText, Is.EqualTo("important note"),
				"formatting never changes the plain text");

			Assert.That(composedBefore.EditContext.TrySetRichText(richField, value.WsTag, formatted),
				Is.True);
			composedBefore.EditContext.Commit();

			var stored = m_entry.Bibliography.get_String(Cache.DefaultAnalWs);
			Assert.That(stored.Text, Is.EqualTo("important note"));
			Assert.That(stored.RunCount, Is.EqualTo(2), "the span boundary split the value into two runs");
			Assert.That(stored.get_RunText(0), Is.EqualTo("important"));
			Assert.That(stored.get_Properties(0)
				.GetIntPropValues((int)FwTextPropType.ktptBold, out _),
				Is.EqualTo((int)FwTextToggleVal.kttvForceOn), "the selected span is bold in the domain");
			Assert.That(stored.get_RunText(1), Is.EqualTo(" note"));
			Assert.That(stored.get_Properties(1)
				.GetIntPropValues((int)FwTextPropType.ktptBold, out _), Is.LessThanOrEqualTo(0),
				"the unselected tail stays non-bold");
		}

		// PHASE 3 (test c): applying a NAMED CHARACTER STYLE over a selected span, then committing
		// through the edit context, stores a TsString whose covered span carries ktptNamedStyle while
		// the rest stays unstyled. Clearing the style removes it.
		[Test]
		public void Edit_ApplySpanNamedStyle_OverSpan_RoundTripsKtptNamedStyleThroughToTsString()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				m_entry.Bibliography.set_String(Cache.DefaultAnalWs,
					TsStringUtils.MakeString("important note", Cache.DefaultAnalWs));
			});

			var composedBefore = FullEntryRegionComposer.Compose(m_entry, Cache);
			var richField = composedBefore.Model.Fields.Single(f => f.Field == "Bibliography");
			var value = richField.Values.Single();
			Assert.That(value.RichText.LossyProperties, Is.False);

			// Style the first word "important" (indices 0..9) with the built-in "Emphasis" style.
			var styled = RegionRichTextEditAlgorithms.ApplySpanNamedStyle(value.RichText, 0, 9, "Emphasis");
			Assert.That(styled.PlainText, Is.EqualTo("important note"),
				"styling never changes the plain text");

			Assert.That(composedBefore.EditContext.TrySetRichText(richField, value.WsTag, styled), Is.True);
			composedBefore.EditContext.Commit();

			var stored = m_entry.Bibliography.get_String(Cache.DefaultAnalWs);
			Assert.That(stored.Text, Is.EqualTo("important note"));
			Assert.That(stored.RunCount, Is.EqualTo(2), "the span boundary split the value into two runs");
			Assert.That(stored.get_RunText(0), Is.EqualTo("important"));
			Assert.That(stored.get_Properties(0).GetStrPropValue((int)FwTextPropType.ktptNamedStyle),
				Is.EqualTo("Emphasis"), "the selected span carries the named style in the domain");
			Assert.That(stored.get_RunText(1), Is.EqualTo(" note"));
			Assert.That(stored.get_Properties(1).GetStrPropValue((int)FwTextPropType.ktptNamedStyle),
				Is.Null, "the unstyled tail carries no named style");

			// Now CLEAR the style back off the same span and round-trip again: ktptNamedStyle is gone.
			var composedAfter = FullEntryRegionComposer.Compose(m_entry, Cache);
			var afterField = composedAfter.Model.Fields.Single(f => f.Field == "Bibliography");
			var afterValue = afterField.Values.Single();
			var cleared = RegionRichTextEditAlgorithms.ApplySpanNamedStyle(afterValue.RichText, 0, 9, null);
			Assert.That(composedAfter.EditContext.TrySetRichText(afterField, afterValue.WsTag, cleared), Is.True);
			composedAfter.EditContext.Commit();

			var storedCleared = m_entry.Bibliography.get_String(Cache.DefaultAnalWs);
			Assert.That(storedCleared.Text, Is.EqualTo("important note"));
			for (var i = 0; i < storedCleared.RunCount; i++)
				Assert.That(storedCleared.get_Properties(i).GetStrPropValue((int)FwTextPropType.ktptNamedStyle),
					Is.Null, "clearing removed the named style across the value");
		}

		// PHASE 3 (test d): the composer supplies the project's CHARACTER style names onto each editable
		// text field's AvailableNamedStyles (the host seam the per-WS style picker reads), sourced from
		// Cache.LangProject.StylesOC filtered to character-type styles. Paragraph styles are excluded.
		[Test]
		public void Compose_SuppliesCharacterStyleNames_OnEditableTextField()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var fact = Cache.ServiceLocator.GetInstance<IStStyleFactory>();
				var strong = fact.Create();
				Cache.LangProject.StylesOC.Add(strong);
				strong.Name = "Strong";
				strong.Type = StyleType.kstCharacter;
				var para = fact.Create();
				Cache.LangProject.StylesOC.Add(para);
				para.Name = "Block Quote";
				para.Type = StyleType.kstParagraph;
			});

			var composed = FullEntryRegionComposer.Compose(m_entry, Cache);
			var editableText = composed.Model.Fields
				.First(f => f.Kind == RegionFieldKind.Text && f.IsEditable);
			Assert.That(editableText.AvailableNamedStyles, Does.Contain("Strong"),
				"the composer stamps the project's character style names onto editable text rows");
			Assert.That(editableText.AvailableNamedStyles, Does.Not.Contain("Block Quote"),
				"paragraph-type styles are excluded — only character styles are offered");
		}

		// PHASE 4 (test d): the composer supplies the project's WRITING SYSTEMS onto each editable text
		// field's AvailableWritingSystems (the host seam the per-WS retag picker reads), sourced from the
		// project's analysis + vernacular writing systems. Each option carries the stable IETF tag.
		[Test]
		public void Compose_SuppliesWritingSystemOptions_OnEditableTextField()
		{
			var composed = FullEntryRegionComposer.Compose(m_entry, Cache);
			var editableText = composed.Model.Fields
				.First(f => f.Kind == RegionFieldKind.Text && f.IsEditable);

			Assert.That(editableText.AvailableWritingSystems, Is.Not.Empty,
				"the composer stamps the project's writing systems onto editable text rows");
			var analTag = Cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Id;
			var vernTag = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Id;
			Assert.That(editableText.AvailableWritingSystems.Select(w => w.Tag), Does.Contain(analTag),
				"the analysis writing system is offered");
			Assert.That(editableText.AvailableWritingSystems.Select(w => w.Tag), Does.Contain(vernTag),
				"the vernacular writing system is offered");
			Assert.That(editableText.AvailableWritingSystems.All(w => !string.IsNullOrEmpty(w.DisplayName)),
				Is.True, "every offered writing system carries a display name");
		}

		// PHASE 4 (test c): a per-run writing-system retag round-trips through the composed edit context,
		// preserving ktptWs on exactly the retagged span when the rebuilt TsString is stored.
		[Test]
		public void RetagWritingSystem_RoundTripsKtptWs_OverTheSpan_AndCommits()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				m_entry.Bibliography.set_String(Cache.DefaultAnalWs,
					TsStringUtils.MakeString("alpha beta", Cache.DefaultAnalWs));
			});

			var composed = FullEntryRegionComposer.Compose(m_entry, Cache);
			var field = composed.Model.Fields.Single(f => f.Field == "Bibliography");
			var value = field.Values.Single();
			var vernTag = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Id;

			// Retag "alpha" (the first 5 chars) to the vernacular ws.
			var retagged = RegionRichTextEditAlgorithms.RetagSpanWritingSystem(value.RichText, 0, 5, vernTag);
			Assert.That(composed.EditContext.TrySetRichText(field, value.WsTag, retagged), Is.True);
			composed.EditContext.Commit();

			var stored = m_entry.Bibliography.get_String(Cache.DefaultAnalWs);
			Assert.That(stored.Text, Is.EqualTo("alpha beta"), "retag never changes the text");
			Assert.That(stored.RunCount, Is.GreaterThanOrEqualTo(2), "the retag split the value into runs");
			// The first run carries the vernacular ws; a later run carries the analysis ws.
			Assert.That(SIL.LCModel.Core.Text.TsStringUtils.GetWsOfRun(stored, 0),
				Is.EqualTo(Cache.DefaultVernWs), "the retagged span carries ktptWs = the new writing system");
			Assert.That(SIL.LCModel.Core.Text.TsStringUtils.GetWsOfRun(stored, stored.RunCount - 1),
				Is.EqualTo(Cache.DefaultAnalWs), "the untouched tail keeps its original writing system");
		}

		// TASK 2 (reversal plugin, test a): a sense with a reversal entry composes an EDITABLE reversal
		// row through the ReversalIndexEntryPlugin — not the lone Unsupported row.
		[Test]
		public void Compose_SenseWithReversalEntry_ComposesEditableReversalRow_NotUnsupported()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var revIndex = Cache.ServiceLocator.GetInstance<IReversalIndexRepository>()
					.FindOrCreateIndexForWs(Cache.DefaultAnalWs);
				var riEntry = revIndex.FindOrCreateReversalEntry("dwelling");
				riEntry.SensesRS.Add(m_entry.SensesOS[0]);
			});

			var composed = FullEntryRegionComposer.Compose(m_entry, Cache);

			// The reversal slice composes a Custom (plugin) row, never an Unsupported row.
			var reversalRow = composed.Model.Fields
				.FirstOrDefault(f => f.Kind == RegionFieldKind.Custom
					&& (f.Field == "ReferringReversalIndexEntries"
						|| (f.Label != null && f.Label.IndexOf("Reversal", System.StringComparison.OrdinalIgnoreCase) >= 0)));
			Assert.That(reversalRow, Is.Not.Null, "the reversal slice composes as a plugin (Custom) row");
			Assert.That(reversalRow.ControlFactory, Is.Not.Null, "the row carries the plugin control factory");

			// Building the control yields an editable FwMultiWsTextField (not the unsupported rendering).
			var control = reversalRow.ControlFactory();
			Assert.That(control,
				Is.InstanceOf<SIL.FieldWorks.Common.FwAvalonia.Region.FwMultiWsTextField>(),
				"the reversal plugin builds the editable multi-WS reversal-forms field");
		}

		// TASK 2 (reversal plugin, test b): editing a reversal form stages/commits through the edit
		// context — the reversal entry's ReversalForm is updated, on the same fenced session as the region.
		[Test]
		public void ReversalPlugin_EditingAForm_StagesAndCommitsThroughTheEditContext()
		{
			IReversalIndexEntry riEntry = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var revIndex = Cache.ServiceLocator.GetInstance<IReversalIndexRepository>()
					.FindOrCreateIndexForWs(Cache.DefaultAnalWs);
				riEntry = revIndex.FindOrCreateReversalEntry("dwelling");
				riEntry.SensesRS.Add(m_entry.SensesOS[0]);
			});

			var composed = FullEntryRegionComposer.Compose(m_entry, Cache);
			var sense = m_entry.SensesOS[0];
			var plugin = new ReversalIndexEntryPlugin();
			// Reuse the composer's resolved node so the plugin gets real metadata; resolve via the plugin
			// directly with a build context closing over the composed edit context.
			var node = composed.Model.Fields.First(f => f.Kind == RegionFieldKind.Custom
				&& f.ObjectHvo == sense.Hvo);
			var buildContext = new RegionEditorBuildContext(sense, null, () => composed.EditContext, Cache);
			var reversalControl = (SIL.FieldWorks.Common.FwAvalonia.Region.FwMultiWsTextField)
				plugin.BuildControl(buildContext);
			Assert.That(reversalControl, Is.Not.Null);

			// Stage an edit through the reversal context exposed by building the field's own context. We
			// drive the edit through the composed edit context indirectly: the field staged via the
			// plugin's ReversalRegionEditContext. Address it directly to assert the data path.
			var field = new SIL.FieldWorks.Common.FwAvalonia.Region.LexicalEditRegionField(
				"reversal/" + sense.Hvo, "Reversal Entries", "ReferringReversalIndexEntries", null,
				SIL.FieldWorks.Common.FwAvalonia.Region.RegionFieldKind.Text,
				SIL.FieldWorks.Common.FwAvalonia.ViewDefinition.EditorClassification.Known,
				"ReversalEntriesEditor", null,
				SIL.FieldWorks.Common.FwAvalonia.ViewDefinition.SurfaceRouting.Product, null, null, null);
			var analTag = Cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Id;
			var entryByWsKey = new Dictionary<string, IReversalIndexEntry> { [analTag] = riEntry };
			var reversalContext = new ReversalRegionEditContext(Cache, composed.EditContext, entryByWsKey);

			Assert.That(reversalContext.TrySetText(field, analTag, "abode"), Is.True,
				"editing an existing reversal form stages through the reversal edit context");
			reversalContext.Commit();

			Assert.That(riEntry.ReversalForm.get_String(Cache.DefaultAnalWs).Text, Is.EqualTo("abode"),
				"the committed edit updated the reversal entry's form");
			Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.True,
				"the reversal edit lands on the shared global undo stack");
		}

		// DATA-SAFETY (Phase 1, test c): ToTsString of an UNEDITED value reproduces the original
		// TsString exactly via the lossless RichXml fast-path.
		[Test]
		public void RoundTrip_UneditedValue_ReproducesOriginalTsString()
		{
			ITsString original = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var bldr = TsStringUtils.MakeIncStrBldr();
				bldr.SetIntPropValues((int)FwTextPropType.ktptWs,
					(int)FwTextPropVar.ktpvDefault, Cache.DefaultAnalWs);
				bldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Emphasis");
				bldr.Append("Smith ");
				bldr.SetIntPropValues((int)FwTextPropType.ktptWs,
					(int)FwTextPropVar.ktpvDefault, Cache.DefaultVernWs);
				bldr.Append("1999");
				original = bldr.GetString();
				m_entry.Bibliography.set_String(Cache.DefaultAnalWs, original);
			});

			var value = FullEntryRegionComposer.Compose(m_entry, Cache).Model.Fields
				.Single(f => f.Field == "Bibliography").Values.Single();

			var roundTripped = RegionRichTextAdapter.ToTsString(value.RichText,
				Cache.WritingSystemFactory, Cache.DefaultAnalWs);
			Assert.That(roundTripped.Equals(original), Is.True,
				"an unedited value round-trips identically through the lossless RichXml path");
		}

		[Test]
		public void Edit_NestedSecondSenseGloss_CommitsAsOneGlobalUndoStep()
		{
			var composed = FullEntryRegionComposer.Compose(m_entry, Cache);
			var secondGloss = composed.Model.Fields
				.Where(f => f.Field == "Gloss" && f.Kind == RegionFieldKind.Text)
				.Skip(1).First();
			var wsAbbrev = secondGloss.Values[0].WsAbbrev;

			Assert.That(composed.EditContext.TrySetText(secondGloss, wsAbbrev, "edited gloss"), Is.True,
				"composed fields stage by their per-object stable id");
			composed.EditContext.Commit();

			Assert.That(m_entry.SensesOS[1].Gloss.get_String(Cache.DefaultAnalWs).Text, Is.EqualTo("edited gloss"));
			Assert.That(m_entry.SensesOS[0].Gloss.get_String(Cache.DefaultAnalWs).Text, Is.EqualTo("gloss 1"),
				"only the addressed sense changed");

			Cache.ActionHandlerAccessor.Undo();
			Assert.That(m_entry.SensesOS[1].Gloss.get_String(Cache.DefaultAnalWs).Text, Is.EqualTo("gloss 2"),
				"the composed edit is one step on the global undo stack");
		}

		[Test]
		public void Compose_ShowHiddenFields_SurfacesNeverAndEmptyIfdataFields()
		{
			var normal = FullEntryRegionComposer.Compose(m_entry, Cache).Model.Fields;
			var hidden = FullEntryRegionComposer.Compose(m_entry, Cache, showHiddenFields: true).Model.Fields;

			Assert.That(hidden.Count, Is.GreaterThan(normal.Count),
				"show-hidden surfaces strictly more rows, like legacy m_fShowAllFields");
			Assert.That(normal.Any(f => f.Field == "DateCreated"), Is.False,
				"visibility=never fields stay hidden by default");
			Assert.That(hidden.Any(f => f.Field == "DateCreated"), Is.True,
				"visibility=never fields appear under show-hidden");
			var created = hidden.First(f => f.Field == "DateCreated");
			Assert.That(created.Values[0].Value, Is.Not.Empty, "Time fields render a formatted date");
			Assert.That(hidden.Any(f => f.Field == "Bibliography"), Is.True,
				"empty ifdata fields appear under show-hidden");
		}

		// Finding-1 (parity, date): legacy DateSlice renders dt.ToString("f", CurrentUICulture)
		// (the full pattern, carrying the day name) — the composer must match exactly.
		[Test]
		public void Compose_DateCreated_UsesTheLegacyFullDateTimePattern()
		{
			var hidden = FullEntryRegionComposer.Compose(m_entry, Cache, showHiddenFields: true).Model.Fields;
			var created = hidden.First(f => f.Field == "DateCreated");

			Assert.That(created.Values[0].Value,
				Is.EqualTo(m_entry.DateCreated.ToString("f", CultureInfo.CurrentUICulture)),
				"Time fields render exactly like legacy DateSlice (\"f\", CurrentUICulture)");
			Assert.That(created.Values[0].Value,
				Does.Contain(CultureInfo.CurrentUICulture.DateTimeFormat.GetDayName(m_entry.DateCreated.DayOfWeek)),
				"the full pattern carries the day name");
		}

		// Finding-2 (WS spec resolution): the layout ws= spec resolves through the legacy pair —
		// WritingSystemServices.GetMagicWsIdFromName then GetWritingSystemList — not substring
		// heuristics, so ordering ("analysis vernacular") and list membership match legacy slices.
		[TestCase("all analysis")]
		[TestCase("all vernacular")]
		[TestCase("analysis vernacular")]
		[TestCase("vernacular analysis")]
		[TestCase("analysis")]
		[TestCase("vernacular")]
		public void ResolveWritingSystems_MatchesLegacyWritingSystemServices(string spec)
		{
			var magicId = WritingSystemServices.GetMagicWsIdFromName(spec);
			Assert.That(magicId, Is.Not.Zero, "the spec is a legacy magic ws name");
			var expected = WritingSystemServices.GetWritingSystemList(Cache, magicId, forceIncludeEnglish: false);
			Assert.That(expected, Is.Not.Empty);

			var actual = FullEntryRegionComposer.ResolveWritingSystems(Cache, spec);
			Assert.That(actual.Select(ws => ws.Handle), Is.EqualTo(expected.Select(ws => ws.Handle)),
				"the composer resolves ws= specs exactly like legacy SliceFactory/MultiStringSlice");
		}

		[Test]
		public void ResolveWritingSystems_Pronunciation_UsesTheProjectPronunciationList()
		{
			// Point the project pronunciation list at an IPA writing system so it differs from the
			// vernacular default — a vernacular-side heuristic cannot pass by coincidence.
			var container = Cache.ServiceLocator.WritingSystems;
			var saved = container.CurrentPronunciationWritingSystems.ToList();
			CoreWritingSystemDefinition ipa = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				Cache.ServiceLocator.WritingSystemManager.GetOrSet("fr-fonipa", out ipa);
				container.CurrentPronunciationWritingSystems.Clear();
				container.CurrentPronunciationWritingSystems.Add(ipa);
			});
			try
			{
				var actual = FullEntryRegionComposer.ResolveWritingSystems(Cache, "pronunciation");

				var expected = WritingSystemServices.GetWritingSystemList(Cache,
					WritingSystemServices.kwsPronunciations, forceIncludeEnglish: false);
				Assert.That(expected.Select(ws => ws.Handle), Is.EqualTo(new[] { ipa.Handle }));
				Assert.That(actual.Select(ws => ws.Handle), Is.EqualTo(expected.Select(ws => ws.Handle)),
					"pronunciation specs ride kwsPronunciations (legacy SliceFactory.GetWs lane), not vernacular defaults");
			}
			finally
			{
				NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
				{
					container.CurrentPronunciationWritingSystems.Clear();
					foreach (var ws in saved)
						container.CurrentPronunciationWritingSystems.Add(ws);
				});
			}
		}

		[TestCase(null)]
		[TestCase("")]
		[TestCase("not-a-ws-spec")]
		public void ResolveWritingSystems_EmptyOrUnknownSpec_FallsBackToAnalysis(string spec)
		{
			var expected = WritingSystemServices.GetWritingSystemList(Cache,
				WritingSystemServices.kwsAnal, forceIncludeEnglish: false);
			var actual = FullEntryRegionComposer.ResolveWritingSystems(Cache, spec);
			Assert.That(actual.Select(ws => ws.Handle), Is.EqualTo(expected.Select(ws => ws.Handle)),
				"unmarked/unknown specs take GetWritingSystemList's analysis default, like legacy");
		}

		// Finding-3: the Abbreviation is user-editable and can collide across writing systems;
		// composition must still succeed (no ToDictionary crash) and edits must route by the
		// unique IETF tag, never to the wrong alternative.
		[Test]
		public void Compose_DuplicateWsAbbreviations_StillComposes_AndEditsRouteByWsTag()
		{
			var container = Cache.ServiceLocator.WritingSystems;
			var defaultAnal = container.DefaultAnalysisWritingSystem;
			CoreWritingSystemDefinition second = null;
			string originalAbbrev = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				Cache.ServiceLocator.WritingSystemManager.GetOrSet("es", out second);
				container.AddToCurrentAnalysisWritingSystems(second);
				originalAbbrev = second.Abbreviation;
				second.Abbreviation = defaultAnal.Abbreviation; // collide on purpose
			});
			try
			{
				var composed = FullEntryRegionComposer.Compose(m_entry, Cache);
				Assert.That(composed, Is.Not.Null, "duplicate abbreviations must not abort composition");
				var gloss = composed.Model.Fields.First(f => f.Field == "Gloss" && f.Kind == RegionFieldKind.Text);
				Assert.That(gloss.Values.Count, Is.EqualTo(2), "one row per current analysis writing system");
				var secondRow = gloss.Values.Single(v => v.WsTag == second.Id);

				Assert.That(composed.EditContext.TrySetText(gloss, secondRow.WsTag, "glosa"), Is.True,
					"the second row's tag addresses its own alternative");
				composed.EditContext.Commit();

				Assert.That(m_entry.SensesOS[0].Gloss.get_String(second.Handle).Text, Is.EqualTo("glosa"),
					"the edit lands on the writing system addressed by tag");
				Assert.That(m_entry.SensesOS[0].Gloss.get_String(Cache.DefaultAnalWs).Text, Is.EqualTo("gloss 1"),
					"the alternative sharing the abbreviation is untouched");
				Cache.ActionHandlerAccessor.Undo();
			}
			finally
			{
				NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
				{
					second.Abbreviation = originalAbbrev;
					container.AnalysisWritingSystems.Remove(second);
					container.CurrentAnalysisWritingSystems.Remove(second);
				});
			}
		}

		[Test]
		public void Compose_BooleanFields_RenderAsCheckboxKind_AndToggle()
		{
			// Review task 2 made the enumComboBox-backed Allomorph Status row read-only, so this
			// test now exercises a REAL checkbox slice: an alternate form's "Is Abstract Form"
			// (MoStemAllomorph/Normal, editor="Checkbox", visibility=never -> shows under
			// show-hidden) over the persisted IsAbstract boolean flid.
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
				m_entry.AlternateFormsOS.Add(
					Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create()));

			var hidden = FullEntryRegionComposer.Compose(m_entry, Cache, showHiddenFields: true);
			var boolField = hidden.Model.Fields.FirstOrDefault(f => f.Kind == RegionFieldKind.Boolean);
			Assert.That(boolField, Is.Not.Null, "the entry layout carries at least one checkbox field");
			var original = boolField.SelectedOptionKey;

			Assert.That(hidden.EditContext.TrySetOption(boolField, original == "true" ? "false" : "true"), Is.True);
			hidden.EditContext.Cancel(); // viewing test: roll the toggle back
		}

		[Test]
		public void Compose_SenseHeaders_UseHierarchicalNumbering_WithGlossSummary()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var sub = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				m_entry.SensesOS[0].SensesOS.Add(sub);
				sub.Gloss.set_String(Cache.DefaultAnalWs, TsStringUtils.MakeString("subgloss", Cache.DefaultAnalWs));
			});

			var fields = FullEntryRegionComposer.Compose(m_entry, Cache).Model.Fields;
			var headers = fields.Where(f => f.Kind == RegionFieldKind.Header).Select(f => f.Label).ToList();

			Assert.That(headers.Any(h => h.StartsWith("1 ") || h.StartsWith("1 ") || h.StartsWith("1 ", System.StringComparison.Ordinal) || h.StartsWith("1")), Is.True,
				"top senses are numbered: " + string.Join(" | ", headers));
			Assert.That(headers.Any(h => h.StartsWith("1.1")), Is.True,
				"subsenses use hierarchical numbers (1.1): " + string.Join(" | ", headers));
			Assert.That(headers.Any(h => h.Contains("gloss 1")), Is.True,
				"sense headers carry the gloss summary like legacy sense lines");
			Assert.That(fields.First(f => f.Label != null && f.Label.StartsWith("1.1")).IsCollapsible, Is.True,
				"sense items are collapsible");
		}

		[Test]
		public void Edit_MorphType_InComposedView_Commits()
		{
			var composed = FullEntryRegionComposer.Compose(m_entry, Cache);
			var morphType = composed.Model.Fields.Single(f => f.Field == "MorphType" && f.Kind == RegionFieldKind.Chooser);
			var target = morphType.Options.First(o => o.Key != morphType.SelectedOptionKey);

			Assert.That(composed.EditContext.TrySetOption(morphType, target.Key), Is.True);
			composed.EditContext.Commit();
			Assert.That(m_entry.LexemeFormOA.MorphTypeRA.Guid.ToString(), Is.EqualTo(target.Key));
		}

		// Review round 2: legacy MorphTypeAtomicLauncher gates stem<->affix morph-type swaps behind a
		// data-loss prompt AND an allomorph class conversion. Until that class-conversion lane lands,
		// the composed chooser must reject a boundary-crossing assignment instead of creating a
		// model-invalid stem-allomorph-with-affix-type.
		[Test]
		public void Edit_MorphType_StemAllomorphWithAffixType_IsRejected()
		{
			var composed = FullEntryRegionComposer.Compose(m_entry, Cache);
			var morphType = composed.Model.Fields.Single(f => f.Field == "MorphType" && f.Kind == RegionFieldKind.Chooser);
			var before = m_entry.LexemeFormOA.MorphTypeRA;

			Assert.That(composed.EditContext.TrySetOption(morphType,
				MoMorphTypeTags.kguidMorphSuffix.ToString()), Is.False,
				"a suffix morph type on an IMoStemAllomorph crosses the stem/affix boundary");
			Assert.That(m_entry.LexemeFormOA.MorphTypeRA, Is.EqualTo(before), "nothing was assigned");

			Assert.That(composed.EditContext.TrySetOption(morphType,
				MoMorphTypeTags.kguidMorphRoot.ToString()), Is.True,
				"same-side (stem-type) assignments still work");
			composed.EditContext.Cancel();
		}

		// 14.1 — ghost rows: the legacy "Click here to add ..." line is an editable watermark row;
		// typing creates the missing object inside the fenced session (one undoable step) and routes
		// the text into the layout's ghost field (ghost=/ghostWs=).
		[Test]
		public void Compose_GhostLexemeForm_CreatesTheAllomorph_OnFirstEdit()
		{
			ILexEntry bare = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
				bare = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create());

			var composed = FullEntryRegionComposer.Compose(bare, Cache);
			var ghost = composed.Model.Fields.FirstOrDefault(f =>
				f.Field == "LexemeForm" && f.StableId.EndsWith("/ghost"));
			Assert.That(ghost, Is.Not.Null, "an empty lexeme form renders the legacy ghost line");
			Assert.That(ghost.GhostPrompt, Is.Not.Null.And.Not.Empty, "the prompt rides as a watermark");
			Assert.That(ghost.IsEditable, Is.True, "the ghost row accepts typing");
			Assert.That(ghost.Values[0].Value, Is.Empty, "the prompt is never field content");

			Assert.That(composed.EditContext.TrySetText(ghost, ghost.Values[0].WsAbbrev, "casa"), Is.True);
			composed.EditContext.Commit();

			Assert.That(bare.LexemeFormOA, Is.Not.Null, "typing created the missing object (ghost= lane)");
			Assert.That(bare.LexemeFormOA, Is.InstanceOf<IMoStemAllomorph>(),
				"abstract MoForm defaults to a stem allomorph, like legacy CreateAllomorph");
			Assert.That(bare.LexemeFormOA.Form.get_String(Cache.DefaultVernWs).Text, Is.EqualTo("casa"),
				"the typed text landed in the ghost field (Form, ghostWs=vernacular)");
		}

		// Review round 2: the ghost setter's closure caches the created object's hvo, but a Cancel
		// rolls the MakeNewObject back — a later edit through the SAME still-visible view must
		// re-create the object instead of writing to the deleted hvo (which throws).
		[Test]
		public void Compose_GhostEdit_AfterCancel_ReCreatesInsteadOfWritingToTheDeletedObject()
		{
			ILexEntry bare = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
				bare = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create());

			var composed = FullEntryRegionComposer.Compose(bare, Cache);
			var ghost = composed.Model.Fields.First(f =>
				f.Field == "LexemeForm" && f.StableId.EndsWith("/ghost"));

			Assert.That(composed.EditContext.TrySetText(ghost, ghost.Values[0].WsAbbrev, "casa"), Is.True);
			composed.EditContext.Cancel();
			Assert.That(bare.LexemeFormOA, Is.Null, "cancel rolled the created allomorph back");

			Assert.That(() => composed.EditContext.TrySetText(ghost, ghost.Values[0].WsAbbrev, "gato"),
				Throws.Nothing, "the cached hvo is stale after the rollback; the setter must notice");
			composed.EditContext.Commit();

			Assert.That(bare.LexemeFormOA, Is.Not.Null, "typing again re-created the object");
			Assert.That(bare.LexemeFormOA.Form.get_String(Cache.DefaultVernWs).Text, Is.EqualTo("gato"));
		}

		[Test]
		public void Compose_GhostSenses_CreateASense_WithTheTypedGloss()
		{
			ILexEntry bare = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				bare = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				var morph = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				bare.LexemeFormOA = morph;
				morph.Form.set_String(Cache.DefaultVernWs, TsStringUtils.MakeString("casa", Cache.DefaultVernWs));
			});

			var composed = FullEntryRegionComposer.Compose(bare, Cache, showHiddenFields: true);
			var ghost = composed.Model.Fields.FirstOrDefault(f =>
				f.Field == "Senses" && f.StableId.EndsWith("/ghost"));
			Assert.That(ghost, Is.Not.Null, "an entry without senses renders the add-a-sense ghost");
			Assert.That(ghost.IsEditable, Is.True);

			Assert.That(composed.EditContext.TrySetText(ghost, ghost.Values[0].WsAbbrev, "house"), Is.True);
			composed.EditContext.Commit();

			Assert.That(bare.SensesOS.Count, Is.EqualTo(1), "typing created the sense");
			Assert.That(bare.SensesOS[0].Gloss.get_String(Cache.DefaultAnalWs).Text, Is.EqualTo("house"),
				"the typed text became the gloss (the LexSense ghost default)");
		}

		// B2 (xml-retirement-blockers) — ghost metadata generality: the shipped lexeme-form ghost
		// (LexEntryParts.xml LexEntry-Detail-LexemeForm) carries an explicit ghostClass
		// ("MoStemAllomorph", differing from the abstract MoForm field signature) AND
		// ghostInitMethod="SetMorphTypeToRoot". The composer must create the configured class and
		// invoke the init hook by reflection after the typed text lands, inside the same session —
		// exactly GhostStringSliceView.MakeRealObject (GhostStringSlice.cs:279-329).
		[Test]
		public void Compose_GhostLexemeForm_HonorsGhostClass_AndRunsGhostInitMethod()
		{
			ILexEntry bare = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
				bare = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create());

			var composed = FullEntryRegionComposer.Compose(bare, Cache);
			var ghost = composed.Model.Fields.First(f =>
				f.Field == "LexemeForm" && f.StableId.EndsWith("/ghost"));
			Assert.That(composed.EditContext.TrySetText(ghost, ghost.Values[0].WsAbbrev, "casa"), Is.True);
			composed.EditContext.Commit();

			Assert.That(bare.LexemeFormOA, Is.InstanceOf<IMoStemAllomorph>(),
				"ghostClass=MoStemAllomorph picks the concrete class for the abstract MoForm signature");
			var morphType = ((IMoStemAllomorph)bare.LexemeFormOA).MorphTypeRA;
			Assert.That(morphType, Is.Not.Null,
				"ghostInitMethod=SetMorphTypeToRoot must run after creation (B2; was dropped by 14.1)");
			Assert.That(morphType.Guid, Is.EqualTo(MoMorphTypeTags.kguidMorphRoot),
				"SetMorphTypeToRoot assigns the root morph type, like legacy MakeRealObject's reflection hook");

			Cache.ActionHandlerAccessor.Undo();
			Assert.That(bare.LexemeFormOA, Is.Null,
				"creation + text + init method are ONE step on the global undo stack, like the legacy UOW");
		}

		// B2 — the Translations ghost (LexExampleSentence-Detail-TranslationsAllA): no ghostClass
		// (the concrete CmTranslation comes from the field signature), ghostWs="analysis", and
		// ghostInitMethod="SetTypeToFreeTrans" must type the new translation as Free Translation.
		[Test]
		public void Compose_GhostTranslation_CreatesACmTranslation_TypedFreeByGhostInitMethod()
		{
			ILexExampleSentence example = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				example = Cache.ServiceLocator.GetInstance<ILexExampleSentenceFactory>().Create();
				m_entry.SensesOS[0].ExamplesOS.Add(example);
				example.Example.set_String(Cache.DefaultVernWs,
					TsStringUtils.MakeString("una casa", Cache.DefaultVernWs));
			});

			var composed = FullEntryRegionComposer.Compose(m_entry, Cache);
			var ghost = composed.Model.Fields.FirstOrDefault(f =>
				f.Field == "Translations" && f.StableId.EndsWith("/ghost") && f.ObjectHvo == example.Hvo);
			Assert.That(ghost, Is.Not.Null,
				"an example without translations renders the translation ghost row");
			Assert.That(composed.EditContext.TrySetText(ghost, ghost.Values[0].WsAbbrev, "a house"), Is.True);
			composed.EditContext.Commit();

			Assert.That(example.TranslationsOC.Count, Is.EqualTo(1),
				"typing created the CmTranslation (class from the field signature, no ghostClass)");
			var translation = example.TranslationsOC.First();
			Assert.That(translation.Translation.get_String(Cache.DefaultAnalWs).Text, Is.EqualTo("a house"),
				"ghostWs=analysis routes the text into the analysis alternative");
			Assert.That(translation.TypeRA, Is.Not.Null, "ghostInitMethod=SetTypeToFreeTrans ran");
			Assert.That(translation.TypeRA.Guid, Is.EqualTo(CmPossibilityTags.kguidTranFreeTranslation),
				"the new translation is typed Free Translation, like legacy");
		}

		// Review task 3: a ghost prompt whose layout authors NO ghost field (and no init method)
		// must compose NON-editable — before this fix, typing into it called MakeNewObject and
		// silently DISCARDED the typed text (nothing existed to receive the string). The shipped
		// AlternateForms seq (LexEntryParts.xml:119, no ghost= attribute) is exactly that case.
		[Test]
		public void Compose_GhostWithoutGhostField_IsNotEditable_SoTypingCannotCreateAndDiscard()
		{
			var composed = FullEntryRegionComposer.Compose(m_entry, Cache);
			var ghost = composed.Model.Fields.FirstOrDefault(f =>
				f.Field == "AlternateForms" && f.StableId.EndsWith("/ghost"));
			Assert.That(ghost, Is.Not.Null,
				"the empty always-visible alternate-forms sequence still renders its prompt row");
			Assert.That(ghost.IsEditable, Is.False,
				"no ghost field anywhere to route the text: the prompt must not accept typing");

			var before = m_entry.AlternateFormsOS.Count;
			Assert.That(composed.EditContext.TrySetText(ghost, ghost.Values[0].WsAbbrev, "lost"),
				Is.False, "no setter is registered for the non-editable prompt");
			Assert.That(composed.EditContext.IsOpen, Is.False, "nothing staged, no session opened");
			Assert.That(m_entry.AlternateFormsOS.Count, Is.EqualTo(before),
				"no bare object was created behind the user's back");
		}

		// B3 (xml-retirement-blockers) — conditional display: the real shipped variant/complex-form
		// divergence. LexEntryRef/Normal's VariantEntryTypes and ComplexEntryTypes parts are
		// <if field="RefType" intequals="0|1"> twins (LexEntryParts.xml:1133-1162); exactly one may
		// compose per record state. Before B3 both were dropped (conditional-dropped).
		[Test]
		public void Compose_EntryRefConditionals_VariantAndComplexForm_ComposeDifferently()
		{
			ILexEntryRef entryRef = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				entryRef = Cache.ServiceLocator.GetInstance<ILexEntryRefFactory>().Create();
				m_entry.EntryRefsOS.Add(entryRef);
				entryRef.RefType = LexEntryRefTags.krtVariant; // RefType == 0
			});

			var variantFields = FullEntryRegionComposer.Compose(m_entry, Cache).Model.Fields;
			Assert.That(variantFields.Any(f => f.Field == "VariantEntryTypes"), Is.True,
				"a variant ref (RefType=0) composes the Variant Type slice");
			Assert.That(variantFields.Any(f => f.Field == "ComplexEntryTypes"), Is.False,
				"the complex-form twin's intequals=1 condition fails for a variant ref");

			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
				entryRef.RefType = LexEntryRefTags.krtComplexForm); // RefType == 1

			var complexFields = FullEntryRegionComposer.Compose(m_entry, Cache).Model.Fields;
			Assert.That(complexFields.Any(f => f.Field == "ComplexEntryTypes"), Is.True,
				"a complex-form ref (RefType=1) composes the Complex Form Type slice");
			Assert.That(complexFields.Any(f => f.Field == "VariantEntryTypes"), Is.False,
				"the variant twin's intequals=0 condition fails for a complex-form ref");
		}

		// B3 — lengthatleast: LexEntry-Detail-ShowMinorEntry wraps the PublishAsMinorEntry checkbox
		// in <if field="EntryRefs" lengthatleast="1"> — main entries (no refs) must not show it.
		[Test]
		public void Compose_ShowMinorEntry_OnlyWhenTheEntryHasEntryRefs()
		{
			var before = FullEntryRegionComposer.Compose(m_entry, Cache).Model.Fields;
			Assert.That(before.Any(f => f.Field == "PublishAsMinorEntry"), Is.False,
				"a main entry without EntryRefs hides Show Minor Entry, like legacy");

			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var entryRef = Cache.ServiceLocator.GetInstance<ILexEntryRefFactory>().Create();
				m_entry.EntryRefsOS.Add(entryRef);
				entryRef.RefType = LexEntryRefTags.krtVariant;
			});

			var after = FullEntryRegionComposer.Compose(m_entry, Cache).Model.Fields;
			var row = after.FirstOrDefault(f => f.Field == "PublishAsMinorEntry");
			Assert.That(row, Is.Not.Null, "with an EntryRef the lengthatleast=1 condition passes");
			Assert.That(row.Kind, Is.EqualTo(RegionFieldKind.Boolean), "it renders as the legacy checkbox");
		}

		// B3 — <choice>/<where guidequals>: MoAffixAllomorph-Detail-AsPosition shows the infix
		// position slice only for infix (and infixing-interfix) morph types; no branch passes for a
		// prefix, so nothing renders (the shipped choice has no otherwise).
		[Test]
		public void Compose_InfixPosition_ChoiceWhereGuidEquals_FollowsTheMorphType()
		{
			ILexEntry affixEntry = null;
			IMoAffixAllomorph allomorph = null;
			var morphTypes = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				affixEntry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				allomorph = Cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>().Create();
				affixEntry.LexemeFormOA = allomorph;
				allomorph.Form.set_String(Cache.DefaultVernWs,
					TsStringUtils.MakeString("infixo", Cache.DefaultVernWs));
				allomorph.MorphTypeRA = morphTypes.GetObject(MoMorphTypeTags.kguidMorphInfix);
			});

			var infixFields = FullEntryRegionComposer.Compose(affixEntry, Cache).Model.Fields;
			Assert.That(infixFields.Any(f => f.Field == "Position"), Is.True,
				"an infix allomorph composes the Infix Positions slice (guidequals where passes)");

			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
				allomorph.MorphTypeRA = morphTypes.GetObject(MoMorphTypeTags.kguidMorphPrefix));

			var prefixFields = FullEntryRegionComposer.Compose(affixEntry, Cache).Model.Fields;
			Assert.That(prefixFields.Any(f => f.Field == "Position"), Is.False,
				"no where clause passes for a prefix, and the shipped choice has no otherwise");
		}

		// B3 — target="owner" + lengthatleast: MoAffixAllomorph-Detail-MsEnvFeaturesForLexemeForm is
		// <if target="owner" field="MorphoSyntaxAnalyses" lengthatleast="1"> — the test reads the
		// ENTRY's MSA count from the allomorph's row, so data on the allomorph alone must not show it.
		[Test]
		public void Compose_MsEnvFeatures_TargetOwnerCondition_ReadsTheOwningEntry()
		{
			ILexEntry affixEntry = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				affixEntry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				var allomorph = Cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>().Create();
				affixEntry.LexemeFormOA = allomorph;
				allomorph.Form.set_String(Cache.DefaultVernWs,
					TsStringUtils.MakeString("supfix", Cache.DefaultVernWs));
				allomorph.MorphTypeRA = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>()
					.GetObject(MoMorphTypeTags.kguidMorphSuffix);
				// Data in the conditioned field itself — visible only when the CONDITION passes.
				allomorph.MsEnvFeaturesOA = Cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();
			});

			var withoutMsa = FullEntryRegionComposer.Compose(affixEntry, Cache).Model.Fields;
			Assert.That(withoutMsa.Any(f => f.Field == "MsEnvFeatures"), Is.False,
				"no MSA on the owning entry: the target=owner lengthatleast=1 condition fails "
				+ "even though the field itself has data");

			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
				affixEntry.MorphoSyntaxAnalysesOC.Add(
					Cache.ServiceLocator.GetInstance<IMoUnclassifiedAffixMsaFactory>().Create()));

			var withMsa = FullEntryRegionComposer.Compose(affixEntry, Cache).Model.Fields;
			Assert.That(withMsa.Any(f => f.Field == "MsEnvFeatures"), Is.True,
				"with an MSA the owner-hop condition passes and the Required Features row composes");
		}

		// Section 13.2 — composed rows carry the legacy menu bindings from the live shipped layouts
		// and the hvo of the object that owns them, so the host can show the same xCore menu and
		// point command routing at the right object.
		[Test]
		public void Compose_ThreadsLegacyMenuBindings_AndOwningObjectHvos()
		{
			var fields = FullEntryRegionComposer.Compose(m_entry, Cache).Model.Fields;

			var citation = fields.First(f => f.Field == "CitationForm" && f.Kind == RegionFieldKind.Text);
			Assert.That(citation.MenuId, Is.EqualTo("mnuDataTree-Help"),
				"the slice menu from LexEntryParts.xml CitationFormAllV rides the composed row");
			Assert.That(citation.ContextMenuId, Is.EqualTo("mnuDataTree-CitationFormContext"),
				"the in-string contextMenu binding rides the composed row");
			Assert.That(citation.ObjectHvo, Is.EqualTo(m_entry.Hvo),
				"entry-level rows bind the entry");

			var gloss = fields.First(f => f.Field == "Gloss" && f.Kind == RegionFieldKind.Text);
			Assert.That(gloss.ObjectHvo, Is.EqualTo(m_entry.SensesOS[0].Hvo),
				"sense rows bind their own sense so commands (Delete Sense, etc.) target it");

			Assert.That(fields.Count(f => !string.IsNullOrEmpty(f.MenuId)), Is.GreaterThan(3),
				"menu bindings are pervasive in the shipped layouts, not a one-off");
		}

		// 15.3 — sense item headers inherit the sense layout's root binding (LexSense.fwlayout's
		// HeavySummary part ref carries menu="mnuDataTree-Sense"); the Senses sequence node itself
		// has no menu attribute, so without inheritance right-click could never offer Insert Sense.
		[Test]
		public void Compose_SenseHeaders_BindTheSenseMenu_WithInsertSenseDefined()
		{
			var fields = FullEntryRegionComposer.Compose(m_entry, Cache).Model.Fields;
			var senseHeader = fields.First(f => f.Kind == RegionFieldKind.Header
				&& f.Field == "Senses" && f.ObjectHvo == m_entry.SensesOS[0].Hvo);

			Assert.That(senseHeader.MenuId, Is.EqualTo("mnuDataTree-Sense"),
				"the per-sense header carries the legacy sense slice menu");
			Assert.That(senseHeader.HotlinksId, Is.EqualTo("mnuDataTree-Sense-Hotlinks"));

			// And the shipped definition of that menu offers the add-sense commands, in order.
			var includePath = System.IO.Path.Combine(
				SIL.FieldWorks.Common.FwUtils.FwDirectoryFinder.GetCodeSubDirectory(
					@"Language Explorer\Configuration\Lexicon"),
				"DataTreeInclude.xml");
			var menu = System.Xml.Linq.XDocument.Load(includePath).Descendants("menu")
				.First(m => (string)m.Attribute("id") == "mnuDataTree-Sense");
			var commands = menu.Elements("item")
				.Select(i => (string)i.Attribute("command"))
				.Where(c => !string.IsNullOrEmpty(c))
				.ToList();
			Assert.That(commands, Does.Contain("CmdDataTree-Insert-SenseBelow"),
				"Insert Sense is reachable from the sense header right-click");
			Assert.That(commands, Does.Contain("CmdDataTree-Insert-SubSense"));
			Assert.That(commands, Does.Contain("CmdDataTree-Delete-Sense"));
		}

		// Section 13.6 — every menu id the composer emits (plus the ids the host always appends,
		// matching legacy DTMenuHandler.ShowSliceContextMenu) must resolve to a <menu> definition
		// in the shipped window configuration, so XWindow.ShowContextMenu can materialize it.
		[Test]
		public void Compose_EveryMenuBinding_ResolvesInTheShippedWindowConfiguration()
		{
			var fields = FullEntryRegionComposer.Compose(m_entry, Cache, showHiddenFields: true).Model.Fields;
			var composedIds = fields
				.SelectMany(f => new[] { f.MenuId, f.ContextMenuId, f.HotlinksId })
				.Where(id => !string.IsNullOrEmpty(id))
				.Concat(new[] { "mnuDataTree-Object", "mnuDataTree-MultiStringSlice" })
				.Distinct()
				.ToList();
			Assert.That(composedIds.Count, Is.GreaterThan(2), "the composed entry carries menu bindings");

			var configurationDir = SIL.FieldWorks.Common.FwUtils.FwDirectoryFinder
				.GetCodeSubDirectory(@"Language Explorer\Configuration");
			var definedIds = System.IO.Directory
				.GetFiles(configurationDir, "*.xml", System.IO.SearchOption.AllDirectories)
				.SelectMany(file =>
				{
					try { return System.Xml.Linq.XDocument.Load(file).Descendants("menu"); }
					catch (System.Xml.XmlException) { return Enumerable.Empty<System.Xml.Linq.XElement>(); }
				})
				.Select(menu => (string)menu.Attribute("id"))
				.Where(id => !string.IsNullOrEmpty(id))
				.ToHashSet();

			Assert.That(composedIds.Where(id => !definedIds.Contains(id)), Is.Empty,
				"every composed menu id must be materializable by XWindow.ShowContextMenu");
		}
	}

	/// <summary>
	/// B1 (xml-retirement-blockers, task 9.5) — custom fields must not vanish from the composed
	/// view: the `&lt;part customFields="here"/&gt;` placeholder expands from live MDC metadata the
	/// way legacy DataTree.EnsureCustomFields injects a generated `&lt;part ref="Custom"/&gt;` per
	/// custom field of the object's class (and base classes) and SliceFactory.MakeAutoCustomSlice
	/// realizes the editor by CellarPropertyType with the field's Userlabel. The generated part
	/// carries no visibility attribute, so custom fields are visibility=always in legacy — they
	/// show even when empty, with or without "show hidden fields" (DataTree.cs:2435).
	/// </summary>
	[TestFixture]
	public class FullEntryRegionComposerCustomFieldTests : MemoryOnlyBackendProviderTestBase
	{
		private ILexEntry m_entry;
		private ILexExampleSentence m_example;
		private ILexExampleSentence m_noteExample;
		private IMoForm m_alternateForm;
		private ILexPronunciation m_pronunciation;
		private ILexEtymology m_etymology;
		private ILexEntryRef m_entryRef;
		private ILexExtendedNote m_extendedNote;
		private ICmPicture m_picture;
		private ICmTranslation m_translation;
		private ICmTranslation m_noteTranslation;
		private GenDate m_genDate;
		private GenDate m_moFormGenDate;
		private GenDate m_exampleGenDate;
		private IMoMorphType m_listItem;
		private IMoMorphType m_secondListItem;
		private bool m_fieldsCreated;
		private int m_flidEntryMulti;
		private int m_flidEntrySingle;
		private int m_flidEntryDate;
		private int m_flidEntryListRef;
		private int m_flidEntryNumber;
		private int m_flidMoFormSingle;
		private int m_flidMoFormDate;
		private int m_flidMoFormListRef;
		private int m_flidMoFormListVector;
		private int m_flidExampleSingle;
		private int m_flidExampleDate;
		private int m_flidExampleListRef;
		private int m_flidExampleListVector;
		private int m_flidPronunciationSingle;
		private int m_flidEtymologySingle;
		private int m_flidEntryRefSingle;
		private int m_flidExtendedNoteSingle;
		private int m_flidTranslationSingle;
		private int m_flidPictureSingle;
		private int m_flidSenseSingle;

		public override void TestSetup()
		{
			base.TestSetup();
			EnsureCustomFields();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				m_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				var morph = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				m_entry.LexemeFormOA = morph;
				morph.Form.set_String(Cache.DefaultVernWs, TsStringUtils.MakeString("casa", Cache.DefaultVernWs));
				var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				m_entry.SensesOS.Add(sense);
				sense.Gloss.set_String(Cache.DefaultAnalWs, TsStringUtils.MakeString("house", Cache.DefaultAnalWs));

				m_example = Cache.ServiceLocator.GetInstance<ILexExampleSentenceFactory>().Create();
				sense.ExamplesOS.Add(m_example);
				m_example.Example.set_String(Cache.DefaultVernWs,
					TsStringUtils.MakeString("una casa", Cache.DefaultVernWs));
				var translationType = Cache.ServiceLocator.GetInstance<ICmPossibilityRepository>()
					.GetObject(CmPossibilityTags.kguidTranFreeTranslation);
				m_translation = Cache.ServiceLocator.GetInstance<ICmTranslationFactory>()
					.Create(m_example, translationType);
				m_translation.Translation.set_String(Cache.DefaultAnalWs,
					TsStringUtils.MakeString("a house", Cache.DefaultAnalWs));
				m_example.TranslationsOC.Add(m_translation);

				m_alternateForm = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				m_entry.AlternateFormsOS.Add(m_alternateForm);
				m_alternateForm.Form.set_String(Cache.DefaultVernWs,
					TsStringUtils.MakeString("casita", Cache.DefaultVernWs));

				m_pronunciation = Cache.ServiceLocator.GetInstance<ILexPronunciationFactory>().Create();
				m_entry.PronunciationsOS.Add(m_pronunciation);
				m_pronunciation.Form.set_String(Cache.DefaultPronunciationWs,
					TsStringUtils.MakeString("kasa", Cache.DefaultPronunciationWs));

				m_etymology = Cache.ServiceLocator.GetInstance<ILexEtymologyFactory>().Create();
				m_entry.EtymologyOS.Add(m_etymology);
				m_etymology.Gloss.set_String(Cache.DefaultAnalWs,
					TsStringUtils.MakeString("borrowed", Cache.DefaultAnalWs));

				m_entryRef = Cache.ServiceLocator.GetInstance<ILexEntryRefFactory>().Create();
				m_entry.EntryRefsOS.Add(m_entryRef);

				m_extendedNote = Cache.ServiceLocator.GetInstance<ILexExtendedNoteFactory>().Create();
				sense.ExtendedNoteOS.Add(m_extendedNote);
				m_extendedNote.ExtendedNoteTypeRA = CreateExtendedNoteType("Usage");
				m_extendedNote.Discussion.set_String(Cache.DefaultAnalWs,
					TsStringUtils.MakeString("note discussion", Cache.DefaultAnalWs));
				m_noteExample = Cache.ServiceLocator.GetInstance<ILexExampleSentenceFactory>().Create();
				m_extendedNote.ExamplesOS.Add(m_noteExample);
				m_noteExample.Example.set_String(Cache.DefaultVernWs,
					TsStringUtils.MakeString("otra casa", Cache.DefaultVernWs));
				m_noteTranslation = Cache.ServiceLocator.GetInstance<ICmTranslationFactory>()
					.Create(m_noteExample, translationType);
				m_noteTranslation.Translation.set_String(Cache.DefaultAnalWs,
					TsStringUtils.MakeString("another house", Cache.DefaultAnalWs));
				m_noteExample.TranslationsOC.Add(m_noteTranslation);

				m_picture = Cache.ServiceLocator.GetInstance<ICmPictureFactory>().Create();
				sense.PicturesOS.Add(m_picture);
				m_picture.Caption.set_String(Cache.DefaultAnalWs,
					TsStringUtils.MakeString("picture caption", Cache.DefaultAnalWs));

				var sda = Cache.DomainDataByFlid;
				sda.SetMultiStringAlt(m_entry.Hvo, m_flidEntryMulti, Cache.DefaultAnalWs,
					TsStringUtils.MakeString("high-low", Cache.DefaultAnalWs));
				sda.SetMultiStringAlt(m_entry.Hvo, m_flidEntryMulti, Cache.DefaultVernWs,
					TsStringUtils.MakeString("alto-bajo", Cache.DefaultVernWs));
				sda.SetString(m_entry.Hvo, m_flidEntrySingle,
					TsStringUtils.MakeString("from Smith", Cache.DefaultAnalWs));
				m_genDate = new GenDate(GenDate.PrecisionType.Approximate, 3, 14, 2020, true);
				((ISilDataAccessManaged)sda).SetGenDate(m_entry.Hvo, m_flidEntryDate, m_genDate);
				m_listItem = Cache.LangProject.LexDbOA.MorphTypesOA.ReallyReallyAllPossibilities
					.OfType<IMoMorphType>().First();
				m_secondListItem = Cache.LangProject.LexDbOA.MorphTypesOA.ReallyReallyAllPossibilities
					.OfType<IMoMorphType>().Skip(1).First();
				sda.SetObjProp(m_entry.Hvo, m_flidEntryListRef, m_listItem.Hvo);
				sda.SetInt(m_entry.Hvo, m_flidEntryNumber, 42);
				sda.SetString(sense.Hvo, m_flidSenseSingle,
					TsStringUtils.MakeString("sense note", Cache.DefaultAnalWs));
				sda.SetString(m_entry.LexemeFormOA.Hvo, m_flidMoFormSingle,
					TsStringUtils.MakeString("root allomorph note", Cache.DefaultVernWs));
				m_moFormGenDate = new GenDate(GenDate.PrecisionType.Approximate, 1, 2, 2018, true);
				((ISilDataAccessManaged)sda).SetGenDate(m_entry.LexemeFormOA.Hvo, m_flidMoFormDate,
					m_moFormGenDate);
				sda.SetObjProp(m_entry.LexemeFormOA.Hvo, m_flidMoFormListRef, m_listItem.Hvo);
				sda.Replace(m_entry.LexemeFormOA.Hvo, m_flidMoFormListVector, 0, 0,
					new[] { m_listItem.Hvo, m_secondListItem.Hvo }, 2);
				sda.SetString(m_alternateForm.Hvo, m_flidMoFormSingle,
					TsStringUtils.MakeString("alternate allomorph note", Cache.DefaultVernWs));
				((ISilDataAccessManaged)sda).SetGenDate(m_alternateForm.Hvo, m_flidMoFormDate,
					m_moFormGenDate);
				sda.SetObjProp(m_alternateForm.Hvo, m_flidMoFormListRef, m_secondListItem.Hvo);
				sda.Replace(m_alternateForm.Hvo, m_flidMoFormListVector, 0, 0,
					new[] { m_secondListItem.Hvo }, 1);
				sda.SetString(m_example.Hvo, m_flidExampleSingle,
					TsStringUtils.MakeString("example note", Cache.DefaultAnalWs));
				m_exampleGenDate = new GenDate(GenDate.PrecisionType.Approximate, 6, 7, 2016, true);
				((ISilDataAccessManaged)sda).SetGenDate(m_example.Hvo, m_flidExampleDate,
					m_exampleGenDate);
				sda.SetObjProp(m_example.Hvo, m_flidExampleListRef, m_listItem.Hvo);
				sda.Replace(m_example.Hvo, m_flidExampleListVector, 0, 0,
					new[] { m_listItem.Hvo, m_secondListItem.Hvo }, 2);
				sda.SetString(m_noteExample.Hvo, m_flidExampleSingle,
					TsStringUtils.MakeString("extended note example", Cache.DefaultAnalWs));
				sda.SetString(m_pronunciation.Hvo, m_flidPronunciationSingle,
					TsStringUtils.MakeString("pron note", Cache.DefaultAnalWs));
				sda.SetString(m_etymology.Hvo, m_flidEtymologySingle,
					TsStringUtils.MakeString("etymology note", Cache.DefaultAnalWs));
				sda.SetString(m_entryRef.Hvo, m_flidEntryRefSingle,
					TsStringUtils.MakeString("entry ref note", Cache.DefaultAnalWs));
				sda.SetString(m_extendedNote.Hvo, m_flidExtendedNoteSingle,
					TsStringUtils.MakeString("extended note custom", Cache.DefaultAnalWs));
				sda.SetString(m_translation.Hvo, m_flidTranslationSingle,
					TsStringUtils.MakeString("translation custom", Cache.DefaultAnalWs));
				sda.SetString(m_noteTranslation.Hvo, m_flidTranslationSingle,
					TsStringUtils.MakeString("note translation custom", Cache.DefaultAnalWs));
				sda.SetString(m_picture.Hvo, m_flidPictureSingle,
					TsStringUtils.MakeString("picture note", Cache.DefaultAnalWs));
			});
		}

		// The fixture cache is shared across tests (MemoryOnlyBackendProviderTestBase), so the
		// custom fields are created once — re-running UpdateCustomField per test would mint
		// duplicate fields. Created exactly the way AddCustomFieldDlg/legacy tests do.
		private void EnsureCustomFields()
		{
			if (m_fieldsCreated)
				return;
			m_fieldsCreated = true;
			m_flidEntryMulti = MakeCustomField("Tone Pattern", LexEntryTags.kClassId,
				CellarPropertyType.MultiUnicode, WritingSystemServices.kwsAnalVerns);
			m_flidEntrySingle = MakeCustomField("Source Note", LexEntryTags.kClassId,
				CellarPropertyType.String, WritingSystemServices.kwsAnal);
			m_flidEntryDate = MakeCustomField("Date Collected", LexEntryTags.kClassId,
				CellarPropertyType.GenDate, 0);
			m_flidEntryListRef = MakeCustomField("Field Category", LexEntryTags.kClassId,
				CellarPropertyType.ReferenceAtomic, 0, CmPossibilityTags.kClassId,
				Cache.LangProject.LexDbOA.MorphTypesOA.Guid);
			m_flidEntryNumber = MakeCustomField("Frequency Count", LexEntryTags.kClassId,
				CellarPropertyType.Integer, 0);
			m_flidMoFormSingle = MakeCustomField("Allomorph Note", MoFormTags.kClassId,
				CellarPropertyType.String, WritingSystemServices.kwsVern);
			m_flidMoFormDate = MakeCustomField("Allomorph Date", MoFormTags.kClassId,
				CellarPropertyType.GenDate, 0);
			m_flidMoFormListRef = MakeCustomField("Allomorph Category", MoFormTags.kClassId,
				CellarPropertyType.ReferenceAtomic, 0, CmPossibilityTags.kClassId,
				Cache.LangProject.LexDbOA.MorphTypesOA.Guid);
			m_flidMoFormListVector = MakeCustomField("Allomorph Tags", MoFormTags.kClassId,
				CellarPropertyType.ReferenceSequence, 0, CmPossibilityTags.kClassId,
				Cache.LangProject.LexDbOA.MorphTypesOA.Guid);
			m_flidExampleSingle = MakeCustomField("Example Note", LexExampleSentenceTags.kClassId,
				CellarPropertyType.String, WritingSystemServices.kwsAnal);
			m_flidExampleDate = MakeCustomField("Example Date", LexExampleSentenceTags.kClassId,
				CellarPropertyType.GenDate, 0);
			m_flidExampleListRef = MakeCustomField("Example Category", LexExampleSentenceTags.kClassId,
				CellarPropertyType.ReferenceAtomic, 0, CmPossibilityTags.kClassId,
				Cache.LangProject.LexDbOA.MorphTypesOA.Guid);
			m_flidExampleListVector = MakeCustomField("Example Tags", LexExampleSentenceTags.kClassId,
				CellarPropertyType.ReferenceSequence, 0, CmPossibilityTags.kClassId,
				Cache.LangProject.LexDbOA.MorphTypesOA.Guid);
			m_flidPronunciationSingle = MakeCustomField("Pronunciation Note", LexPronunciationTags.kClassId,
				CellarPropertyType.String, WritingSystemServices.kwsAnal);
			m_flidEtymologySingle = MakeCustomField("Etymology Note", LexEtymologyTags.kClassId,
				CellarPropertyType.String, WritingSystemServices.kwsAnal);
			m_flidEntryRefSingle = MakeCustomField("Entry Ref Note", LexEntryRefTags.kClassId,
				CellarPropertyType.String, WritingSystemServices.kwsAnal);
			m_flidExtendedNoteSingle = MakeCustomField("Extended Note Custom", LexExtendedNoteTags.kClassId,
				CellarPropertyType.String, WritingSystemServices.kwsAnal);
			m_flidTranslationSingle = MakeCustomField("Translation Note", CmTranslationTags.kClassId,
				CellarPropertyType.String, WritingSystemServices.kwsAnal);
			m_flidPictureSingle = MakeCustomField("Picture Note", CmPictureTags.kClassId,
				CellarPropertyType.String, WritingSystemServices.kwsAnal);
			m_flidSenseSingle = MakeCustomField("Sense Source", LexSenseTags.kClassId,
				CellarPropertyType.String, WritingSystemServices.kwsAnal);
		}

		private ICmPossibility CreateExtendedNoteType(string name)
		{
			if (Cache.LangProject.LexDbOA.ExtendedNoteTypesOA == null)
				Cache.LangProject.LexDbOA.ExtendedNoteTypesOA =
					Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
			var item = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
			Cache.LangProject.LexDbOA.ExtendedNoteTypesOA.PossibilitiesOS.Add(item);
			item.Name.set_String(Cache.DefaultAnalWs, TsStringUtils.MakeString(name, Cache.DefaultAnalWs));
			return item;
		}

		private int MakeCustomField(string userLabel, int classId, CellarPropertyType type,
			int wsSelector, int dstCls = 0, System.Guid listRootId = default(System.Guid))
		{
			var fd = new FieldDescription(Cache)
			{
				Userlabel = userLabel,
				HelpString = string.Empty,
				Class = classId,
				Type = type,
				WsSelector = wsSelector,
				DstCls = dstCls,
				ListRootId = listRootId
			};
			fd.UpdateCustomField();
			return fd.Id;
		}

		private System.Collections.Generic.IReadOnlyList<LexicalEditRegionField> Compose(bool showHidden = false)
			=> FullEntryRegionComposer.Compose(m_entry, Cache, showHidden).Model.Fields;

		[Test]
		public void Compose_CustomFields_ExpandAtThePlaceholder_WithLegacyLabelsAndValues()
		{
			var fields = Compose();

			// Multistring: an editable text row, one value per ws of the field's WsSelector.
			var multi = fields.FirstOrDefault(f => f.Label == "Tone Pattern");
			Assert.That(multi, Is.Not.Null, "the custom multistring expands at the placeholder");
			Assert.That(multi.Kind, Is.EqualTo(RegionFieldKind.Text));
			Assert.That(multi.IsEditable, Is.True);
			Assert.That(multi.ObjectHvo, Is.EqualTo(m_entry.Hvo), "entry-level custom rows bind the entry");
			var expectedWs = FullEntryRegionComposer.ResolveWritingSystems(Cache, "analysis vernacular");
			Assert.That(multi.Values.Count, Is.EqualTo(expectedWs.Count),
				"kwsAnalVerns yields one row per analysis+vernacular ws, like legacy MultiStringSlice");
			Assert.That(multi.Values.Select(v => v.Value), Does.Contain("high-low").And.Contain("alto-bajo"));

			// Single string: one editable row in the selector's default ws.
			var single = fields.FirstOrDefault(f => f.Label == "Source Note");
			Assert.That(single, Is.Not.Null);
			Assert.That(single.IsEditable, Is.True);
			Assert.That(single.Values.Single().Value, Is.EqualTo("from Smith"));

			// GenDate: an editable Date row (parity — legacy GenDateSlice is editable). 19i.1: the value is the
			// canonical YEAR-granular form the qualifier editor round-trips — NOT gen.ToLongString(). m_genDate is
			// March 14, 2020; ToLongString() would interleave the DAY ("14"), which the year-granular editor would
			// mis-read as the year and store on the next commit (silent corruption to year 14). The user sees
			// year/precision/era combos, never this raw string.
			var date = fields.FirstOrDefault(f => f.Label == "Date Collected");
			Assert.That(date, Is.Not.Null);
			Assert.That(date.Kind, Is.EqualTo(RegionFieldKind.Date), "GenDate composes as an editable Date row");
			Assert.That(date.IsEditable, Is.True, "GenDate is editable (matches existing GenDate rows)");
			Assert.That(date.Values.Single().Value, Is.EqualTo("About AD 2020"),
				"GenDate value is the year-granular canonical form, not the month/day long string");
			Assert.That(date.Values.Single().Value, Does.Not.Contain("14"),
				"the day must never leak into the year-granular value (19i.1 corruption guard)");

			// Possibility-list reference: the 6.3 chooser lane makes custom reference rows
			// editable too — options from the field's own list, current selection carried.
			var listRef = fields.FirstOrDefault(f => f.Label == "Field Category");
			Assert.That(listRef, Is.Not.Null);
			Assert.That(listRef.IsEditable, Is.True, "custom possibility references take the 6.3 chooser lane");
			Assert.That(listRef.SelectedOptionKey, Is.EqualTo(m_listItem.Guid.ToString()));
			Assert.That(listRef.Options.Select(o => o.Key), Does.Contain(m_listItem.Guid.ToString()),
				"options come from the custom field's own possibility list");

			// Integer: editable like the existing int rows.
			var number = fields.FirstOrDefault(f => f.Label == "Frequency Count");
			Assert.That(number, Is.Not.Null);
			Assert.That(number.IsEditable, Is.True);
			Assert.That(number.Values.Single().Value, Is.EqualTo("42"));

			// The sense-level custom field rides the sense's own placeholder, bound to the sense.
			var senseField = fields.FirstOrDefault(f => f.Label == "Sense Source");
			Assert.That(senseField, Is.Not.Null, "LexSense custom fields expand inside the sense block");
			Assert.That(senseField.ObjectHvo, Is.EqualTo(m_entry.SensesOS[0].Hvo));
			Assert.That(senseField.Values.Single().Value, Is.EqualTo("sense note"));
			var glossIndex = fields.ToList().FindIndex(f => f.Field == "Gloss" && f.ObjectHvo == m_entry.SensesOS[0].Hvo);
			var senseFieldIndex = fields.ToList().IndexOf(senseField);
			Assert.That(senseFieldIndex, Is.GreaterThan(glossIndex),
				"the sense placeholder sits after the authored sense fields");
		}

		[Test]
		public void Compose_CustomFields_SitAtTheLegacyPlaceholderPosition()
		{
			// The LexEntry placeholder sits after the authored entry fields (CitationForm etc.) and
			// before the trailing DateCreated/DateModified never-fields.
			var fields = Compose(showHidden: true).ToList();
			var citationIndex = fields.FindIndex(f => f.Field == "CitationForm");
			var customIndex = fields.FindIndex(f => f.Label == "Source Note");
			var dateCreatedIndex = fields.FindIndex(f => f.Field == "DateCreated" && f.ObjectHvo == m_entry.Hvo);

			Assert.That(customIndex, Is.GreaterThan(citationIndex),
				"entry custom rows come after the authored entry fields");
			Assert.That(customIndex, Is.LessThan(dateCreatedIndex),
				"entry custom rows come before the trailing never-visibility fields, like the layout's placeholder");

			// Creation order (the MDC enumeration legacy FieldDescription.FieldDescriptors walks).
			var multiIndex = fields.FindIndex(f => f.Label == "Tone Pattern");
			var dateIndex = fields.FindIndex(f => f.Label == "Date Collected");
			Assert.That(multiIndex, Is.LessThan(customIndex), "custom rows keep field-creation order");
			Assert.That(customIndex, Is.LessThan(dateIndex), "custom rows keep field-creation order");
		}

		[Test]
		public void Compose_CustomFields_ExpandInNestedAllomorphAndExampleLayouts()
		{
			ILexEntry affixEntry = null;
			IMoAffixAllomorph affixLexemeForm = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				affixEntry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				affixLexemeForm = Cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>().Create();
				affixEntry.LexemeFormOA = affixLexemeForm;
				affixLexemeForm.Form.set_String(Cache.DefaultVernWs,
					TsStringUtils.MakeString("-ka", Cache.DefaultVernWs));
				Cache.DomainDataByFlid.SetString(affixLexemeForm.Hvo, m_flidMoFormSingle,
					TsStringUtils.MakeString("affix allomorph note", Cache.DefaultVernWs));
			});

			var fields = Compose();
			var rootAllomorph = fields.FirstOrDefault(f => f.Label == "Allomorph Note"
				&& f.ObjectHvo == m_entry.LexemeFormOA.Hvo);
			var alternateAllomorph = fields.FirstOrDefault(f => f.Label == "Allomorph Note"
				&& f.ObjectHvo == m_alternateForm.Hvo);
			var rootAllomorphDate = fields.FirstOrDefault(f => f.Label == "Allomorph Date"
				&& f.ObjectHvo == m_entry.LexemeFormOA.Hvo);
			var rootAllomorphCategory = fields.FirstOrDefault(f => f.Label == "Allomorph Category"
				&& f.ObjectHvo == m_entry.LexemeFormOA.Hvo);
			var rootAllomorphTags = fields.FirstOrDefault(f => f.Label == "Allomorph Tags"
				&& f.ObjectHvo == m_entry.LexemeFormOA.Hvo);
			var exampleField = fields.FirstOrDefault(f => f.Label == "Example Note"
				&& f.ObjectHvo == m_example.Hvo);
			var exampleDate = fields.FirstOrDefault(f => f.Label == "Example Date"
				&& f.ObjectHvo == m_example.Hvo);
			var exampleCategory = fields.FirstOrDefault(f => f.Label == "Example Category"
				&& f.ObjectHvo == m_example.Hvo);
			var exampleTags = fields.FirstOrDefault(f => f.Label == "Example Tags"
				&& f.ObjectHvo == m_example.Hvo);

			Assert.That(rootAllomorph, Is.Not.Null,
				"the lexeme-form layout expands MoForm custom fields inside AsLexemeFormBasic");
			Assert.That(rootAllomorph.Values.Single().Value, Is.EqualTo("root allomorph note"));
			Assert.That(alternateAllomorph, Is.Not.Null,
				"the alternate-form layout expands MoForm custom fields inside the allomorph sequence");
			Assert.That(alternateAllomorph.Values.Single().Value, Is.EqualTo("alternate allomorph note"));
			Assert.That(fields.Count(f => f.Label == "Allomorph Note"), Is.EqualTo(2),
				"one MoForm custom row is emitted per visited allomorph object");
			Assert.That(rootAllomorphDate, Is.Not.Null,
				"nested MoForm GenDate custom fields render in the lexeme-form layout");
			Assert.That(rootAllomorphDate.Kind, Is.EqualTo(RegionFieldKind.Date),
				"GenDate composes as an editable Date row");
			Assert.That(rootAllomorphDate.IsEditable, Is.True, "GenDate is editable (parity with legacy)");
			// 19i.1: canonical year-granular value (m_moFormGenDate is Jan 2, 2018), not the month/day long string.
			Assert.That(rootAllomorphDate.Values.Single().Value, Is.EqualTo("About AD 2018"));
			Assert.That(rootAllomorphCategory, Is.Not.Null,
				"nested MoForm atomic custom references render as chooser rows");
			Assert.That(rootAllomorphCategory.Kind, Is.EqualTo(RegionFieldKind.Chooser));
			Assert.That(rootAllomorphCategory.IsEditable, Is.True);
			Assert.That(rootAllomorphCategory.SelectedOptionKey, Is.EqualTo(m_listItem.Guid.ToString()));
			Assert.That(rootAllomorphTags, Is.Not.Null,
				"nested MoForm vector custom references render as reference-vector rows");
			Assert.That(rootAllomorphTags.Kind, Is.EqualTo(RegionFieldKind.ReferenceVector));
			Assert.That(rootAllomorphTags.IsEditable, Is.True);
			Assert.That(rootAllomorphTags.Items.Select(i => i.Key),
				Does.Contain(m_listItem.Guid.ToString()).And.Contain(m_secondListItem.Guid.ToString()));
			Assert.That(exampleField, Is.Not.Null,
				"the example detail layout expands LexExampleSentence custom fields");
			Assert.That(exampleField.Values.Single().Value, Is.EqualTo("example note"));
			Assert.That(exampleDate, Is.Not.Null,
				"example custom GenDate fields render in the nested example layout");
			// 19i.1: canonical year-granular value (m_exampleGenDate is Jun 7, 2016), not the month/day long string.
			Assert.That(exampleDate.Values.Single().Value, Is.EqualTo("About AD 2016"));
			Assert.That(exampleCategory, Is.Not.Null,
				"example custom atomic references render as chooser rows");
			Assert.That(exampleCategory.Kind, Is.EqualTo(RegionFieldKind.Chooser));
			Assert.That(exampleCategory.SelectedOptionKey, Is.EqualTo(m_listItem.Guid.ToString()));
			Assert.That(exampleTags, Is.Not.Null,
				"example custom possibility vectors render as editable reference vectors");
			Assert.That(exampleTags.Kind, Is.EqualTo(RegionFieldKind.ReferenceVector));
			Assert.That(exampleTags.Items.Select(i => i.Key),
				Does.Contain(m_listItem.Guid.ToString()).And.Contain(m_secondListItem.Guid.ToString()));

			var affixFields = FullEntryRegionComposer.Compose(affixEntry, Cache).Model.Fields;
			var affixCustom = affixFields.FirstOrDefault(f => f.Label == "Allomorph Note"
				&& f.ObjectHvo == affixLexemeForm.Hvo);
			Assert.That(affixCustom, Is.Not.Null,
				"the affix lexeme-form layout also expands MoForm custom fields");
			Assert.That(affixCustom.Values.Single().Value, Is.EqualTo("affix allomorph note"));
		}

		[Test]
		public void Compose_CustomFields_ExpandInPreviouslyMissingNestedDetailLayouts()
		{
			var fields = Compose();
			Assert.That(fields.Any(f => f.Label == "Pronunciation Note" && f.ObjectHvo == m_pronunciation.Hvo),
				Is.True, "pronunciation custom fields should render in the pronunciation detail layout");
			Assert.That(fields.Any(f => f.Label == "Etymology Note" && f.ObjectHvo == m_etymology.Hvo),
				Is.True, "etymology custom fields should render in the etymology detail layout");
			Assert.That(fields.Any(f => f.Label == "Entry Ref Note" && f.ObjectHvo == m_entryRef.Hvo),
				Is.True, "entry-reference custom fields should render in the entry-ref detail layout");
			Assert.That(fields.Any(f => f.Label == "Extended Note Custom" && f.ObjectHvo == m_extendedNote.Hvo),
				Is.True, "extended-note custom fields should render in the extended-note detail layout");
			Assert.That(fields.Any(f => f.Label == "Example Note" && f.ObjectHvo == m_noteExample.Hvo),
				Is.True, "the example Notes layout should render LexExampleSentence custom fields");
			Assert.That(fields.Any(f => f.Label == "Translation Note" && f.ObjectHvo == m_translation.Hvo),
				Is.True, "translation custom fields should render in the TranslationAndType layout");
			Assert.That(fields.Any(f => f.Label == "Translation Note" && f.ObjectHvo == m_noteTranslation.Hvo),
				Is.True, "translation custom fields should render in the Translation layout used by note examples");
			Assert.That(fields.Any(f => f.Label == "Picture Note" && f.ObjectHvo == m_picture.Hvo),
				Is.True, "picture custom fields should render in the picture detail layout");
		}

		[Test]
		public void Edit_CustomFields_StageThroughTheFencedSession_AsOneUndoStep()
		{
			var composed = FullEntryRegionComposer.Compose(m_entry, Cache);
			var multi = composed.Model.Fields.First(f => f.Label == "Tone Pattern");
			var single = composed.Model.Fields.First(f => f.Label == "Source Note");

			// Address the analysis alternative by its own WS tag: the row order follows the field's
			// WsSelector resolution (legacy GetWritingSystemList), so Values[0] need not be analysis.
			var analTag = Cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Id;
			Assert.That(multi.Values.Select(v => v.WsTag), Does.Contain(analTag),
				"the multistring row carries the analysis alternative");
			Assert.That(composed.EditContext.TrySetText(multi, analTag, "low-high"), Is.True,
				"custom text rows stage through the same setter registry as authored fields");
			Assert.That(composed.EditContext.TrySetText(single, single.Values[0].WsTag, "from Jones"), Is.True);
			composed.EditContext.Commit();

			var sda = Cache.DomainDataByFlid;
			Assert.That(sda.get_MultiStringAlt(m_entry.Hvo, m_flidEntryMulti, Cache.DefaultAnalWs).Text,
				Is.EqualTo("low-high"));
			Assert.That(sda.get_StringProp(m_entry.Hvo, m_flidEntrySingle).Text, Is.EqualTo("from Jones"));

			Cache.ActionHandlerAccessor.Undo();
			Assert.That(sda.get_MultiStringAlt(m_entry.Hvo, m_flidEntryMulti, Cache.DefaultAnalWs).Text,
				Is.EqualTo("high-low"), "one undo reverts the whole session, both custom edits included");
			Assert.That(sda.get_StringProp(m_entry.Hvo, m_flidEntrySingle).Text, Is.EqualTo("from Smith"));
		}

		[Test]
		public void Edit_CustomFields_OnNestedAllomorphAndExampleLayouts_StageThroughTheFencedSession()
		{
			var composed = FullEntryRegionComposer.Compose(m_entry, Cache);
			var rootAllomorph = composed.Model.Fields.First(f => f.Label == "Allomorph Note"
				&& f.ObjectHvo == m_entry.LexemeFormOA.Hvo);
			var rootAllomorphCategory = composed.Model.Fields.First(f => f.Label == "Allomorph Category"
				&& f.ObjectHvo == m_entry.LexemeFormOA.Hvo);
			var rootAllomorphTags = composed.Model.Fields.First(f => f.Label == "Allomorph Tags"
				&& f.ObjectHvo == m_entry.LexemeFormOA.Hvo);
			var alternateAllomorph = composed.Model.Fields.First(f => f.Label == "Allomorph Note"
				&& f.ObjectHvo == m_alternateForm.Hvo);
			var exampleField = composed.Model.Fields.First(f => f.Label == "Example Note"
				&& f.ObjectHvo == m_example.Hvo);
			var exampleCategory = composed.Model.Fields.First(f => f.Label == "Example Category"
				&& f.ObjectHvo == m_example.Hvo);
			var exampleTags = composed.Model.Fields.First(f => f.Label == "Example Tags"
				&& f.ObjectHvo == m_example.Hvo);
			var sda = Cache.DomainDataByFlid;

			Assert.That(composed.EditContext.TrySetText(rootAllomorph,
				rootAllomorph.Values.Single().WsTag, "root note edited"), Is.True);
			Assert.That(composed.EditContext.TrySetText(alternateAllomorph,
				alternateAllomorph.Values.Single().WsTag, "alternate note edited"), Is.True);
			Assert.That(composed.EditContext.TrySetText(exampleField,
				exampleField.Values.Single().WsTag, "example note edited"), Is.True);
			Assert.That(composed.EditContext.TrySetOption(rootAllomorphCategory,
				m_secondListItem.Guid.ToString()), Is.True);
			Assert.That(composed.EditContext.TryAddReferenceItem(rootAllomorphTags,
				m_secondListItem.Guid.ToString()), Is.False,
				"adding a duplicate nested custom vector item is rejected");
			Assert.That(composed.EditContext.TryRemoveReferenceItem(rootAllomorphTags,
				m_listItem.Guid.ToString()), Is.True);
			Assert.That(composed.EditContext.TrySetOption(exampleCategory,
				m_secondListItem.Guid.ToString()), Is.True);
			Assert.That(composed.EditContext.TryRemoveReferenceItem(exampleTags,
				m_secondListItem.Guid.ToString()), Is.True);
			composed.EditContext.Commit();

			Assert.That(sda.get_StringProp(m_entry.LexemeFormOA.Hvo, m_flidMoFormSingle).Text,
				Is.EqualTo("root note edited"));
			Assert.That(sda.get_ObjectProp(m_entry.LexemeFormOA.Hvo, m_flidMoFormListRef),
				Is.EqualTo(m_secondListItem.Hvo));
			Assert.That(sda.get_VecSize(m_entry.LexemeFormOA.Hvo, m_flidMoFormListVector), Is.EqualTo(1));
			Assert.That(sda.get_VecItem(m_entry.LexemeFormOA.Hvo, m_flidMoFormListVector, 0),
				Is.EqualTo(m_secondListItem.Hvo));
			Assert.That(sda.get_StringProp(m_alternateForm.Hvo, m_flidMoFormSingle).Text,
				Is.EqualTo("alternate note edited"));
			Assert.That(sda.get_StringProp(m_example.Hvo, m_flidExampleSingle).Text,
				Is.EqualTo("example note edited"));
			Assert.That(sda.get_ObjectProp(m_example.Hvo, m_flidExampleListRef),
				Is.EqualTo(m_secondListItem.Hvo));
			Assert.That(sda.get_VecSize(m_example.Hvo, m_flidExampleListVector), Is.EqualTo(1));
			Assert.That(sda.get_VecItem(m_example.Hvo, m_flidExampleListVector, 0),
				Is.EqualTo(m_listItem.Hvo));

			Cache.ActionHandlerAccessor.Undo();
			Assert.That(sda.get_StringProp(m_entry.LexemeFormOA.Hvo, m_flidMoFormSingle).Text,
				Is.EqualTo("root allomorph note"));
			Assert.That(sda.get_ObjectProp(m_entry.LexemeFormOA.Hvo, m_flidMoFormListRef),
				Is.EqualTo(m_listItem.Hvo));
			Assert.That(sda.get_VecSize(m_entry.LexemeFormOA.Hvo, m_flidMoFormListVector), Is.EqualTo(2));
			Assert.That(sda.get_StringProp(m_alternateForm.Hvo, m_flidMoFormSingle).Text,
				Is.EqualTo("alternate allomorph note"));
			Assert.That(sda.get_StringProp(m_example.Hvo, m_flidExampleSingle).Text,
				Is.EqualTo("example note"));
			Assert.That(sda.get_ObjectProp(m_example.Hvo, m_flidExampleListRef),
				Is.EqualTo(m_listItem.Hvo));
			Assert.That(sda.get_VecSize(m_example.Hvo, m_flidExampleListVector), Is.EqualTo(2));
		}

		[Test]
		public void Edit_CustomFields_OnPreviouslyMissingNestedDetailLayouts_StageThroughTheFencedSession()
		{
			var composed = FullEntryRegionComposer.Compose(m_entry, Cache);
			var pron = composed.Model.Fields.First(f => f.Label == "Pronunciation Note" && f.ObjectHvo == m_pronunciation.Hvo);
			var etym = composed.Model.Fields.First(f => f.Label == "Etymology Note" && f.ObjectHvo == m_etymology.Hvo);
			var entryRef = composed.Model.Fields.First(f => f.Label == "Entry Ref Note" && f.ObjectHvo == m_entryRef.Hvo);
			var extNote = composed.Model.Fields.First(f => f.Label == "Extended Note Custom" && f.ObjectHvo == m_extendedNote.Hvo);
			var noteExample = composed.Model.Fields.First(f => f.Label == "Example Note" && f.ObjectHvo == m_noteExample.Hvo);
			var translation = composed.Model.Fields.First(f => f.Label == "Translation Note" && f.ObjectHvo == m_translation.Hvo);
			var noteTranslation = composed.Model.Fields.First(f => f.Label == "Translation Note" && f.ObjectHvo == m_noteTranslation.Hvo);
			var picture = composed.Model.Fields.First(f => f.Label == "Picture Note" && f.ObjectHvo == m_picture.Hvo);
			var sda = Cache.DomainDataByFlid;

			Assert.That(composed.EditContext.TrySetText(pron, pron.Values.Single().WsTag, "pron note edited"), Is.True);
			Assert.That(composed.EditContext.TrySetText(etym, etym.Values.Single().WsTag, "etym note edited"), Is.True);
			Assert.That(composed.EditContext.TrySetText(entryRef, entryRef.Values.Single().WsTag, "entry ref edited"), Is.True);
			Assert.That(composed.EditContext.TrySetText(extNote, extNote.Values.Single().WsTag, "extended note edited"), Is.True);
			Assert.That(composed.EditContext.TrySetText(noteExample, noteExample.Values.Single().WsTag, "note example edited"), Is.True);
			Assert.That(composed.EditContext.TrySetText(translation, translation.Values.Single().WsTag, "translation edited"), Is.True);
			Assert.That(composed.EditContext.TrySetText(noteTranslation, noteTranslation.Values.Single().WsTag, "note translation edited"), Is.True);
			Assert.That(composed.EditContext.TrySetText(picture, picture.Values.Single().WsTag, "picture edited"), Is.True);
			composed.EditContext.Commit();

			Assert.That(sda.get_StringProp(m_pronunciation.Hvo, m_flidPronunciationSingle).Text, Is.EqualTo("pron note edited"));
			Assert.That(sda.get_StringProp(m_etymology.Hvo, m_flidEtymologySingle).Text, Is.EqualTo("etym note edited"));
			Assert.That(sda.get_StringProp(m_entryRef.Hvo, m_flidEntryRefSingle).Text, Is.EqualTo("entry ref edited"));
			Assert.That(sda.get_StringProp(m_extendedNote.Hvo, m_flidExtendedNoteSingle).Text, Is.EqualTo("extended note edited"));
			Assert.That(sda.get_StringProp(m_noteExample.Hvo, m_flidExampleSingle).Text, Is.EqualTo("note example edited"));
			Assert.That(sda.get_StringProp(m_translation.Hvo, m_flidTranslationSingle).Text, Is.EqualTo("translation edited"));
			Assert.That(sda.get_StringProp(m_noteTranslation.Hvo, m_flidTranslationSingle).Text, Is.EqualTo("note translation edited"));
			Assert.That(sda.get_StringProp(m_picture.Hvo, m_flidPictureSingle).Text, Is.EqualTo("picture edited"));

			Cache.ActionHandlerAccessor.Undo();
			Assert.That(sda.get_StringProp(m_pronunciation.Hvo, m_flidPronunciationSingle).Text, Is.EqualTo("pron note"));
			Assert.That(sda.get_StringProp(m_etymology.Hvo, m_flidEtymologySingle).Text, Is.EqualTo("etymology note"));
			Assert.That(sda.get_StringProp(m_entryRef.Hvo, m_flidEntryRefSingle).Text, Is.EqualTo("entry ref note"));
			Assert.That(sda.get_StringProp(m_extendedNote.Hvo, m_flidExtendedNoteSingle).Text, Is.EqualTo("extended note custom"));
			Assert.That(sda.get_StringProp(m_noteExample.Hvo, m_flidExampleSingle).Text, Is.EqualTo("extended note example"));
			Assert.That(sda.get_StringProp(m_translation.Hvo, m_flidTranslationSingle).Text, Is.EqualTo("translation custom"));
			Assert.That(sda.get_StringProp(m_noteTranslation.Hvo, m_flidTranslationSingle).Text, Is.EqualTo("note translation custom"));
			Assert.That(sda.get_StringProp(m_picture.Hvo, m_flidPictureSingle).Text, Is.EqualTo("picture note"));
		}

		// Review task 5: a plain String prop reads the WHOLE string (get_StringProp), so the
		// row's writing system must come from the STRING's own first run when there is content —
		// not from the layout's ws= spec — and write-back must use that same ws (legacy
		// StringSlice renders the string's own run properties). The layout ws only seeds an
		// empty string.
		[Test]
		public void Compose_StringProp_DerivesTheRowWsFromTheStoredString_AndWritesBackSymmetrically()
		{
			var vern = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;
			var sda = Cache.DomainDataByFlid;
			// "Source Note" is a kwsAnal-selector String field, but the user typed vernacular.
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
				sda.SetString(m_entry.Hvo, m_flidEntrySingle,
					TsStringUtils.MakeString("vern note", vern.Handle)));

			var composed = FullEntryRegionComposer.Compose(m_entry, Cache);
			var single = composed.Model.Fields.First(f => f.Label == "Source Note");
			Assert.That(single.Values.Single().WsTag, Is.EqualTo(vern.Id),
				"the row's ws (abbrev/font/RTL identity) follows the stored string's first run");

			Assert.That(composed.EditContext.TrySetText(single, vern.Id, "edited note"), Is.True,
				"the derived ws is also the row's write-back key");
			composed.EditContext.Commit();
			var stored = sda.get_StringProp(m_entry.Hvo, m_flidEntrySingle);
			Assert.That(stored.Text, Is.EqualTo("edited note"));
			Assert.That(TsStringUtils.GetWsOfRun(stored, 0), Is.EqualTo(vern.Handle),
				"write-back keeps the string's own ws — read and write are symmetric");
		}

		[Test]
		public void Compose_EmptyStringProp_FallsBackToTheLayoutWs()
		{
			ILexEntry bare = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				bare = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				var morph = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				bare.LexemeFormOA = morph;
				morph.Form.set_String(Cache.DefaultVernWs, TsStringUtils.MakeString("gato", Cache.DefaultVernWs));
			});

			var composed = FullEntryRegionComposer.Compose(bare, Cache);
			var single = composed.Model.Fields.First(f => f.Label == "Source Note" && f.ObjectHvo == bare.Hvo);
			Assert.That(single.Values.Single().WsTag,
				Is.EqualTo(Cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Id),
				"an empty String prop seeds from the field's own ws selector (kwsAnal)");
		}

		// Review task 7: clearing an int box must be SAFE. Legacy IntegerSlice treats a
		// non-numeric box — including empty — as invalid on focus loss (warn + restore the
		// stored value; Convert.ToInt32("") throws), never committing empty as 0. The composed
		// setter mirrors that: empty/whitespace stages NOTHING, and the last successfully staged
		// value is what a commit applies.
		[Test]
		public void Edit_IntegerField_EmptyAndWhitespace_StageNothing()
		{
			var composed = FullEntryRegionComposer.Compose(m_entry, Cache);
			var number = composed.Model.Fields.First(f => f.Label == "Frequency Count");
			var sda = Cache.DomainDataByFlid;

			Assert.That(composed.EditContext.TrySetText(number, "", ""), Is.False,
				"clearing the box stages nothing (legacy: invalid, restore)");
			Assert.That(composed.EditContext.TrySetText(number, "", "   "), Is.False);
			Assert.That(composed.EditContext.TrySetText(number, "", "not-a-number"), Is.False);
			Assert.That(composed.EditContext.IsOpen, Is.False,
				"rejected values must not leave an empty fence open");
			Assert.That(sda.get_IntProp(m_entry.Hvo, m_flidEntryNumber), Is.EqualTo(42),
				"the stored value is untouched");

			// Clear-then-retype: only the parseable states stage; commit applies the LAST one.
			Assert.That(composed.EditContext.TrySetText(number, "", "5"), Is.True);
			Assert.That(composed.EditContext.TrySetText(number, "", ""), Is.False,
				"a clear after a valid stage still stages nothing");
			Assert.That(composed.EditContext.TrySetText(number, "", "55"), Is.True);
			composed.EditContext.Commit();
			Assert.That(sda.get_IntProp(m_entry.Hvo, m_flidEntryNumber), Is.EqualTo(55));
		}

		[Test]
		public void Compose_EmptyCustomFields_StayVisible_LikeLegacyAlwaysVisibility()
		{
			// Legacy generated custom part refs carry no visibility attribute -> "always": an empty
			// custom field still shows its (blank) row, with or without "show hidden fields".
			ILexEntry bare = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				bare = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				var morph = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				bare.LexemeFormOA = morph;
				morph.Form.set_String(Cache.DefaultVernWs, TsStringUtils.MakeString("gato", Cache.DefaultVernWs));
			});

			foreach (var showHidden in new[] { false, true })
			{
				var fields = FullEntryRegionComposer.Compose(bare, Cache, showHidden).Model.Fields;
				foreach (var label in new[] { "Tone Pattern", "Source Note", "Date Collected", "Field Category" })
				{
					var row = fields.FirstOrDefault(f => f.Label == label);
					Assert.That(row, Is.Not.Null,
						$"empty custom field '{label}' must stay visible (showHidden={showHidden}), like legacy visibility=always");
					Assert.That(row.Values.All(v => string.IsNullOrEmpty(v.Value)), Is.True);
				}
			}
		}

		[Test]
		public void Compose_CustomFields_AreNotDuplicated_AcrossRepeatComposes()
		{
			var first = Compose();
			var second = Compose();
			Assert.That(second.Count(f => f.Label == "Source Note"),
				Is.EqualTo(first.Count(f => f.Label == "Source Note")).And.EqualTo(1),
				"each custom field renders exactly one row per object per compose");
		}
	}

	/// <summary>Task 3.14 — the cross-surface DnD payloads round-trip through OS data objects.</summary>
	[TestFixture]
	public class FwDragDropDataTests : MemoryOnlyBackendProviderTestBase
	{
		[Test]
		public void RecordDrag_RoundTrips_ToTheSameObject()
		{
			ILexEntry entry = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
				entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create());

			var dataObject = FwDragDropData.CreateRecordDataObject(entry);
			Assert.That(FwDragDropData.TryGetRecord(dataObject, Cache, out var resolved), Is.True);
			Assert.That(resolved, Is.SameAs(entry));
			Assert.That(dataObject.GetData(System.Windows.Forms.DataFormats.UnicodeText), Is.Not.Null,
				"external drop targets get a plain-text label");
		}

		[Test]
		public void NonRecordData_DoesNotResolve()
		{
			var dataObject = new System.Windows.Forms.DataObject(System.Windows.Forms.DataFormats.UnicodeText, "just text");
			Assert.That(FwDragDropData.TryGetRecord(dataObject, Cache, out _), Is.False);
		}

		[Test]
		public void TextDrag_CarriesTheSameDualLanePayloadAsTheClipboard()
		{
			var clipboard = new FwTsStringClipboard(Cache.WritingSystemFactory);
			var payload = clipboard.FromTsString(TsStringUtils.MakeString("casa", Cache.DefaultVernWs));

			var dataObject = FwDragDropData.CreateTextDataObject(payload);
			Assert.That(dataObject.GetDataPresent(SIL.FieldWorks.Common.RootSites.TsStringWrapper.TsStringFormat), Is.True,
				"text drags carry the legacy rich lane");
			Assert.That(dataObject.GetData(System.Windows.Forms.DataFormats.UnicodeText), Is.EqualTo("casa"));
		}

		[Test]
		public void TextDrag_FromAvaloniaPayload_LegacyClipboardBridgeReadsBothRichAndNeutralLanes()
		{
			var clipboard = new FwTsStringClipboard(Cache.WritingSystemFactory);
			var richPayload = clipboard.FromTsString(TsStringUtils.MakeString("casa", Cache.DefaultVernWs));
			var dataObject = FwDragDropData.CreateTextDataObject(richPayload);

			ClipboardUtils.SetDataObject(dataObject, true, 3, 50);

			var readBack = clipboard.GetText();
			Assert.That(readBack, Is.Not.Null);
			Assert.That(readBack.RichXml, Is.Not.Null.And.Not.Empty,
				"legacy readers must still see the TsString lane from an Avalonia-originated drag payload");
			Assert.That(readBack.RichText, Is.Not.Null,
				"the same payload must project the neutral rich lane for Avalonia consumers");
			Assert.That(readBack.PlainText, Is.EqualTo("casa"));
		}
	}
}

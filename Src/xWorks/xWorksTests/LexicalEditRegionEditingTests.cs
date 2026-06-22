// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Globalization;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.Seams;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.Cellar;
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

				editing = false;
				controller.NotifyEditCompleted();
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
		private GenDate m_genDate;
		private IMoMorphType m_listItem;
		private bool m_fieldsCreated;
		private int m_flidEntryMulti;
		private int m_flidEntrySingle;
		private int m_flidEntryDate;
		private int m_flidEntryListRef;
		private int m_flidEntryNumber;
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
				sda.SetObjProp(m_entry.Hvo, m_flidEntryListRef, m_listItem.Hvo);
				sda.SetInt(m_entry.Hvo, m_flidEntryNumber, 42);
				sda.SetString(sense.Hvo, m_flidSenseSingle,
					TsStringUtils.MakeString("sense note", Cache.DefaultAnalWs));
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
			m_flidSenseSingle = MakeCustomField("Sense Source", LexSenseTags.kClassId,
				CellarPropertyType.String, WritingSystemServices.kwsAnal);
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

			// GenDate: read-only, formatted exactly like the existing GenDate rows.
			var date = fields.FirstOrDefault(f => f.Label == "Date Collected");
			Assert.That(date, Is.Not.Null);
			Assert.That(date.IsEditable, Is.False, "GenDate stays read-only (matches existing GenDate rows)");
			Assert.That(date.Values.Single().Value, Is.EqualTo(m_genDate.ToLongString()));

			// Possibility-list reference: read-only joined name for now (chooser write-back rides 6.3).
			var listRef = fields.FirstOrDefault(f => f.Label == "Field Category");
			Assert.That(listRef, Is.Not.Null);
			Assert.That(listRef.IsEditable, Is.False, "reference write-back is deferred to the 6.3 chooser lane");
			Assert.That(listRef.Values.Single().Value, Is.EqualTo(m_listItem.ShortName));

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
	}
}

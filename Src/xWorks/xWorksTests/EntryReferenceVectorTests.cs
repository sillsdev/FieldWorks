// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// winforms-free-lexeme-editor.md D3 (wave 3) — entry-reference vectors: the legacy
	/// <c>EntrySequenceReferenceSlice</c> fields (ComponentLexemes/PrimaryLexemes on LexEntryRef,
	/// targeting ILexEntry OR ILexSense) compose as EDITABLE ReferenceVector rows whose items are
	/// headwords and whose ADD is a type-ahead lexicon search (<see cref="LexicalEditRegionField.SearchOptions"/>)
	/// — possibility lists enumerate, lexicons search, so the whole lexicon is never materialized
	/// as Options. Writes ride sda.Replace inside the fenced session, plus the legacy launcher's
	/// ComponentLexemes coupling (first component becomes the primary lexeme; the complex form
	/// shows under new components) which LCModel does NOT apply on add (pinned below).
	/// </summary>
	[TestFixture]
	public class EntryReferenceVectorTests : MemoryOnlyBackendProviderTestBase
	{
		private ILexEntry m_entry;   // "burro" — the composed pane's own entry (a complex form)
		private ILexEntryRef m_ref;  // its complex-form LexEntryRef
		private ILexEntry m_casa;    // component already in the vector
		private ILexEntry m_cantar;  // search candidate (shares the "ca" prefix with casa)
		private ILexEntry m_perro;   // add candidate
		private ILexSense m_perroSense;

		public override void TestSetup()
		{
			base.TestSetup();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				m_entry = MakeEntry("burro");
				m_casa = MakeEntry("casa");
				m_cantar = MakeEntry("cantar");
				m_perro = MakeEntry("perro");
				m_perroSense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				m_perro.SensesOS.Add(m_perroSense);
				m_perroSense.Gloss.set_String(Cache.DefaultAnalWs,
					TsStringUtils.MakeString("dog", Cache.DefaultAnalWs));

				m_ref = Cache.ServiceLocator.GetInstance<ILexEntryRefFactory>().Create();
				m_entry.EntryRefsOS.Add(m_ref);
				m_ref.RefType = LexEntryRefTags.krtComplexForm;
				m_ref.ComponentLexemesRS.Add(m_casa);
			});
		}

		private ILexEntry MakeEntry(string form)
		{
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			var morph = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			entry.LexemeFormOA = morph;
			morph.Form.set_String(Cache.DefaultVernWs, TsStringUtils.MakeString(form, Cache.DefaultVernWs));
			return entry;
		}

		private ComposedEntryRegion Compose() => FullEntryRegionComposer.Compose(m_entry, Cache);

		private static LexicalEditRegionField ComponentsField(ComposedEntryRegion composed)
			=> composed.Model.Fields.Single(f => f.Field == "ComponentLexemes"
				&& f.Kind == RegionFieldKind.ReferenceVector);

		[Test]
		public void Compose_ComponentLexemes_IsEditableReferenceVector_WithHeadwordItems_AndSearchNotOptions()
		{
			var composed = Compose();
			var field = ComponentsField(composed);

			Assert.That(field.IsEditable, Is.True,
				"D3: the entry-reference vector is editable, not an unsupported/read-only row");
			Assert.That(field.Label, Is.EqualTo("Components"),
				"the RefType=complex-form conditional picked the Components slice");
			Assert.That(field.Items.Select(i => i.Key),
				Is.EqualTo(new[] { m_casa.Guid.ToString() }), "current refs ride the row in vector order");
			Assert.That(field.Items.Single().Name, Is.EqualTo(m_casa.HeadWord.Text),
				"items display the headword");
			Assert.That(field.Options, Is.Empty,
				"lexicons SEARCH: the whole lexicon is never materialized as options");
			Assert.That(field.SearchOptions, Is.Not.Null, "the add slot is the type-ahead search lane");
		}

		[Test]
		public void Search_MatchesHeadwordPrefix_CaseInsensitive_ExcludingSelfAndPresentItems()
		{
			// NB: the memory-only fixture's cache persists across the fixture's tests, so the
			// lexicon may hold look-alike entries from other tests' setups — the assertions are
			// containment-based against THIS test's objects.
			var composed = Compose();
			var field = ComponentsField(composed);

			var results = field.SearchOptions("CA");
			Assert.That(results.Select(r => r.Key), Does.Contain(m_cantar.Guid.ToString()),
				"the headword-prefix match is case-insensitive");
			Assert.That(results.Select(r => r.Key), Does.Not.Contain(m_casa.Guid.ToString()),
				"casa is excluded because it is already in the vector");
			Assert.That(results.Select(r => r.Name),
				Has.All.StartsWith("ca").IgnoreCase, "every result matches the prefix");
			Assert.That(results.Single(r => r.Key == m_cantar.Guid.ToString()).Name,
				Is.EqualTo(m_cantar.HeadWord.Text), "results display the headword");

			Assert.That(field.SearchOptions("bu").Select(r => r.Key),
				Does.Not.Contain(m_entry.Guid.ToString()),
				"the pane's own entry is excluded — no self-reference offers");
			Assert.That(field.SearchOptions("zz"), Is.Empty, "a non-matching search returns empty");
			Assert.That(field.SearchOptions("  "), Is.Empty,
				"a blank query returns empty rather than enumerating the lexicon");
		}

		[Test]
		public void Add_EntryBySearchKey_CommitsAsOneUndoStep_AndAppliesLegacyComponentCoupling()
		{
			// Legacy-coupling baseline (D3 item 3): a plain ComponentLexemesRS.Add in setup did NOT
			// populate PrimaryLexemes/ShowComplexFormsIn — LCModel has no ADD side effect, so the
			// composer's setter must carry EntrySequenceReferenceLauncher.AddNewObjectsToProperty's
			// coupling explicitly.
			Assert.That(m_ref.PrimaryLexemesRS, Is.Empty,
				"baseline: LCModel does not couple ComponentLexemes adds by itself");

			var composed = Compose();
			var field = ComponentsField(composed);

			Assert.That(composed.EditContext.TryAddReferenceItem(field, m_perro.Guid.ToString()), Is.True,
				"a key returned by search (the entry guid) stages through the fenced session");
			composed.EditContext.Commit();

			Assert.That(m_ref.ComponentLexemesRS.Select(c => c.Guid),
				Is.EqualTo(new[] { m_casa.Guid, m_perro.Guid }), "the add appends to the vector");
			Assert.That(m_ref.PrimaryLexemesRS.Select(p => p.Guid), Is.EqualTo(new[] { m_perro.Guid }),
				"legacy coupling: the component added while PrimaryLexemes was empty becomes primary");
			Assert.That(m_ref.ShowComplexFormsInRS.Select(s => s.Guid), Does.Contain(m_perro.Guid),
				"legacy coupling: a non-derivative complex form shows under the new component");

			Cache.ActionHandlerAccessor.Undo();
			Assert.That(m_ref.ComponentLexemesRS.Select(c => c.Guid), Is.EqualTo(new[] { m_casa.Guid }),
				"the add plus its coupling is ONE step on the global undo stack");
			Assert.That(m_ref.PrimaryLexemesRS, Is.Empty);
			Assert.That(m_ref.ShowComplexFormsInRS, Is.Empty);
		}

		[Test]
		public void Add_SenseTarget_IsAccepted_AndDisplaysTheOwnerOutline()
		{
			var composed = Compose();
			var field = ComponentsField(composed);

			Assert.That(composed.EditContext.TryAddReferenceItem(field, m_perroSense.Guid.ToString()),
				Is.True, "ComponentLexemes targets ILexEntryOrLexSense — a sense guid is a valid key");
			composed.EditContext.Commit();
			Assert.That(m_ref.ComponentLexemesRS.Select(c => c.Guid), Does.Contain(m_perroSense.Guid));

			var recomposed = ComponentsField(Compose());
			Assert.That(recomposed.Items.Select(i => i.Key), Does.Contain(m_perroSense.Guid.ToString()));
			Assert.That(recomposed.Items.Single(i => i.Key == m_perroSense.Guid.ToString()).Name,
				Is.EqualTo(m_perroSense.OwnerOutlineNameForWs(Cache.DefaultVernWs).Text),
				"a sense item displays the owner-outline headword, like the legacy HeadWord display");
		}

		[Test]
		public void Remove_Component_ClearsPrimaryLexemes_ViaLCModelSideEffects()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				m_ref.PrimaryLexemesRS.Add(m_casa);
				m_ref.ShowComplexFormsInRS.Add(m_casa);
			});

			var composed = Compose();
			var field = ComponentsField(composed);

			Assert.That(composed.EditContext.TryRemoveReferenceItem(field, m_casa.Guid.ToString()), Is.True);
			composed.EditContext.Commit();

			Assert.That(m_ref.ComponentLexemesRS, Is.Empty);
			// Legacy-coupling finding (D3 item 3), pinned empirically: unlike ADD, the
			// PrimaryLexemes REMOVE coupling IS an LCModel side effect, so the composer's plain
			// sda.Replace removal needs no twin there. ShowComplexFormsIn is NOT cleared by
			// LCModel — and the legacy slice's non-virtual remove path (VectorReferenceView →
			// plain vector removal) adds no explicit coupling either, so retaining it is
			// legacy-faithful (recorded in the D3 lane notes).
			Assert.That(m_ref.PrimaryLexemesRS, Is.Empty,
				"LCModel's remove side effects clear the departing component from PrimaryLexemes");
			Assert.That(m_ref.ShowComplexFormsInRS.Select(s => s.Guid), Is.EqualTo(new[] { m_casa.Guid }),
				"ShowComplexFormsIn is untouched, exactly like the legacy slice's plain removal");

			Cache.ActionHandlerAccessor.Undo();
			Assert.That(m_ref.ComponentLexemesRS.Select(c => c.Guid), Is.EqualTo(new[] { m_casa.Guid }),
				"the remove is one undo step");
			Assert.That(m_ref.PrimaryLexemesRS.Select(p => p.Guid), Is.EqualTo(new[] { m_casa.Guid }),
				"undo restores the LCModel-coupled PrimaryLexemes removal too");
		}

		[Test]
		public void Add_RejectsSelfDuplicateGarbageAndNonEntryTargets_WithoutOpeningASession()
		{
			var composed = Compose();
			var field = ComponentsField(composed);

			Assert.That(composed.EditContext.TryAddReferenceItem(field, m_entry.Guid.ToString()), Is.False,
				"a direct self-reference (the pane's own entry as its own component) must reject");
			Assert.That(composed.EditContext.TryAddReferenceItem(field, m_casa.Guid.ToString()), Is.False,
				"duplicates are rejected, like the legacy launcher's AddItem");
			Assert.That(composed.EditContext.TryAddReferenceItem(field, "not-a-guid"), Is.False);
			Assert.That(composed.EditContext.TryAddReferenceItem(field, Guid.NewGuid().ToString()), Is.False,
				"an unknown guid must not stage");
			Assert.That(composed.EditContext.TryAddReferenceItem(field, m_ref.Guid.ToString()), Is.False,
				"an object that is neither entry nor sense must not stage");
			Assert.That(composed.EditContext.TryRemoveReferenceItem(field, m_cantar.Guid.ToString()),
				Is.False, "removing an item that is not in the vector must not stage");

			Assert.That(composed.EditContext.IsOpen, Is.False, "rejected edits must not open the fence");
			Assert.That(m_ref.ComponentLexemesRS.Count, Is.EqualTo(1), "nothing was written");
		}
	}
}

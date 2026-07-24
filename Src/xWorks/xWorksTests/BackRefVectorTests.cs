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
	/// Task 1 — virtual back-reference editable writes. The Subentries and VisibleComplexFormBackRefs
	/// virtual vectors are OWNED by the OTHER entry's LexEntryRef, so the composer routes add/remove
	/// across objects (legacy EntrySequenceReferenceLauncher.AddNewObjectsToProperty /
	/// RemoveFromPropertyAt): Subentries writes the chosen complex-form entry's
	/// LexEntryRef.PrimaryLexemes; VisibleComplexFormBackRefs writes its ShowComplexFormsIn. The pane
	/// entry's own flid is never written. VariantFormEntryBackRefs stays READ-ONLY (its legacy add
	/// inserts a NEW variant entry, not a chooser-add of an existing ref) — asserted below.
	/// </summary>
	[TestFixture]
	public class BackRefVectorTests : MemoryOnlyBackendProviderTestBase
	{
		private ILexEntry m_component; // "casa" — the composed pane's own entry (a component)
		private ILexEntry m_complex;   // "casita" — a complex form whose components include casa
		private ILexEntryRef m_complexRef;
		private ILexEntry m_complex2;  // "casona" — a second complex form of casa (add candidate)
		private ILexEntryRef m_complex2Ref;

		public override void TestSetup()
		{
			base.TestSetup();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				m_component = MakeEntry("casa");
				m_complex = MakeEntry("casita");
				m_complex2 = MakeEntry("casona");

				m_complexRef = MakeComplexFormRef(m_complex, m_component);
				m_complex2Ref = MakeComplexFormRef(m_complex2, m_component);
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

		private ILexEntryRef MakeComplexFormRef(ILexEntry complexForm, ILexEntry component)
		{
			var ler = Cache.ServiceLocator.GetInstance<ILexEntryRefFactory>().Create();
			complexForm.EntryRefsOS.Add(ler);
			ler.RefType = LexEntryRefTags.krtComplexForm;
			ler.ComponentLexemesRS.Add(component);
			return ler;
		}

		private ComposedEntryRegion Compose() => FullEntryRegionComposer.Compose(m_component, Cache);

		private static LexicalEditRegionField FindField(ComposedEntryRegion composed, string field)
			=> composed.Model.Fields.SingleOrDefault(f => f.Field == field
				&& f.Kind == RegionFieldKind.ReferenceVector);

		// ===== Subentries =====

		[Test]
		public void Subentries_IsEditableReferenceVector_WithSearchNotOptions()
		{
			// Make casita a subentry of casa (PrimaryLexemes contains casa).
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
				m_complexRef.PrimaryLexemesRS.Add(m_component));

			var field = FindField(Compose(), "Subentries");
			Assert.That(field, Is.Not.Null, "the Subentries virtual vector composes as a ReferenceVector row");
			Assert.That(field.IsEditable, Is.True, "the cross-object back-ref write makes it editable");
			Assert.That(field.Items.Select(i => i.Key), Is.EqualTo(new[] { m_complex.Guid.ToString() }),
				"items key on the OWNING complex-form entry, in vector order");
			Assert.That(field.Items.Single().Name, Is.EqualTo(m_complex.HeadWord.Text),
				"items display the owning entry's headword");
			Assert.That(field.Options, Is.Empty, "lexicons SEARCH: no full option list materialized");
			Assert.That(field.SearchOptions, Is.Not.Null, "the add slot is the type-ahead search path");
			Assert.That(field.SearchOptions("cas").Select(r => r.Key), Does.Contain(m_complex2.Guid.ToString()),
				"the search offers the entry's other complex forms not yet subentries");
		}

		[Test]
		public void Subentries_Add_WritesThePrimaryLexemesOfTheChosenEntryRef_AndRoundTrips()
		{
			Assert.That(m_complexRef.PrimaryLexemesRS, Is.Empty, "baseline: not yet a subentry");

			var composed = Compose();
			var field = FindField(composed, "Subentries");
			// With no current subentries the field hides-when-empty unless always-visible; surface it
			// by making one subentry exist first so the row composes with the add slot.
			if (field == null)
			{
				NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
					m_complex2Ref.PrimaryLexemesRS.Add(m_component));
				composed = Compose();
				field = FindField(composed, "Subentries");
			}
			Assert.That(field, Is.Not.Null);

			Assert.That(composed.EditContext.TryAddReferenceItem(field, m_complex.Guid.ToString()), Is.True,
				"adding the complex-form entry stages the cross-object PrimaryLexemes write");
			composed.EditContext.Commit();

			Assert.That(m_complexRef.PrimaryLexemesRS.Select(p => p.Guid),
				Is.EqualTo(new[] { m_component.Guid }),
				"the write landed on the CHOSEN entry's LexEntryRef.PrimaryLexemes, not the pane's flid");
			Assert.That(m_component.ComplexFormEntries.Contains(m_complex), Is.True,
				"the relationship round-trips: casita is a complex form of casa");

			Cache.ActionHandlerAccessor.Undo();
			Assert.That(m_complexRef.PrimaryLexemesRS, Is.Empty, "the cross-object add is one undo step");
		}

		[Test]
		public void Subentries_Remove_ClearsThePrimaryLexemesOfTheOwningRef_AndRoundTrips()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
				m_complexRef.PrimaryLexemesRS.Add(m_component));

			var composed = Compose();
			var field = FindField(composed, "Subentries");
			Assert.That(field, Is.Not.Null);
			Assert.That(field.Items.Select(i => i.Key), Does.Contain(m_complex.Guid.ToString()));

			Assert.That(composed.EditContext.TryRemoveReferenceItem(field, m_complex.Guid.ToString()), Is.True);
			composed.EditContext.Commit();

			Assert.That(m_complexRef.PrimaryLexemesRS, Is.Empty,
				"remove clears m_component from the owning ref's PrimaryLexemes");

			Cache.ActionHandlerAccessor.Undo();
			Assert.That(m_complexRef.PrimaryLexemesRS.Select(p => p.Guid),
				Is.EqualTo(new[] { m_component.Guid }), "the cross-object remove is one undo step");
		}

		[Test]
		public void Subentries_RejectsGarbageDuplicateAndNonComplexFormKeys_WithoutOpeningASession()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
				m_complexRef.PrimaryLexemesRS.Add(m_component));

			var composed = Compose();
			var field = FindField(composed, "Subentries");

			Assert.That(composed.EditContext.TryAddReferenceItem(field, m_complex.Guid.ToString()), Is.False,
				"already a subentry: duplicate rejects");
			Assert.That(composed.EditContext.TryAddReferenceItem(field, "not-a-guid"), Is.False);
			Assert.That(composed.EditContext.TryAddReferenceItem(field, Guid.NewGuid().ToString()), Is.False);
			Assert.That(composed.EditContext.TryAddReferenceItem(field, m_component.Guid.ToString()), Is.False,
				"an entry that is not a complex form of the pane entry has no owning ref: rejects");
			Assert.That(composed.EditContext.TryRemoveReferenceItem(field, m_complex2.Guid.ToString()), Is.False,
				"removing an entry not in the vector rejects");

			Assert.That(composed.EditContext.IsOpen, Is.False, "rejected edits never open the fence");
			Assert.That(m_complexRef.PrimaryLexemesRS.Count, Is.EqualTo(1), "nothing changed");
		}

		// ===== VisibleComplexFormBackRefs =====

		[Test]
		public void VisibleComplexFormBackRefs_Add_WritesShowComplexFormsInOfTheOwningRef_AndRoundTrips()
		{
			// Surface the row by making at least one back-ref present first.
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
				m_complex2Ref.ShowComplexFormsInRS.Add(m_component));

			var composed = Compose();
			var field = FindField(composed, "VisibleComplexFormBackRefs");
			Assert.That(field, Is.Not.Null, "the VisibleComplexFormBackRefs virtual vector composes editable");
			Assert.That(field.IsEditable, Is.True);
			Assert.That(field.Items.Select(i => i.Key), Does.Contain(m_complex2.Guid.ToString()),
				"items key on the OWNING complex-form entry of each back-ref");

			Assert.That(composed.EditContext.TryAddReferenceItem(field, m_complex.Guid.ToString()), Is.True);
			composed.EditContext.Commit();

			Assert.That(m_complexRef.ShowComplexFormsInRS.Select(s => s.Guid),
				Is.EqualTo(new[] { m_component.Guid }),
				"the write landed on the chosen entry's LexEntryRef.ShowComplexFormsIn");

			Cache.ActionHandlerAccessor.Undo();
			Assert.That(m_complexRef.ShowComplexFormsInRS, Is.Empty, "one undo step");
		}

		[Test]
		public void VisibleComplexFormBackRefs_Remove_ClearsShowComplexFormsInOfTheOwningRef()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				m_complexRef.ShowComplexFormsInRS.Add(m_component);
				m_complex2Ref.ShowComplexFormsInRS.Add(m_component);
			});

			var composed = Compose();
			var field = FindField(composed, "VisibleComplexFormBackRefs");
			Assert.That(field, Is.Not.Null);

			Assert.That(composed.EditContext.TryRemoveReferenceItem(field, m_complex.Guid.ToString()), Is.True);
			composed.EditContext.Commit();

			Assert.That(m_complexRef.ShowComplexFormsInRS, Is.Empty,
				"the chosen entry's ref no longer shows the pane entry");
			Assert.That(m_complex2Ref.ShowComplexFormsInRS.Select(s => s.Guid),
				Is.EqualTo(new[] { m_component.Guid }), "the OTHER back-ref is untouched");
		}

		// ===== Deferred: VariantFormEntryBackRefs stays read-only =====

		[Test]
		public void VariantFormEntryBackRefs_StaysReadOnly()
		{
			// Make casa a variant of casita (a variant LexEntryRef on casa whose component is casita),
			// so casita.VariantFormEntryBackRefs contains that ref. Compose casita (the referenced
			// entry) and assert the back-ref vector, if surfaced, is NOT editable.
			ILexEntry referenced = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				referenced = MakeEntry("raiz");
				var variantRef = Cache.ServiceLocator.GetInstance<ILexEntryRefFactory>().Create();
				m_component.EntryRefsOS.Add(variantRef);
				variantRef.RefType = LexEntryRefTags.krtVariant;
				variantRef.ComponentLexemesRS.Add(referenced);
			});

			var composed = FullEntryRegionComposer.Compose(referenced, Cache);
			var field = composed.Model.Fields.FirstOrDefault(f => f.Field == "VariantFormEntryBackRefs"
				&& f.Kind == RegionFieldKind.ReferenceVector);
			if (field != null)
				Assert.That(field.IsEditable, Is.False,
					"VariantFormEntryBackRefs is deferred: its legacy add inserts a NEW variant entry");
		}
	}
}

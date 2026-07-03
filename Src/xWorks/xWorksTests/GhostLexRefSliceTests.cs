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
	/// D3 follow-up — the top-level ghost relation rows (`GhostLexRefSlice`) for empty Components /
	/// Variant of must not fall back to the unsupported lane. They compose as editable
	/// search-backed ReferenceVector rows and create the missing LexEntryRef on first add.
	/// </summary>
	[TestFixture]
	public class GhostLexRefSliceTests : MemoryOnlyBackendProviderTestBase
	{
		private ILexEntry m_entry;
		private ILexEntry m_component;
		private ILexEntry m_variant;

		public override void TestSetup()
		{
			base.TestSetup();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				m_entry = MakeEntry("burro");
				m_component = MakeEntry("casa");
				m_variant = MakeEntry("cantar");
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


		private ComposedEntryRegion Compose(bool showHidden = false)
			=> FullEntryRegionComposer.Compose(m_entry, Cache, showHidden);

		private static LexicalEditRegionField GhostRow(ComposedEntryRegion composed, string label)
			=> composed.Model.Fields.Single(f => f.Field == "EntryRefs" && f.Label == label);

		[Test]
		public void Compose_EmptyGhostRelationRows_AreEditableReferenceVectors_NotUnsupported()
		{
			var composed = Compose(showHidden: true);
			var components = GhostRow(composed, "Components");
			var variantOf = GhostRow(composed, "Variant of");

			Assert.That(components.Kind, Is.EqualTo(RegionFieldKind.ReferenceVector),
				"an empty Components ghost row must compose as an editable reference vector");
			Assert.That(variantOf.Kind, Is.EqualTo(RegionFieldKind.ReferenceVector),
				"an empty Variant of ghost row must compose as an editable reference vector");
			Assert.That(components.IsEditable, Is.True);
			Assert.That(variantOf.IsEditable, Is.True);
			Assert.That(components.Items, Is.Empty);
			Assert.That(variantOf.Items, Is.Empty);
			Assert.That(components.Options, Is.Empty,
				"lexicon-backed add uses search, not a pre-materialized option list");
			Assert.That(variantOf.Options, Is.Empty);
			Assert.That(components.SearchOptions, Is.Not.Null);
			Assert.That(variantOf.SearchOptions, Is.Not.Null);
			Assert.That(composed.Model.Fields.Any(f => f.Kind == RegionFieldKind.Unsupported
				&& (f.Label == "Components" || f.Label == "Variant of")), Is.False,
				"the ghost relation rows must not degrade to ksUnsupportedEditor");

			var search = components.SearchOptions("ca");
			Assert.That(search.Select(r => r.Key), Does.Contain(m_component.Guid.ToString()));
			Assert.That(search.Select(r => r.Key), Does.Not.Contain(m_entry.Guid.ToString()),
				"the owning entry itself must never be offered as its own component/variant target");
		}

		[Test]
		public void Edit_GhostComponents_Add_CreatesComplexFormEntryRef_AndIsUndoable()
		{
			var composed = Compose();
			var components = GhostRow(composed, "Components");

			Assert.That(composed.EditContext.TryAddReferenceItem(components, m_component.Guid.ToString()),
				Is.True, "first add should create the missing complex-form LexEntryRef");
			composed.EditContext.Commit();

			Assert.That(m_entry.EntryRefsOS, Has.Count.EqualTo(1));
			var created = m_entry.EntryRefsOS.Single();
			Assert.That(created.RefType, Is.EqualTo(LexEntryRefTags.krtComplexForm));
			Assert.That(created.ComponentLexemesRS.Select(c => c.Guid),
				Is.EqualTo(new[] { m_component.Guid }));
			Assert.That(created.PrimaryLexemesRS.Select(c => c.Guid),
				Is.EqualTo(new[] { m_component.Guid }),
				"ghost complex-form creation keeps the legacy first-component coupling");
			Assert.That(created.ComplexEntryTypesRS.Any(), Is.True,
				"ghost complex-form creation seeds the unspecified complex-form type");

			Cache.ActionHandlerAccessor.Undo();
			Assert.That(m_entry.EntryRefsOS, Is.Empty,
				"creating the LexEntryRef and wiring its targets must be one undo step");
		}

		[Test]
		public void Edit_GhostVariantOf_Add_CreatesVariantEntryRef_AndIsUndoable()
		{
			var composed = Compose(showHidden: true);
			var variantOf = GhostRow(composed, "Variant of");

			Assert.That(composed.EditContext.TryAddReferenceItem(variantOf, m_variant.Guid.ToString()),
				Is.True, "first add should create the missing variant LexEntryRef");
			composed.EditContext.Commit();

			Assert.That(m_entry.EntryRefsOS, Has.Count.EqualTo(1));
			var created = m_entry.EntryRefsOS.Single();
			Assert.That(created.RefType, Is.EqualTo(LexEntryRefTags.krtVariant));
			Assert.That(created.ComponentLexemesRS.Select(c => c.Guid),
				Is.EqualTo(new[] { m_variant.Guid }));
			Assert.That(created.VariantEntryTypesRS.Any(), Is.True,
				"ghost variant creation seeds the unspecified variant type");
			Assert.That(created.PrimaryLexemesRS, Is.Empty,
				"variant ghost creation does not apply the complex-form primary-lexeme coupling");

			Cache.ActionHandlerAccessor.Undo();
			Assert.That(m_entry.EntryRefsOS, Is.Empty,
				"creating the variant LexEntryRef must roll back as one undo step");
		}

		[Test]
		public void Edit_GhostRows_RejectInvalidKeys_WithoutOpeningTheFence()
		{
			var composed = Compose();
			var components = GhostRow(composed, "Components");

			Assert.That(composed.EditContext.TryAddReferenceItem(components, m_entry.Guid.ToString()), Is.False,
				"self-reference must reject");
			Assert.That(composed.EditContext.TryAddReferenceItem(components, "not-a-guid"), Is.False);
			Assert.That(composed.EditContext.TryAddReferenceItem(components, Guid.NewGuid().ToString()), Is.False,
				"an unknown guid must not stage");
			Assert.That(composed.EditContext.IsOpen, Is.False,
				"rejected ghost edits must not open the fenced session");
			Assert.That(m_entry.EntryRefsOS, Is.Empty, "nothing was written");
		}
	}
}
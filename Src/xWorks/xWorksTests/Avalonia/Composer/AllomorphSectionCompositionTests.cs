// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Composition of the entry-level Allomorphs section (LexEntry.fwlayout's
	/// AlternateFormsSection summary header over the AlternateForms owning sequence), covering
	/// the
	/// structural and label parity items of the LT-22672 test plan.
	///
	/// Legacy nests three levels -- the "Allomorphs" banner, the per-allomorph row labelled by
	/// its
	/// concrete type, then that allomorph's fields. These tests assert the composed model matches
	/// that shape, so the extra sequence banner and the raw model-name item labels the composer
	/// currently emits are failures rather than accepted behavior.
	/// </summary>
	[TestFixture]
	public class AllomorphSectionCompositionTests : MemoryOnlyBackendProviderTestBase
	{
		private const string SectionLabel = "Allomorphs";
		private const string StemItemLabel = "Stem Allomorph";
		private const string AffixItemLabel = "Affix Allomorph";

		private ILexEntry m_entry;

		public override void TestSetup()
		{
			base.TestSetup();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				m_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				var lexemeForm = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				m_entry.LexemeFormOA = lexemeForm;
				lexemeForm.Form.set_String(Cache.DefaultVernWs,
					TsStringUtils.MakeString("barigi", Cache.DefaultVernWs));

				var stem = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				m_entry.AlternateFormsOS.Add(stem);
				stem.Form.set_String(Cache.DefaultVernWs,
					TsStringUtils.MakeString("barigi-stem", Cache.DefaultVernWs));

				var affix = Cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>().Create();
				m_entry.AlternateFormsOS.Add(affix);
				affix.Form.set_String(Cache.DefaultVernWs,
					TsStringUtils.MakeString("-ri", Cache.DefaultVernWs));
			});
		}

		// The section header plus every following row indented deeper than it, stopping at the
		// next row at its own level or shallower -- the view's own ownership rule for a
		// collapsible header.
		private static List<DetailField> SectionSubtree(IReadOnlyList<DetailField> fields)
		{
			var headerIndex = fields.ToList().FindIndex(
				f => f.Kind == DetailFieldKind.Header && f.Label == SectionLabel);
			Assert.That(headerIndex, Is.GreaterThanOrEqualTo(0),
				"the entry layout composes an Allomorphs section header");

			var subtree = new List<DetailField> { fields[headerIndex] };
			var sectionIndent = fields[headerIndex].Indent;
			for (var i = headerIndex + 1; i < fields.Count; i++)
			{
				if (fields[i].Indent <= sectionIndent)
					break;
				subtree.Add(fields[i]);
			}

			return subtree;
		}

		private List<DetailField> ComposeSectionSubtree(bool showHiddenFields = false)
			=> SectionSubtree(DetailComposer.Compose(m_entry, Cache, showHiddenFields).Model.Fields);

		private static string Describe(IEnumerable<DetailField> fields)
			=> string.Join("\n", fields.Select(f => $"  indent {f.Indent} | {f.Kind} | {f.Label}"));

		/// <summary>
		/// The section nests three levels like legacy -- banner, per-allomorph row, that
		/// allomorph's fields -- with exactly one banner and no per-item banner.
		/// </summary>
		[Test]
		public void Compose_AllomorphSection_MatchesLegacyRowStructure()
		{
			var subtree = ComposeSectionSubtree();
			var sectionIndent = subtree[0].Indent;

			var banners = subtree
				.Where(f => f.Kind == DetailFieldKind.Header && f.Indent <= sectionIndent + 1)
				.ToList();
			Assert.That(banners.Count, Is.EqualTo(1),
				"legacy shows ONE Allomorphs banner; a sequence banner under the section header is a "
				+ "duplicate. Composed:\n" + Describe(subtree));

			var itemRows = subtree.Where(f => f.Indent == sectionIndent + 1).ToList();
			Assert.That(itemRows.Count, Is.EqualTo(2),
				"one row per allomorph sits directly under the banner. Composed:\n" + Describe(subtree));

			Assert.That(subtree.Max(f => f.Indent), Is.EqualTo(sectionIndent + 2),
				"an allomorph's own fields are the deepest level; anything deeper is extra nesting. "
				+ "Composed:\n" + Describe(subtree));
		}

		/// <summary>
		/// No row label falls back to its raw model field name. A field-name fallback bypasses
		/// StringTable localization, so such a label is untranslatable by construction.
		/// </summary>
		[Test]
		public void Compose_AllomorphSection_EmitsNoRawModelFieldNames()
		{
			var subtree = ComposeSectionSubtree();

			var rawNamed = subtree
				.Where(f => !string.IsNullOrEmpty(f.Label)
					&& (f.Label == f.Field || f.Label.Contains("AlternateForms")))
				.ToList();

			Assert.That(rawNamed, Is.Empty,
				"a user-visible label must never be the model field name. Offending rows:\n"
				+ Describe(rawNamed));
		}

		/// <summary>
		/// Each allomorph row is labelled by its concrete type, from the item layout's own
		/// label, rather than a numbered section-plus-index string.
		/// </summary>
		[Test]
		public void Compose_AllomorphItems_UseTypeLabels_WhenSectionHasStemAndAffix()
		{
			var subtree = ComposeSectionSubtree();
			var sectionIndent = subtree[0].Indent;
			var itemLabels = subtree
				.Where(f => f.Indent == sectionIndent + 1)
				.Select(f => f.Label)
				.ToList();

			Assert.That(itemLabels, Does.Contain(StemItemLabel),
				"the stem allomorph is labelled by its type. Composed:\n" + Describe(subtree));
			Assert.That(itemLabels, Does.Contain(AffixItemLabel),
				"the affix allomorph is labelled by its type. Composed:\n" + Describe(subtree));
		}

		/// <summary>
		/// The rows a user edits compose as real editors carrying real values, not as the
		/// labeled Unsupported worklist row.
		/// </summary>
		[Test]
		public void Compose_StemAllomorph_RendersFormAndMorphType_NotUnsupported()
		{
			var subtree = ComposeSectionSubtree();

			var forms = subtree.Where(f => f.Field == "Form").ToList();
			Assert.That(forms, Is.Not.Empty, "each allomorph composes its Form row");
			Assert.That(forms.All(f => f.Kind == DetailFieldKind.Text), Is.True,
				"Form is a multi-writing-system text row");
			Assert.That(
				forms.Any(f => f.Values != null
					&& f.Values.Any(v => v.Value == "barigi-stem")),
				Is.True,
				"the stem allomorph's Form row carries its vernacular value");

			var morphTypes = subtree.Where(f => f.Field == "MorphType").ToList();
			Assert.That(morphTypes, Is.Not.Empty, "each allomorph composes its Morph Type row");
			Assert.That(morphTypes.All(f => f.Kind == DetailFieldKind.Chooser), Is.True,
				"Morph Type is a chooser row, never an Unsupported row");
		}

		/// <summary>
		/// The Show Hidden Fields setting gates the section's never/ifdata rows.
		/// </summary>
		[Test]
		public void Compose_AllomorphSection_HidesIfDataRows_WhenShowHiddenFieldsIsOff()
		{
			var hidden = ComposeSectionSubtree(showHiddenFields: false);
			var shown = ComposeSectionSubtree(showHiddenFields: true);

			Assert.That(hidden.Any(f => f.Field == "IsAbstract"), Is.False,
				"IsAbstract is visibility=never, so it is absent unless hidden fields are shown");
			Assert.That(shown.Any(f => f.Field == "IsAbstract"), Is.True,
				"IsAbstract appears once hidden fields are shown");

			Assert.That(hidden.Any(f => f.Field == "StemName"), Is.False,
				"StemName is visibility=ifdata and empty here, so it is absent");
		}

		/// <summary>
		/// An affix process composes without throwing and degrades honestly -- its rule formula
		/// is the labeled Unsupported row while the rest of the item composes normally.
		/// </summary>
		[Test]
		public void Compose_AffixProcess_RendersUnsupportedRow_ForRuleFormula()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var process = Cache.ServiceLocator.GetInstance<IMoAffixProcessFactory>().Create();
				m_entry.AlternateFormsOS.Add(process);
			});

			var subtree = ComposeSectionSubtree(showHiddenFields: true);

			Assert.That(subtree.Any(f => f.Kind == DetailFieldKind.Unsupported), Is.True,
				"the affix process rule formula has no Avalonia editor and shows the Unsupported row");
			Assert.That(subtree.Count(f => f.Field == "Form"), Is.EqualTo(3),
				"the process allomorph still composes its own Form row alongside the other two");
		}

		/// <summary>
		/// An entry with no allomorphs offers the watermarked add row rather than nothing.
		/// </summary>
		[Test]
		public void Compose_EmptyAllomorphSection_ShowsGhostAddRow()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor,
				() => m_entry.AlternateFormsOS.Clear());

			var subtree = ComposeSectionSubtree();

			Assert.That(subtree.Any(f => !string.IsNullOrEmpty(f.GhostPrompt)), Is.True,
				"an empty allomorph sequence composes the ghost add row. Composed:\n"
				+ Describe(subtree));
		}

		/// <summary>
		/// The domain decides whether a field applies to THIS object, and the composer must ask
		/// --
		/// legacy does, through SliceFilter -> ICmObject.IsFieldRelevant.
		/// MoStemAllomorph.IsFieldRelevant returns false for StemName unless the morph type is a
		/// root/stem/phrase kind, so a clitic or particle allomorph has no Stem Allomorph Label
		/// row at all.
		///
		/// Found by the developer: legacy showed the row on one allomorph of four, the New UI on
		/// all four. The four differed only by morph type.
		/// </summary>
		[Test]
		public void Compose_StemName_OnlyWhereTheDomainSaysTheFieldApplies()
		{
			var morphTypes = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>();
			IMoStemAllomorph stem = null;
			IMoStemAllomorph particle = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				stem = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				m_entry.AlternateFormsOS.Add(stem);
				stem.Form.set_String(Cache.DefaultVernWs,
					TsStringUtils.MakeString("stemform", Cache.DefaultVernWs));
				stem.MorphTypeRA = morphTypes.GetObject(MoMorphTypeTags.kguidMorphStem);

				particle = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				m_entry.AlternateFormsOS.Add(particle);
				particle.Form.set_String(Cache.DefaultVernWs,
					TsStringUtils.MakeString("particleform", Cache.DefaultVernWs));
				particle.MorphTypeRA = morphTypes.GetObject(MoMorphTypeTags.kguidMorphParticle);
			});

			var fields = DetailComposer.Compose(m_entry, Cache, showHiddenFields: true).Model.Fields;

			Assert.That(fields.Any(f => f.Field == "StemName" && f.ObjectHvo == stem.Hvo), Is.True,
				"a stem-type allomorph keeps its Stem Allomorph Label row");
			Assert.That(fields.Any(f => f.Field == "StemName" && f.ObjectHvo == particle.Hvo),
				Is.False,
				"a particle has no stem name to give, so the row must not compose -- and note "
				+ "showHiddenFields is ON: relevance is not a hidden field, it does not apply");
		}
	}
}

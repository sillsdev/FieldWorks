// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The composer asks the DOMAIN whether a field applies to an object before emitting a row,
	/// which legacy does through SliceFilter -> ICmObject.IsFieldRelevant.
	///
	/// Five classes override it in liblcm. MoStemAllomorph.StemName is covered by
	/// AllomorphSectionCompositionTests; the detail-view relevant remainder is covered here.
	/// VirtualOrdering also overrides it, but that class is not shown in a detail view, so there
	/// is nothing for the composer to gate.
	///
	/// Every test runs with showHiddenFields TRUE. Relevance is not a hidden field -- legacy
	/// withholds an irrelevant row even with Show Hidden Fields on -- and the flag also keeps an
	/// ifdata row from disappearing for the WRONG reason and passing the test by accident.
	/// </summary>
	[TestFixture]
	public class DetailFieldRelevanceTests : MemoryOnlyBackendProviderTestBase
	{
		private IMoMorphTypeRepository MorphTypes
			=> Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>();

		private static bool HasRow(System.Collections.Generic.IEnumerable<
			SIL.FieldWorks.Common.FwAvalonia.Detail.DetailField> fields, string field, int hvo)
			=> fields.Any(f => f.Field == field && f.ObjectHvo == hvo);

		/// <summary>
		/// A prefix composes no Infix Positions row.
		///
		/// NOT relevance coverage, despite living here. Verified by removing the relevance gate:
		/// this still passes, because the layout already excludes Position for a non-infix
		/// through its own choice/where guidequals filter, so IsFieldRelevant is never reached.
		/// The row is gated twice and this pins the outer gate -- kept because the negative case
		/// was otherwise untested, and labelled so nobody reads it as covering the domain rule.
		/// MoAffixAllomorph.Position's relevance rule therefore has NO composer test that bites.
		/// </summary>
		[Test]
		public void Compose_InfixPosition_OnlyForAnInfix()
		{
			ILexEntry entry = null;
			IMoAffixAllomorph infix = null;
			IMoAffixAllomorph prefix = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				infix = Cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>().Create();
				entry.AlternateFormsOS.Add(infix);
				infix.Form.set_String(Cache.DefaultVernWs,
					TsStringUtils.MakeString("infixo", Cache.DefaultVernWs));
				infix.MorphTypeRA = MorphTypes.GetObject(MoMorphTypeTags.kguidMorphInfix);

				prefix = Cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>().Create();
				entry.AlternateFormsOS.Add(prefix);
				prefix.Form.set_String(Cache.DefaultVernWs,
					TsStringUtils.MakeString("prefixo", Cache.DefaultVernWs));
				prefix.MorphTypeRA = MorphTypes.GetObject(MoMorphTypeTags.kguidMorphPrefix);
			});

			var fields = DetailComposer.Compose(entry, Cache, showHiddenFields: true).Model.Fields;

			Assert.That(HasRow(fields, "Position", infix.Hvo), Is.True,
				"an infix has a position, so the row composes");
			Assert.That(HasRow(fields, "Position", prefix.Hvo), Is.False,
				"a prefix does not -- excluded by the layout's guidequals filter before the "
				+ "domain rule is even consulted");
		}

		/// <summary>
		/// MoAffixForm.InflectionClasses is relevant only when the entry's MSAs include an
		/// inflectional affix MSA (ILexEntry.SupportsInflectionClasses). An entry with a stem MSA
		/// has no inflection classes to offer its affix forms.
		///
		/// Composed twice over one fixture, with only the precondition changed between them: the
		/// absent half alone would pass just as well if the row never composed for any reason.
		/// </summary>
		[Test]
		public void Compose_AffixInflectionClasses_OnlyWhenTheEntrySupportsThem()
		{
			ILexEntry entry = null;
			IMoAffixAllomorph affix = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				affix = Cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>().Create();
				entry.AlternateFormsOS.Add(affix);
				affix.Form.set_String(Cache.DefaultVernWs,
					TsStringUtils.MakeString("affixo", Cache.DefaultVernWs));
				affix.MorphTypeRA = MorphTypes.GetObject(MoMorphTypeTags.kguidMorphPrefix);
				// A STEM msa, so the entry supports no inflection classes.
				entry.MorphoSyntaxAnalysesOC.Add(
					Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create());
			});

			Assert.That(entry.SupportsInflectionClasses(), Is.False,
				"fixture check: a stem MSA gives the entry no inflection classes");

			var fields = DetailComposer.Compose(entry, Cache, showHiddenFields: true).Model.Fields;

			Assert.That(HasRow(fields, "InflectionClasses", affix.Hvo), Is.False,
				"with nothing to choose from, the domain says the row does not apply");

			// The positive control: give the entry an inflectional affix MSA with a part of
			// speech, change nothing else, and the same row on the same object composes.
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var pos = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create();
				Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Add(pos);
				pos.Name.set_String(Cache.DefaultAnalWs,
					TsStringUtils.MakeString("Noun", Cache.DefaultAnalWs));
				var inflMsa = Cache.ServiceLocator.GetInstance<IMoInflAffMsaFactory>().Create();
				entry.MorphoSyntaxAnalysesOC.Add(inflMsa);
				inflMsa.PartOfSpeechRA = pos;
			});

			Assert.That(entry.SupportsInflectionClasses(), Is.True,
				"fixture check: an inflectional affix MSA with a part of speech supplies them");

			fields = DetailComposer.Compose(entry, Cache, showHiddenFields: true).Model.Fields;

			Assert.That(HasRow(fields, "InflectionClasses", affix.Hvo), Is.True,
				"now there is something to choose from, so the row composes");
		}

		/// <summary>
		/// MoStemMsa.InflectionClass is irrelevant until a part of speech is chosen -- there is
		/// no inflection class to pick without one. This one is NOT an allomorph field; it is the
		/// Grammatical Info section, which is why the gate lives in Walk rather than in the
		/// allomorph path.
		///
		/// Composed twice over one fixture, with only the part of speech changed between them:
		/// the absent half alone would pass just as well if the row never composed for any
		/// reason.
		/// </summary>
		[Test]
		public void Compose_StemMsaInflectionClass_OnlyOnceAPartOfSpeechIsChosen()
		{
			ILexEntry entry = null;
			IMoStemMsa msa = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				var lexemeForm = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				entry.LexemeFormOA = lexemeForm;
				lexemeForm.Form.set_String(Cache.DefaultVernWs,
					TsStringUtils.MakeString("barigi", Cache.DefaultVernWs));
				msa = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
				entry.MorphoSyntaxAnalysesOC.Add(msa); // no PartOfSpeechRA
			});

			var fields = DetailComposer.Compose(entry, Cache, showHiddenFields: true).Model.Fields;

			Assert.That(HasRow(fields, "InflectionClass", msa.Hvo), Is.False,
				"no part of speech means no inflection class to choose, and the domain says so");

			// The positive control: choose one, change nothing else, and the same row on the
			// same object composes.
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var pos = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create();
				Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Add(pos);
				pos.Name.set_String(Cache.DefaultAnalWs,
					TsStringUtils.MakeString("Noun", Cache.DefaultAnalWs));
				msa.PartOfSpeechRA = pos;
			});

			fields = DetailComposer.Compose(entry, Cache, showHiddenFields: true).Model.Fields;

			Assert.That(HasRow(fields, "InflectionClass", msa.Hvo), Is.True,
				"with a part of speech chosen there is an inflection class to pick, so it shows");
		}
	}
}

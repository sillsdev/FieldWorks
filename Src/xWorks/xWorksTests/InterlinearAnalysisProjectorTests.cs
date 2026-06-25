// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// avalonia-interlinear-editor (task 1.3) — T1 for the read side of design Decision 1: the
	/// <see cref="InterlinearAnalysisProjector"/> walks a real <c>WfiAnalysis</c>'s morph bundles into the
	/// LCModel-free <c>InterlinearAnalysisModel</c>. Parity claim: each projected bundle surfaces exactly what
	/// the legacy <c>InterlinVc</c> morph-bundle lines show — the morph form, the lex-gloss
	/// (<c>SenseRA.Gloss</c>), and the grammatical-info abbreviation (<c>MsaRA.InterlinearAbbr</c>, the same
	/// the LexPos line uses) — plus the morph/sense/MSA GUIDs the write-back will key edits on. Asserted
	/// against the live LCModel values, so the test pins the projection to domain truth rather than a
	/// hand-copied string.
	/// </summary>
	[TestFixture]
	public class InterlinearAnalysisProjectorTests : MemoryOnlyBackendProviderTestBase
	{
		private IWfiWordform m_wordform;
		private IWfiAnalysis m_analysis;
		private IWfiMorphBundle m_prefix;
		private IWfiMorphBundle m_stem;

		public override void TestSetup()
		{
			base.TestSetup();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				m_wordform = Cache.ServiceLocator.GetInstance<IWfiWordformFactory>().Create();
				m_wordform.Form.set_String(Cache.DefaultVernWs, MakeVern("kapula"));

				m_analysis = Cache.ServiceLocator.GetInstance<IWfiAnalysisFactory>().Create();
				m_wordform.AnalysesOC.Add(m_analysis);

				m_prefix = MakeBundle("ka", "P", "pfx", MoMorphTypeTags.kguidMorphPrefix);
				m_stem = MakeBundle("pula", "rain", "n", MoMorphTypeTags.kguidMorphStem);
			});
		}

		private ITsString MakeVern(string text) => TsStringUtils.MakeString(text, Cache.DefaultVernWs);
		private ITsString MakeAnal(string text) => TsStringUtils.MakeString(text, Cache.DefaultAnalWs);

		private IPartOfSpeech MakePos(string abbr)
		{
			var pos = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create();
			Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Add(pos);
			pos.Name.set_String(Cache.DefaultAnalWs, MakeAnal(abbr));
			pos.Abbreviation.set_String(Cache.DefaultAnalWs, MakeAnal(abbr));
			return pos;
		}

		// Builds an entry (lexeme form + MSA + sense) of the given slot type and adds a morph bundle to the
		// analysis pointing at its morph / sense / MSA — the shape the legacy MakeBundle test helper produces.
		private IWfiMorphBundle MakeBundle(string form, string gloss, string posAbbr, System.Guid slotType)
		{
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			IMoForm moForm;
			IMoMorphSynAnalysis msa;
			if (slotType == MoMorphTypeTags.kguidMorphStem)
			{
				moForm = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				var stemMsa = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
				entry.MorphoSyntaxAnalysesOC.Add(stemMsa);
				stemMsa.PartOfSpeechRA = MakePos(posAbbr);
				msa = stemMsa;
			}
			else
			{
				moForm = Cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>().Create();
				var affixMsa = Cache.ServiceLocator.GetInstance<IMoInflAffMsaFactory>().Create();
				entry.MorphoSyntaxAnalysesOC.Add(affixMsa);
				affixMsa.PartOfSpeechRA = MakePos(posAbbr);
				msa = affixMsa;
			}
			entry.LexemeFormOA = moForm;
			moForm.Form.set_String(Cache.DefaultVernWs, MakeVern(form));
			moForm.MorphTypeRA = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(slotType);

			var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			entry.SensesOS.Add(sense);
			sense.Gloss.set_String(Cache.DefaultAnalWs, MakeAnal(gloss));
			sense.MorphoSyntaxAnalysisRA = msa;

			var bundle = Cache.ServiceLocator.GetInstance<IWfiMorphBundleFactory>().Create();
			m_analysis.MorphBundlesOS.Add(bundle);
			bundle.MorphRA = moForm;
			bundle.SenseRA = sense;
			bundle.MsaRA = msa;
			return bundle;
		}

		[Test]
		public void ProjectAnalysis_ProducesOneLine_OverTheWordform()
		{
			var model = InterlinearAnalysisProjector.ProjectAnalysis(m_analysis, Cache);

			Assert.That(model.Wordform, Is.EqualTo("kapula"));
			Assert.That(model.WordformGuid, Is.EqualTo(m_wordform.Guid));
			Assert.That(model.HasAnalysis, Is.True);
			Assert.That(model.Lines, Has.Count.EqualTo(1), "the slice projects ONE analysis line per invocation");
			Assert.That(model.Lines[0].AnalysisGuid, Is.EqualTo(m_analysis.Guid));
			Assert.That(model.Lines[0].Wordform, Is.EqualTo("kapula"));
		}

		[Test]
		public void ProjectAnalysis_EachBundle_SurfacesTheInterlinVcLines()
		{
			var line = InterlinearAnalysisProjector.ProjectAnalysis(m_analysis, Cache).Lines[0];

			Assert.That(line.Bundles, Has.Count.EqualTo(2));

			var ka = line.Bundles[0];
			Assert.That(ka.Morph, Is.EqualTo("ka-"),
				"morph form = MorphRA vernacular form WITH its morph-type marker (prefix → trailing '-')");
			Assert.That(ka.Gloss, Is.EqualTo("P"), "lex-gloss = SenseRA.Gloss");
			Assert.That(ka.GrammaticalInfo, Is.EqualTo(m_prefix.MsaRA.InterlinearAbbr),
				"grammatical info = MsaRA.InterlinearAbbr (the LexPos line)");

			var pula = line.Bundles[1];
			Assert.That(pula.Morph, Is.EqualTo("pula"));
			Assert.That(pula.Gloss, Is.EqualTo("rain"));
			Assert.That(pula.GrammaticalInfo, Is.EqualTo(m_stem.MsaRA.InterlinearAbbr));

			// The lex-entry headword line (legacy kflidLexEntries) = the morph's owning entry's HeadWord.
			var stemEntry = (ILexEntry)m_stem.MorphRA.Owner;
			Assert.That(pula.LexEntry, Is.EqualTo(stemEntry.HeadWord.Text),
				"the lex-entry line surfaces the owning entry's headword (distinct from the morpheme form)");
		}

		[Test]
		public void ProjectAnalysis_CarriesTheMorphSenseMsaGuids_ForWriteBack()
		{
			var line = InterlinearAnalysisProjector.ProjectAnalysis(m_analysis, Cache).Lines[0];

			var pula = line.Bundles[1];
			Assert.That(pula.MorphGuid, Is.EqualTo(m_stem.MorphRA.Guid));
			Assert.That(pula.SenseGuid, Is.EqualTo(m_stem.SenseRA.Guid));
			Assert.That(pula.MsaGuid, Is.EqualTo(m_stem.MsaRA.Guid));
		}

		[Test]
		public void ProjectAnalysis_EmptyAnalysis_IsBareWordform()
		{
			IWfiAnalysis empty = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				empty = Cache.ServiceLocator.GetInstance<IWfiAnalysisFactory>().Create();
				m_wordform.AnalysesOC.Add(empty);
			});

			var model = InterlinearAnalysisProjector.ProjectAnalysis(empty, Cache);
			Assert.That(model.Wordform, Is.EqualTo("kapula"));
			Assert.That(model.HasAnalysis, Is.False, "an analysis with no bundles renders the bare-wordform state");
			Assert.That(model.Lines[0].Bundles, Is.Empty);
		}
	}
}

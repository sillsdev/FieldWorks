using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Rhino.Mocks;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.FDO.FDOTests
{
	class FdoFactoryAdditionsTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		/// <summary>
		/// Helper
		/// </summary>
		private LexEntryComponents SetupComponentsForEntryCreation(out int germanWsId,
			out ILexEntryFactory lexFactory, out ITsString tssGermanGloss)
		{
			germanWsId = Cache.WritingSystemFactory.GetWsFromStr("de");
			Cache.LangProject.AnalysisWritingSystems.Add(
				Cache.WritingSystemFactory.get_EngineOrNull(germanWsId) as IWritingSystem);
			lexFactory = Cache.ServiceLocator.GetInstance<ILexEntryFactory>();
			tssGermanGloss = Cache.TsStrFactory.MakeString("da", germanWsId);
			var tssVernacForm = Cache.TsStrFactory.MakeString("bunk",
				Cache.WritingSystemFactory.GetWsFromStr(Cache.LangProject.DefaultVernacularWritingSystem.Id));
			var msa = new SandboxGenericMSA
			{
				MainPOS = Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Where(
					pos => pos.Name.AnalysisDefaultWritingSystem.Text == "noun")
					.Cast<IPartOfSpeech>()
					.FirstOrDefault(),
				MsaType = MsaType.kStem
			};

			var lexEntryComponents = new LexEntryComponents()
			{
				GlossAlternatives = new List<ITsString>() { tssGermanGloss },
				LexemeFormAlternatives = new List<ITsString>() { tssVernacForm },
				MSA = msa,
				MorphType = Cache.LangProject.LexDbOA.MorphTypesOA.PossibilitiesOS.Where(
					mt => mt.Name.AnalysisDefaultWritingSystem.Text == "stem")
					.Cast<IMoMorphType>()
					.FirstOrDefault()
			};
			return lexEntryComponents;
		}

		[Test]
		public void EntryCreatedWithNonDefaultAnalysisGlossDoesNotFillInDefaultAnalysisGloss()
		{
			int germanWsId;
			ILexEntryFactory lexFactory;
			ITsString tssGermanGloss;
			var lexEntryComponents = SetupComponentsForEntryCreation(out germanWsId, out lexFactory, out tssGermanGloss);

			// SUT
			var lexentry = lexFactory.Create(lexEntryComponents);

			Assert.AreEqual(1, lexentry.SensesOS[0].Gloss.StringCount, "The gloss should have exactly one string, the entry for german");
			Assert.AreEqual(lexentry.SensesOS[0].Gloss.StringOrNull(germanWsId).Text, "da", "The german gloss should contain the string 'da'");
		}

		[Test]
		public void Create_AppliesWSFromGlossTsString()
		{
			int germanWsId;
			ILexEntryFactory lexFactory;
			ITsString tssGermanGloss;
			var lexEntryComponents = SetupComponentsForEntryCreation(out germanWsId, out lexFactory, out tssGermanGloss);

			// SUT
			ILexEntry newEntry = lexFactory.Create(lexEntryComponents.MorphType,
				lexEntryComponents.LexemeFormAlternatives[0],
				tssGermanGloss,
				lexEntryComponents.MSA);

			Assert.That(newEntry.SensesOS[0].Gloss.get_String(germanWsId).Text, Is.EqualTo("da"), "Expected gloss using required WS not found");
			Assert.That(newEntry.SensesOS[0].Gloss.StringCount, Is.EqualTo(1), "Unexpected extra glosses in sense");
		}

		private void SetupForLexSenseFactoryCreate(out int germanWsId, out ITsString tssGermanGloss,
			out ILexEntry entry, out ILexSenseFactory lexSenseFactory, out SandboxGenericMSA msa)
		{
			germanWsId = Cache.WritingSystemFactory.GetWsFromStr("de");
			Cache.LangProject.AnalysisWritingSystems.Add(
				Cache.WritingSystemFactory.get_EngineOrNull(germanWsId) as IWritingSystem);
			var lexFactory = Cache.ServiceLocator.GetInstance<ILexEntryFactory>();
			tssGermanGloss = Cache.TsStrFactory.MakeString("da", germanWsId);
			entry = lexFactory.Create();
			lexSenseFactory = Cache.ServiceLocator.GetInstance<ILexSenseFactory>();
			msa = new SandboxGenericMSA
			{
				MainPOS =
					Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Where(
						pos => pos.Name.AnalysisDefaultWritingSystem.Text == "noun")
						.Cast<IPartOfSpeech>()
						.FirstOrDefault(),
				MsaType = MsaType.kStem
			};
		}

		[Test]
		public void LexSenseFactoryCreate_UsesWsFromGlossTss()
		{
			int germanWsId;
			ITsString tssGermanGloss;
			ILexEntry entry;
			ILexSenseFactory lexSenseFactory;
			SandboxGenericMSA msa;
			SetupForLexSenseFactoryCreate(out germanWsId, out tssGermanGloss, out entry, out lexSenseFactory, out msa);

			var sense = lexSenseFactory.Create(entry, msa, tssGermanGloss);
			Assert.AreEqual(sense.Gloss.get_String(germanWsId).Text, "da");
			Assert.AreEqual(1, sense.Gloss.StringCount);
		}

		[Test]
		public void LexSenseFactoryCreate_NullGlossTss_DoesNotThrow()
		{
			int germanWsId;
			ITsString tssGermanGloss;
			ILexEntry entry;
			ILexSenseFactory lexSenseFactory;
			SandboxGenericMSA msa;
			SetupForLexSenseFactoryCreate(out germanWsId, out tssGermanGloss, out entry, out lexSenseFactory, out msa);

			string nullGloss = null;
			ILexSense sense = null;
			Assert.DoesNotThrow(() => sense = lexSenseFactory.Create(entry, msa, nullGloss));
			Assert.AreEqual(0, sense.Gloss.StringCount);
		}
	}
}

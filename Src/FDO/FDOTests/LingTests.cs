// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2002, SIL International. All Rights Reserved.
// <copyright from='2002' to='2002' company='SIL International'>
//		Copyright (c) 2002, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: LingTests.cs
// Responsibility: John Hatton
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.CoreTests;
using SIL.FieldWorks.FDO.DomainImpl;
using SIL.FieldWorks.FDO.FDOTests.CellarTests;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FDO.Validation;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.CoreImpl;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.FDOTests.LingTests
{
	/// <summary>
	/// Test stuff related to morphology.
	/// </summary>
	[TestFixture]
	public class MorphologyTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		/// <summary>
		/// What it says.
		/// </summary>
		[Test]
		public void InflAffixTemplate_ReferenceTargetCandidates()
		{
			var services = Cache.ServiceLocator;
			// We need a PartOfSpeech owning another POS owning an MoInflAffixTemplate. Set up the required owners.
			if (Cache.LangProject.PartsOfSpeechOA == null)
				Cache.LangProject.PartsOfSpeechOA = services.GetInstance<ICmPossibilityListFactory>().Create();
			var verb = services.GetInstance<IPartOfSpeechFactory>().Create();
			Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Add(verb);
			var transverb = services.GetInstance<IPartOfSpeechFactory>().Create();
			verb.SubPossibilitiesOS.Add(transverb);
			var template = services.GetInstance<IMoInflAffixTemplateFactory>().Create();
			transverb.AffixTemplatesOS.Add(template);

			// We need affix slots on both templates: at least one that looks like a prefix, one that looks like a
			// suffix, and one we can't tell about. A slot with no affixes is ambiguous; to be unambiguous,
			// it has to have an affix that belongs to a LexEntry of an appropriate type.
			var prefixV = MakeSlot(services, verb.AffixSlotsOC, MoMorphTypeTags.kguidMorphPrefix);
			var suffixV = MakeSlot(services, verb.AffixSlotsOC, MoMorphTypeTags.kguidMorphSuffix);
			var prefixT = MakeSlot(services, verb.AffixSlotsOC, MoMorphTypeTags.kguidMorphPrefix);
			var suffixT = MakeSlot(services, verb.AffixSlotsOC, MoMorphTypeTags.kguidMorphSuffix);
			var ambigV = services.GetInstance<IMoInflAffixSlotFactory>().Create();
			verb.AffixSlotsOC.Add(ambigV);

			var prefixSlots = new HashSet<ICmObject>(template.ReferenceTargetCandidates(MoInflAffixTemplateTags.kflidPrefixSlots));
			Assert.IsTrue(prefixSlots.Contains(prefixV));
			Assert.IsTrue(prefixSlots.Contains(prefixT));
			Assert.IsTrue(prefixSlots.Contains(ambigV));
			Assert.IsFalse(prefixSlots.Contains(suffixV));
			Assert.IsFalse(prefixSlots.Contains(suffixT));

			var suffixSlots = new HashSet<ICmObject>(template.ReferenceTargetCandidates(MoInflAffixTemplateTags.kflidSuffixSlots));
			Assert.IsFalse(suffixSlots.Contains(prefixV));
			Assert.IsFalse(suffixSlots.Contains(prefixT));
			Assert.IsTrue(suffixSlots.Contains(ambigV));
			Assert.IsTrue(suffixSlots.Contains(suffixV));
			Assert.IsTrue(suffixSlots.Contains(suffixT));

			var allSlots = new HashSet<ICmObject>(template.ReferenceTargetCandidates(MoInflAffixTemplateTags.kflidSlots));
			Assert.IsTrue(allSlots.Contains(prefixV));
			Assert.IsTrue(allSlots.Contains(prefixT));
			Assert.IsTrue(allSlots.Contains(ambigV));
			Assert.IsTrue(allSlots.Contains(suffixV));
			Assert.IsTrue(allSlots.Contains(suffixT));
		}

		/// <summary>
		/// Make a slot which can be identified as the specified type.
		/// For this to be true, the slot must have an msa, owned by a LexEntry, which owns a form, which has
		/// the required type.
		/// </summary>
		IMoInflAffixSlot MakeSlot(IFdoServiceLocator services, IFdoOwningCollection<IMoInflAffixSlot> dest, Guid slotType)
		{
			var entry = services.GetInstance<ILexEntryFactory>().Create();
			var form = services.GetInstance<IMoAffixAllomorphFactory>().Create();
			entry.LexemeFormOA = form;
			form.MorphTypeRA = services.GetInstance<IMoMorphTypeRepository>().GetObject(slotType);
			var msa = services.GetInstance<IMoInflAffMsaFactory>().Create();
			entry.MorphoSyntaxAnalysesOC.Add(msa);
			var slot = services.GetInstance<IMoInflAffixSlotFactory>().Create();
			dest.Add(slot);
			msa.SlotsRC.Add(slot);
			// slot.Affixes.Add(msa); does not add it!
			return slot;
		}

		/// <summary>
		/// Verify that a new MoAffixProcess is properly initialized. This is needed at least for the Affix Process slice
		/// to work properly. See FWR-1619.
		/// </summary>
		[Test]
		public void NewMoAffixProcessHasInputAndOutput()
		{
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			var affixProcess = Cache.ServiceLocator.GetInstance<IMoAffixProcessFactory>().Create();
			entry.LexemeFormOA = affixProcess;
			Assert.That(affixProcess.InputOS, Has.Count.EqualTo(1));
			Assert.That(affixProcess.OutputOS, Has.Count.EqualTo(1));
			Assert.That(affixProcess.InputOS[0], Is.TypeOf(typeof(PhVariable)));
			Assert.That(affixProcess.OutputOS[0], Is.TypeOf(typeof(MoCopyFromInput)));
		}

		/// <summary>
		/// Trivial test that a newly created InflAffixTemplate has the Final property set to true.
		/// </summary>
		[Test]
		public void NewInflectionalTemplateHasFinalTrue()
		{
			if (Cache.LangProject.PartsOfSpeechOA == null)
				Cache.LangProject.PartsOfSpeechOA = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
			var pos = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create();
			Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Add(pos);
			var template = Cache.ServiceLocator.GetInstance<MoInflAffixTemplateFactory>().Create();
			pos.AffixTemplatesOS.Add(template);
			Assert.That(template.Final, Is.True);
		}

		/// <summary>
		/// Confirm that a new MoStemName is created with on FsFeatStruc in its Regions.
		/// </summary>
		[Test]
		public void NewStemNameHasFeatures()
		{
			if (Cache.LangProject.PartsOfSpeechOA == null)
				Cache.LangProject.PartsOfSpeechOA = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
			var pos = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create();
			Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Add(pos);
			var msn = Cache.ServiceLocator.GetInstance<IMoStemNameFactory>().Create();
			pos.StemNamesOC.Add(msn);
			Assert.That(msn.RegionsOC, Has.Count.EqualTo(1)); // can only be FsFeatStruc, there are no subclasses.
		}
	}



	#region LingTests

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// General tests in the Linguistics model area, including those that used to use a real
	/// database with SQL access.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class LingTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		#region Tests
				/// <summary>
		/// Check to see whether homograph validation works.
		/// </summary>
		[Test]
		public void HomographValidationWorks()
		{
			string sLexForm = "unitTestLexemeForm";
			var lme = MakeEntry(sLexForm);
			string homographForm = lme.HomographForm;
			Assert.AreEqual(sLexForm, homographForm, "lexeme form and homograph form are not the same.");

			lme.HomographNumber = 1;
			bool fOk = LexDb.CorrectHomographNumbers(new List<ILexEntry>(new [] {lme}));
			Assert.IsTrue(fOk, "CorrectHomographNumbers should renumber single item to 0.");
			Assert.That(lme.HomographNumber, Is.EqualTo(0));

			// Make sure it has 2 homographs.
			var lme2 = MakeEntry(sLexForm);
			var lme3 = MakeEntry(sLexForm);

			// This version of the CollectHomographs will not include the lme entry.
			List<ILexEntry> rgHomographs = ((LexEntry)lme).CollectHomographs();
			Assert.AreEqual(rgHomographs.Count, 2, "Wrong homograph count.");

			// Reset all the homograph numbers to zero.
			foreach (ILexEntry le in rgHomographs)
			{
				le.HomographNumber = 0;
			}

			// Restore valid homograph numbers by calling ValidateHomographNumbers.
			fOk = LexDb.CorrectHomographNumbers(rgHomographs);
			Assert.IsFalse(fOk, "CorrectHomographNumbers had to renumber homographs");
			int n = 1;
			foreach (ILexEntry le in rgHomographs)
			{
				Assert.AreEqual(n++, le.HomographNumber, "Wrong homograph number found.");
			}

			// If we get here without asserting, the renumbering worked okay.
			fOk = LexDb.CorrectHomographNumbers(rgHomographs);
			Assert.IsTrue(fOk, "CorrectHomographNumbers should not have to renumber this time.");

			// Reset all the homograph numbers by multiplying each by 2.
			foreach (ILexEntry le in rgHomographs)
			{
				le.HomographNumber *= 2;
			}

			// Restore valid homograph numbers by calling ValidateHomographNumbers.
			fOk = LexDb.CorrectHomographNumbers(rgHomographs);
			Assert.IsFalse(fOk, "CorrectHomographNumbers had to renumber homographs");
			n = 1;
			foreach (ILexEntry le in rgHomographs)
			{
				Assert.AreEqual(n++, le.HomographNumber, "Wrong homograph number found.");
			}
		}

		private ILexEntry MakeEntry(string sLexForm)
		{
			var lme = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			lme.LexemeFormOA = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			lme.LexemeFormOA.Form.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString(sLexForm, Cache.DefaultVernWs);
			return lme;
		}

		/// <summary>
		/// Test adding wordform with special characters
		/// </summary>
		[Test]
		public void AddWordformSpecialCharacter()
		{
			char kSpecialCharacter = '\x014b';		// ŋ
			// Make a wordform which has a random number in it in order to reduce the chance to the word is already loaded.
			string kWordForm = "aaa" + kSpecialCharacter;
			IWfiWordform word = Cache.ServiceLocator.GetInstance<IWfiWordformFactory>().Create();
			word.Form.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString(kWordForm, Cache.DefaultVernWs);
			Assert.IsTrue(word.Hvo != 0, "Adding word failed, gave hvo = 0");

			int checkIndex = kWordForm.Length - 1;
			Assert.AreEqual(kSpecialCharacter, word.Form.VernacularDefaultWritingSystem.Text[checkIndex],
				"Special character was not handled correctly.");
		}

		/// <summary>
		/// Test the ParseCount method.
		/// </summary>
		[Test]
		public void CheckAgentCounts()
		{
			string kWordForm = "aaa";
			IWfiWordform word = Cache.ServiceLocator.GetInstance<IWfiWordformFactory>().Create();
			word.Form.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString(kWordForm, Cache.DefaultVernWs);
			Assert.IsTrue(word.Hvo != 0, "Adding word failed, gave hvo = 0");
			IWfiWordform wf = null;
			foreach (var x in Cache.ServiceLocator.GetInstance<IWfiWordformRepository>().AllInstances())
			{
				wf = x;
				break;
			}
			Assert.IsNotNull(wf, "Retrieving wordform failed.");
			Assert.AreEqual(word, wf, "Only one wordform -- the one we created -- exists.");
			int cOldUser = wf.UserCount;
			int cOldParse = wf.ParserCount;
			IWfiAnalysis wa1 = Cache.ServiceLocator.GetInstance<IWfiAnalysisFactory>().Create();
			wf.AnalysesOC.Add(wa1);
			IWfiAnalysis wa2 = Cache.ServiceLocator.GetInstance<IWfiAnalysisFactory>().Create();
			wf.AnalysesOC.Add(wa2);
			ICmAgent ca = Cache.LangProject.DefaultParserAgent;
			ca.SetEvaluation(wa1, Opinions.approves);
			ca = Cache.LangProject.DefaultUserAgent;
			ca.SetEvaluation(wa2, Opinions.approves);
			int cNewUser = wf.UserCount;
			int cNewParse = wf.ParserCount;
			Assert.IsTrue(cOldUser + 1 == cNewUser, "UserCount wrong: Expected " +
				cNewUser + " but got " + (cOldUser + 1));
			Assert.IsTrue(cOldParse + 1 == cNewParse, "ParserCount wrong: Expected " +
				cNewParse + " but got " + (cOldParse + 1));
		}

		/// <summary>
		/// Test the FeaturesTSS virtual property.
		/// </summary>
		[Test]
		public void TestFeaturesTss()
		{
			ILexEntry le = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			IMoStemMsa msa = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
			le.MorphoSyntaxAnalysesOC.Add(msa);
			Assert.That(msa.FeaturesTSS.Length, Is.EqualTo(0),
				"1 with no features, FeaturesTSS should produce an empty string");

			var featStruc = Cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();
			msa.MsFeaturesOA = featStruc;
			var featSpec = Cache.ServiceLocator.GetInstance<IFsClosedValueFactory>().Create();
			featStruc.FeatureSpecsOC.Add(featSpec);
			Assert.That(msa.FeaturesTSS.Text, Is.EqualTo(featSpec.ShortNameTSS.Text),
				"1 with features, FeaturesTSS should not be an empty string");

			//Test for MoDerivAffMsa
			IMoDerivAffMsa mdam = Cache.ServiceLocator.GetInstance<IMoDerivAffMsaFactory>().Create();
			le.MorphoSyntaxAnalysesOC.Add(mdam);
			Assert.That(mdam.FeaturesTSS.Length, Is.EqualTo(0),
				"2 with no features, FeaturesTSS should produce an empty string");

			ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
			var featStruc2 = Cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();
			mdam.ToMsFeaturesOA = featStruc2;
			var featSpec2 = Cache.ServiceLocator.GetInstance<IFsClosedValueFactory>().Create();
			featStruc2.FeatureSpecsOC.Add(featSpec2);
			var featStruc3 = Cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();
			mdam.FromMsFeaturesOA = featStruc3;
			var featSpec3 = Cache.ServiceLocator.GetInstance<IFsClosedValueFactory>().Create();
			featStruc3.FeatureSpecsOC.Add(featSpec3);
			tisb.AppendTsString(featStruc3.ShortNameTSS);
			tisb.AppendTsString(Cache.MakeUserTss(" > "));
			tisb.AppendTsString(featStruc2.ShortNameTSS);
			Assert.That(mdam.FeaturesTSS.Text, Is.EqualTo(tisb.Text),
				"2 with two features separated by >, FeaturesTSS should not be an empty string");

			//Test for MoInflAffMsa
			IMoInflAffMsa miam = Cache.ServiceLocator.GetInstance<IMoInflAffMsaFactory>().Create();
			le.MorphoSyntaxAnalysesOC.Add(miam);
			Assert.That(miam.FeaturesTSS.Length, Is.EqualTo(0),
				"3 with no features, FeaturesTSS should produce an empty string");

			var featStruc4 = Cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();
			miam.InflFeatsOA = featStruc4;
			var featSpec4 = Cache.ServiceLocator.GetInstance<IFsClosedValueFactory>().Create();
			featStruc4.FeatureSpecsOC.Add(featSpec4);
			Assert.That(miam.FeaturesTSS.Text, Is.EqualTo(featSpec4.ShortNameTSS.Text),
				"3 with features. FeaturesTSS should not be an empty string");

			//Test for MoDerivStepMsa
			IMoDerivStepMsa mdsm = Cache.ServiceLocator.GetInstance<IMoDerivStepMsaFactory>().Create();
			le.MorphoSyntaxAnalysesOC.Add(mdsm);
			Assert.That(mdsm.FeaturesTSS.Length, Is.EqualTo(0),
				"4 with no features, FeaturesTSS should produce an empty string");

			var featStruc5 = Cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();
			mdsm.MsFeaturesOA = featStruc5;
			var featSpec5 = Cache.ServiceLocator.GetInstance<IFsClosedValueFactory>().Create();
			featStruc5.FeatureSpecsOC.Add(featSpec5);
			Assert.That(mdsm.FeaturesTSS.Text, Is.EqualTo(featSpec5.ShortNameTSS.Text),
				"4 with one features. FeaturesTSS should not be an empty string");

			var featStruc6 = Cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();
			mdsm.InflFeatsOA = featStruc6;
			var featSpec6 = Cache.ServiceLocator.GetInstance<IFsClosedValueFactory>().Create();
			featStruc6.FeatureSpecsOC.Add(featSpec6);
			ITsIncStrBldr tisc = TsIncStrBldrClass.Create();
			tisc.AppendTsString(featStruc5.ShortNameTSS);
			tisc.AppendTsString(Cache.MakeUserTss(" / "));
			tisc.AppendTsString(featStruc6.ShortNameTSS);
			Assert.That(mdsm.FeaturesTSS.Text, Is.EqualTo(tisc.Text),
				"4 with two features separated by /, FeaturesTSS should not be an empty string");

			mdsm.MsFeaturesOA = null;
			Assert.That(mdsm.FeaturesTSS.Text, Is.EqualTo(featSpec6.ShortNameTSS.Text),
				"4 with one features (MsInflFeatsOA). FeaturesTSS should not be an empty string");		}

		/// <summary>
		/// Test the ExceptionFeaturesTSS virtual property.
		/// </summary>
		[Test]
		public void TestExceptionFeaturesTss()
		{
			//Test for MoStemMsa
			ILexEntry le = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			IMoStemMsa msm = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
			le.MorphoSyntaxAnalysesOC.Add(msm);
			Assert.That(msm.ExceptionFeaturesTSS.Length, Is.EqualTo(0),
				"1 with no features, ExceptionFeaturesTSS should produce an empty string");

			var restrictionsList = Cache.LangProject.RestrictionsOA;
			if (restrictionsList == null)
			{
				restrictionsList = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
			}
			var cmPoss = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
			restrictionsList.PossibilitiesOS.Add(cmPoss);
			msm.ProdRestrictRC.Add(cmPoss);
			ITsIncStrBldr tisc = TsIncStrBldrClass.Create();
			tisc.AppendTsString(cmPoss.Abbreviation.BestAnalysisVernacularAlternative);
			Assert.That(msm.ExceptionFeaturesTSS.Text, Is.EqualTo(tisc.Text),
				"1 with prodRestrict, ExceptionFeaturesTSS should not be an empty string");

			//Test for MoDerivAffMsa
			IMoDerivAffMsa mdam = Cache.ServiceLocator.GetInstance<IMoDerivAffMsaFactory>().Create();
			le.MorphoSyntaxAnalysesOC.Add(mdam);
			ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
			tisb.AppendTsString(Cache.MakeUserTss(""));
			Assert.That(mdam.ExceptionFeaturesTSS.Length, Is.EqualTo(0),
				"2 with no prodRestrict, ExceptionFeaturesTSS should produce an empty string");

			var cmPoss2 = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
			restrictionsList.PossibilitiesOS.Add(cmPoss2);
			mdam.FromProdRestrictRC.Add(cmPoss2);
			tisb.AppendTsString(cmPoss2.Abbreviation.BestAnalysisVernacularAlternative);
			tisb.AppendTsString(Cache.MakeUserTss(String.Format(" > {0}", Strings.ksQuestions)));
			Assert.That(mdam.ExceptionFeaturesTSS.Text, Is.EqualTo(tisb.Text),
				"2 with FromProdRestrict, ExceptionFeaturesTSS should not be an empty string");

			var cmPoss3 = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
			restrictionsList.PossibilitiesOS.Add(cmPoss3);
			mdam.ToProdRestrictRC.Add(cmPoss3);
			tisb.Clear();
			tisb.AppendTsString(cmPoss2.Abbreviation.BestAnalysisVernacularAlternative);
			tisb.AppendTsString(Cache.MakeUserTss(" > "));
			tisb.AppendTsString(cmPoss3.Abbreviation.BestAnalysisVernacularAlternative);
			Assert.That(mdam.ExceptionFeaturesTSS.Text, Is.EqualTo(tisb.Text),
				"2 with ToProdRestrict and FromProdRestrict, ExceptionFeaturesTSS should not be an empty string");

			mdam.FromProdRestrictRC.Remove(cmPoss2);
			tisb.Clear();
			tisb.AppendTsString(Cache.MakeUserTss(Strings.ksQuestions));
			tisb.AppendTsString(Cache.MakeUserTss(" > "));
			tisb.AppendTsString(cmPoss3.Abbreviation.BestAnalysisVernacularAlternative);
			Assert.That(mdam.ExceptionFeaturesTSS.Text, Is.EqualTo(tisb.Text),
				"2 with ToProdRestrict only, ExceptionFeaturesTSS should not be an empty string");

			//*Test for MoInflAffMsa
			IMoInflAffMsa miam = Cache.ServiceLocator.GetInstance<IMoInflAffMsaFactory>().Create();
			le.MorphoSyntaxAnalysesOC.Add(miam);
			Assert.That(miam.ExceptionFeaturesTSS.Length, Is.EqualTo(0),
				"3 with no features, ExceptionFeaturesTSS should produce an empty string");

			var cmPoss4 = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
			restrictionsList.PossibilitiesOS.Add(cmPoss4);
			miam.FromProdRestrictRC.Add(cmPoss3);
			tisb.Clear();
			tisb.AppendTsString(cmPoss4.Abbreviation.BestAnalysisVernacularAlternative);
			Assert.That(miam.ExceptionFeaturesTSS.Text, Is.EqualTo(tisb.Text),
				"3 with FromProdRestrict. ExceptionFeaturesTSS should not be an empty string");

			//Test for MoDerivStepMsa
			IMoDerivStepMsa mdsm = Cache.ServiceLocator.GetInstance<IMoDerivStepMsaFactory>().Create();
			le.MorphoSyntaxAnalysesOC.Add(mdsm);
			Assert.That(mdsm.ExceptionFeaturesTSS.Length, Is.EqualTo(0),
				"4 with no features, ExceptionFeaturesTSS should produce an empty string");

			var cmPoss5 = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
			restrictionsList.PossibilitiesOS.Add(cmPoss5);
			mdsm.ProdRestrictRC.Add(cmPoss5);
			tisb.Clear();
			tisb.AppendTsString(cmPoss5.Abbreviation.BestAnalysisVernacularAlternative);
			Assert.That(mdsm.ExceptionFeaturesTSS.Text, Is.EqualTo(tisb.Text),
				"4 with ProdRestrict. ExceptionFeaturesTSS should not be an empty string");
		}

		/// <summary>
		/// Test the NumberOfEntries methods.
		/// </summary>
		[Test]
		public void CheckNumberOfEntries()
		{
			ILexEntry le = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			IPartOfSpeech pos = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create();
			Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Add(pos);
			ILexEntry lme = null;
			foreach (var x in Cache.LangProject.LexDbOA.Entries)
			{
				lme = x;
				break;
			}
			Assert.IsNotNull(lme, "failed to retrieve ILexEntry object");
			Assert.AreEqual(le, lme, "should be only one ILexEntry -- the one we just created");
			int cRef1 = pos.NumberOfLexEntries;
			IMoStemMsa msm = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
			lme.MorphoSyntaxAnalysesOC.Add(msm);
			msm.PartOfSpeechRA = pos;
			int cRef2 = pos.NumberOfLexEntries;
			Assert.IsTrue(cRef1 + 1 == cRef2, "NumberOfLexEntries for MoStemMsa was " +
				cRef2 + " but should have been" + (cRef1 + 1));

			IMoMorphType mmt = null;
			foreach (var x in Cache.LangProject.LexDbOA.MorphTypesOA.PossibilitiesOS)
			{
				mmt = x as IMoMorphType;
				break;
			}
			Assert.IsNotNull(mmt, "failed to retrieve IMoMorphType object");
			cRef1 = mmt.NumberOfLexEntries;
			IMoStemAllomorph msa = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			lme.AlternateFormsOS.Add(msa);
			msa.MorphTypeRA = mmt;
			cRef2 = mmt.NumberOfLexEntries;
			Assert.IsTrue(cRef1 + 1 == cRef2, "NumberOfLexEntries for MoMorphType was " +
				cRef2 + " but should have been" + (cRef1 + 1));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// test the static method which retrieves the set of abbreviations for PhNaturalClasses
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetNaturalClassAbbreviations()
		{
			string[] sa = Cache.LangProject.PhonologicalDataOA.AllNaturalClassAbbrs().ToArray();
			Assert.IsTrue(sa.Length == 0, "Expect no natural classes");

			IPhNCSegments c1 = Cache.ServiceLocator.GetInstance<IPhNCSegmentsFactory>().Create();
			Cache.LangProject.PhonologicalDataOA.NaturalClassesOS.Add(c1);
			c1.Abbreviation.AnalysisDefaultWritingSystem = Cache.TsStrFactory.MakeString("a", Cache.DefaultAnalWs);
			IPhNCSegments c2 = Cache.ServiceLocator.GetInstance<IPhNCSegmentsFactory>().Create();
			Cache.LangProject.PhonologicalDataOA.NaturalClassesOS.Add(c2);
			c2.Abbreviation.AnalysisDefaultWritingSystem = Cache.TsStrFactory.MakeString("b", Cache.DefaultAnalWs);
			IPhNCFeatures c3 = Cache.ServiceLocator.GetInstance<IPhNCFeaturesFactory>().Create();
			Cache.LangProject.PhonologicalDataOA.NaturalClassesOS.Add(c3);
			c3.Abbreviation.AnalysisDefaultWritingSystem = Cache.TsStrFactory.MakeString("c", Cache.DefaultAnalWs);

			string[] sa2 = Cache.LangProject.PhonologicalDataOA.AllNaturalClassAbbrs().ToArray();
			Assert.IsTrue(sa2.Length == 3, "Expect three abbreviations in the set of natural classes");
		}

		/// <summary>
		/// Check to see whether homograph collection works.
		/// This needs a real database because it uses SQL to get a base list of entries
		/// to compare.
		/// </summary>
		[Test]
		public void HomographCollectionWorks()
		{
			IMoMorphType mmtStem;
			IMoMorphType mmtPrefix;
			IMoMorphType mmtSuffix;
			IMoMorphType mmtInfix;
			IMoMorphType mmtBoundStem;
			IMoMorphType mmtProclitic;
			IMoMorphType mmtEnclitic;
			IMoMorphType mmtSimulfix;
			IMoMorphType mmtSuprafix;
			Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetMajorMorphTypes(
				out mmtStem, out mmtPrefix, out mmtSuffix, out mmtInfix, out mmtBoundStem, out mmtProclitic,
				out mmtEnclitic, out mmtSimulfix, out mmtSuprafix);

			ILexEntry lme;
			string sLexForm = CreateThreeStemHomographs(mmtStem, out lme);
			ILexEntry lme4 = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			lme4.LexemeFormOA = Cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>().Create();
			lme4.LexemeFormOA.Form.set_String(Cache.DefaultVernWs, sLexForm);
			lme4.LexemeFormOA.MorphTypeRA = mmtSuffix;

			string homographForm = lme.HomographForm;
			Assert.AreEqual(sLexForm, homographForm, "Lexeme form and homograph form are not the same.");

			// These two tests check lexeme forms of the three/four entries.
			// This version of the CollectHomographs method includes all the entries.
			List<ILexEntry> rgHomographs = Cache.ServiceLocator.GetInstance<ILexEntryRepository>().GetHomographs(sLexForm);
			Assert.AreEqual(rgHomographs.Count, 4, "Wrong homograph count.");

			// This version of the CollectHomographs method will not include the lme4 entry.
			rgHomographs = Cache.ServiceLocator.GetInstance<ILexEntryRepository>().CollectHomographs(sLexForm, mmtStem);
			Assert.AreEqual(rgHomographs.Count, 3, "Wrong homograph count.");

			// This version of the CollectHomographs method will include only the lme4 entry.
			rgHomographs = Cache.ServiceLocator.GetInstance<ILexEntryRepository>().CollectHomographs(sLexForm, mmtSuffix);
			Assert.AreEqual(rgHomographs.Count, 1, "Wrong homograph count.");

			// Now set the citation form to something different than sLexForm.
			string sCitationForm = "unitTestCitationForm";
			lme.CitationForm.set_String(Cache.DefaultVernWs, sCitationForm);
			homographForm = lme.HomographForm;
			Assert.AreEqual(sCitationForm, homographForm, "Citation form and homograph form are not the same.");

			// This version of the CollectHomographs method will not include the lme4 entry.
			rgHomographs = Cache.ServiceLocator.GetInstance<ILexEntryRepository>().CollectHomographs(sLexForm, mmtStem);
			Assert.AreEqual(rgHomographs.Count, 2, "Wrong homograph count.");

			// This version of the CollectHomographs method will include the lme4 entry.
			rgHomographs = Cache.ServiceLocator.GetInstance<ILexEntryRepository>().GetHomographs(sLexForm);
			Assert.AreEqual(rgHomographs.Count, 3, "Wrong homograph count.");
		}

		private string CreateThreeStemHomographs(IMoMorphType mmtStem, out ILexEntry lme)
		{
			lme = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			string sLexForm = "unitTestLexemeForm";
			lme.LexemeFormOA = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			lme.LexemeFormOA.Form.set_String(Cache.DefaultVernWs, sLexForm);
			lme.LexemeFormOA.MorphTypeRA = mmtStem;

			// Make sure it has 3 other homographs (2 of the same morphtype).
			ILexEntry lme2 = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			lme2.LexemeFormOA = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			lme2.LexemeFormOA.Form.set_String(Cache.DefaultVernWs, sLexForm);
			lme2.LexemeFormOA.MorphTypeRA = mmtStem;

			ILexEntry lme3 = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			lme3.LexemeFormOA = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			lme3.LexemeFormOA.Form.set_String(Cache.DefaultVernWs, sLexForm);
			lme3.LexemeFormOA.MorphTypeRA = mmtStem;

			return sLexForm;
		}

		/// <summary>
		/// </summary>
		[Test]
		public void CheckEndoCentricCompound()
		{
			IMoEndoCompound cmp = Cache.ServiceLocator.GetInstance<IMoEndoCompoundFactory>().Create();
			Cache.LangProject.MorphologicalDataOA.CompoundRulesOS.Add(cmp);
			Assert.IsNotNull(cmp.LeftMsaOA);
			Assert.IsNotNull(cmp.RightMsaOA);
		}

		/// <summary>
		/// </summary>
		[Test]
		public void CheckEnvironmentMessages()
		{
			string[] saSegments = { "a", "ai", "b", "c", "d", "e", "f", "fl", "fr",
									"í",  // single combined Unicode acute i
									"H"
								};
			string[] saNaturalClasses = { "V", "Vowels", "C", "+son", "+lab, +vd", "+ant, -cor, -vd" };

			PhonEnvRecognizer rec = new PhonEnvRecognizer(saSegments, saNaturalClasses);
			string strRep;
			strRep = "/ _ q";
			CheckEnvironmentMessage(rec, strRep, "There is a problem with this environment string '/ _ q': The phoneme which begins 'q' was not found in the set of representations for any Phoneme.");
			strRep = "/ _ aqa";
			CheckEnvironmentMessage(rec, strRep, "There is a problem with this environment string '/ _ aqa': The phoneme which begins 'qa' was not found in the set of representations for any Phoneme.");
			strRep = "/ _ [COP]";
			CheckEnvironmentMessage(rec, strRep, "There is a problem with this environment string '/ _ [COP]': The abbreviation for the class 'COP' was not found in the set of Natural Classes.");
			strRep = "/ [C][V] _ [V][COP]";
			CheckEnvironmentMessage(rec, strRep, "There is a problem with this environment string '/ [C][V] _ [V][COP]': The abbreviation for the class 'COP' was not found in the set of Natural Classes.");
			strRep = "/ [C _";
			CheckEnvironmentMessage(rec, strRep, "There is a problem with this environment string '/ [C _': There is a missing closing square bracket ']' somewhere around here: '_'.");
			strRep = "/ C] _";
			CheckEnvironmentMessage(rec, strRep, "There is a problem with this environment string '/ C] _': There is a missing opening square bracket '[' somewhere around here: 'C] _'.");
			strRep = "/ (a _";
			CheckEnvironmentMessage(rec, strRep, "There is a problem with this environment string '/ (a _': There is a missing closing parenthesis ')' somewhere around here: '_'.");
			strRep = "/ a) _";
			CheckEnvironmentMessage(rec, strRep, "There is a problem with this environment string '/ a) _': There is a missing opening parenthesis '(' somewhere around here: 'a) _'.");
			strRep = " a _";
			CheckEnvironmentMessage(rec, strRep, "There is a problem with this environment string ' a _': There is some kind of error somewhere around here: ' _'.  It may be a missing underscore _, two or more underscores, a missing slash /, something beyond a word boundary symbol #, or two or more word boundary symbols at the same edge.");
			strRep = "/ a ";
			CheckEnvironmentMessage(rec, strRep, "There is a problem with this environment string '/ a ': There is some kind of error somewhere around here: ' a '.  It may be a missing underscore _, two or more underscores, a missing slash /, something beyond a word boundary symbol #, or two or more word boundary symbols at the same edge.");
			strRep = "/ _ a _";
			CheckEnvironmentMessage(rec, strRep, "There is a problem with this environment string '/ _ a _': There is some kind of error somewhere around here: '_'.  It may be a missing underscore _, two or more underscores, a missing slash /, something beyond a word boundary symbol #, or two or more word boundary symbols at the same edge.");
			strRep = "/ b # a _";
			CheckEnvironmentMessage(rec, strRep, "There is a problem with this environment string '/ b # a _': There is some kind of error somewhere around here: ' a _'.  It may be a missing underscore _, two or more underscores, a missing slash /, something beyond a word boundary symbol #, or two or more word boundary symbols at the same edge.");
			strRep = "/ _ b # a";
			CheckEnvironmentMessage(rec, strRep, "There is a problem with this environment string '/ _ b # a': There is some kind of error somewhere around here: 'a'.  It may be a missing underscore _, two or more underscores, a missing slash /, something beyond a word boundary symbol #, or two or more word boundary symbols at the same edge.");
			strRep = "/ ## a _";
			CheckEnvironmentMessage(rec, strRep, "There is a problem with this environment string '/ ## a _': There is some kind of error somewhere around here: ' a _'.  It may be a missing underscore _, two or more underscores, a missing slash /, something beyond a word boundary symbol #, or two or more word boundary symbols at the same edge.");
			strRep = "/ _ a ##";
			CheckEnvironmentMessage(rec, strRep, "There is a problem with this environment string '/ _ a ##': There is some kind of error somewhere around here: '#'.  It may be a missing underscore _, two or more underscores, a missing slash /, something beyond a word boundary symbol #, or two or more word boundary symbols at the same edge.");
		}

		private static void CheckEnvironmentMessage(PhonEnvRecognizer rec, string strRep, string sExpected)
		{
			if (rec.Recognize(strRep))
				Assert.Fail("Environment '" + strRep + "' should fail, but did not");
			else
			{
				int pos;
				string sMessage;
				StringServices.CreateErrorMessageFromXml(strRep, rec.ErrorMessage, out pos, out sMessage);
				Assert.AreEqual(sExpected, sMessage);
			}
		}

		/// <summary>
		/// </summary>
		[Test]
		public void CheckExoCentricCompound()
		{
			IMoExoCompound cmp = Cache.ServiceLocator.GetInstance<IMoExoCompoundFactory>().Create();
			Cache.LangProject.MorphologicalDataOA.CompoundRulesOS.Add(cmp);
			Assert.IsNotNull(cmp.LeftMsaOA);
			Assert.IsNotNull(cmp.RightMsaOA);
			Assert.IsNotNull(cmp.ToMsaOA);
		}

		/// <summary>
		/// Check the merging MSAs, when two entries are merged.
		/// </summary>
		[Test]
		public void MergeEntryMSAs()
		{
			// grab the factories that we'll need.
			IPartOfSpeechFactory factPOS = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>();
			ILexEntryFactory factLex = Cache.ServiceLocator.GetInstance<ILexEntryFactory>();
			IMoStemMsaFactory factStemMsa = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>();
			IMoDerivAffMsaFactory factDerivMsa = Cache.ServiceLocator.GetInstance<IMoDerivAffMsaFactory>();
			IMoInflAffMsaFactory factInflMsa = Cache.ServiceLocator.GetInstance<IMoInflAffMsaFactory>();

			ILexDb ldb = Cache.LangProject.LexDbOA;
			IFdoOwningSequence<ICmPossibility> posSeq = Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS;
			IPartOfSpeech pos = factPOS.Create();
			posSeq.Add(pos);
			ILexEntry lmeKeeper = factLex.Create();
			ILexEntry lmeSrc = factLex.Create();

			try
			{
				// Set up stem MSAs.
				IMoStemMsa stemKeeper = factStemMsa.Create();
				lmeKeeper.MorphoSyntaxAnalysesOC.Add(stemKeeper);
				stemKeeper.PartOfSpeechRA = pos;
				IMoStemMsa stemToss = factStemMsa.Create();
				lmeSrc.MorphoSyntaxAnalysesOC.Add(stemToss);
				stemToss.PartOfSpeechRA = pos;
				IMoStemMsa stemKeep = factStemMsa.Create();
				lmeSrc.MorphoSyntaxAnalysesOC.Add(stemKeep);
				IPartOfSpeech pos2 = factPOS.Create();
				posSeq.Add(pos2);
				stemKeep.PartOfSpeechRA = pos2;

				// Set up deriv affix MSAs.
				IMoDerivAffMsa daKeeper = factDerivMsa.Create();
				lmeKeeper.MorphoSyntaxAnalysesOC.Add(daKeeper);
				daKeeper.FromPartOfSpeechRA = pos;
				daKeeper.ToPartOfSpeechRA = pos;
				IMoDerivAffMsa daToss = factDerivMsa.Create();
				lmeSrc.MorphoSyntaxAnalysesOC.Add(daToss);
				daToss.FromPartOfSpeechRA = pos;
				daToss.ToPartOfSpeechRA = pos;

				// Set up inflectional affix MSAs.
				IMoInflAffMsa iaKeeper = factInflMsa.Create();
				lmeKeeper.MorphoSyntaxAnalysesOC.Add(iaKeeper);
				iaKeeper.PartOfSpeechRA = pos;
				IMoInflAffMsa iaToss = factInflMsa.Create();
				lmeSrc.MorphoSyntaxAnalysesOC.Add(iaToss);
				iaToss.PartOfSpeechRA = pos;

				// Merge entries.
				lmeKeeper.MergeObject(lmeSrc);
				Assert.AreEqual(4, lmeKeeper.MorphoSyntaxAnalysesOC.Count);
			}
			finally
			{
				lmeKeeper.Delete();
			}
		}

		/// <summary>
		/// Check the merging allomorphs, when two entries are merged.
		/// </summary>
		[Test]
		public void MergeEntryAllomorphs()
		{
			IMoMorphType mmtStem;
			IMoMorphType mmtPrefix;
			IMoMorphType mmtSuffix;
			IMoMorphType mmtInfix;
			IMoMorphType mmtBoundStem;
			IMoMorphType mmtProclitic;
			IMoMorphType mmtEnclitic;
			IMoMorphType mmtSimulfix;
			IMoMorphType mmtSuprafix;
			Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetMajorMorphTypes(
				out mmtStem, out mmtPrefix, out mmtSuffix, out mmtInfix, out mmtBoundStem, out mmtProclitic,
				out mmtEnclitic, out mmtSimulfix, out mmtSuprafix);

			ILexEntryFactory factLex = Cache.ServiceLocator.GetInstance<ILexEntryFactory>();
			IMoStemAllomorphFactory factStemAllo = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>();
			IMoAffixAllomorphFactory factAffixAllo = Cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>();

			ILexDb ldb = Cache.LangProject.LexDbOA;
			ILexEntry lmeKeeper = factLex.Create();
			ILexEntry lmeSrc = factLex.Create();

			try
			{
				// Set up stem allomorphs.
				IMoStemAllomorph stemKeeper = factStemAllo.Create();
				lmeKeeper.AlternateFormsOS.Add(stemKeeper);
				stemKeeper.MorphTypeRA = mmtStem;
				stemKeeper.Form.set_String(Cache.DefaultVernWs, "cat");
				IMoStemAllomorph stemToss = factStemAllo.Create();
				lmeSrc.AlternateFormsOS.Add(stemToss);
				stemToss.MorphTypeRA = mmtStem;
				stemToss.Form.set_String(Cache.DefaultVernWs, "cat");
				IMoStemAllomorph stemKeep = factStemAllo.Create();
				lmeSrc.AlternateFormsOS.Add(stemKeep);
				stemKeep.MorphTypeRA = mmtStem;
				stemKeep.Form.set_String(Cache.DefaultVernWs, "meow");

				// Set up affix allomorphs.
				IMoAffixAllomorph daKeeper = factAffixAllo.Create();
				lmeKeeper.AlternateFormsOS.Add(daKeeper);
				daKeeper.MorphTypeRA = mmtPrefix;
				daKeeper.Form.set_String(Cache.DefaultVernWs, "s");
				IMoAffixAllomorph daToss = factAffixAllo.Create();
				lmeSrc.AlternateFormsOS.Add(daToss);
				daToss.MorphTypeRA = mmtPrefix;
				daToss.Form.set_String(Cache.DefaultVernWs, "s");

				// Merge entries.
				lmeKeeper.MergeObject(lmeSrc);
				Assert.AreEqual(3, lmeKeeper.AlternateFormsOS.Count);
			}
			finally
			{
				lmeKeeper.Delete();
			}
		}

		/// <summary>
		/// Used with exporting to get, e.g. ("-is, -iz")
		/// </summary>
		[Test]
		public void MoForm_GetFormWithMarkers()
		{
			IMoMorphType mmtStem;
			IMoMorphType mmtPrefix;
			IMoMorphType mmtSuffix;
			IMoMorphType mmtInfix;
			IMoMorphType mmtBoundStem;
			IMoMorphType mmtProclitic;
			IMoMorphType mmtEnclitic;
			IMoMorphType mmtSimulfix;
			IMoMorphType mmtSuprafix;
			Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetMajorMorphTypes(
				out mmtStem, out mmtPrefix, out mmtSuffix, out mmtInfix, out mmtBoundStem, out mmtProclitic,
				out mmtEnclitic, out mmtSimulfix, out mmtSuprafix);
			var systems = Cache.LangProject.CurrentVernacularWritingSystems;
			if (systems.Count() < 2)
				Cache.LangProject.AddToCurrentVernacularWritingSystems(Cache.LangProject.CurrentAnalysisWritingSystems.First());
			ILexEntry le = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			le.LexemeFormOA = Cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>().Create();
			le.LexemeFormOA.Form.set_String(Cache.DefaultVernWs, "xyzTest1");
			le.LexemeFormOA.MorphTypeRA = mmtPrefix;
			ILexSense ls = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			le.SensesOS.Add(ls);
			ls.Definition.set_String(Cache.DefaultAnalWs, "xyzDefn1.1");

			IMoForm m = MorphServices.MakeMorph(le, TsStringUtils.MakeTss("-is", Cache.DefaultVernWs));
			Assert.AreEqual(mmtSuffix, m.MorphTypeRA);
			Assert.AreEqual("is", m.Form.get_String(systems.First().Handle).Text);
			Assert.IsTrue(m is IMoAffixAllomorph, "\"-is\" should have produced an affix allomorph");
			Assert.AreEqual(LexEntryTags.kflidAlternateForms, m.OwningFlid, "MakeMorph should have created an alternate form");
			m.Form.set_String(systems.ElementAt(1).Handle, "iz");
			Assert.AreEqual(2, m.Form.StringCount);
			string f1 = m.GetFormWithMarkers(systems.First().Handle);
			Assert.AreEqual("-is", f1);
			string f2 = m.GetFormWithMarkers(systems.ElementAt(1).Handle);
			Assert.AreEqual("-iz", f2);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// test that a new PhPhoneme comes with a PhCode.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreatePhPhonemeWithPhCode()
		{
			IPhPhonemeSet ps = Cache.ServiceLocator.GetInstance<IPhPhonemeSetFactory>().Create();
			Cache.LangProject.PhonologicalDataOA.PhonemeSetsOS.Add(ps);
			IPhPhoneme phone = Cache.ServiceLocator.GetInstance<IPhPhonemeFactory>().Create();
			ps.PhonemesOC.Add(phone);
			Assert.IsTrue((phone.CodesOS != null) && (phone.CodesOS.Count > 0),
				"PhPhoneme should have at least one PhCode");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// test creating PhPhonData.PhonRuleFeats
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RebuildPhonRuleFeats()
		{
			// exception "features"
			var restrictionsList = Cache.LangProject.MorphologicalDataOA.ProdRestrictOA;
			if (restrictionsList == null)
			{
				restrictionsList = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
			}
			var prodRestrict1 = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
			restrictionsList.PossibilitiesOS.Add(prodRestrict1);
			var wsBestAnalysis = WritingSystemServices.InterpretWsLabel(Cache, "best analysis", null, 0, 0, null);
			prodRestrict1.Name.set_String(wsBestAnalysis, "Exception feature the first");
			var prodRestrict2 = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
			restrictionsList.PossibilitiesOS.Add(prodRestrict2);
			var prodRestrict3 = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
			restrictionsList.PossibilitiesOS.Add(prodRestrict3);
			var prodRestrict4 = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
			restrictionsList.PossibilitiesOS.Add(prodRestrict4);
			restrictionsList.PossibilitiesOS.Remove(prodRestrict4);  // testing removal when there are no PhonRuleFeats yet
			// inflection classes
			IPartOfSpeech pos = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create();
			Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Add(pos);
			IMoInflClass moInflClass1 = Cache.ServiceLocator.GetInstance<IMoInflClassFactory>().Create();
			pos.InflectionClassesOC.Add(moInflClass1);
			moInflClass1.Name.set_String(wsBestAnalysis, "Inflection class the first");
			IMoInflClass moInflClass2 = Cache.ServiceLocator.GetInstance<IMoInflClassFactory>().Create();
			pos.InflectionClassesOC.Add(moInflClass2);
			IMoInflClass moInflClass3 = Cache.ServiceLocator.GetInstance<IMoInflClassFactory>().Create();
			pos.InflectionClassesOC.Add(moInflClass3);
			IMoInflClass moInflClass4 = Cache.ServiceLocator.GetInstance<IMoInflClassFactory>().Create();
			pos.InflectionClassesOC.Add(moInflClass4);
			pos.InflectionClassesOC.Remove(moInflClass4);  // testing removal when there are no PhonRuleFeats yet
			// Collect up exception features and inflection classes
			var result = new List<ICmObject>();
			var prodRestricts = Cache.LangProject.MorphologicalDataOA.ProdRestrictOA.PossibilitiesOS.Cast<ICmObject>();
			result.AddRange(prodRestricts);
			var inflClasses = pos.AllInflectionClasses;
			result.AddRange(inflClasses);

			var phonRuleFeats = Cache.LangProject.PhonologicalDataOA.PhonRuleFeatsOA;
			Assert.AreEqual(0, phonRuleFeats.PossibilitiesOS.Count,
				"PhonRuleFeats should be empty");
			Cache.LangProject.PhonologicalDataOA.RebuildPhonRuleFeats(result);
			Assert.AreEqual(6, phonRuleFeats.PossibilitiesOS.Count,
				"PhonRuleFeats should have six items");
			string sExceptionFeatureNewName = "Exception feature one";
			prodRestrict1.Name.set_String(wsBestAnalysis, sExceptionFeatureNewName);
			string sInflClassNewName = "Inflection class one";
			moInflClass1.Name.set_String(wsBestAnalysis, sInflClassNewName);
			result = new List<ICmObject>();
			prodRestricts = Cache.LangProject.MorphologicalDataOA.ProdRestrictOA.PossibilitiesOS.Cast<ICmObject>();
			result.AddRange(prodRestricts);
			inflClasses = pos.AllInflectionClasses;
			result.AddRange(inflClasses);
			Cache.LangProject.PhonologicalDataOA.RebuildPhonRuleFeats(result);
			Assert.AreEqual(6, phonRuleFeats.PossibilitiesOS.Count,
				"PhonRuleFeats should have six items after exception feature rename and after inflection class rename");
			var sFirstOnesName = phonRuleFeats.PossibilitiesOS.First().Name.get_String(wsBestAnalysis);
			Assert.AreEqual(sExceptionFeatureNewName, sFirstOnesName.Text,
				"PhonRuleFeats should reflect a name change for exception features");
			sFirstOnesName = phonRuleFeats.PossibilitiesOS.ElementAt(3).Name.get_String(wsBestAnalysis);
			Assert.AreEqual(sInflClassNewName, sFirstOnesName.Text,
				"PhonRuleFeats should reflect a name change for inflection classes");
			restrictionsList.PossibilitiesOS.Remove(prodRestrict2);
			Assert.AreEqual(5, phonRuleFeats.PossibilitiesOS.Count,
				"PhonRuleFeats should have five items after deleting one productivity restriction");
			pos.InflectionClassesOC.Remove(moInflClass2);
			Assert.AreEqual(4, phonRuleFeats.PossibilitiesOS.Count,
				"PhonRuleFeats should have four items after deleting one inflection class");
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// test that ShortName for optional slots have parentheses and non-optional do not.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InflAffixSlotShortName()
		{
			IPartOfSpeech pos = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create();
			Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Add(pos);
			IMoInflAffixSlot slot = Cache.ServiceLocator.GetInstance<IMoInflAffixSlotFactory>().Create();
			pos.AffixSlotsOC.Add(slot);
			slot.Optional = false;
			string sName = "TestSlot";
			slot.Name.set_String(Cache.DefaultAnalWs, sName);
			Assert.AreEqual(sName, slot.ShortName);
			slot.Optional = true;
			Assert.AreEqual("(" + sName + ")", slot.ShortName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// test the method which produces a citation form with markers
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CitationFormWithAffixType()
		{
			ILexEntry entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			string sULForm = "abc";
			entry.CitationForm.set_String(Cache.DefaultVernWs, sULForm);
			IMoAffixAllomorph allomorph = Cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>().Create();
			entry.LexemeFormOA = allomorph;
			Set<ICmPossibility> morphTypes = Cache.LangProject.LexDbOA.MorphTypesOA.ReallyReallyAllPossibilities;
			foreach (IMoMorphType mmt in morphTypes)
			{
				allomorph.MorphTypeRA = mmt;
				if (mmt.Guid == MoMorphTypeTags.kguidMorphBoundRoot)
					Assert.AreEqual("*" + sULForm, entry.CitationFormWithAffixType, "Expected * prefix for bound root with CF");
				else if (mmt.Guid == MoMorphTypeTags.kguidMorphBoundStem)
					Assert.AreEqual("*" + sULForm, entry.CitationFormWithAffixType, "Expected * prefix for bound stem with CF");
				else if (mmt.Guid == MoMorphTypeTags.kguidMorphCircumfix)
					Assert.AreEqual(sULForm, entry.CitationFormWithAffixType, "Expected no prefix or postfix for circumfix");
				else if (mmt.Guid == MoMorphTypeTags.kguidMorphClitic)
					Assert.AreEqual(sULForm, entry.CitationFormWithAffixType, "Expected no prefix or postfix for clitic");
				else if (mmt.Guid == MoMorphTypeTags.kguidMorphEnclitic)
					Assert.AreEqual("=" + sULForm, entry.CitationFormWithAffixType, "Expected = prefix for enclitic");
				else if (mmt.Guid == MoMorphTypeTags.kguidMorphInfix)
					Assert.AreEqual("-" + sULForm + "-", entry.CitationFormWithAffixType, "Expected - prefix and - postfix for infix");
				else if (mmt.Guid == MoMorphTypeTags.kguidMorphInfixingInterfix)
					Assert.AreEqual("-" + sULForm + "-", entry.CitationFormWithAffixType, "Expected - prefix and - postfix for infixing interfix");
				else if (mmt.Guid == MoMorphTypeTags.kguidMorphParticle)
					Assert.AreEqual(sULForm, entry.CitationFormWithAffixType, "Expected no prefix or postfix for particle");
				else if (mmt.Guid == MoMorphTypeTags.kguidMorphPrefix)
					Assert.AreEqual(sULForm + "-", entry.CitationFormWithAffixType, "Expected - postfix for prefix");
				else if (mmt.Guid == MoMorphTypeTags.kguidMorphPrefixingInterfix)
					Assert.AreEqual(sULForm + "-", entry.CitationFormWithAffixType, "Expected - postfix for prefixing interfix");
				else if (mmt.Guid == MoMorphTypeTags.kguidMorphProclitic)
					Assert.AreEqual(sULForm + "=", entry.CitationFormWithAffixType, "Expected = postfix for proclitic");
				else if (mmt.Guid == MoMorphTypeTags.kguidMorphRoot)
					Assert.AreEqual(sULForm, entry.CitationFormWithAffixType, "Expected no prefix or postfix for root");
				else if (mmt.Guid == MoMorphTypeTags.kguidMorphSimulfix)
					Assert.AreEqual("=" + sULForm + "=", entry.CitationFormWithAffixType, "Expected = prefix and = postfix for simulfix");
				else if (mmt.Guid == MoMorphTypeTags.kguidMorphStem)
					Assert.AreEqual(sULForm, entry.CitationFormWithAffixType, "Expected no prefix or postfix for stem");
				else if (mmt.Guid == MoMorphTypeTags.kguidMorphSuffix)
					Assert.AreEqual("-" + sULForm, entry.CitationFormWithAffixType, "Expected - prefix for suffix");
				else if (mmt.Guid == MoMorphTypeTags.kguidMorphSuffixingInterfix)
					Assert.AreEqual("-" + sULForm, entry.CitationFormWithAffixType, "Expected - prefix for suffixing interfix");
				else if (mmt.Guid == MoMorphTypeTags.kguidMorphSuprafix)
					Assert.AreEqual("~" + sULForm + "~", entry.CitationFormWithAffixType, "Expected ~ prefix and ~ postfix for suprafix");
				else if (mmt.Guid == MoMorphTypeTags.kguidMorphPhrase)
					Assert.AreEqual(sULForm, entry.CitationFormWithAffixType, "Expected no prefix or postfix for phrase");
				else if (mmt.Guid == MoMorphTypeTags.kguidMorphDiscontiguousPhrase)
					Assert.AreEqual(sULForm, entry.CitationFormWithAffixType, "Expected no prefix or postfix for discontiguous phrase");
				else
					Assert.AreEqual(sULForm, entry.CitationFormWithAffixType, "Expected no prefix or postfix for Unknown");
			}
		}

		/// <summary>
		/// Test POS Requires Inflection.
		/// </summary>
		[Test]
		public void POSRequiresInflection()
		{
			// setup expected test data
			CreatePartOfSpeech(Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS, "adjunct", false);
			IPartOfSpeech posMarker = CreatePartOfSpeech(Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS,
				"marker", false);
			CreatePartOfSpeech(posMarker.SubPossibilitiesOS, "aspectual", false);
			IPartOfSpeech posNoun = CreatePartOfSpeech(Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS,
				"noun", true);
			CreatePartOfSpeech(posNoun.SubPossibilitiesOS, "common noun", false);
			IPartOfSpeech posRelator = CreatePartOfSpeech(Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS,
				"relator", false);
			IPartOfSpeech posAdposition = CreatePartOfSpeech(posRelator.SubPossibilitiesOS,
				"adposition", false);
			CreatePartOfSpeech(posAdposition.SubPossibilitiesOS, "preposition", false);

			Assert.IsTrue(Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Count > 0);
			foreach (ICmPossibility poss in Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS)
			{
				IPartOfSpeech pos = poss as IPartOfSpeech;
				switch (pos.Name.AnalysisDefaultWritingSystem.Text)
				{
					case "adjunct":
						Assert.IsFalse(pos.RequiresInflection, "adjunct does not require inflection");
						break;
					case "marker":
						VerifyRequiresInflectionOnSubCategory(pos, "aspectual", false);
						break;
					case "noun":
						Assert.IsTrue(pos.RequiresInflection, "noun requires inflection");
						VerifyRequiresInflectionOnSubCategory(pos, "common noun", true);
						break;
					case "relator":
						VerifyRequiresInflectionOnSubSubCategory(pos, "adposition", "preposition", false);
						break;
				}
			}
		}

		IPartOfSpeech CreatePartOfSpeech(IFdoOwningSequence<ICmPossibility> owningSeq,
			string sName, bool fCreateAffixTemplate)
		{
			IPartOfSpeech pos = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create();
			owningSeq.Add(pos);
			pos.Name.set_String(Cache.DefaultAnalWs, sName);
			if (fCreateAffixTemplate)
			{
				IMoInflAffixTemplate t = Cache.ServiceLocator.GetInstance<IMoInflAffixTemplateFactory>().Create();
				pos.AffixTemplatesOS.Add(t);
			}
			return pos;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// test the method which tells us whether the inflection classes field is relevant
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InflectionClassIsRelevant()
		{
			ILexEntry entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			IMoAffixAllomorph allomorph = Cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>().Create();
			entry.AlternateFormsOS.Add(allomorph);
			var propsToMonitor = new HashSet<Tuple<int, int>>();
			Assert.IsFalse(allomorph.IsFieldRelevant(MoAffixFormTags.kflidInflectionClasses, propsToMonitor),
				"InflectionClass should not be relevant until an inflectional affix MSA with a category has been added.");
			Assert.That(propsToMonitor, Is.Empty);
			IMoInflAffMsa orange = Cache.ServiceLocator.GetInstance<IMoInflAffMsaFactory>().Create();
			entry.MorphoSyntaxAnalysesOC.Add(orange);
			Assert.IsFalse(allomorph.IsFieldRelevant(MoAffixFormTags.kflidInflectionClasses, propsToMonitor),
				"InflectionClass should not be relevant until an inflectional affix MSA with a category has been added.");
			// Review JohnT: should this result in monitoring any properties?
			IPartOfSpeech pos = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create();
			Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Add(pos);
			orange.PartOfSpeechRA = pos;
			propsToMonitor.Clear();
			Assert.IsTrue(allomorph.IsFieldRelevant(MoAffixFormTags.kflidInflectionClasses, propsToMonitor),
				"InflectionClass should now be relevant since an inflectional affix MSA with a category has been added.");
			Assert.That(propsToMonitor, Is.Empty);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// test the method which tells us whether the inflection classes field is relevant
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CliticVsStemMsaWithoutCategory()
		{
			ILexEntry entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			IMoStemAllomorph allomorph = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			entry.LexemeFormOA = allomorph;
			IMoStemMsa msa = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
			entry.MorphoSyntaxAnalysesOC.Add(msa);
			SetMorphType(allomorph, MoMorphTypeTags.kguidMorphClitic);
			Assert.AreEqual("Clitic of unknown category", msa.LongNameTs.Text);
			IMoStemAllomorph allo2 = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			entry.AlternateFormsOS.Add(allo2);
			SetMorphType(allomorph, MoMorphTypeTags.kguidMorphStem);
			Assert.AreEqual("Stem/root of unknown category; takes any affix", msa.LongNameTs.Text);
		}

		void VerifyPropsToMonitor(HashSet<Tuple<int, int>> propsToMonitor, Tuple<int, int>[] expected)
		{
			Assert.That(propsToMonitor.Count, Is.EqualTo(expected.Length));
			foreach (var item in expected)
				Assert.That(propsToMonitor.Contains(item));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// test the method which tells us whether the inflection classes field is relevant
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FromPartsOfSpeechIsRelevant()
		{
			ILexEntry entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			IMoStemAllomorph allomorph = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			entry.AlternateFormsOS.Add(allomorph);
			IMoStemMsa msa = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
			entry.MorphoSyntaxAnalysesOC.Add(msa);
			SetMorphType(allomorph, MoMorphTypeTags.kguidMorphStem);
			var propsToMonitor = new HashSet<Tuple<int, int>>();
			Assert.IsFalse(msa.IsFieldRelevant(MoStemMsaTags.kflidFromPartsOfSpeech, propsToMonitor),
				"FromPartsOfSpeech should not be relevant until the entry contains a proclitic or an enclitic.");
			var initialPropsToMonitor = new[]
				{
					new Tuple<int, int>(allomorph.Hvo, MoFormTags.kflidMorphType),
					new Tuple<int, int>(entry.Hvo, LexEntryTags.kflidLexemeForm),
					new Tuple<int, int>(entry.Hvo, LexEntryTags.kflidAlternateForms)
				};
			VerifyPropsToMonitor(propsToMonitor, initialPropsToMonitor);
			propsToMonitor.Clear();
			SetMorphType(allomorph, MoMorphTypeTags.kguidMorphProclitic);
			Assert.IsTrue(msa.IsFieldRelevant(MoStemMsaTags.kflidFromPartsOfSpeech, propsToMonitor),
				"FromPartsOfSpeech should now be relevant since the entry has a proclitic.");
			VerifyPropsToMonitor(propsToMonitor, initialPropsToMonitor);
			SetMorphType(allomorph, MoMorphTypeTags.kguidMorphEnclitic);
			Assert.IsTrue(msa.IsFieldRelevant(MoStemMsaTags.kflidFromPartsOfSpeech, propsToMonitor),
				"FromPartsOfSpeech should now be relevant since the entry has an enclitic.");
			SetMorphType(allomorph, MoMorphTypeTags.kguidMorphClitic);
			Assert.IsFalse(msa.IsFieldRelevant(MoStemMsaTags.kflidFromPartsOfSpeech, propsToMonitor),
				"FromPartsOfSpeech should not be relevant until the entry contains a proclitic or an enclitic.");
			SetMorphType(allomorph, MoMorphTypeTags.kguidMorphBoundRoot);
			Assert.IsFalse(msa.IsFieldRelevant(MoStemMsaTags.kflidFromPartsOfSpeech, propsToMonitor),
				"FromPartsOfSpeech should not be relevant until the entry contains a proclitic or an enclitic.");
			SetMorphType(allomorph, MoMorphTypeTags.kguidMorphBoundStem);
			Assert.IsFalse(msa.IsFieldRelevant(MoStemMsaTags.kflidFromPartsOfSpeech, propsToMonitor),
				"FromPartsOfSpeech should not be relevant until the entry contains a proclitic or an enclitic.");
			SetMorphType(allomorph, MoMorphTypeTags.kguidMorphCircumfix);
			Assert.IsFalse(msa.IsFieldRelevant(MoStemMsaTags.kflidFromPartsOfSpeech, propsToMonitor),
				"FromPartsOfSpeech should not be relevant until the entry contains a proclitic or an enclitic.");
			SetMorphType(allomorph, MoMorphTypeTags.kguidMorphDiscontiguousPhrase);
			Assert.IsFalse(msa.IsFieldRelevant(MoStemMsaTags.kflidFromPartsOfSpeech, propsToMonitor),
				"FromPartsOfSpeech should not be relevant until the entry contains a proclitic or an enclitic.");
			SetMorphType(allomorph, MoMorphTypeTags.kguidMorphInfix);
			Assert.IsFalse(msa.IsFieldRelevant(MoStemMsaTags.kflidFromPartsOfSpeech, propsToMonitor),
				"FromPartsOfSpeech should not be relevant until the entry contains a proclitic or an enclitic.");
			SetMorphType(allomorph, MoMorphTypeTags.kguidMorphInfixingInterfix);
			Assert.IsFalse(msa.IsFieldRelevant(MoStemMsaTags.kflidFromPartsOfSpeech, propsToMonitor),
				"FromPartsOfSpeech should not be relevant until the entry contains a proclitic or an enclitic.");
			SetMorphType(allomorph, MoMorphTypeTags.kguidMorphParticle);
			Assert.IsFalse(msa.IsFieldRelevant(MoStemMsaTags.kflidFromPartsOfSpeech, propsToMonitor),
				"FromPartsOfSpeech should not be relevant until the entry contains a proclitic or an enclitic.");
			SetMorphType(allomorph, MoMorphTypeTags.kguidMorphPhrase);
			Assert.IsFalse(msa.IsFieldRelevant(MoStemMsaTags.kflidFromPartsOfSpeech, propsToMonitor),
				"FromPartsOfSpeech should not be relevant until the entry contains a proclitic or an enclitic.");
			SetMorphType(allomorph, MoMorphTypeTags.kguidMorphPrefix);
			Assert.IsFalse(msa.IsFieldRelevant(MoStemMsaTags.kflidFromPartsOfSpeech, propsToMonitor),
				"FromPartsOfSpeech should not be relevant until the entry contains a proclitic or an enclitic.");
			SetMorphType(allomorph, MoMorphTypeTags.kguidMorphPrefixingInterfix);
			Assert.IsFalse(msa.IsFieldRelevant(MoStemMsaTags.kflidFromPartsOfSpeech, propsToMonitor),
				"FromPartsOfSpeech should not be relevant until the entry contains a proclitic or an enclitic.");
			SetMorphType(allomorph, MoMorphTypeTags.kguidMorphRoot);
			Assert.IsFalse(msa.IsFieldRelevant(MoStemMsaTags.kflidFromPartsOfSpeech, propsToMonitor),
				"FromPartsOfSpeech should not be relevant until the entry contains a proclitic or an enclitic.");
			SetMorphType(allomorph, MoMorphTypeTags.kguidMorphSimulfix);
			Assert.IsFalse(msa.IsFieldRelevant(MoStemMsaTags.kflidFromPartsOfSpeech, propsToMonitor),
				"FromPartsOfSpeech should not be relevant until the entry contains a proclitic or an enclitic.");
			SetMorphType(allomorph, MoMorphTypeTags.kguidMorphSuffix);
			Assert.IsFalse(msa.IsFieldRelevant(MoStemMsaTags.kflidFromPartsOfSpeech, propsToMonitor),
				"FromPartsOfSpeech should not be relevant until the entry contains a proclitic or an enclitic.");
			SetMorphType(allomorph, MoMorphTypeTags.kguidMorphSuffixingInterfix);
			Assert.IsFalse(msa.IsFieldRelevant(MoStemMsaTags.kflidFromPartsOfSpeech, propsToMonitor),
				"FromPartsOfSpeech should not be relevant until the entry contains a proclitic or an enclitic.");
			SetMorphType(allomorph, MoMorphTypeTags.kguidMorphSuprafix);
			Assert.IsFalse(msa.IsFieldRelevant(MoStemMsaTags.kflidFromPartsOfSpeech, propsToMonitor),
				"FromPartsOfSpeech should not be relevant until the entry contains a proclitic or an enclitic.");

			IMoStemAllomorph lf = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			entry.LexemeFormOA = lf;
			SetMorphType(lf, MoMorphTypeTags.kguidMorphProclitic);
			propsToMonitor.Clear();
			Assert.IsTrue(msa.IsFieldRelevant(MoStemMsaTags.kflidFromPartsOfSpeech, propsToMonitor),
				"FromPartsOfSpeech should now be relevant since the entry has a proclitic.");
			VerifyPropsToMonitor(propsToMonitor, initialPropsToMonitor.Concat(new [] {new Tuple<int, int>(lf.Hvo, MoFormTags.kflidMorphType)}).ToArray());
		}

		private void SetMorphType(IMoStemAllomorph allomorph, Guid guidType)
		{
			allomorph.MorphTypeRA = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(guidType);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// test the method which tells us whether inflection class is relevant for compound rules
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InflectionClassInCompoundStemMsasIsRelevant()
		{
			IMoExoCompound compound = Cache.ServiceLocator.GetInstance<IMoExoCompoundFactory>().Create();
			Cache.LangProject.MorphologicalDataOA.CompoundRulesOS.Add(compound);
			IMoStemMsaFactory factStemMsa = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>();
			IMoStemMsa msaLeft = factStemMsa.Create();
			compound.LeftMsaOA = msaLeft;
			IMoStemMsa msaRight = factStemMsa.Create();
			compound.RightMsaOA = msaRight;
			IMoStemMsa msaTo = factStemMsa.Create();
			compound.ToMsaOA = msaTo;
			var propsToMonitor = new HashSet<Tuple<int, int>>();
			Assert.IsFalse(msaLeft.IsFieldRelevant(MoStemMsaTags.kflidInflectionClass, propsToMonitor),
				"Inflection Class should not be relevant for LeftMsa.");
			Assert.IsFalse(msaRight.IsFieldRelevant(MoStemMsaTags.kflidInflectionClass, propsToMonitor),
				"Inflection Class should not be relevant for RightMsa.");
			Assert.IsFalse(msaTo.IsFieldRelevant(MoStemMsaTags.kflidInflectionClass, propsToMonitor),
				"Inflection Class should not be relevant for ToMsa if it does not have a category.");
			IPartOfSpeech pos = new PartOfSpeech();
			Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Add(pos);
			msaTo.PartOfSpeechRA = pos;
			Assert.IsTrue(msaTo.IsFieldRelevant(MoStemMsaTags.kflidInflectionClass, propsToMonitor),
				"Inflection Class should be relevant for ToMsa when it has a category.");
		}


		#endregion Tests

		//        #region broken tests
		//
		//        /// <summary>
		//        /// </summary>
		//        [Test]
		//        [Ignore("Broken until recursive reference Targets is reimplemented (JH).")]
		//        public void RefTargCandOnMoStemMsa()
		//        {
		//            CheckDisposed();
		//
		//            MoStemMsa msa = GetFirstMoStemMsa();
		//            Set<int> hvos = msa.ReferenceTargetCandidates(MoStemMsaTags.kflidPartOfSpeech);
		//            ObjectLabelCollection items = new ObjectLabelCollection(Cache, hvos);
		//            Assert.AreEqual(items.Count, Cache.LangProject.PartsOfSpeechOA.ReallyReallyAllPossibilities.Count, "Wrong count");
		//        }
		//
		//        #endregion broken tests
		//
		//        #region Misc methods
		//
		//        /// <summary>
		//        /// </summary>
		//        protected MoStemMsa GetFirstMoStemMsa()
		//        {
		//            int[] hvos = GetHvosForFirstNObjectsOfClass("MoStemMsa", 1);
		//            return (MoStemMsa)CmObject.CreateFromDBObject(Cache, hvos[0]);
		//
		//        }

		private void VerifyRequiresInflectionOnSubSubCategory(IPartOfSpeech pos, string sPOSName, string sPOSSubCatName, bool fExpectTrue)
		{
			Assert.IsTrue(pos.SubPossibilitiesOS.Count > 0);
			foreach (ICmPossibility poss in pos.SubPossibilitiesOS)
			{
				pos = poss as IPartOfSpeech;
				if (pos.Name.AnalysisDefaultWritingSystem.Text == sPOSName)
					VerifyRequiresInflectionOnSubCategory(pos, sPOSSubCatName, fExpectTrue);
			}
		}
		private void VerifyRequiresInflectionOnSubCategory(IPartOfSpeech pos, string sPOSName, bool fExpectTrue)
		{
			Assert.IsTrue(pos.SubPossibilitiesOS.Count > 0);
			foreach (ICmPossibility poss in pos.SubPossibilitiesOS)
			{
				pos = poss as IPartOfSpeech;
				if (pos.Name.AnalysisDefaultWritingSystem.Text == sPOSName)
				{
					if (fExpectTrue)
						Assert.IsTrue(pos.RequiresInflection, sPOSName + " requires inflection");
					else
						Assert.IsFalse(pos.RequiresInflection, sPOSName + " does not require inflection");
				}
			}
		}

		/// <summary>
		/// </summary>
		[Test]
		public void LexemeFormStemAllomorphIsRelevant()
		{
			ILexEntry stemEntry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			IMoStemAllomorph stemAllomorph = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			stemEntry.LexemeFormOA = stemAllomorph;
			var propsToMonitor = new HashSet<Tuple<int, int>>();
			Assert.IsFalse(stemAllomorph.IsFieldRelevant(MoStemAllomorphTags.kflidStemName, propsToMonitor),
				"Stem allomorph stem name should not be relevant when there is no morph type.");
			foreach (IMoMorphType mmt in Cache.LangProject.LexDbOA.MorphTypesOA.PossibilitiesOS)
			{
				stemAllomorph.MorphTypeRA = mmt;
				if (mmt.Guid == MoMorphTypeTags.kguidMorphRoot)
				{
					Assert.IsTrue(stemAllomorph.IsFieldRelevant(MoStemAllomorphTags.kflidStemName, propsToMonitor),
						"Stem allomorph stem name should be relevant for a root morph type.");
					VerifyPropsToMonitor(propsToMonitor,
						new[] {new Tuple<int, int>(stemAllomorph.Hvo, MoFormTags.kflidMorphType)});
				}
				else if (mmt.Guid == MoMorphTypeTags.kguidMorphBoundRoot)
					Assert.IsTrue(stemAllomorph.IsFieldRelevant(MoStemAllomorphTags.kflidStemName, propsToMonitor),
						"Stem allomorph stem name should be relevant for a bound root morph type.");
				else if (mmt.Guid == MoMorphTypeTags.kguidMorphStem)
					Assert.IsTrue(stemAllomorph.IsFieldRelevant(MoStemAllomorphTags.kflidStemName, propsToMonitor),
						"Stem allomorph stem name should be relevant for a stem morph type.");
				else if (mmt.Guid == MoMorphTypeTags.kguidMorphBoundStem)
					Assert.IsTrue(stemAllomorph.IsFieldRelevant(MoStemAllomorphTags.kflidStemName, propsToMonitor),
						"Stem allomorph stem name should be relevant for a bound stem morph type.");
				else if (mmt.Guid == MoMorphTypeTags.kguidMorphCircumfix)
					Assert.IsFalse(stemAllomorph.IsFieldRelevant(MoStemAllomorphTags.kflidStemName, propsToMonitor),
						"Stem allomorph stem name should not be relevant for a circumfix morph type.");
				else if (mmt.Guid == MoMorphTypeTags.kguidMorphDiscontiguousPhrase)
					Assert.IsFalse(stemAllomorph.IsFieldRelevant(MoStemAllomorphTags.kflidStemName, propsToMonitor),
						"Stem allomorph stem name should not be relevant for a discontiguous phrasemorph type.");
				else if (mmt.Guid == MoMorphTypeTags.kguidMorphClitic)
					Assert.IsFalse(stemAllomorph.IsFieldRelevant(MoStemAllomorphTags.kflidStemName, propsToMonitor),
						"Stem allomorph stem name should not be relevant for a clitic morph type.");
				else if (mmt.Guid == MoMorphTypeTags.kguidMorphEnclitic)
					Assert.IsFalse(stemAllomorph.IsFieldRelevant(MoStemAllomorphTags.kflidStemName, propsToMonitor),
						"Stem allomorph stem name should not be relevant for an enclitic morph type.");
				else if (mmt.Guid == MoMorphTypeTags.kguidMorphInfix)
					Assert.IsFalse(stemAllomorph.IsFieldRelevant(MoStemAllomorphTags.kflidStemName, propsToMonitor),
						"Stem allomorph stem name should not be relevant for an infix morph type.");
				else if (mmt.Guid == MoMorphTypeTags.kguidMorphInfixingInterfix)
					Assert.IsFalse(stemAllomorph.IsFieldRelevant(MoStemAllomorphTags.kflidStemName, propsToMonitor),
						"Stem allomorph stem name should not be relevant for an infixing interfix morph type.");
				else if (mmt.Guid == MoMorphTypeTags.kguidMorphParticle)
					Assert.IsFalse(stemAllomorph.IsFieldRelevant(MoStemAllomorphTags.kflidStemName, propsToMonitor),
						"Stem allomorph stem name should not be relevant for a particle morph type.");
				else if (mmt.Guid == MoMorphTypeTags.kguidMorphPhrase)
					Assert.IsTrue(stemAllomorph.IsFieldRelevant(MoStemAllomorphTags.kflidStemName, propsToMonitor),
						"Stem allomorph stem name should be relevant for a phrase morph type.");
				else if (mmt.Guid == MoMorphTypeTags.kguidMorphPrefix)
					Assert.IsFalse(stemAllomorph.IsFieldRelevant(MoStemAllomorphTags.kflidStemName, propsToMonitor),
						"Stem allomorph stem name should not be relevant for a prefix morph type.");
				else if (mmt.Guid == MoMorphTypeTags.kguidMorphPrefixingInterfix)
					Assert.IsFalse(stemAllomorph.IsFieldRelevant(MoStemAllomorphTags.kflidStemName, propsToMonitor),
						"Stem allomorph stem name should not be relevant for a prefixing interfix morph type.");
				else if (mmt.Guid == MoMorphTypeTags.kguidMorphProclitic)
					Assert.IsFalse(stemAllomorph.IsFieldRelevant(MoStemAllomorphTags.kflidStemName, propsToMonitor),
						"Stem allomorph stem name should not be relevant for a proclitic morph type.");
				else if (mmt.Guid == MoMorphTypeTags.kguidMorphSimulfix)
					Assert.IsFalse(stemAllomorph.IsFieldRelevant(MoStemAllomorphTags.kflidStemName, propsToMonitor),
						"Stem allomorph stem name should not be relevant for a simulfix morph type.");
				else if (mmt.Guid == MoMorphTypeTags.kguidMorphSuffix)
					Assert.IsFalse(stemAllomorph.IsFieldRelevant(MoStemAllomorphTags.kflidStemName, propsToMonitor),
						"Stem allomorph stem name should not be relevant for a suffix morph type.");
				else if (mmt.Guid == MoMorphTypeTags.kguidMorphSuffixingInterfix)
					Assert.IsFalse(stemAllomorph.IsFieldRelevant(MoStemAllomorphTags.kflidStemName, propsToMonitor),
						"Stem allomorph stem name should not be relevant for a suffixing interfix morph type.");
				else if (mmt.Guid == MoMorphTypeTags.kguidMorphSuprafix)
					Assert.IsFalse(stemAllomorph.IsFieldRelevant(MoStemAllomorphTags.kflidStemName, propsToMonitor),
						"Stem allomorph stem name should not be relevant for a suprafix morph type.");
			}
		}

		private ILexEntry MakeEntry(string lf, string gloss)
		{
			ILexEntry entry = null;
			entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			var form = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			entry.LexemeFormOA = form;
			form.Form.VernacularDefaultWritingSystem =
				Cache.TsStrFactory.MakeString(lf, Cache.DefaultVernWs);
			var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			entry.SensesOS.Add(sense);
			sense.Gloss.AnalysisDefaultWritingSystem = Cache.TsStrFactory.MakeString(gloss, Cache.DefaultAnalWs);
			return entry;
		}

		private ILexRefType MakeLexRefType(string name)
		{
			ILexRefType result = null;
			if (Cache.LangProject.LexDbOA.ReferencesOA == null)
				Cache.LangProject.LexDbOA.ReferencesOA = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
			result = Cache.ServiceLocator.GetInstance<ILexRefTypeFactory>().Create();
			Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.Add(result);
			result.MappingType = (int)LexRefTypeTags.MappingTypes.kmtSenseTree;
			result.Name.AnalysisDefaultWritingSystem = Cache.TsStrFactory.MakeString(name, Cache.DefaultAnalWs);
			return result;
		}

		private ILexReference MakeLexReference(ILexRefType owner, ILexSense firstTarget)
		{
			ILexReference result = null;
			result = Cache.ServiceLocator.GetInstance<ILexReferenceFactory>().Create();
			owner.MembersOC.Add(result);
			result.TargetsRS.Add(firstTarget);
			return result;
		}

		/// <summary>
		/// Set up a LexReference (Part/Whole) and delete the LexReference.
		/// </summary>
		[Test]
		public void LexReferenceDeleteLexReferences()
		{
			var entry1 = MakeEntry("food", "stuff to eat that is nutritional");
			var sense1 = entry1.SensesOS[0];
			var entry2 = MakeEntry("dog food", "food for dogs");
			var sense2 = entry2.SensesOS[0];
			var lexRefType1 = MakeLexRefType("TestRelationPartWhole");
			var lexRef1 = MakeLexReference(lexRefType1, sense1);  //first add the current sense as the part
			lexRef1.TargetsRS.Insert(1, sense2);

			Assert.AreEqual(0, (entry1 as LexEntry).MinimalLexReferences.Count,
							"entry1 has one MinimalLexReference after creating LexReference with entry1");
			Assert.AreEqual(1, (sense1 as LexSense).MinimalLexReferences.Count,
							"sense1 has no MinimalLexReferences after creating LexReference with entry1");
			Assert.AreEqual(0, (entry2 as LexEntry).MinimalLexReferences.Count,
							"entry2 has no MinimalLexReferences after creating LexReference with entry1");
			Assert.AreEqual(1, (sense2 as LexSense).MinimalLexReferences.Count,
							"sense2 has no MinimalLexReferences after creating LexReference with entry1");

			Cache.DomainDataByFlid.DeleteObj(lexRef1.Hvo);

			Assert.AreEqual(0, (entry1 as LexEntry).MinimalLexReferences.Count,
							"entry1 has one MinimalLexReference after creating LexReference with entry1");
			Assert.AreEqual(0, (sense1 as LexSense).MinimalLexReferences.Count,
							"sense1 has no MinimalLexReferences after creating LexReference with entry1");
			Assert.AreEqual(0, (entry2 as LexEntry).MinimalLexReferences.Count,
							"entry2 has no MinimalLexReferences after creating LexReference with entry1");
			Assert.AreEqual(0, (sense2 as LexSense).MinimalLexReferences.Count,
							"sense2 has no MinimalLexReferences after creating LexReference with entry1");
		}

		/// <summary>
		/// Set up a LexReference (Part/Whole). Delete one of the references. This simulates the user deleting
		/// one either the whole or part sense by selecting the item in the slice and pressing the delete key.
		/// </summary>
		[Test]
		public void LexReferenceRemoveAllReferences()
		{
			var entry1 = MakeEntry("food", "stuff to eat that is nutritional");
			var sense1 = entry1.SensesOS[0];
			var entry2 = MakeEntry("dog food", "food for dogs");
			var sense2 = entry2.SensesOS[0];
			var lexRefType1 = MakeLexRefType("TestRelationPartWhole");
			var lexRef1 = MakeLexReference(lexRefType1, sense1);  //first add the current sense as the part
			lexRef1.TargetsRS.Insert(1, sense2);

			Assert.AreEqual(0, (entry1 as LexEntry).MinimalLexReferences.Count,
							"entry1 has one MinimalLexReference after creating LexReference with entry1");
			Assert.AreEqual(1, (sense1 as LexSense).MinimalLexReferences.Count,
							"sense1 has no MinimalLexReferences after creating LexReference with entry1");
			Assert.AreEqual(0, (entry2 as LexEntry).MinimalLexReferences.Count,
							"entry2 has no MinimalLexReferences after creating LexReference with entry1");
			Assert.AreEqual(1, (sense2 as LexSense).MinimalLexReferences.Count,
							"sense2 has no MinimalLexReferences after creating LexReference with entry1");

			lexRef1.TargetsRS.RemoveAt(1);

			Assert.AreEqual(0, (entry1 as LexEntry).MinimalLexReferences.Count,
							"entry1 has one MinimalLexReference after creating LexReference with entry1");
			Assert.AreEqual(0, (sense1 as LexSense).MinimalLexReferences.Count,
							"sense1 has no MinimalLexReferences after creating LexReference with entry1");
			Assert.AreEqual(0, (entry2 as LexEntry).MinimalLexReferences.Count,
							"entry2 has no MinimalLexReferences after creating LexReference with entry1");
			Assert.AreEqual(0, (sense2 as LexSense).MinimalLexReferences.Count,
							"sense2 has no MinimalLexReferences after creating LexReference with entry1");
		}
	}

	#endregion LingTests

	#region MsaCleanupTests

	/// <summary>
	/// Test the various ways that Msas need to be cleaned up when deleting referencing objects.
	/// </summary>
	[TestFixture]
	public class MsaCleanupTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="cf"></param>
		/// <param name="defn"></param>
		/// <param name="domain">May be null.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private ILexEntry MakeLexEntry(string cf, string defn, ICmSemanticDomain domain)
		{
			var servLoc = Cache.ServiceLocator;
			var le = servLoc.GetInstance<ILexEntryFactory>().Create();

			var ws = Cache.DefaultVernWs;
			le.CitationForm.set_String(ws, Cache.TsStrFactory.MakeString(cf, ws));
			var ls = servLoc.GetInstance<ILexSenseFactory>().Create();
			le.SensesOS.Add(ls);
			ws = Cache.DefaultAnalWs;
			ls.Definition.set_String(ws, Cache.TsStrFactory.MakeString(defn, ws));
			if (domain != null)
				ls.SemanticDomainsRC.Add(domain);
			var msa = servLoc.GetInstance<IMoStemMsaFactory>().Create();
			le.MorphoSyntaxAnalysesOC.Add(msa);
			ls.MorphoSyntaxAnalysisRA = msa;
			return le;
		}

		/// <summary>
		/// Check that deleting a LexSense also deletes any MSA that was referred to only by that
		/// LexSense.
		/// </summary>
		[Test]
		public void DeleteLexSense()
		{
			var le = MakeLexEntry("xyzTest1", "xyzDefn1.1", null);
			var ls = le.SensesOS[0];
			var msa = le.MorphoSyntaxAnalysesOC.ToArray()[0];
			le.SensesOS.Remove(ls);
			Assert.AreEqual(0, le.MorphoSyntaxAnalysesOC.Count, "Msa not deleted.");
			Assert.AreEqual((int)SpecialHVOValues.kHvoObjectDeleted, msa.Hvo, "Msa not with correct hvo.");
		}
		/// <summary>
		/// Check that changing an MSA on the LexSense also deletes any MSA
		/// that was referred to only by that LexSense.
		/// </summary>
		[Test]
		public void ChangeMsaOnLexSense()
		{
			var le = MakeLexEntry("xyzTest1", "xyzDefn1.1", null);
			var ls = le.SensesOS[0];
			var oldMsa = le.MorphoSyntaxAnalysesOC.ToArray()[0];

			// Add new MSA to LexEntry
			var msa = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
			le.MorphoSyntaxAnalysesOC.Add(msa);
			// Change the LexSense MSA
			ls.MorphoSyntaxAnalysisRA = msa;
			Assert.AreEqual(1, le.MorphoSyntaxAnalysesOC.Count, "Msa not deleted.");
			Assert.AreEqual((int)SpecialHVOValues.kHvoObjectDeleted, oldMsa.Hvo, "Msa not with correct hvo.");
		}

		/// <summary>
		/// Check that deleting a WfiMorphBundle also deletes any MSA that was referred to only
		/// by that WfiMorphBundle.
		/// </summary>
		[Test]
		public void DeleteWfiMorphBundle()
		{
			var le = MakeLexEntry("xyzTest1", "xyzDefn1.1", null);
			var ls = le.SensesOS[0];
			var oldMsa = le.MorphoSyntaxAnalysesOC.ToArray()[0];

			// Setup WfiMorphBundle
			var servLoc = Cache.ServiceLocator;
			var wf = servLoc.GetInstance<IWfiWordformFactory>().Create();
			var anal = servLoc.GetInstance<IWfiAnalysisFactory>().Create();
			wf.AnalysesOC.Add(anal);
			var wmb = servLoc.GetInstance<IWfiMorphBundleFactory>().Create();
			anal.MorphBundlesOS.Add(wmb);
			var bearNForm = servLoc.GetInstance<IMoStemAllomorphFactory>().Create();
			le.AlternateFormsOS.Add(bearNForm);
			wmb.MorphRA = bearNForm;
			wmb.MsaRA = ls.MorphoSyntaxAnalysisRA;

			// Delete the LexSense
			le.SensesOS.Remove(ls);
			Assert.AreEqual(1, le.MorphoSyntaxAnalysesOC.Count);

			// Delete the WfiMorphBundle
			anal.MorphBundlesOS.Remove(wmb);
			Assert.AreEqual(0, le.MorphoSyntaxAnalysesOC.Count);
			Assert.AreEqual((int)SpecialHVOValues.kHvoObjectDeleted, oldMsa.Hvo, "Msa not with correct hvo.");
		}

		/// <summary>
		/// Check that changing an MSA on the WfiMorphBundle also deletes any MSA that was referred to only by that
		/// WfiMorphBundle.
		/// </summary>
		[Test]
		public void ChangeMsaOnWfiMorphBundle()
		{
			var le = MakeLexEntry("xyzTest1", "xyzDefn1.1", null);
			var ls = le.SensesOS[0];
			var oldMsa = le.MorphoSyntaxAnalysesOC.ToArray()[0];

			// Setup WfiMorphBundle
			var servLoc = Cache.ServiceLocator;
			var wf = servLoc.GetInstance<IWfiWordformFactory>().Create();
			var anal = servLoc.GetInstance<IWfiAnalysisFactory>().Create();
			wf.AnalysesOC.Add(anal);
			var wmb = servLoc.GetInstance<IWfiMorphBundleFactory>().Create();
			anal.MorphBundlesOS.Add(wmb);
			var bearNForm = servLoc.GetInstance<IMoStemAllomorphFactory>().Create();
			le.AlternateFormsOS.Add(bearNForm);
			wmb.MorphRA = bearNForm;
			wmb.MsaRA = ls.MorphoSyntaxAnalysisRA;

			// Add new MSA to LexEntry
			var msa = servLoc.GetInstance<IMoStemMsaFactory>().Create();
			le.MorphoSyntaxAnalysesOC.Add(msa);
			// Change LexSense MSA to new MSA
			ls.MorphoSyntaxAnalysisRA = msa;
			Assert.AreEqual(2, le.MorphoSyntaxAnalysesOC.Count);

			// Change Msa on WfiMorphBundle
			wmb.MsaRA = ls.MorphoSyntaxAnalysisRA;
			Assert.AreEqual(msa.Hvo, wmb.MsaRA.Hvo);
			Assert.AreEqual(1, le.MorphoSyntaxAnalysesOC.Count);
			Assert.AreEqual((int)SpecialHVOValues.kHvoObjectDeleted, oldMsa.Hvo, "Msa not with correct hvo.");
		}

		/// <summary>
		/// Check that deleting a MoMorphAdhocProhib also deletes any MSA that was
		/// referred to only by that object.
		/// </summary>
		[Test]
		public void DeleteMoMorphAdhocProhib()
		{
			var le = MakeLexEntry("xyzTest1", "xyzDefn1.1", null);
			var ls = le.SensesOS[0];
			var baseMsa = ls.MorphoSyntaxAnalysisRA;

			var servLoc = Cache.ServiceLocator;
			var mmac = servLoc.GetInstance<IMoMorphAdhocProhibFactory>().Create();
			Cache.LangProject.MorphologicalDataOA.AdhocCoProhibitionsOC.Add(mmac);
			// Set FirstMorpheme
			mmac.FirstMorphemeRA = baseMsa;

			// Build Morphemes (Reference Sequence) for our mmac
			var stemMsaFactory = servLoc.GetInstance<IMoStemMsaFactory>();
			var i = 0;
			for (; i < 2; i++)
			{
				// Add new MSA to our LexEntry
				var msa = stemMsaFactory.Create();
				le.MorphoSyntaxAnalysesOC.Add(msa);
				Assert.AreEqual(i + 2, le.MorphoSyntaxAnalysesOC.Count);
				mmac.MorphemesRS.Add(msa);
			}

			// Build RestOfMorphs (Reference Sequence) for our mmac
			for (var j = 0; j < 2; j++)
			{
				// Add new MSA to our LexEntry
				var msa = stemMsaFactory.Create();
				le.MorphoSyntaxAnalysesOC.Add(msa);
				// Add new MSA to Components
				mmac.RestOfMorphsRS.Add(msa);
			}

			// Delete the LexSense so that when we delete the msa,
			// mmac can delete its FirstMorpheme.
			le.SensesOS.Remove(ls);
			Assert.AreEqual(5, le.MorphoSyntaxAnalysesOC.Count);
			// Delete the MoMorphAdhocProhib (mmac)
			Cache.LangProject.MorphologicalDataOA.AdhocCoProhibitionsOC.Remove(mmac);
			Assert.AreEqual(0, le.MorphoSyntaxAnalysesOC.Count);
		}

		/// <summary>
		/// Check that deleting a MoMorphSynAnalysis also deletes any MSA that was
		/// referred to only by that object.
		/// </summary>
		[Test]
		public void DeleteMoMorphSynAnalysis()
		{
			var le = MakeLexEntry("xyzTest1", "xyzDefn1.1", null);
			var ls = le.SensesOS[0];
			var baseMsa = ls.MorphoSyntaxAnalysisRA;

			// Build Components (Reference Sequence) for our baseMSA
			var msaFactory = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>();
			for (int i = 1; i < 4; i++)
			{
				// Add new MSA to our LexEntry
				var msa = msaFactory.Create();
				le.MorphoSyntaxAnalysesOC.Add(msa);
				baseMsa.ComponentsRS.Add(msa);
			}

			// Delete baseMsa by deleting the only reference to it (LexSense)
			le.SensesOS.Remove(ls);
			Assert.AreEqual(0, le.MorphoSyntaxAnalysesOC.Count);
			Assert.AreEqual((int)SpecialHVOValues.kHvoObjectDeleted, baseMsa.Hvo, "Msa not with correct hvo.");
		}
	}
	#endregion MsaCleanupTests

	#region EqualMsaTests

	/// <summary>
	///
	/// </summary>
	[TestFixture]
	public class EqualMsaTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		#region Test setup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			base.CreateTestData();

			ICmPossibilityList list = Cache.LangProject.MorphologicalDataOA.ProdRestrictOA;
			if (list.PossibilitiesOS.Count < 4)
			{
				ICmPossibilityFactory fact = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>();
				while (list.PossibilitiesOS.Count < 4)
					list.PossibilitiesOS.Add(fact.Create());
			}
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected ILexEntry MakeLexEntry(string cf, string defn)
		{
			ILexEntry le = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			le.CitationForm.set_String(Cache.DefaultVernWs, cf);
			ILexSense ls = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			le.SensesOS.Add(ls);
			ls.Definition.set_String(Cache.DefaultAnalWs, defn);
			IMoMorphSynAnalysis msa = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
			le.MorphoSyntaxAnalysesOC.Add(msa);
			ls.MorphoSyntaxAnalysisRA = msa;
			return le;
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void MoStemMsa_EqualMsa()
		{
			ILangProject lp = Cache.LangProject;
			ILexEntry le = MakeLexEntry("xyzTest1", "xyzDefn1.1");

			IMoStemMsa stemMsa1 = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
			IMoStemMsa stemMsa2 = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
			IMoDerivAffMsa derivAffixMsa = Cache.ServiceLocator.GetInstance<IMoDerivAffMsaFactory>().Create();
			IMoInflAffMsa inflAffixMsa = Cache.ServiceLocator.GetInstance<IMoInflAffMsaFactory>().Create();
			IMoUnclassifiedAffixMsa unclassifiedAffixMsa = Cache.ServiceLocator.GetInstance<IMoUnclassifiedAffixMsaFactory>().Create();

			le.MorphoSyntaxAnalysesOC.Add(stemMsa1);
			le.MorphoSyntaxAnalysesOC.Add(stemMsa2);
			le.MorphoSyntaxAnalysesOC.Add(derivAffixMsa);
			le.MorphoSyntaxAnalysesOC.Add(inflAffixMsa);
			le.MorphoSyntaxAnalysesOC.Add(unclassifiedAffixMsa);

			// Verify we fail comparing on different MSA types.
			Assert.IsFalse(stemMsa1.EqualsMsa(derivAffixMsa));
			Assert.IsFalse(stemMsa1.EqualsMsa(inflAffixMsa));
			Assert.IsFalse(stemMsa1.EqualsMsa(unclassifiedAffixMsa));

			// Verify that stemMsa1 equals itself.
			Assert.IsTrue(stemMsa1.EqualsMsa(stemMsa1), stemMsa1.ToString() + " - should equal itself.");

			// Verify that stemMsa1 equals stemMsa2
			Assert.IsTrue(stemMsa1.EqualsMsa(stemMsa2), stemMsa1.ToString() + " - should equal - " + stemMsa2.ToString());

			// compare with on different PartOfSpeech
			IFdoOwningSequence<ICmPossibility> posSeq = lp.PartsOfSpeechOA.PossibilitiesOS;
			IPartOfSpeech pos1 = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create();
			posSeq.Add(pos1);
			IPartOfSpeech pos2 = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create();
			posSeq.Add(pos2);

			stemMsa1.PartOfSpeechRA = pos1;
			stemMsa2.PartOfSpeechRA = pos2;
			Assert.IsTrue(stemMsa1.PartOfSpeechRA != stemMsa2.PartOfSpeechRA, "msa POSes should not be equal.");
			Assert.IsFalse(stemMsa1.EqualsMsa(stemMsa2), "stemMsa1 should not be equal to stemMsa2 due to different POS");

			// reset POS
			stemMsa1.PartOfSpeechRA = stemMsa2.PartOfSpeechRA;
			Assert.IsTrue(stemMsa1.EqualsMsa(stemMsa2), "stemMsa1 & stemMsa2 should be equal with matching POS");

			// compare on different InflectionClass
			pos1.InflectionClassesOC.Add(new MoInflClass());
			pos2.InflectionClassesOC.Add(new MoInflClass());
			foreach (IMoInflClass mic in pos1.InflectionClassesOC)
			{
				stemMsa1.InflectionClassRA = mic;
				break;
			}
			foreach (IMoInflClass mic in pos2.InflectionClassesOC)
			{
				stemMsa2.InflectionClassRA = mic;
				break;
			}
			Assert.IsTrue(stemMsa1.InflectionClassRA != stemMsa2.InflectionClassRA, "inflection classes should not be equal.");
			Assert.IsFalse(stemMsa1.EqualsMsa(stemMsa2), "stemMsa1 should not be equal to stemMsa2 due to different inflection classes.");

			// reset InflectionClass
			stemMsa1.InflectionClassRA = stemMsa2.InflectionClassRA;
			Assert.IsTrue(stemMsa1.EqualsMsa(stemMsa2), "stemMsa1 & stemMsa2 should be equal with matching inflection classes");

			// compare different Productivity Restrictions
			ICmPossibility pr1 = lp.MorphologicalDataOA.ProdRestrictOA.PossibilitiesOS[0];
			ICmPossibility pr2 = lp.MorphologicalDataOA.ProdRestrictOA.PossibilitiesOS[1];

			stemMsa1.ProdRestrictRC.Add(pr1);
			stemMsa2.ProdRestrictRC.Add(pr2);
			Assert.IsFalse(stemMsa1.EqualsMsa(stemMsa2), "stemMsa1 should not be equal to stemMsa2 due to productivity restrictions differences.");

			stemMsa1.ProdRestrictRC.Add(pr2);
			Assert.IsFalse(stemMsa1.EqualsMsa(stemMsa2), "stemMsa1 should not be equal to stemMsa2 due to productivity restrictions count difference.");

			// Match Productivity Restrictions
			stemMsa2.ProdRestrictRC.Add(pr1);
			Assert.IsTrue(stemMsa1.EqualsMsa(stemMsa2), "stemMsa1 & stemMsa2 should be equal with matching productivity restrictions");

			// compare different MsFeatures
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(MoreCellarTests.ksFS1);
			XmlNode itemNeut = doc.SelectSingleNode("/item/item[3]");
			XmlNode itemFem = doc.SelectSingleNode("/item/item[2]");
			stemMsa1.MsFeaturesOA = Cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();
			stemMsa2.MsFeaturesOA = Cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();

			stemMsa1.MsFeaturesOA.AddFeatureFromXml(itemNeut, Cache.LangProject.MsFeatureSystemOA);
			stemMsa2.MsFeaturesOA.AddFeatureFromXml(itemFem, Cache.LangProject.MsFeatureSystemOA);

			Assert.IsFalse(stemMsa1.MsFeaturesOA.IsEquivalent(stemMsa2.MsFeaturesOA), "MsFeaturesOA should not be equal.");
			Assert.IsFalse(stemMsa1.EqualsMsa(stemMsa2), "stemMsa1 should not be equal to stemMsa2 due to different MsFeaturesOA.");

			// match feature structures
			stemMsa1.MsFeaturesOA.AddFeatureFromXml(itemFem, Cache.LangProject.MsFeatureSystemOA);
			Assert.IsTrue(stemMsa1.EqualsMsa(stemMsa2), "stemMsa1 & stemMsa2 should be equal with matching feature structure");
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void MoInflAffMsa_EqualMsa()
		{
			ILangProject lp = Cache.LangProject;
			ILexEntry le = MakeLexEntry("xyzTest1", "xyzDefn1.1");

			IMoInflAffMsa infAffixMsa1 = Cache.ServiceLocator.GetInstance<IMoInflAffMsaFactory>().Create();
			IMoInflAffMsa infAffixMsa2 = Cache.ServiceLocator.GetInstance<IMoInflAffMsaFactory>().Create();
			IMoStemMsa stemMsa = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create(); ;
			IMoDerivAffMsa derivAffixMsa = Cache.ServiceLocator.GetInstance<IMoDerivAffMsaFactory>().Create();
			IMoUnclassifiedAffixMsa unclassifiedAffixMsa = Cache.ServiceLocator.GetInstance<IMoUnclassifiedAffixMsaFactory>().Create();

			le.MorphoSyntaxAnalysesOC.Add(infAffixMsa1);
			le.MorphoSyntaxAnalysesOC.Add(infAffixMsa2);
			le.MorphoSyntaxAnalysesOC.Add(stemMsa);
			le.MorphoSyntaxAnalysesOC.Add(derivAffixMsa);
			le.MorphoSyntaxAnalysesOC.Add(unclassifiedAffixMsa);

			// Verify we fail comparing on different MSA types.
			Assert.IsFalse(infAffixMsa1.EqualsMsa(stemMsa));
			Assert.IsFalse(infAffixMsa1.EqualsMsa(derivAffixMsa));
			Assert.IsFalse(infAffixMsa1.EqualsMsa(unclassifiedAffixMsa));

			// Verify that infAffixMsa1 equals itself.
			Assert.IsTrue(infAffixMsa1.EqualsMsa(infAffixMsa1), infAffixMsa1.ToString() + " - should equal itself.");

			// Verify that infAffixMsa1 equals infAffixMsa2
			Assert.IsTrue(infAffixMsa1.EqualsMsa(infAffixMsa2), infAffixMsa1.ToString() + " - should equal - " + infAffixMsa2.ToString());

			// compare with on different PartOfSpeech
			IFdoOwningSequence<ICmPossibility> posSeq = lp.PartsOfSpeechOA.PossibilitiesOS;
			IPartOfSpeech pos1 = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create();
			posSeq.Add(pos1);
			IPartOfSpeech pos2 = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create();
			posSeq.Add(pos2);

			infAffixMsa1.PartOfSpeechRA = pos1;
			infAffixMsa2.PartOfSpeechRA = pos2;
			Assert.IsTrue(infAffixMsa1.PartOfSpeechRA != infAffixMsa2.PartOfSpeechRA, "msa POSes should not be equal.");
			Assert.IsFalse(infAffixMsa1.EqualsMsa(infAffixMsa2), "infAffixMsa1 should not be equal to infAffixMsa2 due to different POS");

			// reset POS
			infAffixMsa1.PartOfSpeechRA = infAffixMsa2.PartOfSpeechRA;
			Assert.IsTrue(infAffixMsa1.EqualsMsa(infAffixMsa2), "infAffixMsa1 & infAffixMsa2 should be equal with matching POS");

			// skip AffixCategory

			// compare different Slots
			IMoInflAffixSlot slot1 = Cache.ServiceLocator.GetInstance<IMoInflAffixSlotFactory>().Create();
			IMoInflAffixSlot slot2 = Cache.ServiceLocator.GetInstance<IMoInflAffixSlotFactory>().Create();
			pos1.AffixSlotsOC.Add(slot1);
			pos1.AffixSlotsOC.Add(slot2);

			infAffixMsa1.SlotsRC.Add(slot1);
			infAffixMsa2.SlotsRC.Add(slot2);
			Assert.IsFalse(infAffixMsa1.EqualsMsa(infAffixMsa2), "infAffixMsa1 should not be equal to infAffixMsa2 due to affix slots differences.");

			infAffixMsa1.SlotsRC.Add(slot2);
			Assert.IsTrue(infAffixMsa1.SlotsRC.Count != infAffixMsa2.SlotsRC.Count);
			Assert.IsFalse(infAffixMsa1.EqualsMsa(infAffixMsa2), "infAffixMsa1 should not be equal to infAffixMsa2 due to affix slots Count differences.");

			infAffixMsa2.SlotsRC.Add(slot1);
			Assert.IsTrue(infAffixMsa1.EqualsMsa(infAffixMsa2), "infAffixMsa1 should equal infAffixMsa2 due to affix slots matching.");

			// compare different FromProdRestrict
			ICmPossibility pr1 = lp.MorphologicalDataOA.ProdRestrictOA.PossibilitiesOS[0];
			ICmPossibility pr2 = lp.MorphologicalDataOA.ProdRestrictOA.PossibilitiesOS[1];

			infAffixMsa1.FromProdRestrictRC.Add(pr1);
			infAffixMsa2.FromProdRestrictRC.Add(pr2);
			Assert.IsFalse(infAffixMsa1.EqualsMsa(infAffixMsa2), "infAffixMsa1 should not be equal to infAffixMsa2 due to productivity restrictions differences.");

			infAffixMsa1.FromProdRestrictRC.Add(pr2);
			Assert.IsFalse(infAffixMsa1.EqualsMsa(infAffixMsa2), "infAffixMsa1 should not be equal to infAffixMsa2 due to productivity restrictions count difference.");

			// Match Productivity Restrictions
			infAffixMsa2.FromProdRestrictRC.Add(pr1);
			Assert.IsTrue(infAffixMsa1.EqualsMsa(infAffixMsa2), "infAffixMsa1 & infAffixMsa2 should be equal with matching productivity restrictions");

			// compare different InflFeats
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(MoreCellarTests.ksFS1);
			XmlNode itemNeut = doc.SelectSingleNode("/item/item[3]");
			XmlNode itemFem = doc.SelectSingleNode("/item/item[2]");
			infAffixMsa1.InflFeatsOA = Cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();
			infAffixMsa2.InflFeatsOA = Cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();
			infAffixMsa1.InflFeatsOA.AddFeatureFromXml(itemNeut, Cache.LangProject.MsFeatureSystemOA);
			infAffixMsa2.InflFeatsOA.AddFeatureFromXml(itemFem, Cache.LangProject.MsFeatureSystemOA);

			Assert.IsFalse(infAffixMsa1.InflFeatsOA.IsEquivalent(infAffixMsa2.InflFeatsOA), "InflFeatsOA should not be equal.");
			Assert.IsFalse(infAffixMsa1.EqualsMsa(infAffixMsa2), "infAffixMsa1 should not be equal to infAffixMsa2 due to different InflFeatsOA.");

			// match feature structures
			infAffixMsa1.InflFeatsOA.AddFeatureFromXml(itemFem, Cache.LangProject.MsFeatureSystemOA);
			Assert.IsTrue(infAffixMsa1.EqualsMsa(infAffixMsa2), "infAffixMsa1 & infAffixMsa2 should be equal with matching feature structure");
		}

		/// <summary>
		/// TODO: Add to tests when we start using MoDerivStepMsa.
		/// </summary>
		[Test]
		[Ignore("Add this test after we start using MoDerivStepMsa.")]
		public void MoDerivStepMsa_EqualMsa()
		{
			// Basically the same properties tested in MoStemMsa_EqualMsa()?
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void MoDerivAffMsa_EqualMsa()
		{
			ILangProject lp = Cache.LangProject;
			ILexEntry le = MakeLexEntry("xyzTest1", "xyzDefn1.1");

			IMoDerivAffMsa derivAffixMsa1 = Cache.ServiceLocator.GetInstance<IMoDerivAffMsaFactory>().Create();
			IMoDerivAffMsa derivAffixMsa2 = Cache.ServiceLocator.GetInstance<IMoDerivAffMsaFactory>().Create();
			IMoStemMsa stemMsa = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
			IMoInflAffMsa inflAffixMsa = Cache.ServiceLocator.GetInstance<IMoInflAffMsaFactory>().Create();
			IMoUnclassifiedAffixMsa unclassifiedAffixMsa = Cache.ServiceLocator.GetInstance<IMoUnclassifiedAffixMsaFactory>().Create();

			le.MorphoSyntaxAnalysesOC.Add(derivAffixMsa1);
			le.MorphoSyntaxAnalysesOC.Add(derivAffixMsa2);
			le.MorphoSyntaxAnalysesOC.Add(stemMsa);
			le.MorphoSyntaxAnalysesOC.Add(inflAffixMsa);
			le.MorphoSyntaxAnalysesOC.Add(unclassifiedAffixMsa);

			// Verify we fail comparing on different MSA types.
			Assert.IsFalse(derivAffixMsa1.EqualsMsa(stemMsa));
			Assert.IsFalse(derivAffixMsa1.EqualsMsa(inflAffixMsa));
			Assert.IsFalse(derivAffixMsa1.EqualsMsa(unclassifiedAffixMsa));

			// Verify that derivAffixMsa1 equals itself.
			Assert.IsTrue(derivAffixMsa1.EqualsMsa(derivAffixMsa1), derivAffixMsa1.ToString() + " - should equal itself.");

			// Verify that derivAffixMsa1 equals derivAffixMsa2
			Assert.IsTrue(derivAffixMsa1.EqualsMsa(derivAffixMsa2), derivAffixMsa1.ToString() + " - should equal - " + derivAffixMsa2.ToString());

			// compare with on different FromPartOfSpeech
			IFdoOwningSequence<ICmPossibility> posSeq = lp.PartsOfSpeechOA.PossibilitiesOS;
			IPartOfSpeech pos1 = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create();
			posSeq.Add(pos1);
			IPartOfSpeech pos2 = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create();
			posSeq.Add(pos2);
			derivAffixMsa1.FromPartOfSpeechRA = pos1;
			derivAffixMsa2.FromPartOfSpeechRA = pos2;
			Assert.IsTrue(derivAffixMsa1.FromPartOfSpeechRA != derivAffixMsa2.FromPartOfSpeechRA, "msa POSes should not be equal.");
			Assert.IsFalse(derivAffixMsa1.EqualsMsa(derivAffixMsa2), "derivAffixMsa1 should not be equal to derivAffixMsa2 due to different FromPartOfSpeech");

			// reset POS
			derivAffixMsa1.FromPartOfSpeechRA = derivAffixMsa2.FromPartOfSpeechRA;
			Assert.IsTrue(derivAffixMsa1.EqualsMsa(derivAffixMsa2), "derivAffixMsa1 & derivAffixMsa2 should be equal with matching POS");

			// compare with on different ToPartOfSpeech
			IPartOfSpeech pos3 = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create();
			posSeq.Add(pos3);
			IPartOfSpeech pos4 = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create();
			posSeq.Add(pos4);
			derivAffixMsa1.ToPartOfSpeechRA = pos3;
			derivAffixMsa2.ToPartOfSpeechRA = pos4;
			Assert.IsTrue(derivAffixMsa1.ToPartOfSpeechRA != derivAffixMsa2.ToPartOfSpeechRA, "msa POSes should not be equal.");
			Assert.IsFalse(derivAffixMsa1.EqualsMsa(derivAffixMsa2), "derivAffixMsa1 should not be equal to derivAffixMsa2 due to different ToPartOfSpeech");

			// reset POS
			derivAffixMsa1.ToPartOfSpeechRA = derivAffixMsa2.ToPartOfSpeechRA;
			Assert.IsTrue(derivAffixMsa1.EqualsMsa(derivAffixMsa2), "derivAffixMsa1 & derivAffixMsa2 should be equal with matching POS");

			// compare on different FromInflectionClass
			IMoInflClass mic1 = Cache.ServiceLocator.GetInstance<IMoInflClassFactory>().Create();
			pos1.InflectionClassesOC.Add(mic1);
			IMoInflClass mic2 = Cache.ServiceLocator.GetInstance<IMoInflClassFactory>().Create();
			pos2.InflectionClassesOC.Add(mic2);
			derivAffixMsa1.FromInflectionClassRA = mic1;
			derivAffixMsa2.FromInflectionClassRA = mic2;
			Assert.IsTrue(derivAffixMsa1.FromInflectionClassRA != derivAffixMsa2.FromInflectionClassRA, "inflection classes should not be equal.");
			Assert.IsFalse(derivAffixMsa1.EqualsMsa(derivAffixMsa2), "derivAffixMsa1 should not be equal to derivAffixMsa2 due to different inflection classes.");

			// reset InflectionClass
			derivAffixMsa1.FromInflectionClassRA = derivAffixMsa2.FromInflectionClassRA;
			Assert.IsTrue(derivAffixMsa1.EqualsMsa(derivAffixMsa2), "derivAffixMsa1 & derivAffixMsa2 should be equal with matching inflection classes");

			// compare on different FromStemName
			IMoStemName stname1 = Cache.ServiceLocator.GetInstance<IMoStemNameFactory>().Create();
			pos1.StemNamesOC.Add(stname1);
			IMoStemName stname2 = Cache.ServiceLocator.GetInstance<IMoStemNameFactory>().Create();
			pos2.StemNamesOC.Add(stname2);
			derivAffixMsa1.FromStemNameRA = stname1;
			derivAffixMsa2.FromStemNameRA = stname2;
			Assert.IsTrue(derivAffixMsa1.FromStemNameRA != derivAffixMsa2.FromStemNameRA, "stem names should not be equal.");
			Assert.IsFalse(derivAffixMsa1.EqualsMsa(derivAffixMsa2), "derivAffixMsa1 should not be equal to derivAffixMsa2 due to different stem names.");

			// reset StemName
			derivAffixMsa1.FromStemNameRA = derivAffixMsa2.FromStemNameRA;
			Assert.IsTrue(derivAffixMsa1.EqualsMsa(derivAffixMsa2), "derivAffixMsa1 & derivAffixMsa2 should be equal with matching stem names");

			// compare on different ToInflectionClass
			IMoInflClass mic3 = Cache.ServiceLocator.GetInstance<IMoInflClassFactory>().Create();
			pos3.InflectionClassesOC.Add(mic3);
			IMoInflClass mic4 = Cache.ServiceLocator.GetInstance<IMoInflClassFactory>().Create();
			pos4.InflectionClassesOC.Add(mic4);
			derivAffixMsa1.ToInflectionClassRA = mic3;
			derivAffixMsa2.ToInflectionClassRA = mic4;
			Assert.IsTrue(derivAffixMsa1.ToInflectionClassRA != derivAffixMsa2.ToInflectionClassRA, "inflection classes should not be equal.");
			Assert.IsFalse(derivAffixMsa1.EqualsMsa(derivAffixMsa2), "derivAffixMsa1 should not be equal to derivAffixMsa2 due to different inflection classes.");

			// reset InflectionClass
			derivAffixMsa1.ToInflectionClassRA = derivAffixMsa2.ToInflectionClassRA;
			Assert.IsTrue(derivAffixMsa1.EqualsMsa(derivAffixMsa2), "derivAffixMsa1 & derivAffixMsa2 should be equal with matching inflection classes");

			// compare different FromProdRestrict
			ICmPossibility pr1 = lp.MorphologicalDataOA.ProdRestrictOA.PossibilitiesOS[0];
			ICmPossibility pr2 = lp.MorphologicalDataOA.ProdRestrictOA.PossibilitiesOS[1];

			derivAffixMsa1.FromProdRestrictRC.Add(pr1);
			derivAffixMsa2.FromProdRestrictRC.Add(pr2);
			Assert.IsFalse(derivAffixMsa1.EqualsMsa(derivAffixMsa2), "derivAffixMsa1 should not be equal to derivAffixMsa2 due to productivity restrictions differences.");

			derivAffixMsa1.FromProdRestrictRC.Add(pr2);
			Assert.IsFalse(derivAffixMsa1.EqualsMsa(derivAffixMsa2), "derivAffixMsa1 should not be equal to derivAffixMsa2 due to productivity restrictions count difference.");

			// Match Productivity Restrictions
			derivAffixMsa2.FromProdRestrictRC.Add(pr1);
			Assert.IsTrue(derivAffixMsa1.EqualsMsa(derivAffixMsa2), "derivAffixMsa1 & derivAffixMsa2 should be equal with matching productivity restrictions");

			// compare different ToProdRestrict
			ICmPossibility pr3 = lp.MorphologicalDataOA.ProdRestrictOA.PossibilitiesOS[2];
			ICmPossibility pr4 = lp.MorphologicalDataOA.ProdRestrictOA.PossibilitiesOS[3];

			derivAffixMsa1.ToProdRestrictRC.Add(pr3);
			derivAffixMsa2.ToProdRestrictRC.Add(pr4);
			Assert.IsFalse(derivAffixMsa1.EqualsMsa(derivAffixMsa2), "derivAffixMsa1 should not be equal to derivAffixMsa2 due to productivity restrictions differences.");

			derivAffixMsa1.ToProdRestrictRC.Add(pr4);
			Assert.IsFalse(derivAffixMsa1.EqualsMsa(derivAffixMsa2), "derivAffixMsa1 should not be equal to derivAffixMsa2 due to productivity restrictions count difference.");

			// Match Productivity Restrictions
			derivAffixMsa2.ToProdRestrictRC.Add(pr3);
			Assert.IsTrue(derivAffixMsa1.EqualsMsa(derivAffixMsa2), "derivAffixMsa1 & derivAffixMsa2 should be equal with matching productivity restrictions");

			// compare different FromMsFeatures
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(MoreCellarTests.ksFS1);
			XmlNode itemNeut = doc.SelectSingleNode("/item/item[3]");
			XmlNode itemFem = doc.SelectSingleNode("/item/item[2]");
			derivAffixMsa1.FromMsFeaturesOA = Cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();
			derivAffixMsa2.FromMsFeaturesOA = Cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();
			derivAffixMsa1.FromMsFeaturesOA.AddFeatureFromXml(itemNeut, Cache.LangProject.MsFeatureSystemOA);
			derivAffixMsa2.FromMsFeaturesOA.AddFeatureFromXml(itemFem, Cache.LangProject.MsFeatureSystemOA);

			Assert.IsFalse(derivAffixMsa1.FromMsFeaturesOA.IsEquivalent(derivAffixMsa2.FromMsFeaturesOA), "FromMsFeaturesOA should not be equal.");
			Assert.IsFalse(derivAffixMsa1.EqualsMsa(derivAffixMsa2), "derivAffixMsa1 should not be equal to derivAffixMsa2 due to different FromMsFeaturesOA.");

			// match feature structures
			derivAffixMsa1.FromMsFeaturesOA.AddFeatureFromXml(itemFem, Cache.LangProject.MsFeatureSystemOA);
			Assert.IsTrue(derivAffixMsa1.EqualsMsa(derivAffixMsa2), "derivAffixMsa1 & derivAffixMsa2 should be equal with matching feature structure");

			// compare different ToMsFeatures
			derivAffixMsa1.ToMsFeaturesOA = Cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();
			derivAffixMsa2.ToMsFeaturesOA = Cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();
			derivAffixMsa1.ToMsFeaturesOA.AddFeatureFromXml(itemFem, Cache.LangProject.MsFeatureSystemOA);
			derivAffixMsa2.ToMsFeaturesOA.AddFeatureFromXml(itemNeut, Cache.LangProject.MsFeatureSystemOA);

			Assert.IsFalse(derivAffixMsa1.ToMsFeaturesOA.IsEquivalent(derivAffixMsa2.ToMsFeaturesOA), "ToMsFeaturesOA should not be equal.");
			Assert.IsFalse(derivAffixMsa1.EqualsMsa(derivAffixMsa2), "derivAffixMsa1 should not be equal to derivAffixMsa2 due to different ToMsFeaturesOA.");

			// match feature structures
			derivAffixMsa1.ToMsFeaturesOA.AddFeatureFromXml(itemNeut, Cache.LangProject.MsFeatureSystemOA);
			Assert.IsTrue(derivAffixMsa1.EqualsMsa(derivAffixMsa2), "derivAffixMsa1 & derivAffixMsa2 should be equal with matching feature structure");
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void MoUnclassifiedAffixMsa_EqualMsa()
		{
			ILangProject lp = Cache.LangProject;
			ILexEntry le = MakeLexEntry("xyzTest1", "xyzDefn1.1");

			IMoUnclassifiedAffixMsa unclassifiedAffixMsa1 = Cache.ServiceLocator.GetInstance<IMoUnclassifiedAffixMsaFactory>().Create();
			IMoUnclassifiedAffixMsa unclassifiedAffixMsa2 = Cache.ServiceLocator.GetInstance<IMoUnclassifiedAffixMsaFactory>().Create();
			IMoStemMsa stemMsa = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
			IMoInflAffMsa inflAffixMsa = Cache.ServiceLocator.GetInstance<IMoInflAffMsaFactory>().Create();
			IMoDerivAffMsa derivAffixMsa = Cache.ServiceLocator.GetInstance<IMoDerivAffMsaFactory>().Create();

			le.MorphoSyntaxAnalysesOC.Add(unclassifiedAffixMsa1);
			le.MorphoSyntaxAnalysesOC.Add(unclassifiedAffixMsa2);
			le.MorphoSyntaxAnalysesOC.Add(stemMsa);
			le.MorphoSyntaxAnalysesOC.Add(inflAffixMsa);
			le.MorphoSyntaxAnalysesOC.Add(derivAffixMsa);

			// Verify we fail comparing on different MSA types.
			Assert.IsFalse(unclassifiedAffixMsa1.EqualsMsa(stemMsa));
			Assert.IsFalse(unclassifiedAffixMsa1.EqualsMsa(inflAffixMsa));
			Assert.IsFalse(unclassifiedAffixMsa1.EqualsMsa(derivAffixMsa));

			// Verify that unclassifiedAffixMsa1 equals itself.
			Assert.IsTrue(unclassifiedAffixMsa1.EqualsMsa(unclassifiedAffixMsa1), unclassifiedAffixMsa1.ToString() + " - should equal itself.");

			// Verify that unclassifiedAffixMsa1 equals unclassifiedAffixMsa2
			Assert.IsTrue(unclassifiedAffixMsa1.EqualsMsa(unclassifiedAffixMsa2), unclassifiedAffixMsa1.ToString() + " - should equal - " + unclassifiedAffixMsa2.ToString());

			// compare with on different PartOfSpeech
			IFdoOwningSequence<ICmPossibility> posSeq = lp.PartsOfSpeechOA.PossibilitiesOS;
			IPartOfSpeech pos1 = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create();
			posSeq.Add(pos1);
			IPartOfSpeech pos2 = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create();
			posSeq.Add(pos2);

			unclassifiedAffixMsa1.PartOfSpeechRA = pos1;
			unclassifiedAffixMsa2.PartOfSpeechRA = pos2;
			Assert.IsTrue(unclassifiedAffixMsa1.PartOfSpeechRA != unclassifiedAffixMsa2.PartOfSpeechRA, "msa POSes should not be equal.");
			Assert.IsFalse(unclassifiedAffixMsa1.EqualsMsa(unclassifiedAffixMsa2), "unclassifiedAffixMsa1 should not be equal to unclassifiedAffixMsa2 due to different POS");

			// reset POS
			unclassifiedAffixMsa1.PartOfSpeechRA = unclassifiedAffixMsa2.PartOfSpeechRA;
			Assert.IsTrue(unclassifiedAffixMsa1.EqualsMsa(unclassifiedAffixMsa2), "unclassifiedAffixMsa1 & unclassifiedAffixMsa2 should be equal with matching POS");
		}

	}

	#endregion EqualMsaTests

	/// <summary>
	/// Test stuff related to LexDb.
	/// </summary>
	[TestFixture]
	public class LexDbTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{

		/// <summary>
		/// Test that PublicationTypes automatically creates it if needed.
		/// </summary>
		[Test]
		public void PublicationTypesCreation()
		{
			var publicationTypes = Cache.LangProject.LexDbOA.PublicationTypesOA;
			Assert.That(publicationTypes, Is.Not.Null);
			Assert.That(publicationTypes, Is.EqualTo(Cache.LangProject.LexDbOA.PublicationTypesOA), "should not make a new one each time!");
			var wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");

			Assert.That(publicationTypes.Name.get_String(wsEn).Text, Is.EqualTo("Publications"));
			Assert.That(publicationTypes.PossibilitiesOS.Count, Is.EqualTo(1));
			var mainDict = publicationTypes.PossibilitiesOS[0];
			Assert.That(mainDict.Name.get_String(wsEn).Text, Is.EqualTo("Main Dictionary"));
			Assert.That(mainDict.IsProtected, Is.True);
			// There are various other properties we set but it seems merely redundant to verify them all.
		}

		/// <summary>
		/// Test that PublicationTypes can be created on demand even if no UOW is current.
		/// </summary>
		[Test]
		public void PublicationTypesCreationNeedingUow()
		{
			Cache.ActionHandlerAccessor.EndUndoTask(); // terminate the one started in the setup code.
			var publicationTypes = Cache.LangProject.LexDbOA.PublicationTypesOA;
			Assert.That(publicationTypes, Is.Not.Null);
		}
		/// <summary>
		/// Test conversion of LexEntryType to LexEntryInflType.
		/// </summary>
		[Test]
		public void ConvertLexEntryInflTypes()
		{
			var variantEntryTypes = Cache.LangProject.LexDbOA.VariantEntryTypesOA;
			Assert.That(variantEntryTypes, Is.Not.Null);
			Assert.That(variantEntryTypes, Is.EqualTo(Cache.LangProject.LexDbOA.VariantEntryTypesOA), "should not make a new one each time!");
			var wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
			Assert.That(variantEntryTypes.PossibilitiesOS.Count, Is.EqualTo(6));
			var letFactory = Cache.ServiceLocator.GetInstance<ILexEntryTypeFactory>();
			var lexEntryType1 = variantEntryTypes.PossibilitiesOS[0] as ILexEntryType;
			var lexEntryType2 = variantEntryTypes.PossibilitiesOS[1] as ILexEntryType;
			var lexEntryType1Sub1 = letFactory.Create();
			lexEntryType1.SubPossibilitiesOS.Add(lexEntryType1Sub1);

			var leFactory = Cache.ServiceLocator.GetInstance<ILexEntryFactory>();
			var lerFactory = Cache.ServiceLocator.GetInstance<ILexEntryRefFactory>();

			var entry1 = leFactory.Create();
			var ler1 = lerFactory.Create();
			entry1.EntryRefsOS.Add(ler1);
			ler1.VariantEntryTypesRS.Add(lexEntryType1);
			ler1.VariantEntryTypesRS.Add(lexEntryType2);

			var entry2 = leFactory.Create();
			var ler2 = lerFactory.Create();
			entry2.EntryRefsOS.Add(ler2);
			ler2.VariantEntryTypesRS.Add(lexEntryType1);

			using (var progressBar = new ProgressBar())
			{
				var itemsToChange = new List<ILexEntryType>();
				itemsToChange.Add(lexEntryType1);
				itemsToChange.Add(lexEntryType1Sub1);

				Cache.LangProject.LexDbOA.ConvertLexEntryInflTypes(progressBar, itemsToChange);
				var leit1 = ler1.VariantEntryTypesRS[0];
				Assert.AreEqual(LexEntryInflTypeTags.kClassId, leit1.ClassID, "first lex entry type of first entry should be irregularly inflected form");
				leit1 = ler1.VariantEntryTypesRS[1];
				Assert.AreEqual(LexEntryTypeTags.kClassId, leit1.ClassID, "second lex entry type of first entry should be variant");
				leit1 = ler2.VariantEntryTypesRS[0];
				Assert.AreEqual(LexEntryInflTypeTags.kClassId, leit1.ClassID, "first lex entry type of second entry should be irregularly inflected form");

				variantEntryTypes = Cache.LangProject.LexDbOA.VariantEntryTypesOA;
				Assert.That(variantEntryTypes, Is.Not.Null);
				Assert.That(variantEntryTypes, Is.EqualTo(Cache.LangProject.LexDbOA.VariantEntryTypesOA), "should not make a new one each time!");
				Assert.That(variantEntryTypes.PossibilitiesOS.Count, Is.EqualTo(6));

				lexEntryType1 = variantEntryTypes.PossibilitiesOS[0] as ILexEntryType;
				Assert.AreEqual(LexEntryInflTypeTags.kClassId, lexEntryType1.ClassID, "first type should be irregularly inflected form");
				lexEntryType1Sub1 = lexEntryType1.SubPossibilitiesOS[0] as ILexEntryType;
				Assert.AreEqual(LexEntryInflTypeTags.kClassId, lexEntryType1Sub1.ClassID, "first sub-type of first type should be irregularly inflected form");
			}
		}
		/// <summary>
		/// Test conversion of LexEntryInflType to LexEntryType.
		/// </summary>
		[Test]
		public void ConvertLexEntryTypes()
		{
			var variantEntryTypes = Cache.LangProject.LexDbOA.VariantEntryTypesOA;
			Assert.That(variantEntryTypes, Is.Not.Null);
			Assert.That(variantEntryTypes, Is.EqualTo(Cache.LangProject.LexDbOA.VariantEntryTypesOA), "should not make a new one each time!");
			Assert.That(variantEntryTypes.PossibilitiesOS.Count, Is.EqualTo(6));
			var leitFactory = Cache.ServiceLocator.GetInstance<ILexEntryInflTypeFactory>();
			var lexEntryInflType1 = variantEntryTypes.PossibilitiesOS[3] as ILexEntryInflType;
			var lexEntryInflType2 = variantEntryTypes.PossibilitiesOS[4] as ILexEntryInflType;
			var lexEntryInflType1Sub1 = leitFactory.Create();
			lexEntryInflType1.SubPossibilitiesOS.Insert(0, lexEntryInflType1Sub1);

			var leFactory = Cache.ServiceLocator.GetInstance<ILexEntryFactory>();
			var lerFactory = Cache.ServiceLocator.GetInstance<ILexEntryRefFactory>();

			var entry1 = leFactory.Create();
			var ler1 = lerFactory.Create();
			entry1.EntryRefsOS.Add(ler1);
			ler1.VariantEntryTypesRS.Add(lexEntryInflType1);
			ler1.VariantEntryTypesRS.Add(lexEntryInflType2);

			var entry2 = leFactory.Create();
			var ler2 = lerFactory.Create();
			entry2.EntryRefsOS.Add(ler2);
			ler2.VariantEntryTypesRS.Add(lexEntryInflType1);

			using (var progressBar = new ProgressBar())
			{
				var itemsToChange = new List<ILexEntryType>();
				itemsToChange.Add(lexEntryInflType1);
				itemsToChange.Add(lexEntryInflType1Sub1);

				Cache.LangProject.LexDbOA.ConvertLexEntryTypes(progressBar, itemsToChange);
				var let1 = ler1.VariantEntryTypesRS[0];
				Assert.AreEqual(LexEntryTypeTags.kClassId, let1.ClassID, "first lex entry type of first entry should be variant");
				let1 = ler1.VariantEntryTypesRS[1];
				Assert.AreEqual(LexEntryInflTypeTags.kClassId, let1.ClassID, "second lex entry type of first entry should be irregularly inflected form");
				let1 = ler2.VariantEntryTypesRS[0];
				Assert.AreEqual(LexEntryTypeTags.kClassId, let1.ClassID, "first lex entry type of second entry should be variant");

				variantEntryTypes = Cache.LangProject.LexDbOA.VariantEntryTypesOA;
				Assert.That(variantEntryTypes, Is.Not.Null);
				Assert.That(variantEntryTypes, Is.EqualTo(Cache.LangProject.LexDbOA.VariantEntryTypesOA), "should not make a new one each time!");
				Assert.That(variantEntryTypes.PossibilitiesOS.Count, Is.EqualTo(6));

				var lexEntryType1 = variantEntryTypes.PossibilitiesOS[3] as ILexEntryType;
				Assert.AreEqual(LexEntryTypeTags.kClassId, lexEntryType1.ClassID, "third type should be variant");
				var lexEntryType1Sub1 = lexEntryType1.SubPossibilitiesOS[0] as ILexEntryType;
				Assert.AreEqual(LexEntryTypeTags.kClassId, lexEntryType1Sub1.ClassID, "third's first type should be variant");
			}
		}
	}
}

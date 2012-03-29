// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: SandboxBase.cs
// Responsibility: pyle
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// <summary>
	/// </summary>
	[TestFixture]
	public class MatchingMorphsLogicTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private SetupMatchingMorphs m_matchingMorphs;

		/// <summary>
		///
		/// </summary>
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor,
			   DoSetupFixture);
		}

		/// <summary>
		///
		/// </summary>
		public override void FixtureTeardown()
		{
			if (m_matchingMorphs != null)
				m_matchingMorphs.Dispose();
			m_matchingMorphs = null;
			base.FixtureTeardown();
		}

		void DoSetupFixture()
		{

			m_matchingMorphs = new SetupMatchingMorphs(this);
		}

		/// <summary>
		/// We initially tried to do SetupMatchingMorphs on a per test basis,
		/// but discovered that Undo wasn't undoing everything we expected.
		/// (See UndoAllIssueTest)
		/// </summary>
		internal class SetupMatchingMorphsDisabled : SetupMatchingMorphs
		{
			internal SetupMatchingMorphsDisabled(MatchingMorphsLogicTests testFixture)
			{

			}

			protected override void DoSetup()
			{
				// don't do any data setup.
			}

		}

		// REVIEW (TomB): There is no reason to derive from FwDisposableBase because neither
		// Dispose method is being overriden. Either override one of those methods or get rid
		// of all the using statements where objects of this class are instantiated.
		internal class SetupMatchingMorphs : FwDisposableBase
		{
			internal SetupMatchingMorphs()
			{

			}
			internal SetupMatchingMorphs(MatchingMorphsLogicTests testFixture)
			{
				Cache = testFixture.Cache;
				DoSetup();
			}

			protected virtual void DoSetup()
			{
				ILexEntry newEntry;
				ILexSense newSense;
				var msa1 = new SandboxGenericMSA();
				msa1.MsaType = MsaType.kInfl;
				SetupLexEntryAndSense(Cache, "ipr-", "inflectional prefix", msa1, out newEntry, out newSense);
				SetupLexEntryAndSense(Cache, "-isu", "inflectional suffix", msa1, out newEntry, out newSense);
				SetupLexEntryAndSense(Cache, "-ii-", "inflectional infix", msa1, out newEntry, out newSense);
				var msa2 = new SandboxGenericMSA();
				msa2.MsaType = MsaType.kStem;
				SetupLexEntryAndSense(Cache, "s", "stem1", msa1, out newEntry, out newSense);
				AddAllomorph<IMoStemAllomorphFactory, IMoStemAllomorph>(newEntry, "sa", newEntry.LexemeFormOA.MorphTypeRA);
				SetupLexEntryAndSense(Cache, "s", "stem2", msa1, out newEntry, out newSense);
				var morphTypeEnclitic =
					Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(
						MoMorphTypeTags.kguidMorphEnclitic);
				var morphTypeProclitic =
					Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(
						MoMorphTypeTags.kguidMorphProclitic);
				AddAllomorph<IMoStemAllomorphFactory, IMoStemAllomorph>(newEntry, "pro", morphTypeProclitic);
				AddAllomorph<IMoStemAllomorphFactory, IMoStemAllomorph>(newEntry, "enc", morphTypeEnclitic);
			}

			FdoCache Cache { get; set; }

			private void SetupPartsOfSpeech()
			{
				// setup language project parts of speech
				var partOfSpeechFactory = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>();
				var adjunct = partOfSpeechFactory.Create();
				var noun = partOfSpeechFactory.Create();
				var verb = partOfSpeechFactory.Create();
				var transitiveVerb = partOfSpeechFactory.Create();
				Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Add(adjunct);
				Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Add(noun);
				Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Add(verb);
				verb.SubPossibilitiesOS.Add(transitiveVerb);
				adjunct.Name.set_String(Cache.DefaultAnalWs, "adjunct");
				noun.Name.set_String(Cache.DefaultAnalWs, "noun");
				verb.Name.set_String(Cache.DefaultAnalWs, "verb");
				transitiveVerb.Name.set_String(Cache.DefaultAnalWs, "transitive verb");
			}

			static internal void SetupLexEntryAndSense(FdoCache cache, string fullForm, string senseGloss, SandboxGenericMSA msa,
				out ILexEntry lexEntry1_Entry, out ILexSense lexEntry1_Sense1)
			{
				lexEntry1_Entry = cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(fullForm, senseGloss, msa);
				lexEntry1_Sense1 = lexEntry1_Entry.SensesOS[0];
			}

			static internal void AddAllomorph<TAllomorphFactory, TItem>(ILexEntry entry, string allomorphForm, IMoMorphType morphType)
				where TAllomorphFactory : IFdoFactory<TItem>
				where TItem : IMoForm
			{
				var Cache = entry.Cache;
				ITsString tssAllomorphForm = TsStringUtils.MakeTss(allomorphForm, Cache.DefaultVernWs);
				TAllomorphFactory stemFactory = Cache.ServiceLocator.GetInstance<TAllomorphFactory>();
				var allomorph = stemFactory.Create();
				entry.AlternateFormsOS.Add(allomorph);
				allomorph.MorphTypeRA = morphType;
				allomorph.Form.set_String(TsStringUtils.GetWsAtOffset(tssAllomorphForm, 0), tssAllomorphForm);
			}
		}

		private void CheckExpected<TMorphExpected>(int expectedMatchCount,
			string prefixMarker, string form, string postfixMarker)
		{
			var targetForm = TsStringUtils.MakeTss(form, Cache.DefaultVernWs);
			var matches = MorphServices.GetMatchingMorphs(Cache,
				prefixMarker, targetForm, postfixMarker);
			Assert.AreEqual(expectedMatchCount, matches.Count());
			foreach (var match in matches)
				Assert.IsTrue(match is TMorphExpected, "Expected class " + typeof(TMorphExpected).Name);
		}

		private void CheckEmpty(string prefixMarker, string form, string postfixMarker)
		{
			ITsString tssForm = TsStringUtils.MakeTss(form, Cache.DefaultVernWs);
			var emptySet = MorphServices.GetMatchingMorphs(Cache,
																		prefixMarker, tssForm, postfixMarker);
			Assert.AreEqual(0, emptySet.Count());
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void MatchingMorphs_Stems()
		{
			using (new SetupMatchingMorphsDisabled(this))
			{
				// match stem morphs
				var stems = MorphServices.GetMatchingMorphs(Cache,
					"", TsStringUtils.MakeTss("s", Cache.DefaultVernWs), "");
				Assert.AreEqual(2, stems.Count());
				foreach (var stem in stems)
					Assert.IsTrue(stem is IMoStemAllomorph, "Expected stems");
				// make sure we don't get any results matching prefix/postfix markers.
				CheckEmpty("-", "s", "");
				CheckEmpty("",  "s", "-");
				CheckEmpty("-", "s", "-");
			}
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void MatchingMorphs_StemAllomorphs()
		{
			using (new SetupMatchingMorphsDisabled(this))
			{
				// match stem morphs
				var stemAllomorphs = MorphServices.GetMatchingMorphs(Cache,
					"", TsStringUtils.MakeTss("sa", Cache.DefaultVernWs), "");
				Assert.AreEqual(1, stemAllomorphs.Count());
				foreach (var stem in stemAllomorphs)
					Assert.IsTrue(stem is IMoStemAllomorph, "Expected stem allomorph");
				// make sure we don't get any results matching prefix/postfix markers.
				CheckEmpty("-", "sa", "");
				CheckEmpty("", "sa", "-");
				CheckEmpty("-", "sa", "-");
			}
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void MatchingMorphs_Prefixes()
		{
			using (new SetupMatchingMorphsDisabled(this))
			{
				var prefixes = MorphServices.GetMatchingMorphs(Cache,
					"", TsStringUtils.MakeTss("ipr", Cache.DefaultVernWs), "-");
				Assert.AreEqual(1, prefixes.Count());
				foreach (var prefix in prefixes)
					Assert.IsTrue(prefix is IMoAffixAllomorph, "Expected affix");
				CheckEmpty("",  "ipr", "");
				CheckEmpty("-", "ipr", "");
				CheckEmpty("-", "ipr", "-");
			}
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void MatchingMorphs_Suffixes()
		{
			using (new SetupMatchingMorphsDisabled(this))
			{
				var suffixes = MorphServices.GetMatchingMorphs(Cache,
					"-", TsStringUtils.MakeTss("isu", Cache.DefaultVernWs), "");
				Assert.AreEqual(1, suffixes.Count());
				foreach (var suffix in suffixes)
					Assert.IsTrue(suffix is IMoAffixAllomorph, "Expected affix");
				CheckEmpty("",  "isu", "");
				CheckEmpty("",  "isu", "-");
				CheckEmpty("-", "isu", "-");
			}
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void MatchingMorphs_Proclitic()
		{
			using (new SetupMatchingMorphsDisabled(this))
			{
				var procliticForm = TsStringUtils.MakeTss("pro", Cache.DefaultVernWs);
				var proclitics = MorphServices.GetMatchingMorphs(Cache,
					"", procliticForm, "=");
				Assert.AreEqual(1, proclitics.Count());
				foreach (var proclitic in proclitics)
					Assert.IsTrue(proclitic is IMoStemAllomorph, "Expected proclitic");
				// check that it's okay to match without postfix marker
				CheckExpected<IMoStemAllomorph>(1, "", "pro", "");
				CheckEmpty("-", "pro", "-");
				CheckEmpty("-", "pro", "");
				CheckEmpty("",  "pro", "-");
			}
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void MatchingMorphs_Enclitic()
		{
			using (new SetupMatchingMorphsDisabled(this))
			{
				var encliticForm = TsStringUtils.MakeTss("enc", Cache.DefaultVernWs);
				var enclitics = MorphServices.GetMatchingMorphs(Cache,
					"=", encliticForm, "");
				Assert.AreEqual(1, enclitics.Count());
				foreach (var enclitic in enclitics)
					Assert.IsTrue(enclitic is IMoStemAllomorph, "Expected enclitic");
				// check that it's okay to match without postfix marker
				CheckExpected<IMoStemAllomorph>(1, "", "enc", "");
				CheckEmpty("-", "enc", "-");
				CheckEmpty("-", "enc", "");
				CheckEmpty("", "enc", "-");
			}
		}

	}

	/// <summary>
	///
	/// </summary>
	[TestFixture]
	public class InflectionalVariantTests : MemoryOnlyBackendProviderReallyRestoredForEachTestTestBase
	{
		/// <summary>
		/// Setup the data for this test after the cache has been setup.
		/// </summary>
		public override void TestSetup()
		{
			base.TestSetup();
		}

		/// <summary>
		///
		/// </summary>
		private void DoSetup()
		{
			ILexEntry newMainEntry;
			ILexEntry newDummyEntry;
			ILexSense newDummySense;
			var msaStem = new SandboxGenericMSA();
			msaStem.MsaType = MsaType.kStem;
			MatchingMorphsLogicTests.SetupMatchingMorphs.SetupLexEntryAndSense(Cache, "mainEntry", "mainEntrySense", msaStem, out newMainEntry, out newDummySense);
			MatchingMorphsLogicTests.SetupMatchingMorphs.AddAllomorph<IMoStemAllomorphFactory, IMoStemAllomorph>(newMainEntry, "mainEntryAllomorph1", newMainEntry.LexemeFormOA.MorphTypeRA);

			// Setup variant data
			const string variantTypeNameIIV = "Irregular Inflectional Variant";
			var eng = Cache.ServiceLocator.WritingSystemManager.UserWritingSystem;
			string variantTypeName = variantTypeNameIIV;

			ILexEntryType letIrrInflVariantType = LookupLexEntryType(Cache.ServiceLocator, variantTypeNameIIV, eng);
			ILexEntryRef newDummyLer;
			SetupLexEntryVariant(Cache, "vaIrrInflVar", newMainEntry, letIrrInflVariantType, out newDummyLer);

			ITsStrFactory sf = TsStrFactoryClass.Create();
			// Create new variantType
			var variantTypesList = Cache.LanguageProject.LexDbOA.VariantEntryTypesOA;
			ILexEntryType letNewPlural = InsertInflType(letIrrInflVariantType, Cache.ServiceLocator,
														sf.MakeString("NewPlural", eng.Handle),
														sf.MakeString("NPl.", eng.Handle),
														"", ".PL");
			ILexEntryType letNewPast = InsertInflType(letIrrInflVariantType, Cache.ServiceLocator,
													  sf.MakeString("NewPast", eng.Handle),
													  sf.MakeString("NPst.", eng.Handle),
													  "", ".PST");
			ILexEntryRef newLerPlural;
			SetupLexEntryVariant(Cache, "vaNewPlural", newMainEntry, letNewPlural, out newLerPlural);
			ILexEntryRef newLerPast;
			SetupLexEntryVariant(Cache, "vaNewPast", newMainEntry, letNewPast, out newLerPast);
		}

		static internal ILexEntryInflType InsertInflType<TOwner>(TOwner owningObj, IFdoServiceLocator sl, ITsString variantTypeName, ITsString abbr,
			string glossPrepend, string glossAppend)
			where TOwner : ICmObject
		{
			var leitfFactory = sl.GetInstance<ILexEntryInflTypeFactory>();

			ILexEntryInflType let = null;
			if (typeof(TOwner) is ICmPossibilityList)
			{
				var owningList = sl.GetInstance<ILexDb>().VariantEntryTypesOA;
				owningList.PossibilitiesOS.Insert(0, leitfFactory.Create());
				let = owningList.PossibilitiesOS[0] as ILexEntryInflType;
			}
			else if (owningObj is ICmPossibility)
			{
				(owningObj as ICmPossibility).SubPossibilitiesOS.Insert(0, leitfFactory.Create());
				let = (owningObj as ICmPossibility).SubPossibilitiesOS[0] as ILexEntryInflType;
			}

			let.Name.set_String(TsStringUtils.GetWsAtOffset(variantTypeName, 0), variantTypeName);
			let.Abbreviation.set_String(TsStringUtils.GetWsAtOffset(abbr, 0), abbr);

			var wsGlossDefault = TsStringUtils.GetWsAtOffset(variantTypeName, 0);
			let.GlossPrepend.set_String(wsGlossDefault, glossPrepend);
			let.GlossAppend.set_String(wsGlossDefault, glossAppend);

			//var possFactory = sl.GetInstance<ICmPossibilityFactory>();
			//var dataReader = (IDataReader)sl.GetInstance<IDataSetup>();
			//var leitfFactoryInternal = sl.GetInstance<ILexEntryInflTypeFactoryInternal>();
			//leitfFactoryInternal.Create(
			//        new Guid(),
			//        dataReader.GetNextRealHvo(),
			//        owningList,
			//        0, // owningList.PossibilitiesOS.Count - 1, // Zero based ord.
			//        variantTypeName, TsStringUtils.GetWsAtOffset(variantTypeName, 0),
			//        abbr, TsStringUtils.GetWsAtOffset(abbr, 0));););

			return let;
		}

		static internal ILexEntryType LookupLexEntryType(IFdoServiceLocator sl, string variantTypeName, IWritingSystem ws)
		{
			var eng = sl.WritingSystemManager.UserWritingSystem;
			var letRepo = sl.GetInstance<ILexEntryTypeRepository>();
			ILexEntryType let = letRepo.AllInstances().Where(
				someposs => someposs.Name.get_String(ws.Handle).Text == variantTypeName).FirstOrDefault();

			return let;
		}


		static internal void SetupLexEntryVariant(FdoCache cache, string morphForm, ILexEntry lexEntry_mainEntry, ILexEntryType let, out ILexEntryRef lef)
		{
			ITsStrFactory sf = TsStrFactoryClass.Create();
			ITsString mf = sf.MakeString(morphForm, cache.DefaultVernWs);
			lef = lexEntry_mainEntry.CreateVariantEntryAndBackRef(let, mf);
		}

		private void SetupBoundStemAndMainEntryWithVariant()
		{
			ILexEntry newMainEntry;
			ILexEntry newBoundStem;
			ILexEntry newDummyEntry; ILexSense newDummySense;
			var msaStem = new SandboxGenericMSA() {MsaType = MsaType.kStem};
			MatchingMorphsLogicTests.SetupMatchingMorphs.SetupLexEntryAndSense(Cache, "boundStem", "boundStemSense", msaStem, out newBoundStem, out newDummySense);
			newBoundStem.LexemeFormOA.MorphTypeRA = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphBoundStem);
			MatchingMorphsLogicTests.SetupMatchingMorphs.SetupLexEntryAndSense(Cache, "mainEntry", "mainEntrySense", msaStem, out newMainEntry, out newDummySense);

			// Setup variant data
			const string variantTypeNameIIV = "Irregular Inflectional Variant";
			var eng = Cache.ServiceLocator.WritingSystemManager.UserWritingSystem;
			string variantTypeName = variantTypeNameIIV;

			ILexEntryType letIrrInflVariantType = LookupLexEntryType(Cache.ServiceLocator, variantTypeNameIIV, eng);
			ILexEntryRef newLer;
			SetupLexEntryVariant(Cache, "vaIrrInflVar", newBoundStem, letIrrInflVariantType, out newLer);
			var newVariant = newLer.Owner as ILexEntry;
			newVariant.MakeVariantOf(newMainEntry, letIrrInflVariantType);
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void GetVariantRef()
		{
			// Setup bound stem before mainEntry, and have varIrrInfVar pointing to both.
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, SetupBoundStemAndMainEntryWithVariant);

			ITsStrFactory sf = TsStrFactoryClass.Create();
			ITsString variantIrrInfl = sf.MakeString("vaIrrInflVar", Cache.DefaultVernWs);
			var variant = Cache.ServiceLocator.GetInstance<ILexEntryRepository>().GetHomographs("vaIrrInflVar").FirstOrDefault();

			/*
			 * Test for bound stem
			 */
			{
				var variantRef1 = DomainObjectServices.GetVariantRef(variant, false);
				var entry1 = variantRef1.ComponentLexemesRS[0] as ILexEntry;
				Assert.That(entry1, Is.Not.Null);
				Assert.That(entry1.LexemeFormOA.MorphTypeRA.Guid, Is.EqualTo(MoMorphTypeTags.kguidMorphBoundStem));
			}

			/*
			 * Test for excluding bound stem
			 */
			{
				var variantRef2 = DomainObjectServices.GetVariantRef(variant, true);
				var entry2 = variantRef2.ComponentLexemesRS[0] as ILexEntry;
				Assert.That(entry2, Is.Not.Null);
				Assert.That(entry2.LexemeFormOA.MorphTypeRA.Guid, Is.EqualTo(MoMorphTypeTags.kguidMorphStem));
			}
		}


		/// <summary>
		///
		/// </summary>
		[Test]
		public void GetVariantRefs()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, SetupBoundStemAndMainEntryWithVariant);
			ITsStrFactory sf = TsStrFactoryClass.Create();
			ITsString variantIrrInfl = sf.MakeString("vaIrrInflVar", Cache.DefaultVernWs);
			var variant = Cache.ServiceLocator.GetInstance<ILexEntryRepository>().GetHomographs("vaIrrInflVar").FirstOrDefault();

			/*
			 * Test for order of variant refs
			 */
			{
				var variantRefs = DomainObjectServices.GetVariantRefs(variant);
				Assert.That(variantRefs.Count(), Is.EqualTo(2));
				var varRef1 = variantRefs.ElementAt(0);
				var varRef2 = variantRefs.ElementAt(1);

				// First ref should point to bound stem
				{
					var entry1 = varRef1.ComponentLexemesRS[0] as ILexEntry;
					Assert.That(entry1, Is.Not.Null);
					Assert.That(entry1.LexemeFormOA.MorphTypeRA.Guid, Is.EqualTo(MoMorphTypeTags.kguidMorphBoundStem));
				}

				// Second should point to stem
				{
					var entry2 = varRef2.ComponentLexemesRS[0] as ILexEntry;
					Assert.That(entry2, Is.Not.Null);
					Assert.That(entry2.LexemeFormOA.MorphTypeRA.Guid, Is.EqualTo(MoMorphTypeTags.kguidMorphStem));
				}
			}

		}

		/// <summary>
		/// TODO: move +.pl or +.pst to LexGloss (see LT-9681).
		/// </summary>
		[Test]
		public void LexGlossInflectionalVariantOfEntry_WithoutLexEntryInflType()
		{

		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void LexGlossInflectionalVariantOfEntry_WitLexEntryInflType_Plural_GlossAppend()
		{

		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void LexGlossInflectionalVariantOfEntry_WitLexEntryInflType_Plural_GlossPrepend()
		{

		}


	}

	/// <summary>
	/// We expect Undo to undo all the data created during a test.
	/// To verify this, we run two tests, both creating a new entry and testing the same preconditions and postconditions
	/// </summary>
	[TestFixture]
	public class UndoAllIssueTest : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private void MakeSureEverythingIsUndoneAfterTests()
		{
			Assert.AreEqual(0, Cache.ServiceLocator.GetInstance<ILexEntryRepository>().Count, "Expected no initial entries");
			Assert.AreEqual(0, Cache.ServiceLocator.GetInstance<ILexSenseRepository>().Count, "Expected no initial senses");
			Assert.AreEqual(0, Cache.ServiceLocator.GetInstance<IMoFormRepository>().Count, "Expected no initial moForms");

			ITsString tssFullForm = TsStringUtils.MakeTss("entryToUndo", Cache.DefaultVernWs);
			var entryComponents = MorphServices.BuildEntryComponents(Cache,
																			tssFullForm);
			entryComponents.GlossAlternatives.Add(TsStringUtils.MakeTss("senseToUndo", Cache.DefaultVernWs));
			ILexEntry newEntry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(entryComponents);

			Assert.AreEqual(1, Cache.ServiceLocator.GetInstance<ILexEntryRepository>().Count);
			Assert.AreEqual(1, Cache.ServiceLocator.GetInstance<ILexSenseRepository>().Count);
			Assert.AreEqual(1, Cache.ServiceLocator.GetInstance<IMoFormRepository>().Count);
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void NewEntry()
		{
			MakeSureEverythingIsUndoneAfterTests();
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void NewEntry2()
		{
			MakeSureEverythingIsUndoneAfterTests();
		}
	}
}
